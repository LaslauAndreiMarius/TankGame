using UnityEngine;

public class TurretKR1 : TurretBase
{
    public override string Id => "KR1";
    public override string DisplayName => "KR-1";
    public override string Role => "Tesla";
    public override int Price => 1700;

    public override int Damage => 18;
    public override float FireInterval => 0.8f;   // seconds between shots
    public override float Range => 10.0f;
    public override float BulletSpeed => 12.0f;

    // Where the barrel tip is (distance from the turret's pivot) and where the pivot sits in the sprite (0-1)
    public override float MuzzleLength => 1.25f;
    public override Vector2 SpritePivot => new Vector2(0.5f, 0.3333f);

    public override BulletKind Kind => BulletKind.Electric;
    public override float AreaRadius => 2.4f;
    public override string BulletTypeName => "ELECTRIC";
}
