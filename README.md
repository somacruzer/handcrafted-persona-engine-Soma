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
An AI-powered interactive avatar engine using Live2D, Large Language Models (LLMs), Automatic Speech Recognition (ASR), Text-to-Speech (TTS), and Real-time Voice Cloning (RVC). Designed primarily for VTubing, streaming, and virtual assistant applications. Let's bring your character to life! ✨
</p>

<img src="assets/header.png" alt="Persona Engine" height="450">

<h2>💖 See it in Action! 💖</h2>
<p>Watch Persona Engine work its magic:</p>
<!-- TODO: Replace with actual URL and Thumbnail -->
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

## 🌸 Overview: What's Inside?

Persona Engine listens to your voice 🎤, thinks using powerful AI language models 🧠 (guided by a personality you define!), speaks back with a synthesized voice 🔊 (which can even be cloned!), and animates a cute Live2D avatar 🎭 accordingly. The visuals can easily pop into your streaming software like OBS Studio using Spout!

> **❗ Important Note on AI Model and Personality:**
> Persona Engine shines brightest with a **specially fine-tuned Large Language Model (LLM)**. This model understands the engine's unique way of sending info, leading to more natural, in-character chats!
>
> While you *can* use standard OpenAI-compatible models (like those from Ollama, Groq, OpenAI, etc.) by carefully editing the `Resources/Prompts/personality.txt` file, it might take some extra effort (prompt engineering magic!) to get perfect results.
>
> ✨ We've included a helpful template! Look for `Resources/Prompts/personality_example.txt` as a starting point if you're using a standard model. Copy its ideas or content into your *actual* `personality.txt` file.
>
> The fine-tuned model is currently being tested. Want to try it or see a demo? Hop into our Discord! 😊

## ✨ Features Galore!
<div align="center">
<img src="assets/mascot_wand.png" width="150" alt="Mascot with Wand">
</div>

