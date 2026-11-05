using System.Collections.Generic;
using UnityEngine;

// Per-renderer, reversible sunlight emphasis. No extra lights, colliders or material copies.
public sealed class CourtyardMorningAccents : MonoBehaviour
{
    sealed class Accent
    {
        public MeshRenderer renderer;
        public Vector4 originalZone, zone;
        public float originalStrength, strength;
    }
    readonly List<Accent> accents = new List<Accent>();
    readonly HashSet<MeshRenderer> assigned = new HashSet<MeshRenderer>();
    static readonly int Zone = Shader.PropertyToID("_MorningAccent");
    static readonly int Strength = Shader.PropertyToID("_MorningAccentStrength");

    internal static void Install()
    {
        if (GameObject.Find("Courtyard morning accents") != null) return;
        var style = new GameObject("Courtyard morning accents").AddComponent<CourtyardMorningAccents>();
        var entrance = GameObject.Find("Tower entrance");
        if (entrance != null)
            style.Add(entrance.transform.position + Vector3.up * 2.7f, 7f, .55f,
                "Tower entrance", "Player residence tower", "Aurenfeld raised approach");
        style.Add(new Vector3(-42.5f, 2.5f, 7), 7.5f, .65f,
            "Illustrated west breach", "West wall near", "West wall far", "Broken west wall base");
        style.Add(new Vector3(43, 3.2f, -1), 6.5f, .45f,
            "Illustrated east crossing", "East wall north of gate", "East wall south of gate", "Gate stone pier");
        var portal = GameObject.Find("Illustrated castle entrance");
        if (portal != null)
            style.Add(portal.transform.position + Vector3.up * 2.3f, 6f, .38f,
                "Illustrated castle entrance");
        var pottingTable = GameObject.Find("Garden potting table");
        if (pottingTable != null)
            style.Add(pottingTable.transform.position + Vector3.up, 8f, .30f,
                "Garden potting table", "Kitchen garden layout", "Garden bench seat", "Garden bench stone foot",
                "Marta side bench", "Worn approach flagstone");
        style.Apply(true);
    }
    void Add(Vector3 center, float radius, float strength, params string[] roots)
    {
        foreach (string name in roots)
        {
            // Some authored pieces share a name (e.g. the two gate piers).
            foreach (var transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform.name != name) continue;
                foreach (var renderer in transform.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.name == "Garden soil bed" || renderer.name == "Garden earth clod") continue;
                    var material = renderer.sharedMaterial;
                    if (!renderer.enabled || material == null || !material.HasProperty(Strength)
                        || renderer.bounds.SqrDistance(center) > radius * radius || !assigned.Add(renderer)) continue;
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                    accents.Add(new Accent
                    {
                        renderer = renderer, zone = new Vector4(center.x, center.y, center.z, radius), strength = strength,
                        originalZone = block.HasVector(Zone) ? block.GetVector(Zone) : material.GetVector(Zone),
                        originalStrength = block.HasFloat(Strength) ? block.GetFloat(Strength) : material.GetFloat(Strength)
                    });
                }
            }
        }
    }
    void Apply(bool active)
    {
        foreach (var accent in accents)
        {
            if (accent.renderer == null) continue;
            var block = new MaterialPropertyBlock(); accent.renderer.GetPropertyBlock(block);
            block.SetVector(Zone, active ? accent.zone : accent.originalZone);
            block.SetFloat(Strength, active ? accent.strength : accent.originalStrength);
            accent.renderer.SetPropertyBlock(block);
        }
    }
    void OnEnable() => Apply(true);
    void OnDisable() => Apply(false);
    void OnDestroy() => Apply(false);
}
