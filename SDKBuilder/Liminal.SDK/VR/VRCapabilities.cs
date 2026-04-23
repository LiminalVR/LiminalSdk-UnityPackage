using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// A collection of capabilities for VR devices.
    /// </summary>
    public enum VRDeviceCapability
    {
        None = 0,

        /// <summary>
        /// Indicates if the device supports at least one controller.
        /// </summary>
        Controller = 1 << 0,

        /// <summary>
        /// Indicates if the device supports at least two controllers.
        /// </summary>
        DualController = 1 << 1,

        /// <summary>
        /// Indicates if the device supports user-presence detection.
        /// </summary>
        UserPrescenceDetection = 1 << 2,
    }

    /// <summary>
    /// A collection of capabilities for VR headsets.
    /// </summary>
    public enum VRHeadsetCapability
    {
        None = 0,

        /// <summary>
        /// Indicates if the headset supports positional tracking (true for 6 DOF headsets).
        /// </summary>
        PositionalTracking = 1 << 0,

        /// <summary>
        /// Indicates if the headset has an external camera.
        /// </summary>
        ExternalCamera = 1 << 1,

        /// <summary>
        /// Indicates if the headset has an external stereo camera.
        /// </summary>
        ExternalStereoCamera = 1 << 2,

        /// <summary>
        /// True if the device has a digital input pad on the headset itself
        /// </summary>
        HeadsetDPad = 1 << 3,
    }

    /// <summary>
    /// A collection of capabilities for VR input devices.
    /// </summary>
    public enum VRInputDeviceCapability
    {
        None = 0,

        /// <summary>
        /// Indicates if an input device has positional tracking (true for 6 DOF input devices).
        /// </summary>
        PositionalTracking = 1 << 0,

        /// <summary>
        /// Indicates if an input device has a directional input method (eg. joystick or touchpad).
        /// </summary>
        DirectionalInput = 1 << 1,

        /// <summary>
        /// Indicates if an input device supports touch events (ie. touch-down, touch-up, touching) (true for devices with touchpads).
        /// </summary>
        Touch = 1 << 2,

        /// <summary>
        /// Indicates if the controller has a trigger button.
        /// </summary>
        TriggerButton = 1 << 3,

        /// <summary>
        /// Indicates if the controller has a digital input pad.
        /// </summary>
        DPad = 1 << 5,

        /// <summary>
        /// Indicates if the controller supports haptic feedback.
        /// </summary>
        Haptic = 1 << 6,
    }
}
