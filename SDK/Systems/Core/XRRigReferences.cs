using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Liminal.SDK.V2
{
    /// <summary>
    /// Dumb data holder that lives on an XR rig prefab's root. Bundles the references a spawner
    /// (<see cref="ExperienceAppManager"/>, the platform simulator, the back-button menu, etc.)
    /// needs after instantiating the prefab, so each spawner doesn't re-walk the hierarchy with
    /// its own ad-hoc finds.
    /// </summary>
    public class XRRigReferences : MonoBehaviour
    {
        public XRUIInputModule XRUIInputModule;
        public TrackedDevicePhysicsRaycaster TrackedDevicePhysicsRaycaster;

        public XROrigin XROrigin;
        public GameObject XRRig;

        public Transform RigRightHand;
        public Transform RigLeftHand;

        public Transform RigRightHandTracked;
        public Transform RigLeftHandTracked;

        public Transform CameraOffset;

        public Transform RigRightHandVisual;
        public Transform RigLeftHandVisual;


        public TrackedPoseDriver OriginalTrackedPoseDriver;

        public XRRayInteractor RightControllerRayInteractor;
        public XRRayInteractor LeftControllerRayInteractor;

    }
}
