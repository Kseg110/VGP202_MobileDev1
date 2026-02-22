using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class CanvasManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button backButton;
    public Button quitButton;
    public Button pauseButton;

    public Button resumeGame;
    public Button returnToMenu;
    public Button endSceneRestartGameButton;
    public Button endSceneReturnToMenuButton;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject pauseMenuPanel;
    public GameObject endScene;

    [Header("Text Elements")]
    public TMP_Text livesText;
    public TMP_Text shotsText;
    public TMP_Text levelText;
    public TMP_Text coinsText;

    void Start()
    {
        FindPanels();
        SetupAllButtons();
        Debug.Log("[CanvasManager] Start completed.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPanels();
        SetupAllButtons();
        Debug.Log($"[CanvasManager] SceneLoaded: {scene.name} - re-bound UI references.");
    }

    private void FindPanels()
    {
        if (mainMenuPanel == null)
            mainMenuPanel = GameObject.Find("Menu");
        
        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingMenu");
        
        if (pauseMenuPanel == null)
            pauseMenuPanel = GameObject.Find("PauseMenu");
        
        if (endScene == null)
            endScene = GameObject.Find("EndScenePannel");

        if (livesText == null)
            livesText = GameObject.Find("LivesText")?.GetComponent<TMP_Text>();
        
        if (shotsText == null)
            shotsText = GameObject.Find("ShotsText")?.GetComponent<TMP_Text>();
        
        if (levelText == null)
            levelText = GameObject.Find("Level")?.GetComponent<TMP_Text>();
        
        if (coinsText == null)
            coinsText = GameObject.Find("CoinsText")?.GetComponent<TMP_Text>();

        if (livesText != null && GameManager.Instance != null)
        {
            livesText.text = $"Lives: {GameManager.Instance.lives}";
            GameManager.Instance.OnLivesChanged -= OnLivesChangedHandler;
            GameManager.Instance.OnLivesChanged += OnLivesChangedHandler;
        }
    }

    private void SetupAllButtons()
    {
        // Setup main menu buttons (these should be assigned in Inspector)
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(NewGame);
            Debug.Log("[CanvasManager] PlayButton listener added");
        }
        else
        {
            Debug.LogWarning("[CanvasManager] PlayButton is NULL!");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
            Debug.Log("[CanvasManager] SettingsButton listener added");
        }
        else
        {
            Debug.LogWarning("[CanvasManager] SettingsButton is NULL!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("[CanvasManager] QuitButton listener added");
        }
        else
        {
            Debug.LogWarning("[CanvasManager] QuitButton is NULL!");
        }

        // Setup settings button (if assigned in Inspector)
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
            Debug.Log("[CanvasManager] BackButton listener added");
        }

        // Setup pause buttons (if assigned in Inspector)
        if (resumeGame != null)
        {
            resumeGame.onClick.RemoveAllListeners();
            resumeGame.onClick.AddListener(() => { SetMenus(null, pauseMenuPanel); ResumeGame(); });
            Debug.Log("[CanvasManager] ResumeButton listener added");
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
            Debug.Log("[CanvasManager] PauseButton listener added");
        }

        // Setup end scene buttons (if assigned in Inspector)
        if (endSceneRestartGameButton != null)
        {
            endSceneRestartGameButton.onClick.RemoveAllListeners();
            endSceneRestartGameButton.onClick.AddListener(NewGame);
            Debug.Log("[CanvasManager] RestartButton listener added");
        }

        if (endSceneReturnToMenuButton != null)
        {
            endSceneReturnToMenuButton.onClick.RemoveAllListeners();
            endSceneReturnToMenuButton.onClick.AddListener(ReturnToMenu);
            Debug.Log("[CanvasManager] EndSceneReturnToMenuButton listener added");
        }
    }

    void OpenSettings()
    {
        Debug.Log("[CanvasManager] OpenSettings called!");
        SetMenus(settingsPanel, mainMenuPanel);
    }

    void CloseSettings()
    {
        Debug.Log("[CanvasManager] CloseSettings called!");
        SetMenus(mainMenuPanel, settingsPanel);
    }

    void TogglePause()
    {
        Debug.Log("[CanvasManager] TogglePause called!");
        
        if (pauseMenuPanel == null) return;

        if (pauseMenuPanel.activeSelf)
        {
            SetMenus(null, pauseMenuPanel);
            ResumeGame();
        }
        else
        {
            SetMenus(pauseMenuPanel, null);
            PauseGame();
        }
    }

    void SetMenus(GameObject menuToActivate, GameObject menuToDeactivate)
    {
        if (menuToDeactivate) 
        {
            menuToDeactivate.SetActive(false);
            Debug.Log("[CanvasManager] Deactivated: " + menuToDeactivate.name);
        }
        
        if (menuToActivate) 
        {
            menuToActivate.SetActive(true);
            Debug.Log("[CanvasManager] Activated: " + menuToActivate.name);
        }
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[CanvasManager] Game Resumed");
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("[CanvasManager] Game Paused");
    }

    void ReturnToMenu()
    {
        Debug.Log("[CanvasManager] Returning to Menu");
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void QuitGame()
    {
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[CanvasManager] QuitGame invoked.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void NewGame()
    {
        Debug.Log("[CanvasManager] NewGame invoked.");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLoadFromCheckpoint(false);
        }
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); 
    }

    void Update()
    {
        if (!pauseMenuPanel) return;

        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    private void OnLivesChangedHandler(int lives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }
}