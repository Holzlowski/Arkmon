using UnityEditor;
using UnityEngine;

public static class ConfigureGlutfunke
{
    [MenuItem("Arkmon/Configure Glutfunke claw animation")]
    public static void Run()
    {
        const string image="Assets/Sprites/Monster/Kafla-SparkBody.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(image);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        // The replacement body keeps the original rig's 1.67-unit height and bottom pivot.
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.spritePixelsPerUnit=height/1.67f;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;settings.spritePivot=new Vector2(.5f,0);
        importer.SetTextureSettings(settings);importer.SaveAndReimport();
        const string path="Assets/Prefabs/Monster/Monster.prefab";
        GameObject prefab=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var data=new SerializedObject(prefab.GetComponent<PaperSwordAnimation>());
            data.FindProperty("sparkBodySprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(image);
            data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();
        Debug.Log("Glutfunke claw body configured.");
    }
}
