using System;
using UnityEngine;

/// <summary>
/// <see cref="LiminalConfig"/> is used to save and set experience settings when loading and unloading an experience.
/// </summary>
[Serializable]
public class LiminalConfig
{
    [HideInInspector]
    public ExperienceProfile SavedProfile;

    [HideInInspector]
    public ExperienceProfile ProfileToApply;

    public bool OverrideProfile;

    /// <summary>
    /// Saves the currently active settings as an experience profile and then applies to set profile 
    /// </summary>
    public void Apply()
    {
        SaveProfile();

        if (ProfileToApply == null)
            return;

        ProfileToApply.ApplyConfigSettings();
    }

    private void SaveProfile()
    {
        var newProfile = ScriptableObject.CreateInstance<ExperienceProfile>();

        newProfile.Init();

        SavedProfile = newProfile;
    }

    /// <summary>
    /// Resets the settings to what they were before the experience started.
    /// </summary>
    public void Release()
    {
        SavedProfile.ApplyConfigSettings();
    }

}

public enum ESDKType
{
    UnityXR,
    Emulator,
    OVR,
    OpenVR,
    Pico
}

[Serializable]
public class SDKSettings
{
    public ESDKType Android = ESDKType.UnityXR;
    public ESDKType Standalone = ESDKType.UnityXR;
}

[Serializable]
public class ProjectSettings
{
    public bool Override;
    public ExperienceProfile Profile;
}
