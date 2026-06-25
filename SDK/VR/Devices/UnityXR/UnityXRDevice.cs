using System;
using System.Collections;
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;
using System.Linq;
using Liminal.SDK.Extensions;
using Liminal.SDK.VR.EventSystems;
using Liminal.SDK.VR.Pointers;
using Unity.XR.CoreUtils;
using UnityEngine.Events;

namespace Liminal.SDK.XR
{
	public enum UnityXRControllerMask
	{
		None = 0,
		Left = 1 << 0,
		Right = 1 << 1
	}

	/// <summary>
	/// IVRDevice implementation for the UnityXR system
	/// 
	/// UnityXR supports many systems, so individual UnityXR-prefixed scripts will handle internal wrapping or feature-specific restrictions for now.
	/// </summary>
	public class UnityXRDevice : IVRDevice
	{
		private static readonly VRDeviceCapability _capabilities = 
			VRDeviceCapability.Controller |
			// Is this VRDeviceCapability needed? Will having it in break things? ... only time will tell
			VRDeviceCapability.DualController |
			VRDeviceCapability.UserPrescenceDetection;

#region Variables
		public string Name => "UnityXR";
		public int InputDeviceCount => mInputDevicesList.Count;

		public IVRHeadset Headset { get; private set; }
		public IEnumerable<IVRInputDevice> InputDevices { get; private set; }
		private readonly List<IVRInputDevice> mInputDevicesList = new List<IVRInputDevice>();

		public IVRInputDevice PrimaryInputDevice { get; private set;  }
		public IVRInputDevice SecondaryInputDevice { get; private set; }

		private UnityXRControllerMask mControllerMask = UnityXRControllerMask.None;

		// XRNode/UnityXRController pairs to check for presence of valid controllers
		private KeyValuePair<XRNode, UnityXRControllerMask>[] mNodes =
		{
			new KeyValuePair<XRNode, UnityXRControllerMask>(XRNode.LeftHand, UnityXRControllerMask.Left),
			new KeyValuePair<XRNode, UnityXRControllerMask>(XRNode.RightHand, UnityXRControllerMask.Right)
			// head, maybe?
		};

		public int CpuLevel { get; set; }
		public int GpuLevel { get; set; }
#endregion

#region Events
		public event VRInputDeviceEventHandler InputDeviceConnected;
		public event VRInputDeviceEventHandler InputDeviceDisconnected;
		public event VRDeviceEventHandler PrimaryInputDeviceChanged;
#endregion

		public UnityXRDevice()
        {
            Setup();
        }

        public void Setup()
        {
            PrimaryInputDevice = new UnityXRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = new UnityXRController(VRInputDeviceHand.Left);

            // Populate the InputDevices collection so VRAvatarHand.InputDevice can
            // resolve a controller by hand. Without this the list stays empty and
            // Hand.InputDevice always returns null (mirrors OpenVRDevice/GearVRDevice).
            mInputDevicesList.Clear();
            mInputDevicesList.Add(PrimaryInputDevice);
            mInputDevicesList.Add(SecondaryInputDevice);
            InputDevices = mInputDevicesList;
        }

        private void UpdateHandVisibility()
        {
        }

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		public void SetupAvatar(IVRAvatar avatar)
        {

		}

        /// <summary>
        /// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
        /// </summary>
        public void Update()
        {

        }
	}
}


