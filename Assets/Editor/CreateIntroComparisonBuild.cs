using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateIntroComparisonBuild
{
    private const string MenuPath = "Assets/Scenes/IntroComparison.unity";

    [MenuItem("Arkmon/Build Windows intro comparison")]
    public static void Run()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Comparison camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(camera, scene);
            var view = camera.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.09f, .07f, .055f);
            var menu = new GameObject("Intro comparison", typeof(IntroComparisonMenu));
            SceneManager.MoveGameObjectToScene(menu, scene);
            EditorSceneManager.SaveScene(scene, MenuPath);

        }
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-arkmonBuildPath");
        string output = index >= 0 && index + 1 < args.Length ? args[index + 1]
            : Path.GetFullPath("Builds/Arkmon-Intro-Vergleich/Arkmon.exe");
        var scenes = new[] { MenuPath, "Assets/Scenes/TowerRoomPaper.unity" }
            .Concat(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)).Distinct().ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"INTRO_COMPARISON_BUILD {report.summary.result}");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Intro comparison build failed.");
    }
}
