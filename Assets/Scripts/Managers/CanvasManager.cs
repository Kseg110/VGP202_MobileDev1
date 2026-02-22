using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings")]
    public Button backButton;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    void Start()
    {
        SetupButtons();
        Debug.Log("[CanvasManager] Start completed.");
    }

    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(NewGame);
            //Debug.Log("[CanvasManager] PlayButton listener added");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
            //Debug.Log("[CanvasManager] SettingsButton listener added");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            //Debug.Log("[CanvasManager] QuitButton listener added");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
            //Debug.Log("[CanvasManager] BackButton listener added");
        }
    }

    void OpenSettings()
    {
        Debug.Log("[CanvasManager] OpenSettings called!");
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    void CloseSettings()
    {
        Debug.Log("[CanvasManager] CloseSettings called!");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
}