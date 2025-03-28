using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Live2D.App;
using PersonaEngine.Lib.Live2D.Framework;
using PersonaEngine.Lib.Live2D.Framework.Model;
using PersonaEngine.Lib.Live2D.LipSync.Viseme;

namespace PersonaEngine.Lib.Live2D.LipSync;

public class LipSyncManager
{
    private static readonly HashSet<VisemeType> vowels = new() {
                                                                   VisemeType.AA,
                                                                   VisemeType.AH,
                                                                   VisemeType.EH,
                                                                   VisemeType.IY,
                                                                   VisemeType.UW,
                                                                   VisemeType.OW,
                                                                   VisemeType.AY,
                                                                   VisemeType.OY,
                                                                   VisemeType.AW
                                                               };

    private readonly VisemeGenerator _visemeGenerator;

    private readonly List<VisemeTimeWindow> activeVisemesTemp = new();

    private readonly List<VisemeTimeWindow> activeVisemeWindows = new();

    private readonly CubismModel model;

    private readonly float noiseFrequency = 0.5f;

    private readonly Dictionary<string, ParameterState> parameterStates = new();

    private readonly bool useMouthParameters;

    private readonly bool useVoiceParameters;

    private readonly List<string> voiceParameters = new() {
                                                              CubismDefaultParameterId.ParamVoiceA,
                                                              CubismDefaultParameterId.ParamVoiceI,
                                                              CubismDefaultParameterId.ParamVoiceU,
                                                              CubismDefaultParameterId.ParamVoiceE,
                                                              CubismDefaultParameterId.ParamVoiceO,
                                                              CubismDefaultParameterId.ParamVoiceSilence
                                                          };

    private float animationTime;

    private IStreamingAudioPlayerHost? audioPlayer;

    private float audioReferenceTime;

    private int currentVisemeIndex;

    private bool isAudioPlaying;

    private string lastActiveParameterMapping = CubismDefaultParameterId.ParamVoiceA;

    private float noiseAccumulator;

    private LipSyncTemplate template;

    private List<TimeCodedViseme> visemeSequence = new();

    public LipSyncManager(LAppModel model, LipSyncTemplate? template = null)
    {
        this.model    = model.Model;
        this.template = template ?? LipSyncTemplates.Default;

        useVoiceParameters = model.Parameters.Contains(CubismDefaultParameterId.ParamVoiceA);
        useMouthParameters = !useVoiceParameters &&
                             model.Parameters.Contains(CubismDefaultParameterId.ParamMouthOpenY) &&
                             model.Parameters.Contains(CubismDefaultParameterId.ParamMouthForm);

        if ( useVoiceParameters )
        {
            foreach ( var param in voiceParameters )
            {
                parameterStates[param] = new ParameterState();
            }
        }

        if ( useMouthParameters )
        {
            parameterStates[CubismDefaultParameterId.ParamMouthOpenY] = new ParameterState();
            parameterStates[CubismDefaultParameterId.ParamMouthForm]  = new ParameterState();
        }

        model.CustomValueUpdate =  true;
        model.ValueUpdate       += _ => AnimationUpdate(LAppPal.DeltaTime);

        var config         = new VisemeMappingConfig { PrimaryStressMultiplier = 1.3f, InsertNeutralForGaps = false };
        var mapper         = VisemeMapperFactory.CreateIPAMapper();
        var timingStrategy = VisemeMapperFactory.CreateContextualTimingStrategy(config, 2);
        _visemeGenerator = new VisemeGenerator();
    }

    public void SubscribeToAudioPlayer(IStreamingAudioPlayerHost audioPlayer)
    {
        this.audioPlayer                =  audioPlayer;
        audioPlayer.OnPlaybackStarted   += AudioPlayerOnPlaybackStarted;
        audioPlayer.OnPlaybackCompleted += AudioPlayerOnPlaybackCompleted;
    }

    private void AudioPlayerOnPlaybackStarted(object? sender, AudioPlaybackEventArgs e)
    {
        if ( audioPlayer != null )
        {
            audioReferenceTime = audioPlayer.CurrentTime;
        }

        animationTime      = 0f;
        isAudioPlaying     = true;
        currentVisemeIndex = 0;
        visemeSequence.Clear();
        activeVisemeWindows.Clear();

        var phonemeTimings = ProcessPhonemeTimings(e);
        visemeSequence = _visemeGenerator.GenerateVisemesFromPhonemes(phonemeTimings);

        activeVisemeWindows.Capacity = Math.Max(activeVisemeWindows.Capacity, visemeSequence.Count);
        foreach ( var viseme in visemeSequence )
        {
            activeVisemeWindows.Add(new VisemeTimeWindow(
                                                         viseme,
                                                         template.AnticipationMargin,
                                                         template.LingerMargin
                                                        ));
        }
    }

