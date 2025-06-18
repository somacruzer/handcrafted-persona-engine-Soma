﻿using PersonaEngine.Lib.Core.Conversation.Abstractions.Configuration;

namespace PersonaEngine.Lib.Configuration;

public record AvatarAppConfig
{
    public WindowConfiguration Window { get; set; } = new();

    public LlmOptions Llm { get; set; } = new();

    public TtsConfiguration Tts { get; set; } = new();

    public AsrConfiguration Asr { get; set; } = new();

    public MicrophoneConfiguration Microphone { get; set; } = new();

    public SubtitleOptions Subtitle { get; set; } = new();

    public Live2DOptions Live2D { get; set; } = new();

    public Tha4Options Tha4 { get; set; } = new();

    public SpoutConfiguration[] SpoutConfigs { get; set; } = [];

    public VisionConfig Vision { get; set; } = new();

    public RouletteWheelOptions RouletteWheel { get; set; } = new();

    public ConversationOptions Conversation { get; set; } = new();
    
    // This would need to be seperate per configured conversation session
    public ConversationContextOptions ConversationContext { get; set; } = new();
}