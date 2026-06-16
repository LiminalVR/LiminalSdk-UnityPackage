using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Forces the Android <c>INTERNET</c> permission on for every player build.
    /// <para>
    /// The limapp downloader needs network access at runtime, but Unity's default "Internet Access = Auto"
    /// mode decides the permission by statically scanning the build for networking APIs — and it can't see
    /// the calls because they live in runtime-loaded limapp assemblies. The result is an APK that silently
    /// omits <c>INTERNET</c>, so every <see cref="UnityEngine.Networking.UnityWebRequest"/> fails on device
    /// with "Cannot resolve destination host" even though the network is fine.
    /// </para>
    /// This step sets <see cref="PlayerSettings.Android.forceInternetPermission"/> before the manifest is
    /// generated, so the permission can never be dropped regardless of the Player Settings value.
    /// </summary>
    public class AndroidInternetPermissionBuildStep : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            if (!PlayerSettings.Android.forceInternetPermission)
            {
                PlayerSettings.Android.forceInternetPermission = true;
                Debug.Log("[Liminal.Android] Forced android.permission.INTERNET on for this Android build.");
            }
        }
    }
}
