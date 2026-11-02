# 54 init-leftovers — 정적 분석 결과

- 요청서: `_workspace/sast/54_init-leftovers.md` (Q109 나머지 — D·B·C·E, 요청 17문항)
- 모듈 / 바이너리: ntdll (`ms/bin/…/System32/ntdll.dll`), ntoskrnl (`…/ntoskrnl.exe`)
- PDB GUID 확인 (`fetch_pdb.py` 출력, 요청서 값 뒤에 Age `1` 이 붙은 모양): **둘 다 일치**
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (요청서 `…F9E` + Age 1)
  - ntdll `C3093720177DA6851519DCFFE8DF53FE1` (요청서 `…FE` + Age 1)
- 분석: 둘 다 이미 되어 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.**
- 뽑은 것 (`export`, 모두 새로 만든 `.asm`·`.c`):
  - ntdll: B 다섯 함수 depth 1 → 14개, `LdrpGenSecurityCookie` depth 1 (쿠키 줄기를 끝까지 보려고 보탬)
  - ntoskrnl: C 다섯 함수 depth 1 → 18개, 그 밖에 `PsQueryProcessAttributes`·`ExpGetSystemFlushInformation`·`PspCaptureCreateInfo`·`ExInitializeLeapSecondData`·`ExpReadLeapSecondData`·`ExpParseAndUpdateLeapSecondData` (각 depth 0, 질문 답에 필요해서 보탬)
  - 이미 있던 것은 다시 안 뽑음: ntdll `_LdrpInitialize`·`LdrInitializePerThreadRng`·`LdrpProcessMappedModule`·`LdrpInitializeProcess`·`RtlGetCurrentServiceSessionId`, ntoskrnl `PspAllocateProcess`·`MmCreatePeb`·`PspSetupUserProcessAddressSpace`·`PspApplyMitigationOptions`
  - `_refs` 새로: ntdll `LdrSystemDllInitBlock`(0x128)·`0x1e7050`(0x30)·`RtlpDynamicFunctionTableLock`·`0x1d2408`·`__isa_info`·`SecurityCookieInitCount`·`SpecialMachineFramesInitCount`·`RtlpContinuationContextMachineFrameEntries`·`x86_cpu_enable_*`·`g_SymCrypt*`, ntoskrnl `ExLeapSecondDataSectionPointer`
