using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private const int BargeInDetectionMinLength = 5;

    private static readonly TimeSpan BargeInDetectionDuration = TimeSpan.FromMilliseconds(400);

    private readonly string _context = "Relaxed discussion in discord voice chat.";

    private readonly IChatEngine _llmEngine;

    private readonly ILogger _logger;

    private readonly CancellationTokenSource _mainCts = new();

    private readonly IMicrophone _microphone;

    private readonly StringBuilder _pendingTranscript = new();

    private readonly StringBuilder _potentialBargeInText = new();

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

    private string _currentSpeaker = "User";

    private TaskCompletionSource<bool>? _firstTokenTcs;

    private bool _isInStreamingState = false;

    private DateTimeOffset? _potentialBargeInStartTime;

    private ConversationState _state = ConversationState.Idle;

    private DateTimeOffset? _transcriptionStartTimestamp;

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

        _transcriptionChannel = Channel.CreateBounded<TranscriptionSegment>(
                                                                            new BoundedChannelOptions(100) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });

        _visualQaService.StartAsync().ConfigureAwait(false);

        _transcriptionTask = RunTranscriptionLoopAsync(_mainCts.Token);
        _processingTask    = ProcessTranscriptionsAsync(_mainCts.Token);

        _logger.LogInformation("ConversationManager initialized and ready");
    }

    public IAggregatedStreamingAudioPlayer AudioPlayer { get; }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing ConversationManager...");
        try
        {
            if ( !_mainCts.IsCancellationRequested )
            {
                await _mainCts.CancelAsync();
            }

            await CancelLlmProcessingAsync(true);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await Task.WhenAll(_transcriptionTask, _processingTask).WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout waiting for tasks during disposal.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during task wait in disposal.");
            }
        }
        catch (OperationCanceledException)
        {
            /* Expected */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ConversationManager disposal");
        }
        finally
        {
            _stateLock.Dispose();
            _activeLlmCts.Dispose();
            _mainCts.Dispose();
            _transcriptionChannel.Writer.TryComplete();
        }

        _logger.LogInformation("ConversationManager disposed successfully");
    }

    public void Execute(GL gl) { }

    private async Task RunTranscriptionLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting transcription loop");
        
        try
        {
            _microphone.StartRecording();
            await foreach ( var event_ in _transcriptor.TranscribeAsync(_microphone, cancellationToken) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch ( event_ )
                {
                    case RealtimeSegmentRecognized recognized:
                        _potentialBargeInStartTime = null;
                        _potentialBargeInText.Clear();
                        if ( !recognized.Segment.Metadata.TryGetValue("User", out var user) )
                        {
                            user = "User";
                        }

                        var segment = new TranscriptionSegment(recognized.Segment.Text, user, DateTimeOffset.Now);
                        await _transcriptionChannel.Writer.WriteAsync(segment, cancellationToken);

                        break;
                    case RealtimeSegmentRecognizing recognizing:
                        _logger.LogTrace("Recognizing: {Text}", recognizing.Segment.Text);
                        if ( _isInStreamingState )
                        {
                            if ( string.IsNullOrWhiteSpace(recognizing.Segment.Text) )
                            {
                                continue;
                            }

                            if ( _potentialBargeInStartTime == null )
                            {
                                _potentialBargeInStartTime = DateTimeOffset.Now;
                                _potentialBargeInText.Clear();
                                _logger.LogDebug("Potential barge-in started.");
                            }

                            _potentialBargeInText.Clear().Append(recognizing.Segment.Text);
                            var bargeInDuration = DateTimeOffset.Now - _potentialBargeInStartTime.Value;
                            if ( bargeInDuration >= BargeInDetectionDuration && _potentialBargeInText.Length >= BargeInDetectionMinLength )
                            {
                                _logger.LogInformation("Barge-in detected: Duration={Duration}ms, Length={Length}", bargeInDuration.TotalMilliseconds, _potentialBargeInText.Length);
                                await TriggerBargeInAsync(cancellationToken); // Fire and forget is okay here
                                _potentialBargeInStartTime = null;
                                _potentialBargeInText.Clear();
                            }
                        }
                        else
                        {
                            _potentialBargeInStartTime = null;
                            _potentialBargeInText.Clear();
                        }

                        // Cancel if processing and new speech before first token
                        if ( _state == ConversationState.Processing && _firstTokenTcs is { Task.IsCompleted: false } )
                        {
                            _logger.LogDebug("Cancelling LLM call due to new speech before first token");
                            // Don't await here, let the loop continue immediately
                            _ = CancelLlmProcessingAsync();
                        }

                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Transcription loop cancelled via main token.");
        }
        catch (ChannelClosedException)
        {
            _logger.LogInformation("Transcription channel closed, ending loop.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transcription loop");
        }
        finally
        {
            _microphone.StopRecording();
            _transcriptionChannel.Writer.TryComplete(new OperationCanceledException("Transcription loop ending."));
            _logger.LogInformation("Transcription loop completed");
        }
    }

    private async Task ProcessTranscriptionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting transcription processing");

        try
        {
            await foreach ( var segment in _transcriptionChannel.Reader.ReadAllAsync(cancellationToken) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ( _pendingTranscript.Length == 0 )
                {
                    _transcriptionStartTimestamp = segment.Timestamp;
                    _currentSpeaker              = segment.User;
                }

                _pendingTranscript.Append(segment.Text);
                _logger.LogDebug("Received final transcript segment: User='{User}', Text='{Text}'", segment.User, segment.Text);
                await HandleTranscriptionSegmentAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Transcription processing cancelled via main token.");
        }
        catch (ChannelClosedException ex)
        {
            _logger.LogInformation("Transcription channel closed, ending processing loop. Reason: {Exception}", ex.InnerException?.Message ?? "Channel completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcriptions");
        }
        finally
        {
            _logger.LogInformation("Transcription processing loop completed.");
        }
    }

    private async Task HandleTranscriptionSegmentAsync(CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);
        
        try
        {
            var currentState = _state;
            _logger.LogDebug("HandleTranscriptionSegmentAsync entered. Current state: {State}", currentState);

            switch ( currentState )
            {
                case ConversationState.Idle:
                    _logger.LogInformation("Idle state: Transitioning to Processing and starting LLM task.");
                    await TransitionToStateAsync(ConversationState.Processing);
                    _ = StartLlmProcessingTaskAsync(CancellationToken.None);

                    break;

                case ConversationState.Processing:
                case ConversationState.Streaming:
                case ConversationState.Cancelling:
                    _logger.LogDebug("State is {State}. Appending transcript. No new task started.", currentState);

                    break;
            }
        }
        // No catch block here needed as ProcessTranscriptionsAsync has one,
        // but ensure lock is always released.
        finally
        {
            _stateLock.Release();
            _logger.LogDebug("HandleTranscriptionSegmentAsync finished.");
        }
    }

    /// <summary>
    ///     Wrapper method to start StartLlmProcessingAsync via Task.Run with robust error handling.
    /// </summary>
    private Task StartLlmProcessingTaskAsync(CancellationToken externalCancellationToken)
    {
        return Task.Run(async () =>
                        {
                            try
                            {
                                _logger.LogDebug("Task.Run: Executing StartLlmProcessingAsync.");
                                await StartLlmProcessingAsync(externalCancellationToken);
                                _logger.LogDebug("Task.Run: StartLlmProcessingAsync completed.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Unhandled exception directly within StartLlmProcessingAsync execution task.");
                                await _stateLock.WaitAsync();
                                try
                                {
                                    _logger.LogWarning("Attempting to reset state to Idle due to StartLlmProcessingAsync task failure.");
                                    _pendingTranscript.Clear();
                                    _transcriptionStartTimestamp = null;
                                    _activeProcessingTask        = null; 
                                    await TransitionToStateAsync(ConversationState.Idle);
                                }
                                catch (Exception lockEx)
                                {
                                    _logger.LogError(lockEx, "Failed to acquire lock or reset state after StartLlmProcessingAsync task failure.");
                                }
                                finally
                                {
                                    if ( _stateLock.CurrentCount == 0 )
                                    {
                                        _stateLock.Release();
                                    }
                                }
                            }
                        });
    }

    /// <summary>
    ///     Starts processing the current accumulated transcript with the LLM.
    ///     Sets up the processing task and its continuation handler.
    /// </summary>
    private async Task StartLlmProcessingAsync(CancellationToken externalCancellationToken)
    {
        _logger.LogDebug("StartLlmProcessingAsync entered.");
        using var linkedCts     = CancellationTokenSource.CreateLinkedTokenSource(_mainCts.Token, externalCancellationToken);
        var       combinedToken = linkedCts.Token;

        _activeLlmCts?.Dispose(); 
        _activeLlmCts = CancellationTokenSource.CreateLinkedTokenSource(combinedToken);
        var llmCancellationToken = _activeLlmCts.Token;

        string transcript;
        await _stateLock.WaitAsync(combinedToken);
        try
        {
            transcript = _pendingTranscript.ToString();
            if ( string.IsNullOrWhiteSpace(transcript) )
            {
                _logger.LogWarning("StartLlmProcessingAsync: Empty transcript. Returning to Idle.");
                await TransitionToStateAsync(ConversationState.Idle);

                return;
            }

            _logger.LogInformation("StartLlmProcessingAsync: Processing transcript ({Length} chars): '{Transcript}'", transcript.Length, transcript.Substring(0, Math.Min(transcript.Length, 100)));
        }
        finally
        {
            _stateLock.Release();
        }

        Task? currentProcessingTask = null;
        try
        {
            _logger.LogDebug("StartLlmProcessingAsync: Calling ProcessLlmRequestAsync.");
            // --- CRITICAL: Assign the task immediately ---
            currentProcessingTask = ProcessLlmRequestAsync(transcript, llmCancellationToken);
            _activeProcessingTask = currentProcessingTask;
            _logger.LogDebug("StartLlmProcessingAsync: ProcessLlmRequestAsync called, task assigned.");

            _logger.LogDebug("StartLlmProcessingAsync: Setting up continuation task.");
            _ = currentProcessingTask.ContinueWith(
                                                   continuationAction: task => _ = HandleProcessingTaskCompletion(task),
                                                   cancellationToken: CancellationToken.None,        
                                                   continuationOptions: TaskContinuationOptions.None, 
                                                   scheduler: TaskScheduler.Default
                                                  );

            _logger.LogDebug("StartLlmProcessingAsync: Continuation task setup complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartLlmProcessingAsync: Synchronous exception during ProcessLlmRequestAsync call or task assignment.");

            await _stateLock.WaitAsync(); // Use CancellationToken.None? Or combinedToken? Let's use None for cleanup.
            try
            {
                _logger.LogWarning("StartLlmProcessingAsync: Resetting state to Idle due to synchronous startup failure.");
                _pendingTranscript.Clear();
                _transcriptionStartTimestamp = null;
                _activeProcessingTask        = null;
                await TransitionToStateAsync(ConversationState.Idle);
            }
            finally
            {
                _stateLock.Release();
            }
            // Do not re-throw, let the method complete. The error is logged.
        }

        _logger.LogDebug("StartLlmProcessingAsync finished.");
    }

    /// <summary>
    ///     Handles the completion (success, fault, cancellation) of the LLM processing task.
    ///     This runs as a Task Continuation.
    /// </summary>
    private async Task HandleProcessingTaskCompletion(Task completedTask)
    {
        _logger.LogDebug("HandleProcessingTaskCompletion entered. Task Status: {Status}", completedTask.Status);
        await _stateLock.WaitAsync(CancellationToken.None);
        try
        {
            // This prevents stale continuations from messing up the state if a new task started quickly.
            if ( completedTask != _activeProcessingTask )
            {
                _logger.LogWarning("HandleProcessingTaskCompletion: Stale continuation detected for TaskId {CompletedTaskId}. Current active TaskId is {ActiveTaskId}. Ignoring.",
                                   completedTask.Id, _activeProcessingTask?.Id ?? -1);

                return; 
            }

            var finalState        = ConversationState.Idle; 
            var restartProcessing = false;                 

            switch ( completedTask.Status )
            {
                case TaskStatus.Faulted:
                    _logger.LogError(completedTask.Exception?.Flatten().InnerExceptions.FirstOrDefault(), "LLM processing task failed.");
                    _pendingTranscript.Clear(); 
                    _transcriptionStartTimestamp = null;

                    break;

                case TaskStatus.Canceled:
                    _logger.LogInformation("LLM processing task was cancelled. Current state: {State}, Pending transcript length: {Length}", _state, _pendingTranscript.Length);

                    if ( _pendingTranscript.Length > 0 )
                    {
                        _logger.LogInformation("HandleProcessingTaskCompletion: Restarting processing after cancellation as pending transcript exists.");
                        finalState        = ConversationState.Processing;
                        restartProcessing = true;                       
                    }
                    else
                    {
                        _logger.LogInformation("HandleProcessingTaskCompletion: Cancelled with no pending transcript. Transitioning to Idle.");
                        _pendingTranscript.Clear(); 
                        _transcriptionStartTimestamp = null;
                    }

                    break;

                case TaskStatus.RanToCompletion:
                    _logger.LogDebug("LLM processing task completed successfully.");
                    _pendingTranscript.Clear(); 
                    _transcriptionStartTimestamp = null;

                    break;

                default:
                    _logger.LogWarning("HandleProcessingTaskCompletion: Unexpected task status {Status}", completedTask.Status);
                    _pendingTranscript.Clear(); 
                    _transcriptionStartTimestamp = null;

                    break;
            }

            await TransitionToStateAsync(finalState);

            // Clean up the task reference *only if* we are not restarting immediately
            // If restarting, the new StartLlmProcessingAsync will overwrite _activeProcessingTask
            if ( !restartProcessing )
            {
                _logger.LogDebug("HandleProcessingTaskCompletion: Clearing active processing task reference.");
                _activeProcessingTask = null;
            }
            else
            {
                _logger.LogDebug("HandleProcessingTaskCompletion: Keeping active processing task reference as restart is pending.");
            }

            if ( restartProcessing )
            {
                _logger.LogDebug("HandleProcessingTaskCompletion: Initiating processing restart task.");
                _ = StartLlmProcessingTaskAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error within HandleProcessingTaskCompletion continuation.");

            try
            {
                _pendingTranscript.Clear();
                _transcriptionStartTimestamp = null;
                _activeProcessingTask        = null; 
                await TransitionToStateAsync(ConversationState.Idle);
            }
            catch (Exception recoveryEx)
            {
                _logger.LogError(recoveryEx, "Failed to recover state to Idle within HandleProcessingTaskCompletion catch block.");
            }
        }
        finally
        {
            _stateLock.Release();
            _logger.LogDebug("HandleProcessingTaskCompletion finished.");
        }
    }

    private async Task ProcessLlmRequestAsync(string transcript, CancellationToken cancellationToken)
    {
        _logger.LogDebug("ProcessLlmRequestAsync entered for transcript: '{StartOfTranscript}...'", transcript.Substring(0, Math.Min(transcript.Length, 50)));
        var stopwatch = Stopwatch.StartNew();
        _firstTokenTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            // 1. Get LLM Response Stream
            _logger.LogDebug("Requesting LLM stream...");
            var textStream = _llmEngine.GetStreamingChatResponseAsync(
                                                                      new ChatMessage(_currentSpeaker, transcript),
                                                                      new InjectionMetadata(
                                                                                            _topics.AsReadOnly(),
                                                                                            _context,
                                                                                            _visualQaService.ScreenCaption ?? string.Empty),
                                                                      cancellationToken: cancellationToken);

            // Wrap stream for first token detection
            var (firstTokenDetectedStream, firstTokenTask) = WrapWithFirstTokenDetection(textStream, stopwatch, cancellationToken);

            // Start a task to transition state once the first token arrives
            var stateTransitionTask = firstTokenTask.ContinueWith(async _ =>
                                                                  {
                                                                      _logger.LogDebug("First token detected task continuation running.");
                                                                      await _stateLock.WaitAsync(CancellationToken.None);
                                                                      try
                                                                      {
                                                                          if ( _state == ConversationState.Processing )
                                                                          {
                                                                              _logger.LogInformation("First token received, transitioning state Processing -> Streaming.");
                                                                              await TransitionToStateAsync(ConversationState.Streaming);
                                                                          }
                                                                          else
                                                                          {
                                                                              _logger.LogWarning("First token received, but state was already {State}. No transition.", _state);
                                                                          }
                                                                      }
                                                                      finally
                                                                      {
                                                                          _stateLock.Release();
                                                                      }
                                                                  }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

            // 2. Synthesize Speech Stream
            _logger.LogDebug("Requesting TTS stream...");
            var audioSegments = _ttsSynthesizer.SynthesizeStreamingAsync(firstTokenDetectedStream, cancellationToken: cancellationToken);

            // Wrap audio for end-to-end latency tracking
            var latencyTrackedAudio = WrapAudioSegments(audioSegments, cancellationToken);

            // 3. Play Audio Stream
            _logger.LogDebug("Starting audio playback...");
            await AudioPlayer.StartPlaybackAsync(latencyTrackedAudio, cancellationToken);

            // Await the state transition task to ensure it completes if first token arrived.
            // Use a timeout to prevent hanging if first token never arrives but playback finishes?
            // Or rely on cancellation? Let's rely on cancellation for now.
            await stateTransitionTask.WaitAsync(cancellationToken);

            _logger.LogInformation("Audio playback completed naturally.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("ProcessLlmRequestAsync cancelled.");
            _firstTokenTcs?.TrySetCanceled(cancellationToken);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM processing, TTS, or playback within ProcessLlmRequestAsync.");
            _firstTokenTcs?.TrySetException(ex);

            throw; 
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogDebug("ProcessLlmRequestAsync finished execution. Elapsed: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            // Reset TCS for the next run - do this in the continuation or StartLlmProcessing?
            // Let's reset it here for now, assuming ProcessLlmRequestAsync represents one full attempt.
            _firstTokenTcs = null;
        }
    }

    private (IAsyncEnumerable<string> Stream, Task FirstTokenTask) WrapWithFirstTokenDetection(
        IAsyncEnumerable<string> source, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var tcs = _firstTokenTcs ?? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstTokenTcs = tcs;

        async IAsyncEnumerable<string> WrappedStream()
        {
            var firstChunkProcessed = false;
            await foreach ( var chunk in source.WithCancellation(cancellationToken) )
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ( !firstChunkProcessed && !string.IsNullOrEmpty(chunk) )
                {
                    if ( !tcs.Task.IsCompleted ) 
                    {
                        stopwatch.Stop();
                        _logger.LogInformation("First token latency: {Latency}ms", stopwatch.ElapsedMilliseconds);
                        tcs.TrySetResult(true);
                    }

                    firstChunkProcessed = true;
                }

                yield return chunk;
            }

            if ( !firstChunkProcessed )
            {
                _logger.LogWarning("LLM stream completed without yielding any non-empty chunks.");
                tcs.TrySetCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }
        }

        return (WrappedStream(), tcs.Task);
    }

    private async IAsyncEnumerable<AudioSegment> WrapAudioSegments(
        IAsyncEnumerable<AudioSegment> source, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var firstSegment = true;
        await foreach ( var segment in source.WithCancellation(cancellationToken) )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ( firstSegment )
            {
                firstSegment = false;
                if ( _transcriptionStartTimestamp.HasValue )
                {
                    var latency = DateTimeOffset.Now - _transcriptionStartTimestamp.Value;
                    _logger.LogInformation("End-to-end latency (to first audio segment): {Latency}ms", latency.TotalMilliseconds);
                }
            }

            yield return segment;
        }
    }

    private async Task TriggerBargeInAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("TriggerBargeInAsync entered.");
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            if ( _state != ConversationState.Streaming )
            {
                _logger.LogWarning("Barge-in trigger attempted but state was {State}. Aborting.", _state);

                return;
            }

            _logger.LogInformation("Triggering Barge-In: Cancelling current response and stopping audio.");
            await TransitionToStateAsync(ConversationState.Cancelling);
            await AudioPlayer.StopPlaybackAsync(); 
            await CancelLlmProcessingAsync();  
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled during TriggerBargeInAsync.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during barge-in trigger execution.");
        }
        finally
        {
            _stateLock.Release();
        }

        _logger.LogDebug("TriggerBargeInAsync finished.");
    }

    private async Task CancelLlmProcessingAsync(bool stopAudio = false)
    {
        _logger.LogInformation("Attempting to cancel current LLM processing... StopAudio={StopAudio}", stopAudio);
        var ctsToCancel = _activeLlmCts;

        if ( ctsToCancel is { IsCancellationRequested: false } )
        {
            _logger.LogDebug("Signalling cancellation via CancellationTokenSource.");
            ctsToCancel.Cancel();
        }
        else
        {
            _logger.LogDebug("Cancellation already requested or no active CTS.");
        }

        if ( stopAudio )
        {
            try
            {
                await AudioPlayer.StopPlaybackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during explicit audio stop on cancellation.");
            }
        }

        var taskToWait = _activeProcessingTask;
        if ( taskToWait is { IsCompleted: false } /* && task was associated with ctsToCancel - hard to check */ )
        {
            _logger.LogDebug("Waiting briefly for active processing task ({TaskId}) to observe cancellation...", taskToWait.Id);
            await Task.WhenAny(taskToWait, Task.Delay(100));
            _logger.LogDebug("Brief wait completed. Task Status: {Status}", taskToWait.Status);
        }
        else
        {
            _logger.LogDebug("No active/incomplete task found to wait for, or no CTS was cancelled.");
        }
    }

    private Task TransitionToStateAsync(ConversationState newState)
    {
        var oldState = _state;
        if ( oldState == newState )
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("State transition: {OldState} -> {NewState}", oldState, newState);
        _state = newState;

        var wasStreaming = _isInStreamingState;
        _isInStreamingState = newState == ConversationState.Streaming;

        if ( wasStreaming && !_isInStreamingState )
        {
            _logger.LogDebug("Exiting Streaming state, resetting barge-in tracking.");
            _potentialBargeInStartTime = null;
            _potentialBargeInText.Clear();
        }

        return Task.CompletedTask;
    }

    private enum ConversationState
    {
        Idle,

        Processing,

        Streaming,

        Cancelling
    }

    private readonly record struct TranscriptionSegment(string Text, string User, DateTimeOffset Timestamp);
}