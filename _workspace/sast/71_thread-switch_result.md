# 71 thread-switch — 정적 분석 결과

- 요청서: `_workspace/sast/71_thread-switch.md` (Q141b1, PE_emulator 요청 25 ㄴ-1 — 스레드 넘기기 본문)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도: `run_ghidra.sh map ntoskrnl.exe KiSwapThread` 만들었다 → `ms/disassemble/ntoskrnl/_map/KiSwapThread_{edges,nodes}.tsv`, `_summary.txt` (2,938함수, 5초). `NtYieldExecution` 뿌리 지도는 안 만들었다 (callee 가 1개라 필요 없음).
- 뽑은 함수 (`NtYieldExecution` 만 depth 1, 나머지 depth 0, 새로 **21개**): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/` 에 `.c`/`.asm`. 이 중 `KiFindRescheduleContextEntryForPrcb` (48B) 는 같이 딸려 나온 것.
  - 요청서 대상: `KiSwapThread`(1637B) `KiSearchForNewThread`(675B) `KiSwapContext`(214B) `KiInsertDeferredReadyList`(90B) `KiDeferredReadyThread`(166B) `KiDeferredReadySingleThread`(9230B) `NtYieldExecution`(16B, depth 1 → callee `Ordinal_6` = PDB `KeYieldExecution` 1290B 가 같이 나옴. 꼬리 호출이 아니라 보통 CALL). (`SwapContext.asm`/`.c` 는 이미 있었다.)
  - **요청서 범위를 넓힌 곳 (이유)**: 일이 두 단계 아래에서 일어난다. `KiSearchForNewThread` 는 NextThread 를 보고, 없으면 `KiSearchForNewThreadsForRescheduleContext`(4580B) 에 맡긴다. 지도에서 이 함수는 "공용 도구 (caller 22곳)" 라 안 펼쳐져 있어서 이름으로 직접 뽑았다. 같이: `KiCommitRescheduleContext`·`KiCommitRescheduleContextEntry`·`KiStartRescheduleContext`·`KiSearchForNewThreadsWithinRestOfSystem`·`KiStartReadyQueueEnumeratorForRescheduleContext`(yield/우선순위 마스크)·`KiStartReadyListEnumerator`·`KiAdvanceReadyListEnumerator`·`KiQueueReadyThread`(yield 가 현재 스레드를 목록에 넣는 곳)·`KiCanLocalReadyThreadBeScheduledToRescheduleContextEntry`·`KiReadyDeferredReadyList`·`KiUpdatePriorityMatrixForRunningTransition`. `KiSwapThread`·`KiSearchForNewThread` 를 depth 1 로 안 뽑은 이유: callee 가 락·ETW·통계 helper 40여 개라서. 줄기(고르기·넘기기) 위의 것만 이름으로 뽑았다.
  - 안 읽은 것 (이름만): `KiAbProcessPre/PostContextSwitch` · `KiCaptureTotalCyclesCurrentThread` · `KiUpdateGroupSchedulingRank` · ETW 함수들 · 락 함수들 (`KiAcquire/ReleasePrcbLocksForIsolationUnit`) · `KiSearchForNewThreadsWithinL0/L1SearchContext` (다른 CPU 훔치기 본체, `KiSearchForNewThreadsOnTarget` 7609B 쪽) · `KiComputeNewPriority` · `KiUpdateThreadPriority` · 헤테로(`Hetero`) 갈래.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` 를 `_calls.tsv` 에서 다시 확인.

## 스크립트 문제

없다. 참고만:

- `_calls.tsv` 는 **뽑은 함수가 부르는 것**만 있다. "누가 `KiSwapThread` 를 부르나" 는 `_calls.tsv` 에서 안 나오고 `run_ghidra.sh refs ntoskrnl.exe KiSwapThread` 로 봐야 한다 (`ms/disassemble/ntoskrnl/_refs/KiSwapThread.tsv`). 스크립트 문제는 아님.
- 이름 주의: PDB 의 `KeYieldExecution` 을 Ghidra 가 **`Ordinal_6`** 로 잡았다 (rva `0x2137a0`, 내보내기 번호 6). `.asm`/`.c` 파일 이름은 `Ordinal_6`. 문서에는 PDB 이름 `KeYieldExecution` 을 쓴다 (`pdb_aliases.py --where ntoskrnl.exe KeYieldExecution` 로 확인).

## 읽는 법 (표기)

- 커널 PDB 는 공개 심볼이라 **`_KPRCB` 가 `+0x6d0` 에서 잘려 있다** (`pdb_struct.py ntoskrnl.exe _KPRCB` → sizeof `0x700`, 이름 있는 칸은 `+0x6d0` 까지). 그래서 아래 `PRCB+0x83d8`·`+0x8440`·`+0x8e48` 같은 칸은 **이름이 없다**. 오프셋·모양은 `[정적]`, 붙인 이름 (ReadySummary 등) 은 `[추측: 이름]`. 이름 근거는 같은 모양의 공개 구조체 `_KSHARED_READY_QUEUE` (`ReadySummary +0x8`, `ReadyListHead +0x40`) 와 `_KI_RESCHEDULE_CONTEXT_ENTRY` (`pdb_struct.py` 로 봄).
- 공개된 칸: `_KPRCB` `CurrentThread +0x8` · `NextThread +0x10` · `IdleThread +0x18` · `IdleState +0x23` · `Number +0x24` · `PrcbLock +0x30` · `SchedulerSubNode +0xc0` · `GroupSetMember +0xc8`. `_KTHREAD` `QuantumTarget +0x20` · `ThreadLock +0x40` · `CycleTime +0x48` · `CurrentRunTime +0x50` · `ExpectedRunTime +0x54` · `KernelStack +0x58` · `Running +0x71` · `ReadyTime +0x84` · `ApcState.Process +0xb8` · `ApcState.KernelApcPending +0xc1` · `Priority +0xc3` · `WaitStatus +0xc8` · `WaitListEntry +0xd8` (준비 목록에 꿰는 고리도 이 칸) · `State +0x184` · `WaitIrql +0x186` · `Preempted +0x235` · `AdjustReason +0x236` · `WaitReason +0x283` · `AbWaitObject +0x3d0`.
- `State` 값 (KTHREAD_STATE): 1 Ready, 2 Running, 3 Standby, 5 Waiting, 7 DeferredReady — 코드에서 쓰는 숫자만 적었다.
- `[정적]` 은 "코드에 이렇게 쓰여 있다". 실행 때 이 길로 간다는 뜻이 아니다.

## 한눈에 (그림)

```
KiSwapThread(Thread=RCX, Prcb=RDX, R8=out포인터)        ← KiCommitThreadWait +0x220 등에서 부름
  │ +0x14  PRCB+0x2d08 != 0 이면 먼저 KiProcessThreadWaitList (+0x516)
  │ +0x35  KiAbProcessPreContextSwitch
  │ +0x45~0x5a  ExpectedRunTime = (Expected + CurrentRun) / 2 ; CurrentRunTime = 0
  │ +0x6d~0xa5  PRCB 락 잡기 (스핀)
  ├─ +0xdc  KiSearchForNewThread(Prcb, 0, &락핸들) ──→ 다음 스레드 RBP
  │            ├ +0x20  NextThread(PRCB+0x10) 먼저 봄. 있고 Idle 이 아니면 그대로 돌려줌
  │            ├ +0x69  KiSearchForNewThreadsForRescheduleContext  (우선순위 높은 순, 앞에서부터)
  │            ├ +0x7f  KiCommitRescheduleContext  → KiCommitRescheduleContextEntry 가 NextThread 칸에 씀
  │            ├ +0xdf  못 찾으면 KiSearchForNewThreadsWithinRestOfSystem (다른 CPU 훔치기)
  │            └ 끝까지 없으면 NextThread = IdleThread(PRCB+0x18)
  ├─ +0x179 NextThread=0, +0x17d CurrentThread=새 스레드, +0x190 새 스레드 State=2(Running)
  ├─ +0x21f 새 스레드 == 지금 스레드? → 넘기기 안 하고 +0x3ba 쪽으로
  ├─ +0x243 KiSwapContext(old, new, WaitIrql)  ──→ SwapContext (스택 바꿈 / 필요하면 CR3 바꿈)
  │             … (다시 이 스레드 차례가 오면 여기서 이어서)
  ├─ +0x24e KiAbProcessPostContextSwitch
  ├─ +0x25c RDI = [Thread+0xc8] (WaitStatus)  ← 돌려줄 값
  ├─ +0x2ee APC 대기 중이면 IRQL 1 로 낮춰 KiDeliverApc
  └─ +0x28e IRQL 복원, +0x29a RAX=RDI, +0x2ac RET
```

