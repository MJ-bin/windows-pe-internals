# 80 console-kernelbase — 정적 분석 결과

- 요청서: `_workspace/sast/80_console-kernelbase.md` (Q149a, PE_emulator 요청 27 의 kernelbase 쪽 — 콘솔 입출력 길). 앞 바퀴 `_workspace/sast/63_dll-init-rest_result.md` §8 (`ConsoleInitialize`, run68) 을 먼저 읽었고, 겹치는 건 "§8" 로 가리키기만 한다. 이번에 새로 본 것만 적는다.
- 모듈: kernelbase (`ms/bin/.../System32/KernelBase.dll`), PDB guid_age `72CCE506944DE0267C172AD64287F9BD1` (sast/79 와 같은 값 = 같은 빌드 26100.9457). ntoskrnl 은 안 씀 (커널 GUID 검사 해당 없음).
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, 도는 Ghidra 작업 없음).
- 지도 (새로 만듦, `ms/disassemble/kernelbase/_map/`): `ConsoleInitialize` — 함수 30개 (`expanded=25, helper=5`, 끊긴 끝 31종 전부 `external` = import). `ConsoleCallServerGeneric` — 2개 (`ReadConsoleInternal` 하나만 부름).
- 뽑은 함수 (전부 `export`, **SCRIPT ERROR 없음**, 로그 `ms/ghidra/kernelbase.export.sast80{a..g}.stdout`):
  - 요청서 대로: `^(ConsoleInitialize|ConsoleCommitState|ConsoleAllocate|ConsoleCreateConnectionObject)$` depth 1 (→ 21개) / `^ConsoleLaunchServerProcess$` 0 / `^(ConsoleCallServer|ConsoleCallServerGeneric|ConsoleCallServerWithBuffers)$` depth 1 (→ 5개) / 질문 2·3·4·5 의 API 13개 (`GetStdHandle` … `GetFileType`) depth 0.
  - **넓힌 것과 이유** (모두 지도로 길을 먼저 찾고 필요한 것만, 아래에 쓴 이유 때문):
    - `ReadConsoleInternal` (depth 0) — `ReadConsoleW` 는 80바이트 껍데기이고 실제 IOCTL 입력을 만드는 곳이 이 함수 (`ConsoleCallServerGeneric` 의 유일한 caller). 질문 2.
    - `GetConsoleLangId` (depth 0) · `SetThreadLocale` (depth 1) — 지도에서 `ConsoleCommitState → SetTEBLangID → GetConsoleLangId` 가 **syscall 로 닿는 줄기인데 run68 이 안 열었음**. `SetConsoleOutputCP` 성공 뒤에도 같은 줄기를 탐. 질문 1 의 syscall 표에 들어가야 해서.
  - 새로 뽑힌 depth-1 callee 중 읽은 것: `ConsoleShouldAllocateConsole`, `ConsoleRationalizeAllocationOptionsWithPeb`, `ConsoleCloseIfConsoleHandle`, `ConsoleAttachOrAllocate`, `ConsoleCreateStandardIoObjects`, `ConsoleInitializeServerData`, `ConsoleCopyStringToBuffer`, `InitializeCtrlHandling`, `SetTEBLangID`. 호출 목록만 본 것: `ConsoleSanitizeStandardIoObjects`, `ConsoleCleanupConnectionState`, `ConsoleIsCallerInLowbox`, `InitExeName`.
  - 내가 파이썬으로 직접 읽은 것 (임시, 레포 밖): 바이너리 `.rdata` 의 문자열 (이름 7개 + 매니페스트 문자열 3개 + `dde.`/`hotkey.`/`Command Prompt`) 과 `GetFileType` 의 점프표 (`0xc63b0`/`0xc63c4`).
- **버전 확인: 일치.** `_calls.tsv` 의 `ret_off` 를 앞 결과서 값과 맞춰 봤다: `ConsoleInitialize +0x19f` → `ConsoleCreateConnectionObject` · `+0x1cf` → `ConsoleCommitState` (sast/63 §8 과 같음), `ConsoleCommitState +0x91` → `NtDeviceIoControlFile` · `+0xf0` → `NtSetInformationProcess` (run51 과 같음). 스크립트 문제 없음.
- 이 결과서의 `+0x` 는 전부 `.asm` 줄에서 다시 찾아 확인했다 (부르기는 `ret=` 줄, 점프 목적지는 "목적지 − 함수 시작").

## 한 줄 결론 (PE_emulator 용)

1. **run68 에서 빠졌던 4번째 syscall 이 있다.** `ConsoleInitialize` 의 "부모 콘솔에 붙기" 길은 syscall 이 3번이 아니라 **4번**이다. 마지막에 `ConsoleCommitState +0xfa/+0xff` → `SetTEBLangID` → `GetConsoleLangId +0xe1/+0xe8` 이 **`NtDeviceIoControlFile(새 핸들 0x60, 0x500016)`** 을 한 번 더 부른다 (API 번호 `0x01000008`, 출력 2바이트). sast/63 §8 ("syscall 3번") 과 표 12~14 에 한 줄 붙여야 한다.
2. **"ConsoleHandle = 0 → 콘솔 없음 → 조용히 성공" 이 아니다.** 콘솔 앱 (Subsystem 3) 이고 매니페스트가 없으면 `ConsoleHandle = 0` 은 **`ConsoleAllocate` (새 콘솔 만들기)** 로 간다. 조용히 AL=1 로 끝나는 길은 따로 있다 (질문 1(c) 표).
3. **콘솔 API 는 길이 세 가지다.** (가) `WriteFile`/`ReadFile` — kernelbase 안에 콘솔 갈림이 **없고** `NtWriteFile`/`NtReadFile` 로 바로 간다. (나) `GetConsoleMode` 류 — `NtDeviceIoControlFile(params.ConsoleHandle, 0x500016)`. (다) `WriteConsoleW`/`ReadConsoleW` — `NtDeviceIoControlFile(앱이 가진 콘솔 핸들 그 자체, 0x500016)`.
4. **`params.ConsoleHandle` 이 0 (또는 음수) 이면** (나)·(다) 는 **syscall 없이** 상태 `0xc0000008` 로 끝난다. (가) 는 상관없이 syscall 까지 간다.
5. `GetFileType` 은 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 하나. 장치 종류 `0x50` (콘솔) 이면 `FILE_TYPE_CHAR` (2).

## 질문별 답 [정적]

### 먼저: IOCTL `0x500016` 한 가지로 다 통한다 (질문 2 의 바탕)

- 번호 풀이 (`CTL_CODE` 규칙 계산, 이름은 `[추측]`): `0x500016` = 장치 종류 `0x50` (`FILE_DEVICE_CONSOLE`) · 함수 5 · 방식 2 (`METHOD_OUT_DIRECT`). `0x500023` = 함수 8 · 방식 3 (`NEITHER`). `0x500037` = 함수 13 · 방식 3. PDB 에 `CONSOLE_*`·`CD_*` 계열 타입·열거형은 **없다** (`pdb_struct.py --list` 0개) → 아래 이름은 모두 `[추측: 이름]` (칸 위치·크기는 `[정적]`).
- 부르는 모양은 언제나 같다: `NtDeviceIoControlFile(FileHandle, 0, 0, 0, &IoStatusBlock, 0x500016, &설명서, 설명서크기, NULL, 0)`. **데이터는 `InputBuffer` 에 직접 안 담고, "설명서" 안의 포인터가 가리키는 곳 (호출한 프로세스 메모리) 에 있다.** 그래서 에뮬레이터의 `NtDeviceIoControlFile` 몸은 설명서를 읽고 그 안의 포인터를 다시 따라가야 한다.
- **설명서 (`[추측: CD_USER_DEFINED_IO]`)**:

