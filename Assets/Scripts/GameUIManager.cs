using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Procedurally builds every non-HUD game panel (Garage, Contract, Planet, Sell, Upgrade).
/// Works entirely without prefabs — safe for immediate Play Mode use.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    private static GameUIManager _instance;
    public static GameUIManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<GameUIManager>();
            return _instance;
        }
    }

    // ── Panels ───────────────────────────────────────────────────────────────
    private Canvas uiCanvas;
    private GameObject garagePanel;
    private GameObject contractPanel;
    private GameObject planetPanel;
    private GameObject sellPanel;
    private GameObject upgradePanel;
    private GameObject resultPanel;
    private GameObject warpPanel;
    private TextMeshProUGUI garageCreditsTxt;

    // ── Manual click detection (bypasses EventSystem to avoid InputSystem issues) ──
    private Button[] _activeButtons;

    // ── Default Data ─────────────────────────────────────────────────────────
    private Contract[] defaultContracts;

    private struct PlanetInfo { public string name; public HorrorVisualPreset.PlanetTheme theme; public int difficulty; }
    private PlanetInfo[] defaultPlanets;

    // ── Default Upgrade Definitions ──────────────────────────────────────────
    private struct UpgradeDef { public string name; public int cost; public bool isBus; }
    private UpgradeDef[] upgradePool;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        CreateDefaultData();
        BuildCanvas();
        BuildAllPanels();
        RegisterGarageUpgrades();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.OnStateChanged += OnStateChanged;

        OnStateChanged(GameLoopManager.Instance != null
            ? GameLoopManager.Instance.CurrentState
            : GameLoopManager.GameState.Garage);
    }

    private void OnDestroy()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.OnStateChanged -= OnStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Data Creation ─────────────────────────────────────────────────────────

    private void CreateDefaultData()
    {
        // Contracts
        defaultContracts = new Contract[3];

        Contract c0 = ScriptableObject.CreateInstance<Contract>();
        c0.name = c0.contractName = "Routine Salvage";
        c0.creditGoal = 800; c0.durationDays = 3; c0.survivorBonusGoal = 1;
        c0.debtIncreaseOnFailure = 400; c0.reputationLossOnFailure = 0.1f;
        defaultContracts[0] = c0;

        Contract c1 = ScriptableObject.CreateInstance<Contract>();
        c1.name = c1.contractName = "Deep Extraction";
        c1.creditGoal = 1800; c1.durationDays = 4; c1.survivorBonusGoal = 2;
        c1.debtIncreaseOnFailure = 800; c1.reputationLossOnFailure = 0.2f;
        defaultContracts[1] = c1;

        Contract c2 = ScriptableObject.CreateInstance<Contract>();
        c2.name = c2.contractName = "High Risk Haul";
        c2.creditGoal = 3500; c2.durationDays = 5; c2.survivorBonusGoal = 3;
        c2.debtIncreaseOnFailure = 1500; c2.reputationLossOnFailure = 0.35f;
        defaultContracts[2] = c2;

        // Planets
        defaultPlanets = new PlanetInfo[]
        {
            new PlanetInfo { name = "Assurance",  theme = HorrorVisualPreset.PlanetTheme.Default, difficulty = 1 },
            new PlanetInfo { name = "Vow",         theme = HorrorVisualPreset.PlanetTheme.Ice,     difficulty = 2 },
            new PlanetInfo { name = "Offense",     theme = HorrorVisualPreset.PlanetTheme.Mining,  difficulty = 3 },
        };

        // Upgrades
        upgradePool = new UpgradeDef[]
        {
            new UpgradeDef { name = "Hull Reinforcement", cost = 600,  isBus = true  },
            new UpgradeDef { name = "Power Cell",         cost = 400,  isBus = true  },
            new UpgradeDef { name = "Speed Boots",        cost = 300,  isBus = false },
            new UpgradeDef { name = "Stamina Pack",       cost = 250,  isBus = false },
            new UpgradeDef { name = "O2 Tank",            cost = 350,  isBus = false },
        };
    }

    private void RegisterGarageUpgrades()
    {
        if (GarageManager.Instance == null) return;
        GarageManager.Instance.availableBusUpgrades.Clear();
        GarageManager.Instance.availablePlayerUpgrades.Clear();

        foreach (var def in upgradePool)
        {
            if (def.isBus)
            {
                var u = ScriptableObject.CreateInstance<BusUpgrade>();
                u.upgradeName = def.name; u.cost = def.cost;
                GarageManager.Instance.availableBusUpgrades.Add(u);
            }
            else
            {
                var u = ScriptableObject.CreateInstance<PlayerUpgrade>();
                u.upgradeName = def.name; u.cost = def.cost; u.maxLevel = 3;
                GarageManager.Instance.availablePlayerUpgrades.Add(u);
            }
        }
    }

    // ── Canvas ───────────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        GameObject cGO = new GameObject("GameUI_Canvas");
        DontDestroyOnLoad(cGO);
        uiCanvas = cGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 20; // Higher than HUD overlays so buttons are on top
        CanvasScaler cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists — without it, no UI buttons respond to clicks
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            DontDestroyOnLoad(esGO);
            esGO.AddComponent<EventSystem>();
            var uiModule = esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
        }
    }

    // ── Panel Builders ───────────────────────────────────────────────────────

    private void BuildAllPanels()
    {
        garagePanel   = BuildGaragePanel();
        contractPanel = BuildContractPanel();
        planetPanel   = BuildPlanetPanel();
        sellPanel     = BuildSellPanel();
        upgradePanel  = BuildUpgradePanel();
        resultPanel   = BuildResultPanel();
        warpPanel     = BuildWarpPanel();
    }

    private GameObject BuildGaragePanel()
    {
        GameObject panel = CreatePanel("GaragePanel", new Color(0.04f, 0.04f, 0.08f, 0.92f));

        AddLabel(panel.transform, "SPACE BUS SALVAGE", new Vector2(0, 340), 52, Color.white, true);
        AddLabel(panel.transform, "GARAGE HUB", new Vector2(0, 280), 28, new Color(0.6f, 0.6f, 1f));

        garageCreditsTxt = AddLabel(panel.transform, "Credits: 0", new Vector2(0, 220), 26, new Color(1f, 0.85f, 0.2f));

        AddButton(panel.transform, "START MISSION", new Vector2(0, 100), new Color(0.1f, 0.5f, 0.1f),
            () => GameLoopManager.Instance?.StartContractSelection());

        AddButton(panel.transform, "UPGRADE SHOP", new Vector2(0, 20), new Color(0.1f, 0.2f, 0.5f),
            () => GameLoopManager.Instance?.OpenUpgradeShop());

        AddLabel(panel.transform, "WASD = Move  |  Mouse = Look  |  E = Interact  |  Q = Bus Door  |  T = Pilot Seat  |  F = Flashlight  |  Shift = Sprint  |  Tab = Exit Bus  |  R = Return to Base",
            new Vector2(0, -370), 14, new Color(0.5f, 0.5f, 0.5f));

        return panel;
    }

    private GameObject BuildContractPanel()
    {
        GameObject panel = CreatePanel("ContractPanel", new Color(0.04f, 0.04f, 0.08f, 0.95f));
        AddLabel(panel.transform, "SELECT CONTRACT", new Vector2(0, 350), 44, Color.white, true);

        float startX = -540f;
        float cardW = 340f;
        for (int i = 0; i < defaultContracts.Length; i++)
        {
            Contract c = defaultContracts[i];
            float x = startX + i * (cardW + 40f);
            BuildContractCard(panel.transform, c, new Vector2(x, 0));
        }

        AddButton(panel.transform, "← BACK", new Vector2(-600, -380), new Color(0.3f, 0.1f, 0.1f),
            () => GameLoopManager.Instance?.TransitionTo(GameLoopManager.GameState.Garage));

        return panel;
    }

    private void BuildContractCard(Transform parent, Contract c, Vector2 pos)
    {
        GameObject card = new GameObject($"Card_{c.contractName}");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340, 420);
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.15f, 1f);

        Transform ct = card.transform;
        AddLabel(ct, c.contractName,                     new Vector2(0,  160), 22, Color.white, true);
        AddLabel(ct, $"Goal: {c.creditGoal} Credits",    new Vector2(0,  100), 18, new Color(1f, 0.85f, 0.2f));
        AddLabel(ct, $"Duration: {c.durationDays} Days", new Vector2(0,   60), 17, Color.cyan);
        AddLabel(ct, $"Bonus: Rescue {c.survivorBonusGoal}", new Vector2(0, 20), 16, Color.green);
        AddLabel(ct, $"Penalty: -{c.debtIncreaseOnFailure}", new Vector2(0, -20), 16, Color.red);

        Contract captured = c;
        AddButton(ct, "SELECT", new Vector2(0, -140), new Color(0.1f, 0.4f, 0.1f),
            () => GameLoopManager.Instance?.SelectContract(captured),
            new Vector2(260, 50));
    }

    private GameObject BuildPlanetPanel()
    {
        GameObject panel = CreatePanel("PlanetPanel", new Color(0.04f, 0.04f, 0.08f, 0.95f));
        AddLabel(panel.transform, "SELECT PLANET", new Vector2(0, 350), 44, Color.white, true);

        float startX = -440f;
        float cardW = 300f;
        for (int i = 0; i < defaultPlanets.Length; i++)
        {
            PlanetInfo p = defaultPlanets[i];
            float x = startX + i * (cardW + 40f);
            BuildPlanetCard(panel.transform, p, new Vector2(x, 0));
        }

        AddButton(panel.transform, "← BACK", new Vector2(-600, -380), new Color(0.3f, 0.1f, 0.1f),
            () => GameLoopManager.Instance?.TransitionTo(GameLoopManager.GameState.ContractSelection));

        return panel;
    }

    private void BuildPlanetCard(Transform parent, PlanetInfo p, Vector2 pos)
    {
        GameObject card = new GameObject($"Planet_{p.name}");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 380);
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        card.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 1f);

        Transform ct = card.transform;
        Color diffColor = p.difficulty <= 1 ? Color.green : (p.difficulty == 2 ? Color.yellow : Color.red);
        string diffLabel = p.difficulty <= 1 ? "SAFE" : (p.difficulty == 2 ? "MODERATE" : "DANGEROUS");

        AddLabel(ct, p.name,                              new Vector2(0,  140), 26, Color.white, true);
        AddLabel(ct, p.theme.ToString(),                  new Vector2(0,   90), 18, Color.cyan);
        AddLabel(ct, $"Difficulty: {diffLabel}",          new Vector2(0,   50), 17, diffColor);
        AddLabel(ct, $"Risk Level: {p.difficulty}/3",     new Vector2(0,   10), 16, diffColor);

        PlanetInfo captured = p;
        AddButton(ct, "DEPLOY →", new Vector2(0, -110), new Color(0.5f, 0.1f, 0.1f),
            () => GameLoopManager.Instance?.DeployToPlanet(captured.name, captured.theme, captured.difficulty),
            new Vector2(220, 50));
    }

    private GameObject BuildSellPanel()
    {
        GameObject panel = CreatePanel("SellPanel", new Color(0.04f, 0.06f, 0.04f, 0.95f));
        AddLabel(panel.transform, "MISSION COMPLETE", new Vector2(0, 280), 48, new Color(0.4f, 1f, 0.6f), true);
        AddLabel(panel.transform, "─── RETURNED TO BASE ───", new Vector2(0, 220), 22, new Color(0.5f, 0.8f, 0.5f));

        // Dynamic texts refreshed in OnStateChanged
        panel.AddComponent<SellPanelRefresher>();

        AddButton(panel.transform, "SELL ALL LOOT", new Vector2(0, -80), new Color(0.1f, 0.5f, 0.1f),
            () => { GameLoopManager.Instance?.ConfirmSell(); });

        AddButton(panel.transform, "SKIP", new Vector2(0, -160), new Color(0.2f, 0.2f, 0.2f),
            () => { GameLoopManager.Instance?.ConfirmSell(); });

        return panel;
    }

    private GameObject BuildUpgradePanel()
    {
        GameObject panel = CreatePanel("UpgradePanel", new Color(0.04f, 0.04f, 0.1f, 0.95f));
        AddLabel(panel.transform, "UPGRADE SHOP", new Vector2(0, 350), 44, Color.white, true);

        AddLabel(panel.transform, "BUS UPGRADES", new Vector2(-300, 270), 24, new Color(1f, 0.8f, 0.2f));
        AddLabel(panel.transform, "PLAYER UPGRADES", new Vector2(260, 270), 24, new Color(0.4f, 1f, 0.4f));

        // Bus upgrades (left column)
        float yBus = 200f;
        foreach (var def in upgradePool)
        {
            if (!def.isBus) continue;
            UpgradeDef d = def;
            AddButton(panel.transform, $"{d.name}  [{d.cost}¢]", new Vector2(-300, yBus),
                new Color(0.1f, 0.2f, 0.5f),
                () => {
                    var bus = GarageManager.Instance?.availableBusUpgrades.Find(u => u.upgradeName == d.name);
                    if (bus != null) GarageManager.Instance.BuyBusUpgrade(bus);
                },
                new Vector2(380, 52));
            yBus -= 70f;
        }

        // Player upgrades (right column)
        float yPlayer = 200f;
        foreach (var def in upgradePool)
        {
            if (def.isBus) continue;
            UpgradeDef d = def;
            AddButton(panel.transform, $"{d.name}  [{d.cost}¢]", new Vector2(260, yPlayer),
                new Color(0.1f, 0.4f, 0.1f),
                () => {
                    var pu = GarageManager.Instance?.availablePlayerUpgrades.Find(u => u.upgradeName == d.name);
                    if (pu != null) GarageManager.Instance.BuyPlayerUpgrade(pu);
                },
                new Vector2(380, 52));
            yPlayer -= 70f;
        }

        panel.AddComponent<UpgradePanelCredits>();

        AddButton(panel.transform, "← BACK TO GARAGE", new Vector2(0, -380), new Color(0.3f, 0.1f, 0.1f),
            () => GameLoopManager.Instance?.CloseUpgradeShop());

        return panel;
    }

    private GameObject BuildResultPanel()
    {
        GameObject panel = CreatePanel("ResultPanel", new Color(0.02f, 0.0f, 0.0f, 0.97f));

        // Blood-red title
        AddLabel(panel.transform, "YOU DIED", new Vector2(0, 260), 80, new Color(0.9f, 0.05f, 0.05f), true);
        AddLabel(panel.transform, "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", new Vector2(0, 195), 20, new Color(0.5f, 0.0f, 0.0f));

        // Stats at death
        panel.AddComponent<DeathStatDisplay>();

        AddLabel(panel.transform, "Press any key to return to Garage", new Vector2(0, -260), 24, new Color(0.6f, 0.6f, 0.6f));
        panel.AddComponent<ResultPanelInput>();
        return panel;
    }

    private GameObject BuildWarpPanel()
    {
        GameObject panel = new GameObject("WarpPanel");
        panel.transform.SetParent(uiCanvas.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;
        panel.SetActive(false);
        return panel;
    }

    // ── Warp Animation ────────────────────────────────────────────────────────

    public void PlayReturnAnimation(System.Action onComplete)
    {
        StartCoroutine(WarpRoutine(onComplete));
    }

    private System.Collections.IEnumerator WarpRoutine(System.Action onComplete)
    {
        warpPanel.SetActive(true);
        Image bg = warpPanel.GetComponent<Image>();

        // ── Build warp text
        GameObject textGO = new GameObject("WarpText");
        textGO.transform.SetParent(warpPanel.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta = new Vector2(800f, 100f);
        textRT.anchoredPosition = new Vector2(0f, 60f);
        TextMeshProUGUI label = textGO.AddComponent<TextMeshProUGUI>();
        label.text = "WARPING TO BASE...";
        label.fontSize = 48;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.4f, 0.8f, 1f, 0f);
        label.fontStyle = FontStyles.Bold;

        // ── Build stars (radiate from center outward)
        int starCount = 40;
        var starImages = new Image[starCount];
        var starDirs   = new Vector2[starCount];
        for (int i = 0; i < starCount; i++)
        {
            GameObject s = new GameObject("Star" + i);
            s.transform.SetParent(warpPanel.transform, false);
            RectTransform sRT = s.AddComponent<RectTransform>();
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.5f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            starDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float dist = Random.Range(20f, 80f);
            sRT.anchoredPosition = starDirs[i] * dist;
            float w = Random.Range(2f, 5f);
            float h = Random.Range(12f, 35f);
            sRT.sizeDelta = new Vector2(w, h);
            sRT.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(starDirs[i].y, starDirs[i].x) * Mathf.Rad2Deg - 90f);
            Image si = s.AddComponent<Image>();
            si.color = new Color(0.9f, 0.95f, 1f, 0f);
            si.raycastTarget = false;
            starImages[i] = si;
        }

        // Phase 1: fade to black (0.5s)
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float a = t / 0.5f;
            bg.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        bg.color = Color.black;

        // Phase 2: warp stars + text appear (1.5s)
        float warpDuration = 1.5f;
        float warpSpeed = 900f;
        for (float t = 0f; t < warpDuration; t += Time.unscaledDeltaTime)
        {
            float progress = t / warpDuration;
            float starAlpha = Mathf.Clamp01(progress * 3f);
            float textAlpha = Mathf.Clamp01((progress - 0.2f) * 5f);
            label.color = new Color(0.4f, 0.8f, 1f, textAlpha);

            for (int i = 0; i < starCount; i++)
            {
                RectTransform sRT = starImages[i].rectTransform;
                sRT.anchoredPosition += starDirs[i] * warpSpeed * Time.unscaledDeltaTime;
                starImages[i].color = new Color(0.9f, 0.95f, 1f, starAlpha * Random.Range(0.7f, 1f));
            }
            yield return null;
        }

        // Phase 3: destroy all warp objects FIRST so they can't block UI clicks
        Destroy(textGO);
        for (int i = 0; i < starCount; i++)
            if (starImages[i] != null) Destroy(starImages[i].gameObject);
        warpPanel.SetActive(false);
        bg.color = new Color(0f, 0f, 0f, 0f);

        // Phase 4: callback — sell panel will now appear with nothing blocking it
        onComplete?.Invoke();
    }

    // ── State Switch ─────────────────────────────────────────────────────────

    private void OnStateChanged(GameLoopManager.GameState state)
    {
        garagePanel?.SetActive(state == GameLoopManager.GameState.Garage);
        contractPanel?.SetActive(state == GameLoopManager.GameState.ContractSelection);
        planetPanel?.SetActive(state == GameLoopManager.GameState.PlanetSelection);
        sellPanel?.SetActive(state == GameLoopManager.GameState.Selling);
        upgradePanel?.SetActive(state == GameLoopManager.GameState.Upgrading);
        resultPanel?.SetActive(state == GameLoopManager.GameState.Result);

        // Lock/unlock cursor based on state
        bool menuOpen = state != GameLoopManager.GameState.Exploration;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;

        if (state == GameLoopManager.GameState.Selling)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Time.timeScale < 1f) Time.timeScale = 1f;
        }

        // Collect all interactable buttons in the newly active panel for manual click detection
        _activeButtons = null;
        GameObject activePanel = state switch
        {
            GameLoopManager.GameState.Garage           => garagePanel,
            GameLoopManager.GameState.ContractSelection => contractPanel,
            GameLoopManager.GameState.PlanetSelection  => planetPanel,
            GameLoopManager.GameState.Selling          => sellPanel,
            GameLoopManager.GameState.Upgrading        => upgradePanel,
            _                                          => null
        };
        if (activePanel != null)
            _activeButtons = activePanel.GetComponentsInChildren<Button>(false);

        // Refresh garage credits display
        if (state == GameLoopManager.GameState.Garage && garageCreditsTxt != null && GarageManager.Instance != null)
            garageCreditsTxt.text = $"Credits: {GarageManager.Instance.totalCredits}";
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Destroy any EventSystem that belongs to the newly loaded scene —
        // duplicate EventSystems can make InputSystemUIInputModule stop processing clicks.
        var allES = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in allES)
        {
            if (es.gameObject.scene == scene)
                Destroy(es.gameObject);
        }

        // Re-apply current state so panels and cursor are correct after scene swap
        var state = GameLoopManager.Instance != null
            ? GameLoopManager.Instance.CurrentState
            : GameLoopManager.GameState.Garage;
        OnStateChanged(state);
    }

    void Update()
    {
        // Enforce cursor visibility in all menu states every frame
        if (GameLoopManager.Instance != null &&
            GameLoopManager.Instance.CurrentState != GameLoopManager.GameState.Exploration)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        // Manual click detection — bypasses EventSystem so buttons always work
        // even when InputSystemUIInputModule fails after scene transitions.
        if (_activeButtons != null &&
            UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            foreach (var btn in _activeButtons)
            {
                if (btn == null || !btn.isActiveAndEnabled || !btn.interactable) continue;
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                {
                    btn.onClick.Invoke();
                    break;
                }
            }
        }

        // Refresh garage credits every 10 frames
        if (Time.frameCount % 10 == 0 && garageCreditsTxt != null && GarageManager.Instance != null
            && GameLoopManager.Instance?.CurrentState == GameLoopManager.GameState.Garage)
            garageCreditsTxt.text = $"Credits: {GarageManager.Instance.totalCredits}";
    }

    // ── UI Helper Factories ──────────────────────────────────────────────────

    private GameObject CreatePanel(string name, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(uiCanvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        go.SetActive(false);
        return go;
    }

    private TextMeshProUGUI AddLabel(Transform parent, string text, Vector2 pos, int size,
        Color color, bool bold = false)
    {
        GameObject go = new GameObject("Lbl_" + text.Substring(0, Mathf.Min(text.Length, 20)));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1200, 80);
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return tmp;
    }

    private Button AddButton(Transform parent, string label, Vector2 pos, Color bgColor,
        UnityEngine.Events.UnityAction onClick, Vector2? size = null)
    {
        Vector2 btnSize = size ?? new Vector2(400, 60);

        GameObject go = new GameObject("Btn_" + label.Substring(0, Mathf.Min(label.Length, 20)));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = btnSize;
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.35f;
        cb.pressedColor = bgColor * 0.7f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        // Label
        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        RectTransform lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = Mathf.Clamp(Mathf.RoundToInt(btnSize.y * 0.42f), 14, 32);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return btn;
    }
}

