using System.Collections.Generic;
using UnityEngine;

// A grid laid over the map. It remembers which spots a tank can stand on, and finds
// routes between two points with the A* algorithm. One PathGrid per map scene.
public class PathGrid : MonoBehaviour
{
    public static PathGrid Instance { get; private set; }

    [Header("Area covered by the grid (world units)")]
    public Vector2 min = new Vector2(-21f, -16f);
    public Vector2 max = new Vector2(21f, 16f);

    [Header("Grid")]
    public float cellSize = 0.5f;       // smaller = more precise, but more cells to search
    public float agentRadius = 0.7f;    // obstacles are "inflated" by this much so the tank doesn't scrape walls
    public LayerMask obstacleMask = ~0;

    [Header("Search")]
    public int snapSearchRadius = 8;    // if a start/goal is inside an obstacle, look this many cells around for free ground
    public int smoothingWindow = 24;    // how far ahead the path smoothing looks

    [Header("Debug")]
    public bool alwaysShowGrid = false;   // draw the grid even when this object is not selected

    private int width;
    private int height;
    private bool[] walkable;
    private bool built;

    // Working memory for A*. The "stamp" trick avoids clearing these arrays before every search.
    private float[] gCost;
    private int[] parent;
    private int[] seen;
    private int[] closed;
    private int stamp;

    private readonly MinHeap heap = new MinHeap();
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    // The 8 directions: 4 straight, then 4 diagonal
    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    // ------------------------------------------------------------------
    //  Setup
    // ------------------------------------------------------------------

