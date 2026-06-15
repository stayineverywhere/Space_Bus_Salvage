using UnityEngine;

public class BusInteriorZone : MonoBehaviour
{
    public float healRate = 8f;
    public float oxygenRefillRate = 15f;
    public BusPowerSystem powerSystem;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool hasPower = powerSystem == null || powerSystem.isPowerOn;
        if (!hasPower) return;

        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        pm.Heal(healRate * Time.deltaTime);
        pm.RefillOxygen(oxygenRefillRate * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Bus] Player entered safe zone — healing active.");
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null) pm.isInsideBus = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Bus] Player left safe zone.");
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null) pm.isInsideBus = false;

            // Restore exterior world if player walked out physically (no Q/fade transition)
            if (SceneBootstrap.ExteriorWorldRoot != null && !SceneBootstrap.ExteriorWorldRoot.activeSelf)
            {
                BusTransitionController.Instance?.ForceOutsideState();
            }
        }
    }
}
