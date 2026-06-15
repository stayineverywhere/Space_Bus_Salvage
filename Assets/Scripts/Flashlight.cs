using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public Light flashlightLight;
    public bool isOn = true;
    public float batteryLife = 100f;
    public float drainRate = 0.1f;

    void Update()
    {
        if (PlayerInputController.Instance == null) return;

        if (PlayerInputController.Instance.FlashlightPressed)
        {
            isOn = !isOn;
            if (flashlightLight != null) flashlightLight.enabled = isOn;
        }

        if (isOn && flashlightLight != null)
        {
            batteryLife -= drainRate * Time.deltaTime;
            if (batteryLife <= 0)
            {
                batteryLife = 0;
                flashlightLight.enabled = false;
                isOn = false;
            }
        }
    }
}
