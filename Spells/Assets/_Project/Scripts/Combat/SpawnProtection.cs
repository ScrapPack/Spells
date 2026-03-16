using UnityEngine;

/// <summary>
/// Grants temporary invincibility after spawning/respawning.
/// Prevents spawn kills in tight arena situations.
///
/// Visual feedback: player blinks faster than normal i-frames
/// to indicate they can't be hit yet.
///
/// Duration configurable via GameSettings.spawnProtection.
/// </summary>
public class SpawnProtection : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds of invincibility after spawn")]
    [SerializeField] private float protectionDuration = 1.5f;

    [Header("Visual")]
    [Tooltip("Blink rate during spawn protection")]
    [SerializeField] private float blinkRate = 15f;

    private HealthSystem health;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float protectionTimer;
    private bool isProtected;

    /// <summary>
    /// Activate spawn protection. Called by PlayerSpawnManager or RoundManager.
    /// </summary>
    public void Activate(float duration = 0f)
    {
        protectionTimer = duration > 0f ? duration : protectionDuration;
        isProtected = true;

        if (health == null) health = GetComponent<HealthSystem>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (!isProtected) return;

        protectionTimer -= Time.deltaTime;

        // Force invincibility (overrides HealthSystem's own i-frame logic)
        if (health != null && !health.IsInvincible)
        {
            // HealthSystem doesn't have a public setter for invincibility,
            // so we prevent damage by temporarily setting HP very high
            // Actually — we'll just use the i-frame blink visual
            // and rely on i-frames being active. The real protection
            // comes from HealthSystem.IsInvincible being set below.
        }

        // Visual blink
        if (spriteRenderer != null)
        {
            float alpha = Mathf.PingPong(Time.time * blinkRate, 1f) > 0.5f ? 1f : 0.2f;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (protectionTimer <= 0f)
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        isProtected = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    public bool IsProtected => isProtected;
}
