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

		private UnityXRController mRightController;
		private UnityXRController mLeftController;
		public List<UnityXRInputDevice> XRInputs { get; } = new List<UnityXRInputDevice>();
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

#region Constructors
		public UnityXRDevice()
        {
            Setup();
        }

        public void Setup()
        {
            Headset = new UnityXRHeadset();
            PrimaryInputDevice = mRightController = new UnityXRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = mLeftController = new UnityXRController(VRInputDeviceHand.Left);

            InputDevices = new List<IVRInputDevice>
            {
                PrimaryInputDevice,
                SecondaryInputDevice,
            };

            XRInputs.Add(mRightController);
            XRInputs.Add(mLeftController);
        }

        #endregion

        private void UpdateHandVisibility()
        {
        }

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		public void SetupAvatar(IVRAvatar avatar)
        {
            if (!Data.ContainsKey(avatar))
            {
                var setupData = new SetupData();
                Data[avatar] = setupData;
            }

            // It may be a smart idea to duplicate existing rig for comparison.
            /*var copy = GameObject.Instantiate(avatar.Transform.gameObject, avatar.Transform.parent);
            copy.transform.name = "Avatar Copy";
            copy.gameObject.SetActive(false);*/

            Debug.Log("[UnityXRDevice] Setting up avatar");
            Assert.IsNotNull(avatar);

			// Clean up existing pointers.
			if(VRDevice.Device?.PrimaryInputDevice?.Pointer != null)
                VRPointerInputModule.RemovePointer(VRDevice.Device.PrimaryInputDevice.Pointer);

            if (VRDevice.Device?.SecondaryInputDevice?.Pointer != null)
				VRPointerInputModule.RemovePointer(VRDevice.Device.SecondaryInputDevice.Pointer);

			var unityAvatar = avatar.Transform.gameObject.GetComponent<UnityXRAvatar>();

			if(unityAvatar == null)
                unityAvatar = avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();

            unityAvatar.gameObject.SetActive(true);

            Debug.Log("[UnityXRDevice] Setting up managers");
			SetupManager(avatar);

            Debug.Log("[UnityXRDevice] Creating XR Rig");
			var rig = CreateXRRig(avatar);

            Debug.Log("[UnityXRDevice] Setup Camera Rig");
			SetupCameraRig(avatar, rig);

            Debug.Log("[UnityXRDevice] Initialize Avatar");
			// Does this need to happen a second time? Probably not!
			unityAvatar.Initialize(avatar, this);

            Debug.Log("[UnityXRDevice] Setup Controllers");
			SetupControllers(avatar, rig);
		}

        private XROrigin CreateXRRig(IVRAvatar avatar)
        {
			// Maybe a way to pass in one?
			// Instantiate a new one
            var xrReferences = avatar.Transform.GetComponentInChildren<XRInputReferences>(true);
            
            //if(xrReferences == null)
            //    xrReferences = GameObject.FindObjectOfType<XRInputReferences>();
			
            if (xrReferences == null)
            {
                var xrRigPrefab = Resources.Load("Complete XR Origin Set Up Variant");
                var rigObject = GameObject.Instantiate(xrRigPrefab) as GameObject;
                xrReferences = rigObject.GetComponent<XRInputReferences>();
            }

			// unless you need to offset the difference...
            xrReferences.transform.SetParent(avatar.Transform);
			xrReferences.transform.position = avatar.Transform.position;
			xrReferences.transform.rotation = avatar.Transform.rotation;

            return xrReferences.XROrigin;
        }

		private void SetupControllers(IVRAvatar avatar, XROrigin rig)
        {
			var controllers = rig.GetComponentsInChildren<ActionBasedController>().ToList();
			var leftHand = controllers.FirstOrDefault(x => x.transform.name.Contains("LeftHand"));
			var rightHand = controllers.FirstOrDefault(x => x.transform.name.Contains("RightHand"));

			avatar.PrimaryHand.TrackedObject = new UnityXRTrackedControllerProxy(rightHand, avatar);
			avatar.SecondaryHand.TrackedObject = new UnityXRTrackedControllerProxy(leftHand, avatar);

			var rightPointer = ActivatePointer(avatar.PrimaryHand, rightHand);
			var leftPointer = ActivatePointer(avatar.SecondaryHand, leftHand);

            mRightController.Bind(rightHand, avatar.PrimaryHand, rightPointer);
			mLeftController.Bind(leftHand, avatar.SecondaryHand, leftPointer);
		}

		public LaserPointerVisual ActivatePointer(IVRAvatarHand hand, ActionBasedController xrController)
        {
			// check if the hand even have a controller attached. // Do we include inactive ones?
            var controllerComponent = hand.Transform.GetComponentInChildren<VRAvatarController>(includeInactive:true);
            if (controllerComponent == null)
            {
				Debug.Log("No avatar controller found");
                return null;
            }
			
            //var controllerPrefabName = xrController.controllerNode == XRNode.LeftHand ? "Neo3_L" : "Neo3_R";
            var controllerPrefabName = xrController.transform.name.Contains("LeftHand") ? "PicoNeo_Controller_Visual_L" : "PicoNeo_Controller_Visual_R";
			
            //var prefab = Resources.Load($"Prefabs/{controllerPrefabName}");
            //var controllerInstance = GameObject.Instantiate(prefab, controllerComponent.transform) as GameObject;
            //controllerInstance.transform.localPosition = Vector3.zero;
            //controllerInstance.transform.localEulerAngles = Vector3.zero;

			// if the controller instance has laser pointer, no pointer making one

			var pointerVisual = xrController.GetComponentInChildren<LaserPointerVisual>();

            if (pointerVisual == null)
            {
                pointerVisual = controllerComponent.GetComponentInChildren<LaserPointerVisual>();
            }

            if (pointerVisual == null)
            {
                var laserPointerPrefab = Resources.Load("LaserPointer");
                var pointerVisualGO = (GameObject) GameObject.Instantiate(laserPointerPrefab, controllerComponent.transform);
                pointerVisual = pointerVisualGO.GetComponent<LaserPointerVisual>();
            }

			
            pointerVisual?.Bind(hand.InputDevice.Pointer);

			// Apparently it's binding to the wrong one.
			var device = hand.InputDevice as UnityXRController;
			Debug.Log($"Binding: Input Device: , Hand: {device.Hand}, Avatar Device: {hand.Transform.name}");

			hand.InputDevice.Pointer.Transform = pointerVisual.transform;

			// Always add it back in into the list.
            VRPointerInputModule.RemovePointer(hand.InputDevice.Pointer);
            VRPointerInputModule.AddPointer(hand.InputDevice.Pointer);
            hand.InputDevice.Pointer.Activate();
            return pointerVisual;
        }

		private void SetupManager(IVRAvatar avatar)
        {
            var interactionManager = avatar.Transform.GetComponentInChildren<XRInteractionManager>();
            var manager = interactionManager ?? new GameObject("XRInteractionManager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(avatar.Transform);
        }

        public Dictionary<IVRAvatar, SetupData> Data = new Dictionary<IVRAvatar, SetupData>();

        public class SetupData
        {
            public GameObject _tracker;
            public GameObject _offset;
            public TrackedPoseDriver _cameraDriver;
        }

		private void SetupCameraRig(IVRAvatar avatar, XROrigin xrRig)
        {
			//avatar.Head.Transform.SetParent(xrRig.Camera.transform);
            xrRig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            xrRig.Camera.enabled = false;

			// Deduct Default Device Tracking Height from the avatar head as these experiences originally built for Oculus Go
			// The head is placed at least 1.7 up. TBH this needs more investigation.
            //var defaultDeviceTrackingHeight = 1.7f;
			//Ripple Effect 1.5f. But that doesn't mean other experiences do that?
			// Some may be 1.7?
            //avatar.Head.Transform.position -= Vector3.up * defaultDeviceTrackingHeight;

			// Create a tracker, this tracker will be driven by the real life camera as local position from floor.
			// The head is used as an offset. This is because existing limapps use case move the head up and around etc.
			// Note, they also use VRAvatar or maybe nesting of the avatar. 

            var setupData = Data[avatar];

            if(setupData._tracker == null)
                setupData._tracker = new GameObject("Tracker");

            if(setupData._offset == null)
                setupData._offset = new GameObject("Offset");

            setupData._offset.transform.SetParent(avatar.Head.Transform);
            setupData._offset.transform.Identity();

            setupData._tracker.transform.SetParent(setupData._offset.transform);
            setupData._tracker.transform.Identity();

            // We move the center eye but the tracker gets destroyed!
			avatar.Head.CenterEyeCamera.transform.SetParent(setupData._tracker.transform);
			avatar.Head.CenterEyeCamera.transform.Identity();

            if(setupData._cameraDriver == null)
                setupData._cameraDriver = setupData._tracker.AddComponent<TrackedPoseDriver>();

			RecenterHeight(setupData);

            _startTime = Time.time;
        }

        private float _startTime;

		// You want to try call this at some point later after launching the experience or setting up the avatar instead.

        public static bool UpdateControllers = true;

        [Obsolete("No longer used, was used prior to Pico Neo when XR did not have Device Tracking.")]
        public static bool UpdateHeight = false;

        public SetupData GetData(IVRAvatar avatar)
        {
            return avatar == null ? null : Data.GetValueOrDefault(avatar);
        }

        /// <summary>
        /// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
        /// </summary>
        public void Update()
        {
            var avatar = VRAvatar.Active;
            var setupData = GetData(avatar);

            if (setupData == null || setupData._tracker == null)
                return;
            
			// The camera floor offset object is necessary to match the head position to make sure the controllers are in place.
			// Do note a problem that will exist is if you bend up and down, that would kind of add an offset?

			// I think was for PICO
            var xrRig = CreateXRRig(avatar);
            xrRig.CameraFloorOffsetObject.transform.position = setupData._offset.transform.position;

            var elapsed = Time.time - _startTime;

            if (UpdateHeight)
            {
                if (elapsed < 3)
                    RecenterHeight(setupData);
            }

            if (UpdateControllers)
            {
                //if (elapsed > .2f && elapsed < 1)
                {
                    mRightController.SyncControllers();
                    mLeftController.SyncControllers();
                }
            }
        }

        /// <summary>
        /// Some Liminal Experiences are 6m in the sky and we expect that's where the user's POV starts from regardless or how tall they are.
        /// So thi s method offset it by the real world height from ground to line the user up.
        /// </summary>
		public void RecenterHeight(SetupData data)
        {
            var realWorldHeight = data._tracker.transform.localPosition.y;
			var targetLocalPosition = new Vector3(0, -realWorldHeight, 0);
            data._offset.transform.localPosition = targetLocalPosition;
        }
	}
}


