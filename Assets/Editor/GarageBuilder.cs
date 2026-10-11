using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
//
//   Tools > Tank Game > Setup Garage Assets     (once: imports the tank images and creates the hull / turret assets and the coin)
//   Tools > Tank Game > Build Garage UI         (run it inside the MainMenu scene, after Build Main Menu)
//   Tools > Tank Game > Build Coin HUD          (run it inside a MAP scene)
//   Tools > Tank Game > Add Loadout To Player   (run it inside a MAP scene)
//   Tools > Tank Game > Cheats > ...            (coins for testing, and reset progress)
public static class GarageBuilder
{
    const float PixelScale = 3f;           // 1 texture pixel of the pixel-art UI = 3 pixels on a 1920x1080 screen
    const float TankPixelsPerUnit = 40f;   // the new tank images: 40 pixels = 1 world unit

    static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    static readonly Color Orange = new Color32(0xE8, 0x87, 0x2B, 255);
    static readonly Color Blue = new Color32(0x48, 0x8C, 0xEB, 255);
    static readonly Color DarkBar = new Color32(0x14, 0x16, 0x1A, 255);
    static readonly Color OutlineColor = new Color32(0x0B, 0x0C, 0x0E, 255);

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopRight = new Vector2(1f, 1f);
    static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    class ButtonSprites
    {
        public Sprite normal, hover, pressed;
    }

    // ==================================================================
    //  1. ASSETS: tank parts and the coin
    // ==================================================================
    [MenuItem("Tools/Tank Game/Setup Garage Assets")]
    static void SetupAssets()
    {
        Directory.CreateDirectory("Assets/Resources/TankParts/Hulls");
        Directory.CreateDirectory("Assets/Resources/TankParts/Turrets");
        AssetDatabase.Refresh();

        List<string> problems = new List<string>();
        int hullCount = 0;
        int turretCount = 0;

        // One asset per hull class. The class holds the numbers; the asset only holds the picture.
        foreach (System.Type type in TypeCache.GetTypesDerivedFrom<HullBase>())
        {
            if (type.IsAbstract) continue;

            HullBase instance = (HullBase)ScriptableObject.CreateInstance(type);
            string path = "Assets/Resources/TankParts/Hulls/Hull_" + instance.Id + ".asset";

            HullBase asset = AssetDatabase.LoadAssetAtPath<HullBase>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(instance, path);
                asset = instance;
            }
            else
            {
                Object.DestroyImmediate(instance);
            }

            asset.sprite = LoadSprite(asset.Id + "_Black_Bot", TankPixelsPerUnit, Center, Vector4.zero, problems);
            EditorUtility.SetDirty(asset);
            hullCount++;
        }

        foreach (System.Type type in TypeCache.GetTypesDerivedFrom<TurretBase>())
        {
            if (type.IsAbstract) continue;

            TurretBase instance = (TurretBase)ScriptableObject.CreateInstance(type);
            string path = "Assets/Resources/TankParts/Turrets/Turret_" + instance.Id + ".asset";

            TurretBase asset = AssetDatabase.LoadAssetAtPath<TurretBase>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(instance, path);
                asset = instance;
            }
            else
            {
                Object.DestroyImmediate(instance);
            }

