using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Liminal.SDK.Build
{
    internal static class PlayerSettingsConfigurator
    {
        private const AndroidSdkVersions MinAndroidSdk = AndroidSdkVersions.AndroidApiLevel30;
        private const string LegacyPluginsAndroidPath = "Assets/Plugins/Android";
        private const string OvrAndroidMrcDefine = "OVR_ANDROID_MRC";

        public static void ConfigureAndroid()
        {
            EnsureLinearColorSpace();
            EnsureAndroidBuildTarget();
            EnsureMinAndroidApiLevel();
            EnsureAndroidScriptingDefine(OvrAndroidMrcDefine);
            DeleteLegacyPluginsAndroid();
        }

        /// <summary>
        /// True when every change made by <see cref="ConfigureAndroid"/> is already in effect.
        /// Surfaced as the "Done" badge in the Setup wizard.
        /// </summary>
        public static bool IsAndroidConfigured()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                return false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                return false;
            if ((int)PlayerSettings.Android.minSdkVersion < (int)MinAndroidSdk)
                return false;

            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android) ?? string.Empty;
            var hasOvrDefine = defines
                .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Contains(OvrAndroidMrcDefine);
            if (!hasOvrDefine)
                return false;

            if (AssetDatabase.IsValidFolder(LegacyPluginsAndroidPath))
                return false;

            return true;
        }

        private static void EnsureLinearColorSpace()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Debug.Log("[PlayerSettings] Color Space already Linear.");
                return;
            }

            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("[PlayerSettings] Color Space set to Linear.");
        }

        private static void EnsureAndroidBuildTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[PlayerSettings] Active build target already Android.");
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[PlayerSettings] Failed to switch active build target to Android. Is the Android module installed?");
                return;
            }

            Debug.Log("[PlayerSettings] Active build target switched to Android.");
        }

        private static void EnsureMinAndroidApiLevel()
        {
            if ((int)PlayerSettings.Android.minSdkVersion >= (int)MinAndroidSdk)
            {
                Debug.Log($"[PlayerSettings] Min Android API already {(int)PlayerSettings.Android.minSdkVersion} (>= {(int)MinAndroidSdk}).");
                return;
            }

            PlayerSettings.Android.minSdkVersion = MinAndroidSdk;
            Debug.Log($"[PlayerSettings] Min Android API set to {(int)MinAndroidSdk} (Android 11).");
        }

        private static void EnsureAndroidScriptingDefine(string define)
        {
            var target = NamedBuildTarget.Android;
            var current = PlayerSettings.GetScriptingDefineSymbols(target) ?? string.Empty;
            var symbols = current.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (symbols.Contains(define))
            {
                Debug.Log($"[PlayerSettings] Android scripting define '{define}' already present.");
                return;
            }

            symbols.Add(define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            Debug.Log($"[PlayerSettings] Added '{define}' to Android scripting defines.");
        }

        private static void DeleteLegacyPluginsAndroid()
        {
            if (!AssetDatabase.IsValidFolder(LegacyPluginsAndroidPath))
            {
                Debug.Log($"[PlayerSettings] {LegacyPluginsAndroidPath} not present — nothing to delete.");
                return;
            }

            if (AssetDatabase.DeleteAsset(LegacyPluginsAndroidPath))
                Debug.Log($"[PlayerSettings] Deleted {LegacyPluginsAndroidPath}.");
            else
                Debug.LogError($"[PlayerSettings] Failed to delete {LegacyPluginsAndroidPath}.");
        }
    }
}
