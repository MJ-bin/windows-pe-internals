# 2026-10-22 run17 — 로더 입구 이름 사슬 정리: `LdrpInitialize`(0xe1448) 는 꼬리 점프로 비켜서고, 콜스택의 `LdrpInitialize+0x16c` 는 `_LdrpInitialize`(0x8bf8c) 다. 첫 스레드 판정 = 전역 `LdrpProcessInitialized` (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`
  - 바이너리 CodeView 값과 `ms/sym/ntdll.dll.pdb.info.txt` 값을 직접 대조해 일치 (결과서).
  - analyze 는 다시 안 했다. 요청서 4개 함수는 **이미 뽑혀 있던 파일**을 읽었다.
  - 버전 대조: 일치. `_calls.tsv` 의 `LdrInitializeThunk +0xe`, `LdrpInitialize +0x3c tail`, `LdrpInitializeInternal +0x5a`, `_LdrpInitialize +0x16c` 가 01_sprint 관찰과 같다. 손계산 `0x18008be88+0x55 = 0x18008bedd`, `0x18008bf8c+0x167 = 0x18008c0f3` 도 `CALL` 줄 주소와 같다 (결과서).
- 근거로 읽은 함수
  - 요청서 4개: `LdrInitializeThunk` (rva `0xe1420`), `LdrpInitialize` (`0xe1448`, 99바이트), `LdrpInitializeInternal` (`0x8be88`, 251바이트), `_LdrpInitialize` (`0x8bf8c`, 883바이트)
  - 넓힌 것 3개 (depth 0): `LdrpInitializeHotPatching`, `LdrpInitializationComplete`, `LdrpWaitForInitializationComplete` — 갈림 값이 정해지는 곳, "2 = 끝남", 나중 스레드가 기다리는 곳을 보려고.
  - 전역 변수 참조(`refs`) 11개 → `ms/disassemble/ntdll/_refs/`

## 목적

- 대기열 **Q8** (단계 2 = ntdll 지도의 첫 항목). flow.md 2.4 의 두 가지를 닫는다.
  1. **이름 사슬**: 01_sprint 콜스택 `LdrInitializeThunk+0xe` ← `LdrpInitializeInternal+0x5a` ← `LdrpInitialize+0x16c` 와 정적 이름(`LdrpInitialize` 0xe1448 의 꼬리 점프, `_LdrpInitialize` 0x8bf8c) 이 어떻게 맞는가.
  2. **갈림길**: `[추측]` "첫 스레드인가를 보고 고른다" 의 실제 검사 줄과 값의 출처.
- 덤으로: 커널이 넘긴 `RDX` 가 어디까지 가나 (run16 이 Q8 로 미룸), 디버거가 붙었을 때만 타는 길.

## 방법

