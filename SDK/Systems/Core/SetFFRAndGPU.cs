using UnityEngine;

namespace Liminal.SDK.V2
{
    public class SetFFRAndGPU : MonoBehaviour
    {
        // Mirrors the legacy OVRManager.FixedFoveatedRenderingLevel values (0-4) so existing
        // serialized inspector selections are preserved after the OpenXR migration.
        public enum FixedFoveatedRenderingLevel
        {
            Off = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            HighTop = 4,
        }

        public FixedFoveatedRenderingLevel FFRLevel;

        public void Awake()
        {
            SetFFR();
        }

        void OnDestroy()
        {
            ResetFFR();
        }

        public void SetFFR()
        {
            ApplyFoveation((int)FFRLevel);
        }

        public void ResetFFR()
        {
            ApplyFoveation((int)FixedFoveatedRenderingLevel.Off);
        }

        // Goes through OVRManager.fixedFoveatedRenderingLevel, the same path limapps' own SetFFRAndGPU
        // scripts use: under OpenXR its bridge (OVRManager.OpenXRFoveation) maps the 0-4 level onto
        // XRDisplaySubsystem.foveatedRenderingLevel (level / 4: 0 = off .. 1 = max) with no gaze, on every
        // XR display, applies it once a display is running if none is yet, and remembers it (so the getter
        // and the platform's logs agree). With the legacy Oculus plugin it uses that plugin as before.
        private static void ApplyFoveation(int level)
        {
            OVRManager.fixedFoveatedRenderingLevel = (OVRManager.FixedFoveatedRenderingLevel)Mathf.Clamp(level, 0, 4);
        }
    }
}
