using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [Header("Stat Bars")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar manaBar;
    [SerializeField] private HealthBar staminaBar;

    [Header("Lock On")]
    [SerializeField] private RectTransform lockOnUI;
    [SerializeField] private RectTransform lockOnCenterIcon;
    [SerializeField] private Vector3 lockOnTargetHealthWorldOffset;
    [SerializeField] private RectTransform lockOnTargetHealth;
    [SerializeField] private HealthBar lockOnTargetHealthBar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBar.UpdateMaxHealthUI(PlayerController.Instance.GetMaxHealth());
        manaBar.UpdateMaxHealthUI(PlayerController.Instance.GetMaxMana());
        staminaBar.UpdateMaxHealthUI(PlayerController.Instance.GetMaxStamina());
    }

    // Update is called once per frame
    void LateUpdate()
    {
        healthBar.UpdateHealthUI(PlayerController.Instance.GetHealth());
        manaBar.UpdateHealthUI(PlayerController.Instance.GetMana());
        staminaBar.UpdateHealthUI(PlayerController.Instance.GetStamina());

        HandleLockOn();
    }

    private void HandleLockOn()
    {
        Transform lockOnTarget = PlayerController.Instance.GetLockOnTarget();
        if (lockOnTarget == null)
        {
            if (lockOnUI.gameObject.activeSelf)
                lockOnUI.gameObject.SetActive(false);
            return;
        }

        Vector3 targetIconScreenPos = Camera.main.WorldToScreenPoint(lockOnTarget.position);
        lockOnCenterIcon.position = targetIconScreenPos;

        if (lockOnTarget.TryGetComponent(out Health health))
        {
            Vector3 targetHealthScreenPos = Camera.main.WorldToScreenPoint(lockOnTarget.position + lockOnTargetHealthWorldOffset);
            lockOnTargetHealth.position = targetHealthScreenPos;
            lockOnTargetHealth.gameObject.SetActive(true);

            lockOnTargetHealthBar.UpdateMaxHealthUI(health.GetMaxHealth());
            lockOnTargetHealthBar.UpdateHealthUI(health.GetHealth());
        }
        else lockOnTargetHealth.gameObject.SetActive(false);

        lockOnUI.gameObject.SetActive(true);
    }
}
