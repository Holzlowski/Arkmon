using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class RefineIntroRoomDetails
{
    private const string Art = "Assets/Art/Intro/";

    [MenuItem("Arkmon/Refine room heraldry and seams")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        var old = GameObject.Find("Intro room heraldry and seams");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Intro room heraldry and seams").transform;
        var wall = GameObject.Find("Round tower visible walls").GetComponent<Renderer>().sharedMaterial;
        CloseDoorRecess(root, wall);
        var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "Window upper seam stone";
        lintel.transform.SetParent(root, false);
        lintel.transform.position = new Vector3(0, 5.1f, 6);
        lintel.transform.localScale = new Vector3(2.8f, .24f, .72f);
        lintel.GetComponent<Renderer>().sharedMaterial = wall;
        foreach (float x in new[] { -1.38f, 1.38f })
        {
            var jamb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jamb.name = "Window side seam stone";
            jamb.transform.SetParent(root, false);
            jamb.transform.position = new Vector3(x, 3.6f, 5.96f);
            jamb.transform.localScale = new Vector3(.13f, 3.2f, .72f);
            jamb.GetComponent<Renderer>().sharedMaterial = wall;
        }
        // The floor reaches into the wall thickness, including the tangent doorway corners.
        GameObject.Find("Stone floor").transform.localScale = new Vector3(1.06f, 1, 1.06f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(.17f, .16f, .14f);
        var sun = GameObject.Find("Window morning light").GetComponent<Light>();
        sun.intensity = 31;
        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Art/SceneStyle/Tower morning Volume.asset");
        if (profile.TryGet<ColorAdjustments>(out var grade))
        {
            grade.postExposure.Override(-.05f);
            EditorUtility.SetDirty(grade);
        }
        var door = GameObject.Find("Courtyard door");
        var illustration = door.GetComponentInChildren<SpriteRenderer>();
        illustration.color = new Color(.72f, .70f, .66f);
        var crest = GameObject.Find("Aurenfeld family crest");
        var bed = GameObject.Find("Canopy bed").transform;
        AddCrest(root, crest, "Aurenfeld bed crest", bed, new Vector3(0, .84f, -1.70f), new Vector3(.48f, .54f, 1), Quaternion.identity);
        AddCrest(root, crest, "Aurenfeld door crest", illustration.transform, new Vector3(0, 2.57f, .025f), new Vector3(.54f, .61f, 1), Quaternion.identity);
        Rug();
        FixTowerDoorAndWindow.Apply();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ImportElianWalkRig.Run();
    }

    private static void AddCrest(Transform root, GameObject source, string name, Transform parent, Vector3 position, Vector3 scale, Quaternion angle)
    {
        // Use the same shield mesh and painted emblem as the wall, with actual surface depth.
        var old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localRotation = angle;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        obj.GetComponent<Renderer>().sharedMaterial = source.GetComponent<Renderer>().sharedMaterial;
    }

    private static void CloseDoorRecess(Transform root, Material stone)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        // Restore the curved stone behind the interaction door; its old rectangular backing
        // left wedges at both sides of the round wall and along the floor.
        for (int i = 0; i < 8; i++)
        {
            float a = (60 + i * 2.5f) * Mathf.Deg2Rad, b = a + 2.5f * Mathf.Deg2Rad;
            int n = v.Count;
            foreach (var p in new[] { Point(a, 0), Point(a, 3.82f), Point(b, 3.82f), Point(b, 0) })
            { v.Add(p); uv.Add(new Vector2(Mathf.Atan2(p.x, p.z + 1.7f) * 7.82f * .3f, p.y * .3f)); }
            t.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }
        var mesh = SaveMesh("Tower curved door backing", v, uv, t);
        var obj = new GameObject("Tower curved door backing", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        obj.transform.SetParent(root, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
        obj.GetComponent<Renderer>().sharedMaterial = stone;
    }

    private static Vector3 Point(float angle, float y) => new Vector3(Mathf.Sin(angle) * 7.85f, y, Mathf.Cos(angle) * 7.85f - 1.7f);

    private static void Rug()
    {
        const string texturePath = Art + "Aurenfeld-Star-Rug.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var obj = GameObject.Find("Heirloom rug");
        var sprite = obj.GetComponent<SpriteRenderer>();
        if (sprite != null) Object.DestroyImmediate(sprite);
        obj.transform.SetPositionAndRotation(new Vector3(-.2f, .012f, 2.3f), Quaternion.identity);
        obj.transform.localScale = Vector3.one;
        var v = new List<Vector3> { new Vector3(-2.875f, 0, -1.92f), new Vector3(-2.875f, 0, 1.92f), new Vector3(2.875f, 0, 1.92f), new Vector3(2.875f, 0, -1.92f) };
        var mesh = SaveMesh("Aurenfeld flat rug", v, new List<Vector2> { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, new List<int> { 0, 1, 2, 0, 2, 3 });
        var filter = obj.GetComponent<MeshFilter>();
        if (filter == null) filter = obj.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = obj.AddComponent<MeshRenderer>();
        const string materialPath = Art + "Aurenfeld woven rug.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, materialPath); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .5f);
        mat.EnableKeyword("_ALPHATEST_ON"); mat.SetOverrideTag("RenderType", "TransparentCutout"); mat.renderQueue = 2450;
        mat.SetFloat("_Cull", 0); mat.SetFloat("_Smoothness", .02f); renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        EditorUtility.SetDirty(mat);
    }

    private static Mesh SaveMesh(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
    {
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + name + ".asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); return saved;
    }
}
