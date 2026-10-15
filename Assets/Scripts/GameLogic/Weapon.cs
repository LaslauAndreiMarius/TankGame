using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;            // used when no garage turret is set (Classic enemies)

    public AudioClip shootSound;

    [HideInInspector] public float damageMultiplier = 1f;      // set by the double-damage power-up
    [HideInInspector] public TurretBase turret;                 // set by TankLoadout / EnemyLoadout
    [HideInInspector] public float damageScale = 1f;            // Mayhem enemies hit softer than the same turret on the player
    [HideInInspector] public float fireIntervalScale = 1f;      // ...and shoot a little slower

    private float nextFireTime = 0f;

    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        float interval = (turret != null ? turret.FireInterval : fireRate) * fireIntervalScale;
        nextFireTime = Time.time + interval;

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
        bullet.damage = Mathf.Max(1, Mathf.RoundToInt(bullet.damage * damageMultiplier * damageScale));
        return bullet;
    }
}
