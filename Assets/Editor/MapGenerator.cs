using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// EDITOR TOOL: this file must live in a folder named exactly "Editor" (e.g. Assets/Editor).
// Use it from the menu:  Tools > Tank Game > Generate Map
// It builds ground, outer walls and obstacles in the currently open scene.
// Running it again replaces the previous result, so you can tweak the layout and re-run.
public static class MapGenerator
{
    const string RootName = "MapGeometry";

    static readonly int[] Signs = { -1, 1 };
    static System.Random rng;

    // ------------------------------------------------------------------
    //  MENU ITEMS
    // ------------------------------------------------------------------
    [MenuItem("Tools/Tank Game/Generate Map")]
    static void Generate()
    {
        // 1. Find the textures anywhere in the project (and set them up as sprites)
        Sprite ground = LoadSprite("simple_ground", 0f);   // 0 = keep its existing Pixels Per Unit
        Sprite wallTile = LoadSprite("wall_tile", 64f);
        Sprite crate = LoadSprite("crate", 64f);
        Sprite rock = LoadSprite("rock", 64f);
        Sprite barrel = LoadSprite("barrel", 64f);
        Sprite trap = LoadSprite("tank_trap", 64f);

        string missing = "";
        if (ground == null) missing += "  simple_ground\n";
        if (wallTile == null) missing += "  wall_tile\n";
        if (crate == null) missing += "  crate\n";
        if (rock == null) missing += "  rock\n";
        if (barrel == null) missing += "  barrel\n";
        if (trap == null) missing += "  tank_trap\n";

        if (missing != "")
        {
            EditorUtility.DisplayDialog("Missing textures",
                "These images were not found in the project:\n\n" + missing +
                "\nImport them into the Assets folder (the file names must match exactly) and try again.",
                "OK");
            return;
        }

        // 2. Remove a previous generated map, if there is one
        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        // 3. Create the parent objects
        rng = new System.Random(1234);   // fixed seed = same "random" rotations every time

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Map");

        Transform walls = NewGroup("Walls", root.transform);
        Transform obstacles = NewGroup("Obstacles", root.transform);

        // 4. Ground (no collider on purpose: the spawners treat colliders as "blocked")
        GameObject g = new GameObject("Ground");
        g.transform.SetParent(root.transform);
        g.transform.position = Vector3.zero;
        SpriteRenderer gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = ground;
        gsr.drawMode = SpriteDrawMode.Tiled;
        gsr.size = new Vector2(44f, 34f);
        gsr.sortingOrder = -10;

        // 5. Outer walls (inside edge: x = +-20, y = +-15)
        AddWall(walls, "Wall_Top", new Vector2(0f, 15.5f), new Vector2(42f, 1f), wallTile, 0);
        AddWall(walls, "Wall_Bottom", new Vector2(0f, -15.5f), new Vector2(42f, 1f), wallTile, 0);
        AddWall(walls, "Wall_Left", new Vector2(-20.5f, 0f), new Vector2(1f, 32f), wallTile, 0);
        AddWall(walls, "Wall_Right", new Vector2(20.5f, 0f), new Vector2(1f, 32f), wallTile, 0);

        // 6. The layout. Every block below is mirrored into all 4 corners of the map.
        //    Positions are in world units. Edit the numbers to change the map and run the tool again.
        //    The area around (0, 0) stays empty because that is where the player starts.
        foreach (int sx in Signs)
        {
            foreach (int sy in Signs)
            {
                // Corner bunkers: an L-shaped concrete wall
                AddWall(obstacles, "Bunker_H", P(14.5f, 11.5f, sx, sy), new Vector2(6f, 1f), wallTile, 0);
                AddWall(obstacles, "Bunker_V", P(17f, 8.5f, sx, sy), new Vector2(1f, 5f), wallTile, 0);

                // Crate cluster
                AddCrate(obstacles, P(6f, 5f, sx, sy), crate);
                AddCrate(obstacles, P(7f, 5f, sx, sy), crate);
                AddCrate(obstacles, P(6f, 6f, sx, sy), crate);

                // Tank traps in a line with gaps a tank can drive through
                AddTrap(obstacles, P(2.5f, 9f, sx, sy), trap);
                AddTrap(obstacles, P(5f, 9f, sx, sy), trap);
                AddTrap(obstacles, P(7.5f, 9f, sx, sy), trap);

                // Rocks
                AddRock(obstacles, P(14.5f, 3.5f, sx, sy), rock);
                AddRock(obstacles, P(4f, 12.5f, sx, sy), rock);

                // Barrel group
                AddBarrel(obstacles, P(10f, 8.5f, sx, sy), barrel);
                AddBarrel(obstacles, P(10.8f, 8.8f, sx, sy), barrel);
                AddBarrel(obstacles, P(10.3f, 7.7f, sx, sy), barrel);
            }
        }

        // Rocks on the middle axes (written out by hand, since they aren't mirrored 4 ways)
        AddRock(obstacles, new Vector2(12f, 0f), rock);
        AddRock(obstacles, new Vector2(-12f, 0f), rock);
        AddRock(obstacles, new Vector2(0f, 6.5f), rock);
        AddRock(obstacles, new Vector2(0f, -6.5f), rock);

        // 7. Finish
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log("Map generated: " + walls.childCount + " walls, " + obstacles.childCount +
                  " obstacles. Remember to delete any OLD ground/walls from this scene, and save it (Ctrl+S).");

        if (GameObject.Find("Ground") != null && GameObject.Find("Ground") != g)
            Debug.LogWarning("There is another object called 'Ground' in the scene. Delete the old one so they don't overlap.");
    }

