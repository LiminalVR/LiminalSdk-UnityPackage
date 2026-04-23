using Liminal.Core.Fader;
using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars.Extensions;
using Liminal.SDK.VR.EventSystems;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// An event handling delegate with a single <see cref="IVRAvatar"/> argument.
    /// </summary>
    /// <param name="avatar">The <see cref="IVRAvatar"/> the event relates to.</param>
    public delegate void VRAvatarEventHandler(IVRAvatar avatar);

    /// <summary>
    /// A full, concrete implementation of <see cref="IVRAvatar"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Avatar Body")]
    public class VRAvatar : MonoBehaviour, IVRAvatar
    {
        #region Static

        private static IVRAvatar _active;

        /// <summary>
        /// Gets the current active <see cref="IVRAvatar"/>.
        /// </summary>
        public static IVRAvatar Active
        {
            get { return _active; }
            private set
            {
                if (value != _active)
                {
                    _active = value;

                    if (AvatarChanged != null)
                        AvatarChanged(_active);
                }
            }
        }
        
        /// <summary>
        /// Raised when the active avatar has changed.
        /// </summary>
        public static event VRAvatarEventHandler AvatarChanged;

        #endregion

        private readonly List<IVRAvatarExtension> mExtensions = new List<IVRAvatarExtension>();
        private readonly List<IVRAvatarLimb> mLimbs = new List<IVRAvatarLimb>();
        private readonly List<IVRAvatarHand> mHands = new List<IVRAvatarHand>();
        private IScreenFader mScreenFader;

        [Header("Components")]
        [Tooltip("The head limb of the avatar. This contains the main VR device camera(s).")]
        [SerializeField] private VRAvatarHead m_Head = null;
        [Tooltip("The primary hand limb of the avatar.")]
        [SerializeField] private VRAvatarHand m_PrimaryHand = null;
        [Tooltip("The secondary hand limb of the avatar.")]
        [SerializeField] private VRAvatarHand m_SecondaryHand = null;
        [Tooltip("The container for auxiliary avatar systems.")]
        [SerializeField] private Transform m_Auxiliaries = null;

        #region Properties

        /// <summary>
        /// Indicates if the avatar is currently active.
        /// </summary>
        public bool IsActive
        {
            get { return gameObject.activeSelf; }
        }

        /// <summary>
        /// Gets the avatar's transform.
        /// </summary>
        public Transform Transform
        {
            get { return transform; }
        }

        /// <summary>
        /// Gets the container for auxiliary systems.
        /// </summary>
        public Transform Auxiliaries
        {
            get
            {
                if (m_Auxiliaries == null)
                {
                    m_Auxiliaries = new GameObject("Auxiliaries") { hideFlags = HideFlags.NotEditable }.transform;
                    m_Auxiliaries.SetParentAndIdentity(transform);
                }

                return m_Auxiliaries;
            }
        }

        /// <summary>
        /// Gets the Head limb.
        /// </summary>
        public IVRAvatarHead Head
        {
            get { return m_Head; }
        }
        
        /// <summary>
        /// Gets the primary hand limb.
        /// </summary>
        public IVRAvatarHand PrimaryHand
        {
            get { return m_PrimaryHand; }
        }

        /// <summary>
        /// Gets the secondary hand limb.
        /// </summary>
        public IVRAvatarHand SecondaryHand
        {
            get { return m_SecondaryHand; }
        }

        /// <summary>
        /// Gets a list of all hand limbs.
        /// </summary>
        public IList<IVRAvatarHand> Hands
        {
            get { return mHands; }
        }

        /// <summary>
        /// Gets a list of all limbs.
        /// </summary>
        public IList<IVRAvatarLimb> Limbs
        {
            get { return mLimbs; }
        }

        /// <summary>
        /// Gets the list of all extension assigned to the avatar.
        /// </summary>
        public IList<IVRAvatarExtension> Extensions
        {
            get { return mExtensions; }
        }

        /// <summary>
        /// Gets the screen fader for the avatar.
        /// </summary>
        public IScreenFader ScreenFader
        {
            get
            {
                if (mScreenFader == null)
                    AssignScreenFader();

                return mScreenFader;
            }
        }

        /// <summary>
        /// Gets the forward looking direction vector for the avatar. This is a shortcut to the head's active eye camera forward.
        /// </summary>
        public Vector3 LookForward
        {
            get
            {
                var camera = m_Head.ActiveEyeCamera;
                if (camera == null)
                    return Vector3.zero;

                return camera.transform.forward;
            }
        }

        /// <summary>
        /// Gets the looking rotation for the avatar. This is a shortcut to the head's active eye camera rotation.
        /// </summary>
        public Quaternion LookRotation
        {
            get
            {
                var camera = m_Head.ActiveEyeCamera;
                if (camera == null)
                    return Quaternion.identity;

                return camera.transform.rotation;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (m_Head != null)
                mLimbs.Add(m_Head);

            if (m_PrimaryHand != null)
            {
                mLimbs.Add(m_PrimaryHand);
                mHands.Add(m_PrimaryHand);
            }

            if (m_SecondaryHand != null && (m_SecondaryHand != m_PrimaryHand))
            {
                mLimbs.Add(m_SecondaryHand);
                mHands.Add(m_SecondaryHand);
            }
        }

        private void Start()
        {
            SetupEventSystem();
        }

        private void OnEnable()
        {
            if (Active != null && !ReferenceEquals(Active, this))
            {
                Debug.LogWarning("Only one VRAvatar should be active at any given time.", Active.Transform.gameObject);
                Active.Transform.gameObject.SetActive(false);
            }

            Active = this;
        }

        private void OnDisable()
        {
            if (Active != null && ReferenceEquals(Active, this))
                Active = null;
        }

        private void Update()
        {
            for (int i = 0; i < mLimbs.Count; ++i)
            {
                var limb = mLimbs[i];
                if (limb != null)
                    limb.UpdateState();
            }
        }

        #endregion

        /// <summary>
        /// Initializes all avatar extensions.
        /// </summary>
        public void InitializeExtensions()
        {
            // Initialize extensions
            GetComponents(mExtensions);
            foreach (var extension in mExtensions)
            {
                if (extension != null)
                {
                    extension.Initialize(this);
                }
            }
        }

        /// <summary>
        /// Gets the limb for the specified <see cref="VRAvatarLimbAlias"/> alias.
        /// </summary>
        /// <param name="alias">The limb alias.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> for the specified alias, or null if no limb is available for the supplied alias value.</returns>
        public IVRAvatarLimb GetLimb(VRAvatarLimbAlias alias)
        {
            switch (alias)
            {
                case VRAvatarLimbAlias.Head:
                    return m_Head;

                case VRAvatarLimbAlias.PrimaryHand:
                    return m_PrimaryHand;

                case VRAvatarLimbAlias.SecondaryHand:
                    return m_SecondaryHand;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the first <see cref="IVRAvatarLimb"/> of the specified type attached to the avatar.
        /// </summary>
        /// <param name="type">The <see cref="VRAvatarLimbType"/> of the limb to retrieve.</param>
        /// <returns>The first <see cref="IVRAvatarLimb"/> of the specified type.</returns>
        public IVRAvatarLimb GetLimb(VRAvatarLimbType type)
        {
            for (int i = 0; i < mLimbs.Count; ++i)
            {
                var limb = mLimbs[i];
                if (limb == null)
                    continue;

                if (limb.LimbType == type)
                    return limb;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="IVRAvatarLimb"/> assigned to the specified <see cref="IVRDeviceComponent"/>.
        /// </summary>
        /// <param name="deviceComponent">The VR device component of the limb to retrieve.</param>
        /// <returns>The <see cref="IVRAvatarLimb"/> assigned to the specified <see cref="IVRDeviceComponent"/>.</returns>
        public IVRAvatarLimb GetLimb(IVRDeviceComponent deviceComponent)
        {
            if (deviceComponent == null)
                return null;

            for (int i = 0; i < mLimbs.Count; ++i)
            {
                var limb = mLimbs[i];
                if (limb.DeviceComponent == deviceComponent)
                    return limb;
            }

            return null;
        }

        /// <summary>
        /// Indicates if the avatar has a <see cref="IVRAvatarLimb"/> of the specified <see cref="VRAvatarLimbType"/> type.
        /// </summary>
        /// <param name="type">The <see cref="VRAvatarLimbType"/> of the limb to search for.</param>
        /// <returns>A boolean indicating if the avatar has a <see cref="IVRAvatarLimb"/> of the specified <see cref="VRAvatarLimbType"/> type.</returns>
        public bool HasLimb(VRAvatarLimbType type)
        {
            for (int i = 0; i < mLimbs.Count; ++i)
            {
                var limb = mLimbs[i];
                if (limb == null)
                    continue;

                if (limb.LimbType == type)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the active state for the hand limbs.
        /// </summary>
        /// <param name="activeState">The active state for the hand limbs.</param>
        public void SetHandsActive(bool activeState)
        {
            if (m_SecondaryHand != null)
                m_SecondaryHand.gameObject.SetActive(activeState);

            if (m_PrimaryHand != null)
                m_PrimaryHand.gameObject.SetActive(activeState);
        }

        /// <summary>
        /// Sets the active state for all limbs of the specified <see cref="VRAvatarLimbType"/>.
        /// </summary>
        /// <param name="limbType">The <see cref="VRAvatarLimbType"/>.</param>
        /// <param name="activeState">The active state for all limbs of the supplied type.</param>
        public void SetLimbActiveState(VRAvatarLimbType limbType, bool activeState)
        {
            foreach (var limb in Limbs)
            {
                if ((limb != null) && (limb.LimbType == limbType))
                {
                    limb.SetActive(activeState);
                }
            }
        }

        /// <summary>
        /// Sets the active state for the avatar.
        /// </summary>
        /// <param name="activeState">The active state to set.</param>
        public void SetActive(bool activeState)
        {
            gameObject.SetActive(activeState);
        }

        /// <summary>
        /// Get the first extension of the specified type, or null if no extension exists of this type.
        /// </summary>
        /// <typeparam name="TExtension">The extension of the specified type.</typeparam>
        /// <returns>The extension of the specified type, or null if no extension exists of this type.</returns>
        public TExtension GetExtension<TExtension>() where TExtension : IVRAvatarExtension
        {
            foreach (var ext in mExtensions)
            { 
                if ((ext != null) && typeof(TExtension).IsAssignableFrom(ext.GetType()))
                    return (TExtension)ext;
            }

            return default(TExtension);
        }

        /// <summary>
        /// Gets all extensions of the specified type and places them into the supplied list and returns the number of objects that were added to the list.
        /// The list is cleared before any objects are added.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extensions to add.</typeparam>
        /// <param name="list">The list to add the extensions to.</param>
        /// <returns>The number of objects that were added to the list.</returns>
        public int GetExtensions<TExtension>(IList<TExtension> list) where TExtension : IVRAvatarExtension
        {
            if (list == null)
                throw new ArgumentNullException("list");

            int c = 0;
            foreach (var ext in mExtensions)
            {
                if ((ext != null) && typeof(TExtension).IsAssignableFrom(ext.GetType()))
                {
                    list.Add((TExtension)ext);
                    c++;
                }
            }

            return c;
        }

        /// <summary>
        /// Get the first extension of the specified type, or null if no extension exists of this type.
        /// </summary>
        /// <param name="type">The type of the extension.</param>
        /// <returns>The extension of the specified type, or null if no extension exists of this type.</returns>
        public IVRAvatarExtension GetExtension(Type type)
        {
            foreach (var ext in mExtensions)
            {
                if ((ext != null) && type.IsAssignableFrom(ext.GetType()))
                    return ext;
            }

            return null;
        }

        /// <summary>
        /// Gets all extensions of the specified type and places them into the supplied list and returns the number of objects that were added to the list.
        /// The list is cleared before any objects are added.
        /// </summary>
        /// <param name="type">The type of the extensions to add.</param>
        /// <param name="list">The list to add the extensions to.</param>
        /// <returns>The number of objects that were added to the list.</returns>
        public int GetExtensions(Type type, IList<IVRAvatarExtension> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            int c = 0;
            foreach (var ext in mExtensions)
            {
                if ((ext != null) && type.IsAssignableFrom(ext.GetType()))
                {
                    list.Add(ext);
                    c++;
                }
            }

            return c;
        }

        private void SetupEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                eventSystem.sendNavigationEvents = true;
            }

            if (m_Auxiliaries != null)
                eventSystem.transform.SetParentAndIdentity(m_Auxiliaries);

            VRPointerInputModule inputModule = null;
            foreach (var module in eventSystem.gameObject.GetComponents<BaseInputModule>())
            {
                if (module is VRPointerInputModule)
                {
                    inputModule = (VRPointerInputModule)module;
                }
                else if (!(module is StandaloneInputModule))
                {
                    Destroy(module);
                }
            }

            // Attach a VRPointerInputModule if required
            if (inputModule == null)
                inputModule = eventSystem.gameObject.AddComponent<VRPointerInputModule>();

            // Update the event system
            eventSystem.UpdateModules();
        }

        private void AssignScreenFader()
        {
            // It is possible the avatar is setup to use per-eye cameras, so in that case two
            // faders would be required (one for each eye) - the compound fader can fade multipler IScreenFader
            // instances simultaneously with a single operations.
            var fader = GetComponent<CompoundScreenFader>();
            if (fader == null)
            {
                // No existing fader found on the avatar
                // Create one and assign all ScreenFader components within the avatar
                fader = gameObject.AddComponent<CompoundScreenFader>();
                gameObject.GetComponentsInChildren(true, fader.Faders);
            }

            // Active avatar fader is always the singleton fader!
            fader.IsSingleton = true;
            mScreenFader = fader;
        }
    }
}
