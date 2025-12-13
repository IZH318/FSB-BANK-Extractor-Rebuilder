/**
 * @file HelpForm.cs
 * @brief Provides a form to display help and license information for the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form uses a TabControl to separate help content by language (Korean and English).
 * The content has been thoroughly revised and expanded based on a deep analysis of the application's
 * entire codebase to ensure accuracy and completeness, reflecting every feature and logic with user-friendly examples.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-13
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
                "  - 다단계 진행률 표시: 로딩 시, [SCANNING] -> [PRE-PROCESSING] -> [ANALYZING] -> [FINALIZING] 순서로 하단 상태 바에 현재 작업 단계가 표시됩니다.\n" +
                "  - Strings Bank 자동 로드: 로딩 시, 같은 폴더 내의 *.strings.bank 파일을 자동으로 먼저 로드하여 이벤트, 버스 등의 실제 이름을 복원합니다.\n" +
                "    (이름이 GUID로 표시될 경우, 'File' -> 'Load Strings Bank (Manual)...' 메뉴로 수동 지정이 가능합니다.)\n" +
                "  - 오류 로그: 파일 로딩, 추출, 리빌드 등 주요 작업 중 오류 발생 시, 프로그램 폴더에 상세 내용이 담긴 'ErrorLog_*.log' 파일을 생성합니다.\n\n" +

                "● 2. 구조 탐색 및 스마트 검색\n" +
                "  - 계층 뷰: Bank 파일 내부에 숨겨진 FSB5 데이터 청크를 식별하고, Audio/Event/Bus 등 모든 FMOD 객체를 계층 구조로 시각화합니다.\n" +
                "  - 상세 정보: 우측 패널에서 선택된 항목의 상세 메타데이터(포맷, 채널, 비트 심도, 샘플레이트, 루프 구간, 데이터 오프셋 및 크기, GUID 등)를 실시간으로 표시합니다.\n" +
                "    (예시: Format: VORBIS, Channels: 2, Loop: 500ms - 2500ms 등)\n" +
                "  - 스마트 검색 (Ctrl+F): 검색어 입력 시, 500ms의 지연 처리 후 결과가 리스트 뷰로 전환됩니다.\n" +
                "    * 결과 내 기능: 검색 결과 목록에서 직접 재생, 단일 추출, 리빌드, 데이터 복사 등 모든 주요 기능을 우클릭 메뉴로 사용할 수 있습니다.\n" +
                "    * 원본 위치로 이동: 결과 항목 우클릭 -> 'Open File Location'을 선택하면, 트리 뷰의 원본 위치로 즉시 이동하고 해당 항목이 선택됩니다.\n" +
                "    * 데이터 복사: 우클릭 메뉴를 통해 이름, 전체 경로, GUID(이벤트/뱅크)를 클립보드로 복사할 수 있습니다.\n\n" +

                "● 3. 오디오 재생 시스템\n" +
                "  - 하이브리드 재생 엔진: 순수 오디오 데이터(.fsb)는 FMOD Core API로, 복잡한 로직이 포함된 이벤트는 FMOD Studio API로 재생합니다.\n" +
                "  - 제어 패널: 재생/일시정지, 정지, 타임라인 탐색 바, 볼륨 슬라이더(0~100%)를 제공합니다.\n" +
                "  - 'Force Loop' 체크박스: 파일의 기본 루프 설정과 관계없이 재생 중인 사운드의 루프 여부를 강제로 제어합니다.\n" +
                "    - 체크 시: 사운드가 강제로 루프됩니다. 만약 사운드 자체에 루프 구간이 설정되어 있다면 해당 구간을 반복하며, 없을 경우 사운드 전체를 반복합니다.\n" +
                "    - 체크 해제 시: 사운드가 원래 루프 사운드였더라도 한 번만 재생하고 멈춥니다.\n" +
                "  - 'Auto-Play' 체크박스: 체크 시, 트리 뷰나 검색 결과에서 항목을 클릭하여 선택할 때마다 자동으로 재생을 시작합니다.\n\n" +

                "● 4. 추출 시스템\n" +
                "  - 추출 포맷: FMOD의 내부 포맷(Vorbis, FADPCM 등)에 관계없이, 모든 오디오는 표준 WAV(RIFF) 헤더가 포함된 무손실 파일로 변환되어 저장됩니다.\n" +
                "  - 상세 진행률 표시: 파일 추출 시, '[EXTRACTING] [1/5] audio.wav | 512.5 KB / 1024.0 KB (50%)'와 같이 개별 파일의 처리 진행률(KB)을 실시간으로 표시합니다.\n" +
                "  - 추출 모드:\n" +
                "    1. 'Extract Checked' / 'Extract All' (일괄 추출): 체크된 항목 또는 전체 항목을 한 번에 추출합니다.\n" +
                "    2. 'Extract This Item...' (단일 추출): 특정 오디오 항목을 우클릭하여 개별적으로 추출합니다.\n" +
                "  - 저장 경로 및 폴더 생성 규칙:\n" +
                "    - 일괄 추출 시: 'Same as source file', 'Custom path' 등 설정된 기준 경로 하위에 폴더 구조를 자동으로 생성합니다.\n" +
                "      (예시: 'C:\\Game\\sound.bank' 내 'sfx.fsb'의 파일들은 'C:\\Game\\sound\\sfx\\' 폴더에 나뉘어 저장됩니다.)\n" +
                "    - 단일 추출 시: 폴더가 자동으로 생성되지 않으며, 사용자가 '다른 이름으로 저장' 대화상자에서 직접 위치와 파일명을 지정합니다.\n" +
                "  - 'Verbose Log' 체크박스: 체크 시, 추출된 각 파일의 성공 여부, 포맷, 루프 정보, 소요 시간 등이 기록된 TSV(탭 구분 값) 로그 파일을 생성합니다.\n\n" +

                "● 5. 리빌드 및 리패킹\n" +
                "  * 필수 조건: 프로그램 실행 폴더에 FMOD 공식 빌드 툴인 'fsbankcl.exe' 파일이 반드시 존재해야 합니다.\n" +
                "  - 리빌드 매니저: 'Rebuild Manager...' 메뉴 선택 시, 교체할 파일 목록을 관리하는 전용 창이 열립니다.\n" +
                "    * 일괄/단일 관리: 여러 파일을 한 번에 교체하거나, 목록에서 특정 파일만 선택하여 교체할 수 있습니다.\n" +
                "    * 자동 매칭 (Auto-Match from Folder): 지정한 폴더 내에서 교체할 파일을 자동으로 찾아주는 기능입니다.\n" +
                "      - 1단계: 정확히 일치 (Exact Match)\n" +
                "        - 원본 내부 이름과 파일 이름이 완전히 같은 경우를 우선적으로 찾습니다.\n" +
                "        - 예시: 리빌드 목록에 'footstep_grass_01'이 있다면, 폴더에서 'footstep_grass_01.wav' 파일을 찾아 연결합니다.\n" +
                "      - 2단계: 스마트 매칭 (Smart Match)\n" +
                "        - 1단계에서 일치하는 파일을 찾지 못했을 경우, 원본 이름의 숫자 접미사('_01', '_02' 등)를 제외한 기본 이름으로 다시 검색합니다.\n" +
                "        - 예시: 리빌드 목록에 'footstep_grass_01', 'footstep_grass_02'가 있고 폴더에 'footstep_grass.wav' 파일만 있을 경우, 두 항목 모두의 교체 대상으로 'footstep_grass.wav'를 제안합니다.\n" +
                "        - 최종 확인: 검색이 끝나면, 정확히 일치한 항목과 스마트 매칭으로 찾은 항목의 수를 보여주며, 어떤 항목을 적용할지 사용자가 최종 선택할 수 있습니다.\n" +
                "  - 다단계 진행률 표시: 리빌드 시, [1/4 PREPARING] -> [2/4 BUILDING] -> [3/4 PATCHING] -> [4/4 CLEANUP] 순서로 하단 상태 바에 내부 프로세스 단계가 표시됩니다.\n" +
                "  - 오디오 재생 길이(Duration) 검증: 리빌드 시작 전, 교체할 오디오의 재생 시간이 원본보다 길 경우 경고창을 표시합니다.\n" +
                "    (예시: 5초 길이의 음성 대사를 7초짜리 파일로 교체할 경우, 게임 내 이벤트가 5초에 맞춰 종료되어 뒤쪽 2초가 잘릴 수 있습니다. 사용자가 위험을 인지하고 동의할 경우에만 계속 진행할 수 있습니다.)\n" +
                "  - 데이터 크기(Size) 최적화 알고리즘:\n" +
                "    교체되는 FSB 데이터는 반드시 원본 청크의 파일 크기와 정확히 일치해야 합니다. 이를 위해 아래와 같은 로직이 자동으로 작동합니다:\n" +
                "    1. 품질 조절 가능 포맷 (Vorbis):\n" +
                "       - '이진 탐색(Binary Search)' 알고리즘을 사용하여 최적의 압축 품질을 자동으로 찾아냅니다. (예: 50% 품질 -> 25% 품질 -> 37% 품질 순으로 최적점을 탐색)\n" +
                "       - 최종 결과물이 원본보다 약간 작을 경우, 부족한 만큼 0으로 채워(패딩) 크기를 맞춥니다.\n" +
                "    2. 고정 비트레이트 포맷 (PCM, FADPCM):\n" +
                "       - 결과물이 원본보다 작을 경우: 남은 공간을 0으로 채웁니다(패딩).\n" +
                "       - 결과물이 원본보다 클 경우: .bank 파일 손상을 유발할 수 있으므로 경고창을 표시하며, 사용자가 동의할 경우에만 진행됩니다.\n\n" +

                "● 6. 데이터 관리 도구\n" +
                "  - 'Index Tools...' 메뉴 (FSB 컨테이너 노드 우클릭): 많은 수의 오디오가 포함된 FSB 파일에서 특정 항목을 쉽게 찾고 선택할 수 있습니다.\n" +
                "    * 'Jump to Index' 모드: 특정 인덱스 번호로 즉시 스크롤하고 선택합니다.\n" +
                "    * 'Select Indices' 모드: '1-10, 15, 20-30'과 같은 형식으로 범위를 지정하여 여러 항목을 한 번에 체크합니다.\n" +
                "  - CSV 내보내기 (Ctrl+Shift+C): 현재 트리 구조의 모든 메타데이터를 CSV 파일로 내보냅니다.\n" +
                "    (활용: 게임에 포함된 모든 사운드 자산의 목록을 만들어 스프레드시트 프로그램에서 분석하거나 문서화할 때 유용합니다.)\n\n" +

                "● 7. 오디오 분석기\n" +
                "  - 실행: 'Tools' 메뉴 -> 'Audio Analyzer...'를 선택하여 실시간 분석 창을 엽니다. 오디오 재생 시 데이터가 자동으로 연동됩니다.\n" +
                "  - 다중 뷰 및 분할 화면: 오실로스코프(Oscilloscope), 스펙트럼(Spectrum), 스펙트로그램(Spectrogram) 3개의 분석 도구를 2개의 패널에 자유롭게 배치하고, 슬라이더로 패널 크기를 조절할 수 있습니다.\n" +
                "  - 시각화:\n" +
                "    * 정적 파형(Static Waveform) & 벡터스코프(Vectorscope): 전체 오디오 파형과 스테레오 위상을 상단에 고정 표시하며, 재생 위치를 실시간으로 추적합니다.\n" +
                "      (예시: 벡터스코프에서 파형이 수직선에 가까우면 모노 사운드, 넓게 퍼져 있으면 스테레오 이미지가 넓은 사운드임을 의미합니다.)\n" +
                "    * FFT 스펙트럼(Spectrum) & 스펙트로그램(Spectrogram): 오디오의 샘플레이트를 감지하여 나이퀴스트 주파수(Nyquist Frequency)까지의 대역폭을 자동으로 설정하여 주파수 분포를 시각화합니다.\n" +
                "  - 채널 통계:\n" +
                "    * 레벨 미터(Level Meter): 각 채널별 실시간 평균 음량(RMS) 및 최대 피크(Peak) 레벨을 제공하며, 0dBFS를 초과하는 디지털 클리핑 발생 시 붉은색 표시와 함께 횟수를 카운트합니다.\n" +
                "    * 상세 통계: 샘플 피크(Sample Peak), 최대/최소 RMS, DC 오프셋(DC Offset) 등의 수치를 정밀하게 추적하여 표시합니다.\n" +
                "  - 라우드니스(Loudness) 및 표준:\n" +
                "    * 표준 규격: EBU R 128, ATSC A/85 등 주요 방송 표준을 선택하여 목표 레벨 준수 여부를 검사할 수 있습니다.\n" +
                "      (예시: EBU R 128 표준 선택 시, Integrated LUFS 값이 목표치인 -23 LUFS에 근접하는지 확인할 수 있습니다.)\n" +
                "    * 측정 항목: 전체 누적 음량(Integrated LUFS), 단기(Short-term LUFS), 순간(Momentary LUFS) 및 트루 피크(True Peak, dBTP)를 측정합니다.\n" +
                "    * 'Reset' 버튼: 누적 음량은 재생하는 동안 계속 누적되므로, 새로운 구간을 측정할 때는 'Reset' 버튼을 눌러 초기화해야 합니다.\n" +
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
                "It integrates with the official FMOD tool 'fsbankcl.exe' to provide a robust audio rebuilding/replacement feature.\n\n\n" +

                "● 1. File Loading & Initialization\n" +
                "  - Open File/Folder: Use the 'File' menu or Drag & Drop files/folders from Explorer.\n" +
                "  - Recursive Scan: When a folder is loaded, all subdirectories are scanned for .bank/.fsb files.\n" +
                "  - Multi-Stage Progress: During loading, the status bar indicates the current stage: [SCANNING] -> [PRE-PROCESSING] -> [ANALYZING] -> [FINALIZING].\n" +
                "  - Auto-Load Strings Bank: Automatically detects and loads *.strings.bank files to resolve asset names (Events, Buses, etc.).\n" +
                "    (If names appear as GUIDs, use 'File' -> 'Load Strings Bank (Manual)...')\n" +
                "  - Error Logging: If an error occurs during a major operation (loading, extraction, rebuild), a detailed 'ErrorLog_*.log' file is created in the application's directory.\n\n" +

                "● 2. Structure Explorer & Smart Search\n" +
                "  - Tree View: Visualizes the hierarchy (Bank -> FSB -> Audio/Events) and identifies hidden FSB5 data chunks within .bank files.\n" +
                "  - Details Panel: Displays detailed metadata for the selected item.\n" +
                "    (e.g., Format: VORBIS, Channels: 2, Loop: 500ms - 2500ms, etc.)\n" +
                "  - Smart Search (Ctrl+F): Filters items into a list view after a 500ms debounce delay.\n" +
                "    * In-Result Actions: Right-click menu in search results allows for direct playback, extraction, rebuilding, and data copying.\n" +
                "    * Locate Original: Right-click -> 'Open File Location' jumps to the node's original position in the main tree view.\n" +
                "    * Copy Data: Right-click to copy Name, Full Path, or GUID (for Events/Banks) to the clipboard.\n\n" +

                "● 3. Audio Playback System\n" +
                "  - Hybrid Engine: Uses FMOD Core API for raw audio data and FMOD Studio API for complex events.\n" +
                "  - Controls: Play/Pause, Stop, Seek Bar, and Volume Slider (0-100%).\n" +
                "  - 'Force Loop' Checkbox: Overrides the sound's default settings to forcibly enable or disable looping.\n" +
                "    - Checked: Forces the sound to loop. If the sound file has its own defined loop points, it will use them. Otherwise, the entire sound will loop.\n" +
                "    - Unchecked: Forces the sound to play only once, even if it was originally a looping sound.\n" +
                "  - 'Auto-Play' Checkbox: When checked, selecting an item in any view starts playback immediately.\n\n" +

                "● 4. Extraction System\n" +
                "  - Format: All audio is converted to standard WAV files with proper RIFF headers.\n" +
                "  - Detailed Progress: During extraction, the status bar shows per-file progress in real-time (e.g., '[EXTRACTING] [1/5] audio.wav | 512.5 KB / 1024.0 KB (50%)').\n" +
                "  - Modes:\n" +
                "    1. 'Extract Checked' / 'Extract All' (Batch Extraction): Extracts checked or all items at once.\n" +
                "    2. 'Extract This Item...' (Single Extraction): Extracts an individual item by right-clicking it.\n" +
                "  - Path & Folder Generation:\n" +
                "    - For Batch Extraction: Folders are created automatically under the chosen base path ('Same as source file', 'Custom path', etc.).\n" +
                "      (e.g., Files from 'sfx.fsb' inside 'C:\\Game\\sound.bank' are saved to 'C:\\Game\\sound\\sfx\\').\n" +
                "    - For Single Extraction: No folders are created automatically; you specify the exact location and filename in the 'Save As' dialog.\n" +
                "  - 'Verbose Log' Checkbox: Generates a detailed TSV log file with success status, format info, and timings for each extracted file.\n\n" +

                "● 5. Rebuilding & Repacking\n" +
                "  * Requirement: The official FMOD tool 'fsbankcl.exe' must be in the application's directory.\n" +
                "  - Rebuild Manager: The 'Rebuild Manager...' menu opens a dedicated dialog for managing file replacements.\n" +
                "    * Batch/Single Mode: Replace one or many files in a single operation.\n" +
                "    * Auto-Match (from Folder): A feature to automatically find replacement files in a selected folder.\n" +
                "      - Step 1: Exact Match\n" +
                "        - It first looks for files with a name that is identical to the internal sound name.\n" +
                "        - Example: If the rebuild list contains 'footstep_grass_01', it will find and link the 'footstep_grass_01.wav' file in the folder.\n" +
                "      - Step 2: Smart Match\n" +
                "        - If an exact match is not found, it performs a second search by removing numeric suffixes from the original name.\n" +
                "        - Example: If the rebuild list has 'footstep_grass_01' and 'footstep_grass_02', and the folder only contains 'footstep_grass.wav', it will suggest 'footstep_grass.wav' as the replacement for both items.\n" +
                "        - Final Confirmation: After scanning, it reports the number of exact and smart matches found, allowing you to choose which ones to apply.\n" +
                "  - Multi-Stage Progress: The status bar shows the internal stages of the rebuild process: [1/4 PREPARING] -> [2/4 BUILDING] -> [3/4 PATCHING] -> [4/4 CLEANUP].\n" +
                "  - Audio Duration Validation: Before rebuilding, the tool warns if a replacement audio's duration is longer than the original.\n" +
                "    (e.g., If a 5-second voice line is replaced with a 7-second one, the game event might still end at 5 seconds, cutting off the last 2 seconds. The process only continues if the user acknowledges the risk.)\n" +
                "  - Data Size Optimization Algorithm:\n" +
                "    The new data chunk must exactly match the original chunk's file size. The tool enforces this automatically:\n" +
                "    1. Variable Quality Formats (Vorbis):\n" +
                "       - Uses a 'Binary Search' algorithm to automatically find the optimal compression quality. (e.g., It tests 50% quality -> 25% -> 37% to find the best fit.)\n" +
                "       - If the final result is slightly smaller, the remaining space is filled with zeros (Padding).\n" +
                "    2. Fixed Formats (PCM, FADPCM):\n" +
                "       - If Smaller: The remaining space is padded with zeros.\n" +
                "       - If Larger: A strong warning is displayed as this can corrupt a .bank file. The user must agree to proceed.\n\n" +

                "● 6. Data Management Tools\n" +
                "  - Index Tools (Right-click FSB container node): For easily navigating FSBs with hundreds of subsounds.\n" +
                "    * 'Jump to Index' Mode: Immediately scroll to and select a specific index number.\n" +
                "    * 'Select Indices' Mode: Batch-check items using syntax like '1-10, 15, 20-30'.\n" +
                "  - CSV Export (Ctrl+Shift+C): Exports all tree metadata to a CSV file.\n" +
                "    (Use Case: Useful for creating a complete inventory of all game audio assets for analysis in spreadsheet software like Excel.)\n\n" +

                "● 7. Audio Analyzer\n" +
                "  - Launch: Go to 'Tools' -> 'Audio Analyzer...' to open the real-time analysis window, which automatically links to any playing audio.\n" +
                "  - Multi-View & Split Screen: Freely arrange Oscilloscope, Spectrum, and Spectrogram tools in two panels and adjust the panel size with a slider.\n" +
                "  - Visualization:\n" +
                "    * Static Waveform & Vectorscope: A static overview of the entire waveform and stereo phase is fixed at the top, tracking the playhead in real-time.\n" +
                "      (e.g., In the Vectorscope, a vertical line indicates a mono sound, while a wide pattern suggests a broad stereo image.)\n" +
                "    * FFT Spectrum & Spectrogram: Dynamically scales the frequency range up to the Nyquist Frequency (SampleRate / 2) based on the source audio.\n" +
                "  - Channel Statistics:\n" +
                "    * Meters: Monitors per-channel real-time Peak/RMS levels and detects 0dBFS digital clipping with red indicators and a clip counter.\n" +
                "    * Detail Stats: Tracks precise values for Sample Peak, Max/Min RMS, and DC Offset.\n" +
                "  - Loudness & Standards:\n" +
                "    * Standards: Select broadcasting standards (EBU R 128, etc.) to verify compliance.\n" +
                "      (e.g., When targeting EBU R 128, the analyzer shows if your Integrated LUFS is close to the -23 LUFS target.)\n" +
                "    * Measurements: Monitors Integrated Loudness, Short-term, Momentary LUFS, and True Peak (dBTP).\n" +
                "    * 'Reset' Functionality: Use the 'Reset' button to clear cumulative Integrated Loudness stats for a fresh measurement.\n" +
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
                "  - Rebuild Failed: 'fsbankcl.exe' is missing, or the replacement audio file is too large to fit even at the lowest compression quality (0).\n" +
                "  - No Sound: The codec might be unsupported by FMOD, or the file could be encrypted.\n" +
                "  - FMOD Init Failed: May be caused by an incompatible or corrupted FMOD library (fmod.dll, etc.) in the system path.\n\n" +

                "● 10. License Information\n" +
                "  - FMOD Engine: This program uses FMOD Engine (Core/Studio API) version 2.03.11.\n" +
                "    - Copyright © Firelight Technologies Pty Ltd.\n" +
                "    - Used under license agreement. Refer to FMOD_LICENSE.TXT for details.\n" +
                "  - Icon Attribution: 'Unboxing icons' created by Graphix's Art - Flaticon.\n" +
                "    - URL: https://www.flaticon.com/free-icons/unboxing\n" +
                "  - Source Code License: The code for this program (excluding FMOD Engine) is distributed under the GNU General Public License v3.0.";

            // Assign the help text to the RichTextBox controls in each tab.
            // These controls (richTextBoxKorean, richTextBoxEnglish) are assumed to be defined in the Designer.cs file.
            richTextBoxKorean.Text = helpTextKR;
            richTextBoxEnglish.Text = helpTextEN;

            // Ensure the scrollbars start at the top of the text.
            richTextBoxKorean.Select(0, 0);
            richTextBoxEnglish.Select(0, 0);
        }
    }
}