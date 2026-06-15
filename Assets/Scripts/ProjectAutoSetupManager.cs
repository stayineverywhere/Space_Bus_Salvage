using UnityEngine;

public class ProjectAutoSetupManager : MonoBehaviour
{
    public static ProjectAutoSetupManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AssignTags();
        EnsureEventSystem();
        EnsurePlanetManager();
    }

    private void AssignTags()
    {
        foreach (var obj in FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude))
            if (obj.CompareTag("Untagged")) TrySetTag(obj.gameObject, "Player");

        foreach (var obj in FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude))
            if (obj.CompareTag("Untagged")) TrySetTag(obj.gameObject, "Monster");

        foreach (var obj in FindObjectsByType<LootItem>(FindObjectsInactive.Exclude))
            if (obj.CompareTag("Untagged")) TrySetTag(obj.gameObject, "Loot");

        foreach (var obj in FindObjectsByType<BusController>(FindObjectsInactive.Exclude))
            if (obj.CompareTag("Untagged")) TrySetTag(obj.gameObject, "Bus");

        foreach (var obj in FindObjectsByType<AIDroneCompanion>(FindObjectsInactive.Exclude))
            if (obj.CompareTag("Untagged")) TrySetTag(obj.gameObject, "Drone");

        Debug.Log("[AutoSetup] Tags assigned.");
    }

    private void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; }
        catch { Debug.LogWarning($"[AutoSetup] Tag '{tag}' not defined in project. Add it in Tags & Layers."); }
    }

    private void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var go = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem));
            var uiModule = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
            Debug.Log("[AutoSetup] Created EventSystem with InputSystemUIInputModule and assigned default actions.");
        }
    }

    private void EnsurePlanetManager()
    {
        if (PlanetManager.Instance == null)
        {
            GameObject go = new GameObject("PlanetManager");
            go.AddComponent<PlanetManager>();
            DontDestroyOnLoad(go);
        }
    }
}
