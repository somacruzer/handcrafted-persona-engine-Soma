using System.Numerics;

using FontStashSharp;

namespace PersonaEngine.Lib.UI.Text.Subtitles;

public interface IAnimationStrategy
{
    Vector2 CalculateScale(float progress);

    FSColor CalculateColor(FSColor startColor, FSColor endColor, float progress);
}