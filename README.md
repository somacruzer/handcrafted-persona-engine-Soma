<div align="center">
<h1>
Persona Engine <img src="./assets/dance.webp" width="30px">
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

**Important Note on AI Model:** Persona Engine is designed to work optimally with a **specially fine-tuned Large Language Model (LLM)**. This model understands the specific way the engine sends information and generates more natural, in-character responses. While you *can* use other standard OpenAI-compatible models by carefully editing the `personality.txt` prompt file, the results may be less ideal or require significant prompt engineering. The fine-tuned model is currently undergoing testing and may be released publicly in the future. **To experience the engine with its intended model or see a demo, please join our Discord community!**

## ✨ Features

*   **Live2D Avatar Integration:** Loads and renders Live2D models. (Potential for lip-sync/animation triggers).
*   **AI-Driven Conversation:** Connects to OpenAI-compatible LLM APIs (local/cloud), uses `personality.txt`. Optimized for a specific fine-tuned model (see Overview).
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
Need help getting started? Have questions or ideas? Want to see a live demo, test the special fine-tuned model, or interact directly with a Persona Engine instance? Join the Discord server!
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
2.  **Processing:** 🧠 LLM (with Personality - ideally the fine-tuned model) -> 💬 Response -> (Optional) 🤬 Profanity Check.
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

Before you start, make sure your system meets these requirements:

*   **Operating System:** Currently, the project is primarily developed and tested on **Windows**. Pre-built releases are Windows-only. Running on Linux/macOS is possible by building from source but requires advanced setup for native dependencies (CUDA, Spout, Audio, etc.) and is **not officially supported**.
*   **GPU (NVIDIA CUDA Strongly Recommended):** A modern NVIDIA GPU with CUDA support is **highly recommended** for good performance. AI tasks like Whisper (speech-to-text), ONNX Runtime (TTS), and RVC (voice cloning) run much faster on a GPU.
    *   Make sure you have the latest NVIDIA drivers installed.
    *   Performance on CPU-only or non-NVIDIA GPUs may be very slow or unstable.
