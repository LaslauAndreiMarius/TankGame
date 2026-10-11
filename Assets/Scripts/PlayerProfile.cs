using System;
using System.Collections.Generic;
using UnityEngine;

// Everything the player keeps between runs: coins, owned parts, and the tank they built.
// Saved with PlayerPrefs, so it survives closing the game.
public static class PlayerProfile
{
    [Serializable]
    private class SaveData
    {
        public int coins;
        public List<string> ownedHulls = new List<string>();
        public List<string> ownedTurrets = new List<string>();
        public string equippedHull;
        public string equippedTurret;
    }

    private const string Key = "TankArena.Profile";
    private static SaveData data;

    // Fired whenever anything changes (UI listens to this to refresh itself)
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        data = null;
        Changed = null;
    }

    static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    // ---- Coins ----
    public static int Coins { get { return Data.coins; } }

    public static void AddCoins(int amount)
    {
        Data.coins = Mathf.Max(0, Data.coins + amount);
        Save();
    }

    // ---- Owned and equipped parts ----
    public static bool OwnsHull(string id) { return Data.ownedHulls.Contains(id); }
    public static bool OwnsTurret(string id) { return Data.ownedTurrets.Contains(id); }

    public static HullBase EquippedHull { get { return TankCatalog.GetHull(Data.equippedHull); } }
    public static TurretBase EquippedTurret { get { return TankCatalog.GetTurret(Data.equippedTurret); } }

    public static bool TryBuyHull(HullBase hull)
    {
        if (OwnsHull(hull.Id) || Data.coins < hull.Price) return false;
        Data.coins -= hull.Price;
        Data.ownedHulls.Add(hull.Id);
        Save();
        return true;
    }

    public static bool TryBuyTurret(TurretBase turret)
    {
        if (OwnsTurret(turret.Id) || Data.coins < turret.Price) return false;
        Data.coins -= turret.Price;
        Data.ownedTurrets.Add(turret.Id);
        Save();
        return true;
    }

    public static bool EquipHull(HullBase hull)
    {
        if (!OwnsHull(hull.Id)) return false;
        Data.equippedHull = hull.Id;
        Save();
        return true;
    }

    public static bool EquipTurret(TurretBase turret)
    {
        if (!OwnsTurret(turret.Id)) return false;
        Data.equippedTurret = turret.Id;
        Save();
        return true;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(Key);
        data = null;
        if (Changed != null) Changed();
    }

    // ---- Saving and loading ----
    static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }

    static void Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
        if (data == null) data = new SaveData();

        // Everyone owns and uses the free starter parts
        HullBase starterHull = TankCatalog.StarterHull;
        TurretBase starterTurret = TankCatalog.StarterTurret;

        if (starterHull != null && !data.ownedHulls.Contains(starterHull.Id)) data.ownedHulls.Add(starterHull.Id);
        if (starterTurret != null && !data.ownedTurrets.Contains(starterTurret.Id)) data.ownedTurrets.Add(starterTurret.Id);

        if (TankCatalog.GetHull(data.equippedHull) == null || !data.ownedHulls.Contains(data.equippedHull))
            data.equippedHull = starterHull != null ? starterHull.Id : "";
        if (TankCatalog.GetTurret(data.equippedTurret) == null || !data.ownedTurrets.Contains(data.equippedTurret))
            data.equippedTurret = starterTurret != null ? starterTurret.Id : "";
    }
}
