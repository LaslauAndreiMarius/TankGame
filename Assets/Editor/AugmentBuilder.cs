using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
//
//   Tools > Tank Game > Setup Augments                    (once, and again whenever you add a new augment)
//   Tools > Tank Game > Add Augments Tab To Garage        (run it inside the MainMenu scene)
//   Tools > Tank Game > Setup Augments In This Scene      (run it inside a map scene)
//   Tools > Tank Game > Setup Augments In All Map Scenes  (does the same for every map in the Build Profiles list)
//   Tools > Tank Game > Cheats > ...                      (unlock / reset augments for testing)
public static class AugmentBuilder
{
    const float PixelScale = 3f;

    static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    static readonly Color Orange = new Color32(0xE8, 0x87, 0x2B, 255);

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopRight = new Vector2(1f, 1f);
    static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    // ==================================================================
    //  1. THE AUGMENT ASSETS
    // ==================================================================
    [MenuItem("Tools/Tank Game/Setup Augments")]
    static void SetupAugments()
    {
        Directory.CreateDirectory("Assets/Resources/Augments");
        AssetDatabase.Refresh();

        List<string> missingImages = new List<string>();
        List<string> missingSounds = new List<string>();
        AudioClip ready = LoadClip("sfx_augment_ready");
        AudioClip denied = LoadClip("sfx_augment_denied");
        if (ready == null) missingSounds.Add("sfx_augment_ready");
        if (denied == null) missingSounds.Add("sfx_augment_denied");

        int count = 0;

        // One asset for every augment class. Add a new class and run this again: that is all it takes.
        foreach (System.Type type in TypeCache.GetTypesDerivedFrom<AugmentBase>())
        {
            if (type.IsAbstract) continue;

            AugmentBase instance = (AugmentBase)ScriptableObject.CreateInstance(type);
            string path = "Assets/Resources/Augments/Augment_" + instance.Id + ".asset";

            AugmentBase asset = AssetDatabase.LoadAssetAtPath<AugmentBase>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(instance, path);
                asset = instance;
            }
            else
            {
                Object.DestroyImmediate(instance);
            }

            asset.icon = LoadSprite("icon_augment_" + asset.Id, Vector4.zero, missingImages);
            asset.useSound = LoadClip("sfx_augment_" + asset.Id);
            if (asset.useSound == null) missingSounds.Add("sfx_augment_" + asset.Id);
            asset.readySound = ready;
            asset.deniedSound = denied;

            EditorUtility.SetDirty(asset);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Augments ready: " + count + " augment(s).");
        if (missingImages.Count > 0 || missingSounds.Count > 0)
        {
            string message = "";
            if (missingImages.Count > 0)
                message += "Icons not found (an augment without its icon is hidden from the garage):\n  " + string.Join("\n  ", missingImages) + "\n\n";
            if (missingSounds.Count > 0)
                message += "Sounds not found (the augment will be silent):\n  " + string.Join("\n  ", missingSounds) + "\n\n";
            message += "They are in the Augment pack's Textures and Audio folders. Import them and run this tool again.";
            EditorUtility.DisplayDialog("Some files are missing", message, "OK");
        }
    }

