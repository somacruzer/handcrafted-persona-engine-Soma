namespace PersonaEngine.Lib.Live2D.LipSync;

/// <summary>
///     Defines configuration parameters for lip sync animation behavior.
/// </summary>
public class LipSyncTemplate
{
    public LipSyncTemplate(
        float anticipationMargin        = 0.05f,
        float lingerMargin              = 0.05f,
        float gaussianSpreadMinimum     = 0.02f,
        float baseSmoothingTime         = 0.1f,
        float minSmoothingTime          = 0.02f,
        float smoothingDifferenceFactor = 0.3f)
    {
        AnticipationMargin        = anticipationMargin;
        LingerMargin              = lingerMargin;
        GaussianSpreadMinimum     = gaussianSpreadMinimum;
        BaseSmoothingTime         = baseSmoothingTime;
        MinSmoothingTime          = minSmoothingTime;
        SmoothingDifferenceFactor = smoothingDifferenceFactor;
    }

    /// <summary>
    ///     Time in seconds to begin transitioning to a viseme before its actual start time.
    /// </summary>
    public float AnticipationMargin { get; set; }

    /// <summary>
    ///     Time in seconds to continue showing a viseme after its actual end time.
    /// </summary>
    public float LingerMargin { get; set; }

    /// <summary>
    ///     Minimum spread value for the Gaussian influence curve to prevent overly sharp transitions.
    /// </summary>
    public float GaussianSpreadMinimum { get; set; }

    /// <summary>
    ///     Base time in seconds for smoothing parameter changes.
    /// </summary>
    public float BaseSmoothingTime { get; set; }

    /// <summary>
    ///     Minimum time in seconds for smoothing parameter changes, used for fast transitions.
    /// </summary>
    public float MinSmoothingTime { get; set; }

    /// <summary>
    ///     Factor to adjust smoothing time based on the difference between current and target values.
    /// </summary>
    public float SmoothingDifferenceFactor { get; set; }

    /// <summary>
    ///     Creates a new template with adjusted anticipation and linger margins.
    /// </summary>
    public LipSyncTemplate WithMargins(float anticipationMargin, float lingerMargin)
    {
        return new LipSyncTemplate(
                                   anticipationMargin,
                                   lingerMargin,
                                   GaussianSpreadMinimum,
                                   BaseSmoothingTime,
                                   MinSmoothingTime,
                                   SmoothingDifferenceFactor);
    }

    /// <summary>
    ///     Creates a new template with adjusted smoothing parameters.
    /// </summary>
    public LipSyncTemplate WithSmoothingValues(
        float baseSmoothingTime,
        float minSmoothingTime,
        float smoothingDifferenceFactor)
    {
        return new LipSyncTemplate(
                                   AnticipationMargin,
                                   LingerMargin,
                                   GaussianSpreadMinimum,
                                   baseSmoothingTime,
                                   minSmoothingTime,
                                   smoothingDifferenceFactor);
    }

    /// <summary>
    ///     Creates a new template with an adjusted Gaussian spread minimum.
    /// </summary>
    public LipSyncTemplate WithGaussianSpreadMinimum(float gaussianSpreadMinimum)
    {
        return new LipSyncTemplate(
                                   AnticipationMargin,
                                   LingerMargin,
                                   gaussianSpreadMinimum,
                                   BaseSmoothingTime,
                                   MinSmoothingTime,
                                   SmoothingDifferenceFactor);
    }
}

/// <summary>
///     Provides predefined LipSyncTemplate configurations.
/// </summary>
public static class LipSyncTemplates
{
    /// <summary>
    ///     Default template with balanced values suitable for most cases.
    /// </summary>
    public static LipSyncTemplate Default { get; } = new();

    /// <summary>
    ///     Template optimized for fast speech with quicker transitions.
    /// </summary>
    public static LipSyncTemplate FastSpeech { get; } = new(
                                                            0.03f,
                                                            0.03f,
                                                            0.015f,
                                                            0.07f,
                                                            0.01f,
                                                            0.4f);

    /// <summary>
    ///     Template optimized for slow, deliberate speech with smoother transitions.
    /// </summary>
    public static LipSyncTemplate SlowSpeech { get; } = new(
                                                            0.08f,
                                                            0.08f,
                                                            0.03f,
                                                            0.15f,
                                                            0.05f,
                                                            0.2f);

    /// <summary>
    ///     Template with exaggerated mouth movements for emphasis.
    /// </summary>
    public static LipSyncTemplate Exaggerated { get; } = new(
                                                             0.1f,
                                                             0.1f,
                                                             0.04f,
                                                             0.08f,
                                                             0.01f,
                                                             0.5f);
}