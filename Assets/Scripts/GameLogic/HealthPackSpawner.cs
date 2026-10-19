using UnityEngine;

// Places health packs on the map and decides how strong they are.
//   Classic: a pack heals a big, fixed part of your maximum health.
//   Mayhem:  the heal starts smaller and grows with every wave (and happens faster).
public class HealthPackSpawner : MonoBehaviour
{
    public static HealthPackSpawner Instance { get; private set; }

    public GameObject packPrefab;

    [Header("Spawning")]
    public float classicInterval = 25f;      // seconds between packs
    public float mayhemInterval = 14f;
    public int maxPacks = 2;                 // on the map at once
    public bool onlyWhenHurt = true;         // packs appear only while you are missing health
    public float hurtBelow = 0.85f;          // "hurt" = under this share of your maximum
    public float minDistanceFromPlayer = 5f;
    public float minDistanceBetweenPacks = 10f;
    public float clearRadius = 1f;
    public Vector2 mapMin = new Vector2(-19f, -14f);
    public Vector2 mapMax = new Vector2(19f, 14f);

    [Header("Healing: Classic")]
    public float classicFraction = 0.85f;    // share of your MAXIMUM health. From 1 HP you end up close to full, but not full.
    public float classicDuration = 4f;       // seconds the regeneration takes

    [Header("Healing: Mayhem")]
    public float mayhemStartFraction = 0.55f;     // wave 1
    public float mayhemFractionPerWave = 0.05f;   // gets stronger every wave...
    public float mayhemMaxFraction = 0.95f;       // ...up to this (raise to 1 if you want a full refill)
    public float mayhemStartDuration = 5f;        // wave 1 takes this long...
    public float mayhemMinDuration = 2.5f;        // ...and the heal gets faster until this
    public int mayhemFastestWave = 10;

    private float timer;
    private Transform player;
    private Health playerHealth;
    private MayhemManager mayhem;
    private PathGrid grid;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mayhem = FindFirstObjectByType<MayhemManager>();
        if (mayhem != null) mayhem.WaveCleared += OnWaveCleared;

        grid = PathGrid.Get();
        grid.HasLineOfSight(Vector2.zero, Vector2.zero);   // makes the grid build itself on first use
    }

    void OnDestroy()
    {
        if (mayhem != null) mayhem.WaveCleared -= OnWaveCleared;
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    //  How strong a pack is right now (read by the pack when it is collected)
    // ------------------------------------------------------------------
    public static float HealFractionNow()
    {
        return Instance != null ? Instance.CalculateFraction() : 0.85f;
    }

    public static float DurationNow()
    {
        return Instance != null ? Instance.CalculateDuration() : 4f;
    }

    float CalculateFraction()
    {
        if (GameSettings.Mode != GameMode.Mayhem) return classicFraction;

        int wave = Mathf.Max(1, mayhem != null ? mayhem.Wave : 1);
        return Mathf.Min(mayhemMaxFraction, mayhemStartFraction + mayhemFractionPerWave * (wave - 1));
    }

    float CalculateDuration()
    {
        if (GameSettings.Mode != GameMode.Mayhem) return classicDuration;

        int wave = Mathf.Max(1, mayhem != null ? mayhem.Wave : 1);
        float progress = Mathf.Clamp01((wave - 1f) / Mathf.Max(1, mayhemFastestWave - 1));
        return Mathf.Lerp(mayhemStartDuration, mayhemMinDuration, progress);
    }

    // ------------------------------------------------------------------
    //  Spawning
    // ------------------------------------------------------------------
    void Update()
    {
        if (player == null) FindPlayer();
        if (player == null) return;

        timer += Time.deltaTime;
        float interval = GameSettings.Mode == GameMode.Mayhem ? mayhemInterval : classicInterval;
        if (timer < interval) return;
        if (HealthPackPickup.All.Count >= maxPacks) return;
        if (onlyWhenHurt && !PlayerIsHurt()) return;

        if (TrySpawn()) timer = 0f;
    }

    // In Mayhem, clearing a wave always drops a pack (unless the map is already full of them)
    void OnWaveCleared(int wave, int bonus)
    {
        if (player != null && HealthPackPickup.All.Count < maxPacks + 1) TrySpawn();
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerHealth = p.GetComponent<Health>();
    }

    bool PlayerIsHurt()
    {
        return playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth * hurtBelow;
    }

    bool TrySpawn()
    {
        if (packPrefab == null || player == null) return false;

        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(mapMin.x, mapMax.x), Random.Range(mapMin.y, mapMax.y));

            if (grid != null && !grid.IsWalkable(candidate)) continue;
            if (Physics2D.OverlapCircle(candidate, clearRadius) != null) continue;
            if (Vector2.Distance(candidate, player.position) < minDistanceFromPlayer) continue;

            bool tooClose = false;
            foreach (HealthPackPickup other in HealthPackPickup.All)
            {
                if (Vector2.Distance(candidate, other.transform.position) < minDistanceBetweenPacks) { tooClose = true; break; }
            }
            if (tooClose) continue;

            Instantiate(packPrefab, candidate, Quaternion.identity);
            return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((mapMin + mapMax) / 2f, mapMax - mapMin);
    }
}