// ── Helper MonoBehaviours attached to panels ─────────────────────────────────

public class SellPanelRefresher : MonoBehaviour
{
    private TextMeshProUGUI earnedTxt;
    private TextMeshProUGUI bankTxt;

    private void Start()
    {
        earnedTxt = CreateText("Earned: 0 Credits", new Vector2(0, 100), 36, new Color(1f, 0.85f, 0.2f));
        bankTxt   = CreateText("Bank: 0 Credits",   new Vector2(0, 40),  28, Color.white);
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (earnedTxt == null || bankTxt == null) return;
        int earned = GarageManager.Instance?.pendingSellAmount ?? 0;
        int bank   = GarageManager.Instance?.totalCredits ?? 0;
        earnedTxt.text = $"Loot Value: +{earned} Credits";
        bankTxt.text   = $"Current Bank: {bank} Credits";
    }

    private TextMeshProUGUI CreateText(string text, Vector2 pos, int size, Color color)
    {
        GameObject go = new GameObject("SellTxt");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 70);
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}

public class UpgradePanelCredits : MonoBehaviour
{
    private TextMeshProUGUI creditsTxt;

    private void Start()
    {
        GameObject go = new GameObject("UpgradeCredits");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 60);
        rt.anchoredPosition = new Vector2(0, 290);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        creditsTxt = go.AddComponent<TextMeshProUGUI>();
        creditsTxt.fontSize = 26;
        creditsTxt.alignment = TextAlignmentOptions.Center;
        creditsTxt.color = new Color(1f, 0.85f, 0.2f);
    }

    private void Update()
    {
        if (creditsTxt != null && GarageManager.Instance != null)
            creditsTxt.text = $"Available: {GarageManager.Instance.totalCredits} Credits";
    }
}

