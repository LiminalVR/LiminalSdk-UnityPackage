using Liminal.SDK.VR.Pointers;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// An interface for controller visuals.
    /// </summary>
    public interface IVRControllerVisual
    {
        /// <summary>
        /// Gets the GameObject for the controller visual.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Gets the GameObject for the controller visual.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Gets or sets the <see cref="IVRPointerVisual"/> for the controller.
        /// </summary>
        IVRPointerVisual PointerVisual { get; }

        /// <summary>
        /// Gets the enumerable collection of controller nodes belonging to the visual.
        /// </summary>
        IEnumerable<VRControllerNode> Nodes { get; }

        /// <summary>
        /// Gets the enumerable collection of controller input visuals belonging to the visual.
        /// </summary>
        IEnumerable<VRControllerInputVisual> Inputs { get; }

        /// <summary>
        /// Gets the the <see cref="VRControllerNode"/> with the name specified by <paramref name="nodeName"/>.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>The <see cref="VRControllerNode"/> with the name specified by <paramref name="nodeName"/>.</returns>
        VRControllerNode GetNode(string nodeName);

        /// <summary>
        /// Gets the the <see cref="VRControllerInputVisual"/> with the name specified by <paramref name="inputName"/>.
        /// </summary>
        /// <param name="inputName">The name of the input component.</param>
        /// <returns>The <see cref="VRControllerInputVisual"/> with the name specified by <paramref name="inputName"/>.</returns>
        VRControllerInputVisual GetInput(string inputName);
    }
}
