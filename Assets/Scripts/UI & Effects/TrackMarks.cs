using UnityEngine;

public class TrackMarks : MonoBehaviour
{
    public GameObject markPrefab;
    public float trackOffset = 0.42f;    // distance from the tank's center to each track
    public float spawnDistance = 0.9f;   // a new pair of marks every this many units driven

    private Vector2 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, lastPos) < spawnDistance) return;
        lastPos = transform.position;

        Vector3 side = transform.right * trackOffset;
        Instantiate(markPrefab, transform.position - side, transform.rotation);   // left track
        Instantiate(markPrefab, transform.position + side, transform.rotation);   // right track
    }
}