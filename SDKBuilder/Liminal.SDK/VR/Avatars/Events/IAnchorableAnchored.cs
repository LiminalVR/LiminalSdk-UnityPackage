namespace Liminal.SDK.VR.Avatars.Events
{
    /// <summary>
    /// An interface that can be implemented by components that want to receive OnAnchored events from <see cref="Avatars.Interaction.Anchorable"/> components.
    /// </summary>
    public interface IAnchorableAnchored
    {
        /// <summary>
        /// Executed when an <see cref="Avatars.Interaction.Anchorable"/> component on the same object dispatched and OnAnchored event.
        /// </summary>
        /// <param name="limb">The limb that object was anchored to.</param>
        void OnAnchored(IVRAvatarLimb limb);
    }
}
