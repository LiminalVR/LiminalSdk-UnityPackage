using System;
using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// GameObjects with this component attached will automatically have a device-specific controller attached to them when the avatar is initialized.
    /// Generally this component is added to an object anchored to the hands.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Avatar/Controller")]
    public class VRAvatarController : MonoBehaviour
    {
        public VRControllerVisual ControllerVisual { get; set; }
    }

    [Serializable]
    public class ControllerVisualSettings
    {
        public bool Visible = true;
    }
    
    [Serializable]
    public class PointerVisualSettings
    {
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngle;

        public bool ReticleVisibility = true;
        public bool HideReticleWhenInvalid = true;

        public float DefaultReticleDistance = 20f;
        public float GrowTime = 0.1f;
        public float MaxDistance = 20f;
    }

}
