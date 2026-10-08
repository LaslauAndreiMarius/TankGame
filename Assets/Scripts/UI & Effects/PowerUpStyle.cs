using UnityEngine;

// One place that decides how each power-up looks (colors and names), so the boxes,
// the HUD cards and the glow around the tank always agree.
public static class PowerUpStyle
{
    public static Color ColorFor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.DoubleDamage: return new Color32(222, 66, 56, 255);    // red
            case PowerUpType.DoubleArmor:  return new Color32(72, 140, 235, 255);   // blue
            default:                       return new Color32(245, 200, 50, 255);   // yellow (speed)
        }
    }

    public static string LabelFor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.DoubleDamage: return "DAMAGE x2";
            case PowerUpType.DoubleArmor:  return "ARMOR x2";
            default:                       return "SPEED x2";
        }
    }
}