    // ==================================================================
    //  2. THE AUGMENTS TAB IN THE GARAGE (main menu)
    // ==================================================================
    [MenuItem("Tools/Tank Game/Add Augments Tab To Garage")]
    static void AddAugmentsTab()
    {
        GameObject panelObject = GameObject.Find("GaragePanel");
        GarageUI ui = panelObject != null ? panelObject.GetComponent<GarageUI>() : null;
        if (ui == null || ui.turretTabButton == null)
        {
            EditorUtility.DisplayDialog("Garage",
                "Open the MainMenu scene and run Tools > Tank Game > Build Garage UI first. " +
                "This tool adds the AUGMENTS tab to that garage.", "OK");
            return;
        }

        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", new Vector4(8, 8, 8, 8), missing);
        if (ReportMissing(missing)) return;

        Transform box = ui.turretTabButton.transform.parent;   // the big garage frame

        // Running the tool again replaces what it added before
        DestroyChild(box, "AugmentsTab");
        DestroyChild(box, "AugmentInfoPanel");

        // The new tab is a copy of the TURRETS tab, moved one place to the right
        GameObject tab = Object.Instantiate(ui.turretTabButton.gameObject, box);
        tab.name = "AugmentsTab";
        Undo.RegisterCreatedObjectUndo(tab, "Add Augments Tab");
        tab.GetComponent<RectTransform>().anchoredPosition = new Vector2(1292f, -110f);
        tab.GetComponentInChildren<TextMeshProUGUI>().text = "AUGMENTS";

        // The panel that replaces the hull / turret stats while this tab is open
        Image infoImage = NewImage("AugmentInfoPanel", box, panel, Color.white, true);
        RectTransform info = infoImage.rectTransform;
        Place(info, TopLeft, TopLeft, new Vector2(48f, -604f), new Vector2(560f, 348f));

        TextMeshProUGUI nameText = NewText("Name", info, "AUGMENT", 32f, Orange, TextAlignmentOptions.TopLeft);
        Place(nameText.rectTransform, TopLeft, TopLeft, new Vector2(24f, -16f), new Vector2(512f, 40f));
        TextMeshProUGUI description = NewText("Description", info, "", 32f, Cream, TextAlignmentOptions.TopLeft);
        Place(description.rectTransform, TopLeft, TopLeft, new Vector2(24f, -70f), new Vector2(512f, 150f));
        TextMeshProUGUI details = NewText("Details", info, "", 32f, Orange, TextAlignmentOptions.TopLeft);
        Place(details.rectTransform, TopLeft, TopLeft, new Vector2(24f, -236f), new Vector2(512f, 100f));
        info.gameObject.SetActive(false);

        Transform stats = box.Find("StatsPanel");

        Undo.RecordObject(ui, "Add Augments Tab");
        ui.augmentTabButton = tab.GetComponent<Button>();
        ui.augmentTabImage = tab.GetComponent<Image>();
        ui.statsRoot = stats != null ? stats.gameObject : null;
        ui.augmentInfoRoot = info.gameObject;
        ui.augmentName = nameText;
        ui.augmentDescription = description;
        ui.augmentDetails = details;
        EditorUtility.SetDirty(ui);

        if (stats == null)
            Debug.LogWarning("Could not find the 'StatsPanel' object, so the stats will stay visible on the AUGMENTS tab.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = tab;
        Debug.Log("AUGMENTS tab added to the garage.");
    }

    // ==================================================================
    //  3. THE AUGMENT SYSTEM IN THE MAP SCENES
    // ==================================================================
    [MenuItem("Tools/Tank Game/Setup Augments In This Scene")]
    static void SetupThisScene()
    {
        if (SetupOpenScene())
            Debug.Log("Augments are set up in this scene. Save it with Ctrl+S.");
    }

    [MenuItem("Tools/Tank Game/Setup Augments In All Map Scenes")]
    static void SetupAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string original = EditorSceneManager.GetActiveScene().path;
        int done = 0;

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            string name = Path.GetFileNameWithoutExtension(scene.path);
            if (string.Equals(name, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) continue;

            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            if (SetupOpenScene())
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                done++;
            }
        }

        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log("Augments set up and saved in " + done + " map scene(s).");
    }

    static bool SetupOpenScene()
    {
        List<string> missing = new List<string>();
        Vector4 slice = new Vector4(8, 8, 8, 8);
        Sprite panel = LoadSprite("ui_panel", slice, missing);
        Sprite buttonNormal = LoadSprite("ui_button_normal", slice, missing);
        Sprite buttonHover = LoadSprite("ui_button_hover", slice, missing);
        Sprite barFill = LoadSprite("ui_bar_fill", Vector4.zero, missing);
        Sprite vignetteSprite = LoadSprite("ui_vignette", Vector4.zero, missing, FilterMode.Bilinear);
        if (ReportMissing(missing)) return false;

        // ---- The controller ----
        if (Object.FindFirstObjectByType<AugmentController>() == null)
        {
            GameObject go = new GameObject("AugmentController");
            Undo.RegisterCreatedObjectUndo(go, "Setup Augments");
            go.AddComponent<AugmentController>();
        }

        // ---- The widget and the screen glow ----
        DestroyExisting("AugmentHUDCanvas");
        Canvas canvas = NewCanvas("AugmentHUDCanvas");
        canvas.sortingOrder = -1;   // behind the other HUD canvases
        Transform root = canvas.transform;

        Image vignette = NewImage("AugmentVignette", root, vignetteSprite, new Color(1f, 1f, 1f, 0f), false);
        Stretch(vignette.rectTransform);

        RectTransform widget = NewRect("AugmentWidget", root);
        Place(widget, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(176f, 190f));

        Image background = NewImage("Background", widget, panel, Color.white, true);
        Stretch(background.rectTransform);

        Image frame = NewImage("IconFrame", widget, buttonNormal, Color.white, true);
        Place(frame.rectTransform, TopCenter, TopCenter, new Vector2(0f, -16f), new Vector2(112f, 112f));

        Image icon = NewImage("Icon", frame.rectTransform, null, Color.white, false);
        icon.preserveAspect = true;
        Place(icon.rectTransform, Center, Center, Vector2.zero, new Vector2(80f, 80f));

        // A dark sweep over the icon that melts away while the augment recharges
        Image cooldown = NewImage("Cooldown", frame.rectTransform, barFill, new Color(0f, 0f, 0f, 0.65f), false);
        Place(cooldown.rectTransform, Center, Center, Vector2.zero, new Vector2(88f, 88f));
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;

        // The orange frame that pulses when the augment is ready
        Image glow = NewImage("ReadyGlow", widget, buttonHover, new Color(1f, 1f, 1f, 0f), true);
        Place(glow.rectTransform, TopCenter, TopCenter, new Vector2(0f, -16f), new Vector2(112f, 112f));

        Image badge = NewImage("KeyBadge", widget, buttonNormal, Color.white, true);
        Place(badge.rectTransform, TopRight, TopRight, new Vector2(-6f, -6f), new Vector2(56f, 56f));
        TextMeshProUGUI keyText = NewText("Key", badge.rectTransform, "E", 32f, Cream, TextAlignmentOptions.Center);
        Stretch(keyText.rectTransform);

        TextMeshProUGUI state = NewText("State", widget, "READY", 32f, Cream, TextAlignmentOptions.Center);
        Place(state.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 14f), new Vector2(164f, 40f));

        UIPunch punch = widget.gameObject.AddComponent<UIPunch>();

        AugmentHUD hud = canvas.gameObject.AddComponent<AugmentHUD>();
        hud.widget = widget.gameObject;
        hud.icon = icon;
        hud.cooldownOverlay = cooldown;
        hud.readyGlow = glow;
        hud.keyBadge = badge.gameObject;
        hud.keyText = keyText;
        hud.stateText = state;
        hud.punch = punch;
        hud.vignette = vignette;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
    }

    // ==================================================================
    //  4. CHEATS FOR TESTING
    // ==================================================================
    [MenuItem("Tools/Tank Game/Cheats/Unlock All Augments")]
    static void CheatUnlock()
    {
        AugmentProfile.UnlockAll();
        Debug.Log("All augments unlocked.");
    }

    [MenuItem("Tools/Tank Game/Cheats/Reset Augments")]
    static void CheatReset()
    {
        AugmentProfile.ResetAll();
        Debug.Log("Augments reset: none owned, none equipped.");
    }

    // ==================================================================
    //  HELPERS
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

    static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
    }

    static bool ReportMissing(List<string> missing)
    {
        if (missing.Count == 0) return false;

        EditorUtility.DisplayDialog("Missing images",
            "These images were not found in the project:\n\n  " + string.Join("\n  ", missing) +
            "\n\nThey come from the UI pack and the Augment pack. Import them into the Assets folder and try again.", "OK");
        return true;
    }

    static Sprite LoadSprite(string name, Vector4 border, List<string> missing, FilterMode filter = FilterMode.Point)
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
            if (importer.filterMode != filter) { importer.filterMode = filter; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
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
