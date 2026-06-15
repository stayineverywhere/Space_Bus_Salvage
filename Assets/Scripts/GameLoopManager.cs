using UnityEngine;

public class GameLoopManager : MonoBehaviour
{
    private static GameLoopManager _instance;
    public static GameLoopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<GameLoopManager>();
            }
            return _instance;
        }
    }

    public enum GameState
    {
        Garage,
        ContractSelection,
        PlanetSelection,
        Exploration,
        Selling,
        Upgrading,
        Result
    }

    public GameState CurrentState { get; private set; } = GameState.Garage;
    public System.Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── State Transitions ───────────────────────────────────────────────────

    public void TransitionTo(GameState newState)
    {
        if (CurrentState == newState) return;
        Debug.Log($"[GameLoop] {CurrentState} → {newState}");
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void StartContractSelection()
    {
        // If a contract is active mid-run, skip straight to planet selection
        if (ContractManager.Instance?.currentContract != null)
        {
            TransitionTo(GameState.PlanetSelection);
            return;
        }
        TransitionTo(GameState.ContractSelection);
    }

    public void SelectContract(Contract contract)
    {
        if (ContractManager.Instance == null)
        {
            Debug.LogError("[GameLoop] ContractManager missing!");
            return;
        }
        ContractManager.Instance.SetContract(contract);
        TransitionTo(GameState.PlanetSelection);
    }

    public void DeployToPlanet(string planetName, HorrorVisualPreset.PlanetTheme theme, int difficulty)
    {
        if (PlanetManager.Instance != null)
            PlanetManager.Instance.SetActivePlanet(planetName, theme, difficulty);

        TransitionTo(GameState.Exploration);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene("PlanetExploration");
        else
            Debug.LogWarning("[GameLoop] SceneLoader missing — staying in current scene.");
    }

    public void ReturnToGarage()
    {
        StartCoroutine(ReturnToGarageRoutine());
    }

    private System.Collections.IEnumerator ReturnToGarageRoutine()
    {
        // Re-enable player movement if disabled (pilot seat)
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.enabled = true;
            pm.isInsideBus = false;
        }

        // Play warp animation and wait for it to finish
        bool animDone = false;
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.PlayReturnAnimation(() => animDone = true);
        else
            animDone = true;

        yield return new WaitUntil(() => animDone);

        // Tally ALL loot: stored in bus + carried by player + children of bus transform
        if (GarageManager.Instance != null)
        {
            int lootTotal = 0;

            BusController bus = Object.FindAnyObjectByType<BusController>();
            PlayerMovement pm2 = Object.FindAnyObjectByType<PlayerMovement>();

            // 1. BusStorage list
            if (bus != null)
            {
                BusStorage stor = bus.storage ?? bus.GetComponent<BusStorage>();
                if (stor == null) stor = bus.gameObject.AddComponent<BusStorage>();
                bus.storage = stor;
                foreach (var item in stor.storedLoot)
                    if (item != null) lootTotal += item.value;
                Debug.Log($"[GameLoop] BusStorage: {lootTotal} ({stor.storedLoot.Count} items)");
            }

            // 2. Loot currently carried by player
            if (pm2 != null && pm2.carriedLoot != null)
            {
                lootTotal += pm2.carriedLoot.value;
                Debug.Log($"[GameLoop] Carried loot: +{pm2.carriedLoot.value} ({pm2.carriedLoot.itemName})");
                pm2.carriedLoot = null;
            }

            // 3. Any LootItem that ended up as a child of the bus but not in storedLoot list
            if (bus != null)
            {
                foreach (var item in bus.GetComponentsInChildren<LootItem>())
                {
                    if (bus.storage != null && !bus.storage.storedLoot.Contains(item))
                    {
                        lootTotal += item.value;
                        Debug.Log($"[GameLoop] Bus child loot: +{item.value} ({item.itemName})");
                    }
                }
            }

            // 4. Final fallback: ContractManager accumulated credits (from each deposit)
            if (ContractManager.Instance != null && ContractManager.Instance.currentCredits > lootTotal)
            {
                Debug.Log($"[GameLoop] Using ContractManager fallback: {ContractManager.Instance.currentCredits}");
                lootTotal = ContractManager.Instance.currentCredits;
            }

            Debug.Log($"[GameLoop] TOTAL to sell: {lootTotal}");
            GarageManager.Instance.AddLootCredits(lootTotal);
        }

        if (CurseManager.Instance != null)
            CurseManager.Instance.ClearCurse();

        // Show sell panel — LoadScene happens in ConfirmSell so the scene transition
        // doesn't interfere with button clicks while the sell panel is open.
        TransitionTo(GameState.Selling);
    }

    public void ConfirmSell()
    {
        if (GarageManager.Instance != null)
            GarageManager.Instance.ConfirmSell();

        var cm = ContractManager.Instance;

        if (cm != null && cm.currentContract != null)
        {
            if (cm.IsQuotaMet())
            {
                // Contract complete — global day +1, back to contract selection
                cm.HandleSuccess();
                TransitionTo(GameState.ContractSelection);
            }
            else if (cm.HasPlanetDaysRemaining())
            {
                // Quota not met but days remain — advance planet day, back to planet selection
                cm.AdvancePlanetDay();
                TransitionTo(GameState.PlanetSelection);
            }
            else
            {
                // Quota not met and no days left — game over
                cm.HandleFailure();
                EndGame();
                return;
            }
        }
        else
        {
            TransitionTo(GameState.Garage);
        }

        if (SceneLoader.Instance == null)
        {
            GameObject sl = new GameObject("SceneLoader");
            sl.AddComponent<SceneLoader>();
        }
        SceneLoader.Instance?.LoadScene("GarageHub");
    }

    public void OpenUpgradeShop()
    {
        TransitionTo(GameState.Upgrading);
    }

    public void CloseUpgradeShop()
    {
        TransitionTo(GameState.Garage);
    }

    public void EndGame()
    {
        TransitionTo(GameState.Result);
        Debug.Log("[GameLoop] Game cycle ended.");
    }

    // ── Legacy alias kept for external callers ───────────────────────────────
    public void SelectContract(Contract contract, bool legacy)
    {
        SelectContract(contract);
    }
}
