using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class AimingSystem
{
    [Header("Aiming Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lineLength = 2.0f;
    
    [Header("Touch Aiming Settings")]
    [SerializeField] private float touchSensitivity = 1.0f;
    [SerializeField] private float minTouchDistance = 1f;
    [SerializeField] private bool useDirectionalAiming = true;
    [SerializeField] private bool enableTouchAiming = true;
    
    [Header("Curve Shot Settings")]
    [SerializeField] private float maxCurveIntensity = 3.0f;
    [SerializeField] private Key curveModifierKey = Key.LeftShift;
    [SerializeField] private Key leftCurveKey = Key.A;
    [SerializeField] private Key rightCurveKey = Key.D;
    [SerializeField] private float curveInputSensitivity = 1.0f;
    [SerializeField] private float maxCurveAngle = 30f;
    
    private Camera mainCam;
    private Transform ballTransform;
    
    private Vector2 dragStartScreenPos;
    private bool isDragging;
    
    public Vector3 AimDirection { get; private set; } = Vector3.right;
    public float CurrentAimLineLength { get; private set; } = 1.0f;     
    public float CurveIntensity { get; private set; }
    public bool IsCurveShotActive { get; private set; }

    public void Initialize(Camera camera, Transform ball)
    {
        mainCam = camera;
        ballTransform = ball;
        AimDirection = Vector3.right;
        
        //Debug.Log($"[AimingSystem] Initialized - Camera: {camera.name}, Ball Position: {ballTransform.position}, Camera Position: {camera.transform.position}");
        //Debug.Log($"Camera: {mainCam}, BallTransform: {ballTransform}");
    }

    public void UpdateAiming()
    {
        bool isInputActive = false;
        Vector2 screenPos = Vector2.zero;

        if (InputManager.Instance != null && InputManager.Instance.IsTouching())
        {
            isInputActive = true;
            screenPos = InputManager.Instance.GetTouchScreenPosition();
            UpdateAimingFromScreenPosition(screenPos); // Update on click/drag
        }

        UpdateCurveInput();
        UpdateAimLineLength();
    }
    
    private void UpdateAimingFromScreenPosition(Vector2 screenPos)
    {
        if (mainCam == null || ballTransform == null) return;
        
        // Cast a ray from screen position 
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        
        Plane gamePlane = new Plane(Vector3.forward, new Vector3(0, 0, ballTransform.position.z));
        
        //Debug.Log($"[AimingSystem] Ray: Origin={ray.origin}, Direction={ray.direction}, Ball Z={ballTransform.position.z}");
        
        if (gamePlane.Raycast(ray, out float distance))
        {
            // world position where the ray hits plane
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            
            // Calculate direction from ball to mouse position (in XY plane)
            Vector3 direction = mouseWorldPos - ballTransform.position;
            direction.z = 0f; 
            
            if (direction.sqrMagnitude > 0.01f)
            {
                AimDirection = direction.normalized;
            }
        }
    }
    
    public void OnTouchEnd()
    {
        Debug.Log("[AimingSystem] Touch input ended");
    }

    public void OnTouchRelease()
    {
        if (InputManager.Instance != null)
        {
            Vector2 releaseScreenPos = InputManager.Instance.GetTouchScreenPosition();
            //Debug.Log($"[AimingSystem] Touch released at: {releaseScreenPos}");
            UpdateAimingFromScreenPosition(releaseScreenPos);
        }
    }

    private void UpdateCurveInput()
    {
        if (Keyboard.current == null) 
        {
            IsCurveShotActive = false;
            CurveIntensity = 0f;
            return;
        }
        
        bool curveModifierPressed = Keyboard.current[curveModifierKey].isPressed;
        
        if (!curveModifierPressed)
        {
            IsCurveShotActive = false;
            CurveIntensity = 0f;
            return;
        }

        bool leftCurvePressed = Keyboard.current[leftCurveKey].isPressed;
        bool rightCurvePressed = Keyboard.current[rightCurveKey].isPressed;

        if (leftCurvePressed || rightCurvePressed)
        {
            IsCurveShotActive = true;
            
            if (leftCurvePressed && !rightCurvePressed)
            {
                CurveIntensity = -maxCurveIntensity;
            }
            else if (rightCurvePressed && !leftCurvePressed)
            {
                CurveIntensity = maxCurveIntensity;
            }
            else
            {
                CurveIntensity = 0f;
            }
        }
        else
        {
            IsCurveShotActive = true;
            CurveIntensity = 0f;
        }
    }
    
    private void UpdateAimLineLength()
    {
        if (AimDirection == Vector3.zero)
        {
            AimDirection = Vector3.right;
            CurrentAimLineLength = lineLength;
            return;
        }
        
        if (Physics.Raycast(ballTransform.position, AimDirection, out RaycastHit hit, lineLength, groundLayer))
        {
            CurrentAimLineLength = hit.distance;
            //Debug.DrawLine(ballTransform.position, hit.point, Color.red, 0.1f);
        }
        else
        {
            CurrentAimLineLength = lineLength;
            //Debug.DrawLine(ballTransform.position, ballTransform.position + (AimDirection * lineLength), Color.green, 0.1f);
        }
    }

    public Vector3 GetCurvedVelocity(Vector3 baseVelocity)
    {
        if (!IsCurveShotActive || Mathf.Abs(CurveIntensity) < 0.1f)
        {
            return baseVelocity;
        }

        // For 2D XY plane, perpendicular is calculated with Z-axis cross product
        Vector3 perpendicularDirection = Vector3.Cross(baseVelocity.normalized, Vector3.forward).normalized;
        float curveForce = CurveIntensity * 0.15f;
        Vector3 curveVector = perpendicularDirection * curveForce;
        
        return baseVelocity + curveVector;
    }

    public Vector3[] GetCurvePreviewPoints(int pointCount = 15)
    {
        if (!IsCurveShotActive || Mathf.Abs(CurveIntensity) < 0.1f)
            return null;

        Vector3[] points = new Vector3[pointCount];
        Vector3 startPos = ballTransform.position;
        Vector3 baseDirection = AimDirection;
        float maxDistance = CurrentAimLineLength;
        
        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector3 point = CalculateCurveArcPoint(startPos, baseDirection, maxDistance, t);
            points[i] = point;
        }
        
        return points;
    }

    private Vector3 CalculateCurveArcPoint(Vector3 startPos, Vector3 direction, float maxDistance, float t)
    {
        float distance = maxDistance * t;
        Vector3 basePoint = startPos + direction * distance;
        
        // For 2D XY plane, perpendicular uses Z-axis
        Vector3 perpendicularDirection = Vector3.Cross(direction, Vector3.forward).normalized;
        float curveAmount = Mathf.Sin(t * Mathf.PI) * (CurveIntensity / maxCurveIntensity) * (maxDistance * 0.2f);
        Vector3 curveOffset = perpendicularDirection * curveAmount;
        
        return basePoint + curveOffset;
    }
}