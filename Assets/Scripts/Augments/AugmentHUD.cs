using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the equipped augment in the corner of the screen (icon, key, READY / ACTIVE / seconds until recharged)
// and draws the screen glow of whatever augment provides one (IScreenEffect).
public class AugmentHUD : MonoBehaviour
{
    [Header("Widget")]
    public GameObject widget;
    public Image icon;
    public Image cooldownOverlay;    // a dark sweep that shrinks while the augment recharges
    public Image readyGlow;          // the orange frame that pulses when it is ready
    public GameObject keyBadge;
    public TextMeshProUGUI keyText;
    public TextMeshProUGUI stateText;
    public UIPunch punch;

    [Header("Screen glow")]
    public Image vignette;

    private static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    private static readonly Color Green = new Color32(0x6F, 0xBF, 0x3F, 255);
    private static readonly Color Orange = new Color32(0xE8, 0x87, 0x2B, 255);

    private bool wasReady = true;
    private int shownSeconds = -1;
    private string shownState = "";

    void Update()
    {
        AugmentAbility ability = AugmentController.Instance != null ? AugmentController.Instance.Current : null;

        if (ability == null)
        {
            widget.SetActive(false);
            SetVignette(Color.white, 0f);
            return;
        }

        widget.SetActive(true);
        if (icon.sprite != ability.Definition.icon) icon.sprite = ability.Definition.icon;

        IActivatable activatable = ability as IActivatable;
        keyBadge.SetActive(activatable != null);

        if (activatable == null) ShowPassive();
        else ShowActivatable(activatable);

        IScreenEffect effect = ability as IScreenEffect;
        if (effect != null) SetVignette(effect.ScreenColor, effect.ScreenIntensity);
        else SetVignette(Color.white, 0f);
    }

    void ShowPassive()
    {
        cooldownOverlay.fillAmount = 0f;
        SetGlow(0f);
        SetState("PASSIVE", Cream);
    }

    void ShowActivatable(IActivatable a)
    {
        keyText.text = a.ActivationKey.ToString();

        if (a.IsReady)
        {
            cooldownOverlay.fillAmount = 0f;
            SetGlow(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f));   // pulsing frame: ready to use
            SetState("READY", Green);
            if (!wasReady) punch.Punch();                                  // pop the moment it recharges
        }
        else if (a.IsActive)
        {
            cooldownOverlay.fillAmount = 0f;
            SetGlow(1f);
            SetState("ACTIVE " + Mathf.CeilToInt(a.SecondsLeft), Orange);
        }
        else
        {
            cooldownOverlay.fillAmount = 1f - a.CooldownProgress;          // the dark sweep melts away as it recharges
            SetGlow(0f);
            SetState(Mathf.CeilToInt(a.SecondsLeft) + "s", Cream);
        }

        wasReady = a.IsReady;
    }

    void SetState(string text, Color color)
    {
        if (text != shownState)   // only rewrite the text when it changes
        {
            shownState = text;
            stateText.text = text;
        }
        stateText.color = color;
    }

    void SetGlow(float alpha)
    {
        Color c = readyGlow.color;
        c.a = alpha;
        readyGlow.color = c;
    }

    void SetVignette(Color color, float intensity)
    {
        Color c = color;
        c.a = Mathf.Lerp(vignette.color.a, Mathf.Clamp01(intensity) * 0.6f, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        vignette.color = c;
    }
}
