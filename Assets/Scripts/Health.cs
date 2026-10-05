using UnityEngine;

public class Health : MonoBehaviour
{
    [HideInInspector] public float damageTakenMultiplier = 1f;
    public int maxHealth = 100;
    public GameObject deathEffect;
    public AudioClip deathSound;

    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;   // two bullets in the same frame must not kill it twice

        amount = Mathf.Max(1, Mathf.RoundToInt(amount * damageTakenMultiplier));   // double armor
        currentHealth -= amount;
        Debug.Log(gameObject.name + " health: " + currentHealth);

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

    if (deathEffect != null)
        Instantiate(deathEffect, transform.position, Quaternion.identity);

    Sfx.Play(deathSound, transform.position);

    Destroy(gameObject);
}
}