public class DeathStatDisplay : MonoBehaviour
{
    private TextMeshProUGUI creditsText;
    private TextMeshProUGUI dayText;
    private TextMeshProUGUI causeText;

    private void Start()
    {
        creditsText = CreateText("", new Vector2(0, 100), 26, new Color(1f, 0.85f, 0.2f));
        dayText     = CreateText("", new Vector2(0, 55),  22, new Color(0.7f, 0.7f, 1f));
        causeText   = CreateText("", new Vector2(0, 0),   20, new Color(0.8f, 0.4f, 0.4f));
    }

    private void OnEnable()
    {
        if (creditsText == null) return;
        int credits = ContractManager.Instance?.currentCredits ?? 0;
        int day = ContractManager.Instance?.currentDay ?? 1;
        creditsText.text = $"Credits collected: {credits}";
        dayText.text = $"Day {day} — mission failed";

        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            causeText.text = pm.oxygen <= 0f ? "Cause of death: Oxygen deprivation" : "Cause of death: Fatal injuries";
        else
            causeText.text = "";
    }

    private TextMeshProUGUI CreateText(string text, Vector2 pos, int size, Color color)
    {
        GameObject go = new GameObject("DeathStat");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 60);
        rt.anchoredPosition = pos;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}

public class ResultPanelInput : MonoBehaviour
{
    private bool _triggered = false;

    private void OnEnable()
    {
        _triggered = false;
    }

    private void Update()
    {
        if (_triggered) return;

        bool anyPressed = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame) ||
                          (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame);
        if (anyPressed)
        {
            _triggered = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameLoopManager.Instance?.TransitionTo(GameLoopManager.GameState.Garage);
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("GarageHub");
        }
    }
}
