using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liminal.SDK.Tools
{
    /// <summary>
    /// One-click migration from a local copy of the reflection setup (scripts/shaders/textures
    /// duplicated under Assets, typically in a "_ReflectionSetup" folder) to the Reflection System
    /// shipped with the Liminal SDK package.
    ///
    /// - References to local copies of SDK scripts/shaders (and byte-identical textures/meshes)
    ///   are repointed to the SDK package assets by rewriting GUIDs in the serialized files.
    /// - Project-tuned assets (materials, prefabs, and any texture whose content differs from the
    ///   SDK copy) keep their GUIDs and are moved out of the legacy folder, so scene references
    ///   and all planar shader settings (ramp, colors, ripple, strengths, fades, blend modes,
    ///   render queue) are preserved exactly.
    /// - The legacy folder is deleted once everything in it is migrated.
    ///
    /// Only the planar reflection implementation is treated as important; the reflection-probe
    /// fallback assets follow the same keep/repoint/delete rules but get no special reporting.
    /// </summary>
    public class ReflectionMigrationTool : EditorWindow
    {
        private const string TargetFolder = "Assets/Reflection (Migrated)";
        private const string MirrorShaderSdkGuid = "96345e600b1052a42a646fc6be248d32";

        private struct SdkRef
        {
            public readonly string Guid;
            public readonly bool RequireIdenticalBytes;

            public SdkRef(string guid, bool requireIdenticalBytes)
            {
                Guid = guid;
                RequireIdenticalBytes = requireIdenticalBytes;
            }
        }

        // Local file name (numeric copy suffixes stripped) -> the SDK package asset that supersedes it.
        // Textures/meshes are only repointed when byte-identical, so a project-tweaked ramp is never
        // silently replaced by the SDK one.
        private static readonly Dictionary<string, SdkRef> SdkEquivalents = new Dictionary<string, SdkRef>(StringComparer.OrdinalIgnoreCase)
        {
            { "MirrorReflection.cs",      new SdkRef("749c7112cf6f9e54eb5f7ce9db1dc94e", false) },
            { "ReflectionOffsetModel.cs", new SdkRef("9fe27bc10fd646d44870bc892c769e7d", false) },
            { "ReflectionSetup.cs",       new SdkRef("b557b211c898cc843a3c63d7b5ce183b", false) },
            { "XRDeviceUtils.cs",         new SdkRef("456b2b97416717549a9ad45a4966f6eb", false) },
            { "EDeviceModelType.cs",      new SdkRef("1ca2e494b87909842aa16f240008d8c3", false) },
            { "SetReflectionProperty.cs", new SdkRef("1bc01457433fbe345b688cb2505d9c40", false) },
            { "Mirror.shader",            new SdkRef(MirrorShaderSdkGuid, false) },
            { "ReflectiveSurface.shader", new SdkRef("507e5419769c64e00932f47c2307f9fa", false) },
            { "Ramp.psd",                 new SdkRef("3702ae58a677d4c49a2781925a7dfb4d", true) },
            { "Ripple.jpg",               new SdkRef("5e95c80f7bf56e74f9071a7dce126c3d", true) },
            { "PlanarFloor.fbx",          new SdkRef("f1e3fa893d760184c9266c1ce82dde9e", true) },
        };

        // Serialized text formats that can reference other assets by GUID.
        private static readonly HashSet<string> YamlExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".mat", ".asset", ".anim", ".controller", ".overridecontroller",
            ".playable", ".mask", ".signal", ".preset", ".rendertexture", ".spriteatlas",
            ".terrainlayer", ".guiskin", ".flare", ".physicmaterial", ".mixer",
        };

        // Asset types that are safe to delete when nothing references them. Scripts and shaders that
        // have no SDK equivalent are kept instead, since code can use them without a GUID reference.
        private static readonly HashSet<string> DeletableWhenUnreferenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mat", ".prefab", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr",
            ".fbx", ".obj", ".rendertexture", ".cubemap", ".anim",
        };

        // The planar settings that matter, in inspector order: yaml property -> display label.
        private static readonly (string Prop, string Label)[] PlanarFloats =
        {
            ("_RippleStrength", "Ripple Strength"),
            ("_RippleSpeed", "Ripple Speed"),
            ("_ReflectionStrength", "Reflection Strength"),
            ("_FadeDistance", "Fade Distance"),
            ("_FadeScaleX", "Fade Scale X"),
            ("MySrcMode", "SrcMode"),
            ("MyDstMode", "DstMode"),
            ("_EnableTint", "Enable Tint"),
            ("_EnableRampAlpha", "Enable Ramp Alpha"),
        };

        private class LegacyAsset
        {
            public string Path;
            public string Guid;
            public string SdkGuid;
            public string SdkPath;
            public bool Keep;
            public bool Delete;
            public string Note;
            public string Content;
            public readonly List<string> ReferencedBy = new List<string>();
        }

        private class MaterialReport
        {
            public string Path;
            public readonly List<(string Label, string Value)> Settings = new List<(string, string)>();
        }

        private readonly List<string> _legacyFolders = new List<string>();
        private readonly List<LegacyAsset> _assets = new List<LegacyAsset>();
        private readonly Dictionary<string, List<string>> _fileEdits = new Dictionary<string, List<string>>();
        private readonly List<MaterialReport> _materials = new List<MaterialReport>();

        private bool _scanned;
        private bool _sdkOk;
        private string _sdkStatus = "";
        private string _lastResult;
        private Vector2 _scroll;

        [MenuItem("Liminal/Migrate Reflection To SDK")]
        public static void Open()
        {
            var window = GetWindow<ReflectionMigrationTool>("Reflection Migration");
            window.minSize = new Vector2(560, 400);
            window.Show();
        }

        private void OnEnable()
        {
            Scan();
        }

        #region Scan

        private void Scan()
        {
            _legacyFolders.Clear();
            _assets.Clear();
            _fileEdits.Clear();
            _materials.Clear();
            _scanned = false;

            var sdkShaderPath = AssetDatabase.GUIDToAssetPath(MirrorShaderSdkGuid);
            _sdkOk = !string.IsNullOrEmpty(sdkShaderPath);
            _sdkStatus = _sdkOk
                ? $"Liminal SDK Reflection System found: {sdkShaderPath}"
                : "Liminal SDK Reflection System not found in this project. Make sure the com.liminal.sdk package is installed.";

            if (!_sdkOk)
            {
                _scanned = true;
                return;
            }

            try
            {
                var allFiles = CollectAssetFiles();
                FindLegacyFolders(allFiles);

                if (_legacyFolders.Count == 0)
                {
                    _scanned = true;
                    return;
                }

                ClassifyLegacyAssets(allFiles);
                ScanReferences(allFiles);
                ResolveActions();
                BuildMaterialReports();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _scanned = true;
        }

        private static List<string> CollectAssetFiles()
        {
            var dataPath = Application.dataPath.Replace('\\', '/');
            return Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(p => "Assets" + p.Substring(dataPath.Length))
                .Where(p => !p.Contains("/."))
                .ToList();
        }

        private void FindLegacyFolders(List<string> allFiles)
        {
            foreach (var file in allFiles)
            {
                var name = Path.GetFileName(file);
                if (!name.Equals("MirrorReflection.cs", StringComparison.OrdinalIgnoreCase) &&
                    !name.Equals("Mirror.shader", StringComparison.OrdinalIgnoreCase))
                    continue;

                // An embedded copy of the SDK itself is not a legacy folder.
                var guid = AssetDatabase.AssetPathToGUID(file);
                if (SdkEquivalents.Values.Any(s => s.Guid == guid))
                    continue;

                var folder = file.Substring(0, file.LastIndexOf('/'));
                if (!_legacyFolders.Contains(folder))
                    _legacyFolders.Add(folder);
            }

            // Drop folders nested inside another legacy folder.
            _legacyFolders.RemoveAll(f => _legacyFolders.Any(other => other != f && f.StartsWith(other + "/", StringComparison.Ordinal)));
        }

        private bool IsInLegacyFolder(string assetPath)
        {
            return _legacyFolders.Any(f => assetPath.StartsWith(f + "/", StringComparison.Ordinal));
        }

        private void ClassifyLegacyAssets(List<string> allFiles)
        {
            foreach (var file in allFiles.Where(IsInLegacyFolder))
            {
                var asset = new LegacyAsset
                {
                    Path = file,
                    Guid = AssetDatabase.AssetPathToGUID(file),
                };

                if (YamlExtensions.Contains(Path.GetExtension(file)))
                    asset.Content = SafeRead(file);

                // "Mirror 1.shader" and similar duplicate-suffixed copies map to the same SDK asset.
                var baseName = Regex.Replace(Path.GetFileNameWithoutExtension(file), @"\s+\d+$", "") + Path.GetExtension(file);
                if (SdkEquivalents.TryGetValue(baseName, out var sdk))
                {
                    var sdkPath = AssetDatabase.GUIDToAssetPath(sdk.Guid);
                    if (string.IsNullOrEmpty(sdkPath))
                    {
                        asset.Note = "no SDK equivalent found in this project";
                    }
                    else if (sdk.RequireIdenticalBytes && !FilesAreIdentical(file, sdkPath))
                    {
                        asset.Note = "content differs from the SDK copy, keeping the project version";
                    }
                    else
                    {
                        asset.SdkGuid = sdk.Guid;
                        asset.SdkPath = sdkPath;
                        asset.Note = "superseded by the SDK package";
                    }
                }

                _assets.Add(asset);
            }
        }

        private void ScanReferences(List<string> allFiles)
        {
            var corpus = allFiles
                .Where(f => !IsInLegacyFolder(f) && YamlExtensions.Contains(Path.GetExtension(f)))
                .ToList();

            for (int i = 0; i < corpus.Count; i++)
            {
                if (i % 25 == 0)
                    EditorUtility.DisplayProgressBar("Reflection Migration", $"Scanning references ({i}/{corpus.Count})", (float)i / corpus.Count);

                var text = SafeRead(corpus[i]);
                if (text == null)
                    continue;

                foreach (var asset in _assets)
                {
                    if (text.Contains(asset.Guid))
                        asset.ReferencedBy.Add(corpus[i]);
                }
            }
        }

        private void ResolveActions()
        {
            // Anything without an SDK replacement stays in the project if something references it,
            // including references from other kept legacy assets (e.g. prefab -> material -> ramp).
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var asset in _assets.Where(a => a.SdkGuid == null && !a.Keep))
                {
                    var referenced = asset.ReferencedBy.Count > 0;
                    if (!referenced)
                    {
                        var keeper = _assets.FirstOrDefault(k => k.Keep && k.Content != null && k.Content.Contains(asset.Guid));
                        if (keeper != null)
                        {
                            asset.ReferencedBy.Add(keeper.Path);
                            referenced = true;
                        }
                    }

                    if (referenced)
                    {
                        asset.Keep = true;
                        changed = true;
                    }
                }
            }

            foreach (var asset in _assets.Where(a => a.SdkGuid == null && !a.Keep))
            {
                if (DeletableWhenUnreferenced.Contains(Path.GetExtension(asset.Path)))
                {
                    asset.Delete = true;
                    asset.Note = "not referenced anywhere in the project";
                }
                else
                {
                    asset.Keep = true;
                    asset.Note = "not referenced by any asset, kept for safety (may be used from code)";
                }
            }

            foreach (var asset in _assets.Where(a => a.SdkGuid != null))
            {
                asset.Delete = true;

                var referencing = new HashSet<string>(asset.ReferencedBy);
                foreach (var keeper in _assets.Where(k => k.Keep && k.Content != null && k.Content.Contains(asset.Guid)))
                    referencing.Add(keeper.Path);

                foreach (var file in referencing)
                {
                    if (!_fileEdits.TryGetValue(file, out var edits))
                        _fileEdits[file] = edits = new List<string>();
                    edits.Add($"{Path.GetFileName(asset.Path)} → {asset.SdkPath}");
                }
            }
        }

        private void BuildMaterialReports()
        {
            var mirrorShaderGuids = new HashSet<string> { MirrorShaderSdkGuid };
            foreach (var asset in _assets.Where(a => a.SdkGuid == MirrorShaderSdkGuid))
                mirrorShaderGuids.Add(asset.Guid);

            var candidates = new List<(string Path, string Text)>();
            foreach (var asset in _assets.Where(a => a.Keep && a.Content != null && a.Path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                candidates.Add((asset.Path, asset.Content));

            foreach (var file in _fileEdits.Keys.Where(f => f.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
            {
                if (candidates.Any(c => c.Path == file))
                    continue;
                var text = SafeRead(file);
                if (text != null)
                    candidates.Add((file, text));
            }

            foreach (var (path, text) in candidates)
            {
                var shaderGuid = Match1(text, @"m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})");
                if (shaderGuid == null || !mirrorShaderGuids.Contains(shaderGuid))
                    continue;

                var report = new MaterialReport { Path = path };
                report.Settings.Add(("Shader", shaderGuid == MirrorShaderSdkGuid
                    ? "FX/MirrorReflection (SDK)"
                    : "FX/MirrorReflection (local copy, will repoint to SDK)"));

                AddTextureSetting(report, text, "_RampTex", "Ramp");
                AddColorSetting(report, text, "_Color", "Color");
                AddColorSetting(report, text, "_ColorHorizon", "Horizon Color");
                AddTextureSetting(report, text, "_RippleTex", "Ripple");

                foreach (var (prop, label) in PlanarFloats)
                {
                    var value = Match1(text, @"-\s*" + Regex.Escape(prop) + @":\s*([-+0-9.eE]+)");
                    if (value != null)
                        report.Settings.Add((label, value));
                }

                var queue = Match1(text, @"m_CustomRenderQueue:\s*(-?\d+)");
                if (queue != null)
                    report.Settings.Add(("Render Queue", queue));

                _materials.Add(report);
            }
        }

        private void AddTextureSetting(MaterialReport report, string yaml, string prop, string label)
        {
            var pattern = @"-\s*" + Regex.Escape(prop) + @":\s*\r?\n" +
                          @"\s*m_Texture:\s*\{fileID:\s*-?\d+(?:,\s*guid:\s*([0-9a-f]{32}))?(?:,\s*type:\s*\d+)?\}\s*\r?\n" +
                          @"\s*m_Scale:\s*\{x:\s*([^,]+),\s*y:\s*([^}]+)\}\s*\r?\n" +
                          @"\s*m_Offset:\s*\{x:\s*([^,]+),\s*y:\s*([^}]+)\}";
            var match = Regex.Match(yaml, pattern);
            if (!match.Success)
                return;

            var guid = match.Groups[1].Success ? match.Groups[1].Value : null;
            string display;
            if (guid == null)
            {
                display = "None";
            }
            else
            {
                display = Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid));
                var legacy = _assets.FirstOrDefault(a => a.Guid == guid);
                if (legacy != null)
                    display += legacy.SdkGuid != null ? " (local copy, will repoint to SDK)" : " (project copy, kept)";
            }

            report.Settings.Add((label, $"{display}   Tiling ({match.Groups[2].Value.Trim()}, {match.Groups[3].Value.Trim()})   Offset ({match.Groups[4].Value.Trim()}, {match.Groups[5].Value.Trim()})"));
        }

        private static void AddColorSetting(MaterialReport report, string yaml, string prop, string label)
        {
            var match = Regex.Match(yaml, @"-\s*" + Regex.Escape(prop) + @":\s*\{r:\s*([^,]+),\s*g:\s*([^,]+),\s*b:\s*([^,]+),\s*a:\s*([^}]+)\}");
            if (match.Success)
                report.Settings.Add((label, $"RGBA({match.Groups[1].Value.Trim()}, {match.Groups[2].Value.Trim()}, {match.Groups[3].Value.Trim()}, {match.Groups[4].Value.Trim()})"));
        }

        private static string Match1(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string SafeRead(string path)
        {
            try
            {
                if (new FileInfo(path).Length > 48 * 1024 * 1024)
                    return null;
                return File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private static bool FilesAreIdentical(string assetPathA, string assetPathB)
        {
            try
            {
                var a = File.ReadAllBytes(FileUtil.GetPhysicalPath(assetPathA));
                var b = File.ReadAllBytes(FileUtil.GetPhysicalPath(assetPathB));
                return a.Length == b.Length && a.SequenceEqual(b);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Migrate

        private void Migrate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var report = new StringBuilder();
            report.AppendLine("=== Liminal Reflection Migration ===");

            var moved = _assets.Where(a => a.Keep).ToList();
            var deleted = _assets.Where(a => a.Delete).ToList();
            var newPaths = new Dictionary<string, string>();

            try
            {
                if (moved.Count > 0 && !AssetDatabase.IsValidFolder(TargetFolder))
                    AssetDatabase.CreateFolder("Assets", TargetFolder.Substring("Assets/".Length));

                foreach (var asset in moved)
                {
                    var destination = AssetDatabase.GenerateUniqueAssetPath($"{TargetFolder}/{Path.GetFileName(asset.Path)}");
                    var error = AssetDatabase.MoveAsset(asset.Path, destination);
                    if (!string.IsNullOrEmpty(error))
                    {
                        report.AppendLine($"MOVE FAILED  {asset.Path}: {error}");
                        continue;
                    }

                    newPaths[asset.Path] = destination;
                    report.AppendLine($"MOVED    {asset.Path} → {destination}");
                }

                var remaps = _assets.Where(a => a.SdkGuid != null).ToList();
                foreach (var pair in _fileEdits)
                {
                    var actualPath = newPaths.TryGetValue(pair.Key, out var movedPath) ? movedPath : pair.Key;
                    var text = File.ReadAllText(actualPath);
                    foreach (var remap in remaps)
                        text = text.Replace(remap.Guid, remap.SdkGuid);
                    File.WriteAllText(actualPath, text, new UTF8Encoding(false));
                    report.AppendLine($"REPOINTED {actualPath}: {string.Join("; ", pair.Value)}");
                }

                foreach (var asset in deleted)
                {
                    AssetDatabase.DeleteAsset(asset.Path);
                    report.AppendLine($"DELETED  {asset.Path} ({asset.Note})");
                }

                foreach (var folder in _legacyFolders)
                {
                    if (!Directory.Exists(folder))
                        continue;

                    var leftovers = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                        .Any(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
                    if (leftovers)
                    {
                        report.AppendLine($"KEPT     {folder} (still contains other assets)");
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(folder);
                        report.AppendLine($"DELETED  {folder}");
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
            }

            // Reload any open scene that was rewritten on disk so the editor picks up the changes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(scene.path) && _fileEdits.ContainsKey(scene.path))
                {
                    EditorSceneManager.OpenScene(scene.path);
                    break;
                }
            }

            _lastResult = $"Migration complete: {moved.Count} asset(s) moved to '{TargetFolder}', " +
                          $"{_fileEdits.Count} file(s) repointed to the SDK, {deleted.Count} asset(s) deleted.";
            report.AppendLine(_lastResult);
            Debug.Log(report.ToString());

            Scan();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_sdkStatus, _sdkOk ? MessageType.Info : MessageType.Error);

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);

            if (!_sdkOk || !_scanned)
                return;

            if (_legacyFolders.Count == 0)
            {
                EditorGUILayout.HelpBox("Nothing to migrate. No local copies of the reflection system were found under Assets, this project already uses the SDK Reflection System.", MessageType.Info);
                if (GUILayout.Button("Rescan"))
                    Scan();
                return;
            }

            EditorGUILayout.LabelField($"Legacy reflection folder(s): {string.Join(", ", _legacyFolders)}", EditorStyles.wordWrappedLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var moved = _assets.Where(a => a.Keep).ToList();
            var deleted = _assets.Where(a => a.Delete).ToList();

            if (_materials.Count > 0)
            {
                DrawHeader($"Planar reflection materials, settings preserved ({_materials.Count})");
                foreach (var material in _materials)
                {
                    EditorGUILayout.LabelField(material.Path, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var (label, value) in material.Settings)
                        EditorGUILayout.LabelField(label, value);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(4);
                }
            }

            if (_fileEdits.Count > 0)
            {
                DrawHeader($"References repointed to the SDK package ({_fileEdits.Count} file(s))");
                foreach (var pair in _fileEdits)
                {
                    EditorGUILayout.LabelField(pair.Key, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var edit in pair.Value)
                        EditorGUILayout.LabelField(edit, EditorStyles.wordWrappedMiniLabel);
                    EditorGUI.indentLevel--;
                }
            }

            if (moved.Count > 0)
            {
                DrawHeader($"Project assets kept, moved to '{TargetFolder}' ({moved.Count})");
                foreach (var asset in moved)
                {
                    var reason = asset.Note ?? $"referenced by {asset.ReferencedBy.Count} asset(s)";
                    EditorGUILayout.LabelField($"{asset.Path}  —  {reason}", EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (deleted.Count > 0)
            {
                DrawHeader($"Assets deleted ({deleted.Count})");
                foreach (var asset in deleted)
                    EditorGUILayout.LabelField($"{asset.Path}  —  {asset.Note}", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("This rewrites files on disk and cannot be undone from the editor. Make sure the project is committed or backed up first.", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                var color = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
                if (GUILayout.Button("Migrate Now", GUILayout.Height(32)))
                    Migrate();
                GUI.backgroundColor = color;

                if (GUILayout.Button("Rescan", GUILayout.Height(32), GUILayout.Width(100)))
                    Scan();
            }

            EditorGUILayout.Space(4);
        }

        private static void DrawHeader(string text)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #endregion
    }
}
