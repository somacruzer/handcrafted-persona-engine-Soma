using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using PersonaEngine.Lib.TTS.Synthesis;

using PortAudioSharp;

using Stream = PortAudioSharp.Stream;

namespace PersonaEngine.Lib.Audio.Player;

/// <summary>
///     PortAudio implementation of <see cref="IStreamingAudioPlayer" /> for real-time audio playback.
///     This implementation is thread-safe and designed for low-latency audio processing.
/// </summary>
public sealed class PortAudioStreamingPlayer : IStreamingAudioPlayer, IStreamingAudioPlayerHost
{
    public const int DefaultSampleRate = 24000;

    public const int DefaultMaxBufferCount = 100;

    public const int DefaultFrameBufferSize = 1024;

    // Thread-safe for concurrent producer/consumer pattern with bounded capacity for backpressure
    private readonly BlockingCollection<AudioBuffer> _audioQueue;

    // Pre-allocated frame buffer to avoid GC in audio callback
    private readonly float[] _frameBuffer;

    private readonly uint _frameBufferSize;

    private readonly ILogger<PortAudioStreamingPlayer> _logger;

    private readonly int _maxBufferCount;

    // Audio processing state - use concurrent collection for thread-safe event queueing
    private readonly ConcurrentQueue<(AudioSegment Segment, bool IsStart)> _pendingEvents = new();

    // Synchronization objects
    private readonly SemaphoreSlim _playbackLock = new(1, 1);

    private readonly int _sampleRate;

    private readonly object _stateLock = new();

    private Stream? _audioStream;

    private AudioBuffer? _currentBuffer;

    private volatile float _currentTime;

    // Disposal tracking
    private int _disposed;

    private CancellationTokenSource? _eventProcessingCts;

    // Task completion and cancellation management
    private TaskCompletionSource<object?>? _playbackCompletion;

    private CancellationTokenSource? _playbackCts;

    private volatile bool _producerCompleted;

