using UnityEngine;

public class Health : MonoBehaviour
{
    [HideInInspector] public float damageTakenMultiplier = 1f;
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(1, Mathf.RoundToInt(amount * damageTakenMultiplier));   // double armor
        currentHealth -= amount;
        Debug.Log(gameObject.name + " health: " + currentHealth);

        if (currentHealth <= 0)
            Destroy(gameObject);
    }
}