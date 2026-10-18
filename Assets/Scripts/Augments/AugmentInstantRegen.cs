using UnityEngine;

// AUGMENT: Instant Regen. Health packs heal you at once instead of over several seconds.
public class AugmentInstantRegen : AugmentBase
{
    public override string Id => "InstantRegen";
    public override string DisplayName => "Instant Regen";
    public override string CardName => "REGEN";
    public override string Description => "Health packs heal you instantly instead of over several seconds.";
    public override string Details => "TYPE: PASSIVE\nWORKS WHEN YOU COLLECT A HEALTH PACK";
    public override int Price => 600;

    public override AugmentAbility CreateAbility() { return new InstantRegenAbility(); }
}

public class InstantRegenAbility : AugmentAbility, IHealthPackModifier, IScreenEffect
{
    private float flashStart = -10f;

    // ---- IHealthPackModifier ----
    public float ModifyRegenDuration(float duration)
    {
        return 0f;   // no waiting
    }

    public void OnHealthPackCollected(float amount)
    {
        flashStart = Time.time;
        PlayUseSound();
    }

    // ---- IScreenEffect: a short green flash when a pack is collected ----
    public Color ScreenColor { get { return new Color(0.25f, 1f, 0.4f); } }
    public float ScreenIntensity { get { return Mathf.Clamp01(1f - (Time.time - flashStart) / 0.6f) * 0.6f; } }
}
