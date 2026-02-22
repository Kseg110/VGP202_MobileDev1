// commented out save logic, will implement in future build to allow player to save and close the game and return to current run in the future.

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine.Audio;

[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    public AudioMixerGroup masterMixerGroup;
    public AudioMixerGroup musicMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    public delegate void PlayerSpawnDelegate(BilliardController playerInstance);
    public event PlayerSpawnDelegate OnPlayerControllerCreated;

    #region Player Controller Information
    public BilliardController playerPrefab;
    private BilliardController _playerInstance;
    public BilliardController playerInstance => _playerInstance;
    #endregion

    #region UI References
    public GameObject gameOverCanvasPrefab;
    private GameObject gameOverCanvasInstance;
    #endregion

    private AudioSource audioSource;

    public event Action<int> OnLivesChanged;
    public event Action<int> OnScoreChanged;

    #region Stats
    public int maxLives = 7;
    private int _lives = 7;
    private int _shots = 10;
    private protected int _maxShots = 10;
    private protected int _rounds = 0;
    private bool winCheck = false;

    public int lives
    {
        get => _lives;
        set
        {
            if (value <= 0)
            {
                Debug.Log("Game Over!");
                GameOver();
                _lives = 0;
            }
            else if (value < _lives)
            {
                Debug.Log("Ouch! You lost a life");
                _lives = value;
            }
            else if (value > maxLives)
            {
                _lives = maxLives;
            }
            else
            {
                _lives = value;
            }
            Debug.Log($"Lives: {_lives}");
            OnLivesChanged?.Invoke(_lives);
        }
    }

    public int shots
    {
        get => _shots;
        set => _shots = value;
    }

    public int rounds
    {
        get => _rounds;
        set => _rounds = value;
    }
    #endregion

    #region Singleton Pattern
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        Destroy(gameObject);
    }
    #endregion

    void Start()
    {

    }

    private void OnEnable()
    {
        // Subscribe to mobile input events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchBegin += HandleTouchInput;
            InputManager.Instance.OnTouchEnd += HandleTouchRelease;
            InputManager.Instance.OnPhoneTilt += HandlePhoneTilt;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from mobile input events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchBegin -= HandleTouchInput;
            InputManager.Instance.OnTouchEnd -= HandleTouchRelease;
            InputManager.Instance.OnPhoneTilt -= HandlePhoneTilt;
        }
    }

    #region Mobile Input Handlers
    private void HandleTouchInput()
    {
        // Handle touch began - could be used for game mechanics
        Debug.Log("Touch detected");
    }

    private void HandleTouchRelease()
    {
        // Handle touch release - could be used for game mechanics
        Debug.Log("Touch released");
    }

    private void HandlePhoneTilt(Vector3 tiltData)
    {
        // Handle phone tilt - could be used for game mechanics
        // This is great for mobile billiards games!
    }
    #endregion

    private bool isPaused = false;

    void Update()
    {
        // Keep only essential Update logic
        // For mobile, you might want to handle back button presses instead of Escape
        HandleMobileBackButton();

        // Ensure there is always one AudioListener in the scene
        EnsureAudioListener();

        // Debug keys removed - rely on mouse for desktop testing and InputManager for mobile.
    }

    private void HandleMobileBackButton()
    {
        // Handle Android back button or iOS equivalent
        if (Application.platform == RuntimePlatform.Android)
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }
    }

    private void HandleBackButton()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            // On main menu, quit application or minimize
            Application.Quit();
        }
        else
        {
            // In game, return to menu
            SceneManager.LoadScene(0);
        }
    }

    private void EnsureAudioListener()
    {
        if (FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
        {
            if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }
            else if (Camera.main == null)
            {
                GameObject fallbackCam = new GameObject("FallbackCamera");
                fallbackCam.AddComponent<Camera>();
                fallbackCam.AddComponent<AudioListener>();
            }
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void GameOver()
    {
        SceneManager.LoadScene(0);
    }

    public Transform spawnPoint; 
    public void SetLoadFromCheckpoint(bool value)
    {
        PlayerPrefs.SetInt("LoadFromCheckpoint", value ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"SetLoadFromCheckpoint called with value: {value}");
    }

    public void WinGame()
    {
        winCheck = true;
        Debug.Log("You WIN!");
        SceneManager.sceneLoaded += OnMenuSceneLoaded;
        SceneManager.LoadScene("Menu");
    }

    private void OnMenuSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            GameObject mainMenu = GameObject.Find("Menu");

            GameObject endScene = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "EndScene" && go.scene.name == "Menu")
                {
                    endScene = go;
                    break;
                }
            }

            if (mainMenu != null)
                mainMenu.SetActive(false);
            if (endScene != null)
                endScene.SetActive(true);

            SceneManager.sceneLoaded -= OnMenuSceneLoaded;
        }
    }

    public void PlayerDied()
    {
        SceneManager.LoadScene("EndScene");
    }
}