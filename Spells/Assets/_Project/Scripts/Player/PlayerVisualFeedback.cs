using UnityEngine;

/// <summary>
/// Visual feedback for combat events: damage flash, i-frame blink,
/// parry flash, death effect. Subscribes to HealthSystem and ParrySystem events.
/// Works with SpriteRenderer — changes color/alpha for feedback.
/// </summary>
public class PlayerVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Damage Flash")]
    [Tooltip("Color to flash on hit")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [Tooltip("Duration of damage flash")]
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("Parry Flash")]
    [SerializeField] private Color parryActiveColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private Color parrySuccessColor = new Color(0f, 1f, 1f, 1f);

    [Header("I-Frame Blink")]
    [Tooltip("Blink rate during invincibility (times per second)")]
    [SerializeField] private float blinkRate = 10f;

    private Color originalColor;
    private HealthSystem health;
    private ParrySystem parry;

    private float flashTimer;
    private Color flashColor;
    private bool isFlashing;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        health = GetComponent<HealthSystem>();
        parry = GetComponent<ParrySystem>();

        // Subscribe to events
        if (health != null)
        {
            health.OnDamaged.AddListener(OnDamaged);
            health.OnDeath.AddListener(OnDeath);
        }

        if (parry != null)
        {
            parry.OnParryStart.AddListener(OnParryStart);
            parry.OnParrySuccess.AddListener(OnParrySuccess);
            parry.OnParryWhiff.AddListener(OnParryWhiff);
        }
    }

    private void OnDamaged(float amount)
    {
        StartFlash(damageFlashColor, damageFlashDuration);
    }

    private void OnDeath()
    {
        // Could trigger death animation/particles here
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
    }

    private void OnParryStart()
    {
        StartFlash(parryActiveColor, 0f); // Stays until parry ends
    }

    private void OnParrySuccess()
    {
        StartFlash(parrySuccessColor, 0.15f);
    }

    private void OnParryWhiff()
    {
        // Briefly dim on whiff
        StartFlash(new Color(0.5f, 0.5f, 0.5f, 1f), 0.1f);
    }

    private void StartFlash(Color color, float duration)
    {
        flashColor = color;
        flashTimer = duration;
        isFlashing = true;

        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        // Timed flash
        if (isFlashing && flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                isFlashing = false;
                spriteRenderer.color = originalColor;
            }
        }

        // I-frame blink (overrides other effects during invincibility)
        if (health != null && health.IsInvincible)
        {
            float alpha = Mathf.PingPong(Time.time * blinkRate, 1f) > 0.5f ? 1f : 0.3f;
            Color c = isFlashing ? flashColor : originalColor;
            spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
        }
        else if (!isFlashing)
        {
            spriteRenderer.color = originalColor;
        }

        // Parry active glow (sustained, not timed)
        if (parry != null && parry.IsParrying && !isFlashing)
        {
            spriteRenderer.color = parryActiveColor;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged.RemoveListener(OnDamaged);
            health.OnDeath.RemoveListener(OnDeath);
        }
        if (parry != null)
        {
            parry.OnParryStart.RemoveListener(OnParryStart);
            parry.OnParrySuccess.RemoveListener(OnParrySuccess);
            parry.OnParryWhiff.RemoveListener(OnParryWhiff);
        }
    }
}
