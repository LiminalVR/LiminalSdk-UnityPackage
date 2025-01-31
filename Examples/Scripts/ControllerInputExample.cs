using System.Collections.Generic;
using System.Linq;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using System.Text;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using Liminal.SDK.VR.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class ControllerInputExample : MonoBehaviour
{
    public Text InputText;

    private void Update()
    {
        var device = VRDevice.Device;
        if (device != null)
        {
            StringBuilder inputStringBuilder = new StringBuilder("");

            AppendDeviceInput(inputStringBuilder, device.PrimaryInputDevice, "Primary");
            inputStringBuilder.AppendLine();
            AppendDeviceInput(inputStringBuilder, device.SecondaryInputDevice, "Secondary");

            InputText.text = inputStringBuilder.ToString();
        }

        var avatar = VRAvatar.Active;
        if (avatar != null)
        {
            var rightHand = avatar.PrimaryHand;
            var helper = rightHand.Transform.GetComponentInChildren<GearVRControllerInputVisual>(true);
        }
    }

    public void SendControllerHaptics()
    {
        var device = VRDevice.Device;
        device?.PrimaryInputDevice?.SendInputHaptics(.5f, .5f, 0.05f);
    }

    public void SetControllerVisibility(bool state)
    {
        var avatar = VRAvatar.Active;
        avatar.PrimaryHand.SetControllerVisibility(state);
        avatar.SecondaryHand.SetControllerVisibility(state);
    }

    /// <summary>
    /// This example only hide the left hand pointer so you can still use the right hand to re-activate it.
    /// </summary>
    /// <param name="state"></param>
    public void SetPointerVisibility(bool state)
    {
        GearVRAvatar.PointerActivationType = EPointerActivationType.None;

        var avatar = VRAvatar.Active;

        switch (state)
        {
            case false:
                //avatar.PrimaryHand?.InputDevice?.Pointer.Deactivate();
                avatar.SecondaryHand?.InputDevice?.Pointer.Deactivate();
                break;
            case true:
                avatar.PrimaryHand?.InputDevice?.Pointer.Activate();
                avatar.SecondaryHand?.InputDevice?.Pointer.Activate();
                break;
        }
    }

    [ContextMenu("Hide Controllers")]
    public void TestHideControllers()
    {
        SetControllerVisibility(false);
    }

    public void AppendDeviceInput(StringBuilder builder, IVRInputDevice inputDevice, string deviceName)
    {
        if (inputDevice == null)
            return;

        builder.AppendLine($"{deviceName} Back: {inputDevice.GetButton(VRButton.Back)}");
        builder.AppendLine($"{deviceName} Touch Pad Touching: {inputDevice.IsTouching}");
        builder.AppendLine($"{deviceName} Trigger: {inputDevice.GetButton(VRButton.Trigger)}");
        builder.AppendLine($"{deviceName} Primary: {inputDevice.GetButton(VRButton.Primary)}");
        builder.AppendLine($"{deviceName} Secondary: {inputDevice.GetButton(VRButton.Seconday)}");
        builder.AppendLine($"{deviceName} Three: {inputDevice.GetButton(VRButton.Three)}");
        builder.AppendLine($"{deviceName} Four: {inputDevice.GetButton(VRButton.Four)}");

        inputDevice.GetButtonDown(VRButton.One);

        /*
        builder.AppendLine($"{deviceName} Axis One: {inputDevice.GetAxis2D(VRAxis.One)}");
        builder.AppendLine($"{deviceName} Axis One Raw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");

        builder.AppendLine($"{deviceName} Axis Two: {inputDevice.GetAxis1D(VRAxis.Two)}");
        builder.AppendLine($"{deviceName} Axis Two Raw: {inputDevice.GetAxis1D(VRAxis.TwoRaw):0.00}");

        builder.AppendLine($"{deviceName} Axis Three: {inputDevice.GetAxis1D(VRAxis.Three)}");
        builder.AppendLine($"{deviceName} Axis Three Raw: {inputDevice.GetAxis1D(VRAxis.ThreeRaw):0.00}");
        */

        builder.AppendLine($"{deviceName} Axis2D-One: {inputDevice.GetAxis2D(VRAxis.One)}");
        builder.AppendLine($"{deviceName} Axis2D-OneRaw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");
    }

    public void End() 
    {
        ExperienceApp.End();
    }
}