using UnityEngine;

[System.Serializable]
public class BallMovement
{
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    
    private Rigidbody rb;

    public void Initialize(Rigidbody rigidbody)
    {
        rb = rigidbody;
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
    }
}