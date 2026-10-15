using System;
using System.Collections;
using UnityEngine;

// The Mayhem game mode: endless waves of enemies that spawn all around the player.
// Every wave brings more enemies, and they use better hulls and turrets from the garage parts.
// Only runs when GameModeController finds that Mayhem was chosen in the menu.
public class MayhemManager : MonoBehaviour
{
    public enum Phase { Countdown, Fighting }

    [Header("Enemy")]
    public GameObject enemyPrefab;                          // taken from the Classic EnemySpawner when left empty
    public Color enemyTint = new Color(1f, 0.62f, 0.56f);   // the garage images are dark, so enemies get a red tint

    [Header("Waves")]
    public float firstCountdown = 4f;
    public float breakTime = 4f;
    public int enemiesFirstWave = 6;
    public int enemiesPerWave = 2;        // more enemies each wave
    public int maxAliveBase = 3;          // wave N allows (this + N) enemies alive at once...
    public int maxAliveCap = 10;          // ...up to this many
    public float levelPerWave = 0.8f;     // how quickly the enemies' parts improve (1 = one tier per wave)
    public int lateGameWave = 12;         // after this wave enemies also get tougher

    [Header("Spawning around the player")]
    public float minSpawnDistance = 13.5f;   // far enough to be outside the screen
    public float maxSpawnDistance = 22f;
    public float spawnClearRadius = 1.2f;
    public Vector2 mapMin = new Vector2(-19f, -14f);   // only used if no PathGrid is available
    public Vector2 mapMax = new Vector2(19f, 14f);

    [Header("Enemy strength (compared with the same parts on the player)")]
    public float healthScale = 0.35f;
    public float speedScale = 0.55f;
    public float damageScale = 0.6f;
    public float fireIntervalScale = 1.4f;

    [Header("Rewards")]
    public int baseCoins = 5;             // coins an enemy drops...
    public int coinsPerTier = 3;          // ...plus this for every tier of its hull and turret
    public int waveBonusPerWave = 20;     // bonus for clearing a wave = this x the wave number

    public event Action<int> WaveStarted;          // wave number
    public event Action<int, int> WaveCleared;     // wave number, bonus coins

    public int Wave { get; private set; }
    public int WaveTotal { get; private set; }
    public Phase CurrentPhase { get; private set; }
    public float CountdownLeft { get; private set; }
    public int Remaining { get { return (WaveTotal - spawned) + Alive; } }
    public static int BestWave { get { return PlayerPrefs.GetInt("TankArena.BestWave", 0); } }

    private int spawned;
    private int pending;      // enemies that are flashing in but not here yet
    private bool aborted;
    private bool hadPlayer;
    private Transform player;
    private PathGrid grid;

    int Alive { get { return EnemyAI.All.Count + pending; } }

    void Start()
    {
        if (enemyPrefab == null)
        {
            EnemySpawner classic = FindFirstObjectByType<EnemySpawner>();
            if (classic != null) enemyPrefab = classic.enemyPrefab;
        }
        if (enemyPrefab == null)
        {
            Debug.LogError("MayhemManager has no enemy prefab. Assign one, or keep the Classic EnemySpawner in the scene.");
            enabled = false;
            return;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            hadPlayer = true;
        }

        grid = PathGrid.Get();
        grid.HasLineOfSight(Vector2.zero, Vector2.zero);   // makes the grid build itself on first use

        StartCoroutine(Run());
    }

    // ------------------------------------------------------------------
    //  The wave loop
    // ------------------------------------------------------------------
    IEnumerator Run()
    {
        Wave = 1;
        yield return Countdown(firstCountdown);

        while (!aborted)
        {
            yield return PlayWave();
            if (aborted) yield break;

            // Wave cleared: pay the bonus, rest a moment, then the next wave
            int bonus = waveBonusPerWave * Wave;
            PlayerProfile.AddCoins(bonus);
            if (WaveCleared != null) WaveCleared(Wave, bonus);

            Wave++;
            yield return Countdown(breakTime);
        }
    }

