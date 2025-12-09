/**
 * @file HelpForm.cs
 * @brief Provides a form to display help and license information for the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form uses a TabControl to separate help content by language (Korean and English).
 * Completely revised based on deep code analysis to include every feature and logic.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-09
 */

using System.Windows.Forms;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    public partial class HelpForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the HelpForm class.
        /// </summary>
        public HelpForm()
        {
            InitializeComponent();
            LoadHelpContent();
        }

        /// <summary>
        /// Loads and assigns the help and license content to the form's controls.
        /// </summary>
        private void LoadHelpContent()
        {
            // Define the help content strings for each language based on precise code analysis.
            string helpTextKR =
                "===== FSB/BANK Extractor & Rebuilder (GUI) 사용 설명서 (KR) =====\n\n" +
                "본 프로그램은 FMOD 오디오 엔진(v2.03.11)을 기반으로 .bank 및 .fsb 파일을 분석, 재생, 추출하고,\n" +
                "외부 FMOD 공식 툴('fsbankcl.exe')을 이용하여 오디오를 안전하게 교체(리빌드)하는 통합 솔루션입니다.\n\n\n" +

                "● 1. 파일 불러오기 및 초기화\n" +
                "  - 파일/폴더 열기: 'File' 메뉴를 이용하거나, 탐색기에서 파일/폴더를 프로그램 창으로 드래그 앤 드롭하여 불러옵니다.\n" +
                "  - 재귀적 탐색: 폴더를 불러올 경우, 모든 하위 폴더를 포함하여 .bank와 .fsb 파일을 검색합니다.\n" +
                "  - 병렬 처리 분석: 다중 스레딩(최대 4개)을 활용하여 대규모 파일 목록을 신속하게 분석합니다.\n" +
                "  - Strings Bank 자동 로드: 로딩 시, 같은 폴더 내의 *.strings.bank 파일을 자동으로 먼저 로드하여 이벤트, 버스 등의 실제 이름을 복원합니다.\n" +
                "    (이름이 GUID로 표시될 경우, 'File' -> 'Load Strings Bank (Manual)...' 메뉴로 수동 지정이 가능합니다.)\n" +
                "  - 오류 로그: 파일 분석 중 오류 발생 시, 프로그램 폴더에 상세 내용이 담긴 'ErrorLog_*.log' 파일을 생성합니다.\n\n" +

                "● 2. 구조 탐색 및 스마트 검색\n" +
                "  - 계층 뷰: Bank 파일 내부에 숨겨진 FSB5 데이터 청크를 식별하고, Audio/Event/Bus 등 모든 FMOD 객체를 계층 구조로 시각화합니다.\n" +
                "  - 상세 정보: 우측 패널에서 선택된 항목의 상세 메타데이터(포맷, 채널, 비트 심도, 샘플레이트, 루프 구간, 데이터 오프셋 및 크기, GUID 등)를 실시간으로 표시합니다.\n" +
                "  - 스마트 검색 (Ctrl+F): 검색어 입력 시, 타이핑 랙 방지를 위해 500ms의 입력 지연 처리 후 결과가 리스트 뷰로 전환됩니다.\n" +
                "    * 결과 내 기능: 검색 결과 목록에서 직접 재생, 단일 추출, 리빌드, 데이터 복사 등 모든 주요 기능을 우클릭 메뉴로 사용할 수 있습니다.\n" +
                "    * 원본 위치로 이동: 결과 항목 우클릭 -> 'Open File Location'을 선택하면, 트리 뷰의 원본 위치로 즉시 이동하고 해당 항목이 선택됩니다.\n" +
                "    * 데이터 복사: 우클릭 메뉴를 통해 이름, 전체 경로, GUID(이벤트/뱅크)를 클립보드로 복사할 수 있습니다.\n\n" +

                "● 3. 오디오 재생 시스템\n" +
                "  - 하이브리드 재생 엔진: 순수 오디오 데이터(.fsb)는 FMOD Core API로, 복잡한 로직이 포함된 이벤트는 FMOD Studio API로 재생하여 정확성을 보장합니다.\n" +
                "  - 제어 패널: 재생/일시정지, 정지, 타임라인 탐색 바, 볼륨 슬라이더(0~100%)를 제공합니다.\n" +
                "  - 'Loop' 체크박스: 체크 시, 파일 헤더에 정의된 루프 시작/종료 지점(ms 단위)을 정확히 반영하여 끊김 없는 루프 재생을 지원합니다.\n" +
                "  - 'Auto-Play' 체크박스: 체크 시, 트리 뷰나 검색 결과에서 항목을 클릭하여 선택할 때마다 자동으로 재생을 시작합니다.\n\n" +

                "● 4. 추출 시스템\n" +
                "  - 추출 포맷: FMOD의 내부 포맷(Vorbis, FADPCM 등)에 관계없이, 모든 오디오는 표준 WAV(RIFF) 헤더가 포함된 무손실 파일로 변환되어 저장됩니다.\n" +
                "  - 추출 모드:\n" +
                "    1. 'Extract Checked' (Ctrl+E): 현재 뷰(트리 또는 검색 결과)에서 체크된 항목들만 추출합니다.\n" +
                "    2. 'Extract All' (Ctrl+Shift+E): 현재 로드된 모든 파일의 모든 오디오를 추출합니다.\n" +
                "    3. 단일 추출: 특정 오디오 항목을 우클릭하여 'Extract This Item...'으로 즉시 단일 추출합니다.\n" +
                "  - 저장 경로 옵션: 추출 작업의 기준이 되는 최상위 폴더를 설정합니다.\n" +
                "    1. Same as source file: 원본 .bank/.fsb 파일이 위치한 경로를 기준으로 삼습니다.\n" +
                "    2. Custom path: 사용자가 미리 지정한 고정 폴더를 기준으로 삼습니다.\n" +
                "    3. Ask every time: 매 추출 작업 시마다 기준 폴더를 묻습니다.\n" +
                "  - 지능형 폴더 생성 규칙: 파일 관리를 용이하게 하기 위해, 기준 경로 하위에 다음과 같은 폴더 구조를 자동으로 생성합니다.\n" +
                "    * Bank 내 단일 FSB 인식 시: '[Bank 파일명]' 이름의 폴더를 생성하여 그 안에 저장합니다.\n" +
                "    * Bank 내 다중 FSB(2개 이상) 인식 시: '[Bank 파일명]/[내부 FSB명]' 형식의 중첩 폴더를 생성하여 구분 저장합니다.\n" +
                "  - 'Verbose Log' 체크박스: 체크 시, 추출된 각 파일의 성공 여부, 포맷, 루프 정보, 소요 시간 등이 기록된 TSV(탭 구분 값) 로그 파일을 생성합니다.\n\n" +

                "● 5. 리빌드 및 리패킹\n" +
                "  * 필수 조건: 프로그램 실행 폴더에 FMOD 공식 빌드 툴인 'fsbankcl.exe' 파일이 반드시 존재해야 합니다.\n" +
                "  - 개요: 원본 .bank/.fsb 파일의 구조와 오프셋을 그대로 유지한 채, 특정 사운드 데이터 청크만 교체하는 고급 기능입니다.\n" +
                "  - 자동화 프로세스: 임시 작업 공간 생성 -> 대상 FSB의 모든 서브 사운드 분해 및 메타데이터(manifest.json) 저장 -> 교체할 사운드 덮어쓰기 -> 재압축 -> 원본 파일에 재조립(패치).\n" +
                "  - 오디오 재생 길이(Duration) 검증: 리빌드 시작 전, 교체할 오디오의 재생 시간이 원본보다 길 경우 경고창을 표시합니다. FMOD 이벤트는 원본 길이를 기준으로 타임라인이 설계된 경우가 많으므로, 더 긴 오디오로 교체하면 이벤트가 중간에 끊기거나 루프가 깨지는 등 예기치 않은 동작을 유발할 수 있습니다. 이것은 파일 손상과는 별개로, 게임 내 사운드 동작에 관한 경고이며 사용자가 위험을 인지하고 동의할 경우에만 계속 진행할 수 있습니다.\n" +
                "  - 데이터 크기(Size) 최적화 알고리즘:\n" +
                "    파일 구조 손상을 방지하기 위해, 교체되는 FSB 데이터는 반드시 원본 청크의 파일 크기와 정확히 일치해야 합니다. 이를 위해 아래와 같은 로직이 자동으로 작동합니다:\n" +
                "    1. 품질 조절 가능 포맷 (Vorbis):\n" +
                "       - '이진 탐색' 알고리즘을 사용하여, 원본 용량 제한을 초과하지 않는 최적의 압축 품질(0~100)을 자동으로 찾아냅니다. 이 과정에서 여러 번의 테스트 빌드가 수행될 수 있습니다.\n" +
                "       - 최적 품질로 빌드된 최종 결과물이 원본보다 약간 작을 경우, 부족한 만큼 0으로 채워(패딩) 크기를 정확히 맞춥니다.\n" +
                "    2. 고정 비트레이트 포맷 (PCM, FADPCM):\n" +
                "       - 결과물이 원본보다 작을 경우: 남은 공간을 0으로 채워(패딩) 오프셋을 맞춥니다.\n" +
                "       - 결과물이 원본보다 클 경우: .bank 파일의 후속 데이터 오프셋을 깨뜨려 심각한 손상을 유발할 수 있으므로 강력한 경고창을 표시합니다. 사용자가 위험을 인지하고 동의할 경우에만 진행됩니다.\n" +
                "         (팁: 원본보다 큰 데이터는 독립적인 .fsb 파일로 저장할 때만 안전하게 사용할 수 있습니다.)\n\n" +

                "● 6. 데이터 관리 도구\n" +
                "  - 'Index Tools...' 메뉴 (FSB 컨테이너 노드 우클릭): 수백 개의 오디오가 포함된 FSB 파일에서 특정 항목을 쉽게 찾고 선택할 수 있습니다.\n" +
                "    * 'Jump to Index' 모드: 특정 인덱스 번호로 즉시 스크롤하고 선택합니다.\n" +
                "    * 'Select by Range' 모드: '1-10, 15, 20-30'과 같은 형식으로 복잡한 범위를 지정하여 여러 항목을 한 번에 체크합니다.\n" +
                "  - CSV 내보내기 (Ctrl+Shift+C): 현재 트리 구조의 모든 메타데이터(계층 경로, 인덱스, 포맷, 채널, 루프 정보, GUID 등)를 엑셀에서 열 수 있는 CSV 파일로 상세하게 내보냅니다.\n\n" +

                "● 7. 오디오 분석기\n" +
                "  - 실행: 'Tools' 메뉴 -> 'Audio Analyzer...'를 선택하여 실시간 분석 창을 엽니다. 오디오 재생 시 데이터가 자동으로 연동됩니다.\n" +
                "  - 시각화:\n" +
                "    * 정적 파형: 전체 오디오 파일의 파형을 정규화하여 미리 렌더링하고, 현재 재생 위치를 나타내는 헤드를 실시간으로 추적합니다.\n" +
                "    * FFT 스펙트럼: 오디오의 샘플레이트를 감지하여 나이퀴스트 주파수(샘플레이트/2)까지의 대역폭을 자동으로 설정하여 주파수 분포를 시각화합니다.\n" +
                "  - 채널 통계:\n" +
                "    * 레벨 미터: 각 채널별 실시간 평균 음량(RMS) 및 최대 피크(Peak) 레벨을 제공하며, 0dBFS를 초과하는 디지털 클리핑 발생 시 붉은색 표시와 함께 횟수를 카운트합니다.\n" +
                "    * 상세 통계: 샘플 피크, 최대/최소 RMS, DC 오프셋 등의 수치를 소수점 단위로 정밀하게 추적하여 표시합니다.\n" +
                "  - 라우드니스 및 표준:\n" +
                "    * 표준 규격: EBU R 128, ATSC A/85 등 주요 방송 표준을 선택하여 목표 레벨 준수 여부를 검사할 수 있습니다.\n" +
                "    * 측정 항목: 전체 누적 음량(Integrated), 단기(Short-term), 순간(Momentary) LUFS 및 샘플 간 피크를 고려한 트루 피크(dBTP)를 정밀하게 측정합니다.\n" +
                "    * 'Reset' 버튼: 누적 음량은 재생하는 동안 값이 계속 누적되므로, 새로운 구간을 측정할 때는 'Reset' 버튼을 눌러 초기화해야 합니다.\n" +
                "      (중요: 메인 폼의 볼륨 슬라이더를 조절하면 측정값 왜곡을 방지하기 위해 분석 데이터가 자동으로 리셋됩니다.)\n\n" +

                "● 8. 단축키\n" +
                "  - Ctrl + O : 파일 열기\n" +
                "  - Ctrl + Shift + O : 폴더 열기\n" +
                "  - Ctrl + E : 체크된 항목 추출\n" +
                "  - Ctrl + Shift + E : 모든 항목 추출\n" +
                "  - Ctrl + Shift + C : CSV로 내보내기\n" +
                "  - Ctrl + F : 검색창으로 포커스 이동\n" +
                "  - F1 : 도움말 창 열기\n\n" +

                "● 9. 문제 해결\n" +
                "  - 리빌드 실패: 'fsbankcl.exe' 파일이 없거나, 교체할 오디오 파일의 용량이 너무 커서 최저 압축 품질(0)로도 원본 데이터 청크 크기를 맞출 수 없는 경우 발생합니다.\n" +
                "  - 소리가 나지 않음: FMOD에서 지원하지 않는 코덱이거나 파일이 암호화된 경우일 수 있습니다.\n" +
                "  - FMOD 초기화 실패: 호환되지 않는 FMOD 라이브러리(fmod.dll 등)가 시스템에 있거나 손상된 경우 발생할 수 있습니다.\n\n" +

                "● 10. 라이선스 및 저작권 정보 (License Information)\n" +
                "  - FMOD Engine: 본 프로그램은 FMOD Engine (Core/Studio API) 2.03.11 버전을 사용하였습니다.\n" +
                "    - FMOD Engine 저작권: © Firelight Technologies Pty Ltd.\n" +
                "    - FMOD Engine은 라이선스 계약에 따라 사용되었습니다. 자세한 사항은 FMOD_LICENSE.TXT를 참조하십시오.\n" +
                "  - 아이콘 출처: 'Unboxing icons' created by Graphix's Art - Flaticon.\n" +
                "    - URL: https://www.flaticon.com/free-icons/unboxing\n" +
                "  - 프로그램 라이선스: 본 프로그램의 소스 코드(FMOD 엔진 제외)는 GNU General Public License v3.0 하에 배포됩니다.";

            string helpTextEN =
                "===== FSB/BANK Extractor & Rebuilder (GUI) User Manual (EN) =====\n\n" +
                "This application utilizes the FMOD Engine (v2.03.11) to analyze, play, and extract .bank/.fsb files.\n" +
                "It integrates with 'fsbankcl' to provide a robust audio rebuilding/replacement feature.\n\n\n" +

                "● 1. File Loading & Initialization\n" +
                "  - Open File: Use 'File' menu or Drag & Drop files from Explorer onto the application.\n" +
                "  - Open Folder: Recursively scans a directory for all .bank and .fsb files.\n" +
                "  - Performance: Uses multi-threaded scanning (max 4 threads) for fast loading.\n" +
                "  - Strings Bank: Automatically detects *.strings.bank files to resolve filenames.\n" +
                "    (If detection fails, use 'File' -> 'Load Strings Bank (Manual)...')\n\n" +

                "● 2. Structure Explorer & Smart Search\n" +
                "  - Tree View: Visualizes the hierarchy (Bank -> FSB -> Audio/Events) with icons.\n" +
                "  - Details Panel: Displays Format, Channels, Bit-depth, Loop Points, Data Size, and GUIDs.\n" +
                "  - Smart Search (Ctrl+F): Filters items into a list view after a short typing delay.\n" +
                "    * Actions: You can Play, Extract, and Rebuild directly from search results.\n" +
                "    * Locate: Right-click -> 'Open File Location' jumps to the node in the main tree.\n" +
                "    * Copy Data: Right-click to copy Name, Path, or GUID to clipboard.\n\n" +

                "● 3. Audio Playback System\n" +
                "  - Engine: Supports both Raw FSB streams (Core) and FMOD Studio Events.\n" +
                "  - Controls: Play/Pause, Stop, Seek Bar, and Volume Slider.\n" +
                "  - Loop: When checked, playback adheres to the loop start/end points defined in the file.\n" +
                "  - Auto-Play: When checked, selecting an item in the list starts playback immediately.\n\n" +

                "● 4. Extraction System\n" +
                "  - Format: All audio is converted to standard WAV files with proper RIFF headers.\n" +
                "  - Modes:\n" +
                "    1. Extract Checked (Ctrl+E): Exports only checked items.\n" +
                "    2. Extract All (Ctrl+Shift+E): Exports all loaded audio assets.\n" +
                "    3. Single Extract: Right-click item -> 'Extract This Item...'.\n" +
                "  - Path Options (Combo Box): Sets the base directory for extraction.\n" +
                "    1. Same as source file: Uses the directory where the source file is located.\n" +
                "    2. Custom path: Uses a user-defined fixed directory.\n" +
                "    3. Ask every time: Prompts for the base location on each operation.\n" +
                "  - Automatic Folder Generation:\n" +
                "    Regardless of the path option, subfolders are created automatically:\n" +
                "    * Single FSB detected: Creates a folder named '[FileName]'.\n" +
                "    * Multiple FSBs detected: Creates folders named '[FileName]_[InternalFSBName]'.\n" +
                "  - Verbose Log: Generates a detailed log file with timestamp, success status, and format info.\n\n" +

                "● 5. Rebuilding & Repacking [Advanced]\n" +
                "  * Requirement: 'fsbankcl.exe' must be in the app directory.\n" +
                "  - Concept: Replaces specific audio data while preserving the original Bank structure.\n" +
                "  - Process: Create Workspace -> Extract Subsounds -> Replace -> Repack -> Patch Original.\n" +
                "  - Audio Duration Validation: Before rebuilding, the tool checks if the replacement audio's playback time (duration) is longer than the original. Since FMOD events are often timed to the original audio's duration, using a longer file can cause unexpected in-game behavior like sounds cutting off prematurely or broken loops. This is a warning about gameplay behavior, separate from file corruption, and the process will only continue if the user acknowledges the risk.\n" +
                "  - Data Size Optimization Logic:\n" +
                "    To avoid corrupting the .bank file's structure, the new data chunk must exactly match the original chunk's file size (in bytes). The tool enforces this automatically:\n" +
                "    1. Variable Quality Formats (Vorbis):\n" +
                "       - Uses a 'Binary Search' algorithm to automatically find the highest Quality (0-100)\n" +
                "         that fits within the original size limit.\n" +
                "    2. Fixed Formats (PCM, FADPCM):\n" +
                "       - If Smaller: The remaining space is filled with zeros (Padding) to match offsets.\n" +
                "       - If Larger: A Warning is displayed. You can proceed at your own risk.\n" +
                "         (Warning: Oversized data is unsafe for .bank files, but okay for standalone .fsb files.)\n\n" +

                "● 6. Data Management Tools\n" +
                "  - Index Tools (Right-click Folder): Useful for FSBs with many subsounds.\n" +
                "    * Jump: Scroll to a specific index number immediately.\n" +
                "    * Select Range: Batch check items using syntax like '1-10, 15, 20-30'.\n" +
                "  - CSV Export (Ctrl+Shift+C): Exports all tree metadata (Path, Index, Format, Loop, GUID, etc.)\n" +
                "    to an Excel-compatible CSV file.\n\n" +

                "● 7. Audio Analyzer\n" +
                "  - Launch: Go to 'Tools' -> 'Audio Analyzer...' to open the real-time analysis window.\n" +
                "  - Visualization:\n" +
                "    * Static Waveform: Pre-renders the normalized waveform and tracks the playhead position in real-time.\n" +
                "    * FFT Spectrum: Dynamically scales the frequency range up to the Nyquist frequency (SampleRate / 2) based on the source audio.\n" +
                "  - Channel Statistics:\n" +
                "    * Meters: Monitors per-channel Peak/RMS levels and detects 0dBFS digital clipping with red indicators.\n" +
                "    * Detail Stats: Tracks precise values for Sample Peak, Max/Min RMS, and DC Offset.\n" +
                "  - Loudness & Standards:\n" +
                "    * Standards: Select broadcasting standards (EBU R 128, ATSC A/85, ARIB, OP-59) to verify compliance against targets.\n" +
                "    * Measurements: Monitors Integrated Loudness (Cumulative), Short-term, Momentary LUFS, and True Peak (dBTP).\n" +
                "    * Reset Functionality: Use the 'Reset' button to clear cumulative Integrated Loudness stats for a fresh measurement.\n" +
                "      (Note: Adjusting the volume slider in the main form automatically triggers a reset to ensure accuracy.)\n\n" +

                "● 8. Keyboard Shortcuts\n" +
                "  - Ctrl + O : Open File\n" +
                "  - Ctrl + Shift + O : Open Folder\n" +
                "  - Ctrl + E : Extract Checked\n" +
                "  - Ctrl + Shift + E : Extract All\n" +
                "  - Ctrl + Shift + C : Export CSV\n" +
                "  - Ctrl + F : Focus Search\n" +
                "  - F1 : Open Help\n\n" +

                "● 9. Troubleshooting\n" +
                "  - Rebuild Failed: 'fsbankcl.exe' is missing, or the file is too large to fit even at lowest quality.\n" +
                "  - No Sound: Codec might be unsupported or file is encrypted (Check FMOD error logs).\n\n" +

                "● 10. License Information\n" +
                "  - FMOD Engine: This program uses FMOD Engine (Core/Studio API) version 2.03.11.\n" +
                "    - Copyright © Firelight Technologies Pty Ltd.\n" +
                "    - Used under license agreement. Refer to FMOD_LICENSE.TXT for details.\n" +
                "  - Icon Attribution: 'Unboxing icons' created by Graphix's Art - Flaticon.\n" +
                "    - URL: https://www.flaticon.com/free-icons/unboxing\n" +
                "  - Source Code License: The code for this program (excluding FMOD Engine) is distributed under the GNU General Public License v3.0.";

            // Assign the help text to the RichTextBox controls in each tab.
            richTextBoxKorean.Text = helpTextKR;
            richTextBoxEnglish.Text = helpTextEN;

            // Ensure the scrollbars start at the top of the text.
            richTextBoxKorean.Select(0, 0);
            richTextBoxEnglish.Select(0, 0);
        }
    }
}