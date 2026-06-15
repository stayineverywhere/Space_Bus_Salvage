using UnityEngine;

public abstract class LootItem : MonoBehaviour
{
    [Header("Loot Data")]
    public string itemName;
    public int value;
    public float curseValue = 10f; // Amount added to Global Curse Meter
    public string curseDescription;

    public abstract void ApplyCurse();
    public abstract void RemoveCurse();
}
