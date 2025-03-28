using Microsoft.ML.OnnxRuntime;

namespace PersonaEngine.Lib.ASR.VAD;

internal class SileroInferenceState(OrtIoBinding binding)
{
    /// Array for storing the context + input

    public float[] State { get; set; } = new float[SileroConstants.StateSize];

    // The state for the next inference
    public float[] PendingState { get; set; } = new float[SileroConstants.StateSize];

    public float[] Output { get; set; } = new float[SileroConstants.OutputSize];

    public OrtIoBinding Binding { get; } = binding;
}