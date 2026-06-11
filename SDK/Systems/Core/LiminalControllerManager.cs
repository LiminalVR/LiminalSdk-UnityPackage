using System;
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.XR.Hands.Samples.VisualizerSample;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Liminal.SDK.V2
{
    [Flags]
    public enum EControllerDisplay
    {
        None = 0,
        Mesh = 1 << 0,         // Default controller mesh (e.g. Quest 3 controller).
        CustomMesh = 1 << 1,   // Custom hand mesh (e.g. wand, weapon, custom hand model).
        Pointer = 1 << 2,      // Controller ray interactor.
        HandMesh = 1 << 3,     // HandVisualizer drawn mesh (shared — both hands or neither).
        HandPointer = 1 << 4,  // Hand-tracking ray interactor.
        All = Mesh | CustomMesh | Pointer | HandMesh | HandPointer,
    }

    public class LiminalControllerManager : MonoBehaviour
    {
        public ExperienceAppManager ExperienceAppManager;
        public XRRigReferences XRRigReferences => ExperienceAppManager != null ? ExperienceAppManager.SpawnedRig : null;

        // Default controller meshes (e.g. Quest 3 controller models).
        [Header("Default Controller Meshes")]
        public GameObject DefaultLeftHandMesh => XRRigReferences.LeftControllerVisual.gameObject;
        public GameObject DefaultRightHandMesh => XRRigReferences.RightControllerVisual.gameObject;

        // Custom meshes already attached to the avatar — wand, weapon, custom hand, etc.
        [Header("Custom Avatar Meshes")]
        public GameObject AvatarCustomLeftHandMesh;
        public GameObject AvatarCustomRightHandMesh;

        [Header("Custom Mesh Hand-Tracking Local Rotation")]
        [Tooltip("Local rotation applied to the custom left hand mesh while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomLeftHandTrackingRotation = new Vector3(-20f, 0f, -90f);
        [Tooltip("Local rotation applied to the custom right hand mesh while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomRightHandTrackingRotation = new Vector3(-20f, 0f, 90f);

        [Header("Defaults applied on Awake")]
        public EControllerDisplay LeftDefault = EControllerDisplay.All;
        public EControllerDisplay RightDefault = EControllerDisplay.All;

        private Quaternion _customLeftBaseLocalRotation;
        private Quaternion _customRightBaseLocalRotation;
        private bool _customBaseRotationsCached;

        public static bool IsHandTracking => XRInputModalityManager.currentInputMode.Value == XRInputModalityManager.InputMode.TrackedHand;

        public List<GameObject> HideWhenUsingHands;

        public void ApplyDefaults()
        {
            UpdateHandConfiguration(VRInputDeviceHand.Left, LeftDefault);
            UpdateHandConfiguration(VRInputDeviceHand.Right, RightDefault);
        }

        private void LateUpdate()
        {
            ApplyCustomMeshHandTrackingRotation();

            foreach (var go in HideWhenUsingHands)
            {
                if (go != null)
                    go.SetActive(!IsHandTracking);
            }
        }

        private void ApplyCustomMeshHandTrackingRotation()
        {
            if (!_customBaseRotationsCached)
            {
                if (AvatarCustomLeftHandMesh != null)
                    _customLeftBaseLocalRotation = AvatarCustomLeftHandMesh.transform.localRotation;
                if (AvatarCustomRightHandMesh != null)
                    _customRightBaseLocalRotation = AvatarCustomRightHandMesh.transform.localRotation;
                _customBaseRotationsCached = true;
            }

            if (AvatarCustomLeftHandMesh != null)
            {
                AvatarCustomLeftHandMesh.transform.localRotation = IsHandTracking
                    ? Quaternion.Euler(CustomLeftHandTrackingRotation)
                    : _customLeftBaseLocalRotation;
            }

            if (AvatarCustomRightHandMesh != null)
            {
                AvatarCustomRightHandMesh.transform.localRotation = IsHandTracking
                    ? Quaternion.Euler(CustomRightHandTrackingRotation)
                    : _customRightBaseLocalRotation;
            }
        }

        public void UpdateHandConfiguration(VRInputDeviceHand hand, EControllerDisplay display)
        {
            bool wantDefault = (display & EControllerDisplay.Mesh) != 0;
            bool wantCustom = (display & EControllerDisplay.CustomMesh) != 0;
            bool wantPointer = (display & EControllerDisplay.Pointer) != 0;
            bool wantHandMesh = (display & EControllerDisplay.HandMesh) != 0;
            bool wantHandPointer = (display & EControllerDisplay.HandPointer) != 0;

            SetDefaultMeshActive(hand, wantDefault);
            SetCustomMeshActive(hand, wantCustom);
            SetPointerActive(hand, wantPointer);
            SetHandPointerActive(hand, wantHandPointer);
            // HandVisualizer.drawMeshes is shared between hands. Last per-hand call wins;
            // call ApplyDefaults() if you want both flags ORed.
            SetHandMeshActive(wantHandMesh);
        }

        public void SetDefaultMeshActive(VRInputDeviceHand hand, bool active)
        {
            var mesh = GetDefaultMesh(hand);
            if (mesh != null)
                mesh.SetActive(active);
        }

        public void SetCustomMeshActive(VRInputDeviceHand hand, bool active)
        {
            var mesh = GetCustomMesh(hand);
            if (mesh != null)
                mesh.SetActive(active);
        }

        public void SetPointersActive(bool active)
        {
            SetPointerActive(VRInputDeviceHand.Left, active);
            SetPointerActive(VRInputDeviceHand.Right, active);
        }

        public void SetHandMeshActive(bool active)
        {
            var visualizer = XRRigReferences != null ? XRRigReferences.HandVisualizer : null;
            if (visualizer != null)
                visualizer.drawMeshes = active;
        }

        public void SetHandPointerActive(VRInputDeviceHand hand, bool active)
        {
            var interactor = GetHandRayInteractor(hand);
            if (interactor != null)
                interactor.gameObject.SetActive(active);
        }

        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor GetHandRayInteractor(VRInputDeviceHand hand)
        {
            if (XRRigReferences == null) return null;
            switch (hand)
            {
                case VRInputDeviceHand.Left: return XRRigReferences.LeftHandRayInteractor;
                case VRInputDeviceHand.Right: return XRRigReferences.RightHandRayInteractor;
                default: return null;
            }
        }

        public void SetPointerActive(VRInputDeviceHand hand, bool active)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    ExperienceAppManager.LeftControllerRayInteractor.gameObject.SetActive(active);
                    break;
                case VRInputDeviceHand.Right:
                    ExperienceAppManager.RightControllerRayInteractor.gameObject.SetActive(active);
                    break;
                case VRInputDeviceHand.None:
                default:
                    Debug.LogWarning($"Trying to set pointer active for hand type NONE or unknown. Hand: {hand}");
                    break;
            }
        }

        private GameObject GetDefaultMesh(VRInputDeviceHand hand)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left: return DefaultLeftHandMesh;
                case VRInputDeviceHand.Right: return DefaultRightHandMesh;
                default: return null;
            }
        }

        private GameObject GetCustomMesh(VRInputDeviceHand hand)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left: return AvatarCustomLeftHandMesh;
                case VRInputDeviceHand.Right: return AvatarCustomRightHandMesh;
                default: return null;
            }
        }
    }
}
