namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class VisemeMapperFactory
{
    public static IPhonemeVisemeMapper CreateIPAMapper() { return new IPAVisemeMapper(); }

    public static IPhonemeVisemeMapper CreateCustomMapper(Dictionary<string, VisemeType> customMappings) { return new IPAVisemeMapper(customMappings); }

    public static IVisemeTimingStrategy CreateStandardTimingStrategy(VisemeMappingConfig config = null) { return new StandardVisemeTimingStrategy(config); }

    public static IVisemeTimingStrategy CreateContextualTimingStrategy(
        VisemeMappingConfig config        = null,
        int                 contextWindow = 1)
    {
        return new ContextualVisemeTimingStrategy(config, contextWindow);
    }
}