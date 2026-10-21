# 2026-10-21 run13 — 첫 스레드를 멈춰 두는 장치(멈춤 APC + `SuspendEvent`)와 풀어 주는 nt 쪽 사슬 `NtResumeThread` → `PsMultiResumeThread` → `KeResumeThread` → `KiResumeThread` (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb `ntkrnlmp.pdb` GUID `C29EBFB06B78B3C020DCA66D99713F9E1`
  - run3(관찰)·run9 ~ run12(정적)와 같은 커널이다. 버전 대조: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 가 그대로 맞다 (결과서).
- 뽑은 함수 (모두 depth 0)
  - 요청서 6개: `NtResumeThread`, `PsResumeThread`, `KeResumeThread`, `KeSuspendThread`, `KiSuspendThread`, `NtAlertResumeThread`
  - 넓힌 것: `PsMultiResumeThread`, `KiResumeThread`, `KiSchedulerApc`, `KiInsertQueueApc`, `KeAlertResumeThread`, `KeInitThread`, `KiDeliverApc`, `PspUserThreadStartup`, `KeReadyThread`, `KiSignalThreadForApc`, `KiRequestSchedulerApcThread`, `KiInitializeContextThread`, `SwapContext`
  - 넓힌 이유: `NtResumeThread` 가 `PsResumeThread` 가 아니라 `PsMultiResumeThread` 를 불러서 사슬이 끊겼고, "멈춘 스레드가 서는 곳"을 따라가려면 APC 를 거는 곳·처리하는 곳·새 스레드의 첫 실행 길이 필요했다 (결과서 "범위를 넓힌 이유").
- 필드 이름은 결과서가 PDB 타입(`_KTHREAD`, `_KAPC`, `_KWAIT_BLOCK`, `_EPROCESS`, `_KWAIT_REASON` 등)으로 맞췄다. 그래서 아래 필드 이름은 `[정적]` 이다.
- 장부(Ghidra) 밖 근거 하나: `KiStartUserThread` 는 Ghidra 가 함수로 안 잡아서 `ms/decomp`·`ms/disassemble` 에 없다. 결과서가 **원본 바이트(objdump)** 로 읽었다. 아래에서 "(원본 바이트, objdump)" 라고 붙인다.

## 목적

- 대기열 **Q5a**. flow.md 2.2 의 `[공백]` "첫 스레드를 누가·언제 풀어 주나" 중 **nt 쪽**을 채운다.
  1. 풀어 주는 사슬: `NtResumeThread` → … → `KeResumeThread` 의 호출 줄. `PsResumeThread` 가 끼나.
  2. `KeResumeThread` 가 멈춤 횟수를 깎고 0 이 되면 무엇을 해서 스레드를 다시 달리게 하나.
  3. 멈춰 두는 쪽(`KeSuspendThread`/`KiSuspendThread`)이 하는 일, 멈춘 첫 스레드가 **실제로 서는 곳**, run12 `KeReadyThread`(+0x5a6) 와 어떻게 맞물리나.
  4. `NtAlertResumeThread` 와 `NtResumeThread` 의 차이 (Q5b 준비).
- "부모(cmd.exe)가 실제로 이걸 부르나" 는 이번 범위 밖이다 → **Q5b** (KERNELBASE, 다음 바퀴).

## 방법

- 정적: `_workspace/sast/08_resume-thread.md` — ntoskrnl, `^(NtResumeThread|PsResumeThread|KeResumeThread|KeSuspendThread|KiSuspendThread|NtAlertResumeThread)$` depth 0, 이어서 위 "넓힌 것"
- 결과서: `_workspace/sast/08_resume-thread_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntoskrnl/_calls.tsv`, `ms/disassemble/ntoskrnl/<함수>.asm`
  - `ms/decomp/ntoskrnl/<함수>.c` (머리말 `callers:` 줄)
