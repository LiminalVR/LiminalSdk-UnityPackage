namespace Liminal.SDK.VR.Avatars.Events
{
    /// <summary>
    /// An interface that can be implemented by components that want to receive OnUnanchored events from <see cref="Avatars.Interaction.Anchorable"/> components.
    /// </summary>
    public interface IAnchorableUnanchored
    {
        /// <summary>
        /// Executed when an <see cref="Avatars.Interaction.Anchorable"/> component on the same object dispatched and OnUnanchored event.
        /// </summary>
        /// <param name="limb">The limb that object was unanchored from.</param>
        void OnUnanchored(IVRAvatarLimb limb);
    }
}