    IEnumerator Countdown(float seconds)
    {
        CurrentPhase = Phase.Countdown;
        CountdownLeft = seconds;

        while (CountdownLeft > 0f)
        {
            if (PlayerGone()) { aborted = true; yield break; }
            CountdownLeft -= Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator PlayWave()
    {
        CurrentPhase = Phase.Fighting;
        WaveTotal = enemiesFirstWave + enemiesPerWave * (Wave - 1);
        spawned = 0;
        pending = 0;

        int maxAlive = Mathf.Clamp(maxAliveBase + Wave, maxAliveBase + 1, maxAliveCap);
        float interval = Mathf.Max(0.6f, 2.2f - 0.12f * Wave);

        RecordWave();
        if (WaveStarted != null) WaveStarted(Wave);

        float timer = 1.5f;   // a short pause so the "WAVE" banner can be read
        while (spawned < WaveTotal || Alive > 0)
        {
            if (PlayerGone()) { aborted = true; yield break; }

            timer -= Time.deltaTime;
            if (timer <= 0f && spawned < WaveTotal && Alive < maxAlive)
            {
                Vector2 point;
                if (TryGetSpawnPoint(out point))
                {
                    spawned++;
                    pending++;
                    StartCoroutine(SpawnEnemy(point));
                    timer = interval;
                }
                else
                {
                    timer = 0.2f;   // no free spot this time, try again very soon
                }
            }
            yield return null;
        }
    }

    bool PlayerGone()
    {
        return hadPlayer && player == null;
    }

    void RecordWave()
    {
        if (Wave > BestWave)
        {
            PlayerPrefs.SetInt("TankArena.BestWave", Wave);
            PlayerPrefs.Save();
        }

        // The death screen shows this line
        if (GameManager.Instance != null)
            GameManager.Instance.deathSummary = "WAVE " + Wave + "   BEST " + Mathf.Max(Wave, BestWave);
    }

    // ------------------------------------------------------------------
    //  Spawning
    // ------------------------------------------------------------------
    IEnumerator SpawnEnemy(Vector2 point)
    {
        // A red flash first, so enemies don't just pop into existence
        PickupBurst.Spawn(point, new Color(1f, 0.3f, 0.25f));
        yield return new WaitForSeconds(0.35f);

        GameObject enemy = Instantiate(enemyPrefab, point, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        pending--;

        if (TankCatalog.Hulls.Count == 0 || TankCatalog.Turrets.Count == 0) yield break;   // no garage parts: plain enemy

        int hullIndex = PickTier(TankCatalog.Hulls.Count);
        int turretIndex = PickTier(TankCatalog.Turrets.Count);

        // After the late-game wave, enemies keep getting a little tougher
        float late = Mathf.Max(0, Wave - lateGameWave);
        float health = healthScale * (1f + 0.1f * late);
        float damage = damageScale * (1f + 0.05f * late);
        int coins = baseCoins + coinsPerTier * (hullIndex + turretIndex);

        EnemyLoadout loadout = enemy.GetComponent<EnemyLoadout>();
        if (loadout == null) loadout = enemy.AddComponent<EnemyLoadout>();
        loadout.Apply(TankCatalog.Hulls[hullIndex], TankCatalog.Turrets[turretIndex],
                      health, speedScale, damage, fireIntervalScale, coins, enemyTint);
    }

    // Parts are sorted from the first (cheapest) to the last (best). The wave decides where in the list
    // an enemy lands: mostly around the current level, sometimes a little weaker or a little stronger.
    int PickTier(int count)
    {
        float level = (Wave - 1) * levelPerWave;
        float roll = level + UnityEngine.Random.Range(-2f, 0.75f);
        return Mathf.Clamp(Mathf.RoundToInt(roll), 0, count - 1);
    }

    // A random spot in a ring around the player that is outside the screen, on free ground
    bool TryGetSpawnPoint(out Vector2 point)
    {
        Vector2 center = player != null ? (Vector2)player.position : Vector2.zero;

        for (int i = 0; i < 40; i++)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (grid != null)
            {
                if (!grid.IsWalkable(candidate)) continue;   // inside a wall, an obstacle or outside the map
            }
            else if (candidate.x < mapMin.x || candidate.x > mapMax.x || candidate.y < mapMin.y || candidate.y > mapMax.y)
            {
                continue;
            }

            if (IsOnScreen(candidate)) continue;
            if (Physics2D.OverlapCircle(candidate, spawnClearRadius) != null) continue;

            point = candidate;
            return true;
        }

        point = Vector2.zero;
        return false;
    }

    bool IsOnScreen(Vector2 worldPoint)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 v = cam.WorldToViewportPoint(worldPoint);
        const float margin = 0.1f;
        return v.x > -margin && v.x < 1f + margin && v.y > -margin && v.y < 1f + margin;
    }
}
