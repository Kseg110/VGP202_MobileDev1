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

        // line renderer default setup 
        aimLine.positionCount = 2;
        aimLine.enabled = false;
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
        //raycast from camera to mouse to ground
        Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // ball to mouse hit hit point calculation.
            Vector3 mousePos = hit.point;

            // lock Y to balls Y to ensure a flat vector is drawn.
            mousePos.y = transform.position.y;

            //Get Normalized direction.
            aimDirection = (mousePos - transform.position).normalized;
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
        //line starting point at the ball center
        aimLine.SetPosition(0, transform.position);
        //end line aim
        aimLine.SetPosition(1, transform.position + (aimDirection * length));
    }
}
