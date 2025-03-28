namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public interface IVisemeTimingStrategy
{
    List<TimeCodedViseme> GenerateVisemeTimings(List<PhonemeTimingInfo> phonemeTimings, IPhonemeVisemeMapper mapper);
}