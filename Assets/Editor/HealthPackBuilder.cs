using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
//
//   Tools > Tank Game > Setup Health Packs In This Scene       (run it inside a map scene)
//   Tools > Tank Game > Setup Health Packs In All Map Scenes   (does the same for every map in the Build Profiles list)
//
// It creates the HealthPackBox prefab (once), a HealthPackSpawner in the scene, and the green screen effect.
// Running it again is safe.
public static class HealthPackBuilder
{
    [MenuItem("Tools/Tank Game/Setup Health Packs In This Scene")]
    static void SetupThisScene()
    {
        GameObject prefab = EnsurePrefab();
        if (prefab == null) return;

        if (SetupOpenScene(prefab))
            Debug.Log("Health packs are set up in this scene. Save it with Ctrl+S.");
    }

    [MenuItem("Tools/Tank Game/Setup Health Packs In All Map Scenes")]
    static void SetupAllScenes()
    {
        GameObject prefab = EnsurePrefab();
        if (prefab == null) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string original = EditorSceneManager.GetActiveScene().path;
        int done = 0;

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            string name = Path.GetFileNameWithoutExtension(scene.path);
            if (string.Equals(name, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) continue;

            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            if (SetupOpenScene(prefab))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                done++;
            }
        }

        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log("Health packs set up and saved in " + done + " map scene(s).");
    }

    // ------------------------------------------------------------------
    //  The prefab
    // ------------------------------------------------------------------
    static GameObject EnsurePrefab()
    {
        List<string> missing = new List<string>();
        Sprite box = LoadSprite("pickup_box_health", 40f, Vector4.zero, FilterMode.Point, missing);
        if (ReportMissing(missing)) return null;

        AudioClip sound = LoadClip("health_pickup");

        string path = FindPrefabPath("HealthPackBox");
        if (path == null)
        {
            path = "Assets/HealthPackBox.prefab";

            GameObject temp = new GameObject("HealthPackBox");
            BoxCollider2D collider = temp.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.9f, 0.9f);
            temp.AddComponent<HealthPackPickup>();

            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            HealthPackPickup pack = contents.GetComponent<HealthPackPickup>();
            if (pack == null) pack = contents.AddComponent<HealthPackPickup>();
            pack.boxSprite = box;
            if (sound != null) pack.pickupSound = sound;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        if (sound == null)
            Debug.Log("No 'health_pickup' sound found, so collecting a pack is silent. Import health_pickup.wav and run this tool again.");

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ------------------------------------------------------------------
    //  The scene
    // ------------------------------------------------------------------
    static bool SetupOpenScene(GameObject prefab)
    {
        List<string> missing = new List<string>();
        Sprite vignette = LoadSprite("ui_vignette", 100f, Vector4.zero, FilterMode.Bilinear, missing);
        if (ReportMissing(missing)) return false;

        // ---- The spawner ----
        HealthPackSpawner spawner = Object.FindFirstObjectByType<HealthPackSpawner>();
        if (spawner == null)
        {
            GameObject go = new GameObject("HealthPackSpawner");
            Undo.RegisterCreatedObjectUndo(go, "Setup Health Packs");
            spawner = go.AddComponent<HealthPackSpawner>();
        }
        spawner.packPrefab = prefab;
        EditorUtility.SetDirty(spawner);

        // ---- The green glow at the screen edges ----
        GameObject old = GameObject.Find("HealEffectCanvas");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject canvasObject = new GameObject("HealEffectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Setup Health Packs");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;   // behind the other HUD canvases

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("HealVignette", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rt = imageObject.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = vignette;
        image.color = new Color(0.25f, 1f, 0.4f, 0f);
        image.raycastTarget = false;

        HealEffectUI effect = canvasObject.AddComponent<HealEffectUI>();
        effect.vignette = image;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
    }

    // ------------------------------------------------------------------
    //  Asset loading
    // ------------------------------------------------------------------
    static bool ReportMissing(List<string> missing)
    {
        if (missing.Count == 0) return false;

        EditorUtility.DisplayDialog("Missing images",
            "These images were not found in the project:\n\n  " + string.Join("\n  ", missing) +
            "\n\npickup_box_health.png is in this pack's Textures folder. ui_vignette.png comes from the UI pack. " +
            "Import them into the Assets folder and try again.", "OK");
        return true;
    }

    static string FindPrefabPath(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return path;
        }
        return null;
    }

    static Sprite LoadSprite(string name, float pixelsPerUnit, Vector4 border, FilterMode filter, List<string> missing)
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
            if (importer.filterMode != filter) { importer.filterMode = filter; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; changed = true; }
            if (changed) importer.SaveAndReimport();

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