- 버전 확인 (`_calls.tsv` `ret_off`, 스킬 "알려진 사실" 대조): **전부 일치**
  - `NtCreateUserProcess +0xc36 → PspAllocateProcess` (call `+0xc31`), `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` (call `+0x802`)
  - `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` (call `+0x1bb6`), `LdrpInitializeNode +0x16b/+0x19c`
  - `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitialize +0x3c` 꼬리호출 → `LdrpInitializeInternal`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess`, `LdrpLoadDll +0x100`
- 스크립트 문제: 없음.
- **끝낸 것 / 못 한 것**: 17문항 모두 답이 있다. 다만 아래는 **부분 답**이다 (다음 바퀴 몫).
  - 13: 셋째 칸 `0x02002002` 에서 비트 12~13·24~25 의 "2" 가 처음 어디서 오는지 (입력 쪽) — 안 봄. 옵션 번호 ↔ 이름 표도 못 맞춤.
  - 12: `CreateInfo +0x1d8` 를 읽는 함수 — 안 봄. 확장 모듈(`ext-ms-win-ntos-processparameters`)이 실제로 걸리는지는 동적.
  - 8: 지역 플래그 비트 18~26 (섹션 정보 뒤 4바이트에서 옴) — 질문 밖이라 안 봄.

## 읽는 법 (오프셋 약속)

- `call +0x..` = CALL 명령이 있는 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리.
  `_refs` 의 `함수+0x..` 는 **명령 시작 자리**다 (ret 자리가 아님). 그 명령이 마침 CALL 바로 다음 줄이면 "(= 앞 call 의 ret)" 라고 적었다.
- 분기 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 빼서** 오프셋으로 적었다 (주소 끝자리를 오프셋으로 읽지 않음).
- 요청서가 적은 `PspSetupUserProcessAddressSpace +0x227 · +0x234 · +0x263` 은 call 자리가 아니라 **ret 자리**다 (call 은 `+0x222 · +0x22f · +0x25e`). 요청서의 `+0x106` (PspLocateInPEManifest) 은 **call** 자리 (ret `+0x10b`).
- 요청서의 `LdrpInitialize +0x41 · +0x22 · +0x27` 은 모두 **call 자리** (ret `+0x46 · +0x27 · +0x2c`).

---

## 질문별 답 [정적]

### D. 기록 정정 확인 (ntdll)

**1. `_LdrpInitialize +0x10d ~ +0x13e` 가 `RtlpDynamicFunctionTable*` 등을 쓰는 곳인가 → 예, 맞다.**
- 근거: `ms/disassemble/ntdll/_LdrpInitialize.asm` (모두 명령 시작 자리), 쓰는 값 `RBX` = 0 (`+0x28 XOR EBX,EBX` 뒤 안 바뀜). `_refs/0x1e7050.tsv`·`_refs/RtlpDynamicFunctionTableLock.tsv` 와도 같다.

| 줄 | 칸 (PDB 이름, rva) | 쓰는 값 |
|---|---|---|
| `+0x10d` | (RAX 에 `RtlpDynamicFunctionTable` 주소를 담음) | — |
| `+0x114` | `RtlpDynamicFunctionTable +0x8` (rva `0x1e7068`, 목록 머리의 `Blink`) | 머리 자기 주소 (`0x1e7060`) |
| `+0x11b` | `RtlpDynamicFunctionTable` (rva `0x1e7060`, `Flink`) | 머리 자기 주소 → **빈 원형 목록** |
| `+0x122` | `RtlpDynamicFunctionTableLock` (`.data` rva `0x1d4210`, 8바이트 칸) | 0 (= SRWLOCK 처음 값) |
| `+0x129` | `RtlpDynamicFunctionTableTreeMin` (`0x1e7070`) | 0 |
| `+0x130` | `RtlpDynamicFunctionTableTreeMax` (`0x1e7078`) | 0 |
| `+0x137` | `RtlpDynamicCallbackTableTreeMin` (`0x1e7058`) | 0 |
| `+0x13e` | `RtlpDynamicCallbackTableTreeMax` (`0x1e7050`) | 0 |

  - PE 파일 속 처음 값이 전부 0 이라 (`.mrdata` 파일 값 읽음) 위 0 쓰기는 **값이 안 바뀐다**. 목록 머리 둘만 0 → 자기 주소로 바뀐다.
  - 갈래 조건: `+0x3a CMPXCHG.LOCK [LdrpProcessInitialized]` 이 돌려준 값이 0 (첫 스레드) → `+0x5a JZ` 목적지 `+0xcf` 로 와서 이 줄들에 닿는다. 값이 1 (`+0x46 JZ` → `+0x257`) 이면 다른 스레드가 하는 중이라 안 쓴다.
  - 락 사용 확인: `RtlpLookupDynamicFunctionEntry +0x11` 이 `LEA RCX,[락]` 뒤 `CALL 0x180024b90` (= `RtlAcquireSRWLockShared`, `_LdrpInitialize +0x363` 이 같은 주소를 그렇게 부른다) — objdump 로 확인.
  - 이 칸들을 나중에 바꾸는 곳 (`_refs/0x1e7050.tsv`): `RtlAddFunctionTable`·`RtlInstallFunctionTableCallback`·`RtlAddGrowableFunctionTable`·`RtlDeleteFunctionTable` 가 목록 머리/`Blink` 를 쓰고, TreeMin/Max 는 `LEA RCX,[..]` 로 **주소를 넘겨** 안쪽에서 쓴다 (호출로 안 닿는 쓰기 — 다른 도우미 몫).

**2. 시작 정보 블록 `+0x98` 을 읽는 곳 → `LdrInitializePerThreadRng +0x4` 와 `LdrpProcessMappedModule +0xe7` 이 맞다. 읽는 칸은 둘 다 4바이트 (dword).**
- 칸: `LdrSystemDllInitBlock +0x98` (rva `0x1ea4e8`, `.mrdata`). 지금 `_refs/LdrSystemDllInitBlock.tsv` 로 보면 **읽는 곳은 5곳**이다 (전부 dword 읽기, 쓰는 곳은 없음):

| 읽는 곳 (명령 시작) | 읽는 모양 | 쓰임 |
|---|---|---|
| `LdrInitializePerThreadRng +0x4` | `MOV EAX,dword` (0 확장) | `+0x11 XOR RAX,&LdrSystemDllInitBlock` · `+0x14 XOR RAX,RCX(TEB)` → 스택 씨앗 칸, 이어서 KUSD 틱 두 칸과 섞어 `TEB+0x260` 의 `RtlInitializeTRng` 씨앗. 씨앗이 0 이면 `EDX=1` (`+0x47 JC` → `+0x59`) |
| `LdrpProcessMappedModule +0xe7` (= `LdrpGenRandom` call `+0xe2` 의 ret) | `MOV R9D,dword` | `+0xf9 XOR R9,RAX` (난수와 섞음) → `LdrInitSecurityCookie` 4번째 인자 (call `+0x107`, ret `+0x10c`) |
| `InitSecurityCookie +0x1d` (= `LdrpGenRandom` call `+0x18` 의 ret) | `MOV R9D,dword` | `+0x31 XOR R9,RAX` → `LdrInitSecurityCookie` 씨앗 (질문 4a) |
| `RtlpInitRandomExVector +0x3d` | `XOR R8D,dword` | 안 따라감 |
| `AvrfMiniLoadDll +0x83c` | `MOV R9D,dword` | 안 따라감 |

- 같은 블록 `+0x9c` 는 바이트 비트 검사 (`TEST byte,1`, CFG 강제 여부) — 14곳에서 읽는다. 쓰는 쪽은 커널 (`PspPrepareSystemDllInitBlock +0x100 · +0x106` 이 `+0x98` 난수 `ExGenRandom`, `+0x9c` = 0 또는 `TEST [CreateInfo+0x11],2` 이면 1).

**3. `PebLdr.Initialized` 와 `RtlpFcProcessManager`·스핀 횟수 (`LdrpInitializeProcess`)**
- (a) **`Initialized` = 1 을 쓰는 줄은 `+0x166` 이다. `+0x162` 가 아니다.**
  - `+0x162 LEA R15D,[R14+1]` 은 값 1 을 `R15` 에 만드는 줄 (`R14` = 0, `+0x66 XOR R14D,R14D` 뒤 안 바뀜). 실제 저장은 `+0x166 MOV byte [PebLdr+0x4],R15B` (rva `0x1d2884`). 짐 지도 3.4 의 "`②… +0x166`" 이 이미 맞다.
  - 같은 묶음: `+0x153` call (ret `+0x158`, `LdrInitializePerThreadRng`) → `+0x158` `Length` = `0x58`. (`+0x158` 는 앞 call 의 ret 자리이자 다음 명령 시작.)
- (b) **`RtlpFcProcessManager` (rva `0x1d3c40`, 다음 이름 `RtlpStackTraceDatabase` `0x1d3d58` 까지 `0x118` 바이트) 쓰기 `+0x20d ~ +0x286`** (call 은 전부 `memset`):

| 줄 | 칸 | 값 |
|---|---|---|
| call `+0x21c` (ret `+0x221`) | `+0x0 ~ +0x117` 전부 | 0 (`R8D=0x118`) |
| call `+0x22d` (ret `+0x232`) | `+0x20` 부터 `0xd0` 바이트 (`R8`=`RBX`=`0xd0`, `+0xf4 MOV EBX,0xd0`) | 0 |
| `+0x237` | `+0x8 ~ +0x17` (16바이트) | 0 |
| `+0x23e` | `+0x18` (8바이트) | 0 |
| call `+0x254` (ret `+0x259`) | `+0x20` 부터 `0x60` 바이트 (`+0x245 LEA EBX,[RAX+0x60]`) | 0 |
| call `+0x265` (ret `+0x26a`) | `+0x80` 부터 `0x60` 바이트 | 0 |
| `+0x26a` · `+0x271` | `+0xf0` · `+0x100` (8바이트씩) | 0 |
| `+0x27f` · `+0x286` | `+0x110` · `+0x108` | `&RtlpFcProcessManager+0x108` (빈 원형 목록 머리: `Flink`=`Blink`=자기) |

  - 요약: 구조체 전체 0 → 목록 머리 한 쌍만 자기 주소. 그 뒤 `+0x28d` call (ret `+0x292`) `RtlpInitializeNonVolatileFlush` (질문 6). `Fc` 가 무슨 약자인지는 코드에 없다 `[공백]`.
- (c) **스핀 횟수 쓰기 `+0x1cd ~ +0x203`: 전역 3개, 쓰기 6줄.** 조건은 `PEB.NumberOfProcessors` (`[RDI+0xb8]`, `RDI`=PEB, `+0x1d4 CMP …,R15D(=1)`) 가 **1보다 클 때** (`JBE` 가 아니면). `+0x1db`·`+0x1ee`·`+0x201` 의 `JBE` 는 `+0x1d4` 비교 결과를 그대로 쓴다 (사이 `MOV` 는 플래그를 안 바꿈). 목적지: `+0x1e7`·`+0x1fa`·`+0x20d`.

| 전역 (rva) | 먼저 | CPU 둘 이상이면 덮음 |
|---|---|---|
| `SRWLockSpinCycleCount` (`0x1d41a4`) | `+0x1cd` = 0 (`R14D`) | `+0x1dd` = `0x2800` |
| `ConditionVariableSpinCycleCount` (`0x1d3d90`) | `+0x1e7` = 0 | `+0x1f0` = `0x2800` |
| `RtlpWaitOnAddressSpinCycleCount` (`0x1d3c0c`) | `+0x1fa` = 0 | `+0x203` = `0x2800` |

  - (`+0x203` 은 끝 줄 `MOV [..],0x2800` 의 **시작** 자리 (10바이트) 라서 요청서의 `+0x203` 이 마지막 쓰기가 맞다.)

### B. ntdll 본문

`LdrpInitialize` (rva `0xe1448`, 99B) 가 세 함수를 부르는 모양 (`.asm` 으로 확인, 목적지는 시작 주소를 뺀 값):
- `+0xa CMP byte [SecurityCookieInitialized],0` → `+0x17 JZ` 목적지 `+0x41` (깃발 0 이면 쿠키 초기화로).
- `+0x41` call `InitSecurityCookie` (ret `+0x46`) → `+0x46 ~ +0x61` : `GS:[0x60]`(PEB) → `[PEB+0x20]`(`ProcessParameters`) → `CMP dword [..+0x8],0` (`Flags`, 맨 윗 비트) → `+0x58 JGE` 목적지 `+0x19`; 아니면 `+0x5a MOV byte [LdrpIsSecureProcess],1` → `+0x19`.
- `+0x19 CMP byte [SpecialMachineFramesInitialized],0` → `+0x20 JNZ` 목적지 `+0x27` ; 0 이면 `+0x22` call `InitSpecialMachineFrames` (ret `+0x27`).
- `+0x27` call `__cpu_features_init` (ret `+0x2c`) — **깃발 검사가 없다, 매번 부른다.**
- `+0x3c JMP` → `LdrpInitializeInternal` (꼬리호출).

**4a. `InitSecurityCookie` (rva `0xe1548`, 112B)**
- 쓰는 것과 값 (명령 시작 자리):

| 줄 | 쓰는 것 | 값 |
|---|---|---|
| `+0x9` | `SecurityCookieInitCount` (rva `0x1d23d4`, 4B) `XADD.LOCK` | +1 (돌려받은 값+1 = 이 스레드 순번, `+0x13 CMP EAX,1`) |
| call `+0x18` (ret `+0x1d`) | — `LdrpGenRandom` | KUSD `ProcessorFeatures[28]` (`[0x7ffe0290]`) 이 0 이 아니면 `RDRAND` 최대 10번, 아니면 0 |
| `+0x1d` | (읽기) `LdrSystemDllInitBlock +0x98` dword | `+0x31 XOR R9,RAX` → 씨앗 = dword ^ 난수 |
| call `+0x38` (ret `+0x3d`) | **`__security_cookie`** (`.mrdata` rva `0x1ea5a8`) — `LdrInitSecurityCookie` 안 `+0x1e9 MOV [RDI],RBX` | 아래 식 |
| `+0x3d` → `+0x42` | `SecurityCookieInitialized` (rva `0x1d23ce`) | **`+0x42 MOV byte [..],1`** (앞 `+0x3d OR.LOCK [RSP],0` 은 메모리 장벽) |

  - 쿠키 식 (`LdrInitSecurityCookie.asm`): 새 값 = `(LdrpGenSecurityCookie(&쿠키) ^ (씨앗 하위32비트 << 16) ^ (씨앗 >> 32)) & 0xFFFFFFFFFFFF` (48비트). call `+0x75` (ret `+0x7a`).
    - 현재 값이 기본값 `0x2b992ddfa232` (파일 값, `+0x5f`) 와 다르면 아무것도 안 하고 0 돌려줌 (`+0x6c JNZ` → `+0x1a0`). 새 값이 기본값이거나 0 이면 `0x2b992ddfa233` 으로 바꿈 (`+0x221`).
    - `__security_cookie` 가 `.mrdata` (읽기 전용) 안이라 **쓰기 전에 보호 풀기**: `NtProtectVirtualMemory` 2번 (call `+0x1e0` ret `+0x1e5` : `PAGE_READWRITE`(4), 8바이트 / 쓰기 `+0x1e9` / call `+0x212` ret `+0x217` : 옛 보호 되돌림).
  - `LdrpGenSecurityCookie` (rva `0xe1838`): KUSD `+0x14`·`+0x18` (`InterruptTime`)·`+0x320` (`TickCountQuad`)·`+0x4` (`TickCountMultiplier`) + TEB `+0x40`·`+0x48` (`ClientId`) + 주소 인자를 섞고, **`NtQueryPerformanceCounter`** 한 번 (call `+0x8d`, ret `+0x92`). 전역은 안 씀.
- syscall 정리: `NtQueryPerformanceCounter` 1 · `NtProtectVirtualMemory` 2 (첫 스레드만), `NtDelayExecution` (기다리는 스레드만).
- 갈래: `+0x16 JNZ` 목적지 `+0x4f` — 순번이 1 이 **아니면** (= 다른 스레드가 이미 시작) 쓰지 않고 `NtDelayExecution(-0x493e0 → 30ms)` (call `+0x61`, ret `+0x66`) 를 돌며 `SecurityCookieInitialized` 가 켜질 때까지 기다림 (`+0x6d JZ` → `+0x5a`, 켜지면 `+0x6f JMP` → `+0x49` 끝). 첫 스레드는 그냥 지나간다. (`LdrpInitialize +0xa` 가 먼저 깃발을 보므로 실제로는 거의 한 번.)

**4b. `InitSpecialMachineFrames` (rva `0xe14b4`, 139B)**
- 쓰는 것:

| 줄 | 쓰는 것 | 값 |
|---|---|---|
| `+0xf` | `SpecialMachineFramesInitCount` (rva `0x1d23f0`, 4B) `XADD.LOCK` | +1, `+0x19 CMP EAX,1` |
| call `+0x39` (ret `+0x3e`), `+0x46` | `RtlpContinuationContextMachineFrameEntries[0..1]` (rva `0x1cc208`, 8B×2, `.data`) | 칸 안 함수 주소 → `RtlLookupFunctionEntry` 로 `RUNTIME_FUNCTION` 찾아 `ImageBase + [RAX+8]` (`UnwindData`) 로 **바꿔 씀**. 2번 (`+0x2b LEA EDI,[RAX+1]`=2, `+0x4d SUB`, `+0x51 JNZ` → `+0x2e`) |
| `+0x57` | `SpecialMachineFramesInitialized` (rva `0x1d23d0`) | **`MOV byte [..],1`** (앞 `+0x53 OR.LOCK [RSP],EDI` 장벽) |

  - 파일 속 처음 값 (`.data` 읽음): `[0]` = ntdll+`0x164e10` (`KiUserExceptionDispatch[er]`), `[1]` = ntdll+`0x164ca0` (`KiUserApcDispatch[er]`) — 이름은 PDB 로 확인. 읽는 쪽: `RtlpIsContinuationContextMachineFrameEntry +0xb · +0x12` 가 `CMP RCX,[표+RDX*8]` (`_refs`). 즉 "이 풀림 정보가 커널→유저 진입 함수의 것인가" 를 가르는 표 `[추측]`.
- syscall: **없음** (`RtlLookupFunctionEntry` 는 ntdll 안 함수). 기다리는 스레드만 `NtDelayExecution` (call `+0x7c`, ret `+0x81`).
- 갈래: `+0x1c JNZ` 목적지 `+0x6a` — 순번이 1 이 아니면 `SpecialMachineFramesInitialized` 가 켜질 때까지 30ms 씩 대기 (`+0x88 JZ` → `+0x75`, 끝 `+0x8a JMP` → `+0x5e`).

**4c. `__cpu_features_init` (rva `0x12e47c`, 130B)**
- 쓰는 것은 **`__isa_info` (rva `0x1ccf00`, 1바이트) 하나**: `+0x74 MOV byte [..],DL`. 파일 처음 값은 `2`.
- 값: `RtlIsProcessorFeaturePresent(id)` 네 번 (call `+0xb` ret `+0x10`: id `0x26` · call `+0x17` ret `+0x1c`: `0x2a` · call `+0x45` ret `+0x4a`: `0x27` · call `+0x5f` ret `+0x64`: `0x28`) → 결과로 비트를 켠다:
  - 항상 `0x1`, `0x2a` 있으면 `0x2`, `0x27` 있으면 `0x4`, `0x26` 있으면 `0x8`, `0x28` 있으면 `0x10`.
  - 번호 이름은 winnt.h 의 `PF_*` (38 SSE4.2 · 39 AVX · 40 AVX2 · 42 ERMS) `[추측]` — 코드엔 숫자만 있다.
  - `RtlIsProcessorFeaturePresent`: 번호 < `0x40` 이면 KUSD `ProcessorFeatures` 칸 (`0x7ffe0274 + 번호`) 바이트, `0xc0` 미만이면 `RtlProcessorFeaturesBitMap` 비트 (`+0x1b ~ +0x29`). 읽기만.
- syscall: **없음.** 갈래: **없음** (매번 같은 길). 이 바이트를 읽는 쪽: `memset`·`wcslen`·`strlen` 류 (`_refs/__isa_info.tsv`).
- **run44 사진 `P1b~P2` 에 바뀐 CPU 기능 전역 6개 (`x86_cpu_enable_avx512`·`x86_cpu_enable_ssse3`·`x86_cpu_enable_simd`·`g_SymCryptFlags`·`g_SymCryptCpuFeaturesPresentCheck`·`g_SymCryptCpuid1`) 는 `__cpu_features_init` 이 쓰지 않는다. 답: 아니오.** 그 6개의 쓴 자리는 아래 (`_refs`, `P1b` = `LdrpInitializeProcess +0x1bb6` call 보다 **뒤** 코드라 구간이 맞다):

| 전역 (rva) | 쓰는 줄 | 값 |
|---|---|---|
| `x86_cpu_enable_ssse3` (`0x1d21e4`) | `LdrpInitializeProcess +0x1d75` | `CPUID(1).ECX & 0x200` |
| `x86_cpu_enable_simd` (`0x1d21e8`) | `+0x1d8e` | `CPUID(1)` 의 `EDX` 비트 26 · `ECX` 비트 20 · `ECX` 비트 1 셋 다 켜지면 1, 아니면 0 (`EDI`=1 / `R15D`=0) |
| `x86_cpu_enable_avx512` (`0x1d21e0`) | `+0x1da1` (call `+0x1d99` ret `+0x1d9e`: `RtlIsProcessorFeaturePresent(0x29)` 결과 0/1), 켜진 경우 `CPUID(7).ECX` 비트 10 이 꺼져 있으면 `+0x1ddf` 로 0 | 0 또는 1 |
| `g_SymCryptFlags` (`0x1d2230`) | `+0x1df4` (`OR.LOCK`, call `+0x1def` ret `+0x1df4` 가 `SymCryptDetectCpuFeaturesByCpuid`) | 비트 1 |
| `g_SymCryptCpuFeaturesPresentCheck` (`0x1d2234`) | `+0x1e27` | `~g_SymCryptCpuFeaturesNotPresent` |
| `g_SymCryptCpuid1` (`0x1d33f0`·`0x1d33f4`) | `SymCryptDetectCpuFeaturesByCpuid +0xee · +0xf4` | `CPUID` 결과 `EAX`·`EBX` |

**4d. 힌트 이름 3개 → 모두 PDB 이름으로 있다 (`pdb_aliases.py --where ntdll.dll <이름>`):** `SecurityCookieInitCount` rva `0x1d23d4` · `SpecialMachineFramesInitCount` rva `0x1d23f0` · `__isa_info` rva `0x1ccf00`. 쓰는 함수: 앞 둘은 각각 `InitSecurityCookie +0x9` · `InitSpecialMachineFrames +0xf` (둘 다 `XADD.LOCK`), `__isa_info` 는 `__cpu_features_init +0x74`. 모두 baggage 7.3 `≤P1a` 구간과 맞다 (`LdrpInitialize` 는 `LdrpInitializeProcess` 보다 먼저).

**5. `RtlEncodePointer` (rva `0xe9610`, 95B)**
- 쿠키 출처: **캐시 전역을 먼저 본다.** 캐시 = PDB 이름 `?CookieValue@?1??RtlpGetCookieValue@@9@9` (함수 안 `static` 변수, rva `0x1d2408`, 4바이트). `RtlDecodePointer +0x6` 가 읽고 `+0x5c` 가 쓰는 **같은 칸** (`_refs/0x1d2408.tsv`).
- 흐름 (목적지는 시작 주소를 뺌): `+0xe MOV EAX,[캐시]` → `+0x14 TEST` → `+0x16 JZ` 목적지 `+0x2a`.
  - 캐시 ≠ 0: `+0x1a XOR RAX,RBX` (`RBX`=인자 포인터, `RAX`=쿠키 0 확장) → `+0x20 ROR RAX,CL` (`CL` = 쿠키 & `0x3f`) → 끝. **syscall 없음.**
  - 캐시 = 0: call `+0x43` (ret `+0x48`) **`NtQueryInformationProcess(-1, 0x24, &버퍼, 4, NULL)`** (`+0x3b OR RCX,-1` 현재 프로세스, `+0x3f LEA EDX,[R9+0x20]` R9D=4 → 클래스 `0x24` = `ProcessCookie`, `+0x6 AND [RSP+0x48],0` 버퍼 0 으로 시작). 성공하면 `+0x50 MOV [캐시],EAX` 한 번 (조건 비교 없는 보통 저장) → `+0x56 JMP` 목적지 `+0x18` 로 돌아가 위 식. 실패(`+0x4a JS` → `+0x58`)면 call `+0x5a` (ret `+0x5f`) `RtlRaiseStatus`.
- 답: **처음 부를 때만 syscall** (캐시가 0 인 동안; 성공하면 이후 모든 `RtlEncodePointer`·`RtlDecodePointer` 는 syscall 없이 캐시로). 커널 쪽은 sast/45 (`_EPROCESS.Cookie +0x2b8`, 처음 0 이면 만듦).
- **정정 한 가지** (run51 서술): 이 프로세스에서 `NtQueryInformationProcess(0x24)` 를 **가장 먼저 부르는 코드는 `LdrpInitializeProcess +0x12f RtlEncodePointer(0)`** (call `+0x12f`, ret `+0x134`, 안쪽 call `+0x43`) 이다. `RtlInitializeHeapManager +0x9d → RtlpGetModifiedProcessCookie +0x22` (같은 클래스 `0x24`, 캐시 **없이** 매번 부름, 값에 `*0x7fffffed + 0x7fffffc3 mod 0x7fffffff` 를 해서 돌려줌) 는 코드 순서상 **두 번째**다 (`+0x9ed` 쪽이 `+0x12f` 보다 뒤). baggage 1280줄의 "(+0xa34 안 탐) 커널 칸 `EPROCESS.Cookie` 를 처음 만듦" 은 "처음 만드는 호출 = `+0x12f` 쪽, 힙 관리자 쪽은 이미 만들어진 값을 읽음" 으로 고쳐야 한다.
- 이 캐시 칸은 이 쓴 자리가 baggage 7.3 `≤P1a` 목록 (`?CookieValue@?1??RtlpGetCookieValue@@9@9`) 에서 빠진다.

**6. `RtlpInitializeNonVolatileFlush` (rva `0xfb648`, 144B)**
- **syscall 을 부른다:** call `+0x31` (ret `+0x36`) `NtQuerySystemInformation(0xc0, &버퍼(0x20B), 0x20, NULL)` — 클래스 `0xc0` 은 nt PDB 열거형 `SystemFlushInformation` (192). 커널 쪽 `ExpGetSystemFlushInformation` 이 채운다 (`+0x0` dword 플래그: 비트0 항상 1 · 비트1 = `KeFeatureBits` 비트 35 · 비트2 = 비트 36, `+0x4` dword = `KPRCB+0xe8` 캐시 줄 크기, `+0x8` qword = 플랫폼 플러시 능력 `ExpPlatformFlushCapabilities` — 없으면 ACPI `NFIT` 표를 읽음). 비트가 어느 CPU 명령인지는 안 봤다 `[공백]`.
- 쓰는 것과 값 (모두 성공했을 때만; `+0x38 JS` 목적지 `+0x72` 로 실패하면 아무것도 안 씀). 파일 처음 값은 괄호:

| 줄 | 전역 (rva) | 값 | 조건 |
|---|---|---|---|
| `+0x41` | `RtlpIsFlushRequired` (`0x1cd0c0`, 1B, 처음 1) | 0 | `[버퍼+8]` 비트 0 (`+0x3a TEST [RSP+0x28],1`, `JZ` 목적지 `+0x48`) |
| `+0x85` (그 뒤 `JMP` 목적지 `+0x60`) | `RtlpOptimalFlushMethod` (`0x1cd0b0`, 4B, 처음 1) | 2 | `[버퍼+0]` 비트 2 (`+0x48 TEST [RSP+0x20],4`, `JNZ` 목적지 `+0x85`) |
| `+0x56` | `RtlpOptimalFlushMethod` | 3 | 비트 2 는 꺼지고 비트 1 이 켜졌을 때 (`+0x4f TEST …,2`, `JZ` 목적지 `+0x67`) |
| `+0x60` | `RtlpIsDrainRequired` (`0x1ce749`, 1B, 처음 0) | 1 | 위 둘 중 하나일 때 |
| `+0x6b` | `RtlpClFlushSize` (`0x1cd0b8`, 8B, 처음 64) | `[버퍼+4]` (0 확장) | 성공하면 항상 (`+0x54 JZ` 목적지 `+0x67` 도 여기로 옴) |

- 호출한 쪽: `LdrpInitializeProcess +0x28d` (ret `+0x292`). 네 전역 모두 baggage 7.3 `≤P1a` 목록에 있다 → 쓴 자리 확인.

**7. `RtlGetCurrentServiceSessionId` (rva `0x38fc0`, 25B) 와 ETW 갈래 (`LdrpInitializationComplete`)**
- 이 함수: `GS:[0x60]` (PEB) → `[PEB+0x90]` (`SharedData`) → 0 이면 **0 돌려줌** (`+0x13 JNZ` 목적지 `+0x17`), 아니면 `[SharedData]` dword. test.exe 는 K1 사진에서 `PEB+0x90 = (null)` 이고 (`PspMapSiloSharedDataView` 질문 15 가 보통 프로세스에선 안 씀) → **0**.
- 호출한 쪽 `LdrpInitializationComplete` (call `+0x19`, ret `+0x1e`). 조건 (모두 `.asm` 줄):
  1. `+0x20 JNZ` 목적지 `+0xac` : 세션 번호 **≠ 0** 이면 검사 칸이 `PEB.SharedData + 0x22a` (`+0xbc`) 로 바뀌고 `+0xc3 JMP` 목적지 `+0x2c` 로 합류.
  2. 세션 번호 = 0: 검사 칸 = **KUSD `0x7ffe0384`** (`+0x26`). KUSD `+0x380` 은 `UserModeGlobalLogger[16]` (PDB) 이라 그 **둘째 원소의 아래 바이트**다.
  3. `+0x2c CMP byte [칸],0` → `+0x30 JZ` 목적지 `+0x44` : 0 이면 **ETW 안 하고** 건너뜀.
  4. 아니면 `+0x32 PEB` → `+0x3b TEST byte [PEB+0x378],4` (`TracingFlags` 비트 2 `LibLoaderTracingEnabled`) → 꺼져 있으면 `+0x42 JNZ` 안 탐 → `+0x44`.
  5. 켜져 있으면 (`+0x42 JNZ` 목적지 `+0x67`) 다시 `RtlGetCurrentServiceSessionId` (call `+0x67`, ret `+0x6c`) → 0 이면 `+0x6e JZ` 목적지 `+0xc8` (`MOV ECX,0x7ffe0385` → `+0xcd JMP` 목적지 `+0x87`), 아니면 `SharedData + 0x22b`. 그 바이트의 **비트 5 (0x20)** 가 꺼져 있으면 (`+0x8a JZ` 목적지 `+0x44`) 건너뜀.
  6. 켜져 있으면 `LdrpLogEtwEvent` (call `+0xa5`, ret `+0xaa`) 를 부르고 `+0xaa JMP` 목적지 `+0x44` 로 합류.
  7. 어느 쪽이든 `+0x44 INC.LOCK [LdrpProcessInitialized]` (1 → 2, 첫째 인자) → `[LdrpInitCompleteEvent]` 가 0 이 아니면 `NtSetEvent` (call `+0x51`, ret `+0x56`).
- 정리: ETW 이벤트는 **KUSD(또는 silo 공유 데이터) 의 전역 로거 칸이 0 이 아니고 + `PEB.TracingFlags` 비트 2 가 켜지고 + 그 칸 윗 바이트 비트 5 가 켜졌을 때만** 남는다. 이 VM 값은 정적으로 못 안다 → 동적.

### C. ntoskrnl 본문·비트 뜻

**8. ⓐ `PspAllocateProcess +0x2277 ~ +0x22e1` — PEB `BitField` 비트마다 출처**
- 이 줄들은 PEB 를 직접 쓰는 게 아니라 **견본 (지역 변수 `[RSP+0x1a8]`)** 을 만든다. `+0x2266 MOV [RSP+0x1a8],0` (앞 8바이트) · `+0x226e OR [RSP+0x1b0],-1` (`Mutant` = -1) · 이 줄들이 `[RSP+0x1ab]` (= PEB `+0x3` `BitField`) 를 채우고 (`+0x22e1 MOV [RSP+0x1ab],R12B`), `+0x2304 LEA RDX,[RSP+0x1a8]` 로 `MmCreatePeb` (call `+0x230f`, ret `+0x2314`) 에 넘긴다 (3.1 첫 줄의 "견본 복사").
- 지역 플래그 `EBX` (이하 `F`) 는 함수 앞부분에서 조건마다 켜는 비트 모음이다. 비트 8·12·13 을 보면:

| PEB `BitField` 비트 | 이름 | 값의 출처 (명령 시작 자리) |
|---|---|---|
| 0 | `ImageUsesLargePages` | `F` 비트 12 (`+0x2282 SHR EDX,0xc` · `+0x2285 AND DL,R13B`) |
| 1 | `IsProtectedProcess` | `EPROCESS.Protection` (`+0x5fa`, `_PS_PROTECTION`) 의 `Type` (하위 3비트) **≠ 0** (`+0x2277 MOV AL,[R14+0x5fa]` · `+0x227e AND AL,7` · `+0x2288 CMP SIL,AL` · `+0x228b SBB CL,CL` · `+0x228d AND CL,2`) |
| 2 | `IsImageDynamicallyRelocated` | `F` 비트 13 (`+0x22a1 MOV ECX,EBX` · `+0x22a3 SHR ECX,0xd`) |
| 3 | `SkipPatchingUser32Forwarders` | 안 켬 (앞 바이트 값 0 이 남음, `+0x2292 MOV CL,[RSP+0x1ab]` · `+0x2299 AND CL,0xfc`) |
| 4 | `IsPackagedProcess` | call `+0x225f` (ret `+0x2264`) **`PsQueryProcessAttributes(process, 0, &[RSP+0x95])`** 의 둘째 출력 (`+0x2252 LEA R8,[RSP+0x95]`) = 주 토큰에 `PspPackagedAppClaim` 보안 속성 (문자열 **`WIN://PKG`**) 이 있는가 (`PsQueryProcessAttributes +0x51` call `SeSecurityAttributePresent`, ret `+0x56` 에서 `[RSI]=AL`). 읽는 줄 `+0x22c9 MOV R12B,[RSP+0x95]` · `+0x22d1 AND R12B,R13B` · `+0x22d7 SHL R12B,4` |
| 5 | `IsAppContainer` | call `+0x1060` (ret `+0x1065`) **`SeQueryInformationToken(토큰, 0x1d, &[RSP+0xe4])`** (클래스 29 = `TokenIsAppContainer` `[추측: 번호→이름 상식]`) 결과 ≠ 0 (`+0x22bb MOV EAX,[RSP+0xe4]` · `+0x22c2 NEG EAX` · `+0x22c4 SBB CL,CL` · `+0x22c6 AND CL,2` · `+0x22d4 OR R12B,CL`, 합쳐서 `SHL 4` 로 비트 4·5) |
| 6 | `IsProtectedProcessLight` | `Protection.Type == 1` (`+0x22a9 CMP AL,R13B` · `+0x22ac SETNZ AL` · `+0x22af SUB AL,R13B` · `+0x22b2 AND AL,0x10` · `+0x22b6 SHL CL,2`) |
| 7 | `IsLongPathAwareProcess` | 안 켬 (커널은 안 씀; `+0x229e AND DL,0xbb` 가 2·6 만 지우고 7 은 앞 값 0) |

  - `AND DL,0xbb` 가 비트 2·6 을 지운 뒤 비트 2 (`F`13)·6 (`Type==1`) 을 다시 켠다. `AND DL,0xcf` 는 비트 4·5 를 비우고 `R12B` 가 채운다.
