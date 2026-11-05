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
    internal Material FieldMaterial => material;
    public void SetClearArea(Rect area) { clearWorldArea = area; rebuild = true; }
    private GameObject generated;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private bool rebuild;
    private float tuftPhase;
    private MeadowRootContacts rootContacts;
    private readonly List<MeshRenderer> nearRenderers = new List<MeshRenderer>();
    private readonly List<MeshRenderer> shadowCandidates = new List<MeshRenderer>();
    private MaterialPropertyBlock shadowProperties;
    private bool nearShadowsEnabled = true;
    private Vector3 shadowEye = Vector3.positiveInfinity;
    public bool NearShadowsEnabled { get => nearShadowsEnabled; set { nearShadowsEnabled = value; RefreshNearShadows(true); } }
    private bool Aurenfeld => Application.isPlaying && gameObject.scene.name == "StartScene";

    // Chunk selection avoids submitting the whole landscape to the shadow map.
    // The shader then fades individual tufts out between 26 and 34 metres.
    private void LateUpdate() => RefreshNearShadows();
    private void RefreshNearShadows(bool force = false)
    {
        if (!Aurenfeld || nearRenderers.Count == 0) return;
        var camera = Camera.main;
        if (camera != null && !force && (camera.transform.position - shadowEye).sqrMagnitude < .0025f) return;
        shadowEye = camera != null ? camera.transform.position : Vector3.positiveInfinity;
        shadowCandidates.Clear();
        foreach (var renderer in nearRenderers)
        {
            if (renderer.shadowCastingMode != ShadowCastingMode.Off) renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (nearShadowsEnabled && camera != null && renderer.bounds.SqrDistance(camera.transform.position) < 36 * 36)
                shadowCandidates.Add(renderer);
        }
        if (camera == null) return;
        var eye = camera.transform.position;
        shadowCandidates.Sort((a,b) => a.bounds.SqrDistance(eye).CompareTo(b.bounds.SqrDistance(eye)));
        if (shadowProperties == null) shadowProperties = new MaterialPropertyBlock();
        for (int i = 0; i < Mathf.Min(4, shadowCandidates.Count); i++)
        {
            var renderer = shadowCandidates[i];
            renderer.GetPropertyBlock(shadowProperties);
            shadowProperties.SetVector("_GrassShadowCamera", new Vector4(eye.x, eye.y, eye.z, 1));
            renderer.SetPropertyBlock(shadowProperties); renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
    }
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
        if (Aurenfeld) rootContacts = new MeadowRootContacts(transform);
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
                        lod == 0 ? nearSpacing * densityFactor : farSpacing, seed + x + z * 10, lod == 0);
                    mesh.hideFlags = HideFlags.DontSave;
                    meshes.Add(mesh);
                    var part = new GameObject("Vegetation LOD" + lod, typeof(MeshFilter), typeof(MeshRenderer))
                        { hideFlags = HideFlags.DontSave };
                    part.transform.SetParent(chunk.transform, false);
                    part.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = part.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = Aurenfeld;
                    if (Aurenfeld && lod == 0) nearRenderers.Add(renderer);
                    var contact = lod == 0 ? rootContacts?.Build(chunk.transform) : null;
                    lods[lod] = new LOD(lod == 0 ? .45f : .008f,
                        contact != null ? new Renderer[] { renderer, contact } : new Renderer[] { renderer });
                }
                var group = chunk.GetComponent<LODGroup>();
                if (Aurenfeld)
                {
                    group.fadeMode = LODFadeMode.CrossFade;
                    group.animateCrossFading = false;
                    lods[0].fadeTransitionWidth = .22f;
                    lods[1].fadeTransitionWidth = .18f;
                }
                group.SetLODs(lods);
                group.RecalculateBounds();
            }
        rootContacts?.FinishSampling();
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
        rootContacts?.Dispose(); rootContacts = null;
        nearRenderers.Clear(); shadowCandidates.Clear(); shadowEye = Vector3.positiveInfinity;
        foreach (Mesh mesh in meshes)
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        meshes.Clear();
    }

    internal Vector3 OriginalSurfacePoint(Vector3 world)
    {
        var local=transform.InverseTransformPoint(world);local.y=Height(local.x,local.z);
        return transform.TransformPoint(local);
    }
    public Vector3 SurfacePoint(Vector3 world)
    {
        var point=OriginalSurfacePoint(world);
        return Application.isPlaying && gameObject.scene.name=="StartScene" ? CourtyardBreachStyle.ExteriorPoint(point) : point;
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

    private Mesh Build(float x0, float z0, float step, int patchSeed, bool detailed)
    {
        var random = new System.Random(patchSeed);
        bool illustrated = Aurenfeld;
        vertices.Clear(); colors.Clear(); uv.Clear(); triangles.Clear();
        rootContacts?.Begin();
        for (float z = z0; z < z0 + 40; z += step)
            for (float x = x0; x < x0 + 40; x += step)
            {
                float wx = x + Sample(random) * step, wz = z + Sample(random) * step;
                Vector3 world = transform.TransformPoint(new Vector3(wx, 0, wz));
                if (clearWorldArea.width > 0 && clearWorldArea.Contains(new Vector2(world.x, world.z))) continue;
                // The entire board, its walls and a small margin remain clear.
                if (wx > 4.8f && wx < 32.4f && wz > -2 && wz < 25) continue;
                float gold = Gold(wx, wz), y = Height(wx, wz) + .01f;
                if (illustrated)
                {
                    if (CourtyardFieldMargins.IsBarePath(world) || CourtyardEastFieldStyle.IsBarePath(world) || CourtyardFarmApproach.IsBareGround(world)) continue;
                    gold = CourtyardFieldMargins.AdjustGold(world, gold);
                }
                // Shared world-space masks form islands and worn gaps across chunk/LOD boundaries.
                float island = Mathf.PerlinNoise((world.x + 431) * .09f, (world.z + 173) * .09f);
                float detail = Mathf.PerlinNoise((world.x + 79) * .38f, (world.z + 217) * .38f);
                float fullness = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.20f, .65f, island));
                bool westernGrain = illustrated && world.x < -54;
                bool stubble = westernGrain && ((fullness < .14f && detail > .55f) || Sample(random) > Mathf.Lerp(.94f, .995f, fullness));
                if (illustrated && !westernGrain && (island < .20f || Sample(random) > Mathf.Lerp(.50f, .98f, fullness))) continue;
                Color tip = Color.Lerp(new Color(.44f, .65f, .24f), new Color(.86f, .69f, .31f), gold).linear;
                Color root = Color.Lerp(new Color(.21f, .36f, .1f), new Color(.53f, .4f, .16f), gold).linear;
                float height = Mathf.Lerp(.4f, 1.15f, gold) * (.75f + Sample(random) * .5f);
                if (illustrated)
                {
                    tip = Color.Lerp(new Color(.57f,.70f,.28f), new Color(.96f,.78f,.36f), gold).linear;
                    root = Color.Lerp(new Color(.13f,.27f,.075f), new Color(.36f,.28f,.09f), gold).linear;
                    float tint = Mathf.Lerp(.88f, 1.06f, detail); tip *= tint; root *= tint;
                    height *= Mathf.Lerp(.80f, 1.25f, fullness) * Mathf.Lerp(.85f, 1.12f, detail);
                }
                if (stubble) height = .13f + detail * .19f;
                int bladeCount = illustrated ? (detailed ? 3 + Mathf.RoundToInt(fullness * 2) : 3) : 3;
                if (westernGrain) bladeCount = stubble ? 2 : (detailed ? 5 + Mathf.RoundToInt(fullness * 2) : 3);
                if (illustrated && detailed)
                    rootContacts?.Add(new Vector3(wx + .125f, y, wz + .125f), .28f + bladeCount * .025f);
                tuftPhase = illustrated ? Mathf.Repeat(world.x * .731f + world.z * .317f, 1) : 0;
                float clusterHeight = height;
                for (int blade = 0; blade < bladeCount; blade++)
                {
                    height = westernGrain ? clusterHeight * (.78f + Sample(random) * .42f) : clusterHeight;
                    float angle = Sample(random) * Mathf.PI * 2;
                    Vector3 side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 basePoint = new Vector3(wx + Sample(random) * (westernGrain ? .55f : .25f), y, wz + Sample(random) * (westernGrain ? .55f : .25f));
                    if(Application.isPlaying && gameObject.scene.name=="StartScene")
                        basePoint=transform.InverseTransformPoint(CourtyardBreachStyle.ExteriorPoint(transform.TransformPoint(basePoint)));
                    if (illustrated) basePoint = rootContacts.Root(basePoint);
                    float width = Mathf.Lerp(illustrated ? .082f : .065f, illustrated ? .046f : .035f, gold);
                    int i = vertices.Count;
                    Vertex(basePoint - side * width, root, 0);
                    Vertex(basePoint + side * width, root, 0);
                    Color middle = illustrated ? Color.Lerp(root, tip, .62f) : tip;
                    Vertex(basePoint + Vector3.up * height * .65f + side * width * .6f, middle, .65f);
                    Vertex(basePoint + Vector3.up * height * .65f - side * width * .4f, middle, .65f);
                    Vertex(basePoint + Vector3.up * height + side * .16f, tip, 1);
                    Triangle(i, i + 2, i + 1); Triangle(i, i + 3, i + 2); Triangle(i + 3, i + 4, i + 2);
                    if (gold <= .6f || stubble) continue;
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
        Bounds bounds = mesh.bounds; bounds.Expand(illustrated ? 1.8f : .6f); mesh.bounds = bounds;
        mesh.UploadMeshData(true); // Release the CPU copy after uploading; wind runs on the GPU.
        return mesh;
    }

    private static float Sample(System.Random random) => (float)random.NextDouble();
    private void Vertex(Vector3 position, Color color, float windWeight)
    {
        vertices.Add(position); colors.Add(color); uv.Add(new Vector2(tuftPhase, windWeight));
    }
    private void Triangle(int a, int b, int c)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
    }
}
