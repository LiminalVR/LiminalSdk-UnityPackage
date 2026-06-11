using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Liminal.SDK.Build
{
    internal static class XRSettingsConfigurator
    {
        private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string OculusLoaderTypeName = "Unity.XR.Oculus.OculusLoader";
        private const string SettingsFolder = "Assets/XR";
        private const string SettingsAssetPath = SettingsFolder + "/XRGeneralSettings.asset";

        // XRDeviceSimulatorSettings is internal — accessed reflectively to avoid an InternalsVisibleTo dance.
        private const string SimulatorSettingsTypeName = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings, Unity.XR.Interaction.Toolkit";
        private const string SimulatorSettingsFieldName = "m_AutomaticallyInstantiateSimulatorPrefab";

        // The Liminal platform baseline: interaction profiles (Oculus Touch, Hand Interaction,
        // Meta Quest Touch Plus) plus hand-tracking features. MetaQuestFeature is Android-only,
        // so it's appended per-target in RequiredFeaturesFor.
        private static readonly Type[] CommonFeatureTypes =
        {
            typeof(OculusTouchControllerProfile),
            typeof(HandInteractionProfile),
            typeof(MetaQuestTouchPlusControllerProfile),
            typeof(HandCommonPosesInteraction),   // "Hand Interaction Poses"
            typeof(PalmPoseInteraction),          // "Palm Pose"
            typeof(HandTracking),                 // "Hand Tracking Subsystem"
            typeof(MetaHandTrackingAim),          // "Meta Hand Tracking Aim"
        };

        // Scripting defines recommended by OpenXR project validation: switch bindings to
        // InputSystem.XR.PoseControl and StickControl thumbsticks. Safe here — no project code
        // depends on the legacy OpenXR.Input.PoseControl / Vector2Control types.
        private static readonly string[] ValidationDefines =
        {
            "USE_INPUT_SYSTEM_POSE_CONTROL",
            "USE_STICK_CONTROL_THUMBSTICKS",
        };

        private static readonly NamedBuildTarget[] DefineTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.Standalone,
        };

        public static void ConfigureOpenXRForAndroidAndStandalone()
        {
            var perTarget = GetOrCreatePerBuildTargetSettings();
            ConfigureForBuildTarget(perTarget, BuildTargetGroup.Android);
            ConfigureForBuildTarget(perTarget, BuildTargetGroup.Standalone);

            // After the per-target passes so the OpenXR settings assets exist.
            ApplyValidationFixes();

            EnableXRInteractionSimulator();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Applies the same fixes as the OpenXR / Meta Quest project-validation FixIt buttons
        /// (Project Validation window), mirroring the package implementations.
        /// </summary>
        private static void ApplyValidationFixes()
        {
            // [OpenXR] "Only arm64 or x86_x64 is supported on Android with OpenXR."
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

#if UNITY_6000_0_OR_NEWER
            // [Meta Quest Support] "Application Entry Point is required to set to Game Activity for Unity 6.0+."
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
#endif

            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (androidOpenXRSettings != null)
            {
                // [Meta Quest Support] "Latency Optimization is recommended to be set to Prioritize Input Polling."
                androidOpenXRSettings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
                EditorUtility.SetDirty(androidOpenXRSettings);

                // [Meta Quest Support] "Optimize Buffer Discards is only supported on Vulkan graphics API."
                // The Liminal platform requires GLES3 (enforced by the Configure Android step), so resolve
                // this by disabling the Vulkan-only optimization instead of switching graphics APIs.
                SetOptimizeBufferDiscards(androidOpenXRSettings, false);
            }

            // [OpenXR] PoseControl / StickControl recommendations.
            foreach (var target in DefineTargets)
                foreach (var define in ValidationDefines)
                    AddScriptingDefine(target, define);

            Debug.Log("[XRSetup] Validation fixes applied: IL2CPP + ARM64, Game Activity entry point, Prioritize Input Polling, Optimize Buffer Discards disabled, PoseControl/StickControl defines.");
        }

        // m_optimizeBufferDiscards is internal on MetaQuestFeature — accessed via SerializedObject.
        private const string OptimizeBufferDiscardsFieldName = "m_optimizeBufferDiscards";

        private static void SetOptimizeBufferDiscards(OpenXRSettings androidOpenXRSettings, bool value)
        {
            var questFeature = androidOpenXRSettings.GetFeature(typeof(MetaQuestFeature));
            if (questFeature == null)
                return;

            var so = new SerializedObject(questFeature);
            var prop = so.FindProperty(OptimizeBufferDiscardsFieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[XRSetup] '{OptimizeBufferDiscardsFieldName}' not found on MetaQuestFeature — OpenXR package layout changed?");
                return;
            }

            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(questFeature);
        }

        private static bool IsOptimizeBufferDiscardsDisabled(OpenXRSettings androidOpenXRSettings)
        {
            var questFeature = androidOpenXRSettings.GetFeature(typeof(MetaQuestFeature));
            if (questFeature == null)
                return true;

            var prop = new SerializedObject(questFeature).FindProperty(OptimizeBufferDiscardsFieldName);
            return prop == null || !prop.boolValue;
        }

        private static void AddScriptingDefine(NamedBuildTarget target, string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (HasDefine(defines, define))
                return;

            defines = string.IsNullOrEmpty(defines) ? define : defines + ";" + define;
            PlayerSettings.SetScriptingDefineSymbols(target, defines);
        }

        private static bool HasDefine(string defines, string define)
        {
            return defines.Split(';').Any(d => d.Trim() == define);
        }

        /// <summary>
        /// True when both Android and Standalone have the OpenXR loader assigned, 'Initialize XR on Startup'
        /// enabled, all required OpenXR interaction profiles / features enabled, and the project-validation
        /// fixes applied. Surfaced as the "Done" badge in the Setup wizard.
        /// </summary>
        public static bool IsOpenXRConfiguredForAndroidAndStandalone()
        {
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var perTarget) || perTarget == null)
                return false;

            return IsConfiguredForTarget(perTarget, BuildTargetGroup.Android)
                && IsConfiguredForTarget(perTarget, BuildTargetGroup.Standalone)
                && AreValidationFixesApplied();
        }

        private static bool AreValidationFixesApplied()
        {
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                return false;

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                return false;

#if UNITY_6000_0_OR_NEWER
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.GameActivity)
                return false;
