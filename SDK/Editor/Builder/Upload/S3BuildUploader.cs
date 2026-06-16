using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Headless S3 upload of a built limapp's Android + Standalone zips, used by the post-build
    /// auto-upload hook. Mirrors the Upload tab (Standalone falls back to the Android build) but derives
    /// keys from <see cref="S3UploaderSettings"/> and shows no confirmation dialog.
    /// </summary>
    public static class S3BuildUploader
    {
        /// <summary>
        /// Uploads the Android and Standalone zips for <paramref name="id"/> from <c>Limapp-output/</c> to S3.
        /// Returns false (and logs) when credentials are missing, no build exists, or an upload fails.
        /// </summary>
        public static bool TryUploadBuild(int id)
        {
            var config = new S3Config
            {
                AccessKeyId = S3UploaderSettings.AccessKeyId,
                SecretAccessKey = S3UploaderSettings.SecretAccessKey,
                Region = S3UploaderSettings.Region,
                Bucket = S3UploaderSettings.Bucket,
            };

            if (string.IsNullOrEmpty(config.AccessKeyId) || string.IsNullOrEmpty(config.SecretAccessKey) ||
                string.IsNullOrEmpty(config.Region) || string.IsNullOrEmpty(config.Bucket))
            {
                Debug.LogWarning("[Liminal.S3] Auto-upload skipped: credentials/bucket not set (Build Window > Upload tab).");
                return false;
            }

            var outputRoot = Path.Combine(GetProjectRoot(), "Limapp-output");
            var androidZip = Path.Combine(outputRoot, "Android", id + ".zip").Replace('\\', '/');
            var standaloneZip = Path.Combine(outputRoot, "Standalone", id + ".zip").Replace('\\', '/');
            var hasAndroid = File.Exists(androidZip);

            var items = new List<(string zip, string key)>();
            if (hasAndroid)
                items.Add((androidZip, BuildKey("Android", id)));

            // Standalone falls back to the Android build when no Standalone build exists.
            var standaloneSource = File.Exists(standaloneZip) ? standaloneZip : (hasAndroid ? androidZip : null);
            if (standaloneSource != null)
                items.Add((standaloneSource, BuildKey("Standalone", id)));

            if (items.Count == 0)
            {
                Debug.LogWarning($"[Liminal.S3] Auto-upload skipped: no Android or Standalone build found for id {id}.");
                return false;
            }

            var cannedAcl = S3UploaderSettings.PublicRead ? "public-read" : null;
            var allOk = true;

            try
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var (zip, key) = items[i];
                    var label = $"{Path.GetFileName(zip)} ({i + 1}/{items.Count})";

                    S3UploadResult result;
                    try
                    {
                        result = S3Uploader.PutObjectFromFile(config, key, zip,
                            cannedAcl: cannedAcl,
                            onProgress: (sent, total) =>
                            {
                                var pct = total > 0 ? (float)sent / total : 0f;
                                EditorUtility.DisplayProgressBar("Uploading to S3",
                                    $"{label}  {ToMb(sent):0.0}/{ToMb(total):0.0} MB", pct);
                            });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        allOk = false;
                        continue;
                    }

                    if (result.Success)
                    {
                        Debug.Log($"[Liminal.S3] Uploaded '{zip}' → {result.ObjectUrl} ({(int)result.StatusCode}).");
                    }
                    else
                    {
                        Debug.LogError($"[Liminal.S3] Upload failed ({(int)result.StatusCode}) for {key}: {result.Error}");
                        allOk = false;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return allOk;
        }

        private static string BuildKey(string platform, int id)
        {
            var folder = (S3UploaderSettings.Folder ?? string.Empty).Trim('/', '\\');
            var fileName = id + ".zip";
            return string.Join("/", new[] { folder, platform, fileName }.Where(p => !string.IsNullOrEmpty(p)));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
        }

        private static float ToMb(long bytes) => bytes / 1024f / 1024f;
    }
}