- 오프셋 읽는 법 (run9 ~ 12 와 같음)
  - `call` = call 명령이 있는 자리
  - `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 이걸 쓴다.
- 이름 하나 주의: `KiSchedulerApc +0xf2` 의 대상은 `_calls.tsv` 에 `alias:KeWaitForMutexObject+1` 로 찍혀 있다. 그런데 PDB 공개 심볼 이름은 **`KeWaitForSingleObject` 하나**다 (asm 줄 주석 `ALIASES(1): KeWaitForSingleObject`, 별칭 표에도 이 주소는 무리가 아님). 그래서 이 문서와 flow.md 는 `KeWaitForSingleObject` 로 쓴다.
  - (장부 표시: `KeWaitForMutexObject` — 같은 함수의 공개 이름 둘. `KeWaitForMutexObject` 는 내보내기 표에 있는 같은 주소의 다른 이름이다.)
- KeInitThread 의 APC 루틴 주소 `0x1404a10c0` 은 이름이 100개 붙은 별칭 무리다. 하는 일로만 적는다: "아무것도 안 하는 함수" (같은 주소 이름 100개, 후보: `KiSchedulerApcNop`, `KiSchedulerApcRundown` 등).

## 결과

### 정적 — 풀어 주는 사슬 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtResumeThread` | `+0x7d` | `+0x82` | `nt!ObpReferenceObjectByHandleWithTag` (핸들 → 스레드, 접근 권한 `0x1000`) | [정적] |
| `nt!NtResumeThread` | `+0x96` | **`+0x9b`** | `nt!PsMultiResumeThread(스레드, &이전횟수, 1)` | [정적] |
| `nt!NtResumeThread` | `+0xa5` | `+0xaa` | `nt!ObfDereferenceObjectWithTag` | [정적] |
| `nt!PsMultiResumeThread` | `+0x20` | **`+0x25`** | `nt!KeResumeThread(스레드, 횟수)` | [정적] |
| `nt!PsMultiResumeThread` | `+0x6a` | `+0x6f` | `nt!EtwTiLogSuspendResumeThread` (기록이 켜졌을 때만) | [정적] |
| `nt!KeResumeThread` | `+0x4a` | `+0x4f` | `nt!KiAcquireKobjectLockSafe(스레드+0x2e0)` | [정적] |
| `nt!KeResumeThread` | `+0x82` | **`+0x87`** | `nt!KiResumeThread` (**멈춤 횟수가 0 이 될 때만**) | [정적] |
| `nt!KeResumeThread` | `+0xa0` | `+0xa5` | `nt!KiExitDispatcher` (끝에서 늘) | [정적] |
| `nt!KiResumeThread` | `+0xa6` | **`+0xab`** | `nt!KiTryUnwaitThread` (`SuspendEvent` 를 기다리는 스레드 깨우기) | [정적] |
| `nt!KiResumeThread` | `+0xca` | `+0xcf` | `nt!KiSignalThreadForApc` (가벼운 멈춤 되돌리기 갈래) | [정적] |
| `nt!KiResumeThread` | `+0x199` | `+0x19e` | `nt!KiSignalThread` (가벼운 멈춤 되돌리기 갈래) | [정적] |
| `nt!PsResumeThread` | `+0xa` | `+0xf` | `nt!PsMultiResumeThread(…, 1)` — **위 사슬에 안 낌** | [정적] |

- `PsResumeThread` (20바이트)는 `CALL` + `RET` 인 얇은 껍데기다 (꼬리호출 아님). `NtResumeThread` 는 이걸 거치지 않고 `PsMultiResumeThread` 를 바로 부른다.
  - `PsResumeThread` 의 caller 는 Ghidra 가 못 찾았다 (`callers:` 빈칸). 드라이버용 내보내기 함수로 보인다 `[추측]`.
- `KeResumeThread` 를 부르는 곳 (`callers:`): `KeAlertResumeThread`, `PsMultiResumeThread`, `PsMultiResumeProcess`.

