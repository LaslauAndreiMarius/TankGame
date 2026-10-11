using UnityEngine;

public class Health : MonoBehaviour
{
    [HideInInspector] public float damageTakenMultiplier = 1f;   // set by the double-armor power-up
    public int maxHealth = 100;
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("Rewards")]
    public int coinReward = 10;   // coins an enemy drops when it dies

    private int currentHealth;
    private bool isDead = false;

    // Read-only access for the HUD
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    void Awake()
    {
        // Awake (not Start) so the HUD can read a valid value on the very first frame
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;   // two bullets in the same frame must not kill it twice

        amount = Mathf.Max(1, Mathf.RoundToInt(amount * damageTakenMultiplier));
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;

        // Tell the GameManager who died
        if (GameManager.Instance != null)
        {
            if (CompareTag("Player")) GameManager.Instance.PlayerDied();
            else if (CompareTag("Enemy")) GameManager.Instance.AddKill();
        }

        // Enemies leave coins behind that the player can drive over
        if (CompareTag("Enemy")) CoinPickup.Drop(transform.position, coinReward);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Sfx.Play(deathSound, transform.position);

        Destroy(gameObject);
    }
}
