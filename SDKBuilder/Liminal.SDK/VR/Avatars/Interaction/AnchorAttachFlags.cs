using System;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// Flags for controlling the behaviour of limb anchor attachments.
    /// </summary>
    [Flags]
    public enum AnchorAttachFlags
    {
        None = 0,

        /// <summary>
        /// Determines if an object should be reparented to the anchor when attached.
        /// </summary>
        ReparentToAnchor = 1 << 0,

        /// <summary>
        /// Determines if anchor handlers are ignored when attaching an object.
        /// </summary>
        IgnoreAnchorHandlers = 1 << 1,

        /// <summary>
        /// The default settings for attached an object to an anchor.
        /// </summary>
        Default = ReparentToAnchor,
    }
}
