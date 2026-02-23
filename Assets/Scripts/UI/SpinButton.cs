using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    // Emits normalized spin in range [-1..1] for X and Y relative to the ball center
    public event Action<Vector2> OnSpinChanged;

    // Expose state so other systems can query whether the UI is open
    public bool IsOpen => isOpen;

    private bool isOpen;

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

        // Use the new Input System: prefer touch when available, fallback to mouse
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            // primaryTouch will reference the active touch on screen devices
            var primary = touchscreen.primaryTouch;
            if (primary != null && primary.press.isPressed)
            {
                Vector2 pos = primary.position.ReadValue();
                TryMoveIndicator(pos);
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 pos = mouse.position.ReadValue();
            TryMoveIndicator(pos);
        }
    }

    private void ToggleSpinUI()
    {
        if (spinUIPanel == null)
            return;

        isOpen = !isOpen;
        spinUIPanel.SetActive(isOpen);

        // Pause the game while SpinUI is open to prevent undesired inputs
        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            // ensure indicator is visible and centered when opened
            CenterIndicator();
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

        // localPoint is relative to the ballRect pivot (for default pivot=0.5,0.5 (center) localPoint 0,0 is center).
        Vector2 anchoredPos = localPoint;

        // compute usable radius inside which the indicator can move (subtract half indicator size so it stays fully inside)
        float radius = Mathf.Min(ballRect.rect.width, ballRect.rect.height) * 0.5f - Mathf.Min(spinIndicator.rect.width, spinIndicator.rect.height) * 0.5f;
        radius = Mathf.Max(0f, radius);

        // clamp to circular bounds
        if (anchoredPos.magnitude > radius)
            anchoredPos = anchoredPos.normalized * radius;

        spinIndicator.anchoredPosition = anchoredPos;

        // normalized spin in [-1,1]
        Vector2 normalized = radius > 0f ? new Vector2(anchoredPos.x / radius, anchoredPos.y / radius) : Vector2.zero;
        normalized = Vector2.ClampMagnitude(normalized, 1f);

        OnSpinChanged?.Invoke(normalized);
    }

    // Returns currently selected spin normalized to [-1..1] per axis (0 = center)
    public Vector2 GetSpinNormalized()
    {
        if (ballRect == null || spinIndicator == null)
            return Vector2.zero;

        float radius = Mathf.Min(ballRect.rect.width, ballRect.rect.height) * 0.5f - Mathf.Min(spinIndicator.rect.width, spinIndicator.rect.height) * 0.5f;
        if (radius <= 0f) return Vector2.zero;
        Vector2 anchored = spinIndicator.anchoredPosition;
        return Vector2.ClampMagnitude(new Vector2(anchored.x / radius, anchored.y / radius), 1f);
    }

    private void CenterIndicator()
    {
        if (spinIndicator != null)
            spinIndicator.anchoredPosition = Vector2.zero;

        OnSpinChanged?.Invoke(Vector2.zero);
    }

    private void OnDestroy()
    {
        if (spinButton != null)
            spinButton.onClick.RemoveListener(ToggleSpinUI);

        // Ensure timeScale restored if object destroyed while panel was open
        Time.timeScale = 1f;
    }
}
