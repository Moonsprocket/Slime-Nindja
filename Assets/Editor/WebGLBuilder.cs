#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    [MenuItem("Build/Build WebGL (Release)")]
    public static void BuildRelease()
    {
        Build(BuildOptions.None);
    }

    [MenuItem("Build/Build WebGL (Development)")]
    public static void BuildDev()
    {
        Build(BuildOptions.Development);
    }

    public static void Build(BuildOptions options = BuildOptions.None)
    {
        Debug.Log("[WebGLBuilder] Starting WebGL Build...");

        // Ensure settings are configured for WebGL and Yandex Games
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "PROJECT:YandexGames";
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.WebGL.maximumMemorySize = 512;

        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Build/WebGL");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = outputDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = options
        };

        Debug.Log($"[WebGLBuilder] Building to {outputDir} with options: {options}...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuilder] BUILD SUCCEEDED! Total size: {summary.totalSize} bytes, Total time: {summary.totalTime.TotalSeconds:F1}s");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] BUILD FAILED with result: {summary.result}. Errors: {summary.totalErrors}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
