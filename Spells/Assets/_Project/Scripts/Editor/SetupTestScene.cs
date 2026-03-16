using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a fully-wired combat test scene with all game managers,
/// UI, arena, and player spawning configured.
///
/// Menu: Spells → Setup Test Scene
///
/// Prerequisites: Run "Setup MVP Assets" and "Setup Player Prefab" first.
/// </summary>
public class SetupTestScene : Editor
{
    private static readonly string DataRoot = "Assets/_Project/Data";

    [MenuItem("Spells/Setup Test Scene", false, 102)]
    public static void Setup()
    {
        // Verify prerequisites
        var wizardClass = AssetDatabase.LoadAssetAtPath<ClassData>($"{DataRoot}/Classes/Wizard.asset");
        var warriorClass = AssetDatabase.LoadAssetAtPath<ClassData>($"{DataRoot}/Classes/Warrior.asset");
        var gameSettings = AssetDatabase.LoadAssetAtPath<GameSettings>($"{DataRoot}/Settings/DefaultGameSettings.asset");
        var sharedMovement = AssetDatabase.LoadAssetAtPath<MovementData>($"{DataRoot}/Movement/SharedMovement.asset");
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/PlayerCharacter.prefab");

        if (wizardClass == null || warriorClass == null || gameSettings == null)
        {
            EditorUtility.DisplayDialog("Missing Assets",
                "Run 'Spells → Setup MVP Assets' first to create the required data assets.",
                "OK");
            return;
        }

        if (playerPrefab == null)
        {
            EditorUtility.DisplayDialog("Missing Prefab",
                "PlayerCharacter prefab not found. Ensure it exists at:\nAssets/_Project/Prefabs/Player/PlayerCharacter.prefab",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Setup Test Scene",
            "This will create a new scene 'CombatTestArena' with:\n\n" +
            "• Game managers (Match, Round, Draft)\n" +
            "• UI (HUD, KillFeed, Scoreboard, etc.)\n" +
            "• Camera with MultiTargetCamera\n" +
            "• Arena builder with platforms\n" +
            "• Player spawn system\n\n" +
            "The current scene will be saved first.",
            "Create Scene",
            "Cancel"))
        {
            return;
        }

        // Save current scene
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CombatTestArena";

        // ── Camera ──
        var cameraGo = new GameObject("Main Camera");
        var camera = cameraGo.AddComponent<Camera>();
        cameraGo.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 15f;
        camera.transform.position = new Vector3(0, 4, -10);
        camera.backgroundColor = new Color(0.08f, 0.06f, 0.12f); // Dark fantasy
        camera.clearFlags = CameraClearFlags.SolidColor;
        var multiCam = cameraGo.AddComponent<MultiTargetCamera>();
        cameraGo.AddComponent<AudioListener>();

        // ── Game Managers ──
        var managersGo = new GameObject("GameManagers");

        var matchManager = managersGo.AddComponent<MatchManager>();
        var roundManager = managersGo.AddComponent<RoundManager>();
        var draftManager = managersGo.AddComponent<DraftManager>();
        var charSelectManager = managersGo.AddComponent<CharacterSelectManager>();
        var analyticsComp = managersGo.AddComponent<CombatAnalytics>();

        // SpellEffectRegistry is a singleton that registers itself
        managersGo.AddComponent<SpellEffectRegistry>();

        // ── Wire manager references via SerializedObject ──
        WireMatchManager(matchManager, roundManager, draftManager, charSelectManager,
            analyticsComp, multiCam);

        // ── Singletons (game feel) ──
        var feelGo = new GameObject("GameFeel");
        feelGo.AddComponent<ScreenShake>();
        feelGo.AddComponent<Hitstop>();

        // ── UI Canvas (all OnGUI-based, no Canvas needed) ──
        var uiGo = new GameObject("UI");
        uiGo.AddComponent<CombatHUD>();
        uiGo.AddComponent<KillFeed>();
        uiGo.AddComponent<Scoreboard>();
        uiGo.AddComponent<RoundAnnouncer>();
        uiGo.AddComponent<RoundTimerUI>();
        uiGo.AddComponent<DraftUI>();
        uiGo.AddComponent<CharacterSelectUI>();
        uiGo.AddComponent<PostMatchSummary>();

        // ── Arena Builder ──
        var arenaGo = new GameObject("ArenaSetup");
        var arenaBuilder = arenaGo.AddComponent<TestArenaBuilder>();

        // Wire arena builder's serialized fields
        var arenaSerObj = new SerializedObject(arenaBuilder);
        SetField(arenaSerObj, "movementData", sharedMovement);
        SetField(arenaSerObj, "playerPrefab", playerPrefab);
        arenaSerObj.ApplyModifiedProperties();

        // ── Player Spawn Manager (on same object as arena) ──
        var spawnManager = arenaGo.AddComponent<PlayerSpawnManager>();
        var spawnSerObj = new SerializedObject(spawnManager);
        SetField(spawnSerObj, "defaultClassData", wizardClass); // Default class for testing
        SetField(spawnSerObj, "multiTargetCamera", multiCam);
        SetField(spawnSerObj, "matchManager", matchManager);
        spawnSerObj.ApplyModifiedProperties();

        // ── Lighting ──
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.9f, 0.85f, 0.75f);
        light.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Save scene ──
        string scenePath = "Assets/Scenes/CombatTestArena.unity";
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log($"[Spells] ✓ Test scene created at: {scenePath}");
        Debug.Log("[Spells] To play: Open the scene, enter Play mode, press any button to join as a player.");

        EditorUtility.DisplayDialog("Scene Created",
            $"Combat test scene saved to:\n{scenePath}\n\n" +
            "To test:\n" +
            "1. Open the scene\n" +
            "2. Enter Play mode\n" +
            "3. Press any button/key to join\n" +
            "4. Use WASD + mouse to play\n\n" +
            "Default class: Wizard\n" +
            "Up to 4 players can join with separate input devices.",
            "OK");
    }

    private static void WireMatchManager(MatchManager match, RoundManager round,
        DraftManager draft, CharacterSelectManager charSelect,
        CombatAnalytics analytics, MultiTargetCamera camera)
    {
        var serObj = new SerializedObject(match);
        SetField(serObj, "roundManager", round);
        SetField(serObj, "draftManager", draft);
        SetField(serObj, "multiCamera", camera);
        SetField(serObj, "charSelect", charSelect);
        SetField(serObj, "analytics", analytics);

        // Wire other references that might exist
        var announcer = Object.FindAnyObjectByType<RoundAnnouncer>();
        var killFeed = Object.FindAnyObjectByType<KillFeed>();
        if (announcer != null) SetField(serObj, "announcer", announcer);
        if (killFeed != null) SetField(serObj, "killFeed", killFeed);

        serObj.ApplyModifiedProperties();
    }

    private static void SetField(SerializedObject serObj, string fieldName, Object value)
    {
        var prop = serObj.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning($"[Spells] Field '{fieldName}' not found on {serObj.targetObject.GetType().Name}");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
