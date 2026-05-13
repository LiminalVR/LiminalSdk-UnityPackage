using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using App.core;
using App.Core;
using Liminal.SDK.VR;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Liminal.SDK.VR.Input;
using Newtonsoft.Json;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Experience = App.Shared.Experience;

namespace App.Simulator
{
    /// <summary>
    /// Simulates the Platform loading style. 
    /// </summary>
    public class PlatformSimulator : MonoBehaviour
    {
        public List<int> ExperienceIds;

        public Transform Layout;
        public ExperienceSimulatorIconButton ExperienceIconButtonPrefab;

        public static PlatformSimulator Instance;

        public ExperiencesResponse ExperiencesResponse;

        public LimappPlayer LimappPlayer;

        public GameObject[] TestDeletes;


        IEnumerator Start()
        {
            Instance = this;
            LimappPlayer.SetupDevice();
            BetterStreamingAssets.Initialize();
            LogStreamingAssetCheck(26);
            SetupExperiences();
            yield return null;
        }

        public void CleanPreviousAvatar()
        {
            for (int i = 0; i < TestDeletes.Length; i++)
                Destroy(TestDeletes[i]);
        }

        private void LogStreamingAssetCheck(int experienceId)
        {
            string manifestPath = $"Limapps/Android/{experienceId}/manifest.json";
            string bundlePath = $"Limapps/Android/{experienceId}/appBundle.bundle";
            string legacyBundlePath = $"Limapps/Android/{experienceId}/appBundle";
            string dirPath = $"Limapps/Android/{experienceId}";

            bool manifestExists = BetterStreamingAssets.FileExists(manifestPath);
            bool bundleExists = BetterStreamingAssets.FileExists(bundlePath);
            bool legacyBundleExists = BetterStreamingAssets.FileExists(legacyBundlePath);
            bool dirExists = BetterStreamingAssets.DirectoryExists(dirPath);

            Debug.Log($"[BSA Check] Root={BetterStreamingAssets.Root}");
            Debug.Log($"[BSA Check] Dir '{dirPath}' exists in APK: {dirExists}");
            Debug.Log($"[BSA Check] '{manifestPath}' exists in APK: {manifestExists}");
            Debug.Log($"[BSA Check] '{bundlePath}' exists in APK: {bundleExists}");
            Debug.Log($"[BSA Check] '{legacyBundlePath}' (legacy) exists in APK: {legacyBundleExists}");

            if (dirExists)
            {
                var files = BetterStreamingAssets.GetFiles(dirPath, "*", System.IO.SearchOption.AllDirectories);
                Debug.Log($"[BSA Check] Files under '{dirPath}': {string.Join(", ", files)}");
            }
        }

        private void SetupExperiences()
        {
            foreach (var experienceId in ExperienceIds)
            {
                var instance = Instantiate(ExperienceIconButtonPrefab, Layout);
                instance.Bind(experienceId);
                instance.Button.onClick.AddListener(() => {
                    CleanPreviousAvatar();
                    StartCoroutine(LoadLimapp(experienceId)); 
                });
            }

            StartCoroutine(GetExperiences());
        }

        /// <summary>
        /// Resets XRI's process-wide singleton state and starts the limapp. The reset is required
        /// because XR Interaction Toolkit caches the first-encountered <see cref="XRInteractionManager"/>
        /// in a static field (<c>ComponentLocatorUtility&lt;XRInteractionManager&gt;.s_ComponentCache</c>) and
        /// holds pending interactor/interactable registrations in static waitlists. Without resetting,
        /// the limapp's manager either gets auto-destroyed (default <c>EnforceSingle</c> mode) or its
        /// interactors register against the simulator's manager, leaving them orphaned from their own
        /// interactables.
        /// </summary>
        private IEnumerator LoadLimapp(int experienceId)
        {
            ResetXRInteractionState();
            yield return null; // let Destroy() settle before the limapp scene activates
            yield return LimappPlayer.Play(new LimappBase(experienceId));
        }

        private static void ResetXRInteractionState()
        {
            // Include inactive — stale UI modules / managers can hide on disabled GameObjects in the
            // simulator's avatar hierarchy and still influence input wiring.
            var managers = FindObjectsByType<XRInteractionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in managers)
            {
                Debug.Log($"[PlatformSimulator] Destroying simulator-side XRInteractionManager '{m.name}' before limapp load.");
                Destroy(m);
            }

            // XRUIInputModule on the simulator avatar's EventSystem grabs the limapp's rays for
            // UI hit-testing and bends the visual to whatever UI element it intersects. Strip it
            // so the limapp's own UI module (if any) becomes the only one driving rays.
            var uiModules = FindObjectsByType<XRUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var module in uiModules)
            {
                Debug.Log($"[PlatformSimulator] Destroying simulator-side XRUIInputModule on '{module.gameObject.name}' before limapp load.");
                Destroy(module);
            }

            // Defensive: clear XRI's static waitlists in case any of the simulator's interactors
            // re-added themselves after their manager went away. Reflection because the fields are private.
            var t = typeof(XRInteractionManager);
            foreach (var fieldName in new[] { "s_WaitlistGroups", "s_WaitlistInteractors", "s_WaitlistInteractables", "s_WaitlistSnapVolumes" })
            {
                var field = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
                field?.SetValue(null, null);
            }
        }

        private IEnumerator GetExperiences()
        {
            string url = $"https://api.liminalvr.com/api/experiences/all";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error: " + webRequest.error);
                }
                else
                {
                    ExperiencesResponse = JsonConvert.DeserializeObject<ExperiencesResponse>(webRequest.downloadHandler.text);
                    LimappPlayer.SetExperiencesData(ExperiencesResponse);
                }
            }
        }
    }

}

