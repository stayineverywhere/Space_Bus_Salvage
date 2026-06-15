using UnityEngine;

/// <summary>
/// Legacy scene initializer. Deprecated in favor of the automated GameBootstrapper.
/// Immediately self-destructs to prevent duplicate singleton initialization.
/// </summary>
public class GameStartInitializer : MonoBehaviour
{
    public static GameStartInitializer Instance { get; private set; }

    private void Awake()
    {
        Destroy(gameObject);
    }
}
