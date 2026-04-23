using System;
using UnityEngine;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// An abstract base implementation of <see cref="IVRPointerVisual"/>.
    /// </summary>
    public abstract class BasePointerVisual : MonoBehaviour, IVRPointerVisual
    {
        protected bool mActive;
        private IVRPointer mPointer;

        #region Properties

        /// <summary>
        /// Gets the <see cref="IVRPointer"/> the visual is currently bound to.
        /// </summary>
        public IVRPointer Pointer
        {
            get { return mPointer; }
        }

        #endregion

        #region MonoBehaviour

        private void OnDestroy()
        {
            if (mPointer != null)
            {
                if (mPointer.Transform == transform)
                    mPointer.Transform = null;

                mPointer.ActiveStateChanged -= OnPointerActiveStateChanged;
                
                // Wrapping try/catch here so if there are fatal errors in custom implementations, the app won't crash
                try
                {
                    OnUnbinding(mPointer);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                mPointer = null;
            }

            OnDestroyed();
        }

        #endregion

        /// <summary>
        /// Binds the pointer visual to an <see cref="IVRPointer"/> instance.
        /// </summary>
        /// <param name="pointer">The <see cref="IVRPointer"/> to bind the visual to.</param>
        public void Bind(IVRPointer pointer)
        {
            if (pointer != mPointer)
            {
                Unbind();
                mPointer = pointer;

                if (mPointer == null)
                {
                    SetActive(false);
                }
                else
                {
                    mPointer.ActiveStateChanged -= OnPointerActiveStateChanged;
                    mPointer.ActiveStateChanged += OnPointerActiveStateChanged;
                    OnBinding(mPointer);
                }
            }

            if (mPointer != null)
            {
                SetActive(mPointer.IsActive);
            }
        }

        /// <summary>
        /// Unbinds the pointer visual from the current <see cref="IVRPointer"/> instance.
        /// </summary>
        public void Unbind()
        {
            if (mPointer == null)
                return;

            if (mPointer.Transform == transform)
                mPointer.Transform = null;

            mPointer.ActiveStateChanged -= OnPointerActiveStateChanged;
            
            OnUnbinding(mPointer);
            mPointer = null;
            SetActive(false);
        }

        /// <summary>
        /// Sets the active state of the pointer visual.
        /// </summary>
        /// <param name="activeState">The active state of the pointer visual.</param>
        public virtual void SetActive(bool activeState)
        {
            mActive = activeState;

            // https://developer.cloud.unity3d.com/perfreporting/orgs/16767581212782/projects/734fea03-bc96-4627-9715-d7d017ca77f7/problems/3b3976376bab1663acd511f0e83ccc26/
            if (gameObject != null)
            {
                gameObject.SetActive(mActive);
            }
        }

        protected virtual void OnDestroyed() { }

        #region Event Handlers

        protected virtual void OnBinding(IVRPointer pointer) { }

        protected virtual void OnUnbinding(IVRPointer pointer) { }

        private void OnPointerActiveStateChanged(IVRPointer pointer)
        {
            if (pointer == null || pointer.Transform == null)
                return;

            // Update active state
            try
            {
                SetActive(pointer.IsActive);
            }
            catch
            {
                Debug.Log("Skipped as Pointer is null.");
            }
        }

        #endregion
    }
}
