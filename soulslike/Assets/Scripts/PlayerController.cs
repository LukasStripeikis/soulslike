using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;

public enum PlayerStateName : uint
{
    Idle        = 0,
    Crouching   = 1,
    Walking     = 2,
    Sprinting   = 3,
    Jumping     = 4,
    Falling     = 5,
    Rolling     = 6,
}

[System.Serializable]
public class PlayerState
{
    public PlayerStateName StateName;
    public float StateTime;
}

[System.Serializable]
public class PlayerStateTransition
{
    public PlayerStateName StartState;
    public PlayerStateName EndState;
    public float TransitionTime;
}

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float minMoveInputValue;
    [SerializeField] private float minRotationInputValue;
    private Vector2 moveInput = Vector2.zero;
    private bool isJumpButtonPressed = false;
    private bool isCrouchButtonPressed = false;
    private bool isSprintButtonHeld = false;

    [Header("Movement")]
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float rotateSpeed;

    [Space(10)]
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private Transform groundedBoxCenter;
    [SerializeField] private Vector3 groundedBoxHalfExtents;
    [SerializeField] private LayerMask groundMask;
    private Vector3 velocity = Vector3.zero;
    private Vector3 moveInputVelocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;

    [Header("States")]
    [SerializeField] private PlayerStateName startState;
    [SerializeField] private PlayerState[] overrideStateData;
    [SerializeField] private PlayerStateTransition[] stateTransitions;
    private StateMachine stateMachine;

    [Header("Stats")]
    [SerializeField] private int maxHealth;
    private int health;

    [SerializeField] private int maxMana;
    private int mana;

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
            float stateDuration = playerState!=null? playerState.StateTime : State.INDEFINITE_STATE_DURATION;
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
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        bool foundGroundCollider = Physics.CheckBox(groundedBoxCenter.position, 
            groundedBoxHalfExtents, transform.rotation, groundMask);
        Debug.Log($"Stamina: {stamina}");
        //Debug.Log($"Is grounded:{IsGrounded()} found collider: {foundGroundCollider} "+
        //$"state:{(PlayerStateName)stateMachine.GetCurrentState().GetId()} vel: {velocity} acceleration:{acceleration}");
        if (IsGrounded())
        {
            //If state machine says we are grounded but we find no ground collider,
            //it means we may have been suddenly moved or walked off edge, so we enter freefall
            if (!foundGroundCollider)
            {
                SetState(PlayerStateName.Falling);
                ApplyGravity();
            }
            //If we are grounded, found ground, NOT crouching, we check for jumping
            else if (!IsState(PlayerStateName.Crouching) && isJumpButtonPressed)
            {
                SetState(PlayerStateName.Jumping);
                velocity.y += jumpSpeed;
                ApplyGravity();
            }
        }
        //If we are not grounded and are in jumping state, but have begun moving down,
        //the jump has ended and now gravity is pulling back down so we transition to fall
        else if (IsState(PlayerStateName.Jumping) && velocity.y < 0.0f)
        {
            SetState(PlayerStateName.Falling);
        }
        //If we have are NOT grounded but have found the ground collider,
        //it means we have finished fall and hit ground, so we stop gravity
        else if (foundGroundCollider)
        {
            SetState(PlayerStateName.Idle);
            acceleration.y += gravity;
            velocity.y = 0.0f;
        }

        if (IsGrounded())
        {
            //Left/Right move dir determines player XZ plane (ground) rotation
            if (Mathf.Abs(moveInput.x) >= minRotationInputValue)
            {
                Vector3 newRotation = transform.eulerAngles;
                newRotation.y += moveInput.x * rotateSpeed * deltaTime;
                transform.eulerAngles = newRotation;
            }

            //Crouch button toggles crouch state
            if (isCrouchButtonPressed)
            {
                if (IsState(PlayerStateName.Crouching)) SetState(PlayerStateName.Idle);
                else SetState(PlayerStateName.Crouching);
            }

            //Up/Down move dir determines forward movement with either sprinting or walking 
            if (Mathf.Abs(moveInput.y) > minMoveInputValue)
            {
                float moveSpeed;
                if (isSprintButtonHeld && HasStamina())
                {
                    SetState(PlayerStateName.Sprinting);
                    moveSpeed = sprintSpeed;
                }
                else if (IsState(PlayerStateName.Crouching))
                {
                    moveSpeed = crouchSpeed;
                }
                else
                {
                    SetState(PlayerStateName.Walking);
                    moveSpeed = walkSpeed;
                }
                //Since we may get velocity from other places we dont want to mess with it
                //so we keep track how much velocity input adds and reset it every frame
                velocity -= moveInputVelocity;
                moveInputVelocity = transform.forward * moveSpeed * Mathf.Sign(moveInput.y);
                velocity += moveInputVelocity;
            }
            //If we dont have move input, but we have registered a move input velocity
            //while still grounded, we stop the movement and only if we are not crouching
            //we revert back to idle since crouching state can exist for velocity >= 0
            //as long as the crouch button is toggled
            else if (moveInputVelocity.sqrMagnitude > 0.0f)
            {
                if (!IsState(PlayerStateName.Crouching)) SetState(PlayerStateName.Idle);
                velocity -= moveInputVelocity;
                moveInputVelocity = Vector3.zero;
            }
        }

        //NOTE: its best to set rigidbody's velocity then set pos ourselves
        //since collisions may become impacted if the body is just teleported rather than knows
        //its movement itself
        //rigidbody.linearVelocity = velocity;
        transform.position += velocity * deltaTime;
        velocity += acceleration * deltaTime;
    }
    void Update()
    {
        float deltaTime = Time.deltaTime;
        stateMachine.Update(deltaTime);

        moveInput = inputActions["Move"].ReadValue<Vector2>().normalized;
        isSprintButtonHeld = Utils.IsActionHeld(inputActions["Sprint"]);
        isJumpButtonPressed = inputActions["Jump"].WasPressedThisFrame();
        isCrouchButtonPressed = inputActions["Crouch"].WasPressedThisFrame();

        if (IsState(PlayerStateName.Sprinting)) SetStaminaDelta(-sprintStaminaLossRate * deltaTime);
        else if (stamina < maxStamina) SetStaminaDelta(staminaGainRate * deltaTime);

        if (HasEnoughStamina(rollStaminaCost) && inputActions["Roll"].WasPressedThisFrame())
        {
            SetState(PlayerStateName.Rolling);
            SetStaminaDelta(-rollStaminaCost);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(groundedBoxCenter.position,
            transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, groundedBoxHalfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }

    public bool IsWalkingOrSprinting()
    {
        return stateMachine.IsState((uint)PlayerStateName.Walking) || 
               stateMachine.IsState((uint)PlayerStateName.Sprinting);
    }
    public bool IsWalkingSprintingOrCrouching()
    {
        return IsWalkingOrSprinting() || 
               stateMachine.IsState((uint)PlayerStateName.Crouching);
    }
    public bool IsGrounded()
    {
        return !stateMachine.IsState((uint)PlayerStateName.Jumping) && 
               !stateMachine.IsState((uint)PlayerStateName.Falling);
    }
    public bool IsState(PlayerStateName state)
    {
        return stateMachine.IsState((uint)state);
    }
    private bool SetState(PlayerStateName state)
    {
        return stateMachine.TrySetState((uint)state);
    }

    private void ApplyGravity()
    {
        acceleration.y -= gravity;
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
        stamina= Mathf.Clamp(stamina + delta, 0, maxStamina);
    }
    public int GetMaxStamina() { return maxStamina; }
    public int GetStamina() { return (int)stamina; }
    public bool HasStamina() { return stamina > 0; }
    private bool HasEnoughStamina(float staminaConsumed)
    {
        return stamina - staminaConsumed > 0.0f;
    }
}
