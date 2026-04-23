using Liminal.SDK.Extensions;
using Liminal.SDK.Serialization;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.Core
{
    /// <summary>
    /// The entry-point for an Experience App. A GameObject with this component is required to be at the root of your scene, and all your scene objects should be nested below it.
    /// The master app will search for this component when initializing your app. If it is not found or incorrectly configured, your experience app will not work.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class ExperienceApp : MonoBehaviour
    {
        /// <summary>
        /// Contains settings for controlling a full screen fade.
        /// </summary>
        [Serializable]
        public class FadeSettings
        {
            [Tooltip("Should this fade occur.")]
            [SerializeField] private bool m_Fade = true;
            [Tooltip("The color of the fade.")]
            [SerializeField] private Color m_Color = Color.black;
            [Tooltip("The duration of the fade.")]
            [SerializeField] private float m_Duration = 2f;

            public bool DoFade
            {
                get { return m_Fade; }
            }

            public Color Color
            {
                get { return m_Color; }
            }

            public float Duration
            {
                get { return m_Duration; }
            }
        }

        /// <summary>
        /// The current pause state of the experience.
        /// </summary>
        public static bool Paused { get; protected set; }

        /// <summary>
        /// Pauses the experience using timeScale, can be overriden by developers to provide unique functionality.
        /// </summary>
        public virtual void Pause()
        {
            if (Paused)
                return;

            var sceneRoots = gameObject.scene.GetRootGameObjects();
            if (sceneRoots != null && sceneRoots.Length > 0)
            {
                _audioSources = new List<AudioSource>();
                foreach (var root in sceneRoots)
                {
                    _audioSources.AddRange(root.GetComponentsInChildren<AudioSource>().Where(x => x.isPlaying));
                }

                if (_audioSources != null && _audioSources.Count > 0)
                {
                    foreach (var source in _audioSources)
                    {
                        if (source != null)
                            source.Pause();
                    }
                }
            }

            Time.timeScale = 0;
            Paused = true;
        }

        /// <summary>
        /// Resumes the experience using timeScale, can be overriden by developers to provide unique functionality.
        /// </summary>
        public virtual void Resume()
        {
            if (!Paused)
                return;

            if (_audioSources != null && _audioSources.Count > 0)
            {
                foreach (var source in _audioSources)
                {
                    if (source != null)
                    {
                        source.Play();
                    }
                }
            }

            Time.timeScale = 1;
            Paused = false;
        }

        #region Static

        private static bool _isEmulator = true;
        private static bool _isEnding = false;
        private static AssetBundle _assetBundle = null;

        /// <summary>
        /// Gets the current <see cref="ExperienceApp"/> instance.
        /// </summary>
        private static ExperienceApp Instance
        {
            get; set;
        }

        /// <summary>
        /// Gets the <see cref="UnityEngine.AssetBundle"/> that the experience was loaded from.
        /// </summary>
        public static AssetBundle AssetBundle
        {
            get { return _assetBundle; }
        }

        /// <summary>
        /// Indicates if the application is running as an emulator. This will be true whenever you are running the app inside
        /// the Unity editor during development, and false whenever the app is running inside the Liminal app.
        /// </summary>
        public static bool IsEmulator
        {
            get { return _isEmulator; }
        }

        /// <summary>
        /// Indicates if the application is in an ending state and shutting down.
        /// </summary>
        public static bool IsEnding
        {
            get { return _isEnding; }
        }

        public static event Action OnInitializeBegin;

        /// <summary>
        /// Raised when the <see cref="ExperienceApp"/> is initializing.
        /// </summary>
        public static event Action Initializing;

        /// <summary>
        /// Raised when the <see cref="ExperienceApp"/> is shutting down.
        /// </summary>
        public static event Action ShuttingDown;

        /// <summary>
        /// Raised when <see cref="ExperienceApp"/> is completed
        /// </summary>
        public static event Action<bool> OnComplete;

        /// <summary>
        /// The default call experience end call for Platform App v1.2.0 where by default completed is true.
        /// </summary>
        public static void End()
        {
            End(true);
        }

        public virtual void EndExperience() => End(true);

        /// <summary>
        /// Ends the experience and returns to the Liminal app.
        /// </summary>
        public static void End(bool completed)
        {
            if (Instance == null)
            {
                Debug.LogError("No ExperienceApp is activate.");
                return;
            }

            Instance.InternalEnd();
            _isEnding = true;

            OnComplete?.Invoke(completed);
        }
        #endregion

        private List<GameObject> mToAwake;
        private IVRAvatar mAvatar;
        private bool mInitialized;
        private bool mShutdown;

        [SerializeField, HideInInspector] private TextAsset m_AppData = null;
        [SerializeField, HideInInspector] private AssetLookup m_AssetLookup = null;
        [SerializeField, HideInInspector] private List<GameObject> m_RootGameObjects = null;
        [Tooltip("Settings for the fade in transition when the app begins.")]
        [SerializeField] private FadeSettings m_FadeIn = new FadeSettings();
        [Tooltip("Settings for the fade out transition when the app ends.")]
        [SerializeField] private FadeSettings m_FadeOut = new FadeSettings();


        #region Properties

        /// <summary>
        /// Gets the settings for the fade in transition when the app begins.
        /// </summary>
        public FadeSettings FadeInSettings
        {
            get { return m_FadeIn; }
        }

        /// <summary>
        /// Gets the settings for the fade out transition when the app ends.
        /// </summary>
        public FadeSettings FadeOutSettings
        {
            get { return m_FadeOut; }
        }

        /// <summary>
        /// Gets the list of GameObjects on the root of the scene.
        /// </summary>
        public List<GameObject> RootGameObjects
        {
            get { return m_RootGameObjects; }
        }

        /// <summary>
        /// Stores the Limapp config for an experience as well as teh temporary config for the platform settings.
        /// </summary>
        public LiminalConfig LimappConfig = new LiminalConfig();

        private List<AudioSource> _audioSources;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            Debug.Log("[Experience App] Awake");
            Debug.Log("[Experience App] Awake 2");

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Disable child objects that are currently active
            mToAwake = new List<GameObject>();
            for (int i = 0; i < transform.childCount; ++i)
            {
                var go = transform.GetChild(i).gameObject;
                if (go.activeSelf && go != gameObject)
                {
                    go.SetActive(false);
                    mToAwake.Add(go);
                }
            }

            if (_isEmulator)
            {
                m_RootGameObjects = gameObject.scene.GetRootGameObjects().ToList();
            }
            else
            {
                Debug.Log("[Experience App] Deserializing");

                // Deactivate, wait for the master app to initialize us
                // At this point we are effectively paused...
                var deserializer = new AppDeserializer(m_AssetLookup);
                deserializer.Deserialize(m_RootGameObjects, m_AppData);

                Debug.Log("[Experience App] Deserialized");


                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Execute shutdown when running as an emulator
            // This will ensure that the app is shutdown as it would in a live environment, and will suppress the
            // warning message below
            if (_isEmulator)
                Shutdown();

            if (!mShutdown)
                Debug.LogError("ExperienceApp was destroyed before Shutdown() was called. Was it destroyed by accident?");

            if (Instance == this)
                Instance = null;

            Initializing = null;
            ShuttingDown = null;
            m_RootGameObjects.Clear();
        }

        private IEnumerator Start()
        {
            if (_isEmulator)
            {
                yield return Initialize();
            }
        }

        #endregion
        
        private IEnumerator Initialize()
        {
            if (mInitialized)
                yield break;

            OnInitializeBegin?.Invoke();

            gameObject.SetActive(true);

            yield return InitializeVRDevice();
            yield return SetupAvatar();
            mAvatar.ScreenFader.GotoBlack(hidePointer: true);
            mAvatar.ScreenFader.GotoBlack();

            Debug.Log("Experience app running from SDK");
            //LimappConfig?.Apply();

            if (m_FadeIn != null && m_FadeIn.DoFade)
            {
                var color = m_FadeIn.Color; color.a = 1f;
                mAvatar.ScreenFader.FadeTo(color, _isEmulator ? 0f : 1f);
                yield return mAvatar.ScreenFader.WaitUntilFadeComplete();
            }

            AwakenChildren();

            if (Initializing != null)
                Initializing();

            mInitialized = true;

            // Fade in
            yield return null;

            var duration = (m_FadeIn != null) ? Mathf.Max(m_FadeIn.Duration, 0f) : 1f;
            if (m_FadeIn != null && m_FadeIn.DoFade)
                mAvatar.ScreenFader.FadeToClear(duration);
        }

        public IEnumerator InitializePublic()
        {
            if (mInitialized)
                yield break;

            Debug.Log("[Experience App] initialize Begin");
            OnInitializeBegin?.Invoke();

            Debug.Log("[Experience App] On");
            gameObject.SetActive(true);

            Debug.Log("[Experience App] Initialize Device");
            yield return InitializeVRDevice();

            Debug.Log("[Experience App] Setup Avatar");
            yield return SetupAvatar();

            Debug.Log("[Experience App] Go Black");
            mAvatar.ScreenFader.GotoBlack(hidePointer: true);
            mAvatar.ScreenFader.GotoBlack();

            LimappConfig?.Apply();
            
            if (m_FadeIn != null)
            {
                var color = m_FadeIn.Color; color.a = 1f;
                mAvatar.ScreenFader.FadeTo(color, _isEmulator ? 0f : 1f);
                yield return mAvatar.ScreenFader.WaitUntilFadeComplete();
            }

            Debug.Log("[Experience App] Awaken Children");
            AwakenChildren();

            if (Initializing != null)
                Initializing();

            mInitialized = true;

            // Fade in
            yield return null;

            var duration = (m_FadeIn != null) ? Mathf.Max(m_FadeIn.Duration, 0f) : 1f;
            mAvatar.ScreenFader.FadeToClear(duration);
        }


        private IEnumerator InitializeVRDevice()
        {
            // When running inside the master app, the VRDevice will already have been initialized
            // so we don't need to do anything here in this case - this ensures that VREmulator
            // will not try and and take control and run an emulator device!
            if (!_isEmulator)
                yield break;

            var deviceInitializer = GetComponent<IVRDeviceInitializer>();
            var device = deviceInitializer.CreateDevice();
            VRDevice.Initialize(device);
        }

        private IEnumerator SetupAvatar()
        {
            mAvatar = GetComponentInChildren<IVRAvatar>(includeInactive: true);
            VRDevice.Device.SetupAvatar(mAvatar);
            yield break;
        }

        private void InternalEnd()
        {
            Debug.Log("[ExperienceApp] Ending");

            // Deactivate the avatar hands
            if (mAvatar != null)
                mAvatar.SetHandsActive(false);
        }

        private void Shutdown()
        {
            if (mShutdown)
                return;

            Debug.Log("[ExperienceApp] Shutting down...");

            mShutdown = true;
            Initializing = null;

            //LimappConfig?.Release();

            if (ShuttingDown != null)
                ShuttingDown();

            Debug.Log("[ExperienceApp] Shutdown");

        }

        private void AwakenChildren()
        {
            // Enable all child objects
            foreach (var go in mToAwake)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }

            mToAwake.Clear();
            mToAwake = null;
        }
    }
}
