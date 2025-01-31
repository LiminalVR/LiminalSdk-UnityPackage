using System;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;

/// <summary>
/// A wrapper utility to interface with OVRInput and OVRPlugin for common OVR Usages.
/// </summary>
public static class OVRUtils
{
    public static bool IsOculusQuest => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest;

    [Obsolete("Oculus go no longer supported by OVR")]
    public static bool IsOculusGo => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.None;

    /// <summary>
    /// When both controllers are connected, Controller.Touch is used.
    /// When one controller is connected, the individual Controller.RTouch is used.
    /// </summary>
    public static bool IsQuestControllerConnected
        => OVRInput.IsControllerConnected(OVRInput.Controller.Touch) ||
           OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) ||
           OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

    [Obsolete("Oculus go no longer supported by OVR")]
    public static bool IsGearVRHeadset()
    {
        return false; 
    }

    public static bool IsRift()
    {
        OVRPlugin.SystemHeadset headsetType = OVRPlugin.GetSystemHeadsetType();
        switch (headsetType)
        {
            case OVRPlugin.SystemHeadset.Rift_DK1:
            case OVRPlugin.SystemHeadset.Rift_DK2:
            case OVRPlugin.SystemHeadset.Rift_CV1:
            case OVRPlugin.SystemHeadset.Rift_CB:
            case OVRPlugin.SystemHeadset.Rift_S: 
                return true;
            default:
                return false;
        }
    }

    public static bool IsLimbConnected(VRAvatarLimbType limbType)
    {
        var type = GetControllerType(limbType);
        return OVRInput.IsControllerConnected(type);
    }

    /// <summary>
    /// OVRInput.Controller will return it as an enum and not a mask.
    /// </summary>
    /// <param name="limbType"></param>
    /// <returns></returns>
    public static OVRInput.Controller GetControllerType(VRAvatarLimbType limbType)
    {
        switch (limbType)
        {
            case VRAvatarLimbType.LeftHand:
                return OVRInput.Controller.LTouch;
            case VRAvatarLimbType.RightHand:
                return OVRInput.Controller.RTouch;
            default:
                return OVRInput.Controller.None;
        }
    }

    /// <summary>
    /// OVRInput.Controller will return it as an enum and not a mask.
    /// </summary>
    /// <param name="limbType"></param>
    /// <returns></returns>
    public static OVRInput.Controller GetControllerType(VRInputDeviceHand hand)
    {
        switch (hand)
        {
            case VRInputDeviceHand.Left:
                return OVRInput.Controller.LTouch;
            case VRInputDeviceHand.Right:
                return OVRInput.Controller.RTouch;
            default:
                return OVRInput.Controller.None;
        }
    }
}