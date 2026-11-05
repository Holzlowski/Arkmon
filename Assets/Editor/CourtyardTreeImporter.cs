using UnityEditor;
using UnityEngine;

// This folder contains standalone painted silhouettes, not atlas slices.
public sealed class CourtyardTreeImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool farm = assetPath == "Assets/Resources/CourtyardStyle/Farm/AurenfeldFarm.png";
        if ((!assetPath.StartsWith("Assets/Resources/CourtyardStyle/Trees/") && !farm) || !assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.mipMapsPreserveCoverage = true;
        importer.alphaTestReferenceValue = .35f;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, farm ? .04f : .015f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }
}
