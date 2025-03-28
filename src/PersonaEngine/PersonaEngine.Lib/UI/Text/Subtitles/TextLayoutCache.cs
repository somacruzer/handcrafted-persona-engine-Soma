using System.Numerics;

using FontStashSharp;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class TextLayoutCache
{
    private readonly Dictionary<string, Vector2> _cache = new();

    private readonly DynamicSpriteFont _font;

    private readonly object _lock = new();

    public TextLayoutCache(DynamicSpriteFont font, float sideMargin, int viewportWidth, int viewportHeight)
    {
        _font          = font;
        SideMargin     = sideMargin;
        ViewportWidth  = viewportWidth;
        ViewportHeight = viewportHeight;
        LineHeight     = _font.MeasureString("Ay").Y;
    }

    public float SideMargin { get; private set; }

    public int ViewportWidth { get; private set; }

    public int ViewportHeight { get; private set; }

    public float AvailableWidth => ViewportWidth - 2 * SideMargin;

    public float LineHeight { get; private set; }

    public void UpdateViewport(int width, int height)
    {
        ViewportWidth  = width;
        ViewportHeight = height;
    }

    public Vector2 MeasureText(string text)
    {
        lock (_lock)
        {
            if ( _cache.TryGetValue(text, out var size) )
            {
                return size;
            }

            size         = _font.MeasureString(text);
            _cache[text] = size;

            return size;
        }
    }
}