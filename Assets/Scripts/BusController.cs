using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 1000f;
    public float currentHealth;
    public float moveSpeed = 10f;

    [Header("Sub-Systems")]
    public BusPowerSystem powerSystem;
    public BusStorage storage;
    public BusDoor[] doors;

    [Header("Damage Feedback")]
    public Light[] interiorLights;
    private float damageFlickerTimer;

    private bool isOccupied;
    private GameObject currentDriver;
    private PlayerMovement driverMovement;

    private AudioSource alarmSource;
    private AudioClip alarmClip;
    private float alarmTimer;

    private void Start()
    {
        Debug.Log($"[Diagnostic] BusController.Start() running on GameObject '{gameObject.name}' with InstanceID {gameObject.GetInstanceID()}");
        currentHealth = maxHealth;
        if (powerSystem == null) powerSystem = GetComponent<BusPowerSystem>();
        if (storage    == null) storage     = GetComponent<BusStorage>();
        if (storage    == null) storage     = gameObject.AddComponent<BusStorage>();
        
        // Reconstruct the physical bus interior to be hollow, walkable, and opaque
        ReconstructBusInterior();

        // Auto-assign doors and interior lights if not set
        if (doors == null || doors.Length == 0)
        {
            doors = GetComponentsInChildren<BusDoor>(true);
        }
        if (interiorLights == null || interiorLights.Length == 0)
        {
            interiorLights = GetComponentsInChildren<Light>(true);
        }

        SetupAlarm();
        SnapToGround();
        CreateLootStorageZone();
        CreateNavigationBeacon();
    }

    private void ReconstructBusInterior()
    {
        Debug.Log("[Bus] Reconstructing bus interior to be walkable and opaque from inside...");

        // 1. Resize root BoxCollider to cover only the wheels/floor, freeing head/body space so the player can enter
        BoxCollider rootCol = GetComponent<BoxCollider>();
        if (rootCol != null)
        {
            rootCol.center = new Vector3(0f, -1.25f, 0f);
            rootCol.size = new Vector3(4f, 0.1f, 10f);
            rootCol.isTrigger = false;
        }

        // 2. Hide or destroy overlapping primitive cubes in the middle of the bus
        Transform layout = transform.Find("Interior_Layout");
        if (layout != null)
        {
            System.Collections.Generic.List<GameObject> toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in layout)
            {
                if (child.name == "Cockpit" || child.name == "CargoRoom" || child.name == "Corridor1")
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }
        }

        // 3. Disable the root primitive mesh renderer if it exists as a fallback
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            var r = GetComponent<MeshRenderer>();
            if (r != null) r.enabled = false;
        }

        // 4. Create standard metallic materials for the interior
        Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        wallMat.color = new Color(0.12f, 0.13f, 0.16f); // dark sci-fi gray
        if (wallMat.HasProperty("_Metallic")) wallMat.SetFloat("_Metallic", 0.8f);
        if (wallMat.HasProperty("_Glossiness")) wallMat.SetFloat("_Glossiness", 0.5f);

        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        floorMat.color = new Color(0.18f, 0.18f, 0.2f); // industrial floor
        if (floorMat.HasProperty("_Metallic")) floorMat.SetFloat("_Metallic", 0.9f);
        if (floorMat.HasProperty("_Glossiness")) floorMat.SetFloat("_Glossiness", 0.4f);

        // 5. Build hollow walls, ceiling, and floor (opaque cubes pointing inwards)
        GameObject cabin = new GameObject("WalkableCabin");
        cabin.transform.SetParent(this.transform, false);
        cabin.transform.localPosition = Vector3.zero;

        // Floor
        CreateWall("Floor", cabin.transform, new Vector3(0f, -1.2f, 0f), new Vector3(3.8f, 0.1f, 9.6f), floorMat);
        // Ceiling
        CreateWall("Ceiling", cabin.transform, new Vector3(0f, 1.2f, 0f), new Vector3(3.8f, 0.1f, 9.6f), wallMat);
        // Left Wall
        CreateWall("LeftWall", cabin.transform, new Vector3(-1.9f, 0f, 0f), new Vector3(0.1f, 2.4f, 9.6f), wallMat);
        // Right Wall
        CreateWall("RightWall", cabin.transform, new Vector3(1.9f, 0f, 0f), new Vector3(0.1f, 2.4f, 9.6f), wallMat);
        // Front Wall (Cockpit)
        CreateWall("FrontWall", cabin.transform, new Vector3(0f, 0f, 4.8f), new Vector3(3.8f, 2.4f, 0.1f), wallMat);

        // Back Wall (Entrance area) has Left and Right columns with a door in the middle
        CreateWall("BackWall_Left", cabin.transform, new Vector3(-1.3f, 0f, -4.8f), new Vector3(1.2f, 2.4f, 0.1f), wallMat);
        CreateWall("BackWall_Right", cabin.transform, new Vector3(1.3f, 0f, -4.8f), new Vector3(1.2f, 2.4f, 0.1f), wallMat);
        CreateWall("BackWall_Top", cabin.transform, new Vector3(0f, 1.1f, -4.8f), new Vector3(1.4f, 0.1f, 0.1f), wallMat); // Thin door header at y=1.1, giving 2.2m clearance!

        // --- Beautiful Orange Glowing Entrance Frame Trim ---
        Material orangeGlowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        orangeGlowMat.color = new Color(0.18f, 0.12f, 0.08f); // Dark orange base
        orangeGlowMat.EnableKeyword("_EMISSION");
        orangeGlowMat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.0f) * 3.5f); // High intensity neon orange!

        // Left trim pillar
        GameObject trimLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trimLeft.name = "OrangeTrim_Left";
        trimLeft.transform.SetParent(cabin.transform, false);
        trimLeft.transform.localPosition = new Vector3(-0.7f, -0.05f, -4.75f);
        trimLeft.transform.localScale = new Vector3(0.08f, 2.2f, 0.08f);
        DestroyImmediate(trimLeft.GetComponent<BoxCollider>()); // Visual only
        trimLeft.GetComponent<Renderer>().material = orangeGlowMat;

        // Right trim pillar
        GameObject trimRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trimRight.name = "OrangeTrim_Right";
        trimRight.transform.SetParent(cabin.transform, false);
        trimRight.transform.localPosition = new Vector3(0.7f, -0.05f, -4.75f);
        trimRight.transform.localScale = new Vector3(0.08f, 2.2f, 0.08f);
        DestroyImmediate(trimRight.GetComponent<BoxCollider>()); // Visual only
        trimRight.GetComponent<Renderer>().material = orangeGlowMat;

        // Top trim beam
        GameObject trimTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trimTop.name = "OrangeTrim_Top";
        trimTop.transform.SetParent(cabin.transform, false);
        trimTop.transform.localPosition = new Vector3(0f, 1.05f, -4.75f);
        trimTop.transform.localScale = new Vector3(1.48f, 0.08f, 0.08f);
        DestroyImmediate(trimTop.GetComponent<BoxCollider>()); // Visual only
        trimTop.GetComponent<Renderer>().material = orangeGlowMat;

        // Orange point light above entrance doorway
        GameObject orangeLightGO = new GameObject("OrangeEntranceIndicatorLight");
        orangeLightGO.transform.SetParent(cabin.transform, false);
        orangeLightGO.transform.localPosition = new Vector3(0f, 1.1f, -4.6f);
        Light ol = orangeLightGO.AddComponent<Light>();
        ol.type = LightType.Point;
        ol.color = new Color(1.0f, 0.35f, 0.0f); // High-intensity orange
        ol.intensity = 2.5f;
        ol.range = 5.0f;

        // Floating world prompt sign
        GameObject canvasGO = new GameObject("EntranceWorldCanvas");
        canvasGO.transform.SetParent(cabin.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 1.4f, -4.75f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(3f, 1f);
        canvasGO.AddComponent<BillboardUI>();

        GameObject textGO = new GameObject("EntranceText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TMPro.TextMeshPro tmp = textGO.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "🚍 SPACE BUS\n[Q] Toggle Door";
        tmp.fontSize = 2.2f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(1.0f, 0.5f, 0.0f); // Warm warning orange

        // 6. Create the Back Door (the actual interactive entrance door)
        GameObject doorHinge = new GameObject("BackDoorHinge");
        doorHinge.transform.SetParent(cabin.transform, false);
        doorHinge.transform.localPosition = new Vector3(-0.7f, -0.05f, -4.8f); // Vertically centered at y=-0.05f for 2.2m height!

        GameObject doorMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorMesh.name = "BackDoorMesh";
        doorMesh.transform.SetParent(doorHinge.transform, false);
        doorMesh.transform.localPosition = new Vector3(0.7f, 0f, 0f); // offset by half width
        doorMesh.transform.localScale = new Vector3(1.4f, 2.2f, 0.1f); // Perfectly matches 2.2m opening height!
        
        Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        doorMat.color = new Color(0.24f, 0.28f, 0.35f); // blue metallic
        if (doorMat.HasProperty("_Metallic")) doorMat.SetFloat("_Metallic", 0.85f);
        doorMesh.GetComponent<Renderer>().material = doorMat;

        // Add BusDoor component to Hinge so it swings open!
        BusDoor bd = doorHinge.AddComponent<BusDoor>();
        bd.openAngle = -90f; // swings open 90 degrees outward
        bd.speed = 3f;

        // Exterior door frame — orange pillars visible from OUTSIDE the bus
        Material extFrameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        extFrameMat.color = new Color(0.18f, 0.12f, 0.08f);
        extFrameMat.EnableKeyword("_EMISSION");
        extFrameMat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.0f) * 2.5f);

        GameObject extLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        extLeft.name = "ExtFrame_Left";
        extLeft.transform.SetParent(cabin.transform, false);
        extLeft.transform.localPosition = new Vector3(-0.72f, -0.05f, -4.88f);
        extLeft.transform.localScale = new Vector3(0.1f, 2.2f, 0.16f);
        DestroyImmediate(extLeft.GetComponent<BoxCollider>());
        extLeft.GetComponent<Renderer>().material = extFrameMat;

        GameObject extRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        extRight.name = "ExtFrame_Right";
        extRight.transform.SetParent(cabin.transform, false);
        extRight.transform.localPosition = new Vector3(0.72f, -0.05f, -4.88f);
        extRight.transform.localScale = new Vector3(0.1f, 2.2f, 0.16f);
        DestroyImmediate(extRight.GetComponent<BoxCollider>());
        extRight.GetComponent<Renderer>().material = extFrameMat;

        GameObject extTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        extTop.name = "ExtFrame_Top";
        extTop.transform.SetParent(cabin.transform, false);
        extTop.transform.localPosition = new Vector3(0f, 1.07f, -4.88f);
        extTop.transform.localScale = new Vector3(1.56f, 0.1f, 0.16f);
        DestroyImmediate(extTop.GetComponent<BoxCollider>());
        extTop.GetComponent<Renderer>().material = extFrameMat;

        // Exterior orange point light (shines toward outside)
        GameObject extLightGO = new GameObject("ExteriorDoorLight");
        extLightGO.transform.SetParent(cabin.transform, false);
        extLightGO.transform.localPosition = new Vector3(0f, 0.5f, -5.2f);
        Light extLight = extLightGO.AddComponent<Light>();
        extLight.type = LightType.Point;
        extLight.color = new Color(1.0f, 0.35f, 0.0f);
        extLight.intensity = 3.0f;
        extLight.range = 6f;

        // Exterior floating label (faces outward, away from the bus)
        GameObject extCanvasGO = new GameObject("ExteriorDoorCanvas");
        extCanvasGO.transform.SetParent(cabin.transform, false);
        extCanvasGO.transform.localPosition = new Vector3(0f, 1.5f, -5.1f);
        extCanvasGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Face outward
        Canvas extCanvas = extCanvasGO.AddComponent<Canvas>();
        extCanvas.renderMode = RenderMode.WorldSpace;
        extCanvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(3f, 1f);

        GameObject extTextGO = new GameObject("ExteriorDoorText");
        extTextGO.transform.SetParent(extCanvasGO.transform, false);
        TMPro.TextMeshPro extTmp = extTextGO.AddComponent<TMPro.TextMeshPro>();
        extTmp.text = "DOOR\n[Q] Open / Enter";
        extTmp.fontSize = 2.2f;
        extTmp.alignment = TMPro.TextAlignmentOptions.Center;
        extTmp.color = new Color(1.0f, 0.5f, 0.0f);

        // 9. Add beautiful soft blue lighting inside the cabin for atmosphere (with flickering effects)
        GameObject blueLight1 = new GameObject("InteriorBlueLight1");
        blueLight1.transform.SetParent(cabin.transform, false);
        blueLight1.transform.localPosition = new Vector3(0f, 0.8f, 1.8f);
        Light l1 = blueLight1.AddComponent<Light>();
        l1.type = LightType.Point;
        l1.color = new Color(0.12f, 0.45f, 1.0f); // Pure sci-fi blue
        l1.intensity = 3.0f;
        l1.range = 8f;
        
        var flicker1 = blueLight1.AddComponent<FlickeringLight>();
        flicker1.targetLight = l1;
        flicker1.minIntensity = 1.5f;
        flicker1.maxIntensity = 4.0f;
        flicker1.flickerSpeed = 0.08f;

        GameObject blueLight2 = new GameObject("InteriorBlueLight2");
        blueLight2.transform.SetParent(cabin.transform, false);
        blueLight2.transform.localPosition = new Vector3(0f, 0.8f, -1.8f);
        Light l2 = blueLight2.AddComponent<Light>();
        l2.type = LightType.Point;
        l2.color = new Color(0.12f, 0.45f, 1.0f);
        l2.intensity = 3.0f;
        l2.range = 8f;

        var flicker2 = blueLight2.AddComponent<FlickeringLight>();
        flicker2.targetLight = l2;
        flicker2.minIntensity = 1.5f;
        flicker2.maxIntensity = 4.0f;
        flicker2.flickerSpeed = 0.08f;

        // 10. Create procedural glowing Control Terminal/Screen inside (Lethal Company style cockpit dashboard)
        GameObject consoleDesk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        consoleDesk.name = "Cockpit_Console_Desk";
        consoleDesk.transform.SetParent(cabin.transform, false);
        consoleDesk.transform.localPosition = new Vector3(0f, -0.6f, 4.3f);
        consoleDesk.transform.localScale = new Vector3(2.5f, 0.8f, 0.8f);
        
        Material deskMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        deskMat.color = new Color(0.15f, 0.15f, 0.17f);
        if (deskMat.HasProperty("_Metallic")) deskMat.SetFloat("_Metallic", 0.75f);
        consoleDesk.GetComponent<Renderer>().material = deskMat;

        // Add 2 glowing display screens on the desk
        GameObject screen1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen1.name = "Screen_Status_Display";
        screen1.transform.SetParent(consoleDesk.transform, false);
        screen1.transform.localPosition = new Vector3(-0.5f, 0.7f, 0.1f);
        screen1.transform.localScale = new Vector3(0.35f, 0.6f, 0.1f);
        screen1.transform.localRotation = Quaternion.Euler(-15f, 15f, 0f);
        DestroyImmediate(screen1.GetComponent<BoxCollider>());

        Material cyanScreen = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        cyanScreen.EnableKeyword("_EMISSION");
        cyanScreen.color = new Color(0f, 0.35f, 0.4f);
        cyanScreen.SetColor("_EmissionColor", Color.cyan * 2.2f); // glowing cyan data screen
        screen1.GetComponent<Renderer>().material = cyanScreen;

        GameObject screen2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen2.name = "Screen_Nav_Radar";
        screen2.transform.SetParent(consoleDesk.transform, false);
        screen2.transform.localPosition = new Vector3(0.5f, 0.7f, 0.1f);
        screen2.transform.localScale = new Vector3(0.35f, 0.6f, 0.1f);
        screen2.transform.localRotation = Quaternion.Euler(-15f, -15f, 0f);
        DestroyImmediate(screen2.GetComponent<BoxCollider>());

        Material greenScreen = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        greenScreen.EnableKeyword("_EMISSION");
        greenScreen.color = new Color(0f, 0.4f, 0.1f);
        greenScreen.SetColor("_EmissionColor", Color.green * 2.0f); // glowing radar green screen
        screen2.GetComponent<Renderer>().material = greenScreen;

        // Auto-refresh the doors array so BusController updates it
        doors = new BusDoor[] { bd };
        Debug.Log("[Diagnostic] ReconstructBusInterior completed successfully!");
    }

    private void CreateWall(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;
    }

    private void CreateLootStorageZone()
    {
        GameObject storageZone = new GameObject("LootStorageZone");
        storageZone.transform.SetParent(this.transform, false);
        // Place right on the custom cabin floor (Floor is at Y = -1.2)
        storageZone.transform.localPosition = new Vector3(0f, -1.18f, -1.8f); // Cargo bay area

        BoxCollider trigger = storageZone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2.5f, 2.0f, 2.5f);
        trigger.center = new Vector3(0f, 1.0f, 0f); // perfectly encapsulates player inside trigger

        storageZone.AddComponent<LootStorageZone>();

        // Visual Outline Area (Flat green quad on the floor)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
        visual.name = "StorageVisualPlane";
        visual.transform.SetParent(storageZone.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.01f, 0f); // slightly elevated to avoid Z-fighting
        visual.transform.localScale = new Vector3(0.25f, 1f, 0.25f); // 2.5m x 2.5m area
        DestroyImmediate(visual.GetComponent<MeshCollider>());

        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.EnableKeyword("_EMISSION");
            mat.color = new Color(0.0f, 0.4f, 0.0f, 0.8f);
            mat.SetColor("_EmissionColor", Color.green * 2.5f);
            rend.material = mat;
        }

        // Floating world prompt
        GameObject canvasGO = new GameObject("StorageWorldCanvas");
        canvasGO.transform.SetParent(storageZone.transform, false);
        canvasGO.transform.localPosition = Vector3.up * 1.2f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(3f, 1f);
        canvasGO.AddComponent<BillboardUI>();

        GameObject textGO = new GameObject("StoragePromptText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TMPro.TextMeshPro tmp = textGO.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "LOOT STORAGE ZONE\n[E] Deposit Cargo";
        tmp.fontSize = 2.4f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.green;
    }

    private void CreateNavigationBeacon()
    {
        // Add a pulsing blue navigation light above the bus
        GameObject beacon = new GameObject("NavigationBeaconLight");
        beacon.transform.SetParent(this.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 3f, 0f); // High above the hull

        Light l = beacon.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.5f, 1.0f); // Beacon blue
        l.intensity = 3.0f;
        l.range = 25f;

        beacon.AddComponent<PulsingBeacon>();

        // Add a world canvas label: "SPACE BUS" high in the sky
        GameObject canvasGO = new GameObject("BusSkyWorldCanvas");
        canvasGO.transform.SetParent(this.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 4.5f, 0f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(4f, 1.5f);
        canvasGO.AddComponent<BillboardUI>();

        GameObject textGO = new GameObject("BusSkyText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TMPro.TextMeshPro tmp = textGO.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "SPACE BUS\n[Return / Store]";
        tmp.fontSize = 3.2f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.3f, 0.7f, 1.0f);
    }

    private void SnapToGround()
    {
        RaycastHit hit;
        // Cast down to align to terrain
        if (Physics.Raycast(transform.position + Vector3.up * 15f, Vector3.down, out hit, 40f))
        {
            transform.position = hit.point + Vector3.up * 1.5f; // Snaps center so wheels sit perfectly on the ground!
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !isOccupied;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void SetupAlarm()
    {
        alarmSource = gameObject.AddComponent<AudioSource>();
        alarmSource.loop = false;
        alarmSource.playOnAwake = false;
        alarmSource.spatialBlend = 1.0f; // 3D sound
        alarmSource.minDistance = 5f;
        alarmSource.maxDistance = 30f;
        alarmClip = CreateAlarmBeepClip();
    }

    private AudioClip CreateAlarmBeepClip()
    {
        int samplerate = 44100;
        float frequency = 900f; // 900Hz tone for alarm beep
        float duration = 0.25f;
        int sampleCount = Mathf.RoundToInt(samplerate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / samplerate) * 0.5f;
        }
        AudioClip clip = AudioClip.Create("AlarmBeep", sampleCount, 1, samplerate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void HandleAlarmSound()
    {
        if (currentHealth < maxHealth * 0.25f && currentHealth > 0f)
        {
            alarmTimer -= Time.deltaTime;
            if (alarmTimer <= 0f)
            {
                alarmTimer = 1.0f; // Beep every second
                if (alarmSource != null && alarmClip != null)
                {
                    alarmSource.PlayOneShot(alarmClip);
                }
            }
        }
    }

    private void HandleLowPowerFlicker()
    {
        if (powerSystem != null && powerSystem.isPowerOn && powerSystem.currentPower < 30f)
        {
            bool flicker = (Random.value > 0.3f);
            foreach (var l in interiorLights)
            {
                if (l != null) l.enabled = flicker;
            }
        }
    }

    public void EnterBus(GameObject player)
    {
        if (isOccupied) return;
        isOccupied = true;
        currentDriver  = player;
        driverMovement = player.GetComponent<PlayerMovement>();

        // Disable player movement, not the whole GO
        if (driverMovement != null) driverMovement.enabled = false;

        // Seat the player inside
        player.transform.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.3f;
        player.transform.SetParent(transform);

        // Turn off isKinematic so we can drive with physical movements
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Debug.Log("[Bus] Player boarded.");
    }

    public void ExitBus()
    {
        if (!isOccupied) return;
        isOccupied = false;

        if (currentDriver != null)
        {
            currentDriver.transform.SetParent(null);
            currentDriver.transform.position = transform.position + transform.right * 3f + Vector3.up * 0.5f;
            if (driverMovement != null)
            {
                driverMovement.enabled = true;
                driverMovement.isInsideBus = false;
            }
            currentDriver = null;
            driverMovement = null;
        }

        // Freeze bus instantly on exit so it doesn't roll away or fall
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("[Bus] Player exited.");
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        damageFlickerTimer = 0.5f;
        Debug.Log($"[Bus] Damage taken. HP: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f) OnBusDestroyed();
    }

    public void Repair(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void OnBusDestroyed()
    {
        Debug.LogWarning("[Bus] Bus destroyed!");
        if (powerSystem != null) powerSystem.SetPower(false);
        // Force return to garage
        GameLoopManager.Instance?.ReturnToGarage();
    }

    private void Update()
    {
        if (PlayerInputController.Instance == null) return;

        HandleDrivingInput();
        HandleDamageFlicker();
        HandleAlarmSound();
        UpdateWindowVisibility();

        // Q key: enter or exit bus interior (walk-in, not driving)
        bool inputUnlocked = BusTransitionController.Instance == null || !BusTransitionController.Instance.IsInputLocked;
        if (!isOccupied && inputUnlocked && PlayerInputController.Instance.DoorOpenClosePressed && IsPlayerNearby())
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null && BusTransitionController.Instance != null)
            {
                if (!pm.isInsideBus && BusTransitionController.Instance.CurrentState == BusTransitionController.BusCabinState.OutsideBus)
                {
                    bool anyDoorOpen = System.Array.Exists(doors, d => d != null && d.isOpen);
                    if (anyDoorOpen)
                    {
                        // Door already open — close it, stay outside
                        foreach (var d in doors) { if (d != null && d.isOpen) d.ToggleDoor(); }
                    }
                    else
                    {
                        // Door closed — open it and enter
                        foreach (var d in doors) d?.ToggleDoor();
                        BusTransitionController.Instance.EnterBus(pm, new Vector3(0f, 0f, -1.5f));
                    }
                }
                else if (pm.isInsideBus && BusTransitionController.Instance.CurrentState == BusTransitionController.BusCabinState.InsideBus)
                {
                    foreach (var d in doors) d?.ToggleDoor();
                    // Exit to world position just outside the back of the bus
                    Vector3 exitWorldPos = transform.TransformPoint(new Vector3(0f, 1.5f, -6.5f));
                    BusTransitionController.Instance.ExitBus(pm, exitWorldPos);
                }
            }
            else if (pm != null && BusTransitionController.Instance == null)
            {
                // Fallback: no transition controller — just toggle door
                foreach (var d in doors) d?.ToggleDoor();
            }
        }
    }

    private void UpdateWindowVisibility()
    {
        // Check if player is inside or driving
        bool inside = isOccupied;
        if (!inside)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null && pm.isInsideBus) inside = true;
        }

        // Toggle windows
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.gameObject.name.ToLower().Contains("window") || r.gameObject.name.ToLower().Contains("glass"))
            {
                // When inside, we hide the window meshes so they don't render external scenery,
                // leaving our beautiful solid, opaque cargo cabin walls to block the outside view.
                // When outside, we keep them active so the glass looks natural and reflections work.
                r.enabled = !inside;
            }
        }
    }

    private void HandleDrivingInput()
    {
        if (!isOccupied) return;

        // Exit bus should always be allowed regardless of power!
        if (PlayerInputController.Instance.ExitBusPressed)
        {
            ExitBus();
            return;
        }

        // R from pilot seat → return to base
        if (PlayerInputController.Instance.ReturnToBasePressed)
        {
            ExitBus();
            GameLoopManager.Instance?.ReturnToGarage();
            return;
        }

        bool hasPower = powerSystem == null || powerSystem.isPowerOn;
        if (!hasPower) return;

        Vector2 moveInput = PlayerInputController.Instance.MoveInput;
        float move = moveInput.y * moveSpeed * Time.deltaTime;
        float turn = moveInput.x * 45f * Time.deltaTime;
        transform.Translate(0f, 0f, move);
        transform.Rotate(0f, turn, 0f);
    }

    private void HandleDamageFlicker()
    {
        if (damageFlickerTimer <= 0f) return;
        damageFlickerTimer -= Time.deltaTime;
        bool flicker = (Mathf.Sin(Time.time * 40f) > 0f);
        foreach (var l in interiorLights)
            if (l != null) l.enabled = flicker;

        if (damageFlickerTimer <= 0f)
            foreach (var l in interiorLights)
                if (l != null) l.enabled = powerSystem == null || powerSystem.isPowerOn;
    }

    private bool IsPlayerNearby()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.transform.position) < 6f;
    }
}
