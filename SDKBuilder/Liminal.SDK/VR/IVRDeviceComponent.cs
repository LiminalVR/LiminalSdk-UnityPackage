using Liminal.SDK.VR.Pointers;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// An interface for representing a single hardware component of a <see cref="IVRDevice"/>.
    /// </summary>
    public interface IVRDeviceComponent
    {
        /// <summary>
        /// Gets the name of the device component.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="IVRPointer"/> assigned to the component.
        /// </summary>
        IVRPointer Pointer { get; }
    }
}