### 정적 — 멈춰 두는 쪽 (`_calls.tsv`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `nt!PspInsertThread` | `+0x2e0` | `+0x2e5` | `nt!KeSuspendThread` (flags bit0 일 때만, run9·run12) | [정적] |
| `nt!KeSuspendThread` | `+0x45` | `+0x4a` | `nt!KiAcquireKobjectLockSafe(스레드+0x2e0)` | [정적] |
| `nt!KeSuspendThread` | `+0x66` | **`+0x6b`** | `nt!KiSuspendThread` (직전 `+0x60` 에서 `SuspendCount+1` 저장) | [정적] |
| `nt!KeSuspendThread` | `+0x86` | `+0x8b` | `nt!KiExitDispatcher` | [정적] |
| `nt!KeSuspendThread` | `+0xd2` | `tail` | 조각 `FUN_1406f517a` → 상태 `0xc000004a` 를 던짐 (멈춤 횟수가 이미 최대 `0x7f` 일 때만) | [정적] |
| `nt!KiSuspendThread` | `+0x83` | **`+0x88`** | `nt!KiInsertQueueApc(스레드+0x288 = SchedulerApc)` | [정적] |
| `nt!KiSuspendThread` | `+0x1f6` | **`+0x1fb`** | `nt!KiSignalThreadForApc` (가벼운 멈춤이 아닐 때. 새 스레드는 여기 `[추측]`) | [정적] |
| `nt!KiSuspendThread` | `+0x1de` | `+0x1e3` | `nt!KiSignalThread` (가벼운 멈춤 갈래) | [정적] |
| `nt!KiSuspendThread` | `+0x266` | `+0x26b` | `nt!KiCancelTimer` (가벼운 멈춤 갈래, 타이머가 있을 때) | [정적] |
| `nt!KeInitThread` | `+0x152` | `+0x157` | `nt!KeInitializeApc` — 스레드 안의 `SchedulerApc` 를 만든다 (NormalRoutine = `KiSchedulerApc`) | [정적] |
| `nt!KeInitThread` | `+0x163` | `+0x168` | `nt!KeInitializeEvent` — `SuspendEvent` 를 **신호 상태(1)** 로 만든다 | [정적] |

- `KeSuspendThread` 를 부르는 곳: `PspInsertThread`, `PsSuspendThread`. `KiSuspendThread` 를 부르는 곳: `KeSuspendThread`, `KiFreezeSingleThread`, `KiAdjustThreadTimer` (`callers:`).

### 정적 — 멈춘 첫 스레드가 서는 곳 (test.exe 스레드 안)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `nt!KiInitializeContextThread` | `+0x1e1` (`LEA`, 호출 아님) | — | 첫 커널 스택 자리에 `KiStartUserThread`(`0x1406afbd0`) 주소를 넣는다 | [정적] |
| `nt!KiStartUserThread` | `+0x23` | `+0x28` | 저장해 둔 시작 루틴(`rax`)을 CFG 검사 함수(`nt+0x6b77f0`) 를 거쳐 부름 | [정적] (원본 바이트, objdump) |
| `nt!PspUserThreadStartup` | `+0x1d` | `+0x22` | `nt!KiLowerIrqlProcessIrqlFlags` (`KiIrqlFlags` 가 켜졌을 때만) | [정적] |
| `nt!PspUserThreadStartup` | `+0x22` (`MOV CR8,RDI`, 호출 아님) | — | **IRQL 을 0 으로 내림** (`RDI=0`, `+0xd XOR EDI,EDI`) | [정적] |
| `nt!PspUserThreadStartup` | `+0x37` / `+0x9c` / `+0xae` | `+0x3c` / `+0xa1` / `+0xb3` | `PspDisablePrimaryTokenExchange` / `PspNotifyThreadCreation` / `PspInitializeThunkContext` | [정적] |
| (APC 인터럽트) `FUN_1406b2db3` | — | — | `nt!KiDeliverApc` — `callers:` 에 있는 조각. `KiApcInterrupt` 의 떨어진 조각으로 보임 | [정적] 조각 / [추측] 이름 |
| `nt!KiDeliverApc` | `+0x21b` | **`+0x220`** | `nt!KiSchedulerApc(스레드)` — 함수 포인터가 아니라 **직접** 부름 (`+0x1ff LEA` 로 비교) | [정적] |
| `nt!KiSchedulerApc` | `+0xf2` | **`+0xf7`** | `nt!KeWaitForSingleObject(SuspendEvent, 이유 5 Suspended, …)` (장부 표시: `KeWaitForMutexObject` — 같은 함수의 공개 이름 둘) | [정적] |

