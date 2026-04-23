using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A concrete implementation of <see cref="BasePointer"/> that triggers interaction on hover, after a specified time delay and duration has passed.
    /// </summary>
    public class TimedGazePointer : BasePointer
    {
        // Hover state
        private bool mHovering;
        private GameObject mHoverTarget;
        private float mHoverStartTime;
        private bool mHoverPressTriggered;
        private bool mHoverReleaseTriggered;
        private float mHoverActivationDuration = 2f;
        private float mHoverDelay = 0.5f;

        #region Properties

        /// <summary>
        /// Gets or sets the amount of time the pointer must be hovered over an object before the timer begins.
        /// </summary>
        public float HoverDelay
        {
            get { return mHoverDelay; }
            set { mHoverDelay = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the amount of time the pointer must be hovered over an object before it is activated.
        /// </summary>
        public float HoverActivationDuration
        {
            get { return mHoverActivationDuration; }
            set { mHoverActivationDuration = Mathf.Max(value, 0); }
        }

        /// <summary>
        /// Returns the length of time the pointer has been hovering over <see cref="CurrentTarget"/>.
        /// </summary>
        public float HoverDuration
        {
            get
            {
                if (!mHovering)
                    return 0;

                var elapsed = Time.realtimeSinceStartup - (mHoverStartTime + mHoverDelay);
                return elapsed;
            }
        }

        /// <summary>
        /// Gets the amount of time until <see cref="CurrentTarget"/> will be activated due to being hovered.
        /// </summary>
        public float HoverTimeToActivation
        {
            get
            {
                if (!mHovering)
                    return 0;

                var elapsed = Time.realtimeSinceStartup - (mHoverStartTime + mHoverDelay);
                return (mHoverActivationDuration - elapsed);
            }
        }

        /// <summary>
        /// Gets the normalized progress of the current hover.
        /// </summary>
        public float HoverTimeNormalized
        {
            get
            {
                if (!mHovering || mHoverPressTriggered)
                    return 0;

                var elapsed = Time.realtimeSinceStartup - (mHoverStartTime + mHoverDelay);
                return Mathf.Clamp01(elapsed / mHoverActivationDuration);
            }
        }

        /// <summary>
        /// Indicates if the hover press has been triggered.
        /// </summary>
        public bool HoverPressTriggered
        {
            get { return mHoverPressTriggered; }
        }

        #endregion

        public TimedGazePointer(IVRDeviceComponent deviceComponent) : base(deviceComponent)
        {
            //
        }

        public override void OnPointerEnter(GameObject target)
        {
            mHoverTarget = target;
            mHovering = IsInteractable(target);
            mHoverStartTime = Time.realtimeSinceStartup;
            mHoverReleaseTriggered = false;
            mHoverPressTriggered = false;
        }

        public override void OnPointerExit(GameObject target)
        {
            if (mHoverTarget != target)
                return;

            mHoverTarget = null;
            mHovering = false;

            // If the 'down' state was triggered, trigger the 'up' state
            mHoverReleaseTriggered = mHoverPressTriggered;
            mHoverPressTriggered = false;
        }

        private bool IsInteractable(GameObject target)
        {
            if (target == null)
                return false;

            if (target.GetComponentInParent<IPointerClickHandler>() != null)
                return true;

            if (target.GetComponentInParent<IPointerDownHandler>() != null)
                return true;

            if (target.GetComponentInParent<ISubmitHandler>() != null)
                return true;

            return false;
        }

        public override bool GetButtonDown()
        {
            if (mHoverTarget == null)
                return false;

            if (!mHoverPressTriggered)
            {
                var elapsed = (Time.realtimeSinceStartup - (mHoverStartTime + mHoverDelay));
                if (elapsed >= mHoverActivationDuration)
                {
                    mHoverPressTriggered = true;
                    return true;
                }
            }

            return false;
        }

        public override bool GetButtonUp()
        {
            var wasTriggered = mHoverReleaseTriggered;

            // Reset the up trigger once hit
            mHoverReleaseTriggered = false;
            return wasTriggered;
        }
    }
}
