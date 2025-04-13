<div align="center">
<h1>
Persona Engine <img src="./assets/dance.webp" width="30px" alt="Dancing Mascot">
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
An AI-powered interactive avatar engine using Live2D, Large Language Models (LLMs), Automatic Speech Recognition (ASR), Text-to-Speech (TTS), and optional Real-time Voice Cloning (RVC). Designed primarily for VTubing, streaming, and virtual assistant applications. Let's bring your character to life! ✨
</p>

<img src="assets/header.png" alt="Persona Engine" width="650">

<h2>💖 See it in Action! 💖</h2>
<p>Watch Persona Engine work its magic:</p>
<a href="https://www.youtube.com/watch?v=4V2DgI7OtHE" target="_blank">
  <img src="assets/demo_1.png" alt="Persona Engine Demo Video" width="600">
</a>
</div>


## <a id="toc"></a>📜 Table of Contents

* [🌸 Overview: What's Inside?](#overview)
* [✨ Features Galore!](#features)
* [💬 Join Our Community!](#community)
* [⚙️ Architecture / How it Works](#architecture)
* [📋 Prerequisites: Let's Get Ready!](#prerequisites)
    * [System Requirements](#prereq-system)
    * [Installing NVIDIA CUDA and cuDNN (REQUIRED!)](#prereq-cuda)
    * [Software to Install](#prereq-software)
    * [Essential Models & Resources (Downloads!)](#prereq-models)
    * [Optional: RVC Models](#prereq-rvc)
    * [LLM Access](#prereq-llm)
    * [Spout Receiver](#prereq-spout)
* [🚀 Getting Started: Let's Go!](#getting-started)
    * [Easy Install (Recommended for Windows)](#install-release)
    * [Building from Source (Advanced)](#install-source)
* [🔧 Configuration (`appsettings.json`)](#configuration)
* [▶️ Usage: Showtime!](#usage)
* [🛠️ Troubleshooting](#troubleshooting)
* [💡 Potential Use Cases](#use-cases)
* [🙌 Contributing](#contributing)


## <a id="overview"></a>🌸 Overview: What's Inside?

Persona Engine listens to your voice 🎤, thinks using powerful AI language models 🧠 (guided by a personality you define!), speaks back with a synthesized voice 🔊 (which can optionally be cloned using RVC!), and animates a Live2D avatar 🎭 accordingly. The included "Aria" model is specially rigged for optimal performance, but you can use your own (see [Live2D Integration Guide](./Live2D.md)). The visuals easily integrate into streaming software like OBS Studio using Spout!

> **❗ Important Note on AI Model and Personality:**
> Persona Engine shines brightest with a **specially fine-tuned Large Language Model (LLM)**. This model understands the engine's unique way of sending info, leading to more natural, in-character chats!
>
> While you *can* use standard OpenAI-compatible models (like those from Ollama, Groq, OpenAI, etc.) by carefully editing the `Resources/Prompts/personality.txt` file, it might take some extra effort (prompt engineering magic!) to get perfect results.
>
> ✨ I've included a helpful template for standard models! Look for `personality_example.txt` **in the root of the source code repository** (you might need to grab it from the [GitHub repo page](https://github.com/fagenorn/handcrafted-persona-engine) if you only downloaded the release `.zip`). This file is a starting point if you're using a standard model. Copy its ideas or content into your *actual* `personality.txt` file (located in `Resources/Prompts/` inside the extracted release folder).
>
> The fine-tuned model is currently being tested. Want to try it or see a demo? Hop into our Discord! 😊


## <a id="features"></a>✨ Features Galore!
<div align="center">
<img src="assets/mascot_wand.png" width="150" alt="Mascot with Wand">
</div>

* 🎭 **Live2D Avatar Integration:** Loads and renders Live2D models, including the specially rigged "Aria" model provided. Supports emotion-driven animations and VBridger-standard lip-sync. **See the detailed [Live2D Integration & Rigging Guide](./Live2D.md) for custom model requirements!**
* 🧠 **AI-Driven Conversation:** Connects to OpenAI-compatible LLM APIs (run locally or in the cloud!), using `personality.txt`. Optimized for the special fine-tuned model (see [Overview](#overview)).
* 🗣️ **Voice Interaction:** Listens via microphone (NAudio/PortAudio), detects speech with Silero VAD, and understands you with Whisper ASR (Whisper.net). Uses a small model for interruption detection and a larger one for transcription. **NVIDIA GPU REQUIRED!**
* 🔊 **Advanced Text-to-Speech (TTS):** A sophisticated pipeline (normalization, segmentation, phonemization, ONNX synthesis) brings text to life, supporting custom `kokoro` voices. Uses `espeak-ng` as a fallback for unknown words (e.g., "heyyyyyy"). **NVIDIA GPU REQUIRED!**
* 👤 **Optional Real-time Voice Cloning (RVC):** Integrates RVC ONNX models to make the TTS voice sound like someone specific in real-time. **NVIDIA GPU REQUIRED!** Can be disabled in `appsettings.json` if performance is a concern (can be CPU-intensive).
* 📜 **Customizable Subtitles:** Show what's being said with lots of options to make it look just right.
* 💬 **Control UI & Chat Viewer:** A dedicated UI window allows monitoring, live adjustment of some settings (TTS & Roulette Wheel), and viewing/editing the conversation history.
* 👀 **Screen Awareness (Experimental):** Optional Vision module lets the AI "see" application windows.
* 🎡 **Interactive Roulette Wheel (Experimental):** Spin a fun wheel on screen!
* 📺 **Streaming Output (Spout):** Sends the visuals (Avatar, Roulette) directly to OBS or other Spout-compatible software via separate configurable streams. No window capture needed!
* 🎶 **Audio Output:** Plays generated speech clearly (via PortAudio).
* ⚙️ **Configuration:** Easy setup via `appsettings.json` (see [Configuration](#configuration) for the structure) and the built-in UI editor for certain settings.
* 🤬 **Profanity Detection:** Basic + ML-based filtering options.


<div align="center">
<br>
<h2><a id="community"></a>💬 Join Our Community! 💬</h2>
<p>
Need help getting started? Have questions or brilliant ideas? 💡 Want to see a live demo, test the special fine-tuned model, or chat directly with a Persona Engine character? Having trouble converting RVC models or rigging your own Live2D model? Come say hi on Discord! 👋
</p>
<a href="https://discord.gg/p3CXEyFtrA" target="_blank">
<img src="assets/discord.png" alt="Join Discord Img"
  width="400"
  /></a>
  <br>
<img src="https://img.shields.io/discord/1347649495646601419?label=Join%20Discord&logo=discord&style=for-the-badge" alt="Join Discord Badge" />
</a>
<br>
</div>


## <a id="architecture"></a>⚙️ Architecture / How it Works

Persona Engine operates in a continuous loop, bringing your character to life through these steps:

1.  **Listen:** 🎤 Your microphone captures audio. The Voice Activity Detector (VAD) identifies when you start and stop speaking.
2.  **Understand:** 👂 As you speak, the faster `ggml-tiny.en.bin` Whisper model performs intermediate recognition, partly to detect if you interrupt the character. Once you finish, the more accurate `ggml-large-v3-turbo.bin` model transcribes your full speech to text.
3.  **Contextualize (Optional):** 👀 If the Vision module is enabled, it captures text from a specified window to give the AI awareness of on-screen activity.
4.  **Think:** 🧠 Your transcribed text, conversation history, optional vision context, and the character's rules (from `personality.txt`) are sent to the configured Large Language Model (LLM).
5.  **Respond:** 💬 The LLM generates a text response, potentially including emotion tags (e.g., `[EMOTION:😊]`).
6.  **Filter (Optional):** 🤬 The response can be checked for profanity.
7.  **Speak:** 🔊 The Text-to-Speech (TTS) system converts the response text into audio using a `kokoro` voice model. It uses `espeak-ng` as a phonemizer fallback for unusual words.
8.  **Clone (Optional):** 👤 If RVC is enabled, the generated TTS audio is modified in real-time to match the target voice profile using the selected RVC ONNX model.
9.  **Animate:** 🎭
    * Phoneme timings from the TTS/RVC process drive lip-sync animations based on the VBridger standard.
    * Emotion tags from the LLM trigger corresponding facial expressions and body motions defined in the Live2D model.
    * Idle animations and blinking keep the character looking natural when not speaking.
    * **(See [Live2D Integration & Rigging Guide](./Live2D.md) for rigging details!)**
10. **Display:**
    * 📜 Subtitles for the character's speech are generated.
    * 📺 The animated Live2D avatar and subtitles (and optional Roulette Wheel) are rendered and sent out via Spout streams.
    * 🎶 The final synthesized audio is played through your selected output device.
11. **Loop:** The engine returns to listening for your next input.

<div align="center">
<br/>
*(Conceptual Flow Diagram would go here if desired)*
<br/>
<br/>
</div>


## <a id="prerequisites"></a>📋 Prerequisites: Let's Get Ready!

Before starting the magic, let's gather the supplies! Make sure you have everything below. **An NVIDIA GPU with CUDA is currently MANDATORY.**

<div align="center">
<img src="assets/mascot_checklist.png" width="150" alt="Mascot with Checklist">
<p>Make sure you have these ready:</p>
</div>

### <a id="prereq-system"></a>1. System Requirements 🖥️

<details>
<summary><strong>➡️ Click here for detailed system notes...</strong></summary>

* 💻 **Operating System:**
    * ✅ **Windows (Strongly Recommended):** Developed and tested primarily on Windows 10/11. Pre-built releases are Windows-only.
    * ⚠️ **Linux / macOS:** Possible *only* by building from source. Needs significant technical expertise for setup (CUDA, Spout alternatives like Syphon, Audio library linking) and is **not officially supported or tested**. Proceed at your own risk!
* 💪 **Graphics Card (GPU):**
    * ✅ **NVIDIA GPU with CUDA Support (REQUIRED):** This is **absolutely essential** for the engine to function. ASR, TTS, and RVC heavily rely on CUDA for acceleration via ONNX Runtime. Without a compatible NVIDIA GPU and correctly installed CUDA/cuDNN, the application **will not work**. See the **[CUDA & cuDNN Installation Guide](#prereq-cuda)** below – follow it precisely! You MUST install the latest NVIDIA drivers.
    * ❌ **CPU-Only / AMD / Intel GPUs:** **Not supported.** The AI components require CUDA acceleration and will fail to initialize or run without it.
* 🎤 **Microphone:** To talk to your character!
* 🎧 **Speakers / Headphones:** To hear them reply!

</details>

### <a id="prereq-cuda"></a>2. 💪 Installing NVIDIA CUDA and cuDNN (MANDATORY!)

This step is **non-negotiable** for running Persona Engine. The AI components (ASR, TTS, RVC) rely specifically on CUDA and cuDNN being installed correctly. **Follow these steps carefully, especially the manual cuDNN copy.**

<details>
<summary><strong>➡️ Click here for the REQUIRED CUDA + cuDNN Setup Guide (Windows)...</strong></summary>

Persona Engine relies on specific CUDA components (ONNX Runtime CUDA Provider) which have strict dependencies. Failure to install these correctly *will* result in errors preventing the application from running (see [Troubleshooting](#troubleshooting)).

1.  **Check GPU Compatibility & Install Driver:**
    * Make sure your NVIDIA GPU supports CUDA ([NVIDIA CUDA GPUs list](https://developer.nvidia.com/cuda-gpus)).
    * Get the **latest NVIDIA Game Ready or Studio driver** ([NVIDIA Driver Downloads](https://www.nvidia.com/Download/index.aspx)). A clean install is often best.

2.  **Install CUDA Toolkit (Version 12.1 or 12.2 Recommended):**
    * The engine expects CUDA Runtime 12.1 or 12.2. **CUDA 12.2 is recommended**.
    * Go to the [NVIDIA CUDA Toolkit 12.2 Download Archive](https://developer.nvidia.com/cuda-12-2-0-download-archive). (Using the archive ensures you get the correct version).
    * Choose your system settings (Windows, x86_64, 11 or 10, `exe (local)`).
    * Download and run the installer. **Express (Recommended)** is usually fine.
    * Note the install path (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2`).

3.  **Install cuDNN Library (CRITICAL STEP - Manual Copy Required!):**
    * cuDNN accelerates deep learning operations used by the engine.
    * **❗ You MUST download the TARBALL (.tar.xz or .zip) version, NOT the .exe installer.** The installer often doesn't place files where ONNX Runtime expects them.
    * Go to the [NVIDIA cuDNN Download Page](https://developer.nvidia.com/rdp/cudnn-download) (Requires a free NVIDIA Developer account).
    * **Very Important:** Select the cuDNN version **compatible with your installed CUDA Toolkit**. For CUDA 12.2, choose a **cuDNN v8.9.x or v9.x for CUDA 12.x**. Specifically, **cuDNN v9 is recommended for CUDA 12.2**.
    * Download the "**Local Installer for Windows (Tar)**" or "(Zip)" file for your chosen version (e.g., `cudnn-windows-x86_64-9.x.x.x_cuda12-archive.zip`).
    * **Extract the cuDNN archive** somewhere temporary (e.g., your Downloads folder). You'll find folders like `bin`, `include`, `lib`.
    * **Manually copy the extracted files into your CUDA Toolkit installation directory:**
        * Copy the **contents** of the extracted cuDNN `bin` folder -> Your CUDA Toolkit `bin` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\bin`)
        * Copy the **contents** of the extracted cuDNN `include` folder -> Your CUDA Toolkit `include` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\include`)
        * Copy the **contents** of the extracted cuDNN `lib` folder (or `lib\x64`) -> Your CUDA Toolkit `lib\x64` folder (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\lib\x64`)
        * *(Ensure `v12.2` matches your installed CUDA version!)*

4.  **Add CUDA Binaries to System Path (Important!):**
    * Helps Windows and the application find the necessary CUDA libraries.
    * Search "Environment Variables" in Windows -> Click "Edit the system environment variables".
    * Click the "Environment Variables..." button.
    * Under "System variables", find the `Path` variable -> Click "Edit...".
    * Click "New" and add the path to your CUDA `bin` directory (use your actual CUDA version):
        * `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\bin`
    * *(Optional but sometimes helpful: Add `libnvvp` path too)*
        * `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\libnvvp`
    * Click OK on all windows to save the changes.

5.  **Restart Your Computer!**
    * This is **crucial** for the system PATH changes and driver/library updates to take full effect.

6.  **Verification (Optional but Recommended):**
    * After restarting, open Command Prompt (`cmd`).
    * Type `nvidia-smi` and press Enter. If it runs and shows your GPU details along with the CUDA version you installed (e.g., CUDA Version: 12.2), the driver and basic CUDA setup are likely correct.
    * When you run Persona Engine, check its console window for messages indicating that CUDA is detected and initialized successfully (e.g., messages from ONNX Runtime). If you see errors mentioning `cudnn64_*.dll` or `onnxruntime_providers_cuda.dll`, double-check the cuDNN manual copy (Step 3) and PATH variable (Step 4). See [Troubleshooting](#troubleshooting).

</details>

### <a id="prereq-software"></a>3. 🛠️ Software to Install (Besides CUDA/Drivers)

You need these two helpers installed *before* running Persona Engine:

* ✅ **[.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0):** The engine's core framework. Install the **Runtime** (not SDK unless building from source) system-wide from Microsoft. (The release `.zip` *might* include a `dotnet_runtime` folder for convenience, but a system-wide install is preferred).
* ✅ **[`espeak-ng`](https://github.com/espeak-ng/espeak-ng/releases):** Required by the TTS system for phonemization, especially as a **fallback for unknown or out-of-dictionary words** (like slang or expressive sounds, e.g., "soooooo cool"). **TTS may fail or sound incorrect without it!**
    1.  Go to the `espeak-ng` releases page.
    2.  Download the latest installer for Windows (e.g., `espeak-ng-*.msi`).
    3.  ❗ **Important:** During installation, **make sure to check the box that says "Add espeak-ng to the system PATH"**. This is the easiest way.
    4.  *Alternatively*: If you don't add it to PATH during install, you **must** manually find the `espeak-ng` installation folder (usually `C:\Program Files\eSpeak NG`) and put its *path* (e.g., `"C:\\Program Files\\eSpeak NG"`) into the `Config.Tts.EspeakPath` setting in `appsettings.json`. (Note: the value should be the *folder path*, not the path to the `.dll` itself, and use double backslashes `\\` in JSON).

### <a id="prereq-models"></a>4. ❗ Essential Models & Resources (Download Separately!) ❗

The release `.zip` includes most core components (TTS models, VAD, the "Aria" avatar, personality template). However, the large Whisper ASR models need to be downloaded manually, and the `personality_example.txt` file needs to be obtained from the source repository if using a standard LLM.

* 🧠 **Whisper ASR Models (Mandatory Download):**
    * **What:** AI models that convert your voice 🗣️ into text 📝. **GGUF format is required.**
    * You need **both**:
        * `ggml-tiny.en.bin` (Faster, used for intermediate recognition and detecting speech interruptions)
        * `ggml-large-v3-turbo.bin` (Slower, higher accuracy, used for final transcription - Recommended for actual use!)
    * **Where to get them:** Download both `.bin` files from the dedicated release tag:
        **[➡️ Download Whisper Models Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
    * **Where they go:** After unzipping Persona Engine, place **both** downloaded `.bin` files directly into the 📁 `Resources/Models/` folder.

* 🎭 **Live2D Avatar Model ("Aria" Included / Replaceable):**
    * **What:** Your character's data files (`.model3.json`, textures, physics, motions, etc.). Rigging quality significantly impacts animation.
    * **Included:** A specially rigged demo avatar ("Aria") is provided in 📁 `Resources/Live2D/Avatars/aria/`. The default configuration (`appsettings.json`) points to this model. "Aria" is designed to work well with the engine's animation features.
    * **To use yours:**
        1.  **Crucially, review the [Live2D Integration & Rigging Guide](./Live2D.md)** to understand the required parameters (especially for VBridger lip-sync) and animation setup.
        2.  Create a new folder inside 📁 `Resources/Live2D/Avatars/` (e.g., `MyAvatar`), place all your Live2D model files inside that folder.
        3.  Edit `appsettings.json` and change the `Config.Live2D.ModelName` setting to match your folder name (e.g., `"MyAvatar"`).

* 📝 **Personality Prompts (`personality.txt` & `personality_example.txt`):**
    * **What:** Text files that instruct the LLM on how your character should behave, speak, and react. 🧠✨ Critical for defining the persona.
    * **`personality.txt`:** This is the **active** configuration file loaded by the engine, located in `Resources/Prompts/` within the extracted application folder. It's initially optimized for the special fine-tuned model (see [Overview](#overview)).
    * **`personality_example.txt`:** This is a **template and guide** specifically designed for use with **standard** OpenAI-compatible LLMs (like those from Ollama, Groq, standard OpenAI models). **It is located in the root of the source code repository**, not in the release `.zip`. You must get it from [GitHub](https://github.com/fagenorn/handcrafted-persona-engine).
    * ❗ **Action Required if using a Standard LLM:** The default `personality.txt` (included in the release `.zip`) likely **won't work well** out-of-the-box with standard models. You **must**:
        1.  Obtain `personality_example.txt` from the [source code repository](https://github.com/fagenorn/handcrafted-persona-engine/blob/main/personality_example.txt).
        2.  Open the `personality.txt` file located in `Resources/Prompts/` inside your Persona Engine folder.
        3.  **Delete the default contents of `personality.txt`.**
        4.  **Copy the entire contents from `personality_example.txt` into the now empty `personality.txt`**.
        5.  Afterwards, customize the instructions within `personality.txt` extensively to define your specific character. This requires prompt engineering effort!
    * **Where it is:** The *active* `personality.txt` is in the 📁 `Resources/Prompts/` folder. The *example template* `personality_example.txt` is in the [repository root](https://github.com/fagenorn/handcrafted-persona-engine).

* 🔊 **TTS Resources (Included in Release):** Files needed for speech synthesis (voices, phonemizers, etc.). Found in 📁 `Resources/Models/kokoro/`. Typically no user action needed here.
* 👂 **VAD Model (Included in Release):** The Voice Activity Detection model (`silero_vad.onnx`). Found in 📁 `Resources/Models/`.

### <a id="prereq-rvc"></a>5. Optional: 👤 RVC Models (for Voice Cloning)

* **What:** If you want the synthesized TTS voice to mimic a specific target voice, you need a Real-time Voice Cloning (RVC) model trained for that voice. The model **must be exported to ONNX format** (usually a single `.onnx` file).
* **Note on `.pth` files:** Standard RVC training outputs `.pth` files. These **cannot be used directly** and **must be converted to the ONNX format**. This conversion process can be complex. Need assistance? Join our Discord! 😊
* **Performance:** RVC adds computational load, primarily on the CPU (due to pitch estimation like crepe) and GPU. If you experience performance issues, you can disable RVC by setting `Config.Tts.Rvc.Enabled` to `false` in `appsettings.json`.
* **Where it goes:** Place your converted RVC `.onnx` file inside the 📁 `Resources/Models/rvc/voice/` folder. You'll also need to enable RVC and potentially set the correct `Config.Tts.Rvc.DefaultVoice` name (matching the `.onnx` filename without extension) in `appsettings.json`.

### <a id="prereq-llm"></a>6. 🧠 LLM Access (The "Brain")

* **What:** You need connectivity to a Large Language Model service via an OpenAI-compatible API. This involves knowing:
    * **API Endpoint URL:** The web address where the LLM service is hosted (e.g., `http://localhost:11434/v1` for a local Ollama instance, or a cloud provider's URL like `https://api.groq.com/openai/v1`). Set in `Config.Llm.TextEndpoint`.
    * **(Optional) API Key:** A secret token required by some services (e.g., OpenAI, Groq). Set in `Config.Llm.TextApiKey`. Leave blank if not needed.
    * **Model Name:** The identifier of the specific AI model you want to use (e.g., `gpt-4o`, `llama3`, `mistral`, `your-fine-tuned-model-id`). Set in `Config.Llm.TextModel`.
* **Options:**
    * **🏠 Local:** Run an LLM on your own computer using tools like Ollama, LM Studio, Jan, etc. Often requires a proxy like LiteLLM to provide the OpenAI-compatible `/v1` endpoint. Needs a powerful PC, especially significant **GPU VRAM** (Video Memory)!
    * **☁️ Cloud:** Use a hosted LLM service (OpenAI, Groq, Anthropic, Together AI, etc.). Usually requires account creation, API key generation, and may incur costs based on usage.
* ❗ **Personality Reminder:** Remember to configure `Resources/Prompts/personality.txt` appropriately for the type of LLM you are connecting to (use `personality_example.txt` from the repo as a guide for standard models). Join the Discord for details on accessing the recommended fine-tuned model.

### <a id="prereq-spout"></a>7. 📺 Spout Receiver (To See Your Avatar!)

* **What:** Persona Engine **does not display the avatar in its own main window**. It renders the avatar and sends the video feed out using a technology called **Spout**. You need a separate application capable of receiving a Spout stream to view the output.
* **Recommendation:** ✅ **OBS Studio** is the most common and highly recommended application for this, especially if you plan on streaming or recording.
* **Required Plugin for OBS:** You must install the **Spout2 Plugin for OBS Studio**. Download it from: [https://github.com/Off-World-Live/obs-spout2-plugin/releases](https://github.com/Off-World-Live/obs-spout2-plugin/releases) (Make sure to download the correct version for your OBS installation).
* **How:** Install the Spout2 plugin into your OBS Studio installation directory. After starting Persona Engine, you will add a "Spout2 Capture" source in OBS to receive the video feed (details in [Getting Started](#getting-started)). Persona Engine can output multiple Spout streams (e.g., one for the Live2D model, one for the Roulette Wheel) configurable in `appsettings.json`.


## <a id="getting-started"></a>🚀 Getting Started: Let's Go!
<div align="center">
<img src="assets/mascot_wrench.png" width="150" alt="Mascot with Wrench">
<p>Ready to bring your character to life? Choose your path:</p>
</div>

### <a id="install-release"></a>Method 1: Easy Install with Pre-built Release (Recommended for Windows Users 💖)

This is the simplest way to get up and running on Windows.

**Step 1: 💾 Download & Extract Persona Engine**

<div align="center">
  <a href="https://github.com/fagenorn/handcrafted-persona-engine/releases/latest" target="_blank">
  <img
  src="assets/download.png"
  alt="Download Latest Release Button"
  width="300"
>
  </a>
  <p><i>(Click the button, get the `.zip` file from the latest release!)</i></p>
</div>

* Locate the downloaded `.zip` archive (e.g., `PersonaEngine_vX.Y.Z.zip`).
* Right-click the file and choose "Extract All..." (or use a tool like 7-Zip or WinRAR).
* Select a location to extract the files. ✅ **Recommended:** A simple path like `C:\PersonaEngine` or on another drive. **Avoid** system-protected folders like `C:\Program Files` or `C:\Windows`.

**Step 2: 🛠️ Install Prerequisites (CRITICAL - Do not skip!)**

* ✅ **NVIDIA Driver, CUDA, and cuDNN:** **MANDATORY!** Did you install them correctly following the **REQUIRED** guide? (See [Prerequisites Section 2](#prereq-cuda), paying close attention to the **manual cuDNN copy from the tar/zip archive**). **Reboot** after installation! The application WILL NOT RUN without this.
* ✅ **.NET 9.0 Runtime:** Is the runtime installed system-wide? (See [Prerequisites Section 3](#prereq-software)).
* ✅ **`espeak-ng`:** Is it installed, and crucially, was it **added to the system PATH** during installation? (See [Prerequisites Section 3](#prereq-software)). If not added to PATH, you'll need to edit `appsettings.json` later.

**Step 3: 📥 Download and Place Required Whisper Models**

* Go to the Whisper Models download page: **[➡️ Download Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
* Download **both** `.bin` files: `ggml-tiny.en.bin` and `ggml-large-v3-turbo.bin`.
* Navigate into the folder where you extracted Persona Engine. Find the 📁 `Resources/Models/` subfolder.
* Place the two downloaded `.bin` files directly inside this `Resources/Models/` folder.

**Step 4: ⚙️ Initial Configuration (`appsettings.json` & `personality.txt`)**

* In your extracted Persona Engine folder, find the `appsettings.json` file. Open it with a good text editor (like Notepad++, VS Code, Sublime Text, or even standard Notepad). **Refer to the [Configuration](#configuration) section for the exact structure.**
* **Primary Settings to Verify/Edit:**
    * `Config.Llm` section:
        * `TextEndpoint`: Set this to the full URL of your LLM API service (e.g., `"https://api.groq.com/openai/v1"`).
        * `TextModel`: Enter the exact name of the LLM model you intend to use (e.g., `"llama-3.1-70b-versatile"`).
        * `TextApiKey`: Enter your API key *only if* your LLM service requires one. Otherwise, leave it as `""` (empty string).
    * `Config.Live2D` section:
        * `ModelName`: By default, this is `"aria"`. If you plan to use the included model initially, leave it. If you've placed your own **correctly rigged** Live2D model in `Resources/Live2D/Avatars/YourModelFolder/`, change this value to `"YourModelFolder"`. (See [Live2D Guide](./Live2D.md)).
    * `(If needed) Config.Tts.EspeakPath`: If you did **not** add `espeak-ng` to your system PATH during installation, find its installation folder (e.g., `C:\Program Files\eSpeak NG`) and put that path here (use double backslashes, e.g., `"C:\\Program Files\\eSpeak NG"`). Otherwise, leave it as `"espeak-ng"`.
* Save the changes to `appsettings.json`.
* **Configure Personality (`personality.txt`) - IMPORTANT!**
    * Navigate to the 📁 `Resources/Prompts/` folder inside your Persona Engine installation directory.
    * ❗ **Reminder:** If you are **not** using the special fine-tuned LLM (ask on Discord!), the default `personality.txt` content (included in the `.zip`) is likely unsuitable for standard models.
    * **Action for Standard LLMs:**
        1.  Go to the [Persona Engine GitHub repository](https://github.com/fagenorn/handcrafted-persona-engine).
        2.  Find and open the `personality_example.txt` file in the main (root) directory.
        3.  Copy its entire contents.
        4.  Open the `personality.txt` file located in your `Resources/Prompts/` folder.
        5.  **Delete all the default content** inside `personality.txt`.
        6.  **Paste the content you copied from `personality_example.txt`** into the now empty `personality.txt`.
        7.  Now, **edit `personality.txt`** thoroughly. Modify the instructions, rules, character descriptions, example dialogues, etc., to match the specific persona you want to create. This is a crucial step and may require experimentation (prompt engineering).
    * Save `personality.txt`.

**Step 5: ▶️ Run Persona Engine!**

* Find and double-click the `PersonaEngine.exe` file in the main extracted folder.
* A **Configuration and Control UI** window should appear. This window is for settings, monitoring, and chat history; **it does not show the avatar**.
* A separate **console window** (black background with text) will likely open behind the UI. **Watch this console window carefully** for startup messages. Look for confirmation that **CUDA/GPU is detected and initialized successfully by ONNX Runtime**. Note any errors, especially `LoadLibrary failed` or cuDNN errors (see [Troubleshooting](#troubleshooting)).
* If the LLM connection is successful and the required models/prerequisites are found, the engine should initialize and start listening for your voice. Try speaking into your microphone! 🎤

**Step 6: 📺 View the Avatar (via Spout in OBS)**

* You need a Spout receiver application running. This guide uses OBS Studio.
* ✅ Ensure **OBS Studio** is installed.
* ✅ Ensure the **Spout2 Plugin for OBS** is installed correctly (link in [Prerequisites Section 7](#prereq-spout)).
* Launch OBS Studio.
* In the "Sources" panel (usually at the bottom), click the "+" button.
* Select **"Spout2 Capture"** from the list.
* Give the source a name (e.g., "Persona Engine Avatar") and click OK.
* A properties window will appear. Look for the "Spout Sender" dropdown list.
* Select the sender corresponding to the Persona Engine avatar (the default name is "Live2D", check `Config.SpoutConfigs` in `appsettings.json`).
* Click OK. Your Live2D avatar, rendered by Persona Engine, should now appear in your OBS scene! ✨ Resize and position as needed.
* (Optional) Add another "Spout2 Capture" source for the Roulette Wheel if enabled, selecting its corresponding sender name (default "RouletteWheel").

**Step 7: 🔧 Further Customization (Optional)**

* Explore other settings in `appsettings.json` (like audio input/output devices, TTS voice selection, speed/pitch, subtitle appearance, RVC enabling/tuning) or adjust live settings using the **Configuration and Control UI** (specifically for TTS and Roulette Wheel sections). Check the UI's chat history panel.

---

### <a id="install-source"></a>Method 2: Building from Source (For Developers & Advanced Users 🛠️)

*(This requires more technical steps and familiarity with .NET development.)*

1.  **Prerequisites:**
    * ✅ Install **Git**.
    * ✅ Install **.NET 9.0 SDK** (Software Development Kit) ([Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0)).
    * ✅ Install **`espeak-ng`** (+ added to system PATH or path configured). (See [Prerequisites Section 3](#prereq-software)).
    * ✅ **MANDATORY:** Install **NVIDIA Driver, CUDA, and cuDNN** following the **REQUIRED** guide meticulously (**manual cuDNN copy from tar/zip!**). (See [Prerequisites Section 2](#prereq-cuda)). **Reboot** after install.
    * ⚠️ Non-Windows: Requires finding and installing equivalents for PortAudio, Spout (like Syphon on macOS), and handling platform-specific dependencies. **This is unsupported territory!**
2.  **Clone Repository:**
    ```bash
    git clone [https://github.com/fagenorn/handcrafted-persona-engine.git](https://github.com/fagenorn/handcrafted-persona-engine.git)
    cd handcrafted-persona-engine
    ```
3.  **Restore Dependencies:** Open a terminal or command prompt in the repository root directory and run:
    ```bash
    dotnet restore
    ```
4.  **Build the Application:**
    ```bash
    # Example command for a Release build for Windows x64:
    dotnet publish PersonaEngine.App -c Release -r win-x64 -o ./publish --self-contained false
    # Adjust -r (runtime identifier) if needed (linux-x64, osx-x64 are unsupported but theoretically possible)
    # --self-contained false relies on the globally installed .NET Runtime
    ```
5.  **Navigate to Output Directory:** The built application files will be in the `./publish` folder (or wherever your `-o` argument pointed).
6.  **Prepare Models & Resources Directory:**
    * Go into the `./publish` directory.
    * Manually create the necessary folder structure if it doesn't exist: 📁 `Resources/Models/rvc/voice`, 📁 `Resources/Live2D/Avatars`, 📁 `Resources/Prompts`.
    * 📥 **Download Whisper GGUF models:** Get `ggml-tiny.en.bin` and `ggml-large-v3-turbo.bin` from the [Whisper Models Release](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models) and place them into `./publish/Resources/Models/`.
    * 🎭 **Copy Live2D Model:** Copy the included "Aria" model folder (`Resources/Live2D/Avatars/aria`) from the repository into `./publish/Resources/Live2D/Avatars/`. Or place your own custom-rigged model folder here (check [Live2D Guide](./Live2D.md) first!).
    * 🔊 **Copy Core Models:** Copy the contents of the *original repository's* `Resources/Models` folder (including the `kokoro` subfolder and `silero_vad.onnx`) into your `./publish/Resources/Models/` folder.
    * 📝 **Copy Prompts:** Copy `personality.txt` and `personality_example.txt` from the *original repository's* `Resources/Prompts` folder into your `./publish/Resources/Prompts/` folder. **Important:** You will likely need to edit the copied `personality.txt` based on `personality_example.txt` if using a standard LLM (see step 7).
    * 👤 **Place RVC Models (Optional):** If using RVC, place your `.onnx` RVC model file(s) into `./publish/Resources/Models/rvc/voice/`.
7.  **Configure `appsettings.json` & `personality.txt`:**
    * Copy the default `appsettings.json` file from the source project (`PersonaEngine.App/appsettings.json`) into your `./publish` directory.
    * ⚙️ Edit `./publish/appsettings.json`: Configure `Config.Llm` settings (Endpoint, Model, Key), set `Config.Live2D.ModelName` to your model's folder name ("aria" or your custom one), and adjust `Config.Tts.EspeakPath` if needed (as described in Method 1, Step 4). **Refer to the [Configuration](#configuration) section for the structure.**
    * ❗ **Critically:** Edit `./publish/Resources/Prompts/personality.txt`. **If using a standard LLM, you MUST replace its content with the content from `personality_example.txt` (also copied in step 6)** and then customize it extensively for your character and LLM.
8.  **Run the Application:**
    * Open a terminal or command prompt inside the `./publish` directory.
    * ▶️ Execute the application: `dotnet PersonaEngine.App.dll`
    * Monitor the console output carefully for initialization messages (especially **CUDA status from ONNX Runtime**) and any errors.
    * 📺 Set up your Spout receiver (e.g., OBS) to view the avatar output as described in Method 1, Step 6.


## <a id="configuration"></a>🔧 Configuration (`appsettings.json`)

<div align="center">
<img src="assets/mascot_cog.png" width="150" alt="Mascot with Cog">
<p>Configuring your engine!</p>
</div>

This JSON file, located in the main application folder (or `./publish` if built from source), is your primary control panel. Open it with a text editor. Changes typically require restarting the application, **except for settings within the `Tts` and `RouletteWheel` sections, which can be modified live via the Control UI.**

The structure is as follows (referencing the default values):

```json
{
  "Config": {
    "Window": { // Basic window settings (less relevant as output is via Spout)
      "Width": 1920,
      "Height": 1080,
      "Title": "Persona Engine",
      "Fullscreen": false
    },
    "Llm": { // Large Language Model connection settings
      "TextApiKey": "gsk_...", // Your API Key (if required, else "")
      "TextModel": "llama-3.1-70b-versatile", // Model name identifier
      "TextEndpoint": "https://api.groq.com/openai/v1", // API URL
      "VisionApiKey": "sk-...", // API Key for Vision model (if used)
      "VisionModel": "...", // Vision model name
      "VisionEndpoint": "http://..." // Vision model API URL
    },
    "Tts": { // Text-to-Speech settings (Live Editable via UI)
      "EspeakPath": "espeak-ng", // Path to espeak-ng install dir or "espeak-ng" if in PATH
      "Voice": { // Base TTS voice settings
        "DefaultVoice": "en_custom_2", // Name of the kokoro voice model to use
        "UseBritishEnglish": false, // Pronunciation preference
        "DefaultSpeed": 1.0, // Speech rate (1.0 = normal)
        "MaxPhonemeLength": 510, // Internal buffer limit
        "SampleRate": 24000, // Audio sample rate
        "TrimSilence": false // Whether to trim silence from audio ends
      },
      "Rvc": { // Real-time Voice Cloning settings
        "DefaultVoice": "KasumiVA", // Name of the RVC .onnx model (in Resources/Models/rvc/voice/)
        "Enabled": true, // Enable/Disable RVC globally
        "HopSize": 64, // RVC processing parameter (affects quality/latency)
        "SpeakerId": 0, // Speaker ID within the RVC model (usually 0)
        "F0UpKey": 1 // Pitch shift adjustment (semitones)
      }
    },
    "Subtitle": { // Subtitle appearance settings
      "Font": "DynaPuff_Condensed-Bold.ttf", // Font file name (in Resources/Fonts/)
      "FontSize": 125,
      "Color": "#FFf8f6f7", // Text color (ARGB Hex)
      "HighlightColor": "#FFc4251e", // Highlight color (if supported)
      "BottomMargin": 250, // Pixels from bottom edge
      "SideMargin": 30, // Pixels from side edges
      "InterSegmentSpacing": 10, // Space between lines/segments
      "MaxVisibleLines": 2, // Max lines shown at once
      "AnimationDuration": 0.3, // Fade in/out time (seconds)
      "Width": 1080, // Canvas width for positioning
      "Height": 1920 // Canvas height for positioning
    },
    "Live2D": { // Live2D model settings
      "ModelPath": "Resources/Live2D/Avatars", // Base path for avatar folders
      "ModelName": "aria", // Folder name of the model to load
      "Width": 1080, // Render target width
      "Height": 1920 // Render target height
    },
    "SpoutConfigs": [ // Configuration for Spout video outputs
      {
        "OutputName": "Live2D", // Name OBS will see for this stream
        "Width": 1080, // Output resolution width
        "Height": 1920 // Output resolution height
      },
      {
        "OutputName": "RouletteWheel", // Separate stream for the roulette wheel
        "Width": 1080,
        "Height": 1080
      }
    ],
    "Vision": { // Experimental screen awareness settings
      "WindowTitle": "Microsoft Edge", // Window title to capture text from
      "Enabled": false, // Enable/Disable vision module
      "CaptureInterval": "00:00:59", // How often to capture (HH:MM:SS)
      "CaptureMinPixels": 50176, // Min window size to capture
      "CaptureMaxPixels": 4194304 // Max window size to capture
    },
    "RouletteWheel": { // Experimental roulette wheel settings (Live Editable via UI)
      "Font": "DynaPuff_Condensed-Bold.ttf",
      "FontSize": 24,
      "TextColor": "#FFFFFF",
      "TextScale": 1.0,
      "TextStroke": 2.0,
      "AdaptiveText": true,
      "RadialTextOrientation": true,
      "SectionLabels": [ "Yes", "No" ], // Text for each wheel section
      "SpinDuration": 8.0, // Seconds for spin animation
      "MinRotations": 5.0, // Minimum full rotations during spin
      "WheelSizePercentage": 1.0, // Size relative to its spout output dimensions
      "Width": 1080, // Render target width
      "Height": 1080, // Render target height
      "PositionMode": "Anchored", // Positioning method
      "ViewportAnchor": "Center", // Anchor point
      "PositionXPercentage": 0.5, // X position (0-1)
      "PositionYPercentage": 0.5, // Y position (0-1)
      "AnchorOffsetX": 0,
      "AnchorOffsetY": 0,
      "AbsolutePositionX": 0,
      "AbsolutePositionY": 0,
      "Enabled": false, // Enable/Disable the roulette wheel feature
      "RotationDegrees": -90.0, // Initial rotation offset
      "AnimateToggle": true, // Animate showing/hiding
      "AnimationDuration": 0.5 // Show/hide animation time
    }
  }
}
````

  * Ensure JSON syntax is correct (commas between elements, quotes around keys and string values, correct braces `{}` and brackets `[]`).

## <a id="usage"></a>▶️ Usage: Showtime\!

1.  ✅ **Double-check Prerequisites**: Is **NVIDIA drivers, CUDA, and cuDNN** installed correctly (using the **manual tarball copy method** as per [Section 2](#prereq-cuda))? Is .NET Runtime installed? Is `espeak-ng` installed (+ in PATH or path set in JSON)? Are the **Whisper `.bin` models** in `Resources/Models/`? Is your Spout receiver (OBS + Plugin) ready? **This is crucial - the app won't run without CUDA.**
2.  ⚙️ **Verify Configuration**: Did you set the correct `Config.Llm.TextEndpoint`, `Config.Llm.TextModel`, and `Config.Llm.TextApiKey` in `appsettings.json`? Is `Config.Live2D.ModelName` set to your avatar's folder name ("aria" or your custom one)?
3.  📝 **Check Personality**: Is `Resources/Prompts/personality.txt` configured appropriately for your **chosen LLM**? (Did you copy from `personality_example.txt` in the repo and customize if using a standard model?)
4.  ▶️ **Run the Application**: Execute `PersonaEngine.exe` (from release) or `dotnet PersonaEngine.App.dll` (if built from source).
5.  🖥️ **Monitor Startup**: The **Config & Control UI** will appear. Pay close attention to the **console window** behind it for log messages. **Critically, look for "CUDA" and "ONNX Runtime" messages confirming successful GPU initialization.** Errors here usually point to CUDA/cuDNN installation issues ([Troubleshooting](#troubleshooting)).
6.  📺 **Activate Spout Receiver**: Open OBS (or your chosen receiver), add a "Spout2 Capture" source, and select the Persona Engine sender name (default: "Live2D"). The avatar should appear. Add other Spout sources if needed (e.g., "RouletteWheel").
7.  🎤 **Interact**: Start talking\! The expected flow is:
      * 👂 VAD detects speech.
      * 📝 Whisper transcribes speech to text (check console/UI).
      * 🧠 Text (plus context) sent to LLM.
      * 💬 LLM generates a response.
      * 🔊 TTS synthesizes the response audio (potentially with RVC).
      * 🎶 Synthesized audio plays through your speakers/headphones.
      * 📜 Subtitles appear on the Spout output feed.
      * 🎭 Live2D avatar animates (lip sync, emotions).
8.  ⚙️ **Use the Control UI**:
      * Monitor performance metrics.
      * View the live **Chat History**. You can right-click messages to edit or delete them, or insert new messages manually into the history (this won't trigger generation, just adds context).
      * Adjust **TTS** settings (voice, speed, pitch, RVC parameters) on the fly.
      * Control and configure the **Roulette Wheel** if enabled.
      * Toggle features or change input/output devices if applicable.

## <a id="troubleshooting"></a>🛠️ Troubleshooting

<div align="center">
<img src="assets/mascot_hardhat.png" width="150" alt="Mascot with Hardhat">
<p>Having trouble? Here are some common issues and solutions:</p>
</div>

  * **CRITICAL Error: `DllNotFoundException` or `LoadLibrary failed with error 126: The specified module could not be found` (often mentioning `onnxruntime_providers_cuda.dll`, `cublas64_*.dll`, `cudnn64_*.dll`, etc.)**

      * **Cause:** This almost always indicates an incorrect **CUDA or cuDNN installation**, or missing dependencies for the ONNX Runtime CUDA provider. Persona Engine **cannot run** without these.
      * **Solution:** You likely did not follow the specific **manual installation steps for cuDNN using the tarball/zip archive** OR your CUDA installation/PATH setup is incorrect.
        1.  Go back to the **[REQUIRED CUDA + cuDNN Setup Guide](#prereq-cuda)**.
        2.  **Carefully re-do Step 3 (Install cuDNN Library)**. Ensure you downloaded the **TAR or ZIP** version of cuDNN compatible with your CUDA 12.x installation (v9 recommended for CUDA 12.2).
        3.  **Manually copy** the files from the extracted cuDNN `bin`, `include`, and `lib` folders into the corresponding folders within your CUDA Toolkit installation directory (e.g., `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.2\`).
        4.  Ensure the CUDA `bin` directory (e.g., `...\CUDA\v12.2\bin`) is correctly listed in your **System Environment Variables `Path`** (Step 4).
        5.  **Restart your computer** (Step 5) is essential after making these changes.
        6.  Run Persona Engine again and check the console logs meticulously for CUDA/ONNX initialization messages.

  * **Text-to-Speech (TTS) is Silent or Crashes the App**

      * **Cause 1:** `espeak-ng` is not installed or not accessible via system PATH or the configured `Config.Tts.EspeakPath`.
      * **Solution 1:** Install `espeak-ng` (see [Prerequisites Section 3](#prereq-software)). **During installation, ensure you check the box to "Add espeak-ng to the system PATH"**. If you missed this, either reinstall `espeak-ng` and check the box, OR find the `espeak-ng` installation folder (e.g., `C:\Program Files\eSpeak NG`) and put its full path (use double backslashes like `"C:\\Program Files\\eSpeak NG"`) into the `Config.Tts.EspeakPath` setting in `appsettings.json`, then restart the app.
      * **Cause 2:** TTS models (`kokoro` voices) are missing or corrupted.
      * **Solution 2:** Ensure the `Resources/Models/kokoro` folder exists and contains the necessary TTS model files (usually included in the release `.zip`). If building from source, ensure you copied these correctly.

  * **App Crashes on Startup or When Speaking (Whisper/ASR Issue)**

      * **Cause:** The required Whisper ASR models (`.bin` files) are missing or in the wrong location.
      * **Solution:** Confirm you downloaded **both** `ggml-tiny.en.bin` AND `ggml-large-v3-turbo.bin` from the [Whisper Models Release](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models). Make sure these two files are placed **directly** inside the `Resources/Models/` folder (not in a subfolder).

  * **No Response from LLM / LLM Errors in Console or UI**

      * **Cause 1:** Incorrect LLM configuration in `appsettings.json`.
      * **Solution 1:** Double-check `Config.Llm.TextEndpoint` URL. Is it correct and reachable? (Try accessing the base URL in a browser). Is `Config.Llm.TextModel` exactly right? Is `Config.Llm.TextApiKey` correct (or `""` if not needed)?
      * **Cause 2:** The LLM service itself is down or having issues.
      * **Solution 2:** Check the status of your LLM provider (if cloud-based) or ensure your local LLM server (Ollama, etc.) is running correctly and accessible at the specified endpoint.
      * **Cause 3:** Badly formatted `personality.txt` for the connected LLM.
      * **Solution 3:** If using a standard LLM, ensure `personality.txt` was structured based on `personality_example.txt` (obtained from the repo). Some LLMs are very sensitive to prompt formatting. Simplify the prompt to test basic connectivity. Check the console/UI for specific error messages returned *from* the LLM API.

  * **Avatar Not Appearing in OBS / Spout Receiver**

      * **Cause 1:** Persona Engine is not running or failed to initialize Spout (check console for errors).
      * **Solution 1:** Ensure `PersonaEngine.exe` is running and check the console logs for any Spout-related errors during startup, particularly after the CUDA/ONNX checks.
      * **Cause 2:** Spout2 Plugin for OBS is not installed or not loaded correctly.
      * **Solution 2:** Re-download and reinstall the [Spout2 Plugin for OBS](https://github.com/Off-World-Live/obs-spout2-plugin/releases) suitable for your OBS version. Restart OBS.
      * **Cause 3:** Incorrect Spout source configuration in OBS.
      * **Solution 3:** In OBS, remove the existing "Spout2 Capture" source. Add a new one. In its properties, click the "Spout Sender" dropdown. Does the correct Persona Engine sender name (e.g., "Live2D" or "RouletteWheel" as defined in `Config.SpoutConfigs`) appear? If yes, select it. If not, there's likely an issue with Spout initialization in Persona Engine or the OBS plugin.
      * **Cause 4:** Firewall blocking Spout communication (less common locally).
      * **Solution 4:** Temporarily disable your firewall to test. If it works, create specific firewall rules to allow OBS and Persona Engine communication.

### Still Stuck?

<img src="assets/mascot_sigh.png" alt="Mascot Giving Up" width="150" align="right">

  * Check the **console window** and the **Control UI's log/status sections** for detailed error messages - these are often key\!
  * Join our [**Discord Community**](#community)\! Ask for help in the support channels, providing details about:
      * What you were trying to do.
      * What happened (and any specific error messages from the console or UI).
      * Your operating system (Windows version).
      * Your **NVIDIA GPU model**.
      * Which LLM you are trying to connect to.
      * Confirmation you followed the CUDA/cuDNN install guide precisely.

## <a id="use-cases"></a>💡 Potential Use Cases: Imagine the Fun\!

<div align="center">
<img  src="assets/mascot_light.png" width="150" alt="Mascot with Lightbulb">
</div>

  * 🎬 **VTubing & Live Streaming:** An AI co-host, interactive character reacting to chat/events, or fully AI-driven VTuber using models like the included "Aria" or your own.
  * 🤖 **Virtual Assistant:** A personalized, animated desktop companion for tasks or information.
  * 🏪 **Interactive Kiosks:** An engaging, talking guide for museums, trade shows, retail environments, or information booths.
  * 🎓 **Educational Tools:** An AI language practice partner, a historical figure Q\&A bot, or an interactive tutor with a face.
  * 🎮 **Gaming:** Creating more dynamic and conversational NPCs, companions, or even AI opponents in games.
  * 💬 **Character Chatbots:** A more immersive way to interact with fictional characters online.

## <a id="contributing"></a>🙌 Contributing

Contributions are welcome\! If you have improvements, bug fixes, or new features in mind:

1.  Fork the repository.
2.  Create a new feature branch (`git checkout -b feature/YourAmazingFeature`).
3.  Make your changes and commit them (`git commit -m 'Add some AmazingFeature'`).
4.  Push your branch to your fork (`git push origin feature/YourAmazingFeature`).
5.  Open a Pull Request back to the main repository.

Please try to adhere to the existing coding style and conventions. For major changes or new feature ideas, it's often best to discuss them first by opening an Issue on GitHub or chatting on the Discord server. Your help is appreciated\! 😊

-----

*Remember to consult the [Live2D Integration & Rigging Guide](./Live2D.md) for details on preparing custom avatars.*