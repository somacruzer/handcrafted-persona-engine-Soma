namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public record PhonemeTimingInfo
{
    public PhonemeTimingInfo(string phoneme, double startTime, double endTime)
    {
        Phoneme   = phoneme;
        StartTime = startTime;
        EndTime   = endTime;
    }

    public string Phoneme { get; }

    public double StartTime { get; }

    public double EndTime { get; }

    public double Duration => EndTime - StartTime;
}