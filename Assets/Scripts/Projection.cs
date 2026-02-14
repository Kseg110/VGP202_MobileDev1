using UnityEngine;
using UnityEngine.SceneManagement;

public class Projection : PhysicsMaterialManager
{
    [SerializeField] private Transform _obstaclesParent;
    [SerializeField] private LineRenderer _line;
    [SerializeField] private int _maxPhysicsFrameIterations;
    [SerializeField] private LineRenderer _curvePreviewLine;
    
    private Scene _simulationScene;
    private PhysicsScene _physicsScene;

    protected override void Start()
    {
        base.Start();
        CreatePhysicsScene();
        SetupCurvePreviewLine();
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

    private void SetupCurvePreviewLine()
    {
        if (_curvePreviewLine == null)
        {
            GameObject curveLineObj = new GameObject("CurvePreviewLine");
            _curvePreviewLine = curveLineObj.AddComponent<LineRenderer>();
        }
        
        _curvePreviewLine.positionCount = 50;
        _curvePreviewLine.startWidth = 0.03f;
        _curvePreviewLine.endWidth = 0.03f;
        _curvePreviewLine.material = new Material(Shader.Find("Sprites/Default"));
        _curvePreviewLine.startColor = Color.yellow;
        _curvePreviewLine.endColor = Color.orange;
        _curvePreviewLine.enabled = false;
    }

    public void SimulateTrajectory(BilliardController PlayerCueBall, Vector2 pos, Vector2 velocity)
    {
        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        ghostObj.GetComponent<Renderer>().enabled = false;
        
        Collider ballCollider = ghostObj.GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = GetBallMaterial();
        }

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

    public void SimulateCurvedTrajectory(BilliardController PlayerCueBall, Vector2 pos, Vector3 curvedVelocity, float curveIntensity)
    {
        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        ghostObj.GetComponent<Renderer>().enabled = false;
        
        Collider ballCollider = ghostObj.GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = GetBallMaterial();
        }

        Rigidbody ghostRb = ghostObj.GetComponent<Rigidbody>();
        if (ghostRb != null)
        {
            RigidbodyConfigurator.ConfigureBilliardBall(ghostRb);
        }

        SceneManager.MoveGameObjectToScene(ghostObj.gameObject, _simulationScene);
        
        // Apply curved force to the ghost ball
        BallMovement ghostBallMovement = ghostObj.GetComponent<BallMovement>();
        if (ghostBallMovement != null)
        {
            ghostBallMovement.ApplyForceWithCurve(curvedVelocity, curveIntensity);
        }
        else
        {
            ghostRb.AddForce(curvedVelocity, ForceMode.Impulse);
        }

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

    public void ShowCurvePreview(Vector3 startPos, Vector3 velocity, float curveIntensity)
    {
        if (Mathf.Abs(curveIntensity) < 0.1f)
        {
            _curvePreviewLine.enabled = false;
            return;
        }

        _curvePreviewLine.enabled = true;
        
        // Calculate curved trajectory preview using physics-based simulation
        for (int i = 0; i < _curvePreviewLine.positionCount; i++)
        {
            float t = i / (float)(_curvePreviewLine.positionCount - 1);
            Vector3 position = CalculateCurvedPosition(startPos, velocity, curveIntensity, t);
            _curvePreviewLine.SetPosition(i, position);
        }
    }

    private Vector3 CalculateCurvedPosition(Vector3 startPos, Vector3 velocity, float curveIntensity, float t)
    {
        float maxTime = 3.0f; // Maximum preview time
        float currentTime = t * maxTime;
        
        // Base position with velocity and drag
        Vector3 basePos = startPos + velocity * currentTime * (1 - currentTime * 0.3f);
        
        // Add curve effect (Magnus-like force) - more realistic curve
        Vector3 perpendicular = Vector3.Cross(velocity.normalized, Vector3.forward).normalized;
        float curveDecay = Mathf.Exp(-currentTime * 2f); // Exponential decay
        float curveEffect = curveIntensity * currentTime * currentTime * 0.3f * curveDecay;
        
        return basePos + perpendicular * curveEffect;
    }

    public void HideCurvePreview()
    {
        if (_curvePreviewLine != null)
            _curvePreviewLine.enabled = false;
    }
}
