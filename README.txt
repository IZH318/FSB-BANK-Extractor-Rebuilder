## 프로그램 실행 안내 / Program Execution Guide



===== [ 한국어 / Korean ] =====

※ FSB_BANK_Extractor_Rebuilder_CS_GUI.exe가 정상적으로 실행되지 않는다면, 먼저 .NET Framework 4.8이 설치되어 있는지 확인해 주십시오.

본 프로그램은 배포된 ZIP 파일 내에 실행에 필요한 모든 파일(FMOD 라이브러리, 리빌드 도구 등)이 포함되어 있습니다.
압축을 풀고 파일들을 분리하지 않은 상태에서 실행해야 합니다.

■ 필수 구성 파일 (삭제 및 이동 금지):
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (실행 파일)
- fmod.dll (FMOD Core 라이브러리)
- fmodstudio.dll (FMOD Studio 라이브러리)
- fsbankcl.exe (오디오 리빌드 기능을 위한 필수 도구)
- Newtonsoft.Json.dll (JSON 데이터 처리 라이브러리)

- [기타 필수 라이브러리]
- libfsbvorbis64.dll
- opus.dll
- Qt6Core.dll
- Qt6Gui.dll
- Qt6Network.dll
- Qt6Widgets.dll

※ FMOD 버전 정보: 2.03.11 - Studio API minor release (build 158528)


● 사용 방법

1. 압축 풀기:
   - 다운로드한 ZIP 파일의 압축을 원하시는 폴더에 풉니다.

2. 파일 확인:
   - 압축이 풀린 폴더 안에 실행 파일(.exe)과 위 목록의 모든 파일들이 함께 있는지 확인합니다.

   [올바른 폴더 구조 예시]
   [폴더]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- fmod.dll
   +-- fmodstudio.dll
   +-- fsbankcl.exe
   +-- Newtonsoft.Json.dll
   +-- libfsbvorbis64.dll
   +-- opus.dll
   +-- Qt6Core.dll
   +-- Qt6Gui.dll
   +-- Qt6Network.dll
   +-- Qt6Widgets.dll

3. 프로그램 실행:
   - FSB_BANK_Extractor_Rebuilder_CS_GUI.exe 을 실행합니다.


● 오류 발생 시 (실행이 안 될 경우)

1. 파일 위치 확인:
   - 실행 파일(.exe)만 바탕화면 등으로 따로 꺼내면 실행되지 않습니다.
   - 반드시 위 목록의 모든 파일들이 실행 파일과 같은 폴더에 있어야 합니다.

2. .NET Framework 확인:
   - 윈도우 기능 켜기/끄기 또는 마이크로소프트 홈페이지에서 .NET Framework 4.8이 설치되어 있는지 확인해 주세요.

3. 윈도우 보안 차단 해제:
   - 간혹 윈도우 보안 설정으로 인해 DLL 또는 EXE 파일 로드가 차단될 수 있습니다.
   - 각 파일(특히 DLL)을 우클릭 -> 속성 -> 하단 '차단 해제' 체크 후 적용해 보세요.


● 저작권 및 출처

- FMOD 저작권 정보:
  - 이 프로그램은 FMOD Engine을 사용합니다.
  - Copyright © Firelight Technologies Pty Ltd.

- 아이콘 출처:
  - 이름: Unboxing icons
  - 제작자: Graphix's Art
  - 제공처: Flaticon (https://www.flaticon.com/free-icons/unboxing)



===== [ English ] =====

※ If FSB_BANK_Extractor_Rebuilder_CS_GUI.exe does not launch correctly, please check if .NET Framework 4.8 is installed first.

This program comes as a ZIP package containing all necessary files (including FMOD libraries, rebuild tools, etc.) required for execution.
Please extract the archive and run the program without separating the files.

■ Included Files (Do Not Delete or Move):
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (Executable)
- fmod.dll (FMOD Core Library)
- fmodstudio.dll (FMOD Studio Library)
- fsbankcl.exe (Required tool for audio rebuilding feature)
- Newtonsoft.Json.dll (JSON data handling library)

- [Other Required Libraries]
- libfsbvorbis64.dll
- opus.dll
- Qt6Core.dll
- Qt6Gui.dll
- Qt6Network.dll
- Qt6Widgets.dll

※ FMOD Version: 2.03.11 - Studio API minor release (build 158528)


● How to Use

1. Extract the ZIP:
   - Extract the downloaded ZIP file to a folder of your choice.

2. Verify Files:
   - Ensure that the executable (.exe) and all files listed above are located in the SAME folder.

   [Correct Folder Structure]
   [Folder]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- fmod.dll
   +-- fmodstudio.dll
   +-- fsbankcl.exe
   +-- Newtonsoft.Json.dll
   +-- libfsbvorbis64.dll
   +-- opus.dll
   +-- Qt6Core.dll
   +-- Qt6Gui.dll
   +-- Qt6Network.dll
   +-- Qt6Widgets.dll

3. Run the Program:
   - Launch FSB_BANK_Extractor_Rebuilder_CS_GUI.exe


● Troubleshooting

1. Check File Location:
   - Do NOT move the .exe file alone to the Desktop or another location.
   - It must remain in the same folder with all the files listed above.

2. Check .NET Framework:
   - Ensure .NET Framework 4.8 is installed on your system.

3. Unblock Files:
   - Sometimes Windows Security blocks downloaded DLL or EXE files.
   - Right-click each file (especially DLLs) -> Properties -> Check 'Unblock' at the bottom -> Apply.


● Credits & Copyright

- FMOD Copyright Notice:
  - This program uses FMOD Engine.
  - Copyright © Firelight Technologies Pty Ltd.

- Icon Credits:
  - Name: Unboxing icons
  - Author: Graphix's Art
  - Source: Flaticon (https://www.flaticon.com/free-icons/unboxing)