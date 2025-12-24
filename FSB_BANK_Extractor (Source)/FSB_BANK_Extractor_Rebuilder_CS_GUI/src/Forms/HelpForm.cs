/**
 * @file HelpForm.cs
 * @brief Provides a form to display help and license information for the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This form uses a TabControl to separate help content by language (Korean and English).
 * The content has been thoroughly revised based on a deep analysis of the application's codebase.
 * It now accurately reflects features like the Hybrid Parsing Engine, FSB5-exclusive rebuilding,
 * and Smart Cleanup, ensuring users have precise technical information.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
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
                "본 프로그램은 FMOD 오디오 엔진(v2.03.11)과 자체 개발된 '레거시 바이너리 파서'를 결합하여,\n" +
                "FSB5 .bank/.fsb 파일 및 FSB3/4 파일의 구조를 정밀 분석하고 오디오를 추출하는 통합 솔루션입니다.\n\n\n" +

                "● 1. 파일 불러오기 및 초기화\n" +
                "  - 파일/폴더 열기: 'File' 메뉴를 이용하거나, 탐색기에서 파일/폴더를 프로그램 창으로 드래그 앤 드롭하여 불러옵니다.\n" +
                "  - 재귀적 탐색: 폴더를 불러올 경우, 모든 하위 폴더를 포함하여 .bank와 .fsb 파일을 검색합니다.\n" +
                "  - 다단계 진행률 표시: 로딩 시, [SCANNING] -> [PRE-PROCESSING] -> [ANALYZING] -> [FINALIZING] 순서로 하단 상태 바에 현재 작업 단계가 표시됩니다.\n" +
                "  - Strings Bank 자동 로드: 로딩 시, 같은 폴더 내의 *.strings.bank 파일을 자동으로 감지 및 로드하여 이벤트, 버스 등의 실제 이름을 복원합니다.\n" +
                "    (이름이 GUID로 표시될 경우, 'File' -> 'Load Strings Bank (Manual)...' 메뉴로 수동 지정이 가능합니다.)\n" +
                "  - 잔여 임시 파일 자동 삭제: 프로그램 시작 시, 이전 세션에서 비정상 종료 등으로 인해 남겨진 임시 작업 파일(Temporary Workspace)들을 자동으로 감지하여 정리합니다.\n" +
                "  - 오류 로그: 파일 로딩, 추출, 리빌드 등 주요 작업 중 오류 발생 시, 프로그램 폴더에 상세 내용이 담긴 'ErrorLog_*.log' 파일을 생성합니다.\n\n" +

                "● 2. 구조 탐색 및 하이브리드 파싱\n" +
                "  본 프로그램은 두 가지 파싱 엔진을 동시에 사용하여 호환성을 극대화했습니다.\n" +
                "  - 최신 포맷 (FSB5): FMOD 공식 API를 사용하여 완벽한 호환성을 보장합니다.\n" +
                "  - 레거시 포맷 (FSB3/FSB4): 자체 내장된 바이너리 파서를 통해 헤더 구조를 직접 해석하여 상세 정보를 표시합니다.\n" +
                "  - 상세 정보: 우측 패널에서 선택된 항목의 상세 메타데이터(포맷, 채널, 비트 심도, 샘플레이트, 루프 구간, 데이터 오프셋 및 크기, GUID 등)를 실시간으로 표시합니다.\n" +
                "  - 스마트 검색 (Ctrl+F): 검색어 입력 시, 500ms의 지연 처리 후 결과가 리스트 뷰로 전환됩니다.\n" +
                "    * 결과 내 기능: 검색 결과 목록에서 우클릭 시 'Select All(전체 선택)', 재생, 추출, 리빌드, 데이터 복사 기능을 사용할 수 있습니다.\n" +
                "    * 원본 위치로 이동: 결과 항목 우클릭 -> 'Open File Location'을 선택하면, 트리 뷰의 원본 위치로 즉시 이동하고 해당 항목이 선택됩니다.\n" +
                "    * 데이터 복사: 우클릭 메뉴를 통해 이름, 전체 경로, GUID(이벤트/뱅크)를 클립보드로 복사할 수 있습니다.\n\n" +

                "● 3. 오디오 재생 시스템\n" +
                "  - 하이브리드 재생 엔진: 순수 오디오 데이터(.fsb)는 FMOD Core API로, 복잡한 로직이 포함된 이벤트는 FMOD Studio API로 재생합니다.\n" +
                "  - 레거시 재생 지원: FSB3/4 포맷은 메모리 버퍼링 및 자동 변환 방식을 통해 재생을 지원합니다.\n" +
                "  - 제어 패널: 재생/일시정지, 정지, 타임라인 탐색 바, 볼륨 슬라이더(0~100%)를 제공합니다.\n" +
                "  - 'Force Loop' 체크박스: 파일의 기본 루프 설정과 관계없이 재생 중인 사운드의 루프 여부를 강제로 제어합니다.\n" +
                "    - 체크 시: 사운드가 강제로 루프됩니다. 만약 사운드 자체에 루프 구간이 설정되어 있다면 해당 구간을 반복하며, 없을 경우 사운드 전체를 반복합니다.\n" +
                "    - 체크 해제 시: 사운드가 원래 루프 사운드였더라도 한 번만 재생하고 멈춥니다.\n" +
                "  - 'Auto-Play' 체크박스: 체크 시, 트리 뷰나 검색 결과에서 항목을 클릭하여 선택할 때마다 자동으로 재생을 시작합니다.\n\n" +

                "● 4. 추출 시스템\n" +
                "  - 광범위한 호환성: 최신 FMOD 엔진이 공식 지원하지 않는 구버전 포맷(FSB3/4)이라도 자체 디코더를 통해 표준 WAV(RIFF) 파일로 변환하여 추출합니다.\n" +
                "  - 상세 진행률 표시: 파일 추출 시, '[EXTRACTING] [1/5] audio.wav | 0.50 MB / 1.00 MB'와 같이 개별 파일의 처리 용량을 실시간으로 표시합니다.\n" +
                "  - 추출 모드:\n" +
                "    1. 'Extract Checked' / 'Extract All' (일괄 추출): 체크된 항목 또는 전체 항목을 한 번에 추출합니다.\n" +
                "    2. 'Extract This Item...' (단일 추출): 특정 오디오 항목을 우클릭하여 개별적으로 추출합니다.\n" +
                "  - 저장 경로 설정 (드롭다운 메뉴):\n" +
                "    - 'Same as source file': 원본 파일이 있는 폴더 하위에 구조를 생성하여 저장합니다.\n" +
                "    - 'Custom path': 사용자가 지정한 특정 폴더에 모든 파일을 저장합니다.\n" +
                "    - 'Ask every time': 추출 버튼을 누를 때마다 저장할 폴더를 물어봅니다.\n" +
                "  - 'Verbose Log' 체크박스: 체크 시, 추출된 각 파일의 성공 여부, 포맷, 루프 정보, 소요 시간 등이 기록된 TSV(탭 구분 값) 로그 파일을 생성합니다.\n\n" +

                "● 5. 리빌드 및 리패킹\n" +
                "  * 지원 대상:\n" +
                "    - .bank 파일 내부에 포함된 FSB 데이터 영역 패치.\n" +
                "    - 독립형 .fsb 파일의 전체 리빌드.\n" +
                "  * 중요 제약 사항:\n" +
                "    - 리빌드 기능은 오직 'FSB5' 포맷만 지원합니다.\n" +
                "  * 필수 조건:\n" +
                "    - 프로그램 실행 폴더에 FMOD 공식 빌드 툴인 'fsbankcl.exe' 파일이 반드시 존재해야 합니다.\n" +
                "  - 리빌드 매니저: 'Rebuild Manager...' 메뉴 선택 시, 교체할 파일 목록을 관리하는 전용 창이 열립니다.\n" +
                "    * 일괄/단일 관리: 여러 파일을 한 번에 교체하거나, 목록에서 특정 파일만 선택하여 교체할 수 있습니다.\n" +
                "    * 자동 매칭 (Auto-Match from Folder): 지정한 폴더 내에서 교체할 파일을 자동으로 찾아주는 기능입니다.\n" +
                "      - 지원 포맷: WAV, OGG, MP3, FLAC, AIFF, M4A 등 FMOD가 인식 가능한 대부분의 표준 오디오 파일.\n" +
                "      - 1단계: 정확히 일치 (Exact Match)\n" +
                "        - 원본 내부 이름과 파일 이름이 완전히 같은 경우를 우선적으로 찾습니다.\n" +
                "        - 예시: 리빌드 목록에 'footstep_grass_01'이 있다면, 폴더에서 'footstep_grass_01.wav' 파일을 찾아 연결합니다.\n" +
                "      - 2단계: 스마트 매칭 (Smart Match)\n" +
                "        - 1단계에서 일치하는 파일을 찾지 못했을 경우, 원본 이름의 숫자 접미사('_01', '_02' 등)를 제외한 기본 이름으로 다시 검색합니다.\n" +
                "        - 예시: 리빌드 목록에 'footstep_grass_01', 'footstep_grass_02'가 있고 폴더에 'footstep_grass.wav' 파일만 있을 경우, 두 항목 모두의 교체 대상으로 'footstep_grass.wav'를 제안합니다.\n" +
                "        - 최종 확인: 검색이 끝나면, 정확히 일치한 항목과 스마트 매칭으로 찾은 항목의 수를 보여주며, 어떤 항목을 적용할지 사용자가 최종 선택할 수 있습니다.\n" +
                "  - 다단계 진행률 표시: 리빌드 시, [1/4 PREPARING] -> [2/4 BUILDING] -> [3/4 PATCHING] -> [4/4 CLEANUP] 순서로 하단 상태 바에 내부 프로세스 단계가 표시됩니다.\n" +
                "  - 오디오 재생 길이(Duration) 검증: 리빌드 시작 전, 교체할 오디오의 재생 시간이 원본보다 길 경우 경고창을 표시합니다.\n" +
                "    (예시: 5초 길이의 음성 대사를 7초짜리 파일로 교체할 경우, 게임 내 이벤트가 5초에 맞춰 종료되어 뒤쪽 2초가 잘릴 수 있습니다. (사용자가 동의할 경우에만 진행.))\n" +
                "  - 데이터 크기 최적화 알고리즘:\n" +
                "    리빌드 결과물의 용량이 원본 청크 크기와 다를 경우의 처리 로직입니다:\n" +
                "    1. 품질 조절 가능 포맷 (Vorbis):\n" +
                "       - '이진 탐색' 알고리즘을 사용하여, 원본 용량 제한을 초과하지 않는 최적의 압축 품질(0~100)을 자동으로 찾아냅니다. 이 과정에서 여러 번의 테스트 빌드가 수행될 수 있습니다.\n" +
                "       - 최적 품질로 빌드된 최종 결과물이 원본보다 작을 경우, 부족한 만큼 0으로 채워(패딩) 크기를 정확히 맞춥니다.\n" +
                "    2. 고정 비트레이트 포맷 (PCM, FADPCM):\n" +
                "       - 결과물이 원본보다 작을 경우: 남은 공간을 0으로 채워(패딩) 오프셋을 맞춥니다.\n" +
                "       - 결과물이 원본보다 클 경우: .bank 파일의 후속 데이터 오프셋을 깨뜨려 심각한 손상을 유발할 수 있으므로 경고창을 표시합니다. (사용자가 동의할 경우에만 진행.)\n" +
                "         (1) .bank 파일 수정 시: 내부 오프셋이 밀려 파일 구조가 손상되므로 게임 내에서의 정상 작동을 보장할 수 없습니다.\n" +
                "         (2) .fsb 파일 수정 시: 파일 자체는 정상적으로 생성되지만 원본보다 용량이 커집니다.\n\n" +

                "● 6. 데이터 관리 도구\n" +
                "  - 'Select All' (모두 선택): 트리 뷰나 검색 결과의 우클릭 메뉴에서 'Select All'을 선택하여 현재 보이는 모든 항목을 한 번에 체크할 수 있습니다.\n" +
                "  - 'Index Tools...' 메뉴 (FSB 컨테이너 노드 우클릭): 많은 수의 오디오가 포함된 FSB 파일에서 특정 항목을 쉽게 찾고 선택할 수 있습니다.\n" +
                "    * 'Jump to Index' 모드: 특정 인덱스 번호로 즉시 스크롤하고 선택합니다.\n" +
                "    * 'Select Indices' 모드: '1-10, 15, 20-30'과 같은 형식으로 범위를 지정하여 여러 항목을 한 번에 체크합니다.\n" +
                "  - CSV 내보내기 (Ctrl+Shift+C): 이름, 인덱스, 경로, 인코딩(Format), 채널, 비트 심도, 주파수(Hz), 루프 구간, GUID 등 트리 뷰의 모든 상세 기술 정보를 CSV 파일로 내보냅니다.\n\n" +

                "● 7. 오디오 분석기\n" +
                "  - 실행: 'Tools' 메뉴 -> 'Audio Analyzer...'를 선택하여 실시간 분석 창을 엽니다. 오디오 재생 시 데이터가 자동으로 연동됩니다.\n" +
                "  - 다중 뷰 및 분할 화면: 오실로스코프(Oscilloscope), 스펙트럼(Spectrum), 스펙트로그램(Spectrogram) 3개의 분석 도구를 2개의 패널에 자유롭게 배치하고, 슬라이더로 패널 크기를 조절할 수 있습니다.\n" +
                "  - 시각화:\n" +
                "    * 정적 파형(Static Waveform) & 벡터스코프(Vectorscope): 전체 파형 탐색과 함께, 좌우 채널의 위상 상관관계(Phase Correlation)를 리사주(Lissajous) 도형으로 시각화합니다.\n" +
                "      (화면의 수직선은 완벽한 모노를 의미하며, 원형에 가까울수록 넓은 스테레오 이미지를 나타냅니다. 반면, 수평선은 위상 상쇄(Out of Phase) 위험을 경고합니다.)\n" +
                "    * FFT 스펙트럼(Spectrum) & 스펙트로그램(Spectrogram): 오디오의 샘플레이트를 감지하여 나이퀴스트 주파수(Nyquist Frequency)까지의 대역폭을 자동으로 설정하여 주파수 분포를 시각화합니다.\n" +
                "  - 채널 통계:\n" +
                "    * 레벨 미터(Level Meter): 각 채널별 실시간 평균 음량(RMS) 및 최대 피크(Peak) 레벨을 제공하며, 0dBFS를 초과하는 디지털 클리핑 발생 시 붉은색 표시와 함께 횟수를 카운트합니다.\n" +
                "    * 상세 통계: 샘플 피크(Sample Peak), 최대/최소 RMS, DC 오프셋(DC Offset) 등의 수치를 정밀하게 추적하여 표시합니다.\n" +
                "    * 미터 설정 조정: 상단의 설정 패널에서 미터의 반응 속도('Meter Response')와 피크 레벨이 표시되는 유지 시간('Peak Hold')을 슬라이더로 직접 조절하여 시각적 피드백을 개인화할 수 있습니다.\n" +
                "  - 라우드니스(Loudness) 및 표준:\n" +
                "    * 표준 규격: EBU R 128 (-23 LUFS), ATSC A/85, ARIB TR-B32, OP-59, ITU-R BS.1770와 같은 방송 표준을 선택하여 라우드니스 규정 준수 여부를 검사할 수 있습니다.\n" +
                "      (분석기는 선택한 표준의 목표치에 대한 편차를 실시간으로 표시하여 직관적인 모니터링을 돕습니다.)\n" +
                "    * 측정 항목: 전체 누적 음량(Integrated LUFS), 단기(Short-term), 순간(Momentary) 라우드니스뿐만 아니라, 일반적인 디지털 미터가 놓칠 수 있는 샘플 간 피크(Inter-sample Peak)를 감지하는 트루 피크(True Peak, dBTP)를 정밀하게 측정합니다.\n" +
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
                "  - 목록이 반환되지 않음: FMOD에서 지원하지 않는 코덱이거나 파일이 암호화된 경우일 수 있습니다.\n" +
                "  - 소리가 나지 않음: FMOD에서 지원하지 않는 코덱이거나 파일이 암호화된 경우일 수 있습니다.\n" +
                "  - 리빌드 실패: 'fsbankcl.exe' 파일이 없거나, 교체할 오디오 파일의 용량이 너무 커서 최저 압축 품질(0)로도 원본 데이터 청크 크기를 맞출 수 없는 경우 발생합니다. 또한, 대상 파일이 FSB5 포맷인지 확인하십시오.\n" +
                "  - FMOD 초기화 실패: 호환되지 않는 FMOD 라이브러리(fmod.dll 등)가 시스템에 있거나 손상된 경우 발생할 수 있습니다.\n\n" +

                "● 10. 라이선스 및 저작권 정보 (License Information)\n" +
                "  - FMOD Engine: 본 프로그램은 FMOD Engine (Core/Studio API) 2.03.11 버전을 사용하였습니다.\n" +
                "    - FMOD Engine 저작권: © Firelight Technologies Pty Ltd.\n" +
                "    - FMOD Engine은 라이선스 계약에 따라 사용되었습니다. 자세한 사항은 FMOD_LICENSE.TXT를 참조하십시오.\n" +
                "  - 아이콘 출처: 본 프로그램의 아이콘은 Google AI Studio의 Gemini 2.5 Flash Image 모델을 사용하여 생성되었습니다.\n" +
                "    - 생성 플랫폼: Google AI Studio (Gemini 2.5 Flash Image)\n" +
                "  - 프로그램 라이선스: 본 프로그램의 소스 코드(FMOD 엔진 제외)는 GNU General Public License v3.0 하에 배포됩니다.";

            string helpTextEN =
                "===== FSB/BANK Extractor & Rebuilder (GUI) User Manual (EN) =====\n\n" +
                "This application utilizes the FMOD Audio Engine (v2.03.11) combined with a custom 'Legacy Binary Parser' to provide an integrated solution.\n" +
                "It performs precise structural analysis and audio extraction for FSB5 .bank/.fsb files and legacy FSB3/4 files.\n\n\n" +

                "● 1. File Loading & Initialization\n" +
                "  - Open File/Folder: Use the 'File' menu or Drag & Drop files/folders from Explorer into the application window.\n" +
                "  - Recursive Scan: When a folder is loaded, all subdirectories are also scanned for .bank and .fsb files.\n" +
                "  - Multi-Stage Progress: During loading, the status bar displays the current stage: [SCANNING] -> [PRE-PROCESSING] -> [ANALYZING] -> [FINALIZING].\n" +
                "  - Auto-Load Strings Bank: Automatically detects and loads *.strings.bank files in the same directory to restore actual names for Events, Buses, etc.\n" +
                "    (If names appear as GUIDs, you can manually load them via 'File' -> 'Load Strings Bank (Manual)...'.)\n" +
                "  - Automatic Temp File Cleanup: Upon startup, the application automatically detects and removes residual temporary workspaces left by previous abnormal sessions/crashes.\n" +
                "  - Error Logging: If errors occur during major operations (Loading, Extraction, Rebuilding), an 'ErrorLog_*.log' file with details is created in the program folder.\n\n" +

                "● 2. Structure Explorer & Hybrid Parsing\n" +
                "  This program uses two parsing engines simultaneously to maximize compatibility.\n" +
                "  - Modern Format (FSB5): Uses the official FMOD API to ensure perfect compatibility.\n" +
                "  - Legacy Format (FSB3/FSB4): Uses a built-in custom binary parser to interpret header structures and display detailed info.\n" +
                "  - Detailed Metadata: The right panel displays real-time metadata for the selected item (Format, Channels, BitDepth, SampleRate, Loop Points, Data Offset/Size, GUID, etc.).\n" +
                "  - Smart Search (Ctrl+F): Results appear in a list view after a 500ms debounce delay.\n" +
                "    * In-Result Features: Right-click on search results to access 'Select All', Play, Extract, Rebuild, and Copy Data functions.\n" +
                "    * Locate Original: Right-click -> 'Open File Location' jumps immediately to the original node in the Tree View and selects it.\n" +
                "    * Copy Data: Copy Name, Full Path, or GUID (Event/Bank) to the clipboard via the context menu.\n\n" +

                "● 3. Audio Playback System\n" +
                "  - Hybrid Playback Engine: Pure audio data (.fsb) plays via FMOD Core API, while complex Events play via FMOD Studio API.\n" +
                "  - Legacy Support: FSB3/4 formats are supported through memory buffering and automatic conversion.\n" +
                "  - Control Panel: Play/Pause, Stop, Timeline Seek Bar, Volume Slider (0-100%).\n" +
                "  - 'Force Loop' Checkbox: Overrides the file's default loop settings during playback.\n" +
                "    - Checked: Forces the sound to loop. If the sound has defined loop points, it uses them; otherwise, it loops the entire file.\n" +
                "    - Unchecked: Plays the sound only once and stops, even if the sound was originally designed to loop.\n" +
                "  - 'Auto-Play' Checkbox: When checked, selecting an item in the Tree View or Search Results automatically starts playback.\n\n" +

                "● 4. Extraction System\n" +
                "  - Broad Compatibility: Even legacy formats (FSB3/4) unsupported by the modern FMOD engine are decoded and extracted as standard WAV (RIFF) files.\n" +
                "  - Detailed Progress: Displays per-file processing size in real-time (e.g., '[EXTRACTING] [1/5] audio.wav | 0.50 MB / 1.00 MB').\n" +
                "  - Extraction Modes:\n" +
                "    1. 'Extract Checked' / 'Extract All' (Batch): Extracts checked items or all items at once.\n" +
                "    2. 'Extract This Item...' (Single): Right-click a specific audio item to extract it individually.\n" +
                "  - Save Path Settings (Dropdown):\n" +
                "    - 'Same as source file': Creates a directory structure under the folder containing the original file.\n" +
                "    - 'Custom path': Saves all files to a specific user-defined folder.\n" +
                "    - 'Ask every time': Prompts for a save location every time the extract button is pressed.\n" +
                "  - 'Verbose Log' Checkbox: When checked, generates a TSV (Tab-Separated Values) log file recording success status, format, loop info, and elapsed time for each extracted file.\n\n" +

                "● 5. Rebuilding & Repacking\n" +
                "  * Supported Targets:\n" +
                "    - Patching FSB data areas embedded within .bank files.\n" +
                "    - Full rebuild of standalone .fsb files.\n" +
                "  * Important Restrictions:\n" +
                "    - Rebuilding is supported ONLY for the 'FSB5' format.\n" +
                "  * Prerequisites:\n" +
                "    - The official FMOD build tool 'fsbankcl.exe' must exist in the application's execution directory.\n" +
                "  - Rebuild Manager: Selecting 'Rebuild Manager...' opens a dedicated window to manage the file replacement list.\n" +
                "    * Batch/Single Management: Replace multiple files at once or select specific files from the list.\n" +
                "    * Auto-Match from Folder: Automatically finds replacement files within a specified folder.\n" +
                "      - Supported Formats: Most standard audio files recognized by FMOD, including WAV, OGG, MP3, FLAC, AIFF, and M4A.\n" +
                "      - Step 1: Exact Match\n" +
                "        - Looks for files where the filename matches the internal sound name exactly.\n" +
                "        - Example: If 'footstep_grass_01' is in the list, it looks for 'footstep_grass_01.wav'.\n" +
                "      - Step 2: Smart Match\n" +
                "        - If no exact match is found, it searches again using the base name, excluding numeric suffixes (e.g., '_01', '_02').\n" +
                "        - Example: If the list has 'footstep_grass_01' & 'footstep_grass_02' but the folder only has 'footstep_grass.wav', it proposes this file for both items.\n" +
                "        - Final Confirmation: Reports the number of Exact/Smart matches found, allowing you to choose which ones to apply.\n" +
                "  - Multi-Stage Progress: Displays internal stages: [1/4 PREPARING] -> [2/4 BUILDING] -> [3/4 PATCHING] -> [4/4 CLEANUP].\n" +
                "  - Audio Duration Validation: Before rebuilding, warns if the replacement audio duration is longer than the original.\n" +
                "    (Example: Replacing a 5s dialogue with a 7s file may cause the audio to cut off early in-game. Proceed only if agreed.)\n" +
                "  - Data Size Optimization Algorithm:\n" +
                "    Logic to handle cases where the rebuild result differs in size from the original chunk:\n" +
                "    1. Variable Quality Formats (Vorbis):\n" +
                "       - Uses a 'Binary Search' algorithm to automatically find the optimal compression quality (0-100) that fits within the original size limit. Multiple test builds may run during this process.\n" +
                "       - If the result at optimal quality is smaller, it is padded with zeros to match the exact size.\n" +
                "    2. Fixed Bitrate Formats (PCM, FADPCM):\n" +
                "       - If Smaller: The remaining space is filled with zeros (Padding) to match offsets.\n" +
                "       - If Larger: A strong warning is displayed because it disrupts subsequent data offsets in the .bank file. (Proceed only if agreed.)\n" +
                "         (1) For .bank files: Internal offsets will shift, breaking file structure and likely causing game crashes.\n" +
                "         (2) For .fsb files: The file is generated correctly but will have a larger file size than the original.\n\n" +

                "● 6. Data Management Tools\n" +
                "  - 'Select All': Batch-check all currently visible items via the right-click menu in Tree View or Search Results.\n" +
                "  - 'Index Tools...' (Right-click FSB Container Node): Easily find and select items in FSB files with many audio tracks.\n" +
                "    * 'Jump to Index' Mode: Immediately scroll to and select a specific index number.\n" +
                "    * 'Select Indices' Mode: Check multiple items at once using ranges (e.g., '1-10, 15, 20-30').\n" +
                "  - CSV Export (Ctrl+Shift+C): Exports all detailed technical info from the Tree View (Name, Index, Path, Format, Channels, BitDepth, Hz, Loop, GUID) to a CSV file.\n\n" +

                "● 7. Audio Analyzer\n" +
                "  - Launch: Select 'Tools' -> 'Audio Analyzer...' to open the real-time analysis window. Data syncs automatically during playback.\n" +
                "  - Multi-View & Split Screen: Freely arrange 3 analysis tools (Oscilloscope, Spectrum, Spectrogram) across 2 panels and adjust sizes via a slider.\n" +
                "  - Visualization:\n" +
                "    * Static Waveform & Vectorscope: Navigates the full waveform and visualizes stereo Phase Correlation using a Lissajous figure.\n" +
                "      (Vertical line = Perfect Mono; Circular shape = Wide Stereo; Horizontal line = Out of Phase warning.)\n" +
                "    * FFT Spectrum & Spectrogram: Visualizes frequency distribution, automatically setting the bandwidth up to the Nyquist Frequency based on sample rate.\n" +
                "  - Channel Statistics:\n" +
                "    * Level Meter: Provides real-time RMS and Peak levels per channel. Counts occurrences of digital clipping (> 0dBFS) with red indicators.\n" +
                "    * Detailed Stats: Tracks precise values for Sample Peak, Max/Min RMS, and DC Offset.\n" +
                "    * Adjustable Metering: The settings panel at the top allows you to customize the visual feedback by adjusting the meter's responsiveness ('Meter Response') and the duration the peak level is displayed ('Peak Hold') using sliders.\n" +
                "  - Loudness & Standards:\n" +
                "    * Standards: You can select broadcasting standards such as EBU R 128 (-23 LUFS), ATSC A/85, ARIB TR-B32, OP-59, and ITU-R BS.1770 to verify loudness compliance.\n" +
                "      (The analyzer highlights real-time deviations from the target value for intuitive monitoring.)\n" +
                "    * Measurements: Precisely measures Integrated, Short-term, and Momentary Loudness, as well as True Peak (dBTP) to detect inter-sample peaks missed by standard meters.\n" +
                "    * 'Reset' Button: Integrated Loudness accumulates over time. Press 'Reset' to clear stats for a new measurement.\n" +
                "      (Important: Adjusting the main form's volume slider automatically resets analysis data to prevent measurement distortion.)\n\n" +

                "● 8. Shortcuts\n" +
                "  - Ctrl + O : Open File\n" +
                "  - Ctrl + Shift + O : Open Folder\n" +
                "  - Ctrl + E : Extract Checked\n" +
                "  - Ctrl + Shift + E : Extract All\n" +
                "  - Ctrl + Shift + C : Export to CSV\n" +
                "  - Ctrl + F : Focus Search Bar\n" +
                "  - F1 : Open Help\n\n" +

                "● 9. Troubleshooting\n" +
                "  - List Not Returned: The codec may be unsupported by FMOD or the file might be encrypted.\n" +
                "  - No Sound: The codec may be unsupported or the file is encrypted.\n" +
                "  - Rebuild Failed: 'fsbankcl.exe' is missing, or the replacement audio is too large to fit even at the lowest compression quality (0). Also, ensure the target is an FSB5 format container.\n" +
                "  - FMOD Init Failed: Can occur if an incompatible or corrupted FMOD library (fmod.dll) exists on the system.\n\n" +

                "● 10. License Information\n" +
                "  - FMOD Engine: This program uses FMOD Engine (Core/Studio API) version 2.03.11.\n" +
                "    - Copyright © Firelight Technologies Pty Ltd.\n" +
                "    - Used under license agreement. Refer to FMOD_LICENSE.TXT for details.\n" +
                "  - Icon Source: The icon for this program was generated using the Gemini 2.5 Flash Image model via Google AI Studio.\n" +
                "    - Platform: Google AI Studio (Gemini 2.5 Flash Image)\n" +
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