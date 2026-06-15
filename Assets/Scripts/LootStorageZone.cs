using UnityEngine;

public class LootStorageZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null && pm.carriedLoot != null)
            {
                HUDManager.Instance?.SetInteractionPrompt(true);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                // Just show prompt — actual deposit is handled in PlayerMovement.Update()
                if (pm.carriedLoot != null)
                    HUDManager.Instance?.SetInteractionPrompt(true, "[E] DEPOSIT LOOT");
                else
                    HUDManager.Instance?.SetInteractionPrompt(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HUDManager.Instance?.SetInteractionPrompt(false);
        }
    }

    public void CompleteDeposit(PlayerMovement pm)
    {
        if (pm == null || pm.carriedLoot == null) return;

        BusController bus = GetComponentInParent<BusController>();
        if (bus != null)
        {
            // Ensure storage component exists even if not wired in Inspector
            if (bus.storage == null)
                bus.storage = bus.GetComponent<BusStorage>() ?? bus.gameObject.AddComponent<BusStorage>();

            LootItem loot = pm.carriedLoot;

            // Attempt to store in bus storage
            if (bus.storage.StoreItem(loot))
            {
                // Loot parented to bus and deactivated
                loot.transform.SetParent(bus.transform);
                loot.transform.localPosition = Vector3.zero;
                
                var mr = loot.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                var col = loot.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                // Add to contract progress
                ContractManager.Instance?.AddCredits(loot.value);

                // Apply curse
                loot.ApplyCurse();
                CurseManager.Instance?.AddCurse(loot.curseValue);

                // Play feedback sound and shake
                ScreenShakeManager.Instance?.Shake(0.15f, 0.1f);
                Debug.Log($"[Storage] Stored {loot.itemName} successfully.");

                // Clear carried loot
                pm.carriedLoot = null;
            }
        }
    }
}