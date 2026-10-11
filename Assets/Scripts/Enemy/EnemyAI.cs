using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static readonly List<EnemyAI> All = new List<EnemyAI>();   // every living enemy (used for spacing)

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
    public LayerMask obstacleMask;           // optional: restrict the "can I shoot?" check to these layers

    [Header("Pathfinding")]
    public bool usePathfinding = true;       // untick to see the old "drive straight at the player" behaviour
    public float repathInterval = 0.4f;      // seconds between route recalculations
    public float waypointReach = 0.6f;       // how close counts as "arrived" at a waypoint
    public float goalMoveThreshold = 1.5f;   // recalculate early if the target moved this far
    public float separationRadius = 2f;      // enemies push apart when closer than this
    public float separationWeight = 0.8f;
    public bool drawPath = false;            // shows the route in the Scene / Game view (Gizmos must be on)

    private Transform player;
    private Rigidbody2D rb;
    private Weapon weapon;
    private PathGrid grid;

    // "Can I shoot the player?" check
    private ContactFilter2D shotFilter;
    private readonly RaycastHit2D[] rayHits = new RaycastHit2D[8];

    // Following a route
    private List<Vector2> path;
    private int pathIndex;
    private Vector2 pathGoal;
    private float repathTimer;
    private bool forceRepath;

    // Getting unstuck
    private Vector2 lastCheckPos;
    private float stuckTimer;
    private float moveCommandTime;
    private float unstickTimer;
    private float lastSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon = GetComponent<Weapon>();
        receiver = GetComponent<PowerUpReceiver>();

        int mask = obstacleMask.value == 0 ? ~0 : obstacleMask.value;   // empty mask = check everything
        shotFilter = new ContactFilter2D();
        shotFilter.useTriggers = false;
        shotFilter.SetLayerMask(mask);
    }

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (usePathfinding) grid = PathGrid.Get();

        lastCheckPos = rb.position;
        repathTimer = Random.value * repathInterval;   // so enemies don't all recalculate in the same frame
    }

    // ------------------------------------------------------------------
    //  Turret: aiming is visual and smooth, so it runs every frame
    // ------------------------------------------------------------------
    void Update()
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)turret.position;
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;

        float current = turret.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, turretRotateSpeed * Time.deltaTime);
        turret.rotation = Quaternion.Euler(0f, 0f, next);

        // Only shoot if close enough, actually aimed at the player, and no wall is in the way
        float aimError = Mathf.Abs(Mathf.DeltaAngle(next, targetAngle));
        if (toPlayer.magnitude <= shootRange && aimError <= aimTolerance && HasClearShot(toPlayer))
            weapon.TryShoot();
    }

    // Is there a wall between the turret and the player?
    bool HasClearShot(Vector2 toPlayer)
    {
        float distance = toPlayer.magnitude;
        if (distance < 0.01f) return true;

        int count = Physics2D.Raycast(turret.position, toPlayer / distance, shotFilter, rayHits, distance);
        for (int i = 0; i < count; i++)
        {
            if (PathGrid.IsSolid(rayHits[i].collider)) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------
    //  Hull: physics movement, so it runs in FixedUpdate
    // ------------------------------------------------------------------
    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector2 pos = rb.position;

        // Recovering from being stuck: reverse out for a moment
        if (unstickTimer > 0f)
        {
            unstickTimer -= dt;
            float direction = lastSpeed >= 0f ? -1f : 1f;   // go the opposite way to what got us stuck
            rb.linearVelocity = (Vector2)transform.up * direction * moveSpeed * speedMultiplier * 0.6f;
            rb.angularVelocity = 0f;
            return;
        }
        CheckStuck(pos, dt);

        // Priority 1: a nearby power-up box
        PowerUpPickup pickup = FindNearbyPickup();
        if (pickup != null)
        {
            Steer(SteerPointFor(pickup.transform.position), dt);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - pos;
        float distance = toPlayer.magnitude;

        // Priority 2: a wall is between us and the player, so follow a route around it
        if (grid != null && !grid.HasLineOfSight(pos, player.position))
        {
            Steer(SteerPointFor(player.position), dt);
            return;
        }
        ClearPath();

        // Priority 3: clear view of the player, so keep the preferred distance
        float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        float angleError = Mathf.DeltaAngle(rb.rotation, targetAngle);
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, hullRotateSpeed * dt));

        float speed = 0f;
        float currentMax = moveSpeed * speedMultiplier;
        if (distance > preferredDistance)
        {
            if (Mathf.Abs(angleError) < 60f) speed = currentMax;   // don't drive sideways while still turning
        }
        else if (distance < tooCloseDistance)
        {
            speed = -currentMax * 0.6f;                            // back away, still facing the player
        }
        // between the two distances: stand still

        Move(speed, dt);
    }

    // Turns the hull toward a point and drives at it (used while following a route or fetching a power-up)
    void Steer(Vector2 target, float dt)
    {
        Vector2 direction = target - rb.position;
        direction = (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero)
                    + Separation() * separationWeight;

        if (direction.sqrMagnitude < 0.0001f)
        {
            Move(0f, dt);
            return;
        }

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float angleError = Mathf.DeltaAngle(rb.rotation, targetAngle);
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, hullRotateSpeed * dt));

        float speed = Mathf.Abs(angleError) < 60f ? moveSpeed * speedMultiplier : 0f;
        Move(speed, dt);
    }

    void Move(float speed, float dt)
    {
        rb.linearVelocity = (Vector2)transform.up * speed;
        rb.angularVelocity = 0f;

        lastSpeed = speed;
        if (Mathf.Abs(speed) > 0.01f) moveCommandTime += dt;
    }

    // A push away from nearby enemies, so they spread out instead of piling up in a corridor
    Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        foreach (EnemyAI other in All)
        {
            if (other == this) continue;

            Vector2 away = rb.position - other.rb.position;
            float d = away.magnitude;
            if (d > 0.001f && d < separationRadius)
                push += away / d * (1f - d / separationRadius);
        }
        return push;
    }

    // ------------------------------------------------------------------
    //  Following a route
    // ------------------------------------------------------------------

    // Where should the hull head right now if it wants to get to "goal"?
    Vector2 SteerPointFor(Vector2 goal)
    {
        return grid != null ? GetSteerPoint(goal) : goal;
    }

    Vector2 GetSteerPoint(Vector2 goal)
    {
        Vector2 pos = rb.position;

        // Nothing in the way: drive straight at it
        if (grid.HasLineOfSight(pos, goal))
        {
            ClearPath();
            return goal;
        }

        // Work out a new route now and then, or when the target has moved, or when we got stuck
        repathTimer -= Time.fixedDeltaTime;
        bool goalMoved = path != null && (goal - pathGoal).sqrMagnitude > goalMoveThreshold * goalMoveThreshold;

        if (path == null || repathTimer <= 0f || goalMoved || forceRepath)
        {
            path = grid.FindPath(pos, goal) ?? new List<Vector2>();   // an empty list means "no route exists"
            pathIndex = 0;
            pathGoal = goal;
            repathTimer = repathInterval * Random.Range(0.8f, 1.2f);
            forceRepath = false;
        }

        if (path.Count == 0) return goal;   // no route found: head straight for it and hope

        // Move on to the next waypoint once we reach one
        while (pathIndex < path.Count - 1 && Vector2.Distance(pos, path[pathIndex]) < waypointReach)
            pathIndex++;

        // Cut corners: jump ahead to the furthest waypoint that is already in a clear line
        for (int i = path.Count - 1; i > pathIndex; i--)
        {
            if (grid.HasLineOfSight(pos, path[i]))
            {
                pathIndex = i;
                break;
            }
        }

        return path[pathIndex];
    }

    void ClearPath()
    {
        path = null;
    }

    // If we keep driving but hardly move, we are stuck on something: back up and recalculate the route
    void CheckStuck(Vector2 pos, float dt)
    {
        stuckTimer += dt;
        if (stuckTimer < 0.6f) return;

        float moved = (pos - lastCheckPos).magnitude;
        if (moveCommandTime > 0.45f && moved < 0.12f)
        {
            unstickTimer = 0.45f;
            forceRepath = true;
        }

        lastCheckPos = pos;
        stuckTimer = 0f;
        moveCommandTime = 0f;
    }

    // ------------------------------------------------------------------
    //  Power-ups
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    //  Debug drawing
    // ------------------------------------------------------------------
    void OnDrawGizmos()
    {
        if (!drawPath || path == null || path.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector3 previous = transform.position;
        for (int i = pathIndex; i < path.Count; i++)
        {
            Gizmos.DrawLine(previous, path[i]);
            Gizmos.DrawSphere(path[i], 0.12f);
            previous = path[i];
        }
    }
}
