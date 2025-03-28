namespace PersonaEngine.Lib.Audio;

public interface IMicrophone : IAwaitableAudioSource
{
    void StartRecording();

    void StopRecording();
}