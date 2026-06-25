using System;
using Liminal.SDK.Editor.Build;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// The window to export and build the limapp
    /// </summary>
    public class BuildWindow : BaseWindowDrawer
    {
        private int _selectedBuildTarget;

        public override void Draw(BuildWindowConfig config)
        {
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Build Limapp");
                EditorGUILayout.LabelField("This process will build a limapp file that will run on the Liminal Platform");
                EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);

                DrawSceneSelector(ref _scenePath, "Target Scene", config);

                config.TargetScene = _scenePath;
                EditorGUILayout.Space();

                var target = string.Empty;

                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.Android:
                        target = "Android";
                        break;
                    case BuildTarget.StandaloneWindows:
                        target = "Windows Standalone | Emulator | Editor";
                        break;
                    default:
                        target = "UNSUPPORTED PLATFORM";
                        break;
                }

                var buildTargetNames = new[] { $"Current ({target})", "Standalone Windows", "Android" };
                var sizes = new[] { 0, 1, 2 };


                _selectedPlatform = config.SelectedPlatform;
                _selectedBuildTarget = (int)config.SelectedPlatform;
                _selectedBuildTarget = EditorGUILayout.IntPopup("Build Target: ", _selectedBuildTarget, buildTargetNames, sizes);
                _selectedPlatform = (BuildPlatform)_selectedBuildTarget;

                config.SelectedPlatform = _selectedPlatform;

                _validation = RunValidation();
                DrawValidationPanel(_validation);

                EditorGUILayout.EndVertical();
            }
        }

        public override void DrawFooter(BuildWindowConfig config)
        {
            LiminalUserSettings.AutoCopyAfterBuild = EditorGUILayout.ToggleLeft(
                new GUIContent("Copy to StreamingAssets after Build",
                    "When on, a finished build's scene bundle is copied into the configured targets' " +
                    "StreamingAssets/Limapps folders. Configure targets in the Settings tab."),
                LiminalUserSettings.AutoCopyAfterBuild);

            LiminalUserSettings.AutoCopyDllAfterBuild = EditorGUILayout.ToggleLeft(
                new GUIContent("Copy DLL after Build",
                    "When on, the built app DLL is mirrored into the configured DLL destination folders. " +
                    "Independent of the StreamingAssets copy above."),
                LiminalUserSettings.AutoCopyDllAfterBuild);

            S3UploaderSettings.AutoUploadAfterBuild = EditorGUILayout.ToggleLeft(
                new GUIContent("Upload to S3 after Build",
                    "When on, the finished build's Android + Standalone zips are uploaded to S3 using the " +
                    "Upload tab's credentials/settings (Standalone falls back to the Android build). Off by default."),
                S3UploaderSettings.AutoUploadAfterBuild);

            GUI.enabled = !EditorApplication.isCompiling && _validation.CanBuild;
            var buildClicked = GUILayout.Button("Build", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f));
            GUI.enabled = !EditorApplication.isCompiling;

            if (!buildClicked)
                return;

            var buildTargetOkay = true;
            switch (_selectedPlatform)
            {
                case BuildPlatform.GearVR when EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android:
                case BuildPlatform.Standalone when EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows:
                    buildTargetOkay = false;
                    break;
            }

            if (!buildTargetOkay)
            {
                if (!EditorUtility.DisplayDialog("Platform Issue Detected",
                        "Outstanding platform issues have been detected in your project. " +
                        "To reduce build time, ensure your current build target matches your selected platform",
                        "Build Anyway", "Cancel Build"))
                {
                    return;
                }
            }

            Build();
        }

        private void Build()
        {
            SettingsUtils.CopyProjectSettingsToProfile();
            EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Single);

            switch (_selectedPlatform)
            {
                case BuildPlatform.Current:
                    AppBuilder.BuildCurrentPlatform();
                    break;

                case BuildPlatform.GearVR:
                    AppBuilder.BuildLimapp(BuildTarget.Android, AppBuildInfo.BuildTargetDevices.GearVR);
                    break;

                case BuildPlatform.Standalone:
                    AppBuilder.BuildLimapp(BuildTarget.StandaloneWindows, AppBuildInfo.BuildTargetDevices.Emulator);
                    break;
            }
        }

        public void DrawSceneSelector(ref string scenePath, string name, BuildWindowConfig config)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, GUILayout.Width(Size.x * 0.2F));

                if (AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset)) != null)
                {
                    _targetScene = (SceneAsset) AssetDatabase.LoadAssetAtPath(config.TargetScene, typeof(SceneAsset));
                }

                _targetScene = (SceneAsset)EditorGUILayout.ObjectField(_targetScene, typeof(SceneAsset), true, GUILayout.Width(Size.x * 0.75F));

                if (_targetScene != null)
                {
                    scenePath = AssetDatabase.GetAssetPath(_targetScene);
                }
                else
                {
                    _targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneManager.GetActiveScene().path);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private BuildPlatform _selectedPlatform;
        private SceneAsset _targetScene;
        private string _scenePath = string.Empty;
        private BuildValidation _validation;

        private BuildValidation RunValidation()
        {
            var v = new BuildValidation();

            v.SceneOk = !string.IsNullOrEmpty(_scenePath);
            v.SceneDetail = v.SceneOk ? _scenePath : "No scene selected";

            AppManifest manifest = null;
            try
            {
                manifest = AppBuilder.ReadAppManifest();
                v.ManifestOk = manifest.Id > 0;
                v.ManifestDetail = v.ManifestOk
                    ? $"Id={manifest.Id}, Name={manifest.Name}"
                    : "Manifest Id is 0 — run 'Liminal > Create or Update App Manifest'";
            }
            catch (Exception ex)
            {
                v.ManifestOk = false;
                v.ManifestDetail = ex.Message;
            }

            var asmName = manifest?.Name;
            v.AsmName = asmName;

            if (!string.IsNullOrEmpty(asmName))
            {
                var asmdefPath = Path.Combine(Application.dataPath, asmName + ".asmdef").Replace('\\', '/');
                v.AsmdefOk = File.Exists(asmdefPath);
                v.AsmdefDetail = v.AsmdefOk
                    ? asmdefPath
                    : "Missing — click 'Setup Limapp Build' to create";

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var dllPath = Path.Combine(projectRoot, "Library/Bee/PlayerScriptAssemblies", asmName + ".dll").Replace('\\', '/');
                if (File.Exists(dllPath))
                {
                    v.PlayerDllOk = true;
                    var info = new FileInfo(dllPath);
                    v.PlayerDllDetail = $"{dllPath}  ({info.Length / 1024} KB, {info.LastWriteTime:g})";
                }
                else
                {
                    v.PlayerDllOk = false;
                    v.PlayerDllDetail = $"Not found at {dllPath} — Build will offer to compile it on demand";
                }
            }
            else
            {
                v.AsmdefDetail = "Manifest must be valid first";
                v.PlayerDllDetail = "Manifest must be valid first";
            }

            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            v.TargetOk = true;
            v.TargetDetail = $"Active: {activeTarget}, Selected: {_selectedPlatform}";
            switch (_selectedPlatform)
            {
                case BuildPlatform.GearVR when activeTarget != BuildTarget.Android:
                case BuildPlatform.Standalone when activeTarget != BuildTarget.StandaloneWindows:
                    v.TargetOk = false;
                    v.TargetDetail += " — switch active target to avoid a slow re-compile";
                    break;
            }

            v.CompileOk = !EditorApplication.isCompiling;
            v.CompileDetail = v.CompileOk ? "Idle" : "Compilation in progress — wait before building";

            return v;
        }

        private static void DrawValidationPanel(BuildValidation v)
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawCheck("Scene assigned", v.SceneOk, v.SceneDetail);
            DrawCheck("App manifest", v.ManifestOk, v.ManifestDetail);

            Action setupFix = (!v.AsmdefOk && v.ManifestOk) ? (Action)LimapPatchWindow.SetupLimappBuild : null;
            DrawCheck("Root asmdef", v.AsmdefOk, v.AsmdefDetail, fixLabel: "Setup", onFix: setupFix);

            // Offer the compile action whenever the manifest/asmdef are valid, even when the DLL already
            // exists, so a stale player DLL (e.g. one that still contains a since-deleted script) can be
            // force-rebuilt on demand rather than being trusted because it merely exists.
            Action compileFix = null;
            if (v.ManifestOk && v.AsmdefOk && !string.IsNullOrEmpty(v.AsmName))
            {
                var asmName = v.AsmName;
                compileFix = () =>
                {
                    try
                    {
                        AppBuilder.EnsurePlayerDll(asmName);
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogError($"[Liminal.Build] Failed to compile player DLL: {ex.Message}");
                    }
                };
            }
            DrawCheck("Player DLL (no UNITY_EDITOR)", v.PlayerDllOk, v.PlayerDllDetail,
                fixLabel: v.PlayerDllOk ? "Recompile" : "Compile", onFix: compileFix);

            DrawCheck("Build target", v.TargetOk, v.TargetDetail, warningOnly: true);
            DrawCheck("Scripts not compiling", v.CompileOk, v.CompileDetail);
            EditorGUI.indentLevel--;
        }

        private static void DrawCheck(string label, bool ok, string detail, bool warningOnly = false, string fixLabel = null, Action onFix = null)
        {
            var iconName = ok ? "TestPassed" : (warningOnly ? "TestIgnored" : "TestFailed");
            var icon = EditorGUIUtility.IconContent(iconName);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField(label, GUILayout.Width(220));
            EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
            // Render the fix button whenever an action is supplied (not only on failure) so passing checks
            // can still expose an on-demand action such as a forced player DLL recompile.
            if (onFix != null && !string.IsNullOrEmpty(fixLabel))
            {
                if (GUILayout.Button(fixLabel, GUILayout.Width(80)))
                    onFix();
            }
            EditorGUILayout.EndHorizontal();
        }

        private struct BuildValidation
        {
            public string AsmName;
            public bool SceneOk;
            public string SceneDetail;
            public bool ManifestOk;
            public string ManifestDetail;
            public bool AsmdefOk;
            public string AsmdefDetail;
            public bool PlayerDllOk;
            public string PlayerDllDetail;
            public bool TargetOk;
            public string TargetDetail;
            public bool CompileOk;
            public string CompileDetail;

            public bool CanBuild => SceneOk && ManifestOk && AsmdefOk && CompileOk;
        }
    }

    public enum BuildPlatform
    {
        Current,
        Standalone,
        GearVR
    }
}
