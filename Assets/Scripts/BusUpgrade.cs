using UnityEngine;

[CreateAssetMenu(fileName = "NewBusUpgrade", menuName = "GarageHub/BusUpgrade")]
public class BusUpgrade : ScriptableObject
{
    public string upgradeName;
    public string description;
    public int cost;
    public bool isUnlocked = false;

    public enum UpgradeType
    {
        CargoExpansion,
        AutoDoors,
        HeaterSystem,
        Generator,
        Surveillance,
        MedicalStation
    }

    public UpgradeType type;
}
