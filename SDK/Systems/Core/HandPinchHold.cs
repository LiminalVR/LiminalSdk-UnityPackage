using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Hands;
using UnityEngine.XR.OpenXR.Features.Interactions;
using ISCommonUsages = UnityEngine.InputSystem.CommonUsages;

namespace Liminal.SDK.V2
{
    /// <summary>
    /// Hysteresis-based pinch-hold reader for hand tracking.
    ///
    /// The Meta runtime's boolean pinch (pinch_ext -> pinchTouched, roughly 0.6 press /
    /// 0.5 release) drops out when a held pinch relaxes into a fist/grip pose: the
    /// analog pinch value sags to ~0.25-0.45 even though the fingers never separate,
    /// so a held trigger "stops firing" after a few seconds. This reads the analog
    /// pinchValue straight off the HandInteraction device with a much wider hysteresis
    /// window: a pinch must reach <see cref="PressThreshold"/> to start, but keeps
    /// holding until it falls below <see cref="ReleaseThreshold"/>.
    ///
    /// Reading the control directly (rather than through an InputAction) also avoids
    /// two traps: the SDK input actions asset only binds pinchValue for the right hand,
    /// and Button actions never re-perform on an already-held control after a device
    /// re-add (e.g. a Link hiccup or tracking session restart).
    ///
    /// The analog value also sags below the release threshold for around a second at a
    /// time when the tracked hand goes edge-on to the headset cameras (typical of the
    /// extended aiming hand), then recovers. A raw threshold releases on the first bad
    /// frame, so release is grace-timed — and the grace is tiered on what the value
    /// reads while below threshold: a deliberate release collapses to a flat 0 within a
    /// frame or two (<see cref="OpenHandGraceSeconds"/>, keeps the gun feeling
    /// responsive), while a partial reading (~0.05-0.2) means the fingers are still
    /// together and tracking is sagging (<see cref="SagGraceSeconds"/>).
    ///
    /// Two further runtime quirks (both reproduced by rotating the palm toward the
    /// headset): while MetaAimHand reports SystemGesture the runtime suppresses pinch
    /// data entirely (value reads ~0 with the fingers still pinched), so the hold state
    /// is frozen for the duration of the gesture; and after tracking degrades the value
    /// can sit in the 0.45-0.55 band where the runtime's own boolean pinch flickers at
    /// its ~0.5 threshold, so that boolean is accepted as press evidence alongside
    /// <see cref="PressThreshold"/> — once latched, the wide hysteresis keeps the hold
    /// steady instead of stuttering with the button.
    /// </summary>
    public static class HandPinchHold
    {
        public const float PressThreshold = 0.55f;
        public const float ReleaseThreshold = 0.2f;

        /// <summary>Grace when the value reads fully open (~0): trust it quickly.</summary>
        public const float OpenHandGraceSeconds = 0.15f;

        /// <summary>Grace when the value reads partial (0.02-0.2): likely a tracking sag.</summary>
        public const float SagGraceSeconds = 1.2f;

        private const float OpenHandValue = 0.02f;

        private struct HandState
        {
            public bool Held;
            public int LastFrame;
            public float BelowReleaseSince;
        }

        private static HandState _left = new HandState { LastFrame = -1 };
        private static HandState _right = new HandState { LastFrame = -1 };

        public static bool IsHeld(VRInputDeviceHand hand)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    return IsHeld(ref _left, ISCommonUsages.LeftHand, MetaAimHand.left);
                case VRInputDeviceHand.Right:
                    return IsHeld(ref _right, ISCommonUsages.RightHand, MetaAimHand.right);
                default:
                    return false;
            }
        }

        private static bool IsHeld(ref HandState state, InternedString usage, MetaAimHand aim)
        {
            // Evaluate once per frame per hand; further callers this frame reuse the result.
            if (state.LastFrame == Time.frameCount)
                return state.Held;
            state.LastFrame = Time.frameCount;

            var device = InputSystem.GetDevice<HandInteractionProfile.HandInteraction>(usage);
            if (device == null || !device.added)
            {
                state.Held = false;
                state.BelowReleaseSince = -1f;
                return false;
            }

            // While the Meta system gesture is in progress pinchValue is suppressed to
            // ~0 regardless of the real pose, so keep whatever state we had (with a
            // fresh grace window for when real data resumes).
            if (aim != null && aim.added &&
                ((MetaAimFlags)aim.aimFlags.ReadValue() & MetaAimFlags.SystemGesture) != 0)
            {
                state.BelowReleaseSince = -1f;
                return state.Held;
            }

            float value = device.pinchValue.ReadValue();

            if (!state.Held)
            {
                // The runtime's boolean pinch presses at ~0.5 with its own hysteresis;
                // accept it as press evidence so the 0.45-0.55 band latches the hold
                // instead of leaving firing to stutter on the flickering button.
                state.Held = value >= PressThreshold || device.pinchTouched.isPressed;
                state.BelowReleaseSince = -1f;
            }
            else if (value >= ReleaseThreshold)
            {
                state.BelowReleaseSince = -1f;
            }
            else
            {
                if (state.BelowReleaseSince < 0f)
                    state.BelowReleaseSince = Time.unscaledTime;

                float grace = value <= OpenHandValue ? OpenHandGraceSeconds : SagGraceSeconds;
                if (Time.unscaledTime - state.BelowReleaseSince >= grace)
                {
                    state.Held = false;
                    state.BelowReleaseSince = -1f;
                }
            }

            return state.Held;
        }
    }
}
