# 2026-11-02 run59 — 초기화 남은 일 (Q109 나머지 B·C·D·E, PE_emulator 요청 4-B~E): `LdrpInitialize` 가 부르는 세 함수 본문 — `InitSecurityCookie` 는 쿠키 칸을 **callee `LdrInitSecurityCookie` 안에서** `NtProtectVirtualMemory` 로 풀고 써서 `SecurityCookieInitialized` = 1 (`+0x42`), `InitSpecialMachineFrames` 는 표 2칸을 풀림 정보 주소로 바꿈 (syscall 없음), `__cpu_features_init` 은 `__isa_info` 1바이트만 (syscall 없음). `RtlEncodePointer` 는 캐시가 0 일 때만 `NtQueryInformationProcess(0x24)` — **이 프로세스의 첫 호출은 `LdrpInitializeProcess +0x12f`** (힙 관리자 쪽 아님). `RtlpInitializeNonVolatileFlush` 는 시스템 정보 `0xc0` 으로 전역 넷. 커널은 PEB `BitField` 비트별 출처 · `+0x138` · `+0x7c8` · `+0x90` 쓰는 줄. 환경 변수 이름 47개 (K1 덤프) (정적 + 덤프)

## PE_emulator 쪽에서 쓸 것

결과서 (`_workspace/sast/54_init-leftovers_result.md`) 의 표 둘을 **글자 하나 안 바꾸고** 아래 1절·2절에 옮겼다. 표 밑 글머리와 3절은 flow-writer 가 `.asm` 의 `CALL … ret=` 줄로 다시 맞춘 것이다. 오프셋 읽는 법:

- `call +0x..` = CALL 명령이 있는 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리.
- 분기 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 오프셋.
- `.asm`·`.c` 는 `ms/disassemble/{ntdll,ntoskrnl}/<함수>.asm` (커밋 안 함).

### 1. PE_emulator 용 한 줄 표 `[정적]` (결과서 그대로)

| 함수 | 쓰는 것 | syscall | 갈래 조건 |
|---|---|---|---|
| `LdrpInitialize` (ntdll 99B) | `LdrpIsSecureProcess` = 1 (쿠키 초기화 직후, `ProcessParameters.Flags` 맨 윗 비트가 켜졌을 때) | — | 쿠키 깃발 0 → `InitSecurityCookie`; 특수프레임 깃발 0 → `InitSpecialMachineFrames`; `__cpu_features_init` 은 항상; 끝에 `LdrpInitializeInternal` 꼬리호출 |
| `InitSecurityCookie` | `SecurityCookieInitCount` +1, `__security_cookie` (48비트), `SecurityCookieInitialized` = 1 | `NtQueryPerformanceCounter` 1, `NtProtectVirtualMemory` 2 (쿠키 칸 쓰기 풀기·되돌림), 대기 스레드는 `NtDelayExecution` | 순번 1 만 초기화; 현재 쿠키가 파일 기본값 `0x2b992ddfa232` 일 때만 바꿈 |
| `InitSpecialMachineFrames` | `SpecialMachineFramesInitCount` +1, `RtlpContinuationContextMachineFrameEntries[0..1]`, `SpecialMachineFramesInitialized` = 1 | 없음 (대기 스레드만 `NtDelayExecution`) | 순번 1 만 초기화 |
| `__cpu_features_init` | `__isa_info` (1B) | 없음 | 없음 (항상 같은 길) |
| `RtlEncodePointer` | 캐시 `CookieValue` (처음만) | `NtQueryInformationProcess(ProcessCookie 0x24)` — 캐시가 0 일 때만 | 캐시 0 → syscall 뒤 저장, 실패 → `RtlRaiseStatus` |
| `RtlpInitializeNonVolatileFlush` | `RtlpIsFlushRequired`·`RtlpOptimalFlushMethod`·`RtlpIsDrainRequired`·`RtlpClFlushSize` | `NtQuerySystemInformation(SystemFlushInformation 0xc0)` | syscall 실패 시 아무것도 안 씀; 비트별 (질문 6 표) |
| `RtlGetCurrentServiceSessionId` | (읽기) `PEB.SharedData` → 첫 dword | — | `SharedData` NULL → 0 |
| `LdrpInitializationComplete` ETW 갈래 | `LdrpProcessInitialized` 1→2, 이벤트 set | `NtSetEvent` (이벤트가 있을 때), ETW 켜지면 `LdrpLogEtwEvent` | 질문 7의 1~6 |
| `_LdrpInitialize` (첫 스레드, `+0x10d ~ +0x13e`) | 동적 함수표 머리 자기 주소·락·Tree 칸 0 | — | `LdrpProcessInitialized` CMPXCHG 가 0 일 때만 |
| `PspAllocateProcess` PEB 견본 | PEB `BitField` 비트들 (질문 8) | `SeQueryInformationToken(0x1d)`, `PsQueryProcessAttributes` (속성 읽기, 커널 안) | `F` 비트 8 (PEB 있는 프로세스) |
| `PspSetupUserProcessAddressSpace` → `PspLocateInPEManifest` | (찾으면) 만들기 정보 `+0x10`b5·`+0x130`·`+0x138`, 매개변수 `Flags` b13 | — | `CreateInfo+0x10` 비트 4 (= 사용자 `DetectManifest`) 켜졌을 때; 못 찾음 코드는 성공 처리 |
| `PspGetProcessParameterOverrides` | 만들기 정보 `+0x1d8` (스텁이면 0) | — | 확장 import 가 `ret 0` 스텁 |
| `PspUpdatePebForAffinityChange` | PEB `+0x138` (WoW64 면 PEB32 `+0xc0`) | — | PEB 있을 때; 남의 프로세스면 rundown 보호 |
| `KeCopyXfdMaskToPeb` | PEB `+0x7c8` (WoW64 면 PEB32 `+0x480`) | — | `KeFeatureBits` 비트 `0x37` 켜졌을 때만 |
| `PspMapSiloSharedDataView` | (서버 silo 만) PEB `+0x90` | `MmMapViewOfSection` (silo 만) | 서버 silo 없으면 아무것도 안 하고 0 |

- **syscall 이 표의 함수 본문에 있는 게 아니다** (flow-writer `.asm` 확인 — 표는 안 고침). 사슬:
  - `InitSecurityCookie` 본문의 syscall 은 `NtDelayExecution` (call `+0x61`, ret `+0x66`) 하나뿐이고, 순번이 1 이 아닌 (기다리는) 스레드만 탄다.
  - `NtQueryPerformanceCounter` 는 `InitSecurityCookie` (call `+0x38`) → `LdrInitSecurityCookie` (call `+0x75`) → `LdrpGenSecurityCookie` (call `+0x8d`, ret `+0x92`) 안.
  - `NtProtectVirtualMemory` 둘은 `InitSecurityCookie` (call `+0x38`) → `LdrInitSecurityCookie` 안 (call `+0x1e0`, ret `+0x1e5` · call `+0x212`, ret `+0x217`). 쿠키 쓰기는 그 사이 `+0x1e9`.
  - `InitSpecialMachineFrames` 본문 syscall = `NtDelayExecution` (call `+0x7c`, ret `+0x81`, 기다리는 스레드만).
  - `RtlEncodePointer` 는 본문 (call `+0x43`, ret `+0x48`).
  - `RtlpInitializeNonVolatileFlush` 도 본문 (call `+0x31`, ret `+0x36`, 클래스는 `+0x1e MOV ECX,0xc0`). `.asm` 은 이 주소를 **같은 주소 이름 2개**로 보인다 — 후보 `NtQuerySystemInformation`·`RtlGetNativeSystemInformation` (run45 `LdrInitializeMrdata` 와 같은 것).
  - `LdrpInitializationComplete`: `NtSetEvent` call `+0x51` (ret `+0x56`), `LdrpLogEtwEvent` call `+0xa5` (ret `+0xaa`).
  - `PspAllocateProcess` 견본 줄의 `SeQueryInformationToken` (call `+0x1060`, ret `+0x1065`) · `PsQueryProcessAttributes` (call `+0x225f`, ret `+0x2264`), `PspMapSiloSharedDataView` 의 `MmMapViewOfSection` (call `+0xcad2f`, ret `+0xcad34`) 은 **커널 안 함수**다 (유저 syscall 아님).

### 2. 짐 지도용 새 "쓴 자리" `[정적]` (결과서 그대로)

(칸마다 단계 ①·② / 놓는 법 / 경로. 근거 `[정적]` = 이 결과서의 줄. "7.3" = 모르는 칸 목록에서 빼기)

