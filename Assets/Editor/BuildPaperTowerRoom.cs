using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPaperTowerRoom
{
    private const string PreviewPath = "Assets/Scenes/TowerRoomPaper.unity";
    private const string Art = "Assets/Art/SceneStyle/";
    private const string Materials = "Assets/Art/Intro/PaintedFurniture";

    [MenuItem("Arkmon/Create painted 3D tower comparison")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TowerRoom.unity");
        if (!AssetDatabase.IsValidFolder(Materials))
            AssetDatabase.CreateFolder("Assets/Art/Intro", "PaintedFurniture");

        var shader = Shader.Find("Arkmon/Illustrated Wardrobe");
        var furnitureWood = WardrobePaint(shader, "Illustrated furniture oak", false, .48f);
        var copies = new Dictionary<Material, Material>();
        foreach (string name in new[] { "Canopy bed", "Wardrobe", "Writing desk", "Bedside chest", "Bedside water table", "Elian study wall shelf", "Folded bedside throw", "Deep window seat" })
        {
            var prop = GameObject.Find(name);
            if (prop == null) continue;
            foreach (var renderer in prop.GetComponentsInChildren<MeshRenderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null || !source.HasProperty("_BaseMap") || !source.HasProperty("_Smoothness"))
                        continue;
                    if (!copies.TryGetValue(source, out var painted))
                    {
                        string materialName = source.name.ToLowerInvariant();
                        painted = materialName.Contains("oak") ? furnitureWood
                            : materialName.Contains("linen") || materialName.Contains("velvet") ? FabricPaint(shader, source)
                            : Paint(source);
                        copies.Add(source, painted);
                    }
                    slots[i] = painted;
                }
                renderer.sharedMaterials = slots;
            }
        }
        var doors = WardrobePaint(shader, "Illustrated wardrobe panels", true, .42f);
        var frame = WardrobePaint(shader, "Illustrated wardrobe frame", false, .39f);
        var interior = WardrobePaint(shader, "Illustrated wardrobe interior", false, .24f);
        foreach (var renderer in GameObject.Find("Wardrobe").GetComponentsInChildren<MeshRenderer>(true))
        {
            switch (renderer.name)
            {
                case "Illustrated left door":
                case "Illustrated right door":
                    renderer.sharedMaterial = doors; break;
                case "Dark inner back":
                case "Solid door thickness":
                    renderer.sharedMaterial = interior; break;
                case "Oak cabinet side":
                case "Cabinet base":
                case "Upper cornice":
                case "Carved foot":
                case "Curved painted crown":
                    renderer.sharedMaterial = frame; break;
            }
        }
        PaintRoomSurfaces(shader, furnitureWood, copies);
        EditorSceneManager.SaveScene(scene, PreviewPath, true);
        AssetDatabase.SaveAssets();
    }

    private static void PaintRoomSurfaces(Shader shader, Material wood, Dictionary<Material, Material> copies)
    {
        string[] groups = { "Round tower visible walls", "Tower upper cutaway masonry", "Stone floor",
            "Window rounded limestone frame", "Window slender mullions", "Window curtain rod",
            "Gathered window curtain", "Painted three dimensional door", "Elian personal room details",
            "Tower candle chandelier", "Heirloom rug", "Aurenfeld family crest", "Aurenfeld family crest (1)" };
        var visited = new HashSet<MeshRenderer>();
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            bool selected = false;
            for (var parent = renderer.transform; parent != null && !selected; parent = parent.parent)
                selected = System.Array.IndexOf(groups, parent.name) >= 0;
            if (!selected || !visited.Add(renderer)) continue;
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                var source = slots[i];
                if (source == null || !source.HasProperty("_BaseMap") ||
                    AssetDatabase.GetAssetPath(source).StartsWith(Materials + "/")) continue;
                if (!copies.TryGetValue(source, out var painted))
                {
                    string name = source.name.ToLowerInvariant();
                    if (source.HasProperty("_Illustration"))
                    {
                        painted = Paint(source);
                        painted.SetFloat("_Illustration", 1);
                        EditorUtility.SetDirty(painted);
                    }
                    else if (name.Contains("green oak"))
                        painted = FabricPaint(shader, source, source.GetColor("_BaseColor") * .56f);
                    else if (name.Contains("oak") || renderer.name.Contains("mullion"))
                        painted = wood;
                    else if (name.Contains("velvet") || name.Contains("linen"))
                        painted = FabricPaint(shader, source);
                    else if (source.HasProperty("_Smoothness"))
                        painted = Paint(source);
                    else continue;
                    copies.Add(source, painted);
                }
                slots[i] = painted;
            }
            renderer.sharedMaterials = slots;
        }
    }

    private static Material WardrobePaint(Shader shader, string name, bool panels, float tint)
    {
        string path = Materials + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", new Color(tint, tint * .94f, tint * .86f));
        material.SetFloat("_Panels", panels ? 1f : 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material FabricPaint(Shader shader, Material source, Color? tint = null)
    {
        string name = "Illustrated fabric - " + source.name;
        var paint = WardrobePaint(shader, name, false, .52f);
        if (tint.HasValue) paint.SetColor("_BaseColor", tint.Value);
        paint.SetFloat("_Fabric", 1f);
        paint.SetTexture("_PaintMap", source.GetTexture("_BaseMap"));
        paint.SetTextureScale("_PaintMap", source.GetTextureScale("_BaseMap"));
        paint.SetTextureOffset("_PaintMap", source.GetTextureOffset("_BaseMap"));
        EditorUtility.SetDirty(paint);
        return paint;
    }

    private static Material Paint(Material source)
    {
        string path = Materials + "/" + source.name + ".mat";
        var painted = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (painted == null)
        {
            painted = new Material(source);
            AssetDatabase.CreateAsset(painted, path);
        }
        else painted.CopyPropertiesFromMaterial(source);

        string name = source.name.ToLowerInvariant();
        string texture = name.Contains("oak") ? "Tower-Oak-Painted.png"
            : name.Contains("velvet") ? "Bed-Velvet-Painted.png"
            : name.Contains("linen") ? "Bed-Linen-Painted.png" : null;
        if (texture != null)
            painted.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + texture));

        if (painted.HasProperty("_Smoothness"))
        {
        painted.SetFloat("_Smoothness", .015f);
        painted.SetFloat("_Metallic", 0);
        painted.SetFloat("_SpecularHighlights", 0);
        painted.SetFloat("_EnvironmentReflections", 0);
        painted.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        painted.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        }

        // A restrained pigment fill preserves the painted detail under the room's warm shadows.
        var map = painted.GetTexture("_BaseMap");
        if (map != null && painted.HasProperty("_EmissionMap"))
        {
            Color tint = painted.GetColor("_BaseColor");
            painted.SetTexture("_EmissionMap", map);
            painted.SetTextureScale("_EmissionMap", painted.GetTextureScale("_BaseMap"));
            painted.SetTextureOffset("_EmissionMap", painted.GetTextureOffset("_BaseMap"));
            painted.SetColor("_EmissionColor", new Color(tint.r, tint.g, tint.b) * .14f);
            painted.EnableKeyword("_EMISSION");
            painted.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        EditorUtility.SetDirty(painted);
        return painted;
    }
}
