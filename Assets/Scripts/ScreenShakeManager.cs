using UnityEngine;
using System.Collections;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Shake(float duration, float magnitude)
    {
        if (Camera.main == null) return;
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;

        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Camera.main.transform.localPosition = originalPos +
                new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (Camera.main != null)
            Camera.main.transform.localPosition = originalPos;
    }
}
