using UnityEngine;

public class FadeAndDestroy : MonoBehaviour
{
    public float lifetime = 6f;

    private SpriteRenderer sr;
    private float startAlpha;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startAlpha = sr.color.a;   // the opacity set in the prefab
    }

    void Update()
    {
        timer += Time.deltaTime;

        Color c = sr.color;
        c.a = startAlpha * (1f - timer / lifetime);
        sr.color = c;

        if (timer >= lifetime) Destroy(gameObject);
    }
}