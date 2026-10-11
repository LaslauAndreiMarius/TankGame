using UnityEngine;

public class TurretPT1 : TurretBase
{
    public override string Id => "PT1";
    public override string DisplayName => "PT-1";
    public override string Role => "Piercer";
    public override int Price => 650;

    public override int Damage => 24;
    public override float FireInterval => 0.9f;   // seconds between shots
    public override float Range => 16.0f;
    public override float BulletSpeed => 22.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.2375f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.2721f);

    public override BulletKind Kind => BulletKind.Piercing;
    public override int Pierce => 2;
    public override string BulletTypeName => "PIERCING";
}
