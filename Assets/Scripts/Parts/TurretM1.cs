using UnityEngine;

public class TurretM1 : TurretBase
{
    public override string Id => "M1";
    public override string DisplayName => "M-1";
    public override string Role => "Burst";
    public override int Price => 1250;

    public override int Damage => 9;
    public override float FireInterval => 0.95f;   // seconds between shots
    public override float Range => 12.0f;
    public override float BulletSpeed => 18.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.275f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.2917f);

    public override int BurstCount => 3;
    public override float BurstDelay => 0.08f;
    public override string BulletTypeName => "BURST";
}
