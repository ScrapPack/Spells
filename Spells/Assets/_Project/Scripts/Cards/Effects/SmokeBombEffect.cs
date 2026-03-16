using UnityEngine;

/// <summary>
/// Smoke Bomb (Rogue, Tier 1): On taking damage, become invisible for 1.5 seconds.
/// While invisible, you can't attack.
///
/// GDD: "On taking damage, become invisible for 1.5 seconds.
/// While invisible, you can't attack."
///
/// Defensive escape tool for the fragile Rogue (2 HP). Getting hit
/// gives you a chance to reposition, but you sacrifice offense.
/// Synergizes with Ambush (reposition behind the attacker).
///
/// Stacking: Each stack adds +0.5s invisibility duration.
/// </summary>
public class SmokeBombEffect : SpellEffect
{
    private float invisDuration;
    private float invisTimer;
    private bool isInvisible;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool colorStored;

    protected override void OnApply()
    {
        invisDuration = 1.5f + (StackCount - 1) * 0.5f;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && !colorStored)
        {
            originalColor = spriteRenderer.color;
            colorStored = true;
        }

        if (Health != null)
        {
            Health.OnDamaged.AddListener(OnDamaged);
        }
    }

    private void OnDamaged(float amount)
    {
        if (Health == null || !Health.IsAlive) return;
        if (isInvisible) return; // Don't re-trigger while already invisible

        ActivateSmokeBomb();
    }

    private void ActivateSmokeBomb()
    {
        isInvisible = true;
        invisTimer = invisDuration;

        // Visual: make nearly invisible
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 0.1f);
        }

        // Disable attack
        if (Spawner != null)
            Spawner.enabled = false;
    }

    private void Update()
    {
        if (!isInvisible) return;

        invisTimer -= Time.deltaTime;
        if (invisTimer <= 0f)
        {
            EndInvisibility();
        }
    }

    private void EndInvisibility()
    {
        isInvisible = false;

        // Restore visibility
        if (spriteRenderer != null && colorStored)
        {
            spriteRenderer.color = originalColor;
        }

        // Re-enable attack
        if (Spawner != null)
            Spawner.enabled = true;
    }

    public override void OnRoundStart()
    {
        if (isInvisible) EndInvisibility();
    }

    public override void OnRemove()
    {
        if (isInvisible) EndInvisibility();

        if (Health != null)
            Health.OnDamaged.RemoveListener(OnDamaged);
    }
}
