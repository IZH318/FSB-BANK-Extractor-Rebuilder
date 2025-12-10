# FSB/BANK Extractor & Rebuilder

<div align="center">

| CLI Version (v1.x) | GUI Version (v3.x) |
| :---: | :---: |
| <img width="400" alt="CLI Screenshot" src="https://github.com/user-attachments/assets/a6eca308-23af-4068-ac3a-75543cc6411f"> | <img width="400" alt="GUI Screenshot" src="https://github.com/user-attachments/assets/6b1affa3-e0e6-4234-8154-e6dcbd313405"> |

</div>

<BR>

This program analyzes audio streams from FMOD Sound Bank (`.fsb`) and Bank (`.bank`) files, allows navigation of their contents, and extracts them as Waveform Audio (`.wav`) files. It offers both a Command Line Interface (CLI) version and a Graphical User Interface (GUI) version. <BR> <BR>

**Starting with GUI v3.0.0**, the project has evolved beyond simple extraction to include a **'Rebuild' feature**, allowing users to replace specific audio files and repackage them. Accordingly, the project name has been changed to **FSB/BANK Extractor & Rebuilder**. <BR> <BR>

⚠️ **Note:** This program is a project re-implemented in C++ and C#, inspired by the `fsb_aud_extr.exe` program written by id-daemon on the zenhax.com forum ([Post Link](https://zenhax.com/viewtopic.php@t=1901.html)). <BR> <BR>

---

