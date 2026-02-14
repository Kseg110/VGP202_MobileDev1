using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class AimingSystem
{
    [Header("Aiming Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lineLength = 2.0f;
    
    [Header("Curve Shot Settings")]
    [SerializeField] private float maxCurveIntensity = 3.0f;
    [SerializeField] private Key curveModifierKey = Key.LeftShift;
    [SerializeField] private Key leftCurveKey = Key.A;
    [SerializeField] private Key rightCurveKey = Key.D;
    [SerializeField] private float curveInputSensitivity = 1.0f;
    [SerializeField] private float maxCurveAngle = 30f; // Changed to 30 degrees as requested
    
    private Camera mainCam;
    private Transform ballTransform;
    
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
        Ray camRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane playerPlane = new Plane(Vector3.forward, ballTransform.position);
        
        if (playerPlane.Raycast(camRay, out float hitDistance))
        {
            Vector3 mouseWorldPos = camRay.GetPoint(hitDistance);
            Vector3 direction = (mouseWorldPos - ballTransform.position).normalized;
            
            // Ensure direction is normalized and on the correct plane
            direction = new Vector3(direction.x, direction.y, 0f).normalized;

            // Update curve input based on key combinations
            UpdateCurveInput();

            // Raycast from ball position in the aim direction to detect obstacles
            if (Physics.Raycast(ballTransform.position, direction, out RaycastHit hit, lineLength, groundLayer))
            {
                AimDirection = direction;
                CurrentAimLineLength = hit.distance;
                Debug.DrawLine(ballTransform.position, hit.point, Color.red, 0.1f);
            }
            else
            {
                AimDirection = direction;
                CurrentAimLineLength = lineLength;
                Debug.DrawLine(ballTransform.position, ballTransform.position + (direction * lineLength), Color.green, 0.1f);
            }
        }
        else
        {
            Debug.LogWarning("Plane raycast did not hit");
            AimDirection = Vector3.right;
            CurrentAimLineLength = lineLength;
        }
    }

    private void UpdateCurveInput()
    {
        // Check if curve modifier key is held
        bool curveModifierPressed = Keyboard.current[curveModifierKey].isPressed;
        
        if (!curveModifierPressed)
        {
            IsCurveShotActive = false;
            CurveIntensity = 0f;
            return;
        }

        // Check for left and right curve keys
        bool leftCurvePressed = Keyboard.current[leftCurveKey].isPressed;
        bool rightCurvePressed = Keyboard.current[rightCurveKey].isPressed;

        if (leftCurvePressed || rightCurvePressed)
        {
            IsCurveShotActive = true;
            
            // Calculate curve intensity based on key pressed
            if (leftCurvePressed && !rightCurvePressed)
            {
                // Left curve (negative intensity)
                CurveIntensity = -maxCurveIntensity;
                Debug.Log("Left curve active: " + CurveIntensity);
            }
            else if (rightCurvePressed && !leftCurvePressed)
            {
                // Right curve (positive intensity)
                CurveIntensity = maxCurveIntensity;
                Debug.Log("Right curve active: " + CurveIntensity);
            }
            else
            {
                // Both keys pressed - no curve
                CurveIntensity = 0f;
            }
        }
        else
        {
            IsCurveShotActive = true; // Modifier is held but no direction keys
            CurveIntensity = 0f;
        }
    }

    public Vector3 GetCurvedVelocity(Vector3 baseVelocity)
    {
        if (!IsCurveShotActive || Mathf.Abs(CurveIntensity) < 0.1f)
        {
            Debug.Log("No curve applied - returning base velocity: " + baseVelocity);
            return baseVelocity;
        }

        // Calculate the perpendicular direction for curve (Magnus effect) in XY plane
        Vector3 perpendicularDirection = Vector3.Cross(baseVelocity.normalized, Vector3.forward).normalized;
        
        // Apply curve force as initial sideways velocity (reduced for more realistic curve)
        float curveForce = CurveIntensity * 0.15f; // Reduced multiplier for gentler curve
        Vector3 curveVector = perpendicularDirection * curveForce;
        
        // Combine base velocity with curve velocity
        Vector3 curvedVelocity = baseVelocity + curveVector;
        
        Debug.Log($"Curved velocity applied: {curvedVelocity} (Original: {baseVelocity}, Curve: {curveVector})");
        return curvedVelocity;
    }

    public Vector3[] GetCurvePreviewPoints(int pointCount = 15)
    {
        if (!IsCurveShotActive || Mathf.Abs(CurveIntensity) < 0.1f)
            return null;

        Vector3[] points = new Vector3[pointCount];
        Vector3 startPos = ballTransform.position;
        Vector3 baseDirection = AimDirection;
        float maxDistance = CurrentAimLineLength;
        
        // Calculate curve arc points
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
        // Calculate the arc point based on curve intensity
        float distance = maxDistance * t;
        
        // Base position along the straight line
        Vector3 basePoint = startPos + direction * distance;
        
        // Calculate curve offset (perpendicular to direction in XY plane)
        Vector3 perpendicularDirection = Vector3.Cross(direction, Vector3.forward).normalized;
        
        // Create an arc using sine function for smooth curve
        float curveAmount = Mathf.Sin(t * Mathf.PI) * (CurveIntensity / maxCurveIntensity) * (maxDistance * 0.2f);
        Vector3 curveOffset = perpendicularDirection * curveAmount;
        
        return basePoint + curveOffset;
    }
}