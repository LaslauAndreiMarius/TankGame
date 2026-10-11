using UnityEngine;

public class TurretPZ1 : TurretBase
{
    public override string Id => "PZ1";
    public override string DisplayName => "PZ-1";
    public override string Role => "Rookie";
    public override int Price => 0;

    public override int Damage => 10;
    public override float FireInterval => 0.4f;   // seconds between shots
    public override float Range => 11.0f;
    public override float BulletSpeed => 14.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.175f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.254f);

    public override string BulletTypeName => "SHELL";
}
