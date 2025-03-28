using Hexa.NET.ImGui;

namespace PersonaEngine.Lib.UI.GUI;

public readonly struct ImGuiFontConfig
{
    public ImGuiFontConfig(string fontPath, float[] fontSizes, Func<ImGuiIOPtr, IntPtr>? getGlyphRange = null)
    {
        foreach ( var fontSize in fontSizes )
        {
            if ( fontSize <= 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(fontSize));
            }
        }

        FontPath      = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
        FontSizes     = fontSizes;
        GetGlyphRange = getGlyphRange;
    }

    public string FontPath { get; }

    public float[] FontSizes { get; }

    public Func<ImGuiIOPtr, IntPtr>? GetGlyphRange { get; }
}