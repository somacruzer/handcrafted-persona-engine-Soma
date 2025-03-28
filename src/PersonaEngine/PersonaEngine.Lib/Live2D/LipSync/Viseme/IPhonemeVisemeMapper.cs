namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public interface IPhonemeVisemeMapper
{
    VisemeType MapPhonemeToViseme(string phoneme);

    bool TryMapPhonemeToViseme(string phoneme, out VisemeType viseme);
}