using UnityEngine;

[System.Serializable]
public class LimappAudioConfiguration
{
    public AudioSpeakerMode SpeakerMode;
    public int DspBufferSize;
    public int SampleRate;
    public int NumRealVoices;
    public int NumVirtualVoices;

    public void GetAudioConfiguration()
    {
        var audioConfig = UnityEngine.AudioSettings.GetConfiguration();

        SpeakerMode = audioConfig.speakerMode; 
        DspBufferSize = audioConfig.dspBufferSize; 
        SampleRate = audioConfig.sampleRate; 
        NumRealVoices = audioConfig.numRealVoices;
        NumVirtualVoices = audioConfig.numVirtualVoices;
    }

    public void SetAudioConfiguration()
    {
        var audioConfig = UnityEngine.AudioSettings.GetConfiguration();

        audioConfig.speakerMode = SpeakerMode;
        audioConfig.dspBufferSize = DspBufferSize;
        audioConfig.sampleRate = SampleRate;
        audioConfig.numRealVoices = NumRealVoices;
        audioConfig.numVirtualVoices = NumVirtualVoices;

        UnityEngine.AudioSettings.Reset(audioConfig);
    }
}