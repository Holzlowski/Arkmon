using UnityEngine;

public class CombatStrikeVisual : MonoBehaviour
{
    private Material material;
    private LineRenderer line;
    private Vector3 origin, destination;
    private float elapsed;
    public static void Show(Vector3 origin, Vector3 destination, bool fire)
    {
        Show(origin, destination, fire ? new Color(1f, .35f, .03f) : new Color(1f, .9f, .7f),
            fire ? .15f : .08f, fire ? "Flame sword slash" : "Claw slash");
    }

    public static void ShowIce(Vector3 origin, Vector3 destination)
        => Show(origin, destination, new Color(.44f, .86f, 1f), .12f, "Ice volley");

    internal static void Show(Vector3 origin, Vector3 destination, Color tint, float width, string name)
    {
        var root = new GameObject(name);
        var effect = root.AddComponent<CombatStrikeVisual>();
        effect.origin = origin; effect.destination = destination;
        effect.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        effect.material.color = tint;
        effect.line = root.AddComponent<LineRenderer>();
        effect.line.sharedMaterial = effect.material;
        effect.line.positionCount = 8; effect.line.numCapVertices = 3;
        effect.line.startWidth = width; effect.line.endWidth = 0.01f;
        effect.Draw(0.2f);
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / 0.35f);
        Draw(progress);
        if (progress >= 1f) Destroy(gameObject);
    }
    private void Draw(float progress)
    {
        for (int i = 0; i < 8; i++)
        {
            float t = (float)i / 7 * progress;
            line.SetPosition(i, Vector3.Lerp(origin, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f);
        }
        line.widthMultiplier = 1f - progress * 0.8f;
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
