using UnityEngine;

namespace Liminal.SDK.VR.Pointers
{
    /// <summary>
    /// A component that controls the timer visual for a <see cref="TimedGazePointer"/> implementation. This component should be nested at, or below, a <see cref="GazePointerVisual"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GazePointerTimerVisual : MonoBehaviour
    {
        private static class Uniforms
        {
            public static readonly int Progress = Shader.PropertyToID("_Progress");
        }

        private GazePointerVisual mPointerVisual;
        private TimedGazePointer mTimedPointer;
        private MeshRenderer mRenderer;
        private MaterialPropertyBlock mPropertyBlock;

        #region MonoBehaviour

        private void Awake()
        {
            mRenderer = GetComponent<MeshRenderer>();
            mPointerVisual = GetComponentInParent<GazePointerVisual>();
            mTimedPointer = (mPointerVisual != null)
                ? mPointerVisual.Pointer as TimedGazePointer
                : null;

            mPropertyBlock = new MaterialPropertyBlock();
        }

        private void OnTransformParentChanged()
        {
            mPointerVisual = GetComponentInParent<GazePointerVisual>();
            mTimedPointer = (mPointerVisual != null)
                ? mPointerVisual.Pointer as TimedGazePointer
                : null;
        }

        private void Update()
        {
            if (mTimedPointer == null)
            {
                mTimedPointer = (mPointerVisual != null)
                    ? mPointerVisual.Pointer as TimedGazePointer
                    : null;
            }

            UpdateTimer();
        }

        #endregion

        private void UpdateTimer()
        {
            if (mRenderer == null)
                return;

            if (mTimedPointer == null)
            {
                mRenderer.enabled = false;
                return;
            }

            var progress = mTimedPointer.HoverTimeNormalized;
            if (progress <= 0)
            {
                mRenderer.enabled = false;
                return;
            }

            // Update property block
            mPropertyBlock.SetFloat(Uniforms.Progress, progress);

            // Enable renderer and set material properties
            mRenderer.enabled = true;
            mRenderer.SetPropertyBlock(mPropertyBlock);
        }
    }
}
