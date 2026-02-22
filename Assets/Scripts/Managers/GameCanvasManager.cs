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

    [Header("End Scene Panel")] // optional - it will look for the buttons if not applied manually 
    [Tooltip("If not assigned the script will try to find 'EndScenePanel' or 'EndScene' in the scene.")]
    public GameObject endScenePanel;
    [Tooltip("If not assigned the script will try to find 'RestartButton' child under EndScenePanel.")]
    public Button endRestartButton;
    [Tooltip("If not assigned the script will try to find 'QuitButton' child under EndScenePanel.")]
    public Button endQuitButton;

    private bool isInitialized = false;

    void Start()
    {
        SetupButtons();
        SetupEndSceneButtons();
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

    // Try to wire the EndScene panel buttons. Safe if fields already assigned.
    private void SetupEndSceneButtons()
    {
        // Resolve panel if needed
        if (endScenePanel == null)
        {
            var found = GameObject.Find("EndScenePanel");
            if (found == null)
                found = GameObject.Find("EndScene");
            if (found != null)
                endScenePanel = found;
        }

        if (endScenePanel != null)
        {
            // Try to resolve Restart button
            if (endRestartButton == null)
            {
                var restartTf = endScenePanel.transform.Find("RestartButton");
                if (restartTf != null)
                    endRestartButton = restartTf.GetComponent<Button>();
                else
                {
                    var go = GameObject.Find("RestartButton");
                    if (go != null) endRestartButton = go.GetComponent<Button>();
                }
            }

            // Try to resolve Quit button
            if (endQuitButton == null)
            {
                var quitTf = endScenePanel.transform.Find("QuitButton");
                if (quitTf != null)
                    endQuitButton = quitTf.GetComponent<Button>();
                else
                {
                    var go = GameObject.Find("QuitButton");
                    if (go != null) endQuitButton = go.GetComponent<Button>();
                }
            }

            // Wire listeners (use existing methods)
            if (endRestartButton != null)
            {
                endRestartButton.onClick.RemoveAllListeners();
                endRestartButton.onClick.AddListener(RestartGame);
            }

            if (endQuitButton != null)
            {
                endQuitButton.onClick.RemoveAllListeners();
                endQuitButton.onClick.AddListener(ReturnToMenu);
            }
        }
    }

    // Public method used by GameManager to show the end scene UI.
    public void ShowEndScene(bool isWin)
    {
        // Ensure we have the panel and the buttons wired.
        if (endScenePanel == null)
        {
            var found = GameObject.Find("EndScenePanel");
            if (found == null) found = GameObject.Find("EndScene");
            if (found != null) endScenePanel = found;
        }

        SetupEndSceneButtons();

        if (endScenePanel == null) return;

        // Toggle titles (match names used in prefab)
        var lossTitle = endScenePanel.transform.Find("LossTitle");
        var winTitle = endScenePanel.transform.Find("WinTitle");
        if (lossTitle != null) lossTitle.gameObject.SetActive(!isWin);
        if (winTitle != null) winTitle.gameObject.SetActive(isWin);

        endScenePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Public hide method (optional)
    public void HideEndScene()
    {
        if (endScenePanel != null)
            endScenePanel.SetActive(false);
        Time.timeScale = 1f;
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
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Also ensure EndScene panel hidden initially if present
        if (endScenePanel != null)
            endScenePanel.SetActive(false);
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
        }
    }

    private void ResumeGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        
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

        }
    }

    private void UpdateShots(int shots)
    {
        if (shotsText != null)
        {
            shotsText.SetText("Shots: {0}", shots);
            shotsText.ForceMeshUpdate();

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
