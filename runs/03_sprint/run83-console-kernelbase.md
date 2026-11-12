# 2026-11-13 run83 — 콘솔 입출력, kernelbase 쪽 (Q149a, PE_emulator 요청 27): **`ConsoleInitialize` 는 매개변수 `ConsoleHandle` 값 하나로 길을 가른다. 실제 핸들이면 부모 콘솔에 붙고 (syscall 4 — `NtCreateFile` `\Connect` · `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)` · **`NtDeviceIoControlFile(0x500016, API 0x01000008)`, run68 이 빠뜨린 넷째**), 콘솔 앱인데 0 · −2 · −3 이면 `ConsoleAllocate` 로 새 콘솔 (conhost 는 kernelbase 가 안 띄우고 `ConDrv` 에 IOCTL `0x500037` 로 맡김, syscall 11), −1 이면 연결 없이 commit 만 (syscall 1), 콘솔 앱이 아니면 아무것도 안 만든다. 콘솔 API 는 길이 셋: `WriteFile` · `ReadFile` 은 콘솔 갈림 없이 `NtWriteFile` · `NtReadFile`, `GetConsoleMode` 류는 `NtDeviceIoControlFile(params.ConsoleHandle, 0x500016)`, `WriteConsoleW` · `ReadConsoleW` 는 `NtDeviceIoControlFile(앱이 가진 핸들, 0x500016)` — 데이터는 IOCTL 입력 버퍼가 아니라 "설명서" 안 포인터가 가리키는 곳에 있다.** `_KernelBaseBaseDllInitialize +0x521 ConsoleInitialize` → `+0x177 JZ → +0x142` (`+0x146 ConsoleAllocate`) · `+0x19a ConsoleCreateConnectionObject` → `+0x1ca ConsoleCommitState` → `+0xfa SetTEBLangID` → `GetConsoleLangId +0xe1 NtDeviceIoControlFile` · `WriteFile +0x86 NtWriteFile` · `GetFileType +0x72 NtQueryVolumeInformationFile`

## PE_emulator 쪽에서 쓸 것

- **맨 먼저 — "`ConsoleHandle = 0` 이면 콘솔 없음 → 조용히 성공" 은 틀렸다** `[정적]`. exe 가 콘솔 앱 (`Subsystem 3`) 이고 매니페스트가 없으면 `ConsoleHandle = 0` 은 **`ConsoleAllocate` (새 콘솔 만들기)** 로 간다: `ConsoleInitialize +0x170 MOV RDX,[params+0x10]` · `+0x174 TEST RDX,RDX` · **`+0x177 JZ` → `+0x142`** (call `+0x146` / ret `+0x14b` `ConsoleAllocate`).
  - `ConsoleAllocate` 가 실패하면 `+0x14f ~ +0x162` 가 `ConsoleStateLock` 을 지우고 **AL 0** → `_KernelBaseBaseDllInitialize +0x526 TEST AL,AL` · `+0x528 JZ` → `+0xf6` (AL 0 을 그대로 돌려줌) = **kernelbase 시작 함수 FALSE** (flow-writer `.asm`). 그 뒤 로더가 무엇을 하는지는 이번에 안 봤다 `[추측: 프로세스 초기화 실패]`.
  - 조용히 AL 1 로 끝나는 길은 셋뿐이다: (i) exe `Subsystem ≠ 3` (ii) `ConsoleHandle = −1` (iii) 매니페스트 `consoleAllocationPolicy` 가 `d`/`D` (detached) 이고 `ConsoleHandle` 이 0 (또는 −2).
  - **`[공백]` — PE_emulator 쪽에서 확인할 몫**: PE_emulator 는 `ConsoleHandle` · 표준 핸들 0 으로 은퇴 뒤 `NtWriteFile(0)` 까지 왔다. 이 코드대로면 (가) 진짜 `ConsoleInitialize` 를 안 지났거나 (흉내 몸 `Src/ntdll/BaseDllInit.cpp` 쪽), (나) `ConsoleAllocate` 의 `NtCreateFile` 들과 IOCTL 이 가짜 성공을 돌려줬거나다. 결과서는 "(나) 면 표준 핸들이 0 이 아니어야" 라고 했지만, `ConsoleCommitState` 는 상태 칸 값이 0 이면 안 쓴다 (`+0x112` · `+0x124` · `+0x136 TEST RAX,RAX`) — 가짜 `NtCreateFile` 이 핸들 칸을 안 채우고 성공만 주면 0 그대로일 수 있다 `[추측]`. 그러니 "표준 핸들 0" 만으로는 둘을 못 가른다. 볼 것: kernelbase `ConsoleIsConsoleApplication` (`kernelbase+0x3af188`) 값, `ConsoleInitialize` 반환 `AL`, `ConsoleAllocate +0x150` 의 `NtCreateFile` 상태와 출력 핸들.
- **한 줄 (요청 27 의 kernelbase 몫)**: 에뮬레이터가 콘솔을 흉내 낼 때 중심 syscall 은 셋이다 — **`NtCreateFile` (`\Device\ConDrv\…` 이름 · `RootDirectory` 기준 이름) · `NtDeviceIoControlFile` (IOCTL `0x500016` 하나에 API 번호로 갈림, `0x500023` · `0x500037` 은 연결 때만) · `NtWriteFile` / `NtReadFile`** (곁가지: 연결 때 `NtSetInformationProcess(0x31)` · `NtDuplicateObject` · `NtClose`, 핸들 종류 묻기 `NtQueryVolumeInformationFile`). `WriteFile` 은 kernelbase 안에서 콘솔인지 안 묻는다. `GetFileType` 은 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 의 `DeviceType` 이 `0x50` 이면 "문자 장치" (2). CRT 쪽 (어느 API 를 언제 부르나) 과 (가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표는 다음 바퀴 run84 (Q149b).
  - test.exe 만 보면 콘솔 출력은 **`WriteFile` 하나** — `Log` 가 CRT 없이 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)` 을 부른다 `[소스]` (`sample/log.h:38`). 그러니 test.exe 의 `Log` 길에는 `params.StandardOutput` 에 쓸 수 있는 핸들 하나와 `NtWriteFile` 몸만 있으면 되고 콘솔 IOCTL 은 없다 `[정적 + 소스]`. `/MT` CRT 초기화가 표준 핸들에 무엇을 묻는지 (`GetFileType` 등) 는 run84.
- 질문마다 (결과서 sast/80 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄):

| 질문 (원문 요청 27 번호 · sast/80 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 · 1 콘솔을 얻는 길 셋 | `ConsoleShouldAllocateConsole` 이 FALSE (콘솔 앱 아님 · detached 정책 + 0) 면 아무것도 안 만듦. TRUE 면 `ConsoleHandle` 로: 실제 핸들 → 붙기 (syscall 4), 0 · −2 · −3 → 새 콘솔 (syscall 11), −1 → commit 만 (syscall 1, `ConsoleHandle` 은 0 이 됨). 아래 "질문 1" 표 | **틀림** — 모두 0 이면 (콘솔 앱 · 매니페스트 없음) 새 콘솔 길. 위 "맨 먼저" | `ConsoleInitialize +0x7b` · `+0x83 JNZ` · `+0x89` · `+0xb8` · **`+0xbf JNZ`** · `+0xc5 ~ +0xce` · `+0x166` · **`+0x170 ~ +0x189`** · **`+0x146`** · **`+0x19a`** · **`+0x1ca`**, `ConsoleShouldAllocateConsole +0x16` · `+0x1d JZ` · `+0x46` · `+0x54 JS` · `+0x56 ~ +0x7b` · `+0x96 ~ +0x9b` |
| 2 · 2 자주 쓰는 콘솔 API | 길 셋 — (가) `WriteFile` · `ReadFile` → `NtWriteFile` · `NtReadFile` 바로, (나) `GetConsoleMode` · `SetConsoleMode` · `GetConsoleScreenBufferInfo(Ex)` · `Get/SetConsoleOutputCP` → `NtDeviceIoControlFile(params.ConsoleHandle, 0x500016)`, (다) `WriteConsoleW` · `ReadConsoleW` → `NtDeviceIoControlFile(앱 핸들, 0x500016)`. 아래 표 A · B · C | 엔진의 `NtDeviceIoControlFile` 몸이 **설명서 안 포인터를 따라가** 메시지를 읽고 답을 같은 자리에 써야 한다. `NtWriteFile` 몸은 핸들이 가짜 콘솔 출력이면 화면 쪽으로 | `ConsoleCallServer +0x4d` · `+0x5e/+0x61` · `+0x9a` · `+0xc3` · **`+0xd7`**, `ConsoleCallServerGeneric +0x6f/+0x72` · **`+0x109`**, `WriteConsoleInternal +0x50/+0x55` · `+0x84/+0x87` · `+0xd9` · `+0xe9` · **`+0x10d`**, `GetConsoleMode +0xdb`, `GetConsoleOutputCP +0xde`, `WriteFile +0x39` · `+0x42` · `+0x4b` · **`+0x86`** |
| 3 · 3 `GetFileType` | `NtQueryVolumeInformationFile(h, 4 = FileFsDeviceInformation, 8B)` 하나. `DeviceType 0x50` → 2 (`FILE_TYPE_CHAR`), `0x11` → 3 (PIPE), `2 ~ 8` · `0x24` → 1 (DISK). **핸들 0 → syscall 없이 0** + `ERROR_INVALID_HANDLE` | 표준 핸들 0 이면 `GetFileType` 은 엔진까지 안 온다. 가짜 콘솔 핸들을 만들면 이 질의에 `DeviceType = 0x50` 을 줘야 "콘솔" | `GetFileType +0x15/+0x18 JZ` · `+0x55/+0x58` · `+0x5a R9D 8` · `+0x60 [RSP+0x20] 4` · **`+0x72`** · `+0x87 CMP EAX,0x50` · `+0x8c MOV EAX,2` · 점프표 `+0xb8 ~ +0x105` |
| 4 · 4 표준 핸들 0 일 때 | `GetStdHandle` 은 0 을 그대로 (오류 안 세움 — −1 만 오류). **`WriteFile(0, …)` 은 kernelbase 가 안 막고 `NtWriteFile(0)` 까지 간다**. `WriteConsoleW(0)` · `GetFileType(0)` · `ReadConsoleW(0)` 은 syscall 없이 막힘. `GetConsoleMode` 류는 앱 핸들이 아니라 `params.ConsoleHandle ≤ 0` 으로 막힘 | **맞다** — `NtWriteFile(0)` 26번 `0xc0000008` 은 이 코드와 맞는다 `[정적]`. test.exe 는 CRT 를 안 거치고 `Log` 마다 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)` 을 직접 부른다 `[소스]` (`sample/log.h:38`) → `GetStdHandle +0x36` 이 `params+0x28` (0) → `WriteFile +0x86 NtWriteFile(0)`. 26 이 `Log` 횟수와 같은지는 안 셌다 `[추측]` | `GetStdHandle +0x66/+0x6a` · `+0x1d ~ +0x27` · `+0x49 ~ +0x53` · `+0x98`, `WriteFile +0x39` · `+0x42` · `+0x4b` · **`+0x86`** · `+0x143`, `WriteConsoleInternal +0x84/+0x87` |
| 5 · 5 표준 출력이 파일 · 파이프 | **예 — `WriteFile` 은 `NtWriteFile` 만** (+ `0x103` 이면 `NtWaitForSingleObject`). condrv 가 끼는 건 받는 장치가 `ConDrv` 라서지 kernelbase 가 고른 게 아니다 | **맞다** — 리다이렉트면 kernelbase 쪽은 `NtWriteFile` 하나 | `WriteFile` 의 비교가 `+0x39` · `+0x42` · `+0x4b` (표준 상수) · `+0x63` (`lpOverlapped`) 뿐, `+0x86` · `+0x113` · `+0x1d1` |

