# 2026-11-14 run92 — bcrypt 시작 함수 (`DllMain`, Ghidra `entry`) 의 attach 길과 TRUE 최소 조건 — `\Device\KsecDD` · IOCTL `0x390400` (Q172, PE_emulator 요청 32 — 한 바퀴로 끝): **샘플 221개가 막힌 까닭은 run91 과 같은 뿌리 — `NtOpenFile(\Device\KsecDD)` 이 "status 0 인데 핸들 칸을 안 씀" 하나다. 핸들 칸은 전역 `g_hIoDevice` (bcrypt rva `0x25ae0`, PDB 이름) 이고, `IoCallKernelDriver` 는 열기 status 만 보고 (`+0x1d0 TEST R12D` · `+0x1d3 JNS`) 칸 값을 검사 없이 그대로 `NtDeviceIoControlFile` 에 넘긴다 (`+0x8e`). 칸이 0 이면 커널이 `0xc0000008` 을 주고, `BcpRegisterConfigChangeNotifyNoLogging` → `InitializeSystemPreferredCache` → `InitializeCNG` 이 그 음수를 차례로 올려 `DllMain +0x93` 에서 FALSE. 재시도는 없다 (로그의 `NtOpenFile` 1번). TRUE 가 되려면 바깥은 둘만 맞추면 된다 — `NtOpenFile` 이 status ≥ 0 과 함께 0 아닌 핸들을 칸에 쓰고, 그 핸들로 `NtDeviceIoControlFile(0x390400, 입력 0x68, 출력 8)` 이 status ≥ 0 (출력 · IOSB 안 봄, PENDING 이어도 안 기다림). 입력 `0x68` 에 든 핸들은 이벤트 하나 (`+0x60`) 뿐, 세마포어 · PID · 함수 주소는 없다. bcryptprimitives 와 달리 `GetModuleHandleExW(5, &DllMain)` 실패도 FALSE 다 (다음 벽 후보). test.exe 는 이 DLL 을 안 올린다 (사진 모듈 6개, `\KnownDlls` 목록에도 없음 `[관찰]` run44).** `DllMain +0x2c LdrDisableThreadCalloutsForDll` · `+0x80 WppInitUm` · `+0x85 McGenEventRegister_EtwEventRegister` · `+0x8a InitializeCNG` (`+0x66 GetTrustedEnvironment` (`+0x4 IsEnclave +0x44 NtQueryInformationProcess(0x56)` · `+0xd IsTrustlet +0x34 NtQueryInformationProcess(0)`) · `+0x2e InitializeLoader` · `+0x40 InitializeConfig` (`+0x31 InitializeProcessImageNameUM +0xb9` → kernelbase `QueryFullProcessImageNameW +0xb1 NtQueryInformationProcess(0x2b)` · `+0x46 TlgRegisterAggregateProviderEx` · `+0x62` 꼬리 `IoStartup`) · `+0x52 InitializeSystemPreferredCache` (`+0x38 RtlInitializeResource` → ntdll `+0xb3` · `+0xe2 NtCreateSemaphore` · `+0xd0 BcpRegisterConfigChangeNotifyNoLogging` (`+0x68 CreateEventW` → kernelbase `+0x76 NtCreateEvent` · `+0xda` · `+0x136 IoPack_Configuration_ParamBlock` · `+0x15a IoCallKernelDriver` (`+0x1b5 _IoOpenDevice +0x7a NtOpenFile` · `+0xc2 NtDeviceIoControlFile(0x390400)`)))) → `+0x93 JZ +0xca` → `+0x12d GetModuleHandleExW` → `+0x13b JNZ +0x1c5` TRUE / 그 밖은 `+0x189` FALSE

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### 요청 32 결론 (run92)

- **막힘은 `NtOpenFile` 의 핸들 칸 하나** `[정적]` — run91 (`\Device\CNG`) 과 같은 뿌리, 다른 모양.
  - `_IoOpenDevice +0x66 LEA RCX,[g_hIoDevice]` — `NtOpenFile` 의 첫 인자가 **전역 칸의 주소** 다. 지역 칸에 받아 옮기지 않는다.
  - `IoCallKernelDriver +0x1ba MOV R12D,EAX` → `+0x1d0 TEST R12D,R12D` · `+0x1d3 JNS +0x8e` — **status 만 본다.**
  - `+0x8e MOV RCX,[g_hIoDevice]` 를 그대로 `+0xc2 NtDeviceIoControlFile` 첫 인자로 쓴다. 열고 난 뒤 칸이 0 인지 보는 줄은 없다 (열기 **전** `+0x85 TEST RAX,RAX` · `+0x88 JZ` 는 "이미 열었나" 검사).
  - 칸이 0 → 커널이 `0xc0000008` (지금 엔진도 같은 답) → `+0xd0 JS +0x15c` → `+0x168 JNZ +0x101` → 음수 반환 → 위로 올라가 FALSE.
- **TRUE 최소 조건 (일반 프로세스 — `g_TrustedEnvironment = 1`)** `[정적]`:
  1. `NtOpenFile(\Device\KsecDD)` → status ≥ 0 **이고 `*FileHandle` (= bcrypt `+0x25ae0`) 에 0 아닌 값**.
  2. `NtDeviceIoControlFile(그 핸들, 0x390400, 입력 0x68, 출력 8)` → status ≥ 0. 출력 8B · IOSB 는 아무도 안 본다 (0 으로 둬도 됨).
  3. `NtCreateEvent` · `NtCreateSemaphore` × 2 · 힙 할당 둘 성공 (핸들 0 아님), `GetModuleHandleExW(5, &DllMain)` 성공.
  4. 나머지 (`NtQueryInformationProcess` `0x56` · `0x2b` · `NtTraceControl`) 는 아무 답이나 — 단 `0x56` 성공이면 첫 DWORD 가 1 · `0x10` · `0x11` 이 아닐 것, class 0 답 `+0x38` bit 7 = 0 (run91 과 같은 함수).
- **재시도 없음** — 한 번 열고 한 번 IOCTL. 그래서 로그의 `NtOpenFile` 이 1번이다 (run91 은 5번).
- **IOCTL 기능 번호는 입력 안에 있다** — 코드는 늘 `0x390400`, 입력 `+0x4` 가 기능 번호. attach 는 **`0x10500`** ("설정 바뀜 알림 등록", `BcpRegister… +0x155`). 같은 코드로 `BCryptResolveProviders` 는 `0x20000` 을 보낸다 (`+0x590`) — 그쪽은 출력을 읽는다 (다음 벽 후보, 아래).
- **`STATUS_PENDING` (`0x103`) 도 성공으로 친다** (`+0xd0 JS` 는 부호만). Event 인자가 0 이고 attach 길에 `NtWaitForSingleObject` 가 없다 (run91 의 kernelbase `DeviceIoControl` 처럼 기다리는 줄 없음). 그래도 엔진은 0 이 안전.
- **bcrypt 만의 FALSE 갈래 — `GetModuleHandleExW(5, &DllMain, &h)` 실패** (`DllMain +0x13b JNZ +0x1c5` 안 탐 → `+0x189`). 5 = 고정 (PIN) + 주소로 찾기. kernelbase 안은 `RtlPcToFileHeader` (`+0x88`) · `LdrAddRefDll` (`+0x130`) — ntdll 본문은 안 읽음 `[공백]`. 엔진이 KsecDD 를 고치면 이 줄에 처음 닿는다.
- **`NtClose` × 5 (로그 9번) 중 셋은 코드로 설명된다** `[정적]` — 이벤트 1 (`BcpRegister… +0x1e9 CloseHandle`) + 세마포어 2 (`CleanupSystemPreferredCache +0x8e RtlDeleteResource` → ntdll `+0x12` · `+0x1b NtClose`). 나머지 2 는 bcrypt 코드에서 못 찾음 `[공백]` (정리 길의 ntdll 쪽일 것 `[추측]`). **TRUE 길에서는 attach 가 아무것도 안 닫는다.**
- **attach 가 TRUE 로 끝나면 남는 것** `[정적]` (결과서): `g_hIoDevice` ≠ 0 (열린 KsecDD 핸들 1) · 이벤트 핸들 (rva `0x25b10`, 1) · `g_pCachedRngRWLock` (rva `0x25bc8`, 힙 `0x60` — 세마포어 2) · rva `0x25b18` (힙 `0x28` 임계영역) · rva `0x25ac4` = 1 · `g_TrustedEnvironment` = 1 · `g_fLoadCNGDone` = 1 · `g_hInstance+8` |= `0xe`. DLL 은 고정 (PIN) 된다.