| 칸 | 단계 | 값 | 놓는 법 | 경로 (줄) | 들어갈 곳 |
|---|---|---|---|---|---|
| `RtlpDynamicFunctionTable` (`.mrdata` `0x1e7060`, 목록 머리 16B) | ② | 자기 주소 (빈 원형 목록) | 직접 | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize +0x114 · +0x11b` | 3.9 새 줄, 7.3 `.mrdata ≤P1a` 에서 뺌 |
| `RtlpDynamicFunctionTableTreeMin` · `TreeMax` (`0x1e7070`·`0x1e7078`), `RtlpDynamicCallbackTableTreeMin` · `TreeMax` (`0x1e7058`·`0x1e7050`) | ② | 0 (파일 값도 0 — 값 안 바뀜) | 직접 | `_LdrpInitialize +0x129 · +0x130 · +0x137 · +0x13e` (나중 쓰기는 주소를 넘겨 안쪽에서) | 3.9 새 줄 (또는 한 줄로), 7.3 `.mrdata` `Tree*` 에서 첫 쓰기 확인 |
| `RtlpDynamicFunctionTableLock` (`.data` `0x1d4210`) | ② | 0 | 직접 | `_LdrpInitialize +0x122` | 3.8 새 줄 (SRWLOCK 처음 값), 7.3 `.data 끝까지 0` 에서 "0 을 씀" 으로 |
| `RtlpFcProcessManager` (`.data` `0x1d3c40`, `0x118`B) | ② | 전부 0, `+0x108` 목록 머리 자기 주소 | 직접 (memset 4번 + 8바이트 저장) | `②… → LdrpInitializeProcess` call `+0x21c · +0x22d · +0x254 · +0x265`, `+0x237 · +0x23e · +0x26a · +0x271 · +0x27f · +0x286` | 3.8 새 줄, 7.3 `.data ≤P1a` 에서 뺌 |
| `SRWLockSpinCycleCount`·`ConditionVariableSpinCycleCount`·`RtlpWaitOnAddressSpinCycleCount` | ② | 0, CPU 2개 이상이면 `0x2800` | 직접 | `LdrpInitializeProcess +0x1cd · +0x1dd · +0x1e7 · +0x1f0 · +0x1fa · +0x203` | 3.8 새 줄 3, 7.3 `.data ≤P1a` 에서 뺌 |
| `RtlpOptimalFlushMethod`·`RtlpClFlushSize`·`RtlpIsFlushRequired`·`RtlpIsDrainRequired` | ② | 1/2/3 · 캐시 줄 크기 · 0 · 1 (질문 6 표) | 직접 (결과는 syscall 에서) | `LdrpInitializeProcess +0x28d → RtlpInitializeNonVolatileFlush +0x41 · +0x56 · +0x60 · +0x6b · +0x85` ← syscall `NtQuerySystemInformation(0xc0)` | 3.8 새 줄 4, 7.3 `.data ≤P1a` 에서 뺌. syscall 한 줄은 **커널이 칸을 채움** 쪽(2장 nt 몫)에도 |
| `__isa_info` (`.data` `0x1ccf00`) | ② | `1 \| 2 \| 4 \| 8 \| 0x10` 조합 | 직접 | `LdrpInitialize +0x27 → __cpu_features_init +0x74` | 3.8 새 줄, 7.3 `.data ≤P1a` 에서 뺌 |
| `SecurityCookieInitCount` (`0x1d23d4`)·`SpecialMachineFramesInitCount` (`0x1d23f0`) | ② | +1 (`XADD`) | 직접 | `LdrpInitialize +0x41 → InitSecurityCookie +0x9` · `+0x22 → InitSpecialMachineFrames +0xf` | 3.8 새 줄 2, 7.3 `.data ≤P1a` 에서 뺌 |
| `__security_cookie` (`.mrdata` `0x1ea5a8`) 쓰는 줄 | ② | 48비트 난수 식 (4a) | `NtProtectVirtualMemory` 로 풀고 직접, 다시 보호 | `LdrpInitialize +0x41 → InitSecurityCookie +0x38 → LdrInitSecurityCookie +0x1e9` (보호 풀기 call `+0x1e0`, 되돌림 call `+0x212`) | 3.9 줄에 경로·syscall 보탬 (사진 `K1 이전` → `P1~P1a` 와 맞음) |
| `RtlpContinuationContextMachineFrameEntries` (`.data` `0x1cc208`, 8B×2) | ② | 함수 주소 → 그 함수 `UnwindData` 주소 | 직접 | `LdrpInitialize +0x22 → InitSpecialMachineFrames +0x46` | 3.8 새 줄, 7.3 `.data ≤P1a` 에서 뺌 |
| `?CookieValue@?1??RtlpGetCookieValue@@9@9` (`.data` `0x1d2408`) | ② | 커널 `ProcessCookie` 값 (syscall 결과) | 직접 (캐시) | `LdrpInitializeProcess +0x12f → RtlEncodePointer +0x50` ← syscall `NtQueryInformationProcess(0x24)` call `+0x43` | 3.8 새 줄, 7.3 `.data ≤P1a` 에서 뺌 |
| `x86_cpu_enable_ssse3`·`x86_cpu_enable_simd`·`x86_cpu_enable_avx512`·`g_SymCryptFlags`·`g_SymCryptCpuFeaturesPresentCheck`·`g_SymCryptCpuid1` | ② (`P1b~P2`) | 질문 4c 표 | 직접 (+ CPUID) | `LdrpInitializeProcess +0x1d75 · +0x1d8e · +0x1da1 · +0x1ddf · +0x1df4 · +0x1e27`, `SymCryptDetectCpuFeaturesByCpuid +0xee · +0xf4` | 3.8 새 줄 6, **7.3 `.data P1b~P2 (6)` 이 0 으로** |
| PEB `+0x3` `BitField` 비트별 | ① | 질문 8 표 | 직접 (견본 → `MmCreatePeb`) | `①… → PspAllocateProcess +0x2277 ~ +0x22e1` | 3.1 첫 줄 "견본 복사" 에 비트별 출처 보탬 (K1 `0x4`) |
| `_KPROCESS.SecureState` (`+0x170`) 비트 0 `SecureProcess` | ① | 1 (보안 프로세스일 때만) | 직접 (`VslCreateSecureProcess` 성공 뒤) | `①… → PspAllocateProcess → KeInitializeProcess +0x473` (읽는 쪽 `MmCreatePeb +0x2b8`) | 3.12 (이미 줄이 있으면 뜻만 보탬) |
| 만들기 정보 (커널 복사본) `+0x10` 비트 4 (`DetectManifest`) · 비트 5 · `+0x130` · `+0x138`, 매개변수 사본 `Flags` 비트 13 | ① | 매니페스트를 찾으면 켬 (test.exe 는 자원이 없어 안 켜짐) | 직접 | `PspCaptureCreateInfo +0x77 ~ +0x95` (비트 4) / `PspSetupUserProcessAddressSpace +0x106 → PspLocateInPEManifest +0x88 · +0x8c · +0x99` | 범위 밖 구조체 (만들기 정보, 3.2 `+0x8 Flags` 줄에 "비트 13 조건" 보탬) |
| 만들기 정보 (커널 복사본) `+0x1d8` | ① | 0 (스텁) | 직접 | `NtCreateUserProcess +0xab3 → PspGetProcessParameterOverrides +0x4a` | 범위 밖 구조체, 기록만 |
| PEB `+0x138` `ActiveProcessAffinityMask` (이미 있으면 근거 칸) | ① | `GROUP_AFFINITY.Mask` | 직접 | `PspSetupUserProcessAddressSpace +0x22f → PspUpdatePebForAffinityChange +0x54 → PspWritePebAffinityInfoAttached +0x6f` | 3.1 (본문 줄 보탬) |
| PEB `+0x7c8` `ExtendedFeatureDisableMask` | ① | `KPROCESS +0x198` 값 (조건 `KeFeatureBits` 비트 `0x37`) | 직접 (조건부) | `PspSetupUserProcessAddressSpace +0x222 → KeCopyXfdMaskToPeb → 조각 +0x7` | 3.1 (조건 보탬) |
| PEB `+0x90` `SharedData` | ① (서버 silo 만) | 뷰 주소 | 직접 (`MmMapViewOfSection`) | `PspSetupUserProcessAddressSpace +0x25e → PspMapSiloSharedDataView +0xcad48` | 3.1 (test.exe 안 탐 — NULL) |
| 시작 정보 블록 `+0xa0 ~ +0xb7` (옵션 24B) | ① | 질문 13 | 직접 (복사) | `PspPrepareSystemDllInitBlock +0x126 · +0x132 ~ +0x138` ← 표는 `PspApplyMitigationOptions +0x5ef · +0x5f4` 가 제자리 갱신 | 3.9 `LdrSystemDllInitBlock` 줄에 `+0xb0` 출처 보탬 |

- 표 밑 (flow-writer `.asm` 확인 — 표 숫자는 안 고침):
  - 마지막 줄 `PspPrepareSystemDllInitBlock +0x126` 은 스택 사본을 **읽는** 줄 (`MOVUPS XMM0,[RSP+0x28]`) 이고, 블록 `+0xa0` 에 **쓰는** 줄은 `+0x12b` (`MOVUPS [RBX+0xa0],XMM0`) 다. `+0xb0` 도 `+0x132` 읽기 → `+0x138` 쓰기. run32·flow.md 의 `+0x12b`·`+0x138` 이 맞다.
  - `PspMapSiloSharedDataView +0xcad48` 은 함수 몸에서 떨어진 조각 안 줄이다 (`+0x1f JZ 0x140b3e83a` − 시작 `0x140a73b70` = `+0xcacca` 부터).
  - `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (baggage 3장 줄임).
  - `__security_cookie` 줄의 `NtProtectVirtualMemory` 는 `LdrInitSecurityCookie` 안이다 (1절 사슬).
  - `PspSetupUserProcessAddressSpace +0x222 · +0x22f · +0x25e` 는 모두 **call** 자리 (ret `+0x227 · +0x234 · +0x263`).

### 3. 함수별 차례 `[정적]` (`.asm` 줄)

라벨은 call / ret. 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 명령 시작 자리.

