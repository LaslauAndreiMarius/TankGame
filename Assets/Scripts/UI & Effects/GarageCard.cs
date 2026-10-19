using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One clickable card in the garage list (a hull or a turret).
public class GarageCard : MonoBehaviour
{
    public Button button;
    public Image background;
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statusText;   // "OWNED" / "EQUIPPED"
    public GameObject priceRow;          // coin icon + price, shown while the part is still locked
    public TextMeshProUGUI priceText;
    public Sprite normalSprite;
    public Sprite selectedSprite;

    private static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    private static readonly Color Green = new Color32(0x6F, 0xBF, 0x3F, 255);
    private static readonly Color Red = new Color32(0xD6, 0x3A, 0x33, 255);

    public void Show(string title, Sprite sprite, bool owned, bool equipped, bool selected, int price, bool affordable)
    {
        nameText.text = title;

        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.rectTransform.sizeDelta = sprite.rect.size * 2f;   // whole-number scale keeps pixel art sharp
        }

        background.sprite = selected ? selectedSprite : normalSprite;

        priceRow.SetActive(!owned);
        statusText.gameObject.SetActive(owned);

        if (!owned)
        {
            priceText.text = price.ToString();
            priceText.color = affordable ? Cream : Red;
        }
        else
        {
            statusText.text = equipped ? "EQUIPPED" : "OWNED";
            statusText.color = equipped ? Green : Cream;
        }
    }
}
