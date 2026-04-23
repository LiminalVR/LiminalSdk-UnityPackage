using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using System.Collections.Generic;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// An event handler delegate that takes a single <see cref="IVRDevice"/> argument.
    /// </summary>
    /// <param name="vrDevice">The <see cref="IVRDevice"/> that event relates to.</param>
    public delegate void VRDeviceEventHandler(IVRDevice vrDevice);

    /// <summary>
    /// An event handler delegate relating to a VR input device.
    /// </summary>
    /// <param name="vrDevice">The <see cref="IVRDevice"/> that event relates to.</param>
    /// <param name="inputDevice">The <see cref="IVRInputDevice"/> the event relates to.</param>
    public delegate void VRInputDeviceEventHandler(IVRDevice vrDevice, IVRInputDevice inputDevice);

    /// <summary>
    /// An interface representing a VR hardware device.
    /// </summary>
    public interface IVRDevice
    {
        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the number of input devices currently connected.
        /// </summary>
        int InputDeviceCount { get; }

        /// <summary>
        /// Gets the <see cref="IVRHeadset"/> of the device.
        /// </summary>
        IVRHeadset Headset { get; }

        /// <summary>
        /// Gets an enumerable collection of <see cref="IVRInputDevice"/> objects currently connected.
        /// </summary>
        IEnumerable<IVRInputDevice> InputDevices { get; }

        /// <summary>
        /// Gets the primary input device, if connected and available.
        /// </summary>
        IVRInputDevice PrimaryInputDevice { get; }

        /// <summary>
        /// Gets the secondary input device, if connected and available.
        /// </summary>
        IVRInputDevice SecondaryInputDevice { get; }

        /// <summary>
        /// Raised when a <see cref="IVRInputDevice"/> is connected.
        /// </summary>
        event VRInputDeviceEventHandler InputDeviceConnected;

        /// <summary>
        /// Raised when a <see cref="IVRInputDevice"/> is disconnected.
        /// </summary>
        event VRInputDeviceEventHandler InputDeviceDisconnected;

        /// <summary>
        /// Raised when the primary <see cref="IVRInputDevice"/> has changed.
        /// </summary>
        event VRDeviceEventHandler PrimaryInputDeviceChanged;
        
        /// <summary>
        /// Indicates if the device has a specific set of capabilities. This method returns true only if ALL values within the <paramref name="capabilities"/> bitmask are available on the device.
        /// </summary>
        /// <param name="capabilities">The capabilities to check. This value is a bitmask of <see cref="VRDeviceCapability"/> values.</param>
        /// <returns>A boolean indicating if the device has ALL the capabilities specified by the <paramref name="capabilities"/> bitmask.</returns>
        bool HasCapabilities(VRDeviceCapability capabilities);

        /// <summary>
        /// [Internal use] Sets up an <see cref="IVRAvatar"/> instance for this device. You should not have to call this manually.
        /// </summary>
        /// <param name="avatar">The <see cref="IVRAvatar"/> instance to bind to the device.</param>
        void SetupAvatar(IVRAvatar avatar);

        /// <summary>
        /// [Internal use] Updates the state of the device. You do not have to call this manually.
        /// </summary>
        void Update();

        /// <summary>
        /// Control the CPU clock speed if possible. See OVRManager.cpuLevel.
        /// </summary>
        int CpuLevel { get; set; }

        /// <summary>
        /// Control the CPU clock speed if possible. See OVRManager.gpuLevel.
        /// </summary>
        int GpuLevel { get; set; }
    }
}