| 칸 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 8B | "Client" — 앱이 들고 있는 콘솔 핸들 값 (없으면 0) |
| `+0x08` | 4B | 입력 버퍼 개수 (최소 1) |
| `+0x0c` | 4B | 출력 버퍼 개수 (최소 1) |
| `+0x10 …` | 16B × (입력+출력) | 버퍼 칸 `{ 4B 크기, 4B 빈칸, 8B 포인터 }`. 입력 칸들 먼저, 출력 칸들 나중 |

  - 설명서 전체 크기 = `0x10 + 0x10 × (입력 + 출력)`: 입력 1·출력 1 이면 `0x30`, 입력 2·출력 1 이면 `0x40`, 입력 3·출력 2 이면 `0x60`.
  - **첫 입력 버퍼 = "메시지"**: `+0 API 번호 (4B)`, `+4 body 크기 (4B)`, `+8 body`. 크기 = `8 + body크기`. **첫 출력 버퍼 = 같은 메시지의 body 자리** (`메시지 + 8`, 크기 body 크기). 곧 서버는 답을 같은 자리에 덮어 쓴다.
  - 돌려주는 값 = `NtDeviceIoControlFile` 의 NTSTATUS 그대로 (서버가 완료시킨 상태). 메시지 안에 따로 상태 칸은 없다.
- API 번호 (`[추측: 이름]`, 값은 `.asm` 의 즉시값): 앞 1바이트 = 층 (1 또는 2), 뒤 = 번호.

| API 번호 | 쓰는 함수 (이번에 본 것) | body 크기 |
|---|---|---|
| `0x01000000` `[추측: GetCP]` | `GetConsoleOutputCP` (`+0xc1`) | 8 |
| `0x01000001` `[추측: GetMode]` | `GetConsoleMode` (`+0xc8`) | 4 |
| `0x01000002` `[추측: SetMode]` | `SetConsoleMode` (`+0x18`) | 4 |
| `0x01000005` `[추측: ReadConsole]` | `ReadConsoleInternal` (`+0x18c`) | 0x14 |
| `0x01000006` `[추측: WriteConsole]` | `WriteConsoleInternal` (`+0xe9`) | 8 |
| `0x01000008` `[추측: GetLangId]` | `GetConsoleLangId` (`+0xc4`) | 2 |
| `0x02000004` `[추측: SetCP]` | `SetConsoleOutputCP` (`+0x26`) | 8 |
| `0x02000007` `[추측: GetScreenBufferInfo]` | `GetConsoleScreenBufferInfoEx` (`+0x3f`) | 0x5c |

### 공통 경로 세 함수 + 인라인 (질문 2 "공통 경로")

| 함수 | rva | `NtDeviceIoControlFile` 의 `FileHandle` | 설명서 `Client` | 막는 줄 (syscall 안 하고 돌려줌) |
|---|---|---|---|---|
| `ConsoleCallServer` | `0xbe9a0` | **`PEB→ProcessParameters→ConsoleHandle`** (`+0x4d`) | 첫 인자 (앱 핸들, 또는 0) | `+0x5e TEST RSI,RSI` / `+0x61 JLE` → `+0x10c` `0xc0000008` (**부호 있는 비교 ≤ 0**) |
| `ConsoleCallServerWithBuffers` | `0xef540` | 같음 (`ConsoleHandle`) | 첫 인자 | `+0x7c/+0x7f` → `+0x13b` `0xc0000008`. 추가: 입력+출력 추가 버퍼가 `n+m+2 > 10` 이면 `+0x76 JA` → `+0x1a3` `0xc00000bb` |
| `ConsoleCallServerGeneric` | `0xdf180` | **첫 인자 (앱이 가진 핸들 그 자체)** | 둘째 인자 | `+0x6f/+0x72` 첫 인자 ≤ 0 → `+0x134` `0xc0000008` (**`ConsoleHandle` 는 안 봄**). 개수 `> 10` 이면 `+0x69` → `+0x1a1` `0xc00000bb` |
| (인라인) `GetConsoleMode` · `GetConsoleOutputCP` · `GetConsoleLangId` | | `ConsoleHandle` | 앱 핸들 / 0 / 0 | `ConsoleHandle ≤ 0` → `0xc0000008` |
| (인라인) `WriteConsoleInternal` | `0xf7cb8` | **앱이 가진 출력 핸들 그 자체** (`+0xd9 MOV RCX,R14`) | 0 (`+0xa2`) | `+0x50/+0x55` `ConsoleHandle == 0` → `+0x148` / `+0x84/+0x87` 앱 핸들 ≤ 0 → `+0x148` (둘 다 `0xc0000008`) |

- `ConsoleCallServer` 의 호출 모양: `(Client, 메시지 포인터, API번호, body크기)`. 안에서 메시지 앞 8바이트를 `API번호`·`body크기` 로 채우고 (`+0x9a`, `+0xc3`), 설명서 `0x30` 바이트를 만들어 `+0xd7/+0xde` 에서 IOCTL.
- `ConsoleCallServer` 의 부르는 곳이 `SetConsoleMode`·`SetConsoleOutputCP`·`GetConsoleScreenBufferInfoEx` 등 30곳 (decomp 머리 주석). `Generic` 의 caller 는 `ReadConsoleInternal` 하나 (지도 확인).

---

### 질문 1. 콘솔을 얻는 길 셋

**전체 갈림 (`ConsoleInitialize` 판 — 처음 분석한 §8 은 "붙기" 만 풀었다):**

| 조건 | 가는 곳 (줄) | syscall | `ConsoleInitialize` 반환 |
|---|---|---|---|
| exe 의 `Subsystem ≠ 3` (GUI 앱) | `ConsoleShouldAllocateConsole +0x16 CMP` / `+0x1d JZ` → `+0x7d` FALSE. `ConsoleInitialize +0xbf` 안 뛰고 `+0xc5` 로: `ConsoleIsConsoleApplication` = 0 이라 `+0xce` `ConsoleHandle ← 0`, 표준 핸들 칸 셋을 `ConsoleCloseIfConsoleHandle` (`+0xe2`·`+0xf7`·`+0x100`), `ConsoleConnectionState` 0 으로 | 칸이 0 이면 **없음**. 칸이 0 아니면 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) | AL=1 |
| 콘솔 앱 + **매니페스트 정책 `d`/`D` (detached) + `ConsoleHandle == 0`** | `ConsoleShouldAllocateConsole +0x7b JNZ` 안 뛰고 `+0x7d` FALSE → 위와 같은 `+0xc5` 갈래 (`ConsoleHandle ≠ 0` 이면 `-2` 만 아니면 TRUE) | 같음 | AL=1 |
| 콘솔 앱 + `ConsoleHandle = -1` | TRUE → `+0x166` → `+0x170` → `+0x179 CMP RDX,-1` `JZ` → `+0x1c6`: **빈 상태로 `ConsoleCommitState`** | `NtSetInformationProcess(-1, 0x31, {1}, 8)` **1번** (+ `InitializeCtrlHandling`·`SetTEBLangID` 는 syscall 없음: `ConsoleHandle` 가 0 이라 `GetConsoleLangId` 가 `0xc0000008` 로 막힘) | AL=1 |
| 콘솔 앱 + `ConsoleHandle = 0`, `-2`, `-3` (+ 매니페스트 정책이 없거나 detached 아님) | `+0x170 MOV RDX,[RBX+0x10]` → `+0x177 JZ` (0), `+0x183 JZ` (-2), `+0x189 JZ` (-3) → `+0x142 ConsoleAllocate` (**(b)**) | 아래 (b) 표 | 성공이면 AL=1, 실패면 `RtlDeleteCriticalSection(ConsoleStateLock)` 하고 **AL=0** (`+0x14f ~ +0x164`) |
| 콘솔 앱 + `ConsoleHandle` = 실제 핸들 (≠ 0, -1, -2, -3) | `+0x19a` `ConsoleCreateConnectionObject` → `+0x1ca` `ConsoleCommitState` (**(a)**) | 아래 (a) 표 | AL=1 |

