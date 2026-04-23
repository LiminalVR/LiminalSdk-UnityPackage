using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Liminal.Core.Fader
{
	public class CompoundScreenFader : ScreenFader, IScreenFader
    {
        [SerializeField] private List<ScreenFader> m_Faders = new List<ScreenFader>();

        #region Properties

        /// <summary>
        /// Indicates if a fade is currently in progress.
        /// </summary>
        public override bool IsFading
		{
			get
            {
                for (int i = 0; i < m_Faders.Count; ++i)
                {
                    var fader = m_Faders[i];
                    if (fader == null || !fader.enabled)
                        continue;

                    if (fader.IsFading)
                        return true;
                }

                return false;
            }
		}

        /// <summary>
        /// Gets the list of <see cref="ScreenFader"/> instances controlled by this compound fader.
        /// </summary>
        public List<ScreenFader> Faders
        {
            get { return m_Faders; }
        }

        #endregion
        public override void GotoColor(Color color)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.GotoColor(color);
            }
        }

        public override void GotoColor(Color color, bool hidePointer = true)
		{
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.GotoColor(color, hidePointer);
            }
		}
        public override void GotoBlack()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.GotoBlack();
            }
        }
        public override void GotoBlack(bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                Debug.Log("Finding faders");
                var fader = m_Faders[i];

                Debug.Log("Is Active Fade");
                if (!IsActiveFader(fader))
                    continue;

                Debug.Log(fader.GetType());
                Debug.Log("Actually go to black");
                fader.GotoBlack(hidePointer);
            }
        }
        public override void GotoClear()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.GotoClear();
            }
        }
        public override void GotoClear(bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.GotoClear(hidePointer);
            }
        }
        public override void FadeToBlack()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToBlack();
            }
        }
        public override void FadeToBlack(bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToBlack(hidePointer);
            }
        }

        public override void FadeToBlack(float duration)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToBlack(duration);
            }
        }
        
        public override void FadeToBlack(float duration, bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToBlack(duration, hidePointer);
            }
        }
        public override void FadeToClear()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClear();
            }
        }
        public override void FadeToClear(bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClear(hidePointer);
            }
        }
        public override void FadeToClear(float duration)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClear(duration);
            }
        }
        public override void FadeToClear(float duration, bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClear(duration, hidePointer);
            }
        }
        public override void FadeToClearFromBlack()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClearFromBlack();
            }
        }
        public override void FadeToClearFromBlack(bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClearFromBlack(hidePointer);
            }
        }
        public override void FadeToClearFromBlack(float duration)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClearFromBlack(duration);
            }
        }
        public override void FadeToClearFromBlack(float duration, bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeToClearFromBlack(duration, hidePointer);
            }
        }
        public override void FadeTo(Color color)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeTo(color);
            };
        }
        public override void FadeTo(Color color, bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeTo(color, hidePointer);
            };
		}
        public override void FadeTo(Color color, float duration)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader) || !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeTo(color, duration);
            }
        }
        public override void FadeTo(Color color, float duration, bool hidePointer = true)
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader)|| !fader.gameObject.activeInHierarchy)
                    continue;

                fader.FadeTo(color, duration, hidePointer);
            }
        }

		public override void StopFade()
        {
            for (int i = 0; i < m_Faders.Count; ++i)
            {
                var fader = m_Faders[i];
                if (!IsActiveFader(fader))
                    continue;

                fader.StopFade();
            }
        }

		public override IEnumerator WaitUntilFadeComplete()
        {
            while (IsFading)
                yield return null;

            yield break;
        }
        
        private bool IsActiveFader(ScreenFader fader)
        {
            return (fader != null && fader.enabled);// && fader.gameObject.activeInHierarchy);
        }
    }
}