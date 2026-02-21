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
    public GameObject gameOverCanvasPrefab; // Drag your Game Over UI Canvas prefab here in the Inspector
    private GameObject gameOverCanvasInstance;
    #endregion

    //public AudioClip scorePickup;
    private AudioSource audioSource;

    public event Action<int> OnLivesChanged;
    public event Action<int> OnScoreChanged;

    #region Stats
    public int maxLives = 7;
    private int _lives = 7;

    //private int _coins = 4;

    private int _shots = 10;
    private protected int _maxShots = 10;
 
    private protected int _rounds = 0;
    private bool winCheck = false;

   /* public int coins
    {
        get => _coins;
        set
        {
            if (value < 0)
                _coins = 0;
            else
                _coins = value;
            Debug.Log($"Coins: {_coins}");
            OnCoinsChanged?.Invoke(_coins);
        }
    } */
    public int lives
    {
        get => _lives;
        set
        {
            if (value <= 0)
            {
                //gameover
                Debug.Log("Game Over!");
                GameOver();
                _lives = 0;
            }
            else if (value < _lives)
            {
                //play hurt sound
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
        set
        {
            _shots = value;
        }
    }

    public int rounds
    {
        get => _rounds;
        set
        {
            _rounds = value;
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
            return;
        }

        Destroy(gameObject);
    }
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnEnable()
    {
        //SceneManager.activeSceneChanged += SceneChanged;
    }

    private void OnDisable()
    {
        //SceneManager.activeSceneChanged -= SceneChanged;
    }


    void GameOver()
    {
        SceneManager.LoadScene(0);
    }

   /* void Respawn()
    {
        _playerInstance.transform.position = SaveManager.LoadPosition();
    }

    public void RespawnPlayerAt(Vector3 position)
{
    if (_playerInstance != null)
    {
        Destroy(_playerInstance.gameObject);
    }
    _playerInstance = Instantiate(playerPrefab, position, Quaternion.identity);
    OnPlayerControllerCreated?.Invoke(_playerInstance);
    Debug.Log("GameManager: Player respawned at checkpoint.");
} */


    private bool isPaused = false;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                SceneManager.LoadScene(1);
            }
            else
            {
                // Unlock and show cursor before returning to menu
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(0);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            lives--; // set to subtract lives for testing
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // Ensure there is always one AudioListener in the scene
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

    public Transform spawnPoint; 
    public void SetLoadFromCheckpoint(bool value)
    {
        PlayerPrefs.SetInt("LoadFromCheckpoint", value ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"SetLoadFromCheckpoint called with value: {value}");
    }

    #region save logic
    /* void OnApplicationQuit()
    {
        if (_playerInstance != null)
        {
            string weaponPrefabName = _playerInstance.CurWeapon ? _playerInstance.CurWeapon.GetType().Name : "";
            SaveManager.SavePlayer(
                _playerInstance.transform.position,
                _playerInstance.GetHealth(),
                _playerInstance.GetScore(),
                weaponPrefabName
            );
            Debug.Log($"GameManager: Saved weapon prefab name on Quit: {weaponPrefabName}");
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && _playerInstance != null)
        {
            string weaponPrefabName = _playerInstance.CurWeapon ? _playerInstance.CurWeapon.GetType().Name : "";
            SaveManager.SavePlayer(
                _playerInstance.transform.position,
                _playerInstance.GetHealth(),
                _playerInstance.GetScore()
            );
        }
    }
    public void StartLevel(Vector3 startPosition, bool loadFromSave)
    {
        _playerInstance = Instantiate(playerPrefab, startPosition, Quaternion.identity);
        Debug.Log("GameManager: Player instantiated.");
        OnPlayerControllerCreated?.Invoke(_playerInstance);
        Debug.Log("GameManager: OnPlayerControllerCreated event fired.");

        if (loadFromSave)
        {
            // Load persistent data
            _playerInstance.SetHealth(SaveManager.LoadHealth());
            _playerInstance.SetScore(SaveManager.LoadScore());
            _playerInstance.EquipWeaponByName(SaveManager.LoadWeapon());
        }
        else
        {
            // Overwrite save only on new game
            _playerInstance.SetHealth(maxLives);
            _playerInstance.SetScore(0);
            SaveManager.SavePlayer(startPosition, maxLives, 0, "");
        }
    } */

    #endregion
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameObject mainMenu = GameObject.Find("Menu");

            // Find EndScene even if it's inactive
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