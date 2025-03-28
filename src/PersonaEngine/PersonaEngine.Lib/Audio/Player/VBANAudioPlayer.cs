using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.Audio.Player;

/// <summary>
///     Implementation of IStreamingAudioPlayer using VBAN protocol.
/// </summary>
public class VBANAudioPlayer : IStreamingAudioPlayer, IStreamingAudioPlayerHost
{
    private readonly IAudioBufferManager _bufferManager;

    private readonly int _channels;

    private readonly ILogger _logger;

    private readonly IPlaybackController _playbackController;

    private readonly SemaphoreSlim _playbackLock = new(1, 1);

    private readonly object _stateLock = new();

    private readonly IAudioTransport _transport;

    private readonly int _vbanPacketSize;

    private int _disposed;

    private CancellationTokenSource? _playbackCts;

    private Task? _playbackTask;

    private volatile PlayerState _state = PlayerState.Uninitialized;

    /// <summary>
    ///     Creates a new instance of VBANAudioPlayer.
    /// </summary>
    public VBANAudioPlayer(
        IAudioTransport           transport,
        IAudioBufferManager       bufferManager,
        IPlaybackController       playbackController,
        int                       vbanPacketSize = 256,
        int                       channels       = 1,
        ILogger<VBANAudioPlayer>? logger         = null)
    {
        _transport          = transport ?? throw new ArgumentNullException(nameof(transport));
        _bufferManager      = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
        _playbackController = playbackController ?? throw new ArgumentNullException(nameof(playbackController));

        _vbanPacketSize = vbanPacketSize > 0
                              ? vbanPacketSize
                              : throw new ArgumentException("VBAN packet size must be positive", nameof(vbanPacketSize));

        _channels = channels > 0
                        ? channels
                        : throw new ArgumentException("Channels must be positive", nameof(channels));

        _logger = logger ?? NullLogger<VBANAudioPlayer>.Instance;

        // Set initial state
        lock (_stateLock)
        {
            _state = PlayerState.Initialized;
        }
    }

    /// <summary>
    ///     Starts playback of the provided audio segments.
    /// </summary>
    public async Task StartPlaybackAsync(
        IAsyncEnumerable<AudioSegment> audioSegments,
        CancellationToken              cancellationToken = default)
    {
        ThrowIfDisposed();

        // Ensure only one playback operation can run at a time
        var       timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        using var linkedCts    = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);

        try
        {
            await _playbackLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
        {
            throw new TimeoutException("Timed out waiting for previous playback to complete.");
        }

        try
        {
            // Stop any existing playback
            await StopPlaybackAsync(CancellationToken.None).ConfigureAwait(false);

            // Initialize transport
            await _transport.InitializeAsync();

            // Set up new playback state
            lock (_stateLock)
            {
                _state = PlayerState.Starting;
            }

            _playbackCts?.Dispose();
            _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _playbackController.Reset();
            _bufferManager.Clear();

            // Start producer and consumer tasks
            var producerTask = _bufferManager.EnqueueSegmentsAsync(audioSegments, _playbackCts.Token);
            _playbackTask =
                ProcessAudioAsync(_playbackCts.Token);

            lock (_stateLock)
            {
                _state = PlayerState.Playing;
            }

            await producerTask.ConfigureAwait(false);
            await _playbackTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio playback");
            _bufferManager.Clear();

            throw new AudioException("Error during audio playback", ex);
        }
        finally
        {
            await StopPlaybackAsync(CancellationToken.None).ConfigureAwait(false);
            _playbackLock.Release();
        }
    }

    /// <summary>
    ///     Disposes resources used by the player.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if ( Interlocked.Exchange(ref _disposed, 1) != 0 )
        {
            return;
        }

