using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [Header("Map area (inside the walls)")]
    public Vector2 mapMin = new Vector2(-19f, -14f);
    public Vector2 mapMax = new Vector2(19f, 14f);

    [Header("Spawning")]
    public float spawnInterval = 4f;
    public int maxEnemies = 5;
    public float offscreenMargin = 0.15f;   // extra distance outside the screen edge (fraction of screen)
    public float clearRadius = 1f;          // required free space around the spawn point

    private Camera cam;
    private float timer;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Stop spawning once the player is dead
        if (GameObject.FindGameObjectWithTag("Player") == null) return;

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;

        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies) return;

        if (TryGetSpawnPoint(out Vector2 point))
        {
            Instantiate(enemyPrefab, point, Quaternion.identity);
            timer = 0f;
        }
    }

    bool TryGetSpawnPoint(out Vector2 point)
    {
        // Try random spots; accept the first one that is off-screen and not inside something
        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(mapMin.x, mapMax.x),
                Random.Range(mapMin.y, mapMax.y));

            if (IsVisible(candidate)) continue;
            if (Physics2D.OverlapCircle(candidate, clearRadius) != null) continue;

            point = candidate;
            return true;
        }

        point = Vector2.zero;
        return false;   // no valid spot found this time, try again next frame
    }

    bool IsVisible(Vector2 worldPoint)
    {
        // Viewport coordinates: (0,0) is the bottom-left of the screen, (1,1) the top-right
        Vector3 v = cam.WorldToViewportPoint(worldPoint);
        float m = offscreenMargin;
        return v.x > -m && v.x < 1f + m && v.y > -m && v.y < 1f + m;
    }

    // Draws the spawn area in the Scene view when the object is selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 size = mapMax - mapMin;
        Gizmos.DrawWireCube((mapMin + mapMax) / 2f, size);
    }
}