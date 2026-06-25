using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Liminal.Core.Fader;

namespace App.Simulator
{
    /// <summary>
    /// Simulates the Platform loading style. 
    /// </summary>
    public class PlatformSimulator : MonoBehaviour
    {
        [Tooltip("Auto-populated at runtime from the DLLs in DllsFolder. Any ids set here are used as a fallback when no DLLs are found (e.g. in a device build).")]
        public List<int> ExperienceIds;

        [Tooltip("Folder (relative to the Assets folder) scanned for App<id>.dll files. Each DLL becomes an experience button.")]
        public string DllsFolder = "TestApp/DLLs";

        public Transform Layout;
        public ExperienceSimulatorIconButton ExperienceIconButtonPrefab;

        public static PlatformSimulator Instance;

        public ExperiencesResponse ExperiencesResponse;

        public LimappPlayer LimappPlayer;

        public ExperienceAppManager ExperienceAppManager;

        public ExperienceSettingsMenu ExperienceSettingsMenu;

        private void OnEnable()
        {
            // When the running experience ends itself via ExperienceApp.End(), return to the
            // platform through the same flow as the settings menu's Quit button. Subscribed here
            // rather than on ExperienceSettingsMenu because that menu's GameObject can be inactive,
            // which would stop it from ever (un)subscribing; the simulator is always active.
            Liminal.SDK.Core.ExperienceApp.OnComplete += OnExperienceComplete;
        }

        private void OnDisable()
        {
            Liminal.SDK.Core.ExperienceApp.OnComplete -= OnExperienceComplete;
        }

        private void OnExperienceComplete(bool completed)
        {
            ExperienceSettingsMenu.Exit();
        }

        IEnumerator Start()
        {
            Instance = this;

            LimappPlayer.SetupDevice();
            BetterStreamingAssets.Initialize();
            LogStreamingAssetCheck(26);

            // Drop any App<id>.dll into DllsFolder and it shows up as a button.
            var discovered = LoadExperienceIdsFromDlls();
            if (discovered.Count > 0)
                ExperienceIds = discovered;

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

        /// <summary>
        /// Editor helper: scan the DLL folder and write the result into <see cref="ExperienceIds"/>
        /// so it can be inspected/tweaked without entering play mode. Right-click the component header.
        /// </summary>
        [ContextMenu("Populate Experience Ids From DLLs")]
        private void PopulateExperienceIdsFromDlls()
        {
            ExperienceIds = LoadExperienceIdsFromDlls();
            Debug.Log($"[Simulator] Found {ExperienceIds.Count} experience(s): {string.Join(", ", ExperienceIds)}");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Scans <see cref="DllsFolder"/> (relative to the Assets folder) for limapp DLLs named
        /// like "App000000000047.dll" and returns their ids (e.g. 47), sorted and de-duplicated.
        /// Returns an empty list when the folder is missing — e.g. in a device build — so the
        /// serialized <see cref="ExperienceIds"/> is kept as a fallback.
        /// </summary>
        private List<int> LoadExperienceIdsFromDlls()
        {
            var ids = new List<int>();
            var path = Path.Combine(Application.dataPath, DllsFolder);

            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"[Simulator] DLL folder '{path}' not found; using serialized ExperienceIds.");
                return ids;
            }

            foreach (var file in Directory.GetFiles(path, "App*.dll"))
            {
                // GetFiles' "*.dll" can over-match (e.g. .dll.meta) on Windows, so confirm the real extension.
                if (!file.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // "App000000000047" -> "000000000047" -> 47
                var name = Path.GetFileNameWithoutExtension(file).Substring("App".Length);
                if (int.TryParse(name, out var id))
                    ids.Add(id);
                else
                    Debug.LogWarning($"[Simulator] Could not parse experience id from '{Path.GetFileName(file)}'.");
            }

            return ids.Distinct().OrderBy(id => id).ToList();
        }

        private void SetupExperiences()
        {
            foreach (var experienceId in ExperienceIds)
            {
                var instance = Instantiate(ExperienceIconButtonPrefab, Layout);
                instance.Bind(experienceId);
                instance.Button.onClick.AddListener(() => {
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
            CleanPreviousAvatar();

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

