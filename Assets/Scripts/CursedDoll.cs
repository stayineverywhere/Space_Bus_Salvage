using UnityEngine;

public class CursedDoll : LootItem
{
    void Awake()
    {
        itemName = "Cursed Doll";
        value = 600;
        curseValue = 25f; // Significant curse contribution
        curseDescription = "Increases monster spawn rate while carried.";
    }

    public override void ApplyCurse()
    {
        Debug.Log("Curse Applied: Monster spawn rate increased.");
    }

    public override void RemoveCurse()
    {
        Debug.Log("Curse Removed: Monster spawn rate normalized.");
    }
}
