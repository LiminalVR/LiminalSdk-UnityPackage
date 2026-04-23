using Liminal.SDK.VR.Avatars.Events;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// Smoothly transitions an object to an avatar anchor.
    /// </summary>
    [RequireComponent(typeof(Anchorable))]
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Interaction/Smooth Anchor")]
    public class SmoothAnchor : MonoBehaviour, IAnchorHandler, IAnchorableAnchored
    {
        #region Contants

        private const float MinSmoothing = 0f;
        private const float MaxSmoothing = 0.9999f;

        /// <summary>
        /// The default travel time for smooth anchored objects.
        /// </summary>
        public const float DefaultTravelTime = 0.05f;

        /// <summary>
        /// The default position smooething time for smooth anchored objects.
        /// </summary>
        public const float DefaultPositionSmoothing = 0.4f;

        /// <summary>
        /// The default rotation smooething time for smooth anchored objects.
        /// </summary>
        public const float DefaultRotationSmoothing = 0.3f;

        #endregion

        private Anchorable mAnchorable;
        private bool mPositionComplete;
        private bool mRotationComplete;
        private float mInvTravelTime;
        
        [Tooltip("The desired time for the object to travel to the anchor point once anchored.")]
        [SerializeField] private float m_TravelTime = DefaultTravelTime;
        [Header("Smoothing")]
        [Tooltip("The smoothing factor applied to the object's position to eliminate jitter. A higher value applies more damping to the position.")]
        [SerializeField, Range(MinSmoothing, MaxSmoothing)] private float m_PositionSmoothing = DefaultPositionSmoothing;
        [Tooltip("The smoothing factor applied to the object's rotation to eliminate jitter. A higher value applies more damping to the rotation.")]
        [SerializeField, Range(MinSmoothing, MaxSmoothing)] private float m_RotationSmoothing = DefaultRotationSmoothing;

        #region Properties
        
        /// <summary>
        /// Gets or sets the desired travel time for the object when attaching to the anchor.
        /// </summary>
        public float TravelTime
        {
            get { return m_TravelTime; }
            set
            {
                m_TravelTime = Mathf.Max(value, 0);
                UpdateCachedValues();
            }
        }

        /// <summary>
        /// Gets or sets the smoothing factor applied to the object's position to eliminate jitter. A higher value applies more damping to the position.
        /// </summary>
        public float PositionSmoothing
        {
            get { return m_PositionSmoothing; }
            set { m_PositionSmoothing = Mathf.Clamp(value, MinSmoothing, MaxSmoothing); }
        }

        /// <summary>
        /// Gets or sets the smoothing factor applied to the object's rotation to eliminate jitter. A higher value applies more damping to the rotation.
        /// </summary>
        public float RotationSmoothing
        {
            get { return m_RotationSmoothing; }
            set { m_RotationSmoothing = Mathf.Clamp(value, MinSmoothing, MaxSmoothing); }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            mAnchorable = GetComponent<Anchorable>();
            mAnchorable.ReparentToAnchor = false;

            UpdateCachedValues();
        }

        private void OnValidate()
        {
            m_TravelTime = Mathf.Max(m_TravelTime, 0);
            m_PositionSmoothing = Mathf.Clamp(m_PositionSmoothing, MinSmoothing, MaxSmoothing);
            m_RotationSmoothing = Mathf.Clamp(m_RotationSmoothing, MinSmoothing, MaxSmoothing);

            mAnchorable = GetComponent<Anchorable>();
            if (mAnchorable != null)
                mAnchorable.ReparentToAnchor = false;

            UpdateCachedValues();
        }

        #endregion

        /// <summary>
        /// Applies modifications to the position of the anchored object.
        /// </summary>
        /// <param name="current">The current position.</param>
        /// <param name="target">The target position.</param>
        public void ModifyPosition(ref Vector3 current, ref Vector3 target)
        {
            var smoothTarget = target;
            if (!mPositionComplete)
            {
                smoothTarget = (m_PositionSmoothing > 0)
                    ? Vector3.Lerp(current, target, Time.smoothDeltaTime * mInvTravelTime)
                    : target;

                const float minDist = 0.01f;
                mPositionComplete = ((smoothTarget - target).sqrMagnitude < (minDist * minDist));
            }

            // Apply low-pass filter to remove jitter
            current = (current * m_PositionSmoothing) + (smoothTarget * (1f - m_PositionSmoothing));
        }

        /// <summary>
        /// Applies modifications to the rotation of the anchored object.
        /// </summary>
        /// <param name="current">The current rotation.</param>
        /// <param name="target">The target rotation.</param>
        public void ModifyRotation(ref Quaternion current, ref Quaternion target)
        {
            var smoothTarget = target;
            if (!mRotationComplete)
            {
                smoothTarget = (mInvTravelTime > 0)
                    ? Quaternion.Slerp(current, target, Time.smoothDeltaTime * mInvTravelTime)
                    : target;

                const float minAngle = 0.2f;
                mRotationComplete = (Quaternion.Angle(smoothTarget, target) < minAngle);
            }

            // Apply low-pass filter to remove jitter
            current = Quaternion.Slerp(current, smoothTarget, 1f - m_RotationSmoothing);
        }

        private void UpdateCachedValues()
        {
            mInvTravelTime = (m_TravelTime > 0) ? 1f / m_TravelTime : 0f;
        }

        #region Event Handlers

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void OnAnchored(IVRAvatarLimb limb)
        {
            mPositionComplete = false;
            mRotationComplete = false;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion
    }
}
