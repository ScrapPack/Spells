using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural explosion ring visual. Spawned by ExplosiveBehavior at the point of impact.
/// Draws an expanding circle using a LineRenderer — no art assets required.
/// Scales with explosion radius so the NUKE produces a much larger ring than a normal
/// Explosive Round.
/// </summary>
public class ExplosionVFX : MonoBehaviour
{
    private const int Segments = 40;

    /// <summary>
    /// Spawn an explosion ring at <paramref name="position"/> scaled to <paramref name="radius"/>.
    /// </summary>
    public static void Spawn(Vector2 position, float radius)
    {
        var go = new GameObject("ExplosionVFX");
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.AddComponent<ExplosionVFX>().StartCoroutine(go.GetComponent<ExplosionVFX>().Animate(radius));
    }

    private IEnumerator Animate(float radius)
    {
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = Segments;
        lr.sortingOrder  = 20;
        lr.material      = new Material(Shader.Find("Sprites/Default"));

        float duration = Mathf.Lerp(0.25f, 0.5f, radius / 10f); // bigger = longer
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Expand quickly at first, then ease off
            float currentRadius = radius * (1f - Mathf.Pow(1f - t, 2f));
            float alpha         = 1f - t;
            float lineWidth     = Mathf.Lerp(radius * 0.2f, 0.05f, t);

            lr.startWidth = lineWidth;
            lr.endWidth   = lineWidth;

            // Orange core → yellow edge → transparent
            Color c = Color.Lerp(new Color(1f, 0.4f, 0f, 1f), new Color(1f, 1f, 0.2f, 0f), t);
            c.a = alpha;
            lr.startColor = c;
            lr.endColor   = c;

            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius,
                    0f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
