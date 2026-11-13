# 2026-11-13 run91 — bcryptprimitives 시작 함수 (`DllMain`, Ghidra `entry`) 의 attach 길과 TRUE 최소 조건 (Q169, PE_emulator 요청 31 — 한 바퀴로 끝): **샘플 1183개가 막힌 까닭은 `NtOpenFile(\Device\CNG)` 의 "status 0 인데 핸들 칸을 안 씀" 하나다. `GetCngHandle` 은 status 만 보고 성공으로 치지만 돌려주는 값은 핸들 칸 (`[RBP+0x10]`, 처음 0) 이라, 0 이면 "못 열었다" 가 되고 캐시 `g_hCNGDriver` 도 0 그대로라 다음 시도에서 또 연다. 시드 함수를 5번 불러 보고 포기 → `InitUmRootRngState` 0 → `MsProviderStart` `0xc00000e5` → `DllMain` 이 `SetLastError(0x54f)` 하고 FALSE. TRUE 가 되려면 바깥은 둘만 맞추면 된다 — `NtOpenFile` 이 status ≥ 0 과 함께 0 아닌 핸들을 쓰고, 그 핸들로 `NtDeviceIoControlFile(0x390008, 입력 없음, 출력 0x30B)` 이 status ≥ 0 (출력 내용은 안 봄). `NtQueryInformationProcess` · `NtTraceControl` · 레지스트리는 무엇을 돌려줘도 된다 (class 0 답의 `+0x38` bit 7 만 꺼져 있으면). 26100.9457 커널은 class `0x56` 을 구현하지 않아 (`0xc0000003`) 진짜 Windows 는 attach 에서 NtQIP 를 두 번 부른다. test.exe 는 이 DLL 을 안 올린다 (사진 모듈 6개 `[관찰]` run44).** `DllMain +0x15f GetTrustedEnvironment` (`+0x4 IsEnclave +0x44 NtQueryInformationProcess(0x56)` · `+0xd IsTrustlet +0x34 NtQueryInformationProcess(0)`) · `+0x49` PE 머리 검사 · `+0xc8 WppInitUm +0x6e EtwRegisterTraceGuidsW` · `+0xcd MsProviderStart` (`+0x64 TraceLoggingRegisterEx… +0x55 EventRegister · +0x7c EventSetInformation` · `+0x13b InitUmRootRngState +0x7a` (CFG 간접) `GetSeedFromKernelState` (`+0x1e GetCngHandle +0x94 NtOpenFile` · `+0x3a RtlGetSystemGlobalData` · `+0x77 DeviceIoControl` → kernelbase `+0x6c NtDeviceIoControlFile`), 실패면 `+0x129 ~ +0x137` 다시 (5번) · `+0x14f IsInLSAProcess`) → `+0xd4 JS +0x1be` → `+0x1c3 SetLastError(0x54f)` → `+0x1cf` FALSE / `+0x128` TRUE

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### 요청 31 결론 (run91)

- **막힘은 `NtOpenFile` 의 핸들 칸 하나** `[정적]`.
  - `GetCngHandle +0xa0 TEST EAX,EAX` · `+0xa2 JS` 는 status 만 본다. status 0 이면 `+0xa4` 에서 핸들 칸 `[RBP+0x10]` 을 꺼내 `+0xaa LOCK CMPXCHG` 로 캐시 `g_hCNGDriver` (`0x1800a42b0`) 에 넣고, `+0xb8` 에서 **그 핸들 칸 값을 돌려준다**.
  - 엔진이 칸을 안 쓰면 칸은 `+0x25` 에서 넣어 둔 캐시 값 (= 0) 그대로다. 그래서 0 을 0 위에 쓰고 (캐시 빈 채) NULL 을 돌려준다.
  - `GetSeedFromKernelState +0x29 JZ +0xa0` → FALSE → `InitUmRootRngState +0x83 JZ +0x129` → `EBX++` 해서 5번째 실패면 `+0x12e JNC +0xbd` 로 0.
  - 로그의 `NtOpenFile` × 5 = "캐시가 끝까지 0" = 핸들이 한 번도 안 채워졌다는 뜻이다. 코드로 설명되는 길은 이것뿐이다.
- **TRUE 최소 조건 (일반 프로세스 — lsass 아님)** `[정적]`:
  1. `NtOpenFile(\Device\CNG)` → status ≥ 0 **이고 `*FileHandle` ≠ 0**.
  2. `NtDeviceIoControlFile(그 핸들, 0x390008, 입력 NULL/0, 출력 0x30B)` → status ≥ 0. 출력 내용 · 길이 · `IoStatus.Information` 은 아무도 안 본다 (0 으로 둬도 통과, 난수 48B 권장).
  3. 나머지 (`NtQueryInformationProcess` 둘 · `NtTraceControl` · 레지스트리 · `RtlGetSystemGlobalData` 의 KUSD 읽기) 는 아무 답이나 된다. 단 class 0 답의 `+0x38` bit 7 은 0, KUSD 쪽은 읽혀야 한다.
- **5번 되풀이는 이름 5개가 아니다** — 같은 `\Device\CNG` 를 시드 함수 실패마다 다시 연다. 캐시가 차면 그 뒤 시도는 `NtOpenFile` 없이 `NtDeviceIoControlFile` 만 다시 한다 (`GetCngHandle +0x34 JNZ +0xbc`).
- **class `0x56`**: 26100.9457 커널은 이 class 를 구현하지 않는다 — ntoskrnl `NtQueryInformationProcess +0x2e5` 바이트 표에서 `0x56` → 번호 72 → 목적지 `+0x42fe MOV EAX,0xc0000003` (class 5 와 같은 기본 갈래) `[정적]` (flow-writer 가 PE 바이트로 다시 읽음). 그래서 진짜는 `IsEnclave` 실패 → `IsTrustlet` (class 0, 길이 `0x40`) 로 **두 번**. 지금 엔진은 `0x56` 에 성공 + 0 칸을 줘서 한 번으로 끝난다 — 결과 (`g_TrustedEnvironment = 1`) 는 같다.
- **`NtAllocateVirtualMemory` · `NtClose` × 2 는 attach 코드 몫이 아니다** `[정적]` — attach 지도에서 `CloseHandle` 은 `GetCngHandle +0xd5` (두 스레드가 동시에 열 때) 와 `TearDownRNGStates +0x4d` (lsass · DETACH 길) 둘뿐이고, `Virtual*` 호출 줄은 없다. FALSE 뒤 ntdll 로더의 정리로 보인다 `[추측]` → Q170 ① (후순위).
- **attach 가 TRUE 로 끝나면 남는 것**: `g_hCNGDriver` ≠ 0 (열린 핸들 1개), `g_rngState` (`0x1800a42a8`) = 1, `g_fProviderStarted` = 1, `g_rootAesRngState` 채워짐, `g_TrustedEnvironment` = 1 `[정적]` (결과서).

### 원문 질문이 어디서 답해졌나

