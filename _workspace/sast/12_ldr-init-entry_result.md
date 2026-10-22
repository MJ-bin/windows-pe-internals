# 12 ldr-init-entry — 정적 분석 결과

- 요청서: `_workspace/sast/12_ldr-init-entry.md` (Q8, 02_sprint, 2026-10-22)
- 모듈 / 바이너리: ntdll (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntdll.dll`)
  - 바이너리 CodeView GUID+Age = `C3093720177DA6851519DCFFE8DF53FE1` = `ms/sym/ntdll.dll.pdb.info.txt` 의 값 (직접 읽어 대조, 일치).
- 뽑은 함수: 요청서 4개는 **다시 뽑지 않고** 있던 파일을 읽었다 (`LdrInitializeThunk`, `LdrpInitialize`, `LdrpInitializeInternal`, `_LdrpInitialize`). analyze 안 함.
  - 범위를 **좁게 넓힌 것** (이유는 아래): 함수 3개 export depth 0, 전역 변수 refs 11개. 전부 몇 초짜리.
- 버전 확인: **일치**. `_calls.tsv` 의 `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitialize +0x3c tail LdrpInitializeInternal`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess` 4줄이 01_sprint 관찰·알려진 사실과 같다. 손계산: `LdrpInitializeInternal` 0x18008be88+0x55 = 0x18008bedd (`CALL` 줄 주소와 같음), `_LdrpInitialize` 0x18008bf8c+0x167 = 0x18008c0f3 (같음).
- 판정: **분석성공** (아래 "판정").

## 스크립트 문제

막히는 문제는 없다. 제안 1건 (안 고쳐도 됨).

- 증상: `_LdrpInitialize +0x9a  CALL RAX` 가 `_calls.tsv` 에 `indirect:?`. CFG 검사 함수를 안 거친 평범한 `CALL RAX` 라 `indirect:cfg:<출처>` 규칙이 안 걸린다.
- 실제: 바로 앞 `+0x90 MOV RAX,[0x1801e70c8]` (`.asm` 주석에 `g_LdrpWow64LdrpInitialize` 가 이미 찍혀 있음). 같은 "RAX 를 채운 칸" 거슬러 찾기를 평범한 `CALL reg` 에도 쓰면 `indirect:g_LdrpWow64LdrpInitialize` 로 찍을 수 있다.
- 위치: `ExportFunctions.java` 의 `indirect:cfg:` 출처 찾는 부분 (줄 번호는 안 봄). 급하지 않다.

## 질문별 답 [정적]

### 1. 이름 사슬

- **PDB 이름 문자열** (`llvm-pdbutil-14 dump --publics ms/sym/ntdll.dll.pdb`, 구역 1 오프셋 + 0x1000 = RVA):

| RVA | PDB 공개 이름 (정확히) | 그 주소의 다른 이름 |
|---|---|---|
| 0xe1420 | `LdrInitializeThunk` | 없음 |
| 0xe1448 | `LdrpInitialize` | 없음 |
| 0x8be88 | `LdrpInitializeInternal` | 없음 |
| 0x8bf8c | `_LdrpInitialize` (**앞 밑줄 포함**) | 없음 |

  - 네 주소 모두 이름이 하나뿐이다 (`ms/sym/ntdll.dll.aliases.tsv` 에도 줄 없음). 세 이름은 `pdb_aliases.py --where` 결과(요청서)와 같다.
  - **밑줄은 소스에도 있다**: `_LdrpInitialize.asm +0x1ba` `LEA R8,[0x18017c7f0]` 가 가리키는 문자열이 `_LdrpInitialize` 다 (원본 바이트로 읽음). `LdrpLogInternal("minkernel\ldr\ldrinit.c", 줄, "_LdrpInitialize", ...)` 에 함수 이름으로 넘긴다 (`+0x1ba`, `+0x2da` 두 곳, `.c` 에도 보임). 링커가 붙인 장식이 아니라 MS 소스의 함수 이름이다.
- **WinDbg 가 0x8bf8c 를 `LdrpInitialize` 로 보여 준 이유**: 이름 표만으로는 설명이 안 된다. 0x8bf8c 에는 `_LdrpInitialize` 한 이름뿐이고, `LdrpInitialize` 는 0xe1448 (99바이트) 에만 있다.
  - `[정적]` 확실한 것: 콜스택의 `+0x16c` 는 0xe1448 쪽일 수 없다 (99바이트라 `+0x16c` 가 함수 밖). 0x8bf8c+0x16c = 0x8c0f8 이 `_LdrpInitialize.asm +0x16c` (`LdrpInitializeProcess` 부른 직후)다. 그래서 프레임 주인은 `_LdrpInitialize` 다.
  - `[추측]` 표시가 `LdrpInitialize` 인 건 디버거의 심볼 엔진이 공개 심볼 맨 앞 `_` 를 떼어 보여 주기 때문일 것. 정황: `runs/01_sprint/run6-symcheck.md` 표에 `_LdrpInitialize` 가 "같은 이름으로 검색됨, 별개 함수" 로 적혀 있다. 반대 정황: run8 에서 `dll1!_DllMainCRTStartup` 은 밑줄이 그대로 보였다 — 그건 sample 자체 PDB 라 같은 규칙인지 모른다. 확인은 동적 (`ln`, 아래).
- **콜스택에 0xe1448 이 안 보인 이유 = `+0x3c` 꼬리 점프 때문인가? → 예.**
  - `LdrpInitialize.asm` `+0x32 MOV RBX,[RSP+0x30]` (맡겨 둔 RBX 되찾기) → `+0x37 ADD RSP,0x20` → `+0x3b POP RDI` → `+0x3c JMP 0x18008be88` (`; TAILJMP -> LdrpInitializeInternal`). 점프 시점에 `RSP` 가 들어올 때와 같아서 `[RSP]` 는 여전히 `LdrInitializeThunk +0xe` 다. 새 돌아올 주소를 안 쌓으니 `LdrpInitialize` 프레임이 없다.
  - 그래서 `LdrpInitializeInternal` 의 `ret`(`+0x75`)은 바로 `LdrInitializeThunk +0xe` 로 간다.

### 2. `LdrpInitialize` (0xe1448, 99바이트)

- **시간 순서** (`LdrpInitialize.asm`):
  1. `+0xa CMP [SecurityCookieInitialized],0` → `+0x17 JZ +0x41` : 0 이면 `+0x41` 에서 `InitSecurityCookie` (ret `+0x46`) 를 부른다. 그 뒤 `+0x46 MOV RAX,GS:[0x60]` (PEB) → `+0x4f MOV R8,[RAX+0x20]` (PEB.ProcessParameters, PDB 형식으로 확인) → `+0x53 CMP [R8+0x8],0` (`RTL_USER_PROCESS_PARAMETERS.Flags`, +0x8 는 PDB 형식 확인) → `+0x58 JGE` : **Flags 의 맨 윗 비트(부호)가 켜져 있으면** `+0x5a` 에서 `LdrpIsSecureProcess = 1`. (그 비트 이름은 PDB 에 없다 `[공백]`.) 그 뒤 `+0x61 JMP +0x19`.
  2. `+0x19 CMP [SpecialMachineFramesInitialized],0` → `+0x20 JNZ` : 0 이면 `+0x22` `InitSpecialMachineFrames` (ret `+0x27`).
  3. `+0x27` `__cpu_features_init` (ret `+0x2c`) — **조건 없이 항상**.
  4. `+0x3c` `JMP LdrpInitializeInternal` (꼬리 점프).
- **`+0x46 InitSecurityCookie` 갈래가 타는 때**: `SecurityCookieInitialized == 0` 일 때 (`+0xa`/`+0x17`). 이 변수는 ntdll 의 `.data` 끝 0 채움 구간 (파일에 값이 없음 = 처음 0). 쓰는 곳은 `InitSecurityCookie +0x42` (`MOV [..],1`) 하나뿐이다 (`refs` → `_refs/SecurityCookieInitialized.tsv`). `SpecialMachineFramesInitialized` 도 같은 모양 (쓰는 곳 `InitSpecialMachineFrames +0x57`).
  - 그래서 새 프로세스 첫 스레드는 두 갈래를 다 타고, 나중 스레드는 둘 다 건너뛴다. 단 커널이 미리 써 넣는지는 정적으로 안 봤다 → 동적 (아래 2번).
- **`RCX`·`RDX` 는 그대로인가? → 예.** `LdrInitializeThunk` 는 `RCX` 만 `RBX` 에 쥐고 `RDX` 는 안 건드린 채 부른다 (`+0x6`, `+0x9`). `LdrpInitialize` 는 맨 처음 `+0x11 MOV RBX,RDX`, `+0x14 MOV RDI,RCX` 로 둘 다 피신시켰다가 (둘 다 호출 사이에 안 변하는 레지스터) `+0x2c MOV RDX,RBX`, `+0x2f MOV RCX,RDI` 로 되돌려 점프한다.

### 3. `LdrpInitializeInternal` (0x8be88, 251바이트)

- **들어가자마자 (+0x1a ~ +0x32)**: `TEB(GS:[0x30]) +0x17ee` 의 `SameTebFlags` 와 `0x4000` 을 검사한다. PDB `_TEB` 형식으로 `+0x17ee` = `SameTebFlags`, `0x4000` = **`SkipLoaderInit`** (비트 14), `0x2000` = `LoaderWorker`, `0x40` = `ClonedThread`, `0x20` = `RanProcessInit` 이다 (PDB 형식 직접 확인). 켜져 있으면 (`+0x32 JNZ +0x66`) 아무것도 안 하고 그냥 돌아간다 (`_LdrpInitialize` 도 안 부름).
  - 이 비트는 스레드를 만들 때 "로더 일 건너뛰기" 로 받은 스레드에만 켜질 것이다 `[추측]`. test.exe 첫 스레드가 이 비트를 갖는지는 값을 안 봤다 → 동적.
- **첫 스레드가 타는 길** (아래 + 는 `.asm` 의 call_off(ret_off)):
  1. `+0x3b LOCK CMPXCHG [LdrpHotPatchInitialized],1` (기대값 0). 이 변수는 ntdll `.data` 끝 0 채움 구간 (처음 0) 이고 `LdrpInitializeInternal` 과 `LdrpInitializationComplete` (포인터로 받아서) 밖엔 안 쓴다 (`_refs/LdrpHotPatchInitialized.tsv`). → 옛 값 0 이면 `+0x45 JZ +0x77` **"처음 온 스레드" 길**.
  2. `+0x8e` (ret `+0x93`) `NtCreateEvent(&LdrpHotPatchInitCompleteEvent, 0x1f0003, NULL, 0, FALSE)`.
  3. `+0x93` (ret `+0x98`) `LdrpInitializeHotPatching` — 여기서 `LdrpIsHotPatchingEnabled` 가 정해진다 (아래).
  4. `+0xa7` `LdrpNtdllHotPatchContext = &(이 함수의 지역 칸 [RSP+0x30])`, 지역 칸 16바이트를 0 으로.
  5. `+0x98 CMP [LdrpIsHotPatchingEnabled],0` 가 0 이 아니면 `+0xbd` (ret `+0xc2`) `LdrpLoadPatchedNtdll(RDX 원래 값, &지역칸)`, 결과를 `[RSP+0x38]` 에 저장.
  6. `+0xda` (ret `+0xdf`) `LdrpInitializationComplete(&LdrpHotPatchInitialized, &LdrpHotPatchInitCompleteEvent, 0x1488)` → `+0xdf JMP +0x4f`.
  7. `+0x4f`~`+0x55` (ret `+0x5a`) **`_LdrpInitialize(RCX 그대로, RDX 그대로)`**.
  8. 돌아와서 `+0x5a TEST EBX,EBX` (EBX = 1번의 옛 값): 0 이었으면 `+0x5e` `LdrpNtdllHotPatchContext = 0` 으로 비우고, 아니면 건너뜀 → `+0x66`~`+0x74` 레지스터 되돌림 → **`+0x75 RET`**. 돌아올 주소 = `LdrInitializeThunk +0xe` (1번 답의 꼬리 점프 때문).
  - 옛 값이 1 이면 (`+0x49 JZ +0xe4`) `+0xf2` (ret `+0xf7`) `LdrpWaitForInitializationComplete` 로 기다렸다 `+0x4f` 로. 2 (이미 끝남) 이면 아무것도 안 하고 `+0x4f`. 나중 스레드(Worker)는 이쪽이다.
- **`NtCreateEvent`·`LdrpInitializeHotPatching`·`LdrpLoadPatchedNtdll` 이 test.exe 첫 스레드에서 타나**:
  - `NtCreateEvent`(`+0x8e`)·`LdrpInitializeHotPatching`(`+0x93`): 위 1번이 "옛 값 0" 이면 **조건 없이 탄다**. 새 프로세스 첫 스레드는 `.data` 처음 값 0 을 만나므로 타는 길이다 (커널이 미리 써 놓는지는 안 봄 → 동적). 값의 출처 = 전역 변수 `LdrpHotPatchInitialized` (PEB 칸 아님).
  - `LdrpLoadPatchedNtdll`(`+0xbd`): `LdrpIsHotPatchingEnabled` (0x1801d26f0) 가 0 이 아닐 때만. **정적으로는 못 정한다.** 그 값은 `LdrpInitializeHotPatching +0x55` (`MOV [LdrpIsHotPatchingEnabled],BL`) 가 정한다 (`_refs/LdrpIsHotPatchingEnabled.tsv`):
    - `+0x14`~`+0x1f`: `PEB.ProcessParameters.Flags` (`[PEB+0x20]+0x8`) 의 `0x2000000` 비트가 켜져 있으면 곧바로 꺼짐 (`BL = 0`).
    - 아니면 `+0x40` (ret `+0x45`) `NtManageHotPatch(9, &{1,0}, 8, &out)` 를 부르고, 상태가 `0xc00000bb` 또는 `0xc0000002` 면 꺼짐, 그 밖의 값이면 켜짐 (`BL = 1` 유지). `0xc00000bb`, `0xc0000002` 가 어떤 NTSTATUS 인지는 이름 확인 안 함 `[추측: NOT_SUPPORTED / NOT_IMPLEMENTED]`.
  - 이 변수는 `LdrpInitializeProcess +0x587` 도 쓴다 (Q9 에서). `LdrpNtdllHotPatchContext` 는 `LdrpInitializeProcess +0x1233`·`+0x18ef`, `LdrpInitializeNtdllDataTableEntry +0x1b7` 이 읽는다 (Q9 에서 이어질 연결점).
- **`_LdrpInitialize` 가 돌아온 뒤** 위 8번 → `+0x75 RET` → `LdrInitializeThunk +0xe` (`MOV DL,1`, `MOV RCX,RBX`) → `+0x13` (ret `+0x18`) `NtContinue`. `LdrpInitializeInternal` 은 반환값을 안 쓴다 (`EAX` 안 봄).

### 4. `_LdrpInitialize` (0x8bf8c, 883바이트) 갈림길

- **갈림길 검사 줄**: `+0x38 XOR EAX,EAX` → **`+0x3a LOCK CMPXCHG [LdrpProcessInitialized],R15D`** (R15D = 1, 기대값 0) → `+0x43 CMP EAX,R15D` → `+0x4c ~ +0x58` PEB·`TEST EAX,EAX` → **`+0x5a JZ +0xcf`**.
  - 값의 출처: **전역 변수 `LdrpProcessInitialized`** (0x1801d2988, `.data` 0 채움 구간 = 처음 0. PEB·TEB 칸 아님). 비교 상대는 0.
  - 옛 값 **0** → 이 스레드가 첫 스레드 (변수는 1로 바뀜) → `+0xcf` 길 → `+0x167` `LdrpInitializeProcess`.
  - 옛 값 **1** (다른 스레드가 하는 중) → `+0x43 JZ +0x257`: `TEB.SameTebFlags & 0x2000` (`LoaderWorker`) 이면 기다리지 않고 나중 스레드 길로, 아니면 `+0x273` 에서 기다렸다 `+0x278 JMP +0x38` 로 다시 검사.
  - 옛 값 **2** (끝남) → 나중 스레드 길. 2 = 끝남인 근거: `LdrpInitializationComplete +0x44 INC.LOCK [상태 변수]` (1→2), `LdrFastFailInLoaderCallout +0x9 CMP [..],2`, 복제 프로세스 길의 `+0x28b MOV EAX,2 ; CMPXCHG`.
  - 그 뒤 `+0x62 CMP byte [PEB],BL` (PEB.InheritedAddressSpace) 가 0 이 아니면 **복제(fork) 프로세스의 첫 스레드** 길 (`+0x284`). 일반 새 스레드는 0 이라 `+0x6a` 로.
- **첫 스레드 길을 하는 일 순서** (`+0xcf` 부터. 괄호는 call_off(ret_off) 또는 줄):
  1. 대기 신호 만들기: `NtCreateEvent(&LdrpInitCompleteEvent, 0x1f0003, ...)` (`+0xe5`(`+0xea`)).
  2. 표시·전역 설정 (잡일): `TEB.SameTebFlags |= 0x20` (`RanProcessInit`, `+0xea`), `PEB.LoaderLock = &LdrpLoaderLock` (`+0xfa`, PEB `+0x110`), `LdrInitState = 0` (`+0x101`), `PEB.CrossProcessFlags |= 2` (`ProcessInitializing`, `+0x107`, PEB `+0x50` 비트 1 — PDB 형식으로 확인), 그리고 동적 함수 표(`RtlpDynamicFunctionTable` 계열) 머리 두 칸을 자기 자신으로·잠금과 트리 범위 5칸을 0 으로 (`+0x10d`~`+0x13e`, 쓰기 7줄).
  3. `LdrInitializeMrdata()` (`+0x145`(`+0x14a`)): 음수면 실패 길. (이름으로 보아 `.mrdata` 구역 준비 `[추측]`, 본문 안 봄.)
  4. `LdrpIsSecureProcess != 0` 이면 `LdrpDetourExist = 1` (`+0x154`/`+0x1d7`).
  5. **`LdrpInitializeProcess(RCX 그대로, RDX 그대로)`** (`+0x167`(`+0x16c`)). 이 호출은 예외 처리 범위 안이다 (아래 "예외 착지점").
  6. 음수면 로그 (`LdrpLogInternal`, 소스 줄 `0xa78`, `+0x1cd`) 후 실패 길. 아니면 `LdrpLogDllState(exe 항목 …, 0x14ae)` (`+0x18b`(`+0x190`)).
  7. `PEB.MinimumStackCommit` (PEB `+0x318`, PDB 확인) 가 0 이 아니면 `LdrpTouchThreadStack` (`+0x19c`(`+0x1a1`)).
  8. `LdrInitState = 3`, `PEB.CrossProcessFlags` 의 `ProcessInitializing` 끄기 (`+0x205`, `+0x20f`).
  9. `LdrpThreadPool != 0` 이면 `RtlpHpGCTimerEnable` (`+0x27d`(`+0x282`)).
  10. 상태가 음수면 실패 길 (아래). 아니면 `LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, 0x1484)` (`+0x24d`(`+0x252`)): 상태 1→2 + 신호 켜기 (`NtSetEvent`). 단 `UseWOW64 != 0` 이면 `LdrpProcessInitialized == 1` 일 때만 (`+0x22d`, `+0x31b`).
  11. 끝에서 `TEB.SameTebFlags & 0x2000` 이 0 이면 `NtTestAlert` (`+0xb9`(`+0xbe`)) 부르고 `RET` (`+0xcd`).
  - 실패 길: `LdrpInitializationFailure` (`+0x2f9`(`+0x2fe`)) → `NtTerminateProcess(-1, 상태)` (`+0x304`(`+0x309`)) → `RtlRaiseStatus` (`+0x315`).
  - `LdrInitializePerThreadRng` 는 첫 스레드에선 여기서 안 부른다. 대신 `LdrpInitializeProcess +0x153` 에서 부른다 (`_calls.tsv`). 나중 스레드는 `_LdrpInitialize +0x83` 에서 부른다.
- **예외 착지점 (`.asm` 끝 "읽지 않은 틈" `+0x1e5`~`+0x204`)**: 풀었다. 원본 바이트와 `UNWIND_INFO`(0x1adcac)를 직접 읽었다.
  - 범위 표: `+0x154 ~ +0x1e5` 가 보호 구역, 필터 = `_LdrpInitialize$filt$0` (rva 0x169643, 안에서 `LdrpInitializeProcessWrapperFilter` 를 부름), 착지점 = `+0x1e5`, 처리기 = `__C_specific_handler`. 즉 4번~7번 앞부분 (`LdrpInitializeProcess` 호출 포함) 에서 난 예외를 필터가 받아들이면 `+0x1e5` 로 온다.
  - 착지점은 `MOV [RSP+0x30],EAX` (EAX 는 예외 코드일 것 `[추측]`), 레지스터(EBX=0, R15D=1, R13D=0x2000, RSI=PEB, R14=TEB) 되돌리기, 그리고 `+0x205` 로 흘러든다. 그 뒤 `+0x225` 에서 상태가 음수면 실패 길 → 프로세스 종료.
  - `.c` 에는 이 try/except 가 안 그려진다.
- **`+0x9c indirect:?`**: `+0x88 CMP [UseWOW64],0` → 0 이 아니면 `+0x90 MOV RAX,[g_LdrpWow64LdrpInitialize]` (0x1801e70c8, `.mrdata`, 파일 처음 값 0) → `+0x97 MOV RCX,R12` → `+0x9a CALL RAX`. 대상 = `g_LdrpWow64LdrpInitialize` 칸의 값. 쓰는 곳: `LdrpLoadWow64 +0xe8` 이 칸 주소를 인자로 넘김 (`_refs/g_LdrpWow64LdrpInitialize.tsv`, 이름 표 `PTR_g_LdrpWow64LdrpInitialize_180173838`), 읽는 곳: 여기와 `LdrpInitializeProcess +0x17ed`. PDB 문자열 `Wow64LdrpInitialize` 가 있어서 wow64 쪽 함수를 가리킬 것 `[추측]`.
  - **첫 스레드는 안 탄다**: 이 줄은 나중 스레드 길 (`+0x80~+0x9f`) 에만 있다. 게다가 `UseWOW64` 는 `LdrpInitializeProcess +0x5f2` 만 쓰고 test.exe 는 x64 빌드(`sample/build.bat`: x64 도구만 허용)라 0 일 것 `[추측]`.
- **`+0x354 LdrpDoDebuggerBreak`**: `_LdrpInitialize` 안에서는 **복제 프로세스 첫 스레드 길에서 PEB.BeingDebugged (PEB `+0x2`, PDB 확인) 가 켜졌을 때만** 탄다.
  - 길: `+0x62` `PEB.InheritedAddressSpace != 0` → `+0x284` → `+0x29b` → `+0x32d` (락·조건 변수 0 으로, PEB 맨 앞 바이트 0 으로) → `+0x346 CMP [PEB+2],0` → 0 이 아니면 `+0x34f` (ret `+0x354`).
  - 일반 새 프로세스(test.exe) 길에선 닿지 않는다. 사용자 모드 디버거가 붙었을 때의 길이지만 복제 프로세스에서만 (CLAUDE.md "디버거가 붙어서 달라지는 점" 거리). 커널 디버거만 붙었을 때 `BeingDebugged` 가 켜지는지는 안 봤다 `[추측: 안 켜진다]`.
  - 참고 (Q9 몫): 이 함수는 `LdrpInitializeProcess +0x17d8`, `+0x1b3b` 에서도 불린다. 둘 다 바로 앞에 `[RDI+0x2]` 바이트 검사가 있다 (`+0x17d2`, `+0x1b35`). 본문은 안 봄.
- **나중 스레드가 기다리는 곳 (한 줄씩)**:
  - `LdrpWaitForInitializationComplete` (`+0x273`(`+0x278`)): **나중 스레드 길** (옛 값 1 이고 `LoaderWorker` 아님). 기다린 뒤 `+0x38` 로 가서 다시 검사. 함수 안은 신호가 있으면 `NtWaitForSingleObject`, 없으면 `NtDelayExecution` (−0x493e0 = −300000 단위, 100ns 라 30ms) 로 상태가 1 인 동안 되풀이 (`LdrpWaitForInitializationComplete.asm`).
  - SRW 잠금·조건 변수 (`+0x363`~`+0x38a`): **나중 스레드 길**, 단 로더 일이 아니라 `LdrpForkInProgress != 0` (`RtlCloneUserProcess`·`RtlPrepareForProcessCloning` 이 켜고 끔, `_refs/LdrpForkInProgress.tsv`) 인 동안 기다린다. 첫 스레드가 끝날 때까지 기다리는 곳은 위 첫 번째다.
  - 첫 스레드 쪽에서 알려 주는 곳: 위 10번 `LdrpInitializationComplete`.
- **나중 스레드(Worker) 길 요약**: 옛 값 2 → `+0x4c` → `+0x62` (InheritedAddressSpace 0) → `+0x6a` (`ClonedThread` 0) → `+0x74` (`LdrpForkInProgress` 0) → `LdrInitializePerThreadRng(TEB)` (`+0x83`(`+0x88`)) → [`UseWOW64` 이면 `CALL RAX` `+0x9a`] → `LdrpInitializeThread(RCX 그대로)` (`+0xa2`(`+0xa7`)) → `NtTestAlert` (`LoaderWorker` 아니면) → `RET`.

### 5. 커널이 넘긴 `RDX`

- `LdrInitializeThunk`: 읽지도 저장하지도 않고 그대로 `LdrpInitialize` 를 부른다 (`+0x9`). 돌아온 뒤 `+0xe MOV DL,1` 은 `NtContinue` 의 두 번째 인자를 새로 쓰는 것이라 커널이 준 값과 무관하다.
- `LdrpInitialize`: `RBX` 에 쥐었다가 (`+0x11`) `RDX` 로 돌려 (`+0x2c`) 넘긴다. 읽어서 쓰진 않는다.
- `LdrpInitializeInternal`: `RDI` 에 쥔다 (`+0x28`). 쓰이는 곳 둘: ① `LdrpIsHotPatchingEnabled` 일 때 `LdrpLoadPatchedNtdll` 의 **첫 번째 인자** (`+0xba MOV RCX,RDI`), ② `_LdrpInitialize` 의 두 번째 인자 (`+0x4f MOV RDX,RDI`).
- `_LdrpInitialize`: 들어오자마자 집 칸 (`[RSP+0x10]`) 에 저장 (`+0x0`). **첫 스레드 길에서만** `+0x15c MOV RDX,[RSP+0x88]` 로 꺼내 `LdrpInitializeProcess` 의 두 번째 인자로 넘긴다. 나중 스레드 길(`LdrpInitializeThread`, `CALL RAX`)은 `RDX` 를 안 쓴다 (`RCX` = `R12` 만).
- 이름: PDB 에 지역 변수·인자 이름이 없다 (공개 PDB). Ghidra 는 `param_2` 로만 적는다. "ntdll 이 올라간 주소" 같은 뜻 이름은 이 네 함수 안에선 안 나온다 (그래서 이름 `[추측]` 그대로).
- 정리: **`LdrpInitializeProcess` 안으로 넘어간다** (본문은 Q9). 그에 더해 핫패치가 켜진 경우 `LdrpLoadPatchedNtdll` 도 받는다.
- `RCX`(맡긴 문맥)는 `LdrInitializeThunk` 의 `RBX` 에 남고 (`+0x6`) 끝에 `NtContinue` 인자가 된다 (`+0x10`). 로더 쪽에서는 `LdrpInitializeInternal` `RSI` → `_LdrpInitialize` `R12` 로 가서 `LdrpInitializeProcess`·`LdrpInitializeThread`·`CALL RAX` 의 첫 인자가 된다.

### 6. 표기

- 조각(`frag:`)·`FAKECALL` 은 이 네 함수에 없다. `tail` 은 `LdrpInitialize +0x3c` 하나. `seh:` 는 `_LdrpInitialize$filt$0` 하나 (위). `indirect:?` 는 `+0x9a` 하나.
- `.asm` 끝 "읽지 않은 틈" 은 `_LdrpInitialize` 의 `+0x1e5~+0x204` 하나뿐 (위에서 풂). 나머지 세 함수에는 목록이 없다. `LdrpInitializeNode` 의 틈은 이 요청 범위가 아니라 안 봤다.
- `.c` 주의: `.c` 의 `_LdrpInitCompleteEvent`, `_LdrpForkActiveLock`, `_RtlpDynamicFunctionTableLock` 은 `.asm` 주석의 `LdrpInitCompleteEvent`, `LdrpForkActiveLock`, `RtlpDynamicFunctionTableLock` 과 같은 변수다 (Ghidra 가 겹침 표기). `DAT_1801e7068` = `RtlpDynamicFunctionTable +0x8`. `.c` 에는 `try/except` 가 없다. 흐름·조건은 전부 `.asm` 으로 확인했다.

## 범위를 넓힌 것과 이유

- `export ntdll.dll '^(LdrpInitializeHotPatching|LdrpInitializationComplete|LdrpWaitForInitializationComplete)$' 0` — 갈림길 값(`LdrpIsHotPatchingEnabled`)이 `LdrpInitializeHotPatching` 에서 정해지고, `2 = 끝남` 과 "나중 스레드가 기다리는 곳" 을 확인하려면 나머지 둘이 필요했다 (작은 도우미, 25/50/41줄). `.c` 머리 `callers:` 줄로 caller 가 `LdrpInitializeInternal`·`_LdrpInitialize` 임을 확인.
- `refs ntdll.dll <이름>` 11개: `SecurityCookieInitialized`, `SpecialMachineFramesInitialized`, `LdrpProcessInitialized`, `LdrpHotPatchInitialized`, `LdrpNtdllHotPatchContext`, `LdrpIsHotPatchingEnabled`, `LdrpIsSecureProcess`, `UseWOW64`, `g_LdrpWow64LdrpInitialize`, `LdrpForkInProgress`, `LdrInitState` → `ms/disassemble/ntdll/_refs/`.
- PDB 공개 이름 표·형식 (`llvm-pdbutil-14 dump --publics --types ms/sym/ntdll.dll.pdb`, 임시 파일은 scratchpad) 과 원본 바이트 (`objdump`, `UNWIND_INFO` 읽기). analyze 는 안 했다.
- `ms/decomp`·`ms/disassemble` 에 들어온 새 파일은 위 함수 3개의 `.c`·`.asm` 이고, `_calls.tsv` 에 그 줄이 더해졌다.

## flow.md 2.4 그림 고칠 안

```
LdrInitializeThunk                         ← Rcx = 맡긴 문맥, Rdx = 커널 PspSystemDlls 칸 값 [추측: ntdll 주소]
 ├─ (1) +0xe    LdrpInitialize (0xe1448)      ← +0x3c 꼬리 JMP: 프레임이 안 남아 콜스택엔 없음
 │      └─ (JMP) LdrpInitializeInternal (0x8be88)   ← 콜스택 +0x5a 가 이 자리 (SameTebFlags 0x4000 아니면 진행)
 │            └─ +0x5a  _LdrpInitialize (0x8bf8c)   ← WinDbg 는 `LdrpInitialize` 로 보임 (밑줄 없이) [추측]
 │                  ├─ +0x16c  LdrpInitializeProcess   ← 첫 스레드 (LdrpProcessInitialized 옛 값 0)
 │                  └─ +0xa7   LdrpInitializeThread    ← 나중 스레드 (옛 값 2)
 └─ (2) +0x18   NtContinue(맡긴 문맥, TRUE)
```

- 본문 이름도 같이: 2.4 `ntdll!LdrpInitialize` 소제목은 `_LdrpInitialize` (0x8bf8c, 콜스택 이름 `LdrpInitialize`) 로. 거기 `[추측]` "첫 스레드인가를 보고 갈림길" 은 `[정적]` 로 올릴 수 있다 (근거: `+0x3a` 전역 변수 `LdrpProcessInitialized` 의 `CMPXCHG`, 옛 값 0/1/2). `+0xe` 줄의 "이름 정리는 Q8" 은 닫을 수 있다.
- 4장의 `[공백]` "대상 이름: 관찰 `LdrpInitializeInternal` vs 정적 `LdrpInitialize`" 도 닫는다: 콜스택에 `LdrpInitializeInternal` 이 보인 건 `LdrpInitialize` 가 꼬리 점프로 비켜서이고, `+0x5a` 를 부르는 쪽은 `_LdrpInitialize` 다.

## 동적분석으로 확인해야 할 것

1. **WinDbg 이름 표시**: `ln ntdll+0x8c0f8` 과 `x ntdll!*LdrpInitialize` — 0x8bf8c 가 어떻게 나오는지 (`_` 가 떨어지는지). 1번 `[추측]` 확인.
2. **첫 스레드에서 두 갈래가 타는지**: `LdrpInitialize` 입구와 `+0x3c` 직전에서 `rcx`·`rdx` 비교 (같아야 함, `rdx` 는 `lm m ntdll` 주소와 비교), `db ntdll+0x1d23ce L1` (`SecurityCookieInitialized`), `db ntdll+0x1d23d0 L1` (`SpecialMachineFramesInitialized`). 첫 스레드 0, Worker 1 이어야 한다.
3. **`LdrpInitializeInternal`**: 입구에서 `dw @$teb+0x17ee L1` (`0x4000`·`0x2000` 비트 0 인지), `dd ntdll+0x1d2928 L1` (`LdrpHotPatchInitialized` 처음 값 0). `+0x98` 에서 `db ntdll+0x1d26f0 L1` (`LdrpIsHotPatchingEnabled`) 과 `LdrpInitializeHotPatching +0x45` 에서 `r eax` (`NtManageHotPatch` 상태) — 이 둘이 `LdrpLoadPatchedNtdll` 이 불리는지를 정한다.
4. **`_LdrpInitialize +0x43` 에서 `r eax`** (`CMPXCHG` 옛 값): 첫 스레드 0, Worker 2 인지. `+0x16c` 도착 때 `eax` (`LdrpInitializeProcess` 반환값).
5. `+0x9a` 가 안 타는지 (`UseWOW64` = `db`/`dd ntdll+0x1d2868` 0). `+0x354` 도 test.exe 에선 안 걸려야 한다.
6. 돌아오는 길: `ntdll+0xe142e` (`LdrInitializeThunk +0xe`) 에 도착하는 시점이 `LdrpInitializeProcess` 가 모두 끝난 뒤인지 (`k`).
- **추천 멈춤점** (`/p <test EPROCESS>` 로 test.exe 만, dast 가 정함). 이름이 겹치니 **주소로**: `ntdll+0xe1448`, `ntdll+0xe1484` (`+0x3c` JMP 직전), `ntdll!LdrpInitializeInternal` (`+0x0`, `+0x98`, `+0x5a`), `ntdll+0x8bf8c` (`_LdrpInitialize`), `ntdll+0x8bfcf` (`+0x43`), `ntdll+0x8c0f8` (`+0x16c` 반환), `ntdll+0xe142e`.
- dast 요청 필요: 위 1~4 (한 요청으로 묶을 수 있음, 값 읽기 위주).

## Q9 로 넘길 점

- `LdrpInitializeProcess` 에서 `LdrInitializePerThreadRng` (`+0x153`), `UseWOW64` 쓰기 (`+0x5f2`), `LdrpIsHotPatchingEnabled` 쓰기 (`+0x587`), `LdrpNtdllHotPatchContext` 읽기 (`+0x1233`, `+0x18ef`), `LdrpDoDebuggerBreak` (`+0x17d8`, `+0x1b3b`, 앞 `[RDI+2]` 검사), `LdrInitState` 쓰기 (`+0x17dd`, `+0x1a4c`, `+0x1b1e`), `g_LdrpWow64LdrpInitialize` 읽기 (`+0x17ed`).

## 판정

**분석성공** — 요청서 질문 1~6 에 답이 있다. 이름 사슬은 PDB 와 소스 문자열로 확정했고 (`_LdrpInitialize` 의 밑줄은 진짜), 갈림길 검사 줄과 값의 출처를 찾았다. 정적으로 못 정한 건 런타임 값 (`LdrpIsHotPatchingEnabled`, `SameTebFlags` 값, 0 채움 변수를 커널이 미리 쓰는지) 과 WinDbg 가 밑줄을 떼는 이유 `[추측]` 이고 모두 위 동적 목록에 있다.
