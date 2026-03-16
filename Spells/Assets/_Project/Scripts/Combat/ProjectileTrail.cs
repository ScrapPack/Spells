using UnityEngine;

/// <summary>
/// Adds a visual trail to projectiles using Unity's TrailRenderer.
/// Color and width can be customized per class.
/// Automatically configured if a TrailRenderer is on the projectile prefab,
/// or creates one dynamically.
/// </summary>
[RequireComponent(typeof(Projectile))]
public class ProjectileTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private Color trailColor = new Color(0.5f, 0.7f, 1f, 1f);
    [SerializeField] private float trailWidth = 0.15f;
    [SerializeField] private float trailDuration = 0.2f;
    [SerializeField] private int cornerVertices = 2;

    [Header("Fade")]
    [SerializeField] private Color trailEndColor = new Color(0.5f, 0.7f, 1f, 0f);

    private TrailRenderer trail;

    private void Start()
    {
        trail = GetComponent<TrailRenderer>();

        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        ConfigureTrail();
    }

    private void ConfigureTrail()
    {
        if (trail == null) return;

        trail.time = trailDuration;
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth * 0.3f;
        trail.numCornerVertices = cornerVertices;
        trail.numCapVertices = 2;
        trail.minVertexDistance = 0.05f;

        // Gradient: start color → transparent
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailEndColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(trailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;

        // Use default sprite material for 2D rendering
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }

    /// <summary>
    /// Override trail color (e.g., for reflected projectiles or class-specific colors).
    /// </summary>
    public void SetColor(Color color)
    {
        trailColor = color;
        trailEndColor = new Color(color.r, color.g, color.b, 0f);

        if (trail != null)
            ConfigureTrail();
    }
}
