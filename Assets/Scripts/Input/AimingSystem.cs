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
    [SerializeField] private float minTouchDistance = 50f;
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
    private float gameZDepth;
    
    public Vector3 AimDirection { get; private set; } = Vector3.right;
    public float CurrentAimLineLength { get; private set; } = 1.0f;     
    public float CurveIntensity { get; private set; }
    public bool IsCurveShotActive { get; private set; }

    public void Initialize(Camera camera, Transform ball)
    {
        mainCam = camera;
        ballTransform = ball;
        AimDirection = Vector3.right;
        
        if (mainCam != null && ballTransform != null)
        {
            gameZDepth = mainCam.WorldToScreenPoint(ballTransform.position).z;
            Debug.Log($"[AimingSystem] Initialized - Camera: {camera.name}, IsOrthographic: {camera.orthographic}, Ball Position: {ballTransform.position}, Camera Position: {camera.transform.position}, Calculated Z Depth: {gameZDepth}");
        }
    }

    public void UpdateAiming()
    {
        bool isInputActive = false;
        Vector2 screenPos = Vector2.zero;
        
        // Debug input state every frame
        Debug.Log($"[AimingSystem] UpdateAiming called - InputManager exists: {InputManager.Instance != null}, Mouse exists: {Mouse.current != null}");
        
        // Try InputManager first
        if (InputManager.Instance != null)
        {
            bool touching = InputManager.Instance.IsTouching();
            Debug.Log($"[AimingSystem] InputManager.IsTouching(): {touching}");
            
            if (touching)
            {
                isInputActive = true;
                screenPos = InputManager.Instance.GetTouchScreenPosition();
                Debug.Log($"[AimingSystem] Using InputManager - ScreenPos: {screenPos}");
            }
        }
        else
        {
            Debug.LogWarning("[AimingSystem] InputManager.Instance is NULL!");
        }
        
        // Fallback to direct Mouse input
        if (!isInputActive && Mouse.current != null)
        {
            bool mousePressed = Mouse.current.leftButton.isPressed;
            Debug.Log($"[AimingSystem] Mouse.leftButton.isPressed: {mousePressed}");
            
            if (mousePressed)
            {
                isInputActive = true;
                screenPos = Mouse.current.position.ReadValue();
                Debug.Log($"[AimingSystem] Using Mouse fallback - ScreenPos: {screenPos}");
            }
        }
        
        Debug.Log($"[AimingSystem] Final isInputActive: {isInputActive}");
        
        // Update aiming if any input is active
        if (isInputActive)
        {
            UpdateAimingFromScreenPosition(screenPos);
        }

        // Update curve input
        UpdateCurveInput();
        
        // Update aim line length with raycast
        UpdateAimLineLength();
    }
    
    private void UpdateAimingFromScreenPosition(Vector2 screenPos)
    {
        if (mainCam == null || ballTransform == null)
        {
            Debug.LogError("[AimingSystem] MainCam or BallTransform is NULL!");
            return;
        }
        
        // Convert screen position to world position using the stored Z depth
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, gameZDepth);
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(screenPoint);
        
        // For 2D games, ensure Z is the same as the ball
        mouseWorldPos.z = ballTransform.position.z;
        
        // Calculate direction from ball to mouse world position
        Vector3 direction = (mouseWorldPos - ballTransform.position);
        direction.z = 0f;
        
        Debug.Log($"[AimingSystem] ScreenPos: {screenPos}, ScreenPoint (with Z): {screenPoint}, MouseWorld: {mouseWorldPos}, Ball: {ballTransform.position}, Direction: {direction}, Magnitude: {direction.magnitude}");
        
        if (direction.sqrMagnitude > 0.01f)
        {
            AimDirection = direction.normalized;
            Debug.Log($"[AimingSystem] *** AIM UPDATED *** Direction: {AimDirection}");
        }
        else
        {
            Debug.LogWarning($"[AimingSystem] Direction too small - Magnitude: {direction.magnitude}");
        }
    }
    
    public void OnTouchEnd()
    {
        Debug.Log("[AimingSystem] Touch input ended");
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
            Debug.DrawLine(ballTransform.position, hit.point, Color.red, 0.1f);
        }
        else
        {
            CurrentAimLineLength = lineLength;
            Debug.DrawLine(ballTransform.position, ballTransform.position + (AimDirection * lineLength), Color.green, 0.1f);
        }
    }

    public Vector3 GetCurvedVelocity(Vector3 baseVelocity)
    {
        if (!IsCurveShotActive || Mathf.Abs(CurveIntensity) < 0.1f)
        {
            return baseVelocity;
        }

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
        Vector3 perpendicularDirection = Vector3.Cross(direction, Vector3.forward).normalized;
        float curveAmount = Mathf.Sin(t * Mathf.PI) * (CurveIntensity / maxCurveIntensity) * (maxDistance * 0.2f);
        Vector3 curveOffset = perpendicularDirection * curveAmount;
        
        return basePoint + curveOffset;
    }
}