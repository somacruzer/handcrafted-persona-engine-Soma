using Microsoft.Extensions.Logging;

using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Live2D.App;
using PersonaEngine.Lib.Live2D.Framework.Motion;

namespace PersonaEngine.Lib.Live2D.Behaviour.Emotion;

/// <summary>
///     Manages Live2D model animations and expressions synchronized with detected speech emotions from audio playback.
/// </summary>
/// <remarks>
///     Responsibilities include:
///     - Mapping detected emotion events (e.g., emojis from <see cref="IEmotionService" />) to Live2D expressions and
///     motions.
///     - Applying expressions for a configurable duration (<see cref="EXPRESSION_HOLD_DURATION_SECONDS" />) before
///     potentially reverting to neutral.
///     - Playing specific emotion-driven motions or a neutral "talking" motion during audio playback.
///     - Handling emotion timing based on the audio player's current position.
///     - Persisting the last triggered expression/motion until the hold time expires or it's replaced.
///     This service assumes LipSync, Blinking, and Idle animations are managed by other services with appropriate
///     priorities.
///     It requires an <see cref="IEmotionService" /> for timed emotion data and an
///     <see cref="IStreamingAudioPlayerHost" /> for playback state and time.
/// </remarks>
public class EmotionAnimationService : ILive2DAnimationService
{
    #region Constructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="EmotionAnimationService" />.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="emotionService">The service providing emotion data for audio segments.</param>
    public EmotionAnimationService(ILogger<EmotionAnimationService> logger, IEmotionService emotionService)
    {
        _logger         = logger ?? throw new ArgumentNullException(nameof(logger));
        _emotionService = emotionService ?? throw new ArgumentNullException(nameof(emotionService));
    }

    #endregion

    #region Nested Types

    /// <summary> Defines the Live2D actions associated with a detected emotion identifier. </summary>
    private record EmotionMapping(string? ExpressionId, string? MotionGroup);

    #endregion

    #region Configuration

    // How long to hold an expression after it's triggered before reverting to neutral (if playback stops or no new emotion overrides it).
    private const float EXPRESSION_HOLD_DURATION_SECONDS = 3.0f;

    // Expression ID for the neutral state.
    private const string NEUTRAL_EXPRESSION_ID = "neutral";

    // Motion group used for the default talking animation when no specific emotion motion is active.
    private const string NEUTRAL_TALKING_MOTION_GROUP = "Talking";

    // Priority for motions triggered by specific emotions. Should typically be high to override idle/neutral talking.
    private const MotionPriority EMOTION_MOTION_PRIORITY = MotionPriority.PriorityForce;

    // Priority for the neutral talking motion. Should be lower than emotion motions but higher than idle.
    private const MotionPriority NEUTRAL_TALKING_MOTION_PRIORITY = MotionPriority.PriorityNormal;

    // Defines the mapping between detected emotion identifiers (e.g., emojis) and Live2D assets.
    // TODO: Consider loading this from an external configuration file for customization.
    private static readonly Dictionary<string, EmotionMapping> EmotionMap = new() {
                                                                                      // Positive Emotions
                                                                                      { "😊", new EmotionMapping("happy", "Happy") },
                                                                                      { "🤩", new EmotionMapping("excited_star", "Excited") },
                                                                                      { "😎", new EmotionMapping("cool", "Confident") },
                                                                                      { "😏", new EmotionMapping("smug", "Confident") },
                                                                                      { "💪", new EmotionMapping("determined", "Confident") },
                                                                                      // Reactive Emotions
                                                                                      { "😳", new EmotionMapping("embarrassed", "Nervous") },
                                                                                      { "😲", new EmotionMapping("shocked", "Surprised") },
                                                                                      { "🤔", new EmotionMapping("thinking", "Thinking") },
                                                                                      { "👀", new EmotionMapping("suspicious", "Thinking") },
                                                                                      // Negative Emotions
                                                                                      { "😤", new EmotionMapping("frustrated", "Angry") },
                                                                                      { "😢", new EmotionMapping("sad", "Sad") },
                                                                                      { "😅", new EmotionMapping("awkward", "Nervous") },
                                                                                      { "🙄", new EmotionMapping("dismissive", "Annoyed") },
                                                                                      // Expressive Reactions
                                                                                      { "💕", new EmotionMapping("adoring", "Happy") },
                                                                                      { "😂", new EmotionMapping("laughing", "Happy") },
                                                                                      { "🔥", new EmotionMapping("passionate", "Excited") },
                                                                                      { "✨", new EmotionMapping("sparkle", "Happy") }
                                                                                      // Neutral (can map if specific neutral actions are needed, otherwise handled implicitly)
                                                                                      // { "😐", new EmotionMapping(NEUTRAL_EXPRESSION_ID, null) } // Example if needed
                                                                                  };

