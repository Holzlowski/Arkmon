using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateCourtyardScaleReview
{
    public const string ScenePath = "Assets/Scenes/AnimationStudies/CourtyardScaleReview.unity";
    private const string GuardScenePath = "Assets/Scenes/AnimationStudies/GuardFaceReview.unity";

    [MenuItem("Arkmon/Animationsprobe/Größenprobe öffnen")]
    public static void Open()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
        else Create();
    }

    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Hof · Größenprobe",typeof(CourtyardScaleReview));
        if (!EditorSceneManager.SaveScene(scene,ScenePath))
            throw new IOException("The scale review scene could not be saved.");
    }

    [MenuItem("Arkmon/Animationsprobe/Burgwache vergleichen")]
    public static void OpenGuardComparison()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(GuardScenePath)) EditorSceneManager.OpenScene(GuardScenePath);
        else CreateGuardComparison();
    }

    [MenuItem("Arkmon/Animationsprobe/Panfries Schildposen vergleichen")]
    public static void OpenPanfrieComparison()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/AnimationStudies/PanfrieShieldReview.unity");
    }

    public static void CreateGuardComparison()
    {
        var variant = AssetDatabase.LoadAssetAtPath<Sprite>(ConfigureCourtyardGuard.FaceVariantPath);
        if (variant == null) variant = ConfigureCourtyardGuard.ImportFaceVariant();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var review = new GameObject("Burgwache · Gesichtsvergleich").AddComponent<CourtyardScaleReview>();
        var settings = new SerializedObject(review);
        settings.FindProperty("guardBodyVariant").objectReferenceValue = variant;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene,GuardScenePath))
            throw new IOException("The guard review scene could not be saved.");
    }
}
