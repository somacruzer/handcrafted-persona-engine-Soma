namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public record TimeCodedViseme
{
    public TimeCodedViseme(VisemeType viseme, double startTime, double endTime, float intensity = 1.0f)
    {
        Viseme    = viseme;
        StartTime = startTime;
        EndTime   = endTime;
        Intensity = intensity;
    }

    public VisemeType Viseme { get; }

    public double StartTime { get; }

    public double EndTime { get; }

    public float Intensity { get; }

    // Allow creating a new instance with updated properties
    public TimeCodedViseme WithIntensity(float newIntensity) { return new TimeCodedViseme(Viseme, StartTime, EndTime, newIntensity); }

    public TimeCodedViseme WithTimeRange(double newStartTime, double newEndTime) { return new TimeCodedViseme(Viseme, newStartTime, newEndTime, Intensity); }
}