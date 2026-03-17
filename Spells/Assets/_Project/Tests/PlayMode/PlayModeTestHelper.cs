using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper to build minimal test scenes for PlayMode physics tests.
/// Creates ground, walls, slopes, and players with TestInputProvider.
/// </summary>
public static class PlayModeTestHelper
{
    public struct TestPlayer
    {
        public GameObject gameObject;
        public TestInputProvider input;
        public PlayerController controller;
        public PlayerStateMachine stateMachine;
        public PhysicsCheck physics;
        public Rigidbody2D rb;
    }

    /// <summary>
    /// Spawn a test player at the given position with a TestInputProvider.
    /// Call this BEFORE waiting for Start() to fire.
    /// </summary>
    public static TestPlayer SpawnTestPlayer(Vector3 position)
    {
        var go = new GameObject("TestPlayer");
        go.transform.position = position;

        // Rigidbody2D
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1f);

        // Zero-friction material (matching PlayerController.Start)
        var mat = new PhysicsMaterial2D("TestPlayerMat");
        mat.friction = 0f;
        mat.bounciness = 0f;
        rb.sharedMaterial = mat;

        // PhysicsCheck (auto-detects layers in Awake)
        var physics = go.AddComponent<PhysicsCheck>();

        // Load the ACTUAL SharedMovement.asset so tests use real game values.
        // This is critical for autoresearch — modifying the .asset must affect tests.
        MovementData movementData = null;
#if UNITY_EDITOR
        movementData = AssetDatabase.LoadAssetAtPath<MovementData>(
            "Assets/_Project/Data/Movement/SharedMovement.asset");
#endif
        if (movementData == null)
            movementData = ScriptableObject.CreateInstance<MovementData>();

        // PlayerController — AddComponent triggers Awake() synchronously,
        // which checks baseMovementData (null at this point) and logs an error.
        // Suppress this expected error, then inject Data via reflection.
        LogAssert.Expect(LogType.Error, "PlayerController: No MovementData assigned!");

        var controller = go.AddComponent<PlayerController>();

        // Inject both baseMovementData AND Data — Awake already ran and
        // failed to clone, so Data is still null. We set it directly.
        var baseField = typeof(PlayerController).GetField("baseMovementData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (baseField != null)
            baseField.SetValue(controller, movementData);

        var dataProp = typeof(PlayerController).GetProperty("Data",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (dataProp != null)
            dataProp.SetValue(controller, movementData.Clone());

        // TestInputProvider BEFORE PlayerStateMachine
        // (StateMachine.Start looks for IInputProvider via GetComponent)
        var input = go.AddComponent<TestInputProvider>();

        // PlayerStateMachine last — its Start() will find Controller.Data != null
        var stateMachine = go.AddComponent<PlayerStateMachine>();

        return new TestPlayer
        {
            gameObject = go,
            input = input,
            controller = controller,
            stateMachine = stateMachine,
            physics = physics,
            rb = rb
        };
    }

    /// <summary>Create a flat ground platform.</summary>
    public static GameObject CreateGround(Vector3 position, float width = 20f, float height = 1f)
    {
        var ground = new GameObject("TestGround");
        ground.transform.position = position;
        ground.layer = LayerMask.NameToLayer("Ground");

        var col = ground.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);

        var rb = ground.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        return ground;
    }

    /// <summary>Create a vertical wall on the "Wall" layer.</summary>
    public static GameObject CreateWall(Vector3 position, float width = 1f, float height = 10f)
    {
        var wall = new GameObject("TestWall");
        wall.transform.position = position;
        wall.layer = LayerMask.NameToLayer("Wall");

        var col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        return wall;
    }

    /// <summary>Create a slope (rotated ground surface).</summary>
    public static GameObject CreateSlope(Vector3 position, float angle, float length = 5f)
    {
        var slope = new GameObject("TestSlope");
        slope.transform.position = position;
        slope.transform.rotation = Quaternion.Euler(0, 0, angle);
        slope.layer = LayerMask.NameToLayer("Ground");

        var col = slope.AddComponent<BoxCollider2D>();
        col.size = new Vector2(length, 0.5f);

        var rb = slope.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        return slope;
    }

    /// <summary>Clean up all test objects.</summary>
    public static void Cleanup(params GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
                Object.Destroy(obj);
        }
    }
}
