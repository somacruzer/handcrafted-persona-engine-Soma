using System.Numerics;

using FontStashSharp;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public class PopAnimation : IAnimationStrategy
{
    private readonly float _overshoot;

    public PopAnimation(float overshoot = 1.70158f) { _overshoot = overshoot; }

    public Vector2 CalculateScale(float progress)
    {
        var scale = 1 + (_overshoot + 1) * MathF.Pow(progress - 1, 3) + _overshoot * MathF.Pow(progress - 1, 2);

        return new Vector2(scale, scale);
    }

    public FSColor CalculateColor(FSColor startColor, FSColor endColor, float progress)
    {
        return new FSColor(
                           (byte)(startColor.R + (endColor.R - startColor.R) * progress),
                           (byte)(startColor.G + (endColor.G - startColor.G) * progress),
                           (byte)(startColor.B + (endColor.B - startColor.B) * progress),
                           (byte)(startColor.A + (endColor.A - startColor.A) * progress)
                          );
    }
}