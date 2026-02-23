using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BilliardBall : MonoBehaviour
{
    [Header("Settings")]
    public float curveStrength = 15.0f;
    public float spinDecayRate = 0.5f;
    public float stopVelocityThreshold = 0.1f;

    [Header("Spin Settings")]
    public float spinStrength = 15f;
    public float topSpinEffect = 8f;

    [Header("Kinematic Physics")]
    [SerializeField] private float linearDamping = 0.95f;
    [SerializeField] private float angularDamping = 0.98f;
    [SerializeField] private float restitution = 0.8f; // Bounciness

    [Header("State - Debug Info")]
    [SerializeField] private float debugCurrentSideSpin = 0f;
    [SerializeField] private Vector2 debugCurrentSpin = Vector2.zero;
    [SerializeField] private Vector3 debugCurrentVelocity = Vector3.zero;
    
    public float currentSideSpin = 0f;
    private Vector2 currentSpin = Vector2.zero;

    private Rigidbody rb;
    private bool wasMovingLastFrame = false;

    // Manual velocity tracking for kinematic mode
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 currentAngularVelocity = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Start()
    {
        Debug.Log($"[BilliardBall] Initialized in KINEMATIC mode with curveStrength: {curveStrength}, spinDecayRate: {spinDecayRate}");
    }
    
    public void Initialize(Rigidbody rigidbody, MonoBehaviour ownerMonoBehaviour = null)
    {
        rb = rigidbody;
    }

    // Shoot with kinematic velocity
    public void Shoot(Vector3 direction, float power, float sideSpin = 0f)
    {
        currentVelocity = direction.normalized * power;
        
        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, 0f);
        
        // Apply angular velocity for visual spin
        ApplyAngularVelocity(currentSpin);
        
        UpdateDebugValues();
        Debug.Log($"[BilliardBall] Kinematic shot: Direction={direction}, Power={power}, Velocity={currentVelocity.magnitude:F2}, Spin={sideSpin}");
    }

    // Convert force to velocity (kinematic mode)
    public void ApplyForce(Vector3 force)
    {
        currentVelocity += force / rb.mass;
        
        if (currentSpin.magnitude > 0.01f)
        {
            ApplyAngularVelocity(currentSpin);
            Debug.Log($"[BilliardBall] Applied existing spin: {currentSpin}");
        }
        
        UpdateDebugValues();
        Debug.Log($"[BilliardBall] Applied kinematic force: {force}, NewVelocity={currentVelocity.magnitude:F2}");
    }

    public void ApplyForceWithCurve(Vector3 baseForce, float curveIntensity)
    {
        currentVelocity += baseForce / rb.mass;
        
        float sideSpin = Mathf.Clamp(curveIntensity / 2.0f, -1f, 1f);
        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, currentSpin.y);
        
        ApplyAngularVelocity(currentSpin);
        UpdateDebugValues();
        
        Debug.Log($"[BilliardBall] Applied kinematic curved force: Velocity={currentVelocity.magnitude:F2}, Spin={sideSpin}");
    }

    public void ApplyForceWithCurve(Vector3 baseForce, Vector3 lateralDirection, float lateralIntensity)
    {
        currentVelocity += baseForce / rb.mass;

        float sign = 0f;
        if (lateralDirection != Vector3.zero)
        {
            sign = Mathf.Sign(Vector3.Dot(lateralDirection.normalized, Vector3.right));
        }

        float sideSpin = Mathf.Clamp((lateralIntensity / 2.0f) * sign, -1f, 1f);
        currentSideSpin = sideSpin;
        currentSpin = new Vector2(sideSpin, currentSpin.y);

        ApplyAngularVelocity(currentSpin);
        UpdateDebugValues();
        
        Debug.Log($"[BilliardBall] Applied kinematic curved force (3-arg): Velocity={currentVelocity.magnitude:F2}, Spin={sideSpin}");
    }

    public void ApplyForce(Vector3 force, Vector2 spin)
    {
        currentVelocity += force / rb.mass;
        ApplySpin(spin);
        UpdateDebugValues();
        Debug.Log($"[BilliardBall] Applied kinematic force with spin: Velocity={currentVelocity.magnitude:F2}, Spin={spin}");
    }

    public void ApplyForceWithCurve(Vector3 force, float curveIntensity, Vector2 spin)
    {
        currentVelocity += force / rb.mass;
        ApplySpin(spin);
        
        if (spin.magnitude < 0.01f && Mathf.Abs(curveIntensity) > 0.01f)
        {
            float legacySideSpin = Mathf.Clamp(curveIntensity / 2.0f, -1f, 1f);
            currentSideSpin = legacySideSpin;
            currentSpin = new Vector2(legacySideSpin, currentSpin.y);
        }
        
        UpdateDebugValues();
        Debug.Log($"[BilliardBall] Applied kinematic curved force with spin: Velocity={currentVelocity.magnitude:F2}, FinalSpin={currentSpin}");
    }

    public void ApplyForceAndResetSpin(Vector3 force)
    {
        currentVelocity += force / rb.mass;
        currentSideSpin = 0f;
        currentSpin = Vector2.zero;
        currentAngularVelocity = Vector3.zero;
        
        UpdateDebugValues();
        Debug.Log($"[BilliardBall] Applied kinematic straight force with spin reset: Velocity={currentVelocity.magnitude:F2}");
    }

    private void ApplySpin(Vector2 spin)
    {
        currentSpin = spin;
        currentSideSpin = spin.x;
        ApplyAngularVelocity(spin);
        UpdateDebugValues();
        
        Debug.Log($"[BilliardBall] Applied spin: {spin}, SideSpin: {currentSideSpin}");
    }

    private void ApplyAngularVelocity(Vector2 spin)
    {
        // Convert spin to angular velocity 
        Vector3 angularVel = new Vector3(-spin.y, 0, spin.x) * spinStrength;
        currentAngularVelocity += angularVel;
    }

    public bool IsBallMoving()
    {
        bool isLinearVelocityLow = currentVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularVelocityLow = currentAngularVelocity.magnitude <= stopVelocityThreshold;
        
        bool isMoving = !(isLinearVelocityLow && isAngularVelocityLow);
        
        if (!isMoving && wasMovingLastFrame)
        {
            currentVelocity = Vector3.zero;
            currentAngularVelocity = Vector3.zero;
            currentSideSpin = 0f;
            currentSpin = Vector2.zero;
            
            UpdateDebugValues();
            Debug.Log("[BilliardBall] Ball stopped, spin and velocity reset");
        }
        
        wasMovingLastFrame = isMoving;
        return isMoving;
    }

    void FixedUpdate()
    {
        UpdateDebugValues();
        
        // Apply spin effects 
        if (currentVelocity.magnitude > stopVelocityThreshold && currentSpin.magnitude > 0.01f)
        {
            ApplySpinEffectsKinematic();
            DecaySpin();
        }
        
        // Apply damping (friction)
        ApplyDamping();
        
        // Manual position update 
        Vector3 newPosition = rb.position + currentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
        
        if (currentAngularVelocity.magnitude > 0.01f)
        {
            Quaternion deltaRotation = Quaternion.Euler(currentAngularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    private void ApplySpinEffectsKinematic()
    {
        // Apply Magnus effect (side spin curve)
        if (Mathf.Abs(currentSpin.x) > 0.01f)
        {
            ApplyMagnusEffectKinematic();
        }
        
        // Apply top/back spin effect
        if (Mathf.Abs(currentSpin.y) > 0.01f)
        {
            ApplyTopSpinEffectKinematic();
        }
    }

    private void ApplyMagnusEffectKinematic()
    {
        if (currentVelocity.magnitude < 0.01f) return;
        
        // Calculate perpendicular direction for curve
        Vector3 perpDirection = Vector3.Cross(currentVelocity.normalized, Vector3.forward).normalized;
        
        // Calculate Magnus effect strength
        float velocityMultiplier = Mathf.Clamp(currentVelocity.magnitude, 1.0f, 12f);
        float magnusAcceleration = -currentSpin.x * curveStrength * velocityMultiplier;
        
        // Apply as direct velocity change (acceleration * deltaTime)
        Vector3 magnusVelocityChange = perpDirection * magnusAcceleration * Time.fixedDeltaTime;
        currentVelocity += magnusVelocityChange;
        
        // Debug visualization
        Debug.DrawRay(transform.position, magnusVelocityChange * 10f, Color.green, 0.1f);
        Debug.DrawRay(transform.position, currentVelocity.normalized * 2f, Color.blue, 0.1f);
        
        if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"[BilliardBall] Magnus Effect: Acceleration={magnusAcceleration:F3}, SideSpin={currentSpin.x:F3}, Velocity={currentVelocity.magnitude:F3}");
        }
    }

    private void ApplyTopSpinEffectKinematic()
    {
        if (currentVelocity.magnitude < 0.01f) return;
        
        // Top spin accelerates, back spin decelerates
        Vector3 topSpinAcceleration = currentVelocity.normalized * (currentSpin.y * topSpinEffect);
        currentVelocity += topSpinAcceleration * Time.fixedDeltaTime;
        
        if (Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"[BilliardBall] Top Spin Effect: Acceleration={topSpinAcceleration.magnitude:F3}, TopSpin={currentSpin.y:F3}");
        }
    }

    private void ApplyDamping()
    {
        // Apply exponential damping for smooth deceleration
        float dampingFactor = Mathf.Pow(linearDamping, Time.fixedDeltaTime * 60f);
        currentVelocity *= dampingFactor;
        
        float angularDampingFactor = Mathf.Pow(angularDamping, Time.fixedDeltaTime * 60f);
        currentAngularVelocity *= angularDampingFactor;
        
        // Stop completely if below threshold
        if (currentVelocity.magnitude < stopVelocityThreshold)
        {
            currentVelocity = Vector3.zero;
        }
        
        if (currentAngularVelocity.magnitude < stopVelocityThreshold)
        {
            currentAngularVelocity = Vector3.zero;
        }
    }

    void DecaySpin()
    {
        currentSpin = Vector2.MoveTowards(currentSpin, Vector2.zero, spinDecayRate * Time.fixedDeltaTime);
        currentSideSpin = currentSpin.x;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;
        
        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal;
        
        // Reflect velocity off the collision normal
        Vector3 reflectedVelocity = Vector3.Reflect(currentVelocity, normal);
        currentVelocity = reflectedVelocity * restitution;
        
        // Optionally reduce spin on collision
        currentSpin *= 0.9f;
        currentSideSpin = currentSpin.x;
        currentAngularVelocity *= 0.9f;
        
        Debug.Log($"[BilliardBall] Collision with {collision.gameObject.name}: Normal={normal}, NewVelocity={currentVelocity.magnitude:F2}");
        
        // Transfer momentum to other kinematic ball
        var otherBall = collision.rigidbody?.GetComponent<BilliardBall>();
        if (otherBall != null)
        {
            // Calculate impulse transfer between two balls
            Vector3 relativeVelocity = currentVelocity;
            Vector3 impulse = relativeVelocity * rb.mass * 0.5f;
            otherBall.ApplyForce(impulse);
            
            // Reduce our velocity by the transferred amount
            currentVelocity *= 0.5f;
        }
        // Transfer to dynamic rigidbody
        else if (collision.rigidbody != null && !collision.rigidbody.isKinematic)
        {
            collision.rigidbody.AddForce(currentVelocity * rb.mass * 0.5f, ForceMode.Impulse);
        }
    }

    private void UpdateDebugValues()
    {
        debugCurrentSideSpin = currentSideSpin;
        debugCurrentSpin = currentSpin;
        debugCurrentVelocity = currentVelocity;
    }

    // Public property to get current velocity (for trajectory prediction)
    public Vector3 Velocity => currentVelocity;
}