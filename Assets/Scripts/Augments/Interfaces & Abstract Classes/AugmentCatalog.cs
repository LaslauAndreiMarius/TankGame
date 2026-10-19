using System.Collections.Generic;
using UnityEngine;

// The list of every augment in the game, loaded from Assets/Resources/Augments.
public static class AugmentCatalog
{
    private static List<AugmentBase> augments;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        augments = null;
    }

    public static IReadOnlyList<AugmentBase> All
    {
        get
        {
            Load();
            return augments;
        }
    }

    public static AugmentBase Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Load();
        foreach (AugmentBase a in augments) if (a.Id == id) return a;
        return null;
    }

    static void Load()
    {
        if (augments != null) return;

        augments = new List<AugmentBase>(Resources.LoadAll<AugmentBase>("Augments"));
        augments.Sort((a, b) => a.Price.CompareTo(b.Price));

        // An augment without its picture can't be shown in the garage
        int hidden = augments.RemoveAll(a => a.icon == null);
        if (hidden > 0)
            Debug.LogWarning(hidden + " augment(s) are hidden because their icon is missing. Run Tools > Tank Game > Setup Augments.");
    }
}
