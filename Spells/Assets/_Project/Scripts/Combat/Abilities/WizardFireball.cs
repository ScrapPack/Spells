using UnityEngine;

/// <summary>
/// Wizard special ability: arcane shield.
/// Creates a blue shield around the player that grants invincibility and
/// reflects enemy projectiles back at their owner.
/// </summary>
public class WizardFireball : ClassAbility
{
    [Header("Shield Settings")]
    [SerializeField] private float shieldDuration = 2f;
    [SerializeField] private float shieldRadius = 1.5f;
    [SerializeField] private float reflectSpeedMultiplier = 1.2f;
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

        // Trigger collider for reflecting projectiles
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = shieldRadius;
        col.isTrigger = true;

        // Rigidbody required for trigger events
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Attach reflector component
        var reflector = go.AddComponent<ShieldReflector>();
        reflector.Initialize(Identity.PlayerID, reflectSpeedMultiplier);

        return go;
    }

    private void OnDestroy()
    {
        if (shieldVisual != null)
            Destroy(shieldVisual);
    }
}

/// <summary>
/// Attached to the shield visual. Reflects enemy projectiles on contact.
/// </summary>
public class ShieldReflector : MonoBehaviour
{
    private int ownerPlayerID;
    private float speedMultiplier;

    public void Initialize(int ownerID, float speedMult)
    {
        ownerPlayerID = ownerID;
        speedMultiplier = speedMult;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var proj = other.GetComponent<Projectile>();
        if (proj == null) return;

        // Only reflect enemy projectiles
        if (proj.OwnerPlayerID == ownerPlayerID) return;

        // Reflect back away from shield center
        Vector2 reflectDir = (other.transform.position - transform.position).normalized;
        proj.Reflect(ownerPlayerID, reflectDir, speedMultiplier);
    }
}
