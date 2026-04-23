using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// <see cref="ExperienceProfile"/> is used to Save the settings for an experience.
/// </summary>

[Serializable]
public class ExperienceProfile : ScriptableObject
{
    public LimappAudioSettings LimappAudioSettings;
    public LimappGraphicsSettings LimappGraphicsSettings;
    public LimappPhysicsSettings LimappPhysicsSettings;
    public LimappPhysics2DSettings LimappPhysics2DSettings;
    public LimappQualitySettings LimappQualitySettings;
    public LimappTimeSettings LimappTimeSettings;

    public void Init()
    {
        return;
        LimappAudioSettings = new LimappAudioSettings();
        LimappGraphicsSettings = new LimappGraphicsSettings();
        LimappPhysicsSettings = new LimappPhysicsSettings();
        LimappPhysics2DSettings = new LimappPhysics2DSettings();
        LimappQualitySettings = new LimappQualitySettings();
        LimappTimeSettings = new LimappTimeSettings();

        SaveProjectSettings();
    }

    public void SaveProjectSettings()
    {
        return;
        LimappAudioSettings.SaveSettings();
        LimappGraphicsSettings.SaveSettings();
        LimappPhysicsSettings.SaveSettings();
        LimappPhysics2DSettings.SaveSettings();
        LimappQualitySettings.SaveSettings();
        LimappTimeSettings.SaveSettings();
    }

    public void ApplyConfigSettings()
    {
        return;
        LimappAudioSettings.ApplySettings();
        LimappGraphicsSettings.ApplySettings();
        LimappPhysicsSettings.ApplySettings();
        LimappPhysics2DSettings.ApplySettings();
        LimappQualitySettings.ApplySettings();
        LimappTimeSettings.ApplySettings();
    }
}
