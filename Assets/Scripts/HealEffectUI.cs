using UnityEngine;
using UnityEngine.UI;

// The green glow at the edges of the screen while the player is regenerating health.
// It is the green twin of the red low-health warning.
public class HealEffectUI : MonoBehaviour
{
    public Image vignette;
    public Color color = new Color(0.25f, 1f, 0.4f, 0f);

    private HealthRegen regen;
    private float findTimer;

    void Update()
    {
        if (regen == null) TryFindPlayer();

        float target = 0f;
        if (regen != null && regen.IsHealing)
            target = (0.28f + 0.1f * Mathf.Sin(Time.unscaledTime * 5f)) * regen.Intensity;   // a gentle pulse

        Color c = color;
        c.a = Mathf.Lerp(vignette.color.a, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        vignette.color = c;
    }

    // The component only exists on the player after the first pack was collected, so keep looking
    void TryFindPlayer()
    {
        findTimer -= Time.unscaledDeltaTime;
        if (findTimer > 0f) return;
        findTimer = 0.25f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) regen = p.GetComponent<HealthRegen>();
    }
}
