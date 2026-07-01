using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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

        // Maps the 0-4 enum onto the provider-agnostic XRDisplaySubsystem.foveatedRenderingLevel,
        // a normalized float (0 = off .. 1 = max). Flags are left at None for fixed (non gaze)
        // foveation, matching the old Oculus FFR behaviour. Works under OpenXR and Oculus alike.
        private static void ApplyFoveation(int level)
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);

            var normalized = Mathf.Clamp01(level / 4f);
            foreach (var display in displays)
            {
                display.foveatedRenderingLevel = normalized;
                display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
            }
        }
    }
}
