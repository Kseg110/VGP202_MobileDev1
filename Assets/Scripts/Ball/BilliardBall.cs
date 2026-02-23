using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BilliardBall : MonoBehaviour
{
    [Header("Settings")]
    public float curveStrength = 15.0f; // Increased from 8.0f for more noticeable curves
    public float spinDecayRate = 0.5f; // Reduced from 1.0f so spin lasts longer
    public float stopVelocityThreshold = 0.1f;

    [Header("Spin Settings")]
    public float spinStrength = 15f; // Increased from 10f for more visual spin
    public float topSpinEffect = 8f; // Increased from 5f for more top spin effect

    [Header("State - Debug Info")]
    [SerializeField] private float debugCurrentSideSpin = 0f; // For inspector visibility
    [SerializeField] private Vector2 debugCurrentSpin = Vector2.zero; // For inspector visibility
    
    // Range: -1 (Max Right Spin) to 1 (Max Left Spin)
    public float currentSideSpin = 0f;
    
    // Current 2D spin values from UI
    private Vector2 currentSpin = Vector2.zero;

    private Rigidbody rb;
    private bool wasMovingLastFrame = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Debug.Log($"[BilliardBall] Initialized with curveStrength: {curveStrength}, spinDecayRate: {spinDecayRate}");
    }
    
    public void Initialize(Rigidbody rigidbody, MonoBehaviour ownerMonoBehaviour = null)
    {
        rb = rigidbody;
    }

    // Call this to shoot the ball (adapted for 3D)
    public void Shoot(Vector3 direction, float power, float sideSpin = 0f)
    {
        // Apply immediate forward impulse
        rb.AddForce(direction.normalized * power, ForceMode.Impulse);

        // Set the side spin 
        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, 0f); // Convert to 2D spin
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        
        Debug.Log($"[BilliardBall] Shot applied: Direction={direction}, Power={power}, Spin={sideSpin}");
    }

    // Legacy compatibility method - FIXED to apply current spin when no explicit spin is provided
    public void ApplyForce(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        
        // Apply current spin if it exists (from UI)
        if (currentSpin.magnitude > 0.01f)
        {
            Vector3 angular = new Vector3(-currentSpin.y, 0, currentSpin.x) * spinStrength;
            rb.AddTorque(angular, ForceMode.Impulse);
            Debug.Log($"[BilliardBall] Applied existing spin: {currentSpin}, Torque: {angular}");
        }
        
        Debug.Log($"[BilliardBall] Applied force: {force}, PreservingSpin={currentSpin}");
    }

    // Existing two-argument API (kept for compatibility)
    public void ApplyForceWithCurve(Vector3 baseForce, float curveIntensity)
    {
        // Convert curve intensity to side spin - slightly more conservative conversion
        float sideSpin = Mathf.Clamp(curveIntensity / 2.0f, -1f, 1f); // Changed back to 2.0f for stronger curves
        
        rb.AddForce(baseForce, ForceMode.Impulse);
        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, currentSpin.y); // Preserve Y spin from UI
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        
        Debug.Log($"[BilliardBall] Applied curved force: {baseForce}, Spin={sideSpin}, CurveIntensity={curveIntensity}");
    }

    public void ApplyForceWithCurve(Vector3 baseForce, Vector3 lateralDirection, float lateralIntensity)
    {
        rb.AddForce(baseForce, ForceMode.Impulse);

        // Determine spin sign from lateralDirection
        float sign = 0f;
        if (lateralDirection != Vector3.zero)
        {
            sign = Mathf.Sign(Vector3.Dot(lateralDirection.normalized, Vector3.right));
        }

        // Map lateralIntensity for sidespin
        float sideSpin = Mathf.Clamp((lateralIntensity / 2.0f) * sign, -1f, 1f);

        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, currentSpin.y); // Preserve Y spin from UI
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;

        Debug.Log($"[BilliardBall] Applied curved force (3-arg): {baseForce}, LateralDir={lateralDirection}, Intensity={lateralIntensity}, Spin={sideSpin}");
    }

    public void ApplyForce(Vector3 force, Vector2 spin)
    {
        rb.AddForce(force, ForceMode.Impulse);
        ApplySpin(spin);
        Debug.Log($"[BilliardBall] Applied force with spin: {force}, Spin={spin}");
    }

    public void ApplyForceWithCurve(Vector3 force, float curveIntensity, Vector2 spin)
    {
        // Apply base force
        rb.AddForce(force, ForceMode.Impulse);
        
        // Apply spin (this will set both currentSpin and currentSideSpin)
        ApplySpin(spin);
        
        // If there's curve intensity but no spin from UI, use legacy curve system
        if (spin.magnitude < 0.01f && Mathf.Abs(curveIntensity) > 0.01f)
        {
            float legacySideSpin = Mathf.Clamp(curveIntensity / 2.0f, -1f, 1f);
            currentSideSpin = legacySideSpin;
            currentSpin = new Vector2(legacySideSpin, currentSpin.y);
        }
        
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        
        Debug.Log($"[BilliardBall] Applied curved force with spin: {force}, CurveIntensity={curveIntensity}, Spin={spin}, FinalSpin={currentSpin}");
    }

    // Add explicit method for resetting spin when needed
    public void ApplyForceAndResetSpin(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        currentSideSpin = 0f;
        currentSpin = Vector2.zero;
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        Debug.Log($"[BilliardBall] Applied straight force with spin reset: {force}");
    }

    private void ApplySpin(Vector2 spin)
    {
        // Store the current spin for Magnus force calculations
        currentSpin = spin;
        currentSideSpin = spin.x; // X component is side spin
        
        // Apply visual torque for ball rotation
        Vector3 angular = new Vector3(-spin.y, 0, spin.x) * spinStrength;
        rb.AddTorque(angular, ForceMode.Impulse);
        
        // Update debug values
        debugCurrentSpin = currentSpin;
        debugCurrentSideSpin = currentSideSpin;
        
        Debug.Log($"[BilliardBall] Applied spin: {spin}, Torque: {angular}, SideSpin: {currentSideSpin}");
    }

    public bool IsBallMoving()
    {
        bool isLinearVelocityLow = rb.linearVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularVelocityLow = rb.angularVelocity.magnitude <= stopVelocityThreshold;
        
        bool isMoving = !(isLinearVelocityLow && isAngularVelocityLow);
        
        // Reset when ball completely stops
        if (!isMoving && wasMovingLastFrame)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            currentSideSpin = 0f;
            currentSpin = Vector2.zero;
            debugCurrentSideSpin = currentSideSpin;
            debugCurrentSpin = currentSpin;
            
            Debug.Log("[BilliardBall] Ball stopped, spin reset");
        }
        
        wasMovingLastFrame = isMoving;
        return isMoving;
    }

    void FixedUpdate()
    {
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        
        // Only apply spin effects if the ball is moving and has spin applied
        if (rb.linearVelocity.magnitude > stopVelocityThreshold && currentSpin.magnitude > 0.01f)
        {
            ApplySpinEffects();
            DecaySpin();
        }
    }

    void ApplySpinEffects()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector2 velocity2D = new Vector2(velocity.x, velocity.y);
        
        if (velocity2D.magnitude < 0.01f) return;
        
        // Apply side spin (Magnus force)
        if (Mathf.Abs(currentSpin.x) > 0.01f)
        {
            ApplyMagnusForce();
        }
        
        // Apply top/back spin effect (affects velocity magnitude)
        if (Mathf.Abs(currentSpin.y) > 0.01f)
        {
            ApplyTopSpinEffect();
        }
    }

    void ApplyMagnusForce()
    {
        // Get current velocity direction (in XY)
        Vector3 velocity = rb.linearVelocity;
        Vector2 velocity2D = new Vector2(velocity.x, velocity.y);
        
        if (velocity2D.magnitude < 0.01f) return;
        
        // Calculate the perpendicular vector (cross product with Z-axis)
        Vector3 perpDirection = Vector3.Cross(velocity.normalized, Vector3.forward).normalized;

        // Calculate Force
        float velocityMultiplier = Mathf.Clamp(velocity2D.magnitude, 1.0f, 12f); // Increased range for stronger effect
        float magnusForceMagnitude = -currentSpin.x * curveStrength * velocityMultiplier;

        // Apply the force (only in XY plane)
        Vector3 magnusForce = perpDirection * magnusForceMagnitude;
        rb.AddForce(magnusForce, ForceMode.Force);
        
        // Debug visualization and logging
        Debug.DrawRay(transform.position, magnusForce * 0.5f, Color.green, 0.1f);
        Debug.DrawRay(transform.position, velocity.normalized * 2f, Color.blue, 0.1f);
        if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"[BilliardBall] Magnus Force: {magnusForce.magnitude:F3}, SideSpin: {currentSpin.x:F3}, Velocity: {velocity2D.magnitude:F3}");
        }
    }

    void ApplyTopSpinEffect()
    {
        // Top spin (positive Y) makes ball accelerate, back spin (negative Y) makes it decelerate
        Vector3 velocity = rb.linearVelocity;
        Vector3 topSpinForce = velocity.normalized * (currentSpin.y * topSpinEffect);
        
        rb.AddForce(topSpinForce, ForceMode.Force);
        
        if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"[BilliardBall] Top Spin Force: {topSpinForce.magnitude:F3}, TopSpin: {currentSpin.y:F3}");
        }
    }

    void DecaySpin()
    {
        // Decay both spin components
        currentSpin = Vector2.MoveTowards(currentSpin, Vector2.zero, spinDecayRate * Time.fixedDeltaTime);
        currentSideSpin = currentSpin.x; // Keep side spin in sync
    }
}