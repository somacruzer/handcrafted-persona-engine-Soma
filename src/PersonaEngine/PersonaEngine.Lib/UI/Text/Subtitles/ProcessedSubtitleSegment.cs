using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class ProcessedSubtitleSegment
{
    public ProcessedSubtitleSegment(AudioSegment audioSegment, string fullText, float startTime)
    {
        AudioSegment = audioSegment;
        FullText     = fullText;
        StartTime    = startTime;
    }

    public AudioSegment AudioSegment { get; }

    public string FullText { get; }

    public float StartTime { get; }

    public List<ProcessedSubtitleLine> Lines { get; } = new();
}