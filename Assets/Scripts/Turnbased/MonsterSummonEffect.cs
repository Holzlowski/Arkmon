using System.Collections;
using UnityEngine;

public class MonsterSummonEffect : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float travelDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float arrivalDuration = 0.3f;
    [SerializeField] private Color color = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Material effectMaterial;
    private GameObject effectRoot;
    private Material runtimeMaterial;

    public IEnumerator Play(Vector3 origin, Vector3 destination, Color? monsterColor = null)
    {
        Color tint = monsterColor ?? color;
        effectRoot = new GameObject("Monster summon effect");
        effectRoot.transform.SetParent(transform, false);
        // Tint a private copy so different monsters never recolor the shared asset.
        runtimeMaterial = effectMaterial != null ? new Material(effectMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        runtimeMaterial.color = tint;
        Material material = runtimeMaterial;
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.transform.SetParent(effectRoot.transform, false);
        Destroy(pulse.GetComponent<Collider>());
        pulse.GetComponent<Renderer>().sharedMaterial = material;
        pulse.transform.localScale = Vector3.one * 0.3f;
        TrailRenderer trail = pulse.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = 0.2f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0.01f;
        trail.startColor = Color.white;
        trail.endColor = Color.white;
        pulse.transform.position = origin;
        trail.Clear();
        Vector3 target = destination + Vector3.up * 0.6f;
        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            float t = Mathf.Clamp01(elapsed / travelDuration);
            pulse.transform.position = Vector3.Lerp(origin, target, t)
                + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        pulse.transform.position = target;
        trail.emitting = false;
        elapsed = 0f;
        while (elapsed < arrivalDuration)
        {
            float t = elapsed / arrivalDuration;
            pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Clear();
    }

    private void Clear()
    {
        if (effectRoot != null) Destroy(effectRoot);
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        effectRoot = null;
        runtimeMaterial = null;
    }

    private void OnDisable() => Clear();
}
