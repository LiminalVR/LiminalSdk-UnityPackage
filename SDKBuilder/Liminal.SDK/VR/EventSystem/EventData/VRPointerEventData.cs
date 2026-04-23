using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.EventSystems
{
    /// <summary>
    /// Event payload associated with VR device pointer events.
    /// </summary>
    public class VRPointerEventData : PointerEventData
    {
        #region Properties

        /// <summary>
        /// The GameObject the pointer is currently hovered over.
        /// </summary>
        public GameObject Current { get; set; }

        /// <summary>
        /// The <see cref="IVRPointer"/> that the event data data relates to.
        /// </summary>
        public IVRPointer Pointer { get; set; }

        #endregion

        public VRPointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
            //
        }

        /// <summary>
        /// Resets the event state.
        /// </summary>
        public override void Reset()
        {
            Current = null;
            Pointer = null;
            base.Reset();
        }
    }
}