### 질문 1 — 콘솔을 얻는 길 (갈래별 syscall 표)

**전체 갈림** (`ConsoleInitialize`, rva `0xbeb60`. run68 은 "붙기" 만 풀었다). "params" = `PEB→ProcessParameters` (`GS:[0x60]+0x20`), `ConsoleHandle` = `params+0x10`.

| 조건 | 가는 곳 (줄) | syscall | 반환 |
|---|---|---|---|
| exe `Subsystem ≠ 3` (GUI 앱) | `+0x7b CMP word [NT+0x5c],3` · `+0x83 JNZ` → `+0x13a` (`ConsoleIsConsoleApplication` ← 0) → `ConsoleShouldAllocateConsole +0x16` · `+0x1d JZ` → FALSE → `+0xbf` 안 뛰고 `+0xc5`: **`+0xce ConsoleHandle ← 0`**, 표준 칸 셋 `ConsoleCloseIfConsoleHandle` (`+0xe2` · `+0xf7` · `+0x100`), `ConsoleConnectionState` 0 (`+0x10a ~ +0x11f`) | 칸이 0 이면 **없음**. 0 이 아니면 칸마다 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) | AL 1 |
| 콘솔 앱 + 매니페스트 정책 `d`/`D` + `ConsoleHandle == 0` (또는 −2) | `ConsoleShouldAllocateConsole +0x56 ~ +0x67` (`'D'` · `'d'` 만) · `+0x76 CMP [params+0x10],0` · `+0x7b JNZ` 안 뜀 → `+0x7d` FALSE → 위와 같은 `+0xc5` (`+0xcc JNZ` 로 `+0xce` 는 건너뜀 — 콘솔 앱이라 `ConsoleHandle` 은 그대로) | 같음 | AL 1 |
| 콘솔 앱 + `ConsoleHandle = −1` | TRUE → `+0x166` → `+0x170` → `+0x179 CMP RDX,-1` · `+0x17d JZ` → `+0x1c6`: **빈 상태로 `ConsoleCommitState`** → `+0x40` 가 `ConsoleHandle ← 0` (flow-writer) | `NtSetInformationProcess(-1, 0x31, {1}, 8)` **1번** (`GetConsoleLangId` 는 `ConsoleHandle` 0 이라 `+0x62 JLE` 로 막힘) | AL 1 |
| 콘솔 앱 + `ConsoleHandle = 0` · −2 · −3 (매니페스트 정책이 없거나 detached 아님) | **`+0x177 JZ`** (0) · `+0x183 JZ` (−2) · `+0x189 JZ` (−3) → `+0x142` → **`+0x146 ConsoleAllocate`** — **(b)** | 아래 (b) 표 | 성공 AL 1, 실패면 `+0x156 RtlDeleteCriticalSection` → **AL 0** → kernelbase 시작 함수 FALSE |
| 콘솔 앱 + `ConsoleHandle` = 실제 핸들 | `+0x19a ConsoleCreateConnectionObject` → `+0x1ca ConsoleCommitState` — **(a)**. test.exe (`ConsoleHandle = 4` `[관찰]` run44) 는 이 길 | 아래 (a) 표 | AL 1 |

- `ConsoleFlags & 4` (`+0x166`) 면 표준 칸 셋을 먼저 `ConsoleCloseIfConsoleHandle` (`+0x213` · `+0x21c` · `+0x225`), `ConsoleFlags & 2` (`+0x1b9`) 면 commit 앞에 `+0x233 ConsoleSanitizeStandardIoObjects` (안은 안 읽음). test.exe 는 `ConsoleFlags = 0` `[관찰]` (`04_baggage-read_P1a.txt:2514`).
- `ConsoleRationalizeAllocationOptionsWithPeb` (`+0x8f`, 부르는 것 없음): `ConsoleHandle` 0 · −2 → 전역 `0x3aa9f8 ← 1`, −3 → 2, −1 → `|= -1`. 이 값이 연결 데이터 `+0x21` 바이트 (`≠ 2`) 로 들어간다. test.exe (4) 는 아무것도 안 쓴다 (run68 과 같음).
- −1 · −2 · −3 이 각각 `DETACHED_PROCESS` · `CREATE_NEW_CONSOLE` · `CREATE_NO_WINDOW` 인지는 `[추측]` — 값을 **쓰는** 쪽 (부모 `CreateProcessInternalW`) 은 안 읽었다.

#### (a) 부모가 물려줌 (`ConsoleHandle` = 실제 핸들) — syscall 표, 차례대로

| # | 부르는 자리 (call/ret) | syscall | 값 | 쓰이는 곳 |
|---|---|---|---|---|
| a1 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` | **`NtCreateFile`** | 이름 `\Connect`, **`RootDirectory` = 물려받은 `ConsoleHandle`** (test.exe 4; NULL 이면 `\Device\ConDrv\Connect`), 접근 `0x12019f`, `Attributes 0x40`, 공유 7, 방식 2 (`FILE_CREATE`), 옵션 `0x20`, **`EaBuffer` = 아래 `0x54b`B** | 새 핸들 → 상태 `+8` (`ConsoleInitialize +0x1b5`). 임시 힙 블록은 `+0x1e1 RtlFreeHeap` |
| a2 | `ConsoleCommitState +0x8a/+0x91` ← `ConsoleInitialize +0x1ca/+0x1cf` | **`NtDeviceIoControlFile`** | 핸들 = a1 새 핸들, IOCTL **`0x500023`** `[추측: 서버 PID 얻기]`, 입력 없음, 출력 8B (= conhost PID) | 상태 `+8` 이 0 이면 건너뜀 (`+0x4e/+0x51 JZ`) |
| a3 | `ConsoleCommitState +0xe9/+0xf0` | **`NtSetInformationProcess`** | `-1`, 클래스 `0x31` (`EPROCESS.OwnerProcessId` 둘째 쓰기 — run51), 값 `PID \| 1`, 8B | 늘 (a2 를 건너뛰어도 값 1 로) |
| a4 | `ConsoleCommitState +0xfa/+0xff` → `SetTEBLangID +0x12/+0x17` → `GetConsoleLangId +0xe1/+0xe8` | **`NtDeviceIoControlFile`** — **run68 에 없던 넷째** | 핸들 = `params.ConsoleHandle` (`+0x4c`, 이제 a1 새 핸들), IOCTL **`0x500016`**, API **`0x01000008`** (`+0xc4`), body 2B (출력 `WORD` = 콘솔 언어 ID) | 성공이면 `SetTEBLangID +0x20/+0x25 SetThreadLocale(LangId)` → `+0x5a` **`TEB+0x108` (`CurrentLocale`) ← LCID** (캐시에 노드가 있으면 syscall 없음 — 아래 "보탠 것" ⑤). 실패면 `+0xf3 RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 하고 넘어감 |

- 실패 갈래: a1 이 `0xc0000022` 면 (`+0x1d4`) `+0x1e3 ConsoleIsCallerInLowbox` (`NtQueryInformationToken`) 가 로우박스라고 할 때 `+0x1fe NtClose(ConsoleHandle)` → `+0x20a JMP` → **(b) 새 콘솔**. 그 밖의 실패는 AL 0.
- **a1 의 `EaBuffer`** (`FILE_FULL_EA_INFORMATION` 한 칸, `0x54b`B — `ConsoleCreateConnectionObject +0xb2` · `+0xc3 ~ +0xe3` · `+0xe6` 복사): `+0x0 NextEntryOffset` 0 · `+0x4 Flags` 0 · `+0x5 EaNameLength` 6 · `+0x6 EaValueLength 0x53c` · `+0x8` 이름 `"server"` (ANSI, NUL 포함 7B, `.rdata 0x2a6248`) · `+0xf` 값 = **연결 데이터 `0x53c`B**. 이름 인자가 있으면 (`"attach"` · `"broker"` — `ConsoleAttachOrAllocate` 길) 둘째 EA 칸이 `+0x54c` 에 붙는다 (`+0x79 JNZ` → `+0x21f`).
- **연결 데이터 `0x53c`B** (`ConsoleInitializeServerData`, rva `0xbfbb4`, `+0x69/+0x6e` 에서 부름. 칸 위치 `[정적]`, 이름 `[추측]`):

| 칸 | 크기 | 내용 (읽는 원본) |
|---|---|---|
| `+0x00` | 4B | `params.ShellInfo` (`+0xd8`) 의 `"dde."` 뒤 숫자 (없으면 0) |
| `+0x04` | 4B | `WindowFlags & 0x200` 이면 `params.StandardInput` 값, 아니면 `ShellInfo` 의 `"hotkey."` 뒤 숫자 |
| `+0x08` | 4B | `params.WindowFlags` (`+0xa4`), 전역 `0x3aa9fc ≠ 0` 이면 `\| 1` |
| `+0x0c` · `+0x0e` | 2B · 2B | `FillAttribute` (`WindowFlags & 0x10`) · 전역 `0x3aaa00` (`ShowWindowFlags`, `0x3aa9fc ≠ 0` 일 때) |
| `+0x10 ~ +0x1a` | 2B × 6 | `CountCharsX/Y` (`& 8`) · `CountX/Y` (`& 2`) · `StartingX/Y` (`& 4`) |
| `+0x1c` | 4B | `params.ProcessGroupId` (`+0x408`, 0 이면 내 PID) |
| `+0x20` · `+0x21` | 1B · 1B | `ConsoleIsConsoleApplication` (`0x3af188`) · 전역 `0x3aa9f8 ≠ 2` |
| `+0x22` / `+0x24` | 2B / ≤ `0x208`B | 창 제목 바이트 수 / 제목 (`params.WindowTitle`; 비면 `"Command Prompt"`) |
| `+0x22e` / `+0x230` | 2B / ≤ `0xfe`B | exe 이름 (`PEB→Ldr` 첫 모듈 `BaseDllName`) |
| `+0x330` / `+0x332` | 2B / ≤ `0x208`B | 현재 디렉터리 (`params.CurrentDirectory.DosPath`) |

- 이 줄기가 PEB 에서 **읽기만** 하는 칸: `+0x10` · `+0x18` · `+0x20` · `+0x30`. PEB 자신에 쓰는 칸은 없다.
- **`ConsoleCommitState` (rva `0xbedb0`) 가 쓰는 칸** (flow-writer `.asm` 으로 줄 맞춤):
  - `params.WindowFlags` 비트 9 (`0x200`) · 10 (`0x400`) 지우기 — `+0x28 BT` · `+0x2c JC` → **`+0x14a`**, `+0x32 BT` · `+0x36 JC` → **`+0x159`**.
  - **`params.ConsoleHandle` ← 상태 `+8`** (`+0x3c` · **`+0x40`**) — 늘 (상태 `+8` 이 0 이어도).
  - `ConsoleConnectionState` (`0x3af120`, `0x38`B) ← 상태 통째 (`+0x96 ~ +0xbb`).
  - **`params.StandardInput` · `Output` · `Error` ← 상태 `+0x18` · `+0x20` · `+0x28`** — `+0xc3 TEST byte [상태],7` 이 0 이 아니고, 칸마다 그 비트 (`1` · `2` · `4`) 가 켜지고 값이 0 이 아닐 때만 (**`+0x11c`** · **`+0x12e`** · **`+0x140`**). 붙기 길은 상태 비트 칸을 안 켜서 (아래 "보탠 것" ④) test.exe 는 안 탄다.
  - `InitializeCtrlHandling` (`+0xf5/+0xfa`, syscall 없음) · `TEB+0x108` (a4 성공일 때).
  - `params.ConsoleFlags` (`+0x18`) 는 **읽기만** (`ConsoleInitialize +0x166` · `+0x1b9`).
