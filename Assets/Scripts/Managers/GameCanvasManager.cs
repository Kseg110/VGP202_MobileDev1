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

    void Start()
    {
        SetupButtons();
        InitializeHUD();
        Debug.Log("[GameCanvasManager] Start completed.");
    }

    private void SetupButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
            Debug.Log("[GameCanvasManager] PauseButton listener added");
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("[GameCanvasManager] ResumeButton listener added");
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
            Debug.Log("[GameCanvasManager] ReturnToMenuButton listener added");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("[GameCanvasManager] RestartButton listener added");
        }
    }

    private void InitializeHUD()
    {
        if (GameManager.Instance != null)
        {
            UpdateLives(GameManager.Instance.lives);
            GameManager.Instance.OnLivesChanged -= UpdateLives;
            GameManager.Instance.OnLivesChanged += UpdateLives;
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    void Update()
    {
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
        SceneManager.LoadScene(0);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameCanvasManager] Restarting Game");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLoadFromCheckpoint(false);
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {lives}";
        }
    }

    public void UpdateShots(int shots)
    {
        if (shotsText != null)
        {
            shotsText.text = $"Shots: {shots}";
        }
    }

    public void UpdateLevel(int level)
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
        }
    }
}
