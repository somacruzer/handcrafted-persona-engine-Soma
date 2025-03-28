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
*   **Real-time Voice Cloning (RVC):** Integrates RVC ONNX models for real-time voice transformation.
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
Need help getting started? Have questions or ideas? Want to see a live demo, test the special fine-tuned model, or interact directly with a Persona Engine instance? Having trouble converting RVC models? Join the Discord server!
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
3.  **Output:** 🔊 TTS -> 🎤 RVC (Optional / ONNX) -> 🎭 Live2D Animation -> 📜 Subtitles -> 🎶 Audio Playback -> 📺 Spout Visuals.

<div align="center">
<br/>
<img
  src="assets/diagram.png"
  alt="Persona Engine Showcase"
  width="600"
>
<br/>
</div>

## 📋 Prerequisites: What You Need Before You Start

Getting Persona Engine running involves a few steps. Please make sure you have the following ready:

### 1. System Requirements

*   **Operating System:**
    *   ✅ **Windows (Recommended):** The engine is primarily developed and tested here. Pre-built releases are Windows-only.
    *   ⚠️ **Linux / macOS:** Possible *only* by building from source. Requires advanced setup for dependencies (CUDA, Spout alternatives, Audio libraries) and is **not officially supported**.
*   **Graphics Card (GPU):**
    *   ✅ **NVIDIA GPU with CUDA (Strongly Recommended):** Essential for good performance! AI tasks (Whisper ASR, TTS, RVC) run much faster on CUDA. Make sure you have the latest NVIDIA drivers.
    *   ⚠️ **CPU-Only / Other GPUs:** Performance will likely be very slow or unstable.
*   **Microphone:** Needed for voice input.
*   **Speakers / Headphones:** Needed to hear the output.

### 2. Software to Install

You need to install these two pieces of software on your system *before* running Persona Engine:

*   **[.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0):**
    *   **What it is:** The core framework the application runs on.
    *   **How to get it:** Download and install it from the official Microsoft website. The installer for pre-built Persona Engine releases *might* prompt you if it's missing, but it's best to install it system-wide beforehand. (It's usually included within the pre-built `.zip` file's `dotnet_runtime` folder for convenience, but installing system-wide is recommended).
