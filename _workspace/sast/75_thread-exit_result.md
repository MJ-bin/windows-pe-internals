# 75 thread-exit — 정적 분석 결과

- 요청서: `_workspace/sast/75_thread-exit.md` (Q141d, PE_emulator 요청 25 ㄹ — 스레드 끝내기)
- 앞 바퀴: `_workspace/sast/73_thread-wake_result.md` (`KiTryUnwaitThread`·`WaitStatus`·`KiProcessThreadWaitList`·수동 재설정 깨움), `_workspace/sast/74_thread-timer_result.md`, `_workspace/sast/71_thread-switch_result.md` (`KiSwapThread`). 겹치는 건 "sast/73 질문 N" 으로 가리키기만 한다.
- 모듈 / 바이너리: ntoskrnl, pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도: `run_ghidra.sh map ntoskrnl.exe NtTerminateThread` (2,451함수), `… PspExitThread` (3,434함수) 만들었다 → `ms/disassemble/ntoskrnl/_map/`. 지도로 얻은 것:
  - `NtTerminateThread` 깊이 1 은 3개뿐 (`PspTerminateThreadByPointer` `ObpReferenceObjectByHandleWithTag` `ObfDereferenceObjectWithTag`), `PspExitThread` 는 깊이 2 (`_edges` 의 `PspTerminateThreadByPointer +0x4a / ret +0x4f`).
  - `PspExitThread` 깊이 1 은 **49개** — 거의 다 ETW·락·LPC·콜백·bugcheck 라서 depth 1 로 통째로 안 뽑고, 아래처럼 depth 0 + 이름으로 골랐다. `KeTerminateThread` 는 caller 1곳 (`PspExitThread`), `MmDeleteTeb` caller 3곳 (`PspExitThread` + 만들기 실패 되돌림 2곳).
  - **지도에서 안 보이는 이음새 둘** (간접 호출·데이터 참조라서 `_calls.tsv` 에도 없음 → `run_ghidra.sh refs` 로 찾음): `PspThreadDelete` (객체 형식의 삭제 루틴으로 등록됨), `PspReaper` (작업 항목 루틴으로 등록됨). 질문 7 에서.
- 뽑은 함수 (새 **16개**, 전부 depth 0): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/` 에 `.c`/`.asm`.
  - 요청서 대상 7: `NtTerminateThread`(216B) `PspTerminateThreadByPointer`(216B) `PspExitThread`(2329B) `KeTerminateThread`(821B) `PspExitLastThread`(739B) `PspTerminateAllThreads`(654B) `PspThreadDelete`(1314B). (`MmDeleteTeb` `ObWaitForSingleObject` 은 이미 있었다.)
  - **요청서 범위를 넓힌 곳 (이유)**:
    1. `KeRequestTerminationThread`(224B) — 질문 5(a). "남이면 APC" 의 APC 를 거는 함수가 이것이라서 (요청서가 짐작한 `PsExitSpecialApc` 는 아님).
    2. `KiSchedulerApcTerminate`(작음, rva `0x892780`) — 질문 5(a). 그 APC 가 도착했을 때 대상 스레드 안에서 `PspExitThread` 를 부르는 곳. (`KiSchedulerApc`·`KiDeliverApc`·`KiInsertQueueApc`·`KeInitThread` 는 이미 있던 것의 몇 줄만 읽음.)
    3. `PspReaper`(171B) — 질문 3·7. `KeTerminateThread` 가 스레드를 얹는 "청소부 목록" 을 비우는 작업 항목 루틴.
    4. `PspExitProcess`(238B) `PspRundownSingleProcess`(1129B) `NtTerminateProcess`(620B) `PspTerminateProcess`(406B) — 질문 4·5(b) "이름만". **부르는 줄과 callee 이름만** 봤다 (본문 안 읽음).
    5. `ObCreateObjectType`(22B) `ObCreateObjectTypeEx`(1614B) — 질문 6. 스레드 형식의 `DefaultObject` 칸이 어떻게 정해지나 (몇 줄만).
  - depth 1 로 안 뽑은 이유: 위 지도 (`PspExitThread` 깊이 1 = 49개, 거의 줄기 밖).
  - 안 읽은 것 (이름만): ETW (`EtwTraceThread`) · 감사 (`SeAuditProcessExit`) · 작업 객체·silo (`PsAttachSiloToCurrentThread`) · LPC (`LpcRequestPort` `LpcExitProcess`) · Win32k 콜아웃 (`PspUpdateCalloutParameters` `PsSessionGetWin32Callouts` `ExCallCallBack`) · 디버거 (`DbgkExitProcess` `DbgkExitThread`) · FLS (`RtlpFlsDataCleanup`) · 전력 (`PoDeletePowerRequest`) · 보안 스레드 (`KeUnsecureThread`) · 락·IRQL 도우미 · `KiSwapThread` 이하 (sast/71).
- refs (누가 부르나·누가 쓰나): `PspThreadDelete` `PspReaper` `PspExitThread` `KiSchedulerApcTerminate` `PspExitLastThread` `KeTerminateThread` `PspTerminateAllThreads` → `ms/disassemble/ntoskrnl/_refs/`.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`, sast/73 의 `KiCommitThreadWait +0x220 / ret +0x225 → KiSwapThread` 를 `_calls.tsv` 에서 다시 확인. 또 run75 가 적은 `KeTerminateThread +0x24f → KiSwapThread` 도 `_calls.tsv` 와 같다.

## 스크립트 문제

없다. 참고만:

- `_calls.tsv` 는 "뽑은 함수가 부르는 것" 만이라, `PspThreadDelete` 와 `PspReaper` 는 caller 가 안 나온다 (간접). `refs` 로 찾았다 (질문 7). `PspExitThread` 의 caller 중 `KiSchedulerApcTerminate +0x2f` 도 같은 이유로 `refs` 에서만 나왔다.
- `PspExitThread` 는 같은 함수의 떨어진 범위(`+0x27db2e` 이후)가 `.asm` 끝에 붙어 있다. 오프셋 표기가 이상해 보여도 정상 (스크립트 설명 그대로).
- `.asm` 에 "읽지 않은 틈 `+0x46f~+0x495` (39바이트)" 가 있다 — TEB 블록 안의 예외 착지점. 흐름은 `+0x46d JMP +0x496` 으로 이어진다.

## 읽는 법 (표기)

- `[정적]` = 코드에 이렇게 쓰여 있다. 실행 때 이 길로 간다는 뜻이 아니다. `[추측]` = 코드 동작에서 짐작한 이름·뜻.
- 칸 이름은 공개 PDB (`pdb_struct.py`) 로 확인한 것만 이름을 쓴다:
  - `_ETHREAD`: `Tcb +0x0` (= `_KTHREAD`, 헤더가 맨 앞) · `Cid +0x508` · `TerminationPort +0x4e8` (= `ReaperLink`, 같은 칸) · `CreateTime +0x4c0` · `ExitTime +0x4c8` · `ThreadListEntry +0x578` · `RundownProtect +0x588` · `CrossThreadFlags +0x5a0` (비트: `Terminated` 0 · `ThreadInserted` 1 · `BreakOnTermination` 5) · `SameThreadPassiveFlags +0x5a4` (`ActiveExWorker` 0) · `ExitStatus +0x5d8` · `Silo +0x698` · `SetContextState +0x6a8` · `OwnerEntryListHead +0x6b8` · `FlsData +0x6f0`.
  - `_KTHREAD`: `Header +0x0` · `ThreadLock +0x40` · `MiscFlags +0x74` (`SystemThread` 비트 10 · `ApcQueueable` 비트 14 · `CetUserShadowStack` 비트 20) · `ThreadFlags +0x78` (`EnableStackSwap` 비트 6 · `TerminationApcRequest` 비트 15) · `ApcState +0x98` (프로세스 `+0xb8`) · `Teb +0xf0` · `ThreadCounters +0x168` · `State +0x184` · `Win32Thread +0x1c8` · `KernelApcDisable +0x1e4` · `Process +0x220` · `PreviousMode +0x232` · `WaitReason +0x283` · `SchedulerApc +0x288` (임베디드 `_KAPC`) · `WaitPrcb +0x2c8` · `LegoData +0x2d0` · `ThreadListEntry +0x2f8`.
  - `_KPROCESS`: `ThreadListHead +0x30` · `ProcessLock +0x40` · `ProcessListEntry +0x110` · `SecureState +0x170`.
  - `_EPROCESS`: `ProcessLock +0x1c8` (푸시락) · `Flags +0x1f4` (비트: `ProcessExiting` 2 · `ProcessDelete` 3 · `BreakOnTermination` 13 · `ProcessRundown` 25 · `ProcessSelfDelete` 30) · `Win32Process +0x298` · `DebugPort +0x308` · `WoW64Process +0x310` · `ThreadListHead +0x370` · `ActiveThreads +0x380` · `LastThreadExitStatus +0x38c` · `ExitStatus +0x554` · `ExitTime +0x5c0` · `Flags3 +0x5fc` (`Minimal` 0) · `ThreadListLock +0x5e0` · `Machine +0x6ec`.
  - `_TEB`: `DeallocationStack +0x1478` · `DbgSsReserved +0x16a0` (16바이트 — `+0x16a8` = 둘째 칸) · `SameTebFlags +0x17ee`.
  - `_OBJECT_TYPE`: `DefaultObject +0x20` · `TypeInfo +0x40` (`DeleteProcedure` = 형식 `+0x88`) · `ObjectTypeFlags` 비트 `UseDefaultObject` 2.
- 숫자만 `[정적]`, 이름은 `[추측]`: `_KTHREAD.State` 값 `4` = Terminated (앞 바퀴가 쓴 `2` Running · `5` Waiting · `7` DeferredReady 와 같은 표 — 공개 PDB 에 열거형 자체는 없음) · `WaitReason 0x16` = `WrTerminated` (sast/73 의 `0x20` = `WrPreempted` 와 같은 표).
- 이름 없는 칸: `PRCB+0x2d08` = 깨운 스레드를 쌓는 목록 머리 (sast/73), `PsReaperListHead`·`PsReaperWorkItem` 는 PDB 이름이 있다.

