using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;
    public static SceneLoader Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<SceneLoader>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (IsSceneInBuildSettings(sceneName))
        {
            StartCoroutine(LoadAsync(sceneName));
        }
        else
        {
            // Scene not in Build Settings — handle in-place (UI panels are already overlaid)
            Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' not found in Build Settings. " +
                             "Add it via File → Build Settings in Unity Editor. " +
                             "Running in single-scene fallback mode.");
            HandleFallback(sceneName);
        }
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            // Path format: "Assets/Scenes/SceneName.unity"
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    private void HandleFallback(string sceneName)
    {
        // For GarageHub: just ensure garage state is active; UI panels handle the rest
        if (sceneName == "GarageHub")
        {
            // Deactivate exploration hazards in current scene (same as CleanGarageHubScene)
            foreach (var m in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Include))
                m.gameObject.SetActive(false);

            foreach (var p in Object.FindObjectsByType<PlanetZoneTrigger>(FindObjectsInactive.Include))
                p.gameObject.SetActive(false);

            // Move player back near bus origin
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = new Vector3(0f, 1f, 0f);

            Debug.Log("[SceneLoader] Fallback: cleared exploration objects, repositioned player.");
        }
        else if (sceneName == "PlanetExploration")
        {
            // Re-activate exploration objects when deploying
            foreach (var m in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Include))
                m.gameObject.SetActive(true);

            Debug.Log("[SceneLoader] Fallback: re-activated exploration objects.");
        }
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        Debug.Log($"[SceneLoader] Loading scene: {sceneName}");
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for '{sceneName}'.");
            yield break;
        }
        while (!operation.isDone)
        {
            yield return null;
        }
        Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded.");
    }
}