- `ConsoleFlags & 4` (`+0x166`) 이면 표준 핸들 셋을 먼저 `ConsoleCloseIfConsoleHandle` (`+0x20f ~ +0x225`), `ConsoleFlags & 2` (`+0x1b9`) 이면 commit 전에 `ConsoleSanitizeStandardIoObjects` (`+0x22f`) — test.exe 는 둘 다 0 (sast/63 §8).
- `ConsoleRationalizeAllocationOptionsWithPeb` (`+0x8f`): `ConsoleHandle` 0·`-2` → 전역 `0x3aa9f8 ← 1`, `-3` → `← 2`, `-1` → `|= -1`. 이 값이 **연결 데이터 `+0x21` 바이트 (`0x3aa9f8 ≠ 2`)** 로 들어간다 (아래 EA 표).
- `-1`/`-2`/`-3` 이 각각 `DETACHED_PROCESS`/`CREATE_NEW_CONSOLE`/`CREATE_NO_WINDOW` 인지는 **`[추측]`** — 값을 **쓰는** 쪽 (부모의 `CreateProcessInternalW`) 은 이번에 안 읽음. 읽는 쪽 코드 (위 표) 에서는 `-1` 만 "연결 없이 commit", 나머지 셋 (0, -2, -3) 은 `ConsoleAllocate` 이다.

#### (a) 부모가 물려줌 (`ConsoleHandle` = 진짜 핸들) — syscall 표 (순서대로)

> `ConsoleInitialize` 의 앞부분 (락·`RtlImageNtHeader`·`ConsoleShouldAllocateConsole`) 은 sast/63 §8 표 그대로. 여기서는 syscall 만.

| # | 부르는 자리 (call/ret) | syscall | 값 | 쓰이는 곳 |
|---|---|---|---|---|
| a1 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` | **`NtCreateFile`** | 이름 `\Connect` (`0x2a6250`), **`RootDirectory` = 부모가 물려준 `ConsoleHandle`** (여기선 4; `NULL` 이면 `\Device\ConDrv\Connect`), 접근 `0x12019f`, `OBJECT_ATTRIBUTES.Attributes = 0x40`, 공유 7, 방식 2 (`FILE_CREATE`), 옵션 `0x20` (동기), **`EaBuffer` 모양은 아래**, `EaLength = 0x54b` | 새 핸들 → `[RBP+0x28]`. 끝나면 임시 힙 블록 `RtlFreeHeap` (`+0x1e1`) |
| a2 | `ConsoleCommitState +0x8a/+0x91` ← `ConsoleInitialize +0x1ca/+0x1cf` | **`NtDeviceIoControlFile`** | 핸들 = a1 의 새 핸들, IOCTL **`0x500023`** `[추측: 서버 PID 얻기]`, 입력 없음, **출력 8바이트** (`+0x70` 에 받음 = conhost PID) | 새 핸들이 0 이면 건너뜀 (`+0x4e/+0x51 JZ` → `+0x96`) |
| a3 | `ConsoleCommitState +0xe9/+0xf0` | **`NtSetInformationProcess`** | 프로세스 `-1`, 클래스 `0x31` (`ProcessConsoleHostProcess` = run51 의 `OwnerProcessId` 둘째 쓰기), 값 `(a2 의 PID) \| 1`, 크기 8 | 항상 (새 핸들이 0 이어도 값 `1` 로 부름) |
| a4 | `ConsoleCommitState +0xfa/+0xff` → `SetTEBLangID +0x12/+0x17` → `GetConsoleLangId +0xe1/+0xe8` | **`NtDeviceIoControlFile`** (**run68 에 없던 것**) | 핸들 = 이제 `params.ConsoleHandle` (= a1 의 새 핸들), IOCTL **`0x500016`**, API `0x01000008`, body 2바이트 (출력 `WORD` = 콘솔의 언어 ID) | 성공이면 `SetThreadLocale(LangId)` (`SetTEBLangID +0x20/+0x25`) — **syscall 없음** (NLS 표 조회 + `TEB+0x108 ← LCID`). 실패면 `RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 하고 무시 |

- 실패 갈래: a1 이 `0xc0000022` (ACCESS_DENIED) → `ConsoleIsCallerInLowbox` (`NtQueryInformationToken`) 가 로우박스라고 하면 `NtClose(ConsoleHandle)` 하고 **(b) `ConsoleAllocate`** 로 (`ConsoleInitialize +0x1d4 ~ +0x20a`). 그 밖의 실패는 AL=0.
- **a1 의 `EaBuffer` (`FILE_FULL_EA_INFORMATION` 한 칸, 총 `0x54b` 바이트)** — `ConsoleCreateConnectionObject +0xb2`·`+0xc3 ~ +0xe3` 와 `+0xe6` (복사) 로 만든다:

| 칸 | 값 |
|---|---|
| `+0x00` `NextEntryOffset` (4B) | 0 |
| `+0x04` `Flags` (1B) | 0 |
| `+0x05` `EaNameLength` (1B) | 6 |
| `+0x06` `EaValueLength` (2B) | `0x53c` |
| `+0x08` `EaName` (7B, NUL 포함) | `"server"` (ANSI, `0x2a6248`) |
| `+0x0f` 값 (0x53c B) | **연결 데이터** (아래) |

  - (`R8` = NULL 일 때 위가 전부다. `R8` 가 이름이면 — 즉 `ConsoleAttachOrAllocate` 의 `"attach"`/`"broker"` — 둘째 EA 칸이 뒤에 하나 붙는다 (`+0x54c`, 이름 `R8`, 값 `R9`/크기 `word`). `+0x79 JNZ` → `+0x21f`, `+0xf0 ~ +0x140`.)
  - **연결 데이터 0x53c 바이트** (`ConsoleInitializeServerData` `0xbfbb4` 가 채움; 칸 위치는 `[정적]`, 이름은 `[추측]`):

| 칸 | 크기 | 내용 (읽는 원본) |
|---|---|---|
| `+0x00` | 4B | `params.ShellInfo` (`+0xd8`) 에서 `"dde."` 뒤 숫자 (없으면 0) |
| `+0x04` | 4B | `WindowFlags & 0x200` 이면 `params.StandardInput` (`+0x20`) 값, 아니면 `ShellInfo` 의 `"hotkey."` 뒤 숫자 (없으면 0) |
| `+0x08` | 4B | `params.WindowFlags` (`+0xa4`), 전역 `0x3aa9fc ≠ 0` 이면 `\|= 1` |
| `+0x0c` | 2B | `params.FillAttribute` (`+0xa0`), `WindowFlags & 0x10` 일 때만 |
| `+0x0e` | 2B | 전역 `0x3aaa00` (= `ShowWindowFlags`), 위 `0x3aa9fc ≠ 0` 일 때만 |
| `+0x10 / +0x12` | 2B ×2 | `CountCharsX/Y` (`+0x98/+0x9c`), `WindowFlags & 8` |
| `+0x14 / +0x16` | 2B ×2 | `CountX/Y` (`+0x90/+0x94`), `WindowFlags & 2` |
| `+0x18 / +0x1a` | 2B ×2 | `StartingX/Y` (`+0x88/+0x8c`), `WindowFlags & 4` |
| `+0x1c` | 4B | `params.ProcessGroupId` (`+0x408`) (0 이면 `TEB+0x40` = 내 PID) |
| `+0x20` | 1B | `ConsoleIsConsoleApplication` (`0x3af188`) |
| `+0x21` | 1B | 전역 `0x3aa9f8 ≠ 2` |
| `+0x22` / `+0x24` | 2B / ≤0x208B | 창 제목 바이트 수 / 제목 (`params.WindowTitle` `+0xb0`; 비면 `"Command Prompt"` `0x2a62d8`) + NUL |
| `+0x22e` / `+0x230` | 2B / ≤0xfe B | 내 exe 이름 (`PEB→Ldr→첫 모듈→BaseDllName`) 바이트 수 / 이름 |
| `+0x330` / `+0x332` | 2B / ≤0x208B | 현재 디렉터리 (`params.CurrentDirectory.DosPath` `+0x38`) 바이트 수 / 문자열 |

  - 이 줄기가 읽기만 하는 PEB 칸: `PEB+0x10` (이미지 베이스), `+0x18` (`Ldr`), `+0x20` (`ProcessParameters`), `+0x30` (`ProcessHeap`). **PEB 자신에 쓰는 칸은 없다.**
