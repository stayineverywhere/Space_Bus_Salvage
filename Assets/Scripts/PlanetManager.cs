using UnityEngine;
using System.Collections.Generic;

public class PlanetManager : MonoBehaviour
{
    private static PlanetManager _instance;
    public static PlanetManager Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<PlanetManager>();
            return _instance;
        }
    }

    public string activePlanetName { get; private set; }
    public HorrorVisualPreset.PlanetTheme activeTheme { get; private set; }
    public int activeDifficulty { get; private set; }

    public List<PlanetData> registeredPlanets = new List<PlanetData>();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlanet(PlanetData planet)
    {
        if (!registeredPlanets.Contains(planet))
            registeredPlanets.Add(planet);
    }

    // Called by GameLoopManager when deploying to a planet
    public void SetActivePlanet(string name, HorrorVisualPreset.PlanetTheme theme, int difficulty)
    {
        activePlanetName = name;
        activeTheme = theme;
        activeDifficulty = difficulty;

        Debug.Log($"[Planet] Active: {name} | Theme: {theme} | Difficulty: {difficulty}");

        if (HorrorVisualPreset.Instance != null)
        {
            HorrorVisualPreset.Instance.currentTheme = theme;
            HorrorVisualPreset.Instance.ApplyHorrorVisuals();
        }
    }

    // Legacy overload for PlanetData-based callers
    public void SelectPlanet(PlanetData planet)
    {
        if (planet == null) return;
        SetActivePlanet(planet.planetName, planet.visualTheme, planet.difficulty);
    }
}
