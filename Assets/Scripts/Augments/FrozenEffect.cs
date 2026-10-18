using System.Collections.Generic;
using UnityEngine;

// Added to an enemy while it is frozen: it stops the tank, tints it icy blue, surrounds it with a frosty
// glow and a slowly turning snowflake, and blinks shortly before the ice melts. Remove() brings it back to life.
public class FrozenEffect : MonoBehaviour
{
    private static readonly Color IceColor = new Color(0.55f, 0.85f, 1f);

    private IFreezable freezable;
    private Rigidbody2D body;
    private RigidbodyConstraints2D oldConstraints;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private SpriteRenderer glow;
    private readonly List<SpriteRenderer> snowflake = new List<SpriteRenderer>();
    private Transform decoration;
    private float endTime;
    private bool restored;

    public void Begin(float seconds)
    {
        endTime = Time.time + seconds;

        // Remember how the tank looked, so it can be restored exactly
        renderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;

        // Stop it
        freezable = GetComponent<IFreezable>();
        if (freezable != null) freezable.SetFrozen(true);

        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            oldConstraints = body.constraints;
            body.constraints = RigidbodyConstraints2D.FreezeAll;   // it can't even be pushed
        }

        BuildDecoration();
    }

    void BuildDecoration()
    {
        GameObject root = new GameObject("FrostEffect");
        decoration = root.transform;
        decoration.SetParent(transform, false);

        // Soft icy glow under the tank
        glow = NewPart("Glow", GlowSprite.Get(), -2);
        glow.transform.localScale = Vector3.one * 3.2f;

        // A snowflake made of three crossing glowing bars
        for (int i = 0; i < 3; i++)
        {
            SpriteRenderer bar = NewPart("Bar" + i, GlowSprite.Get(), 8);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            bar.transform.localScale = new Vector3(2.2f, 0.17f, 1f);
            bar.color = new Color(0.85f, 0.97f, 1f, 0.9f);
            snowflake.Add(bar);
        }
    }

    SpriteRenderer NewPart(string partName, Sprite sprite, int order)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(decoration, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    void Update()
    {
        if (restored) return;

        float left = endTime - Time.time;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);

        // In the last moments the ice flickers, a warning that the enemy is about to wake up
        float strength = 0.75f;
        if (left < 1.5f) strength = Mathf.Lerp(0.2f, 0.75f, 0.5f + 0.5f * Mathf.Sin(Time.time * 16f));

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = Color.Lerp(originalColors[i], new Color(IceColor.r, IceColor.g, IceColor.b, originalColors[i].a), strength);
        }

        glow.color = new Color(0.5f, 0.9f, 1f, (0.35f + 0.2f * pulse) * (left < 1.5f ? strength : 1f));
        decoration.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.z + Time.time * 25f);   // upright, turning slowly
    }

    // Called when the freeze ends
    public void Remove()
    {
        Restore();
        Destroy(this);
    }

    void OnDestroy()
    {
        Restore();
    }

    void Restore()
    {
        if (restored) return;
        restored = true;

        if (freezable != null) freezable.SetFrozen(false);
        if (body != null) body.constraints = oldConstraints;

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = originalColors[i];
        }

        if (decoration != null) Destroy(decoration.gameObject);
    }
}
