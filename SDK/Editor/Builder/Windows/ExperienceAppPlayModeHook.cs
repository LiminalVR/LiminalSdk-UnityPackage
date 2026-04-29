using Liminal.SDK;
using UnityEditor;

namespace Liminal.SDK.Build
{
    [InitializeOnLoad]
    internal static class ExperienceAppPlayModeHook
    {
        private const string ExperienceAppObjectName = "[ExperienceApp]";

        static ExperienceAppPlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var app = GameObjectUtils.FindInactiveByName(ExperienceAppObjectName);
            if (app != null && app.activeSelf)
                app.SetActive(false);
        }
    }
}
