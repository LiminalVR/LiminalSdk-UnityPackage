using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A generic gaze pointer visual that supports base implementations of <see cref="BasePointer"/>, and also <see cref="TimedGazePointer"/>.
    /// </summary>
    public class GazePointerVisual : BasePointerVisual
    {
        [Header("Components")]
        [Tooltip("The reticle visual for the pointer.")]
        [SerializeField] private BaseReticleVisual m_Reticle = null;
        [Header("Settings")]
        [Tooltip("The constant distance of the reticle from the camera.")]
        [SerializeField] private float m_ReticleDistance = 1f;
        [Tooltip("Indicates if the reticle is hidden when the pointer is not hitting any objects.")]
        [SerializeField] private bool m_HideReticleWhenInvalid = false;

        #region MonoBehaviour

        private void OnValidate()
        {
            m_ReticleDistance = Mathf.Max(m_ReticleDistance, 0);
        }

        private void Awake()
        {
            //
        }

        private void LateUpdate()
        {
            if (mActive)
            {
                if (Pointer == null)
                {
                    // No pointer bound
                    // We do not have sufficient information to display a reticle, so
                    // disable all the renderering components.
                    SetComponentsActiveState(false);
                }
                else
                {
                    SetComponentsActiveState(true);
                    UpdateReticle(Pointer.CurrentRaycastResult);
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
            if (m_Reticle != null)
                m_Reticle.gameObject.SetActive(activeState);
        }
        
        private void UpdateReticle(RaycastResult result)
        {
            if (m_Reticle == null)
                return;
            
            m_Reticle.CurrentRaycastResult = result;
            m_Reticle.transform.localPosition = new Vector3(0, 0, m_ReticleDistance);
            m_Reticle.gameObject.SetActive(!m_HideReticleWhenInvalid || result.isValid);
        }
    }
}
