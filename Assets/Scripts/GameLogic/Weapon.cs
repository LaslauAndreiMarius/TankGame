using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;            // used when no garage turret is set (enemies)

    public AudioClip shootSound;

    [HideInInspector] public float damageMultiplier = 1f;   // set by the double-damage power-up
    [HideInInspector] public TurretBase turret;              // set by TankLoadout on the player

    private float nextFireTime = 0f;

    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + (turret != null ? turret.FireInterval : fireRate);

        if (turret != null) turret.Fire(this);   // each turret type decides how it shoots
        else SpawnBullet(null, 0f);

        Sfx.Play(shootSound, firePoint.position);
    }

    // Creates one bullet. angleOffset turns it left or right (used for scatter shots).
    public Bullet SpawnBullet(TurretBase fromTurret, float angleOffset)
    {
        Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, 0f, angleOffset);
        GameObject go = Instantiate(bulletPrefab, firePoint.position, rotation);

        Bullet bullet = go.GetComponent<Bullet>();
        bullet.ownerTag = gameObject.tag;
        if (fromTurret != null) bullet.Configure(fromTurret);
        bullet.damage = Mathf.RoundToInt(bullet.damage * damageMultiplier);
        return bullet;
    }
}
