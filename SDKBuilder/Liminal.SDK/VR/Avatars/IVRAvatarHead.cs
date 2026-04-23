using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A delegate for use with camera change events on a <see cref="IVRAvatarHead"/> instance.
    /// </summary>
    /// <param name="head">The <see cref="IVRAvatarHead"/> the event relates to.</param>
    public delegate void ActiveCameraChangedEventHandler(IVRAvatarHead head);

    /// <summary>
    /// An extension interface for <see cref="IVRAvatarLimb"/> representing the head limb.
    /// </summary>
    public interface IVRAvatarHead : IVRAvatarLimb
    {
        /// <summary>
        /// Gets the <see cref="IVRHeadset"/> currently assigned to this limb.
        /// </summary>
        IVRHeadset Headset { get; }

        /// <summary>
        /// Gets the active eye camera. If <see cref="UsePerEyeCameras"/> is <code>true</code>, this will return <see cref="LeftEyeCamera"/> if active, otherwise
        /// it will return <see cref="CenterEyeCamera"/>. This value should be used if you need to reference the camera, but do not need a specific eye camera.
        /// </summary>
        Camera ActiveEyeCamera { get; }

        /// <summary>
        /// Gets the main eye camera assigned to the head.
        /// </summary>
        Camera CenterEyeCamera { get; }

        /// <summary>
        /// Gets the main eye camera assigned to the head.
        /// </summary>
        Camera LeftEyeCamera { get; }

        /// <summary>
        /// Gets the main eye camera assigned to the head.
        /// </summary>
        Camera RightEyeCamera { get; }

        /// <summary>
        /// Determines if the left/right eye cameras are used, or if center eye camera is used.
        /// If true, the left and right eye cameras are used and the center eye camera is disabled. If false, the center eye camera is used and the left/right eye cameras are disabled.
        /// </summary>
        bool UsePerEyeCameras { get; set; }

        /// <summary>
        /// Raised when the active camera is changed.
        /// </summary>
        event ActiveCameraChangedEventHandler ActiveCameraChanged;
    }
}
