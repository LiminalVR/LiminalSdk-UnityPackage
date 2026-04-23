using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// Allows an <see cref="Anchorable"/> object to be attached to a <see cref="IVRAvatarLimb"/> when clicked with a VR pointer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Anchorable))]
    [AddComponentMenu("VR/Interaction/Attach On Click")]
    public class AttachOnClick : MonoBehaviour, IPointerClickHandler
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void OnPointerClick(PointerEventData eventData)
        {
            // Find the limb that sent this event
            var limb = VRAvatar.Active.GetLimb(eventData);
            if (limb != null)
            {
                // Attach the object to the limb anchor and consume the event
                limb.Attach(gameObject);
                eventData.Use();
            }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
