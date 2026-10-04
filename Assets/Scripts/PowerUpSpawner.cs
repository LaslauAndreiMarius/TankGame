using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject pickupPrefab;

    [Header("Map area (inside the walls)")]
    public Vector2 mapMin = new Vector2(-19f, -14f);
    public Vector2 mapMax = new Vector2(19f, 14f);

    [Header("Spawning")]
    public float spawnInterval = 8f;
    public int maxBoxes = 4;
    public float minDistanceBetweenBoxes = 12f;   // this keeps them spread out
    public float minDistanceFromPlayer = 4f;      // not spawned right on top of you
    public float clearRadius = 1f;                // free space needed (no walls)

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        if (PowerUpPickup.All.Count >= maxBoxes) return;

        if (TryGetSpawnPoint(out Vector2 point))
        {
            GameObject box = Instantiate(pickupPrefab, point, Quaternion.identity);
            box.GetComponent<PowerUpPickup>().type = (PowerUpType)Random.Range(0, 3);
            timer = 0f;
        }
    }

    bool TryGetSpawnPoint(out Vector2 point)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(mapMin.x, mapMax.x),
                Random.Range(mapMin.y, mapMax.y));

            if (Physics2D.OverlapCircle(candidate, clearRadius) != null) continue;

            if (player != null &&
                Vector2.Distance(candidate, player.transform.position) < minDistanceFromPlayer) continue;

            bool tooClose = false;
            foreach (PowerUpPickup other in PowerUpPickup.All)
            {
                if (Vector2.Distance(candidate, other.transform.position) < minDistanceBetweenBoxes)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            point = candidate;
            return true;
        }

        point = Vector2.zero;
        return false;   // no valid spot this time, we try again next frame
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube((mapMin + mapMax) / 2f, mapMax - mapMin);
    }
}