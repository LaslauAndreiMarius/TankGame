using UnityEngine;

public class HullM1 : HullBase
{
    public override string Id => "M1";
    public override string DisplayName => "M-1";
    public override string Role => "Battle";
    public override int Price => 1000;

    public override float MoveSpeed => 4.6f;
    public override float TurnSpeed => 150.0f;
    public override int MaxHealth => 175;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(0.1125f, 0.15f);
}
