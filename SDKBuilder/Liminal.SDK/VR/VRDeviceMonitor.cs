using System.Collections;
using UnityEngine;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// A silent background component that runs coroutines for managing the state of VRDevice.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class VRDeviceMonitor : MonoBehaviour
    {
        /// <summary>
        /// The rate at which the monitor update cycle will tick.
        /// </summary>
        const float MonitorInterval = 0.5f;

        private static VRDeviceMonitor _instance;

        private bool mMonitoring;
        private Coroutine mMonitorRoutine;

        #region MonoBehaviour

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            var device = VRDevice.Device;
            if (device != null)
            {
                device.Update();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnEnable()
        {
            // This section has been refactored to always update instead of having an update interval.
            // This was an early optimization that made little sense.
            /*if (mMonitoring)
            {
                // Restart the monitoring routine if we were not told to ever stop
                mMonitorRoutine = null;
                StartMonitoring();
            }*/
        }

        private void OnDisable()
        {
            mMonitorRoutine = null;
        }

        #endregion

        /// <summary>
        /// Begins the device monitoring routine.
        /// </summary>
        public void StartMonitoring()
        {
            if (mMonitorRoutine != null)
                return;

            mMonitorRoutine = StartCoroutine(Monitor());
            mMonitoring = true;
        }

        /// <summary>
        /// Stops the device monitoring routine.
        /// </summary>
        public void StopMonitoring()
        {
            if (mMonitorRoutine != null)
            {
                StopCoroutine(mMonitorRoutine);
                mMonitorRoutine = null;
            }

            mMonitoring = false;
        }

        private IEnumerator Monitor()
        {
            while (true)
            {
                // Update the connected device
                var device = VRDevice.Device;
                if (device != null)
                    device.Update();

                yield return Wait(MonitorInterval);
            }
        }

        private IEnumerator Wait(float duration)
        {
            var startTime = Time.realtimeSinceStartup;
            while ((Time.realtimeSinceStartup - startTime) < duration)
                yield return null;
        }
    }
}
