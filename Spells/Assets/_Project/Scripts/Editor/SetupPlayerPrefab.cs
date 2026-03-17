using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds all combat and game system components to the PlayerCharacter prefab.
/// The original prefab has movement components; this adds everything needed
/// for the full combat system.
///
/// Menu: Spells → Setup Player Prefab
///
/// Safe to run multiple times — skips components that already exist.
/// </summary>
public class SetupPlayerPrefab : Editor
{
    [MenuItem("Spells/Setup Player Prefab", false, 101)]
    public static void Setup()
    {
        if (!DoSetup())
        {
            EditorUtility.DisplayDialog("Error",
                "Player prefab not found at:\nAssets/_Project/Prefabs/Player/PlayerCharacter.prefab\n\nCreate it first or update the path.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Player Prefab Setup",
            "Player prefab setup complete. Check console for details.", "OK");
    }

    /// <summary>
    /// Adds all combat/game components to the player prefab. No UI dialogs.
    /// Returns false if the prefab is missing.
    /// </summary>
    public static bool DoSetup()
    {
        string prefabPath = "Assets/_Project/Prefabs/Player/PlayerCharacter.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[Spells] Player prefab not found at: {prefabPath}");
            return false;
        }

        // Open prefab for editing
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        int added = 0;

        // ── Core identity ──
        added += EnsureComponent<PlayerIdentity>(prefabRoot);

        // ── Combat systems ──
        added += EnsureComponent<ClassManager>(prefabRoot);
        added += EnsureComponent<HealthSystem>(prefabRoot);
        added += EnsureComponent<ProjectileSpawner>(prefabRoot);
        added += EnsureComponent<ParrySystem>(prefabRoot);
        added += EnsureComponent<CombatEventRouter>(prefabRoot);
        added += EnsureComponent<SpawnProtection>(prefabRoot);

        // ── Card system ──
        added += EnsureComponent<CardInventory>(prefabRoot);
        added += EnsureComponent<ProjectileModifierSystem>(prefabRoot);

        // ── Player lifecycle ──
        added += EnsureComponent<PlayerDeathHandler>(prefabRoot);
        added += EnsureComponent<PlayerVisualFeedback>(prefabRoot);

        // Save changes
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();

        string message = added > 0
            ? $"Added {added} components to PlayerCharacter prefab."
            : "All components already present. No changes needed.";

        Debug.Log($"[Spells] ✓ Player prefab setup: {message}");
        return true;
    }

    /// <summary>
    /// Adds a component if it doesn't already exist. Returns 1 if added, 0 if skipped.
    /// </summary>
    private static int EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() != null)
        {
            Debug.Log($"[Spells] Skipped (exists): {typeof(T).Name}");
            return 0;
        }

        go.AddComponent<T>();
        Debug.Log($"[Spells] Added: {typeof(T).Name}");
        return 1;
    }
}
