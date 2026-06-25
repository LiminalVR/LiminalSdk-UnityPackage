using System;
using System.IO;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Build;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Liminal.SDK.Build
{
    public class LiminalConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, new GUIContent("App Settings"), true);
        }
    }

    [CustomPropertyDrawer(typeof(LiminalConfig))]
    public class ProjectSettingsPropertyDrawer : PropertyDrawer
    {
        private Rect _current;
        private Rect _first;

        private float _singleLine = EditorGUIUtility.singleLineHeight;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var overrideProp = property.FindPropertyRelative("OverrideProfile");
            var size = overrideProp.boolValue ? 3 : 2;
            if (property.isExpanded)
                return _singleLine * size;

            return _singleLine;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            position.height = height;
            _first = position;
            _current = position;
            var row1 = position;

            EditorGUI.PropertyField(row1, property, new GUIContent("Project Settings"), includeChildren: false);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var overrideProp = property.FindPropertyRelative("OverrideProfile");
                var profileProp = property.FindPropertyRelative("ProfileToApply");

                EditorGUI.PropertyField(NextRow(width: position.width * 0.9f), overrideProp);

                var useProfile = overrideProp.boolValue;

                if (GUI.changed && !useProfile)
                    profileProp.objectReferenceValue = null;

                if (useProfile)
                {

                    if (GUI.changed)
                    {
                        profileProp.objectReferenceValue = SettingsUtils.CreateProfile();
                        SettingsUtils.CopyProjectSettingsToProfile();
                    }

                    EditorGUI.PropertyField(NextRow(width: position.width * 0.9f), profileProp, includeChildren: false);

                    if (profileProp.objectReferenceValue == null && SettingsUtils.HasFile)
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("Find")))
                        {
                            var profile = Resources.Load<ExperienceProfile>("LimappConfig");
                            profileProp.objectReferenceValue = profile;
                        }
                    }
                    else if (profileProp.objectReferenceValue == null)
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("Add")))
                        {
                            SettingsUtils.CreateProfile();
                            var profile =
                                AssetDatabase.LoadAssetAtPath<ExperienceProfile>(
                                    $"{SDKResourcesConsts.LiminalSettingsConfigPath}");
                            profileProp.objectReferenceValue = profile;
                        }
                    }
                    else
                    {
                        if (GUI.Button(NextColumn(expand: true), new GUIContent("X")))
                            profileProp.objectReferenceValue = null;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        public Rect NextRow(float? height = null, float? width = null)
        {
            if (height == null)
                height = _first.height;

            if (width == null)
                width = _first.width;

            var next = _current;
            next.width = width.Value;
            next.height = height.Value;
            next.y += _current.height;
            
            next.x = _first.x;

            _current = next;
            return next;
        }

        public Rect NextColumn(float? height = null, float? width = null, bool expand = false)
        {
            if (height == null)
                height = _first.height;

            if (width == null)
                width = _first.width;

            if(expand)
                width = _first.width - _current.width;

            var next = _current;
            next.width = width.Value;
            next.height = height.Value;
            next.x += _current.width;

            _current = next;
            return next;
        }
    }

    public class SettingsWindow : BaseWindowDrawer
    {
        public override void Draw(BuildWindowConfig config)
        {
            GUILayout.Space(4);
            DrawLinks(config);

            GUILayout.Space(12);
            DrawDeploy(config);

            GUILayout.Space(12);
            DrawExperienceSettings();
        }

        private static void DrawLinks(BuildWindowConfig config)
        {
            DrawSectionHeader(
                "Links",
                "Local paths to the Liminal SDK package and the Platform App Unity project. Shared across every limapp project on this machine.");

            // Auto-populate the SDK path on first draw so the user has a sensible default they can override.
            if (string.IsNullOrEmpty(LiminalUserSettings.LiminalSdkPath))
                LiminalUserSettings.LiminalSdkPath = ResolveSdkPath();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawPathRow("Liminal SDK", LiminalUserSettings.LiminalSdkPath, p => LiminalUserSettings.LiminalSdkPath = p);
                DrawPathRow("Platform App", LiminalUserSettings.PlatformAppProjectPath, p => LiminalUserSettings.PlatformAppProjectPath = p);
            }
            GUILayout.EndVertical();
        }

        private static void DrawDeploy(BuildWindowConfig config)
        {
            DrawSectionHeader(
                "Deploy",
                "Copies the latest build into StreamingAssets/Limapps/<Platform>/<id>/. Both Android and Standalone are copied — Standalone falls back to Android when no Standalone build exists.");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                LiminalUserSettings.AutoCopyAfterBuild = EditorGUILayout.ToggleLeft(
                    new GUIContent("Auto Copy StreamingAssets after Build", "Copy the built scene bundle into each configured target's StreamingAssets/Limapps folder when a build finishes."),
                    LiminalUserSettings.AutoCopyAfterBuild);

                LiminalUserSettings.AutoCopyDllAfterBuild = EditorGUILayout.ToggleLeft(
                    new GUIContent("Auto Copy DLL after Build", "Mirror the built app DLL into each configured target's DLL destination folder when a build finishes. Independent of the StreamingAssets copy."),
                    LiminalUserSettings.AutoCopyDllAfterBuild);

                LiminalUserSettings.RevealInFinderAfterBuild = EditorGUILayout.ToggleLeft(
                    new GUIContent("Reveal in Finder after Build", "Open the build output folder when a build finishes."),
                    LiminalUserSettings.RevealInFinderAfterBuild);
            }
            GUILayout.EndVertical();

            GUILayout.Space(6);

            DrawDeployTarget(
                title: "To SDK",
                basePath: LiminalUserSettings.LiminalSdkPath,
                basePathLabel: "Liminal SDK",
                streamingAssetsRoot: ResolveSdkStreamingAssetsRoot(LiminalUserSettings.LiminalSdkPath),
                dllFolder: LiminalUserSettings.SdkDllDestinationFolder,
                onDllFolderChanged: p => LiminalUserSettings.SdkDllDestinationFolder = p,
                config: config);

            GUILayout.Space(6);

            DrawDeployTarget(
                title: "To Platform App",
                basePath: LiminalUserSettings.PlatformAppProjectPath,
                basePathLabel: "Platform App",
                streamingAssetsRoot: ResolvePlatformAppStreamingAssetsRoot(LiminalUserSettings.PlatformAppProjectPath),
                dllFolder: LiminalUserSettings.PlatformAppDllDestinationFolder,
                onDllFolderChanged: p => LiminalUserSettings.PlatformAppDllDestinationFolder = p,
                config: config);
        }

        private static void DrawDeployTarget(
            string title,
            string basePath,
            string basePathLabel,
            string streamingAssetsRoot,
            string dllFolder,
            Action<string> onDllFolderChanged,
            BuildWindowConfig config)
        {
            var basePathOk = !string.IsNullOrEmpty(basePath) && Directory.Exists(basePath);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (!basePathOk)
                    GUILayout.Label($"Set '{basePathLabel}' to enable", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                using (new EditorGUI.DisabledScope(!basePathOk))
                {
                    DrawDestinationRow("StreamingAssets", basePathOk ? streamingAssetsRoot : null);
                    DrawPathRow("Mirror DLL to", dllFolder, onDllFolderChanged, hintWhenEmpty: "optional");

                    GUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Copy Latest Build", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.3f)))
                        CopyLatestBuildTo(streamingAssetsRoot, dllFolder, config);

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(dllFolder)))
                    {
                        if (GUILayout.Button(
                                new GUIContent("Copy DLL", "Copy only the app DLL into the 'Mirror DLL to' folder, skipping StreamingAssets."),
                                GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.3f),
                                GUILayout.Width(90)))
                            CopyLatestBuildTo(streamingAssetsRoot, dllFolder, config, copyStreamingAssets: false, copyDll: true);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        private static void DrawSectionHeader(string title, string description)
        {
            EditorGUIHelper.DrawTitle(title);
            if (!string.IsNullOrEmpty(description))
            {
                GUILayout.Space(2);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(4);
            }
        }

        /// <summary>
        /// Read-only "→ &lt;path&gt;" row that previews where a deploy action will write to.
        /// </summary>
        private static void DrawDestinationRow(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var display = string.IsNullOrEmpty(path) ? "(unavailable — set the parent path above)" : "→  " + path.Replace('\\', '/');
            EditorGUILayout.SelectableLabel(display, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawExperienceSettings()
        {
            DrawSectionHeader(
                "Experience Settings",
                "Inspector for the ExperienceApp in the active scene.");

            var experienceApp = GameObject.FindObjectOfType<ExperienceApp>();
            if (experienceApp == null)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    "No ExperienceApp in the active scene. Open the limapp scene to edit its settings.",
                    EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            var app = UnityEditor.Editor.CreateEditor(experienceApp);
            app.DrawDefaultInspector();
            GUILayout.EndVertical();
        }

        private static void DrawPathRow(string label, string path, Action<string> onChanged, string hintWhenEmpty = null)
        {
            EditorGUILayout.BeginHorizontal();

            var labelContent = string.IsNullOrEmpty(hintWhenEmpty) || !string.IsNullOrEmpty(path)
                ? new GUIContent(label)
                : new GUIContent($"{label}  ({hintWhenEmpty})");
            GUILayout.Label(labelContent, GUILayout.Width(110));

            var newPath = EditorGUILayout.TextField(path ?? string.Empty);
            if (newPath != path)
            {
                path = newPath;
                onChanged?.Invoke(path);
            }

            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                var picked = EditorUtility.OpenFolderPanel($"Pick {label} folder", path ?? string.Empty, string.Empty);
                if (!string.IsNullOrEmpty(picked))
                {
                    path = picked.Replace('\\', '/');
                    onChanged?.Invoke(path);
                    GUIUtility.keyboardControl = 0;
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path) || !Directory.Exists(path)))
            {
                if (GUILayout.Button("Reveal", GUILayout.Width(60)))
                    EditorUtility.RevealInFinder(path.TrimEnd('/', '\\') + Path.DirectorySeparatorChar);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string ResolveSdkPath()
        {
            var package = PackageInfo.FindForAssembly(typeof(AppBuilder).Assembly);
            return package?.resolvedPath?.Replace('\\', '/') ?? string.Empty;
        }

        /// <summary>SDK package lives at <c>&lt;host&gt;/Assets/Liminal-SDK/</c>; StreamingAssets sits at
        /// <c>&lt;host&gt;/Assets/StreamingAssets/Limapps/</c>, one directory up.</summary>
        private static string ResolveSdkStreamingAssetsRoot(string sdkPath)
        {
            if (string.IsNullOrEmpty(sdkPath))
                return string.Empty;
            var parent = Path.GetDirectoryName(sdkPath.TrimEnd('/', '\\'));
            return string.IsNullOrEmpty(parent)
                ? string.Empty
                : Path.Combine(parent, "StreamingAssets", "Limapps");
        }

        /// <summary>Platform App path is the Unity project root, so StreamingAssets is at
        /// <c>&lt;root&gt;/Assets/StreamingAssets/Limapps/</c>.</summary>
        private static string ResolvePlatformAppStreamingAssetsRoot(string platformAppPath)
        {
            if (string.IsNullOrEmpty(platformAppPath))
                return string.Empty;
            return Path.Combine(platformAppPath, "Assets", "StreamingAssets", "Limapps");
        }

        /// <summary>
        /// Runs Copy Latest Build for every deploy target whose base path is currently valid.
        /// Used by the Build button so a successful build is immediately staged into the configured
        /// destinations. Silent when no build output exists yet (e.g. the build was for a single
        /// platform) — exceptions still surface via the standard error dialog.
        /// </summary>
        public static void CopyLatestBuildToAllConfiguredTargets(bool copyStreamingAssets = true, bool copyDll = true)
        {
            // Settings tab auto-populates this on first draw; if the user goes straight from a fresh
            // install to the Build button, it's still empty here. Resolve once so the SDK target
            // works without requiring a Settings-tab visit first.
            if (string.IsNullOrEmpty(LiminalUserSettings.LiminalSdkPath))
                LiminalUserSettings.LiminalSdkPath = ResolveSdkPath();

            var sdkPath = LiminalUserSettings.LiminalSdkPath;
            Debug.Log($"[Liminal.Deploy] Auto-copy after build. SDK path='{sdkPath}', PlatformApp path='{LiminalUserSettings.PlatformAppProjectPath}'");

            if (!string.IsNullOrEmpty(sdkPath) && Directory.Exists(sdkPath))
            {
                var streamingRoot = ResolveSdkStreamingAssetsRoot(sdkPath);
                Debug.Log($"[Liminal.Deploy] Copying to SDK target → {streamingRoot}");
                CopyLatestBuildTo(
                    streamingRoot,
                    LiminalUserSettings.SdkDllDestinationFolder,
                    config: null,
                    silentIfMissing: true,
                    copyStreamingAssets: copyStreamingAssets,
                    copyDll: copyDll);
            }
            else
            {
                Debug.Log($"[Liminal.Deploy] Skipping SDK target — path empty or missing on disk.");
            }

            var platformAppPath = LiminalUserSettings.PlatformAppProjectPath;
            if (!string.IsNullOrEmpty(platformAppPath) && Directory.Exists(platformAppPath))
            {
                var streamingRoot = ResolvePlatformAppStreamingAssetsRoot(platformAppPath);
                Debug.Log($"[Liminal.Deploy] Copying to Platform App target → {streamingRoot}");
                CopyLatestBuildTo(
                    streamingRoot,
                    LiminalUserSettings.PlatformAppDllDestinationFolder,
                    config: null,
                    silentIfMissing: true,
                    copyStreamingAssets: copyStreamingAssets,
                    copyDll: copyDll);
            }
            else
            {
                Debug.Log($"[Liminal.Deploy] Skipping Platform App target — path empty or missing on disk.");
            }
        }

        private static void CopyLatestBuildTo(string streamingAssetsRoot, string dllFolder, BuildWindowConfig config, bool silentIfMissing = false, bool copyStreamingAssets = true, bool copyDll = true)
        {
            if (!copyStreamingAssets && !copyDll)
                return;

            try
            {
                var manifest = AppBuilder.ReadAppManifest();
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var idStr = manifest.Id.ToString();

                var androidSrc = Path.Combine(projectRoot, "Limapp-output", "Android", idStr);
                var standaloneSrc = Path.Combine(projectRoot, "Limapp-output", "Standalone", idStr);

                var hasAndroid = Directory.Exists(androidSrc);
                var hasStandalone = Directory.Exists(standaloneSrc);

                if (!hasAndroid && !hasStandalone)
                {
                    if (silentIfMissing)
                    {
                        Debug.LogWarning($"[Liminal.Deploy] No build output found at {androidSrc} or {standaloneSrc} — skipping copy.");
                        return;
                    }

                    EditorUtility.DisplayDialog(
                        "Copy Latest Build",
                        $"No build was found for either platform:\n{androidSrc}\n{standaloneSrc}\n\nRun the Build tab first.",
                        "OK");
                    return;
                }

                // Standalone falls back to Android when no Standalone build exists. Android does not
                // fall back to Standalone — if only Standalone was built, the Android slot is skipped.
                var androidEffective = hasAndroid ? androidSrc : null;
                var standaloneEffective = hasStandalone ? standaloneSrc : (hasAndroid ? androidSrc : null);

                if (copyStreamingAssets)
                {
                    if (string.IsNullOrEmpty(streamingAssetsRoot))
                        throw new Exception("StreamingAssets destination could not be resolved.");

                    if (androidEffective != null)
                        CopyOutput("Android", androidEffective, streamingAssetsRoot, manifest);

                    if (standaloneEffective != null)
                        CopyOutput("Standalone", standaloneEffective, streamingAssetsRoot, manifest);
                }

                // Single flat DLL copy. Copy whichever platform's App DLL was built most recently so a
                // stale output for the *other* platform can't shadow the build you just made (e.g. an old
                // Standalone build silently overriding a fresh Android one). Falls back to the previous
                // Standalone-preference only when neither candidate DLL is present, so CopyDll still emits
                // its 'not found' warning.
                if (copyDll && !string.IsNullOrEmpty(dllFolder))
                {
                    var dllSrcFolder = NewestAssemblyFolder(manifest.Name, standaloneSrc, androidSrc)
                        ?? standaloneEffective ?? androidEffective;
                    if (dllSrcFolder != null)
                        CopyDll(dllSrcFolder, dllFolder, manifest);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Copy Latest Build", $"Copy failed: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Copies the unzip-folder contents into <c>&lt;streamingAssetsRoot&gt;/&lt;Platform&gt;/&lt;id&gt;/</c>
        /// (wiping any prior copy at that location) and drops a PluginImporter meta on the bundled DLL so
        /// the destination Unity project imports it with Auto Reference off.
        /// </summary>
        private static void CopyOutput(string platformName, string srcFolder, string streamingAssetsRoot, AppManifest manifest)
        {
            var dstFolder = Path.Combine(streamingAssetsRoot, platformName, manifest.Id.ToString());
            if (Directory.Exists(dstFolder))
                Directory.Delete(dstFolder, recursive: true);
            CopyDirectory(srcFolder, dstFolder);
            Debug.Log($"[Deploy] StreamingAssets ({platformName}): '{srcFolder}' → '{dstFolder}'.");

            var bundledDll = Path.Combine(dstFolder, "assemblyFolder", manifest.Name + ".dll");
            if (File.Exists(bundledDll))
                WritePluginMeta(bundledDll);
        }

        /// <summary>
        /// Returns the candidate build-output folder whose <c>assemblyFolder/&lt;asmName&gt;.dll</c> was
        /// written most recently, or <c>null</c> if none of the candidates contain that DLL. Used to copy
        /// the freshest platform's App DLL rather than blindly preferring one platform. On equal timestamps
        /// the earliest candidate wins, so pass the preferred platform first.
        /// </summary>
        private static string NewestAssemblyFolder(string asmName, params string[] candidateFolders)
        {
            string newest = null;
            var newestTime = DateTime.MinValue;
            foreach (var folder in candidateFolders)
            {
                if (string.IsNullOrEmpty(folder))
                    continue;

                var dll = Path.Combine(folder, "assemblyFolder", asmName + ".dll");
                if (!File.Exists(dll))
                    continue;

                var written = File.GetLastWriteTimeUtc(dll);
                if (newest == null || written > newestTime)
                {
                    newest = folder;
                    newestTime = written;
                }
            }
            return newest;
        }

        private static void CopyDll(string srcFolder, string dllFolder, AppManifest manifest)
        {
            var srcDll = Path.Combine(srcFolder, "assemblyFolder", manifest.Name + ".dll");
            if (!File.Exists(srcDll))
            {
                Debug.LogWarning($"[Deploy] DLL not found at '{srcDll}' — skipping DLL copy.");
                return;
            }

            Directory.CreateDirectory(dllFolder);
            var dstDll = Path.Combine(dllFolder, manifest.Name + ".dll");
            File.Copy(srcDll, dstDll, overwrite: true);
            WritePluginMeta(dstDll);
            Debug.Log($"[Deploy] DLL: '{srcDll}' → '{dstDll}'.");
        }

        /// <summary>
        /// Writes a PluginImporter meta alongside the copied DLL so the destination Unity project imports
        /// it with Auto Reference disabled. The GUID is derived from the destination path itself, so two
        /// different destinations never collide and re-deploys to the same location keep the same GUID
        /// (any asmdef references pointing at the DLL stay resolved).
        /// </summary>
        private static void WritePluginMeta(string dllPath)
        {
            var guid = DeterministicGuid(dllPath.Replace('\\', '/').ToLowerInvariant());
            var content =
                "fileFormatVersion: 2\n" +
                $"guid: {guid}\n" +
                "PluginImporter:\n" +
                "  externalObjects: {}\n" +
                "  serializedVersion: 2\n" +
                "  iconMap: {}\n" +
                "  executionOrder: {}\n" +
                "  defineConstraints: []\n" +
                "  isPreloaded: 0\n" +
                "  isOverridable: 1\n" +
                "  isExplicitlyReferenced: 1\n" +
                "  validateReferences: 1\n" +
                "  platformData:\n" +
                "  - first:\n" +
                "      : Any\n" +
                "    second:\n" +
                "      enabled: 1\n" +
                "      settings: {}\n" +
                "  - first:\n" +
                "      Editor: Editor\n" +
                "    second:\n" +
                "      enabled: 0\n" +
                "      settings:\n" +
                "        DefaultValueInitialized: true\n" +
                "  userData: \n" +
                "  assetBundleName: \n" +
                "  assetBundleVariant: \n";
            File.WriteAllText(dllPath + ".meta", content);
        }

        private static string DeterministicGuid(string seed)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
                var sb = new System.Text.StringBuilder(32);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: true);
            foreach (var dir in Directory.GetDirectories(src))
                CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
        }
    }
}