## 한눈에 (그림)

```
[Worker 가 돌아옴]  … RtlUserThreadStart → BaseThreadInitThunk +0x20 → RtlExitUserThread → NtTerminateThread(0, status)   (ntdll 쪽은 run25)

NtTerminateThread(핸들, status)
  핸들 0   : 프로세스 ActiveThreads == 1 이면 0xc00000db 로 돌려보냄 (마지막 스레드는 프로세스 끝내기 길로 가라)
  핸들 -2  : (검사 없이) 자기
  그 밖    : 핸들 → 스레드 객체. 자기를 가리키면 자기, 아니면 남
  └ PspTerminateThreadByPointer(스레드, status, 직접끝내기)
        자기 + 직접 : CrossThreadFlags.Terminated 켜고 → PspExitThread(status)   ← 안 돌아옴
        남         : Terminated 켜고(처음 켠 쪽만 ExitStatus 씀) → KeRequestTerminationThread → (그 스레드가 커널 APC 를 받으면)
                     KiSchedulerApcTerminate → PspExitThread(Thread.ExitStatus)

PspExitThread (끝나는 스레드 자신의 문맥에서)
  ExitStatus 적기 → 스레드 끝 통지 → [프로세스 락] ActiveThreads-1
        ├ 0 이 됨 (마지막)  : 프로세스 Flags |= ProcessDelete|ProcessRundown, PspExitLastThread 4단계 (0·1·2·3)
        └ 아님              : Process.LastThreadExitStatus 적기
  디버거·LPC 끝 메시지·Win32k 콜아웃 → FLS·타이머·I/O 취소·뮤턴트 정리
  TEB 블록 (마지막 스레드는 건너뜀):  유저 스택 ZwFreeVirtualMemory(MEM_RELEASE) → 디버그 핸들 닫기 → MmDeleteTeb
  ExitTime → KeRundownApcQueues → … → KeTerminateThread  ← 안 돌아옴

KeTerminateThread (IRQL 2)
  KPROCESS 스레드 목록에서 빼기 → 스레드 객체 SignalState=1 → 기다리는 모든 블록 KiTryUnwaitThread (신호는 그대로 남음)
  → State=Terminated(4) → KiProcessThreadWaitList → PsReaperListHead 에 얹음(+ 작업 항목) → KiSwapThread  ← 안 돌아옴

(나중) PspReaper → KeDeleteThread·커널 스택 해제·참조 하나 내림 … 참조가 0 이 될 때 PspThreadDelete (KPROCESS 아닌 EPROCESS 목록에서 빼기·TID 반납 등. TEB·유저 스택은 여기서 안 품)
```

## 질문별 답 [정적]

### 1. 사슬 — `NtTerminateThread` → `PspTerminateThreadByPointer` → `PspExitThread`

인자: `NtTerminateThread(RCX=ThreadHandle, EDX=ExitStatus)` (`+0x20 MOV EBP,EDX`). `RSI` = 지금 스레드 (`+0x17 MOV RSI,GS:[0x188]`).

| 경우 | 줄 | 하는 일 |
|---|---|---|
| 핸들 `0` | `+0x22 TEST RCX,RCX / +0x25 JNZ +0x60` 을 안 탐 → `+0x27 MOV RAX,[RSI+0xb8]` (프로세스) · `+0x2e MOV ECX,[RAX+0x380]` (`ActiveThreads`) · **`+0x34 CMP ECX,1 / +0x37 JZ +0x59`** → `+0x59 MOV EAX,0xc00000db` (`STATUS_CANT_TERMINATE_SELF`) | 마지막 (하나 남은) 스레드면 **끝내지 않고 실패로 돌려보낸다.** (유저 쪽 `RtlExitUserThread` 가 "마지막이면 프로세스 끝내기" 로 가야 하는 까닭 `[추측]`, ntdll 쪽은 이번에 안 읽음) |
| 핸들 `0`, 하나 아님 | `+0x39 MOV R8B,1 / +0x3c MOV EDX,EBP / +0x3e MOV RCX,RSI` → **`+0x41 / ret +0x46 PspTerminateThreadByPointer(지금 스레드, status, 직접=1)`** | 자기 자신 끝내기 |
| 핸들 `-2` (`NtCurrentThread()`) | `+0x60 CMP RCX,-2 / +0x64 JZ +0x39` | 위 `ActiveThreads` 검사 **없이** 곧장 같은 자기 자신 길 |
| 그 밖 핸들 | **`+0x95 / ret +0x9a ObpReferenceObjectByHandleWithTag`** (`EDX=1` = `THREAD_TERMINATE`, 형식 `PsThreadType` `+0x81`, 모드 = `PreviousMode` `+0x7a`, 태그 `0x65547350`) → 실패면 `+0x9e JS` 로 그 코드 반환 | 핸들 → 스레드 객체 |
| …객체가 자기 | `+0xa5 CMP RCX,RSI / +0xa8 JZ +0xca` → `+0xcf / ret +0xd4 ObfDereferenceObjectWithTag` → `+0xd4 JMP +0x39` | 자기를 가리키는 진짜 핸들도 **자기 길** (역시 `ActiveThreads` 검사 없음) |
| …객체가 남 | `+0xaa XOR R8D,R8D` → **`+0xaf / ret +0xb4 PspTerminateThreadByPointer(스레드, status, 직접=0)`** → `+0xc0 / ret +0xc5` 참조 내림 | 남 끝내기 |

`PspTerminateThreadByPointer(Thread=RCX→RBX, ExitStatus=EDX→ESI, 직접=R8B→BPL)` 의 갈림:

- **자기 + 직접**: `+0x2d TEST BPL,BPL / +0x30 JZ +0x50` 을 안 타고, `+0x32 MOV RAX,GS:[0x188] / +0x3b CMP RBX,RAX / +0x3e JNZ +0x50` 도 안 타면 → **`+0x40 OR.LOCK [RBX+0x5a0],1`** (`CrossThreadFlags.Terminated`) → `+0x48 MOV ECX,ESI` → **`+0x4a / ret +0x4f PspExitThread(ExitStatus)`** → `+0x4f INT3` (돌아오지 않는 호출).
- **그 밖 (남, 또는 직접=0)**: `+0x50` — 질문 5(a).
- 먼저 `+0x29 TEST AL,0x20 / +0x2b JNZ +0x9b` (`CrossThreadFlags.BreakOnTermination`): 켜져 있고 프로세스 `Flags & 0x40000008` (`ProcessDelete|ProcessSelfDelete`) 이 아니면 `+0xc8 / ret +0xcd PspCatchCriticalBreak` (중요 스레드 끝날 때 멈춤. 이름만).
- 호출하는 곳 (`_calls.tsv`+`refs`): `NtTerminateThread +0x41/+0xaf`, `NtTerminateProcess +0x1ec`, `PspTerminateAllThreads +0xd4/+0x221`, `PspUserThreadStartup +0xf0`. `PspExitThread` 를 부르는 곳은 이 함수 `+0x4a`, `NtTerminateProcess +0x161` (자기 프로세스 끝내기 뒤 자기), `KiSchedulerApcTerminate +0x2f` 셋.

**지금 우리 (PE_emulator, 197 메모 기준):** "`PspTerminateThreadByPointer` 가 `bTerminated` 를 켜고 지금 스레드면 이어서 `PspExitThread`" — **맞다.** 모자란 것: (1) **핸들 0 + `ActiveThreads==1` 이면 `0xc00000db`** (`-2`·자기 핸들은 이 검사 없음) 갈래, (2) "남이면 `PspExitThread` 를 그 자리에서 부르지 않고 종료 요청만 건다" (질문 5(a)) — 지금 에뮬레이터가 남도 곧바로 끝낸다면 **다르다.**

### 2. `PspExitThread` 본문 순서 [줄기만, 위에서 아래 실행 순서]

인자: `ECX`=ExitStatus (`+0x15 MOV R13D,ECX`), `RDI`=지금 스레드 (`+0x39 MOV RDI,GS:[0x188]`), `R14`=`ThreadsProcess` (`+0x4a MOV R14,[RDI+0x220]`). `R12B` = **"마지막 스레드" 표지** (처음 0, `+0xf4 MOVZX R12D,SIL`).

**순서 표** (한 줄 = 한 일. `★` = 질문이 물은 줄. 괄호 안은 이름만 본 것):

