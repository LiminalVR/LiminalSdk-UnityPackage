using System.Collections.Generic;
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
    /// frame, so release is grace-timed. A partial reading (~0.05-0.2) means the fingers
    /// are still together and tracking is sagging (<see cref="SagGraceSeconds"/>). A flat
    /// 0 is ambiguous: a deliberate release reads 0 within a frame or two, but the
    /// runtime also collapses a still-held pinch to flat 0 for 0.1-2s during tracking
    /// outages — with the device added, isTracked set, MetaAimFlags.Valid up, no
    /// SystemGesture, and the aim subsystem's pinchStrengthIndex collapsing in lockstep
    /// (verified in Big Top Blast over Link, July 2026), so no pinch_ext-side signal
    /// distinguishes the two. The hand skeleton does: thumb-index fingertip gap reads
    /// 4-18mm on a held pinch and 28mm+ on an open hand even while the pinch value is
    /// suppressed (verified on-device, July 2026), so a held pinch is sustained while
    /// the tracked skeleton shows the fingertips together
    /// (<see cref="HeldFingerGapMeters"/>). The flat-zero window
    /// (<see cref="OpenHandGraceSeconds"/>) remains as the fallback for when the
    /// skeleton is untracked, timed from the first zero frame and sized to bridge the
    /// common sub-second outage at the cost of continuing to fire that long after a
    /// real release. pinch_ext/ready_ext was tried and reverted: it reads 0 for a
    /// lowered/untracked hand exactly like a suppressed one, and freezing on it left
    /// the gun firing forever on-device.
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

        /// <summary>Grace when the value reads fully open (~0), timed from the first zero
        /// frame. A real release reads 0 almost immediately, but so does a tracking outage
        /// on a still-held pinch and no signal tells them apart — sized to bridge the
        /// common sub-second outage; a deliberate release keeps firing this long.</summary>
        public const float OpenHandGraceSeconds = 0.75f;

        /// <summary>Grace when the value reads partial (0.02-0.2): likely a tracking sag.</summary>
        public const float SagGraceSeconds = 1.2f;

        private const float OpenHandValue = 0.02f;

        /// <summary>Thumb-index fingertip gap (hand skeleton) at or below which a held
        /// pinch is sustained even when the runtime's pinch value collapses. On-device
        /// readings: held pinch 4-18mm, open hand 28mm+. Sustain-only — never presses.</summary>
        public const float HeldFingerGapMeters = 0.025f;

        private struct HandState
        {
            public bool Held;
            public int LastFrame;
            public float BelowReleaseSince;
            public float AtZeroSince;
        }

        private static HandState _left = new HandState { LastFrame = -1, BelowReleaseSince = -1f, AtZeroSince = -1f };
        private static HandState _right = new HandState { LastFrame = -1, BelowReleaseSince = -1f, AtZeroSince = -1f };

        public static bool IsHeld(VRInputDeviceHand hand)
        {
            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    return IsHeld(ref _left, ISCommonUsages.LeftHand, MetaAimHand.left, isRight: false);
                case VRInputDeviceHand.Right:
                    return IsHeld(ref _right, ISCommonUsages.RightHand, MetaAimHand.right, isRight: true);
                default:
                    return false;
            }
        }

        private static bool IsHeld(ref HandState state, InternedString usage, MetaAimHand aim, bool isRight)
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
                state.AtZeroSince = -1f;
                return false;
            }

            // Hand-skeleton evidence, an independent data path from pinch_ext: joint
            // poses stay honest while the runtime suppresses the pinch value (system
            // gesture, partial palm-toward-headset angles). On-device: a held pinch
            // reads a 4-18mm thumb-index gap, an open hand 28mm+.
            bool skeletonTracked = TryGetFingertipGap(isRight, out float fingerGap);
            bool fingersTogether = skeletonTracked && fingerGap <= HeldFingerGapMeters;

            // While the Meta system gesture is in progress pinchValue is suppressed to
            // ~0 regardless of the real pose, so keep whatever state we had (with a
            // fresh grace window for when real data resumes). Only while the skeleton
            // is still tracked: any freeze on an untracked hand has no exit and fires
            // forever (a lost hand must fall through to the grace timers instead).
            if (skeletonTracked && aim != null && aim.added &&
                ((MetaAimFlags)aim.aimFlags.ReadValue() & MetaAimFlags.SystemGesture) != 0)
            {
                state.BelowReleaseSince = -1f;
                state.AtZeroSince = -1f;
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
                state.AtZeroSince = -1f;
            }
            else if (value >= ReleaseThreshold || fingersTogether)
            {
                // Value healthy — or the runtime's pinch collapsed while the skeleton
                // still shows the fingertips together, i.e. suppression, not a release.
                state.BelowReleaseSince = -1f;
                state.AtZeroSince = -1f;
            }
            else
            {
                if (state.BelowReleaseSince < 0f)
                    state.BelowReleaseSince = Time.unscaledTime;

                // Flat zero gets its own window from the first zero frame rather than
                // re-tiering the whole below-threshold episode: a sag that eventually
                // touches 0 would otherwise release instantly on time accrued during
                // the partial readings.
                if (value <= OpenHandValue)
                {
                    if (state.AtZeroSince < 0f)
                        state.AtZeroSince = Time.unscaledTime;
                }
                else
                {
                    state.AtZeroSince = -1f;
                }

                bool sagExpired = Time.unscaledTime - state.BelowReleaseSince >= SagGraceSeconds;
                bool zeroExpired = state.AtZeroSince >= 0f &&
                                   Time.unscaledTime - state.AtZeroSince >= OpenHandGraceSeconds;
                if (sagExpired || zeroExpired)
                {
                    state.Held = false;
                    state.BelowReleaseSince = -1f;
                    state.AtZeroSince = -1f;
                }
            }

            return state.Held;
        }

        private static readonly List<XRHandSubsystem> s_HandSubsystems = new List<XRHandSubsystem>();

        /// <summary>
        /// Thumb-index fingertip distance from the running XR Hands subsystem. Returns
        /// false when the hand is untracked or joint poses are unavailable — callers
        /// must treat that as "no evidence", never as "still pinching".
        /// </summary>
        private static bool TryGetFingertipGap(bool isRight, out float gap)
        {
            gap = float.MaxValue;

            SubsystemManager.GetSubsystems(s_HandSubsystems);
            foreach (var subsystem in s_HandSubsystems)
            {
                if (!subsystem.running)
                    continue;

                var hand = isRight ? subsystem.rightHand : subsystem.leftHand;
                if (!hand.isTracked)
                    return false;

                var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
                var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
                if (thumbTip.TryGetPose(out var thumbPose) && indexTip.TryGetPose(out var indexPose))
                {
                    gap = Vector3.Distance(thumbPose.position, indexPose.position);
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
