using UnityEngine;
using UnityEditor;

/// <summary>
/// Single entry point to run all setup steps in sequence.
/// Safe for batch mode: no UI dialogs, logs all progress.
///
/// Menu: Spells → Batch Setup All (CLI Safe)
///
/// CLI usage:
///   Unity -batchmode -nographics -quit \
///     -projectPath /path/to/Spells \
///     -executeMethod BatchSetup.RunAll -logFile -
/// </summary>
public static class BatchSetup
{
    [MenuItem("Spells/Batch Setup All (CLI Safe)", false, 99)]
    public static void RunAll()
    {
        Debug.Log("[Spells] ═══════════════════════════════════════════");
        Debug.Log("[Spells] Starting batch setup...");
        Debug.Log("[Spells] ═══════════════════════════════════════════");

        // Step 1: Create all ScriptableObject assets and prefabs
        Debug.Log("[Spells] Step 1/3: Creating MVP assets...");
        SetupMVPAssets.DoSetup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 2: Add combat components to player prefab
        Debug.Log("[Spells] Step 2/3: Setting up player prefab...");
        bool prefabOk = SetupPlayerPrefab.DoSetup();
        if (!prefabOk)
        {
            Debug.LogError("[Spells] ✗ Player prefab setup failed. Aborting.");
            return;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 3: Create the combat test scene
        Debug.Log("[Spells] Step 3/3: Creating test scene...");
        bool sceneOk = SetupTestScene.DoSetup();
        if (!sceneOk)
        {
            Debug.LogError("[Spells] ✗ Test scene setup failed.");
            return;
        }

        Debug.Log("[Spells] ═══════════════════════════════════════════");
        Debug.Log("[Spells] ✓ Batch setup complete!");
        Debug.Log("[Spells]   • Data assets created in Assets/_Project/Data/");
        Debug.Log("[Spells]   • Projectile prefabs in Assets/_Project/Prefabs/Projectiles/");
        Debug.Log("[Spells]   • Player prefab updated with combat components");
        Debug.Log("[Spells]   • Test scene at Assets/Scenes/CombatTestArena.unity");
        Debug.Log("[Spells] ═══════════════════════════════════════════");
    }
}
