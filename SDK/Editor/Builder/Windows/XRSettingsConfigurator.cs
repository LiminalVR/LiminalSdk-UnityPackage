using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Liminal.SDK.Build
{
    internal static class XRSettingsConfigurator
    {
        private const string OculusLoaderTypeName = "Unity.XR.Oculus.OculusLoader";
        private const string SettingsFolder = "Assets/XR";
        private const string SettingsAssetPath = SettingsFolder + "/XRGeneralSettings.asset";

        // XRDeviceSimulatorSettings is internal — accessed reflectively to avoid an InternalsVisibleTo dance.
        private const string SimulatorSettingsTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings, Unity.XR.Interaction.Toolkit";
        private const string SimulatorSettingsFieldName = "m_AutomaticallyInstantiateSimulatorPrefab";

        public static void ConfigureOculusForAndroidAndStandalone()
        {
            var perTarget = GetOrCreatePerBuildTargetSettings();
            ConfigureForBuildTarget(perTarget, BuildTargetGroup.Android);
            ConfigureForBuildTarget(perTarget, BuildTargetGroup.Standalone);

            EnableXRInteractionSimulator();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// True when both Android and Standalone have the Oculus loader assigned and 'Initialize XR on Startup'
        /// enabled. Surfaced as the "Done" badge in the Setup wizard.
        /// </summary>
        public static bool IsOculusConfiguredForAndroidAndStandalone()
        {
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var perTarget) || perTarget == null)
                return false;

            return IsOculusConfiguredForTarget(perTarget, BuildTargetGroup.Android)
                && IsOculusConfiguredForTarget(perTarget, BuildTargetGroup.Standalone);
        }

        private static bool IsOculusConfiguredForTarget(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup target)
        {
            var settings = perTarget.SettingsForBuildTarget(target);
            if (settings == null || !settings.InitManagerOnStart || settings.Manager == null)
                return false;

            foreach (var loader in settings.Manager.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == OculusLoaderTypeName)
                    return true;
            }
            return false;
        }

        private static void EnableXRInteractionSimulator()
        {
            var type = Type.GetType(SimulatorSettingsTypeName);
            if (type == null)
            {
                Debug.LogWarning("[XRSetup] XR Interaction Toolkit not found — skipping simulator setting.");
                return;
            }

            // ScriptableSettings<T>.Instance lazily creates the asset if missing.
            var instanceProp = type.BaseType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var asset = instanceProp?.GetValue(null) as ScriptableObject;
            if (asset == null)
            {
                Debug.LogWarning("[XRSetup] Could not load XRDeviceSimulatorSettings — skipping simulator setting.");
                return;
            }

            var so = new SerializedObject(asset);
            var prop = so.FindProperty(SimulatorSettingsFieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[XRSetup] Field '{SimulatorSettingsFieldName}' not found on XRDeviceSimulatorSettings — XRI version mismatch?");
                return;
            }

            prop.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            Debug.Log("[XRSetup] 'Use XR Interaction Simulator in scenes' enabled.");
        }

        private static void ConfigureForBuildTarget(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup target)
        {
            var settings = perTarget.SettingsForBuildTarget(target);
            var assetPath = AssetDatabase.GetAssetPath(perTarget);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = $"{target} Settings";
                AssetDatabase.AddObjectToAsset(settings, assetPath);
                perTarget.SetSettingsForBuildTarget(target, settings);
            }

            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = $"{target} Providers";
                AssetDatabase.AddObjectToAsset(manager, assetPath);
                settings.Manager = manager;
            }

            settings.InitManagerOnStart = true;

            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, OculusLoaderTypeName, target))
            {
                Debug.LogError($"[XRSetup] Failed to assign Oculus loader for {target}. Is com.unity.xr.oculus installed?");
                return;
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
            EditorUtility.SetDirty(perTarget);

            Debug.Log($"[XRSetup] {target}: Oculus loader assigned, Initialize XR on Startup = true.");
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreatePerBuildTargetSettings()
        {
            EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var perTarget);
            if (perTarget != null)
                return perTarget;

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets", "XR");

            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, SettingsAssetPath);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            return perTarget;
        }
    }
}
