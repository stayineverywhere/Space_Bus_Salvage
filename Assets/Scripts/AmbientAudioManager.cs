using UnityEngine;

public class AmbientAudioManager : MonoBehaviour
{
    public AudioSource mechanicalHum;
    public AudioSource windDistortion;
    public float dangerVolumeBoost = 0.5f;

    void Update()
    {
        if (CurseManager.Instance != null)
        {
            float ratio = CurseManager.Instance.globalCurseValue / CurseManager.Instance.maxCurseValue;
            if (windDistortion != null) windDistortion.volume = 0.2f + (ratio * dangerVolumeBoost);
        }
    }
}
