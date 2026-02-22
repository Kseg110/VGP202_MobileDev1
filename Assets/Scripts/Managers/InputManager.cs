using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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

    [Header("Behavior")]
    [Tooltip("If true, the InputManager will try to find an AimInputArea in each loaded scene.")]
    [SerializeField] private bool autoFindAimInputArea = true;

    [Header("Debug")]
    [SerializeField] private bool debugInputManager = false;

    private Camera cachedMainCamera;

    // Deferred processing flags and positions (avoid calling EventSystem API inside InputAction callbacks)
    private bool pendingTouchStart;
    private Vector2 pendingTouchStartPos;
    private bool pendingTouchEnd;
    private Vector2 pendingTouchEndPos;

    private void Awake()
    {
        //singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        input = new PlayerControls();

        // initial cache (will be refreshed on scene load)
        cachedMainCamera = Camera.main;

        // attempt to auto-find the AimInputArea by name if not assigned in inspector
        if (aimInputArea == null)
        {
            TryFindAimInputAreaInScene();
        }
    }

    private void OnEnable()
    {
        input.Enable();

        // subscribe to scene loaded so we can refresh camera / area refs after scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // touch input - route through lightweight callbacks that defer UI checks to Update
        input.Gameplay.Touch.started += OnGameplayTouchStarted;
        input.Gameplay.Touch.canceled += OnGameplayTouchCanceled;

        // tilt input
        input.Gameplay.Accelerometer.performed += ctx => OnPhoneTilt?.Invoke(ctx.ReadValue<Vector3>());
    }

    private void OnDisable()
    {
        input.Disable();

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (input != null)
        {
            input.Gameplay.Touch.started -= OnGameplayTouchStarted;
            input.Gameplay.Touch.canceled -= OnGameplayTouchCanceled;
            input.Gameplay.Accelerometer.performed -= ctx => OnPhoneTilt?.Invoke(ctx.ReadValue<Vector3>());
        }
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh camera reference (Camera.main may change after load)
        cachedMainCamera = Camera.main;

        if (debugInputManager)
            Debug.Log($"[InputManager] SceneLoaded: {scene.name}. MainCamera={(cachedMainCamera? cachedMainCamera.name : "null")}");

        // If owner-assigned collider is invalid or belongs to a different scene, try to re-resolve it.
        bool needResolve = false;
        if (aimInputArea == null)
            needResolve = true;
        else
        {
            // Unity's overloaded null check handles destroyed UnityEngine.Object.
            if (aimInputArea.gameObject == null)
                needResolve = true;
            else
            {
                // If the collider belongs to a different scene than the newly loaded scene, re-find.
                if (autoFindAimInputArea && aimInputArea.gameObject.scene != scene)
                    needResolve = true;
            }
        }

        if (needResolve && autoFindAimInputArea)
        {
            TryFindAimInputAreaInScene(scene);
        }
    }

    // Public setter so other scripts can assign the area at runtime if needed
    public void SetAimInputArea(Collider2D collider)
    {
        aimInputArea = collider;
        if (debugInputManager)
            Debug.Log($"[InputManager] AimInputArea explicitly set: {(collider ? collider.name : "null")}");
    }

    private void TryFindAimInputAreaInScene()
    {
        TryFindAimInputAreaInScene(SceneManager.GetActiveScene());
    }

    private void TryFindAimInputAreaInScene(Scene scene)
    {
        if (!scene.IsValid()) return;
        var objs = scene.GetRootGameObjects();
        foreach (var root in objs)
        {
            var found = root.transform.Find("AimInputArea");
            if (found != null)
            {
                var c = found.GetComponent<Collider2D>();
                if (c != null)
                {
                    aimInputArea = c;
                    if (debugInputManager) Debug.Log($"[InputManager] Auto-found AimInputArea on scene load: {found.name}");
                    return;
                }
            }
        }

        // fallback general find by name (works across nested objects)
        var go = GameObject.Find("AimInputArea");
        if (go != null)
        {
            var c = go.GetComponent<Collider2D>();
            if (c != null)
            {
                aimInputArea = c;
                if (debugInputManager) Debug.Log($"[InputManager] Auto-found AimInputArea (fallback): {go.name}");
            }
        }
    }

    // Try to resolve a usable screen position from available devices/actions.
    // Returns true when a candidate position was found (not necessarily inside aim area).
    private bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;

        // 1) Touchscreen primary touch (mobile)
        try
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.isPressed)
                {
                    screenPos = primary.position.ReadValue();
                    return true;
                }
            }
        }
        catch { /* ignore device query errors */ }

        // 2) InputAction pointer (project-specific action)
        if (input != null)
        {
            try
            {
                var pointerAction = input.Gameplay.PointerPosition;
                if (pointerAction != null && pointerAction.activeControl != null)
                {
                    Vector2 actionPos = pointerAction.ReadValue<Vector2>();
                    if (actionPos != Vector2.zero)
                    {
                        screenPos = actionPos;
                        return true;
                    }
                }
            }
            catch { /* ignore */ }
        }

        // 3) Generic pointer device
        try
        {
            if (Pointer.current != null)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                if (pointerPos != Vector2.zero)
                {
                    screenPos = pointerPos;
                    return true;
                }
            }
        }
        catch { /* ignore */ }

        // 4) Mouse device (editor)
        try
        {
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (mousePos != Vector2.zero)
                {
                    screenPos = mousePos;
                    return true;
                }
            }
        }
        catch { /* ignore */ }

        return false;
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
        Vector2 worldPoint2 = new (worldPoint3.x, worldPoint3.y);

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

    // Lightweight callbacks: only capture candidate positions and defer checks to Update().
    private void OnGameplayTouchStarted(InputAction.CallbackContext ctx)
    {
        if (TryGetPointerScreenPosition(out Vector2 pos))
        {
            pendingTouchStart = true;
            pendingTouchStartPos = pos;
        }
    }

    private void OnGameplayTouchCanceled(InputAction.CallbackContext ctx)
    {
        if (TryGetPointerScreenPosition(out Vector2 pos))
        {
            pendingTouchEnd = true;
            pendingTouchEndPos = pos;
        }
        else
        {
            // If no position could be resolved, still mark an end at zero to process (will be ignored if over UI)
            pendingTouchEnd = true;
            pendingTouchEndPos = Vector2.zero;
        }
    }

    // Update processes pending touch start/end to allow safe EventSystem/GraphicRaycaster usage
    private void Update()
    {
        // Process pending touch start
        if (pendingTouchStart)
        {
            pendingTouchStart = false;
            Vector2 pos = pendingTouchStartPos;

            bool overUI = IsPointerOverUIAtPosition(pos);
            bool inside = IsScreenPositionWithinAimArea(pos);
            if (debugInputManager) Debug.Log($"[InputManager] PendingStart pos={pos} overUI={overUI} insideArea={inside}");
            if (!overUI && inside)
            {
                OnTouchBegin?.Invoke();
            }
        }

        // Process pending touch end
        if (pendingTouchEnd)
        {
            pendingTouchEnd = false;
            Vector2 pos = pendingTouchEndPos;

            bool overUI = IsPointerOverUIAtPosition(pos);
            bool inside = IsScreenPositionWithinAimArea(pos);
            if (debugInputManager) Debug.Log($"[InputManager] PendingEnd pos={pos} overUI={overUI} insideArea={inside}");
            if (!overUI && inside)
            {
                OnTouchEnd?.Invoke();
            }
            else
            {
                // Also invoke OnTouchEnd for cases where we couldn't get a position but the pointer isn't over UI.
                if (pos == Vector2.zero)
                {
                    bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                    if (!pointerOverUI)
                    {
                        OnTouchEnd?.Invoke();
                    }
                }
            }
        }
    }

    // Safely test whether a screen position is over any UI element using EventSystem raycast.
    // This is safe when called from Update (not inside InputAction callbacks).
    private bool IsPointerOverUIAtPosition(Vector2 screenPos)
    {
        if (EventSystem.current == null)
            return false;

        // If position is zero and there is a mouse device, use current mouse position
        if (screenPos == Vector2.zero && Mouse.current != null)
        {
            try
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (mousePos != Vector2.zero)
                    screenPos = mousePos;
            }
            catch { }
        }

        if (screenPos == Vector2.zero)
            return false;

        PointerEventData pdata = new (EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List <RaycastResult>();
        EventSystem.current.RaycastAll(pdata, results);
        bool isOver = results != null && results.Count > 0;
        if (debugInputManager && isOver) Debug.Log($"[InputManager] UI Raycast hit {results.Count} at {screenPos}");
        return isOver;
    }

    public Vector2 GetTouchScreenPosition()
    {
        // Attempt to get a pointer position and only return it if it's inside the aim area.
        if (TryGetPointerScreenPosition(out Vector2 pos))
        {
            if (IsScreenPositionWithinAimArea(pos))
            {
                return pos;
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
            cameraToUse = cachedMainCamera != null ? cachedMainCamera : Camera.main;
        }

        Vector2 screenPos = GetTouchScreenPosition();

        if (screenPos == Vector2.zero)
            return Vector3.zero;

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
        // Do not use EventSystem.IsPointerOverGameObject inside callbacks; this method is safe to call from gameplay Update.
        if (EventSystem.current != null)
        {
            // If mouse present and over UI, ignore
            if (Mouse.current != null)
            {
                try
                {
                    Vector2 mpos = Mouse.current.position.ReadValue();
                    if (IsPointerOverUIAtPosition(mpos))
                        return false;
                }
                catch { }
            }
        }

        // Use TryGetPointerScreenPosition and require it to be inside the aim area
        if (TryGetPointerScreenPosition(out Vector2 pos))
        {
            return IsScreenPositionWithinAimArea(pos);
        }

        // As last fallback check the Touch action value but do not assume a valid position
        if (input != null)
        {
            try
            {
                if (input.Gameplay.Touch.ReadValue<float>() > 0f)
                {
                    if (TryGetPointerScreenPosition(out Vector2 fallbackPos))
                    {
                        return IsScreenPositionWithinAimArea(fallbackPos);
                    }
                    return false;
                }
            }
            catch { /* ignore */ }
        }

        return false;
    }

    public Vector2 GetTouchDelta(Vector2 lastPosition)
    {
        return GetTouchScreenPosition() - lastPosition;
    }
}