```
LdrpInitialize (ntdll 0xe1448, 99B)                         ← LdrInitializeThunk call +0x9 / ret +0xe
 │  +0xa  CMP byte [SecurityCookieInitialized],0 → +0x17 JZ +0x41
 ├─ call +0x41 / ret +0x46  InitSecurityCookie (0xe1548, 112B)
 │     +0x9   XADD.LOCK [SecurityCookieInitCount],1 → +0x13 CMP 순번,1 → +0x16 JNZ +0x4f (기다리는 길)
 │     ├─ call +0x18 / ret +0x1d  LdrpGenRandom          KUSD ProcessorFeatures[28] ≠ 0 이면 RDRAND (최대 10번), 아니면 0
 │     │  +0x1d  MOV R9D,[LdrSystemDllInitBlock+0x98] → +0x31 XOR R9,RAX (씨앗)
 │     ├─ call +0x38 / ret +0x3d  LdrInitSecurityCookie(0, 0, &__security_cookie, 씨앗)
 │     │     +0x5f  기본값 0x2b992ddfa232 → +0x6c JNZ +0x1a0 (현재 값이 기본값과 다르면 안 씀)
 │     │     ├─ call +0x75 / ret +0x7a  LdrpGenSecurityCookie → call +0x8d / ret +0x92 NtQueryPerformanceCounter
 │     │     ├─ call +0x1e0 / ret +0x1e5  NtProtectVirtualMemory (8B, PAGE_READWRITE)
 │     │     │  +0x1e9  MOV [RDI],RBX   __security_cookie = 새 값 (48비트, 0·기본값이면 0x2b992ddfa233 — +0x221)
 │     │     └─ call +0x212 / ret +0x217  NtProtectVirtualMemory (옛 보호로)
 │     │  +0x3d  OR.LOCK [RSP],0 (장벽) → +0x42  SecurityCookieInitialized = 1
 │     └─ (기다리는 길) +0x4f → call +0x61 / ret +0x66 NtDelayExecution(−0x493e0 = 30ms) → +0x66 깃발 검사 → +0x6d JZ +0x5a 다시 / +0x6f JMP +0x49 끝
 │  +0x46 ~ +0x58  PEB → ProcessParameters → Flags (+0x8) 맨 윗 비트 → +0x58 JGE +0x19
 │  +0x5a  LdrpIsSecureProcess = 1 → +0x61 JMP +0x19
 │  +0x19  CMP byte [SpecialMachineFramesInitialized],0 → +0x20 JNZ +0x27
 ├─ call +0x22 / ret +0x27  InitSpecialMachineFrames (0xe14b4, 139B)
 │     +0xf   XADD.LOCK [SpecialMachineFramesInitCount],1 → +0x19 CMP 순번,1 → +0x1c JNZ +0x6a (기다리는 길)
 │     ├─ call +0x39 / ret +0x3e  RtlLookupFunctionEntry   (두 바퀴: +0x2b EDI=2, +0x4d SUB, +0x51 JNZ +0x2e)
 │     │  +0x46  RtlpContinuationContextMachineFrameEntries[i] = ImageBase + RUNTIME_FUNCTION.UnwindData
 │     │  +0x53  OR.LOCK [RSP],EDI (장벽) → +0x57  SpecialMachineFramesInitialized = 1
 │     └─ (기다리는 길) call +0x7c / ret +0x81 NtDelayExecution (30ms)
 ├─ call +0x27 / ret +0x2c  __cpu_features_init (0x12e47c, 130B)   ← 깃발 검사 없음, 매번
 │     ├─ call +0xb / +0x17 / +0x45 / +0x5f  RtlIsProcessorFeaturePresent(0x26 / 0x2a / 0x27 / 0x28)   (ret +0x10 / +0x1c / +0x4a / +0x64)
 │     │  +0x74  MOV byte [__isa_info],DL   (항상 1, 0x2a → 2, 0x27 → 4, 0x26 → 8, 0x28 → 0x10)
 └─ +0x3c  JMP LdrpInitializeInternal (꼬리)

_LdrpInitialize (첫 스레드: +0x3a CMPXCHG.LOCK [LdrpProcessInitialized] 이 0 을 돌려줌 → +0x5a JZ +0xcf)
    +0x10d LEA RAX,&RtlpDynamicFunctionTable
    +0x114 · +0x11b  RtlpDynamicFunctionTable Blink · Flink = 자기 (빈 원형 목록)
    +0x122  RtlpDynamicFunctionTableLock = 0 (RBX, +0x28 XOR EBX,EBX)
    +0x129 · +0x130 · +0x137 · +0x13e  RtlpDynamicFunctionTableTreeMin · Max · RtlpDynamicCallbackTableTreeMin · Max = 0

LdrpInitializeProcess (0x8c404)
 ├─ call +0x12f / ret +0x134  RtlEncodePointer(0) (0xe9610, 95B)
 │     +0xe  MOV EAX,[CookieValue 캐시 0x1d2408] → +0x16 JZ +0x2a
 │     ├─ (캐시 0) call +0x43 / ret +0x48  NtQueryInformationProcess(−1, 0x24, &버퍼, 4, NULL)
 │     │     +0x4a JS +0x58 → call +0x5a RtlRaiseStatus / 성공 → +0x50 캐시 = 값 → +0x56 JMP +0x18
 │     │  +0x18 ~ +0x20  (포인터 XOR 쿠키) ROR (쿠키 & 0x3f)
 │  +0x134  RtlpUnhandledExceptionFilter = 돌려받은 값 (flow-writer .asm — 결과서 표 밖)
 ├─ call +0x153 / ret +0x158  LdrInitializePerThreadRng
 │  +0x158  PebLdr.Length = 0x58 · +0x162 LEA R15D,[R14+1] (값 1 만들기) · +0x166 PebLdr.Initialized = R15B
 │  +0x1cd  SRWLockSpinCycleCount = 0 · +0x1d4 CMP PEB.NumberOfProcessors,1 → +0x1db JBE · +0x1dd = 0x2800
 │  +0x1e7  ConditionVariableSpinCycleCount = 0 · (+0x1ee JBE) · +0x1f0 = 0x2800
 │  +0x1fa  RtlpWaitOnAddressSpinCycleCount = 0 · (+0x201 JBE) · +0x203 = 0x2800
 ├─ call +0x21c · +0x22d · +0x254 · +0x265  memset   RtlpFcProcessManager 0x118B 를 0 으로 (ret +0x221 · +0x232 · +0x259 · +0x26a)
 │  +0x26a · +0x271  +0xf0 · +0x100 = 0 / +0x27f · +0x286  +0x110 · +0x108 = &RtlpFcProcessManager+0x108 (빈 목록)
 └─ call +0x28d / ret +0x292  RtlpInitializeNonVolatileFlush (0xfb648, 144B)
       +0x1e MOV ECX,0xc0 → call +0x31 / ret +0x36  시스템 정보 조회 (같은 주소 이름 2개) (0xc0, 버퍼 0x20B)
       +0x38 JS +0x72 (실패면 아무것도 안 씀)
       +0x41 RtlpIsFlushRequired = 0 · +0x85 RtlpOptimalFlushMethod = 2 (→ +0x8f JMP +0x60) · +0x56 = 3
       +0x60 RtlpIsDrainRequired = 1 · +0x6b RtlpClFlushSize = [버퍼+4]

LdrpInitializationComplete (ETW 갈래)
 ├─ call +0x19 / ret +0x1e  RtlGetCurrentServiceSessionId (0x38fc0, 25B)   PEB.SharedData NULL → 0
 │  +0x20 JNZ +0xac (≠ 0 → SharedData+0x22a) / +0x26 칸 = KUSD 0x7ffe0384 → +0x2c CMP → +0x30 JZ +0x44
 │  +0x3b TEST PEB.TracingFlags,4 → +0x42 JNZ +0x67
 ├─ call +0x67 / ret +0x6c  RtlGetCurrentServiceSessionId → +0x6e JZ +0xc8 (0x7ffe0385) → +0x87 TEST [칸],0x20 → +0x8a JZ +0x44
 ├─ call +0xa5 / ret +0xaa  LdrpLogEtwEvent
 │  +0x44  INC.LOCK [첫째 인자]  (LdrpProcessInitialized 1 → 2)
 └─ call +0x51 / ret +0x56  NtSetEvent (이벤트가 있을 때)
```

```
PspAllocateProcess (ntoskrnl)
 │  지역 플래그 EBX (= F): +0x2ba ~ +0x31f (최소 프로세스·섹션 유무), +0x9c5 비트 13 (ImageDynamicallyRelocated, 경로 A), +0x21c6 ~ +0x21db 비트 12 (큰 페이지), +0x21e9 BTS 비트 8
 ├─ call +0x98c / ret +0x991  MmGetSectionInformation(섹션, 4, &[R15+0x38]) → +0x99f ImageFlags (경로 A)
 ├─ call +0xbab / ret +0xbb0  RtlQueryImageFileKeyOption (UseLargePages) → +0xbcd OR R12D,0x10 → +0xbd9 9번째 인자 칸
 ├─ call +0x1060 / ret +0x1065  SeQueryInformationToken(토큰, 0x1d) → IsAppContainer
 │  +0x2248  BT EBX,8 → JNC +0x2364
 ├─ call +0x225f / ret +0x2264  PsQueryProcessAttributes → IsPackagedProcess (WIN://PKG)
 │  +0x2266 · +0x226e  견본 앞 8B = 0, Mutant = −1
 │  +0x2277 ~ +0x22e1  BitField 바이트 만들기 → +0x22e1 MOV [RSP+0x1ab],R12B
 ├─ call +0x230f / ret +0x2314  MmCreatePeb(…, &견본 +0x2304 LEA)
 │     +0x2b8  TEST [EPROCESS+0x170],1 (SecureState.SecureProcess) → +0x2c0 JNZ +0x5e6 (고정값) / call +0x2c9 RtlImageNtHeader
 │  +0x236e JNS · +0x2377 JZ · +0x2380 JNZ → 셋 다 +0x2574 로 (아래 복사본)
 ├─ call +0x2397 / ret +0x239c  PspSetupUserProcessAddressSpace
 │     +0xb8 CMP params.LoaderThreads,0 → +0xc1 MOV EAX,[PsDefaultLoaderThreads] → +0xc7 params+0x40c
 │     ├─ call +0xf5 / ret +0xfa  KiStackAttachProcess
 │     │  +0xfa  TEST byte [CreateInfo+0x10],0x10 → +0xfe JZ +0x115
 │     ├─ call +0x106 / ret +0x10b  PspLocateInPEManifest      (조건부. test.exe 는 자원이 없어 결과가 안 남음)
 │     │     +0x6 WoW64 아님 → call +0x70 / ret +0x75 LdrResSearchResource(RT_MANIFEST 24, ID 1)
 │     │     +0x88 CreateInfo+0x10 |= 0x20 · +0x8c +0x138 = 크기 · +0x99 BTS params.Flags,13
 │     ├─ call +0x222 / ret +0x227  KeCopyXfdMaskToPeb
 │     │     +0x0 BT [KeFeatureBits],0x37 → 꺼지면 +0xb RET / 조각 FUN_1406f9d66 +0x7 MOV [PEB+0x7c8],[KPROCESS+0x198]
 │     ├─ call +0x22f / ret +0x234  PspUpdatePebForAffinityChange
 │     │     +0x37 JZ +0x69 (PEB 없음) / call +0x4c KiStackAttachProcess → call +0x54 / ret +0x59 PspWritePebAffinityInfoAttached
 │     │        call +0x64 / ret +0x69 KeQueryPrimaryGroupAffinityProcess → +0x6f MOV [PEB+0x138],RSI (Mask)
 │     ├─ call +0x250 / ret +0x255  MmMapApiSetView (run57)
 │     └─ call +0x25e / ret +0x263  PspMapSiloSharedDataView
 │           call +0xb PsGetProcessServerSilo → call +0x16 PsIsHostSilo → +0x1f JZ 안 탐 → +0x25 XOR EAX,EAX → RET (보통 프로세스)
 └─ (+0x2574 갈래, PspSetupUserProcessAddressSpace 를 건너뛸 때만) +0x2574 BT EBX,8 · +0x257e TEST BL,0x10 → JNC/JNZ +0x260d
       call +0x259e / ret +0x25a3  KeCopyXfdMaskToPeb · call +0x25ae / ret +0x25b3  PspUpdatePebForAffinityChange
       call +0x25ba / ret +0x25bf  MmMapApiSetView · call +0x25c8 / ret +0x25cd  PspMapSiloSharedDataView

NtCreateUserProcess
 └─ call +0xab3 / ret +0xab8  PspGetProcessParameterOverrides (104B)
       call +0x34 / ret +0x3b  [__imp_PsGetProcessParameterOverrides] (간접, PE 파일 값 = ntoskrnl rva 0x4a10c0 스텁 `ret 0`)
       +0x4a  MOV [CreateInfo+0x1d8],RAX  (스텁이면 0) / call +0x60 PspDestroyProcessParameterOverrides (모양이 2 가 아닐 때)

PspPrepareSystemDllInitBlock (run32)
    +0x64 ~ +0x78  CreateInfo+0x150 (16B) · +0x160 (8B) 를 지역 사본으로
    +0x100  블록 +0x98 = ExGenRandom 결과 · +0x106 +0x9c
    +0x126 읽기 → +0x12b MOVUPS [블록+0xa0] · +0x132 읽기 → +0x138 MOVSD [블록+0xb0]
```

