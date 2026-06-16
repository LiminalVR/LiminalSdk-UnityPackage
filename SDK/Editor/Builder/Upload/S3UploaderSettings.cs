using UnityEditor;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Per-user, machine-wide settings for the S3 limapp uploader, persisted via <see cref="EditorPrefs"/>
    /// (same scope as <see cref="LiminalUserSettings"/>).
    /// <para>
    /// NOTE: <see cref="SecretAccessKey"/> is stored in plaintext in EditorPrefs (Windows registry under
    /// HKCU). Use a dedicated, least-privilege upload key — not a root/admin key.
    /// </para>
    /// </summary>
    public static class S3UploaderSettings
    {
        private const string KeyPrefix = "Liminal.Build.S3.";

        public static string AccessKeyId
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(AccessKeyId), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(AccessKeyId), value ?? string.Empty);
        }

        public static string SecretAccessKey
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(SecretAccessKey), string.Empty);
            set => EditorPrefs.SetString(KeyPrefix + nameof(SecretAccessKey), value ?? string.Empty);
        }

        public static string Region
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(Region), "ap-southeast-2");
            set => EditorPrefs.SetString(KeyPrefix + nameof(Region), value ?? string.Empty);
        }

        public static string Bucket
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(Bucket), "liminal-resources");
            set => EditorPrefs.SetString(KeyPrefix + nameof(Bucket), value ?? string.Empty);
        }

        /// <summary>Key prefix ("folder") within the bucket that uploads are placed under.</summary>
        public static string Folder
        {
            get => EditorPrefs.GetString(KeyPrefix + nameof(Folder), "app/Limapp/v3");
            set => EditorPrefs.SetString(KeyPrefix + nameof(Folder), value ?? string.Empty);
        }

        /// <summary>When true, uploads are sent with the <c>public-read</c> canned ACL.</summary>
        public static bool PublicRead
        {
            get => EditorPrefs.GetBool(KeyPrefix + nameof(PublicRead), true);
            set => EditorPrefs.SetBool(KeyPrefix + nameof(PublicRead), value);
        }

        /// <summary>When true, a finished build is automatically uploaded to S3. Off by default.</summary>
        public static bool AutoUploadAfterBuild
        {
            get => EditorPrefs.GetBool(KeyPrefix + nameof(AutoUploadAfterBuild), false);
            set => EditorPrefs.SetBool(KeyPrefix + nameof(AutoUploadAfterBuild), value);
        }

        /// <summary>
        /// Clears the target settings (Region, Bucket, Folder, Public read) so their getters fall back to
        /// the current code defaults. Credentials are intentionally left untouched.
        /// </summary>
        public static void ResetTargetDefaults()
        {
            EditorPrefs.DeleteKey(KeyPrefix + nameof(Region));
            EditorPrefs.DeleteKey(KeyPrefix + nameof(Bucket));
            EditorPrefs.DeleteKey(KeyPrefix + nameof(Folder));
            EditorPrefs.DeleteKey(KeyPrefix + nameof(PublicRead));
        }
    }
}
