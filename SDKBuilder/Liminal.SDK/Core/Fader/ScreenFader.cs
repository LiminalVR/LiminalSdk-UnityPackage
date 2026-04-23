using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Liminal.Core.Fader
{
	public abstract class ScreenFader : MonoBehaviour, IScreenFader
    {
		#region Static

		private static IScreenFader _instance;

        /// <summary>
        /// Gets the singleton <see cref="IScreenFader"/> instance that is currently active.
        /// </summary>
		public static IScreenFader Instance
		{
			get { return _instance; }
		}

		#endregion
        
        [SerializeField] private bool m_Singleton = false;

        #region Properties
        
        /// <summary>
        /// Indicates if this fader is the Singleton <see cref="IScreenFader"/>.
        /// </summary>
        public bool IsSingleton
        {
            get { return m_Singleton; }
            set
            {
                m_Singleton = value;
                PrepareSingleton();
            }
        }

        /// <summary>
        /// Indicates if a fade is currently in progress.
        /// </summary>
        public abstract bool IsFading
        {
            get;
        }
        
		#endregion

		#region MonoBehaviour

        private void OnEnable()
        {
            PrepareSingleton();
            OnEnabled();
        }

        private void OnDisable()
		{
			if (_instance != null && ReferenceEquals(_instance, this))
				_instance = null;
				
			OnDisabled();
		}

        #endregion
        public abstract void GotoColor(Color color);
        public abstract void GotoColor(Color color, bool hidePointer = true);
        public abstract void GotoBlack();
        public abstract void GotoBlack(bool hidePointer = true);
        public abstract void GotoClear();
        public abstract void GotoClear(bool hidePointer = true);
        public abstract void FadeToBlack();
        public abstract void FadeToBlack(bool hidePointer = true);
        public abstract void FadeToBlack(float duration);
        public abstract void FadeToBlack(float duration, bool hidePointer = true);
        public abstract void FadeToClear();
        public abstract void FadeToClear(bool hidePointer = true);
        public abstract void FadeToClear(float duration);
        public abstract void FadeToClear(float duration, bool hidePointer = true);
        public abstract void FadeToClearFromBlack();
        public abstract void FadeToClearFromBlack(bool hidePointer = true);
        public abstract void FadeToClearFromBlack(float duration);
        public abstract void FadeToClearFromBlack(float duration, bool hidePointer = true);
        public abstract void FadeTo(Color color);
        public abstract void FadeTo(Color color, bool hidePointer = true);
        public abstract void FadeTo(Color color, float duration);
        public abstract void FadeTo(Color color, float duration, bool hidePointer = true);
        public abstract void StopFade();
        public abstract IEnumerator WaitUntilFadeComplete();
		protected virtual void OnEnabled() { }
		protected virtual void OnDisabled() { }

        private void PrepareSingleton()
        {
            if (m_Singleton)
            {
                if (gameObject.activeInHierarchy)
                {
                    if (_instance != null && !ReferenceEquals(_instance, this))
                    {
                        enabled = false;
                        return;
                    }

                    _instance = this;
                }
            }
            else
            {
                if (ReferenceEquals(_instance, this))
                    _instance = null;
            }
        }
    }
}