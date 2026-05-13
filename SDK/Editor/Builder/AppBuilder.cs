using Liminal.SDK.Build;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Serialization;
using Liminal.SDK.Serialization;
using Liminal.SDK.VR.Avatars;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Provides functionality for building a Liminal app.
    /// </summary>
    public static class AppBuilder
    {
        /// <summary>
        /// The name of the application serialized data file.
        /// </summary>
        private const string AppDataName = "AppData.json";

        /// <summary>
        /// The output name of the application assembly.
        /// </summary>
        private const string AppAssemblyFileName = "AppModule.dll";

        #region Public Interface

        [MenuItem("AssemblyBuilder Example/Build Assembly Sync")]
        public static void BuildAssemblySync()
        {
            var buildInfo = new AppBuildInfo()
            {
                Scene = SceneManager.GetActiveScene(),
                BuildTarget = BuildTarget.StandaloneWindows,
                BuildTargetDevice = AppBuildInfo.BuildTargetDevices.None,
            };

            var app = UnityEngine.Object.FindObjectOfType<ExperienceApp>();
            VerifyAppSceneSetup(app);
            ClearAppData(app);

            var appManifest = ReadAppManifest();
            var asmName = "App" + appManifest.Id.ToString().PadLeft(AppManifest.MaxIdLength, '0');
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);

            var asmPath = GetAppAssemblyPath() + ".bytes";
            EnsureAsmdefCompiledAssembly(asmName, asmPath);
        }

        /// <summary>
        /// Builds a Liminal Experience Application from the current scene and build target.
        /// </summary>
        public static void BuildCurrentPlatform()
        {
            Build(new AppBuildInfo()
            {
                Scene = SceneManager.GetActiveScene(),
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                BuildTargetDevice = AppBuildInfo.BuildTargetDevices.None,
            });
        }

        /// <summary>
        /// Builds a Liminal Experience Application from the current scene and standalone build target.
        /// </summary>
        public static void BuildLimapp(BuildTarget target, AppBuildInfo.BuildTargetDevices devices)
        {
            Build(new AppBuildInfo()
            {
                Scene = SceneManager.GetActiveScene(),
                BuildTarget = target,
                BuildTargetDevice = devices,
            });
        }

        /// <summary>
        /// Lists the assemblies that will be included in the App build.
        /// </summary>
        [MenuItem("Liminal/List Included Assemblies")]
        public static void ListAssemblies()
        {
            var plugins = GetIncludedPlugins(EditorUserBuildSettings.selectedBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget);
            var names = plugins.Select(x => x.assetPath);
            Debug.LogFormat("Assemblies included in App Build ({0}): {1}", plugins.Count(), string.Join("\n", names.ToArray()));
        }

        public static void UpdateProgressBar(string title, string description, float progress)
        {
            EditorUtility.DisplayProgressBar(title, description, progress);
        }

        /// <summary>
        /// Builds a Liminal App.
        /// </summary>
        /// <param name="buildInfo">The build information.</param>
        public static void Build(AppBuildInfo buildInfo)
        {
            if (buildInfo == null)
                throw new ArgumentNullException("buildInfo");

            var assetBundles = AssetDatabase.GetAllAssetBundleNames();
            foreach (var bundle in assetBundles)
            {
                // boolean true forces the asset bundles to be deleted even if they're in use.
                AssetDatabase.RemoveAssetBundleName(bundle, true);
            }

            AssetImporter.GetAtPath(buildInfo.Scene.path).SetAssetBundleNameAndVariant("appscene", "");

            // Get and verify the target platform is supported
            var appPlatform = MapAppTargetPlatform(buildInfo.BuildTarget);
            if (appPlatform == AppPackPlatform.Unknown)
                throw new Exception(string.Format("The supplied buildTarget is currently unsupported: {0}", buildInfo.BuildTarget));

            // Read the app manifest (this will throw if there are errors)
            var appManifest = ReadAppManifest();
            if (appManifest.Id == 0)
            {
                throw new Exception("Application Id is zero");
            }

            UpdateProgressBar("Building Limapp", "Checking Scene", 0.1F);

            // Find the ExperienceApp
            var app = UnityEngine.Object.FindObjectOfType<ExperienceApp>();
            VerifyAppSceneSetup(app);

            Debug.LogFormat("[Liminal.Build] Building app {0}, for platform {1}", appManifest.Id, appPlatform);

            // Clear out existing data fields on the experience
            ClearAppData(app);

            var asmName = "App" + appManifest.Id.ToString().PadLeft(AppManifest.MaxIdLength, '0');
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildInfo.BuildTarget);

            // The runtime master player expects [ExperienceApp] to be disabled in the packed scene so
            // nothing inside it Awakes before the avatar/device is wired up. Toggle off for the bundle
            // build and restore in the finally so editor state is unchanged afterwards.
            var appWasActive = app != null && app.gameObject.activeSelf;
            if (appWasActive)
                app.gameObject.SetActive(false);

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                // Ensure the output directory is ready
                var outputPath = GetOutputPath();
                Directory.CreateDirectory(outputPath);

                // Source the limapp's compiled DLL from the asmdef pipeline (Library/Bee/PlayerScriptAssemblies)
                // and run Cecil post-processing on it. The asmdef must be named after manifest.Name (== asmName),
                // which is what `Liminal > Setup Limapp Build` produces.
                // NOTE: .bytes extension is used so that unity won't try to load the DLL
                var asmPath = GetAppAssemblyPath() + ".bytes";
                EnsureAsmdefCompiledAssembly(asmName, asmPath);
                
                // Build asset lookup for the current scene
                Debug.Log("[Liminal.Build] Building asset lookup...");
                var assetLookupBuilder = new AssetLookupBuilder();
                var assetLookup = assetLookupBuilder.Build(buildInfo.Scene);

                // Serialize scene data
                // This will write any data from classes/structs in the app marked with [Serializable], so that we
                // can deserialize them when loaded into the master app (Unity won't do this with loaded assemblies...)
                Debug.Log("[Liminal.Build] Serializing scene...");
                var serializer = new AppSerializer(new AssemblyDataProvider(asmName), assetLookup);
                var jsonPath = Path.Combine(outputPath, AppDataName);
                var jsonData = serializer.Serialize(buildInfo.Scene, jsonPath);

                UpdateProgressBar("Building Limapp", "Serializing App Data", 0.2F);
                // Assign serialized app data and lookup to the ExperienceApp object
                SetAppData(app, jsonData, assetLookup);

                // Build asset bundles
                UpdateProgressBar("Building Limapp", "Building scene AssetBundle", 0.4F);
                Debug.Log("[Liminal.Build] Building scene AssetBundle...");

                // Try ChunckBase instead.
                BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle, buildInfo.BuildTarget);


                // We need to replace it with addressable
                //var settings = AddressableAssetSettingsDefaultObject.Settings;
                //if (settings == null)
                //    return;

                //var context = new AddressablesDataBuilderInput(settings);
                //settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);



                // Scene Processor, replaces Assembly-Csharp with new AssemblyName.

                // Pack to AppPack (.limapp)
                // This will also compress the app (using LZMA)
                UpdateProgressBar("Building Limapp", "Writing output", 0.7F);
                Debug.Log("[Liminal.Build] Writing output...");

                var appPack = new AppPack()
                {
                    TargetPlatform = appPlatform,
                    ApplicationId = appManifest.Id,
                    Assemblies = BuildPackAssemblyRawBytesList(asmPath, asmName, buildTargetGroup, buildInfo.BuildTarget),
                    SceneBundle = File.ReadAllBytes(Path.Combine(outputPath, "appscene")),
                };

                // Write the limapp's DLL + AssetBundle to Limapp-output/<Platform>/<Id>/ and zip it.
                // The IL2CPP master player drops the DLL in directly, so the legacy compressed .limapp
                // wrapper is no longer produced.
                var resolvedPlatformName = appPlatform == AppPackPlatform.Android ? "Android" : "Standalone";
                var zipPath = WriteOutputZip(appPack, resolvedPlatformName);

                UpdateProgressBar("Building Limapp", "Cleaning up", 0.9F);
                Debug.Log("[Liminal.Build] Cleaning up...");

                foreach (var file in Directory.GetFiles(outputPath))
                    TryDeleteFile(file);

                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
                sw.Stop();

                var zipFile = new FileInfo(zipPath);
                Debug.LogFormat("[Liminal.Build] Build completed in {0:0.00}s. Size: {1:0.00}mb, Output: {2}", sw.Elapsed.TotalSeconds, BytesToMb(zipFile.Length), zipPath);

                EditorUtility.RevealInFinder(zipPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Liminal.Build] Build failed.");
                Debug.LogException(ex);

                EditorUtility.ClearProgressBar();
            }
            finally
            {
                // Ensure everything is always cleaned up...
                ClearAppData(app);
                AssetLookupBuilder.DestroyExisting(buildInfo.Scene);

                if (appWasActive && app != null)
                    app.gameObject.SetActive(true);

                GUIUtility.ExitGUI();
            }
        }

        /// <summary>
        /// Writes the AppPack's assemblies and scene bundle into <c>Limapp-output/&lt;platform&gt;/&lt;id&gt;/</c>
        /// and zips the result. Returns the absolute path to the produced zip file.
        /// </summary>
        private static string WriteOutputZip(AppPack appPack, string platformName)
        {
            var outputBase = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Limapp-output", platformName));
            Directory.CreateDirectory(outputBase);

            var appFolder = Path.Combine(outputBase, appPack.ApplicationId.ToString());
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, recursive: true);
            Directory.CreateDirectory(appFolder);

            var assemblyFolder = Path.Combine(appFolder, "assemblyFolder");
            Directory.CreateDirectory(assemblyFolder);

            foreach (var asmBytes in appPack.Assemblies)
            {
                var asm = System.Reflection.Assembly.Load(asmBytes);
                File.WriteAllBytes(Path.Combine(assemblyFolder, asm.GetName().Name + ".dll"), asmBytes);
            }

            File.WriteAllBytes(Path.Combine(appFolder, "appBundle.bundle"), appPack.SceneBundle);

            var manifest = new LimappExplorer.AppManifest
            {
                ExtractedFrom = $"App {appPack.ApplicationId}",
                CreatedDate = DateTime.UtcNow.ToString()
            };
            File.WriteAllText(Path.Combine(appFolder, "manifest.json"), JsonConvert.SerializeObject(manifest));

            var zipPath = Path.Combine(outputBase, $"{appPack.ApplicationId}.zip");
            Experimental.UnzipTest.ZipFolder(appFolder, zipPath);

            return Path.GetFullPath(zipPath);
        }
        #endregion

        private static float BytesToMb(long len)
        {
            return (len / 1024f / 1024f);
        }

        private static void TryDeleteFile(string file)
        {
            try { File.Delete(file); }
            catch { }
        }

        private static void VerifyAppSceneSetup(ExperienceApp app)
        {

        }
        
        private static IEnumerable<PluginImporter> GetIncludedPlugins(BuildTargetGroup targetGroup, BuildTarget target)
        {
            var list = new List<PluginImporter>();
            var importers = PluginImporter.GetImporters(targetGroup, target);
            foreach (var plugin in importers)
            {
                // Skip anything in the /Liminal folder
                if (plugin.assetPath.IndexOf("Assets/Liminal", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip Unity extensions
                if (plugin.assetPath.IndexOf("Editor/Data/UnityExtensions", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                // Skip anything located in the Packages/ folder of the main project
                if (plugin.assetPath.IndexOf("Packages/", StringComparison.OrdinalIgnoreCase) == 0)
                    continue;

                // Skip native plugins, and anything that won't normally be included in a build
                if (plugin.isNativePlugin || !plugin.ShouldIncludeInBuild())
                    continue;
                
                list.Add(plugin);
            }

            return list;
        }

        private static List<byte[]> BuildPackAssemblyRawBytesList(string mainAppAsmPath, string mainAsmName, BuildTargetGroup targetGroup, BuildTarget target)
        {
            var list = new List<byte[]>();

            if (File.Exists(mainAppAsmPath))
            {
                list.Add(File.ReadAllBytes(mainAppAsmPath));
            }

            foreach (var plugin in GetIncludedPlugins(targetGroup, target))
            {
                list.Add(File.ReadAllBytes(plugin.assetPath));
            }

            // Pre-existing asmdefs in the project (e.g. XR Interaction Toolkit samples) compile into their own
            // DLLs alongside the main app DLL. Pack those too so referenced types resolve at runtime.
            foreach (var asmdefDllPath in GetIncludedAsmdefAssemblies(mainAsmName))
            {
                Debug.LogFormat("[Liminal.Build] Packing asmdef-produced assembly: {0}", asmdefDllPath);
                list.Add(File.ReadAllBytes(asmdefDllPath));
            }

            return list;
        }

        /// <summary>
        /// Locates the limapp's root-asmdef-compiled DLL in the Unity build cache, copies it to
        /// <paramref name="outputBytesPath"/>, and runs Cecil post-processing in place.
        /// </summary>
        /// <remarks>
        /// Replaces the legacy Roslyn <c>AppAssemblyBuilder</c> path. Requires <c>Liminal &gt; Setup Limapp Build</c>
        /// to have been run so that an asmdef named <paramref name="asmName"/> exists at the project root.
        /// </remarks>
        private static void EnsureAsmdefCompiledAssembly(string asmName, string outputBytesPath)
        {
            if (EditorApplication.isCompiling)
                throw new Exception("[Liminal.Build] Scripts are still compiling. Wait for compilation to finish before building.");

            // Always run a fresh player compile so the packed DLL reflects the latest source.
            // Editor recompiles only update Library/ScriptAssemblies (with UNITY_EDITOR defined);
            // Library/Bee/PlayerScriptAssemblies is otherwise only refreshed by a full player build,
            // so trusting whatever's cached there would silently ship stale code.
            var sourceDll = EnsurePlayerDll(asmName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputBytesPath));
            File.Copy(sourceDll, outputBytesPath, overwrite: true);

            new AppAssemblyPostProcessor().PostProcess(outputBytesPath);
        }

        /// <summary>
        /// Force-compiles the player DLL and copies it into Unity's player-assemblies cache
        /// (<c>Library/Bee/PlayerScriptAssemblies/&lt;asmName&gt;.dll</c>) so that subsequent
        /// <see cref="Build"/> calls can pick it up via 'Use Current' without recompiling.
        /// Surfaced as the inline 'Compile' fix on the Build Settings validation panel.
        /// </summary>
        public static string EnsurePlayerDll(string asmName)
        {
            try
            {
                var compiledPath = CompilePlayerScripts(asmName);
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var beeDir = Path.Combine(projectRoot, "Library/Bee/PlayerScriptAssemblies");
                Directory.CreateDirectory(beeDir);
                var beePath = Path.Combine(beeDir, asmName + ".dll");
                File.Copy(compiledPath, beePath, overwrite: true);
                Debug.LogFormat("[Liminal.Build] Player DLL ready at {0}", beePath);
                return beePath;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Force-compiles all player scripts for the active build target into a temp folder and
        /// returns the path to <paramref name="asmName"/>.dll within it.
        /// </summary>
        private static string CompilePlayerScripts(string asmName)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputFolder = Path.Combine(projectRoot, "Temp/LimappPlayerCompile");
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, recursive: true);
            Directory.CreateDirectory(outputFolder);

            var target = EditorUserBuildSettings.activeBuildTarget;
            var settings = new ScriptCompilationSettings
            {
                target = target,
                group = BuildPipeline.GetBuildTargetGroup(target),
                options = ScriptCompilationOptions.None,
            };

            Debug.LogFormat("[Liminal.Build] Compiling player scripts ({0}) to '{1}'...", target, outputFolder);
            UpdateProgressBar("Building Limapp", "Compiling player scripts", 0.05F);
            PlayerBuildInterface.CompilePlayerScripts(settings, outputFolder);

            var dllPath = Path.Combine(outputFolder, asmName + ".dll");
            if (!File.Exists(dllPath))
            {
                throw new Exception(
                    $"[Liminal.Build] Player compile finished but '{asmName}.dll' was not produced at '{dllPath}'. " +
                    "Check that an asmdef named '" + asmName + "' exists and includes the active build platform. " +
                    "If the asmdef is missing, run 'Liminal > Setup Limapp Build'.");
            }
            return dllPath;
        }

        /// <summary>
        /// Enumerates DLL paths produced by asmdefs in the player build, excluding the main app asmdef and
        /// anything under the Liminal SDK or the Packages folder.
        /// </summary>
        private static IEnumerable<string> GetIncludedAsmdefAssemblies(string mainAsmName)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var asm in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                if (string.Equals(asm.name, mainAsmName, StringComparison.Ordinal))
                    continue;

                var firstSource = asm.sourceFiles?.FirstOrDefault();
                if (string.IsNullOrEmpty(firstSource))
                    continue;

                var normalised = firstSource.Replace('\\', '/');
                if (normalised.IndexOf("Assets/Liminal/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (normalised.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var outputPath = asm.outputPath;
                if (string.IsNullOrEmpty(outputPath))
                    continue;

                if (!Path.IsPathRooted(outputPath))
                    outputPath = Path.Combine(projectRoot, outputPath);

                if (!File.Exists(outputPath))
                    continue;

                yield return outputPath;
            }
        }

        public static AppManifest ReadAppManifest()
        {
            var appManifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Liminal/liminalapp.json");
            if (appManifestAsset == null)
            {
                throw new Exception("Application manifest not found. Run 'Liminal > Create or Update App Manifest' and ensure your app details are correct.");
            }
            try
            {
                return JsonConvert.DeserializeObject<AppManifest>(appManifestAsset.text);
            }
            catch (Exception)
            {
                Debug.LogError("Failed to read AppManifest");
                throw;
            }
        }

        private static AppPackPlatform MapAppTargetPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return AppPackPlatform.Android;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return AppPackPlatform.WindowsStandalone;

                default:
                    return AppPackPlatform.Unknown;
            }
        }

        private static string GetAppAssemblyPath()
        {
            return Path.Combine(GetOutputPath(), AppAssemblyFileName).Replace("\\", "/");
        }

        private static string GetOutputPath()
        {
            return "Assets/_Builds";
        }
        
        private static void SetAppData(ExperienceApp app, TextAsset jsonData, AssetLookup assetLookup)
        {
            if (app == null)
                return;

            var so = new SerializedObject(app);
            so.FindProperty("m_AppData").objectReferenceValue = jsonData;
            so.FindProperty("m_AssetLookup").objectReferenceValue = assetLookup;

            var rootObjects = app.gameObject.scene.GetRootGameObjects();
            var pRootObjects = so.FindProperty("m_RootGameObjects");
            for (int i = 0; i < rootObjects.Length; ++i)
            {
                pRootObjects.InsertArrayElementAtIndex(i);
                pRootObjects.GetArrayElementAtIndex(i).objectReferenceValue = rootObjects[i];
            }
            
            so.ApplyModifiedProperties();
        }

        private static void ClearAppData(ExperienceApp app)
        {
            if (app == null)
                return;

            var so = new SerializedObject(app);
            so.FindProperty("m_AppData").objectReferenceValue = null;
            so.FindProperty("m_AssetLookup").objectReferenceValue = null;
            so.FindProperty("m_RootGameObjects").ClearArray();
            so.FindProperty("m_RootGameObjects").arraySize = 0;
            so.ApplyModifiedProperties();
        }
    }
}