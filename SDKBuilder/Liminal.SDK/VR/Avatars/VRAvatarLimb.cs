using Liminal.SDK.Collections;
using Liminal.SDK.VR.Avatars.Events;
using Liminal.SDK.VR.Avatars.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An abstract base class representing an avatar limb.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class VRAvatarLimb : MonoBehaviour, IVRAvatarLimb
    {
        /// <summary>
        /// A collection of events relating to avatar limbs.
        /// </summary>
        [Serializable]
        public class LimbEvents
        {
            [Tooltip("Raised when an object is anchored to the limb.")]
            [SerializeField] private VRLimbAttachmentEvent m_OnAnchored = new VRLimbAttachmentEvent();
            [Tooltip("Raised when an object is unanchored from the limb.")]
            [SerializeField] private VRLimbAttachmentEvent m_OnUnanchored = new VRLimbAttachmentEvent();

            /// <summary>
            /// Raised when an object is anchored to the limb.
            /// </summary>
            public VRLimbAttachmentEvent OnAnchored
            {
                get { return m_OnAnchored; }
            }

            /// <summary>
            /// Raised when an object unanchored from the limb.
            /// </summary>
            public VRLimbAttachmentEvent OnUnanchored
            {
                get { return m_OnUnanchored; }
            }
        }

        /// <summary>
        /// Settings for managing the tracked state of the limb.
        /// </summary>
        [Serializable]
        public class TrackedObjectSettings
        {
            [Tooltip("Determines if the limb's active state is modified when the tracked object's active state changes.")]
            public bool MatchActive = true;
            [Tooltip("Determines if the limb's position should match the tracked object's position.")]
            public bool MatchPosition = true;
            [Tooltip("Determines if the limb's rotation should match the tracked object's rotation.")]
            public bool MatchRotation = true;
        }

        private IVRAvatar mAavtar;
        private IVRTrackedObjectProxy mTrackedObject;

        [Header("Limb")]
        [Tooltip("The type of this limb.")]
        [SerializeField] private VRAvatarLimbType m_Type = VRAvatarLimbType.None;
        [Tooltip("The anchor point of the limb, where attachments are parented.")]
        [SerializeField] private Transform m_Anchor = null;
        [Tooltip("The list of objects currently attached to the limb anchor. Objects placed in this list at design time will automatically be anchored when the application runs.")]
        [SerializeField] private List<GameObject> m_AnchorAttachments = null;
        [Tooltip("Tracking settings for the limb.")]
        [SerializeField] private TrackedObjectSettings m_Tracking = null;
        [SerializeField] private LimbEvents m_Events = null;

        #region MonoBehaviour

        /// <summary>
        /// Gets the <see cref="IVRAvatar"/> the limb is attached to.
        /// </summary>
        public IVRAvatar Avatar
        {
            get
            {
                if (mAavtar == null)
                    mAavtar = GetComponent<IVRAvatar>();

                return mAavtar;
            }
        }
        
        /// <summary>
        /// Gets the <see cref="IVRDeviceComponent"/> the limb is assigned to.
        /// </summary>
        public abstract IVRDeviceComponent DeviceComponent
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="VRAvatarLimbType"/> of this limb.
        /// </summary>
        public VRAvatarLimbType LimbType
        {
            get { return m_Type; }
        }

        /// <summary>
        /// Gets the tracked object assigned to the limb.
        /// </summary>
        public IVRTrackedObjectProxy TrackedObject
        {
            get { return mTrackedObject; }
            set { mTrackedObject = value; }
        }

        /// <summary>
        /// Gets the anchor transform for the limb.
        /// </summary>
        public Transform Anchor
        {
            get { return (m_Anchor == null) ? transform : m_Anchor; }
        }

        /// <summary>
        /// Gets the list of GameObjects currently attached to the limb anchor.
        /// </summary>
        public List<GameObject> AttachedObjects
        {
            get { return m_AnchorAttachments; }
        }

        /// <summary>
        /// Gets the limb events.
        /// </summary>
        public LimbEvents Events
        {
            get { return m_Events; }
        }

        /// <summary>
        /// Indicates if the limb is currently active.
        /// </summary>
        public bool IsActive
        {
            get { return gameObject.activeSelf; }
        }

        /// <summary>
        /// Gets the transform for this limb.
        /// </summary>
        public Transform Transform
        {
            get
            {
                return transform;
            }
        }

        /// <summary>
        /// Gets or sets the tracking settings for the limb.
        /// </summary>
        public TrackedObjectSettings TrackingSettings
        {
            get { return m_Tracking; }
            set { m_Tracking = value; }
        }

        #endregion

        #region Events

        #endregion

        #region MonoBehaviour

        protected virtual void Awake()
        {
            mAavtar = GetComponentInParent<IVRAvatar>();

            foreach (var attachment in m_AnchorAttachments)
            {
                if (attachment != null)
                {
                    InternalAnchorObject(attachment, AnchorAttachFlags.Default | AnchorAttachFlags.IgnoreAnchorHandlers);
                }
            }
        }
        
        protected virtual void OnTransformParentChanged()
        {
            mAavtar = GetComponentInParent<IVRAvatar>();
        }

        protected virtual void OnDestroy()
        {
            m_Events.OnAnchored.RemoveAllListeners();
            m_Events.OnUnanchored.RemoveAllListeners();
        }
        
        protected virtual void LateUpdate()
        {
            MatchTrackedObjectTransform();
        }

        #endregion
        
        /// <summary>
        /// [Internal use only] Updates the internal state of the limb. You should not call this from your own code.
        /// </summary>
        public void UpdateState()
        {
            if ((mTrackedObject != null) && m_Tracking.MatchActive)
            {
                gameObject.SetActive(mTrackedObject.IsActive);
            }
        }

        /// <summary>
        /// Sets the active state for the limb.
        /// </summary>
        /// <param name="activeState">The active state of the limb.</param>
        public void SetActive(bool activeState)
        {
            gameObject.SetActive(activeState);
        }

        /// <summary>
        /// Attaches a <see cref="GameObject"/> to the limb anchor.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to attach to the limb anchor.</param>
        /// <param name="flags">Options for attaching the object to the limb anchor.</param>
        public void Attach(GameObject gameObject, AnchorAttachFlags flags = AnchorAttachFlags.Default)
        {
            if (!m_AnchorAttachments.Contains(gameObject))
            {
                m_AnchorAttachments.Add(gameObject);
                InternalAnchorObject(gameObject, flags);
            }
        }

        /// <summary>
        /// Unattaches a <see cref="GameObject"/> from the limb anchor and attaches to the specified parent. If no parent transform is supplied,
        /// the object will be reparented to the scene root.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to unattach.</param>
        /// <param name="newParent">The <see cref="Transform"/> to reparent the object to. Use null to reparent to the scene root.</param>
        /// <returns>A boolean indicating if the object was successfully unattached.</returns>
        public bool Unattach(GameObject gameObject, Transform newParent = null)
        {
            if (gameObject == null)
                return false;

            if (!m_AnchorAttachments.Remove(gameObject))
                return false;

            gameObject.transform.parent = newParent;

            // Notify IUnanchored components that they have been unanchored
            NotifyUnanchored(gameObject);

            if (m_Events.OnUnanchored != null)
                m_Events.OnUnanchored.Invoke(this, gameObject);

            return true;
        }
        
        /// <summary>
        /// Unattaches all GameObjects from the limb and reparents them to <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent">The transform to parent all current attachments to.</param>
        public void UnattachAll(Transform newParent = null)
        {
            for (int i = m_AnchorAttachments.Count - 1; i >= 0; --i)
            {
                var obj = m_AnchorAttachments[i];
                if (obj != null)
                {
                    Unattach(obj, newParent);
                }
            }
        }

        private void MatchTrackedObjectTransform()
        {
            if (mTrackedObject == null)
                return;

            if (m_Tracking.MatchPosition)
                transform.position = mTrackedObject.Position;
            
            if (m_Tracking.MatchRotation)
                transform.rotation = mTrackedObject.Rotation;
        }

        private void InternalAnchorObject(GameObject gameObject, AnchorAttachFlags flags)
        {
            var anchorTransform = Anchor;
            var anchorable = gameObject.GetComponent<Anchorable>();
            if (anchorable == null)
            {
                if ((flags & AnchorAttachFlags.ReparentToAnchor) != 0)
                    gameObject.transform.parent = anchorTransform;
            }

            if ((flags & AnchorAttachFlags.IgnoreAnchorHandlers) != 0 || gameObject.GetComponent<IAnchorHandler>() == null)
            {
                // Anchor handlers are ignored, or no handler exists on the object
                // Set the position/rotation to match the anchor immediately
                gameObject.transform.SetPositionAndRotation(anchorTransform.position, anchorTransform.rotation);
            }

            // Notify IAnchored components that they have been anchored
            NotifyAnchored(gameObject);

            // Raise anchor event
            if (m_Events.OnAnchored != null)
                m_Events.OnAnchored.Invoke(this, gameObject);
        }
        
        private void NotifyAnchored(GameObject gameObject)
        {
            using (var pList = new PooledList<IAnchorableAnchored>())
            {
                var list = pList.List;
                gameObject.GetComponentsInChildren(list);
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i].OnAnchored(this);
                }
            }
        }
        
        private void NotifyUnanchored(GameObject gameObject)
        {
            using (var pList = new PooledList<IAnchorableUnanchored>())
            {
                var list = pList.List;
                gameObject.GetComponentsInChildren(list);
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i].OnUnanchored(this);
                }
            }
        }
    }
}
