using System.Collections.Concurrent;
using System.Numerics;

using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class SegmentManager
{
    private readonly ConcurrentDictionary<ProcessedSubtitleLine, LineData> _lineMap = new();

    private readonly Lock _lockObject = new();

    private readonly int _maxVisibleLines;

    private readonly List<ProcessedSubtitleSegment> _segments = new();

    private Queue<LineData> _lineQueue = new();

    public SegmentManager(int maxVisibleLines) { _maxVisibleLines = Math.Max(1, maxVisibleLines); }

    public void AddSegment(ProcessedSubtitleSegment segment)
    {
        lock (_lockObject)
        {
            _segments.Add(segment);
        }
    }

    public void RemoveSegment(AudioSegment audioSegment)
    {
        lock (_lockObject)
        {
            var segmentIndex = _segments.FindIndex(s => s.AudioSegment == audioSegment);
            if ( segmentIndex >= 0 )
            {
                var segment = _segments[segmentIndex];
                _segments.RemoveAt(segmentIndex);

                // Remove lines from dictionary and queue
                var needRebuildQueue = false;
                foreach ( var line in segment.Lines )
                {
                    if ( _lineMap.TryRemove(line, out _) )
                    {
                        needRebuildQueue = true;
                    }
                }

                if ( needRebuildQueue )
                {
                    var newQueue = new Queue<LineData>(_lineQueue.Where(data => !segment.Lines.Contains(data.Line)));
                    _lineQueue = newQueue;
                }
            }
        }
    }

    public List<ProcessedSubtitleLine> GetVisibleLines(float currentTime)
    {
        lock (_lockObject)
        {
            UpdateLineQueue(currentTime);

            return _lineQueue.Select(data => data.Line).ToList();
        }
    }

    private void UpdateLineQueue(float currentTime)
    {
        foreach ( var segment in _segments )
        {
            foreach ( var line in segment.Lines )
            {
                if ( !_lineMap.ContainsKey(line) && line.HasStarted(currentTime) )
                {
                    if ( _lineQueue.Count >= _maxVisibleLines )
                    {
                        var removedLine = _lineQueue.Dequeue();
                        _lineMap.TryRemove(removedLine.Line, out _);
                    }

                    var lineData = new LineData(line, currentTime);
                    _lineQueue.Enqueue(lineData);
                    _lineMap[line] = lineData;
                }
            }
        }
    }

    public void PositionLines(
        List<ProcessedSubtitleLine> lines,
        int                         viewportWidth,
        int                         viewportHeight,
        float                       bottomMargin,
        float                       lineHeight,
        float                       lineSpacing)
    {
        if ( lines.Count == 0 )
        {
            return;
        }

        var currentY = viewportHeight - bottomMargin - lineHeight / 2;

        for ( var i = lines.Count - 1; i >= 0; i-- )
        {
            var line       = lines[i];
            var totalWidth = line.Words.Sum(w => w.Size.X);
            var startX     = (viewportWidth - totalWidth) / 2f;

            foreach ( var word in line.Words )
            {
                word.Position =  new Vector2(startX + word.Size.X / 2, currentY);
                startX        += word.Size.X;
            }

            currentY -= lineHeight + lineSpacing;
        }
    }

    public void Update(float currentTime, float deltaTime)
    {
        lock (_lockObject)
        {
            var completedSegments = new List<ProcessedSubtitleSegment>();

            foreach ( var segment in _segments )
            {
                foreach ( var line in segment.Lines )
                {
                    foreach ( var word in line.Words )
                    {
                        word.UpdateAnimationProgress(currentTime, deltaTime);
                    }
                }

                // Check if segment is complete
                if ( segment.Lines.All(l => l.Words.All(w => w.IsComplete(currentTime))) )
                {
                    completedSegments.Add(segment);
                }
            }

            // Remove completed segments
            foreach ( var segment in completedSegments )
            {
                _segments.Remove(segment);
            }

            UpdateLineQueue(currentTime);
        }
    }

    private class LineData
    {
        public LineData(ProcessedSubtitleLine line, float currentTime)
        {
            Line      = line;
            AddedTime = currentTime;
        }

        public ProcessedSubtitleLine Line { get; }

        public float AddedTime { get; }
    }
}