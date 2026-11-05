using UnityEditor;
using UnityEngine;

// Reuse the approved architectural atlas; keep garden accents as separate materials.
public static class ImportGardenIllustration
{
    [MenuItem("Arkmon/Import illustrated garden materials")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        Copy("Oak","Garden sage",new Color(.28f,.78f,.62f),.7f);
        Copy("Plaster","Garden clay",new Color(.84f,.47f,.29f),1.2f);
        AssetDatabase.SaveAssets();
    }
    static void Copy(string source,string name,Color tint,float scale)
    {
        const string folder="Assets/Resources/CourtyardStyle/";
        var original=AssetDatabase.LoadAssetAtPath<Material>(folder+source+".mat");
        if(original==null)throw new System.InvalidOperationException("Import courtyard illustration first: "+source);
        var target=AssetDatabase.LoadAssetAtPath<Material>(folder+name+".mat");
        if(target==null){target=new Material(original);AssetDatabase.CreateAsset(target,folder+name+".mat");}
        else target.CopyPropertiesFromMaterial(original);
        target.SetColor("_BaseColor",tint);target.SetFloat("_TextureScale",scale);EditorUtility.SetDirty(target);
    }
}
