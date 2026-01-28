using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class BilliardController : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float maxPower = 20f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float stopVelocityThreshold = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visuals")]
    [SerializeField] private Transform arrowIndicator; // assign - input arrow child in inspector
    [SerializeField] private float lineLength = 2.0f;
    [SerializeField] private Color minPowerColor = Color.white;
    [SerializeField] private Color maxPowerColor = Color.red;

    private Rigidbody rb;
    private LineRenderer aimLine;
    private Camera mainCam;

    private bool isCharging;
    private float currentPower;
    private Vector3 aimDirection;
    private bool isBallMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        aimLine = GetComponent<LineRenderer>();
        mainCam = Camera.main;

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

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
        // check to see if ball is stopped.
        isBallMoving = rb.angularVelocity.magnitude > stopVelocityThreshold;
    }

    private void HandleAiming()
    {
        Ray camRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane playerPlane = new Plane(Vector3.forward, transform.position); // <-- XY plane!
        float hitDistance;

        if (playerPlane.Raycast(camRay, out hitDistance))
        {
            Vector3 mouseWorldPos = camRay.GetPoint(hitDistance);
            Vector3 direction = (mouseWorldPos - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, mouseWorldPos);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distance, groundLayer))
            {
                aimDirection = (hit.point - transform.position).normalized;
                Debug.DrawLine(transform.position, hit.point, Color.green, 0.1f);
            }
            else
            {
                aimDirection = direction;
                Debug.DrawLine(transform.position, mouseWorldPos, Color.magenta, 0.1f);
            }
            Debug.Log("Aim direction set: " + aimDirection);
        }
        else
        {
            Debug.LogWarning("Plane raycast did not hit");
            aimDirection = Vector3.right; // fallback for XY
        }
    }

    private void HandleShooting()
    {
        //start charging
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isCharging = true;
            currentPower = 0f;
        }

        // charging up
        if(Mouse.current.leftButton.isPressed && isCharging)
        {
            //increase power over time, stops at max power
            currentPower += chargeSpeed * Time.deltaTime;
            currentPower = Mathf.Clamp(currentPower, 0, maxPower);
        }

        // fire
        if (Mouse.current.leftButton.wasPressedThisFrame && isCharging)
        {
            Shoot();
            isCharging = false;

        }
    }   
    
    private void Shoot()
    {
        // Immediate force impulse.
        rb.AddForce(aimDirection * currentPower, ForceMode.Impulse);

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

            // default aiming visuals
            DrawAimLine(lineLength);
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

            // line length grows with strength
            DrawAimLine(lineLength * (0.5f + powerPercent));
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
}
