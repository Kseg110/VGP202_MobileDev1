using UnityEngine;

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

    public Vector2 GetTouchScreenPosition()
    {
        return input.Gameplay.PrimaryPosition.ReadValue<Vector2>();
    }

    public Vector3 GetTouchWorldPosition(Camera cameraToUse = null)
    {
        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }

        Vector2 screenPos = GetTouchScreenPosition();
        Vector3 worldPos = cameraToUse.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cameraToUse.nearClipPlane));
        return worldPos;
    }
}
