using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;

    public AudioClip shootSound;

    [HideInInspector] public float damageMultiplier = 1f;

    private float nextFireTime = 0f;

    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet b = bullet.GetComponent<Bullet>();
        b.ownerTag = gameObject.tag;
        b.damage = Mathf.RoundToInt(b.damage * damageMultiplier);

        Sfx.Play(shootSound, firePoint.position);
    }
}