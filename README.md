<div align="center">
<h1>
Persona Engine
</h1>
<p>
An AI-powered interactive avatar engine using Live2D, Large Language Models (LLMs), Automatic Speech Recognition (ASR), Text-to-Speech (TTS), and Real-time Voice Cloning (RVC). Designed primarily for VTubing, streaming, and virtual assistant applications.
</p>

<img src="assets/header.png" alt="Persona Engine"  height="450"  >

<h2>✨ See it in Action! ✨</h2>
<p>Watch Persona Engine bring a character to life:</p>
<a href="YOUR_DEMO_VIDEO_URL_HERE" target="_blank">
  <img src="URL_TO_YOUR_VIDEO_THUMBNAIL_HERE" alt="Persona Engine Demo Video" width="600">
  <!-- Suggestion: Make this thumbnail look like a video player with a play button -->
</a>
<br/>
<img
  src="assets/demo_1.png"
  alt="Persona Engine Showcase"
  width="500"
>
</div>

## Overview

Persona Engine brings 2D digital characters to life. It listens to user voice input, processes it using powerful AI language models, generates a response based on a defined personality, speaks the response using synthesized and potentially cloned voice, and animates a Live2D avatar accordingly. The visual output can be easily integrated into streaming software like OBS Studio via Spout.

## ✨ Features

*   **Live2D Avatar Integration:** Loads and renders Live2D models. (Potential for lip-sync/animation triggers).
*   **AI-Driven Conversation:** Connects to OpenAI-compatible LLM APIs (local/cloud), uses `personality.txt`.
*   **Voice Interaction:** Microphone input (NAudio/PortAudio), Silero VAD, Whisper ASR (Whisper.net).
*   **Advanced Text-to-Speech (TTS):** Sophisticated pipeline (normalization, segmentation, phonemization, ONNX synthesis), supports custom `kokoro` voices.
*   **Real-time Voice Cloning (RVC):** Integrates RVC models for real-time voice transformation.
*   **Customizable Subtitles:** Real-time display with extensive configuration options.
*   **Screen Awareness (Experimental):** Optional Vision module to capture windows and use Vision LLMs.
*   **Interactive Roulette Wheel (Experimental):** Optional on-screen spinning wheel.
*   **Streaming Output (Spout):** Direct visual output to OBS/Spout software.
*   **Audio Output:** Plays audio via PortAudio.
*   **Configuration:** `appsettings.json` and integrated UI editor.
*   **Profanity Detection:** Basic + ML-based filtering.

<div align="center">
<br>
<h2>💬 Join Our Community! 💬</h2>
<p>
Need help getting started? Have questions or ideas? Want to see a live demo or interact directly with a Persona Engine instance? Join the Discord server!
</p>
<a href="https://discord.gg/p3CXEyFtrA" target="_blank">
<img src="assets/discord.png" alt="Join Discord Img"
  width="400"
  /></a>
  <br>
  <a href="https://discord.gg/p3CXEyFtrA" target="_blank">
<img src="https://img.shields.io/discord/1347649495646601419?label=Join%20Discord&logo=discord&style=for-the-badge" alt="Join Discord Badge" />
</a>
</div>

## ⚙️ Architecture / How it Works

The engine follows a general pipeline:

1.  **Input:** 🎤 Mic -> 🗣️ VAD -> 📝 ASR (Whisper) -> (Optional) 👀 Vision.
2.  **Processing:** 🧠 LLM (with Personality) -> 💬 Response -> (Optional) 🤬 Profanity Check.
3.  **Output:** 🔊 TTS -> 🎤 RVC (Optional) -> 🎭 Live2D Animation -> 📜 Subtitles -> 🎶 Audio Playback -> 📺 Spout Visuals.

<div align="center">
<br/>
<img
  src="assets/diagram.png"
  alt="Persona Engine Showcase"
  width="600"
>
<br/>
</div>

## 📋 Prerequisites

