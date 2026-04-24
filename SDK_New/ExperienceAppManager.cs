using Liminal.SDK.VR.Avatars;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Liminal.SDK.V2
{
    /// <summary>
    /// Replaces Experience App and load up VR.
    /// Properly map to VRAvatar / Hands Etc
    /// </summary>
    public class ExperienceAppManager : MonoBehaviour
    {
        // This is the old experience content holder. We want EVERYTHING off so nothing looks for VRAvatar etc until it is all set up.
        public GameObject ExperienceAppObject;
        public GameObject XRRig;

        public bool RunOnStart = false;

        public Transform AvatarRightHand;
        public Transform RigRightHand;

        public Transform AvatarLeftHand;
        public Transform RigLeftHand;

        public Transform VRAvatar;

        public Transform CameraOffset;
        public Transform Head;

        private void Start()
        {
            if(RunOnStart)
                Setup();
        }

        [ContextMenu("Setup Test")]
        public void Setup()
        {
            //XRRig.transform.SetParent(VRAvatar.transform.parent);

            // Move the whole avatar into CameraOffset to maintain any offsets yet still able to apply offset!
            //VRAvatar.SetParent(CameraOffset);

            // Turn it on after it all.
            ExperienceAppObject.SetActive(true);
        }

        private void LateUpdate()
        {
            // Update and Sync controller positions.
            AvatarRightHand.transform.position = RigRightHand.transform.position;
            AvatarRightHand.transform.rotation = RigRightHand.transform.rotation;

            AvatarLeftHand.transform.position = RigRightHand.transform.position;
            AvatarLeftHand.transform.rotation = RigLeftHand.transform.rotation;

            // Offset takes head position that was how we offset the head. 
            CameraOffset.transform.position = Head.position;
        }
    }
}
