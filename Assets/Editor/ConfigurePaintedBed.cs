using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurePaintedBed
{
    private const string Art = "Assets/Art/SceneStyle/";
    [MenuItem("Arkmon/Build painted canopy bed")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        Apply(); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    private static Material Mat(string name, Color color, bool cloth = false)
    {
        string path = Art + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .04f);
        if (cloth) { mat.SetFloat("_Cull", 0); mat.doubleSidedGI = true; }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + (cloth ? name.Contains("green") ? "Bed-Velvet-Painted.png" : "Bed-Linen-Painted.png" : "Tower-Oak-Painted.png")));
        mat.SetTextureScale("_BaseMap", cloth ? new Vector2(2, 2) : new Vector2(.6f, 1.5f));
        EditorUtility.SetDirty(mat); return mat;
    }
    private static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name; part.transform.SetParent(parent, false);
        part.transform.localPosition = pos; part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>()); return part.transform;
    }
    private static void Cloth(Transform parent, string name, float width, float height, bool horizontal, Material mat, Vector3 position)
    {
        const int columns = 32, rows = 20;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for (int row = 0; row <= rows; row++) for (int col = 0; col <= columns; col++)
        {
            float u = col / (float)columns, v = row / (float)rows, x = (u - .5f) * width;
            float fold = horizontal
                ? Mathf.Sin(u * Mathf.PI * 5 + v * 2) * .032f + Mathf.Sin(v * Mathf.PI * 4 + u * 3) * .012f
                : Mathf.Sin(u * Mathf.PI * 8) * (.025f + v * .035f);
            vertices.Add(horizontal ? new Vector3(x, fold, v * height) : new Vector3(x, -v * height, fold));
            uv.Add(new Vector2(u, v));
            if (row == rows || col == columns) continue;
            int a = row * (columns + 1) + col, b = a + columns + 1;
            triangles.AddRange(new[]{a,b,b+1,a,b+1,a+1});
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + name + ".asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(parent, false); part.transform.localPosition = position;
        part.GetComponent<MeshFilter>().sharedMesh = saved; part.GetComponent<Renderer>().sharedMaterial = mat;
    }
    public static void Apply()
    {
        var bed = GameObject.Find("Canopy bed"); if (bed == null) return;
        var billboard = bed.GetComponent<PaperPropVisual>(); if (billboard != null) Object.DestroyImmediate(billboard);
        Material spriteMaterial = GameObject.FindWithTag("Player").GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;
        foreach(var oldSprite in bed.GetComponentsInChildren<SpriteRenderer>(true))
        { if(oldSprite.gameObject==bed)Object.DestroyImmediate(oldSprite);else Object.DestroyImmediate(oldSprite.gameObject); }
        Transform previous = bed.transform.Find("Painted bed geometry"); if (previous != null) Object.DestroyImmediate(previous.gameObject);
        var model = new GameObject("Painted bed geometry").transform; model.SetParent(bed.transform, false);
        Material wood = Mat("Bed painted oak", new Color(.64f,.46f,.29f));
        Material cream = Mat("Bed painted linen", Color.white, true);
        Material green = Mat("Bed painted green velvet", Color.white, true);
        Material gold = Mat("Bed embroidered gold", new Color(.83f,.62f,.27f), true);
        Box(model,"Oak bed frame",new Vector3(0,.52f,0),new Vector3(2.25f,.3f,3.3f),wood);
        Box(model,"Linen mattress",new Vector3(0,.79f,0),new Vector3(2.12f,.28f,3.1f),cream);
        Box(model,"Carved headboard",new Vector3(0,1.26f,1.55f),new Vector3(2.28f,1.32f,.15f),wood);
        Box(model,"Low footboard",new Vector3(0,.85f,-1.6f),new Vector3(2.28f,.65f,.14f),wood);
        foreach(float x in new[]{-1.14f,1.14f}) foreach(float z in new[]{-1.65f,1.65f})
        {
            Box(model,"Canopy oak post",new Vector3(x,1.65f,z),new Vector3(.14f,3.3f,.14f),wood);
            Box(model,"Gold post collar",new Vector3(x,3.2f,z),new Vector3(.19f,.09f,.19f),gold);
        }
        foreach(float x in new[]{-1.14f,1.14f}) Box(model,"Canopy side rail",new Vector3(x,3.25f,0),new Vector3(.16f,.16f,3.5f),wood);
        foreach(float z in new[]{-1.65f,1.65f}) Box(model,"Canopy end rail",new Vector3(0,3.25f,z),new Vector3(2.45f,.16f,.16f),wood);
        Cloth(model,"Bed canopy cloth",2.4f,3.35f,true,green,new Vector3(0,3.3f,-1.67f));
        Cloth(model,"Bed folded cover",2.12f,2.12f,true,green,new Vector3(0,.97f,-1.47f));
        Cloth(model,"Bed cover foot drape",2.12f,.43f,false,green,new Vector3(0,.97f,-1.48f));
        Box(model,"Cover gold border",new Vector3(0,1.025f,-.92f),new Vector3(2.1f,.015f,.07f),gold);
        foreach(float x in new[]{-.51f,.51f})
        {
            var cushion=GameObject.CreatePrimitive(PrimitiveType.Sphere);cushion.name="Soft linen pillow";cushion.transform.SetParent(model,false);
            cushion.transform.localPosition=new Vector3(x,1.06f,1.07f);cushion.transform.localScale=new Vector3(.95f,.27f,.61f);
            cushion.GetComponent<Renderer>().sharedMaterial=cream;Object.DestroyImmediate(cushion.GetComponent<Collider>());cushion.transform.localRotation=Quaternion.Euler(-8,0,0);
        }
        // Narrow gathered corner curtains leave the mattress and player visible.
        foreach(float x in new[]{-.91f,.91f}) foreach(float z in new[]{-1.59f,1.59f})
        {
            Cloth(model,"Bed corner curtain " + x + " " + z,.42f,2.55f,false,green,new Vector3(x,3.18f,z));
            Box(model,"Curtain gold tie",new Vector3(x,1.75f,z-.06f),new Vector3(.43f,.07f,.06f),gold);
        }
        var collider = bed.GetComponent<BoxCollider>() ?? bed.AddComponent<BoxCollider>(); collider.center = new Vector3(0,.75f,0); collider.size = new Vector3(2.35f,1.5f,3.45f);
        var sleeping = new GameObject("Boy sleeping on mattress",typeof(SpriteRenderer));sleeping.transform.SetParent(bed.transform,false);
        sleeping.transform.localPosition=new Vector3(0,1.20f,.02f);sleeping.transform.localRotation=Quaternion.Euler(80,0,0);
        var illustration=sleeping.GetComponent<SpriteRenderer>();illustration.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Prince-Sleeping.png");illustration.sharedMaterial=spriteMaterial;
        if(illustration.sprite!=null)sleeping.transform.localScale=Vector3.one*(2.5f/illustration.sprite.bounds.size.y);
        var visual = bed.GetComponent<PaintedCanopyBed>() ?? bed.AddComponent<PaintedCanopyBed>();
        var data = new SerializedObject(visual);data.FindProperty("model").objectReferenceValue=model.gameObject;data.FindProperty("sleepingIllustration").objectReferenceValue=illustration;var covers=data.FindProperty("sleepingCover");covers.arraySize=5;
        covers.GetArrayElementAtIndex(0).objectReferenceValue=model.Find("Bed folded cover");
        covers.GetArrayElementAtIndex(1).objectReferenceValue=model.Find("Bed cover foot drape");
        covers.GetArrayElementAtIndex(2).objectReferenceValue=model.Find("Cover gold border");
        int pillowIndex=3;foreach(Transform child in model)if(child.name=="Soft linen pillow")covers.GetArrayElementAtIndex(pillowIndex++).objectReferenceValue=child;
        data.ApplyModifiedPropertiesWithoutUndo();
        if (illustration != null) illustration.enabled = false;
        var player = GameObject.FindWithTag("Player");if(player!=null)player.transform.position=visual.BedsidePosition;
    }
}