*   **Operating System:** Currently, the project is primarily developed and tested on **Windows**. Pre-built releases are Windows-only. While the core .NET code is cross-platform, running on Linux/macOS would require building from source and ensuring all native dependencies (CUDA, Spout, Audio libraries, etc.) are correctly installed and available for those platforms.
*   **GPU (NVIDIA CUDA Recommended):** A GPU with CUDA support is **highly recommended** for acceptable performance, especially for Whisper, ONNX Runtime, and RVC.
    *   Ensure you have compatible NVIDIA drivers installed.
    *   The project is currently configured to leverage CUDA via ONNX Runtime and Whisper.net.
    *   *Note:* While ONNX Runtime supports other execution providers (CPU, DirectML), adjustments to the codebase would be needed to utilize them effectively. Performance without a GPU may be very slow.
*   **.NET 9.0 Runtime:** Required to *run* the application (usually included in pre-built releases or installed automatically).
*   **.NET 9.0 SDK:** Required *only if building from source*.
*   **Models & Resources (Essential - Download Separately):**
    *   **❗ Important:** These resources are **NOT** included in the code repository or the pre-built releases. You **must** download or provide them yourself.
    *   **Live2D Avatar Model:** Your own model files (place in `Resources/Live2D/Avatars/`).
    *   **Whisper Model:** Download a GGUF format model (e.g., `ggml-large-v3-turbo.bin`) from Hugging Face or other sources. Place it in `Resources/Models/`.
    *   **TTS Resources:**
        *   TTS voice models (e.g., `kokoro` format) placed in the configured `ModelDirectory` (`Resources/Models/TTS/` by default).
        *   `espeak-ng` installed and accessible in your system's PATH (if using the Espeak phonemizer fallback). Download from [espeak-ng releases](https://github.com/espeak-ng/espeak-ng/releases).
        *   Other required TTS dependency models (like OpenNLP sentence models, phonemizer models) need to be present (check `Resources/Models/TTS/` and subfolders for expected locations/names based on configuration).
    *   **Other ONNX Models:** Ensure required utility models (like `silero_vad.onnx`) are present in `Resources/Models/`. These might be included in releases, but verify.
    *   **(Optional) RVC Models:** If using RVC, place the required model files (`.pth`, `.index`) in an accessible location and configure the paths in `appsettings.json`.
*   **LLM Access:** An OpenAI-compatible API endpoint (URL) and potentially an API key. This can be a local server (like llama.cpp, Ollama + LiteLLM) or a cloud service (OpenAI, Groq, etc.).

## 🚀 Getting Started

There are two main ways to get Persona Engine running:

### Method 1: Using Pre-built Releases (Recommended for Windows Users)

This is the easiest way to get started on Windows.

