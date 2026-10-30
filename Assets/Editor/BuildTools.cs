using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu + batch entry points. Batch: Unity -batchmode -quit -projectPath . -executeMethod BuildTools.BuildWeb
public static class BuildTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Ferryman/Create Main Scene")]
    public static void EnsureScene()
    {
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            go.AddComponent<Tuning>();
            go.AddComponent<Game>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    // Nimble Fox web settings (see nimble-web-build-spec.md).
    static void ApplyWebSettings()
    {
        PlayerSettings.productName = "Ferryman";
        PlayerSettings.companyName = "HenryAndThatLi";
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.threadsSupport = false;
    }

    [MenuItem("Ferryman/Build Web")]
    public static void BuildWeb()
    {
        EnsureScene();
        ApplyWebSettings();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "WebGLBuild",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        Debug.Log("FERRYMAN BUILD RESULT: " + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
