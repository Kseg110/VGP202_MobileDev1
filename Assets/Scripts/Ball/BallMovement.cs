using UnityEngine;

[System.Serializable]
public class BallMovement
{
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    
    private Rigidbody rb;
    private MonoBehaviour owner; // Reference to the owning MonoBehaviour

    public void Initialize(Rigidbody rigidbody, MonoBehaviour ownerMonoBehaviour = null)
    {
        rb = rigidbody;
        owner = ownerMonoBehaviour;
    }

    public bool IsBallMoving()
    {
        bool isLinearVelocityLow = rb.linearVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularVelocityLow = rb.angularVelocity.magnitude <= stopVelocityThreshold;
        
        if (isLinearVelocityLow && isAngularVelocityLow)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return false;
        }
        
        return true;
    }

    public void ApplyForce(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        Debug.Log($"Applied straight force: {force}");
    }

    public void ApplyForceWithCurve(Vector3 baseForce, float curveIntensity)
    {
        // Apply the base force first
        rb.AddForce(baseForce, ForceMode.Impulse);
        
        Debug.Log($"Applied curved shot: BaseForce={baseForce}, CurveIntensity={curveIntensity}");
        
        // Start continuous curve force application if owner is available
        if (owner != null && Mathf.Abs(curveIntensity) > 0.1f)
        {
            owner.StartCoroutine(ApplyContinuousCurve(curveIntensity));
        }
    }

    private System.Collections.IEnumerator ApplyContinuousCurve(float initialCurveIntensity)
    {
        float currentCurveIntensity = initialCurveIntensity;
        float timeElapsed = 0f;
        float curveDuration = 1.5f; // How long the curve effect lasts
        
        while (rb.linearVelocity.magnitude > stopVelocityThreshold && 
               Mathf.Abs(currentCurveIntensity) > 0.1f && 
               timeElapsed < curveDuration)
        {
            // Calculate perpendicular force based on current velocity (in XY plane)
            Vector3 velocity = rb.linearVelocity;
            Vector3 perpendicularDirection = Vector3.Cross(velocity.normalized, Vector3.forward).normalized;
            
            // Apply continuous curve force (Magnus effect)
            float curveForce = currentCurveIntensity * velocity.magnitude * 0.08f; // Adjusted multiplier
            Vector3 curveVector = perpendicularDirection * curveForce;
            
            rb.AddForce(curveVector, ForceMode.Force);
            
            // Gradually reduce curve intensity over time
            currentCurveIntensity *= 0.95f; // Slower decay for more pronounced curve
            timeElapsed += Time.fixedDeltaTime;
            
            yield return new WaitForFixedUpdate();
        }
        
        Debug.Log("Curve force application ended");
    }
}