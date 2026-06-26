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
        bool changed = false;

        if (!content.Contains("SELECT_TOOLCHAIN_SDK_AUTOMATICALLY=\\\"true\\\""))
        {
            content = content.Replace(
                "SELECT_TOOLCHAIN_SDK_AUTOMATICALLY=\\\"false\\\"",
                "SELECT_TOOLCHAIN_SDK_AUTOMATICALLY=\\\"true\\\"");
            changed = true;
        }

        string quarantineCmd = "\\nxattr -rd com.apple.quarantine \\\"$IL2CPP_DIR\\\" 2>/dev/null || true\\nchmod +x \\\"$IL2CPP_DIR\\\"/*.dylib 2>/dev/null || true";
        if (!content.Contains("xattr -rd com.apple.quarantine"))
        {
            content = content.Replace(
                "\\\"$IL2CPP_DIR/bee_backend/mac-$HOST_ARCH_BEE/bee_backend\\\"\\n\\nARGS=(",
                "\\\"$IL2CPP_DIR/bee_backend/mac-$HOST_ARCH_BEE/bee_backend\\\"" + quarantineCmd + "\\n\\nARGS=(");
            changed = true;
        }

        if (!content.Contains("HOST_ARCH=\\\"x86_64\\\""))
        {
            content = content.Replace(
                "HOST_ARCH_BEE=\\\"x64\\\"\\nfi\\n\\nif [ \\\"$ARCHS\\\"",
                "HOST_ARCH_BEE=\\\"x64\\\"\\nfi\\n\\nif [ \\\"$HOST_ARCH\\\" = \\\"arm64\\\" ]; then\\n    HOST_ARCH=\\\"x86_64\\\"\\n    HOST_ARCH_BEE=\\\"x64\\\"\\nfi\\n\\nif [ \\\"$ARCHS\\\"");
            changed = true;
        }

        if (changed)
        {
            File.WriteAllText(pbxPath, content);
            Debug.Log("[iOSBuilder] Patched pbxproj: toolchain auto-detect + quarantine + x86_64 IL2CPP fallback.");
        }
        else
        {
            Debug.Log("[iOSBuilder] pbxproj already patched.");
        }

        string fixScript = Path.Combine(path, "fix_il2cpp.sh");
        File.WriteAllText(fixScript,
            "#!/bin/bash\n" +
            "DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\n" +
            "IL2CPP_DIR=\"$DIR/Il2CppOutputProject/IL2CPP/build/deploy_x86_64\"\n" +
            "echo \"Fixing IL2CPP binaries at $IL2CPP_DIR...\"\n" +
            "xattr -cr \"$IL2CPP_DIR\" 2>/dev/null\n" +
            "chmod +x \"$IL2CPP_DIR/il2cpp\" \"$IL2CPP_DIR/il2cpp-compile\" 2>/dev/null\n" +
            "chmod +x \"$IL2CPP_DIR/\"*.dylib 2>/dev/null\n" +
            "chmod +x \"$IL2CPP_DIR/bee_backend/mac-x64/bee_backend\" 2>/dev/null\n" +
            "echo \"Done. Now open Xcode and build.\"\n");
        Debug.Log("[iOSBuilder] Wrote fix_il2cpp.sh.");
    }
}