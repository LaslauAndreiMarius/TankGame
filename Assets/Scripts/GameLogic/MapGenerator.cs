using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 20;
    public int mapHeight = 12;
    public int obstacleCount = 40;

    [Header("Prefabs (Assign in Inspector)")]
    public GameObject wallPrefab; 
    public GameObject[] obstaclePrefabs; 

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        // Create an empty parent object to keep the hierarchy clean
        Transform mapHolder = new GameObject("GeneratedMap").transform;

        // 1. Generate the outer walls
        for (int x = -1; x <= mapWidth; x++)
        {
            for (int y = -1; y <= mapHeight; y++)
            {
                // If it's on the edge, place a wall
                if (x == -1 || x == mapWidth || y == -1 || y == mapHeight)
                {
                    Vector2 wallPos = new Vector2(x, y);
                    GameObject newWall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
                    newWall.transform.SetParent(mapHolder);
                }
            }
        }

        // 2. Scatter obstacles randomly within the boundaries
        int placedObstacles = 0;
        int safetyNet = 0; // Prevents infinite loops if the map is too full

        while (placedObstacles < obstacleCount && safetyNet < 1000)
        {
            int randomX = Random.Range(0, mapWidth);
            int randomY = Random.Range(0, mapHeight);
            Vector2 randomPos = new Vector2(randomX, randomY);

            // Check if the space is already occupied by using Physics2D
            Collider2D isOccupied = Physics2D.OverlapCircle(randomPos, 0.1f);
            
            if (isOccupied == null)
            {
                // Select a random obstacle from the array
                GameObject obstacleToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                GameObject newObstacle = Instantiate(obstacleToSpawn, randomPos, Quaternion.identity);
                newObstacle.transform.SetParent(mapHolder);
                
                placedObstacles++;
            }
            safetyNet++;
        }
    }
}