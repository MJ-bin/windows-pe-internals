# 88 bcrypt-ksecdd — 정적 분석 결과

- 요청서: `_workspace/sast/88_bcrypt-ksecdd.md` (Q172, PE_emulator 요청 32)
- 스크립트 문제: **없음.** (analyze 로그에 `WindowsResourceReference` 클래스 못 찾음 WARN 한 줄 — 결과에 영향 없음, sast/87 과 같음)
- 모듈 / 바이너리: bcrypt (`ms/bin/.../System32/bcrypt.dll`, 183376 바이트, x64, 이미지 베이스 `0x180000000`)
  - **PDB GUID+Age = `5F1077ED8D3ABC83AD7D378F94EA41901`** (`ms/sym/bcrypt.dll.pdb`, 별칭 표 `bcrypt.dll.aliases.tsv`)
  - **이번에 처음 analyze 함** (16:13~16:13, 약 17초). `SetPdb:` 줄 있음, `_index.tsv` 함수 311개, `.gpr` 있음.
  - ntdll·kernelbase 는 다시 analyze 안 함 (함수 몇 개 export 만).
- 이름 메모: 진입점 `DllMain` (PDB 이름, `pdb_aliases.py --where` 로 확인) 은 Ghidra 에서 **`entry`** (rva `0x11140`, 파일 PE 머리의 `AddressOfEntryPoint` 와 같음). 파일은 `entry.asm`. 문서·그림에는 `DllMain` 으로 쓰고 Ghidra 이름을 한 번 밝힘.
- 뽑은 함수:
  - bcrypt 38개 (정규식 2묶음, depth 0) → `ms/decomp/bcrypt/`, `ms/disassemble/bcrypt/`
  - 지도: `run_ghidra.sh map bcrypt.dll entry` → `ms/disassemble/bcrypt/_map/entry_{edges,nodes}.tsv`, `entry_summary.txt` (106개, 깊이 10)
  - 누가 읽고 쓰나(`refs`): `ms/disassemble/bcrypt/_refs/` (rva `0x25ae0`·`0x25b10`·`0x25ac4`·`0x25b18`·`0x25bc8`·`0x25ad8`·`0x25ae8`, 함수 `0x7f50`·`0x5bf4`·`0x8154`)
  - kernelbase 4개 (`QueryFullProcessImageNameW`, `CreateEventW`, `CreateEventExW`, `GetModuleHandleExW`), ntdll 2개 (`RtlInitializeResource`, `RtlDeleteResource`) — 모두 depth 0
  - ntoskrnl 은 이미 있던 `NtQueryInformationProcess.asm` + PE 바이트(점프 표)만 봄.
- 범위를 넓힌 이유: 요청서 대상 "attach 갈래에서 `NtOpenFile`·`NtDeviceIoControlFile` 을 부르는 함수" 는 진입점 바로 밑이 아니라 깊이 5~6 (`IoCallKernelDriver`, `_IoOpenDevice`). 지도로 길을 찾아 그 줄기만 뽑음. 질문 3 은 이름만 보려고 `BCryptResolveProviders` 등 8개를 depth 0 으로 추가.
- 버전 확인: **일치** (이 모듈은 "알려진 사실" 표에 없음. 대신 에뮬레이터 로그의 값과 코드의 값을 하나씩 대조)
  - `NtOpenFile` 접근 `0x100003` · 공유 `7` · 옵션 `0x20` · 속성 `0` · 루트 `0` = `_IoOpenDevice +0x5a / +0x6d / +0x4a / +0x3f / +0x36` 값과 같음. 이름 `\Device\KsecDD` 는 PE 바이트 rva `0x1f098` 에서 직접 읽음.
  - 핸들 칸 `bcrypt + 0x25ae0` = PDB 전역 `g_hIoDevice` (`_IoOpenDevice +0x66 LEA RCX,[0x180025ae0]`). 로그와 같음.
  - `NtDeviceIoControlFile` 코드 `0x390400` · 입력 길이 `0x68` · 출력 길이 `8` = `IoCallKernelDriver +0xb5` / `BcpRegisterConfigChangeNotifyNoLogging` 이 넘기는 `[RSP+0x70]`=`0x68` / `IoCallKernelDriver +0x78` `R15D=8`. 같음.
  - `NtQueryInformationProcess(-1, 0x56, …, 0xb0)` = `IsEnclave +0x40~+0x44`, `(…, 0x2b, …, 0x218)` = kernelbase `QueryFullProcessImageNameW +0xae/+0xb1`. 같음.
  - `NtCreateSemaphore ×2` (접근 `0x100003`, 처음 0, 한도 `0x7fffffff`) = ntdll `RtlInitializeResource +0xb3`, `+0xe2`. `NtCreateEvent` (접근 `0x1f0003`, 넷째 인자 1) = kernelbase `CreateEventW +0x76`. 같음.
  - syscall 차례 (로그의 1~9번) = 코드의 호출 차례와 같음 (아래 1-a).

---

## 한눈에 (먼저 읽기)

- 막힘의 원인은 **sast/87 (`\Device\CNG`) 과 같은 뿌리**다. `NtOpenFile(\Device\KsecDD)` 가 "status 0 인데 핸들 칸(`g_hIoDevice`, rva `0x25ae0`)을 안 씀" → 그 칸(0)을 그대로 `NtDeviceIoControlFile` 에 넘김 → 진짜 커널이면 `0xc0000008` → 연쇄로 `DllMain` FALSE. 로그와 한 칸도 안 어긋남. `[정적]`
- **bcryptprimitives 와 다른 점 (중요):**
  1. **핸들 칸을 코드가 직접 검사하지 않는다.** 열고 나서 status ≥ 0 이면 전역 `g_hIoDevice` 를 다시 읽어 그대로 IOCTL 에 넘긴다. 0 이면 터지는 건 **커널**(`0xc0000008`) 이고 우리 에뮬레이터가 그걸 돌려주고 있어서 지금 로그가 나온다. (bcryptprimitives 는 `GetCngHandle` 이 값을 돌려주고 호출자가 0 인지 검사.)
  2. **재시도 없음.** 한 번 열고 한 번 IOCTL. 실패하면 바로 위로 올라가서 FALSE.
  3. **IOCTL 입력이 있다 (`0x68` 바이트).** 입력 안에 **이벤트 핸들 하나**가 들어가고 (+0x60), 세마포어·PID·함수 주소는 안 들어간다.
  4. **IOCTL 출력은 읽지 않는다** (버려짐). 성공 status 만 필요.
  5. **`GetModuleHandleExW(5, &DllMain, …)` 가 실패해도 TRUE 였던 bcryptprimitives 와 달리, bcrypt 는 이게 실패하면 FALSE.** (`DllMain +0x13b JNZ` 안 타면 `+0x189 XOR EAX,EAX`.) 에뮬레이터는 이 줄까지 아직 못 왔으니 안 막혔을 뿐 — 다음 벽 후보.
- **TRUE 가 되는 최소 조건 (일반 프로세스):**
  1. `NtOpenFile(\Device\KsecDD)` → status ≥ 0 **그리고 `*FileHandle` (= 전역 `g_hIoDevice`) 에 0 아닌 값을 써 줌.**
  2. `NtDeviceIoControlFile(그 핸들, 0x390400, 입력 0x68, 출력 8)` → status ≥ 0. 출력·IOSB 안 봄. 0 으로 둬도 됨.
  3. `NtCreateEvent`·`NtCreateSemaphore ×2`·힙 할당 성공, `GetModuleHandleExW` 성공.
  4. 나머지 (`NtQueryInformationProcess` 0x56·0x2b, `NtTraceControl`) 는 **무엇을 돌려줘도** TRUE 에 영향 없음 (0x56 성공일 때 `buf[0]` 가 1·0x10·0x11 이면 길이 달라짐 — 아래 5번 표).
- **IOCTL 이 STATUS_PENDING(0x103) 을 줘도 `NtWaitForSingleObject` 는 부르지 않는다.** 음수만 아니면 성공으로 친다. 그래도 `0` 을 주는 게 안전. `[정적]`

