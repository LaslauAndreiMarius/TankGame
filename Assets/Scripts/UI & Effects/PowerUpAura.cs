using UnityEngine;

// A soft colored glow under a tank while it has a power-up, so everyone (including you,
// looking at an enemy) can see which effects are active. Added automatically on first pickup.
[RequireComponent(typeof(PowerUpReceiver))]
public class PowerUpAura : MonoBehaviour
{
    private PowerUpReceiver receiver;
    private SpriteRenderer aura;

    private static readonly PowerUpType[] AllTypes =
    {
        PowerUpType.DoubleDamage, PowerUpType.DoubleArmor, PowerUpType.DoubleSpeed
    };

    void Awake()
    {
        receiver = GetComponent<PowerUpReceiver>();

        GameObject go = new GameObject("PowerUpAura");
        go.transform.SetParent(transform, false);
        aura = go.AddComponent<SpriteRenderer>();
        aura.sprite = GlowSprite.Get();
        aura.sortingOrder = -2;   // above the ground, below the tank
        aura.color = new Color(1f, 1f, 1f, 0f);
    }

    void Update()
    {
        Color sum = Color.black;
        int active = 0;

        foreach (PowerUpType type in AllTypes)
        {
            if (!receiver.IsActive(type)) continue;
            sum += PowerUpStyle.ColorFor(type);
            active++;
        }

        // Several effects at once blend their colors
        Color c = active > 0 ? sum / active : aura.color;
        float target = active > 0 ? 0.55f + 0.2f * Mathf.Sin(Time.time * 5f) : 0f;
        c.a = Mathf.Lerp(aura.color.a, target, 10f * Time.deltaTime);
        aura.color = c;

        float size = 2.8f + 0.2f * Mathf.Sin(Time.time * 5f);
        aura.transform.localScale = new Vector3(size, size, 1f);
    }
}
