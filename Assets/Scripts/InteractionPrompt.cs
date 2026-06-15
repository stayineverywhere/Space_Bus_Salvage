using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    public static InteractionPrompt Instance { get; private set; }
    public GameObject promptUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowPrompt(bool show)
    {
        if (promptUI != null) promptUI.SetActive(show);
    }
}
