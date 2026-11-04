using UnityEditor;
using UnityEngine;

public static class ConfigureIdaWorkshop
{
    [MenuItem("Arkmon/Build Ida workshop and expressions")]
    public static void Run(){ImportArt();ConfigureIntroCourtyard.Run();}
    public static void ImportArt()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Ida"))AssetDatabase.CreateFolder("Assets/Resources","Ida");
        var texture=CourtyardCharacterArt.Texture(CourtyardCharacterArt.Folder+"Ida poses.png");
        float w=texture.width*.5f,h=texture.height*.5f;
        string[] poses={"Idle","WorkForward","WorkBack","WorkMiddle"};
        for(int i=0;i<4;i++)
            CourtyardCharacterArt.Sprite(texture,new Rect((i%2+.22f)*w,(1-i/2)*h,w*.70f,h),
                new Vector2(.4f,.01f),h/2.45f,"Assets/Resources/Ida/"+poses[i]+".asset","Ida-"+poses[i]);
        CourtyardCharacterArt.Portraits("Ida");AssetDatabase.SaveAssets();
    }
}