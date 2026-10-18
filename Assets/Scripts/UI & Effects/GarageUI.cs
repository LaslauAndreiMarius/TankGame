using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The garage screen: pick a hull, a turret and an augment, see how the tank looks and what it can do,
// buy new parts with coins and equip them. Built by Tools > Tank Game > Build Garage UI,
// and extended with the AUGMENTS tab by Tools > Tank Game > Add Augments Tab To Garage.
[RequireComponent(typeof(CanvasGroup))]
public class GarageUI : MonoBehaviour
{
    [System.Serializable]
    public class StatRow
    {
        public TextMeshProUGUI label;
        public Image fill;
        public TextMeshProUGUI value;

        public void Set(string labelText, float normalized, string valueText)
        {
            label.text = labelText;
            fill.fillAmount = Mathf.Clamp01(normalized);
            value.text = valueText;
        }
    }

    private enum Tab { Hulls, Turrets, Augments }

    [Header("Header")]
    public TextMeshProUGUI coinsText;

    [Header("Tabs")]
    public Button hullTabButton;
    public Button turretTabButton;
    public Image hullTabImage;
    public Image turretTabImage;

    [Header("Cards")]
    public RectTransform cardGrid;
    public GarageCard cardTemplate;

    [Header("Preview")]
    public RectTransform previewArea;
    public Image hullPreview;
    public Image turretPreview;
    public TextMeshProUGUI previewTitle;

    [Header("Stats")]
    public TextMeshProUGUI hullHeader;
    public StatRow hullSpeed;
    public StatRow hullTurning;
    public StatRow hullHealth;
    public TextMeshProUGUI turretHeader;
    public StatRow turretDamage;
    public StatRow turretRate;
    public StatRow turretRange;
    public Image bulletIcon;
    public TextMeshProUGUI bulletName;

    [Header("Action")]
    public Button actionButton;
    public TextMeshProUGUI actionLabel;
    public TextMeshProUGUI messageText;

    [Header("Augments (added by Add Augments Tab To Garage)")]
    public Button augmentTabButton;
    public Image augmentTabImage;
    public GameObject statsRoot;            // the hull / turret stats panel
    public GameObject augmentInfoRoot;      // shown instead of the stats while the AUGMENTS tab is open
    public TextMeshProUGUI augmentName;
    public TextMeshProUGUI augmentDescription;
    public TextMeshProUGUI augmentDetails;

    private static readonly Color ActiveTab = new Color32(0xFF, 0xC8, 0x80, 255);
    private static readonly Color InactiveTab = new Color32(0xB0, 0xB0, 0xB0, 255);

