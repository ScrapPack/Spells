using UnityEngine;

/// <summary>
/// Wires combat events together on a player.
/// Connects HealthSystem, ParrySystem, ScreenShake, Hitstop, and KillFeed.
/// This is the integration layer — individual systems stay decoupled,
/// this component routes events between them.
/// </summary>
public class CombatEventRouter : MonoBehaviour
{
    private HealthSystem health;
    private ParrySystem parry;
    private PlayerIdentity identity;
    private PlayerStateMachine stateMachine;
    private CombatAnalytics analytics;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        parry = GetComponent<ParrySystem>();
        identity = GetComponent<PlayerIdentity>();
        stateMachine = GetComponent<PlayerStateMachine>();
        analytics = Object.FindAnyObjectByType<CombatAnalytics>();

        if (health != null)
        {
            health.OnDamaged.AddListener(OnTookDamage);
            health.OnDeath.AddListener(OnDied);
        }

        if (parry != null)
        {
            parry.OnParrySuccess.AddListener(OnParrySuccess);
            parry.OnParryWhiff.AddListener(OnParryWhiff);
        }
    }

    private void OnTookDamage(float amount)
    {
        // Hitstop on damage
        if (Hitstop.Instance != null)
            Hitstop.Instance.StopOnHit();

        // Screen shake on damage
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.ShakeOnHit();

        // Analytics
        if (analytics != null && identity != null)
        {
            analytics.RecordDamageReceived(identity.PlayerID, amount);
            if (health.LastAttackerID >= 0)
                analytics.RecordDamageDealt(health.LastAttackerID, amount);
        }
    }

    private void OnDied()
    {
        // Big hitstop and shake on kill
        if (Hitstop.Instance != null)
            Hitstop.Instance.StopOnKill();
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.ShakeOnKill();

        // Report to kill feed with proper kill credit
        var killFeed = Object.FindAnyObjectByType<KillFeed>();
        if (killFeed != null && identity != null)
        {
            string victimName = $"Player {identity.PlayerID + 1}";
            string killerName = null;
            Color killerColor = Color.white;

            // Look up killer identity from LastAttackerID
            if (health.LastAttackerID >= 0)
            {
                killerName = $"Player {health.LastAttackerID + 1}";

                // Try to get killer's class color
                var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
                foreach (var pi in allPlayers)
                {
                    if (pi.PlayerID == health.LastAttackerID)
                    {
                        var cm = pi.GetComponent<ClassManager>();
                        if (cm != null && cm.CurrentClass != null)
                            killerColor = cm.CurrentClass.classColor;
                        break;
                    }
                }
            }

            killFeed.AddElimination(victimName, killerName, killerColor);
        }

        // Analytics: record death and kill credit
        if (analytics != null && identity != null)
        {
            analytics.RecordDeath(identity.PlayerID);
            if (health.LastAttackerID >= 0)
                analytics.RecordKill(health.LastAttackerID);
        }
    }

    private void OnParrySuccess()
    {
        // Hitstop and shake on parry (more dramatic than regular hit)
        if (Hitstop.Instance != null)
            Hitstop.Instance.StopOnParry();
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.ShakeOnParry();

        // Analytics
        if (analytics != null && identity != null)
            analytics.RecordParrySuccess(identity.PlayerID);
    }

    private void OnParryWhiff()
    {
        // Analytics
        if (analytics != null && identity != null)
            analytics.RecordParryFail(identity.PlayerID);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged.RemoveListener(OnTookDamage);
            health.OnDeath.RemoveListener(OnDied);
        }
        if (parry != null)
        {
            parry.OnParrySuccess.RemoveListener(OnParrySuccess);
            parry.OnParryWhiff.RemoveListener(OnParryWhiff);
        }
    }
}
