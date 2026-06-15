using UnityEngine;
using System.Collections.Generic;

public class AIDroneCompanion : MonoBehaviour
{
    public Transform followTarget;
    public float followDistance = 3f;
    public float followSpeed = 5f;

    [Header("Scanning")]
    public float scanRange = 20f;
    public float scanInterval = 1f;

    public List<LootItem> carriedLoot = new List<LootItem>();

    private float scanTimer;

    private void Start()
    {
        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) followTarget = player.transform;
        }
    }

    private void Update()
    {
        if (followTarget != null)
        {
            float speed = followSpeed;
            if (PlayerInputController.Instance != null && PlayerInputController.Instance.IsSprinting)
                speed *= 2f;

            Vector3 targetPos = followTarget.position + Vector3.up * 1.5f - followTarget.forward * followDistance;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed);
            transform.LookAt(followTarget.position);
        }

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanForDanger();
        }
    }

    private void ScanForDanger()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Monster"))
                Debug.Log($"[Drone] Danger: {hit.transform.name}");
        }
    }

    public void PickUpLoot(LootItem item)
    {
        carriedLoot.Add(item);
        item.gameObject.SetActive(false);
        Debug.Log($"[Drone] Carrying {item.itemName}.");
    }
}
