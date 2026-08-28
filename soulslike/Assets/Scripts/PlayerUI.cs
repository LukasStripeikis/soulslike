using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private HealthBar manaBar;
    [SerializeField] private HealthBar staminaBar;

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
    }
}
