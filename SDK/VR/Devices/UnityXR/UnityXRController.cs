using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using Object = UnityEngine.Object;
using System.Linq;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using UnityEngine.InputSystem;

namespace Liminal.SDK.XR
{
	/// <summary>
	/// Mappings and further manual information available here: https://docs.unity3d.com/Manual/xr_input.html
	/// All of the below are on a per-controller basis and may or may not exist depending on the platform that it currently running
	/// 
	/// Buttons:
	/// - primaryButton
	/// - secondaryButton
	/// - secondaryTouch
	/// - gripButton
	/// - triggerButton
	/// - menuButton
	/// - primary2DAxisClick
	/// - primary2DAxisTouch
	/// - userPresence (WMR, Oculus)
	/// 
	/// Axis:
	/// - trigger
	/// - grip
	/// - batteryLevel (only WMR)
	/// 
	/// 2D Axis:
	/// - primary2DAxis
	/// - secondary2DAxis
	/// </summary>
	public class UnityXRController : UnityXRInputDevice
	{
		public override string Name => "UnityXRController";

		// TODO: Confirm this?
		public override int ButtonCount => 3;

		// this is mapped to 'primaryTouch' inputFeature
		public override bool IsTouching { get => GetButton(VRButton.Touch); }

		private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput |
																		VRInputDeviceCapability.Touch |
																		VRInputDeviceCapability.TriggerButton;
		private VRInputDeviceHand mHand;
		public override VRInputDeviceHand Hand => mHand;

        private ActionBasedController _controller;
        private IVRAvatarHand _avatarHand;
        private LaserPointerVisual _pointer;


		public UnityXRController(VRInputDeviceHand hand) : base(OVRUtils.GetControllerType(hand))
		{
			mHand = hand;
			Pointer?.Activate();
			Debug.Log($"[{GetType().Name}] UnityXRController({hand}) created.");
        }

		public UnityXRController()
		{
		}

		protected override IVRPointer CreatePointer()
		{
			return new InputDevicePointer(this);
		}

		public override bool HasCapabilities(VRInputDeviceCapability capabilities)
		{
			return ((_capabilities & capabilities) == capabilities);
		}

		public override bool HasAxis1D(string axis)
        {
            return false;
        }

		public override bool HasAxis2D(string axis)
		{
            return false;
		}

		public override bool HasButton(string button)
        {
            return false;
        }

		public override float GetAxis1D(string axis)
		{
			if (!HasAxis1D(axis)) return 0f;

            return 0f;
        }

		public override Vector2 GetAxis2D(string axis)
        {
            var action = GetInputAction(axis);
            return action.ReadValue<Vector2>();
		}

		public override bool GetButton(string button) 
            => GetInputAction(button).IsPressed();
		public override bool GetButtonDown(string button) 
            => GetInputAction(button).WasPressedThisFrame();
		public override bool GetButtonUp(string button) 
            => GetInputAction(button).WasReleasedThisFrame();

		public XRInputControllerReferences InputRefs => XRInputReferences.Instance.GetHandInputReferences(Hand);

        public void Bind(ActionBasedController controller, IVRAvatarHand avatarHand, LaserPointerVisual pointer)
        {
            _controller = controller;
            _avatarHand = avatarHand;
            _pointer = pointer;
			SyncControllers();
        }

		public override void Update()
        {
        }

        public void SyncControllers()
        {
            var controllerVisual = _avatarHand.Transform.GetComponentInChildren<VRAvatarController>();
            var hasControllerVisual = controllerVisual != null;

			// This is the default angle we use for all experiences. Some experiences might feel awkward if we start using 0.
            var laserPointerXAngle = 0;
			
            if (hasControllerVisual)
            {
                _controller.hideControllerModel = false;

                //_controller.modelParent = _avatarHand.Anchor;
                _controller.modelParent = controllerVisual.transform;

				//_controller.transform.SetParent(controllerVisual.transform);
                _controller.model.localPosition = Vector3.zero;
                _controller.model.localEulerAngles = new Vector3(laserPointerXAngle, 0, 0);
            }
            else
            {
                _controller.hideControllerModel = true;
            }

			//Could it be wrong one or need to be after? Make am ethod to call this after
			// Pointer might be null going into this experience, should we do another check?
            if (_pointer != null)
            {
                Debug.Log("[Unity XR Controller] - Setting up pointers");

                _pointer.transform.SetParent(_controller.model);
                _pointer.transform.localPosition = Vector3.zero;
                _pointer.transform.localEulerAngles = new Vector3(laserPointerXAngle, 0, 0);
                _pointer.transform.name += $"(Modified) - {_pointer.transform.localEulerAngles}";
            }
            else
            {
                Debug.Log("[Unity XR Controller] - No pointers found.");
            }
        }

		/// <summary>
		/// We decided to provide an input refs as we don't want a major dependency on the ActionBasedController.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public InputAction GetInputAction(string name)
        {
            return InputRefs.GetInputAction(name);
        }
	}
}