### 원문 질문이 어디서 답해졌나

| 원문 요청 32 | 답한 곳 | 한 줄 · 지금 PE_emulator |
|---|---|---|
| 1 여는 함수 · 무엇을 보나 · FALSE 자리 · 칸 이름 | 아래 "attach 차례 표" 19 ~ 24 · "FALSE 로 가는 자리" | `_IoOpenDevice` (`IoCallKernelDriver +0x1b5`) 가 연다. 보는 것은 `NtOpenFile` status 와 IOCTL status, **핸들 칸은 안 봄**. 칸 = **`g_hIoDevice`** (`?g_hIoDevice@@3REAXEA`). FALSE 는 `DllMain +0x189` 한 점, 오는 길 둘. 지금: **값은 다 맞음, 핸들 칸 비움이 틀림** |
| 2 IOCTL `0x390400` 입력 · 출력 · TRUE 조건 · 실패 | 아래 "입력 칸 표" · "최소 조건 표" 8 | 입력 `0x68` = 머리만 (상수 `0x1a2b3c4d` · 기능 `0x10500` · "없음" 표시 `-1` 여럿 · **이벤트 핸들 `+0x60`**). 출력 8B 는 **안 읽음**. 성공 status 만. 실패면 FALSE (넘어가지 않음). 지금: **길이 · 코드 맞음, 핸들만 채우면 status 0 으로 통과** |
| 3 TRUE 뒤 첫 사용 (이름만) | 아래 "아직 확인 필요" 정적 ② | `g_hIoDevice` 를 다시 읽는 곳 = `IoCallKernelDriver` (다음 IOCTL) · `BCryptResolveProviders` (직접 IOCTL, 기능 `0x20000`, **출력을 `IoUnpack_ProviderRef` 로 읽음**) · `IoCallKernelDriverQueryLoop` · `IoShutdown`. 출력 0 으로 터질 자리는 attach 안엔 없고 뒤쪽 `BCryptResolveProviders` 후보 |
| 4 NtQIP `0x2b` · 세마포어 · 이벤트 · `0x56` | 아래 차례 표 6 · 11 · 14 · 17 | `0x2b` (길이 `0x218` = 머리 `0x10` + 글자 260) 는 **결과를 버림**. PDB 에 `PROCESSINFOCLASS` 없음 — 이름 `ProcessImageFileNameWin32` 는 `[추측]` (커널 처리기가 `IoQueryFileDosDeviceName` 을 부름 `[정적]`). 세마포어 둘 = `RtlInitializeResource` 의 읽기·쓰기 잠금 (`g_pCachedRngRWLock`). 이벤트 = KsecDD 알림용, IOCTL 입력에 들어감. `0x56` = run91 과 같은 `IsEnclave` |
| 5 최소 조건 표 | 아래 "최소 조건 표" · "run91 과 나란히" | — |

### attach 차례 표 (일반 프로세스, 결과서 1-a 를 줄임 — 근거 줄은 flow-writer 가 `.asm` 으로 다시 봄)

