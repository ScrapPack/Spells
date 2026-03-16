using UnityEngine;

/// <summary>
/// Camera screen shake for impact feedback.
/// GDD: "Punchy, satisfying hit sounds (screen shake on impact)".
/// Shakes the camera with configurable intensity and duration.
/// Multiple overlapping shakes accumulate.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("Default shake intensity (displacement in units)")]
    [SerializeField] private float defaultIntensity = 0.15f;
    [Tooltip("Default shake duration")]
    [SerializeField] private float defaultDuration = 0.12f;

    private float shakeTimer;
    private float shakeIntensity;
    private Vector3 originalLocalPos;
    private bool isShaking;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Trigger a screen shake. Can be called from anywhere via ScreenShake.Instance.Shake().
    /// </summary>
    public void Shake(float intensity = 0f, float duration = 0f)
    {
        float i = intensity > 0f ? intensity : defaultIntensity;
        float d = duration > 0f ? duration : defaultDuration;

        // Take the stronger shake if already shaking
        if (i > shakeIntensity || shakeTimer <= 0f)
        {
            shakeIntensity = i;
        }

        shakeTimer = Mathf.Max(shakeTimer, d);

        if (!isShaking)
        {
            originalLocalPos = transform.localPosition;
            isShaking = true;
        }
    }

    /// <summary>
    /// Shake presets for common events.
    /// </summary>
    public void ShakeOnHit() => Shake(0.12f, 0.1f);
    public void ShakeOnParry() => Shake(0.2f, 0.15f);
    public void ShakeOnKill() => Shake(0.3f, 0.25f);

    private void LateUpdate()
    {
        if (!isShaking) return;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;

            // Perlin noise for smoother shake (less jarring than pure random)
            float t = Time.time * 25f;
            float offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shakeIntensity;
            float offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shakeIntensity;

            // Decay intensity over duration
            float decay = shakeTimer / defaultDuration;
            transform.localPosition = originalLocalPos + new Vector3(offsetX * decay, offsetY * decay, 0f);
        }
        else
        {
            // Reset to original position
            transform.localPosition = originalLocalPos;
            isShaking = false;
            shakeIntensity = 0f;
        }
    }
}
