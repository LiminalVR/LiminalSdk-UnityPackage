using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// The top level class the manages the VRDevice infrastructure.
    /// </summary>
    public static class VRDevice
    {
        private static VRDeviceMonitor _deviceMonitor;
        private static IVRDevice _activeDevice;
        private static bool _initialized;

        #region Properties

        /// <summary>
        /// Indicates if the the VRDevice framework has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Indicates if an <see cref="IVRDevice"/> has been assigned.
        /// </summary>
        public static bool HasDevice
        {
            get { return _activeDevice != null; }
        }

        /// <summary>
        /// Gets the current <see cref="IVRDevice"/>. Returns null if there are no active devices.
        /// </summary>
        public static IVRDevice Device
        {
            get { return _activeDevice; }
        }

        /// <summary>
        /// Gets the name of the connected <see cref="IVRDevice"/>. Returns null if no devices are connected.
        /// </summary>
        public static string DeviceName
        {
            get
            {
                if (_activeDevice == null)
                    return string.Empty;

                return _activeDevice.Name;
            }
        }

        /// <summary>
        /// Determines the presence state of the user.
        /// Not all VR devices support this feature and may return a value of <see cref="UserPresenceState.Unsupported"/>.
        /// </summary>
        public static bool IsUserPresent
        {
            get
            {
                Debug.LogError("User present not implemented in Liminal SDK Builder - VR Device");
                return true;
            }
        }

        /// <summary>
        /// Gets the tracking space type from Unity's XRDevice. Liminal recommends using the <see cref="IVRDevice.HasCapabilities(VRDeviceCapability)"/> to determine tracking capabilities of the connected device.
        /// </summary>
        public static TrackingSpaceType XRTrackingSpaceType
        {
            get { return XRDevice.GetTrackingSpaceType(); }
        }
        
        /// <summary>
        /// Gets the model name of the connected from Unity's XRDevice.
        /// </summary>
        public static string XRModelName
        {
            get { return SystemInfo.deviceModel; }
        }

        /// <summary>
        /// Gets the refresh rate of the connected VR device from Unity's XRDevice.
        /// </summary>
        public static float RefreshRate
        {
            get { return XRDevice.refreshRate; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the <see cref="VRDevice"/> framework has completed initialization.
        /// </summary>
        public static event Action Initialized;

        #endregion

        public static void Replace(IVRDevice device)
        {
            _activeDevice = device;
        }

        /// <summary>
        /// Initializes the VRDevice framework for the specified <see cref="IVRDevice"/> instance.
        /// </summary>
        /// <param name="device">The <see cref="IVRDevice"/> to initialize.</param>
        public static void Initialize(IVRDevice device)
        {
            if (_initialized)
                return;

            if (device == null)
                throw new ArgumentNullException("device");

            _initialized = true;
            _activeDevice = device;

            if (_activeDevice == null)
            {
                Debug.LogError("[VRDevice] No VR device was detected");
            }
            else
            {
                Debug.LogFormat("[VRDevice] Device: {0} ({1})", _activeDevice.Name, _activeDevice.GetType().FullName);
                Debug.LogFormat("[VRDevice] Headset={0}", _activeDevice.Headset.Name);
                Debug.LogFormat("[VRDevice] InputDevices={0}", string.Join(";", _activeDevice.InputDevices.Select(x => string.Format("{0}", x.Name)).ToArray()));

                CreateDeviceMonitorIfRequired();
            }
            
            if (Initialized != null)
                Initialized();
        }
        
        /// <summary>
        /// Loads the specified VR device into Unity's XR support system.
        /// </summary>
        /// <returns>The loading task.</returns>
        public static IEnumerator LoadXRDevice(string device)
        {
            Debug.LogFormat("[VRDevice] XRSettings.supportedDevices={0}", string.Join(";", XRSettings.supportedDevices));

            // Load the specified device
            if (!string.IsNullOrEmpty(device) && (XRSettings.loadedDeviceName != device))
            {
                if (Array.IndexOf(XRSettings.supportedDevices, device) == -1)
                {
                    Debug.LogFormat("[VRDevice] Unsupported XR device: {0}", device);
                }
                else
                {
                    Debug.LogFormat("[VRDevice] Loading XR device: {0}", device);
                    XRSettings.LoadDeviceByName(device);
                }
            }

            // Wait a frame for the device to be loaded...
            yield return null;

            // Done, renable XR
            XRSettings.enabled = true;
            Debug.LogFormat("[VRDevice] XRSettings.loadedDeviceName={0}", XRSettings.loadedDeviceName);
        }
        
        private static void CreateDeviceMonitorIfRequired()
        {
            _deviceMonitor = UnityEngine.Object.FindObjectOfType<VRDeviceMonitor>();
            if (_deviceMonitor == null)
            {
                _deviceMonitor = new GameObject("VRDeviceMonitor")
                {
                    hideFlags = HideFlags.HideAndDontSave
                }
                .AddComponent<VRDeviceMonitor>();
            }

            _deviceMonitor.StartMonitoring();
        }
    }
}