    [MenuItem("Tools/Tank Game/Remove Generated Map")]
    static void Remove()
    {
        GameObject old = GameObject.Find(RootName);
        if (old == null) { Debug.Log("No generated map found in this scene."); return; }

        Undo.DestroyObjectImmediate(old);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ------------------------------------------------------------------
    //  BUILDING BLOCKS
    // ------------------------------------------------------------------

    // Mirror helper: (x, y) in the top-right corner -> any of the four corners
    static Vector2 P(float x, float y, int sx, int sy)
    {
        return new Vector2(x * sx, y * sy);
    }

    static Transform NewGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = Vector3.zero;
        return go.transform;
    }

    // A rectangular wall made of repeating tiles, with a matching box collider
    static void AddWall(Transform parent, string name, Vector2 pos, Vector2 size, Sprite tile, int order)
    {
        GameObject go = NewObject(name, parent, pos, tile, order);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = size;
    }

    static void AddCrate(Transform parent, Vector2 pos, Sprite sprite)
    {
        GameObject go = NewObject("Crate", parent, pos, sprite, 0);
        go.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(-12f, 12f));

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.95f, 0.95f);
    }

    static void AddRock(Transform parent, Vector2 pos, Sprite sprite)
    {
        GameObject go = NewObject("Rock", parent, pos, sprite, 0);
        go.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(0f, 360f));

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.radius = 0.55f;
    }

    static void AddBarrel(Transform parent, Vector2 pos, Sprite sprite)
    {
        GameObject go = NewObject("Barrel", parent, pos, sprite, 0);
        go.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(0f, 360f));

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.radius = 0.28f;
    }

    static void AddTrap(Transform parent, Vector2 pos, Sprite sprite)
    {
        GameObject go = NewObject("TankTrap", parent, pos, sprite, 0);
        go.transform.rotation = Quaternion.Euler(0f, 0f, RandomRange(0f, 120f));

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.radius = 0.38f;
    }

    // Creates a GameObject with a SpriteRenderer (shared by all the helpers above)
    static GameObject NewObject(string name, Transform parent, Vector2 pos, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;

        // If you create a Layer called "Obstacle", everything built here is put on it.
        // That's the layer EnemyAI's "Obstacle Mask" uses to stop enemies shooting through walls.
        int layer = LayerMask.NameToLayer("Obstacle");
        if (layer >= 0) go.layer = layer;

        return go;
    }

    static float RandomRange(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    // ------------------------------------------------------------------
    //  TEXTURE SETUP
    // ------------------------------------------------------------------

    // Finds an image by file name (without extension) and makes sure it is imported as a sprite
    static Sprite LoadSprite(string name, float pixelsPerUnit)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (pixelsPerUnit > 0f && !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();

            // Tiled drawing needs the sprite's mesh type to be "Full Rect"
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

        return null;
    }
}
