using UnityEditor;
using UnityEngine;

// Sprite rectangles are metadata; generated source pixels remain untouched.
public static class CourtyardCharacterArt
{
    public const string Folder="Assets/Art/Intro/CharacterStyle/";
    public static Texture2D Texture(string path,bool preserveDimensions=false)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        if(preserveDimensions)importer.npotScale=TextureImporterNPOTScale.None;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static Sprite Sprite(Texture2D texture,Rect rect,Vector2 pivot,float ppu,string path,string name)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var replacement=UnityEngine.Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);
        replacement.name=name;
        if(existing==null)
        {
            AssetDatabase.CreateAsset(replacement,path);
            replacement.name=name;
            EditorUtility.SetDirty(replacement);
            return replacement;
        }
        // Keep references valid across both image and slicing/layout revisions.
        EditorUtility.CopySerialized(replacement,existing);
        Object.DestroyImmediate(replacement);
        EditorUtility.SetDirty(existing);
        return existing;
    }
    public static void Portraits(string speaker)
    {
        const string folder="Assets/Resources/DialoguePortraits";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","DialoguePortraits");
        var texture=Texture(Folder+speaker+" expressions.png");
        float w=texture.width*.5f,h=texture.height*.5f;
        string[] moods={"neutral","friendly","surprised","worried"};
        var sprites=new Sprite[4];
        for(int i=0;i<4;i++)
            sprites[i]=Sprite(texture,new Rect(i%2*w,(1-i/2)*h,w,h),new Vector2(.5f,.5f),100,
                folder+"/"+speaker+"-"+moods[i]+".asset",speaker+"-"+moods[i]);
        string path=folder+"/"+speaker+" Portrait Set.asset";
        var set=AssetDatabase.LoadAssetAtPath<DialoguePortraitSet>(path);
        if(set==null){set=ScriptableObject.CreateInstance<DialoguePortraitSet>();AssetDatabase.CreateAsset(set,path);}
        set.speaker=speaker;set.neutral=sprites[0];set.friendly=sprites[1];set.surprised=sprites[2];set.worried=sprites[3];
        EditorUtility.SetDirty(set);
    }
}