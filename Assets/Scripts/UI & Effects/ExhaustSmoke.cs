using UnityEngine;

public class ExhaustSmoke : MonoBehaviour
{
    public GameObject smokePrefab;
    public Transform exhaustPoint;     // empty child at the rear of the tank
    public float interval = 0.2f;

    private Rigidbody2D rb;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb.linearVelocity.magnitude < 0.5f) return;   // only while moving

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            Instantiate(smokePrefab, exhaustPoint.position, Quaternion.identity);
        }
    }
}