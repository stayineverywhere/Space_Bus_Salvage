using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

/// <summary>
/// Zero-config entry point. Runs automatically before any scene loads — no
/// GameObjects required in any scene. Bootstraps all singleton managers once.
/// </summary>
public static class GameBootstrapper
{
    private static bool _booted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (_booted) return;
        _booted = true;

        Debug.Log("[Bootstrapper] Booting Space Bus Salvage...");

#if UNITY_EDITOR
        EnsureScenesInBuildSettings();
#endif

        // Input — must exist before anything else
        EnsureSingleton<PlayerInputController>("PlayerInputController");

        // Persistent managers bundle
        GameObject managers = new GameObject("PersistentManagers");
        Object.DontDestroyOnLoad(managers);
        managers.AddComponent<GameLoopManager>();
        managers.AddComponent<GarageManager>();
        managers.AddComponent<ContractManager>();
        managers.AddComponent<CurseManager>();
        managers.AddComponent<SafeSpawnManager>();
        managers.AddComponent<ProjectAutoSetupManager>();
        managers.AddComponent<PlanetManager>();
        managers.AddComponent<ScreenShakeManager>();
        managers.AddComponent<SceneLoader>();

        // HUD
        EnsureSingleton<HUDManager>("HUDManager");

        // Game UI (creates all panel canvases)
        EnsureSingleton<GameUIManager>("GameUIManager");

        // Listen for scene loads to run per-scene bootstrap
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[Bootstrapper] All singletons ready.");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Bootstrapper] Scene loaded: {scene.name}");

        // Run per-scene bootstrap if no SceneBootstrap GO exists
        SceneBootstrap existing = Object.FindAnyObjectByType<SceneBootstrap>();
        if (existing == null)
        {
            GameObject bsGO = new GameObject("SceneBootstrap_Auto");
            bsGO.AddComponent<SceneBootstrap>();
        }
    }

    private static T EnsureSingleton<T>(string goName) where T : MonoBehaviour
    {
        T existing = Object.FindAnyObjectByType<T>();
        if (existing != null) return existing;
        GameObject go = new GameObject(goName);
        Object.DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }

#if UNITY_EDITOR
    static void EnsureScenesInBuildSettings()
    {
        var required = new[]
        {
            "Assets/Scenes/GarageHub.unity",
            "Assets/Scenes/PlanetExploration.unity",
            "Assets/Scenes/SampleScene.unity",
        };

        var current = EditorBuildSettings.scenes.ToList();
        bool changed = false;
        foreach (string path in required)
        {
            if (System.IO.File.Exists(path) && !current.Any(s => s.path == path))
            {
                current.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
                Debug.Log($"[Bootstrapper] Added to Build Settings: {path}");
            }
        }
        if (changed) EditorBuildSettings.scenes = current.ToArray();
    }
#endif
}
