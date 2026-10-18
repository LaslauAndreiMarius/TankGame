using UnityEngine;

// Base class for every hull. Each hull type is a small subclass that only fills in its own numbers
// The sprite is assigned by Tools > Tank Game > Setup Garage Assets.
public abstract class HullBase : ScriptableObject
{
    public Sprite sprite;

    public abstract string Id { get; }            // matches the image file name, e.g. "KV1" for KV1_Black_Bot.png
    public abstract string DisplayName { get; }
    public abstract string Role { get; }
    public abstract int Price { get; }            // in coins

    public abstract float MoveSpeed { get; }      // world units per second
    public abstract float TurnSpeed { get; }      // degrees per second
    public abstract int MaxHealth { get; }

    // Where the turret ring is on this hull, in world units from the center of the sprite
    public virtual Vector2 TurretMount { get { return Vector2.zero; } }
}