            // The turret image turns around its pivot, which each turret class knows
            asset.sprite = LoadSprite(asset.Id + "_Black_Top", TankPixelsPerUnit, asset.SpritePivot, Vector4.zero, problems);
            asset.bulletSprite = LoadSprite("bullet_" + asset.Id, TankPixelsPerUnit, Center, Vector4.zero, problems);
            EditorUtility.SetDirty(asset);
            turretCount++;
        }

        // The coin that enemies drop
        Sprite coin = LoadSprite("coin", 32f, Center, Vector4.zero, problems);
        AudioClip coinSound = LoadClip("coin_pickup");
        if (coin != null)
        {
            GameObject root = new GameObject("CoinPickup");
            CircleCollider2D col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = coin;
            sr.sortingOrder = 1;

            CoinPickup pickup = root.AddComponent<CoinPickup>();
            pickup.pickupSound = coinSound;

            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/CoinPickup.prefab");
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ReportSetup(hullCount, turretCount, coin != null, problems);
    }

    // Prints what was created and explains exactly which images are missing and why
    static void ReportSetup(int hullCount, int turretCount, bool coinMade, List<string> problems)
    {
        Debug.Log("Garage assets ready: " + hullCount + " hull classes, " + turretCount + " turret classes, coin prefab " +
                  (coinMade ? "created." : "NOT created."));
        if (problems.Count == 0) return;

        List<string> tankImages = new List<string>();
        List<string> packImages = new List<string>();
        foreach (string name in problems)
        {
            if (name == "coin" || name.StartsWith("bullet_")) packImages.Add(name);
            else tankImages.Add(name);

            Debug.LogWarning("Image not found: '" + name + "'. " + SimilarFiles(name));
        }

        string message = "";
        if (tankImages.Count > 0)
        {
            message += "Tank images not found in the Assets folder:\n  " + string.Join("\n  ", tankImages) +
                       "\n\nThey must be inside Assets and named exactly like this. " +
                       "A hull or turret without its image is hidden from the garage.\n\n";
        }
        if (packImages.Count > 0)
        {
            message += "Images from the Garage pack not found:\n  " + string.Join("\n  ", packImages) +
                       "\n\nThese are NOT your tank images. They are coin.png and the bullet_*.png files in the pack's " +
                       "Textures folder, and they must be imported too.\n";
        }
        EditorUtility.DisplayDialog("Some images are missing", message, "OK");
    }

    // Lists files whose name looks like the missing one (for example a typo or an extra " (1)")
    static string SimilarFiles(string name)
    {
        string token = name.Split('_')[0];
        List<string> found = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets(token))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path)) continue;
            if (Path.GetFileName(path).IndexOf(token, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

            found.Add(path);
            if (found.Count >= 3) break;
        }

        return found.Count > 0
            ? "Similar files in the project: " + string.Join(", ", found)
            : "No file with '" + token + "' in its name exists anywhere in Assets.";
    }

    // ==================================================================
    //  2. THE GARAGE SCREEN
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build Garage UI")]
    static void BuildGarageUI()
    {
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
        GameObject panelObject = GameObject.Find("GaragePanel");
        if (menu == null || panelObject == null)
        {
            EditorUtility.DisplayDialog("Garage",
                "Open the MainMenu scene and run Tools > Tank Game > Build Main Menu first. " +
                "This tool fills the GaragePanel that it creates.", "OK");
            return;
        }

        List<string> missing = new List<string>();
        Vector4 slice = new Vector4(8, 8, 8, 8);
        Sprite panel = LoadSprite("ui_panel", 100f, Center, slice, missing);
        ButtonSprites btn = new ButtonSprites();
        btn.normal = LoadSprite("ui_button_normal", 100f, Center, slice, missing);
        btn.hover = LoadSprite("ui_button_hover", 100f, Center, slice, missing);
        btn.pressed = LoadSprite("ui_button_pressed", 100f, Center, slice, missing);
        Sprite barFill = LoadSprite("ui_bar_fill", 100f, Center, Vector4.zero, missing);
        Sprite coin = LoadSprite("coin", 32f, Center, Vector4.zero, missing);
        if (ReportMissing(missing)) return;

        AudioClip hoverSound = LoadClip("ui_hover");
        AudioClip clickSound = LoadClip("ui_click");

        // Empty the old placeholder, but keep the panel itself (the menu already points at it)
        Transform root = panelObject.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);

        GarageUI ui = panelObject.GetComponent<GarageUI>();
        if (ui == null) ui = Undo.AddComponent<GarageUI>(panelObject);

        // The big frame. It is tall enough to cover the game logo behind it.
        Image boxImg = NewImage("Box", root, panel, Color.white, true);
        RectTransform box = boxImg.rectTransform;
        Place(box, Center, Center, Vector2.zero, new Vector2(1760f, 1000f));

        // ---- Header ----
        TextMeshProUGUI title = NewText("Title", box, "GARAGE", 64f, Orange, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, TopLeft, TopLeft, new Vector2(48f, -30f), new Vector2(600f, 80f));

        Image coinIcon = NewImage("CoinIcon", box, coin, Color.white, false);
        Place(coinIcon.rectTransform, TopRight, TopRight, new Vector2(-250f, -46f), new Vector2(48f, 48f));
        TextMeshProUGUI coinsText = NewText("Coins", box, "0", 48f, Cream, TextAlignmentOptions.MidlineRight);
        Place(coinsText.rectTransform, TopRight, TopRight, new Vector2(-48f, -40f), new Vector2(190f, 60f));

        // ---- Preview (left, top) ----
        Image previewPanel = NewImage("PreviewPanel", box, panel, Color.white, true);
        Place(previewPanel.rectTransform, TopLeft, TopLeft, new Vector2(48f, -110f), new Vector2(560f, 480f));
        Image floor = NewImage("Floor", previewPanel.rectTransform, null, new Color(0.08f, 0.09f, 0.1f, 0.9f), false);
        Stretch(floor.rectTransform, 24, 24, 24, 24);

        RectTransform previewArea = NewRect("PreviewArea", previewPanel.rectTransform);
        Stretch(previewArea, 8, 8, 8, 8);

        Image hullPreview = NewImage("Hull", previewArea, null, Color.white, false);
        Place(hullPreview.rectTransform, Center, Center, Vector2.zero, new Vector2(100f, 100f));
        Image turretPreview = NewImage("Turret", previewArea, null, Color.white, false);
        Place(turretPreview.rectTransform, Center, Center, Vector2.zero, new Vector2(100f, 100f));

        TextMeshProUGUI previewTitle = NewText("PreviewTitle", previewPanel.rectTransform, "", 32f, Cream, TextAlignmentOptions.Center);
        Place(previewTitle.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 28f), new Vector2(500f, 36f));

        // ---- Stats (left, bottom) ----
        Image statsPanel = NewImage("StatsPanel", box, panel, Color.white, true);
        Place(statsPanel.rectTransform, TopLeft, TopLeft, new Vector2(48f, -604f), new Vector2(560f, 348f));
        Transform stats = statsPanel.rectTransform;

        TextMeshProUGUI hullHeader = NewText("HullHeader", stats, "HULL", 32f, Orange, TextAlignmentOptions.MidlineLeft);
        Place(hullHeader.rectTransform, TopLeft, TopLeft, new Vector2(24f, -12f), new Vector2(512f, 34f));
        GarageUI.StatRow hullSpeed = NewStatRow(stats, -48f, barFill, Blue);
        GarageUI.StatRow hullTurning = NewStatRow(stats, -82f, barFill, Blue);
        GarageUI.StatRow hullHealth = NewStatRow(stats, -116f, barFill, Blue);

        TextMeshProUGUI turretHeader = NewText("TurretHeader", stats, "TURRET", 32f, Orange, TextAlignmentOptions.MidlineLeft);
        Place(turretHeader.rectTransform, TopLeft, TopLeft, new Vector2(24f, -156f), new Vector2(512f, 34f));
        GarageUI.StatRow turretDamage = NewStatRow(stats, -192f, barFill, Orange);
        GarageUI.StatRow turretRate = NewStatRow(stats, -226f, barFill, Orange);
        GarageUI.StatRow turretRange = NewStatRow(stats, -260f, barFill, Orange);

        TextMeshProUGUI bulletLabel = NewText("BulletLabel", stats, "BULLET", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(bulletLabel.rectTransform, TopLeft, TopLeft, new Vector2(24f, -294f), new Vector2(150f, 34f));
        Image bulletIcon = NewImage("BulletIcon", stats, null, Color.white, false);
        bulletIcon.preserveAspect = true;
        Place(bulletIcon.rectTransform, TopLeft, TopLeft, new Vector2(178f, -293f), new Vector2(32f, 32f));
        TextMeshProUGUI bulletName = NewText("BulletName", stats, "", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(bulletName.rectTransform, TopLeft, TopLeft, new Vector2(222f, -294f), new Vector2(320f, 34f));

        // ---- Tabs and cards (right) ----
        Button hullTab = NewButton("HullsTab", box, "HULLS", btn, hoverSound, clickSound);
        Place(hullTab.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(660f, -110f), new Vector2(300f, 84f));
        Button turretTab = NewButton("TurretsTab", box, "TURRETS", btn, hoverSound, clickSound);
        Place(turretTab.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(976f, -110f), new Vector2(300f, 84f));

        RectTransform grid = NewRect("CardGrid", box);
        Place(grid, TopLeft, TopLeft, new Vector2(660f, -214f), new Vector2(1052f, 542f));
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(196f, 262f);
        layout.spacing = new Vector2(18f, 18f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;

        GarageCard template = BuildCardTemplate(box, btn, coin, hoverSound, clickSound);

        // ---- Bottom row ----
        TextMeshProUGUI message = NewText("Message", box, "", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(message.rectTransform, TopLeft, TopLeft, new Vector2(660f, -764f), new Vector2(1052f, 40f));

        Button action = NewButton("ActionButton", box, "EQUIP", btn, hoverSound, clickSound);
        Place(action.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(660f, -812f), new Vector2(520f, 100f));
        Button back = NewButton("BackButton", box, "BACK", btn, hoverSound, clickSound);
        Place(back.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(1212f, -812f), new Vector2(500f, 100f));

        // ---- Connect everything ----
        Undo.RecordObject(ui, "Build Garage UI");
        ui.coinsText = coinsText;
        ui.hullTabButton = hullTab;
        ui.turretTabButton = turretTab;
        ui.hullTabImage = hullTab.GetComponent<Image>();
        ui.turretTabImage = turretTab.GetComponent<Image>();
        ui.cardGrid = grid;
        ui.cardTemplate = template;
        ui.previewArea = previewArea;
        ui.hullPreview = hullPreview;
        ui.turretPreview = turretPreview;
        ui.previewTitle = previewTitle;
        ui.hullHeader = hullHeader;
        ui.hullSpeed = hullSpeed;
        ui.hullTurning = hullTurning;
        ui.hullHealth = hullHealth;
        ui.turretHeader = turretHeader;
        ui.turretDamage = turretDamage;
        ui.turretRate = turretRate;
        ui.turretRange = turretRange;
        ui.bulletIcon = bulletIcon;
        ui.bulletName = bulletName;
        ui.actionButton = action;
        ui.actionLabel = action.GetComponentInChildren<TextMeshProUGUI>();
        ui.messageText = message;
        EditorUtility.SetDirty(ui);

        UnityEventTools.AddPersistentListener(back.onClick, menu.ShowMain);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panelObject;
        Debug.Log("Garage UI built. Press Play, open the Garage, and use Tools > Tank Game > Cheats > Add 1000 Coins to try buying.");
    }

    static GarageCard BuildCardTemplate(Transform parent, ButtonSprites sprites, Sprite coin, AudioClip hoverSound, AudioClip clickSound)
    {
        Image bg = NewImage("CardTemplate", parent, sprites.normal, Color.white, true);
        bg.raycastTarget = true;
        RectTransform rt = bg.rectTransform;
        rt.sizeDelta = new Vector2(196f, 262f);

        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = new SpriteState();
        state.highlightedSprite = sprites.hover;
        state.pressedSprite = sprites.pressed;
        state.disabledSprite = sprites.pressed;
        // selectedSprite stays empty on purpose: GarageCard chooses the selected look itself
        button.spriteState = state;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        UIButtonEffects fx = bg.gameObject.AddComponent<UIButtonEffects>();
        fx.hoverSound = hoverSound;
        fx.clickSound = clickSound;

        Image icon = NewImage("Icon", rt, null, Color.white, false);
        icon.preserveAspect = true;
        Place(icon.rectTransform, TopCenter, TopCenter, new Vector2(0f, -18f), new Vector2(160f, 170f));

        TextMeshProUGUI nameText = NewText("Name", rt, "NAME", 32f, Cream, TextAlignmentOptions.Center);
        Place(nameText.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 66f), new Vector2(184f, 40f));

        RectTransform priceRow = NewRect("PriceRow", rt);
        Place(priceRow, BottomCenter, BottomCenter, new Vector2(0f, 18f), new Vector2(184f, 40f));
        Image priceIcon = NewImage("Coin", priceRow, coin, Color.white, false);
        Place(priceIcon.rectTransform, Center, Center, new Vector2(-44f, 0f), new Vector2(32f, 32f));
        TextMeshProUGUI priceText = NewText("Price", priceRow, "0", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(priceText.rectTransform, Center, new Vector2(0f, 0.5f), new Vector2(-20f, 0f), new Vector2(110f, 40f));

        TextMeshProUGUI status = NewText("Status", rt, "OWNED", 32f, Cream, TextAlignmentOptions.Center);
        Place(status.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 18f), new Vector2(184f, 40f));

        GarageCard card = bg.gameObject.AddComponent<GarageCard>();
        card.button = button;
        card.background = bg;
        card.icon = icon;
        card.nameText = nameText;
        card.statusText = status;
        card.priceRow = priceRow.gameObject;
        card.priceText = priceText;
        card.normalSprite = sprites.normal;
        card.selectedSprite = sprites.hover;   // the orange-outlined look marks the selected card

        bg.gameObject.SetActive(false);   // it is only a template; the garage makes copies of it
        return card;
    }

    static GarageUI.StatRow NewStatRow(Transform parent, float y, Sprite fillSprite, Color color)
    {
        GarageUI.StatRow row = new GarageUI.StatRow();

        row.label = NewText("Label", parent, "STAT", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(row.label.rectTransform, TopLeft, TopLeft, new Vector2(24f, y), new Vector2(150f, 34f));

        RectTransform bar = NewRect("Bar", parent);
        Place(bar, TopLeft, TopLeft, new Vector2(178f, y - 5f), new Vector2(190f, 24f));
        Image outline = NewImage("Outline", bar, null, OutlineColor, false);
        Stretch(outline.rectTransform);
        Image background = NewImage("Background", bar, null, DarkBar, false);
        Stretch(background.rectTransform, 3, 3, 3, 3);

        row.fill = NewImage("Fill", bar, fillSprite, color, false);
        Stretch(row.fill.rectTransform, 3, 3, 3, 3);
        row.fill.type = Image.Type.Filled;
        row.fill.fillMethod = Image.FillMethod.Horizontal;
        row.fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        row.fill.fillAmount = 0.5f;

        row.value = NewText("Value", parent, "0", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(row.value.rectTransform, TopLeft, TopLeft, new Vector2(378f, y), new Vector2(172f, 34f));
        return row;
    }

    // ==================================================================
    //  3. COIN COUNTER IN THE GAME
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build Coin HUD")]
    static void BuildCoinHud()
    {
        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", 100f, Center, new Vector4(8, 8, 8, 8), missing);
        Sprite coin = LoadSprite("coin", 32f, Center, Vector4.zero, missing);
        if (ReportMissing(missing)) return;

        DestroyExisting("CoinHUDCanvas");
        Canvas canvas = NewCanvas("CoinHUDCanvas");

        Image panelImg = NewImage("CoinPanel", canvas.transform, panel, Color.white, true);
        Place(panelImg.rectTransform, TopRight, TopRight, new Vector2(-24f, -24f), new Vector2(300f, 84f));

        Image icon = NewImage("CoinIcon", panelImg.rectTransform, coin, Color.white, false);
        PlaceLeft(icon.rectTransform, 24f, new Vector2(48f, 48f));

        TextMeshProUGUI text = NewText("CoinText", panelImg.rectTransform, "0", 48f, Cream, TextAlignmentOptions.MidlineRight);
        Place(text.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(190f, 60f));
        UIPunch punch = text.gameObject.AddComponent<UIPunch>();

        CoinCounterUI counter = canvas.gameObject.AddComponent<CoinCounterUI>();
        counter.text = text;
        counter.punch = punch;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Coin HUD built.");
    }

    // ==================================================================
    //  4. DRESS THE PLAYER IN THE GARAGE PARTS
    // ==================================================================
    [MenuItem("Tools/Tank Game/Add Loadout To Player")]
    static void AddLoadout()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Loadout", "No object tagged 'Player' was found in this scene.", "OK");
            return;
        }

        if (player.GetComponent<TankLoadout>() == null) Undo.AddComponent<TankLoadout>(player);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("TankLoadout added to '" + player.name + "'. If the Player is a prefab, open the Overrides drop-down in the Inspector and click Apply All.");
    }

    // ==================================================================
    //  5. CHEATS FOR TESTING
    // ==================================================================
    [MenuItem("Tools/Tank Game/Cheats/Add 1000 Coins")]
    static void CheatCoins()
    {
        PlayerProfile.AddCoins(1000);
        Debug.Log("Coins: " + PlayerProfile.Coins);
    }

    [MenuItem("Tools/Tank Game/Cheats/Reset Progress")]
    static void CheatReset()
    {
        PlayerProfile.ResetAll();
        Debug.Log("Progress reset: 0 coins, starter parts only.");
    }

    // ==================================================================
    //  UI HELPERS
    // ==================================================================
    static Canvas NewCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Build UI");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        if (sliced && sprite != null)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / PixelScale;
        }
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button NewButton(string name, Transform parent, string label, ButtonSprites sprites, AudioClip hoverSound, AudioClip clickSound)
    {
        Image img = NewImage(name, parent, sprites.normal, Color.white, true);
        img.raycastTarget = true;

        Button button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState state = new SpriteState();
        state.highlightedSprite = sprites.hover;
        state.pressedSprite = sprites.pressed;
        state.selectedSprite = sprites.normal;
        state.disabledSprite = sprites.pressed;
        button.spriteState = state;

        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        TextMeshProUGUI text = NewText("Label", img.rectTransform, label, 48f, Cream, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);

        UIButtonEffects fx = img.gameObject.AddComponent<UIButtonEffects>();
        fx.label = text.rectTransform;
        fx.hoverSound = hoverSound;
        fx.clickSound = clickSound;
        return button;
    }

    static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = Center;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void PlaceLeft(RectTransform rt, float x, Vector2 size)
    {
        Vector2 leftMiddle = new Vector2(0f, 0.5f);
        Place(rt, leftMiddle, leftMiddle, new Vector2(x, 0f), size);
    }

    static void DestroyExisting(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
    }

    // ==================================================================
    //  ASSET LOADING
    // ==================================================================
    static bool ReportMissing(List<string> missing)
    {
        if (missing.Count == 0) return false;

        EditorUtility.DisplayDialog("Missing images",
            "These images were not found in the project:\n\n  " + string.Join("\n  ", missing) +
            "\n\nImport them into the Assets folder (the file names must match exactly) and try again.", "OK");
        return true;
    }

    // Finds an image by file name, makes sure it is imported as a crisp sprite, and applies the pivot and slicing border
    static Sprite LoadSprite(string name, float pixelsPerUnit, Vector2 pivot, Vector4 border, List<string> missing)
    {
        foreach (string guid in AssetDatabase.FindAssets(name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), name, System.StringComparison.OrdinalIgnoreCase)) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)) { importer.spritePixelsPerUnit = pixelsPerUnit; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
            if (changed) importer.SaveAndReimport();

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool settingsChanged = false;
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settingsChanged = true;
            }
            if (settings.spriteAlignment != (int)SpriteAlignment.Custom || (settings.spritePivot - pivot).sqrMagnitude > 0.00001f)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                settingsChanged = true;
            }
            if (settingsChanged)
            {
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        missing.Add(name);
        return null;
    }

    static AudioClip LoadClip(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