- **지역 플래그 비트 12 (0x1000) 와 비트 13 (0x2000):**
  - **비트 12 = "큰 페이지 사용 요청"** : `+0x21c6 MOV ECX,[RSP+0x8b0]` · `+0x21cd AND ECX,0x10` · `+0x21d0 SHL ECX,8` · `+0x21d5 BTR EAX,0xc` · `+0x21db OR EBX,EAX` (**`F` 비트 0 이 꺼져 있을 때만**, `+0x21c4 JNZ` 목적지 `+0x21f4`). `[RSP+0x8b0]` 은 `PspAllocateProcess` **9번째 인자** (프로세스 만들기 플래그, 호출자 `NtCreateUserProcess +0xc00 ~ +0xc31` 이 `[RBP+0x7d0]` 를 넘김). 그 플래그 `0x10` 은 호출자 값이거나 **IFEO `UseLargePages` 값이 0 이 아닐 때** `OR R12D,0x10` (`+0xbcd`, `RtlQueryImageFileKeyOption` call `+0xbab` ret `+0xbb0`, 저장 `+0xbd9`). 이름 `LARGE_PAGES` 는 phnt 에서 `[추측]`, 코드 증거는 IFEO 문자열 `UseLargePages` (`.c` 610줄).
  - **비트 13 = "이미지가 동적으로 재배치됨"** : 섹션 이미지 정보 `ImageFlags` (`_SECTION_IMAGE_INFORMATION +0x33`) 의 **비트 2 `ImageDynamicallyRelocated`**. 두 군데에서 켠다 (같은 계산):
    - 경로 A (`test.exe` 가 타는 쪽, 11번째 인자 `R15` ≠ 0): call `+0x98c` (ret `+0x991`) `MmGetSectionInformation(섹션, 4, &[R15+0x38])` 가 `[R15+0x38]` 에 `_SECTION_IMAGE_INFORMATION` 모양을 채움 → `+0x99f MOVZX ECX,byte [R15+0x6b]` (= `+0x38 + 0x33` = `ImageFlags`) → `+0x9c0 AND ECX,4` · `+0x9c3 OR EBX,ECX` · `+0x9c5 SHL EBX,0xb` (비트 2 → 13) · `+0x9c8 OR EBX,R12D`.
    - 경로 B (`R15` = 0 이거나 최소 프로세스인 쪽, `+0x966 TEST R15,R15` · `+0x96f TEST BL,2` 가 `+0xef2` 로 보냄): call `+0xf0c` (ret `+0xf11`) `MmGetSectionInformation(…, 4, &[RSP+0x3b0])` → `+0xf1b MOVZX ECX,byte [RSP+0x3e3]` → `+0xf46 AND ECX,R12D(=4)` … `+0xf4b SHL EDX,0xb` · `+0xf4e ~ +0xf59` 합침.
    - `ImageFlags` 비트 1 (`ComPlusILOnly`) 은 같은 식으로 `F` 비트 14 로 간다 (질문 밖).
  - `F` 비트 8 은 질문 10.
