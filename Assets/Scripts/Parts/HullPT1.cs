using UnityEngine;

public class HullPT1 : HullBase
{
    public override string Id => "PT1";
    public override string DisplayName => "PT-1";
    public override string Role => "Hunter";
    public override int Price => 450;

    public override float MoveSpeed => 5.0f;
    public override float TurnSpeed => 150.0f;
    public override int MaxHealth => 130;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(-0.0125f, 0.075f);
}
