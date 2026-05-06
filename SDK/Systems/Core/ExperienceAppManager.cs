using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
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
        // This is the old experience content holder. We want EVERYTHING off so nothing looks for VRAvatar etc until it is all set up.
        [Header("Auto Find")]
        public GameObject ExperienceApp;
        public VRAvatar VRAvatar;

        public XROrigin XROrigin;
        public GameObject XRRig;

        public bool RunOnStart = false;

        public VRAvatarHand AvatarLeftHand;
        public VRAvatarHand AvatarRightHand;
        public VRAvatarHead AvatarHead;

        [Header("Assign These")]
        public Transform RigRightHand;
        public Transform RigLeftHand;
        public Transform CameraOffset;

        public TrackedPoseDriver OriginalTrackedPoseDriver;

        public XRRayInteractor RightControllerRayInteractor;
        public XRRayInteractor LeftControllerRayInteractor;

        public LiminalControllerManager ControllerManager;

        [ContextMenu("1 - Find References")]
        public void FindReferences()
        {
            // Picks up MyExperienceApp subclasses too — fall back to name lookup if no component is present.
            var oldExperienceApp = FindAnyObjectByType<ExperienceApp>(FindObjectsInactive.Include);
            ExperienceApp = oldExperienceApp != null
                ? oldExperienceApp.gameObject
                : GameObjectUtils.FindInactiveByName("[ExperienceApp]");

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
            // We hide the old exp app on purpose.
            ExperienceApp.SetActive(false);

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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if(RunOnStart)
                Setup();
        }

        [ContextMenu("Setup Test")]
        public void Setup()
        {
            if (ControllerManager != null)
                ControllerManager.ApplyDefaults();

            SceneSetup();
            DeviceManager.Initialize(new UnityXRDevice());
            ExperienceApp.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
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
