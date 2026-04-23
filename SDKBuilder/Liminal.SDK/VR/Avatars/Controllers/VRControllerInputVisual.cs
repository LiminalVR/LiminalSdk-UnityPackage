using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Input;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// Used to represent an input component visual (buttons, touchpads, triggers, etc) on a VR Controller device.
    /// </summary>
    [AddComponentMenu("")]
    public abstract class VRControllerInputVisual : MonoBehaviour
    {
        private GameObject mTooltipInstance;
        private VRControllerTipAlignment mCachedAlignment;
        private bool mTooltipVisible;

        [Tooltip("The input name the visual component relates to. This should correspond to VRButton or VRAxis values.")]
        [SerializeField] private string m_InputName = null;
        [Tooltip("The direction of tooltips displayed on this visual component will face.")]
        [SerializeField] private VRControllerTipAlignment m_TipAlignment = VRControllerTipAlignment.Left;
        [SerializeField] private Transform m_TipParentProxy = null;

        #region Properties

        /// <summary>
        /// Gets the input name the visual component relates to. This should correspond to <see cref="VRButton"/> or <see cref="VRAxis"/> values.
        /// </summary>
        public string InputName
        {
            get { return m_InputName; }
        }

        /// <summary>
        /// Gets or sets the color of the input visual.
        /// </summary>
        public abstract Color Color
        {
            get; set;
        }

        /// <summary>
        /// Gets the direction of tooltips displayed on this visual component.
        /// </summary>
        public VRControllerTipAlignment TipAlignment
        {
            get { return m_TipAlignment; }
            set
            {
                m_TipAlignment = value;
                if (mTooltipVisible)
                    ShowTip();
            }
        }

        /// <summary>
        /// Indicates if the tooltip for this input visual is currently visible.
        /// </summary>
        public bool TipVisible
        {
            get { return mTooltipVisible; }
        }

        #endregion

        #region MonoBehaviour

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                // Alignment changed?
                if (mTooltipVisible && m_TipAlignment != mCachedAlignment)
                    ShowTip();
            }
        }

        #endregion

        /// <summary>
        /// Resets the color override of the visual component.
        /// </summary>
        public abstract void ResetColor();

        /// <summary>
        /// Displays a tooltip for this input visual.
        /// </summary>
        /// <returns>The tooltip GameObject instance, or null if no instance was able to be created.</returns>
        public GameObject ShowTip()
        {
            if (m_TipAlignment != mCachedAlignment)
            {
                DestroyTooltipInstance();
                mCachedAlignment = m_TipAlignment;
            }

            if (mTooltipInstance == null)
                CreateTooltipInstance();

            if (mTooltipInstance == null)
                return null;

            mTooltipVisible = true;

            mTooltipInstance.gameObject.SetActive(true);
            return mTooltipInstance;
        }

        /// <summary>
        /// Displays a tooltip for this input visual and returns a component of the specified by <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the Component to retrieve from the tip.</typeparam>
        /// <returns>The Component of the type specified by <typeparamref name="T"/> on the tooltip GameObject instance,
        /// or null if no instance was able to be created or the instance does not have a component of the specified type.
        /// </returns>
        public T ShowTip<T>() where T : Component
        {
            var tip = ShowTip();
            if (tip == null)
                return null;

            return tip.GetComponent<T>();
        }

        /// <summary>
        /// Hides the tooltip for this input visual, if one exists.
        /// </summary>
        public void HideTip()
        {
            if (mTooltipInstance != null)
                mTooltipInstance.gameObject.SetActive(false);

            mTooltipVisible = false;
        }

        private void DestroyTooltipInstance()
        {
            if (mTooltipInstance != null)
            {
                Destroy(mTooltipInstance.gameObject);
                mTooltipInstance = null;
            }
        }

        private void CreateTooltipInstance()
        {
            if (VRAvatar.Active == null)
            {
                Debug.LogError("No active VRAvatar found.", this);
                return;
            }

            var avatarXform = VRAvatar.Active.Transform;
            if (avatarXform == null)
            {
                Debug.LogError("No transform found for the active VRAvatar.", this);
                return;
            }

            var library = avatarXform.GetComponent<VRControllerTipPrefabs>();
            if (library == null)
            {
                Debug.LogError("No VRControllerTipPrefabs library found. Add a VRControllerTipPrefabs component to the VRAvatar to use tooltips.", this);
                return;
            }

            var prefab = library.GetPrefab(m_TipAlignment);
            if (prefab == null)
            {
                Debug.LogError(string.Format("No prefab assigned to direction {0} in the tip library.", m_TipAlignment.ToString()), library);
                return;
            }

            var parent = (m_TipParentProxy != null)
                ? m_TipParentProxy
                : transform;

            mTooltipInstance = Instantiate(prefab);
            mTooltipInstance.transform.SetParentAndIdentity(parent);
        }
    }
}
