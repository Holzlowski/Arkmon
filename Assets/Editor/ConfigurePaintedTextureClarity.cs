using UnityEditor;
using UnityEngine;

public static class ConfigurePaintedTextureClarity
{
    [MenuItem("Arkmon/Refine painted texture clarity")]
    public static void Run()
    {
        // Preserve mipmaps against distant shimmer; retain more detail at oblique angles.
        foreach (string name in new[] { "Tower-Oak-Painted", "Tower-Plaster-Painted", "DemoStone",
            "Bed-Linen-Painted", "Bed-Velvet-Painted", "Wardrobe-Door-Painted", "DemoDoor" })
        {
            string path = "Assets/Art/SceneStyle/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 16;
            importer.mipMapBias = -.25f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(width,height));
            // Match Windows builds to the editor without overriding other platforms.
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = importer.maxTextureSize;
            standalone.format = TextureImporterFormat.RGBA32;
            standalone.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
}
