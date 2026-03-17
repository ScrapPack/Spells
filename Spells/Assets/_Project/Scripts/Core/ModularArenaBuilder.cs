using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds arenas from ArenaLayoutData by composing modular ArenaPieceData entries.
/// Alternative to TestArenaBuilder — supports configurable arena designs.
///
/// Reads an ArenaLayoutData asset and instantiates geometry at runtime:
/// platforms, bridges, walls, ramps, kill zones, and spawn point markers.
/// Exposes spawn point arrays for player, monster, and chest placement.
///
/// Geometry creation methods mirror TestArenaBuilder's approach
/// (BoxCollider2D for platforms, PolygonCollider2D for ramps).
/// </summary>
public class ModularArenaBuilder : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private ArenaLayoutData layout;

    [Header("References")]
    [SerializeField] private MultiTargetCamera multiCamera;

    public Transform[] PlayerSpawnPoints { get; private set; }
    public Transform[] MonsterSpawnPoints { get; private set; }
    public Transform[] ChestSpawnPoints { get; private set; }
    public ArenaLayoutData CurrentLayout => layout;

    private GameObject arenaRoot;

    /// <summary>
    /// Build the arena from a layout asset. Can be called with a different layout to rebuild.
    /// </summary>
    public void Build(ArenaLayoutData overrideLayout = null)
    {
        if (overrideLayout != null)
            layout = overrideLayout;

        if (layout == null)
        {
            Debug.LogError("ModularArenaBuilder: No ArenaLayoutData assigned!");
            return;
        }

        // Clean up previous arena
        if (arenaRoot != null)
            Destroy(arenaRoot);

        arenaRoot = new GameObject($"Arena_{layout.arenaName}");

        var playerSpawns = new List<Transform>();
        var monsterSpawns = new List<Transform>();
        var chestSpawns = new List<Transform>();

        if (layout.pieces == null) return;

        foreach (var placement in layout.pieces)
        {
            if (placement == null || placement.piece == null) continue;

            switch (placement.piece.pieceType)
            {
                case ArenaPieceData.PieceType.Platform:
                    BuildPlatform(placement);
                    break;
                case ArenaPieceData.PieceType.Bridge:
                    BuildBridge(placement);
                    break;
                case ArenaPieceData.PieceType.Wall:
                    BuildWall(placement);
                    break;
                case ArenaPieceData.PieceType.Ramp:
                    BuildRamp(placement);
                    break;
                case ArenaPieceData.PieceType.Gap:
                    // Gaps are intentional empty space — no geometry
                    break;
                case ArenaPieceData.PieceType.SpawnPoint:
                    BuildSpawnPoint(placement, playerSpawns, monsterSpawns, chestSpawns);
                    break;
                case ArenaPieceData.PieceType.KillZone:
                    BuildKillZone(placement);
                    break;
                case ArenaPieceData.PieceType.MovingPlatform:
                    BuildMovingPlatform(placement);
                    break;
            }
        }

        PlayerSpawnPoints = playerSpawns.ToArray();
        MonsterSpawnPoints = monsterSpawns.ToArray();
        ChestSpawnPoints = chestSpawns.ToArray();

        Debug.Log($"ModularArenaBuilder: Built '{layout.arenaName}' — {layout.pieces.Length} pieces, " +
                  $"{playerSpawns.Count} player spawns, {monsterSpawns.Count} monster spawns, " +
                  $"{chestSpawns.Count} chest spawns.");
    }

    /// <summary>
    /// Destroy the current arena.
    /// </summary>
    public void Clear()
    {
        if (arenaRoot != null)
            Destroy(arenaRoot);

        PlayerSpawnPoints = null;
        MonsterSpawnPoints = null;
        ChestSpawnPoints = null;
    }

    // =========================================================
    // Piece Builders
    // =========================================================

    private void BuildPlatform(ArenaPlacement placement)
    {
        var piece = placement.piece;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Platform_{piece.pieceName}";
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);
        go.transform.localScale = new Vector3(piece.size.x, piece.size.y, 1);

        if (placement.rotation != 0f)
            go.transform.rotation = Quaternion.Euler(0, 0, placement.rotation);

        SetupVisual(go, piece.pieceColor);

        // Remove 3D collider, add 2D
        var collider3D = go.GetComponent<Collider>();
        if (collider3D != null) DestroyImmediate(collider3D);

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        go.layer = LayerMask.NameToLayer("Ground");
        if (go.layer == -1) go.layer = 0;
    }

    private void BuildBridge(ArenaPlacement placement)
    {
        // Bridges are narrow platforms — same geometry, different proportions
        var piece = placement.piece;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Bridge_{piece.pieceName}";
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);
        go.transform.localScale = new Vector3(piece.size.x, piece.size.y, 1);

        if (placement.rotation != 0f)
            go.transform.rotation = Quaternion.Euler(0, 0, placement.rotation);

        SetupVisual(go, piece.pieceColor);

        var collider3D = go.GetComponent<Collider>();
        if (collider3D != null) DestroyImmediate(collider3D);

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        go.layer = LayerMask.NameToLayer("Ground");
        if (go.layer == -1) go.layer = 0;
    }

    private void BuildWall(ArenaPlacement placement)
    {
        var piece = placement.piece;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall_{piece.pieceName}";
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);
        go.transform.localScale = new Vector3(piece.size.x, piece.size.y, 1);

        if (placement.rotation != 0f)
            go.transform.rotation = Quaternion.Euler(0, 0, placement.rotation);

        SetupVisual(go, piece.pieceColor);

        var collider3D = go.GetComponent<Collider>();
        if (collider3D != null) DestroyImmediate(collider3D);

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        // Walls use "Wall" layer for wall slide detection
        go.layer = LayerMask.NameToLayer("Wall");
        if (go.layer == -1)
        {
            go.layer = LayerMask.NameToLayer("Ground");
            if (go.layer == -1) go.layer = 0;
        }
    }

    private void BuildRamp(ArenaPlacement placement)
    {
        var piece = placement.piece;
        float halfW = piece.size.x / 2f;
        float halfH = piece.size.y / 2f;

        // Determine slope direction from rotation
        // 0° rotation = high on left (slopeLeft=true)
        // 180° rotation = high on right (slopeLeft=false)
        bool slopeLeft = Mathf.Abs(placement.rotation) < 90f;

        var go = new GameObject($"Ramp_{piece.pieceName}");
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);

        // Create visual mesh
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        SetupVisual(meshRenderer, piece.pieceColor);

        Vector3[] vertices;
        if (slopeLeft)
        {
            vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, -0.5f),
                new Vector3(halfW, -halfH, -0.5f),
                new Vector3(-halfW, halfH, -0.5f),
                new Vector3(-halfW, -halfH, 0.5f),
                new Vector3(halfW, -halfH, 0.5f),
                new Vector3(-halfW, halfH, 0.5f),
            };
        }
        else
        {
            vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, -0.5f),
                new Vector3(halfW, -halfH, -0.5f),
                new Vector3(halfW, halfH, -0.5f),
                new Vector3(-halfW, -halfH, 0.5f),
                new Vector3(halfW, -halfH, 0.5f),
                new Vector3(halfW, halfH, 0.5f),
            };
        }

        int[] triangles = new int[]
        {
            0, 2, 1,
            3, 4, 5,
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            0, 3, 5, 0, 5, 2,
        };

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // 2D polygon collider
        var polyCollider = go.AddComponent<PolygonCollider2D>();
        Vector2[] colliderPoints;
        if (slopeLeft)
        {
            colliderPoints = new Vector2[]
            {
                new Vector2(-halfW, -halfH),
                new Vector2(halfW, -halfH),
                new Vector2(-halfW, halfH),
            };
        }
        else
        {
            colliderPoints = new Vector2[]
            {
                new Vector2(-halfW, -halfH),
                new Vector2(halfW, -halfH),
                new Vector2(halfW, halfH),
            };
        }
        polyCollider.SetPath(0, colliderPoints);

        go.layer = LayerMask.NameToLayer("Ground");
        if (go.layer == -1) go.layer = 0;
    }

    private void BuildSpawnPoint(ArenaPlacement placement, List<Transform> playerSpawns,
        List<Transform> monsterSpawns, List<Transform> chestSpawns)
    {
        var piece = placement.piece;
        var go = new GameObject($"Spawn_{piece.spawnType}_{piece.pieceName}");
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);

        switch (piece.spawnType)
        {
            case ArenaPieceData.SpawnPointType.Player:
                playerSpawns.Add(go.transform);
                break;
            case ArenaPieceData.SpawnPointType.Monster:
                monsterSpawns.Add(go.transform);
                break;
            case ArenaPieceData.SpawnPointType.Chest:
                chestSpawns.Add(go.transform);
                break;
        }
    }

    private void BuildMovingPlatform(ArenaPlacement placement)
    {
        var piece = placement.piece;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"MovingPlatform_{piece.pieceName}";
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);
        go.transform.localScale = new Vector3(piece.size.x, piece.size.y, 1);

        SetupVisual(go, piece.pieceColor);

        // Remove 3D collider, add 2D
        var collider3D = go.GetComponent<Collider>();
        if (collider3D != null) DestroyImmediate(collider3D);

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        // Add MovingPlatform component with settings from piece data
        var mp = go.AddComponent<MovingPlatform>();
        mp.moveDirection = piece.moveDirection;
        mp.amplitude = piece.moveAmplitude;
        mp.speed = piece.moveSpeed;

        go.layer = LayerMask.NameToLayer("Ground");
        if (go.layer == -1) go.layer = 0;
    }

    private void BuildKillZone(ArenaPlacement placement)
    {
        var piece = placement.piece;
        var go = new GameObject($"KillZone_{piece.pieceName}");
        go.transform.parent = arenaRoot.transform;
        go.transform.position = new Vector3(placement.position.x, placement.position.y, 0);

        // Trigger collider for kill detection
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = piece.size;
        collider.isTrigger = true;

        // Add EnvironmentHazard for damage
        var hazard = go.AddComponent<EnvironmentHazard>();
        // EnvironmentHazard handles damage via its own serialized fields
        // The kill zone damage is set high enough to one-shot players

        // Visual indicator (semi-transparent)
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        spriteRenderer.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        go.transform.localScale = new Vector3(piece.size.x, piece.size.y, 1);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void SetupVisual(GameObject go, Color color)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
            SetupVisual(renderer, color);
    }

    private void SetupVisual(MeshRenderer renderer, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        renderer.material = mat;
    }
}
