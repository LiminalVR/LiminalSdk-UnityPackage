using System;
using UnityEngine;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A concrete implementation of <see cref="BaseReticleVisual"/> the manages a standard pointer reticle.
    /// <br/>Most of this was borrowed from <see cref="GvrControllerReticleVisual"/>. This implementation cleans up the code a little and fixes some rendering issues when multiple cameras are active.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ReticleVisual : BaseReticleVisual
    {
        [Serializable]
        public struct FaceCameraSettings
        {
            public bool AlongXAxis;
            public bool AlongYAxis;
            public bool AlongZAxis;

            /// <summary>
            /// Indicates if any axes are set to false.
            /// </summary>
            public bool IsAnyAxisOff
            {
                get { return !AlongXAxis || !AlongYAxis || !AlongZAxis; }
            }

            public FaceCameraSettings(bool enableAll)
            {
                AlongXAxis = enableAll;
                AlongYAxis = enableAll;
                AlongZAxis = enableAll;
            }
        }
        
        [Tooltip("Determines if the size of the reticle is based on the distance from the camera.")]
        [SerializeField] private bool m_SizeBasedOnCameraDistance = true;
        [Tooltip("Final size of the reticle in meters when it is 1 meter from the camera.")]
        [SerializeField] private float m_SizeMeters = 0.1f;
        [SerializeField] private float m_InvalidScale = 0.25f;
        [Tooltip("Determines how the reticle faces the camera.")]
        [SerializeField] private FaceCameraSettings m_FaceCamera = new FaceCameraSettings(true);
        [Tooltip("The sorting order for the reticle.")]
        [SerializeField, Range(-32767, 32767)] private int m_SortingOrder = 0;

        #region Properties

        /// <summary>
        /// The size of the reticle's mesh, in meters.
        /// </summary>
        public float MeshSizeMeters
        {
            get; private set;
        }

        /// <summary>
        /// The ratio of the reticleMeshSizeMeters to 1 meter.
        /// If reticleMeshSizeMeters is 10, then reticleMeshSizeRatio is 0.1.
        /// </summary>
        public float MeshSizeRatio
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets the sorting order for the mesh.
        /// </summary>
        public int SortingOrder
        {
            get { return m_SortingOrder; }
            set { m_SortingOrder = Mathf.Clamp(value, -32767, 32767); }
        }

        /// <summary>
        /// Determines if the size of the reticle is based on the distance from the camera.
        /// </summary>
        public bool SizeBasedOnCameraDistance
        {
            get { return m_SizeBasedOnCameraDistance; }
            set { m_SizeBasedOnCameraDistance = value; }
        }

        /// <summary>
        /// Final size of the reticle in meters when it is 1 meter from the camera.
        /// </summary>
        public float SizeMeters
        {
            get { return m_SizeMeters; }
            set { m_SizeMeters = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the camera-facing settings for the reticle.
        /// </summary>
        public FaceCameraSettings FaceCamera
        {
            get { return m_FaceCamera; }
            set { m_FaceCamera = value; }
        }

        #endregion

        protected MeshRenderer mMeshRenderer;
        protected MeshFilter mMeshFilter;

        private Vector3 mPreRenderLocalScale;
        private Quaternion mPreRenderLocalRotation;

        #region MonoBehaviour

        protected virtual void Awake()
        {
            mMeshRenderer = GetComponent<MeshRenderer>();
            mMeshFilter = GetComponent<MeshFilter>();
        }

        protected virtual void OnEnable()
        {
            UpdateMesh();
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                UpdateMesh();
            }
        }

        protected virtual void OnWillRenderObject()
        {
            mPreRenderLocalScale = transform.localScale;
            mPreRenderLocalRotation = transform.localRotation;

            var camera = Camera.main;

            if (camera == null)
                return;

            if ((camera.cullingMask & (1 << gameObject.layer)) != 0)
            {
                UpdateSize(camera);
                UpdateOrientation(camera);
            }
        }

        protected virtual void OnRenderObject()
        {
            // It is possible for paired calls to OnWillRenderObject/OnRenderObject to be nested if
            // Camera.Render is explicitly called for any special effects. To avoid the reticle being
            // rotated/scaled incorrectly in that case, the reticle is reset to it's pre-OnWillRenderObject
            // after a render has finished.

            // FIX: This check ensures the transform isn't reset if the rendering camera won't actually
            // be rendering this object
            if ((Camera.current.cullingMask & (1 << gameObject.layer)) != 0)
            {
                transform.localScale = mPreRenderLocalScale;
                transform.localRotation = mPreRenderLocalRotation;
            }
        }
        
        #endregion

        protected void UpdateMesh()
        {
            MeshSizeMeters = 1.0f;
            MeshSizeRatio = 1.0f;

            if (mMeshFilter != null && mMeshFilter.mesh != null)
            {
                MeshSizeMeters = mMeshFilter.mesh.bounds.size.x;
                if (MeshSizeMeters != 0.0f)
                    MeshSizeRatio = 1.0f / MeshSizeMeters;
            }

            if (mMeshRenderer != null)
                mMeshRenderer.sortingOrder = m_SortingOrder;
        }

        protected virtual void UpdateSize(Camera camera)
        {
            if (camera == null)
                return;

            var scale = m_SizeMeters;

            // Adjust scale based on the current distance from the camera
            if (m_SizeBasedOnCameraDistance)
            {
                var reticleDistanceFromCamera = (transform.position - camera.transform.position).magnitude;
                scale *= MeshSizeRatio * reticleDistanceFromCamera;
            }
            
            // If the raycast is invalid, apply the invalid scale
            if (!CurrentRaycastResult.isValid)
                scale *= m_InvalidScale;

            transform.localScale = new Vector3(scale, scale, scale);
        }

        protected virtual void UpdateOrientation(Camera camera)
        {
            if (camera == null)
                return;

            var direction = transform.position - camera.transform.position;
            if (direction.sqrMagnitude <= 0)
                return;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            if (m_FaceCamera.IsAnyAxisOff)
            {
                var euler = transform.localEulerAngles;
                if (!m_FaceCamera.AlongXAxis)
                    euler.x = 0.0f;

                if (!m_FaceCamera.AlongYAxis)
                    euler.y = 0.0f;

                if (!m_FaceCamera.AlongZAxis)
                    euler.z = 0.0f;

                transform.localEulerAngles = euler;
            }
        }
    }
}