    #endregion

    #region Dependencies

    private readonly ILogger<EmotionAnimationService> _logger;

    private readonly IEmotionService _emotionService;

    private IStreamingAudioPlayerHost? _audioPlayerHost;

    #endregion

    #region State

    private LAppModel? _model;

    private readonly List<EmotionTiming> _activeEmotions = new(); // Emotions for the current audio segment

    private readonly HashSet<string> _availableExpressions = new();

    private readonly HashSet<string> _availableMotionGroups = new();

    private int _currentEmotionIndex = -1; // Index of the currently active timed emotion in _activeEmotions

    private string? _triggeredEmotionEmoji = null; // The last emotion emoji triggered by timing

    private string? _activeExpressionId = null; // The expression currently *set* on the model

    private float _timeSinceExpressionSet = 0.0f; // Time since the current expression was applied

    private CubismMotionQueueEntry? _currentEmotionMotionEntry = null; // Handle for the active *emotion-specific* motion

    private CubismMotionQueueEntry? _neutralTalkingMotionEntry = null; // Handle for the active *neutral talking* motion

    private bool _isStarted = false;

    private bool _isPlaying = false;

    private bool _isSubscribed = false;

    private bool _disposed = false;

    #endregion

    #region ILive2DAnimationService Implementation

    /// <summary>
    ///     Subscribes this service to receive playback events from the specified audio player host.
    /// </summary>
    /// <param name="audioPlayerHost">The audio player host to subscribe to.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="audioPlayerHost" /> is null.</exception>
    public void SubscribeToAudioPlayerHost(IStreamingAudioPlayerHost audioPlayerHost)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(audioPlayerHost);

        if ( _audioPlayerHost == audioPlayerHost )
        {
            return;
        }

        UnsubscribeFromCurrentHost();

        _audioPlayerHost                     =  audioPlayerHost;
        _audioPlayerHost.OnPlaybackStarted   += HandlePlaybackStarted;
        _audioPlayerHost.OnPlaybackCompleted += HandlePlaybackCompleted;
        _isSubscribed                        =  true;