*   🎭 **Live2D Avatar Integration:** Loads and renders your Live2D models. (Lip-sync/animation triggers planned!)
*   🧠 **AI-Driven Conversation:** Connects to OpenAI-compatible LLM APIs (run locally or in the cloud!), using `personality.txt`. Optimized for our special fine-tuned model (see [Overview](#🌸-overview-whats-inside)).
*   🗣️ **Voice Interaction:** Listens via microphone (NAudio/PortAudio), detects speech with Silero VAD, and understands you with Whisper ASR (Whisper.net).
*   🔊 **Advanced Text-to-Speech (TTS):** A fancy pipeline (normalization, segmentation, phonemization, ONNX synthesis) brings text to life, supporting custom `kokoro` voices.
*   👤 **Real-time Voice Cloning (RVC):** Integrates RVC ONNX models to make the TTS voice sound like someone specific in real-time!
*   📜 **Customizable Subtitles:** Show what's being said with lots of options to make it look just right.
*   👀 **Screen Awareness (Experimental):** Optional Vision module lets the AI "see" application windows.
*   🎡 **Interactive Roulette Wheel (Experimental):** Spin a fun wheel on screen!
*   📺 **Streaming Output (Spout):** Sends the visuals directly to OBS or other Spout-compatible software. No window capture needed!
*   🎶 **Audio Output:** Plays generated speech clearly (via PortAudio).
*   ⚙️ **Configuration:** Easy setup via `appsettings.json` and a built-in UI editor.
*   🤬 **Profanity Detection:** Basic + ML-based filtering options.

<div align="center">
<br>
<h2>💬 Join Our Community! 💬</h2>
<p>
Need help getting started? Have questions or brilliant ideas? 💡 Want to see a live demo, test the special fine-tuned model, or chat directly with a Persona Engine character? Having trouble converting RVC models? Come say hi on Discord! 👋
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

## ⚙️ Architecture / How it Works

It's like a little assembly line for bringing characters to life!

1.  **Input:** 🎤 Mic -> 👂 VAD (Detects Speech) -> 📝 ASR (Speech-to-Text) -> (Optional) 👀 Vision (Sees Screen).
2.  **Processing:** 🧠 LLM (Uses Personality from `personality.txt` - ideally the fine-tuned one!) -> 💬 Response -> (Optional) 🤬 Profanity Check.
3.  **Output:** 🔊 TTS (Text-to-Speech) -> 👤 RVC (Optional Voice Clone) -> 🎭 Live2D Animation -> 📜 Subtitles -> 🎶 Audio Playback -> 📺 Spout Visuals.

<div align="center">
<br/>
<img
  src="assets/diagram.png"
  alt="Persona Engine Architecture Diagram"
  width="600"
>
<br/>
<br/>
</div>

## 📋 Prerequisites: Let's Get Ready!

Before we start the magic, let's gather our supplies! Make sure you have everything below.

<div align="center">
<img src="assets/mascot_checklist.png" width="150" alt="Mascot with Checklist">
<p>Make sure you have these ready:</p>
</div>

### 1. System Requirements 🖥️

<details>
<summary><strong>➡️ Click here for detailed system notes...</strong></summary>

*   💻 **Operating System:**
    *   ✅ **Windows (Recommended):** Developed and tested here! Pre-built releases are Windows-only.
    *   ⚠️ **Linux / macOS:** Possible *only* by building from source. Needs extra tech skills for setup (CUDA, Spout alternatives, Audio libs) and is **not officially supported**. Good luck, adventurer!
*   💪 **Graphics Card (GPU):**
    *   ✅ **NVIDIA GPU with CUDA (Strongly Recommended):** Makes AI tasks WAY faster! Essential for smooth ASR, TTS, and RVC. See the **[CUDA & cuDNN Installation Guide](#-installing-nvidia-cuda-and-cudnn-for-gpu-acceleration)** below. Get the latest NVIDIA drivers!
    *   ⚠️ **CPU-Only / Other GPUs:** Might be very slow or unstable. Performance will likely suffer. 🐢
*   🎤 **Microphone:** To talk to your character!
*   🎧 **Speakers / Headphones:** To hear them reply!

</details>

### 2. 💪 Installing NVIDIA CUDA and cuDNN (For Super Speed!)

This step is highly recommended if you have an NVIDIA GPU! It makes the AI bits run much, much faster.

<details>
<summary><strong>➡️ Click here for the CUDA + cuDNN Setup Guide (Windows)...</strong></summary>

1.  **Check GPU Compatibility & Install Driver:**
    *   Make sure your NVIDIA GPU can use CUDA ([NVIDIA CUDA GPUs list](https://developer.nvidia.com/cuda-gpus)).
    *   Get the **latest NVIDIA Game Ready or Studio driver** ([NVIDIA Driver Downloads](https://www.nvidia.com/Download/index.aspx)). Clean install recommended!

2.  **Install CUDA Toolkit:**
    *   Go to the [NVIDIA CUDA Toolkit download page](https://developer.nvidia.com/cuda-toolkit-archive) (using the archive helps match versions if needed, or get the latest from the main [CUDA Toolkit page](https://developer.nvidia.com/cuda-downloads)).
    *   Choose your system settings (Windows, x86_64, version, `exe (local)`).
    *   Download and run the installer. **Express (Recommended)** is usually fine.
    *   Note the install path (like `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y`).

3.  **Install cuDNN Library:**
    *   This helps deep learning go zoom! 🚀
    *   Go to [NVIDIA cuDNN download page](https://developer.nvidia.com/cudnn-downloads) (requires free developer account).
    *   **Very Important:** Download the cuDNN version that **matches your installed CUDA Toolkit version** (e.g., "cuDNN vA.B.C for CUDA X.Y").
    *   Get the "Local Installer for Windows (Zip)" for your matching version.
    *   **Extract the cuDNN zip** somewhere temporary. You'll see `bin`, `include`, `lib` folders.
    *   **Copy the files into your CUDA Toolkit folder:**
        *   Copy contents of cuDNN `bin` -> CUDA Toolkit `bin` (e.g., `C:\...CUDA\vX.Y\bin`)
        *   Copy contents of cuDNN `include` -> CUDA Toolkit `include` (e.g., `C:\...CUDA\vX.Y\include`)
        *   Copy contents of cuDNN `lib\x64` -> CUDA Toolkit `lib\x64` (e.g., `C:\...CUDA\vX.Y\lib\x64`)
        *   *(Replace `vX.Y` with your actual CUDA version number!)*

4.  **Add cuDNN to System Path (Important!):**
    *   Helps Windows find the CUDA stuff reliably.
    *   Search "Environment Variables" in Windows -> "Edit the system environment variables".
    *   Click "Environment Variables..." button.
    *   Under "System variables", find `Path` -> "Edit...".
    *   Click "New" and add these paths (use your CUDA version):
        *   `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\bin`
        *   `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\vX.Y\libnvvp`
    *   Click OK everywhere to save.
    *   **Restart your computer!** This is crucial for changes to take effect.

5.  **Verification (Optional):**
    *   After restart, open Command Prompt (`cmd`) and type `nvidia-smi`. If it shows your GPU info and CUDA version, you're likely good to go! Check Persona Engine's console window when it starts for CUDA messages.

</details>

### 3. 🛠️ Software to Install (Besides CUDA/Drivers)

You need these two little helpers installed *before* running Persona Engine:

*   ✅ **[.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0):** The engine's core framework. Install this system-wide from Microsoft. (It might be included in the `.zip`'s `dotnet_runtime` folder for convenience, but system-wide install is best).
*   ✅ **[`espeak-ng`](https://github.com/espeak-ng/espeak-ng/releases):** Helps the TTS pronounce words correctly (phonemization). **TTS might not work without it!**
    1.  Go to the `espeak-ng` releases page.
    2.  Download the installer (e.g., `espeak-ng-*.msi` for Windows).
    3.  ❗ **Important:** During installation, **check the box to "Add espeak-ng to the system PATH"**. This makes life easier!
    4.  *Alternatively*: If not added to PATH, you *must* put the full path to `espeak-ng.dll` in `appsettings.json` (`Tts.EspeakPath`).

### 4. ❗ Essential Models & Resources (Download Separately!) ❗

The releases include almost everything (TTS bits, VAD, demo avatar, personality templates). But the Whisper models are big, so you need to grab them yourself!

*   🧠 **Whisper ASR Models (Mandatory Download):**
    *   **What:** The AI models that turn your speech 🗣️ into text 📝. Must be **GGUF format**.
    *   You need **both**:
        *   `ggml-tiny.en.bin` (Faster, good for quick tests)
        *   `ggml-large-v3-turbo.bin` (More accurate, recommended!)
    *   **Where to get them:** Download both `.bin` files from the releases page:
        **[➡️ Download Whisper Models Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
    *   **Where they go:** Put **both** downloaded `.bin` files directly into the 📁 `Resources/Models/` folder *after* you unzip Persona Engine.

*   🎭 **Your Live2D Avatar Model (Demo Included / Replaceable):**
    *   **What:** Your character's files (`.model3.json`, textures, motions, etc.).
    *   **Included:** A demo avatar ("Haru") is usually in 📁 `Resources/Live2D/Avatars/Haru/`. The default settings use this.
    *   **To use yours:** Make a new folder in 📁 `Resources/Live2D/Avatars/` (like `MyCutie`), put your model files there. Then, change `Live2D.ModelName` in `appsettings.json` to `"MyCutie"`.

*   📝 **Personality Prompts (Included - `personality.txt` & `personality_example.txt`):**
    *   **What:** Text files that tell the LLM how your character should act! 🧠✨
    *   **`personality.txt`:** The **active** file the engine uses. It's initially set up for the special fine-tuned model (see [Overview](#🌸-overview-whats-inside)).
    *   **`personality_example.txt`:** A **template/starting point** if you're using a **standard** OpenAI-compatible LLM.
    *   ❗ **If using a standard LLM:** You'll probably need to copy ideas/content from `personality_example.txt` into `personality.txt` and then customize `personality.txt` for your character.
    *   **Where it is:** Look in 📁 `Resources/Prompts/`.

*   🔊 **TTS Resources (Included):** Files for speech generation (voices, phonemizers, etc.). Usually in 📁 `Resources/Models/kokoro/`. No need to touch!
*   👂 **VAD Model (Included):** Detects speech (`silero_vad.onnx`). In 📁 `Resources/Models/`.

### 5. Optional: 👤 RVC Models (for Voice Cloning)

*   **What:** Want the TTS to sound like a specific voice? You need a trained RVC model exported to **ONNX format** (usually a `.onnx` file).
*   **Note on `.pth` files:** Standard RVC training gives `.pth` files. These **must be converted to ONNX**. Need help? Ask on Discord! 😊
*   **Where it goes:** Put the `.onnx` file inside the 📁 `Resources/Models/rvc/voice/` folder.

### 6. 🧠 LLM Access (The "Brain")

*   **What:** You need access to a Large Language Model API. This means knowing:
    *   **API Endpoint URL:** The web address of the LLM service (e.g., `http://localhost:11434/v1` for local Ollama+LiteLLM, or a cloud service URL).
    *   **(Optional) API Key:** A secret password if needed (like for OpenAI, Groq).
    *   **Model Name:** The specific AI model (e.g., `gpt-4o`, `llama3`, `your-fine-tuned-model`).
*   **Options:**
    *   **🏠 Local:** Run on your PC (Ollama, LM Studio, etc., maybe via LiteLLM proxy). Needs a beefy PC, especially GPU memory!
    *   **☁️ Cloud:** Use a service (OpenAI, Groq, Anthropic, etc.). May need signup, keys, and might cost money based on use.
*   ❗ **Friendly Reminder:** The default `personality.txt` is tuned for our special model! For standard models, use `Resources/Prompts/personality_example.txt` as inspiration and edit `personality.txt` carefully. Join Discord for info on the fine-tuned model!

### 7. 📺 Spout Receiver (To See Your Avatar!)

*   **What:** Persona Engine **doesn't show the avatar in its own window**. It sends the picture out using **Spout**. You need another app to catch this stream!
*   **Recommendation:** ✅ **OBS Studio** is perfect for this, especially for streaming.
*   **Required Plugin:** Get the **Spout2 Plugin for OBS**: [https://github.com/Off-World-Live/obs-spout2-plugin/releases](https://github.com/Off-World-Live/obs-spout2-plugin/releases)
*   **How:** Install the plugin for OBS. We'll set it up after starting Persona Engine (see [Getting Started](#🚀-getting-started)).


## 🚀 Getting Started: Let's Go!
<div align="center">
<img src="assets/mascot_wrench.png" width="150" alt="Mascot with Wrench">
<p>Ready to bring your character to life? Choose your path:</p>
</div>

### Method 1: Easy Install with Pre-built Release (Recommended for Windows Users 💖)

The simplest way to get started on Windows!

**Step 1: 💾 Download & Extract Persona Engine**

<div align="center">
  <a href="https://github.com/fagenorn/handcrafted-persona-engine/releases" target="_blank">
  <img
  src="assets/download.png"
  alt="Download Latest Release Button"
  width="300"
>
  </a>
  <p><i>(Click the button, grab the `.zip` from the latest release!)</i></p>
</div>

*   Find the downloaded `.zip` (like `PersonaEngine_vX.Y.Z.zip`).
*   Right-click -> "Extract All..." (or use 7-Zip/WinRAR).
*   Choose a location (like `C:\PersonaEngine`). ✅ **Avoid** system folders like Program Files.

**Step 2: 🛠️ Install Prerequisites (If you skipped 'em!)**

*   ✅ Installed **NVIDIA Driver, CUDA, cuDNN**? (See [Prerequisites](#2-💪-installing-nvidia-cuda-and-cudnn-for-super-speed) if using GPU).
*   ✅ Installed **.NET 9.0 Runtime**? (See [Prerequisites](#3-🛠️-software-to-install-besides-cudadrivers)).
*   ✅ Installed **`espeak-ng`** (and added to PATH)? Needed for TTS! (See [Prerequisites](#3-🛠️-software-to-install-besides-cudadrivers)).

**Step 3: 📥 Download and Place Required Whisper Models**

*   Go get the Whisper Models: **[➡️ Download Here ⬅️](https://github.com/fagenorn/handcrafted-persona-engine/releases/tag/whisper_models)**
*   Download **both** `ggml-tiny.en.bin` and `ggml-large-v3-turbo.bin`.
*   Place these two `.bin` files right into the 📁 `Resources/Models/` folder inside your extracted Persona Engine directory.

**Step 4: ⚙️ Quick Configuration (`appsettings.json` & `personality.txt`)**

*   Find `appsettings.json` in the extracted folder. Open with a text editor (Notepad++, VS Code, even Notepad).
*   **Key Settings to Check First:**
    *   `Llm` section:
        *   `TextEndpoint`: Your LLM service URL (e.g., `http://localhost:11434/v1`).
        *   `TextModel`: The LLM name (e.g., `llama3`).
        *   `TextApiKey`: Your API key *if* needed (leave `""` if not).
    *   `Live2D` section:
        *   Check `ModelName`. Default is likely `"Haru"`. If using your own model later, change this to your model's folder name (e.g., `"MyCutie"`).
*   Save `appsettings.json`.
*   **(Optional but Recommended!) Configure Personality (`personality.txt`):**
    *   Go to 📁 `Resources/Prompts/`.
    *   ❗ **Remember:** If **not** using the special fine-tuned LLM, the default `personality.txt` might not work well with standard models.
    *   Open `personality_example.txt`. Use its structure/ideas as a base.
    *   **Copy the good bits from `personality_example.txt` into `personality.txt` (replacing the old content), then edit `personality.txt`** to define your character's personality, rules, etc. This might take some tweaking! (See [Overview](#🌸-overview-whats-inside)).

**Step 5: ▶️ Run Persona Engine!**

*   Double-click `PersonaEngine.exe`.
*   The **Configuration and Control UI** window appears. (The avatar isn't here!). A console window behind it shows logs - check for GPU/CUDA messages!
*   If the LLM is reachable and models are in place, it should start listening. Try talking! 🎤

**Step 6: 📺 View the Avatar (via Spout)**

*   Need a Spout receiver! Let's use OBS.
*   ✅ **Install OBS Studio** if you don't have it.
*   ✅ **Install the Spout2 Plugin for OBS** (link in [Prerequisites](#7-📺-spout-receiver-to-see-your-avatar)).
*   Open OBS.
*   In "Sources", click "+" -> **"Spout2 Capture"**.
*   In properties, click "Spout Sender" dropdown. Select the Persona Engine sender (like "PersonaEngineOutput").
*   Your avatar should appear in OBS! ✨

**Step 7: 🔧 Further Tweaks (Optional)**

*   Adjust more settings in `appsettings.json` (audio devices, TTS voice/speed, subtitles, RVC) or use the UI while it's running.

---

### Method 2: Building from Source (For Devs & Adventurers! 🛠️)

*(Needs updates based on project structure changes - basic steps below)*

1.  **Prerequisites:**
    *   ✅ Install **Git**.
    *   ✅ Install **.NET 9.0 SDK** ([Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0)).
    *   ✅ Install **`espeak-ng`** (+ PATH). (See [Prerequisites](#3-🛠️-software-to-install-besides-cudadrivers)).
    *   ✅ Install **NVIDIA Driver, CUDA, cuDNN** if using GPU. (See [Prerequisites](#2-💪-installing-nvidia-cuda-and-cudnn-for-super-speed)).
    *   ⚠️ Non-Windows: Find equivalent dependencies (PortAudio, Spout/Syphon, etc.). **Unsupported!**
2.  **Clone Repo:**
    ```bash
    git clone https://github.com/fagenorn/handcrafted-persona-engine.git
    cd handcrafted-persona-engine
    ```
3.  **Restore Dependencies:** `dotnet restore`
4.  **Build:**
    ```bash
    # Example Release build:
    dotnet publish PersonaEngine.App -c Release -o ./publish --self-contained false
    # Add `-r win-x64` etc. if needed
    ```
5.  **Go to Output:** The app is in `./publish` (or your `-o` path).
6.  **Place Models & Resources:**
    *   Go to `./publish`.
    *   Create 📁 `Resources/Models`, 📁 `Resources/Live2D/Avatars`, 📁 `Resources/Prompts`.
    *   📥 Download and place **Whisper GGUF models** into 📁 `Resources/Models/`.
    *   🎭 Place your Live2D model into 📁 `Resources/Live2D/Avatars/YourModelName/`.
    *   🔊 Copy TTS (`kokoro`), VAD (`silero_vad.onnx`) models from source repo's `Resources/Models` to output's 📁 `Resources/Models`.
    *   📝 Copy `personality.txt` & `personality_example.txt` from source repo's `Resources/Prompts` to output's 📁 `Resources/Prompts/`.
    *   👤 Place optional RVC ONNX models into 📁 `Resources/Models/rvc/voice/`.
7.  **Configure `appsettings.json` & `personality.txt`:**
    *   Copy `appsettings.json` from `PersonaEngine.App` project folder to `./publish`.
    *   ⚙️ Configure LLM/Live2D settings (like Method 1, Step 4).
    *   ❗ **Crucially:** Edit `personality.txt` in `./publish/Resources/Prompts/`. **If using a standard LLM, copy from `personality_example.txt` into `personality.txt`** and modify heavily for your character!
    *   Set paths if needed (like `Tts.EspeakPath` if espeak isn't in PATH).
8.  **Run:**
    *   Open terminal in `./publish`.
    *   ▶️ Run: `dotnet PersonaEngine.App.dll`
    *   Check console for logs (GPU status!).
    *   📺 Set up Spout receiver (OBS) to view avatar.


## 🔧 Configuration (`appsettings.json`)

<div align="center">
<img src="assets/mascot_cog.png" width="150" alt="Mascot with Wand">
<p>Configuring your engine!</p>
</div>

This file is your control panel! Open it with a text editor.

*   🖼️ `Window`: Size, title, fullscreen.
*   🧠 `Llm`: API keys, models, URLs for text/vision. (❗ Remember `personality.txt` needs to match your LLM choice! Use `_example.txt` for standard models).
*   🔊 `Tts`: Paths for Whisper, TTS models, Espeak (`EspeakPath` if needed). Voice settings (`Voice`, speed), RVC toggle.
*   📜 `Subtitle`: Font, size, colors, position, cool animations!
*   🎭 `Live2D`: Path to avatars folder, `ModelName` (must match your avatar's folder name!).
*   📺 `SpoutConfigs`: Output names/sizes for OBS/Spout.
*   👀 `Vision`: Screen capture settings (experimental).
*   🎡 `RouletteWheel`: Fun wheel settings (experimental).

## ▶️ Usage: Showtime!

1.  ✅ Double-check **Prerequisites**: Got Whisper models? .NET? espeak-ng? CUDA/cuDNN (if GPU)? Spout receiver ready?
2.  ⚙️ Make sure `appsettings.json` has your LLM info & `Live2D.ModelName` is correct.
3.  📝 Ensure 📁 `Resources/Prompts/personality.txt` is set up for your LLM (use `personality_example.txt` as a guide if needed!).
4.  ▶️ Run the app (`PersonaEngine.exe` or `dotnet PersonaEngine.App.dll`).
5.  🖥️ The **Config & Control UI** appears (No avatar here!). Check the console window behind it for logs.
6.  📺 **Set up your Spout receiver** (like OBS + Spout2 Capture source) to see the avatar.
7.  🎤 Speak! The engine should:
    *   👂 Detect speech (VAD).
    *   📝 Transcribe (Whisper).
    *   🧠 Send to LLM (with personality).
    *   💬 Get response.
    *   🔊 Speak response (TTS + maybe RVC).
    *   🎶 Play audio.
    *   📜 Show subtitles (on Spout output).
    *   🎭 Animate avatar (visible via Spout).
8.  ⚙️ Use the UI to monitor or change settings on the fly!

## 💡 Potential Use Cases: Imagine the Fun!

<div align="center">
<img src="assets/mascot_light.png" width="150" alt="Mascot with Wand">
</div>

*   🎬 **VTubing & Live Streaming:** An AI co-host or character reacting live!
*   🤖 **Virtual Assistant:** A friendly desktop helper.
*   🏪 **Interactive Kiosks:** An engaging animated guide for events or info booths.
*   🎓 **Educational Tools:** An AI tutor or language partner with a face.
*   🎮 **Gaming:** Smarter, chattier NPCs or companions.

## 🙌 Contributing

I'd love your help! ❤️

1.  Fork the repo.
2.  Create a branch (`git checkout -b feature/your-cool-idea`).
3.  Make changes.
4.  Commit (`git commit -m 'Added this cool thing'`).
5.  Push (`git push origin feature/your-cool-idea`).
6.  Open a Pull Request!

Please try to match the coding style. Let's chat about big ideas on Discord or in GitHub Issues first! 😊