| # | 자리 (call/ret) | 부르는 것 · 하는 일 | syscall · 갈림 |
|---|---|---|---|
| 1 | `DllMain +0x23 / +0x26` | `CMP EDX,1 · JNZ +0x18d` — 이유 1 만 아래로 (`+0x11` 에서 `g_hInstance` ← `RCX`) | — |
| 2 | `+0x2c/+0x33` | `LdrDisableThreadCalloutsForDll(RCX = 자기 hModule)` | ntdll 본문 syscall 없음 (보탬 ③). 반환 안 봄 |
| 3 | `+0x41/+0x46` · `+0x46/+0x4b` | `_DllMainCRTStartupForGS2` · `SafeAllocaInitialize` | 반환 안 봄 |
| 4 | `+0x80/+0x85` · `+0x85/+0x8a` | `WppInitUm` (→ `+0x5f EtwRegisterTraceGuidsW`) · `McGenEventRegister_EtwEventRegister` (→ `+0x27 EtwEventRegister`) | `NtTraceControl` 후보 (로그 1번). 반환 안 봄 (바로 다음이 call) |
| 5 | **`+0x8a/+0x8f`** | **`InitializeCNG`** (rva `0xb1c0`) — 아래 6 ~ 18 | `+0x91 TEST` · **`+0x93 JZ +0xca`** — 0 이 아니면 FALSE ① |
| 6 | `InitializeCNG +0x6` · `+0xb ~ +0x13` → `+0x66/+0x6b` | `SymCryptInitEnvWindowsUsermodeWin8_1nLater` · `g_TrustedEnvironment` (rva `0x25ac0`) 0 이면 `GetTrustedEnvironment` → `+0x4 IsEnclave` / `+0xd IsTrustlet` | **`IsEnclave +0x44/+0x4b NtQueryInformationProcess(-1, 0x56, buf, 0xb0)`** (로그 2번), 0 이면 `IsTrustlet +0x34/+0x3b (-1, 0, buf, 0x40)`. run91 과 같은 함수 · 같은 오프셋 |
| 7 | `InitializeCNG +0x15 / +0x17` | `TEST AL,0x18 · JZ +0x2e` — 엔클레이브면 `+0x19` 로 **초기화 통째로 건너뛰고 0** | 일반 (1) 은 `+0x2e` |
| 8 | `+0x2e/+0x33` | `InitializeLoader` → `_InitializeLoaderLock` → `InitializeCriticalSection` | 늘 0 (`+0x37 JNZ` 안 닿음) |
| 9 | `+0x40/+0x45` | **`InitializeConfig`** — 아래 10 ~ 12 | 늘 0 (`+0x49 JNZ` 안 닿음) |
| 10 | `InitializeConfig +0x1f / +0x22` · `+0x2d / +0x2f` | 환경 `& 1` 이면 아래로 (아니면 `+0x4d`) | 일반은 아래로 |
| 11 | `+0x31/+0x36` → `InitializeProcessImageNameUM +0xb9/+0xc0` → kernelbase `QueryFullProcessImageNameW +0xb1/+0xb8` | **`NtQueryInformationProcess(-1, 0x2b, buf, 0x218, &len)`** (로그 3번; class = `+0xae LEA EDX,[RBX+0x1b]`, 길이 = `+0x74 LEA R15D,[0x10+RCX*2]`) | **결과 버림** (`InitializeConfig` 는 `EAX` 안 봄) |
| 12 | `+0x36/+0x3d` · `+0x42 / +0x44` · `+0x46/+0x4b` → `+0x4b JMP +0x5e` · `+0x62` 꼬리 `JMP IoStartup` | `RtlDllShutdownInProgress` (참이면 `+0x44 JNZ` 로 건너뜀) · `TlgRegisterAggregateProviderEx` (안에서 `TraceLoggingRegisterEx_EventRegister_EventSetInformation` 을 부름, 지도 depth 4) · `IoStartup +0x10` `g_hIoDevice = 0` · `+0x1e InitializeCriticalSection(g_csIoInitialize)` · `+0x2a` rva `0x25ad8` = 1 | `NtTraceControl` 후보 (로그 4번). 반환 안 봄. `IoStartup` 은 0 (보탬 ① — 결과서의 `+0x59` 는 이 길에 없음) |
| 13 | **`InitializeCNG +0x52/+0x57`** | **`InitializeSystemPreferredCache`** (rva `0x59e0`) — 아래 14 ~ 18 | **여기서만 음수가 나온다.** `+0x59 TEST` · `+0x5b JNZ +0x6d` → `+0x86 UninitializeCNG` → `+0x8b JMP +0x25` (그 status 반환) |
| 14 | `InitializeSystemPreferredCache +0x19/+0x20` · `+0x38/+0x3f` | `RtlAllocateHeap(…, 0x60)` → `g_pCachedRngRWLock` · `RtlInitializeResource` | **ntdll `RtlInitializeResource +0xb3/+0xb8` · `+0xe2/+0xe7 NtCreateSemaphore`** (로그 5번, `+0xc4 JS`). 힙 NULL → `+0x2f JZ +0x1b1` `0xc0000017` |
| 15 | `+0x98/+0x9f` · `+0xb7/+0xbe` | `RtlAllocateHeap(…, 0x28)` → rva `0x25b18` · `RtlInitializeCriticalSection` | NULL → `+0xae JZ +0x165` `0xc0000017`. 음수 → `+0xc7 JS +0x111` |
| 16 | **`+0xc9 ~ +0xd0/+0xd5`** | **`BcpRegisterConfigChangeNotifyNoLogging(&rva 0x25b10)`** (rva `0x5bf4`) | `+0xd9 JNS +0x157` → rva `0x25ac4` = 1, 0 반환. 음수면 `+0x145 TEST · +0x147 JNS` 안 탐 → `+0x149 CleanupSystemPreferredCache` → 그 status |
| 17 | `BcpRegister… +0x68/+0x6f` → kernelbase `CreateEventW +0x76/+0x7d` | **`NtCreateEvent(&h, 0x1f0003, NULL, 1, 0)`** (로그 6번) | `+0x74` 칸 `[RSI]` (= rva `0x25b10`) 에 저장 · `+0x77 TEST RAX · +0x7a JZ +0x1af` (NULL → `GetLastError` → `WinErrorToNtStatus` → 음수) |
| 18 | `+0x80 ~ +0x90` · `+0xda/+0xdf` · `+0x136/+0x13b` | 환경 `& 1` 이면 (`+0x8e TEST AL,1 · +0x90 JZ +0x16e`) `IoPack_Configuration_ParamBlock` 두 번 (크기 → `0x68`, 그다음 버퍼 `[RSP+0x80]` 채움) | 계산만. `+0xe3` · `+0x13f JNZ +0x1e1` 은 이번 인자로 안 닿음 |
| 19 | **`+0x155` · `+0x15a/+0x15f`** | **`IoCallKernelDriver(0x10500, 입력, 0x68, NULL, NULL)`** (rva `0x7f50`) — 아래 20 ~ 24 | **`+0x163 JS +0x1e1`** → `+0x1e9 CloseHandle(이벤트)` → 음수 반환 |
| 20 | `IoCallKernelDriver +0x55` · `+0x5b` | 입력 `+0x0` ← `0x1a2b3c4d`, `+0x4` ← 기능 번호 | — |
| 21 | `+0x5e / +0x61` → `+0x121` → `+0x6a / +0x6d` → `+0x73` · `+0x78` | 출력 길이 포인터 (5번째 인자 `R14`) · 출력 버퍼 (`RDI`) 가 둘 다 NULL → 제 지역 `[RSP+0x60]` 8B 를 출력 칸으로 (보탬 ②) | — |
| 22 | `+0x7e ~ +0x88` | `g_hIoDevice` 가 0 이면 `+0x88 JZ +0x196` | 처음엔 0 (`IoStartup +0x10`) |
| 23 | `+0x196 ~ +0x1cb` · **`+0x1b5/+0x1ba`** | `EnterCriticalSection(g_csIoInitialize)` → `+0x1a9` 다시 0 인지 → **`_IoOpenDevice`** (rva `0x8154`) → `LeaveCriticalSection` | **`_IoOpenDevice +0x2a RtlInitUnicodeString(L"\Device\KsecDD")` · `+0x7a/+0x81 NtOpenFile(&g_hIoDevice, 0x100003, &OA{0x30, 루트 0, 속성 0}, &IOSB, 7, 0x20)`** (로그 7번) |
| 24 | **`+0x1ba` · `+0x1d0 / +0x1d3`** → **`+0x8e ~ +0xc2/+0xc9`** | status ≥ 0 이면 칸 검사 없이 `+0x8e` 로 → **`NtDeviceIoControlFile(g_hIoDevice, 0, 0, 0, &IOSB[RSP+0x50], 0x390400, 입력, 0x68, [RSP+0x60], 8)`** (로그 8번) | 열기 음수 → `+0x1d9` 그 status. IOCTL `+0xce TEST · +0xd0 JS +0x15c` (음수 → 실패, `0xc0000023` 이면 `+0x161 JZ +0x133` `0xc00000e5`) |
| 25 | `+0xd6 / +0xd9` → `+0xff` | `TEST R14,R14 · JZ` — 출력 길이 포인터 NULL → 출력 · IOSB 안 보고 `XOR EAX,EAX` | **성공 0** |
| 26 | `InitializeCNG +0x5d ~ +0x64` → `+0x19` | `g_hInstance+8` 에 `0x2`·`0x4`·`0x8` (`+0x39` · `+0x4b` · `+0x5d`) · `g_fLoadCNGDone = 1` · 0 | — |
| 27 | `DllMain +0xca ~ +0x11c` | WPP 플래그 `[WPP_MAIN_CB+0x1c] & 0x20` 이면 `+0xed GetModuleFileNameW` · `+0x117 WPP_SF_qS` (보탬 ④) | ETW 세션이 켤 때만. TRUE/FALSE 에 안 닿음 |
| 28 | **`+0x12d/+0x134`** | **`GetModuleHandleExW(5, &DllMain, &h)`** (`+0x121` · `+0x126`) | `+0x139 TEST` · **`+0x13b JNZ +0x1c5`** — 실패면 FALSE ② |
| 29 | `+0x1c5` → `+0x1d9 RET` | `MOV EAX,1` | **TRUE** |

### FALSE 로 가는 자리 (attach 전부, 결과서 1-d)

FALSE 는 `DllMain +0x189 XOR EAX,EAX` → `+0x18b JMP +0x1ca` 한 점으로 모인다.

| # | 자리 | 조건 | 일반 프로세스 |
|---|---|---|---|
| ① | `+0x93 JZ +0xca` 안 탐 → `+0xa6 JZ` / `+0xb0 JZ` → `+0x189` (WPP 켜졌으면 `+0xc5 JMP +0x170` → `+0x184 WPP_SF_sDsd` → `+0x189`) | `InitializeCNG` 이 0 아님 | **여기가 막힘.** 실제로 0 아닌 값이 나오는 곳은 `InitializeCNG +0x5b JNZ` 하나 (← `InitializeSystemPreferredCache` 음수 ← `BcpRegister… +0x163 JS` ← `IoCallKernelDriver +0xd0 JS` ← IOCTL `0xc0000008`) |
| ② | `+0x13b JNZ +0x1c5` 안 탐 → `+0x141 GetLastError` → `+0x159 JZ` / `+0x15f JZ` → `+0x189` | `GetModuleHandleExW` 0 | **bcrypt 만의 갈래** (bcryptprimitives 는 실패해도 TRUE — run91 ③) |

`InitializeSystemPreferredCache` 가 음수를 주는 자리: `+0x2f JZ +0x1b1` (힙 `0x60` NULL, `0xc0000017`) · `+0xae JZ +0x165` (힙 `0x28` NULL, `0xc0000017`) · `+0xc7 JS +0x111` (임계영역 음수) · 예외 착지점 `+0x46 ~ +0x84` (`RtlInitializeResource` 가 예외를 던질 때, `0xc00000e5` — 결과서가 PE 바이트로 읽음, 착지 조건은 `[추측]`) · **`+0xd9 JNS` 안 탐 (`BcpRegister…` 음수)**.
`BcpRegister…` 가 음수를 주는 자리: `+0x7a JZ +0x1af` (이벤트 NULL) · `+0xe3` / `+0x13f JNZ +0x1e1` (이번 인자로 안 닿음) · **`+0x163 JS +0x1e1` (`IoCallKernelDriver` 음수)**.
`IoCallKernelDriver` 가 음수를 주는 자리: `+0x1d9` (열기 음수) · **`+0xd0 JS +0x15c` (IOCTL 음수 — 로그의 자리)** · `+0x129 ~ +0x133` (입력 길이 < 8, 이번엔 `0x68` 이라 안 닿음).

- 크래시 (FALSE 아님) 자리 (결과서): `RtlInitializeResource` 의 `RtlRaiseStatus` 갈래 (위 착지점이 잡음) · 힙 · 로더 일반 실패.

### IOCTL `0x390400` 입력 `0x68` 바이트 (결과서 2-b — attach 의 실제 인자로 계산)

