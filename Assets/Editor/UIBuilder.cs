using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
//
//   Tools > Tank Game > Build HUD         (run it inside a MAP scene)
//   Tools > Tank Game > Build Main Menu   (run it inside the MAIN MENU scene)
//
// Each tool builds a complete, textured UI and connects every Inspector slot for you.
// Running a tool again replaces what it built before, so you can change the numbers and re-run.
public static class UIBuilder
{
    // The pixel-art textures are small. 1 texture pixel = this many pixels on a 1920x1080 screen.
    const float PixelScale = 3f;

    static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    static readonly Color Orange = new Color32(0xE8, 0x87, 0x2B, 255);
    static readonly Color Red = new Color32(0xD6, 0x3A, 0x33, 255);
    static readonly Color DarkBar = new Color32(0x14, 0x16, 0x1A, 255);
    static readonly Color Steel = new Color32(0x4A, 0x52, 0x5C, 255);

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    class ButtonSprites
    {
        public Sprite normal, hover, pressed;
    }

    // ==================================================================
    //  HUD  (health bar + kills + death screen)
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build HUD")]
    static void BuildHud()
    {
        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", new Vector4(8, 8, 8, 8), missing);
        Sprite barFrame = LoadSprite("ui_bar_frame", new Vector4(3, 3, 3, 3), missing);
        Sprite barFill = LoadSprite("ui_bar_fill", Vector4.zero, missing);
        Sprite heart = LoadSprite("icon_heart", Vector4.zero, missing);
        Sprite skull = LoadSprite("icon_skull", Vector4.zero, missing);
        Sprite vignette = LoadSprite("ui_vignette", Vector4.zero, missing, FilterMode.Bilinear);
        if (ReportMissing(missing)) return;

        DestroyExisting("HUDCanvas");
        Canvas canvas = NewCanvas("HUDCanvas");
        Transform root = canvas.transform;

        // Red glow at the screen edges when health is low. Created first, so it draws behind everything.
        Image vig = NewImage("LowHealthVignette", root, vignette, new Color(1f, 0.1f, 0.1f, 0f), false);
        Stretch(vig.rectTransform);

        // ---- Top panel: [heart] [health bar] | [skull] [kills] ----
        Image panelImg = NewImage("TopPanel", root, panel, Color.white, true);
        RectTransform top = panelImg.rectTransform;
        Place(top, TopCenter, TopCenter, new Vector2(0f, -24f), new Vector2(760f, 96f));

        Image heartImg = NewImage("HeartIcon", top, heart, Color.white, false);
        PlaceLeft(heartImg.rectTransform, 28f, new Vector2(48f, 48f));

        RectTransform bar = NewRect("HealthBar", top);
        PlaceLeft(bar, 92f, new Vector2(420f, 42f));

        // The bar is built in layers (bottom to top): background, trail, fill, frame, text.
        // The 9 pixel inset keeps the fill inside the frame's border.
        Image barBg = NewImage("Background", bar, null, DarkBar, false);
        Stretch(barBg.rectTransform, 9, 9, 9, 9);

        Image trail = NewImage("Trail", bar, barFill, Orange, false);
        Stretch(trail.rectTransform, 9, 9, 9, 9);
        MakeFilled(trail);

        Image fill = NewImage("Fill", bar, barFill, new Color(0.45f, 0.8f, 0.25f), false);
        Stretch(fill.rectTransform, 9, 9, 9, 9);
        MakeFilled(fill);

        Image frame = NewImage("Frame", bar, barFrame, Color.white, true);
        Stretch(frame.rectTransform);

        TextMeshProUGUI value = NewText("Value", bar, "100 / 100", 32f, Cream, TextAlignmentOptions.Center);
        Stretch(value.rectTransform);

        Image divider = NewImage("Divider", top, null, Steel, false);
        PlaceLeft(divider.rectTransform, 536f, new Vector2(3f, 56f));

        Image skullImg = NewImage("SkullIcon", top, skull, Color.white, false);
        PlaceLeft(skullImg.rectTransform, 560f, new Vector2(48f, 48f));

        TextMeshProUGUI kills = NewText("KillsText", top, "0", 48f, Cream, TextAlignmentOptions.MidlineLeft);
        PlaceLeft(kills.rectTransform, 624f, new Vector2(112f, 60f));
        UIPunch punch = kills.gameObject.AddComponent<UIPunch>();

        // ---- Death screen (hidden until the player dies) ----
        RectTransform death = NewRect("DeathPanel", root);
        Stretch(death);
        death.gameObject.AddComponent<CanvasGroup>();

        Image dim = NewImage("Dim", death, null, new Color(0f, 0f, 0f, 0.7f), false);
        Stretch(dim.rectTransform);

        Image box = NewImage("Box", death, panel, Color.white, true);
        Place(box.rectTransform, Center, Center, Vector2.zero, new Vector2(760f, 380f));

        TextMeshProUGUI deathText = NewText("DeathText", box.rectTransform, "YOU DIED", 96f, Red, TextAlignmentOptions.Center);
        Stretch(deathText.rectTransform, 20, 20, 20, 20);
        death.gameObject.SetActive(false);

        // ---- Connect everything ----
        HealthBarUI hb = canvas.gameObject.AddComponent<HealthBarUI>();
        hb.fill = fill;
        hb.trail = trail;
        hb.valueText = value;
        hb.lowHealthVignette = vig;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject g = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(g, "Build HUD");
            gm = g.AddComponent<GameManager>();
        }
        Undo.RecordObject(gm, "Build HUD");
        gm.killsText = kills;
        gm.killsPunch = punch;
        gm.deathPanel = death.gameObject;
        gm.deathText = deathText;
        EditorUtility.SetDirty(gm);

