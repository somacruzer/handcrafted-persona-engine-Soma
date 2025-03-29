<div align="center">
<h1>
Persona Engine <img src="./assets/dance.webp" width="30px">
</h1>
  <a href="https://github.com/fagenorn/handcrafted-persona-engine/releases" target="_blank">
<img alt="GitHub Downloads (all assets, all releases)" src="https://img.shields.io/github/downloads/fagenorn/handcrafted-persona-engine/total">
</a>
  <a href="https://discord.gg/p3CXEyFtrA" target="_blank">
<img alt="Discord" src="https://img.shields.io/discord/1347649495646601419">
</a>
<a href="https://x.com/fagenorn" target="_blank">
<img alt="X (formerly Twitter) Follow" src="https://img.shields.io/twitter/follow/fagenorn">
</a>
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

**Important Note on AI Model and Personality:** Persona Engine is designed to work optimally with a **specially fine-tuned Large Language Model (LLM)**. This model understands the specific way the engine sends information and generates more natural, in-character responses. While you *can* use other standard OpenAI-compatible models by carefully editing the `personality.txt` prompt file, the results may be less ideal or require significant prompt engineering. **For users employing standard models, a `Resources/Prompts/personality_example.txt` file is provided as a basic template. You should use its content as inspiration or copy it to create your *actual* `personality.txt` file.** The fine-tuned model is currently undergoing testing and may be released publicly in the future. **To experience the engine with its intended model or see a demo, please join our Discord community!**

## ✨ Features

