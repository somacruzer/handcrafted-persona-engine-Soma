using System.ComponentModel;

using PersonaEngine.Lib.Utils;

namespace PersonaEngine.Lib;

internal static class ModelUtils
{
    public static string GetModelPath(ModelType modelType)
    {
        var enumDescription = modelType.GetDescription();
        var fullPath        = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Models", $"{enumDescription}");

        // Check file or dir exists
        if ( !Path.Exists(fullPath) )
        {
            throw new ApplicationException($"For {modelType} path {fullPath} doesn't exist");
        }

        return fullPath;
    }
}

internal static class PromptUtils
{
    public static string GetModelPath(Promptype promptype)
    {
        var enumDescription = promptype.GetDescription();
        var fullPath        = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Prompts", $"{enumDescription}");

        // Check file or dir exists
        if ( !Path.Exists(fullPath) )
        {
            throw new ApplicationException($"For {promptype} path {fullPath} doesn't exist");
        }

        return fullPath;
    }
}

public enum Promptype
{
    [Description("personality.txt")] Personality
}

public enum ModelType
{
    [Description("silero_vad_v5.onnx")] Silero,

    [Description("ggml-large-v3-turbo.bin")]
    WhisperGgmlTurbov3,

    [Description("ggml-tiny.en.bin")] WhisperGgmlTiny,

    [Description("badwords.txt")] BadWords,

    [Description("tiny_toxic_detector.onnx")]
    TinyToxic,

    [Description("tiny_toxic_detector_vocab.txt")]
    TinyToxicVocab
}