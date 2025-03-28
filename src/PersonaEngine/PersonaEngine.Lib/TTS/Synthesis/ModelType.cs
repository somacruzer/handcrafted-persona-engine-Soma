using System.ComponentModel;

namespace PersonaEngine.Lib.TTS.Synthesis;

/// <summary>
///     Types of models
/// </summary>
public enum ModelType
{
    [Description("kokoro/model_slim.onnx")]
    KokoroSynthesis,

    [Description("kokoro/voices")] KokoroVoices,

    [Description("kokoro/phoneme_to_id.txt")]
    KokoroPhonemeMappings,

    [Description("opennlp")] OpenNLPDir,

    [Description("rvc/voices")] RVCVoices,

    [Description("rvc/vec-768-layer-12.onnx")]
    RVCHubert,

    [Description("rvc/crepe_tiny.onnx")] RVCCrepeTiny
}