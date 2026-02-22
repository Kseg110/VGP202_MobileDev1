using UnityEngine;

/// <summary>
/// Basic concrete enemy ball type. Attach this to simple enemy ball prefabs.
/// Inherits movement/physics behavior from EnemyBallBase and can be extended with AI/visuals.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class NormiEnemy : EnemyBallBase
{
    [Header("Normi Settings")]
    //[SerializeField] private Color idleColor = Color.cyan;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.2f;

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

    //protected override void OnHitByPlayerBall(BilliardBall playerBall, Collision collision)
    //{
    //    base.OnHitByPlayerBall(playerBall, collision);

    //    // Visual feedback: flash color
    //    if (rend != null)
    //    {
    //        rend.material.color = hitColor;
    //        hitFlashTimer = hitFlashDuration;
    //    }

    //    // Optional: react to hit impulse (e.g., play sound, spawn VFX)
    //    // You can also inspect collision.impulse to determine whether to "destroy" this enemy, deduct HP, etc.
    //}
}
