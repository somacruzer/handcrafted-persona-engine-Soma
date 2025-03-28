namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class StandardVisemeTimingStrategy : IVisemeTimingStrategy
{
    private readonly VisemeMappingConfig _config;

    public StandardVisemeTimingStrategy(VisemeMappingConfig config = null) { _config = config ?? new VisemeMappingConfig(); }

    public List<TimeCodedViseme> GenerateVisemeTimings(
        List<PhonemeTimingInfo> phonemeTimings,
        IPhonemeVisemeMapper    mapper)
    {
        if ( phonemeTimings == null || !phonemeTimings.Any() )
        {
            return new List<TimeCodedViseme>();
        }

        var visemes          = new List<TimeCodedViseme>();
        var stressMultiplier = 1.0f;

        // Calculate average duration ignoring markers
        var durations = phonemeTimings
                        .Where(p => p.Phoneme != "ˈ" && p.Phoneme != "ˌ" && p.Phoneme != "ː")
                        .Select(p => p.Duration)
                        .ToList();

        var averageDuration = durations.Any() ? durations.Average() : 0.1;

        // Define thresholds for scaling based on configuration
        var minDurationThreshold = averageDuration * _config.MinDurationThresholdRatio;
        var maxDurationThreshold = averageDuration * _config.MaxDurationThresholdRatio;

        // Process each phoneme
        for ( var i = 0; i < phonemeTimings.Count; i++ )
        {
            var phoneme = phonemeTimings[i];

            // Handle stress markers
            if ( phoneme.Phoneme == "ˈ" )
            {
                stressMultiplier = _config.PrimaryStressMultiplier;

                continue;
            }

            if ( phoneme.Phoneme == "ˌ" )
            {
                stressMultiplier = _config.SecondaryStressMultiplier;

                continue;
            }

            if ( phoneme.Phoneme == "ː" )
            {
                continue; // ignore length extender markers
            }

            // Calculate intensity based on duration
            var duration        = phoneme.Duration;
            var clampedDuration = Math.Clamp(duration, minDurationThreshold, maxDurationThreshold);
            var normalizedRatio = (clampedDuration - minDurationThreshold) /
                                  (maxDurationThreshold - minDurationThreshold);

            var baseIntensity = _config.MinIntensity +
                                (float)normalizedRatio * (_config.MaxIntensity - _config.MinIntensity);

            var finalIntensity = Math.Clamp(baseIntensity * stressMultiplier, 0f, 1f);

            // Reset stress multiplier after application
            stressMultiplier = 1.0f;

            // Get viseme type for current phoneme
            var visemeType = mapper.MapPhonemeToViseme(phoneme.Phoneme);

            // Handle gap between visemes if needed
            if ( visemes.Count > 0 )
            {
                var lastViseme = visemes[^1];
                var hasGap     = lastViseme.EndTime < phoneme.StartTime;

                if ( hasGap && _config.InsertNeutralForGaps )
                {
                    // Insert a brief neutral gap
                    visemes.Add(new TimeCodedViseme(
                                                    VisemeType.Neutral,
                                                    lastViseme.EndTime,
                                                    phoneme.StartTime));
                }

                // Check if we can merge with previous viseme
                if ( lastViseme.Viseme == visemeType &&
                     Math.Abs(lastViseme.EndTime - phoneme.StartTime) < _config.MaxGapForMerging )
                {
                    // Update the last viseme instead of adding a new one
                    var averageIntensity = (lastViseme.Intensity + finalIntensity) / 2;
                    visemes[^1] = new TimeCodedViseme(
                                                      visemeType,
                                                      lastViseme.StartTime,
                                                      phoneme.EndTime,
                                                      averageIntensity);

                    continue;
                }
            }

            // Add new viseme
            visemes.Add(new TimeCodedViseme(
                                            visemeType,
                                            phoneme.StartTime,
                                            phoneme.EndTime,
                                            finalIntensity));
        }

        return visemes;
    }
}