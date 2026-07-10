using System.Collections.Generic;
using System.Linq;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using System.Text;
using Liminal.SDK.V2;
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

    [Tooltip("Thumb gesture detectors to display. Found automatically when left empty.")]
    public XRThumbGestureDetector[] ThumbGestureDetectors;

    private class GestureDisplayState
    {
        public XRThumbGestureDetector Detector;
        public bool ThumbPressed;
        public string LastGesture = "None";
        public float LastGestureTime = float.NegativeInfinity;
    }

    private readonly List<GestureDisplayState> _gestureStates = new List<GestureDisplayState>();
    private readonly List<(UnityEngine.Events.UnityEvent Event, UnityEngine.Events.UnityAction Action)> _gestureSubscriptions =
        new List<(UnityEngine.Events.UnityEvent, UnityEngine.Events.UnityAction)>();

    private void OnEnable()
    {
        if (ThumbGestureDetectors == null || ThumbGestureDetectors.Length == 0)
            ThumbGestureDetectors = FindObjectsByType<XRThumbGestureDetector>(FindObjectsInactive.Include);

        foreach (var detector in ThumbGestureDetectors)
        {
            if (detector == null)
                continue;

            var state = new GestureDisplayState { Detector = detector };
            _gestureStates.Add(state);

            Subscribe(detector.OnThumbDown, () => state.ThumbPressed = true);
            Subscribe(detector.OnThumbUp, () => state.ThumbPressed = false);
            Subscribe(detector.OnThumbTap, () => SetGesture(state, "Thumb Tap"));
            Subscribe(detector.OnSwipeLeft, () => SetGesture(state, "Swipe Left"));
            Subscribe(detector.OnSwipeRight, () => SetGesture(state, "Swipe Right"));
        }
    }

    private void OnDisable()
    {
        foreach (var (gestureEvent, action) in _gestureSubscriptions)
            gestureEvent.RemoveListener(action);

        _gestureSubscriptions.Clear();
        _gestureStates.Clear();
    }

    private void Subscribe(UnityEngine.Events.UnityEvent gestureEvent, UnityEngine.Events.UnityAction action)
    {
        gestureEvent.AddListener(action);
        _gestureSubscriptions.Add((gestureEvent, action));
    }

    private static void SetGesture(GestureDisplayState state, string gestureName)
    {
        state.LastGesture = gestureName;
        state.LastGestureTime = Time.unscaledTime;
    }

    private void Update()
    {
        StringBuilder inputStringBuilder = new StringBuilder("");

        var device = DeviceManager.Device;
        if (device != null)
        {
            AppendDeviceInput(inputStringBuilder, device.PrimaryInputDevice, "Primary");
            inputStringBuilder.AppendLine();
            AppendDeviceInput(inputStringBuilder, device.SecondaryInputDevice, "Secondary");
        }

        AppendGestureInput(inputStringBuilder);

        InputText.text = inputStringBuilder.ToString();
    }

    public void AppendGestureInput(StringBuilder builder)
    {
        foreach (var state in _gestureStates)
        {
            if (state.Detector == null)
                continue;

            var hand = state.Detector.useRightHand ? "R" : "L";
            builder.AppendLine();
            builder.AppendLine($"[{hand}] Thumb Pressed: {state.ThumbPressed}");
            builder.AppendLine($"[{hand}] Joystick: {(state.Detector.JoystickActive ? state.Detector.JoystickAxis.ToString("F2") : "inactive")}");

            var age = Time.unscaledTime - state.LastGestureTime;
            builder.AppendLine(float.IsInfinity(age)
                ? $"[{hand}] Last Gesture: None"
                : $"[{hand}] Last Gesture: {state.LastGesture} ({age:F1}s ago)");
        }
    }

    public void SetControllerVisibility(bool state)
    {
        var avatar = VRAvatar.Active;
        avatar.PrimaryHand.SetControllerVisibility(state);
        avatar.SecondaryHand.SetControllerVisibility(state);
    }

    public void Log(string text)
    {
        Debug.Log(text);
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
        builder.AppendLine($"{deviceName} One: {inputDevice.GetButton(VRButton.One)}");
        builder.AppendLine($"{deviceName} Trigger: {inputDevice.GetButton(VRButton.Trigger)}");
        builder.AppendLine($"{deviceName} Primary: {inputDevice.GetButton(VRButton.Primary)}");

        bool pinching = HandPinchHold.IsHeld(inputDevice.Hand);
        builder.AppendLine($"{deviceName} pinching: {pinching}");

       
        builder.AppendLine($"{deviceName} Secondary: {inputDevice.GetButton(VRButton.Seconday)}");
        builder.AppendLine($"{deviceName} Three: {inputDevice.GetButton(VRButton.Three)}");
        builder.AppendLine($"{deviceName} Four: {inputDevice.GetButton(VRButton.Four)}");

        builder.AppendLine($"{deviceName} Axis2D-One: {inputDevice.GetAxis2D(VRAxis.One)}");
        builder.AppendLine($"{deviceName} Axis2D-OneRaw: {inputDevice.GetAxis2D(VRAxis.OneRaw)}");
    }

    public void End() 
    {
        ExperienceApp.End();
    }
}