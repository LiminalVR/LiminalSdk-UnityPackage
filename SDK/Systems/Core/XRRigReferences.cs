using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands.Samples.VisualizerSample;
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

        public TrackedPoseDriver OriginalTrackedPoseDriver;
        public Transform CameraOffset;


        [Header("Controllers")]
        public Transform RightController;
        public Transform LeftController;


        public Transform RightControllerVisual;
        public Transform LeftControllerVisual;

        public XRRayInteractor RightControllerRayInteractor;
        public XRRayInteractor LeftControllerRayInteractor;

        [Header("Hands")]

        public Transform RightHand;
        public Transform LeftHand;

        public XRRayInteractor RightHandRayInteractor;
        public XRRayInteractor LeftHandRayInteractor;

        public HandVisualizer HandVisualizer;
    }
}
