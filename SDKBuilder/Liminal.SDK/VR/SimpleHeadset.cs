using Liminal.SDK.VR.Pointers;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// A simple IVRHeadset implementation suitable for headsets with no input capabilities.
    /// </summary>
    public class SimpleHeadset : IVRHeadset
    {
        private string mName;
        private readonly VRHeadsetCapability mCapabilities;
        private IVRPointer mPointer;

        string IVRDeviceComponent.Name { get { return mName; } }
        IVRPointer IVRDeviceComponent.Pointer { get { return mPointer; } }

        public SimpleHeadset(string name, VRHeadsetCapability capabilities)
        {
            mName = name;
            mCapabilities = capabilities;
            mPointer = new TimedGazePointer(this);
            mPointer.Deactivate();
        }

        public bool HasCapabilities(VRHeadsetCapability capabilities)
        {
            return ((mCapabilities & capabilities) == capabilities);
        }
    }
}
