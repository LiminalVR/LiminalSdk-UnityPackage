using System;
using System.Collections.Generic;
using Liminal.SDK.VR.Input;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using ISCommonUsages = UnityEngine.InputSystem.CommonUsages;
using ISInputDevice = UnityEngine.InputSystem.InputDevice;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// Mapping from Liminal SDK -> New Unity XR Input System. This is used by the XRInputDevice to get the correct input action for each button/axis.
    /// </summary>
    public class XRInputReferences : MonoBehaviour
    {
        public XRInputControllerReferences LeftControllerReferences;
        public XRInputControllerReferences RightControllerReferences;

        public static XRInputReferences Instance;

        private void Awake()
        {
            Instance = this;
        }

        // OnEnable + OnDestroy keep Instance in sync with rig spawn/despawn. Without this, the
        // limapp rig steals Instance on Awake, then its destruction leaves Instance pointing at a
        // fake-null reference until the next rig spawns — UnityXRController.GetButton* dereference
        // it during that gap and throw.
        private void OnEnable()
        {
            Instance = this;

            InputSystem.onDeviceChange += OnDeviceChange;
            foreach (var device in InputSystem.devices)
                FixHandUsage(device);
            AddUsageFallbackBindings();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private static void OnDeviceChange(ISInputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
                FixHandUsage(device);
        }

        /// <summary>
        /// Works around an Input System race: XR devices that arrive before the OpenXR interaction
        /// profile registers its layout get stuck on an auto-generated XRInputV1 layout with no
        /// XRController base and no hand usage, and are never rebuilt because the re-match keeps
        /// producing the same layout name (InputManager.RecreateDevicesUsingLayoutWithInferiorMatch
        /// short-circuits when the name is unchanged). Bindings like
        /// "&lt;XRController&gt;{RightHand}/{TriggerButton}" then resolve to zero controls.
        /// The device's controls still carry proper usages, so restoring the hand usage here (and
        /// adding layout-free fallback bindings below) makes the actions work again.
        /// </summary>
        private static void FixHandUsage(ISInputDevice device)
        {
            var description = device.description;
            if (string.IsNullOrEmpty(description.interfaceName)
                || !description.interfaceName.StartsWith("XRInput", StringComparison.Ordinal)
                || device.usages.Count > 0
                || string.IsNullOrEmpty(description.capabilities))
                return;

            XRDeviceDescriptor descriptor;
            try
            {
                descriptor = XRDeviceDescriptor.FromJson(description.capabilities);
            }
            catch (Exception)
            {
                return;
            }

            if (descriptor == null)
                return;

            if ((descriptor.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Right) != 0)
                InputSystem.SetDeviceUsage(device, ISCommonUsages.RightHand);
            else if ((descriptor.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Left) != 0)
                InputSystem.SetDeviceUsage(device, ISCommonUsages.LeftHand);
        }

        /// <summary>
        /// For every action referenced by this rig, mirrors each "&lt;XRController&gt;{Hand}/..."
        /// binding with a layout-free "{Hand}/..." variant. The original binding requires the
        /// device layout to derive from XRController, which the stuck generated layouts don't;
        /// the fallback only needs the hand usage (restored by <see cref="FixHandUsage"/>) and the
        /// control usage, both of which the stuck devices have. Idempotent: skips paths already
        /// present, so multiple rigs/spawns won't duplicate bindings.
        /// </summary>
        private void AddUsageFallbackBindings()
        {
            AddUsageFallbackBindings(LeftControllerReferences);
            AddUsageFallbackBindings(RightControllerReferences);
        }

        private const string XRControllerLayoutPrefix = "<XRController>";

        private static void AddUsageFallbackBindings(XRInputControllerReferences references)
        {
            if (references == null)
                return;

            AddUsageFallbackBindings(references.Trigger);
            AddUsageFallbackBindings(references.Joystick);
            AddUsageFallbackBindings(references.Two);
            AddUsageFallbackBindings(references.Three);
            AddUsageFallbackBindings(references.Four);
            AddUsageFallbackBindings(references.Touch);
            AddUsageFallbackBindings(references.Back);
        }

        private static void AddUsageFallbackBindings(InputActionReference reference)
        {
            var action = reference != null ? reference.action : null;
            if (action == null)
                return;

            List<string> missingFallbacks = null;
            var bindings = action.bindings;

            for (int i = 0; i < bindings.Count; i++)
            {
                var path = bindings[i].path;
                if (string.IsNullOrEmpty(path) || !path.StartsWith(XRControllerLayoutPrefix, StringComparison.Ordinal))
                    continue;

                var fallbackPath = path.Substring(XRControllerLayoutPrefix.Length);
                if (string.IsNullOrEmpty(fallbackPath) || HasBindingWithPath(bindings, fallbackPath))
                    continue;

                missingFallbacks ??= new List<string>();
                if (!missingFallbacks.Contains(fallbackPath))
                    missingFallbacks.Add(fallbackPath);
            }

            if (missingFallbacks == null)
                return;

            var wasEnabled = action.enabled;
            if (wasEnabled)
                action.Disable();

            foreach (var path in missingFallbacks)
                action.AddBinding(path);

            if (wasEnabled)
                action.Enable();
        }

        private static bool HasBindingWithPath(UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputBinding> bindings, string path)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].path == path)
                    return true;
            }
            return false;
        }

        public XRInputControllerReferences GetHandInputReferences(VRInputDeviceHand handType)
        {
            switch (handType)
            {
                case VRInputDeviceHand.Left:
                    return LeftControllerReferences;
                case VRInputDeviceHand.Right:
                    return RightControllerReferences;
                case VRInputDeviceHand.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(handType), handType, "No references for hand type of NONE");
            }
        }
    }
}