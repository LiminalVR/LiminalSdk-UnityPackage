using Liminal.SDK.VR.Avatars.Controllers;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An interface for implementing device-specific avatar behaviours.
    /// </summary>
    public interface IVRDeviceAvatar
    {
        /// <summary>
        /// Gets the <see cref="IVRAvatar"/> for this device controller.
        /// </summary>
        IVRAvatar Avatar { get; }

        /// <summary>
        /// Instantiates a <see cref="VRControllerVisual"/> for a limb.
        /// </summary>
        /// <param name="limb">The limb for the controller.</param>
        /// <returns>The newly instantiated controller visual for the specified limb, or null if no controller visual was able to be created.</returns>
        VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb);
    }
}
