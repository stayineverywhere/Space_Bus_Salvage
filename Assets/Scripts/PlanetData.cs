using UnityEngine;

public class PlanetData : MonoBehaviour
{
    public PlanetType type;
    public HorrorVisualPreset.PlanetTheme visualTheme;
    [Range(1, 5)]
    public int difficulty = 1;
    public string planetName;

    [Header("Zone Settings")]
    public float effectRadius = 50f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
