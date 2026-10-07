using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds juice to a UI button: it grows when hovered, shrinks and pushes its label down
// when pressed, and plays sounds. Sprite changes (normal / hover / pressed) are handled
// by the Button's own "Sprite Swap" transition.
[RequireComponent(typeof(Button))]
public class UIButtonEffects : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Motion")]
    public float hoverScale = 1.06f;
    public float pressedScale = 0.95f;
    public float pressedLabelOffset = 6f;   // pixels the label moves down while pressed
    public float smoothing = 18f;           // higher = snappier
    public RectTransform label;             // optional: the text child that shifts when pressed

    [Header("Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private Button button;
    private bool hovered;
    private bool pressed;
    private Vector2 labelStart;
    private float scale = 1f;

    void Awake()
    {
        button = GetComponent<Button>();
        if (label != null) labelStart = label.anchoredPosition;
        button.onClick.AddListener(PlayClick);
    }

    void OnDisable()
    {
        // Reset, so a button hidden while hovered doesn't come back enlarged
        hovered = false;
        pressed = false;
        scale = 1f;
        transform.localScale = Vector3.one;
        if (label != null) label.anchoredPosition = labelStart;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        hovered = true;
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable) pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    void Update()
    {
        // Unscaled time, so menus still animate if the game is paused
        float target = !button.interactable ? 1f : pressed ? pressedScale : hovered ? hoverScale : 1f;
        float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);

        scale = Mathf.Lerp(scale, target, k);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (label != null)
        {
            Vector2 goal = labelStart + (pressed ? new Vector2(0f, -pressedLabelOffset) : Vector2.zero);
            label.anchoredPosition = Vector2.Lerp(label.anchoredPosition, goal, k);
        }
    }

    void PlayClick()
    {
        PlaySound(clickSound);
    }

    static void PlaySound(AudioClip clip)
    {
        if (clip == null || Camera.main == null) return;
        Sfx.Play(clip, Camera.main.transform.position);
    }
}