---

## 질문별 답 [정적]

### 1. `\Device\KsecDD` 를 여는 함수와 FALSE 자리

#### 1-a. attach 길 차례 표 (`DllMain` = Ghidra `entry`, 일반 프로세스)

`reason = EDX`. `+0x23 CMP EDX,1; +0x26 JNZ +0x18d` → attach(1) 만 아래로. (`+0x18d` 쪽: reason 2·3 은 `+0x18f JNZ +0x1c5` 로 바로 TRUE, reason 0 은 정리 후 `+0x1c5` TRUE.) 표기: "call/ret" = call 자리 / ret 자리. 오프셋은 `ms/disassemble/bcrypt/<함수>.asm` 줄.

| # | 함수 + 오프셋 (call/ret) | 부르는 것 | syscall / 갈림 |
|---|---|---|---|
| 1 | `DllMain +0x2c/+0x33` | `LdrDisableThreadCalloutsForDll` (ntdll) | 반환 안 봄. syscall 없음으로 보임 `[공백]` |
| 2 | `DllMain +0x41/+0x46`, `+0x46/+0x4b` | `_DllMainCRTStartupForGS2` (→ `__security_init_cookie`), `SafeAllocaInitialize` (→ `RtlImageNtHeader`) | 반환 안 봄 |
| 3 | `DllMain +0x80/+0x85`, `+0x85/+0x8a` | `WppInitUm` (→ `EtwRegisterTraceGuidsW`), `McGenEventRegister_EtwEventRegister` (→ `EtwEventRegister`) | **`NtTraceControl` 여럿** (로그 1번). **반환 안 봄** (바로 다음 줄이 `CALL InitializeCNG`) |
| 4 | `DllMain +0x8a/+0x8f` | **`InitializeCNG`** | 반환 `EAX` 를 `+0x93 JZ +0xca` 로 봄. **0 이면 성공 길, ≠ 0 이면 FALSE (1-d)** |
| 5 | `InitializeCNG +0x6/+0xb` | `SymCryptInitEnvWindowsUsermodeWin8_1nLater` (→ `SymCryptDetectCpuFeaturesByCpuid`, `GetEnabledXStateFeatures`) | cpuid + 공유 페이지. 반환 안 봄 |
| 6 | `InitializeCNG +0xb ~ +0x13`, `+0x66/+0x6b` | 전역 `g_TrustedEnvironment` (rva `0x25ac0`) 읽음. 0 이면 `GetTrustedEnvironment` | 처음엔 0 → 아래 7 |
| 7 | `GetTrustedEnvironment +0x4/+0x9`, `+0xd/+0x12` | `IsEnclave`, (0 이면) `IsTrustlet` | **`NtQueryInformationProcess(-1, 0x56, buf, 0xb0)`** (`IsEnclave +0x44/+0x4b`, 로그 2번) → 필요하면 `(-1, 0, buf, 0x40)` (`IsTrustlet +0x34/+0x3b`). 결과를 `g_TrustedEnvironment` 에 저장 (`+0x1b`) → 5번 표 |
| 8 | `InitializeCNG +0x15 TEST AL,0x18; +0x17 JZ +0x2e` | 환경값 `& 0x18` ≠ 0 (엔클레이브) 이면 `+0x19` 로: `g_fLoadCNGDone=1`, 반환 0 (**초기화 통째로 건너뛰고 TRUE**) | 일반(1) 은 `+0x2e` 로 감 |
| 9 | `InitializeCNG +0x2e/+0x33` | `InitializeLoader` → `_InitializeLoaderLock` → `InitializeCriticalSection` | **항상 0 반환** (`_InitializeLoaderLock +0x16` 뒤 `EBX=0`). `+0x37 JNZ` 안 닿음 |
| 10 | `InitializeCNG +0x40/+0x45` | **`InitializeConfig`** | 아래 11. **항상 0 반환** (끝이 `+0x62 JMP IoStartup` 꼬리호출, `IoStartup` 은 `EBX=0` 반환) → `+0x49 JNZ` 안 닿음 |
| 11 | `InitializeConfig +0x31/+0x36` → `InitializeProcessImageNameUM +0xb9/+0xc0` → kernelbase `QueryFullProcessImageNameW +0xb1/+0xb8` | **`NtQueryInformationProcess(-1, 0x2b, buf, 0x218)`** (로그 3번) | 환경값 `& 1` 일 때만 (`InitializeConfig +0x1f TEST CL,1; +0x22 JZ +0x4d`). **결과가 안 쓰임** (실패하면 로그만 남기고 반환, 호출한 쪽은 `EAX` 를 안 봄) |
| 12 | `InitializeConfig +0x36/+0x3d`, `+0x46/+0x4b`, `+0x59/+0x5e` | `RtlDllShutdownInProgress`, `TlgRegisterAggregateProviderEx`, `TraceLoggingRegisterEx_EventRegister_EventSetInformation` | **`NtTraceControl` ×4** (로그 4번). 반환 안 봄 |
| 13 | `InitializeConfig +0x62` (꼬리 `JMP`) → `IoStartup +0x10 / +0x17~+0x25 / +0x2a` | `g_hIoDevice = 0`, `InitializeCriticalSection(g_csIoInitialize)`, 시작 표시 (rva `0x25ad8`) = 1 | 반환 0. syscall 없음 |
| 14 | `InitializeCNG +0x52/+0x57` | **`InitializeSystemPreferredCache`** | **여기서만 실패가 나온다.** 아래 15~19. 음수면 `+0x5b JNZ +0x6d` → FALSE |
| 15 | `InitializeSystemPreferredCache +0x19/+0x20`, `+0x38/+0x3f` | `RtlAllocateHeap(PEB 힙, 0, 0x60)` → 전역 `g_pCachedRngRWLock` (rva `0x25bc8`, PDB 이름), `RtlInitializeResource` | **`NtCreateSemaphore` ×2** (ntdll `RtlInitializeResource +0xb3/+0xb8`, `+0xe2/+0xe7`; 로그 5번). NULL 이면 `+0x2f JZ +0x1b1` → `0xc0000017` |
| 16 | `InitializeSystemPreferredCache +0x98/+0x9f`, `+0xb7/+0xbe` | `RtlAllocateHeap(…, 0x28)` → rva `0x25b18`, `RtlInitializeCriticalSection` | syscall 없음. NULL 이면 `+0xae JZ +0x165` → `0xc0000017`. 반환 음수면 `+0xc7 JS +0x111` |
| 17 | `InitializeSystemPreferredCache +0xc9 ~ +0xd0/+0xd5` | **`BcpRegisterConfigChangeNotifyNoLogging(&rva 0x25b10)`** (이벤트 핸들을 받을 칸) | 반환 음수면 `+0xd9 JNS` 안 타고 실패. 0 이면 `+0x157` 에서 rva `0x25ac4` = 1 (등록됐다는 표시), 성공 |
| 18 | `BcpRegisterConfigChangeNotifyNoLogging +0x68/+0x6f` → kernelbase `CreateEventW +0x76/+0x7d` | **`NtCreateEvent(&h, 0x1f0003, NULL, 1, 0)`** (로그 6번) | `+0x77 TEST RAX,RAX; +0x7a JZ +0x1af` → NULL 이면 `GetLastError` → `WinErrorToNtStatus` → 실패. 성공이면 핸들을 `*(0x25b10)` 에 저장 (`+0x74`) |
| 19 | `BcpRegisterConfigChangeNotifyNoLogging +0x80 ~ +0x90`, `+0xda/+0xdf`, `+0x136/+0x13b` | 환경값 `& 1` 이면 `IoPack_Configuration_ParamBlock` 2번 (1번 = 크기만 구함 → `0x68`, 2번 = 로컬 버퍼 `[RSP+0x80]` 에 채움) | 순수 계산. syscall 없음. 인자가 거의 다 NULL 이라 실패 길 안 닿음 |
| 20 | `BcpRegisterConfigChangeNotifyNoLogging +0x15a/+0x15f` | **`IoCallKernelDriver(0x10500, 입력, 0x68, NULL, NULL)`** | 반환 음수면 `+0x163 JS +0x1e1` → 이벤트 `CloseHandle` (`+0x1e9/+0x1f0`) → 음수 그대로 반환 |
| 21 | `IoCallKernelDriver +0x55 / +0x5b` | 입력 맨 앞에 상수 `0x1a2b3c4d` 와 기능 번호 `0x10500` 씀 | — |
| 22 | `IoCallKernelDriver +0x7e ~ +0x88` | `g_hIoDevice` 를 읽어 0 인지 봄. **0 이면 `+0x88 JZ +0x196`** | 처음엔 0 |
| 23 | `IoCallKernelDriver +0x196 ~ +0x1cb` | `EnterCriticalSection(g_csIoInitialize)` → 다시 0 인지 확인 → **`_IoOpenDevice` (`+0x1b5/+0x1ba`)** → `LeaveCriticalSection` | 열린 status 를 `R12D` 에 보관 (`+0x1ba`) |
| 24 | `_IoOpenDevice +0x2a/+0x31`, `+0x7a/+0x81` | `RtlInitUnicodeString(L"\Device\KsecDD")`, **`NtOpenFile(&g_hIoDevice, 0x100003, &OA, &IOSB, 7, 0x20)`** (로그 7번) | 반환 status 를 그대로 돌려줌 |
| 25 | `IoCallKernelDriver +0x1d0 ~ +0x1d9` | `TEST R12D,R12D; JNS +0x8e` | **status ≥ 0 이면 핸들 칸 값을 검사하지 않고** `+0x8e` 로. 음수면 그 status 를 반환 (`+0x1d9`) |
| 26 | `IoCallKernelDriver +0x8e ~ +0xc2/+0xc9` | `g_hIoDevice` 를 다시 읽어 **`NtDeviceIoControlFile(g_hIoDevice, 0, 0, 0, IOSB, 0x390400, 입력, 0x68, 출력, 8)`** (로그 8번) | `+0xce TEST EAX; +0xd0 JS +0x15c` → 음수면 실패 (아래 2-d) |
| 27 | `IoCallKernelDriver +0xd6 ~ +0xd9`, `+0xff` | `TEST R14,R14; JZ +0xff` → 출력 길이 포인터가 NULL 이라 **출력·IOSB 안 보고** `XOR EAX,EAX` 로 0 반환 | **성공** |
| 28 | `InitializeSystemPreferredCache +0x157` | rva `0x25ac4` = 1, `EBX = 0` → 반환 0 | — |
| 29 | `InitializeCNG +0x5d ~ +0x64` | 켜진 단계 표시 (`g_hInstance+8` 에 `0x2`·`0x4`·`0x8`), `JMP +0x19` → `g_fLoadCNGDone = 1`, 반환 0 | — |
| 30 | `DllMain +0x93 JZ +0xca` → `+0x12d/+0x134` | **`GetModuleHandleExW(5, &DllMain, &h)`** (5 = 고정 + 주소로 찾기) | `+0x139 TEST EAX; +0x13b JNZ +0x1c5` → **성공이면 TRUE**. 실패면 `+0x141/+0x148 GetLastError` 후 FALSE (1-d) |
| 31 | `DllMain +0x1c5` | `MOV EAX,1` → `+0x1ca ~ +0x1d9` ret | **TRUE** |