- `KiStartUserThread +0x28` 은 run4 `[관찰]` 콜스택 `nt!KiStartUserThread+0x28` → `nt!PspUserThreadStartup` 과 **오프셋까지 같다.** 사이의 CFG 검사 함수는 콜스택에 안 보였으니 넘겨주기만 하는 모양으로 보인다 `[추측]`.
- `KiDeliverApc` 의 함수 포인터 호출 4곳(`+0x748`, `+0x755`, `+0x767`, `+0x774`, `indirect:cfg:?`)은 일반 APC 의 NormalRoutine 자리다. `KiSchedulerApc` 는 그걸 안 타고 `+0x220` 에서 직접 불린다.

### 호출 관계

부모 cmd.exe 스레드 (멈춰 두기, run12 의 `+0x2e5` 아래):

```
PspInsertThread
 └─ (2) +0x2e5  KeSuspendThread                 [정적] flags bit0 일 때만
                 │  (+0x60 SuspendCount+1 저장)
                 └─ +0x6b  KiSuspendThread       [정적]
                      │  (ApcQueueable 꺼짐 → 0 반환 → KeSuspendThread +0xd7 이 횟수 되돌림)
                      │  (SuspendEvent.SignalState 1 → 0, +0x6c)
                      ├─ (1) +0x88   KiInsertQueueApc      [정적] 스레드 안 SchedulerApc 걸기
                      └─ (2) +0x1fb  KiSignalThreadForApc  [정적] 새 스레드 쪽 갈래 [추측]
                                     (아직 안 달린 스레드면 KernelApcPending=1 만 켬)
```

부모 cmd.exe 스레드 (풀어 주기, 나중 시스템 콜 — 부모가 정말 부르는지는 Q5b):

```
NtResumeThread
 ├─ (1) +0x82  ObpReferenceObjectByHandleWithTag    [정적] 핸들 → 스레드
 ├─ (2) +0x9b  PsMultiResumeThread                  [정적]
 │              └─ +0x25  KeResumeThread            [정적] SuspendCount -= 1 (+0x68)
 │                   └─ +0x87  KiResumeThread       [정적] 횟수가 0 이 될 때만
 │                        │  (+0x20 SuspendEvent.SignalState = 1)
 │                        └─ +0xab  KiTryUnwaitThread  [정적] SuspendEvent 를 기다리던 스레드 깨우기
 └─ (3) +0xaa  ObfDereferenceObjectWithTag          [정적]
```

test.exe 첫 스레드 (처음 달리기 시작한 직후, 유저로 가기 전):

```
KiStartUserThread                                   (Ghidra 함수 아님, 원본 바이트)
 └─ +0x28  PspUserThreadStartup                     [관찰] run4 + [정적] (원본 바이트)
            │  +0x22  MOV CR8,RDI (=0, IRQL 내림)   [정적]
            ═▶ APC 인터럽트 (KiApcInterrupt 조각 FUN_1406b2db3)   [추측] 배달 시점
                 └─ ?  KiDeliverApc
                        └─ +0x220  KiSchedulerApc   [정적]
                             └─ +0xf7  KeWaitForSingleObject(SuspendEvent, Suspended)  [정적]
                                  … 부모의 KiResumeThread +0xab 가 깨울 때까지 잠듦 [추측]
            (깬 뒤) +0x3c / +0xa1 / +0xb3 … → 유저 쪽 (2.3)   [정적] 호출 자리 / [추측] 순서
```

## 확인된 것

- **풀어 주는 nt 쪽 사슬은 `NtResumeThread +0x9b` → `PsMultiResumeThread +0x25` → `KeResumeThread +0x87` → `KiResumeThread +0xab` → `KiTryUnwaitThread`.** [정적]
  - `PsResumeThread` 는 이 사슬에 **없다**. `NtResumeThread` 는 `PsMultiResumeThread` 를 풀 횟수 상수 1 로 직접 부른다.
  - `NtResumeThread` 는 부른 쪽이 유저 모드(`_KTHREAD.PreviousMode` `+0x232` ≠ 0)면 이전 횟수를 돌려받을 포인터를 먼저 한 번 읽고 되써 본다 (`+0x32 ~ +0x4a`). 끝에서 `*이전횟수` 를 쓰고 (`+0xb4`) 0 을 돌려준다.
