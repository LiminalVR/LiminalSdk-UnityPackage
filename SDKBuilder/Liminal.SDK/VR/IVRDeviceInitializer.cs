namespace Liminal.SDK.VR
{
    /// <summary>
    /// An interface for allowing an object to create <see cref="IVRDevice"/> instances.
    /// </summary>
    public interface IVRDeviceInitializer
    {
        /// <summary>
        /// Creates a new <see cref="IVRDevice"/> and returns it.
        /// </summary>
        /// <returns>The <see cref="IVRDevice"/> that was created.</returns>
        IVRDevice CreateDevice();
    }
}
