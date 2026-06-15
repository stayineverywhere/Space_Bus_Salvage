using UnityEngine;

public class MonsterVisuals : MonoBehaviour
{
    public Renderer monsterRenderer;
    [ColorUsage(true, true)]
    public Color emissiveColor = Color.red;
    public float bobSpeed = 2f;
    public float bobAmount = 0.1f;

    private Material instancedMaterial;
    private Transform modelChild;

    void Start()
    {
        // 1. Identify which monster we are styling based on attached components
        bool isWatcher = GetComponent<WatcherAI>() != null;
        bool isCleaner = GetComponent<CleanerAI>() != null;
        bool isLurker  = GetComponent<LurkerAI>() != null;

        // 2. Setup Base Renderer and Materials
        if (monsterRenderer == null) monsterRenderer = GetComponentInChildren<Renderer>();
        if (monsterRenderer != null)
        {
            instancedMaterial = monsterRenderer.material;
            ConfigureMonsterMaterial(isWatcher, isCleaner, isLurker);
        }

        // 3. Customize Silhouette, Mesh Scaling, and Spawn VFX
        ApplyMonsterCustomizations(isWatcher, isCleaner, isLurker);

        // Cache child transform safely
        if (transform.childCount > 0) modelChild = transform.GetChild(0);
    }

