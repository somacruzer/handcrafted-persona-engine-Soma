using System.Collections.Concurrent;
using System.Drawing;
using System.Text;

using FontStashSharp;

using Microsoft.Extensions.Options;

using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Configuration;
using PersonaEngine.Lib.TTS.Synthesis;
using PersonaEngine.Lib.UI.Text.Rendering;

using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class SubtitleRenderer : IRenderComponent
{
    private readonly SubtitleOptions _config;

    private readonly FontProvider _fontProvider;

    private readonly ConcurrentQueue<AudioSegment> _pendingSegments = new();

    private IAnimationStrategy _animationStrategy;

    private IStreamingAudioPlayerHost? _audioPlayer;

    private bool _disposed;

    private DynamicSpriteFont _font;

    private GL _gl;

    private FSColor _highlightColor;

    private TextLayoutCache _layoutCache;

    private FSColor _normalColor;

    private SegmentManager _segmentManager;

    private TextRenderer _textRenderer;

    private int _viewportWidth,
                _viewportHeight;

    private List<ProcessedSubtitleLine> _visibleLines = new();

    public SubtitleRenderer(IOptions<SubtitleOptions> config, IStreamingAudioPlayerHost audioPlayer, FontProvider fontProvider)
    {
        _config       = config.Value;
        _fontProvider = fontProvider;
        SubscribeToAudioPlayer(audioPlayer);
    }

    public bool UseSpout => true;

    public string SpoutTarget => "Live2D";

    public void Initialize(GL gl, IView view, IInputContext input)
    {
        _gl           = gl;
        _textRenderer = new TextRenderer(_gl);

        var fontSystem = _fontProvider.GetFontSystem(_config.Font);
        _font = fontSystem.GetFont(_config.FontSize);

        var normalColor = ColorTranslator.FromHtml(_config.Color);
        _normalColor = new FSColor(normalColor.R, normalColor.G, normalColor.B, normalColor.A);
        var hColor = ColorTranslator.FromHtml(_config.HighlightColor);
        _highlightColor = new FSColor(hColor.R, hColor.G, hColor.B, hColor.A);

        _layoutCache       = new TextLayoutCache(_font, _config.SideMargin, _config.Width, _config.Height);
        _segmentManager    = new SegmentManager(_config.MaxVisibleLines);
        _animationStrategy = new PopAnimation();

        Resize();
    }

    public void Update(float deltaTime)
    {
        if ( _audioPlayer == null )
        {
            return;
        }

        var currentTime = _audioPlayer.CurrentTime;

        _segmentManager.Update(currentTime, deltaTime);
        _visibleLines = _segmentManager.GetVisibleLines(currentTime);

        _segmentManager.PositionLines(
                                      _visibleLines,
                                      _viewportWidth,
                                      _viewportHeight,
                                      _config.BottomMargin,
                                      _layoutCache.LineHeight,
                                      _config.InterSegmentSpacing);
    }

    public void Render(float deltaTime)
    {
        if ( _audioPlayer == null )
        {
            return;
        }

        var currentTime = _audioPlayer.CurrentTime;

        _textRenderer.Begin();

        foreach ( var line in _visibleLines )
        {
            foreach ( var word in line.Words )
            {
                if ( !word.HasStarted(currentTime) )
                {
                    continue;
                }

                var progress = word.IsComplete(currentTime) ? 1.0f : word.AnimationProgress;
                var scale    = _animationStrategy.CalculateScale(progress);
                var color    = _animationStrategy.CalculateColor(_highlightColor, _normalColor, progress);

                _font.DrawText(
                               _textRenderer,
                               word.Text,
                               word.Position,
                               color,
                               scale: scale,
                               effect: FontSystemEffect.Stroked,
                               effectAmount: 3,
                               origin: word.Size / 2);
            }
        }

        _textRenderer.End();
    }

    public void Dispose()
    {
        // Context is destroyed anyway when app closes.

        return;

        if ( !_disposed )
        {
            _textRenderer.Dispose();
            _disposed = true;
        }
    }

    public void Resize()
    {
        _viewportWidth  = _config.Width;
        _viewportHeight = _config.Height;
        _layoutCache.UpdateViewport(_viewportWidth, _viewportHeight);

        if ( _audioPlayer != null && _visibleLines.Count > 0 )
        {
            _segmentManager.PositionLines(
                                          _visibleLines,
                                          _viewportWidth,
                                          _viewportHeight,
                                          _config.BottomMargin,
                                          _layoutCache.LineHeight,
                                          _config.InterSegmentSpacing);
        }

        _textRenderer.OnViewportChanged(_viewportWidth, _viewportHeight);
    }

    public void SubscribeToAudioPlayer(IStreamingAudioPlayerHost audioPlayer)
    {
        _audioPlayer                     =  audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        _audioPlayer.OnPlaybackStarted   += OnPlaybackStarted;
        _audioPlayer.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    private void OnPlaybackStarted(object? sender, AudioPlaybackEventArgs e)
    {
        _pendingSegments.Enqueue(e.Segment);
        ProcessPendingSegmentsAsync();
    }

    private void OnPlaybackCompleted(object? sender, AudioPlaybackEventArgs e) { _segmentManager.RemoveSegment(e.Segment); }

    private async void ProcessPendingSegmentsAsync()
    {
        while ( _pendingSegments.TryDequeue(out var audioSegment) )
        {
            var processedSegment = await Task.Run(() => ProcessAudioSegment(audioSegment));
            _segmentManager.AddSegment(processedSegment);
        }
    }

    private ProcessedSubtitleSegment ProcessAudioSegment(AudioSegment audioSegment)
    {
        var segmentStartTime = _audioPlayer?.CurrentTime ?? 0f;
        if ( audioSegment.Tokens.Count > 0 && audioSegment.Tokens[0].StartTs.HasValue )
        {
            segmentStartTime += (float)audioSegment.Tokens[0].StartTs.Value;
        }

        var textLength  = audioSegment.Tokens.Sum(t => t.Text.Length + t.Whitespace.Length);
        var textBuilder = new StringBuilder(textLength);
        foreach ( var token in audioSegment.Tokens )
        {
            textBuilder.Append(token.Text);
            textBuilder.Append(token.Whitespace);
        }

        var fullText = textBuilder.ToString();
        var segment  = new ProcessedSubtitleSegment(audioSegment, fullText, segmentStartTime);

        var currentLine      = new ProcessedSubtitleLine(0);
        var currentLineWidth = 0f;

        // Use for loop instead of foreach to access tokens by index
        for ( var i = 0; i < audioSegment.Tokens.Count; i++ )
        {
            var token       = audioSegment.Tokens[i];
            var displayText = token.Text + token.Whitespace;
            var tokenSize   = _layoutCache.MeasureText(displayText);

            if ( currentLineWidth + tokenSize.X > _layoutCache.AvailableWidth && currentLine.Words.Any() )
            {
                segment.Lines.Add(currentLine);
                currentLine      = new ProcessedSubtitleLine(segment.Lines.Count);
                currentLineWidth = 0f;
            }

            // Determine token timing based on the specific cases
            float tokenStart,
                  tokenDuration;

            // Case 1: Token has its own timestamps
            if ( token is { StartTs: not null, EndTs: not null } )
            {
                tokenStart    = (float)token.StartTs.Value;
                tokenDuration = (float)(token.EndTs.Value - token.StartTs.Value);
            }
            // Case 2: Single token with no timestamps
            else if ( audioSegment.Tokens.Count == 1 && !token.StartTs.HasValue )
            {
                tokenStart    = 0f;
                tokenDuration = audioSegment.DurationInSeconds;
            }
            // Case 3: First token with no start timestamp and not by itself
            else if ( i == 0 && !token.StartTs.HasValue )
            {
                tokenStart = 0f;

                // If next token has a start time, use that as end
                if ( i + 1 < audioSegment.Tokens.Count && audioSegment.Tokens[i + 1].StartTs.HasValue )
                {
                    tokenDuration = (float)audioSegment.Tokens[i + 1].StartTs.Value;
                }
                else
                {
                    tokenDuration = _config.AnimationDuration;
                }
            }
            // Case 4: Token with missing timestamps, but not the first
            else
            {
                var prevToken = audioSegment.Tokens[i - 1];

                // Determine start time based on previous token
                if ( !token.StartTs.HasValue )
                {
                    // Use previous token's end time if available
                    if ( prevToken.EndTs.HasValue )
                    {
                        tokenStart = (float)prevToken.EndTs.Value;
                    }
                    // Use previous token's start time + duration if end not available
                    else if ( prevToken.StartTs.HasValue )
                    {
                        var prevDuration = _config.AnimationDuration;
                        if ( prevToken.EndTs.HasValue )
                        {
                            prevDuration = (float)(prevToken.EndTs.Value - prevToken.StartTs.Value);
                        }

                        tokenStart = (float)prevToken.StartTs.Value + prevDuration;
                    }
                    else
                    {
                        tokenStart = 0f;
                    }
                }
                else
                {
                    tokenStart = (float)token.StartTs.Value;
                }

                // Determine duration
                if ( token.EndTs.HasValue )
                {
                    tokenDuration = (float)(token.EndTs.Value - (token.StartTs.HasValue ? token.StartTs.Value : 0));
                }
                // If next token has start time, use that to calculate end
                else if ( i + 1 < audioSegment.Tokens.Count && audioSegment.Tokens[i + 1].StartTs.HasValue )
                {
                    tokenDuration = (float)audioSegment.Tokens[i + 1].StartTs.Value - tokenStart;
                }
                // Use previous token's duration if available
                else if ( prevToken.EndTs.HasValue && prevToken.StartTs.HasValue )
                {
                    tokenDuration = (float)(prevToken.EndTs.Value - prevToken.StartTs.Value);
                }
                // Default fallback duration
                else
                {
                    tokenDuration = _config.AnimationDuration;
                }
            }

            var word = new ProcessedWord(displayText, segmentStartTime + tokenStart, tokenDuration, tokenSize);
            currentLine.AddWord(word);
            currentLineWidth += tokenSize.X;
        }

        if ( currentLine.Words.Any() )
        {
            segment.Lines.Add(currentLine);
        }

        return segment;
    }
}