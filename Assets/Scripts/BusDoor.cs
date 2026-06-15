using UnityEngine;

public class BusDoor : MonoBehaviour
{
    public bool isOpen = false;
    public float openAngle = 90f;
    public float speed = 3f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private Material doorMat;
    private Light doorLight;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(0, openAngle, 0) * closedRotation;

        // Auto-configure door visuals (emissive materials + indicator lights)
        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null) r = GetComponent<Renderer>();
        if (r != null)
        {
            // Instance the material so we don't leak shared asset changes
            doorMat = r.material;
            doorMat.EnableKeyword("_EMISSION");
            UpdateDoorVisuals();
        }

        // Add a small localized spotlight above the door frame
        GameObject lightGO = new GameObject("DoorLightSource");
        lightGO.transform.SetParent(this.transform, false);
        lightGO.transform.localPosition = new Vector3(0.7f, 1.2f, 0.2f); // Above the door

        doorLight = lightGO.AddComponent<Light>();
        doorLight.type = LightType.Spot;
        doorLight.spotAngle = 60f;
        doorLight.range = 3.5f;
        UpdateLightVisuals();
    }

    void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * speed);
        UpdateInteractableColliders();
    }

    private void UpdateInteractableColliders()
    {
        Transform rootBus = transform;
        while (rootBus.parent != null && (rootBus.parent.GetComponent<BusController>() != null || rootBus.parent.name.Contains("Bus") || rootBus.parent.name.Contains("Cabin")))
        {
            rootBus = rootBus.parent;
        }

        var interactables = rootBus.GetComponentsInChildren<BusDoorInteractable>(true);
        foreach (var interactable in interactables)
        {
            if (Vector3.Distance(transform.position, interactable.transform.position) < 4.0f)
            {
                var col = interactable.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.enabled = isOpen;
                    col.isTrigger = isOpen;
                }
            }
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        Debug.Log("Bus Door: " + (isOpen ? "Opening" : "Closing"));
        UpdateDoorVisuals();
        UpdateLightVisuals();
    }

    private void UpdateDoorVisuals()
    {
        if (doorMat == null) return;

        // Glow orange! (Bright warning orange when open to welcome the player, dim warning orange when closed)
        Color orangeGlow = new Color(1.0f, 0.35f, 0.0f); // Pure neon orange
        float multiplier = isOpen ? 3.5f : 1.2f;
        doorMat.SetColor("_EmissionColor", orangeGlow * multiplier);
        doorMat.color = new Color(0.18f, 0.12f, 0.08f); // Rusty/industrial dark orange body
    }

    private void UpdateLightVisuals()
    {
        if (doorLight == null) return;

        // Spotlight shines bright orange when open, and dim warning orange when closed
        doorLight.color = new Color(1.0f, 0.35f, 0.0f);
        doorLight.intensity = isOpen ? 4f : 1f;
    }
}
