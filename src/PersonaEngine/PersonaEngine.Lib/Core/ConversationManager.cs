using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using PersonaEngine.Lib.ASR.Transcriber;
using PersonaEngine.Lib.Audio;
using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.LLM;
using PersonaEngine.Lib.TTS.Synthesis;
using PersonaEngine.Lib.UI.Common;
using PersonaEngine.Lib.Vision;

using Silk.NET.OpenGL;

namespace PersonaEngine.Lib.Core;

public sealed class ConversationManager : IAsyncDisposable, IStartupTask
{
    private readonly string _context = "Relaxed discussion in discord voice chat.";

    private readonly IChatEngine _llmEngine;

    private readonly ILogger _logger;

    private readonly CancellationTokenSource _mainCts = new();

    private readonly IMicrophone _microphone;

    private readonly StringBuilder _pendingTranscript = new();

    private readonly Task _processingTask;

    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private readonly List<string> _topics = ["casual conversation"];

    private readonly Channel<TranscriptionSegment> _transcriptionChannel;

    private readonly Task _transcriptionTask;

    private readonly IRealtimeSpeechTranscriptor _transcriptor;

    private readonly ITtsEngine _ttsSynthesizer;

    private readonly IVisualQAService _visualQaService;

    private CancellationTokenSource _activeLlmCts = new();

    private Task? _activeProcessingTask;

    // Track the current speaker
    private string _currentSpeaker = "User";

    private TaskCompletionSource<bool>? _firstTokenTcs;

    private ConversationState _state = ConversationState.Idle;

    private DateTimeOffset? _transcriptionStartTimestamp;

    /// <summary>
    ///     Creates a new instance of the ConversationManager with the required dependencies.
    /// </summary>
    public ConversationManager(
        IMicrophone                     microphone,
        IRealtimeSpeechTranscriptor     transcriptor,
        IChatEngine                     llmEngine,
        ITtsEngine                      ttsSynthesizer,
        IVisualQAService                visualQaService,
        IAggregatedStreamingAudioPlayer audioPlayer,
        ILogger<ConversationManager>    logger)
    {
        _microphone      = microphone ?? throw new ArgumentNullException(nameof(microphone));
        _transcriptor    = transcriptor ?? throw new ArgumentNullException(nameof(transcriptor));
        _llmEngine       = llmEngine ?? throw new ArgumentNullException(nameof(llmEngine));
        _ttsSynthesizer  = ttsSynthesizer ?? throw new ArgumentNullException(nameof(ttsSynthesizer));
        _visualQaService = visualQaService ?? throw new ArgumentNullException(nameof(visualQaService));
        AudioPlayer      = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        _logger          = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure transcription channel with bounded capacity to avoid memory issues
        _transcriptionChannel = Channel.CreateBounded<TranscriptionSegment>(
                                                                            new BoundedChannelOptions(100) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });

        // Start the visual service
        _visualQaService.StartAsync().ConfigureAwait(false);

        // Start background tasks
        _transcriptionTask = RunTranscriptionLoopAsync(_mainCts.Token);
        _processingTask    = ProcessTranscriptionsAsync(_mainCts.Token);

