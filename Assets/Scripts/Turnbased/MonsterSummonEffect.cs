using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSummonEffect : MonoBehaviour
{
    public event Action<Vector3> OnPulseMoved;
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
        SoundManager.instance?.PlayEffect(reverse ? "recall" : "summon");
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
            var sparks = new GameObject("Colored summon sparks", typeof(ParticleSystem));
            sparks.transform.SetParent(pulse.transform, false);
            ParticleSystem particles = sparks.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true; main.startLifetime = .85f; main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .25f); main.startColor = tint;
            main.startRotation = Mathf.PI * .25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 180;
            var emission = particles.emission; emission.rateOverTime = 85;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .6f;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Resources.Load<Material>("Magic Sparks") ?? material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            particles.Play();
            particles.Emit(24);
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
                OnPulseMoved?.Invoke(pulse.transform.position);
                travel += Time.deltaTime;
                yield return null;
            }
            pulse.transform.position = destination;
            OnPulseMoved?.Invoke(destination);
            particles.Emit(42);
            trail.emitting = false;
            if (!reverse)
            {
                float elapsed = 0f;
                while (elapsed < arrivalDuration)
                {
                    OnPulseMoved?.Invoke(destination);
                    pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, elapsed / arrivalDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                SoundManager.instance?.PlayEffect("summon-arrive");
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