- **`KeResumeThread` 는 멈춤 횟수를 깎고, 0 이 되면 `KiResumeThread` 로 깨운다.** [정적]
  - 순서: IRQL 2 로 올림 (`+0x26 MOV CR8`) → `SuspendEvent` 잠금 (`+0x4a`) → 이전 `SuspendCount`(`+0x284`) 읽기 → `SuspendCount -= 횟수` 저장 (`+0x68`) → **새 횟수가 0 이고 얼림 표시(`+0x78` bit 14 FreezeCount)가 꺼져 있으면** `KiResumeThread` (`+0x82`) → `KiExitDispatcher` (`+0xa0`) → 이전 횟수 반환.
  - 얼린 스레드는 여기서 안 깨운다. 얼림을 푸는 쪽(`KiThawSingleThread`)이 `KiResumeThread` 를 따로 부른다 (`callers:`).
- **`KiResumeThread` 의 깨움 = "이벤트 신호 + 기다림 줄 풀기".** [정적]
  - `+0x20` 에서 `스레드+0x2e4`(`SuspendEvent.SignalState`) 에 **1 을 직접 쓴다** (`KeSetEvent` 를 안 부름).
  - `SuspendEvent` 의 기다림 줄(`+0x2e8`)을 돌며 기다림 블록마다 종류(`+0x10`)를 보고, 1 이면 **`KiTryUnwaitThread` (`+0xab`)** 로 그 스레드를 기다림에서 풀어 준다. 멈춘 스레드를 깨우는 호출이 이것이다. (2 → `KiInsertQueueInternal` `+0x241`, 4 → `KeInsertQueueDpc` `+0x3e6`)
  - 다른 갈래: "가벼운 멈춤"(아래)에서 되돌릴 때는 떼어 놨던 기다림 블록을 원래 객체 줄에 다시 끼우고, 타이머가 멈췄으면 다시 걸고, `KiSignalThreadForApc`(`+0xcf`) 또는 `KiSignalThread`(`+0x19e`) 로 민다. 블록 상태 숫자(4, 5, 6)의 이름은 PDB 에 열거형이 없어 `[추측]`.
  - "깨움 APC" 같은 건 따로 걸지 않는다.
- **`PsMultiResumeThread` 는 완전히 풀렸을 때 프로세스에 "첫 풀어줌" 표시를 켠다.** [정적]
  - `KeResumeThread` 가 돌려준 이전 횟수가 0 이 아니고 요청 횟수 이하면: `_EPROCESS +0x5fc` bit 15 (`Flags3.ProcessFirstResume`) 를 켠다 (`+0x4e` `OR.LOCK`, 이미 켜져 있으면 건너뜀). bit 20 (`EnableThreadSuspendResumeLogging`) 이 켜져 있으면 `EtwTiLogSuspendResumeThread` (`+0x6f`).
  - 이 표시를 **읽는 곳**은 이번에 안 봤다 `[공백]`.
