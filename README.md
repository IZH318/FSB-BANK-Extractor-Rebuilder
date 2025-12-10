# FSB/BANK Extractor & Rebuilder

<div align="center">

| CLI 버전 (v1.x) | GUI 버전 (v3.x) |
| :---: | :---: |
| <img width="400" alt="CLI Screenshot" src="https://github.com/user-attachments/assets/a6eca308-23af-4068-ac3a-75543cc6411f"> | <img width="400" alt="GUI Screenshot" src="https://github.com/user-attachments/assets/6b1affa3-e0e6-4234-8154-e6dcbd313405"> |

</div>

<BR>

FMOD Sound Bank(`.fsb`) 및 Bank(`.bank`) 파일의 오디오 스트림을 분석하고, 내용을 탐색하며, Waveform Audio(`.wav`) 파일로 추출하는 프로그램입니다. 명령줄(CLI) 버전과 그래픽 사용자 인터페이스(GUI) 버전을 모두 제공합니다. <BR> <BR>

**GUI v3.0.0 버전부터는** 단순 추출을 넘어, 원하는 오디오로 교체하여 다시 패키징하는 **'리빌드(Rebuild)' 기능이 추가**되었습니다. 이에 맞춰 프로젝트 이름도 **FSB/BANK Extractor & Rebuilder**로 변경되었습니다. <BR> <BR>

