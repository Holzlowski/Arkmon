using UnityEditor;
using UnityEngine;

public static class ImportStableIllustration
{
    [MenuItem("Arkmon/Import illustrated stable surfaces")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        const string folder="Assets/Resources/CourtyardStyle";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","CourtyardStyle");
        string path=CourtyardCharacterArt.Folder+"Hannes illustrated surfaces.png";
        CourtyardCharacterArt.Texture(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string[] names={"Stable oak","Stable roof","Stable earth","Stable straw earth"};
        Color[] colors={new Color(.60f,.72f,.85f),new Color(.57f,.68f,.72f),new Color(.62f,.70f,.79f,.90f),new Color(.68f,.75f,.82f,.94f)};
        for(int i=0;i<4;i++)
        {
            path=folder+"/"+names[i]+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find(i<2?"Arkmon/Courtyard Illustrated Surface":"Arkmon/Courtyard Painted Ground Patch");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",colors[i]);
            material.SetVector("_Tile",new Vector4(.5f,.5f,i%2*.5f,(1-i/2)*.5f));
            material.SetFloat("_TextureScale",i==0?.7f:i==1?.52f:.50f);
            if(i<2)material.SetFloat("_Wood",i==0?1:0);
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
}
