using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows one card per active power-up: its icon, name, a draining bar and the seconds left.
// Cards pop in when collected, punch when refreshed, and blink in the last 3 seconds.
public class PowerUpHUD : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public PowerUpType type;
        public RectTransform card;         // the whole card (shown / hidden)
        public CanvasGroup group;          // used to fade the card
        public Image bar;                  // the draining bar (Image type: Filled)
        public TextMeshProUGUI timeText;   // "12s"

        [HideInInspector] public float scale = 0.6f;
        [HideInInspector] public float alpha;
        [HideInInspector] public float lastRemaining;
        [HideInInspector] public int lastSeconds = -1;
    }

    public Entry[] entries;

    private PowerUpReceiver receiver;
    private float findTimer;

    void Update()
    {
        if (receiver == null) TryFindPlayer();

        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);

        foreach (Entry e in entries)
        {
            if (e.card == null || e.group == null || e.bar == null || e.timeText == null) continue;

            float remaining = receiver != null ? receiver.GetRemaining(e.type) : 0f;
            bool active = remaining > 0f;
            bool visible = active || e.alpha > 0.02f;

            if (visible != e.card.gameObject.activeSelf)
            {
                e.card.gameObject.SetActive(visible);
                if (visible)
                {
                    e.scale = 0.6f;   // appearing: start small and transparent
                    e.alpha = 0f;
                    e.lastRemaining = 0f;
                    e.lastSeconds = -1;
                }
            }
            if (!visible) continue;

            // Collecting the same power-up again refreshes the timer: give the card a little punch
            if (active && e.lastRemaining > 0f && remaining > e.lastRemaining + 0.5f) e.scale = 1.18f;
            e.lastRemaining = remaining;

            float targetAlpha = active ? 1f : 0f;
            if (active && remaining < 3f)
                targetAlpha *= 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));   // blink when almost over

            e.alpha = Mathf.Lerp(e.alpha, targetAlpha, k);
            e.scale = Mathf.Lerp(e.scale, active ? 1f : 0.85f, k);
            e.group.alpha = e.alpha;
            e.card.localScale = new Vector3(e.scale, e.scale, 1f);

            if (active)
            {
                e.bar.fillAmount = remaining / receiver.duration;

                int seconds = Mathf.CeilToInt(remaining);
                if (seconds != e.lastSeconds)
                {
                    e.timeText.text = seconds + "s";
                    e.lastSeconds = seconds;
                }
            }
        }
    }

    void TryFindPlayer()
    {
        findTimer -= Time.unscaledDeltaTime;
        if (findTimer > 0f) return;
        findTimer = 0.25f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) receiver = p.GetComponent<PowerUpReceiver>();
    }
}
