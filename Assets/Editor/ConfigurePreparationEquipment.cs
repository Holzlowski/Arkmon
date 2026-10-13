using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurePreparationEquipment
{
    [MenuItem("Arkmon/Configure preparation equipment")]
    public static void Run()
    {
        foreach (string name in new[] { "Prince-Room-NoLamp", "Prince-Mantle-NoLamp" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/" + name + ".png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = height / 2f;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(.5f, 0); settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        ConfigurePlayerWalk.Run();
        foreach (string name in new[] { "Prince-Room", "Prince-Mantle", "Prince-Room-NoLamp", "Prince-Mantle-NoLamp" })
        {
            string path = "Assets/Resources/" + name + "-Portrait.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/" + name + ".png");
            // Include the upper body and lamp, so equipment is visible in the portrait.
            var portrait = Sprite.Create(texture, new Rect(texture.width * .12f, texture.height * .30f,
                texture.width * .76f, texture.height * .69f), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            portrait.name = name + "-Portrait";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing == null) AssetDatabase.CreateAsset(portrait, path);
            else { EditorUtility.CopySerialized(portrait, existing); Object.DestroyImmediate(portrait); }
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        ApplyTowerLamp(); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    public static void ApplyTowerLamp()
    {
        var desk = GameObject.Find("Writing desk"); if (desk == null) return;
        var old = desk.transform.Find("Player lamp pickup"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Player lamp pickup"); root.transform.SetParent(desk.transform, false);
        root.transform.localPosition = new Vector3(.20f, 1.19f, -.22f);
        var interaction = root.AddComponent<DemoInteractable>(); interaction.kind = DemoInteractable.Kind.Lamp;
        var trigger = root.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .38f; trigger.center = Vector3.up * .22f;
        var visual = new GameObject("Golden player lantern").transform; visual.SetParent(root.transform, false);
        var gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneStyle/Desk aged brass.mat");
        string path = "Assets/Art/SceneStyle/Player lamp warm glass.mat";
        var glass = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (glass == null) { glass = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(glass, path); }
        glass.SetColor("_BaseColor", new Color(1, .77f, .28f)); glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(1, .62f, .18f) * .65f); EditorUtility.SetDirty(glass);
        Part(visual, "Lantern body", new Vector3(0, .21f, 0), new Vector3(.34f, .32f, .26f), gold, PrimitiveType.Sphere);
        Part(visual, "Lantern foot", new Vector3(0, .03f, 0), new Vector3(.23f, .03f, .21f), gold, PrimitiveType.Cylinder);
        Part(visual, "Lantern cap", new Vector3(0, .39f, 0), new Vector3(.24f, .025f, .20f), gold, PrimitiveType.Cylinder);
        var window = Part(visual, "Diamond glass", new Vector3(0, .22f, -.122f), new Vector3(.12f, .16f, .025f), glass, PrimitiveType.Cube);
        window.localRotation = Quaternion.Euler(0, 0, 45);
        for (int i = 0; i < 20; i++)
        {
            float angle = i * Mathf.PI * 2 / 20;
            Part(visual, "Lantern handle", new Vector3(-.12f + Mathf.Cos(angle) * .115f, .30f + Mathf.Sin(angle) * .15f, .01f),
                Vector3.one * .045f, gold, PrimitiveType.Sphere);
        }
        var pickup = root.AddComponent<CollectiblePlayerLamp>();
        var serialized = new SerializedObject(pickup); serialized.FindProperty("appearance").objectReferenceValue = visual.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo(); root.AddComponent<QuestHint>().SetInteractionTarget(interaction);
    }
    private static Transform Part(Transform root, string name, Vector3 position, Vector3 scale, Material material, PrimitiveType shape)
    {
        var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(root, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>());
        return part.transform;
    }
}