    private List<PhonemeTimingInfo> ProcessPhonemeTimings(AudioPlaybackEventArgs e)
    {
        var tokenCount     = e.Segment.Tokens.Count;
        var phonemeTimings = new List<PhonemeTimingInfo>(tokenCount * 3);

        foreach ( var token in e.Segment.Tokens )
        {
            if ( token.Phonemes == null || !token.StartTs.HasValue || !token.EndTs.HasValue )
            {
                continue;
            }

            var tokenPhonemes = token.Phonemes.Trim();
            if ( string.IsNullOrEmpty(tokenPhonemes) )
            {
                continue;
            }

            var tokenStart    = token.StartTs.Value;
            var tokenEnd      = token.EndTs.Value;
            var tokenDuration = tokenEnd - tokenStart;
            var count         = tokenPhonemes.Length;

            if ( count == 0 )
            {
                continue;
            }

            var segmentDuration = tokenDuration / count;
            for ( var i = 0; i < count; i++ )
            {
                var phonemeStart = tokenStart + i * segmentDuration;
                var phonemeEnd   = phonemeStart + segmentDuration;
                phonemeTimings.Add(new PhonemeTimingInfo(tokenPhonemes[i].ToString(), phonemeStart, phonemeEnd));
            }
        }

        return phonemeTimings;
    }

    private void AudioPlayerOnPlaybackCompleted(object? sender, AudioPlaybackEventArgs e)
    {
        isAudioPlaying = false;
        visemeSequence.Clear();
        activeVisemeWindows.Clear();
        currentVisemeIndex = 0;

        foreach ( var state in parameterStates.Values )
        {
            state.TargetValue = 0f;
        }
    }

    public void AnimationUpdate(float deltaTime)
    {
        if ( isAudioPlaying )
        {
            if ( audioPlayer != null )
            {
                var expectedTime = audioPlayer.CurrentTime - audioReferenceTime;
                var correction   = (expectedTime - animationTime) * 0.1f;
                animationTime += deltaTime + correction;
            }
            else
            {
                animationTime += deltaTime;
            }

            UpdateVisemeInfluences();
        }
        else
        {
            foreach ( var state in parameterStates )
            {
                state.Value.TargetValue = 0f;
            }
        }

        UpdateParameters(deltaTime);
    }

    private void UpdateVisemeInfluences()
    {
        UpdateCurrentVisemeIndex();

        foreach ( var state in parameterStates.Values )
        {
            state.TargetValue = 0f;
        }

        var activeVisemes = GetActiveVisemes();

        foreach ( var visemeWindow in activeVisemes )
        {
            ApplyVisemeInfluence(visemeWindow.Viseme, visemeWindow.EffectiveStartTime, visemeWindow.EffectiveEndTime);
        }

        ApplyMicroexpressions();
    }

    private List<VisemeTimeWindow> GetActiveVisemes()
    {
        const float lookaheadTime = 0.15f;
        activeVisemesTemp.Clear();

        for ( var i = 0; i < activeVisemeWindows.Count; i++ )
        {
            var window = activeVisemeWindows[i];
            if ( animationTime + lookaheadTime >= window.EffectiveStartTime &&
                 animationTime <= window.EffectiveEndTime )
            {
                activeVisemesTemp.Add(window);
            }
        }

        return activeVisemesTemp;
    }

    private void ApplyVisemeInfluence(TimeCodedViseme viseme, float effectiveStart, float effectiveEnd)
    {
        var midpoint = (viseme.StartTime + viseme.EndTime) / 2f;
        var spread   = Math.Max((viseme.EndTime - viseme.StartTime) / 2f, template.GaussianSpreadMinimum);
        spread /= 1 + GetAudioEnergyFactor() * 0.5f;

        var weight = (float)Math.Exp(-Math.Pow(animationTime - midpoint, 2) / (2 * spread * spread));

        var contextIntensity = viseme.Intensity;
        if ( IsVowel(viseme.Viseme) )
        {
            var hasConsonant = false;
            for ( var i = 0; i < visemeSequence.Count; i++ )
            {
                var v = visemeSequence[i];
                if ( !IsVowel(v.Viseme) && Math.Abs(v.StartTime - viseme.StartTime) < 0.1f )
                {
                    hasConsonant = true;

                    break;
                }
            }

            if ( !hasConsonant )
            {
                contextIntensity *= 1.1f;
            }
        }

        var contribution = weight * contextIntensity;

        if ( useVoiceParameters )
        {
            var parameterId = MapVisemeToParameter(viseme.Viseme, lastActiveParameterMapping);
            lastActiveParameterMapping = parameterId;

            if ( parameterStates.TryGetValue(parameterId, out var state) )
            {
                state.TargetValue += contribution;
            }
        }
        else if ( useMouthParameters )
        {
            ApplyVisemeToMouthParameters(viseme.Viseme, contribution);
        }
    }

