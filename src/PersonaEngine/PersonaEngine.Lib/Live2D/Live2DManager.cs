using Microsoft.Extensions.Options;

using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Configuration;
using PersonaEngine.Lib.Live2D.App;
using PersonaEngine.Lib.Live2D.Framework.Rendering;
using PersonaEngine.Lib.Live2D.LipSync;
using PersonaEngine.Lib.UI;

using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PersonaEngine.Lib.Live2D;

/// <summary>
///     Wraps Live2D model loading, animation, and transformations.
/// </summary>
public class Live2DManager : IRenderComponent
{
    private readonly IStreamingAudioPlayerHost _audioPlayer;

    private readonly IOptionsMonitor<Live2DOptions> _options;

    private LAppDelegate _lapp;

    private LipSyncManager _lipSyncManager;

    public Live2DManager(IOptionsMonitor<Live2DOptions> options, IStreamingAudioPlayerHost audioPlayer)
    {
        _options     = options;
        _audioPlayer = audioPlayer;
    }

    public bool UseSpout => true;

    public string SpoutTarget => "Live2D";

    public void Update(float deltaTime) { }

    public void Render(float deltaTime)
    {
        _lapp.Update(deltaTime);
        _lapp.Run();
    }

    public void Resize() { _lapp.Resize(); }

    public void Initialize(GL gl, IView view, IInputContext input)
    {
        var config = _options.CurrentValue;
        _lapp = new LAppDelegate(new SilkNetApi(gl, config.Width, config.Height), _ => { }) { BGColor = new CubismTextureColor(0, 0, 0, 0) };

        LoadModel(config.ModelPath, config.ModelName);
    }

    public void Dispose()
    {
        // Context is destroyed anyway when app closes.

        return;

        _lapp.Dispose();
    }

    /// <summary>
    ///     Loads a Live2D model from the given path.
    /// </summary>
    public void LoadModel(string modelPath, string modelName)
    {
        var model = _lapp.Live2dManager.LoadModel(modelPath, modelName);
        model.CustomValueUpdate = true;

        model.ModelMatrix.Translate(0.0f, -1.8f);
        model.ModelMatrix.ScaleRelative(2.5f, 2.5f);

        Resize();

        _lipSyncManager = new LipSyncManager(model);
        RegisterLipSync(_audioPlayer);
    }

    public void RegisterLipSync(IStreamingAudioPlayerHost audioPlayer) { _lipSyncManager.SubscribeToAudioPlayer(audioPlayer); }
}