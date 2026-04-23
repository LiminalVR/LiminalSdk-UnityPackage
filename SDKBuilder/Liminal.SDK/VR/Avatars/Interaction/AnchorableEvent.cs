using System;
using UnityEngine.Events;

namespace Liminal.SDK.VR.Avatars.Interaction
{
    /// <summary>
    /// A serializable unity event for <see cref="Anchorable"/> objects.
    /// </summary>
    [Serializable]
    public class AnchorableEvent : UnityEvent<Anchorable>
    {

    }
}