| # | 오프셋 (call / ret) | 한 일 | 조건 |
|---|---|---|---|
| 1 | `+0x66 / +0x6b` | `PspClearProcessThreadCidRefs(Thread, …)` | 항상 |
| 2 | `+0x75 JNZ +0x72b` → `+0x740 KeBugCheckEx(5)` | 붙은 프로세스 (`Tcb.ApcState.Process` `+0xb8`) 가 `ThreadsProcess` 와 다르면 bugcheck | 항상 검사 |
| 3 | `+0x90 MOV CR8,RSI(0)` | IRQL 0 으로 내림 (`KiIrqlFlags≠0` 이면 `+0x8b / +0x90 KiLowerIrqlProcessIrqlFlags`) | |
| 4 | `+0xa3 → +0x759` · `+0xb1 → +0x76f` · `+0xbe → +0x77a` | `ActiveExWorker` 켜져 있으면 bugcheck `0xe9` · `KernelApcDisable≠0` 이면 bugcheck `0x20` · `ChargeOnlySession(+0x568)` 이 있으면 `KeSetThreadChargeOnlySchedulingGroup`+참조 내림 | |
| 5 | `+0xcb / +0xd0` · `+0xd3 / +0xd8` · **`+0xdf / +0xe4`** | `PspEmptyPropertySet(Thread+0x658)` · `PspRevertContainerImpersonation` · **`ExWaitForRundownProtectionRelease(Thread.RundownProtect +0x588)`** (남이 이 스레드 참조를 쥐고 있는 동안 기다림, 이후 새 참조 못 얻음) | |
| 6 | `+0xee → +0x713 PoDeletePowerRequest` | `LegacyPowerObject(+0x570)` 있으면 지움 | |
| 7 | `+0x100 MOV [RDI+0x5d8],R13D` | **`Thread.ExitStatus = status`** | |
| 8 | `+0x119 / +0x11e EtwTraceThread` | ETW | `PerfGlobalGroupMask & 2` |
| 9 | `+0x122 ADD word [RDI+0x1e4],-1` | `KernelApcDisable` 를 -1 (임계 영역 들어감 — 끝날 때까지 커널 APC 안 받음) | |
| 10 | **`+0x142 / +0x147 PspCallThreadNotifyRoutines(Thread, 0, 0)`** | **스레드 끝남 통지** (`PsSetCreateThreadNotifyRoutine` 로 등록된 콜백들) | `Process.Flags3.Minimal` 이고 `PicoContext(+0x640)==0` 이면 건너뜀 (`+0x12a~+0x13a`, `+0x673`) |
| 11 | `+0x156 / +0x15b KeAbPreAcquire`, `+0x160 BTS.LOCK [RBX]`, `+0x171 / +0x176 ExfAcquirePushLockExclusiveEx` | **프로세스 락** (`EPROCESS.ProcessLock +0x1c8`) 독점 | |
| 12 | ★ **`+0x184 MOV EAX,[R14+0x380]` / `+0x18b SUB EAX,R15D` / `+0x18e MOV [R14+0x380],EAX`** | **`ActiveThreads -= 1`** | 항상 (락 안) |
| 13 | ★ `+0x1a5 MOV EAX,[R14+0x380]` / `+0x1ac TEST` / **`+0x1ae CMOVZ R12D,R15D`** / `+0x1ba TEST R12B,R12B` / **`+0x1bd JNZ +0x685`** | 0 이면 **마지막** 표지 `R12B=1` 로 갈림 | |
| 14a | ★ (마지막) `+0x685 OR.LOCK [R14+0x1f4],0x2000008` → **`+0x6a1 / ret +0x6a6 PspExitLastThread(ECX=0, DL=1, 프로세스, 스레드, status)`** → `+0x6a6 JMP +0x1de` | `Process.Flags |= ProcessDelete(0x8) | ProcessRundown(0x2000000)`, **단계 0** (질문 4). 이 호출이 프로세스 락도 풀어서 `+0x1d9` 를 건너뜀 | `R12B≠0` |
| 14b | (마지막 아님) `+0x1c3 CMP R13D,0xc000004b / +0x1ca JZ` 아니면 `+0x1cc MOV [R14+0x38c],R13D` | `Process.LastThreadExitStatus = status` (단 `STATUS_THREAD_IS_TERMINATING` 이면 안 씀) | `R12B==0` |
| 15 | `+0x1d9 / +0x1de PspUnlockProcessExclusive(Process, Thread)` | 프로세스 락 풀기 | 마지막 아닐 때만 여기서 (마지막은 14a 안에서) |
| 16 | `+0x1de → +0x1ef JNZ +0x27db2e` (cold) `PsAttachSiloToCurrentThread` | `Thread.Silo(+0x698)` 가 `-3` 아니면 silo/작업 객체 쪽 정리 (이름만) | |
| 17 | `+0x1f5 CMP [R14+0x308],RSI` → `+0x7b9 / +0x7be DbgkExitProcess` 또는 `+0x27dbac / +0x27dbb1 DbgkExitThread` | `DebugPort` 가 있으면 디버거에 프로세스/스레드 끝 알림 | `SystemThread` 아니면. 마지막이면 Process, 아니면 Thread |
| 18 | `+0x202 TEST [R14+0x170],1` → `+0x7d3 KeUnsecureThread`, `+0x7e8 MmUnlockPages`, `+0x7fa ExFreePoolWithTag` | 보안 프로세스 (`KPROCESS.SecureState` 비트 0) 의 보안 스레드 정리 (이름만) | |
| 19 | `+0x20f CMP [KdDebuggerEnabled]` → `+0x216 JNZ +0x27dbb7` … `+0x27dbff / ret +0x27dc04 PspCatchCriticalBreak` | 중요 스레드 끝남 알림 (이름만) | 커널 디버거 켜짐 + `BreakOnTermination` |
| 20 | (마지막) `+0x21c → +0x58d / ret +0x592 PspExitLastThread(1)` | **단계 1** | `R12B≠0` |
| 21 | `+0x225 MOV R13,[RDI+0x4e8]` → `+0x6ab` 고리: `+0x6d0 / +0x6d5 LpcRequestPort` · `+0x6ef / +0x6f4 ObfDereferenceObject` · `+0x700 / +0x705 ExFreePoolWithTag` (`+0x80f KeDelayExecutionThread` 다시 시도) | **`TerminationPort` 목록** (스레드 끝 알림 LPC 메시지 `0x300008`) | 목록이 비어있지 않을 때 |
| 22 | `+0x235 TEST [RDI+0x5a0],2` → `+0x242 / +0x247 PsCaptureExceptionPort`, `+0x273 / +0x278 LpcRequestPort`, `+0x291 / +0x296 ObfDereferenceObject` | 예외 포트로 끝 메시지 | `ThreadInserted` |
| 23 | `+0x296 MOV RAX,[RDI+0x1c8]` → `+0x2d5 / +0x2da PspUpdateCalloutParameters`, `+0x2de / +0x2e3 PsSessionGetWin32Callouts`, `+0x2f1 / +0x2f6 ExCallCallBack` | `Win32Thread` 있으면 **Win32k 스레드 끝 콜아웃** (이름만) | |
| 24 | (마지막) `+0x2f6 → +0x5a6 / ret +0x5ab PspExitLastThread(2)` | **단계 2** | `R12B≠0` |
| 25 | `+0x2ff MOV EAX,[RDI+0x78] / +0x302 TEST AL,0x40 / +0x304 JZ +0x819` → `+0x82b KeBugCheckEx(0x94)` | `EnableStackSwap` 가 꺼져 있으면 (커널 스택이 잠긴 채 끝남) bugcheck | |
| 26 | `+0x30a~+0x33b` : `FlsData(+0x6f0) |= 1` → 값이 있었으면 `+0x336 / +0x33b RtlpFlsDataCleanup` | FLS 첫 정리 (이름만) | |
| 27 | `+0x340 IoCancelThreadIo` · `+0x345 ExTimerRundown` · `+0x34d CmNotifyRunDown` · `+0x35b KiRundownMutants` · `+0x363 PspSchedulerSharedDataRegionSlotFree` | I/O 취소·타이머·레지스트리 통지·**뮤턴트 놓기 (`Thread+0x308` 목록, sast/73 질문 4)**·스케줄러 공유 칸 (이름만) | |
| 28 | ★ **`+0x368 … +0x4b8 / ret +0x4bd`** — TEB 블록 | **유저 스택 풀기 → 디버그 핸들 닫기 → 그림자 스택 → `MmDeleteTeb`** | 아래 (b)(c) |
| 29 | `+0x4bd / +0x4c2 KeQuerySystemTimeUnsafe` → 0 이면 `+0x5c9 MOV RAX,[0xfffff78000000014]` (`KUSER_SHARED_DATA.SystemTime`), 아니면 `+0x4d2 / +0x4d7 KeQuerySystemTimePrecise` | **`Thread.ExitTime (+0x4c8)`** | |
| 30 | (마지막) `+0x4d7 → +0x5bf / ret +0x5c4 PspExitLastThread(3)` | **단계 3 (`PspExitProcess`·`PspRundownSingleProcess`)** | `R12B≠0` |
| 31 | **`+0x4e3 / +0x4e8 KeRundownApcQueues(Thread)`** | **APC 큐 비우기** (커널·유저 APC 정리) | |
| 32 | `+0x4e8 CMP [RDI+0x2d0],RSI` → `+0x844 / +0x849 PspLegoNotifyRoutine` (간접) | `LegoData` 있으면 레고 통지 | |
| 33 | `+0x4f5~+0x614` : `FlsData |= 2` → 값 있으면 `+0x5e2 RtlpFlsSlotDataValidateEmpty`, `+0x60a RtlpFlsDataCleanup(…,2)` | FLS 끝 정리 | |
| 34 | `+0x525 / +0x52a PspThreadIndexCleanup(Thread, 마지막?)` | 스레드 번호표 정리 | |
| 35 | `+0x52a [RDI+0x6a8]` → `+0x85a / +0x85f ExFreePoolWithTag` | `SetContextState` 해제 | 있을 때 |
| 36 | `+0x53a LEA RAX,[RDI+0x6b8] / +0x544 CMP R9,RAX / +0x547 JNZ +0x565` → `+0x577 KeBugCheckEx(0x1c6)` | `OwnerEntryListHead` 가 비어 있지 않으면 bugcheck | |
| 37 | ★ **`+0x549 MOV RCX,RDI` → `+0x54c / ret +0x551 KeTerminateThread(Thread)`** | **마지막 호출. 안 돌아온다** (`+0x551` 은 그냥 끝맺음 코드, 질문 3(c)) | 항상 |

**(a) `ActiveThreads` 와 마지막 갈래 — 표의 12~14.** `+0x18e` 가 내리는 줄, `+0x1ae/+0x1bd` 가 0 일 때 마지막으로 가르는 줄, `+0x6a1 / ret +0x6a6` 이 `PspExitLastThread`. 락 (`ProcessLock +0x1c8`) 안에서 한다. `Minimal` 프로세스 변종 (`Flags3.Minimal` + `Flags4.MinimalTerminate`, `+0x619~+0x64f`) 도 `R12B=1` 로 합류한다 (이름만).

**(b) TEB 풀기 — `MmDeleteTeb` 를 부르는 줄: `+0x4b8 / ret +0x4bd`.** `MmDeleteTeb(Process=R14, Teb=RBX)`. 가는 조건 (모두 지켜야 함):