| 오프셋 | 크기 | 값 | 출처 |
|---|---|---|---|
| `+0x00` | 4 | `0x1a2b3c4d` (고정) | `IoCallKernelDriver +0x55` |
| `+0x04` | 4 | **`0x10500`** (기능 번호) | `IoCallKernelDriver +0x5b` ← `BcpRegister… +0x155` |
| `+0x08` · `+0x18` · `+0x38` | 4 | 0 | `IoPack_Configuration_ParamBlock +0xd2` · `+0xe0` · `+0xe6` (인자 1 · 3 · 7) |
| `+0x0c` | 4 | 0 | 안 씀 (버퍼 `memset`, `BcpRegister… +0x2d ~ +0x40`) |
| `+0x10` · `+0x20` · `+0x28` · `+0x30` | 8 | `-1` (문자열 넷 없음) | `IoPack… +0x11a` · `+0x12a` · `+0x136` · `+0x142` |
| `+0x40` · `+0x48` | 8 | `-1` (`CRYPT_CONTEXT_CONFIG` · `…_FUNCTION_CONFIG` 없음) | `IoPack… +0x107`/`+0x154` · `+0xed`/`+0x168` |
| `+0x50` | 4 | 0 (덩어리 크기) | `IoPack… +0x10a` |
| `+0x58` | 8 | `-1` (덩어리 없음) | `IoPack… +0xf4` · `+0x172 IoPack_Buffer` |
| **`+0x60`** | 8 | **이벤트 핸들** (rva `0x25b10` 의 값) | `IoPack… +0xf8` · `+0xff` ← `BcpRegister… +0xa8` |

- 합 `0x68` = 머리만 (`IoPack… +0x4a MOV EDI,0x68` — 문자열 · 덩어리가 없어 안 늘어남). `-1` = "이 칸이 가리키는 데이터 없음".
- **세마포어 둘 · PID · 스레드 ID · 함수 주소는 안 들어간다** `[정적]`. 엔진은 입력을 읽을 필요가 없다 — 만들 것은 **답** (status) 이다.

### 최소 조건 표 (결과서 질문 5, 일반 프로세스 — `g_TrustedEnvironment = 1`)

| # | syscall (또는 부품) | 입력 | 기대 답 | 안 맞으면 |
|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | `(-1, 0x56, buf, 0xb0, NULL)` | 아무 status. 성공이면 첫 DWORD 가 1 · `0x10` · `0x11` 이 **아닌** 값. 진짜는 `0xc0000003` (run91) | `1` → 환경 8, `0x10`/`0x11` → `0x10`: `InitializeCNG +0x17` 이 초기화를 건너뛰고 TRUE (KsecDD 안 건드림 — 쓰지 말 것) |
| 2 | `NtQueryInformationProcess` (1 이 실패일 때만) | `(-1, 0, buf, 0x40, NULL)` | 성공 + `buf+0x38` bit 7 = 0, 또는 실패 | bit 7 = 1 → 환경 4 → `BcpRegister… +0x90 JZ` 로 IOCTL 건너뜀 (쓰지 말 것) |
| 3 | `NtTraceControl` × 여럿 | ETW 등록 | 아무 status | 영향 없음 |
| 4 | `NtQueryInformationProcess` | `(-1, 0x2b, buf, 0x218, &len)` | 아무 답 | 영향 없음 (결과 버림) |
| 5 | 힙 둘 + `NtCreateSemaphore` × 2 | 힙 `0x60` · `0x28`, 세마포어 `0x100003` · 처음 0 · 한도 `0x7fffffff` | 힙 NULL 아님, 세마포어 성공 + 핸들 채움 | 힙 NULL → `0xc0000017` → FALSE ①. 세마포어 실패 → 예외 → `0xc00000e5` → FALSE ① `[추측]` |
| 6 | **`NtCreateEvent`** | `(&h, 0x1f0003, NULL, 1, 0)` | **status ≥ 0 + `*h` ≠ 0** | FALSE ① |
| 7 | **`NtOpenFile`** | `\Device\KsecDD`, `0x100003`, 공유 7, 옵션 `0x20`, 속성 0, 루트 0, **칸 = bcrypt `+0x25ae0`** | **status ≥ 0 + 그 칸에 0 아닌 핸들** | status < 0 → FALSE ①. status ≥ 0 인데 칸 0 → 8 이 `0xc0000008` → FALSE ① (**지금 엔진**) |
| 8 | **`NtDeviceIoControlFile`** | 7 의 핸들, Event · APC 0, `0x390400`, 입력 `0x68` (위 표), 출력 8B | **status ≥ 0** (0 권장). 출력 · IOSB 안 봄 | 음수 → FALSE ①. `0x103` 은 성공으로 침 (안 기다림) |
| 9 | (ntdll · kernelbase 코드) `GetModuleHandleExW(5, &DllMain, …)` | `RtlPcToFileHeader` · `LdrAddRefDll(1, 이미지 시작)` | 성공 | FALSE ② |
| 10 | `NtClose` | — | TRUE 길엔 호출 없음 | — |

### run91 (bcryptprimitives) 과 나란히 (결과서 5번 끝 표)

| | bcryptprimitives (run91) | bcrypt (run92) |
|---|---|---|
| 장치 이름 | `\Device\CNG` | `\Device\KsecDD` |
| `NtOpenFile` 접근 / 속성 | `0x100001` / `0x40` | `0x100003` / 0 |
| 핸들을 받는 곳 | 지역 칸 `[RBP+0x10]` → 캐시 `g_hCNGDriver` | **전역 `g_hIoDevice` 에 바로** |
| 열고 나서 핸들 0 검사 | **한다** (호출자 `GetSeedFromKernelState +0x29`) | **안 한다** (IOCTL 로 가서 커널이 `0xc0000008`) |
| 재시도 | 5번 | 없음 |
| IOCTL 코드 / 입력 / 출력 | `0x390008` / 없음 / `0x30` | `0x390400` / **`0x68` (이벤트 핸들 1)** / 8 |
| IOCTL 출력 | 시드로 씀 (내용 검사 없음) | **버림** |
| IOCTL status | ≥ 0 이면 성공 (`0x103` 이면 kernelbase 가 기다림) | ≥ 0 이면 성공 (`0x103` 도 안 기다림) |
| `IsEnclave` · `GetTrustedEnvironment` | 같은 꼴 | 같은 꼴 (같은 오프셋) |
| `GetModuleHandleExW(5, …)` 실패 | TRUE | **FALSE** |
| 로그의 막힘 모양 | `NtOpenFile` × 5 | `NtOpenFile` × 1 + `NtDeviceIoControlFile`(핸들 0) |
| 엔진이 고칠 것 | `NtOpenFile` 이 핸들 칸에 쓰기 | **같음** (+ IOCTL `0x390400` 에 status 0) |

### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 `NtOpenFile` 몸이 `\Device\KsecDD` 도 알아보고 **핸들 칸에 쓰는지** (run91 의 `\Device\CNG` 와 같은 고침), `NtDeviceIoControlFile` 몸이 코드 `0x390400` 에 status 0 을 주는지.
- 고친 뒤 막힘이 `GetModuleHandleExW(5, &DllMain)` 로 옮겨 가는지 (FALSE ②). 엔진의 `RtlPcToFileHeader` · `LdrAddRefDll` 은 ntdll 코드라 로더 표 (모듈 목록 · 그래프 노드) 가 바르면 될 것 `[추측: ntdll 본문 안 읽음]`.
- 프로그램이 `BCryptOpenAlgorithmProvider` 를 부르면 `BCryptResolveProviders` 가 같은 핸들로 기능 `0x20000` IOCTL 을 하고 **출력을 해석한다** — "출력 0" 으로는 공급자를 못 찾을 수 있다 (다음 요청 후보).

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 값은 run44 덤프 읽기 로그를 다시 읽었다 (flow-writer).
- 정적:
  - **bcrypt pdb `5F1077ED8D3ABC83AD7D378F94EA41901`** — **처음 analyze** (`System32\bcrypt.dll` x64, 183376B, 이미지 베이스 `0x180000000`, `_index.tsv` 함수 311, 별칭 표 `ms/sym/bcrypt.dll.aliases.tsv`). 38개 뽑음 (정규식 두 묶음, depth 0), 지도 `ms/disassemble/bcrypt/_map/entry_*` (106 함수, 깊이 10), 참조 `_refs/` (rva 7 · 함수 3).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — `RtlInitializeResource` · `RtlDeleteResource` depth 0 새로 (다시 analyze 안 함).
  - kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` — `QueryFullProcessImageNameW` · `CreateEventW` · `CreateEventExW` · `GetModuleHandleExW` depth 0 새로.
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` — 있던 `NtQueryInformationProcess.asm` + PE 바이트 (점프 표 → class `0x2b` `+0x1424`, `0x1b` `+0xe21`) 만.
- 이름: PDB 이름 `DllMain` 은 Ghidra 가 **`entry`** 로 잡았다 (`pdb_aliases.py --where bcrypt.dll DllMain` → rva `0x11140`, `_index.tsv` 이름 `entry`). 이 문서는 `DllMain` 으로 쓴다. `IoCallKernelDriver` · `_IoOpenDevice` · `IoStartup` · `IoPack_*` 는 `_calls.tsv` 에 `alias:…+1` 로 찍혔지만 같은 주소 이름은 그 함수의 C++ 꾸민 이름 하나뿐이다 (예: `?IoCallKernelDriver@@YAJKPEAEK0PEAK@Z` — `--where` 로 확인). 핸들 칸 = `?g_hIoDevice@@3REAXEA` (rva `0x25ae0`, 함수 밖 전역).
- 버전 대조 (결과서): "알려진 사실" 표에 없는 모듈이라, **에뮬레이터 로그 값과 코드 값이 같은 것**으로 맞춤 — `NtOpenFile` 다섯 값 · 칸 주소 `+0x25ae0` · IOCTL 코드 · 입력 `0x68` · 출력 8 · NtQIP 두 호출 · 세마포어 · 이벤트 인자 · syscall 차례 1 ~ 8.

