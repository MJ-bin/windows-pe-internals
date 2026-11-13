# 87 bcrypt-init — 정적 분석 결과

- 요청서: `_workspace/sast/87_bcrypt-init.md` (Q169, PE_emulator 요청 31)
- 스크립트 문제: **없음.** (analyze 로그에 `WindowsResourceReference` 클래스 못 찾음 WARN 한 줄이 있지만 결과에 영향 없음)
- 모듈 / 바이너리:
  - bcryptprimitives (`ms/bin/.../System32/bcryptprimitives.dll`, 716408 바이트, x64, 이미지 베이스 `0x180000000`)
  - **PDB GUID+Age = `E63663F36C3ABFBF74EACD102DC13DBB1`** (`ms/sym/bcryptprimitives.dll.pdb`, 별칭 표 `bcryptprimitives.dll.aliases.tsv`)
  - **이번에 처음 analyze 함** (21:52~21:53, 약 26초). `SetPdb:` 줄 있음, `_index.tsv` 함수 1209개, `.gpr` 있음.
  - ntdll·kernelbase·ntoskrnl 은 다시 analyze 안 함 (읽기만, export 몇 개).
- 이름 메모: 진입점 `DllMain` (PDB 이름) 은 Ghidra 에서 **`entry`** 로 잡힘 (`rva 0x433c0`). `pdb_aliases.py --where` 로 확인. 문서·그림에는 `DllMain` 으로 쓰고 Ghidra 이름을 한 번 밝힘.
- 뽑은 함수:
  - bcryptprimitives 47개 (`^entry$` depth 1 → 14개, 정규식 28개 depth 0, 정규식 3개 depth 0, 정규식 2개 depth 0) → `ms/decomp/bcryptprimitives/`, `ms/disassemble/bcryptprimitives/`
  - 지도: `run_ghidra.sh map bcryptprimitives.dll entry` → `ms/disassemble/bcryptprimitives/_map/entry_{edges,nodes}.tsv`, `entry_summary.txt` (285개 함수, 깊이 10)
  - ntdll 1개 (`^RtlGetSystemGlobalData$` depth 0), kernelbase 1개 (`^DeviceIoControl$` depth 0)
  - ntoskrnl 은 이미 있던 `NtQueryInformationProcess.asm` 과 PE 바이트(점프 표)만 봄.
- 범위를 넓힌 이유: 요청서 대상 "attach 갈래 초기화 함수" 를 지도로 찾음. `NtOpenFile` 은 진입점 바로 밑이 아니라 깊이 4 (`GetCngHandle`) 에 있고, 거기로 가는 길은 **함수 포인터** (`g_pGetSeedFunction`) 라 지도에 선이 없음 → `InitUmRootRngState` 의 `.asm` 로 포인터 값을 따라가 `GetSeedFromKernelState` 를 따로 뽑음.
- 버전 확인: **일치**
  - 이 모듈은 "알려진 사실" 표에 없음. 대신 **에뮬레이터 로그의 값과 코드의 값이 같음** (둘이 같은 DLL 이라는 증거):
    - `NtOpenFile` 접근 `0x100001` · 공유 `7` · 옵션 `0x20` = `GetCngHandle +0x77 / +0x87 / +0x5c` 값과 같음.
    - `NtQueryInformationProcess(-1, 0x56, …, 0xb0)` = `IsEnclave +0x31~+0x44` 와 같음.
    - `NtOpenFile` 5번 = `InitUmRootRngState` 재시도 5번 (아래 Q1) 과 같음.
    - 로그의 syscall 차례 (NtQIP → NtTraceControl×3 → NtOpenFile×5) = 코드의 호출 차례와 같음.
  - PE 머리: 진입점 RVA `0x433c0` = Ghidra `entry` 시작, `DllCharacteristics = 0x41e0`.

---

## 한눈에 (먼저 읽기)

- 샘플 1183개가 막히는 원인은 **`NtOpenFile(\Device\CNG)` 가 "status 0 인데 핸들 칸을 안 씀"** 이라서다. 코드 쪽에서 끝까지 따라가 로그와 한 칸도 안 어긋나게 설명된다. `[정적]`
  - `GetCngHandle` 은 status 만 보고 성공으로 치지만, 돌려주는 값은 **핸들 칸(0 그대로)**. 0 이면 "못 열었다" 로 보고, 캐시(`g_hCNGDriver`)에도 0 만 남아서 **다음 시도에 또 연다.**
  - 시드 함수를 최대 5번 불러보고 포기 → `InitUmRootRngState` 가 0 → `MsProviderStart` 가 `0xc00000e5` → `DllMain` 이 `SetLastError(0x54f)` 하고 FALSE.
- **TRUE 가 되는 최소 조건 (일반 프로세스, lsass 아님):**
  1. `NtOpenFile(\Device\CNG)` → status ≥ 0 **그리고 핸들 칸에 0 아닌 값.**
  2. `NtDeviceIoControlFile(그 핸들, 코드 0x390008, 입력 없음, 출력 0x30바이트)` → status ≥ 0. 출력 내용은 안 봄.
  3. 나머지 (`NtQueryInformationProcess`, `NtTraceControl`, 레지스트리) 는 **무엇을 돌려줘도** TRUE 에 영향 없음 (단 `class 0` 답의 bit 7 이 켜지면 안 됨).
- **새로 알게 된 것:** 26100.9457 커널은 `NtQueryInformationProcess` class `0x56` 을 **구현하지 않는다** (jump 표에서 기본 갈래 → `STATUS_INVALID_INFO_CLASS 0xc0000003`). 진짜 Windows 는 attach 때 NtQIP 를 **2번** 부른다 (0x56 실패 → class 0). 에뮬레이터 로그는 1번뿐 → 에뮬레이터는 0x56 에 "성공 + 0칸" 을 준 것. 결과(`g_TrustedEnvironment = 1`)는 같다.

---

## 질문별 답 [정적]

### 1. attach 길과 `NtOpenFile`

#### 1-a. attach 차례 표 (`DllMain` = Ghidra `entry`, 일반 프로세스 기준)

`reason = EDX`. `entry +0x1c / +0x1f` 에서 `CMP EDX,1; JNZ +0x13e` → attach(1) 만 아래로. 오프셋은 `ms/disassemble/bcryptprimitives/<함수>.asm` 줄. "call/ret" = `call 자리 / ret 자리`.

