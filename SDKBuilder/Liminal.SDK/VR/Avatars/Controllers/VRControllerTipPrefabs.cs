using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// A lookup component for controller tooltip prefabs. Add your custom tooltip GameObject to this lookup to have them displayed when <see cref="VRControllerInputVisual.ShowTip"/> is used.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Controller Tooltips")]
    public class VRControllerTipPrefabs : MonoBehaviour
    {
        [Tooltip("The left-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_LeftPrefab = null;
        [Tooltip("The right-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_RightPrefab = null;
        [Tooltip("The forward-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_ForwardPrefab = null;
        [Tooltip("The backward-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_BackwardPrefab = null;
        [Tooltip("The upward-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_UpPrefab = null;
        [Tooltip("The downward-aligned tooltip prefab.")]
        [SerializeField] private GameObject m_DownPrefab = null;

        #region Properties

        /// <summary>
        /// Gets or sets left-aligned tooltip prefab.
        /// </summary>
        public GameObject LeftPrefab
        {
            get { return m_LeftPrefab; }
            set { m_LeftPrefab = value; }
        }

        /// <summary>
        /// Gets or sets right-aligned tooltip prefab.
        /// </summary>
        public GameObject RightPrefab
        {
            get { return m_RightPrefab; }
            set { m_RightPrefab = value; }
        }

        /// <summary>
        /// Gets or sets forward-aligned tooltip prefab.
        /// </summary>
        public GameObject ForwardPrefab
        {
            get { return m_ForwardPrefab; }
            set { m_ForwardPrefab = value; }
        }

        /// <summary>
        /// Gets or sets backward-aligned tooltip prefab.
        /// </summary>
        public GameObject BackwardPrefab
        {
            get { return m_BackwardPrefab; }
            set { m_BackwardPrefab = value; }
        }

        /// <summary>
        /// Gets or sets upward-aligned tooltip prefab.
        /// </summary>
        public GameObject UpPrefab
        {
            get { return m_UpPrefab; }
            set { m_UpPrefab = value; }
        }

        /// <summary>
        /// Gets or sets downward-aligned tooltip prefab.
        /// </summary>
        public GameObject DownPrefab
        {
            get { return m_DownPrefab; }
            set { m_DownPrefab = value; }
        }

        #endregion

        /// <summary>
        /// Gets the controller tooltip prefab for the direction specified by <paramref name="direction"/>.
        /// </summary>
        /// <param name="direction">The direction of the tip prefab to retrieve.</param>
        /// <returns>The tooltip prefab for the specified direction.</returns>
        public GameObject GetPrefab(VRControllerTipAlignment direction)
        {
            switch (direction)
            {
                case VRControllerTipAlignment.Left:
                    return m_LeftPrefab;

                case VRControllerTipAlignment.Right:
                    return m_RightPrefab;

                case VRControllerTipAlignment.Forward:
                    return m_ForwardPrefab;

                case VRControllerTipAlignment.Backward:
                    return m_BackwardPrefab;

                case VRControllerTipAlignment.Up:
                    return m_UpPrefab;

                case VRControllerTipAlignment.Down:
                    return m_DownPrefab;

                default:
                    return null;
            }
        }
    }
}