- **멈춰 두기는 "횟수 올리기 + 멈춤 APC 걸기 + `SuspendEvent` 내리기" 셋이다.** [정적]
  - `KeSuspendThread`: 횟수가 이미 `0x7f` 면 `0xc000004a` 를 던지고 끝 (`+0xd2` tail 조각). 아니면 **`+0x60` 에서 `SuspendCount+1` 을 먼저 저장** → `KiSuspendThread` (`+0x6b`) → `KiSuspendThread` 가 0(실패)을 돌려주면 `+0xd7` 에서 횟수를 되돌림 → 이전 횟수 반환.
  - `KiSuspendThread` 순서:
    1. 스레드 잠금(`+0x40` bit0) 잡기 (`+0x27`).
    2. `ApcQueueable`(`+0x74` bit 14) 이 꺼져 있으면 아무것도 안 하고 0 반환 (`+0x4b`).
    3. `SuspendEvent.SignalState`(`+0x2e4`) 가 이미 0 이면 "이미 멈춤" 으로 보고 APC 를 또 안 걸고 1 반환 (`+0x58` → `+0x5f` `JZ`).
    4. 아니면 **`SignalState = 0`** (`+0x6c`), `SchedulerApc.Inserted`(`+0x2da`) 가 0 이면 1 로 바꾸고 **`KiInsertQueueApc(스레드+0x288)`** (`+0x88`).
    5. 두 갈래: **가벼운 멈춤** (이미 기다리는 중 등 조건이 모두 맞을 때: 기다림 블록을 객체 줄에서 떼어 놓고 `KiSignalThread` `+0x1e3`) / **그 밖** (APC 를 방금 걸었으면 **`KiSignalThreadForApc` `+0x1fb`**).
  - 아직 한 번도 안 달린 새 스레드는 "그 밖" 갈래다 `[추측]` (가벼운 멈춤 조건 중 "이미 Waiting" 이 안 맞으므로).
  - `KiSignalThreadForApc` 는 대상이 지금 이 CPU 에서 도는 스레드가 아니고 커널 모드 APC 면 먼저 **`ApcState.KernelApcPending`(`+0xc1`) = 1** 을 쓴다. 상태가 Running·Waiting 이 아니면 **표시만 켜고 끝**이다 (`.c` 읽은 만큼). 새 스레드의 State 가 0(Initialized)인지는 쓰는 줄을 못 찾아 `[추측]`.
- **멈춤 APC 와 `SuspendEvent` 는 스레드가 만들어질 때 미리 준비된다.** [정적] (`KeInitThread`)
  - `+0x152` `KeInitializeApc`: APC = `스레드+0x288`(`SchedulerApc`), NormalRoutine = **`KiSchedulerApc`**, 커널 모드, NormalContext = 그 스레드. KernelRoutine·RundownRoutine 은 아무것도 안 하는 함수 (같은 주소 이름 100개, 후보: `KiSchedulerApcNop`, `KiSchedulerApcRundown`).
  - `+0x163` `KeInitializeEvent(스레드+0x2e0, Notification, TRUE)`: `SuspendEvent` 는 **처음에 신호 상태(1)**. 그래서 `SignalState` 1 = 안 멈춤, 0 = 멈춤.
  - `+0xed` `BTS EAX,0xe`: `ApcQueueable` 을 기본으로 켠다.
  - `SchedulerApc` 는 멈춤 전용이 아니다. 끝내기(`KeRequestTerminationThread`) 등도 같은 APC 를 건다 (`KiInsertQueueApc.c` `callers:`).
- **멈춘 스레드가 서는 곳 = `KiSchedulerApc +0xf2` 의 `KeWaitForSingleObject(SuspendEvent, …)`.** [정적]
  - `KiSchedulerApc` 는 `SuspendEvent.SignalState` 가 **0 이고** 끝내라는 요청(`+0x78` bit 15)이 없으면 `+0x74` bit 19(`SuspendSchedulerApcWait`) 를 켜고 기다린다. 기다림 이유 = **5 (`_KWAIT_REASON` Suspended)**, Alertable = FALSE, Timeout 없음. 돌아오면 `+0xf7` 에서 bit 19 를 끈다.
  - 그래서 그 스레드 스택은 `KeWaitForSingleObject` ← `KiSchedulerApc+0xf7` ← `KiDeliverApc+0x220` ← … 모양일 것이다.
- **경주(race)도 코드가 처리한다.** [정적] 풀어 주기가 먼저 와서 `KiResumeThread +0x20` 이 `SignalState = 1` 을 이미 썼으면, 스레드가 `KiSchedulerApc` 에 와도 `+0x66 CMP [R15+4],0` 에서 기다림을 건너뛴다.
- **`NtAlertResumeThread` 는 "알림(alert) 켜고 풀기"이고, `PsMultiResumeThread` 를 안 거친다.** [정적]
  - 접근 권한 인자 `0x2` (`NtResumeThread` 는 `0x1000`). 부른 쪽이 유저면 시스템 스레드 대상은 `0xc0000022` 로 거절.
  - `+0xb4` `KeAlertResumeThread` → 그 안에서 `KeAlertThread(스레드, 0)` (`+0x34`) 다음 `KeResumeThread(스레드, 1)` (`+0x41`).
  - 그래서 `ProcessFirstResume` 표시와 ETW 기록은 **`NtResumeThread` 쪽만** 한다. Q5b 에서 부모가 어느 쪽을 부르는지 가를 때 쓸 수 있다.
  - `0x1000`·`0x2` 가 SDK 의 `THREAD_RESUME`·`THREAD_SUSPEND_RESUME` 라는 건 헤더 상식으로 맞춘 것 `[추측]`.
