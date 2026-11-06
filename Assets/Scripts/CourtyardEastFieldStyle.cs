using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The existing far bank ends at x=100. Keep the meadow level across that seam,
// then let it rise into the original fields. Shared by terrain, props and grass.
public sealed class CourtyardEastFieldStyle : MonoBehaviour
{
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Material> materials = new List<Material>();
    readonly List<Transform> plants = new List<Transform>();
    readonly List<MeshRenderer> retiredPaths = new List<MeshRenderer>();
    MeadowVegetation meadow;
    MeshRenderer farBank;
    Material originalBank;

    public static Vector3 ExteriorPoint(Vector3 p)
    {
        if (p.x < 50 || p.x > 140 || p.z < -45 || p.z > 92) return p;
        float across = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(50, 57, p.x))
            * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(100, 140, p.x)));
        float along = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-45, -27, p.z))
            * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(73, 92, p.z)));
        p.y = Mathf.Lerp(p.y, -.035f, across * along);
        return p;
    }

    public static float PathZ(float x)
    {
        float t = Mathf.Clamp01((x - 54) / 96);
        return -1 + 22 * t * t + Mathf.Sin(t * Mathf.PI * 2) * 3 * t;
    }
    static float HalfWidth(float x) => Mathf.Lerp(2.25f, 1.15f, Mathf.InverseLerp(51, 130, x));
    public static float EdgeDistance(Vector3 p)
    {
        if (p.x < 50.5f || p.x > 146) return 100;
        float side = Mathf.Sign(p.z - PathZ(p.x));
        float scallop = .15f * Mathf.Sin(p.x * .69f + side) + .12f * Mathf.Sin(p.x * 1.71f);
        float road = Mathf.Abs(p.z - PathZ(p.x)) - HalfWidth(p.x) * (1 + .07f * Mathf.Sin(p.x * .63f));
        return Mathf.Min(road - scallop, CourtyardGateForecourt.EdgeDistance(p));
    }
    public static bool IsBarePath(Vector3 p) => EdgeDistance(p) < .35f;
    public static float CropEdgeGrowth(Vector3 p) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 2.1f, EdgeDistance(p)));


    internal static void Install()
    {
        if (GameObject.Find("Eastern field approach") != null) return;
        var horizon = GameObject.Find("Castle horizon and golden fields");
        var meadow = horizon != null ? horizon.GetComponentInChildren<MeadowVegetation>() : null;
        if (meadow == null) return;
        var style = new GameObject("Eastern field approach").AddComponent<CourtyardEastFieldStyle>();
        style.meadow = meadow;
        style.Build();
    }
    Vector3 Ground(Vector3 p)
    {
        p = meadow.SurfacePoint(p);
        // The solid authored bank covers the lowered meadow until its outer edge.
        if (p.x <= 100 && p.z >= -27 && p.z <= 73) p.y = 0;
        return p;
    }
    void Build()
    {
        var source = Resources.Load<Material>("CourtyardStyle/Stable earth");
        if (source == null) return;
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (renderer.name == "Curved courtyard path" && renderer.bounds.min.x > 49)
            { retiredPaths.Add(renderer); renderer.enabled = false; }
        // This solid bank covers the meadow mesh: tint its own surface as well as the field vertices.
        farBank = GameObject.Find("Far stream bank")?.GetComponent<MeshRenderer>();
        if (farBank != null)
        {
            originalBank = farBank.sharedMaterial;
            var soil = new Material(originalBank) { name = "Eastern golden field soil" };
            soil.SetColor("_BaseColor", new Color(.64f, .46f, .20f));
            materials.Add(soil); farBank.sharedMaterial = soil;
        }
        var earth = new Material(source) { name = "Eastern field earth path" };
        materials.Add(earth); earth.SetFloat("_WheelRuts",.28f); earth.SetColor("_BaseColor", new Color(.68f, .74f, .77f, .92f));
        var verge = new Material(source) { name = "Eastern field dry verge" };
        materials.Add(verge); verge.SetTexture("_BaseMap", Texture2D.whiteTexture);
        verge.SetColor("_BaseColor", new Color(.53f, .46f, .25f, .30f));
        // Transparent ribbons share a footprint; draw earth after the wider grass fringe.
        earth.renderQueue = verge.renderQueue + 1;
        Ribbon("Eastern dry path margin", verge, .85f, .026f);
        Ribbon("Eastern winding field path", earth, 0, .045f);
        var random = new System.Random(619);
        for (int i = 0; i < 70; i++)
        {
            float x = 53 + (float)random.NextDouble() * 59;
            float side = i % 2 == 0 ? 1 : -1;
            float z = PathZ(x) + side * (HalfWidth(x) + 1.1f + (float)random.NextDouble() * 6);
            if (Mathf.PerlinNoise(x * .24f, z * .3f) < .48f || CourtyardGateForecourt.IsBareGround(new Vector3(x,0,z))) continue;
            var sprite = Resources.Load<Sprite>("CourtyardStyle/" + (i % 9 == 0 ? "Pebbles" : i % 4 == 0 ? "Mixed" : "Grass"));
            if (sprite == null) continue;
            var plant = new GameObject("Eastern path planting", typeof(SpriteRenderer));
            plant.transform.SetParent(transform, false);
            plant.transform.position = Ground(new Vector3(x, 0, z)) + Vector3.up * .025f;
            plant.transform.localScale = Vector3.one * ((.23f + (float)random.NextDouble() * .25f) / sprite.bounds.size.y);
            var renderer = plant.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.flipX = i % 2 == 0; renderer.color = new Color(.94f,.76f,.39f);
            plants.Add(plant.transform);
        }
    }
    void Ribbon(string name, Material material, float extraWidth, float offset)
    {
        const int rows = 201, columns = 13;
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
        for (int row = 0; row < rows; row++)
        {
            float x = Mathf.Lerp(50.5f, 150, row / (float)(rows - 1));
            float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(138, 150, x));
            for (int col = 0; col < columns; col++)
            {
                float side = col / (float)(columns - 1) * 2 - 1;
                float width = (HalfWidth(x) + extraWidth) * (1 + .07f * Mathf.Sin(x * .63f));
                vertices.Add(Ground(new Vector3(x, 0, PathZ(x) + side * width)) + Vector3.up * offset);
                colors.Add(new Color(Mathf.Max(Mathf.Abs(side), fade), (side+1)*.5f, 0, 1));
                if (row == 0 || col == 0) continue;
                int n = vertices.Count - 1, previous = n - columns;
                triangles.AddRange(new[] { previous - 1, previous, n - 1, previous, n, n - 1 });
            }
        }
        var mesh = new Mesh { name = name }; meshes.Add(mesh);
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
    void LateUpdate()
    {
        var camera = Camera.main; if (camera == null) return;
        foreach (var plant in plants) plant.rotation = camera.transform.rotation;
    }
    void OnDestroy()
    {
        if (farBank != null) farBank.sharedMaterial = originalBank;
        foreach (var renderer in retiredPaths) if (renderer != null) renderer.enabled = true;
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
