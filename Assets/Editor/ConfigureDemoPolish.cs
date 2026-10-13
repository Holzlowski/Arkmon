using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Edit only the demo's named presentation objects; retain gameplay and scene systems.
public static class ConfigureDemoPolish
{
    private const string Art = "Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Polish demo rooms")]
    public static void Run()
    {
        Sprite door = ImportSprite("DemoDoor.png", 420);
        Sprite bench = ImportSprite("DemoWorkbench.png", 560);
        Sprite fountain = ImportSprite("DemoFountain-Side.png", 420);
        TightenAtlasSprite(2, new Rect(1010, 500, 526, 524), 2.7f);
        TightenAtlasSprite(5, new Rect(1060, 0, 476, 490), 5.5f);
        Material roomFloor = Floor("Tower soft paving", .24f, .27f, .8f);
        Material courtFloor = Floor("Courtyard soft paving", .34f, .24f, 0);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var parent = GameObject.Find("Tower room interior").transform;
        SetMaterial("Stone floor", roomFloor);
        Move("Canopy bed", new Vector3(-5, 0, 3));
        Move("Wardrobe", new Vector3(2.7f, 0, 3.5f));
        Move("Take green mantle", new Vector3(2.7f, 0, 1.9f));
        Move("Writing desk", new Vector3(-1.1f, 0, 4.7f));
        Move("Look over golden fields", new Vector3(-.5f, 0, 3.6f));
        RightWallDoor(parent, door);
        Move("Door to courtyard", new Vector3(6.4f, 0, 1f));
        PaintedVista(parent);
        // A rounded sill and shallow trim make the window read as castle architecture.
        Trim(parent, "Polished window sill", new Vector3(0, 1.94f, 5.65f), new Vector3(6.1f, .16f, 1.1f));
        Trim(parent, "Window left trim", new Vector3(-3.05f, 3.25f, 5.5f), new Vector3(.22f, 2.7f, .2f));
        Trim(parent, "Window right trim", new Vector3(3.05f, 3.25f, 5.5f), new Vector3(.22f, 2.7f, .2f));
        Trim(parent, "Tower ceiling", new Vector3(0, 5.05f, 0), new Vector3(16, .25f, 12));
        Material beams = AssetDatabase.LoadAssetAtPath<Material>(Art + "Tower warm wood.mat");
        if (beams == null) { beams = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(beams, Art + "Tower warm wood.mat"); }
        beams.SetColor("_BaseColor", new Color(.45f, .3f, .18f)); beams.SetFloat("_Smoothness", .12f); EditorUtility.SetDirty(beams);
        for (int x = -6; x <= 6; x += 4) Patch(parent, "Ceiling beam " + x, new Vector3(x, 4.8f, 0), new Vector3(.22f, .3f, 12), beams);
        ClearTerrain("Tower golden field vista", "Tower meadow cutout", new Vector4(-20, -25, 20, 10));
        Lighting(true); CameraFraming(true);
        ConfigureTowerCeiling.Apply(parent);
        ConfigureDemoRefinements.ApplyWardrobe();
        EditorSceneManager.SaveScene(scene);

        scene = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        parent = GameObject.Find("Demo castle courtyard").transform;
        SetMaterial("Courtyard stone floor", courtFloor);
        CourtyardTower(parent);
        ConfigureDemoRefinements.ApplyGate();
        Replace(parent, "Tower entrance", door, new Vector3(-22, 0, 2.9f), 2.6f, true);
        var entrance = GameObject.Find("Tower entrance");
        Object.DestroyImmediate(entrance.GetComponent<PaperPropVisual>());
        entrance.transform.Find("Paper illustration").localRotation = Quaternion.identity;
        Remove("Workbench legs");
        Replace(parent, "Workbench", bench, new Vector3(-15.5f, 0, 4.3f), 3.2f, true);
        Remove("Fountain water");
        Replace(parent, "Fountain basin", fountain, new Vector3(-1, 0, 4.3f), 3.3f, true);
        Move("Wheelbarrow", new Vector3(-6.3f, 0, 5.4f));
        Move("Bench seat", new Vector3(5, .55f, 3.8f));
        var legs = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Bench leg").ToArray();
        for (int i = 0; i < legs.Length; i++) legs[i].position = new Vector3(4 + i * 2, .25f, 3.8f);
        // Separate garden plots and a marked practice area leave the main east-west route open.
        Material garden = AssetDatabase.LoadAssetAtPath<Material>(Art + "Garden green.mat");
        for (int i = 0; i < 3; i++)
        {
            Patch(parent, "Herb bed " + i, new Vector3(3 + i * 1.8f, .14f, 5.7f), new Vector3(1.45f, .12f, 1.55f), garden);
            Trim(parent, "Herb border " + i, new Vector3(3 + i * 1.8f, .17f, 4.9f), new Vector3(1.55f, .15f, .15f));
        }
        Material practice = Floor("Practice paving", .18f, .22f, 0); practice.SetColor("_BaseColor", new Color(.87f, .91f, .82f)); EditorUtility.SetDirty(practice);
        Patch(parent, "John practice paving", new Vector3(13, .018f, 1.2f), new Vector3(5.5f, .035f, 3.5f), practice);
        ClearTerrain("Castle horizon and golden fields", "Courtyard meadow cutout", new Vector4(-26, -12, 26, 10));
        Lighting(false); CameraFraming(false);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        Debug.Log("Demo visual polish applied without replacing scene systems.");
    }
    private static void CourtyardTower(Transform parent)
    {
        Remove("West tower"); Remove("Player residence tower");
        var root = new GameObject("Player residence tower"); root.transform.SetParent(parent);
        root.transform.position = new Vector3(-22, 0, 6);
        Material stone = AssetDatabase.LoadAssetAtPath<Material>(Art + "Demo painted castle.mat");
        Material trim = AssetDatabase.LoadAssetAtPath<Material>(Art + "Light limestone.mat");
        Material roof = TowerMaterial("Tower slate roof", new Color(.24f, .34f, .39f));
        Material glass = TowerMaterial("Tower window shadow", new Color(.12f, .2f, .21f));
        TowerPart(root.transform, "Octagonal tower", 2.8f, 2.8f, 11, 0, stone);
        TowerPart(root.transform, "Tower foundation", 3f, 3f, .35f, 0, trim);
        TowerPart(root.transform, "Lower stone cornice", 2.95f, 2.95f, .2f, 4.5f, trim);
        TowerPart(root.transform, "Upper stone cornice", 2.95f, 2.95f, .2f, 8, trim);
        TowerPart(root.transform, "Roof eaves", 3.15f, 3.15f, .25f, 10.9f, roof);
        TowerPart(root.transform, "Pointed slate roof", 3.15f, .12f, 3.6f, 11.15f, roof);
        // The façade stays three-dimensional; its stone uses the existing painted shader.
        var collider = root.AddComponent<BoxCollider>();
        collider.center = Vector3.up * 5.5f; collider.size = new Vector3(4.5f, 11, 4.5f);
        foreach (float height in new[] { 5.7f, 9.1f })
        {
            Vector3 center = new Vector3(-22, height, 3.39f);
            Patch(root.transform, "Tower window " + height, center, new Vector3(.72f, 1.45f, .12f), glass);
            Trim(root.transform, "Tower window sill " + height, center + new Vector3(0, -.78f, -.1f), new Vector3(1.05f, .18f, .35f));
            Trim(root.transform, "Tower window lintel " + height, center + Vector3.up * .78f, new Vector3(.95f, .16f, .22f));
            for (int side = -1; side <= 1; side += 2)
                Trim(root.transform, "Tower window jamb " + height + " " + side, center + Vector3.right * side * .43f, new Vector3(.14f, 1.5f, .22f));
        }
    }
    private static Material TowerMaterial(string name, Color color)
    {
        string path = Art + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .05f);
        EditorUtility.SetDirty(material); return material;
    }
    private static void TowerPart(Transform parent, string name, float bottom, float top, float height, float y, Material material)
    {
        // Duplicate vertices per face retain the illustrated octagonal silhouette.
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        for (int side = 0; side < 8; side++)
        {
            float a = (side * 45 + 22.5f) * Mathf.Deg2Rad;
            float b = ((side + 1) * 45 + 22.5f) * Mathf.Deg2Rad;
            Vector3 first = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            Vector3 second = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            int index = vertices.Count;
            vertices.Add(first * bottom); vertices.Add(second * bottom);
            vertices.Add(first * top + Vector3.up * height); vertices.Add(second * top + Vector3.up * height);
            triangles.AddRange(new[] { index, index + 2, index + 1, index + 1, index + 2, index + 3 });
            index = vertices.Count;
            vertices.Add(Vector3.up * height); vertices.Add(first * top + Vector3.up * height); vertices.Add(second * top + Vector3.up * height);
            triangles.AddRange(new[] { index, index + 2, index + 1 });
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.up * y; obj.GetComponent<MeshFilter>().sharedMesh = saved; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    private static Material Floor(string name, float detail, float scale, float warmth)
    {
        string path = Art + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Arkmon/Painted castle")); AssetDatabase.CreateAsset(material, path); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "DemoStone.png"));
        material.SetColor("_BaseColor", new Color(1f, .98f, .94f));
        material.SetFloat("_DetailStrength", detail); material.SetFloat("_TextureScale", scale); material.SetFloat("_WarmLight", warmth);
        EditorUtility.SetDirty(material); return material;
    }
    private static void RightWallDoor(Transform parent, Sprite sprite)
    {
        Replace(parent, "Courtyard door", sprite, new Vector3(7.65f, 0, 1f), 2.7f, true);
        var root = GameObject.Find("Courtyard door");
        // Wall-mounted art stays in the wall plane instead of facing the camera.
        Object.DestroyImmediate(root.GetComponent<PaperPropVisual>());
        root.transform.Find("Paper illustration").localRotation = Quaternion.Euler(0, -90, 0);
        var collider = root.GetComponent<BoxCollider>();
        collider.center = Vector3.up * 1.9f; collider.size = new Vector3(.18f, 3.8f, 2.1f);
    }
    private static void PaintedVista(Transform parent)
    {
        Remove("Painted window scenery"); Remove("Painted window vista");
        var root = new GameObject("Painted window scenery"); root.transform.SetParent(parent);
        var background = VistaLayer(root.transform, "Painted window vista", "PaintedWindowVista.png", new Vector3(0, 3.2f, 6.72f), new Vector2(9, 6), false);
        var fields = VistaLayer(root.transform, "Distant field hills", "WindowFieldHills.png", new Vector3(0, 3.2f, 6.5f), new Vector2(9, 6), true);
        var clouds = VistaLayer(root.transform, "Drifting painted clouds", "WindowClouds.png", new Vector3(0, 4.3f, 6.3f), new Vector2(11, 3), true);
        root.AddComponent<WindowVistaParallax>().Configure(background, fields, clouds);
        // The window now shows the painted vista; nearby procedural hills must not overlap it.
        var landscape = GameObject.Find("Tower golden field vista"); if (landscape != null) landscape.SetActive(false);
    }
    private static Transform VistaLayer(Transform parent, string name, string texture, Vector3 position, Vector2 size, bool transparent)
    {
        string path = Art + texture;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing painted window vista: " + path);
        importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        string materialPath = Art + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, materialPath); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path)); material.SetColor("_BaseColor", Color.white);
        if (transparent)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
        }
        material.SetFloat("_Cull", 0); EditorUtility.SetDirty(material);
        var vista = GameObject.CreatePrimitive(PrimitiveType.Quad); vista.name = name;
        vista.transform.SetParent(parent); vista.transform.position = position;
        vista.transform.localScale = new Vector3(size.x, size.y, 1);
        Object.DestroyImmediate(vista.GetComponent<Collider>());
        var renderer = vista.GetComponent<Renderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return vista.transform;
    }
    private static void TightenAtlasSprite(int index, Rect rect, float width)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Demo prop " + index + ".asset");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "DemoProps.png");
        if (sprite == null || texture == null) return;
        var replacement = Sprite.Create(texture, rect, new Vector2(.5f, 0), rect.width / width);
        EditorUtility.CopySerialized(replacement, sprite); Object.DestroyImmediate(replacement); EditorUtility.SetDirty(sprite);
    }
    private static void SetMaterial(string name, Material material) => GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;
    private static void Move(string name, Vector3 position) { var obj = GameObject.Find(name); if (obj != null) obj.transform.position = position; }
    private static void Remove(string name)
    { foreach (var obj in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == name).ToArray()) Object.DestroyImmediate(obj.gameObject); }
    private static void Replace(Transform parent, string name, Sprite sprite, Vector3 position, float width, bool collision)
    {
        Remove(name);
        var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
        var pivot = new GameObject("Paper illustration", typeof(SpriteRenderer)); pivot.transform.SetParent(root.transform, false);
        pivot.GetComponent<SpriteRenderer>().sprite = sprite;
        float scale = width / sprite.bounds.size.x; pivot.transform.localScale = Vector3.one * scale;
        pivot.transform.localPosition = Vector3.up * -sprite.bounds.min.y * scale;
        var visual = root.AddComponent<PaperPropVisual>(); var data = new SerializedObject(visual);
        data.FindProperty("paperPivot").objectReferenceValue = pivot.transform; data.ApplyModifiedPropertiesWithoutUndo();
        if (collision) { var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * .7f; collider.size = new Vector3(width * .7f, 1.4f, .6f); }
    }
    private static void Trim(Transform parent, string name, Vector3 position, Vector3 size)
        => Patch(parent, name, position, size, AssetDatabase.LoadAssetAtPath<Material>(Art + "Light limestone.mat"));
    private static void Patch(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        Remove(name); var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent);
        obj.transform.position = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    private static void CameraFraming(bool room)
    {
        foreach (var camera in Object.FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None))
        {
            camera.m_Lens.FieldOfView = room ? 52 : 50;
            camera.transform.rotation = Quaternion.Euler(12, 0, 0);
            var framing = camera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing == null) continue;
            framing.m_CameraDistance = room ? 11 : 8.5f;
            framing.m_TrackedObjectOffset = room ? new Vector3(1.6f, .45f, 2.1f) : new Vector3(0, .3f, 2f);
            framing.m_ScreenY = .55f; framing.m_XDamping = .6f; framing.m_YDamping = .8f;
            framing.m_DeadZoneWidth = .1f; framing.m_SoftZoneWidth = .65f;
            camera.PreviousStateIsValid = false; EditorUtility.SetDirty(camera);
        }
    }
    private static void Lighting(bool room)
    {
        RenderSettings.ambientLight = room ? new Color(.86f, .8f, .69f) : new Color(.85f, .9f, 1f);
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { light.intensity = room ? .8f : 1f; light.color = new Color(1, .94f, .83f); }
        string path = Art + (room ? "Tower morning Volume.asset" : "Courtyard morning Volume.asset");
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        var color = Component<ColorAdjustments>(profile); color.contrast.Override(-3); color.saturation.Override(-3);
        color.postExposure.Override(0); color.colorFilter.Override(room ? new Color(1, .98f, .94f) : Color.white);
        var bloom = Component<Bloom>(profile); bloom.intensity.Override(.08f); bloom.threshold.Override(1.2f);
        Component<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
        Component<Vignette>(profile).intensity.Override(room ? .1f : .06f);
        foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) { volume.sharedProfile = profile; EditorUtility.SetDirty(volume); }
        EditorUtility.SetDirty(profile);
    }
    private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var value)) return value;
        value = profile.Add<T>(); AssetDatabase.AddObjectToAsset(value, profile); return value;
    }
    private static void ClearTerrain(string rootName, string materialName, Vector4 area)
    {
        var root = GameObject.Find(rootName); if (root == null) return;
        string path = Art + materialName + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Arkmon/Meadow")); AssetDatabase.CreateAsset(material, path); }
        material.SetVector("_ClearArea", area); EditorUtility.SetDirty(material);
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "Arkmon/Meadow") renderer.sharedMaterial = material;
    }
    private static Sprite ImportSprite(string name, float ppu)
    {
        string path = Art + name;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing polish sprite: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        settings.spritePivot = new Vector2(.5f, 0); importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
