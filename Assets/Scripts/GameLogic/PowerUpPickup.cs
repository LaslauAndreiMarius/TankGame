using System.Collections.Generic;
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    // A shared list of every box currently in the world. The spawner and the enemy AI both use it.
    public static List<PowerUpPickup> All = new List<PowerUpPickup>();

    public PowerUpType type;
    public float lifetime = 40f;     // disappears if nobody takes it
    public float blinkTime = 6f;     // starts blinking this many seconds before it disappears

    [Header("Look (filled in by Tools > Tank Game > Setup Power-Up Boxes)")]
    public Sprite damageBox;
    public Sprite armorBox;
    public Sprite speedBox;
    public AudioClip pickupSound;

    private SpriteRenderer rootRenderer;   // the old plain square, if the prefab still has one
    private SpriteRenderer visual;         // the box artwork
    private SpriteRenderer glow;           // colored halo behind the box
    private SpriteRenderer shadow;         // soft shadow on the ground
    private float age;
    private float phase;
    private bool collected;

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        rootRenderer = GetComponent<SpriteRenderer>();
        phase = Random.value * 6.28f;   // so the boxes don't all bob in sync

        shadow = NewLayer("Shadow", -4);
        glow = NewLayer("Glow", -3);
        visual = NewLayer("Visual", 0);
    }

    void Start()
    {
        Color color = PowerUpStyle.ColorFor(type);
        Sprite art = SpriteFor(type);

        if (art != null)
        {
            visual.sprite = art;
            if (rootRenderer != null) rootRenderer.enabled = false;   // hide the old plain square
        }
        else if (rootRenderer != null)
        {
            rootRenderer.color = color;   // no artwork assigned: fall back to a colored square
            visual.enabled = false;
        }

        glow.sprite = GlowSprite.Get();
        shadow.sprite = GlowSprite.Get();
        shadow.color = new Color(0f, 0f, 0f, 0.4f);
        shadow.transform.localPosition = new Vector3(0.05f, -0.38f, 0f);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Time.time + phase;
        Color color = PowerUpStyle.ColorFor(type);

        // Pop in: grows from nothing with a little overshoot
        float pop = Mathf.Clamp01(age / 0.35f);
        float grow = pop < 1f ? EaseOutBack(pop) : 1f;

        float bob = Mathf.Sin(t * 3f) * 0.06f;
        float pulse = 1f + Mathf.Sin(t * 4f) * 0.04f;
        float glowPulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);

        // Blink in the last seconds, so the player knows it is about to vanish
        float visibility = 1f;
        float left = lifetime - age;
        if (left < blinkTime)
        {
            float rate = Mathf.Lerp(14f, 5f, left / blinkTime);
            visibility = Mathf.Sin(age * rate) > -0.2f ? 1f : 0.25f;
        }

        visual.transform.localPosition = new Vector3(0f, bob, 0f);
        visual.transform.localScale = Vector3.one * grow * pulse;
        SetAlpha(visual, visibility);

        glow.transform.localScale = Vector3.one * (2.1f + 0.35f * glowPulse) * grow;
        glow.color = new Color(color.r, color.g, color.b, (0.40f + 0.25f * glowPulse) * visibility);

        shadow.transform.localScale = new Vector3(1.1f, 0.6f, 1f) * grow;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        PowerUpReceiver receiver = other.GetComponentInParent<PowerUpReceiver>();
        if (receiver == null) return;   // not a tank (a wall or a bullet, for example)

        collected = true;
        receiver.Activate(type);

        // The glow around the tank is added the first time that tank collects anything
        if (receiver.GetComponent<PowerUpAura>() == null)
            receiver.gameObject.AddComponent<PowerUpAura>();

        PickupBurst.Spawn(transform.position, PowerUpStyle.ColorFor(type));
        Sfx.Play(pickupSound, transform.position);

        Destroy(gameObject);
    }

    // Kept so older scripts that call PowerUpPickup.ColorFor still work
    public static Color ColorFor(PowerUpType t)
    {
        return PowerUpStyle.ColorFor(t);
    }

    Sprite SpriteFor(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.DoubleDamage: return damageBox;
            case PowerUpType.DoubleArmor:  return armorBox;
            default:                       return speedBox;
        }
    }

    SpriteRenderer NewLayer(string layerName, int order)
    {
        GameObject go = new GameObject(layerName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }

    static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
