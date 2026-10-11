using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    public float lifetime = 3f;

    public GameObject hitEffect;
    public AudioClip hitSound;

    [HideInInspector] public string ownerTag;   // who fired me

    // Filled in by Configure() when a garage turret fired this bullet
    [HideInInspector] public BulletKind kind = BulletKind.Standard;
    [HideInInspector] public float areaRadius;
    [HideInInspector] public int pierceLeft;

    private static readonly Color ExplosionColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color ElectricColor = new Color(0.45f, 0.8f, 1f);

    // Copies the turret's numbers and artwork onto this bullet
    public void Configure(TurretBase turret)
    {
        damage = turret.Damage;
        speed = turret.BulletSpeed;
        lifetime = turret.Range / turret.BulletSpeed;   // range = how far it flies before disappearing
        kind = turret.Kind;
        areaRadius = turret.AreaRadius;
        pierceLeft = turret.Pierce;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && turret.bulletSprite != null)
        {
            sr.sprite = turret.bulletSprite;
            sr.transform.localRotation = Quaternion.identity;   // the garage bullets already point up
            sr.transform.localScale = Vector3.one;

            Animator animator = sr.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
        }
    }

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.up * speed;
        Destroy(gameObject, lifetime);   // cleanup so bullets don't live forever
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;                 // ignore pickup boxes and coins
        if (other.CompareTag(ownerTag)) return;      // don't hit the shooter's team
        if (other.CompareTag("Bullet")) return;      // ignore other bullets

        if (kind == BulletKind.Explosive || kind == BulletKind.Electric)
        {
            Explode();
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);

            if (pierceLeft > 0)
            {
                pierceLeft--;
                SpawnHitEffect();
                return;   // keep flying through the target
            }
        }

        SpawnHitEffect();
        Destroy(gameObject);
    }

    // Damages everything in a circle around the impact point
    void Explode()
    {
        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, areaRadius);

        foreach (Collider2D c in hits)
        {
            if (c.isTrigger || c.CompareTag(ownerTag)) continue;

            Health health = c.GetComponent<Health>();
            if (health == null) continue;

            // Explosions hurt less toward the edge; electric zones hurt everyone inside the same
            float distance = Mathf.Clamp01(Vector2.Distance(center, c.transform.position) / areaRadius);
            float falloff = kind == BulletKind.Electric ? 1f : Mathf.Lerp(1f, 0.5f, distance);
            health.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage * falloff)));
        }

        bool electric = kind == BulletKind.Electric;
        AreaBlast.Spawn(center, areaRadius, electric ? ElectricColor : ExplosionColor, electric);
        Sfx.Play(hitSound, transform.position);
        Destroy(gameObject);
    }

    void SpawnHitEffect()
    {
        if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Sfx.Play(hitSound, transform.position);
    }
}
    