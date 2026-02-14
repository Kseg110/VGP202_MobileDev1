using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class BilliardController : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float maxPower = 20f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial ballPhysicsMaterial;

    [Header("Visuals")]
    [SerializeField] private Transform arrowIndicator; 
    [SerializeField] private float lineLength = 2.0f;
    [SerializeField] private Color minPowerColor = Color.white;
    [SerializeField] private Color maxPowerColor = Color.red;

    [Header("UI")]
    [SerializeField] private RadialPowerBar powerBar; 

    private Rigidbody rb;
    private LineRenderer aimLine;
    private Camera mainCam;

    private bool isCharging;
    private float currentPower;
    private Vector3 aimDirection;
    private bool isBallMoving;

    private enum ShootState { Idle, Ready, Charging }
    private ShootState shootState = ShootState.Idle;
    private float currentAimLineLength;

    public float PowerPercentage => currentPower / maxPower;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aimLine = GetComponent<LineRenderer>();
        mainCam = Camera.main;

        // DISABLE GRAVITY 
        rb.useGravity = false;

        // Add drag for friction simulation
        rb.linearDamping = 1f; // Linear damping
        rb.angularDamping = 2f; // Angular damping
        
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        // Apply physics material
        ApplyPhysicsMaterial();

        aimLine.positionCount = 2;
        aimLine.enabled = false;
        aimLine.startWidth = 0.05f;
        aimLine.endWidth = 0.05f;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = Color.red;
        aimLine.endColor = Color.red;
    }
    
    void Update()
    {
        CheckMovement();

        // disable aiming while ball is rolling/ in motion.
        if (isBallMoving)
        {
            aimLine.enabled = false;
            arrowIndicator.gameObject.SetActive(false);
            return;
        }
        HandleAiming();
        HandleShooting();
        UpdateVisuals();
    }

    private void CheckMovement()
    {
        // Check both linear and angular velocity - this was the main issue!
        bool isLinearVelocityLow = rb.linearVelocity.magnitude <= stopVelocityThreshold;
        bool isAngularVelocityLow = rb.angularVelocity.magnitude <= stopVelocityThreshold;
        
        // Ball is considered stopped when BOTH velocities are below threshold
        if (isLinearVelocityLow && isAngularVelocityLow)
        {
            // Force complete stop to prevent infinite sliding
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isBallMoving = false;
        }
        else
        {
            isBallMoving = true;
        }
    }

    private void HandleAiming()
    {
        Ray camRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane playerPlane = new Plane(Vector3.forward, transform.position); // <-- XY plane
        float hitDistance;

        if (playerPlane.Raycast(camRay, out hitDistance))
        {
            Vector3 mouseWorldPos = camRay.GetPoint(hitDistance);
            Vector3 direction = (mouseWorldPos - transform.position).normalized;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, Mathf.Infinity, groundLayer))
            {
                if (hit.collider.CompareTag("Obstacle"))
                {
                    aimDirection = (hit.point - transform.position).normalized;
                    currentAimLineLength = hit.distance;
                    Debug.DrawLine(transform.position, hit.point, Color.green, 0.1f);
                }
                else
                {
                    aimDirection = direction;
                    currentAimLineLength = lineLength;
                    Debug.DrawLine(transform.position, mouseWorldPos, Color.magenta, 0.1f);
                }
            }
            else
            {
                aimDirection = direction;
                currentAimLineLength = lineLength;
                Debug.DrawLine(transform.position, mouseWorldPos, Color.magenta, 0.1f);
            }
            
            aimDirection.z = 0f;
            aimDirection = aimDirection.normalized;
            
            Debug.Log("Aim direction set: " + aimDirection);
        }
        else
        {
            Debug.LogWarning("Plane raycast did not hit");
            aimDirection = Vector3.right; // fallback
            currentAimLineLength = lineLength;
        }
    }

    private void HandleShooting()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !isCharging)
        {
            isCharging = true;
            currentPower = 0f;
            shootState = ShootState.Charging; // This is good
            
            if (powerBar != null) powerBar.SetActive(true);
        }

        // charging up / increasing power / velocity
        if(Mouse.current.leftButton.isPressed && isCharging)
        {
            //increase power over time, stops at max power
            currentPower += chargeSpeed * Time.deltaTime;
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);

            //update radial power bar 
            if (powerBar != null) powerBar.UpdatePower(PowerPercentage);
        }

        //shoot on button release - FIXED: changed from wasPressedThisFrame to wasReleasedThisFrame
        if (Mouse.current.leftButton.wasReleasedThisFrame && isCharging)
        {
            Shoot();
            isCharging = false;
            shootState = ShootState.Idle; // This is good
            
            if (powerBar != null) powerBar.SetActive(false);
        }
    }   
    
    public void Shoot()
    {
        // Use the existing aimDirection and currentPower for mouse input
        Vector3 force = aimDirection * currentPower;
        rb.AddForce(force, ForceMode.Impulse);

        // Hide visuals - vector after shooting.
        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);

        //Reset power
        currentPower = 0f;
    }

    // Fix the Vector2 velocity overload
    public void Shoot(Vector2 velocity)
    {
        Vector3 force = new Vector3(velocity.x, velocity.y, 0);
        rb.AddForce(force, ForceMode.Impulse);

        // Hide visuals - vector after shooting.
        aimLine.enabled = false;
        arrowIndicator.gameObject.SetActive(false);
    }

    private void UpdateVisuals()
    {
        if (!isCharging && !Mouse.current.leftButton.isPressed)
        {
            //aiming idle state
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            // Use the dynamic aim line length
            DrawAimLine(currentAimLineLength);
            RotateArrow();

            // reset colors
            aimLine.startColor = minPowerColor;
            aimLine.endColor = minPowerColor;
        }
        else if (isCharging)
        {
            //charging state
            aimLine.enabled = true;
            arrowIndicator.gameObject.SetActive(true);

            //calculate percentages for visuals (0 to 1)
            float powerPercent = currentPower / maxPower;

            // Scale the line length with power, but cap at the collision point
            float scaledLength = Mathf.Min(currentAimLineLength, lineLength * (0.5f + powerPercent));
            DrawAimLine(scaledLength);
            RotateArrow();

            //color change based on power
            Color ChargeColor = Color.Lerp(minPowerColor, maxPowerColor, powerPercent);
            aimLine.startColor = ChargeColor;
            aimLine.endColor = ChargeColor;

            // scale arrow based on power
            arrowIndicator.localScale = Vector3.one * (1f + powerPercent * 0.5f);
        }
    }


    private void RotateArrow()
    {
        if (aimDirection != Vector3.zero)
        {
            //arrow facing aim direction
            Quaternion lookRot = Quaternion.LookRotation(aimDirection);
            arrowIndicator.rotation = lookRot;
        }
    }

    private void DrawAimLine(float length)
    {
        Vector3 start = transform.position + Vector3.up * 0.05f;
        Vector3 end = start + (aimDirection * length);
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
        Debug.Log(aimDirection);    
    }

    private void ApplyPhysicsMaterial()
    {
        // Create the same material as in Projection script if not assigned
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = new PhysicsMaterial("BallMaterial");
            ballPhysicsMaterial.bounciness = 0.8f;
            ballPhysicsMaterial.dynamicFriction = 0.1f;
            ballPhysicsMaterial.staticFriction = 0.1f;
            ballPhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            ballPhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        }

        // Apply to the ball's collider
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = ballPhysicsMaterial;
        }
    }
}
