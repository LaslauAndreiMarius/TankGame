using System.Collections.Generic;
using UnityEngine;

// The list of every hull and turret in the game. They are loaded from Assets/Resources/TankParts.
public static class TankCatalog
{
    private static List<HullBase> hulls;
    private static List<TurretBase> turrets;
    private static float maxMoveSpeed, maxTurnSpeed, maxHealth;
    private static float maxDamage, maxShotsPerSecond, maxRange;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        hulls = null;
        turrets = null;
    }

    public static IReadOnlyList<HullBase> Hulls { get { Load(); return hulls; } }
    public static IReadOnlyList<TurretBase> Turrets { get { Load(); return turrets; } }

    // The biggest value of each stat, so the garage can draw bars relative to the best part
    public static float MaxMoveSpeed { get { Load(); return maxMoveSpeed; } }
    public static float MaxTurnSpeed { get { Load(); return maxTurnSpeed; } }
    public static float MaxHealth { get { Load(); return maxHealth; } }
    public static float MaxDamage { get { Load(); return maxDamage; } }
    public static float MaxShotsPerSecond { get { Load(); return maxShotsPerSecond; } }
    public static float MaxRange { get { Load(); return maxRange; } }

    // The free parts every new player starts with
    public static HullBase StarterHull
    {
        get
        {
            Load();
            return hulls.Count > 0 ? hulls[0] : null;   // the list is sorted by price, so the cheapest is first
        }
    }

    public static TurretBase StarterTurret
    {
        get
        {
            Load();
            return turrets.Count > 0 ? turrets[0] : null;
        }
    }

    public static HullBase GetHull(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Load();
        foreach (HullBase h in hulls) if (h.Id == id) return h;
        return null;
    }

    public static TurretBase GetTurret(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Load();
        foreach (TurretBase t in turrets) if (t.Id == id) return t;
        return null;
    }

    static void Load()
    {
        if (hulls != null) return;

        hulls = new List<HullBase>(Resources.LoadAll<HullBase>("TankParts/Hulls"));
        turrets = new List<TurretBase>(Resources.LoadAll<TurretBase>("TankParts/Turrets"));
        hulls.Sort((a, b) => a.Price.CompareTo(b.Price));
        turrets.Sort((a, b) => a.Price.CompareTo(b.Price));

        // A part whose image is missing can't be shown or used, so it is left out
        int hidden = hulls.RemoveAll(h => h.sprite == null) + turrets.RemoveAll(t => t.sprite == null);
        if (hidden > 0)
        {
            Debug.LogWarning(hidden + " tank part(s) are hidden because their image is missing. " +
                             "Run Tools > Tank Game > Setup Garage Assets to see which ones.");
        }

        maxMoveSpeed = maxTurnSpeed = maxHealth = 1f;
        maxDamage = maxShotsPerSecond = maxRange = 1f;

        foreach (HullBase h in hulls)
        {
            maxMoveSpeed = Mathf.Max(maxMoveSpeed, h.MoveSpeed);
            maxTurnSpeed = Mathf.Max(maxTurnSpeed, h.TurnSpeed);
            maxHealth = Mathf.Max(maxHealth, h.MaxHealth);
        }
        foreach (TurretBase t in turrets)
        {
            maxDamage = Mathf.Max(maxDamage, t.TotalDamagePerShot);
            maxShotsPerSecond = Mathf.Max(maxShotsPerSecond, t.ShotsPerSecond);
            maxRange = Mathf.Max(maxRange, t.Range);
        }
    }
}