1.  **Download:** Go to the [**Releases**](https://github.com/fagenorn/handcrafted-persona-engine/releases) page of this repository and download the latest release `.zip` file (e.g., `PersonaEngine_vX.Y.Z.zip`).
2.  **Extract:** Unzip the downloaded file to a location of your choice.
3.  **Configure `appsettings.json`:**
    *   Open `appsettings.json` located in the extracted application directory.
    *   **Crucially, update:**
        *   `Llm.TextEndpoint`, `Llm.TextModel`, `Llm.TextApiKey`.
        *   `Llm.VisionEndpoint`, `Llm.VisionModel`, `Llm.VisionApiKey` (if using Vision).
        *   `Live2D.ModelName` to match your avatar's folder name under `Resources/Live2D/Avatars/`.
        *   `Tts.EspeakPath` if `espeak-ng` is not automatically found in your PATH.
        *   Configure `Tts.Voice` and `Tts.Rvc` options as needed.
        *   Review and adjust `SpoutConfigs`, `Subtitle`, `Vision`, `RouletteWheel` settings.
4.  **Run:** Execute the `PersonaEngine.exe` file located in the extracted application directory.

### Method 2: Building from Source (Advanced / Other Platforms)

This method is for developers or users wanting to run on potentially unsupported platforms (Linux/macOS) or modify the code. **Note:** Running on non-Windows platforms is untested and may require significant effort to ensure all native dependencies are available and correctly linked.

1.  **Install Prerequisites:**
    *   Git: [https://git-scm.com/](https://git-scm.com/)
    *   .NET 9.0 SDK: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
    *   (Windows) Ensure CUDA toolkit/drivers are installed if using GPU features.
    *   (Linux/macOS) You will need to manually ensure equivalent native libraries (CUDA, PortAudio, potentially Spout alternatives, espeak-ng) are installed and accessible to the .NET runtime. This can be complex.
2.  **Clone the Repository:**
    ```bash
    git clone https://github.com/fagenorn/handcrafted-persona-engine
    cd handcrafted-persona-engine
    ```
3.  **Restore Dependencies:**
    ```bash
    dotnet restore src/PersonaEngine/PersonaEngine.sln
    ```
4.  **Build the Solution:**
    ```bash
    # For Debug build (output typically in src/PersonaEngine/PersonaEngine.App/bin/Debug/net9.0/)
    dotnet build src/PersonaEngine/PersonaEngine.sln -c Debug

    # For Release build (output typically in src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/)
    dotnet build src/PersonaEngine/PersonaEngine.sln -c Release
    ```
5.  **Place Models & Resources:**
    *   Navigate to the build output directory (e.g., `src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/`).
    *   Create the `Resources` directory structure if it doesn't exist (`Resources/Live2D/Avatars`, `Resources/Models/TTS`, etc.).
    *   Download the required Live2D, Whisper, TTS, ONNX VAD, and potentially RVC models (see **Prerequisites**).
    *   Place them into the correct subdirectories within the `Resources` folder you just created/found.
6.  **Configure `appsettings.json`:**
    *   Open `appsettings.json` located in the build output directory (e.g., `src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/appsettings.json`).
    *   Configure it following the same steps as in **Method 1, Step 4**.
7.  **Run the Application:**
    ```bash
    # Navigate to the App's build output directory
    cd src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/
    # Run the application
    dotnet PersonaEngine.App.dll
    ```
    *(Or run the executable directly, e.g., `PersonaEngine.App.exe` on Windows)*

## 🔧 Configuration (`appsettings.json`)

The `appsettings.json` file controls most aspects of the engine. Refer to the file itself for detailed comments on each setting:

*   `Window`: Dimensions, title, fullscreen.
*   `Llm`: API keys, models, endpoints for text/vision.
*   `Tts`: Model/resource paths, voice settings (default voice, speed, RVC), Whisper model path.
*   `Subtitle`: Font, size, colors, margins, animation, layout.
*   `Live2D`: Avatar resource path, model name, render dimensions.
*   `SpoutConfigs`: Spout output names and resolutions.
*   `Vision`: Screen capture settings.
*   `RouletteWheel`: Interactive wheel settings.
*   `Audio`: Input/Output device selection, VAD settings.
*   `Profanity`: Filter settings.

## ▶️ Usage

1.  Ensure all prerequisites are met and `appsettings.json` is configured correctly.
2.  Run the application using the appropriate method from the "Getting Started" section.
3.  The main window should appear displaying the Live2D avatar.
4.  Speak into your configured microphone. The engine should detect your voice (VAD), transcribe it (Whisper), send it to the LLM, get a response, synthesize it (TTS/RVC), play the audio, and display subtitles.
5.  If Spout outputs are configured, add a Spout2 Capture source in your streaming software (e.g., OBS Studio) and select the configured Spout sender name.

## 💡 Potential Use Cases

*   **VTubing & Live Streaming:** Interactive AI companion/character.
*   **Virtual Assistant:** Desktop character for voice commands/Q&A.
*   **Interactive Kiosks/Installations:** Animated character for public engagement.
*   **Educational Tools:** AI tutor or guide with a visual presence.
*   **Gaming:** AI-powered NPC or companion character.

## 🙌 Contributing

Contributions are welcome! Please follow standard practices:

1.  Fork the repository.
2.  Create a new branch (`git checkout -b feature/your-feature-name`).
3.  Make your changes.
4.  Commit your changes (`git commit -m 'Add some feature'`).
5.  Push to the branch (`git push origin feature/your-feature-name`).
6.  Open a Pull Request.

Please ensure your code adheres to the project's coding style where applicable. Discuss potential changes or features in the Discord or via Issues first!