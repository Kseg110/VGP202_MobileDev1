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
    [SerializeField] private BilliardBall billiardBall;
    public AimingSystem aimingSystem; 
    [SerializeField] private RigidbodyConfig rigidbodyConfig;
    [SerializeField] private Projection trajectoryProjection;

    [Header("Visuals")]
    [SerializeField] private Transform arrowIndicator; 
    [SerializeField] private Color minPowerColor = Color.white;
    [SerializeField] private Color maxPowerColor = Color.red;
    [SerializeField] private Color curveColor = Color.yellow;

    [Header("UI")]
    [SerializeField] private RadialPowerBar powerBar;
    [SerializeField] private ShootButton shootButton;

    private Rigidbody rb;
    private LineRenderer aimLine;
    private Camera mainCam;

    private bool isCharging;
    private float currentPower;

    public float PowerPercentage => currentPower / maxPower;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aimLine = GetComponent<LineRenderer>();
        mainCam = Camera.main;

        RigidbodyConfigurator.ConfigureRigidbody(rb, rigidbodyConfig);
        
        if (billiardBall == null)
        {
            billiardBall = GetComponent<BilliardBall>();
            if (billiardBall == null)
            {
                billiardBall = gameObject.AddComponent<BilliardBall>();
            }
        }
        
        billiardBall.Initialize(rb, this);
        aimingSystem.Initialize(mainCam, transform);
        
        SetupLineRenderer();
    }

    protected override void Start()
    {
        base.Start();
        ApplyPhysicsMaterial();

        // Find ShootButton if not assigned
        if (shootButton == null)
        {
            shootButton = FindFirstObjectByType<ShootButton>();
            if (shootButton == null)
            {
                //Debug.LogWarning("[BilliardController] ShootButton not found in scene!");
            }
        }
        
        // Setup shoot button connection
        SetupShootButton();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchEnd += OnTouchEnd;
        }
        
    }

    private void SetupShootButton()
    {
        if (shootButton != null)
        {
            shootButton.OnStartCharging += StartPowerCharging;
            shootButton.OnFireShot += FireShot;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (shootButton != null)
        {
            shootButton.OnStartCharging -= StartPowerCharging;
            shootButton.OnFireShot -= FireShot;
        }
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchEnd -= OnTouchEnd;
        }
    }

    private void OnTouchEnd()
    {
        aimingSystem.OnTouchRelease();
    }

    void Update()
    {
        bool isBallMoving = billiardBall.IsBallMoving();

        if (isBallMoving)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            trajectoryProjection?.HideCurvePreview();
            return;
        }

        // Temporarily disable aiming while pause menu or spin UI are open to prevent conflicts
        var canvasMgr = FindAnyObjectByType<GameCanvasManager>();
        if (canvasMgr != null && canvasMgr.pauseMenuPanel != null && canvasMgr.pauseMenuPanel.activeSelf)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            trajectoryProjection?.HideCurvePreview();
            return;
        }

        var spinBtn = FindFirstObjectByType<SpinButton>();
        if (spinBtn != null && spinBtn.IsOpen)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            trajectoryProjection?.HideCurvePreview();
            return;
        }

        aimingSystem.UpdateAiming();
        
        // Handle charging if in charging state
        if (isCharging)
        {
            HandlePowerCharging();
        }
        
        UpdateVisuals();
        UpdateTrajectoryPreview();
    }

    private void StartPowerCharging()
    {
        if (billiardBall.IsBallMoving())
        {
            return;
        }
        
        isCharging = true;
        currentPower = 0f;
        
        if (powerBar != null) 
            powerBar.SetActive(true);
    }
    
    private void HandlePowerCharging()
    {
        if (!isCharging) return;
        
        currentPower += chargeSpeed * Time.deltaTime;
        currentPower = Mathf.Clamp(currentPower, 0, maxPower);

        if (powerBar != null) 
            powerBar.UpdatePower(PowerPercentage);
            
        // Update button visual feedback
        if (shootButton != null)
        {
            if (PowerPercentage >= 0.8f)
            {
                shootButton.SetReadyToFireState();
            }
            else
            {
                shootButton.SetChargingState();
            }
        }
    }
    
    private void FireShot()
    {
        if (!isCharging)
        {
            return;
        }
        
        Vector3 baseForce = aimingSystem.AimDirection * currentPower;
        
        // Apply force with or without curve
        if (aimingSystem.IsCurveShotActive && Mathf.Abs(aimingSystem.CurveIntensity) > 0.1f)
        {
            billiardBall.ApplyForceWithCurve(baseForce, aimingSystem.CurveIntensity);
        }
        else
        {
            billiardBall.ApplyForce(baseForce);
        }

        // Decrement shots in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Shots--;
        }

        // Reset state
        isCharging = false;
        currentPower = 0f;
        
        // Hide UI elements
        if (powerBar != null) 
            powerBar.SetActive(false);
        if (shootButton != null) 
            shootButton.SetIdleState();
            
        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
        trajectoryProjection?.HideCurvePreview();
    }

    // Legacy shoot method for compatibility
    public void Shoot()
    {
        FireShot();
    }

    public void Shoot(Vector2 velocity)
    {
        Vector3 force = new Vector3(velocity.x, velocity.y, 0);
        billiardBall.ApplyForce(force);

        // Decrement shots
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Shots--;
        }

        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
        trajectoryProjection?.HideCurvePreview();
    }

    private void UpdateVisuals()
    {
        bool showAiming = !billiardBall.IsBallMoving();
        
        aimLine.enabled = showAiming;
        arrowIndicator.gameObject.SetActive(showAiming);
        
        if (!showAiming) return;

        DrawAimLine(aimingSystem.CurrentAimLineLength);
        RotateArrow();

        // Set color based on state and curve mode
        Color lineColor;
        if (isCharging)
        {
            float powerPercent = currentPower / maxPower;
            Color baseColor = aimingSystem.IsCurveShotActive ? curveColor : minPowerColor;
            lineColor = Color.Lerp(baseColor, maxPowerColor, powerPercent);
            
            // Scale arrow based on power
            arrowIndicator.localScale = Vector3.one * (1f + powerPercent * 0.5f);
        }
        else
        {
            lineColor = aimingSystem.IsCurveShotActive ? curveColor : minPowerColor;
            arrowIndicator.localScale = Vector3.one;
        }
        
        aimLine.startColor = lineColor;
        aimLine.endColor = lineColor;
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
        
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    private void DrawCurvedAimLine(float length)
    {
        Vector3[] curvePoints = aimingSystem.GetCurvePreviewPoints(12);
        
        if (curvePoints != null && curvePoints.Length > 1)
        {
            aimLine.positionCount = curvePoints.Length;
            
            Vector3 startPos = transform.position + Vector3.up * 0.05f;
            float maxDistance = 0f;
            
            for (int i = 1; i < curvePoints.Length; i++)
            {
                float distance = Vector3.Distance(startPos, curvePoints[i]);
                if (distance > maxDistance) maxDistance = distance;
            }
            
            for (int i = 0; i < curvePoints.Length; i++)
            {
                Vector3 scaledPoint;
                if (i == 0)
                {
                    scaledPoint = startPos;
                }
                else
                {
                    Vector3 direction = curvePoints[i] - curvePoints[0];
                    float scale = length / maxDistance;
                    scaledPoint = startPos + direction * scale;
                }
                
                aimLine.SetPosition(i, scaledPoint);
            }
        }
        else
        {
            DrawStraightAimLine(length);
        }
    }

    private void SetupLineRenderer()
    {
        aimLine.positionCount = 2;
        aimLine.enabled = true;
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