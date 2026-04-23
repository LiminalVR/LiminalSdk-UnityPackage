using Liminal.SDK.VR.Input;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// Extension methods for the <see cref="IVRDevice"/> interface.
    /// </summary>
    public static class IVRDeviceExtensions
    {
        /// <summary>
        /// Set the primary pointer active
        /// </summary>
        /// <param name="device"></param>
        /// <param name="isActive"></param>
        public static void SetPrimaryPointerActive(this IVRDevice device, bool isActive)
        {
            var pointer = ((device != null) && (device.PrimaryInputDevice != null)) ? device.PrimaryInputDevice.Pointer : null;
            if (pointer != null)
            {
                if (isActive)
                {
                    pointer.Activate();
                }
                else
                {
                    pointer.Deactivate();
                }
            }
        }
    }
}
