using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    public float lifetime = 3f;

    [HideInInspector] public string ownerTag;   // who fired me

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.up * speed;
        Destroy(gameObject, lifetime);   // cleanup so bullets don't live forever
    }

    void OnTriggerEnter2D(Collider2D other)
    {   
        if (other.isTrigger) return;   // ignore pickup boxes and other triggers
        if (other.CompareTag(ownerTag)) return;   // don't hit the shooter's team
        if (other.CompareTag("Bullet")) return;   // ignore other bullets

        Health health = other.GetComponent<Health>();
        if (health != null) health.TakeDamage(damage);

        Destroy(gameObject);   // disappear on any hit, including walls
    }
}