*   **[`espeak-ng`](https://github.com/espeak-ng/espeak-ng/releases):**
    *   **What it is:** A text processing tool critical for the Text-to-Speech (TTS) system to understand how to pronounce words (phonemization). **TTS will likely fail without it.**
    *   **How to get it:**
        1.  Go to the `espeak-ng` releases page.
        2.  Download the appropriate installer for your system (e.g., `espeak-ng-*.msi` for Windows).
        3.  **Important:** During installation, ensure you check the option to **"Add espeak-ng to the system PATH"**. This is the easiest way.
        4.  *Alternatively*, if you don't add it to PATH, you *must* manually specify the full path to `espeak-ng.dll` (or equivalent library file for your OS) in the `Tts.EspeakPath` setting within `appsettings.json`.

### 3. ❗ Essential Models & Resources (Download Separately) ❗

These large files are **NOT** included in the main Persona Engine download/repository. **You MUST download them yourself** and place them into the correct folders *after* you extract or build the engine (see "Getting Started"). The engine needs these to function properly (hear you, speak, show your avatar).

*   **Your Live2D Avatar Model:**
    *   **What:** The visual character files (`.model3.json`, textures, motions, physics, etc.).
    *   **Where it goes:** Inside a dedicated subfolder within `Resources/Live2D/Avatars/`. For example, if your character is named "MyChar", put its files in `Resources/Live2D/Avatars/MyChar/`. You'll then set `"ModelName": "MyChar"` in `appsettings.json`.
*   **Whisper ASR Model (for Speech-to-Text):**
    *   **What:** The AI model that converts your speech into text. Needs to be in **GGUF format**.
    *   **Recommendation:** Start with `ggml-large-v3-turbo.bin` (or a smaller/faster variant like `medium` or `base` if needed).
    *   **Where to find:** Search for "Whisper GGUF models" on sites like Hugging Face (e.g., search repositories like `ggerganov/whisper.cpp`).
    *   **Where it goes:** Place the downloaded `.bin` file directly into the `Resources/Models/` folder.
*   **TTS Resources (for Speaking):**
    *   **What:** Files needed for the engine to generate speech. This includes:
        *   **Voice Models:** Specific voice data (e.g., in the custom `kokoro` format, or potentially other ONNX-based formats).
        *   **Phonemizer Models:** Files used by `espeak-ng` or similar tools (often included with `espeak-ng` or downloaded separately depending on TTS pipeline specifics).
        *   **Sentence Splitter Models:** Files for breaking text into sentences (e.g., OpenNLP models like `en-sent.bin`).
    *   **Where it goes:** These generally go into `Resources/Models/TTS/` and its subdirectories. Check the default configuration (`Tts.ModelDirectory` in `appsettings.json`) and any documentation specific to the voice models you acquire.
*   **VAD Model (for Voice Activity Detection):**
    *   **What:** A small model to detect when you start and stop speaking (`silero_vad.onnx`).
    *   **Where it goes:** Should be in the `Resources/Models/` folder. This file *might* be included in pre-built releases, but double-check.

### 4. Optional: RVC Models (for Voice Cloning)

*   **What:** If you want to use Real-time Voice Cloning (RVC) to make the TTS output sound like a specific target voice, you need a trained RVC model exported to the **ONNX format**. This usually involves a `.onnx` file containing the voice model itself.
*   **Note on `.pth` files:** Standard RVC training often produces `.pth` files. These **must be converted to ONNX** to be used with Persona Engine. If you need help with conversion, please **join our Discord**!
*   **Where it goes:** Place the `.onnx` file somewhere accessible on your computer. You will then specify the full path to the `.onnx` file in the `Tts.Rvc` section of `appsettings.json`.

### 5. LLM Access (The "Brain")

*   **What:** You need access to a Large Language Model (LLM) API that can process chat-like requests. This involves:
    *   **API Endpoint URL:** The web address of the LLM service (e.g., `http://localhost:11434/v1/chat/completions` for a local Ollama+LiteLLM setup, or a cloud provider's URL).
    *   **(Optional) API Key:** A secret password/token required by some services (like OpenAI, Groq, Anthropic).
    *   **Model Name:** The specific name of the model you want to use (e.g., `gpt-4o`, `llama3`, `your-fine-tuned-model`).
*   **Options:**
    *   **Local:** Run an LLM on your own PC (using tools like Ollama, LM Studio, llama.cpp, often with a proxy like LiteLLM to provide an OpenAI-compatible endpoint). Requires a powerful PC, especially GPU memory (VRAM).
    *   **Cloud:** Use a hosted service (OpenAI, Groq, Anthropic, Together AI, etc.). Often requires registration, API keys, and may incur costs based on usage.
*   **Important Reminder:** The default `personality.txt` file is designed for a **specific fine-tuned model** (see Overview). Using standard models will likely require significant adjustments to `personality.txt` to get good, in-character results. Join the Discord for access/info on the fine-tuned model.

## 🚀 Getting Started

There are two ways to use Persona Engine:

---

### Method 1: Easy Install with Pre-built Release (Recommended for Windows Users)

This is the simplest way to get started if you're on Windows and don't want to deal with code.

**Step 1: Download & Extract Persona Engine**

<div align="center" style="margin: 20px;">
  <a href="https://github.com/fagenorn/handcrafted-persona-engine/releases" target="_blank">
  <img
  src="assets/download.png"
  alt="Persona Engine Showcase"
  width="350"
>
  </a>
  <p><i>(Click the button to go to the Releases page. Download the `.zip` file from the latest release.)</i></p>
</div>

*   Find the downloaded `.zip` file (e.g., `PersonaEngine_vX.Y.Z.zip`).
*   Right-click the file and choose "Extract All..." or use a program like 7-Zip or WinRAR.
*   Choose a location (e.g., `C:\PersonaEngine`). **Avoid** system folders like Program Files.

**Step 2: Install Prerequisites (If you haven't already)**

*   Make sure you have installed the **.NET 9.0 Runtime** (see Prerequisites).
*   Make sure you have installed **`espeak-ng`** and added it to your system PATH (see Prerequisites). The engine needs this for TTS.

**Step 3: Quick Configuration (`appsettings.json`)**

*   Inside the extracted Persona Engine folder, find `appsettings.json`.
*   Open it with a text editor (Notepad, Notepad++, VS Code).
*   **Crucial Settings to Start:**
    *   `Llm` section:
        *   Set `TextEndpoint`: The URL of your LLM service (e.g., `http://localhost:11434/v1` for local).
        *   Set `TextModel`: The name of the LLM you want to use (e.g., `llama3`, `gpt-4o`).
        *   Set `TextApiKey`: Enter your API key *only if* your LLM service requires one (leave empty `""` otherwise).
    *   `Live2D` section:
        *   Set `ModelName`: Change this to **exactly match** the name of your avatar's folder that you will place inside `Resources/Live2D/Avatars/` in the next step. (e.g., if your avatar files are in a folder named "MyChar", set this to `"MyChar"`).
*   Save the `appsettings.json` file.

**Step 4: Add Essential Resources (Models & Avatar)**

*   Now, go into the `Resources` folder within your extracted Persona Engine directory.
*   **Place the files you downloaded separately (see Prerequisites):**
    *   **Live2D Avatar:** Create a folder inside `Resources/Live2D/Avatars/` with the *exact same name* you set for `ModelName` in Step 3. Put all your avatar files (`.model3.json`, textures, etc.) into this folder.
        *   Example: If `ModelName` is `"MyChar"`, put files in `Resources/Live2D/Avatars/MyChar/`.

**Step 5: Run Persona Engine!**

*   Double-click the `PersonaEngine.exe` file in the main folder.
*   You won't be able to see your Live2D model since this is being output to spout.
*   If the LLM is reachable and the resources are in the right place, it should start listening! Try speaking into your microphone.

**Step 6: Further Configuration (Optional but Recommended)**

*   Once it's running, you might want to fine-tune other settings in `appsettings.json`:

---

### Method 2: Building from Source (Advanced / Developers / Other Platforms)

This method is for developers or users wanting to run on potentially unsupported platforms (Linux/macOS) or modify the code. **Note:** Running on non-Windows platforms is untested, requires installing many system libraries manually (CUDA, PortAudio, Spout alternatives, espeak-ng), and may require code changes.

1.  **Install Prerequisites:**
    *   Git: [https://git-scm.com/](https://git-scm.com/)
    *   .NET 9.0 SDK: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
    *   `espeak-ng`: Install globally and ensure it's in the system PATH (see Prerequisites).
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
    *   Download and place all required Live2D, Whisper, TTS, ONNX VAD, and optional RVC ONNX models into the correct subdirectories within `Resources` (see **Prerequisites** and **Method 1, Step 4** for locations).
6.  **Configure `appsettings.json`:**
    *   Copy or create `appsettings.json` in the build output directory.
    *   Configure it following the same steps as in **Method 1, Step 3** (for essential LLM/Live2D settings) and **Step 6** (for other settings like audio, TTS/RVC paths, etc.). Remember to set the RVC path to your `.onnx` file if using it.
7.  **Run the Application:**
    ```bash
    # Navigate to the App's build output directory
    cd src/PersonaEngine/PersonaEngine.App/bin/Release/net9.0/
    # Run the application
    dotnet PersonaEngine.App.dll
    ```
    *(Or run the executable directly, e.g., `PersonaEngine.exe` on Windows)*

---

## 🔧 Configuration (`appsettings.json`)

The `appsettings.json` file controls most aspects of the engine. Open it in a text editor to adjust settings. Refer to the comments within the file (if available in the release) or the structure itself for guidance:

*   `Window`: Dimensions, title, fullscreen.
*   `Llm`: API keys, models, endpoints for text/vision. **Remember:** The default `personality.txt` is optimized for a specific fine-tuned model (see Overview). Adjust prompts if using other models.
*   `Tts`: Paths for Whisper model, TTS models, Espeak library (`EspeakPath` if not in PATH). Voice settings (default voice `Voice`, speed). RVC settings.
*   `Subtitle`: Font, size, colors, margins, animation, layout.
*   `Live2D`: Path to avatars directory, `ModelName` (must match your avatar's folder name).
*   `SpoutConfigs`: Spout output names and resolutions for streaming software like OBS.
*   `Vision`: Screen capture settings (experimental).
*   `RouletteWheel`: Interactive wheel settings (experimental).

## ▶️ Usage

1.  Ensure all **Prerequisites** are met (especially downloaded models, installed `.NET` and `espeak-ng`).
2.  Make sure `appsettings.json` is configured correctly with your LLM details, Live2D `ModelName`, and that resource files are placed correctly (see "Getting Started").
3.  Run the application using the appropriate method (`PersonaEngine.exe` for pre-built release, `dotnet PersonaEngine.App.dll` for source build).
4.  The main window should appear displaying the Live2D avatar.
5.  Speak into your configured microphone. The engine should:
    *   Detect when you start and stop speaking (VAD).
    *   Transcribe your speech to text (Whisper).
    *   Send the text (and personality context) to the LLM.
    *   Receive a response from the LLM.
    *   Convert the response text to speech (TTS, potentially using RVC if configured).
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