| 원문 요청 31 | 답한 곳 | 한 줄 · 지금 PE_emulator |
|---|---|---|
| 1 여는 객체 이름 · 5번 조건 · FALSE 자리 | 아래 "attach 차례 표" · "FALSE 로 가는 자리" | 이름 **`\Device\CNG`** (`.rdata` rva `0x8d0b8`, `\Device\KsecDD` 아님). 접근 `0x100001` · 공유 `7` · 옵션 `0x20` · 속성 `0x40` · 루트 0 — **로그 값과 같음**. 5번 = 같은 이름, 시드 함수 BOOL 이 0 일 때. FALSE 자리 셋 (일반 프로세스는 ② 하나만). 지금: **값은 맞음, 핸들 칸 비움이 틀림** |
| 2 연 뒤 syscall · IOCTL · 출력 칸 · TRUE 조건 | 아래 "최소 조건 표" 4 ~ 6 | `NtDeviceIoControlFile` 코드 `0x390008`, 입력 없음, 출력 시드 `0x30B`. **성공 status 만으로 됨** (출력 안 봄). 지금: 로그에 안 나옴 — 핸들이 비어 그 앞에서 갈림 (요청서 `[추측]` → `[정적]`) |
| 3 class `0x56` 결과를 쓰나 | 아래 "attach 차례 표" 2 · 3 | 쓴다 — 첫 DWORD 로 환경값 (`0x10`/`0x11` → `0x10`, `1` → `8`, 그 밖 → `1`). 실패면 class 0 으로 한 번 더 → 보통 `1`. 지금: **결과 맞음** (진짜는 `0xc0000003`, syscall 수만 다름) |
| (요청서 4) `NtTraceControl` × 3 | 아래 "결과서와 다르게 적은 곳 · 보탠 것" ⑧ | 부르는 곳 셋 (`EtwRegisterTraceGuidsW` · `EventRegister` · `EventSetInformation`), **반환값을 아무도 안 봄**. 3 의 정확한 구성은 `[추측]` → Q170. 지금: **맞음** (받아 주기만) |
| (요청서 5) 최소 조건 표 | 아래 "최소 조건 표" | — |

### attach 차례 표 (일반 프로세스, 결과서 1-a 를 줄임 — 근거 줄은 flow-writer 가 `.asm` 으로 다시 봄)

| # | 자리 (call/ret) | 부르는 것 · 하는 일 | syscall · 갈림 |
|---|---|---|---|
| 1 | `DllMain +0x1c / +0x1f` | `CMP EDX,1 · JNZ +0x13e` — 이유 1 만 아래로 | — |
| 2 | `+0x25 ~ +0x2d` → `+0x15f/+0x164` | `g_TrustedEnvironment` (`0x1800a42c0`) 가 0 이면 `GetTrustedEnvironment` → `+0x4/+0x9 IsEnclave` | `IsEnclave +0x44/+0x4b` **`NtQueryInformationProcess(-1, 0x56, buf, 0xb0, NULL)`** (버퍼 `+0x26 memset 0xb0`, class = `+0x40 LEA EDX,[R9-0x5a]`) |
| 3 | `GetTrustedEnvironment +0xd/+0x12` | `IsTrustlet` (2 가 0 일 때만) → `+0x12 ~ +0x19` 0 → 1 / 1 → 4 → `+0x1b` 저장 | `IsTrustlet +0x34/+0x3b` **`NtQueryInformationProcess(-1, 0, buf, 0x40, NULL)`** (`buf[0] = 0x40`), 성공이면 `buf+0x38` bit 7 (`+0x44 ~ +0x4b`) |
| 4 | `+0x33 ~ +0x49` | 환경 bit 8 이 없으면 **제 PE 머리**: `[이미지 + e_lfanew + 0x5e] & 0x80` (`DllCharacteristics` `FORCE_INTEGRITY`, 파일 값 `0x41e0` — 켜짐) | **`+0x49 JZ +0x1cf` = FALSE ①** |
| 5 | `+0x52/+0x59` · `+0x69/+0x6e` · `+0x6e/+0x73` | `DisableThreadLibraryCalls` · `_DllMainCRTStartupForGS2` (`/GS` 쿠키) · `SymCryptInitEnvWindowsUsermodeWin8_1nLater` (`+0xe SymCryptDetectCpuFeaturesByCpuid` · `+0x13 GetEnabledXStateFeatures`) | 결과서: syscall 없음 (kernelbase → ntdll 안은 안 읽음). 결과 안 봄 |
| 6 | `+0x73 ~ +0x88` | 환경 다시 읽음. bit 8 (엔클레이브) 이면 `+0x199` CPU 기능 검사 | 일반은 안 감 (FALSE ③) |
| 7 | `+0x8e/+0x93` · `+0xa1 ~ +0xbd` | `SafeAllocaInitialize` (`+0x11 RtlImageNtHeader`) · WPP 칸 (`WPP_GLOBAL_Control` ← `&WPP_MAIN_CB`) | — |
| 8 | `+0xc8/+0xcd` | `WppInitUm` → `+0x6e/+0x75` **`EtwRegisterTraceGuidsW`** | `NtTraceControl` 후보. 반환 안 봄 (바로 다음이 call) |
| 9 | **`+0xcd/+0xd2`** | **`MsProviderStart`** (rva `0x436c4`) — 아래 표 | `+0xd2 TEST EAX,EAX · +0xd4 JS +0x1be` = **FALSE ②** |
| 10 | `+0xda ~ +0xe5` | WPP 플래그 `[WPP_MAIN_CB+0x1c] & 0x20` 이면 `+0x1d6` 할당 · `+0x1f9 GetModuleFileNameW` 하고 돌아옴 | ETW 세션이 켤 때만 (보탬 ④) |
| 11 | `+0xfc/+0x103` | `GetModuleHandleExW(5, &DllMain, &h)` | 실패해도 TRUE (보탬 ③) |
| 12 | `+0x128` → `+0x13c RET` | `MOV EAX,1` | **TRUE** |

`MsProviderStart` 안:

