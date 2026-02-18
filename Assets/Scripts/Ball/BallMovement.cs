using UnityEngine;

[System.Serializable]
public class BallMovement
{
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    [SerializeField] private float curvePullForce = 5.0f; // Increased from 2.0f
    
    private Rigidbody rb;
    private MonoBehaviour owner;
    private Vector3 pullDirection;
    private float pullIntensity;

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
        // Apply the original straight force first
        rb.AddForce(baseForce, ForceMode.Impulse);
        
        Debug.Log($"CURVE SHOT - BaseForce: {baseForce}, CurveIntensity: {curveIntensity}");
        
        // Set up the pulling force for the curve effect
        if (owner != null && Mathf.Abs(curveIntensity) > 0.1f)
        {
            // Calculate the pull direction based on curve intensity
            Vector3 perpendicularToMovement = Vector3.Cross(baseForce.normalized, Vector3.forward).normalized;
            
            // For right curve (positive intensity), we want to pull left
            // For left curve (negative intensity), we want to pull right
            pullDirection = perpendicularToMovement * Mathf.Sign(curveIntensity);
            pullIntensity = Mathf.Abs(curveIntensity);
            
            Debug.Log($"CURVE SETUP - Pull Direction: {pullDirection}, Pull Intensity: {pullIntensity}");
            
            owner.StartCoroutine(ApplyProjectileCurve());
        }
        else
        {
            Debug.Log("NO CURVE - Intensity too low or owner null");
        }
    }

    private System.Collections.IEnumerator ApplyProjectileCurve()
    {
        float timeElapsed = 5f;
        float maxCurveDuration = 1f; // Reduced duration for more intense effect
        float currentPullIntensity = pullIntensity;
        
        Debug.Log("STARTING PROJECTILE CURVE");
        
        while (rb.linearVelocity.magnitude > stopVelocityThreshold && 
               timeElapsed < maxCurveDuration && 
               currentPullIntensity > 0.1f)
        {
            // Calculate the current pull force
            float velocityFactor = rb.linearVelocity.magnitude / 10f; // Increased divisor for stronger effect
            float timeFactor = 1f - (timeElapsed / maxCurveDuration); // Decay over time
            
            // Apply the pulling force
            Vector3 pullForceVector = pullDirection * currentPullIntensity * curvePullForce * velocityFactor * timeFactor;
            rb.AddForce(pullForceVector, ForceMode.Force);
            
            // Enhanced debug visualization
            Debug.DrawRay(rb.position, pullForceVector * 5f, Color.red, 0.1f);
            Debug.DrawRay(rb.position, rb.linearVelocity.normalized * 3f, Color.blue, 0.1f);
            
            // Log force values occasionally
            if (Time.fixedTime % 0.2f < Time.fixedDeltaTime)
            {
                Debug.Log($"CURVE FORCE: {pullForceVector.magnitude:F2}, Velocity: {rb.linearVelocity.magnitude:F2}");
            }
            
            // Gradually reduce the pull intensity
            currentPullIntensity *= 0.96f; // Slower decay for longer effect
            timeElapsed += Time.fixedDeltaTime;
            
            yield return new WaitForFixedUpdate();
        }
        
        Debug.Log("PROJECTILE CURVE ENDED");
    }
}