- 지금 우리: (a) 는 해당 없음 (부모가 없으니 실제 핸들이 없다). 진짜 VM 이 한 일의 비교용.

#### (b) 새 콘솔 (`ConsoleHandle` = 0 · −2 · −3, 그리고 로우박스 실패) — `ConsoleAllocate` (rva `0xbf41c`, 974B)

- `+0x52 TEST byte [0x7ffe02f0],0x40` — **KUSD `SharedDataFlags` 비트 6 `DbgConsoleBrokerEnabled`** (ntoskrnl PDB 이름, 결과서). 켜지면 `+0x5a JNZ` → `+0x301` → `+0x303/+0x308 ConsoleAttachOrAllocate(NULL)` (중개 길; `0xc0000080` 을 돌려주면 `+0x30d JZ` → `+0x60` 옛 길로). **이 VM 은 `SharedDataFlags = 0x10e` `[관찰]` (`04_baggage-read_P1a.txt:2938`, run68) → 비트 6 = 0 → 옛 길만.** 에뮬레이터의 `0x7ffe02f0` 값도 비트 6 이 꺼져 있어야 같은 길이다.
- 옛 길: conhost 를 **kernelbase 가 직접 띄우지 않는다** — `ConDrv` 서버 장치에 IOCTL `0x500037` 로 "이 매개변수로 띄워 줘" 하고 맡긴다.

