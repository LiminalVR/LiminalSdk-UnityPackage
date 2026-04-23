using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    public delegate void PointerActiveStateChanged(IVRPointer pointer);

    /// <summary>
    /// An interface representing a pointer that can be used by VR input devices.
    /// </summary>
    public interface IVRPointer
    {
        /// <summary>
        /// Indicates if the pointer is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the <see cref="IVRDeviceComponent"/> the pointer is bound to.
        /// </summary>
        IVRDeviceComponent DeviceComponent { get; }
        
        /// <summary>
        /// Gets the current input <see cref="RaycastResult"/>.
        /// </summary>
        RaycastResult CurrentRaycastResult { get; set; }

        /// <summary>
        /// Gets the transform for the origin of the pointer ray.
        /// </summary>
        Transform Transform { get; set; }

        /// <summary>
        /// Raised when the active state of the pointer has changed.
        /// </summary>
        event PointerActiveStateChanged ActiveStateChanged;

        /// <summary>
        /// Activates the pointer.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates the pointer.
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// [Internal use] Executed when the pointer hovers over an interactable GameObject.
        /// </summary>
        /// <param name="target">The GameObject the pointer the pointer entered.</param>
        void OnPointerEnter(GameObject target);

        /// <summary>
        /// [Internal use] Executed when the pointer leaves an interactable GameObject after <see cref="OnPointerEnter(GameObject)"/> has been executed.
        /// </summary>
        /// <param name="target">The GameObject the pointer the pointer exited.</param>
        void OnPointerExit(GameObject target);

        /// <summary>
        /// [Internal use] Indicates if the primary interaction button was pressed.
        /// </summary>
        /// <returns>A boolean indicating if the primary interaction button was pressed.</returns>
        bool GetButtonDown();

        /// <summary>
        /// [Internal use] Indicates if the primary interaction button was released.
        /// </summary>
        /// <returns>A boolean indicating if the primary interaction button was released.</returns>
        bool GetButtonUp();
    }
}