- **`KiStartUserThread +0x28` 이 코드로도 맞다.** [관찰] run4 + [정적] (원본 바이트, objdump)
  - `KiInitializeContextThread +0x1e1` `LEA` 로 새 스레드 첫 커널 스택에 `KiStartUserThread` 주소를 넣는다 (`+0x1e8` 에서 저장).
  - `KiStartUserThread` 는 IRQL 을 1 (APC_LEVEL) 로 올리고(`MOV CR8,1`) 시작 루틴을 `+0x23` 에서 부른다 → 돌아올 자리 `+0x28`.
  - 그 시작 루틴이 `PspAllocateThread +0x681` 이 넣은 `PspUserThreadStartup` 이라는 건 run4 `[관찰]` 과 맞는다. 다만 `KeInitThread` → `KiInitializeContextThread` → 스택까지 값이 넘어가는 길을 한 줄씩 따라가진 않았다 `[추측]`.
- **`PspUserThreadStartup` 의 첫 일은 IRQL 을 0 으로 내리는 것이다** (`+0x22` `MOV CR8,RDI`, `RDI=0`). 그 뒤에야 `PspDisablePrimaryTokenExchange` (`+0x3c`), `PspNotifyThreadCreation` (`+0xa1`), `PspInitializeThunkContext` (`+0xb3`). [정적]

## 추측으로 남긴 연결 (동적 확인 거리)

