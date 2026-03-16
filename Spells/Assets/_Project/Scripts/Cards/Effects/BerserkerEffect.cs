using UnityEngine;

/// <summary>
/// Berserker (Warrior, Tier 2): +1 damage when below half HP.
/// Can't parry when below half HP.
///
/// GDD: "+1 damage when below half HP. Can't parry when below half HP."
///
/// Pure risk-reward: when losing, you hit harder but can't defend.
/// At 4HP Warrior, triggers at 2HP or below. Synergizes with
/// retrievable axes (keep fighting without parry).
///
/// Stacking: Each stack adds +1 more damage bonus below half HP.
/// </summary>
public class BerserkerEffect : SpellEffect
{
    private int bonusDamage;
    private bool isBerserking;
    private ParrySystem parrySystem;

    protected override void OnApply()
    {
        bonusDamage = StackCount;
        parrySystem = GetComponent<ParrySystem>();

        if (Health != null)
        {
            Health.OnHealthChanged.AddListener(OnHealthChanged);
            CheckBerserkerState();
        }
    }

    private void OnHealthChanged(int currentHP, int maxHP)
    {
        CheckBerserkerState();
    }

    private void CheckBerserkerState()
    {
        if (Health == null) return;

        bool shouldBerserk = Health.IsAlive && Health.CurrentHP <= Health.MaxHP / 2;

        if (shouldBerserk && !isBerserking)
        {
            isBerserking = true;

            // Boost damage
            if (Class != null && Class.CombatData != null)
                Class.CombatData.projectileDamage += bonusDamage;

            // Disable parry
            if (parrySystem != null)
                parrySystem.ParryDisabled = true;
        }
        else if (!shouldBerserk && isBerserking)
        {
            isBerserking = false;

            if (Class != null && Class.CombatData != null)
                Class.CombatData.projectileDamage -= bonusDamage;

            if (parrySystem != null)
                parrySystem.ParryDisabled = false;
        }
    }

    public override void OnRoundStart()
    {
        // CombatData is re-cloned each round, so reset tracking
        isBerserking = false;
        CheckBerserkerState();
    }

    public override void OnRemove()
    {
        if (Health != null)
            Health.OnHealthChanged.RemoveListener(OnHealthChanged);

        if (isBerserking && parrySystem != null)
            parrySystem.ParryDisabled = false;
    }
}
