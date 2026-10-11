using UnityEngine;

public class HullPZ1 : HullBase
{
    public override string Id => "PZ1";
    public override string DisplayName => "PZ-1";
    public override string Role => "Starter";
    public override int Price => 0;

    public override float MoveSpeed => 5.0f;
    public override float TurnSpeed => 180.0f;
    public override int MaxHealth => 100;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(0.0125f, 0.075f);
}