```
(부팅) Phase1InitializationDiscard call +0xcaa / ret +0xcaf (_calls.tsv) → ExInitializeLeapSecondData
    call +0x3e / ret +0x43  ZwCreateSection(0x1000, 보호 4, SEC_COMMIT, 파일 없음)
    call +0xb5 / ret +0xba  시스템 공간에 붙이기 (같은 주소 이름 2개: MmMapViewInSessionSpace · MmMapViewInSystemSpace)
    call +0x1ab memset → call +0x1b5 / ret +0x1ba ExpReadLeapSecondData → +0x1ba ExLeapSecondData = 쓰기 주소
    +0x19c  ExLeapSecondDataSectionPointer = 섹션 (이 칸을 쓰는 곳은 여기 하나)
MmCreatePeb +0x126  ExLeapSecondDataSectionPointer 읽기 → call +0x12d / ret +0x132 MmMapViewOfSection → PEB +0x7b8
ntdll 읽는 쪽: RtlpTimeToTimeFields +0x1e · RtlpTimeFieldsToTime +0x1d  (MOV …,[PEB+0x7b8], NULL·Enabled 0 이면 건너뜀)
```

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 질문 17 (환경 변수 이름) 만 run44 의 K1 덤프를 서버에서 **읽기 전용**으로 읽었다 (결과서). 날짜는 결과서 날짜.
- 03_sprint 열일곱째 바퀴. plan **Q109 나머지 (B·C·D (1)~(3)·E)** — 사용자 결정 2026-11-01 (PE_emulator 요청 4-B~E). **짐 지도 바퀴** — `baggage.md` 에 새 쓴 자리 (아래 "짐 지도 반영").
- 정적: ntoskrnl (`ntkrnlmp.pdb`) `C29EBFB06B78B3C020DCA66D99713F9E1` · ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — flow.md 기준과 같다 (결과서 `fetch_pdb.py` 출력, 요청서 값 + Age 1).
- 버전 대조 (결과서, `_calls.tsv` `ret_off`): `NtCreateUserProcess +0xc36 → PspAllocateProcess` (call `+0xc31`), `PspInsertThread +0x807` (call `+0x802`), `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` (call `+0x1bb6`), `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitializeInternal +0x5a`, `_LdrpInitialize +0x16c`, `LdrpLoadDll +0x100` — 전부 일치.
- K1 덤프: `ms/dump/03_baggage-snap/K1/memory.dmp` (vmss2core DMP64, 커밋 안 함). run44 와 같은 실행의 같은 순간 (`PspAllocateThread` 입구).

## 목적

- PE_emulator 가 "공사 중 빈 함수" 로 둔 초기화 함수들이 **무엇을 쓰고 어떤 syscall 을 부르는지** 를 `[정적] run59` 근거로 남긴다 (요청 4-B~E).
  - D: 기록 정정 확인 셋 (`_LdrpInitialize +0x10d ~ +0x13e`, 시작 정보 블록 `+0x98` 읽는 곳, `PebLdr.Initialized`·`RtlpFcProcessManager`·스핀 횟수).
  - B: ntdll 본문 — `InitSecurityCookie`·`InitSpecialMachineFrames`·`__cpu_features_init`·`RtlEncodePointer`·`RtlpInitializeNonVolatileFlush`·`RtlGetCurrentServiceSessionId` (ETW 갈래).
  - C: ntoskrnl 비트 뜻 — PEB `BitField` 비트 출처 (ⓐ), `+0x170` 비트 0 (ⓒ), `F` 비트 8 (ⓓ), 만들기 정보 `+0x10` 비트 4·`PspLocateInPEManifest` (ⓔ), `PspGetProcessParameterOverrides` (ⓖ), 옵션 셋째 칸 (ⓗ), PEB `+0x138`·`+0x7c8` (ⓙ), `PspMapSiloSharedDataView` (ⓛ), `LeapSecondData` (ⓑ).
  - E: K1 환경 블록 변수 이름 (ⓕ).
- 범위 밖 (요청서): 실행 값 (깃발 = Q43, `PsDefaultLoaderThreads` = Q79, 스위치 = Q35), ⓠ (run58), Q111.

## 방법

- 정적: `_workspace/sast/54_init-leftovers.md` — 순서 D → B → C → E.
  - 이미 analyze 된 프로젝트 (`.gpr` + `_index.tsv` + `SetPdb:` 줄) — **다시 analyze 안 함**.
  - 새로 뽑음: ntdll B 다섯 함수 depth 1 → 14개 + `LdrpGenSecurityCookie` depth 1, ntoskrnl C 다섯 함수 depth 1 → 18개 + depth 0 여섯 (`PsQueryProcessAttributes`·`ExpGetSystemFlushInformation`·`PspCaptureCreateInfo`·`ExInitializeLeapSecondData`·`ExpReadLeapSecondData`·`ExpParseAndUpdateLeapSecondData`). `_calls.tsv` 지금 ntdll 2,338 · ntoskrnl 4,910줄.
  - 이미 있던 것은 읽기만: ntdll `_LdrpInitialize`·`LdrInitializePerThreadRng`·`LdrpProcessMappedModule`·`LdrpInitializeProcess`·`RtlGetCurrentServiceSessionId`, ntoskrnl `PspAllocateProcess`·`MmCreatePeb`·`PspSetupUserProcessAddressSpace`·`PspApplyMitigationOptions`.
  - `_refs` 새로: ntdll `LdrSystemDllInitBlock`·`0x1e7050`·`RtlpDynamicFunctionTableLock`·`0x1d2408`·`__isa_info`·`SecurityCookieInitCount`·`SpecialMachineFramesInitCount`·`RtlpContinuationContextMachineFrameEntries`·`x86_cpu_enable_*`·`g_SymCrypt*`, ntoskrnl `ExLeapSecondDataSectionPointer`.
  - 그 밖 (결과서): PDB 이름 `pdb_aliases.py --where`, 파일 처음 값 (`.data`·`.mrdata`) 읽기, `PEB+0x7b8` 읽는 함수는 `ntdll.dll` 전체 `objdump` 검색 (레지스터 상대라 `_refs` 로 안 잡힘), import 스텁 바이트, `sample/bin/test.exe` 자원 디렉터리 (`pefile`).
  - 질문 17: 임시 스크립트 (scratchpad, 커밋 안 함) — DUMP_HEADER64 `PhysicalMemoryBlock` 으로 물리 → 파일 오프셋, test.exe `DirectoryTableBase` `0x192c2e000` (K1 로그 328줄) 로 4단계 페이지 걷기 → 환경 `0x25996cb0734`, 크기 `0xd14` (로그 2348·2363줄). 앞 `0x200` 바이트가 K1 로그와 같은지 확인.
- 결과서: `_workspace/sast/54_init-leftovers_result.md` (**분석성공**, PDB 일치, 17문항 모두 답 — 부분 답 둘).
- 이 문서를 쓰며 다시 맞춘 것:
  - 오케스트레이터: `LdrpInitializeProcess +0x162 LEA`·`+0x166 MOV byte [PebLdr+0x4]`, 스핀 6줄 `+0x1cd`·`+0x1dd`·`+0x1e7`·`+0x1f0`·`+0x1fa`·`+0x203`; `InitSecurityCookie` CALL `+0x18`·`+0x38`·`+0x61`, `+0x42` 깃발; `LdrInitSecurityCookie` CALL `+0x1e0`·`+0x212`; `RtlEncodePointer +0xe`·`+0x43`·`+0x50`; `RtlpInitializeNonVolatileFlush +0x1e`·`+0x31`; `PspSetupUserProcessAddressSpace +0xc1`; `PspWritePebAffinityInfoAttached +0x6f`; `KeCopyXfdMaskToPeb` 조각 `MOV [R8+0x7c8]`.
  - flow-writer: 위 3절의 모든 CALL 줄 (`CALL … ret=`) 과 분기 목적지 (`LdrpInitialize`·`InitSecurityCookie`·`InitSpecialMachineFrames`·`RtlEncodePointer`·`RtlpInitializeNonVolatileFlush`·`LdrpInitializationComplete`·`_LdrpInitialize +0x10d ~ +0x13e`), `PspAllocateProcess` `+0x2248`·`+0x2266 ~ +0x2314`·`+0x236e ~ +0x2397`·`+0x2574 ~ +0x25cd`, `PspPrepareSystemDllInitBlock +0x64 ~ +0x138`, `MmCreatePeb +0x126 ~ +0x132`·`+0x2b8 ~ +0x2c9`, `KeInitializeProcess +0x44d ~ +0x473`, `PspLocateInPEManifest`·`PspGetProcessParameterOverrides`·`PspMapSiloSharedDataView`·`PspUpdatePebForAffinityChange`·`ExInitializeLeapSecondData` CALL 줄, `_calls.tsv` 의 `ExInitializeLeapSecondData` 부르는 쪽. KUSD `UserModeGlobalLogger` 칸 모양 (`struct__KUSER_SHARED_DATA.tsv`).
