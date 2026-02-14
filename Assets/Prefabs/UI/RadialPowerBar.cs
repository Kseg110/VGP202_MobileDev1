using UnityEngine;
using UnityEngine.UI;

public class RadialPowerBar : MonoBehaviour
{
    [Header("Radial Power Bar Settings")]
    [SerializeField] private Image powerFill; // Assign the radial fill Image component
    [SerializeField] private Image backgroundRing; // Optional: background ring
    [SerializeField] private Color minPowerColor = Color.green;
    [SerializeField] private Color maxPowerColor = Color.red;
    [SerializeField] private float smoothSpeed = 5f; // Smooth transition speed
    
    private float targetFillAmount = 0f;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Get or add CanvasGroup for fade in/out
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Start hidden
        SetActive(false);
        
        // Ensure powerFill is set to radial fill
        if (powerFill != null)
        {
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Radial360;
            powerFill.fillOrigin = 0; // Top
            powerFill.fillAmount = 0f;
        }
    }

    void Update()
    {
        // Smooth the fill animation
        if (powerFill != null && Mathf.Abs(powerFill.fillAmount - targetFillAmount) > 0.01f)
        {
            powerFill.fillAmount = Mathf.Lerp(powerFill.fillAmount, targetFillAmount, smoothSpeed * Time.deltaTime);
            
            // Update color based on fill amount
            powerFill.color = Color.Lerp(minPowerColor, maxPowerColor, powerFill.fillAmount);
        }
    }
    
    /// <summary>
    /// Update the power bar with a value between 0 and 1
    /// </summary>
    /// <param name="powerPercentage">Power value from 0 to 1</param>
    public void UpdatePower(float powerPercentage)
    {
        targetFillAmount = Mathf.Clamp01(powerPercentage);
    }
    
    /// <summary>
    /// Show or hide the power bar
    /// </summary>
    /// <param name="active">True to show, false to hide</param>
    public void SetActive(bool active)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
        }
        else
        {
            gameObject.SetActive(active);
        }
        
        // Reset fill when hiding
        if (!active)
        {
            targetFillAmount = 0f;
            if (powerFill != null)
                powerFill.fillAmount = 0f;
        }
    }
}
