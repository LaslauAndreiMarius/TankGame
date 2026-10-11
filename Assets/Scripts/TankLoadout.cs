using UnityEngine;

// Put this on the Player. When a map starts it dresses the tank in the hull and turret chosen in the
// garage and applies their stats (speed, turning, health, damage, fire rate...).
// It runs before the other scripts so the health and weapon already have the right numbers.
[DefaultExecutionOrder(-200)]
public class TankLoadout : MonoBehaviour
{
    void Awake()
    {
        HullBase hull = PlayerProfile.EquippedHull;
        TurretBase turret = PlayerProfile.EquippedTurret;

        if (hull == null || turret == null)
        {
            Debug.LogWarning("TankLoadout: no tank parts found. Run Tools > Tank Game > Setup Garage Assets. The tank keeps its old look.");
            return;
        }
        Apply(hull, turret);
    }

    public void Apply(HullBase hull, TurretBase turret)
    {
        SpriteRenderer hullRenderer = FindRenderer("HullVisual");
        SpriteRenderer turretRenderer = FindRenderer("TurretVisual");
        Transform turretRoot = FindChild("Turret");
        Transform firePoint = FindChild("FirePoint");

        // ---- Looks ----
        if (hullRenderer != null && hull.sprite != null)
        {
            hullRenderer.sprite = hull.sprite;
            ResetTransform(hullRenderer.transform);

            // The old track animation would keep overwriting the new sprite
            Animator animator = hullRenderer.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
        }

        if (turretRoot != null) turretRoot.localPosition = hull.TurretMount;

        if (turretRenderer != null && turret.sprite != null)
        {
            turretRenderer.sprite = turret.sprite;
            ResetTransform(turretRenderer.transform);   // the sprite's pivot is already the turning point
        }

        if (firePoint != null) firePoint.localPosition = new Vector3(0f, turret.MuzzleLength, 0f);

        // The collider and the exhaust smoke follow the size of the new hull
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
        TankMovement movement = GetComponent<TankMovement>();
        if (movement != null)
        {
            movement.moveSpeed = hull.MoveSpeed;
            movement.rotateSpeed = hull.TurnSpeed;
        }

        Health health = GetComponent<Health>();
        if (health != null) health.maxHealth = hull.MaxHealth;

        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null) weapon.turret = turret;
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