| # | 부르는 자리 | syscall | 값 | 쓰이는 곳 |
|---|---|---|---|---|
| b1 | `ConsoleAllocate +0x149/+0x150` | `NtCreateFile` | `\Device\ConDrv\Server` (`.rdata 0x2a62f8`), 접근 `0x12019f`, `Attributes 0x42`, 공유 7, 방식 2, 옵션 0, EA 없음 | 서버 핸들 `[RSP+0x60]` |
| b1′ | `+0x175/+0x17c`, `+0x20b/+0x212` | b1 이 실패할 때만 `NtSetSystemInformation(0x84, {DWORD 1}, 4)` 하고 **b1 한 번 더** | 클래스 `0x84` 의 커널 쪽 뜻 `[공백]` | 또 실패면 그 상태 → `ConsoleInitialize` AL 0 |
| b2 | `+0x2b4/+0x2bb` | `NtCreateFile` | `\Reference` (`0x2a6230`), **`RootDirectory` = b1 서버 핸들**, 접근 `0x12019f`, `Attributes 0x40`, 공유 7, 방식 2, 옵션 `0x20` | 참조 핸들 `[RSP+0x70]` |
| b3 | `ConsoleLaunchServerProcess +0xc6/+0xcd` ← `ConsoleAllocate +0x326/+0x32b` | `NtDeviceIoControlFile` | 핸들 = **b1 서버 핸들**, IOCTL **`0x500037`** `[추측: 서버 띄우기]`, **입력 = `RTL_USER_PROCESS_PARAMETERS`** (`+0x67/+0x6e RtlCreateProcessParametersEx`, 크기 = 블록 `Length` + 4, 정규화 플래그 1), 출력 없음 | 이미지 `\??\<NtSystemRoot>\system32\conhost.exe`, 명령줄 `…conhost.exe 0xffffffff -ForceV1` (`0x2a61d0` 형식, `RtlStringCbPrintfW` `+0x8c`), 데스크톱 = 내 `params.DesktopInfo`. 끝에 `+0xd9 RtlDestroyProcessParameters` |
| b4 | `ConsoleAllocate +0x332/+0x339` | `NtClose` | b1 서버 핸들 | |
| b5 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleAllocate +0x357/+0x35c` | `NtCreateFile` | `\Connect`, **`RootDirectory` = b2 참조 핸들**, EA `"server"` + 연결 데이터 (a1 과 같은 꼴, `0x54b`) | 연결 핸들 → 상태 `+8` (`+0x388`), 참조 핸들 → 상태 `+0x10` (`+0x38f`), 상태 `+0x30` = 연결 핸들 ≠ 0 (`+0x37d` · `+0x380`) |
| b6 | `ConsoleCreateStandardIoObjects +0xb1/+0xb8` ← `ConsoleAllocate +0x39f/+0x3a4` | `NtCreateFile` | `\Input` (`0x2a6298`), `RootDirectory` = 연결 핸들, 접근 `0x12019f`, `Attributes 0x42` (상속), 방식 2, 옵션 `0x20` | 표준 입력 → 상태 `+0x18` |
| b7 | `ConsoleCreateStandardIoObjects +0x151/+0x158` | `NtCreateFile` | `\Output` (`0x2a6288`), 나머지 b6 과 같음 | 표준 출력 → 상태 `+0x20` |
| b8 | `ConsoleCreateStandardIoObjects +0x1a8/+0x1af` | `NtDuplicateObject` | `-1` → b7 핸들 → `-1`, 옵션 6 | 표준 에러 = **출력의 복사본** → 상태 `+0x28`. 상태 `+0` = 7 |
| b9 ~ b11 | `ConsoleCommitState` | a2 · a3 · a4 와 같음 (핸들 = b5 연결 핸들) | | `params.ConsoleHandle` ← 연결 핸들, `params.Standard*` ← b6 ~ b8 (`+0x11c` · `+0x12e` · `+0x140`) |

- 합: 실패 없는 옛 길 = **syscall 11** (b1 ~ b8 의 8 + commit 3).
- `params.WindowFlags & 0x100` (`+0x393` · `+0x39d JNZ`) 면 b6 ~ b8 대신 `+0x3c1/+0x3c6 ConsoleSanitizeStandardIoObjects` (이미 가진 표준 핸들 검사 · 대체 — 안은 안 읽음). 실패면 `+0x3ad/+0x3b2 ConsoleCleanupConnectionState` (`NtClose` 5개까지).
- 중개 길 (비트 6 켜짐): `ConsoleAttachOrAllocate(NULL)` → `ConsoleCreateConnectionObject(root = NULL, EA 이름 "broker", 값 = 내 데스크톱 이름 `0x208`B)` 로 `\Device\ConDrv\Connect` 를 바로 → `\Reference` → b6 ~ b8. conhost 를 띄우는 IOCTL 이 없다. 이 VM 은 안 타서 더 안 읽음.
- **지금 우리**: 이 길이 PE_emulator 의 지금 상태 (콘솔 앱 · `ConsoleHandle` 0) 가 타는 길이다. 가짜 콘솔을 세우려면 **핸들을 돌려주는 `NtCreateFile` 다섯 (b1 · b2 · b5 · b6 · b7) + `NtDuplicateObject` 하나 + IOCTL 세 종류 (`0x500037` · `0x500023` · `0x500016`/`0x01000008`) + `NtSetInformationProcess(0x31)`** 가 대답해야 한다. 하나라도 실패를 돌려주면 kernelbase 시작 함수가 FALSE 다 (위 "맨 먼저").

#### (c) 콘솔 없음

- 어디서 갈리나: ① `Subsystem ≠ 3` ② 매니페스트 detached 정책 + `ConsoleHandle == 0` → 둘 다 `ConsoleInitialize +0xbf JNZ` 가 안 뛰고 `+0xc5` 로. ③ `ConsoleHandle = −1` → 빈 상태 commit (syscall 1).
- 안 하는 것: `NtCreateFile` (연결 · 서버 · 참조 · 입력 · 출력) 전부, IOCTL `0x500023` · `0x500037` · `0x500016`. ① ② 는 `ConsoleCommitState` 도 안 불러 `NtSetInformationProcess` 도 없다.
- **표준 핸들**: ① ② 는 `ConsoleCloseIfConsoleHandle` (rva `0xbf19c`) 로 **칸이 콘솔 장치를 가리킬 때만** 닫고 0 으로 한다 — 칸 0 → 그대로 / `(값 & 0x10000003) == 3` → syscall 없이 칸만 0 / 그 밖은 `+0x4c/+0x53 NtQueryVolumeInformationFile` 해서 `DeviceType == 0x50` 이면 `+0x66/+0x6d NtClose` + 칸 0. **파일 · 파이프 핸들은 그대로 남는다.** 입력 칸은 `WindowFlags & 0x200`, 출력 칸은 `& 0x400` 이면 안 건드리고 (`+0xd2 ~ +0xf1`), 에러 칸은 늘 검사. ③ 은 상태 비트가 0 이라 `params.Standard*` 를 안 바꾼다 — **매개변수 블록 값 그대로** (모두 0 이면 0). 단 ③ 은 `ConsoleHandle` 을 −1 → **0** 으로 바꾼다 (`ConsoleCommitState +0x40`, flow-writer).

### 질문 2 — 자주 쓰는 콘솔 API (원문 꼴: API · syscall · IOCTL · 입력 · 기대 답)

**IOCTL `0x500016` 하나로 다 통한다.** 번호 풀이 (`CTL_CODE` 계산, 이름 `[추측]`): `0x500016` = 장치 `0x50` (`FILE_DEVICE_CONSOLE`) · 함수 5 · `METHOD_OUT_DIRECT`, `0x500023` = 함수 8 · NEITHER, `0x500037` = 함수 13 · NEITHER. PDB 에 `CONSOLE_*` · `CD_*` 타입 · 열거형이 없다 (`pdb_struct.py --list` 0개, 결과서) — 이름은 모두 `[추측: 이름]`, 칸 위치 · 크기는 `[정적]`.

- 부르는 꼴은 늘 같다: `NtDeviceIoControlFile(FileHandle, 0, 0, 0, &IoStatus, 0x500016, &설명서, 설명서 크기, NULL, 0)`. **데이터는 설명서 안 포인터가 가리키는 곳 (호출한 프로세스 메모리) 에 있다.** 돌려주는 값 = NTSTATUS 그대로 (메시지 안에 상태 칸 없음).
- **설명서** `[추측: CD_USER_DEFINED_IO]`: `+0x00` 8B "Client" (앱 콘솔 핸들, 없으면 0) · `+0x08` 4B 입력 버퍼 개수 · `+0x0c` 4B 출력 버퍼 개수 · `+0x10 …` 버퍼 칸 16B 씩 `{ 4B 크기, 4B 빈칸, 8B 포인터 }` (입력 먼저, 출력 나중). 크기 = `0x10 + 0x10 × (입력 + 출력)` — 1·1 `0x30`, 2·1 `0x40`, 3·2 `0x60`.
  - **첫 입력 버퍼 = 메시지**: `+0` API 번호 4B · `+4` body 크기 4B · `+8` body. **첫 출력 버퍼 = 같은 메시지의 body 자리** (`메시지 + 8`) — 서버가 답을 같은 자리에 덮어 쓴다.

| API 번호 `[추측: 이름]` | 쓰는 함수 (자리) | body |
|---|---|---|
| `0x01000000` GetCP | `GetConsoleOutputCP` (`+0xc1`) | 8 |
| `0x01000001` GetMode | `GetConsoleMode` (`+0xc8`) | 4 |
| `0x01000002` SetMode | `SetConsoleMode` (`+0x18`) | 4 |
| `0x01000005` ReadConsole | `ReadConsoleInternal` (`+0x18c`) | `0x14` |
| `0x01000006` WriteConsole | `WriteConsoleInternal` (`+0xe9`) | 8 |
| `0x01000008` GetLangId | `GetConsoleLangId` (`+0xc4`) | 2 |
| `0x02000004` SetCP | `SetConsoleOutputCP` (`+0x26`) | 8 |
| `0x02000007` GetScreenBufferInfo | `GetConsoleScreenBufferInfoEx` (`+0x3f`) | `0x5c` |

**공통 경로 셋 + 인라인**

| 함수 (rva) | `FileHandle` | 설명서 `Client` | syscall 없이 막는 줄 |
|---|---|---|---|
| `ConsoleCallServer` (`0xbe9a0`) — 부르는 곳 30곳 | **`params.ConsoleHandle`** (`+0x4d`) | 첫 인자 (앱 핸들 또는 0) | `+0x5e TEST RSI,RSI` · `+0x61 JLE` → `+0x10c` `0xc0000008` (부호 있는 ≤ 0) |
| `ConsoleCallServerWithBuffers` (`0xef540`) | 같음 | 첫 인자 | `+0x7c/+0x7f` → `0xc0000008`. 추가 버퍼 `n + m + 2 > 10` 이면 `+0x76 JA` → `0xc00000bb` |
| `ConsoleCallServerGeneric` (`0xdf180`) — caller 는 `ReadConsoleInternal` 하나 | **첫 인자 = 앱 핸들 그 자체** | 둘째 인자 | `+0x6f/+0x72` 첫 인자 ≤ 0 → `0xc0000008` (`ConsoleHandle` 안 봄). 개수 `> 10` → `0xc00000bb` |
| (인라인) `GetConsoleMode` · `GetConsoleOutputCP` · `GetConsoleLangId` | `ConsoleHandle` | 앱 핸들 / 0 / 0 | `ConsoleHandle ≤ 0` → `0xc0000008` |
| (인라인) `WriteConsoleInternal` (`0xf7cb8`) | **앱 출력 핸들 그 자체** (`+0xd9 MOV RCX,R14`) | 0 (`+0xa2`) | `+0x50/+0x55` `ConsoleHandle == 0` · `+0x84/+0x87` 앱 핸들 ≤ 0 → `+0x148` `0xc0000008` |

**표 A — syscall 로 가는 길과 입력**

| API (rva) | 하위 호출 (call/ret) | syscall | IOCTL / API | 입력 (body 칸) | FileHandle | Client |
|---|---|---|---|---|---|---|
| `GetStdHandle` (`0xec300`) | `BaseSetLastNTError` (값 −1 일 때만) | **없음** | — | — | — | — |
| `WriteFile` (`0xb2070`) | `NtWriteFile` (`+0x86/+0x8d`; 겹침 `+0x113/+0x11a`), `0x103` 이면 `NtWaitForSingleObject` (`+0x1d1/+0x1d8`) | `NtWriteFile` | — | `(h, 0, 0, 0, &IoStatus, buf, len, 0, 0)` | `h` (표준 상수면 `params.Standard*`) | — |
| `WriteConsoleW` (`0xf7c80`) → `WriteConsoleInternal` (`+0x9/+0xe`) | | `NtDeviceIoControlFile` (`+0x10d/+0x114`) | `0x500016` / `0x01000006` | body 8B: `+0` 쓴 바이트 수 (출력) · `+4` 유니코드 1B (`W` 는 1). 입력 버퍼 2 (메시지 `0x10`B · 글자 `nChars × 2`B `lpBuffer`), 출력 1 (body). 설명서 `0x40` | 앱 출력 핸들 | 0 |
| `ReadFile` (`0x22a90`) | `NtReadFile` (`+0x86/+0x8d`; 겹침 `+0x123/+0x12a`), `NtWaitForSingleObject` (`+0x207/+0x20e`) | `NtReadFile` | — | 같은 꼴 | `h` (같은 상수 바꾸기) | — |
| `ReadConsoleW` (`0xdeef0`) → `ReadConsoleInternal` (`0xdef48`, `+0x19/+0x1e`) → `ConsoleCallServerGeneric` (`+0x19c/+0x1a1`) | | `NtDeviceIoControlFile` (Generic `+0x109/+0x110`) | `0x500016` / `0x01000005` | body `0x14`B: `+0` 유니코드 1B · `+2` exe 이름 글자 수 2B · `+4` 초기 글자 바이트 4B · `+8` `CtrlWakeupMask` (`pInputControl+8`) · `+0xc` 컨트롤 키 상태 (출력) · `+0x10` 읽을 바이트 (입력 `nChars × 2` / 출력 읽은 바이트). 추가 입력 2 (exe 이름 ≤ `0x28` 글자 · 초기 글자), 추가 출력 1 (`lpBuffer`). 설명서 `0x60` | **앱 입력 핸들** | 0 |
| `GetConsoleMode` (`0xf2860`) | (인라인) | `NtDeviceIoControlFile` (`+0xdb/+0xe2`) | `0x500016` / `0x01000001` | body 4B (출력 모드). 설명서 `0x30` | `params.ConsoleHandle` | 앱 핸들 |
| `SetConsoleMode` (`0xbe6f0`) | `ConsoleCallServer` (`+0x26/+0x2b`) | 같음 (`+0xd7/+0xde`) | `0x500016` / `0x01000002` | body 4B (입력 모드) | `params.ConsoleHandle` | 앱 핸들 |
| `GetConsoleScreenBufferInfo` (`0xbe7f0`) | `GetConsoleScreenBufferInfoEx` (`+0x33/+0x38`) — `cbSize 0x60` 임시 구조체 → 22B 만 복사 (`+0x3c ~ +0x5f`) | (아래) | | | | |
| `GetConsoleScreenBufferInfoEx` (`0xbe870`) | `ConsoleCallServer` (`+0x48/+0x4d`) | 같음 | `0x500016` / `0x02000007` | body `0x5c`B (출력, 표 C) | `params.ConsoleHandle` | 앱 핸들 |
| `GetConsoleOutputCP` (`0xb9db0`) | (인라인) | `NtDeviceIoControlFile` (`+0xde/+0xe5`) | `0x500016` / `0x01000000` | body 8B: `+4` = 1 (출력 쪽 CP `[추측]`), `+0` 출력 CP. 설명서 `0x30` | `params.ConsoleHandle` | **0** |
| `SetConsoleOutputCP` (`0xbe1a0`) | `ConsoleCallServer` (`+0x37/+0x3c`), 성공이면 `SetTEBLangID` (`+0x42/+0x47`) → `GetConsoleLangId` IOCTL 한 번 더 | 같음 | `0x500016` / `0x02000004` | body 8B: `+0` CP · `+4` = 1 | `params.ConsoleHandle` | **0** |

- `WriteConsoleInternal` 글자 수 → 바이트: `nChars × 2` (유니코드) / `nChars` (`WriteConsoleA`, 플래그 0) (`+0x5b` · `+0x6b CMOVZ`).

**표 B — 기대하는 답과 실패일 때**

| API | 성공 (서버가 주는 것) | 반환 | 실패면 (NTSTATUS < 0) | `ConsoleHandle ≤ 0` 이면 |
|---|---|---|---|---|
| `GetStdHandle` | — | 칸 값 그대로 (0 도) | 칸 −1 · 모르는 상수 → `BaseSetLastNTError(0xc0000008)` + −1 (`+0x71/+0x76`, `+0x9c`) | 상관없음 |
| `WriteFile` | `IoStatus.Information` → `*lpNumberOfBytesWritten` (`+0xac ~ +0xb0`) | TRUE | `BaseSetLastNTError` (`+0x143/+0x148`) + FALSE. `0x8xxxxxxx` 경고면 바이트 수는 채움 (`+0x17a ~ +0x183`) | **상관없음 — `NtWriteFile` 까지 간다** |
| `WriteConsoleW` | body `+0` 바이트 → `*lpNumberOfCharsWritten = /2` (`+0x14f ~ +0x161`) | TRUE | `RtlSetLastWin32ErrorAndNtStatusFromNtStatus` + FALSE (`WriteConsoleW +0x1d ~ +0x2b`). `0xc000000d` 는 `0xc0000008` 로 바꿈 (`+0x11d/+0x122` → `+0x148`) | syscall 없이 `0xc0000008` → FALSE |
| `ReadFile` | `Information` → `*lpNumberOfBytesRead` | TRUE | FALSE + `BaseSetLastNTError`. **예외: `0xc0000011` (EOF) 이고 겹침이 아니면 TRUE · 0 바이트** (`+0xa7 ~ +0xba`) | 상관없음 |
| `ReadConsoleW` | body `+0x10` → `*lpNumberOfCharsRead = /2`, `+0xc` → `pInputControl+0xc` (`+0x1f4 ~ +0x20e`) | TRUE | 오류 처리 + FALSE. **`0xc0000120` (CANCELLED) 는 오류 코드만 세우고 TRUE** (`ReadConsoleW +0x43 ~ +0x4d`). `0x101` · `0xc0000120` 이면 `+0x213/+0x21a NtYieldExecution`. `pInputControl+0 ≠ 0x10` 이면 무시 (`+0xf2` · `+0xf5`), `+4` 가 너무 크면 `0xc000000d` (`+0xfa` · `+0xfc`) | 앱 입력 핸들 ≤ 0 → `0xc0000008` (`ConsoleHandle` 안 봄) |
| `GetConsoleMode` | body `+0` → `*lpMode` (`+0x137 ~ +0x13b`) | TRUE | `RtlNtStatusToDosError` → `RtlSetLastWin32Error` + FALSE (`+0xed ~ +0x10b`) | `0xc0000008` → `ERROR_INVALID_HANDLE` (6) FALSE (`+0x5f/+0x62` → `+0x130`) |
| `SetConsoleMode` | (상태만) | TRUE | 위와 같음, FALSE | 같음 |
| `GetConsoleScreenBufferInfoEx` | body `0x5c` → 사용자 구조체 (표 C) | TRUE | `cbSize ≠ 0x60` → **syscall 없이** `0xc000000d` → FALSE (`+0x2c/+0x2f` → `+0x107`) | `0xc0000008` FALSE |
| `GetConsoleScreenBufferInfo` | 위에서 22B 복사 | TRUE | `Ex` 가 FALSE 면 복사 안 함 (`+0x38 TEST EAX` · `+0x3a JZ`) | 같음 |
| `GetConsoleOutputCP` | body `+0` | **CP 값** | 오류 코드 + **0 반환** (`+0xee ~ +0x10a`) | → 0 (`+0x5c/+0x5f` → `+0x12a`) |
| `SetConsoleOutputCP` | (상태만), 성공이면 `SetTEBLangID` | TRUE | FALSE (`+0x40 JS` → `+0x58`) | `0xc0000008` FALSE |

**표 C — `GetConsoleScreenBufferInfoEx` body `0x5c`B** (`[정적]` 위치, 이름 `[추측]`; 계산 줄 `+0x7f ~ +0x9f`)

| body | 크기 | 사용자 구조체 (`CONSOLE_SCREEN_BUFFER_INFOEX`) |
|---|---|---|
| `+0x00` | 4B (`SHORT X, Y`) | `dwSize` (`+4`) |
| `+0x04` | 4B | `dwCursorPosition` (`+8`) |
| `+0x08` | 2B × 2 | `srWindow.Left/Top` (`+0xe/+0x10`) |
| `+0x0c` | 2B | `wAttributes` (`+0xc`) |
| `+0x0e` | 2B × 2 | 창 **너비 · 높이** → `Right = Left + 너비 − 1`, `Bottom = Top + 높이 − 1` (`+0x12/+0x14`) |
| `+0x12` | 4B | `dwMaximumWindowSize` (`+0x16`) |
| `+0x16` | 2B | `wPopupAttributes` (`+0x1a`) |
| `+0x18` | 1B | `bFullscreenSupported` (`+0x1c`) |
| `+0x1c` | 64B | `ColorTable` (`+0x20`, 16 × `COLORREF`) |

- **`WriteFile` 이 콘솔이면 condrv, 아니면 `NtWriteFile` 로 가르는 줄은 없다.** `WriteFile.asm` 의 비교는 `+0x39 CMP ECX,-0xb` · `+0x42 CMP ECX,-0xc` · `+0x4b CMP ECX,-0xa` (표준 상수 → 칸 값) 와 `+0x63` (`lpOverlapped` 있나) 뿐. 핸들 하위 비트 · `ConsoleHandle` 비교 없음. `ReadFile` 도 같다 (`+0x3d` · `+0x46` · `+0x4f`).

### 질문 3 · 4 · 5

- **3 `GetFileType`** (rva `0xc6290`, 259B): `+0x72/+0x79 NtQueryVolumeInformationFile(h, &IoStatus, &out, 8, 4)` 하나. 상태 < 0 → `BaseSetLastNTError` + 0 (`+0x7e ~ +0x9e`). `DeviceType == 0x50` → **2** (`+0x87` · `+0x8c`). 그 밖 `DeviceType − 2 ≤ 0x29` 는 점프표 (`+0xb8 ~ +0x105`, 표 rva `0xc63b0` + 바이트 색인 `0xc63c4` — 결과서가 바이너리에서 직접 읽음): **1** ← `2 ~ 8` · `0x24`, **3** ← `0x11`, **2** ← `0xb` · `0xf` · `0x15` · `0x16` · `0x18` · `0x1b` · `0x1c` · `0x1d` · `0x2b`, 그 밖 (범위 밖 포함) → `RtlSetLastWin32Error(0)` + **0**. **핸들 0 → syscall 없이** `BaseSetLastNTError(0xc0000008)` + 0 (`+0x15/+0x18 JZ` → `+0xa6 ~ +0xb0`), 표준 상수인데 칸이 0 이어도 같음 (`+0x55/+0x58`). 0 이 아닌 잘못된 핸들 (−1 등) 은 kernelbase 가 안 거르고 커널 상태를 그대로 오류로 세운다.
- **4 표준 핸들 0**:

| 함수 | 0 일 때 | syscall |
|---|---|---|
| `GetStdHandle(-10/-11/-12)` | 칸의 0 을 그대로, 오류 안 세움 (`+0x66 CMP RBX,-1` · `+0x6a JNZ`). `WindowFlags & 0x200` / `& 0x400` 이면 칸을 안 읽고 0 (`+0x49 ~ +0x53` · `+0x1d ~ +0x27` → `+0x98`) | 없음 |
| `WriteFile(0, …)` · `ReadFile(0, …)` | **kernelbase 가 안 막는다** → `+0x86/+0x8d NtWriteFile(0)` / `NtReadFile(0)` → 커널 `0xc0000008` 이면 `+0x143/+0x148 BaseSetLastNTError` + FALSE | **간다** |
| `GetFileType(0)` · `WriteConsoleW(0)` · `ReadConsoleW(0)` | 막음 (위) — `ReadConsoleW` 는 `ExeNameLock` 공유 락과 이름 복사는 먼저 함 | 없음 |
| `GetConsoleMode` 등 (나) 계통 | 앱 핸들이 아니라 **`params.ConsoleHandle ≤ 0`** 이면 막음 | `ConsoleHandle` 에 달림 |

- **5 리다이렉트**: `WriteFile` 안 syscall 은 파일이든 파이프든 콘솔이든 `NtWriteFile` (+ `0x103` 이면 `NtWaitForSingleObject`) 뿐. 파일 · 파이프면 `GetFileType` 이 1 · 3 을 주고 CRT 가 콘솔 전용 API 를 안 쓸 것 `[추측]` (run84).

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` (결과서, 기준과 같음 — 다시 analyze 안 함). KUSD `SharedDataFlags` 비트 이름은 결과서가 ntoskrnl PDB 로, flow-writer 가 ntdll PDB (`pdb_struct.py ntdll.dll _KUSER_SHARED_DATA`) 로 한 번 더 봤다 (같음).
- 버전 대조 (결과서): `ConsoleInitialize` ret `+0x19f` → `ConsoleCreateConnectionObject` · `+0x1cf` → `ConsoleCommitState` (sast/63 과 같음), `ConsoleCommitState` ret `+0x91` → `NtDeviceIoControlFile` · `+0xf0` → `NtSetInformationProcess` (run51 과 같음).
- 칸 이름: `_RTL_USER_PROCESS_PARAMETERS` (ntdll PDB) `ConsoleHandle +0x10` · `ConsoleFlags +0x18` · `StandardInput +0x20` · `StandardOutput +0x28` · `StandardError +0x30` · `CurrentDirectory +0x38` · `WindowFlags +0xa4` · `WindowTitle +0xb0` · `DesktopInfo +0xc0` · `ShellInfo +0xd0` · `ProcessGroupId +0x408`. `_TEB` `CurrentLocale +0x108`. `_KUSER_SHARED_DATA` `SharedDataFlags +0x2f0` (비트 6 `DbgConsoleBrokerEnabled`, 비트 8 `DbgMultiSessionSku` — ntdll PDB).

