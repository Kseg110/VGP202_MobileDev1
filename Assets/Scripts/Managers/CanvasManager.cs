using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button backButton;
    public Button quitButton;

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
        if (playButton) playButton.onClick.AddListener(NewGame);
        if (settingsButton) settingsButton.onClick.AddListener(() => SetMenus(settingsPanel, mainMenuPanel));
        if (backButton) backButton.onClick.AddListener(() => SetMenus(mainMenuPanel, settingsPanel));

        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        if (resumeGame) resumeGame.onClick.AddListener(() => { SetMenus(null, pauseMenuPanel); ResumeGame(); });
        if (returnToMenu) returnToMenu.onClick.AddListener(ReturnToMenu);
        //if (continueGameButton) continueGameButton.onClick.AddListener(LoadGame);
        if (endSceneRestartGameButton) endSceneRestartGameButton.onClick.AddListener(NewGame);
        if (endSceneReturnToMenuButton) endSceneReturnToMenuButton.onClick.AddListener(ReturnToMenu);

        if (livesText)
        {
            livesText.text = $"Lives: {GameManager.Instance.lives}";
            GameManager.Instance.OnLivesChanged -= OnLivesChangedHandler;
            GameManager.Instance.OnLivesChanged += OnLivesChangedHandler;
        }

        //if (scoreText)
        //{
        //    scoreText.text = $"Coins: {GameManager.Instance.coins}";
        //    GameManager.Instance.OnScoreChanged -= OnScoreChangedHandler;
        //    GameManager.Instance.OnScoreChanged += OnScoreChangedHandler;
        //}


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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void NewGame()
    {
        GameManager.Instance.SetLoadFromCheckpoint(false);
        Time.timeScale =1f;
        //SaveManager.SavePlayer(new Vector3(369.529999f,0.150000006f,471.540009f), GameManager.Instance.maxLives,0, "");
        SceneManager.LoadScene(1); 
    }

    //void LoadGame()
    //{
    //    GameManager.Instance.SetLoadFromCheckpoint(true);
    //    SceneManager.LoadScene(1);
    //}

    // Update is called once per frame
    void Update()
        {
            if (!pauseMenuPanel) return;

            if (Input.GetKeyDown(KeyCode.P))
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