using UnityEditor;
using UnityEngine;
public static class ConfigureMagicVisuals
{
    [MenuItem("Arkmon/Configure magic materials")]
    public static void Run()
    {
        Shader shader = Shader.Find("Arkmon/Magic Sparks");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new System.Exception("Magic sparks shader failed");
        const string path = "Assets/Resources/Magic Sparks.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
    }
}