## 질문별 답 [정적]

### 1. `KiSwapThread`

**(a) 부르는 순서와 다음 스레드 고르는 곳** — `ms/disassemble/ntoskrnl/KiSwapThread.asm`, `_calls.tsv`

본 줄기 (오프셋은 call 자리 / ret 자리):

| 순서 | call / ret | 부르는 것 | 하는 일 (코드에서 읽은 것) |
|---|---|---|---|
| 0 | `+0x14` `CMP [RDX+0x2d08],0` → `+0x2a JNZ +0x508` → `+0x516 / +0x51b` | `KiProcessThreadWaitList` (EDX=1, R8=0, R9=2) | PRCB+0x2d08 칸이 0 이 아니면 먼저 이걸 처리하고 `+0x30` 으로 되돌아온다. 이름 없는 칸이라 뜻은 `[추측: 대기 목록 머리]` |
| 1 | `+0x35 / +0x3a` | `KiAbProcessPreContextSwitch` | 이름만 (run52 caller 목록에 있던 것) |
| 2 | `+0x40 / +0x45` | `KiCaptureTotalCyclesCurrentThread` | 이름만 |
| — | `+0x45~+0x5a` | (인라인) | `ExpectedRunTime(+0x54) = ExpectedRunTime/2 + CurrentRunTime(+0x50)/2`, `CurrentRunTime = 0` |
| — | `+0x6d~+0xa5` | (인라인) | PRCB 락 잡기. `[PRCB+0x8e58]` 가 락 묶음 (첫 바이트 = 개수, `+8` 부터 PRCB 포인터), 각 PRCB 의 `PrcbLock(+0x30)` 비트 0 을 `BTS.LOCK` 으로 스핀 |
| 3 | `+0xc2 / +0xc7` | `KiUpdateGroupSchedulingRank` | 이름만. 결과 칸 `[RSP+0x78]` 이 0 이 아니면 `+0x575` (락 풀고 `KiProcessScbTracingList` 부르고 다시 잡음) |
| **4** | **`+0xdc / +0xe1`** | **`KiSearchForNewThread`** (RCX=Prcb, EDX=0, R8=`&[RSP+0x70]` 락 핸들) | **다음 스레드를 고른다. 결과 RAX → RBP** |
| 5 | `+0xe4 / +0xe9` | `Feature_HeteroRescheduleFixes__private_IsEnabledNoReportingNoInline` | 기능 켜짐 표시 확인. 꺼져 있으면 `+0x40cff8` 쪽 (헤테로 갈래) 으로 갔다가 `+0x174` 로 되돌아옴. 본 줄기에는 영향 없음 |
| 6 | `+0x243 / +0x248` | `KiSwapContext` | 실제로 바꾸는 곳 → 1(b) |
| 7 | `+0x24e / +0x253` | `KiAbProcessPostContextSwitch` | 이름만 |
| 8 | `+0x2ee / +0x2f3` | `KiDeliverApc` (0,0,0) | 1(c) |
| (곁가지) | `+0x372 / +0x377` `KiCancelTimer` · `+0x3d3` `KiResumeThreadCycleAccumulation` · `+0x4f3` `KiSwapContext`(VP 백킹 스레드) · `+0x5cd` `KiInsertDeferredPreemptionApc` · ETW (`+0x47c` `+0x48e` `+0x4bf` `+0x537` `+0x56b`) · `+0x344~+0x35b` 헤테로 | | 줄기 밖. 이름만 |

**다음 스레드를 고르는 줄**:

- **`KiSwapThread` 자신은 `NextThread` 를 안 본다.** 락을 잡고 `+0xdc` 에서 곧바로 `KiSearchForNewThread` 를 부른다. `[R14+0x10]` (NextThread) 를 읽는 줄은 `+0x179` (0 으로 **씀**) 와 `+0x360` (헤테로 갈래에서 락을 다시 잡은 뒤 **읽음**) 뿐이다.
- **`NextThread` 를 먼저 보는 곳은 `KiSearchForNewThread` 안**이다: `KiSearchForNewThread.asm +0x20 MOV RBX,[RCX+0x10]` → `+0x2a TEST RBX,RBX` → `+0x2d JNZ +0x110` → `+0x110 CMP RBX,[RCX+0x18]` (IdleThread 와 같은지). NextThread 가 있고 **Idle 이 아니면** `+0x11f KiDowngradeIsolationUnitLockHandle` 만 하고 그 값을 그대로 돌려준다 (`+0xec` 뒤 `+0x107 RET`). NextThread 가 없거나 Idle 이면 `+0x33` 부터 찾기 시작한다. → 질문 2.
- 고른 뒤 `KiSwapThread` 가 하는 바꿔치기: `+0x179 MOV [R14+0x10],R15` (NextThread = 0), `+0x17d MOV [R14+0x8],RBP` (**CurrentThread = 새 스레드**), `+0x181~+0x190` 새 스레드 `State(+0x184)` 가 1(Ready) 이면 `ReadyTime(+0x84)` 에 기다린 시간을 더하고 (`+0x305~+0x317`), `State = 2` (Running). `+0x197~+0x2f8` 서브노드 `StealableStandbyThreads(+0x190)` 에서 이 CPU 비트를 지운다. `+0x1b5~+0x210` PRCB 락들을 푼다 (`AND.LOCK [lock+0x30], 0`).
- **새 스레드 == 지금 스레드** (`+0x21f CMP R13,RBP` → `+0x222 JZ +0x3ba`) 이면 문맥 전환을 안 한다: `+0x3d3 KiResumeThreadCycleAccumulation`, `+0x3db KiAbProcessPostContextSwitch` 만 하고 `+0x25c` (돌아온 뒤 처리) 로 간다.

**(b) 실제로 바꾸는 줄** — `KiSwapThread +0x243 CALL KiSwapContext` (ret `+0x248`). 인자: RCX = 지금 스레드 (R13), RDX = 새 스레드 (RBP), R8 = 지금 스레드의 `WaitIrql(+0x186)` (`+0x212 MOVZX EBX,[R13+0x186]` → `+0x239 MOVZX R8D,BL`). 다른 호출 자리: `+0x4f3` (ret `+0x4f8`) — 지금 스레드 `[R13+0x78]` 의 `VpBackingThread(0x400000)` 비트가 서 있고 `State==5`(Waiting) 일 때 (`+0x22b~+0x233`, `+0x4c9`). 본 줄기는 `+0x243`.
  - 반환값 AL → `+0x24b MOVZX ESI,AL` (APC 대기 표시, 5번 질문).

**(c) 돌아온 뒤** — (`KiSwapContext` 가 돌아왔다는 건 나중에 이 스레드 차례가 와서 다시 이 스택으로 들어왔다는 뜻.)

