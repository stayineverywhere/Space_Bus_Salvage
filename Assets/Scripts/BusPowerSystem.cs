using UnityEngine;

public class BusPowerSystem : MonoBehaviour
{
    public float maxPower = 100f;
    public float currentPower;
    public float consumptionRate = 0.5f;
    public bool isPowerOn = true;

    [Header("Affected Systems")]
    public GameObject interiorLights;

    void Start()
    {
        currentPower = maxPower;
    }

    void Update()
    {
        // Power drain removed — power stays at full
    }

    public void SetPower(bool state)
    {
        isPowerOn = state;
        if (interiorLights != null) interiorLights.SetActive(state);
        Debug.Log("Bus Power: " + (state ? "ON" : "OFF"));
    }

    public void Recharge(float amount)
    {
        currentPower = Mathf.Min(currentPower + amount, maxPower);
    }
}