- **`ConsoleCommitState` 가 쓰는 칸** (`ConsoleCommitState.asm`): `params.WindowFlags` (`+0xa4`) 에서 비트 `0x200`·`0x400` 지움 (`+0x32 ~ +0x36`, `+0x146 ~ +0x15f`) / `params.ConsoleHandle` (`+0x10`) ← 상태의 새 핸들 (`+0x3c/+0x40`) / `params.StandardInput`·`Output`·`Error` (`+0x20`/`+0x28`/`+0x30`) ← 상태의 칸, **상태 첫 비트(`1`/`2`/`4`) 가 켜지고 값이 0 이 아닐 때만** (`+0x10e ~ +0x144`) / 전역 `ConsoleConnectionState` (`0x3af120`, 0x38B) ← 상태 통째 복사 (`+0x96 ~ +0xbb`) / `HandlerListLength`·`AllocatedHandlerListLength`·`HandlerList`·`SingleHandler` (`InitializeCtrlHandling`, syscall 없음). `params.ConsoleFlags` (`+0x18`) 는 **읽기만**.
- **지금 우리 (PE_emulator)**: (a) 는 해당 없음 (부모가 없으니 `ConsoleHandle` 가 진짜 핸들일 수 없음). 단, 이 표가 "진짜 VM 이 한 일" 이니 비교용으로 쓴다.

#### (b) 새 콘솔 (`ConsoleHandle` = 0, -2, -3 — 그리고 로우박스 실패) — syscall 표 (`ConsoleAllocate`, 974B)

- `ConsoleAllocate +0x52 TEST byte [0x7ffe02f0],0x40` — **`KUSER_SHARED_DATA.SharedDataFlags` 의 비트 6 `DbgConsoleBrokerEnabled`** (ntoskrnl PDB 로 확인) 가 켜져 있으면 `+0x5a JNZ` → `+0x301` **`ConsoleAttachOrAllocate`** 부터 시도한다 (`0xc0000080` 을 돌려주면 `+0x30d JZ` 로 `+0x60` 아래 옛 길로). **이 VM 은 `SharedDataFlags = 0x10e` (`[관찰]` run44; sast/63) → 비트 6 = 0 → 옛 길만 탄다.** 에뮬레이터의 `0x7ffe02f0` 값이 `0x10e` 와 같은지는 확인 필요.
- **옛 길 (이 VM 이 타는 길)**: 새 콘솔 서버 (conhost) 를 **kernelbase 가 직접 띄우지 않는다.** conhost 실행은 `ConDrv` 장치에 IOCTL `0x500037` 로 "이 매개변수로 띄워 줘" 하고 맡긴다.