1. `+0x368 MOV RBX,[RDI+0xf0]` — `KTHREAD.Teb ≠ 0` (`+0x377 JZ +0x4bd`). 먼저 **`+0x37d MOV [RDI+0xf0],RSI(0)`** (커널 쪽 `Teb` 칸을 지움) · `+0x38a / ret +0x38f PspLockUnlockTebAccessExclusive` (TEB 건드리는 쪽과 맞춤. 이름만).
2. `+0x38f TEST [RDI+0x74],0x400 / +0x396 JNZ +0x4bd` — **`SystemThread` 가 아니어야** 한다.
3. `+0x39c MOV EAX,[R14+0x1f4] / +0x3a3 TEST EAX,0x40000008 / +0x3a8 JNZ +0x4bd` — **프로세스 `Flags` 의 `ProcessDelete`(0x8) · `ProcessSelfDelete`(0x40000000) 가 꺼져 있어야** 한다.
   - **마지막 스레드는 건너뛴다**: 표 14a (`+0x685`) 가 이 블록보다 **먼저** `ProcessDelete` 를 켜 놨다. 곧 마지막 스레드는 TEB 도, 유저 스택도, `+0x4b8` 도 안 부른다. (프로세스 주소 공간째 질문 4 의 `PspRundownSingleProcess → MmCleanProcessAddressSpace` 가 치운다 `[정적: 이름·순서]`.)
4. `MmDeleteTeb` 가 하는 일 (본문 `.asm`): `+0x47 KeStackAttachProcess` → `+0x56 MiObtainReferencedVadEx(Teb)` → `+0x69 MiFreeToSubAllocatedRegion(Vad, Teb, 크기)` (크기 `+0x3d MOV EBX,0x2000`; `WoW64Process≠0` 이고 `Machine` 이 `0x14c`/`0x1c4` 면 `0x4000`, `+0xa7`) → `+0x75 KiUnstackDetachProcess`. 곧 TEB 쪽 하위 할당 영역에 돌려놓는다.
5. 만들기 쪽 (`PspAllocateThread` · `PspInsertThread +0x9ab` 의 `MmDeleteTeb`) 은 만들다 실패했을 때 되돌림이다 (run74 와 짝).

**(c) 유저 스택 풀기 — 있다. 커널이 한다. `ZwFreeVirtualMemory` 를 `PspExitThread +0x3f2 / ret +0x3f7` 에서 부른다.**

- 조건 (위 (b)의 1~3 에 **더해**): `+0x3ae MOV EAX,[RDI+0x5a0] / +0x3b4 TEST AL,2 / +0x3b6 JZ +0x400` — **`CrossThreadFlags.ThreadInserted` 가 켜져 있어야.** (`PspInsertThread +0x490 OR.LOCK [R14+0x5a0],2` 가 켠다. 끼워 넣기까지 못 간 스레드는 스택을 안 품 — 그 경우는 만든 쪽이 되돌림.)
- 줄: `+0x3b8 MOV RAX,[RBX+0x1478]` (**TEB 에서 바로 읽은 `DeallocationStack`**, 커널 쪽 복사본 아님 — 유저 메모리를 이 시점에 읽음) → `+0x3bf/+0x3c7` 로 `BaseAddress` 칸에 담고 → `+0x3cc/+0x3d4` `RegionSize = 0` → `+0x3dc MOV R9D,0x8000` (`MEM_RELEASE`) → `+0x3ef MOV RCX,R13` (R13 은 `+0x318 OR R13,-1` 로 `-1` = 자기 프로세스) → **`+0x3f2 / ret +0x3f7 ZwFreeVirtualMemory(-1, &DeallocationStack, &0, MEM_RELEASE)`**. 크기 0 + MEM_RELEASE = 예약 영역 **통째로** (`RtlCreateUserStack` 이 예약한 영역 전체: 커밋·가드 포함, run74).
- **`SameTebFlags` (`TEB+0x17ee`) 는 안 본다.** `PspExitThread`·`MmDeleteTeb`·`PspThreadDelete`·`KeTerminateThread`·`PspExitLastThread` `.asm` 에서 `0x17ee`/`0x17ec` 를 `grep` 하면 0건. 조건은 위처럼 커널 쪽 칸들 (`Teb≠0`, `SystemThread`, 프로세스 `Flags`, `ThreadInserted`) 뿐이다. (`FreeStackOnTermination` 같은 비트는 공개 PDB `SameTebFlags` 에 없음.)
- WoW64 (`Process.WoW64Process +0x310 ≠ 0` 이고 `Machine` `0x14c`/`0x1c4`): 한 번 더 `+0x42f MOV EAX,[RBX+0x2e0c]` (32비트 TEB 의 `DeallocationStack`) → **`+0x457 / ret +0x45c ZwFreeVirtualMemory`**. 네이티브 64비트 test.exe 는 해당 없음.
- 이어서 `+0x400 MOV RCX,[RBX+0x16a8]` (`TEB.DbgSsReserved` 둘째 칸 — 디버그 쪽 핸들 `[추측: 뜻]`) 가 0 이 아니면 **`+0x40f / ret +0x414 ObCloseHandle(핸들, KernelMode=1)`**. 그다음 `+0x496~+0x4ac`: `ThreadInserted` 이고 `MiscFlags.CetUserShadowStack(0x100000)` 이면 `+0x654 / ret +0x659 PspFreeCurrentThreadUserShadowStack` (이름만) → 그리고 (b)의 `MmDeleteTeb`.
- 유저 쪽 `ntdll` 이 스택을 안 푸는 것은 PE_emulator 메모 (`RtlExitUserThread` 는 스택을 안 품) 와 **같은 결론**: 푸는 건 커널, 끝나는 스레드 자신의 문맥에서.

**(d) 프로세스의 스레드 목록에서 빼는 줄 — `PspExitThread` 에는 없다.** 목록이 둘이고 둘 다 다른 함수가 뺀다:

| 목록 | 빼는 곳 | 줄 |
|---|---|---|
| `KPROCESS.ThreadListHead (+0x30)` ← `KTHREAD.ThreadListEntry (+0x2f8)` | **`KeTerminateThread`** | `+0xe2 KiAcquireKobjectLockSafe(Process)` · 안전 검사 `+0xee~+0xf7` · **`+0x116 MOV [RAX],RDX` / `+0x119 MOV [RDX+8],RAX`** · `+0x11d AND.LOCK [RDI],~0x80` |
| `EPROCESS.ThreadListHead (+0x370)` ← `ETHREAD.ThreadListEntry (+0x578)` | **`PspThreadDelete`** (객체가 지워질 때) | `+0x13c PspLockProcessExclusive` · `+0x155~+0x16e` 푸시락 `EPROCESS.ThreadListLock (+0x5e0)` · `+0x17b KeFoldProcessStatisticsThread` · 안전 검사 `+0x180~+0x190` · **`+0x374 MOV [RAX],RCX` / `+0x377 MOV [RCX+8],RAX`** · `+0x396 PspUnlockProcessExclusive`. (넣는 쪽은 `PspInsertThread +0x6e3~+0x6ea`) |

→ 곧 **끝난 스레드도 `EPROCESS.ThreadListHead` 에는 객체가 지워질 때까지 남아 있다.** 그래서 `PspExitLastThread` 단계 0 이 이 목록을 돌며 "아직 신호가 아닌 (`SignalState==0`) 다른 스레드" 를 기다린다 (질문 4).

**(e) `KeTerminateThread` 부르는 줄: `+0x54c / ret +0x551`.** 이 함수의 마지막 줄기. 이 뒤로 `PspExitThread` 에 하는 일이 없다 (그냥 맺음 코드).

**그 밖 (이름과 오프셋만):** 위 표에 한 줄씩. 끝 통지 = 10 (`PspCallThreadNotifyRoutines +0x142`), APC 정리 = 31 (`KeRundownApcQueues +0x4e3`), LPC = 21·22 (`LpcRequestPort +0x6d0/+0x273`) 와 단계 3 의 `LpcExitProcess` (`PspExitLastThread +0xe9`).

**지금 우리 (PE_emulator):** "`PspExitThread` 가 마지막인지 갈라 처리, 아니면 진단 한 줄 + 넘김, 이벤트·CPU 멈춤 없음" — **줄기 모양은 맞다 (마지막 갈래 있음).** 모자란 것 (197 메모의 "공사 중" 목록 그대로 확인): **`ActiveThreads` 내림 (`+0x18e`)** · **TEB·유저 스택 풀기 (`+0x3f2`·`+0x4b8`, 마지막 스레드는 건너뜀)** · `ExitTime` · **`ThreadListHead` 빼기는 `PspExitThread` 가 아니라 (KPROCESS 쪽) `KeTerminateThread` 와 (EPROCESS 쪽) `PspThreadDelete` 가 한다** — 197 메모가 "`ThreadListHead` 빼기 (+0x370) 더함" 을 `PspExitThread` 안 주석으로 두었다면 **자리가 다르다.**

### 3. `KeTerminateThread` (`KeTerminateThread(Thread=RCX→RBX)`, 821B)

**(a) `SignalState = 1` 줄: `+0x16c MOV dword ptr [RBX+4],0x1`.** 그 앞 `+0x157 MOV RBP,GS:[0x20]` (PRCB) · `+0x160 / ret +0x168 KiAcquireKobjectLockSafe(Thread)` (헤더 락, 비트 7), 뒤 `+0x1a9 AND.LOCK [RBX],0xffffff7f` (락 풀기). **이미 1 이어도 그냥 덮어쓴다** (`KeSetEvent` 처럼 옛 값을 보고 빠지는 검사가 없음). 객체 `Type` 은 6 (스레드, sast/73).

**(b) 기다리던 쪽을 깨우는 꼴 — sast/73 질문 2(a) "수동 재설정" 과 같은 결과: 전부 깨우고 신호로 남는다.** 다른 점은 부르는 모양.

