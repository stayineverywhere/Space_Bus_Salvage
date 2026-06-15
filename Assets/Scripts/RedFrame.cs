using UnityEngine;

public class RedFrame : LootItem
{
    void Awake()
    {
        itemName = "Red Frame";
        value = 800;
        curseValue = 35f; // Highest curse contribution
        curseDescription = "Reduces bus power stability.";
    }

    public override void ApplyCurse()
    {
        Debug.Log("Curse Applied: Bus power stability reduced.");
    }

    public override void RemoveCurse()
    {
        Debug.Log("Curse Removed: Bus power stabilized.");
    }
}
