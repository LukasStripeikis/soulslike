using UnityEngine;

public enum PlayerAnimationTrigger : uint
{
    Jump    = 0,
    Fall    = 1,
    Land    = 2,
    Roll    = 3,
}

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    private const string PARAM_F_ANIM_SPEED= "AnimationSpeed";
    private const string PARAM_I_HORIZONTAL_MOVE_MODE= "HorizontalMoveMode";
    private const string PARAM_I_IS_LOCKED_ON = "InLockedOn";
    private const string PARAM_F_MOVE_DIR_X = "MoveInputX";
    private const string PARAM_F_MOVE_DIR_Y = "MoveInputY";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetAnimationSpeed(float speed)
    {
        animator.SetFloat(PARAM_F_ANIM_SPEED, speed);
    }
    public void SetActionTrigger(PlayerAnimationTrigger trigger)
    {
        animator.SetTrigger(trigger.ToString());
    }
    public void SetHorizontalMoveMode(PlayerHorizontalMoveMode moveMode)
    {
        animator.SetInteger(PARAM_I_HORIZONTAL_MOVE_MODE, (int)moveMode);
    }
    public void SetLockedOn(bool isLockedOn)
    {
        animator.SetBool(PARAM_I_IS_LOCKED_ON, isLockedOn);
    }
    public void SetMoveInput(Vector2 moveInput)
    {
        animator.SetFloat(PARAM_F_MOVE_DIR_X, moveInput.x);
        animator.SetFloat(PARAM_F_MOVE_DIR_Y, moveInput.y);
    }
}