- **K1 사진 대조 (`ms/logs/04_baggage-read_K1.txt`)**: `BitField = 0x4` (`IsImageDynamicallyRelocated` 만 1), `_SECTION_IMAGE_INFORMATION.ImageFlags = 0x4` (1462줄), `Mutant = -1` → 위 식과 **일치**.

**9. ⓒ `MmCreatePeb +0x2b8` — 프로세스 `+0x170` 비트 0**
- 칸 이름: `_EPROCESS.Pcb` (`_KPROCESS`) 의 **`SecureState`** (`+0x170`, 8바이트 공용체) 의 **`Flags.SecureProcess`** (비트 0, 비트 1 은 `TrustedApp`). PDB 확인.
- 뜻: "보안 프로세스" (VSL/가상 보안 모드 쪽에서 만든 프로세스). 켜는 곳은 한 곳: `KeInitializeProcess +0x44d ~ +0x473` — call `+0x45c` (ret `+0x461`) `VslCreateSecureProcess(process, 0, &SecureState)` 가 성공하면 `+0x473 OR [RBX],R9(=1)`.
- `MmCreatePeb +0x2b8 TEST byte [R13+0x170],1` → `+0x2c0 JNZ` 목적지 `+0x5e6`: 켜져 있으면 이미지 NT 헤더를 읽지 않고 **고정값**을 쓴다 (`+0x5e6 ~ +0x5f9`: 서브시스템 `ESI`=3, 특성 `0x22` (= 실행 파일 + 큰 주소 인식), NT 헤더 포인터 0). 꺼져 있으면 `+0x2c6` 로 가서 `RtlImageNtHeader` (call `+0x2c9`, ret `+0x2ce`) 로 PEB 의 서브시스템·버전 칸을 이미지 헤더에서 채운다.
- test.exe: 보통 프로세스라 꺼짐으로 읽힌다 `[추측]` (이 비트를 켜는 줄이 위 한 곳뿐). K1 로그엔 `SecureState` 안쪽 값이 안 찍혀 있다 → 동적 D5.

