using System;
using UnityEngine;
using UnityEngine.Events;

namespace Liminal.SDK.VR.Avatars
{
    /// <summary>
    /// A serializable unity event for <see cref="IVRAvatarLimb"/> attachment events.
    /// </summary>
    [Serializable]
    public class VRLimbAttachmentEvent : UnityEvent<IVRAvatarLimb, GameObject>
    {

    }
}
