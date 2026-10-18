using UnityEngine;

// Heals a tank gradually instead of all at once. A health pack hands this component an amount and a time,
// and it spreads the healing evenly over that time. It is added to the player the first time a pack is collected.
[RequireComponent(typeof(Health))]
public class HealthRegen : MonoBehaviour
{
    private Health health;
    private float remaining;   // health still to be given
    private float timeLeft;    // seconds until all of it has been given
    private float carry;       // health is a whole number, so fractions are collected here until they add up to 1

    public bool IsHealing { get { return remaining > 0.01f; } }
    public float Remaining { get { return remaining; } }

    // 1 while healing, fading out over the last moments. The green screen effect uses this.
    public float Intensity { get { return IsHealing ? Mathf.Clamp01(timeLeft / 0.6f) : 0f; } }

    void Awake()
    {
        health = GetComponent<Health>();
    }

    // Collecting a second pack while still healing adds to what is left
    public void AddHeal(float amount, float duration)
    {
        if (remaining <= 0.01f) timeLeft = duration;
        else timeLeft = Mathf.Max(timeLeft, duration);

        remaining += amount;
    }

    void Update()
    {
        if (remaining <= 0.01f) return;

        // Nothing left to heal: stop (the extra is simply lost)
        if (health.CurrentHealth >= health.MaxHealth)
        {
            remaining = 0f;
            carry = 0f;
            return;
        }

        float dt = Time.deltaTime;
        float step = timeLeft > dt ? remaining * (dt / timeLeft) : remaining;   // an even share of what is left
        remaining -= step;
        timeLeft -= dt;
        carry += step;

        int whole = Mathf.FloorToInt(carry);
        if (whole > 0)
        {
            health.Heal(whole);
            carry -= whole;
        }

        if (timeLeft <= 0f)
        {
            remaining = 0f;
            if (carry >= 0.5f) health.Heal(Mathf.RoundToInt(carry));
            carry = 0f;
        }
    }
}
