using UnityEngine;
using System.Collections.Generic;

public class SnowfallEffect : MonoBehaviour
{
    // Optional override — if left empty the material is built in code
    [SerializeField] Material snowMaterialOverride;

    private ParticleSystem _ps;
    private ParticleSystem _splatPs;
    private Transform      _followTarget;
    private Material       _builtMaterial;
    private readonly List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _followTarget = Camera.main?.transform;
        if (_followTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) _followTarget = p.transform;
        }

        _builtMaterial = BuildSnowMaterial();

        BuildSnowfall();
        BuildSplatSystem();
        _ps.Play();
    }

    void Update()
    {
        if (_followTarget == null || _ps == null) return;
        float x = _followTarget.position.x;
        float z = _followTarget.position.z;
        float y = _followTarget.position.y + 15f;
        _ps.transform.position = new Vector3(x, y, z);
        if (_splatPs != null)
            _splatPs.transform.position = new Vector3(x, 0f, z);
    }

    void OnDestroy()
    {
        if (_ps      != null) Destroy(_ps.gameObject);
        if (_splatPs != null) Destroy(_splatPs.gameObject);
        if (_builtMaterial != null) Destroy(_builtMaterial);
    }

    // Called by SnowCollisionRelay on the PS GameObject
    public void OnParticleCollision(GameObject other)
    {
        if (_ps == null || _splatPs == null) return;
        int count = _ps.GetCollisionEvents(other, _collisionEvents);
        for (int i = 0; i < count; i++)
        {
            _splatPs.transform.position = _collisionEvents[i].intersection;
            _splatPs.Emit(1);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void BuildSnowfall()
    {
        GameObject psGO = new GameObject("SnowParticles");
        DontDestroyOnLoad(psGO);
        _ps = psGO.AddComponent<ParticleSystem>();

        // Main
        var main = _ps.main;
        main.loop            = true;
        main.prewarm         = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(0.95f, 0.97f, 1f, 0.65f),
                                   new Color(1f, 1f, 1f, 0.95f));
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles    = 600;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.05f;

        // Emission
        var emission = _ps.emission;
        emission.rateOverTime = 80f;

        // Shape — wide thin box above player
        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(40f, 0.1f, 40f);

        // Size over Lifetime
        var sol = _ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f,   1f),
            new Keyframe(0.8f, 0.95f),
            new Keyframe(1f,   0f)));

        // Noise — gentle drift
        var noise = _ps.noise;
        noise.enabled     = true;
        noise.strength    = new ParticleSystem.MinMaxCurve(0.28f);
        noise.frequency   = 0.35f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.07f);
        noise.quality     = ParticleSystemNoiseQuality.Low;
        noise.damping     = true;

        // Velocity over Lifetime — wind sway
        var vol = _ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x       = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vol.z       = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vol.space   = ParticleSystemSimulationSpace.World;

        // Collision
        var col = _ps.collision;
        col.enabled               = true;
        col.type                  = ParticleSystemCollisionType.World;
        col.mode                  = ParticleSystemCollisionMode.Collision3D;
        col.sendCollisionMessages = true;
        col.lifetimeLoss          = 1f;
        col.bounceMultiplier      = 0f;
        col.radiusScale           = 0.4f;

        // Renderer
        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode      = ParticleSystemRenderMode.Billboard;
        rend.maxParticleSize = 0.5f;
        rend.sortingOrder    = 1;
        rend.material        = _builtMaterial;

        psGO.AddComponent<SnowCollisionRelay>().owner = this;
    }

    void BuildSplatSystem()
    {
        GameObject go = new GameObject("SnowSplat");
        DontDestroyOnLoad(go);
        _splatPs = go.AddComponent<ParticleSystem>();

        var main = _splatPs.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor      = new Color(1f, 1f, 1f, 0.4f);
        main.maxParticles    = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = _splatPs.emission;
        emission.enabled = false;

        var shape = _splatPs.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;

        var sol = _splatPs.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var rend = _splatPs.GetComponent<ParticleSystemRenderer>();
        rend.renderMode  = ParticleSystemRenderMode.Billboard;
        rend.material    = _builtMaterial;
        rend.sortingOrder = 1;
    }

    // ── Material ─────────────────────────────────────────────────────────────
    Material BuildSnowMaterial()
    {
        // User-assigned override wins
        if (snowMaterialOverride != null) return snowMaterialOverride;

        Shader sh = Shader.Find("Custom/SnowParticle");
        if (sh == null)
        {
            Debug.LogWarning("[SnowfallEffect] Custom/SnowParticle shader not found. Using URP fallback.");
            sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }
        if (sh == null)
        {
            Debug.LogWarning("[SnowfallEffect] URP particle shader not found.");
            sh = Shader.Find("Standard");
        }

        Material mat = new Material(sh);

        // Assign snowflake crystal texture
        Texture2D tex = BuildSnowflakeCrystalTexture(128);
        if (mat.HasProperty("_MainTex"))  mat.SetTexture("_MainTex",  tex);
        if (mat.HasProperty("_BaseMap"))  mat.SetTexture("_BaseMap",  tex);

        // Tint — slightly cool white
        Color tint = new Color(0.92f, 0.96f, 1f, 0.88f);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", tint);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);

        // URP transparency keywords (no-op on built-in pipeline)
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f);
        if (mat.HasProperty("_ZWrite"))  mat.SetFloat("_ZWrite",  0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        return mat;
    }

    // ── Procedural 6-armed snowflake crystal ─────────────────────────────────
    static Texture2D BuildSnowflakeCrystalTexture(int size)
    {
        Color[] px  = new Color[size * size];
        Vector2 ctr = new Vector2(size * 0.5f, size * 0.5f);
        float arm   = size * 0.44f;
        float br    = arm  * 0.32f;
        float lw    = size * 0.030f;
        float bw    = size * 0.018f;

        for (int i = 0; i < 6; i++)
        {
            float   a   = i * 60f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            DrawSeg(px, size, ctr, ctr + dir * arm, lw);

            float[] tt = { 0.33f, 0.62f };
            foreach (float t in tt)
            {
                Vector2 root = ctr + dir * (arm * t);
                float a1 = a + 60f * Mathf.Deg2Rad;
                float a2 = a - 60f * Mathf.Deg2Rad;
                DrawSeg(px, size, root, root + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * br, bw);
                DrawSeg(px, size, root, root + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * br, bw);
            }
        }
        DrawCircle(px, size, ctr, size * 0.055f);

        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.SetPixels(px);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    static void DrawSeg(Color[] px, int size, Vector2 a, Vector2 b, float hw)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - hw - 1));
        int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + hw + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - hw - 1));
        int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + hw + 1));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float alpha = Mathf.Clamp01(1f - (SegDist(new Vector2(x, y), a, b) - hw + 1f));
                if (alpha <= 0f) continue;
                int idx = y * size + x;
                px[idx] = new Color(1f, 1f, 1f, Mathf.Max(px[idx].a, alpha));
            }
    }

    static void DrawCircle(Color[] px, int size, Vector2 c, float r)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(c.x - r - 1));
        int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(c.x + r + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(c.y - r - 1));
        int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(c.y + r + 1));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float alpha = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(x, y), c) - r + 1f));
                if (alpha <= 0f) continue;
                int idx = y * size + x;
                px[idx] = new Color(1f, 1f, 1f, Mathf.Max(px[idx].a, alpha));
            }
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        if (ab.sqrMagnitude < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}

// Proxy: OnParticleCollision is only fired on the PS's own GameObject
public class SnowCollisionRelay : MonoBehaviour
{
    public SnowfallEffect owner;
    void OnParticleCollision(GameObject other) => owner?.OnParticleCollision(other);
}
