using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HorrorVisualPreset : MonoBehaviour
{
    public static HorrorVisualPreset Instance { get; private set; }

    [Header("Environment Settings")]
    public Color ambientColor = new Color(0.1f, 0.1f, 0.15f); // Very dark blue/gray
    public Color fogColor = new Color(0.05f, 0.05f, 0.08f);
    public float fogDensity = 0.02f;

    [Header("Fallbacks")]
    public Material defaultMaterial;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlanetManager.Instance != null)
        {
            currentTheme = PlanetManager.Instance.activeTheme;
        }
        ApplyHorrorVisuals();
    }

    public enum PlanetTheme { Default, Ice, Toxic, Mining, Gravity }
    public PlanetTheme currentTheme = PlanetTheme.Default;

    public void ApplyHorrorVisuals()
    {
        Debug.Log($"[HorrorVisuals] Applying atmospheric preset for {currentTheme}...");

        switch (currentTheme)
        {
            case PlanetTheme.Ice:
                ambientColor = new Color(0.12f, 0.18f, 0.25f);
                fogColor = new Color(0.4f, 0.55f, 0.7f);
                fogDensity = 0.04f;
                break;
            case PlanetTheme.Toxic:
                ambientColor = new Color(0.08f, 0.18f, 0.08f);
                fogColor = new Color(0.2f, 0.4f, 0.15f);
                fogDensity = 0.07f;
                break;
            case PlanetTheme.Mining:
                ambientColor = new Color(0.05f, 0.03f, 0.02f);
                fogColor = new Color(0.12f, 0.09f, 0.06f);
                fogDensity = 0.11f; // Clustrophobic dark fog
                break;
            case PlanetTheme.Gravity:
                ambientColor = new Color(0.15f, 0.05f, 0.25f);
                fogColor = new Color(0.12f, 0.0f, 0.25f);
                fogDensity = 0.03f;
                break;
            default:
                ambientColor = new Color(0.1f, 0.1f, 0.15f);
                fogColor = new Color(0.05f, 0.05f, 0.08f);
                fogDensity = 0.02f;
                break;
        }

        // Apply RenderSettings Fog
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        // Dynamically adjust all directional lights in the scene
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (l.type == LightType.Directional)
            {
                if (currentTheme == PlanetTheme.Mining)
                {
                    l.intensity = 0.12f; // pitch-dark
                    l.color = new Color(0.4f, 0.35f, 0.3f);
                }
                else if (currentTheme == PlanetTheme.Ice)
                {
                    l.intensity = 0.65f;
                    l.color = new Color(0.85f, 0.95f, 1.0f);
                }
                else if (currentTheme == PlanetTheme.Toxic)
                {
                    l.intensity = 0.45f;
                    l.color = new Color(0.65f, 0.85f, 0.65f);
                }
                else if (currentTheme == PlanetTheme.Gravity)
                {
                    l.intensity = 0.35f;
                    l.color = new Color(0.8f, 0.6f, 1.0f);
                }
                else
                {
                    l.intensity = 0.8f;
                    l.color = new Color(0.7f, 0.8f, 1.0f);
                }
            }
        }

        // Apply ground ice material reflections if in ice planet
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        foreach (var rend in renderers)
        {
            if (rend.gameObject.name.ToLower().Contains("ground") || rend.gameObject.name.ToLower().Contains("floor"))
            {
                Material m = rend.material;
                if (m != null)
                {
                    if (currentTheme == PlanetTheme.Ice)
                    {
                        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.1f);
                        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.9f); // highly reflective ice
                    }
                    else if (currentTheme == PlanetTheme.Toxic)
                    {
                        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.05f);
                        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.5f); // wet sludge
                    }
                }
            }
        }

        EnsureLightSource();
        SetupPostProcessing();
        ValidateAllMaterials();
        ValidateMonsterMeshes();
    }

    private void EnsureLightSource()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        if (lights.Length == 0)
        {
            Debug.Log("[HorrorVisuals] No lights found! Creating emergency dim light.");
            GameObject lightGO = new GameObject("EmergencyDimLight");
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.intensity = 0.5f;
            l.range = 10f;
            l.color = Color.white;
            lightGO.transform.position = Vector3.up * 5f;
        }
    }

    private void SetupPostProcessing()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Ensure Volume exists
        Volume volume = Object.FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            GameObject volumeGO = new GameObject("HorrorPostProcessVolume");
            volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        // Add Volume Profile and Effects if missing
        if (volume.profile == null)
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
        }

        // Configure Vignette (higher claustrophobic dark feel in Mining Planet)
        Vignette vignette;
        if (!volume.profile.TryGet(out vignette))
        {
            vignette = volume.profile.Add<Vignette>(true);
        }
        if (vignette != null)
        {
            float targetIntensity = (currentTheme == PlanetTheme.Mining) ? 0.65f : 0.45f;
            vignette.intensity.Override(targetIntensity);
            vignette.smoothness.Override(0.4f);
            vignette.active = true;
        }

        // Configure Film Grain
        FilmGrain grain;
        if (!volume.profile.TryGet(out grain))
        {
            grain = volume.profile.Add<FilmGrain>(true);
        }
        if (grain != null)
        {
            grain.type.Override(FilmGrainLookup.Medium2);
            grain.intensity.Override(0.5f);
            grain.active = true;
        }

        // Configure Chromatic Aberration for otherworldly Gravity Planet
        ChromaticAberration aberration;
        if (!volume.profile.TryGet(out aberration))
        {
            aberration = volume.profile.Add<ChromaticAberration>(true);
        }
        if (aberration != null)
        {
            float targetAmount = (currentTheme == PlanetTheme.Gravity) ? 0.8f : 0.05f;
            aberration.intensity.Override(targetAmount);
            aberration.active = (targetAmount > 0f);
        }

        // Configure Color Adjustments for ambient tinting
        ColorAdjustments colorAdjust;
        if (!volume.profile.TryGet(out colorAdjust))
        {
            colorAdjust = volume.profile.Add<ColorAdjustments>(true);
        }
        if (colorAdjust != null)
        {
            Color targetColor = Color.white;
            if (currentTheme == PlanetTheme.Ice) targetColor = new Color(0.85f, 0.95f, 1.0f); // cold blue
            else if (currentTheme == PlanetTheme.Toxic) targetColor = new Color(0.82f, 1.0f, 0.82f); // toxic green
            else if (currentTheme == PlanetTheme.Mining) targetColor = new Color(0.9f, 0.82f, 0.75f); // dark amber/dirt

            colorAdjust.colorFilter.Override(targetColor);
            colorAdjust.active = true;
        }
    }

    private void ValidateAllMaterials()
    {
        if (defaultMaterial == null)
        {
            defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        foreach (var rend in renderers)
        {
            Material[] mats = rend.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                {
                    Debug.LogWarning($"[HorrorVisuals] Missing material found on {rend.name}! Applying default.");
                    mats[i] = defaultMaterial;
                    changed = true;
                }
            }
            if (changed) rend.sharedMaterials = mats;
        }
    }

    private void ValidateMonsterMeshes()
    {
        MonsterVisuals[] monsters = Object.FindObjectsByType<MonsterVisuals>(FindObjectsInactive.Exclude);
        foreach (var monster in monsters)
        {
            MeshFilter filter = monster.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh == null)
            {
                Debug.LogWarning($"[HorrorVisuals] Missing mesh on monster {monster.name}! Falling back to cube.");
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                filter.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(cube);
            }
        }
    }
}
