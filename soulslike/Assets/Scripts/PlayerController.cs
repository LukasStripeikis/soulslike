using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;

public enum PlayerStateName : uint
{
    Idle            = 0,
    CrouchIdle      = 1,
    CrouchWalking   = 2,
    Walking         = 3,
    Running         = 4,
    Sprinting       = 5,
    Jumping         = 6,
    Falling         = 7,
    Rolling         = 8,
}
public enum PlayerHorizontalMoveMode : uint
{
    Idle        = 0,
    CrouchIdle  = 1,
    CrouchWalk  = 2,
    Walk        = 3,
    Run         = 4,
    Sprint      = 5
}

[System.Serializable]
public class PlayerState
{
    public PlayerStateName StateName;
    public AnimationClip StateClip;
    public float StateTime;
}

[System.Serializable]
public class PlayerStateTransition
{
    public PlayerStateName StartState;
    public PlayerStateName EndState;
    public float TransitionTime;
}

[System.Serializable]
public class MovementSettings
{
    public float Acceleration;
    public float Decceleration;
    public float MaxSpeed;
    public float RotateSpeed;
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    /// <summary>
    /// The min magnitude of the 2d horizontal move input which must occur
    /// to transition from a walk to a run. NOTE: the walk/run distinction is only
    /// possible on gamepad as keyboard can not have walk state due to only allowing integer
    /// move input values (you either move forward or not, no in-between like on a joystick)
    /// </summary>
    [SerializeField] private float runInputMagnitudeThreshold;
    private Vector2 moveInput = Vector2.zero;
    private bool isJumpButtonPressed = false;
    private bool isCrouchButtonPressed = false;
    private bool isSprintButtonHeld = false;
    private bool isDodgeButtonPressed = false;
    private bool isLockOnButtonPressed = false;

    [Header("Movement")]
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private float idleRotateSpeed;
    /// <summary>
    /// Walking occurs when the horizontal move input is slightly pressed
    /// for digital input like gamepad joystick (no walk on keyboard)
    /// </summary>
    [SerializeField] private MovementSettings walkMoveSettings;
    /// <summary>
    /// Running occurs when the horizontal move input is fully pressed
    /// for digital input like gamepads or when any press occurs for keyboard
    /// </summary>
    [SerializeField] private MovementSettings runMoveSettings;
    /// <summary>
    /// Sprint occurs when a separate modifier key/button is pressed
    /// along with the horizontal move input
    /// </summary>
    [SerializeField] private MovementSettings sprintMoveSettings;

    [Space(10)]
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float jumpGravity;
    [SerializeField] private float fallGravity;
    [SerializeField] private Transform groundedBoxCenter;
    [SerializeField] private Vector3 groundedBoxHalfExtents;
    [SerializeField] private LayerMask groundMask;

    [Space(10)]
    [SerializeField] private bool doCrouching;
    [SerializeField] private float crouchSpeed;
    private Vector3 velocity = Vector3.zero;
    private Vector3 horizontalMoveVelocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;
    private Vector3 newAcceleration = Vector3.zero;
    private bool updateInput = true;

    [Space(10)]
    [SerializeField] private bool doLockOn;
    [SerializeField] private float lockOnRadius;
    [SerializeField] private LayerMask lockOnLayers;
    private Vector3 targetFacingDirection;
    private Transform lockOnTarget = null;
    public const int MAX_LOCKABLE_TARGETS_DURING_TEST = 32;
    //NOTE: to help reduce reallocation cost, we set limit of the stored entires
    //so we can reuse the same array but that also means we can not have more than this
    //amount in any given area to be locked onto before testing again
    private Collider[] lockableTargets= new Collider[MAX_LOCKABLE_TARGETS_DURING_TEST];
    

    [Header("States")]
    [SerializeField] private PlayerStateName startState;
    [SerializeField] private PlayerState[] overrideStateData;
    [SerializeField] private PlayerStateTransition[] stateTransitions;
    private StateMachine stateMachine;

    [Header("Animation")]
    [SerializeField] private PlayerAnimator animator;
    [SerializeField] private Transform boneRigRoot;
    /// <summary>
    /// NOTE: since some animations may move the bone rig away from player position
    /// if this is true, we will update the player pos to match the bone rig
    /// </summary>
    [SerializeField] private bool updatePosToBoneRig;
    [SerializeField] private float updatePosToBoneRigThreshold;
    Vector3 targetBoneRigPlayerOffset;

