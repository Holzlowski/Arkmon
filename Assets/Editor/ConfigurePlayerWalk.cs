using UnityEditor;
using UnityEngine;

public static class ConfigurePlayerWalk
{
    [MenuItem("Arkmon/Import player walk cycles")]
    public static void Run()
    {
        foreach(string outfit in new[]{"Room","Mantle","Room-NoLamp","Mantle-NoLamp"})
        {
            string name="Prince-"+outfit+"-Walk";
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+name+".png");
            if (importer == null) continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            float cellWidth=width/4f,cellHeight=height/2f;
            importer.spritePixelsPerUnit=cellHeight/2f;
            var frames=new SpriteMetaData[8];
            for(int i=0;i<8;i++)frames[i]=new SpriteMetaData{name=name+"-"+i.ToString("D2"),
                rect=new Rect((i%4)*cellWidth,(1-i/4)*cellHeight,cellWidth,cellHeight),
                alignment=(int)SpriteAlignment.BottomCenter,pivot=new Vector2(.5f,0)};
            #pragma warning disable CS0618
            importer.spritesheet=frames;
            #pragma warning restore CS0618
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
}
