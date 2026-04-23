using Liminal.SDK.VR;

namespace Liminal.SDK.Input
{
    /// <summary>
    /// A helper class that provides information about the input capabilities of the current VR device.
    /// </summary>
    public static class VRInput
    {
        /// <summary>
        /// Indicates if the current VR input supports dual controllers.
        /// </summary>
        public static bool SupportsDualControllers
        {
            get
            {
                var device = VRDevice.Device;
                if (device == null)
                    return false;

                return device.HasCapabilities(VRDeviceCapability.DualController);
            }
        }
    }
}