- `+0x24e / +0x253 KiAbProcessPostContextSwitch(R13)`.
- **돌려줄 값 = `WaitStatus`**: `+0x25c MOV RDI,[R13+0xc8]` (`_KTHREAD.WaitStatus`). 마지막 `+0x29a MOV RAX,RDI` → `+0x2ac RET`. 칸 이름·오프셋만 적는다 (누가 어떤 값을 쓰는지는 Q141c).
- `+0x263 BTR [R13+0x74],9` (`MiscFlags.TimerActive`). 켜져 있었으면 (`JC +0x559`) `+0x369~+0x377 KiCancelTimer(&Thread->Timer(+0x100), 1)`.
- **세 번째 인자 R8 (R12)**: 0 이 아니면 `+0x2bd MOV RAX,[R13+0x3d0]` · `+0x2c4 MOV [R12],RAX` — `AbWaitObject(+0x3d0)` 를 출력 칸에 적어 준다 (`+0x26f TEST R12,R12`).
- **APC 처리**: KiSwapContext 반환값 (`SIL`) 이 0 이 아니면 (`+0x274 TEST SIL,SIL → +0x277 JNZ +0x2ca`): IRQL 을 1 (APC_LEVEL) 로 낮추고 (`+0x2d3~+0x2e3`), `+0x2ee KiDeliverApc(0,0,0)` (ret `+0x2f3`).
- 마무리: `+0x279~+0x28e` IRQL 을 `WaitIrql`(EBX) 로 복원 (`MOV CR8,RBX`), 저장 레지스터 복원, `RET`.

**누가 `KiSwapThread` 를 부르나** (`run_ghidra.sh refs`, `_refs/KiSwapThread.tsv`): `KiCommitThreadWait +0x220` (기다림 → Q141c), `KiInSwapSingleProcess +0x120`, `KeTerminateThread +0x24f` (ret `+0x254`), `KiExecuteDpcDelegate +0x8b`, 그리고 데이터 참조 2곳 (`0xe786c`, 언윈드 정보). 퀀텀 끝·선점은 `KiSwapThread` 를 거치지 않고 `KiQuantumEnd`·`KiDispatchInterrupt` 쪽에서 곧바로 `KiSwapContext` 를 부르는 길일 가능성이 있다 (`KiCheckForThreadDispatch +0x10e`, `KiHandleDeferredPreemption +0x297` 가 `KiSwapContext` 를 직접 부름 — 지도 edges) — 다음 바퀴 Q141b2 에서 볼 것.

**PE_emulator 와 비교**: "스레드 목록에서 다음 실행 가능한 걸 고름 (우선순위 없음)" — **모자라거나 틀리다.** 진짜는 ① PRCB 의 `NextThread`(미리 정해 둔 스레드) 가 있으면 그것 ② 없으면 우선순위 높은 순으로 준비 목록 ③ 그래도 없으면 다른 CPU 에서 훔치기 ④ 그래도 없으면 `IdleThread`. 돌려주는 값이 `WaitStatus` 인 것, 같은 스레드가 다시 뽑히면 문맥 전환을 안 하는 것은 흉내 낼 만하다.

### 2. `KiSearchForNewThread`

파일: `ms/disassemble/ntoskrnl/KiSearchForNewThread.asm`, 고르는 본체 `KiSearchForNewThreadsForRescheduleContext.asm`/`.c`.

**(a) 어디서 고르나, 순서** (`KiSearchForNewThread` 를 `KiSwapThread` 가 `EDX=0` 으로 부른 경우):

1. **`PRCB.NextThread`** (`+0x20 MOV RBX,[RCX+0x10]`, `+0x2d JNZ +0x110`). 있고 Idle 이 아니면 끝 (위 1(a)).
2. **이 CPU 의 reschedule 문맥을 만들고 한 번에 찾는다** (`+0x33 MOV RBP,[RCX+0x37e8]` 문맥 버퍼, `+0x45 memset(0xf0)`, `+0x53/+0x58 KiStartRescheduleContext`).
   - NextThread 가 0 이면 `+0x15d` 에서 이 CPU 칸(`_KI_RESCHEDULE_CONTEXT_ENTRY`)을 **"Idle" 상태로 초기화**한다 (`+0x16c/+0x171 KiFindRescheduleContextEntryForPrcb`, `+0x185 [entry+0x10]=[Prcb+0x18]`, `+0x19c [entry+0x18]=[Prcb+0x18]`, `+0x18f` 플래그 `(flags&0xc5)|5` = Idle 비트0 + CanCheckDeferredPreemption 비트2). → 현재 CPU 는 "아무 스레드나 받는다".
   - **`+0x69 / +0x6e KiSearchForNewThreadsForRescheduleContext`** 가 준비 목록을 뒤진다 (아래 (b)(c)).
   - **`+0x7f / +0x84 KiCommitRescheduleContext`** → 안에서 칸마다 `KiCommitRescheduleContextEntry` (`KiCommitRescheduleContext +0x56/+0x5b`). `KiCommitRescheduleContextEntry.asm +0x149 MOV [RBX+0x10],RDI` — **찾은 스레드(칸의 NewThread)를 `PRCB.NextThread` 에 적고**, `+0x179 MOV [RDI+0x184],3` (State = Standby).
   - 다시 읽기: `+0xa7 MOV RBX,[RDI+0x10]` (NextThread). 있으면 `+0xae JNZ +0x151` → `+0x151 CMP RBX,[RDI+0x18]`, Idle 이 아니면 `+0x15b JMP +0xec` 로 가서 그대로 돌려준다. 찾은 스레드가 있으면 `+0x216~+0x287` 에서 락을 풀고 `KiReadyDeferredReadyList` (`+0x287 / +0x28c`) 로 밀려난 스레드들을 준비 목록에 돌려놓은 뒤 `+0x275 KiAcquirePrcbLocksForIsolationUnit` 로 락을 다시 잡고 `+0xa7` 로 간다.
3. **준비 목록은 "이 CPU 것" 과 "공유 것" 을 한꺼번에** 본다 (아래 (b)). 순서가 "이 CPU → 공유" 가 아니다.
   - 이 CPU 것: `PRCB+0x83d8` (32비트 비트맵) + `PRCB+0x8440 + 우선순위×0x10` (리스트 머리). `[추측: 이름 ReadySummary / DispatcherReadyListHead]`.
   - 공유 것: `PRCB+0x8e48` 이 가리키는 `_KSHARED_READY_QUEUE` (`+0x8` ReadySummary, `+0x40 + 우선순위×0x10` ReadyListHead). 근거: `KiSearchForNewThreadsForRescheduleContext.asm +0x1f6~+0x205` (`ADD RAX,4; SHL RAX,4; ADD RAX,R8`), `+0x925~+0x933` (`ADD RAX,0x844; SHL RAX,4`).
4. **그래도 없으면 시스템의 나머지에서 훔치기**: `KiSearchForNewThread +0xd4 / +0xd9` 락 풀고 → **`+0xdf / +0xe4 KiSearchForNewThreadsWithinRestOfSystem`** (`.asm`: `[Prcb+0x9418] != 0` 이면 `+0x1b KiSearchForNewThreadsWithinL0SearchContext`, 못 찾으면 `[Prcb+0x9420] != 0` 일 때 `+0x41 …WithinL1SearchContext`). 본체 (`…OnTarget` 등) 는 안 읽음. 못 찾으면 `+0x12c / +0x131 KiReparkIdlingProcessorIfNecessary`.
   - 건너뛰는 조건: `+0xc7 TEST [RDI+0x23],0x10` (`IdleState` 비트 4) → `+0x28e` 로 가서 락만 풀고 NextThread 를 돌려준다. `+0xb4 TEST [RDI+0x23],1` 이 서 있는데 고를 게 없으면 `+0x109 INT 0x29` (ECX=0x21, fastfail) — 뜻은 안 풀었다.
5. 훔치기에서 찾으면 그 스레드를 돌려줌. 못 찾고 `R15B`(=EDX) 가 0 이면 락을 다시 잡고 (`+0x146`) `+0x14b MOV RBX,[RDI+0x10]` → 돌려줌 (보통 Idle).

