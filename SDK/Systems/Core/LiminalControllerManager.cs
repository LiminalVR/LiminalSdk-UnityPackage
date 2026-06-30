using System;
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.XR.Hands;
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
        [Tooltip("When off (default), the custom hand meshes' rotation is left untouched. When on, the rotations below are applied while Hand Tracking is active.")]
        public bool ApplyCustomRotation = false;
        [Tooltip("Local rotation applied to the custom left hand mesh while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomLeftHandTrackingRotation = new Vector3(-20f, 0f, -90f);
        [Tooltip("Local rotation applied to the custom right hand mesh while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomRightHandTrackingRotation = new Vector3(-20f, 0f, 90f);

        [Header("Custom Mesh Hand-Tracking Local Position Offset")]
        [Tooltip("When off (default), the custom hand meshes' position is left untouched. When on, the offsets below are applied while Hand Tracking is active.")]
        public bool ApplyCustomPosition = false;
        [Tooltip("Local position offset added to the custom left hand mesh's rest position while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomLeftHandTrackingPositionOffset = Vector3.zero;
        [Tooltip("Local position offset added to the custom right hand mesh's rest position while Hand Tracking is active. Ignored when using controllers.")]
        public Vector3 CustomRightHandTrackingPositionOffset = Vector3.zero;

        [Header("Defaults applied on Awake")]
        public EControllerDisplay LeftDefault = EControllerDisplay.All;
        public EControllerDisplay RightDefault = EControllerDisplay.All;

        private Quaternion _customLeftBaseLocalRotation;
        private Quaternion _customRightBaseLocalRotation;
        private Vector3 _customLeftBaseLocalPosition;
        private Vector3 _customRightBaseLocalPosition;
        private bool _customBaseTransformsCached;

        // There is one hand subsystem per process no matter how many XR rigs are spawned,
        // so read hand-tracking state straight from it. XRInputModalityManager.currentInputMode
        // is a *static* shared by every rig's modality manager; with multiple rigs the last
        // manager to change mode wins, so it can report TrackedHand while the rig you're using
        // has no tracked hands (and vice versa). The running hand subsystem is the single
        // source of truth, and keeps IsHandTracking and IsHandTracked() consistent.
        private static readonly List<XRHandSubsystem> s_HandSubsystems = new List<XRHandSubsystem>();

        private static XRHandSubsystem RunningHandSubsystem()
        {
            SubsystemManager.GetSubsystems(s_HandSubsystems);
            for (int i = 0; i < s_HandSubsystems.Count; i++)
            {
                if (s_HandSubsystems[i].running)
                    return s_HandSubsystems[i];
            }
            return null;
        }

        public static bool IsHandTracking
        {
            get
            {
                var subsystem = RunningHandSubsystem();
                return subsystem != null && (subsystem.leftHand.isTracked || subsystem.rightHand.isTracked);
            }
        }

        /// <summary>
        /// Fires when hand-tracking starts or stops (true = hands now tracked). Static to match
        /// <see cref="IsHandTracking"/>, so consumers can subscribe without holding a manager
        /// reference. Driven by the LateUpdate poll below, so it catches hands coming online a few
        /// frames after Start (common in-editor) as well as the user switching between hands and
        /// controllers at runtime. Subscribers must unsubscribe (e.g. in OnDisable) to avoid leaking
        /// across limapp load/unload and domain reloads.
        /// </summary>
        public static event Action<bool> HandTrackingChanged;

        private static bool s_handTracking;

        /// <summary>
        /// The active controller manager — the platform's when running inside the platform,
        /// otherwise the limapp's own.
        /// </summary>
        public static LiminalControllerManager Current
        {
            get
            {
                var app = ExperienceAppManager.Instance;
                return app != null ? app.ControllerManager : null;
            }
        }

        /// <summary>
        /// True while the given hand has live hand-tracking data. Always false when
        /// no hand subsystem is running (e.g. using controllers). Reads the running hand
        /// subsystem directly rather than a single rig's visualizer, so it stays correct
        /// when multiple rigs are spawned.
        /// </summary>
        public bool IsHandTracked(VRInputDeviceHand hand)
        {
            var subsystem = RunningHandSubsystem();
            if (subsystem == null)
                return false;

            switch (hand)
            {
                case VRInputDeviceHand.Left: return subsystem.leftHand.isTracked;
                case VRInputDeviceHand.Right: return subsystem.rightHand.isTracked;
                default: return false;
            }
        }

        public List<GameObject> HideWhenUsingHands;

        public void ApplyDefaults()
        {
            UpdateHandConfiguration(VRInputDeviceHand.Left, LeftDefault);
            UpdateHandConfiguration(VRInputDeviceHand.Right, RightDefault);
        }

        private void LateUpdate()
        {
            // Poll once and reuse: IsHandTracking walks the subsystem list each call.
            bool handTracking = IsHandTracking;

            if (handTracking != s_handTracking)
            {
                s_handTracking = handTracking;
                HandTrackingChanged?.Invoke(handTracking);
            }

            ApplyCustomMeshHandTrackingTransform();

            foreach (var go in HideWhenUsingHands)
            {
                if (go != null)
                    go.SetActive(!handTracking);
            }
        }

        private void ApplyCustomMeshHandTrackingTransform()
        {
            // Off by default: leave the meshes untouched unless an override is enabled.
            if (!ApplyCustomRotation && !ApplyCustomPosition)
                return;

            if (!_customBaseTransformsCached)
            {
                if (AvatarCustomLeftHandMesh != null)
                {
                    _customLeftBaseLocalRotation = AvatarCustomLeftHandMesh.transform.localRotation;
                    _customLeftBaseLocalPosition = AvatarCustomLeftHandMesh.transform.localPosition;
                }
                if (AvatarCustomRightHandMesh != null)
                {
                    _customRightBaseLocalRotation = AvatarCustomRightHandMesh.transform.localRotation;
                    _customRightBaseLocalPosition = AvatarCustomRightHandMesh.transform.localPosition;
                }
                _customBaseTransformsCached = true;
            }

            // Cache once: IsHandTracking polls the subsystem list, so avoid calling it per mesh.
            bool handTracking = IsHandTracking;

            if (AvatarCustomLeftHandMesh != null)
            {
                if (ApplyCustomRotation)
                    AvatarCustomLeftHandMesh.transform.localRotation = handTracking
                        ? Quaternion.Euler(CustomLeftHandTrackingRotation)
                        : _customLeftBaseLocalRotation;
                if (ApplyCustomPosition)
                    AvatarCustomLeftHandMesh.transform.localPosition = handTracking
                        ? _customLeftBaseLocalPosition + CustomLeftHandTrackingPositionOffset
                        : _customLeftBaseLocalPosition;
            }

            if (AvatarCustomRightHandMesh != null)
            {
                if (ApplyCustomRotation)
                    AvatarCustomRightHandMesh.transform.localRotation = handTracking
                        ? Quaternion.Euler(CustomRightHandTrackingRotation)
                        : _customRightBaseLocalRotation;
                if (ApplyCustomPosition)
                    AvatarCustomRightHandMesh.transform.localPosition = handTracking
                        ? _customRightBaseLocalPosition + CustomRightHandTrackingPositionOffset
                        : _customRightBaseLocalPosition;
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

        public void SetHandMeshActive(VRInputDeviceHand hand, bool active)
        {
            var visualizer = XRRigReferences != null ? XRRigReferences.HandVisualizer : null;
            if (visualizer == null)
                return;

            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    visualizer.SetHandMeshVisible(Handedness.Left, active);
                    break;
                case VRInputDeviceHand.Right:
                    visualizer.SetHandMeshVisible(Handedness.Right, active);
                    break;
                case VRInputDeviceHand.None:
                default:
                    Debug.LogWarning($"Trying to set hand mesh active for hand type NONE or unknown. Hand: {hand}");
                    break;
            }
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