#endif

            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (androidOpenXRSettings == null || androidOpenXRSettings.latencyOptimization != OpenXRSettings.LatencyOptimization.PrioritizeInputPolling)
                return false;

            if (!IsOptimizeBufferDiscardsDisabled(androidOpenXRSettings))
                return false;

            foreach (var target in DefineTargets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(target);
                foreach (var define in ValidationDefines)
                {
                    if (!HasDefine(defines, define))
                        return false;
                }
            }

            return true;
        }

        private static IEnumerable<Type> RequiredFeaturesFor(BuildTargetGroup target)
        {
            foreach (var type in CommonFeatureTypes)
                yield return type;

            if (target == BuildTargetGroup.Android)
                yield return typeof(MetaQuestFeature); // "Meta Quest Support"
        }

        private static bool IsConfiguredForTarget(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup target)
        {
            var settings = perTarget.SettingsForBuildTarget(target);
            if (settings == null || !settings.InitManagerOnStart || settings.Manager == null)
                return false;

            var hasOpenXRLoader = false;
            foreach (var loader in settings.Manager.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == OpenXRLoaderTypeName)
                {
                    hasOpenXRLoader = true;
                    break;
                }
            }

            if (!hasOpenXRLoader)
                return false;

            var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (openXRSettings == null)
                return false;

            foreach (var featureType in RequiredFeaturesFor(target))
            {
                var feature = openXRSettings.GetFeature(featureType);
                if (feature == null || !feature.enabled)
                    return false;
            }

            return true;
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

            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, OpenXRLoaderTypeName, target))
            {
                Debug.LogError($"[XRSetup] Failed to assign OpenXR loader for {target}. Is com.unity.xr.openxr installed?");
                return;
            }

            // OpenXR must be the active provider — drop the legacy Oculus loader if it was assigned.
            XRPackageMetadataStore.RemoveLoader(settings.Manager, OculusLoaderTypeName, target);

            EnableOpenXRFeatures(target);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
            EditorUtility.SetDirty(perTarget);

            Debug.Log($"[XRSetup] {target}: OpenXR loader assigned, Initialize XR on Startup = true.");
        }

        private static void EnableOpenXRFeatures(BuildTargetGroup target)
        {
            // Ensures the per-target OpenXR settings asset exists and its feature list is populated.
            FeatureHelpers.RefreshFeatures(target);

            var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (openXRSettings == null)
            {
                Debug.LogError($"[XRSetup] No OpenXR settings found for {target} — interaction profiles/features not enabled.");
                return;
            }

            foreach (var featureType in RequiredFeaturesFor(target))
            {
                var feature = openXRSettings.GetFeature(featureType);
                if (feature == null)
                {
                    Debug.LogWarning($"[XRSetup] {target}: OpenXR feature '{featureType.Name}' not found — is its package installed?");
                    continue;
                }

                if (!feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                }
            }

            EditorUtility.SetDirty(openXRSettings);
            Debug.Log($"[XRSetup] {target}: OpenXR interaction profiles and hand tracking features enabled.");
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
