using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class BilliardController : PhysicsMaterialManager
{
    [Header("Power Settings")]
    [SerializeField] private float maxPower = 20f;
    [SerializeField] private float chargeSpeed = 10f;

    [Header("Components")]
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private AimingSystem aimingSystem;
    [SerializeField] private RigidbodyConfig rigidbodyConfig;
    [SerializeField] private Projection trajectoryProjection;

    [Header("Visuals")]
    [SerializeField] private Transform arrowIndicator; 
    [SerializeField] private Color minPowerColor = Color.white;
    [SerializeField] private Color maxPowerColor = Color.red;
    [SerializeField] private Color curveColor = Color.yellow;

    [Header("UI")]
    [SerializeField] private RadialPowerBar powerBar; 

    private Rigidbody rb;
    private LineRenderer aimLine;
    private Camera mainCam;

    private bool isCharging;
    private float currentPower;
    private enum ShootState { Idle, Ready, Charging }
    private ShootState shootState = ShootState.Idle;

    public float PowerPercentage => currentPower / maxPower;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aimLine = GetComponent<LineRenderer>();
        mainCam = Camera.main;

        RigidbodyConfigurator.ConfigureRigidbody(rb, rigidbodyConfig);
        
        // Initialize with this MonoBehaviour as owner for coroutines
        ballMovement.Initialize(rb, this);
        aimingSystem.Initialize(mainCam, transform);
        
        SetupLineRenderer();
    }

    protected override void Start()
    {
        base.Start();
        ApplyPhysicsMaterial();
    }
    
    void Update()
    {
        bool isBallMoving = ballMovement.IsBallMoving();

        if (isBallMoving)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            trajectoryProjection?.HideCurvePreview();
            return;
        }

        aimingSystem.UpdateAiming();
        HandleShooting();
        UpdateVisuals();
        UpdateTrajectoryPreview();
        
        // Debug key states (less frequent logging)
        if (aimingSystem.IsCurveShotActive && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Curve Shot Active - Intensity: {aimingSystem.CurveIntensity}");
        }
    }

    private void HandleShooting()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !isCharging)
        {
            isCharging = true;
            currentPower = 0f;
            shootState = ShootState.Charging;
            
            if (powerBar != null) powerBar.SetActive(true);
        }

        if(Mouse.current.leftButton.isPressed && isCharging)
        {
            currentPower += chargeSpeed * Time.deltaTime;
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);

            if (powerBar != null) powerBar.UpdatePower(PowerPercentage);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isCharging)
        {
            Shoot();
            isCharging = false;
            shootState = ShootState.Idle;
            
            if (powerBar != null) powerBar.SetActive(false);
        }
    }   
    
    public void Shoot()
    {
        Vector3 baseForce = aimingSystem.AimDirection * currentPower;
        
        Debug.Log($"Shooting - Curve Active: {aimingSystem.IsCurveShotActive}, Intensity: {aimingSystem.CurveIntensity}");
        
        // Use the enhanced ball movement with curve
        if (aimingSystem.IsCurveShotActive && Mathf.Abs(aimingSystem.CurveIntensity) > 0.1f)
        {
            ballMovement.ApplyForceWithCurve(baseForce, aimingSystem.CurveIntensity);
        }
        else
        {
            ballMovement.ApplyForce(baseForce);
        }

        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
        trajectoryProjection?.HideCurvePreview();
        currentPower = 0f;
    }

    public void Shoot(Vector2 velocity)
    {
        Vector3 force = new Vector3(velocity.x, velocity.y, 0);
        ballMovement.ApplyForce(force);

        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
        trajectoryProjection?.HideCurvePreview();
    }

    private void UpdateVisuals()
    {
        if (!isCharging && !Mouse.current.leftButton.isPressed)
        {
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            DrawAimLine(aimingSystem.CurrentAimLineLength);
            RotateArrow();

            // Set color based on curve shot mode
            Color lineColor = aimingSystem.IsCurveShotActive ? curveColor : minPowerColor;
            aimLine.startColor = lineColor;
            aimLine.endColor = lineColor;
        }
        else if (isCharging)
        {
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            float powerPercent = currentPower / maxPower;
            float scaledLength = Mathf.Min(aimingSystem.CurrentAimLineLength, aimingSystem.CurrentAimLineLength * (0.5f + powerPercent));
            DrawAimLine(scaledLength);
            RotateArrow();

            // Color changes based on power and curve mode
            Color baseColor = aimingSystem.IsCurveShotActive ? curveColor : minPowerColor;
            Color chargeColor = Color.Lerp(baseColor, maxPowerColor, powerPercent);
            aimLine.startColor = chargeColor;
            aimLine.endColor = chargeColor;

            arrowIndicator.localScale = Vector3.one * (1f + powerPercent * 0.5f);
        }
    }

    private void UpdateTrajectoryPreview()
    {
        if (trajectoryProjection != null && aimingSystem.IsCurveShotActive)
        {
            Vector3 baseVelocity = aimingSystem.AimDirection * (currentPower > 0 ? currentPower : 1f);
            Vector3 curvedVelocity = aimingSystem.GetCurvedVelocity(baseVelocity);
            
            trajectoryProjection.ShowCurvePreview(transform.position, curvedVelocity, aimingSystem.CurveIntensity);
        }
        else
        {
            trajectoryProjection?.HideCurvePreview();
        }
    }

    private void RotateArrow()
    {
        if (aimingSystem.AimDirection != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(aimingSystem.AimDirection);
            arrowIndicator.rotation = lookRot;
        }
    }

    private void DrawAimLine(float length)
    {
        // Check if we should draw a curved line or straight line
        if (aimingSystem.IsCurveShotActive && Mathf.Abs(aimingSystem.CurveIntensity) > 0.1f)
        {
            DrawCurvedAimLine(length);
        }
        else
        {
            DrawStraightAimLine(length);
        }
    }

    private void DrawStraightAimLine(float length)
    {
        Vector3 start = transform.position + Vector3.up * 0.05f;
        Vector3 end = start + (aimingSystem.AimDirection * length);
        
        // Ensure we're using 2-point line renderer for straight lines
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    private void DrawCurvedAimLine(float length)
    {
        // Get curve preview points from aiming system
        Vector3[] curvePoints = aimingSystem.GetCurvePreviewPoints(12);
        
        if (curvePoints != null && curvePoints.Length > 1)
        {
            // Set up line renderer for curve
            aimLine.positionCount = curvePoints.Length;
            
            // Find the maximum distance to scale points to fit within length
            float maxDistance = 0f;
            Vector3 startPos = transform.position + Vector3.up * 0.05f;
            
            for (int i = 1; i < curvePoints.Length; i++)
            {
                float distance = Vector3.Distance(startPos, curvePoints[i]);
                if (distance > maxDistance) maxDistance = distance;
            }
            
            // Apply scaled curve points to line renderer
            for (int i = 0; i < curvePoints.Length; i++)
            {
                Vector3 scaledPoint;
                if (i == 0)
                {
                    scaledPoint = startPos;
                }
                else
                {
                    // Scale the point to fit within the desired length
                    Vector3 direction = curvePoints[i] - curvePoints[0];
                    float scale = length / maxDistance;
                    scaledPoint = startPos + direction * scale;
                }
                
                aimLine.SetPosition(i, scaledPoint);
            }
        }
        else
        {
            // Fallback to straight line if curve points are not available
            DrawStraightAimLine(length);
        }
    }

    private void SetupLineRenderer()
    {
        aimLine.positionCount = 2;
        aimLine.enabled = false;
        aimLine.startWidth = 0.05f;
        aimLine.endWidth = 0.05f;   
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = Color.red;
        aimLine.endColor = Color.red;
    }

    private void ApplyPhysicsMaterial()
    {
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = GetBallMaterial();
        }
    }
}