**PE_emulator 와 비교**: 우선순위 없음 → **틀림.** 또 "이 CPU 목록 → 공유 목록 → 훔치기" 는 맞는 순서가 아니다. 진짜는 (NextThread) → (이 CPU 목록과 공유 목록을 우선순위로 섞어서) → (훔치기) → (Idle). CPU 가 하나인 에뮬레이터라면 공유 목록·훔치기는 건너뛰어도 된다 `[추측: 에뮬레이터 범위]`.

**(b) 가장 높은 우선순위부터?** — **예.** `KiSearchForNewThreadsForRescheduleContext.asm`:

- 먼저 `KiStartReadyQueueEnumeratorForRescheduleContext` (`+0xd8` 부름, ret `+0xdd`) 가 칸마다 "이 CPU 가 받을 수 있는 우선순위 마스크" 를 만들고 (`+0x1cf MOV ECX,[RAX+0x83d8]` · `+0x1d5 AND [RDX],ECX` 이 CPU 비트맵과 AND, `+0x1dc~+0x1e0` 공유 비트맵 `[R9+8]` 과 AND), 이 마스크에서 **가장 높은 켜진 비트** 를 `BSR` 로 고른다: `+0x125 AND EAX,0xfffffffe` · **`+0x131 BSR R11D,EAX`** (우선순위 1~31 의 가장 높은 것).
- 단계가 셋이다 (`+0xf6 MOV DIL,1`, `+0x100~+0x110` 분기, `+0x2b5 INC DIL; +0x2b8 CMP DIL,4` 로 반복): **① 우선순위 1~31** (`+0x131` BSR) → **② 그룹 스케줄링용 별도 목록(SCB)** (`+0x107 JZ +0x216`, `KiAdvanceReadyQueueEnumeratorToNextScb` 등) → **③ 우선순위 0** (`+0x3bc AND EAX,1`, **`+0x3c8 BSR R11D,EAX`**). 우선순위 0 은 맨 마지막이다. 마지막 BSR 한 곳 더: `+0xa6a BSR EAX,ECX` (SCB 쪽).
- 같은 우선순위에 이 CPU 목록과 공유 목록이 **둘 다** 있으면 **번갈아** 고른다: `+0x1d6 JNZ +0xc61` → `+0xc61 BT [R10+0x83dc],EBX; SETNC AL` (PRCB+0x83dc 의 우선순위 비트: 0 이면 공유, 1 이면 이 CPU). 이 CPU 에서 꺼내면 비트를 지우고 (`+0x992 BTR [R14+0x83dc],R15D`), 공유에서 꺼내면 세운다 (`+0x783 BTS`). `[추측: 공평하게 번갈아 하려는 장치]`
- 칸마다 어떤 우선순위부터 받는지의 마스크는 5번 질문 아래 "yield 마스크" 참고.

**PE_emulator 와 비교**: 우선순위 있음 (31 → 1, 마지막에 0). **틀림** (현재 우선순위 없음).

**(c) 같은 우선순위 목록에서 맨 앞?** — **예, 맨 앞부터 본다. 하지만 "맨 앞에서 쓸 수 있는 첫 번째".**

- 걷는 줄: `+0x2d0 MOV RAX,[RBP-0x38]` (지금 자리) → **`+0x2d4 MOV RDI,[RAX]`** (`Flink` = 목록 맨 앞) → `+0x2db CMP RDI,[RBP-0x40]` (머리로 돌아왔으면 끝 → `+0x2df JZ +0x698` → `+0x69c / +0x6a1 KiAdvanceReadyQueueEnumeratorToNextList` 로 다음 우선순위 목록) → `+0x2e9 LEA RSI,[RDI-0xd8]` (고리 → `_KTHREAD`, `WaitListEntry(+0xd8)`).
- 첫 후보를 어느 CPU 칸도 못 받으면 (칸 순회 `+0x659 INC R14D` → `+0x35d~+0x36a`, 칸을 다 돌았으면 `+0x370/+0x373 JMP +0xdd` = 루프 맨 위) **다음 `Flink` 로 간다.** 받는지 보는 검사: 그룹 스케줄링 `+0x647 / +0x64c KiEvaluateGroupSchedulingPreemption` · `+0x955 / +0x95a KiIsThreadConstrainedBySchedulingGroup`, 우선순위 비교 `+0xd92~+0xdb3` (후보가 더 높으면 받음 `+0xd9f JG +0x725`, 같으면 `Partial/FullYield(0x18)` 일 때만 `+0xdab`, 더 낮으면 `FullYield(0x10)` 이고 후보 우선순위≠0 일 때만 `+0x715~+0x71f`), 다른 CPU 칸 쪽은 `+0x1081 / +0x1086 KiCanLocalReadyThreadBeScheduledToRescheduleContextEntry`. (친화도 모음 번호가 낡은 스레드 `+0x324 CMP [RSI+0x238],KiCpuSetSequence` `+0x32b JNZ +0xab2` 는 건너뛰지 않고 목록에서 빼서 밀린 목록에 달고 `State=7` 로 한 뒤 다시 시작한다 — 이 부분은 디컴파일 줄거리만 읽음 `[정적, 디컴파일 해석]`.)
- 뽑으면: 이 CPU 목록에서는 `+0x9a7~+0x9c4` 로 고리를 빼고, 목록이 비면 비트맵 비트를 끈다 (**`+0x9cd~+0x9db` `BTC ECX,EAX; MOV [R14+0x83d8],ECX`**), `+0x9e2 ADD [R14+0x8674],-1` (이 CPU 준비 스레드 수), `+0x9f6 SUB [R14+0x8678],[RSI+0x6f8]`. 공유 쪽은 `+0x8f9~+0x918` (`[R9+8]` 비트 끄기), `+0x7bc ADD [R9+0xc],-1`.
- 뽑은 스레드를 칸에 적기: `+0xed8 MOV [R9+0x20],RSI` (CompareThread) · `+0xeef MOV [R9+0x28],RSI` (NewThread) · `+0xf0f~+0xf13 [RSI+0x218] = Prcb.Number`. 안 뽑힌 다른 칸은 그 CPU 의 `IdleThread(+0x18)` 로 되돌린다 (`+0x866 MOV RDI,[R14+0x18]`).

**PE_emulator 와 비교**: 목록 앞에서부터 꺼내는 건 **맞다** (FIFO). 우선순위가 같을 때만.

**(d) 고를 게 없으면 — `IdleThread`?** — **예. 다만 `KiSearchForNewThread` 가 `+0x18` 을 직접 돌려주는 게 아니라, 칸에 미리 넣어 둔 값이 `NextThread` 로 커밋돼서 나온다.**

- `KiSearchForNewThread +0x185 MOV [R9+0x10],RAX` · `+0x19c MOV [R9+0x18],RAX` (RAX = `[Prcb+0x18]` = `IdleThread`) — 이 CPU 칸의 CompareThread·NewThread 기본값을 IdleThread 로.
- 찾는 동안 못 찾으면 이 기본값이 그대로 남고, `KiCommitRescheduleContextEntry +0x149 MOV [RBX+0x10],RDI` 가 그걸 `PRCB.NextThread` 에 적는다. `KiSearchForNewThread +0xa7 / +0x14b MOV RBX,[RDI+0x10]` 으로 읽어서 돌려준다 (`+0x151 CMP RBX,[RDI+0x18]` 로 Idle 인지 확인해서 Idle 이면 훔치기까지 해 본다).
- `KiSwapThread` 쪽에서는 그 Idle 도 똑같이 `CurrentThread` 로 놓고 (`+0x17d`) 넘어간다.
- 주의: `EDX != 0` 으로 부르면 (`+0x58 TEST R15B,R15B → +0x5b JZ +0x15d` 를 안 타서 위 기본값 적기를 건너뜀) NewThread 가 0 일 수 있고, 그때는 `KiCommitRescheduleContextEntry +0x208 MOV [RBX+0x10],R11` (R11=0) 로 NextThread 가 0 이 된다. `KiSwapThread` 는 `EDX=0` 이라 해당 없음.

