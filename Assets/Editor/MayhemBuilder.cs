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
//   Tools > Tank Game > Build Map Panel (Modes)         (run it inside the MainMenu scene)
//   Tools > Tank Game > Setup Mayhem In This Scene      (run it inside a map scene)
//   Tools > Tank Game > Setup Mayhem In All Map Scenes  (does the same for every map in the Build Profiles list)
public static class MayhemBuilder
{
    const float PixelScale = 3f;

    static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    static readonly Color Orange = new Color32(0xE8, 0x87, 0x2B, 255);
    static readonly Color DarkBar = new Color32(0x14, 0x16, 0x1A, 255);
    static readonly Color OutlineColor = new Color32(0x0B, 0x0C, 0x0E, 255);

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    class ButtonSprites
    {
        public Sprite normal, hover, pressed;
    }

    // ==================================================================
    //  1. THE MAP SCREEN WITH THE MODE SWITCH (main menu)
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build Map Panel (Modes)")]
    static void BuildMapPanel()
    {
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
        GameObject panelObject = GameObject.Find("MapPanel");
        if (menu == null || panelObject == null)
        {
            EditorUtility.DisplayDialog("Map panel",
                "Open the MainMenu scene and run Tools > Tank Game > Build Main Menu first. " +
                "This tool fills the MapPanel that it creates.", "OK");
            return;
        }

        List<string> maps = FindMapScenes();
        if (maps.Count == 0)
        {
            EditorUtility.DisplayDialog("Map panel",
                "No map scenes were found. Add your maps to File > Build Profiles > Scene List first " +
                "(every scene except MainMenu counts as a map).", "OK");
            return;
        }

        List<string> missing = new List<string>();
        Vector4 slice = new Vector4(8, 8, 8, 8);
        Sprite panel = LoadSprite("ui_panel", slice, missing);
        ButtonSprites btn = new ButtonSprites();
        btn.normal = LoadSprite("ui_button_normal", slice, missing);
        btn.hover = LoadSprite("ui_button_hover", slice, missing);
        btn.pressed = LoadSprite("ui_button_pressed", slice, missing);
        if (ReportMissing(missing)) return;

        AudioClip hoverSound = LoadClip("ui_hover");
        AudioClip clickSound = LoadClip("ui_click");

        // Empty the old panel, but keep the panel itself (the menu already points at it)
        Transform root = panelObject.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);

        ModeSelectorUI selector = panelObject.GetComponent<ModeSelectorUI>();
        if (selector == null) selector = Undo.AddComponent<ModeSelectorUI>(panelObject);

        Image boxImg = NewImage("Box", root, panel, Color.white, true);
        RectTransform box = boxImg.rectTransform;
        Place(box, Center, Center, new Vector2(0f, -90f), new Vector2(1000f, 700f));

        TextMeshProUGUI header = NewText("Header", box, "SELECT MAP", 64f, Orange, TextAlignmentOptions.Center);
        Place(header.rectTransform, TopCenter, TopCenter, new Vector2(0f, -36f), new Vector2(800f, 80f));

        // The mode switch
        Button classic = NewButton("ClassicButton", box, GameSettings.DisplayName(GameMode.Classic), btn, hoverSound, clickSound);
        Place(classic.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(100f, -130f), new Vector2(380f, 84f));
        Button mayhem = NewButton("MayhemButton", box, GameSettings.DisplayName(GameMode.Mayhem), btn, hoverSound, clickSound);
        Place(mayhem.GetComponent<RectTransform>(), TopLeft, TopLeft, new Vector2(520f, -130f), new Vector2(380f, 84f));

        TextMeshProUGUI description = NewText("ModeDescription", box, "", 32f, Cream, TextAlignmentOptions.Center);
        Place(description.rectTransform, TopLeft, TopLeft, new Vector2(60f, -226f), new Vector2(880f, 76f));

