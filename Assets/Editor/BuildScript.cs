using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildScript
{
    // Scene order matters: Bootstrap must be index 0, game scene index 1
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Bootstrap.unity",   // index 0 — PIN keypad, Verse auth
        "Assets/Scenes/ballooon.unity",    // index 1 — balloon game
        "Assets/Scenes/Game_Garden.unity", // index 2 — garden game
        "Assets/Scenes/Game_Pose.unity",   // index 3 — gesture pose game
    };

    public static void BuildAndroid()
    {
        string outPath = Path.GetFullPath("Builds/balloon.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var opts = new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = outPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
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
