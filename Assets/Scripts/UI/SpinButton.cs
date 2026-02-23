using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SpinButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button spinButton;
    [Tooltip("Root panel that contains BG, Ball and SpinIndicator")]
    [SerializeField] private GameObject spinUIPanel;
    [Tooltip("RectTransform of the Ball image (container for the indicator)")]
    [SerializeField] private RectTransform ballRect;
    [Tooltip("RectTransform of the small indicator that represents the spin point")]
    [SerializeField] private RectTransform spinIndicator;
    [Tooltip("Optional: parent canvas used for proper ScreenPoint -> RectTransform conversion")]
    [SerializeField] private Canvas rootCanvas;

    // Normalized spin for X and Y relative to the ball center
    public event Action<Vector2> OnSpinChanged;

    // Check if UI is open
    public bool IsOpen => isOpen;

    private bool isOpen;
    private bool buttonClickedThisFrame; // Track if button was clicked this frame
    private Vector2 persistentSpin = Vector2.zero; // Store spin 

    private void Awake()
    {
        if (spinButton == null)
            spinButton = GetComponent<Button>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (spinButton != null)
        {
            spinButton.onClick.RemoveListener(ToggleSpinUI);
            spinButton.onClick.AddListener(ToggleSpinUI);
        }

        if (spinUIPanel != null)
            spinUIPanel.SetActive(false);

        CenterIndicator();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (buttonClickedThisFrame)
        {
            buttonClickedThisFrame = false;
            return;
        }

        // New Input System: uses touch when available, fallback to mouse
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var primary = touchscreen.primaryTouch;
            if (primary != null && primary.press.wasPressedThisFrame) 
            {
                Vector2 pos = primary.position.ReadValue();
                if (IsPositionWithinBallArea(pos))
                {
                    TryMoveIndicator(pos);
                }
                return;
            }
            else if (primary != null && primary.press.isPressed)
            {
                Vector2 pos = primary.position.ReadValue();
                if (IsPositionWithinBallArea(pos))
                {
                    TryMoveIndicator(pos);
                }
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) // Only check for new presses
        {
            Vector2 pos = mouse.position.ReadValue();
            if (IsPositionWithinBallArea(pos))
            {
                TryMoveIndicator(pos);
            }
        }
        else if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 pos = mouse.position.ReadValue();
            if (IsPositionWithinBallArea(pos))
            {
                TryMoveIndicator(pos);
            }
        }
    }

    private bool IsPositionWithinBallArea(Vector2 screenPosition)
    {
        if (ballRect == null)
            return false;

        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        bool isWithinBallRect = RectTransformUtility.RectangleContainsScreenPoint(ballRect, screenPosition, cam);
        
        if (!isWithinBallRect)
            return false;

        if (EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            
            foreach (var result in raycastResults)
            {
                if (result.gameObject != ballRect.gameObject && 
                    result.gameObject.GetComponent<Button>() != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ToggleSpinUI()
    {
        if (spinUIPanel == null)
            return;

        buttonClickedThisFrame = true;

        isOpen = !isOpen;
        spinUIPanel.SetActive(isOpen);

        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {

            RestoreSpinIndicator();
        }
        else
        {

            persistentSpin = GetSpinNormalized();
        }
    }

    private void TryMoveIndicator(Vector2 screenPosition)
    {
        if (ballRect == null || spinIndicator == null)
            return;

        // choose camera depending on canvas render mode
        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ballRect, screenPosition, cam, out Vector2 localPoint))
            return;

        // localPoint is relative to the ballRect pivot / center
        Vector2 anchoredPos = localPoint;

        // compute usable radius inside which the indicator can move (subtract half indicator size so it stays fully inside)
        float radius = Mathf.Min(ballRect.rect.width, ballRect.rect.height) * 0.5f - Mathf.Min(spinIndicator.rect.width, spinIndicator.rect.height) * 0.5f;
        radius = Mathf.Max(0f, radius);

        // clamp to circular bounds
        if (anchoredPos.magnitude > radius)
            anchoredPos = anchoredPos.normalized * radius;

        spinIndicator.anchoredPosition = anchoredPos;

        Vector2 normalized = radius > 0f ? new Vector2(anchoredPos.x / radius, anchoredPos.y / radius) : Vector2.zero;
        normalized = Vector2.ClampMagnitude(normalized, 1f);

        // Update persistent spin
        persistentSpin = normalized;
        
        OnSpinChanged?.Invoke(normalized);
    }

    // Returns currently selected spin normalized 
    public Vector2 GetSpinNormalized()
    {
        // Return persistent spin regardless of whether panel is open or closed
        return persistentSpin;
    }

    // Public method to reset spin (called by BilliardController when ball stops)
    public void ResetSpin()
    {
        persistentSpin = Vector2.zero;
        
        if (spinIndicator != null)
            spinIndicator.anchoredPosition = Vector2.zero;

        OnSpinChanged?.Invoke(Vector2.zero);
        
        Debug.Log("[SpinButton] Spin reset after ball stopped");
    }

    private void CenterIndicator()
    {
        if (spinIndicator != null)
            spinIndicator.anchoredPosition = Vector2.zero;

        persistentSpin = Vector2.zero;
        OnSpinChanged?.Invoke(Vector2.zero);
    }

    private void RestoreSpinIndicator()
    {
        if (spinIndicator == null || ballRect == null)
            return;

        float radius = Mathf.Min(ballRect.rect.width, ballRect.rect.height) * 0.5f - Mathf.Min(spinIndicator.rect.width, spinIndicator.rect.height) * 0.5f;
        radius = Mathf.Max(0f, radius);

        Vector2 indicatorPos = persistentSpin * radius;
        spinIndicator.anchoredPosition = indicatorPos;
        
        Debug.Log($"[SpinButton] Restored spin indicator to: {persistentSpin}, Position: {indicatorPos}");
    }

    private void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(ToggleSpinUI);
        Time.timeScale = 1f;
    }
}
