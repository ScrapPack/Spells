using UnityEngine;

/// <summary>
/// The shrinking arena boundary. Players outside the boundary take damage.
/// Syncs with RoundManager.ZoomProgress to shrink over time.
///
/// Works as an inverse safe zone: players inside the zone are safe,
/// players outside take periodic damage. The zone shrinks as the round
/// progresses, forcing players into tighter spaces.
///
/// Visual: could be a ring of fire, energy field, or just a damage zone.
/// For now, uses a simple box bounds check.
/// </summary>
public class ArenaZone : MonoBehaviour
{
    [Header("Bounds")]
    [Tooltip("Full arena size at round start")]
    [SerializeField] private Vector2 fullSize = new Vector2(30f, 20f);
    [Tooltip("Minimum arena size at full compression")]
    [SerializeField] private Vector2 minSize = new Vector2(8f, 6f);
    [Tooltip("Center of the arena")]
    [SerializeField] private Vector2 center = Vector2.zero;

    [Header("Damage")]
    [Tooltip("Damage per tick when outside the zone")]
    [SerializeField] private float outOfBoundsDamage = 1f;
    [Tooltip("Seconds between damage ticks")]
    [SerializeField] private float damageCooldown = 1f;
    [Tooltip("Knockback toward arena center when hit")]
    [SerializeField] private float pushForce = 5f;

    [Header("References")]
    [SerializeField] private RoundManager roundManager;

    public Vector2 CurrentSize { get; private set; }
    public Rect CurrentBounds { get; private set; }

    private float damageTimer;
    private readonly System.Collections.Generic.Dictionary<int, float> playerCooldowns =
        new System.Collections.Generic.Dictionary<int, float>();

    private void Start()
    {
        CurrentSize = fullSize;
        UpdateBounds();
    }

    private void Update()
    {
        if (roundManager == null || !roundManager.RoundActive) return;

        // Shrink based on zoom progress
        float zoom = roundManager.ZoomProgress;
        CurrentSize = Vector2.Lerp(fullSize, minSize, zoom);
        UpdateBounds();

        // Tick player cooldowns
        var keys = new System.Collections.Generic.List<int>(playerCooldowns.Keys);
        foreach (int key in keys)
        {
            if (playerCooldowns[key] > 0f)
                playerCooldowns[key] -= Time.deltaTime;
        }

        // Check all alive players
        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            var health = player.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            Vector2 pos = player.transform.position;
            if (!CurrentBounds.Contains(pos))
            {
                DamageOutOfBoundsPlayer(player, health);
            }
        }
    }

    private void DamageOutOfBoundsPlayer(PlayerIdentity player, HealthSystem health)
    {
        int pid = player.PlayerID;

        // Check cooldown
        if (playerCooldowns.ContainsKey(pid) && playerCooldowns[pid] > 0f)
            return;

        // Apply damage (no attacker — environmental)
        health.TakeDamage(outOfBoundsDamage, -1);

        // Push toward center
        if (pushForce > 0f)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 toCenter = (center - (Vector2)player.transform.position).normalized;
                rb.linearVelocity += toCenter * pushForce;
            }
        }

        playerCooldowns[pid] = damageCooldown;
    }

    private void UpdateBounds()
    {
        float halfW = CurrentSize.x * 0.5f;
        float halfH = CurrentSize.y * 0.5f;
        CurrentBounds = new Rect(center.x - halfW, center.y - halfH, CurrentSize.x, CurrentSize.y);
    }

    /// <summary>
    /// Reset to full size for new round.
    /// </summary>
    public void ResetForRound()
    {
        CurrentSize = fullSize;
        UpdateBounds();
        playerCooldowns.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize bounds in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, CurrentSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, minSize);
    }
}