    // Enemies call this. If the scene has no PathGrid, one is created with the default settings.
    public static PathGrid Get()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<PathGrid>();
        if (Instance == null)
        {
            GameObject go = new GameObject("PathGrid (auto)");
            Instance = go.AddComponent<PathGrid>();
            Debug.LogWarning("No PathGrid in this scene, so one was created with the default settings. " +
                             "Add an empty object with a PathGrid component to control the area and see its gizmos.");
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // only one grid per scene
            return;
        }
        Instance = this;
    }

    void Start()
    {
        EnsureBuilt();
    }

    void EnsureBuilt()
    {
        if (!built) Build();
    }

    // Scans the map and marks every cell as walkable or blocked.
    // Right-click the component title > "Rebuild Grid" to preview it in the Scene view without pressing Play.
    [ContextMenu("Rebuild Grid")]
    public void Build()
    {
        Physics2D.SyncTransforms();   // make sure the physics world knows where everything is

        width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt((max.y - min.y) / cellSize));
        int count = width * height;

        walkable = new bool[count];
        gCost = new float[count];
        parent = new int[count];
        seen = new int[count];
        closed = new int[count];
        stamp = 0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(obstacleMask);

        int blocked = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBlocked = IsBlocked(CellCenter(x, y), filter);
                walkable[y * width + x] = !isBlocked;
                if (isBlocked) blocked++;
            }
        }

        built = true;

        Debug.Log("PathGrid built: " + width + " x " + height + " cells, " + blocked + " blocked, " + (count - blocked) + " walkable.");
        if (blocked == 0)
        {
            Debug.LogWarning("PathGrid found NO obstacles. Check that your walls have Collider2D components (not set to 'Is Trigger'), " +
                             "that Obstacle Mask is set to 'Everything', and that Min / Max cover the map.");
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();   // redraw the Scene view so the result shows immediately
#endif
    }

    // A cell is blocked if a solid collider comes within agentRadius of its center
    bool IsBlocked(Vector2 point, ContactFilter2D filter)
    {
        int n = Physics2D.OverlapCircle(point, agentRadius, filter, overlapResults);
        for (int i = 0; i < n; i++)
        {
            if (IsSolid(overlapResults[i])) return true;
        }
        return false;
    }

    // Walls and obstacles are solid. Tanks (they have a moving Rigidbody2D), bullets and pickup boxes are not.
    public static bool IsSolid(Collider2D c)
    {
        if (c.isTrigger) return false;
        Rigidbody2D body = c.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    // ------------------------------------------------------------------
    //  Grid helpers
    // ------------------------------------------------------------------

    public Vector2 CellCenter(int x, int y)
    {
        return new Vector2(min.x + (x + 0.5f) * cellSize, min.y + (y + 0.5f) * cellSize);
    }

    public bool IsWalkable(Vector2 point)
    {
        int x = Mathf.FloorToInt((point.x - min.x) / cellSize);
        int y = Mathf.FloorToInt((point.y - min.y) / cellSize);
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        return walkable[y * width + x];
    }

    // True if the straight line from a to b stays on walkable ground the whole way.
    // The grid is already inflated by the tank's size, so a clear line means the tank can drive it.
    public bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        EnsureBuilt();

        float distance = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(distance / (cellSize * 0.5f));
        for (int i = 1; i < steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
            if (!IsWalkable(p)) return false;
        }
        return true;
    }

    // Finds the cell for a world point. If that cell is blocked, uses the nearest free one instead.
    bool NearestWalkable(Vector2 p, out int cx, out int cy)
    {
        cx = Mathf.Clamp(Mathf.FloorToInt((p.x - min.x) / cellSize), 0, width - 1);
        cy = Mathf.Clamp(Mathf.FloorToInt((p.y - min.y) / cellSize), 0, height - 1);
        if (walkable[cy * width + cx]) return true;

        float best = float.MaxValue;
        int bestX = -1;
        int bestY = -1;

        for (int r = 1; r <= snapSearchRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;   // only the outer ring

                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    if (!walkable[y * width + x]) continue;

                    float d = dx * dx + dy * dy;
                    if (d < best) { best = d; bestX = x; bestY = y; }
                }
            }
            if (bestX >= 0) break;   // found free ground in this ring
        }

        if (bestX < 0) return false;
        cx = bestX;
        cy = bestY;
        return true;
    }

    Vector2 GoalPoint(Vector2 goalWorld, int gx, int gy)
    {
        return IsWalkable(goalWorld) ? goalWorld : CellCenter(gx, gy);
    }

    // ------------------------------------------------------------------
    //  A* path search
    // ------------------------------------------------------------------

    // Returns a list of waypoints from start to goal (not including the start itself),
    // or null if no route exists.
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 goalWorld)
    {
        EnsureBuilt();

        int sx, sy, gx, gy;
        if (!NearestWalkable(startWorld, out sx, out sy)) return null;
        if (!NearestWalkable(goalWorld, out gx, out gy)) return null;

        int start = sy * width + sx;
        int goal = gy * width + gx;

        if (start == goal)
        {
            List<Vector2> single = new List<Vector2>();
            single.Add(GoalPoint(goalWorld, gx, gy));
            return single;
        }

        stamp++;
        heap.Clear();

        seen[start] = stamp;
        gCost[start] = 0f;
        parent[start] = -1;
        heap.Push(start, Heuristic(sx, sy, gx, gy));

        bool found = false;
        int iterations = 0;
        int maxIterations = width * height * 2;

        while (heap.Count > 0 && iterations++ < maxIterations)
        {
            float key;
            int current = heap.Pop(out key);

            if (closed[current] == stamp) continue;   // an outdated duplicate entry
            closed[current] = stamp;

            if (current == goal) { found = true; break; }

            int cx = current % width;
            int cy = current / width;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + DX[d];
                int ny = cy + DY[d];
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                int neighbor = ny * width + nx;
                if (!walkable[neighbor] || closed[neighbor] == stamp) continue;

                bool diagonal = DX[d] != 0 && DY[d] != 0;
                // No cutting corners: a diagonal step needs both side cells to be free
                if (diagonal && (!walkable[cy * width + nx] || !walkable[ny * width + cx])) continue;

                float newCost = gCost[current] + (diagonal ? 1.41421356f : 1f);
                if (seen[neighbor] == stamp && newCost >= gCost[neighbor]) continue;

                seen[neighbor] = stamp;
                gCost[neighbor] = newCost;
                parent[neighbor] = current;
                heap.Push(neighbor, newCost + Heuristic(nx, ny, gx, gy));
            }
        }

        if (!found) return null;

        // Walk back from the goal to the start, then flip the list around
        List<Vector2> raw = new List<Vector2>();
        int node = goal;
        while (node != start)
        {
            raw.Add(CellCenter(node % width, node / width));
            node = parent[node];
        }
        raw.Reverse();
        raw[raw.Count - 1] = GoalPoint(goalWorld, gx, gy);   // the last waypoint is the real target

        return Smooth(startWorld, raw);
    }

    // Estimated remaining cost: the shortest possible route if there were no obstacles
    float Heuristic(int x, int y, int gx, int gy)
    {
        float dx = Mathf.Abs(x - gx);
        float dy = Mathf.Abs(y - gy);
        return (dx + dy) + (1.41421356f - 2f) * Mathf.Min(dx, dy);
    }

    // The raw path zig-zags along grid cells. This keeps only the corners that are really needed,
    // by skipping ahead to the furthest waypoint that can be reached in a straight line.
    List<Vector2> Smooth(Vector2 from, List<Vector2> raw)
    {
        List<Vector2> result = new List<Vector2>();
        Vector2 anchor = from;
        int index = 0;

        while (index < raw.Count)
        {
            int farthest = index;
            int last = Mathf.Min(raw.Count - 1, index + smoothingWindow);

            for (int j = last; j > index; j--)
            {
                if (HasLineOfSight(anchor, raw[j])) { farthest = j; break; }
            }

            result.Add(raw[farthest]);
            anchor = raw[farthest];
            index = farthest + 1;
        }
        return result;
    }

    // ------------------------------------------------------------------
    //  Debug drawing (select the PathGrid object, with Gizmos turned on in the Scene view)
    // ------------------------------------------------------------------
    void OnDrawGizmos()         { if (alwaysShowGrid) DrawGrid(); }
    void OnDrawGizmosSelected() { if (!alwaysShowGrid) DrawGrid(); }

    void DrawGrid()
    {
        // The yellow rectangle is the area the grid covers. It always shows.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((min + max) / 2f, max - min);

        if (!built)
        {
            if (Application.isPlaying) return;
            Build();   // in the editor, build automatically so the preview appears
        }

        // Red squares are cells a tank can NOT enter (obstacles, made bigger by the tank's size)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Vector3 size = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.01f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!walkable[y * width + x]) Gizmos.DrawCube(CellCenter(x, y), size);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Priority queue: always gives back the entry with the lowest key (a binary min-heap)
    // ------------------------------------------------------------------
    private class MinHeap
    {
        private int[] nodes = new int[256];
        private float[] keys = new float[256];
        public int Count;

        public void Clear() { Count = 0; }

        public void Push(int node, float key)
        {
            if (Count == nodes.Length)
            {
                System.Array.Resize(ref nodes, Count * 2);
                System.Array.Resize(ref keys, Count * 2);
            }

            int i = Count++;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (keys[p] <= key) break;
                nodes[i] = nodes[p];
                keys[i] = keys[p];
                i = p;
            }
            nodes[i] = node;
            keys[i] = key;
        }

        public int Pop(out float key)
        {
            int top = nodes[0];
            key = keys[0];
            Count--;

            if (Count > 0)
            {
                int node = nodes[Count];
                float k = keys[Count];
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1;
                    if (l >= Count) break;
                    int r = l + 1;
                    int c = (r < Count && keys[r] < keys[l]) ? r : l;
                    if (keys[c] >= k) break;
                    nodes[i] = nodes[c];
                    keys[i] = keys[c];
                    i = c;
                }
                nodes[i] = node;
                keys[i] = k;
            }
            return top;
        }
    }
}
