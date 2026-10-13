using UnityEngine;

public class FlameSwordImpactVisual : MonoBehaviour
{
    private FireVisualStyle style;
    private SpriteRenderer[] flames;
    private float elapsed;
    private void Start()
    {
        style = Resources.Load<FireVisualStyle>("Paper Fire");
        if (style == null || style.frames == null || style.frames.Length == 0) { Destroy(gameObject); return; }
        flames = new SpriteRenderer[3];
        for (int i = 0; i < flames.Length; i++)
        {
            var obj = new GameObject("Impact paper flame", typeof(SpriteRenderer)); obj.transform.SetParent(transform, false);
            flames[i] = obj.GetComponent<SpriteRenderer>(); flames[i].sprite = style.frames[0];
            flames[i].sharedMaterial = style.material; flames[i].sortingOrder = 40;
        }
        var embers = new GameObject("Fire impact embers", typeof(ParticleSystem)); embers.transform.SetParent(transform, false);
        ParticleSystem particles = embers.GetComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main; main.loop = false; main.startLifetime = .5f; main.startSpeed = 2.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(.1f, .23f); main.startColor = new Color(1f, .5f, .08f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission; emission.rateOverTime = 0;
        var size = particles.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Magic Sparks");
        particles.Play(); particles.Emit(45);
    }
    private void LateUpdate()
    {
        if (flames == null || Camera.main == null) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / .55f);
        for (int i = 0; i < flames.Length; i++)
        {
            flames[i].sprite = style.frames[(Mathf.FloorToInt(elapsed * 20) + i) % style.frames.Length];
            Transform flame = flames[i].transform;
            flame.position = transform.position + Camera.main.transform.right * (i - 1) * .28f
                - Camera.main.transform.forward * .15f;
            flame.rotation = Camera.main.transform.rotation * Quaternion.Euler(0, 0, (i - 1) * -15f);
            float height = (i == 1 ? 1.7f : 1.1f) * Mathf.Lerp(.6f, 1f, Mathf.Min(1, progress * 5));
            flame.localScale = Vector3.one * height / flames[i].sprite.bounds.size.y;
            flames[i].color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, progress));
        }
        if (progress >= 1) Destroy(gameObject);
    }
    public static void Play(Vector3 position)
    { var root = new GameObject("Flame sword impact"); root.transform.position = position; root.AddComponent<FlameSwordImpactVisual>(); }
}