*   **.NET 9.0 Runtime:** The program needs this to run. The installer for pre-built releases *might* prompt you to install it if missing, or you can get it from Microsoft's website. (Included in pre-built .zip files usually).
*   **Models & Resources (Essential - Download Separately):**
    *   **❗ IMPORTANT:** These large files are **NOT** included in the main download. You **MUST** get them yourself.
    *   **Live2D Avatar Model:** Your own character files (`.model3.json`, textures, motions, etc.). You'll place these inside the `Resources/Live2D/Avatars/` folder later.
    *   **Whisper Model (for Speech-to-Text):** Download a GGUF format model. A good starting point is `ggml-large-v3-turbo.bin`. You can find these on sites like Hugging Face. Place the downloaded `.bin` file in the `Resources/Models/` folder.
    *   **TTS Resources (for Speaking):**
        *   TTS voice models (e.g., `kokoro` format) go into the `Resources/Models/TTS/` folder (or wherever `ModelDirectory` points in settings).
        *   **`espeak-ng`:** This is needed for text processing *before* speech synthesis. Download it from the [espeak-ng releases page](https://github.com/espeak-ng/espeak-ng/releases) and install it. Make sure it's added to your system's PATH during installation, or you'll need to specify the path in `appsettings.json`.
        *   Other required TTS files (like OpenNLP sentence models, phonemizer models) need to be in `Resources/Models/TTS/` and its subfolders. Check the default structure and configuration.
    *   **Other ONNX Models:** Files like `silero_vad.onnx` (for voice activity detection) should be in `Resources/Models/`. These might be included in releases, but double-check.
    *   **(Optional) RVC Models (for Voice Cloning):** If using RVC, you need `.pth` and `.index` files for your voice model. Place them somewhere accessible and set the paths in `appsettings.json`.
*   **LLM Access (for Conversation Brain):**
    *   You need access to an AI that understands chat requests. This requires an API endpoint (a URL address) and sometimes an API Key (like a password).
    *   This can be a **local server** running on your own PC (like `llama.cpp`, `Ollama` with `LiteLLM`) or a **cloud service** (like OpenAI, Groq, Anthropic - requires an account and potentially payment).
    *   *Note:* Remember, the engine's default prompts (`personality.txt`) are best suited for the specific fine-tuned model mentioned in the Overview. Using other models might require changing the `personality.txt` file significantly.

## 🚀 Getting Started

There are two ways to use Persona Engine:

---

### Method 1: Easy Install with Pre-built Release (Recommended for Windows Users)

This is the simplest way to get started if you're on Windows and don't want to deal with code.

**Step 1: Download Persona Engine**

<div align="center" style="margin: 20px;">
  <a href="https://github.com/fagenorn/handcrafted-persona-engine/releases" target="_blank" style="display: inline-block; padding: 15px 30px; background-color: #4CAF50; color: white; text-align: center; text-decoration: none; font-size: 18px; font-weight: bold; border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.2); transition: background-color 0.3s;">
    ⬇️ Download Latest Release (.zip) ⬇️
  </a>
  <p style="margin-top: 10px;">(Click the button to go to the Releases page. Download the `.zip` file from the latest release.)</p>
</div>

**Step 2: Extract the Files**

*   Find the downloaded `.zip` file (e.g., `PersonaEngine_vX.Y.Z.zip`) in your Downloads folder.
*   Right-click the file and choose "Extract All..." or use a program like 7-Zip or WinRAR.
*   Choose a location to extract the files to (e.g., create a new folder like `C:\PersonaEngine`). **Avoid** system folders like Program Files.

**Step 3: Add Your Models & Resources**

*   Go into the folder where you extracted Persona Engine.
*   You'll see a `Resources` folder. Inside it, you need to place the **essential models and avatar files** you downloaded separately (see **Prerequisites** section above):
    *   Put your **Live2D Avatar** folder(s) into `Resources/Live2D/Avatars/`.
    *   Put your downloaded **Whisper Model** (`.bin` file) into `Resources/Models/`.
    *   Put your **TTS Voice Models** and related files into `Resources/Models/TTS/`.
    *   Make sure other required models like `silero_vad.onnx` are in `Resources/Models/`.
    *   If using **RVC**, have your `.pth` and `.index` files ready somewhere (you'll point to them in the config).
*   **Install `espeak-ng`** if you haven't already (see Prerequisites).

**Step 4: Configure the Engine (`appsettings.json`)**

*   Inside the extracted Persona Engine folder, find the file named `appsettings.json`.
*   Open it with a text editor (like Notepad, Notepad++, VS Code).
*   **Carefully update these important settings:**
    *   `Llm`: Set `TextEndpoint` (the URL of your LLM), `TextModel` (the model name your LLM uses), and `TextApiKey` (if your LLM requires one).
    *   `Llm`: Do the same for `VisionEndpoint`, `VisionModel`, `VisionApiKey` *only if* you plan to use the experimental screen awareness feature.
    *   `Live2D`: Change `ModelName` to exactly match the name of your avatar's folder inside `Resources/Live2D/Avatars/`.
    *   `Tts`: If you installed `espeak-ng` somewhere unusual, set the correct `EspeakPath`.
    *   `Tts`: Configure your desired `Voice` (matching your TTS model folder) and `Rvc` options (paths to `.pth` and `.index` files if using RVC).
    *   Review other settings like `SpoutConfigs` (for OBS), `Subtitle`, `Audio` (microphone/speaker selection), etc., and adjust if needed. Save the file when done.

**Step 5: Run Persona Engine!**

*   Double-click the `PersonaEngine.exe` file located in the main folder you extracted.
*   The application window should appear with your Live2D avatar.
*   If everything is configured correctly, it should start listening for your voice!

---

### Method 2: Building from Source (Advanced / Developers / Other Platforms)

This method is for developers or users wanting to run on potentially unsupported platforms (Linux/macOS) or modify the code. **Note:** Running on non-Windows platforms is untested, requires installing many system libraries manually (CUDA, PortAudio, Spout alternatives, espeak-ng), and may require code changes.

1.  **Install Prerequisites:**
    *   Git: [https://git-scm.com/](https://git-scm.com/)
    *   .NET 9.0 SDK: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
    *   (Windows) Ensure CUDA toolkit/drivers are installed if using GPU features.
    *   (Linux/macOS) Manually install equivalent native libraries (CUDA, PortAudio, espeak-ng, etc.) and ensure they are accessible to .NET. This can be complex. Spout may require alternatives like NDI or Syphon depending on your needs and effort.
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
    *   Create the `Resources` directory structure (`Resources/Live2D/Avatars`, `Resources/Models/TTS`, etc.).
    *   Download and place all required Live2D, Whisper, TTS, ONNX VAD, and optional RVC models into the correct subdirectories within `Resources` (see **Prerequisites**).
    *   Install `espeak-ng` globally or ensure it's accessible.
6.  **Configure `appsettings.json`:**
    *   Copy or create `appsettings.json` in the build output directory.
    *   Configure it following the same steps as in **Method 1, Step 4**.
7.  **Run the Application:**
    ```bash
    # Navigate to the App's build output directory
    cd src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/
    # Run the application
    dotnet PersonaEngine.App.dll
    ```
    *(Or run the executable directly, e.g., `PersonaEngine.App.exe` on Windows)*

---

## 🔧 Configuration (`appsettings.json`)

The `appsettings.json` file controls most aspects of the engine. Open it in a text editor to adjust settings. Refer to the comments within the file (if available in the release) or the structure itself for guidance:

*   `Window`: Dimensions, title, fullscreen.
*   `Llm`: API keys, models, endpoints for text/vision. **Remember:** The default `personality.txt` is optimized for a specific fine-tuned model (see Overview). Adjust prompts if using other models.
*   `Tts`: Model/resource paths (Whisper model, TTS models, Espeak path), voice settings (default voice, speed, RVC paths/settings).
*   `Subtitle`: Font, size, colors, margins, animation, layout.
*   `Live2D`: Avatar resource path, `ModelName` (must match your avatar's folder name).
*   `SpoutConfigs`: Spout output names and resolutions for streaming software like OBS.
*   `Vision`: Screen capture settings (experimental).
*   `RouletteWheel`: Interactive wheel settings (experimental).
*   `Audio`: Input/Output device selection (use device names or IDs), VAD sensitivity settings.
*   `Profanity`: Filter settings.

## ▶️ Usage

1.  Ensure all **Prerequisites** are met (especially downloaded models and installed `espeak-ng`).
2.  Make sure `appsettings.json` is configured correctly with your API keys, model paths, avatar name, etc.
3.  Run the application using the appropriate method from the "Getting Started" section (`PersonaEngine.exe` for pre-built release).
4.  The main window should appear displaying the Live2D avatar.
5.  Speak into your configured microphone. The engine should:
    *   Detect when you start and stop speaking (VAD).
    *   Transcribe your speech to text (Whisper).
    *   Send the text (and personality context) to the LLM.
    *   Receive a response from the LLM.
    *   Convert the response text to speech (TTS/RVC).
    *   Play the spoken audio.
    *   Display subtitles.
    *   Animate the avatar (basic mouth movement planned).
6.  **Streaming:** If Spout outputs are configured in `appsettings.json` (e.g., `SpoutConfigs` has an entry), add a "Spout2 Capture" source in OBS Studio (you might need the Spout plugin for OBS) and select the sender name you configured (e.g., "PersonaEngineOutput").

## 💡 Potential Use Cases

*   **VTubing & Live Streaming:** Create an interactive AI co-host or character that responds to chat or voice.
*   **Virtual Assistant:** A desktop character providing information or performing tasks via voice commands.
*   **Interactive Kiosks/Installations:** An engaging animated character for museums, events, or information booths.
*   **Educational Tools:** An AI tutor, language practice partner, or guide with a friendly visual presence.
*   **Gaming:** Powering NPCs or companion characters with more dynamic conversational abilities.

## 🙌 Contributing

Contributions are welcome! Please follow standard practices:

1.  Fork the repository.
2.  Create a new branch (`git checkout -b feature/your-feature-name`).
3.  Make your changes.
4.  Commit your changes (`git commit -m 'Add some feature'`).
5.  Push to the branch (`git push origin feature/your-feature-name`).
6.  Open a Pull Request.

Please ensure your code adheres to the project's coding style where applicable. Discuss potential changes or features in the Discord or via GitHub Issues first!

---