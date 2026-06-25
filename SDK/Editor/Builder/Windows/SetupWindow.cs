using System;
using System.IO;
using System.Reflection;
using Liminal.SDK.Core;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.V2;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Guides the user through the steps to make the active scene a Liminal Experience App.
    /// </summary>
    public class SetupWindow : BaseWindowDrawer
    {
        // Loaded via Resources so it resolves whether the SDK is imported under Assets/ or Packages/com.liminal.sdk/.
        private const string ExperienceManagerResourceName = "ExperienceManager";
        private const string VRAvatarResourceName = "VRAvatar";
        private const string ExperienceManagerObjectName = "ExperienceManager";
        private const string ExperienceAppObjectName = "[ExperienceApp]";

        private const string TemplateSetupFolder = "Assets/Liminal/TemplateSetup";
        private const string TextMeshProFolder = "Assets/TextMesh Pro";
        private const string TMPSettingsAssetPath = TextMeshProFolder + "/Resources/TMP Settings.asset";

        public override void Draw(BuildWindowConfig config)
        {
            DrawOpenScene(config);

            var hasExperienceApp = FindAnyExperienceApp() != null;
            var hasExperienceAppGo = FindRootByName(ExperienceAppObjectName) != null;
            var hasVRAvatar = FindAnyVRAvatar() != null;
            var hasManager = FindAnyManager() != null;

            // If the GameObject exists but is missing the component (script broken, script removed,
            // etc.) the fix is to add the component; otherwise the fix is to create the GameObject.
            var verifyButtonLabel = hasExperienceAppGo ? "Restore Component" : "Create [ExperienceApp]";

            GUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Scene Setup");
                GUILayout.Label("Complete each step in order. Steps unlock as their prerequisites are met. Or press Run All to do every step at once (already-done steps are skipped).", EditorStyles.wordWrappedLabel);
                GUILayout.Space(8);

                using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
                {
                    var prevBtnBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.5f, 0.75f, 1f);
                    if (GUILayout.Button("Run All", GUILayout.Height(30)))
                        RunAllSteps(config);
                    GUI.backgroundColor = prevBtnBg;
                }
                GUILayout.Space(8);

                var hasTemplateSetup = AssetDatabase.IsValidFolder(TemplateSetupFolder);
                DrawStep(
                    index: 1,
                    title: "Remove TemplateSetup",
                    description: "Deletes the legacy Assets/Liminal/TemplateSetup folder, which is incompatible with this SDK version.",
                    done: !hasTemplateSetup,
                    enabled: hasTemplateSetup,
                    buttonLabel: "Delete TemplateSetup",
                    onClick: DeleteTemplateSetup);

                var tmpUpToDate = IsTextMeshProUpToDate();
                DrawStep(
                    index: 2,
                    title: "Refresh TextMesh Pro",
                    description: "Deletes the legacy Assets/TextMesh Pro folder and silently re-imports the TMP Essential Resources from the built-in uGUI package, so the resources match this Unity version. Stays pressable even when marked Done so you can force a clean delete + re-import if a stale copy lingers.",
                    done: tmpUpToDate,
                    enabled: true,
                    buttonLabel: "Delete & Refresh TMP Resources",
                    onClick: RefreshTextMeshPro);

                DrawStep(
                    index: 3,
                    title: "Verify [ExperienceApp]",
                    description: "Ensures the scene has a GameObject with an ExperienceApp (or subclass) component. Pending if the [ExperienceApp] GameObject is missing, or exists but lost its script.",
                    done: hasExperienceApp,
                    enabled: !hasExperienceApp,
                    buttonLabel: verifyButtonLabel,
                    onClick: EnsureExperienceApp);

                DrawStep(
                    index: 4,
                    title: "Spawn VRAvatar",
                    description: "Drops the VRAvatar prefab into the active scene. Required before wiring up the ExperienceManager.",
                    done: hasVRAvatar,
                    enabled: hasExperienceApp && !hasVRAvatar,
                    buttonLabel: "Spawn VRAvatar",
                    onClick: SpawnVRAvatar);

                DrawStep(
                    index: 5,
                    title: "Setup Scene",
                    description: "Removes any VREmulator from the scene, spawns the ExperienceManager prefab, and wires up references to the existing ExperienceApp / VRAvatar.",
                    done: hasManager,
                    enabled: hasExperienceApp && hasVRAvatar && !hasManager,
                    buttonLabel: "Setup Scene",
                    onClick: SpawnAndFindReferences);

                DrawStep(
                    index: 6,
                    title: "Configure XR",
                    description: "Sets OpenXR as the XR provider with 'Initialize XR on Startup' for both Android and Windows (Standalone), and enables the required interaction profiles (Oculus Touch, Hand Interaction, Meta Quest Touch Plus) and features (Hand Interaction Poses, Hand Tracking Subsystem, Meta Hand Tracking Aim, Meta Quest Support, Palm Pose). Sets the Standalone stereo render mode to Multi Pass (Android/Quest stays Single Pass Instanced). Also applies the OpenXR project-validation fixes: IL2CPP + ARM64, Game Activity entry point, Prioritize Input Polling latency, Optimize Buffer Discards disabled (it is Vulkan-only; the platform uses GLES3), and the PoseControl/StickControl input defines.",
                    done: XRSettingsConfigurator.IsOpenXRConfiguredForAndroidAndStandalone(),
                    enabled: hasManager,
                    buttonLabel: "Configure XR",
                    onClick: XRSettingsConfigurator.ConfigureOpenXRForAndroidAndStandalone);

                DrawStep(
                    index: 7,
                    title: "Configure Android",
                    description: "Forces Linear color space, switches to Android build target, sets Min API Level to 30 (Android 11), forces GLES3 as the sole graphics API (removes Vulkan), and deletes the legacy Assets/Plugins/Android folder.",
                    done: PlayerSettingsConfigurator.IsAndroidConfigured(),
                    enabled: hasManager,
                    buttonLabel: "Configure Android",
                    onClick: PlayerSettingsConfigurator.ConfigureAndroid);

                DrawStep(
                    index: 8,
                    title: "Setup Limapp Build",
                    description: "Creates the project-root assembly definition (named after the app manifest) plus a single shared Editor assembly definition, and drops asmref pointers into every /Editor folder so all editor scripts compile into one assembly (fixes cross-folder editor references like Mirza Beig's Particle Scaler ↔ Particle Playback). Required so the limapp DLL compiles without UNITY_EDITOR. Safe to re-run — references are refreshed and legacy per-folder Editor asmdefs are auto-migrated to asmrefs.",
                    done: IsLimappBuildConfigured(),
                    enabled: hasManager,
                    buttonLabel: "Setup Limapp Build",
                    onClick: LimapPatchWindow.SetupLimappBuild);
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Convenience button at the top of the Setup tab: opens the scene currently targeted in the
        /// Build tab (<see cref="BuildWindowConfig.TargetScene"/>). The numbered steps below all operate
        /// on the active scene, so this lets the user load that scene first.
        /// </summary>
        private static void DrawOpenScene(BuildWindowConfig config)
        {
            var scenePath = config.TargetScene;
            var sceneAsset = string.IsNullOrEmpty(scenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            GUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Open Scene");

                var label = sceneAsset != null
                    ? $"Target scene (from Build tab): {scenePath}"
                    : "No target scene selected — pick one in the Build tab's Target Scene field.";
                GUILayout.Label(label, EditorStyles.wordWrappedLabel);

                using (new EditorGUI.DisabledScope(sceneAsset == null))
                {
                    if (GUILayout.Button("Open Scene"))
                        OpenTargetScene(scenePath);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void OpenTargetScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return;

            // Give the user a chance to save before we replace the open scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[Setup] Opened scene {scenePath}.");
        }

        private static GUIStyle _stepTitleStyle;
        private static GUIStyle StepTitleStyle => _stepTitleStyle ??= new GUIStyle(EditorStyles.boldLabel) { richText = true };

        private static void DrawStep(int index, string title, string description, bool done, bool enabled, string buttonLabel, System.Action onClick)
        {
            var prevBg = GUI.backgroundColor;
            if (done)
                GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;
            {
                GUILayout.BeginHorizontal();
                {
                    var status = done ? "<color=#5FCC5F>[Done]</color>" : "[Pending]";
                    GUILayout.Label($"{status}  {index}. {title}", StepTitleStyle);
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(description, EditorStyles.wordWrappedLabel);

                using (new EditorGUI.DisabledScope(!enabled))
                {
                    if (GUILayout.Button(buttonLabel))
                        onClick?.Invoke();
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        /// <summary>
        /// Runs every setup step in order, skipping any that already report Done so it's safe to re-run
        /// and never duplicates scene objects. Each step's "done" check is re-evaluated as it goes, so
        /// steps that unlock once their prerequisite ran (e.g. Spawn VRAvatar after Verify [ExperienceApp])
        /// execute correctly. Shows a cancelable progress bar.
        /// </summary>
        private static void RunAllSteps(BuildWindowConfig config)
        {
            if (!EditorUtility.DisplayDialog(
                    "Run All Setup Steps",
                    "This runs every setup step in order on the active scene and changes project settings (XR, Android, asmdefs), compiles the player DLL, sets the build target to Current, saves the scene, then builds the limapp. Steps already marked Done are skipped.\n\nContinue?",
                    "Run All", "Cancel"))
                return;

            string asmName = null;
            try { asmName = AppBuilder.ReadAppManifest()?.Name; }
            catch (Exception ex) { Debug.LogWarning($"[Setup] Could not read app manifest for the player DLL step: {ex.Message}"); }

            // (label, isDone, run) for each step, in execution order. isDone is re-evaluated per step.
            var steps = new (string Label, Func<bool> Done, Action Run)[]
            {
                ("Remove TemplateSetup", () => !AssetDatabase.IsValidFolder(TemplateSetupFolder), DeleteTemplateSetup),
                ("Refresh TextMesh Pro", IsTextMeshProUpToDate, RefreshTextMeshPro),
                ("Verify [ExperienceApp]", () => FindAnyExperienceApp() != null, EnsureExperienceApp),
                ("Spawn VRAvatar", () => FindAnyVRAvatar() != null, SpawnVRAvatar),
                ("Setup Scene", () => FindAnyManager() != null, SpawnAndFindReferences),
                ("Configure XR", XRSettingsConfigurator.IsOpenXRConfiguredForAndroidAndStandalone, XRSettingsConfigurator.ConfigureOpenXRForAndroidAndStandalone),
                ("Configure Android", PlayerSettingsConfigurator.IsAndroidConfigured, PlayerSettingsConfigurator.ConfigureAndroid),
                ("Setup Limapp Build", IsLimappBuildConfigured, LimapPatchWindow.SetupLimappBuild),
                // The Build tab's "Compile" fix. Done = the player DLL already exists (same check that
                // hides the Compile button), so re-runs skip the slow recompile.
                ("Compile Player DLL", () => PlayerDllExists(asmName), () => CompilePlayerDll(asmName)),
                // Point the Build tab at the active platform.
                ("Set Build Target to Current", () => config.SelectedPlatform == BuildPlatform.Current, () => config.SelectedPlatform = BuildPlatform.Current),
                // Persist the scene before it is built.
                ("Save Scene", () => !EditorSceneManager.GetActiveScene().isDirty, () => EditorSceneManager.SaveOpenScenes()),
                // Build the limapp for the current platform (always runs — this is the goal of Run All).
                ("Build Limapp", () => false, BuildLimappCurrent),
            };

            try
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    var step = steps[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Liminal Scene Setup",
                            $"Step {i + 1}/{steps.Length}: {step.Label}",
                            (float)i / steps.Length))
                    {
                        Debug.LogWarning($"[Setup] Run All cancelled before '{step.Label}'.");
                        return;
                    }

                    if (step.Done())
                    {
                        Debug.Log($"[Setup] Step {i + 1}/{steps.Length} '{step.Label}' already done — skipping.");
                        continue;
                    }

                    Debug.Log($"[Setup] Step {i + 1}/{steps.Length}: {step.Label}...");
                    step.Run();
                }

                EditorUtility.DisplayProgressBar("Liminal Scene Setup", "Done", 1f);
                Debug.Log("[Setup] Run All complete. TMP import / script compilation may continue in the background.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// True when the compiled player DLL already exists in Unity's player-assemblies cache. Mirrors the
        /// Build tab's "Player DLL" validation check (existence only), which is what gates its Compile button.
        /// </summary>
        private static bool PlayerDllExists(string asmName)
        {
            if (string.IsNullOrEmpty(asmName))
                return false;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var dllPath = Path.Combine(projectRoot, "Library/Bee/PlayerScriptAssemblies", asmName + ".dll");
            return File.Exists(dllPath);
        }

        /// <summary>
        /// Runs the same action as the Build tab's "Compile" fix: force-compiles the player DLL (no UNITY_EDITOR)
        /// and drops it into the player-assemblies cache so a build can pick it up.
        /// </summary>
        private static void CompilePlayerDll(string asmName)
        {
            if (string.IsNullOrEmpty(asmName))
            {
                Debug.LogWarning("[Setup] Skipping player DLL compile — app manifest name is unavailable.");
                return;
            }

            try
            {
                AppBuilder.EnsurePlayerDll(asmName);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[Setup] Failed to compile player DLL: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the limapp for the current (active) platform — the same action as the Build tab's
        /// Build button with target "Current". Copies project settings into the build profile first.
        /// </summary>
        private static void BuildLimappCurrent()
        {
            try
            {
                SettingsUtils.CopyProjectSettingsToProfile();
                AppBuilder.BuildCurrentPlatform();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[Setup] Limapp build failed: {ex.Message}");
            }
        }

        private static void DeleteTemplateSetup()
        {
            if (AssetDatabase.DeleteAsset(TemplateSetupFolder))
                Debug.Log($"[Setup] Deleted {TemplateSetupFolder}.");
            else
                Debug.LogWarning($"[Setup] Could not delete {TemplateSetupFolder}.");
        }

        /// <summary>
        /// Mirrors the check TMP's own resource importer uses: the TMP Settings asset exists and its
        /// (internal) assetVersion matches the package's s_CurrentAssetVersion. If the internals move
        /// in a future uGUI version, falls back to just checking the asset exists.
        /// </summary>
        private static bool IsTextMeshProUpToDate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(TMPSettingsAssetPath);
            if (settings == null)
                return false;

            var currentVersion = typeof(TMP_Settings)
                .GetField("s_CurrentAssetVersion", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) as string;

            var assetVersion = new SerializedObject(settings).FindProperty("assetVersion");
            if (currentVersion == null || assetVersion == null)
                return true;

            return assetVersion.stringValue == currentVersion;
        }

        private static void RefreshTextMeshPro()
        {
            if (AssetDatabase.IsValidFolder(TextMeshProFolder))
            {
                if (!AssetDatabase.DeleteAsset(TextMeshProFolder))
                {
                    Debug.LogWarning($"[Setup] Could not delete {TextMeshProFolder} — aborting TMP refresh.");
                    return;
                }

                Debug.Log($"[Setup] Deleted {TextMeshProFolder}.");
            }

            // A fresh (non-update) import ships TMP Settings with the current asset version already
            // set, so no SetAssetVersion call is needed afterwards. Import is async; the step's Done
            // badge flips once it completes.
            TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
            Debug.Log("[Setup] Importing TMP Essential Resources...");
        }

        private static void SpawnVRAvatar()
        {
            var prefab = Resources.Load<VRAvatar>(VRAvatarResourceName);
            if (prefab == null)
            {
                Debug.LogError($"[Setup] Could not find {VRAvatarResourceName} prefab in any Resources folder.");
                return;
            }

            var instance = (VRAvatar)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                Debug.LogError("[Setup] Failed to instantiate VRAvatar prefab.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance.gameObject, "Spawn VRAvatar");
            Selection.activeGameObject = instance.gameObject;
            EditorSceneManager.MarkSceneDirty(instance.gameObject.scene);
        }

        private static void SpawnAndFindReferences()
        {
            // The VREmulator is an in-editor stand-in; the ExperienceManager drives the real rig,
            // so leaving it in the scene causes two devices to initialise. Remove it during setup.
            DeleteVREmulators();

            var instance = FindRootByName(ExperienceManagerObjectName);
            if (instance != null)
            {
                Debug.Log($"[Setup] {ExperienceManagerObjectName} already exists — skipping spawn.", instance);
            }
            else
            {
                var prefab = Resources.Load<GameObject>(ExperienceManagerResourceName);
                if (prefab == null)
                {
                    Debug.LogError($"[Setup] Could not find {ExperienceManagerResourceName} prefab in any Resources folder.");
                    return;
                }

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    Debug.LogError("[Setup] Failed to instantiate ExperienceManager prefab.");
                    return;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Spawn ExperienceManager");
            }

            Selection.activeGameObject = instance;

            var experienceApp = FindRootByName(ExperienceAppObjectName);
            if (experienceApp == null)
            {
                Debug.LogWarning($"[Setup] {ExperienceAppObjectName} can't be found in the active scene.");
            }
            else
            {
                ReparentStrayRootsInto(experienceApp.transform, instance);
            }

            var manager = instance.GetComponent<ExperienceAppManager>();
            if (manager != null)
            {
                manager.FindReferences();
            }
            else
            {
                Debug.LogError("[Setup] ExperienceManager prefab is missing ExperienceAppManager component.", instance);
            }

            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        private static bool IsLimappBuildConfigured()
        {
            try
            {
                var manifest = AppBuilder.ReadAppManifest();
                var rootAsmdefPath = Path.Combine(Application.dataPath, manifest.Name + ".asmdef");
                var sharedEditorAsmdefPath = Path.Combine(
                    Application.dataPath, LimapPatchWindow.SharedEditorAsmdefFolder, manifest.Name + ".Editor.asmdef");
                return File.Exists(rootAsmdefPath) && File.Exists(sharedEditorAsmdefPath);
            }
            catch
            {
                return false;
            }
        }

        private static ExperienceApp FindAnyExperienceApp()
        {
            return Object.FindAnyObjectByType<ExperienceApp>(FindObjectsInactive.Include);
        }

        private static VRAvatar FindAnyVRAvatar()
        {
            return Object.FindAnyObjectByType<VRAvatar>(FindObjectsInactive.Include);
        }

        private static void EnsureExperienceApp()
        {
            var type = FindBestExperienceAppType();
            if (type == null)
            {
                Debug.LogError("[Setup] No concrete ExperienceApp type was found in any loaded assembly. Liminal SDK assembly may not be loaded.");
                return;
            }

            var go = FindRootByName(ExperienceAppObjectName);
            if (go == null)
            {
                go = new GameObject(ExperienceAppObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Create [ExperienceApp]");
            }

            // Strip any missing-script components first so a re-add doesn't compound them.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            if (go.GetComponent<ExperienceApp>() == null)
            {
                Undo.AddComponent(go, type);
                Debug.Log($"[Setup] Added {type.Name} to {ExperienceAppObjectName}.", go);
            }
            else
            {
                Debug.Log($"[Setup] {ExperienceAppObjectName} already has an ExperienceApp component.", go);
            }

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        /// <summary>
        /// Walks all loaded assemblies for the most-derived non-abstract subclass of
        /// <see cref="ExperienceApp"/> so the verify step prefers the project's own
        /// subclass over the base SDK type. Falls back to base ExperienceApp if no
        /// subclass exists.
        /// </summary>
        private static Type FindBestExperienceAppType()
        {
            var baseType = typeof(ExperienceApp);
            Type best = baseType.IsAbstract ? null : baseType;
            int bestDepth = 0;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type == baseType) continue;
                    if (type.IsAbstract) continue;
                    if (!baseType.IsAssignableFrom(type)) continue;

                    int depth = 0;
                    var t = type;
                    while (t != null && t != baseType)
                    {
                        t = t.BaseType;
                        depth++;
                    }

                    if (depth > bestDepth)
                    {
                        bestDepth = depth;
                        best = type;
                    }
                }
            }

            return best;
        }

        private static ExperienceAppManager FindAnyManager()
        {
            return Object.FindAnyObjectByType<ExperienceAppManager>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Removes any <see cref="VREmulator"/> component from the loaded scene(s). Only the component
        /// is removed — the GameObject hosting it is left intact.
        /// </summary>
        private static void DeleteVREmulators()
        {
            var emulators = Object.FindObjectsByType<VREmulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var emulator in emulators)
            {
                if (emulator == null)
                    continue;

                Debug.Log($"[Setup] Removing VREmulator component from '{emulator.gameObject.name}'.", emulator.gameObject);
                Undo.DestroyObjectImmediate(emulator);
            }
        }

        private static GameObject FindRootByName(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == name)
                        return root;
                }
            }

            return null;
        }

        private static void ReparentStrayRootsInto(Transform experienceApp, GameObject experienceManager)
        {
            var roots = experienceApp.gameObject.scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == experienceApp.gameObject) continue;
                if (root == experienceManager) continue;

                Undo.SetTransformParent(root.transform, experienceApp, "Reparent into [ExperienceApp]");
            }
        }
    }
}
