using UnityEngine;

public class HullT1 : HullBase
{
    public override string Id => "T1";
    public override string DisplayName => "T-1";
    public override string Role => "Balanced";
    public override int Price => 300;

    public override float MoveSpeed => 5.4f;
    public override float TurnSpeed => 190.0f;
    public override int MaxHealth => 115;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(-0.0125f, 0.05f);
}
