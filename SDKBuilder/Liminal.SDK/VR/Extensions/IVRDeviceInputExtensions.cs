namespace Liminal.SDK.VR
{
    /// <summary>
    /// Input extension methods for the <see cref="IVRDevice"/> interface.
    /// </summary>
    public static class IVRDeviceInputExtensions
    {
        //#TODO In the future, we need to prepare to multiple inputs, at the moment we are assuming Primary Input is the expected input.

        /// <summary>
        /// Wrapper for IVRInputDevice.GetButtonDown to handle null checks etc
        /// </summary>
        public static bool GetButtonDown(this IVRDevice device, string button)
        {
            return (device != null) && (device.PrimaryInputDevice != null) && device.PrimaryInputDevice.GetButtonDown(button);
        }

        /// <summary>
        /// Wrapper for IVRInputDevice.GetButtonDown to handle null checks etc
        /// </summary>
        public static bool GetButton(this IVRDevice device, string button)
        {
            return (device != null) && (device.PrimaryInputDevice != null) && device.PrimaryInputDevice.GetButton(button);
        }

        /// <summary>
        /// Wrapper for IVRInputDevice.GetButtonUp to handle null checks etc
        /// </summary>
        public static bool GetButtonUp(this IVRDevice device, string button)
        {
            return (device != null) && (device.PrimaryInputDevice != null) && device.PrimaryInputDevice.GetButtonUp(button);
        }
    }
}
