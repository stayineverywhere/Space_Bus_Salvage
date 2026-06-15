using UnityEngine;

[CreateAssetMenu(fileName = "NewContract", menuName = "ContractSystem/Contract")]
public class Contract : ScriptableObject
{
    public string contractName;
    public int creditGoal = 5000;
    public int survivorBonusGoal = 2;
    public int durationDays = 3;

    [Header("Penalties")]
    public int debtIncreaseOnFailure = 1000;
    public float reputationLossOnFailure = 0.2f;

    [Header("Rewards")]
    public string planetToUnlock;
    public string busUpgradeName;
}
