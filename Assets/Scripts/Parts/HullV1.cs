using UnityEngine;

public class HullV1 : HullBase
{
    public override string Id => "V1";
    public override string DisplayName => "V-1";
    public override string Role => "Scout";
    public override int Price => 150;

    public override float MoveSpeed => 6.4f;
    public override float TurnSpeed => 230.0f;
    public override int MaxHealth => 70;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(-0.0125f, 0.05f);
}
