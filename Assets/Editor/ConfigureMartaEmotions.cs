using UnityEditor;
using UnityEngine;

public static class ConfigureMartaEmotions
{
    [MenuItem("Arkmon/Configure Marta dialogue expressions")]
    public static void Run()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Marta"))AssetDatabase.CreateFolder("Assets/Resources","Marta");
        var texture=CourtyardCharacterArt.Texture(CourtyardCharacterArt.Folder+"Marta gardener.png");
        CourtyardCharacterArt.Sprite(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.015f),
            texture.height/2.26f,"Assets/Resources/Marta/Gardener.asset","Marta-Gardener");
        CourtyardCharacterArt.Portraits("Marta");AssetDatabase.SaveAssets();
    }
}