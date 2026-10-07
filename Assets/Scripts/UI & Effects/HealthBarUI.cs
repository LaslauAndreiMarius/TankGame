using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the player's health: a colored bar, a slower "damage trail" behind it,
// the numbers, and a pulsing red screen edge when health is low.
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public Image fill;                 // the main bar (Image type: Filled)
    public Image trail;                // the orange bar behind it that drains slowly
    public TextMeshProUGUI valueText;  // "72 / 100"
    public Image lowHealthVignette;    // full-screen red edge glow

    [Header("Colors")]
    public Color healthyColor = new Color(0.45f, 0.80f, 0.25f);
    public Color midColor = new Color(0.95f, 0.75f, 0.15f);
    public Color lowColor = new Color(0.85f, 0.18f, 0.15f);

    [Header("Behavior")]
    public float fillSpeed = 12f;          // how fast the main bar follows the real value
    public float trailDelay = 0.45f;       // seconds before the trail starts draining
    public float trailSpeed = 0.5f;        // trail drain speed (fraction of the bar per second)
    public float lowHealthThreshold = 0.3f;

    private Health target;
    private bool hadPlayer;
    private float shownFill = 1f;
    private float shownTrail = 1f;
    private int lastCurrent = -1;
    private int lastMax = -1;
    private float lastDamageTime = -10f;
    private float findTimer;

    void Update()
    {
        if (target == null && !hadPlayer) TryFindPlayer();

        int max = lastMax > 0 ? lastMax : 100;
        int current;

        if (target != null)
        {
            hadPlayer = true;
            max = Mathf.Max(1, target.MaxHealth);
            current = target.CurrentHealth;
        }
        else
        {
            current = hadPlayer ? 0 : max;   // the player object is gone = dead
        }

        float goal = Mathf.Clamp01((float)current / max);

        if (lastCurrent >= 0 && current < lastCurrent) lastDamageTime = Time.unscaledTime;

        if (current != lastCurrent || max != lastMax)
        {
            if (valueText != null) valueText.text = current + " / " + max;
            lastCurrent = current;
            lastMax = max;
        }

        float k = 1f - Mathf.Exp(-fillSpeed * Time.unscaledDeltaTime);
        shownFill = Mathf.Lerp(shownFill, goal, k);

        if (goal > shownTrail)
            shownTrail = goal;   // healing: the trail jumps up with the bar
        else if (Time.unscaledTime - lastDamageTime > trailDelay)
            shownTrail = Mathf.MoveTowards(shownTrail, goal, trailSpeed * Time.unscaledDeltaTime);

        if (fill != null)
        {
            fill.fillAmount = shownFill;
            fill.color = ColorFor(goal);
        }
        if (trail != null) trail.fillAmount = shownTrail;

        UpdateVignette(goal, k);
    }

    void UpdateVignette(float goal, float k)
    {
        if (lowHealthVignette == null) return;

        float alpha = 0f;
        if (hadPlayer && goal <= 0f) alpha = 0.3f;
        else if (goal <= lowHealthThreshold) alpha = 0.18f + 0.14f * Mathf.Sin(Time.unscaledTime * 6f);

        Color c = lowHealthVignette.color;
        c.a = Mathf.Lerp(c.a, alpha, k);
        lowHealthVignette.color = c;
    }

    Color ColorFor(float f)
    {
        return f > 0.5f
            ? Color.Lerp(midColor, healthyColor, (f - 0.5f) * 2f)
            : Color.Lerp(lowColor, midColor, f * 2f);
    }

    void TryFindPlayer()
    {
        findTimer -= Time.unscaledDeltaTime;
        if (findTimer > 0f) return;
        findTimer = 0.25f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.GetComponent<Health>();
    }
}
