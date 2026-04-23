using System.Collections;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars.Controllers
{
    /// <summary>
    /// Extension methods for <see cref="VRControllerInputVisual"/> components.
    /// </summary>
    public static class VRControllerInputVisualExtensions
    {
        private static AnimationCurve _linearCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private static AnimationCurve _sCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// Fades the color of a <see cref="VRControllerInputVisual"/> over time.
        /// </summary>
        /// <param name="inputVisual">The <see cref="VRControllerInputVisual"/> to fade.</param>
        /// <param name="color">The color to fade to.</param>
        /// <param name="duration">The duration of the fade, in secondse.</param>
        /// <param name="curve">The curve of the fade over time.</param>
        /// <returns>An awaitable Coroutine.</returns>
        public static Coroutine FadeColor(this VRControllerInputVisual inputVisual, Color color, float duration, AnimationCurve curve = null)
        {
            return inputVisual.StartCoroutine(_Fade(inputVisual, color, duration, curve));
        }

        /// <summary>
        /// Pulses the color of a <see cref="VRControllerInputVisual"/> over time.
        /// </summary>
        /// <param name="inputVisual">The <see cref="VRControllerInputVisual"/> to pulse.</param>
        /// <param name="color">The color to fade to.</param>
        /// <param name="count">The number of times to pulse within <paramref name="duration"/>.</param>
        /// <param name="duration">The duration of the pulse, in seconds.</param>
        /// <param name="curve">The curve of the fade over time.</param>
        /// <returns>An awaitable Coroutine.</returns>
        public static Coroutine PulseColor(this VRControllerInputVisual inputVisual, Color color, int count, float duration, AnimationCurve curve = null)
        {
            return inputVisual.StartCoroutine(_Pulse(inputVisual, color, count, duration, curve));
        }

        /// <summary>
        /// Pulses the color of a <see cref="VRControllerInputVisual"/> at a specific rate indefinitely.
        /// </summary>
        /// <param name="inputVisual">The <see cref="VRControllerInputVisual"/> to pulse.</param>
        /// <param name="color">The color to fade to.</param>
        /// <param name="rate">The rate of each pulse, in seconds.</param>
        /// <param name="curve">The curve of the fade over time.</param>
        /// <returns>An awaitable Coroutine.</returns>
        public static Coroutine PulseColor(this VRControllerInputVisual inputVisual, Color color, float rate, AnimationCurve curve = null)
        {
            return inputVisual.StartCoroutine(_PulseIndefinitely(inputVisual, color, rate, curve));
        }

        #region Routines

        private static IEnumerator _Fade(VRControllerInputVisual inputVisual, Color color, float duration, AnimationCurve curve)
        {
            if (duration <= 0)
            {
                inputVisual.Color = color;
                yield break;
            }

            curve = curve ?? _linearCurve;

            var startColor = inputVisual.Color;
            var startTime = Time.time;
            while (true)
            {
                var elapsed = Time.time - startTime;
                var t = Mathf.Clamp01(elapsed / duration);
                inputVisual.Color = Color.Lerp(startColor, color, curve.Evaluate(t));

                if (t >= 1f)
                    break;

                yield return null;
            }
        }

        private static IEnumerator _Pulse(VRControllerInputVisual inputVisual, Color color, int count, float duration, AnimationCurve curve)
        {
            if (duration <= 0 || count <= 0)
                yield break;

            curve = curve ?? _sCurve;

            var startColor = inputVisual.Color;
            var doubleCount = count * 2;
            var rate = duration / doubleCount;
            int dir = 1;
            for (int i = 0; i < doubleCount; ++i)
            {
                var startTime = Time.time;
                while (true)
                {
                    var elapsed = Time.time - startTime;
                    var t = Mathf.Clamp01(elapsed / rate);
                    var f = curve.Evaluate(t);
                    if (dir < 0)
                        f = 1 - f;

                    inputVisual.Color = Color.Lerp(startColor, color, f);

                    if (t >= 1f)
                        break;

                    yield return null;
                }

                // Reverse direction
                dir *= -1;
            }

        }

        private static IEnumerator _PulseIndefinitely(VRControllerInputVisual inputVisual, Color color, float rate, AnimationCurve curve)
        {
            if (rate <= 0)
                yield break;

            curve = curve ?? _sCurve;

            var startColor = inputVisual.Color;
            int dir = 1;
            while (true)
            {
                var startTime = Time.time;
                while (true)
                {
                    var elapsed = Time.time - startTime;
                    var t = Mathf.Clamp01(elapsed / rate);
                    var f = curve.Evaluate(t);
                    if (dir < 0)
                        f = 1 - f;

                    inputVisual.Color = Color.Lerp(startColor, color, f);

                    if (t >= 1f)
                        break;

                    yield return null;
                }

                // Reverse direction
                dir *= -1;
            }
        }

        #endregion
    }
}
