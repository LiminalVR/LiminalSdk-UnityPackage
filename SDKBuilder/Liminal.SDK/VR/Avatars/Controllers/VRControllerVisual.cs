using Liminal.SDK.VR.Pointers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// Represents a VR controller visual object that can be bound to a <see cref="VRAvatarController"/>.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class VRControllerVisual : MonoBehaviour, IVRControllerVisual, ISerializationCallbackReceiver
    {
        private IVRPointerVisual mPointerVisual;
        private Dictionary<string, VRControllerNode> mNodesByName;
        private Dictionary<string, VRControllerInputVisual> mInputVisualsByName;
        [Header("Pointer")]
        [Tooltip("The pointer visual for the controller.")]
        [SerializeField] private BasePointerVisual m_PointerVisual = null;

        // Internal/hidden values
        [SerializeField, HideInInspector] private List<VRControllerNode> m_NodeList = null;
        [SerializeField, HideInInspector] private List<string> m_NodeNames = null;
        [SerializeField, HideInInspector] private List<VRControllerInputVisual> m_InputVisualList = null;
        [SerializeField, HideInInspector] private List<string> m_InputVisualNames = null;

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="IVRPointerVisual"/> for the controller.
        /// </summary>
        public IVRPointerVisual PointerVisual
        {
            get
            {
                return mPointerVisual;
            }

            set
            {
                mPointerVisual = value;
                m_PointerVisual = mPointerVisual as BasePointerVisual;
            }
        }

        public IEnumerable<VRControllerNode> Nodes { get { return m_NodeList; } }

        public IEnumerable<VRControllerInputVisual> Inputs { get { return m_InputVisualList; } }

        #endregion


        #region MonoBehaviour

        protected virtual void Awake()
        {
            mPointerVisual = m_PointerVisual;
        }

        private void OnEnable()
        {
            if (mPointerVisual != null)
            {
                var limb = GetComponentInParent<IVRAvatarLimb>();

                if (limb != null)
                {
                    IVRDeviceComponent deviceComponent = limb.DeviceComponent;
                    if (deviceComponent != null)
                    {
                        var ptr = deviceComponent.Pointer;
                        ptr.Transform = m_PointerVisual.transform;
                        m_PointerVisual.Bind(ptr);
                    }
                    else
                    {
                        // This can happen if no controller was connected during startup, or due to order of initialisation
                        Debug.Log("No limb device component found");
                    }
                }
            }
        }

        #endregion
        
        public VRControllerNode GetNode(string nodeName)
        {
            VRControllerNode node;
            mNodesByName.TryGetValue(nodeName, out node);
            return node;
        }

        public VRControllerInputVisual GetInput(string inputName)
        {
            VRControllerInputVisual visual;
            mInputVisualsByName.TryGetValue(inputName, out visual);
            return visual;
        }

        #region Serialization
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void OnBeforeSerialize()
        {
            // Node components
            m_NodeList = m_NodeList ?? new List<VRControllerNode>();
            m_NodeList.Clear();
            GetComponentsInChildren(true, m_NodeList);
            m_NodeNames = m_NodeList.Select(x => x.NodeName).ToList();

            // Input visuals
            m_InputVisualList = m_InputVisualList ?? new List<VRControllerInputVisual>();
            m_InputVisualList.Clear();
            GetComponentsInChildren(true, m_InputVisualList);
            m_InputVisualNames = m_InputVisualList.Select(x => x.InputName).ToList();
        }

        public void OnAfterDeserialize()
        {
            // Build node lookup
            mNodesByName = new Dictionary<string, VRControllerNode>();
            if (m_NodeList != null)
            {
                for (int i = 0; i < m_NodeList.Count; ++i)
                {
                    var node = m_NodeList[i];
                    if (node == null)
                        continue;

                    var name = m_NodeNames[i];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    mNodesByName[name] = node;
                }
            }
            
            // Build input visual lookup
            mInputVisualsByName = new Dictionary<string, VRControllerInputVisual>();
            if (m_InputVisualList != null)
            {
                for (int i = 0; i < m_InputVisualList.Count; ++i)
                {
                    var visual = m_InputVisualList[i];
                    if (visual == null)
                        continue;
                    
                    var name = m_InputVisualNames[i];
                    if (string.IsNullOrEmpty(name))
                        continue;
                    
                    mInputVisualsByName[name] = visual;
                }
            }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        #endregion
    }
}
