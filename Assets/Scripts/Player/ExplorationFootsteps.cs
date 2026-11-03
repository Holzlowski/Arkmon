using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerController))]
public sealed class ExplorationFootsteps : MonoBehaviour
{
    private enum Surface { Stone, Carpet, Grass, Wood }
    private readonly List<(MeshFilter mesh, Surface surface)> overlays = new();
    private PlayerController player;
    private Collider body;
    private Vector3 previous;
    private float distance;
    private int variant;
    private bool courtyard;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Collider>();
        previous = transform.position;
        courtyard = gameObject.scene.name == "StartScene";
        foreach (var mesh in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mesh.gameObject.scene != gameObject.scene || mesh.sharedMesh == null) continue;
            switch (mesh.name)
            {
                case "Heirloom rug": overlays.Add((mesh, Surface.Carpet)); break;
                case "East west crossing path":
                case "Connecting path": overlays.Add((mesh, Surface.Stone)); break;
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector3 movement = position - previous;
        previous = position;
        movement.y = 0;
        float travelled = movement.magnitude;
        if (!player.IsMoving || travelled > .6f || body == null ||
            !Physics.Raycast(new Vector3(position.x, body.bounds.min.y + .18f, position.z),
                Vector3.down, out var hit, .36f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            distance = 0;
            return;
        }
        distance += travelled;
        const float stepLength = 1.2f; // Two footfalls per 2.4-unit walking cycle.
        if (distance < stepLength) return;
        distance -= stepLength;
        Surface surface = SurfaceAt(hit);
        string material = surface switch
        {
            Surface.Carpet => "carpet", Surface.Grass => "grass", Surface.Wood => "wood", _ => "concrete"
        };
        SoundManager.instance?.PlayEffect($"IntroFoley/footstep_{material}_{variant:000}", .38f);
        variant = (variant + 1) % 3;
    }

    private Surface SurfaceAt(RaycastHit hit)
    {
        foreach (var (mesh, surface) in overlays)
        {
            if (mesh == null || !mesh.gameObject.activeInHierarchy) continue;
            var bounds = mesh.sharedMesh.bounds;
            Vector3 p = mesh.transform.InverseTransformPoint(hit.point) - bounds.center;
            // Painted overlays sit just above the solid floor.
            if (Mathf.Abs(mesh.transform.TransformPoint(bounds.center).y - hit.point.y) > .16f) continue;
            if (surface == Surface.Carpet)
            {
                float x = p.x / bounds.extents.x, z = p.z / bounds.extents.z;
                if (x*x + z*z <= 1) return surface;
            }
            else if (Mathf.Abs(p.x) <= bounds.extents.x && Mathf.Abs(p.z) <= bounds.extents.z)
                return surface;
        }
        var material = hit.collider.GetComponent<Renderer>()?.sharedMaterial;
        if (material != null && material.name.IndexOf("oak", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Surface.Wood;
        return courtyard && (hit.collider.name == "Courtyard walkable ground"
            || hit.collider.name == "Rolling courtyard meadow") ? Surface.Grass : Surface.Stone;
    }
}
