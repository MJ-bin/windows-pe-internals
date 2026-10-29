# 2026-10-30 run51 — 프로세스 객체 뒤 구간 (Q78c2b): `_EPROCESS` 남은 모름 30 → **여전히 모름 1** (`SharedCommitCharge`). 뒤 구간 29칸은 nt 직접 12 · test.exe 가 부른 syscall 안 5 · 다른 프로세스·드라이버가 부름 12. `OwnerProcessId` 둘째 쓰기 = **`NtSetInformationProcess` 클래스 `0x31` `+0x1ecd`**, 부르는 쪽 **`KERNELBASE!ConsoleCommitState +0xe9`** (같은 함수가 매개변수 `ConsoleHandle` 도 씀) (정적)

## 요약 표 (30칸 + `OwnerProcessId` 둘째 쓰기 → ?)

- 결과서 맨 앞 표 그대로 (구간별). 열 뜻:
  - **nt 직접** = 커널이 자기 길 (프로세스·스레드 넣기, 첫 스레드 시작) 에서 쓴다. syscall 아님
  - **syscall 안 (test.exe 가 부름)** = test.exe 자신의 ntdll·KERNELBASE 가 부른 시스템 콜 안에서 커널이 쓴다
  - **다른 프로세스·드라이버가 부름** = csrss 나 드라이버가 부른 시스템 콜 (또는 nt 공개 함수) 안에서 커널이 쓴다
  - **여전히 모름** = 쓰는 줄 후보만

| 구간 | 칸 | nt 직접 | syscall 안 (test.exe 가 부름) | 다른 프로세스·드라이버가 부름 | 여전히 모름 |
|---|---|---|---|---|---|
| `K1 이전` | 1 | — | — | — | **1** (`SharedCommitCharge`, 후보 좁힘) |
| `K2~K3` | 3 | **3** | — | — | — |
| `K3~P0` | 11 | — | — | **11** (csrss 2 · `mssecflt.sys` 6 · bam 3 `[추측]`) | — |
| `P0~P1` | 9 | **9** | — | — | — |
| `P1~P1a` | 5 | — | **5** | — | — |
| `P1a~P1b` | 1 | — | — | **1** (win32k 콜아웃) | — |
| **합 (30)** | **30** | **12** | **5** | **12** | **1** |
| `OwnerProcessId` 둘째 쓰기 | +1 | — | **1** (KERNELBASE 가 부른 `NtSetInformationProcess`) | — | — |

- **쓰는 줄은 29칸 모두 `.asm` 에서 확인** (결과서 + 이 문서를 쓰며 flow-writer 가 다시 봄, 아래 "환경").
- "다른 프로세스·드라이버가 부름" 중 **부르는 쪽 이름이 `[정적]` 인 건 csrss 2칸뿐**. `mssecflt.sys`·`bam.sys`·win32k 는 호출 줄은 있지만, 이번 test.exe 실행에서 정말 그쪽이 불렀는지는 `[추측]` (동적 F2·F3·F9).

짐 지도 (`baggage.md` 2장) 로 옮길 때 — 기존 규칙 그대로:

- 쓰는 줄이 nt 안이면 **nt 로 센다** ("syscall 로 커널이 채우는 칸은 nt 로 센다", run47 의 `nt (부르는 쪽 다른 드라이버)`). 부르는 쪽 (csrss·드라이버·KERNELBASE·win32k) 은 경로 칸에만 적는다. 그래서 29칸 모두 "nt 가 씀".
- 놓는 법은 이번부터 둘로 가른다: **`syscall (test.exe 가 부름)`** / **`syscall (다른 프로세스·드라이버가 부름)`**. `BamPpmPolicy`·`Win32Process` 는 syscall 이 아니라 nt 공개 함수·함수표라 `직접 (nt 함수, 부르는 쪽 …)`.
- 매개변수 `ConsoleHandle` 둘째 쓰기는 **유저 칸이고 쓰는 줄이 KERNELBASE 안**이라 "다른 DLL" 에 센다 (3.2 새 줄).
- **정정 둘 (run50 의 "아무도 안 씀" 28 에서 빠짐)**: `ExceptionPortValue.ExceptionPortState` (비트 0:3) 와 `Flags3.EnableProcessImpersonationLogging` (비트 31) 은 이번 쓰는 줄 (`NtSetInformationProcess` 클래스 8 의 `lock cmpxchg [+0x240]` 은 8바이트 통째, 클래스 `0x60` 은 비트 31 을 지웠다 다시 켬) 이 같이 쓴다 → "nt 가 씀". 아래 "정정".

