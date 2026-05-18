using System;
using System.Collections.Generic;
using App.Core;
using App.core;
using Liminal.SDK.Core;
using Liminal.SDK.V2;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using System.Collections;
using Liminal.SDK.VR.Utils;

namespace App.PlatformViewer
{
    public class ExperienceSettingsMenu : MonoBehaviour
    {
        public Action OnExitExperience;

        public Button AcceptButton;
        public Button DeclineButton;
        
        public ExperienceAppManager PlatformExperienceAppManager => ExperienceAppManager.PlatformInstance;
        public ExperienceAppManager LimappExperienceAppManager => ExperienceAppManager.LimappInstance;

        public GameObject Content;

        [Tooltip("Layer mask the menu raycasters use while open. Defaults to SettingsMenuLayer (10).")]
        public LayerMask Mask = 1 << 10;

        private Vector3 PlatformHeadCachedPosition;
        private Quaternion PlatformHeadCachedRotation;

        private bool _isOpened;

        private XRRigReferences _maskedRig;
        private LayerMask _cachedPhysicsRaycasterMask;
        private LayerMask _cachedLeftRayMask;
        private LayerMask _cachedRightRayMask;

        public ExperienceApp ExperienceApp => LimappExperienceAppManager.ExperienceApp;

        public FollowCamera FollowEyeCamera;


        private void Awake()
        {
            Content.SetActive(false);

            AcceptButton.onClick.AddListener(() =>
            {
               Exit(); 
            });

            DeclineButton.onClick.AddListener(() =>
            {
                Hide();
            });
        }

        private void Update()
        {
            var device = DeviceManager.Device;
            if (device == null)
                return;

            if (device.GetButtonDown(VRButton.Back))
            {
                ToggleState();
            }
        }

        public void ToggleState()
        {
            FollowEyeCamera.AlignToCameraImmediately();
            
            if (_isOpened)
                Hide();
            else
                Show();
        }

        [ContextMenu("Show")]
        public void Show()
        {
            _isOpened = true;
            Content.SetActive(true);

            SetPlatformAvatarHeadTranformsToExperience();

            SetExperienceAppManagerActive(LimappExperienceAppManager, false);
            SetExperienceAppManagerActive(PlatformExperienceAppManager, true);
            ApplyMenuMaskTo(PlatformExperienceAppManager != null ? PlatformExperienceAppManager.SpawnedRig : null);
            ExperienceApp.Pause();
        }


        [ContextMenu("Hide")]
        public void Hide()
        {
            _isOpened = false;
            Content.SetActive(false);

            ExperienceApp.Resume();
            RestoreMenuMaskFor(PlatformExperienceAppManager != null ? PlatformExperienceAppManager.SpawnedRig : null);
            // Deactivate platform first so the shared InputActionAsset's disable/re-enable churn
            // happens before the limapp's TrackedPoseDriver re-binds. Activating limapp first means
            // its TrackedPoseDriver binds to an enabled action, then immediately sees the action
            // disabled by platform's OnDisable cascade — it enters a 'tracking lost' state and
            // holds the last pose even after we re-enable.
            SetExperienceAppManagerActive(PlatformExperienceAppManager, false);
            SetExperienceAppManagerActive(LimappExperienceAppManager, true);
        }

        public Coroutine Exit()
        {
            if(_isOpened)
                Hide();

            return StartCoroutine(Routine());

            IEnumerator Routine()
            {
                yield return LimappPlayer.Instance.End();

                SetPlatformAvatarHeadTransformToCached();
                SetExperienceAppManagerActive(PlatformExperienceAppManager, true);
            }
        }

