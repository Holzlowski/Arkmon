using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

// Scoped, repeatable presentation changes; furniture and layout are preserved.
public static class ConfigurePresentationPolish
{
    [MenuItem("Arkmon/Configure readable paper presentation")]
    public static void Run()
    {
        var style=AssetDatabase.LoadAssetAtPath<PaperUIStyle>("Assets/Resources/Paper UI.asset");
        const string fontPath="Assets/Art/Fonts/Alegreya SDF.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(font==null)
        {
            Font source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Alegreya-Regular.ttf");
            font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="Alegreya SDF";AssetDatabase.CreateAsset(font,fontPath);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(Texture2D atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
        }
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        string glyphs = new string(Enumerable.Range(32, 95).Concat(Enumerable.Range(160, 96))
            .Select(c => (char)c).ToArray()) + "−–—…·»«→←↑↓";
        font.TryAddCharacters(glyphs);
        // Freeze the prepared atlas: TMP clears dynamic assets on editor shutdown.
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        foreach (Texture2D atlas in font.atlasTextures) EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(font.material);
        EditorUtility.SetDirty(font);style.font=font;
        var player=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Prince-Mantle.png").OfType<Texture2D>().First();
        Sprite portrait=Sprite.Create(player,new Rect(player.width*.22f,player.height*.60f,player.width*.55f,player.height*.39f),new Vector2(.5f,.5f),100);
        portrait.name="Player portrait";
        const string portraitPath="Assets/Art/UI/Player portrait.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
        if(existing==null){AssetDatabase.CreateAsset(portrait,portraitPath);style.playerPortrait=portrait;}
        else{EditorUtility.CopySerialized(portrait,existing);Object.DestroyImmediate(portrait);EditorUtility.SetDirty(existing);style.playerPortrait=existing;}
        EditorUtility.SetDirty(style);
        ConfigureDemoRefinements.ConfigureRoomPortrait();
        foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/Characters","Assets/Sprites/Monster","Assets/Resources","Assets/Art/UI"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if(!path.EndsWith(".png"))continue;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null || path.StartsWith("Assets/Resources/") && !path.Contains("Prince-") && !path.Contains("Player-") && !path.Contains("John-"))continue;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;
            importer.SaveAndReimport();
        }
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-HighFidelity.asset");
        pipeline.renderScale=1;pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
        PlayerSettings.resizableWindow=true;
        foreach(string scenePath in new[]{"Assets/Scenes/TowerRoom.unity","Assets/Scenes/StartScene.unity","Assets/Scenes/BattleScene.unity"})
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            foreach(Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.allowMSAA=true;
                var data=camera.GetComponent<UniversalAdditionalCameraData>();
                if(data!=null){data.antialiasing=AntialiasingMode.None;EditorUtility.SetDirty(data);}
                EditorUtility.SetDirty(camera);
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();Debug.Log("Presentation polish configured: player portrait, Alegreya, native render scale, clean sprites.");
    }
}
