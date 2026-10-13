using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;

namespace Liminal.SDK.Build
{
    /// <summary>Batch-mode entry points for checking the SDK's Quest configuration and Android player.</summary>
    public static class QuestBuildValidation
    {
        // Run configuration in a separate Unity invocation: define changes require a domain reload.
        public static void Configure()
        {
            RequireAndroidTarget();
            XRSettingsConfigurator.ConfigureOpenXRForAndroidAndStandalone();
            AssetDatabase.SaveAssets();
            Debug.Log("[Liminal.Quest] OpenXR configuration saved.");
        }

        public static void Validate()
        {
            RequireAndroidTarget();
            if (!XRSettingsConfigurator.IsOpenXRConfiguredForAndroidAndStandalone())
                throw new BuildFailedException("Liminal OpenXR settings are incomplete. Run QuestBuildValidation.Configure first.");
            if (!PlayerSettingsConfigurator.IsAndroidConfigured())
                throw new BuildFailedException("Liminal Android settings are incomplete. Run Configure Android in the Liminal setup window.");

            var issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
            foreach (var issue in issues)
                Debug.LogWarning("[Liminal.Quest] OpenXR validation: " + issue.message);
            if (issues.Any(issue => issue.error))
                throw new BuildFailedException("OpenXR project validation reported blocking errors.");

            Debug.Log("[Liminal.Quest] Android ARM64 / IL2CPP and OpenXR validation passed.");
        }

        /// <summary>
        /// Builds the supplied scene to an APK. Pass -liminalSmokeScene Assets/...unity and
        /// -liminalSmokeOutput /absolute/path.apk. Does not install or launch on a device.
        /// </summary>
        public static void BuildSmokePlayer()
        {
            Validate();
            var scene = Argument("-liminalSmokeScene");
            var output = Argument("-liminalSmokeOutput");
            if (string.IsNullOrEmpty(scene) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new BuildFailedException("Supply an existing scene with -liminalSmokeScene.");
            if (string.IsNullOrEmpty(output) || !output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Supply an APK path with -liminalSmokeOutput.");

            output = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var oldBundle = EditorUserBuildSettings.buildAppBundle;
            var oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            var oldIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            var excludedFixtures = new List<PluginImporter>();
            try
            {
                if (Environment.GetCommandLineArgs().Contains("-liminalExcludeLegacyFixtures"))
                {
                    // The repository's old limapp binaries are content fixtures, not SDK code.
                    // Exclude them only for this explicitly requested SDK smoke build. Their
                    // importer settings and bytes remain unchanged for normal simulator builds.
                    foreach (var importer in PluginImporter.GetAllImporters())
                    {
                        if (!importer.assetPath.StartsWith("Assets/TestApp/DLLs/", StringComparison.Ordinal))
                            continue;
                        importer.SetIncludeInBuildDelegate(_ => false);
                        excludedFixtures.Add(importer);
                    }
                    Debug.Log($"[Liminal.Quest] SDK-only smoke build: excluding {excludedFixtures.Count} legacy experience DLLs.");
                }
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                // A smoke APK must not replace the platform application on a headset.
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.liminalvr.sdk.smoketest");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development,
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"Quest smoke build failed with {report.summary.totalErrors} errors.");
                Debug.Log($"[Liminal.Quest] Smoke APK built: {output}");
            }
            finally
            {
                foreach (var importer in excludedFixtures)
                    importer.SetIncludeInBuildDelegate(null);
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldIdentifier);
            }
        }

        private static void RequireAndroidTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Start Unity with -buildTarget Android for Quest validation.");
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
