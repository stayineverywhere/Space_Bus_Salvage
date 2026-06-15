using UnityEngine;
using TMPro;

public class BusDoorInteractable : MonoBehaviour
{
    public bool isEntrance = true; // True for entering bus, false for exiting bus
    public Vector3 spawnOffset = new Vector3(0f, -0.6f, -1.8f); // Offset for teleportation target

    [Header("Orange Glow Visuals")]
    private Material glowMat;
    private Light orangeLight;

    [Header("UI Config")]
    private GameObject worldCanvasGO;

    private void Start()
    {
        // 1. Setup BoxCollider so players can raycast hit it
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = false; // Rigid collider for raycasting
        col.size = new Vector3(1.2f, 1.8f, 0.2f);

        // 2. Setup glowing orange visual model
        GameObject visualMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualMesh.name = "DoorVisualMesh";
        visualMesh.transform.SetParent(this.transform, false);
        visualMesh.transform.localPosition = Vector3.zero;
        visualMesh.transform.localScale = new Vector3(1.2f, 1.8f, 0.1f);
        DestroyImmediate(visualMesh.GetComponent<BoxCollider>()); // No duplicate collision

        glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        glowMat.color = new Color(0.18f, 0.12f, 0.08f); // Rusty industrial background
        glowMat.EnableKeyword("_EMISSION");
        glowMat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.0f) * 1.5f); // Emissive orange glow
        visualMesh.GetComponent<Renderer>().material = glowMat;

        // 3. Add pulsing orange indicator light
        GameObject lightGO = new GameObject("OrangeIndicatorLight");
        lightGO.transform.SetParent(this.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 1f, 0.15f);
        orangeLight = lightGO.AddComponent<Light>();
        orangeLight.type = LightType.Point;
        orangeLight.color = new Color(1.0f, 0.35f, 0.0f); // Bright neon orange
        orangeLight.intensity = 2f;
        orangeLight.range = 5f;

        // 4. Create floating UI prompt above the door
        CreateWorldSpaceUI();
    }

    private void CreateWorldSpaceUI()
    {
        worldCanvasGO = new GameObject("DoorWorldCanvas");
        worldCanvasGO.transform.SetParent(this.transform, false);
        worldCanvasGO.transform.localPosition = Vector3.up * 1.2f;

        Canvas canvas = worldCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRT = worldCanvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(3f, 1f);
        worldCanvasGO.AddComponent<BillboardUI>();

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(worldCanvasGO.transform, false);
        TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
        tmp.text = isEntrance ? "ENTER BUS\n[Q]" : "LEAVE BUS\n[Q]";
        tmp.fontSize = 2.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1.0f, 0.5f, 0.0f); // High contrast orange
    }

    private void Update()
    {
        // Pulsing glow animation
        if (glowMat != null)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * 3.5f, 1f) * 1.5f;
            glowMat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.0f) * pulse);
            if (orangeLight != null) orangeLight.intensity = pulse * 1.5f;
        }
    }

    public void TriggerDoorTransition(PlayerMovement player)
    {
        if (BusTransitionController.Instance == null) return;

        if (isEntrance)
        {
            // Enter transition
            BusTransitionController.Instance.EnterBus(player, spawnOffset);
        }
        else
        {
            // Exit transition - teleport to external back door world position
            BusController bus = Object.FindAnyObjectByType<BusController>();
            if (bus != null)
            {
                // Teleport player 2m outside the back of the bus in world coordinates
                Vector3 exitWorldPos = bus.transform.TransformPoint(new Vector3(0f, 1.0f, -6.5f));
                BusTransitionController.Instance.ExitBus(player, exitWorldPos);
            }
        }
    }
}