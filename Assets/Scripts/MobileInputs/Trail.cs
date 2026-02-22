using System;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class Trail : MonoBehaviour
{
    private TrailRenderer tr;
    private Camera mainCamera;
    private bool subscribed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tr = GetComponent<TrailRenderer>();
        mainCamera = Camera.main;
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    private void OnDestroy()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (!subscribed && InputManager.Instance != null)
        {
            InputManager.Instance.OnTouchBegin += EnableTrailRenderer;
            InputManager.Instance.OnTouchEnd += DisableTrailRenderer;
            subscribed = true;
        }
    }

    private void TryUnsubscribe()
    {
        if (subscribed)
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnTouchBegin -= EnableTrailRenderer;
                InputManager.Instance.OnTouchEnd -= DisableTrailRenderer;
            }
            subscribed = false;
        }
    }

    private void EnableTrailRenderer()
    {
        UpdatePosition();
        if (tr != null)
        {
            tr.enabled = true;
            tr.Clear();
        }
    }
    private void DisableTrailRenderer()
    {
        if (tr != null)
            tr.enabled = false;
    }

    void UpdatePosition()
    {
        if (InputManager.Instance == null) return;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Use the trail's current z-plane so the trail doesn't jump to camera near plane or z=0 incorrectly
        float planeZ = transform.position.z;
        Vector3 worldPos = InputManager.Instance.GetTouchWorldPosition(mainCamera, planeZ);
        // Ensure z stays at the object's z
        worldPos.z = planeZ;
        transform.position = worldPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (tr != null && tr.enabled)
        {
            UpdatePosition();
        }
    }
}
