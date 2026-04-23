using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// An interface for the visual components of a pointer reticle.
    /// </summary>
    public interface IVRReticleVisual
    {
        /// <summary>
        /// Gets or sets the current raycast result from the event system.
        /// </summary>
        RaycastResult CurrentRaycastResult { get; set; }
    }
}
