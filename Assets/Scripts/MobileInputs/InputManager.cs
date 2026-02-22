using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    //singleton instance
    public static InputManager Instance { get; private set; }

    //events
    public event System.Action OnTouchBegin;
    public event System.Action OnTouchEnd;
    public event System.Action<Vector3> OnPhoneTilt;

    private PlayerControls input;

    [Header("Input Area (optional)")]
    [Tooltip("If set, only touches/points inside this 2D collider will be treated as aiming input.")]
    [SerializeField] private Collider2D aimInputArea;

    private Camera cachedMainCamera;

    private void Awake()
    {
        //singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        input = new PlayerControls();

        // cache main camera reference (may be null in some editor scenarios)
        cachedMainCamera = Camera.main;

        // attempt to auto-find the AimInputArea by name if not assigned in inspector
        if (aimInputArea == null)
        {
            var go = GameObject.Find("AimInputArea");
            if (go != null)
            {
                aimInputArea = go.GetComponent<Collider2D>();
            }
        }
    }

    private void OnEnable()
    {
        input.Enable();

        //touch input
        input.Gameplay.Touch.started += ctx => OnTouchBegin?.Invoke();
        input.Gameplay.Touch.canceled += ctx => OnTouchEnd?.Invoke();
        //tilt input
        input.Gameplay.Accelerometer.performed += ctx => OnPhoneTilt?.Invoke(ctx.ReadValue<Vector3>());
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }

    // Public setter so other scripts can assign the area at runtime if needed
    public void SetAimInputArea(Collider2D collider)
    {
        aimInputArea = collider;
    }

    // Returns true if the provided screen position is inside the configured AimInputArea.
    // If no area is configured, this returns true (no restriction).
    private bool IsScreenPositionWithinAimArea(Vector2 screenPos)
    {
        if (aimInputArea == null)
            return true;

        Camera cam = cachedMainCamera != null ? cachedMainCamera : Camera.main;
        if (cam == null)
            return true; // can't evaluate without a camera; default to allowing input

        Vector3 worldPoint3 = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        Vector2 worldPoint2 = new Vector2(worldPoint3.x, worldPoint3.y);

        // Use Collider2D.OverlapPoint if available on the assigned collider
        // Fallback to Physics2D.OverlapPoint in case
        try
        {
            return aimInputArea.OverlapPoint(worldPoint2);
        }
        catch
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPoint2);
            return hit == aimInputArea;
        }
    }

    public Vector2 GetTouchScreenPosition()
    {
        // 1) Touchscreen primary touch (mobile)
        if (Touchscreen.current != null)
        {
            var primary = Touchscreen.current.primaryTouch;
            if (primary != null && primary.press.isPressed)
            {
                Vector2 touchPos = primary.position.ReadValue();
                if (IsScreenPositionWithinAimArea(touchPos))
                {
                    //Debug.Log($"[InputManager] Touch position (device): {touchPos}");
                    return touchPos;
                }
            }
        }

        // 2) InputAction pointer (project-specific action) if it has an active control and returns a non-zero value
        if (input != null)
        {
            var pointerAction = input.Gameplay.PointerPosition;
            if (pointerAction != null && pointerAction.activeControl != null)
            {
                Vector2 actionPos = pointerAction.ReadValue<Vector2>();
                if (actionPos != Vector2.zero && IsScreenPositionWithinAimArea(actionPos))
                {
                    //Debug.Log($"[InputManager] Pointer position (action): {actionPos}");
                    return actionPos;
                }
            }
        }

        // 3) Generic pointer device (covers pen, touch pointer, and mouse)
        if (Pointer.current != null)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();
            if (pointerPos != Vector2.zero && IsScreenPositionWithinAimArea(pointerPos))
            {
                //Debug.Log($"[InputManager] Pointer position (device): {pointerPos}");
                return pointerPos;
            }
        }

        // 4) Mouse device (editor)
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos != Vector2.zero && IsScreenPositionWithinAimArea(mousePos))
            {
                //Debug.Log($"[InputManager] Mouse position (device): {mousePos}");
                return mousePos;
            }
        }
        return Vector2.zero;
    }

    // Convert screen position to world position by raycasting to a plane at planeZ.
    // Default planeZ = 0 (XY plane). This avoids using camera.nearClipPlane which can produce unexpected results.
    public Vector3 GetTouchWorldPosition(Camera cameraToUse = null, float planeZ = 0f)
    {
        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }


        Vector2 screenPos = GetTouchScreenPosition();

        // Create a ray and intersect with plane at Z = planeZ
        Ray ray = cameraToUse.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);
            return worldPos;
        }

        // Fallback to ScreenToWorldPoint (should rarely happen)
        Vector3 fallback = cameraToUse.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cameraToUse.nearClipPlane));
        return fallback;
    }

    public Vector2 GetTouchDirection(Vector2 startPos, Vector2 currentPos)
    {
        return (currentPos - startPos).normalized;
    }

    public float GetTouchDistance(Vector2 startPos, Vector2 currentPos)
    {
        return Vector2.Distance(startPos, currentPos);
    }

    // For billiards aiming
    public Vector3 GetAimDirection(Camera camera)
    {
        Vector2 screenPos = GetTouchScreenPosition();
        Vector3 worldPos = GetTouchWorldPosition(camera);
        return worldPos;
    }

    // Utility methods for billiards gameplay
    public bool IsTouching()
    {
        // Mouse (editor) check
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (IsScreenPositionWithinAimArea(mousePos))
                return true;
        }

        // Touchscreen device check (safe)
        try
        {
            if (Touchscreen.current != null)
            {
                // primaryTouch is usually present; check its press state
                if (Touchscreen.current.primaryTouch != null && Touchscreen.current.primaryTouch.press.isPressed)
                {
                    Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                    if (IsScreenPositionWithinAimArea(touchPos))
                        return true;
                }
            }
        }
        catch
        {
            // ignore device query errors
        }

        // As last fallback check the Touch action value
        if (input != null)
        {
            try
            {
                if (input.Gameplay.Touch.ReadValue<float>() > 0f)
                {
                    Vector2 actionPos = Vector2.zero;
                    try
                    {
                        actionPos = input.Gameplay.PointerPosition.ReadValue<Vector2>();
                    }
                    catch { }

                    // If PointerPosition couldn't provide a value, try to read current pointer or touchscreen position.
                    if (actionPos == Vector2.zero)
                    {
                        if (Pointer.current != null)
                        {
                            actionPos = Pointer.current.position.ReadValue();
                        }
                        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
                        {
                            actionPos = Touchscreen.current.primaryTouch.position.ReadValue();
                        }
                    }

                    // If we still can't determine a valid position, treat this as NOT touching for aiming purposes.
                    // This prevents UI presses outside the aim area from being treated as valid aiming input.
                    if (actionPos == Vector2.zero)
                        return false;

                    if (IsScreenPositionWithinAimArea(actionPos))
                        return true;
                }
            }
            catch
            {
                // ignore
            }
        }

        return false;
    }

    public Vector2 GetTouchDelta(Vector2 lastPosition)
    {
        return GetTouchScreenPosition() - lastPosition;
    }
}
