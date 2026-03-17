using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a fully-wired movement test scene with the arena builder,
/// camera, and player spawning configured.
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
        var sharedMovement = AssetDatabase.LoadAssetAtPath<MovementData>($"{DataRoot}/Movement/SharedMovement.asset");
        var playerPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/PlayerCharacter.prefab");

        if (sharedMovement == null)
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
            "• TestArenaBuilder (static + procedural arena)\n" +
            "• MultiTargetCamera\n" +
            "• SpellEffectRegistry\n" +
            "• Press any button/key to join as a player\n\n" +
            "The current scene will be saved first.",
            "Create Scene",
            "Cancel"))
        {
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        DoSetup();

        EditorUtility.DisplayDialog("Scene Created",
            "Movement test scene saved to:\nAssets/Scenes/CombatTestArena.unity\n\n" +
            "To test:\n" +
            "1. Open the scene\n" +
            "2. Enter Play mode\n" +
            "3. Press any button/key to join as a player",
            "OK");
    }

    /// <summary>
    /// Creates the test scene without UI dialogs. Safe for batch mode.
    /// Returns false if prerequisites are missing.
    /// </summary>
    public static bool DoSetup()
    {
        var sharedMovement = AssetDatabase.LoadAssetAtPath<MovementData>($"{DataRoot}/Movement/SharedMovement.asset");
        var playerPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/PlayerCharacter.prefab");

        if (sharedMovement == null)
        {
            Debug.LogError("[Spells] SharedMovement asset not found. Run SetupMVPAssets.DoSetup() first.");
            return false;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[Spells] PlayerCharacter prefab not found.");
            return false;
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CombatTestArena";

        // ── Camera ──
        var cameraGo = new GameObject("Main Camera");
        var camera   = cameraGo.AddComponent<Camera>();
        cameraGo.tag = "MainCamera";
        camera.orthographic     = true;
        camera.orthographicSize = 15f;
        camera.transform.position = new Vector3(0, 4, -10);
        camera.backgroundColor = new Color(0.08f, 0.06f, 0.12f);
        camera.clearFlags      = CameraClearFlags.SolidColor;
        var multiCam = cameraGo.AddComponent<MultiTargetCamera>();
        cameraGo.AddComponent<AudioListener>();

        // ── SpellEffectRegistry (singleton) ──
        new GameObject("SpellEffectRegistry").AddComponent<SpellEffectRegistry>();

        // ── Arena Builder ──
        var arenaGo      = new GameObject("ArenaSetup");
        var arenaBuilder = arenaGo.AddComponent<TestArenaBuilder>();

        var arenaSerObj = new SerializedObject(arenaBuilder);
        SetField(arenaSerObj, "movementData", sharedMovement);
        SetField(arenaSerObj, "playerPrefab", playerPrefab);
        arenaSerObj.ApplyModifiedProperties();

        // ── Procedural Level Generation (optional) ──
        var modularBuilder   = arenaGo.AddComponent<ModularArenaBuilder>();
        var modBuilderSerObj = new SerializedObject(modularBuilder);
        SetField(modBuilderSerObj, "multiCamera", multiCam);
        modBuilderSerObj.ApplyModifiedProperties();

        var levelGenerator  = arenaGo.AddComponent<ProceduralLevelGenerator>();
        var levelGenSerObj  = new SerializedObject(levelGenerator);
        SetField(levelGenSerObj, "movementData", sharedMovement);

        var biomeDir   = "Assets/_Project/Data/Biomes";
        string[] biomeNames = { "ForestTemple", "DesertRuins", "VolcanicCaldera", "CrystalCavern", "StormCitadel" };
        var biomesProp = levelGenSerObj.FindProperty("biomePool");
        if (biomesProp != null)
        {
            biomesProp.arraySize = biomeNames.Length;
            for (int i = 0; i < biomeNames.Length; i++)
            {
                var biome = AssetDatabase.LoadAssetAtPath<BiomeData>($"{biomeDir}/{biomeNames[i]}.asset");
                if (biome != null)
                    biomesProp.GetArrayElementAtIndex(i).objectReferenceValue = biome;
                else
                    Debug.LogWarning($"[Spells] Biome asset not found: {biomeNames[i]}.asset — run Spells → Setup Biomes first.");
            }
        }
        levelGenSerObj.ApplyModifiedProperties();

        // ── Lighting ──
        var lightGo = new GameObject("Directional Light");
        var light   = lightGo.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(0.9f, 0.85f, 0.75f);
        light.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Save scene ──
        string scenePath = "Assets/Scenes/CombatTestArena.unity";
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log($"[Spells] ✓ Test scene created at: {scenePath}");
        Debug.Log("[Spells] Enter Play mode and press any button/key to join as a player.");
        return true;
    }

    private static void SetField(SerializedObject serObj, string fieldName, Object value)
    {
        var prop = serObj.FindProperty(fieldName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[Spells] Field '{fieldName}' not found on {serObj.targetObject.GetType().Name}");
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
