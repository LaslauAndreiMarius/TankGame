using System.Collections.Generic;
using UnityEngine;

// A health box lying on the map. Drive into it (while hurt) to start regenerating health.
public class HealthPackPickup : MonoBehaviour
{
    public static readonly List<HealthPackPickup> All = new List<HealthPackPickup>();

    public Sprite boxSprite;        // assigned by Tools > Tank Game > Setup Health Packs In This Scene
    public AudioClip pickupSound;
    public float lifetime = 35f;
    public float blinkTime = 6f;

    private static readonly Color PackColor = new Color32(80, 220, 110, 255);

    private SpriteRenderer visual;
    private SpriteRenderer glow;
    private SpriteRenderer shadow;
    private float age;
    private float phase;
    private bool collected;

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        phase = Random.value * 6.28f;
        shadow = NewLayer("Shadow", -4);
        glow = NewLayer("Glow", -3);
        visual = NewLayer("Visual", 0);
    }

    void Start()
    {
        visual.sprite = boxSprite;
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

        // Pop in, then bob and glow gently
        float pop = Mathf.Clamp01(age / 0.35f);
        float grow = pop < 1f ? EaseOutBack(pop) : 1f;
        float bob = Mathf.Sin(t * 3f) * 0.06f;
        float pulse = 1f + Mathf.Sin(t * 4f) * 0.04f;
        float glowPulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);

        // Blink in the last seconds
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
        glow.color = new Color(PackColor.r, PackColor.g, PackColor.b, (0.40f + 0.25f * glowPulse) * visibility);

        shadow.transform.localScale = new Vector3(1.1f, 0.6f, 1f) * grow;
    }

    void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    void OnTriggerStay2D(Collider2D other)  { TryCollect(other); }   // so a pack you stood on while healthy works once you are hurt

    void TryCollect(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;

        Health health = other.GetComponent<Health>();
        if (health == null) return;

        HealthRegen regen = other.GetComponent<HealthRegen>();
        float alreadyComing = regen != null ? regen.Remaining : 0f;
        float missing = health.MaxHealth - health.CurrentHealth - alreadyComing;
        if (missing < 1f) return;   // already healthy: the pack waits for you

        // How much and how fast depends on the mode (and the wave, in Mayhem)
        float amount = Mathf.Min(health.MaxHealth * HealthPackSpawner.HealFractionNow(), missing);
        float duration = AugmentHooks.RegenDuration(HealthPackSpawner.DurationNow());   // an augment may change the time

        if (regen == null) regen = other.gameObject.AddComponent<HealthRegen>();
        regen.AddHeal(amount, duration);
        AugmentHooks.HealthPackCollected(amount);

        collected = true;
        PickupBurst.Spawn(transform.position, PackColor);
        Sfx.Play(pickupSound, transform.position);
        Destroy(gameObject);
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
