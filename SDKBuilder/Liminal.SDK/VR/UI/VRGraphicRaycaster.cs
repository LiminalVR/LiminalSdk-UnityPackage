using Liminal.SDK.VR.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Liminal.SDK.VR.UI
{
    /// <summary>
    /// A <see cref="GraphicRaycaster"/> implementation that is compatible with the <see cref="VRPointerInputModule"/> input module. Add this component
    /// to your Unity UI <see cref="Canvas"/> objects to allow interaction with VR devices.
    /// </summary>
    public class VRGraphicRaycaster : GraphicRaycaster
    {
        /// <summary>
        /// The camera that will generate rays for this raycaster.
        /// </summary>
        public override Camera eventCamera
        {
            get
            {
                // Use the camera for the controllers via the VR input module
                return VRPointerInputModule.RaycastEventCamera;
            }
        }
    }
}
