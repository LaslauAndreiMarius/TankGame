// What a bullet does when it hits something
public enum BulletKind
{
    Standard,    // damages the one thing it hits
    Piercing,    // goes through several targets
    Explosive,   // blast that damages everything nearby (damage drops toward the edge)
    Electric     // zone of full damage around the impact point
}
