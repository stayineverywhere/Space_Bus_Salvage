using UnityEngine;

/// <summary>
/// Per-scene bootstrapper. Attach to a GameObject in each scene.
/// Safe to call multiple times — checks for existing objects before creating.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    public static GameObject ExteriorWorldRoot { get; private set; }

    [Header("Prefabs (optional)")]
    public GameObject playerPrefab;
    public GameObject busPrefab;

    [Header("Generation Prefabs (optional)")]
    public GameObject[] monsterPrefabs;
    public GameObject[] lootPrefabs;

    [Header("Spawn Points")]
    public Vector3 playerSpawnPos = new Vector3(0f, 1f, 0f);
    public Vector3 busSpawnPos    = new Vector3(5f, 0f, 5f);

    private void Start()
    {
#if UNITY_EDITOR
        if (playerPrefab == null)
            playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (busPrefab == null)
            busPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpaceBus.prefab");
        
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            monsterPrefabs = new GameObject[]
            {
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Watcher.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cleaner.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lurker.prefab")
            };
        }
        if (lootPrefabs == null || lootPrefabs.Length == 0)
        {
            lootPrefabs = new GameObject[]
            {
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CursedDoll.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BrokenClock.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedFrame.prefab")
            };
        }
#endif
        BootstrapScene();
    }

    public void BootstrapScene()
    {
        Debug.Log($"[Bootstrap] Scene: {gameObject.scene.name}");

        // Create the unified container for separating the exterior world from bus interior
        ExteriorWorldRoot = GameObject.Find("ExteriorWorld_Root");
        if (ExteriorWorldRoot == null)
        {
            ExteriorWorldRoot = new GameObject("ExteriorWorld_Root");
        }

        EnsureCoreSystems();
        SetupEnvironment();
        
        if (gameObject.scene.name == "GarageHub")
        {
            CleanGarageHubScene();
        }
        else
        {
            BuildProceduralLevel();
        }

        SpawnPlayer();
        SpawnBus();

        SafeSpawnManager.Instance?.ResetAllPhysics();
    }

    private void CleanGarageHubScene()
    {
        Debug.Log("[Bootstrap] Cleaning up exploration hazards from GarageHub safe base...");

        // Restore default skybox in GarageHub base
#if UNITY_EDITOR
        Material defaultSky = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        if (defaultSky != null)
        {
            RenderSettings.skybox = defaultSky;
        }
#endif
        
        // Deactivate pre-placed monsters in GarageHub
        foreach (var m in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Include))
        {
            m.gameObject.SetActive(false);
        }
        
        // Deactivate pre-placed loot pickups in GarageHub
        foreach (var l in Object.FindObjectsByType<LootPickup>(FindObjectsInactive.Include))
        {
            l.gameObject.SetActive(false);
        }

        // Deactivate pre-placed planet zone triggers in GarageHub
        foreach (var p in Object.FindObjectsByType<PlanetZoneTrigger>(FindObjectsInactive.Include))
        {
            p.gameObject.SetActive(false);
        }

        // Deactivate survivors pre-placed in GarageHub
        foreach (var s in Object.FindObjectsByType<Survivor>(FindObjectsInactive.Include))
        {
            s.gameObject.SetActive(false);
        }
    }

    private void EnsureCoreSystems()
    {
        // If the master initializer didn't run (e.g. scene loaded directly), boot it now
        if (GameStartInitializer.Instance == null)
            new GameObject("GameStartInitializer", typeof(GameStartInitializer));

        // Create the screen transition controller
        if (BusTransitionController.Instance == null)
        {
            new GameObject("BusTransitionController", typeof(BusTransitionController));
        }
    }

    private void SetupEnvironment()
    {
        // Directional light
        bool hasDirectional = false;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (l.type == LightType.Directional)
            {
                hasDirectional = true;
                if (ExteriorWorldRoot != null) l.transform.SetParent(ExteriorWorldRoot.transform);
                break;
            }
        }

        if (!hasDirectional)
        {
            GameObject lg = new GameObject("DirectionalLight");
            Light dl = lg.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.intensity = 0.8f;
            dl.color = new Color(0.7f, 0.8f, 1f);
            lg.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            if (ExteriorWorldRoot != null) lg.transform.SetParent(ExteriorWorldRoot.transform);
        }

        // Camera
        if (Camera.main == null)
        {
            GameObject cg = new GameObject("Main Camera");
            cg.tag = "MainCamera";
            Camera cam = cg.AddComponent<Camera>();
            cam.nearClipPlane = 0.15f; // tighter near clip to reduce mesh clipping artifacts
            cg.AddComponent<AudioListener>();
        }
        else
        {
            // Tighten near clip on existing camera too
            if (Camera.main.nearClipPlane > 0.15f)
                Camera.main.nearClipPlane = 0.15f;
        }

        // Horror visuals
        if (HorrorVisualPreset.Instance == null)
            new GameObject("HorrorVisualSystem", typeof(HorrorVisualPreset));
    }

    private void BuildProceduralLevel()
    {
        if (gameObject.scene.name != "PlanetExploration") return;

        Debug.Log("[Bootstrap] Generating procedural planet exploration level...");

        // 1. Create Ground Plane — large enough to meet the mountain bases (radius ~65m)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Procedural_Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200m x 200m
        if (ExteriorWorldRoot != null) ground.transform.SetParent(ExteriorWorldRoot.transform);

        // Apply dark terrain material
        MeshRenderer mr = ground.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var theme = PlanetManager.Instance != null ? PlanetManager.Instance.activeTheme : HorrorVisualPreset.PlanetTheme.Default;
            switch (theme)
            {
                case HorrorVisualPreset.PlanetTheme.Ice:
                    m.color = new Color(0.7f, 0.8f, 0.9f); // light cold gray
                    break;
                case HorrorVisualPreset.PlanetTheme.Toxic:
                    m.color = new Color(0.2f, 0.3f, 0.2f); // toxic sludge green
                    break;
                case HorrorVisualPreset.PlanetTheme.Mining:
                    m.color = new Color(0.15f, 0.1f, 0.08f); // dark coal/rock
                    break;
                case HorrorVisualPreset.PlanetTheme.Gravity:
                    m.color = new Color(0.15f, 0.05f, 0.25f); // cosmic purple
                    break;
                default:
                    m.color = new Color(0.1f, 0.1f, 0.12f); // default dark concrete
                    break;
            }
            mr.material = m;
        }

        // 2. Create High-Fidelity Sci-Fi Ruins / Obstacles procedurally
        int ruinsCount = 15;
        for (int i = 0; i < ruinsCount; i++)
        {
            Vector3 ruinPos = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
            
            // Align position perfectly with the ground surface
            RaycastHit groundHit;
            if (Physics.Raycast(ruinPos + Vector3.up * 20f, Vector3.down, out groundHit, 40f))
            {
                ruinPos = groundHit.point;
            }

            // Ensure away from player and bus spawns to prevent getting stuck
            if (Vector3.Distance(ruinPos, playerSpawnPos) < 15f || Vector3.Distance(ruinPos, busSpawnPos) < 15f)
                continue;

            SpawnProceduralSciFiStructure(ruinPos, i);
        }

        // 3. Build NavMesh Surface
        var surface = ground.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.BuildNavMesh();
        Debug.Log("[Bootstrap] NavMesh built successfully.");

        // 4. Spawn Loot items randomly on the ground
        int lootCount = Random.Range(8, 12);
        int spawned = 0;

        // Try prefab-based spawn first
        if (lootPrefabs != null)
        {
            // Filter out null entries
            var validPrefabs = System.Array.FindAll(lootPrefabs, p => p != null);
            if (validPrefabs.Length > 0)
            {
                for (int i = 0; i < lootCount; i++)
                {
                    Vector3 lootPos = GetRandomGroundPosition(40f);
                    GameObject inst = Instantiate(validPrefabs[Random.Range(0, validPrefabs.Length)], lootPos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                    if (ExteriorWorldRoot != null) inst.transform.SetParent(ExteriorWorldRoot.transform);
                    // ALWAYS assign value — prefab may have value=0
                    AssignLootValue(inst);
                    spawned++;
                }
            }
        }

        // Procedural fallback if no prefabs available
        if (spawned == 0)
        {
            for (int i = 0; i < lootCount; i++)
            {
                Vector3 lootPos = GetRandomGroundPosition(40f);
                SpawnProceduralLoot(lootPos);
            }
        }

        // 5. Spawn Monsters randomly away from player and bus
        if (monsterPrefabs != null && monsterPrefabs.Length > 0)
        {
            int monsterCount = Random.Range(3, 5);
            for (int i = 0; i < monsterCount; i++)
            {
                Vector3 monsterPos = new Vector3(Random.Range(-45f, 45f), 1f, Random.Range(-45f, 45f));
                if (Vector3.Distance(monsterPos, playerSpawnPos) < 20f || Vector3.Distance(monsterPos, busSpawnPos) < 20f)
                {
                    continue;
                }
                // Raycast down
                RaycastHit hit;
                if (Physics.Raycast(monsterPos + Vector3.up * 10f, Vector3.down, out hit, 20f))
                {
                    monsterPos = hit.point + Vector3.up * 0.1f;
                }
                GameObject selectedMonster = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
                if (selectedMonster != null)
                {
                    GameObject inst = Instantiate(selectedMonster, monsterPos, Quaternion.identity);
                    if (ExteriorWorldRoot != null) inst.transform.SetParent(ExteriorWorldRoot.transform);
                }
            }
        }

        // 6. Spawn Atmospheric particles
        SpawnAtmosphericParticles();

        // 7. Spawn theme-specific environmental structures
        SpawnThemeEnvironment();

        // Circular boundary wall following the mountain ring
        AddInvisibleBoundaryWalls(44f);
    }

    private Vector3 GetRandomGroundPosition(float range)
    {
        Vector3 pos = new Vector3(Random.Range(-range, range), 1f, Random.Range(-range, range));
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            pos = hit.point + Vector3.up * 0.2f;
        return pos;
    }

    private void AssignLootValue(GameObject inst)
    {
        LootItem item = inst.GetComponent<LootItem>();
        if (item == null) item = inst.GetComponentInChildren<LootItem>();
        if (item == null) return;

        // Set value based on type if it's still 0 (prefab default)
        if (item.value <= 0)
        {
            if (item is CursedDoll)      { item.value = Random.Range(150, 280); item.itemName = "Cursed Doll"; }
            else if (item is BrokenClock) { item.value = Random.Range(80, 160);  item.itemName = "Broken Clock"; }
            else if (item is RedFrame)    { item.value = Random.Range(200, 350); item.itemName = "Red Frame"; }
            else                          { item.value = Random.Range(60, 200);  }
        }
        if (string.IsNullOrEmpty(item.itemName)) item.itemName = item.GetType().Name;
        Debug.Log($"[Bootstrap] Spawned loot: {item.itemName} (value={item.value})");
    }

    private void SpawnProceduralLoot(Vector3 pos)
    {
        // Pick a random loot type
        int type = Random.Range(0, 3);
        GameObject go = new GameObject();
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        if (ExteriorWorldRoot != null) go.transform.SetParent(ExteriorWorldRoot.transform);

        LootItem item;
        switch (type)
        {
            case 0:
                go.name = "CursedDoll";
                item = go.AddComponent<CursedDoll>();
                item.itemName = "Cursed Doll";
                item.value = Random.Range(150, 280);
                item.curseValue = Random.Range(20f, 35f);
                break;
            case 1:
                go.name = "BrokenClock";
                item = go.AddComponent<BrokenClock>();
                item.itemName = "Broken Clock";
                item.value = Random.Range(80, 160);
                item.curseValue = Random.Range(10f, 20f);
                break;
            default:
                go.name = "RedFrame";
                item = go.AddComponent<RedFrame>();
                item.itemName = "Red Frame";
                item.value = Random.Range(200, 350);
                item.curseValue = Random.Range(15f, 30f);
                break;
        }

        // Add visual mesh and pickup component
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.transform.SetParent(go.transform, false);
        mesh.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Destroy(mesh.GetComponent<BoxCollider>());
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", type == 0 ? Color.red * 2f : type == 1 ? Color.yellow * 2f : Color.magenta * 2f);
        mesh.GetComponent<Renderer>().material = mat;

        go.AddComponent<LootPickup>();
        Debug.Log($"[Bootstrap] Procedural loot spawned: {item.itemName} (value={item.value})");
    }

    private void SpawnProceduralSciFiStructure(Vector3 position, int index)
    {
        GameObject root = new GameObject("SciFi_Obstacle_" + index);
        root.transform.position = position;
        if (ExteriorWorldRoot != null) root.transform.SetParent(ExteriorWorldRoot.transform);

        int choice = Random.Range(0, 4);
        Material metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        metalMat.color = new Color(0.18f, 0.18f, 0.2f);
        if (metalMat.HasProperty("_Metallic")) metalMat.SetFloat("_Metallic", 0.8f);
        if (metalMat.HasProperty("_Glossiness")) metalMat.SetFloat("_Glossiness", 0.6f);

        Material emissiveMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        emissiveMat.EnableKeyword("_EMISSION");
        Color glowColor = Color.cyan * 1.5f;
        var theme = PlanetManager.Instance != null ? PlanetManager.Instance.activeTheme : HorrorVisualPreset.PlanetTheme.Default;
        if (theme == HorrorVisualPreset.PlanetTheme.Toxic) glowColor = Color.green * 1.8f;
        else if (theme == HorrorVisualPreset.PlanetTheme.Mining) glowColor = new Color(1f, 0.4f, 0f) * 1.8f; // orange warning
        else if (theme == HorrorVisualPreset.PlanetTheme.Gravity) glowColor = Color.magenta * 2.0f;
        emissiveMat.SetColor("_EmissionColor", glowColor);

        if (choice == 0)
        {
            // ── Alien Ruin Pillar ──
            int parts = Random.Range(3, 5);
            float currentHeight = 0f;
            for (int i = 0; i < parts; i++)
            {
                float w = Random.Range(2.5f, 4.5f) / (i + 1f);
                float h = Random.Range(2f, 4f);
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = new Vector3(Random.Range(-0.2f, 0.2f), currentHeight + h/2f, Random.Range(-0.2f, 0.2f));
                part.transform.localScale = new Vector3(w, h, w);
                part.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
                part.GetComponent<Renderer>().material = metalMat;
                
                // Add a glowing energy band in between
                if (i < parts - 1)
                {
                    GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    band.transform.SetParent(root.transform, false);
                    band.transform.localPosition = new Vector3(0f, currentHeight + h, 0f);
                    band.transform.localScale = new Vector3(w + 0.15f, 0.15f, w + 0.15f);
                    Destroy(band.GetComponent<BoxCollider>());
                    band.GetComponent<Renderer>().material = emissiveMat;
                }
                currentHeight += h;
            }
        }
        else if (choice == 1)
        {
            // ── Industrial Machinery & Pipes ──
            // Center main box
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.transform.SetParent(root.transform, false);
            machine.transform.localPosition = Vector3.up * 1.5f;
            machine.transform.localScale = new Vector3(3f, 3f, 3f);
            machine.GetComponent<Renderer>().material = metalMat;

            // Horizontal pipes
            int pipes = Random.Range(2, 4);
            for (int i = 0; i < pipes; i++)
            {
                GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.transform.SetParent(root.transform, false);
                pipe.transform.localPosition = new Vector3(0f, 1f + i * 0.8f, 0f);
                pipe.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
                pipe.transform.rotation = Quaternion.Euler(90f, i * 45f, 0f);
                pipe.GetComponent<Renderer>().material = metalMat;
            }

            // Glowing status panels
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 2.2f, 1.55f);
            panel.transform.localScale = new Vector3(1.2f, 0.6f, 0.1f);
            Destroy(panel.GetComponent<BoxCollider>());
            panel.GetComponent<Renderer>().material = emissiveMat;
        }
        else if (choice == 2)
        {
            // ── Debris Pile / Wreckage ──
            int debrisCount = Random.Range(5, 9);
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.transform.SetParent(root.transform, false);
                debris.transform.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(0.2f, 1.2f), Random.Range(-2f, 2f));
                debris.transform.localScale = new Vector3(Random.Range(0.8f, 2.5f), Random.Range(0.8f, 2.5f), Random.Range(0.8f, 2.5f));
                debris.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                
                // Rust steel color
                Material rustMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rustMat.color = new Color(0.18f, 0.12f, 0.08f); // brown rust
                if (rustMat.HasProperty("_Glossiness")) rustMat.SetFloat("_Glossiness", 0.1f);
                debris.GetComponent<Renderer>().material = rustMat;
            }
        }
        else
        {
            // ── Sci-Fi Crates Stack ──
            int crateCount = Random.Range(4, 7);
            Vector3[] localOffsets = new Vector3[]
            {
                new Vector3(-0.9f, 0.6f, -0.9f),
                new Vector3(0.9f, 0.6f, -0.9f),
                new Vector3(-0.9f, 0.6f, 0.9f),
                new Vector3(0.9f, 0.6f, 0.9f),
                new Vector3(-0.4f, 1.8f, -0.4f),
                new Vector3(0.4f, 1.8f, 0.4f)
            };
            for (int i = 0; i < Mathf.Min(crateCount, localOffsets.Length); i++)
            {
                GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.transform.SetParent(root.transform, false);
                crate.transform.localPosition = localOffsets[i];
                crate.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                crate.transform.rotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
                crate.GetComponent<Renderer>().material = metalMat;

                // Emissive corner highlights on crates
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                corner.transform.SetParent(crate.transform, false);
                corner.transform.localPosition = new Vector3(0f, 0.61f, 0f);
                corner.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
                Destroy(corner.GetComponent<BoxCollider>());
                corner.GetComponent<Renderer>().material = emissiveMat;
            }
        }
    }

    private void SpawnThemeEnvironment()
    {
        var theme = PlanetManager.Instance != null ? PlanetManager.Instance.activeTheme : HorrorVisualPreset.PlanetTheme.Default;

        switch (theme)
        {
            case HorrorVisualPreset.PlanetTheme.Ice:
                SpawnIceEnvironment();
                break;
            case HorrorVisualPreset.PlanetTheme.Toxic:
                SpawnToxicEnvironment();
                break;
            case HorrorVisualPreset.PlanetTheme.Mining:
                SpawnMiningEnvironment();
                break;
            case HorrorVisualPreset.PlanetTheme.Gravity:
                SpawnGravityEnvironment();
                break;
            default:
                SpawnDefaultEnvironment();
                break;
        }
    }

    private void SpawnIceEnvironment()
    {
        // 1. Setup Sunrise Skybox
#if UNITY_EDITOR
        Material snowSkybox = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Snow Mountain/Materials/enviromentMap.mat");
        if (snowSkybox != null)
        {
            RenderSettings.skybox = snowSkybox;
        }
#endif

        // 2. Spawn massive backdrop mountains around the map boundaries (outside play area)
#if UNITY_EDITOR
        GameObject mountainPF = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Snow Mountain/Prefab/mountain_Snow_000.prefab");
        if (mountainPF != null)
        {
            int mountainCount = 8;
            for (int i = 0; i < mountainCount; i++)
            {
                float angle = (i * 360f / mountainCount) * Mathf.Deg2Rad;
                float radius = 55f + Random.Range(0f, 10f);
                // Y=-3 so the mountain base is buried underground — only slopes protrude, blending naturally with the flat ground
                Vector3 mountainPos = new Vector3(Mathf.Cos(angle) * radius, -3f, Mathf.Sin(angle) * radius);

                GameObject mInst = Instantiate(mountainPF, mountainPos, Quaternion.identity);
                mInst.name = "BackgroundMountain_" + i;

                // Point mountain toward center of play area
                Vector3 dirToCenter = (Vector3.zero - mountainPos);
                dirToCenter.y = 0f;
                if (dirToCenter != Vector3.zero)
                {
                    mInst.transform.rotation = Quaternion.LookRotation(dirToCenter);
                }

                // Scale up for majestic look
                mInst.transform.localScale = new Vector3(2.5f, Random.Range(1.8f, 2.5f), 2.5f);
                if (ExteriorWorldRoot != null) mInst.transform.SetParent(ExteriorWorldRoot.transform);

                // Mountain is purely visual — remove all colliders
                foreach (Collider col in mInst.GetComponentsInChildren<Collider>(true))
                    Destroy(col);

                // Fix pink material: replace Built-in shaders with URP equivalents
                FixMaterialsForURP(mInst);
            }
        }
#endif



        // 4. Spawn Hail Particles Pack (눈보라 우박 효과)
#if UNITY_EDITOR
        GameObject hailPF = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Realistic Hail Set/Prefabs/Hail - Heavy.prefab");
        if (hailPF != null)
        {
            GameObject hailInst = Instantiate(hailPF, Vector3.zero, Quaternion.identity);
            hailInst.name = "HailParticlesEffect";
            if (ExteriorWorldRoot != null) hailInst.transform.SetParent(ExteriorWorldRoot.transform);
            FixMaterialsForURP(hailInst);
        }
#endif

        // Ice stalagmite pillars and frozen ground
        Material iceMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        iceMat.color = new Color(0.78f, 0.9f, 1.0f);
        if (iceMat.HasProperty("_Metallic")) iceMat.SetFloat("_Metallic", 0.05f);
        if (iceMat.HasProperty("_Glossiness")) iceMat.SetFloat("_Glossiness", 0.95f);

        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-50f, 50f), 0f, Random.Range(-50f, 50f));
            if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;

            // Cluster of ice pillars
            int clusterSize = Random.Range(1, 4);
            for (int j = 0; j < clusterSize; j++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                float height = Random.Range(2f, 8f);
                float width = Random.Range(0.4f, 1.2f);

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "IcePillar";
                pillar.transform.position = pos + offset;
                pillar.transform.localScale = new Vector3(width, height * 0.5f, width);
                pillar.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                pillar.GetComponent<Renderer>().material = iceMat;
                if (ExteriorWorldRoot != null) pillar.transform.SetParent(ExteriorWorldRoot.transform);
            }
        }

        // Frozen lake patches (flat reflective planes)
        Material frozenMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        frozenMat.color = new Color(0.7f, 0.85f, 0.95f, 0.9f);
        if (frozenMat.HasProperty("_Glossiness")) frozenMat.SetFloat("_Glossiness", 1.0f);

        for (int i = 0; i < 6; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-40f, 40f), 0.01f, Random.Range(-40f, 40f));
            GameObject lake = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lake.name = "FrozenLake";
            lake.transform.position = pos;
            lake.transform.localScale = new Vector3(Random.Range(0.8f, 2.0f), 1f, Random.Range(0.8f, 2.0f));
            lake.GetComponent<Renderer>().material = frozenMat;
            Destroy(lake.GetComponent<MeshCollider>());
            if (ExteriorWorldRoot != null) lake.transform.SetParent(ExteriorWorldRoot.transform);
        }
    }

    private void SpawnToxicEnvironment()
    {
        // Toxic pools that damage the player
        Material poolMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        poolMat.EnableKeyword("_EMISSION");
        poolMat.color = new Color(0.1f, 0.4f, 0.1f, 0.85f);
        poolMat.SetColor("_EmissionColor", new Color(0.0f, 1.0f, 0.0f) * 0.8f);
        if (poolMat.HasProperty("_Glossiness")) poolMat.SetFloat("_Glossiness", 0.9f);

        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-45f, 45f), 0.02f, Random.Range(-45f, 45f));
            if (Vector3.Distance(pos, playerSpawnPos) < 10f || Vector3.Distance(pos, busSpawnPos) < 10f) continue;

            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Plane);
            pool.name = "ToxicPool";
            pool.transform.position = pos;
            pool.transform.localScale = new Vector3(Random.Range(0.5f, 1.5f), 1f, Random.Range(0.5f, 1.5f));
            pool.GetComponent<Renderer>().material = poolMat;

            // Replace MeshCollider with a Trigger for damage
            Destroy(pool.GetComponent<MeshCollider>());
            BoxCollider bc = pool.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(10f, 0.5f, 10f);
            bc.center = new Vector3(0f, 0.25f, 0f);
            pool.AddComponent<ToxicPoolDamage>();
            if (ExteriorWorldRoot != null) pool.transform.SetParent(ExteriorWorldRoot.transform);
        }

        // Corroded industrial vats
        Material vat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        vat.color = new Color(0.15f, 0.25f, 0.15f);
        if (vat.HasProperty("_Metallic")) vat.SetFloat("_Metallic", 0.6f);

        for (int i = 0; i < 6; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f));
            if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "ToxicVat";
            cylinder.transform.position = pos + Vector3.up;
            cylinder.transform.localScale = new Vector3(Random.Range(1f, 2f), Random.Range(1f, 2f), Random.Range(1f, 2f));
            cylinder.GetComponent<Renderer>().material = vat;
            if (ExteriorWorldRoot != null) cylinder.transform.SetParent(ExteriorWorldRoot.transform);
        }
    }

    private void SpawnMiningEnvironment()
    {
        // 1. Spawn desolate trees and cacti from the Fog Forest (Fake Fog) pack to enhance visual depth and atmosphere!
#if UNITY_EDITOR
        GameObject dryTreePF = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cody Dreams/Fake Fog Presets/Prefabs/Tree.prefab");
        GameObject cactiPF = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cody Dreams/Fake Fog Presets/Prefabs/Cacti.prefab");
        
        if (dryTreePF != null)
        {
            int treeCount = Random.Range(10, 16);
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
                if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;
                
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    pos = hit.point;
                }
                
                GameObject inst = Instantiate(dryTreePF, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                inst.name = "DesolateFogTree_" + i;
                inst.transform.localScale = Vector3.one * Random.Range(0.7f, 1.2f);
                if (ExteriorWorldRoot != null) inst.transform.SetParent(ExteriorWorldRoot.transform);
                FixMaterialsForURP(inst);
            }
        }

        if (cactiPF != null)
        {
            int cactiCount = Random.Range(8, 12);
            for (int i = 0; i < cactiCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
                if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;

                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    pos = hit.point;
                }

                GameObject inst = Instantiate(cactiPF, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                inst.name = "DesolateCacti_" + i;
                inst.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
                if (ExteriorWorldRoot != null) inst.transform.SetParent(ExteriorWorldRoot.transform);
                FixMaterialsForURP(inst);
            }
        }
