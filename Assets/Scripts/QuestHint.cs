using System.Collections.Generic;
using UnityEngine;

// Quest logic activates the cue; the actual interaction focus controls brightness.
[DisallowMultipleComponent, DefaultExecutionOrder(11000)]
public class QuestHint : MonoBehaviour
{
    [SerializeField] private bool objectiveActive = true;
    [SerializeField] private Color tint = new Color(1, .73f, .34f);
    [SerializeField, Range(0,1)] private float distantStrength = .12f, nearbyStrength = .48f;
    private readonly List<MeshRenderer> surfaces = new List<MeshRenderer>();
    private readonly List<LineRenderer> outlines = new List<LineRenderer>();
    private sealed class DoorSparkles
    {
        public ParticleSystem system;
        public Bounds bounds;
        public readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[72];
    }
    private readonly List<DoorSparkles> sparkles = new List<DoorSparkles>();
    private Material sparkleMaterial;
    private Material material;
    private MaterialPropertyBlock properties;
    private PlayerInteractionFocus focus;
    private IInteractableAsPlayer target;
    public float Strength { get; private set; }
    public bool IsBright { get; private set; }
    public bool IsVisible => objectiveActive && isActiveAndEnabled && (surfaces.Count > 0 || sparkles.Count > 0);
    public void SetInteractionTarget(IInteractableAsPlayer interaction) => target = interaction;
    public void SetObjectiveActive(bool active)
    {
        objectiveActive = active;
        if (!active) { Strength = 0; IsBright = false; foreach(var surface in surfaces) if(surface != null) surface.enabled = false; }
        if (!active) ClearSparkles();
        if (!active) foreach(var line in outlines) if(line != null) line.enabled = false;
    }
    private void Start()
    {
        if(target == null) target = GetComponent<IInteractableAsPlayer>();
        bool wardrobe = GetComponent<PaintedWardrobe>() != null;
        Shader shader = Resources.Load<Shader>(wardrobe ? "QuestOutline" : "QuestShimmer");
        if (shader == null) { Debug.LogError("Quest shimmer shader missing", this); enabled = false; return; }
        material = new Material(shader); properties = new MaterialPropertyBlock();
        if (wardrobe)
        {
            var sparkleShader = Resources.Load<Shader>("QuestMote");
            if (sparkleShader != null) sparkleMaterial = new Material(sparkleShader);
            foreach (var face in GetComponentsInChildren<MeshFilter>(true))
            {
                if (!face.name.StartsWith("Illustrated ") || !face.name.EndsWith(" door")) continue;
                Bounds bounds = face.sharedMesh.bounds;
                if (sparkleMaterial != null)
                {
                    var obj = new GameObject("Door quest sparkles", typeof(ParticleSystem));
                    obj.transform.SetParent(face.transform, false);
                    var system = obj.GetComponent<ParticleSystem>();
                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = system.main; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.maxParticles = 72; main.playOnAwake = false;
                    var emission = system.emission; emission.enabled = false;
                    var shape = system.shape; shape.enabled = false;
                    var renderer = system.GetComponent<ParticleSystemRenderer>();
                    renderer.sharedMaterial = sparkleMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    sparkles.Add(new DoorSparkles { system = system, bounds = bounds });
                }
                for (int layer = 0; layer < 2; layer++)
                {
                    var obj = new GameObject(layer == 0 ? "Door outline glow" : "Door outline core", typeof(LineRenderer));
                    obj.transform.SetParent(face.transform, false);
                    var line = obj.GetComponent<LineRenderer>(); line.sharedMaterial = material;
                    line.useWorldSpace = false; line.loop = true; line.positionCount = 4;
                    line.widthMultiplier = layer == 0 ? .055f : .015f;
                    line.numCornerVertices = 4; line.numCapVertices = 4;
                    line.SetPositions(new[] { new Vector3(bounds.min.x,bounds.min.y,-.015f),new Vector3(bounds.min.x,bounds.max.y,-.015f),
                        new Vector3(bounds.max.x,bounds.max.y,-.015f),new Vector3(bounds.max.x,bounds.min.y,-.015f) });
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                    outlines.Add(line);
                }
            }
            Strength = objectiveActive ? distantStrength : 0;
            return;
        }
        foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
        {
            var original = filter.GetComponent<MeshRenderer>();
            if (original == null || !original.enabled || filter.sharedMesh == null) continue;
            var obj = new GameObject("Quest shimmer surface", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(filter.transform, false);obj.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var surface = obj.GetComponent<MeshRenderer>();surface.sharedMaterial = material;
            surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; surface.receiveShadows = false; surfaces.Add(surface);
        }
        Strength = objectiveActive ? distantStrength : 0;
    }
    private void LateUpdate()
    {
        if (material == null) return;
        if (focus == null) focus = FindFirstObjectByType<PlayerInteractionFocus>();
        IsBright = objectiveActive && target != null && focus != null && ReferenceEquals(focus.Current, target);
        Strength = objectiveActive ? Mathf.MoveTowards(Strength, IsBright ? nearbyStrength : distantStrength, Time.unscaledDeltaTime*1.5f) : 0;
        properties.SetColor("_Tint", tint); properties.SetFloat("_Strength", Strength);
        foreach(var surface in surfaces) if(surface != null) { surface.enabled = objectiveActive; surface.SetPropertyBlock(properties); }
        foreach(var line in outlines) if(line != null)
        {
            properties.SetColor("_Tint", new Color(tint.r,tint.g,tint.b,line.name.EndsWith("glow") ? .07f : .16f));
            line.enabled = objectiveActive; line.SetPropertyBlock(properties);
        }
        UpdateSparkles();
    }
    private void UpdateSparkles()
    {
        if (!objectiveActive) { ClearSparkles(); return; }
        float time = Time.unscaledTime;
        foreach (var door in sparkles)
        {
            float width = door.bounds.size.x, height = door.bounds.size.y, perimeter = 2*(width+height);
            for (int i = 0; i < door.particles.Length; i++)
            {
                float distance = Mathf.Repeat(i*perimeter/door.particles.Length + time*.07f, perimeter);
                Vector3 point;
                if(distance < height) point = new Vector3(door.bounds.min.x,door.bounds.min.y+distance,-.04f);
                else if(distance < height+width) point = new Vector3(door.bounds.min.x+distance-height,door.bounds.max.y,-.04f);
                else if(distance < 2*height+width) point = new Vector3(door.bounds.max.x,door.bounds.max.y-(distance-height-width),-.04f);
                else point = new Vector3(door.bounds.max.x-(distance-2*height-width),door.bounds.min.y,-.04f);
                // Independent slow drift and twinkle break up the straight contour.
                point += new Vector3(Mathf.Sin(time*.9f+i*2.3f),Mathf.Cos(time*.7f+i*1.7f),0)*.045f;
                float twinkle = Mathf.Pow(.5f+.5f*Mathf.Sin(time*1.6f+i*2.4f),2);
                door.particles[i] = new ParticleSystem.Particle {
                    position = point, startSize = Mathf.Lerp(.022f,.055f,twinkle),
                    startColor = new Color(tint.r,tint.g,tint.b,Strength*Mathf.Lerp(.15f,1.4f,twinkle)),
                    startLifetime = 1, remainingLifetime = 1
                };
            }
            door.system.SetParticles(door.particles,door.particles.Length);
        }
    }
    private void ClearSparkles() { foreach(var door in sparkles) if(door.system != null) door.system.Clear(); }
    private void OnDisable() { IsBright = false; Strength = 0; ClearSparkles(); foreach(var surface in surfaces) if(surface != null) surface.enabled = false; foreach(var line in outlines) if(line != null) line.enabled = false; }
    private void OnDestroy()
    {
        foreach(var surface in surfaces) if(surface != null){surface.enabled=false;Destroy(surface.gameObject);}
        if(material != null) Destroy(material);
        foreach(var line in outlines) if(line != null) Destroy(line.gameObject);
        foreach(var door in sparkles) if(door.system != null) Destroy(door.system.gameObject);
        if(sparkleMaterial != null) Destroy(sparkleMaterial);
    }
}