    private void ConfigureMonsterMaterial(bool isWatcher, bool isCleaner, bool isLurker)
    {
        if (instancedMaterial == null) return;

        instancedMaterial.EnableKeyword("_EMISSION");

        if (isWatcher)
        {
            // Watcher: Crimson textured skin with red glowing elements
            instancedMaterial.color = new Color(0.12f, 0.02f, 0.02f, 1f);
            instancedMaterial.SetColor("_EmissionColor", Color.red * 1.8f);
        }
        else if (isCleaner)
        {
            // Cleaner: Metallic chrome drone body with amber indicator
            instancedMaterial.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            if (instancedMaterial.HasProperty("_Metallic")) instancedMaterial.SetFloat("_Metallic", 0.9f);
            if (instancedMaterial.HasProperty("_Glossiness")) instancedMaterial.SetFloat("_Glossiness", 0.85f);
            instancedMaterial.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 2.0f);
        }
        else if (isLurker)
        {
            // Lurker: Dark shadow silhouette
            instancedMaterial.color = new Color(0.01f, 0.01f, 0.01f, 0.95f);
            instancedMaterial.SetColor("_EmissionColor", new Color(0.2f, 0f, 0.5f) * 1.2f);
        }
    }

    private void ApplyMonsterCustomizations(bool isWatcher, bool isCleaner, bool isLurker)
    {
        if (isWatcher)
        {
            // Tall humanoid scale
            transform.localScale = new Vector3(1.1f, 2.3f, 1.1f);
            CreateGlowingEyes();
        }

        if (isCleaner)
        {
            // Small scavenger drone scale
            transform.localScale = new Vector3(1.0f, 0.8f, 1.0f);
        }

        if (isLurker)
        {
            // Shadowy humanoid scale
            transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            CreateShadowSmokeVFX();
        }

        CreateSpawnMist();
    }

    private void CreateGlowingEyes()
    {
        Transform head = transform.Find("VisualModel/Monster_Head") ?? transform.Find("Body");
        if (head == null) head = this.transform;

        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "Watcher_Left_Eye";
        leftEye.transform.SetParent(head, false);
        leftEye.transform.localPosition = new Vector3(-0.15f, 0.4f, 0.35f);
        leftEye.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        Destroy(leftEye.GetComponent<SphereCollider>());

        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "Watcher_Right_Eye";
        rightEye.transform.SetParent(head, false);
        rightEye.transform.localPosition = new Vector3(0.15f, 0.4f, 0.35f);
        rightEye.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        Destroy(rightEye.GetComponent<SphereCollider>());

        Material eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.color = Color.red;
        eyeMat.SetColor("_EmissionColor", Color.red * 4.0f);

        leftEyeMat = new Material(eyeMat);
        rightEyeMat = new Material(eyeMat);
        leftEye.GetComponent<Renderer>().material = leftEyeMat;
        rightEye.GetComponent<Renderer>().material = rightEyeMat;
    }

    private void CreateShadowSmokeVFX()
    {
        GameObject smokeGO = new GameObject("ShadowSmokeVFX");
        smokeGO.transform.SetParent(this.transform, false);
        smokeGO.transform.localPosition = Vector3.up * 0.5f;

        ParticleSystem ps = smokeGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.08f, 0.01f, 0.18f, 0.6f);
        main.startSize = 0.8f;
        main.startLifetime = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var rend = smokeGO.GetComponent<ParticleSystemRenderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply") ?? Shader.Find("Standard"));
        }
    }

    private void CreateSpawnMist()
    {
        GameObject mistGO = new GameObject("SpawnMistVFX");
        mistGO.transform.SetParent(this.transform, false);
        mistGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = mistGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        main.startSize = 1.5f;
        main.startLifetime = 0.8f;
        main.duration = 0.5f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        Destroy(mistGO, 1.2f);
    }

    // Eye materials for Watcher so we can tint them independently
    private Material leftEyeMat;
    private Material rightEyeMat;
    private float eyeBlinkTimer;

    void Update()
    {
        if (modelChild != null)
        {
            float newY = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            modelChild.localPosition = new Vector3(0f, newY, 0f);
        }

        WatcherAI watcher = GetComponent<WatcherAI>();
        if (watcher != null && instancedMaterial != null)
        {
            UpdateWatcherVisuals(watcher);
        }

        LurkerAI lurker = GetComponent<LurkerAI>();
        if (lurker != null && instancedMaterial != null)
        {
            UpdateLurkerVisuals(lurker);
        }
    }

    private void UpdateWatcherVisuals(WatcherAI watcher)
    {
        bool isChasing = watcher.currentState == MonsterAI.MonsterState.Chase ||
                         watcher.currentState == MonsterAI.MonsterState.Attack;

        // Body glow: dim red at rest → bright red alert when chasing
        float bodyGlow = isChasing ? 5.0f : 1.8f;
        Color bodyEmission = isChasing ? Color.red * bodyGlow : (Color.red * 0.6f + Color.white * 0.2f) * bodyGlow;
        instancedMaterial.SetColor("_EmissionColor",
            Color.Lerp(instancedMaterial.GetColor("_EmissionColor"), bodyEmission, Time.deltaTime * 4f));

        // Flashlight reaction — amplify when light aimed at watcher
        bool flashlightAimed = IsFlashlightAimedAtMe();
        if (flashlightAimed && !isChasing)
        {
            instancedMaterial.SetColor("_EmissionColor", Color.red * 8f);
        }

        // Eye blink: rapidly blink eyes when chasing, slow blink when idle
        eyeBlinkTimer -= Time.deltaTime;
        float blinkInterval = isChasing ? 0.12f : 1.8f;
        if (eyeBlinkTimer <= 0f) eyeBlinkTimer = blinkInterval;
        bool eyesOpen = eyeBlinkTimer > blinkInterval * 0.15f;

        Color eyeColor = isChasing ? Color.red * 8f : Color.red * 4f;
        if (leftEyeMat != null)  leftEyeMat.SetColor("_EmissionColor",  eyesOpen ? eyeColor : Color.black);
        if (rightEyeMat != null) rightEyeMat.SetColor("_EmissionColor", eyesOpen ? eyeColor : Color.black);
    }

    private void UpdateLurkerVisuals(LurkerAI lurker)
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;

        float dist = Vector3.Distance(transform.position, pm.transform.position);
        bool isChasing = lurker.currentState == MonsterAI.MonsterState.Chase ||
                         lurker.currentState == MonsterAI.MonsterState.Attack;

        // Alpha: barely visible from far → fully visible when close / chasing
        float targetAlpha;
        if (isChasing)
            targetAlpha = Mathf.Lerp(0.15f, 1.0f, 1f - Mathf.Clamp01((dist - 3f) / 12f));
        else
            targetAlpha = Mathf.Lerp(0.05f, 0.35f, 1f - Mathf.Clamp01((dist - 5f) / 20f));

        Color col = instancedMaterial.color;
        col.a = Mathf.Lerp(col.a, targetAlpha, Time.deltaTime * 3f);
        instancedMaterial.color = col;

        // Enable transparent rendering when lurker should fade
        if (targetAlpha < 0.9f)
        {
            instancedMaterial.SetFloat("_Surface", 1f); // URP transparent mode
            instancedMaterial.renderQueue = 3000;
        }
    }

    private bool IsFlashlightAimedAtMe()
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return false;
        
        Flashlight fl = pm.GetComponentInChildren<Flashlight>();
        if (fl == null || !fl.gameObject.activeInHierarchy || !fl.GetComponent<Light>().enabled) return false;

        Vector3 dirToMonster = (transform.position - pm.transform.position).normalized;
        float dot = Vector3.Dot(pm.transform.forward, dirToMonster);
        
        if (dot > 0.94f && Vector3.Distance(transform.position, pm.transform.position) < 25f)
        {
            if (Physics.Raycast(pm.transform.position, dirToMonster, out RaycastHit hit, 25f))
            {
                if (hit.transform.root == this.transform.root) return true;
            }
        }
        return false;
    }
}
