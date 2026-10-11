using System.Collections;
using UnityEngine;

// Base class for every turret. Each turret type is a small subclass that fills in its own numbers
// (see TurretKV1.cs for an example) and can override Fire() for special behavior.
public abstract class TurretBase : ScriptableObject
{
    public Sprite sprite;         // assigned by Tools > Tank Game > Setup Garage Assets
    public Sprite bulletSprite;   // the bullet this turret shoots (bullet_<Id>.png)

    public abstract string Id { get; }            // matches the image file name, e.g. "KV1" for KV1_Black_Top.png
    public abstract string DisplayName { get; }
    public abstract string Role { get; }
    public abstract int Price { get; }

    public abstract int Damage { get; }           // per bullet
    public abstract float FireInterval { get; }   // seconds between shots
    public abstract float Range { get; }          // how far bullets fly (world units)
    public abstract float BulletSpeed { get; }

    public abstract float MuzzleLength { get; }   // distance from the turret's pivot to the end of the barrel
    public abstract Vector2 SpritePivot { get; }  // where the turret turns, inside its sprite (0-1)

    // ---- Optional behavior: subclasses override only what makes them special ----
    public virtual BulletKind Kind { get { return BulletKind.Standard; } }
    public virtual float AreaRadius { get { return 0f; } }     // blast size for Explosive / Electric
    public virtual int Pierce { get { return 0; } }            // extra targets a Piercing bullet can pass through
    public virtual int Pellets { get { return 1; } }           // bullets per shot (more than 1 = scatter)
    public virtual float SpreadAngle { get { return 0f; } }    // total fan angle of the pellets, in degrees
    public virtual int BurstCount { get { return 1; } }        // shots fired one after another per trigger pull
    public virtual float BurstDelay { get { return 0.08f; } }
    public virtual string BulletTypeName { get { return Kind.ToString().ToUpper(); } }

    // ---- Numbers the garage shows ----
    public int TotalDamagePerShot { get { return Damage * Pellets * BurstCount; } }
    public float ShotsPerSecond { get { return 1f / FireInterval; } }

    public string DamageLabel
    {
        get
        {
            int n = Pellets * BurstCount;
            return n > 1 ? Damage + "x" + n : Damage.ToString();
        }
    }

    // Called by the Weapon each time the trigger is pulled
    public virtual void Fire(Weapon weapon)
    {
        if (BurstCount <= 1) FireVolley(weapon);
        else weapon.StartCoroutine(BurstRoutine(weapon));
    }

    // One volley: a single bullet, or a fan of pellets
    protected void FireVolley(Weapon weapon)
    {
        int count = Mathf.Max(1, Pellets);
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? Mathf.Lerp(-SpreadAngle * 0.5f, SpreadAngle * 0.5f, i / (count - 1f)) : 0f;
            weapon.SpawnBullet(this, angle);
        }
    }

    IEnumerator BurstRoutine(Weapon weapon)
    {
        for (int i = 0; i < BurstCount; i++)
        {
            if (weapon == null) yield break;
            FireVolley(weapon);
            yield return new WaitForSeconds(BurstDelay);
        }
    }
}
