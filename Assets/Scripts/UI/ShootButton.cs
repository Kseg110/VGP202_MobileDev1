using UnityEngine;
using UnityEngine.UI;
using System;

public class ShootButton : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button shootButton;
    [SerializeField] private Image buttonIcon;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private Color readyToFireColor = Color.red;
    
    public event Action OnStartCharging;
    public event Action OnFireShot;
    
    public bool IsCharging { get; private set; }
    
    private BilliardController currentPlayer;
    
    private void Awake()
    {
        if (shootButton == null)
            shootButton = GetComponent<Button>();
            
        if (buttonIcon == null)
            buttonIcon = GetComponent<Image>();
    }
    
    private void Start()
    {
        shootButton.onClick.AddListener(HandleButtonClick);
        SetIdleState();
        
        // Subscribe to GameManager player spawn event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerControllerCreated += ConnectToPlayer;
            
            // Check if player already exists
            if (GameManager.Instance.PlayerInstance != null)
            {
                ConnectToPlayer(GameManager.Instance.PlayerInstance);
            }
        }
    }
    
    private void ConnectToPlayer(BilliardController player)
    {
        if (currentPlayer != null)
        {
            OnStartCharging = null;
            OnFireShot = null;
        }
        
        currentPlayer = player;
       
        //Debug.Log($"[ShootButton] Connected to player: {player.gameObject.name}");
    }
    
    private void HandleButtonClick()
    {
        if (!IsCharging)
        {
            StartCharging();
        }
        else
        {
            FireShot();
        }
    }
    
    private void StartCharging()
    {
        IsCharging = true;
        SetChargingState();
        OnStartCharging?.Invoke();
        //Debug.Log("[ShootButton] Started charging");
    }
    
    private void FireShot()
    {
        IsCharging = false;
        SetIdleState();
        OnFireShot?.Invoke();
        //Debug.Log("[ShootButton] Fired shot");
    }
    
    public void SetIdleState()
    {
        IsCharging = false;
        if (buttonIcon != null)
            buttonIcon.color = idleColor;
    }
    
    public void SetChargingState()
    {
        if (buttonIcon != null)
            buttonIcon.color = chargingColor;
    }
    
    public void SetReadyToFireState()
    {
        if (buttonIcon != null)
            buttonIcon.color = readyToFireColor;
    }
    
    private void OnDestroy()
    {
        if (shootButton != null)
            shootButton.onClick.RemoveListener(HandleButtonClick);
            
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerControllerCreated -= ConnectToPlayer;
        }
    }
}
