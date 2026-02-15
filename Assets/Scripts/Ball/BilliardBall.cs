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

    // Initialize method for compatibility with existing system
    public void Initialize(Rigidbody rigidbody, MonoBehaviour ownerMonoBehaviour = null)
    {
        rb = rigidbody;
    }

    // Call this to shoot the ball (adapted for 3D)
    public void Shoot(Vector3 direction, float power, float sideSpin = 0f)
    {
        // 1. Apply the immediate forward impulse
        rb.AddForce(direction.normalized * power, ForceMode.Impulse);

        // 2. Set the side spin ("English")
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

    public void ApplyForceWithCurve(Vector3 baseForce, float curveIntensity)
    {
        // Convert curve intensity to side spin - slightly more conservative conversion
        float sideSpin = Mathf.Clamp(curveIntensity / 2.5f, -1f, 1f); // Changed from /2.0f to /2.5f for gentler curves
        
        rb.AddForce(baseForce, ForceMode.Impulse);
        currentSideSpin = sideSpin;
        debugCurrentSideSpin = currentSideSpin;
        
        Debug.Log($"[BilliardBall] Applied curved force: {baseForce}, Spin={sideSpin}, CurveIntensity={curveIntensity}");
    }

    public bool IsBallMoving()
    {
        bool isLinearVelocityLow = rb.linearVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularVelocityLow = rb.angularVelocity.magnitude <= stopVelocityThreshold;
        
        bool isMoving = !(isLinearVelocityLow && isAngularVelocityLow);
        
        // Only reset when ball completely stops and wasn't moving last frame
        if (!isMoving && wasMovingLastFrame)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            currentSideSpin = 0f; // Reset immediately when stopped
            debugCurrentSideSpin = currentSideSpin;
            
            Debug.Log("[BilliardBall] Ball stopped, spin reset");
        }
        
        wasMovingLastFrame = isMoving;
        return isMoving;
    }

    void FixedUpdate()
    {
        // Update debug value
        debugCurrentSideSpin = currentSideSpin;
        
        // Only apply curve if the ball is moving and has spin
        if (rb.linearVelocity.magnitude > stopVelocityThreshold && Mathf.Abs(currentSideSpin) > 0.01f)
        {
            ApplyMagnusForce();
            DecaySpin();
        }
    }

    void ApplyMagnusForce()
    {
        // 1. Get current velocity direction (in XY plane for top-down view)
        Vector3 velocity = rb.linearVelocity;
        Vector2 velocity2D = new Vector2(velocity.x, velocity.y);
        
        // Skip if velocity is too small to prevent NaN issues
        if (velocity2D.magnitude < 0.01f) return;
        
        // 2. Calculate the perpendicular vector for 3D (cross product with Z-axis)
        Vector3 perpDirection = Vector3.Cross(velocity.normalized, Vector3.forward).normalized;

        // 3. Calculate Force - More balanced formula
        // The force should be proportional to velocity for realistic Magnus effect
        float velocityMultiplier = Mathf.Clamp(velocity2D.magnitude, 0.8f, 8f); // Reduced max clamp for gentler curves
        
        // **FIX: Invert the spin direction to match the expected curve direction**
        float magnusForceMagnitude = -currentSideSpin * curveStrength * velocityMultiplier; // Added negative sign here

        // 4. Apply the force (only in XY plane)
        Vector3 magnusForce = perpDirection * magnusForceMagnitude;
        rb.AddForce(magnusForce, ForceMode.Force);
        
        // Debug visualization and logging
        Debug.DrawRay(transform.position, magnusForce * 0.5f, Color.green, 0.1f);
        Debug.DrawRay(transform.position, velocity.normalized * 2f, Color.blue, 0.1f);
        
        // Occasional debug logging
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

    // Add this method to check for conflicts
    void Start()
    {
        // Check for component conflicts
        BallMovement oldBallMovement = GetComponent<BallMovement>();
        if (oldBallMovement != null)
        {
            Debug.LogWarning("[BilliardBall] Found old BallMovement component! Please remove it to avoid conflicts.");
        }
        
        Debug.Log($"[BilliardBall] Initialized with curveStrength: {curveStrength}, spinDecayRate: {spinDecayRate}");
    }
}