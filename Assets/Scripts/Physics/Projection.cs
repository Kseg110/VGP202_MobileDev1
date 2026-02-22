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

        // Ensure a trajectory line exists so simulations don't throw if _line wasn't assigned in inspector
        if (_line == null)
        {
            Debug.LogWarning("[Projection] _line was not assigned in the inspector. Creating a fallback trajectory LineRenderer.");
            GameObject lineObj = new GameObject("TrajectoryLine");
            _line = lineObj.AddComponent<LineRenderer>();
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startWidth = 0.03f;
            _line.endWidth = 0.03f;
            _line.startColor = Color.white;
            _line.endColor = Color.white;
            _line.enabled = false;
        }
    }

    void CreatePhysicsScene()
    {
        _simulationScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        _physicsScene = _simulationScene.GetPhysicsScene();

        if (_obstaclesParent == null)
        {
            Debug.LogWarning("[Projection] _obstaclesParent has not been assigned. No obstacle ghosts will be created in the simulation scene.");
            return;
        }

        foreach (Transform obj in _obstaclesParent)
        {
            var ghostObj = Instantiate(obj.gameObject, obj.position, obj.rotation);
            var renderer = ghostObj.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
            
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
        if (_simulationScene == null || !_physicsScene.IsValid())
        {
            Debug.LogWarning("[Projection] Simulation scene is not available. Call CreatePhysicsScene() first.");
            return;
        }

        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        var ghostRenderer = ghostObj.GetComponent<Renderer>();
        if (ghostRenderer != null) ghostRenderer.enabled = false;
        
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

        if (_line == null)
        {
            Debug.LogWarning("[Projection] Trajectory LineRenderer is null; skipping visualization.");
            DestroyImmediate(ghostObj.gameObject);
            return;
        }

        _line.positionCount = _maxPhysicsFrameIterations;

        for (int i = 0; i < _maxPhysicsFrameIterations; i++)
        {
            _physicsScene.Simulate(Time.fixedDeltaTime);
            _line.SetPosition(i, ghostObj.transform.position);
            
            if (ghostRb != null && ghostRb.linearVelocity.magnitude < 0.1f)
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
        if (_simulationScene == null || !_physicsScene.IsValid())
        {
            Debug.LogWarning("[Projection] Simulation scene is not available. Call CreatePhysicsScene() first.");
            return;
        }

        Vector3 position3D = new Vector3(pos.x, PlayerCueBall.transform.position.y, pos.y);
        
        var ghostObj = Instantiate(PlayerCueBall, position3D, Quaternion.identity);
        var ghostRenderer = ghostObj.GetComponent<Renderer>();
        if (ghostRenderer != null) ghostRenderer.enabled = false;
        
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
        
        // Apply curved force to the ghost ball using new system
        BilliardBall ghostBilliardBall = ghostObj.GetComponent<BilliardBall>(); // Changed from BallMovement
        if (ghostBilliardBall != null)
        {
            // Use a short impulse + curve pull so the ghost behaves like real shot
            ghostBilliardBall.ApplyForceWithCurve(curvedVelocity, Vector3.Cross(curvedVelocity.normalized, Vector3.forward), curveIntensity);
        }
        else if (ghostRb != null)
        {
            ghostRb.AddForce(curvedVelocity, ForceMode.Impulse);
        }

        if (_line == null)
        {
            Debug.LogWarning("[Projection] Trajectory LineRenderer is null; skipping visualization.");
            DestroyImmediate(ghostObj.gameObject);
            return;
        }

        _line.positionCount = _maxPhysicsFrameIterations;

        for (int i = 0; i < _maxPhysicsFrameIterations; i++)
        {
            _physicsScene.Simulate(Time.fixedDeltaTime);
            _line.SetPosition(i, ghostObj.transform.position);
            
            if (ghostRb != null && ghostRb.linearVelocity.magnitude < 0.1f)
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
