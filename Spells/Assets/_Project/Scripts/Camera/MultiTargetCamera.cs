using System.Collections.Generic;
using UnityEngine;

public class MultiTargetCamera : MonoBehaviour
{
    [Header("Targets")]
    private List<Transform> targets = new List<Transform>();

    [Header("Camera Settings")]
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 18f;
    [SerializeField] private float orthographicSizePadding = 2f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Round Zoom")]
    [Tooltip("Minimum ortho size at full compression (end of round)")]
    [SerializeField] private float zoomMinSize = 3f;

    private Camera cam;
    private float zoomProgress;

    /// <summary>
    /// Drive camera zoom. 0 = no zoom, 1 = full compression.
    /// </summary>
    public void SetZoomProgress(float progress)
    {
        zoomProgress = Mathf.Clamp01(progress);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    public void AddTarget(Transform target)
    {
        if (!targets.Contains(target))
            targets.Add(target);
    }

    public void RemoveTarget(Transform target)
    {
        targets.Remove(target);
    }

    private void LateUpdate()
    {
        targets.RemoveAll(t => t == null);
        if (targets.Count == 0) return;

        Vector3 center = GetCenterPoint();
        Vector3 targetPosition = center + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        float targetSize = GetRequiredOrthographicSize();
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, smoothSpeed * Time.deltaTime);
    }

    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
            return targets[0].position;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 1; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }
        return bounds.center;
    }

    private float GetRequiredOrthographicSize()
    {
        if (targets.Count <= 1)
            return minOrthographicSize;

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 1; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        float sizeY = bounds.size.y / 2f + orthographicSizePadding;
        float sizeX = (bounds.size.x / 2f + orthographicSizePadding) / cam.aspect;
        float requiredSize = Mathf.Max(sizeY, sizeX);

        // Apply round zoom compression: shrinks the max allowed size over time
        float currentMax = Mathf.Lerp(maxOrthographicSize, zoomMinSize, zoomProgress);
        float currentMin = Mathf.Lerp(minOrthographicSize, zoomMinSize, zoomProgress);
        return Mathf.Clamp(requiredSize, currentMin, currentMax);
    }
}
