using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// An interface that should be placed on components that handle anchoring events.
    /// </summary>
    public interface IAnchorHandler
    {
        /// <summary>
        /// Gets or sets the enabled state of the handler.
        /// </summary>
        bool enabled { get; set; }

        /// <summary>
        /// Applies modifications to the position of the anchored object.
        /// </summary>
        /// <param name="current">The current position.</param>
        /// <param name="target">The target position.</param>
        void ModifyPosition(ref Vector3 current, ref Vector3 target);

        /// <summary>
        /// Applies modifications to the rotation of the anchored object.
        /// </summary>
        /// <param name="current">The current rotation.</param>
        /// <param name="target">The target rotation.</param>
        void ModifyRotation(ref Quaternion current, ref Quaternion target);
    }
}
