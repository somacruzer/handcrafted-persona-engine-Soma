namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public enum VisemeType
{
    Neutral, // Rest position

    BMP, // Bilabial consonants (b, m, p)

    FV, // Labiodental consonants (f, v)

    ThDh, // Dental fricatives (θ, ð)

    SZ, // Sibilants (s, z, ʃ, ʒ)

    TD, // Alveolar stops and approximants (t, d, l, ɾ)

    N, // Nasal (n)

    R, // Rhotic (ɹ)

    KG, // Velar consonants (k, ɡ, ŋ)

    CH, // Affricates (ʧ, ʤ)

    W, // Labio-velar approximant (w)

    Y, // Palatal approximant (j, represented as “y”)

    AA, // Open vowels (ɑ, æ, a)

    AH, // Mid-central vowels (ə, ʌ, ɜ, ᵊ)

    EH, // Mid-front vowels (ɛ, A)

    IY, // Close front vowels (i, ɪ, ᵻ)

    UW, // Close back vowels (u, ʊ)

    OW, // Back rounded vowels (ɔ, O, Q, ɒ)

    AY, // Diphthong (“I” as in “eye”)

    OY, // Diphthong (“Y” as in “oy”)

    AW // Diphthong (“W” as in “ow”)
}