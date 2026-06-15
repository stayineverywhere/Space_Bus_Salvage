using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    private static HUDManager _instance;
    public static HUDManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<HUDManager>();
            return _instance;
        }
    }

    // ── UI References (auto-created if null) ────────────────────────────────
    private Canvas mainCanvas;
    private Slider healthSlider;
    private Slider staminaSlider;
    private Slider oxygenSlider;
    private Slider curseMeterSlider;
    private Image curseEffectOverlay;
    private Image damageFlashOverlay;
    private TextMeshProUGUI contractGoalText;
    private TextMeshProUGUI dayText;
    private TextMeshProUGUI creditsText;
    private Slider busHealthSlider;
    private TextMeshProUGUI busPowerText;
    private GameObject interactionPromptGO;

    // New Screen-Aligned UI Fields
    private TextMeshProUGUI planetNameText;
    private TextMeshProUGUI lootCountText;
    private TextMeshProUGUI contractProgressText;
    private TextMeshProUGUI globalDayText;

    // Navigation and Interaction Feedback Fields
    private GameObject compassPanel;
    private TextMeshProUGUI compassArrowText;
    private TextMeshProUGUI compassDistanceText;
    private Slider interactionProgressSlider;
    private TextMeshProUGUI interactionProgressText;

    // Smooth target values
    private float targetHealth = 100f;
    private float targetStamina = 100f;
    private float targetOxygen = 100f;

    // Damage flash state
    private float flashAlpha;
    private const float FlashFadeSpeed = 3f;

    // Oxygen danger overlay
    private Image oxygenDangerOverlay;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInitializer();
    }

    private void EnsureInitializer()
    {
        if (HUDInitializer.Instance == null)
        {
            HUDInitializer hi = gameObject.AddComponent<HUDInitializer>();
            HUDInitializer.Initialize(hi);
        }
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        if (HUDInitializer.Instance == null) EnsureInitializer();

        mainCanvas = HUDInitializer.Instance.EnsureCanvas();
        Transform p = mainCanvas.transform;

        // Top-Left aligned Sliders (HP, Stamina, Oxygen) with labels
        healthSlider  = GetOrCreate<Slider>("HealthBar",  () => HUDInitializer.Instance.CreateSlider("HealthBar",  new Vector2(120f, -22f),  Color.red,   p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)));
        staminaSlider = GetOrCreate<Slider>("StaminaBar", () => HUDInitializer.Instance.CreateSlider("StaminaBar", new Vector2(120f, -48f),  Color.green, p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)));
        oxygenSlider  = GetOrCreate<Slider>("OxygenBar",  () => HUDInitializer.Instance.CreateSlider("OxygenBar",  new Vector2(120f, -74f),  Color.cyan,  p, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)));

        // Stat labels (HP / ST / O2)
        EnsureStatLabel(p, "LblHP",  "HP",  new Vector2(20f, -22f), Color.red);
        EnsureStatLabel(p, "LblST",  "ST",  new Vector2(20f, -48f), Color.green);
        EnsureStatLabel(p, "LblO2",  "O2",  new Vector2(20f, -74f), Color.cyan);

        // Top-Right aligned Texts (Credits, Planet Name, Loot Count, Global Day)
        creditsText    = GetOrCreate<TextMeshProUGUI>("CreditsText",    () => HUDInitializer.Instance.CreateText("CreditsText",    "Credits: 0",       new Vector2(-20f, -20f), 20, p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopRight));
        planetNameText = GetOrCreate<TextMeshProUGUI>("PlanetNameText", () => HUDInitializer.Instance.CreateText("PlanetNameText", "Planet: Unknown",  new Vector2(-20f, -50f), 18, p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopRight));
        lootCountText  = GetOrCreate<TextMeshProUGUI>("LootCountText",  () => HUDInitializer.Instance.CreateText("LootCountText",  "Loot: 0/20",       new Vector2(-20f, -80f), 18, p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopRight));
        globalDayText  = GetOrCreate<TextMeshProUGUI>("GlobalDayText",  () => HUDInitializer.Instance.CreateText("GlobalDayText",  "Day: 1",           new Vector2(-20f, -110f), 18, p, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopRight));

        // Top-Center aligned Texts (Goal, Day, Progress %)
        contractGoalText     = GetOrCreate<TextMeshProUGUI>("ContractGoal",         () => HUDInitializer.Instance.CreateText("ContractGoal",         "Goal: ---",     new Vector2(0f, -20f), 20, p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAlignmentOptions.Top));
        dayText              = GetOrCreate<TextMeshProUGUI>("DayText",              () => HUDInitializer.Instance.CreateText("DayText",              "Day: 1",        new Vector2(0f, -50f), 18, p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAlignmentOptions.Top));
        contractProgressText = GetOrCreate<TextMeshProUGUI>("ContractProgressText", () => HUDInitializer.Instance.CreateText("ContractProgressText", "Progress: 0%", new Vector2(0f, -80f), 18, p, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAlignmentOptions.Top));

        // Bottom-Center aligned Sliders and Texts (Bus HP, Power %, Curse Meter)
        busHealthSlider  = GetOrCreate<Slider>("BusHealth",   () => HUDInitializer.Instance.CreateSlider("BusHealth",  new Vector2(0f, 60f),  Color.yellow,  p, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)));
        busPowerText     = GetOrCreate<TextMeshProUGUI>("BusPower", () => HUDInitializer.Instance.CreateText("BusPower", "Power: 100%", new Vector2(0f, 30f), 18, p, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), TextAlignmentOptions.Bottom));
        curseMeterSlider = GetOrCreate<Slider>("CurseMeter",  () => HUDInitializer.Instance.CreateSlider("CurseMeter", new Vector2(0f, 100f), Color.magenta, p, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f)));

        curseEffectOverlay = GetOrCreate<Image>("CurseOverlay", () => HUDInitializer.Instance.CreateOverlay("CurseOverlay", new Color(0.5f, 0f, 0.5f, 0f), p));
        if (curseEffectOverlay != null) curseEffectOverlay.raycastTarget = false;
        damageFlashOverlay = GetOrCreate<Image>("DamageFlash",  () => HUDInitializer.Instance.CreateOverlay("DamageFlash",  new Color(1f, 0f, 0f, 0f), p));
        if (damageFlashOverlay != null) damageFlashOverlay.raycastTarget = false;
        oxygenDangerOverlay = GetOrCreate<Image>("OxygenDanger", () => HUDInitializer.Instance.CreateOverlay("OxygenDanger", new Color(0f, 0.5f, 1f, 0f), p));
        if (oxygenDangerOverlay != null) oxygenDangerOverlay.raycastTarget = false;

        if (interactionPromptGO == null)
        {
            var tmp = GetOrCreate<TextMeshProUGUI>("InteractionPrompt",
                () => HUDInitializer.Instance.CreateText("InteractionPrompt", "[E] INTERACT", new Vector2(0f, -200f), 28, p, TextAlignmentOptions.Center));
            interactionPromptGO = tmp.gameObject;
            interactionPromptGO.SetActive(false);
        }

        // Top-Center Compass Navigation
        compassPanel = GameObject.Find("CompassPanel");
        if (compassPanel == null)
        {
            compassPanel = new GameObject("CompassPanel");
            compassPanel.transform.SetParent(p, false);
            RectTransform compassRT = compassPanel.AddComponent<RectTransform>();
            compassRT.anchorMin = new Vector2(0.5f, 1f);
            compassRT.anchorMax = new Vector2(0.5f, 1f);
            compassRT.pivot = new Vector2(0.5f, 1f);
            compassRT.sizeDelta = new Vector2(250, 60);
            compassRT.anchoredPosition = new Vector2(0f, -110f);

            compassArrowText = HUDInitializer.Instance.CreateText("CompassArrow", "▲", new Vector2(0f, 12f), 24, compassPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
            compassDistanceText = HUDInitializer.Instance.CreateText("CompassDistance", "Bus: 0m", new Vector2(0f, -18f), 16, compassPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        }
        else
        {
            compassArrowText = compassPanel.transform.Find("CompassArrow")?.GetComponent<TextMeshProUGUI>();
            compassDistanceText = compassPanel.transform.Find("CompassDistance")?.GetComponent<TextMeshProUGUI>();
        }

        // Center Interaction Progress Slider
        interactionProgressSlider = GetOrCreate<Slider>("InteractionProgressBar", () => HUDInitializer.Instance.CreateSlider("InteractionProgressBar", new Vector2(0f, -100f), Color.yellow, p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)));
        interactionProgressSlider.gameObject.SetActive(false);

        interactionProgressText = GetOrCreate<TextMeshProUGUI>("InteractionProgressText", () => HUDInitializer.Instance.CreateText("InteractionProgressText", "Picking up...", new Vector2(0f, -75f), 16, p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center));
        interactionProgressText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Periodic rebuild safety
        if (Time.frameCount % 300 == 0) BuildUI();

        UpdatePlayerStats();
        UpdateContractUI();
        UpdateBusUI();
        UpdateCurseUI();
        UpdateDamageFlash();
        UpdateCompassUI();
        UpdateInteractionProgressUI();
    }

    // ── Update Methods ───────────────────────────────────────────────────────

    private void UpdatePlayerStats()
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            targetHealth  = (player.maxHealth  > 0f) ? (player.health  / player.maxHealth  * 100f) : 100f;
            targetStamina = (player.maxStamina > 0f) ? (player.stamina / player.maxStamina * 100f) : 100f;
            targetOxygen  = (player.maxOxygen  > 0f) ? (player.oxygen  / player.maxOxygen  * 100f) : 100f;
        }

        float lerpSpeed = Time.deltaTime * 8f;
        if (healthSlider)  healthSlider.value  = Mathf.Lerp(healthSlider.value,  targetHealth,  lerpSpeed);
        if (staminaSlider) staminaSlider.value = Mathf.Lerp(staminaSlider.value, targetStamina, lerpSpeed);
        if (oxygenSlider)  oxygenSlider.value  = Mathf.Lerp(oxygenSlider.value,  targetOxygen,  lerpSpeed);

        // Flash health bar red when low
        if (healthSlider != null)
        {
            Transform fill = healthSlider.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                Image img = fill.GetComponent<Image>();
                if (img != null)
                    img.color = targetHealth < 25f
                        ? Color.Lerp(Color.red, new Color(1f, 0.4f, 0.4f), Mathf.PingPong(Time.time * 3f, 1f))
                        : Color.red;
            }
        }

        if (player != null && oxygenSlider != null)
        {
            oxygenSlider.gameObject.SetActive(!player.isInsideBus);
        }

        // Oxygen danger pulsing overlay
        if (oxygenDangerOverlay != null && player != null)
        {
            if (!player.isInsideBus && player.oxygen < 20f)
            {
                float t = Mathf.PingPong(Time.time * 3f, 1f);
                float ratio = 1f - (player.oxygen / 20f);
                Color c = oxygenDangerOverlay.color;
                c.a = Mathf.Lerp(0f, 0.45f * ratio, t);
                oxygenDangerOverlay.color = c;
            }
            else
            {
                Color c = oxygenDangerOverlay.color;
                c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 5f);
                oxygenDangerOverlay.color = c;
            }
        }
    }

    private void UpdateCompassUI()
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        BusController bus = Object.FindAnyObjectByType<BusController>();
        
        if (player == null || bus == null)
        {
            if (compassPanel != null) compassPanel.SetActive(false);
            return;
        }

        if (compassPanel != null) compassPanel.SetActive(true);

        Vector3 playerPos = player.transform.position;
        Vector3 busPos = bus.transform.position;
        float dist = Vector3.Distance(playerPos, busPos);

        // Compass Arrow Pointing to Bus
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam != null && compassArrowText != null)
        {
            Vector3 toBus = (busPos - playerPos);
            toBus.y = 0f; // horizontal direction

            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            float forwardDot = Vector3.Dot(camForward, toBus.normalized);
            float rightDot = Vector3.Dot(camRight, toBus.normalized);

            float angle = Mathf.Atan2(rightDot, forwardDot) * Mathf.Rad2Deg;
            compassArrowText.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        if (compassDistanceText != null)
        {
            bool carryingLoot = (player.carriedLoot != null);
            if (carryingLoot)
            {
                compassDistanceText.text = $"[!] Return to Bus: {Mathf.RoundToInt(dist)}m";
                compassDistanceText.color = Color.yellow;
                if (compassArrowText != null) compassArrowText.color = Color.yellow;
            }
            else
            {
                compassDistanceText.text = $"Bus: {Mathf.RoundToInt(dist)}m";
                compassDistanceText.color = Color.white;
                if (compassArrowText != null) compassArrowText.color = Color.white;
            }
        }
    }

    private void UpdateInteractionProgressUI()
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null || interactionProgressSlider == null || interactionProgressText == null)
        {
            if (interactionProgressSlider != null) interactionProgressSlider.gameObject.SetActive(false);
            if (interactionProgressText != null) interactionProgressText.gameObject.SetActive(false);
            return;
        }

        if (player.currentInteractionState == PlayerMovement.InteractionState.Interacting)
        {
            interactionProgressSlider.gameObject.SetActive(true);
            interactionProgressText.gameObject.SetActive(true);

            float ratio = player.interactionProgress; // 0.0f to 1.0f
            interactionProgressSlider.value = ratio * 100f;
            interactionProgressText.text = player.interactionActionName;
        }
        else
        {
            interactionProgressSlider.gameObject.SetActive(false);
            interactionProgressText.gameObject.SetActive(false);
        }
    }

    private void UpdateContractUI()
    {
        if (ContractManager.Instance == null) return;
        if (contractGoalText)
        {
            if (ContractManager.Instance.currentContract != null)
                contractGoalText.text = $"Goal: {ContractManager.Instance.currentCredits}/{ContractManager.Instance.currentContract.creditGoal}";
            else
                contractGoalText.text = "No Contract";
        }
        if (dayText)
        {
            if (ContractManager.Instance.currentContract != null)
                dayText.text = $"Day: {ContractManager.Instance.currentDay}/{ContractManager.Instance.currentContract.durationDays}";
            else
                dayText.text = "";
        }
        if (creditsText && GarageManager.Instance != null)
            creditsText.text = $"Credits: {GarageManager.Instance.totalCredits}";

        if (globalDayText)
            globalDayText.text = $"Day: {ContractManager.Instance?.globalDay ?? 1}";

        // Update Planet Name
        if (planetNameText)
        {
            string planetName = (PlanetManager.Instance != null && !string.IsNullOrEmpty(PlanetManager.Instance.activePlanetName))
                ? PlanetManager.Instance.activePlanetName
                : "Orbit / Garage";
            planetNameText.text = $"Planet: {planetName}";
        }

        // Update Loot Count in Bus Storage
        if (lootCountText)
        {
            BusController bus = Object.FindAnyObjectByType<BusController>();
            int currentLoot = (bus != null && bus.storage != null) ? bus.storage.storedLoot.Count : 0;
            int maxLoot = (bus != null && bus.storage != null) ? bus.storage.maxCapacity : 20;
            lootCountText.text = $"Loot: {currentLoot}/{maxLoot}";
        }

        // Update Contract Progress % completion
        if (contractProgressText)
        {
            if (ContractManager.Instance.currentContract != null && ContractManager.Instance.currentContract.creditGoal > 0)
            {
                float pct = (float)ContractManager.Instance.currentCredits / ContractManager.Instance.currentContract.creditGoal * 100f;
                contractProgressText.text = $"Progress: {Mathf.RoundToInt(pct)}%";
            }
            else
            {
                contractProgressText.text = "";
            }
        }
    }

    private void UpdateBusUI()
    {
        BusController bus = Object.FindAnyObjectByType<BusController>();
        if (bus == null) return;
        if (busHealthSlider)
        {
            busHealthSlider.maxValue = bus.maxHealth;
            busHealthSlider.value = Mathf.Lerp(busHealthSlider.value, bus.currentHealth, Time.deltaTime * 5f);
        }
        if (bus.powerSystem != null && busPowerText)
            busPowerText.text = $"Power: {Mathf.RoundToInt(bus.powerSystem.currentPower)}%";
    }

    private void UpdateCurseUI()
    {
        if (CurseManager.Instance == null) return;
        if (curseMeterSlider)
        {
            curseMeterSlider.maxValue = CurseManager.Instance.maxCurseValue;
            curseMeterSlider.value = Mathf.Lerp(curseMeterSlider.value,
                CurseManager.Instance.globalCurseValue, Time.deltaTime * 5f);
        }
        if (curseEffectOverlay != null)
        {
            float a = (CurseManager.Instance.globalCurseValue / CurseManager.Instance.maxCurseValue) * 0.35f;
            Color c = curseEffectOverlay.color;
            c.a = Mathf.Lerp(c.a, a, Time.deltaTime * 3f);
            curseEffectOverlay.color = c;
        }
    }

    private void UpdateDamageFlash()
    {
        if (damageFlashOverlay == null) return;
        flashAlpha = Mathf.Max(0f, flashAlpha - FlashFadeSpeed * Time.deltaTime);
        Color c = damageFlashOverlay.color;
        c.a = flashAlpha;
        damageFlashOverlay.color = c;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void TriggerDamageFlash()
    {
        flashAlpha = 0.6f;
    }

    public void TriggerDeathFlash()
    {
        // Full crimson screen fade for death
        flashAlpha = 1.0f;
    }

    public void SetInteractionPrompt(bool active, string customText = "[E] INTERACT")
    {
        if (interactionPromptGO != null)
        {
            interactionPromptGO.SetActive(active);
            var tmp = interactionPromptGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = customText;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureStatLabel(Transform parent, string goName, string text, Vector2 pos, Color color)
    {
        if (mainCanvas == null) return;
        if (mainCanvas.transform.Find(goName) != null) return;

        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(60f, 22f);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.color = color;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }

    private T GetOrCreate<T>(string goName, System.Func<T> createFunc) where T : Component
    {
        if (mainCanvas != null)
        {
            Transform child = mainCanvas.transform.Find(goName);
            if (child != null)
            {
                T comp = child.GetComponent<T>();
                if (comp != null) return comp;
            }
        }
        GameObject go = GameObject.Find(goName);
        if (go != null)
        {
            T comp = go.GetComponent<T>();
            if (comp != null) return comp;
        }
        return createFunc();
    }
}
