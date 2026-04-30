using Liminal.SDK.V2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Build
{
    public class SceneMigrationWindow : BaseWindowDrawer
    {
        // Loaded via Resources so it resolves whether the SDK is imported under Assets/ or Packages/com.liminal.sdk/.
        private const string ExperienceManagerResourceName = "ExperienceManager";
        private const string ExperienceManagerObjectName = "ExperienceManager";
        private const string ExperienceAppObjectName = "[ExperienceApp]";

        public override void Draw(BuildWindowConfig config)
        {
            GUILayout.BeginVertical("Box");
            {
                EditorGUIHelper.DrawTitle("Scene Migration");
                GUILayout.Label(
                    "Spawns the ExperienceManager prefab into the active scene and wires up references to the existing ExperienceApp / VRAvatar.",
                    EditorStyles.wordWrappedLabel);

                GUILayout.Space(8);

                if (GUILayout.Button("Spawn ExperienceManager & Find References"))
                {
                    SpawnAndFindReferences();
                }

                GUILayout.Space(8);

                GUILayout.Label(
                    "Enables the Oculus XR provider and 'Initialize XR on Startup' for both Android and Windows (Standalone).",
                    EditorStyles.wordWrappedLabel);

                if (GUILayout.Button("Configure Oculus XR (Android + Windows)"))
                {
                    XRSettingsConfigurator.ConfigureOculusForAndroidAndStandalone();
                }
            }
            GUILayout.EndVertical();
        }

        private static void SpawnAndFindReferences()
        {
            var instance = FindRootByName(ExperienceManagerObjectName);
            if (instance != null)
            {
                Debug.Log($"[SceneMigration] {ExperienceManagerObjectName} already exists — skipping spawn.", instance);
            }
            else
            {
                var prefab = Resources.Load<GameObject>(ExperienceManagerResourceName);
                if (prefab == null)
                {
                    Debug.LogError($"[SceneMigration] Could not find {ExperienceManagerResourceName} prefab in any Resources folder.");
                    return;
                }

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    Debug.LogError("[SceneMigration] Failed to instantiate ExperienceManager prefab.");
                    return;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Spawn ExperienceManager");
            }

            Selection.activeGameObject = instance;

            var experienceApp = FindRootByName(ExperienceAppObjectName);
            if (experienceApp == null)
            {
                Debug.LogWarning($"[SceneMigration] {ExperienceAppObjectName} can't be found in the active scene.");
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
                Debug.LogError("[SceneMigration] ExperienceManager prefab is missing ExperienceAppManager component.", instance);
            }

            EditorSceneManager.MarkSceneDirty(instance.scene);
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
