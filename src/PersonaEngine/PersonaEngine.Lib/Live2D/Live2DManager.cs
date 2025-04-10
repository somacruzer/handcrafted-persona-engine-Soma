using Microsoft.Extensions.Options;

using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Configuration;
using PersonaEngine.Lib.Live2D.App;
using PersonaEngine.Lib.Live2D.Behaviour;
using PersonaEngine.Lib.Live2D.Behaviour.LipSync;
using PersonaEngine.Lib.Live2D.Framework.Rendering;
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

    private IList<ILive2DAnimationService> _live2DAnimationServices;

    public Live2DManager(IOptionsMonitor<Live2DOptions> options, IEnumerable<ILive2DAnimationService> live2DAnimationServices,IStreamingAudioPlayerHost audioPlayer)
    {
        _options     = options;
        _live2DAnimationServices = live2DAnimationServices.ToList();
        _audioPlayer = audioPlayer;
    }

    public bool UseSpout => true;

    public string SpoutTarget => "Live2D";

    public int Priority => 100;

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

        // model.ModelMatrix.Translate(0.0f, -1.8f);
        // model.ModelMatrix.ScaleRelative(2.5f, 2.5f);

        Resize();

        foreach ( var animationService in _live2DAnimationServices )
        {
            animationService.SubscribeToAudioPlayerHost(_audioPlayer);
            model.ValueUpdate += _ => animationService.Update(LAppPal.DeltaTime);
            animationService.Start(model);
        }
    }
}