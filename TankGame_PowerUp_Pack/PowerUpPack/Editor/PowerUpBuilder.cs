using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
//
//   Tools > Tank Game > Setup Power-Up Boxes   (updates your PowerUpBox prefab, works from any scene)
//   Tools > Tank Game > Build Power-Up HUD     (run it inside a MAP scene)
//
// Running either tool again is safe: it replaces what it built before.
public static class PowerUpBuilder
{
    // 1 texture pixel of the pixel-art UI = this many pixels on a 1920x1080 screen
    const float PixelScale = 3f;

    static readonly Color Cream = new Color32(0xF2, 0xE6, 0xC0, 255);
    static readonly Color DarkBar = new Color32(0x14, 0x16, 0x1A, 255);
    static readonly Color Outline = new Color32(0x0B, 0x0C, 0x0E, 255);

    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 RightMiddle = new Vector2(1f, 0.5f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    // ==================================================================
    //  1. THE BOXES IN THE WORLD
    // ==================================================================
    [MenuItem("Tools/Tank Game/Setup Power-Up Boxes")]
    static void SetupBoxes()
    {
        List<string> missing = new List<string>();
        // 32 pixel boxes at 40 pixels-per-unit = 0.8 world units wide, like the old squares
        Sprite damage = LoadSprite("pickup_box_damage", Vector4.zero, 40f, missing);
        Sprite armor = LoadSprite("pickup_box_armor", Vector4.zero, 40f, missing);
        Sprite speed = LoadSprite("pickup_box_speed", Vector4.zero, 40f, missing);
        if (ReportMissing(missing)) return;

        AudioClip sound = LoadClip("powerup_pickup");

        // Use your existing prefab, or make a new one if it can't be found
        string path = FindPrefabPath("PowerUpBox");
        bool created = false;
        if (path == null)
        {
            path = "Assets/PowerUpBox.prefab";
            GameObject temp = new GameObject("PowerUpBox");
            temp.AddComponent<BoxCollider2D>();
            temp.AddComponent<PowerUpPickup>();
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            created = true;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            PowerUpPickup pickup = contents.GetComponent<PowerUpPickup>();
            if (pickup == null) pickup = contents.AddComponent<PowerUpPickup>();
            pickup.damageBox = damage;
            pickup.armorBox = armor;
            pickup.speedBox = speed;
            if (sound != null) pickup.pickupSound = sound;

            BoxCollider2D col = contents.GetComponent<BoxCollider2D>();
            if (col == null) col = contents.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);
            col.offset = Vector2.zero;

            contents.transform.localScale = Vector3.one;   // the old prefab was scaled to 0.8
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // If a spawner is open in the scene, make sure it uses this prefab
        PowerUpSpawner spawner = Object.FindFirstObjectByType<PowerUpSpawner>();
        if (spawner != null && (created || spawner.pickupPrefab == null))
        {
            Undo.RecordObject(spawner, "Setup Power-Up Boxes");
            spawner.pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(spawner);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Power-up boxes updated: " + path + (created
            ? "  (a new prefab was created - drag it into the PowerUpSpawner's 'Pickup Prefab' slot if it isn't there)"
            : ""));
        if (sound == null)
            Debug.Log("No 'powerup_pickup' sound found, so collecting a box is silent. Import powerup_pickup.wav and run this tool again.");
    }

