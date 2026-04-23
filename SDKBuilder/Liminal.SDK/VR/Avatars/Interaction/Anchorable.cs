using Liminal.SDK.VR.Avatars.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// A component for allowing objects to be anchorable to <see cref="IVRAvatar"/> limbs.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Interaction/Anchorable Object")]
    public class Anchorable : MonoBehaviour, IAnchorableAnchored, IAnchorableUnanchored
    {
        /// <summary>
        /// A collection of events for <see cref="Anchorable"/> objects.
        /// </summary>
        [Serializable]
        public class AnchorableEvents
        {
            [Tooltip("Raised when an object is anchored to the limb.")]
            [SerializeField] private AnchorableEvent m_OnAnchored = new AnchorableEvent();
            [Tooltip("Raised when an object is unanchored from the limb.")]
            [SerializeField] private AnchorableEvent m_OnUnanchored = new AnchorableEvent();

            /// <summary>
            /// Raise when the object is anchored to the limb.
            /// </summary>
            public AnchorableEvent OnAnchored
            {
                get { return m_OnAnchored; }
            }

            /// <summary>
            /// Raised when the object is unanchored from the limb.
            /// </summary>
            public AnchorableEvent OnUnanchored
            {
                get { return m_OnUnanchored; }
            }
        }

        private readonly List<IAnchorHandler> mHandlers = new List<IAnchorHandler>();
        private IVRAvatarLimb mLimb;
        private Transform mOriginalParent;
        private bool mWasKinematic;

        [Tooltip("Determines if the object is reparented to the limb when anchored.")]
        [SerializeField] private bool m_ReparentToAnchor = true;
        [Tooltip("Determines if the VRAvatarController attached to the limb should be hidden.")]
        [SerializeField] private bool m_HideController = false;
        [Tooltip("Determines if other anchored objects on the limb are unanchored when this object is anchored.")]
        [SerializeField] private bool m_UnanchorOthers = false;
        [Tooltip("A pivot transform to mount at the limb anchor point when anchored. Use this to specify a relative position for the object when anchored.")]
        [SerializeField] private Transform m_AnchorPivot = null;
        [Tooltip("The events available to the anchorable object.")]
        [SerializeField] private AnchorableEvents m_Events = new AnchorableEvents();

        #region Properties

        /// <summary>
        /// Gets the <see cref="IVRAvatarLimb"/> the object is currently attached to.
        /// </summary>
        public IVRAvatarLimb AttachedLimb
        {
            get { return mLimb; }
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatar"/> the object is currently attached to. This is a shortcut to <see cref="IVRAvatarLimb.Avatar"/>.
        /// </summary>
        public IVRAvatar AttachedAvatar
        {
            get { return mLimb != null ? mLimb.Avatar : null; }
        }

        /// <summary>
        /// Indicates if the object is currently anchored to any limbs.
        /// </summary>
        public bool IsAnchored
        {
            get { return mLimb != null; }
        }

        /// <summary>
        /// Gets or sets the anchor pivot for the object. This transform will be matched to the anchor transform on the limb when the object is anchored.
        /// </summary>
        public Transform AnchorPivot
        {
            get { return m_AnchorPivot; }
            set { m_AnchorPivot = value; }
        }

        /// <summary>
        /// Determines if the object is parented to the limb anchor when anchored.
        /// </summary>
        public bool ReparentToAnchor
        {
            get { return m_ReparentToAnchor; }
            set { m_ReparentToAnchor = value; }
        }

        /// <summary>
        /// Indicates if the <see cref="VRAvatarController"/> attached to the limb will be deactivated when this object becomes anchored.
        /// </summary>
        public bool HideController
        {
            get { return m_HideController; }
            set { m_HideController = value; }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            GetComponentsInChildren(true, mHandlers);
        }

        private void OnDestroy()
        {
            if (m_Events.OnAnchored != null)
                m_Events.OnAnchored.RemoveAllListeners();

            if (m_Events.OnUnanchored != null)
                m_Events.OnUnanchored.RemoveAllListeners();
        }

        private void LateUpdate()
        {
            if (mLimb != null)
            {
                var currentPos = transform.position;
                var currentRot = transform.rotation;

                var targetPos = ComputeTargetPosition();
                var targetRot = ComputeTargetRotation();

                if (mHandlers.Count > 0)
                {
                    for (int i = 0; i < mHandlers.Count; ++i)
                    {
                        var handler = mHandlers[i];
                        if (handler == null || !handler.enabled)
                            continue;

                        handler.ModifyPosition(ref currentPos, ref targetPos);
                        handler.ModifyRotation(ref currentRot, ref targetRot);
                    }
                }
                else
                {
                    currentPos = targetPos;
                    currentRot = targetRot;
                }

                transform.SetPositionAndRotation(currentPos, currentRot);
            }
        }

        #endregion

        /// <summary>
        /// [Internal use] Called by the VRAvatar system when this object is anchored to a limb. You should not need to call this manually.
        /// </summary>
        /// <param name="limb">The limb the object was anchored to.</param>
        public void OnAnchored(IVRAvatarLimb limb)
        {
            if (mLimb == limb)
                return;

            // Store original parent so that it can be restored when unanchored
            // Note that this is only stored if the current limb is null - this avoids a potential situation
            // where the original parent could be another limb that the object was previous anchored to
            if (mLimb == null)
                mOriginalParent = transform.parent;

            mLimb = limb;
            
            // Reparent to anchor transform
            if (m_ReparentToAnchor)
                transform.parent = mLimb.Anchor;

            if (m_UnanchorOthers)
            {
                for (int i = mLimb.AttachedObjects.Count - 1; i >= 0; --i)
                {
                    var obj = mLimb.AttachedObjects[i];
                    if (obj != gameObject)
                    {
                        mLimb.Unattach(obj, null);
                    }
                }
            }

            if (m_HideController)
            {
                var controller = mLimb.Transform.GetComponentInChildren<VRAvatarController>(includeInactive: true);
                if (controller != null)
                    controller.gameObject.SetActive(false);
            }

            // Make the object kinematic
            var rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                mWasKinematic = rigidBody.isKinematic;
                rigidBody.isKinematic = true;
            }

            // Raise event
            if (m_Events.OnAnchored != null)
                m_Events.OnAnchored.Invoke(this);
        }

        /// <summary>
        /// [Internal use] Called by the VRAvatar system when this object is unanchored from a limb. You should not need to call this manually.
        /// </summary>
        /// <param name="limb">The limb the object was unanchored from.</param>
        public void OnUnanchored(IVRAvatarLimb limb)
        {
            if (mLimb == null || mLimb != limb)
                return;

            if (m_HideController)
            {
                var controller = mLimb.Transform.GetComponentInChildren<VRAvatarController>(includeInactive: true);
                if (controller != null)
                    controller.gameObject.SetActive(true);
            }

            // If parented to this limb, return to the original parent
            if (transform.parent == mLimb.Anchor)
                transform.parent = mOriginalParent;

            mLimb = null;

            
            var rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                rigidBody.isKinematic = mWasKinematic;
            }

            // Raise event
            if (m_Events.OnUnanchored != null)
                m_Events.OnUnanchored.Invoke(this);
        }

        private Vector3 ComputeTargetPosition()
        {
            var pos = mLimb.Anchor.position;

            // Append world-space relative position of anchor pivot
            if (m_AnchorPivot != null)
                pos += (transform.position - m_AnchorPivot.position);

            return pos;
        }

        private Quaternion ComputeTargetRotation()
        {
            var rot = mLimb.Anchor.rotation;

            // Append world-space relative rotation of anchor pivot
            if (m_AnchorPivot != null)
                rot *= Quaternion.Inverse(m_AnchorPivot.rotation) * transform.rotation;

            return rot;
        }
    }
}
