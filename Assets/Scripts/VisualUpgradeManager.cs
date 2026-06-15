using UnityEngine;
using System.Collections.Generic;

public class VisualUpgradeManager : MonoBehaviour
{
    public static VisualUpgradeManager Instance { get; private set; }

    [Header("Environment Props")]
    public List<GameObject> propPrefabs = new List<GameObject>();
    public Material sciFiMaterial;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void DecorateScene()
    {
        Debug.Log("[VisualUpgrade] Decorating scene with props...");
        
        // Find ground surfaces
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var rend in renderers)
        {
            if (rend.gameObject.name.ToLower().Contains("floor") || rend.gameObject.name.ToLower().Contains("ground"))
            {
                PopulateSurface(rend);
            }
        }
    }

    private void PopulateSurface(MeshRenderer surface)
    {
        Bounds bounds = surface.bounds;
        int propCount = Mathf.FloorToInt((bounds.size.x * bounds.size.z) / 10f); // 1 prop per 10 sqm
        
        for (int i = 0; i < propCount; i++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 pos = new Vector3(x, bounds.max.y + 5f, z);

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 10f))
            {
                if (hit.collider.gameObject == surface.gameObject)
                {
                    SpawnRandomProp(hit.point);
                }
            }
        }
    }

    private void SpawnRandomProp(Vector3 position)
    {
        if (propPrefabs.Count == 0)
        {
            // Create a procedural crate if no prefabs
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "SciFi_Crate";
            crate.transform.position = position + Vector3.up * 0.5f;
            crate.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            
            if (sciFiMaterial != null)
            {
                crate.GetComponent<MeshRenderer>().material = sciFiMaterial;
            }
        }
        else
        {
            GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];
            Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        }
    }
}
