using UnityEngine;

// The expanding ring and flash that appear when a power-up is collected.
public class PickupBurst : MonoBehaviour
{
    public float duration = 0.45f;

    private SpriteRenderer ring;
    private SpriteRenderer flash;
    private Color color;
    private float t;

    public static void Spawn(Vector3 position, Color color)
    {
        GameObject go = new GameObject("PickupBurst");
        go.transform.position = position;

        PickupBurst burst = go.AddComponent<PickupBurst>();
        burst.color = color;
        burst.flash = burst.NewLayer("Flash", GlowSprite.Get(), 5);
        burst.ring = burst.NewLayer("Ring", GlowSprite.GetRing(), 6);
    }

    SpriteRenderer NewLayer(string layerName, Sprite sprite, int order)
    {
        GameObject go = new GameObject(layerName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float ease = 1f - (1f - k) * (1f - k);   // fast at first, then slows down

        ring.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.4f, ease);
        Color r = color;
        r.a = 1f - k;
        ring.color = r;

        flash.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 2.6f, ease);
        Color f = Color.Lerp(color, Color.white, 0.5f);
        f.a = (1f - k) * 0.8f;
        flash.color = f;

        if (k >= 1f) Destroy(gameObject);
    }
}
