using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The wave display for Mayhem: current wave, enemies left, a progress bar, a countdown between
// waves, and a big banner when a wave starts or is cleared.
public class MayhemHUD : MonoBehaviour
{
    public MayhemManager manager;

    [Header("Wave panel")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemiesText;
    public TextMeshProUGUI countdownText;
    public Image progressFill;

    [Header("Banner")]
    public CanvasGroup bannerGroup;
    public RectTransform bannerRect;
    public TextMeshProUGUI bannerTitle;
    public TextMeshProUGUI bannerSub;

    private const float BannerDuration = 2.6f;
    private float bannerTime = -1f;
    private int shownWave = -1;
    private int shownRemaining = -1;
    private int shownCountdown = -1;
    private MayhemManager.Phase shownPhase;

    void OnEnable()
    {
        if (manager == null) manager = FindFirstObjectByType<MayhemManager>();
        if (manager != null)
        {
            manager.WaveStarted += OnWaveStarted;
            manager.WaveCleared += OnWaveCleared;
        }
        if (bannerGroup != null) bannerGroup.alpha = 0f;
    }

    void OnDisable()
    {
        if (manager != null)
        {
            manager.WaveStarted -= OnWaveStarted;
            manager.WaveCleared -= OnWaveCleared;
        }
    }

    void OnWaveStarted(int wave)
    {
        ShowBanner("WAVE " + wave, manager.WaveTotal + " ENEMIES");
    }

    void OnWaveCleared(int wave, int bonus)
    {
        ShowBanner("WAVE CLEARED", "+" + bonus + " COINS");
    }

    void ShowBanner(string title, string sub)
    {
        bannerTitle.text = title;
        bannerSub.text = sub;
        bannerTime = 0f;
    }

    void Update()
    {
        if (manager == null) return;

        UpdateNumbers();
        UpdateBanner();
    }

    void UpdateNumbers()
    {
        bool fighting = manager.CurrentPhase == MayhemManager.Phase.Fighting;
        int remaining = manager.Remaining;
        int countdown = Mathf.CeilToInt(manager.CountdownLeft);

        // Text is only rewritten when something changed
        if (manager.Wave != shownWave)
        {
            shownWave = manager.Wave;
            waveText.text = "WAVE " + shownWave;
        }

        if (remaining != shownRemaining || manager.CurrentPhase != shownPhase || countdown != shownCountdown)
        {
            shownRemaining = remaining;
            shownCountdown = countdown;
            shownPhase = manager.CurrentPhase;

            enemiesText.text = fighting ? "ENEMIES  " + remaining : "GET READY";
            countdownText.text = fighting ? "" : "NEXT WAVE IN  " + Mathf.Max(0, countdown);
        }

        float progress = (fighting && manager.WaveTotal > 0) ? 1f - (float)remaining / manager.WaveTotal : 0f;
        progressFill.fillAmount = Mathf.Lerp(progressFill.fillAmount, progress, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
    }

    void UpdateBanner()
    {
        if (bannerTime < 0f) return;

        bannerTime += Time.unscaledDeltaTime;
        float t = bannerTime / BannerDuration;
        if (t >= 1f)
        {
            bannerTime = -1f;
            bannerGroup.alpha = 0f;
            return;
        }

        // Quick fade in, hold, then fade out. The banner also grows a little as it appears.
        float alpha = t < 0.1f ? t / 0.1f : (t > 0.75f ? (1f - t) / 0.25f : 1f);
        float scale = Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(t / 0.15f));
        bannerGroup.alpha = alpha;
        bannerRect.localScale = new Vector3(scale, scale, 1f);
    }
}
