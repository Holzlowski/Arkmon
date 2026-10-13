using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;

// A soft contact shadow keeps the unlit paper art unchanged.
[DisallowMultipleComponent]
public class PaperGroundShadow : MonoBehaviour
{
    private Renderer illustration;
    private MeshRenderer shadow;
    private MaterialPropertyBlock properties;
    private Vector3 lastGroundPosition;
    private float groundHeight;
    private bool groundKnown;
    private bool registered;
    private static Mesh mesh;
    private static Material material;
    private static int users;
    private static readonly List<Renderer> paving = new List<Renderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= AddSceneShadows;
        SceneManager.sceneLoaded += AddSceneShadows;
    }

    private static void AddSceneShadows(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) paving.Clear();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.IndexOf("paving", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && renderer.bounds.size.y < .2f) paving.Add(renderer);
                Material paper = renderer.sharedMaterial;
                if (!renderer.enabled || paper == null || !paper.name.Contains("-Paper")
                    || paper.name.Contains("Cloud") || renderer is ParticleSystemRenderer) continue;
                if (renderer.GetComponentInParent<PaperGroundShadow>() == null)
                    Attach(renderer.gameObject, renderer);
            }
    }

    public static void Attach(GameObject owner, Renderer renderer)
    {
        if (renderer == null) return;
        PaperGroundShadow component = owner.GetComponent<PaperGroundShadow>();
        if (component == null) component = owner.AddComponent<PaperGroundShadow>();
        component.illustration = renderer;
    }

    private void Awake()
    {
        if (material == null)
        {
            Shader shader = Resources.Load<Shader>("PaperGroundShadow");
            if (shader == null) { enabled = false; return; }
            material = new Material(shader) { name = "Shared paper ground shadow" };
            mesh = new Mesh { name = "Shared paper ground shadow quad" };
            mesh.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f),
                new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
        }
        users++;
        registered = true;
        var plane = new GameObject("Paper contact shadow", typeof(MeshFilter), typeof(MeshRenderer));
        plane.transform.SetParent(transform, false);
        plane.GetComponent<MeshFilter>().sharedMesh = mesh;
        shadow = plane.GetComponent<MeshRenderer>();
        shadow.sharedMaterial = material;
        shadow.shadowCastingMode = ShadowCastingMode.Off;
        shadow.receiveShadows = false;
        properties = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (shadow == null || illustration == null) return;
        bool visible = illustration.enabled && illustration.gameObject.activeInHierarchy;
        shadow.enabled = visible;
        if (!visible) return;
        Vector3 anchor = transform.position;
        // Raycast only after actual movement; ignore this figure's own collider.
        if (!groundKnown || (anchor - lastGroundPosition).sqrMagnitude > .0001f)
        {
            float reach = Mathf.Max(3, illustration.bounds.size.y + 2);
            groundHeight = 0;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(anchor + Vector3.up * reach,
                Vector3.down, reach + 10, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)
                    || transform.IsChildOf(hit.transform) || hit.normal.y < .9f) continue;
                if (hit.distance < nearest) { nearest = hit.distance; groundHeight = hit.point.y; }
            }
            // Decorative paving sits above the collider floor without its own collider.
            foreach (Renderer tile in paving)
            {
                if (tile == null || !tile.enabled) continue;
                Bounds bounds = tile.bounds;
                if (anchor.x >= bounds.min.x && anchor.x <= bounds.max.x
                    && anchor.z >= bounds.min.z && anchor.z <= bounds.max.z
                    && bounds.max.y >= groundHeight && bounds.max.y - groundHeight < .3f)
                    groundHeight = bounds.max.y;
            }
            groundKnown = true;
            lastGroundPosition = anchor;
        }
        SpriteRenderer sprite = illustration as SpriteRenderer;
        float width = sprite != null && sprite.sprite != null
            ? sprite.sprite.bounds.size.x * Mathf.Abs(sprite.transform.lossyScale.x)
            : Mathf.Max(illustration.bounds.size.x, illustration.bounds.size.z);
        width = Mathf.Max(.01f, width * .72f);
        shadow.transform.SetPositionAndRotation(new Vector3(anchor.x, groundHeight + .008f, anchor.z), Quaternion.identity);
        // World dimensions are independent of the parent illustration's scaling.
        Vector3 parentScale = transform.lossyScale;
        shadow.transform.localScale = new Vector3(width / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), width * .52f / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
        float opacity = sprite != null ? sprite.color.a : 1;
        properties.SetColor("_ShadowColor", new Color(.12f, .09f, .07f, .3f * opacity));
        shadow.SetPropertyBlock(properties);
    }

    private void OnDisable() { if (shadow != null) shadow.enabled = false; }
    private void OnDestroy()
    {
        if (!registered) return;
        if (shadow != null) Destroy(shadow.gameObject);
        if (--users == 0)
        {
            Destroy(material); Destroy(mesh);
            material = null; mesh = null;
        }
    }
}