- 정적: `_workspace/sast/12_ldr-init-entry.md` — ntdll, `^(LdrInitializeThunk|LdrpInitialize|LdrpInitializeInternal|_LdrpInitialize)$` depth 0 (+ 도우미 3개 depth 0, refs 11개)
- 결과서: `_workspace/sast/12_ldr-init-entry_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/{LdrInitializeThunk,LdrpInitialize,LdrpInitializeInternal,_LdrpInitialize,LdrpInitializeHotPatching,LdrpInitializationComplete,LdrpWaitForInitializationComplete}.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/*.tsv`
- 이름은 **PDB 공개 심볼 표**(`llvm-pdbutil-14 dump --publics`)로, 구조체 칸 이름(`_TEB`, `_PEB`, `RTL_USER_PROCESS_PARAMETERS`)은 **PDB 타입 레코드**로 맞췄다 (결과서).
- `.asm` 에 없는 곳(`_LdrpInitialize` 예외 착지점 `+0x1e5~+0x204`, 문자열 `"_LdrpInitialize"`)은 결과서가 **원본 바이트**와 `UNWIND_INFO` 로 읽었다. 이 문서에서 "(원본 바이트)" 라고 붙인 곳이 그것이다.
- 오프셋 읽는 법 (run9 ~ 16 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.

## 결과

한 줄로: **입구 `LdrpInitialize` 는 잡일 몇 개 하고 꼬리 점프로 빠지고, `LdrpInitializeInternal` 이 핫패치 준비를 먼저 한 뒤 `_LdrpInitialize` 를 부른다. `_LdrpInitialize` 는 전역 깃발 `LdrpProcessInitialized` 를 0 → 1 로 먼저 바꾼 스레드에게 "프로세스 전체 준비"(`LdrpInitializeProcess`) 를 맡긴다.**

- 비유: 새 직원들이 같은 사무실에 처음 온다. 문 앞 칠판(`LdrpProcessInitialized`)에 "0 = 아직 아무도 안 함" 이라고 적혀 있다. **제일 먼저 칠판을 1 로 고친 사람**이 사무실 전체 세팅을 맡고, 다 끝나면 2 로 고치고 종(이벤트)을 친다. 늦게 온 사람은 1 이면 종이 울릴 때까지 기다리고, 2 면 자기 자리 정리(`LdrpInitializeThread`)만 한다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrInitializeThunk` | `+0x9` | `+0xe` | `ntdll!LdrpInitialize` (0xe1448) | [26100.9457 확인] (ret `+0xe`, run7) + [정적] (대상 이름) |
| `ntdll!LdrpInitialize` | `+0x41` | `+0x46` | `ntdll!InitSecurityCookie` (`SecurityCookieInitialized == 0` 일 때) | [정적] |
| `ntdll!LdrpInitialize` | `+0x22` | `+0x27` | `ntdll!InitSpecialMachineFrames` (`SpecialMachineFramesInitialized == 0` 일 때) | [정적] |
| `ntdll!LdrpInitialize` | `+0x27` | `+0x2c` | `ntdll!__cpu_features_init` (늘) | [정적] |
| `ntdll!LdrpInitialize` | `+0x3c` | (꼬리 JMP) | `ntdll!LdrpInitializeInternal` | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0x8e` | `+0x93` | `ntdll!NtCreateEvent` (핫패치 끝 신호) | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0x93` | `+0x98` | `ntdll!LdrpInitializeHotPatching` | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0xbd` | `+0xc2` | `ntdll!LdrpLoadPatchedNtdll` (`LdrpIsHotPatchingEnabled != 0` 일 때만) | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0xda` | `+0xdf` | `ntdll!LdrpInitializationComplete` (핫패치 상태 1 → 2) | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0x55` | `+0x5a` | `ntdll!_LdrpInitialize` (0x8bf8c) | [26100.9457 확인] (ret `+0x5a`, run7) + [정적] |
| `ntdll!LdrpInitializeInternal` | `+0xf2` | `+0xf7` | `ntdll!LdrpWaitForInitializationComplete` (늦게 온 스레드) | [정적] |
| `ntdll!LdrpInitializeHotPatching` | `+0x40` | `+0x45` | `ntdll!NtManageHotPatch` | [정적] |
| `ntdll!_LdrpInitialize` | `+0xe5` | `+0xea` | `ntdll!NtCreateEvent` (`LdrpInitCompleteEvent`) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x145` | `+0x14a` | `ntdll!LdrInitializeMrdata` | [정적] |
| `ntdll!_LdrpInitialize` | `+0x167` | **`+0x16c`** | `ntdll!LdrpInitializeProcess` (첫 스레드) | [26100.9457 확인] (run7) + [정적] |
| `ntdll!_LdrpInitialize` | `+0x18b` | `+0x190` | `ntdll!LdrpLogDllState` | [정적] |
| `ntdll!_LdrpInitialize` | `+0x19c` | `+0x1a1` | `ntdll!LdrpTouchThreadStack` (`PEB.MinimumStackCommit != 0` 일 때) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x24d` | `+0x252` | `ntdll!LdrpInitializationComplete` (로더 상태 1 → 2, 신호 켜기) | [정적] |
| `ntdll!_LdrpInitialize` | `+0xb9` | `+0xbe` | `ntdll!NtTestAlert` (`LoaderWorker` 아닐 때, 두 길 공통 끝) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x83` | `+0x88` | `ntdll!LdrInitializePerThreadRng` (나중 스레드 길) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x9a` | `+0x9c` | 간접 `CALL RAX` ← `g_LdrpWow64LdrpInitialize` (나중 스레드 길, `UseWOW64 != 0` 일 때) | [정적] |
| `ntdll!_LdrpInitialize` | `+0xa2` | **`+0xa7`** | `ntdll!LdrpInitializeThread` (나중 스레드) | [26100.9457 확인] (Worker, run7) + [정적] |
| `ntdll!_LdrpInitialize` | `+0x273` | `+0x278` | `ntdll!LdrpWaitForInitializationComplete` (상태 1 이고 `LoaderWorker` 아님) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x34f` | `+0x354` | `ntdll!LdrpDoDebuggerBreak` (복제 프로세스 + `BeingDebugged`) | [정적] |
| `ntdll!_LdrpInitialize` | `+0x2f9` / `+0x304` | `+0x2fe` / `+0x309` | `ntdll!LdrpInitializationFailure` / `ntdll!NtTerminateProcess` (실패 길) | [정적] |

- 같은 장부의 나머지 (`LdrpLogInternal` 2곳, `RtlpHpGCTimerEnable` `+0x282`, 복제 길 `LdrpInitializationComplete` `+0x2be`, `RtlRaiseStatus` `+0x31a`, SRW 잠금·조건 변수 3곳 `+0x368`~`+0x38f`, `seh:` `LdrpInitializeProcessWrapperFilter`) 는 개수만 둔다.

### 정적 — ① 이름 사슬

PDB 공개 이름 (결과서, 네 주소 모두 이름이 하나뿐):

| rva | PDB 공개 이름 (정확히) | 크기 |
|---|---|---|
| `0xe1420` | `LdrInitializeThunk` | 32 |
| `0xe1448` | `LdrpInitialize` | 99 |
| `0x8be88` | `LdrpInitializeInternal` | 251 |
| `0x8bf8c` | **`_LdrpInitialize`** (앞 밑줄 포함) | 883 |

- **밑줄은 MS 소스의 함수 이름이다.** `_LdrpInitialize +0x1ba`·`+0x2da` 가 로그 함수 `LdrpLogInternal` 에 넘기는 함수 이름 문자열이 `"_LdrpInitialize"` 다 (원본 바이트). 링커 장식이 아니다.
- **콜스택 `LdrpInitialize+0x16c` 의 주인은 `_LdrpInitialize` 다.** 0xe1448 쪽은 99바이트라 `+0x16c` 가 함수 밖이다. `0x8bf8c + 0x16c` 는 `_LdrpInitialize` 의 `LdrpInitializeProcess` 부른 직후 자리다.
  - WinDbg 가 왜 밑줄 없이 보였는지는 이름 표로 설명이 안 된다 `[추측]` (아래 "정적과 동적이 다른 곳").
- **콜스택에 0xe1448(`LdrpInitialize`) 이 안 보인 이유 = `+0x3c` 꼬리 점프.** `+0x32`~`+0x3b` 에서 스택을 들어올 때 모양으로 되돌리고 `+0x3c JMP` 한다. 새 돌아올 주소를 쌓지 않으니 `[RSP]` 는 여전히 `LdrInitializeThunk +0xe` 다. 그래서 `LdrpInitializeInternal` 의 `+0x75 RET` 은 곧장 `LdrInitializeThunk +0xe` 로 간다.

### 정적 — ② `LdrpInitialize` (0xe1448): 잡일 후 꼬리 점프

| 순서 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0xa`, `+0x17` → `+0x41` (ret `+0x46`) | 전역 `SecurityCookieInitialized == 0` 이면 `InitSecurityCookie`. 그 뒤 `PEB.ProcessParameters.Flags` 맨 윗 비트가 켜져 있으면 `LdrpIsSecureProcess = 1` (`+0x46`~`+0x5a`). 그 비트 이름은 PDB 에 없다 |
| 2 | `+0x19`, `+0x20` → `+0x22` (ret `+0x27`) | 전역 `SpecialMachineFramesInitialized == 0` 이면 `InitSpecialMachineFrames` |
| 3 | `+0x27` (ret `+0x2c`) | `__cpu_features_init` — 조건 없이 늘 |
| 4 | `+0x3c` | **`JMP LdrpInitializeInternal`** |

- 두 깃발 변수는 ntdll `.data` 끝 0 채움 구간(처음 값 0)이고, 1 로 쓰는 곳은 각각 `InitSecurityCookie +0x42`, `InitSpecialMachineFrames +0x57` 하나뿐이다 (`_refs/`). 그래서 **새 프로세스 첫 스레드는 두 갈래를 다 타고, 나중 스레드는 건너뛴다** `[정적]`. 커널이 미리 써 두는지는 안 봤다 (동적).
- `RCX`·`RDX` 는 **받은 그대로** 넘긴다: `+0x11 MOV RBX,RDX`, `+0x14 MOV RDI,RCX` 로 피신 → `+0x2c`/`+0x2f` 로 되돌려 점프.

### 정적 — ③ `LdrpInitializeInternal` (0x8be88): 핫패치 준비 → `_LdrpInitialize`

| 순서 | 자리 | 하는 일 |
|---|---|---|
| 0 | `+0x1a`~`+0x32` | `TEB.SameTebFlags`(`+0x17ee`) 의 **`SkipLoaderInit`(0x4000)** 이 켜져 있으면 아무것도 안 하고 돌아감 (`_LdrpInitialize` 도 안 부름) |
| 1 | `+0x3b` | `LOCK CMPXCHG [LdrpHotPatchInitialized], 1` (기대 0). 옛 값 0 이면 "처음 온 스레드" 길 (`+0x45 JZ`) |
| 2 | `+0x8e` (ret `+0x93`) | `NtCreateEvent(&LdrpHotPatchInitCompleteEvent, …)` |
| 3 | `+0x93` (ret `+0x98`) | `LdrpInitializeHotPatching` — 여기서 `LdrpIsHotPatchingEnabled` 가 정해진다 |
| 4 | `+0xa7`~ | `LdrpNtdllHotPatchContext` = 이 함수 지역 칸 주소, 0 으로 채움 |
| 5 | `+0x98` 검사 → `+0xbd` (ret `+0xc2`) | `LdrpIsHotPatchingEnabled != 0` 이면 `LdrpLoadPatchedNtdll(RDX 원래 값, &지역 칸)` |
| 6 | `+0xda` (ret `+0xdf`) | `LdrpInitializationComplete(&LdrpHotPatchInitialized, &…Event, …)` — 상태 1 → 2 |
| 7 | `+0x4f`~`+0x55` (ret **`+0x5a`**) | **`_LdrpInitialize(RCX 그대로, RDX 그대로)`** |
| 8 | `+0x5a`~`+0x75` | 1번 옛 값이 0 이었으면 `LdrpNtdllHotPatchContext = 0` 으로 비움 → **`+0x75 RET`** → `LdrInitializeThunk +0xe` |

- 옛 값 1 (다른 스레드가 하는 중) 이면 `+0xf2` (ret `+0xf7`) `LdrpWaitForInitializationComplete` 로 기다렸다 7번으로. 옛 값 2 (끝남) 면 바로 7번으로.
- `LdrpIsHotPatchingEnabled` 가 정해지는 법 (`LdrpInitializeHotPatching`): `PEB.ProcessParameters.Flags` 의 `0x2000000` 비트가 켜져 있으면 꺼짐. 아니면 `+0x40` (ret `+0x45`) `NtManageHotPatch(9, …)` 결과가 `0xc00000bb`·`0xc0000002` 면 꺼짐, 그 밖이면 켜짐. **정적으로는 test.exe 때 값을 못 정한다** → 동적. (두 NTSTATUS 이름은 확인 안 함 `[추측: NOT_SUPPORTED / NOT_IMPLEMENTED]`.)
- `LdrpInitializeInternal` 은 `_LdrpInitialize` 반환값(`EAX`)을 안 본다.

### 정적 — ④ `_LdrpInitialize` (0x8bf8c): 첫 스레드 판정과 두 갈래

**갈림 검사 줄**: `+0x38 XOR EAX,EAX` → **`+0x3a LOCK CMPXCHG [LdrpProcessInitialized], R15D`** (R15D = 1, 기대 0) → `+0x43 CMP EAX,R15D` → `+0x58 TEST EAX,EAX` → **`+0x5a JZ +0xcf`**.

| `LdrpProcessInitialized` 옛 값 | 뜻 | 가는 길 |
|---|---|---|
| **0** | 아무도 안 했다 → 이 스레드가 첫 스레드 (값은 1 로 바뀜) | `+0xcf` → … → `+0x167` (ret **`+0x16c`**) **`LdrpInitializeProcess`** |
| 1 | 다른 스레드가 하는 중 | `+0x43 CMP` → `+0x46 JZ +0x257`: `TEB.SameTebFlags & 0x2000`(`LoaderWorker`) 면 기다리지 않고 나중 스레드 길, 아니면 `+0x273` (ret `+0x278`) `LdrpWaitForInitializationComplete` 뒤 `+0x38` 로 다시 검사 |
| 2 | 끝났다 | 나중 스레드 길 → `+0xa2` (ret **`+0xa7`**) **`LdrpInitializeThread`** |

- 값의 출처 = **ntdll 전역 변수 `LdrpProcessInitialized`** (rva `0x1d2988`, `.data` 0 채움 구간 = 처음 0). PEB·TEB 칸이 아니다.
- "2 = 끝남" 근거: `LdrpInitializationComplete +0x44` 가 `LOCK INC` (1 → 2), `LdrFastFailInLoaderCallout +0x9 CMP [..],2`, 복제 길 `+0x28b MOV EAX,2` (결과서).
- 그 뒤 `+0x62` `PEB.InheritedAddressSpace` 가 0 이 아니면 **복제(fork) 프로세스 첫 스레드** 길 (`+0x284`). 보통 새 프로세스는 0.

첫 스레드 길 (`+0xcf` 부터, 하는 일 단위):

| 순서 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0xe5` (ret `+0xea`) | `NtCreateEvent(&LdrpInitCompleteEvent, …)` — 늦게 온 스레드가 기다릴 종 |
| 2 | `+0xea`~`+0x13e` | 잡일 7줄 남짓: `TEB.SameTebFlags |= RanProcessInit(0x20)`, `PEB.LoaderLock = &LdrpLoaderLock`, `LdrInitState = 0`, `PEB.CrossProcessFlags |= ProcessInitializing(2)`, 동적 함수 표 머리 초기화 |
| 3 | `+0x145` (ret `+0x14a`) | `LdrInitializeMrdata()` — 음수면 실패 길. 하는 일은 이름으로만 `[추측: .mrdata 구역 준비]` |
| 4 | `+0x154` | `LdrpIsSecureProcess != 0` 이면 `LdrpDetourExist = 1` |
| 5 | `+0x167` (ret **`+0x16c`**) | **`LdrpInitializeProcess(RCX 그대로, RDX 그대로)`** — 예외 보호 구역 안 |
| 6 | `+0x18b` (ret `+0x190`) | 성공이면 `LdrpLogDllState(…)`. 음수면 `LdrpLogInternal` 후 실패 길 |
| 7 | `+0x19c` (ret `+0x1a1`) | `PEB.MinimumStackCommit != 0` 이면 `LdrpTouchThreadStack` |
| 8 | `+0x205`, `+0x20f` | `LdrInitState = 3`, `ProcessInitializing` 끄기 |
| 9 | `+0x27d` (ret `+0x282`) | `LdrpThreadPool != 0` 이면 `RtlpHpGCTimerEnable` |
| 10 | `+0x24d` (ret `+0x252`) | **`LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, …)`** — 상태 1 → 2, 종 치기(`NtSetEvent`) |
| 11 | `+0xb9` (ret `+0xbe`) → `+0xcd` | `LoaderWorker` 아니면 `NtTestAlert`, 그리고 `RET` |

- 실패 길: `LdrpInitializationFailure` (ret `+0x2fe`) → `NtTerminateProcess(-1, 상태)` (ret `+0x309`) → `RtlRaiseStatus`.
- **예외 착지점** (`.asm` 끝 "읽지 않은 틈" `+0x1e5`~`+0x204`, 원본 바이트·`UNWIND_INFO`): 보호 구역 `+0x154`~`+0x1e5`, 필터 `_LdrpInitialize$filt$0` (안에서 `LdrpInitializeProcessWrapperFilter`), 착지점 `+0x1e5`, 처리기 `__C_specific_handler`. 즉 `LdrpInitializeProcess` 안에서 난 예외를 필터가 받으면 `+0x1e5` → `+0x205` 로 흘러 상태가 음수면 프로세스 종료. `.c` 에는 이 try/except 가 안 나온다.
- `LdrInitializePerThreadRng` 는 첫 스레드 길엔 없다. 첫 스레드는 `LdrpInitializeProcess +0x153` 에서 부른다 (`_calls.tsv`).

나중 스레드 길 (옛 값 2): `+0x62` (InheritedAddressSpace 0) → `+0x6a` (`ClonedThread` 0) → `+0x74` (`LdrpForkInProgress` 0) → `+0x83` (ret `+0x88`) `LdrInitializePerThreadRng(TEB)` → [`UseWOW64 != 0` 이면 `+0x9a CALL RAX`] → `+0xa2` (ret `+0xa7`) `LdrpInitializeThread(RCX 그대로)` → `NtTestAlert` → `RET`.

- `+0x9a CALL RAX` 의 대상 = 전역 `g_LdrpWow64LdrpInitialize` 칸 값 (`+0x90`, `.mrdata`, 파일 처음 값 0). 그 칸 주소를 넘기는 곳은 `LdrpLoadWow64 +0xe8` (`_refs/`). 첫 스레드 길엔 이 줄이 없다. `UseWOW64` 는 `LdrpInitializeProcess +0x5f2` 만 쓰고, test.exe 는 x64 빌드(`sample/build.bat` `[소스]`)라 0 일 것 `[추측]`.
- SRW 잠금·조건 변수 기다림 (`+0x363`~`+0x38a`) 도 나중 스레드 길이지만, 로더 일이 아니라 **`LdrpForkInProgress != 0` (프로세스 복제 중)** 인 동안 기다리는 곳이다.

### 정적 — ⑤ 커널이 넘긴 `RDX` 의 길

| 함수 | 어떻게 다루나 |
|---|---|
| `LdrInitializeThunk` | 안 건드리고 `+0x9` 에서 그대로 부름. `+0xe MOV DL,1` 은 `NtContinue` 인자를 새로 쓰는 것 |
| `LdrpInitialize` | `RBX` 에 피신 (`+0x11`) → `RDX` 로 되돌려 점프 (`+0x2c`) |
| `LdrpInitializeInternal` | `RDI` 에 쥠 (`+0x28`). ① 핫패치가 켜졌으면 `LdrpLoadPatchedNtdll` **첫 인자** (`+0xba`), ② `_LdrpInitialize` 둘째 인자 (`+0x4f`) |
| `_LdrpInitialize` | 집 칸에 저장 (`+0x0`). **첫 스레드 길에서만** `+0x15c` 로 꺼내 `LdrpInitializeProcess` 둘째 인자로. 나중 스레드 길은 안 씀 |

- 공개 PDB 라 인자 이름은 없다. "ntdll 이 올라간 주소" 라는 이름은 여전히 `[추측]`.
- `RCX`(맡긴 문맥)는 `LdrInitializeThunk` 의 `RBX` 에 남아 끝에 `NtContinue` 인자가 되고, 로더 쪽으로는 `LdrpInitializeInternal` `RSI` → `_LdrpInitialize` `R12` 로 가서 `LdrpInitializeProcess`·`LdrpInitializeThread`·`CALL RAX` 의 첫 인자가 된다.

### 호출 관계

```
LdrInitializeThunk                          ← Rcx = 맡긴 문맥, Rdx = 커널 PspSystemDlls[0]+0x20 값 [추측: ntdll 주소]
 ├─ (1) +0xe   LdrpInitialize (0xe1448)       [관찰] ret +0xe (run7) + [정적] 이름
 │      ├─ (1) +0x46  InitSecurityCookie        (처음 한 번)            [정적]
 │      ├─ (2) +0x27  InitSpecialMachineFrames  (처음 한 번)            [정적]
 │      ├─ (3) +0x2c  __cpu_features_init       (늘)                    [정적]
 │      └─ (4) ═▶ +0x3c JMP  LdrpInitializeInternal (0x8be88)   ← 꼬리 점프, 콜스택엔 0xe1448 안 보임
 │              ├─ (1) +0x93  NtCreateEvent                 ┐ 핫패치 깃발 옛 값 0 일 때    [정적]
 │              ├─ (2) +0x98  LdrpInitializeHotPatching     │
 │              ├─ (3) +0xc2  LdrpLoadPatchedNtdll          │ (LdrpIsHotPatchingEnabled 일 때만)
 │              ├─ (4) +0xdf  LdrpInitializationComplete    ┘
 │              └─ (5) +0x5a  _LdrpInitialize (0x8bf8c)      [관찰] run7 (표시 이름 LdrpInitialize) + [정적]
 │                      │  +0x3a CMPXCHG LdrpProcessInitialized (옛 값 0 / 1 / 2)
 │                      ├─ 첫 스레드 (옛 값 0)
 │                      │   ├─ (1) +0xea   NtCreateEvent (LdrpInitCompleteEvent)  [정적]
 │                      │   ├─ (2) +0x14a  LdrInitializeMrdata                     [정적]
 │                      │   ├─ (3) +0x16c  LdrpInitializeProcess                   [관찰] run7 + [정적]
 │                      │   ├─ (4) +0x252  LdrpInitializationComplete (1 → 2)      [정적]
 │                      │   └─ (5) +0xbe   NtTestAlert                              [정적]
 │                      └─ 나중 스레드 (옛 값 2; 1 이면 +0x278 LdrpWaitForInitializationComplete 뒤 다시 검사)
 │                          ├─ (1) +0x88   LdrInitializePerThreadRng               [정적]
 │                          ├─ (2) +0xa7   LdrpInitializeThread                     [관찰] run7 Worker + [정적]
 │                          └─ (3) +0xbe   NtTestAlert                              [정적]
 └─ (2) +0x18  NtContinue(맡긴 문맥, TRUE)                                          [정적] run16
