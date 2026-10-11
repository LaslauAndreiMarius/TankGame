using UnityEngine;

public class TurretEM1 : TurretBase
{
    public override string Id => "EM1";
    public override string DisplayName => "EM-1";
    public override string Role => "Siege";
    public override int Price => 3000;

    public override int Damage => 56;
    public override float FireInterval => 1.6f;   // seconds between shots
    public override float Range => 15.0f;
    public override float BulletSpeed => 12.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.3f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.2973f);

    public override BulletKind Kind => BulletKind.Explosive;
    public override float AreaRadius => 2.8f;
    public override string BulletTypeName => "HEAVY SHELL";
}
