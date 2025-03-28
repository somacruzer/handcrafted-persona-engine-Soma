using Microsoft.Extensions.Logging;

using NAudio.Wave;

namespace PersonaEngine.Lib.Audio;

/// <summary>
///     Represents a source that captures audio from a microphone.
/// </summary>
public sealed class MicrophoneInputNAudioSource : AwaitableWaveFileSource, IMicrophone
{
    private readonly CancellationTokenSource _cts;

    private readonly ILogger _logger;

    private readonly WaveInEvent _microphoneIn;

    private bool _isRecording;

    private Dictionary<string, string> _metadata;

    public MicrophoneInputNAudioSource(
        ILogger<MicrophoneInputNAudioSource> logger,
        int                                  deviceNumber        = 0,
        int                                  sampleRate          = 16000,
        int                                  bitsPerSample       = 16,
        int                                  channels            = 1,
        bool                                 storeSamples        = true,
        bool                                 storeBytes          = false,
        int                                  initialSizeFloats   = DefaultInitialSize,
        int                                  initialSizeBytes    = DefaultInitialSize,
        IChannelAggregationStrategy?         aggregationStrategy = null)
        : this(new Dictionary<string, string>(), logger, deviceNumber, sampleRate, bitsPerSample, channels,
               storeSamples, storeBytes, initialSizeFloats, initialSizeBytes, aggregationStrategy) { }

    private MicrophoneInputNAudioSource(
        Dictionary<string, string>           metadata,
        ILogger<MicrophoneInputNAudioSource> logger,
        int                                  deviceNumber        = 0,
        int                                  sampleRate          = 16000,
        int                                  bitsPerSample       = 16,
        int                                  channels            = 1,
        bool                                 storeSamples        = true,
        bool                                 storeBytes          = false,
        int                                  initialSizeFloats   = DefaultInitialSize,
        int                                  initialSizeBytes    = DefaultInitialSize,
        IChannelAggregationStrategy?         aggregationStrategy = null)
        : base(metadata, storeSamples, storeBytes, initialSizeFloats, initialSizeBytes, aggregationStrategy)
    {
        _logger   = logger;
        _cts      = new CancellationTokenSource();
        _metadata = metadata;

        _microphoneIn = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = new WaveFormat(sampleRate, bitsPerSample, channels) };

        Initialize(new AudioSourceHeader { BitsPerSample = (ushort)bitsPerSample, Channels = (ushort)channels, SampleRate = (uint)sampleRate });

        _microphoneIn.DataAvailable    += WaveIn_DataAvailable;
        _microphoneIn.RecordingStopped += MicrophoneIn_RecordingStopped;
    }

    public void StartRecording()
    {
        if ( _isRecording )
        {
            return;
        }

        _isRecording = true;
        _microphoneIn.StartRecording();
    }

    public void StopRecording()
    {
        if ( !_isRecording )
        {
            return;
        }

        _isRecording = false;
        _microphoneIn.StopRecording();
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e) { WriteData(e.Buffer.AsMemory(0, e.BytesRecorded)); }

    private void MicrophoneIn_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        if ( e.Exception != null )
        {
            throw e.Exception;
        }

        Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if ( disposing )
        {
            _cts.Cancel();
            StopRecording();

            _microphoneIn.DataAvailable    -= WaveIn_DataAvailable;
            _microphoneIn.RecordingStopped -= MicrophoneIn_RecordingStopped;
            _microphoneIn.Dispose();
        }

        base.Dispose(disposing);
    }
}