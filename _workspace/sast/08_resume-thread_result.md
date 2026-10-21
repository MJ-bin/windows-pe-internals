# 08 resume-thread — 정적 분석 결과

- 요청서: `_workspace/sast/08_resume-thread.md` (Q5a)
- 날짜: 2026-10-21 (02_sprint)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음)
- ntoskrnl analyze: **다시 하지 않음** (`.gpr` + `_index.tsv` + analyze 로그의 `SetPdb:` 줄 모두 있음). export 만 했다.
- 뽑은 함수 (모두 depth 0, 로그 `ms/ghidra/ntoskrnl.export.sast08{,b,c,d,e}.stdout`)
  - 1차 (요청서 정규식 6개): `NtResumeThread PsResumeThread KeResumeThread KeSuspendThread KiSuspendThread NtAlertResumeThread`
  - 넓힌 것 (이유는 아래 "범위를 넓힌 이유"): `PsMultiResumeThread KiResumeThread KiSchedulerApc KiInsertQueueApc KeAlertResumeThread KeInitThread`, `KiDeliverApc PspUserThreadStartup KeReadyThread KiSignalThreadForApc KiRequestSchedulerApcThread`, `KiInitializeContextThread`, `SwapContext`
  - 위치: `ms/decomp/ntoskrnl/<함수>.c`, `ms/disassemble/ntoskrnl/<함수>.asm`, 호출표 `ms/disassemble/ntoskrnl/_calls.tsv`
- 버전 확인: **일치**
  - `NtCreateUserProcess +0xc31 (ret +0xc36) → PspAllocateProcess` 일치
  - `PspInsertThread +0x802 (ret +0x807) → PspCallProcessNotifyRoutines` 일치
  - 덤으로: `NtResumeThread +0x26` 이 읽는 `[스레드+0x232]` 가 PDB `_KTHREAD.PreviousMode`(+0x232) 와 같다 (스크립트 오프셋·PDB·바이너리가 서로 맞는다).

## KERNELBASE 준비 상태 (Q5b 용, 뒤에서 걸어 둔 것)

- PDB: `ms/sym/KernelBase.dll.pdb` (12,521,472 바이트), guid_age=`72CCE506944DE0267C172AD64287F9BD1`
- 별칭 표: `ms/sym/KernelBase.dll.aliases.tsv` (별칭 무리 228곳, 이름 39,912개)
- analyze: `setsid nohup ... run_ghidra.sh analyze KernelBase.dll` 를 23:24 에 띄움
  - PID: run_ghidra.sh **1272290**, java **1272392**
  - 로그: `ms/ghidra/kernelbase.analyze.log`, `ms/ghidra/kernelbase.analyze.stdout`
  - 띄운 직후 확인: `SetPdb: KernelBase.dll <- ms/sym/KernelBase.dll.pdb` 줄 있음. `SCRIPT ERROR` 없음.
  - **이미 끝났다** (23:26, 약 2분). `Import succeeded`, `ExportIndex: 6448 functions`, 프로세스·잠금 파일 없음.
    - `.gpr` (0바이트, ntdll·ntoskrnl 것과 같음) + `ms/decomp/kernelbase/_index.tsv` + `SetPdb:` 줄 → "이미 분석됨" 세 조건 모두 충족.
  - 로그에 `osgi.ee=UNKNOWN` 글자가 있다. 원인은 Ghidra 가 자기 스크립트 `WindowsResourceReference` 를 컴파일 못 한 WARN 이고, 같은 WARN 이 ntdll·ntoskrnl analyze 로그에도 있다. 우리 스크립트(`SetPdb`, `ExportIndex`)는 정상 실행. 결과에 영향 없음.
  - Q5b 때 볼 이름: `_index.tsv` 에 `CreateProcessInternalW` (rva 0x7d6e0, 28087바이트), `ResumeThread` (rva 0xf1f00, 51바이트) 있음. 지도(`map`)는 아직 안 만들었다 (요청서대로 Q5b 때).

## 스크립트 문제

막힌 것은 없다. 작은 것 하나만 (결과에는 영향 없음, 고칠지는 오케스트레이터가 정한다).

