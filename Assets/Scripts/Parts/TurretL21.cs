using UnityEngine;

public class TurretL21 : TurretBase
{
    public override string Id => "L21";
    public override string DisplayName => "L-21";
    public override string Role => "Grenadier";
    public override int Price => 900;

    public override int Damage => 26;
    public override float FireInterval => 1.1f;   // seconds between shots
    public override float Range => 13.0f;
    public override float BulletSpeed => 11.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.225f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.3099f);

    public override BulletKind Kind => BulletKind.Explosive;
    public override float AreaRadius => 1.8f;
    public override string BulletTypeName => "EXPLOSIVE";
}