**10. ⓓ `PspAllocateProcess +0x2248 BT EBX,8` — 비트 8 의 뜻**
- 켜는 줄 `+0x21e9 BTS EBX,8` 는 `F` 비트 0 과 비트 1 이 둘 다 꺼졌을 때 (sast/50).
- `F` 앞 비트들 (`+0x2ba ~ +0x31f`): 만들기 플래그 (9번째 인자) 비트 11 (`0x800`) → `F`=2 (최소 프로세스), 비트 13 (`0x2000`) → `F`=6, 비트 14 (`0x4000`) → `F`=0xe — 이름은 phnt `MINIMAL_PROCESS`·`CLONE_MINIMAL`·`CLONE_MINIMAL_REDUCED_COMMIT` `[추측]`. 최소 프로세스 플래그가 없으면: **7번째 인자 (섹션 객체. `NtCreateUserProcess` 가 `+0x972` (ret) `ObReferenceObjectByHandle` 로 얻어 `+0x978 MOV [RBP+0x270],RCX` 에 둔 값) 가 0 이 아니면 `F`=0** (정상: 섹션 있음), 0 이면 부모 (첫째 인자) 가 있을 때 `F`=4, 없을 때 `F`=1 (`+0x2fc ~ +0x30d`).
- 비트 8 읽는 곳은 둘: `+0x2248 BT EBX,8` (JNC 목적지 `+0x2364` → `PsQueryProcessAttributes`·`MmCreatePeb`·`PspSetProcessAffinitySafe` 건너뜀) 과 `+0x2574 BT EBX,8` (JNC 목적지 `+0x260d`, 이어서 `+0x257e TEST BL,0x10` 도 같은 `+0x260d` 로).
- **뜻 (코드 효과로 본 것): "PEB 를 가지는 프로세스" — 최소 프로세스도 아니고 (`F` 비트 1 꺼짐) 부모도 섹션도 없는 시스템류도 아닌 (`F` 비트 0 꺼짐) 경우.** 즉 정상 사용자 프로세스 (`F`=0) 와 부모 주소 공간 복제 (`F`=4, `+0x22e9 TEST BL,4` → `+0x23a3` 에서 부모의 `Peb` 를 그대로 가져옴) 가 해당. 이름은 코드에 없다 → 우리 말 요약 `[추측: 이름]`, 효과는 `[정적]`. test.exe 는 `F`=0 → 비트 8 켜짐 → `MmCreatePeb` 를 탄다 (sast/50 과 같음).

