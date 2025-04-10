using System.Diagnostics;

using Microsoft.Extensions.Logging;

using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Live2D.App;
using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.Live2D.Behaviour.LipSync;

/// <summary>
/// LipSync service for VBridger parameter conventions.
/// </summary>
public class VBridgerLipSyncService : ILive2DAnimationService
{
    #region Configuration Constants

    // Responsiveness vs. smoothness tuning parameters
    private const float SMOOTHING_FACTOR = 35.0f;         // How quickly parameters move towards target (higher = faster)
    private const float NEUTRAL_RETURN_FACTOR = 15.0f;    // How quickly parameters return to neutral when idle
    private const float CHEEK_PUFF_DECAY_FACTOR = 80.0f;  // How quickly CheekPuff returns to 0
    private const float NEUTRAL_THRESHOLD = 0.02f;        // Threshold for considering a value as neutral

    #endregion

    #region Parameter Names

    private static readonly string ParamMouthOpenY = "ParamMouthOpenY";
    private static readonly string ParamJawOpen = "ParamJawOpen";
    private static readonly string ParamMouthForm = "ParamMouthForm";             // Smile/Frown
    private static readonly string ParamMouthShrug = "ParamMouthShrug";           // Lip shrug/tension
    private static readonly string ParamMouthFunnel = "ParamMouthFunnel";         // Kissy/Funnel shape
    private static readonly string ParamMouthPuckerWiden = "ParamMouthPuckerWiden"; // Width
    private static readonly string ParamMouthPressLipOpen = "ParamMouthPressLipOpen"; // Lip Separation/Press
    private static readonly string ParamMouthX = "ParamMouthX";                   // Horizontal shift
    private static readonly string ParamCheekPuffC = "ParamCheekPuffC";           // Cheek Puff

    #endregion

    #region Dependencies and State

    private LAppModel? _model;
    private IStreamingAudioPlayerHost? _audioPlayerHost;

    private readonly List<TimedPhoneme> _activePhonemes = new List<TimedPhoneme>();
    private int _currentPhonemeIndex = -1;
    private bool _isSubscribed = false;
    private bool _isPlaying = false;
    private bool _isStarted = false;
    private bool _disposed = false;

    private PhonemePose _currentTargetPose = PhonemePose.Neutral;
    private PhonemePose _nextTargetPose = PhonemePose.Neutral;
    private float _interpolationT = 0f;

    private readonly Dictionary<string, float>       _currentParameterValues = new Dictionary<string, float>();
    private readonly Dictionary<string, PhonemePose> _phonemeMap;
    private readonly HashSet<char>                   _phonemeShapeIgnoreChars = ['ˈ', 'ˌ', 'ː']; // Ignore stress/length marks

    private readonly ILogger<VBridgerLipSyncService> _logger;
    
    #endregion


    public VBridgerLipSyncService(ILogger<VBridgerLipSyncService> logger)
    {
        _logger = logger;
        _phonemeMap  = InitializeMisakiPhonemeMap_Revised();
    }

    private void InitializeCurrentParameters()
    {
        if ( _model == null )
        {
            return;
        }
        
        var cubismModel = _model.Model;
        // Fetch initial values from the model
        _currentParameterValues[ParamMouthOpenY] = cubismModel.GetParameterValue(ParamMouthOpenY);
        _currentParameterValues[ParamJawOpen] = cubismModel.GetParameterValue(ParamJawOpen);
        _currentParameterValues[ParamMouthForm] = cubismModel.GetParameterValue(ParamMouthForm);
        _currentParameterValues[ParamMouthShrug] = cubismModel.GetParameterValue(ParamMouthShrug);
        _currentParameterValues[ParamMouthFunnel] = cubismModel.GetParameterValue(ParamMouthFunnel);
        _currentParameterValues[ParamMouthPuckerWiden] = cubismModel.GetParameterValue(ParamMouthPuckerWiden);
        _currentParameterValues[ParamMouthPressLipOpen] = cubismModel.GetParameterValue(ParamMouthPressLipOpen);
        _currentParameterValues[ParamMouthX] = cubismModel.GetParameterValue(ParamMouthX);
        _currentParameterValues[ParamCheekPuffC] = 0f;
    }

    #region ILipSyncService Implementation

