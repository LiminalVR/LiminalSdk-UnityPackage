using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.XR;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [ContextMenu("1 - Find References")]
        public void FindReferences()
        {
            var oldExperienceApp = FindAnyObjectByType<ExperienceApp>(FindObjectsInactive.Include);
            if (oldExperienceApp != null)
            {
                // May want to remove component or replace it.
            }
            else
            {
                ExperienceApp = GameObjectUtils.FindInactiveByName("[ExperienceApp]");
            }

            VRAvatar = FindAnyObjectByType<VRAvatar>(FindObjectsInactive.Include);

            AvatarLeftHand = VRAvatar.SecondaryHand as VRAvatarHand;
            AvatarRightHand = VRAvatar.PrimaryHand as VRAvatarHand;

            AvatarHead = VRAvatar.Head as VRAvatarHead;
        }

        [ContextMenu("2 - Scene Setup")]
        public void SceneSetup()
        {
            // We hide the old exp app on purpose.
            ExperienceApp.SetActive(false);

            // Disabling as this component moves our EventSystem and causes Interaction issues.
            VRAvatar.enabled = false;

            XROrigin.Camera = AvatarHead.CenterEyeCamera;
            XROrigin.CameraFloorOffsetObject = AvatarHead.transform.gameObject;
        }

        private void Awake()
        {
            if(RunOnStart)
                Setup();
        }

        [ContextMenu("Setup Test")]
        public void Setup()
        {
            SceneSetup();
            DeviceManager.Initialize(new UnityXRDevice());
            ExperienceApp.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            // Update and Sync controller positions.
            AvatarRightHand.transform.position = RigRightHand.transform.position;
            AvatarRightHand.transform.rotation = RigRightHand.transform.rotation;

            AvatarLeftHand.transform.position = RigRightHand.transform.position;
            AvatarLeftHand.transform.rotation = RigLeftHand.transform.rotation;

            // Offset takes head position that was how we offset the head. 
            CameraOffset.transform.position = AvatarHead.transform.position;
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