**11. ⓔ `PspSetupUserProcessAddressSpace +0xfa ~ +0x10f`**
- (a) **만들기 정보 `+0x10` 비트 4 의 뜻**: 사용자 모드 `PS_CREATE_INFO` 의 `InitFlags` 비트 1 (**`DetectManifest`**, phnt 이름 `[추측]`) 이 커널 복사본에서 비트 4 로 옮겨진 것. 근거: `PspCaptureCreateInfo +0x77 ~ +0x95` (`MOV CL,[R10+0x10]` · `SHL AL,3` · `XOR … AND AL,0x10 …` → `[R8+0x10]`), 같은 곳에서 비트 0 → 1 (`+0x7b ADD CL,CL`), 비트 2 → 7 (`+0x41 ~ +0x52`), 비트 3 → `[+0x11]` 비트 0 (`+0x56 ~ +0x68`) 로 옮김. 사용자 쪽 켜는 줄: `KERNELBASE!CreateProcessInternalW +0x2add OR byte [RSP+0x560],R13B` (`R13D`=2, `+0x2a1f MOV R13D,2`) — 조건은 `[RSP+0x190]` (속성 구조) 가 없거나 `+0x18` 이름이 비었거나 `[+0x50]` 비트 2 가 켜졌을 때 (`+0x2abc ~ +0x2add`).
  - 읽는 줄: `PspSetupUserProcessAddressSpace +0xfa TEST byte [RSI+0x10],0x10` (`+0xf5` call `KiStackAttachProcess` 의 ret 자리) → `+0xfe JZ` 목적지 `+0x115` (끄면 건너뜀).
- (b) **`PspLocateInPEManifest` (call `+0x106`, ret `+0x10b`) 하는 일** (`PspLocateInPEManifest.asm`): 새 프로세스에 붙은 상태 (`+0xf5` `KiStackAttachProcess`) 에서 **exe 이미지 안의 `RT_MANIFEST` (24) 자원 `ID 1`** 을 찾는다.
  - `+0x6 CMP [process+0x310],0` (`WoW64Process`) 가 0 이면 (64비트 exe) `+0x1e` 로: `LdrResSearchResource(이미지 시작 = EPROCESS.SectionBaseAddress, 경로 {0x18, 1, 0}, 수준 3, 플래그 0x30, 포인터 출력 = 만들기 정보 +0x130, 크기 출력)` (call `+0x70`, ret `+0x75`).
  - 찾으면: 크기가 4GB 이하 아니면 `0xC000007B` (`+0x86 JA` → `+0xfe`). 맞으면 **만들기 정보 `+0x10` 비트 5 (`0x20`) 를 켬** (`+0x88`) · **`+0x130` 포인터** (위 출력) · **`+0x138` 크기** (`+0x8c`) · **매개변수 사본 `Flags` 비트 13 (`0x2000`)** 켬 (`+0x99 BTS [process params +0x8],0xd`; 매개변수 포인터 = `[만들기 정보+0xd8]`) → 0 돌려줌.
  - 못 찾은 코드 `0xC0000089 ~ 0xC000008B` 와 `0xC0000204` 는 **성공(0)** 으로 바꿔 돌려준다 (`+0xa7 ~ +0xb7`). 32비트(`0x14c`)·ARM 용 (`0x1c4`) 은 `+0xbb` 쪽 다른 길.
- (c) **test.exe 가 이 길을 타나**: 정적으로는 못 가른다. 다만 **결과는 정해진다:** `sample/bin/test.exe` 는 **자원 디렉터리가 없다** (PE 헤더 데이터 디렉터리 2번 = 0, `.rsrc` 구역 없음 — `sample/build.bat` 이 `cl … /link /DEBUG` 만, 매니페스트 안 넣음) → 부르든 말든 `+0x130/+0x138/비트 5/Flags 비트 13` 은 **안 켜진다.** K1 사진 `ProcessParameters.Flags = 0x4001` (비트 13 꺼짐, 2337줄) 과 맞다. (run32 순서 표에 빠진 이유: 이 호출이 `+0xfa` 비트 4 조건 아래라서.)

**12. ⓖ `PspGetProcessParameterOverrides` (call `NtCreateUserProcess +0xab3`, ret `+0xab8`)**
- 본문 (`PspGetProcessParameterOverrides.asm`, 104B): 입력 `RCX` = 만들기 정보 (커널 복사본, `[RBP+0x1a0]`), `RDX` = `[RBP+0x68]`. 지역 구조 `{버전 1, 출력 = RCX+0xf0, 입력 = RDX}` 를 만들어 **인자 하나 `&구조`, `&결과포인터`** 로 간접 호출 `CALL [0x140150340]` = import 슬롯 **`__imp_PsGetProcessParameterOverrides`** (call `+0x34`, ret `+0x3b`; 확장 API 집합 `ext-ms-win-ntos-processparameters-l1-1-0`).
  - 그 슬롯의 **PE 파일 값은 ntoskrnl 안 `rva 0x4a10c0`** 의 스텁이고, 그 스텁은 `ret 0` (바이트 `c2 00 00`) 한 줄이다 → **아무것도 안 하고 돌아온다** (파일 값 기준; 부팅 때 확장 모듈이 붙어 슬롯이 바뀌는지는 동적).
  - 돌아온 결과 포인터 `[RSP+0x50]`: 0 이면 `+0x4a MOV [RBX+0x1d8],RAX` (0 저장). 0 이 아닌데 `[RAX]` 가 `2` 이면 (`+0x58 CMP …,2`, `JZ` 목적지 `+0x4a`) 그 포인터를 저장, 아니면 call `+0x60` (ret `+0x65`) `PspDestroyProcessParameterOverrides` 로 지우고 0 저장.
- **쓰는 곳은 딱 하나: 만들기 정보 (커널 복사본) `+0x1d8` (8바이트 포인터).** 매개변수 사본은 안 건드린다. 읽는 쪽은 이번에 안 찾았다 `[공백]`.
- **`PsDefaultLoaderThreads` 를 읽나: 아니오.** 이 함수 `.asm` 에 그 주소 참조가 없다. 읽는 곳은 이미 아는 `PspSetupUserProcessAddressSpace +0xc1` (`MOV EAX,[..]`, 앞 `+0xb8 CMP [params+0x40c],0`) 한 곳이고 `+0xc7` 이 `LoaderThreads` (`_RTL_USER_PROCESS_PARAMETERS +0x40c`, PDB 확인) 에 쓴다 (Q79 와 같음).

**13. ⓗ 시작 정보 블록 완화 옵션 24B 셋째 칸 `+0xb0` (사진 `0x02002002`)**
- **출처 (끝에서 끝까지):**
  1. 만들기 정보 (커널 복사본) `+0x150` (16B) · `+0x160` (8B) 이 24바이트 옵션 표. `PspPrepareSystemDllInitBlock +0x64 ~ +0x78` 이 이를 지역에 복사하고 `+0x126 MOVUPS [RBX+0xa0]` · `+0x132 ~ +0x138 MOVSD [RBX+0xb0]` 로 **블록 `+0xa0` (16B) · `+0xb0` (8B)** 에 쓴다 (`RBX` = ntdll 안 시작 정보 블록).
  2. 그 옵션 표는 `PspApplyMitigationOptions(process, …, R8 = &표, R9 = &감사표)` 가 **제자리에서 고쳐 쓴다** (`+0x5ef MOVUPS [R12],XMM0` · `+0x5f4 MOVSD [R12+0x10],XMM1`, `R12` = 인자 `R8`). 부모 값은 `PspInheritMitigationOptions` 가 옵션 40개 (`0x28`) × 4비트씩 합친다 (비트 2 켜진 칸은 부모 값 유지, 아니면 직접 지정 값, 둘 다 없으면 부모 값).
- **비트 뜻:** 표는 3×8바이트에 **옵션 40개, 옵션 하나가 4비트** (아래 비트 0~1 = 상태: 1 = 켬, 2 = 끔 `[추측]` — 코드는 `== 1` 일 때만 효과를 내고 2 는 효과가 없다, 비트 2 = 부모 값 고정). **셋째 8바이트 = 옵션 32~39, 하위 32비트만 쓴다.** `PspApplyMitigationOptions +0x510 ~ +0x5e0` (+ `+0x123 ~ +0x131`) 가 하는 일:

| 셋째 칸 비트 | `== 1` 일 때 효과 (프로세스 필드, PDB 이름) | 그 밖의 줄 |
|---|---|---|
| 0~1 | 감사표의 같은 칸이 1 이면 `MitigationFlags2 \|= 0x40000` (`AuditUserCetSetContextIpValidation`) | `+0x510 ~ +0x518`: `MitigationFlags2` 비트 14 (`CetUserShadowStacks`) 가 **꺼져 있으면 이 칸을 2 로 고쳐 씀** (`+0x500 TEST [RDI+0x754],0x4000`, `JNZ` 목적지 `+0xa08`) |
| 4~5 | 1 → `MitigationFlags2 \|= 0x200000` (`BlockNonCetBinaries`) · 3 → `0x200000 \| 0x400000` (`…NonEhcont`) · 감사표 1 → `\|= 0x800000` (`AuditBlockNonCetBinaries`) | |
| 12~13 | 1 → `\|= 0x8000000` (`PointerAuthUserIp`); 0 이면 1 로 고쳐 쓰고 같은 효과 (`+0x561 ~ +0x575`, 쓰기 `+0xe65 ~ +0xe74`); 감사표 1 → 다른 감사 비트 (`R8D`, 따라가지 않음) | |
| 16~17 | 1 → `\|= R14D` (값 안 따라감) | |
| 20~21 | 1 → `PspSetRedirectionTrustPolicy(process, 1)` (call `+0xe9d`, ret `+0xea2`) | |
| 24~25 | 1 → `MitigationFlags3 (+0x7d8) \|= 1` (`+0xea7`) | |
| 28~29 | 1 → `MitigationFlags3 \|= 6` (`+0xb9a`) | |

