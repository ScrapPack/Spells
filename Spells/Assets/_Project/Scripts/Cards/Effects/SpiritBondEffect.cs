using UnityEngine;

/// <summary>
/// Spirit Bond (Shaman, Tier 1): Summons share your movement (they mirror you).
/// Summons also share your damage (you take hits when they do).
///
/// GDD: "Summons share your movement (they mirror you).
/// Summons also share your damage (you take hits when they do)."
///
/// Creates a mirrored spirit entity that follows the player on the
/// opposite side. The spirit acts as a second hitbox — projectiles
/// that hit the spirit deal damage to the player. Effectively doubles
/// your offensive presence but also doubles your target area.
///
/// Stacking: Each stack creates an additional spirit at a different offset.
/// </summary>
public class SpiritBondEffect : SpellEffect
{
    private int spiritCount;
    private GameObject[] spirits;

    protected override void OnApply()
    {
        spiritCount = StackCount;
        SpawnSpirits();
    }

    public override void OnRoundStart()
    {
        DestroySpirits();
        SpawnSpirits();
    }

    private void SpawnSpirits()
    {
        if (Identity == null || Health == null) return;

        spirits = new GameObject[spiritCount];

        for (int i = 0; i < spiritCount; i++)
        {
            // Distribute spirits around the player
            float angle = (360f / spiritCount) * i + 180f; // Start opposite
            float dist = 2f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * dist,
                Mathf.Sin(angle * Mathf.Deg2Rad) * dist * 0.3f, // Flatten Y offset
                0f
            );

            var spiritObj = new GameObject($"Spirit_P{Identity.PlayerID}_{i}");
            spiritObj.AddComponent<Rigidbody2D>();
            spiritObj.AddComponent<CircleCollider2D>();

            // Add a visual (simple sprite)
            var sr = spiritObj.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.8f, 1f, 0.5f); // Ghostly blue

            var spirit = spiritObj.AddComponent<SpiritEntity>();
            spirit.Initialize(
                transform,
                Identity.PlayerID,
                Health,
                offset,
                1f // 1 damage shared per hit
            );

            spirits[i] = spiritObj;
        }
    }

    private void DestroySpirits()
    {
        if (spirits == null) return;

        for (int i = 0; i < spirits.Length; i++)
        {
            if (spirits[i] != null)
                Object.Destroy(spirits[i]);
        }
        spirits = null;
    }

    public override void OnRemove()
    {
        DestroySpirits();
    }
}
