using App.Core;
using App.Shared;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.core
{
    public class LimappPlayer : MonoBehaviour
    {
        public VREmulator DeviceInitializer;

        public VRAvatar Avatar;
        public GameObject SceneContainer;

        [Header("URP Resources")]
        public RenderPipelineAsset URPAsset;

        public Material UnlitMaterial;
        public Material GridMaterial;

        private LimappBase _currentLimapp; // <--- Track current experience

        public ExperiencesResponse ExperiencesResponse;

        public static LimappPlayer Instance;

        private void Start()
        {
            Instance = this;
        }


        public void SetupDevice()
        {
            var device = DeviceInitializer.CreateDevice();
            VRDevice.Initialize(device);
            VRDevice.Device.SetupAvatar(Avatar);
        }

        private void Update()
        {
            UnityXRDevice.UpdateControllers = true;
        }

        [ContextMenu("End")]
        public void End()
        {
            StartCoroutine(EndRoutine());

            IEnumerator EndRoutine()
            {
                yield return _currentLimapp.Unload();
                SceneContainer.SetActive(true);

                // 1 - Try set up avatar only
                // Seems like we need to wait one second before setting up avatar again?
                yield return new WaitForSeconds(1);
                VRDevice.Device.SetupAvatar(Avatar);

                // 2 - Try creating a new device but searching for origin.

                QualitySettings.renderPipeline = null;
            }
        }

        public void Play(int id)
        {
            StartCoroutine(PlayRoutine());

            IEnumerator PlayRoutine()
            {
                var useURP = new HashSet<int>() { 40 };
                QualitySettings.renderPipeline = useURP.Contains(id) ? URPAsset : null;

                var limapp = new LimappBase(id);
                _currentLimapp = limapp; // <-- Store reference

                yield return limapp.LoadScene();

                SceneContainer.SetActive(false);

                yield return new WaitUntil(() => limapp.ExperienceApp != null);

                ApplySpecialCases(limapp);
            }
        }

        private void ApplySpecialCases(LimappBase limapp)
        {
            var limappSpecialCases = new LimappSpecialCases();

            var avatar = limapp.ExperienceApp.GetComponentInChildren<VRAvatar>(true);
            var exp = ExperiencesResponse.Experiences.FirstOrDefault(x => x.Id == limapp.Id);

            if (exp != null)
            {
                var config = JsonConvert.DeserializeObject<ExperienceConfig>(exp.ConfigJson);

                limappSpecialCases.ApplySpecificAppSettings(limapp.Id, avatar, config);
                limappSpecialCases.ResetHandChildrenPositions(avatar, config);
            }
        }


        /// <summary>
        /// Temp used in the simulator
        /// </summary>
        /// <param name="experiencesResponse"></param>
        public void SetExperiencesData(ExperiencesResponse experiencesResponse)
        {
            ExperiencesResponse = experiencesResponse;
        }
    }
}

public class ExperiencesResponse
{
    public List<Experience> Experiences { get; set; }
}