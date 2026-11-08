using System.Collections.Generic;
using UnityEngine;

// One scene-owned setup; wind runs on the GPU, without per-plant Update behaviours.
public sealed class CourtyardPlantWind : MonoBehaviour
{
    sealed class Binding
    {
        public SpriteRenderer renderer;
        public Sprite original, animated;
        public Material originalMaterial, windMaterial;
        public Bounds originalBounds, windBounds;
    }
    readonly List<Binding> bindings = new List<Binding>();
    readonly Dictionary<Sprite, Sprite> sprites = new Dictionary<Sprite, Sprite>();
    readonly Dictionary<Sprite, Material> materials = new Dictionary<Sprite, Material>();

    internal static void Install()
    {
        if (GameObject.Find("Courtyard gentle plant wind") != null) return;
        var shader = Resources.Load<Shader>("CourtyardPlantWind");
        if (shader == null || !shader.isSupported) return;
        var style = new GameObject("Courtyard gentle plant wind").AddComponent<CourtyardPlantWind>();
        var allowed = new Dictionary<Sprite, float>();
        foreach (var kind in new[] { "Grass", "Clover", "Mixed", "Fern", "Flowers" })
        {
            var sprite = Resources.Load<Sprite>("CourtyardStyle/" + kind);
            if (sprite != null) allowed[sprite] = kind == "Clover" ? .025f : kind == "Fern" ? .06f : .045f;
        }
        foreach (var kind in new[] { "Oak", "Birch", "Apple", "Willow" })
        {
            var tree = Resources.Load<Sprite>("CourtyardStyle/Trees/" + kind);
            if(tree!=null)allowed[tree]=kind=="Willow"?.30f:kind=="Birch"?.24f:.18f;
        }
        foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            var source = renderer.sprite;
            if (renderer.gameObject.scene != style.gameObject.scene || source == null || !allowed.TryGetValue(source, out var strength)) continue;
            if (!style.sprites.TryGetValue(source, out var sprite))
            {
                sprite = Subdivide(source); style.sprites.Add(source, sprite);
                var material = new Material(shader) { name = "Gentle wind " + source.name };
                material.SetVector("_PlantShape", new Vector4(source.bounds.min.y, source.bounds.size.y, strength, strength>.1f?1:0));
                style.materials.Add(source, material);
            }
            var bounds = renderer.localBounds;
            var expanded = bounds;
            expanded.Expand(new Vector3(strength * 2.5f / Mathf.Max(.001f, Mathf.Abs(renderer.transform.lossyScale.x)), 0, 0));
            style.bindings.Add(new Binding { renderer = renderer, original = source, animated = sprite,
                originalMaterial = renderer.sharedMaterial, windMaterial = style.materials[source], originalBounds = bounds, windBounds = expanded });
        }
        style.Apply();
    }
    static Sprite Subdivide(Sprite source)
    {
        // Clone the Sprite, never alter the resource used by the editor or another scene.
        var copy = Instantiate(source); copy.name = source.name + " wind mesh";
        const int columns = 5, rows = 7;
        var vertices = new Vector2[columns * rows]; var triangles = new List<ushort>();
        // OverrideGeometry expects pixel coordinates inside the sprite rectangle;
        // Unity applies the pivot and pixels-per-unit conversion itself.
        var minimum = Vector2.zero;
        var maximum = source.rect.size;
        for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
        {
            int i = row * columns + column;
            vertices[i] = new Vector2(Mathf.Lerp(minimum.x, maximum.x, column / (float)(columns - 1)),
                Mathf.Lerp(minimum.y, maximum.y, row / (float)(rows - 1)));
            if (row == 0 || column == 0) continue;
            triangles.AddRange(new[] { (ushort)(i - columns - 1), (ushort)(i - 1), (ushort)(i - columns),
                (ushort)(i - columns), (ushort)(i - 1), (ushort)i });
        }
        copy.OverrideGeometry(vertices, triangles.ToArray()); return copy;
    }
    void Apply()
    {
        foreach (var binding in bindings)
        {
            if (binding.renderer == null || binding.renderer.sprite != binding.original) continue;
            binding.renderer.sprite = binding.animated; binding.renderer.sharedMaterial = binding.windMaterial;
            binding.renderer.localBounds = binding.windBounds;
        }
    }
    void Restore()
    {
        foreach (var binding in bindings)
        {
            var renderer = binding.renderer; if (renderer == null) continue;
            if (renderer.sprite == binding.animated) { renderer.sprite = binding.original; renderer.localBounds = binding.originalBounds; }
            if (renderer.sharedMaterial == binding.windMaterial) renderer.sharedMaterial = binding.originalMaterial;
        }
    }
    void OnEnable() => Apply();
    void OnDisable() => Restore();
    void OnDestroy()
    {
        Restore();
        foreach (var sprite in sprites.Values) if (sprite != null) Destroy(sprite);
        foreach (var material in materials.Values) if (material != null) Destroy(material);
    }
}