## 목적

- PE_emulator 요청 32 (샘플 221개 — 악성 57 / 정상 164 — 가 bcrypt `DllMain` FALSE → 로더 `0xc0000142` 에서 막힘, 같은 기본 대답을 받은 샘플 531개). 사용자 결정 2026-11-14: Q104 VM 단계 앞에서 끼워 **지금 먼저**, 정적만. 커널 `ksecdd.sys` 의 `0x390400` 처리는 범위 밖.
- `\Device\KsecDD` 를 여는 함수와 보는 값 · FALSE 자리, IOCTL `0x390400` 의 입력 칸 · 출력 사용 · TRUE 조건, TRUE 뒤 첫 사용 (이름만), NtQIP `0x2b` · 세마포어 · 이벤트, 최소 조건 표.

## 방법

- 정적: `_workspace/sast/88_bcrypt-ksecdd.md` — bcrypt analyze → 지도 (`entry` 뿌리) → 줄기 (깊이 5 ~ 6 의 `IoCallKernelDriver` · `_IoOpenDevice`) 를 정규식 depth 0 으로 뽑음 + 질문 3 용 이름만 8개.
- 결과서: `_workspace/sast/88_bcrypt-ksecdd_result.md` (**분석성공** — 못 닫은 것은 `NtClose` 나머지 2 의 정체 · `GetModuleHandleExW` 안쪽 ntdll syscall 유무, 둘 다 이번 막힘과 무관).
- flow-writer 가 다시 본 것:
  - bcrypt `.asm`: `entry` 전부, `InitializeCNG` 전부, `InitializeConfig` 전부, `IoStartup` 전부, `_IoOpenDevice` 전부, `GetTrustedEnvironment` 전부, `IoCallKernelDriver +0x0 ~ +0x13e` · `+0x15c ~ +0x168` · `+0x196 ~ +0x1d9`, `BcpRegisterConfigChangeNotifyNoLogging` 의 결과서 줄 (`+0x2a` · `+0x2d ~ +0x40` · `+0x68 ~ +0x90` · `+0xa8` · `+0xda ~ +0x163` · `+0x1af` · `+0x1e1 ~ +0x1f0`), `InitializeSystemPreferredCache` 의 결과서 줄 (`+0x19 ~ +0x3f` · `+0x98 ~ +0xd9` · `+0x145 ~ +0x165` · `+0x1b1`), `IoPack_Configuration_ParamBlock` 의 칸 쓰기 줄, `IsEnclave +0x40 ~ +0x52`, `InitializeProcessImageNameUM +0xb9 ~ +0xc7`, `_calls.tsv` 의 `entry` 줄, 지도 `entry_edges.tsv` 의 `TlgRegisterAggregateProviderEx` · `McGenEventRegister_EtwEventRegister` 줄.
  - ntdll `LdrDisableThreadCalloutsForDll` 전부 · `_calls.tsv` 줄, `RtlInitializeResource +0xb3 ~ +0xe7`, `RtlDeleteResource +0x12 ~ +0x20`; kernelbase `QueryFullProcessImageNameW +0x74` · `+0xae ~ +0xb8`, `CreateEventW +0x76`, `GetModuleHandleExW` `_calls.tsv` 줄 (`+0x88 RtlPcToFileHeader` · `+0x110 LdrGetDllHandle` · `+0x130 LdrAddRefDll`); ntoskrnl `NtQueryInformationProcess +0xe21` · `+0xe84` · `+0x1424` · `+0x148a` · `+0x14b9`.
  - 이름: `pdb_aliases.py --where bcrypt.dll` (`DllMain` · `g_hIoDevice` · `IoCallKernelDriver`), `--lookup bcrypt.dll 0x11140` (별칭 무리 아님).
  - 사진 (run44 덤프 읽기 로그): 모듈 목록 · `\KnownDlls` 목록 (아래 "사진 대조"). sample 소스 `grep`.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/bcrypt/_calls.tsv` · `.asm`, ntdll · kernelbase · ntoskrnl `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `bcrypt!DllMain` (`entry`) | `+0x2c` | `+0x33` | `LdrDisableThreadCalloutsForDll` | `RCX` = 자기 hModule (`+0x11` · `+0x20`) | [정적] |
