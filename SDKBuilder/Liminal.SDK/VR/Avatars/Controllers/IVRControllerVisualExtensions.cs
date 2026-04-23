using System;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// Extension methods for the <see cref="IVRControllerVisual"/> interface.
    /// </summary>
    public static class VRControllerVisualExtensions
    {
        /// <summary>
        /// Gets the transform for the <see cref="VRControllerNode"/> on this controller visual with specified name.
        /// </summary>
        /// <param name="controllerVisual">The controller visual.</param>
        /// <param name="nodeName">The name of the node to retrieve the transform of.</param>
        /// <returns>The transform for the <see cref="VRControllerNode"/> on this controller visual with specified name, or null if no node is found.</returns>
        public static Transform GetNodeTransform(this IVRControllerVisual controllerVisual, string nodeName)
        {
            if (controllerVisual == null)
                throw new ArgumentNullException("controllerVisual");

            var node = controllerVisual.GetNode(nodeName);
            return (node != null) ? node.transform : null;
        }
    }
}
