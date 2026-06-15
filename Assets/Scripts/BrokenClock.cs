using UnityEngine;

public class BrokenClock : LootItem
{
    void Awake()
    {
        itemName = "Broken Clock";
        value = 500;
        curseValue = 15f;
        curseDescription = "Accelerates in-game time.";
    }

    public override void ApplyCurse()
    {
        Debug.Log("Curse Applied: In-game time accelerated.");
    }

    public override void RemoveCurse()
    {
        Debug.Log("Curse Removed: Time flow normalized.");
    }
}
