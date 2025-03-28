namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class ProcessedSubtitleLine
{
    public ProcessedSubtitleLine(int lineIndex) { LineIndex = lineIndex; }

    public int LineIndex { get; }

    public List<ProcessedWord> Words { get; } = new();

    public bool HasStarted(float currentTime) { return Words.Any(w => w.HasStarted(currentTime)); }

    public bool IsComplete(float currentTime) { return Words.All(w => w.IsComplete(currentTime)); }

    public void AddWord(ProcessedWord word) { Words.Add(word); }
}