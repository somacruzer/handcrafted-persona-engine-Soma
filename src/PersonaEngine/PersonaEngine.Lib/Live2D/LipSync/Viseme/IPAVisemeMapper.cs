namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class IPAVisemeMapper : IPhonemeVisemeMapper
{
    private readonly Dictionary<string, VisemeType> _ipaToViseme;

    public IPAVisemeMapper() { _ipaToViseme = CreateDefaultMappings(); }

    public IPAVisemeMapper(Dictionary<string, VisemeType> customMappings) { _ipaToViseme = customMappings ?? CreateDefaultMappings(); }

    public VisemeType MapPhonemeToViseme(string phoneme)
    {
        return _ipaToViseme.TryGetValue(phoneme, out var viseme)
                   ? viseme
                   : VisemeType.Neutral;
    }

    public bool TryMapPhonemeToViseme(string phoneme, out VisemeType viseme) { return _ipaToViseme.TryGetValue(phoneme, out viseme); }

    private static Dictionary<string, VisemeType> CreateDefaultMappings()
    {
        // Group phonemes by viseme categories for better readability and maintenance
        var bilabials    = new[] { "b", "m", "p" };
        var alveolars    = new[] { "d", "t", "l" };
        var labiodentals = new[] { "f", "v" };
        var glottals     = new[] { "h" };
        var palatals     = new[] { "j" };
        var velars       = new[] { "k", "ɡ", "ŋ" };
        var sibilants    = new[] { "s", "z", "ʃ", "ʒ" };
        var nasals       = new[] { "n" };
        var labiovelar   = new[] { "w" };
        var retroflex    = new[] { "ɹ" };
        var dentals      = new[] { "ð", "θ" };
        var affricates   = new[] { "ʤ", "ʧ" };

        // Vowels by category
        var iyVowels = new[] { "i", "ɪ", "ᵻ" };
        var uwVowels = new[] { "u", "ʊ" };
        var aaVowels = new[] { "ɑ", "æ", "a" };
        var ahVowels = new[] { "ə", "ɜ", "ʌ", "ᵊ" };
        var owVowels = new[] { "ɔ", "O", "Q", "ɒ" };

        // Diphthongs
        var ehDiphthongs = new[] { "A" }; // expands to eɪ
        var ayDiphthongs = new[] { "I" }; // expands to aɪ
        var awDiphthongs = new[] { "W" }; // expands to aʊ
        var oyDiphthongs = new[] { "Y" }; // expands to ɔɪ

        // Create mapping dictionary
        var mappings = new Dictionary<string, VisemeType>();

        // Helper to add multiple phonemes with the same viseme
        void AddPhonemeGroup(string[] phonemes, VisemeType viseme)
        {
            foreach ( var phoneme in phonemes )
            {
                mappings[phoneme] = viseme;
            }
        }

        // Add mappings by group
        AddPhonemeGroup(bilabials, VisemeType.BMP);
        AddPhonemeGroup(alveolars, VisemeType.TD);
        AddPhonemeGroup(labiodentals, VisemeType.FV);
        AddPhonemeGroup(glottals, VisemeType.Neutral);
        AddPhonemeGroup(palatals, VisemeType.Y);
        AddPhonemeGroup(velars, VisemeType.KG);
        AddPhonemeGroup(sibilants, VisemeType.SZ);
        AddPhonemeGroup(nasals, VisemeType.N);
        AddPhonemeGroup(labiovelar, VisemeType.W);
        AddPhonemeGroup(retroflex, VisemeType.R);
        AddPhonemeGroup(dentals, VisemeType.ThDh);
        AddPhonemeGroup(affricates, VisemeType.CH);

        // Add vowel mappings
        AddPhonemeGroup(iyVowels, VisemeType.IY);
        AddPhonemeGroup(uwVowels, VisemeType.UW);
        AddPhonemeGroup(aaVowels, VisemeType.AA);
        AddPhonemeGroup(ahVowels, VisemeType.AH);
        AddPhonemeGroup(owVowels, VisemeType.OW);

        // Add diphthong mappings
        AddPhonemeGroup(ehDiphthongs, VisemeType.EH);
        AddPhonemeGroup(ayDiphthongs, VisemeType.AY);
        AddPhonemeGroup(awDiphthongs, VisemeType.AW);
        AddPhonemeGroup(oyDiphthongs, VisemeType.OY);

        // American flap (between t and d)
        mappings["ɾ"] = VisemeType.TD;

        return mappings;
    }
}