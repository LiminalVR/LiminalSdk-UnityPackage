using Liminal.SDK.VR.Input;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An extension interface for <see cref="IVRAvatarLimb"/> for hand limbs.
    /// </summary>
    public interface IVRAvatarHand : IVRAvatarLimb
    {
        /// <summary>
        /// Gets the <see cref="IVRInputDevice"/> currently assigned to this hand.
        /// </summary>
        IVRInputDevice InputDevice { get; }
    }
}
