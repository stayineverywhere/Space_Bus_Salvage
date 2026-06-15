using UnityEngine;

public class PlayerModeManager : MonoBehaviour
{
    public static PlayerModeManager Instance { get; private set; }

    public enum GameMode { SinglePlayer, MultiPlayer }
    public GameMode currentGameMode = GameMode.SinglePlayer;

    [Header("Single Player Settings")]
    public float singlePlayerHealth = 200f;
    public GameObject dronePrefab;

    [Header("Multiplayer Settings")]
    public float multiPlayerHealth = 100f;
    public float lootValueMultiplier = 1.2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeMode(int playerCount)
    {
        currentGameMode = (playerCount <= 1) ? GameMode.SinglePlayer : GameMode.MultiPlayer;
        ApplyModeSettings();
    }

    private void ApplyModeSettings()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Note: Actual Health component would be updated here
        if (currentGameMode == GameMode.SinglePlayer)
        {
            Debug.Log("Single Player Mode: Health boosted, spawning drone.");
            if (dronePrefab != null) Instantiate(dronePrefab, player.transform.position + Vector3.up * 2f, Quaternion.identity);
        }
        else
        {
            Debug.Log($"Multiplayer Mode: Loot value multiplier set to {lootValueMultiplier}x.");
        }
    }
}
