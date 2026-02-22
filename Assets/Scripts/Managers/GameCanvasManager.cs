using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class GameCanvasManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public GameObject hudPanel;
    public TMP_Text livesText;
    public TMP_Text shotsText;
    public TMP_Text levelText;
    public TMP_Text coinsText;

    [Header("Game Buttons")]
    public Button pauseButton;
    public Button shootButton;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button returnToMenuButton;
    public Button restartButton;

    private bool isInitialized = false;

    void Start()
    {
        SetupButtons();
        // Delay initialization to ensure GameManager is ready
        Invoke(nameof(InitializeHUD), 0.1f);
    }

    void Update()
    {
        // Try to initialize if not yet done
        if (!isInitialized && GameManager.Instance != null)
        {
            InitializeHUD();
        }
        
        // Right-click or ESC to pause
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            TogglePause();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void SetupButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void InitializeHUD()
    {
        if (isInitialized) return;
        
        if (GameManager.Instance != null)
        {
            // Initialize all HUD elements with current values
            UpdateLives(GameManager.Instance.Lives);
            UpdateShots(GameManager.Instance.Shots);
            UpdateLevel(GameManager.Instance.Rounds);
            
            // Unsubscribe first to prevent duplicates
            GameManager.Instance.OnLivesChanged -= UpdateLives;
            GameManager.Instance.OnShotsChanged -= UpdateShots;
            GameManager.Instance.OnRoundsChanged -= UpdateLevel;
            
            // Subscribe to all events
            GameManager.Instance.OnLivesChanged += UpdateLives;
            GameManager.Instance.OnShotsChanged += UpdateShots;
            GameManager.Instance.OnRoundsChanged += UpdateLevel;
            
            isInitialized = true;
            Debug.Log($"[GameCanvasManager] HUD Initialized - Lives: {GameManager.Instance.Lives}, Shots: {GameManager.Instance.Shots}, Level: {GameManager.Instance.Rounds}");
        }
        else
        {
            Debug.LogWarning("[GameCanvasManager] GameManager instance not found! Will retry...");
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    public void TogglePause()
    {
        if (pauseMenuPanel == null) return;

        if (pauseMenuPanel.activeSelf)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("[GameCanvasManager] Game Paused");
        }
    }

    private void ResumeGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f;
            Debug.Log("[GameCanvasManager] Game Resumed");
        }
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameCanvasManager] Returning to Menu");
        
        // Reset GameManager values for next game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Lives = GameManager.Instance.maxLives;
            GameManager.Instance.Shots = 10;
            GameManager.Instance.Rounds = 0;
        }
        
        SceneManager.LoadScene(0);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameCanvasManager] Restarting Game");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLoadFromCheckpoint(false);
            
            // Reset stats
            GameManager.Instance.Lives = GameManager.Instance.maxLives;
            GameManager.Instance.Shots = 10;
            GameManager.Instance.Rounds = 0;
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            // Use SetText to avoid allocations and ForceMeshUpdate to ensure TMP rebuilds the mesh immediately.
            livesText.SetText("Lives: {0}", lives);
            livesText.ForceMeshUpdate();

            Debug.Log($"[GameCanvasManager] Lives updated to: {lives} -> written to '{livesText.gameObject.name}', Font='{livesText.font?.name}', Text='{livesText.text}'");
        }
    }

    private void UpdateShots(int shots)
    {
        if (shotsText != null)
        {
            shotsText.SetText("Shots: {0}", shots);
            shotsText.ForceMeshUpdate();

            Debug.Log($"[GameCanvasManager] Shots updated to: {shots} -> written to '{shotsText.gameObject.name}', Font='{shotsText.font?.name}', Text='{shotsText.text}'");
        }
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level: {level}";
        }
    }

    public void UpdateCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {coins}";
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= UpdateLives;
            GameManager.Instance.OnShotsChanged -= UpdateShots;
            GameManager.Instance.OnRoundsChanged -= UpdateLevel;
        }
    }
}
