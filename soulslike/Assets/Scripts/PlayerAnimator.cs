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
}
