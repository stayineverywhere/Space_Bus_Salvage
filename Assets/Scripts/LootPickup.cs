using UnityEngine;
using TMPro;

[RequireComponent(typeof(LootItem))]
public class LootPickup : MonoBehaviour
{
    private LootItem lootItem;
    private bool pickedUp;
    private GameObject worldCanvasGO;

    void Start()
    {
        lootItem = GetComponent<LootItem>();

        // Primary solid collider so raycasts from PlayerMovement can hit this object
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.size = new Vector3(0.5f, 0.5f, 0.5f);
        }
        else
        {
            col.isTrigger = false;
        }

        // Separate sphere trigger for proximity world-UI popup
        SphereCollider proximity = gameObject.AddComponent<SphereCollider>();
        proximity.isTrigger = true;
        proximity.radius = 2.0f;

        // Build procedural models replacing plain cubes/cylinders
        BuildProceduralAsset();

        // Apply visual enhancements (glowing emission + procedural animation)
        ApplyLootVisualEffects();

        // Create world-space label and pickup prompt
        CreateWorldSpaceUI();
    }

    private void ApplyLootVisualEffects()
    {
        // Random scaling + rotation for natural, organic scatter
        transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
        transform.localScale *= Random.Range(0.9f, 1.25f);

        // Make existing renderers glow beautifully
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend != null)
            {
                Material mat = rend.material;
                if (mat != null)
                {
                    mat.EnableKeyword("_EMISSION");
                    Color glowColor = Color.cyan * 1.5f;
                    if (lootItem != null)
                    {
                        if (lootItem.curseValue > 25f)
                            glowColor = Color.red * 2.0f;
                        else if (lootItem.curseValue > 15f)
                            glowColor = Color.magenta * 1.8f;
                    }
                    mat.SetColor("_EmissionColor", glowColor);
                }
            }
        }

        // Procedural addition: If a primitive cube/sphere, enrich it with a floating rotating component
        if (transform.childCount <= 1)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Artifact_Shard";
            shard.transform.SetParent(this.transform, false);
            shard.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            shard.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Destroy(shard.GetComponent<BoxCollider>());
            shard.AddComponent<RotateObject>();

            Renderer sRend = shard.GetComponent<Renderer>();
            if (sRend != null)
            {
                Material sMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                sMat.EnableKeyword("_EMISSION");
                Color glowColor = (lootItem != null && lootItem.curseValue > 20f) ? Color.red * 2.5f : Color.cyan * 2.5f;
                sMat.SetColor("_EmissionColor", glowColor);
                sRend.material = sMat;
            }
        }
    }

    private void CreateWorldSpaceUI()
    {
        if (worldCanvasGO != null) return;

        worldCanvasGO = new GameObject("LootWorldCanvas");
        worldCanvasGO.transform.SetParent(this.transform, false);
        worldCanvasGO.transform.localPosition = Vector3.up * 0.85f;

        Canvas canvas = worldCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRT = worldCanvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(2f, 1f);
        canvasRT.localScale = new Vector3(1f, 1f, 1f);

        worldCanvasGO.AddComponent<BillboardUI>();

        // Text: Loot Name
        GameObject nameGO = new GameObject("LootNameText");
        nameGO.transform.SetParent(worldCanvasGO.transform, false);
        TextMeshPro tmpName = nameGO.AddComponent<TextMeshPro>();
        tmpName.text = lootItem != null ? lootItem.itemName : "Loot Artifact";
        tmpName.fontSize = 2.5f;
        tmpName.alignment = TextAlignmentOptions.Center;
        tmpName.color = Color.white;

        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.sizeDelta = new Vector2(4f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, 0.15f);

        // Text: Action Prompt
        GameObject promptGO = new GameObject("PickupPromptText");
        promptGO.transform.SetParent(worldCanvasGO.transform, false);
        TextMeshPro tmpPrompt = promptGO.AddComponent<TextMeshPro>();
        tmpPrompt.text = "[E] Pick Up";
        tmpPrompt.fontSize = 1.8f;
        tmpPrompt.alignment = TextAlignmentOptions.Center;
        tmpPrompt.color = new Color(0.7f, 1f, 0.7f, 0.95f);

        RectTransform promptRT = promptGO.GetComponent<RectTransform>();
        promptRT.sizeDelta = new Vector2(4f, 1f);
        promptRT.anchoredPosition = new Vector2(0f, -0.15f);
    }

    // Proximity trigger is used ONLY to show the world-space pickup prompt.
    // Actual pickup is handled by PlayerMovement's raycast E-key carry system.
    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (other.CompareTag("Player") && worldCanvasGO != null)
            worldCanvasGO.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && worldCanvasGO != null)
            worldCanvasGO.SetActive(false);
    }

    private void BuildProceduralAsset()
    {
        // 1. Hide or destroy the original placeholder primitives
        foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.gameObject != this.gameObject)
            {
                Destroy(filter.gameObject);
            }
            else
            {
                var r = filter.GetComponent<Renderer>();
                if (r != null) r.enabled = false; // Hide root primitive
            }
        }

        // 2. Create the container
        GameObject container = new GameObject("ProceduralVisuals");
        container.transform.SetParent(this.transform, false);

        // 3. Setup materials
        Material metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        metalMat.color = new Color(0.18f, 0.18f, 0.2f);
        if (metalMat.HasProperty("_Metallic")) metalMat.SetFloat("_Metallic", 0.75f);
        if (metalMat.HasProperty("_Glossiness")) metalMat.SetFloat("_Glossiness", 0.6f);

        Material copperMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        copperMat.color = new Color(0.42f, 0.24f, 0.15f); // coppery bronze
        if (copperMat.HasProperty("_Metallic")) copperMat.SetFloat("_Metallic", 0.85f);
        if (copperMat.HasProperty("_Glossiness")) copperMat.SetFloat("_Glossiness", 0.7f);

        Material whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        whiteMat.color = new Color(0.85f, 0.85f, 0.82f); // porcelain white
        if (whiteMat.HasProperty("_Glossiness")) whiteMat.SetFloat("_Glossiness", 0.85f);

        Material glassMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        glassMat.color = new Color(0.8f, 0.95f, 1.0f, 0.35f); // clear glass
        if (glassMat.HasProperty("_Glossiness")) glassMat.SetFloat("_Glossiness", 1.0f);

        Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        glowMat.EnableKeyword("_EMISSION");
        Color glowColor = Color.cyan * 2f;
        if (lootItem != null)
        {
            if (lootItem is CursedDoll) glowColor = Color.red * 2.8f;
            else if (lootItem is BrokenClock) glowColor = new Color(1f, 0.5f, 0f) * 2.2f;
            else if (lootItem is RedFrame) glowColor = Color.magenta * 2.5f;
        }
        glowMat.SetColor("_EmissionColor", glowColor);

        // 4. Build distinct realistic shapes based on type
        if (lootItem is CursedDoll)
        {
            // Humanoid Porcelain Doll body
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torso.transform.SetParent(container.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            torso.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
            Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().material = whiteMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(container.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material = whiteMat;

            // Cracked details
            GameObject crack1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack1.transform.SetParent(head.transform, false);
            crack1.transform.localPosition = new Vector3(0.12f, 0.05f, 0.12f);
            crack1.transform.localScale = new Vector3(0.1f, 0.6f, 0.02f);
            crack1.transform.localRotation = Quaternion.Euler(0f, 45f, 35f);
            Destroy(crack1.GetComponent<Collider>());
            Material darkCrack = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            darkCrack.color = Color.black;
            crack1.GetComponent<Renderer>().material = darkCrack;

            // Crimson glowing eyes
            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.transform.SetParent(head.transform, false);
            eyeL.transform.localPosition = new Vector3(-0.16f, 0.15f, 0.35f);
            eyeL.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Destroy(eyeL.GetComponent<Collider>());
            eyeL.GetComponent<Renderer>().material = glowMat;

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.transform.SetParent(head.transform, false);
            eyeR.transform.localPosition = new Vector3(0.16f, 0.15f, 0.35f);
            eyeR.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Destroy(eyeR.GetComponent<Collider>());
            eyeR.GetComponent<Renderer>().material = glowMat;
        }
        else if (lootItem is BrokenClock)
        {
            // Rusty round frame
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.transform.SetParent(container.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            rim.transform.localScale = new Vector3(0.55f, 0.04f, 0.55f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(rim.GetComponent<Collider>());
            rim.GetComponent<Renderer>().material = copperMat;

            // Inside dial face
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.transform.SetParent(rim.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            face.transform.localScale = new Vector3(0.85f, 0.1f, 0.85f);
            Destroy(face.GetComponent<Collider>());
            Material dialMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            dialMat.color = new Color(0.72f, 0.68f, 0.6f); // old yellowed paper
            face.GetComponent<Renderer>().material = dialMat;

            // Broken dial hands
            GameObject handH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handH.transform.SetParent(face.transform, false);
            handH.transform.localPosition = new Vector3(0f, 0.51f, 0.18f);
            handH.transform.localScale = new Vector3(0.06f, 0.02f, 0.45f);
            handH.transform.localRotation = Quaternion.Euler(0f, 55f, 0f);
            Destroy(handH.GetComponent<Collider>());
            handH.GetComponent<Renderer>().material = metalMat;

            // Cracked glass cover
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.transform.SetParent(rim.transform, false);
            glass.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            glass.transform.localScale = new Vector3(0.82f, 0.02f, 0.82f);
            Destroy(glass.GetComponent<Collider>());
            glass.GetComponent<Renderer>().material = glassMat;
        }
        else if (lootItem is RedFrame)
        {
            // Rectangular industrial frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(container.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            frame.transform.localScale = new Vector3(0.48f, 0.64f, 0.08f);
            Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().material = metalMat;

            // Pulsing Holographic screen inside
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.transform.SetParent(frame.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.51f);
            screen.transform.localScale = new Vector3(0.86f, 0.86f, 0.1f);
            Destroy(screen.GetComponent<Collider>());
            screen.GetComponent<Renderer>().material = glowMat;

            // A small core rotating artifact shard inside
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.transform.SetParent(container.transform, false);
            shard.transform.localPosition = new Vector3(0f, 0.25f, 0.1f);
            shard.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Destroy(shard.GetComponent<Collider>());
            shard.AddComponent<RotateObject>();
            shard.GetComponent<Renderer>().material = glowMat;
        }
        else
        {
            // Generic industrial cargo/scrap parts
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(container.transform, false);
            cylinder.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            cylinder.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            Destroy(cylinder.GetComponent<Collider>());
            cylinder.GetComponent<Renderer>().material = metalMat;

            GameObject gear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gear.transform.SetParent(cylinder.transform, false);
            gear.transform.localPosition = new Vector3(0f, 0f, 0f);
            gear.transform.localScale = new Vector3(1.4f, 0.2f, 1.4f);
            gear.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Destroy(gear.GetComponent<Collider>());
            gear.GetComponent<Renderer>().material = copperMat;
        }

        // Add slow breathing procedural animation
        container.AddComponent<LootVisualBreathing>();
    }
}

// ── Floating Shard Spin Helper ───────────────────────────────────────────────
public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(20f, 50f, 25f);

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            0.2f + Mathf.Sin(Time.time * 2.5f) * 0.05f,
            transform.localPosition.z
        );
    }
}

// ── World Canvas Camera facing Billboard helper ──────────────────────────────
public class BillboardUI : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.forward);
        }
    }
}

// ── Loot Breathing/Spin animation ───────────────────────────────────────────
public class LootVisualBreathing : MonoBehaviour
{
    private float randOffset;
    void Start()
    {
        randOffset = Random.Range(0f, 500f);
    }
    void Update()
    {
        transform.Rotate(Vector3.up * 18f * Time.deltaTime, Space.World);
        float verticalOffset = Mathf.Sin((Time.time + randOffset) * 2.2f) * 0.04f;
        transform.localPosition = new Vector3(transform.localPosition.x, verticalOffset, transform.localPosition.z);
    }
}
