using UnityEngine;

public class Weapon : MonoBehaviour
{
    [HideInInspector] public float damageMultiplier = 1f;

    public GameObject bulletPrefab;
    public Transform firePoint;       // the empty object at the barrel tip
    public float fireRate = 0.4f;     // seconds between shots

    private float nextFireTime = 0f;

    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;   // still cooling down
        nextFireTime = Time.time + fireRate;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet b = bullet.GetComponent<Bullet>();
        b.ownerTag = gameObject.tag;
        b.damage = Mathf.RoundToInt(b.damage * damageMultiplier);   // double damage
    }
}