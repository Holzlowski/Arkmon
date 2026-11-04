using UnityEditor;
using UnityEngine;

public static class ConfigureHannesStable
{
    [MenuItem("Arkmon/Build Hannes stable and expressions")]
    public static void Run(){ImportArt();ConfigureIntroCourtyard.Run();}
    public static void ImportArt()
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Hannes"))AssetDatabase.CreateFolder("Assets/Resources","Hannes");
        var texture=CourtyardCharacterArt.Texture(CourtyardCharacterArt.Folder+"Hannes poses.png");
        float w=texture.width*.5f,h=texture.height*.5f;
        string[] poses={"Idle","WorkForward","WorkBack","WorkMiddle"};
        for(int i=0;i<4;i++)
            CourtyardCharacterArt.Sprite(texture,new Rect((i%2+.05f)*w,(1-i/2)*h,w*.90f,h),
                new Vector2(.3888889f,.01f),h/2.65f,"Assets/Resources/Hannes/"+poses[i]+".asset","Hannes-"+poses[i]);
        CourtyardCharacterArt.Portraits("Hannes");AssetDatabase.SaveAssets();
    }
}