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
    public BilliardController PlayerInstance => _playerInstance;
    #endregion

    #region UI References
    public GameObject gameOverCanvasPrefab;
    private GameObject gameOverCanvasInstance;
    public GameObject endScenePanel;
    #endregion

    private AudioSource audioSource;

    // Events for UI updates
    public event Action<int> OnLivesChanged;
    public event Action<int> OnShotsChanged;
    public event Action<int> OnRoundsChanged;
    //public event Action<int> OnScoreChanged;

    #region Stats
    public int maxLives = 7;
    private int _lives = 7;
    private int _shots = 10;
    private protected int _maxShots = 10;
    private protected int _rounds = 0;
    private bool winCheck = false;

    public int Lives
    {
        get => _lives;
        set
        {
            if (value <= 0)
            {
                GameOver();
                _lives = 0;
            }
            else if (value < _lives)
            {
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
            OnLivesChanged?.Invoke(_lives);
        }
    }

    public int Shots
    {
        get => _shots;
        set
        {
            _shots = value;
            OnShotsChanged?.Invoke(_shots);
        }
    }

    public int Rounds
    {
        get => _rounds;
        set
        {
            _rounds = value;
            OnRoundsChanged?.Invoke(_rounds);
        }
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
            
            // Subscribe to scene loading to spawn player
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            return;
        }

        Destroy(gameObject);
    }
    #endregion

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if this is the game scene (not menu scene)
        if (scene.buildIndex == 1 || scene.name.Contains("Game"))
        {
            SpawnPlayer();
        }
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
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region Mobile Input Handlers
    private void HandleTouchInput()
    {
        // Reserved for gameplay usage; logging removed.
    }

    private void HandleTouchRelease()
    {
        // Reserved for gameplay usage; logging removed.
    }

    private void HandlePhoneTilt(Vector3 tiltData)
    {
        // Handle phone tilt - could be used for game mechanics
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
                GameObject fallbackCam = new ("FallbackCamera");
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
        // Show end scene panel if available
        if (endScenePanel != null)
        {
            endScenePanel.SetActive(true);
        }
        else
        {
            // Fallback to loading menu scene
            SceneManager.LoadScene(0);
        }
    }

    public Transform spawnPoint; 
    
    private void SpawnPlayer()
    {
        // Don't spawn if player already exists
        if (_playerInstance != null)
        {
            return;
        }

        if (playerPrefab == null)
        {
            return;
        }

        // Find spawn point if not assigned
        if (spawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");
            if (spawnObj != null)
            {
                spawnPoint = spawnObj.transform;
            }
            else
            {
                spawnPoint = null;
            }
        }

        // Instantiate player
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        
        _playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        _playerInstance.gameObject.name = "Player";
        
        // Notify listeners
        OnPlayerControllerCreated?.Invoke(_playerInstance);
    }

    public void RespawnPlayer(Vector3 position)
    {
        if (_playerInstance != null)
        {
            _playerInstance.transform.position = position;
            
            // Reset rigidbody
            Rigidbody rb = _playerInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Reset ball state
            BilliardBall ball = _playerInstance.GetComponent<BilliardBall>();
            if (ball != null)
            {
                ball.currentSideSpin = 0f;
            }
            
        }
        else
        {
            SpawnPlayer();
        }
    }

    public void SetLoadFromCheckpoint(bool value)
    {
        PlayerPrefs.SetInt("LoadFromCheckpoint", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void WinGame()
    {
        winCheck = true;
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
        // Clean up player instance
        if (_playerInstance != null)
        {
            Destroy(_playerInstance.gameObject);
            _playerInstance = null;
        }
        
        // Show end screen or load end scene
        if (endScenePanel != null)
        {
            endScenePanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("EndScene");
        }
    }
}