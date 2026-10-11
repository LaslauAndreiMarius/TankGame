using UnityEngine;

public class TurretT1 : TurretBase
{
    public override string Id => "T1";
    public override string DisplayName => "T-1";
    public override string Role => "Scatter";
    public override int Price => 400;

    public override int Damage => 6;
    public override float FireInterval => 0.9f;   // seconds between shots
    public override float Range => 7.5f;
    public override float BulletSpeed => 13.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.15f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.2923f);

    public override int Pellets => 5;
    public override float SpreadAngle => 30.0f;
    public override string BulletTypeName => "SCATTER";
}
