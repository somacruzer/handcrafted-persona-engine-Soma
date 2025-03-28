namespace PersonaEngine.Lib.Configuration;

/// <summary>
///     Configuration for the subtitle renderer
/// </summary>
public class SubtitleOptions
{
    /// <summary>
    ///     The font file name
    /// </summary>
    public string Font { get; set; } = "DynaPuff_Condensed-Bold.ttf";

    /// <summary>
    ///     The font size
    /// </summary>
    public int FontSize { get; set; } = 72;

    /// <summary>
    ///     The text color in HTML format
    /// </summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    ///     The highlight color in HTML format
    /// </summary>
    public string HighlightColor { get; set; } = "#4FC3F7";

    /// <summary>
    ///     The bottom margin in pixels
    /// </summary>
    public int BottomMargin { get; set; } = 50;

    /// <summary>
    ///     The side margin in pixels
    /// </summary>
    public int SideMargin { get; set; } = 100;

    /// <summary>
    ///     The spacing between segments in pixels
    /// </summary>
    public float InterSegmentSpacing { get; set; } = 16f;

    /// <summary>
    ///     The maximum number of visible lines
    /// </summary>
    public int MaxVisibleLines { get; set; } = 6;

    /// <summary>
    ///     The default duration for animations when timing isn't specified
    /// </summary>
    public float AnimationDuration { get; set; } = 0.3f;

    public int Width { get; set; } = 1920;

    public int Height { get; set; } = 1080;
}