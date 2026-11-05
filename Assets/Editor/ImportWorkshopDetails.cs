using UnityEditor;
using UnityEngine;

public static class ImportWorkshopDetails
{
    [MenuItem("Arkmon/Import workshop details artwork")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        string path=CourtyardCharacterArt.Folder+"Ida workshop details.png";
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        const string folder="Assets/Resources/WorkshopDetails";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","WorkshopDetails");
        string[] names={"Tools","Repair","Offcuts","Planks","Shavings","Caddy"};
        float[] heights={.70f,.37f,.86f,.65f,.22f,.38f};
        int w=texture.width/3,h=texture.height/2;
        for(int i=0;i<6;i++)
        {
            int ox=i%3*w,oy=(1-i/3)*h,left=w,right=0,bottom=h,top=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(pixels[(oy+y)*texture.width+ox+x].a>200)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            var rect=new Rect(ox+left,oy+bottom,right-left+1,top-bottom+1);
            CourtyardCharacterArt.Sprite(texture,rect,new Vector2(.5f,0),rect.height/heights[i],
                folder+"/"+names[i]+".asset",names[i]);
        }
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
    }
}