**PE_emulator 와 비교**: 고를 게 없으면 Idle 스레드 → 지금 하는 것에 없다면 **추가해야 한다.**

### 3. 같은 우선순위끼리 차례 — 넣는 쪽

**넣는 함수** (`_index.tsv` 로 확인): 목록에 실제로 꿰는 것은 **`KiDeferredReadySingleThread`** (rva `0x30bbf0`, 9230B). 나머지는 곁다리다.

| 함수 | 크기 | 하는 일 |
|---|---|---|
| `KiDeferredReadyThread` | 166B | 껍데기. `+0x23 / +0x28` 에서 `KiDeferredReadySingleThread(Prcb, Thread, &리스트, R9)` 부르고, 딸려 온 "밀린 스레드 단일 연결 목록" 을 `+0x5d / +0x62` 로 하나씩 같은 함수에 넣는다 (R9D=0). `KiFlushSoftwareInterruptBatch` 도 부름 |
| **`KiDeferredReadySingleThread`** | 9230B | 스레드를 **준비 목록에 꿰는 곳** (이 CPU 목록 또는 공유 목록) — 앞/뒤 갈림 아래 |
| `KiInsertDeferredReadyList` | 90B | **준비 목록이 아니다.** 단일 연결 "밀린 스레드 목록" 에 스레드를 맨 앞에 달고 `State = 7` (DeferredReady) 로 적는다 (`+0x33 MOV [RDX+0x184],7` · `+0x3a ADD RDX,0xd8` · `+0x41~+0x47` 머리에 push). `KiSearchForNewThreadsForRescheduleContext` 가 밀어낸 스레드를 담을 때 쓰고 (그때 `[Thread+0x235]=1` `+0xe9d`, `+0x10db` …), 나중에 `KiReadyDeferredReadyList` 가 `KiDeferredReadySingleThread` 로 넣는다 (`KiReadyDeferredReadyList +0x46 / +0x4b`) |
| `KiQueueReadyThread` | 2974B | yield·퀀텀 끝에서 현재 스레드를 목록에 넣는 길. `KiDeferredReadySingleThread` 를 `+0x1f9 / +0x1fe` (R9D=0), 딸린 스레드는 `+0x223 / +0x228` 로 부름. `+0x3a MOVZX R12D,[R8+0x235]` 로 `Preempted` 를 읽고 `+0x1ec MOV [R13+0x235],R12B` 로 그대로 되돌려 둔 뒤 넣는다 |

**앞/뒤를 가르는 줄** (`KiDeferredReadySingleThread.asm`):

- **뒤에 붙이기** (이 CPU 목록): `+0x1711~+0x171f` 로 리스트 머리 계산 (`RAX = Prcb + (우선순위 + 0x844)×0x10`, 고리 `RCX = Thread+0xd8`), **`+0x1726 CMP [RSP+0x58],EDI` / `+0x172a JNZ +0x1824`**. 같으면 (칸이 0) `+0x1730 MOV RDX,[RAX+8]` (머리의 `Blink`) 뒤에 연결: `+0x173d~+0x1747` → **맨 뒤**.
- **앞에 붙이기**: 다르면 (칸이 0 이 아님) `+0x1824 MOV RDX,[RAX]` (머리의 `Flink`) 앞에 연결: `+0x1831~+0x183c` → **맨 앞**, 그리고 `+0x183f BTS [RSI+0x83dc],R14D` (번갈아 칸에서 "이 CPU 쪽 먼저" 로 표시). 둘 다 이어서 `+0x1847~+0x1857` `[RSI+0x83d8] |= 1<<우선순위` (비트맵 켜기).
- 공유 목록도 같은 칸으로 갈린다: 앞 `+0x14c9~+0x14dd` (그리고 `BTR [PRCB+0x83dc]`), 뒤 `+0x1bb1~+0x1bbb`.
- **그 칸 `[RSP+0x58]` 에 무엇이 들어가나**: `KTHREAD.Preempted(+0x235)`. 근거 `+0x1459 MOVZX EAX,[RBX+0x235]` → `+0x1460 MOV [RSP+0x58],EAX`. 이 줄은 `+0xd5 JZ +0x144f` (`AdjustReason(+0x236)==0`) 에서 들어오고, `+0x144f CMP R13,[RBX+0x20]` / `+0x1453 JNC +0x1de0` — `R13 = CycleTime(+0x48)` 이 `QuantumTarget(+0x20)` 보다 **작을 때만** (= 퀀텀이 안 끝났을 때만) 이 줄을 탄다. 퀀텀이 끝났으면 `+0x1de0` 에서 `KiComputeNewPriority` 로 우선순위를 다시 정하고 칸은 처음 값 0 (`+0x63 MOV [RSP+0x58],EDI`, `EDI=0`) 그대로 → **뒤**.
- `Preempted` 를 켜는 곳: `KiSearchForNewThreadsForRescheduleContext +0xe9d` · `+0x10db` · `+0x1114` · `+0x1143` · `+0x1172` (`MOV byte [RCX+0x235],1` 바로 뒤 `KiInsertDeferredReadyList`) — 대기하던 스탠바이 스레드를 더 높은 걸로 밀어낼 때. `KiCommitRescheduleContextEntry +0x3da` · `+0x403` 도 씀. 끄는 곳: `KiDeferredReadySingleThread +0x26d MOV word [RBX+0x235],DI` (퀀텀을 새로 받을 때 `+0x260 MOV [RBX+0x20],RCX` 와 함께 `Preempted`·`AdjustReason` 둘 다 0).

→ **답: 기본은 맨 뒤. "선점당했고 (Preempted≠0) 퀀텀이 아직 안 끝난" 스레드만 맨 앞.** 퀀텀 다 쓴 스레드 · 양보한 스레드 · 깨어난 스레드는 뒤 (`[추측]`의 "선점=앞 / 퀀텀 끝·양보=뒤" 와 맞다).
- 한계: 스택 칸 `[RSP+0x58]` 은 이 함수의 다른 갈래 (`+0x437`, `+0x46e`, `+0x13f7`) 에서 락 개수 같은 값으로도 쓰인다. 이 갈래들이 `+0x170c` 로 이어지는지는 끝까지 안 따라갔다 → 동적으로 확인 (아래).

**PE_emulator 와 비교**: 지금 없음 (`[추측]`). **추가: 맨 뒤 기본, `Preempted` 면 맨 앞.**

### 4. `NtYieldExecution`

**(a) 실제 몸** — **`KeYieldExecution`** (PDB 이름, Ghidra 이름 `Ordinal_6`, rva `0x2137a0`, 1290B). `NtYieldExecution.asm`: `+0x0 SUB RSP,0x28` · `+0x4 XOR ECX,ECX` · **`+0x6 CALL 0x1402137a0` (ret `+0xb`)** · `+0xb ADD RSP,0x28` · `+0xf RET`. **꼬리 호출(JMP)이 아니라 보통 CALL** 이다 (돌아와서 정리하고 RET). 인자 하나 `Flags = 0`. `_calls.tsv`: `NtYieldExecution +0x6 +0xb alias:Ordinal_6+1` (별칭 이름 `KeYieldExecution`).

`KeYieldExecution` (`ms/disassemble/ntoskrnl/Ordinal_6.asm`) 줄기:

