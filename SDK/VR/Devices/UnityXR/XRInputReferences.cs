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
        public XROrigin XROrigin;
        public XRInputControllerReferences LeftControllerReferences;
        public XRInputControllerReferences RightControllerReferences;

        public static XRInputReferences Instance;

        private void Awake()
        {
            Instance = this;
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