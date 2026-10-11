using UnityEngine;

// A coin lying on the map. Drive into it to collect it.
public class CoinPickup : MonoBehaviour
{
    public int value = 5;
    public float lifetime = 20f;       // disappears if you don't take it
    public float blinkTime = 5f;
    public float magnetRadius = 0f;    // 0 = you must drive into the coin. Raise it (e.g. 3) to pull coins toward you.
    public float magnetSpeed = 9f;
    public AudioClip pickupSound;

    private static GameObject prefab;

    private SpriteRenderer sr;
    private Transform visual;
    private Transform player;
    private Vector3 baseScale;
    private float age;
    private float phase;
    private bool collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        prefab = null;
    }

    // Spawns the coins an enemy drops. A bigger reward is split into several coins (up to 4).
    public static void Drop(Vector2 position, int totalValue)
    {
        if (totalValue <= 0) return;

        if (prefab == null) prefab = Resources.Load<GameObject>("CoinPickup");
        if (prefab == null)
        {
            Debug.LogWarning("CoinPickup prefab not found. Run Tools > Tank Game > Setup Garage Assets.");
            return;
        }

        int coins = Mathf.Clamp(Mathf.CeilToInt(totalValue / 5f), 1, 4);
        int each = Mathf.Max(1, totalValue / coins);
        int remainder = totalValue - each * coins;

        for (int i = 0; i < coins; i++)
        {
            Vector2 spot = position + (coins > 1 ? Random.insideUnitCircle * 0.45f : Vector2.zero);
            GameObject go = Instantiate(prefab, spot, Quaternion.identity);
            go.GetComponent<CoinPickup>().value = each + (i == 0 ? remainder : 0);
        }
    }

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        visual = sr != null ? sr.transform : transform;
        baseScale = visual.localScale;
        phase = Random.value * 6.28f;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Spin: squash the coin sideways like a coin turning on its edge
        float spin = Mathf.Abs(Mathf.Cos((Time.time + phase) * 4f));
        visual.localScale = new Vector3(baseScale.x * Mathf.Lerp(0.25f, 1f, spin), baseScale.y, baseScale.z);

        // Blink in the last seconds
        if (sr != null)
        {
            bool faded = lifetime - age < blinkTime && Mathf.Sin(age * 18f) < 0f;
            Color c = sr.color;
            c.a = faded ? 0.3f : 1f;
            sr.color = c;
        }

        // Optional magnet
        if (magnetRadius > 0f && player != null &&
            Vector2.Distance(transform.position, player.position) < magnetRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;

        PlayerProfile.AddCoins(value);
        Sfx.Play(pickupSound, transform.position);
        PickupBurst.Spawn(transform.position, new Color(1f, 0.82f, 0.25f));

        Destroy(gameObject);
    }
}
