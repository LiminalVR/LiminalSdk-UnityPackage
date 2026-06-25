using System;
using System.Collections.Generic;
using App.Core;
using App.core;
using Liminal.Core.Fader;
using Liminal.SDK.Core;
using Liminal.SDK.V2;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Liminal.SDK.VR.Utils;
#if ENABLE_VR || UNITY_GAMECORE
using UnityEngine.XR.Hands;
#endif

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

        // Tracks the hand menu gesture state across frames so we only toggle on the rising edge,
        // mirroring GetButtonDown for the controller menu (Back) button.
        private bool _handMenuWasPressed;

        private void Update()
        {
            var device = DeviceManager.Device;
            if (device != null && device.GetButtonDown(VRButton.Back))
            {
                ToggleState();
            }

            // With hand tracking there is no Back button, so the Meta menu gesture (palm-up pinch
            // on the non-dominant hand) stands in for it and opens/closes this exit menu.
            if (HandMenuPressedThisFrame())
            {
                ToggleState();
            }
        }

        private bool HandMenuPressedThisFrame()
        {
#if ENABLE_VR || UNITY_GAMECORE
            var pressed = IsMenuGesturePressed(MetaAimHand.left) || IsMenuGesturePressed(MetaAimHand.right);
            var down = pressed && !_handMenuWasPressed;
            _handMenuWasPressed = pressed;
            return down;
#else
            return false;
#endif
        }

#if ENABLE_VR || UNITY_GAMECORE
        // The MenuPressed flag is only ever set on the non-dominant hand (the one showing the menu
        // icon), so checking both hands is safe. Returns false when not hand tracking, since the
        // static MetaAimHand devices are null/not added in that case.
        private static bool IsMenuGesturePressed(MetaAimHand hand)
        {
            if (hand == null || !hand.added)
                return false;

            var flags = (MetaAimFlags)hand.aimFlags.ReadValue();
            return (flags & MetaAimFlags.MenuPressed) != 0;
        }
#endif

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

            LimappExperienceAppManager.SetActive(false);
            PlatformExperienceAppManager.SetActive(true);
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
            PlatformExperienceAppManager.SetActive(false);
            LimappExperienceAppManager.SetActive(true);
        }

        // Exit can be triggered both by the Quit button and by the experience ending itself
        // (ExperienceApp.End -> OnComplete). Guard against a second run while the first is still
        // unloading, which would call LimappPlayer.End() again and NRE on the nulled limapp.
        private bool _isExiting;

        public Coroutine Exit()
        {
            if (_isExiting)
                return null;

            _isExiting = true;

            if(_isOpened)
                Hide();

            return CoroutineService.Instance.StartCoroutine(Routine());

            IEnumerator Routine()
            {
                yield return LimappPlayer.Instance.End();

                SetPlatformAvatarHeadTransformToCached();
                PlatformExperienceAppManager.SetActive(true);

                _isExiting = false;
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
