using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>
    /// Manages the game's user interface. Only one ever exists in a scene. Access the Singleton Instance with <see cref="UIManager.Instance"/>
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class UIManager: MonoBehaviour
    {
        [Header("Tooltips")]
        [SerializeField] private GameObject tooltipContainer;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private GameObject tooltipBackground;

        //[Header("Global Text")]
        //[SerializeField] private GlobalTextManager textManager;

        public static UIManager Instance { get; private set; }

        [Header("Editor")]
        [Tooltip("When \"Replace Scene TMPro Text With Override\" button clicked below, ALL TMPro Text GameObjects will have their font replaced with this one")] 
        [SerializeField] private TMP_FontAsset overrideFontAsset = null;
        public TMP_FontAsset OverrideFontAsset { get => overrideFontAsset; }

        /*
        [Tooltip("When \"Override Scene Tooltip Settings\" button clicked below, ALL Tooltip component GameObjects will have their settings replaced with tis one")]
        [SerializeField] private TooltipSettingsSO overrideTooltipSettings = null;
        public TooltipSettingsSO OverrideTooltipSettings { get => overrideTooltipSettings; }
        */


        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        private void Start()
        {
            //DisableTooltip();
        }

        // Update is called once per frame
        private void Update()
        {

        }

        /*
        public void EnableTooltip(TooltipInfo tooltipInfo,PointerEventData eventData)
        {
            Vector3 newPosition;

            //if we don't change the position, make it the mouse position
            if (tooltipInfo.NewTooltipPosition == null)
            {
                //Vector2 screenPosition = Mouse.current.position.ReadValue();
                //newPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                newPosition = eventData.position;
            }
            else newPosition = (Vector3)tooltipInfo.NewTooltipPosition;

            if (tooltipInfo.ExtraBuffer != null) newPosition.x += (float)tooltipInfo.ExtraBuffer;

            tooltipText.enableWordWrapping = false;
            tooltipText.text = tooltipInfo.Text;

            float tooltipTextWidth = -1;
            RectTransform transform = tooltipBackground.GetComponent<RectTransform>();
            //If we can, we change the size of the panel to match the text size
            if (tooltipBackground != null && tooltipText != null)
            {
                tooltipTextWidth = tooltipText.preferredWidth;
                transform.sizeDelta = new Vector2(tooltipText.preferredWidth, tooltipText.preferredHeight);
            }

            //If we have a bigger width, we want to reset the text settings when wrapping is enabled
            if (tooltipTextWidth>= tooltipInfo.NextLineThreshold)
            {
                tooltipText.enableWordWrapping = true;
                transform.sizeDelta = new Vector2(tooltipInfo.NextLineThreshold, tooltipText.preferredHeight);
            }

            //If the new position's textbox is past the screen space on the right side
            if (newPosition.x + tooltipText.preferredWidth / 2f > Screen.width)
                newPosition.x = Screen.width - tooltipText.preferredWidth / 2f;

            //If the new position's textbox is past the screen space on the left side
            else if (newPosition.x - tooltipText.preferredWidth / 2f < 0f)
                newPosition.x = 0f - tooltipText.preferredWidth / 2f;

            tooltipContainer.GetComponent<RectTransform>().position = newPosition;
            if (!tooltipContainer.activeSelf) tooltipContainer.SetActive(true);
        }

        public void DisableTooltip()
        {
            tooltipContainer.SetActive(false);
            tooltipText.text = "";
        }
        */

        /// <summary>
        /// Gradually increases or decreases an image component's color alpha. Note: targetAlpha must be between 0-1. 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="time"></param>
        /// <param name="newAlpha"></param>
        /// <returns></returns>
        public IEnumerator LerpImageAlpha(Image image, float time, float newAlpha, bool disableOnEnd = false, bool enableOnEnd = false)
        {
            Color originalColor = image.color;
            float elapsedTime = 0;
            float targetAlpha = Mathf.Clamp(newAlpha, 0, 1);

            //enable in case it is deactivated
            image.gameObject.SetActive(true);

            while (elapsedTime < time)
            {
                //we use unscaled time since if the game has 0 timeScale, we can still successfully call this
                elapsedTime += Time.unscaledDeltaTime;
                image.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(originalColor.a, targetAlpha, elapsedTime / time));
                yield return new WaitForEndOfFrame();
            }

            if (disableOnEnd) image.gameObject.SetActive(false);
            else if (enableOnEnd) image.gameObject.SetActive(true);
        }


        /// <summary>
        /// Gradually increases or decreases an textMeshPro component's color alpha. Note: targetAlpha must be between 0-1. 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="time"></param>
        /// <param name="newAlpha"></param>
        /// <returns></returns>
        public IEnumerator LerpTextAlpha(TextMeshProUGUI text, float time, float newAlpha, bool disableOnEnd = false, bool enableOnEnd = false)
        {
            Color originalColor = text.color;
            float elapsedTime = 0;
            float targetAlpha = Mathf.Clamp(newAlpha, 0, 1);

            //make sure to enable in case it is deactivated for some reason
            text.gameObject.SetActive(true);

            while (elapsedTime < time)
            {
                //we use unscaled time since if the game has 0 timeScale, we can still successfully call this
                elapsedTime += Time.unscaledDeltaTime;
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(originalColor.a, targetAlpha, elapsedTime / time));
                yield return new WaitForEndOfFrame();
            }

            if (disableOnEnd) text.gameObject.SetActive(false);
            else if (enableOnEnd) text.gameObject.SetActive(true);
        }

        public IEnumerator LerpRectSize(RectTransform rect, Vector2 startSize, Vector2 endSize, float time)
        {
            float elapsedTime = 0;

            while (elapsedTime < time)
            {
                elapsedTime += Time.unscaledDeltaTime;
                rect.sizeDelta= Vector2.Lerp(startSize, endSize, elapsedTime/time);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}


