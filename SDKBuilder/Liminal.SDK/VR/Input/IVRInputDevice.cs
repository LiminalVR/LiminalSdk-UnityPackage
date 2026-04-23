using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.VR.Input
{
    /// <summary>
    /// An interface that represents a hardware VR input device (such as a controller).
    /// </summary>
    public interface IVRInputDevice : IVRDeviceComponent
    {        
        /// <summary>
        /// Gets the number of buttons the device has.
        /// </summary>
        int ButtonCount { get; }
        
        /// <summary>
        /// Gets the hand the device is assigned to.
        /// </summary>
        VRInputDeviceHand Hand { get; }

        /// <summary>
        /// Indicates if the input device has a specific set of capabilities. This method returns true only if ALL values within the <paramref name="capabilities"/> bitmask are available on the input device.
        /// </summary>
        /// <param name="capabilities">The capabilities to check. This value is a bitmask of <see cref="VRInputDeviceCapability"/> values.</param>
        /// <returns>A boolean indicating if the input device has ALL the capabilities specified by the <paramref name="capabilities"/> bitmask.</returns>
        bool HasCapabilities(VRInputDeviceCapability capabilities);

        /// <summary>
        /// Indicates if the input device has a 1-dimensional axis with the specified name.
        /// </summary>
        /// <param name="axis">The axis name.</param>
        /// <returns>A boolean value indicating if the a 1-dimensional axis with the specified name exists on the input device.</returns>
        bool HasAxis1D(string axis);

        /// <summary>
        /// Indicates if the input device has a 2-dimensional axis with the specified name.
        /// </summary>
        /// <param name="axis">The axis name.</param>
        /// <returns>A boolean value indicating if the a 2-dimensional axis with the specified name exists on the input device.</returns>
        bool HasAxis2D(string axis);

        /// <summary>
        /// Indicates if the input device has a button with the specified name.
        /// </summary>
        /// <param name="button">The button name.</param>
        /// <returns>A boolean value indicating if the a button with the specified name exists on the input device.</returns>
        bool HasButton(string button);

        /// <summary>
        /// Gets the current value of the specified 1-dimensional axis on the input device.
        /// </summary>
        /// <param name="axis">The axis name.</param>
        /// <returns>The current value of the axis on the input device.</returns>
        float GetAxis1D(string axis);

        /// <summary>
        /// Gets the current value of the specified 2-dimensional axis on the input device.
        /// </summary>
        /// <param name="axis">The axis name.</param>
        /// <returns>The current value of the axis on the input device.</returns>
        Vector2 GetAxis2D(string axis);

        /// <summary>
        /// Returns true if the specified button is currently being held.
        /// </summary>
        /// <param name="button">The button name.</param>
        /// <returns>A boolean indicating if the specified button is currently being held.</returns>
        bool GetButton(string button);

        /// <summary>
        /// Returns true if the specified button was pressed on this frame. This value is true only for the single frame when the button was initially pressed.
        /// </summary>
        /// <param name="button">The button name.</param>
        /// <returns>A boolean indicating if the specified button was pressed this frame.</returns>
        bool GetButtonDown(string button);

        /// <summary>
        /// Returns true if the specified button was released on this frame. This value is true only for the single frame when the button was released.
        /// </summary>
        /// <param name="button">The button name.</param>
        /// <returns>A boolean indicating if the specified button was released this frame.</returns>
        bool GetButtonUp(string button);

        /// <summary>
        /// Returns true if the touchpad is being touched
        /// </summary>
        bool IsTouching { get; }

    }
}