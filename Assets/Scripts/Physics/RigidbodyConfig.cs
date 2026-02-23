using UnityEngine;

[System.Serializable]
public class RigidbodyConfig
{
    [Header("Physics Settings")]
    public bool useGravity = false;
    public bool isKinematic = false; // Add this
    public float linearDamping = 1f;
    public float angularDamping = 2f;
    public RigidbodyConstraints constraints = RigidbodyConstraints.FreezePositionZ | 
                                            RigidbodyConstraints.FreezeRotationX | 
                                            RigidbodyConstraints.FreezeRotationY;
}

public static class RigidbodyConfigurator
{
    public static void ConfigureRigidbody(Rigidbody rb, RigidbodyConfig config)
    {
        rb.useGravity = config.useGravity;
        rb.isKinematic = config.isKinematic; // Apply kinematic settings 
        rb.linearDamping = config.linearDamping;
        rb.angularDamping = config.angularDamping;
        rb.constraints = config.constraints;
    }
    
    public static void ConfigureBilliardBall(Rigidbody rb)
    {
        var config = new RigidbodyConfig();
        ConfigureRigidbody(rb, config);
    }
}