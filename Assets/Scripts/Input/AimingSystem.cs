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
    [SerializeField] private float minTouchDistance = 0.1f; // Minimum distance for touch drag
    
    [Header("Curve Shot Settings")]
    [SerializeField] private float maxCurveIntensity = 3.0f;
    [SerializeField] private Key curveModifierKey = Key.LeftShift;
    [SerializeField] private Key leftCurveKey = Key.A;
    [SerializeField] private Key rightCurveKey = Key.D;
    [SerializeField] private float curveInputSensitivity = 1.0f;
    [SerializeField] private float maxCurveAngle = 30f;
    
    private Camera mainCam;
    private Transform ballTransform;
    
    // Touch input tracking
    private Vector2 touchStartPosition;
    private Vector2 currentTouchPosition;
    private bool isDragging;
    
    public Vector3 AimDirection { get; private set; }
    public float CurrentAimLineLength { get; private set; } = 1.0f;     
    public float CurveIntensity { get; private set; }
    public bool IsCurveShotActive { get; private set; }

    public void Initialize(Camera camera, Transform ball)
    {
        mainCam = camera;
        ballTransform = ball;
    }

    public void UpdateAiming()
    {
        // Handle both touch and mouse input for flexibility
        if (InputManager.Instance != null && InputManager.Instance.IsTouching())
        {
            UpdateTouchAiming();
        }
        else
        {
            UpdateMouseAiming(); // Fallback for editor/desktop testing
        }

        // Update curve input (keep keyboard controls for curve shots)
        UpdateCurveInput();
        
        // Update aim line length with raycast
        UpdateAimLineLength();
    }
    
    private void UpdateTouchAiming()
    {
        Vector2 screenPosition = InputManager.Instance.GetTouchScreenPosition();
        
        if (!isDragging)
        {
            // Start tracking touch
            touchStartPosition = screenPosition;
            isDragging = true;
        }
        
        currentTouchPosition = screenPosition;
        
        // Calculate direction based on touch drag
        Vector2 dragVector = currentTouchPosition - touchStartPosition;
        
        if (dragVector.magnitude > minTouchDistance)
        {
            // Convert screen space drag to world space direction
            Vector3 ballScreenPos = mainCam.WorldToScreenPoint(ballTransform.position);
            Vector2 ballScreenPos2D = new Vector2(ballScreenPos.x, ballScreenPos.y);
            
            // Calculate aim direction from ball position to touch position
            Vector2 aimScreenDirection = currentTouchPosition - ballScreenPos2D;
            
            // Convert to world space direction
            Vector3 worldDirection = ConvertScreenToWorldDirection(aimScreenDirection);
            
            if (worldDirection != Vector3.zero)
            {
                AimDirection = worldDirection.normalized;
            }
        }
    }
    
    private void UpdateMouseAiming()
    {
        // Original mouse-based aiming (for desktop testing)
        if (Mouse.current != null)
        {
            Ray camRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane playerPlane = new Plane(Vector3.forward, ballTransform.position);
            
            if (playerPlane.Raycast(camRay, out float hitDistance))
            {
                Vector3 mouseWorldPos = camRay.GetPoint(hitDistance);
                Vector3 direction = (mouseWorldPos - ballTransform.position).normalized;
                direction = new Vector3(direction.x, direction.y, 0f).normalized;
                
                if (direction != Vector3.zero)
                {
                    AimDirection = direction;
                }
            }
        }
    }
    
    private Vector3 ConvertScreenToWorldDirection(Vector2 screenDirection)
    {
        // Create a more robust screen-to-world conversion for aiming
        Vector3 ballScreenPos = mainCam.WorldToScreenPoint(ballTransform.position);
        Vector3 targetScreenPos = new Vector3(
            ballScreenPos.x + screenDirection.x, 
            ballScreenPos.y + screenDirection.y, 
            ballScreenPos.z
        );
        
        Vector3 targetWorldPos = mainCam.ScreenToWorldPoint(targetScreenPos);
        Vector3 worldDirection = (targetWorldPos - ballTransform.position);
        
        // Ensure direction is on the XY plane (for 2D billiards)
        worldDirection.z = 0f;
        
        return worldDirection;
    }
    
    public void OnTouchEnd()
    {
        isDragging = false;
    }

    private void UpdateCurveInput()
    {
        // Keep keyboard controls for curve shots (can be adapted to UI buttons later)
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
            AimDirection = Vector3.right; // Default direction
            CurrentAimLineLength = lineLength;
            return;
        }
        
        // Raycast to detect obstacles
        if (Physics.Raycast(ballTransform.position, AimDirection, out RaycastHit hit, lineLength, groundLayer))
        {
            CurrentAimLineLength = hit.distance;
        }
        else
        {
            CurrentAimLineLength = lineLength;
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