- 줄: `+0x173 MOV RSI,[RDI]` (`RDI=Thread+8` 대기 목록 머리) → `+0x176 CMP RSI,RDI / +0x179 JZ +0x1a2` (비면 끝) → `+0x181 MOV CL,[RAX+0x10]` (`WaitType`) → `+0x184 CMP CL,1 / +0x187 JNZ +0x2b4`.
- **`WaitType==1` (한 객체라도 풀리면 깸 = `WaitForSingleObject`, sast/73)**: `+0x18d MOVZX R8D,word [RAX+0x12]` (`WaitKey`) · `+0x192 XOR R9D,R9D` · `+0x195 MOV RDX,RAX` · `+0x198 MOV RCX,RBP` → **`+0x19b / ret +0x1a0 KiTryUnwaitThread(Prcb, WaitBlock, WaitStatus=WaitKey, 0)`** → `+0x1a0 JMP +0x176`(다음 블록). `KeSetEvent` 가 풀어 넣은 것과 **같은 함수의 부름** (sast/73 질문 3). 반환값은 안 본다. test.exe 의 `WaitForSingleObject(t)` 는 `WaitKey=0` → 메인 `WaitStatus = 0` = `STATUS_WAIT_0` (sast/73 질문 2(b)).
- `WaitType==2` (큐 알림): `+0x2b9 BlockState=5` · `+0x2c4 / ret +0x2c9 KiInsertQueueInternal`. `WaitType==4` (DPC): `+0x309 BlockState=5` · `+0x310 AND [RBX+4],0` (SignalState 를 0 으로!) · `+0x31b / ret +0x320 KeInsertQueueDpc`. 그 밖 (`0` = 전부 기다림): `+0x2fe MOV R8D,0x100` → 같은 `KiTryUnwaitThread` (깨난 쪽이 처음부터 다시 검사, sast/73 질문 2(c)).
- 끝에서 **목록 머리를 비운다**: `+0x1a2 MOV [RDI+8],RDI / +0x1a6 MOV [RDI],RDI` (블록을 하나씩 떼지 않음 — sast/73 질문 2(a) 수동 루프와 같다).
- 깨운 스레드를 준비 줄로 보내기: `+0x1b7 CMP [RBP+0x2d08],0 / +0x1bf JZ +0x1d3` → **`+0x1ce / ret +0x1d3 KiProcessThreadWaitList(Prcb, EDX=1, R8D=0, R9D=0)`** (sast/73 질문 2(c): → `KiDeferredReadySingleThread`). **`KiExitDispatcher` 는 안 부른다** — IRQL 2 에서 곧 `KiSwapThread` 로 가기 때문에 스스로 비운다 `[추측: 까닭]`. 이어지는 `KiSwapThread` 도 같은 목록을 한 번 더 확인한다 (sast/73: `KiSwapThread +0x14`).

**(c) `State = Terminated`: `+0x1b0 MOV byte ptr [RBX+0x184],0x4`** (헤더 락을 푼 직후, `+0x1a9` 다음). 마지막 넘기기: **`+0x24f / ret +0x254 KiSwapThread(Thread, Prcb, 0)`** (sast/71 질문 1 — "누가 `KiSwapThread` 를 부르나" 목록에 이미 있음). **돌아오지 않는다**: 스레드가 어느 준비 줄에도 안 들어가고 (이 함수에 `KiQueueReadyThread` 류 호출 없음, `KiSwapThread` 도 현재 스레드를 줄에 넣지 않음 — sast/71) `State=4` 라서 다시 뽑힐 길이 없다. `+0x254` 이후는 일반 맺음 코드. (`+0x46 MOV byte [RBX+0x283],0x16` = `WaitReason=WrTerminated` 도 미리 적는다.)

  그 사이에 하는 일 (순서): `+0x4d~+0x57` IRQL 2 로 (`MOV CR8,R15`, `R15=2`) → … → **`+0x1d3~+0x1fc` 청소부 목록에 얹기**: `LEA R8,[RBX+0x4e8]` (`ETHREAD.ReaperLink`) · `+0x1e8 MOV [R8],RAX` · `+0x1ee CMPXCHG.LOCK [PsReaperListHead],R8` (잠금 없는 push) · 목록이 비어 있었으면 (`+0x1fc TEST RAX,RAX / +0x1ff JNZ +0x287` 을 안 탐) **`+0x213 / ret +0x218 ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)`** (Ghidra 이름 `Ordinal_36`, `EDX=R15D=2`; 큐 종류 이름은 `[추측]`) → 실패하면 `+0x21c BTS [PsReaperWorkItem+0x18]` (표시만 해 두고 나중에) → `+0x22a~+0x23e` 스레드 락 (`Thread+0x40`) 이 풀릴 때까지 기다렸다 지움 (`[RBX+0x40]=0`).

**(d) 프로세스 쪽 스레드 목록에서 빼는 줄 — 있다.** 질문 2(d) 의 표: `KPROCESS.ThreadListHead` 에서 `+0x116/+0x119`. 거기 딸린 일:

- 락: `+0x4d IRQL 2` → `+0x6c / ret +0x71 ExAcquireSpinLockExclusiveAtDpcLevel(Process+0x40)` (`KPROCESS.ProcessLock`) → 프로세스 헤더 락 `+0xe2 / ret +0xe7 KiAcquireKobjectLockSafe(Process)` → … → `+0x137 / ret +0x13c ExReleaseSpinLockExclusiveFromDpcLevel`.
- **이 스레드가 프로세스의 마지막 스레드이면 프로세스도 전역 목록에서 뺀다**: `+0x7f CMP [RSI],RAX(Blink) / +0x82 JNZ +0xdf` 를 안 타면 (`Flink==Blink` = 목록에 자기뿐) → `KiProcessListLock` 큐 스핀락 (`+0xa5 XCHG [KiProcessListLock]`) → `+0xb5 LEA RAX,[RDI+0x110]` (`KPROCESS.ProcessListEntry`) → `+0xce MOV [RCX],RDX` / `+0xd1 MOV [RDX+8],RCX` → `+0xda … KxReleaseQueuedSpinLock`.
- `+0x124 CMP [RBX+0x68],0` (`SchedulingGroup`) 가 있으면 `+0x12e / ret +0x133 KiRemoveThreadFromSchedulingGroup`. `+0x13c MOV RDX,[RBX+0xe8]` (`Queue`) 가 있으면 `+0x152 / ret +0x157 KiActivateWaiterQueueWithNoLocks`. (이름만)

**지금 우리 (PE_emulator):** "끝난 스레드 객체는 신호이고 그대로 남음, 두 번째 기다림도 바로 만족" (192 메모) — 신호 쪽은 **맞다.** 모자란 것: (1) **끝날 때 그 객체를 기다리고 있는 잠든 스레드를 깨우는 일** — `WaitStatus = WaitKey (보통 0)`, 모두 깨움 (수동 이벤트 꼴), 깨운 스레드는 다시 묻지 않음 (sast/73 질문 1(c)) — 194 "잠든 쪽 깨우기" 가 이 자리여야 한다. (2) 신호를 켜는 자리가 `PspExitThread` **끝** (`KeTerminateThread`) 이고, TEB·스택은 그보다 **먼저** 풀린다 — 순서가 바뀌면 깨난 메인이 본 상태가 달라진다 (Worker 의 스택 주소가 이미 없어짐).

### 4. `PspExitLastThread` (`(ECX=단계, DL, R8=프로세스, R9=스레드, [스택]=status)`, 739B)

**답. 한 함수가 4개 단계로 나뉘어 `PspExitThread` 사이사이에 4번 불린다.** 프로세스 종료 길 (`PspExitProcess`·`PspRundownSingleProcess`) 은 **단계 3** 안에서 이어진다. 다른 스레드를 기다리는 것도 있다 (단계 0).

| 단계 | 부르는 곳 (`PspExitThread`) | 줄 | 하는 일 |
|---|---|---|---|
| **0** | `+0x6a1 / ret +0x6a6` (DL=1, 프로세스 락 쥔 채) | `+0x10e` ~ `+0x187` | `+0x122 / ret +0x127 KeForceResumeProcess` (멈춰 있던 프로세스 풀기). `+0x127 CMP [RBX+0x554],0x103` — **프로세스 `ExitStatus` 가 `STATUS_PENDING` 이면** `+0x286` 에서 `status` (단 `0xc000004b` 면 `Process.LastThreadExitStatus +0x38c`) 를 `Process.ExitStatus (+0x554)` 에 적는다 (`+0x1784b4` 조각). `+0x137 LEA R13,[RBX+0x370]` 으로 **`EPROCESS.ThreadListHead` 를 돌며** (`+0x14b LEA R15,[RDI-0x578]` = `ETHREAD`) 자기를 뺀 각 스레드의 **`SignalState` 바이트 (`+0x157 CMP byte [R15+4],0`) 가 0 이면** `+0x223 ObReferenceObjectSafeWithTag` · `+0x232 PspUnlockProcessExclusive` · **`+0x248 / ret +0x24d KeWaitForSingleObject(그 스레드)`** (질문 3 의 `+0x16c` 가 켠 신호를 기다림) · `+0x268 PspLockProcessExclusive` 로 락 다시. 끝에 `+0x170 / ret +0x175 PspUnlockProcessExclusive`. → **"활성 스레드 수가 0 이어도, 다른 스레드가 `KeTerminateThread` 를 끝낼 때까지 기다린다."** |
| **1** | `+0x58d / ret +0x592` | `+0x1a1` | `Process.Flags` 의 `BreakOnTermination (0x2000)` 이면 `+0x1d0 PspCatchCriticalBreak` (중요 프로세스 끝남, 이름만). |
| **2** | `+0x5a6 / ret +0x5ab` | `+0x1d7` | `Process.Win32Process (+0x298)` 있으면 `+0x1fa PspUpdateCalloutParameters` · `+0x203 PsSessionGetWin32Callouts` · `+0x211 ExCallCallBack(…, 0, …)` (Win32k **프로세스** 끝 콜아웃, 이름만). |
| **3** | `+0x5bf / ret +0x5c4` | `+0x58` ~ `+0x10c` | `+0x61 MOV RAX,[R9+0x4c8]` → `+0x6b MOV [RBX+0x5c0],RAX` (**`Process.ExitTime = Thread.ExitTime`**) · **`+0x74 / ret +0x79 PspExitProcess(CL=1, Process)`** (→ `PspCallProcessNotifyRoutines`·`EtwTraceProcess`·`DbgkFlushErrorPort`·`PfProcessExitNotification` …, 이름만) · `+0x81` 토큰 참조 · `+0x91 SeAuditingWithTokenForSubcategory(0x87)` → `+0x2c8 SeAuditProcessExit` (감사) · `+0xb3 DEC word [thread+0x1e4]` (`KernelApcDisable` 다시 올림) · `+0xd2 ExpWnfDeleteProcessContext` · **`+0xe1 / ret +0xe6 PspRundownSingleProcess(Process, 1)`** · `+0xe9 / ret +0xee LpcExitProcess` · `+0x100 ExFreePoolWithTag(Process+0x5c8)`. |