| 〃 | `+0x41` · `+0x46` | `+0x46` · `+0x4b` | `_DllMainCRTStartupForGS2` · `SafeAllocaInitialize` | 반환 안 봄 | [정적] |
| 〃 | `+0x80` | `+0x85` | `WppInitUm` | → `+0x5f/+0x66 EtwRegisterTraceGuidsW`. 반환 안 봄 | [정적] |
| 〃 | `+0x85` | `+0x8a` | `McGenEventRegister_EtwEventRegister` | → `+0x27 EtwEventRegister` (지도). 반환 안 봄 | [정적] |
| 〃 | **`+0x8a`** | **`+0x8f`** | **`InitializeCNG`** | `+0x93 JZ +0xca` (0 아니면 FALSE ①) | [정적] |
| 〃 | `+0xed` | `+0xf4` | `GetModuleFileNameW` | WPP 플래그 `0x20` 일 때만 (보탬 ④) | [정적] |
| 〃 | **`+0x12d`** | **`+0x134`** | **`GetModuleHandleExW`** | `(5, &DllMain, &h)`, `+0x13b JNZ +0x1c5` (FALSE ②) | [정적] |
| 〃 | `+0x141` | `+0x148` | `GetLastError` | FALSE ② 길 (WPP 기록용) | [정적] |
| `InitializeCNG` | `+0x6` | `+0xb` | `SymCryptInitEnvWindowsUsermodeWin8_1nLater` | 반환 안 봄 | [정적] |
| 〃 | `+0x66` | `+0x6b` | `GetTrustedEnvironment` | `+0x13 JZ` (환경 0) 일 때 | [정적] |
| 〃 | `+0x2e` · `+0x40` | `+0x33` · `+0x45` | `InitializeLoader` · `InitializeConfig` | 늘 0 | [정적] |
| 〃 | **`+0x52`** | **`+0x57`** | **`InitializeSystemPreferredCache`** | `+0x5b JNZ +0x6d` | [정적] |
| 〃 | `+0x86` | `+0x8b` | `UninitializeCNG` | 실패 길 | [정적] |
| `GetTrustedEnvironment` | `+0x4` · `+0xd` | `+0x9` · `+0x12` | `IsEnclave` · `IsTrustlet` | run91 과 같음, `+0x1b` 저장 | [정적] |
| `IsEnclave` | `+0x44` | `+0x4b` | `NtQueryInformationProcess` | `(-1, 0x56, …, 0xb0)` (`+0x40 LEA EDX,[R9-0x5a]`) | [정적] |
| `InitializeConfig` | `+0x31` | `+0x36` | `InitializeProcessImageNameUM` | 환경 `& 1` (`+0x22` · `+0x2f`) | [정적] |
| 〃 | `+0x36` | `+0x3d` | `RtlDllShutdownInProgress` | 참이면 `+0x44 JNZ +0x5e` | [정적] |
| 〃 | `+0x46` | `+0x4b` | `TlgRegisterAggregateProviderEx` | → `TraceLoggingRegisterEx_EventRegister_EventSetInformation` (`+0x2a` · `+0x6d` · `+0xa8`, 지도). 반환 안 봄 | [정적] |
| 〃 | `+0x59` | `+0x5e` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` | **환경 `& 1` == 0 일 때만** (`+0x22 JZ +0x4d`) — 일반은 안 감 | [정적] |
| 〃 | `+0x62` (꼬리 `JMP`) | — | `IoStartup` | `+0x10 g_hIoDevice = 0` · `+0x1e InitializeCriticalSection` · `+0x2a` 표시 1 | [정적] |
| `InitializeProcessImageNameUM` | `+0xb9` | `+0xc0` | `QueryFullProcessImageNameW` | 실패도 `+0xc7` 로그만 | [정적] |
| `kernelbase!QueryFullProcessImageNameW` | `+0xb1` | `+0xb8` | `NtQueryInformationProcess` | class `+0xae` = `0x2b`, 길이 `+0x74` = `0x218` | [정적] |
| `nt!NtQueryInformationProcess` | (class `0x2b` 처리기 `+0x1424`) `+0x148a` · `+0x14b9` | `+0x148f` · `+0x14be` | `PsReferenceProcessFilePointer` · `IoQueryFileDosDeviceName` | DOS 경로 이름 (class `0x1b` 은 `+0xe21` → `+0xe84 PsQueryFullProcessImageName`) | [정적] |
| `InitializeSystemPreferredCache` | `+0x19` · `+0x98` | `+0x20` · `+0x9f` | `RtlAllocateHeap` | `0x60` / `0x28`, NULL → `0xc0000017` | [정적] |
| 〃 | `+0x38` | `+0x3f` | `RtlInitializeResource` | → ntdll `+0xb3/+0xb8` · `+0xe2/+0xe7 NtCreateSemaphore` (`+0xc4 JS`) | [정적] |
| 〃 | `+0xb7` | `+0xbe` | `RtlInitializeCriticalSection` | `+0xc7 JS +0x111` | [정적] |
| 〃 | **`+0xd0`** | **`+0xd5`** | **`BcpRegisterConfigChangeNotifyNoLogging`** | `RCX` = rva `0x25b10` (`+0xc9`), `+0xd9 JNS +0x157` | [정적] |
| 〃 | `+0x149` | `+0x14e` | `CleanupSystemPreferredCache` | 음수일 때 (→ `+0x8e RtlDeleteResource` → ntdll `+0x12` · `+0x1b NtClose`) | [정적] |
| `BcpRegisterConfigChangeNotifyNoLogging` | `+0x40` | `+0x45` | `memset` | 버퍼 `[RSP+0x80]` 0 | [정적] |
| 〃 | `+0x68` | `+0x6f` | `CreateEventW` | → kernelbase `+0x76/+0x7d NtCreateEvent` | [정적] |
| 〃 | `+0xda` · `+0x136` | `+0xdf` · `+0x13b` | `IoPack_Configuration_ParamBlock` | 크기 → 채움 | [정적] |
| 〃 | **`+0x15a`** | **`+0x15f`** | **`IoCallKernelDriver`** | `ECX = 0x10500` (`+0x155`), `+0x163 JS +0x1e1` | [정적] |
| 〃 | `+0x1e9` | `+0x1f0` | `CloseHandle` | 실패 길 (이벤트) | [정적] |
| `IoCallKernelDriver` | **`+0x1b5`** | **`+0x1ba`** | **`_IoOpenDevice`** | `+0x88 JZ` (칸 0) · `+0x1a9` 다시 0 일 때 | [정적] |
| 〃 | **`+0xc2`** | **`+0xc9`** | **`NtDeviceIoControlFile`** | `+0x1d3 JNS +0x8e` 뒤, 핸들 = `[g_hIoDevice]` (`+0x8e`) | [정적] |
| `_IoOpenDevice` | `+0x2a` | `+0x31` | `RtlInitUnicodeString` | `L"\Device\KsecDD"` (`+0x10 LEA RDX,[0x18001f098]`) | [정적] |
| 〃 | **`+0x7a`** | **`+0x81`** | **`NtOpenFile`** | `(&g_hIoDevice` `+0x66`, `0x100003` `+0x5a`, OA (`+0x5f` 크기 `0x30`), IOSB, 7 `+0x6d`, `0x20` `+0x4a)` | [정적] |
| `kernelbase!GetModuleHandleExW` | `+0x88` · `+0x130` | `+0x8f` · `+0x137` | `RtlPcToFileHeader` · `LdrAddRefDll` | 주소 → 이미지 시작 · 고정 (`_calls.tsv`) | [정적] |
| `ntdll!LdrDisableThreadCalloutsForDll` | `+0x25` · `+0x3f` | `+0x2a` · `+0x44` | `LdrpFindLoadedDllByHandle` · `LdrpDereferenceModule` | `+0x3b OR byte [항목+0x6a],4` (`TlsIndex` 0 일 때) | [정적] |

### 호출 관계

```
bcrypt!DllMain (Ghidra entry, rva 0x11140, PE 진입점)   이유 = EDX                                       [정적]
 │  +0x23 / +0x26  CMP EDX,1 · JNZ +0x18d     ← 이유 1 (attach) 만 아래로
 ├─ (1) +0x2c  ntdll!LdrDisableThreadCalloutsForDll(자기 hModule)      → +0x3b 항목 Flags 비트 18
 ├─ (2) +0x41  _DllMainCRTStartupForGS2 · +0x46 SafeAllocaInitialize
 ├─ (3) +0x80  WppInitUm → +0x5f EtwRegisterTraceGuidsW                         반환 안 봄
 ├─ (4) +0x85  McGenEventRegister_EtwEventRegister → +0x27 EtwEventRegister     반환 안 봄
 ├─ (5) +0x8a  InitializeCNG (rva 0xb1c0)
 │        ├─ (1) +0x6   SymCryptInitEnvWindowsUsermodeWin8_1nLater
 │        ├─ (2) +0x66  GetTrustedEnvironment                                   ← +0x13 환경값 0 (처음)
 │        │        ├─ (1) +0x4  IsEnclave → +0x44 NtQueryInformationProcess(-1, 0x56, buf, 0xb0)
 │        │        └─ (2) +0xd  IsTrustlet → +0x34 NtQueryInformationProcess(-1, 0, buf, 0x40)   (1) 이 0 일 때만
 │        │  +0x15 / +0x17  환경 & 0x18 → +0x19 (건너뛰고 0)   일반은 +0x2e
 │        ├─ (3) +0x2e  InitializeLoader                                         늘 0
 │        ├─ (4) +0x40  InitializeConfig                                         늘 0
 │        │        ├─ (1) +0x31  InitializeProcessImageNameUM → +0xb9 QueryFullProcessImageNameW
 │        │        │                 └─ kernelbase +0xb1  NtQueryInformationProcess(-1, 0x2b, buf, 0x218)   결과 버림
 │        │        ├─ (2) +0x36  RtlDllShutdownInProgress
 │        │        ├─ (3) +0x46  TlgRegisterAggregateProviderEx → TraceLoggingRegisterEx_…            반환 안 봄
 │        │        └─ (4) +0x62  [꼬리 JMP] IoStartup → +0x10 g_hIoDevice = 0 · +0x1e InitializeCriticalSection
 │        ├─ (5) +0x52  InitializeSystemPreferredCache (rva 0x59e0)
 │        │        ├─ (1) +0x19  RtlAllocateHeap(0x60) → g_pCachedRngRWLock      NULL → 0xc0000017
 │        │        ├─ (2) +0x38  RtlInitializeResource → ntdll +0xb3 · +0xe2 NtCreateSemaphore ×2
 │        │        ├─ (3) +0x98  RtlAllocateHeap(0x28) → rva 0x25b18 · +0xb7 RtlInitializeCriticalSection
 │        │        └─ (4) +0xd0  BcpRegisterConfigChangeNotifyNoLogging(&rva 0x25b10) (rva 0x5bf4)
 │        │                 ├─ (1) +0x68  CreateEventW → kernelbase +0x76 NtCreateEvent(0x1f0003, 자동, 꺼짐)   → [rva 0x25b10]
 │        │                 ├─ (2) +0xda · +0x136  IoPack_Configuration_ParamBlock     크기 0x68 → 버퍼 채움 (+0x60 = 이벤트)
 │        │                 └─ (3) +0x15a IoCallKernelDriver(0x10500, 입력, 0x68, NULL, NULL) (rva 0x7f50)
 │        │                          │  +0x55 · +0x5b  입력 +0 = 0x1a2b3c4d · +4 = 0x10500
 │        │                          │  +0x7e ~ +0x88  g_hIoDevice == 0 → +0x196 (임계영역 안에서 다시 확인)
 │        │                          ├─ (1) +0x1b5  _IoOpenDevice (rva 0x8154)
 │        │                          │        ├─ (1) +0x2a  RtlInitUnicodeString(L"\Device\KsecDD")
 │        │                          │        └─ (2) +0x7a  NtOpenFile(&g_hIoDevice, 0x100003, OA{속성 0}, IOSB, 7, 0x20)
 │        │                          │  +0x1d0 TEST R12D · +0x1d3 JNS +0x8e    ← status 만 봄 (칸 검사 없음)
 │        │                          └─ (2) +0xc2   NtDeviceIoControlFile([g_hIoDevice], 0x390400, 입력 0x68, 출력 8)
 │        │                                   +0xd0 JS +0x15c → 음수 반환 / +0xd9 JZ +0xff → 0 (출력 · IOSB 안 봄)
 │        │                  +0x163 JS +0x1e1 → +0x1e9 CloseHandle(이벤트) → 음수 반환
 │        │           +0xd9 JNS +0x157 (성공) / 음수 → +0x149 CleanupSystemPreferredCache → 그 status
 │        └─ (6) +0x86  UninitializeCNG                    +0x5b JNZ +0x6d 일 때 (실패 길)
 │  +0x91 TEST · +0x93 JZ +0xca  → 아니면 +0x189 FALSE ①
 ├─ (6) +0xed   GetModuleFileNameW                     WPP 플래그 0x20 일 때만
 ├─ (7) +0x12d  GetModuleHandleExW(5, &DllMain, &h) → kernelbase +0x88 RtlPcToFileHeader · +0x130 LdrAddRefDll
 │  +0x13b JNZ +0x1c5  → 아니면 +0x141 GetLastError → +0x189 XOR EAX,EAX  FALSE ②
 └─ +0x1c5  MOV EAX,1 → +0x1d9 RET   TRUE
