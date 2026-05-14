using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
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

        public LiminalControllerManager ControllerManager => SpawnedRig != null ? SpawnedRig.GetComponentInChildren<LiminalControllerManager>(true) : null;

        [Header("Rig")]

        public XROrigin XROrigin => SpawnedRig != null ? SpawnedRig.XROrigin : null;
        public GameObject XRRig => SpawnedRig != null ? SpawnedRig.XRRig : null;
        public Transform RigRightHand => SpawnedRig != null ? SpawnedRig.RigRightHand : null;
        public Transform RigLeftHand => SpawnedRig != null ? SpawnedRig.RigLeftHand : null;
        public Transform CameraOffset => SpawnedRig != null ? SpawnedRig.CameraOffset : null;
        public TrackedPoseDriver OriginalTrackedPoseDriver => SpawnedRig != null ? SpawnedRig.OriginalTrackedPoseDriver : null;
        public XRRayInteractor RightControllerRayInteractor => SpawnedRig != null ? SpawnedRig.RightControllerRayInteractor : null;
        public XRRayInteractor LeftControllerRayInteractor => SpawnedRig != null ? SpawnedRig.LeftControllerRayInteractor : null;

        [Header("Dynamic Rig")]
        [Tooltip("If assigned, SpawnRig() will Instantiate this prefab and assign its XRRigReferences. Leave null to use a rig already wired into RigReferences via the inspector.")]
        [SerializeField] private XRRigReferences _xrRigPrefab;
        
        public XRRigReferences SpawnedRig { get; private set; }

        [ContextMenu("Spawn Rig")]
        public XRRigReferences SpawnRig()
        {
            if(!IsLimapp)
                PlatformInstance = this;

            if (SpawnedRig != null)
                return SpawnedRig;

            if (_xrRigPrefab == null)
            {
                Debug.LogWarning("[ExperienceAppManager] SpawnRig called but no XR Rig prefab is assigned. Falling back to the rig already wired into RigReferences.", this);
                return null;
            }

            SpawnedRig = Instantiate(_xrRigPrefab, transform);
            if (SpawnedRig == null)
                Debug.LogError("[ExperienceAppManager] Spawned rig prefab has no XRRigReferences component on its root.", SpawnedRig);

            RegisterIntearctors();

            return SpawnedRig;
        }

        [ContextMenu("1 - Find References")]
        public void FindReferences()
        {
            // Picks up MyExperienceApp subclasses too — fall back to name lookup if no component is present.
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

            VRAvatar = FindAnyObjectByType<VRAvatar>(FindObjectsInactive.Include);

            AvatarLeftHand = VRAvatar.SecondaryHand as VRAvatarHand;
            AvatarRightHand = VRAvatar.PrimaryHand as VRAvatarHand;

            AvatarHead = VRAvatar.Head as VRAvatarHead;

            if(ControllerManager.AvatarCustomLeftHandMesh == null)
                ControllerManager.AvatarCustomLeftHandMesh = AvatarLeftHand.Anchor.gameObject;
                
            if(ControllerManager.AvatarCustomRightHandMesh == null)
                ControllerManager.AvatarCustomRightHandMesh = AvatarRightHand.Anchor.gameObject;

            SceneSetup();
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

        [ContextMenu("Setup Test")]
        public void Setup()
        {
            SpawnRig();

            if (ControllerManager != null)
                ControllerManager.ApplyDefaults();

            SceneSetup();

            DeviceManager.Initialize(new UnityXRDevice());

            if(ExperienceApp != null)
                ExperienceApp.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (AvatarRightHand == null || SpawnedRig == null)
                return;

            // Update and Sync controller positions.
            AvatarRightHand.transform.position = RigRightHand.transform.position;
            AvatarRightHand.transform.rotation = RigRightHand.transform.rotation;

            AvatarLeftHand.transform.position = RigLeftHand.transform.position;
            AvatarLeftHand.transform.rotation = RigLeftHand.transform.rotation;

            // Offset takes head position that was how we offset the head. 
            CameraOffset.transform.position = AvatarHead.transform.position;
            CameraOffset.transform.rotation = AvatarHead.transform.rotation;
        }
    }
}

// Need to make sure MyExperienceApp doesn't actually do anything anymore.

namespace Liminal.SDK
{
    public class GameObjectUtils
    {

        public static GameObject FindInactiveByName(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindInChildrenIncludingInactive(root.transform, name);
                    if (found != null)
                        return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildrenIncludingInactive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                var result = FindInChildrenIncludingInactive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
