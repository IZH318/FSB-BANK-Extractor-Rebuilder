## 프로그램 실행 안내 / Program Execution Guide



===== [ 한국어 / Korean ] =====

* FSB_BANK_Extractor_Rebuilder_CS_GUI.exe가 정상적으로 실행되지 않는다면, 먼저 .NET Framework 4.8이 설치되어 있는지 확인해 주십시오.

중요:
본 프로그램은 저작권 및 라이선스 정책 준수를 위해 FMOD 관련 라이브러리 및 도구 파일이 포함되어 있지 않습니다.
프로그램을 실행하려면 사용자가 직접 필요한 파일들을 구해서 이 폴더에 복사해 넣어야 합니다.

1. 아키텍처(x86/x64) 선택
본 프로그램은 'Any CPU'로 빌드되어 x86(32비트) 및 x64(64비트) 운영체제 모두에서 네이티브로 실행됩니다.
프로그램이 어떤 모드(32비트 또는 64비트)로 동작할지는 사용자가 복사하는 FMOD 라이브러리(dll)의 아키텍처에 따라 결정됩니다.
사용하시는 운영체제와 동일한 아키텍처의 파일을 사용하는 것을 강력히 권장합니다. (예: 64비트 윈도우 -> x64용 dll 파일 사용)

2. 포함된 파일 (기본)
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (실행 파일)
- Newtonsoft.Json.dll (라이브러리)
- README.txt (안내문)
- FMOD_LICENSE.TXT (라이선스)

3. 사용자가 직접 넣어야 할 파일 (누락 시 기능 제한)
* 모든 파일은 FMOD Engine 설치 폴더에서 가져와야 합니다.

A. [필수 실행용] - FMOD Engine 설치 폴더의 'api' 경로에서 복사하세요.
   - fmod.dll
   - fmodstudio.dll

B. [리빌드 기능용] - FMOD Engine 설치 폴더의 'bin' 경로에서 복사하세요. (선택 사항)
   - fsbankcl.exe
   - opus.dll
   - Qt6Core.dll
   - Qt6Gui.dll
   - Qt6Network.dll
   - Qt6Widgets.dll


--- 사용 방법 (설치 순서) ---

1. 압축 풀기:
   - 다운로드한 ZIP 파일의 압축을 원하시는 폴더에 풉니다.

2. FMOD Engine 다운로드 및 설치:
   - FMOD 공식 홈페이지에서 'FMOD Engine'을 다운로드하여 설치합니다.
   - 주의: 이 프로그램과 호환되는 FMOD Engine 버전은 릴리즈마다 다를 수 있습니다. 정확한 버전 정보는 깃허브 프로젝트의 README 본문이나, 프로그램을 다운로드하신 릴리즈 페이지의 설명을 참고하여 설치해 주십시오.
   - (기본 경로: C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows)

3. 필수 파일 복사:
   - 사용자의 운영체제에 맞는 아키텍처를 선택하여 아래 경로에서 파일을 복사합니다. (x86과 x64 파일을 섞어서 사용하지 마십시오.)

   [실행용 파일]
   - 64비트(x64)용: 'api\core\lib\x64' 및 'api\studio\lib\x64' 경로에서 fmod.dll, fmodstudio.dll을 복사합니다.
   - 32비트(x86)용: 'api\core\lib\x86' 및 'api\studio\lib\x86' 경로에서 fmod.dll, fmodstudio.dll을 복사합니다.

   [리빌드용 파일]
   - 'bin' 폴더에서 'fsbankcl.exe' 및 위에 명시된 모든 관련 DLL들을 복사해 넣습니다.

4. 폴더 구조 확인:
   - 파일을 다 넣었을 때 아래와 같은 구조가 되어야 합니다.

   [올바른 폴더 구조 예시]
   [폴더]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- Newtonsoft.Json.dll
   +-- fmod.dll            <-- (x64 또는 x86 버전을 여기에 복사)
   +-- fmodstudio.dll      <-- (x64 또는 x86 버전을 여기에 복사)
   +-- fsbankcl.exe        <-- (리빌드 기능 사용 시 복사)
   +-- (기타 리빌드용 DLL) <-- (리빌드 기능 사용 시 복사)

5. 프로그램 실행:
   - FSB_BANK_Extractor_Rebuilder_CS_GUI.exe 을 실행합니다.


--- 오류 발생 시 ---

1. 실행 즉시 종료됨 / 오류 메시지 발생:
   - 'fmod.dll' 또는 'fmodstudio.dll'이 폴더에 없거나 아키텍처가 맞지 않는 경우입니다.
   - (예: 64비트 윈도우에서 32비트(x86)용 dll을 사용하면 성능이 저하될 수 있으며, 반대의 경우 실행되지 않을 수 있습니다.)