- 문서: 이 run 문서, `flow.md` (기준, 2.1 `PspAllocateProcess`·`MmCreatePeb`·`PspSetupUserProcessAddressSpace`·`PspPrepareSystemDllInitBlock`, 2.4 `LdrpInitialize`·`_LdrpInitialize`·`LdrpInitializeProcess` 묶음 2·run51 쿠키 절, 4장, 5장), `baggage.md` (아래), run51 문서 맨 위 정정 한 줄.

## 결과

비유: 관리실 (ntdll) 이 문을 열 때 하는 잡일 셋 — **금고 비밀번호 정하기** (보안 쿠키), **비상구 지도 표시** (특수 기계 프레임), **장비 점검표** (CPU 기능 바이트). 비밀번호 쪽지는 잠긴 서랍 (`.mrdata`) 에 있어서, 관리실이 서랍 잠금을 잠깐 풀었다가 (`NtProtectVirtualMemory`) 다시 잠근다. 본사 (커널) 는 건물 안내판 (PEB) 의 몇 칸 — "큰 방 쓰나·보호 건물인가·주소가 옮겨졌나" 비트, CPU 친화도, 확장 기능 끄기 칸 — 을 채운다.

### 정적 — 호출 지점 (`ms/disassemble/{ntdll,ntoskrnl}/_calls.tsv`, `.asm` `CALL … ret=`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitialize` | `+0x41` | `+0x46` | `ntdll!InitSecurityCookie` | [정적] |
| `ntdll!LdrpInitialize` | `+0x22` | `+0x27` | `ntdll!InitSpecialMachineFrames` | [정적] |
| `ntdll!LdrpInitialize` | `+0x27` | `+0x2c` | `ntdll!__cpu_features_init` | [정적] |
| `ntdll!InitSecurityCookie` | `+0x18` | `+0x1d` | `ntdll!LdrpGenRandom` | [정적] |
| `ntdll!InitSecurityCookie` | `+0x38` | `+0x3d` | `ntdll!LdrInitSecurityCookie` | [정적] |
| `ntdll!InitSecurityCookie` | `+0x61` | `+0x66` | `ntdll!NtDelayExecution` (기다리는 스레드만) | [정적] |
| `ntdll!LdrInitSecurityCookie` | `+0x75` | `+0x7a` | `ntdll!LdrpGenSecurityCookie` | [정적] |
| `ntdll!LdrInitSecurityCookie` | `+0x1e0` | `+0x1e5` | `ntdll!NtProtectVirtualMemory` (풀기) | [정적] |
| `ntdll!LdrInitSecurityCookie` | `+0x212` | `+0x217` | `ntdll!NtProtectVirtualMemory` (되돌림) | [정적] |
| `ntdll!LdrpGenSecurityCookie` | `+0x8d` | `+0x92` | `ntdll!NtQueryPerformanceCounter` | [정적] |
| `ntdll!InitSpecialMachineFrames` | `+0x39` | `+0x3e` | `ntdll!RtlLookupFunctionEntry` (두 번 돎) | [정적] |
| `ntdll!InitSpecialMachineFrames` | `+0x7c` | `+0x81` | `ntdll!NtDelayExecution` (기다리는 스레드만) | [정적] |
| `ntdll!__cpu_features_init` | `+0xb` · `+0x17` · `+0x45` · `+0x5f` | `+0x10` · `+0x1c` · `+0x4a` · `+0x64` | `ntdll!RtlIsProcessorFeaturePresent` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x12f` | `+0x134` | `ntdll!RtlEncodePointer` | [정적] |
| `ntdll!RtlEncodePointer` | `+0x43` | `+0x48` | `ntdll!NtQueryInformationProcess` (`0x24`, 캐시 0 일 때만) | [정적] |
| `ntdll!RtlEncodePointer` | `+0x5a` | `+0x5f` | `ntdll!RtlRaiseStatus` (실패) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x21c` · `+0x22d` · `+0x254` · `+0x265` | `+0x221` · `+0x232` · `+0x259` · `+0x26a` | `memset` (`RtlpFcProcessManager`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x28d` | `+0x292` | `ntdll!RtlpInitializeNonVolatileFlush` | [정적] |
| `ntdll!RtlpInitializeNonVolatileFlush` | `+0x31` | `+0x36` | 시스템 정보 조회 (`0xc0`) — 같은 주소 이름 2개, 후보: `NtQuerySystemInformation`·`RtlGetNativeSystemInformation` | [정적] |
| `ntdll!LdrpInitializationComplete` | `+0x19` · `+0x67` | `+0x1e` · `+0x6c` | `ntdll!RtlGetCurrentServiceSessionId` | [정적] |
| `ntdll!LdrpInitializationComplete` | `+0xa5` | `+0xaa` | `ntdll!LdrpLogEtwEvent` (ETW 켜졌을 때만) | [정적] |
| `ntdll!LdrpInitializationComplete` | `+0x51` | `+0x56` | `ntdll!NtSetEvent` | [정적] |
| `nt!NtCreateUserProcess` | `+0xab3` | `+0xab8` | `nt!PspGetProcessParameterOverrides` | [정적] |
| `nt!PspGetProcessParameterOverrides` | `+0x34` | `+0x3b` | (간접) `__imp_PsGetProcessParameterOverrides` → 파일 값은 `ret 0` 스텁 | [정적] |
| `nt!PspAllocateProcess` | `+0x1060` | `+0x1065` | `nt!SeQueryInformationToken` (`0x1d`) | [정적] |
| `nt!PspAllocateProcess` | `+0x225f` | `+0x2264` | `nt!PsQueryProcessAttributes` | [정적] |
| `nt!PspAllocateProcess` | `+0x230f` | `+0x2314` | `nt!MmCreatePeb` | [정적] |
| `nt!PspAllocateProcess` | `+0x259e` · `+0x25ae` · `+0x25ba` · `+0x25c8` | `+0x25a3` · `+0x25b3` · `+0x25bf` · `+0x25cd` | `nt!KeCopyXfdMaskToPeb` · `PspUpdatePebForAffinityChange` · `MmMapApiSetView` · `PspMapSiloSharedDataView` (`+0x2574` 갈래 복사본) | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x106` | `+0x10b` | `nt!PspLocateInPEManifest` (`CreateInfo+0x10` 비트 4 일 때) | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x222` | `+0x227` | `nt!KeCopyXfdMaskToPeb` | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x22f` | `+0x234` | `nt!PspUpdatePebForAffinityChange` | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x25e` | `+0x263` | `nt!PspMapSiloSharedDataView` | [정적] |
| `nt!PspLocateInPEManifest` | `+0x70` | `+0x75` | `nt!LdrResSearchResource` | [정적] |
| `nt!PspUpdatePebForAffinityChange` | `+0x54` | `+0x59` | `nt!PspWritePebAffinityInfoAttached` | [정적] |
| `nt!PspWritePebAffinityInfoAttached` | `+0x64` · `+0xc4` | `+0x69` · `+0xc9` | `nt!KeQueryPrimaryGroupAffinityProcess` | [정적] |
| `nt!PspMapSiloSharedDataView` | `+0xb` · `+0x16` | `+0x10` · `+0x1b` | `nt!PsGetProcessServerSilo` · `nt!PsIsHostSilo` | [정적] |
| `nt!PspMapSiloSharedDataView` | `+0xcad2f` | `+0xcad34` | `nt!MmMapViewOfSection` (서버 silo 만) | [정적] |
| `nt!KeInitializeProcess` | `+0x45c` | `+0x461` | `nt!VslCreateSecureProcess` (보안 프로세스만) | [정적] |
| `nt!MmCreatePeb` | `+0x12d` | `+0x132` | `nt!MmMapViewOfSection` (leap second 뷰) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xcaa` | `+0xcaf` | `nt!ExInitializeLeapSecondData` (부팅) | [정적] |
| `nt!ExInitializeLeapSecondData` | `+0x3e` · `+0x1b5` | `+0x43` · `+0x1ba` | `nt!ZwCreateSection` · `nt!ExpReadLeapSecondData` | [정적] |

### 호출 관계

라벨은 ret 자리 (콜스택 숫자). 커널 쪽 (부모 cmd 스레드):

```
NtCreateUserProcess
 ├─ +0xab8  PspGetProcessParameterOverrides → CreateInfo+0x1d8 (스텁이면 0)
 └─ +0xc36  PspAllocateProcess
             ├─ (1) +0x2264  PsQueryProcessAttributes        IsPackagedProcess 재료
             ├─ (2) +0x2314  MmCreatePeb                      견본 복사 → PEB BitField (+0x2b8 SecureState 비트 0 검사)
             └─ (3) +0x239c  PspSetupUserProcessAddressSpace
                          ├─ (1) +0x10b  PspLocateInPEManifest            조건부 (CreateInfo+0x10 비트 4)
                          ├─ (2) +0x227  KeCopyXfdMaskToPeb               PEB+0x7c8 (KeFeatureBits 비트 0x37 일 때만)
                          ├─ (3) +0x234  PspUpdatePebForAffinityChange
                          │             └─ +0x59  PspWritePebAffinityInfoAttached   PEB+0x138
                          ├─ (4) +0x255  MmMapApiSetView                  PEB+0x68 (run57)
                          └─ (5) +0x263  PspMapSiloSharedDataView         서버 silo 없으면 바로 0
```

ntdll 쪽 (test.exe 첫 스레드):

```
LdrInitializeThunk
 └─ +0xe  LdrpInitialize
           ├─ (1) +0x46  InitSecurityCookie
           │              ├─ (1) +0x1d  LdrpGenRandom
           │              └─ (2) +0x3d  LdrInitSecurityCookie
           │                             ├─ (1) +0x7a   LdrpGenSecurityCookie → +0x92 NtQueryPerformanceCounter
           │                             ├─ (2) +0x1e5  NtProtectVirtualMemory   (풀기)
           │                             └─ (3) +0x217  NtProtectVirtualMemory   (되돌림)
           ├─ (2) +0x27  InitSpecialMachineFrames → +0x3e RtlLookupFunctionEntry (두 번)
           ├─ (3) +0x2c  __cpu_features_init → RtlIsProcessorFeaturePresent (네 번)
           └─ ⇒ LdrpInitializeInternal → +0x5a _LdrpInitialize   (+0x10d ~ +0x13e 동적 함수표 전역)
                                                └─ +0x16c LdrpInitializeProcess
                                                           ├─ (1) +0x134  RtlEncodePointer → +0x48 NtQueryInformationProcess(0x24)
                                                           ├─ (2) +0x158  LdrInitializePerThreadRng
                                                           │      +0x166 PebLdr.Initialized · +0x1cd ~ +0x203 스핀 · +0x21c ~ +0x286 RtlpFcProcessManager
                                                           └─ (3) +0x292  RtlpInitializeNonVolatileFlush → +0x36 시스템 정보 0xc0
```

## 확인된 것

D. 기록 정정 확인 (셋 다 PE_emulator 쪽 asm 읽기가 맞다)

- `[정적]` `_LdrpInitialize +0x10d ~ +0x13e` 가 동적 함수표 전역 일곱 줄을 쓴다 (`+0x114`·`+0x11b` 목록 머리 = 자기, `+0x122` 락 0, `+0x129 ~ +0x13e` Tree 넷 0). 값은 `RBX` = 0 (`+0x28 XOR EBX,EBX`). 첫 스레드 길만 (`+0x3a CMPXCHG` 0 → `+0x5a JZ +0xcf`). Tree 넷·락은 파일 처음 값도 0 이라 **값이 안 바뀐다** — 목록 머리만 0 → 자기 주소.
- `[정적]` 시작 정보 블록 `+0x98` 을 읽는 곳은 `LdrInitializePerThreadRng +0x4`·`LdrpProcessMappedModule +0xe7` 이 맞다 (둘 다 dword). 전체는 **다섯 곳** — 그 둘 + `InitSecurityCookie +0x1d` (쿠키 씨앗) · `RtlpInitRandomExVector +0x3d` · `AvrfMiniLoadDll +0x83c`. 쓰는 쪽은 커널 `PspPrepareSystemDllInitBlock +0x100` 뿐 (`ExGenRandom`).
- `[정적]` `PebLdr.Initialized` = 1 은 **`+0x166`** (`MOV byte [PebLdr+0x4],R15B`). `+0x162` 는 값 1 을 레지스터에 만드는 `LEA` 다. baggage 3.4 의 `+0x166` 이 이미 맞았다.
- `[정적]` `RtlpFcProcessManager` (`0x118`B) 는 `memset` 넷 (call `+0x21c` 전체 · `+0x22d` `+0x20`부터 `0xd0` · `+0x254` `+0x20`부터 `0x60` · `+0x265` `+0x80`부터 `0x60`) + 8바이트 0 넷 (`+0x237`·`+0x23e`·`+0x26a`·`+0x271`) 으로 전부 0, 그 뒤 `+0x27f`·`+0x286` 이 `+0x108` 빈 원형 목록 머리. `Fc` 뜻은 코드에 없다 `[공백]`.
- `[정적]` 스핀 횟수는 **전역 셋, 쓰기 여섯 줄** (run18 "스핀 3곳" 은 전역 수). 먼저 0, `PEB.NumberOfProcessors` > 1 이면 `0x2800` 으로 덮음.

B. ntdll 본문

- `[정적]` `LdrpInitialize` 는 쿠키 깃발 0 → `InitSecurityCookie`, 특수 프레임 깃발 0 → `InitSpecialMachineFrames`, **`__cpu_features_init` 은 깃발 없이 매번** (나중 스레드도 부름), 끝에 `LdrpInitializeInternal` 꼬리 점프.
- `[정적]` `InitSecurityCookie`: 순번 칸 `SecurityCookieInitCount` `XADD` (+1) → 순번 1 만 초기화. 씨앗 = `LdrSystemDllInitBlock +0x98` dword ^ `LdrpGenRandom` (`RDRAND`, 없으면 0). 쿠키 식은 `LdrInitSecurityCookie` 안: `LdrpGenSecurityCookie` (KUSD 시각·틱·TEB `ClientId`·주소 + `NtQueryPerformanceCounter`) ^ 씨앗 섞기, 48비트. **현재 값이 파일 기본값 `0x2b992ddfa232` 일 때만 바꾼다.** `.mrdata` 라 `NtProtectVirtualMemory` 로 8바이트를 `PAGE_READWRITE` 로 풀고 쓰고 되돌린다. 그다음 `+0x42` `SecurityCookieInitialized` = 1. 다른 스레드는 30ms 씩 `NtDelayExecution` 하며 깃발을 기다린다.
- `[정적]` `InitSpecialMachineFrames`: 같은 순번 꼴 (`SpecialMachineFramesInitCount`). `RtlpContinuationContextMachineFrameEntries[0..1]` (파일 값 = ntdll+`0x164e10` `KiUserExceptionDispatcher` 쪽 · ntdll+`0x164ca0` `KiUserApcDispatcher` 쪽, 이름은 결과서 PDB 대조) 를 그 함수의 `UnwindData` 주소로 **바꿔 쓴다**. syscall 없음 (`RtlLookupFunctionEntry` 는 ntdll 안). `+0x57` 깃발 = 1. 표의 뜻 ("커널 → 유저 진입 함수의 풀림 정보인가") 은 `[추측]`.
- `[정적]` `__cpu_features_init`: `__isa_info` (파일 값 2) 한 바이트 = `1 | (0x2a→2) | (0x27→4) | (0x26→8) | (0x28→0x10)` (`RtlIsProcessorFeaturePresent` 넷, KUSD `ProcessorFeatures` 읽기만). 번호 이름 (SSE4.2·AVX·AVX2·ERMS) 은 `[추측]`. syscall·갈래 없음.
  - run44 사진 `P1b~P2` 에 바뀐 CPU 기능 전역 6개 (`x86_cpu_enable_*` 셋·`g_SymCrypt*` 셋) 는 **이 함수가 아니라** `LdrpInitializeProcess` 뒤쪽 `+0x1d75 ~ +0x1e27` 과 `SymCryptDetectCpuFeaturesByCpuid +0xee`·`+0xf4` 가 쓴다 — 사진 구간 (`P1b` 뒤) 과 맞다.
- `[정적]` 힌트 이름 `SecurityCookieInitCount` (`0x1d23d4`)·`SpecialMachineFramesInitCount` (`0x1d23f0`)·`__isa_info` (`0x1ccf00`) 는 셋 다 PDB 이름이다 (`pdb_aliases.py --where`).
- `[정적]` `RtlEncodePointer`: 캐시 = PDB 이름 `?CookieValue@?1??RtlpGetCookieValue@@9@9` (`.data` `0x1d2408`, 함수 안 `static`, `RtlDecodePointer` 와 같은 칸). 캐시 ≠ 0 이면 `(포인터 ^ 쿠키) ROR (쿠키 & 0x3f)` 로 끝 — syscall 없음. 0 이면 `NtQueryInformationProcess(-1, 0x24, &버퍼, 4, NULL)` 한 번 → 성공하면 `+0x50` 캐시 저장. 그래서 **syscall 은 처음 한 번** (성공하면 그 뒤 `RtlEncodePointer`·`RtlDecodePointer` 는 캐시).
- `[정적]` **이 프로세스의 첫 `NtQueryInformationProcess(0x24)` 는 `LdrpInitializeProcess +0x12f RtlEncodePointer(0)` 안** (`+0x43`). 힙 관리자 쪽 `RtlInitializeHeapManager +0x9d → RtlpGetModifiedProcessCookie +0x22` (캐시 없이 매번, 값에 곱·더하기 나머지) 는 코드 순서상 둘째 (`+0x9ed` > `+0x12f`). 커널 `EPROCESS.Cookie` (+0x2b8) 를 처음 만드는 호출도 앞쪽 (아래 "정정").
- `[정적]` `RtlpInitializeNonVolatileFlush`: 시스템 정보 클래스 `0xc0` (nt PDB 열거형 `SystemFlushInformation`, 192) 를 `0x20`B 버퍼로. 커널 `ExpGetSystemFlushInformation` 이 채운다 (`+0x0` 플래그 비트 0 늘 1 · 비트 1·2 = `KeFeatureBits` 비트 35·36, `+0x4` = `KPRCB+0xe8` 캐시 줄 크기, `+0x8` = `ExpPlatformFlushCapabilities`, 없으면 ACPI `NFIT`). 성공했을 때만 `RtlpIsFlushRequired`·`RtlpOptimalFlushMethod`·`RtlpIsDrainRequired`·`RtlpClFlushSize` 를 쓴다 (2절 표). 비트가 어느 CPU 명령인지는 `[공백]`.
- `[정적]` `RtlGetCurrentServiceSessionId`: `PEB.SharedData` (`+0x90`) 가 NULL 이면 0. test.exe 는 K1 사진 `PEB+0x90 = (null)` `[관찰]` run44 → 0.
- `[정적]` ETW 갈래 (`LdrpInitializationComplete`): 세션 번호 0 이면 검사 칸 = **KUSD `0x7ffe0384`** — PDB `UserModeGlobalLogger` (`unsigned short[16]`, `+0x380`) 의 **`[2]` 원소 아래 바이트** (윗 바이트는 `+0x385`). 0 이 아니고 + `PEB.TracingFlags` 비트 2 (`LibLoaderTracingEnabled`) + 윗 바이트 비트 5 일 때만 `LdrpLogEtwEvent`. 어느 쪽이든 `+0x44` 에서 `LdrpProcessInitialized` 1 → 2, 이벤트가 있으면 `NtSetEvent`. 실제 값은 동적.

C. ntoskrnl 비트 뜻

- `[정적]` ⓐ PEB `BitField` (`+0x3`) 는 `PspAllocateProcess` 가 **견본** (`[RSP+0x1a8]`, `+0x1ab` = `BitField`) 을 만들어 `MmCreatePeb` 에 넘긴 것을 복사한다 (2절 표 질문 8): 비트 0 `ImageUsesLargePages` ← `F` 비트 12 (큰 페이지 요청 — 9번째 인자 `0x10` 또는 IFEO `UseLargePages`, 이름 `LARGE_PAGES` 는 `[추측]`) · 1 `IsProtectedProcess` ← `Protection.Type ≠ 0` · 2 `IsImageDynamicallyRelocated` ← `F` 비트 13 (`ImageFlags` 비트 2) · 3 `SkipPatchingUser32Forwarders` 안 켬 · 4 `IsPackagedProcess` ← 토큰 `WIN://PKG` 속성 · 5 `IsAppContainer` ← `SeQueryInformationToken(0x1d)` (번호 이름은 `[추측]`) · 6 `IsProtectedProcessLight` ← `Type == 1` · 7 `IsLongPathAwareProcess` 안 켬. `[관찰]` run44 K1 `BitField = 0x4`·`ImageFlags = 0x4`·`Mutant = -1` (`ms/logs/04_baggage-read_K1.txt`, 결과서) 과 맞다.
- `[정적]` ⓒ 프로세스 `+0x170` = `_KPROCESS.SecureState` (PDB), 비트 0 = `Flags.SecureProcess` ("보안 프로세스", VSL). 켜는 곳은 `KeInitializeProcess +0x473` 하나 (`VslCreateSecureProcess` 성공 뒤). `MmCreatePeb +0x2b8` 은 켜져 있으면 이미지 헤더 대신 고정값 (서브시스템 3, 특성 `0x22`). test.exe 는 꺼짐 `[추측]` (값은 동적).
- `[정적]` ⓓ `F` 비트 8 의 효과 = **"PEB 를 가지는 프로세스"** — 최소 프로세스 아님 (`F` 비트 1 꺼짐) + 부모·섹션 없는 시스템류 아님 (`F` 비트 0 꺼짐). 꺼지면 `PsQueryProcessAttributes`·`MmCreatePeb`·`PspSetProcessAffinitySafe` 를 건너뛴다. 이름은 코드에 없다 `[추측: 이름]`. test.exe 는 `F` = 0 → 비트 8 켜짐.
- `[정적]` ⓔ 만들기 정보 `+0x10` 비트 4 = 사용자 `PS_CREATE_INFO.InitFlags` 비트 1 을 `PspCaptureCreateInfo +0x77 ~ +0x95` 가 옮긴 것 (이름 `DetectManifest` 는 phnt `[추측]`, 사용자 쪽 켜는 줄 `KERNELBASE!CreateProcessInternalW +0x2add` — 결과서). `PspLocateInPEManifest` 는 exe 의 `RT_MANIFEST` ID 1 자원을 찾아 `+0x10` 비트 5·`+0x130` 포인터·`+0x138` 크기·매개변수 사본 `Flags` 비트 13 을 켠다. 못 찾음 코드는 성공으로 바꾼다. **test.exe (`sample/bin/`) 는 자원 디렉터리가 없어** 불려도 아무것도 안 켜진다 `[정적]` (PE 헤더) — K1 사진 `Flags = 0x4001` (비트 13 꺼짐) `[관찰]` 과 맞다. 탔는지는 동적 (Q114 6).
- `[정적]` ⓖ `PspGetProcessParameterOverrides` 가 쓰는 곳은 **만들기 정보 (커널 사본) `+0x1d8` 하나** (8B 포인터). 간접 import 슬롯의 **PE 파일 값은 `ret 0` 스텁**이라 파일 값 그대로면 0 을 쓴다. 매개변수 사본은 안 건드리고, `PsDefaultLoaderThreads` 도 안 읽는다 (읽는 곳은 `PspSetupUserProcessAddressSpace +0xc1` 하나 — run32 그대로). `+0x1d8` 을 읽는 쪽은 `[공백]` (Q113).
- `[정적]` ⓗ 시작 정보 블록 `+0xa0 ~ +0xb7` = 만들기 정보 `+0x150`·`+0x160` 의 24바이트 옵션 표 (`PspPrepareSystemDllInitBlock +0x64 ~ +0x78` 읽기 → `+0x12b`·`+0x138` 쓰기). 표는 `PspApplyMitigationOptions` 가 제자리에서 고친다 (`+0x5ef`·`+0x5f4`). 옵션 40개 × 4비트, 셋째 8바이트 = 옵션 32~39. 사진 `0x02002002` = 비트 0~1·12~13·24~25 가 각 2 → `== 1` 효과 없음. 0~1 의 2 는 `+0x510 ~ +0x518` (`CetUserShadowStacks` 꺼짐) 이 쓴 것. **12~13·24~25 의 2 는 입력 표가 갖고 온 값** — 출처 `[공백]` (Q113). ntdll `LdrpInitializeProcess +0x4b3`·`+0xa09` 는 이 8바이트를 스택 사본으로만 옮기고 **비트를 읽지 않는다**.
- `[정적]` ⓙ PEB `+0x138` `ActiveProcessAffinityMask` = `PspWritePebAffinityInfoAttached +0x6f` (`KeQueryPrimaryGroupAffinityProcess` 의 `Mask`, 바뀌면 다시 돎). K1 사진 `3` `[관찰]` 과 맞다. PEB `+0x7c8` `ExtendedFeatureDisableMask` = `KeCopyXfdMaskToPeb` 조각 `+0x7` (`KPROCESS +0x198` 값), `KeFeatureBits` 비트 `0x37` 일 때만 — 사진 0 이라 안 탔는지 0 을 쓴 건지 못 가림 (Q114 3 · Q79).
- `[정적]` ⓛ `PspMapSiloSharedDataView` 는 서버 silo 가 없으면 **아무것도 안 쓰고 0** (`+0x25`). 서버 silo 일 때만 PEB `+0x90` `SharedData` = 뷰 주소. test.exe 는 `PEB+0x90` NULL (K1 `[관찰]`).
- `[정적]` ⓑ PEB `+0x7b8` `LeapSecondData` 를 읽는 ntdll 함수는 `RtlpTimeToTimeFields +0x1e`·`RtlpTimeFieldsToTime +0x1d` 둘. 섹션은 **부팅 때 한 번** `ExInitializeLeapSecondData` (← `Phase1InitializationDiscard +0xcaa`) 가 만든다 — 크기 `0x1000` 익명 섹션, 내용 `_LEAP_SECOND_DATA` (`Enabled` 1B · `Count` 4B · `Data[Count]` 8B씩, 최대 511개), 값은 레지스트리 `Enabled`·`LeapSeconds`. 프로세스마다 `MmCreatePeb +0x126` 이 읽기용 뷰를 붙인다.

E. 덤프

- `[관찰]` (K1 덤프, 결과서) test.exe 환경 블록 (`0x25996cb0734`, `0xd14`B) 의 변수 **이름 47개** (값은 안 적음, 정렬 순서 그대로): `=::` · `=C:` · `=ExitCode` · `ALLUSERSPROFILE` · `APPDATA` · `CommonProgramFiles` · `CommonProgramFiles(x86)` · `CommonProgramW6432` · `COMPUTERNAME` · `ComSpec` · `DriverData` · `EFC_5052_1262719628` · `EFC_5052_1592913036` · `EFC_5052_2283032206` · `EFC_5052_2775293581` · `EFC_5052_3789132940` · `EFC_5052_4126798990` · `HOMEDRIVE` · `HOMEPATH` · `LOCALAPPDATA` · `LOGONSERVER` · `NUMBER_OF_PROCESSORS` · `OneDrive` · `OS` · `Path` · `PATHEXT` · `PROCESSOR_ARCHITECTURE` · `PROCESSOR_IDENTIFIER` · `PROCESSOR_LEVEL` · `PROCESSOR_REVISION` · `ProgramData` · `ProgramFiles` · `ProgramFiles(x86)` · `ProgramW6432` · `PROMPT` · `PSModulePath` · `PUBLIC` · `SESSIONNAME` · `SystemDrive` · `SystemRoot` · `TEMP` · `TMP` · `USERDOMAIN` · `USERDOMAIN_ROAMINGPROFILE` · `USERNAME` · `USERPROFILE` · `windir`. 끝은 빈 항목 둘 (크기 `0xd14` 와 맞음).
  - `=` 로 시작하는 셋은 이름 앞 `=` 가 이름 일부 (드라이브별 현재 폴더·종료 코드 관례). `EFC_5052_*` 6개는 이 VM 부모 셸 쪽이 넣은 것 `[추측]`.
  - 이 VM 부모 셸이 물려준 환경이다. 에뮬레이터가 베낄 값은 아니다 (결과서).

## 정정

- **run51 (그리고 baggage 3.11 `Cookie` 줄·5장 ②, flow.md 2.4 run51 쿠키 절)** — "`RtlInitializeHeapManager → RtlpGetModifiedProcessCookie` 의 `NtQueryInformationProcess(0x24)` 가 커널 칸 `EPROCESS.Cookie` 를 처음 만든다" 는 틀렸다. 코드 순서상 첫 호출은 **`LdrpInitializeProcess +0x12f` (ret `+0x134`) `RtlEncodePointer(0)` → `+0x43` (ret `+0x48`) `NtQueryInformationProcess(0x24)`** 이다 (캐시가 0 이라 syscall 을 탐). 힙 관리자 쪽 (`+0x9ed`) 은 이미 만들어진 값을 읽는다. 커널 쪽 동작 (0 이면 한 번 만듦, sast/45) 은 그대로. run51 문서 맨 위에 한 줄 넣음. baggage·flow.md 는 옛 글에 줄을 긋고 고쳐 씀.
- **`PspSetupUserProcessAddressSpace +0x227 · +0x234 · +0x263`** (요청서·baggage 7.5·5장 ①, flow.md 2.1 대표 줄) 은 ret 자리다 — call 은 `+0x222 · +0x22f · +0x25e`. flow.md 2.1 "순서 전체" 는 이미 call / ret 둘 다 적혀 있어 맞다. baggage·flow.md 의 그냥 `+0x..` 로 적힌 곳에 "(ret)" 을 붙였다.
- **run32 순서 표에 빠진 것** (run32 문서는 안 고침 — 이 절과 flow.md 2.1): `PspAllocateProcess` 에도 `KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`·`MmMapApiSetView`·`PspMapSiloSharedDataView` 를 **직접 부르는 복사본**이 있다 (call `+0x259e`·`+0x25ae`·`+0x25ba`·`+0x25c8`, ret `+0x25a3`·`+0x25b3`·`+0x25bf`·`+0x25cd`). `+0x236e JNS` (`F` 비트 7 꺼짐) · `+0x2377 JZ` (11번째 인자 0) · `+0x2380 JNZ` (최소 프로세스) 중 하나로 `PspSetupUserProcessAddressSpace` (call `+0x2397`) 를 건너뛸 때 `+0x2574` 로 와서, `F` 비트 8 켜짐 (`+0x2574 BT`) · 비트 4 꺼짐 (`+0x257e TEST BL,0x10`) 이면 돈다. test.exe 는 `PspSetupUserProcessAddressSpace` 쪽 `[추측]` (run32 그대로, 동적 Q35). 또 run32 순서 표엔 `+0x106 PspLocateInPEManifest` 가 없다 — `+0xfa` 비트 4 조건 아래라서.
- **요청서 짐작 `+0x162`** (`Initialized` = 1) → 실제 쓰기는 `+0x166`. `+0x162` 는 값 1 을 만드는 `LEA`. baggage 3.4 는 처음부터 `+0x166` 이라 안 고침 (값 칸에 `+0x162` 출처만 보탬).
- **결과서 숫자 몇 개** (뜻은 같고 줄만, 결과서는 안 고침 — 이 문서·flow.md·baggage 는 `.asm` 값으로 씀):
  - 질문 13·짐 지도용 표 마지막 줄 `PspPrepareSystemDllInitBlock +0x126 MOVUPS [RBX+0xa0]` → `+0x126` 은 스택 읽기, 블록 쓰기는 **`+0x12b`** (`+0xb0` 은 `+0x132` 읽기 → `+0x138` 쓰기). run32 가 맞다.
  - 질문 16 `ExInitializeLeapSecondData` "call `+0x3a` (ret `+0x43`) `ZwCreateSection`" → **call `+0x3e`**. "`MmMapViewInSystemSpace`, call `+0xb1`" → **call `+0xb5`** (ret `+0xba`, 같은 주소 이름 2개: `MmMapViewInSessionSpace`·`MmMapViewInSystemSpace`). "`ExpReadLeapSecondData` (call `+0x1b1`)" → **call `+0x1b5`** (ret `+0x1ba`). 셋 다 4바이트씩 앞 줄을 적은 것.
  - 질문 16 "`MmCreatePeb` … `+0x132` `MmMapViewOfSection`" 은 ret 자리 (call `+0x12d`). baggage 3.1 의 `+0x132` 도 ret — "(ret)" 을 붙임.
  - 질문 7 "KUSD `+0x380` 은 `UserModeGlobalLogger[16]` … **둘째 원소**의 아래 바이트" → PDB 타입이 `unsigned short[16]` 이라 `+0x384` 는 **`[2]` (셋째 원소)** 의 아래 바이트다 (`struct__KUSER_SHARED_DATA.tsv`). 갈래 뜻은 같다.
- **baggage 7.1 "전역 281"** (끝까지 0 인 모르는 전역 수) 은 run45·run46 이 닫은 끝까지 0 전역 7개 (1 + 6) 를 안 뺀 옛 숫자였다 (2.1 은 274 로 맞게 셌음). 이번에 274 → 271 로 고치며 같이 바로잡음 (flow-writer 셈).
- 이전 run 문서 정정 줄은 run51 하나만 넣었다. run18 의 "스핀 3곳" 은 전역 수로 읽으면 맞아서 안 고쳤다.

## 짐 지도 반영 (`baggage.md`)

- 2절 표대로 3장에 쓴 자리를 넣었다 (근거 칸 `sast54`, 줄 끝 **"Q109 로 닫힘"**):
  - 3.1 `_PEB` **새 줄 4** — `BitField` 비트 8칸 (견본 출처, 한 줄) · `+0x90` `SharedData` (조건부 안 탐) · `+0x138` `ActiveProcessAffinityMask` · `+0x7c8` `ExtendedFeatureDisableMask` (조건부). 모르던 칸 **11** → 아는 칸 (모두 nt, 정확). `+0x7b8` 줄에 읽는 ntdll 함수·섹션 출처 보탬.
  - 3.2 매개변수 **새 줄 1** — `Flags` 비트 13 (`PspLocateInPEManifest +0x99`, 조건부 안 탐). 칸 수 그대로.
  - 3.4 `Initialized` 줄 값 칸에 `+0x162` 출처.
  - 3.8 `.data` **새 줄 20** — 전역 20개 닫힘 (`≤P1a` 13 · `P1b~P2` 6 · `끝까지 0` 1). `SecurityCookieInitialized`·`SpecialMachineFramesInitialized`·`LdrpIsSecureProcess` 줄 근거 칸에 run59.
  - 3.9 `.mrdata` **새 줄 2** — 동적 함수표 전역 5개 닫힘 (`≤P1a` 3 · `끝까지 0` 2). `__security_cookie` 줄 짐작 → **정확** (`LdrInitSecurityCookie +0x1e9`). `LdrSystemDllInitBlock` 줄에 `+0x98` 읽는 곳·`+0xa0 ~ +0xb7` 출처.
  - 3.11 `Cookie` 줄 경로 정정, 3.12 `SecureState` 줄에 뜻.
- 2장 숫자 다시 셈: 구조체 아는 칸 704 → **715** (정확 649 → 660) · 모름 335 → **324** (`_PEB` 86 → 75), 전역 쓴 자리 아는 것 80 → **105** (`.data` 69 → 89 · `.mrdata` 10 → 15) · 모름 413 → **388**. 3장 줄 620 → **647**.
- 7.3 정리 (25개 빼고 `(해결됨 → run59)` 줄), 7.2 `_PEB` 11칸, 7.5 실마리 보탬 (결과서 표 밖이라 안 셈: `RtlpUnhandledExceptionFilter` `+0x134`, `LdrpEnforceIntegrityContinuity`·`LdrpAuditIntegrityContinuity`), 4장 짝 3줄 (+ `LeapSecondData`·`LdrSystemDllInitBlock` 짝 2줄 고침), 5장 ①·② 순서, 6장 공용 부품 3줄 (첫 스레드만 하는 일 꼴 · `RtlEncodePointer`/`RtlDecodePointer` · `RtlIsProcessorFeaturePresent`), 7.1 숫자 정정 (전역 끝까지 0 모름 `281` 은 run45·run46 몫을 안 뺀 옛 숫자 → 274 → 271), 7.6 (leap second 섹션 내용·만들기 정보 칸·환경 변수 이름), 새 7.19 (동적 질문), 8장, 1장 그림 숫자.

## 아직 확인 필요

결과서 "끝낸 것 / 못 한 것" 과 "동적분석으로 확인해야 할 것".

- **정적으로 남은 것 — 정적 Q113**:
  1. (질문 13) 옵션 표 셋째 칸 비트 12~13·24~25 의 "2" 가 처음 어디서 오나 — 만들기 정보 `+0x160` 을 채우는 `PspBuildCreateProcessContext`·부모 상속 (`PspInheritMitigationOptions`) 쪽. 옵션 번호 ↔ 이름 표 (PDB 에 열거형 없음).
  2. (질문 12) 만들기 정보 `+0x1d8` 을 읽는 함수.
  - (질문 밖, 대기열 번호 없음) 지역 플래그 `F` 비트 18~26 (섹션 정보 뒤 4바이트에서 옴), `F` 비트 4 뜻 (`+0x257e`), `RtlpFcProcessManager` 의 `Fc` 뜻, flush 비트가 어느 CPU 명령인지.
- **동적 후보 1~10 — 동적 Q114** (결과서 번호 그대로, 3 은 **Q79** 에도 있음):
  1. `RtlpInitializeNonVolatileFlush` 결과 — `ntdll!RtlpInitializeNonVolatileFlush+0x36` (ret 자리) 에서 `dq @rsp+0x20 L4`, 뒤에 전역 넷 (`0x1cd0c0`·`0x1cd0b0`·`0x1cd0b8`·`0x1ce749`).
  2. `__isa_info` (`ntdll+0x1ccf00`) — `ntdll!__cpu_features_init+0x7a` (`+0x74` 쓰기 바로 뒤).
  3. `PEB+0x7c8`·`KeFeatureBits` 비트 `0x37` — `KeCopyXfdMaskToPeb +0x0` 에서 `dq nt!KeFeatureBits` (덤프로도 됨 — Q79 와 같은 읽기).
  4. `__imp_PsGetProcessParameterOverrides` 슬롯이 실행 때도 `ret 0` 스텁인가 — `dq nt!_imp_PsGetProcessParameterOverrides L2`.
  5. `EPROCESS+0x170` (`SecureState`) = 0 — `MmCreatePeb +0x2b8` 에서 `db @r13+0x170 L1`.
  6. `PspLocateInPEManifest` 가 불리나 — 입구 (`/p` test.exe) 또는 `PspSetupUserProcessAddressSpace +0xfa` 에서 `db @rsi+0x10 L1` (결과는 같아서 낮음).
  7. 옵션 셋째 칸 12~13·24~25 의 2 출처 — `PspApplyMitigationOptions` 입구에서 `dq @r8 L3` (입구·출구 비교).
  8. leap second — `dq nt!ExLeapSecondData L1` (`Enabled`·`Count`).
  9. ETW 갈래 — `LdrpInitializationComplete +0x2c` 에서 `db 7ffe0384 L2`, `dd @$peb+378 L1`.
  10. `NumberOfProcessors` 와 스핀 3칸 — `LdrpInitializeProcess +0x20d` 에서 `dd @$peb+b8 L1`, `dd ntdll+1d41a4 L1`.
- 결과서 표 밖이라 안 셌지만 `.asm` 에 쓰는 줄이 보이는 전역 (다음 바퀴에 결과서로 닫을 후보): `RtlpUnhandledExceptionFilter` (`LdrpInitializeProcess +0x134`, `RtlEncodePointer(0)` 결과), `LdrpEnforceIntegrityContinuity`·`LdrpAuditIntegrityContinuity` (`+0x4c4 ~ +0x4fe`, 결과서 질문 13 끝). baggage 7.5.

## 도구 메모

- 결과서 "스크립트 문제" 없음.
- **결과서 숫자 넷이 CALL 앞 줄·읽는 줄을 적었다** (`+0x126` vs `+0x12b`, `+0x3a`·`+0xb1`·`+0x1b1` vs `+0x3e`·`+0xb5`·`+0x1b5`). run56~58 에 이어 네 번째 — 이번엔 call/ret 섞임이 아니라 "명령 하나 앞" 이다. 결과서 양식에 "`.asm` 줄을 그대로 붙여 넣기 (주소 포함)" 를 넣자는 제안은 보고에만 (하네스는 안 고침).
- 덤프에서 유저 가상 주소를 읽는 임시 스크립트 (DUMP_HEADER64 → 물리 블록 → CR3 4단계) 는 scratchpad 에만 있다. 짐 지도 사진 읽기 (Q79) 에 다시 쓸 만하다 — windbg-kd-runbook 이 아니라 dast 쪽 도구 후보.
