using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Soft ambient contact for stationary work furniture, in addition to real sun shadows.
public sealed class CourtyardFurnitureShadows : MonoBehaviour
{
    sealed class Contact { public Renderer source; public MeshRenderer shadow; }
    readonly List<Contact> contacts = new List<Contact>();
    readonly List<Mesh> meshes = new List<Mesh>();
    Material material;

    internal static void Install()
    {
        if (GameObject.Find("Courtyard furniture contacts") != null) return;
        var shader = Resources.Load<Shader>("PaperGroundShadow");
        if (shader == null) return;
        var style = new GameObject("Courtyard furniture contacts").AddComponent<CourtyardFurnitureShadows>();
        style.material = new Material(shader) { name = "Soft courtyard furniture contact" };
        style.material.SetColor("_ShadowColor", new Color(.12f, .10f, .08f, .34f));
        Physics.SyncTransforms();
        foreach (string name in new[] { "Workbench", "Garden potting table", "Marta side bench", "Garden bench seat" })
        {
            var furniture = GameObject.Find(name);
            if (furniture != null) style.Add(furniture.transform);
        }
    }

    void Add(Transform furniture)
    {
        var filters = furniture.GetComponentsInChildren<MeshFilter>().Where(f =>
            f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().enabled).ToArray();
        if (filters.Length == 0) return;
        // Measure in the furniture's horizontal axes; a rotated bench keeps a narrow footprint.
        var rotation = Quaternion.Euler(0, furniture.eulerAngles.y, 0);
        var inverse = Quaternion.Inverse(rotation);
        var points = filters.SelectMany(f => f.sharedMesh.vertices.Select(v => inverse * (f.transform.TransformPoint(v) - furniture.position))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var p in points) bounds.Encapsulate(p);
        float width = Mathf.Max(.2f, bounds.size.x * 1.24f), depth = Mathf.Max(.2f, bounds.size.z * 1.4f);
        const int columns = 9, rows = 7;
        var vertices = new Vector3[columns * rows]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
        for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
        {
            int i = row * columns + col;
            float u = col / (float)(columns - 1), v = row / (float)(rows - 1);
            var point = furniture.position + rotation * new Vector3(bounds.center.x + (u - .5f) * width, bounds.min.y, bounds.center.z + (v - .5f) * depth);
            // Sample only walkable ground; never put the contact on a seat or tabletop.
            var hits = Physics.RaycastAll(point + Vector3.up * .3f, Vector3.down, 6, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.CompareTag("Ground") && h.normal.y > .9f && !h.transform.IsChildOf(furniture)).OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0) return; // Unsupported furniture should not create a floating dark patch.
            point.y = hits[0].point.y + .06f; // Above the thin decorative dirt/paving surfaces.
            vertices[i] = point; uv[i] = new Vector2(u, v);
            if (row > 0 && col > 0) triangles.AddRange(new[] { i - columns - 1, i - 1, i - columns, i - columns, i - 1, i });
        }
        var mesh = new Mesh { name = furniture.name + " soft contact" }; meshes.Add(mesh);
        mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        var obj = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(transform, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        var source = filters.OrderByDescending(f => f.GetComponent<MeshRenderer>().bounds.size.sqrMagnitude).First().GetComponent<MeshRenderer>();
        contacts.Add(new Contact { source = source, shadow = renderer });
    }
    void LateUpdate()
    {
        foreach (var contact in contacts)
            contact.shadow.enabled = contact.source != null && contact.source.enabled && contact.source.gameObject.activeInHierarchy;
    }
    void OnDisable() { foreach (var contact in contacts) if (contact.shadow != null) contact.shadow.enabled = false; }
    void OnDestroy()
    {
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
