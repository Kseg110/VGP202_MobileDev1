using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class Projection : MonoBehaviour
{
    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial ballPhysicsMaterial;
    [SerializeField] private PhysicsMaterial wallPhysicsMaterial;

    void Start()
    {
        CreatePhysicsScene();
        CreatePhysicsMaterials();
    }

    private Scene _simulationScene;
    private PhysicsScene _physicsScene;
    [SerializeField] private Transform _obstaclesParent;

    void CreatePhysicsScene()
    {
        _simulationScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        _physicsScene = _simulationScene.GetPhysicsScene();

        foreach (Transform obj in _obstaclesParent)
        {
            var ghostObj = Instantiate(obj.gameObject, obj.position, obj.rotation);
            ghostObj.GetComponent<Renderer>().enabled = false;
            
            // Apply physics material to wall colliders
            Collider wallCollider = ghostObj.GetComponent<Collider>();
            if (wallCollider != null && wallPhysicsMaterial != null)
            {
                wallCollider.material = wallPhysicsMaterial;
            }
            
            SceneManager.MoveGameObjectToScene(ghostObj, _simulationScene);
        }    
    }

    private void CreatePhysicsMaterials()
    {
        // Create bouncy ball material if not assigned
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = new PhysicsMaterial("BallMaterial");
            ballPhysicsMaterial.bounciness = 0.8f;      // High bounce
            ballPhysicsMaterial.dynamicFriction = 0.1f;  // Low rolling friction
            ballPhysicsMaterial.staticFriction = 0.1f;   // Low static friction
            ballPhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            ballPhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        }

        // Create wall material if not assigned
        if (wallPhysicsMaterial == null)
        {
            wallPhysicsMaterial = new PhysicsMaterial("WallMaterial");
            wallPhysicsMaterial.bounciness = 0.8f;       // High bounce
            wallPhysicsMaterial.dynamicFriction = 0.1f;  // Low friction
            wallPhysicsMaterial.staticFriction = 0.1f;   // Low friction
            wallPhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            wallPhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
        }
    }

    [SerializeField] private LineRenderer _line;
    [SerializeField] private int _maxPhysicsFrameIterations;
    
    public void SimulateTrajectory(BilliardController PlayerCueBall, Vector2 pos, Vector2 velocity)
    {
        // Convert Vector2 pos to Vector3 for Instantiate
        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        ghostObj.GetComponent<Renderer>().enabled = false;
        
        // Apply physics material to the ball
        Collider ballCollider = ghostObj.GetComponent<Collider>();
        if (ballCollider != null && ballPhysicsMaterial != null)
        {
            ballCollider.material = ballPhysicsMaterial;
        }

        // Configure rigidbody for proper bouncing
        Rigidbody ghostRb = ghostObj.GetComponent<Rigidbody>();
        if (ghostRb != null)
        {
            ghostRb.useGravity = false;
            ghostRb.linearDamping = 1f;   // Match your BilliardController settings
            ghostRb.angularDamping = 2f;  // Match your BilliardController settings
            ghostRb.constraints = RigidbodyConstraints.FreezePositionZ | 
                                 RigidbodyConstraints.FreezeRotationX | 
                                 RigidbodyConstraints.FreezeRotationY;
        }

        SceneManager.MoveGameObjectToScene(ghostObj.gameObject, _simulationScene);

        ghostObj.Shoot(velocity);

        _line.positionCount = _maxPhysicsFrameIterations;

        for (int i = 0; i < _maxPhysicsFrameIterations; i++)
        {
            _physicsScene.Simulate(Time.fixedDeltaTime);
            _line.SetPosition(i, ghostObj.transform.position);
            
            // Optional: Break early if ball stops moving
            if (ghostRb.velocity.magnitude < 0.1f)
            {
                // Fill remaining positions with the final position
                for (int j = i + 1; j < _maxPhysicsFrameIterations; j++)
                {
                    _line.SetPosition(j, ghostObj.transform.position);
                }
                break;
            }
        }

        // Clean up the ghost object
        DestroyImmediate(ghostObj.gameObject);
    }
}
