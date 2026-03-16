using UnityEngine;

/// <summary>
/// Brief time freeze on impact for dramatic effect.
/// Sets Time.timeScale to 0 for a few frames, then restores.
/// Essential for making hits feel punchy — every fighting game does this.
/// </summary>
public class Hitstop : MonoBehaviour
{
    public static Hitstop Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("Duration of hitstop in real-time seconds (unscaled)")]
    [SerializeField] private float defaultDuration = 0.06f;
    [Tooltip("Time scale during hitstop (0 = full freeze, 0.1 = slow-mo)")]
    [SerializeField] private float hitstopTimeScale = 0.02f;

    private float stopTimer;
    private float originalTimeScale = 1f;
    private bool isStopped;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Trigger a hitstop. Freezes time briefly.
    /// Longer duration for stronger hits.
    /// </summary>
    public void Stop(float duration = 0f)
    {
        float d = duration > 0f ? duration : defaultDuration;

        // Don't shorten an existing hitstop
        if (isStopped && stopTimer > d) return;

        if (!isStopped)
            originalTimeScale = Time.timeScale;

        isStopped = true;
        stopTimer = d;
        Time.timeScale = hitstopTimeScale;
        Time.fixedDeltaTime = 0.02f * hitstopTimeScale; // Keep physics in sync
    }

    /// <summary>
    /// Presets for common combat events.
    /// </summary>
    public void StopOnHit() => Stop(0.04f);
    public void StopOnParry() => Stop(0.08f);
    public void StopOnKill() => Stop(0.12f);

    private void Update()
    {
        if (!isStopped) return;

        // Use unscaledDeltaTime since we're manipulating timeScale
        stopTimer -= Time.unscaledDeltaTime;

        if (stopTimer <= 0f)
        {
            isStopped = false;
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = 0.02f * originalTimeScale;
        }
    }

    private void OnDestroy()
    {
        // Safety: restore time scale if destroyed during hitstop
        if (isStopped)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
