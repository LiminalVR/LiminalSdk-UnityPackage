using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// An abstract base implementation of <see cref="IVRReticleVisual"/>.
    /// </summary>
    public abstract class BaseReticleVisual : MonoBehaviour, IVRReticleVisual
    {
        private RaycastResult mCurrentRaycastResult;

        /// <summary>
        /// Gets or sets the current raycast result from the event system.
        /// </summary>
        public RaycastResult CurrentRaycastResult
        {
            get { return mCurrentRaycastResult; }
            set { mCurrentRaycastResult = value; }
        }
    }
}