## 목적

- PE_emulator 요청 27 (콘솔 입출력) 의 kernelbase 몫 (Q149a — 원문 1 · 2 · 4 · 5 와 3 의 `GetFileType`): 콘솔을 얻는 길 셋 (물려받음 · 새 콘솔 · 없음) 과 `ConsoleInitialize` 아래 syscall · IOCTL · 바뀌는 칸, 자주 쓰는 콘솔 API 가 condrv 에 보내는 것 (API · syscall · IOCTL · 입력 · 기대 답), 표준 핸들 0 · 리다이렉트일 때.
- CRT (ucrtbase) · condrv.sys · (가)/(나) 최소 조건 표는 짝 바퀴 Q149b (run84).

## 방법

- 정적: `_workspace/sast/80_console-kernelbase.md` — kernelbase
  - 지도 (새로, `ms/disassemble/kernelbase/_map/`): `ConsoleInitialize` (함수 30개 — `expanded 25 · helper 5`, 끊긴 끝 31종 모두 import), `ConsoleCallServerGeneric` (2개).
  - 뽑음 (모두 `export`, SCRIPT ERROR 없음 — 로그 `ms/ghidra/kernelbase.export.sast80{a..g}.stdout`): `ConsoleInitialize` · `ConsoleCommitState` · `ConsoleAllocate` · `ConsoleCreateConnectionObject` depth 1 (21개), `ConsoleLaunchServerProcess` 0, `ConsoleCallServer` · `ConsoleCallServerGeneric` · `ConsoleCallServerWithBuffers` depth 1 (5개), API 13개 depth 0 (`GetStdHandle` … `GetFileType`). 넓힌 것: `ReadConsoleInternal` (0), `GetConsoleLangId` (0) · `SetThreadLocale` (1) — 지도에서 `ConsoleCommitState → SetTEBLangID → GetConsoleLangId` 가 syscall 로 닿는데 run68 이 안 연 줄기라서.
  - `.asm` 에 없는 곳 — 결과서가 임시 파이썬 (레포 밖) 으로 직접 읽음: `.rdata` 문자열 (이름 7 · 매니페스트 3 · `dde.` · `hotkey.` · `Command Prompt`), `GetFileType` 점프표 (`0xc63b0` · `0xc63c4`).
