using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.Audio.Player;

public class AudioPlaybackEventArgs : EventArgs
{
    public AudioPlaybackEventArgs(AudioSegment segment) { Segment = segment; }

    public AudioSegment Segment { get; }
}

public interface IStreamingAudioPlayer : IAsyncDisposable
{
    Task StartPlaybackAsync(IAsyncEnumerable<AudioSegment> audioSegments, CancellationToken cancellationToken = default);
}

public interface IStreamingAudioPlayerHost
{
    float CurrentTime { get; }

    event EventHandler<AudioPlaybackEventArgs>? OnPlaybackStarted;

    event EventHandler<AudioPlaybackEventArgs>? OnPlaybackCompleted;
}

public interface IAggregatedStreamingAudioPlayer : IStreamingAudioPlayer
{
    IReadOnlyCollection<IStreamingAudioPlayer> Players { get; }

    void AddPlayer(IStreamingAudioPlayer player);

    bool RemovePlayer(IStreamingAudioPlayer player);
}