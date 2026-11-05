using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The existing soil and borders retain their interaction/collision layout.
[DefaultExecutionOrder(9501)]
public sealed class CourtyardVegetableBed : MonoBehaviour
{
    readonly List<SpriteRenderer> plants = new List<SpriteRenderer>();
    readonly List<Renderer> earth = new List<Renderer>();
    Renderer soil;
    Transform art;
    Color dryEarth;
    float wetness;
    public float Wetness => wetness;

    internal static void Install()
    {
        var beds = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.name == "Garden soil bed").OrderBy(r => r.transform.position.x).ToArray();
        var oldCrops = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.name == "Lettuce" || r.name == "Carrot greens" ||
                        r.name == "Carrot shoulders" || r.name == "Bean leaves").ToArray();
        for (int i = 0; i < beds.Length; i++)
        {
            if (beds[i].GetComponent<CourtyardVegetableBed>() != null) continue;
            var bed = beds[i].gameObject.AddComponent<CourtyardVegetableBed>();
            bed.soil = beds[i];
            bed.dryEarth = bed.soil.sharedMaterial.GetColor("_BaseColor");
            bed.Build(i);
            // Leave the original art visible if a required resource is missing.
            if (bed.plants.Count == 0) continue;
            var bounds = beds[i].bounds;
            foreach (var old in oldCrops)
                if (old.transform.position.x > bounds.min.x && old.transform.position.x < bounds.max.x &&
                    old.transform.position.z > bounds.min.z && old.transform.position.z < bounds.max.z)
                    old.enabled = false;
            bed.SetWetness(CourtyardGarden.IsBedWatered(i) ? 1 : 0);
        }
    }

    void Build(int kind)
    {
        string[] names = kind == 0 ? new[] {"Cabbage", "CabbageSmall"} :
                         kind == 1 ? new[] {"Carrot", "CarrotSmall"} : new[] {"Bean", "Bean"};
        var sprites = names.Select(n => Resources.Load<Sprite>("GardenPlants/" + n)).ToArray();
        if (sprites.Any(s => s == null)) return;
        art = new GameObject("Illustrated vegetable bed").transform;
        art.SetParent(transform.parent, false);
        var bounds = soil.bounds;
        var footprint = new Vector3(bounds.size.x / 4.3f, 1, bounds.size.z / 3.3f);
        float plantScale = Mathf.Min(footprint.x, footprint.z);
        var random = new System.Random(729 + kind);
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
            {
                float dx = ((float)random.NextDouble() - .5f) * .15f;
                float dz = ((float)random.NextDouble() - .5f) * .12f;
                var point = new Vector3(bounds.center.x + (-1.5f + col + dx) * footprint.x,
                    bounds.max.y + .012f, bounds.center.z + (-.91f + row * .93f + dz) * footprint.z);
                AddPlant(sprites[(row + col) % 2], point, (.91f + (float)random.NextDouble() * .17f) * plantScale, col % 2 == 1);
                // Low irregular clods define the rows without introducing obstacles.
                var clod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                clod.name = "Garden earth clod";
                var collider = clod.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
                clod.transform.SetParent(art, true); clod.transform.position = point - Vector3.up * .018f;
                clod.transform.localScale = Vector3.Scale(new Vector3(.38f, .055f, .25f), footprint);
                var renderer = clod.GetComponent<Renderer>(); renderer.sharedMaterial = soil.sharedMaterial;
                earth.Add(renderer);
            }
        var grass = Resources.Load<Sprite>("GardenPlants/Grass");
        if (grass != null)
            foreach (var offset in new[] {new Vector3(-1.82f,0,.72f),new Vector3(1.78f,0,-.45f),new Vector3(.36f,0,1.35f)})
                AddPlant(grass, new Vector3(bounds.center.x,bounds.max.y+.01f,bounds.center.z)+Vector3.Scale(offset, footprint), .8f * plantScale, offset.x < 0);
    }

    void AddPlant(Sprite sprite, Vector3 point, float scale, bool mirror)
    {
        var obj = new GameObject("Painted garden " + sprite.name, typeof(SpriteRenderer));
        obj.transform.SetParent(art, true); obj.transform.position = point;
        // Use an unscaled sibling holder so billboards do not shear with the soil cube.
        obj.transform.localScale = Vector3.one * scale;
        var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.flipX = mirror;
        plants.Add(renderer);
    }

    void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null) return;
        foreach (var plant in plants)
            plant.transform.rotation = camera.transform.rotation;
    }

    void OnDestroy() { if (art != null) Destroy(art.gameObject); }

    public void SetWetness(float amount)
    {
        wetness = Mathf.Clamp01(amount);
        foreach (var plant in plants) plant.color = Color.Lerp(Color.white, new Color(.88f,.96f,.91f,1),wetness);
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", Color.Lerp(dryEarth * .88f, dryEarth * .49f, wetness));
        foreach (var clod in earth) clod.SetPropertyBlock(block);
    }
}
