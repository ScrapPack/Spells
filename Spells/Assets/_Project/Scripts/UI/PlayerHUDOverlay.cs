using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space HUD that renders a health bar and ammo squares above each player.
/// Call Initialize() after all players are spawned.
/// </summary>
public class PlayerHUDOverlay : MonoBehaviour
{
    [Tooltip("How many world units above the player the HUD sits.")]
    [SerializeField] private float worldOffsetY = 1.6f;

    [Tooltip("Pixel size of each ammo square.")]
    [SerializeField] private float ammoSquareSize = 14f;

    [Tooltip("Pixel gap between ammo squares.")]
    [SerializeField] private float ammoSpacing = 3f;

    [Tooltip("Pixel width of the health bar.")]
    [SerializeField] private float healthBarWidth = 66f;

    [Tooltip("Pixel height of the health bar.")]
    [SerializeField] private float healthBarHeight = 9f;

    [Tooltip("Pixel gap between the health bar and the ammo row above it.")]
    [SerializeField] private float rowSpacing = 4f;

    // ─────────────────────────────────────────────────────────────────────────

    private struct PlayerHUD
    {
        public Transform       playerTransform;
        public HealthSystem    health;
        public ProjectileSpawner spawner;
        public RectTransform   root;
        public Image           healthBarFill;
        public Image[]         ammoSquares;
    }

    private PlayerHUD[]    huds;
    private RectTransform  canvasRect;
    private Camera         cam;

    // ─────────────────────────────────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────────────────────────────────

    public void Initialize(GameObject[] players)
    {
        cam = Camera.main;

        // Screen-space overlay canvas — ConstantPixelSize (default) so
        // WorldToScreenPoint pixel coords match canvas local coords 1:1.
        var canvasGo = new GameObject("PlayerHUDCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;          // below card-pick (90) and game-over (100)
        canvasGo.AddComponent<CanvasScaler>(); // leaves ScaleMode at ConstantPixelSize
        canvasRect = canvas.GetComponent<RectTransform>();

        huds = new PlayerHUD[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            huds[i] = BuildHUD(canvasGo.transform, players[i], i);
        }
    }

    private PlayerHUD BuildHUD(Transform parent, GameObject player, int index)
    {
        var hud = new PlayerHUD
        {
            playerTransform = player.transform,
            health          = player.GetComponent<HealthSystem>(),
            spawner         = player.GetComponent<ProjectileSpawner>()
        };

        // Root — anchored to canvas centre so anchoredPosition == canvas-local offset.
        var rootGo = new GameObject($"HUD_P{index + 1}");
        rootGo.transform.SetParent(parent, false);
        hud.root            = rootGo.AddComponent<RectTransform>();
        hud.root.anchorMin  = new Vector2(0.5f, 0.5f);
        hud.root.anchorMax  = new Vector2(0.5f, 0.5f);
        hud.root.pivot      = new Vector2(0.5f, 0f);   // bottom-centre — floats above player
        hud.root.sizeDelta  = Vector2.zero;

        // ── Health bar ────────────────────────────────────────────────────────
        // Dark background
        var bgGo  = new GameObject("HP_BG");
        bgGo.transform.SetParent(rootGo.transform, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        var bgRt  = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin        = new Vector2(0.5f, 0f);
        bgRt.anchorMax        = new Vector2(0.5f, 0f);
        bgRt.pivot            = new Vector2(0.5f, 0f);
        bgRt.sizeDelta        = new Vector2(healthBarWidth, healthBarHeight);
        bgRt.anchoredPosition = new Vector2(0f, rowSpacing);

        // Green fill
        var fillGo = new GameObject("HP_Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        hud.healthBarFill            = fillGo.AddComponent<Image>();
        hud.healthBarFill.color      = new Color(0.2f, 0.85f, 0.25f);
        hud.healthBarFill.type       = Image.Type.Filled;
        hud.healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        hud.healthBarFill.fillOrigin = 0; // fills left → right
        var fillRt  = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin  = Vector2.zero;
        fillRt.anchorMax  = Vector2.one;
        fillRt.offsetMin  = Vector2.zero;
        fillRt.offsetMax  = Vector2.zero;

        // ── Ammo squares ──────────────────────────────────────────────────────
        int   maxAmmo = hud.spawner != null ? hud.spawner.MaxAmmo : 3;
        float totalW  = maxAmmo * ammoSquareSize + (maxAmmo - 1) * ammoSpacing;
        float startX  = -totalW * 0.5f + ammoSquareSize * 0.5f;
        float ammoY   = rowSpacing + healthBarHeight + rowSpacing;

        hud.ammoSquares = new Image[maxAmmo];
        for (int j = 0; j < maxAmmo; j++)
        {
            var sqGo  = new GameObject($"Ammo_{j}");
            sqGo.transform.SetParent(rootGo.transform, false);
            var sq    = sqGo.AddComponent<Image>();
            sq.color  = Color.white;

            var rt             = sqGo.GetComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f, 0f);
            rt.anchorMax       = new Vector2(0.5f, 0f);
            rt.pivot           = new Vector2(0.5f, 0f);
            rt.sizeDelta       = new Vector2(ammoSquareSize, ammoSquareSize);
            rt.anchoredPosition = new Vector2(startX + j * (ammoSquareSize + ammoSpacing), ammoY);

            hud.ammoSquares[j] = sq;
        }

        return hud;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-frame update
    // ─────────────────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (huds == null || !cam) return;

        for (int i = 0; i < huds.Length; i++)
        {
            ref var hud = ref huds[i];
            if (!hud.root || hud.playerTransform == null) continue;

            // Convert world position above the player into canvas-local position.
            Vector3 worldPos  = hud.playerTransform.position + Vector3.up * worldOffsetY;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPos);
            hud.root.anchoredPosition = localPos;

            // Health bar — green → red as HP drops
            if (hud.healthBarFill && hud.health != null)
            {
                float t = hud.health.MaxHP > 0
                    ? (float)hud.health.CurrentHP / hud.health.MaxHP
                    : 0f;
                hud.healthBarFill.fillAmount = t;
                hud.healthBarFill.color = Color.Lerp(
                    new Color(0.9f, 0.15f, 0.1f),   // red   (0 HP)
                    new Color(0.2f, 0.85f, 0.25f),   // green (full HP)
                    t);
            }

            // Ammo squares — white when loaded, dark when spent
            if (hud.ammoSquares != null && hud.spawner != null)
            {
                int cur = hud.spawner.CurrentAmmo;
                for (int j = 0; j < hud.ammoSquares.Length; j++)
                {
                    if (!hud.ammoSquares[j]) continue;
                    hud.ammoSquares[j].color = j < cur
                        ? Color.white
                        : new Color(0.18f, 0.18f, 0.18f, 0.55f);
                }
            }
        }
    }
}
