using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ConfigureKaflaRefinement
{
    const string Art="Assets/Sprites/Monster/";
    [MenuItem("Arkmon/Refine Kafla starter art")]
    public static void Run()
    {
        const string path=Art+"Kafla-Refined-Rig.png";
        var importer=Prepare(path,true);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Rect body=OpaqueRect(texture,0,.45f),arm=OpaqueRect(texture,.45f,.72f),sword=OpaqueRect(texture,.75f,1);
        importer.spritePixelsPerUnit=body.height/1.67f;
        #pragma warning disable CS0618
        importer.spritesheet=new[]{Frame("Kafla-Body",body,new Vector2(.5f,0)),Frame("Kafla-Arm",arm,new Vector2(1,.5f)),Frame("Kafla-Sword",sword,new Vector2(.5f,.08f))};
        #pragma warning restore CS0618
        importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        var bodySprite=sprites.First(s=>s.name=="Kafla-Body");var armSprite=sprites.First(s=>s.name=="Kafla-Arm");
        var swordSprite=sprites.First(s=>s.name=="Kafla-Sword");
        // A shorter forearm stays ahead of the cape while the shoulder cap is behind it.
        var forearm=Sprite.Create(texture,new Rect(arm.x,arm.y,arm.width*.78f,arm.height),new Vector2(1,.5f),importer.spritePixelsPerUnit,0,SpriteMeshType.FullRect);
        forearm=Save("Kafla-Refined-Forearm",forearm);
        var gripImporter=Prepare(Art+"Kafla-Grip-Arm.png",false);
        var gripTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(gripImporter.assetPath);
        Rect gripRect=OpaqueRect(gripTexture,0,1);
        float gripPpu=gripRect.height/armSprite.bounds.size.y;
        Rect forearmRect=new Rect(gripRect.x+gripRect.width*.32f,gripRect.y,gripRect.width*.68f,gripRect.height);
        var grippingArm=Save("Kafla-Gripping-Arm",Sprite.Create(gripTexture,forearmRect,new Vector2(1,.5f),gripPpu,0,SpriteMeshType.FullRect));
        var grippingForearm=Save("Kafla-Gripping-Forearm",Sprite.Create(gripTexture,new Rect(forearmRect.x,forearmRect.y,forearmRect.width*.8f,forearmRect.height),new Vector2(1,.5f),gripPpu,0,SpriteMeshType.FullRect));
        var grippingPaw=Save("Kafla-Gripping-Paw",Sprite.Create(gripTexture,new Rect(gripRect.x,gripRect.y,gripRect.width*.44f,gripRect.height),new Vector2(.5f,.5f),gripPpu,0,SpriteMeshType.FullRect));
        var sparkImporter=Prepare(Art+"Kafla-Refined-Spark-Rig.png",false);
        var sparkTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(sparkImporter.assetPath);
        var spark=Save("Kafla-Refined-SparkBody",Sprite.Create(sparkTexture,body,new Vector2(.5f,0),importer.spritePixelsPerUnit,0,SpriteMeshType.FullRect));
        var portraitRect=new Rect(body.x+body.width*.15f,body.y+body.height*.48f,body.width*.78f,body.height*.52f);
        var portrait=Save("Kafla-Refined-Portrait",Sprite.Create(texture,portraitRect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect));
        const string prefabPath="Assets/Prefabs/Monster/Monster.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animation=prefab.GetComponent<PaperSwordAnimation>();var data=new SerializedObject(animation);
            var renderer=(SpriteRenderer)data.FindProperty("bodyRenderer").objectReferenceValue;
            renderer.sprite=bodySprite;
            data.FindProperty("armSprite").objectReferenceValue=grippingArm;
            data.FindProperty("forearmSprite").objectReferenceValue=grippingForearm;
            data.FindProperty("gripHandSprite").objectReferenceValue=grippingPaw;
            data.FindProperty("scrapeHandSprite").objectReferenceValue=forearm;
            data.FindProperty("wristReach").floatValue=1.2f;
            data.FindProperty("swordSprite").objectReferenceValue=swordSprite;
            data.FindProperty("sparkBodySprite").objectReferenceValue=spark;
            // The tail changes the overall sprite width; anchor at the actual cape/torso joint.
            data.FindProperty("shoulderAnchor").vector2Value=new Vector2(.025f,.47f);
            data.FindProperty("scrapeShoulderDrop").floatValue=0;
            data.FindProperty("scrapeArmAngle").floatValue=-160;
            data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var style=Resources.Load<PaperUIStyle>("Paper UI");style.flacaPortrait=portrait;EditorUtility.SetDirty(style);
        importer.isReadable=false;importer.SaveAndReimport();sparkImporter.isReadable=false;sparkImporter.SaveAndReimport();gripImporter.isReadable=false;gripImporter.SaveAndReimport();AssetDatabase.SaveAssets();
    }
    static TextureImporter Prepare(string path,bool sliced)
    {
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=sliced?SpriteImportMode.Multiple:SpriteImportMode.None;
        i.alphaIsTransparency=true;i.mipmapEnabled=false;i.npotScale=TextureImporterNPOTScale.None;
        i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=2048;i.filterMode=FilterMode.Bilinear;i.isReadable=true;i.SaveAndReimport();return i;
    }
    static SpriteMetaData Frame(string name,Rect rect,Vector2 pivot)=>new SpriteMetaData{name=name,rect=rect,alignment=(int)SpriteAlignment.Custom,pivot=pivot};
    static Rect OpaqueRect(Texture2D texture,float from,float to)
    {
        var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
        for(int y=0;y<texture.height;y++)for(int x=(int)(from*texture.width);x<(int)(to*texture.width);x++)
            if(pixels[y*texture.width+x].a>64){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
    }
    static Sprite Save(string name,Sprite sprite)
    {
        sprite.name=name;string path=Art+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(old==null){AssetDatabase.CreateAsset(sprite,path);return sprite;}
        EditorUtility.CopySerialized(sprite,old);Object.DestroyImmediate(sprite);return old;
    }
}
