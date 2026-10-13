using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureDemoOpening
{
    private static Material Stone => AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Demo painted castle.mat");
    private static Material Wood => AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Walnut wood.mat");
    private static Material LightStone => AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Light limestone.mat");
    private static Texture2D props;
    [MenuItem("Arkmon/Build demo opening")]
    public static void Run()
    {
        ImportSprite("Assets/Resources/John-Summoning.png", 768);
        ImportSprite("Assets/Resources/Prince-Room.png", 768);
        ImportSprite("Assets/Resources/Prince-Mantle.png", 768);
        props = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/SceneStyle/DemoProps.png");
        string stonePath = "Assets/Art/SceneStyle/Demo painted castle.mat";
        Material painted = AssetDatabase.LoadAssetAtPath<Material>(stonePath);
        if (painted == null) { painted = new Material(Shader.Find("Arkmon/Painted castle")); AssetDatabase.CreateAsset(painted, stonePath); }
        painted.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/SceneStyle/DemoStone.png"));
        EditorUtility.SetDirty(painted);
        if (props == null) throw new System.InvalidOperationException("Demo prop atlas missing");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(props));
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.SaveAndReimport();
        props = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/SceneStyle/DemoProps.png");
        MonsterDefinition cat = AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Scriptable Objects/Monsters/Flaca.asset");
        cat.name = "Kafla"; EditorUtility.SetDirty(cat);
        Scene court = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        // Retain scene systems and the existing meadow, replace the courtyard geometry.
        foreach (GameObject root in court.GetRootGameObjects())
            if (!KeepSystem(root) && root.name != "Castle horizon and golden fields") Object.DestroyImmediate(root);
        var world = new GameObject("Demo castle courtyard");
        var landscape = court.GetRootGameObjects().First(r => r.name == "Castle horizon and golden fields");
        ClearLandscape(landscape, new Rect(-26, -10, 52, 20));
        Block(world.transform, "Courtyard stone floor", new Vector3(0, -.25f, 0), new Vector3(48, .5f, 16), Stone).tag = "Ground";
        Block(world.transform, "Back rampart", new Vector3(0, 1.2f, 8), new Vector3(48, 2.4f, .7f), Stone);
        Block(world.transform, "West tower", new Vector3(-22, 3, 6), new Vector3(4, 6, 4), Stone);
        Block(world.transform, "East tower", new Vector3(22, 3, 6), new Vector3(4, 6, 4), Stone);
        for (int x = -20; x <= 20; x += 2) Block(world.transform, "Rampart crown", new Vector3(x, 2.7f, 8), new Vector3(.8f, .7f, .9f), LightStone);
        Block(world.transform, "South boundary", new Vector3(0, .35f, -8), new Vector3(48, .7f, .7f), Stone);
        Block(world.transform, "West boundary", new Vector3(-24, 1, 0), new Vector3(.6f, 2, 16), Stone);
        Block(world.transform, "Gate left pillar", new Vector3(24, 2, -4), new Vector3(1, 4, 6), Stone);
        Block(world.transform, "Gate right pillar", new Vector3(24, 2, 5), new Vector3(1, 4, 5), Stone);
        Block(world.transform, "Gate arch", new Vector3(24, 4.2f, .5f), new Vector3(1.4f, .8f, 4), LightStone);
        Interact(world.transform, "Outside gate", new Vector3(22, 0, .5f), DemoInteractable.Kind.CourtyardGate);
        Prop(world.transform, "Stable", 5, new Vector3(-10, 0, 5), 5.5f, true);
        Prop(world.transform, "Wheelbarrow", 4, new Vector3(-6, 0, 4), 2.3f, true);
        Block(world.transform, "Tower entrance", new Vector3(-20, 1.5f, 4), new Vector3(1.8f, 3, .25f), Wood);
        Block(world.transform, "Workbench", new Vector3(-15, .8f, 3), new Vector3(2.5f, .3f, 1), Wood);
        foreach (int side in new[] { -1, 1 }) Block(world.transform, "Workbench legs", new Vector3(-15 + side, .4f, 3), new Vector3(.2f, .8f, .8f), Wood);
        for (int x = -18; x <= 18; x += 9)
        {
            Block(world.transform, "Arrival banner", new Vector3(x, 1.7f, 7.55f), new Vector3(.65f, 1.5f, .06f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Garden green.mat"), false);
            Block(world.transform, "Banner gold clasp", new Vector3(x, 2.45f, 7.5f), new Vector3(.85f, .12f, .1f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Amber rune.mat"), false);
        }
        for (int i = 0; i < 3; i++) Crate(world.transform, new Vector3(-14 + i * 1.2f, 0, 5));
        Block(world.transform, "Garden soil", new Vector3(5, .06f, 5), new Vector3(6, .1f, 3), Wood, false);
        for (int i = 0; i < 8; i++)
        {
            var flower = GameObject.CreatePrimitive(PrimitiveType.Quad); flower.name = "Garden flowers"; flower.transform.SetParent(world.transform);
            flower.transform.position = new Vector3(2.5f + i * .7f, .55f, 5); flower.transform.localScale = Vector3.one;
            Object.DestroyImmediate(flower.GetComponent<Collider>());
            flower.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MeadowFlowers-Paper.mat");
        }
        Block(world.transform, "Fountain basin", new Vector3(-1, .45f, 3), new Vector3(2.4f, .9f, 2), LightStone);
        Block(world.transform, "Fountain water", new Vector3(-1, .91f, 3), new Vector3(1.9f, .04f, 1.5f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Blue.mat"), false);
        Block(world.transform, "Bench seat", new Vector3(4, .55f, 2), new Vector3(2.8f, .2f, .7f), Wood);
        foreach (int side in new[] { -1, 1 }) Block(world.transform, "Bench leg", new Vector3(4 + side, .25f, 2), new Vector3(.2f, .5f, .6f), Wood);
        // NPC and player came from the scene's existing prefabs and retain dialogue/battle data.
        NPC john = Object.FindFirstObjectByType<NPC>(); john.transform.position = new Vector3(13, 0, 2);
        GameObject player = GameObject.FindWithTag("Player"); player.transform.position = new Vector3(-17, 1, 0); player.transform.rotation = Quaternion.identity;
        ConfigureCamera(player.transform, new Vector3(0, 6, -10), 50);
        var intro = world.AddComponent<DemoOpening>();
        var so = new SerializedObject(intro); so.FindProperty("towerRoom").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(court);
        // Copy the scene systems for the room, without NPC or courtyard decoration.
        EditorSceneManager.SaveScene(court, "Assets/Scenes/TowerRoom.unity", true);
        Scene room = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        foreach (GameObject root in room.GetRootGameObjects())
            if (!KeepSystem(root) || root.GetComponentInChildren<NPC>() != null) Object.DestroyImmediate(root);
        world = new GameObject("Tower room interior");
        Block(world.transform, "Stone floor", new Vector3(0, -.25f, 0), new Vector3(16, .5f, 12), Stone).tag = "Ground";
        Block(world.transform, "West wall", new Vector3(-8, 2.5f, 0), new Vector3(.6f, 5, 12), Stone);
        Block(world.transform, "East wall", new Vector3(8, 2.5f, 0), new Vector3(.6f, 5, 12), Stone);
        // A real opening frames the distant fields; the front wall is cut away for the camera.
        Block(world.transform, "Window wall left", new Vector3(-5.5f, 2.5f, 6), new Vector3(5, 5, .7f), Stone);
        Block(world.transform, "Window wall right", new Vector3(5.5f, 2.5f, 6), new Vector3(5, 5, .7f), Stone);
        Block(world.transform, "Window sill", new Vector3(0, 1, 6), new Vector3(6, 2, .7f), Stone);
        Block(world.transform, "Window lintel", new Vector3(0, 4.7f, 6), new Vector3(6, .6f, .7f), Stone);
        Block(world.transform, "Front invisible boundary", new Vector3(0, 1, -6), new Vector3(16, 2, .4f), Stone).GetComponent<Renderer>().enabled = false;
        Prop(world.transform, "Canopy bed", 0, new Vector3(-5, 0, 3), 4.2f, true);
        Prop(world.transform, "Wardrobe", 1, new Vector3(5.3f, 0, 3.5f), 3.5f, true);
        Prop(world.transform, "Writing desk", 2, new Vector3(0, 0, 4.5f), 2.7f, true);
        var rug = Prop(world.transform, "Heirloom rug", 3, new Vector3(0, .025f, 0), 5f, false);
        rug.transform.rotation = Quaternion.Euler(90, 0, 0);
        Interact(world.transform, "Take green mantle", new Vector3(4.7f, 0, 2), DemoInteractable.Kind.Wardrobe);
        Interact(world.transform, "Look over golden fields", new Vector3(0, 0, 4), DemoInteractable.Kind.Window);
        Block(world.transform, "Courtyard door", new Vector3(7.6f, 1.7f, -3), new Vector3(.2f, 3.4f, 2), Wood);
        Interact(world.transform, "Door to courtyard", new Vector3(6, 0, -3), DemoInteractable.Kind.RoomDoor);
        player = GameObject.FindWithTag("Player"); player.transform.position = new Vector3(-3, 1, 0);
        ConfigureCamera(player.transform, new Vector3(0, 6, -11), 48);
        intro = world.AddComponent<DemoOpening>(); so = new SerializedObject(intro); so.FindProperty("towerRoom").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
        Scene battle = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity", OpenSceneMode.Additive);
        var meadow = battle.GetRootGameObjects().First(r => r.name == "Idyllic meadow");
        var horizon = Object.Instantiate(meadow); SceneManager.MoveGameObjectToScene(horizon, room); horizon.name = "Tower golden field vista";
        horizon.transform.position += new Vector3(-18, -2, 24);
        ClearLandscape(horizon, new Rect(-16, -12, 32, 24));
        foreach (Collider col in horizon.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        EditorSceneManager.CloseScene(battle, true); SceneManager.SetActiveScene(room);
        EditorSceneManager.SaveScene(room);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/TowerRoom.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/StartScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/BattleScene.unity", true) };
        AssetDatabase.SaveAssets();
        ConfigureDemoPolish.Run();
    }
    private static bool KeepSystem(GameObject root) => root.GetComponentInChildren<PlayerController>() != null
        || root.GetComponentInChildren<NPC>() != null || root.GetComponentInChildren<Camera>() != null
        || root.GetComponentInChildren<CinemachineVirtualCamera>() != null || root.GetComponentInChildren<Canvas>() != null
        || root.GetComponentInChildren<SceneLoadManager>() != null || root.GetComponentInChildren<SoundManager>() != null
        || root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>() != null || root.GetComponentInChildren<Light>() != null
        || root.GetComponentInChildren<UnityEngine.Rendering.Volume>() != null;
    private static void ClearLandscape(GameObject landscape, Rect area)
    {
        foreach (var vegetation in landscape.GetComponentsInChildren<MeadowVegetation>()) vegetation.SetClearArea(area);
        foreach (Transform child in landscape.GetComponentsInChildren<Transform>())
        {
            if (child == null || child == landscape.transform || child.GetComponent<MeadowVegetation>() != null) continue;
            if ((child.name == "Meadow tree" || child.name == "Meadow wildflowers" || child.name == "Meadow thistles")
                && area.Contains(new Vector2(child.position.x, child.position.z))) Object.DestroyImmediate(child.gameObject);
        }
    }
    private static void ConfigureCamera(Transform player, Vector3 offset, float fov)
    {
        foreach (var camera in Object.FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None))
        { camera.Follow = player; camera.LookAt = player; camera.m_Lens.FieldOfView = fov;
          var follow = camera.GetCinemachineComponent<CinemachineTransposer>(); if (follow != null) follow.m_FollowOffset = offset; }
    }
    private static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material material, bool collision = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent);
        obj.transform.position = pos; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
    }
    private static GameObject Prop(Transform parent, string name, int cell, Vector3 pos, float width, bool collision)
    {
        string path = "Assets/Art/SceneStyle/Demo prop " + cell + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        // Individual bounds avoid neighbouring prop fragments where the atlas crosses a cell edge.
        Rect[] cuts = { new Rect(0, 449, 550, 575), new Rect(560, 474, 440, 550), new Rect(1010, 500, 526, 524),
            new Rect(0, 44, 565, 400), new Rect(565, 24, 450, 430), new Rect(1060, 0, 476, 490) };
        Rect cut = cuts[cell];
        Sprite updated = Sprite.Create(props, cut, new Vector2(.5f, 0), cut.width / width);
        if (sprite == null) { sprite = updated; AssetDatabase.CreateAsset(sprite, path); }
        else { EditorUtility.CopySerialized(updated, sprite); Object.DestroyImmediate(updated); EditorUtility.SetDirty(sprite); }
        var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent); obj.transform.position = pos;
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
        if (cell != 3)
        {
            var pivot = new GameObject("Paper illustration"); pivot.transform.SetParent(obj.transform, false);
            var renderer = pivot.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            Object.DestroyImmediate(obj.GetComponent<SpriteRenderer>());
            var visual = obj.AddComponent<PaperPropVisual>(); var data = new SerializedObject(visual);
            data.FindProperty("paperPivot").objectReferenceValue = pivot.transform; data.ApplyModifiedPropertiesWithoutUndo();
        }
        if (collision) { var col = obj.AddComponent<BoxCollider>(); col.center = Vector3.up * .6f; col.size = new Vector3(width * .65f, 1.2f, .8f); }
        return obj;
    }
    private static void Interact(Transform parent, string name, Vector3 pos, DemoInteractable.Kind kind)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = pos;
        var col = obj.AddComponent<SphereCollider>(); col.radius = .35f; col.isTrigger = true;
        obj.AddComponent<DemoInteractable>().kind = kind;
    }
    private static void Crate(Transform parent, Vector3 position)
    {
        var root = new GameObject("Paper supply crate"); root.transform.SetParent(parent); root.transform.position = position;
        var image = new GameObject("Crate illustration", typeof(SpriteRenderer)); image.transform.SetParent(root.transform, false);
        var body = image.GetComponent<SpriteRenderer>(); body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/WoodenCrate-Paper.png");
        image.transform.localScale = Vector3.one * (1.2f / body.sprite.bounds.size.x);
        var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * .5f; collider.size = new Vector3(1, 1, .7f);
        var visual = root.AddComponent<PaperPropVisual>(); var data = new SerializedObject(visual);
        data.FindProperty("paperPivot").objectReferenceValue = image.transform; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ImportSprite(string path, float ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing sprite: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter; settings.spritePivot = new Vector2(.5f, 0);
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
    }
}
