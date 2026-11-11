using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CreateElianBucketStudy
{
    public const string ScenePath="Assets/Scenes/AnimationStudies/ElianBucketMotionStudy.unity";
    [MenuItem("Arkmon/Animationsprobe/Eimer öffnen")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(System.IO.File.Exists(ScenePath))EditorSceneManager.OpenScene(ScenePath);
        else Create();
    }
    public static void Create()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Elian bucket motion study",typeof(ElianBucketMotionStudy));
        EditorSceneManager.SaveScene(scene,ScenePath);
        Debug.Log("ELIAN_BUCKET_STUDY_READY "+ScenePath+" (not added to build settings)");
    }
}