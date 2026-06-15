using UnityEngine;
using System.Collections.Generic;

public class BusStorage : MonoBehaviour
{
    public List<LootItem> storedLoot = new List<LootItem>();
    public int maxCapacity = 20;

    public bool StoreItem(LootItem item)
    {
        if (storedLoot.Count < maxCapacity)
        {
            storedLoot.Add(item);
            Debug.Log($"Stored {item.itemName} in Bus Storage.");
            return true;
        }
        Debug.Log("Bus Storage is full!");
        return false;
    }

    public int GetTotalValue()
    {
        int total = 0;
        foreach (var item in storedLoot) total += item.value;
        return total;
    }
}