📢 **Development Status Notice** <BR>
Development of the **C++ (CLI)** and **C# (CLI)** versions is currently **paused**. <BR>
If you require usage in a CLI environment, please use the latest stable release **[v1.1.0](https://github.com/IZH318/FSB-BANK-Extractor-Rebuilder/releases/tag/v1.1.0)**. <BR>
We will provide an update via this README if development on the CLI versions resumes in the future. <BR>

---

<BR>

## 🔍 Key Features and Improvements

- **Common Improvements**

   - **Extended File Handling:**
       - **Bank File Support (.bank):** Directly analyzes and processes internal FSB data included in `.bank` files, in addition to standalone FSB files. (The original program only supported FSB files) <BR> <BR>

   - **Enhanced Output Control:**
       - **Various Output Directory Options:** Flexibly select WAV save locations via command line arguments or GUI options (`-res`, `-exe`, `-o` options).
       - **Auto Sub-folder Generation:** Automatically creates sub-folders based on original filenames to classify and save extracted files systematically.
       - **Improved WAV Filename Generation:** Uses internal Sub-Sound names from the FSB to improve file identification after extraction.
       - **Supports customized output, organized file structure, and efficient workflow.** <BR> <BR>

   - **Robust Error Handling and Verification:**
       - **Verbose Logging:** Detailed logs (via `-v` argument or GUI checkbox) support in-depth analysis and debugging.
       - **Log Levels:** Logs are classified into INFO, WARNING, and ERROR levels for efficient issue identification.
       - **Progress Indicators (CLI & GUI):** Clear status updates via text in CLI and visual progress bars in GUI.
       - **Enhanced debugging, error tracking, and user feedback.** <BR> <BR>

   - **Internationalization Support:**
       - **Full Unicode Support:** Perfect compatibility with multi-language file paths and internal sound names using UTF-8 encoding.
       - **Enhanced Filename Compatibility:** Automatically converts special characters unusable in filenames into compatible forms to prevent file system errors.
       - **Global compatibility, data loss prevention, and broad user support.** <BR> <BR>

   - **Improved Code Quality and Maintainability:**
       - **Modern Languages (C++, C#) & OOP Design:** Designed with Object-Oriented Programming for extensibility.
       - **Automatic Resource Management (RAII/using):** Prevents memory leaks and improves stability.
       - **Latest FMOD Engine:** Utilizes the latest FMOD Engine (CLI: v2.03.06, GUI: v2.03.11) to leverage the newest features and improvements.
       - **Enhanced code quality, easy maintenance, increased program stability, and utilization of modern FMOD engine features.** <BR> <BR>

- **CLI Version Improvements**

   - **Output Control via Command Line Options:** Flexible output directory selection via `-res`, `-exe`, and `-o` arguments.
   - **Text-based Progress Indicator:** Provides text-based progress updates when processing large files.
   - **Enhanced command line control, improved CLI feedback, and optimization for CLI workflows and automation.** <BR> <BR>

- **GUI Version Improvements**

   - **Audio Rebuild & Repacking:**
       - Added the ability to **replace** specific audio within `.bank` or `.fsb` files with other audio files (WAV, MP3, OGG, etc.) provided by the user.
       - Integrates with the official FMOD command-line tool `fsbankcl.exe` to stably generate FSB files compatible with the original.
       - Introduces a **Binary Search algorithm** to automatically find the optimal compression quality that does not exceed the original file's data size.
   - **Real-time Audio Analyzer (Tools Menu):**
       - **Comprehensive Visualization:** Renders Waveform, Spectrum, Spectrogram, Vectorscope, and Oscilloscope in real-time.
       - **Precision Metering:** Provides channel-specific RMS/Peak metering, Clipping counters, and DC Offset.
       - **Loudness Analysis:** Measures LUFS (Integrated/Short-term/Momentary) and True Peak (dBTP) based on broadcasting standards like EBU R 128.
   - **Mass File Management and Index Tools:** 
       - Supports **Jump to Index** to instantly move to a specific number (Index) among thousands of audio files, and **Select Range** (e.g., `100-200`) to check multiple files at once, maximizing workflow efficiency.
   - **Audio Preview System:** 
       - Instantly Play, Pause, and Stop audio within the program without needing to extract.
       - Features a **Seek Bar**, **Volume Control**, and **Force Loop** options for precise audio data verification.
   - **Strings Bank Integration:** 
       - Automatically detects or allows manual loading of `.strings.bank` files to convert encrypted GUIDs (e.g., `{a1b2...}`) into developer-assigned **real event names**.
   - **Real-time Search and Advanced Navigation:** 
       - Equipped with an optimized search engine to quickly filter and display only matching items among thousands of audio nodes.
       - Using **Open File Location** in search results instantly moves to the original location within the tree structure to identify the file's context.
   - **Integrated Details Panel:** 
       - Eliminates the inconvenience of popup windows by immediately displaying metadata such as Format (PCM, ADPCM, etc.), Channels, Bitrate, Loop points, GUID, and original path in the right panel when an item is clicked.
   - **Data Management and Export:** 
       - **CSV Export:** Export the structure and detailed properties of all currently loaded files to a CSV file.
       - **Checkbox-based Extraction:** Select specific items via checkboxes to batch extract only what you need.
       - Flexible extraction path options: 'Same as original', 'Custom fixed path', or 'Ask every time'.
   - **User Convenience & Workflow:**
       - **Drag & Drop:** Easily load files and folders by dragging them from Explorer into the program window.
       - **Shortcuts:** Full support for shortcuts like Open File (`Ctrl+O`), Search (`Ctrl+F`), and Extract (`Ctrl+E`).
   - **Performance Optimization:** 
       - **Parallel Scanning & Async Processing:** Fully adopted `Parallel.ForEach` and `async/await` to analyze large quantities of files/folders at high speed without UI freezing.
       - **Low-level Binary Parsing:** Directly analyzes FSB headers alongside FMOD API to ensure more accurate audio data offsets and lengths for specific compression formats.

<BR>

## 🔄 Update History

### v3.0.0 (2025-12-09) - GUI Only
The v3.0.0 update focuses on adding key new features and significantly improving the internal code structure. **(No changes to CLI versions)**

-   #### **✨ New Features**
    -   **Audio Rebuild & Repacking System:**
        -   Added functionality to replace specific audio in `.bank` or `.fsb` files with user-provided audio files (WAV, MP3, etc.).
        -   Uses the official FMOD tool `fsbankcl.exe` to generate FSB files compatible with the original.
        -   **Binary Search Algorithm:** Automatically finds the optimal compression quality (Vorbis) that fits within the original data size limit, preventing file corruption.
        -   (Note) Applies **Zero-Padding** automatically if the new file is smaller, perfectly maintaining the original file structure.
    -   **Real-time Audio Analyzer:**
        -   Added a tool to analyze playing sounds in detail. (Tools menu)
        -   **Visualizations:** Displays Waveform, Spectrum, Spectrogram, Vectorscope, and Oscilloscope.
        -   **Channel Metering:** Provides detailed statistics per channel, including RMS/Peak levels, Clipping counter, and DC Offset.
        -   **Loudness Analysis:** Measures Integrated/Short-term/Momentary LUFS and True Peak (dBTP) based on major broadcast standards (e.g., EBU R 128).

-   #### **🚀 Convenience Improvements**
    -   **Extraction Path Options:** Added options to choose extraction location: 'Same as original', 'Custom fixed path', or 'Ask every time'.
    -   **Audio Length Validation:** Added a warning feature during rebuild if the replacement audio is longer than the original, which could cause playback errors (cutting off, loop breaking) in-game. Provides an option to **force save as a standalone file (.fsb)** if size is exceeded.
    -   **Context Menu Improvements:** Reorganized the right-click menu to show only relevant options (Extract, Rebuild, Copy GUID, etc.) based on the selected item type.

-   #### **🛠️ Internal Structure Improvements**
    -   **Async/Await Implementation:** Converted time-consuming I/O operations like file loading, extraction, and rebuilding to asynchronous methods, preventing UI freezes during large tasks.
    -   **Code Modularity:** Separated UI logic into distinct forms (`IndexToolForm`, etc.) and applied polymorphism to the `NodeData` class to improve code structure for future maintenance.
    -   **Timer Logic Update:** Switched to a high-precision background timer (`System.Threading.Timer`) to optimize FMOD engine updates and UI refreshing, reducing system load.

-   #### **⚡ Performance & Stability**
    -   **Low-level Binary Parsing:** Modified to read FSB headers directly instead of relying solely on the FMOD API, ensuring more accurate audio data offsets and lengths for certain compression formats.
    -   **Error Handling & Logging:** Improved exception handling to automatically generate detailed error logs with stack traces. Detailed error log files are now **automatically created upon error**, regardless of the Verbose option.

<BR>

<details>
<summary>📜 Previous Updates - Click to Expand</summary>
<BR>
<details>
<summary>v2.1.0 (2025-11-26) - GUI Only</summary>
Reflecting user requests and feedback, features have been added to **maximize the efficiency of managing large numbers of audio files**. **(No changes to CLI versions)**

-   #### **🔧 Index Tools**
    -   **Sub-Sound Index Support:** You can now see the internal index number of each audio file in the file list.
    -   **Range Selection:** Without needing to check manually one by one, you can select (Check) hundreds of files at once using range input like `100-200` or comma separation like `10, 20`.
    -   **Jump to Index:** Entering a specific number (Sub-Sound Index) will scroll to and focus on that audio file immediately.
    -   **Smart Input Detection:** If the input contains symbols like `,` or `-`, it automatically switches to **Select Range** mode; if only numbers are entered, it switches to **Jump to Index** mode, reducing unnecessary clicks.

-   #### **🔎 Search Enhancements**
    -   **Open File Location:** Right-clicking an item in the search result list and selecting `Open File Location` switches to the Tree View, expands the path to the actual file, and highlights it.
    -   **Consistent Menu UI:** The context menu in search results has been reorganized to match the main Tree View (Extract, Copy, etc.) for a unified User Experience (UX).

-   #### **🛠 Other Improvements**
    -   **Improved Safety:** Enhanced exception handling to display guidance messages when attempting invalid operations on empty containers or parent nodes without audio files.
    -   **Help Updated:** Descriptions for new features (Index Tools, Context Menu) have been added to the Help (F1).
</details>

<details>
<summary>v2.0.0 (2025-11-25) - GUI Only</summary>
The GUI version has been revamped from a simple 'extractor' to a comprehensive <b>'FMOD Audio Analysis Tool'</b>. <b>(No changes to CLI versions)</b><BR><BR>

-   #### **🖥️ Interface and Experience**
    -   **Structure Explorer Introduced**: Replaced the flat list view with a tree view interface that perfectly visualizes the internal hierarchy of FMOD Banks.
    -   **Integrated Main Window**: Integrated detailed information (formerly separate Details Form) into the right panel of the main window for simultaneous navigation and information viewing.
    -   **Icon System**: Applied specific icons for files, folders, events, parameters, and audio nodes to improve visibility.
    -   **Enhanced Status Bar**: Displays the currently processing filename, overall progress, elapsed time, and volume status in real-time.

-   #### **🔊 Audio Playback and Control**
    -   **In-App Player**: Preview sounds directly via the FMOD engine without extraction.
    -   **Playback Control**: Supports Play/Pause/Stop buttons and seeking via a Seek Bar.
    -   **Loop Support**: Test loop behavior using the `Force Loop` option if loop points exist in the source file.
    -   **Auto-Play**: Automatically plays audio upon clicking an item when `Auto-Play on Select` is enabled.

-   #### **💾 Data Processing and Extraction**
    -   **Strings Bank Support**: Added mapping logic to restore obfuscated GUIDs to actual event names by loading `.strings.bank` files. (Supports manual load menu).
    -   **CSV Export**: Added functionality to save detailed info (file list, path, format, length, GUID, etc.) as Excel-compatible CSV files.
    -   **Enhanced Selective Extraction**: Extract only files selected via checkboxes or extract only search results.

-   #### **⚡ Performance and Optimization**
    -   **Parallel Loading System**: Applied `Parallel.ForEach` multi-threading for folder loading to drastically reduce analysis time for thousands of files.
    -   **Search Optimization**: Improved response speed for search input.
    -   **Memory Leak Prevention**: Strengthened the cleanup process to forcibly release FMOD system resources and clear temporary resources upon program exit (`OnFormClosing`).
    -   **FMOD Studio API Integration**: Upgraded the engine to use Studio API alongside the Core API to analyze event structures in Bank files.

-   #### **⌨️ Convenience Features**
    -   **Shortcuts**: `Ctrl+O` (Open File), `Ctrl+Shift+O` (Open Folder), `Ctrl+E` (Extract Checked), `Ctrl+Shift+E` (Extract All), `Ctrl+Shift+C` (Export CSV), `Ctrl+F` (Search), `F1` (Help).
    -   **Context Menu**: Right-click tree nodes to access Play, Stop, Extract, and Copy Name/Path/GUID options.

</details>

<details>
<summary>v1.1.0 (2025-11-18)</summary>
This update focused on preventing data loss during file extraction and significantly improving the organization of extracted files.

-   #### **✨ New Features**
    -   **FMOD Tag-based Auto Folder Generation**: Reads "language" tags included in FMOD sound files to automatically create sub-folders matching language codes (e.g., 'EN', 'JP') and saves files there. This allows for more systematic management of multi-language audio.
-   #### **🛠️ Improvements and Fixes**
    -   **File Overwrite Prevention**: Previously, if multiple sub-sounds had the same name within a single FSB/BANK file, files would be overwritten, causing data loss. Now, numeric suffixes like `_1`, `_2` are automatically appended to ensure all sounds are safely extracted with unique filenames.
    -   **Extraction Logic Refactoring**: Refactored filename generation and path handling logic to increase stability and robustly support new features (tag-based folder creation, overwrite prevention).
</details>

<details>
<summary>v1.0.0 (2025-02-19)</summary>
   
-   #### **Misc**
    -   Initial release of `FSB/BANK Extractor`.

</details>
</details>

<BR>

## 💾 Download <BR>
| Program                                | URL                                                | Required | Remarks                                                                                        |
|----------------------------------------|----------------------------------------------------|----------|------------------------------------------------------------------------------------------------|
| `.NET Framework 4.8`             | [Download](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)   | Optional | ◼ (Install if errors occur) For GUI use |
| `Visual Studio 2022 (v143)`            | [Download](https://visualstudio.microsoft.com/)   | Optional | ◼ (Developers only) For Solution/Project work |
| `FMOD Engine API`             | [Download](https://www.fmod.com/download#fmodengine)   | Optional | ◼ (Developers only) For building source; `api` folder and libraries (`fmod.dll`, etc.) from FMOD SDK are required. |

<BR>

## 🛠️ Development Environment

**[ Common ]**
1. **OS: Windows 10 Pro 22H2 (x64)** <BR>
2. **IDE: Visual Studio 2022 (v143)** <BR> <BR>

**[ FMOD File Setup for Project Build ]**
- This repository does not contain FMOD API source code or binary files, in accordance with FMOD licensing policies.
- To successfully build the project, you must manually download the FMOD Studio API and copy the required files to each project folder. <BR> <BR>

1.  **Download FMOD Studio API:**
    -   Download and install `FMOD Engine (v2.03.06)` or `FMOD Engine (v2.03.11)` via the link in the 'Download' section above. <BR>
    -   (Default install path: `C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows`) <BR> <BR>

2.  **Copy Required Files:**
    -   Locate the files listed below in the FMOD installation directory and copy them into each `FSB_BANK_Extractor\` project folder. <BR> <BR>

    | Filename | Source Path (Based on FMOD Install Folder) | `CS` | `CS_GUI` |
    |---|---|:---:|:---:|
    | **fmod.cs** | `api\core\inc` | O | O |
    | **fmod_dsp.cs** | `api\core\inc` | O | O |
    | **fmod_errors.cs** | `api\core\inc` | O | O |
    | **fmod_studio.cs** | `api\studio\inc` | | O |
    | **fmod.dll** | `api\core\lib\x86` | O | O |
    | **fmodL.dll** | `api\core\lib\x86` | O | |
    | **fmodstudio.dll** | `api\studio\lib\x86` | | O |
    | **fsbankcl.exe** | `bin` | | O |
    | **libfsbvorbis64.dll** | `bin` | | O |
    | **opus.dll** | `bin` | | O |
    | **Qt6Core.dll** | `bin` | | O |
    | **Qt6Gui.dll** | `bin` | | O |
    | **Qt6Network.dll** | `bin` | | O |
    | **Qt6Widgets.dll** | `bin` | | O |
    <BR>

3.  **Confirm Final Folder Structure:**
    -   If all files are copied correctly, the key file structure should look like this:
    -   (The list below omits some auto-generated files like `*.Designer.cs`, `*.resx`, and `bin`/`obj` folders for clarity, focusing on `FSB_BANK_Extractor_Rebuilder_CS_GUI`.) <BR> <BR>

    ```
    FSB_BANK_Extractor_Rebuilder_CS_GUI/
    │
    ├─ App.config
    ├─ packages.config
    │
    ├─ FSB_BANK_Extractor_CS_GUI.csproj
    ├─ FSB_BANK_Extractor_CS_GUI.cs
    ├─ AudioAnalyzerForm.cs
    ├─ HelpForm.cs
    ├─ IndexToolForm.cs
    ├─ RebuildOptionsForm.cs
    ├─ Program.cs
    │
    ├─ FMOD_LICENSE.TXT
    ├─ unboxing_Edit.ico
    │
    ├─ # FMOD C# Wrapper Files (Must be copied)
    ├─ fmod.cs
    ├─ fmod_dsp.cs
    ├─ fmod_errors.cs
    ├─ fmod_studio.cs
    │
    ├─ # FMOD Runtime Binaries (Must be copied)
    ├─ fmod.dll
    ├─ fmodstudio.dll
    │
    ├─ # FMOD Bank Tool & Dependencies (Must be copied)
    ├─ fsbankcl.exe
    ├─ libfsbvorbis64.dll
    ├─ opus.dll
    ├─ Qt6Core.dll
    ├─ Qt6Gui.dll
    ├─ Qt6Network.dll
    └─ Qt6Widgets.dll
    ```

**[ C++ CLI and C# CLI Versions ]**
- **API: FMOD Engine (v2.03.06)** <BR>
- Desktop development with C++ workload required <BR>
- C++ Compiler set to ISO C++17 Standard <BR>
- .NET desktop development workload required <BR>
- Windows SDK Version 10.0 (Latest installed version) <BR> <BR>

**[ C# GUI Version ]**
- **API: FMOD Engine (v2.03.11)**
- **Required NuGet Packages:**
  - **Newtonsoft.Json:** This project uses the Newtonsoft.Json package. It is **automatically installed** when building the solution for the first time in Visual Studio.
  - If a build error occurs, right-click the solution in **Solution Explorer** and select **'Restore NuGet Packages'**, or use `Update-Package -reinstall` in the **Package Manager Console**.
- .NET desktop development workload required
- C# Compiler targeted for .NET Framework 4.8

<BR>

## ⏩ Usage

**[ ===== FSB_BANK_Extractor_CLI (C++ and C# CLI Versions) ===== ]**

![Capture_2025_02_19_13_50_51_945](https://github.com/user-attachments/assets/a6eca308-23af-4068-ac3a-75543cc6411f) <BR> <BR>

**1. Run Command Prompt (cmd.exe) or PowerShell.** <BR> <BR>

**2. Navigate to the directory where the program is located.** <BR>  Use the `cd <program_file_path>` command (e.g., `cd D:\tools\FSB_BANK_Extractor`) <BR> <BR>

**3. Execute the program by entering the following command**: <BR>

   - **Basic Usage**: `program.exe <audio_file_path>` <BR>
   
   - **Usage with Options**: `program.exe <audio_file_path> [Options]` <BR>
   
       - **※ `program.exe` refers to the C++ CLI exe file or C# CLI exe file.** <BR>
           - C++ Version: `FSB_BANK_Extractor_CPP_CLI.exe` <BR>
           - C# Version: `FSB_BANK_Extractor_CS_CLI.exe` <BR> <BR>

   - `<audio_file_path>`: **Required**, Enter the path of the FSB or Bank file to process. <BR>
     You must enter the **path to the FSB or Bank file**. <BR>
     (* Example: `C:\sounds\music.fsb` or `audio.bank` *) <BR> <BR>

   - `[Options]`: **Optional**, You can selectively use the following options as needed. Each option is added after `<audio_file_path>`, separated by spaces. <BR>
     - `-res`: **Saves WAV files in the same folder as the FSB/Bank file.** (Default option; behaves like `-res` if omitted) <BR>
       **Usage Example**: `program.exe audio.fsb -res` (* `-res` can be omitted, same as `program.exe audio.fsb` *) <BR>

     - `-exe`: **Saves WAV files in the same folder as the program executable.** <BR>
       **Usage Example**: `program.exe sounds.fsb -exe` <BR>

     - `-o <output_directory>`: **Saves WAV files in a user-specified folder.** You must enter the path for the folder to save WAV files in `<output_directory>`. <BR>
       **Usage Example (Absolute Path)**: `program.exe voices.bank -o "C:\output\audio"` <BR>
       **Usage Example (Relative Path)**: `program.exe effects.fsb -o "output_wav"` <BR>

     - `-v`: **Enables Verbose Logging.** <BR>
       **Usage Example**: `program.exe music.bank -v` <BR> <BR>

   - **[ 💡 Tips ]**
     - **Default Option**: If you run `program.exe <audio_file_path>` without options, the `-res` option is applied. <BR>
     - **Select Only One Output Folder Option**: The `-res`, `-exe`, and `-o <output_directory>` options **cannot be used simultaneously**. <BR>
     - **Combine with Verbose Logging**: The `-v` option **can be used together** with output folder options. <BR>
     - **-h or -help Option**: Enter `program.exe -h` or `program.exe -help` to view help. <BR> <BR> <BR>



**[ ===== FSB_BANK_Extractor_CS_GUI (C# GUI Version) ===== ]**

<img width="786" height="593" src="https://github.com/user-attachments/assets/6b1affa3-e0e6-4234-8154-e6dcbd313405" /> <BR> <BR>

**1. Run the `FSB_BANK_Extractor_CS_GUI.exe` file.** <BR> <BR>

**2. GUI Operation**:

   - **Loading Files and Folders**:
      - Click **`File` > `Open File...`** or **`Open Folder...`** in the top menu to load files.
      - Alternatively, **Drag and Drop** FSB/Bank files from Windows Explorer onto the program window.
      - **[ 💡 Note ]** If filenames appear as encrypted GUIDs, load a `.strings.bank` file along with them, or use the **`File` > `Load Strings Bank (Manual)...`** menu. <BR> <BR>

   - **Navigation and Preview**:
      - **Structure Explorer**: Check the internal actual hierarchy (Events, Buses, Audio) of the Bank in the left Tree View.
      - **Search Filter**: Enter text in the top **Search** bar to filter the list and show only matching items. Right-clicking an item in the search results and selecting **`Open File Location`** moves to the original location in the Tree View, and allows for **immediate extraction or rebuilding**.
      - **Index Tools**: Right-click an FSB/Bank node and run **`Index Tools...`** to jump to a specific index number (`Jump to Index`) or check multiple items at once by specifying a range (`Select Range`, e.g., `100-200, 305`).
      - **Details**: Click an item to view real-time information such as format, channels, and loop points in the right **Details** panel.
      - **Audio Playback**: Use the `Play(▶)`, `Stop(■)` buttons, seek bar, volume slider, and `Loop` checkbox in the bottom panel to verify sounds before extraction. If `Auto-Play` is checked, audio plays automatically upon selection.
      - **Data Copy**: **Right-click** items in the Tree View or Search Results to easily copy the Name (`Copy Name`), Full Path (`Copy Path`), or GUID (`Copy GUID`) to the clipboard.
      - **Tree View Control**: Right-click anywhere and use **`Expand All`** or **`Collapse All`** to open or close all folders at once. <BR> <BR>
   
   - **Audio Rebuild (Replace)**:
      - <img width="386" height="253" alt="image" src="https://github.com/user-attachments/assets/f8460282-065c-4baa-8a16-24d8e7698059" />
      - **Start Rebuild**: **Right-click** the audio file you want to replace and select **`Rebuild Sound with fsbankcl...`**.
      - **Select File & Options**: Choose the new audio file (WAV, MP3, etc.) and specify the compression format (Vorbis, FADPCM, PCM) in the options window.
      - **Auto-Optimization (Vorbis Only)**: The program **automatically optimizes** the compression quality to fit the original file's data size, ensuring safe replacement without corrupting the file structure. <BR> <BR>

   - **File Extraction**:
      - **Set Extraction Path**: Select the default save location from the combo box at the bottom right: 'Same as source file', 'Custom path', or 'Ask every time'.
      - **Selective Extraction**: Check the **checkboxes** of desired items in the **Structure Explorer** (Tree View) or **Search Result List**. Then click **`File` > `Extract Checked...`** to specify a save folder. (Shortcut: `Ctrl + E`)
      - **Extract All**: Click **`File` > `Extract All...`** to extract all currently loaded items at once. (Shortcut: `Ctrl + Shift + E`) <BR> <BR>

   - **Analysis Tools & Other Options**:
      - **Real-time Audio Analyzer**:
         - <img width="706" height="513" alt="image" src="https://github.com/user-attachments/assets/b17f9845-60f5-46ec-ba9c-f0e41239b235" />
         - Click **`Tools` > `Audio Analyzer...`** in the top menu to open the analysis window. You can check professional data such as Waveform, FFT Spectrum, **LUFS**, and **True Peak (dBTP)** in real-time during playback.
      - **CSV Export**: Save the file list as an Excel-compatible file via **`File` > `Export List to CSV...`**. (Shortcut: `Ctrl + Shift + C`)
      - **Verbose Logging**: Enable the **`Verbose Log`** checkbox at the bottom to save detailed logs of the extraction or rebuild process to a file.
      - **Help & Info**: Check the full feature description via **`File` > `Help`** (or `F1`), and view version info in **`File` > `About`**. <BR>

<BR>

## ⚖️ License

- **FMOD**
   - This project was created for personal, non-commercial use and includes the FMOD Engine, which is subject to the **FMOD Engine License Agreement** provided by Firelight Technologies Pty Ltd.
   
   - The full text of the **FMOD Engine License Agreement** for this project is included in the **FMOD_LICENSE.TXT** file.
   
   - **Please refer to the FMOD_LICENSE.TXT file for the specific terms and conditions of the FMOD Engine license applicable to this project.**
   
   - General information about FMOD licensing can be found on the official FMOD website ([FMOD Licensing](https://www.fmod.com/licensing)) and in the general **FMOD End User License Agreement (EULA)** ([FMOD End User License Agreement](https://www.fmod.com/licensing#fmod-end-user-license-agreement)).
   
   - **Key points regarding the use of the FMOD Engine in this project (Summary - see FMOD_LICENSE.TXT for details):**
     
      - **License:** The **FMOD_LICENSE.TXT** file contains the definitive license terms for the FMOD Engine in this project.
      - **Non-Commercial Use:** This project may be used only for personal, educational, or hobby purposes, and is licensed for non-commercial use under the terms of the attached **FMOD_LICENSE.TXT**. It cannot be used for commercial purposes, revenue generation, or any form of monetary gain.
      - **Attribution (When Distributing the Program):** If you distribute a program built with the FMOD Engine for non-commercial purposes permitted by the license, you must include the "FMOD" and "Firelight Technologies Pty Ltd." attribution within the program as specified in the general FMOD EULA and **FMOD_LICENSE.TXT** file.
      - **Redistribution Restrictions:** Redistribution of FMOD Engine components in this project follows the terms specified in the **FMOD_LICENSE.TXT** file and the general FMOD EULA. Generally, only runtime libraries are allowed for redistribution in a non-commercial context. <BR> <BR>

- **Icons Used in This Project:**

  - **Icon Name:** Unboxing icons
   - **Creator:** Graphix's Art
   - **Source:** Flaticon
   - **URL:** https://www.flaticon.com/free-icons/unboxing <BR> <BR>

- **Project Code License**

   - The code for this project, excluding the FMOD Engine and the icons themselves, is licensed under the **GNU General Public License v3.0**.

<BR>

## 👏 Special Thanks To & References

-   **[FMOD FSB files extractor (through their API)](https://zenhax.com/viewtopic.php@t=1901.html)**
    -   The `fsb_aud_extr.exe` created by **id-daemon** on the zenhax.com forum was a crucial reference that provided the core idea for this tool.
-   **[Redelax](https://github.com/Redelax)**
    -   Reported the issue where data was overwritten and lost when filenames were duplicated. Thanks to this, we were able to improve the program to be more stable.
-   **[TigerShota](https://github.com/TigerShota)**
    -   Suggested range selection based on Sub-Sound Index, the ability to jump directly to an index, and the feature to open file locations from search results.
-   **[immortalx74](https://github.com/immortalx74)**
    -   Suggested the necessity of multi-selection for bulk file processing. We referred to this feedback to implement the Sub-Sound Index range selection feature.
    -   Suggested the core principle of the Rebuild feature for replacing audio data. The idea that "the replacement sound must be the same size or smaller than the original, and replaced at the binary level while maintaining the original index" provided the decisive basis for implementing the current stable Rebuild system.
