using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    // Batch mode entry: Unity.exe -batchmode -quit -projectPath <path> -executeMethod WebGLBuild.Build
    public static void Build()
    {
        PlayerSettings.productName = "Mask";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        // itch.io does not send Content-Encoding headers, so gzip needs the JS fallback.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        BuildReport report = BuildPipeline.BuildPlayer(scenes, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);

        Debug.Log($"WebGL build result: {report.summary.result}, size {report.summary.totalSize} bytes");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
