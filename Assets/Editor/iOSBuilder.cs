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

        int shellStart = content.IndexOf("shellScript = \"mkdir");
        if (shellStart >= 0)
        {
            int shellEnd = content.IndexOf("\";", shellStart) + 2;
            string oldShell = content.Substring(shellStart, shellEnd - shellStart);
            string newShell = "shellScript = \"cd \\\"$PROJECT_DIR\\\" && mkdir -p \\\"$CONFIGURATION_TEMP_DIR/artifacts/arm64/buildstate\\\" && make\\n\";";
            content = content.Replace(oldShell, newShell);
            changed = true;
        }

        if (changed)
        {
            File.WriteAllText(pbxPath, content);
        }

        string makefile = Path.Combine(path, "Makefile");
        if (!File.Exists(makefile))
        {
            File.WriteAllText(makefile,
                "# Generated - compiles IL2CPP C++ to libGameAssembly.a using Xcode's clang++\n" +
                "XCODE_DEVELOPER := $(shell xcode-select -p)\n" +
                "SDK_PATH := $(shell xcrun --sdk iphoneos --show-sdk-path)\n" +
                "CLANG := $(XCODE_DEVELOPER)/Toolchains/XcodeDefault.xctoolchain/usr/bin/clang++\n" +
                "\n" +
                "SRC_DIR := Il2CppOutputProject/Source/il2cppOutput\n" +
                "OBJ_DIR := Il2CppTempDirArtifacts/Release/objs\n" +
                "LIB_DIR := Libraries\n" +
                "IL2CPP_DIR := Il2CppOutputProject/IL2CPP\n" +
                "\n" +
                "SOURCES := $(wildcard $(SRC_DIR)/*.cpp)\n" +
                "OBJECTS := $(patsubst $(SRC_DIR)/%.cpp,$(OBJ_DIR)/%.o,$(SOURCES))\n" +
                "\n" +
                "CFLAGS := -arch arm64 -isysroot $(SDK_PATH) -miphoneos-version-min=12.0 -std=c++17 -stdlib=libc++ \\\n" +
                "  -fno-exceptions -fno-rtti -O2 -DNDEBUG \\\n" +
                "  -I$(SRC_DIR) -I$(IL2CPP_DIR)/libil2cpp -I$(IL2CPP_DIR)/libil2cpp/codegen \\\n" +
                "  -I$(IL2CPP_DIR)/libil2cpp/os -I$(IL2CPP_DIR)/libil2cpp/os/Posix -I$(IL2CPP_DIR)/libil2cpp/os/c-api \\\n" +
                "  -I$(IL2CPP_DIR)/libil2cpp/utils -I$(IL2CPP_DIR)/libil2cpp/vm -I$(IL2CPP_DIR)/libil2cpp/vm-utils \\\n" +
                "  -I$(IL2CPP_DIR)/libil2cpp/metadata -I$(IL2CPP_DIR)/libil2cpp/gc \\\n" +
                "  -I$(IL2CPP_DIR)/libil2cpp/mono -I$(IL2CPP_DIR)/libmono -I$(IL2CPP_DIR)/external \\\n" +
                "  -I$(LIB_DIR) -I$(LIB_DIR)/baselib/Include\n" +
                "\n" +
                ".PHONY: all\n" +
                "all: $(CONFIGURATION_BUILD_DIR)/libGameAssembly.a\n" +
                "$(OBJ_DIR): ; mkdir -p $(OBJ_DIR)\n" +
                "$(CONFIGURATION_BUILD_DIR)/libGameAssembly.a: $(OBJ_DIR) $(OBJECTS)\n" +
                "\tar rcs $@ $(OBJECTS)\n" +
                "$(OBJ_DIR)/%.o: $(SRC_DIR)/%.cpp\n" +
                "\t$(CLANG) $(CFLAGS) -c $< -o $@\n");
        }

        Debug.Log("[iOSBuilder] Replaced IL2CPP shell script with Xcode-native clang++ compilation (Makefile).");
    }
}