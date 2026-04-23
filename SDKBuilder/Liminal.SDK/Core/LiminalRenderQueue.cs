using UnityEngine.Rendering;

namespace Liminal.SDK.Core
{
    public static class LiminalRenderQueue
    {
        /// <summary>
        /// Screen fader will try to not to obscure materials with a higher RenderQueue 
        /// </summary>
        public static readonly int Fader = (int) RenderQueue.Overlay;
    }
}
