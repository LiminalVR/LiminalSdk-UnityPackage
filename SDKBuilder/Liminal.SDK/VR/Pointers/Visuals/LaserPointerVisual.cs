using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A concrete implementation of <see cref="BasePointerVisual"/> that uses a <see cref="LineRenderer"/> to draw a 'laser' along the pointer direction.
    /// </summary>
    public class LaserPointerVisual : BasePointerVisual
    {
        private float mTargetDistance;
        private float mCurrentDistance;
        private Vector3[] mLinePoints = new Vector3[2];

        [Header("Components")]
        [Tooltip("The LineRenderer component for drawing the laser visual.")]
        [SerializeField] private LineRenderer m_LineRenderer = null;
        [Tooltip("The reticle visual component.")]
        [SerializeField] private BaseReticleVisual m_Reticle = null;
        [Header("Settings")]
        [Tooltip("The distance of the reticle from the origin when not hitting any objects.")]
        [SerializeField] private float m_DefaultReticleDistance = 20f;
        [Tooltip("The maximum distance of the laser beam.")]
        [SerializeField] private float m_MaxBeamDistance = 0.75f;
        [Tooltip("The desired time for the laser to grow when hitting an object further away than the current position.")]
        [SerializeField] private float m_GrowTime = 0.1f;
        [Tooltip("Indicates if the reticle is hidden when the pointer is not hitting any objects.")]
        [SerializeField] private bool m_HideReticleWhenInvalid = true;

        #region Properties
        
        /// <summary>
        /// Gets or sets the maximum distance of the beam.
        /// </summary>
        public float MaxDistance
        {
            get { return m_MaxBeamDistance; }
            set { m_MaxBeamDistance = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the default distance of the reticle when not hitting any objects.
        /// </summary>
        public float DefaultReticleDistance
        {
            get { return m_DefaultReticleDistance; }
            set { m_DefaultReticleDistance = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the desired time for the laser to grow when hitting an object further away than the current position.
        /// </summary>
        public float GrowTime
        {
            get { return m_GrowTime; }
            set { m_GrowTime = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Determines if the reticle is hidden when not hitting any objects.
        /// </summary>
        public bool HideReticleWhenInvalid
        {
            get { return m_HideReticleWhenInvalid; }
            set { m_HideReticleWhenInvalid = value; }
        }

        #endregion

        #region MonoBehaviour

        private void OnValidate()
        {
            m_GrowTime = Mathf.Max(0, m_GrowTime);
            m_DefaultReticleDistance = Mathf.Max(0, m_DefaultReticleDistance);
            m_MaxBeamDistance = Mathf.Max(0, m_MaxBeamDistance);
        }

        private void Awake()
        {
            m_LineRenderer.positionCount = 2;
        }

        private void OnEnable()
        {
            mCurrentDistance = 0;
            SetComponentsActiveState(mActive);

            Debug.Log($"Pointer {Pointer != null}");
            if (Pointer != null)
                UpdateBeam(Pointer.CurrentRaycastResult);
        }

        private void LateUpdate()
        {
            if (mActive)
            {
                if (Pointer == null)
                {
                    // No pointer bound
                    // We do not have sufficient information to display a laser/reticle, so
                    // disable all the renderering components.
                    SetComponentsActiveState(false);
                }
                else
                {
                    SetComponentsActiveState(true);
                    UpdateBeam(Pointer.CurrentRaycastResult);
                }
            }
        }

        #endregion

        /// <summary>
        /// Sets the active state of the pointer visual.
        /// </summary>
        /// <param name="activeState">The active state of the pointer visual.</param>
        public override void SetActive(bool activeState)
        {
            base.SetActive(activeState);
            SetComponentsActiveState(activeState);
        }

        private void SetComponentsActiveState(bool activeState)
        {
            if (m_LineRenderer != null)
                m_LineRenderer.enabled = activeState;

            if (m_Reticle != null)
                m_Reticle.gameObject.SetActive(activeState);
        }

        private void UpdateBeam(RaycastResult result)
        {
            var distance = result.distance;
            if (distance <= 0 || !result.isValid)
                distance = m_MaxBeamDistance;
            
            // Setup target distance for reticle, compute current distance
            // If the target distance is shorter than the current, we'll snap directly to it
            // Otherwise, ease toward the target distance
            mTargetDistance = Math.Min(distance, m_MaxBeamDistance);
            mCurrentDistance = (mTargetDistance > mCurrentDistance && m_GrowTime > 0f)
                ? Mathf.Lerp(mCurrentDistance, mTargetDistance, Time.unscaledDeltaTime * (1f / m_GrowTime))
                : mTargetDistance;
            
            var origin = transform.position;
            var target = origin + transform.forward * mCurrentDistance;
            mLinePoints[0] = origin;
            mLinePoints[1] = target;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.positionCount = mLinePoints.Length;
                m_LineRenderer.useWorldSpace = true;
                m_LineRenderer.SetPositions(mLinePoints);
            }

            if (m_Reticle != null)
            {
                m_Reticle.CurrentRaycastResult = result;
                m_Reticle.transform.position = result.isValid
                    ? origin + (transform.forward * result.distance)
                    : origin + (transform.forward * m_DefaultReticleDistance);

                m_Reticle.gameObject.SetActive(!m_HideReticleWhenInvalid || result.isValid);
            }
        }
    }
}
