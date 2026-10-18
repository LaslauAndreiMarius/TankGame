// The only place the rest of the game touches augments. A system that wants to be augment-friendly
// calls one of these one-liners; it never needs to know which augments exist.
public static class AugmentHooks
{
    // Health packs: how long should the regeneration take?
    public static float RegenDuration(float duration)
    {
        IHealthPackModifier modifier = AugmentController.Get<IHealthPackModifier>();
        return modifier != null ? modifier.ModifyRegenDuration(duration) : duration;
    }

    public static void HealthPackCollected(float amount)
    {
        IHealthPackModifier modifier = AugmentController.Get<IHealthPackModifier>();
        if (modifier != null) modifier.OnHealthPackCollected(amount);
    }

    // Power-up boxes: the player just collected one
    public static void PowerUpCollected(PowerUpType type)
    {
        IPowerUpListener listener = AugmentController.Get<IPowerUpListener>();
        if (listener != null) listener.OnPowerUpCollected(type);
    }
}
