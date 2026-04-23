using Liminal.SDK.VR.Avatars.Events;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// When an object with this component attached becomes anchored to a limb, the component will activates the specified <see cref="IVRPointerVisual"/>.
    /// When the object is unanchored, the pointer visual will be unbound and deactivated.
    /// </summary>
    [AddComponentMenu("VR/Interaction/Activate Pointer on Anchored")]
    public class ActivatePointerOnAnchor : MonoBehaviour, IAnchorableAnchored, IAnchorableUnanchored
    {
        [Tooltip("The pointer visual to activate for the limb when attached.")]
        [SerializeField] private BasePointerVisual m_PointerVisual = null;

        #region MonoBehaviour

        private void Awake()
        {
            if (m_PointerVisual == null)
                m_PointerVisual = GetComponentInChildren<BasePointerVisual>();
        }

        #endregion

        #region Event Handlers
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void OnAnchored(IVRAvatarLimb limb)
        {
            if (limb == null)
                return;

            if (m_PointerVisual != null)
            {
                var pointer = limb.DeviceComponent.Pointer;
                m_PointerVisual.Bind(pointer);
            }
        }

        public void OnUnanchored(IVRAvatarLimb limb)
        {
            if (m_PointerVisual != null)
            {
                m_PointerVisual.Unbind();
                m_PointerVisual.gameObject.SetActive(false);
            }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        #endregion
    }
}
