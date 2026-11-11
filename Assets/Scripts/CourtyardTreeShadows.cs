using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Static, terrain-conforming paper-tree shadows for the western lookout groves.
// Kept separate from moving characters and their existing contact shadows.
public sealed class CourtyardTreeShadows : MonoBehaviour
{
    struct Triangle { public Vector3 a, b, c; }
    sealed class Tree { public SpriteRenderer source; public MeshRenderer[] shadows; }
    readonly List<Tree> trees = new List<Tree>();
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Material> materials = new List<Material>();
    readonly Dictionary<Vector2Int, List<Triangle>> ground = new Dictionary<Vector2Int, List<Triangle>>();
    MeadowVegetation meadow;
    static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 8), Mathf.FloorToInt(p.z / 8));

    internal static void Install()
    {
        if (GameObject.Find("Western grove morning shadows") != null) return;
        var groves = GameObject.Find("Western field margins");
        var horizon = GameObject.Find("Castle horizon and golden fields");
        var shader = Resources.Load<Shader>("PaperGroundShadow");
        var casterShader = Shader.Find("Universal Render Pipeline/Lit");
        if (groves == null || horizon == null || shader == null || casterShader == null || RenderSettings.sun == null) return;
        var style = new GameObject("Western grove morning shadows").AddComponent<CourtyardTreeShadows>();
        style.meadow = horizon.GetComponentInChildren<MeadowVegetation>();
        style.IndexGround(horizon);
        var contact = new Material(shader) { name = "Western tree root contact" }; style.materials.Add(contact);
        contact.SetColor("_ShadowColor", new Color(.12f, .15f, .18f, .30f));
        var painted = new Dictionary<Sprite, Material>();
        var light = -RenderSettings.sun.transform.forward;
        var away = new Vector3(-light.x, 0, -light.z).normalized;
        var across = Vector3.Cross(Vector3.up, away);
        foreach (var tree in groves.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "Western field tree"))
        {
            if (tree.sprite == null) continue;
            if (!painted.TryGetValue(tree.sprite, out var material))
            {
                material = CreateCasterMaterial(tree.sprite.texture);
                style.materials.Add(material);
                painted.Add(tree.sprite, material);
            }
            float width = tree.sprite.bounds.size.x * Mathf.Abs(tree.transform.lossyScale.x);
            var anchor = tree.transform.position;
            var canopy = CreateCaster(style.transform,tree,across,material,out var mesh);
            style.meshes.Add(mesh);
            var root = style.Patch("Tree root contact shadow", anchor, Vector3.right, Vector3.forward, width * .31f, width * .23f, contact);
            style.trees.Add(new Tree { source = tree, shadows = new[] { canopy, root } });
        }
        // Sampling is needed only during construction, not on every frame.
        style.ground.Clear();
    }
    void IndexGround(GameObject horizon)
    {
        foreach (var filter in horizon.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader.name != "Arkmon/Meadow" || filter.sharedMesh == null) continue;
            var vertices = filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
            var indices = filter.sharedMesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var t = new Triangle { a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]] };
                var min = Cell(Vector3.Min(t.a, Vector3.Min(t.b, t.c))); var max = Cell(Vector3.Max(t.a, Vector3.Max(t.b, t.c)));
                for (int x = min.x; x <= max.x; x++) for (int z = min.y; z <= max.y; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (!ground.TryGetValue(cell, out var list)) { list = new List<Triangle>(); ground.Add(cell, list); }
                    list.Add(t);
                }
            }
        }
    }
    float Height(Vector3 p)
    {
        if (ground.TryGetValue(Cell(p), out var list)) foreach (var t in list)
        {
            var a = t.b - t.a; var b = t.c - t.a; var d = p - t.a;
            float determinant = a.x * b.z - a.z * b.x;
            if (Mathf.Abs(determinant) < .00001f) continue;
            float u = (d.x * b.z - d.z * b.x) / determinant;
            float v = (a.x * d.z - a.z * d.x) / determinant;
            if (u >= -.0001f && v >= -.0001f && u + v <= 1.0001f) return t.a.y + u * a.y + v * b.y;
        }
        return meadow != null ? meadow.SurfacePoint(p).y : p.y;
    }
    internal static Material CreateCasterMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        var material = new Material(shader) { name = "Tree alpha shadow caster" };
        material.SetTexture("_BaseMap",texture);
        material.SetFloat("_AlphaClip",1);
        material.SetFloat("_Cutoff",.35f);
        material.SetFloat("_Cull",0);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType","TransparentCutout");
        material.renderQueue = 2450;
        return material;
    }

    internal static MeshRenderer CreateCaster(Transform parent, SpriteRenderer tree, Vector3 across, Material material, out Mesh mesh)
    {
        var source = tree.sprite.vertices; var vertices = new Vector3[source.Length];
        var scale = tree.transform.lossyScale;
        for (int i = 0; i < source.Length; i++)
            vertices[i] = tree.transform.position + across * (source[i].x * Mathf.Abs(scale.x) * (tree.flipX ? -1 : 1))
                + Vector3.up * (source[i].y * Mathf.Abs(scale.y));
        mesh = new Mesh { name = "Tree illustration shadow caster" };
        mesh.vertices = vertices; mesh.uv = tree.sprite.uv; mesh.triangles = tree.sprite.triangles.Select(i=>(int)i).ToArray();
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly; renderer.receiveShadows = false;
        return renderer;
    }
    MeshRenderer Patch(string name, Vector3 anchor, Vector3 across, Vector3 along, float width, float length, Material material)
    {
        const int columns = 9, rows = 9;
        var vertices = new Vector3[columns * rows]; var uv = new Vector2[vertices.Length]; var indices = new List<int>();
        for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
        {
            int i = row * columns + column; float u = column / (float)(columns - 1), v = row / (float)(rows - 1);
            var p = anchor + across * ((u - .5f) * width) + along * ((v - .5f) * length);
            p.y = Height(p) + .065f; vertices[i] = p; uv[i] = new Vector2(u, v);
            if (row == 0 || column == 0) continue;
            indices.AddRange(new[] { i - columns - 1, i - 1, i - columns, i - columns, i - 1, i });
        }
        var mesh = new Mesh { name = name }; meshes.Add(mesh);
        mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false; return renderer;
    }
    void LateUpdate()
    {
        foreach (var tree in trees)
            foreach (var renderer in tree.shadows) renderer.enabled = tree.source != null && tree.source.enabled && tree.source.gameObject.activeInHierarchy;
    }
    void OnDisable() { foreach (var tree in trees) foreach (var renderer in tree.shadows) if (renderer != null) renderer.enabled = false; }
    void OnDestroy()
    {
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
