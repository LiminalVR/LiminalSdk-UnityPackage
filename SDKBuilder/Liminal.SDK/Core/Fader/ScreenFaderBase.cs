using UnityEngine;
using System.Collections;
using System.Text;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Pointers;
using UnityEngine.UI;

namespace Liminal.Core.Fader
{
	public abstract class ScreenFaderBase : ScreenFader
    {
		private bool mFadeActive;
		private Coroutine mCurrentFade;
        
		[SerializeField] private float m_DefaultDuration = 2f;
		[SerializeField] private bool m_FadeOnStart = true;
		[SerializeField] private Color m_StartColor = Color.black;
        [SerializeField] private Color m_CurrentColor = Color.clear;

        private const int m_hiddenPointerRenderQueue = 3999;
        private const int m_VisiblePointerRenderQueue = 4100;

        #region Properties

        /// <summary>
        /// Indicates if a fade is currently in progress.
        /// </summary>
        public override bool IsFading
		{
			get { return mFadeActive; }
		}

		public Color CurrentColor
		{
			get { return m_CurrentColor; }
		}

		public float DefaultDuration
		{
			get { return m_DefaultDuration; }
			set { m_DefaultDuration = value; }
		}

		public bool FadeOnStart
		{
			get { return m_FadeOnStart; }
			set { m_FadeOnStart = value; }
		}

		public Color StartColor
		{
			get { return m_StartColor; }
			set { m_StartColor = value; }
		}

		#endregion

		#region MonoBehaviour

		private void Awake()
		{
			OnAwake();
		}

		private void Start()
		{
			if (m_FadeOnStart)
			{
				GotoColor(m_StartColor);
				FadeToClear();
			}

			OnStart();
		}

		protected virtual void OnValidate()
		{
			m_DefaultDuration = Mathf.Max(m_DefaultDuration, 0);
            ApplyColor(m_CurrentColor);
		}

        protected override void OnDisabled()
        {
            base.OnDisabled();
            mFadeActive = false;
        }

        #endregion
        public override void GotoColor(Color color)
        {
            StopFade();
            m_CurrentColor = color;
            ApplyColor(color);
        }
        public override void GotoColor(Color color, bool hidePointer = true)
		{
			StopFade();
			m_CurrentColor = color;
			ApplyColor(color);
		}
        public override void GotoBlack()
        {
            GotoColor(Color.black);
        }
        public override void GotoBlack(bool hidePointer = true)
		{
			GotoColor(Color.black, hidePointer);
		}
        public override void GotoClear()
        {
            GotoColor(Color.clear);
        }
        public override void GotoClear(bool hidePointer = true)
		{
			GotoColor(Color.clear, hidePointer);
		}
        public override void FadeToBlack()
        {
            FadeTo(Color.black, m_DefaultDuration);
        }
        public override void FadeToBlack(bool hidePointer = true)
        {
            FadeTo(Color.black, m_DefaultDuration, hidePointer);
		}
        public override void FadeToBlack(float duration)
        {
            FadeTo(Color.black, duration);
        }
        public override void FadeToBlack(float duration, bool hidePointer = true)
		{
			FadeTo(Color.black, duration, hidePointer);
		}
        public override void FadeToClear()
        {
            FadeTo(Color.clear, m_DefaultDuration);
        }
        public override void FadeToClear(bool hidePointer = true)
        {
			FadeTo(Color.clear, m_DefaultDuration, hidePointer);
		}
        public override void FadeToClear(float duration)
        {
            FadeTo(Color.clear, duration);
        }
        public override void FadeToClear(float duration, bool hidePointer = true)
        {
            FadeTo(Color.clear, duration, hidePointer);
        }
        public override void FadeToClearFromBlack()
        {
            GotoBlack();
            FadeTo(Color.clear);
        }
        public override void FadeToClearFromBlack(bool hidePointer = true)
		{
			GotoBlack();
			FadeTo(Color.clear, hidePointer);
		}
        public override void FadeToClearFromBlack(float duration)
        {
            GotoBlack();
            FadeTo(Color.clear, duration);
        }
        public override void FadeToClearFromBlack(float duration, bool hidePointer = true)
		{
			GotoBlack();
			FadeTo(Color.clear, duration, hidePointer);
		}
        public override void FadeTo(Color color)
        {
            FadeTo(color, m_DefaultDuration);
        }
        public override void FadeTo(Color color, bool hidePointer = true)
		{
			FadeTo(color, m_DefaultDuration, hidePointer);
		}
        public override void FadeTo(Color color, float duration)
        {
            StopFade();
            if (enabled)
            {
                mCurrentFade = StartCoroutine(DoFade(duration, color));
            }
        }
        public override void FadeTo(Color color, float duration, bool hidePointer = true)
		{
			StopFade();
			if (enabled)
			{
				mCurrentFade = StartCoroutine(DoFade(duration, color, hidePointer));
			}
		}

		public override void StopFade()
		{
			mFadeActive = false;

			if (mCurrentFade == null)
				return;

			StopCoroutine(mCurrentFade);
			mCurrentFade = null;
		}

		public override IEnumerator WaitUntilFadeComplete()
		{
			while (mFadeActive)
				yield return null;

			yield break;
		}

		private IEnumerator DoFade(float duration, Color targetColor, bool hidePointer = true)
		{
			mFadeActive = true;

            SetPointerRenderQueue(hidePointer ? m_hiddenPointerRenderQueue : m_VisiblePointerRenderQueue);

            if (duration > 0 && (targetColor != m_CurrentColor))
			{
				var startTime = Time.realtimeSinceStartup;
				var startColor = m_CurrentColor;
				while (true)
				{
					var t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
					m_CurrentColor = Color.Lerp(startColor, targetColor, t);
					ApplyColor(m_CurrentColor);

					if (t >= 1f)
						break;

					yield return null;
				}
			}

            if (!hidePointer)
            {
                SetPointerRenderQueue(m_VisiblePointerRenderQueue);
            }

            // Ensure the target color is set
            m_CurrentColor = targetColor;
			ApplyColor(m_CurrentColor);

			mFadeActive = false;
			yield break;
		}

        protected void SetPointerRenderQueue(int renderQueue)
        {
            var avatar = FindObjectOfType<VRAvatar>();
            var hands = avatar.GetComponentsInChildren<VRAvatarHand>(true);

            foreach (var hand in hands)
            {
                var rend = hand.GetComponentInChildren<LineRenderer>(true);

                if (rend == null)
                    continue;

                rend.material.renderQueue = renderQueue;
            }
        }

		protected abstract void ApplyColor(Color color);

		protected virtual void OnAwake() { }
		protected virtual void OnStart() { }
	}
}