짐 지도 2장 숫자 변화 (`_EPROCESS`):

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_EPROCESS` (263칸) | 152 → **183** | 28 → **26** | 53 | 30 → **1** | 146 + 6 → **178 + 5** |

- 새 아는 칸 31 = 뒤 구간 29 + 안 씀에서 옮긴 2. 모두 정확 (쓰는 줄 `[정적]`). 그리고 옛 짐작 `ThreadWasActive` 가 정확으로 (`PspInsertThread +0x67f`) → 짐작 6 → 5 (`ThreadListHead`·`Job`·`JobLinks`·`CommitChargeJob`·`Flags.ProcessDelete`).
- 매개변수 (`_RTL_USER_PROCESS_PARAMETERS`): 다른 DLL 0 → **1** (`ConsoleHandle`, KERNELBASE). 아는 칸 수는 그대로 (이미 아는 칸).
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 476 → **507** (정확 417 → **449** · 짐작 59 → **58**) + 아무도 안 씀 79 → **77** + 범위 밖 길에서만 씀 **53** + 모름 848 → **819**.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 요청서·결과서 날짜.
- 03_sprint 아홉째 바퀴 (짐 지도 바퀴, plan **Q78c2b** — Q78c2 의 둘째 조각).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음). ntdll·kernelbase·csrsrv 는 부르는 쪽 이름·정보 클래스만 보느라 한 단계씩 열었다. 이미 analyze 된 프로젝트에서 뽑기만.
- 버전 대조 (결과서): `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines`, ntdll `LdrpInitializeProcess` ret `+0x1bbb` → `LdrpInitializeGraphRecurse`. 사진 값 대조: K1 `OwnerProcessId` `0x19a8` 은 `PspAllocateProcess +0x710` 뒤 값 (`|2` 없음), `0x729` 아래 2비트 = 1 은 클래스 `0x31` 이 요구하는 모양.
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer):
  - `_calls.tsv` 로 call/ret: `PspUserThreadStartup +0x37/+0x3c → PspDisablePrimaryTokenExchange`, `+0x9c/+0xa1 → PspNotifyThreadCreation`, `PspNotifyThreadCreation +0x1c/+0x21 → DbgkCreateThread`, `+0xda/+0xdf → PfProcessCreateNotification`, `PfProcessCreateNotification +0x70 → PfCalculateProcessHash`, `+0x3e → PfSnBeginAppLaunch`, `PfSnBeginAppLaunch +0x254 → PfSnBeginScenario`, `EtwpAddNotificationEvent +0x54 → EtwpAddDataSource`, `NtSetWnfProcessNotificationEvent +0xa8 → ExpWnfCreateProcessContext`, `ObpReferenceCurrentDeviceMap +0x286 → ObpSetCurrentProcessDeviceMap`, `PspInsertProcess +0x25e → PspValidateJobAffinityState`, csrsrv `CsrpCreateProcess +0x34d → CsrSetExceptionPort`, `CsrSetExceptionPort +0x4f/+0x7b → NtSetInformationProcess`, kernelbase `_KernelBaseBaseDllInitialize +0x521 → ConsoleInitialize`, `ConsoleCommitState +0x8a → NtDeviceIoControlFile`·`+0xe9/+0xf0 → NtSetInformationProcess`, ntdll `RtlpGetModifiedProcessCookie +0x22 → NtQueryInformationProcess`, `EtwpRegisterTpNotificationOnce +0x8c → NtTraceControl`, `RtlpWnfRegisterTpNotification +0x77 → NtSetWnfProcessNotificationEvent`, `LdrpInitializeProcess +0x9ed`·`+0xbe2`·`+0xc06`·`+0xc23`·`+0x119d` 호출.
  - `.asm` 쓰는 줄: `PspInsertProcess +0x202`, `PspInsertThread +0x678`·`+0x67f`·`+0x77b`, `PspDisablePrimaryTokenExchange` 전체 (`+0x16`·`+0x22`·`+0x36`·`+0xbb`·`+0xc4`·`+0x111`), `DbgkCreateThread +0x85`, `PspNotifyThreadCreation +0xc1`, `PfCalculateProcessHash +0x137`, `PfSnAddProcessTrace +0x42`, `NtSetInformationProcess +0x19f`·`+0x203`·`+0x20f`·`+0x23d`·`+0x1128`·`+0x1a32`·`+0x1eb0`·`+0x1ecd`, `PspSetProcessPpmPolicy +0xa5`, `NtQueryInformationProcess +0x1b8b`·`+0x2b42`, `ObpSetCurrentProcessDeviceMap +0xf8`, `EtwpAddDataSource +0x49`, `RtlInsertDynamicUserFunctionTable +0x20c`, `ExpWnfCreateProcessContext +0x104`, `PsSetProcessWin32Process +0x86`, kernelbase `ConsoleCommitState +0x3c`·`+0x40`·`+0xd8`.
  - **`PspDisablePrimaryTokenExchange +0xa4` 는 없는 줄이다** — 명령 시작은 `+0xa2` · `+0xad` 이고, `SecurityFreezeComplete` (비트 22, `0x16`) 를 켜는 줄은 **`+0xc4`** (주소 `140910fa4`). 결과서 1번 표의 "`+0xa4`" 는 주소 끝자리를 오프셋으로 읽은 것으로 보인다. 결과서 7번 표의 "`+0xa4 / +0xc4`" 중 `+0xc4` 만 쓴다 (아래 "정정").
  - `.asm` 이 없는 곳 (결과서가 `objdump -d` / PE 직접 읽기로만 봄): `PfSnActivateTrace +0x47`·`PfSnBeginTrace +0x227`·`PfSnBeginScenario +0x1b7` (프리페치 흔적 사슬 뒤 셋), `MiInsertPageInList +0x1fc`·`MiInsertPagesInList +0x64a`, `MiInsertSharedCommitNode` 호출자 줄·`MiMapViewOfImageSection +0x4e9`, `PsConvertToGuiThread +0x1e0 / +0x238`, `NtTraceControl +0x69f ~ +0x775`, 드라이버 (`mssecflt.sys` rva `0xbef6`, `bam.sys` rva `0x1621b`), win32k 쪽 (`win32kbase!AllocateW32ProcessImpl +0xe0` 등, 공개 PDB 이름), KERNELBASE `ConsoleInitialize +0x1ca`·`AttachConsole +0x89` 등. 이 문서에선 결과서의 함수+오프셋 그대로 쓰고 "`.asm` 없음" 이라고 붙였다.

## 목적

- 짐 지도 Q78 의 다섯째 조각 (Q78c2 의 둘째 조각). `_EPROCESS` (baggage 3.11) 남은 모름 30 을 닫는다 — 만들기 길 **밖**에서 쓰이는 칸.
  - 칸마다 쓰는 줄 + **누가 부르는 syscall / 함수 안**인지 (직접 / test.exe 의 syscall / 다른 프로세스·드라이버의 syscall), 정보 클래스까지.
  - 3.11 머리 "Q78c2b 에서 볼 것" 메모를 확인해서 옮기기.
  - `OwnerProcessId` 둘째 쓰기 (`P1a~P1b` `0x19a8` → `0x729`) 와 옛 Q65 남은 둘 (`NtSetInformationProcess +0x1ecd`, `ObCloseHandleTableEntry +0x7a9` 인자).
- 범위 밖: 이미 닫힌 칸, 사용자 모드 쪽 본문 (KERNELBASE·csrss·win32k·드라이버 — 이름만), 종료 길.

## 방법

- 정적: `_workspace/sast/45_eprocess-later.md` — 새로 depth 0 **27개**:
  - ntoskrnl 21: `NtSetInformationProcess` (큰 함수, 갈래만)·`PsSetProcessWin32Process`·`ExpWnfCreateProcessContext` (요청서 표) + 칸을 실제로 쓰는 줄이 있어 더한 18개 (`PspNotifyThreadCreation`·`DbgkCreateThread`·`DbgkCreateMinimalProcess`·`PsCallImageNotifyRoutines`·`PfProcessCreateNotification`·`PfCalculateProcessHash`·`PfSnActivateTrace`·`PfSnAddProcessTrace`·`PfSnBeginAppLaunch`·`ObpSetCurrentProcessDeviceMap`·`ObpReferenceCurrentDeviceMap`·`EtwpAddDataSource`·`EtwpAddNotificationEvent`·`EtwpQueueNotification`·`RtlInsertDynamicUserFunctionTable`·`NtSetWnfProcessNotificationEvent`·`ExpWnfResolveScopeInstance`·`EtwExitProcess`).
  - ntdll 4 (`EtwpRegisterTpNotificationOnce`·`LdrpEnableUMGLTracingStateSync`·`RtlpInitializeWnf`·`RtlpWnfRegisterTpNotification`), kernelbase 1 (`ConsoleCommitState`), csrsrv 1 (`CsrSetExceptionPort`).
  - 이미 `.c` 가 있던 14개는 읽기만 (`PspInsertProcess`·`PspInsertThread`·`PspUserThreadStartup`·`PspDisablePrimaryTokenExchange`·`PspSetProcessPpmPolicy`·`PspCallProcessNotifyRoutines`·`NtQueryInformationProcess`·`ObCloseHandleTableEntry`·`PspProcessClose`·`PspAllocateProcess`·`ObpLookupObjectName`·`ObInitProcess`·`ExDupHandleTable`·`MiMapProcessExecutable`).
  - ntoskrnl `_calls.tsv` 3,744 → 4,281줄 (줄이 안 사라짐).
  - 칸 오프셋으로 쓰는 줄을 커널 전체에서 훑기 + 호출자 찾기 + 드라이버·사용자 모드 import 호출 자리 찾기: 결과서가 `objdump -d` + 임시 스크립트 (스크래치, 레포 밖) 로. 오탐 가능.
  - 정보 클래스 **번호**는 코드의 점프 표·비교 상수라 `[정적]`. **이름은 `[추측]`** — ntoskrnl PDB 에 `PROCESSINFOCLASS` 열거형이 없다 (결과서가 `llvm-pdbutil dump -types` 로 확인).
- 결과서: `_workspace/sast/45_eprocess-later_result.md` (**분석성공**)
- 문서: 이 run 문서, `baggage.md` (기준, 쓰는 말, 1장, 2장·2.1, 3장 읽는 법·3.2 새 줄·3.11 다시 씀, 5장, 7.1·7.2·7.4·7.5·7.6·7.8, 새 7.14, 8장), `flow.md` (기준·2.1 `PspInsertProcess`·`PspInsertThread`·2.2 csrss 절·2.3 트리와 `PspUserThreadStartup` 절·2.4 새 절·2.5 KERNELBASE 시작 함수·4장·5장). 그림 (mermaid) 은 안 바꿨다 (아래 "그림"). 이전 문서는 `runs/02_sprint/run33-first-resume.md`·`run34-parent-leftovers.md` 맨 위 정정 한 줄씩.

## 결과

비유 (영업 신고서, run50 이어서):

- run50 은 본사가 신고서를 **처음 쓸 때**였다. 이번엔 신고서가 나간 **뒤에** 칸이 채워지는 곳이다.
- 채우는 사람이 넷이다:
  - **본사 직원 (nt 직접)** — 상가 명부에 올렸다는 도장 (`ProcessInserted`), 첫 출근 날 찍는 도장 (`CreateReported`·`ImageNotifyDone`·`LaunchPrefetched`·토큰 얼림).
  - **가게 주인 자신 (test.exe 의 syscall)** — 처음 필요할 때 "열쇠 하나 주세요" 하면 본사가 그 자리에서 만들어 준다 (쿠키·장치 맵·ETW·함수표·WNF). **게으른 만들기**.
  - **주민센터 (csrss)** — 전입 신고를 받으면서 "문제 생기면 우리한테 연락" 하는 번호 (`ExceptionPort`) 를 적는다.
  - **경비 업체 (드라이버)** — 보안 기록 스위치 (`mssecflt.sys`), 깨우기 채널·전력 정책 (`bam.sys`), 창 그리기 장부 (win32k). 이 사람들이 정말 이번에 왔는지는 아직 짐작.

### 정적 — 호출 지점 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspInsertProcess` | `+0x1de` | `+0x1e3` | `nt!ObInsertObjectEx` | [정적] |
| `nt!PspInsertProcess` | `+0x25e` | `+0x263` | `nt!PspValidateJobAffinityState` (작업 있을 때만) | [정적] |
| `nt!PspUserThreadStartup` | `+0x37` | `+0x3c` | `nt!PspDisablePrimaryTokenExchange` | [정적] |
| `nt!PspUserThreadStartup` | `+0x9c` | `+0xa1` | `nt!PspNotifyThreadCreation` | [정적] |
| `nt!PspNotifyThreadCreation` | `+0x1c` | `+0x21` | `nt!DbgkCreateThread` | [정적] |
| `nt!PspNotifyThreadCreation` | `+0xda` | `+0xdf` | `nt!PfProcessCreateNotification` | [정적] |
| `nt!PfProcessCreateNotification` | `+0x3e` | `+0x43` | `nt!PfSnBeginAppLaunch` | [정적] |
| `nt!PfProcessCreateNotification` | `+0x70` | `+0x75` | `nt!PfCalculateProcessHash` | [정적] |
| `nt!PfSnBeginAppLaunch` | `+0x254` | `+0x259` | `nt!PfSnBeginScenario` | [정적] |
| `nt!ObpReferenceCurrentDeviceMap` | `+0x286` | `+0x28b` | `nt!ObpSetCurrentProcessDeviceMap` | [정적] |
| `nt!EtwpAddNotificationEvent` | `+0x54` | `+0x59` | `nt!EtwpAddDataSource` | [정적] |
| `nt!NtSetWnfProcessNotificationEvent` | `+0xa8` | `+0xad` | `nt!ExpWnfCreateProcessContext` | [정적] |
| `csrsrv!CsrpCreateProcess` | `+0x34d` | `+0x352` | `csrsrv!CsrSetExceptionPort` | [정적] |
| `csrsrv!CsrSetExceptionPort` | `+0x4f` / `+0x7b` | `+0x56` / `+0x82` | `NtSetInformationProcess` (클래스 8, 길이 8 / 16) | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x521` | `+0x526` | `KERNELBASE!ConsoleInitialize` | [정적] |
| `KERNELBASE!ConsoleCommitState` | `+0x8a` | `+0x91` | `NtDeviceIoControlFile` (`0x500023`, 출력 8바이트) | [정적] |
| `KERNELBASE!ConsoleCommitState` | `+0xe9` | `+0xf0` | `NtSetInformationProcess` (`-1`, 클래스 `0x31`, 8바이트) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x9d` | `+0xa2` | `ntdll!RtlpGetModifiedProcessCookie` | [정적] |
| `ntdll!RtlpGetModifiedProcessCookie` | `+0x22` | `+0x27` | `NtQueryInformationProcess` (클래스 `0x24`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xbe2` | `+0xbe7` | `ntdll!LdrpEnableUMGLTracingStateSync` | [정적] |
| `ntdll!RtlpWnfRegisterTpNotification` | `+0x77` | `+0x7c` | `NtSetWnfProcessNotificationEvent` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc06` / `+0xc23` | `+0xc0b` / `+0xc28` | `ntdll!EtwEventRegister` | [정적] |
| `ntdll!EtwpRegisterTpNotificationOnce` | `+0x8c` | `+0x91` | `NtTraceControl` (함수 코드 `0x1b`, 길이 4) | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x19e` | `+0x1a3` | `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable` | [정적] |

- `ConsoleCommitState` 의 호출자 `ConsoleInitialize +0x1ca`, 다른 호출자 `AttachConsole +0x89`·`AllocConsoleWithOptions +0xa6`·`FreeConsole +0x4a` 는 결과서 `objdump` (`.asm` 없음).
- `RtlpInsertOrRemoveScpCfgFunctionTable +0xc4 → RtlAddGrowableFunctionTable (호출 +0x14f, EDX = 0x35) → NtSetInformationProcess`, `EtwEventRegister +0x1e → EtwNotificationRegister +0x15c → EtwpRegisterProvider +0xaf → EtwpRegisterTpNotificationOnce`, `LdrpEnableUMGLTracingStateSync +0x7a → RtlpSubscribeWnfStateChangeNotificationInternal +0x189 → RtlpInitializeWnf +0x98 → RtlpWnfRegisterTpNotification` 은 결과서 4번 표 (`EtwNotificationRegister +0x15c`·`LdrpEnableUMGLTracingStateSync +0x7a`·`RtlpInitializeWnf +0x98` 은 `_calls.tsv` 로도 맞음).

### 호출 관계 (대표 지점)

```
① 부모 cmd 스레드, NtCreateUserProcess 안
 ├─ +0xfc2 PspInsertProcess
 │    └─ +0x1e3 ObInsertObjectEx 성공 뒤 +0x202  Flags.ProcessInserted (작업 있으면 +0x263 PspValidateJobAffinityState 가 같은 비트)
 └─ +0x101a PspInsertThread
      ├─ +0x65a ActiveThreads +1 → +0x678 ActiveThreadsHighWatermark → +0x67f Flags4.ThreadWasActive (bts)
      └─ +0x77b Flags2.PrimaryTokenFrozen  ← ActiveThreads == 2 일 때만 (둘째 스레드) — test.exe 첫 스레드엔 안 탐

K3~P0 (부모 쪽 마무리 ~ 자식 첫 스레드 시작 사이, 다른 프로세스·드라이버)
 ├─ csrss  CsrpCreateProcess +0x34d → CsrSetExceptionPort → NtSetInformationProcess(자식, 클래스 8) +0x1128  ExceptionPort*   [정적]
 ├─ mssecflt.sys rva 0xbef6 → ZwSetInformationProcess(클래스 0x60) +0x19f/+0x203 Flags3 · +0x20f/+0x23d Flags2 로깅 비트   [정적: 호출 줄 / 추측: 이번에 불림]
 ├─ bam.sys rva 0x1621b → ZwQueryInformationProcess(클래스 0x51) +0xd0d → PspAllocateAndQueryProcessNotificationChannel +0x11d  WakeChannel·WakeInfo   [같음]
 └─ (함수표 PspBamHostInterface 경유) PspSetProcessPpmPolicy +0xa5  BamPpmPolicy   [추측: 부르는 쪽]

① test.exe 첫 스레드, PspUserThreadStartup 안 (P0~P1)
 ├─ (1) +0x3c PspDisablePrimaryTokenExchange
 │        +0x16 이미 켜졌으면 건너뜀 / +0x22 PrimaryTokenFrozen
 │        +0x36 SecurityFreezeComplete 이미 켜졌으면 끝
 │        +0xbb (또는 +0x10c KeSynchronizeAddressPolicy 뒤 +0x111) AddressPolicyFrozen → +0xc4 SecurityFreezeComplete
 └─ (4) +0xa1 PspNotifyThreadCreation
          ├─ +0x21 DbgkCreateThread   +0x85 Flags |= 0x400001 (CreateReported·ImageNotifyDone)
          │        옛 ImageNotifyDone = 0 이면 PsCallImageNotifyRoutines (exe, 시스템 DLL) · 디버그 포트 있으면 메시지
          └─ +0xc1 LaunchPrefetched (처음일 때만) → +0xdf PfProcessCreateNotification
                   ├─ +0x75 PfCalculateProcessHash +0x137  ImagePathHash        (프리페치 설정 조건)
                   └─ +0x43 PfSnBeginAppLaunch +0x259 PfSnBeginScenario … PfSnAddProcessTrace +0x42  PrefetchTrace  (시나리오 허락될 때)

② test.exe 첫 스레드, ntdll·KERNELBASE 가 부른 syscall 안 (P1~P1b)
 LdrpInitializeProcess
 ├─ +0x9f2 RtlInitializeHeapManager → +0xa2 RtlpGetModifiedProcessCookie → +0x27 NtQueryInformationProcess(0x24)
 │        → nt +0x2b42 Cookie (0 이면 한 번만)
 ├─ +0xbe7 LdrpEnableUMGLTracingStateSync → … → RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent
 │        → nt +0xad ExpWnfCreateProcessContext +0x104  WnfContext
 ├─ +0xc0b / +0xc28 EtwEventRegister → … → EtwpRegisterTpNotificationOnce +0x91 NtTraceControl(0x1b)
 │        → nt +0x775 EtwpAddNotificationEvent +0x59 EtwpAddDataSource +0x49  EtwDataSource
 ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry → +0x124 LdrpProcessMappedModule → +0x1a3 RtlpInsertOrRemoveScpCfgFunctionTable
 │        → RtlAddGrowableFunctionTable → NtSetInformationProcess(0x35) → nt RtlInsertDynamicUserFunctionTable +0x20c  InvertedFunctionTable
 │        (ntdll 안 CFG 갈래에 따라 syscall 까지 가는지 [공백])
 ├─ ?  `\??\` 이름 찾는 첫 syscall → nt ObpLookupObjectName +0x145e → ObpReferenceCurrentDeviceMap +0x28b → ObpSetCurrentProcessDeviceMap +0xf8  DeviceMap  [공백: ntdll 호출자]
 └─ (kernel32 준비 안) KERNELBASE 시작 함수 _KernelBaseBaseDllInitialize +0x526 ConsoleInitialize → ConsoleCommitState
          +0x40 매개변수 ConsoleHandle (4 → 0x60)
          +0x91 NtDeviceIoControlFile(0x500023) → +0xd8 값 | 1 → +0xf0 NtSetInformationProcess(-1, 0x31) → nt +0x1ecd  OwnerProcessId (0x19a8 → 0x729)

P1a~P1b  win32k 콜아웃 → PsSetProcessWin32Process +0x86  Win32Process   (트리거 [공백], 후보 PsConvertToGuiThread +0x1e0)
```

### 정보 클래스 번호 (`[정적]`) · 이름 (`[추측]`)

| 번호 | 이름 `[추측]` | 코드가 하는 일 `[정적]` | 칸 |
|---|---|---|---|
| `0x8` (설정) | `ProcessExceptionPort` | LPC 포트 핸들 참조 + `SeTcbPrivilege` + 접근 `0x800`, `lock cmpxchg [+0x240]` | `ExceptionPort*` |
| `0x24` (조회) | `ProcessCookie` | 0 이면 난수를 만들어 한 번만 넣음 (핸들 `-1`, 길이 4) | `Cookie` |
| `0x31` (설정·조회) | `ProcessConsoleHostProcess` | 현재 프로세스만, 값 아래 2비트 = `01` 이어야, 권한 검사 없음. 조회도 같은 칸을 돌려줌 | `OwnerProcessId` |
| `0x35` (설정) | `ProcessDynamicFunctionTableInformation` | 핸들 `-1`, 16바이트, `RtlInsertDynamicUserFunctionTable` | `InvertedFunctionTable` |
| `0x51` (조회) | `ProcessWakeInformation` | 출력 `0x30` = `WakeInfo` 크기, 없으면 채널 만듦 | `WakeChannel`·`WakeInfo` |
| `0x57` (설정) | `ProcessEnableReadWriteVmLogging` | 1바이트, 비트 0·1 → `Flags2` 24·25 | `Flags2` 로깅 |
| `0x60` (설정) | `ProcessEnableLogging` | 4바이트, 비트 0~6 → `Flags2` 24·25 + `Flags3` 19·20·29·28·31 | `Flags2`·`Flags3` 로깅 |

- run46 이 `[추측]` 으로 적은 `ProcessCookie` 는 **이름만 여전히 추측, 번호 `0x24` 와 동작은 `[정적]` 확정**.

### 뒤 구간 칸별 — 쓰는 줄 (짐 지도 3.11 줄과 같음)

| 구간 | 칸 | 쓰는 줄 `[정적]` | 놓는 법 | 부르는 쪽 |
|---|---|---|---|---|
| `K2~K3` | `Flags.ProcessInserted` (26) | `PspInsertProcess +0x202` (`lock or 0x4000000`; 작업 있으면 `PspValidateJobAffinityState +0x33`) | 직접 | nt |
| `K2~K3` | `ActiveThreadsHighWatermark` | `PspInsertThread +0x678` (스레드 달 때마다) | 직접 | nt |
| `K2~K3` | `Flags4` · `.ThreadWasActive` (0) | `PspInsertThread +0x67f` (`bts`, 판정 `+0x696`) | 직접 | nt |
| `K3~P0` | `ExceptionPortData` · `ExceptionPortValue` (+ `ExceptionPortState`) | `NtSetInformationProcess` 클래스 8 `+0x1128` | syscall (다른 프로세스·드라이버가 부름) | csrss `CsrSetExceptionPort` `[정적]` |
| `K3~P0` | `Flags2.EnableWriteVmLogging` (25) | 클래스 `0x60`/`0x57` `+0x20f` 지움 · `+0x23d` 켬 | 같음 | `mssecflt.sys` `[추측]` |
| `K3~P0` | `Flags3` · 로깅 비트 넷 (19·20·28·29, + 31) | 클래스 `0x60` `+0x19f` 지움 · `+0x203` 켬 | 같음 | `mssecflt.sys` `[추측]` |
| `K3~P0` | `WakeChannel` · `WakeInfo` | 조회 클래스 `0x51` `+0xd0d` → `PspAllocateAndQueryProcessNotificationChannel +0x11d` (게으른) | 같음 | `bam.sys` `[추측]` |
| `K3~P0` | `BamPpmPolicy` | `PspSetProcessPpmPolicy +0xa5` (기능 플래그 둘 켜졌을 때) | 직접 (nt 함수) | `bam.sys` 함수표 `[추측]` |
| `P0~P1` | `Flags2.PrimaryTokenFrozen` (15) | `PspDisablePrimaryTokenExchange +0x22` (첫 스레드). 둘째 줄 `PspInsertThread +0x77b` 는 안 탐 | 직접 | nt |
| `P0~P1` | `Flags3.AddressPolicyFrozen` (14) · `.SecurityFreezeComplete` (22) | 같은 함수 `+0xbb`·`+0x111` (14) · `+0xc4` (22) | 직접 | nt |
| `P0~P1` | `Flags.CreateReported` (0) · `.ImageNotifyDone` (22) | `DbgkCreateThread +0x85` (`OR 0x400001`) | 직접 | nt |
| `P0~P1` | `Flags.LaunchPrefetched` (19) | `PspNotifyThreadCreation +0xc1` | 직접 | nt |
| `P0~P1` | `ImagePathHash` | `PfCalculateProcessHash +0x137` (프리페치 설정 조건) | 직접 (조건부) | nt |
| `P0~P1` | `PrefetchTrace` | `PfSnAddProcessTrace +0x42` (시나리오 허락) | 직접 (조건부) | nt |
| `P0~P1` | `ModifiedPageCount` | `MiInsertPageInList +0x1fc` · `MiInsertPagesInList +0x64a` (런타임 카운터, `.asm` 없음) | 직접 | nt |
| `P1~P1a` | `Cookie` | `NtQueryInformationProcess` 클래스 `0x24` `+0x2b42` | syscall (test.exe 가 부름) | ntdll `RtlpGetModifiedProcessCookie` |
| `P1~P1a` | `DeviceMap` | `ObpSetCurrentProcessDeviceMap +0xf8` (`\??\` 이름 찾기) | 같음 (게으른) | ntdll 쪽 `[공백]` |
| `P1~P1a` | `EtwDataSource` | `NtTraceControl` 함수 코드 `0x1b` → `EtwpAddDataSource +0x49` | 같음 | ntdll `EtwpRegisterTpNotificationOnce` |
| `P1~P1a` | `InvertedFunctionTable` | `NtSetInformationProcess` 클래스 `0x35` → `RtlInsertDynamicUserFunctionTable +0x20c` | 같음 | ntdll `RtlAddGrowableFunctionTable` (탐 여부 `[공백]`) |
| `P1~P1a` | `WnfContext` | `NtSetWnfProcessNotificationEvent` → `ExpWnfCreateProcessContext +0x104` | 같음 | ntdll `RtlpWnfRegisterTpNotification` |
| `P1a~P1b` | `Win32Process` | `PsSetProcessWin32Process +0x86` | 직접 (nt 공개 함수) | win32k (`win32kbase!AllocateW32ProcessImpl +0xe0`, `.asm` 없음) |
| `P1a~P1b` | `OwnerProcessId` (둘째 쓰기) | `NtSetInformationProcess` 클래스 `0x31` `+0x1ecd` | syscall (test.exe 가 부름) | `KERNELBASE!ConsoleCommitState +0xe9` |
| `K1 이전` | `SharedCommitCharge` | 후보 `MiInsertSharedCommitNode +0x33e` — 부르는 쪽 후보에 **`MiMapViewOfImageSection +0x4e9`** 가 새로 (exe·ntdll 붙일 때 `[추측]`) | ? | 모름 (F12) |

- `PspUserThreadStartup` 은 **스레드마다** 같은 길을 돈다. 칸을 처음 켜는 건 첫 스레드뿐 — `ImageNotifyDone`·`LaunchPrefetched`·`PrimaryTokenFrozen`·`SecurityFreezeComplete` 에 "이미 켜졌으면 건너뜀" 검사가 있다. 일꾼 스레드 (P1a~P1b) 땐 건너뛴다.
- 사진 `Enable…Logging 비트 5개` (`K3~P0`) = `EnableWriteVmLogging` + `Flags3` 넷 (입력 비트 1·2·3·4·5) 과 모양이 맞다 `[추측: 값 대조는 Q79]`.

### 옛 Q65 남은 것 (닫힘)

- `NtSetInformationProcess +0x1ecd` = **`OwnerProcessId` 둘째 쓰기**. 클래스 `0x31` (`+0x1a32`), 현재 프로세스만, 값 아래 2비트 = `01` (`+0x1eb0 ~ +0x1eb7`), `MOV [RCX+0x2d8],RDX`. 오케스트레이터가 `.asm` 으로 확인, flow-writer 도 같은 줄을 봄.
- `ObCloseHandleTableEntry +0x7a9` = 개체 형식의 `CloseProcedure` 간접 호출 (앞 `+0x61d ~ +0x62e` 가 `IopCloseFile` 이면 직접 `+0x637`). 인자 `RCX` 핸들 표 주인 프로세스 · `RDX` 개체 · `R8D` 접근 권한 · `R9` 핸들 개수, 다섯째는 안 채움. `PspProcessClose` 이름 `(Process, Object, GrantedAccess, ProcessHandleCount, SystemHandleCount)` 는 `[추측]` (ntddk/WRK `OB_CLOSE_METHOD` 모양). **짐에 영향 없음.**
- `PspAllocateProcess +0x710` 의 `|2` (호출한 쪽 PID 태그) 와 `+0x1ecd` 의 `|1` (콘솔 호스트 태그) 은 **같은 칸의 두 태그**다. 태그가 붙으면 PID (4의 배수) 와 절대 같지 않아 `PspProcessClose +0x22 ~ +0x30` 비교가 안 걸린다.

## 확인된 것

- `[정적]` `_EPROCESS` 뒤 구간 29칸의 쓰는 줄은 모두 nt 안이다 (sast/45 결과서 + 위 `.asm` 줄). 남은 모름은 `SharedCommitCharge` 1칸.
- `[정적]` 첫 스레드 시작 (`PspUserThreadStartup`) 이 9칸 (`P0~P1`) 을 켠다 — `PspDisablePrimaryTokenExchange` (토큰 얼림 3비트) 와 `PspNotifyThreadCreation` (`DbgkCreateThread +0x85` 두 비트, `+0xc1` 프리페치 표시와 프리페치 알림). run15 의 "정상 길 4개 중 1·4 의 본문" 이 닫혔다.
- `[정적]` test.exe 자신이 부른 syscall 안에서 커널이 처음 만드는 칸 다섯 (쿠키·장치 맵·ETW·동적 함수표·WNF) — 모두 "자기 프로세스, 처음 필요할 때".
- `[정적]` `OwnerProcessId` 둘째 쓰기와 매개변수 `ConsoleHandle` 둘째 쓰기는 **같은 함수 `KERNELBASE!ConsoleCommitState` 의 같은 호출**에서 나온다 (`+0x40` / `+0xf0`). KERNELBASE DLL 초기화 (`P1a~P1b`) 안.
- `[정적]` csrss 가 자식에 예외 포트를 단다 (`CsrpCreateProcess +0x34d → CsrSetExceptionPort`). 부모 cmd 의 KERNELBASE 자신이 `K3~P0` 칸을 쓰는 줄은 없다 (`CreateProcessInternalW` 에 `NtSetInformationProcess` 호출 줄이 없음, 결과서).
- `[관찰 간접]` 사진 (run44) 구간이 코드 단계와 모두 맞다 (`K2~K3` = `PspInsertProcess`/`PspInsertThread`, `P0~P1` = `PspUserThreadStartup`, `P1~P1a` = `LdrpInitializeProcess` 앞쪽, `P1a~P1b` = KERNELBASE 시작).

## 정정

- **`OwnerProcessId` 는 "부모 PID" 가 아니다.** 만들 때 값은 **`NtCreateUserProcess` 를 부른 프로세스의 PID** (`PspAllocateProcess +0x6f1 ~ +0x710`: `GS:[0x188] → +0xb8` = 지금 도는 프로세스) 이고, 뒤에 **콘솔 호스트 프로세스 ID `|1`** 로 덮인다 (클래스 `0x31`). cmd 가 부모이자 호출한 쪽이라 test.exe 에선 값이 같다. `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS` 로 부모를 바꿔도 호출한 쪽이 들어간다 (결과서). 고친 곳: baggage 3.11 `+0x2d8` 줄, flow.md 2.1 `PspAllocateProcess` 절, `runs/02_sprint/run33-first-resume.md`·`run34-parent-leftovers.md` 맨 위 한 줄 (그 문서들 본문의 "부모 PID" 는 그대로 둠).
- **`PspInsertThread +0x77b` (`PrimaryTokenFrozen`) 는 둘째 스레드용**이다 (`ActiveThreads` 가 올린 뒤 정확히 2, `+0x69c ~ +0x6a6`). run50 이 "켜는 줄이 둘" 로 적은 것 중 test.exe 첫 스레드 때 타는 건 `PspDisablePrimaryTokenExchange +0x22` 하나.
- **`PspDisablePrimaryTokenExchange +0xa4` → `+0xc4`** — `SecurityFreezeComplete` 를 켜는 줄 (`BTS.LOCK [RSI+0x5fc],0x16`) 은 `+0xc4` 다 (주소 `140910fa4`). `+0xa4` 라는 명령 시작은 없다. run49 (flow.md 2.3, baggage 3.11 메모) 와 결과서 1번 표의 `+0xa4` 를 고침. `+0x111 JC +0xc4` (`0x140910fa4`) 로도 맞다.
- **run50 "아무도 안 씀" 28 중 둘은 쓰는 줄이 있다**: `ExceptionPortValue.ExceptionPortState` (클래스 8 의 8바이트 `cmpxchg` 가 같이 씀, 상태 3비트) · `Flags3.EnableProcessImpersonationLogging` (클래스 `0x60` 이 `+0x19f` 에서 지우고 `+0x203` 에서 입력 비트 6 으로 다시 켬). run50 이 커널 전체 훑기에서 `NtSetInformationProcess` 갈래를 못 본 것 (훑기 한계 — run50 "아직 확인 필요" 의 오탐·빠짐 경고 그대로). 사진은 끝까지 0 이라 "0 을 씀 / 안 켬" 쪽이다. 2장 안 씀 79 → 77.
- **`ThreadWasActive` 짐작 → 정확** (`PspInsertThread +0x67f`, 판정 `+0x696`). run12 의 "오프셋 미상" 을 채움.
- **`Cookie` 복제 길**: `PspAllocateProcess +0x2183` 이 부모 값을 복사 (섹션 없는 복제 길, test.exe 안 탐). run50 "조건부 안 탐" 목록에 이 칸이 빠져 있었다 — 3.11 `Cookie` 줄에 같이 적음.

## 아직 확인 필요

동적 질문 (결과서 F1 ~ F13 번호 그대로). 짐 지도 7.14 에도 적음. 급한 것은 없다 — 안 닫아도 3.11 칸 표는 1칸 빼고 완성.

- **덤프로 (plan Q79 에 출처로)**:
  - **F1** `ExceptionPortData` 값이 csrss 의 `CsrApiPort` 객체인가 — 사진 P0 `+0x240` 값 & `!object`. (실행 중이면 `nt!NtSetInformationProcess+0x1128` rva `0x933ad8`, 대상 `rcx` 이미지 = test.exe, 현재 프로세스 = csrss.exe 인지·`rdx`·`k`; `csrsrv!CsrSetExceptionPort+0x4f/+0x7b`)
  - **F7** `InvertedFunctionTable` 이 정말 클래스 `0x35` 로 왔나 — 사진 `P1~P1a` 값의 풀 내용 (`dq <값> L6`: 개수·지움 표시). 실행 중이면 `nt!RtlInsertDynamicUserFunctionTable+0x20c` (rva `0x451310`, 현재 = test.exe), `k` → `RtlAddGrowableFunctionTable+0x14f`
  - **F10** `OwnerProcessId` `0x729` = conhost PID `0x728 | 1` 인가 — P1b/P2 사진의 프로세스 목록에서 PID `0x728` 이미지 이름. 실행 중이면 `nt!NtSetInformationProcess+0x1ecd` (rva `0x93487d`) `rdx`·`k` (`KERNELBASE!ConsoleCommitState+0xf0`), `ConsoleCommitState+0x91` 뒤 `[rsp+0x70]`
- **plan Q97 (E5 와 같음)**: **F12** `SharedCommitCharge` K1 `0x2b` 를 올린 줄 — `nt!MiInsertSharedCommitNode+0x33e` (rva `0x9097be`, `r13` = 새 EPROCESS) 에서 `r8`·`k` (`MiMapViewOfImageSection+0x4e9` 이 exe·ntdll 어느 쪽인지). 낮음.
- **새 plan Q98 (실행 중 멈춤, 낮음)**:
  - **F2** 로깅 비트를 쓴 주체가 `mssecflt.sys` 인가·입력 값 — `nt!NtSetInformationProcess+0x203` (rva `0x932bb3`) 대상 = test.exe, `k`·`[rsp+0x90]`; 대안 `bp mssecflt+0xbef6`
  - **F3** `WakeChannel` 을 만든 주체 (`bam.sys`?) — `nt!PspAllocateAndQueryProcessNotificationChannel+0x11d` (rva `0x8d368d`) `rbp` = test.exe, `k`; 대안 `bp bam+0x1621b`
  - **F4** `Cookie` 값·호출자 (확인용) — `nt!NtQueryInformationProcess+0x2b42` (rva `0x9bde22`) `r8d`, 현재 = test.exe
  - **F5** `DeviceMap` 을 처음 채운 syscall 과 ntdll 호출자 — `nt!ObpSetCurrentProcessDeviceMap+0xf8` (rva `0x96bad4`) `r15` = test.exe, `k`
  - **F6** `EtwDataSource` (확인용) — `nt!EtwpAddDataSource+0x49` (rva `0x882cf1`), `k`
  - **F8** `WnfContext` (확인용) — `nt!ExpWnfCreateProcessContext+0x104` (rva `0xa50f28`), `k`
  - **F9** `Win32Process` 를 켠 주체와 트리거 — `nt!PsSetProcessWin32Process+0x86` (rva `0xa62d56`) `rbx` = test.exe, 현재 프로세스 (test.exe / csrss)·`k`
  - **F11** 프리페치 세 칸이 이 VM 에서 켜지나 — `nt!PfCalculateProcessHash+0x137` (rva `0x945db7`), `nt!PfSnAddProcessTrace+0x42` (rva `0x402af2`), `nt!PspNotifyThreadCreation+0xc1` (rva `0x9110d9`). 안 걸리면 "줄은 있으나 이 설정에선 안 탐". `ImagePathHash` 값은 사진 값 vs 직접 계산 (대문자 전체 경로, `×0x25`, 시작 `0x4cb2f`) 으로 정적 대조도 가능
  - **F13** `ModifiedPageCount` 가 P0~P1 에 처음 오른 줄 — `ba w4 <EPROCESS>+0x550` 또는 `nt!MiInsertPageInList+0x1fc` (rva `0x32412c`)
- **정적 (다음 바퀴 후보)**:
  - run50 의 "범위 밖 길에서만 씀" 중 **`InvertedFunctionTableLock` (`+0x5d0`)** 은 이번 클래스 `0x35` 길 (`RtlInsertDynamicUserFunctionTable`) 이 잡는 락이다 (결과서 4번 표). **`ThreadListLock` (`+0x5e0`)** 은 `PspInsertThread` 가 스레드 목록에 달 때 잡는 푸시락이다 (run12). 둘 다 초기화 길 안이면 "nt 가 씀 (잡았다 놓음)" 으로 옮겨야 한다. 잡는 줄을 `.asm` 으로 확인하면 옮김 (이번엔 결과서에 줄이 없어서 안 옮김).
  - `DeviceMap` 의 ntdll 쪽 첫 호출자 — `\??\` 경로를 처음 여는 ntdll 호출 (P1 ~ P1a). 정적으론 후보가 너무 많음 → F5.
  - `ModifiedPageCount` 가 어느 페이지 목록 (변경 목록?) 일 때 오르나 — `MiInsertPageInList` 인자 뜻 `[추측]`.
  - 새 짐 후보 (칸 표 없음, 짐 지도 7.6 에 기록만): `_WNF_PROCESS_CONTEXT` (0x88B), ETW 데이터 소스 (0x28B), 동적 함수표 (`0x10 + 24×N`), `_PS_PROCESS_WAKE_INFORMATION` (0x30B, PDB 에 있음), `ExceptionPort` 가 가리키는 csrss `CsrApiPort`.

## 그림

- flow.md 그림 (mermaid) 은 안 바꿨다. 새로 나온 호출 (`PspNotifyThreadCreation → DbgkCreateThread`·`PfProcessCreateNotification`, ntdll·KERNELBASE 의 syscall 길) 은 **이미 그림에 있는 상자 안쪽의 칸 쓰기 자리**라 새 단계·새 caller 가 아니다. 2.3 글 트리와 2.4·2.5 글에 `(run51)` 줄로만 넣었다. csrss·드라이버 쪽은 다른 프로세스·드라이버라 그림 1·2 밖이다 (2.2 글).

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 1 (sast/43·44 에 이어 세 번째)**: `scripts/field_writers.py <바이너리> --off 0x5e8 [--type _EPROCESS]` (오프셋 쓰는 줄 훑기) + `scripts/callers.py <바이너리> <함수>` (누가 이 함수를 부르나). `_calls.tsv` 는 뽑은 함수의 호출만 있어 거꾸로 찾기가 안 된다. 이번 run50 "아무도 안 씀" 정정 둘도 이 훑기의 빠짐이다 — 오프셋 숫자 훑기만으로 "안 씀" 을 정하면 큰 함수 (`NtSetInformationProcess`) 안 갈래를 놓친다.
  - **제안 2**: `pdb_struct.py` 에 열거형 (`LF_ENUM`) 지원 — `PROCESSINFOCLASS` 가 PDB 에 없음을 `llvm-pdbutil` 로 직접 확인해야 했다.
  - **제안 3**: `imports_callers.py <바이너리> <import 이름>` — IAT 호출 자리 + 앞쪽 `edx` (정보 클래스) 를 같이.
- flow-writer 가 본 함정 (windbg-kd-runbook pitfalls 가 아니라 정적 쪽): 결과서 오프셋을 `.asm` 의 **주소 끝자리**와 섞어 읽는 일 (`+0xa4` vs 주소 `…fa4` = `+0xc4`). `.asm` 의 `+0x` 열로만 적자.
- 이번 바퀴엔 디버거를 안 붙였다. 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진). 단 `DbgkCreateThread` 는 디버그 포트가 있으면 메시지를 보내는 길이 있다 — 커널 디버거 (KDNET) 는 프로세스 디버그 포트가 아니라 이 갈래와 상관없다 `[추측]`.
