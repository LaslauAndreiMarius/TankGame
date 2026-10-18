using System;
using System.Collections.Generic;
using UnityEngine;

// Which augments the player owns and which one is equipped (only one at a time). Saved with PlayerPrefs.
// Augments are paid for with the same coins as hulls and turrets.
public static class AugmentProfile
{
    [Serializable]
    private class SaveData
    {
        public List<string> owned = new List<string>();
        public string equipped = "";
    }

    private const string Key = "TankArena.Augments";
    private static SaveData data;

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

    public static AugmentBase Equipped { get { return AugmentCatalog.Get(Data.equipped); } }

    public static bool Owns(string id) { return Data.owned.Contains(id); }

    public static bool TryBuy(AugmentBase augment)
    {
        if (Owns(augment.Id) || PlayerProfile.Coins < augment.Price) return false;

        Data.owned.Add(augment.Id);
        PlayerProfile.AddCoins(-augment.Price);   // also refreshes anything showing the coins
        Save();
        return true;
    }

    // Pass null to take the augment off
    public static bool Equip(AugmentBase augment)
    {
        if (augment != null && !Owns(augment.Id)) return false;

        Data.equipped = augment != null ? augment.Id : "";
        Save();
        return true;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(Key);
        data = null;
        if (Changed != null) Changed();
    }

    public static void UnlockAll()
    {
        foreach (AugmentBase a in AugmentCatalog.All)
            if (!Data.owned.Contains(a.Id)) Data.owned.Add(a.Id);
        Save();
    }

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
    }
}
