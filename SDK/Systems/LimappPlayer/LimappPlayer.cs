using App.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using App.Shared;

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

        private bool _ended;

        private void Awake()
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
            //UnityXRDevice.UpdateControllers = true;
        }

        [ContextMenu("End")]
        public Coroutine End()
        {
            return StartCoroutine(EndRoutine());

            IEnumerator EndRoutine()
            {
                yield return _currentLimapp.Unload();
                _currentLimapp = null;

                if(SceneContainer != null)
                    SceneContainer.SetActive(true);

                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
            }
        }

        public Coroutine Play(int id, ExperienceConfig? config = null)
        {
            return Play(new LimappBase(id), config);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public Coroutine Play(LimappBase limapp, ExperienceConfig? config = null)
        {
            return StartCoroutine(PlayRoutine());

            IEnumerator PlayRoutine()
            {
                var useURP = new HashSet<int>() { 40 };
                var id = limapp.Id;

                //var asset = useURP.Contains(id) ? URPAsset : null;
                //GraphicsSettings.defaultRenderPipeline = URPAsset;
                //GraphicsSettings.defaultRenderPipeline = asset;
                //QualitySettings.renderPipeline = asset;

                _currentLimapp = limapp; // <-- Store reference

                yield return limapp.LoadScene();

                if(SceneContainer != null)
                    SceneContainer.SetActive(false);

                yield return new WaitUntil(() => limapp.ExperienceApp != null);
                ApplySpecialCases(limapp, config);

                limapp.SetState(ELimappState.Running);
            }
        }

        private void ApplySpecialCases(LimappBase limapp, ExperienceConfig? config)
        {
            if (ExperiencesResponse == null)
                return;

            var limappSpecialCases = new LimappSpecialCases();
            var avatar = limapp.ExperienceApp.GetComponentInChildren<VRAvatar>(true);

            var exp = ExperiencesResponse.Experiences.FirstOrDefault(x => x.Id == limapp.Id);

            if (exp != null) 
            {
                limappSpecialCases.ApplySpecificAppSettings(limapp.Id, avatar, config.GetValueOrDefault());
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

        public LimappBase GetCurrent()
        {
            if (_currentLimapp == null)
                return null;

            return _currentLimapp;
        }

        public ELimappState GetState()
        {
            if (_currentLimapp == null)
                return ELimappState.Idle;

            return _currentLimapp.State;
        }

        public void SetState(ELimappState state)
        {
            if (_currentLimapp == null)
                return;

            _currentLimapp.State = state;
        }

        public LimappBase CurrentLimapp => _currentLimapp;
    }
}

public class ExperiencesResponse
{
    public List<Experience> Experiences { get; set; }
}
