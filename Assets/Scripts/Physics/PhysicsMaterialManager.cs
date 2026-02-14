using UnityEngine;

public abstract class PhysicsMaterialManager : MonoBehaviour
{
    [Header("Physics Materials")]
    [SerializeField] protected PhysicsMaterial ballPhysicsMaterial;
    [SerializeField] protected PhysicsMaterial wallPhysicsMaterial;

    protected virtual void Start()
    {
        CreatePhysicsMaterials();
    }

    protected virtual void CreatePhysicsMaterials()
    {
        if (ballPhysicsMaterial == null)
        {
            ballPhysicsMaterial = CreateBallMaterial();
        }

        if (wallPhysicsMaterial == null)
        {
            wallPhysicsMaterial = CreateWallMaterial();
        }
    }

    protected virtual PhysicsMaterial CreateBallMaterial()
    {
        var material = new PhysicsMaterial("BallMaterial");
        material.bounciness = 0.8f;
        material.dynamicFriction = 0.1f;
        material.staticFriction = 0.1f;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        material.bounceCombine = PhysicsMaterialCombine.Maximum;
        return material;
    }

    protected virtual PhysicsMaterial CreateWallMaterial()
    {
        var material = new PhysicsMaterial("WallMaterial");
        material.bounciness = 0.8f;
        material.dynamicFriction = 0.1f;
        material.staticFriction = 0.1f;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        material.bounceCombine = PhysicsMaterialCombine.Maximum;
        return material;
    }

    public PhysicsMaterial GetBallMaterial() => ballPhysicsMaterial;
    public PhysicsMaterial GetWallMaterial() => wallPhysicsMaterial;
}