    // ==================================================================
    //  2. THE HUD CARDS
    // ==================================================================
    [MenuItem("Tools/Tank Game/Build Power-Up HUD")]
    static void BuildHud()
    {
        List<string> missing = new List<string>();
        Sprite panel = LoadSprite("ui_panel", new Vector4(8, 8, 8, 8), 100f, missing);
        Sprite fill = LoadSprite("ui_bar_fill", Vector4.zero, 100f, missing);
        Sprite damageIcon = LoadSprite("icon_damage", Vector4.zero, 100f, missing);
        Sprite armorIcon = LoadSprite("icon_armor", Vector4.zero, 100f, missing);
        Sprite speedIcon = LoadSprite("icon_speed", Vector4.zero, 100f, missing);
        if (ReportMissing(missing)) return;

        DestroyExisting("PowerUpHUDCanvas");
        Canvas canvas = NewCanvas("PowerUpHUDCanvas");

        // A row at the bottom of the screen. Hidden cards take no space, so the visible ones stay centered.
        RectTransform row = NewRect("Cards", canvas.transform);
        Place(row, BottomCenter, BottomCenter, new Vector2(0f, 28f), new Vector2(1240f, 100f));

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        PowerUpHUD hud = canvas.gameObject.AddComponent<PowerUpHUD>();
        hud.entries = new PowerUpHUD.Entry[3];
        hud.entries[0] = BuildCard(row, PowerUpType.DoubleDamage, panel, damageIcon, fill);
        hud.entries[1] = BuildCard(row, PowerUpType.DoubleArmor, panel, armorIcon, fill);
        hud.entries[2] = BuildCard(row, PowerUpType.DoubleSpeed, panel, speedIcon, fill);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;

        foreach (PowerUpHUD other in Object.FindObjectsByType<PowerUpHUD>(FindObjectsSortMode.None))
        {
            if (other != hud)
                Debug.LogWarning("Found an OLD power-up HUD on '" + other.gameObject.name + "'. Delete that object (and its old timer bars).");
        }
        Debug.Log("Power-up HUD built. Press Play and collect a box to see it.");
    }

    static PowerUpHUD.Entry BuildCard(Transform parent, PowerUpType type, Sprite panel, Sprite icon, Sprite fillSprite)
    {
        Color color = PowerUpStyle.ColorFor(type);

        Image cardImg = NewImage(type + "Card", parent, panel, Color.white, true);
        RectTransform card = cardImg.rectTransform;
        card.sizeDelta = new Vector2(360f, 100f);   // the layout group positions it
        CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();

        Image iconImg = NewImage("Icon", card, icon, Color.white, false);
        PlaceLeft(iconImg.rectTransform, 28f, new Vector2(48f, 48f));

        TextMeshProUGUI label = NewText("Label", card, PowerUpStyle.LabelFor(type), 32f, color, TextAlignmentOptions.MidlineLeft);
        Place(label.rectTransform, TopLeft, TopLeft, new Vector2(92f, -14f), new Vector2(200f, 36f));

        // The bar: black outline, dark background, colored fill. The 3 pixel inset is the outline.
        RectTransform bar = NewRect("Bar", card);
        Place(bar, TopLeft, TopLeft, new Vector2(92f, -54f), new Vector2(150f, 30f));

        Image outline = NewImage("Outline", bar, null, Outline, false);
        Stretch(outline.rectTransform);
        Image background = NewImage("Background", bar, null, DarkBar, false);
        Stretch(background.rectTransform, 3, 3, 3, 3);
        Image fill = NewImage("Fill", bar, fillSprite, color, false);
        Stretch(fill.rectTransform, 3, 3, 3, 3);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        TextMeshProUGUI time = NewText("Time", card, "15s", 48f, Cream, TextAlignmentOptions.MidlineRight);
        Place(time.rectTransform, RightMiddle, RightMiddle, new Vector2(-24f, 0f), new Vector2(96f, 60f));

        card.gameObject.SetActive(false);   // cards only appear while their effect is active

        PowerUpHUD.Entry entry = new PowerUpHUD.Entry();
        entry.type = type;
        entry.card = card;
        entry.group = group;
        entry.bar = fill;
        entry.timeText = time;
        return entry;
    }

    // ==================================================================
    //  UI HELPERS
    // ==================================================================
    static Canvas NewCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Build Power-Up HUD");

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
        rt.pivot = new Vector2(0.5f, 0.5f);
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

        EditorUtility.DisplayDialog("Missing textures",
            "These images were not found in the project:\n\n  " + string.Join("\n  ", missing) +
            "\n\nImport them into the Assets folder (the file names must match exactly) and try again.", "OK");
        return true;
    }

    static string FindPrefabPath(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return path;
        }
        return null;
    }

    static Sprite LoadSprite(string name, Vector4 border, float pixelsPerUnit, List<string> missing)
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
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)) { importer.spritePixelsPerUnit = pixelsPerUnit; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
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
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