- **사진 `0x02002002` = 비트 0~1 = 2, 12~13 = 2, 24~25 = 2** (모두 "2", 위 표 어느 `== 1` 도 안 걸림 → 셋째 칸이 켜는 프로세스 필드 없음). 비트 0~1 의 2 는 `+0x510 ~ +0x518` 이 쓴 것 (`CetUserShadowStacks` 꺼짐 → 이 칸 := 2). **비트 12~13·24~25 의 2 는 `PspApplyMitigationOptions` 가 쓰지 않는다 → 입력 표가 이미 갖고 온 값**이다 (만들기 정보 `+0x160` 을 채우는 `PspBuildCreateProcessContext`·부모 상속 쪽) `[공백]`.
- 옵션 번호 ↔ 이름 (예: 칸 32 가 무슨 완화인지) 은 PDB 에 열거형이 없어 못 맞췄다 `[공백]`.
- **ntdll 쪽 지역 사본 `LdrpInitializeProcess +0x4b3 · +0xa09`:** 블록 `+0xb0` (8바이트) 를 `MOVSD` 로 읽어 스택 사본으로만 옮긴다 (`+0x4bb → [RSP+0x260]`, 조건 갈래 `+0x4ec → [RSP+0x280]`, `+0xa11 → [RSP+0x2a0]`). **그 사본의 비트를 ntdll 이 읽는 곳은 없다** (`[RSP+0x250 ~ +0x2a0]` 참조는 저 쓰기뿐). 블록 `+0xb0` 을 읽는 곳도 이 둘뿐 (`_refs/LdrSystemDllInitBlock.tsv`).
  - 같은 블록 첫 두 칸을 읽어 쓰는 줄: 둘째 8바이트 `+0xa8` 의 비트 4~5 (`+0x4c4 ~ +0x4d6`) = 1 이면 `LdrpEnforceIntegrityContinuity` = 1 (`+0x4db SETZ`), 1 또는 3 이면 `LdrpAuditIntegrityContinuity` = 1 (`+0x4f5 ~ +0x4fe`); 첫 8바이트 `+0xa0` 의 비트 12~13 이 1 이면 call `+0xa34` (ret `+0xa39`) `RtlSetHeapInformation(NULL, 1, 0, 0)` (힙 손상 때 종료, `EBX`=1 은 `+0x984` — `.c` 421줄과 같음).

**14. ⓙ `PspUpdatePebForAffinityChange`·`KeCopyXfdMaskToPeb`**
- `PspUpdatePebForAffinityChange(?, process)` (call `PspSetupUserProcessAddressSpace +0x22f`, ret `+0x234`; `PspAllocateProcess +0x25ae`, ret `+0x25b3` 에도 같은 호출): PEB 가 없으면(`+0x30 CMP [process+0x2e0],0` → `+0x37 JZ` 목적지 `+0x69`) 끝. 있으면 (남의 프로세스일 땐 `ExAcquireRundownProtection` 먼저, call `+0x92`) `KiStackAttachProcess` (call `+0x4c`) → **`PspWritePebAffinityInfoAttached`** (call `+0x54`, ret `+0x59`) → `KiUnstackDetachProcess`.
  - 쓰는 곳 (`PspWritePebAffinityInfoAttached`): **PEB `+0x138` `ActiveProcessAffinityMask`** (8B) `+0x6f MOV [R15+0x138],RSI` — 값 = `KeQueryPrimaryGroupAffinityProcess` (call `+0x64`, ret `+0x69`) 가 돌려준 `GROUP_AFFINITY.Mask` (`[RSP+0x20]`). WoW64 프로세스면 32비트 PEB `+0xc0` 에 `(Mask >> 32) \| Mask` 하위 32비트 (`+0x84`). 쓰고 나서 다시 물어(call `+0xc4`, ret `+0xc9`) 그룹·마스크가 바뀌었으면 다시 돈다 (`+0xd3 JNZ`·`+0xde JNZ` 목적지 `+0x38`). K1 사진 `+0x138 = 3` (CPU 0·1) 과 맞다.
- `KeCopyXfdMaskToPeb(process)` (call `PspSetupUserProcessAddressSpace +0x222`, ret `+0x227`; `PspAllocateProcess +0x259e`, ret `+0x25a3`): `+0x0 BT [KeFeatureBits],0x37` 이 꺼져 있으면 즉시 끝 (`+0xb RET`). 켜져 있으면 PEB (`[RCX+0x2e0]`) 가 있을 때 **PEB `+0x7c8` `ExtendedFeatureDisableMask`** (8B) := `KPROCESS +0x198` `ExtendedFeatureDisableMask` (조각 `FUN_1406f9d66 +0x7`); WoW64 면 32비트 PEB `+0x480` 도 같은 값 (`+0x1a`). K1 사진 `+0x7c8 = 0`, `KPROCESS+0x198 = 0` → 갈래를 안 탔는지 0 을 쓴 건지는 정적으로 못 가른다 (3.12·3.1 기록 그대로).

**15. ⓛ `PspMapSiloSharedDataView` (call `PspSetupUserProcessAddressSpace +0x25e`, ret `+0x263`; `PspAllocateProcess +0x25c8`, ret `+0x25cd`)**
- 보통 프로세스 (서버 silo 없음) 는 **일찍 나간다.** `+0xb` call `PsGetProcessServerSilo` (= `[EPROCESS+0x5f0]`, ret `+0x10`) → `+0x16` call `PsIsHostSilo` (= `silo == NULL`, ret `+0x1b`) → AL = 1 → `+0x1f JZ` 안 탐 → `+0x25 XOR EAX,EAX` → `RET`. **아무것도 안 쓰고 0 돌려준다.**
- 서버 silo 안 프로세스일 때만 (`JZ` 목적지 = 떨어진 범위 `+0xcacca`): silo 공유 데이터 섹션 (`[silo객체+0x5e0]+0x510`) 을 `MmMapViewOfSection` (call `+0xcad2f`, ret `+0xcad34`, 크기 0 = 전체, 보호 2, 할당 `0x400000`) 으로 새 프로세스에 붙이고 **PEB `+0x90` `SharedData`** := 뷰 주소 (`+0xcad48`), WoW64 면 32비트 PEB `+0x50` := 하위 32비트 (`+0xcad6a`).
- 그래서 test.exe 는 `PEB+0x90` = NULL (K1 사진 `(null)` 과 맞음), `RtlGetCurrentServiceSessionId` = 0.

**16. ⓑ PEB `+0x7b8` `LeapSecondData`**
- **읽는 ntdll 함수 둘:** `RtlpTimeToTimeFields +0x1e` (rva `0xc16be`, 명령 `MOV R10,[RAX+0x7b8]`, 앞 `+0x6` `MOV RAX,GS:[0x60]`) 와 `RtlpTimeFieldsToTime +0x1d` (rva `0xc1ced`, `MOV RDI,[RAX+0x7b8]`). 둘 다 읽은 뒤 NULL 이면 건너뛰고 (`TEST`·`JE`), 첫 바이트 `Enabled` 가 0 이어도 건너뛴다 (`CMP byte [..],0` · `JE`), 그다음 `+0x4` `Count` 를 읽는다. (같은 오프셋 `[rcx+0x7b8]` 이 `RtlpAllocateActivationContextStackFrame +0x3a5` 에도 있으나 다른 구조체 초기화 — 거짓 짝.) 찾은 방법: `ntdll.dll` 전체 `objdump` 에서 `+0x7b8]` 검색 (레지스터 상대라 `_refs` 로는 안 잡힘).
- 섹션 (`ExLeapSecondDataSectionPointer`, rva `0xfd9fe0`; 쓰는 곳 하나 `ExInitializeLeapSecondData +0x19c`, 읽는 곳 `MmCreatePeb +0x126`) — 만드는 곳이 정적으로 보인다 (**부팅 때 한 번, `Ⓑ` 단계**):
  - `ExInitializeLeapSecondData`: call `+0x3a` (ret `+0x43`) `ZwCreateSection(접근 6, 크기 0x1000, 보호 4, 할당 0x8000000 (SEC_COMMIT), 파일 없음)` → **크기 1쪽 (`0x1000`), 익명 (페이지 파일 기반) 섹션.** 시스템 공간에 붙여(`MmMapViewInSystemSpace`, call `+0xb1`) 잠그고(`MmProbeAndLockPages`) 쓰기용 주소 `ExLeapSecondData` (`rva 0xefee28`) 를 얻어 `memset(…, 0)` 후 `ExpReadLeapSecondData(…, 0)` (call `+0x1b1`) 로 채운다.
  - 내용 (`_LEAP_SECOND_DATA`, PDB `0x10`+ 가변): `+0x0` `Enabled` (1B, 레지스트리 값 `Enabled` (REG_DWORD, `ZwQueryValueKey`) ≠ 0 이면 1; 값이 없으면 1), `+0x4` `Count` (레지스트리 이진값 `LeapSeconds` 의 길이 ÷ 12), `+0x8` `Data[Count]` (항목당 8바이트, `RtlParseLeapSecondData` 가 12바이트 항목을 변환). 상한: `8 + 8×Count <= 0x1000` 아니면 오류 2 (`ExpParseAndUpdateLeapSecondData +0x4d ~ +0x5c`) → 최대 항목 511개.
  - 프로세스마다: `MmCreatePeb +0x126` 이 `ExLeapSecondDataSectionPointer` 를 읽고 `+0x132` `MmMapViewOfSection` 으로 **읽기용 뷰**를 붙여 PEB `+0x7b8` 에 주소를 둔다 (baggage 3.1 에 이미 있음).
  - 이 VM 의 레지스트리 값 (`Enabled`·`LeapSeconds`) 은 정적으로 못 안다 → 동적(`dq nt!ExLeapSecondData`).

