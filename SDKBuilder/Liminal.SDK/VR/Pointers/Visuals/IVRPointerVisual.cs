using UnityEngine;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// An interface for the visual components of a <see cref="IVRPointer"/>.
    /// </summary>
    public interface IVRPointerVisual
    {
        /// <summary>
        /// Gets the transfrom for the pointer visual.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Gets the <see cref="IVRPointer"/> instance the visual is bound to.
        /// </summary>
        IVRPointer Pointer { get; }
        
        /// <summary>
        /// Binds the pointer visual to an <see cref="IVRPointer"/> instance.
        /// </summary>
        /// <param name="pointer"></param>
        void Bind(IVRPointer pointer);

        /// <summary>
        /// Unbinds the pointer visual from the current <see cref="IVRPointer"/> instance.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Sets the active state of the pointer visual.
        /// </summary>
        /// <param name="activeState">The active state of the pointer visual.</param>
        void SetActive(bool activeState);
    }
}