| # | 자리 (call/ret) | 하는 일 | 갈림 |
|---|---|---|---|
| a | `+0x2d ~ +0x62` | 환경값 (0 이면 `+0x4e GetTrustedEnvironment`). `+0x5f TEST CL,0x18` 이면 b 건너뜀 | — |
| b | `+0x64/+0x69` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` → `+0x55/+0x5c EventRegister` · `+0x7c/+0x83 EventSetInformation` | `NtTraceControl` 후보. `+0x69` 에서 `EAX` 를 덮어 반환 버림 |
| c | `+0x84 / +0x87` | `환경 & 0x16 == 0` → `JZ +0x10e` (일반). 아니면 IUM 갈래 (`+0xaa GetModuleHandleExW` / `+0xce LoadLibraryW`) | 일반은 안 감 |
| d | `+0x10e ~ +0x135` | `g_fSteEnabled` · `g_fSelftest` · `g_fSelftestsRunning` = 1 → 셀프테스트 셋 (`+0x126` · `+0x12b` · `+0x130`, 계산만) → `+0x135` 끔 | 틀리면 `SymCryptFatal` (결과서) |
| e | **`+0x13b/+0x140`** | **`InitUmRootRngState`** (rva `0x2b5dc`) — 아래 | 0 이면 `+0x142 JNZ` 안 탐 → `+0x144 R9D=0xa1` → `+0x14a JMP +0xec` → `EBX = 0xc00000e5` → `+0x104 DebugTraceError` → `+0x109 JMP +0x34c` → 반환 `0xc00000e5` |
| f | `+0x14c` · `+0x14f/+0x154` | `SIL = 1` · **`IsInLSAProcess`** (레지스트리 `…\Control\Lsa` `LsaPid` = 내 PID 인가) | 0 이면 `+0x156 JZ +0x345` |
| g | `+0x345 ~ +0x35f` | `g_fProviderStarted ← SIL` (1) → `+0x353 JNZ +0x35f` → `EAX = EBX = 0` | **성공** |

`InitUmRootRngState` → `GetSeedFromKernelState` → `GetCngHandle`:

| # | 자리 | 하는 일 |
|---|---|---|
| a | `InitUmRootRngState +0x19 ~ +0x64` | 환경으로 시드 함수 고름. 일반 → `+0x5d LEA RAX,[GetSeedFromKernelState]` → `+0x64` `g_pGetSeedFunction` (`0x1800a42a0`) (bit 8 → `+0xd8 GetSeedFromRdrand`, `& 0x16` → `+0xf3 ~ +0x108 GetProcAddress(g_hIumBase, "GetSeedFromIumKernelState")`) |
| b | `+0x6b ~ +0x7a/+0x7f` | **CFG 간접 호출** `g_pGetSeedFunction(buf 0x30B, 0x30, &ver)` (`_calls.tsv` `indirect:cfg:GetSeedFromKernelState`) |
| c | `+0x81 / +0x83` | 0 이면 `JZ +0x129` → `+0x129 INC EBX · +0x12b CMP EBX,5 · +0x12e JNC +0xbd` (5번째면 반환 0) · `+0x130` 포인터 다시 · `+0x137 JMP +0x6b` |
| d | `+0x9a/+0x9f` | 성공이면 `AesRNGState_init(&g_rootAesRngState, buf, ver)`. `+0xa1 JS +0x13c` → 0 (재시도 없음) |
| e | `+0xa7 ~ +0xbd` | 시드 버퍼 0 으로 지움 · `g_rngState = 1` · 반환 1 |
| f | `GetSeedFromKernelState +0x1e/+0x23` | `GetCngHandle` → `+0x26 TEST · +0x29 JZ +0xa0` (→ `+0xa0 JMP +0x8f`) 반환 0 |
| g | `+0x2b ~ +0x3a/+0x41` | ntdll `RtlGetSystemGlobalData(1, &v, 8)` → `+0x48 JNS`, 아니면 `+0x4a INT 0x2c` (크래시) |
| h | `+0x4c ~ +0x77/+0x7e` | **`DeviceIoControl(h, 0x390008, NULL, 0, buf, 0x30, &n, NULL)`** (`+0x6a MOV EDX,0x390008`, 출력 길이 = 받은 `EDX` 0x30) |
| i | `+0x83 ~ +0x8c` | 0 이면 그대로 0. 아니면 `*ver = v` 하고 0 아닌 값 반환 |
| j | `GetCngHandle +0x10 ~ +0x34` | 캐시 `g_hCNGDriver` 읽음, 0 아니면 `+0x34 JNZ +0xbc` 바로 반환. `+0x25` 핸들 칸 `[RBP+0x10]` ← 그 값 (= 0) |
| k | `+0x3a ~ +0x8f` · `+0x45/+0x4c` | `RtlInitUnicodeString(L"\Device\CNG")` · OA (크기 `0x30`, 루트 0, 속성 `0x40`) · IOSB 0 |
| l | **`+0x94/+0x9b`** | **`NtOpenFile(&[RBP+0x10], 0x100001, &OA, &IOSB, 7, 0x20)`** (`+0x77` · `+0x87` · `+0x5c`) |
| m | `+0xa0 ~ +0xb8` | `+0xa2 JS +0xb8` (실패) / `+0xa4 ~ +0xb6` CMPXCHG (기대 0) → `+0xb8 MOV RBX,[RBP+0x10]` → **핸들 칸 값 반환** |
| n | `+0xd1 ~ +0xe1` | CMPXCHG 에 져서 옛값 ≠ 0 이면 내 핸들 `CloseHandle` (`+0xd5/+0xdc`) 하고 옛값 반환 |

### FALSE 로 가는 자리 (attach 전부, 결과서 1-e)

| # | 자리 | 조건 | 일반 프로세스 |
|---|---|---|---|
| ① | `DllMain +0x49 JZ +0x1cf` (`SetLastError` 없음) | 환경 bit 8 없음 **그리고** PE 머리 `DllCharacteristics & 0x80 == 0` | 파일 값 `0x41e0` 이라 안 감 — 단 엔진이 머리 페이지를 메모리에 올려 둬야 (`base+0x3c` · `base+e_lfanew+0x5e` 읽기) |
| ② | `+0xd4 JS +0x1be` → `+0x1be MOV ECX,0x54f` → `+0x1c3 SetLastError` → `+0x1cf XOR EAX,EAX` → `+0x1d1 JMP +0x12d` | `MsProviderStart` 음수 | **여기가 막힘** (`InitUmRootRngState` 0 → `0xc00000e5`). 다른 음수 길: IUM 갈래 `+0xe4` · lsass 갈래 `AlgorithmCheck` (`+0x28c`) — 일반은 안 감 |
| ③ | `+0x1a1 JNZ` / `+0x1b9 JMP` → 조각 `FUN_180081a96` (`MOV ECX,0x32` → `JMP DllMain+0x1c3`) → `SetLastError(0x32)` → `+0x1cf` | 엔클레이브 (bit 8) 일 때 CPU 기능 검사 | 안 감 |

- 크래시 (FALSE 아님) 자리 (결과서): `GetSeedFromKernelState +0x4a INT 0x2c` · `TraceLoggingRegisterEx… +0x2e JNZ +0xab → INT 0x29` (이미 등록된 상태 — 첫 attach 는 안 걸림) · `SymCryptFatal` (셀프테스트 실패).

### 최소 조건 표 (결과서 질문 5, 일반 프로세스 — `lsass` 아님, `g_TrustedEnvironment = 1`)

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 |
|---|---|---|---|---|
| 1 | `NtQueryInformationProcess` | `(-1, 0x56, buf, 0xb0, NULL)` | 아무 status. 성공이면 `buf[0]` 는 1 · `0x10` · `0x11` 이 **아닌** 값. 진짜는 `0xc0000003` | `1` → 환경 8 (SGX 갈래), `0x10`/`0x11` → IUM 갈래 → FALSE |
| 2 | `NtQueryInformationProcess` (1 이 실패일 때만) | `(-1, 0, buf, 0x40, NULL)`, `buf[0] = 0x40` | 성공 + `buf+0x38` bit 7 = 0, 또는 실패 | bit 7 = 1 → 환경 4 → IUM 갈래 → FALSE |
| 3 | `NtTraceControl` × 3 ~ | ETW 등록 | 아무 status | 영향 없음 |
| 4 | **`NtOpenFile`** | `\Device\CNG`, `0x100001`, 공유 7, 옵션 `0x20`, 속성 `0x40`, 루트 0 | **status ≥ 0 + `*FileHandle` ≠ 0** | 5번 재시도 → FALSE ② |
| 5 | (ntdll 코드가 읽음) KUSD `+0x250` `RNGSeedVersion` 8B | `RtlGetSystemGlobalData(1, buf, 8)` | 페이지가 읽혀야 함. 값은 아무거나 (run44 사진 `0xc`) | 페이지 없음 → 접근 위반 |
| 6 | **`NtDeviceIoControlFile`** | 4 의 핸들, `0x390008`, 입력 NULL/0, 출력 `0x30B` | **status ≥ 0**. 출력 임의 48B 권장 (안 봄) | 음수 → 재시도 (이땐 `NtOpenFile` 없이 이것만 5번) → FALSE ②. `0x103` → `NtWaitForSingleObject(핸들)` 갔다 옴 (피할 것) |
| 7 | 레지스트리 (`RegOpenKeyExW` · `RegQueryValueExW` · `RegCloseKey` → NT 함수 이름은 `[추측]`, Q170 ②) | `HKLM\System\CurrentControlset\Control\Lsa` · `LsaPid` | 아무 답 (내 PID 와 같지만 않으면) | 같으면 lsass 갈래 (`AlgorithmCheck` 등) |
| 8 | PE 머리 (메모리) | `[base+0x3c]` · `[base+e_lfanew+0x5e]` | `& 0x80` ≠ 0 | FALSE ① |
| 9 | CPUID · XState | `GetEnabledXStateFeatures` | 아무거나 | 영향 없음 |

### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 `NtOpenFile` 몸이 `\Device\CNG` 를 알아보고 **핸들 칸을 쓰는지**, 그 핸들을 `NtDeviceIoControlFile(0x390008)` 몸이 받아 48B 를 채우는지. 경로를 모를 때 "status 0 + 칸 비움" 은 진짜 커널에 없는 답이다 `[추측: 진짜 IopCreateFile 은 성공이면 늘 핸들을 씀 — 이번엔 안 읽음]`.
- KnownDll 섹션으로 올릴 때 PE 머리 페이지가 실제로 붙어 있는지 (FALSE ①).
- class `0x56` 을 `0xc0000003` 로 바꾸면 class 0 (`0x40`) 이 한 번 더 나간다 — 그 답의 `+0x38` bit 7 을 0 으로.
- 고친 뒤에도 `NtAllocateVirtualMemory` · `NtClose` × 2 가 같은 자리에서 나오면 → Q170 ① (ntdll FALSE 정리 길) 을 앞당길 것.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 값은 run44 덤프 읽기 로그를 다시 읽었다 (flow-writer).
- 정적:
  - **bcryptprimitives pdb `E63663F36C3ABFBF74EACD102DC13DBB1`** — **처음 analyze** (`System32\bcryptprimitives.dll` x64, 716408B, 이미지 베이스 `0x180000000`, 함수 1209, `_index.tsv` 있음, 별칭 표 `ms/sym/bcryptprimitives.dll.aliases.tsv`). 47개 뽑음 (`^entry$` depth 1 → 14개 + 정규식 depth 0), 지도 `ms/disassemble/bcryptprimitives/_map/entry_*` (285 함수, 깊이 10).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — `RtlGetSystemGlobalData` depth 0 새로 (다시 analyze 안 함).
  - kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` — `DeviceIoControl` depth 0 새로.
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` — 있던 `NtQueryInformationProcess.asm` + PE 바이트 (점프 표) 만.
- 이름: PDB 이름 `DllMain` 은 Ghidra 가 **`entry`** 로 잡았다 (`pdb_aliases.py --where bcryptprimitives.dll DllMain` → rva `0x433c0`, `_index.tsv` 이름 `entry`). 이 문서는 `DllMain` 으로 쓴다. 나머지 함수 (`GetCngHandle` · `GetSeedFromKernelState` · `IsEnclave` · `IsTrustlet` · `InitUmRootRngState` · `MsProviderStart`) 는 PDB 이름과 같다 (같은 명령으로 확인).
- 버전 대조 (결과서): "알려진 사실" 표에 없는 모듈이라, **에뮬레이터 로그 값과 코드 값이 같은 것**으로 맞춤 — `NtOpenFile` 세 값 · NtQIP `(-1, 0x56, …, 0xb0)` · 5번 · syscall 차례. flow-writer: PE 머리 AEP `0x433c0` (= `entry` 시작) · `DllCharacteristics 0x41e0` · `.rdata` rva `0x8d0b8` = `\Device\CNG` 를 `pefile` 로 다시 읽음.

## 목적

- PE_emulator 요청 31 (샘플 1183개 — r01 병목의 14 % — 가 bcryptprimitives `DllMain` FALSE 에서 막힘). 사용자 결정 2026-11-13: Q104 VM 단계 앞에서 끼워 **지금 먼저**, 정적만.
- attach 길과 `NtOpenFile` (이름 · 5번 조건 · 칸 안 쓴 경우 · FALSE 자리), 연 뒤 syscall (IOCTL · 버퍼 · TRUE 조건), class `0x56`, `NtTraceControl` × 3, 최소 조건 표.

## 방법

- 정적: `_workspace/sast/87_bcrypt-init.md` — bcryptprimitives analyze → 지도 (`entry` 뿌리) → `^entry$` depth 1 + 줄기 정규식 depth 0. `NtOpenFile` 까지 가는 길은 함수 포인터 (`g_pGetSeedFunction`) 라 지도에 선이 없어, `InitUmRootRngState` `.asm` 의 포인터 값으로 `GetSeedFromKernelState` 를 따로 뽑음 (결과서).
- 결과서: `_workspace/sast/87_bcrypt-init_result.md` (**분석성공** — 못 닫은 것은 `NtAllocateVirtualMemory` · `NtClose` × 2 의 정체 (attach 몫 아님까지) 와 `NtTraceControl` 3 의 구성뿐, 둘 다 결과에 영향 없음).
- 오케스트레이터가 `.asm` 으로 맞춘 줄: `GetCngHandle +0x94/+0x9b NtOpenFile`, `+0xa0 ~ +0xb8` (status 만 보고 핸들 칸 `[RBP+0x10]` 반환), `InitUmRootRngState +0x7a` 간접 호출, `+0x129 ~ +0x12e` 재시도 5번, `entry +0xcd MsProviderStart`, `+0xd4 JS`, `+0x1c3 SetLastError`, `+0x1cf XOR EAX,EAX`, `GetSeedFromKernelState +0x6a 0x390008` / `+0x77 DeviceIoControl`.
- flow-writer 가 다시 본 것:
  - bcryptprimitives `.asm`: `entry` 전부 (조각 둘 포함), `GetCngHandle` 전부, `GetSeedFromKernelState` 전부, `InitUmRootRngState` 전부, `GetTrustedEnvironment` · `IsEnclave` · `IsTrustlet` 전부, `MsProviderStart +0x0 ~ +0x1c8` · `+0x340 ~ +0x389`, `DebugTraceError +0x0 ~ +0x3f`, `WppInitUm` 전부, `SymCryptInitEnvWindowsUsermodeWin8_1nLater` 앞부분, `TraceLoggingRegisterEx… +0x0 ~ +0x2e`, `_calls.tsv` 의 attach 줄 (위 함수 13개) · `indirect` 두 줄, 지도 `entry_edges.tsv` 의 `NtOpenFile` · `CloseHandle` · `DeviceIoControl` · `NtQueryInformationProcess` 줄.
  - ntdll `RtlGetSystemGlobalData +0x0 ~ +0x26` · `+0x6f`, kernelbase `DeviceIoControl +0x0 ~ +0xaf` · `+0x150 ~ +0x1f8`, ntoskrnl `NtQueryInformationProcess +0x2d5 ~ +0x2f7` · `+0x42fe`.
  - PE 바이트 (`pefile`, scratchpad 임시 스크립트): ntoskrnl 바이트 표 rva `0x9bf75c` + `0x56` → 72, 4바이트 표 rva `0x9bf638` [72] → `0x9bf5de` (= `+0x42fe`), `0x55` → `0x9bf380` · `0x57` → `0x9bd25d` · class 5 → `0x9bf5de` — **결과서와 같음**. bcryptprimitives AEP · `DllCharacteristics` · `.rdata` `0x8d0b8`.
  - 이름: `pdb_aliases.py --where bcryptprimitives.dll` (`DllMain` 외 6개), `--lookup bcryptprimitives.dll 0x27ce0` (`DllMain +0x1d9` 의 `alias:BCryptAlloc+2` → 같은 주소 이름 3개).
  - 사진 (run44 덤프 읽기 로그): 모듈 목록 · 이미지 VAD · `\KnownDlls` 목록 · KUSD `RNGSeedVersion` (아래 "사진 대조").

## 결과

### 정적 — 호출 지점 (`ms/disassemble/bcryptprimitives/_calls.tsv` · `.asm`, kernelbase · ntdll · ntoskrnl `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `bcryptprimitives!DllMain` (`entry`) | `+0x15f` | `+0x164` | `GetTrustedEnvironment` | `+0x2d JZ` — 환경값 0 (처음) 일 때 | [정적] |
| 〃 | `+0x52` | `+0x59` | `DisableThreadLibraryCalls` | `+0x49 JZ` (FALSE ①) 를 지난 뒤 | [정적] |
| 〃 | `+0x69` | `+0x6e` | `_DllMainCRTStartupForGS2` | `/GS` 쿠키 | [정적] |
| 〃 | `+0x6e` | `+0x73` | `SymCryptInitEnvWindowsUsermodeWin8_1nLater` | CPU 기능 · XState (결과 안 봄) | [정적] |
| 〃 | `+0x8e` | `+0x93` | `SafeAllocaInitialize` | — | [정적] |
| 〃 | `+0xc8` | `+0xcd` | `WppInitUm` | → `+0x6e/+0x75 EtwRegisterTraceGuidsW`. 반환 안 봄 | [정적] |
| 〃 | `+0xcd` | `+0xd2` | **`MsProviderStart`** | `+0xd4 JS +0x1be` (FALSE ②) | [정적] |
| 〃 | `+0x1c3` | `+0x1ca` | `SetLastError` | `ECX = 0x54f` (`+0x1be`, FALSE ②) / `0x32` (조각 `FUN_180081a96`, FALSE ③) | [정적] |
| 〃 | `+0xfc` | `+0x103` | `GetModuleHandleExW` | `(5, &DllMain, &h)`, 실패도 TRUE | [정적] |
| `GetTrustedEnvironment` | `+0x4` | `+0x9` | `IsEnclave` | 0 아니면 `+0xb JNZ` 그 값 저장 | [정적] |
| 〃 | `+0xd` | `+0x12` | `IsTrustlet` | `+0x12 ~ +0x19` 0 → 1 / 1 → 4, `+0x1b` 저장 | [정적] |
| `IsEnclave` | `+0x44` | `+0x4b` | `NtQueryInformationProcess` | `(-1, 0x56, rsp+0x30, 0xb0, NULL)` | [정적] |
| `IsTrustlet` | `+0x34` | `+0x3b` | `NtQueryInformationProcess` | `(-1, 0, rsp+0x30, 0x40, NULL)` | [정적] |
| `MsProviderStart` | `+0x64` | `+0x69` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` | 환경 `& 0x18` 이 0 일 때. 반환 버림 | [정적] |
| `TraceLoggingRegisterEx_…` | `+0x55` · `+0x7c` | `+0x5c` · `+0x83` | `EventRegister` · `EventSetInformation` | ntdll `EtwEventRegister` · `EtwEventSetInformation` (결과서) | [정적] |
| `MsProviderStart` | `+0x126` · `+0x12b` · `+0x130` | `+0x12b` · `+0x130` · `+0x135` | `SymCryptRngAes{Instantiate,Reseed,Generate}Selftest` | 계산만 (결과서) | [정적] |
| 〃 | `+0x13b` | `+0x140` | **`InitUmRootRngState`** | 0 → `+0x144` → `0xc00000e5` | [정적] |
| 〃 | `+0x104` | `+0x109` | `DebugTraceError` | 실패 길 (WPP 기록만, `EBX` 안 건드림) | [정적] |
| 〃 | `+0x14f` | `+0x154` | `IsInLSAProcess` | 0 → `+0x156 JZ +0x345` (성공) | [정적] |
| `InitUmRootRngState` | `+0x7a` | `+0x7f` | (CFG 간접) `g_pGetSeedFunction` = `GetSeedFromKernelState` | `+0x5d` · `+0x64` 에서 넣은 값. 0 이면 `+0x129` 재시도 (5번) | [정적] |
| 〃 | `+0x9a` | `+0x9f` | `AesRNGState_init` | 음수 → `+0x13c` 0 | [정적] |
| `GetSeedFromKernelState` | `+0x1e` | `+0x23` | `GetCngHandle` | NULL → `+0x29 JZ +0xa0` → 0 | [정적] |
| 〃 | `+0x3a` | `+0x41` | `RtlGetSystemGlobalData` | `(1, rsp+0x68, 8)` — ntdll `+0x14 MOV RAX,[0x7ffe0250]` · `+0x1c` 씀 · `+0x1f` 0 | [정적] |
| 〃 | `+0x77` | `+0x7e` | `DeviceIoControl` | `(h, 0x390008, NULL, 0, buf, 0x30, &n, NULL)` | [정적] |
| `GetCngHandle` | `+0x45` | `+0x4c` | `RtlInitUnicodeString` | `L"\Device\CNG"` (`0x18008d0b8`) | [정적] |
| 〃 | **`+0x94`** | **`+0x9b`** | **`NtOpenFile`** | `+0x34 JNZ` 안 탔을 때 (캐시 0) | [정적] |
| 〃 | `+0xd5` | `+0xdc` | `CloseHandle` | CMPXCHG 에 졌을 때만 | [정적] |
| `kernelbase!DeviceIoControl` | `+0x6c` | `+0x73` | `NtDeviceIoControlFile` | 장치 종류 ≠ 9 (`+0x2b` · `+0x5e`), 겹침 구조 없음 (`+0x1d JNZ` 안 탐) | [정적] |
| 〃 | `+0x1de` | `+0x1e5` | `NtWaitForSingleObject` | status `0x103` 일 때만 (`+0x7f JZ +0x1d6`) | [정적] |
| `nt!NtQueryInformationProcess` | (직접) | — | `+0x2f7 JMP RDX` | `+0x2d5 CMP EDI,0x73` · `+0x2e5` 바이트 표 · `+0x2ed` 4바이트 표 → class `0x56` 목적지 `+0x42fe MOV EAX,0xc0000003` | [정적] (PE 바이트) |

### 호출 관계

```
bcryptprimitives!DllMain (Ghidra entry, rva 0x433c0, PE 진입점)   이유 = EDX                                   [정적]
 │  +0x1c / +0x1f  CMP EDX,1 · JNZ +0x13e     ← 이유 1 (attach) 만 아래로
 ├─ (1) +0x15f  GetTrustedEnvironment        ← +0x2d 환경값 0 (처음)
 │        ├─ (1) +0x4  IsEnclave → +0x44 NtQueryInformationProcess(-1, 0x56, buf, 0xb0)     진짜 = 0xc0000003 → 0
 │        └─ (2) +0xd  IsTrustlet → +0x34 NtQueryInformationProcess(-1, 0, buf, 0x40)      (1) 이 0 일 때만 → +0x38 bit 7
 │  +0x33 ~ +0x49  PE 머리 [base + e_lfanew + 0x5e] & 0x80 → 0 이면 +0x49 JZ +0x1cf  FALSE ①
 ├─ (2) +0x52  DisableThreadLibraryCalls
 ├─ (3) +0x69  _DllMainCRTStartupForGS2
 ├─ (4) +0x6e  SymCryptInitEnvWindowsUsermodeWin8_1nLater → +0xe SymCryptDetectCpuFeaturesByCpuid · +0x13 GetEnabledXStateFeatures
 │  +0x86 / +0x88  환경 bit 8 → +0x199 CPU 검사 → FALSE ③ (일반은 안 감)
 ├─ (5) +0x8e  SafeAllocaInitialize
 ├─ (6) +0xc8  WppInitUm → +0x6e EtwRegisterTraceGuidsW                       반환 안 봄
 ├─ (7) +0xcd  MsProviderStart (rva 0x436c4)
 │        ├─ (1) +0x64  TraceLoggingRegisterEx_EventRegister_EventSetInformation   반환 버림 (+0x69)
 │        │        ├─ (1) +0x55  EventRegister
 │        │        └─ (2) +0x7c  EventSetInformation
 │        │  +0x84 / +0x87  환경 & 0x16 == 0 → +0x10e  (아니면 IUM 갈래 +0xaa GetModuleHandleExW / +0xce LoadLibraryW)
 │        ├─ (2) +0x126 · +0x12b · +0x130  셀프테스트 셋                       계산만
 │        ├─ (3) +0x13b  InitUmRootRngState (rva 0x2b5dc)
 │        │        │  +0x5d / +0x64  g_pGetSeedFunction ← GetSeedFromKernelState (일반)
 │        │        ├─ (1) +0x7a  [CFG 간접] GetSeedFromKernelState(buf, 0x30, &ver) (rva 0x46d00)
 │        │        │        ├─ (1) +0x1e  GetCngHandle (rva 0x46da8)
 │        │        │        │        │  +0x10 ~ +0x34  캐시 g_hCNGDriver ≠ 0 → 그 값 바로 반환
 │        │        │        │        ├─ (1) +0x45  RtlInitUnicodeString(L"\Device\CNG")
 │        │        │        │        ├─ (2) +0x94  NtOpenFile(&[RBP+0x10], 0x100001, OA{0x40}, IOSB, 7, 0x20)
 │        │        │        │        │       +0xa2 JS +0xb8 · +0xaa LOCK CMPXCHG 캐시 ← [RBP+0x10] · +0xb8 [RBP+0x10] 반환
 │        │        │        │        └─ (3) +0xd5  CloseHandle                   CMPXCHG 에 졌을 때만
 │        │        │        │  +0x29 JZ +0xa0   핸들 0 → 반환 0
 │        │        │        ├─ (2) +0x3a  ntdll!RtlGetSystemGlobalData(1, &v, 8)     KUSD +0x250 읽기 (음수면 +0x4a INT 0x2c)
 │        │        │        └─ (3) +0x77  DeviceIoControl(h, 0x390008, NULL, 0, buf, 0x30)
 │        │        │                 └─ kernelbase +0x6c  NtDeviceIoControlFile    (0x103 이면 +0x1de NtWaitForSingleObject)
 │        │        │  +0x83 JZ +0x129 → +0x129 ~ +0x137 다시 (+0x6b) — 5번째 실패면 +0x12e JNC +0xbd → 반환 0
 │        │        └─ (2) +0x9a  AesRNGState_init(&g_rootAesRngState, buf, ver)   음수 → 0 (재시도 없음)
 │        │  +0x142 JNZ 안 탐 → +0x144 R9D=0xa1 → +0xec EBX = 0xc00000e5
 │        ├─ (4) +0x104  DebugTraceError → +0x109 JMP +0x34c → 반환 0xc00000e5        (실패 길)
 │        └─ (5) +0x14f  IsInLSAProcess → 0 → +0x156 JZ +0x345 → 반환 0                (성공 길)
 │  +0xd2 TEST · +0xd4 JS +0x1be → +0x1be ECX = 0x54f
 ├─ (8) +0x1c3  SetLastError → +0x1cf XOR EAX,EAX → FALSE ②                        (실패 길)
 ├─ (8) +0xfc   GetModuleHandleExW(5, &DllMain, &h)                                 (성공 길, 실패해도 TRUE)
 └─ +0x128  MOV EAX,1 → +0x13c RET   TRUE