    private void ApplyVisemeToMouthParameters(VisemeType viseme, float contribution)
    {
        var mappedParam = MapVisemeToParameter(viseme, lastActiveParameterMapping);
        lastActiveParameterMapping = mappedParam;

        if ( mappedParam == CubismDefaultParameterId.ParamVoiceSilence )
        {
            // No contribution for silence
        }
        else if ( mappedParam == CubismDefaultParameterId.ParamVoiceI ||
                  mappedParam == CubismDefaultParameterId.ParamVoiceE )
        {
            parameterStates[CubismDefaultParameterId.ParamMouthOpenY].TargetValue += contribution * 0.5f;
            parameterStates[CubismDefaultParameterId.ParamMouthForm].TargetValue  += contribution * 0.5f;
        }
        else
        {
            parameterStates[CubismDefaultParameterId.ParamMouthOpenY].TargetValue += contribution;
        }
    }

    private void ApplyMicroexpressions()
    {
        noiseAccumulator += LAppPal.DeltaTime * noiseFrequency;
        var noise = (float)(0.02 * Math.Sin(noiseAccumulator));

        foreach ( var state in parameterStates.Values )
        {
            if ( state.TargetValue > 0.1f )
            {
                state.TargetValue = Math.Clamp(state.TargetValue + noise, 0f, 1f);
            }
        }
    }

    private void UpdateParameters(float deltaTime)
    {
        foreach ( var entry in parameterStates )
        {
            var paramId = entry.Key;
            var state   = entry.Value;

            state.TargetValue = Math.Clamp(state.TargetValue, 0f, 1f);

            var difference = Math.Abs(state.TargetValue - state.CurrentValue);

            var adaptiveSmoothingTime = template.BaseSmoothingTime;
            if ( difference > 0.1f )
            {
                adaptiveSmoothingTime = Math.Max(
                                                 template.MinSmoothingTime,
                                                 template.BaseSmoothingTime - difference * template.SmoothingDifferenceFactor
                                                );
            }

            var stateVelocity = state.Velocity;
            state.CurrentValue = SmoothParameterValue(
                                                      state.CurrentValue,
                                                      state.TargetValue,
                                                      ref stateVelocity,
                                                      deltaTime,
                                                      adaptiveSmoothingTime
                                                     );

            state.Velocity = stateVelocity;

            if ( model.ParameterIds.Contains(paramId) )
            {
                model.SetParameterValue(paramId, state.CurrentValue);
            }
        }
    }

    private float SmoothParameterValue(float current, float target, ref float velocity, float deltaTime, float smoothingTime)
    {
        var spring = 1.0f / (smoothingTime * smoothingTime);
        var damper = 2.0f / smoothingTime;

        var springForce = (target - current) * spring;
        var damperForce = velocity * damper;

        velocity += (springForce - damperForce) * deltaTime;

        var newValue = current + velocity * deltaTime;

        if ( Math.Abs(target - newValue) < 0.01f && Math.Abs(velocity) < 0.01f )
        {
            newValue = target;
            velocity = 0;
        }

        return newValue;
    }

    private void UpdateCurrentVisemeIndex()
    {
        var newIndex = currentVisemeIndex;

        while ( newIndex < visemeSequence.Count &&
                animationTime > visemeSequence[newIndex].EndTime + template.LingerMargin )
        {
            newIndex++;
        }

        currentVisemeIndex = newIndex;
    }

    private float GetAudioEnergyFactor() { return 0.5f; }

    private bool IsVowel(VisemeType viseme) { return vowels.Contains(viseme); }

    private string MapVisemeToParameter(VisemeType viseme, string fallbackParameter)
    {
        return viseme switch {
            VisemeType.BMP or VisemeType.N or VisemeType.KG or VisemeType.FV or VisemeType.TD or VisemeType.ThDh
                => CubismDefaultParameterId.ParamVoiceSilence,
            VisemeType.IY or VisemeType.SZ
                => CubismDefaultParameterId.ParamVoiceI,
            VisemeType.CH or VisemeType.EH
                => CubismDefaultParameterId.ParamVoiceE,
            VisemeType.R or VisemeType.W or VisemeType.Y or VisemeType.UW or VisemeType.AH
                => CubismDefaultParameterId.ParamVoiceU,
            VisemeType.AA or VisemeType.AY or VisemeType.AW
                => CubismDefaultParameterId.ParamVoiceA,
            VisemeType.OW or VisemeType.OY
                => CubismDefaultParameterId.ParamVoiceO,
            _ => fallbackParameter
        };
    }

    public void SetTemplate(LipSyncTemplate newTemplate) { template = newTemplate; }

    private class VisemeTimeWindow
    {
        public readonly float EffectiveEndTime;

        public readonly float EffectiveStartTime;

        public readonly TimeCodedViseme Viseme;

        public VisemeTimeWindow(TimeCodedViseme viseme, float anticipationMargin, float lingerMargin)
        {
            Viseme             = viseme;
            EffectiveStartTime = (float)viseme.StartTime - anticipationMargin;
            EffectiveEndTime   = (float)viseme.EndTime + lingerMargin;
        }
    }

    private class ParameterState
    {
        public ParameterState()
        {
            CurrentValue = 0f;
            TargetValue  = 0f;
            Velocity     = 0f;
        }

        public float CurrentValue { get; set; }

        public float TargetValue { get; set; }

        public float Velocity { get; set; }
    }
}