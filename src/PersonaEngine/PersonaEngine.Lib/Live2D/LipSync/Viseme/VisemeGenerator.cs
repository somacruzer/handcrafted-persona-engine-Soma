namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class VisemeGenerator
{
    private readonly IPhonemeVisemeMapper _mapper;

    private readonly IVisemeTimingStrategy _timingStrategy;

    public VisemeGenerator(
        IPhonemeVisemeMapper  mapper         = null,
        IVisemeTimingStrategy timingStrategy = null)
    {
        _mapper         = mapper ?? VisemeMapperFactory.CreateIPAMapper();
        _timingStrategy = timingStrategy ?? VisemeMapperFactory.CreateStandardTimingStrategy();
    }

    public List<TimeCodedViseme> GenerateVisemesFromPhonemes(List<PhonemeTimingInfo> phonemeTimings) { return _timingStrategy.GenerateVisemeTimings(phonemeTimings, _mapper); }
}