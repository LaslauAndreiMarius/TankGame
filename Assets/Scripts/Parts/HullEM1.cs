using UnityEngine;

public class HullEM1 : HullBase
{
    public override string Id => "EM1";
    public override string DisplayName => "EM-1";
    public override string Role => "Fortress";
    public override int Price => 2600;

    public override float MoveSpeed => 3.2f;
    public override float TurnSpeed => 100.0f;
    public override int MaxHealth => 320;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(0.0125f, 0.05f);
}
