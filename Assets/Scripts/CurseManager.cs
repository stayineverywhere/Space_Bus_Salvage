using UnityEngine;
using UnityEngine.Events;

public class CurseManager : MonoBehaviour
{
    private static CurseManager _instance;
    public static CurseManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<CurseManager>();
            return _instance;
        }
    }

    [Header("Curse Meter")]
    public float globalCurseValue;
    public float maxCurseValue = 100f;

    [Header("Scaling")]
    public float difficultyMultiplier = 1f;
    public float spawnRateMultiplier  = 1f;
    public float busInstabilityMultiplier = 1f;

    [Header("Events")]
    public UnityEvent onCurseThresholdReached;

    private bool hasTriggeredAnomaly;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCurse(float amount)
    {
        globalCurseValue = Mathf.Min(globalCurseValue + amount, maxCurseValue);
        UpdateMultipliers();
        ApplyVisualDistortion();
        CheckThreshold();
        Debug.Log($"[Curse] {globalCurseValue:F0}/{maxCurseValue} | Difficulty x{difficultyMultiplier:F2}");
    }

    public void ClearCurse()
    {
        globalCurseValue = 0f;
        hasTriggeredAnomaly = false;
        UpdateMultipliers();
        ApplyVisualDistortion();
    }

    private void UpdateMultipliers()
    {
        float r = globalCurseValue / maxCurseValue;
        difficultyMultiplier     = 1f + r * 1.5f;
        spawnRateMultiplier      = 1f + r * 2f;
        busInstabilityMultiplier = 1f + r * 1f;
    }

    private void ApplyVisualDistortion()
    {
        // Increase monster spawn scaling through MonsterManager if present
        MonsterManager mm = Object.FindAnyObjectByType<MonsterManager>();
        // MonsterManager can read spawnRateMultiplier when it spawns

        // Bus instability
        BusPowerSystem bps = Object.FindAnyObjectByType<BusPowerSystem>();
        if (bps != null)
        {
            float ratio = globalCurseValue / maxCurseValue;
            bps.consumptionRate = 0.5f + ratio * 1.5f;
        }
    }

    private void CheckThreshold()
    {
        if (globalCurseValue >= maxCurseValue && !hasTriggeredAnomaly)
        {
            hasTriggeredAnomaly = true;
            TriggerAnomaly();
        }
    }

    private void TriggerAnomaly()
    {
        Debug.LogWarning("[Curse] ANOMALY TRIGGERED — max curse reached!");
        onCurseThresholdReached?.Invoke();
        ScreenShakeManager.Instance?.Shake(1.5f, 0.3f);
        HUDManager.Instance?.TriggerDamageFlash();
    }
}
