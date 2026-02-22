using UnityEngine;

public class Pockets : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float respawnDelay = 0.5f;
    
    private Vector3 lastStationaryPosition;

    void Start()
    {
        // Subscribe to player spawn event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerControllerCreated += OnPlayerSpawned;
            
            // If player already exists, get initial position
            if (GameManager.Instance.PlayerInstance != null)
            {
                OnPlayerSpawned(GameManager.Instance.PlayerInstance);
            }
        }
        else
        {
            //Debug.LogError("[Pockets] GameManager instance not found!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerControllerCreated -= OnPlayerSpawned;
        }
    }

    private void OnPlayerSpawned(BilliardController player)
    {
        if (player != null)
        {
            lastStationaryPosition = player.transform.position;
            //Debug.Log($"[Pockets] Initial player position tracked: {lastStationaryPosition}");
        }
    }

    void Update()
    {
        // Track the last stationary position of the player ball
        if (GameManager.Instance != null && GameManager.Instance.PlayerInstance != null)
        {
            BilliardBall billiardBall = GameManager.Instance.PlayerInstance.GetComponent<BilliardBall>();
            if (billiardBall != null && !billiardBall.IsBallMoving())
            {
                lastStationaryPosition = GameManager.Instance.PlayerInstance.transform.position;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the pocket is the player
        BilliardController ball = other.GetComponent<BilliardController>();
        
        if (ball != null)
        {
            //Debug.Log($"[Pockets] Trigger detected! Object: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
            HandlePlayerFallInPocket(ball);
        }
        else
        {
            //Debug.Log($"[Pockets] Trigger detected but no BilliardController found on: {other.gameObject.name}");
        }
    }

    private void HandlePlayerFallInPocket(BilliardController ball)
    {
        // Subtract 1 life from player
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Lives -= 1;
            
            //Debug.Log($"[Pockets] Player fell in pocket! Lives remaining: {GameManager.Instance.Lives}");
            
            // Check if player has lives remaining
            if (GameManager.Instance.Lives > 0)
            {
                // Respawn player at last stationary position
                StartCoroutine(RespawnPlayer());
            }
            else
            {
                // Player has no lives left - trigger game over
                HandleGameOver();
            }
        }
        else
        {
            //Debug.LogError("[Pockets] GameManager instance not found!");
        }
    }

    private System.Collections.IEnumerator RespawnPlayer()
    {
        // Temporarily disable player
        if (GameManager.Instance.PlayerInstance != null)
        {
            GameManager.Instance.PlayerInstance.gameObject.SetActive(false);
        }
        
        // Wait for respawn delay
        yield return new WaitForSeconds(respawnDelay);
        
        // Use GameManager to respawn
        GameManager.Instance.RespawnPlayer(lastStationaryPosition);
        
        // Re-enable player
        if (GameManager.Instance.PlayerInstance != null)
        {
            GameManager.Instance.PlayerInstance.gameObject.SetActive(true);
        }
        
        //Debug.Log($"[Pockets] Player respawned at: {lastStationaryPosition}");
    }

    private void HandleGameOver()
    {
        //Debug.Log("[Pockets] Game Over - No lives remaining!");
        
        // GameManager to handle player death
        GameManager.Instance.PlayerDied();
    }
}
