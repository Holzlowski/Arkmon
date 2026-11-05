using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Small, non-blocking transitions where the courtyard lawn meets the enclosing walls.
public sealed class CourtyardWallFootStyle : MonoBehaviour
{
    readonly List<(MeshRenderer renderer, Vector4 moss)> originals = new List<(MeshRenderer, Vector4)>();
    readonly List<Mesh> meshes = new List<Mesh>();
    readonly List<Transform> plants = new List<Transform>();
    IInteractableAsPlayer[] interactions;
    CourtyardGuardRoute route;
    Material ground;

    internal static void Install()
    {
        if (GameObject.Find("Illustrated wall foot planting") != null) return;
        var source = Resources.Load<Material>("CourtyardStyle/Stable earth");
        if (source == null) return;
        var style = new GameObject("Illustrated wall foot planting").AddComponent<CourtyardWallFootStyle>();
        style.ground = new Material(source) { name = "Wall foot soft moss earth" };
        style.ground.SetColor("_BaseColor", new Color(.40f, .48f, .26f, .6f));
        style.interactions = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IInteractableAsPlayer>().ToArray();
        style.route = Resources.Load<CourtyardGuardRoute>("Guard/Patrol");
        Physics.SyncTransforms();
        foreach (string name in new[] { "North castle wall", "East wall north of gate", "East wall south of gate" })
        {
            var renderer = GameObject.Find(name)?.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.sharedMaterial.HasProperty("_FootMoss")) continue;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            style.originals.Add((renderer, block.GetVector("_FootMoss")));
            block.SetVector("_FootMoss", new Vector4(.02f, name == "East wall south of gate" ? .65f : 1.65f, .92f, renderer.bounds.center.x * .13f));
            renderer.SetPropertyBlock(block);
        }
        // Gaps are deliberate: no continuous hedge and no planting through the gate.
        int seed = 0;
        foreach (float z in new[] { -14f, -10.5f, -7f, 5.3f, 8.7f, 13f, 16.3f })
            style.Group(new Vector3(41.25f, 0, z), true, seed++);
        foreach (float x in new[] { -40f, -29f, -21f, -10f, 5f, 20f, 29f, 39f })
            style.Group(new Vector3(x, 0, 17.2f), false, seed++);
    }
    bool Clear(Vector3 p)
    {
        if (p.x > 38 && p.z > -5 && p.z < 3.6f) return false;
        foreach (var target in interactions)
        {
            var d = target.GetTransform() - p; d.y = 0;
            if (d.sqrMagnitude < 2.1f * 2.1f) return false;
        }
        if (route != null) for (int i = 1; i < route.stops.Length; i++)
        {
            var a = route.stops[i - 1].position; var b = route.stops[i].position; a.y = b.y = p.y;
            var d = b - a; float t = d.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector3.Dot(p - a, d) / d.sqrMagnitude);
            if (Vector3.Distance(p, a + d * t) < 1.5f) return false;
        }
        // Houses, cellar walls and furniture are solid; keep the whole small cluster outside them.
        return !Physics.OverlapSphere(p + Vector3.up * .38f, .7f, ~0, QueryTriggerInteraction.Ignore)
            .Any(c => !c.CompareTag("Ground"));
    }
    static bool Ground(ref Vector3 p)
    {
        bool found = false; float y = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(new Vector3(p.x, 20, p.z), Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.CompareTag("Ground")) { y = Mathf.Max(y, hit.point.y); found = true; }
        if (found) p.y = y + .025f;
        return found;
    }
    void Group(Vector3 centre, bool east, int seed)
    {
        if (!Ground(ref centre) || !Clear(centre)) return;
        Patch(centre, east, seed);
        var random = new System.Random(1507 + seed);
        for (int i = 0; i < 7; i++)
        {
            float along = ((float)random.NextDouble() - .5f) * 2.1f;
            float across = ((float)random.NextDouble() - .5f) * .65f;
            var p = centre + (east ? new Vector3(across, 0, along) : new Vector3(along, 0, across));
            if (!Ground(ref p) || !Clear(p)) continue;
            string kind = i == 0 ? "Fern" : i % 2 == 0 ? "Clover" : "Mixed";
            var sprite = Resources.Load<Sprite>("CourtyardStyle/" + kind); if (sprite == null) continue;
            var go = new GameObject("Wall foot " + kind, typeof(SpriteRenderer)); go.transform.SetParent(transform, false);
            go.transform.position = p;
            float height = i == 0 ? .70f : .28f + (float)random.NextDouble() * .23f;
            go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.flipX = i % 2 == 0;
            renderer.color = new Color(.82f, .94f, .71f); plants.Add(go.transform);
        }
    }
    void Patch(Vector3 centre, bool east, int seed)
    {
        const int sides = 24, rings = 6;
        var v = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
        for (int ring = 0; ring < rings; ring++) for (int side = 0; side < sides; side++)
        {
            float r = ring / (float)(rings - 1), angle = side * Mathf.PI * 2 / sides;
            float irregular = 1 + .12f * Mathf.Sin(angle * 3 + seed);
            var p = centre + new Vector3(Mathf.Cos(angle) * (east ? .7f : 1.55f), 0, Mathf.Sin(angle) * (east ? 1.55f : .7f)) * r * irregular;
            Ground(ref p); v.Add(p); colors.Add(new Color(r, 0, 0, 1));
            if (ring == 0) continue;
            int n = ring * sides + side, next = ring * sides + (side + 1) % sides;
            triangles.AddRange(new[] { n - sides, next, n, n - sides, next - sides, next });
        }
        var mesh = new Mesh { name = "Soft wall foot earth" }; meshes.Add(mesh);
        mesh.SetVertices(v); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = ground; renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
    void LateUpdate()
    {
        var camera = Camera.main; if (camera == null) return;
        foreach (var plant in plants) plant.rotation = camera.transform.rotation;
    }
    void OnDestroy()
    {
        foreach (var saved in originals)
        {
            if (saved.renderer == null) continue;
            var block = new MaterialPropertyBlock(); saved.renderer.GetPropertyBlock(block);
            block.SetVector("_FootMoss", saved.moss); saved.renderer.SetPropertyBlock(block);
        }
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        if (ground != null) Destroy(ground);
    }
}
