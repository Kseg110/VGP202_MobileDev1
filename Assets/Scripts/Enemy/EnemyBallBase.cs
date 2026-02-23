using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public abstract class EnemyBallBase : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] protected RigidbodyConfig rigidbodyConfig;
    [SerializeField] protected float stopVelocityThreshold = 0.1f;
    [SerializeField] protected float mass = 1f;

    [Header("Curve (optional simple support)")]
    [SerializeField] protected float curvePullForce = 4f;
    [SerializeField] protected float curveDuration = 0.6f;

    [Header("Tags")]
    [SerializeField] private string enemyTag = "Enemy";

    protected Rigidbody rb;
    private bool wasMovingLastFixedUpdate;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Apply configured rigidbody settings (uses existing helper)
        if (rigidbodyConfig != null)
        {
            RigidbodyConfigurator.ConfigureRigidbody(rb, rigidbodyConfig);
        }

        // Apply mass
        rb.mass = mass;
    }

    protected virtual void Start()
    {
        // placeholder for future derived classes
    }

    protected virtual void FixedUpdate()
    {
        // simple motion 
        bool isMoving = IsBallMoving();

        // Derived classes can use this info via overridden FixedUpdate 
        wasMovingLastFixedUpdate = isMoving;
    }

    public virtual void Initialize(Rigidbody rigidbody, RigidbodyConfig config = null)
    {
        rb = rigidbody ?? rb;

        if (config != null)
        {
            rigidbodyConfig = config;
            RigidbodyConfigurator.ConfigureRigidbody(rb, rigidbodyConfig);
        }

        rb.mass = mass;
    }

    public virtual bool IsBallMoving()
    {
        bool isLinearLow = rb.linearVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularLow = rb.angularVelocity.magnitude <= stopVelocityThreshold;

        bool isMoving = !(isLinearLow && isAngularLow);

        if (!isMoving && wasMovingLastFixedUpdate)
        {
            // fully stopped -> clear small residuals
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        return isMoving;
    }

    // Add similar kinematic support
    public virtual void ApplyForce(Vector3 impulse)
    {
        if (rb.isKinematic)
        {
            // Store and apply velocity manually (see pattern above)
        }
        else
        {
            rb.AddForce(impulse, ForceMode.Impulse);
        }
        //Debug.Log($"[EnemyBallBase] ApplyForce impulse={impulse}");
    }

    public virtual void ApplyForceWithCurve(Vector3 baseImpulse, Vector3 lateralDirection, float lateralIntensity)
    {
        rb.AddForce(baseImpulse, ForceMode.Impulse);

        if (lateralIntensity > 0.01f && lateralDirection != Vector3.zero)
        {
            StopAllCoroutines();
            StartCoroutine(ApplySimpleCurveCoroutine(lateralDirection.normalized, lateralIntensity));
        }

        //Debug.Log($"[EnemyBallBase] ApplyForceWithCurve base={baseImpulse} lateralDir={lateralDirection} intensity={lateralIntensity}");
    }

    private IEnumerator ApplySimpleCurveCoroutine(Vector3 lateralDir, float intensity)
    {
        float elapsed = 0f;
        float currentIntensity = intensity;

        while (elapsed < curveDuration && rb.linearVelocity.magnitude > stopVelocityThreshold)
        {
            // linear falloff of intensity
            float t = 1f - (elapsed / curveDuration);
            Vector3 lateralForce = lateralDir * (currentIntensity * curvePullForce * t) * Time.fixedDeltaTime;
            rb.AddForce(lateralForce, ForceMode.Force);

            // debug
            Debug.DrawRay(rb.position, lateralForce * 10f, Color.magenta, 0.1f);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody == null) return;

        var playerBall = collision.rigidbody.GetComponent<BilliardBall>();
        if (playerBall != null)
        {
            //OnHitByPlayerBall(playerBall, collision);
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Only process if this object has the designated enemy tag
        if (!gameObject.CompareTag(enemyTag)) return;

        // If the trigger is a pocket (either by component or by tag), destroy this enemy
        if (other.GetComponent<Pockets>() != null || other.CompareTag("Pocket"))
        {
            //Debug.Log($"[EnemyBallBase] Enemy '{name}' fell into pocket and will be destroyed.");
            Destroy(gameObject);
        }
    }
    //protected virtual void OnHitByPlayerBall(BilliardBall playerBall, Collision collision)
    //{
    //    Debug.Log($"[EnemyBallBase] Hit by player ball: {name}. Collision impulse approx: {collision.impulse.magnitude:F3}");
    //}
}
