using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using App.core;
using App.Core;
using Liminal.SDK.VR;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Liminal.SDK.VR.Input;
using Newtonsoft.Json;
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


        private void Start()
        {
            Instance = this;
            LimappPlayer.SetupDevice();
            BetterStreamingAssets.Initialize();
            StartCoroutine(GetExperiences());
        }

        private void Update()
        {
            var device = VRDevice.Device;
            if (device != null)
            {
                if (device.PrimaryInputDevice.GetButtonDown(VRButton.Back))
                {
                    LimappPlayer.End();
                }
            }
        }

        private void SetupExperiences()
        {
            foreach (Transform child in Layout)
            {
                Destroy(child.gameObject);
            }

            foreach (var experienceId in ExperienceIds)
            {
                var instance = Instantiate(ExperienceIconButtonPrefab, Layout);
                instance.Bind(experienceId);
                instance.Button.onClick.AddListener(() => LimappPlayer.Play(new LimappBase(experienceId)));
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

                    ExperienceIds.Clear();
                    foreach (var exp in ExperiencesResponse.Experiences)
                    {
                        ExperienceIds.Add(exp.Id);

                        // Download it.
                    }

                    SetupExperiences();
                }
            }
        }
    }

}

