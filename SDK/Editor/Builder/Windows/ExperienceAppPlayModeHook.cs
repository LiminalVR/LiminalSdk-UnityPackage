using Liminal.SDK.V2;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    [InitializeOnLoad]
    internal static class ExperienceAppPlayModeHook
    {
        static ExperienceAppPlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var app = FindAssignedExperienceApp();

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    Debug.Log("Entering Play Mode");
                    if (app != null && app.activeSelf)
                        app.SetActive(false);

                    break;
                case PlayModeStateChange.EnteredEditMode:
                    Debug.Log("Entering Edit Mode");    
                    if (app != null && !app.activeSelf)
                        app.SetActive(true);
                    break;
            }

        }

        private static GameObject FindAssignedExperienceApp()
        {
            var managers = Object.FindObjectsByType<ExperienceAppManager>(FindObjectsInactive.Include);
            foreach (var manager in managers)
            {
                if (manager != null && manager.ExperienceApp != null)
                    return manager.ExperienceApp.gameObject;
            }

            return null;
        }
    }
}
