using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup mainPanel;
    public CanvasGroup mapPanel;
    public CanvasGroup garagePanel;

    [Header("Transitions")]
    public CanvasGroup fadeOverlay;        // full-screen black image used to fade in and out
    public float panelFadeTime = 0.18f;
    public float sceneFadeTime = 0.45f;
    public float slideDistance = 40f;      // panels slide this many pixels while fading

    [Header("Sound")]
    public AudioClip startSound;           // played when a map is launched

    private CanvasGroup current;
    private bool busy;
    private readonly Dictionary<CanvasGroup, Vector2> basePositions = new Dictionary<CanvasGroup, Vector2>();

    void Awake()
    {
        Time.timeScale = 1f;   // in case we arrive here from a frozen game
    }

    void Start()
    {
        Register(mainPanel);
        Register(mapPanel);
        Register(garagePanel);

        SetState(mainPanel, 1f, true);
        SetState(mapPanel, 0f, false);
        SetState(garagePanel, 0f, false);
        current = mainPanel;

        if (fadeOverlay != null) StartCoroutine(FadeOverlayRoutine(1f, 0f));   // fade in from black
    }

    // ---- Buttons call these ----
    public void ShowMain()   { SwitchTo(mainPanel); }
    public void ShowMaps()   { SwitchTo(mapPanel); }
    public void ShowGarage() { SwitchTo(garagePanel); }

    public void LoadMap(string sceneName)
    {
        if (busy) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;   // Application.Quit() does nothing in the editor
#else
        Application.Quit();
#endif
    }

    // ---- Panel switching ----
    void Register(CanvasGroup group)
    {
        if (group != null) basePositions[group] = ((RectTransform)group.transform).anchoredPosition;
    }

    void SetState(CanvasGroup group, float alpha, bool active)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;
        ((RectTransform)group.transform).anchoredPosition = basePositions[group];
    }

    void SwitchTo(CanvasGroup next)
    {
        if (busy || next == null || next == current) return;
        StartCoroutine(SwitchRoutine(next));
    }

    IEnumerator SwitchRoutine(CanvasGroup next)
    {
        busy = true;
        CanvasGroup from = current;

        from.interactable = false;
        from.blocksRaycasts = false;
        yield return Animate(from, 1f, 0f, 0f, slideDistance);        // old panel fades out, slides up

        next.blocksRaycasts = true;
        yield return Animate(next, 0f, 1f, -slideDistance, 0f);       // new panel fades in from below
        next.interactable = true;

        current = next;
        busy = false;
    }

    IEnumerator Animate(CanvasGroup group, float fromAlpha, float toAlpha, float fromY, float toY)
    {
        RectTransform rt = (RectTransform)group.transform;
        Vector2 basePos = basePositions[group];
        float t = 0f;

        while (t < panelFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / panelFadeTime));
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, k);
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Lerp(fromY, toY, k));
            yield return null;
        }

        group.alpha = toAlpha;
        rt.anchoredPosition = basePos + new Vector2(0f, toY);
    }

    // ---- Starting a map ----
    IEnumerator LoadRoutine(string sceneName)
    {
        busy = true;

        if (startSound != null && Camera.main != null)
            Sfx.Play(startSound, Camera.main.transform.position);

        if (fadeOverlay != null) yield return FadeOverlayRoutine(0f, 1f);   // fade to black

        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOverlayRoutine(float from, float to)
    {
        fadeOverlay.blocksRaycasts = true;
        fadeOverlay.alpha = from;
        float t = 0f;

        while (t < sceneFadeTime)
        {
            t += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, t / sceneFadeTime);
            yield return null;
        }

        fadeOverlay.alpha = to;
        fadeOverlay.blocksRaycasts = to > 0.5f;
    }
}
