using UnityEngine;

public class TurretV1 : TurretBase
{
    public override string Id => "V1";
    public override string DisplayName => "V-1";
    public override string Role => "Rapid fire";
    public override int Price => 200;

    public override int Damage => 5;
    public override float FireInterval => 0.12f;   // seconds between shots
    public override float Range => 9.0f;
    public override float BulletSpeed => 16.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.1875f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.2692f);

    public override string BulletTypeName => "TRACER";
}
