using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.EventSystems
{
    /// <summary>
    /// A <see cref="PhysicsRaycaster"/> implementation that is compatible with the <see cref="VRPointerInputModule"/> input module.
    /// Add this component to the avatar to allow interaction on physical objects with VR devices.
    /// </summary>
    public class VRPhysicsRaycaster : BaseRaycaster
    {
        private class HitComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit lhs, RaycastHit rhs)
            {
                return lhs.distance.CompareTo(rhs.distance);
            }
        }

        private static HitComparer _hitComparer = new HitComparer();

        protected const int NoEventMask = -1;

        private RaycastHit[] mHitCache;
        private int mLastMaxRayIntersections = 0;

        [Tooltip("A mask indicating which layers are able to be hit by the raycaster.")]
        [SerializeField] protected LayerMask m_EventMask = NoEventMask;
        [Tooltip("The maximum number of raycast intersections that can be resolved with a single cast. Set this value to an appropriate number for the size of your scene..")]
        [SerializeField] protected int m_MaxRayIntersections = 64;

        #region Properties

        /// <summary>
        /// Get the camera that is used for this module.
        /// </summary>
        public override Camera eventCamera
        {
            get { return VRPointerInputModule.RaycastEventCamera ?? Camera.main; }
        }

        /// <summary>
        /// Get the depth of the configured camera.
        /// </summary>
        public int Depth
        {
            get { return (eventCamera != null) ? (int)eventCamera.depth : 0xFFFFFF; }
        }

        /// <summary>
        /// Logical and of Camera mask and eventMask.
        /// </summary>
        public int FinalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : NoEventMask; }
        }

        /// <summary>
        /// Mask of allowed raycast events.
        /// </summary>
        public LayerMask EventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }

        /// <summary>
        /// Max number of ray intersection allowed to be found.
        /// </summary>
        public int MaxRayIntersections
        {
            get { return m_MaxRayIntersections; }
            set { m_MaxRayIntersections = Math.Max(value, 0); }
        }

        #endregion

        private VRPhysicsRaycaster()
        {
            //
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var evCam = eventCamera;
            if (evCam == null || !evCam.pixelRect.Contains(eventData.position))
                return;

            Ray ray; float distanceToClipPlane;
            ComputeRayAndDistance(eventData, out ray, out distanceToClipPlane);

            int hits;
            if (m_MaxRayIntersections == 0)
            {
                mHitCache = Physics.RaycastAll(ray, distanceToClipPlane, FinalEventMask);
                hits = mHitCache.Length;
            }
            else
            {
                if (mLastMaxRayIntersections != m_MaxRayIntersections)
                {
                    mHitCache = new RaycastHit[m_MaxRayIntersections];
                    mLastMaxRayIntersections = m_MaxRayIntersections;
                }

                hits = Physics.RaycastNonAlloc(ray, mHitCache, distanceToClipPlane, FinalEventMask);
            }
            
            if (hits > 1)
            {

                if (hits == m_MaxRayIntersections)
                {
                    m_MaxRayIntersections *= 2;
                    Debug.LogWarningFormat(
                        "Raycast returned {0} hits, which is the current maximum. Some hits may have been lost. " +
                        "Setting MaxRaycastHits to {1}. Please set MaxRaycastHits to a sufficiently high value for your scene.",
                        hits, m_MaxRayIntersections);
                }

                Array.Sort(mHitCache, 0, hits, _hitComparer);
            }

            if (hits != 0)
            {
                for (int i = 0, bmax = hits; i < bmax; ++i)
                {
                    var hit = mHitCache[i];
                    resultAppendList.Add(new RaycastResult
                    {
                        gameObject = hit.collider.gameObject,
                        module = this,
                        distance = hit.distance,
                        worldPosition = hit.point,
                        worldNormal = hit.normal,
                        screenPosition = eventData.position,
                        index = resultAppendList.Count,
                        sortingLayer = 0,
                        sortingOrder = 0
                    });
                }
            }
        }

        private void ComputeRayAndDistance(PointerEventData eventData, out Ray ray, out float distanceToClipPlane)
        {
            var evCam = eventCamera;
            ray = evCam.ScreenPointToRay(eventData.position);

            var projectionDirection = ray.direction.z;
            distanceToClipPlane = Mathf.Approximately(0f, projectionDirection)
                ? Mathf.Infinity
                : Mathf.Abs((evCam.farClipPlane - evCam.nearClipPlane) / projectionDirection);
        }
    }
}
