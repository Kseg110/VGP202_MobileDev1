using UnityEngine;
using UnityEngine.SceneManagement;

public class Projection : PhysicsMaterialManager
{
    [SerializeField] private Transform _obstaclesParent;
    [SerializeField] private LineRenderer _line;
    [SerializeField] private int _maxPhysicsFrameIterations;
    
    private Scene _simulationScene;
    private PhysicsScene _physicsScene;

    protected override void Start()
    {
        base.Start(); // Creates physics materials
        CreatePhysicsScene();
    }

    void CreatePhysicsScene()
    {
        _simulationScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        _physicsScene = _simulationScene.GetPhysicsScene();

        foreach (Transform obj in _obstaclesParent)
        {
            var ghostObj = Instantiate(obj.gameObject, obj.position, obj.rotation);
            ghostObj.GetComponent<Renderer>().enabled = false;
            
            Collider wallCollider = ghostObj.GetComponent<Collider>();
            if (wallCollider != null)
            {
                wallCollider.material = GetWallMaterial();
            }
            
            SceneManager.MoveGameObjectToScene(ghostObj, _simulationScene);
        }    
    }

        public void SimulateTrajectory(BilliardController PlayerCueBall, Vector2 pos, Vector2 velocity)
    {
        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        ghostObj.GetComponent<Renderer>().enabled = false;
        
        // Apply physics material
        Collider ballCollider = ghostObj.GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = GetBallMaterial();
        }

        // Configure rigidbody
        Rigidbody ghostRb = ghostObj.GetComponent<Rigidbody>();
        if (ghostRb != null)
        {
            RigidbodyConfigurator.ConfigureBilliardBall(ghostRb);
        }

        SceneManager.MoveGameObjectToScene(ghostObj.gameObject, _simulationScene);
        ghostObj.Shoot(velocity);

        _line.positionCount = _maxPhysicsFrameIterations;

        for (int i = 0; i < _maxPhysicsFrameIterations; i++)
        {
            _physicsScene.Simulate(Time.fixedDeltaTime);
            _line.SetPosition(i, ghostObj.transform.position);
            
            if (ghostRb.linearVelocity.magnitude < 0.1f)
            {
                for (int j = i + 1; j < _maxPhysicsFrameIterations; j++)
                {
                    _line.SetPosition(j, ghostObj.transform.position);
                }
                break;
            }
        }

        DestroyImmediate(ghostObj.gameObject);
    }
}
