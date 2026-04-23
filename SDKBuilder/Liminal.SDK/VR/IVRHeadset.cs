namespace Liminal.SDK.VR
{
    /// <summary>
    /// An interface representing the HMD component of a VR device.
    /// </summary>
    public interface IVRHeadset : IVRDeviceComponent
    {
        /// <summary>
        /// Indicates if the headset has any of the capabilities specified by the supplied capabilities mask. Returns true if the device has all the capabilities specified by the mask.
        /// </summary>
        /// <param name="capabilities">A mask of <see cref="VRHeadsetCapability"/> flags to test for.</param>
        /// <returns>A boolean indicating if the device has all of the capabilities specified by the mask.</returns>
        bool HasCapabilities(VRHeadsetCapability capabilities);
    }
}