### E. 있으면 좋음

**17. ⓕ K1 환경 블록의 변수 이름 (값 뺌)**
- **읽었다.** K1 덤프 (`ms/dump/03_baggage-snap/K1/memory.dmp`, vmss2core 가 만든 DMP64, 8GB) 를 서버에서 **읽기 전용**으로 열어: 물리 메모리 구간 표 (헤더 `0x88`) → test.exe 의 `DirectoryTableBase` `0x192c2e000` (로그 328줄) 로 4단계 페이지 걷기 → 환경 가상 주소 `0x25996cb0734`, 크기 `0xd14` (3348) 바이트 (로그 2348·2363줄) 를 읽었다. 앞 `0x200` 바이트가 로그와 같은지 확인 (`=::=::\` … `=C:=C:\…`).
  - 방법 한 줄: 임시 스크립트 (scratchpad, 커밋 안 함) — DUMP_HEADER64 의 `PhysicalMemoryBlock` 으로 물리 → 파일 오프셋, CR3 로 4단계 변환.
- 이름 47개 (값은 적지 않았다. 환경 정렬 순서 그대로):
  `=::` · `=C:` · `=ExitCode` · `ALLUSERSPROFILE` · `APPDATA` · `CommonProgramFiles` · `CommonProgramFiles(x86)` · `CommonProgramW6432` · `COMPUTERNAME` · `ComSpec` · `DriverData` · `EFC_5052_1262719628` · `EFC_5052_1592913036` · `EFC_5052_2283032206` · `EFC_5052_2775293581` · `EFC_5052_3789132940` · `EFC_5052_4126798990` · `HOMEDRIVE` · `HOMEPATH` · `LOCALAPPDATA` · `LOGONSERVER` · `NUMBER_OF_PROCESSORS` · `OneDrive` · `OS` · `Path` · `PATHEXT` · `PROCESSOR_ARCHITECTURE` · `PROCESSOR_IDENTIFIER` · `PROCESSOR_LEVEL` · `PROCESSOR_REVISION` · `ProgramData` · `ProgramFiles` · `ProgramFiles(x86)` · `ProgramW6432` · `PROMPT` · `PSModulePath` · `PUBLIC` · `SESSIONNAME` · `SystemDrive` · `SystemRoot` · `TEMP` · `TMP` · `USERDOMAIN` · `USERDOMAIN_ROAMINGPROFILE` · `USERNAME` · `USERPROFILE` · `windir`
  - `=` 로 시작하는 셋 (`=::`·`=C:`·`=ExitCode`) 은 이름 앞 `=` 가 이름 일부 (드라이브별 현재 디렉터리 / 종료 코드 관례). `EFC_5052_*` 6개는 이 VM 의 부모 셸 쪽이 넣은 것으로 보인다 `[추측]` (값이 매번 다를 수 있는 보통 변수).
  - 끝: 마지막 항목 `windir` 뒤 빈 항목 둘 (끝 널 둘) — 크기 `0xd14` 와 맞는다.
  - 이 목록은 **이 VM 의 부모 셸이 물려준 환경**이라 에뮬레이터가 그대로 베낄 값이 아니다 (PE_emulator 는 자기 환경으로 만들면 됨).

---

## 짐 지도용: 새로 알게 된 "쓴 자리" (`baggage.md` 3장 어느 절·줄)

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

### 기존 기록과 어긋난 곳 (고칠 후보)
1. `LdrpInitializeProcess +0x162` 가 `Initialized` 를 쓴다는 짐작 → 실제는 `+0x166` (이미 3.4 는 `+0x166`). 요청서의 `+0x162` 만 틀림.
2. 첫 `NtQueryInformationProcess(0x24)` 호출 위치 (baggage 1280줄, run51): 힙 관리자 쪽이 아니라 `LdrpInitializeProcess +0x12f RtlEncodePointer` 쪽이 먼저.
3. `PspSetupUserProcessAddressSpace +0x227 · +0x234 · +0x263` 은 ret 자리 (call `+0x222 · +0x22f · +0x25e`). 앞으로 "+0x.. call" / "ret +0x.." 둘 다 적자.
4. `PspAllocateProcess` 에도 `KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`·`MmMapApiSetView`·`PspMapSiloSharedDataView` 를 **직접 부르는 복사본**이 있다 (`+0x2574 ~ +0x25cd`: call `+0x259e · +0x25ae · +0x25ba · +0x25c8`). `+0x236e JNS` (`F` 비트 7 꺼짐) · `+0x2377 JZ` (11번째 인자 0) · `+0x2380 JNZ` (최소 프로세스) 중 하나로 `PspSetupUserProcessAddressSpace` (call `+0x2397`) 를 건너뛸 때 이 복사본이 돌고, 안쪽에서 다시 `F` 비트 8 켜짐 · 비트 4 꺼짐 (`+0x2574`, `+0x257e`) 이어야 한다. run32 순서 표엔 `PspSetupUserProcessAddressSpace` 쪽만 있다.

---

## PE_emulator 용 한 줄 표

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

---

## 동적분석으로 확인해야 할 것 (dast 요청 필요)

멈춤점 후보는 "이름 + 이유". 모두 정적으로 갈래·값이 못 정해진 것들.

1. **`RtlpInitializeNonVolatileFlush` 결과 값** — `LdrpInitializeProcess +0x292` 직후 `db`/`dq` : `RtlpIsFlushRequired` (`0x1cd0c0`)·`RtlpOptimalFlushMethod` (`0x1cd0b0`)·`RtlpClFlushSize` (`0x1cd0b8`)·`RtlpIsDrainRequired` (`0x1ce749`). 같이 `NtQuerySystemInformation` 반환 버퍼 32B. 추천: `ntdll!RtlpInitializeNonVolatileFlush +0x36` (ret 자리) 에서 `dq @rsp+0x20 L4`.
2. **`__isa_info`** (`ntdll+0x1ccf00`) 한 바이트 값 (예상: 이 CPU 의 SSE4.2·AVX·AVX2·ERMS 에 따라 `1` 에 `2·4·8·0x10` 을 더한 값): 추천 `ntdll!__cpu_features_init +0x7a` (store `+0x74` 바로 뒤).
3. **`PEB+0x7c8`·`KeFeatureBits` 비트 `0x37`** — `KeCopyXfdMaskToPeb +0x0` 에서 `dq nt!KeFeatureBits` 비트 55 (갈래 탔는지).
4. **`__imp_PsGetProcessParameterOverrides` 슬롯** — `dq nt!_imp_PsGetProcessParameterOverrides L2` 가 `nt!ext_ms_win_ntos_processparameters…` (스텁 `ret 0`) 를 가리키는지.
5. **`EPROCESS+0x170` (`SecureState`)** = 0 인지 (K1 로그엔 안쪽이 안 찍힘). 추천: `MmCreatePeb +0x2b8` 에서 `db @r13+0x170 L1`.
6. **`PspLocateInPEManifest` 가 불리는가** — 입구 멈춤점 (`/p` test.exe) 이나 `PspSetupUserProcessAddressSpace +0xfa` 에서 `db @rsi+0x10 L1` (비트 4). 결과는 어차피 같으니 (자원 없음) 우선순위 낮음.
7. **옵션 표 셋째 칸의 비트 12~13·24~25 = 2 의 출처** — `PspApplyMitigationOptions` 입구에서 `dq @r8 L3` (입력 표). 입구 vs 출구 비교로 `+0x510` 이 쓴 비트만 가려진다.
8. **leap second** — `dq nt!ExLeapSecondData L1` → `Enabled`·`Count` (이 VM 레지스트리 값).
9. **`UserModeGlobalLogger` (KUSD `+0x384`)·`PEB.TracingFlags`** 값 — `LdrpInitializationComplete +0x2c` 에서 `db 7ffe0384 L2` , `dd @$peb+378 L1` (ETW 갈래).
10. **`NumberOfProcessors`** (`PEB+0xb8`) 와 스핀 3칸 (`SRWLockSpinCycleCount` 등) 이 `0` 인지 `0x2800` 인지 — `LdrpInitializeProcess +0x20d` (스핀 쓰기 바로 뒤) 에서 `dd @$peb+b8 L1`, `dd ntdll+1d41a4 L1`.

## 판정

**분석성공.** 요청 17문항 모두 `.asm`·`_refs` 줄로 답했다 (D 3 · B 4 · C 9 · E 1, 번호 합 17). 부분 답은 위 "못 한 것" (13의 비트 값 출처·이름, 12의 `+0x1d8` 읽는 쪽) 둘뿐이고 다음 바퀴 몫이다. 버전 대조 일치, PDB GUID 일치. 기록과 어긋난 곳 4가지는 "기존 기록과 어긋난 곳" 에 모았다 (flow-writer 가 `baggage.md` 3.1·3.4·3.8·3.9·7.3 반영할 때 볼 것).
