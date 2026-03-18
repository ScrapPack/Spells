using UnityEngine;

/// <summary>
/// Wizard special ability: arcane shield.
/// Creates a blue shield around the player that grants invincibility.
/// The shield is on the Wall layer so projectiles bounce off it like any wall,
/// and it physically blocks other players.
/// </summary>
public class WizardFireball : ClassAbility
{
    [Header("Shield Settings")]
    [SerializeField] private float shieldDuration = 2f;
    [SerializeField] private float shieldRadius = 1.5f;
    [SerializeField] private float shieldKnockback = 20f;
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.5f, 1f, 0.4f);

    private GameObject shieldVisual;
    private float shieldTimer;

    protected override void Start()
    {
        base.Start();
        abilityName = "Arcane Shield";
        cooldownDuration = 8f;
    }

    protected override void Activate()
    {
        if (Health == null) return;

        Health.GrantInvincibility(shieldDuration);

        if (shieldVisual != null)
            Destroy(shieldVisual);

        shieldVisual = CreateShieldVisual();
        shieldTimer = shieldDuration;
        IsActive = true;

        KnockbackPlayersInRadius();
    }

    private void KnockbackPlayersInRadius()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        var hits = Physics2D.OverlapCircleAll(transform.position, shieldRadius, 1 << playerLayer);
        foreach (var hit in hits)
        {
            // Skip self
            var id = hit.GetComponent<PlayerIdentity>();
            if (id != null && id.PlayerID == Identity.PlayerID) continue;

            var hitRb = hit.GetComponent<Rigidbody2D>();
            if (hitRb == null) continue;

            Vector2 dir = (hit.transform.position - transform.position).normalized;
            // Default to right if standing directly on top
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            hitRb.linearVelocity = dir * shieldKnockback;
        }
    }

    protected override void Tick()
    {
        shieldTimer -= Time.deltaTime;

        if (shieldVisual != null)
        {
            shieldVisual.transform.position = transform.position;

            // Fade out in the last 0.5 seconds
            if (shieldTimer < 0.5f)
            {
                var sr = shieldVisual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = Mathf.Clamp01(shieldTimer / 0.5f) * shieldColor.a;
                    sr.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, alpha);
                }
            }
        }

        if (shieldTimer <= 0f)
        {
            if (shieldVisual != null)
                Destroy(shieldVisual);
            EndAbility();
        }
    }

    private GameObject CreateShieldVisual()
    {
        var go = new GameObject("ArcaneShield");
        go.transform.position = transform.position;

        // Wall layer so projectiles bounce off it using existing wall-bounce logic
        go.layer = LayerMask.NameToLayer("Wall");

        // Visual
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = shieldColor;
        sr.sortingOrder = 10;

        int texSize = 128;
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        float center = texSize / 2f;
        float outerRadius = center;
        float innerRadius = center - 8f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                    tex.SetPixel(x, y, Color.white);
                else if (dist < innerRadius)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.1f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), texSize / (shieldRadius * 2f));

        // Non-trigger collider on Wall layer: blocks players and bounces projectiles
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = shieldRadius;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Let the shield owner move freely inside their own shield
        var ownerCollider = GetComponent<Collider2D>();
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(col, ownerCollider);

        return go;
    }

    private void OnDestroy()
    {
        if (shieldVisual != null)
            Destroy(shieldVisual);
    }
}