| # | 부르는 자리 | syscall | 값 | 쓰이는 곳 |
|---|---|---|---|---|
| b1 | `ConsoleAllocate +0x149/+0x150` | `NtCreateFile` | 이름 `\Device\ConDrv\Server` (`0x2a62f8`), 접근 `0x12019f`, `Attributes = 0x42`, 공유 7, 방식 2 (`FILE_CREATE`), 옵션 0, EA 없음 | 서버 핸들 → `[RSP+0x60]` |
| b1′ | `ConsoleAllocate +0x175/+0x17c`, `+0x20b/+0x212` | (b1 이 실패하면만) `NtSetSystemInformation(0x84, {DWORD 1}, 4)` 하고 **b1 한 번 더** | 클래스 `0x84` 의 커널 쪽 뜻은 이번에 안 봄 `[공백]` | 두 번째도 실패하면 그 상태를 돌려줌 → `ConsoleInitialize` AL=0 |
| b2 | `ConsoleAllocate +0x2b4/+0x2bb` | `NtCreateFile` | 이름 `\Reference` (`0x2a6230`), **`RootDirectory` = b1 서버 핸들**, 접근 `0x12019f`, `Attributes = 0x40`, 공유 7, 방식 2, 옵션 `0x20` | 참조 핸들 → `[RSP+0x70]` |
| b3 | `ConsoleLaunchServerProcess +0xc6/+0xcd` ← `ConsoleAllocate +0x326/+0x32b` | `NtDeviceIoControlFile` | 핸들 = **b1 서버 핸들**, IOCTL **`0x500037`** `[추측: 서버 띄우기]`, **입력 = `RTL_USER_PROCESS_PARAMETERS`** (`RtlCreateProcessParametersEx` 로 만든 블록, 크기 = 블록의 `Length` 칸 `+4`, 플래그 1 = 정규화됨), 출력 없음 | 입력 안: 이미지 경로 `\??\<NtSystemRoot>\system32\conhost.exe`, 명령줄 `…conhost.exe 0xffffffff -ForceV1` (`0x2a61d0` 형식 문자열, `RtlStringCbPrintfW` `+0x8c`), 데스크톱 정보 = 내 `params.DesktopInfo` (`+0xc0`) 복사. 끝나면 `RtlDestroyProcessParameters` (`+0xd9`) |
| b4 | `ConsoleAllocate +0x332/+0x339` | `NtClose` | b1 서버 핸들 | |
| b5 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleAllocate +0x357/+0x35c` | `NtCreateFile` | 이름 `\Connect`, **`RootDirectory` = b2 참조 핸들**, 접근 `0x12019f`, EA `"server"` + 연결 데이터 (a1 과 같은 모양, `0x54b`) | 연결 핸들 → 상태 `+8`, 참조 핸들 → 상태 `+0x10`, 상태 `+0x30` = 연결 핸들 ≠ 0 |
| b6 | `ConsoleCreateStandardIoObjects +0xb1/+0xb8` ← `ConsoleAllocate +0x39f/+0x3a4` | `NtCreateFile` | 이름 `\Input` (`0x2a6298`), `RootDirectory` = 연결 핸들 (상태 `+8`), 접근 `0x12019f`, `Attributes = 0x42` (상속), 방식 2, 옵션 `0x20` | 표준 입력 → 상태 `+0x18` |
| b7 | `ConsoleCreateStandardIoObjects +0x151/+0x158` | `NtCreateFile` | 이름 `\Output` (`0x2a6288`), 나머지 b6 과 같음 | 표준 출력 → 상태 `+0x20` |
| b8 | `ConsoleCreateStandardIoObjects +0x1a8/+0x1af` | `NtDuplicateObject` | `-1` → b7 핸들 → `-1`, 옵션 `6` (같은 접근·속성) | 표준 에러 → 상태 `+0x28` (**출력의 복사본**). 상태 `+0` = 7 |
| b9~b12 | `ConsoleCommitState` | a2, a3, a4 와 같음 (핸들 = b5 의 연결 핸들) | | `params.ConsoleHandle` ← 연결 핸들, `params.Standard*` ← b6~b8 (`+0x10e ~ +0x144`) |

- `params.WindowFlags & 0x100` (`+0x393 TEST … 0x100`) 이면 b6~b8 대신 `ConsoleSanitizeStandardIoObjects` (이미 가진 표준 핸들 검사·대체, `NtCreateFile`/`NtDuplicateObject`/`NtClose` 가 조건부로 나옴 — **이번엔 안 읽음**).
- 실패하면 `ConsoleCleanupConnectionState` (`NtClose` 5개까지) 하고 상태 돌려줌.
- 중개 (broker) 길 (`SharedDataFlags` 비트 6 이 켜진 경우): `ConsoleAttachOrAllocate(NULL)` → `ConsoleCreateConnectionObject(root=NULL, EA 이름 "broker", 값 = 내 데스크톱 이름 0x208B)` 가 `\Device\ConDrv\Connect` 를 직접 `NtCreateFile` (EA 둘) → `\Reference` (`NtCreateFile`, root = 연결 핸들) → b6~b8 (또는 sanitize). conhost 띄우는 IOCTL 이 없다. 이 길은 이 VM 이 안 타서 더 안 읽음.
- **지금 우리 (PE_emulator)**: 위 b1~b8·a2~a4 가 실제로 나가는 syscall 목록이다. 에뮬레이터가 "가짜 콘솔" 을 세우려면 이 중 **핸들을 돌려주는 `NtCreateFile` 5개 (b1·b2·b5·b6·b7)** 와 `NtDeviceIoControlFile` 3종 (`0x500037`, `0x500023`, `0x500016`+API) 이 답해야 한다.

#### (c) 콘솔 없음

- **표 위쪽 참조.** 어디서 갈리나: ① `Subsystem ≠ 3` (`+0x7b CMP word [NT+0x5c],3`, `+0x83 JNZ` → `+0x13a` AL=0 → `ConsoleIsConsoleApplication` = 0, `ConsoleShouldAllocateConsole +0x1d JZ`) ② 매니페스트 `consoleAllocationPolicy` = `d`/`D` + `ConsoleHandle == 0` (`ConsoleShouldAllocateConsole +0x56 ~ +0x7d`) → 둘 다 `ConsoleInitialize +0xbf JNZ` 가 **안 뛰고** `+0xc5` 로 떨어진다 (`+0xce` 의 `ConsoleHandle ← 0` 은 콘솔 앱이 **아닐** 때만). ③ `ConsoleHandle = -1` 은 `ConsoleCommitState` 를 빈 상태로 부름 (syscall 1번).
- **안 하는 것**: `NtCreateFile` (연결·서버·참조·입력·출력) 전부, 핸들 만들기, `0x500023`·`0x500037`·`0x500016` IOCTL. ① ② 에서는 `ConsoleCommitState` 도 안 불러서 `NtSetInformationProcess` 도 없다.
- **표준 핸들은 어떻게 되나**: ① ② 갈래 (`+0xc5` 이하) 는 `ConsoleCloseIfConsoleHandle` 로 **칸이 콘솔 장치를 가리킬 때만** 닫고 칸을 0 으로 만든다 (`ConsoleCloseIfConsoleHandle +0x1a ~ +0x72`: 칸 0 → 그대로 반환 / `(값 & 0x10000003) == 3` → syscall 없이 칸만 0 / 그 밖엔 `NtQueryVolumeInformationFile` 해서 `DeviceType == 0x50` 이면 `NtClose` + 칸 0). **파일·파이프 핸들은 그대로 남는다.** 입력 칸 (`+0x20`) 은 `WindowFlags & 0x200`, 출력 칸 (`+0x28`) 은 `WindowFlags & 0x400` 이 켜져 있으면 건드리지 않고 (그 칸이 표준 핸들이 아닌 다른 값을 담고 있다는 뜻), 에러 칸 (`+0x30`) 은 항상 검사. ③ (`-1`) 은 `ConsoleCommitState` 가 `params.Standard*` 를 안 바꾸니 (`상태 첫 비트 = 0`) **매개변수 블록 값 그대로**. 모두 0 이면 그대로 0.
- **PE_emulator 질문 ("모두 0 상태가 `ConsoleInitialize` 를 성공 (AL 1) 으로 끝내나")**: **exe 가 콘솔 앱 (Subsystem 3) 이고 매니페스트가 없으면 아니오.** `ConsoleHandle = 0` 은 위 표처럼 `ConsoleAllocate` 로 간다 (`ConsoleInitialize +0x177 JZ` → `+0x142`). AL=1 로 끝나려면 (i) exe 의 Subsystem 이 3 이 아니거나 (ii) `ConsoleHandle` 가 `-1` 이거나 (iii) 매니페스트 정책이 detached 이어야 한다. **지금 PE_emulator 가 이미 `NtWriteFile(0)` 까지 왔다는 것 = 위 셋 중 하나이거나 `ConsoleAllocate` 의 `NtCreateFile` 들이 이미 가짜 성공을 돌려준 경우인데, 후자라면 표준 핸들이 0 이 아니어야 한다** (b6~b8 이 `params.Standard*` 를 채우니까) → `[공백]` (에뮬레이터 쪽에서 `ConsoleInitialize` 를 지났는지, `0x3af188` 값이 무엇인지 확인).

---

### 질문 2. 자주 쓰는 콘솔 API 표

> 모두 `ms/disassemble/kernelbase/<이름>.asm` + `ms/decomp/kernelbase/<이름>.c`. "핸들 0" 칸이 질문 4 와 겹친다.

#### 표 A — syscall 로 가는 길과 입력 모양

| API (rva) | 하위 호출 | syscall | IOCTL / API 번호 | 입력 (body 칸) | FileHandle ← | Client ← |
|---|---|---|---|---|---|---|
| **`GetStdHandle`** (`0xec300`) | `BaseSetLastNTError` (결과가 -1 일 때만) | **없음** | — | — | — | — |
| **`WriteFile`** (`0xb2070`) | `NtWriteFile` (`+0x86/+0x8d`; 겹침 쓰기면 `+0x113/+0x11a`), `0x103` 이면 `NtWaitForSingleObject` (`+0x1d1/+0x1d8`) | `NtWriteFile` | — (IOCTL 아님) | `(hFile, 0,0,0, &IoStatus, buf, len, 0, 0)` | `hFile` (표준 핸들 상수면 `params.Standard*` 로 바꿈) | — |
| **`WriteConsoleW`** (`0xf7c80`) → `WriteConsoleInternal` (`0xf7cb8`) | | `NtDeviceIoControlFile` (`+0x10d/+0x114`) | `0x500016` / `0x01000006` | body 8B: `+0` 쓴 **바이트 수 (출력)**, `+4` 유니코드 플래그 (1 바이트, `WriteConsoleW` 는 1). 입력 버퍼 2 (메시지 `0x10`B, **글자 `nChars×2` 바이트 `lpBuffer`**), 출력 버퍼 1 (body 8B). 설명서 `0x40`B | 앱 출력 핸들 | 0 |
| **`ReadFile`** (`0x22a90`) | `NtReadFile` (`+0x86/+0x8d`; 겹침 `+0x123/+0x12a`), `0x103` 이면 `NtWaitForSingleObject` (`+0x207/+0x20e`) | `NtReadFile` | — | `(hFile, 0,0,0, &IoStatus, buf, len, 0, 0)` | `hFile` (같은 상수 바꾸기) | — |
| **`ReadConsoleW`** (`0xdeef0`) → `ReadConsoleInternal` (`0xdef48`) → `ConsoleCallServerGeneric` (`+0x19c/+0x1a1`) | | `NtDeviceIoControlFile` (Generic `+0x109/+0x110`) | `0x500016` / `0x01000005` | body **0x14B** (칸: `+0` 유니코드 1B, `+1` 0, `+2` exe 이름 길이(글자) 2B, `+4` 초기 글자 바이트 수 4B, `+8` `CtrlWakeupMask` 4B (`pInputControl→+8`), `+0xc` 컨트롤 키 상태 (**출력**), `+0x10` 읽을 바이트 수 (**입력** `nChars×2` / **출력** 읽은 바이트 수)). 추가 입력 버퍼 2 (내 exe 이름 `≤ 0x28` 글자, 초기 글자 `lpBuffer`), 추가 출력 버퍼 1 (`lpBuffer` `nChars×2` 바이트). 설명서 `0x60`B | **앱 입력 핸들** | 0 |
| **`GetConsoleMode`** (`0xf2860`) | (인라인, `RtlNtStatusToDosError` + `RtlSetLastWin32Error`) | `NtDeviceIoControlFile` (`+0xdb/+0xe2`) | `0x500016` / `0x01000001` | body 4B (출력 `DWORD` 모드). 설명서 `0x30` | `params.ConsoleHandle` | 앱 핸들 (첫 인자) |
| **`SetConsoleMode`** (`0xbe6f0`) | `ConsoleCallServer` (`+0x26/+0x2b`) | 같음 | `0x500016` / `0x01000002` | body 4B (입력 `DWORD` 모드 `EDX`) | `params.ConsoleHandle` | 앱 핸들 |
| **`GetConsoleScreenBufferInfo`** (`0xbe7f0`) | `GetConsoleScreenBufferInfoEx` (`+0x33/+0x38`) — `cbSize = 0x60` 짜리 임시 구조체를 만들고 결과 중 22바이트만 복사 (`+0x3c ~ +0x5f`) | (아래) | | | | |
| **`GetConsoleScreenBufferInfoEx`** (`0xbe870`) | `ConsoleCallServer` (`+0x48/+0x4d`) | 같음 | `0x500016` / `0x02000007` | body **0x5c**B (출력, 칸은 표 C) | `params.ConsoleHandle` | 앱 핸들 |
| **`GetConsoleOutputCP`** (`0xb9db0`) | (인라인) | 같음 (`+0xde/+0xe5`) | `0x500016` / `0x01000000` | body 8B: `+4` = 1 (**출력** 쪽 코드 페이지를 달라는 뜻 `[추측]`), `+0` = 출력 `DWORD` 코드 페이지. 설명서 `0x30` | `params.ConsoleHandle` | **0** |
| **`SetConsoleOutputCP`** (`0xbe1a0`) | `ConsoleCallServer` (`+0x37/+0x3c`), 성공이면 `SetTEBLangID` (`+0x42/+0x47`) → `GetConsoleLangId` IOCTL 한 번 더 | 같음 | `0x500016` / `0x02000004` | body 8B: `+0` 코드 페이지, `+4` = 1 | `params.ConsoleHandle` | **0** |

- 위 "FileHandle ←" 에서 `params` = `PEB→ProcessParameters` (`GS:[0x60]` → `+0x20`), `ConsoleHandle` = `params+0x10`.
- `WriteConsoleInternal` 입력 문자 수 `nChars` → 바이트 `ESI = nChars × 2` (유니코드) / `nChars` (`WriteConsoleA`, 플래그 0) (`+0x5b`, `+0x6b CMOVZ`).

#### 표 B — 기대하는 답과 실패일 때

| API | 성공 (서버가 돌려주는 것) | 반환값 | 실패면 (NTSTATUS < 0) | `ConsoleHandle ≤ 0` 이면 |
|---|---|---|---|---|
| `GetStdHandle` | — | 칸의 값 그대로 (0 도 그대로) | 칸 = -1 이면 `BaseSetLastNTError(0xc0000008)` 하고 -1 반환 (`+0x71/+0x76`). 모르는 상수도 같음 (`+0x9c OR RBX,-1`) | (상관없음) |
| `WriteFile` | `IoStatusBlock.Information` → `*lpNumberOfBytesWritten` (`+0xac ~ +0xb0`) | TRUE | `BaseSetLastNTError(상태)` (`+0x141 ~ +0x148`) 하고 FALSE. 상태가 `0x8xxxxxxx` (경고) 면 쓴 바이트 수는 채워줌 (`+0x17a ~ +0x183`) | **상관없음** — 그래도 `NtWriteFile` 까지 간다 |
| `WriteConsoleW` | body `+0` 바이트 수 → `*lpNumberOfCharsWritten = 바이트/2` (`WriteConsoleInternal +0x14f ~ +0x161`) | TRUE | `RtlSetLastWin32ErrorAndNtStatusFromNtStatus(상태)` 하고 FALSE (`WriteConsoleW +0x1d ~ +0x2b`). 상태 `0xc000000d` (INVALID_PARAMETER) 는 `0xc0000008` 로 **바꿔서** (`WriteConsoleInternal +0x11d/+0x122` → `+0x148`) | `ConsoleHandle == 0` 또는 앱 핸들 ≤ 0 → **syscall 없이** `0xc0000008` → FALSE (`ERROR_INVALID_HANDLE`) |
| `ReadFile` | `Information` → `*lpNumberOfBytesRead` | TRUE | 실패면 FALSE + `BaseSetLastNTError`. **예외: 상태 `0xc0000011` (END_OF_FILE) 이고 겹침이 아니면 TRUE, 읽은 바이트 0** (`+0xa7 ~ +0xba`). 겹침 읽기에서 EOF 는 FALSE | 상관없음 |
| `ReadConsoleW` | body `+0x10` 읽은 바이트 → `*lpNumberOfCharsRead = /2`, body `+0xc` → `pInputControl→+0xc` (컨트롤 키 상태) (`ReadConsoleInternal +0x1f4 ~ +0x20e`) | TRUE | `RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 하고 FALSE. **예외: `0xc0000120` (CANCELLED) 는 오류 코드는 세우되 TRUE** (`ReadConsoleW +0x43 ~ +0x4d`). 상태 `0x101` (ALERTED)·`0xc0000120` 이면 `NtYieldExecution` 도 한 번 (`ReadConsoleInternal +0x213/+0x21a`). `pInputControl→+0` (nLength) 이 `0x10` 이 아니면 `pInputControl` 을 **무시** (`+0xf2 CMP` / `+0xf5 JNZ` → `+0x117`). `pInputControl→+4` (nInitialChars) 가 읽을 글자 수보다 크면 `0xc000000d` (`+0xfa CMP` / `+0xfc JA` → `+0x10d`) | 첫 인자 (앱 입력 핸들) ≤ 0 이면 Generic 이 `0xc0000008` (이 함수는 `ConsoleHandle` 안 봄) |
| `GetConsoleMode` | body `+0` `DWORD` → `*lpMode` (`+0x137 ~ +0x13b`) | TRUE | `RtlNtStatusToDosError` → `RtlSetLastWin32Error` 하고 FALSE (`+0xed ~ +0x10b`) | `ConsoleHandle ≤ 0` → 상태 `0xc0000008` → `ERROR_INVALID_HANDLE` (6) FALSE (`+0x5f/+0x62` → `+0x130`) |
| `SetConsoleMode` | (상태만) | TRUE | 위와 같은 오류 처리, FALSE | 같음 (`ConsoleCallServer` 안) |
| `GetConsoleScreenBufferInfoEx` | body 0x5c 를 사용자 구조체로 옮김 (표 C) | TRUE | `cbSize ≠ 0x60` → **syscall 없이** `0xc000000d` → `ERROR_INVALID_PARAMETER` FALSE (`+0x2c/+0x2f` → `+0x107`). 서버 실패면 오류 처리 후 FALSE | `0xc0000008` FALSE |
| `GetConsoleScreenBufferInfo` | 위 결과에서 22바이트 복사 (`dwSize`·`dwCursorPosition`·`wAttributes`·`srWindow`·`dwMaximumWindowSize`) | TRUE | `Ex` 가 FALSE 면 복사 안 하고 FALSE (`+0x38 TEST EAX` `+0x3a JZ`) | 같음 |
| `GetConsoleOutputCP` | body `+0` `DWORD` | **코드 페이지 값** | 오류 코드 세우고 **0 반환** (`+0xee ~ +0x10a`) | `0xc0000008` → 0 반환 (`+0x5c/+0x5f` → `+0x12a`) |
| `SetConsoleOutputCP` | (상태만) 성공이면 `SetTEBLangID` | TRUE | 오류 처리 후 FALSE (`+0x40 JS` → `+0x58`) | `0xc0000008` FALSE |