- 파일: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` (asm·`_calls.tsv` 를 쓰는 부분)
- 증상: `KiSchedulerApc` 처럼 **Ghidra 함수 몸이 두 군데로 떨어진 경우**, 떨어진 쪽 줄이 `+0x4106d8 1406d9028 ...` 처럼 시작점에서 아주 먼 `+0x..` 로 찍히고, `.asm` 에 `; --- 조각` 이나 `; --- 주인 없는 덩어리` 머리가 **없다.** `_calls.tsv` 도 `KiSchedulerApc +0x4106df +0x4106e4 KiReleaseThreadLockLowerIrql` 로 들어간다.
- 문제: 이 오프셋은 WinDbg `nt!KiSchedulerApc+0x..` 로 나오는 값이 아닐 가능성이 크다 (실제 주소 `0x1406d9028`, RVA 0x6d9028). 읽는 사람이 `frag:` 처럼 "다른 기준"이라는 걸 알아채기 어렵다.
- 제안: 이런 줄에는 `; --- 함수 몸의 떨어진 범위 <시작>~<끝> (오프셋은 함수 시작 기준)` 머리를 붙이고, reference.md 의 오프셋 표기에 한 줄 설명을 넣는다.
- 이번 결과서는 이 줄을 근거로 쓰지 않았다.

## 범위를 넓힌 이유

- 요청서 대상 6개만으로는 "풀어 주는 사슬"이 끊긴다. **`NtResumeThread` 는 `PsResumeThread` 를 부르지 않고 `PsMultiResumeThread` 를 부른다.** (`_calls.tsv`) → 그 함수를 뽑았다.
- `KeResumeThread` 가 실제로 스레드를 깨우는 일은 `KiResumeThread` 안에 있어서 뽑았다.
- 질문 3 ("멈춘 첫 스레드가 서는 곳")을 코드로 따라가려면 멈춤 APC 가 걸리는 곳(`KiInsertQueueApc`, `KeInitThread` 에서 APC 를 만드는 곳), APC 를 처리하며 기다리는 함수(`KiSchedulerApc`), 그걸 부르는 함수(`KiDeliverApc`), 새 스레드의 첫 실행 길(`PspUserThreadStartup`, `KiInitializeContextThread`, `SwapContext`)이 필요했다.
- 지도(`_map/NtCreateUserProcess_*`)로는 `NtResumeThread` 쪽을 못 찾는다 (다른 시스템 콜). 그래서 이름은 `_index.tsv`, 호출 관계는 각 `.c` 머리의 `callers:` 와 `_calls.tsv` 로 찾았다.
- 필드 이름은 PDB 타입으로 확인했다: `llvm-pdbutil-14 dump -types ms/sym/ntoskrnl.exe.pdb` 에서 `_KTHREAD`, `_KAPC`, `_KAPC_STATE`, `_KWAIT_BLOCK`, `_EPROCESS`, `_KTHREAD_STATE`, `_KWAIT_REASON` 을 읽었다. 그래서 아래 필드 이름은 `[정적]` (PDB 타입 기준). 비트 이름도 PDB 비트필드 위치로 맞췄다.

### 이번에 쓴 필드 (PDB `_KTHREAD` 등, 26100.9457)

| 오프셋 | 이름 (PDB) | 뜻 (쉬운 말) |
|---|---|---|
| `_KTHREAD +0x40` | ThreadLock | 스레드 자체를 잠그는 자리 (비트 0 을 잡는다) |
| `+0x70` | WaitRegister (`_KWAIT_STATUS_REGISTER`) | 대기 상태 묶음 (하위 3비트를 `&7` 로 읽음) |
| `+0x74` bit 14 | ApcQueueable | 이 스레드에 APC 를 걸 수 있나 |
| `+0x74` bit 10 / 17 / 18 / 19 | SystemThread / TimerSuspended / SuspendedWaitMode / SuspendSchedulerApcWait | 시스템 스레드 / 타이머 멈춤 / 멈춤 대기를 유저 모드로 할지 / "멈춤 APC 안에서 기다리는 중" 표시 |
| `+0x78` bit 14 / 15 | FreezeCount / TerminationApcRequest | 얼림 표시 / 끝내라는 APC 요청 |
| `+0x98` ApcState → `+0xc0 / +0xc1 / +0xc2` | KernelApcInProgress / **KernelApcPending** / UserApcPending | 커널 APC 처리 중 / 커널 APC 가 줄 서 있음 / 유저 APC 가 줄 서 있음 |
| `+0x184` | State | 스레드 상태 (PDB 열거형: 0 Initialized, 1 Ready, 2 Running, 5 Waiting, 7 DeferredReady …) |
| `+0x186` | WaitIrql | 기다릴 때의 IRQL |
| `+0x1e4 / +0x1e6` | KernelApcDisable / SpecialApcDisable | APC 막기 카운터 |
| `+0x220` | Process | 스레드가 속한 프로세스 |
| `+0x232` | PreviousMode | 부른 쪽이 유저인지 커널인지 |
| `+0x284` | **SuspendCount** (signed char) | 멈춤 횟수 |
| `+0x288` (크기 0x58) | **SchedulerApc** (`_KAPC`) | 스레드가 미리 품고 있는 APC. 멈춤에 쓴다 |
| `_KAPC +0x50 / +0x51 / +0x52` | ApcStateIndex / ApcMode / **Inserted** | → SchedulerApc 의 Inserted 는 스레드 `+0x2da` |
| `+0x2e0` (KEVENT) | **SuspendEvent** | 멈춘 스레드가 기다리는 이벤트. `+0x2e4` 가 SignalState, `+0x2e8` 가 기다리는 줄 |
| `_EPROCESS +0x5fc` bit 15 / 20 | Flags3.ProcessFirstResume / EnableThreadSuspendResumeLogging | "첫 풀어줌" 표시 / 스레드 멈춤·풀어줌 ETW 기록 켜짐 |

## 질문별 답 [정적]

### 질문 1. 풀어 주는 사슬 (`NtResumeThread` → … → `KeResumeThread`)

- 답: `NtResumeThread` → **`PsMultiResumeThread`** → `KeResumeThread` → (멈춤 횟수가 0 이 되면) `KiResumeThread`. `PsResumeThread` 는 이 사슬에 **끼지 않는다.**
- 호출 줄 (`call_off` / **`ret_off`**):

| 부르는 쪽 | call_off | ret_off | 부르는 대상 | 메모 |
|---|---|---|---|---|
| `NtResumeThread` (0xa2d600, 190B) | +0x7d | **+0x82** | `ObpReferenceObjectByHandleWithTag` | 핸들→스레드. 접근 권한 인자 `0x1000`, 타입 `PsThreadType`, 태그 `0x75537350` |
| `NtResumeThread` | +0x96 | **+0x9b** | `PsMultiResumeThread(스레드, &이전횟수, 1)` | 세 번째 인자(풀 횟수)가 상수 1 |
| `NtResumeThread` | +0xa5 | +0xaa | `ObfDereferenceObjectWithTag` | 참조 풀기 |
| `PsMultiResumeThread` (0x2c9aa0, 141B) | +0x20 | **+0x25** | `KeResumeThread(스레드, 횟수)` | |
| `PsMultiResumeThread` | +0x6a | +0x6f | `EtwTiLogSuspendResumeThread` | 조건부 (아래 질문 2) |
| `KeResumeThread` (0x2c9b34, 205B) | +0x82 | **+0x87** | `KiResumeThread(스레드, 현재 PRCB, 0)` | **횟수가 0 이 될 때만** |
| `KeResumeThread` | +0xa0 | +0xa5 | `KiExitDispatcher` | 끝에서 항상 |
| `PsResumeThread` (0xa52c50, 20B) | +0xa | +0xf | `PsMultiResumeThread(…, 1)` | **꼬리호출 아님** (`CALL` + `RET`). 인자 `r8d=1` 만 채워 주는 얇은 껍데기 |

- `PsResumeThread` 의 호출자: `.c` 머리의 `callers:` 가 비어 있다 → 이 바이너리 안에서 직접 부르는 `CALL` 은 Ghidra 가 못 찾았다 (드라이버용 내보내기 함수로 보임, `[추측]`). `NtResumeThread` 쪽에서는 안 부른다 `[정적]`.
- `KeResumeThread` 를 부르는 함수 (Ghidra `callers:`): `KeAlertResumeThread`, `PsMultiResumeThread`, `PsMultiResumeProcess`.
- `NtResumeThread` 앞쪽: 부른 쪽이 유저 모드(`[스레드+0x232] != 0`)면 `PreviousSuspendCount` 포인터를 `0x7fffffff0000` 으로 눌러 가며 한 번 읽고 되쓴다 (+0x32~+0x4a, 쓰기 가능 검사 용도). 끝에서 `*PreviousSuspendCount = 이전횟수` (+0xb4) 를 쓰고 성공값 0 을 돌려준다.
- 근거: `ms/disassemble/ntoskrnl/{NtResumeThread,PsMultiResumeThread,KeResumeThread,PsResumeThread}.asm`, `_calls.tsv`. 오프셋은 asm 줄에서 직접 읽었다.

### 질문 2. `KeResumeThread` 가 하는 일

한 줄: **멈춤 횟수를 깎고, 0 이 되면 `KiResumeThread` 로 "SuspendEvent 를 신호 상태로 만들고, 그 이벤트를 기다리던 스레드를 깨운다."**

`KeResumeThread(스레드, 횟수)` 순서:
1. IRQL 을 2 (DISPATCH_LEVEL) 로 올린다 (+0x21 `MOV CR8,2`). 스레드의 `SuspendEvent` 헤더의 잠금을 잡는다 (+0x4a `KiAcquireKobjectLockSafe(스레드+0x2e0)`).
2. 이전 멈춤 횟수(`+0x284`)를 읽어 둔다 (돌려줄 값). 요청 횟수가 이보다 크면 이전 횟수로 줄인다.
3. 요청 횟수가 0 이 아니고 이전 횟수가 0 이 아니면 `SuspendCount -= 횟수` 를 저장한다 (+0x68).
4. **새 횟수가 0 이고, `ThreadFlags` 의 FreezeCount 비트(`+0x78` bit 14, 0x4000)가 꺼져 있으면** `KiResumeThread(스레드, PRCB, 0)` 를 부른다 (+0x82). 얼려 있는 스레드는 여기서 안 깨운다 (얼림을 푸는 쪽 `KiThawSingleThread` 가 `KiResumeThread` 를 따로 부른다. 같은 `.c` 머리 `callers:` 에 있음).
5. 잠금을 풀고 `KiExitDispatcher` 를 부른 뒤 **이전 횟수**를 돌려준다.

`KiResumeThread` (0x2c909c, 1020B) 가 하는 일 (코드 읽은 만큼):
- **+0x20: `스레드+0x2e4 (SuspendEvent.SignalState) = 1`** 을 직접 쓴다. `KeSetEvent` 를 부르지 않는다.
- 스레드 잠금(`+0x40` 비트 0)을 잡고 상태(`+0x184`)를 본다.
- **일반 경로** (스레드가 `SuspendEvent` 를 기다리는 중): `SuspendEvent` 의 기다림 줄(`+0x2e8`)을 돌면서 줄에 선 `_KWAIT_BLOCK` 마다 `WaitType`(`+0x10`)을 본다.
  - 1 → `KiTryUnwaitThread` (+0xa6 call / **+0xab** ret): 그 스레드를 기다림에서 풀어 준비 상태로 만든다. **멈춘 스레드를 깨우는 호출이 이것이다.**
  - 2 → `KiInsertQueueInternal` (+0x238 / +0x241), 4 → `KeInsertQueueDpc` (+0x3e1 / +0x3e6). (`_KWAIT_BLOCK +0x18` 이 Thread / NotificationQueue / Dpc 로 겹쳐 있는 모양과 맞다.)
- **가벼운 멈춤에서 되돌리는 경로** (상태 5 이고 `WaitRegister & 7 == 4`, +0x54~+0x5b): 멈추면서 떼어 놓았던 기다림 블록(`BlockState` 6)을 다시 4 로 되돌려 원래 객체의 기다림 줄에 끼우거나(+0x279~), 그 사이 신호가 왔으면 5 로 바꾸고, `TimerSuspended`(`+0x74` bit 17)였으면 타이머를 다시 건다 (`KiComputeDueTime`, `KiInsertTimerTable`, `KiTimerWaitTest`). 마지막에 `KiSignalThreadForApc`(+0xca / +0xcf) 또는 `KiSignalThread`(+0x199 / +0x19e)로 스레드를 실행 쪽으로 밀어 준다. `BlockState` 숫자의 이름은 PDB 에 열거형이 없어 `[추측]`.
- 종합: 깨움은 "이벤트 신호 + 기다림 줄 풀기" 방식이다. 별도 "깨움 APC" 같은 건 안 건다.

`PsMultiResumeThread` 가 덧붙이는 일:
- `KeResumeThread` 가 돌려준 이전 횟수가 **0 이 아니고, 요청 횟수 이하**일 때 (= 이 호출로 완전히 풀림):
  - 프로세스의 `Flags3.ProcessFirstResume` (`_EPROCESS +0x5fc` bit 15) 를 켠다 (+0x4e `OR.LOCK`, 이미 켜져 있으면 건너뜀).
  - `EnableThreadSuspendResumeLogging` (bit 20) 이 켜져 있으면 `EtwTiLogSuspendResumeThread` 를 부른다 (+0x6a / +0x6f).
- `[공백]` `ProcessFirstResume` 를 읽는 곳은 이번에 안 봤다. 이름대로면 "이 프로세스의 첫 풀어줌 표시"다.
- 끝에 `*이전횟수 = 이전 값` 을 쓰고 상태 0 을 돌려준다.

근거: `KeResumeThread.asm`, `KiResumeThread.asm/.c`, `PsMultiResumeThread.asm`, `_calls.tsv`, PDB 타입.

### 질문 3. 멈춰 두는 쪽이 하는 일

#### 3-a. 횟수만 올리나, APC 도 거나?

- 답: **둘 다 한다.** 횟수를 올리고, 멈춤용 APC 를 스레드 안에 미리 들어 있는 `SchedulerApc` 로 건다. `KiSuspendThread` 가 `KiInsertQueueApc` 를 부른다.
- `KeSuspendThread` (0x48ec74, 222B) — `PspInsertThread +0x2e5` 가 부르는 함수:

| 부르는 쪽 | call_off | ret_off | 대상 | 메모 |
|---|---|---|---|---|
| `KeSuspendThread` | +0x45 | +0x4a | `KiAcquireKobjectLockSafe(스레드+0x2e0)` | IRQL 2 로 올린 뒤 (+0x25) |
| `KeSuspendThread` | +0x66 | **+0x6b** | `KiSuspendThread` | **그 직전(+0x60)에 `SuspendCount+1` 을 먼저 저장** |
| `KeSuspendThread` | +0x86 | +0x8b | `KiExitDispatcher` | |
| `KeSuspendThread` | +0xd2 | `tail` | `FUN_1406f517a` (조각) | 횟수가 0x7f(최대)일 때만. 조각 안에서 `ExRaiseStatus` 별칭(`RtlRaiseStatus` 와 같은 주소)을 `0xc000004a` 로 부른다 (조각 +0xc / +0x11) |

  - 순서: `SuspendCount` 가 0x7f 면 상태 `0xc000004a` 를 던지고 끝. 아니면 `+1` 저장 → `KiSuspendThread` 호출 → **`KiSuspendThread` 가 0(실패)을 돌려주면 `SuspendCount-1` 로 되돌린다** (+0xd7) → 이전 횟수를 돌려준다.
  - `KeSuspendThread` 를 부르는 곳 (Ghidra `callers:`): `PspInsertThread`, `PsSuspendThread`. `KiSuspendThread` 를 부르는 곳: `KeSuspendThread`, `KiFreezeSingleThread`, `KiAdjustThreadTimer`.
- `KiSuspendThread` (0x265dc4, 849B) 가 하는 일, 순서대로:
  1. 스레드 잠금(`+0x40` 비트 0) 잡기 (+0x27).
  2. **`ApcQueueable` (`+0x74` bit 14) 가 꺼져 있으면 아무것도 안 하고 0 을 돌려준다** (+0x4b → +0x1fe). (그래서 위 `KeSuspendThread` 가 횟수를 되돌린다.)
  3. **`SuspendEvent.SignalState`(`+0x2e4`) 가 0 이면** 이미 멈춘 것으로 보고 APC 를 또 걸지 않고 1(성공)을 돌려준다 (+0x58 → +0x5f).
  4. 0 이 아니면 **`SignalState = 0`** 으로 내리고 (+0x6c) `SchedulerApc.Inserted`(`+0x2da`)가 0 이면 **1 로 바꾸고 `KiInsertQueueApc(스레드+0x288)` 를 부른다** (+0x83 call / **+0x88** ret).
  5. 이어서 두 갈래:
     - **가벼운 멈춤** (스레드가 이미 `Waiting`(State 5)이고 `WaitRegister & 7 == 1` 이고 `KernelApcDisable == 0` 등 조건이 모두 맞을 때, +0x95~+0xf0): 스레드를 깨워서 APC 를 돌리지 않고, 기다림 블록들을 `BlockState` 6 으로 바꿔 객체 기다림 줄에서 떼어 놓고 (+0x13a~+0x15e 루프, 객체를 잠그고 줄에서 빼는 안쪽은 +0x21f~+0x2a3), 타이머가 있으면 `KiCancelTimer` (+0x266 / +0x26b), 필요하면 `KiDecrementProcessStackCount` (+0x2af / +0x2b4), 마지막에 `WaitRegister` 를 4 로 바꾸고 상태가 맞으면 `KiSignalThread` (+0x1de / +0x1e3).
     - **그 밖** (조건이 하나라도 안 맞을 때. 아직 한 번도 안 달린 새 스레드가 여기): APC 를 방금 새로 걸었으면 `KiSignalThreadForApc(PRCB, SchedulerApc, 2, 0)` (+0x1f6 / **+0x1fb**).
  6. 잠금 풀고 1 을 돌려준다.
- `KiSignalThreadForApc` 는 (`.c` 읽은 만큼) 스레드가 **지금 이 CPU 에서 돌고 있는 스레드가 아니고** APC 가 커널 모드(`ApcMode == 0`)면 먼저 `ApcState.KernelApcPending (+0xc1) = 1` 을 쓴다. 그 뒤 상태가 Running(2)이면 IPI/소프트웨어 인터럽트를 요청하고, Waiting(5)이면 조건에 따라 깨워서 APC 를 처리하게 한다. 그 밖의 상태(아직 안 달린 새 스레드)는 **플래그만 켜고 끝난다.** (새 스레드의 State 가 0 Initialized 인지는 `KeInitThread` 에서 따로 쓰는 줄을 못 찾았다. 비워진 채라 0 일 `[추측]`.)

- `SchedulerApc` 가 어떤 APC 인지 (`KeInitThread` +0x152 `KeInitializeApc` 호출 인자, 오프셋 `[정적]`):

| 인자 | 값 | 읽은 것 |
|---|---|---|
| Apc | `스레드+0x288` | 스레드 안의 `SchedulerApc` |
| Thread | 그 스레드 | |
| Environment | 0 | |
| KernelRoutine / RundownRoutine | `0x1404a10c0` (같은 주소) | 아무것도 안 하는 함수. 이 주소엔 이름이 **100개** 붙은 별칭 무리가 있고 (`pdb_aliases.py --lookup 0x4a10c0`) 그 안에 `KiSchedulerApcNop`, `KiSchedulerApcRundown` 이 들어 있다. 한 이름만 문서에 쓰지 않는다. |
| NormalRoutine | `0x1402c8950` = **`KiSchedulerApc`** | |
| ApcMode | 0 (커널) | |
| NormalContext | 그 스레드 | `KiSchedulerApc` 의 첫 인자가 스레드인 까닭 |

  - 같은 자리에서 `KeInitializeEvent(스레드+0x2e0, 0 (Notification), TRUE)` — **`SuspendEvent` 는 처음에 신호 상태(`SignalState = 1`)**다. 그래서 `SignalState == 1` = "멈춘 게 아님", `0` = "멈춤"으로 읽힌다.
  - `NormalRoutine` 이 있으므로 `KiInsertQueueApc` 는 이 APC 를 **커널 모드 일반 APC 줄(`ApcState+0x98` 의 첫 줄) 맨 뒤**에 넣는다 (`KiInsertQueueApc +0x28`~`+0x6e`, 줄 선택 `+0x2d` `MOVSX [APC+0x51]`).
  - `KeInitThread +0xed` 는 `ApcQueueable` 을 **기본으로 켠다** (`BTS EAX,0xe`; 초기화 플래그 bit1 이 켜진 특수 스레드만 끈다). `WaitIrql`(`+0x186`) 은 같은 플래그로 1 (또는 2) 로 넣는다 (+0x10a~+0x10c).
  - `SchedulerApc` 는 멈춤에만 쓰는 게 아니다: `KiSchedulerApc` 의 끝내기 경로, `KiRequestSchedulerApcThread` (`KiSlistRollbackDpc` 가 부름), `KeRequestTerminationThread` 등도 `KiInsertQueueApc` 로 같은 APC 를 건다 (`KiInsertQueueApc.c` 머리 `callers:`).

#### 3-b. 멈춘 첫 스레드가 실제로 서는 곳

코드로 확인된 고리를 앞에서부터 이으면:

1. `[정적]` `PspInsertThread +0x2e5` 의 `KeSuspendThread` 로 멈춤 APC 가 **스레드 안에 걸리고** `KernelApcPending = 1` 이 켜진다. 이때 스레드는 아직 한 번도 달리지 않았다. (3-a)
2. `[정적]` `PspInsertThread +0x5a6` `KeReadyThread` 가 스레드를 대기 줄에 세운다 (run12). `KeReadyThread` 는 멈춤 여부를 안 본다.
3. `[정적]` `KiInitializeContextThread` 가 새 스레드의 첫 커널 스택에 시작 주소로 **`KiStartUserThread`** (`0x1406afbd0`) 를 깐다 (+0x1e1 `LEA`, `[스택 슬롯]` 에 저장). (`KiStartUserThread`·`KiStartUserThreadReturn` 은 PDB 공개 심볼 이름으로 맞췄다. Ghidra 함수가 아니라 `ms/decomp`·`ms/disassemble` 에는 없고, `objdump -D -b binary` 로 바이트를 직접 읽었다. 스크립트 결과가 아니다.)
4. `[정적]` 그 스레드가 처음 CPU 에 올라갈 때 `SwapContext` 끝(조각 `frag:FUN_1406b686f+0x292`, 실제 주소 `1406b6b01`~`1406b6b1e`)에 이런 코드가 있다: **"다음에 달릴 스레드의 `ApcState.KernelApcPending (+0xc1)` 이 1 이고, `(인자 CL | SpecialApcDisable(+0x1e6))` 가 0 이 아니면 `HalRequestSoftwareInterrupt(1)` (APC_LEVEL) 을 부른다."** `KeInitThread` 가 `WaitIrql` 에 1 을 넣어 두므로 CL 이 이 값이면 조건이 맞는다 (CL 이 정말 새 스레드의 `WaitIrql` 인지는 `[추측]`).
5. `[정적]` `KiStartUserThread` (바이트 읽음)는 IRQL 을 1 (APC_LEVEL) 로 올리고(`MOV CR8,1`), 저장해 둔 시작 루틴(`rax`)을 CFG 검사 함수 `0x6b77f0` 를 거쳐 부른다 (`0x6afbf3` call = 함수 시작 +0x23, ret +0x28). 그 시작 루틴은 `PspAllocateThread +0x681` 이 `LEA 0x140910de0` (= `PspUserThreadStartup`) 을 `KeInitThread` 에 넘기는 구조체(+0x128)에 넣는 값이다 (`KeInitThread` 호출은 +0x6f4 / +0x6f9). 이 구조체가 `KiInitializeContextThread` 를 거쳐 첫 스택에 들어간다고 읽은 것은 `[추측]` (사이의 전달을 한 줄씩 따라가진 않았다).
6. `[정적]` **`PspUserThreadStartup` 의 첫 일이 IRQL 을 0 으로 내리는 것이다** (+0x22 `MOV CR8,RDI` (`RDI=0`), 앞 +0x1d 는 `KiIrqlFlags` 가 켜졌을 때만 `KiLowerIrqlProcessIrqlFlags`). 그 뒤에야 `PspDisablePrimaryTokenExchange`(+0x37 / +0x3c), `PspNotifyThreadCreation`(+0x9c / +0xa1), `PspInitializeThunkContext`(+0xae / +0xb3) 가 나온다.
7. `[정적]` 대기 중인 APC 인터럽트는 `KiApcInterrupt` → `KiDeliverApc` 로 이어지고 (`KiDeliverApc.c` 머리 `callers:` 에 `FUN_1406b2db3` 가 있다 — `KiApcInterrupt` 의 떨어진 조각으로 보임 `[추측]`), `KiDeliverApc` 가 `NormalRoutine == KiSchedulerApc` 인지 비교해서 **직접 부른다** (+0x1ff `LEA`, +0x21b call / **+0x220** ret, 인자 = NormalContext = 스레드). 그 앞 +0x1f7 에서 `MOV CR8,0` 로 IRQL 을 내린다.
8. `[정적]` `KiSchedulerApc` 는 `SuspendEvent.SignalState (스레드+0x2e4)` 가 **0 이고** `TerminationApcRequest(+0x78 bit 15)` 가 꺼져 있으면 `+0x74` bit 19 (`SuspendSchedulerApcWait`) 를 켜고 **`KeWaitForSingleObject` 를 부른다** (+0xf2 call / **+0xf7** ret). 인자: 객체 = `스레드+0x2e0` (`SuspendEvent`), **WaitReason = 5 (PDB 열거형 `_KWAIT_REASON`: Suspended)**, WaitMode = `+0x74` bit 18 또는 트랩 프레임 쪽 값, Alertable = FALSE, Timeout = NULL. 돌아오면 bit 19 를 끈다 (+0xf7). (이 호출은 `KeWaitForMutexObject` 와 **같은 주소**라서 `_calls.tsv` 에 `alias:KeWaitForMutexObject+1` 로 찍힌다. 이름을 하나로 못 박지 않는다.)
9. **결론**
   - `[정적]` 멈춘 스레드가 서는 곳 = **`KiSchedulerApc +0xf2` 의 `KeWaitForSingleObject(SuspendEvent, Suspended, …)`**. 스택에는 `KeWaitForSingleObject ← KiSchedulerApc+0xf7 ← KiDeliverApc+0x220` 이 보일 것이다.
   - `[추측]` 새 스레드의 APC 는 `PspUserThreadStartup +0x22` 의 IRQL 내림 바로 뒤(인터럽트 프레임의 복귀 주소는 +0x26 부근)에서 배달되어, **`PspDisablePrimaryTokenExchange` / `PspNotifyThreadCreation` / `PspInitializeThunkContext` 가 돌기 전에** 멈출 것이다. 근거: 4~7 이 이 순서로 맞는다. 하지만 `SwapContext` 의 CL 인자가 무엇인지, 실제로 그 자리에서 인터럽트가 걸리는지는 코드만으로는 못 정한다 (동적 확인).
   - `[정적]` **풀어 주는 쪽이 먼저 오는 경주(race) 도 코드가 처리한다.** `KiResumeThread` 가 `SignalState = 1` 을 먼저 쓰기 때문에, 스레드가 `KiSchedulerApc` 에 도착했을 때 이미 1 이면 `+0x66 CMP [R15+4],0` 에서 기다림 블록을 건너뛰고 그냥 돌아간다 (기다린 적이 없으니 깨울 것도 없다).

#### 3-c. run12 `PspInsertThread +0x5a6 KeReadyThread` 와의 맞물림 (한 문단)

멈춘 스레드도 대기 줄에는 똑같이 선다. "멈춤"은 스레드를 줄에서 빼 두는 방식이 아니라, **멈춤 APC 를 먼저 걸어 두고 `SuspendEvent` 를 0 으로 내려 놓는 방식**이다. 그래서 `KeReadyThread` 가 올려 준 스레드는 CPU 가 고르는 순간 달리기 시작하고, `KiStartUserThread` → `PspUserThreadStartup` 에서 IRQL 이 내려가자마자 APC 가 처리되어 `KiSchedulerApc` 에서 `SuspendEvent` 를 기다리며 잠든다 (Waiting, 이유 Suspended). 나중에 부모가 `NtResumeThread` 를 부르면 `SuspendCount` 가 1 → 0 이 되면서 `KiResumeThread` 가 `SuspendEvent` 를 1 로 올리고 기다리던 스레드를 `KiTryUnwaitThread` 로 깨운다. 깨어난 스레드는 `KiSchedulerApc` 를 빠져나와 `KiDeliverApc` 로 돌아가 하던 시작 길(`PspUserThreadStartup` 의 나머지 → 유저 모드)로 간다. 즉 **첫 스레드는 "아직 한 번도 안 달림"이 아니라 "커널 시작 코드 맨 앞에서 잠든 상태"**일 가능성이 크다 (`[추측]`, 위 3-b 와 같은 근거).

### 질문 4. `NtAlertResumeThread` 와 `NtResumeThread` 의 차이

- 답 (한 줄): **둘 다 멈춤을 푸는데, `NtAlertResumeThread` 는 스레드에 "알림(alert)"을 먼저 켜고 푼다.** 부르는 함수·검사도 조금 다르다.
- 근거 `NtAlertResumeThread.asm` (0x786810, 225B) 대 `NtResumeThread.asm`:

| | `NtResumeThread` | `NtAlertResumeThread` |
|---|---|---|
| 핸들 접근 권한 인자 | `0x1000` (+0x75 `MOV EDX,0x1000`) | `0x2` (+0x73 `MOV EDX,0x2`) |
| 추가 검사 | 없음 | 부른 쪽이 유저 모드면 대상이 `SystemThread`(`+0x74` bit 10) 일 때 `0xc0000022` (접근 거부) 로 거절 (+0x8d~+0xb3) |
| 부르는 함수 | `PsMultiResumeThread(스레드, &이전횟수, 1)` (+0x96 / +0x9b) | **`KeAlertResumeThread(스레드)` (+0xaf / +0xb4)** |
| 안쪽 | `KeResumeThread(스레드, 1)` | `KeAlertThread(스레드, 0)` (+0x2f / +0x34) 그다음 `KeResumeThread(스레드, 1)` (+0x3c / **+0x41**), 둘을 IRQL 2 로 올린 채 이어서 한다 |
| `ProcessFirstResume` 표시 / ETW 기록 | **한다** (`PsMultiResumeThread` 안) | **안 한다** (`PsMultiResumeThread` 를 안 거침) |

  - 접근 권한 숫자 `0x1000`·`0x2` 는 코드의 값 `[정적]`. 이 값이 SDK 의 `THREAD_RESUME` / `THREAD_SUSPEND_RESUME` 와 같다는 건 헤더를 아는 것으로 본 것이다 `[추측에 가까운 상식]`.
  - `KeAlertThread` 의 두 번째 인자 0 (= 커널 모드 알림)은 asm 값(`XOR EDX,EDX`)이다.
- Q5b 에서 KERNELBASE 의 `ResumeThread` 가 어느 쪽 시스템 콜을 부르는지 보면, 위 표에서 `ProcessFirstResume`/ETW 표시가 붙는지가 갈린다.

### 질문 5. 표기 규칙

- 이번 요청 함수들 안에서 본 것:
  - **tail**: `KeSuspendThread +0xd2 tail FUN_1406f517a` (조각 = 최대 횟수 에러 경로, `ExRaiseStatus(0xc000004a)`).
  - **frag**: 위 조각, `SwapContext` 끝 조각(`frag:FUN_1406b686f+0x..`), `PspInsertThread` 쪽 조각(sast/03 에서 이미 정리).
  - **alias**: `KiSchedulerApc +0xf2 → alias:KeWaitForMutexObject+1` (`KeWaitForSingleObject` 와 같은 주소), `KeSuspendThread` 조각의 `alias:ExRaiseStatus+1` (`RtlRaiseStatus`), `KeInitThread` 의 `0x1404a10c0` (100개 무리).
  - **indirect-cfg**: 이번 핵심 함수에는 없다. `KiDeliverApc +0x743`~`+0x76f` 에 `indirect:cfg:?` 4곳이 있는데, 일반 APC 의 NormalRoutine 을 부르는 자리다. `KiSchedulerApc` 는 이 간접 호출을 안 타고 +0x21b 에서 **직접** 불린다.
  - **주인 없는 덩어리**: 이번에 뽑은 함수에는 없다. (대신 위 "스크립트 문제"의 떨어진 범위 한 곳.)
  - `INT 0x29` (`TRAP`): `KiSuspendThread +0x24b`, `KiResumeThread +0x17d`. 리스트 깨짐 검사 실패 시 fail-fast 자리라서 호출로 안 센다.

## 동적분석으로 확인해야 할 것 (dast 요청 필요: 아래)

1. **새 스레드가 정말 `KiSchedulerApc` 에서 기다리는가, 그리고 어디서 배달되는가** `[추측]`
   - 확인 거리: 부모가 `NtResumeThread` 를 부르기 전에 test.exe 첫 스레드의 스택. 기대: `KeWaitForSingleObject ← KiSchedulerApc+0xf7 ← KiDeliverApc+0x220 ← …`, 그 아래가 `PspUserThreadStartup+0x26` 근처인지.
   - 추천 멈춤점: `nt!KiSchedulerApc` 입구 (조건: `rcx` = test.exe 의 스레드, 곧 `poi(@rcx+0x220)` 가 test.exe EPROCESS). 입구에서 `dd @rcx+0x2e4 L1` 이 0 이면 곧 기다리고, 1 이면 이미 풀려서 안 기다린다 (경주). 기다리는 자리는 `nt!KiSchedulerApc+0xf2`.
2. **순서**: `nt!PspUserThreadStartup` 입구, `+0x22`(IRQL 내림), `+0x9c`(`PspNotifyThreadCreation`), `+0xae`(`PspInitializeThunkContext`) 와 `nt!KiSchedulerApc`, `nt!NtResumeThread` 중 누가 먼저인가. 이게 흐름도에서 "시작 코드 중 어디까지가 풀어 주기 전에 도는가"를 정한다.
3. **풀어 주는 순간**
   - `nt!PsMultiResumeThread` 입구 (`rcx` = 스레드, `rdx` = 이전횟수 포인터, `r8d` = 1). 콜스택 `NtResumeThread+0x9b` 확인 (`ret_off` 값). 조건은 `rcx` 의 Process 가 test.exe.
   - `nt!KeResumeThread+0x82` (`KiResumeThread` 부르기 직전): `dt nt!_KTHREAD @rbx SuspendCount SuspendEvent` — 이때 `SuspendCount` 는 0.
   - `nt!KiResumeThread+0xa6` (`KiTryUnwaitThread` 부르기 직전): 깨우는 대상.
   - "값이 바뀌는 순간" 후보: 스레드 `+0x284` (SuspendCount 0→1, 1→0), `+0x2e4` (SuspendEvent.SignalState 1→0, 0→1). 코드 쓰는 자리는 각각 `KeSuspendThread +0x60`, `KiSuspendThread +0x6c`, `KeResumeThread +0x68`, `KiResumeThread +0x20`.
4. **`KiSuspendThread` 가 어느 갈래를 타는가**: 새 스레드는 가벼운 멈춤 갈래가 아닌 쪽(+0x1f6 `KiSignalThreadForApc`)을 탈 것이다 `[추측]`. `nt!KiSuspendThread+0x83` (APC 삽입)과 `+0x1f6` 도착 여부.
5. **`PspInsertThread +0x2e5` 의 `KeSuspendThread` 가 test.exe 에서 실제로 불리는가** (flags bit0 가 켜졌는지). 안 불리면 위 3-b·3-c 는 test.exe 에 해당하지 않는다.
6. **누가 풀어 주는가 (Q5b)**: 부모(cmd.exe) 쪽 `NtResumeThread` 인지 `NtAlertResumeThread` 인지. 둘 중 어느 시스템 콜 입구(`nt!NtResumeThread` / `nt!NtAlertResumeThread`)에 서는지 보면 된다.

- 추천 멈춤점 요약: `nt!PsMultiResumeThread`, `nt!KeResumeThread+0x82`, `nt!KiResumeThread+0xa6`, `nt!KiSchedulerApc` (+`0xf2`), `nt!PspUserThreadStartup` (+`0x22`)
- 멈춤점마다 test.exe 일 때만 서게 조건을 붙여야 한다 (커널 함수라 다른 프로세스·스레드에서도 계속 불린다. `KiSchedulerApc`·`KeResumeThread` 는 특히 자주 불린다).

## 판정

**분석성공.** 질문 1·2·3-a·4 는 코드(`[정적]`)로 끝까지 답했고, 3-b 는 앞 8고리가 코드로 이어진다. 다만 "새 스레드가 `PspUserThreadStartup +0x22` 의 IRQL 내림에서 APC 를 받는다"는 마지막 연결은 `SwapContext` 인자 해석과 실제 인터럽트 발생에 달려 있어 `[추측]` 으로 남겼고, 동적 확인 거리로 넘겼다.
