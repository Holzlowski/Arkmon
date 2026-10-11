using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSummonEffect : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float travelDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float arrivalDuration = 0.3f;
    [SerializeField] private Color color = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Material effectMaterial;
    private readonly Dictionary<GameObject, Material> activeEffects = new Dictionary<GameObject, Material>();

    public IEnumerator Play(Vector3 origin, Vector3 destination, Color? monsterColor = null)
        => Animate(origin, destination + Vector3.up * 0.6f, monsterColor ?? color, false, null, null);

    public IEnumerator PlayRecall(Vector3 monsterOrigin, Vector3 trainerVessel, Color monsterColor,
        Action<float> onAbsorbProgress, Action onAbsorbed)
        => Animate(monsterOrigin, trainerVessel, monsterColor, true, onAbsorbProgress, onAbsorbed);

    private IEnumerator Animate(Vector3 origin, Vector3 destination, Color tint, bool reverse,
        Action<float> onAbsorbProgress, Action onAbsorbed)
    {
        GameObject root = new GameObject(reverse ? "Monster recall effect" : "Monster summon effect");
        root.transform.SetParent(transform, false);
        Material material = effectMaterial != null ? new Material(effectMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = tint;
        activeEffects.Add(root, material);
        try
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulse.transform.SetParent(root.transform, false);
            Destroy(pulse.GetComponent<Collider>());
            pulse.GetComponent<Renderer>().sharedMaterial = material;
            pulse.transform.position = origin;
            pulse.transform.localScale = Vector3.one * 0.3f;
            TrailRenderer trail = pulse.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.2f;
            trail.startWidth = 0.18f; trail.endWidth = 0.01f;
            trail.startColor = Color.white; trail.endColor = Color.white;
            trail.emitting = !reverse;
            trail.Clear();
            if (reverse)
            {
                float elapsed = 0f;
                while (elapsed < arrivalDuration)
                {
                    float t = Mathf.Clamp01(elapsed / arrivalDuration);
                    pulse.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.3f, t);
                    onAbsorbProgress?.Invoke(t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                onAbsorbProgress?.Invoke(1f);
                onAbsorbed?.Invoke();
                pulse.transform.localScale = Vector3.one * 0.3f;
                trail.emitting = true;
                trail.Clear();
            }
            float travel = 0f;
            while (travel < travelDuration)
            {
                float t = Mathf.Clamp01(travel / travelDuration);
                pulse.transform.position = Vector3.Lerp(origin, destination, t)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.5f);
                travel += Time.deltaTime;
                yield return null;
            }
            pulse.transform.position = destination;
            trail.emitting = false;
            if (!reverse)
            {
                float elapsed = 0f;
                while (elapsed < arrivalDuration)
                {
                    pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, elapsed / arrivalDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }
        finally { Clear(root, material); }
    }

    private void Clear(GameObject root, Material material)
    {
        if (!activeEffects.Remove(root)) return;
        if (root != null) Destroy(root);
        if (material != null) Destroy(material);
    }

    private void OnDisable() => CancelAll();

    public void CancelAll()
    {
        foreach (GameObject root in new List<GameObject>(activeEffects.Keys)) Clear(root, activeEffects[root]);
    }
}