    private CanvasGroup group;
    private bool wasVisible;
    private Tab tab = Tab.Hulls;
    private HullBase selectedHull;
    private TurretBase selectedTurret;
    private AugmentBase selectedAugment;
    private readonly List<GarageCard> cards = new List<GarageCard>();
    private float turretAngle;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);

        hullTabButton.onClick.AddListener(() => ShowTab(Tab.Hulls));
        turretTabButton.onClick.AddListener(() => ShowTab(Tab.Turrets));
        if (augmentTabButton != null) augmentTabButton.onClick.AddListener(() => ShowTab(Tab.Augments));
        actionButton.onClick.AddListener(OnAction);
    }

    void OnEnable()
    {
        PlayerProfile.Changed += OnProfileChanged;
        AugmentProfile.Changed += OnProfileChanged;
    }

    void OnDisable()
    {
        PlayerProfile.Changed -= OnProfileChanged;
        AugmentProfile.Changed -= OnProfileChanged;
    }

    void OnProfileChanged()
    {
        if (wasVisible) RefreshAll();
    }

    void Update()
    {
        // The menu fades this panel in and out, so notice when it becomes visible
        bool visible = group.alpha > 0.01f;
        if (visible && !wasVisible) Open();
        wasVisible = visible;

        if (visible) AimPreviewTurret();
    }

    // ------------------------------------------------------------------
    //  Opening and switching
    // ------------------------------------------------------------------
    void Open()
    {
        if (TankCatalog.Hulls.Count == 0 || TankCatalog.Turrets.Count == 0)
        {
            messageText.text = "No tank parts found. Run Tools > Tank Game > Setup Garage Assets.";
            return;
        }

        selectedHull = PlayerProfile.EquippedHull ?? TankCatalog.StarterHull;
        selectedTurret = PlayerProfile.EquippedTurret ?? TankCatalog.StarterTurret;
        selectedAugment = AugmentProfile.Equipped ?? FirstAugment();
        tab = Tab.Hulls;
        messageText.text = "";

        RebuildCards();
        RefreshAll();
    }

    static AugmentBase FirstAugment()
    {
        return AugmentCatalog.All.Count > 0 ? AugmentCatalog.All[0] : null;
    }

    void ShowTab(Tab newTab)
    {
        if (selectedHull == null) return;
        tab = newTab;
        messageText.text = "";
        RebuildCards();
        RefreshAll();
    }

    void SelectHull(HullBase hull)
    {
        selectedHull = hull;
        messageText.text = "";
        RefreshAll();
    }

    void SelectTurret(TurretBase turret)
    {
        selectedTurret = turret;
        messageText.text = "";
        RefreshAll();
    }

    void SelectAugment(AugmentBase augment)
    {
        selectedAugment = augment;
        messageText.text = "";
        RefreshAll();
    }

    // ------------------------------------------------------------------
    //  Cards
    // ------------------------------------------------------------------
    void RebuildCards()
    {
        foreach (GarageCard old in cards)
        {
            if (old == null) continue;
            old.gameObject.SetActive(false);   // leaves the grid layout right away
            Destroy(old.gameObject);
        }
        cards.Clear();

        if (tab == Tab.Turrets)
        {
            foreach (TurretBase t in TankCatalog.Turrets)
            {
                TurretBase turret = t;   // a copy for the click handler below
                GarageCard card = NewCard();
                card.button.onClick.AddListener(() => SelectTurret(turret));
                cards.Add(card);
            }
        }
        else if (tab == Tab.Augments)
        {
            foreach (AugmentBase a in AugmentCatalog.All)
            {
                AugmentBase augment = a;
                GarageCard card = NewCard();
                card.button.onClick.AddListener(() => SelectAugment(augment));
                cards.Add(card);
            }
        }
        else
        {
            foreach (HullBase h in TankCatalog.Hulls)
            {
                HullBase hull = h;
                GarageCard card = NewCard();
                card.button.onClick.AddListener(() => SelectHull(hull));
                cards.Add(card);
            }
        }
    }

    GarageCard NewCard()
    {
        GarageCard card = Instantiate(cardTemplate, cardGrid);
        card.gameObject.SetActive(true);
        return card;
    }

    void RefreshCards()
    {
        int coins = PlayerProfile.Coins;

        if (tab == Tab.Turrets)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                TurretBase t = TankCatalog.Turrets[i];
                cards[i].Show(t.DisplayName, t.sprite, PlayerProfile.OwnsTurret(t.Id),
                    t == PlayerProfile.EquippedTurret, t == selectedTurret, t.Price, coins >= t.Price);
            }
        }
        else if (tab == Tab.Augments)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                AugmentBase a = AugmentCatalog.All[i];
                cards[i].Show(a.CardName, a.icon, AugmentProfile.Owns(a.Id),
                    a == AugmentProfile.Equipped, a == selectedAugment, a.Price, coins >= a.Price);
            }
        }
        else
        {
            for (int i = 0; i < cards.Count; i++)
            {
                HullBase h = TankCatalog.Hulls[i];
                cards[i].Show(h.DisplayName, h.sprite, PlayerProfile.OwnsHull(h.Id),
                    h == PlayerProfile.EquippedHull, h == selectedHull, h.Price, coins >= h.Price);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Refreshing everything on screen
    // ------------------------------------------------------------------
    void RefreshAll()
    {
        if (selectedHull == null || selectedTurret == null) return;

        coinsText.text = PlayerProfile.Coins.ToString();
        hullTabImage.color = tab == Tab.Hulls ? ActiveTab : InactiveTab;
        turretTabImage.color = tab == Tab.Turrets ? ActiveTab : InactiveTab;
        if (augmentTabImage != null) augmentTabImage.color = tab == Tab.Augments ? ActiveTab : InactiveTab;

        RefreshCards();
        RefreshPreview();
        RefreshStats();
        RefreshAction();
    }

    void RefreshPreview()
    {
        HullBase h = selectedHull;
        TurretBase t = selectedTurret;
        if (h.sprite == null || t.sprite == null) return;

        // Pick the biggest whole-number zoom that still fits the turret in every direction
        float ppu = h.sprite.pixelsPerUnit;
        Vector2 mount = h.TurretMount * ppu;
        float reach = Mathf.Max(h.sprite.rect.height * 0.5f, mount.magnitude + t.MuzzleLength * t.sprite.pixelsPerUnit);
        float area = Mathf.Min(previewArea.rect.width, previewArea.rect.height);
        float scale = Mathf.Max(2f, Mathf.Floor(area * 0.5f / reach));

        hullPreview.sprite = h.sprite;
        hullPreview.rectTransform.sizeDelta = h.sprite.rect.size * scale;
        hullPreview.rectTransform.anchoredPosition = Vector2.zero;

        // The turret image turns around its pivot, which sits on the hull's turret ring
        turretPreview.sprite = t.sprite;
        turretPreview.rectTransform.sizeDelta = t.sprite.rect.size * scale;
        turretPreview.rectTransform.pivot = new Vector2(
            t.sprite.pivot.x / t.sprite.rect.width, t.sprite.pivot.y / t.sprite.rect.height);
        turretPreview.rectTransform.anchoredPosition = mount * scale;

        previewTitle.text = h.DisplayName + "  +  " + t.DisplayName;
    }

    // The turret in the preview follows your mouse, like in the game
    void AimPreviewTurret()
    {
        if (turretPreview == null || turretPreview.sprite == null || Mouse.current == null) return;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                previewArea, Mouse.current.position.ReadValue(), null, out local)) return;

        Vector2 direction = local - turretPreview.rectTransform.anchoredPosition;
        float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        turretAngle = Mathf.LerpAngle(turretAngle, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        turretPreview.rectTransform.localRotation = Quaternion.Euler(0f, 0f, turretAngle);
    }

    void RefreshStats()
    {
        // The AUGMENTS tab swaps the stats panel for a description of the selected augment
        bool augments = tab == Tab.Augments && augmentInfoRoot != null && statsRoot != null;
        if (statsRoot != null) statsRoot.SetActive(!augments);
        if (augmentInfoRoot != null) augmentInfoRoot.SetActive(augments);

        if (augments)
        {
            RefreshAugmentInfo();
            return;
        }

        HullBase h = selectedHull;
        TurretBase t = selectedTurret;
        HullBase eh = PlayerProfile.EquippedHull ?? h;       // what you are using now (for the +/- comparison)
        TurretBase et = PlayerProfile.EquippedTurret ?? t;

        hullHeader.text = "HULL  " + h.DisplayName + "  " + StateTag(PlayerProfile.OwnsHull(h.Id), h == eh, h.Price);
        hullSpeed.Set("SPEED", h.MoveSpeed / TankCatalog.MaxMoveSpeed, Value(h.MoveSpeed, eh.MoveSpeed, "0.0"));
        hullTurning.Set("TURNING", h.TurnSpeed / TankCatalog.MaxTurnSpeed, Value(h.TurnSpeed, eh.TurnSpeed, "0"));
        hullHealth.Set("HEALTH", h.MaxHealth / TankCatalog.MaxHealth, Value(h.MaxHealth, eh.MaxHealth, "0"));

        turretHeader.text = "TURRET  " + t.DisplayName + "  " + StateTag(PlayerProfile.OwnsTurret(t.Id), t == et, t.Price);
        turretDamage.Set("DAMAGE", t.TotalDamagePerShot / TankCatalog.MaxDamage,
            t.DamageLabel + Delta(t.TotalDamagePerShot, et.TotalDamagePerShot, "+0;-0"));
        turretRate.Set("FIRE RATE", t.ShotsPerSecond / TankCatalog.MaxShotsPerSecond,
            Value(t.ShotsPerSecond, et.ShotsPerSecond, "0.0"));
        turretRange.Set("RANGE", t.Range / TankCatalog.MaxRange, Value(t.Range, et.Range, "0.0"));

        bulletIcon.sprite = t.bulletSprite;
        bulletIcon.enabled = t.bulletSprite != null;
        bulletName.text = t.BulletTypeName;
    }

    void RefreshAugmentInfo()
    {
        AugmentBase a = selectedAugment;
        if (a == null)
        {
            augmentName.text = "NO AUGMENTS";
            augmentDescription.text = "Run Tools > Tank Game > Setup Augments.";
            augmentDetails.text = "";
            return;
        }

        augmentName.text = a.DisplayName.ToUpper() + "  " + StateTag(AugmentProfile.Owns(a.Id), a == AugmentProfile.Equipped, a.Price);
        augmentDescription.text = a.Description;
        augmentDetails.text = a.Details;
    }

    static string StateTag(bool owned, bool equipped, int price)
    {
        if (!owned) return "[" + price + " COINS]";
        return equipped ? "[EQUIPPED]" : "[OWNED]";
    }

    // "5.4" followed by a green "+0.4" or red "-0.6" compared to what you have equipped
    static string Value(float value, float equipped, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture) + Delta(value, equipped, "+" + format + ";-" + format);
    }

    static string Delta(float value, float equipped, string format)
    {
        float diff = value - equipped;
        if (Mathf.Abs(diff) < 0.001f) return "";

        string color = diff > 0f ? "#6FBF3F" : "#D63A33";
        return " <color=" + color + ">" + diff.ToString(format, CultureInfo.InvariantCulture) + "</color>";
    }

    // ------------------------------------------------------------------
    //  Buying and equipping
    // ------------------------------------------------------------------
    void RefreshAction()
    {
        bool owned, equipped;
        int price;

        if (tab == Tab.Augments)
        {
            if (selectedAugment == null)
            {
                actionButton.interactable = false;
                actionLabel.text = "-";
                return;
            }
            owned = AugmentProfile.Owns(selectedAugment.Id);
            equipped = selectedAugment == AugmentProfile.Equipped;
            price = selectedAugment.Price;
        }
        else if (tab == Tab.Turrets)
        {
            owned = PlayerProfile.OwnsTurret(selectedTurret.Id);
            equipped = selectedTurret == PlayerProfile.EquippedTurret;
            price = selectedTurret.Price;
        }
        else
        {
            owned = PlayerProfile.OwnsHull(selectedHull.Id);
            equipped = selectedHull == PlayerProfile.EquippedHull;
            price = selectedHull.Price;
        }

        if (!owned)
        {
            bool canAfford = PlayerProfile.Coins >= price;
            actionButton.interactable = canAfford;
            actionLabel.text = canAfford ? "BUY  " + price : "NEED " + price;
        }
        else if (equipped && tab == Tab.Augments)
        {
            actionButton.interactable = true;   // an augment can be taken off again
            actionLabel.text = "UNEQUIP";
        }
        else if (equipped)
        {
            actionButton.interactable = false;
            actionLabel.text = "EQUIPPED";
        }
        else
        {
            actionButton.interactable = true;
            actionLabel.text = "EQUIP";
        }
    }

    void OnAction()
    {
        if (tab == Tab.Augments) ActOnAugment(selectedAugment);
        else if (tab == Tab.Turrets) ActOnTurret(selectedTurret);
        else ActOnHull(selectedHull);

        RefreshAll();
    }

    void ActOnHull(HullBase h)
    {
        if (!PlayerProfile.OwnsHull(h.Id))
        {
            if (PlayerProfile.TryBuyHull(h)) { PlayerProfile.EquipHull(h); messageText.text = "Bought and equipped " + h.DisplayName + "!"; }
            else messageText.text = "Not enough coins.";
        }
        else if (PlayerProfile.EquipHull(h)) messageText.text = "Equipped " + h.DisplayName + ".";
    }

    void ActOnTurret(TurretBase t)
    {
        if (!PlayerProfile.OwnsTurret(t.Id))
        {
            if (PlayerProfile.TryBuyTurret(t)) { PlayerProfile.EquipTurret(t); messageText.text = "Bought and equipped " + t.DisplayName + "!"; }
            else messageText.text = "Not enough coins.";
        }
        else if (PlayerProfile.EquipTurret(t)) messageText.text = "Equipped " + t.DisplayName + ".";
    }

    // Only one augment can be equipped at a time: equipping another one replaces it
    void ActOnAugment(AugmentBase a)
    {
        if (a == null) return;

        if (!AugmentProfile.Owns(a.Id))
        {
            if (AugmentProfile.TryBuy(a)) { AugmentProfile.Equip(a); messageText.text = "Bought and equipped " + a.DisplayName + "!"; }
            else messageText.text = "Not enough coins.";
        }
        else if (a == AugmentProfile.Equipped)
        {
            AugmentProfile.Equip(null);
            messageText.text = a.DisplayName + " removed.";
        }
        else
        {
            AugmentProfile.Equip(a);
            messageText.text = "Equipped " + a.DisplayName + ".";
        }
    }
}