- 결과서: `_workspace/sast/80_console-kernelbase_result.md` (**분석성공**. 질문 1 ~ 5 모두 `.asm` 줄로 답함, run68 정정 제안 하나. 스크립트 문제 없음)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `ConsoleInitialize +0x142` · `+0x177`, `ConsoleCommitState +0xfa`, `GetConsoleLangId +0xe1`, `GetFileType +0x72`, `WriteFile +0x86` · `+0x113`, `_KernelBaseBaseDllInitialize +0x521` · `+0x526`.
- flow-writer 도 `.asm` 으로 다시 봤다: `ConsoleInitialize +0x0 ~ +0x245` (전부), `ConsoleCommitState +0x0 ~ +0x15f` (전부), `ConsoleShouldAllocateConsole` (전부), `SetTEBLangID` (전부), `SetThreadLocale` (전부), `GetConsoleLangId +0x0 ~ +0x10d`, `GetStdHandle` (전부), `ConsoleAllocate +0x52` · `+0x5a` · `+0x149` · `+0x175` · `+0x20b` · `+0x2b4` · `+0x2ff ~ +0x3a8`, `ConsoleCallServer +0x4d` · `+0x5e` · `+0x61` · `+0x9a` · `+0xc3` · `+0xd7`, `WriteConsoleInternal +0x50` · `+0x55` · `+0x84` · `+0x87` · `+0xa2` · `+0xd9` · `+0xe9` · `+0x10d` · `+0x11d` · `+0x122`, `GetFileType +0x15` · `+0x18` · `+0x55` · `+0x58` · `+0x5a` · `+0x60` · `+0x72` · `+0x87` · `+0x8c`, `WriteFile +0x39` · `+0x42` · `+0x4b` · `+0x63` · `+0x86` · `+0x113` · `+0x141` · `+0x143` · `+0x1d1`, `_KernelBaseBaseDllInitialize +0xe4 ~ +0x10e` · `+0x4fa ~ +0x52e`, 그리고 `_calls.tsv` 의 이번 함수 줄 전부.
- 값 대조 (flow-writer): run44 덤프 읽기 로그 — 매개변수 `ConsoleHandle` · `ConsoleFlags` · `StandardInput/Output/Error` · `WindowFlags` (`ms/logs/04_baggage-read_P1.txt:2346 ~ 2350` · `:2363`, `04_baggage-read_P1a.txt:2513 ~ 2517` · `:2530`, `04_baggage-read_P1b.txt:3211 ~ 3215` · `:3228`, `04_baggage-read_P2.txt:3223 ~ 3227` · `:3240`), TEB `CurrentLocale` (`04_baggage-read_P1.txt:1821` · `04_baggage-read_P1a.txt:1988` · `04_baggage-read_P1b.txt:2686` · `04_baggage-read_P2.txt:2698`), KUSD `SharedDataFlags` (`04_baggage-read_P1a.txt:2938`, run68 이 읽은 줄). 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/kernelbase/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x521` | `+0x526` | `ConsoleInitialize` | 늘. `+0x526 TEST AL,AL` · `+0x528 JZ` → `+0xf6` (AL 0 반환) | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x63` | `+0x6a` | (IAT) `RtlImageNtHeader` | `+0x7b CMP [NT+0x5c],3` | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x8f` | `+0x94` | `ConsoleRationalizeAllocationOptionsWithPeb` | 전역 `0x3aa9f8` | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0xb8` | `+0xbd` | `ConsoleShouldAllocateConsole` | `+0xbf JNZ` TRUE → `+0x166` | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0xe2` · `+0xf7` · `+0x100` | `+0xe7` · `+0xfc` · `+0x105` | `ConsoleCloseIfConsoleHandle` | FALSE 갈래 — 표준 칸 셋 | [정적] |
| `KERNELBASE!ConsoleInitialize` | **`+0x146`** | `+0x14b` | `ConsoleAllocate` | `ConsoleHandle` 0 · −2 · −3 (`+0x177` · `+0x183` · `+0x189 JZ` → `+0x142`), 로우박스 실패 (`+0x20a`) | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x19a` | `+0x19f` | `ConsoleCreateConnectionObject` | 실제 핸들 | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x1ca` | `+0x1cf` | `ConsoleCommitState` | 붙기 · −1 (`+0x17d JZ` → `+0x1c6`) · 새 콘솔 성공 (`+0x14d JNS` → `+0x1c6`) | [정적] |
| `KERNELBASE!ConsoleInitialize` | `+0x1e3` | `+0x1e8` | `ConsoleIsCallerInLowbox` | a1 이 `0xc0000022` | [정적] |
| `KERNELBASE!ConsoleCreateConnectionObject` | `+0x69` | `+0x6e` | `ConsoleInitializeServerData` | 연결 데이터 `0x53c` | [정적] |
| `KERNELBASE!ConsoleCreateConnectionObject` | `+0x1c1` | `+0x1c8` | (IAT) `NtCreateFile` | `\Connect` + EA `"server"` | [정적] |
| `KERNELBASE!ConsoleCommitState` | `+0x8a` | `+0x91` | (IAT) `NtDeviceIoControlFile` | `0x500023`, 상태 `+8` ≠ 0 | [정적] |
| `KERNELBASE!ConsoleCommitState` | `+0xe9` | `+0xf0` | (IAT) `NtSetInformationProcess` | `-1`, `0x31`, 8B | [정적] |
| `KERNELBASE!ConsoleCommitState` | `+0xf5` | `+0xfa` | `InitializeCtrlHandling` | syscall 없음 | [정적] |
| `KERNELBASE!ConsoleCommitState` | **`+0xfa`** | `+0xff` | `SetTEBLangID` | 늘 | [정적] |
| `KERNELBASE!SetTEBLangID` | `+0x12` | `+0x17` | `GetConsoleLangId` | | [정적] |
| `KERNELBASE!SetTEBLangID` | `+0x20` | `+0x25` | `SetThreadLocale` | `GetConsoleLangId` ≠ 0 (`+0x19 JZ` 안 탐) | [정적] |
| `KERNELBASE!GetConsoleLangId` | **`+0xe1`** | `+0xe8` | (IAT) `NtDeviceIoControlFile` | `ConsoleHandle > 0` (`+0x5f` · `+0x62 JLE`), `0x500016` · API `0x01000008` | [정적] |
| `KERNELBASE!SetThreadLocale` | `+0x93` | `+0x98` | `MakeLocHashNode` | 해시 표에 LCID 노드가 없을 때만 | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x149` · `+0x20b` | `+0x150` · `+0x212` | (IAT) `NtCreateFile` | `\Device\ConDrv\Server` (둘째는 b1 실패 뒤) | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x175` | `+0x17c` | (IAT) `NtSetSystemInformation` | 클래스 `0x84` (b1 실패 뒤) | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x2b4` | `+0x2bb` | (IAT) `NtCreateFile` | `<서버>\Reference` | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x303` | `+0x308` | `ConsoleAttachOrAllocate` | KUSD `+0x2f0` 비트 6 (`+0x52` · `+0x5a JNZ` → `+0x301`) — 이 VM 안 탐 | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x326` | `+0x32b` | `ConsoleLaunchServerProcess` | 옛 길 | [정적] |
| `KERNELBASE!ConsoleLaunchServerProcess` | `+0xc6` | `+0xcd` | (IAT) `NtDeviceIoControlFile` | `0x500037`, 입력 = 매개변수 블록 | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x332` | `+0x339` | (IAT) `NtClose` | 서버 핸들 | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x357` | `+0x35c` | `ConsoleCreateConnectionObject` | root = 참조 핸들 | [정적] |
| `KERNELBASE!ConsoleAllocate` | `+0x39f` | `+0x3a4` | `ConsoleCreateStandardIoObjects` | `WindowFlags & 0x100` 이 아니면 | [정적] |
| `KERNELBASE!ConsoleCreateStandardIoObjects` | `+0xb1` · `+0x151` · `+0x1a8` | `+0xb8` · `+0x158` · `+0x1af` | `NtCreateFile` · `NtCreateFile` · `NtDuplicateObject` | `\Input` · `\Output` · 에러 = 출력 복사 | [정적] |
| `KERNELBASE!ConsoleCloseIfConsoleHandle` | `+0x4c` · `+0x66` | `+0x53` · `+0x6d` | `NtQueryVolumeInformationFile` · `NtClose` | 칸 ≠ 0 이고 `(값 & 0x10000003) ≠ 3` / `DeviceType 0x50` | [정적] |
| `KERNELBASE!WriteFile` | **`+0x86`** · `+0x113` | `+0x8d` · `+0x11a` | (IAT) `NtWriteFile` | 겹침 아님 / 겹침 | [정적] |
| `KERNELBASE!ReadFile` | `+0x86` · `+0x123` | `+0x8d` · `+0x12a` | (IAT) `NtReadFile` | 같음 | [정적] |
| `KERNELBASE!WriteConsoleW` | `+0x9` | `+0xe` | `WriteConsoleInternal` | | [정적] |
| `KERNELBASE!WriteConsoleInternal` | `+0x10d` | `+0x114` | (IAT) `NtDeviceIoControlFile` | 앱 출력 핸들, `0x01000006` | [정적] |
| `KERNELBASE!ReadConsoleInternal` | `+0x19c` | `+0x1a1` | `ConsoleCallServerGeneric` | → `+0x109/+0x110 NtDeviceIoControlFile` | [정적] |
| `KERNELBASE!GetConsoleMode` · `GetConsoleOutputCP` | `+0xdb` · `+0xde` | `+0xe2` · `+0xe5` | (IAT) `NtDeviceIoControlFile` | 인라인 | [정적] |
| `KERNELBASE!SetConsoleMode` · `SetConsoleOutputCP` · `GetConsoleScreenBufferInfoEx` | `+0x26` · `+0x37` · `+0x48` | `+0x2b` · `+0x3c` · `+0x4d` | `ConsoleCallServer` | → `+0xd7/+0xde NtDeviceIoControlFile` | [정적] |
| `KERNELBASE!GetFileType` | **`+0x72`** | `+0x79` | (IAT) `NtQueryVolumeInformationFile` | 핸들 ≠ 0, 클래스 4, 8B | [정적] |

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리. 같은 번호 둘 = 값에 따라 둘 중 하나.

```
[콘솔 얻기 — kernelbase 시작 함수 안 (2.5)]
KERNELBASE!_KernelBaseBaseDllInitialize (0x4e6c0)
 └─ +0x526  ConsoleInitialize (0xbeb60)            +0x526 TEST AL · +0x528 JZ → +0xf6 (AL 0 = 시작 함수 FALSE)
      ├─ (1) +0x49   RtlInitializeCriticalSection(&ConsoleStateLock)
      ├─ (2) +0x6a   RtlImageNtHeader                +0x7b Subsystem 3? · +0x83 JNZ → +0x13a · +0x89 ConsoleIsConsoleApplication ← 1
      ├─ (3) +0x94   ConsoleRationalizeAllocationOptionsWithPeb      ConsoleHandle 0 · −2 → 0x3aa9f8 ← 1, −3 → 2, −1 → |= −1
      ├─ (4) +0x99   InitializeCtrlHandling
      ├─ (5) +0x9e   InitExeName                      +0x9e ~ +0xb2 ProcessGroupId (0 일 때만)
      ├─ (6) +0xbd   ConsoleShouldAllocateConsole     +0xbf JNZ (TRUE) → +0x166
      │   ── FALSE: +0xc5 (콘솔 앱 아니면 +0xce ConsoleHandle ← 0) ──
      ├─ (7) +0xe7 · +0xfc · +0x105  ConsoleCloseIfConsoleHandle ×3    → +0x53 NtQueryVolumeInformationFile · +0x6d NtClose (콘솔 장치면)
      │   ── TRUE: +0x166 ConsoleFlags & 4 · +0x170 ConsoleHandle 값으로 ──
      ├─ (7) +0x14b  ConsoleAllocate (0xbf41c)        0 (+0x177) · −2 (+0x183) · −3 (+0x189) → +0x142. 실패면 +0x15d RtlDeleteCriticalSection → AL 0
      ├─ (7) +0x19f  ConsoleCreateConnectionObject (0xbf960)     실제 핸들
      │       ├─ (1) +0x6e   ConsoleInitializeServerData (0xbfbb4)     연결 데이터 0x53c
      │       └─ (2) +0x1c8  NtCreateFile(<ConsoleHandle>\Connect, EA "server" 0x54b)          ── SYSCALL a1
      │   +0x1d4 0xc0000022 → +0x1e8 ConsoleIsCallerInLowbox → +0x205 NtClose → +0x142 (새 콘솔)
      └─ (8) +0x1cf  ConsoleCommitState (0xbedb0)     −1 은 +0x17d JZ → +0x1c6 로 바로
           │  +0x2c · +0x36 WindowFlags 비트 9 · 10 → +0x14a · +0x159 지움 · +0x40 ConsoleHandle ← 상태+8
           ├─ (1) +0x91   NtDeviceIoControlFile(새 핸들, 0x500023)  → PID                     ── SYSCALL a2 (상태+8 = 0 이면 +0x51 JZ 건너뜀)
           │  +0x99 ~ +0xbb ConsoleConnectionState ← 상태 · +0xc3 TEST [상태],7 → +0x11c · +0x12e · +0x140 Standard*
           ├─ (2) +0xf0   NtSetInformationProcess(-1, 0x31, PID | 1, 8)                     ── SYSCALL a3
           ├─ (3) +0xfa   InitializeCtrlHandling
           └─ (4) +0xff   SetTEBLangID (0xbefa0)
                ├─ (1) +0x17  GetConsoleLangId (0x4edf4)      +0x4c ConsoleHandle · +0x62 JLE (≤ 0 → 막음)
                │       └─ +0xe8  NtDeviceIoControlFile(ConsoleHandle, 0x500016, API 0x01000008)   ── SYSCALL a4 (run68 에 없던 것)
                └─ (2) +0x25  SetThreadLocale(LangId) (0x4ef40)     +0x5a TEB+0x108 ← LCID · 캐시에 없으면 +0x98 MakeLocHashNode

[새 콘솔 — 옛 길 (KUSD +0x2f0 비트 6 꺼짐)]
KERNELBASE!ConsoleAllocate (0xbf41c)       +0x52 TEST [0x7ffe02f0],0x40 → 켜지면 +0x5a JNZ → +0x308 ConsoleAttachOrAllocate (중개 길)
 ├─ (1) +0x150  NtCreateFile("\Device\ConDrv\Server")                실패면 +0x17c NtSetSystemInformation(0x84) → +0x212 한 번 더
 ├─ (2) +0x2bb  NtCreateFile(<서버>\Reference)
 ├─ (3) +0x32b  ConsoleLaunchServerProcess (0xbf870)
 │       ├─ (1) +0x6e   RtlCreateProcessParametersEx          conhost.exe 0xffffffff -ForceV1
 │       ├─ (2) +0xcd   NtDeviceIoControlFile(서버, 0x500037, 매개변수 블록)
 │       └─ (3) +0xe0   RtlDestroyProcessParameters
 ├─ (4) +0x339  NtClose(서버)
 ├─ (5) +0x35c  ConsoleCreateConnectionObject → +0x1c8 NtCreateFile(<참조>\Connect, EA "server")
 └─ (6) +0x3a4  ConsoleCreateStandardIoObjects (0xbff60)        WindowFlags & 0x100 이면 대신 +0x3c6 ConsoleSanitizeStandardIoObjects
         ├─ (1) +0xb8   NtCreateFile(<연결>\Input)
         ├─ (2) +0x158  NtCreateFile(<연결>\Output)
         └─ (3) +0x1af  NtDuplicateObject(출력 → 에러)
  (성공이면 ConsoleInitialize +0x14d JNS → +0x1c6 ConsoleCommitState — 위 (8))

[콘솔 API — 진입점 뒤]
KERNELBASE!WriteFile (0xb2070)          +0x39 · +0x42 · +0x4b 표준 상수만 바꿈 (콘솔 갈림 없음)
 └─ +0x8d   NtWriteFile(h, …)            (겹침 +0x11a · 0x103 이면 +0x1d8 NtWaitForSingleObject)
KERNELBASE!ReadFile (0x22a90)
 └─ +0x8d   NtReadFile(h, …)
KERNELBASE!WriteConsoleW (0xf7c80)
 └─ +0xe    WriteConsoleInternal (0xf7cb8)     +0x50 ConsoleHandle == 0 · +0x84 앱 핸들 ≤ 0 → +0x148 (0xc0000008)
      └─ +0x114  NtDeviceIoControlFile(앱 출력 핸들, 0x500016, API 0x01000006)
KERNELBASE!ReadConsoleW (0xdeef0)
 └─ +0x1e   ReadConsoleInternal (0xdef48)
      └─ +0x1a1  ConsoleCallServerGeneric (0xdf180)     +0x6f/+0x72 앱 입력 핸들 ≤ 0 → 0xc0000008
           └─ +0x110  NtDeviceIoControlFile(앱 입력 핸들, 0x500016, API 0x01000005)
KERNELBASE!GetConsoleMode (0xf2860)        └─ +0xe2  NtDeviceIoControlFile(ConsoleHandle, 0x500016, 0x01000001)
KERNELBASE!GetConsoleOutputCP (0xb9db0)    └─ +0xe5  NtDeviceIoControlFile(ConsoleHandle, 0x500016, 0x01000000)
KERNELBASE!SetConsoleMode (0xbe6f0)        └─ +0x2b  ConsoleCallServer (0xbe9a0) → +0xde NtDeviceIoControlFile(ConsoleHandle, 0x500016, 0x01000002)
KERNELBASE!SetConsoleOutputCP (0xbe1a0)
 ├─ (1) +0x3c  ConsoleCallServer(…, 0x02000004)
 └─ (2) +0x47  SetTEBLangID                  성공일 때 — GetConsoleLangId IOCTL 한 번 더
KERNELBASE!GetConsoleScreenBufferInfo (0xbe7f0)
 └─ +0x38  GetConsoleScreenBufferInfoEx (0xbe870)     +0x2c cbSize ≠ 0x60 → 0xc000000d
      └─ +0x4d  ConsoleCallServer(…, 0x02000007)
KERNELBASE!GetFileType (0xc6290)           +0x15/+0x18 핸들 0 → 0 (syscall 없음)
 └─ +0x79  NtQueryVolumeInformationFile(h, 4, 8B)       +0x87 DeviceType 0x50 → 2
KERNELBASE!GetStdHandle (0xec300)          syscall 없음 — params +0x20 / +0x28 / +0x30 그대로
```

