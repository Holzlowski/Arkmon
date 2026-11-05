using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Aurenfeld-specific art; the demo and battle tree assets remain available.
[DefaultExecutionOrder(9510)]
public sealed class CourtyardTreeStyle : MonoBehaviour
{
    sealed class Original
    {
        public SpriteRenderer renderer;
        public Sprite sprite;
        public Material material;
        public Vector3 scale, position;
        public Color color;
    }
    readonly List<Original> originals = new List<Original>();
    readonly List<Renderer> retired = new List<Renderer>();
    readonly List<Transform> added = new List<Transform>();
    Material paper;

    internal static void Install()
    {
        if (GameObject.Find("Aurenfeld tree varieties") != null) return;
        var sprites = new[] { "Oak", "Birch", "Apple", "Willow" }
            .Select(n => Resources.Load<Sprite>("CourtyardStyle/Trees/" + n)).ToArray();
        if (sprites.Any(s => s == null)) return;
        var style = new GameObject("Aurenfeld tree varieties").AddComponent<CourtyardTreeStyle>();
        // SpriteRenderer supplies each texture. Do not reuse the old fixed tree material.
        style.paper = new Material(Shader.Find("Sprites/Default")) { name = "Aurenfeld painted tree sprites" };
        var trees = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
            .Where(r => r.name == "Meadow tree" || r.name == "Western field tree" || r.transform.parent?.name == "Meadow tree")
            .OrderBy(r => r.transform.position.x).ThenBy(r => r.transform.position.z).ToArray();
        int index = 0;
        foreach (var tree in trees)
        {
            if (tree.sprite == null) continue;
            var p = tree.transform.position;
            int kind = p.x > 49 && p.x < 60 ? 3 : index++ % 3 == 0 ? 0 : 1;
            float height = tree.sprite.bounds.size.y * Mathf.Abs(tree.transform.lossyScale.y);
            style.originals.Add(new Original { renderer = tree, sprite = tree.sprite, material = tree.sharedMaterial,
                scale = tree.transform.localScale, position = p, color = tree.color });
            float oldBottom = tree.sprite.bounds.min.y * Mathf.Abs(tree.transform.lossyScale.y);
            tree.transform.position += Vector3.up * oldBottom;
            tree.transform.localScale *= height / (sprites[kind].bounds.size.y * Mathf.Abs(tree.transform.lossyScale.y));
            tree.sprite = sprites[kind]; tree.sharedMaterial = style.paper; tree.color = Color.white;
        }
        foreach (var old in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.name == "Tree illustration" && r.transform.parent?.name == "Courtyard paper tree"))
        {
            style.Add(sprites[0], old.transform.parent.position, 5.2f, "Courtyard oak");
            style.retired.Add(old); old.enabled = false;
        }
        // Side of the garden cottage, clear of its beds and the castle facade.
        style.Add(sprites[2], new Vector3(-4.6f, 0, 6.8f), 4.6f, "Garden apple tree");
        // Far bank, north of the bridge: crown does not cross its walkable deck.
        style.Add(sprites[3], new Vector3(52.2f, 0, 9.5f), 7.2f, "Stream willow");
    }
    void Add(Sprite sprite, Vector3 point, float height, string label)
    {
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 20, Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.CompareTag("Ground")) point.y = Mathf.Max(point.y, hit.point.y);
        var tree = new GameObject(label, typeof(SpriteRenderer)); tree.transform.SetParent(transform, false);
        tree.transform.position = point + Vector3.up * .025f;
        tree.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        var renderer = tree.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = paper;
        added.Add(tree.transform);
    }
    void LateUpdate()
    {
        if (Camera.main == null) return;
        foreach (var tree in added) tree.rotation = Camera.main.transform.rotation;
    }
    void OnDestroy()
    {
        foreach (var old in originals) if (old.renderer != null)
        {
            old.renderer.sprite = old.sprite; old.renderer.sharedMaterial = old.material; old.renderer.color = old.color;
            old.renderer.transform.localScale = old.scale; old.renderer.transform.position = old.position;
        }
        foreach (var renderer in retired) if (renderer != null) renderer.enabled = true;
        if (paper != null) Destroy(paper);
    }
}
