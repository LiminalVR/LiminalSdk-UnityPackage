using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// "Upload" tab for the Build Settings window. Picks a built limapp <c>.zip</c> from
    /// <c>Limapp-output/</c> (or any file via the picker) and PUTs it to the configured S3 bucket
    /// using <see cref="S3Uploader"/>. Credentials and target settings live in <see cref="S3UploaderSettings"/>.
    /// </summary>
    public class S3UploaderWindow : BaseWindowDrawer
    {
        private string[] _foundZips = Array.Empty<string>();
        private string _selectedZip;

        // S3 destination keys for each platform, defaulted from the selected build's id. Once the user edits
        // a field we stop auto-deriving — tracked via the id the paths were last derived from.
        private string _androidKey = string.Empty;
        private string _standaloneKey = string.Empty;
        private string _pathSourceId;

        public override void OnEnabled()
        {
            RefreshZips();
        }

        public override void Draw(BuildWindowConfig config)
        {
            DrawCredentials();
            GUILayout.Space(10);
            DrawSource();
            GUILayout.Space(10);
            DrawUpload();
        }

        private void DrawCredentials()
        {
            EditorGUIHelper.DrawTitle("S3 Credentials");
            EditorGUILayout.LabelField(
                "Stored per-machine in EditorPrefs. The secret is saved in plaintext — use a dedicated, " +
                "least-privilege upload key.", EditorStyles.wordWrappedMiniLabel);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                S3UploaderSettings.AccessKeyId = EditorGUILayout.TextField("Access Key Id", S3UploaderSettings.AccessKeyId);
                S3UploaderSettings.SecretAccessKey = EditorGUILayout.PasswordField("Secret Access Key", S3UploaderSettings.SecretAccessKey);
                S3UploaderSettings.Region = EditorGUILayout.TextField("Region", S3UploaderSettings.Region);
                S3UploaderSettings.Bucket = EditorGUILayout.TextField("Bucket", S3UploaderSettings.Bucket);
                S3UploaderSettings.Folder = EditorGUILayout.TextField("Folder", S3UploaderSettings.Folder);

                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(
                        new GUIContent("Reset to defaults", "Resets Region, Bucket, Folder and Public read. Credentials are kept."),
                        GUILayout.Width(130)))
                {
                    S3UploaderSettings.ResetTargetDefaults();
                    _pathSourceId = null;             // force the path fields to re-derive from the new folder
                    GUIUtility.keyboardControl = 0;   // drop focus so edited text buffers don't re-save old values
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawSource()
        {
            EditorGUIHelper.DrawTitle("Source Zip");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Builds in Limapp-output/", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                    RefreshZips();
                EditorGUILayout.EndHorizontal();

                if (_foundZips.Length == 0)
                {
                    EditorGUILayout.LabelField("No zips found under Limapp-output/. Build a limapp first, or pick a file below.",
                        EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    var projectRoot = GetProjectRoot();
                    foreach (var zip in _foundZips)
                    {
                        var selected = zip == _selectedZip;
                        EditorGUILayout.BeginHorizontal();
                        var newSelected = EditorGUILayout.ToggleLeft(MakeRelative(zip, projectRoot), selected);
                        if (newSelected && !selected)
                            SetSelectedZip(zip);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(DescribeFile(zip), EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("File", GUILayout.Width(110));
                var typed = EditorGUILayout.TextField(_selectedZip ?? string.Empty);
                if (typed != (_selectedZip ?? string.Empty))
                    SetSelectedZip(typed);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFilePanel("Select zip to upload", GetProjectRoot(), "zip");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        SetSelectedZip(picked.Replace('\\', '/'));
                        GUIUtility.keyboardControl = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawUpload()
        {
            EditorGUIHelper.DrawTitle("Upload");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EnsureDefaultPaths();

                var id = CurrentId();
                var hasAndroid = File.Exists(PlatformZip("Android", id));
                var hasStandalone = File.Exists(PlatformZip("Standalone", id));

                DrawPathField("Android Path", ref _androidKey, hasAndroid ? "✓ built" : "not built");
                DrawPathField("Standalone Path", ref _standaloneKey,
                    hasStandalone ? "✓ built" : (hasAndroid ? "↳ uses Android" : "not built"));

                var host = $"{S3UploaderSettings.Bucket}.s3.{S3UploaderSettings.Region}.amazonaws.com";
                EditorGUILayout.LabelField("Bucket", $"https://{host}/", EditorStyles.miniLabel);

                GUILayout.Space(4);

                S3UploaderSettings.PublicRead = EditorGUILayout.ToggleLeft(
                    new GUIContent("Public read (x-amz-acl: public-read)",
                        "Marks the uploaded object publicly readable. Fails if the bucket has ACLs disabled " +
                        "(Object Ownership = Bucket owner enforced) — use a bucket policy in that case."),
                    S3UploaderSettings.PublicRead);

                GUILayout.Space(4);

                using (new EditorGUI.DisabledScope(!CanUpload()))
                {
                    if (GUILayout.Button("Upload to S3", GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f)))
                        Upload();
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>One destination-key row with a short status note for the local source.</summary>
        private void DrawPathField(string label, ref string key, string status)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var newKey = EditorGUILayout.TextField(key);
            if (newKey != key)
            {
                key = newKey;
                _pathSourceId = CurrentId(); // user took ownership; stop auto-deriving
            }
            GUILayout.Label(status, EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }

        private bool CanUpload()
        {
            if (string.IsNullOrEmpty(S3UploaderSettings.AccessKeyId) ||
                string.IsNullOrEmpty(S3UploaderSettings.SecretAccessKey) ||
                string.IsNullOrEmpty(S3UploaderSettings.Region) ||
                string.IsNullOrEmpty(S3UploaderSettings.Bucket))
                return false;

            var id = CurrentId();
            return File.Exists(PlatformZip("Android", id)) || File.Exists(PlatformZip("Standalone", id));
        }

        /// <summary>
        /// Uploads the local Android and Standalone builds for the selected id, each to its path field.
        /// Platforms with no local build are skipped.
        /// </summary>
        private void Upload()
        {
            var id = CurrentId();
            var items = new List<UploadItem>();

            var androidZip = PlatformZip("Android", id);
            var hasAndroid = File.Exists(androidZip);
            if (hasAndroid && !string.IsNullOrEmpty(_androidKey))
                items.Add(new UploadItem { Zip = androidZip, Key = _androidKey });

            // Standalone falls back to the Android build when no Standalone build exists, so you don't have
            // to switch platforms just to produce one. (Matches the deploy 'Copy Latest Build' behaviour.)
            var standaloneZip = PlatformZip("Standalone", id);
            var standaloneSource = File.Exists(standaloneZip) ? standaloneZip : (hasAndroid ? androidZip : null);
            if (standaloneSource != null && !string.IsNullOrEmpty(_standaloneKey))
                items.Add(new UploadItem { Zip = standaloneSource, Key = _standaloneKey });

            if (items.Count == 0)
            {
                EditorUtility.DisplayDialog("Upload to S3",
                    $"No Android or Standalone build found for id '{id}' under Limapp-output/.", "OK");
                return;
            }

            ConfirmAndRun(items);
        }

        private void ConfirmAndRun(List<UploadItem> items)
        {
            var config = new S3Config
            {
                AccessKeyId = S3UploaderSettings.AccessKeyId,
                SecretAccessKey = S3UploaderSettings.SecretAccessKey,
                Region = S3UploaderSettings.Region,
                Bucket = S3UploaderSettings.Bucket,
            };

            var lines = string.Join("\n", items.Select(i => $"{Path.GetFileName(i.Zip)}  →  {i.Key}"));
            if (!EditorUtility.DisplayDialog("Upload to S3",
                    $"Upload {items.Count} file(s) to s3://{config.Bucket} ({config.Region})?\n\n{lines}",
                    "Upload", "Cancel"))
                return;

            var cannedAcl = S3UploaderSettings.PublicRead ? "public-read" : null;
            var succeeded = new List<string>();
            var failed = new List<string>();

            try
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var label = $"{Path.GetFileName(item.Zip)} ({i + 1}/{items.Count})";

                    S3UploadResult result;
                    try
                    {
                        result = S3Uploader.PutObjectFromFile(config, item.Key, item.Zip,
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
                        failed.Add($"{item.Key}: {ex.Message}");
                        continue;
                    }

                    if (result.Success)
                    {
                        Debug.Log($"[Liminal.S3] Uploaded '{item.Zip}' → {result.ObjectUrl} ({(int)result.StatusCode}).");
                        succeeded.Add(result.ObjectUrl);
                    }
                    else
                    {
                        Debug.LogError($"[Liminal.S3] Upload failed ({(int)result.StatusCode}) for {item.Key}: {result.Error}");
                        failed.Add($"{item.Key}: {(int)result.StatusCode} {result.Error}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var summary = new StringBuilder();
            if (succeeded.Count > 0)
            {
                summary.AppendLine($"Uploaded {succeeded.Count}:");
                foreach (var url in succeeded)
                    summary.AppendLine(url);
            }
            if (failed.Count > 0)
            {
                if (summary.Length > 0)
                    summary.AppendLine();
                summary.AppendLine($"Failed {failed.Count}:");
                foreach (var f in failed)
                    summary.AppendLine(f);
            }

            EditorUtility.DisplayDialog(
                failed.Count == 0 ? "Upload complete" : "Upload finished with errors",
                summary.ToString(), "OK");
        }

        private struct UploadItem
        {
            public string Zip;
            public string Key;
        }

        private void RefreshZips()
        {
            var root = Path.Combine(GetProjectRoot(), "Limapp-output");
            _foundZips = Directory.Exists(root)
                ? Directory.GetFiles(root, "*.zip", SearchOption.AllDirectories)
                    .Select(p => p.Replace('\\', '/'))
                    .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
                    .ToArray()
                : Array.Empty<string>();

            // Keep a still-valid selection; otherwise default to the most recently built zip.
            if (string.IsNullOrEmpty(_selectedZip) || !File.Exists(_selectedZip))
                SetSelectedZip(_foundZips.FirstOrDefault());
        }

        private void SetSelectedZip(string zip)
        {
            _selectedZip = zip;
        }

        /// <summary>The build id (zip filename without extension) of the current selection, e.g. "24".</summary>
        private string CurrentId()
        {
            return string.IsNullOrEmpty(_selectedZip) ? string.Empty : Path.GetFileNameWithoutExtension(_selectedZip);
        }

        /// <summary>Local path to <c>Limapp-output/&lt;platform&gt;/&lt;id&gt;.zip</c> (empty when no id).</summary>
        private static string PlatformZip(string platform, string id)
        {
            return string.IsNullOrEmpty(id)
                ? string.Empty
                : Path.Combine(GetProjectRoot(), "Limapp-output", platform, id + ".zip").Replace('\\', '/');
        }

        /// <summary>Re-derives both path fields when the selected id changes, until the user edits them.</summary>
        private void EnsureDefaultPaths()
        {
            var id = CurrentId();
            if (id == _pathSourceId)
                return;

            _androidKey = BuildKey("Android", id);
            _standaloneKey = BuildKey("Standalone", id);
            _pathSourceId = id;
        }

        /// <summary>Builds <c>&lt;folder&gt;/&lt;platform&gt;/&lt;id&gt;.zip</c>, e.g. <c>app/Limapp/v3/Android/24.zip</c>.</summary>
        private static string BuildKey(string platform, string id)
        {
            var folder = (S3UploaderSettings.Folder ?? string.Empty).Trim('/', '\\');
            var fileName = string.IsNullOrEmpty(id) ? string.Empty : id + ".zip";
            return string.Join("/", new[] { folder, platform, fileName }.Where(p => !string.IsNullOrEmpty(p)));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
        }

        private static string MakeRelative(string fullPath, string root)
        {
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(root.Length).TrimStart('/', '\\')
                : fullPath;
        }

        private static string DescribeFile(string path)
        {
            var info = new FileInfo(path);
            return $"{ToMb(info.Length):0.0} MB · {info.LastWriteTime:g}";
        }

        private static float ToMb(long bytes) => bytes / 1024f / 1024f;
    }
}