        FinishBuild(canvas.gameObject);
        Debug.Log("HUD built. Delete your OLD 'KillsText' and 'DeathText' objects if they are still in the scene, then save (Ctrl+S).");
    }

    // ==================================================================
    //  MAIN MENU
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build Main Menu")]
    static void BuildMenu()
    {
        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", new Vector4(8, 8, 8, 8), missing);
        ButtonSprites btn = new ButtonSprites();
        btn.normal = LoadSprite("ui_button_normal", new Vector4(8, 8, 8, 8), missing);
        btn.hover = LoadSprite("ui_button_hover", new Vector4(8, 8, 8, 8), missing);
        btn.pressed = LoadSprite("ui_button_pressed", new Vector4(8, 8, 8, 8), missing);
        Sprite title = LoadSprite("ui_title", Vector4.zero, missing);
        Sprite background = LoadSprite("menu_background", Vector4.zero, missing, FilterMode.Bilinear);
        if (ReportMissing(missing)) return;

        AudioClip hoverSound = LoadClip("ui_hover");
        AudioClip clickSound = LoadClip("ui_click");
        AudioClip startSound = LoadClip("ui_start");

        DestroyExisting("MenuCanvas");
        EnsureEventSystem();

        MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu == null)
        {
            GameObject mm = new GameObject("MenuManager");
            Undo.RegisterCreatedObjectUndo(mm, "Build Main Menu");
            menu = mm.AddComponent<MainMenu>();
        }

        Canvas canvas = NewCanvas("MenuCanvas");
        Transform root = canvas.transform;

        Image bg = NewImage("Background", root, background, Color.white, false);
        Stretch(bg.rectTransform);

        Image titleImg = NewImage("Title", root, title, Color.white, false);
        Place(titleImg.rectTransform, TopCenter, TopCenter, new Vector2(0f, -60f), title.rect.size);

        Vector2 bigButton = new Vector2(560f, 112f);
        Vector2 mediumButton = new Vector2(380f, 112f);

        // ---- Main panel ----
        CanvasGroup mainGroup = NewGroup("MainPanel", root);
        Button mapBtn = NewButton("MapButton", mainGroup.transform, "SELECT MAP", btn, hoverSound, clickSound);
        Place(mapBtn.GetComponent<RectTransform>(), Center, Center, new Vector2(0f, 60f), bigButton);
        Button garageBtn = NewButton("GarageButton", mainGroup.transform, "GARAGE", btn, hoverSound, clickSound);
        Place(garageBtn.GetComponent<RectTransform>(), Center, Center, new Vector2(0f, -70f), bigButton);
        Button exitBtn = NewButton("ExitButton", mainGroup.transform, "EXIT", btn, hoverSound, clickSound);
        Place(exitBtn.GetComponent<RectTransform>(), Center, Center, new Vector2(0f, -200f), bigButton);

        TextMeshProUGUI version = NewText("Version", mainGroup.transform, "v0.1", 32f,
            new Color(Cream.r, Cream.g, Cream.b, 0.6f), TextAlignmentOptions.BottomLeft);
        Place(version.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 24f), new Vector2(300f, 48f));

        // ---- Map panel ----
        CanvasGroup mapGroup = NewGroup("MapPanel", root);
        Image mapBox = NewImage("Box", mapGroup.transform, panel, Color.white, true);
        Place(mapBox.rectTransform, Center, Center, new Vector2(0f, -40f), new Vector2(920f, 600f));
        AddHeader(mapBox.rectTransform, "SELECT MAP");

        Button map1 = NewButton("Map1Button", mapBox.transform, "MAP 1", btn, hoverSound, clickSound);
        Place(map1.GetComponent<RectTransform>(), Center, Center, new Vector2(-210f, 20f), mediumButton);
        Button map2 = NewButton("Map2Button", mapBox.transform, "MAP 2", btn, hoverSound, clickSound);
        Place(map2.GetComponent<RectTransform>(), Center, Center, new Vector2(210f, 20f), mediumButton);
        Button mapBack = NewButton("BackButton", mapBox.transform, "BACK", btn, hoverSound, clickSound);
        Place(mapBack.GetComponent<RectTransform>(), BottomCenter, BottomCenter, new Vector2(0f, 48f), mediumButton);

        // ---- Garage panel ----
        CanvasGroup garageGroup = NewGroup("GaragePanel", root);
        Image garageBox = NewImage("Box", garageGroup.transform, panel, Color.white, true);
        Place(garageBox.rectTransform, Center, Center, new Vector2(0f, -40f), new Vector2(920f, 600f));
        AddHeader(garageBox.rectTransform, "GARAGE");

        TextMeshProUGUI soon = NewText("ComingSoon", garageBox.transform, "COMING SOON", 48f, Cream, TextAlignmentOptions.Center);
        Place(soon.rectTransform, Center, Center, new Vector2(0f, 20f), new Vector2(800f, 100f));
        Button garageBack = NewButton("BackButton", garageBox.transform, "BACK", btn, hoverSound, clickSound);
        Place(garageBack.GetComponent<RectTransform>(), BottomCenter, BottomCenter, new Vector2(0f, 48f), mediumButton);

        // ---- Fade overlay (must be the last child, so it draws on top of everything) ----
        CanvasGroup fade = NewGroup("FadeOverlay", root);
        Image black = NewImage("Black", fade.transform, null, Color.black, false);
        Stretch(black.rectTransform);
        fade.alpha = 0f;
        fade.interactable = false;
        fade.blocksRaycasts = false;

        // ---- Connect everything ----
        Undo.RecordObject(menu, "Build Main Menu");
        menu.mainPanel = mainGroup;
        menu.mapPanel = mapGroup;
        menu.garagePanel = garageGroup;
        menu.fadeOverlay = fade;
        menu.startSound = startSound;
        EditorUtility.SetDirty(menu);

        // Same as filling the "On Click ()" list by hand, but done for you
        UnityEventTools.AddPersistentListener(mapBtn.onClick, menu.ShowMaps);
        UnityEventTools.AddPersistentListener(garageBtn.onClick, menu.ShowGarage);
        UnityEventTools.AddPersistentListener(exitBtn.onClick, menu.QuitGame);
        UnityEventTools.AddStringPersistentListener(map1.onClick, menu.LoadMap, "Map_01");
        UnityEventTools.AddStringPersistentListener(map2.onClick, menu.LoadMap, "Map_02");
        UnityEventTools.AddPersistentListener(mapBack.onClick, menu.ShowMain);
        UnityEventTools.AddPersistentListener(garageBack.onClick, menu.ShowMain);

        FinishBuild(canvas.gameObject);

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c != canvas && c.isRootCanvas)
                Debug.LogWarning("Another Canvas called '" + c.name + "' is in this scene. If it is your OLD menu, delete it.");
        }

        if (hoverSound == null || clickSound == null)
            Debug.Log("No ui_hover / ui_click sounds found in the project, so the buttons will be silent. Import the .wav files and run this tool again.");
    }

    // ==================================================================
    //  BUILDING BLOCKS
    // ==================================================================
    static void AddHeader(RectTransform parent, string text)
    {
        TextMeshProUGUI header = NewText("Header", parent, text, 64f, Orange, TextAlignmentOptions.Center);
        Place(header.rectTransform, TopCenter, TopCenter, new Vector2(0f, -48f), new Vector2(800f, 90f));
    }

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

    static CanvasGroup NewGroup(string name, Transform parent)
    {
        RectTransform rt = NewRect(name, parent);
        Stretch(rt);
        return rt.gameObject.AddComponent<CanvasGroup>();
    }

    // sliced = true: a 9-slice image whose corners keep their pixel size while the middle stretches
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
            img.pixelsPerUnitMultiplier = 1f / PixelScale;   // makes the pixel-art borders 3x bigger
        }
        return img;
    }

    static void MakeFilled(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;   // your project's default font
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
        state.selectedSprite = sprites.normal;     // so a clicked button doesn't stay highlighted
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

    // ---- Layout helpers ----
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

    // Anchored to the left edge of the parent, vertically centered; x is the distance from the left edge
    static void PlaceLeft(RectTransform rt, float x, Vector2 size)
    {
        Vector2 leftMiddle = new Vector2(0f, 0.5f);
        Place(rt, leftMiddle, leftMiddle, new Vector2(x, 0f), size);
    }

    // ---- Scene helpers ----
    static void DestroyExisting(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Build UI");
    }

    static void FinishBuild(GameObject root)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
    }

    // ==================================================================
    //  ASSET LOADING (finds files by name and sets their import settings)
    // ==================================================================
    static bool ReportMissing(List<string> missing)
    {
        if (missing.Count == 0) return false;

        EditorUtility.DisplayDialog("Missing textures",
            "These images were not found in the project:\n\n  " + string.Join("\n  ", missing) +
            "\n\nImport them into the Assets folder (the file names must match exactly) and try again.", "OK");
        return true;
    }

    static Sprite LoadSprite(string name, Vector4 border, List<string> missing, FilterMode filter = FilterMode.Point)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f)) { importer.spritePixelsPerUnit = 100f; changed = true; }
            if (importer.filterMode != filter) { importer.filterMode = filter; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
            if (changed) importer.SaveAndReimport();

            // Sliced and tiled images need the sprite's mesh type to be "Full Rect"
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
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
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