        private void ApplyMenuMaskTo(XRRigReferences rig)
        {
            if (rig == null)
                return;

            // If we somehow already masked a (different) rig and never restored it, restore first so we don't leak menu mask into it.
            if (_maskedRig != null && _maskedRig != rig)
                RestoreMenuMaskFor(_maskedRig);

            _maskedRig = rig;

            if (rig.TrackedDevicePhysicsRaycaster != null)
            {
                _cachedPhysicsRaycasterMask = rig.TrackedDevicePhysicsRaycaster.eventMask;
                rig.TrackedDevicePhysicsRaycaster.eventMask = Mask;
            }

            if (rig.LeftControllerRayInteractor != null)
            {
                _cachedLeftRayMask = rig.LeftControllerRayInteractor.raycastMask;
                rig.LeftControllerRayInteractor.raycastMask = Mask;
            }

            if (rig.RightControllerRayInteractor != null)
            {
                _cachedRightRayMask = rig.RightControllerRayInteractor.raycastMask;
                rig.RightControllerRayInteractor.raycastMask = Mask;
            }
        }

        private void RestoreMenuMaskFor(XRRigReferences rig)
        {
            if (rig == null || _maskedRig != rig)
                return;

            if (rig.TrackedDevicePhysicsRaycaster != null)
                rig.TrackedDevicePhysicsRaycaster.eventMask = _cachedPhysicsRaycasterMask;

            if (rig.LeftControllerRayInteractor != null)
                rig.LeftControllerRayInteractor.raycastMask = _cachedLeftRayMask;

            if (rig.RightControllerRayInteractor != null)
                rig.RightControllerRayInteractor.raycastMask = _cachedRightRayMask;

            _maskedRig = null;
        }

        private void SetExperienceAppManagerActive(ExperienceAppManager appManager, bool state)
        {
            appManager.VRAvatar.gameObject.SetActive(state);
            appManager.gameObject.SetActive(state);

            // Deactivating the manager runs InputActionManager.OnDisable on its rig, which
            // Disable()s the shared XRI InputActionAsset — killing TrackedPoseDriver pose input
            // (head locks on device) and other action reads. Force the asset back on after every
            // toggle. The editor's XR Device Simulator drives transforms directly so this never
            // showed up in-editor.
            EnsureXRInputActionsEnabled();

            // XRInteractorLineVisual doesn't fully re-initialise from a GameObject SetActive cycle —
            // raycasts still work but the line + reticle stay in whatever state they were in at
            // disable time. Toggling component.enabled forces OnDisable/OnEnable synchronously and
            // resets the visual.
            if (state && appManager.SpawnedRig != null)
                RefreshLineVisuals(appManager.SpawnedRig);
        }

        private static void RefreshLineVisuals(XRRigReferences rig)
        {
            var visuals = rig.GetComponentsInChildren<XRInteractorLineVisual>(true);
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i].enabled = false;
                visuals[i].enabled = true;
            }
        }

        private static void EnsureXRInputActionsEnabled()
        {
            var refs = Liminal.SDK.XR.XRInputReferences.Instance;
            if (refs == null) return;

            var ctrlRefs = refs.RightControllerReferences;
            if (ctrlRefs == null) return;

            var backRef = ctrlRefs.Back;
            if (backRef == null) return;

            var asset = backRef.action?.actionMap?.asset;
            if (asset != null)
                asset.Enable();
        }

        public void CachePlatformAvatarHeadTransform()
        {
            PlatformHeadCachedPosition = PlatformExperienceAppManager.VRAvatar.Head.Transform.position;
            PlatformHeadCachedRotation = PlatformExperienceAppManager.VRAvatar.Head.Transform.rotation;
        }

        private void SetPlatformAvatarHeadTranformsToExperience()
        {
            PlatformExperienceAppManager.VRAvatar.Head.Transform.position = LimappExperienceAppManager.VRAvatar.Head.Transform.position;
            PlatformExperienceAppManager.VRAvatar.Head.Transform.rotation = LimappExperienceAppManager.VRAvatar.Head.Transform.rotation;
        }

        private void SetPlatformAvatarHeadTransformToCached()
        {
            PlatformExperienceAppManager.VRAvatar.Head.Transform.position = PlatformHeadCachedPosition;
            PlatformExperienceAppManager.VRAvatar.Head.Transform.rotation = PlatformHeadCachedRotation;
        }


    }
}