    public void SubscribeToAudioPlayerHost(IStreamingAudioPlayerHost audioPlayerHost)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VBridgerLipSyncService));
        }

        if (_audioPlayerHost == audioPlayerHost)
        {
            return;
        }

        UnsubscribeFromCurrentHost();

        _audioPlayerHost = audioPlayerHost ?? throw new ArgumentNullException(nameof(audioPlayerHost));

        _audioPlayerHost.OnPlaybackStarted += HandlePlaybackStarted;
        _audioPlayerHost.OnPlaybackCompleted += HandlePlaybackCompleted;
        _isSubscribed = true;

        _logger.LogDebug("Subscribed to Audio Player.");
    }

     private void UnsubscribeFromCurrentHost()
    {
        if (_audioPlayerHost != null)
        {
            _audioPlayerHost.OnPlaybackStarted -= HandlePlaybackStarted;
            _audioPlayerHost.OnPlaybackCompleted -= HandlePlaybackCompleted;
            
            _logger.LogDebug("Unsubscribed from previous Audio Player.");
        }
        _audioPlayerHost = null;
        _isSubscribed = false;
        ResetState();
    }

     public void Start(LAppModel model)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VBridgerLipSyncService));
        }

        _model     = model;
        
        InitializeCurrentParameters();
        
        _isStarted                               = true;
        
        _logger.LogInformation("Started lip syncing.");
    }

    public void Stop()
    {
        _isStarted = false;
        
        _logger.LogInformation("Stopped lip syncing.");
    }

    #endregion

    #region Update Logic

     public void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f)
        {
            return;
        }

        if (_disposed)
        {
            return;
        }

        if ( _model == null )
        {
            return;
        }

        // If not active, smoothly return to neutral
        if (!_isStarted || !_isSubscribed || _audioPlayerHost == null)
        {
            SmoothParametersToNeutral(deltaTime);
            return;
        }

        float currentTime = _audioPlayerHost.CurrentTime;

        UpdateTargetPoses(currentTime); // Find current/next phoneme targets

        // Apply easing to the interpolation factor
        float easedT = EaseInOutQuad(_interpolationT);
        // Calculate the interpolated target pose for this exact frame
        PhonemePose frameTargetPose = PhonemePose.Lerp(_currentTargetPose, _nextTargetPose, easedT);

        // Smoothly move current parameters towards the frame's target pose
        SmoothParametersToTarget(frameTargetPose, deltaTime);

        // Apply the final smoothed values to the model
        ApplySmoothedParameters();
    }

    // Find the current and next phoneme poses based on audio time
    private void UpdateTargetPoses(float currentTime)
    {
        if ( _model == null )
        {
            return;
        }
        
        if (!_isPlaying || _activePhonemes.Count == 0)
        {
            // Not playing or no phonemes, target neutral
            _currentTargetPose = GetPoseFromCurrentValues();
            _nextTargetPose = PhonemePose.Neutral;
            _interpolationT = 0f;
            return;
        }

        // Find Current Phoneme Index - optimization: start from last known index
        int searchStartIndex = Math.Max(0, _currentPhonemeIndex);
        int foundIndex = -1;

        // Check if current time is still within the range of the last known phoneme
        if (_currentPhonemeIndex >= 0 && _currentPhonemeIndex < _activePhonemes.Count)
        {
            var ph = _activePhonemes[_currentPhonemeIndex];
            // Add small epsilon for end time to catch exact matches or slight overshoots
            if (currentTime >= ph.StartTime && currentTime < (ph.EndTime + 0.001))
            {
                foundIndex = _currentPhonemeIndex;
            }
        }

        // If not found in the current index, search the list
        if (foundIndex == -1)
        {
            for (int i = searchStartIndex; i < _activePhonemes.Count; i++)
            {
                var ph = _activePhonemes[i];
                if (currentTime >= ph.StartTime && currentTime < (ph.EndTime + 0.001))
                {
                    foundIndex = i;
                    break;
                }
            }
            // Special case: If time is exactly the end time of the last phoneme
            if (foundIndex == -1 && _activePhonemes.Count > 0 && Math.Abs(currentTime - _activePhonemes.Last().EndTime) < 0.01)
            {
                 foundIndex = _activePhonemes.Count - 1;
            }
        }

        // Update Poses and Interpolation Factor
        if (foundIndex != -1)
        {
            if (foundIndex != _currentPhonemeIndex)
            {
                 // We just entered a new phoneme - use current smoothed values as the starting point
                 _currentTargetPose = GetPoseFromCurrentValues();
                 _currentPhonemeIndex = foundIndex;
            }
            
            // If staying in the same phoneme, ensure current target IS the map target
            if (foundIndex == _currentPhonemeIndex && _interpolationT > 0) {
                 _currentTargetPose = GetPoseForPhoneme(_activePhonemes[_currentPhonemeIndex].Phoneme);
            } else {
                _currentTargetPose = GetPoseFromCurrentValues();
            }

            TimedPhoneme currentPh = _activePhonemes[_currentPhonemeIndex];

            // Look ahead to the next phoneme for the end target of the interpolation
            int nextIndex = _currentPhonemeIndex + 1;
            _nextTargetPose = (nextIndex < _activePhonemes.Count)
                                  ? GetPoseForPhoneme(_activePhonemes[nextIndex].Phoneme)
                                  : PhonemePose.Neutral; // Blend towards neutral at the very end

            // Calculate interpolation factor (0 to 1) representing progress within the current phoneme
            double duration = currentPh.EndTime - currentPh.StartTime;
            _interpolationT = (duration > 0.001) // Avoid division by zero
                                  ? (float)Math.Clamp((currentTime - currentPh.StartTime) / duration, 0.0, 1.0)
                                  : 1.0f; // If duration is tiny, snap to end
        }
        else
        {
            // Current time is outside any known phoneme (silence gap or before/after)
            _currentTargetPose = GetPoseFromCurrentValues();
            _nextTargetPose = PhonemePose.Neutral;
            _interpolationT = 0f;

            // Reset index if time has jumped significantly outside known range
            if (_activePhonemes.Any() &&
                (currentTime < _activePhonemes.First().StartTime - 0.2 ||
                 currentTime > _activePhonemes.Last().EndTime + 0.2))
            {
                _currentPhonemeIndex = -1;
            }
        }
    }

    // Get a PhonemePose representation of the current smoothed values
    private PhonemePose GetPoseFromCurrentValues()
    {
        return new PhonemePose(
            _currentParameterValues[ParamMouthOpenY],
            _currentParameterValues[ParamJawOpen],
            _currentParameterValues[ParamMouthForm],
            _currentParameterValues[ParamMouthShrug],
            _currentParameterValues[ParamMouthFunnel],
            _currentParameterValues[ParamMouthPuckerWiden],
            _currentParameterValues[ParamMouthPressLipOpen],
            _currentParameterValues[ParamMouthX],
            _currentParameterValues[ParamCheekPuffC]
        );
    }

    #endregion

    #region Parameter Smoothing

    // Smoothly interpolate current parameters towards the target pose
    private void SmoothParametersToTarget(PhonemePose targetPose, float deltaTime)
    {
        float smoothFactor = SMOOTHING_FACTOR * deltaTime;

        // Smooth most parameters towards the calculated frame target pose
        _currentParameterValues[ParamMouthOpenY] = Lerp(_currentParameterValues[ParamMouthOpenY], targetPose.MouthOpenY, smoothFactor);
        _currentParameterValues[ParamJawOpen] = Lerp(_currentParameterValues[ParamJawOpen], targetPose.JawOpen, smoothFactor);
        _currentParameterValues[ParamMouthForm] = Lerp(_currentParameterValues[ParamMouthForm], targetPose.MouthForm, smoothFactor);
        _currentParameterValues[ParamMouthShrug] = Lerp(_currentParameterValues[ParamMouthShrug], targetPose.MouthShrug, smoothFactor);
        _currentParameterValues[ParamMouthFunnel] = Lerp(_currentParameterValues[ParamMouthFunnel], targetPose.MouthFunnel, smoothFactor);
        _currentParameterValues[ParamMouthPuckerWiden] = Lerp(_currentParameterValues[ParamMouthPuckerWiden], targetPose.MouthPuckerWiden, smoothFactor);
        _currentParameterValues[ParamMouthPressLipOpen] = Lerp(_currentParameterValues[ParamMouthPressLipOpen], targetPose.MouthPressLipOpen, smoothFactor);
        _currentParameterValues[ParamMouthX] = Lerp(_currentParameterValues[ParamMouthX], targetPose.MouthX, smoothFactor);

        // Special handling for cheek puff - smooth towards target or decay to zero
        if (targetPose.CheekPuffC > NEUTRAL_THRESHOLD) // Target wants puff
        {
             _currentParameterValues[ParamCheekPuffC] = Lerp(_currentParameterValues[ParamCheekPuffC], targetPose.CheekPuffC, smoothFactor);
        }
        else // Target wants no puff, decay existing puff
        {
             float decayFactor = CHEEK_PUFF_DECAY_FACTOR * deltaTime;
             _currentParameterValues[ParamCheekPuffC] = Lerp(_currentParameterValues[ParamCheekPuffC], 0.0f, decayFactor);
        }
    }

    // Smoothly interpolate current parameters towards neutral position
    private void SmoothParametersToNeutral(float deltaTime)
    {
        if (IsApproximatelyNeutral())
        {
            return; // Already neutral, do nothing
        }

        float smoothFactor = NEUTRAL_RETURN_FACTOR * deltaTime;
        PhonemePose neutral = PhonemePose.Neutral;

        _currentParameterValues[ParamMouthOpenY] = Lerp(_currentParameterValues[ParamMouthOpenY], neutral.MouthOpenY, smoothFactor);
        _currentParameterValues[ParamJawOpen] = Lerp(_currentParameterValues[ParamJawOpen], neutral.JawOpen, smoothFactor);
        _currentParameterValues[ParamMouthForm] = Lerp(_currentParameterValues[ParamMouthForm], neutral.MouthForm, smoothFactor);
        _currentParameterValues[ParamMouthShrug] = Lerp(_currentParameterValues[ParamMouthShrug], neutral.MouthShrug, smoothFactor);
        _currentParameterValues[ParamMouthFunnel] = Lerp(_currentParameterValues[ParamMouthFunnel], neutral.MouthFunnel, smoothFactor);
        _currentParameterValues[ParamMouthPuckerWiden] = Lerp(_currentParameterValues[ParamMouthPuckerWiden], neutral.MouthPuckerWiden, smoothFactor);
        _currentParameterValues[ParamMouthPressLipOpen] = Lerp(_currentParameterValues[ParamMouthPressLipOpen], neutral.MouthPressLipOpen, smoothFactor);
        _currentParameterValues[ParamMouthX] = Lerp(_currentParameterValues[ParamMouthX], neutral.MouthX, smoothFactor);

        // Cheek puff decays quickly when returning to neutral
        float decayFactor = CHEEK_PUFF_DECAY_FACTOR * deltaTime;
        _currentParameterValues[ParamCheekPuffC] = Lerp(_currentParameterValues[ParamCheekPuffC], 0.0f, decayFactor);

        // Apply changes immediately when smoothing to neutral
        ApplySmoothedParameters();
    }

    // Check if all relevant parameters are close to their neutral values
    private bool IsApproximatelyNeutral()
    {
        PhonemePose neutral = PhonemePose.Neutral;
        return Math.Abs(_currentParameterValues[ParamMouthOpenY] - neutral.MouthOpenY) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamJawOpen] - neutral.JawOpen) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthForm] - neutral.MouthForm) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthShrug] - neutral.MouthShrug) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthFunnel] - neutral.MouthFunnel) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthPuckerWiden] - neutral.MouthPuckerWiden) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthPressLipOpen] - neutral.MouthPressLipOpen) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamMouthX] - neutral.MouthX) < NEUTRAL_THRESHOLD &&
               Math.Abs(_currentParameterValues[ParamCheekPuffC] - neutral.CheekPuffC) < NEUTRAL_THRESHOLD;
    }

    private void ApplySmoothedParameters()
    {
        if ( _model == null )
        {
            return;
        }
        
        try
        {
            var cubismModel = _model.Model;
            cubismModel.SetParameterValue(ParamMouthOpenY, _currentParameterValues[ParamMouthOpenY]);
            cubismModel.SetParameterValue(ParamJawOpen, _currentParameterValues[ParamJawOpen]);
            cubismModel.SetParameterValue(ParamMouthForm, _currentParameterValues[ParamMouthForm]);
            cubismModel.SetParameterValue(ParamMouthShrug, _currentParameterValues[ParamMouthShrug]);
            cubismModel.SetParameterValue(ParamMouthFunnel, _currentParameterValues[ParamMouthFunnel]);
            cubismModel.SetParameterValue(ParamMouthPuckerWiden, _currentParameterValues[ParamMouthPuckerWiden]);
            cubismModel.SetParameterValue(ParamMouthPressLipOpen, _currentParameterValues[ParamMouthPressLipOpen]);
            cubismModel.SetParameterValue(ParamMouthX, _currentParameterValues[ParamMouthX]);
            cubismModel.SetParameterValue(ParamCheekPuffC, _currentParameterValues[ParamCheekPuffC]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying Live2D parameters");
        }
    }

    #endregion

    #region Event Handlers and State Management

    private void HandlePlaybackStarted(object? sender, AudioPlaybackEventArgs e)
    {
        _logger.LogTrace("Playback Started.");
        ProcessAudioSegment(e.Segment); // Generate phoneme timings
        _isPlaying = true;
        _currentPhonemeIndex = -1; // Reset index for the new segment
        
        // Immediately evaluate the first pose to reduce start delay
        if (_audioPlayerHost != null)
        {
            UpdateTargetPoses(_audioPlayerHost.CurrentTime);
        }
    }

    private void HandlePlaybackCompleted(object? sender, AudioPlaybackEventArgs e)
    {
        _logger.LogTrace("Playback Completed.");
        _isPlaying = false;
    }

    private void ResetState()
    {
        _activePhonemes.Clear();
        _currentPhonemeIndex = -1;
        _isPlaying = false;
        _currentTargetPose = PhonemePose.Neutral;
        _nextTargetPose = PhonemePose.Neutral;
        _interpolationT = 0f;
        InitializeCurrentParameters();
    }

    #endregion

    #region Phoneme Processing

    private void ProcessAudioSegment(AudioSegment segment)
    {
        _activePhonemes.Clear();

        double lastEndTime = 0.0; // Track end time to handle potential gaps/overlaps

        foreach (var token in segment.Tokens)
        {
            if (string.IsNullOrEmpty(token.Phonemes) || !token.StartTs.HasValue || !token.EndTs.HasValue || token.EndTs.Value <= token.StartTs.Value)
            {
                continue; // Skip invalid tokens
            }

            // Basic sanitization/adjustment of timestamps
            double tokenStartTime = Math.Max(lastEndTime, token.StartTs.Value);
            double tokenEndTime = Math.Max(tokenStartTime, token.EndTs.Value);
            if (tokenEndTime <= tokenStartTime)
            {
                continue; // Skip zero/negative duration tokens
            }

            var phonemeChars = SplitPhonemes(token.Phonemes);
            if (phonemeChars.Count == 0)
            {
                continue;
            }

            double tokenDuration = tokenEndTime - tokenStartTime;
            double timePerPhoneme = tokenDuration / phonemeChars.Count;
            double currentPhonemeStartTime = tokenStartTime;

            foreach (var phChar in phonemeChars)
            {
                double phonemeEndTime = currentPhonemeStartTime + timePerPhoneme;
                _activePhonemes.Add(new TimedPhoneme
                {
                    Phoneme = phChar,
                    StartTime = currentPhonemeStartTime,
                    EndTime = phonemeEndTime
                });
                currentPhonemeStartTime = phonemeEndTime;
            }
            lastEndTime = currentPhonemeStartTime; // Update last end time
        }
        _activePhonemes.Sort((a, b) => a.StartTime.CompareTo(b.StartTime)); // Ensure sorted
    }

    // Split phoneme string, ignoring stress marks
    private List<string> SplitPhonemes(string phonemeString)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(phonemeString))
        {
            return result;
        }

        foreach (char c in phonemeString)
        {
            if (!_phonemeShapeIgnoreChars.Contains(c))
            {
                result.Add(c.ToString());
            }
        }
        return result;
    }

    // Get the defined pose for a given phoneme from the map
    private PhonemePose GetPoseForPhoneme(string phoneme)
    {
        if (_phonemeMap.TryGetValue(phoneme, out var pose))
        {
            return pose;
        }
        return PhonemePose.Neutral;
    }

    #endregion

    #region Helper Functions

    // Quadratic ease in/out function for smooth transitions
    private static float EaseInOutQuad(float t)
    {
        t = Math.Clamp(t, 0.0f, 1.0f);
        return t < 0.5f ? 2.0f * t * t : 1.0f - (float)Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0f;
    }

    // Linear interpolation between two values
    private static float Lerp(float a, float b, float t)
    {
        t = Math.Clamp(t, 0.0f, 1.0f);
        return a + (b - a) * t;
    }

    #endregion

    #region Structs and Maps

    // Structure for target values for VBridger parameters for a specific phoneme
    public struct PhonemePose
    {
        public float MouthOpenY;        // 0-1: How open the mouth is
        public float JawOpen;           // 0-1: How open the jaw is
        public float MouthForm;         // -1 (Frown) to +1 (Smile): Vertical lip corner movement
        public float MouthShrug;        // 0-1: Upward lip shrug/tension
        public float MouthFunnel;       // 0-1: Funnel shape (lips forward/pursed)
        public float MouthPuckerWiden;  // -1 (Wide) to +1 (Pucker): Controls mouth width
        public float MouthPressLipOpen; // -1 (Pressed Thin) to 0 (Touching) to +1 (Separated/Teeth)
        public float MouthX;            // -1 to 1: Horizontal mouth shift
        public float CheekPuffC;        // 0-1: Cheek puff amount

        // Neutral pose (all parameters at default/zero)
        public static PhonemePose Neutral = new(0);

        // Constructor
        public PhonemePose(float openY = 0, float jawOpen = 0, float form = 0, float shrug = 0, float funnel = 0, float puckerWiden = 0, float pressLip = 0, float mouthX = 0, float cheekPuff = 0)
        {
            MouthOpenY = openY; JawOpen = jawOpen; MouthForm = form; MouthShrug = shrug;
            MouthFunnel = funnel; MouthPuckerWiden = puckerWiden; MouthPressLipOpen = pressLip;
            MouthX = mouthX; CheekPuffC = cheekPuff;
        }

        // Linear interpolation between two poses
        public static PhonemePose Lerp(PhonemePose a, PhonemePose b, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return new PhonemePose(
               VBridgerLipSyncService.Lerp(a.MouthOpenY, b.MouthOpenY, t),
               VBridgerLipSyncService.Lerp(a.JawOpen, b.JawOpen, t),
               VBridgerLipSyncService.Lerp(a.MouthForm, b.MouthForm, t),
               VBridgerLipSyncService.Lerp(a.MouthShrug, b.MouthShrug, t),
               VBridgerLipSyncService.Lerp(a.MouthFunnel, b.MouthFunnel, t),
               VBridgerLipSyncService.Lerp(a.MouthPuckerWiden, b.MouthPuckerWiden, t),
               VBridgerLipSyncService.Lerp(a.MouthPressLipOpen, b.MouthPressLipOpen, t),
               VBridgerLipSyncService.Lerp(a.MouthX, b.MouthX, t),
               VBridgerLipSyncService.Lerp(a.CheekPuffC, b.CheekPuffC, t)
            );
        }
    }

    // Timed phoneme representation
    private struct TimedPhoneme
    {
        public string Phoneme;
        public double StartTime;
        public double EndTime;
    }

    // Initialize the phoneme mapping based on VBridger parameter definitions
    private Dictionary<string, PhonemePose> InitializeMisakiPhonemeMap_Revised()
    {
        // Phoneme to pose mapping based on VBridger parameter definitions:
        // MouthForm: -1 (Frown) to +1 (Smile)
        // MouthPuckerWiden: -1 (Wide) to +1 (Pucker)
        // MouthPressLipOpen: -1 (Pressed Thin) to +1 (Separated/Teeth)
        
        var map = new Dictionary<string, PhonemePose>();

        // --- Neutral ---
        map.Add("SIL", PhonemePose.Neutral); // Neutral: open=0, jaw=0, form=0, shrug=0, funnel=0, puckerWiden=0, pressLip=0, puff=0

        // --- Shared IPA Consonants ---
        // Plosives (b, p, d, t, g, k) - Focus on closure and puff
        map.Add("b", new PhonemePose(pressLip: -1.0f, cheekPuff: 0.6f)); // Pressed lips, puff
        map.Add("p", new PhonemePose(pressLip: -1.0f, cheekPuff: 0.8f)); // Pressed lips, strong puff
        map.Add("d", new PhonemePose(openY: 0.05f, jawOpen: 0.05f, pressLip: 0.0f, cheekPuff: 0.2f)); // Slight open, lips touch/nearly touch, slight puff
        map.Add("t", new PhonemePose(openY: 0.05f, jawOpen: 0.05f, pressLip: 0.0f, cheekPuff: 0.3f)); // Slight open, lips touch/nearly touch, moderate puff
        map.Add("ɡ", new PhonemePose(openY: 0.1f, jawOpen: 0.15f, pressLip: 0.2f, cheekPuff: 0.5f)); // Back sound, slight open, slight separation, puff
        map.Add("k", new PhonemePose(openY: 0.1f, jawOpen: 0.15f, pressLip: 0.2f, cheekPuff: 0.4f)); // Back sound, slight open, slight separation, moderate puff

        // Fricatives (f, v, s, z, h, ʃ, ʒ, ð, θ) - Focus on partial closure/airflow shapes
        map.Add("f", new PhonemePose(openY: 0.05f, pressLip: -0.2f, form: -0.2f, puckerWiden: -0.1f)); // Lower lip near upper teeth: Slight press, slight frown, slightly wide
        map.Add("v", new PhonemePose(openY: 0.05f, pressLip: -0.1f, form: -0.2f, puckerWiden: -0.1f)); // Voiced 'f': Less press?
        map.Add("s", new PhonemePose(jawOpen: 0.0f, pressLip: 0.9f, form: 0.3f, puckerWiden: -0.6f)); // Teeth close/showing, slight smile, wide
        map.Add("z", new PhonemePose(jawOpen: 0.0f, pressLip: 0.8f, form: 0.2f, puckerWiden: -0.5f)); // Voiced 's': Slightly less extreme?
        map.Add("h", new PhonemePose(openY: 0.2f, jawOpen: 0.2f, pressLip: 0.5f)); // Breathy open, lips separated
        map.Add("ʃ", new PhonemePose(openY: 0.1f, funnel: 0.9f, puckerWiden: 0.6f, pressLip: 0.2f)); // 'sh': Funnel, puckered but flatter than 'oo', slight separation
        map.Add("ʒ", new PhonemePose(openY: 0.1f, funnel: 0.8f, puckerWiden: 0.5f, pressLip: 0.2f)); // 'zh': Similar to 'sh'
        map.Add("ð", new PhonemePose(openY: 0.05f, pressLip: 0.1f, puckerWiden: -0.2f)); // Soft 'th': Tongue tip, lips nearly touching, slightly wide
        map.Add("θ", new PhonemePose(openY: 0.05f, pressLip: 0.2f, puckerWiden: -0.3f)); // Hard 'th': More airflow? More separation/width?

        // Nasals (m, n, ŋ) - Focus on closure or near-closure
        map.Add("m", new PhonemePose(pressLip: -1.0f)); // Pressed lips
        map.Add("n", new PhonemePose(openY: 0.05f, jawOpen: 0.05f, pressLip: 0.0f)); // Like 'd' position, lips touching
        map.Add("ŋ", new PhonemePose(openY: 0.15f, jawOpen: 0.2f, pressLip: 0.4f)); // 'ng': Back tongue, mouth open more, lips separated

        // Liquids/Glides (l, ɹ, w, j) - Varied shapes
        map.Add("l", new PhonemePose(openY: 0.2f, jawOpen: 0.2f, puckerWiden: -0.3f, pressLip: 0.6f)); // Tongue tip visible: Slightly open, slightly wide, lips separated
        map.Add("ɹ", new PhonemePose(openY: 0.15f, jawOpen: 0.15f, funnel: 0.4f, puckerWiden: 0.2f, pressLip: 0.3f)); // 'r': Some funneling, slight pucker, separation
        map.Add("w", new PhonemePose(openY: 0.1f, jawOpen: 0.1f, funnel: 1.0f, puckerWiden: 0.9f, pressLip: -0.3f)); // Like 'u': Strong funnel, strong pucker, lips maybe slightly pressed
        map.Add("j", new PhonemePose(openY: 0.1f, jawOpen: 0.1f, form: 0.6f, shrug: 0.3f, puckerWiden: -0.8f, pressLip: 0.8f)); // 'y': Like 'i', smile, shrug, wide, teeth showing

        // --- Shared Consonant Clusters ---
        map.Add("ʤ", new PhonemePose(openY: 0.1f, funnel: 0.8f, puckerWiden: 0.5f, pressLip: 0.2f, cheekPuff: 0.3f)); // 'j'/'dg': Target 'ʒ' shape + puff
        map.Add("ʧ", new PhonemePose(openY: 0.1f, funnel: 0.9f, puckerWiden: 0.6f, pressLip: 0.2f, cheekPuff: 0.4f)); // 'ch': Target 'ʃ' shape + puff

        // --- Shared IPA Vowels ---
        map.Add("ə", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, pressLip: 0.5f)); // Schwa: Neutral open, lips separated
        map.Add("i", new PhonemePose(openY: 0.1f, jawOpen: 0.1f, form: 0.7f, shrug: 0.4f, puckerWiden: -0.9f, pressLip: 0.9f)); // 'ee': Smile, shrug, wide, teeth showing
        map.Add("u", new PhonemePose(openY: 0.15f, jawOpen: 0.15f, funnel: 1.0f, puckerWiden: 1.0f, pressLip: -0.2f)); // 'oo': Funnel, puckered, slight press
        map.Add("ɑ", new PhonemePose(openY: 0.9f, jawOpen: 1.0f, pressLip: 0.8f)); // 'aa': Very open, lips separated
        map.Add("ɔ", new PhonemePose(openY: 0.6f, jawOpen: 0.7f, funnel: 0.5f, puckerWiden: 0.3f, pressLip: 0.7f)); // 'aw': Open, some funnel, slight pucker, separated
        map.Add("ɛ", new PhonemePose(openY: 0.5f, jawOpen: 0.5f, puckerWiden: -0.5f, pressLip: 0.7f)); // 'eh': Mid open, somewhat wide, separated
        map.Add("ɜ", new PhonemePose(openY: 0.4f, jawOpen: 0.4f, pressLip: 0.6f)); // 'er': Mid open, neutral width, separated (blend with 'ɹ')
        map.Add("ɪ", new PhonemePose(openY: 0.2f, jawOpen: 0.2f, form: 0.2f, puckerWiden: -0.6f, pressLip: 0.8f)); // 'ih': Slight open, slight smile, wide, separated
        map.Add("ʊ", new PhonemePose(openY: 0.2f, jawOpen: 0.2f, funnel: 0.8f, puckerWiden: 0.7f, pressLip: 0.1f)); // 'uu': Slight open, funnel, pucker, near touch
        map.Add("ʌ", new PhonemePose(openY: 0.6f, jawOpen: 0.6f, pressLip: 0.7f)); // 'uh': Mid open, neutral width, separated

        // --- Shared Diphthong Vowels (Targeting the end-shape's characteristics) ---
        map.Add("A", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, form: 0.4f, puckerWiden: -0.7f, pressLip: 0.8f)); // 'ay' (ends like ɪ/i): Mid-close, smile, wide, separated
        map.Add("I", new PhonemePose(openY: 0.4f, jawOpen: 0.4f, form: 0.3f, puckerWiden: -0.6f, pressLip: 0.8f)); // 'eye' (ends like ɪ/i): Mid-open, smile, wide, separated
        map.Add("W", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, funnel: 0.9f, puckerWiden: 0.8f, pressLip: 0.0f)); // 'ow' (ends like ʊ/u): Mid-close, funnel, pucker, touching
        map.Add("Y", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, form: 0.2f, puckerWiden: -0.5f, pressLip: 0.8f)); // 'oy' (ends like ɪ/i): Mid-close, smile, wide, separated

        // --- Shared Custom Vowel ---
        map.Add("ᵊ", new PhonemePose(openY: 0.1f, jawOpen: 0.1f, pressLip: 0.2f)); // Small schwa: Minimal open, slight separation

        // --- American-only ---
        map.Add("æ", new PhonemePose(openY: 0.7f, jawOpen: 0.7f, form: 0.3f, puckerWiden: -0.8f, pressLip: 0.9f)); // 'ae': Open, slight smile, wide, teeth showing
        map.Add("O", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, funnel: 0.8f, puckerWiden: 0.6f, pressLip: 0.1f)); // US 'oh' (ends like ʊ/u): Mid-close, funnel, pucker, near touch
        map.Add("ᵻ", new PhonemePose(openY: 0.15f, jawOpen: 0.15f, puckerWiden: -0.2f, pressLip: 0.6f)); // Between ə/ɪ: Slightly open, neutral-wide, separated
        map.Add("ɾ", new PhonemePose(openY: 0.05f, jawOpen: 0.05f, pressLip: 0.3f)); // Flap 't': Very quick, slight separation

        // --- British-only ---
        map.Add("a", new PhonemePose(openY: 0.7f, jawOpen: 0.7f, puckerWiden: -0.4f, pressLip: 0.8f)); // UK 'ash': Open, less wide than US 'æ', separated
        map.Add("Q", new PhonemePose(openY: 0.3f, jawOpen: 0.3f, funnel: 0.7f, puckerWiden: 0.5f, pressLip: 0.1f)); // UK 'oh' (ends like ʊ/u): Mid-close, funnel, pucker, near touch
        map.Add("ɒ", new PhonemePose(openY: 0.8f, jawOpen: 0.9f, funnel: 0.2f, puckerWiden: 0.1f, pressLip: 0.8f)); // 'on': Open, slight funnel, slight pucker, separated

        return map;
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _logger.LogDebug("Disposing...");
            Stop();
            UnsubscribeFromCurrentHost();
            _activePhonemes.Clear();
            _currentParameterValues.Clear();
        }

        _disposed = true;
        _logger.LogInformation("Disposed");
    }

    #endregion
}