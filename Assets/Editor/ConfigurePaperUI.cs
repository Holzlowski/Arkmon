using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ConfigurePaperUI
{
    [MenuItem("Arkmon/Configure paper UI assets")]
    public static void Run()
    {
        Texture2D panels = Import("Assets/Art/UI/Paper-Panels.png");
        Texture2D icons = Import("Assets/Art/UI/Action-Icons.png");

        var style = AssetDatabase.LoadAssetAtPath<PaperUIStyle>("Assets/Resources/Paper UI.asset");
        if (style == null) { style = ScriptableObject.CreateInstance<PaperUIStyle>(); AssetDatabase.CreateAsset(style, "Assets/Resources/Paper UI.asset"); }
        style.panel = SpriteAsset("Paper panel", panels, UV(panels, .043f, .548f, .435f, .410f), new Vector4(64, 64, 64, 64));
        style.card = SpriteAsset("Paper card", panels, UV(panels, .520f, .548f, .440f, .410f), new Vector4(64, 64, 64, 64));
        style.sageButton = SpriteAsset("Sage button", panels, UV(panels, .043f, .06f, .435f, .420f), new Vector4(64, 64, 64, 64));
        style.selectedCard = SpriteAsset("Selected card", panels, UV(panels, .520f, .06f, .440f, .420f), new Vector4(64, 64, 64, 64));
        Sprite[] glyphs = new Sprite[6]; string[] names = { "Move icon", "Claw icon", "Sword icon", "Escape icon", "Fire icon", "Down icon" };
        for (int i = 0; i < glyphs.Length; i++)
        {
            Rect cell = new Rect(i % 2 * icons.width / 2f, (2 - i / 2) * icons.height / 3f, icons.width / 2f, icons.height / 3f);
            glyphs[i] = SpriteAsset(names[i], icons, VisibleRect(icons, cell), Vector4.zero);
        }
        style.moveIcon = glyphs[0]; style.clawIcon = glyphs[1]; style.swordIcon = glyphs[2];
        style.escapeIcon = glyphs[3]; style.fireIcon = glyphs[4]; style.downIcon = glyphs[5];
        var cat = Texture("Assets/Sprites/Monster/Flaca-Paper.png");
        var sheep = Texture("Assets/Sprites/Monster/Distelwolle-Paper.png");
        var hunter = Texture("Assets/Sprites/Characters/Shepherd-Paper.png");
        style.flacaPortrait = SpriteAsset("Flaca portrait", cat, UV(cat, .09f, .44f, .7f, .54f), Vector4.zero);
        style.sheepPortrait = SpriteAsset("Distelwolle portrait", sheep, UV(sheep, .02f, .14f, .96f, .82f), Vector4.zero);
        style.hunterPortrait = SpriteAsset("Hunter portrait", hunter, UV(hunter, .12f, .48f, .76f, .5f), Vector4.zero);
        style.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/Alegreya SDF.asset")
            ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
        foreach (string path in new[] { "Assets/Art/UI/Paper-Panels.png", "Assets/Art/UI/Action-Icons.png" })
        { var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.isReadable = false; importer.SaveAndReimport(); }
        Debug.Log("Paper UI sprites and shared style configured.");
    }

    private static Texture2D Import(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureShape = TextureImporterShape.Texture2D; importer.textureType = TextureImporterType.Default; importer.spriteImportMode = SpriteImportMode.None;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
        return Texture(path);
    }

    private static Texture2D Texture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().FirstOrDefault();
        if (texture == null) texture = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()?.texture;
        if (texture == null) throw new System.InvalidOperationException("Missing UI texture: " + path);
        return texture;
    }

    private static Rect UV(Texture2D texture, float x, float y, float w, float h)
        => new Rect(Mathf.Round(x * texture.width), Mathf.Round(y * texture.height), Mathf.Round(w * texture.width), Mathf.Round(h * texture.height));

    private static Rect VisibleRect(Texture2D texture, Rect cell)
    {
        Color32[] pixels = texture.GetPixels32();
        int minX = (int)cell.xMax, minY = (int)cell.yMax, maxX = (int)cell.x, maxY = (int)cell.y;
        for (int y = (int)cell.y + 6; y < (int)cell.yMax - 6; y++)
            for (int x = (int)cell.x + 6; x < (int)cell.xMax - 6; x++)
                if (pixels[y * texture.width + x].a > 150)
                { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static Sprite SpriteAsset(string name, Texture2D texture, Rect rect, Vector4 border)
    {
        string path = "Assets/Art/UI/" + name + ".asset";
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
        sprite.name = name;
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing == null) AssetDatabase.CreateAsset(sprite, path);
        else { EditorUtility.CopySerialized(sprite, existing); Object.DestroyImmediate(sprite); sprite = existing; EditorUtility.SetDirty(existing); }
        return sprite;
    }
}
