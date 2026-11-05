using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Bounded western scenery. Field masks and visible verges share the same curves.
[DefaultExecutionOrder(9505)]
public sealed class CourtyardFieldMargins : MonoBehaviour
{
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Transform> illustrations = new List<Transform>();
    private readonly List<GameObject> retired = new List<GameObject>();
    private MeadowVegetation meadow;
    private Material green, earth;

    public static float VergeZ(float x)
    {
        float t = Mathf.Clamp01((-x - 164) / 208);
        return 20 + Mathf.Sin((x + 70) * .055f) * 10 - (x + 70) * .10f + Mathf.Sin(t * Mathf.PI * 3) * 15 * t + 90 * t * t;
    }
    public static float PathHalfWidth(float x) => Mathf.Lerp(1.3f, .85f, Mathf.InverseLerp(-150, -365, x))
        + .16f*Mathf.Pow(Mathf.Sin(x*.055f),2) + .4f*Mathf.Exp(-Mathf.Pow((x+123)/8,2));
    private static float SecondZ(float x) => VergeZ(x) - 38 + Mathf.Sin(x * .08f) * 4;
    private static float Weight(Vector3 p) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-372, -350, p.x))
        * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-46, -54, p.x));
    // Ripe fields begin outside either castle boundary; the courtyard stays green.
    public static float GoldenFieldWeight(Vector3 p) => Mathf.Max(
        Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-46, -54, p.x)),
        Mathf.SmoothStep(0, 1, Mathf.InverseLerp(50, 53.5f, p.x)));
    public static float AdjustGold(Vector3 p, float original) => Mathf.Lerp(original, 1, GoldenFieldWeight(p));
    public static Color GoldenGround(Vector3 p)
    {
        float field = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.32f, .68f,
            Mathf.PerlinNoise((p.x + 400) * .009f, (p.z + 200) * .009f)));
        return Color.Lerp(new Color(.63f, .46f, .19f), new Color(.80f, .65f, .32f), field).linear
            * Mathf.Lerp(.94f, 1.05f, Mathf.PerlinNoise(p.x * .045f + 50, p.z * .045f + 50));
    }
    public static bool IsBarePath(Vector3 p) => Weight(p) > .95f && Mathf.Abs(p.z - VergeZ(p.x)) < PathHalfWidth(p.x) * .67f;

    internal static void Install()
    {
        if (GameObject.Find("Western field margins") != null) return;
        var horizon = GameObject.Find("Castle horizon and golden fields");
        var meadow = horizon != null ? horizon.GetComponentInChildren<MeadowVegetation>() : null;
        if (meadow == null) return;
        var root = new GameObject("Western field margins");
        var style = root.AddComponent<CourtyardFieldMargins>(); style.meadow = meadow;
        style.Build(horizon);
    }
    private void Build(GameObject horizon)
    {
        var source = Resources.Load<Material>("CourtyardStyle/Stable earth");
        if (source == null) return;
        green = new Material(source) { name = "Western field golden stubble" }; materials.Add(green);
        green.SetTexture("_BaseMap", Texture2D.whiteTexture); green.SetColor("_BaseColor", new Color(.58f, .43f, .20f, .80f));
        earth = new Material(source) { name = "Western worn field path" }; materials.Add(earth);
        earth.SetColor("_BaseColor", new Color(.85f, .82f, .70f, .96f));
        earth.SetFloat("_WheelRuts",1); earth.SetFloat("_FogStrength", 1f); green.SetFloat("_FogStrength", 1f);
        earth.renderQueue = green.renderQueue + 1; // The path must always draw above its wider stubble verge.
        var tree = horizon.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Meadow tree")?.GetComponentInChildren<SpriteRenderer>();
        foreach (var old in horizon.GetComponentsInChildren<Transform>())
        {
            var p = old.position;
            if (p.x > -155 && p.x < -48 && p.z > -42 && p.z < 86
                && (old.name == "Meadow tree" || old.name == "Meadow wildflowers" || old.name == "Meadow thistles"))
            { retired.Add(old.gameObject); old.gameObject.SetActive(false); }
        }
        Ribbon(false, 4.8f, green, "Western green field seam");
        Ribbon(true, 4.3f, green, "Western green field seam");
        Ribbon(false, 1.3f, earth, "Western winding field path");
        if (tree != null)
        {
            Grove(tree, new Vector3(-77, 0, -16), 7.1f, 13);
            Grove(tree, new Vector3(-91, 0, 48), 8.8f, 29);
            Grove(tree, new Vector3(-137, 0, -21), 7.8f, 47);
            Grove(tree, new Vector3(-166, 0, 66), 8.4f, 61);
        }
        // Sparse low herbs belong on the road shoulders, not scattered through ripe grain.
        var random = new System.Random(305);
        var patches = new[] { -62f, -79f, -97f, -112f, -133f, -148f };
        for (int patch = 0; patch < patches.Length; patch++)
        {
            float side = patch % 2 == 0 ? 1 : -1;
            for (int i = 0; i < 3; i++)
            {
                float x = patches[patch] + ((float)random.NextDouble() - .5f) * 3.6f;
                float z = VergeZ(x) + side * (PathHalfWidth(x) + .8f + (float)random.NextDouble() * .9f);
                var p = new Vector3(x, 0, z);
                if (CourtyardFarmApproach.IsBareGround(p)) continue;
                Plant(i == 0 ? "Mixed" : "Grass", p, .24f + (float)random.NextDouble() * .19f, patch * 3 + i);
            }
        }
    }

    private Vector3 Ground(Vector3 p) => CourtyardDistantFields.Surface(meadow, p);
    private void Grove(SpriteRenderer source, Vector3 centre, float height, int seed)
    {
        var random = new System.Random(seed);
        for (int i = 0; i < 3; i++)
        {
            // Unequal spacing and depth make each group read as a small field-edge copse.
            var offset = i == 0 ? new Vector3(-4.2f, 0, -3.4f)
                : i == 1 ? new Vector3(1.1f, 0, 1.8f) : new Vector3(3.6f, 0, 5.1f);
            var p = Ground(centre + offset);
            var tree = new GameObject("Western field tree", typeof(SpriteRenderer)); tree.transform.SetParent(transform, false);
            var renderer = tree.GetComponent<SpriteRenderer>(); renderer.sprite = source.sprite; renderer.sharedMaterial = source.sharedMaterial;
            float h = height * (.85f + (float)random.NextDouble() * .3f);
            tree.transform.localScale = Vector3.one * (h / source.sprite.bounds.size.y);
            tree.transform.position = p;
            renderer.flipX = i % 2 == 0; renderer.color = new Color(.94f, .98f, .83f);
            illustrations.Add(tree.transform);
            // Small undergrowth clusters ground the trunks without blocking the middle of the panorama.
            for (int j = 0; j < 3; j++)
            {
                float a = j * 2.3f + i * .7f;
                Plant(j == 0 ? "Mixed" : "Grass", p + new Vector3(Mathf.Cos(a) * 1.4f, 0, Mathf.Sin(a) * 1.4f), .32f + j * .07f, j);
            }
        }
    }
    private void Plant(string kind, Vector3 p, float height, int index)
    {
        var sprite = Resources.Load<Sprite>("CourtyardStyle/" + kind); if (sprite == null) return;
        var go = new GameObject("Western field " + kind, typeof(SpriteRenderer)); go.transform.SetParent(transform, false);
        go.transform.position = Ground(p) + Vector3.up * .04f; go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.flipX = index % 2 == 0;
        renderer.color = new Color(.78f, .75f, .49f); illustrations.Add(go.transform);
    }
    private void Ribbon(bool second, float width, Material material, string name)
    {
        const int rows = 365, columns = 13;
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
        for (int row = 0; row < rows; row++)
        {
            float x = Mathf.Lerp(-48, -372, row / (float)(rows - 1));
            float z = second ? SecondZ(x) : VergeZ(x);
            for (int col = 0; col < columns; col++)
            {
                float side = col / (float)(columns - 1) * 2 - 1;
                var p = new Vector3(x, 0, z + side * width * (material == earth ? PathHalfWidth(x) / 1.3f : Mathf.Lerp(1, .24f, Mathf.InverseLerp(-164, -220, x))) * (1 + .08f * Mathf.Sin(x * .33f)));
                vertices.Add(Ground(p) + Vector3.up * (material == earth ? .095f : .075f));
                // The secondary near-field margin must not become a second dark road on distant hills.
                float seamFade = second ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-164, -210, x)) : 0;
                colors.Add(new Color(Mathf.Max(Mathf.Max(Mathf.Abs(side), 1 - Weight(p)), seamFade), (side+1)*.5f, 0, 1));
                if (row == 0 || col == 0) continue;
                int n = vertices.Count - 1, previous = n - columns;
                triangles.AddRange(new[] { previous - 1, n - 1, previous, previous, n - 1, n });
            }
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
    private void LateUpdate()
    {
        var camera = Camera.main; if (camera == null) return;
        foreach (var illustration in illustrations) illustration.rotation = camera.transform.rotation;
    }
    private void OnDestroy()
    {
        foreach (var go in retired) if (go != null) go.SetActive(true);
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
