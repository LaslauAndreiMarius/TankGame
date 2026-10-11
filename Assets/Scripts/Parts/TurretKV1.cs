using UnityEngine;

public class TurretKV1 : TurretBase
{
    public override string Id => "KV1";
    public override string DisplayName => "KV-1";
    public override string Role => "Railgun";
    public override int Price => 2300;

    public override int Damage => 60;
    public override float FireInterval => 1.5f;   // seconds between shots
    public override float Range => 24.0f;
    public override float BulletSpeed => 30.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.2375f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.25f);

    public override BulletKind Kind => BulletKind.Piercing;
    public override int Pierce => 4;
    public override string BulletTypeName => "RAIL SLUG";
}
