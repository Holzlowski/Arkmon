using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixTowerDoorAndWindow
{
    private const string Art = "Assets/Art/Intro/";

    [MenuItem("Arkmon/Fix tower door and window frame")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        Apply();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void Apply()
    {
        var door = GameObject.Find("Courtyard door").transform;
        var art = door.GetComponentInChildren<SpriteRenderer>(true);
        bool modelled = door.GetComponent<TowerRoomDoor>() != null;
        if(!modelled)DoorSurface(art);
        else
        {
            art.gameObject.SetActive(false);
            foreach(string name in new[]{"Courtyard door stone recess","Tower curved door backing"})
            {var old=GameObject.Find(name);if(old!=null)old.GetComponent<Renderer>().enabled=false;}
        }
        var blocker = door.Find("Door approach blocker");
        if (blocker == null)
        {
            blocker = new GameObject("Door approach blocker", typeof(BoxCollider)).transform;
            blocker.SetParent(door, false);
        }
        // Stop the player before the wall art, allowing for the billboard's full width.
        blocker.localPosition = new Vector3(modelled ? -.9f : -.65f, 1.9f, 0);
        blocker.localRotation = Quaternion.identity;
        blocker.localScale = Vector3.one;
        blocker.GetComponent<BoxCollider>().size = new Vector3(.4f, 3.8f, 3.0f);

        var mullions = GameObject.Find("Window slender mullions").transform;
        var bars = mullions.GetComponentsInChildren<MeshRenderer>();
        foreach (var bar in bars)
        {
            bool vertical = bar.transform.localScale.y > bar.transform.localScale.x;
            bar.transform.position = new Vector3(0, vertical ? 3.275f : 3.15f, 5.91f);
            bar.transform.rotation = Quaternion.identity;
            bar.transform.localScale = vertical ? new Vector3(.075f, 2.6f, .10f) : new Vector3(2.78f, .075f, .10f);
        }
    }

    private static void DoorSurface(SpriteRenderer illustration)
    {
        Sprite sprite = illustration.sprite;
        Bounds b = sprite.bounds;
        Rect r = sprite.textureRect;
        float w = sprite.texture.width, h = sprite.texture.height;
        var mesh = new Mesh { name = "Tower door illustrated surface" };
        mesh.vertices = new[] { new Vector3(b.min.x, b.min.y, 0), new Vector3(b.max.x, b.min.y, 0), new Vector3(b.max.x, b.max.y, 0), new Vector3(b.min.x, b.max.y, 0) };
        mesh.uv = new[] { new Vector2(r.xMin/w, r.yMin/h), new Vector2(r.xMax/w, r.yMin/h), new Vector2(r.xMax/w, r.yMax/h), new Vector2(r.xMin/w, r.yMax/h) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        mesh.RecalculateBounds();
        string meshPath = Art + mesh.name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        var surface = illustration.transform.Find("Lit door surface");
        if (surface == null)
        {
            surface = new GameObject("Lit door surface", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            surface.SetParent(illustration.transform, false);
        }
        var filter = surface.GetComponent<MeshFilter>();
        filter.sharedMesh = saved;
        var renderer = surface.GetComponent<MeshRenderer>();
        string materialPath = Art + "Tower door painted lit.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, materialPath); }
        mat.SetTexture("_BaseMap", sprite.texture);
        mat.SetColor("_BaseColor", new Color(.72f, .70f, .66f));
        // Keep painted detail readable in the deep doorway shadow; direct light still adds to it.
        mat.SetTexture("_EmissionMap", sprite.texture);
        mat.SetColor("_EmissionColor", new Color(.17f, .16f, .14f));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        mat.SetFloat("_AlphaClip", 1); mat.SetFloat("_Cutoff", .5f); mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cull", 0); mat.SetFloat("_Smoothness", .02f);
        mat.SetOverrideTag("RenderType", "TransparentCutout"); mat.renderQueue = 2450;
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        illustration.enabled = false;
        EditorUtility.SetDirty(mat);
    }
}
