using UnityEngine;

// Soft round sprites made in code, so no image files are needed for glows and rings.
public static class GlowSprite
{
    private static Sprite glow;
    private static Sprite ring;

    public static Sprite Get()
    {
        if (glow == null) glow = Make(false);
        return glow;
    }

    public static Sprite GetRing()
    {
        if (ring == null) ring = Make(true);
        return ring;
    }

    static Sprite Make(bool asRing)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;   // keeps Unity from deleting it between scenes

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a = asRing
                    ? Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d - 0.78f) / 0.16f), 1.5f)
                    : Mathf.Pow(Mathf.Clamp01(1f - d), 2f);

                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);   // exactly 1 unit wide
        s.hideFlags = HideFlags.HideAndDontSave;
        return s;
    }
}
