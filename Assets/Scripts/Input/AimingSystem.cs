using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class AimingSystem
{
    [Header("Aiming Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lineLength = 2.0f;
    
    private Camera mainCam;
    private Transform ballTransform;
    
    public Vector3 AimDirection { get; private set; }
    public float CurrentAimLineLength { get; private set; }

    public void Initialize(Camera camera, Transform ball)
    {
        mainCam = camera;
        ballTransform = ball;
    }

    public void UpdateAiming()
    {
        Ray camRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane playerPlane = new Plane(Vector3.forward, ballTransform.position);
        
        if (playerPlane.Raycast(camRay, out float hitDistance))
        {
            Vector3 mouseWorldPos = camRay.GetPoint(hitDistance);
            Vector3 direction = (mouseWorldPos - ballTransform.position).normalized;

            if (Physics.Raycast(ballTransform.position, direction, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                if (hit.collider.CompareTag("Obstacle"))
                {
                    AimDirection = (hit.point - ballTransform.position).normalized;
                    CurrentAimLineLength = hit.distance;
                }
                else
                {
                    AimDirection = direction;
                    CurrentAimLineLength = lineLength;
                }
            }
            else
            {
                AimDirection = direction;
                CurrentAimLineLength = lineLength;
            }
            
            AimDirection = new Vector3(AimDirection.x, AimDirection.y, 0f).normalized;
        }
        else
        {
            AimDirection = Vector3.right;
            CurrentAimLineLength = lineLength;
        }
    }
}