*   **Live2D Avatar Integration:** Loads and renders Live2D models. (Potential for lip-sync/animation triggers).
*   **AI-Driven Conversation:** Connects to OpenAI-compatible LLM APIs (local/cloud), uses `personality.txt`. Optimized for a specific fine-tuned model (see [Overview](#overview)).
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
2.  **Processing:** 🧠 LLM (with Personality from `personality.txt` - ideally the fine-tuned model) -> 💬 Response -> (Optional) 🤬 Profanity Check.
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
    *   ✅ **NVIDIA GPU with CUDA (Strongly Recommended):** Essential for good performance! AI tasks (Whisper ASR, TTS, RVC) run much faster on CUDA. See the **[CUDA & cuDNN Installation Guide](#-installing-nvidia-cuda-and-cudnn-for-gpu-acceleration)** below. Make sure you have the latest NVIDIA drivers.
    *   ⚠️ **CPU-Only / Other GPUs:** Performance will likely be very slow or unstable.
*   **Microphone:** Needed for voice input.
*   **Speakers / Headphones:** Needed to hear the output.

### 2. Installing NVIDIA CUDA and cuDNN (for GPU Acceleration)

For optimal performance, especially with Whisper ASR, TTS, and RVC, running on an NVIDIA GPU with CUDA is highly recommended. Follow these steps to set it up on Windows:

1.  **Check GPU Compatibility & Install Driver:**
    *   Ensure your NVIDIA GPU is CUDA-capable (most modern gaming/workstation GPUs are). You can check the [NVIDIA CUDA GPUs list](https://developer.nvidia.com/cuda-gpus).
    *   Download and install the **latest NVIDIA Game Ready or Studio driver** for your GPU from the [NVIDIA Driver Downloads page](https://www.nvidia.com/Download/index.aspx). A clean installation is often recommended.

2.  **Install CUDA Toolkit:**
    *   The CUDA Toolkit provides the development environment and libraries needed.
    *   Go to the [NVIDIA CUDA Toolkit download page](https://developer.nvidia.com/cuda-toolkit-archive) (archive recommended to match specific dependencies if needed, or get the latest from the main [CUDA Toolkit page](https://developer.nvidia.com/cuda-downloads)).
    *   Select your system configuration (Windows, x86_64, your Windows version, `exe (local)` installer type).
    *   Download and run the installer. The **Express (Recommended)** installation option is usually sufficient.
    *   Note the installation path, which is typically `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y` (where `vX.Y` is the version number, e.g., `v12.1`). You will need this path for the next step.

3.  **Install cuDNN Library:**
    *   cuDNN (CUDA Deep Neural Network library) provides highly tuned primitives for deep learning frameworks. Persona Engine leverages this for AI model performance.
    *   Go to the [NVIDIA cuDNN download page](https://developer.nvidia.com/cudnn-downloads). You will need to join the free NVIDIA Developer Program to download the files.
    *   **Crucially, download the cuDNN version that matches your installed CUDA Toolkit version.** The download page will list cuDNN versions compatible with specific CUDA versions (e.g., "Download cuDNN vA.B.C for CUDA X.Y").
    *   Select the "Local Installer for Windows (Zip)" or similar archive file for your matching CUDA version.
    *   **Extract the downloaded cuDNN zip file** to a temporary location (e.g., your Downloads folder). Inside, you will typically find folders named `bin`, `include`, and `lib`.
    *   **Copy the cuDNN files into your CUDA Toolkit installation directory:**
        *   Open the extracted cuDNN folder.
        *   Navigate into the `bin` subfolder. **Copy** all the files inside it (e.g., `cudnn*.dll`).
        *   Navigate to your CUDA Toolkit installation's `bin` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\bin`). **Paste** the copied files here.
        *   Go back to the extracted cuDNN folder.
        *   Navigate into the `include` subfolder. **Copy** the file(s) inside (e.g., `cudnn*.h`).
        *   Navigate to your CUDA Toolkit installation's `include` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\include`). **Paste** the copied file(s) here.
        *   Go back to the extracted cuDNN folder.
        *   Navigate into the `lib` subfolder, then into the `x64` subfolder inside `lib`. **Copy** the file(s) inside (e.g., `cudnn*.lib`).
        *   Navigate to your CUDA Toolkit installation's `lib\x64` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\lib\x64`). **Paste** the copied file(s) here.
        *   *(Replace `vX.Y` in the paths above with the actual version number of your installed CUDA Toolkit).*
    *   Essentially, you are merging the contents of the cuDNN `bin`, `include`, and `lib\x64` folders into the corresponding folders within your main CUDA Toolkit installation directory.

4.  **Add cuDNN to System Path (Important!):**
    *   While copying the files often works, adding the CUDA paths (which now include cuDNN) to your system's Environment Variables PATH ensures applications can find them reliably.
    *   Search for "Environment Variables" in the Windows search bar and select "Edit the system environment variables".
    *   Click the "Environment Variables..." button.
    *   Under "System variables", find the `Path` variable and click "Edit...".
    *   Click "New" and add the following paths (adjusting `vX.Y` to your CUDA version):
        *   `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\bin`
        *   `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\libnvvp` (Often included, good to have)
    *   Click OK on all windows to save the changes.
    *   **Restart your computer** for the PATH changes to take full effect.

5.  **Verification (Optional):**
    *   After restarting, open Command Prompt (`cmd`) and run `nvidia-smi`. This command should execute and display information about your NVIDIA GPU and the CUDA version detected by the driver. If this works, your driver and basic CUDA installation are likely correct. Persona Engine will attempt to use CUDA/cuDNN when it starts; check its console output for related messages.

With CUDA and cuDNN correctly installed, Persona Engine should be able to utilize your NVIDIA GPU for significantly faster AI processing.

### 3. Software to Install (Besides CUDA/Drivers)

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

### 4. ❗ Essential Models & Resources (Download Separately - Whisper Only!) ❗

The pre-built releases include almost everything you need, including TTS resources, VAD model, a demo Live2D model, and personality templates to get you started quickly.

However, the **Whisper ASR models are large and must be downloaded separately.**

*   **Whisper ASR Models (Mandatory Download):**
    *   **What:** The AI models that convert your speech into text. Needs to be in **GGUF format**. You need **both** of the following files:
        *   `ggml-tiny.en.bin` (Faster, used for quick checks or lower resource systems)
        *   `ggml-large-v3-turbo.bin` (More accurate, recommended for general use if your system can handle it)
    *   **Where to find:** Download both `.bin` files directly from the releases page:
        **[➡️ Download Whisper Models Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
    *   **Where it goes:** Place **both** downloaded `.bin` files directly into the `Resources/Models/` folder *after* you extract the main Persona Engine `.zip`.
*   **Your Live2D Avatar Model (Included Demo / Replaceable):**
    *   **What:** The visual character files (`.model3.json`, textures, motions, physics, etc.).
    *   **Included:** Pre-built releases typically include a demo avatar (e.g., "Haru") located in `Resources/Live2D/Avatars/Haru/`. The default `appsettings.json` is usually configured to use this demo model.
    *   **Replacing:** To use your own avatar, create a new subfolder inside `Resources/Live2D/Avatars/` (e.g., `MyChar`) and place your model files there. Then, update the `Live2D.ModelName` setting in `appsettings.json` to match your folder name (e.g., `"ModelName": "MyChar"`).
*   **Personality Prompts (Included - `personality.txt` and `personality_example.txt`):**
    *   **What:** Text files defining the character's behavior for the LLM.
    *   **`personality.txt`:** This is the **active** file the engine reads. It is initially configured for the fine-tuned model (see [Overview](#overview)).
    *   **`personality_example.txt`:** This is a **template/starting point** provided for users using **standard** OpenAI-compatible LLMs. You'll likely need to modify or replace the contents of `personality.txt` using this example as a base if you aren't using the fine-tuned model.
    *   **Where it is:** Located in `Resources/Prompts/`.
*   **TTS Resources (Included):**
    *   **What:** Files needed for speech generation (voice models, phonemizers, sentence splitters).
    *   **Where it is:** Typically located within `Resources/Models/kokoro/`. You generally don't need to touch this.
*   **VAD Model (Included):**
    *   **What:** The voice activity detection model (`silero_vad.onnx`).
    *   **Where it is:** Located in `Resources/Models/`.

### 5. Optional: RVC Models (for Voice Cloning)

*   **What:** If you want to use Real-time Voice Cloning (RVC) to make the TTS output sound like a specific target voice, you need a trained RVC model exported to the **ONNX format**. This usually involves a `.onnx` file containing the voice model itself.
*   **Note on `.pth` files:** Standard RVC training often produces `.pth` files. These **must be converted to ONNX** to be used with Persona Engine. If you need help with conversion, please **join our Discord**!
*   **Where it goes:** Place the `.onnx` file inside the `Resources/Models/rvc/voice/` folder.

### 6. LLM Access (The "Brain")

*   **What:** You need access to a Large Language Model (LLM) API that can process chat-like requests. This involves:
    *   **API Endpoint URL:** The web address of the LLM service (e.g., `http://localhost:11434/v1` for a local Ollama+LiteLLM setup, or a cloud provider's URL).
    *   **(Optional) API Key:** A secret password/token required by some services (like OpenAI, Groq, Anthropic).
    *   **Model Name:** The specific name of the model you want to use (e.g., `gpt-4o`, `llama3`, `your-fine-tuned-model`).
*   **Options:**
    *   **Local:** Run an LLM on your own PC (using tools like Ollama, LM Studio, llama.cpp, often with a proxy like LiteLLM to provide an OpenAI-compatible endpoint). Requires a powerful PC, especially GPU memory (VRAM).
    *   **Cloud:** Use a hosted service (OpenAI, Groq, Anthropic, Together AI, etc.). Often requires registration, API keys, and may incur costs based on usage.
*   **Important Reminder:** The default `personality.txt` file is designed for a **specific fine-tuned model** (see [Overview](#overview)). Using standard models will likely require significant adjustments to `personality.txt` to get good, in-character results. **A `Resources/Prompts/personality_example.txt` file is included to provide a basic structure and starting point if you need to write your `personality.txt` for a standard model.** Join the Discord for access/info on the fine-tuned model. You can edit the personality prompt in `Resources/Prompts/personality.txt`.

### 7. Spout Receiver (To See Your Avatar)

*   **What:** Persona Engine **does not display the avatar in its own window**. Instead, it sends the visual output via **Spout**. You need another application capable of receiving a Spout stream to see your character.
*   **Recommendation:** **OBS Studio** is commonly used for streaming and works well.
*   **Required Plugin:** You'll need the **Spout2 Plugin for OBS**: [https://github.com/Off-World-Live/obs-spout2-plugin/releases](https://github.com/Off-World-Live/obs-spout2-plugin/releases)
*   **How:** Download and install the plugin for OBS. You'll configure this after running Persona Engine (see [Getting Started](#-getting-started)).

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

*   Make sure you have installed the **NVIDIA Driver, CUDA Toolkit, and cuDNN** following the guide in the Prerequisites section if you plan to use GPU acceleration.
*   Make sure you have installed the **.NET 9.0 Runtime** (see [Prerequisites](#3-software-to-install-besides-cudadrivers)).
*   Make sure you have installed **`espeak-ng`** and added it to your system PATH (see [Prerequisites](#3-software-to-install-besides-cudadrivers)). The engine needs this for TTS.

**Step 3: Download and Place Required Whisper Models**

*   Go to the Whisper Model download link provided in the **Prerequisites** section: **[➡️ Download Whisper Models Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
*   Download **both** `ggml-tiny.en.bin` and `ggml-large-v3-turbo.bin`.
*   Place these two `.bin` files directly into the `Resources/Models/` folder inside your extracted Persona Engine directory.

**Step 4: Quick Configuration (`appsettings.json` and `personality.txt`)**

*   Inside the extracted Persona Engine folder, find `appsettings.json`.
*   Open it with a text editor (Notepad, Notepad++, VS Code).
*   **Crucial Settings to Start:**
    *   `Llm` section:
        *   Set `TextEndpoint`: The URL of your LLM service (e.g., `http://localhost:11434/v1` for local).
        *   Set `TextModel`: The name of the LLM you want to use (e.g., `llama3`, `gpt-4o`).
        *   Set `TextApiKey`: Enter your API key *only if* your LLM service requires one (leave empty `""` otherwise).
    *   `Live2D` section:
        *   Check the `ModelName` value. By default, it likely points to the included demo model (e.g., `"Haru"`). If you want to use your *own* model later, you'll need to put its files in a folder under `Resources/Live2D/Avatars/` and change this setting to match your folder name.
*   Save the `appsettings.json` file.
*   **(Optional but Recommended) Configure Personality (`personality.txt`):**
    *   Navigate to `Resources/Prompts/`. Here you will find `personality.txt` (the active file) and `personality_example.txt` (a template).
    *   **Important:** If you are **not** using the specially fine-tuned LLM (which is not provided in public releases), the default content of `personality.txt` might not work well with standard models.
    *   In this case, open and examine `personality_example.txt` with a text editor. This file provides a basic template suitable as a starting point for standard OpenAI-compatible models.
    *   **You will likely need to copy the contents of `personality_example.txt` into `personality.txt` (overwriting the original content) and *then* modify `personality.txt`** to define your specific character's personality, background, rules, and how it should respond.
    *   Remember: Significant prompt engineering in `personality.txt` might be needed to get good results with standard LLMs (see [Overview](#overview)).

**Step 5: Run Persona Engine!**

*   Double-click the `PersonaEngine.exe` file in the main folder.
*   This will open the main **Configuration and Control UI**, *not* the avatar itself. The console window behind it shows detailed logs. Check the console for messages about CUDA/GPU initialization if you set it up.
*   If the LLM is reachable and the resources (especially Whisper models) are in the right place, the engine should initialize and start listening for your voice input. Try speaking into your microphone.

**Step 6: View the Avatar (via Spout)**

*   Since the avatar isn't shown in the main UI, you need a Spout receiver.
*   **Install OBS Studio** if you don't have it.
*   **Install the Spout2 Plugin for OBS** (download link in Prerequisites).
*   Open OBS Studio.
*   In the "Sources" panel, click the "+" button and add a **"Spout2 Capture"** source.
*   In the source properties, click the "Spout Sender" dropdown. If Persona Engine is running correctly, you should see a sender name listed (e.g., "PersonaEngineOutput" - this name can be configured in `appsettings.json` under `SpoutConfigs`). Select it.
*   Your Live2D avatar should now appear in the OBS preview window!

**Step 7: Further Configuration (Optional)**

*   You can further customize behavior by editing `appsettings.json` (audio devices, TTS voice/speed, subtitle appearance, RVC settings, etc.) or using the built-in UI elements while the engine is running.

---

### Method 2: Building from Source (Advanced / Developers / Other Platforms)

*(This section requires updates to reflect the new structure)*

1.  **Prerequisites:**
    *   Install **Git**.
    *   Install the **.NET 9.0 SDK** (Software Development Kit) from [Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0).
    *   Install **`espeak-ng`** and add to PATH (see [Prerequisites](#3-software-to-install-besides-cudadrivers)).
    *   **Install NVIDIA Driver, CUDA Toolkit, and cuDNN** following the guide above if targeting GPU acceleration.
    *   For non-Windows, find equivalent dependencies (PortAudio, Spout alternative like Syphon on macOS, check Whisper.net/Live2D Cubism Core requirements). This is **unsupported** and requires significant effort.
2.  **Clone the Repository:**
    ```bash
    git clone https://github.com/fagenorn/handcrafted-persona-engine.git
    cd handcrafted-persona-engine
    ```
3.  **Restore Dependencies:**
    ```bash
    dotnet restore
    ```
4.  **Build the Project:**
    ```bash
    # For a standard Release build:
    dotnet publish PersonaEngine.App -c Release -o ./publish --self-contained false
    # Add `-r win-x64` (or your target runtime) if not targeting default platform
    ```
5.  **Navigate to Output:**
    *   The built application will be in the `./publish` directory (or the output directory you specified).
6.  **Place Models & Resources:**
    *   Navigate to the build output directory (e.g., `./publish`).
    *   Create the necessary `Resources` directory structure (`Resources/Models`, `Resources/Live2D/Avatars`, `Resources/Prompts`).
    *   Download and place the required **Whisper GGUF models** (`ggml-tiny.en.bin`, `ggml-large-v3-turbo.bin`) into `Resources/Models/`.
    *   Place your Live2D model files into `Resources/Live2D/Avatars/YourModelName/`.
    *   Ensure included resources (TTS, VAD) are copied correctly during the build or place them manually if needed (check the `.csproj` files for included content). Copy `Resources/Models/kokoro` and `Resources/Models/silero_vad.onnx` from the source repo to your output `Resources/Models` folder.
    *   Copy `Resources/Prompts/personality.txt` and `Resources/Prompts/personality_example.txt` from the source repo to your output `Resources/Prompts/` directory.
    *   Place optional RVC ONNX models into `Resources/Models/rvc/voice/`.
7.  **Configure `appsettings.json` and `personality.txt`:**
    *   Copy `appsettings.json` from the `PersonaEngine.App` project directory to the build output directory (`./publish`).
    *   Configure it following the same steps as in **Method 1, Step 4** (for essential LLM/Live2D settings).
    *   **Crucially:** Edit `personality.txt` in your output `Resources/Prompts/` directory. If using a standard LLM, **copy the contents from `personality_example.txt` into `personality.txt`** and then modify `personality.txt` extensively for your desired character and model.
    *   Set paths for audio, TTS/RVC if needed, especially `Tts.EspeakPath` if `espeak-ng` is not in the system PATH.
8.  **Run the Application:**
    *   Open a terminal or command prompt in the build output directory (`./publish`).
    *   Run the application: `dotnet PersonaEngine.App.dll`
    *   Check the console output for initialization messages, including CUDA/GPU status.
    *   Remember to set up a Spout receiver (like OBS) to view the avatar output.

---

## 🔧 Configuration (`appsettings.json`)

The `appsettings.json` file controls most aspects of the engine. Open it in a text editor to adjust settings. Refer to the comments within the file (if available in the release) or the structure itself for guidance:

*   `Window`: Dimensions, title, fullscreen.
*   `Llm`: API keys, models, endpoints for text/vision. **Remember:** Adjust `personality.txt` prompts if using standard models (see `personality_example.txt` for a starting template).
*   `Tts`: Paths for Whisper model, TTS models, Espeak library (`EspeakPath` if not in PATH). Voice settings (default voice `Voice`, speed). RVC settings.
*   `Subtitle`: Font, size, colors, margins, animation, layout.
*   `Live2D`: Path to avatars directory, `ModelName` (must match your avatar's folder name).
*   `SpoutConfigs`: Spout output names and resolutions for streaming software like OBS.
*   `Vision`: Screen capture settings (experimental).
*   `RouletteWheel`: Interactive wheel settings (experimental).

## ▶️ Usage

1.  Ensure all **Prerequisites** are met (downloaded Whisper models, installed `.NET`, `espeak-ng`, CUDA/cuDNN if using GPU, Spout receiver ready).
2.  Make sure `appsettings.json` is configured correctly with your LLM details and that resource files (Whisper models, Avatar) are placed correctly (see [Getting Started](#-getting-started)).
3.  Ensure `Resources/Prompts/personality.txt` is correctly set up for your chosen LLM (using `personality_example.txt` as a base if needed).
4.  Run the application using the appropriate method (`PersonaEngine.exe` for pre-built release, `dotnet PersonaEngine.App.dll` for source build).
5.  The main **Configuration and Control UI** window will appear. The engine starts processing in the background (check the console window for logs, including CUDA initialization). **The avatar is NOT displayed in this window.**
6.  **Set up your Spout receiver application** (e.g., OBS Studio with the Spout2 Capture source pointing to the Persona Engine sender) to view the Live2D avatar output.
7.  Speak into your configured microphone. The engine should:
    *   Detect when you start and stop speaking (VAD).
    *   Transcribe your speech to text (Whisper - using GPU).
    *   Send the text (and personality context from `personality.txt`) to the LLM.
    *   Receive a response from the LLM.
    *   Convert the response text to speech (TTS, potentially using RVC if configured - using GPU).
    *   Play the spoken audio.
    *   Display subtitles (on the Spout output).
    *   Animate the avatar (basic mouth movement planned, visible via Spout).
8.  Use the UI elements to monitor status or adjust settings on the fly if needed.

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