using System.IO;
using Liminal.SDK.Editor.Build;
using Liminal.SDK.V2;
using Liminal.SDK.VR.Avatars;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        public override void Draw(BuildWindowConfig config)
        {
            var hasVRAvatar = FindAnyVRAvatar() != null;
            var hasManager = FindAnyManager() != null;

            GUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Scene Setup");
                GUILayout.Label("Complete each step in order. Steps unlock as their prerequisites are met.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(8);

                DrawStep(
                    index: 1,
                    title: "Spawn VRAvatar",
                    description: "Drops the VRAvatar prefab into the active scene. Required before wiring up the ExperienceManager.",
                    done: hasVRAvatar,
                    enabled: !hasVRAvatar,
                    buttonLabel: "Spawn VRAvatar",
                    onClick: SpawnVRAvatar);

                DrawStep(
                    index: 2,
                    title: "Setup Scene",
                    description: "Spawns the ExperienceManager prefab and wires up references to the existing ExperienceApp / VRAvatar.",
                    done: hasManager,
                    enabled: hasVRAvatar && !hasManager,
                    buttonLabel: "Setup Scene",
                    onClick: SpawnAndFindReferences);

                DrawStep(
                    index: 3,
                    title: "Configure XR",
                    description: "Enables the Oculus XR provider and 'Initialize XR on Startup' for both Android and Windows (Standalone).",
                    done: XRSettingsConfigurator.IsOculusConfiguredForAndroidAndStandalone(),
                    enabled: hasManager,
                    buttonLabel: "Configure XR",
                    onClick: XRSettingsConfigurator.ConfigureOculusForAndroidAndStandalone);

                DrawStep(
                    index: 4,
                    title: "Configure Android",
                    description: "Forces Linear color space, switches to Android build target, sets Min API Level to 30 (Android 11), and deletes the legacy Assets/Plugins/Android folder.",
                    done: PlayerSettingsConfigurator.IsAndroidConfigured(),
                    enabled: hasManager,
                    buttonLabel: "Configure Android",
                    onClick: PlayerSettingsConfigurator.ConfigureAndroid);

                DrawStep(
                    index: 5,
                    title: "Setup Limapp Build",
                    description: "Creates the project-root assembly definition (named after the app manifest) plus a single shared Editor assembly definition, and drops asmref pointers into every /Editor folder so all editor scripts compile into one assembly (fixes cross-folder editor references like Mirza Beig's Particle Scaler ↔ Particle Playback). Required so the limapp DLL compiles without UNITY_EDITOR. Safe to re-run — references are refreshed and legacy per-folder Editor asmdefs are auto-migrated to asmrefs.",
                    done: IsLimappBuildConfigured(),
                    enabled: hasManager,
                    buttonLabel: "Setup Limapp Build",
                    onClick: LimapPatchWindow.SetupLimappBuild);
            }
            GUILayout.EndVertical();
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

        private static VRAvatar FindAnyVRAvatar()
        {
            return Object.FindAnyObjectByType<VRAvatar>(FindObjectsInactive.Include);
        }

        private static ExperienceAppManager FindAnyManager()
        {
            return Object.FindAnyObjectByType<ExperienceAppManager>(FindObjectsInactive.Include);
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
