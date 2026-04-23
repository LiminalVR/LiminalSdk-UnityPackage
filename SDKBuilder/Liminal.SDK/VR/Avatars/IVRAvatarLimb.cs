using Liminal.SDK.VR.Avatars.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An interface representing and avatar limb (hands, head, etc).
    /// </summary>
    public interface IVRAvatarLimb
    {
        /// <summary>
        /// Gets the <see cref="VRAvatar"/> the limb is attached to.
        /// </summary>
        IVRAvatar Avatar { get; }

        /// <summary>
        /// Gets the <see cref="IVRDeviceComponent"/> the limb is assigned to.
        /// </summary>
        IVRDeviceComponent DeviceComponent { get; }

        /// <summary>
        /// Gets the <see cref="VRAvatarLimbType"/> for this limb.
        /// </summary>
        VRAvatarLimbType LimbType { get; }

        /// <summary>
        /// Gets the <see cref="IVRTrackedObjectProxy"/> assigned to the limb.
        /// </summary>
        IVRTrackedObjectProxy TrackedObject { get; set; }

        /// <summary>
        /// Gets the transform for this limb.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// The anchor transform for the limb.
        /// </summary>
        Transform Anchor { get; }

        /// <summary>
        /// Gets the list of GameObjects currently attached to the limb anchor.
        /// </summary>
        List<GameObject> AttachedObjects { get; }

        /// <summary>
        /// Indicates if the limb is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Sets the active state of the limb.
        /// </summary>
        /// <param name="activeState">The active state of the limb.</param>
        void SetActive(bool activeState);

        /// <summary>
        /// Attachs a GameObject to the limb's anchor.
        /// </summary>
        /// <param name="gameObject">The GameObject to attach</param>
        /// <param name="flags">Settings for attaching the object.</param>
        void Attach(GameObject gameObject, AnchorAttachFlags flags = AnchorAttachFlags.Default);

        /// <summary>
        /// Removes a GameObject from the limb's anchor and returns a boolean indicating if the object was unattached.
        /// </summary>
        /// <param name="gameObject">The GameObject to unattach.</param>
        /// <param name="newParent">The new Transform to parent <paramref name="gameObject"/> to.</param>
        /// <returns>A boolean indicating if <paramref name="gameObject"/> was unattached from <see cref="Anchor"/>.</returns>
        bool Unattach(GameObject gameObject, Transform newParent = null);

        /// <summary>
        /// Unattaches all GameObjects from the limb and reparents them to <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent">The transform to parent all current attachments to.</param>
        void UnattachAll(Transform newParent = null);

        /// <summary>
        /// [Internal use only] Updates the internal state of the limb.
        /// </summary>
        void UpdateState();
    }
}