    [Header("Stats")]
    [SerializeField] private int maxHealth;
    private int health;

    [Space(10)]
    [SerializeField] private int maxMana;
    private int mana;

    [Space(10)]
    [SerializeField] private bool doStamina;
    [SerializeField] private int maxStamina;
    [SerializeField] private float sprintStaminaLossRate;
    [SerializeField] private float staminaGainRate;
    [SerializeField] private float rollStaminaCost;
    private float stamina;

    [Header("Rally")]
    [SerializeField] private bool doRallyHealth;
    [SerializeField] private float rallyDuration;
    [SerializeField] private float rallyPercentageLostPerSecond;
    private float rallyHealth;

    public static PlayerController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        health = maxHealth;
        mana = maxMana;
        stamina = maxStamina;

        if (boneRigRoot != null) targetBoneRigPlayerOffset = boneRigRoot.transform.position - transform.position;
        targetFacingDirection = transform.forward;

        PlayerStateName[] playerStateNames = (PlayerStateName[])Enum.GetValues(typeof(PlayerStateName));
        int stateSize = playerStateNames.Length;
        State[] states = new State[stateSize];

        int overrideStatesSize = overrideStateData.Length;
        for (int i=0; i<stateSize; i++)
        {   
            PlayerStateName stateName = playerStateNames[i];
            PlayerState playerState = null;
            for (int j=0; j<overrideStatesSize; j++)
            {
                if (overrideStateData[j].StateName == stateName)
                {
                    playerState = overrideStateData[j];
                    break;
                }
            }

            float stateDuration;
            if (playerState != null)
            {
                if (playerState.StateClip != null) playerState.StateTime = playerState.StateClip.length;
                stateDuration = playerState.StateTime;
            }
            else stateDuration = State.INDEFINITE_STATE_DURATION;
            states[i] = new State((uint)stateName, stateDuration);
        }

        int transitionSize = this.stateTransitions.Length;
        StateTransition[] transitions = new StateTransition[transitionSize];
        for (int i=0; i<transitionSize; i++)
        {
            PlayerStateTransition transition = this.stateTransitions[i];
            transitions[i] = new StateTransition((uint)transition.StartState, 
                (uint)transition.EndState, transition.TransitionTime);
        }

        stateMachine = new StateMachine(states, transitions, (uint)startState);
        stateMachine.OnStateEnd += (State state) =>
        {
            PlayerStateName playerStateName = (PlayerStateName)state.GetId();
            if (playerStateName == PlayerStateName.Rolling) LockMovement(false);
        };
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        newAcceleration = Vector3.zero;
        HandleVerticalMovement();
        HandleHorizontalMovement(deltaTime);

        transform.position += velocity * deltaTime;

