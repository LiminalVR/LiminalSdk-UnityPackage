using Liminal.SDK.Core;
using Liminal.SDK.OpenVR;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;
using Liminal.SDK.XR;
using UnityEngine;
using UnityEngine.XR;

namespace App
{
    public static class DeviceUtils
    {
        public static IVRDevice CreateDevice()
        {
            // TODO Add an environment SDK Configuration.
#if UNITY_XR
            return CreateDevice(ESDKType.UnityXR);
#else
            var sdkType = Application.platform == RuntimePlatform.Android ? ESDKType.OVR : ESDKType.OpenVR;
            if (Application.isEditor)
                sdkType = ESDKType.Emulator;
            else
                sdkType = ESDKType.OVR;

            return CreateDevice(sdkType);
#endif
        }

        // This is used by the Editor
        public static IVRDevice CreateDevice(ESDKType sdkType)
        {
            Debug.Log("[Device Utils] - Forcing UNITY XR for Android for now.");

            if (!Application.isEditor)
            {
                return new UnityXRDevice();
            }

            switch (sdkType)
            {
#if UNITY_XR
                case ESDKType.UnityXR:
                    Debug.Log("[Device Utils] - Creating Unity XR");
                    return new UnityXRDevice();
#endif
                case ESDKType.OVR:
#if UNITY_6000_7_OR_NEWER
                    // Oculus' legacy loader was removed from Unity. Quest and Link use OpenXR.
                    return new UnityXRDevice();
#else
                    Debug.Log("[Device Utils] - Creating OVR");
                    XRSettings.enabled = true;
                    return new GearVRDevice();
#endif

                case ESDKType.OpenVR:
                    Debug.Log("[Device Utils] - Creating OpenVR");
                    return new OpenVRDevice();

                default:
                    return new EmulatorDevice(VREmulatorDevice.Daydream);
            }
        }
    }
}