| 오프셋 | 하는 일 |
|---|---|
| `+0x25` `TEST ECX,0xfffffffc` → `+0x2b JZ +0x4a` | `Flags` 에 0·1·2·3 밖의 비트가 있으면 `+0x2d MOV EBX,0xc000000d` (STATUS_INVALID_PARAMETER) |
| `+0x4a~+0x6e` | **미리 보기**: `RCX = gs:[0x20]` (PRCB). `[RCX+0x83d8]` (이 CPU 비트맵) 이 0 이고 `[[RCX+0x8e48]+8]` (공유 큐 비트맵) 도 0 이면 → `+0x69 MOV EBX,0x40000024` |
| `+0x79~+0x82` | IRQL 을 2 (DISPATCH_LEVEL) 로: `R12 = CR8` (이전 값) · `MOV CR8,2` |
| `+0x9c~+0xac` | 같은 검사를 IRQL 올린 뒤 한 번 더 (`+0x25f~+0x270`) — 둘 다 비면 `+0x270 JMP +0x395` |
| `+0xb7 / +0xbc` | `KiAbProcessPreContextSwitch(thread, 0)` |
| `+0xbf` | `ThreadLock(+0x40)` 스핀 (`BTS.LOCK`) |
| `+0xef / +0xf4` | `KiAcquirePrcbLocksForIsolationUnit` |
| `+0xf4 CMP [RSI+0x10],R13` | `NextThread == 0` 이면 `+0x2a0` (찾아보기), 아니면 `+0xfe~+0x167` 로 이어서 (이미 정해 둔 스레드 `RBX = NextThread`, `+0x150 MOV RBX,[RSI+0x10]`) |
| **`+0x2a0~+0x33f`** | 찾아보기: 문맥 버퍼 `[RSI+0x37e8]` · `+0x2b2 memset(0xf0)` · `+0x2c1 / +0x2c6 KiStartRescheduleContext` → `+0x2d8 / +0x2dd KiFindRescheduleContextEntryForPrcb` → **`+0x2f0 OR CL,0x10`** (`FullYield`) · `+0x2f6 OR [RAX+0x22],2` (`DoNotSetPreemptionBit`) → **`+0x30b / +0x310 KiSearchForNewThreadsForRescheduleContext`** → **`+0x31d / +0x322 KiCommitRescheduleContext`**. `AL==0` 이고 밀린 목록이 비면 (`+0x329~+0x330`) `+0xfe` 로 되돌아가 NextThread 를 다시 읽는다 → 0 이면 `+0x380` |
| `+0x167~+0x1b4` | 현재 스레드 새 퀀텀: `+0x171 KiUpdateTotalCyclesCurrentThread` · `+0x196 / +0x19b KiQueryQuantumReset` · `+0x19b IMUL EAX,[KiCyclesPerClockQuantum]` · **`+0x1b4 MOV [RDI+0x20],RCX`** (`QuantumTarget = 지금 + 리셋값×주기`) |
| `+0x1be / +0x1c3` | `KiComputeNewPriority(thread, 1)` (우선순위 다시 계산. 안 읽음) |
| `+0x1db~+0x1f4` | `[RDI+0x40]=0` (ThreadLock 해제) · `+0x1e5 [RSI+0x10]=0` (**NextThread = 0**) · `KiCaptureTotalCyclesCurrentThread` · **`+0x1f4 / +0x1f9 KiUpdatePriorityMatrixForRunningTransition(Prcb, 새 스레드)`** — 이 함수가 `CurrentThread = 새 스레드` (`+0x3 MOV [RCX+0x8],RDX`) 와 `State = 2` 를 한다 (`KiUpdatePriorityMatrixForRunningTransition.asm +0x11`) |
| `+0x1fc` `MOV byte [RDI+0x283],0x21` · `+0x207` `MOV [RDI+0x186],R12B` | 지금 스레드 `WaitReason(+0x283) = 0x21` (`WrYieldExecution` 으로 보임 `[추측]`), `WaitIrql = 이전 IRQL` |
| **`+0x211 / +0x216 KiQueueReadyThread(Prcb, &락핸들, 지금 스레드)`** | **지금 스레드를 준비 목록에 넣는다** → 질문 3 의 `KiDeferredReadySingleThread` |
| **`+0x21f / +0x224 KiSwapContext(지금, 새, R8B=1)`** | 넘기기. 세 번째 인자는 `1` (`+0x216 MOV R8B,1`) |
| `+0x227 / +0x22c` | `KiAbProcessPostContextSwitch` |
| `+0x22c MOV EBX,R13D` (=0) · `+0x24c JMP +0x32` | **돌려줄 값 0** |

**(b) 같은 우선순위일 때만 넘기나, 지금 스레드를 맨 뒤로 보내나**

- **같은 우선순위만이 아니다.** `FullYield` 라서 **우선순위 1 이상 아무 준비 스레드나** 받는다 (자기보다 낮아도). 근거 `KiStartReadyQueueEnumeratorForRescheduleContext.asm`:
  - `+0xda TEST R11B,0x10` (칸 플래그 `FullYield`) → `+0xde JNZ +0x251`.
  - `+0x251~+0x25e`: `NEG CL; SBB EDX,EDX; DEC EDX` → 현재 스레드 우선순위(`CL`)가 0 이 아니면 마스크 `0xfffffffe` (우선순위 1~31 전부), 0 이면 `0xffffffff`.
  - 비교용: `FullYield` 없음 · `PartialYield(0x8)` 없음 → `+0xed~+0xfc` 마스크 = 현재 우선순위 **보다 높은** 것만 (`NOT((1<<p)-1)` 하고 `+0xfc BTR EDX,ECX` 로 같은 비트도 끔). `PartialYield` 만 있으면 `+0xf6 TEST R11B,8 → +0xfa JNZ +0xff` 로 `BTR` 을 건너뛰어 같은 것까지 (`>=`).
  - `KeYieldExecution` 은 `Flags` 비트 0 이 **0** (NtYieldExecution 은 0) 이면 `+0x2d0 MOV BL,1` → `FullYield` (`+0x2f0`). 비트 0 이 **1** 이면 `+0x4e5 CMP [RDI+0xc3],0x10` — 현재 우선순위가 16 미만이면 `PartialYield`(`+0x3c7 OR CL,8`), 이상이면 `FullYield`. (NtYieldExecution 에서는 안 탄다.)
  - 같은 결론을 후보를 받을 때의 비교 줄이 또 보여 준다: `KiSearchForNewThreadsForRescheduleContext +0xd92 MOVZX ECX,[RSI+0xc3]` · `+0xd99 CMP CL,[RDX+0xc3]` · `+0xd9f JG +0x725` (더 높음 → 받음) · `+0xda5 JNZ +0x715` (더 낮음) → `+0x715 TEST AL,0x10` (`FullYield`) · `+0x71d TEST CL,CL` (후보 우선순위 0 은 안 받음) → `+0x725` 받음 · 같음이면 `+0xdab TEST AL,0x18`.
  - 그 가운데서 **가장 높은 우선순위의 맨 앞** (질문 2(b)(c)) 이 뽑힌다. 그러니 실제로는 "자기와 같거나 낮은 것 중 가장 높은 우선순위".
- **지금 스레드는 준비 목록에 들어간다** (`+0x211 KiQueueReadyThread`). 퀀텀을 방금 새로 받았으니 (`+0x1b4`) 퀀텀 끝 갈래가 아니고, 맨 뒤냐 앞이냐는 질문 3 규칙 (`Preempted` 가 0 이면 **맨 뒤**) 이다. 이 함수가 `Preempted` 를 따로 지우진 않는다 (`Ordinal_6.asm` 에 `+0x235` 쓰는 줄 없음).

**(c) `0x40000024` (`STATUS_NO_YIELD_PERFORMED`) 를 돌려주는 줄** — 세 곳, 모두 "넘길 스레드가 없다":

1. `+0x69 MOV EBX,0x40000024` (`+0x6e JMP +0x32` → 반환) — IRQL 올리기 **전**에 이 CPU 비트맵과 공유 큐 비트맵이 둘 다 0.
2. `+0x395 MOV EBX,0x40000024` (`+0x39a JMP +0x22f`) — 들어오는 길 셋: ① `+0x270 JMP +0x395` (IRQL 올린 뒤 같은 검사에서 둘 다 0), ② `+0x157 JZ +0x380` (찾아봤는데 `NextThread` 가 여전히 0 → `+0x380~+0x390` 락 풀고 `ThreadLock=0`, `KiAbProcessPostContextSwitch`), ③ `+0x3bf/+0x3c5 JMP +0x395` (VP 백킹 스레드의 `KiVpBackingThreadYieldExecution` 가 0 을 돌려줄 때).
3. 이 값으로 `+0x22f` 로 가서 IRQL 을 원래대로 (`CR8 = R12B`, `+0x244~+0x248`) 하고 반환.

