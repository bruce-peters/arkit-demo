using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class iOSBuilder
{
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string buildPath = System.Environment.GetEnvironmentVariable("IOS_BUILD_PATH");
        if (string.IsNullOrEmpty(buildPath))
            buildPath = "build/iOS";

        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.iOS, BuildOptions.None);
        EditorApplication.Exit(0);
    }

    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string pbxPath = Path.Combine(path, "Unity-iPhone.xcodeproj/project.pbxproj");
        if (!File.Exists(pbxPath))
        {
            Debug.LogWarning($"[iOSBuilder] pbxproj not found at {pbxPath}");
            return;
        }

        string content = File.ReadAllText(pbxPath);
        int shellStart = content.IndexOf("shellScript = \"mkdir");
        if (shellStart >= 0)
        {
            int shellEnd = content.IndexOf("\";", shellStart) + 2;
            string oldShell = content.Substring(shellStart, shellEnd - shellStart);
            string newShell = "shellScript = \"cd \\\"$PROJECT_DIR\\\" && mkdir -p \\\"$CONFIGURATION_TEMP_DIR/artifacts/arm64/buildstate\\\" && make\\n\";";
            content = content.Replace(oldShell, newShell);
            File.WriteAllText(pbxPath, content);
        }

        string srcMakefile = Path.Combine(Application.dataPath, "Editor/Makefile.iOSTemplate");
        string dstMakefile = Path.Combine(path, "Makefile");
        if (File.Exists(srcMakefile))
        {
            File.Copy(srcMakefile, dstMakefile, true);
        }

        Debug.Log("[iOSBuilder] Replaced IL2CPP with native clang++ (make-based).");
    }
}