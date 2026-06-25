using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;

namespace Liminal.SDK.V2
{
    public static class DeviceManager
    {
        public static IVRDevice Device;

        public static void Initialize(IVRDevice device)
        {
            Device = device;
            VRDevice.Initialize(device);
        }

        public static IVRInputDevice GetInput(VRAvatarLimbType limbType)
        {
            if(limbType == VRAvatarLimbType.LeftHand)
            {
                return Device.SecondaryInputDevice;
            }
            else
            {
                return Device.PrimaryInputDevice;
            }
        }
    }
}