#### 표 C — `GetConsoleScreenBufferInfoEx` 의 body 0x5c 바이트 칸 (`[정적]` 위치, 이름 `[추측]`)

| body 칸 | 크기 | 사용자 구조체로 가는 곳 (`CONSOLE_SCREEN_BUFFER_INFOEX`) |
|---|---|---|
| `+0x00` | 4B (`SHORT X,Y`) | `dwSize` (`+4`) |
| `+0x04` | 4B | `dwCursorPosition` (`+8`) |
| `+0x08` | 2B ×2 | 창 왼쪽·위 (`srWindow.Left/Top`, `+0xe/+0x10`) |
| `+0x0c` | 2B | `wAttributes` (`+0xc`) |
| `+0x0e` | 2B ×2 | 창 **너비·높이** → `Right = Left + 너비 − 1`, `Bottom = Top + 높이 − 1` (`+0x12/+0x14`) |
| `+0x12` | 4B | `dwMaximumWindowSize` (`+0x16`) |
| `+0x16` | 2B | `wPopupAttributes` (`+0x1a`) |
| `+0x18` | 1B | `bFullscreenSupported` (`+0x1c`) |
| `+0x1c` | 64B (16 × `COLORREF`) | `ColorTable` (`+0x20`) |

- 계산 줄: `GetConsoleScreenBufferInfoEx +0x7f ~ +0x9f` (`DEC AX; ADD CX,AX` 가 Right/Bottom 만드는 곳).

