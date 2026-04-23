using UnityEngine;

[System.Serializable]
public class LimappAudioSettings : LimappSettings
{
    public float Volume;
    public LimappAudioConfiguration AudioConfig;

    public override void SaveSettings()
    {
        Volume = AudioListener.volume;
        AudioConfig = new LimappAudioConfiguration();
        AudioConfig.GetAudioConfiguration();
    }

    public override void ApplySettings()
    {
        AudioListener.volume = Volume;
        AudioConfig.SetAudioConfiguration();
    }
}