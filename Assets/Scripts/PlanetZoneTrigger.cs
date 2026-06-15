using UnityEngine;

public class PlanetZoneTrigger : MonoBehaviour
{
    public PlanetData planetData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject, false);
        }
    }

    private void ApplyEffect(GameObject player, bool entering)
    {
        if (planetData == null) return;
        switch (planetData.type)
        {
            case PlanetType.Ice:
                // TODO: Apply slow movement and fog
                Debug.Log(entering ? "Entering Ice Planet: Slowing down..." : "Exiting Ice Planet.");
                break;
            case PlanetType.Toxic:
                // TODO: Start/Stop Damage Over Time
                Debug.Log(entering ? "Entering Toxic Planet: Taking damage!" : "Exiting Toxic Planet.");
                break;
            case PlanetType.GravityAnomaly:
                // TODO: Alter gravity
                Debug.Log(entering ? "Entering Gravity Anomaly: Weightlessness..." : "Exiting Gravity Anomaly.");
                break;
            case PlanetType.Mining:
                Debug.Log(entering ? "Entering Mining Planet: High Danger/High Reward." : "Exiting Mining Planet.");
                break;
        }
    }
}
