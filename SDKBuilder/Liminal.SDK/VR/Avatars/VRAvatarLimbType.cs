namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An enumeration of <see cref="IVRAvatar"/> limb types.
    /// </summary>
    public enum VRAvatarLimbType
    {
        /// <summary>
        /// Default value, represents no limb.
        /// </summary>
        None,

        /// <summary>
        /// The head limb.
        /// </summary>
        Head,

        /// <summary>
        /// The left-hand limb.
        /// </summary>
        LeftHand,

        /// <summary>
        /// The right-hand limb.
        /// </summary>
        RightHand,

        /// <summary>
        /// Any other type of limb.
        /// </summary>
        Other,
    }
}
