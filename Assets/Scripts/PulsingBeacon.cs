using UnityEngine;

public class PulsingBeacon : MonoBehaviour
{
    private Light beaconLight;
    private float baseIntensity;
    public float pulseSpeed = 4f;

    void Start()
    {
        beaconLight = GetComponent<Light>();
        if (beaconLight != null) baseIntensity = beaconLight.intensity;
    }

    void Update()
    {
        if (beaconLight != null)
        {
            // Pulse intensity smoothly over time
            float wave = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            beaconLight.intensity = Mathf.Lerp(baseIntensity * 0.4f, baseIntensity * 1.6f, wave);
        }
    }
}