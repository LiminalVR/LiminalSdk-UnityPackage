using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Unity.XR.CoreUtils;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Liminal.SDK.V2
{

    /// <summary>
    /// Replaces Experience App and load up VR.
    /// Properly map to VRAvatar / Hands Etc
    /// </summary>
    public class ExperienceAppManager : MonoBehaviour
    {
        public InputActionReference TestAction;

        public static ExperienceAppManager PlatformInstance;
        public static ExperienceAppManager LimappInstance;

        public bool IsLimapp;

        // This is the old experience content holder. We want EVERYTHING off so nothing looks for VRAvatar etc until it is all set up.
        [Header("Auto Find")]
        public ExperienceApp ExperienceApp;
        public VRAvatar VRAvatar;

        public bool RunOnStart = false;

        public VRAvatarHand AvatarLeftHand;
        public VRAvatarHand AvatarRightHand;
        public VRAvatarHead AvatarHead;

        public LiminalControllerManager ControllerManager;

        [Header("Rig")]

        public XROrigin XROrigin => SpawnedRig != null ? SpawnedRig.XROrigin : null;
        public GameObject XRRig => SpawnedRig != null ? SpawnedRig.XRRig : null;
        public Transform RigRightHand => SpawnedRig != null ? SpawnedRig.RigRightHand : null;
        public Transform RigLeftHand => SpawnedRig != null ? SpawnedRig.RigLeftHand : null;
        public Transform RigRightHandTracked => SpawnedRig != null ? SpawnedRig.RigRightHandTracked : null;
        public Transform RigLeftHandTracked => SpawnedRig != null ? SpawnedRig.RigLeftHandTracked : null;
        public Transform CameraOffset => SpawnedRig != null ? SpawnedRig.CameraOffset : null;
        public TrackedPoseDriver OriginalTrackedPoseDriver => SpawnedRig != null ? SpawnedRig.OriginalTrackedPoseDriver : null;
        public XRRayInteractor RightControllerRayInteractor => SpawnedRig != null ? SpawnedRig.RightControllerRayInteractor : null;
        public XRRayInteractor LeftControllerRayInteractor => SpawnedRig != null ? SpawnedRig.LeftControllerRayInteractor : null;
        
        public XRRigReferences SpawnedRig { get; private set; }

        [ContextMenu("Spawn Rig")]
        public XRRigReferences SpawnRig()
        {
            if(!IsLimapp)
                PlatformInstance = this;

            if (SpawnedRig != null)
                return SpawnedRig;

            var rigPrefab = Resources.Load<GameObject>("XR_Rig")?.GetComponent<XRRigReferences>();

            if (rigPrefab == null)
            {
                Debug.LogWarning("[ExperienceAppManager] SpawnRig called but no XR Rig prefab is assigned.", this);
                return null;
            }

            SpawnedRig = Instantiate(rigPrefab, transform);

            if (SpawnedRig == null)
                Debug.LogError("[ExperienceAppManager] Spawned rig prefab has no XRRigReferences component on its root.", SpawnedRig);

            RegisterIntearctors();

            return SpawnedRig;
        }

        [ContextMenu("1 - Find References")]
        public void FindReferences()
        {
            ControllerManager = GetComponentInChildren<LiminalControllerManager>(true);

            // Picks up MyExperienceApp subclasses too — fall back to name lookup if no component is present.
            if (ExperienceApp == null)
                ExperienceApp = FindAnyObjectByType<ExperienceApp>(FindObjectsInactive.Include);

            if (ExperienceApp == null)
            {
                var fallbackGo = GameObjectUtils.FindInactiveByName("[ExperienceApp]");
                if (fallbackGo != null)
                    ExperienceApp = fallbackGo.GetComponent<ExperienceApp>();
            }

            if (ExperienceApp == null)
            {
                Debug.LogWarning("[ExperienceAppManager] Could not find an ExperienceApp (component or [ExperienceApp] GameObject).", this);
            }

            if(VRAvatar == null){
                if(ExperienceApp != null)
                VRAvatar = ExperienceApp.GetComponentInChildren<VRAvatar>(true);

                if(VRAvatar == null)
                    VRAvatar = FindAnyObjectByType<VRAvatar>(FindObjectsInactive.Include);
            }

            AvatarLeftHand = VRAvatar.SecondaryHand as VRAvatarHand;
            AvatarRightHand = VRAvatar.PrimaryHand as VRAvatarHand;

            AvatarHead = VRAvatar.Head as VRAvatarHead;

            if(ControllerManager.AvatarCustomLeftHandMesh == null)
                ControllerManager.AvatarCustomLeftHandMesh = AvatarLeftHand.Anchor.gameObject;
                
            if(ControllerManager.AvatarCustomRightHandMesh == null)
                ControllerManager.AvatarCustomRightHandMesh = AvatarRightHand.Anchor.gameObject;
        }

        [ContextMenu("2 - Scene Setup")]
        public void SceneSetup()
        {
            XROrigin.Camera = AvatarHead.CenterEyeCamera;
            XROrigin.CameraFloorOffsetObject = AvatarHead.transform.gameObject;
            XROrigin.CameraYOffset = AvatarHead.transform.localPosition.y;

            CopyTrackedPoseDriverToCenterEye();

            VRAvatar.Auxiliaries.gameObject.SetActive(false);
        }

        [ContextMenu("Disable Rays")]
        public void TestDisableRays()
        {
            RightControllerRayInteractor.gameObject.SetActive(false);
            LeftControllerRayInteractor.gameObject.SetActive(false);
        }

        private void CopyTrackedPoseDriverToCenterEye()
        {
            if (OriginalTrackedPoseDriver == null)
            {
                Debug.LogWarning("[ExperienceAppManager] OriginalTrackedPoseDriver not assigned — skipping TrackedPoseDriver copy.", this);
                return;
            }

            if (AvatarHead == null || AvatarHead.CenterEyeCamera == null)
            {
                Debug.LogWarning("[ExperienceAppManager] AvatarHead.CenterEyeCamera missing — skipping TrackedPoseDriver copy.", this);
                return;
            }

            var centerEye = AvatarHead.CenterEyeCamera.gameObject;
            var driver = centerEye.GetComponent<TrackedPoseDriver>();
            if (driver == null)
                driver = centerEye.AddComponent<TrackedPoseDriver>();

            // JsonUtility round-trip copies all [SerializeField] state — same trick the editor uses internally.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(OriginalTrackedPoseDriver), driver);
        }

        private void Awake()
        {
            if(IsLimapp)
                LimappInstance = this;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if(RunOnStart)
                Setup();
        }

        private void OnEnable()
        {
            if (TestAction != null && TestAction.action != null)
                TestAction.action.performed += OnTestActionPerformed;
        }

        private void OnDisable()
        {
            if (TestAction != null && TestAction.action != null)
                TestAction.action.performed -= OnTestActionPerformed;
        }

        private void OnTestActionPerformed(InputAction.CallbackContext ctx)
        {
        }

        public void RegisterIntearctors()
        {
            if (SpawnedRig == null)
            {
                Debug.LogWarning("[ExperienceAppManager] RegisterIntearctors called with no SpawnedRig.", this);
                return;
            }

            var manager = SpawnedRig.GetComponentInChildren<XRInteractionManager>(true);
            if (manager == null)
            {
                Debug.LogWarning("[ExperienceAppManager] No XRInteractionManager found on the spawned rig — interactors won't be registered.", SpawnedRig);
                return;
            }

            var interactors = SpawnedRig.GetComponentsInChildren<IXRInteractor>(true);
            for (int i = 0; i < interactors.Length; i++)
                manager.RegisterInteractor(interactors[i]);
        }

        /// <summary>
        /// Toggle this manager and its avatar on/off. Also re-enables the shared XRI InputActionAsset
        /// (which gets Disable()d by the rig's InputActionManager on deactivation) and refreshes the
        /// rig's ray interactors on activation — they don't fully re-init from a GameObject SetActive
        /// cycle, leaving the laser line/reticle stale even though interactions still work.
        /// </summary>
        public void SetActive(bool state)
        {
            if (VRAvatar != null)
                VRAvatar.gameObject.SetActive(state);
            gameObject.SetActive(state);

            EnsureXRInputActionsEnabled();

            if (state && SpawnedRig != null)
                RefreshRayInteractors(SpawnedRig);
        }

        public static void EnsureXRInputActionsEnabled()
        {
            var refs = XRInputReferences.Instance;
            if (refs == null) return;

            var ctrlRefs = refs.RightControllerReferences;
            if (ctrlRefs == null) return;

            var backRef = ctrlRefs.Back;
            if (backRef == null) return;

            var asset = backRef.action?.actionMap?.asset;
            if (asset != null)
                asset.Enable();
        }

        public static void RefreshRayInteractors(XRRigReferences rig)
        {
            if (rig == null) return;

            if (rig.RightControllerRayInteractor != null)
            {
                rig.RightControllerRayInteractor.enabled = false;
                rig.RightControllerRayInteractor.enabled = true;
            }

            if (rig.LeftControllerRayInteractor != null)
            {
                rig.LeftControllerRayInteractor.enabled = false;
                rig.LeftControllerRayInteractor.enabled = true;
            }
        }

        [ContextMenu("Setup Test")]
        public void Setup()
        {
            FindReferences();
            SpawnRig();
            SceneSetup();

            if (ControllerManager != null)
                ControllerManager.ApplyDefaults();

            if(DeviceManager.Device == null)
                DeviceManager.Initialize(new UnityXRDevice());

            if(ExperienceApp != null)
                ExperienceApp.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (AvatarRightHand == null || SpawnedRig == null)
                return;

            var mode = XRInputModalityManager.currentInputMode.Value;
            var rightHand = mode == XRInputModalityManager.InputMode.TrackedHand
                ? SpawnedRig.RigRightHandTracked
                : SpawnedRig.RigRightHand;

            var leftHand = mode == XRInputModalityManager.InputMode.TrackedHand
                ? SpawnedRig.RigLeftHandTracked
                : SpawnedRig.RigLeftHand;

            // Update and Sync controller positions.
            AvatarRightHand.transform.position = rightHand.transform.position;
            AvatarRightHand.transform.rotation = rightHand.transform.rotation;

            AvatarLeftHand.transform.position = leftHand.transform.position;
            AvatarLeftHand.transform.rotation = leftHand.transform.rotation;

            // Offset takes head position that was how we offset the head.
            CameraOffset.transform.position = AvatarHead.transform.position;
            CameraOffset.transform.rotation = AvatarHead.transform.rotation;

            TriggerTestAction();
        }

        [ContextMenu("Trigger Test Action")]
        private void TriggerTestAction()
        {
            var action = TestAction != null ? TestAction.action : null;
            if (action == null)
            {
                Debug.LogWarning("[ExperienceAppManager] TestAction is not assigned.", this);
                return;
            }

            if (!action.enabled)
                action.Enable();

            StartCoroutine(PressAndRelease(action));
        }

        private static IEnumerator PressAndRelease(InputAction action)
        {
            WriteButton(action, 1f);
            yield return null;
            WriteButton(action, 0f);
        }

        private static void WriteButton(InputAction action, float value)
        {
            foreach (var control in action.controls)
            {
                if (control is not ButtonControl button || button.device == null)
                    continue;

                using (StateEvent.From(button.device, out var eventPtr))
                {
                    button.WriteValueIntoEvent(value, eventPtr);
                    InputSystem.QueueEvent(eventPtr);
                }
            }
        }
    }
}
