using UnityEngine;
using UnityEngine.Rendering;

// Painted scenic landmark, intentionally outside the playable courtyard.
public sealed class CourtyardDistantFarm : MonoBehaviour
{
    public static readonly Vector3 Site = new Vector3(-144, 0, 14);
    Material smokeMaterial, paper;
    internal static void Install()
    {
        if (GameObject.Find("Aurenfeld distant farm") != null) return;
        var meadow = GameObject.Find("Castle horizon and golden fields")?.GetComponentInChildren<MeadowVegetation>();
        var sprite = Resources.Load<Sprite>("CourtyardStyle/Farm/AurenfeldFarm");
        if (meadow == null || sprite == null) return;
        var root = new GameObject("Aurenfeld distant farm");
        var farm = root.AddComponent<CourtyardDistantFarm>();
        root.transform.position = meadow.SurfacePoint(Site);
        // Fixed upright scenic card: no turning buildings as the camera glides through the breach.
        var art = new GameObject("Painted Aurenfeld farm", typeof(SpriteRenderer)); art.transform.SetParent(root.transform, false);
        art.transform.rotation = Quaternion.LookRotation(new Vector3(-69, 0, 20));
        art.transform.localScale = Vector3.one * (20 / sprite.bounds.size.x);
        farm.paper = new Material(Resources.Load<Shader>("AurenfeldScenicPaper")) { name = "Painted distant farm" };
        var renderer = art.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = farm.paper;
        renderer.color = new Color(.94f, .94f, .89f);
        // Anchor at the illustrated chimney mouth (normalized texture coordinates, bottom origin).
        var mouth = new Vector2(.137f, .861f);
        var local = (Vector2.Scale(mouth, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
        farm.Smoke(art.transform.TransformPoint(new Vector3(local.x, local.y, -.02f)));
        CourtyardFarmApproach.Install(root.transform, meadow);
    }
    void Smoke(Vector3 position)
    {
        var shader = Resources.Load<Shader>("CourtyardFarmSmoke"); if (shader == null) return;
        smokeMaterial = new Material(shader) { name = "Warm distant chimney smoke" };
        smokeMaterial.SetColor("_Tint", new Color(.83f,.73f,.56f,.85f));
        var go = new GameObject("Farm chimney smoke", typeof(ParticleSystem)); go.transform.SetParent(transform, false); go.transform.position = position;
        var particles = go.GetComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.useAutoRandomSeed = false; particles.randomSeed = 724;
        var main = particles.main; main.loop = true; main.duration = 12; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8, 11); main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(.9f, 1.3f); main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.cullingMode = ParticleSystemCullingMode.Automatic;
        var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .12f;
        var emission = particles.emission; emission.rateOverTime = 1.8f;
        var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = .14f; velocity.y = .68f; velocity.z = .25f;
        var noise = particles.noise; noise.enabled = true; noise.strength = .16f; noise.frequency = .25f; noise.scrollSpeed = .15f;
        var size = particles.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .6f, 1, 3.1f));
        var fade = particles.colorOverLifetime; fade.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.55f, .12f), new GradientAlphaKey(.38f, .5f), new GradientAlphaKey(0, 1) }); fade.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = smokeMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        particles.Play();
    }
    void OnDestroy()
    {
        if (smokeMaterial != null) Destroy(smokeMaterial);
        if (paper != null) Destroy(paper);
    }
}