        _logger.LogInformation("ConversationManager initialized and ready");
    }

    /// <summary>
    ///     Gets the audio player instance used for playback.
    /// </summary>
    public IStreamingAudioPlayer AudioPlayer { get; }

    /// <summary>
    ///     Asynchronously disposes of resources and stops all background tasks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing ConversationManager...");

        try
        {
            // Cancel all operations
            if ( !_mainCts.IsCancellationRequested )
            {
                await _mainCts.CancelAsync();
            }

            // Cancel any active LLM call
            await CancelLlmProcessingAsync();

            // Wait for tasks to complete with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Task.WhenAny(
                               Task.WhenAll(_transcriptionTask, _processingTask),
                               Task.Delay(Timeout.Infinite, timeoutCts.Token)
                              );
        }
        catch (OperationCanceledException)
        {
            // Expected during disposal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ConversationManager disposal");
        }

        _stateLock.Dispose();
        _activeLlmCts.Dispose();
        _mainCts.Dispose();

        _logger.LogInformation("ConversationManager disposed successfully");
    }

    public void Execute(GL gl) { }

    /// <summary>
    ///     Runs the main transcription loop, capturing audio and converting to text.
    /// </summary>
    private async Task RunTranscriptionLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting transcription loop");

        try
        {
            _microphone.StartRecording();

            await foreach ( var event_ in _transcriptor.TranscribeAsync(_microphone, cancellationToken) )
            {
                switch ( event_ )
                {
                    case RealtimeSegmentRecognized recognized:
                        if ( !recognized.Segment.Metadata.TryGetValue("User", out var user) )
                        {
                            user = "User";
                        }

                        var segment = new TranscriptionSegment(
                                                               recognized.Segment.Text,
                                                               user,
                                                               DateTimeOffset.Now);

                        await _transcriptionChannel.Writer.WriteAsync(segment, cancellationToken);

                        break;

                    case RealtimeSegmentRecognizing recognizing:
                        _logger.LogTrace("Recognizing: {Text}", recognizing.Segment.Text);

                        // If we're getting new speech while waiting for the first LLM token,
                        // cancel the current LLM call as the user is likely changing their request
                        if ( _state == ConversationState.Processing && _firstTokenTcs is { Task.IsCompleted: false } )
                        {
                            _logger.LogDebug("Cancelling LLM call due to new speech before first token");
                            await CancelLlmProcessingAsync();
                        }

                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transcription loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transcription loop");
        }
        finally
        {
            _microphone.StopRecording();
            _transcriptionChannel.Writer.Complete();
            _logger.LogInformation("Transcription loop completed");
        }
    }

    /// <summary>
    ///     Processes transcriptions from the channel and manages state transitions.
    /// </summary>
    private async Task ProcessTranscriptionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting transcription processing");

        try
        {
            await foreach ( var segment in _transcriptionChannel.Reader.ReadAllAsync(cancellationToken) )
            {
                if ( _pendingTranscript.Length == 0 )
                {
                    _transcriptionStartTimestamp = segment.Timestamp;
                    // Update the current speaker when starting a new transcript
                    _currentSpeaker = segment.User;
                }

                _pendingTranscript.Append(segment.Text);
                _logger.LogDebug("Received transcript segment: {Text}", segment.Text);

                await HandleTranscriptionSegmentAsync(segment, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transcription processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcriptions");
        }
    }

    /// <summary>
    ///     Handles a single transcription segment based on the current state.
    /// </summary>
    private async Task HandleTranscriptionSegmentAsync(TranscriptionSegment segment, CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            switch ( _state )
            {
                case ConversationState.Idle:
                    // Start processing when we receive input in idle state
                    await TransitionToStateAsync(ConversationState.Processing);
                    await StartLlmProcessingAsync(cancellationToken);

                    break;

                case ConversationState.Processing:
                    // If we get more input while processing, cancel and restart
                    if ( _activeProcessingTask != null )
                    {
                        await TransitionToStateAsync(ConversationState.Cancelling);
                        await CancelLlmProcessingAsync();
                    }

                    break;

                case ConversationState.Streaming:
                    // Ignore new input while streaming (barge-in not supported in this implementation)
                    _logger.LogDebug("Ignoring new input while streaming response");

                    break;

                case ConversationState.Cancelling:
                    // Just accumulate input while cancelling
                    _logger.LogDebug("Accumulating input while cancelling previous request");

                    break;
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    ///     Starts processing the current transcript with the LLM.
    /// </summary>
    private async Task StartLlmProcessingAsync(CancellationToken cancellationToken)
    {
        // Create a new cancellation token source for this specific LLM call
        _activeLlmCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var llmCancellationToken = _activeLlmCts.Token;

        var transcript = _pendingTranscript.ToString();
        _logger.LogInformation("Processing transcript with LLM: {Length} characters", transcript.Length);

        _activeProcessingTask = ProcessLlmRequestAsync(transcript, llmCancellationToken);

        // Use continuation to handle task completion
        _ = _activeProcessingTask.ContinueWith(
                                               async task =>
                                               {
                                                   await _stateLock.WaitAsync();
                                                   try
                                                   {
                                                       if ( task.IsFaulted )
                                                       {
                                                           _logger.LogError(task.Exception, "LLM processing failed");
                                                           await TransitionToStateAsync(ConversationState.Idle);
                                                       }
                                                       else if ( task.IsCanceled )
                                                       {
                                                           _logger.LogInformation("LLM processing was cancelled");

                                                           // If we're cancelling, we'll want to restart processing with accumulated input
                                                           if ( _state == ConversationState.Cancelling )
                                                           {
                                                               await TransitionToStateAsync(ConversationState.Processing);
                                                               await StartLlmProcessingAsync(CancellationToken.None);
                                                           }
                                                           else
                                                           {
                                                               await TransitionToStateAsync(ConversationState.Idle);
                                                           }
                                                       }
                                                       else
                                                       {
                                                           _logger.LogDebug("LLM processing completed successfully");
                                                           // We've already transitioned to Idle in the successful completion path

                                                           // Clear transcript only on successful completion
                                                           _pendingTranscript.Clear();
                                                           _transcriptionStartTimestamp = null;
                                                       }
                                                   }
                                                   finally
                                                   {
                                                       _stateLock.Release();
                                                   }
                                               },
                                               CancellationToken.None,
                                               TaskContinuationOptions.ExecuteSynchronously,
                                               TaskScheduler.Default);
    }

    /// <summary>
    ///     Processes an LLM request with the given transcript.
    /// </summary>
    private async Task ProcessLlmRequestAsync(string transcript, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _firstTokenTcs = new TaskCompletionSource<bool>();

            var textStream = _llmEngine.GetStreamingChatResponseAsync(
                                                                      new ChatMessage(_currentSpeaker, transcript),
                                                                      new InjectionMetadata(
                                                                                            _topics.AsReadOnly(),
                                                                                            _context,
                                                                                            _visualQaService.ScreenCaption ?? string.Empty),
                                                                      // new OpenAIPromptExecutionSettings { Temperature = 0.5, MaxTokens = 256 },
                                                                      cancellationToken: cancellationToken);

            // Wrap the stream to detect the first token for latency tracking
            var wrappedTextStream = WrapWithFirstTokenDetection(textStream);

            // Convert text to speech
            var audioSegments = _ttsSynthesizer.SynthesizeStreamingAsync(wrappedTextStream, cancellationToken: cancellationToken);

            // Wrap the audio segments for latency tracking
            var wrappedAudioSegments = WrapAudioSegments(audioSegments);

            // When we receive the first token, transition to streaming state
            var firstTokenTask = _firstTokenTcs.Task.ContinueWith(
                                                                  _ =>
                                                                  {
                                                                      stopwatch.Stop();
                                                                      _logger.LogInformation("First token latency: {Latency}ms", stopwatch.ElapsedMilliseconds);

                                                                      return TransitionToStateAsync(ConversationState.Streaming);
                                                                  },
                                                                  cancellationToken,
                                                                  TaskContinuationOptions.ExecuteSynchronously,
                                                                  TaskScheduler.Default);

            // Start audio playback
            await AudioPlayer.StartPlaybackAsync(wrappedAudioSegments, cancellationToken);

            // Wait for playback to complete
            await firstTokenTask;

            // Transition back to idle state when complete
            await TransitionToStateAsync(ConversationState.Idle);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LLM processing cancelled");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM processing");

            throw;
        }
        finally
        {
            _firstTokenTcs = null;
        }

        // Local function to wrap the text stream for first token detection
        async IAsyncEnumerable<string> WrapWithFirstTokenDetection(
            IAsyncEnumerable<string> source)
        {
            await foreach ( var chunk in source.WithCancellation(cancellationToken) )
            {
                if ( !_firstTokenTcs.Task.IsCompleted )
                {
                    _logger.LogDebug("First token received from LLM");
                    _firstTokenTcs.TrySetResult(true);
                }

                yield return chunk;
            }
        }

        // Local function to wrap audio segments for latency tracking
        async IAsyncEnumerable<AudioSegment> WrapAudioSegments(
            IAsyncEnumerable<AudioSegment> source)
        {
            var firstSegment = true;

            await foreach ( var segment in source.WithCancellation(cancellationToken) )
            {
                if ( firstSegment )
                {
                    firstSegment = false;

                    if ( _transcriptionStartTimestamp.HasValue )
                    {
                        var latency = DateTimeOffset.Now - _transcriptionStartTimestamp.Value;
                        _logger.LogInformation("End-to-end latency: {Latency}ms", latency.TotalMilliseconds);
                    }
                }

                yield return segment;
            }
        }
    }

    /// <summary>
    ///     Cancels the current LLM processing task.
    /// </summary>
    private Task CancelLlmProcessingAsync()
    {
        _logger.LogInformation("Cancelling current LLM processing");
        if ( !_activeLlmCts.IsCancellationRequested )
        {
            _activeLlmCts.Cancel();
        }

        _ = Task.Run(async () =>
                     {
                         try
                         {
                             if ( _activeProcessingTask != null )
                             {
                                 await Task.WhenAny(_activeProcessingTask, Task.Delay(100));
                             }
                         }
                         catch (Exception ex)
                         {
                             _logger.LogError(ex, "Error in background cancellation");
                         }
                     });

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Transitions to a new state with proper logging.
    /// </summary>
    private async Task TransitionToStateAsync(ConversationState newState)
    {
        // This method is called with the state lock already held

        var oldState = _state;
        if ( oldState == newState )
        {
            return;
        }

        _logger.LogInformation("State transition: {OldState} -> {NewState}", oldState, newState);
        _state = newState;

        // Special handling for transition to Processing state
        if ( newState == ConversationState.Processing && oldState == ConversationState.Cancelling )
        {
            _activeProcessingTask = null;
        }
    }

    private enum ConversationState
    {
        Idle,

        Processing,

        Streaming,

        Cancelling
    }

    /// <summary>
    ///     Represents a segment of transcribed speech.
    /// </summary>
    private readonly record struct TranscriptionSegment(
        string         Text,
        string         User,
        DateTimeOffset Timestamp);
}