```

## 확인된 것

- **`LdrInitializeThunk +0xe` 가 부르는 건 `LdrpInitialize`(0xe1448) 이고, 그 함수는 `+0x3c` 꼬리 점프로 `LdrpInitializeInternal` 에 넘긴다.** [정적] 점프 때 스택이 들어올 때 모양이라 프레임이 안 남는다. 그래서 run7 콜스택에 `LdrpInitializeInternal` 이 `LdrInitializeThunk+0xe` 바로 위로 보였다 (run16 `[추측]` → `[정적]`).
- **콜스택 `LdrpInitialize+0x16c`·`+0xa7` 의 주인은 `_LdrpInitialize`(0x8bf8c) 다.** [정적] PDB 공개 이름에 밑줄이 붙어 있고 (`"_LdrpInitialize"` 문자열도 코드에 있음), 0xe1448 은 99바이트라 `+0x16c` 를 가질 수 없다.
- **첫 스레드 판정 = 전역 `LdrpProcessInitialized` 를 `LOCK CMPXCHG` 로 0 → 1 바꾸는 데 성공했나.** [정적] `_LdrpInitialize +0x3a`, 갈림 `+0x5a`. flow.md 2.4 `[추측]` "첫 스레드인가를 보고 고른다" 가 코드로 맞았다. 값은 PEB·TEB 칸이 아니라 ntdll 전역이다.
- **첫 스레드는 `LdrpInitializeProcess` 가 끝난 뒤 `LdrpInitializationComplete` 로 상태를 2 로 올리고 신호를 켠다. 그 사이 늦게 온 스레드는 `LdrpWaitForInitializationComplete` 에서 기다린다.** [정적] (`LoaderWorker` 비트가 켜진 스레드는 안 기다림.)
- **`LdrpInitializeInternal` 은 `_LdrpInitialize` 보다 먼저 핫패치 준비를 한다** (`NtCreateEvent` → `LdrpInitializeHotPatching` → (켜졌으면 `LdrpLoadPatchedNtdll`) → `LdrpInitializationComplete`). [정적] 같은 0/1/2 깃발 모양 (`LdrpHotPatchInitialized`).
- **커널이 넘긴 `RDX` 는 네 함수를 그대로 지나 `LdrpInitializeProcess` 의 둘째 인자가 된다** (핫패치가 켜졌으면 `LdrpLoadPatchedNtdll` 도 받음). [정적] run16 이 Q8 로 미룬 질문의 답.
- **디버거가 붙어서 달라지는 길** (CLAUDE.md): `_LdrpInitialize` 안의 `LdrpDoDebuggerBreak` (ret `+0x354`) 는 **복제 프로세스 첫 스레드 길 + `PEB.BeingDebugged`** 일 때만 탄다. [정적] 보통 새 프로세스(test.exe) 길에선 닿지 않는다. `LdrpInitializeProcess +0x17d8`·`+0x1b3b` 에도 같은 호출이 있다 (본문은 Q9).

## 정적과 동적이 다른 곳

- **함수 이름 표시**: run7 `[관찰]` 콜스택은 0x8bf8c 를 `LdrpInitialize` 로 보여 줬다. PDB 공개 이름은 `_LdrpInitialize` 하나뿐이다 [정적].
  - 오프셋(`+0x16c`, `+0xa7`)으로 주소는 확정됐으니 **같은 함수를 다른 이름으로 본 것**이다. 고르지 않고 둘 다 적는다.
  - 왜 밑줄이 빠졌나 `[추측]`: 디버거 심볼 엔진이 공개 심볼 맨 앞 `_` 를 떼어 보여 준다. 정황: run6 에서 `LdrpInitialize` 를 찾을 때 `_LdrpInitialize` 도 "같은 이름으로 검색됨". 반대 정황: run6 표엔 `ntdll!_LdrpInitialize` 로 **밑줄이 붙어** 적혀 있고, run8 의 `dll1!_DllMainCRTStartup` 도 `rcx` 이름 풀이에서 밑줄이 그대로 보였다 (sample 자체 PDB). 그래서 명령(`x` / `k`)이나 PDB 마다 다를 수 있다 `[추측]`. → 동적 Q43 (`ln ntdll+0x8c0f8`, `k`).

## 정정

- **flow.md 2.4·그림 2 의 `ntdll!LdrpInitialize` 노드** (`LdrpInitializeInternal +0x5a` 가 부르는 쪽) 는 `_LdrpInitialize`(0x8bf8c) 로 이름을 바꾸고, 진짜 `LdrpInitialize`(0xe1448) 를 `LdrInitializeThunk +0xe` 바로 다음 (꼬리 점프 상자) 로 넣었다. 01_sprint 기록은 WinDbg 가 보여 준 이름 그대로라 틀린 게 아니다 → 이전 문서는 안 고친다.
- **run16 참조 훑기 오프셋**: `NtCreateThreadEx +0x1c1` → **`+0x1bf`**, `PspWow64InitThread +0x73` → **`+0x71`** (명령 시작 위치. O2 refs 시험 때 `objdump` 로 확인, plan.md 결정 기록). flow.md 2.1 `PspCreateUserContext` 절·3장 머리, `run16-thunk-context-해설.md` 2.11 의 세 줄만 고쳤다. run16 본문(`run16-thunk-context.md`)엔 이 오프셋이 없다.
- **결과서 79줄의 JZ 자리는 `+0x46`** 이다. `_workspace/sast/12_ldr-init-entry_result.md` 79줄 "`+0x43 JZ +0x257`" 은 `_LdrpInitialize.asm` 에서 `+0x43 CMP EAX,R15D` → `+0x46 JZ 0x18008c1e3`(= `+0x257`) 이다. 결과서는 그대로 두고 이 문서 표만 고쳤다.

## 아직 확인 필요

- 동적 → 대기열 **Q43** (주소로 멈춤 — `LdrpInitialize` 이름이 두 함수에 겹쳐 보이니 이름 대신 `ntdll+rva`, 조건은 test.exe 만):
  1. `ln ntdll+0x8c0f8`, `x ntdll!*LdrpInitialize` — 0x8bf8c 가 무슨 이름으로 보이나 (밑줄 `[추측]` 확인).
  2. `ntdll+0xe1448` 입구와 `ntdll+0xe1484` (`+0x3c` JMP 직전) 에서 `rcx`·`rdx` 가 같은지, `rdx` vs `lm m ntdll`. `db ntdll+0x1d23ce L1` (`SecurityCookieInitialized`), `db ntdll+0x1d23d0 L1` (`SpecialMachineFramesInitialized`) — 첫 스레드 0, Worker 1 인지.
  3. `LdrpInitializeInternal` 입구 `dw @$teb+0x17ee L1` (`0x4000`·`0x2000` 비트), `dd ntdll+0x1d2928 L1` (`LdrpHotPatchInitialized`). `LdrpInitializeHotPatching +0x45` 에서 `r eax`, `+0x98` 에서 `db ntdll+0x1d26f0 L1` (`LdrpIsHotPatchingEnabled`) — `LdrpLoadPatchedNtdll` 이 불리나.
  4. `ntdll+0x8bfcf` (`_LdrpInitialize +0x43`) 에서 `r eax` — 첫 스레드 0, Worker 2 인지. `ntdll+0x8c0f8` (`+0x16c`) 도착 때 `eax`.
  5. `+0x9a`·`+0x354` 가 안 타는지 (`dd ntdll+0x1d2868 L1` `UseWOW64` = 0).
  6. `ntdll+0xe142e` (`LdrInitializeThunk +0xe`) 도착이 `LdrpInitializeProcess` 가 다 끝난 뒤인지 (`k`).
- 정적 → **Q9** (`LdrpInitializeProcess` 본문)로 넘길 연결점: `LdrInitializePerThreadRng` `+0x153`, `UseWOW64` 쓰기 `+0x5f2`, `LdrpIsHotPatchingEnabled` 쓰기 `+0x587`, `LdrpNtdllHotPatchContext` 읽기 `+0x1233`·`+0x18ef`, `LdrpDoDebuggerBreak` `+0x17d8`·`+0x1b3b`, `LdrInitState` 쓰기 `+0x17dd`·`+0x1a4c`·`+0x1b1e`, `g_LdrpWow64LdrpInitialize` 읽기 `+0x17ed`. 그리고 `RDX`(ntdll 칸) 를 본문에서 어떻게 쓰나.
- 이번에 안 정한 것 (`[공백]`/`[추측]`): `PEB.ProcessParameters.Flags` 맨 윗 비트·`0x2000000` 비트 이름, `0xc00000bb`·`0xc0000002` NTSTATUS 이름, 커널 디버거만 붙었을 때 `BeingDebugged` 가 켜지나 `[추측: 안 켜짐]`, `LdrInitializeMrdata` 본문, `g_LdrpWow64LdrpInitialize` 가 가리키는 함수.

## 디버거 메모 / 도구 메모

- **이름 겹침 주의**: WinDbg 에서 `LdrpInitialize` 라는 이름이 0xe1448 과 0x8bf8c(`_LdrpInitialize`) 두 함수에 걸쳐 보인다 (run6 `x` 에서 둘 다 검색됨, run7 콜스택은 0x8bf8c 를 `LdrpInitialize` 로 표시). `bp ntdll!LdrpInitialize` 처럼 이름으로 걸면 어느 쪽에 걸리는지 모른다 `[추측]` → **`ntdll+rva` 로 건다.** (windbg-kd-runbook pitfalls 에 넣자고 보고.)
- 도구 제안 (결과서): `_LdrpInitialize +0x9a CALL RAX` 처럼 CFG 검사 없는 평범한 `CALL reg` 도, 바로 앞 `MOV RAX,[전역]` 을 거슬러 찾아 `indirect:g_LdrpWow64LdrpInitialize` 로 찍을 수 있다 (`ExportFunctions.java`, 급하지 않음).