```

### 사진 대조 (run44 덤프 읽기 로그, 새 실행 없음 — flow-writer)

| 칸 | 값 | 줄 | 뜻 |
|---|---|---|---|
| 초기화 끝 (P2) 모듈 목록 `BaseDllName` | `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` — **6개, bcrypt 없음** | `ms/logs/04_baggage-read_P2.txt:3398 · 3503 · 3608 · 3713 · 3818 · 3923` | test.exe 는 초기화 동안 이 DLL 을 안 올린다 `[관찰]` (run91 과 같은 줄) |
| `\KnownDlls` 목록 | 섹션 객체 줄 (`_P2.txt:1377 ~ 1419`, P1a · P1b 같은 줄) 에 `bcryptPrimitives.dll` 은 있고 **`bcrypt.dll` 은 없다** | `_P2.txt:1394` (bcryptPrimitives) | 다른 프로세스가 bcrypt 를 올리면 KnownDll 섹션이 아니라 파일 길 (2.6 "파일로 올리는 길") 로 올라올 것 `[추측: 실행 안 봄]` |
| run44 사진 전부 | `bcrypt.dll` 글자 0건 (`grep -ci 'bcrypt\.dll' ms/logs/04_baggage-read_*.txt`) | — | 이 DLL 칸 (`g_hIoDevice` 등) 값은 사진에 없다 |

- sample 소스 · 빌드 스크립트에도 `crypt` · `bcrypt` · `ksecdd` 글자가 없다 (`grep -ril sample/` 0건) `[소스]`. 진입점 뒤에 올리는지는 사진 범위 밖이다.

## 확인된 것

- `[정적]` attach 는 `DllMain +0x23 / +0x26` 에서 이유 1 만 탄다. FALSE 는 `+0x189` 한 점, 오는 길은 둘 — `InitializeCNG` 0 아님 (`+0x93`) 과 `GetModuleHandleExW` 실패 (`+0x13b`).
- `[정적]` `InitializeCNG` 에서 0 아닌 값이 나오는 곳은 `InitializeSystemPreferredCache` (`+0x5b JNZ`) 하나다. `InitializeLoader` · `InitializeConfig` 는 늘 0 (`IoStartup` 꼬리도 0).
- `[정적]` `\Device\KsecDD` (`.rdata` rva `0x1f098`) 를 여는 곳은 `_IoOpenDevice +0x7a` 하나, 핸들은 전역 `g_hIoDevice` (rva `0x25ae0`) 에 곧바로 쓰인다 (`+0x66`). 접근 `0x100003` · 공유 7 · 옵션 `0x20` · 속성 0 · 루트 0 — 에뮬레이터 로그와 같다.
- `[정적]` `IoCallKernelDriver` 는 열기 status 만 보고 (`+0x1d3 JNS`) 핸들 칸을 검사 없이 IOCTL 에 넘긴다 (`+0x8e`). IOCTL 은 status 부호만 보고 (`+0xd0 JS`), 이번 호출은 출력 길이 포인터가 NULL 이라 출력 · IOSB 를 안 읽는다 (`+0xd9 JZ +0xff`).
- `[정적]` IOCTL 입력 `0x68` 은 머리만이고 핸들은 이벤트 하나 (`+0x60`), 기능 번호 `0x10500` (`+0x4`).
- `[정적]` 세마포어 둘은 `RtlInitializeResource` 가 `g_pCachedRngRWLock` 안에 만든다 (ntdll `+0xb3` · `+0xe2`) — IOCTL 과 무관. 이벤트는 `CreateEventW` (자동 · 꺼짐) → rva `0x25b10`.
- `[정적]` NtQIP `0x56` 은 run91 과 같은 `IsEnclave` (같은 오프셋). NtQIP `0x2b` (`0x218`) 는 결과를 버린다. 커널의 `0x2b` 처리기는 `PsReferenceProcessFilePointer` → `IoQueryFileDosDeviceName` 을 부른다 (DOS 경로).
- `[정적]` 실패 길의 `NtClose` 는 셋까지 설명된다 (이벤트 1 · 세마포어 2). TRUE 길엔 닫는 호출이 없다.
- `[관찰]` run44: test.exe 는 초기화 동안 bcrypt 를 안 올린다 (모듈 6개). `bcrypt.dll` 은 `\KnownDlls` 목록에도 없다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **`InitializeConfig +0x59` (`TraceLoggingRegisterEx_EventRegister_EventSetInformation`) 는 일반 프로세스 길에 없다** — 결과서 1-a 12 는 `+0x36` · `+0x46` · `+0x59` 셋을 차례로 적었지만, `+0x22 JZ +0x4d` 는 환경 `& 1` == 0 일 때의 갈래이고 일반 (환경 1) 은 `+0x31` → `+0x36 RtlDllShutdownInProgress` → (`+0x44 JNZ` 안 타면) `+0x46 TlgRegisterAggregateProviderEx` → `+0x4b JMP +0x5e` 로 `+0x59` 를 건너뛴다. 대신 `TlgRegisterAggregateProviderEx` 가 안에서 같은 함수를 부른다 (지도 `entry_edges.tsv` depth 4: `+0x2a` · `+0x6d` · `+0xa8`). 둘 다 반환을 안 봐서 **결론 같음**. 로그 4번 `NtTraceControl` × 4 의 구성은 그래서 `[추측]` (`TlgRegisterAggregateProviderEx` 안 몇 번인지 안 읽음).
- ② (보탬) **출력 칸을 고르는 길** — `IoCallKernelDriver +0x5e TEST R14 · +0x61 JZ +0x121` → `+0x121 R15D = 0` · `+0x124 JMP +0x6a` → `+0x6a TEST RDI · +0x6d JNZ` 안 탐 → `+0x73 LEA RSI,[RSP+0x60]` · `+0x78 R15D = 8`. 출력 길이 포인터와 출력 버퍼가 **둘 다 NULL** 일 때 제 지역 8B 를 쓴다. 결과서 2-a 와 결론 같음.
- ③ (보탬) **`DllMain +0x2c LdrDisableThreadCalloutsForDll`** — `RCX` 는 진입 때 그대로 (`+0x11` 에서 `g_hInstance` 에 저장, `+0x20 MOV RDI,RCX`) 라 **bcrypt 자기 항목**. ntdll 본문 (`+0x0 ~ +0x50`) 은 `LdrpFindLoadedDllByHandle` · `LdrpDereferenceModule` 만 부르고 syscall 줄이 없다 — 결과서의 "`[공백]`" 을 닫음. 하는 일은 `+0x3b OR byte [항목+0x6a],4` (`Flags` 비트 18 `DontCallForThreads`, 항목 `TlsIndex` 가 0 일 때만 — 짐 지도 3.5 run68 줄과 같은 함수).
- ④ (보탬) **`DllMain +0xca ~ +0x11c` WPP 갈래** — 성공 길에서 `[WPP_MAIN_CB+0x1c] & 0x20` 이면 `+0xed GetModuleFileNameW(NULL, g_fLoadCNGDone+8, 0x100)` · `+0x117 WPP_SF_qS` 를 하고 `+0x11c` 로 돌아온다. 결과서 차례 표엔 없던 줄 (run91 ④ 와 같은 꼴). ETW 세션이 켤 때만이라 TRUE / FALSE 에 안 닿는다.
- ⑤ (보탬) **`InitializeCNG` 실패 길의 정리** — `+0x6d ~ +0x84` WPP 검사 → `+0x86 UninitializeCNG` → `+0x8b JMP +0x25` (그 status 반환). 엔클레이브 건너뛰기 `+0x19` 와 성공 끝 `+0x64 JMP +0x19` 은 같은 자리 (`g_fLoadCNGDone = 1`).
- 이전 run 이 틀린 것은 없다 (bcrypt 는 처음) — 정정 없음.

## 아직 확인 필요

**정적 — 대기열 Q173** (결과서 "다음 요청 후보" 1 · 2 · 3 · 4 + 위 ① 의 구성):

- ① ntdll `LdrAddRefDll` (고정) 과 `RtlPcToFileHeader` — `DllMain` 안에서 `GetModuleHandleExW(5, &DllMain)` 가 syscall 없이 성공하는 조건. KsecDD 가 풀리면 **다음 벽 후보** (FALSE ②).
- ② attach 가 TRUE 된 **다음** 첫 막힘: `BCryptOpenAlgorithmProvider +0x259` → `BCryptResolveProvidersForBcoapCached` → `BCryptResolveProviders` 의 IOCTL (`0x390400`, 기능 `0x20000`) 입출력 모양 (`IoUnpack_ProviderRef` 가 읽는 칸). 출력 0 으로는 안 될 수 있다.
- ③ (후순위) `ksecdd.sys` 의 `0x390400` 처리 — attach 의 `0x10500` 은 "status 0" 으로 충분해서 필요 없을 가능성이 크다.
- ④ (후순위) 실패 길 `NtClose` × 5 의 나머지 2 (정리 길 `TlgUnregisterAggregateProvider` · `CloseThreadpoolTimer` 쪽 ntdll `[추측]`). TRUE 가 되면 필요 없음. run91 의 Q170 ① (FALSE 뒤 로더 정리) 과 같이 볼 수 있다.
- (새로) `NtTraceControl` × 4 구성 — `TlgRegisterAggregateProviderEx` 안 몇 번 (위 ①).

**동적 — 대기열 Q174** (결과서 "동적분석으로 확인해야 할 것", 이번 묶음은 VM 안 함). **test.exe 로는 못 본다** — 초기화 동안 안 올림 `[관찰]`, 진입점 뒤도 sample 에 그런 API 없음 `[소스]`. bcrypt 를 올리는 프로세스가 필요하고 **샘플 고르기는 사용자 결정** (멈춤점은 그 프로세스 조건). Q104 에 안 붙임. bcryptprimitives (Q171) 와 같은 프로세스에서 같이 볼 수 있으면 묶는 게 좋다 `[추측: 두 DLL 을 함께 올리는 프로세스가 흔할 것]`.

- 추천 멈춤점 (WinDbg 모듈 이름 `bcrypt` — 아래 "도구 메모" 의 `entry` 이름 주의):
  - `bcrypt!DllMain+0x8f` (= `InitializeCNG` 반환 직후): `eax` 0 이면 성공 길.
  - `bcrypt!_IoOpenDevice+0x81` (= `NtOpenFile` 반환 직후): `eax` · `dq bcrypt+0x25ae0 L1` (진짜 핸들 모양 — 4의 배수 작은 수 `[추측]`).
  - `bcrypt!IoCallKernelDriver+0xc9` (= IOCTL 반환 직후): `eax` · IOSB `dq @rsp+0x50 L2` · 출력 `dq @rsp+0x60 L1`. 입력은 `+0xc2` 에서 `dq poi(@rsp+0x30) L0xd` (정적 예상: 위 입력 칸 표).
  - `bcrypt!DllMain+0x134` (= `GetModuleHandleExW` 반환 직후): `eax`.
  - `bcrypt!IsEnclave+0x4b`: `eax` (정적으론 `0xc0000003`) · `dd @rsp+0x30 L1`.
- 그 밖: TRUE 로 끝나는 진짜 실행에서 attach 중 `NtClose` 가 0 번인지, `NtTraceControl` 실제 개수 · 인자.

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: **bcrypt pdb 새 줄**, ntoskrnl · kernelbase · ntdll pdb 줄에 run92 몫, 근거 로그 run92 줄 (모듈 6개 · `\KnownDlls` 목록에 bcrypt 없음).
  - 흐름도 앞 글: run92 문단 하나 — **그림은 안 고침** (test.exe 는 이 DLL 을 안 올림).
  - 3장: 새 절 **"bcrypt 시작 함수 (`DllMain`)"** (bcryptprimitives 절 뒤, 3장 끝).
  - 4장: run92 새 줄 셋 (동적 Q174 · 정적 Q173 · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run92 줄.
- 짐 지도 (짐 지도 바퀴 — test.exe 는 bcrypt 를 안 올려 **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**):
  - 3.5 모듈 항목 `DontCallForThreads` 줄에 (run92) 부르는 쪽 메모 하나 (bcrypt `DllMain +0x2c` — bcrypt 자기 항목, test.exe 안 탐).
  - 기준 run92 줄 · 판 목록 (마흔여덟째판), 8장 두 표.
  - 이 DLL 이 놓는 칸 (`g_hIoDevice` · `g_pCachedRngRWLock` 등 bcrypt `.data`, 열린 KsecDD · 이벤트 · 세마포어 핸들) 은 체크리스트 밖 · test.exe 안 탐이라 안 셈 (run91 과 같은 판단).

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 `BeingDebugged` 같은 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`. ETW · WPP 세션이 이 공급자를 켜 두면 `DllMain +0xca ~ +0x11c` · `+0x95 ~ +0xc5` · `InitializeCNG +0x6d ~ +0x84` 의 `WPP_SF_*` 줄이 탄다 — TRUE / FALSE 엔 안 닿는다.
- **Ghidra `entry` 는 WinDbg 심볼이 아니다** (run91 에서 제안한 함정과 같음): 결과서 추천 멈춤점은 `bcrypt!DllMain+…` 로 이미 PDB 이름을 썼다. WinDbg 에선 `bcrypt!DllMain+0x8f` 나 rva (`bcrypt+0x111cf`) 로 걸면 될 것 `[추측]`. `_IoOpenDevice` · `IoCallKernelDriver` 는 C++ 꾸민 이름 (`?IoCallKernelDriver@@YAJKPEAEK0PEAK@Z`) 이라 WinDbg 가 `bcrypt!IoCallKernelDriver` 로 받을지는 `x bcrypt!*IoCallKernelDriver*` 로 먼저 확인 `[추측]` → windbg-kd-runbook pitfalls 에 추가 제안.
- 도구 (결과서 "스크립트 문제"): 없음. analyze 로그의 `WindowsResourceReference` WARN 한 줄은 결과에 영향 없음 (sast/87 과 같음).
