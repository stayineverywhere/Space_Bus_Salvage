using UnityEngine;
using TMPro;

public class DiegeticDashboard : MonoBehaviour
{
    public TextMeshPro fuelText;
    public TextMeshPro powerText;
    public TextMeshPro hullText;

    private BusController bus;

    void Start()
    {
        bus = GetComponentInParent<BusController>();
    }

    void Update()
    {
        if (bus != null)
        {
            if (hullText != null) hullText.text = $"HULL: {Mathf.RoundToInt((bus.currentHealth / bus.maxHealth) * 100)}%";
            if (bus.powerSystem != null && powerText != null) powerText.text = $"PWR: {Mathf.RoundToInt(bus.powerSystem.currentPower)}%";
            if (fuelText != null) fuelText.text = "FUEL: OPTIMAL"; // Placeholder
        }
    }
}
