using UnityEngine;

// Dresses an enemy tank in a hull and a turret from the garage parts and gives it their stats.
// MayhemManager adds this to every enemy it spawns.
public class EnemyLoadout : MonoBehaviour
{
    public void Apply(HullBase hull, TurretBase turret, float healthScale, float speedScale,
                      float damageScale, float fireIntervalScale, int coinReward, Color tint)
    {
        SpriteRenderer hullRenderer = FindRenderer("HullVisual");
        SpriteRenderer turretRenderer = FindRenderer("TurretVisual");
        Transform turretRoot = FindChild("Turret");
        Transform firePoint = FindChild("FirePoint");

        // ---- Looks (tinted red-ish so enemies are easy to tell from the player) ----
        if (hullRenderer != null && hull.sprite != null)
        {
            hullRenderer.sprite = hull.sprite;
            hullRenderer.color = tint;
            ResetTransform(hullRenderer.transform);

            Animator animator = hullRenderer.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;   // the old track animation would overwrite the sprite
        }

        if (turretRoot != null) turretRoot.localPosition = hull.TurretMount;

        if (turretRenderer != null && turret.sprite != null)
        {
            turretRenderer.sprite = turret.sprite;
            turretRenderer.color = tint;
            ResetTransform(turretRenderer.transform);
        }

        if (firePoint != null) firePoint.localPosition = new Vector3(0f, turret.MuzzleLength, 0f);

        if (hull.sprite != null)
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(hull.sprite.bounds.size.x * 0.9f, hull.sprite.bounds.size.y * 0.92f);
                box.offset = Vector2.zero;
            }

            Transform exhaust = FindChild("ExhaustPoint");
            if (exhaust != null) exhaust.localPosition = new Vector3(0f, -hull.sprite.bounds.extents.y, 0f);
        }

        // ---- Stats ----
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed = hull.MoveSpeed * speedScale;
            ai.hullRotateSpeed = hull.TurnSpeed * 0.7f;

            // Fight at a distance that suits the gun: close for a shotgun, far for a railgun
            ai.preferredDistance = Mathf.Clamp(turret.Range * 0.55f, 4f, 9f);
            ai.tooCloseDistance = ai.preferredDistance * 0.6f;
            ai.shootRange = Mathf.Min(turret.Range * 0.9f, 14f);
        }

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaxHealth(Mathf.Max(1, Mathf.RoundToInt(hull.MaxHealth * healthScale)));
            health.coinReward = coinReward;
        }

        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null)
        {
            weapon.turret = turret;
            weapon.damageScale = damageScale;
            weapon.fireIntervalScale = fireIntervalScale;
        }
    }

    static void ResetTransform(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    Transform FindChild(string childName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }

    SpriteRenderer FindRenderer(string childName)
    {
        Transform t = FindChild(childName);
        return t != null ? t.GetComponent<SpriteRenderer>() : null;
    }
}