#endif

        Material steelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        steelMat.color = new Color(0.2f, 0.18f, 0.16f);
        if (steelMat.HasProperty("_Metallic")) steelMat.SetFloat("_Metallic", 0.9f);

        Material warningMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        warningMat.EnableKeyword("_EMISSION");
        warningMat.color = new Color(0.3f, 0.2f, 0.0f);
        warningMat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 3f);

        // Mining drills / excavators
        for (int i = 0; i < 6; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f));
            if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;

            // Drill base
            GameObject drillBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drillBase.name = "MiningDrill";
            drillBase.transform.position = pos + Vector3.up;
            drillBase.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            drillBase.GetComponent<Renderer>().material = steelMat;
            if (ExteriorWorldRoot != null) drillBase.transform.SetParent(ExteriorWorldRoot.transform);

            // Warning beacon light on top
            GameObject beacon = new GameObject("WarningBeacon");
            beacon.transform.SetParent(drillBase.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            Light wl = beacon.AddComponent<Light>();
            wl.type = LightType.Point;
            wl.color = new Color(1f, 0.4f, 0f);
            wl.intensity = 2.5f;
            wl.range = 8f;
            beacon.AddComponent<MiningWarningBlink>();

            // Drill cone
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "DrillBit";
            cone.transform.SetParent(drillBase.transform, false);
            cone.transform.localPosition = new Vector3(0f, -1f, 0f);
            cone.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
            cone.GetComponent<Renderer>().material = warningMat;
        }

        // Ore veins (shiny dark chunks in the ground)
        for (int i = 0; i < 12; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-45f, 45f), 0.2f, Random.Range(-45f, 45f));
            GameObject ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ore.name = "OreChunk";
            ore.transform.position = pos;
            ore.transform.localScale = new Vector3(Random.Range(0.4f, 1.2f), Random.Range(0.3f, 0.8f), Random.Range(0.4f, 1.2f));
            ore.transform.rotation = Quaternion.Euler(Random.Range(0f, 30f), Random.Range(0f, 360f), Random.Range(0f, 30f));

            Material oreMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            oreMat.color = new Color(0.08f, 0.06f, 0.04f);
            oreMat.EnableKeyword("_EMISSION");
            oreMat.SetColor("_EmissionColor", new Color(0.5f, 0.2f, 0f) * 0.4f);
            if (oreMat.HasProperty("_Metallic")) oreMat.SetFloat("_Metallic", 1.0f);
            ore.GetComponent<Renderer>().material = oreMat;
            if (ExteriorWorldRoot != null) ore.transform.SetParent(ExteriorWorldRoot.transform);
        }
    }

    private void SpawnDefaultEnvironment()
    {
        // Vegetation Enhancement: Spawn variety of beautiful Free Trees in the Default Planet!
#if UNITY_EDITOR
        string[] treePaths = new string[] {
            "Assets/Darth_Artisan/Free_Trees/Prefabs/Fir_Tree.prefab",
            "Assets/Darth_Artisan/Free_Trees/Prefabs/Oak_Tree.prefab",
            "Assets/Darth_Artisan/Free_Trees/Prefabs/Palm_Tree.prefab",
            "Assets/Darth_Artisan/Free_Trees/Prefabs/Poplar_Tree.prefab"
        };
        
        System.Collections.Generic.List<GameObject> treePrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (var path in treePaths)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) treePrefabs.Add(prefab);
        }

        if (treePrefabs.Count > 0)
        {
            int treeCount = Random.Range(15, 25);
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-48f, 48f), 0f, Random.Range(-48f, 48f));
                // Keep far away from player and bus spawns to preserve clear play routes!
                if (Vector3.Distance(pos, playerSpawnPos) < 12f || Vector3.Distance(pos, busSpawnPos) < 12f) continue;

                // Raycast to align with ground height
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    pos = hit.point;
                }

                GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Count)];
                GameObject inst = Instantiate(selectedTree, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                inst.name = "VegetationTree_" + i;

                // Random scale variation for natural environment diversity
                inst.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
                if (ExteriorWorldRoot != null) inst.transform.SetParent(ExteriorWorldRoot.transform);

                // Fix pink material: replace Built-in shaders with URP equivalents
                FixMaterialsForURP(inst);
            }
        }
