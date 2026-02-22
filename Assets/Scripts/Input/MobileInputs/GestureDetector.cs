using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class GestureDetector : MonoBehaviour
{
    [Header("Gesture Detection Settings")]
    [Tooltip("Min Distance for Swipe as a percentage of screen height")]
    [Range(0.01f, 0.5f)]
    public float minSwipeDistance = 0.05f;

    [Tooltip("Max Time for Swipe in seconds")]
    public float maxSwipeTime = 0.5f;

    [Tooltip("Directional threshold for swipe detection (0 to 1)")]
    [Range(0.5f, 1f)]
    public float directionalThreshold = 0.8f;

    [Tooltip("Max time before tap times out in seconds")]
    public float maxTapTime = 0.2f;

    //Events for other scripts to scubscribe to
    public event System.Action OnTapDetected;
    public event System.Action<SwipeDirection> OnSwipeDetected;

    //state of our swipe detection
    private Vector2 touchStartPos = Vector2.zero;
    private float touchStartTime = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("GestureDetector: InputManager instance not found in the scene.");

            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchBegin += HandleTouchBegin;
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
        }
    }
    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchBegin -= HandleTouchBegin;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
        }
    }
    private void HandleTouchBegin()
    {
        touchStartTime = Time.time;
        touchStartPos = InputManager.Instance.GetTouchScreenPosition();
    }
    private void HandleTouchEnd()
    {
        float touchDuration = Time.time - touchStartTime;
        Vector2 touchEndPos = InputManager.Instance.GetTouchScreenPosition();

        //Calculate vector of the movement in pixels
        Vector2 swipeVector = touchEndPos - touchStartPos;

        float swipeDistance = swipeVector.magnitude / Screen.height;

        //check for if a tap occurred
        if (touchDuration < maxTapTime && swipeDistance < minSwipeDistance)
        {
            Debug.Log("Tap Detected");
            OnTapDetected?.Invoke();
            return;
        }

        //check for swipe
        if (touchDuration < maxSwipeTime && swipeDistance >= minSwipeDistance)
        {
            Vector2 direction = swipeVector.normalized;

            float dotUp = Vector2.Dot(direction, Vector2.up);
            float dotRight = Vector2.Dot(direction, Vector2.right);

            if (dotUp > directionalThreshold)
            {
                Debug.Log("Swipe Up Detected");
                OnSwipeDetected?.Invoke(SwipeDirection.Up);
                return;
            }
            
            if (dotUp < -directionalThreshold)
            {
                Debug.Log("Swipe Down Detected");
                OnSwipeDetected?.Invoke(SwipeDirection.Down);
                return;
            }
            
            if (dotRight > directionalThreshold)
            {
                Debug.Log("Swipe Right Detected");
                OnSwipeDetected?.Invoke(SwipeDirection.Right);
                return;
            }
            
            if (dotRight < -directionalThreshold)
            {
                Debug.Log("Swipe Left Detected");
                OnSwipeDetected?.Invoke(SwipeDirection.Left);
            }
        }

    }
}

public enum SwipeDirection
{
    Up,
    Down,
    Left,
    Right
}