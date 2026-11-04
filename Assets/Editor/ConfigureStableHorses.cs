using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureStableHorses
{
    [MenuItem("Arkmon/Build Hannes horses and open stalls")]
    public static void Run()
    {
        const string folder="Assets/Resources/StableHorses";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","StableHorses");
        Import("Chestnut",new Vector2(.5f,.104f),2.85f);
        Import("Dun",new Vector2(.5f,.018f),2.72f);
        foreach(string file in new[]{"horse-snort.wav","saddle-brush.ogg","straw-rustle.ogg"})
        {
            var importer=AssetImporter.GetAtPath("Assets/Resources/Audio/Stable/"+file) as AudioImporter;
            if(importer==null)continue;
            importer.forceToMono=true;
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;
            settings.quality=.8f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        var stable=GameObject.Find("Stable");
        if(stable==null)throw new System.InvalidOperationException("The courtyard stable is missing.");
        var masonry=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Intro/Courtyard stone architecture.mat");
        CourtyardStable.Build(stable.transform,masonry);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Import(string name,Vector2 pivot,float width)
    {
        var texture=CourtyardCharacterArt.Texture("Assets/Art/Intro/StableHorses/"+name+".png");
        CourtyardCharacterArt.Sprite(texture,new Rect(0,0,texture.width,texture.height),pivot,texture.width/width,
            "Assets/Resources/StableHorses/"+name+".asset","Stable horse "+name);
    }
}