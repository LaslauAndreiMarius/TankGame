using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The CLASSIC / MAYHEM switch in the map panel of the main menu.
public class ModeSelectorUI : MonoBehaviour
{
    public Button classicButton;
    public Button mayhemButton;
    public Image classicImage;
    public Image mayhemImage;
    public TextMeshProUGUI description;

    private static readonly Color Active = new Color32(0xFF, 0xC8, 0x80, 255);
    private static readonly Color Inactive = new Color32(0xB0, 0xB0, 0xB0, 255);

    void Awake()
    {
        classicButton.onClick.AddListener(() => Choose(GameMode.Classic));
        mayhemButton.onClick.AddListener(() => Choose(GameMode.Mayhem));
    }

    void OnEnable()
    {
        Refresh();
    }

    void Choose(GameMode mode)
    {
        GameSettings.Mode = mode;
        Refresh();
    }

    void Refresh()
    {
        GameMode mode = GameSettings.Mode;
        classicImage.color = mode == GameMode.Classic ? Active : Inactive;
        mayhemImage.color = mode == GameMode.Mayhem ? Active : Inactive;
        description.text = GameSettings.Description(mode);
    }
}
