using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SafeSpawnManager : MonoBehaviour
{
    private static SafeSpawnManager _instance;
    public static SafeSpawnManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<SafeSpawnManager>();
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

    public GameObject SpawnSafe(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Vector3 safePos = GetGroundPosition(position);
        GameObject spawned = Instantiate(prefab, safePos, rotation);
        
        // Reset Physics
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Check for children rigidbodies
        Rigidbody[] childRbs = spawned.GetComponentsInChildren<Rigidbody>();
        foreach (var childRb in childRbs)
        {
            childRb.linearVelocity = Vector3.zero;
            childRb.angularVelocity = Vector3.zero;
        }

        return spawned;
    }

    private Vector3 GetGroundPosition(Vector3 originalPos)
    {
        RaycastHit hit;
        // Raycast from high above the original position down to the ground
        if (Physics.Raycast(originalPos + Vector3.up * 50f, Vector3.down, out hit, 100f))
        {
            // Position slightly above ground to prevent clipping
            return hit.point + Vector3.up * 0.1f;
        }
        
        // Fallback: if no ground detected, ensure it's at least above 0
        return new Vector3(originalPos.x, Mathf.Max(originalPos.y, 0.1f), originalPos.z);
    }

    public void ResetAllPhysics()
    {
        Rigidbody[] allRbs = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude);
        foreach (var rb in allRbs)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
