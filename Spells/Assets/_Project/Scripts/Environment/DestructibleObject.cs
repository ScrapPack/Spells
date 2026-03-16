using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Object that can be destroyed by projectiles or player interaction.
/// Used for: torches (shooting removes light + creates fire hazard),
/// platforms (crack after being stood on), barricades, crates.
///
/// Destruction is visual and gameplay-affecting:
/// - Removes collider (no longer blocks movement)
/// - Optionally spawns hazard on destruction
/// - Can have HP for multi-hit destruction
/// </summary>
public class DestructibleObject : MonoBehaviour
{
    [Header("Durability")]
    [Tooltip("Hits required to destroy. 0 = indestructible")]
    [SerializeField] private int hitPoints = 1;

    [Header("Destruction")]
    [Tooltip("Delay before actually removing the object (for break animation)")]
    [SerializeField] private float destructionDelay = 0.1f;
    [Tooltip("Prefab spawned at destruction point (fire, debris, etc.)")]
    [SerializeField] private GameObject destructionEffectPrefab;
    [Tooltip("Prefab spawned as a hazard at destruction point")]
    [SerializeField] private GameObject hazardPrefab;

    [Header("Cracking (multi-hit)")]
    [Tooltip("Sprites to swap as damage accumulates (index 0 = pristine, last = about to break)")]
    [SerializeField] private Sprite[] damageStages;

    [Header("Events")]
    public UnityEvent OnHit;
    public UnityEvent OnDestroyed;

    private int currentHP;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private int lastAttackerID = -1;

    private void Awake()
    {
        currentHP = hitPoints;
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Called when a projectile hits this object.
    /// </summary>
    public void TakeHit(int damage = 1)
    {
        if (hitPoints <= 0 || currentHP <= 0) return; // Indestructible or already broken

        currentHP -= damage;
        OnHit?.Invoke();

        // Update visual damage stage
        if (damageStages != null && damageStages.Length > 0 && spriteRenderer != null)
        {
            int stageIndex = Mathf.Clamp(
                (hitPoints - currentHP) * damageStages.Length / hitPoints,
                0, damageStages.Length - 1
            );
            spriteRenderer.sprite = damageStages[stageIndex];
        }

        if (currentHP <= 0)
        {
            Destroy();
        }
    }

    private void Destroy()
    {
        OnDestroyed?.Invoke();

        // Spawn destruction effect
        if (destructionEffectPrefab != null)
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);

        // Spawn hazard (with kill credit to whoever destroyed this object)
        if (hazardPrefab != null)
        {
            var hazardObj = Instantiate(hazardPrefab, transform.position, Quaternion.identity);
            var hazard = hazardObj.GetComponent<EnvironmentHazard>();
            if (hazard != null && lastAttackerID >= 0)
                hazard.SetSourcePlayer(lastAttackerID);
        }

        // Disable collider immediately (gameplay), destroy object after delay (visuals)
        if (col != null)
            col.enabled = false;

        if (destructionDelay > 0f)
            Object.Destroy(gameObject, destructionDelay);
        else
            Object.Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Projectiles destroy this object — track who did it for kill credit
        var projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            lastAttackerID = projectile.OwnerPlayerID;
            TakeHit(Mathf.RoundToInt(projectile.Damage));
        }
    }
}