```

### 사진 대조 (run44 덤프 읽기 로그, 새 실행 없음 — flow-writer)

| 칸 | 값 | 줄 | 뜻 |
|---|---|---|---|
| 초기화 끝 (P2) 모듈 목록 `BaseDllName` | `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` — **6개, bcryptprimitives 없음** | `ms/logs/04_baggage-read_P2.txt:3398 · 3503 · 3608 · 3713 · 3818 · 3923` | test.exe 는 초기화 동안 이 DLL 을 안 올린다 `[관찰]` |
| P2 이미지 VAD (`Mapped Exe`) | 6개 (`test.exe` · `dll2` · `dll1` · `KernelBase` · `kernel32` · `ntdll`) | `_P2.txt:2256 ~ 2261` | 위와 같음 `[관찰]` |
| `\KnownDlls` 목록 | `bcryptPrimitives.dll` 섹션 객체 `ffffb689a7be0630` | `_P1a.txt:1394` · `_P1b.txt:1394` · `_P2.txt:1394` | 부팅 때 만든 KnownDll 섹션 — 다른 프로세스가 import 하면 2.6 KnownDll 길로 올라올 것 `[추측: 실행 안 봄]` |
| KUSD `RNGSeedVersion` (`+0x250`, 8B) | **`0xc`** (8장 같음) | `_supp_<K1 ~ P2>.txt:197`, 유저 주소 `_P1a.txt:2905` · `_P1b.txt:4023` · `_P2.txt:4035` | `GetSeedFromKernelState` 가 `RtlGetSystemGlobalData(1)` 로 읽어 `*ver` 에 옮기는 칸 (IOCTL 성공 때만) — 결과서 "동적 4" 의 값 (run44 부팅 기준) `[관찰]` |

- sample 소스에도 암호 · 난수 API 글자가 없다 (`sample/*.c` · `log.h` 에 `bcrypt` · `Crypt` · `rand` 없음) `[소스]`. 진입점 뒤에 올리는지는 사진 범위 밖이다.

## 확인된 것

- `[정적]` attach 는 `DllMain +0x1c / +0x1f` 에서 이유 1 만 탄다. 일반 프로세스의 FALSE 길은 하나 — `MsProviderStart` 가 음수 (`+0xd4 JS`) → `SetLastError(0x54f)` → FALSE. 그 음수는 `InitUmRootRngState` 0 (`MsProviderStart +0x142`) 에서 온다.
- `[정적]` `NtOpenFile` 이 여는 이름은 `\Device\CNG` 하나 (`.rdata` `0x8d0b8`, flow-writer `pefile` 로 다시 읽음). 접근 · 공유 · 옵션 · 속성은 에뮬레이터 로그와 같다.
- `[정적]` `GetCngHandle` 은 status 만 보고, 돌려주는 값은 핸들 칸 `[RBP+0x10]` (`+0xb8`). 칸이 0 이면 NULL → 시드 함수 FALSE → 같은 이름을 5번 (`InitUmRootRngState +0x129 ~ +0x12e`) → 0.
- `[정적]` 연 뒤 syscall 은 `NtDeviceIoControlFile` 하나 (`0x390008`, 입력 없음, 출력 `0x30B`) — kernelbase `DeviceIoControl +0x6c`. 성공 status 만 본다. 그 앞 `RtlGetSystemGlobalData(1, …, 8)` 는 syscall 없이 KUSD `+0x250` 을 읽는다 (ntdll `+0x14`).
- `[정적]` NtQIP class `0x56` (길이 `0xb0`) 은 `IsEnclave` 가 부르고 결과 (첫 DWORD) 를 쓴다. 26100.9457 커널은 이 class 를 기본 갈래 `0xc0000003` 로 보낸다 (PE 바이트 표, flow-writer 다시 읽음 — 결과서와 같음). 그러면 `IsTrustlet` (class 0, `0x40`) 이 한 번 더 나간다.
- `[정적]` `NtTraceControl` 후보 셋 (`EtwRegisterTraceGuidsW` · `EventRegister` · `EventSetInformation`) 의 반환은 attach 결과에 안 닿는다 (`DllMain +0xcd` 바로 다음 call, `MsProviderStart +0x69` 덮기).
- `[정적]` attach 코드에 `NtAllocateVirtualMemory` · `NtClose` 를 부르는 곳은 없다 (지도 `CloseHandle` 둘은 조건부 · 다른 길).
- `[관찰]` run44: test.exe 는 초기화 동안 bcryptprimitives 를 안 올린다 (모듈 6개 · 이미지 VAD 6개). `bcryptPrimitives.dll` 은 `\KnownDlls` 섹션 객체로만 있다. KUSD `RNGSeedVersion` = `0xc` (8장 같음).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **`GetCngHandle +0xa2 JS` 목적지는 `+0xb8`** — 결과서 1-a 표 e 는 "`JS +0xc0`" 인데 `+0xc0` 줄은 없다 (`0x180046e60 − 0x180046da8 = 0xb8`). `+0xb8 MOV RBX,[RBP+0x10]` 로 핸들 칸 값을 돌려주는 건 같아서 결론은 같다. 오케스트레이터가 맞춘 "`+0xa0 ~ +0xb8`" 과 맞다.
- ② **`DebugTraceError` 가 바로 돌아오는 자리** — 결과서 1-d 6 "`+0x17 JZ` (`WPP_GLOBAL_Control` 이 자기 자신 — 초기값 `0x1800a3430`)" → 이 호출 때엔 `DllMain +0xa8 / +0xaf` 가 이미 `WPP_GLOBAL_Control = &WPP_MAIN_CB` (`0x1800a4360`) 로 바꿔 두었다 (`WppInitUm` 은 이 칸을 읽기만 — `+0xa`). 그래서 `+0x14 CMP` 는 같지 않고, 돌아오는 건 `+0x19 TEST byte [WPP_MAIN_CB+0x1c],1` · `+0x1d JNZ` 안 탐 쪽이다 (ETW 세션이 이 공급자를 안 켰을 때 `[추측: 그 비트를 켜는 콜백 0x18005f1e0 안 읽음]`). 어느 쪽이든 `EBX` 를 안 건드려 `MsProviderStart` 는 `0xc00000e5` — 결론 같음.
- ③ **`GetModuleHandleExW` 실패도 TRUE 인 마지막 갈림은 `+0x176 JZ +0x128`** — 결과서 "`+0x16b JZ +0x128`" 의 `+0x16b` 는 `MOV RCX,[WPP_GLOBAL_Control]`. 길: `+0x111 JZ +0x20a` → `+0x20a GetLastError` → `+0x218 JLE +0x169` (또는 `+0x21e ~ +0x228` HRESULT 로 바꿔 `+0x169`) → `+0x169 JNS +0x117` / `+0x176 JZ +0x128` / `+0x188 WPP_SF_qSL` → `+0x18d JMP +0x117` — 셋 다 `+0x128` TRUE.
- ④ (보탬) **`DllMain +0xda ~ +0xe5`** — `MsProviderStart` 성공 뒤 WPP 플래그 (`[WPP_MAIN_CB+0x1c] & 0x20`) 가 켜져 있으면 `+0x1d6` 로 가서 힙 할당 함수 (`+0x1d9`, 같은 주소 이름 3개, 후보: `MSCryptAlloc` · `BCryptAlloc` · `SafeAllocaAllocateFromHeap` — `pdb_aliases.py --lookup bcryptprimitives.dll 0x27ce0`) · `+0x1f9 GetModuleFileNameW` 를 하고 `+0xeb` 로 돌아온다. 결과서 차례 표엔 없던 줄. ETW 세션이 켤 때만이라 TRUE / FALSE 에 안 닿는다.
- ⑤ (보탬) **kernelbase `DeviceIoControl` 갈래** — 장치 종류 (`+0x17 SHR EAX,0x10`) 가 9 면 `+0x5e JZ +0x160 NtFsControlFile`. `0x390008` 은 종류 `0x39` 라 `+0x6c NtDeviceIoControlFile`. 음수 status 는 `+0x87 JS +0x192` → (경고 `0x8…` 이면 `+0x1ad` 바이트 수 씀) → `+0x154 BaseSetLastNTError` → `+0x159` FALSE. `0x103` 은 `+0x7f JZ +0x1d6` → `+0x1de NtWaitForSingleObject(핸들, FALSE, NULL)` → IOSB status 로 `+0x85` 다시. 성공이면 `+0x8d ~ +0x9e` 에서 `*lpBytesReturned = IOSB.Information` (`GetSeedFromKernelState` 는 안 읽음).
- ⑥ (보탬, 사진) KUSD `RNGSeedVersion` = `0xc` `[관찰]` — 결과서 "동적 4" 는 run44 부팅 기준으로 닫힘 (다른 부팅에서 같은지는 모름).
- ⑦ (보탬, 사진) **test.exe 는 bcryptprimitives 를 안 올린다** `[관찰]` — 위 "사진 대조". 그래서 이 바퀴는 그림을 안 고친다.
- ⑧ (보탬, `[추측]`) **`NtTraceControl` × 3 의 구성** — run62 (2.4 "ETW 등록") 의 ntdll 길로 보면 등록 하나마다 `EtwpRegisterProvider +0x14d NtTraceControl(0xf)` 한 번, `EtwEventSetInformation` 은 `EtwpSetProviderTraits +0xdd NtTraceControl(0x1e)` 한 번이다. ETW Once (`EtwpRegisterTpNotificationOnce +0x8c`) 는 프로세스 첫 등록 (ntdll `LdrpInitializeProcess +0xbee`) 에서 이미 끝났으니 다시 안 탄다. 그러면 `EtwRegisterTraceGuidsW` 1 + `EventRegister` 1 + `EventSetInformation` 1 = 3 으로 에뮬레이터 로그와 맞는다. `EtwRegisterTraceGuidsW` 안은 안 읽어서 짐작이다 → Q170.
- ⑨ (보탬) class 0 (길이 `0x40`) 을 진짜 커널이 성공시키는지는 커널 class 0 갈래를 안 읽어 `[추측]` — 실패해도 환경값은 `1` 로 같다 (`IsTrustlet +0x42 JS +0x54` → 0 → `GetTrustedEnvironment` 1). `buf+0x38` bit 7 의 이름 (`IsSecureProcess`) 은 결과서 표기이고 PDB 근거는 안 밝혔다.
- ⑩ (보탬) `SymCryptDetectCpuFeaturesByCpuid` 는 ntdll 에도 같은 이름이 있다 (2.4 run70 "CPU 기능 감지"). bcryptprimitives 는 제 사본을 `SymCryptInitEnvWindowsUsermodeWin8_1nLater +0xe` 에서 부른다 (`g_SymCryptFlags` bit 0 이 꺼져 있을 때, `+0xa / +0xc`).
- 이전 run 이 틀린 것은 없다 (bcryptprimitives 는 처음) — 정정 없음.

## 아직 확인 필요

**정적 — 대기열 Q170** (결과서 "다음 요청 후보" 1 · 2 · 3 · 5 + `NtTraceControl` × 3 구성):

- ① (후순위) ntdll `DllMain` FALSE 뒤 정리 길 — `LdrpInitializeNode +0x298` (`0xc0000142`) 이후 `NtAllocateVirtualMemory` · `NtClose` × 2 가 어디서 나오나. 엔진을 고친 뒤에도 같은 자리에서 막히면 앞당김.
- ② kernelbase `RegOpenKeyExW` · `RegQueryValueExW` · `RegCloseKey` → 어떤 NT 함수 (`IsInLSAProcess` 몫 — attach 가 TRUE 가 된 **다음** 막힘이 여기서 날 때). Q166 ② (.NET 쪽 `Reg*`) 와 같은 kernelbase 함수라 한 번에 볼 수 있다.
- ③ attach 뒤 첫 사용 길: `ProcessPrng` (rva `0x2df90`) → `AesRNGState_select` → `AesRNGState_generate`. 재시드 `AesRNGState_root_reseed +0x43/+0x48` 도 같은 시드 함수를 CFG 간접으로 부른다 (`_calls.tsv` `indirect:cfg:g_pGetSeedFunction`) — 캐시 핸들로 IOCTL 을 다시 하는 때.
- ⑤ (후순위) `cng.sys` 의 `0x390008` 처리 — 엔진은 "48B 채워 주기" 로 충분.
- (새로) `NtTraceControl` × 3 구성 — ntdll `EtwRegisterTraceGuidsW` 안 · `EtwEventRegister` · `EtwEventSetInformation` 이 이 프로세스에서 (Once 가 끝난 뒤) 각각 몇 번 (위 ⑧ 짐작 확인).

**동적 — 대기열 Q171** (결과서 "동적분석으로 확인해야 할 것", 이번 묶음은 VM 안 함). **test.exe 로는 못 본다** — 초기화 동안 안 올림 `[관찰]`, 진입점 뒤도 sample 에 그런 API 없음 `[소스]`. bcryptprimitives 를 올리는 프로세스가 필요하고 **샘플 고르기는 사용자 결정** (이 DLL 은 모든 프로세스가 올릴 수 있어 멈춤점은 그 프로세스 조건으로). Q104 에 안 붙임.

- 추천 멈춤점 넷 (WinDbg 모듈 이름 `bcryptPrimitives` — 아래 "도구 메모" 의 `entry` 이름 주의):
  - `DllMain+0xd2` (= `MsProviderStart` 반환 직후, rva `0x43492`): `eax` 0 이면 성공 길, `0xc00000e5` 면 시드 길 실패.
  - `GetCngHandle+0x9b` (= `NtOpenFile` 반환 직후): `eax` (status) · `dq @rbp+0x10 L1` (핸들 칸 — 진짜 핸들 모양, 4의 배수 작은 수 `[추측]`).
  - `GetSeedFromKernelState+0x7e` (= `DeviceIoControl` 반환 직후): `eax` (BOOL) · `db @rsi L30` (시드 48B).
  - `IsEnclave+0x4b`: `eax` (status — 정적으론 `0xc0000003`) · `dd @rsp+0x30 L1`, 이어 `IsTrustlet+0x3b` 에 서나 (NtQIP 2번).
- 그 밖: `NtDeviceIoControlFile` 의 `IoStatus.Information` (`0x30` 인가 `[추측]`), `LsaPid` 값 · `RegOpenKeyExW` status, `NtTraceControl` 실제 개수 · 인자, TRUE 로 끝나는 진짜 실행에서 `NtAllocateVirtualMemory` · `NtClose` × 2 가 나오나 (안 나오면 "attach 몫 아님" 확인).
- (결과서 "동적 4" KUSD `+0x250` 값은 run44 사진으로 닫힘 — `0xc`.)

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: **bcryptprimitives pdb 새 줄**, ntoskrnl · kernelbase · ntdll pdb 줄에 run91 몫, 근거 로그 run91 줄 (모듈 6개 · 이미지 VAD · `\KnownDlls` · KUSD `RNGSeedVersion`).
  - 흐름도 앞 글: run91 문단 하나 — **그림은 안 고침** (test.exe 는 이 DLL 을 안 올림).
  - 3장: 새 절 **"bcryptprimitives 시작 함수 (`DllMain`)"** (user32 · gdi32 절 뒤, 3장 끝).
  - 4장: run91 새 줄 셋 (동적 Q171 · 정적 Q170 · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run91 줄.
- 짐 지도 (짐 지도 바퀴 — test.exe 는 bcryptprimitives 를 안 올려 **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**):
  - 3.7 KUSD 머리 "ntdll 은 읽기만" 줄에 (run91) 읽는 줄 하나 (`RNGSeedVersion` ← ntdll `RtlGetSystemGlobalData +0x14` ← bcryptprimitives `GetSeedFromKernelState +0x3a`).
  - 7.2 KUSD `K1 이전` 에 `RNGSeedVersion` 실마리 (값 `0xc` `[관찰]`, 읽는 곳, 쓰는 쪽 모름).
  - 기준 run91 줄 · 판 목록 (마흔일곱째판), 8장 두 표.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 `BeingDebugged` 같은 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`. 다만 ETW · WPP 세션이 이 공급자를 켜 두면 `DllMain +0xe5` · `+0x122` · `DebugTraceError +0x1d` · `MsProviderStart` 의 `WPP_SF_*` 줄이 탄다 — TRUE / FALSE 엔 안 닿는다.
- **(새 함정 후보) Ghidra `entry` 는 WinDbg 심볼이 아니다**: 결과서 추천 멈춤점 "`bcryptPrimitives!entry +0xd2`" 의 `entry` 는 Ghidra 가 PE 진입점에 붙인 이름이고 PDB 이름은 `DllMain` 이다 (`pdb_aliases.py --where`). WinDbg 에선 `bcryptPrimitives!DllMain+0xd2` 나 rva (`bcryptPrimitives+0x43492`) 로 걸어야 할 것이다 `[추측]`. → windbg-kd-runbook pitfalls 에 추가 제안 (P46 `FUN_…` 조각과 같은 꼴).
- 모듈 이름 대소문자: `\KnownDlls` 이름은 `bcryptPrimitives.dll` (`_P1a.txt:1394`) — WinDbg 모듈 이름도 `bcryptPrimitives` 일 것 (결과서와 같은 짐작) `[추측]`.
- 도구 (결과서 "스크립트 문제"): 없음. analyze 로그의 `WindowsResourceReference` WARN 한 줄은 결과에 영향 없음.