- `PspRundownSingleProcess` 안 (`_calls.tsv`, 이름만): `+0x68 ExWaitForRundownProtectionRelease` · `+0xa4 KeStackAttachProcess` · **`+0xb5 ObKillProcess` (핸들 표 정리)** · **`+0xbd MmCleanProcessAddressSpace` (주소 공간 치우기)** · `+0xc9 KiUnstackDetachProcess` · `+0x1d5 KeSetProcess` (프로세스 객체 신호) · `+0x1f2 PspRemoveProcessFromJobChain` · `+0x211 PspClearProcessThreadCidRefs` · `+0x346 ObReferenceProcessHandleTable` · `+0x35d ExSweepHandleTable`.
- **TEB·스택 처리가 다르다**: 마지막 스레드는 `PspExitThread` 의 TEB 블록 (표 28) 을 **건너뛰고** (표 14a 가 `ProcessDelete` 를 먼저 켬), 대신 단계 3 의 `PspRundownSingleProcess → MmCleanProcessAddressSpace` 가 프로세스 주소 공간째 (TEB·유저 스택·이미지·힙 전부) 치운다 `[정적: 호출 순서. 이 안의 세부는 안 읽음]`. 그러니 "마지막 스레드의 스택을 따로 풀지 않는다".
- `PspExitLastThread` 를 부르는 곳: `PspExitThread` 4곳 + `PsTerminateMinimalProcess +0xef/+0x103/+0x118/+0x12d` (`refs`; 최소 프로세스 끝내기, 이름만 — `NtTerminateProcess +0xb9` 가 부름).
- `PspExitLastThread` 의 `[RBP+0x50]` = 5번째 인자 `status` (스택). 인자 해석은 호출 자리 4곳의 레지스터 설정 (`ECX`=단계, `DL`=락 쥐고 있나) 으로.

**지금 우리 (PE_emulator):** "마지막이면 `process_exit` 이벤트 + `dwExitStatus` + `bLastThreadExited`" — **큰 줄기는 맞다.** 모자란 것: 마지막 스레드의 **단계 0 대기** ("다른 스레드의 `KeTerminateThread` 가 끝나 신호가 켜질 때까지") 는 CPU 하나·순서 고정이라면 필요 없을 수 있다 `[추측: 에뮬레이터 범위]`. 프로세스 `ExitStatus = ExitStatus (단 0xc000004b 면 LastThreadExitStatus)` 규칙은 필요하다.

### 5. 남의 스레드 끝내기

**(a) `PspTerminateThreadByPointer` 의 남 갈래 (`+0x50~+0x92`).** `PsExitSpecialApc` 같은 이름은 **없다.** 쓰는 것은 `KeRequestTerminationThread`.

- 검사: `+0x50 TEST [RBX+0x74],0x400 / +0x57 JNZ +0x94` — `MiscFlags.SystemThread` 면 `0xc0000022` (`STATUS_ACCESS_DENIED`). `+0x59 MOV EAX,[RDI+0x5fc] / +0x5f TEST AL,1 / +0x61 JNZ +0xd2` — 프로세스 `Flags3.Minimal` 이면 `0xc00000bb` (`STATUS_NOT_SUPPORTED`).
- **`+0x63 BTS.LOCK [RBX+0x5a0],0` (`Terminated`) / `+0x6c JC +0x74`**: 처음 켠 쪽만 **`+0x6e MOV [RBX+0x5d8],ESI` (`ExitStatus` 적기)**. 이미 켜져 있으면 상태 안 덮어씀.
- **`+0x77 / ret +0x7c KeRequestTerminationThread(Thread)`** → `+0x7c XOR EAX,EAX` 로 **즉시 SUCCESS 반환** (끝나길 기다리지 않음).
- `KeRequestTerminationThread` 본문 (`.asm`, 224B): `+0x2e BTS.LOCK [RBX+0x78],0xf` (**`ThreadFlags.TerminationApcRequest`**, 이미 켜져 있으면 `+0x34 JC +0xc3` 으로 끝) · `+0x4c / ret +0x51 KiAcquireThreadLockRaiseToDpc` · `+0x5a TEST [RBX+0x74],0x4000` (`ApcQueueable`, 꺼져 있으면 큐에 안 넣고 건너뜀 `+0x66 JZ +0x8e`) · `+0x42 LEA RDI,[RBX+0x288]` (**`Thread.SchedulerApc`**, 스레드가 만들어질 때 이미 `KeInitializeApc` 해 둔 임베디드 `_KAPC`: `KeInitThread +0x11c~+0x152`, `NormalRoutine = KiSchedulerApc`) · `+0x68 CMP byte [RDI+0x52],0` (`Inserted`) · **`+0x78 / ret +0x7d KiInsertQueueApc`** · **`+0x89 / ret +0x8e KiSignalThreadForApc`** · `+0xa7 / ret +0xac KiExitDispatcher` · 큐에 넣었으면 `+0xb6 / ret +0xbb KeAlertThread(Thread, 0)` · `+0xbe / ret +0xc3 KeForceResumeThread` (멈춰 있던 스레드도 풀어 APC 가 돌게). 보안 스레드면 `+0xd7 VslRequestSecureThreadExit`.
- 그 APC 가 대상 스레드 안에서 도는 길: `KiSchedulerApc +0x41 MOV R14D,[RBX+0x78] / +0x45 AND R14D,0x8000` (`TerminationApcRequest` 읽음) → `+0x1c7 TEST [RBX+0x74],0x4000` → **`+0x1d4 LEA RAX,[KiSchedulerApcTerminate] / +0x1db MOV [RDI+0x20],RAX`** (APC 의 `KernelRoutine` 을 바꿔) → `+0x1f0 / ret +0x1f5 KiInsertQueueApc` → `+0x201 KiSignalThreadForApc`. 커널 APC 를 배달하는 `KiDeliverApc` 는 루틴이 `KiSchedulerApcTerminate` 면 곧장 부른다: `+0x443 LEA RAX,[KiSchedulerApcTerminate]` · `+0x459 CMP R13,RAX` · **`+0x46b / ret +0x470 CALL KiSchedulerApcTerminate`** (`KiInsertQueueApc +0xce~+0xd5` 도 같은 주소를 비교 — 큐 순서). `KiSchedulerApcTerminate` 본문: `+0x17 / ret +0x1c KiIsProcessTerminationRequested` (참이면 `+0x49 / ret +0x4e PsTerminateProcess`) → `+0x29 MOV ECX,[RCX+0x5d8]` (**`Thread.ExitStatus`**) → **`+0x2f / ret +0x34 PspExitThread`** → `+0x34 INT3`. (두 번 돌려 쓰는 순서 — `KiSchedulerApc` 가 `KernelRoutine` 을 갈아 끼우고 다시 넣는 모양 — 은 세 곳의 줄을 맞춘 것이고 한 번에 따라가 확인한 것은 아님 `[추측: 중간 한 걸음]`.)
- **→ 남 끝내기는 그 자리에서 `PspExitThread` 를 부르지 않는다.** 걸어 놓고 돌아간다. 실제 끝냄은 **대상 스레드가 다음에 커널 APC 를 받을 때, 대상 스레드 자신의 문맥에서** 일어난다 (그러니 TEB·스택 풀기도 대상 스레드 문맥).

**(b) `PspTerminateAllThreads(Process=RCX→RDI, 건너뛸스레드=RDX→R14, ExitStatus=R8D→R12D, 플래그=R9D→ESI)`, 654B.**

- 먼저 `+0x14 TEST [RCX+0x1f4],0x2000 / +0x2a JNZ +0x25c` (`Flags.BreakOnTermination`) → `+0x27a PspCatchCriticalBreak` (이름만).
- 스레드 목록을 **뒤에서 앞으로** (`+0x44 / ret +0x49 PspGetPreviousProcessThread`, 고리 `+0xdf / ret +0xe4`) 돌며 각 스레드 `RBP` 마다: `+0x93 / ret +0x98 ObfReferenceObjectWithTag` · **`+0x9e CMP RBP,R14 / +0xa1 JZ +0xd9`** (건너뛸 스레드 = 부른 쪽 스레드는 여기서 안 끝냄) · `+0xb3 / ret +0xb8 ExAcquireRundownProtection(Thread+0x588)` (실패하면 `BTS … 0xf` = `RundownFail`, `+0x1ea`) · **`+0xd4 / ret +0xd9 PspTerminateThreadByPointer(Thread, status, 직접=0)`** (= 위 (a) 의 **APC 길**; `+0xc8 INC R13D` 로 센다).
- 한 바퀴 끝 (`+0xf4 TEST R13D`): 끝낸 스레드가 있고 자기 프로세스면 `+0x1e0 / ret +0x1e5 KeFlushProcessWriteBuffers`. 둘째 바퀴 `+0xfd~+0x169` (`PsGetNextProcessThread` `+0x102/+0x13f`) 가 `ExReleaseRundownProtection` (`+0x12f`) 과 `ObfDereferenceObjectWithTag` (`+0x14e/+0x164`) 으로 뒷정리.
- 플래그 `BL & 2` 이면 (`+0x169 TEST BL,2 / +0x16c JNZ +0x20b`) 부른 쪽 스레드도 `+0x221 / ret +0x226 PspTerminateThreadByPointer(R14, status, 0)`.
- 끝맺음: `+0x172 CMP RDI,R15 / +0x175 JNZ +0x1c0` — 부른 쪽이 **같은 프로세스** 일 때 (`R15 = 부른 스레드의 프로세스 +0xb8`): 스레드를 하나도 못 찾은 표지 (`ESI==0x122`) 이면 `+0x23f / ret +0x244 PspRundownSingleProcess(Process, 0)` 를 곧장 돌리고 (실패하면 `0xc000010a` `STATUS_PROCESS_IS_TERMINATING`). **다른 프로세스** 면 `ExitStatus==0x40010004` (`DBG_TERMINATE_PROCESS`) 일 때 `+0x1ce DbgkClearProcessDebugObject`.
- **부르는 쪽**: `NtTerminateProcess +0x243 / ret +0x248` (`R9D=0`, 자기 프로세스 끝내기) · `PspTerminateProcess +0xf2 / ret +0xf7`. (`NtTerminateProcess` 가 자기 프로세스면 이어서 자기 스레드도 `+0x161 PspExitThread` 로 끝낸다 — `+0x147 OR.LOCK [RSI+0x5a0],1` 로 `Terminated` 먼저 켜고.)
- 「남은 스레드는 APC 로 각자 끝냄」 이라서 이 함수가 돌아와도 그 스레드들은 아직 안 끝났을 수 있다. 마지막으로 끝나는 쪽이 단계 0 의 대기 (질문 4) 로 전부 끝났는지 기다린다.