- `[추측]` **새 스레드의 멈춤 APC 는 `PspUserThreadStartup +0x22` 의 IRQL 내림 직후 배달된다.** 그래서 `PspDisablePrimaryTokenExchange`·`PspNotifyThreadCreation`·`PspInitializeThunkContext` 보다 **먼저** 잠든다.
  - 근거 고리 (각각은 `[정적]`): `KernelApcPending = 1` (부모 쪽 `KiSignalThreadForApc`) → 처음 CPU 에 올라갈 때 `SwapContext` 끝 조각(`frag:FUN_1406b686f+0x292` 근처)이 "다음 스레드에 `KernelApcPending` 이 있고 조건이 맞으면 APC_LEVEL 소프트웨어 인터럽트 요청"(`HalRequestSoftwareInterrupt(1)`) → `KiStartUserThread` 는 IRQL 1 → `PspUserThreadStartup +0x22` 에서 0 으로 내리는 순간 걸린 인터럽트가 들어옴 → `KiDeliverApc +0x220` → `KiSchedulerApc`.
  - 못 정한 것: `SwapContext` 조건의 CL 인자가 정말 새 스레드의 `WaitIrql`(`KeInitThread` 가 1 을 넣음)인지, 실제로 그 자리에서 인터럽트가 들어오는지. `KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 이 `KiApcInterrupt` 라는 것도 이름은 `[추측]`.
- `[추측]` **run12 와 맞물림**: 멈춘 스레드도 `PspInsertThread +0x5a6` `KeReadyThread` 로 대기 줄에 똑같이 선다. "멈춤" 은 줄에서 빼 두는 게 아니라 **멈춤 APC 를 미리 걸고 `SuspendEvent` 를 0 으로 내려 두는 것**이다. 그래서 CPU 가 고르면 달리기 시작하고, 커널 시작 코드 맨 앞에서 APC 를 처리하며 `SuspendEvent` 를 기다리며 잠든다 (Waiting, 이유 Suspended). 부모가 `NtResumeThread` 를 부르면 횟수 1 → 0, `KiResumeThread` 가 이벤트를 1 로 올리고 `KiTryUnwaitThread` 로 깨운다. 깬 스레드는 `KiSchedulerApc` → `KiDeliverApc` 로 돌아가 `PspUserThreadStartup` 나머지 → 유저 쪽으로 간다.
  - 즉 첫 스레드는 "한 번도 안 달림" 이 아니라 **"커널 시작 코드 맨 앞에서 잠든 상태"** 일 가능성이 크다.
  - 전제: test.exe 때 flags bit0 가 실제로 켜져서 `KeSuspendThread` 가 불렸어야 한다 (Q19). 안 불렸다면 이 그림은 test.exe 에 해당하지 않는다.

## 정정

- flow.md 2.2 `KeReadyThread` 의 `[추측]` "멈춤 팻말이 걸려 있으면 줄에 서도 바로 일하진 못한다" (run12 바퀴에 flow.md 에만 쓴 문장)
  - 틀린 건 아니지만 모양이 다르다: 줄에 선 멈춘 스레드는 **달리기는 시작하고**, 커널 시작 코드 맨 앞에서 멈춤 APC 를 처리하며 잠드는 것으로 보인다 (`[추측]`, 위 "추측으로 남긴 연결"). flow.md 2.2 를 고쳐 썼다. run 문서에 쓴 문장이 아니라 이전 run 문서는 건드리지 않았다.

## 아직 확인 필요

- **멈춤 APC 가 정말 `KiSchedulerApc` 에서 기다리나, 어디서 배달되나**: `nt!KiSchedulerApc` 입구 (조건: `poi(@rcx+0x220)` = test.exe EPROCESS). 입구에서 `dd @rcx+0x2e4 L1` 이 0 이면 곧 기다림, 1 이면 이미 풀림(경주). `+0xf2` 에서 `k` — `KiDeliverApc+0x220` 아래가 `PspUserThreadStartup+0x26` 근처인지.
- **순서**: `nt!PspUserThreadStartup` 입구·`+0x22`·`+0x9c`·`+0xae`, `nt!KiSchedulerApc`, `nt!NtResumeThread` 중 누가 먼저인가. 이게 "풀어 주기 전에 시작 코드 어디까지 도나" 를 정한다.
- **풀어 주는 순간**: `nt!PsMultiResumeThread` 입구에서 `k` → `NtResumeThread+0x9b` 인지. `nt!KeResumeThread+0x82` 에서 `dt nt!_KTHREAD @rbx SuspendCount SuspendEvent` (횟수 0). `nt!KiResumeThread+0xa6` 에서 깨우는 대상.
- **값이 바뀌는 순간**: 스레드 `+0x284` (SuspendCount 0→1 `KeSuspendThread +0x60`, 1→0 `KeResumeThread +0x68`), `+0x2e4` (SignalState 1→0 `KiSuspendThread +0x6c`, 0→1 `KiResumeThread +0x20`).
- **`KiSuspendThread` 가 어느 갈래를 타나**: `nt!KiSuspendThread+0x83` (APC 걸기), `+0x1f6` (`KiSignalThreadForApc`) 에 서나.
- **test.exe 에서 `KeSuspendThread` 가 정말 불리나** (Q19, 그대로).
- **부모가 `NtResumeThread` 를 부르나 `NtAlertResumeThread` 를 부르나** → Q5b (KERNELBASE 정적 → 동적은 두 시스템 콜 입구).
- 정적으로 더 볼 것
  - `ProcessFirstResume`(`_EPROCESS +0x5fc` bit 15) 를 읽는 곳.
  - `KeInitThread` → `KiInitializeContextThread` → 첫 스택까지 시작 루틴(`PspUserThreadStartup`) 값이 넘어가는 길 (Q6·Q7).
- 추천 멈춤점 (모두 test.exe 일 때만 서게 조건): `nt!PsMultiResumeThread`, `nt!KeResumeThread+0x82`, `nt!KiResumeThread+0xa6`, `nt!KiSchedulerApc` (+`0xf2`), `nt!PspUserThreadStartup` (+`0x22`). `KiSchedulerApc`·`KeResumeThread` 는 특히 자주 불린다.
