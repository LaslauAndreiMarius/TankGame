using UnityEngine;

public class HullL21 : HullBase
{
    public override string Id => "L21";
    public override string DisplayName => "L-21";
    public override string Role => "Medium";
    public override int Price => 700;

    public override float MoveSpeed => 4.8f;
    public override float TurnSpeed => 170.0f;
    public override int MaxHealth => 150;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(0.0125f, 0.075f);
}
