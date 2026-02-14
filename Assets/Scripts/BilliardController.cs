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

    [Header("Visuals")]
    [SerializeField] private Transform arrowIndicator; 
    [SerializeField] private Color minPowerColor = Color.white;
    [SerializeField] private Color maxPowerColor = Color.red;

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

        // Configure rigidbody using the helper
        RigidbodyConfigurator.ConfigureRigidbody(rb, rigidbodyConfig);
        
        // Initialize components
        ballMovement.Initialize(rb);
        aimingSystem.Initialize(mainCam, transform);
        
        SetupLineRenderer();
    }

    protected override void Start()
    {
        base.Start(); // Creates physics materials
        ApplyPhysicsMaterial();
    }
    
    void Update()
    {
        bool isBallMoving = ballMovement.IsBallMoving();

        if (isBallMoving)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            return;
        }

        aimingSystem.UpdateAiming();
        HandleShooting();
        UpdateVisuals();
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
        Vector3 force = aimingSystem.AimDirection * currentPower;
        ballMovement.ApplyForce(force);

        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
        currentPower = 0f;
    }

    public void Shoot(Vector2 velocity)
    {
        Vector3 force = new Vector3(velocity.x, velocity.y, 0);
        ballMovement.ApplyForce(force);

        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
    }

    private void UpdateVisuals()
    {
        if (!isCharging && !Mouse.current.leftButton.isPressed)
        {
            // Aiming idle state
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            // Use the AimingSystem's current aim line length
            DrawAimLine(aimingSystem.CurrentAimLineLength);
            RotateArrow();

            // Reset colors
            aimLine.startColor = minPowerColor;
            aimLine.endColor = minPowerColor;
        }
        else if (isCharging)
        {
            // Charging state
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            // Calculate percentages for visuals (0 to 1)
            float powerPercent = currentPower / maxPower;

            // Scale the line length with power, but cap at the collision point
            float scaledLength = Mathf.Min(aimingSystem.CurrentAimLineLength, aimingSystem.CurrentAimLineLength * (0.5f + powerPercent));
            DrawAimLine(scaledLength);
            RotateArrow();

            // Color change based on power
            Color chargeColor = Color.Lerp(minPowerColor, maxPowerColor, powerPercent);
            aimLine.startColor = chargeColor;
            aimLine.endColor = chargeColor;

            // Scale arrow based on power
            arrowIndicator.localScale = Vector3.one * (1f + powerPercent * 0.5f);
        }
    }

    private void RotateArrow()
    {
        if (aimingSystem.AimDirection != Vector3.zero)
        {
            // Arrow facing aim direction
            Quaternion lookRot = Quaternion.LookRotation(aimingSystem.AimDirection);
            arrowIndicator.rotation = lookRot;
        }
    }

    private void DrawAimLine(float length)
    {
        Vector3 start = transform.position + Vector3.up * 0.05f;
        Vector3 end = start + (aimingSystem.AimDirection * length);
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
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