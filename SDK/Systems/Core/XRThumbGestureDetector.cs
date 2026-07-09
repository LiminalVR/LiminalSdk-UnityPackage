using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace Liminal.SDK.V2
{
    /// <summary>
    /// Detects Meta-style thumb microgestures (tap and directional swipes) performed against the
    /// side of the index finger while the other fingers are curled into a relaxed fist.
    ///
    /// Direction semantics follow Meta's microgestures:
    /// - Left/Right: thumb slides along the index finger. On the right hand, toward the fingertip
    ///   is Left and toward the wrist is Right; mirrored on the left hand.
    /// - Forward: thumb slides across the index finger toward the knuckles (back of the hand).
    /// - Backward: thumb slides across the index finger toward the palm.
    /// - Tap: thumb presses the middle segment of the index finger and lifts without sliding.
    ///
    /// A gesture only starts while the thumb is pressed against the index finger and the
    /// middle/ring/little fingers are curled, so it is hard to trigger accidentally. The thumb
    /// may lift before the swipe distance is reached (see liftGraceDuration).
    /// </summary>
    public class XRThumbGestureDetector : MonoBehaviour
    {
        enum State
        {
            Idle,
            Pressed,
            LiftGrace,
            Cooldown,
        }

        struct JointData
        {
            public Vector3 Wrist;
            public Vector3 ThumbTip;
            public Vector3 IndexProximal;
            public Vector3 IndexIntermediate;
            public Vector3 IndexDistal;
            public Vector3 MiddleProximal;
            public int ClosedFingerCount;
        }

        [Header("Hand")]
        public bool useRightHand = true;

        [Header("Events")]
        public UnityEvent OnSwipeLeft;
        public UnityEvent OnSwipeRight;
        public UnityEvent OnThumbTap;

        [Tooltip("Fired when the thumb makes contact with the side of the index finger.")]
        public UnityEvent OnThumbDown;

        [Tooltip("Fired when the thumb lifts off the index finger.")]
        public UnityEvent OnThumbUp;

        [Header("Pose Gate")]
        [Tooltip("How many of the middle/ring/little fingers must be curled for a gesture to start.")]
        [Range(0, 3)]
        public int requiredClosedFingers = 3;

        [Tooltip("A finger counts as closed when wrist->tip is under wrist->knuckle times this factor.")]
        public float closedRelativeFactor = 1.45f;

        [Tooltip("A finger also counts as closed when wrist->tip is under this distance in meters.")]
        public float closedAbsoluteDistance = 0.085f;

        [Header("Thumb Press")]
        [Tooltip("Thumb tip must be within this distance (meters) of the index finger to count as pressed. The finger flesh is ~10mm thick, so values much above 0.02 will count hovering as a press.")]
        public float thumbToIndexMaxDistance = 0.018f;

        [Tooltip("Thumb counts as lifted once it is beyond this distance (meters). Must be larger than the press distance.")]
        public float thumbReleaseDistance = 0.03f;

        [Header("Swipe")]
        [Tooltip("Joystick deflection (0-1) the thumb must reach for a swipe. Lower = more sensitive.")]
        [Range(0f, 1f)]
        public float swipeDeflectionThreshold = 0.8f;

        [Tooltip("Rolling window (seconds): the swipe deflection must happen within this window, so slow drift never fires but a flick always can, even mid-hold.")]
        public float maxGestureDuration = 0.65f;

        [Tooltip("Seconds the swipe may keep travelling after the thumb lifts off the finger.")]
        public float liftGraceDuration = 0.15f;

        [Tooltip("Normalized deflection (0-1) past the press point the motion must end at for a swipe, so a fast return-to-center stroke that overshoots slightly does not fire the opposite swipe.")]
        [Range(0f, 1f)]
        public float swipeMinEndDeflection = 0.3f;

        public bool invertHorizontal;

        [Header("Joystick")]
        [Tooltip("Thumb travel (meters) from the press point for full joystick deflection.")]
        public float joystickRadius = 0.02f;

        [Tooltip("Normalized deflection below this is treated as centered.")]
        [Range(0f, 1f)]
        public float joystickDeadzone = 0.15f;

        [Header("Tap")]
        [Tooltip("Minimum press duration (seconds) for a tap, so a single-frame tracking glitch across the press/release thresholds cannot fire one.")]
        public float tapMinDuration = 0.05f;

        [Tooltip("Maximum press duration (seconds) for a tap.")]
        public float tapMaxDuration = 0.35f;

        [Tooltip("Maximum thumb travel (meters) during the press for a tap.")]
        public float tapMaxMovement = 0.012f;

        [Header("Cooldown")]
        [Tooltip("Seconds after a gesture fires before another can start.")]
        public float gestureCooldown = 0.25f;

        [Header("Debug")]
        [Tooltip("Logs press distance, pose gate and thumb position every quarter second.")]
        public bool verboseLogging;

        /// <summary>The active detector for each hand, registered on enable.</summary>
        public static XRThumbGestureDetector LeftHand { get; private set; }
        public static XRThumbGestureDetector RightHand { get; private set; }

        /// <summary>Static gesture events; the argument is the detector that fired (check useRightHand for the hand).</summary>
        public static event Action<XRThumbGestureDetector> SwipeLeftPerformed;
        public static event Action<XRThumbGestureDetector> SwipeRightPerformed;
        public static event Action<XRThumbGestureDetector> TapPerformed;
        public static event Action<XRThumbGestureDetector> ThumbDownPerformed;
        public static event Action<XRThumbGestureDetector> ThumbUpPerformed;

        /// <summary>Poll-style static state, safe to read even when no detector exists.</summary>
        public static bool LeftThumbPressed => LeftHand != null && LeftHand.IsThumbPressed;
        public static bool RightThumbPressed => RightHand != null && RightHand.IsThumbPressed;
        public static Vector2 LeftJoystick => LeftHand != null ? LeftHand.JoystickAxis : Vector2.zero;
        public static Vector2 RightJoystick => RightHand != null ? RightHand.JoystickAxis : Vector2.zero;

        /// <summary>True while the thumb is pressed against the index finger in the gated pose.</summary>
        public bool IsThumbPressed => m_ContactEventActive;

        /// <summary>True while the thumb is pressed in the gated pose and driving the joystick.</summary>
        public bool JoystickActive { get; private set; }

        /// <summary>
        /// Normalized thumb deflection from the press point, deadzone applied, magnitude clamped to 1.
        /// +x = Right, +y = Forward (same direction conventions and invert flags as the swipes).
        /// Zero while the thumb is not pressed.
        /// </summary>
        public Vector2 JoystickAxis { get; private set; }

        XRHandSubsystem m_Subsystem;
        static readonly List<XRHandSubsystem> s_SubsystemsReuse = new();

        State m_State;
        bool m_IsPressed;
        bool m_WasTracked;
        bool m_ContactEventActive;
        float m_PressStartTime;
        Vector2 m_PressStartLocal;
        float m_PeakTravel;
        readonly Queue<(float Time, float Deflection)> m_SwipeSamples = new();
        float m_WindowRise;
        float m_WindowFall;
        float m_LiftTime;
        float m_CooldownUntil;
        float m_NextVerboseTime;
        float m_NextGateLogTime;

        void OnEnable()
        {
            m_State = State.Idle;
            m_IsPressed = false;
            m_ContactEventActive = false;

            if (useRightHand)
                RightHand = this;
            else
                LeftHand = this;

            Log($"Enabled. press<={thumbToIndexMaxDistance * 1000f:F0}mm release>={EffectiveReleaseDistance * 1000f:F0}mm " +
                $"swipe>={swipeDeflectionThreshold:F2} deflection (joystick radius {joystickRadius * 1000f:F0}mm) " +
                $"tap {tapMinDuration:F2}-{tapMaxDuration:F2}s/<={tapMaxMovement * 1000f:F0}mm gate={requiredClosedFingers}/3 fingers closed");
        }

        void OnDisable()
        {
            AbortActiveGesture("component disabled");

            if (RightHand == this)
                RightHand = null;
            if (LeftHand == this)
                LeftHand = null;
        }

        float EffectiveReleaseDistance => Mathf.Max(thumbReleaseDistance, thumbToIndexMaxDistance + 0.005f);

        public void Log(string message)
        {
            Debug.Log($"[ThumbGesture:{(useRightHand ? "R" : "L")}] {message}", this);
        }

        void Update()
        {
            var now = Time.unscaledTime;

            if (!TryGetHand(out var hand))
            {
                AbortActiveGesture("hand tracking lost");
                return;
            }

            if (!TryGetJoints(hand, out var joints))
            {
                AbortActiveGesture("joint poses unavailable");
                return;
            }

            // Distance from the thumb tip to the index finger (proximal and middle segments).
            var pressDistance = Mathf.Min(
                DistanceToSegment(joints.ThumbTip, joints.IndexIntermediate, joints.IndexDistal),
                DistanceToSegment(joints.ThumbTip, joints.IndexProximal, joints.IndexIntermediate));

            var wasPressed = m_IsPressed;
            if (!m_IsPressed && pressDistance <= thumbToIndexMaxDistance)
                m_IsPressed = true;
            else if (m_IsPressed && pressDistance >= EffectiveReleaseDistance)
                m_IsPressed = false;

            var pressEnded = !m_IsPressed && wasPressed;

            if (!TryComputeLocalThumbPosition(joints, out var local))
            {
                AbortActiveGesture("degenerate joint data");
                return;
            }

            if (pressEnded && m_ContactEventActive)
            {
                m_ContactEventActive = false;
                OnThumbUp.Invoke();
                ThumbUpPerformed?.Invoke(this);
            }

            switch (m_State)
            {
                case State.Idle:
                {
                    // Start whenever the thumb is held in the gated pose, not only on the contact
                    // frame, so a press that lands before the fingers finish curling (common when
                    // forming a fist) or that began during LiftGrace/Cooldown still becomes a
                    // gesture instead of being eaten until the next full lift.
                    if (!m_IsPressed)
                        break;

                    if (joints.ClosedFingerCount >= requiredClosedFingers)
                    {
                        m_State = State.Pressed;
                        m_PressStartTime = now;
                        m_PressStartLocal = local;
                        m_PeakTravel = 0f;
                        m_SwipeSamples.Clear();
                        m_ContactEventActive = true;
                        Log($"Thumb down (distance {pressDistance * 1000f:F0}mm, closed fingers {joints.ClosedFingerCount}/3)");
                        OnThumbDown.Invoke();
                        ThumbDownPerformed?.Invoke(this);
                    }
                    else if (now >= m_NextGateLogTime)
                    {
                        m_NextGateLogTime = now + 1f;
                        Log($"Thumb press ignored - pose gate failed ({joints.ClosedFingerCount}/{requiredClosedFingers} fingers closed)");
                    }

                    break;
                }

                case State.Pressed:
                {
                    var travel = local - m_PressStartLocal;
                    m_PeakTravel = Mathf.Max(m_PeakTravel, travel.magnitude);
                    var duration = now - m_PressStartTime;

                    if (pressEnded)
                    {
                        // The pose gate must still (mostly) hold at release — one finger of
                        // hysteresis for tracking noise during the lift — so opening the hand
                        // out of the pose does not read as a tap.
                        if (duration >= tapMinDuration && duration <= tapMaxDuration &&
                            m_PeakTravel <= tapMaxMovement &&
                            joints.ClosedFingerCount >= requiredClosedFingers - 1)
                        {
                            Log($"Thumb tap ({duration:F2}s, peak travel {m_PeakTravel * 1000f:F0}mm)");
                            OnThumbTap.Invoke();
                            TapPerformed?.Invoke(this);
                            StartCooldown(now);
                        }
                        else
                        {
                            m_LiftTime = now;
                            m_State = State.LiftGrace;
                            Log($"Thumb up after {duration:F2}s, travel {Describe(travel)} - watching {liftGraceDuration:F2}s for swipe completion");
                        }
                    }
                    else if (TryDetectSwipe(now, travel, out var swipeEvent, out var swipeName))
                    {
                        FireSwipe(swipeEvent, swipeName, travel, duration, now);
                    }

                    break;
                }

                case State.LiftGrace:
                {
                    var travel = local - m_PressStartLocal;
                    var duration = now - m_PressStartTime;

                    if (TryDetectSwipe(now, travel, out var swipeEvent, out var swipeName))
                    {
                        FireSwipe(swipeEvent, swipeName, travel, duration, now);
                    }
                    else if (now - m_LiftTime >= liftGraceDuration)
                    {
                        Log($"No gesture ({duration:F2}s, travel {Describe(travel)}, window delta L {m_WindowFall:F2} / R {m_WindowRise:F2} vs needed {swipeDeflectionThreshold:F2})");
                        m_State = State.Idle;
                    }

                    break;
                }

                case State.Cooldown:
                {
                    if (now < m_CooldownUntil)
                        break;

                    // Re-arm while still held so back-to-back swipes don't require a full lift.
                    if (m_IsPressed && m_ContactEventActive)
                    {
                        m_State = State.Pressed;
                        if (verboseLogging)
                            Log("Re-armed while held");
                    }
                    else
                    {
                        // Always leave the cooldown once it expires; a press that began during
                        // it (contact not yet active) is picked up by Idle on the next frame.
                        m_State = State.Idle;
                    }

                    break;
                }
            }

            UpdateJoystick(local);

            if (verboseLogging && now >= m_NextVerboseTime)
            {
                m_NextVerboseTime = now + 0.25f;
                Log($"state={m_State} pressDist={pressDistance * 1000f:F0}mm pressed={m_IsPressed} " +
                    $"closed={joints.ClosedFingerCount}/3 local={Describe(local)} " +
                    $"joystick={(JoystickActive ? JoystickAxis.ToString("F2") : "inactive")}");
            }
        }

        void UpdateJoystick(Vector2 local)
        {
            // The joystick is live for the whole gated contact, from thumb down until lift.
            if (!m_ContactEventActive)
            {
                JoystickActive = false;
                JoystickAxis = Vector2.zero;
                return;
            }

            JoystickActive = true;
            JoystickAxis = ComputeJoystickAxis(local - m_PressStartLocal);
        }

        /// <summary>
        /// Normalized joystick deflection for a given thumb travel, deadzone applied,
        /// magnitude clamped to 1. +x = Right, +y = up over the knuckles.
        /// </summary>
        Vector2 ComputeJoystickAxis(Vector2 travel)
        {
            var x = useRightHand ? -travel.x : travel.x;
            if (invertHorizontal)
                x = -x;

            var axis = new Vector2(x, travel.y) / Mathf.Max(joystickRadius, 1e-4f);
            var magnitude = axis.magnitude;
            if (magnitude < joystickDeadzone)
                return Vector2.zero;

            var deflection = Mathf.Min((magnitude - joystickDeadzone) / (1f - joystickDeadzone), 1f);
            return axis / magnitude * deflection;
        }

        bool TryGetHand(out XRHand hand)
        {
            hand = default;

            if (m_Subsystem == null || !m_Subsystem.running)
            {
                SubsystemManager.GetSubsystems(s_SubsystemsReuse);
                m_Subsystem = null;
                for (int i = 0; i < s_SubsystemsReuse.Count; ++i)
                {
                    if (s_SubsystemsReuse[i].running)
                    {
                        m_Subsystem = s_SubsystemsReuse[i];
                        Log($"Using hand subsystem '{m_Subsystem.subsystemDescriptor?.id}'");
                        break;
                    }
                }

                if (m_Subsystem == null)
                    return false;
            }

            hand = useRightHand ? m_Subsystem.rightHand : m_Subsystem.leftHand;

            if (hand.isTracked != m_WasTracked)
            {
                m_WasTracked = hand.isTracked;
                Log(hand.isTracked ? "Hand tracking acquired" : "Hand tracking lost");
            }

            return hand.isTracked;
        }

        bool TryGetJoints(XRHand hand, out JointData joints)
        {
            joints = default;

            if (!TryGetJointPosition(hand, XRHandJointID.Wrist, out joints.Wrist) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbTip, out joints.ThumbTip) ||
                !TryGetJointPosition(hand, XRHandJointID.IndexProximal, out joints.IndexProximal) ||
                !TryGetJointPosition(hand, XRHandJointID.IndexIntermediate, out joints.IndexIntermediate) ||
                !TryGetJointPosition(hand, XRHandJointID.IndexDistal, out joints.IndexDistal) ||
                !TryGetJointPosition(hand, XRHandJointID.MiddleProximal, out joints.MiddleProximal))
            {
                return false;
            }

            joints.ClosedFingerCount =
                (IsFingerClosed(hand, joints.Wrist, XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip) ? 1 : 0) +
                (IsFingerClosed(hand, joints.Wrist, XRHandJointID.RingProximal, XRHandJointID.RingTip) ? 1 : 0) +
                (IsFingerClosed(hand, joints.Wrist, XRHandJointID.LittleProximal, XRHandJointID.LittleTip) ? 1 : 0);

            return true;
        }

        static bool TryGetJointPosition(XRHand hand, XRHandJointID id, out Vector3 position)
        {
            if (hand.GetJoint(id).TryGetPose(out var pose))
            {
                position = pose.position;
                return true;
            }

            position = default;
            return false;
        }

        bool IsFingerClosed(XRHand hand, Vector3 wrist, XRHandJointID proximalId, XRHandJointID tipId)
        {
            if (!TryGetJointPosition(hand, proximalId, out var proximal) ||
                !TryGetJointPosition(hand, tipId, out var tip))
            {
                return false;
            }

            var tipDistance = Vector3.Distance(tip, wrist);
            var proximalDistance = Vector3.Distance(proximal, wrist);
            return tipDistance <= proximalDistance * closedRelativeFactor || tipDistance <= closedAbsoluteDistance;
        }

        /// <summary>
        /// Thumb tip position in hand-relative curvilinear coordinates on the side of the index
        /// finger, so hand/head movement does not register as thumb travel.
        /// x = arc length along the proximal->intermediate->distal bone chain toward the fingertip,
        /// y = thumb height above/below the chain along the hand's up axis (back of the hand),
        /// so forward/backward tracks the thumb physically moving up or down.
        /// Following the bent bone chain (rather than one straight axis) keeps a slide along the
        /// curled finger as pure x travel instead of bleeding into y as the contact point bends
        /// toward the palm. The up axis comes from the proximal segment only, because that segment
        /// does not curl with the fist and so stays aligned with the hand.
        /// </summary>
        bool TryComputeLocalThumbPosition(JointData joints, out Vector2 local)
        {
            local = default;

            // Hand-stable up axis: radial direction (middle finger out through the index finger,
            // thumb side) crossed with the proximal segment.
            var proximalDirection = joints.IndexIntermediate - joints.IndexProximal;
            if (proximalDirection.sqrMagnitude < 1e-8f)
                return false;
            proximalDirection.Normalize();

            var radial = joints.IndexProximal - joints.MiddleProximal;
            radial -= Vector3.Dot(radial, proximalDirection) * proximalDirection;
            if (radial.sqrMagnitude < 1e-8f)
                return false;
            radial.Normalize();

            var upAxis = useRightHand
                ? Vector3.Cross(radial, proximalDirection)
                : Vector3.Cross(proximalDirection, radial);

            var thumb = joints.ThumbTip;
            EvaluateSegment(thumb, joints.IndexProximal, joints.IndexIntermediate, out var tProximal, out var distProximal);
            EvaluateSegment(thumb, joints.IndexIntermediate, joints.IndexDistal, out var tMiddle, out var distMiddle);

            Vector3 segmentStart, segmentEnd;
            float tUnclamped, arcLengthBase;
            if (distProximal <= distMiddle)
            {
                segmentStart = joints.IndexProximal;
                segmentEnd = joints.IndexIntermediate;
                tUnclamped = tProximal;
                arcLengthBase = 0f;
            }
            else
            {
                segmentStart = joints.IndexIntermediate;
                segmentEnd = joints.IndexDistal;
                tUnclamped = tMiddle;
                arcLengthBase = Vector3.Distance(joints.IndexProximal, joints.IndexIntermediate);
            }

            var segmentDirection = segmentEnd - segmentStart;
            var segmentLength = segmentDirection.magnitude;
            if (segmentLength < 1e-4f)
                return false;
            segmentDirection /= segmentLength;

            var closestOnChain = segmentStart + segmentDirection * (Mathf.Clamp01(tUnclamped) * segmentLength);
            var offset = thumb - closestOnChain;

            // Unclamped t lets the arc length extend smoothly past the chain ends.
            local = new Vector2(arcLengthBase + tUnclamped * segmentLength, Vector3.Dot(offset, upAxis));
            return true;
        }

        static void EvaluateSegment(Vector3 point, Vector3 a, Vector3 b, out float tUnclamped, out float distance)
        {
            var ab = b - a;
            tUnclamped = Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f);
            distance = Vector3.Distance(point, a + ab * Mathf.Clamp01(tUnclamped));
        }

        /// <summary>
        /// Watches the change in horizontal deflection over a rolling maxGestureDuration window,
        /// so a flick fires no matter how long the thumb has been resting or how it drifted first.
        /// Raw (deadzone-free) deflection is used so the delta stays linear.
        /// </summary>
        bool TryDetectSwipe(float now, Vector2 travel, out UnityEvent swipeEvent, out string swipeName)
        {
            swipeEvent = null;
            swipeName = null;

            var x = useRightHand ? -travel.x : travel.x;
            if (invertHorizontal)
                x = -x;
            x /= Mathf.Max(joystickRadius, 1e-4f);

            m_SwipeSamples.Enqueue((now, x));
            while (m_SwipeSamples.Count > 0 && m_SwipeSamples.Peek().Time < now - maxGestureDuration)
                m_SwipeSamples.Dequeue();

            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var sample in m_SwipeSamples)
            {
                min = Mathf.Min(min, sample.Deflection);
                max = Mathf.Max(max, sample.Deflection);
            }

            m_WindowRise = x - min;
            m_WindowFall = max - x;

            // A return-to-center stroke has the same window delta as a swipe but ends at or near
            // the press center, so the motion must also end meaningfully past center in its
            // direction — a slight overshoot on the way back must not fire the opposite swipe.
            var rightValid = m_WindowRise >= swipeDeflectionThreshold && x >= swipeMinEndDeflection;
            var leftValid = m_WindowFall >= swipeDeflectionThreshold && x <= -swipeMinEndDeflection;

            if (!rightValid && !leftValid)
                return false;

            var right = rightValid && (!leftValid || m_WindowRise >= m_WindowFall);
            swipeEvent = right ? OnSwipeRight : OnSwipeLeft;
            swipeName = right ? "Right" : "Left";
            return true;
        }

        void FireSwipe(UnityEvent swipeEvent, string swipeName, Vector2 travel, float duration, float now)
        {
            Log($"Swipe {swipeName} ({duration:F2}s, travel {Describe(travel)})");
            swipeEvent.Invoke();
            (swipeEvent == OnSwipeRight ? SwipeRightPerformed : SwipeLeftPerformed)?.Invoke(this);
            StartCooldown(now);
        }

        void StartCooldown(float now)
        {
            m_CooldownUntil = now + gestureCooldown;
            m_State = State.Cooldown;
            m_SwipeSamples.Clear();
        }

        void AbortActiveGesture(string reason)
        {
            JoystickActive = false;
            JoystickAxis = Vector2.zero;
            m_SwipeSamples.Clear();

            if (m_ContactEventActive)
            {
                m_ContactEventActive = false;
                OnThumbUp.Invoke();
                ThumbUpPerformed?.Invoke(this);
            }

            if (m_State == State.Pressed || m_State == State.LiftGrace)
            {
                Log($"Gesture aborted - {reason}");
                m_State = State.Idle;
            }

            m_IsPressed = false;
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            return Vector3.Distance(point, a + ab * t);
        }

        static string Describe(Vector2 v)
        {
            return $"(along {v.x * 1000f:F0}mm, across {v.y * 1000f:F0}mm)";
        }
    }
}
