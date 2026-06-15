using UnityEngine;
using System.Collections.Generic;

public class ContractManager : MonoBehaviour
{
    private static ContractManager _instance;
    public static ContractManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<ContractManager>();
            return _instance;
        }
    }

    [Header("Active Progress")]
    public Contract currentContract;
    public int currentCredits;
    public int currentSurvivorsRescued;
    public int currentDay = 1;

    [Header("Global Stats")]
    public int globalDay = 1;
    public int totalDebt;
    public float reputation = 1f;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetContract(Contract contract)
    {
        currentContract = contract;
        ResetProgress();
        Debug.Log($"[Contract] Active: {contract?.contractName}");
    }

    public void AddCredits(int amount)
    {
        currentCredits += amount;
        string goal = currentContract != null ? $"/{currentContract.creditGoal}" : "";
        Debug.Log($"[Contract] Credits: {currentCredits}{goal}");
    }

    public void RescueSurvivor()
    {
        currentSurvivorsRescued++;
        string goal = currentContract != null ? $"/{currentContract.survivorBonusGoal}" : "";
        Debug.Log($"[Contract] Survivors: {currentSurvivorsRescued}{goal}");
    }

    public void HandleSuccess()
    {
        Debug.Log("[Contract] SUCCESS!");
        globalDay++;
        reputation = Mathf.Min(2f, reputation + 0.1f);
        currentContract = null;
        ResetProgress();
    }

    public void HandleFailure()
    {
        Debug.Log("[Contract] FAILURE!");
        if (currentContract != null)
        {
            totalDebt += currentContract.debtIncreaseOnFailure;
            reputation = Mathf.Max(0f, reputation - currentContract.reputationLossOnFailure);
        }
        currentContract = null;
        ResetProgress();
    }

    public void AdvancePlanetDay()
    {
        currentDay++;
        Debug.Log($"[Contract] Planet Day {currentDay}/{currentContract?.durationDays}");
    }

    public bool IsQuotaMet()
    {
        return currentContract != null && currentCredits >= currentContract.creditGoal;
    }

    public bool HasPlanetDaysRemaining()
    {
        return currentContract != null && currentDay < currentContract.durationDays;
    }

    public void ResetProgress()
    {
        currentDay = 1;
        currentCredits = 0;
        currentSurvivorsRescued = 0;
    }
}
