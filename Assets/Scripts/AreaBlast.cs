using System.Collections.Generic;
using UnityEngine;

// The visual for explosions and electric zones: an expanding ring and flash,
// plus crackling lightning arcs for the electric kind. Made entirely in code.
public class AreaBlast : MonoBehaviour
{
    private const float Duration = 0.4f;

    private SpriteRenderer ring;
    private SpriteRenderer flash;
    private readonly List<SpriteRenderer> bolts = new List<SpriteRenderer>();
    private Color color;
    private float radius;
    private float t;

    public static void Spawn(Vector2 position, float radius, Color color, bool lightning)
    {
        GameObject go = new GameObject("AreaBlast");
        go.transform.position = position;

        AreaBlast blast = go.AddComponent<AreaBlast>();
        blast.color = color;
        blast.radius = radius;
        blast.flash = blast.NewSprite(GlowSprite.Get(), 5);
        blast.ring = blast.NewSprite(GlowSprite.GetRing(), 6);
        if (lightning) blast.BuildBolts();
    }

    SpriteRenderer NewSprite(Sprite sprite, int order)
    {
        GameObject go = new GameObject("Part");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // Several jagged lines from the center to the edge of the zone
    void BuildBolts()
    {
        for (int arc = 0; arc < 5; arc++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 end = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.9f;
            Vector2 side = new Vector2(-end.y, end.x).normalized;

            const int segments = 6;
            Vector2 previous = Vector2.zero;
            for (int s = 1; s <= segments; s++)
            {
                Vector2 point = end * (s / (float)segments);
                if (s < segments) point += side * Random.Range(-0.3f, 0.3f);

                AddSegment(previous, point);
                previous = point;
            }
        }
    }

    void AddSegment(Vector2 a, Vector2 b)
    {
        SpriteRenderer sr = NewSprite(GlowSprite.Get(), 7);
        Vector2 delta = b - a;
        sr.transform.localPosition = (a + b) * 0.5f;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        sr.transform.localScale = new Vector3(delta.magnitude * 1.3f, 0.28f, 1f);
        bolts.Add(sr);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / Duration);
        float ease = 1f - (1f - k) * (1f - k);

        ring.transform.localScale = Vector3.one * (radius * 2f * Mathf.Lerp(0.3f, 1f, ease));
        Color r = color;
        r.a = 1f - k;
        ring.color = r;

        flash.transform.localScale = Vector3.one * (radius * 2f * Mathf.Lerp(0.6f, 1f, ease));
        Color f = color;
        f.a = (1f - k) * 0.5f;
        flash.color = f;

        foreach (SpriteRenderer bolt in bolts)
        {
            Color b = Color.Lerp(color, Color.white, 0.7f);
            b.a = (1f - k) * (Random.value > 0.3f ? 1f : 0.4f);   // flicker
            bolt.color = b;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
