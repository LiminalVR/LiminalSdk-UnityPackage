using System;
using UnityEditor;
using UnityEngine;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Disables 'Auto Reference' on managed DLLs that get imported under <c>Assets/</c>. With auto-reference
    /// on, every predefined assembly references the DLL, which can leak references into the limapp build
    /// and cause type-resolution clashes against assemblies the Platform App already loads. asmdefs that
    /// genuinely need the DLL can list it in their <c>precompiledReferences</c>.
    /// </summary>
    internal class LimappPluginAutoReferenceProcessor : AssetPostprocessor
    {
        // PluginImporter does not expose Auto Reference as a public property — it's only accessible via
        // SerializedObject. The YAML field is `isExplicitlyReferenced` (true == Auto Reference OFF).
        private const string ExplicitRefProp = "m_IsExplicitlyReferenced";

        private void OnPreprocessAsset()
        {
            if (!(assetImporter is PluginImporter plugin))
                return;

            if (!ShouldFix(assetPath))
                return;

            if (TrySetExplicit(plugin))
                Debug.Log($"[LimappPlugin] Disabled 'Auto Reference' on {assetPath}.");
        }

        private static bool ShouldFix(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return false;
            // Leave the SDK's own DLLs alone.
            if (path.StartsWith("Assets/Liminal/", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        /// <summary>
        /// Flips <c>m_IsExplicitlyReferenced</c> to true on the given importer when it's currently false.
        /// Returns true when the property was changed. False means either already set, or the property
        /// is unavailable on this Unity version.
        /// </summary>
        private static bool TrySetExplicit(PluginImporter plugin)
        {
            var so = new SerializedObject(plugin);
            var prop = so.FindProperty(ExplicitRefProp);
            if (prop == null)
            {
                Debug.LogWarning(
                    $"[LimappPlugin] '{ExplicitRefProp}' not found on PluginImporter — Auto Reference cannot be toggled on this Unity version.");
                return false;
            }

            if (prop.boolValue)
                return false;

            prop.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
