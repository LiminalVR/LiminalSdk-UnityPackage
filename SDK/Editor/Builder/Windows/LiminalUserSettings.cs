using UnityEditor;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Per-user, machine-wide settings for the limapp build tooling. Persisted via <see cref="EditorPrefs"/>
    /// so the same values are available across every limapp Unity project opened on this machine — paths
    /// to the SDK, the Platform App, and the DLL deploy folders are user-environment-scoped, not project-scoped.
    /// </summary>
    public static class LiminalUserSettings
    {
        private const string KeyPrefix = "Liminal.Build.";

        public static string LiminalSdkPath
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(LiminalSdkPath), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(LiminalSdkPath), value ?? string.Empty);
        }

        public static string PlatformAppProjectPath
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(PlatformAppProjectPath), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(PlatformAppProjectPath), value ?? string.Empty);
        }

        public static string SdkDllDestinationFolder
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(SdkDllDestinationFolder), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(SdkDllDestinationFolder), value ?? string.Empty);
        }

        public static string PlatformAppDllDestinationFolder
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(PlatformAppDllDestinationFolder), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(PlatformAppDllDestinationFolder), value ?? string.Empty);
        }

        public static bool AutoCopyAfterBuild
        {
            get => EditorPrefs.GetBool(KeyPrefix + nameof(AutoCopyAfterBuild), true);
            set => EditorPrefs.SetBool(KeyPrefix + nameof(AutoCopyAfterBuild), value);
        }

        public static bool RevealInFinderAfterBuild
        {
            get => EditorPrefs.GetBool(KeyPrefix + nameof(RevealInFinderAfterBuild), false);
            set => EditorPrefs.SetBool(KeyPrefix + nameof(RevealInFinderAfterBuild), value);
        }
    }
}
