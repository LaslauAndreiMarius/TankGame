using UnityEngine;

// AUGMENT: Rapid Fire. Press E to shoot much faster for a short time, then wait for it to recharge.
public class AugmentRapidFire : AugmentBase
{
    public override string Id => "RapidFire";
    public override string DisplayName => "Rapid Fire";
    public override string CardName => "RAPID FIRE";
    public override string Description => "Press E to fire four times faster for a few seconds. Then it has to recharge.";
    public override string Details => "TYPE: PRESS E\nLASTS " + RapidFireAbility.DurationSeconds + " S    RECHARGE " + RapidFireAbility.CooldownSeconds + " S";
    public override int Price => 1500;

    public override AugmentAbility CreateAbility() { return new RapidFireAbility(); }
}

public class RapidFireAbility : TimedAbility, IScreenEffect
{
    public const float DurationSeconds = 6f;
    public const float CooldownSeconds = 90f;
    public const float IntervalScale = 0.25f;   // the time between shots is multiplied by this

    protected override float Duration { get { return DurationSeconds; } }
    protected override float Cooldown { get { return CooldownSeconds; } }

    private Weapon weapon;
    private float originalScale = 1f;

    protected override void OnActivated()
    {
        weapon = Player.GetComponent<Weapon>();
        if (weapon == null) return;

        originalScale = weapon.fireIntervalScale;
        weapon.fireIntervalScale = originalScale * IntervalScale;
    }

    protected override void OnEnded()
    {
        if (weapon != null) weapon.fireIntervalScale = originalScale;
    }

    // ---- IScreenEffect: a fast pulsing orange glow while it is on ----
    public Color ScreenColor { get { return new Color(1f, 0.55f, 0.1f); } }

    public float ScreenIntensity
    {
        get
        {
            if (!IsActive) return 0f;

            float fadeIn = Mathf.Clamp01((DurationSeconds - SecondsLeft) / 0.2f);
            float fadeOut = Mathf.Clamp01(SecondsLeft / 0.5f);
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 18f);
            return Mathf.Min(fadeIn, fadeOut) * pulse;
        }
    }
}