(로그 9번의 `NtClose ×5` 는 TRUE 길에는 없다. 아래 4번.)

`GetModuleHandleExW` (kernelbase) 안: `RtlPcToFileHeader` (`+0x88/+0x8f`) 로 주소 → 이미지 시작을 찾고, 핀 플래그라서 `LdrAddRefDll(1, 시작)` (`+0x130/+0x137`). 둘 다 ntdll 안에서 로더 표를 읽는 함수. syscall 없을 것으로 보이지만 안 읽음 `[공백]`.

#### 1-b. 이름·값 (로그와 대조)

- 이름 **`\Device\KsecDD`** (UTF-16, `.rdata` rva `0x1f098`). 접근 `0x100003` (`SYNCHRONIZE | FILE_READ_DATA | FILE_WRITE_DATA`), 공유 `7`, 옵션 `0x20` (`FILE_SYNCHRONOUS_IO_NONALERT`), 속성 `0` (sast/87 의 CNG 는 `0x40`), 루트 0, `IO_STATUS_BLOCK` 지역. **로그와 같음.** `[정적]`
- 핸들 칸 = **`NtOpenFile` 의 첫 인자가 전역 `g_hIoDevice` 의 주소** (`_IoOpenDevice +0x66 LEA RCX,[0x180025ae0]`). 지역 변수에 받아서 옮기는 게 아니라 **전역에 곧바로 쓴다.** PDB 이름은 `?g_hIoDevice@@3REAXEA` (꾸밈된 이름 그대로, WinDbg 에선 `bcrypt!g_hIoDevice` 로 보일 것 `[추측]`).
- **지금 우리 (PE_emulator): 값은 다 맞음. 핸들 칸을 안 쓰는 게 틀림** (아래).

#### 1-c. 이 함수가 보는 것 — status / 핸들 칸 / IOCTL status

| 보는 것 | 보나? | 자리 | 의미 |
|---|---|---|---|
| `NtOpenFile` status | **본다** | `IoCallKernelDriver +0x1ba MOV R12D,EAX` → `+0x1d0 TEST R12D,R12D; +0x1d3 JNS +0x8e` | 음수면 그 status 를 위로 올림 (`+0x1d9`). 0 이상이면 아래로 |
| 핸들 칸 (`g_hIoDevice`, rva `0x25ae0`) | **열고 난 뒤엔 검사 안 함** | `+0x8e MOV RCX,[g_hIoDevice]` → 그대로 `NtDeviceIoControlFile` 첫 인자 | 0 이 들어가면 **커널이** `STATUS_INVALID_HANDLE (0xc0000008)` 를 돌려줌. (열기 **전** 에만 `+0x85 TEST RAX,RAX; +0x88 JZ` 로 "이미 열었나" 를 봄) |
| `NtDeviceIoControlFile` status | **본다** | `+0xce TEST EAX,EAX; +0xd0 JS +0x15c` | 음수면 실패. 예외 둘: `0xc0000023` → `0xc00000e5` (`+0x15c/+0x161 → +0x133`), `0x80000005` → 출력 4바이트 이상일 때만 따로 처리 (`+0x163 ~ +0x1f9`, 이 호출은 출력 길이 포인터가 NULL 이라 쓸 일 없음) |

- 로그의 "NtOpenFile 1번 + NtDeviceIoControlFile(핸들 0) → `0xc0000008`" 는 **코드로 정확히 설명된다**: `+0x1d3 JNS` (status 0 이라 탐) → `+0x8e` 에서 `g_hIoDevice` = 0 → IOCTL(핸들 0) → `0xc0000008` → `+0xd0 JS +0x15c` → `+0x168 JNZ +0x101` → 반환 `0xc0000008`. `[정적]`
- **sast/87 (bcryptprimitives) 과 같은 꼴인가 — 비교 한 줄:** 둘 다 "열기 status ≥ 0 인데 핸들 칸이 0" 이라 막히는 **같은 원인**. 다른 점 = bcryptprimitives 는 `GetCngHandle` 이 칸 값을 돌려주고 **호출자가 0 인지 검사해서 5번 다시 열고** FALSE, bcrypt 는 **검사 없이 IOCTL 까지 가서 커널 답(`0xc0000008`)으로 FALSE**, 재시도 없음. (그래서 로그에 `NtOpenFile` 이 5번이 아니라 1번.)
- **지금 우리: 틀린 곳은 하나 — `NtOpenFile` 이 status 0 만 주고 `*FileHandle` 을 안 씀.** 그 뒤 "핸들 0 → `0xc0000008`" 은 진짜 커널과 같은 답이라 맞다.

#### 1-d. FALSE 로 가는 자리 전부 (attach)

