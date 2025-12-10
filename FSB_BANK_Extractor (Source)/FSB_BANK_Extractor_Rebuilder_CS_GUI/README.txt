## 프로그램 실행 안내 / Program Execution Guide



===== [ 한국어 / Korean ] =====

※ FSB_BANK_Extractor_Rebuilder_CS_GUI.exe가 정상적으로 실행되지 않는다면, 먼저 .NET Framework 4.8이 설치되어 있는지 확인해 주십시오.

⚠️ 중요 안내:
본 프로그램은 저작권 및 라이선스 정책 준수를 위해 FMOD 관련 라이브러리 및 도구 파일이 포함되어 있지 않습니다.
프로그램을 실행하려면 사용자가 직접 필요한 파일들을 구해서 이 폴더에 복사해 넣어야 합니다.

■ 1. 포함된 파일 (기본):
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (실행 파일)
- Newtonsoft.Json.dll (라이브러리)
- README.txt (안내문)
- FMOD_LICENSE.TXT (라이선스)

■ 2. 사용자가 직접 넣어야 할 파일 (누락 시 실행 불가):
※ 모든 파일은 FMOD Engine 설치 폴더에서 가져와야 합니다.

A. [기본 실행용] - FMOD Engine 설치 폴더(`api` 폴더 내)에서 복사하세요.
   - fmod.dll
   - fmodstudio.dll

B. [리빌드 기능용] - FMOD Engine 설치 폴더(`bin` 폴더 내)에서 복사하세요. (선택 사항)
   - fsbankcl.exe
   - libfsbvorbis64.dll
   - opus.dll
   - Qt6Core.dll
   - Qt6Gui.dll
   - Qt6Network.dll
   - Qt6Widgets.dll


● 사용 방법 (설치 순서)

1. 압축 풀기:
   - 다운로드한 ZIP 파일의 압축을 원하시는 폴더에 풉니다.

2. FMOD Engine 다운로드 및 설치:
   - FMOD 공식 홈페이지에서 'FMOD Engine'을 다운로드하여 설치합니다.
   - (기본 경로: `C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows`)

3. 필수 파일 복사:
   - [실행용]: 설치된 FMOD 폴더 내 `api\core\lib\x86` 및 `api\studio\lib\x86` 경로에서 `fmod.dll`, `fmodstudio.dll`을 찾아 복사해 넣습니다.
   - [리빌드용]: 설치된 FMOD 폴더 내 `bin` 폴더에서 `fsbankcl.exe` 및 관련 DLL들을 복사해 넣습니다.

4. 폴더 구조 확인:
   - 파일을 다 넣었을 때 아래와 같은 구조가 되어야 합니다.

   [올바른 폴더 구조 예시]
   [폴더]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- Newtonsoft.Json.dll
   +-- fmod.dll            <-- (FMOD Engine 폴더에서 복사)
   +-- fmodstudio.dll      <-- (FMOD Engine 폴더에서 복사)
   +-- fsbankcl.exe        <-- (FMOD Engine 폴더에서 복사)
   +-- (기타 리빌드용 DLL) <-- (FMOD Engine 폴더에서 복사)

5. 프로그램 실행:
   - FSB_BANK_Extractor_Rebuilder_CS_GUI.exe 을 실행합니다.


● 오류 발생 시

1. 실행 즉시 종료됨 / 오류 메시지 발생:
   - `fmod.dll` 또는 `fmodstudio.dll`이 폴더에 없는 경우입니다. FMOD Engine을 설치하고 파일을 복사했는지 확인하세요.

2. 리빌드(Rebuild) 실패:
   - `fsbankcl.exe` 또는 `Qt6*.dll` 파일들이 없는 경우입니다.

3. 윈도우 보안 차단 해제:
   - 외부에서 가져온 DLL 파일을 윈도우가 차단할 수 있습니다.
   - 각 DLL 파일 우클릭 -> 속성 -> 하단 '차단 해제' 체크 후 적용해 보세요.


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

⚠️ IMPORTANT NOTICE:
Due to copyright and licensing policies, FMOD libraries and tools are NOT included in this package.
You must manually copy the required files into this folder to run the program.

■ 1. Included Files:
- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe (Executable)
- Newtonsoft.Json.dll (Library)
- README.txt
- FMOD_LICENSE.TXT

■ 2. Missing Files (You must copy these):
※ All files must be copied from the FMOD Engine Installation Folder.

A. [For Execution] - Copy from FMOD Engine folder (inside `api` folder):
   - fmod.dll
   - fmodstudio.dll

B. [For Rebuilding] - Copy from FMOD Engine folder (inside `bin` folder): (Optional)
   - fsbankcl.exe
   - libfsbvorbis64.dll
   - opus.dll
   - Qt6Core.dll
   - Qt6Gui.dll
   - Qt6Network.dll
   - Qt6Widgets.dll


● How to Use (Installation Steps)

1. Extract the ZIP:
   - Extract the downloaded ZIP file to a folder of your choice.

2. Download & Install FMOD Engine:
   - Download 'FMOD Engine' from the FMOD official website and install it.
   - (Default Path: `C:\Program Files (x86)\FMOD SoundSystem\FMOD Studio API Windows`)

3. Copy Required Files:
   - [Runtime]: Go to `api\core\lib\x86` and `api\studio\lib\x86` inside the FMOD installation folder, find `fmod.dll` and `fmodstudio.dll`, and copy them here.
   - [Rebuild]: Go to the `bin` folder inside the FMOD installation folder, find `fsbankcl.exe` and related DLLs, and copy them here.

4. Verify Folder Structure:
   - After copying, your folder should look like this:

   [Correct Folder Structure Example]
   [Folder]
   +-- FSB_BANK_Extractor_Rebuilder_CS_GUI.exe
   +-- Newtonsoft.Json.dll
   +-- fmod.dll            <-- (Copied from FMOD Engine)
   +-- fmodstudio.dll      <-- (Copied from FMOD Engine)
   +-- fsbankcl.exe        <-- (Copied from FMOD Engine)
   +-- (Other Rebuild DLLs)<-- (Copied from FMOD Engine)

5. Run the Program:
   - Launch FSB_BANK_Extractor_Rebuilder_CS_GUI.exe


● Troubleshooting

1. Program crashes immediately / Error message:
   - This happens if `fmod.dll` or `fmodstudio.dll` is missing. Ensure you installed FMOD Engine and copied the files.

2. Rebuild Failed:
   - This happens if `fsbankcl.exe` or `Qt6*.dll` files are missing.

3. Unblock Files:
   - Windows might block copied DLL files.
   - Right-click each DLL -> Properties -> Check 'Unblock' at the bottom -> Apply.


● Credits & Copyright

- FMOD Copyright Notice:
  - This program uses FMOD Engine.
  - Copyright © Firelight Technologies Pty Ltd.

- Icon Credits:
  - Name: Unboxing icons
  - Author: Graphix's Art
  - Source: Flaticon (https://www.flaticon.com/free-icons/unboxing)