#### 질문 2 마지막: "`WriteFile` 이 콘솔 핸들이면 condrv 로 가고 아니면 `NtWriteFile` 로 가는 갈림 줄"

- **그런 줄이 없다.** `WriteFile.asm` 에 있는 비교는 `+0x39 CMP ECX,-0xb` · `+0x42 CMP ECX,-0xc` · `+0x4b CMP ECX,-0xa` (표준 핸들 상수 → 칸 값으로 바꾸기, 아래 질문 4) 와 `+0x63 TEST RSI,RSI` (`lpOverlapped` 가 있나) 뿐이다. 핸들 하위 비트 검사도, `ConsoleHandle` 비교도 없다. **콘솔 핸들이든 파일이든 똑같이 `NtWriteFile`** 로 간다. 콘솔이면 그 `NtWriteFile` 을 받는 쪽이 `ConDrv` 장치일 뿐이다 (그 쪽은 Q149b). `ReadFile` 도 같다 (`+0x3d`·`+0x46`·`+0x4f` 가 상수 비교의 전부).
- **지금 우리 (PE_emulator)**: `WriteFile`/`ReadFile` 에는 콘솔 코드가 없다는 점이 쉬운 소식이다 — 에뮬레이터의 `NtWriteFile`/`NtReadFile` 몸이 "핸들이 가짜 콘솔 출력/입력이면 화면 쪽으로" 하면 된다. 반대로 `WriteConsoleW`/`ReadConsoleW`/`GetConsoleMode`/… 는 `NtDeviceIoControlFile` 몸이 설명서를 풀어야 한다.

---

### 질문 3. `GetFileType` (`0xc6290`, 259B)

- 부르는 syscall: **`NtQueryVolumeInformationFile`** 하나 (`+0x72/+0x79`). 인자: 핸들 = 인자(또는 표준 핸들 상수를 바꾼 값), `FsInformationClass = 4` (`FileFsDeviceInformation`, `+0x60 [RSP+0x20] = 4`), 출력 8바이트 (`FILE_FS_DEVICE_INFORMATION { DeviceType, Characteristics }`, `+0x5a R9D = 8`).
- 결과 갈래 (`DeviceType` = 첫 4바이트):
  - 상태 < 0 → `BaseSetLastNTError(상태)` 하고 **0** (`+0x7e ~ +0x9e`).
  - **`DeviceType == 0x50` (콘솔 장치) → `FILE_TYPE_CHAR` (2)** (`+0x87 CMP EAX,0x50` / `+0x8c MOV EAX,2`).
  - 그 밖: `DeviceType − 2 ≤ 0x29` 이면 점프표 (`+0xb8 ~ +0x105`, 표 위치 rva `0xc63b0` + 바이트 색인 `0xc63c4`): **`FILE_TYPE_DISK` (1)** ← `2 ~ 8`, `0x24`; **`FILE_TYPE_PIPE` (3)** ← `0x11` (`FILE_DEVICE_NAMED_PIPE`); **`FILE_TYPE_CHAR` (2)** ← `0xb`, `0xf`, `0x15`, `0x16`, `0x18`, `0x1b`, `0x1c`, `0x1d`, `0x2b`; 그 밖은 `RtlSetLastWin32Error(0)` 하고 **0** (`FILE_TYPE_UNKNOWN`, 오류 코드 없음). 범위 밖 (`> 0x2b`) 도 같은 (`+0xc0 ~ +0xce`).
- 핸들 0 / 잘못된 핸들:
  - **핸들 0 → syscall 없이** `BaseSetLastNTError(0xc0000008)` 하고 0 (`+0x15 TEST RCX,RCX` / `+0x18 JZ` → `+0xa6 ~ +0xb0`). `ERROR_INVALID_HANDLE` (6) 이 세워진다 (변환은 `[추측]`, ntdll 몫).
  - 표준 핸들 상수 (-10/-11/-12) 를 줬는데 그 칸 값이 0 이면 마찬가지 (`+0x55 TEST RCX,RCX` / `+0x58 JZ` → `+0xa6`).
  - 0 이 아닌 잘못된 핸들 (예 -1 `INVALID_HANDLE_VALUE`) 은 **kernelbase 가 걸러내지 않는다** — 그대로 `NtQueryVolumeInformationFile` 에 가고, 거기서 오는 상태를 오류 코드로 세우고 0 을 돌려준다 (어떤 상태인지는 커널 몫 `[동적]`).
- **지금 우리 (PE_emulator)**: 표준 핸들 0 → `GetFileType(0)` 은 **syscall 없이 0 + `ERROR_INVALID_HANDLE`**. 콘솔 가짜 핸들을 만든다면 `NtQueryVolumeInformationFile(FileFsDeviceInformation)` 이 `DeviceType = 0x50` 을 돌려줘야 `FILE_TYPE_CHAR` (CRT 가 `isatty` 로 읽음; CRT 는 Q149b).

### 질문 4. 표준 핸들이 0 일 때 (콘솔 없음)

| 함수 | 0 일 때 | syscall 까지 가나 |
|---|---|---|
| `GetStdHandle(-10/-11/-12)` | 칸의 0 을 **그대로** 돌려줌, 오류 코드 안 세움 (`+0x66 CMP RBX,-1` 에서 -1 만 오류, `+0x6a JNZ` → `+0x76`). 입력(-10)/출력(-11) 은 `WindowFlags & 0x200`/`& 0x400` 이면 칸 안 읽고 0 (`+0x49 ~ +0x53`, `+0x1d ~ +0x27` → `+0x98 XOR EBX,EBX`) | 없음 |
| `WriteFile(0, …)` | kernelbase 가 **안 막는다.** `+0x39`·`+0x42`·`+0x4b` 의 상수 비교는 0 과 안 맞고 → `+0x86/+0x8d` `NtWriteFile(0, …)`. 커널이 `0xc0000008` 을 돌려주면 `+0x141/+0x148` `BaseSetLastNTError` 하고 FALSE | **간다** (`NtWriteFile`) |
| `ReadFile(0, …)` | 같음 (`+0x86/+0x8d` `NtReadFile(0, …)`) | **간다** |
| `GetFileType(0)` | 막음 (질문 3) | 없음 |
| `WriteConsoleW(0, …)` | 막음 (`+0x84/+0x87` 앱 핸들 ≤ 0 → `+0x148`) | 없음 |
| `ReadConsoleW(0, …)` | 막음 (`Generic +0x6f/+0x72`) | 없음 (`ExeNameLock` 공유 락과 이름 복사는 먼저 함) |
| `GetConsoleMode(0, …)` 등 (나) 계통 | 앱 핸들은 안 보고 **`params.ConsoleHandle ≤ 0`** 이면 막음. `ConsoleHandle` 가 진짜면 Client=0 으로 서버까지 감 | `ConsoleHandle` 에 달림 |

- **답: `GetStdHandle` 은 0 을 그대로 주고, `WriteFile(0, …)` 은 kernelbase 가 먼저 막지 않고 `NtWriteFile(0)` 까지 간다.** 사용자가 본 "`NtWriteFile(핸들 0)` 이 26번 `0xc0000008`" 과 이 코드가 맞아떨어진다 (`[정적]` 일치; 그 `26번` 이 `WriteFile` 에서 온 건지는 `[공백]`).
- **지금 우리 (PE_emulator)**: 맞다 — `WriteFile(0)` 이 syscall 까지 가는 건 코드대로다. 다만 "표준 핸들 0 = 콘솔 없음" 이 `ConsoleInitialize` 성공을 뜻하지 않는다 (질문 1 (c)).

### 질문 5. 표준 출력이 파일·파이프일 때