`DllMain` 의 FALSE 는 한 점 `+0x189 XOR EAX,EAX` (→ `+0x18b JMP +0x1ca` → ret) 로 모인다. 거기로 오는 길:

| # | 자리 | 조건 | 일반 프로세스에서 |
|---|---|---|---|
| 1 | `DllMain +0x93 JZ +0xca` 안 탐 → `+0x95 ~ +0xc5` (WPP 로그 갈래 `+0xa6 JZ`·`+0xb0 JZ` → `+0x189`, 켜져 있으면 `+0xc5 JMP +0x170` → `WPP_SF_sDsd` → `+0x189`) | `InitializeCNG` 가 0 이 아닌 NTSTATUS 반환 | **여기가 막힘.** 실제로 음수가 나오는 곳은 하나뿐: `InitializeCNG +0x5b JNZ +0x6d` (← `InitializeSystemPreferredCache` 음수). `+0x37 JNZ +0xb0` (`InitializeLoader`), `+0x49 JNZ +0x8d` (`InitializeConfig`) 는 위 함수들이 항상 0 이라 안 닿음. 닿으면 `+0x86/+0x8b UninitializeCNG` 로 정리 후 `+0x25` 에서 그 status 반환 |
| 2 | `DllMain +0x13b JNZ +0x1c5` 안 탐 → `+0x141/+0x148 GetLastError` → `+0x159 JZ`/`+0x15f JZ` → `+0x189` | `GetModuleHandleExW` 가 0 | **bcrypt 만의 FALSE 갈래.** (bcryptprimitives 는 실패해도 TRUE) |

`InitializeSystemPreferredCache` 가 음수를 주는 자리 (위 1번의 안쪽):

| 자리 | 조건 | status |
|---|---|---|
| `+0x2f JZ +0x1b1` | `RtlAllocateHeap(0x60)` NULL | `0xc0000017` |
| `+0xae JZ +0x165` | `RtlAllocateHeap(0x28)` NULL | `0xc0000017` |
| `+0xc7 JS +0x111` | `RtlInitializeCriticalSection` 음수 | 그 status |
| 예외 착지점 `+0x46 ~ +0x84` (점프로 안 닿는 틈, 바이트로 읽음) | `RtlInitializeResource` 가 예외를 던질 때 (세마포어 못 만들 때 `[추측]`) | `0xc00000e5` (`+0x7b DebugTraceError`, `+0x80 JMP +0x145`) |
| **`+0xd9 JNS +0x157` 안 탐** | **`BcpRegisterConfigChangeNotifyNoLogging` 음수** | 그 status |
| (이어서 `+0x145 ~ +0x14e`) | 음수면 `CleanupSystemPreferredCache` 호출 후 반환 | — |

`BcpRegisterConfigChangeNotifyNoLogging` 이 음수를 주는 자리:

| 자리 | 조건 | status |
|---|---|---|
| `+0x7a JZ +0x1af` | `CreateEventW` NULL | `WinErrorToNtStatus(GetLastError())` |
| `+0xe3 JNZ +0x1e1`, `+0x13f JNZ +0x1e1` | `IoPack_Configuration_ParamBlock` 비정상 | 이번 인자(거의 다 NULL) 로는 안 닿음 |
| **`+0x163 JS +0x1e1`** | **`IoCallKernelDriver` 음수** | **그 status (`0xc0000008` ← 로그의 자리)**. `+0x1e9/+0x1f0 CloseHandle(이벤트)` 후 `+0x16e` 에서 반환 |

`IoCallKernelDriver` 가 음수를 주는 자리:

| 자리 | 조건 | status |
|---|---|---|
| `+0x1d9` | `_IoOpenDevice` 음수 (`NtOpenFile` 실패) | 그 status |
| **`+0xd0 JS +0x15c`** | **`NtDeviceIoControlFile` 음수** | **`0xc0000008` (핸들 0)** ← 로그의 자리. `0xc0000023` 이면 `0xc00000e5` |
| `+0x129`~`+0x133` | 입력 길이 < 8 | `0xc00000e5` (이번엔 `0x68` 이라 안 닿음) |

- 위 말고 attach 에서 `XOR EAX,EAX` 로 끝나는 자리 없음. 나머지는 전부 `+0x1c5 MOV EAX,1`. `[정적]`
- 크래시(FALSE 아님) 자리: `RtlInitializeResource` 의 `RtlRaiseStatus` 갈래 (예외 처리기가 위 표처럼 잡는다), 힙·로더 쪽 일반 실패. 이번엔 안 따라감.
- **지금 우리: 맞게 재현됨.** 로그의 FALSE 는 위 1번 → `+0x5b JNZ` → `+0xd9` → `+0x163 JS` → `+0xd0 JS` 길 그대로.

---

### 2. IOCTL `0x390400`

#### 2-a. 호출 모양

- 부르는 자리: **`IoCallKernelDriver +0xc2/+0xc9`**. 인자 (x64 순서):

| 인자 | 값 | 줄 |
|---|---|---|
| FileHandle | `g_hIoDevice` (`[0x180025ae0]`) | `+0x8e MOV RCX,…` |
| Event / ApcRoutine / ApcContext | 0 / 0 / 0 | `+0xae XOR EDX,EDX`, `+0xa7 XOR R8D,R8D`, `+0x9f XOR R9D,R9D` |
| IoStatusBlock | 지역 `[RSP+0x50]` (16바이트, 0 으로 시작) | `+0x95 LEA RAX,[RSP+0x50]`, `+0xbd` |
| IoControlCode | **`0x390400`** (고정 상수) | `+0xb5 MOV [RSP+0x28],0x390400` |
| InputBuffer / Length | 위 `BcpRegister…` 의 로컬 버퍼 `[RSP+0x80]` / `0x68` | `+0xb0`, `+0xaa` |
| OutputBuffer / Length | 지역 `[RSP+0x60]` (8바이트) / `8` | `+0x73 LEA RSI,[RSP+0x60]`, `+0x78 R15D=8` |

- 기능 번호는 **입력 안**에 있다 (코드는 항상 `0x390400`). 이 호출의 기능 번호 = **`0x10500`** (`BcpRegisterConfigChangeNotifyNoLogging +0x155 MOV ECX,0x10500`, `IoCallKernelDriver +0x5b` 가 입력 +4 에 씀). 같은 `0x390400` 으로 `BCryptResolveProviders` 는 `0x20000` 을 보낸다 (`+0x590 MOV [RSI+4],0x20000`, `+0x276 MOV ECX,0x20000`). 즉 `0x390400` 은 **CNG 설정·공급자 질의용 하나의 만능 통로**고, attach 는 그중 "설정 바뀜 알림 등록" 만 쓴다. `[정적]`
- 출력 길이 포인터(`R14` = `IoCallKernelDriver` 의 5번째 인자) 와 출력 버퍼 포인터(`R9`) 를 **둘 다 NULL 로 부르기 때문에** (`BcpRegister… +0x14a XOR R9D,R9D`, `+0x150 MOV [RSP+0x20],R15` = 0) `IoCallKernelDriver` 가 자기 로컬 8바이트를 출력 칸으로 씀 (`+0x73`, `+0x78`).

#### 2-b. 입력 `0x68` 바이트 칸 표

값은 attach 의 실제 인자(전부 NULL/0, 이벤트 핸들만 있음) 로 계산. 채우는 곳: `IoPack_Configuration_ParamBlock` (rva `0x73cc`) 두 번째 호출 + `IoCallKernelDriver`. 인자 번호는 이 함수의 14개 인자 (`BcpRegister +0xba ~ +0xda` 에서 넘김) 순서.

