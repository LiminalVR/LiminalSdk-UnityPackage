using UnityEngine;

[System.Serializable]
public class LimappTimeSettings : LimappSettings
{
    public float TimeScale;
    public float MaximumParticleDeltaTime;
    public float MaximumDeltaTime;
    public int CaptureFramerate;
    public float FixedDeltaTime;

    public override void SaveSettings()
    {
        TimeScale= Time.timeScale;
        MaximumParticleDeltaTime = Time.maximumParticleDeltaTime;
        MaximumDeltaTime = Time.maximumDeltaTime;
        CaptureFramerate = Time.captureFramerate;
        FixedDeltaTime = Time.fixedDeltaTime;
    }

    public override void ApplySettings()
    {
        Time.timeScale = TimeScale;
        Time.maximumParticleDeltaTime = FixedDeltaTime;
        Time.maximumDeltaTime = MaximumDeltaTime;
        Time.captureFramerate = CaptureFramerate;
        Time.fixedDeltaTime = FixedDeltaTime;
    }
}