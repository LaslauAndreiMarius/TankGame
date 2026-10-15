using UnityEngine;

public enum GameMode
{
    Classic,   // a few enemies at a time, spawning out of sight
    Mayhem     // endless waves around you, stronger tanks every wave
}

// The mode picked in the menu. It is remembered, so the menu starts on the mode you played last.
public static class GameSettings
{
    private const string Key = "TankArena.GameMode";
    private static bool loaded;
    private static GameMode mode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        loaded = false;
    }

    public static GameMode Mode
    {
        get
        {
            if (!loaded)
            {
                mode = (GameMode)Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, 1);
                loaded = true;
            }
            return mode;
        }
        set
        {
            mode = value;
            loaded = true;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static string DisplayName(GameMode m)
    {
        return m == GameMode.Mayhem ? "MAYHEM" : "CLASSIC";
    }

    public static string Description(GameMode m)
    {
        return m == GameMode.Mayhem
            ? "Endless waves close in from every side. Each wave brings better hulls and turrets."
            : "A few enemies at a time, spawning out of sight. Survive, collect coins, upgrade.";
    }
}
