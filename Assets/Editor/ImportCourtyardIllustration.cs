using UnityEditor;
using UnityEngine;

public static class ImportCourtyardIllustration
{
    const string Folder="Assets/Resources/CourtyardStyle";
    [MenuItem("Arkmon/Import courtyard illustration")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources","CourtyardStyle");
        string path=CourtyardCharacterArt.Folder+"Ida illustrated surfaces.png";
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
        importer.SaveAndReimport();var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string[] names={"Oak","Plaster","Roof","Stone"};
        float[] scale={.7f,.32f,.6f,.9f};
        Color[] tint={new Color(.62f,.72f,.83f),new Color(.90f,.95f,1f),new Color(.72f,.78f,.84f),new Color(.90f,.89f,.85f)};
        for(int i=0;i<4;i++)
        {
            string asset=Folder+"/"+names[i]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(asset);
            if(material==null){material=new Material(Shader.Find("Arkmon/Courtyard Illustrated Surface"));AssetDatabase.CreateAsset(material,asset);}
            material.SetTexture("_BaseMap",atlas);material.SetColor("_BaseColor",tint[i]);
            material.SetVector("_Tile",new Vector4(.5f,.5f,i%2*.5f,(1-i/2)*.5f));
            material.SetFloat("_TextureScale",scale[i]);material.SetFloat("_Wood",i==0?1:0);EditorUtility.SetDirty(material);
        }
        path=Folder+"/Soft path.mat";var road=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(road==null){road=new Material(Shader.Find("Arkmon/Courtyard Soft Path"));AssetDatabase.CreateAsset(road,path);}
        EditorUtility.SetDirty(road);
        path=CourtyardCharacterArt.Folder+"Courtyard illustrated verges.png";
        CourtyardCharacterArt.Texture(path);importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        string[] props={"Grass","Clover","Flowers","Pebbles","Fern","Mixed"};
        float[] heights={.32f,.22f,.32f,.22f,.34f,.25f};int w=texture.width/3,h=texture.height/2;
        for(int i=0;i<6;i++)
        {
            int ox=i%3*w,oy=(1-i/3)*h,left=w,right=0,bottom=h,top=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[(oy+y)*texture.width+ox+x].a>200)
            {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            var rect=new Rect(ox+left,oy+bottom,right-left+1,top-bottom+1);
            CourtyardCharacterArt.Sprite(texture,rect,new Vector2(.5f,0),rect.height/heights[i],Folder+"/"+props[i]+".asset",props[i]);
        }
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
    }
}