**(d) `0` (넘김) 은 언제** — `+0x15d` 이후로 가서 (NextThread 가 이미 있었거나, 찾아보기에서 `AL≠0`/밀린 목록이 있어 `NextThread≠0`) **`+0x21f KiSwapContext` 가 돌아온 뒤** `+0x22c MOV EBX,R13D` (R13D=0). 즉 실제로 문맥을 바꿨다가 다시 이 스레드 차례가 와서 돌아왔을 때 0. (돌아오는 시점은 한참 뒤일 수 있다.)

**PE_emulator 와 비교**: 지금 어떻게 하는지 요청서에 없다. 최소 규칙은 맨 아래.

### 5. `KiSwapContext` (214B) 와 `SwapContext` (584B)

`KiSwapContext` 는 껍데기, 몸은 `SwapContext` (`+0x71 CALL 0x1406b6540`, ret `+0x76`). 파일: `KiSwapContext.asm`, `SwapContext.asm` (조각 `FUN_1406b686f` 가 `+0x32f` 부터 이어짐).

**`KiSwapContext`** (인자 RCX=지금 스레드, RDX=새 스레드, R8=지금 스레드 WaitIrql):

- 저장 (**지금 스레드의 스택** 에): `SUB RSP,0x138`, `XMM6~XMM15` (`+0xf~+0x3f`), `RBX RDI RSI R12 R13 R14 R15` (`+0x44~+0x5b`). 비휘발성 레지스터만이다. (그래서 나중에 이 스레드 스택으로 돌아오면 `+0x76` 이하에서 그대로 복원된다.)
- `+0x5f MOV RBX,gs:[0x20]` (PRCB), `+0x68 MOV RDI,RCX`, `+0x6b MOV RSI,RDX`, `+0x6e MOV ECX,R8D`, **`+0x71 CALL SwapContext`**.
- 돌아오면 (`+0x76~+0xca`) 전부 복원, `+0xce ADD RSP,0x138`, `+0xd5 RET`. 반환값 AL 은 `SwapContext` 것이 그대로.

**`SwapContext`** (RDI=지금, RSI=새, RBX=PRCB, CL=WaitIrql):

| 오프셋 | 하는 일 |
|---|---|
| `+0xe CMP byte [RSI+0x71],0` / `+0x12 JNZ +0x65e` | 새 스레드의 `Running(+0x71)` 이 아직 1 이면 (다른 CPU 가 막 내려놓는 중) `+0x65e` 에서 `PAUSE` 하며 기다린다 |
| `+0x18 MOV byte [RSI+0x71],1` | 새 스레드 `Running = 1` |
| `+0x3f CLI` | 인터럽트 막음 |
| `+0x46 KiStartThreadCycleAccumulationContextSwap` · `+0x4b INC [RBX+0x2d3c]` | 사이클 계산 시작 · PRCB 의 전환 횟수 +1 |
| `+0x9a~+0xd7` | **지금 스레드의 부동소수점/XSAVE 상태 저장**: `RCX=[RDI+0x60]` (`StateSaveArea`) 에 `XSAVE`/`XSAVEOPT`/`XSAVES`/`FXSAVE` (CPU 기능 비트 `KeFeatureBits` 로 고름), `+0xd7 STMXCSR [RCX+0x18]` |
| **`+0xf3 MOV [RDI+0x58],RSP`** | **지금 스레드 커널 스택 포인터 저장** (`KernelStack`) |
| **`+0xf7 MOV RSP,[RSI+0x58]`** | **스택 바꾸기** (새 스레드의 `KernelStack`) |
| `+0xfb~+0x12a` | `KiKernelCetEnabled` 면 그림자 스택 처리 (`RSTORSSP`·`SAVEPREVSSP`) |
| `+0x131~+0x14f` | 지금 스레드 프로세스 `[+0x310]` 칸이 0 이 아니면 `RDMSR 0xc0000100` (FS base) 저장 |
| `+0x155~+0x19c` | 추측 실행 제어 `SpecCtrl` 처리 (`KiUpdateSpeculationControl` 은 `+0x197` 에서 부름, ret `+0x19c`) |
| `+0x32f~+0x356` (조각) | `LFENCE`, `OR byte gs:[0x85e],2`, **`STI`** |
| **`+0x357 MOV R14,[RSI+0xb8]` / `+0x35e CMP R14,[RDI+0xb8]` / `+0x365 JZ +0x42a`** | **주소 공간 갈래**: `ApcState.Process(+0xb8)` 두 개 비교. **같은 프로세스면 CR3 를 안 바꾸고** `+0x42a` 로 건너뛴다 |
| (다르면) `+0x36b~+0x380` | `BTS.LOCK` 으로 새 프로세스의 `ActiveProcessors(+0x80)` 에 이 CPU 비트를 켠다 |
| (다르면) `+0x387 MOV RCX,[R14+0x28]` | 새 프로세스 `DirectoryTableBase(+0x28)` |
| (다르면) `+0x38b~+0x3cc` | `KiKvaShadow` 면 사용자/커널 CR3 쌍 처리 (CLI/STI 로 감쌈) |
| (다르면) **`+0x3cd~+0x3e0`** | `HvlEnlightenments` 비트 0 이면 `HvlSwitchVirtualAddressSpace` (`+0x3d9`), 아니면 **`MOV CR3,RCX` (`+0x3e0`)** |
| (다르면) `+0x3e3~+0x404` | KVA 섀도일 때 PCID 비트 없으면 CR4 비트 7 껐다 켜기 (TLB 비움) |
| (다르면) `+0x407~+0x423` | **이전 프로세스** `ActiveProcessors` 에서 이 CPU 비트를 `BTR.LOCK` 으로 끔 |
| `+0x42a~+0x44b` | 새 스레드 `InitialStack(+0x28)` 을 TSS `RSP0` 자리 (`+0x43e`, KVA 섀도면 `[RBX+0xae88]` `+0x444`) 와 `PRCB.RspBase(+0x28)` (`+0x44b`) 에 적음 |
| `+0x45f~+0x482` 등 | 추적·MSR 처리 (`KiCpuTracingFlags`, `WRMSR 0x1c4`) — 줄기 밖 |
| **`+0x4ad MOV byte [RDI+0x71],0`** | **지금 스레드 `Running = 0`** (다른 CPU 가 가져갈 수 있게 풀어 줌. 스택을 바꾼 뒤에 한다) |
| `+0x4b1~+0x4e8` | **새 스레드 부동소수점 상태 복원**: `RCX=[RSI+0x60]` 에 `FXRSTOR`/`XRSTOR`/`XRSTORS`, `+0x4e8 LDMXCSR [RCX+0x18]` |
| `+0x50f~+0x5ad` | 새 스레드의 TEB/GS: `[RSI+0xf0]` (`Teb`) 를 `WRMSR 0xc0000102` (KERNEL_GS_BASE), FS base `WRMSR 0xc0000100` (`[RSI+0x678]`), `MOV FS,0x53`, DS/ES/GS 를 `0x2b` 로 |
| `+0x5bb INC dword [RSI+0x154]` | 새 스레드 `ContextSwitches +1` |
| `+0x5c1~+0x5e6` | `CMP [RSI+0xc1],1` (`KernelApcPending`) 이 1 이고 (`WaitIrql` 와 `SpecialApcDisable(+0x1e6)` 를 OR 한 값) 가 0 이면 반환값 AL = 1 (APC 지금 처리하라). 아니면 `+0x5de HalRequestSoftwareInterrupt(1)` (`+0x5d9 MOV ECX,1`) 하고 AL = 0. `+0x5e6 SETZ AL` |
| `+0x5e9~+0x5ee` | `ADD RSP,0x30; POP RBP; RET` — **이 RET 은 새 스레드 스택에서 일어난다.** 새 스레드가 예전에 `SwapContext` 를 부르고 잠든 곳 (`KiSwapContext +0x76`) 으로 돌아간다 |