**지금 우리 (PE_emulator):** "`PspTerminateThreadByPointer` 에 다른 스레드 먼저, 지금 스레드 마지막" (197 메모) — 순서 (남 → 자기) 는 **맞다.** 다만 진짜는 "남에게는 즉시 끝내기 대신 종료 요청 (APC) 만 건다" 이다. 에뮬레이터가 남을 곧바로 `bTerminated` 로 끝내도 결과 (다시 안 돈다) 는 같지만, **끝나는 시점이 "대상 스레드가 다음에 돌 때"** 가 맞는 모양이다 `[추측: 어디까지 흉내 낼지]`.

### 6. `ObWaitForSingleObject` 가 스레드 객체를 받는 길 — `DefaultObject` 오프셋 (스레드는 0 → 본체가 곧 `Header`)

- `+0x47 / ret +0x4c ObpReferenceObjectByHandleWithTag(핸들, EDX=0x100000 (SYNCHRONIZE), 형식 R8D=0, 모드 R9D=DL, 태그 0x7457624f)` → 본체 포인터 `[RSP+0x48]` → `+0x56 MOV RDI,…` .
- 형식 찾기: `+0x5b LEA RAX,[RDI-0x30]` (객체 머리) · `+0x5f SHR RAX,8` · `+0x66 MOVZX EAX,byte [RDI-0x18]` (`TypeIndex`) · `+0x6d ObHeaderCookie` XOR → `+0x7e MOV R8,[ObTypeIndexTable + idx*8]`.
- **`+0x82 MOV RCX,[R8+0x20]`** = `OBJECT_TYPE.DefaultObject`. 그다음:
  - `+0x86 TEST CL,1 / +0x89 JNZ +0xf8`: 비트 0 = 특수 꼴 (`+0xf8~+0x134`: 비트 1 이 서 있으면 `WaitObjectFlagMask/Offset/PointerOffset` (`OBJECT_TYPE+0xb0/+0xb4/+0xb6`) 로 포인터 또는 `-3`+오프셋을 고름, 아니면 본체 안에 저장된 포인터 `[RCX+RDI-1]`).
  - 비트 0 이 꺼져 있으면 `+0x8b TEST RCX,RCX / +0x8e JS +0x93`: **음수 = 커널 안 절대 포인터 그대로**, **0 이상 = `+0x90 ADD RCX,RDI` (본체 + 오프셋)**.
  - `+0x93 CMP [ExCrossVmMutantObjectType],R8 / +0x9a JZ +0x139` — 교차 VM 뮤턴트는 거절 (`0xc0000024`).
- **스레드 형식은 `DefaultObject == 0` 이라 `RCX = 0 + RDI` = 본체 = `_ETHREAD` = `_KTHREAD.Header` (둘 다 `+0x0`).** 근거 `[정적, 읽은 줄 둘로 유추]`: ① `PspInitPhase0 +0x504~+0x569` 의 스레드 형식 만들기는 `ObCreateObjectType` 로 가는데, 그 22바이트 감싸기는 `ObCreateObjectType +0x9 XOR R9D,R9D` 로 `ObCreateObjectTypeEx` 의 `DefaultObject` 인자(`param_4`)를 **0** 으로 넘기고, ② `ObCreateObjectTypeEx` 는 `UseDefaultObject` 비트 (`ObjectTypeFlags` 비트 2, `0x4`) 가 서 있을 때만 `&ObpDefaultObject` 를 쓰는데 (`.c` 의 `(*(byte*)(lVar4+0x72)&4)!=0` → `puVar13 = &ObpDefaultObject`), ③ 스레드 형식 초기화 값에는 이 비트가 안 켜진다 (`PspInitPhase0 +0x479 OR [RSP+0x62],0xc2` 는 프로세스 형식 몫, `+0x50b OR [RSP+0x62],R12B` 의 `R12B` 는 `+0x26f MOV R12D,0x80` 뒤 안 바뀐 `0x80` 으로 읽힘 — 비트 2 아님). **동적으로 값을 확인할 것** (아래).
- 그다음 `+0xb5 MOV EDX,6 (UserRequest)` · `+0xba / ret +0xbf KeWaitForSingleObject(Object=RCX, …)` (sast/73 질문 1).
- `+0xda ObfDereferenceObjectWithTag` 로 참조 하나 내림 (기다림이 끝난 뒤).

**지금 우리 (PE_emulator):** "기다림 도우미가 스레드 객체를 받는다 (끝났으면 신호)" (192 메모) — **맞다.** 진짜는 핸들 → 객체 형식 → `DefaultObject` 로 "기다릴 디스패처 헤더" 를 고르고, 스레드는 본체 앞머리가 곧 헤더라서 같은 `KeWaitForSingleObject` 하나로 이벤트·뮤턴트·스레드가 다 처리된다.

### 7. `PspThreadDelete` — 객체가 지워질 때. **TEB·유저 스택은 여기서 안 푼다.**

- 부르는 쪽 (간접): `PspInitPhase0 +0x510 LEA RAX,[PspThreadDelete]` / `+0x517 MOV [RBP-0x58],RAX` — **스레드 객체 형식의 삭제 루틴 (`OBJECT_TYPE_INITIALIZER.DeleteProcedure`, `+0x48`) 으로 등록** (같은 자리 `+0x546 MOV [RBP-0x74],0x798` = `sizeof(_ETHREAD)`). `refs` 도 이 두 줄 말고 데이터 참조뿐. 불리는 때 = 객체 참조 수가 0 이 될 때 (`ObfDereferenceObject*` 안의 객체 삭제 길 `[추측]`).
- **`MmDeleteTeb`·`ZwFreeVirtualMemory`·`NtFreeVirtualMemory` 를 부르는 줄이 없다** (`_calls.tsv` 의 `PspThreadDelete` 호출 목록에 없음). → TEB·유저 스택은 질문 2 의 `PspExitThread` (끝나는 순간) 가 푼다. 겹치지 않는다.
- 여기서 하는 일 (줄기만):
  - `+0x17 / ret +0x1c KeCleanupThreadState(Thread)`.
  - 새는지 검사 (bugcheck): `+0x1c~+0x57` (`KernelStackReference +0x5ec` 가 0 이어야 `0x94`, `IoBoostCount +0x5e0`·`IoQoSBoostCount +0x5e4`·`IoQoSThrottleCount +0x5e8` 가 0 이어야 `0x13c`) · `+0x57 LEA RAX,[RDI+0x6d0]` (`DisownedOwnerEntryListHead` 비어야, `0x1c6`).
  - **커널 스택 정리**: `+0x6a CMP [RDI+0x28],R13 / +0x6e JNZ +0x452` (`KTHREAD.InitialStack` 이 아직 남아 있으면) → `+0x45f / ret +0x464 KeEnumerateKernelStackSegments(Thread, 0, 콜백)` (`PspReaper +0x55` 에도 같은 호출 + `+0x5a InitialStack=0`: 청소부가 먼저 해서 보통 여기선 안 탐, 청소부를 못 거친 스레드 몫 `[추측]`).
  - **TID 반납**: `+0x81 CMP [RDI+0x510],R13` → `+0x1cd / ret +0x1d2 ExMapHandleToPointer(PspCidTable, Cid.UniqueThread)` · `+0x1e8 / ret +0x1ed ExDestroyHandle`.
  - `+0xa2 MOV RCX,[RDI+0x6a0]` (`ThreadName`) → `+0x49f ExFreePoolWithTag`. `+0x8e` `ActiveImpersonationInfo` 면 `+0x469 MOV RCX,[RDI+0x538]` (`ClientSecurity`) → `+0x474 ObfDereferenceObject`, `+0x479` `AdjustedClientToken (+0x648)` 도 `+0x489 ObfDereferenceObject` 후 0 (이름만).
  - **프로세스 목록에서 빼기 + 통계 합치기** (질문 2(d) 표): `+0x116~+0x396`.
  - `+0x39b TEST [RDI+0x5a0],0x100000 (PicoNotifyExit)` → 간접 `PspPicoProviderRoutines+0x10` (`+0x51f`).
  - **프로세스 참조 내림**: `+0x3ab CMP RBP,[PsIdleProcess] / +0x3b2 JNZ +0x3cd` → `+0x3d5 / ret +0x3da ObfDereferenceObjectWithTag(Process, 'PsCr')`.
- **청소부 (`PspReaper`)** 는 이보다 앞이다: `KeTerminateThread +0x213 ExQueueWorkItemEx(&PsReaperWorkItem …)` 가 작업 항목을 돌리고 (`PspInitPhase0 +0x9ae LEA RAX,[PspReaper]` / `+0x9b5 MOV [PsReaperWorkItem+0x10],RAX` 로 루틴 등록), `PspReaper` 는 `+0x16 XCHG [PsReaperListHead],1` 로 목록을 통째로 받아 스레드마다 **`+0x27 / ret +0x2c KeDeleteThread`** → `+0x2c [RDI+0x620]` (`ReservedForSynchTracking`) 풀 해제 `+0x9e` → `+0x3b XADD.LOCK [RDI+0x5ec],-1` (`KernelStackReference` 를 내림, 이전 값이 1 이면 `+0x55 KeEnumerateKernelStackSegments` = **커널 스택 해제**, `+0x5a MOV [RDI+0x28],RSI(0)` = `InitialStack` 비움) → **`+0x69 / ret +0x6e ObfDereferenceObjectWithTag(Thread, 'Dflt')`** (스레드 만들 때 둔 참조 하나를 내림). 참조가 그걸로 0 이 되면 `PspThreadDelete`. **test.exe 는 `CreateThread` 로 받은 핸들 `t` 를 닫지 않으므로** (`sample/test.c:34-35`, 프로세스 끝까지) 핸들 참조가 남는 동안 Worker 의 `ETHREAD` 는 지워지지 않는다 `[소스+추측]` — 곧 `PspThreadDelete` 는 프로세스 끝(핸들 정리) 쪽에서 불리는 모양이다. 동적으로 볼 것.

