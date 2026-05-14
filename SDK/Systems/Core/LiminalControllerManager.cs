using System;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using UnityEngine;

namespace Liminal.SDK.V2
{
    [Flags]
    public enum EControllerDisplay
    {
        None = 0,
        Mesh = 1 << 0,         // Default controller mesh (e.g. Quest 3 controller).
        CustomMesh = 1 << 1,   // Custom hand mesh (e.g. wand, weapon, custom hand model).
        Pointer = 1 << 2,
        All = Mesh | CustomMesh | Pointer,
    }

    public class LiminalControllerManager : MonoBehaviour
    {
        public ExperienceAppManager ExperienceAppManager;
        public XRRigReferences XRRigReferences => ExperienceAppManager != null ? ExperienceAppManager.SpawnedRig : null;

        // Default controller meshes (e.g. Quest 3 controller models).
        [Header("Default Controller Meshes")]
        public GameObject DefaultLeftHandMesh => XRRigReferences.RigLeftHandVisual.gameObject;
        public GameObject DefaultRightHandMesh => XRRigReferences.RigRightHandVisual.gameObject;

        // Custom meshes already attached to the avatar — wand, weapon, custom hand, etc.
        [Header("Custom Avatar Meshes")]
        public GameObject AvatarCustomLeftHandMesh;
        public GameObject AvatarCustomRightHandMesh;

        [Header("Defaults applied on Awake")]
        public EControllerDisplay LeftDefault = EControllerDisplay.All;
        public EControllerDisplay RightDefault = EControllerDisplay.All;

        public void ApplyDefaults()
        {
            UpdateHandConfiguration(VRInputDeviceHand.Left, LeftDefault);
            UpdateHandConfiguration(VRInputDeviceHand.Right, RightDefault);
        }

        public void UpdateHandConfiguration(VRInputDeviceHand hand, EControllerDisplay display)
        {
            bool wantDefault = (display & EControllerDisplay.Mesh) != 0;
            bool wantCustom = (display & EControllerDisplay.CustomMesh) != 0;
            bool wantPointer = (display & EControllerDisplay.Pointer) != 0;

            SetDefaultMeshActive(hand, wantDefault);
            SetCustomMeshActive(hand, wantCustom);
            SetPointerActive(hand, wantPointer);
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
