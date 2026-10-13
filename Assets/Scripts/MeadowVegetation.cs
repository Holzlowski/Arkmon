using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Decorative geometry only. Generated meshes are rebuilt, not stored as hundreds of large assets.
[ExecuteAlways]
public class MeadowVegetation : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField, Min(0.5f)] private float nearSpacing = .85f;
    [SerializeField, Min(1f)] private float farSpacing = 2.8f;
    [SerializeField] private int seed = 721;
    [SerializeField] private Rect clearWorldArea;
    public void SetClearArea(Rect area) { clearWorldArea = area; rebuild = true; }
    private GameObject generated;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private bool rebuild;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<Vector2> uv = new List<Vector2>();
    private readonly List<int> triangles = new List<int>();

    private void OnEnable() => rebuild = true;
    private void OnValidate() => rebuild = true;
    private void Update()
    {
        if (!rebuild) return;
        rebuild = false;
        Rebuild();
    }
    private void Rebuild()
    {
        Clear();
        if (material == null) return;
        generated = new GameObject("Generated meadow vegetation") { hideFlags = HideFlags.DontSave };
        generated.transform.SetParent(transform, false);
        for (int z = 0; z < 10; z++)
            for (int x = 0; x < 10; x++)
            {
                var chunk = new GameObject("Meadow chunk", typeof(LODGroup)) { hideFlags = HideFlags.DontSave };
                chunk.transform.SetParent(generated.transform, false);
                var lods = new LOD[2];
                for (int lod = 0; lod < 2; lod++)
                {
                    float distanceFromArena = new Vector2(-180 + x * 40, -180 + z * 40).magnitude;
                    float densityFactor = Mathf.Lerp(1f, 1.5f, Mathf.InverseLerp(80, 160, distanceFromArena));
                    Mesh mesh = Build(18 - 200 + x * 40, 12 - 200 + z * 40,
                        lod == 0 ? nearSpacing * densityFactor : farSpacing, seed + x + z * 10);
                    mesh.hideFlags = HideFlags.DontSave;
                    meshes.Add(mesh);
                    var part = new GameObject("Vegetation LOD" + lod, typeof(MeshFilter), typeof(MeshRenderer))
                        { hideFlags = HideFlags.DontSave };
                    part.transform.SetParent(chunk.transform, false);
                    part.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = part.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    lods[lod] = new LOD(lod == 0 ? .45f : .008f, new Renderer[] { renderer });
                }
                var group = chunk.GetComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();
            }
    }
    private void OnDisable() => Clear();
    private void Clear()
    {
        if (generated != null)
        {
            generated.SetActive(false);
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
        }
        generated = null;
        foreach (Mesh mesh in meshes)
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        meshes.Clear();
    }

    private static float Height(float x, float z)
    {
        float distance = Mathf.Max(Mathf.Abs(x - 18) - 17, Mathf.Abs(z - 12) - 17);
        float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / 30));
        return -.28f + blend * (1.5f + Mathf.PerlinNoise((x + 220) * .016f, (z + 180) * .016f) * 11);
    }

    private static float Gold(float x, float z)
    {
        float distance = Mathf.Max(Mathf.Abs(x - 18) - 17, Mathf.Abs(z - 12) - 17);
        float field = Mathf.PerlinNoise((x + 400) * .009f, (z + 200) * .009f);
        return Mathf.SmoothStep(0, 1, Mathf.Clamp01((field - .43f) * 12))
            * Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / 28));
    }

    private Mesh Build(float x0, float z0, float step, int patchSeed)
    {
        var random = new System.Random(patchSeed);
        vertices.Clear(); colors.Clear(); uv.Clear(); triangles.Clear();
        for (float z = z0; z < z0 + 40; z += step)
            for (float x = x0; x < x0 + 40; x += step)
            {
                float wx = x + Sample(random) * step, wz = z + Sample(random) * step;
                Vector3 world = transform.TransformPoint(new Vector3(wx, 0, wz));
                if (clearWorldArea.width > 0 && clearWorldArea.Contains(new Vector2(world.x, world.z))) continue;
                // The entire board, its walls and a small margin remain clear.
                if (wx > 4.8f && wx < 32.4f && wz > -2 && wz < 25) continue;
                float gold = Gold(wx, wz), y = Height(wx, wz) + .01f;
                Color tip = Color.Lerp(new Color(.44f, .65f, .24f), new Color(.86f, .69f, .31f), gold).linear;
                Color root = Color.Lerp(new Color(.21f, .36f, .1f), new Color(.53f, .4f, .16f), gold).linear;
                float height = Mathf.Lerp(.4f, 1.15f, gold) * (.75f + Sample(random) * .5f);
                for (int blade = 0; blade < 3; blade++)
                {
                    float angle = Sample(random) * Mathf.PI * 2;
                    Vector3 side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 basePoint = new Vector3(wx + Sample(random) * .25f, y, wz + Sample(random) * .25f);
                    float width = Mathf.Lerp(.065f, .035f, gold);
                    int i = vertices.Count;
                    Vertex(basePoint - side * width, root, 0);
                    Vertex(basePoint + side * width, root, 0);
                    Vertex(basePoint + Vector3.up * height * .65f + side * width * .6f, tip, .65f);
                    Vertex(basePoint + Vector3.up * height * .65f - side * width * .4f, tip, .65f);
                    Vertex(basePoint + Vector3.up * height + side * .16f, tip, 1);
                    Triangle(i, i + 2, i + 1); Triangle(i, i + 3, i + 2); Triangle(i + 3, i + 4, i + 2);
                    if (gold <= .6f) continue;
                    for (int grain = 0; grain < 3; grain++)
                    {
                        Vector3 center = basePoint + Vector3.up * (height * .75f + grain * .075f) + side * .12f;
                        int j = vertices.Count;
                        Vertex(center - Vector3.up * .065f, tip, 1);
                        Vertex(center - side * .045f, tip, 1);
                        Vertex(center + Vector3.up * .065f, tip, 1);
                        Vertex(center + side * .045f, tip, 1);
                        Triangle(j, j + 1, j + 2); Triangle(j, j + 2, j + 3);
                    }
                }
            }
        var mesh = new Mesh { name = "Meadow vegetation", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        Bounds bounds = mesh.bounds; bounds.Expand(.6f); mesh.bounds = bounds;
        mesh.UploadMeshData(true); // Release the CPU copy after uploading; wind runs on the GPU.
        return mesh;
    }

    private static float Sample(System.Random random) => (float)random.NextDouble();
    private void Vertex(Vector3 position, Color color, float windWeight)
    {
        vertices.Add(position); colors.Add(color); uv.Add(new Vector2(0, windWeight));
    }
    private void Triangle(int a, int b, int c)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
    }
}
