using Liminal.SDK.VR.Input;
using UnityEngine;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A concrete implementation of <see cref="IVRPointer"/> designed for use with <see cref="IVRInputDevice"/>.
    /// </summary>
    public class InputDevicePointer : BasePointer
    {
        private IVRInputDevice mInputDevice;

        public IVRInputDevice InputDevice { get { return mInputDevice; } }

        public InputDevicePointer(IVRInputDevice inputDevice) : base(inputDevice)
        {
            mInputDevice = inputDevice;
        }

        public override void OnPointerEnter(GameObject target) { }
        public override void OnPointerExit(GameObject target) { }

        public override bool GetButtonDown()
        {
            if (DeviceHasTrigger)
            {
                return mInputDevice.GetButtonDown(VRButton.One);
            }
            else
            {
                return  mInputDevice.GetButtonDown(VRButton.One) || mInputDevice.GetButtonDown(VRButton.Touch);
            }
        }

        public override bool GetButtonUp()
        {
            if (DeviceHasTrigger)
            {
                return mInputDevice.GetButtonUp(VRButton.One);
            }
            else
            {
                return mInputDevice.GetButtonUp(VRButton.One) || mInputDevice.GetButtonUp(VRButton.Touch);
            }
        }

        private bool DeviceHasTrigger
        {
            get
            {
                // All modern devices that we support should have a trigger button. I don't think we should complicate it too much.
                return true;
                //return !mInputDevice.HasCapabilities(VRInputDeviceCapability.TriggerButton);
            }
        }
    }
}
