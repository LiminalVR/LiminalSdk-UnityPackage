using Liminal.SDK.VR;

namespace Liminal.SDK.V2
{
    public static class DeviceManager
    {
        public static IVRDevice Device;

        public static void Initialize(IVRDevice device)
        {
            Device = device;
        }
    }
}