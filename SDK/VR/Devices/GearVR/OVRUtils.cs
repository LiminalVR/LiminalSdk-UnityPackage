using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;

/// <summary>
/// A wrapper utility to interface with OVRInput and OVRPlugin for common OVR Usages.
/// </summary>
public static class OVRUtils
{
    /// <summary>
    /// True for the whole Quest family. MUST be a range check, not equality:
    /// Quest 2/3/Pro report SystemHeadset values this OVRPlugin's enum
    /// predates (Oculus_Quest_2 = 9, ...). Under Meta's compatibility mode
    /// the reported value depends on the manifest — an app declaring
    /// com.oculus.supportedDevices is told the real (or emulated newer)
    /// device, and the old equality check then silently flipped every
    /// caller into the GearVR-remote branch: controllers "disconnected",
    /// A/trigger dead while tracking worked (Everbloom, Quest 3,
    /// 2026-08-12). Mobile values end below the PC block (Rift_DK1 =
    /// 0x1000), so anything in between is a Quest-family headset.
    /// </summary>
    public static bool IsOculusQuest
    {
        get
        {
            var headset = OVRPlugin.GetSystemHeadsetType();
            // Measured on device (2026-08-12): under the recognised
            // contract this old plugin cannot decode the reported device
            // AT ALL and returns None (0) - not a value above Quest 1's.
            // On an Android build of this SDK, an unidentifiable headset
            // IS a newer Quest; GearVR/Go hardware identifies correctly
            // and is long gone regardless. Editor keeps the old behaviour
            // (None there means "no device", not "newer than the enum").
            if (headset == OVRPlugin.SystemHeadset.None)
                return UnityEngine.Application.platform == UnityEngine.RuntimePlatform.Android;
            return headset >= OVRPlugin.SystemHeadset.Oculus_Quest
                && (int)headset < (int)OVRPlugin.SystemHeadset.Rift_DK1;
        }
    }
    public static bool IsOculusGo => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Go;

    /// <summary>
    /// When both controllers are connected, Controller.Touch is used.
    /// When one controller is connected, the individual Controller.RTouch is used.
    /// </summary>
    public static bool IsQuestControllerConnected
        => OVRInput.IsControllerConnected(OVRInput.Controller.Touch) ||
           OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) ||
           OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

    public static bool IsGearVRHeadset()
    {
        OVRPlugin.SystemHeadset headsetType = OVRPlugin.GetSystemHeadsetType();
        switch (headsetType)
        {
            case OVRPlugin.SystemHeadset.GearVR_R320:
            case OVRPlugin.SystemHeadset.GearVR_R321:
            case OVRPlugin.SystemHeadset.GearVR_R322:
            case OVRPlugin.SystemHeadset.GearVR_R323:
            case OVRPlugin.SystemHeadset.GearVR_R324:
            case OVRPlugin.SystemHeadset.GearVR_R325:
                return true;
            default:
                return false;
        }
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
                return IsOculusQuest ? OVRInput.Controller.LTouch : OVRInput.Controller.LTrackedRemote;
            case VRAvatarLimbType.RightHand:
                return IsOculusQuest ? OVRInput.Controller.RTouch : OVRInput.Controller.RTrackedRemote;
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
                return IsOculusQuest ? OVRInput.Controller.LTouch : OVRInput.Controller.LTrackedRemote;
            case VRInputDeviceHand.Right:
                return IsOculusQuest ? OVRInput.Controller.RTouch : OVRInput.Controller.RTrackedRemote;
            default:
                return OVRInput.Controller.None;
        }
    }
}