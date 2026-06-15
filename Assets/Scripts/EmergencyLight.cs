using UnityEngine;

public class EmergencyLight : MonoBehaviour
{
    public Light redLight;
    public float pulseSpeed = 2f;
    public float maxIntensity = 3f;

    void Update()
    {
        if (redLight != null)
        {
            redLight.intensity = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f * maxIntensity;
        }
    }
}
