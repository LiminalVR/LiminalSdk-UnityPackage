using Liminal.SDK.VR.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Liminal.Tools.Common
{
    public static class Lerper
    {
        // handler returns 0->1 t value for custom use
        // Must be called from within a coroutine
        // yield return Lerper.Custom(1.0f, (t) => canvasGroup.alpha = Mathf.Lerp(alpha, 0.0f, t))
        public static IEnumerator Custom(float duration, Action<float> handler)
        {
            if (duration <= 0) yield break;

            float timer = 0;
            while (timer <= duration)
            {
                timer += Time.deltaTime;
                handler?.Invoke(Mathf.Clamp01(timer / duration));
                yield return null;
            }
        }

        // User CoroutineService to handle the routine
        public static Coroutine CustomCo(float duration, Action<float> handler)
        {
            return CoroutineService.Instance.StartCoroutine(Custom(duration, handler));
        }

        // handler returns lerp result for direct use
        // Must be called from within a coroutine
        // yield return Lerper.Basic(1.0f, 1.0f, 0.0f, (t) => canvasGroup.alpha = t);
        public static IEnumerator Basic(float duration, float start, float end, Action<float> handler)
        {
            yield return Custom(duration, (t) => handler?.Invoke(Mathf.Lerp(start, end, t)));
        }

        // User CoroutineService to handle the routine
        public static Coroutine BasicCo(float duration, float start, float end, Action<float> handler)
        {
            return CoroutineService.Instance.StartCoroutine(Basic(duration, start, end, handler));
        }

        // handler returns lerp result for direct use
        // Must be called from within a coroutine
        // yield return Lerper.Basic(1.0f, 1.0f, 0.0f, (t) => canvasGroup.alpha = t);
        public static IEnumerator Color(float duration, Color start, Color end, Action<Color> handler)
        {
            yield return Custom(duration, (t) => handler?.Invoke(UnityEngine.Color.Lerp(start, end, t)));
        }

        // User CoroutineService to handle the routine
        public static Coroutine ColorCo(float duration, Color start, Color end, Action<Color> handler)
        {
            return CoroutineService.Instance.StartCoroutine(Color(duration, start, end, handler));
        }

        // Fade the alpha of a canvasGroup from its current to target
        // Must be called from within a coroutine
        // yield return Lerper.CanvasGroupAlpha(1.0f, 0.0f, canvasGroup);
        public static IEnumerator CanvasGroupAlpha(float duration, float target, CanvasGroup canvasGroup)
        {
            float start = canvasGroup.alpha;
            yield return Basic(duration, start, target, (t) => canvasGroup.alpha = t);
        }

        // User CoroutineService to handle the routine
        public static Coroutine CanvasGroupAlphaCo(float duration, float target, CanvasGroup canvasGroup)
        {
            return CoroutineService.Instance.StartCoroutine(CanvasGroupAlpha(duration, target, canvasGroup));
        }

        // Extension method on canvas group
        public static IEnumerator LerpAlpha(this CanvasGroup canvasGroup, float duration, float target)
        {
            yield return CanvasGroupAlpha(duration, target, canvasGroup);
        }

        // User CoroutineService to handle the routine
        public static Coroutine LerpAlphaCo(this CanvasGroup canvasGroup, float duration, float target)
        {
            return CoroutineService.Instance.StartCoroutine(canvasGroup.LerpAlpha(duration, target));
        }

        // Fade the alpha of a canvasGroup from its current to target
        // Must be called from within a coroutine
        // yield return Lerper.TMPTextColor(1.0f, Color.clear, text);
        public static IEnumerator TMPTextColor(float duration, Color target, TextMeshProUGUI text)
        {
            Color start = text.color;
            yield return Color(duration, start, target, (c) => text.color = c);
        }

        // User CoroutineService to handle the routine
        public static Coroutine TMPTextColorCo(float duration, Color target, TextMeshProUGUI text)
        {
            return CoroutineService.Instance.StartCoroutine(TMPTextColor(duration, target, text));
        }

        // Extension method on TextMeshProUGUI text
        public static IEnumerator LerpColor(this TextMeshProUGUI text, float duration, Color target)
        {
            yield return TMPTextColor(duration, target, text);
        }

        // User CoroutineService to handle the routine
        public static Coroutine LerpColorCo(this TextMeshProUGUI text, float duration, Color target)
        {
            return CoroutineService.Instance.StartCoroutine(text.LerpColor(duration, target));
        }
    }
}