using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// A node that when assigned to child objects <see cref="VRControllerVisual"/> will allow for easy attachment points on the controller in a generic way.
    /// </summary>
    public class VRControllerNode : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// The name of the controller node at the top of the controller.
        /// </summary>
        public const string Tip = "Tip";

        /// <summary>
        /// The name of the controller node at the base of the controller.
        /// </summary>
        public const string Base = "Base";

        /// <summary>
        /// The name of the controller node at the center of the controller body.
        /// </summary>
        public const string Center = "Center";

        #endregion

        [Tooltip("The lookup name of the node. This should correspond to button, axis values, or VRControllerNode constants.")]
        [SerializeField] private string m_NodeName = null;

        #region Properties

        /// <summary>
        /// Gets the name of the node.
        /// </summary>
        public string NodeName
        {
            get { return m_NodeName; }
        }

        #endregion
    }
}