        acceleration = newAcceleration;
        velocity += acceleration * deltaTime;
    }
    void Update()
    {
        float deltaTime = Time.deltaTime;
        stateMachine.Update(deltaTime);
        HandleInput();
        
        if (stamina < maxStamina) SetStaminaDelta(staminaGainRate * deltaTime);

        if (HasEnoughStamina(rollStaminaCost) && isDodgeButtonPressed)
            StartRoll();

        if (doLockOn)
        {
            if (isLockOnButtonPressed) TryToggleLockOn();
            //NOTE: rolling always occurs in the direction we are facing, and since facing dir is modified
            //when we are locked on, if we force same lock on dir when rolling, we would always roll forward
            //into the lock on target, which is not what we want
            if (IsLockedOntoATarget() && !IsState(PlayerStateName.Rolling)) ForceFaceLockOnTarget();
        }

        if (updatePosToBoneRig && boneRigRoot != null)
            UpdatePosFromBoneRig();

        Debug.Log($"Player state: {(PlayerStateName)stateMachine.GetCurrentState().GetId()} "+
        $"v: {velocity} a: {acceleration} isGrounded:{IsGroundedState()}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(groundedBoxCenter.position,
            transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, groundedBoxHalfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.DrawWireSphere(transform.position, lockOnRadius);
    }

    private void HandleInput()
    {
        if (updateInput)
        {
            moveInput = inputActions["Move"].ReadValue<Vector2>().normalized;
            isSprintButtonHeld = Utils.IsActionHeld(inputActions["Sprint"]);
            isJumpButtonPressed = inputActions["Jump"].WasPressedThisFrame();
            isCrouchButtonPressed = inputActions["Crouch"].WasPressedThisFrame();
            isDodgeButtonPressed = inputActions["Roll"].WasPressedThisFrame();
            isLockOnButtonPressed = inputActions["LockOn"].WasPressedThisFrame();
        }
        else
        {
            moveInput = Vector2.zero;
            isSprintButtonHeld = false;
            isJumpButtonPressed = false;
            isCrouchButtonPressed = false;
            isDodgeButtonPressed = false;
            isLockOnButtonPressed = false;
        }

        if (animator != null) animator.SetMoveInput(moveInput);
    }

    private void UpdatePosFromBoneRig()
    {
        Vector3 boneRigOffset = boneRigRoot.transform.position - transform.position - targetBoneRigPlayerOffset;
        boneRigOffset.y = 0.0f;
        if (boneRigOffset.sqrMagnitude >= updatePosToBoneRigThreshold * updatePosToBoneRigThreshold)
        {
            //Debug.Log($"Update to bone rig pos offset: {transform.position} + {boneRigOffset} (mag:{boneRigOffset.magnitude})");
            transform.position += boneRigOffset;
            boneRigRoot.transform.position = transform.position + targetBoneRigPlayerOffset;
        }
    }

    private void HandleVerticalMovement()
    {
        bool foundGroundCollider = Physics.CheckBox(groundedBoxCenter.position, 
            groundedBoxHalfExtents, transform.rotation, groundMask);
        //Debug.Log($"Stamina: {stamina}");
        //Debug.Log($"Is grounded:{IsGrounded()} found collider: {foundGroundCollider} "+
        //$"state:{(PlayerStateName)stateMachine.GetCurrentState().GetId()} vel: {velocity} acceleration:{acceleration}");
        if (IsGroundedState())
        {
            //If state machine says we are grounded but we find no ground collider,
            //it means we may have been suddenly moved or walked off edge, so we enter freefall
            if (!foundGroundCollider) StartFall();            
            else if (!IsCrouchingState() && isJumpButtonPressed) StartJump();
        }
        //If we are not grounded and are in jumping state, but have begun moving down,
        //the jump has ended and now gravity is pulling back down so we transition to fall
        else if (IsState(PlayerStateName.Jumping) && velocity.y < 0.0f) StartFall();
        //If we have are NOT grounded but have found the ground collider,
        //it means we have finished fall and hit ground, so we stop gravity
        else if (foundGroundCollider) HandleLand();

        if (IsState(PlayerStateName.Jumping)) newAcceleration.y -= jumpGravity;
        else if (IsState(PlayerStateName.Falling)) newAcceleration.y -= fallGravity;
    }
    private void HandleHorizontalMovement(float deltaTime)
    {
        //NOTE: if we are not grounded we dont do horizontal movement, BUT
        //if player rolls, the roll can NOT be canceled and thus the horizontal movement
        //may not change during the roll
        if (!IsGroundedState() || IsState(PlayerStateName.Rolling))
            return;

        if (isCrouchButtonPressed && doCrouching) ToggleCrouch();

        Vector3 horizontalMoveDir = CalculateHorizontalMoveDir();
        Debug.Log($"Move dir: {horizontalMoveDir}");
        if (Utils.VecApproxEquals(horizontalMoveDir, Vector3.zero))
        {
            //If we dont have move input, but we have registered a move input velocity
            //while still grounded, we stop the movement and only if we are not crouching
            //we revert back to idle since crouching state can exist for velocity >= 0
            //as long as the crouch button is toggled
            if (horizontalMoveVelocity.sqrMagnitude > 0.0f)
                StopHorizontalMovement();

            return;
        }

        PlayerStateName newMoveState = PlayerStateName.Running;
        if (isSprintButtonHeld && HasStamina()) newMoveState = PlayerStateName.Sprinting;
        else if (moveInput.magnitude < runInputMagnitudeThreshold) newMoveState = PlayerStateName.Walking;
        HandleWalkRunSprint(newMoveState, horizontalMoveDir, deltaTime);

        //If we have a lock on target, we constantly update the facing direction to the target
        //in case target moves and NOT just when the player moves, so to not mess with the lock on updating
        //we dont update rotation here
        if (IsLockedOntoATarget())
            return;

        targetFacingDirection = horizontalMoveDir;
        float rotateSpeed = idleRotateSpeed;
        if (newMoveState == PlayerStateName.Walking) rotateSpeed = walkMoveSettings.RotateSpeed;
        else if (newMoveState == PlayerStateName.Running) rotateSpeed = runMoveSettings.RotateSpeed;
        else if (newMoveState == PlayerStateName.Sprinting) rotateSpeed = sprintMoveSettings.RotateSpeed;

        Quaternion targetRotation = Quaternion.LookRotation(targetFacingDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, rotateSpeed * deltaTime);
    }
    private Vector3 CalculateHorizontalMoveDir()
    {
        Camera camera = playerCamera.GetCamera();
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraRight = camera.transform.right;

        //NOTE: since the camera can be rotated in pitch, the camera forward
        //and right y dir may != 0, but if we used non-0 value it would force
        //player to go into the ground or sky, so we only consider horizontal dir
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        //NOTE: this is not too different from doing moveInput * transform.forward
        //BUT since camera may be opposite to player forward (ex. player front faces camera) 
        // due to rotation we want BACK horizontal input in that case to move the 
        // player FORWARD as it is more intuitive control input
        Vector3 moveDir = cameraForward * moveInput.y + cameraRight * moveInput.x;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        return moveDir;
    }
    private void HandleWalkRunSprint(PlayerStateName state, Vector3 moveDir, float deltaTime)
    {
        MovementSettings movementSettings = null;
        PlayerHorizontalMoveMode moveMode = PlayerHorizontalMoveMode.Idle;
        if (state == PlayerStateName.Walking)
        {
            movementSettings = walkMoveSettings;
            moveMode = PlayerHorizontalMoveMode.Walk;
        }
        else if (state == PlayerStateName.Running)
        {
            movementSettings = runMoveSettings;
            moveMode = PlayerHorizontalMoveMode.Run;
        }
        else if (state == PlayerStateName.Sprinting)
        {
            movementSettings = sprintMoveSettings;
            moveMode = PlayerHorizontalMoveMode.Sprint;
            SetStaminaDelta(-sprintStaminaLossRate * deltaTime);
        }

        if (!IsState(state)) SetState(state);
        if (animator != null) animator.SetHorizontalMoveMode(moveMode);

        Vector3 targetHorizontalVelocity = moveDir * movementSettings.MaxSpeed;
        Vector3 horizontalMoveAcceleration = Vector3.zero;
        //If the amount of acceleration we need to reach the target velocity is greater than the move settings'
        //acceleration, it means we can use the move settings acceleration, otherwise we must set it to the 
        //required amount (which would be less than move settings acceleration) to make ensure we dont go over target velocity
        horizontalMoveVelocity = Utils.MoveTowards(horizontalMoveVelocity, targetHorizontalVelocity, 
            movementSettings.Acceleration, deltaTime, ref horizontalMoveAcceleration);
        
        newAcceleration += horizontalMoveAcceleration;
    }
    private void StopHorizontalMovement()
    {
        if (IsCrouchingState())
        {
            SetState(PlayerStateName.CrouchIdle);
            if (animator != null) animator.SetHorizontalMoveMode(PlayerHorizontalMoveMode.CrouchIdle);
        }
        else
        {
            SetState(PlayerStateName.Idle);
            if (animator != null) animator.SetHorizontalMoveMode(PlayerHorizontalMoveMode.Idle);
        }

        velocity -= horizontalMoveVelocity;
        horizontalMoveVelocity = Vector3.zero;
    }

    private void StartJump()
    {
        SetState(PlayerStateName.Jumping);
        if (animator != null) animator.SetActionTrigger(PlayerAnimationTrigger.Jump);

        velocity.y += jumpSpeed;
    }
    private void StartFall()
    {
        SetState(PlayerStateName.Falling);
        if (animator != null) animator.SetActionTrigger(PlayerAnimationTrigger.Fall);
    }
    private void HandleLand()
    {
        SetState(PlayerStateName.Idle);
        if (animator != null) animator.SetActionTrigger(PlayerAnimationTrigger.Land);

        acceleration.y += fallGravity;
        velocity.y = 0.0f;
    }

    private void StartRoll()
    {
        SetState(PlayerStateName.Rolling);

        //NOTE: since we always roll in the facing direction and facing dir is modified to look
        //towards target in lock on mode, we dont always want to roll forward toward target, so
        //we modify temporarily while in roll mode to face in the direction of the velocity
        if (IsLockedOntoATarget())
        {
            targetFacingDirection = horizontalMoveVelocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetFacingDirection, Vector3.up);
            transform.rotation = targetRotation;
            Debug.Log($"Updated roll dir to: {targetFacingDirection}");
        }
        if (animator != null) animator.SetActionTrigger(PlayerAnimationTrigger.Roll);

        SetStaminaDelta(-rollStaminaCost);
        LockMovement(true);
    }

    private void ToggleCrouch()
    {
    }

    private void TryToggleLockOn()
    {
        if (lockOnTarget != null)
        {
            lockOnTarget = null;
            playerCamera.EnableOrbitMode();
            if (animator != null) animator.SetLockedOn(false);
        }
        else
        {
            int lockOnTargetCount = Physics.OverlapSphereNonAlloc(transform.position, lockOnRadius, 
                lockableTargets, lockOnLayers);
            if (lockOnTargetCount == 0) return;

            Collider closestTarget = null;
            float smallestTargetDistSquared = float.MaxValue;
            float currentTargetDistSquared = 0.0f;
            //NOTE: yes it is not optimized to use lienar search every time when we want to lock onto a new target
            //even if we switch to a different one from lock on state, but technically, after the first time 
            //we find enemies when going into lock on, there could be more enemies that enter the area or get closer
            //when we switch to a different target, thus sorting by distance somewhat useless
            for (int i=0; i<lockOnTargetCount; i++)
            {
                currentTargetDistSquared = (lockableTargets[i].transform.position - transform.position).sqrMagnitude;
                if (currentTargetDistSquared < smallestTargetDistSquared)
                {
                    smallestTargetDistSquared = currentTargetDistSquared;
                    closestTarget = lockableTargets[i];
                }
            }

            lockOnTarget = closestTarget.transform;
            playerCamera.EnableLookatMode(lockOnTarget, true);
            if (animator != null) animator.SetLockedOn(true);
        }
    }

    private void ForceFaceLockOnTarget()
    {
        targetFacingDirection = lockOnTarget.position - transform.position;
        targetFacingDirection.y= 0.0f;
        targetFacingDirection.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(targetFacingDirection, Vector3.up);
        transform.rotation = targetRotation;
    }

    public bool IsCrouchingState()
    {
        return stateMachine.IsState((uint)PlayerStateName.CrouchIdle) || 
               stateMachine.IsState((uint)PlayerStateName.CrouchWalking);
    }
    public bool IsMovingHorizontallyState()
    {
        return stateMachine.IsState((uint)PlayerStateName.Walking)    || 
               stateMachine.IsState((uint)PlayerStateName.Running)    ||
               stateMachine.IsState((uint)PlayerStateName.Sprinting)  ||
               stateMachine.IsState((uint)PlayerStateName.CrouchWalking);
    }
    public bool IsGroundedState()
    {
        return !stateMachine.IsState((uint)PlayerStateName.Jumping) && 
               !stateMachine.IsState((uint)PlayerStateName.Falling);
    }
    public bool IsLockedOntoATarget() { return lockOnTarget != null; }
    public Transform GetLockOnTarget() { return lockOnTarget; }

    public bool IsState(PlayerStateName state)
    {
        return stateMachine.IsState((uint)state);
    }
    private bool SetState(PlayerStateName state)
    {
        return stateMachine.TrySetState((uint)state);
    }

    private void SetHealthDelta(int delta)
    {
        health = Mathf.Clamp(health + delta, 0, maxHealth);
    }
    public void LoseHealth(int delta)
    {
        SetHealthDelta(delta);
    }
    public void GainHealth(int delta)
    {
        SetHealthDelta(delta);
    }

    public int GetMaxHealth() { return maxHealth; }
    public int GetHealth() { return health; }

    public int GetMaxMana() { return maxMana; }
    public int GetMana() { return mana; }

    private void SetStaminaDelta(float delta)
    {
        if (!doStamina)
            return;

        stamina= Mathf.Clamp(stamina + delta, 0, maxStamina);
    }
    public int GetMaxStamina() { return maxStamina; }
    public int GetStamina() { return (int)stamina; }
    public bool HasStamina() { return stamina > 0; }
    private bool HasEnoughStamina(float staminaConsumed)
    {
        if (!doStamina)
            return true;

        return stamina - staminaConsumed > 0.0f;
    }

    private void LockMovement(bool doLock)
    {
        updateInput = !doLock;
    }
}
