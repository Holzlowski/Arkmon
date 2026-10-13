using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CreateDemoBuild
{
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-arkmonBuildPath");
        string output = index >= 0 && index + 1 < args.Length ? args[index + 1]
            : Path.GetFullPath("Builds/Arkmon-Demo-" + PlayerSettings.bundleVersion + "/Arkmon.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Debug.Log("DEMO_BUILD_RESULT " + report.summary.result + " errors=" + report.summary.totalErrors
            + " size=" + report.summary.totalSize + " version=" + PlayerSettings.bundleVersion);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows demo build failed.");
    }
}
