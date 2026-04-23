using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// An abstract base implementation of <see cref="IVRPointer"/>.
    /// </summary>
    public abstract class BasePointer : IVRPointer
    {
        private Transform mTransform;
        private IVRDeviceComponent mDeviceComponent;
        private RaycastResult mCurrentRaycastResult;

        #region Properties

        public bool IsActive { get; private set; }

        public IVRDeviceComponent DeviceComponent { get { return mDeviceComponent; } }

        public RaycastResult CurrentRaycastResult
        {
            get { return mCurrentRaycastResult; }
            set { mCurrentRaycastResult = value; }
        }

        public Transform Transform
        {
            get { return mTransform; }
            set { mTransform = value; }
        }

        #endregion

        #region Events

        public event PointerActiveStateChanged ActiveStateChanged;

        #endregion

        public BasePointer(IVRDeviceComponent deviceComponent)
        {
            mDeviceComponent = deviceComponent;
            EventSystems.VRPointerInputModule.AddPointer(this);
        }
        
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;

                if (ActiveStateChanged != null)
                    ActiveStateChanged(this);
            }
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;

                if (ActiveStateChanged != null)
                    ActiveStateChanged(this);
            }
        }

        public abstract void OnPointerEnter(GameObject target);
        public abstract void OnPointerExit(GameObject target);
        public abstract bool GetButtonDown();
        public abstract bool GetButtonUp();
    }
}
