using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Power-ups")]
    public float pickupSeekRadius = 6f;   
    [HideInInspector] public float speedMultiplier = 1f;
    private PowerUpReceiver receiver;
    
    [Header("Hull (movement)")]
    public float moveSpeed = 2.5f;
    public float hullRotateSpeed = 100f;     // degrees per second
    public float preferredDistance = 7f;     // approaches until this close
    public float tooCloseDistance = 4.5f;    // backs away if closer than this

    [Header("Turret (aiming)")]
    public Transform turret;                 // drag the Turret child here
    public float turretRotateSpeed = 150f;
    public float aimTolerance = 8f;          // max angle error (degrees) at which it may fire
    public float shootRange = 10f;
    public LayerMask obstacleMask;           // optional: walls that block line of sight

    private Transform player;
    private Rigidbody2D rb;
    private Weapon weapon;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon = GetComponent<Weapon>();
        receiver = GetComponent<PowerUpReceiver>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    // Turret: aiming is visual and smooth, so it runs every frame
    void Update()
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)turret.position;
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;

        float current = turret.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, turretRotateSpeed * Time.deltaTime);
        turret.rotation = Quaternion.Euler(0f, 0f, next);

        // Only shoot if close enough, actually aimed at the player, and nothing blocks the shot
        float aimError = Mathf.Abs(Mathf.DeltaAngle(next, targetAngle));
        if (toPlayer.magnitude <= shootRange && aimError <= aimTolerance && HasLineOfSight(toPlayer))
            weapon.TryShoot();
    }

    void FixedUpdate()
{
    if (player == null)
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }

    // Priority 1: a nearby power-up box
    PowerUpPickup pickup = FindNearbyPickup();
    if (pickup != null)
    {
        DriveTowardPoint(pickup.transform.position);
        return;
    }

    // Priority 2: the normal behavior (unchanged apart from speedMultiplier)
    Vector2 toPlayer = (Vector2)player.position - rb.position;
    float distance = toPlayer.magnitude;

    float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
    float angleError = Mathf.DeltaAngle(rb.rotation, targetAngle);
    rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, hullRotateSpeed * Time.fixedDeltaTime));

    float speed = 0f;
    float currentMax = moveSpeed * speedMultiplier;
    if (distance > preferredDistance)
    {
        if (Mathf.Abs(angleError) < 60f) speed = currentMax;
    }
    else if (distance < tooCloseDistance)
    {
        speed = -currentMax * 0.6f;
    }

    rb.linearVelocity = (Vector2)transform.up * speed;
    rb.angularVelocity = 0f;
}

PowerUpPickup FindNearbyPickup()
{
    if (receiver == null) return null;

    // Don't wander off for a box while the player is right on top of us
    if (Vector2.Distance(rb.position, player.position) < tooCloseDistance) return null;

    PowerUpPickup best = null;
    float bestDistance = pickupSeekRadius;

    foreach (PowerUpPickup p in PowerUpPickup.All)
    {
        if (receiver.GetRemaining(p.type) > 3f) continue;   // already have plenty of this one

        float d = Vector2.Distance(rb.position, p.transform.position);
        if (d < bestDistance)
        {
            bestDistance = d;
            best = p;
        }
    }
    return best;
}

void DriveTowardPoint(Vector2 point)
{
    Vector2 to = point - rb.position;
    float targetAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg - 90f;
    float angleError = Mathf.DeltaAngle(rb.rotation, targetAngle);

    rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, hullRotateSpeed * Time.fixedDeltaTime));

    float speed = Mathf.Abs(angleError) < 60f ? moveSpeed * speedMultiplier : 0f;
    rb.linearVelocity = (Vector2)transform.up * speed;
    rb.angularVelocity = 0f;
}

bool HasLineOfSight(Vector2 toPlayer)
{
    // If obstacleMask is empty (the default), nothing blocks the shot
    RaycastHit2D hit = Physics2D.Raycast(turret.position, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
    return hit.collider == null;
}
}