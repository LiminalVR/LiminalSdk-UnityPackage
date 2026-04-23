using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A concrete implementation of <see cref="IVRAvatarHead"/>, representing the head limb.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Head")]
    public class VRAvatarHead : VRAvatarLimb, IVRAvatarHead
    {
        private bool mEyeCameraUpdated;

        [Header("Head")]
        [Tooltip("If true, the left and right eye cameras are used and the center eye camera is disabled. If false, the center eye camera is used and the left/right eye cameras are disabled.")]
        [SerializeField] private bool m_UsePerEyeCameras = false;
        [Tooltip("The center-eye camera. This camera should have its stereo output target set to 'Both'. This camera is only used if UsePerEyeCameras is false.")]
        [SerializeField] private Camera m_CenterEyeCamera = null;
        [Tooltip("The left-eye camera. This camera should have its stereo output target set to 'Left'. This camera is only used if UsePerEyeCameras is true.")]
        [SerializeField] private Camera m_LeftEyeCamera = null;
        [Tooltip("The right-eye camera. This camera should have its stereo output target set to 'Right'. This camera is only used if UsePerEyeCameras is true.")]
        [SerializeField] private Camera m_RightEyeCamera = null;

        #region Properties

        /// <summary>
        /// Gets the <see cref="IVRDeviceComponent"/> the limb is assigned to.
        /// </summary>
        public override IVRDeviceComponent DeviceComponent
        {
            get
            {
                var device = VRDevice.Device;
                return (device != null) ? device.Headset : null;
            }
        }

        /// <summary>
        /// Gets the <see cref="IVRHeadset"/> attached to this limb.
        /// </summary>
        public IVRHeadset Headset
        {
            get
            {
                var device = VRDevice.Device;
                return (device != null) ? device.Headset : null;
            }
        }

        /// <summary>
        /// Gets the active eye camera. If <see cref="UsePerEyeCameras"/> is <code>true</code>, this will return <see cref="LeftEyeCamera"/> if active, otherwise
        /// it will return <see cref="CenterEyeCamera"/>. This value should be used if you need to reference the camera, but do not need a specific eye camera.
        /// </summary>
        public Camera ActiveEyeCamera
        {
            get
            {
                if (!mEyeCameraUpdated)
                    UpdateActiveEyeCameras(true);

                if (m_UsePerEyeCameras && m_LeftEyeCamera != null)
                    return m_LeftEyeCamera;

                return m_CenterEyeCamera;
            }
        }

        /// <summary>
        /// Gets the center eye camera. This camera has <see cref="Camera.stereoTargetEye"/> set to <see cref="StereoTargetEyeMask.Both"/>.
        /// </summary>
        public Camera CenterEyeCamera
        {
            get { return m_CenterEyeCamera; }
        }

        /// <summary>
        /// Gets the left eye camera. This camera has <see cref="Camera.stereoTargetEye"/> set to <see cref="StereoTargetEyeMask.Left"/>.
        /// </summary>
        public Camera LeftEyeCamera
        {
            get { return m_LeftEyeCamera; }
        }

        /// <summary>
        /// Gets the right eye camera. This camera has <see cref="Camera.stereoTargetEye"/> set to <see cref="StereoTargetEyeMask.Right"/>.
        /// </summary>
        public Camera RightEyeCamera
        {
            get { return m_RightEyeCamera; }
        }

        /// <summary>
        /// Determines if the left/right eye cameras are used, or if center eye camera is used.
        /// If true, the left and right eye cameras are used and the center eye camera is disabled. If false, the center eye camera is used and the left/right eye cameras are disabled.
        /// </summary>
        public bool UsePerEyeCameras
        {
            get { return m_UsePerEyeCameras; }
            set
            {
                if (value == m_UsePerEyeCameras)
                    return;

                m_UsePerEyeCameras = value;
                UpdateActiveEyeCameras(false);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the active camera is changed.
        /// </summary>
        public event ActiveCameraChangedEventHandler ActiveCameraChanged;

        #endregion

        #region MonoBehaviours

        protected override void Awake()
        {
            base.Awake();
            UpdateActiveEyeCameras(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ActiveCameraChanged = null;
        }

        #endregion

        private void UpdateActiveEyeCameras(bool suppressEvent)
        {
            mEyeCameraUpdated = true;

            if (m_UsePerEyeCameras)
            {
                if (m_LeftEyeCamera == null || m_RightEyeCamera == null)
                {
                    // If there is a missing camera, disable and update again
                    m_UsePerEyeCameras = false;
                    UpdateActiveEyeCameras(suppressEvent);
                    return;
                }

                // Enable both left/right cameras
                m_LeftEyeCamera.gameObject.SetActive(true);
                m_LeftEyeCamera.enabled = true;

                m_RightEyeCamera.gameObject.SetActive(true);
                m_RightEyeCamera.enabled = true;

                // NOTE: Only the camera itself is disabled here - otherwise the ears and other
                // center-anchored objects would be disabled
                if (m_CenterEyeCamera != null)
                    m_CenterEyeCamera.enabled = false;
            }
            else
            {
                // Disable left/right eyes
                if (m_LeftEyeCamera != null)
                {
                    m_LeftEyeCamera.gameObject.SetActive(false);
                    m_LeftEyeCamera.enabled = false;
                }

                if (m_RightEyeCamera != null)
                {
                    m_RightEyeCamera.gameObject.SetActive(false);
                    m_RightEyeCamera.enabled = false;
                }

                // Enable center eye camera
                if (m_CenterEyeCamera != null)
                    m_CenterEyeCamera.enabled = true;
            }

            if (!suppressEvent)
            {
                // Raise event
                if (ActiveCameraChanged != null)
                    ActiveCameraChanged(this);
            }

        }
    }
}
