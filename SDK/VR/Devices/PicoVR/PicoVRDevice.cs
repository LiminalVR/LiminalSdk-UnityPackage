﻿using System;
using System.Collections;
using System.Collections.Generic;
using Liminal.SDK.Core;
using Liminal.SDK.OpenVR;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.PicoVR
{
    public class PicoVRDevice : IVRDevice
    {
        public string Name => "Pico Device";
        public int InputDeviceCount => 3;
        public IVRHeadset Headset => new SimpleHeadset("Pico Headset", VRHeadsetCapability.PositionalTracking);
        
        public IEnumerable<IVRInputDevice> InputDevices { get; }
        public IVRInputDevice PrimaryInputDevice { get; }
        public IVRInputDevice SecondaryInputDevice { get; }

        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }

        public bool HasCapabilities(VRDeviceCapability capabilities) => VRDeviceCommonUtils.HasCapabilities(capabilities, VRDeviceCommonUtils.GenericDeviceCapability);

        public PicoVRDevice()
        {
            PrimaryInputDevice = new PicoVRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = new PicoVRController(VRInputDeviceHand.Left);

            InputDevices = new List<IVRInputDevice>
            {
                PrimaryInputDevice,
                SecondaryInputDevice,
            };
        }

        public void SetupAvatar(IVRAvatar avatar)
        {
            // Just add the avatar in the scene first, find it and try to hook up the hands
            //var rig = avatar.Transform.GetComponentInChildren<Pvr_UnitySDKManager>();
            //var experienceApp = GameObject.FindObjectOfType<ExperienceApp>();
            //var rig = experienceApp.GetComponentInChildren<Pvr_UnitySDKManager>(includeInactive:true);
            var rigPrefab = Resources.Load("PicoVRRig");
            var rig = GameObject.Instantiate(rigPrefab) as GameObject;

            //rig.transform.SetParent(avatar.Auxiliaries);

            var pvrControllers = rig.GetComponentInChildren<Pvr_Controller>();
            var leftController = pvrControllers.controller0.GetComponent<Pvr_ControllerModuleInit>();
            var rightController = pvrControllers.controller1.GetComponent<Pvr_ControllerModuleInit>();

            avatar.PrimaryHand.TrackedObject = new PicoVRTrackedControllerProxy(rightController, avatar.Head.Transform, avatar.Transform);
            avatar.SecondaryHand.TrackedObject = new PicoVRTrackedControllerProxy(leftController, avatar.Head.Transform, avatar.Transform);

            BindPointer(leftController, avatar.SecondaryHand);
            BindPointer(rightController, avatar.PrimaryHand);

            avatar.InitializeExtensions();
        }

        public void BindPointer(MonoBehaviour controller, IVRAvatarHand hand)
        {
            // If a laser pointer visual is not found, this means the prefab for each controller don't have a laser in it.
            var pointerVisual = controller.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
            pointerVisual.Bind(hand.InputDevice.Pointer);
            hand.InputDevice.Pointer.Transform = pointerVisual.transform;
        }

        #region Unused

        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;

        public void Update() { }

        #endregion
    }
}