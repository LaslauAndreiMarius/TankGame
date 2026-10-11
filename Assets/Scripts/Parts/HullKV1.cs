using UnityEngine;

public class HullKV1 : HullBase
{
    public override string Id => "KV1";
    public override string DisplayName => "KV-1";
    public override string Role => "Heavy";
    public override int Price => 1900;

    public override float MoveSpeed => 3.6f;
    public override float TurnSpeed => 115.0f;
    public override int MaxHealth => 260;

    // Where this hull's turret ring is, in world units from the center of the sprite
    public override Vector2 TurretMount => new Vector2(0.0125f, 0.05f);
}
