namespace PersonaEngine.Lib.Live2D.LipSync.Viseme;

public class VisemeMappingConfig
{
    // Stress multipliers
    public float PrimaryStressMultiplier { get; set; } = 1.2f;

    public float SecondaryStressMultiplier { get; set; } = 1.1f;

    // Intensity settings
    public float MinIntensity { get; set; } = 0.8f;

    public float MaxIntensity { get; set; } = 1.1f;

    // Duration thresholds (as ratios of average duration)
    public double MinDurationThresholdRatio { get; set; } = 0.8;

    public double MaxDurationThresholdRatio { get; set; } = 1.2;

    // Merging settings
    public double MaxGapForMerging { get; set; } = 0.01;

    public bool InsertNeutralForGaps { get; set; } = true;
}