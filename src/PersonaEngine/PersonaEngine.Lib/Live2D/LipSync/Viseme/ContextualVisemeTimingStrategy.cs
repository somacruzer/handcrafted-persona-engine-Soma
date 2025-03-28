namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class ContextualVisemeTimingStrategy : IVisemeTimingStrategy
{
    private readonly VisemeMappingConfig _config;

    private readonly int _contextWindow;

    public ContextualVisemeTimingStrategy(VisemeMappingConfig config = null, int contextWindow = 1)
    {
        _config        = config ?? new VisemeMappingConfig();
        _contextWindow = contextWindow;
    }

    public List<TimeCodedViseme> GenerateVisemeTimings(
        List<PhonemeTimingInfo> phonemeTimings,
        IPhonemeVisemeMapper    mapper)
    {
        // First generate basic visemes using standard strategy
        var standardStrategy = new StandardVisemeTimingStrategy(_config);
        var basicVisemes     = standardStrategy.GenerateVisemeTimings(phonemeTimings, mapper);

        // Now apply contextual adjustments
        return ApplyContextualAdjustments(basicVisemes, phonemeTimings);
    }

    private List<TimeCodedViseme> ApplyContextualAdjustments(
        List<TimeCodedViseme>   visemes,
        List<PhonemeTimingInfo> phonemeTimings)
    {
        // Skip if not enough visemes for context
        if ( visemes.Count <= _contextWindow * 2 )
        {
            return visemes;
        }

        var adjustedVisemes = new List<TimeCodedViseme>(visemes);

        // Apply coarticulation adjustments
        // This is a simplified example - real coarticulation is more complex
        for ( var i = _contextWindow; i < adjustedVisemes.Count - _contextWindow; i++ )
        {
            var current = adjustedVisemes[i];

            // Adjust intensities based on context
            // For example, reduce intensity of consonants between vowels
            var isConsonant       = IsConsonantViseme(current.Viseme);
            var hasPrecedingVowel = i > 0 && IsVowelViseme(adjustedVisemes[i - 1].Viseme);
            var hasFollowingVowel = i < adjustedVisemes.Count - 1 &&
                                    IsVowelViseme(adjustedVisemes[i + 1].Viseme);

            if ( isConsonant && hasPrecedingVowel && hasFollowingVowel )
            {
                // Reduce consonant intensity between vowels (coarticulation effect)
                var adjustedIntensity = current.Intensity * 0.9f;
                adjustedVisemes[i] = current.WithIntensity(adjustedIntensity);
            }
        }

        return adjustedVisemes;
    }

    private bool IsConsonantViseme(VisemeType viseme)
    {
        return viseme == VisemeType.BMP ||
               viseme == VisemeType.TD ||
               viseme == VisemeType.FV ||
               viseme == VisemeType.KG ||
               viseme == VisemeType.SZ ||
               viseme == VisemeType.N ||
               viseme == VisemeType.ThDh ||
               viseme == VisemeType.CH;
    }

    private bool IsVowelViseme(VisemeType viseme)
    {
        return viseme == VisemeType.AA ||
               viseme == VisemeType.AH ||
               viseme == VisemeType.AW ||
               viseme == VisemeType.AY ||
               viseme == VisemeType.EH ||
               viseme == VisemeType.IY ||
               viseme == VisemeType.OW ||
               viseme == VisemeType.OY ||
               viseme == VisemeType.UW;
    }
}