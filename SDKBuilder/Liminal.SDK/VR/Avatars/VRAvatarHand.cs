using Liminal.SDK.VR.Input;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A concrete implmentation of <see cref="IVRAvatarHand"/>, representing a hand limb.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Hand")]
    public class VRAvatarHand : VRAvatarLimb, IVRAvatarLimb, IVRAvatarHand
    {
        public override IVRDeviceComponent DeviceComponent { get { return InputDevice; } }

        /// <summary>
        /// Gets the <see cref="IVRInputDevice"/> currently assigned to this hand.
        /// </summary>
        public IVRInputDevice InputDevice
        {
            get
            {
                var device = VRDevice.Device;
                var inputDevice = device.InputDevices.FirstOrDefault(d => d.Hand == LimbToHand(LimbType));
                return inputDevice;
            }
        }

        /// <summary>
        /// Convert VRAvatarLimbType to VRAvatarHandType
        /// </summary>
        /// <param name="limbType"></param>
        /// <returns></returns>
        private VRInputDeviceHand LimbToHand(VRAvatarLimbType limbType)
        {
            switch (limbType)
            {
                case VRAvatarLimbType.LeftHand:
                    return VRInputDeviceHand.Left;

                case VRAvatarLimbType.RightHand:
                    return VRInputDeviceHand.Right;

                default:
                    return VRInputDeviceHand.None;
            }
        }
    }
}
