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

    public Vector2 GetTouchScreenPosition()
    {
        // 1) Touchscreen primary touch (mobile)
        if (Touchscreen.current != null)
        {
            var primary = Touchscreen.current.primaryTouch;
            if (primary != null && primary.press.isPressed)
            {
                Vector2 touchPos = primary.position.ReadValue();
                Debug.Log($"[InputManager] Touch position (device): {touchPos}");
                return touchPos;
            }
        }

        // 2) InputAction pointer (project-specific action) if it has an active control and returns a non-zero value
        if (input != null)
        {
            var pointerAction = input.Gameplay.PointerPosition;
            if (pointerAction != null && pointerAction.activeControl != null)
            {
                Vector2 actionPos = pointerAction.ReadValue<Vector2>();
                if (actionPos != Vector2.zero)
                {
                    Debug.Log($"[InputManager] Pointer position (action): {actionPos}");
                    return actionPos;
                }
            }
        }

        // 3) Generic pointer device (covers pen, touch pointer, and mouse)
        if (Pointer.current != null)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();
            if (pointerPos != Vector2.zero)
            {
                Debug.Log($"[InputManager] Pointer position (device): {pointerPos}");
                return pointerPos;
            }
        }

        // 4) Mouse device (editor)
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos != Vector2.zero)
            {
                Debug.Log($"[InputManager] Mouse position (device): {mousePos}");
                return mousePos;
            }
        }

        // 5) Last-resort legacy API (will return 0,0 if nothing else)
        Vector2 legacyMousePos = (Vector2)UnityEngine.Input.mousePosition;
        Debug.Log($"[InputManager] Legacy mouse position: {legacyMousePos}");
        return legacyMousePos;
    }

    // Convert screen position to world position by raycasting to a plane at planeZ.
    // Default planeZ = 0 (XY plane). This avoids using camera.nearClipPlane which can produce unexpected results.
    public Vector3 GetTouchWorldPosition(Camera cameraToUse = null, float planeZ = 0f)
    {
        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }

        if (cameraToUse == null)
        {
            Debug.LogWarning("[InputManager] No camera available for Screen->World conversion.");
            return Vector3.zero;
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
            return true;
        }

        // Touchscreen device check (safe)
        try
        {
            if (Touchscreen.current != null)
            {
                // primaryTouch is usually present; check its press state
                if (Touchscreen.current.primaryTouch != null && Touchscreen.current.primaryTouch.press.isPressed)
                    return true;
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
                    return true;
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