⚠️ **참고:** 이 프로그램은 zenhax.com 포럼의 id-daemon 님이 작성한 `fsb_aud_extr.exe` ([게시글 링크](https://zenhax.com/viewtopic.php@t=1901.html))에서 아이디어를 얻어, C++과 C#으로 재구현한 프로젝트입니다. <BR> <BR>

---

📢 **개발 상태 안내**<BR>
현재 C++ 및 C#으로 작성된 **CLI 버전**의 신규 기능 개발은 **중단**된 상태입니다. <BR>
CLI 환경에서의 사용이 필요하시다면, 마지막 안정 버전인 **[v1.1.0](https://github.com/IZH318/FSB-BANK-Extractor-Rebuilder/releases/tag/v1.1.0)** 릴리스를 사용해 주시기 바랍니다. <BR>
추후 CLI 버전 업데이트가 재개될 경우 다시 안내해 드리겠습니다. <BR>

---

**영문 버전의 README는 [README_EN.md](README_EN.md)에서 확인하실 수 있습니다.** <BR>
**You can find the English version of the README in [README_EN.md](README_EN.md).**

<BR>

## 🔍 주요 기능 및 개선 사항

- **공통 개선 사항**

   - **확장된 파일 처리 기능:**
       - **Bank 파일 지원 (.bank):** FSB 파일뿐만 아니라, `.bank` 파일에 포함된 내부 FSB 데이터까지 직접 분석하고 처리합니다. (기존 프로그램은 FSB 파일만 지원) <BR> <BR>

   - **향상된 출력 제어:**
       - **다양한 출력 디렉토리 옵션:** 명령줄 인수 또는 GUI 옵션을 통해 WAV 저장 위치를 유연하게 선택할 수 있습니다 (`-res`, `-exe`, `-o` 옵션).
       - **자동 하위 폴더 생성:** 원본 파일명을 기준으로 하위 폴더를 자동으로 생성하여 추출된 파일을 체계적으로 분류 및 저장합니다.
       - **개선된 WAV 파일 이름 생성:** FSB 내부의 Sub-Sound 이름을 활용하여 파일명을 지정하므로, 추출 후 파일 식별이 용이합니다.
       - **사용자 맞춤형 출력, 체계적인 파일 구성, 효율적인 워크플로우 지원.** <BR> <BR>

   - **강력한 오류 처리 및 검증:**
       - **Verbose Logging:** 상세 로그(명령줄 인수 `-v` 또는 GUI 체크박스 활성화)를 통해 심층 분석 및 디버깅을 지원합니다.
       - **로그 레벨 구분:** INFO, WARNING, ERROR 레벨로 로그를 분류하여 효율적인 문제 식별이 가능합니다.
       - **진행률 표시 (CLI & GUI):** CLI 환경에서는 텍스트로, GUI에서는 시각적인 바로 작업 진행 상태를 명확하게 제공합니다.
       - **향상된 디버깅, 오류 추적, 사용자 피드백 강화.** <BR> <BR>

   - **국제화 지원:**
       - **유니코드 완벽 지원:** UTF-8 인코딩을 사용하여 다국어 파일 경로와 내부 사운드 이름을 완벽하게 호환합니다.
       - **파일명 호환성 강화:** 파일 이름에 사용할 수 없는 특수 기호를 호환 가능한 문자로 자동 변환하여 파일 시스템 오류를 예방합니다.
       - **글로벌 호환성, 데이터 손실 방지, 폭넓은 사용자 지원.** <BR> <BR>

   - **향상된 코드 품질 및 유지보수성:**
       - **최신 언어 (C++, C#) 및 OOP 디자인:** 확장 용이성을 고려한 객체 지향 설계로 작성되었습니다.
       - **안정적인 리소스 관리 (RAII/using):** 자동 리소스 관리 기법을 적용하여 메모리 누수를 방지하고 프로그램 안정성을 높였습니다.
       - **최신 FMOD Engine 버전 사용:** 최신 FMOD Engine(CLI: v2.03.06, GUI: v2.03.11)을 사용하여 최신 기능과 안정성을 활용합니다.
       - **향상된 코드 품질, 쉬운 유지보수, 프로그램 안정성 증대, 최신 FMOD 엔진 기능 활용.** <BR> <BR>

- **CLI 버전 개선 사항**

   - **명령줄 옵션을 통한 출력 제어:** `-res`, `-exe`, `-o` 명령줄 인수를 통해 유연한 출력 디렉토리 선택 기능을 제공합니다.
   - **텍스트 기반 진행률 표시기:** 대용량 파일 처리 시, 텍스트 기반의 진행률 업데이트를 제공합니다.
   - **명령줄 제어 강화, CLI 환경 피드백 개선, 명령줄 워크플로우 및 자동화 작업 최적화.** <BR> <BR>

- **GUI 버전 개선 사항**

   - **오디오 리빌드 및 리패킹:**
       -   `.bank` 또는 `.fsb` 파일 내의 특정 오디오를 사용자가 원하는 다른 오디오 파일(WAV, MP3, OGG 등)로 **교체**하는 기능을 추가했습니다.
       -   FMOD 공식 명령줄 도구인 `fsbankcl.exe`와의 통합을 통해 원본과 완벽하게 호환되는 FSB 파일을 안정적으로 생성합니다.
       -   **이진 탐색(Binary Search) 알고리즘**을 도입하여, 원본 파일의 데이터 크기를 초과하지 않는 최적의 압축 품질을 자동으로 찾아냅니다.
   - **실시간 오디오 분석기 추가 (Tools 메뉴):**
       - **종합 시각화:** 파형, 스펙트럼, 스펙트로그램, 벡터스코프, 오실로스코프를 실시간으로 렌더링합니다.
       - **정밀 측정:** 채널별 RMS/Peak 미터링, 클리핑 카운터, DC 오프셋을 제공합니다.
       - **라우드니스 분석:** EBU R 128 등 방송 표준에 기반한 LUFS 및 True Peak(dBTP)를 측정합니다.
   - **대량 파일 관리 및 인덱스 도구 (Index Tools):** 
       -   수천 개의 오디오 파일 중 특정 번호(Index)로 즉시 이동하는 **Jump to Index** 기능과, 범위(`100-200`) 입력을 통해 대량의 파일을 한 번에 선택하는 **Select Range** 기능을 지원하여 작업 효율을 극대화했습니다.
   - **오디오 미리듣기 시스템:** 
       -   추출 과정을 거치지 않고도 프로그램 내에서 즉시 오디오를 재생(Play), 일시정지(Pause), 정지(Stop)할 수 있습니다.
       -   **탐색 바**와 **볼륨 조절**, **루프(Loop) 강제 재생** 옵션을 통해 오디오 데이터를 세밀하게 검증할 수 있습니다.
   - **Strings Bank 통합 지원:** 
       -   `.strings.bank` 파일을 자동으로 감지하거나 **수동으로 로드**하여, 암호화된 GUID(예: `{a1b2...}`)로 표시되던 항목들을 개발자가 지정한 **실제 이벤트 이름**으로 자동 변환하여 표시합니다.
   - **실시간 검색 및 고급 내비게이션:** 
       -   최적화된 검색 엔진을 탑재하여, 수천 개의 오디오 노드 중 이름이 일치하는 항목만 리스트 형태로 신속하게 필터링하여 보여줍니다.
       -   검색 결과에서 **Open File Location** 기능을 사용하면, 트리 구조 내의 원본 위치로 즉시 이동하여 파일의 맥락을 파악할 수 있습니다.
   - **통합 상세 정보 패널:** 
       -   별도의 팝업 창을 띄우는 불편함 없이, 항목을 클릭하면 우측 패널에서 포맷(PCM, ADPCM 등), 채널, 비트레이트, 루프 구간, GUID, 원본 경로 등의 메타데이터를 즉시 확인할 수 있습니다.
   - **데이터 관리 및 내보내기:** 
       -   **CSV 파일 추출:** 현재 로드된 모든 파일의 구조와 상세 속성을 CSV 파일로 내보낼 수 있습니다.
       -   **체크박스 기반 추출:** 원하는 항목만 체크박스로 선택하여 일괄 추출할 수 있습니다.
       -   추출 위치를 '원본 파일과 동일한 경로', '사용자 지정 고정 경로', '매번 묻기' 중에서 유연하게 선택할 수 있습니다.
   - **사용자 편의성 및 워크플로우:**
       - **드래그 앤 드롭 지원:** 탐색기에서 파일 및 폴더를 프로그램 창으로 직접 끌어와 간편하게 로드할 수 있습니다.
       - **단축키 지원:** 파일 열기(Ctrl+O), 검색(Ctrl+F), 추출(Ctrl+E) 등 주요 기능에 대한 단축키를 완벽하게 지원합니다.
   - **성능 최적화:** 
       -   **병렬 스캔 및 비동기 처리:** `Parallel.ForEach`와 `async/await`를 전면 도입하여 대량의 파일/폴더를 불러올 때 UI 프리징 없이 고속으로 분석합니다.
       -   **저수준 바이너리 파싱:** FSB 헤더를 직접 분석하여 일부 압축 포맷에서 더 정확한 오디오 데이터 정보를 확보하고, 분석 오류를 줄였습니다.

<BR>

## 🔄 업데이트 내역

### v3.0.0 (2025-12-09) (GUI Only)
v3.0.0 업데이트는 핵심적인 신규 기능 추가와 함께, 내부 코드 구조를 대대적으로 개선하는 데 중점을 두었습니다. **(CLI 버전은 변경 사항 없음)**

-   #### **✨ 신규 기능**
    -   **오디오 리빌드 & 리패킹 시스템:**
        -   `.bank` 또는 `.fsb` 파일 내의 특정 오디오를 사용자가 제공한 다른 오디오 파일(WAV, MP3 등)로 교체하는 기능을 추가했습니다.
        -   FMOD 공식 명령줄 도구인 `fsbankcl.exe`를 사용하여 원본과 호환되는 FSB 파일을 생성합니다.
        -   **이진 탐색(Binary Search) 알고리즘**으로 원본 데이터의 크기 제한에 맞는 최적의 압축 품질(Vorbis)을 자동으로 찾아내, 파일 손상을 방지합니다.
        -   (참고) 용량이 작을 경우 **자동으로 제로 패딩(Zero-Padding)을 적용**하여 원본 파일 구조를 완벽히 유지합니다.
    -   **실시간 오디오 분석기:**
        -   재생 중인 사운드를 상세히 분석할 수 있는 도구를 추가했습니다. (Tools 메뉴)
        -   **시각화 도구:** 파형, 스펙트럼, 스펙트로그램, 벡터스코프, 오실로스코프를 실시간으로 표시합니다.
        -   **채널 측정:** RMS/Peak 레벨, 클리핑 카운터, DC 오프셋을 포함한 채널별 상세 통계를 제공합니다.
        -   **라우드니스 분석:** EBU R 128 등 주요 방송 표준에 기반한 Integrated/Short-term/Momentary LUFS 및 True Peak(dBTP)를 측정합니다.

-   #### **🚀 편의성 개선**
    -   **추출 경로 옵션 추가:** 추출 위치를 '원본 파일과 동일한 경로', '사용자 지정 고정 경로', '매번 묻기' 중에서 선택할 수 있는 옵션을 추가했습니다.
    -   **오디오 길이 검증 추가:** 리빌드 시, 교체할 오디오의 재생 시간이 원본보다 길 경우 게임 내에서 발생할 수 있는 동작 오류(이벤트 끊김, 루프 깨짐 등)를 사전에 경고하는 기능을 추가했습니다. 용량 초과 시 경고와 함께 **독립 파일(.fsb)로 강제 저장할 수 있는 옵션**을 제공합니다.
    -   **컨텍스트 메뉴 개선:** 우클릭 시, 선택한 항목의 종류에 따라 '추출', '리빌드', 'GUID 복사' 등 관련된 메뉴만 표시되도록 변경했습니다.

-   #### **🛠️ 내부 구조 개선**
    -   **비동기 처리(Async/Await) 적용:** 파일 로딩, 추출, 리빌드 등 시간이 오래 걸리는 I/O 작업을 비동기 방식으로 전환하여, 대용량 처리 시에도 UI가 멈추지 않도록 개선했습니다.
    -   **코드 모듈화:** 관련 UI 로직을 별도 폼(`IndexToolForm` 등)으로 분리하고 `NodeData` 클래스에 다형성을 적용하는 등, 향후 기능 추가 및 유지보수가 용이하도록 코드 구조를 개선했습니다.
    -   **타이머 로직 변경:** 고정밀 백그라운드 타이머(`System.Threading.Timer`)를 사용하도록 변경하여, FMOD 엔진 업데이트와 UI 갱신 로직을 최적화하고 시스템 부하를 줄였습니다.

-   #### **⚡️ 성능 및 안정성 향상**
    -   **저수준 바이너리 파싱 도입:** FMOD API에만 의존하지 않고 FSB 헤더를 직접 읽어, 일부 압축 포맷에서 더 정확한 오디오 데이터 오프셋과 길이를 가져오도록 수정했습니다.
    -   **오류 처리 및 로깅 개선:** 예외 발생 시 스택 트레이스를 포함한 상세 오류 로그를 자동으로 생성하고, 추출 및 리빌드 과정의 로그를 더욱 체계적으로 기록하도록 변경했습니다. (작업 중 오류 발생 시 **Verbose 옵션과 관계없이 상세한 에러 로그 파일이 자동으로 생성**됩니다.)

<BR>

<details>
<summary>📜 이전 업데이트 내역 - 클릭하여 열기</summary>
<BR>
<details>
<summary>v2.1.0 (2025-11-26) - GUI Only</summary>
   
사용자의 요청과 피드백을 반영하여 **대량의 오디오 파일 관리 효율성을 극대화**하는 기능들이 추가되었습니다. **(CLI 버전은 변경 사항 없음)**

-   #### **🔧 인덱스 도구 (Index Tools)**
    -   **Sub-Sound Index 지원:** 이제 파일 목록에서 각 오디오의 내부 인덱스 번호를 확인할 수 있습니다.
    -   **범위 선택:** 수동으로 하나씩 체크할 필요 없이, `100-200`과 같은 범위나 `10, 20` 같은 쉼표 입력을 통해 수백 개의 파일을 한 번에 선택(Check)할 수 있습니다.
    -   **인덱스 이동:** 특정 번호(Sub-Sound Index)를 입력하면 해당 오디오 파일의 위치로 스크롤이 이동하고 포커스가 맞춰집니다.
    -   **스마트 입력 감지:** 입력창에 `,` 나 `-` 같은 기호가 포함되면 자동으로 **Select Range** 모드로, 숫자만 입력되면 **Jump to Index** 모드로 전환되어 불필요한 클릭을 줄였습니다.

-   #### **🔎 검색 기능 강화**
    -   **파일 위치 열기:** 검색 결과 리스트에서 항목을 우클릭하여 `Open File Location`을 선택하면, 트리 뷰 화면으로 전환되며 해당 파일이 위치한 실제 폴더 경로가 펼쳐지고 파일이 강조됩니다.
    -   **일관된 메뉴 UI:** 검색 결과의 우클릭 메뉴를 메인 트리 뷰와 동일한 구성(추출, 복사 등)으로 개편하여 사용자 경험(UX)을 통일했습니다.

-   #### **🛠 기타 개선 사항**
    -   **안전성 향상:** 오디오 파일이 없는 빈 컨테이너나 상위 노드에서 잘못된 작업을 시도할 때 안내 메시지를 표시하도록 예외 처리를 강화했습니다.
    -   **도움말 업데이트:** 새로운 기능(Index Tools, Context Menu)에 대한 설명이 도움말(F1)에 추가되었습니다.
</details>

<details>
<summary>v2.0.0 (2025-11-25) - GUI Only</summary>
GUI 버전이 단순한 '추출기'를 넘어, <b>'FMOD 오디오 종합 분석 도구'</b> 로 새롭게 개편되었습니다. <b>(CLI 버전은 변경 사항 없음)</b><BR><BR>

-   #### **🖥️ 인터페이스 및 경험**
    -   **Structure Explorer 도입**: 기존의 평면적인 리스트 뷰를 제거하고, FMOD Bank의 내부 계층 구조를 완벽하게 시각화하는 트리 뷰 인터페이스를 적용했습니다.
    -   **통합 메인 윈도우**: 팝업 창으로 뜨던 상세 정보(Details Form)를 메인 윈도우 우측 패널로 통합하여 탐색과 정보 확인이 동시에 가능해졌습니다.
    -   **아이콘 시스템 적용**: 파일, 폴더, 이벤트, 파라미터, 오디오 등 각 노드 타입에 맞는 아이콘을 적용하여 시인성을 높였습니다.
    -   **상태 표시줄 개선**: 현재 처리 중인 파일명, 전체 진행률, 경과 시간, 볼륨 상태 등을 실시간으로 표시합니다.

-   #### **🔊 오디오 재생 및 제어**
    -   **인앱 플레이어**: 추출하지 않고도 FMOD 엔진을 통해 직접 사운드를 미리 들어볼 수 있습니다.
    -   **재생 제어**: 재생/일시정지/정지 버튼 및 탐색 바(Seek Bar)를 통한 구간 이동을 지원합니다.
    -   **루프(Loop) 지원**: 원본 파일에 루프 포인트가 있는 경우, `Force Loop` 옵션을 통해 반복 재생 여부를 테스트할 수 있습니다.
    -   **자동 재생**: `Auto-Play on Select` 옵션 활성화 시, 항목을 클릭하자마자 오디오를 재생합니다.

-   #### **💾 데이터 처리 및 추출**
    -   **Strings Bank 지원**: `.strings.bank` 파일을 로드하여 난수화된 GUID를 실제 이벤트 이름으로 복원하는 매핑 로직을 추가했습니다. (수동 로드 메뉴 지원)
    -   **CSV 내보내기**: 파일 목록, 경로, 포맷, 길이, GUID 등 상세 정보를 엑셀 호환 CSV 파일로 저장하는 기능을 추가했습니다.
    -   **선택 추출 강화**: 체크박스를 통해 원하는 파일만 선별하여 추출하거나, 검색 결과만 따로 추출할 수 있습니다.

-   #### **⚡ 성능 및 최적화**
    -   **병렬 로딩 시스템**: 폴더 단위 로드 시 `Parallel.ForEach` 멀티스레딩을 적용하여 수천 개의 파일을 분석하는 속도를 획기적으로 단축했습니다.
    -   **검색 최적화**: 검색어 입력 시 반응 속도를 높여 쾌적한 검색 환경을 제공합니다.
    -   **메모리 누수 방지**: 프로그램 종료(`OnFormClosing`) 시 FMOD 시스템 리소스를 강제로 해제하고, 임시 리소스를 정리하는 클린업 프로세스를 강화했습니다.
    -   **FMOD Studio API 통합**: 기존 Core API뿐만 아니라 Studio API를 함께 사용하여 Bank 파일의 이벤트 구조까지 분석할 수 있도록 엔진을 업그레이드했습니다.

-   #### **⌨️ 편의 기능**
    -   **단축키 추가**: `Ctrl+O`(파일 열기), `Ctrl+Shift+O`(폴더 열기), `Ctrl+E`(선택 추출), `Ctrl+Shift+E`(전체 추출), `Ctrl+Shift+C`(CSV 내보내기), `Ctrl+F`(검색), `F1`(도움말).
    -   **컨텍스트 메뉴**: 트리 노드 우클릭 시 재생, 정지, 추출, 이름/경로/GUID 복사 메뉴를 제공합니다.

</details>

<details>
<summary>v1.1.0 (2025-11-18)</summary>
이번 업데이트는 파일 추출 시 발생할 수 있는 데이터 손실을 방지하고, 추출된 파일의 정리 편의성을 대폭 향상시키는 데 중점을 두었습니다.

-   #### **✨ 신규 기능**
    -   **FMOD 태그 기반 폴더 자동 생성**: FMOD 사운드 파일 내에 포함된 "language" 태그를 읽어, 'EN', 'JP' 등 언어 코드에 맞는 하위 폴더를 자동으로 생성하고 해당 폴더에 파일을 저장합니다. 다국어 오디오를 포함하는 파일을 보다 체계적으로 관리할 수 있습니다.
-   #### **🛠️ 개선 및 수정 사항**
    -   **파일 덮어쓰기 방지 기능 추가**: 하나의 FSB/BANK 파일 내에 동일한 이름을 가진 하위 사운드가 여러 개 있을 경우, 기존에는 파일이 덮어쓰여 데이터가 유실되었습니다. 이제 `_1`, `_2` 와 같은 숫자 접미사를 자동으로 추가하여 모든 사운드가 고유한 파일명으로 안전하게 추출됩니다.
    -   **추출 로직 리팩토링**: 파일명 생성 및 경로 처리 로직을 리팩토링하여 안정성을 높이고, 새로운 기능(태그 기반 폴더링, 덮어쓰기 방지)을 견고하게 지원하도록 구조를 개선했습니다.
</details>

<details>
<summary>v1.0.0 (2025-02-19)</summary>
   
-   #### **기타**
    -   `FSB/BANK Extractor` 게시

</details>
</details>

<BR>

## 💾 다운로드 <BR>
**⚠️ 저작권 및 라이선스 정책 준수를 위해, 이 리포지토리와 배포 파일에는 FMOD API 관련 소스 코드 및 바이너리 파일이 포함되어 있지 않습니다.** <br>
프로그램을 **개발(Dev)** 하거나 **사용(Build)** 하려면, 아래 표를 참고하여 필요한 파일들을 직접 해당 폴더에 복사해야 합니다. <BR>

| Program                                | URL                                                | 필수여부 | 비고                                                                                           |
|----------------------------------------|----------------------------------------------------|----------|------------------------------------------------------------------------------------------------|
| `.NET Framework 4.8`             | [Download](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)   | 선택     | ◼ (오류 발생시 설치) GUI 사용 |
| `Visual Studio 2022 (v143)`            | [Download](https://visualstudio.microsoft.com/)   | 선택     | ◼ (개발자용) 솔루션(프로젝트) 작업 |
| `FMOD Engine API`             | [Download](https://www.fmod.com/download#fmodengine)   | **필수**     | ◼ (공통) 소스 빌드 및 프로그램 실행 시 FMOD SDK의 `api` 폴더와 `bin` 폴더 파일이 필요합니다. |

<BR>

**[ FMOD 파일 배치 현황표 ]**
- **FMOD API 다운로드 경로:** `C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows` (기본값)
- **O 표기:** 해당 환경에서 정상 작동하기 위해 사용자가 직접 파일을 복사해 넣어야 함을 의미합니다.

| 파일명 | 원본 경로 (FMOD 설치 폴더 기준) | `CS` | `CS_GUI` | `CS_GUI (Build)` |
|---|---|:---:|:---:|:---:|
| **fmod.cs** | `api\core\inc` | O | O | |
| **fmod_dsp.cs** | `api\core\inc` | O | O | |
| **fmod_errors.cs** | `api\core\inc` | O | O | |
| **fmod_studio.cs** | `api\studio\inc` | | O | |
| **fmod.dll** | `api\core\lib\x86` | O | O | O |
| **fmodL.dll** | `api\core\lib\x86` | O | | |
| **fmodstudio.dll** | `api\studio\lib\x86` | | O | O |
| **fsbankcl.exe** | `bin` | | O | O |
| **libfsbvorbis64.dll** | `bin` | | O | O |
| **opus.dll** | `bin` | | O | O |
| **Qt6Core.dll** | `bin` | | O | O |
| **Qt6Gui.dll** | `bin` | | O | O |
| **Qt6Network.dll** | `bin` | | O | O |
| **Qt6Widgets.dll** | `bin` | | O | O |

<BR>

**[ 1. 개발 환경 폴더 구조 예시 (Project) ]** <BR>
소스 코드를 수정하거나 빌드하기 위해 `FSB_BANK_Extractor_Rebuilder_CS_GUI` 프로젝트 폴더를 구성할 때의 모습입니다.

```text
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
├─ # FMOD C# Wrapper Files (복사 필요)
├─ fmod.cs
├─ fmod_dsp.cs
├─ fmod_errors.cs
├─ fmod_studio.cs
│
├─ # FMOD Runtime Binaries (복사 필요)
├─ fmod.dll
├─ fmodstudio.dll
│
├─ # FMOD Bank Tool & Dependencies (복사 필요)
├─ fsbankcl.exe
├─ libfsbvorbis64.dll
├─ opus.dll
├─ Qt6Core.dll
├─ Qt6Gui.dll
├─ Qt6Network.dll
└─ Qt6Widgets.dll
```

<BR>

**[ 2. 실행 환경 폴더 구조 예시 (Build / Release) ]** <BR>
프로그램을 다운로드하여 실제로 사용할 때(ZIP 해제 후)의 폴더 모습입니다.
사용자는 아래 목록에 있는 `dll` 및 `exe` 파일들을 직접 구해서 넣어야 합니다.

```text
(사용자 임의 폴더)/
│
├─ FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
├─ FMOD_LICENSE.TXT
├─ README.txt
├─ Newtonsoft.Json.dll
│
├─ # FMOD Runtime Binaries (FMOD Engine 설치 폴더에서 복사)
├─ fmod.dll
├─ fmodstudio.dll
│
├─ # FMOD Bank Tool & Dependencies (FMOD Engine 설치 폴더에서 복사)
├─ fsbankcl.exe
├─ libfsbvorbis64.dll
├─ opus.dll
├─ Qt6Core.dll
├─ Qt6Gui.dll
├─ Qt6Network.dll
└─ Qt6Widgets.dll
```

<BR>

## 🛠️ 개발 환경

**[ 공통 ]**
1. **OS: Windows 10 Pro 22H2 (x64)** <BR>
2. **IDE: Visual Studio 2022 (v143)** <BR> <BR>

**[ C++ CLI 및 C# CLI 버전 ]**
- **API: FMOD Engine (v2.03.06)** <BR>
- C++ 를 사용한 데스크톱 개발 워크로드 필요 <BR>
- C++ 컴파일러는 ISO C++17 표준으로 설정 <BR>
- .NET 데스크톱 개발 워크로드 필요 <BR>
- Windows SDK 버전 10.0 (최신 설치 버전) <BR> <BR>

**[ C# GUI 버전 ]**
- **API: FMOD Engine (v2.03.11)**
- **필요한 NuGet 패키지:**
  - **Newtonsoft.Json:** 이 프로젝트는 Newtonsoft.Json 패키지를 사용합니다. Visual Studio에서 솔루션을 처음 빌드할 때 **자동으로 설치**됩니다.
  - 빌드 오류 발생 시, **솔루션 탐색기**에서 솔루션 파일을 우클릭한 후 **'NuGet 패키지 복원'** 을 실행하거나, **패키지 관리자 콘솔**에서 `Update-Package -reinstall` 명령을 사용하세요.
- .NET 데스크톱 개발 워크로드 필요
- C# 컴파일러는 .NET Framework 4.8 타겟으로 설정

<BR>

## ⏩ 사용 방법

**[ ===== FSB_BANK_Extractor_CLI (C++ 및 C# CLI 버전) ===== ]**

![캡처_2025_02_19_13_50_51_945](https://github.com/user-attachments/assets/a6eca308-23af-4068-ac3a-75543cc6411f) <BR> <BR>

**1. 명령 프롬프트 (cmd.exe) 또는 PowerShell** 을 실행합니다. <BR> <BR>

**2. 프로그램이 위치한 디렉토리로 이동**합니다. <BR>  `cd <프로그램_파일_경로>` 명령어 사용 (예: `cd D:\tools\FSB_BANK_Extractor`) <BR> <BR>

**3. 다음 명령어를 입력하여 프로그램 실행**: <BR>

   - **기본 사용법**: `프로그램.exe <audio_file_path>` <BR>
   
   - **옵션과 함께 사용**: `프로그램.exe <audio_file_path> [Options]` <BR>
   
       - **※ `프로그램.exe`는 C++ CLI exe 파일 또는 C# CLI exe 파일을 지칭합니다.** <BR>
           - C++ 버전: `FSB_BANK_Extractor_CPP_CLI.exe` <BR>
           - C# 버전: `FSB_BANK_Extractor_CS_CLI.exe` <BR> <BR>

   - `<audio_file_path>`: **필수**,  처리할 FSB 또는 Bank 파일의 경로를 입력합니다. <BR>
     **FSB 또는 Bank 파일의 경로**를 입력해야 합니다. <BR>
     (예시: `C:\sounds\music.fsb` 또는 `audio.bank`) <BR> <BR>

   - `[Options]`: **선택 사항**, 필요에 따라 다음 옵션을 선택적으로 사용할 수 있습니다. 각 옵션은 `<audio_file_path>` 뒤에 공백으로 구분하여 추가합니다. <BR>
     - `-res`: **WAV 파일을 FSB/Bank 파일과 동일한 폴더에 저장**합니다. (기본 옵션, 옵션 생략 시 `-res` 와 동일하게 동작) <BR>
       **사용 예시**: `프로그램.exe audio.fsb -res` (`-res` 옵션 생략 가능, `프로그램.exe audio.fsb` 와 동일) <BR>

     - `-exe`: **WAV 파일을 프로그램 실행 파일과 동일한 폴더에 저장**합니다. <BR>
       **사용 예시**: `프로그램.exe sounds.fsb -exe` <BR>

     - `-o <output_directory>`: **WAV 파일을 사용자가 지정한 폴더에 저장**합니다. `<output_directory>` 에는 WAV 파일을 저장할 폴더의 경로를 입력해야 합니다. <BR>
       **사용 예시 (절대 경로)**: `프로그램.exe voices.bank -o "C:\output\audio"` <BR>
       **사용 예시 (상대 경로)**: `프로그램.exe effects.fsb -o "output_wav"` <BR>

     - `-v`: **Verbose Logging 기능을 활성화**합니다. <BR>
       **사용 예시**: `프로그램.exe music.bank -v` <BR> <BR>

   - **[ 💡 사용 팁 ]**
     - **기본 옵션**: 옵션을 생략하고 `프로그램.exe <audio_file_path>` 와 같이 실행하면, `-res` 옵션이 적용됩니다. <BR>
     - **출력 폴더 옵션 중 하나만 선택**: `-res`, `-exe`, `-o <output_directory>` 옵션은 **동시에 사용할 수 없습니다**. <BR>
     - **Verbose Logging 옵션과 조합**: `-v` 옵션은 출력 폴더 옵션과 **함께 사용 가능**합니다. <BR>
     - **-h 또는 -help 옵션**: `프로그램.exe -h` 또는 `프로그램.exe -help` 를 입력하면 도움말을 볼 수 있습니다. <BR> <BR> <BR>



**[ ===== FSB_BANK_Extractor_CS_GUI (C# GUI 버전) ===== ]**

<img width="786" height="593" src="https://github.com/user-attachments/assets/6b1affa3-e0e6-4234-8154-e6dcbd313405" /> <BR> <BR>

**1. `FSB_BANK_Extractor_CS_GUI.exe` 파일을 실행**합니다. <BR> <BR>

**2. GUI 조작**:

   - **파일 및 폴더 불러오기**:
      - 상단 메뉴의 **`File` > `Open File...`** 또는 **`Open Folder...`** 를 클릭하여 파일을 불러옵니다.
      - 또는, 윈도우 탐색기에서 FSB/Bank 파일을 프로그램 화면으로 **드래그 앤 드롭**합니다.
      - **[ 💡 참고 ]** 파일명이 암호화된 GUID로 보인다면 `.strings.bank` 파일을 함께 로드하거나, **`File` > `Load Strings Bank (Manual)...`** 메뉴를 사용하세요. <BR> <BR>

   - **탐색 및 미리듣기**:
      - **Structure Explorer**: 좌측 트리 뷰에서 Bank 내부의 이벤트, 버스, 오디오 등 실제 계층 구조를 확인합니다.
      - **검색 필터**: 상단 **`Search`** 입력창에 텍스트를 입력하면, 일치하는 항목만 리스트 형태로 필터링되어 표시됩니다. 검색 결과에서 항목을 우클릭하여 **`Open File Location`** 을 선택하면 트리 뷰의 원본 위치로 이동할 수 있으며, **즉시 추출 및 리빌드도 수행**할 수 있습니다.
      - **인덱스 도구**: FSB/Bank 노드를 우클릭하여 **`Index Tools...`** 를 실행하면, 특정 인덱스 번호로 점프하거나(`Jump to Index`), 범위(`100-200, 305`)를 지정하여 여러 항목을 한 번에 체크(`Select Range`)할 수 있습니다.
      - **상세 정보**: 항목을 클릭하면 우측 **Details** 패널에서 포맷, 채널, 루프 구간 등의 정보를 실시간으로 확인할 수 있습니다.
      - **오디오 재생**: 하단 패널의 `Play(▶)`, `Stop(■)` 버튼과 탐색 바, 볼륨 슬라이더, `Loop` 체크박스를 사용하여 추출 전 사운드를 확인합니다. `Auto-Play` 체크 시 항목을 선택할 때마다 자동으로 재생됩니다.
      - **데이터 복사**: 트리 뷰 또는 검색 결과에서 항목을 **우클릭**하여 이름(`Copy Name`), 전체 경로(`Copy Path`), 또는 GUID(`Copy GUID`)를 클립보드로 쉽게 복사할 수 있습니다.
      - **트리 뷰 제어**: 아무 곳이나 우클릭하여 **`Expand All`** 또는 **`Collapse All`** 메뉴로 모든 폴더를 한 번에 열거나 닫을 수 있습니다. <BR> <BR>
   
   - **오디오 리빌드 (교체)**:
      - <img width="386" height="253" alt="image" src="https://github.com/user-attachments/assets/f8460282-065c-4baa-8a16-24d8e7698059" />
      - **리빌드 시작**: 교체하고 싶은 오디오 파일을 **우클릭**한 후 **`Rebuild Sound with fsbankcl...`** 메뉴를 선택합니다.
      - **파일 선택 및 옵션 지정**: 새로 교체할 오디오 파일(WAV, MP3 등)을 선택하고, 나타나는 옵션 창에서 압축 포맷(Vorbis, FADPCM, PCM)을 지정합니다.
      - **자동 최적화 (Vorbis 옵션 전용)**: 프로그램이 원본 파일의 데이터 크기에 맞춰 압축 품질을 **자동으로 최적화**하여 파일 구조 손상 없이 안전하게 오디오를 교체하고 새 파일로 저장합니다. <BR> <BR>

   - **파일 추출**:
      - **추출 경로 설정**: 메인 화면 우측 하단의 콤보박스에서 추출 파일이 저장될 기본 위치를 '원본과 동일한 경로(Same as source file)', '사용자 지정 경로(Custom path)', '매번 묻기(Ask every time)' 중에서 선택합니다.
      - **선택 추출**: **Structure Explorer**(트리 뷰) 또는 **검색 결과 리스트**에서 원하는 항목의 **체크박스**를 선택합니다. 이후 **`File` > `Extract Checked...`** 를 클릭하여 저장할 폴더를 지정합니다. (단축키: `Ctrl + E`)
      - **전체 추출**: **`File` > `Extract All...`** 를 클릭하여 현재 로드된 모든 항목을 한 번에 추출합니다. (단축키: `Ctrl + Shift + E`) <BR> <BR>

   - **분석 도구 및 기타 옵션**:
      - **실시간 오디오 분석기**:
         - <img width="706" height="513" alt="image" src="https://github.com/user-attachments/assets/b17f9845-60f5-46ec-ba9c-f0e41239b235" />
         - 상단 메뉴의 **`Tools` > `Audio Analyzer...`** 를 클릭하여 분석 창을 엽니다. 오디오 재생 시 파형, FFT 스펙트럼, **LUFS**, **True Peak(dBTP)** 등 전문적인 데이터를 실시간으로 확인할 수 있습니다.
      - **CSV 내보내기**: **`File` > `Export List to CSV...`** 를 통해 파일 목록을 엑셀 호환 파일로 저장합니다. (단축키: `Ctrl + Shift + C`)
      - **Verbose Logging**: 하단의 **`Verbose Log`** 체크 박스를 활성화하면 추출 또는 리빌드 과정의 상세 로그가 파일로 저장됩니다.
      - **도움말 및 정보**: **`File` > `Help`** 메뉴 또는 **`F1`** 키를 눌러 프로그램의 전체 기능 설명을 다시 확인할 수 있으며, **`File` > `About`** 메뉴에서 프로그램 버전 정보를 볼 수 있습니다. <BR>

<BR>

## ⚖️ 라이선스

- **FMOD**
   - 본 프로젝트는 개인적, 비상업적 용도로 제작되었으며, Firelight Technologies Pty Ltd.에서 제공하는 **FMOD 엔진 라이선스 계약**의 적용을 받는 FMOD 엔진을 포함하고 있습니다.
   
   - 본 프로젝트에 대한 **FMOD 엔진 라이선스 계약 전문**은 **FMOD_LICENSE.TXT** 파일에 포함되어 있습니다.
   
   - **본 프로젝트에 적용되는 FMOD 엔진 라이선스의 구체적인 조건 및 조항은 FMOD_LICENSE.TXT 파일을 참조하시기 바랍니다.**
   
   - FMOD 라이선스에 대한 일반적인 정보는 공식 FMOD 웹사이트 ([FMOD Licensing](https://www.fmod.com/licensing)) 및 일반적인 **FMOD 최종 사용자 라이선스 계약 (EULA)** ([FMOD End User License Agreement](https://www.fmod.com/licensing#fmod-end-user-license-agreement)) 에서 확인하실 수 있습니다.
   
   - **본 프로젝트에서 FMOD 엔진 사용과 관련된 주요 사항 (요약 - 자세한 내용은 FMOD_LICENSE.TXT 파일 참조):**
     
      - **라이선스:** **FMOD_LICENSE.TXT** 파일은 본 프로젝트의 FMOD 엔진 라이선스에 대한 최종적인 라이선스 조건을 담고 있습니다.
      - **비상업적 용도:** 본 프로젝트는 개인적, 교육적 또는 취미 목적으로만 사용될 수 있으며, 첨부된 **FMOD_LICENSE.TXT** 파일의 조건에 따라 비상업적 용도로 라이선스가 허여되었습니다. 상업적 사용, 수익 창출 또는 어떠한 형태의 금전적 이익을 위한 용도로는 사용될 수 없습니다.
      - **저작자 표시 (프로그램 배포 시):** 라이선스에서 허용하는 비상업적 목적으로 FMOD 엔진으로 빌드된 프로그램을 배포하는 경우, 일반적인 FMOD EULA 및 **FMOD_LICENSE.TXT** 파일에 명시된 바에 따라 프로그램 내에 "FMOD" 및 "Firelight Technologies Pty Ltd." 저작자 표시를 포함해야 합니다.
      - **재배포 제한:** 본 프로젝트에서 FMOD 엔진 구성 요소의 배포는 **FMOD_LICENSE.TXT** 파일 및 일반적인 FMOD EULA에 명시된 조건을 따릅니다. 일반적으로 비상업적 맥락에서 런타임 라이브러리만 재배포가 허용됩니다. <BR> <BR>

- **본 프로젝트에서 사용 된 아이콘:**

  - **아이콘 이름:** Unboxing icons
   - **제작자:** Graphix's Art
   - **제공처:** Flaticon
   - **URL:** https://www.flaticon.com/free-icons/unboxing <BR> <BR>

- **프로젝트 코드 라이선스**

   - FMOD Engine 및 아이콘 자체를 제외한 본 프로젝트의 코드는 **GNU General Public License v3.0** 하에 라이선스가 부여됩니다.

<BR>

## 👏 Special Thanks To & References

-   **[FMOD FSB files extractor (through their API)](https://zenhax.com/viewtopic.php@t=1901.html)**
    -   zenhax.com 포럼의 **id-daemon** 님이 제작한 `fsb_aud_extr.exe`는 본 도구의 핵심 아이디어를 제공한 중요한 레퍼런스입니다.
-   **[Redelax](https://github.com/Redelax)**
    -   파일명 중복 시 데이터가 덮어쓰여 유실되는 문제점을 제보해주셨습니다. 덕분에 프로그램을 더욱 안정적으로 개선할 수 있었습니다.
-   **[TigerShota](https://github.com/TigerShota)**
    -   Sub-Sound Index 기반의 범위 선택 기능, 인덱스로 바로 이동하는 기능, 검색 결과에서 파일 위치를 여는 기능을 제안해주셨습니다.
-   **[immortalx74](https://github.com/immortalx74)**
    -   대량 파일 처리를 위한 다중 선택 기능의 필요성을 제안해주셨습니다. 해당 피드백을 참고하여 Sub-Sound Index 범위 선택 기능으로 적절히 구현했습니다.
    -   오디오 데이터를 교체하는 리빌드(Rebuild) 기능의 핵심 원리를 제안해주셨습니다. "교체될 사운드는 원본과 크기가 같거나 작아야 하며, 원본 순서(Index)를 유지한 채 바이너리 단위에서 교체해야 한다"는 아이디어를 제공해주셨고, 이 개념은 현재의 안정적인 리빌드 시스템을 구현하는 데 결정적인 기반이 되었습니다.
