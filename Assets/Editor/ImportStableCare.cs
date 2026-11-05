using UnityEditor;
using UnityEngine;

public static class ImportStableCare
{
    [MenuItem("Arkmon/Import stable care artwork")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        string path=CourtyardCharacterArt.Folder+"Hannes horse grooming.png";
        CourtyardCharacterArt.Texture(path);
        var actorImporter=(TextureImporter)AssetImporter.GetAtPath(path);
        actorImporter.npotScale=TextureImporterNPOTScale.None;actorImporter.isReadable=true;actorImporter.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var actorPixels=texture.GetPixels32();
        int w=texture.width/2,h=texture.height/2;
        string[] poses={"GroomIdle","GroomForward","GroomBack","GroomMiddle"};
        for(int i=0;i<4;i++)
        {
            int ox=i%2*w,oy=(1-i/2)*h,bottom=h,top=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(actorPixels[(oy+y)*texture.width+ox+x].a>200){bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            int footLeft=w,footRight=0;
            for(int y=bottom;y<bottom+(top-bottom)*.14f;y++)for(int x=0;x<w;x++)
                if(actorPixels[(oy+y)*texture.width+ox+x].a>200){footLeft=Mathf.Min(footLeft,x);footRight=Mathf.Max(footRight,x);}
            var pivot=new Vector2((footLeft+footRight)*.5f/w,bottom/(float)h);
            CourtyardCharacterArt.Sprite(texture,new Rect(ox,oy,w,h),pivot,(top-bottom+1)/2.55f,
                "Assets/Resources/Hannes/"+poses[i]+".asset",poses[i]);
        }
        actorImporter.isReadable=false;actorImporter.SaveAndReimport();
        path=CourtyardCharacterArt.Folder+"Stable care details.png";
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.SaveAndReimport();
        texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        if(!AssetDatabase.IsValidFolder("Assets/Resources/StableCare"))AssetDatabase.CreateFolder("Assets/Resources","StableCare");
        string[] names={"Bridle","Caddy","Hay","Bucket"};
        float[] heights={1.05f,.45f,.44f,.60f};
        for(int i=0;i<4;i++)
        {
            int x0=i%2*texture.width/2,xEnd=x0+texture.width/2;
            // The bridle's tip extends slightly below the atlas midpoint; hay starts lower.
            int y0=i<2?(int)(texture.height*(i==0?.44f:.5f)):0;
            int yEnd=i<2?texture.height:(int)(texture.height*(i==2?.44f:.5f));
            int left=xEnd,right=x0,bottom=yEnd,top=y0;
            for(int y=y0;y<yEnd;y++)for(int x=x0;x<xEnd;x++)
                if(pixels[y*texture.width+x].a>200)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
            CourtyardCharacterArt.Sprite(texture,rect,new Vector2(.5f,0),rect.height/heights[i],
                "Assets/Resources/StableCare/"+names[i]+".asset",names[i]);
        }
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
    }
}