## 확인된 것

- `[정적]` `ConsoleInitialize` 는 `ConsoleShouldAllocateConsole` 이 FALSE 면 (콘솔 앱 아님 · detached 정책 + 0) 아무것도 안 만들고 AL 1, TRUE 면 `ConsoleHandle` 로 셋으로 가른다 — 0 · −2 · −3 → `+0x146 ConsoleAllocate`, −1 → `+0x1ca ConsoleCommitState` (빈 상태), 실제 핸들 → `+0x19a ConsoleCreateConnectionObject` → `+0x1ca`. **콘솔 앱 + 매니페스트 없음 + 0 은 새 콘솔 길이다** (`+0x177 JZ`).
- `[정적]` **붙기 길의 syscall 은 넷**: `NtCreateFile(<4>\Connect)` · `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)` · `NtDeviceIoControlFile(0x500016, API 0x01000008)` (`ConsoleCommitState +0xfa SetTEBLangID` → `GetConsoleLangId +0xe1`). run68 은 셋이라고 했다 (아래 "정정").
- `[정적]` 새 콘솔 옛 길은 conhost 를 kernelbase 가 직접 만들지 않고 `\Device\ConDrv\Server` 핸들에 IOCTL `0x500037` + 매개변수 블록 (`conhost.exe 0xffffffff -ForceV1`) 으로 맡긴다 (`ConsoleLaunchServerProcess +0xc6`). 표준 에러는 출력 핸들의 복사본 (`NtDuplicateObject`).
- `[정적]` `ConsoleInitialize` 가 AL 0 이면 kernelbase 시작 함수도 AL 0 (`_KernelBaseBaseDllInitialize +0x528 JZ` → `+0xf6`).
- `[정적]` 콘솔 API 는 셋으로 갈린다: `WriteFile` · `ReadFile` (콘솔 갈림 없음 — `NtWriteFile` · `NtReadFile`), `ConsoleCallServer` 계통 (`FileHandle = params.ConsoleHandle`), `WriteConsoleInternal` · `ConsoleCallServerGeneric` (`FileHandle` = 앱이 준 핸들). 모두 IOCTL `0x500016` 하나에 API 번호로 갈리고, 데이터는 설명서 안 포인터로 넘긴다.
- `[정적]` `GetFileType` 은 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 하나, `DeviceType 0x50` → 2. 핸들 0 은 syscall 없이 0. `GetStdHandle` 은 syscall 없이 칸 값을 그대로 (0 도).
- `[소스]` test.exe · dll1 · dll2 의 `Log` 는 `OutputDebugStringA(buf)` 다음에 **`WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, i, &written, NULL)`** 을 부른다 (`sample/log.h:37 ~ 38`, CRT `printf` 는 일부러 안 씀 — `log.h:2`). 그래서 test.exe 가 콘솔에 쓰는 길은 `GetStdHandle` (syscall 없음) → `WriteFile` → `NtWriteFile(params.StandardOutput)` 이다 `[정적 + 소스]`. dll2 TLS 콜백부터 (2.8, 은퇴 앞) `Log` 마다.
- `[관찰]` (run44 사진, flow-writer 대조) test.exe `ConsoleHandle` 4 → `0x60` (P1a → P1b), `ConsoleFlags` 0, 표준 핸들 `0x68` · `0x6c` · `0x70` 은 P1 ~ P2 그대로, `WindowFlags` 0, TEB `CurrentLocale` `0x412` 은 P1 ~ P2 그대로, KUSD `SharedDataFlags 0x10e` (비트 6 꺼짐). 붙기 길이고 `ConsoleCommitState` 가 표준 핸들을 안 쓴다는 정적 결론과 맞는다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① flow-writer 보탬: `ConsoleAllocate` 를 부르는 줄은 `+0x177 JZ` 의 목적지 `+0x142` (`LEA RCX,[RBP-0x40]`) 가 아니라 **call `+0x146` / ret `+0x14b`** 이다. 같은 꼴로 중개 길 `ConsoleAttachOrAllocate` 는 call `+0x303` / ret `+0x308` (결과서 `+0x301` 은 목적지 `XOR EDX,EDX`), `WriteFile` 의 `BaseSetLastNTError` 는 call `+0x143` / ret `+0x148` (결과서 질문 4 표 `+0x141` 은 `MOV ECX,EAX`).
- ② flow-writer 보탬: `ConsoleInitialize` 가 AL 0 을 돌려주면 `_KernelBaseBaseDllInitialize +0x526 TEST AL,AL` · `+0x528 JZ` → `+0xf6` 으로 가 그 AL 0 을 그대로 돌려준다 — **kernelbase 시작 함수 FALSE**. 결과서엔 "AL=0" 까지만 있다.
- ③ flow-writer 보탬: **−1 갈래는 `ConsoleHandle` 을 0 으로 바꾼다.** 빈 상태 (`ConsoleInitialize +0x32 ~ +0x3e` 가 상태 `0x30`B 를 0 으로, `+0x21` 이 `+0x30` 칸도 0) 로 `ConsoleCommitState` 에 들어가 `+0x3c` · `+0x40` 이 상태 `+8` (= 0) 을 늘 쓴다. 결과서 (c) ③ "매개변수 블록 값 그대로" 는 표준 핸들 칸 이야기다 (맞음). 결과서가 적은 "`GetConsoleLangId` 가 `ConsoleHandle` 0 이라 막힘" 이 이 줄 때문이다.
- ④ flow-writer 보탬: **test.exe (붙기 길) 에서 `ConsoleCommitState` 가 표준 핸들을 안 쓰는 근거.** 상태 비트 칸 (`+0`) 은 `ConsoleInitialize +0x32 MOVUPS [RBP-0x40]` 로 0 이 되고, 붙기 길은 `+0x1b5` (상태 `+8` = 새 핸들) · `+0x1bd` (`+0x30`) · `+0x1c0` (`+0x10` = 옛 `ConsoleHandle`) 만 쓴다 (`ConsoleFlags & 2` 면 `ConsoleSanitizeStandardIoObjects` 가 끼지만 test.exe 는 0). 그래서 `ConsoleCommitState +0xc3 TEST byte [RBX],7` 이 0 → `+0x10e ~ +0x144` 를 안 탄다 `[정적]`. run44 사진의 표준 핸들이 P1a → P1b 그대로 (`04_baggage-read_P1a.txt:2515 ~ 2517` · `04_baggage-read_P1b.txt:3213 ~ 3215`) `[관찰]` 과 맞는다.
- ⑤ flow-writer 보탬: 결과서 "`SetThreadLocale` — syscall 없음 (NLS 표 조회 + `TEB+0x108 ← LCID`)" 은 **LCID 노드가 캐시에 있을 때** 만이다. `SetThreadLocale` (rva `0x4ef40`) 은 `+0xc CheckSpecialLocales` 뒤 `+0x1a ~ +0x4f` 에서 해시 표 (`TblPtrs+0x8`) 를 찾고, 노드가 있고 `+0x68 ≠ 0` 이면 `+0x51` · **`+0x5a MOV [TEB+0x108],EBX`**. 없으면 `+0x78` 락 → `+0x86 FindLocaleHashNode` → 그래도 없으면 **`+0x93 MakeLocHashNode`** — run68 이 `[공백]` 으로 둔 그 함수 (`NlsProcessInitialize +0x64` 아래, 레지스트리 · csrss 후보) 다. 지정 안 된 LCID 면 `+0xbe GetCurrentNlsCache`. 그래서 넷째 syscall 뒤에 NLS 쪽 syscall 이 더 있을 수 있다 `[공백]` (run68 정적 남은 것 ① 과 같은 몫). TEB `CurrentLocale` 은 사진에서 P1 ~ P2 내내 `0x412` — 같은 값으로 다시 썼는지 안 썼는지는 못 가른다 `[추측]`.
- ⑥ flow-writer 보탬: `ConsoleCommitState` 쓰는 줄 정확히 — `WindowFlags` 비트 9 `+0x14a` · 비트 10 `+0x159`, `StandardInput` `+0x11c` · `StandardOutput` `+0x12e` · `StandardError` `+0x140` (결과서는 범위 `+0x10e ~ +0x144` · `+0x146 ~ +0x15f`). run68 이 "정확한 줄이 없어" 짐 지도 3.2 에 못 옮긴 `WindowFlags` 줄이 이것이다 (짐 지도 7.6.8 메모 고침).
- ⑦ flow-writer 보탬: 매니페스트 정책 비교는 `ConsoleShouldAllocateConsole +0x56 MOVZX` · `+0x60 SUB AX,0x44` · `+0x64 TEST CX(0xffdf),AX` — 첫 글자가 `'D'` · `'d'` 일 때만 detached, 다른 글자면 `+0x67 JNZ` → TRUE. 매니페스트가 없으면 `+0x54 JS` → `+0x92` TRUE (run68 "매니페스트 없음 → TRUE" 와 같음). detached 정책이어도 `ConsoleHandle` 이 0 · −2 가 아니면 TRUE (`+0x96 ~ +0x9b`).
- ⑧ 결과서 "`ConsoleHandle = 0` 은 (b)" 와 원문 "콘솔 없음 (`DETACHED_PROCESS`)" 의 짝은 `[추측]` 이다 — 부모가 `DETACHED_PROCESS` 에 −1 을 쓰는지 0 을 쓰는지는 부모 쪽 코드 (`CreateProcessInternalW`) 를 안 읽어 모른다 (아래 정적 후보 ①).

