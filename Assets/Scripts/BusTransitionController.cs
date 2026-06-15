using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BusTransitionController : MonoBehaviour
{
    public static BusTransitionController Instance { get; private set; }

    // Centralized state machine for enter/exit transitions (Section 4)
    public enum BusCabinState
    {
        OutsideBus,
        TransitionToBus,
        InsideBus,
        TransitionToOutside
    }
    public BusCabinState CurrentState { get; private set; } = BusCabinState.OutsideBus;

    [Header("Transition Settings")]
    public float fadeDuration = 0.25f;
    private Image fadeOverlay;

    [Header("Safe Zone Modifiers")]
    public float staminaRegenMultiplier = 2.0f;
    public float oxygenRegenRate = 20.0f;

    [Header("Engine Hum Audio")]
    private AudioSource engineHumSource;

    // Direct state lock variable to expose input lock (Section 3)
    public bool IsInputLocked { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        SetupFadeOverlay();
        SetupEngineHum();
    }

    private void SetupFadeOverlay()
    {
        // Dynamically find or create fade overlay on the HUD Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject fadeGO = new GameObject("TransitionFadeOverlay");
            fadeGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = fadeGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            fadeOverlay = fadeGO.AddComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.raycastTarget = false;
        }
    }

    private void SetupEngineHum()
    {
        engineHumSource = gameObject.AddComponent<AudioSource>();
        engineHumSource.loop = true;
        engineHumSource.playOnAwake = true;
        engineHumSource.spatialBlend = 0.0f; // 2D ambient sound inside bus
        engineHumSource.volume = 0f; // Start silent, turn up when inside

        // Procedural generator hum noise
        int samplerate = 44100;
        float duration = 1.0f;
        int sampleCount = Mathf.RoundToInt(samplerate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / samplerate;
            // Combined frequencies for sci-fi turbine drone (60Hz core, 120Hz sub, 240Hz overtone)
            float wave = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f +
                         Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.3f +
                         Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f;
            samples[i] = wave * 0.2f;
        }
        AudioClip clip = AudioClip.Create("EngineGeneratorHum", sampleCount, 1, samplerate, false);
        clip.SetData(samples, 0);
        engineHumSource.clip = clip;
        engineHumSource.Play();
    }

    public void EnterBus(PlayerMovement player, Vector3 spawnOffset)
    {
        // 1. Strict state check to prevent double inputs and spam (Section 4)
        if (CurrentState != BusCabinState.OutsideBus) return;
        
        CurrentState = BusCabinState.TransitionToBus;
        StartCoroutine(TransitionRoutine(player, true, spawnOffset));
    }

    public void ExitBus(PlayerMovement player, Vector3 spawnOffset)
    {
        // 1. Strict state check to prevent exit buffering / lag (Section 3 & 4)
        if (CurrentState != BusCabinState.InsideBus) return;

        CurrentState = BusCabinState.TransitionToOutside;
        StartCoroutine(TransitionRoutine(player, false, spawnOffset));
    }

    private IEnumerator TransitionRoutine(PlayerMovement player, bool entering, Vector3 spawnOffset)
    {
        // 2. LOCK INPUT immediately (Section 3)
        IsInputLocked = true;

        // 3. Play Transition Screen Fade Out
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.color = Color.black;

        // 4. THEN EXECUTE IN ORDER (Section 3 - preventing simultaneous changes)
        BusController bus = Object.FindAnyObjectByType<BusController>();
        if (bus != null && player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (entering)
            {
                // Teleport inside (spawnOffset is local coordinate inside cabin)
                player.transform.position = bus.transform.TransformPoint(spawnOffset);
                player.transform.rotation = bus.transform.rotation; // Align player orientation with bus
                player.isInsideBus = true;

                // Hide the exterior level renderers completely (Lethal Company style separation)
                if (SceneBootstrap.ExteriorWorldRoot != null)
                {
                    SceneBootstrap.ExteriorWorldRoot.SetActive(false);
                }

                // Adjust lighting parameters for sealed bus interior
                RenderSettings.ambientLight = new Color(0.02f, 0.05f, 0.15f); // ultra-dark sci-fi blue ambient
                RenderSettings.fog = false; // Turn fog off inside bus

                // Volume of engine generator hum increases inside
                if (engineHumSource != null) engineHumSource.volume = 0.45f;
            }
            else
            {
                // Teleport outside (spawnOffset is world coordinate near back door)
                player.transform.position = spawnOffset;
                player.isInsideBus = false;

                // Restore exterior rendering
                if (SceneBootstrap.ExteriorWorldRoot != null)
                {
                    SceneBootstrap.ExteriorWorldRoot.SetActive(true);
                }

                // Restore atmospheric theme presets of active planet
                if (HorrorVisualPreset.Instance != null)
                {
                    HorrorVisualPreset.Instance.ApplyHorrorVisuals();
                }

                // Engine hum fades out
                if (engineHumSource != null) engineHumSource.volume = 0.05f;
            }

            // Sync character controller physics state immediately after warp
            if (cc != null)
            {
                cc.enabled = true;
                Physics.SyncTransforms();
            }
        }

        yield return new WaitForSeconds(0.15f); // Brief pause in blackness for camera/rendering to settle

        // 5. Fade Screen Back to Clear
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - (t / fadeDuration)));
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.color = new Color(0f, 0f, 0f, 0f);

        // 6. Update Central State Machine
        CurrentState = entering ? BusCabinState.InsideBus : BusCabinState.OutsideBus;

        // 7. UNLOCK INPUT after delay (Section 3 - 0.3s delay)
        yield return new WaitForSeconds(0.3f);
        IsInputLocked = false;
    }

    // Called when player physically walks out through the door without Q press
    public void ForceOutsideState()
    {
        if (CurrentState != BusCabinState.InsideBus && CurrentState != BusCabinState.TransitionToOutside) return;

        CurrentState = BusCabinState.OutsideBus;
        IsInputLocked = false;

        if (SceneBootstrap.ExteriorWorldRoot != null)
            SceneBootstrap.ExteriorWorldRoot.SetActive(true);

        if (HorrorVisualPreset.Instance != null)
            HorrorVisualPreset.Instance.ApplyHorrorVisuals();

        if (engineHumSource != null) engineHumSource.volume = 0.05f;

        RenderSettings.fog = true;
    }
}