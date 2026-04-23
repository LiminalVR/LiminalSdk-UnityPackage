using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An interface for wrapping tracked objects.
    /// </summary>
    public interface IVRTrackedObjectProxy
    {
        /// <summary>
        /// Indicates if the tracked object is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the world-space position of the tracked object.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the world-space rotation of the tracked object.
        /// </summary>
        Quaternion Rotation { get; }
    }
}
