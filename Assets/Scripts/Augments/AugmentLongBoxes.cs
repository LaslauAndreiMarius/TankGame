using UnityEngine;

// AUGMENT: Long Boxes. Power-ups last 30 seconds instead of 15.
public class AugmentLongBoxes : AugmentBase
{
    public override string Id => "LongBoxes";
    public override string DisplayName => "Long Boxes";
    public override string CardName => "LONG BOXES";
    public override string Description => "Power-up boxes last twice as long once you collect them.";
    public override string Details => "TYPE: PASSIVE\nPOWER-UPS LAST " + LongBoxesAbility.Seconds + " SECONDS";
    public override int Price => 500;

    public override AugmentAbility CreateAbility() { return new LongBoxesAbility(); }
}

public class LongBoxesAbility : AugmentAbility, IPowerUpListener
{
    public const float Seconds = 30f;

    private PowerUpReceiver receiver;
    private float originalDuration;

    protected override void OnInitialize()
    {
        receiver = Player.GetComponent<PowerUpReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning("Long Boxes: the player has no PowerUpReceiver.");
            return;
        }

        originalDuration = receiver.duration;
        receiver.duration = Seconds;
    }

    public override void Shutdown()
    {
        if (receiver != null) receiver.duration = originalDuration;   // put things back the way they were
    }

    public void OnPowerUpCollected(PowerUpType type)
    {
        PlayUseSound();
    }
}