## 정정

- **run68 (sast/63 §8) 정정 — "부모 콘솔에 붙기 길 syscall 3번" → 4번.** `ConsoleCommitState +0xfa/+0xff SetTEBLangID` → `+0x12/+0x17 GetConsoleLangId` → **`+0xe1/+0xe8 NtDeviceIoControlFile(params.ConsoleHandle = 0x60, 0x500016, API 0x01000008)`** 가 하나 더 있다. run68 은 `ConsoleCommitState` 에서 `SetTEBLangID` 한 단계 아래를 안 열었다 (지도 `ConsoleInitialize_nodes.tsv` 에 `GetConsoleLangId` 깊이 3 이 있다). 그래서 run68 의 kernelbase 시작 함수 syscall 합 "15 ~ 16번" 도 **16 ~ 17번** (+ NLS 모름) 이 된다. 성공이면 `SetThreadLocale` 이 TEB `CurrentLocale` 을 쓴다 (위 ⑤).
  - run68 문서 맨 위에 정정 한 줄, syscall 표에 한 줄 (`14′`) 을 넣었다.
  - flow.md 2.5 run68 절 (트리 (6) 줄 · "콘솔" 글머리) 과 그림 2 `bc1` 라벨 ("syscall 3" → "syscall 4") 을 고쳤다.
  - 짐 지도 7.6.8 "콘솔 연결 핸들" 줄 · 3.3 `CurrentLocale` 줄에 이 syscall 을 근거로 붙였다 (줄 수 그대로).
- **run82 뒷정리** (explainer 알림 — 같은 스프린트 문서라 그 자리에서 고침, 사실이 틀린 것은 아님):
  - run82 "아직 확인 필요" 의 "정적 — 다음 바퀴 후보" 와 flow.md 4장 run82 정적 줄 → 대기열 **Q157** 로 적음.
  - run82 "호출 관계" 의 `RtlUserThreadStart │ +0x2c (CFG 간접) BaseThreadInitThunk` 는 가지 밖 줄이라 규칙대로 **그 줄 자리 `+0x27`** (ret `+0x2c` — `_calls.tsv` `+0x27/+0x2c`) 이어야 한다. 본문 질문 4 (`+0x27` / `+0x3d`) 와 맞춰 `+0x27 · +0x3d` 로 고쳤다.
  - run82 와 flow.md 기준 run82 줄의 로그 표기 `_supp_K1.txt:215` · `_P2.txt:4053` → 뒤는 보충 로그가 아니라 **`04_baggage-read_P2.txt:4053`** (그 줄이 `KdDebuggerEnabled : 0x3`) — 이름을 끝까지 적었다.

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 늘리지 않음 — 그림 2 `bc1` 라벨의 숫자 하나만 고침): 기준 (kernelbase pdb run 목록 · run83 근거 줄 · run82 로그 표기) · 2.5 머리 run 목록 · 2.5 run68 절 (트리 (6) 줄에 a4 · "콘솔" 글머리 정정 + run83 갈림 요약) · 2.8 `Log` 절 (`WriteFile` → `NtWriteFile` 한 줄 — dll2 TLS 콜백부터, 은퇴 앞에도) · **3장 새 절 "콘솔 입출력 — kernelbase 콘솔 API"** (진입점 뒤 이야기) · 4장 (run82 정적 줄 Q157, run83 새 줄 셋 — 동적 Q104 · 정적 남은 것 · PE_emulator 공백 · 정정 · 보탬) · 5장 (run 표 · 03_sprint 요청서 번호).
- 짐 지도 (짐 지도 바퀴 아님 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**). 이미 있는 줄에 근거만:
  - 3.2 머리 메모 (콘솔 코드가 이 블록에서 읽고 쓰는 칸 — `ConsoleFlags` 읽기만, `WindowFlags` · `Standard*` 조건부 쓰기 줄) · `ConsoleHandle` ② 줄 (넷째 syscall 이 읽음, −1 → 0) · `Standard*` ① 줄 (② 조건부 쓰기 두 곳, test.exe 안 탐).
  - 3.3 `CurrentLocale` 줄 — ② KERNELBASE `SetThreadLocale +0x5a` 쓰기 후보 (a4 성공일 때).
  - 7.2 KUSD `끝까지 0` 의 `SharedDataFlags.DbgConsoleBrokerEnabled` — 읽는 곳 `ConsoleAllocate +0x52` (쓰는 쪽은 그대로 모름).
  - 7.6.8 `ConsoleIsConsoleApplication` 줄 (읽는 곳) · "콘솔 연결 핸들" 줄 (a4 가 씀) · `WindowFlags` 메모 (줄 확정).
  - 기준 run83 줄 · 판 목록 (서른아홉째판) · 8장 두 표.
  - 짐 지도 바퀴 후보 (옮길지는 그때): 3.2 에 `WindowFlags` ② 조건부 줄 · `Standard*` ② 조건부 줄, 3.3 에 `CurrentLocale` ② KERNELBASE 줄.

## 아직 확인 필요

동적 — 결과서 "동적분석으로 확인해야 할 것" 번호 그대로, 모두 **Q104 VM 단계** (이번 묶음은 VM 안 함. 멈춤점은 test.exe 조건):

- 1 KUSD `dd 7ffe02f0 L1` 비트 6 — run44 `0x10e` `[관찰]` 이라 옛 길. 에뮬레이터 값도 같은지 (다르면 중개 길).
- 2 **넷째 syscall**: `kernelbase!GetConsoleLangId+0xe1` 에서 `r rcx` (핸들 = `params.ConsoleHandle` `0x60`?), `+0xe8` 에서 `r eax` (NTSTATUS) · `dw @rsp+0x58 L1` (LangId). 이어서 `kernelbase!SetThreadLocale+0x93` 이 서는지 (캐시에 없으면 — 위 ⑤), `+0x5a` 뒤 TEB `+0x108`.
- 3 서버가 돌려주는 값 (정적으로 못 정함): `GetConsoleMode` 모드 · `GetConsoleOutputCP` CP · `GetConsoleScreenBufferInfoEx` body `0x5c` · LangId — `kernelbase!ConsoleCallServer+0xde` · `GetConsoleMode+0xe2` · `GetConsoleOutputCP+0xe5` · `WriteConsoleInternal+0x114` · `ConsoleCallServerGeneric+0x110` (`eax` 와 body).
- 4 test.exe 가 콘솔에 쓰는 실제 길 — 소스로는 `WriteFile` (`sample/log.h:38` `[소스]`): `nt!NtWriteFile` test.exe 조건에서 핸들이 표준 출력 `0x6c` (run44 사진 `StandardOutput`) 인지, 첫 번이 dll2 TLS 콜백 (`Log`) 인지 (`k`), `nt!NtDeviceIoControlFile` (`IoControlCode == 0x500016`) 이 `Log` 에서는 안 서는지 (`WriteFile` 길엔 IOCTL 이 없다 `[정적]` — 서는 건 2.5 의 넷째 syscall 과, 있다면 CRT 초기화 쪽 `[추측]`).
- 5 −1 · −2 · −3 이름 — 부모 쪽 (정적으로도 가능, 아래 ①).
- 6 `GetFileType` 이 표준 핸들에 주는 값: `kernelbase!GetFileType+0x79` 의 `eax` · `dd @rsp+0x50 L1` (`DeviceType 0x50`?).
- 7 추천 멈춤점 (요약): `kernelbase!GetConsoleLangId+0xe8`, `kernelbase!ConsoleInitialize+0x19f` (run68 Q104 5 와 같은 자리), `kernelbase!ConsoleCommitState+0x91`, `kernelbase!ConsoleAllocate+0x150` (이 VM 은 안 탐 — 안 서는지 확인).

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① 부모 `KERNELBASE!CreateProcessInternalW` 가 `ConsoleHandle` 에 쓰는 값 — `DETACHED_PROCESS` · `CREATE_NEW_CONSOLE` · `CREATE_NO_WINDOW` 와 −1 · −2 · −3 의 짝 (결과서 동적 5 를 정적으로).
- ② `ConsoleSanitizeStandardIoObjects` (rva `0xc015c`) — `ConsoleFlags & 2` · `WindowFlags & 0x100` (`STARTF_USESTDHANDLES` `[추측]`) 갈래, 상태 비트 칸을 켜는 곳.
- ③ `ConsoleAttachOrAllocate` 의 `"attach"` 길을 부르는 곳 (지도 caller 2 — `ConsoleAllocate` 말고 하나 더).
- ④ ntoskrnl `NtSetSystemInformation` 클래스 `0x84` (b1′ — b1 실패 뒤 무엇을 켜나).
- ⑤ (flow-writer) `SetThreadLocale +0x93 MakeLocHashNode` 아래 — run68 정적 남은 것 ① 과 같은 함수라 같이 닫힘.

Q149b (run84) 로 넘길 것 (결과서 "Q149b 로 넘길 것" 그대로):

- (1) condrv.sys 쪽 — `IRP_MJ_CREATE` (`\Device\ConDrv\Server` · `\Device\ConDrv\Connect` · `<서버>\Reference` · `<참조>\Connect` · `<연결>\Input` · `<연결>\Output`, EA `"server"` / `"attach"` / `"broker"`, 접근 `0x12019f`) · IOCTL `0x500016` (설명서 해석 + API 번호 8개, `Client` 0 / 앱 핸들 차이) · `0x500023` (출력 8B PID) · `0x500037` (입력 매개변수 블록) · `IRP_MJ_WRITE` / `IRP_MJ_READ` · `FileFsDeviceInformation` (`DeviceType 0x50`) · 커널 몫 `NtSetSystemInformation(0x84)` · `NtSetInformationProcess(0x31)`.
- (2) CRT (ucrtbase) 가 부를 API `[추측]`: `GetStdHandle` · `GetFileType` · `GetConsoleMode` · `SetConsoleMode` · `GetConsoleOutputCP` · `GetConsoleCP` (이번에 안 읽음) · `WriteFile` · `WriteConsoleW` · `ReadFile` · `ReadConsoleW` · `GetConsoleScreenBufferInfo`. `isatty` 판단이 `GetFileType` 만인지 `GetConsoleMode` 까지인지, `WriteFile` 과 `WriteConsoleW` 를 언제 고르나, 표준 핸들 0 일 때. (test.exe 자신은 출력에 CRT 를 안 쓴다 — `sample/log.h:38` `WriteFile` 직접 `[소스]`. 그래도 `/MT` CRT 초기화가 표준 핸들을 묻는지는 run84 에서.)
- (3) 원문 "(가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표" — 이번 표 (a) · (b) · 표 A · B · C 와 위 (1) (2) 를 합쳐서.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점** `[정적]`: 이번 길을 가르는 칸 (`ConsoleHandle` · `ConsoleFlags` · `WindowFlags` · `Subsystem` · KUSD `SharedDataFlags` 비트 6) 에 디버거를 보는 줄은 없다. `DbgConsoleBrokerEnabled` 는 이름에 `Dbg` 가 붙지만 디버거 여부가 아니라 KUSD 기능 비트로 읽힌다 `[추측: 이름]` — run44 (KDNET) 값 0.
- 도구: 결과서 "스크립트 문제 없음". kernelbase 에 콘솔 구조체 · IOCTL 이름이 PDB 에 없어 (`pdb_struct.py --list` 0개) 이름은 모두 `[추측: 이름]` 으로 두었다.