        try
        {
            // Stop playback
            await StopPlaybackAsync(CancellationToken.None).ConfigureAwait(false);

            // Dispose resources
            _playbackCts?.Dispose();
            _playbackCts = null;

            _playbackLock.Dispose();

            await _bufferManager.DisposeAsync();
            await _transport.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during async disposal");
        }
    }

    public event EventHandler<AudioPlaybackEventArgs>? OnPlaybackStarted;

    public event EventHandler<AudioPlaybackEventArgs>? OnPlaybackCompleted;

    public float CurrentTime => _playbackController.CurrentTime;

    /// <summary>
    ///     Creates a new instance of VBANAudioPlayer with default implementations of dependencies.
    /// </summary>
    public static VBANAudioPlayer Create(
        string          destHost,
        int             destPort,
        string          streamName,
        int             channels            = 1,
        int             maxBufferCount      = 32,
        int             vbanPacketSize      = 256,
        int             initialBufferMs     = 0,
        int             maxAllowableDriftMs = 20,
        ILoggerFactory? loggerFactory       = null)
    {
        var transportLogger = loggerFactory?.CreateLogger<VBANTransport>();
        var bufferLogger    = loggerFactory?.CreateLogger<DefaultAudioBufferManager>();
        var timingLogger    = loggerFactory?.CreateLogger<TimedPlaybackController>();
        var playerLogger    = loggerFactory?.CreateLogger<VBANAudioPlayer>();

        var packetBuilder      = new VBANPacketBuilder(streamName);
        var transport          = new VBANTransport(destHost, destPort, packetBuilder, transportLogger);
        var bufferManager      = new DefaultAudioBufferManager(maxBufferCount, bufferLogger);
        var playbackController = new TimedPlaybackController(initialBufferMs, maxAllowableDriftMs, 20, timingLogger);

        return new VBANAudioPlayer(
                                   transport,
                                   bufferManager,
                                   playbackController,
                                   vbanPacketSize,
                                   channels,
                                   playerLogger);
    }

    /// <summary>
    ///     Stops the current playback.
    /// </summary>
    private async Task StopPlaybackAsync(CancellationToken cancellationToken)
    {
        var shouldStop = false;

        lock (_stateLock)
        {
            if ( _state is PlayerState.Playing or PlayerState.Starting )
            {
                _state     = PlayerState.Stopping;
                shouldStop = true;
            }
        }

        if ( !shouldStop )
        {
            return;
        }

        // Signal cancellation
        if ( _playbackCts is { IsCancellationRequested: false } )
        {
            try
            {
                await _playbackCts.CancelAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while cancelling playback");
            }
        }

        // Clear buffer queue
        _bufferManager.Clear();

        // Wait for playback task to complete
        if ( _playbackTask != null )
        {
            try
            {
                await _playbackTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while waiting for playback task to complete");
            }

            _playbackTask = null;
        }

        lock (_stateLock)
        {
            _state = PlayerState.Stopped;
        }
    }

    /// <summary>
    ///     Processes and sends audio data with precise timing.
    /// </summary>
    private async Task ProcessAudioAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Current buffer being processed
            (Memory<float> Data, AudioSegment Segment)? currentBuffer   = null;
            var                                         currentPosition = 0;
            var                                         segmentStarted  = false;
            // Packet buffer
            var buffer = new float[_vbanPacketSize * _channels];

            while ( !cancellationToken.IsCancellationRequested )
            {
                var isPlaying = false;
                lock (_stateLock)
                {
                    isPlaying = _state is PlayerState.Playing or PlayerState.Starting;
                }

                if ( !isPlaying ||
                     (_bufferManager is { ProducerCompleted: true, BufferCount: 0 } && currentBuffer == null) )
                {
                    if ( _bufferManager is { ProducerCompleted: true, BufferCount: 0 } && currentBuffer == null )
                    {
                        // Playback is complete
                        lock (_stateLock)
                        {
                            _state = PlayerState.Stopped;
                        }

                        break;
                    }

                    // Wait a bit before checking again
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                var samplesInBuffer = 0;

                // Fill the VBAN packet buffer
                while ( samplesInBuffer < _vbanPacketSize * _channels && !cancellationToken.IsCancellationRequested )
                {
                    // Get next buffer if needed
                    if ( currentBuffer == null )
                    {
                        // Use TryTake with a short timeout to be responsive to cancellation
                        if ( !_bufferManager.TryGetNextBuffer(out var nextBuffer, 10, cancellationToken) )
                        {
                            // No more data available yet
                            break;
                        }

                        currentBuffer   = nextBuffer;
                        currentPosition = 0;
                        segmentStarted  = false;
                    }

                    // Notify segment start if needed
                    if ( !segmentStarted )
                    {
                        OnPlaybackStarted?.Invoke(this, new AudioPlaybackEventArgs(currentBuffer.Value.Segment));
                        segmentStarted = true;
                    }

                    // Calculate how many samples to copy
                    var remaining     = currentBuffer.Value.Data.Length - currentPosition;
                    var samplesToCopy = Math.Min(remaining, _vbanPacketSize * _channels - samplesInBuffer);

                    if ( samplesToCopy > 0 )
                    {
                        // Copy data to the output buffer
                        currentBuffer.Value.Data.Span.Slice(currentPosition, samplesToCopy)
                                     .CopyTo(buffer.AsSpan(samplesInBuffer, samplesToCopy));

                        currentPosition += samplesToCopy;
                        samplesInBuffer += samplesToCopy;
                    }

                    // Check if buffer is finished
                    if ( currentPosition >= currentBuffer.Value.Data.Length )
                    {
                        // Notify segment completion
                        OnPlaybackCompleted?.Invoke(this, new AudioPlaybackEventArgs(currentBuffer.Value.Segment));
                        currentBuffer = null;
                    }
                }

                // Check for cancellation frequently
                if ( cancellationToken.IsCancellationRequested )
                {
                    break;
                }

                // Send the VBAN packet if we have any samples
                if ( samplesInBuffer > 0 )
                {
                    try
                    {
                        // Normalize samples per channel
                        var samplesPerChannel = samplesInBuffer / _channels;
                        var sampleRate        = currentBuffer?.Segment.SampleRate ?? 24000; // Default if no current buffer

                        // Let the playback controller decide when to send this packet
                        var shouldSend = await _playbackController.SchedulePlaybackAsync(
                                                                                         samplesPerChannel,
                                                                                         sampleRate,
                                                                                         cancellationToken);

                        if ( shouldSend )
                        {
                            await _transport.SendAudioPacketAsync(
                                                                  buffer.AsMemory(0, samplesInBuffer),
                                                                  sampleRate,
                                                                  samplesPerChannel,
                                                                  _channels,
                                                                  cancellationToken);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error sending audio packet");
                    }
                }
                else
                {
                    // No data available, add a short delay before checking again
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Audio processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data");

            lock (_stateLock)
            {
                _state = PlayerState.Error;
            }
        }
    }

    /// <summary>
    ///     Checks if the player has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if ( Interlocked.CompareExchange(ref _disposed, 0, 0) != 0 )
        {
            throw new ObjectDisposedException(nameof(VBANAudioPlayer));
        }
    }
}