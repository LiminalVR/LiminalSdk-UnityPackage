using System;
using Liminal.SDK.VR.Input;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// Mapping from Liminal SDK -> New Unity XR Input System. This is used by the XRInputDevice to get the correct input action for each button/axis.
    /// </summary>
    public class XRInputReferences : MonoBehaviour
    {
        public XRInputControllerReferences LeftControllerReferences;
        public XRInputControllerReferences RightControllerReferences;

        public static XRInputReferences Instance;

        private void Awake()
        {
            Instance = this;
        }

        // OnEnable + OnDestroy keep Instance in sync with rig spawn/despawn. Without this, the
        // limapp rig steals Instance on Awake, then its destruction leaves Instance pointing at a
        // fake-null reference until the next rig spawns — UnityXRController.GetButton* dereference
        // it during that gap and throw.
        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public XRInputControllerReferences GetHandInputReferences(VRInputDeviceHand handType)
        {
            switch (handType)
            {
                case VRInputDeviceHand.Left:
                    return LeftControllerReferences;
                case VRInputDeviceHand.Right:
                    return RightControllerReferences;
                case VRInputDeviceHand.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(handType), handType, "No references for hand type of NONE");
            }
        }
    }
}