#endif
    }

    private void SpawnGravityEnvironment()
    {
        // Floating rocks/platforms
        Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rockMat.color = new Color(0.12f, 0.06f, 0.2f);
        if (rockMat.HasProperty("_Metallic")) rockMat.SetFloat("_Metallic", 0.3f);

        Material anomalyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        anomalyMat.EnableKeyword("_EMISSION");
        anomalyMat.color = new Color(0.2f, 0.0f, 0.3f);
        anomalyMat.SetColor("_EmissionColor", Color.magenta * 2.5f);

        for (int i = 0; i < 18; i++)
        {
            Vector3 basePos = new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f));
            float floatHeight = Random.Range(2f, 10f);

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "GravityRock";
            rock.transform.position = basePos + Vector3.up * floatHeight;
            rock.transform.localScale = new Vector3(Random.Range(1f, 3f), Random.Range(0.5f, 2f), Random.Range(1f, 3f));
            rock.transform.rotation = Quaternion.Euler(Random.Range(0f, 45f), Random.Range(0f, 360f), Random.Range(0f, 45f));
            rock.GetComponent<Renderer>().material = rockMat;
            rock.AddComponent<GravityFloatAnimation>();
            if (ExteriorWorldRoot != null) rock.transform.SetParent(ExteriorWorldRoot.transform);

            // Gravity rift pillar below floating rock
            if (Random.value > 0.5f)
            {
                GameObject rift = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rift.name = "GravityRift";
                rift.transform.position = basePos + Vector3.up * (floatHeight * 0.5f);
                rift.transform.localScale = new Vector3(0.1f, floatHeight * 0.5f, 0.1f);
                rift.GetComponent<Renderer>().material = anomalyMat;
                Destroy(rift.GetComponent<CapsuleCollider>());
                if (ExteriorWorldRoot != null) rift.transform.SetParent(ExteriorWorldRoot.transform);
            }
        }

        // Central gravity anomaly
        GameObject anomaly = new GameObject("GravityAnomaly");
        anomaly.transform.position = new Vector3(0f, 6f, 20f);
        Light al = anomaly.AddComponent<Light>();
        al.type = LightType.Point;
        al.color = new Color(0.7f, 0.2f, 1.0f);
        al.intensity = 8f;
        al.range = 30f;
        anomaly.AddComponent<GravityAnomalyPulse>();
        if (ExteriorWorldRoot != null) anomaly.transform.SetParent(ExteriorWorldRoot.transform);
    }

    // Circular ring of invisible wall segments that hug the mountain perimeter.
    private void AddInvisibleBoundaryWalls(float radius)
    {
        int   segments  = 36;
        float height    = 40f;
        float thickness = 8f;
        // Arc length per segment + overlap so there are zero gaps
        float segWidth  = (2f * Mathf.PI * radius / segments) + thickness;

        for (int i = 0; i < segments; i++)
        {
            float angle   = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 center  = outward * radius;
            center.y        = height * 0.5f;

            GameObject wall = new GameObject("BoundaryWall_" + i);
            wall.transform.position = center;
            // Z축이 바깥쪽(outward)을 향하게 → 벽 두께 방향이 반경 방향
            wall.transform.rotation = Quaternion.LookRotation(outward);

            BoxCollider bc = wall.AddComponent<BoxCollider>();
            bc.size = new Vector3(segWidth, height, thickness);
            if (ExteriorWorldRoot != null) wall.transform.SetParent(ExteriorWorldRoot.transform);
        }
    }

    // Replaces any Built-in RP shader with the URP/Lit equivalent so prefabs don't show pink.
    // Preserves the original main texture and base color where available.
    private void FixMaterialsForURP(GameObject root)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpLit == null) return;

        foreach (Renderer rend in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = rend.materials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];
                if (src == null) continue;
                string shaderName = src.shader != null ? src.shader.name : "";
                // Only replace non-URP shaders (Built-in, Legacy, etc.)
                if (!shaderName.StartsWith("Universal Render Pipeline") && !shaderName.StartsWith("Shader Graphs"))
                {
                    if (rend is ParticleSystemRenderer && urpParticlesUnlit != null)
                    {
                        Material replacement = new Material(urpParticlesUnlit);
                        
                        // Check if the source shader or material name indicates additive or transparent blending
                        bool isAdditive = shaderName.ToLower().Contains("add") || src.name.ToLower().Contains("add") || src.name.ToLower().Contains("glow");
                        bool isTransparent = isAdditive || shaderName.ToLower().Contains("blend") || shaderName.ToLower().Contains("transparent") || shaderName.ToLower().Contains("alpha") || src.name.ToLower().Contains("flake") || src.name.ToLower().Contains("smoke") || src.name.ToLower().Contains("cloud");

                        if (isTransparent)
                        {
                            replacement.SetFloat("_Surface", 1.0f); // Transparent
                            if (isAdditive)
                            {
                                replacement.SetFloat("_Blend", 1.0f); // Additive
                                replacement.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                replacement.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            }
                            else
                            {
                                replacement.SetFloat("_Blend", 0.0f); // Alpha Blended
                                replacement.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                replacement.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            replacement.SetInt("_ZWrite", 0);
                            replacement.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        }
                        else
                        {
                            replacement.SetFloat("_Surface", 0.0f); // Opaque
                            replacement.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                        }

                        // Carry over main texture and base color
                        if (src.HasProperty("_Color"))         replacement.color = src.color;
                        if (src.HasProperty("_BaseColor"))     replacement.SetColor("_BaseColor", src.GetColor("_BaseColor"));
                        
                        // URP Particles/Unlit uses _BaseMap as main texture
                        if (src.HasProperty("_MainTex") && src.mainTexture != null)
                        {
                            replacement.SetTexture("_BaseMap", src.mainTexture);
                        }
                        else if (src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap") != null)
                        {
                            replacement.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                        }

                        mats[i] = replacement;
                        changed = true;
                    }
                    else
                    {
                        Material replacement = new Material(urpLit);
                        // Carry over base color and main texture if they exist
                        if (src.HasProperty("_Color"))         replacement.color = src.color;
                        if (src.HasProperty("_BaseColor"))     replacement.SetColor("_BaseColor", src.GetColor("_BaseColor"));
                        if (src.HasProperty("_MainTex") && src.mainTexture != null)
                            replacement.mainTexture = src.mainTexture;
                        mats[i] = replacement;
                        changed = true;
                    }
                }
            }
            if (changed) rend.materials = mats;
        }
    }

    private void SpawnAtmosphericParticles()
    {
        var theme = PlanetManager.Instance != null ? PlanetManager.Instance.activeTheme : HorrorVisualPreset.PlanetTheme.Default;
        GameObject psGO = new GameObject("PlanetParticles");
        psGO.transform.position = Vector3.up * 12f;
        if (ExteriorWorldRoot != null) psGO.transform.SetParent(ExteriorWorldRoot.transform);
        ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startSize = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1200;

        var emission = ps.emission;
        emission.rateOverTime = 180f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(80f, 1f, 80f);

        // Wind drift & velocity over lifetime
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        // Custom particle material with soft dot texture
        Texture2D softDotTex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                alpha = Mathf.Pow(alpha, 2f); // Smooth falloff
                softDotTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        softDotTex.Apply();

        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Mobile/Particles/Additive"));
        particleMat.mainTexture = softDotTex;
        
        var psRend = psGO.GetComponent<ParticleSystemRenderer>();
        if (psRend != null)
        {
            psRend.material = particleMat;
        }

        switch (theme)
        {
            case HorrorVisualPreset.PlanetTheme.Ice:
                main.startColor = new Color(0.95f, 0.98f, 1.0f, 0.85f); // Beautiful bright snow
                main.gravityModifier = 0.18f;
                main.startSpeed = 2.5f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);

                // Wind blow drift
                vel.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
                vel.y = new ParticleSystem.MinMaxCurve(-2f, -1f);
                vel.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

                // Use the beautiful SnowFlakes_01 material for falling snow flakes!
#if UNITY_EDITOR
                Material sMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Magic VFX/Magic VFX - Ice (FREE)/Models/Materials/SnowFlakes_01.mat");
                if (sMat != null && psRend != null)
                {
                    // Create an upgraded transparent URP-compatible copy of SnowFlakes_01
                    Material upgradedSnowMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit"));
                    upgradedSnowMat.SetFloat("_Surface", 1.0f); // Transparent
                    upgradedSnowMat.SetFloat("_Blend", 0.0f); // Alpha Blended
                    upgradedSnowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    upgradedSnowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    upgradedSnowMat.SetInt("_ZWrite", 0);
                    upgradedSnowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    
                    if (sMat.HasProperty("_Color")) upgradedSnowMat.color = sMat.color;
                    if (sMat.HasProperty("_BaseColor")) upgradedSnowMat.SetColor("_BaseColor", sMat.GetColor("_BaseColor"));
                    if (sMat.mainTexture != null) upgradedSnowMat.SetTexture("_BaseMap", sMat.mainTexture);
                    
                    psRend.material = upgradedSnowMat;
                }
#endif
                break;
            case HorrorVisualPreset.PlanetTheme.Toxic:
                main.startColor = new Color(0.3f, 0.85f, 0.3f, 0.45f); // Rising gas bubbles
                main.gravityModifier = -0.04f; // rises up
                main.startSpeed = 1.0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
                
                vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                break;
            case HorrorVisualPreset.PlanetTheme.Mining:
                main.startColor = new Color(0.48f, 0.34f, 0.22f, 0.65f); // Heavy suspended dust
                main.gravityModifier = 0.02f;
                main.startSpeed = 0.5f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);

                vel.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.5f, 0f);
                vel.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
                break;
            case HorrorVisualPreset.PlanetTheme.Gravity:
                main.startColor = new Color(0.72f, 0.18f, 0.94f, 0.55f); // anti-grav glowing purple specs
                main.gravityModifier = -0.08f; // floats upwards
                main.startSpeed = 1.4f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);

                vel.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
                vel.y = new ParticleSystem.MinMaxCurve(0.2f, 1.8f);
                vel.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
                break;
            default:
                main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.15f); // light dust specs
                main.gravityModifier = 0.01f;
                main.startSpeed = 0.3f;
                break;
        }
    }

    private void SpawnPlayer()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;

        if (playerPrefab != null && SafeSpawnManager.Instance != null)
        {
            SafeSpawnManager.Instance.SpawnSafe(playerPrefab, playerSpawnPos, Quaternion.identity);
            return;
        }

        // Procedural fallback player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag  = "Player";
        player.transform.position = playerSpawnPos;

        // Remove default collider and add CharacterController
        Object.Destroy(player.GetComponent<CapsuleCollider>());
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0f, 0f);

        player.AddComponent<PlayerMovement>();
        player.AddComponent<Flashlight>();

        // Apply dark material so capsule isn't bright white
        MeshRenderer mr = player.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = new Color(0.15f, 0.15f, 0.2f);
            mr.material = m;
        }

        Debug.Log("[Bootstrap] Procedural player created.");
    }

    private void SpawnBus()
    {
        if (Object.FindAnyObjectByType<BusController>() != null) return;

        if (busPrefab != null && SafeSpawnManager.Instance != null)
        {
            SafeSpawnManager.Instance.SpawnSafe(busPrefab, busSpawnPos, Quaternion.identity);
            return;
        }

        // Procedural fallback bus
        GameObject bus = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bus.name = "SpaceBus";
        bus.tag  = "Bus";
        bus.transform.position = busSpawnPos;
        bus.transform.localScale = new Vector3(4f, 2.5f, 8f);

        BusController bc = bus.AddComponent<BusController>();
        bus.AddComponent<BusPowerSystem>();
        bc.powerSystem = bus.GetComponent<BusPowerSystem>();

        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = new Color(0.2f, 0.25f, 0.35f);
        bus.GetComponent<MeshRenderer>().material = m;

        // Interior zone trigger
        GameObject interior = new GameObject("BusInterior");
        interior.transform.SetParent(bus.transform, false);
        BoxCollider ic = interior.AddComponent<BoxCollider>();
        ic.isTrigger = true;
        ic.size = new Vector3(0.8f, 0.8f, 0.9f);
        BusInteriorZone biz = interior.AddComponent<BusInteriorZone>();
        biz.powerSystem = bc.powerSystem;

        Debug.Log("[Bootstrap] Procedural bus created.");
    }
}
