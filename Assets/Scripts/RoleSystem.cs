using UnityEngine;

public class RoleSystem : MonoBehaviour
{
    public enum PlayerRole { Driver, Scavenger, Lookout }
    public PlayerRole currentRole;

    public void AssignRole(PlayerRole role)
    {
        currentRole = role;
        Debug.Log($"Role Assigned: {role}");
        ApplyRoleBuffs();
    }

    private void ApplyRoleBuffs()
    {
        switch (currentRole)
        {
            case PlayerRole.Driver:
                // Buff bus handling?
                break;
            case PlayerRole.Scavenger:
                // Buff interaction speed?
                break;
            case PlayerRole.Lookout:
                // Buff detection range?
                break;
        }
    }
}