→ **답**: 레지스터는 `KiSwapContext` 가 비휘발성 GPR+XMM6~15 를, `SwapContext` 가 FPU/SSE/AVX 상태 (`StateSaveArea`) 와 `RBP` 를 저장한다. 스택은 `+0xf3`/`+0xf7`. 주소 공간은 `+0x35e/+0x365` 에서 갈리고 같은 프로세스면 안 바꾼다. 부르는 함수: `KiSwapContext → SwapContext` (그 안에서 `KiStartThreadCycleAccumulationContextSwap` · `KiCheckAndApplyCacheIsolation` · `KeCheckAndApplyBamQos` · `KiSaveThreadIptState` · `KiUpdateSpeculationControl` · `HvlSwitchVirtualAddressSpace` · `HalRequestSoftwareInterrupt` · `EtwTraceContextSwap` · `KiRestoreThreadIptState` · `KeBugCheckEx`(`0xb8`, `0x1ce`)).

**PE_emulator 와 비교**: CR3·FPU·MSR 은 흉내 낼 대상이 아니다 (유저 공간 에뮬레이터). 흉내 낼 것: `Running` 이 아직 1 이면 기다리기 · 지금 스레드의 레지스터와 스택 위치를 스레드 쪽 칸에 저장하고 새 스레드의 것을 읽기 · **프로세스가 다르면 주소 공간 전환 (같으면 안 함)** · `ContextSwitches +1` · APC 대기 표시를 반환.

## PE_emulator 가 흉내 낼 최소 규칙

1. **다음 스레드 고르기** (`KiSearchForNewThread`):
   1. CPU 의 `NextThread` 가 있고 Idle 이 아니면 그것.
   2. 아니면 준비 목록에서 **우선순위 31 → 1** 순으로, 같은 우선순위는 **목록 맨 앞** (FIFO). 첫 후보가 이 CPU 에서 못 돌면 다음 것.
   3. 1~31 에 없으면 우선순위 **0**.
   4. 그래도 없으면 (다른 CPU 가 있을 때만) 훔치기, 그래도 없으면 **IdleThread**.
2. **고른 뒤**: 목록에서 빼고 (비면 비트맵 비트 끔), `CurrentThread = 새 스레드`, `NextThread = 0`, 새 스레드 `State = Running(2)`.
3. **같은 스레드가 다시 뽑히면** 문맥 전환 없이 그대로 돌아온다.
4. **목록에 넣기** (`KiDeferredReadySingleThread`): 기본 **맨 뒤**. `Preempted != 0` 이고 퀀텀이 안 끝났으면 **맨 앞**. 퀀텀을 새로 받으면 `Preempted` 를 0 으로. 넣을 때 우선순위 비트맵 비트를 켠다.
5. **넘기기** (`KiSwapContext`/`SwapContext`): 지금 스레드의 상태·스택 위치를 저장 → 새 스레드의 것을 복원. 프로세스가 같으면 주소 공간은 그대로, 다르면 새 프로세스의 주소 공간으로. `Running`: 새 스레드 1, 지금 스레드 0.
6. **돌아온 스레드** (`KiSwapThread` 끝): `WaitStatus (_KTHREAD +0xc8)` 를 돌려준다. APC 가 대기 중이면 (반환 AL=1) IRQL 낮춰 APC 처리.
7. **`NtYieldExecution`** (= `KeYieldExecution(0)`): 준비 목록 (이 CPU+공유) 이 비었으면 `0x40000024` 로 바로 반환. 아니면 우선순위 **1 이상 아무 준비 스레드** 중 가장 높은 것을 고르고 (자기보다 낮아도 됨), 지금 스레드는 새 퀀텀을 받아 준비 목록 맨 뒤에 넣고, 넘겼다가 돌아오면 `0` 반환. 고를 게 없으면 `0x40000024`.
8. **Idle 스레드는 항상 한 개** 있어야 한다 (`PRCB.IdleThread`).

## 동적분석으로 확인해야 할 것 (대기열 Q104 에 붙임, 이번엔 VM 안 함)

- **`Preempted` → 맨 앞 규칙이 실제로 쓰이는지**: 정적으로는 스택 칸 `[RSP+0x58]` 이 다른 갈래에서도 쓰여서 완전히 닫지 못했다. `KiDeferredReadySingleThread +0x1726` 에서 `dq @rsp+0x58 L1` 과 `poi(@rdx)`… (지금 스레드 `Preempted` = `[@rbx+0x235]`) 를 같이 보면 된다. 맨 앞 갈래 (`+0x1824`) 가 타지는지.
- **실제 우선순위 숫자와 비트맵**: `PRCB+0x83d8` (32비트) 와 `PRCB+0x8440` 이후 리스트 머리, `PRCB+0x8e48` → `_KSHARED_READY_QUEUE` (`dt nt!_KSHARED_READY_QUEUE`). test.exe 의 스레드 우선순위 (`Priority(+0xc3)`, `BasePriority`). 상태 사진(덤프)으로 가능.
- **`KiGroupSchedulingEnabled`** (`KiSearchForNewThreadsForRescheduleContext +0x622` 이 읽는 전역) 값 — 켜져 있으면 ② 그룹 스케줄링 단계가 의미를 가진다. 0 이면 PE_emulator 는 무시해도 된다.
- **CPU 개수·Idle 갈래**: VM 이 CPU 하나인지 여럿인지에 따라 `KiSearchForNewThreadsWithinRestOfSystem` (`[Prcb+0x9418]`, `[Prcb+0x9420]`) 을 타는지.
- **`KeYieldExecution` 의 `0x40000024` 갈래**: test.exe 에서 `NtYieldExecution` 이 불리는지 (불리면 `+0x69`/`+0x395` 중 어느 쪽인지). 불리지 않으면 정적 읽기로 충분.
- 추천 멈춤점 (모두 `nt!` 함수 입구/지점, 조건: test.exe 의 스레드일 때만):
  - `nt!KiSwapThread` 입구 (`rcx`=지금 스레드, `rdx`=PRCB), 그리고 `nt!KiSwapThread+0xe1` 의 `rax` (고른 스레드), `+0x248` (넘기기 뒤) 의 `rax`
  - `nt!KiSearchForNewThread+0x2a` (`rbx` = NextThread 가 이미 있었나)
  - `nt!KiSwapContext` 입구 (`rcx`,`rdx`,`r8b`), `nt!SwapContext+0x3e0` (CR3 바꾸는 줄 — 프로세스 다를 때만 도달)
  - `nt!KeYieldExecution+0x69` · `+0x395` · `+0x224` (WinDbg 에서는 PDB 이름 `nt!KeYieldExecution`. Ghidra 파일 이름만 `Ordinal_6`)

## 판정

**분석성공.** 다섯 질문 모두 `.asm` 줄로 닫았다. 단, 질문 3 의 "선점→맨 앞" 은 `[RSP+0x58]` 한 칸이 다른 갈래와 겹쳐서 **부분적으로만** 닫았다 (위 동적 확인 첫 항목). 질문 2 의 `_KPRCB` 칸 이름 (ReadySummary·DispatcherReadyListHead) 은 PDB 가 잘려 있어 `[추측: 이름]` 이다 (오프셋·동작은 `[정적]`).

핵심 정정: PE_emulator 의 "우선순위 없음" 은 틀림. 진짜는 **NextThread → (우선순위 31→1, 같은 우선순위 맨 앞) → 우선순위 0 → 훔치기 → Idle**.
