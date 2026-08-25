using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used to display a health bar that updates based on <see cref="Game.Player.PlayerCharacter"/> events
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private bool lerpGainedHealth;
    [SerializeField] private bool lerpLostHealth;
    [SerializeField] private float healthAnimationDelay;
    [Range(0.0f, 1.0f)][SerializeField] private float healthDecreaseRate;
    [Range(0.0f, 1.0f)][SerializeField] private float healthIncreaseRate;
    private int currentHealth = 0;
    private int currentMaxHealth = 0;
    [SerializeField] private float widthPer1Health = 0;

    [Header("Health Delta")]
    [SerializeField] private bool displayHealthDelta;
    [Range(0.0f, 1.0f)][SerializeField] private float healthDeltaThreshold;
    [Tooltip("If true, will stop any health lerping (if applicable) when lost health delta is displayed")]
    [SerializeField] private bool noHealthLerpOnLostHealthDeltaDisplay;
    [SerializeField] private Slider healthDeltaSlider;
    [SerializeField] private float healthDeltaAnimationDelay;
    [Range(0.0f, 1.0f)][SerializeField] private float healthDeltaDecreaseRate;
    private float healthDeltaFillImageDefaultAlpha = 1f;

    private Timer healthDeltaTimer;
    private float healthDeltaTargetLerpVal;

    private Timer healthTimer;
    private float healthTargetLerpVal;


    // Start is called before the first frame update
    void Awake()
    {
        healthSlider.minValue = 0;
        healthSlider.maxValue = 1;
        healthSlider.value = 1;

        healthDeltaSlider.gameObject.SetActive(false);

        healthDeltaTimer = new Timer();
        healthTimer = new Timer();
    }

    private void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        if (gameObject.name == "Stamina") 
            Debug.Log($"health timer running: {healthTimer.IsRunning()} health delta timer running: {healthDeltaTimer.IsRunning()}");
        healthTimer.Update(deltaTime);
        if (healthTimer.IsFinished())
        {
            float newValue;
            if (healthTargetLerpVal > healthSlider.value)
            {
                newValue = healthSlider.value + (healthIncreaseRate * deltaTime);
                if (newValue >= healthTargetLerpVal)
                { 
                    healthSlider.value = healthTargetLerpVal;
                    healthTimer.Reset();
                }
                else healthSlider.value = newValue;
            }
            else
            {
                newValue = healthSlider.value - (healthDecreaseRate * deltaTime);
                if (newValue <= healthTargetLerpVal)
                { 
                    healthSlider.value = healthTargetLerpVal;
                    healthTimer.Reset();
                }
                else healthSlider.value = newValue;
            }
        }

        healthDeltaTimer.Update(deltaTime);
        if (healthDeltaTimer.IsFinished())
        {
            //NOTE: health delta can ONLY go down since it would be covered if it went up
            float newValue = healthDeltaSlider.value - (healthDeltaDecreaseRate * deltaTime);
            if (newValue <= healthDeltaTargetLerpVal)
            {
                //If we have finished lerping to target health, we reset the timer
                //so we dont use finished state for  
                healthDeltaSlider.value = healthDeltaTargetLerpVal;
                healthDeltaSlider.gameObject.SetActive(false);
                healthDeltaTimer.Reset();
            }
            else healthDeltaSlider.value = newValue;
        }
    }


    public void UpdateHealthUI(int newHealth)
    {
        if (currentMaxHealth==0)
        {
            UnityEngine.Debug.LogError($"Tried to update health UI on {gameObject.name} with a newHealth value of {newHealth} but the max health has not been set yet! " +
                $"UpdateMaxHealthUI() must be called at least once before any UpdateHealthUI() calls!");
            return;
        }
        
        newHealth = Mathf.Clamp(newHealth, 0, currentMaxHealth);
        if (newHealth == currentHealth)
            return;

        int oldHealth = currentHealth;
        currentHealth = newHealth;
        int healthDelta = newHealth - oldHealth;
        float newHealthSliderValue= (float)newHealth / currentMaxHealth;

        bool showHealthDelta = displayHealthDelta &&
            (Mathf.Abs(healthDelta) / (float)currentMaxHealth) >= healthDeltaThreshold;
        //Only do the animation if we lose health since that is only when the animation will be visible
        //(because the current health slider will cover it up when gaining health)
        bool showLoseHealthDelta = showHealthDelta && healthDelta < 0.0f;
        if (showLoseHealthDelta)
        {
            //NOTE: if the slider is ALREADY ACTIVE it means we are already displaying losing health
            //either because it is in the animation delay OR it is already animating
            //AND since by default we need to set the delta slider to old health value to ensure
            //it starts from the correct position, we dont want to jump to lower value if multiple calls overlap
            if (!healthDeltaSlider.IsActive())
            {
                healthDeltaSlider.value = (float)oldHealth / currentMaxHealth;
                healthDeltaSlider.gameObject.SetActive(true);
            }
            StartDeltaHealthAnimation(newHealthSliderValue);
        }
        else if (showHealthDelta && healthDelta > 0.0f)
            healthDeltaSlider.value = (float)newHealth / currentMaxHealth;

        
        bool healthDeltaCanBeLerped = lerpGainedHealth && healthDelta > 0.0f ||
                                      lerpLostHealth && healthDelta < 0.0f;
        if (!healthDeltaCanBeLerped || (showLoseHealthDelta && noHealthLerpOnLostHealthDeltaDisplay))
            healthSlider.value = newHealthSliderValue;
        else StartHealthAnimation(newHealthSliderValue);
    }

    public void UpdateMaxHealthUI(int newMaxHealth)
    {
        RectTransform rect = healthSlider.GetComponent<RectTransform>();

        if (widthPer1Health == 0) widthPer1Health= rect.sizeDelta.x / newMaxHealth;
        float newWidth= newMaxHealth * widthPer1Health;

        rect.anchorMin = new Vector2(0.0f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(0.0f, rect.anchorMax.y);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

        currentMaxHealth=newMaxHealth;
        currentHealth = currentMaxHealth;
    }

    private void StartDeltaHealthAnimation(float newSliderValue)
    {
        //NOTE: if we start an animation when the timer is already running (or is completed)
        //we do not want to start another timer, but instead we just add to the health we need to change
        if (healthDeltaTimer.IsInactive()) healthDeltaTimer.StartTimer(healthDeltaAnimationDelay);
        healthDeltaTargetLerpVal = newSliderValue;
    }
    private void StartHealthAnimation(float newSliderValue)
    {
        if (healthTimer.IsInactive()) healthTimer.StartTimer(healthAnimationDelay);
        healthTargetLerpVal = newSliderValue;
    }

    /// <summary>
    /// Will reset the max health to the current max health. 
    /// Useful if the rect transform was changed (while the current max health was not updated) 
    /// and you need to reset the rect transform to the actual value defined
    /// </summary>
    public void RevertMaxHealth() { UpdateMaxHealthUI(currentMaxHealth); }

    /// <summary>
    /// Will reset the health UI to the current health. 
    /// Useful if the health bar value was changed (while the current health was not updated) 
    /// and you need to reset the value to the actual value defined
    /// </summary>
    public void RevertHealthUI() { UpdateHealthUI(currentHealth); }
}
