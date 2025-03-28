#pragma warning disable SKEXP0001

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using PersonaEngine.Lib.ASR.Transcriber;
using PersonaEngine.Lib.ASR.VAD;
using PersonaEngine.Lib.Audio;
using PersonaEngine.Lib.Audio.Player;
using PersonaEngine.Lib.Configuration;
using PersonaEngine.Lib.Core;
using PersonaEngine.Lib.Live2D;
using PersonaEngine.Lib.LLM;
using PersonaEngine.Lib.Profanity;
using PersonaEngine.Lib.TTS.Audio;
using PersonaEngine.Lib.TTS.RVC;
using PersonaEngine.Lib.TTS.Synthesis;
using PersonaEngine.Lib.UI;
using PersonaEngine.Lib.UI.Common;
using PersonaEngine.Lib.UI.GUI;
using PersonaEngine.Lib.UI.RouletteWheel;
using PersonaEngine.Lib.UI.Text.Subtitles;
using PersonaEngine.Lib.Vision;

namespace PersonaEngine.Lib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration configuration, Action<IKernelBuilder>? configureKernel = null)
    {
        services.Configure<AvatarAppConfig>(configuration.GetSection("Config"));

        services.AddConversation(configuration, configureKernel);
        services.AddUI(configuration);
        services.AddSystemAudioPlayer();
        // services.AddVBANStreamingPlayer();

        services.AddSingleton<AvatarApp>();

        return services;
    }

    public static IServiceCollection AddConversation(this IServiceCollection services, IConfiguration configuration, Action<IKernelBuilder>? configureKernel = null)
    {
        services.AddASRSystem(configuration);
        services.AddTTSSystem(configuration);
        services.AddRVC(configuration);
#pragma warning disable SKEXP0010
        services.AddLLM(configuration, configureKernel);
#pragma warning restore SKEXP0010
        services.AddChatEngineSystem(configuration);

        services.AddSingleton<ConversationManager>();
        services.AddSingleton<IStartupTask>(x => x.GetRequiredService<ConversationManager>());
        services.AddSingleton<ProfanityDetector>();

        return services;
    }

    public static IServiceCollection AddASRSystem(this IServiceCollection services, IConfiguration configuration)
    {
        var realtimeSpeechTranscriptorOptions = new RealtimeSpeechTranscriptorOptions {
                                                                                          AutodetectLanguageOnce        = false,                    // Flag to detect the language only once or for each segment
                                                                                          IncludeSpeechRecogizingEvents = true,                     // Flag to include speech recognizing events (RealtimeSegmentRecognizing)
                                                                                          RetrieveTokenDetails          = false,                    // Flag to retrieve token details
                                                                                          LanguageAutoDetect            = false,                    // Flag to auto-detect the language
                                                                                          Language                      = new CultureInfo("en-US"), // Language to use for transcription
                                                                                          Prompt                        = "Aria, Joobel, Aksel"
                                                                                      };

        var siletroOptions = new SileroVadOptions(ModelUtils.GetModelPath(ModelType.Silero)) {
                                                                                                 Threshold    = 0.5f, // The threshold for Silero VAD. The default is 0.5f.
                                                                                                 ThresholdGap = 0.15f // The threshold gap for Silero VAD. The default is 0.15f.
                                                                                             };

        var vadOptions = new VadDetectorOptions { MinSpeechDuration = TimeSpan.FromMilliseconds(250), MinSilenceDuration = TimeSpan.FromMilliseconds(200) };

        var realTimeOptions = new RealtimeOptions();

        services.AddSingleton<IVadDetector>(sp => new SileroVadDetector(vadOptions, siletroOptions));

        services.AddSingleton<IRealtimeSpeechTranscriptor>(sp => new RealtimeTranscriptor(
                                                                                          // new WhisperOnnxSpeechTranscriptorFactory(ModelUtils.GetModelPath(ModelType.WhisperOnnxGpuFp32)),
                                                                                          new WhisperSpeechTranscriptorFactory(ModelUtils.GetModelPath(ModelType.WhisperGgmlTurbov3)),
                                                                                          sp.GetRequiredService<IVadDetector>(),
                                                                                          new WhisperSpeechTranscriptorFactory(ModelUtils.GetModelPath(ModelType.WhisperGgmlTiny)),
                                                                                          realtimeSpeechTranscriptorOptions,
                                                                                          realTimeOptions,
                                                                                          sp.GetRequiredService<ILogger<RealtimeTranscriptor>>()));

        services.AddSingleton<IMicrophone, MicrophoneInputNAudioSource>();
        services.AddSingleton<IAwaitableAudioSource>(sp => sp.GetRequiredService<IMicrophone>());

        return services;
    }

    public static IServiceCollection AddSystemAudioPlayer(this IServiceCollection services)
    {
        services.AddSingleton<PortAudioStreamingPlayer>();
        services.AddSingleton<AggregatedStreamingAudioPlayer>();
        services.AddSingleton<IAggregatedStreamingAudioPlayer>(provider => provider.GetRequiredService<AggregatedStreamingAudioPlayer>());
        services.AddSingleton<IStreamingAudioPlayer>(provider => provider.GetRequiredService<PortAudioStreamingPlayer>());
        services.AddSingleton<IStreamingAudioPlayerHost>(provider => provider.GetRequiredService<PortAudioStreamingPlayer>());

        return services;
    }

    public static IServiceCollection AddChatEngineSystem(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChatEngineOptions>(options => { options.SystemPrompt = File.ReadAllText(PromptUtils.GetModelPath(Promptype.Personality)); });

        services.AddSingleton<IChatHistoryManagerFactory, ChatHistoryManagerFactory>();
        services.AddSingleton<IChatHistoryManager>(sp => sp.GetRequiredService<IChatHistoryManagerFactory>().Create());
        services.AddSingleton<IChatEngine, SemanticKernelChatEngine>();
        services.AddSingleton<IVisualChatEngine, VisualQASemanticKernelChatEngine>();
        services.AddSingleton<IVisualQAService, VisualQAService>();
        services.AddSingleton<WindowCaptureService>();

        return services;
    }

    [Experimental("SKEXP0010")]
    public static IServiceCollection AddLLM(this IServiceCollection services, IConfiguration configuration, Action<IKernelBuilder>? configureKernel = null)
    {
        services.Configure<LlmOptions>(configuration.GetSection("Config:Llm"));

        services.AddSingleton(sp =>
                              {
                                  var llmOptions    = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
                                  var kernelBuilder = Kernel.CreateBuilder();

                                  kernelBuilder.AddOpenAIChatCompletion(llmOptions.TextModel, new Uri(llmOptions.TextEndpoint), llmOptions.TextApiKey, serviceId: "text");
                                  kernelBuilder.AddOpenAIChatCompletion(llmOptions.VisionEndpoint, new Uri(llmOptions.VisionEndpoint), llmOptions.VisionApiKey, serviceId: "vision");

                                  configureKernel?.Invoke(kernelBuilder);

                                  return kernelBuilder.Build();
                              });

        return services;
    }

    public static IServiceCollection AddTTSSystem(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // Add configuration
        services.Configure<TtsConfiguration>(configuration.GetSection("Config:Tts"));
        services.Configure<KokoroVoiceOptions>(configuration.GetSection("Config:Tts:Voice"));

        // Add core TTS components
        services.AddSingleton<ITtsEngine, TtsEngine>();

        // Add text processing components
        services.AddSingleton<ITextProcessor, TextProcessor>();
        services.AddSingleton<ITextNormalizer, TextNormalizer>();
        services.AddSingleton<ISentenceSegmenter, SentenceSegmenter>();
        services.AddSingleton<IMlSentenceDetector>(provider =>
                                                   {
                                                       var logger        = provider.GetRequiredService<ILogger<OpenNlpSentenceDetector>>();
                                                       var modelProvider = provider.GetRequiredService<IModelProvider>();
                                                       var basePath      = modelProvider.GetModelAsync(TTS.Synthesis.ModelType.OpenNLPDir).GetAwaiter().GetResult().Path;
                                                       var modelPath     = Path.Combine(basePath, "EnglishSD.nbin");

                                                       return new OpenNlpSentenceDetector(modelPath, logger);
                                                   });

        // Add phoneme processing components
        services.AddSingleton<IPhonemizer>(provider =>
                                           {
                                               var posTagger = provider.GetRequiredService<IPosTagger>();
                                               var lexicon   = provider.GetRequiredService<ILexicon>();
                                               var fallback  = provider.GetRequiredService<IFallbackPhonemizer>();

                                               return new PhonemizerG2P(posTagger, lexicon, fallback);
                                           });

        services.AddSingleton<IPosTagger>(provider =>
                                          {
                                              var logger        = provider.GetRequiredService<ILogger<OpenNlpPosTagger>>();
                                              var modelProvider = provider.GetRequiredService<IModelProvider>();

                                              var basePath  = modelProvider.GetModelAsync(TTS.Synthesis.ModelType.OpenNLPDir).GetAwaiter().GetResult().Path;
                                              var modelPath = Path.Combine(basePath, "EnglishPOS.nbin");

                                              return new OpenNlpPosTagger(modelPath, logger);
                                          });

        services.AddSingleton<ILexicon, Lexicon>();
        services.AddSingleton<IFallbackPhonemizer, EspeakFallbackPhonemizer>();

        services.AddSingleton<IAudioSynthesizer, OnnxAudioSynthesizer>();
        services.AddSingleton<IModelProvider>(provider =>
                                              {
                                                  var config = provider.GetRequiredService<IOptions<TtsConfiguration>>().Value;
                                                  var logger = provider.GetRequiredService<ILogger<FileModelProvider>>();

                                                  return new FileModelProvider(config.ModelDirectory, logger);
                                              });

        services.AddSingleton<IKokoroVoiceProvider, KokoroVoiceProvider>();
        services.AddSingleton<ITtsCache, TtsMemoryCache>();
        services.AddSingleton<IAudioFilter, BlacklistAudioFilter>();

        return services;
    }

    public static IServiceCollection AddUI(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.Configure<Live2DOptions>(configuration.GetSection("Config:Live2D"));
        services.Configure<SubtitleOptions>(configuration.GetSection("Config:Subtitle"));
        services.Configure<RouletteWheelOptions>(configuration.GetSection("Config:RouletteWheel"));

        services.AddSingleton<IRenderComponent, Live2DManager>();
        services.AddSingleton<IRenderComponent, SubtitleRenderer>();
        services.AddSingleton<RouletteWheel>();
        services.AddSingleton<IRenderComponent>(x => x.GetRequiredService<RouletteWheel>());
        services.AddConfigEditor();

        services.AddSingleton<FontProvider>();
        services.AddSingleton<IStartupTask>(x => x.GetRequiredService<FontProvider>());

        return services;
    }

    public static IServiceCollection AddConfigEditor(this IServiceCollection services)
    {
        services.AddSingleton<IUiConfigurationManager, UiConfigurationManager>();
        services.AddSingleton<IEditorStateManager, EditorStateManager>();
        services.AddSingleton<IConfigSectionRegistry, ConfigSectionRegistry>();
        services.AddSingleton<IUiThemeManager, UiThemeManager>();
        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<IRenderComponent, ConfigEditorComponent>();

        services.AddSingleton<TtsConfigEditor>();
        services.AddSingleton<RouletteWheelEditor>();
        services.AddSingleton<ChatEditor>();

        services.AddSingleton<IStartupTask, ConfigSectionRegistrationTask>();

        return services;
    }

    public static IServiceCollection AddVBANStreamingPlayer(
        this IServiceCollection services)
    {
        // Register the audio player
        services.AddSingleton<IStreamingAudioPlayer>(sp =>
                                                         VBANAudioPlayer.Create(
                                                                                "127.0.0.1",
                                                                                6980,
                                                                                "TTSAudioVBAN"
                                                                               ));

        return services;
    }

    public static IServiceCollection AddRVC(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RVCFilterOptions>(configuration.GetSection("Config:Tts:Rvc"));

        services.AddSingleton<IRVCVoiceProvider, RVCVoiceProvider>();
        services.AddSingleton<IAudioFilter, RVCFilter>();

        return services;
    }
}