        // One button for every map scene in the build list (up to 4)
        int shown = Mathf.Min(maps.Count, 4);
        for (int i = 0; i < shown; i++)
        {
            int column = i % 2;
            int row = i / 2;
            string sceneName = maps[i];

            Button mapButton = NewButton("Map" + (i + 1) + "Button", box, sceneName.Replace('_', ' ').ToUpper(), btn, hoverSound, clickSound);
            Place(mapButton.GetComponent<RectTransform>(), TopLeft, TopLeft,
                  new Vector2(100f + 420f * column, -330f - 124f * row), new Vector2(380f, 112f));
            UnityEventTools.AddStringPersistentListener(mapButton.onClick, menu.LoadMap, sceneName);
        }
        if (maps.Count > 4)
            Debug.LogWarning("The map panel has room for 4 maps. These were left out: " + string.Join(", ", maps.GetRange(4, maps.Count - 4)));

        Button back = NewButton("BackButton", box, "BACK", btn, hoverSound, clickSound);
        Place(back.GetComponent<RectTransform>(), BottomCenter, BottomCenter, new Vector2(0f, 24f), new Vector2(380f, 96f));
        UnityEventTools.AddPersistentListener(back.onClick, menu.ShowMain);

        Undo.RecordObject(selector, "Build Map Panel");
        selector.classicButton = classic;
        selector.mayhemButton = mayhem;
        selector.classicImage = classic.GetComponent<Image>();
        selector.mayhemImage = mayhem.GetComponent<Image>();
        selector.description = description;
        EditorUtility.SetDirty(selector);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panelObject;
        Debug.Log("Map panel built with " + shown + " map button(s): " + string.Join(", ", maps));
    }

    // Every scene in the build list except the menu counts as a map
    static List<string> FindMapScenes()
    {
        List<string> maps = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            string name = Path.GetFileNameWithoutExtension(scene.path);
            if (string.Equals(name, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) continue;
            maps.Add(name);
        }
        return maps;
    }

    // ==================================================================
    //  2. MAYHEM IN THE MAP SCENES
    // ==================================================================
    [MenuItem("Tools/Tank Game/Setup Mayhem In This Scene")]
    static void SetupThisScene()
    {
        if (SetupOpenScene())
            Debug.Log("Mayhem is set up in this scene. Save it with Ctrl+S.");
    }

    [MenuItem("Tools/Tank Game/Setup Mayhem In All Map Scenes")]
    static void SetupAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string original = EditorSceneManager.GetActiveScene().path;
        int done = 0;

        foreach (string mapName in FindMapScenes())
        {
            string path = null;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (Path.GetFileNameWithoutExtension(scene.path) == mapName) path = scene.path;
            }
            if (path == null) continue;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (SetupOpenScene())
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                done++;
            }
        }

        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log("Mayhem set up and saved in " + done + " map scene(s).");
    }

    // Builds everything Mayhem needs in the currently open scene. Safe to run again.
    static bool SetupOpenScene()
    {
        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", new Vector4(8, 8, 8, 8), missing);
        Sprite barFill = LoadSprite("ui_bar_fill", Vector4.zero, missing);
        if (ReportMissing(missing)) return false;

        // ---- The wave manager ----
        MayhemManager manager = Object.FindFirstObjectByType<MayhemManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("MayhemManager");
            Undo.RegisterCreatedObjectUndo(go, "Setup Mayhem");
            manager = go.AddComponent<MayhemManager>();
        }

        EnemySpawner classicSpawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (classicSpawner != null && manager.enemyPrefab == null)
        {
            manager.enemyPrefab = classicSpawner.enemyPrefab;
            EditorUtility.SetDirty(manager);
        }
        if (classicSpawner == null)
            Debug.LogWarning("No EnemySpawner in '" + EditorSceneManager.GetActiveScene().name + "'. Assign an Enemy Prefab on the MayhemManager by hand.");

        // ---- The wave display ----
        DestroyExisting("MayhemHUDCanvas");
        Canvas canvas = NewCanvas("MayhemHUDCanvas");
        Transform hudRoot = canvas.transform;

        Image wavePanel = NewImage("WavePanel", hudRoot, panel, Color.white, true);
        Place(wavePanel.rectTransform, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(400f, 150f));

        TextMeshProUGUI waveText = NewText("WaveText", wavePanel.rectTransform, "WAVE 1", 48f, Orange, TextAlignmentOptions.MidlineLeft);
        Place(waveText.rectTransform, TopLeft, TopLeft, new Vector2(24f, -14f), new Vector2(352f, 60f));
        TextMeshProUGUI enemiesText = NewText("EnemiesText", wavePanel.rectTransform, "ENEMIES", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(enemiesText.rectTransform, TopLeft, TopLeft, new Vector2(24f, -76f), new Vector2(352f, 36f));

        RectTransform bar = NewRect("Progress", wavePanel.rectTransform);
        Place(bar, TopLeft, TopLeft, new Vector2(24f, -116f), new Vector2(352f, 18f));
        Image outline = NewImage("Outline", bar, null, OutlineColor, false);
        Stretch(outline.rectTransform);
        Image background = NewImage("Background", bar, null, DarkBar, false);
        Stretch(background.rectTransform, 3, 3, 3, 3);
        Image fill = NewImage("Fill", bar, barFill, Orange, false);
        Stretch(fill.rectTransform, 3, 3, 3, 3);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        TextMeshProUGUI countdownText = NewText("Countdown", hudRoot, "", 32f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(countdownText.rectTransform, TopLeft, TopLeft, new Vector2(28f, -184f), new Vector2(500f, 36f));

        // The big banner in the upper middle of the screen
        RectTransform banner = NewRect("Banner", hudRoot);
        Place(banner, TopCenter, TopCenter, new Vector2(0f, -240f), new Vector2(1200f, 200f));
        CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerGroup.interactable = false;
        bannerGroup.blocksRaycasts = false;

        TextMeshProUGUI bannerTitle = NewText("Title", banner, "WAVE 1", 96f, Orange, TextAlignmentOptions.Center);
        Place(bannerTitle.rectTransform, TopCenter, TopCenter, Vector2.zero, new Vector2(1200f, 110f));
        TextMeshProUGUI bannerSub = NewText("Sub", banner, "", 48f, Cream, TextAlignmentOptions.Center);
        Place(bannerSub.rectTransform, TopCenter, TopCenter, new Vector2(0f, -112f), new Vector2(1200f, 60f));

        MayhemHUD hud = canvas.gameObject.AddComponent<MayhemHUD>();
        hud.manager = manager;
        hud.waveText = waveText;
        hud.enemiesText = enemiesText;
        hud.countdownText = countdownText;
        hud.progressFill = fill;
        hud.bannerGroup = bannerGroup;
        hud.bannerRect = banner;
        hud.bannerTitle = bannerTitle;
        hud.bannerSub = bannerSub;

        // ---- The switch between modes ----
        GameModeController controller = Object.FindFirstObjectByType<GameModeController>();
        if (controller == null)
        {
            GameObject go = new GameObject("GameModeController");
            Undo.RegisterCreatedObjectUndo(go, "Setup Mayhem");
            controller = go.AddComponent<GameModeController>();
        }
        controller.mayhem = manager;
        controller.classicSpawner = classicSpawner;
        controller.mayhemHud = canvas.gameObject;
        EditorUtility.SetDirty(controller);

        // Edit mode shows both; the controller turns the right one on when the scene starts
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
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
            "\n\nThey come from the UI pack. Import them into the Assets folder and try again.", "OK");
        return true;
    }

    static Sprite LoadSprite(string name, Vector4 border, List<string> missing)
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
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
            if (changed) importer.SaveAndReimport();

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
        foreach (string guid in AssetDatabase.FindAssets(name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name && AssetImporter.GetAtPath(path) is AudioImporter)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
