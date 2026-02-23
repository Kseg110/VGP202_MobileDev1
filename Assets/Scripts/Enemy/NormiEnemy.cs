using UnityEngine;

/// Basic enemy ball type.
/// Inherits movement/physics behavior from EnemyBallBase.

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class NormiEnemy : EnemyBallBase
{
    [Header("Normi Settings")]
    //[SerializeField] private Color idleColor = Color.cyan;
    [SerializeField] private Color hitColor = Color.red;
    //[SerializeField] private float hitFlashDuration = 0.2f;

    private Renderer rend;
    private float hitFlashTimer;

    protected override void Awake()
    {
        base.Awake();

        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            //rend.material.color = idleColor;
        }
    }

    protected override void Start()
    {
        base.Start();
        // additional init for Normi if needed
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // simple hit flash decay
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.fixedDeltaTime;
            if (hitFlashTimer <= 0f && rend != null)
            {
                //rend.material.color = idleColor;
            }
        }
    }
    /*
    protected override void OnHitByPlayerBall(BilliardBall playerBall, Collision collision)
    {
        base.OnHitByPlayerBall(playerBall, collision);

        // Visual feedback: flash color
        if (rend != null)
        {
            rend.material.color = hitColor;
            hitFlashTimer = hitFlashDuration;
        }
    }*/
}