| 오프셋 | 크기 | 값 (attach) | 출처 (줄) |
|---|---|---|---|
| `+0x00` | 4 | `0x1a2b3c4d` (고정) | `IoCallKernelDriver +0x55` |
| `+0x04` | 4 | **`0x10500`** (기능 번호) | `IoCallKernelDriver +0x5b` ← `BcpRegister… +0x155` |
| `+0x08` | 4 | 0 | `IoPack… +0xcb/+0xd2` ← 인자 1 (`RCX=0`) |
| `+0x0c` | 4 | 0 | 안 씀 (버퍼를 `memset 0x80` 으로 0 채움, `BcpRegister… +0x2d ~ +0x40`) |
| `+0x10` | 8 | **-1** (문자열 #1 없음) | `IoPack… +0x11a` ← 인자 2 NULL → `-1` |
| `+0x18` | 4 | 0 | `IoPack… +0xd9/+0xe0` ← 인자 3 |
| `+0x20` | 8 | -1 (문자열 #2 없음) | `IoPack… +0x12a` ← 인자 4 |
| `+0x28` | 8 | -1 (문자열 #3 없음) | `IoPack… +0x136` ← 인자 5 |
| `+0x30` | 8 | -1 (문자열 #4 없음) | `IoPack… +0x142` ← 인자 6 |
| `+0x38` | 4 | 0 | `IoPack… +0xe3/+0xe6` ← 인자 7 |
| `+0x40` | 8 | -1 (`CRYPT_CONTEXT_CONFIG` 없음) | `IoPack… +0x107` / `+0x154` ← 인자 8 NULL |
| `+0x48` | 8 | -1 (`CRYPT_CONTEXT_FUNCTION_CONFIG` 없음) | `IoPack… +0xed` / `+0x168` ← 인자 9 NULL |
| `+0x50` | 4 | 0 (덩어리 크기) | `IoPack… +0x10a` ← 인자 10 |
| `+0x58` | 8 | -1 (바이트 덩어리 없음) | `IoPack… +0xf4`, 마무리 `IoPack_Buffer +0xd OR [RCX],-1` (`IoPack… +0x172`) ← 인자 11 NULL |
| **`+0x60`** | 8 | **이벤트 핸들** (`CreateEventW` 결과, 전역 rva `0x25b10` 에 든 값) | `IoPack… +0xf8/+0xff` ← 인자 12 = `[RSI]` (`BcpRegister… +0xa8`) |

- 합계 `0x68` = 머리만 (`IoPack… +0x4a MOV EDI,0x68`, 문자열·덩어리가 없어 늘지 않음). 로그의 입력 길이 `0x68` 과 같음. `-1` 은 "이 칸이 가리키는 데이터 없음" 표시 (`IoPack_Buffer +0xd`, `IoPack… +0x1a0 MOV RAX,R9` 의 `R9=-1`).
- **세마포어 둘은 안 들어간다.** 세마포어는 `RtlInitializeResource` 가 읽기·쓰기 잠금 (`g_pCachedRngRWLock`) 안에 만든 것. **들어가는 핸들은 이벤트 하나뿐 (`+0x60`).** `[정적]`
- **프로세스 ID·함수 주소·스레드 ID 는 안 들어간다.** 입력을 만드는 어디에도 `GetCurrentProcessId`·코드 포인터가 없음 (`IoPack…` 의 `memcpy` 는 문자열·덩어리가 있을 때만, 이번엔 안 탐). `[정적]`
- 엔진 입장: **입력은 읽을 필요 없다.** 읽고 싶다면 위 표대로 (+0 가 `0x1a2b3c4d`, +4 가 `0x10500`, +0x60 이 핸들).
- **지금 우리: 길이 `0x68`·코드 `0x390400` 은 맞음.** (우리가 만들 것은 입력이 아니라 **답** 이다. 입력은 bcrypt 가 채운다.)

#### 2-c. 출력 8바이트

- **읽는 곳이 없다.** `NtDeviceIoControlFile` 이 쓰는 출력 버퍼는 `IoCallKernelDriver` 의 로컬 `[RSP+0x60]` 뿐이고, 성공 길 `+0xd6 TEST R14,R14; +0xd9 JZ +0xff` 에서 **출력 길이 포인터가 NULL 이라 출력을 아무 데도 복사하지 않고** `XOR EAX,EAX` 로 끝난다. 전역에 저장하는 줄도 없음. `[정적]`
- IOSB (`[RSP+0x50]`) 도 성공 길에서는 안 읽음 (`+0xd9 JZ` 에서 이미 나감).
- 그래서 "출력 값 모양까지 보나" → **안 본다.** "출력이 0 이어도 되나" → **된다.**

#### 2-d. TRUE 가 되려면 / 실패 때

- **성공 status (≥ 0) 만으로 된다.** 검사는 `+0xce TEST EAX,EAX; +0xd0 JS` 하나 (부호만 봄). `0`·`0x103`(PENDING)·`0x40000000` 같은 양수는 전부 성공으로 친다. `[정적]`
- **`STATUS_PENDING` 이어도 기다리지 않는다.** attach 길에 `NtWaitForSingleObject` 호출이 없다 (지도·`_calls.tsv` 에 없음). Event 인자도 NULL 이라 기다릴 대상이 없음. 열 때 `0x20` (동기 열기) 라서 진짜 Windows 에서는 I/O 관리자가 알아서 기다려서 PENDING 이 안 온다 `[추측]`. 에뮬레이터는 **0 을 주는 게 안전.**
- **실패 때:** IOCTL 이 음수를 주면 FALSE 다 (**그냥 넘어가지 않는다**). 사슬: `IoCallKernelDriver` 음수 → `BcpRegister…` 가 이벤트 닫고 음수 반환 (`+0x163 JS +0x1e1`) → `InitializeSystemPreferredCache` 가 `CleanupSystemPreferredCache` 하고 음수 반환 (`+0xd9`, `+0x147 JNS` 안 탐 → `+0x149/+0x14e`) → `InitializeCNG` 가 `UninitializeCNG` 하고 반환 (`+0x5b JNZ +0x6d`, `+0x86/+0x8b`) → `DllMain +0x93` 에서 FALSE.
- **지금 우리: 핸들만 채우면 IOCTL 은 "status 0" 만으로 통과.** (로그엔 핸들이 0 이라 `0xc0000008`.)

---

### 3. TRUE 뒤 첫 사용 (이름만)

| 읽는 것 | 읽는 함수 (refs 줄) | 한 줄 이유 |
|---|---|---|
| **`g_hIoDevice`** (rva `0x25ae0`) | `IoCallKernelDriver` (`+0x7e`, `+0x8e`, `+0x1a9`) | 다음 IOCTL 마다 같은 핸들을 쓴다 (한 번 열면 다시 안 엶) |
| 〃 | **`BCryptResolveProviders`** (`+0x5a7`, `+0x5d0`, `+0x755`) | `NtDeviceIoControlFile` 을 **직접** 부름 (`+0x5eb/+0x5f2`, 코드 `0x390400`, 기능 `0x20000`) + `IoCallKernelDriver` (`+0x276/+0x27b`) |
| 〃 | `IoCallKernelDriverQueryLoop` (`+0x12d`, `+0x143`, `+0x2ed`) | 설정 질의 반복 |
| 〃 | `IoShutdown` (`+0x20`, `+0x2c`; 닫고 `+0x3f` 에서 0) | DETACH/실패 정리 (`CloseHandle`) |
| IOCTL 출력 값 | **읽는 함수 없음** | 8바이트는 attach 안에서 버려짐 (2-c) |
| 이벤트 핸들 (rva `0x25b10`) | `RegisterReopenWait +0x2c`, `CleanupSystemPreferredCache +0x67/+0x73` | 알림 이벤트를 `RegisterWaitForSingleObjectEx` 로 기다림 (시스템 기본 난수 핸들 다시 열기). 정리 때 닫음 |
| 〃 `0x25b18` (힙 임계영역 포인터) | `RegisterReopenWait +0x6/+0x61`, `CleanupSystemPreferredCache +0xae/+0xc6/+0xd2` | 위 대기 등록을 감쌈 |
| `g_pCachedRngRWLock` (rva `0x25bc8`) | `BCryptGenRandom +0x12a`, `GetSystemPreferredRngHandle +0x45`, `ReopenSystemPreferredRngHandle +0xb/+0x53`, `CleanupSystemPreferredCache +0x82/+0x9a/+0xa6` | 읽기·쓰기 잠금 (세마포어 2개를 든 `RTL_RESOURCE`) |
| 플래그 rva `0x25ac4` | `CleanupSystemPreferredCache +0x5e`, `GetSystemPreferredRngHandle +0xd4` (쓰기) | "알림 등록됨" 표시 |

- **TRUE 뒤 첫 사용 후보 (호출자에 따라):** 프로그램이 `BCryptOpenAlgorithmProvider` 를 부르면 → `+0x259/+0x25e BCryptResolveProvidersForBcoapCached` → (`+0xcf/+0xd4`, `+0x11b/+0x120`) → **`BCryptResolveProviders`** 가 `g_hIoDevice` 를 읽어 IOCTL 을 부른다 (이 호출이 실제로 일어나는 조건은 안 따라감 `[공백]`). `BCryptGenRandom` (시스템 기본 난수 플래그) 은 `GetSystemPreferredRngHandle +0x78/+0x7d OpenSystemPreferredAlgorithmProvider` → 같은 길로 가거나, `+0x9f/+0xa4 RegisterReopenWait` 로 이벤트를 기다림.
- **엔진이 출력 0 을 줬을 때 뒤에서 터질 자리:**
  - **attach 안에는 없다** (출력을 안 읽음).
  - 뒤 쪽: `BCryptResolveProviders` 는 IOCTL 출력을 **읽어서 해석한다** (`+0x4fb/+0x500 IoUnpack_ProviderRef`) → 출력 0 이면 공급자를 못 찾아 `BCryptOpenAlgorithmProvider` 가 실패할 수 있다. 이건 attach 몫이 아니라 **다음 요청 후보**. (기능 번호 `0x20000` 의 답 모양은 이번에 안 읽음.)
- **지금 우리:** 해당 없음 (attach 가 아직 TRUE 가 안 됨).

---

### 4. (작게) attach 차례 앞쪽

- **`NtQueryInformationProcess` class `0x56`, 길이 `0xb0`**: `IsEnclave +0x44/+0x4b` — **sast/87 의 bcryptprimitives `IsEnclave` 와 같은 함수·같은 꼴** (같은 오프셋, 같은 변환표). 이번에 다시 풀지 않고 sast/87 표를 가리킴: status < 0 → `IsTrustlet` 로 감 / 성공 + `buf[0] ∈ {0x10, 0x11}` → 환경 `0x10` / `buf[0] == 1` → `8` / 그 밖 (0 포함) → `1`. (`IsEnclave +0x50 TEST; +0x52 JS +0x8c; +0x5d~+0x69`.) 진짜 Windows 26100.9457 은 `0xc0000003` (sast/87) → 이어서 class 0 호출 → 환경 `1`. **결과가 attach 에 닿는다**: 환경이 `8`/`0x10` 이면 `InitializeCNG +0x15~+0x19` 로 **초기화를 통째로 건너뛰고 TRUE** (KsecDD 도 안 건드림). `& 1` 이 꺼지면 (`4`) `BcpRegister… +0x8e TEST AL,1; +0x90 JZ +0x16e` 로 IOCTL 을 건너뜀. 이 둘은 정상 프로세스 모양이 아니라 **쓰지 말 것**.
- **`NtQueryInformationProcess` class `0x2b`, 길이 `0x218`**:
  - 호출자: `InitializeConfig +0x31/+0x36` → `InitializeProcessImageNameUM +0xb9/+0xc0` → kernelbase `QueryFullProcessImageNameW +0xb1/+0xb8`. 계산: flags=0 이라 `+0xae LEA EDX,[RBX+0x1b]` (`EBX=0x10`) = **`0x2b`**, 길이 `+0x74 LEA R15D,[0x10+RCX*2]` = `0x10 + 2×0x104` = **`0x218`** (UNICODE_STRING 머리 `0x10` + 글자 260개). `[정적]`
  - PDB 이름: **PDB 에 `PROCESSINFOCLASS` 열거형이 없다.** (bcrypt·ntoskrnl·ntdll·kernelbase PDB 4개 모두 `pdb_struct.py --list` 0건.) 이름은 공개 헤더 기억 `ProcessImageFileNameWin32` (43) `[추측]`. 뒷받침: 커널 `NtQueryInformationProcess` 의 class `0x2b` 처리기 (`+0x1424`, 점프 표로 계산) 가 `PsReferenceProcessFilePointer` (`+0x148a`) → `IoQueryFileDosDeviceName` (`+0x14b9`) 를 부른다 = DOS(Win32) 경로 이름. class `0x1b` (`+0xe21`) 은 `PsQueryFullProcessImageName` (`+0xe84`) = NT 경로. `[정적]`
  - **attach 결과에 닿나: 안 닿는다.** `InitializeConfig +0x31/+0x36` 뒤 `EAX` 를 안 봄. `QueryFullProcessImageNameW` 가 실패(`+0xc6 JS +0x128`)해도 `InitializeProcessImageNameUM +0xc7 JNZ` 안 타고 로그만 남기고 끝남 (`+0x3d4` 맺음). 어떤 답을 줘도 TRUE 에 영향 없음.
- **세마포어 둘 (`NtCreateSemaphore` ×2)**: ntdll `RtlInitializeResource +0xb3/+0xb8`, `+0xe2/+0xe7` (접근 `0x100003`, 속성 NULL, 처음 0, 한도 `0x7fffffff`). 호출 줄 `InitializeSystemPreferredCache +0x38/+0x3f`. 이 `RTL_RESOURCE` 는 전역 **`g_pCachedRngRWLock`** (rva `0x25bc8`, PDB 이름) 이 가리키는 힙 `0x60` 바이트. 용도 = 시스템 기본 난수 공급자 핸들 두 개 (rva `0x25ac8`, `0x25ad0`) 를 지키는 읽기·쓰기 잠금 `[추측]` (사용처 3의 표). **IOCTL 입력에는 안 들어간다.**
- **이벤트 (`NtCreateEvent`)**: `BcpRegisterConfigChangeNotifyNoLogging +0x68/+0x6f CreateEventW(NULL, FALSE, FALSE, NULL)` → 자동 초기화 이벤트 (`EventType = 1`). 핸들은 전역 rva `0x25b10` (PDB 이름 없음) 에 저장되고 **IOCTL 입력 `+0x60` 에 들어간다** → 커널(ksecdd)이 "설정이 바뀌었다" 를 알리려고 받는 신호 `[추측]`. 나중에 `RegisterReopenWait +0x49/+0x50 RegisterWaitForSingleObjectEx` 로 기다림 (attach 안에서는 안 기다림). 엔진은 이걸 **신호 주지 않아도 된다.**
- **지금 우리:** `NtQueryInformationProcess` 0x56 에 "성공 + 0칸" 을 주는 것은 **맞는 환경값 (1) 이 나와서 괜찮음** (sast/87 과 같음). 0x2b 는 아무 답이나 OK. 세마포어·이벤트는 만들어 주기만 하면 됨 (핸들 값은 0 이 아니어야 함: 이벤트는 `+0x77 TEST RAX,RAX`, 세마포어는 `RtlInitializeResource +0xc4 JS`).
- **NtClose ×5 (로그 9번)**: 코드로 설명되는 건 3개 — 이벤트 1 (`BcpRegister… +0x1e9/+0x1f0`) + 세마포어 2 (`CleanupSystemPreferredCache +0x8e/+0x95 RtlDeleteResource` → ntdll `RtlDeleteResource +0x12/+0x17`, `+0x1b/+0x20`). 나머지 2개는 bcrypt 코드에서 못 찾음 `[공백]` (`g_hIoDevice` 는 0 이라 `IoShutdown +0x2a JZ` 로 닫지 않음. 정리 길의 `TlgUnregisterAggregateProvider`·`CancelTimerCallbacksAndDeleteTimer`·`CloseThreadpoolTimer` 쪽 ntdll 안일 수 있음 `[추측]`). TRUE 길에서는 attach 가 아무것도 안 닫는다.

---

### 5. 최소 조건 표 — attach 가 TRUE 로 끝나려면 바깥이 줘야 하는 답

(일반 프로세스, `g_TrustedEnvironment = 1`. sast/87 의 5번 표와 같은 칸 순서.)

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 가는 자리 |
|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | `(-1, 0x56, buf, 0xb0, NULL)` | 아무 status. 성공이면 `buf[0]` 가 **1·0x10·0x11 이 아닌** 값 (0 권장). 진짜는 `0xc0000003` | `buf[0]=1` → 환경 `8`, `0x10/0x11` → `0x10`: `InitializeCNG +0x17` 이 초기화를 통째로 건너뛰고 TRUE (KsecDD 안 건드림, 쓰지 말 것) |
| 2 | `NtQueryInformationProcess` (1이 실패일 때만) | `(-1, 0, buf, 0x40, NULL)`, `buf[0]=0x40` | 성공 + `buf+0x38` 의 **bit 7 = 0**, 또는 실패(→ 같은 결과 `1`) | bit 7 = 1 → 환경 `4`: `BcpRegister… +0x90 JZ +0x16e` 로 IOCTL 건너뜀 (쓰지 말 것) |
| 3 | `NtTraceControl` × 여럿 | (ETW 등록) | 아무 status | 영향 없음 (반환 안 봄: `DllMain +0x80`, `+0x85`, `InitializeConfig +0x46`, `+0x59`) |
| 4 | `NtQueryInformationProcess` | `(-1, 0x2b, buf, 0x218, &len)` | 아무 답 | 영향 없음 (결과 버려짐) |
| 5 | ntdll 코드 `RtlAllocateHeap` ×2 + `NtCreateSemaphore` ×2 | 힙 `0x60`·`0x28` 바이트. 세마포어 접근 `0x100003`, 초기 0, 한도 `0x7fffffff` | **힙 NULL 아님, 세마포어 성공 + 핸들 채움** | 힙 NULL → `InitializeSystemPreferredCache +0x2f/+0xae` → `0xc0000017` → FALSE. 세마포어 실패 → `RtlInitializeResource` 예외 → 착지점 `+0x46` → `0xc00000e5` → FALSE `[추측]` |
| 6 | **`NtCreateEvent`** | `(&h, 0x1f0003, NULL, 1, 0)` | **status ≥ 0 + `*h` ≠ 0** | NULL → `BcpRegister… +0x7a JZ +0x1af` → `WinErrorToNtStatus` → 음수 → FALSE |
| 7 | **`NtOpenFile`** | 이름 `\Device\KsecDD`, 접근 `0x100003`, 공유 `7`, 옵션 `0x20`, 속성 `0`, 루트 0, **`FileHandle` = `g_hIoDevice` (bcrypt `+0x25ae0`)** | **status ≥ 0 + 그 포인터가 가리키는 칸(`+0x25ae0`)에 0 아닌 핸들 값을 씀** | status < 0 → `IoCallKernelDriver +0x1d9` → FALSE. status ≥ 0 인데 칸이 0 → 아래 8번이 `0xc0000008` → FALSE (**지금 우리**) |
| 8 | **`NtDeviceIoControlFile`** | 핸들 = `+0x25ae0` 의 값, Event·APC·Context 0, 코드 **`0x390400`**, 입력 버퍼 0x68바이트 (2-b 표), 출력 버퍼 8바이트 | **status ≥ 0** (0 권장). 출력·IOSB 는 안 봄. 입력은 안 읽어도 됨 | 음수 → FALSE (`+0xd0 JS`). `0xc0000023` 이면 `0xc00000e5`. `0x103` 이어도 기다리지는 않지만 비권장 |
| 9 | ntdll·kernelbase 코드 `GetModuleHandleExW(5, &DllMain, …)` | `RtlPcToFileHeader` + `LdrAddRefDll(1, 이미지 시작)` | **성공 (TRUE)** | 실패 → `DllMain +0x13b JNZ` 안 탐 → `+0x189` → **FALSE** (bcryptprimitives 와 다름) |
| 10 | `NtClose` | — | 호출 없음 | — (TRUE 길에서는 attach 가 안 닫음) |

- 이 표를 다 맞추면 attach 는 `+0x1c5 MOV EAX,1` 로 끝난다. 이때 남는 전역: `g_hIoDevice ≠ 0` (열린 핸들 1개), 이벤트 핸들 (rva `0x25b10`, 열린 핸들 1개), `g_pCachedRngRWLock ≠ 0` (세마포어 2개), rva `0x25b18` (힙 임계영역), rva `0x25ac4 = 1`, `g_TrustedEnvironment = 1`, `g_fLoadCNGDone = 1`, `g_hInstance+8` 에 `0xe` (`0x2|0x4|0x8`). DLL 은 고정(PIN) 된다.
- **같은 DLL 안 다른 모듈과의 나란히 보기 (sast/87 vs 이번):**

| | bcryptprimitives (sast/87) | bcrypt (이번) |
|---|---|---|
| 장치 이름 | `\Device\CNG` | `\Device\KsecDD` |
| `NtOpenFile` 접근 / 속성 | `0x100001` / `0x40` | `0x100003` / `0` |
| 핸들을 받는 곳 | 지역 칸 → 캐시 `g_hCNGDriver` | **전역 `g_hIoDevice` 에 바로** |
| 열고 나서 핸들 0 검사 | **한다** (호출자가 0 이면 실패) | **안 한다** (IOCTL 로 가서 커널이 `0xc0000008`) |
| 재시도 | 5번 | 없음 |
| IOCTL 코드 / 입력 / 출력 | `0x390008` / 없음 / `0x30` | `0x390400` / **`0x68` (이벤트 핸들 포함)** / `8` |
| IOCTL 출력 | 시드로 씀 (내용 검사 없음) | **버림** |
| IOCTL status | ≥ 0 이면 성공 | ≥ 0 이면 성공 |
| `IsEnclave`·`GetTrustedEnvironment` | 같은 꼴 | 같은 꼴 (같은 오프셋) |
| `GetModuleHandleExW(5, …)` 실패 | TRUE | **FALSE** |
| 로그에서 보이는 막힘 모양 | `NtOpenFile` ×5 | `NtOpenFile` ×1 + `NtDeviceIoControlFile`(핸들 0) |
| 고칠 것 | `NtOpenFile` 이 핸들 칸에 쓰기 | **같음** (+ IOCTL 은 status 0) |

---

## 동적분석으로 확인해야 할 것

실행 때만 정해지는 것. 칸만 적음 (`[추측]` 은 값 짐작). 모듈 이름은 WinDbg 에서 `bcrypt`. 모든 멈춤은 `test.exe` 일 때만 (`bcrypt.dll` 은 여러 프로세스가 올림).

- **진짜 `NtOpenFile(\Device\KsecDD)` 의 status 와 핸들 값**: `_IoOpenDevice +0x81` 직후 `eax`, 그리고 `[bcrypt+0x25ae0]` (`g_hIoDevice`). 4의 배수 작은 수일 것 `[추측]`.
- **진짜 `NtDeviceIoControlFile` 의 status·`IoStatus.Information`·출력 8바이트**: `IoCallKernelDriver +0xc9` 직후 `eax`, `[rsp+0x50]` (IOSB 16바이트), `[rsp+0x60]` (출력 8바이트). 안 쓰이는 값이지만 진짜 Windows 에서 `0`/`STATUS_SUCCESS` 가 나오는지 확인. 출력은 0 일 것 `[추측]`.
- **진짜 입력 `0x68` 바이트의 실제 모양**: `IoCallKernelDriver +0xc2` 에서 `NtDeviceIoControlFile` 을 부르기 직전 `[rsp+0x30]` 이 가리키는 버퍼 (`dq` 14개). 정적 예상: `+0 = 0x1a2b3c4d`, `+4 = 0x10500`, `+0x10/+0x20/+0x28/+0x30/+0x40/+0x48/+0x58 = -1`, `+0x60 = 이벤트 핸들`.
- **진짜 `NtQueryInformationProcess(-1, 0x56)` 의 status** (sast/87 과 같이 `0xc0000003`) 와 이어지는 class 0 호출 + 최종 `g_TrustedEnvironment` (`[bcrypt+0x25ac0]` = 1): `IsEnclave +0x4b` 직후 `eax`, `GetTrustedEnvironment +0x1b` 직후 `eax`.
- **`GetModuleHandleExW` 반환** (진짜 Windows 에서 `DllMain` 안에서 성공하는지): `DllMain +0x134` 직후 `eax`. 에뮬레이터가 이 줄에 닿으면 다음 벽 후보.
- **`NtClose` 가 attach 중에 한 번도 안 나오는지** (TRUE 길): 진짜 Windows 에서 `bcrypt` 가 올라오는 동안의 `NtClose` 개수.
- **`NtTraceControl` 개수와 인자** (로그의 "여럿"·"×4"): 반환을 안 봐서 결과엔 무관. 에뮬레이터를 진짜와 맞추고 싶을 때만.
- 추천 멈춤점 (모두 `test.exe` 조건):
  - `bcrypt!DllMain +0x8f` (= `InitializeCNG` 반환 직후): `eax` 0 이면 성공 길, 음수면 FALSE 길 (`0xc0000008` 가 예상 `[추측]` — 지금 에뮬레이터에서는).
  - `bcrypt!_IoOpenDevice +0x81` (= `NtOpenFile` 반환 직후): `eax` (status) 와 `[bcrypt+0x25ae0]` (핸들 칸) 둘 다.
  - `bcrypt!IoCallKernelDriver +0xc9` (= `NtDeviceIoControlFile` 반환 직후): `eax`, IOSB `[rsp+0x50]`, 입력 `[rsp+0x30]` (멈춘 순간 `+0xc2` 에서 이미 레지스터에 있음 — 호출 직전 `+0xc2` 에 멈추면 `rdx`/`rsp+0x30` 으로 봄).
  - `bcrypt!DllMain +0x134` (= `GetModuleHandleExW` 반환 직후): `eax`.
  - `bcrypt!IsEnclave +0x4b`: `eax` (status), `[rsp+0x30]` 첫 DWORD.

## 다음 요청 후보

1. **(정적, 작게)** ntdll `LdrAddRefDll` (PIN) 와 `RtlPcToFileHeader`: `DllMain` 안에서 `GetModuleHandleExW(5, &DllMain)` 가 syscall 없이 성공하는 조건. bcrypt 는 실패하면 FALSE 라서, KsecDD 가 풀리면 **다음 벽 후보**. (이번엔 kernelbase 쪽 호출 줄까지만 봄.)
2. **(정적)** attach 가 TRUE 된 **다음** 첫 막힘: `BCryptOpenAlgorithmProvider +0x259/+0x25e` → `BCryptResolveProvidersForBcoapCached` → `BCryptResolveProviders` 의 IOCTL (`0x390400`, 기능 `0x20000`) 입력·출력 모양 (`IoUnpack_ProviderRef` 가 읽는 칸). 출력 0 으로는 안 될 가능성이 있다 (질문 3).
3. **(정적, 후순위)** `ksecdd.sys` 의 `0x390400` 처리. 에뮬레이터 안에서는 attach 용 `0x10500` 에 "status 0" 으로 충분하므로 필요 없을 가능성이 큼. 요청서도 범위 밖.
4. **(정적, 후순위)** `NtClose` ×5 의 나머지 2개 정체 (정리 길 `TlgUnregisterAggregateProvider`·`CloseThreadpoolTimer`). FALSE 가 TRUE 로 바뀌면 필요 없음.
5. **(동적)** 위 추천 멈춤점 5곳 한 번에. 특히 `_IoOpenDevice +0x81` 과 `IoCallKernelDriver +0xc9`.

## 근거 파일

- `ms/disassemble/bcrypt/entry.asm` (`+0x11`, `+0x23~+0x26`, `+0x2c`, `+0x41~+0x8f`, `+0x93`, `+0x95~+0xc5`, `+0xca~+0x13b`, `+0x141~+0x189`, `+0x18d~+0x1d9`)
- `ms/disassemble/bcrypt/InitializeCNG.asm`, `GetTrustedEnvironment.asm`, `IsEnclave.asm`, `IsTrustlet.asm`, `InitializeLoader.asm`, `_InitializeLoaderLock.asm`, `InitializeConfig.asm`, `IoStartup.asm`, `InitializeProcessImageNameUM.asm`
- `ms/disassemble/bcrypt/InitializeSystemPreferredCache.asm` (`+0x46~+0x84` 틈은 PE 바이트 `objdump` 로 읽음), `BcpRegisterConfigChangeNotifyNoLogging.asm`, `IoPack_Configuration_ParamBlock.asm`, `IoPack_Buffer.asm`, `IoCallKernelDriver.asm`, `_IoOpenDevice.asm`
- `ms/disassemble/bcrypt/CleanupSystemPreferredCache.asm`, `UninitializeCNG.asm`, `UninitializeConfig.asm`, `UninitializeLoader.asm`, `IoShutdown.asm`
- 질문 3 (이름만): `BCryptResolveProviders.asm` (`+0x276`, `+0x590`, `+0x5a7~+0x5eb`, `+0x755`), `BCryptResolveProvidersForBcoapCached.asm`, `BCryptOpenAlgorithmProvider.asm` (`_calls.tsv` 줄만), `GetSystemPreferredRngHandle.asm`, `RegisterReopenWait.asm`, `BCryptGenRandom.asm` (`_calls.tsv` 줄만)
- 지도·참조: `ms/disassemble/bcrypt/_map/entry_edges.tsv`, `entry_nodes.tsv`, `entry_summary.txt`; `ms/disassemble/bcrypt/_refs/0x25ae0.tsv`, `0x25b10.tsv`, `0x25ac4.tsv`, `0x25b18.tsv`, `0x25bc8.tsv`, `0x25ad8.tsv`, `0x25ae8.tsv`, `0x7f50.tsv`, `0x5bf4.tsv`, `0x8154.tsv`; 호출 줄 `ms/disassemble/bcrypt/_calls.tsv`
- `ms/disassemble/ntdll/RtlInitializeResource.asm` (`+0x9c~+0xe7`), `RtlDeleteResource.asm`; `ms/disassemble/kernelbase/CreateEventW.asm` (`+0x5a~+0x76`), `QueryFullProcessImageNameW.asm` (`+0x74`, `+0xa2~+0xb1`), `GetModuleHandleExW.asm` (`+0x88`, `+0x130`); `ms/disassemble/ntoskrnl/NtQueryInformationProcess.asm` (`+0x2d5~+0x2f7`, 점프 표 → class `0x2b` `+0x1424`, class `0x1b` `+0xe21`)
- PE 바이트 (직접 읽음): rva `0x1f098` `\Device\KsecDD`, 진입점 rva `0x11140`, `DllCharacteristics 0x41e0`. PDB 공개 이름: `DllMain`, `?g_hIoDevice@@3REAXEA`, `g_pCachedRngRWLock`, `g_TrustedEnvironment`, `g_hInstance`, `g_fLoadCNGDone`, `?g_csIoInitialize@@3U_RTL_CRITICAL_SECTION@@A` (`llvm-pdbutil dump -publics`).

## 판정

**분석성공.** 요청서 5개 질문 모두 `.asm` 줄로 답함. 막힘의 원인 (`NtOpenFile` 이 status 0 + 핸들 칸 0 → IOCTL 핸들 0 → `0xc0000008` → `DllMain` FALSE) 이 코드로 확정됐고 로그의 syscall 차례·인자·길이와 어긋남 없음. IOCTL `0x390400` 은 입력 `0x68` (칸 표 있음, 이벤트 핸들 1개 포함), 출력 8바이트는 **안 읽힘**, 성공 status 만 필요. 새로 찾은 것: bcrypt 만의 FALSE 갈래 `GetModuleHandleExW` 실패 (`DllMain +0x13b`). 못 닫은 것: `NtClose` 나머지 2개의 정체, `GetModuleHandleExW` 안쪽 ntdll 의 syscall 유무 — 둘 다 이번 막힘에는 무관.
