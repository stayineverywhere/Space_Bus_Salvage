using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    public Light targetLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    void Start()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
    }

    void Update()
    {
        if (targetLight != null)
        {
            targetLight.intensity = Mathf.Lerp(targetLight.intensity, Random.Range(minIntensity, maxIntensity), flickerSpeed);
        }
    }
}
