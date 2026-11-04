using UnityEditor;
using UnityEngine;

public static class ImportElianGardenRig
{
    [MenuItem("Arkmon/Import Elian garden animation art")]
    public static void Run()
    {
        const string path="Assets/Art/Intro/CharacterStyle/Elian garden puppet.png";
        AssetDatabase.Refresh();
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Garden"))AssetDatabase.CreateFolder("Assets/Resources","Garden");
        // Atlas rows have different heights. Only sprite metadata is cropped; source pixels stay intact.
        Save(texture,"Family",new RectInt(0,916,512,620),1.42f,new Vector2(.5f,0));
        Save(texture,"Room",new RectInt(512,916,512,620),1.42f,new Vector2(.5f,0));
        Save(texture,"UpperArm",new RectInt(0,512,512,404),.36f,new Vector2(.52f,.90f));
        Save(texture,"Forearm",new RectInt(512,512,512,404),.40f,new Vector2(.18f,.90f));
        Save(texture,"FarForearm",new RectInt(512,0,512,512),.40f,new Vector2(.18f,.90f));
        Save(texture,"Can",new RectInt(0,0,512,512),.64f,new Vector2(.38f,.94f));
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
    }
    static void Save(Texture2D texture,string name,RectInt region,float height,Vector2 pivot)
    {
        var pixels=texture.GetPixels32();int x0=region.xMax,y0=region.yMax,x1=region.xMin,y1=region.yMin;
        for(int y=region.yMin;y<region.yMax;y++)for(int x=region.xMin;x<region.xMax;x++)
            if(pixels[y*texture.width+x].a>200){x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
        if(x1<=x0 || y1<=y0)throw new System.InvalidOperationException("Missing garden sprite "+name);
        var rect=new Rect(x0,y0,x1-x0+1,y1-y0+1);
        float ppu=rect.height/height;
        // These source pieces contain an entire bent arm. Retain only elbow-to-hand;
        // otherwise the separate upper arm gives Elian a second shoulder at his elbow.
        if(name=="Forearm" || name=="FarForearm")rect.height=Mathf.Floor(rect.height*.65f);
        if(name=="UpperArm")
        {
            // The separate sleeve drawing ends in an open cuff; hide it inside the elbow joint.
            float trim=Mathf.Floor(rect.height*.12f);
            float oldHeight=rect.height;
            rect.y+=trim;rect.height-=trim;
            pivot.y=(oldHeight*pivot.y-trim)/rect.height;
        }
        CourtyardCharacterArt.Sprite(texture,rect,pivot,ppu,"Assets/Resources/Garden/"+name+".asset","Garden-"+name);
    }
}