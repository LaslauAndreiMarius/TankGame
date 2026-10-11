using UnityEngine;

public class HullKR1 : HullBase
{
    public override string Id => "KR1";
    public override string DisplayName => "KR-1";
    public override string Role => "Assault";
    public override int Price => 1400;

    public override float MoveSpeed => 4.2f;
    public override float TurnSpeed => 140.0f;
    public override int MaxHealth => 200;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(-0.0625f, 0.15f);
}