2. 리빌드(Rebuild) 실패:
   - 'fsbankcl.exe'가 없거나, 'Qt6*.dll' 같은 의존성 파일들이 누락된 경우입니다. 'bin' 폴더의 관련 파일들을 모두 복사했는지 확인하세요.

3. 윈도우 보안 차단 해제:
   - 외부에서 가져온 DLL 파일을 윈도우가 차단할 수 있습니다.
   - 각 DLL 파일 우클릭 -> 속성 -> 하단 '차단 해제' 체크 후 적용해 보세요.


--- 저작권 및 출처 ---

- FMOD 저작권 정보:
  - 이 프로그램은 FMOD Engine을 사용합니다.
  - Copyright © Firelight Technologies Pty Ltd.

- 아이콘 출처:
  - 이름: Unboxing icons
  - 제작자: Graphix's Art
  - 제공처: Flaticon (https://www.flaticon.com/free-icons/unboxing)



===== [ English ] =====

* If FSB_BANK_Extractor_Rebuilder_CS_GUI.exe does not launch correctly, please check if .NET Framework 4.8 is installed first.

IMPORTANT:
Due to copyright and licensing policies, FMOD libraries and tools are NOT included in this package.
You must manually copy the required files into this folder to run the program.

1. Architecture (x86/x64) Selection
This program is built as 'Any CPU' and runs natively on both x86 (32-bit) and x64 (64-bit) operating systems.
The program's execution mode (32-bit or 64-bit) is determined by the architecture of the FMOD DLLs you copy.
It is strongly recommended to use files that match your OS architecture. (e.g., Use x64 DLLs on a 64-bit Windows OS).

2. Included Files
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (Executable)
- Newtonsoft.Json.dll (Library)
- README.txt
- FMOD_LICENSE.TXT

3. Missing Files (You must copy these)
* All files must be copied from the FMOD Engine Installation Folder.

A. [Required for Execution] - Copy from the 'api' path in your FMOD Engine folder:
   - fmod.dll
   - fmodstudio.dll

B. [For Rebuilding Feature] - Copy from the 'bin' path in your FMOD Engine folder (Optional):
   - fsbankcl.exe
   - opus.dll
   - Qt6Core.dll
   - Qt6Gui.dll
   - Qt6Network.dll
   - Qt6Widgets.dll


--- How to Use (Installation Steps) ---

1. Extract the ZIP:
   - Extract the downloaded ZIP file to a folder of your choice.

2. Download & Install FMOD Engine:
   - Download 'FMOD Engine' from the FMOD official website and install it.
   - Note: The required FMOD Engine version may vary. For the correct version, please refer to the main README on the GitHub project page, or the description on the release page where you downloaded this program.
   - (Default Path: C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows)

3. Copy Required Files:
   - Choose the architecture that matches your OS and copy the files from the paths below. (Do NOT mix x86 and x64 files.)

   [For Runtime]
   - For 64-bit (x64): Copy fmod.dll and fmodstudio.dll from 'api\core\lib\x64' and 'api\studio\lib\x64'.
   - For 32-bit (x86): Copy fmod.dll and fmodstudio.dll from 'api\core\lib\x86' and 'api\studio\lib\x86'.

   [For Rebuilding]
   - Go to the 'bin' folder and copy 'fsbankcl.exe' and all other related DLLs listed above.

4. Verify Folder Structure:
   - After copying, your folder should look like this:

   [Correct Folder Structure Example]
   [Folder]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- Newtonsoft.Json.dll
   +-- fmod.dll            <-- (Copy x64 or x86 version here)
   +-- fmodstudio.dll      <-- (Copy x64 or x86 version here)
   +-- fsbankcl.exe        <-- (Copy if using rebuild)
   +-- (Other Rebuild DLLs)<-- (Copy if using rebuild)

5. Run the Program:
   - Launch FSB_BANK_Extractor_Rebuilder_CS_GUI.exe


--- Troubleshooting ---

1. Program crashes immediately / Error message:
   - This happens if 'fmod.dll' or 'fmodstudio.dll' is missing or has a mismatched architecture.
   - (e.g., Using 32-bit (x86) DLLs on a 64-bit OS may result in performance issues, and the reverse may not run at all.)

2. Rebuild Failed:
   - This happens if 'fsbankcl.exe' is missing, or its dependencies like 'Qt6*.dll' are missing. Ensure all related files from the 'bin' folder are copied.

3. Unblock Files:
   - Windows might block copied DLL files.
   - Right-click each DLL -> Properties -> Check 'Unblock' at the bottom -> Apply.


--- Credits & Copyright ---

- FMOD Copyright Notice:
  - This program uses FMOD Engine.
  - Copyright © Firelight Technologies Pty Ltd.

- Icon Credits:
  - Name: Unboxing icons
  - Author: Graphix's Art
  - Source: Flaticon (https://www.flaticon.com/free-icons/unboxing)