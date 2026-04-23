namespace Liminal.SDK.VR.Input
{
    /// <summary>
    /// Button mapping names for VR input devices.
    /// </summary>
    public static class VRButton
    {
        /// <summary>
        /// The 'back' button input. This button is generally supported on all devices, however functionality differs on each device.
        /// Alternative names for this button may include 'back', 'menu', 'system'.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>App</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Back</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Back</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Start</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Application Menu</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Back = "ButtonBack";

        /// <summary>
        /// The first input button. The button differs on each device, however all input devices support this button. This is usually the primary click/select mechanism.
        /// If the device supports a trigger, this will generally be the primary input. For devices that do not support a trigger, this will be the primary clicking mechanism.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>DPad Tap</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Index Trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Trigger</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string One = "ButtonOne";

        /// <summary>
        /// The second input button. This button is not supported on all devices. Usually devices that offer a touchpad and a trigger button will support <see cref="Two"/>.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Hand Trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Touchpad</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Two = "ButtonTwo";

        /// <summary>
        /// The third input button. This value is not supported on all devices. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>A</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Grip</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Three = "ButtonThree";

        /// <summary>
        /// The fourth input button. This value is not supported on all devices. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>B</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Four = "ButtonFour";

        /// <summary>
        /// The touch/stick input. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.Touch"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>DPad Tap</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Thumbstick</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Touchpad</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Touch = "ButtonTouch";

        /// <summary>
        /// The trigger input. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.TriggerButton"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR</term>
        ///         <description>Trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Index Trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Trigger</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Trigger = "ButtonTrigger";

        /// <summary>
        /// The digital input pad up button. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.DPad"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad Up</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string DPadUp = "ButtonDPadUp";

        /// <summary>
        /// The digital input pad down button. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.DPad"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad Down</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string DPadDown = "ButtonDPadDown";

        /// <summary>
        /// The digital input pad left button. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.DPad"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad Left</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string DPadLeft = "ButtonDPadLeft";

        /// <summary>
        /// The digital input pad right button. This value is only supported by input devices with the <see cref="VRInputDeviceCapability.DPad"/> capability. This button differs on each device.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad Right</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string DPadRight = "ButtonDPadRight";

        /// <summary>
        /// The primary input button. This value is synonomous with <see cref="One"/>.
        /// <seealso cref="One"/>
        /// </summary>
        public const string Primary = One;

        /// <summary>
        /// The secondary input button. This value is synonomous with <see cref="Two"/>.
        /// <seealso cref="One"/>
        /// </summary>
        public const string Seconday = Two;
    }

    /// <summary>
    /// Axis mapping names for VR input devices.
    /// </summary>
    public static class VRAxis
    {
        /// <summary>
        /// The primary input axis in the range of [(-1,-1)...(1,1)], where (0, 0) is the center of the pad or joystick. For triggers, this represents a single axis as a float.
        /// For most devices, this will be either a touchpad or joystick. 
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Thumbstick</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Touchpad</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string One = "AxisOne";

        /// <summary>
        /// The primary input axis in the range of [(0,0)...(1,1)], where (0, 0) is the top left of the pad or joystick, and (1, 1) is the bottom right of the pad or joystick.
        /// For most devices, this will be either a touchpad or joystick.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>D-Pad</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Touchpad</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Thumbstick</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Touchpad</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string OneRaw = "AxisOneRaw";

        /// <summary>
        /// The secondary input axis in the range of [(-1,-1)...(1,1)], where (0, 0) is the center of the pad or joystick. For triggers, this represents a single axis as a float.
        /// Most mobile devices do not have a secondary input axis. Using this axis is not recommended.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Hand trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Trigger</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Two = "AxisTwo";

        /// <summary>
        /// The secondary input axis in the range of [(0,0)...(1,1)], where (0, 0) is the top left of the pad or joystick, and (1, 1) is the bottom right of the pad or joystick.
        /// Most mobile devices do not have a secondary input axis. Using this axis is not recommended.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Index trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string TwoRaw = "AxisTwoRaw";

        /// <summary>
        /// The third input axis in the range of [(-1,-1)...(1,1)], where (0, 0) is the center of the pad or joystick. For triggers, this represents a single axis as a float.
        /// Most devices do not have a third input axis. Using this axis is not recommended.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Hand trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Trigger</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string Three = "AxisThree";

        /// <summary>
        /// The third input axis in the range of [(0,0)...(1,1)], where (0, 0) is the top left of the pad or joystick, and (1, 1) is the bottom right of the pad or joystick. For triggers, this represents a single axis as a float.
        /// Most devices do not have a third input axis. Using this axis is not recommended.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Device</term>
        ///         <description>Button</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Daydream</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Headset</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>GearVR Controller</term>
        ///         <description>Not Supported</description>
        ///     </item>
        ///     <item>
        ///         <term>Oculus Rift</term>
        ///         <description>Hand trigger</description>
        ///     </item>
        ///     <item>
        ///         <term>HTC Vive</term>
        ///         <description>Not Supported</description>
        ///     </item>
        /// </list>
        /// </summary>
        public const string ThreeRaw = "AxisThreeRaw";

        /// <summary>
        /// The primary input axis. This value is synonomous with <see cref="One"/>.
        /// <seealso cref="One"/>
        /// </summary>
        public const string Primary = One;

        /// <summary>
        /// The primary raw input axis. This value is synonomous with <see cref="OneRaw"/>.
        /// <seealso cref="OneRaw"/>
        /// </summary>
        public const string PrimaryRaw = OneRaw;

        /// <summary>
        /// The secondary input axis. This value is synonomous with <see cref="Two"/>.
        /// <seealso cref="Two"/>
        /// </summary>
        public const string Seconday = Two;

        /// <summary>
        /// The secondary raw input axis. This value is synonomous with <see cref="TwoRaw"/>.
        /// <seealso cref="TwoRaw"/>
        /// </summary>
        public const string SecondayRaw = TwoRaw;
    }
}
