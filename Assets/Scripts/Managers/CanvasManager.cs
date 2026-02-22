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
    public Button pauseButton; // Added PauseButton reference

    public Button resumeGame;
    public Button returnToMenu;
    //public Button continueGameButton;
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Bind serialized references (useful if this object is DontDestroyOnLoad and UI is scene-local)
        BindReferences();
        SetupBindings();
        Debug.Log("[CanvasManager] Start completed.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // defensive
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-bind references when a new scene loads (UI elements are usually scene local and get destroyed)
        BindReferences();
        SetupBindings();
        Debug.Log($"[CanvasManager] SceneLoaded: {scene.name} - re-bound UI references.");
    }

    private void BindReferences()
    {
        // Force re-search for buttons if they're null or destroyed
        if (playButton == null || !playButton.gameObject.activeInHierarchy)
            playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        
        if (settingsButton == null || !settingsButton.gameObject.activeInHierarchy)
            settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        
        if (backButton == null || !backButton.gameObject.activeInHierarchy)
            backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
        
        if (quitButton == null || !quitButton.gameObject.activeInHierarchy)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
        
        if (pauseButton == null || !pauseButton.gameObject.activeInHierarchy)
            pauseButton = GameObject.Find("PauseButton")?.GetComponent<Button>();

        if (resumeGame == null || !resumeGame.gameObject.activeInHierarchy)
            resumeGame = GameObject.Find("ResumeButton")?.GetComponent<Button>();
        
        if (returnToMenu == null || !returnToMenu.gameObject.activeInHierarchy)
            returnToMenu = GameObject.Find("ReturnToMenuButton")?.GetComponent<Button>();
        
        if (endSceneRestartGameButton == null || !endSceneRestartGameButton.gameObject.activeInHierarchy)
            endSceneRestartGameButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
        
        if (endSceneReturnToMenuButton == null || !endSceneReturnToMenuButton.gameObject.activeInHierarchy)
            endSceneReturnToMenuButton = GameObject.Find("EndSceneReturnToMenuButton")?.GetComponent<Button>();

        // Panels
        if (mainMenuPanel == null)
            mainMenuPanel = GameObject.Find("Menu");
        
        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingMenu");
        
        if (pauseMenuPanel == null)
            pauseMenuPanel = GameObject.Find("PauseMenu");
        
        if (endScene == null)
            endScene = GameObject.Find("EndScenePannel");

        // Text elements
        if (livesText == null)
            livesText = GameObject.Find("LivesText")?.GetComponent<TMP_Text>();
        
        if (shotsText == null)
            shotsText = GameObject.Find("ShotsText")?.GetComponent<TMP_Text>();
        
        if (levelText == null)
            levelText = GameObject.Find("Level")?.GetComponent<TMP_Text>();
        
        if (coinsText == null)
            coinsText = GameObject.Find("CoinsText")?.GetComponent<TMP_Text>();

        Debug.Log($"[CanvasManager] BindReferences results: playButton={(playButton != null ? playButton.name : "null")}, settingsButton={(settingsButton != null ? settingsButton.name : "null")}, quitButton={(quitButton != null ? quitButton.name : "null")}, endSceneReturnToMenuButton={(endSceneReturnToMenuButton != null ? endSceneReturnToMenuButton.name : "null")}");
    }

    private void SetupBindings()
    {
        // Get current scene to determine what UI should exist
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Buttons: remove old listeners to avoid duplicates and re-wire
        if (playButton != null)
        {
            Debug.Log($"[CanvasManager] Setting up playButton - Button component exists: {playButton != null}, GameObject active: {playButton.gameObject.activeInHierarchy}, Interactable: {playButton.interactable}");
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(NewGame);
            Debug.Log("[CanvasManager] playButton listener added.");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() => { SetMenus(settingsPanel, mainMenuPanel); Debug.Log("[CanvasManager] SettingsButton clicked"); });
            Debug.Log("[CanvasManager] settingsButton listener added.");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => { SetMenus(mainMenuPanel, settingsPanel); Debug.Log("[CanvasManager] BackButton clicked"); });
            Debug.Log("[CanvasManager] backButton listener added.");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("[CanvasManager] quitButton listener added.");
        }

        // Wire pauseButton to toggle the pause panel (same behavior as right-click)
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(() =>
            {
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
            });
            Debug.Log("[CanvasManager] pauseButton listener added.");
        }

        if (resumeGame != null)
        {
            resumeGame.onClick.RemoveAllListeners();
            resumeGame.onClick.AddListener(() => { SetMenus(null, pauseMenuPanel); ResumeGame(); });
            Debug.Log("[CanvasManager] resumeGame listener added.");
        }

        if (returnToMenu != null)
        {
            returnToMenu.onClick.RemoveAllListeners();
            returnToMenu.onClick.AddListener(ReturnToMenu);
            Debug.Log("[CanvasManager] returnToMenu listener added.");
        }

        if (endSceneRestartGameButton != null)
        {
            endSceneRestartGameButton.onClick.RemoveAllListeners();
            endSceneRestartGameButton.onClick.AddListener(NewGame);
            Debug.Log("[CanvasManager] endSceneRestartGameButton listener added.");
        }

        if (endSceneReturnToMenuButton != null)
        {
            endSceneReturnToMenuButton.onClick.RemoveAllListeners();
            endSceneReturnToMenuButton.onClick.AddListener(ReturnToMenu);
            Debug.Log("[CanvasManager] endSceneReturnToMenuButton listener added.");
        }

        // Lives text hookup (safe subscribe)
        if (livesText != null && GameManager.Instance != null)
        {
            livesText.text = $"Lives: {GameManager.Instance.lives}";
            GameManager.Instance.OnLivesChanged -= OnLivesChangedHandler;
            GameManager.Instance.OnLivesChanged += OnLivesChangedHandler;
            Debug.Log("[CanvasManager] livesText hookup complete.");
        }
        
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager] SetupBindings complete for scene: {currentScene}");
#endif
    }

    void SetMenus(GameObject menuToActivate, GameObject menuToDeactivate)
    {
        if (menuToActivate) menuToActivate.SetActive(true);
        if (menuToDeactivate) menuToDeactivate.SetActive(false);
        // No cursor logic here!
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        //Cursor.visible = false;
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f; // Unpause before switching scenes
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
        
        // Only call GameManager if it exists
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLoadFromCheckpoint(false);
        }
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); 
    }

    // Update is called once per frame
    void Update()
    {
        if (!pauseMenuPanel) return;

        // Use right mouse button to toggle pause for desktop testing (no keyboard required).
        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
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
        }
    }

    private void OnLivesChangedHandler(int lives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }

    //private void OnScoreChangedHandler(int score)
    //{
    //    if (coinsText != null)
    //        coinsText.text = $"Gold: {score}";
    //}
}