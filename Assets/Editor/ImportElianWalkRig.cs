using UnityEditor;
using UnityEngine;

public static class ImportElianWalkRig
{
    [MenuItem("Arkmon/Import continuous Elian walk rig")]
    public static void Run()
    {
        const string path = "Assets/Resources/Elian-Walk-Rig.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true; importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int w = texture.width, h = texture.height;
        Save(texture, "Family", new RectInt(0, (int)(h * .39f), w / 2, h - (int)(h * .39f)), 1.42f, false);
        Save(texture, "Room", new RectInt(w / 2, (int)(h * .39f), w - w / 2, h - (int)(h * .39f)), 1.42f, false);
        Save(texture, "NearLeg", new RectInt(0, 0, w / 2, (int)(h * .38f)), .72f, true);
        Save(texture, "FarLeg", new RectInt(w / 2, 0, w - w / 2, (int)(h * .38f)), .72f, true);
        importer.isReadable = false; importer.SaveAndReimport();
        AssetDatabase.SaveAssets();

    }

    private static void Save(Texture2D texture, string name, RectInt region, float height, bool leg)
    {
        int x0 = region.xMax, y0 = region.yMax, x1 = region.xMin, y1 = region.yMin;
        var pixels = texture.GetPixels32();
        for (int y = region.yMin; y < region.yMax; y++)
            for (int x = region.xMin; x < region.xMax; x++)
                if (pixels[y * texture.width + x].a > 200)
                { x0 = Mathf.Min(x0, x); y0 = Mathf.Min(y0, y); x1 = Mathf.Max(x1, x); y1 = Mathf.Max(y1, y); }
        var rect = new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        float pivotX = .5f;
        if (leg)
        {
            float sum = 0, count = 0;
            for (int y = y1 - (int)(rect.height * .12f); y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (pixels[y * texture.width + x].a > 200) { sum += x; count++; }
            if (count > 0) pivotX = (sum / count - x0) / rect.width;
        }
        var sprite = Sprite.Create(texture, rect, new Vector2(pivotX, leg ? 1 : 0), rect.height / height, 0, SpriteMeshType.FullRect);
        sprite.name = "Elian-Walk-" + name;
        string path = "Assets/Resources/" + sprite.name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (saved == null) AssetDatabase.CreateAsset(sprite, path);
        else { EditorUtility.CopySerialized(sprite, saved); Object.DestroyImmediate(sprite); EditorUtility.SetDirty(saved); }
    }
}
