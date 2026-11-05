using UnityEditor;
using UnityEngine;

public static class ImportMartaGardenDetails
{
    [MenuItem("Arkmon/Import Marta garden details")]
    public static void Run()
    {
        const string path="Assets/Art/Intro/CharacterStyle/Marta garden details.png";
        AssetDatabase.Refresh();CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;
        importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        if(!AssetDatabase.IsValidFolder("Assets/Resources/GardenDetails"))AssetDatabase.CreateFolder("Assets/Resources","GardenDetails");
        string[] names={"Seeds","Tools","Cabbage","Carrot","Sprout","Seedlings"};
        float[] heights={.64f,.78f,.72f,.72f,.72f,.38f};
        int width=texture.width/3,height=texture.height/2;
        for(int i=0;i<names.Length;i++)
        {
            int ox=i%3*width,oy=(1-i/3)*height;
            int x0=width,y0=height,x1=0,y1=0;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                if(pixels[(oy+y)*texture.width+ox+x].a>200)
                {x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
            if(x1<=x0 || y1<=y0)throw new System.InvalidOperationException("Empty garden detail "+names[i]);
            var rect=new Rect(ox+x0,oy+y0,x1-x0+1,y1-y0+1);
            CourtyardCharacterArt.Sprite(texture,rect,new Vector2(.5f,0),rect.height/heights[i],
                "Assets/Resources/GardenDetails/"+names[i]+".asset",names[i]);
        }
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
    }
}