| # | 함수 + 오프셋 (call/ret) | 부르는 것 | syscall / 갈림 |
|---|---|---|---|
| 1 | `entry +0x25 ~ +0x2d` | 전역 `g_TrustedEnvironment` (`0x1800a42c0`) 읽음. 0 이면 `+0x15f/+0x164` 로 `GetTrustedEnvironment` | 처음엔 0 → 아래 2 |
| 2 | `GetTrustedEnvironment +0x4/+0x9` | `IsEnclave` | `IsEnclave +0x44/+0x4b` → **`NtQueryInformationProcess(-1, 0x56, buf, 0xb0, NULL)`** |
| 3 | `GetTrustedEnvironment +0xd/+0x12` | `IsTrustlet` (2 가 0 을 돌려줄 때만) | `IsTrustlet +0x34/+0x3b` → **`NtQueryInformationProcess(-1, 0, buf, 0x40, NULL)`**. 결과를 `g_TrustedEnvironment` 에 저장 (`+0x1b`) |
| 4 | `entry +0x33 ~ +0x49` | `g_TrustedEnvironment` 에 bit 8 이 없으면 **내 PE 머리를 읽음**: `[이미지시작 + e_lfanew + 0x5e] & 0x80` | **`+0x49 JZ +0x1cf` = FALSE 갈래 #1** (아래 1-d) |
| 5 | `entry +0x52/+0x59` | `DisableThreadLibraryCalls` | syscall 없음 `[정적]` (kernelbase→ntdll 안은 안 읽음) |
| 6 | `entry +0x69/+0x6e` | `_DllMainCRTStartupForGS2` → `__security_init_cookie` (`+0x9/+0xe`) → `__security_init_cookie_ex` | 호출 없음. syscall 없음 |
| 7 | `entry +0x6e/+0x73` | `SymCryptInitEnvWindowsUsermodeWin8_1nLater` → `SymCryptDetectCpuFeaturesByCpuid` (`+0xe/+0x13`), `GetEnabledXStateFeatures` (`+0x13/+0x1a`) | cpuid + 공유 페이지. syscall 없음. 결과 안 봄 |
| 8 | `entry +0x73 ~ +0x88` | 환경값 다시 읽음. bit 8(엔클레이브) 이면 `+0x199` 로 CPU 기능 검사 | 일반은 안 감 (FALSE 갈래 #3) |
| 9 | `entry +0x8e/+0x93` | `SafeAllocaInitialize` → `RtlImageNtHeader` (`+0x11/+0x18`) | PEB 읽기 (`GS:[0x60]`). syscall 없음 |
| 10 | `entry +0xa1 ~ +0xbd` | WPP 전역 칸 채움 (`WPP_GLOBAL_Control = &WPP_MAIN_CB`) | — |
| 11 | `entry +0xc8/+0xcd` | `WppInitUm` → **`EtwRegisterTraceGuidsW`** (`WppInitUm +0x6e/+0x75`) | `NtTraceControl` 후보 (Q4). **반환값 안 봄** (`entry +0xcd` 바로 다음 call) |
| 12 | `entry +0xcd/+0xd2` | **`MsProviderStart`** | 아래 표. 반환 음수면 `+0xd4 JS +0x1be` = FALSE 갈래 #2 |
| 13 | `entry +0xfc/+0x103` | `GetModuleHandleExW(5, &DllMain, &h)` (5 = 고정 + 주소로 찾기) | 실패해도 TRUE (`+0x111 JZ +0x20a` → … → `+0x128`) |
| 14 | `entry +0x128` | `MOV EAX,1` → `ret` (`+0x13c`) | **TRUE** |

`MsProviderStart` (rva `0x436c4`) 안:

| # | 함수 + 오프셋 (call/ret) | 부르는 것 | syscall / 갈림 |
|---|---|---|---|
| a | `+0x2d ~ +0x62` | `g_TrustedEnvironment` 읽음 (0 이면 `+0x4e/+0x53` `GetTrustedEnvironment`). bit 8·0x10(엔클레이브) 이 없으면 → b | — |
| b | `+0x64/+0x69` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` → `EventRegister` (`+0x55/+0x5c`), `EventSetInformation` (`+0x7c/+0x83`) | `NtTraceControl` 후보 (Q4). **반환값 버려짐** (`+0x69` 에서 `EAX` 를 전역으로 덮음) |
| c | `+0x6f ~ +0x87` | `g_TrustedEnvironment & 0x16` 가 0 이면 `JZ +0x10e` (**일반 프로세스는 여기**). 0 아니면 IUM 갈래 (`+0xaa` `GetModuleHandleExW` / `+0xce` `LoadLibraryW` = `vertdll.dll` / `iumbase.dll`) | 일반은 안 감 |
| d | `+0x10e ~ +0x11c` | `g_fSteEnabled`, `g_fSelftest`, `g_fSelftestsRunning` = 1 | — |
| e | `+0x126/+0x12b`, `+0x12b/+0x130`, `+0x130/+0x135` | `SymCryptRngAesInstantiateSelftest`, `…ReseedSelftest`, `…GenerateSelftest` | 순수 계산. 틀리면 `SymCryptFatal`. syscall 없음 |
| f | **`+0x13b/+0x140`** | **`InitUmRootRngState`** | 아래 표. 반환 0 → `+0x142 JNZ` 안 탐 → `+0x144` `R9D=0xa1; JMP +0xec` → `EBX = 0xc00000e5` → `DebugTraceError` (`+0x104/+0x109`) → `JMP +0x34c` → 반환 `0xc00000e5` |
| g | `+0x14c`, `+0x14f/+0x154` | `SIL=1`; **`IsInLSAProcess`** | 0 이면 `+0x156 JZ +0x345` |
| h | `+0x345 ~ +0x34c` | `g_fProviderStarted = 1` → `+0x353 JNZ +0x35f` → `EAX = EBX = 0` → `ret` (`+0x389`) | **성공 (일반 프로세스)** |
| i | (lsass 일 때만) `+0x17e ~ +0x2a8` | `QueryPerformanceFrequency/Counter`, **`AlgorithmCheck`** (`+0x20e/+0x213`), `GetDriverSelftestCycleCount` (`+0x2a8/+0x2ad`) | 일반 프로세스는 안 감 |

`InitUmRootRngState` (rva `0x2b5dc`) 안:

| # | 오프셋 | 하는 일 |
|---|---|---|
| a | `+0x19 ~ +0x57` | `g_TrustedEnvironment` 보고 시드 함수 고름. bit 8 → `GetSeedFromRdrand`(`0x18005f3f0`, `+0xd8`). `&0x16` → IUM(`GetProcAddress(g_hIumBase, "GetSeedFromIumKernelState")`, `+0xf3 ~ +0x108`). **그 밖(일반) → `GetSeedFromKernelState` (`0x180046d00`, `+0x5d`)** → `g_pGetSeedFunction` (`0x1800a42a0`) 에 저장 (`+0x64`) |
| b | `+0x6b ~ +0x7a` | `R8 = &rsp+0x20` (시드 버전 8바이트 받을 칸), `EDX = 0x30`, `RCX = &rsp+0x28` (시드 0x30바이트, 위에서 0으로 채움) |
| c | **`+0x7a/+0x7f`** | **CFG 간접 호출 `g_pGetSeedFunction(buf, 0x30, &ver)`** (`__guard_dispatch_icall_fptr` 썽크 `0x180082010`). 지도·`_calls.tsv` 엔 `indirect:cfg:GetSeedFromKernelState` 로 나옴 |
| d | `+0x81 ~ +0x83` | 반환 0 이면 `JZ +0x129` (재시도 갈래) |
| e | `+0x129 ~ +0x137` | `EBX++`, `EBX >= 5` 면 `JNC +0xbd` (포기, 반환 0). 아니면 `g_pGetSeedFunction` 다시 읽고 `JMP +0x6b` |
| f | **`+0x9a/+0x9f`** | 성공이면 `AesRNGState_init(&g_rootAesRngState, 시드, 버전)`. `+0xa1 JS +0x13c` → `EDI=0` → 반환 0 (재시도 없음) |
| g | `+0xa7 ~ +0xbd` | 시드 0x30바이트 0으로 지움, `g_rngState (0x1800a42a8) = 1`, 반환 1 |

`GetSeedFromKernelState` (rva `0x46d00`) 안:

| # | 오프셋 (call/ret) | 하는 일 |
|---|---|---|
| a | `+0x1e/+0x23` | **`GetCngHandle`** → `RDI`. `+0x29 JZ +0xa0` → (`+0xa0 JMP +0x8f`) 반환 `EAX = 0` |
| b | `+0x3a/+0x41` | `RtlGetSystemGlobalData(1, &buf, 8)` (ntdll). `+0x48 JNS` 아니면 `+0x4a INT 0x2c` (**크래시**) |
| c | **`+0x77/+0x7e`** | **`DeviceIoControl(핸들, 0x390008, NULL, 0, 시드버퍼, 0x30, &바이트수, NULL)`** → kernelbase → `NtDeviceIoControlFile` |
| d | `+0x83 ~ +0x8c` | 반환 0 이면 그대로 반환 0. 아니면 `*ver = buf` (b 에서 읽은 값) 후 반환(0 아님) |

`GetCngHandle` (rva `0x46da8`) 안:

| # | 오프셋 (call/ret) | 하는 일 |
|---|---|---|
| a | `+0x10 ~ +0x34` | `g_hCNGDriver` (`0x1800a42b0`) 읽음. **0 아니면 바로 그 값 반환** (캐시). 지역 `핸들칸 [RBP+0x10]` 에 같은 값을 미리 넣어 둠 |
| b | `+0x45/+0x4c` | `RtlInitUnicodeString(&이름, L"\Device\CNG")` (문자열 `.rdata` `0x18008d0b8`) |
| c | `+0x51 ~ +0x94` | `OBJECT_ATTRIBUTES`: 크기 `0x30`, **루트 0**, 이름 위 칸, **속성 `0x40`** (`OBJ_CASE_INSENSITIVE`), 보안 0. `IO_STATUS_BLOCK` 0으로 |
| d | **`+0x94/+0x9b`** | **`NtOpenFile(&핸들칸, 0x100001, &OA, &IOSB, 공유 7, 옵션 0x20)`** |
| e | `+0xa0 / +0xa2` | `status < 0` 이면 `JS +0xc0` → `핸들칸` 값(=0) 반환 |
| f | `+0xa4 ~ +0xb6` | **status ≥ 0**: `LOCK CMPXCHG [g_hCNGDriver] = 핸들칸값` (기대값 0). 성공(옛값 0)이면 `+0xb8` 에서 **`핸들칸` 값을 반환** |
| g | `+0xd1/+0xd5/+0xdc` | CMPXCHG 에 져서 (다른 스레드가 먼저 캐시) 옛값 ≠ 0 이면 내 핸들 `CloseHandle` 하고 옛값 반환 |

#### 1-b. `NtOpenFile` 이 여는 이름과 값

- 이름: **`\Device\CNG`** (UTF-16, `.rdata` rva `0x8d0b8`). `\Device\KsecDD` 가 아님. `[정적]`
- 접근 `0x100001` (`SYNCHRONIZE | FILE_READ_DATA`), 공유 `7`, 옵션 `0x20` (`FILE_SYNCHRONOUS_IO_NONALERT`), 속성 `0x40`, 루트 핸들 0. **로그와 같음.**
- 지금 우리 (PE_emulator): **값은 맞음.** 이름을 모르고 status 0 만 주는 것까지는 괜찮으나, **핸들 칸을 안 쓰는 게 틀림** (아래).

#### 1-c. 5번 되풀이하는 조건

- **무엇을 보고 다시 여나:** `g_pGetSeedFunction` 의 반환(BOOL) 이 0 인지. 이름 5개가 아니라 **같은 이름 하나를 5번.** 핸들 값이나 status 를 직접 비교하지 않는다. `[정적]`
- 시도 횟수 변수 `EBX` (0 에서 시작): 실패마다 `INC EBX; CMP EBX,5; JNC` (`+0x129 / +0x12b / +0x12e`) → **5번 부르고 (EBX = 0,1,2,3,4) 포기.**
- 한 번 부를 때 `NtOpenFile` 이 나가는 조건: `GetCngHandle` 에서 캐시 `g_hCNGDriver` 가 0 일 때. **성공해서 캐시가 차면 이후 시도는 `NtOpenFile` 을 안 부르고 `NtDeviceIoControlFile` 만 다시 한다.**
- 로그의 `NtOpenFile` × 5 = 캐시가 계속 0 이었다는 뜻 = **핸들이 한 번도 안 채워졌다.** 코드로 설명되는 유일한 경우 (아래 1-d).
- 지금 우리: **맞게 재현됨** (5번이 코드와 일치). 단 그 원인이 "핸들 칸 비움" 이라는 걸 알아야 고칠 수 있다.

#### 1-d. status 0 인데 핸들 칸이 안 쓰인 경우

1. `GetCngHandle +0xa0 TEST EAX,EAX; +0xa2 JS` 안 탐 (status 0).
2. `+0xa4` `RCX = [RBP+0x10]` = 0 (처음 넣어 둔 값 그대로). `+0xa8 EAX=0`. `+0xaa LOCK CMPXCHG [g_hCNGDriver], RCX`: 기대값 0 = 지금 값 0 → "성공"으로 **0 을 0 위에 씀** → 캐시 비어 있음 그대로. `ZF=1` → `+0xb6 JNZ` 안 탐.
3. `+0xb8 RBX = [RBP+0x10]` = 0 → `+0xcf` 반환 **0 (NULL)**.
4. `GetSeedFromKernelState +0x26/+0x29 JZ +0xa0` → 반환 0 (BOOL FALSE).
5. `InitUmRootRngState +0x83 JZ +0x129` → 재시도 5번 → `+0x12e JNC +0xbd` → 반환 0.
6. `MsProviderStart +0x142` (JNZ 안 탐) → `+0x144 R9D=0xa1` → `+0xec EBX = 0xc00000e5` → `DebugTraceError +0x104/+0x109` (WPP 꺼져 있으면 바로 돌아옴: `DebugTraceError +0x17 JZ`, `WPP_GLOBAL_Control` 이 자기 자신을 가리킴 — 초기값 `0x1800a3430` 확인) → `JMP +0x34c` → `SIL=0` 이라 `TearDownRNGStates` 안 부름 → 반환 `0xc00000e5`.
7. `entry +0xd2 TEST EAX,EAX; +0xd4 JS +0x1be` → `+0x1c3` `SetLastError(0x54f)` (`ERROR_INTERNAL_ERROR`) → `+0x1cf XOR EAX,EAX` → `+0x1d1 JMP +0x12d` → **FALSE**.

`status < 0` 인 경우 (`+0xa2 JS`) 도 반환 0 이라 **똑같이 5번 → FALSE.**

- 지금 우리: **이게 바로 그 막힘.** 틀린 점 = 핸들 칸.

#### 1-e. FALSE 로 가는 자리 전부 (attach)

| # | 자리 | 조건 | 일반 프로세스에서 |
|---|---|---|---|
| 1 | `entry +0x49 JZ +0x1cf` | `g_TrustedEnvironment & 8 == 0` **그리고** `[이미지 + e_lfanew + 0x5e] & 0x80 == 0` (DllCharacteristics 의 `FORCE_INTEGRITY` 비트) | 파일의 값 `0x41e0` → 비트 **켜짐** → 안 감. 단 에뮬레이터가 PE 머리를 메모리에 올려 두고 `base+0x3c` 와 `base+e_lfanew+0x5e` 를 읽을 수 있어야 함 `[정적]` |
| 2 | `entry +0xd4 JS +0x1be` → `SetLastError(0x54f)` | `MsProviderStart` 가 음수 | **여기가 막힘.** 음수가 되는 곳: (가) `MsProviderStart +0x142` (InitUm=0) → `0xc00000e5`. (나) IUM 갈래 `+0xe4` (일반은 안 감). (다) `AlgorithmCheck` 음수 `+0x28c` (lsass 일 때만) |
| 3 | `entry +0x1a1 JNZ` / `+0x1b9 JMP` → `FUN_180081a96` (`MOV ECX,0x32; JMP entry+0x1c3`) → `SetLastError(0x32)` | **엔클레이브(`& 8`)일 때만**, CPU 기능 `g_SymCryptCpuFeaturesNotPresent` 에 bit 2, 또는 (`& 0x200` 이고 bit 10) | 일반은 안 감 |

- 위 3곳 말고 attach 에서 `XOR EAX,EAX` / `MOV EAX,0` 로 끝나는 자리 없음. 나머지는 전부 `+0x128 MOV EAX,1`. `GetModuleHandleExW` 실패(`+0x111 JZ +0x20a`) 도 TRUE 로 끝남 (`+0x16b JZ +0x128`). `[정적]`
- 크래시(`FALSE` 아님)로 끝나는 자리: `GetSeedFromKernelState +0x4a INT 0x2c` (`RtlGetSystemGlobalData` 실패), `TraceLoggingRegisterEx_EventRegister_EventSetInformation +0xb0 INT 0x29` (이미 등록된 상태에서 또 등록 — 첫 attach 는 안 걸림, 전역 `0x1800a3458` 초기 0 확인), `SymCryptFatal`/`TerminateProcess` (셀프테스트·연속 난수 검사 실패).

### 2. 연 뒤 부르는 syscall 차례

| # | 자리 (call/ret) | syscall | 인자 | 읽는 칸 / 갈림 |
|---|---|---|---|---|
| 1 | `GetSeedFromKernelState +0x3a/+0x41` → ntdll `RtlGetSystemGlobalData` | **없음** (공유 페이지 읽기) | ID 1, 길이 8 | `[0x7ffe0250]` = `KUSER_SHARED_DATA.RNGSeedVersion` (8바이트, PDB 확인) 를 읽어 `*buf` 에 씀. 반환 0. 길이가 8이 아니면 `0xc0000206`. 값은 아무거나 |
| 2 | `GetSeedFromKernelState +0x77/+0x7e` → kernelbase `DeviceIoControl` → (`+0x6c/+0x73`) | **`NtDeviceIoControlFile`** | 핸들 = 위 `NtOpenFile` 이 준 값. 이벤트·APC 없음, IOSB 지역. **코드 `0x390008`**. 입력 NULL / 0. **출력 = 시드 버퍼 0x30바이트 (길이 0x30)** | `status ≥ 0` 이면 TRUE (그리고 `lpBytesReturned = IOSB.Information` 를 쓰지만 **아무도 안 읽음**). `status < 0` → `BaseSetLastNTError`, FALSE → 재시도. status `0x103` 이면 `NtWaitForSingleObject(핸들)` 으로 감 (동기 핸들이라 보통 안 옴) |
| 3 | `InitUmRootRngState +0x9a/+0x9f` `AesRNGState_init` | 없음 | `InitializeCriticalSection` (`+0x34/+0x3b`), `SymCryptRngAesFips140_2Instantiate` (`+0x4b/+0x50`) | 시드 **0x30바이트를 그대로 DRBG 씨앗으로**. 내용 검사 없음. `SymCryptRngAesInstantiate +0x18 CMP R8,0x30 JC` 로 길이만 봄. 틀리면 `0xc00000e5` → FALSE (재시도 없음) |
| 4 | `MsProviderStart +0x14f/+0x154` `IsInLSAProcess` | **레지스트리 syscall** (kernelbase `RegOpenKeyExW` 안, 이번엔 안 읽음) | `RegOpenKeyExW(HKLM, L"System\CurrentControlset\Control\Lsa", 0, KEY_QUERY_VALUE)` (`+0x78/+0x7f`), `RegQueryValueExW(hKey, L"LsaPid", …, &type, &dword, &4)` (`+0xb0/+0xb7`), `GetCurrentProcessId` (`+0xc0/+0xc7`), `RegCloseKey` (`+0xd8/+0xdf`) | **어떤 답이든 TRUE 에 영향 없음.** 키 열기 실패(`+0x86 JNZ +0xe4`) / 값 읽기 실패(`+0xbe JNZ +0xd3`) / `LsaPid ≠ 내 PID` 면 전부 "lsass 아님"(0). 결과는 `+0xe4` 에서 전역 `0x1800a3ad0` (초기 `-1`) 에 캐시 |

- **열기 뒤 `NtAllocateVirtualMemory`·`NtClose` × 2 가 무엇인가: bcryptprimitives attach 코드에는 이걸 부르는 곳이 없다.** `[정적]`
  - attach 지도(`entry_edges.tsv`)에서 `CloseHandle` 은 `GetCngHandle +0xd5/+0xdc` (CMPXCHG 에 진 쪽) 과 `TearDownRNGStates +0x4d/+0x54` (캐시된 핸들 닫기) 둘뿐. 첫째는 두 스레드가 동시에 열 때만, 둘째는 `MsProviderStart +0x35a` (InitUm 성공 + 나중에 실패, lsass 에서만) 와 `MsProviderStop`(DETACH) 에서만.
  - `VirtualAlloc`·`VirtualProtect`·`VirtualQuery` 는 임포트에 있지만 attach 지도에 호출 줄이 없음.
  - 그래서 로그의 `NtAllocateVirtualMemory → NtClose × 2 (→ NtUnmapViewOfSection)` 는 **`DllMain` 이 FALSE 로 돌아온 뒤 ntdll 로더의 정리** 로 보인다 `[추측]`. ntdll 의 해당 길은 이번에 안 읽음 (`LdrpInitializeNode +0x293/+0x298 LdrpLogInternal` 뒤 `R14D = 0xc0000142` 까지만 봄). `[공백]`
- **TRUE 가 되려면 답이 무엇이어야 하나:**
  - `NtOpenFile`: status ≥ 0 **이고 핸들 칸 ≠ 0.** (status 만으로는 안 됨.)
  - `NtDeviceIoControlFile`: **성공 status 만으로 된다.** 출력 길이·값·IOSB 를 안 본다. 출력 칸을 안 써도 (0x30바이트 0 그대로) 시드가 영이 될 뿐 코드는 통과한다 `[정적]`. 임의 48바이트 (난수) 를 쓰는 게 안전하다.
- 지금 우리: **`NtDeviceIoControlFile` 은 로그에 안 나왔으니 확인된 것 없음.** 핸들이 비어서 그 앞에서 막힌 것이 맞음 (요청서 `[추측]` → 코드로 확인됨 `[정적]`).

### 3. `NtQueryInformationProcess` class `0x56`

- 호출자: `IsEnclave +0x44/+0x4b`. 인자 만들기: `R9D = 0xb0` (길이), `EDX = R9D - 0x5a = 0x56` (class, `IsEnclave +0x40`), `RCX = -1` (현재 프로세스), `R8 = rsp+0x30` (버퍼, 호출 전에 `memset 0xb0`: `+0x26/+0x2b`), 반환길이 `[rsp+0x20] = NULL`. → **class `0x56`, 길이 `0xb0` 맞음.** `[정적]`
- 이름: **PDB 에 `PROCESSINFOCLASS` 열거형 없음.** (`llvm-pdbutil` 로 ntoskrnl·ntdll 둘 다 찾아봄, 0건. bcryptprimitives PDB 에도 없음.) `ProcessEnclaveInformation` (86) 이라는 건 공개 헤더(phnt) 기억 `[추측]`: 앞뒤 번호 `0x55 = ProcessTelemetryCoverage`, `0x57 = ProcessEnableReadWriteVmLogging` 이 커널 코드(`EtwQueryProcessTelemetryCoverage`, 플래그 비트 읽기)와 맞아서 신뢰는 높지만 이 PDB 로 증명된 이름은 아님.
- **attach 가 결과를 쓰나: 쓴다.** `IsEnclave +0x52 JNS +0x6f` 이후 `buf[0]` (첫 DWORD) 를 읽음:

  | NtQIP 결과 | `IsEnclave` 반환 | `g_TrustedEnvironment` |
  |---|---|---|
  | status < 0 (`+0x52 JNS` 안 탐, `+0x54 XOR EAX,EAX`) | 0 → `IsTrustlet` 로 감 (class 0 결과: 아래) | `IsTrustlet` 가 0 → **1** / 1 → **4** |
  | 성공 + `buf[0] ∈ {0x10, 0x11}` (`+0x78 LEA ECX,[RDX-0x10]; CMP ECX,1; +0x7d JBE` → 떨어진 범위 `+0x3db74 MOV EAX,0x10`) | `0x10` | `0x10` (VBS 엔클레이브) |
  | 성공 + `buf[0] == 1` (`+0x83 CMP EDX,1; CMOVZ`) | `8` | `8` (SGX 엔클레이브) |
  | 성공 + 그 밖 (0 포함) | `1` | **1 (일반)** |

  - `IsTrustlet` class 0 (`ProcessBasicInformation` 확장형, 길이 `0x40`, 호출 전에 `buf[0] = 0x40`): 성공이면 `buf+0x38` (코드 `[rsp+0x68]`) 의 **bit 7** (`SHR 7; AND 1`, `IsSecureProcess`) 을 반환. 실패면 0. → bit 7 이 켜지면 `g_TrustedEnvironment = 4` (트러스트렛) → IUM 갈래로 가서 `GetProcAddress(NULL, …)` / `LoadLibraryW(L"iumbase.dll")` 쪽 → 실패. **그러니 class 0 응답에서 `+0x38` 칸의 bit 7 은 0 이어야 함.**
  - 환경값 `1` 이 되면 지금까지 본 일반 갈래. `8` 이면 시드 함수가 `GetSeedFromRdrand` 로 바뀌고 (`InitUmRootRngState +0xd8`) PE 머리 검사를 건너뛰며 CPU 기능 검사 (FALSE 갈래 #3) 가 켜짐. `0x10` / `4` 면 IUM 갈래.
- **실패·0 이면 어디로 가나:** 실패 → `IsTrustlet`(class 0) 한 번 더 → 보통 `1`. 성공+0 → 바로 `1`. **어느 쪽이든 `1`.**
- **진짜 Windows 26100.9457 은 class `0x56` 에 실패를 돌려준다** `[정적]`:
  - `ntoskrnl!NtQueryInformationProcess +0x2d5 CMP EDI,0x73; +0x2d8 JA +0x42fe`. 그 밑 `+0x2e5` 의 바이트 표(rva `0x9bf75c`) 로 class 를 번호로 바꾸고 `+0x2ed` 의 4바이트 표(rva `0x9bf638`) 에서 목적지를 꺼내 `+0x2f7 JMP RDX`. 표를 PE 바이트에서 직접 읽으면 class `0x56` → 번호 72 → 목적지 rva `0x9bf5de` = `+0x42fe` = **`MOV EAX,0xc0000003`** (`STATUS_INVALID_INFO_CLASS`). class 5 와 같은 목적지. (`0x55 → 0x9bf380`, `0x57 → 0x9bd25d` 는 각자 case 가 있음.)
  - 그래서 진짜 Windows 에서 attach 의 NtQIP 는 **2번**: `0x56 → 0xc0000003`, 그다음 class 0 (길이 0x40) → 성공 + bit 7 꺼짐 → `g_TrustedEnvironment = 1`. 에뮬레이터 로그는 1번뿐이니 **에뮬레이터는 class 0x56 에 성공을 준 것** (영 버퍼 → `IsEnclave` 가 1 반환). 결과는 같으나 syscall 수는 다름.
- 지금 우리: **길이·class 값은 맞고, 결과도 맞는 환경값(1)이 나옴.** 진짜와 다른 점 = 0x56 을 성공으로 답함 (진짜는 `0xc0000003`). 막힘의 원인은 아님.

### 4. (작게) `NtTraceControl` × 3

- 부르는 코드 (이름만) `[정적]`:
  1. `entry +0xc8/+0xcd` `WppInitUm +0x6e/+0x75` → ntdll **`EtwRegisterTraceGuidsW`** (WPP 등록, 콜백 `0x18005f1e0`)
  2. `MsProviderStart +0x64/+0x69` → `TraceLoggingRegisterEx_EventRegister_EventSetInformation +0x55/+0x5c` → **`EventRegister`** (ntdll `EtwEventRegister`)
  3. 같은 함수 `+0x7c/+0x83` → **`EventSetInformation`** (ntdll `EtwEventSetInformation`)
  - ntdll 쪽에서 `NtTraceControl` 을 부르는 줄 (`_calls.tsv`): `EtwpRegisterProvider +0x14d/+0x152`, `EtwpSetProviderTraits +0xdd/+0xe2`, `EtwpTrackProviderBinary +0x6b/+0x70`, `EtwpRegisterTpNotificationOnce +0x8c/+0x91` (프로세스 첫 등록 때 한 번). 위 3개 호출이 각각 어느 줄로 이어지는지는 **이번에 안 따라감** (run51·run62 의 `EtwEventRegister → NtTraceControl` 길 참조). 3 이라는 숫자의 정확한 구성은 `[추측]`.
- attach 결과에 닿나: **안 닿는다.**
  - `WppInitUm` 반환: `entry +0xc8/+0xcd` 바로 다음 `call MsProviderStart` — `EAX` 를 안 봄.
  - `TraceLoggingRegisterEx_…` 반환: `MsProviderStart +0x69` 에서 `MOV EAX,[g_TrustedEnvironment]` 로 덮음.
  - 유일한 위험: `TraceLoggingRegisterEx_… +0x15 CMP [0x1800a3458],0; +0x2e JNZ +0xab → INT 0x29` — 이미 등록된 상태일 때만 크래시. 초기값 0 이고 첫 attach 한 번뿐이라 걸리지 않음. `EventRegister` 가 `0x1800a3458` 에 핸들을 쓰는데, 같은 DLL 을 두 번 attach 하는 일은 없음.
- 지금 우리: **맞음** (3개를 받아 주기만 하면 됨. 반환 무시).

### 5. 최소 조건 표 — attach 가 TRUE 로 끝나려면 바깥이 줘야 하는 답

(일반 프로세스: `lsass` 아님, `g_TrustedEnvironment = 1`)

| # | syscall (또는 읽는 공유 칸) | 입력 | 기대 답 | 안 맞으면 가는 자리 |
|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | `(-1, 0x56, buf, 0xb0, NULL)` | 아무 status. 성공이면 `buf[0]` 는 `0` (또는 1·0x10·0x11 이 **아닌** 값). 진짜는 `0xc0000003` | `buf[0] = 1` → 환경 `8` (SGX): 시드 함수 `GetSeedFromRdrand`, CPU 검사. `0x10/0x11` → 환경 `0x10` (IUM 갈래 → `LoadLibraryW(iumbase.dll)` 실패 → `0xc00000e5` → FALSE) |
| 2 | `NtQueryInformationProcess` (1 이 실패일 때만) | `(-1, 0, buf, 0x40, NULL)`, `buf[0]=0x40` | 성공 + `buf+0x38` 의 **bit 7 = 0**, 또는 실패(→ 같은 결과) | bit 7 = 1 → 환경 `4` (트러스트렛) → IUM 갈래 → FALSE |
| 3 | `NtTraceControl` × 3~ | (ETW 등록) | 아무 status | 영향 없음 |
| 4 | **`NtOpenFile`** | 이름 `\Device\CNG`, 접근 `0x100001`, 공유 `7`, 옵션 `0x20`, 속성 `0x40`, 루트 0 | **status ≥ 0 + `*FileHandle ≠ 0`** (그리고 같은 값을 이후 `NtDeviceIoControlFile` 이 받음) | status < 0 또는 핸들 0 → `GetCngHandle` 이 NULL → 시드 함수 FALSE → **5번 재시도** → `InitUmRootRngState` 0 → `MsProviderStart = 0xc00000e5` → `entry +0xd4 JS` → `SetLastError(0x54f)` → **FALSE** |
| 5 | (ntdll 코드가 읽음) `KUSER_SHARED_DATA` `0x7ffe0250` (8바이트) | `RtlGetSystemGlobalData(1, buf, 8)` | **페이지가 읽혀야 함.** 값은 아무거나 | 페이지 없음 → 접근 위반. (`RtlGetSystemGlobalData` 가 음수를 주면 `GetSeedFromKernelState +0x4a INT 0x2c` 크래시. ID 1 + 길이 8 이면 음수 경로 없음) |
| 6 | **`NtDeviceIoControlFile`** | 핸들 = 4 의 값, 코드 `0x390008`, 입력 NULL/0, 출력 버퍼 `0x30`바이트 | **status ≥ 0.** 출력: 임의 48바이트 권장 (코드는 영이어도 통과). IOSB.Information 은 아무도 안 읽음 | status < 0 → FALSE 반환 → 재시도 5번 (이번엔 `NtOpenFile` 없이 이 호출만 5번) → 같은 FALSE 길. `0x103` → `NtWaitForSingleObject(핸들)` 로 갔다 옴 (피할 것) |
| 7 | 레지스트리 (`RegOpenKeyExW`/`RegQueryValueExW`/`RegCloseKey` → `NtOpenKey*`/`NtQueryValueKey`/`NtClose`, 이번엔 kernelbase 안 안 읽음) | `HKLM\System\CurrentControlset\Control\Lsa`, `LsaPid` | **아무 답.** 실패든 성공이든 내 PID 와 같지만 않으면 TRUE | `LsaPid == 내 PID` 면 lsass 로 보고 `AlgorithmCheck` 등 (Q1 i) 으로 감 — 에뮬레이터에선 일어날 일 없음 |
| 8 | PE 머리 (메모리 안) | `[base+0x3c]`, `[base+e_lfanew+0x5e]` | `DllCharacteristics & 0x80 ≠ 0` (파일 값 `0x41e0`) | 머리가 안 올라갔거나 비트가 꺼져 있으면 `entry +0x49 JZ +0x1cf` → FALSE (`SetLastError` 없음) |
| 9 | CPUID / XState | `GetEnabledXStateFeatures` (공유 페이지) | 아무거나 (소프트웨어 대체 경로 있음) | 영향 없음 (일반은 CPU 검사 FALSE 갈래 안 감) |

- 이 표를 다 맞추면 attach 는 `+0x128 MOV EAX,1` 로 끝난다. 이때 남는 전역: `g_hCNGDriver ≠ 0` (열린 핸들 1개), `g_rngState = 1`, `g_fProviderStarted = 1`, `g_rootAesRngState` 채워짐, `g_TrustedEnvironment = 1`.
- 일반 프로세스에서 attach 가 부르는 syscall 전체(진짜 Windows 기준, 예상): `NtQueryInformationProcess`(0x56) ×1, `NtQueryInformationProcess`(0) ×1, `NtTraceControl` ×3~, `NtOpenFile` ×1, `NtDeviceIoControlFile` ×1, 레지스트리 ×(열기·읽기·닫기). **`NtAllocateVirtualMemory`·`NtClose` 는 attach 코드 몫이 아님.** (단 kernelbase·ntdll 안 간접 호출은 이번에 안 읽음 `[공백]`)

---

## 동적분석으로 확인해야 할 것

실행 때만 정해지는 것. 칸만 적음 (`[추측]` 표시는 값 짐작).

- **진짜 `NtQueryInformationProcess(-1, 0x56)` 의 status**: 정적으론 `0xc0000003`. 그리고 이어서 class 0 호출이 따라오는지. (`IsEnclave +0x4b` 직후 `EAX`, `IsTrustlet +0x3b` 직후)
- **진짜 `NtDeviceIoControlFile` 의 `IoStatus.Information`**: 안 쓰이지만 에뮬레이터를 진짜와 맞추고 싶다면 `0x30` 이 나오는지 `[추측]`. 출력 48바이트는 난수 모양일 것.
- **진짜 `NtOpenFile` 이 돌려주는 핸들 값 모양** (`[RBP+0x10]` = `GetCngHandle +0x9b` 직후 `[rbp+0x10]`): 4의 배수 작은 수 `[추측]`.
- **`KUSER_SHARED_DATA+0x250` (`RNGSeedVersion`) 의 실제 값**: 상관은 없으나 `AesRNGState` 쪽 재시드 판정에 쓰이는 칸 (이번 범위 밖).
- **`LsaPid` 레지스트리 값 / `RegOpenKeyExW` 의 실제 status** (test.exe 에서): 정적으론 "내 PID 가 아니면 상관없음".
- **`NtTraceControl` 3개의 실제 인자·개수**: 위 Q4 의 3개가 각각 `NtTraceControl` 몇 번이 되는지. 첫 등록이라 `EtwpRegisterTpNotificationOnce` 의 `NtCreateEvent` 등이 더 붙을 수 있음.
- **attach 직후 `NtAllocateVirtualMemory`·`NtClose` × 2 의 정체**: FALSE 가 아닌 TRUE 로 끝나는 진짜 실행에서도 나오는지 (나오면 attach 몫 아님이 확인됨).
- 추천 멈춤점 (모듈 이름은 WinDbg 에서 `bcryptPrimitives`):
  - `bcryptPrimitives!entry +0xd2` (= `MsProviderStart` 반환 직후): `eax` 가 0 이면 성공 길, `0xc00000e5` 면 시드 길 실패.
  - `bcryptPrimitives!GetCngHandle +0x9b` (= `NtOpenFile` 반환 직후): `eax` (status) 와 `[rbp+0x10]` (핸들 칸) 둘 다.
  - `bcryptPrimitives!GetSeedFromKernelState +0x7e` (= `DeviceIoControl` 반환 직후): `eax` (BOOL), 시드 버퍼 `rsi`.
  - `bcryptPrimitives!IsEnclave +0x4b`: `eax` (status), `[rsp+0x30]` 첫 DWORD.
  - 조건: 모두 `test.exe` 일 때만 (`.process /p` 또는 이름 비교) — 이 DLL 은 모든 프로세스가 올린다.

## 다음 요청 후보

1. **(정적, 작게)** ntdll: `DllMain` FALSE 뒤 정리 길 — `LdrpInitializeNode +0x298` (`0xc0000142`) 이후 `NtAllocateVirtualMemory`·`NtClose` ×2 가 어디서 나오나. 이번 결과대로 고치면 TRUE 로 가서 필요 없을 수 있으니 **후순위** (고친 뒤에도 같은 자리에서 막히면 그때).
2. **(정적)** kernelbase `RegOpenKeyExW` / `RegQueryValueExW` / `RegCloseKey` → 어떤 syscall (`NtOpenKey*`, `NtQueryValueKey`, `NtClose`) 이 나가는지. `IsInLSAProcess` 가 부르는 것이라 attach 가 TRUE 가 된 **다음에** 막힘이 여기서 나올 때.
3. **(정적)** 같은 DLL 의 attach 뒤 첫 사용 길: `ProcessPrng` (rva `0x2df90`) → `AesRNGState_select` → `AesRNGState_generate`. `g_rngState = 1` 이후 첫 호출이 부르는 것(스레드별 상태·재시드·`RtlGetSystemGlobalData` 비교)을 보는 요청 — 범위 밖이라 이번에 안 봄.
4. **(동적)** 위 추천 멈춤점 4곳. 진짜 Windows 에서 NtQIP 가 2번인지, `\Device\CNG` 핸들 모양, IOCTL 결과를 한 번에 확인.
5. **(정적, 후순위)** `cng.sys` 의 `0x390008` 처리. 에뮬레이터 안에서는 "난수 48바이트 채워 주기" 로 충분하므로 필요 없음. (IOCTL 코드는 장치종류 `0x39` · 함수 `2` · 버퍼 방식 · 접근 제한 없음으로 풀림 `[정적]`. 공개 자료의 `IOCTL_KSEC_RANDOM_FILL_BUFFER` 와 같은 값으로 기억 `[추측]`.)

## 근거 파일

- `ms/disassemble/bcryptprimitives/entry.asm` (`+0x25~+0x49`, `+0xc8~+0xd4`, `+0xfc~+0x128`, `+0x199~+0x1b9`, `+0x1be~+0x1d1`, `+0x20a~+0x228`)
- `ms/disassemble/bcryptprimitives/MsProviderStart.asm`, `GetTrustedEnvironment.asm`, `IsEnclave.asm`, `IsTrustlet.asm`
- `ms/disassemble/bcryptprimitives/InitUmRootRngState.asm`, `GetSeedFromKernelState.asm`, `GetCngHandle.asm`, `AesRNGState_init.asm`, `SymCryptRngAesFips140_2Instantiate.asm`, `SymCryptRngAesInstantiate.asm`
- `ms/disassemble/bcryptprimitives/IsInLSAProcess.asm`, `GetDriverSelftestCycleCount.asm` (IOCTL `0x390084`, 출력 8바이트, lsass 일 때만), `TearDownRNGStates.asm`, `DebugTraceError.asm`, `WppInitUm.asm`, `TraceLoggingRegisterEx_EventRegister_EventSetInformation.asm`
- 지도: `ms/disassemble/bcryptprimitives/_map/entry_edges.tsv`, `entry_nodes.tsv`, `entry_summary.txt`. 호출 줄: `ms/disassemble/bcryptprimitives/_calls.tsv`
- `ms/disassemble/ntdll/RtlGetSystemGlobalData.asm`, `ms/disassemble/kernelbase/DeviceIoControl.asm`, `ms/disassemble/ntoskrnl/NtQueryInformationProcess.asm` (`+0x2d5~+0x2f7`, `+0x42fe`)
- `.rdata` 문자열은 PE 바이트에서 직접 읽음: `0x8d0b8` `\Device\CNG`, `0x8d1d0` `System\CurrentControlset\Control\Lsa`, `0x8d220` `LsaPid`, `0x89978` `GetSeedFromIumKernelState`, `0x8d540` `vertdll.dll`, `0x8d558` `iumbase.dll`.
- 임포트 표 (PE 바이트): ntdll 에서 직접 `NtOpenFile`, `NtQueryInformationProcess`, `RtlGetSystemGlobalData`, `EtwRegisterTraceGuidsW`, `EtwTraceMessage` 등. `DeviceIoControl`·`CloseHandle` 은 api-set (`api-ms-win-core-io-l1-1-0`, `…handle-l1-1-0`) → kernelbase.

## 판정

**분석성공.** 요청서 5개 질문 모두 `.asm` 줄로 답함. 막힘의 원인(`NtOpenFile` 이 status 0 + 핸들 칸 0)이 코드로 확정됐고 로그의 syscall 차례·인자·횟수와 어긋남 없음. 못 닫은 것은 `NtAllocateVirtualMemory`·`NtClose` × 2 의 정체(attach 몫이 아니라는 데까지만)와 `NtTraceControl` 3개의 정확한 구성뿐 — 둘 다 결과(TRUE/FALSE)에 영향 없음.
