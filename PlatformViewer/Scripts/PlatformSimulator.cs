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
using Liminal.SDK.V2;
using Liminal.SDK.VR.Avatars;
using App.PlatformViewer;

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

        public ExperienceAppManager ExperienceAppManager;

        public ExperienceSettingsMenu ExperienceSettingsMenu;


        IEnumerator Start()
        {
            Instance = this;
            LimappPlayer.SetupDevice();
            BetterStreamingAssets.Initialize();
            LogStreamingAssetCheck(26);
            SetupExperiences();
            yield return null;
        }

        public VRAvatar PlatformAvatar;

        public void CleanPreviousAvatar()
        {
            ExperienceSettingsMenu.CachePlatformAvatarHeadTransform();

            PlatformAvatar.gameObject.SetActive(false);
            ExperienceAppManager.gameObject.SetActive(false);
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
                    StartCoroutine(LoadLimapp(experienceId, instance));
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
        private IEnumerator LoadLimapp(int experienceId, ExperienceSimulatorIconButton button)
        {
            yield return null; // let Destroy() settle before the limapp scene activates

            // Download / update the limapp from S3 into persistentDataPath before playing. Re-checks the
            // S3 version each time, so a freshly uploaded build is pulled down automatically.
            var defaultLabel = button != null ? button.Label.text : experienceId.ToString();
            yield return LimappBase.EnsureLatest(experienceId,
                onProgress: p => { if (button != null) button.Label.text = $"{experienceId}\n{p * 100f:0}%"; },
                onStatus: s => { if (button != null) button.Label.text = $"{experienceId}\n{s}"; });
            if (button != null)
                button.Label.text = defaultLabel;

            if (!LimappBase.IsContentAvailable(experienceId))
            {
                Debug.LogError($"[Simulator] Experience {experienceId} could not be downloaded — aborting play.");
                yield break;
            }

            yield return LimappPlayer.Play(new LimappBase(experienceId));
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

