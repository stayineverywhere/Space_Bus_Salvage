using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerUpgrade", menuName = "GarageHub/PlayerUpgrade")]
public class PlayerUpgrade : ScriptableObject
{
    public string upgradeName;
    public string description;
    public int cost;
    public int level = 0;
    public int maxLevel = 5;

    public enum UpgradeType
    {
        Exploration,
        Repair,
        Combat,
        Driving
    }

    public UpgradeType type;
}
