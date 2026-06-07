using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        string outPath = Path.GetFullPath("Builds/balloon.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/ballooon.unity" },
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BuildScript] SUCCESS: " + outPath);
        else
        {
            Debug.LogError("[BuildScript] FAILED: " + report.summary.result);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroidMenu() => BuildAndroid();
}
