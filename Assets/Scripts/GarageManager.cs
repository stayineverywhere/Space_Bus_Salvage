using UnityEngine;
using System.Collections.Generic;

public class GarageManager : MonoBehaviour
{
    private static GarageManager _instance;
    public static GarageManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<GarageManager>();
            return _instance;
        }
    }

    [Header("Currency")]
    public int totalCredits;
    public int pendingSellAmount;

    [Header("Shop Inventory")]
    public List<BusUpgrade> availableBusUpgrades = new List<BusUpgrade>();
    public List<PlayerUpgrade> availablePlayerUpgrades = new List<PlayerUpgrade>();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called when player returns from exploration — stages the sell amount for display
    public void AddLootCredits(int amount)
    {
        pendingSellAmount = amount;
        Debug.Log($"[Garage] Pending sell: {pendingSellAmount} credits.");
    }

    // Called after player confirms sell on the sell screen
    public void ConfirmSell()
    {
        totalCredits += pendingSellAmount;
        Debug.Log($"[Garage] Sold! Bank: {totalCredits} credits.");
        pendingSellAmount = 0;

        // Also clear bus storage when selling!
        BusController bus = Object.FindAnyObjectByType<BusController>();
        if (bus != null && bus.storage != null)
        {
            foreach (var item in bus.storage.storedLoot)
            {
                if (item != null) Destroy(item.gameObject);
            }
            bus.storage.storedLoot.Clear();
        }

    }

    public void AddCredits(int amount)
    {
        totalCredits += amount;
        Debug.Log($"[Garage] Bank: {totalCredits}");
    }

    public bool BuyBusUpgrade(BusUpgrade upgrade)
    {
        if (upgrade.isUnlocked || totalCredits < upgrade.cost)
        {
            Debug.Log("[Garage] Cannot purchase bus upgrade.");
            return false;
        }
        totalCredits -= upgrade.cost;
        upgrade.isUnlocked = true;
        Debug.Log($"[Garage] Purchased {upgrade.upgradeName} for {upgrade.cost}.");
        ApplyBusUpgrade(upgrade);
        return true;
    }

    public bool BuyPlayerUpgrade(PlayerUpgrade upgrade)
    {
        if (upgrade.level >= upgrade.maxLevel || totalCredits < upgrade.cost)
        {
            Debug.Log("[Garage] Cannot purchase player upgrade.");
            return false;
        }
        totalCredits -= upgrade.cost;
        upgrade.level++;
        Debug.Log($"[Garage] {upgrade.upgradeName} → Lv{upgrade.level}.");
        ApplyPlayerUpgrade(upgrade);
        return true;
    }

    private void ApplyBusUpgrade(BusUpgrade upgrade)
    {
        BusController bus = Object.FindAnyObjectByType<BusController>();
        if (bus == null) return;

        switch (upgrade.upgradeName)
        {
            case "Hull Reinforcement":
                bus.maxHealth += 500f;
                bus.currentHealth = Mathf.Min(bus.currentHealth + 500f, bus.maxHealth);
                break;
            case "Power Cell":
                if (bus.powerSystem != null) bus.powerSystem.maxPower += 50f;
                break;
        }
    }

    private void ApplyPlayerUpgrade(PlayerUpgrade upgrade)
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;

        switch (upgrade.upgradeName)
        {
            case "Speed Boots":
                player.speed += 1f;
                break;
            case "Stamina Pack":
                player.maxStamina += 25f;
                player.stamina = player.maxStamina;
                break;
            case "O2 Tank":
                player.maxOxygen += 25f;
                player.oxygen = player.maxOxygen;
                break;
        }
    }
}
