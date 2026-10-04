using UnityEngine;

public enum PowerUpType { DoubleDamage, DoubleArmor, DoubleSpeed }

public class PowerUpReceiver : MonoBehaviour
{
    public float duration = 15f;

    private float[] endTime = new float[3];   // one end-time per power-up type

    private Weapon weapon;
    private Health health;
    private TankMovement playerMove;   // only exists on the player
    private EnemyAI enemyAI;           // only exists on enemies

    void Awake()
    {
        weapon = GetComponent<Weapon>();
        health = GetComponent<Health>();
        playerMove = GetComponent<TankMovement>();
        enemyAI = GetComponent<EnemyAI>();
    }

    public void Activate(PowerUpType type)
    {
        endTime[(int)type] = Time.time + duration;
    }

    public float GetRemaining(PowerUpType type)
    {
        return Mathf.Max(0f, endTime[(int)type] - Time.time);
    }

    public bool IsActive(PowerUpType type)
    {
        return GetRemaining(type) > 0f;
    }

    void Update()
    {
        // Every frame, set the multipliers from what is currently active.
        // When an effect runs out, the multiplier simply goes back to 1.
        if (weapon != null)
            weapon.damageMultiplier = IsActive(PowerUpType.DoubleDamage) ? 2f : 1f;

        if (health != null)
            health.damageTakenMultiplier = IsActive(PowerUpType.DoubleArmor) ? 0.5f : 1f;

        float speed = IsActive(PowerUpType.DoubleSpeed) ? 2f : 1f;
        if (playerMove != null) playerMove.speedMultiplier = speed;
        //if (enemyAI != null) enemyAI.speedMultiplier = speed;
    }
}