- **예 — `WriteFile` 은 `NtWriteFile` 만 부른다.** 파일이든 파이프든 콘솔이든 `WriteFile` 안의 syscall 은 `NtWriteFile` (+ `0x103` 일 때 `NtWaitForSingleObject`) 뿐이고 condrv 로 갈라지는 코드가 kernelbase 에 없다 (질문 2 마지막). 콘솔일 때 condrv 가 끼는 건 "받는 장치가 ConDrv" 라서이지 kernelbase 가 고른 게 아니다.
- 파이프·파일이면 앞에서 `GetFileType` 이 `DISK` (1) / `PIPE` (3) 을 돌려주고 (CRT 가 이걸 보고 콘솔 전용 API 를 안 쓰는 것으로 `[추측]`, Q149b), `WriteConsoleW` 같은 콘솔 API 는 앱이 직접 부르지 않으면 안 불린다.
- **지금 우리 (PE_emulator)**: 맞다 — 표준 출력을 파일로 이어도 kernelbase 쪽은 `NtWriteFile` 하나로 같다.

---

## 동적분석으로 확인해야 할 것 (Q104 에 붙임)

1. **`dd 7ffe02f0 L1` 비트 6** — run44 에서 `0x10e` (`[관찰]`) → 비트 6 = 0 → `ConsoleAllocate` 가 옛 길 (b1~b8) 을 탄다. 에뮬레이터의 값이 같은지 (다르면 `ConsoleAttachOrAllocate` 길).
2. **a4 — 4번째 syscall 확인**: `kernelbase!GetConsoleLangId+0xe1` 의 call, `+0xe8` 의 `r eax` (NTSTATUS), `dw @rsp+0x58 L1` (LangId). 핸들 = `params.ConsoleHandle` (`0x60`) 인지.
3. **서버가 실제로 돌려주는 값** (정적으로는 못 정함): `GetConsoleMode` 의 모드, `GetConsoleOutputCP` 의 코드 페이지, `GetConsoleScreenBufferInfoEx` body 0x5c, `GetConsoleLangId` 의 WORD. 멈춤점 `kernelbase!ConsoleCallServer+0xde` · `kernelbase!GetConsoleMode+0xe2` · `kernelbase!GetConsoleOutputCP+0xe5` · `kernelbase!WriteConsoleInternal+0x114` · `kernelbase!ConsoleCallServerGeneric+0x110` (call 직후, `eax` 와 body).
4. **콘솔에 쓰는 실제 길**: test.exe 가 `WriteFile`/`WriteConsoleW` 중 무엇을 쓰는지는 CRT 몫 (Q149b). 확인용: `nt!NtWriteFile` / `nt!NtDeviceIoControlFile` (조건 `IoControlCode == 0x500016` 그리고 `ProcessParameters` 핸들 `0x60`) 에 test.exe 한정 멈춤점. `NtWriteFile` 핸들 값이 표준 핸들 칸 (`0x68`/`0x6c`/`0x70` — run44 P1a) 인지.
5. **`-1`/`-2`/`-3` 이름**: 부모 쪽 `CreateProcessInternalW` 가 `DETACHED_PROCESS`/`CREATE_NEW_CONSOLE`/`CREATE_NO_WINDOW` 에 어떤 값을 쓰는지 (정적 후속 가능 — 이번 요청 밖).
6. **`GetFileType` 이 표준 핸들에 돌려주는 값**: test.exe 에서 `kernelbase!GetFileType+0x79` (ret) 의 `eax`, `dd @rsp+0x50 L1` (DeviceType = `0x50` 이어야 `FILE_TYPE_CHAR`).
7. 추천 멈춤점 (요약): `kernelbase!GetConsoleLangId+0xe8` (a4), `kernelbase!ConsoleInitialize+0x19f` (a1 새 핸들, 앞 바퀴), `kernelbase!ConsoleCommitState+0x91` (a2 PID), `kernelbase!ConsoleAllocate+0x150` (b1 — 이 VM 에선 안 탐, 확인용).

## Q149b 로 넘길 것

**(1) condrv.sys 쪽에서 확인할 것** (IRP 종류별 — kernelbase 가 보낸 것):

| 종류 | 무엇 | kernelbase 에서 온 자리 |
|---|---|---|
| `IRP_MJ_CREATE` | `\Device\ConDrv\Server`, `\Device\ConDrv\Connect`, `<서버>\Reference`, `<참조>\Connect`, `<연결>\Input`, `<연결>\Output`; EA 이름 `"server"` (값 0x53c, 위 표), `"attach"`/`"broker"` (둘째 EA); 접근 `0x12019f` | a1, b1, b2, b5, b6, b7 |
| IOCTL `0x500016` (함수 5, OUT_DIRECT) | 설명서 해석 + API 번호별 분기. 이번에 본 번호: `0x01000000`(GetCP)·`01`(GetMode)·`02`(SetMode)·`05`(ReadConsole)·`06`(WriteConsole)·`08`(GetLangId)·`0x02000004`(SetCP)·`07`(GetScreenBufferInfo). `Client` 가 0 일 때와 앱 핸들일 때의 차이 | 질문 2 |
| IOCTL `0x500023` (함수 8, NEITHER) | 출력 8B = 서버 PID | a2 |
| IOCTL `0x500037` (함수 13, NEITHER) | 입력 = `RTL_USER_PROCESS_PARAMETERS` (conhost 띄우기) | b3 |
| `IRP_MJ_WRITE` / `IRP_MJ_READ` | 콘솔 출력·입력 핸들에 대한 `NtWriteFile`/`NtReadFile` (`WriteFile`/`ReadFile` 이 보냄) | 질문 2 마지막 |
| `FileFsDeviceInformation` 질의 | 콘솔 객체의 `DeviceType = 0x50` | `GetFileType`, `ConsoleCloseIfConsoleHandle` |
| 커널 몫 | `NtSetSystemInformation` 클래스 `0x84` (b1′), `NtSetInformationProcess` 클래스 `0x31` (a3) | |

**(2) CRT (ucrtbase) 가 부를 API 목록** (이번에 본 kernelbase API 중 CRT 가 쓸 것으로 `[추측]` — ucrtbase 안은 안 읽음): `GetStdHandle`, `GetFileType`, `GetConsoleMode`, `SetConsoleMode`, `GetConsoleOutputCP`, `GetConsoleCP`, `WriteFile`, `WriteConsoleW`, `ReadFile`, `ReadConsoleW`, `GetConsoleScreenBufferInfo`. 확인할 것: `isatty` 판단이 `GetFileType` 만으로 되나 `GetConsoleMode` 까지 보나, `WriteFile` 과 `WriteConsoleW` 중 언제 무엇을 쓰나, 표준 핸들 0 일 때 CRT 가 어떻게 하나.

**(3) 정적 후속 후보**: ① `CreateProcessInternalW` 가 `ConsoleHandle` 에 쓰는 값 (-1/-2/-3 이름), ② `ConsoleSanitizeStandardIoObjects` (STARTF_USESTDHANDLES 갈래), ③ `ConsoleAttachOrAllocate` 의 `"attach"` 길을 부르는 곳 (이번엔 `ConsoleAllocate` 가 `RDX = 0` 으로만 부름 — 다른 caller 는 지도의 caller 수 2).

## run68 (sast/63 §8) 과 다른 점 (정정 제안)

- §8 의 "syscall 3번" 과 표 12~14 → **4번**: `NtCreateFile` 1 · `NtDeviceIoControlFile(0x500023)` 1 · `NtSetInformationProcess(0x31)` 1 · **`NtDeviceIoControlFile(0x500016, API 0x01000008)` 1 (`GetConsoleLangId`)**. `ConsoleCommitState` 표 (§8 `+0xfa/+0xff` `SetTEBLangID`) 에서 한 단계 아래를 안 연 것이 원인. 지도 `ConsoleInitialize_nodes.tsv` 에 `GetConsoleLangId` (깊이 3) 가 있다.
- run68 해설서·`baggage.md` 의 `ConsoleInitialize` 줄기 설명도 같이 고칠지는 오케스트레이터가 정한다 (나는 `runs/` 를 안 만졌다).

## 판정

**분석성공.** 요청서 질문 1~5 모두 `[정적]` 답과 `.asm` 줄 근거가 있다. 못 정한 것은 `[공백]` 으로 위에 표시 (`NtSetSystemInformation` 클래스 `0x84` 의 뜻, `-1/-2/-3` 의 이름, 서버가 돌려주는 값, `ConsoleSanitizeStandardIoObjects` 안). 스크립트 문제 없음.