        _logger.LogDebug("Subscribed to Audio Player Host.");
    }

    /// <summary>
    ///     Starts the animation service for the specified Live2D model.
    /// </summary>
    /// <param name="model">The Live2D model to animate.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="model" /> is null.</exception>
    public void Start(LAppModel model)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(model);

        _model = model;

        ValidateModelAssets();

        _isStarted = true;
        ResetEmotionState(true);
        _logger.LogInformation("EmotionAnimationService started for model.");
    }

    /// <summary>
    ///     Stops the animation service, resetting the model to a neutral state.
    /// </summary>
    public void Stop()
    {
        if ( !_isStarted )
        {
            return;
        }

        _isStarted = false;
        ResetEmotionState(true);
        _logger.LogInformation("EmotionAnimationService stopped.");
        // Note: Does not automatically unsubscribe from the audio host.
    }

    /// <summary>
    ///     Updates the animation state based on elapsed time and audio playback status. Called every frame.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame, in seconds.</param>
    public void Update(float deltaTime)
    {
        if ( !_isStarted || _model == null || _disposed || deltaTime <= 0.0f )
        {
            return;
        }

        UpdatePlaybackState();

        // Update expression hold timer and revert to neutral if expired
        _timeSinceExpressionSet += deltaTime;
        CheckExpressionTimeout();

        if ( _isPlaying )
        {
            HandlePlayingState();
        }
        else
        {
            HandleStoppedState();
        }
    }

    #endregion

    #region Core Logic & State Updates

    private void UpdatePlaybackState() { _isPlaying = _isSubscribed && _audioPlayerHost?.State == PlayerState.Playing; }

    private void CheckExpressionTimeout()
    {
        // Only revert if a non-neutral expression is active and its time is up.
        if ( _activeExpressionId != null && _activeExpressionId != NEUTRAL_EXPRESSION_ID &&
             _timeSinceExpressionSet >= EXPRESSION_HOLD_DURATION_SECONDS )
        {
            _logger.LogTrace("Expression '{ExpressionId}' hold time expired ({HoldDuration}s). Reverting to neutral.",
                             _activeExpressionId, EXPRESSION_HOLD_DURATION_SECONDS);

            ApplyNeutralExpression();
        }
    }

    private void HandlePlayingState()
    {
        if ( _model == null || _audioPlayerHost == null )
        {
            return;
        }

        var currentTime = _audioPlayerHost.CurrentTime;
        UpdateEmotionBasedOnTime(currentTime);

        // Manage motions: Prioritize emotion-specific motions, fallback to neutral talking.
        var isEmotionMotionActive = _currentEmotionMotionEntry is { Finished: false };

        if ( isEmotionMotionActive )
        {
            return;
        }

        _currentEmotionMotionEntry = null;

        // Start neutral talking motion if available and not already running.
        if ( _neutralTalkingMotionEntry is not (null or { Finished: true }) ||
             !_availableMotionGroups.Contains(NEUTRAL_TALKING_MOTION_GROUP) )
        {
            return;
        }

        // If an emotion motion *is* active, the neutral talking motion will be suppressed or overridden due to priority.
        _logger.LogTrace("Starting neutral talking motion (group: {MotionGroup}).", NEUTRAL_TALKING_MOTION_GROUP);
        _neutralTalkingMotionEntry = _model.StartRandomMotion(NEUTRAL_TALKING_MOTION_GROUP, NEUTRAL_TALKING_MOTION_PRIORITY);
    }

    private void HandleStoppedState()
    {
        // When stopped:
        // - Expression timeout is handled globally by CheckExpressionTimeout().
        // - Emotion-specific motions triggered before stopping will continue playing until finished
        //   or replaced by lower-priority idle animations (assumed to be handled elsewhere).
        // - Neutral talking motion should naturally finish or be stopped by idle animations.
        //   We don't explicitly stop _neutralTalkingMotionEntry here, letting priority manage it.
    }

    private void UpdateEmotionBasedOnTime(float currentTime)
    {
        if ( _activeEmotions.Count == 0 )
        {
            return;
        }

        // Find the latest emotion whose timestamp is less than or equal to the current time.
        var targetEmotionIndex = -1;
        for ( var i = 0; i < _activeEmotions.Count; i++ )
        {
            if ( _activeEmotions[i].Timestamp <= currentTime )
            {
                targetEmotionIndex = i;
            }
            else
            {
                break;
            }
        }

        if ( targetEmotionIndex != -1 && targetEmotionIndex != _currentEmotionIndex )
        {
            _currentEmotionIndex = targetEmotionIndex;
            var newEmotionEmoji = _activeEmotions[_currentEmotionIndex].Emotion;
            ApplyEmotion(newEmotionEmoji);
        }

        // Potential enhancement: If playback starts *before* the first timestamp, explicitly set neutral?
        // Current behavior lets the previous state persist until the first timed emotion.
    }

    private void ApplyEmotion(string? emotionEmoji)
    {
        if ( _model == null || emotionEmoji == _triggeredEmotionEmoji )
        {
            return;
        }

        _logger.LogDebug("Applying triggered emotion: {Emotion}", emotionEmoji ?? "Neutral (explicit)");
        _triggeredEmotionEmoji = emotionEmoji;

        EmotionMapping? mapping      = null;
        var             foundMapping = emotionEmoji != null && EmotionMap.TryGetValue(emotionEmoji, out mapping);

        var targetExpressionId = foundMapping ? mapping?.ExpressionId : NEUTRAL_EXPRESSION_ID;
        SetExpression(targetExpressionId);

        _currentEmotionMotionEntry = null;

        if ( foundMapping && !string.IsNullOrEmpty(mapping?.MotionGroup) )
        {
            if ( _availableMotionGroups.Contains(mapping.MotionGroup) )
            {
                _logger.LogTrace("Starting emotion motion from group: {MotionGroup} with priority {Priority}.",
                                 mapping.MotionGroup, EMOTION_MOTION_PRIORITY);

                // Start the motion; priority should interrupt lower-priority motions like neutral talking or idle.
                _currentEmotionMotionEntry = _model.StartRandomMotion(mapping.MotionGroup, EMOTION_MOTION_PRIORITY);
                if ( _currentEmotionMotionEntry == null )
                {
                    _logger.LogWarning("Could not start motion for group '{MotionGroup}'. Is the group empty or definition invalid?", mapping.MotionGroup);
                }
            }
            else
            {
                _logger.LogWarning("Motion group '{MotionGroup}' for emotion '{Emotion}' not found in the current model.", mapping.MotionGroup, emotionEmoji);
            }
        }

        // If no specific motion is triggered here, HandlePlayingState might start the neutral talking motion.
    }

    private void SetExpression(string? expressionId)
    {
        var actualExpressionId = string.IsNullOrEmpty(expressionId) ? NEUTRAL_EXPRESSION_ID : expressionId;
        if ( string.IsNullOrEmpty(NEUTRAL_EXPRESSION_ID) && string.IsNullOrEmpty(expressionId) )
        {
            actualExpressionId = null;
        }

        if ( actualExpressionId == _activeExpressionId )
        {
            return;
        }

        var isTargetNeutral = string.IsNullOrEmpty(actualExpressionId) || actualExpressionId == NEUTRAL_EXPRESSION_ID;

        if ( isTargetNeutral )
        {
            ApplyNeutralExpression();
        }
        else if ( _availableExpressions.Contains(actualExpressionId!) )
        {
            _logger.LogTrace("Setting expression: {ExpressionId}", actualExpressionId);
            _model?.SetExpression(actualExpressionId!);
            _activeExpressionId     = actualExpressionId;
            _timeSinceExpressionSet = 0.0f;
        }
        else
        {
            _logger.LogWarning("Expression '{ExpressionId}' not found in model. Applying neutral instead.", actualExpressionId);
            ApplyNeutralExpression();
        }
    }

    private void ApplyNeutralExpression()
    {
        var neutralToApply = string.IsNullOrEmpty(NEUTRAL_EXPRESSION_ID) ? null : NEUTRAL_EXPRESSION_ID;

        if ( _activeExpressionId != neutralToApply )
        {
            // Log only if changing *to* neutral
            if ( neutralToApply != null || _activeExpressionId != null )
            {
                _logger.LogTrace("Setting neutral expression (was: {PreviousExpression}).", _activeExpressionId ?? "model default");
            }

            if ( neutralToApply != null && _availableExpressions.Contains(neutralToApply) )
            {
                _model?.SetExpression(neutralToApply);
            }
            else
            {
                _logger.LogError("Configured NEUTRAL_EXPRESSION_ID '{NeutralId}' is not available in the model, cannot apply.", NEUTRAL_EXPRESSION_ID);
            }

            _activeExpressionId = neutralToApply;
        }

        _timeSinceExpressionSet = 0.0f;
    }

    #endregion

    #region Event Handlers

    private void HandlePlaybackStarted(object? sender, AudioPlaybackEventArgs e)
    {
        if ( !_isStarted || _model == null )
        {
            return;
        }

        _logger.LogTrace("Audio playback started for segment {SegmentId}.", e.Segment.Id);
        _isPlaying = true;
        ResetEmotionState(false);

        try
        {
            var emotions = e.Segment.GetEmotions(_emotionService);

            if ( emotions.Any() )
            {
                _activeEmotions.AddRange(emotions.OrderBy(em => em.Timestamp));
                _logger.LogDebug("Loaded {Count} timed emotions for the segment.", _activeEmotions.Count);

                // Immediately check if an emotion should be applied based on the *current* playback time
                // (in case playback started mid-segment or very close to the first emotion).
                if ( _audioPlayerHost != null )
                {
                    UpdateEmotionBasedOnTime(_audioPlayerHost.CurrentTime);
                }
            }
            else
            {
                _logger.LogDebug("No timed emotions found for segment {SegmentId}.", e.Segment.Id);
                // If no emotions, the HandlePlayingState will likely trigger the neutral talking motion.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving or processing emotions for audio segment {SegmentId}.", e.Segment.Id);
        }
    }

    private void HandlePlaybackCompleted(object? sender, AudioPlaybackEventArgs e)
    {
        if ( !_isStarted )
        {
            return;
        }

        _logger.LogTrace("Audio playback completed for segment {SegmentId}.", e.Segment.Id);
        _isPlaying = false;
        // The Update loop (specifically HandleStoppedState and CheckExpressionTimeout)
        // will now manage the transition: expression times out, motions finish naturally.
    }

    #endregion

    #region State Management & Validation

    /// <summary> Resets the internal state related to timed emotions for a segment. </summary>
    /// <param name="forceNeutral">If true, immediately applies the neutral expression and clears triggered emotion state.</param>
    private void ResetEmotionState(bool forceNeutral)
    {
        _activeEmotions.Clear();
        _currentEmotionIndex = -1;

        if ( forceNeutral )
        {
            _logger.LogTrace("Forcing neutral state on reset.");
            ApplyNeutralExpression();
            _triggeredEmotionEmoji = null;

            // Explicitly stopping motions here could be added if priority alone isn't sufficient,
            // but it adds complexity. Relying on priority and natural finishing is preferred.
            // Consider model?.MotionManager.StopAllMotions() if a hard reset is needed, but be careful of side effects.
        }

        // If not forcing neutral, the current expression (_activeExpressionId) and its timer remain.
        // The _triggeredEmotionEmoji also remains until the next ApplyEmotion call.
        // This allows the last emotion's visual state to persist briefly after playback starts/stops.

        _logger.LogTrace("Timed emotion state reset (Active emotions cleared. Forced neutral: {ForceNeutral}).", forceNeutral);
    }

    private void ValidateModelAssets()
    {
        if ( _model == null )
        {
            return;
        }

        _logger.LogDebug("Validating configured emotion mappings against model assets...");

        _availableExpressions.Clear();
        _availableMotionGroups.Clear();

        var modelExpressions = new HashSet<string>(_model.Expressions);
        _logger.LogTrace("Model contains {Count} expressions.", modelExpressions.Count);

        var modelMotionGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ( var motionKey in _model.Motions )
        {
            // Attempt to parse group name (e.g., "Idle", "TapBody_Head", etc.) from the key
            var groupName = LAppModel.GetMotionGroupName(motionKey);
            if ( !string.IsNullOrEmpty(groupName) )
            {
                modelMotionGroups.Add(groupName);
            }
        }

        _logger.LogTrace("Model contains {Count} unique motion groups (e.g., {Examples}).",
                         modelMotionGroups.Count, string.Join(", ", modelMotionGroups.Take(5)) + (modelMotionGroups.Count > 5 ? "..." : ""));

        foreach ( var kvp in EmotionMap )
        {
            var emotion = kvp.Key;
            var mapping = kvp.Value;

            if ( !string.IsNullOrEmpty(mapping.ExpressionId) )
            {
                if ( modelExpressions.Contains(mapping.ExpressionId) )
                {
                    _availableExpressions.Add(mapping.ExpressionId);
                }
                else
                {
                    _logger.LogWarning("Validation: Expression '{ExpressionId}' (for emotion '{Emotion}') not found in model.", mapping.ExpressionId, emotion);
                }
            }

            if ( !string.IsNullOrEmpty(mapping.MotionGroup) )
            {
                if ( modelMotionGroups.Contains(mapping.MotionGroup) )
                {
                    _availableMotionGroups.Add(mapping.MotionGroup);
                }
                else
                {
                    _logger.LogWarning("Validation: Motion group '{MotionGroup}' (for emotion '{Emotion}') not found in model.", mapping.MotionGroup, emotion);
                }
            }
        }

        if ( !string.IsNullOrEmpty(NEUTRAL_EXPRESSION_ID) )
        {
            if ( modelExpressions.Contains(NEUTRAL_EXPRESSION_ID) )
            {
                _availableExpressions.Add(NEUTRAL_EXPRESSION_ID);
            }
            else
            {
                _logger.LogWarning("Configured NEUTRAL_EXPRESSION_ID ('{NeutralId}') not found in model expressions!", NEUTRAL_EXPRESSION_ID);
            }
        }
        else
        {
            _logger.LogInformation("NEUTRAL_EXPRESSION_ID is not configured; using model default for neutral.");
        }

        if ( !string.IsNullOrEmpty(NEUTRAL_TALKING_MOTION_GROUP) )
        {
            if ( modelMotionGroups.Contains(NEUTRAL_TALKING_MOTION_GROUP) )
            {
                _availableMotionGroups.Add(NEUTRAL_TALKING_MOTION_GROUP);
            }
            else
            {
                _logger.LogWarning("Validation: Configured NEUTRAL_TALKING_MOTION_GROUP ('{Group}') not found in model motion groups.", NEUTRAL_TALKING_MOTION_GROUP);
            }
        }
        else
        {
            _logger.LogInformation("NEUTRAL_TALKING_MOTION_GROUP is not configured; neutral talking animation disabled.");
        }

        _logger.LogDebug("Validation complete. Usable Expressions: {ExprCount}, Usable Motion Groups: {MotionCount}",
                         _availableExpressions.Count, _availableMotionGroups.Count);
    }
    
    private void UnsubscribeFromCurrentHost()
    {
        if ( _audioPlayerHost != null )
        {
            _audioPlayerHost.OnPlaybackStarted   -= HandlePlaybackStarted;
            _audioPlayerHost.OnPlaybackCompleted -= HandlePlaybackCompleted;
            _logger.LogDebug("Unsubscribed from Audio Player Host.");
        }

        _audioPlayerHost = null;
        _isSubscribed    = false;
        _isPlaying       = false;
        ResetEmotionState(true);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    ///     Releases the resources used by the <see cref="EmotionAnimationService" />.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if ( !_disposed )
        {
            if ( disposing )
            {
                _logger.LogDebug("Disposing EmotionAnimationService...");
                Stop();
                UnsubscribeFromCurrentHost();
                _activeEmotions.Clear();
                _availableExpressions.Clear();
                _availableMotionGroups.Clear();
                _model = null;
                _logger.LogInformation("EmotionAnimationService disposed.");
            }

            // Release unmanaged resources here if any (none in this class directly)

            _disposed = true;
        }
    }

    #endregion
}