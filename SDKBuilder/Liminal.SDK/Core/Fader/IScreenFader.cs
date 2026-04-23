using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.Core.Fader
{
    public interface IScreenFader
    {
        bool IsSingleton { get; }
        bool IsFading { get; }
        void GotoColor(Color color);
        void GotoColor(Color color, bool hidePointer = true);
        void GotoBlack();
        void GotoBlack(bool hidePointer = true);
        void GotoClear();
        void GotoClear(bool hidePointer = true);
        void FadeToBlack(bool hidePointer = true);
        void FadeToBlack(float duration);
        void FadeToBlack(float duration, bool hidePointer = true);
        void FadeToClear();
        void FadeToClear(bool hidePointer = true);
        void FadeToClear(float duration);
        void FadeToClear(float duration, bool hidePointer = true);
        void FadeToClearFromBlack();
        void FadeToClearFromBlack(bool hidePointer = true);
        void FadeToClearFromBlack(float duration);
        void FadeToClearFromBlack(float duration, bool hidePointer = true);
        void FadeTo(Color color);
        void FadeTo(Color color, bool hidePointer = true);
        void FadeTo(Color color, float duration);
        void FadeTo(Color color, float duration, bool hidePointer = true);
        void StopFade();
        IEnumerator WaitUntilFadeComplete();
    }
}
