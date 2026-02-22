using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BilliardBall : MonoBehaviour
{
    [Header("Settings")]
    public float curveStrength = 8.0f; // Reduced from 15.0f for more balanced curve
    public float spinDecayRate = 1.0f; // Reduced from 2.0f (spin lasts longer)
    public float stopVelocityThreshold = 0.1f;

    [Header("State - Debug Info")]
    [SerializeField] private float debugCurrentSideSpin = 0f; // For inspector visibility
    
    // Range: -1 (Max Right Spin) to 1 (Max Left Spin)
    public float currentSideSpin = 0f; 

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
        debugCurrentSideSpin = currentSideSpin;
        
        Debug.Log($"[BilliardBall] Shot applied: Direction={direction}, Power={power}, Spin={sideSpin}");
    }

    // Compatibility methods for existing BallMovement interface
    public void ApplyForce(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        currentSideSpin = 0f; // No spin for straight shots
        debugCurrentSideSpin = currentSideSpin;
        Debug.Log($"[BilliardBall] Applied straight force: {force}");
    }

    // Existing two-argument API (kept for compatibility)
    public void ApplyForceWithCurve(Vector3 baseForce, float curveIntensity)
    {
        // Convert curve intensity to side spin - slightly more conservative conversion
        float sideSpin = Mathf.Clamp(curveIntensity / 2.5f, -1f, 1f); // Changed from /2.0f to /2.5f for gentler curves
        
        rb.AddForce(baseForce, ForceMode.Impulse);
        currentSideSpin = sideSpin;
        debugCurrentSideSpin = currentSideSpin;
        
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
        float sideSpin = Mathf.Clamp((lateralIntensity / 2.5f) * sign, -1f, 1f);

        currentSideSpin = sideSpin;
        debugCurrentSideSpin = currentSideSpin;

        Debug.Log($"[BilliardBall] Applied curved force (3-arg): {baseForce}, LateralDir={lateralDirection}, Intensity={lateralIntensity}, Spin={sideSpin}");
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
            debugCurrentSideSpin = currentSideSpin;
            
            Debug.Log("[BilliardBall] Ball stopped, spin reset");
        }
        
        wasMovingLastFrame = isMoving;
        return isMoving;
    }

    void FixedUpdate()
    {
        debugCurrentSideSpin = currentSideSpin;
        
        // Only apply curve if the ball is moving and has spin applied
        if (rb.linearVelocity.magnitude > stopVelocityThreshold && Mathf.Abs(currentSideSpin) > 0.01f)
        {
            ApplyMagnusForce();
            DecaySpin();
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
        // force should be proportional to velocity 
        float velocityMultiplier = Mathf.Clamp(velocity2D.magnitude, 0.8f, 8f); // Reduced max clamp for gentler curves
        float magnusForceMagnitude = -currentSideSpin * curveStrength * velocityMultiplier; // Added negative sign here

        // Apply the force (only in XY plane)
        Vector3 magnusForce = perpDirection * magnusForceMagnitude;
        rb.AddForce(magnusForce, ForceMode.Force);
        
        // Debug visualization and logging
        Debug.DrawRay(transform.position, magnusForce * 0.5f, Color.green, 0.1f);
        Debug.DrawRay(transform.position, velocity.normalized * 2f, Color.blue, 0.1f);
        if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"[BilliardBall] Magnus Force: {magnusForce.magnitude:F3}, Spin: {currentSideSpin:F3}, Velocity: {velocity2D.magnitude:F3}");
        }
    }

    void DecaySpin()
    {
        // Linearly decay the spin over time so the curve straightens out
        currentSideSpin = Mathf.MoveTowards(currentSideSpin, 0, spinDecayRate * Time.fixedDeltaTime);
    }

   
}