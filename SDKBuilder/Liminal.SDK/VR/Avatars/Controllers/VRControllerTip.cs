using UnityEngine;
using UnityEngine.UI;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// A basic tooltip visual.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRControllerTip : MonoBehaviour
    {
        [Tooltip("The Text field that the tooltip label string is assigned to.")]
        [SerializeField] private Text m_LabelText = null;

        #region Properties

        /// <summary>
        /// Gets or sets the label displayed on the tooltip.
        /// </summary>
        public string Label
        {
            get
            {
                if (m_LabelText == null)
                    return null;

                return m_LabelText.text;
            }

            set
            {
                if (m_LabelText == null)
                    return;

                m_LabelText.text = value;
            }
        }

        #endregion
    }
}