    private volatile PlayerState _state = PlayerState.Uninitialized;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PortAudioStreamingPlayer" /> class.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz. Default is 24000.</param>
    /// <param name="maxBufferCount">Maximum number of audio buffers to queue. Default is 100.</param>
    /// <param name="frameBufferSize">Size of each frame buffer in samples. Default is 1024.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="sampleRate" />, <paramref name="maxBufferCount" />, or
    ///     <paramref name="frameBufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="AudioPlayerInitializationException">
    ///     Thrown when initialization of PortAudio fails.
    /// </exception>
    public PortAudioStreamingPlayer(
        int                                sampleRate      = DefaultSampleRate,
        int                                maxBufferCount  = DefaultMaxBufferCount,
        uint                               frameBufferSize = DefaultFrameBufferSize,
        ILogger<PortAudioStreamingPlayer>? logger          = null)
    {
        _sampleRate = sampleRate > 0
                          ? sampleRate
                          : throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));

        _maxBufferCount = maxBufferCount > 0
                              ? maxBufferCount
                              : throw new ArgumentException("Max buffer count must be positive.", nameof(maxBufferCount));

        _frameBufferSize = frameBufferSize > 0
                               ? frameBufferSize
                               : throw new ArgumentException("Frame buffer size must be positive.", nameof(frameBufferSize));

        _logger = logger ?? NullLogger<PortAudioStreamingPlayer>.Instance;

        // Pre-allocate frame buffer to avoid GC during audio callback
        _frameBuffer = new float[frameBufferSize];

        // Use BoundedCapacity to apply backpressure to producers
        _audioQueue = new BlockingCollection<AudioBuffer>(maxBufferCount);

        // Initialize PortAudio
        InitializePortAudio();

        // Start background task for event processing
        _eventProcessingCts = new CancellationTokenSource();
        _                   = Task.Run(ProcessEventsAsync);
    }

    /// <summary>
    ///     Asynchronously disposes resources used by the player.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if ( Interlocked.Exchange(ref _disposed, 1) != 0 )
        {
            return;
        }

        try
        {
            // Stop event processing
            try
            {
                _eventProcessingCts?.Cancel();
                _eventProcessingCts?.Dispose();
                _eventProcessingCts = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while cancelling event processing");
            }

            // Ensure playback is fully stopped before disposing resources
            await StopPlaybackInternalAsync().ConfigureAwait(false);

            // Clean up managed resources
            _audioStream?.Dispose();
            _audioStream = null;

            _playbackCts?.Dispose();
            _playbackCts = null;

            _playbackLock.Dispose();

            // Complete and dispose the audio queue
            _audioQueue.CompleteAdding();
            _audioQueue.Dispose();

            // Terminate PortAudio if it was initialized
            lock (_stateLock)
            {
                if ( _state != PlayerState.Uninitialized )
                {
                    try
                    {
                        PortAudio.Terminate();
                        _state = PlayerState.Uninitialized;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during PortAudio termination");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
    }

    /// <summary>
    ///     Starts playback of the provided audio segments.
    /// </summary>
    /// <param name="audioSegments">The audio segments to play.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel playback.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the player has been disposed.</exception>
    /// <exception cref="TimeoutException">Thrown if unable to acquire the playback lock within timeout period.</exception>
    /// <exception cref="AudioException">Thrown if an error occurs during playback.</exception>
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
            await StopPlaybackInternalAsync().ConfigureAwait(false);

            // Set up new playback state
            lock (_stateLock)
            {
                _state = PlayerState.Starting;
            }

            _playbackCts?.Dispose();
            _playbackCts        = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _playbackCompletion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _producerCompleted  = false;

            // Clear audio queue from any previous playback
            while ( _audioQueue.TryTake(out _) ) { }

            // Start producer task for audio segments
            var producerTask = EnqueueAudioSegmentsAsync(audioSegments, _playbackCts.Token);

            try
            {
                // Start the audio stream
                StartAudioStream();

                lock (_stateLock)
                {
                    _state = PlayerState.Playing;
                }

                // Wait for producer to finish adding segments
                await producerTask.ConfigureAwait(false);
                _producerCompleted = true;

                // Wait for playback to complete
                await _playbackCompletion.Task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                while ( _audioQueue.TryTake(out _) ) { }

                throw;
            }
        }
        finally
        {
            await CleanupPlaybackAsync().ConfigureAwait(false);
            _playbackLock.Release();
        }
    }

    /// <summary>
    ///     Gets the current playback time in seconds.
    /// </summary>
    public float CurrentTime => _currentTime;

    /// <summary>
    ///     Occurs when playback of an audio segment starts.
    /// </summary>
    public event EventHandler<AudioPlaybackEventArgs>? OnPlaybackStarted;

    /// <summary>
    ///     Occurs when playback of an audio segment completes.
    /// </summary>
    public event EventHandler<AudioPlaybackEventArgs>? OnPlaybackCompleted;

    private async Task StopPlaybackInternalAsync()
    {
        // Fast path if not playing
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
        if ( _playbackCts != null )
        {
            try
            {
                await _playbackCts.CancelAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while cancelling playback");
            }
            finally
            {
                _playbackCts.Dispose();
                _playbackCts = null;
            }
        }

        // Stop accepting new audio segments and clear queue
        _audioQueue.CompleteAdding();
        while ( _audioQueue.TryTake(out _) ) { }

        // Stop audio stream
        StopAudioStream();

        // Signal completion
        _playbackCompletion?.TrySetResult(null);

        lock (_stateLock)
        {
            _state = PlayerState.Stopped;
        }
    }

    private async Task EnqueueAudioSegmentsAsync(
        IAsyncEnumerable<AudioSegment> audioSegments,
        CancellationToken              cancellationToken)
    {
        try
        {
            await foreach ( var segment in audioSegments.WithCancellation(cancellationToken) )
            {
                if ( segment.AudioData.Length <= 0 || cancellationToken.IsCancellationRequested )
                {
                    continue;
                }

                try
                {
                    // Add to bounded collection - will block if queue is full (applying backpressure)
                    _audioQueue.Add(new AudioBuffer(segment.AudioData, segment), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (InvalidOperationException) when (_audioQueue.IsAddingCompleted)
                {
                    // Queue was completed during processing
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Audio enqueuing cancelled");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enqueuing audio segments");

            throw;
        }
        finally
        {
            // Mark producer as completed if we got through all segments
            if ( !cancellationToken.IsCancellationRequested )
            {
                _producerCompleted = true;
            }
        }
    }

    private StreamCallbackResult AudioCallback(
        IntPtr                     input,
        IntPtr                     output,
        uint                       frameCount,
        ref StreamCallbackTimeInfo timeInfo,
        StreamCallbackFlags        statusFlags,
        IntPtr                     userData)
    {
        try
        {
            // Update current time from callback info
            _currentTime = (float)timeInfo.currentTime;

            // Check cancellation first for fast path
            if ( _playbackCts?.Token.IsCancellationRequested == true )
            {
                if ( _currentBuffer != null )
                {
                    _pendingEvents.Enqueue((_currentBuffer.Segment, false));
                }

                _playbackCompletion?.TrySetResult(null);

                return StreamCallbackResult.Complete;
            }

            var samplesRequested = (int)frameCount;

            // Process audio data - lock-free as much as possible
            var samplesCopied = ProcessAudioData(samplesRequested);

            // Copy processed data to output buffer
            Marshal.Copy(_frameBuffer, 0, output, samplesRequested);

            // Check if playback is complete
            if ( _producerCompleted &&
                 _audioQueue.Count == 0 &&
                 _currentBuffer == null )
            {
                lock (_stateLock)
                {
                    _state = PlayerState.Stopped;
                }

                _playbackCompletion?.TrySetResult(null);

                return StreamCallbackResult.Complete;
            }

            return StreamCallbackResult.Continue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in audio callback");

            lock (_stateLock)
            {
                _state = PlayerState.Error;
            }

            _playbackCompletion?.TrySetException(
                                                 new AudioException("Error during audio playback", ex));

            return StreamCallbackResult.Complete;
        }
    }

    private int ProcessAudioData(int frameCount)
    {
        var samplesWritten = 0;
        var isPlaying      = false;

        lock (_stateLock)
        {
            isPlaying = _state is PlayerState.Playing or PlayerState.Starting;
        }

        try
        {
            while ( samplesWritten < frameCount && isPlaying )
            {
                // Get next buffer if needed
                if ( _currentBuffer == null )
                {
                    if ( !_audioQueue.TryTake(out var nextBuffer) )
                    {
                        break;
                    }

                    _currentBuffer = nextBuffer;

                    // Queue start event - don't raise directly from callback
                    _pendingEvents.Enqueue((_currentBuffer.Segment, true));
                }

                // Copy data from current buffer to frame buffer
                var remainingFrames = frameCount - samplesWritten;
                var samplesToCopy   = Math.Min(remainingFrames, _currentBuffer.Remaining);

                var sourceSpan      = _currentBuffer.Data.Span.Slice(_currentBuffer.Position, samplesToCopy);
                var destinationSpan = _frameBuffer.AsSpan(samplesWritten, samplesToCopy);
                sourceSpan.CopyTo(destinationSpan);

                _currentBuffer.Advance(samplesToCopy);
                samplesWritten += samplesToCopy;

                // Check if buffer is finished
                if ( _currentBuffer.IsFinished )
                {
                    // Queue completion event - don't raise directly from callback
                    _pendingEvents.Enqueue((_currentBuffer.Segment, false));
                    _currentBuffer = null;
                }

                // Check state again for early exit
                lock (_stateLock)
                {
                    isPlaying = _state is PlayerState.Playing or PlayerState.Starting;
                }
            }

            // Fill any remaining space with silence
            if ( samplesWritten < frameCount )
            {
                Array.Clear(_frameBuffer, samplesWritten, frameCount - samplesWritten);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data");

            throw new AudioException("Error processing audio data", ex);
        }

        return samplesWritten;
    }

    // Separate task to process events outside the real-time audio thread
    private async Task ProcessEventsAsync()
    {
        while ( _disposed == 0 && _eventProcessingCts?.Token.IsCancellationRequested != true )
        {
            try
            {
                // Process any pending events
                while ( _pendingEvents.TryDequeue(out var eventInfo) )
                {
                    if ( eventInfo.IsStart )
                    {
                        RaisePlaybackStarted(eventInfo.Segment);
                    }
                    else
                    {
                        RaisePlaybackCompleted(eventInfo.Segment);
                    }
                }

                // Small delay to avoid tight CPU usage
                await Task.Delay(1, _eventProcessingCts?.Token ?? CancellationToken.None)
                          .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_eventProcessingCts?.Token.IsCancellationRequested == true)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio events");

                // Add a delay on error to avoid tight loop
                try
                {
                    await Task.Delay(100, _eventProcessingCts?.Token ?? CancellationToken.None)
                              .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void RaisePlaybackStarted(AudioSegment segment)
    {
        // Thread-safe event invocation
        var handler = OnPlaybackStarted;
        if ( handler != null )
        {
            try
            {
                handler(this, new AudioPlaybackEventArgs(segment));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising PlaybackStarted event");
            }
        }
    }

    private void RaisePlaybackCompleted(AudioSegment segment)
    {
        // Thread-safe event invocation
        var handler = OnPlaybackCompleted;
        if ( handler != null )
        {
            try
            {
                handler(this, new AudioPlaybackEventArgs(segment));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising PlaybackCompleted event");
            }
        }
    }

    private void InitializePortAudio()
    {
        _logger.LogInformation("Initializing PortAudio");

        try
        {
            PortAudio.Initialize();
            var deviceIndex = PortAudio.DefaultOutputDevice;

            if ( deviceIndex == PortAudio.NoDevice )
            {
                throw new AudioDeviceNotFoundException("No default output device found");
            }

            var deviceInfo = PortAudio.GetDeviceInfo(deviceIndex);
            _logger.LogInformation("Using output device: {DeviceName}", deviceInfo.name);

            var parameters = new StreamParameters {
                                                      device                    = deviceIndex,
                                                      channelCount              = 1,
                                                      sampleFormat              = SampleFormat.Float32,
                                                      suggestedLatency          = deviceInfo.defaultLowOutputLatency,
                                                      hostApiSpecificStreamInfo = IntPtr.Zero
                                                  };

            _audioStream = new Stream(
                                      null,
                                      parameters,
                                      _sampleRate,
                                      _frameBufferSize,
                                      StreamFlags.ClipOff,
                                      AudioCallback,
                                      IntPtr.Zero);

            lock (_stateLock)
            {
                _state = PlayerState.Initialized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PortAudio");

            lock (_stateLock)
            {
                _state = PlayerState.Error;
            }

            throw new AudioPlayerInitializationException("Failed to initialize audio system", ex);
        }
    }

    private void StartAudioStream()
    {
        if ( _audioStream == null )
        {
            throw new InvalidOperationException("Audio stream is not initialized");
        }

        try
        {
            _audioStream.Start();
            _logger.LogDebug("PortAudio stream started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio stream");

            lock (_stateLock)
            {
                _state = PlayerState.Error;
            }

            throw new AudioException("Failed to start audio playback", ex);
        }
    }

    private void StopAudioStream()
    {
        if ( _audioStream == null )
        {
            throw new InvalidOperationException("Audio stream is not initialized");
        }

        try
        {
            _audioStream.Stop();
            _logger.LogDebug("PortAudio stream stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio stream");
        }
    }

    private Task CleanupPlaybackAsync()
    {
        try
        {
            StopAudioStream();

            // Clear audio queue
            while ( _audioQueue.TryTake(out _) ) { }

            _currentBuffer = null;

            lock (_stateLock)
            {
                _state = PlayerState.Stopped;
            }

            // Clear frame buffer
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during playback cleanup");

            lock (_stateLock)
            {
                _state = PlayerState.Error;
            }
        }

        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if ( Interlocked.CompareExchange(ref _disposed, 0, 0) != 0 )
        {
            throw new ObjectDisposedException(nameof(PortAudioStreamingPlayer));
        }
    }

    private sealed class AudioBuffer
    {
        public AudioBuffer(Memory<float> data, AudioSegment segment)
        {
            Data    = data;
            Segment = segment;
        }

        public Memory<float> Data { get; }

        public AudioSegment Segment { get; }

        public int Position { get; private set; }

        public int Remaining => Data.Length - Position;

        public bool IsFinished => Position >= Data.Length;

        public void Advance(int count) { Position += count; }
    }
}