namespace Liminal.SDK.VR.Avatars.Extensions
{
    /// <summary>
    /// An interface for all <see cref="IVRAvatar"/> extension components.
    /// </summary>
    public interface IVRAvatarExtension
    {
        /// <summary>
        /// [Internal use] Initializes the extension component for the specified avatar.
        /// </summary>
        /// <param name="avatar">The avatar that owns the extension component.</param>
        void Initialize(IVRAvatar avatar);
    }
}
