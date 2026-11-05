using UnityEditor;
using UnityEngine;

public static class ConfigureMartaEmotions
{
    [MenuItem("Arkmon/Configure Marta dialogue expressions")]
    public static void Run()
    {
        RepairWorldSprite();
        CourtyardCharacterArt.Portraits("Marta");AssetDatabase.SaveAssets();
    }
    [MenuItem("Arkmon/Repair Marta sprite proportions")]
    public static void RepairWorldSprite()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(CourtyardCharacterArt.Folder+"Marta gardener.png");
        // Preserve the drawing's 1024 x 1536 aspect instead of stretching it to 1024 x 2048.
        importer.npotScale=TextureImporterNPOTScale.None;
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Marta"))AssetDatabase.CreateFolder("Assets/Resources","Marta");
        var texture=CourtyardCharacterArt.Texture(CourtyardCharacterArt.Folder+"Marta gardener.png");
        CourtyardCharacterArt.Sprite(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.015f),
            texture.height/2.26f,"Assets/Resources/Marta/Gardener.asset","Marta-Gardener");
        AssetDatabase.SaveAssets();
    }
}