**지금 우리 (PE_emulator):** TEB·스택을 "누가 푸나" — **`PspExitThread` 쪽이 맞다** (PE_emulator 메모와 맞음, 객체 삭제 쪽은 아님). 객체 삭제 (`PspThreadDelete`) 는 핸들·참조 계산이 있어야 하는 길이라 지금은 건너뛰어도 된다 `[추측: 에뮬레이터 범위]`.

## 순서 한 장 — `PspExitThread` → `KeTerminateThread` (Worker 같은 "마지막이 아닌 유저 스레드")

| 순 | 한 일 | 줄 |
|---|---|---|
| 1 | `ExitStatus` 적기 | `PspExitThread +0x100` |
| 2 | 스레드 끝 통지 | `+0x142 PspCallThreadNotifyRoutines` |
| 3 | 프로세스 락 → **`ActiveThreads -1`** → 0 아니면 `LastThreadExitStatus` 적고 락 풀기 | `+0x18e` · `+0x1cc` · `+0x1d9` |
| 4 | 디버거·LPC 끝 메시지·Win32k 콜아웃·FLS | `+0x7b9/+0x27dbac` · `+0x6d0/+0x273` · `+0x2f1` · `+0x336` |
| 5 | I/O 취소·타이머·뮤턴트 놓기 | `+0x340~+0x363` |
| 6 | **유저 스택 풀기** (`ZwFreeVirtualMemory`) → 디버그 핸들 닫기 → **`MmDeleteTeb`** | `+0x3f2` · `+0x40f` · `+0x4b8` |
| 7 | `ExitTime` | `+0x4bd` |
| 8 | **APC 큐 비우기** | `+0x4e3 KeRundownApcQueues` |
| 9 | **`KeTerminateThread`** → KPROCESS 목록에서 빼기 → **`SignalState=1`** → 기다리는 모두 깨움 → `State=4` → 청소부 목록에 얹기 → **`KiSwapThread`** | `+0x54c` → `KeTerminateThread +0x116` · `+0x16c` · `+0x19b` · `+0x1b0` · `+0x1d3` · `+0x24f` |

## PE_emulator 가 흉내 낼 최소 규칙 [정적 → 에뮬레이터]

1. **`NtTerminateThread(핸들, status)`**: 핸들 `0` + (프로세스 `ActiveThreads == 1`) 이면 `0xc00000db` 로 돌려보냄. (`-2`·자기 핸들은 이 검사 없음.) 자기면 `Terminated` 켜고 `PspExitThread(status)` — 돌아오지 않음.
2. **남이면 `PspExitThread` 를 안 부르고** `Terminated` 만 켜고 (이미 켜져 있으면 `ExitStatus` 안 씀), `ExitStatus` 적고, "종료 요청" 만 걸고 `SUCCESS` 로 돌아옴. 실제 끝냄은 **그 스레드가 다음에 돌 때, 그 스레드의 문맥에서** `PspExitThread(Thread.ExitStatus)`.
3. **`PspExitThread` 순서**: ① `ExitStatus` 적기 → ② 스레드 끝 통지 → ③ **`ActiveThreads -= 1`**; 0 이면 "마지막" (`Flags |= ProcessDelete`, 단계 0: 프로세스 `ExitStatus` 정하기·다른 스레드 신호 기다리기), 아니면 `LastThreadExitStatus = status` → ④ (유저 스레드이고 마지막이 아니면) **TEB 블록** → ⑤ `ExitTime` → ⑥ APC 큐 비우기 → ⑦ (마지막이면 단계 3: `PspExitProcess` + `PspRundownSingleProcess` — 이게 주소 공간째 치움) → ⑧ **`KeTerminateThread`**.
4. **TEB·유저 스택은 커널이, 끝나는 스레드 자신이, `KeTerminateThread` 보다 앞에서** 푼다. 순서: 유저 스택 (`TEB.DeallocationStack` 를 그때 TEB 에서 읽어 예약 영역 통째로 `MEM_RELEASE`) → `DbgSsReserved` 둘째 칸 핸들 닫기 → `MmDeleteTeb` (TEB 쪽 하위 할당 영역에 돌려놓음). 조건: `Teb≠0`, 시스템 스레드 아님, 프로세스가 `ProcessDelete/ProcessSelfDelete` 아님, 스택은 `ThreadInserted` 일 때만. **`SameTebFlags` 는 안 본다.** ntdll `RtlExitUserThread` 는 스택을 안 품 (맞음).
5. **마지막 스레드는 TEB·스택을 따로 안 푼다.** 프로세스 주소 공간째 (`MmCleanProcessAddressSpace`) 정리하는 길로 간다 (프로세스 쪽 규칙; 지금 에뮬레이터의 process_exit 모양과 맞음).
6. **기다리던 쪽 깨우기 = `KeTerminateThread` 안에서 한 번에**: 스레드 객체 `SignalState = 1` (이미 1 이어도), 기다림 목록 **전부** (`WaitType==1` 은 `WaitStatus = WaitKey` 로, 전부기다림은 `0x100`), 신호는 **그대로 남음** (수동 이벤트 꼴), 목록은 비움. 깨운 스레드는 준비 줄로 (sast/73 질문 2(c)). 깨난 쪽은 `WaitStatus` 를 그대로 돌려줌 (다시 안 묻는다, sast/73 질문 1(c)) — test.exe 메인은 `0`.
7. **끝난 스레드는 다시 돌지 않음**: `State = Terminated`, 어떤 준비 줄에도 안 넣고 `KiSwapThread` 로 다른 스레드를 골라 넘어감 (sast/71). 커널 스택 해제·객체 참조 내림은 나중 일 (청소부 `PspReaper`), `PspThreadDelete` (프로세스 목록에서 빼기·TID 반납) 은 참조가 0 일 때 — 에뮬레이터는 건너뛰어도 됨.
8. **스레드 목록 둘**: `KeTerminateThread` 가 `KPROCESS` 쪽을 빼고 (`+0x116`), `EPROCESS` 쪽 (`+0x370`) 은 객체 삭제까지 남는다. "끝난 스레드를 목록에서 바로 지운다" 로 만들었다면 `EPROCESS` 쪽은 **틀리다.**

## 동적으로 확인할 것 (Q104 에 붙임, 이번 묶음엔 VM 안 함)

- `PspExitThread +0x18e` 직전·직후 `[Process+0x380]` (`ActiveThreads`) — Worker 가 끝날 때 2 → 1 이고 `R12B` (마지막 표지) = 0 인가. 메인이 끝날 때는 1 → 0 이고 `+0x6a1 PspExitLastThread` 로 가는가.
- 질문 2(c): Worker 끝날 때 `nt!PspExitThread+0x3f2` (`ZwFreeVirtualMemory`) 가 불리고, 첫 인자 `-1` / 두 번째 인자가 가리키는 값이 `TEB+0x1478` (= `DeallocationStack`) 과 같은가. `ThreadInserted` (`[Thread+0x5a0] & 2`) 가 켜져 있는가. 같은 스레드 `nt!MmDeleteTeb` (`+0x4b8`) 의 `Teb` 가 Worker TEB 인가. **메인 스레드(마지막) 가 끝날 때는 이 둘이 불리지 않는가** (`+0x3a8 JNZ +0x4bd`).
- `TEB+0x16a8` (`DbgSsReserved` 둘째 칸) 가 test.exe 에서 0 인가 (`+0x40f ObCloseHandle` 가 불리나).
- `KeTerminateThread +0x16c` 직후 `[Thread+4]` = 1, `+0x19b KiTryUnwaitThread` 호출 수·`R8` (WaitKey) = 0, 메인이 `WaitForSingleObject(t)` 에서 `WaitStatus=0` 으로 깨는가. `+0x1b0` State=4, `+0x24f KiSwapThread` 후 Worker 가 다시 안 돌아오는가.
- `PspReaper` 가 언제 도는가 (`bp nt!PspReaper`), `nt!PspThreadDelete` 가 Worker 에 대해 **프로세스 끝 가까이에만** 불리는가 (test.exe 가 `t` 를 안 닫음). `PspThreadDelete` 의 `+0x374` 에서 목록에서 빠지는 시점.
- 질문 6: `dt nt!_OBJECT_TYPE poi(nt!PsThreadType) DefaultObject` = 0 인가 (`ObWaitForSingleObject +0x82` 에서 `RCX` 확인). `TypeInfo.UseDefaultObject` 비트도.
- 질문 5(a): 남 끝내기 (이 test.exe 에는 없음, `NtTerminateProcess` 길에서 볼 수 있음) — `KeRequestTerminationThread` → `KiSchedulerApcTerminate` 사이 중간 한 걸음 (`KiSchedulerApc +0x1d4` 가 `KernelRoutine` 을 바꾸는가).
- 같은 시각에 두 스레드가 `ActiveThreads` 를 내리는 경합 (CPU 둘 이상): 락 안이라 순서는 정해지지만 누가 마지막인지는 런타임.

## 판정

**분석성공.** 요청서 7개 질문 모두 줄 근거와 함께 답했다. 따로 못 정한 것: (1) 질문 5(a) 의 `KiSchedulerApc` ↔ `KiDeliverApc` 사이 중간 한 걸음 (`[추측]` 표시), (2) 질문 6 스레드 형식 `DefaultObject == 0` 은 읽은 줄 셋으로 유추 (동적 확인 항목에 넣음), (3) `PspExitLastThread` 단계 3 안 `PspExitProcess`·`PspRundownSingleProcess` 본문은 이름만. 버전 대조는 일치.
