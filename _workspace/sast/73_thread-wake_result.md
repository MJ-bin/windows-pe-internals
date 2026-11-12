# 73 thread-wake — 정적 분석 결과

- 요청서: `_workspace/sast/73_thread-wake.md` (Q141c1, PE_emulator 요청 25 ㄷ-1·ㄷ-3 — 기다림·깨움과 syscall 출구)
- 앞 바퀴: `_workspace/sast/71_thread-switch_result.md` (`KiSwapThread` 가 `WaitStatus` 돌려줌, 준비 줄 넣기), `_workspace/sast/72_thread-quantum_result.md`. 겹치는 건 "sast/71 질문 N" 으로 가리키기만 한다.
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도: `run_ghidra.sh map ntoskrnl.exe KeSetEvent` (9함수), `… KeWaitForMutexObject` (2,861함수) 만들었다 → `ms/disassemble/ntoskrnl/_map/`. `KeSetEvent` 지도: 깊이 1 이 8개뿐이고 대부분 공용 도구 (락·IRQL·`KiExitDispatcher`) 라 안 펼쳐졌다. 지도로 얻은 것: "누가 깨난 스레드를 준비 줄로 보내나" 는 `KeSetEvent → KiExitDispatcher` 로 이어지는데, 그 아래는 지도에서 helper 로 접혀 있어서 직접 뽑았다 (아래).
- 뽑은 함수 (새 **16개**, 전부 depth 0): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/` 에 `.c`/`.asm`.
  - 요청서 대상: `KeWaitForMutexObject`(= PDB `KeWaitForSingleObject`, 4086B) `KiCommitThreadWait`(1413B) `KeSetEvent`(1920B) `KiTryUnwaitThread`(845B) `KeReleaseMutant`(52B) `KePulseEvent`(497B) `KiWaitSatisfyMutant`(165B) `KiWaitSatisfyOther`(45B) `KiProcessThreadWaitList`(775B) `NtWaitForSingleObject`(109B) `ObWaitForSingleObject`(321B). (`KiSystemServiceStart` 은 이미 있었다.)
  - **요청서 범위를 넓힌 곳 (이유)**:
    - `KeReleaseMutant` 는 52B 감싸기고 몸이 **`KeReleaseMutantEx`**(1521B, rva `0x2635c0`) 라서 이름으로 직접 뽑았다 (depth 2 대신).
    - 깨운 스레드가 "준비 줄" 로 가는 길이 `KeSetEvent → KiExitDispatcher(619B) → KiProcessThreadWaitList → KiDeferredReadySingleThread` 라서 `KiExitDispatcher`, 그리고 `KiEnterDeferredReadyState`(State=7 만 쓰는 85B 도우미), `KiProcessDeferredReadyList`(`+0x50` 에서 `KiDeferredReadySingleThread` 부름 — 확인용), `KiExitThreadWait` 를 뽑았다. 이 넷은 줄기 확인용으로 호출 자리만 봤다.
  - depth 1 로 안 뽑은 이유: `KeWaitForMutexObject` 의 callee 24개는 IRQL·락·RCU·자동부스트(`KeAb*`)·우선순위 도우미라서. 줄기 위 것 (`KiCommitThreadWait`) 만 뽑았다.
  - 안 읽은 것 (이름만): IRQL 함수들 (`KiRaise/LowerIrqlProcessIrqlFlags`) · 락 스핀의 `KiCheckVpBackingLongSpinWaitHypercall`/`HvlNotifyLongSpinWait` · RCU/SRCU (`KiRcu*`, `KiSrcu*`) · 자동부스트 (`KeAbPreAcquire`, `KeAbPostReleaseEx`, `KeAbPreWakeupThread`, `KiAbEntryRemoveFromTree`) · `KiPerformWaitDeferredWork` · `KiSetPriorityThread`·`KiComputeThreadPriorityNonStacking` (`KeWaitForMutexObject +0x9b5` 등, 자동부스트 되돌리기) · `KiInsertTimerTable`·`KiComputeDueTime`·`KiTimerWaitTest`·`KiTraceSetTimer` (Q141c2) · `KiInsertQueueInternal`·`KeInsertQueueDpc` (큐·DPC 기다림 블록) · `KiDirectSwitchThread` · `KiDeliverApc` · ETW (`EtwTraceReadyThread`).
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`, sast/71 의 `KiCommitThreadWait +0x220 / ret +0x225 → KiSwapThread` 를 `_calls.tsv` 에서 다시 확인.

## 스크립트 문제

없다. 참고만:

- `KiWaitSatisfyMutant`·`KiWaitSatisfyOther` 는 `KeWaitForMutexObject` 의 callee 목록에 **없다.** 이 둘은 따로 부르지 않고 **몸에 풀어 넣어져 있다** (아래 질문 1(a)). 이름으로 `_calls.tsv` 를 찾으면 안 나온다.
- 이름 주의: PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (rva `0x217eb0`). `.asm`/`.c` 파일 이름은 `KeWaitForMutexObject`. `ObWaitForSingleObject +0xba` 의 `_calls.tsv` 표기는 `alias:KeWaitForMutexObject+1`. WinDbg 에서는 `nt!KeWaitForSingleObject` 이다.
- `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0` (`pdb_aliases.py --where` 로 다시 확인: `[none]`, "KiSystemServiceStart +0x1a0").

## 읽는 법 (표기)

- `[정적]` = 코드에 이렇게 쓰여 있다. 실행 때 이 길로 간다는 뜻이 아니다.
- 칸 이름은 공개 PDB (`pdb_struct.py`) 로 확인한 것만 이름을 쓴다:
  - `_KTHREAD`: `ThreadLock +0x40` · `WaitRegister +0x70` (비트: `State` 0~2 · `Affinity` 3 · `Priority` 4 · `Apc` 5 · `UserApc` 6 · `Alert` 7) · `Alerted +0x72` · `MiscFlags +0x74` (`WaitNext` 비트 2 · `Alertable` 비트 4 · `TimerActive` 비트 9) · `ApcState.UserApcPending… +0xc2` · `Priority +0xc3` · `WaitStatus +0xc8` · `WaitBlockList +0xd0` · `WaitListEntry +0xd8` · `Queue +0xe8` · `Timer +0x100` · `WaitBlock +0x140` (4개, 하나 0x30B) · `State +0x184` · `WaitIrql +0x186` · `WaitMode +0x187` · `WaitTime +0x1b4` · `KernelApcDisable +0x1e4` · `PreviousMode +0x232` · `Preempted +0x235` · `AdjustReason +0x236` · `AdjustIncrement +0x237` · `WaitBlockCount +0x24b` · `WaitReason +0x283` · `WaitPrcb +0x2c8` · `MutantListHead +0x308` · `AbWaitObject +0x3d0` · `KernelWaitTime +0x3e0` · `UserWaitTime +0x3e8`.
  - `_KWAIT_BLOCK`(0x30B): `WaitListEntry +0x0` · `WaitType +0x10` · `BlockState +0x11` · `WaitKey +0x12` · `Thread +0x18` · `Object +0x20`.
  - `_DISPATCHER_HEADER`: `Type +0x0` (비트 7 = 헤더 락) · `SignalState +0x4` · `WaitListHead +0x8`. `_KOBJECTS`(PDB 열거형 확인): 0 EventNotification · 1 EventSynchronization · 2 Mutant · 5 Semaphore · 6 Thread · 9 TimerSynchronization.
  - `_KMUTANT`: `MutantListEntry +0x18` · `OwnerThread +0x28` · `Abandoned +0x30` 비트 0 · `AbEnabled` 비트 1 · `ApcDisable +0x31`.
  - `_KTRAP_FRAME`: `Rax +0x30` · `Rip +0x168` · `SegCs +0x170`. `KiSystemServiceStart` 에서 `RBP = TrapFrame + 0x80` 이라 `[RBP-0x50]` = `TrapFrame.Rax`.
- 이름 없는 칸 (공개 PDB 가 `_KPRCB` 를 `+0x6d0` 에서 자름 — sast/71): `PRCB+0x2d08` = **깨운 스레드를 쌓는 단일 연결 목록 머리** `[추측: 이름]`. 근거 (`[정적]`): 깨우는 쪽이 여기에 `Thread+0xd8` 를 push 하고 (`KeSetEvent +0x2e5`·`+0x47d`, `KeReleaseMutantEx +0x2ff`, `KiTryUnwaitThread +0x161`), 비우는 쪽은 `KiProcessThreadWaitList +0x1e/+0x38` 하나다. sast/71 의 `[추측: 대기 목록 머리]` 보다 뜻이 좁혀졌다.
- `_WAIT_TYPE` 열거형은 공개 PDB 에 없다. 숫자만 `[정적]`, 이름은 `[추측]`: `WaitType` 1 = 한 객체라도 풀리면 깸 (WaitAny 로 보임, `KeWaitForSingleObject` 가 이 값을 씀 `+0x1d8`), 0 = 전부 풀려야 깸 (WaitAll), 2 = 큐(완료 포트) 알림, 4 = DPC.
- 기다림 상태 숫자 (`WaitRegister.State`, 이름 없음): 0 = 블록은 달았고 아직 `KiCommitThreadWait` 가 안 끝남 · 1 = 커밋됨 (`KiCommitThreadWait +0xb6` 이 씀) · 2 = 커밋 전에 누가 깨움 · 3·5·4 는 아래 질문 3.

## 한눈에 (그림)

```
[기다리는 쪽]   NtWaitForSingleObject +0x65 → ObWaitForSingleObject +0xba → KeWaitForSingleObject(=…MutexObject)
  Thread.State=5(Waiting) 먼저 적고(+0x1a6) → 헤더 락(+0x1eb) → 신호 검사
  ├ 신호 상태 ─ 그 자리에서 가져가고(+0x5f0 이벤트 0 / +0x3e3 뮤턴트 주인 / 세마포어 -1) State=2 → 돌아감, 기다림 없음
  ├ 마감 이미 지남 ─ +0x240 STATUS_TIMEOUT(0x102) 으로 돌아감
  └ 아니면 ─ WaitBlock 을 객체 목록에 달고(+0x7b7) → KiCommitThreadWait(+0x841)
        ├ 커밋 전에 이미 깨워졌거나 APC/Alert 가 와 있음 → 안 자고 WaitStatus 로 돌아감(+0x251~)
        └ 아니면 WaitRegister.State=1(+0xb6) → (마감 있으면 타이머 +0x1f2) → KiSwapThread(+0x220)  ← 여기서 잠
           … 깨워지면 KiSwapThread 가 Thread.WaitStatus 를 돌려줌 (sast/71) → KiCommitThreadWait 가 그대로 돌려줌
  돌려받은 값이 0x100 이면 처음(+0x104)부터 다시, 아니면 그대로 반환(+0x2c4)

[깨우는 쪽]   KeSetEvent / KePulseEvent / KeReleaseMutantEx
  IRQL 2, 헤더 락 → (이벤트 신호 상태를 먼저 적고) → 기다림 블록을 앞에서부터 돌며:
     스레드 락 → Thread.State==5 이면: WaitStatus = WaitKey 를 그 자리에서 씀, State=7(DeferredReady), PRCB+0x2d08 에 push
                 (뮤턴트면 같은 자리에서 주인도 넘김: KiWaitSatisfyMutant)
  → 헤더 락 풀기 → KiExitDispatcher
        → PRCB+0x2d08 비우기: KiProcessThreadWaitList → KiDeferredReadySingleThread (sast/71 질문 3) → 준비 줄(또는 NextThread)
        → NextThread 가 생겼고 IRQL<2 이면 깨운 쪽이 그 자리에서 KiQueueReadyThread + KiSwapContext (+0x1ac)
```

## 질문별 답 [정적]

### 1. 기다리는 쪽 — `KeWaitForSingleObject`(= Ghidra `KeWaitForMutexObject`) → `KiCommitThreadWait`

인자: `RCX`=객체, `EDX`=WaitReason (`ObWaitForSingleObject +0xb5 MOV EDX,6` → `UserRequest`), `R8B`=WaitMode (유저 호출이면 `NtWaitForSingleObject +0x16/+0x1f` 로 읽은 `PreviousMode`), `R9B`=Alertable, 스택=Timeout 포인터.

**(a) 이미 신호 상태면 — 그 자리에서 채우고 돌아간다 (기다리지 않는다). 예.**

- 순서가 중요하다: `+0x1a6 MOV [RDI+0x184],5` (**State=Waiting 을 먼저 적고**), `+0x1b0` WaitReason, `+0x1c5` WaitTime, `+0x1cb ThreadLock=0`, 그다음 `+0x1d8~+0x1e7` 에 `WaitBlock[0]` 을 채운다 (`WaitType=1`, `BlockState=4`, `WaitKey=0`, `Object`), `+0x1eb BTS.LOCK [R12],7` 헤더 락. 신호가 있으면 **`+0x251 MOV [RDI+0x184],2`** 로 State 를 되돌려 놓는다.
- 갈림: `+0x205 AND CL,0x7f` 로 `Type` → `+0x208 CMP CL,2 / +0x20b JZ +0x3e3` (뮤턴트). 뮤턴트 아니면 `+0x211 TEST EDX,EDX / +0x213 JG +0x5f0` (SignalState>0 이면 신호).
- **이벤트·세마포어 가져가는 줄** (`KiWaitSatisfyOther` 가 몸에 풀려 있다 — 호출 없음):
  - `+0x5f0 AND AL,7 / +0x5f2 CMP AL,1 / +0x5f4 JZ +0x645` → `+0x645 MOV [R12+4],R10D` (R10D=0): **`SignalState = 0`** (자동 재설정 이벤트 `Type&7==1`, 타이머 동기형도 같은 모양). 끝나면 `+0x64a JMP +0x246`.
  - `+0x5f6 CMP CL,5 / +0x5f9 JNZ +0x246`, `+0x5ff LEA EAX,[RDX-1] / +0x602 MOV [R12+4],EAX`: 세마포어는 `SignalState-1`.
  - 그 밖 (수동 이벤트 `Type 0`, **스레드 객체 `Type 6`** 등) 은 `+0x5f9 JNZ +0x246` 로 **아무것도 안 바꾸고** 간다 → 신호 상태가 그대로 남는다. (test.exe 의 `WaitForSingleObject(thread)` 가 이 모양.)
  - `+0x246 AND.LOCK [R12],0xffffff7f` (헤더 락 풀기) → `+0x251 State=2` → … `+0x2c4 MOV EAX,R14D` (R14D 는 `+0x1ad` 에서 0 = STATUS_WAIT_0) → `+0x2da RET`.
- **뮤턴트 가져가는 줄** (`KiWaitSatisfyMutant` 가 몸에 풀려 있다): `+0x3e3` 에서 `AbEnabled` 비트 저장 → `+0x3f0 TEST EDX,EDX / +0x3f2 JLE +0x733`.
  - `SignalState ≤ 0` (누가 가졌다): `+0x733 CMP RDI,[R12+0x28]` / `+0x738 JNZ +0x219` — **주인이 나면** `+0x3f8` 으로 (재귀), 아니면 기다리러 `+0x219`.
  - 재귀 한도: `+0x3fd CMP EAX,0x80000000 / +0x402 JZ +0xbb2` → `+0xbc4 KiFastExitThreadWait`, `+0xbc9~+0xbce ExRaiseStatus(0xc0000191)`.
  - `+0x408 SUB EAX,1 / +0x40b MOV [R12+4],EAX / +0x410 JNZ +0x246` — 0 이 아니면 (이미 내가 가졌다 = 재귀 획득) 바로 끝.
  - 0 이 되면 (비었다) **가져간다**: `+0x416 MOV [RDI+0xc8],0` (WaitStatus=0) → `+0x444~+0x44c` `ApcDisable` 이면 `KernelApcDisable-1` → **`+0x489 MOV [R12+0x28],RDI` (OwnerThread=나)** → `+0x48e~+0x49e` `Abandoned` 비트가 서 있으면 끄고 **`+0x499 OR [RDI+0xc8],0x80`** (→ 질문 4(c)) → `+0x4c6~+0x4e9` 내 `MutantListHead(+0x308)` 맨 뒤에 `MutantListEntry` 를 꿴다 → `+0x4ed` 락 풀기, `+0x4f6 State=2`, `+0x507 MOV RSI,[RDI+0xc8]` (돌려줄 값 = WaitStatus).
- 기다림 없이 끝나는 다른 갈래 (줄만): 마감을 **엄격히 넘겼으면** (`+0x356 CMP RCX,RAX / +0x359 JA +0x240`, 상대 마감) 또는 절대 마감값이 0 이면 (`+0x237 TEST R8,R8` 이 0, `JNZ +0x60c` 안 탐) `+0x240 R14D=0x102` (STATUS_TIMEOUT). 같은 시각이면 이 검사를 지나 블록을 달고 `KiCommitThreadWait` 의 타이머 쪽에서 정해진다 (`+0x1f9 JZ +0x395` → `KiTimerWaitTest`, Q141c2) · 이미 `Alerted` `+0x37d`/`+0xb32` → `0x101` · 유저 APC 대기 `+0x3a8 R14D=0xc0` (`STATUS_USER_APC`).

**(b) 신호가 아니면 — 달고, 커밋하고, 잔다.**

- 블록 달기: `+0x35f~+0x36c` (꼬리 검사 `[[Object+0x10]] == &Object->WaitListHead`) → **`+0x7b7~+0x7c1`** `InsertTailList(&Object->WaitListHead, &WaitBlock->WaitListEntry)` (맨 뒤) → `+0x7c5 AND.LOCK [R12],~0x80` 헤더 락 풀기 → `+0x7d2 MOV [RDI+0x24b],1` (`WaitBlockCount=1`).
  - `WaitBlock.Thread (+0x18)` 를 쓰는 줄은 이 함수에 없다 (스레드 만들 때 미리 채워 둔다 `[추측: KeInitThread, 안 확인]`).
- `KiCommitThreadWait(Thread, &WaitBlock, 마감종류, 마감값, …)` 호출: **`+0x841 / ret +0x846`**. 마감종류는 `+0x837 MOV R8D,R14D` (0 = 없음, 1 = 절대, 2 = 상대).
- 커밋 안 (`KiCommitThreadWait.asm`):
  - `+0x38 TEST R8D,R8D / +0x3b JNZ +0x23d` → `+0x23d BTS [RCX+0x74],9` (**`TimerActive`**) + `+0x242 OR [RCX+0x100],0x40000080` (Thread->Timer 머리) — 마감이 있을 때만.
  - `+0x5b MOV [RCX+0xd0],RSI` (`WaitBlockList`), `+0x71 BTS.LOCK [RBX+0x40]` (스레드 락).
  - **`+0x97 MOV AL,[RBX+0x70]` / `+0xa1 TEST AL,0xe7` / `+0xa3 JNZ +0x251`**: `WaitRegister` 에 State·APC·Alert 가 이미 있으면 **자지 않는다** (아래 (c)).
  - 없으면: **`+0xac AND AL,0xf9 / +0xb6 OR AL,SIL(1) / +0xbf MOV [RBX+0x70],AL`** (`WaitRegister.State = 1`) → `+0xc2 / ret +0xc7 KiPerformWaitDeferredWork`.
  - **`+0x160 MOV byte [RBX+0x235],0`** (`Preempted=0`) — 자기 전에 지운다 (→ sast/71 질문 3 의 "Preempted==0 이면 맨 뒤").
  - 마감: `+0x167 TEST R12D,R12D / +0x16a JZ +0x216` (마감 없으면 건너뜀). 있으면 `+0x170 CMP R12D,2 / +0x174 JNZ +0x4d6` 로 갈린다 — 2 (상대, 이미 인터럽트 시간 기준 시각으로 바뀌어 옴) 는 그대로 `+0x17a~+0x1be` (`+0x194 MOV [RDI+0x18],RCX` 만기 시각을 Timer 에 적음), 1 (절대) 은 `+0x4d6` → `+0x4e3 / ret +0x4e8 KiComputeDueTime` 로 바꾼 뒤 합류. 그리고 **`+0x1f2 / ret +0x1f7 KiInsertTimerTable`**; 이미 만기면 (`+0x1f9 JZ +0x395`) `+0x39e KiTimerWaitTest`. (만기 처리 자체는 Q141c2.)
  - **`+0x220 CALL KiSwapThread` (ret `+0x225`)** → `+0x225 MOV R15,RAX` → `+0x228 MOV RAX,R15` → `+0x23b RET`. 아래 (c).

**(c) 깨난 뒤 — `WaitStatus` 를 그대로 돌려준다. 다시 묻지 않는다. 단 `0x100` 이면 처음부터 다시.**

- `KiCommitThreadWait`: `KiSwapThread` 반환값을 **손대지 않고** 반환 (`+0x225~+0x23b`, 사이에 계산 없음). `KiSwapThread` 가 `Thread.WaitStatus(+0xc8)` 를 돌려주는 줄은 sast/71 질문 1(c) (`+0x25c`, `+0x29a`).
- `KeWaitForSingleObject`: `+0x846 MOV R14,RAX` → `+0x859 MOV [RDI+0x3d0],0` (`AbWaitObject` 지움) → **`+0x860 CMP R14D,0x100 / +0x867 JNZ +0x2c4`** → `+0x2c4 MOV EAX,R14D` → `RET`. 즉 **하위 32비트만** 돌려준다.
- **반복 조건은 이것 하나**: 값이 `0x100` (`STATUS_KERNEL_APC`) 이면 `+0x86d MOV RSI,CR8` · `+0x876 MOV CR8,RAX(2)` (IRQL 2 로 올림) · `+0x88d MOV [R13],SIL` (`WaitIrql` 복원) · **`+0x891 JMP +0x104`** (함수 첫머리 근처 반복 지점. 헤더 락부터 **전부 다시 검사**: 신호 상태면 가져가고, 아니면 다시 블록을 달아 다시 잔다). 상대 마감은 처음에 계산해 둔 시각을 그대로 쓴다 (`+0x104` 위에서 `[RBP-0x51]` 에 한 번만 적고 `+0x3de JMP +0x104`: `+0x3b6~+0x3d7`) — 다시 돌 때 새로 늘어나지 않는다.
- 그 밖의 값 (`0`, `0x80`, `0x102`, `0x101`, `0xc0`, 에러) 은 **그대로 반환**. `0x100` 을 만드는 쪽: `KiCommitThreadWait +0x4fa MOV [RBX+0xc8],0x100` (`WaitRegister.Apc` 비트 `+0x435 TEST AL,0x20`), 그리고 **`WaitType==0` (전부 기다림) 블록을 깨울 때 깨우는 쪽이 일부러 `0x100` 을 넘긴다** (질문 2(c)).
- 커밋 전 이미 끝난 기다림 (`KiCommitThreadWait +0xa3 JNZ +0x251`): `+0x251 R13D=0x101`; `WaitRegister.State` 가 0 이면 (`+0x257 TEST AL,7 / +0x259 JZ +0x435`) APC·Alert 때문이다 → `+0x4fa` 0x100 (APC) 또는 `+0x451 MOV [RBX+0xc8],RAX` 로 `0x101`/`0xc0` (Alert/UserApc, `+0x43d~+0x44e` 계산) 을 쓰고, State≠0 이면 **누가 이미 `WaitStatus` 를 써 놨다** → `+0x262 State=2` · 달린 블록 떼기 (`+0x2a0~+0x32a`: `BlockState==4` 인 것만 `+0x2f4 CMP [RSI+0x11],4`, 객체 락 `+0x2ef KiAcquireKobjectLockSafe`) · `+0x287 MOV R15,[RBX+0xc8]` (그 값을 반환) · `+0x2de / ret +0x2e3 KiExitThreadWait`. 이 경우 `KiSwapThread` 로 안 간다 (잠들지 않음).

**지금 우리 (PE_emulator) 와 비교:**
- "스레드를 잠듦으로 표시하고 다른 스레드로 넘김" — **맞다.** 순서만 다르다: 진짜는 State=5 를 먼저 적고 **신호 검사 → 신호면 되돌림**.
- "신호면 안 잔다" 가 없으면 **모자라다**: 진짜는 들어가자마자 그 자리에서 이벤트 0 / 뮤턴트 주인 / 세마포어 -1 을 하고 돌아간다.
- "깨난 스레드가 자기 syscall 을 다시 묻는다" — **틀리다.** 단일 객체 기다림은 `WaitStatus` 를 그대로 돌려준다. 다시 검사하는 건 `0x100` 일 때뿐.

### 2. 깨우는 쪽 — `KeSetEvent` (`KeSetEvent(Event=RCX, Increment=EDX, Wait=R8B)`)

**(a) 자동/수동 재설정 — 예 (아래 줄).** 한 스레드를 깨울 때마다 이벤트를 **그 자리에서** 0 으로 내린다. 신호 상태로 남지 않는다. 수동은 모두 깨우고 신호로 남는다.

- 공통: `+0x12 TEST byte [RCX],0x7f / +0x20 JNZ +0x137` (Type≠0 → 자동 재설정 → `+0x137 XOR EDI,EDI`) · Type==0 (수동) 이면 `EDI=1` (`+0x30`). 수동이고 이미 `SignalState==1` 이고 `Wait==0` 이면 락도 안 잡고 `+0x147 MOV EAX,1 / +0x155 RET` (`+0x26 CMP [R15],1 / +0x2a JZ +0x13e / +0x13e TEST R8B,R8B / +0x141 JNZ +0x30`).
- `+0x3a~+0x48` IRQL 2 로 (`MOV CR8,RCX`), `+0x64 BTS.LOCK [RSI],7` 헤더 락, **`+0x74 MOV EAX,[R15]` (옛 SignalState) → `+0x86 MOV [RSP+0x90],EAX` (돌려줄 값으로 저장) → `+0x8d MOV [R15],1` (SignalState 를 먼저 1 로)** → `+0x94 TEST EAX,EAX / +0x96 JNZ +0xe3` (이미 신호면 아무도 안 깨우고 락만 풀고 끝).
- **자동 재설정 (EDI=0)**: `+0xa8` 부터 기다림 블록을 맨 앞에서 하나씩. 블록 하나를 떼고 (`+0xb7~+0xc4`, `+0x313~+0x316` `RemoveEntryList`) → `WaitType` 에 따라 (`+0x31a MOVZX EAX,[RDI+0x10]`, `+0x31e CMP AL,1`) → 깨움에 **성공**하면 (`+0x470 MOV R14B,1`, 미커밋 `+0x4cd`) `+0x4ab TEST R14B,R14B / +0x4ae JZ +0xa8(실패면 다음 블록)` / `+0x4b4 JMP +0xd1` → **`+0xd1 SUB dword [R15],1`** (SignalState 1→0) → `+0xd5 JNZ +0x781` (0 이 아니면 계속) 아니면 `+0xdb` 로 끝. → **한 스레드만 깨우고, 이벤트는 0 으로 내려가서 끝난다.** 깨울 스레드가 하나도 없으면 (`+0xa8 CMP R12,RAX / +0xab JZ +0xdb`) SignalState 는 1 로 남는다.
  - 이미 다른 쪽이 깨운 스레드 (State≠5 등) 를 만나면 성공이 아니라 SignalState 를 안 줄이고 **다음 블록** 으로 간다.
  - 큐 알림 블록 (`WaitType==2`): `+0x543 [RDI+0x11]=5` · `+0x54e KiInsertQueueInternal` · `+0x553 SUB [R15],1` (이것도 한 몫으로 셈). DPC 블록 (`==4`): `+0x711 MOV [R15],0` · `+0x71f KeInsertQueueDpc`.
- **수동 재설정 (EDI=1)**: `+0x1a3` 부터 **전부** 돈다. 블록을 하나씩 떼지 않고 끝에서 목록 머리를 비운다 (`+0x4fe~+0x506`: `Flink=Blink=머리`). 깨운 스레드마다 SignalState 를 줄이는 줄이 **없다** → **1 로 남는다.**

**(b) 깨울 때 `WaitStatus` 를 쓰는 줄 — 값 = 그 기다림 블록의 `WaitKey`.**

- `WaitKey` 는 `WaitBlock +0x12` 의 16비트. 읽는 줄: 수동 `+0x1c0 MOVZX R13D,word [R14+0x12]`, 자동 `+0x326 MOVZX R13D,word [RDI+0x12]`. **쓰는 줄** (`MOV [RBX+0xc8],R13`):
  | 경우 | 수동 재설정 루프 | 자동 재설정 루프 |
  |---|---|---|
  | 커밋된 기다림 (`WaitRegister.State` 1 또는 4) | `+0x2ec` (그 앞 `+0x2ca / ret +0x2cf KiEnterDeferredReadyState`, `+0x2db~+0x2e5` 에서 PRCB 목록에 push) | `+0x484` (그 앞 `+0x469 MOV [RBX+0x184],7`, `+0x473~+0x47d` push) |
  | 커밋 전 기다림 (State 0) | `+0x171` (그 앞 `+0x168~+0x16e` `WaitRegister=(WR&0xfa)|2`) | `+0x4d6` (그 앞 `+0x4ca~+0x4d3`) |
- 둘 다 곧바로 `AbWaitObject=0` (`+0x178`/`+0x2f3`/`+0x48b`/`+0x4dd`). `KeWaitForSingleObject` 는 `WaitKey=0` 으로 달기 때문에 (`+0x1e2`) 쓰이는 값은 **`0` = `STATUS_WAIT_0`**. (여러 객체 기다림이면 번호 n → `STATUS_WAIT_0+n` 이 되는 모양이지만 `KeWaitForMultipleObjects` 는 이번에 안 읽음 `[공백]`.)

**(c) 깨우는 함수 — 한 객체 기다림은 `KeSetEvent` 자신이 `KiTryUnwaitThread` 의 몸을 풀어 넣어 처리. 진짜 `KiTryUnwaitThread` 는 "전부 기다림 (WaitType 0)" 블록에만 부른다.**

- `KeSetEvent` 에서 `KiTryUnwaitThread` 를 **부르는 자리는 둘뿐**: **`+0x6a6 / ret +0x6ab`** (자동 루프, `R8D=0x100`, `R9D=0`) · **`+0x6d0 / ret +0x6d5`** (수동 루프, 같은 인자). `WaitType` 이 1·2·4 가 아닌 블록, 곧 **0 (전부 기다림)** 일 때다 (`+0x693 CMP AL,4` · `+0x6b9 CMP AL,4`). 이 경우 넘기는 `WaitStatus` 는 **`0x100`** — 일부러 `STATUS_KERNEL_APC` 를 써서 깨난 쪽이 **처음부터 다시 검사** 하게 한다. 이때는 이벤트도 안 내린다 (자동 루프의 `+0x6b4 JMP +0xa8` 는 `SUB [R15],1` 을 건너뜀).
- `WaitType==1` (한 객체 기다림) 은 호출 없이 몸에 풀어 넣었다: 수동 `+0x1b3~+0x2fe`, 자동 `+0x31a~+0x4f0`. 안 순서는 `KiTryUnwaitThread` 와 같다 (질문 3).
- `KePulseEvent` 는 풀어 넣지 않고 진짜 `KiTryUnwaitThread` 를 부른다 (질문 5). `KeReleaseMutantEx` 는 풀어 넣은 쪽 + `KiWaitSatisfyMutant` 호출 + `WaitType==0` 에 `KiTryUnwaitThread` (`+0x472`, `R8D=0x100`, `R9D=0`).
- `KiProcessThreadWaitList` 는 **깨우는 함수가 아니라 깨운 뒤 준비 줄로 보내는 함수**다. `PRCB+0x2d08` 에 쌓인 스레드를 비워서 (`+0x1e MOV RBX,[RCX+0x2d08]` · `+0x38 MOV [RCX+0x2d08],0`) 스레드마다 `AdjustReason(+0x236)=EDX` · `AdjustIncrement(+0x237)=R8B` 를 쓰고 (`+0x98`·`+0x9f`) **`+0x101 / ret +0x106 KiDeferredReadySingleThread`** (sast/71 질문 3 의 그 함수) 를 부른다. 스택이 안 올라와 있는 스레드는 `+0x303 / ret +0x308 KiInswapAndReadyThread` (이름만).
- **누가 `KiProcessThreadWaitList` 를 부르나**: `KeSetEvent` 가 직접 부르지 않는다. `KeSetEvent +0x10c / ret +0x111 KiExitDispatcher` → `KiExitDispatcher +0x18 CMP [RCX+0x2d08],0 / +0x23 JZ +0x46` (비었으면 건너뜀) → **`+0x41 / ret +0x46 KiProcessThreadWaitList`** (`EDX=R8D=1` 이 `AdjustReason`, `R8D=R9D` 가 `Increment`). 또 `KiSwapThread +0x14~+0x2a` (sast/71) 와 `KiCommitThreadWait +0x56a / ret +0x56f` (`+0x2c6 CMP [RDI+0x2d08],0` 에서 비어 있지 않으면) 도 부른다.
- **`Wait` 인자가 참이면 (`KeSetEvent` 의 셋째)** `KiExitDispatcher +0x25 TEST BL,BL / +0x27 JNZ +0xff` → `+0x10a / ret +0x10f KiDirectSwitchThread` 로 곧장 깨운 스레드에게 넘기는 갈래가 있다 (이름만). 일반 `Wait=FALSE` 면 위 `KiProcessThreadWaitList`.
- 준비 줄 맨 앞/뒤: 깨난 스레드는 `AdjustReason=1`(= `KiExitDispatcher` 의 `R8D=1`) 이라 `KiDeferredReadySingleThread +0x3e CMP [RDX+0x236],1 / +0x67 JNZ` 을 안 타고 `AdjustIncrement(+0x237)` 를 읽는 갈래 (`+0x6d`) 로 들어간다. **올려 주는 양은 `KeSetEvent` 의 `Increment` 인자 그대로 `+0x237` 에 적힌다** (`+0x5 MOV [RSP+0x10],EDX` → `+0xf2 MOV R9D,[RSP+0x98]` → `KiExitDispatcher R9D` → `KiProcessThreadWaitList R8D`). 우선순위 16 이상이면 계산 갈래가 다르다 (`+0xdb CMP [RBX+0xc3],0x10 / +0xe5 JGE`) — 계산 본체는 안 읽음 (`KiBoost*` 류, 이름만). 앞/뒤는 `Preempted(+0x235)` 가 `KiCommitThreadWait +0x160` 에서 이미 0 이므로 **뒤** 가 되리라 보이지만 이 갈래의 `[RSP+0x58]` 초깃값 (`+0x63 MOV [RSP+0x58],EDI`, 0) 에 기댄 `[추측]` (sast/71 한계 그대로).
- 깨운 쪽이 그 자리에서 **다른 스레드로 넘어갈 수 있다**: `KiDeferredReadySingleThread +0x1ce2 / ret +0x1ce7 KiCommitRescheduleContextEntry` 가 `PRCB.NextThread` 에 쓰면 (sast/71 질문 2(a)), `KiExitDispatcher +0x56 MOV RAX,[RDI+0x10]` · `+0x5a CMP SIL,2 / +0x5e JNC +0xd4` (이전 IRQL≥2 면 안 함) · **`+0x69 TEST RAX,RAX / +0x6c JNZ +0x134`** → `+0x187 MOV [RBP+0x283],0x20` (`WaitReason=WrPreempted`) · **`+0x19d / ret +0x1a2 KiQueueReadyThread`** (깨운 쪽 자기를 준비 줄에) · **`+0x1ac / ret +0x1b1 KiSwapContext`**. 즉 `KiSwapThread` 를 안 거치는 **또 하나의 넘기기 길**이다. (run75 `[관찰]` "넘김 5번이 `KiSwapThread` 식과 안 맞음" 의 단서 `[추측]`.)

**(d) 돌려주는 값 — 옛 `SignalState`.**

- `+0x86 MOV [RSP+0x90],EAX` → `+0x111 MOV EAX,[RSP+0x90]` → `+0x135 RET`. 이미 신호였으면 1, 신호가 아니었으면 0. (수동 + 이미 신호 + `Wait==0` 이면 `+0x147 MOV EAX,1`.)

**지금 우리 (PE_emulator) 와 비교:**
- "깨움 = 실행 가능 표시만, 깨난 쪽이 다시 물음" — **틀리다.** 깨우는 쪽이 (자동) 이벤트를 0 으로, `WaitStatus` 를 쓰고, 깨난 쪽은 안 묻는다. (다시 묻기는 `WaitAll` 쓰레드뿐, `0x100`.)
- 수동 이벤트: 전부 깨우고 신호 상태 유지 — 에뮬레이터의 지금 동작이 이거면 **맞다.** 자동 재설정: **첫 하나만 깨우고 그 자리에서 0** 을 해야 한다.
- 깨우는 쪽이 `Increment` 를 `AdjustIncrement` 에 적고 준비 줄에 넣는다 — 우선순위 올림 계산이 없으면 **모자라지만** 지금 (우선순위 없음) 에선 건너뛰어도 된다 `[추측: 에뮬레이터 범위]`.

### 3. `KiTryUnwaitThread` (`(Prcb=RCX, WaitBlock=RDX, WaitStatus=R8, 깨운스레드를적어줄칸=R9)` → `AL`: 1 이면 깨움)

`KeSetEvent`·`KePulseEvent`·`KeReleaseMutantEx` 는 R9 = 0 으로 부른다 (`KeSetEvent +0x697`·`+0x6c1`, `KePulseEvent +0x8f`·`+0x167`, `KeReleaseMutantEx +0x466`). R9 가 0 이 아닌 쪽 (뮤턴트 넘기기까지 이 함수가 하는 갈래, `+0x237~+0x2e0`) 은 이번 대상 중 부르는 곳이 없다. 다른 호출 지점 (이 함수의 caller 32곳) 은 안 봤다 `[공백]`.

- `+0x1e MOV RBX,[RDX+0x18]` (스레드 = `WaitBlock.Thread`) → **`+0x33 BTS.LOCK [RBX+0x40]`** 스레드 락 스핀.
- **경쟁을 지나치는 곳**: `+0x57~+0x60 MOVZX EAX,[RBX+0x184] / CMP AL,5 / JNZ +0x186` — `State≠5` (이미 다른 쪽이 깨움 · Running 등) 이면 **아무것도 안 하고** `+0x186` 으로 (AL=0). 그리고 `WaitRegister.State` (`+0x66~+0x6f` `AND EAX,7`) 로 갈린다:
  | `WaitRegister.State` | 줄 | 하는 일 |
  |---|---|---|
  | 1 또는 4 (커밋됨) | `+0x72 JNZ +0x2e5` → `+0x2e5 CMP EAX,4 / +0x2e8 JZ +0x78` | **깨움** (아래) |
  | 0 (커밋 전) | `+0x2ee TEST EAX,EAX / +0x2f0 JNZ +0x316` 안 탐 → `+0x2f2 AND CL,0xfa / +0x2f5 OR CL,2 / +0x2f8 MOV [RBX+0x70],CL` | `State=2` 로 바꾸고 `+0x2fb MOV [RBX+0xc8],R12` (WaitStatus) · `+0x302 [RBX+0x3d0]=0` · `+0x30d MOV [RBP+0x11],SIL(0)` → `+0x311 JMP +0x17a` (깨움 성공 표시) |
  | 5 | `+0x316 CMP EAX,5 / +0x319 JNZ +0x330` → `+0x31b AND CL,0xfe / +0x31e OR CL,6 / +0x321 MOV [RBX+0x70],CL` | `State=6` 으로 바꾸고 **깨우지 않는다** (AL=0) — 다른 쪽이 이미 깨우는 중이라는 표시로 보인다 `[추측]` |
  | 3 | `+0x330 CMP EAX,3 / +0x339 MOV [RBP+0x11],2` | 블록 `BlockState=2` 만 적고 깨우지 않는다 |
  | 그 밖 (2·6·7) | 위 어디에도 안 걸림 | 아무것도 안 함 |
- **깨움 (State 1·4) 의 줄**: `+0x78~+0x91` `Thread.Queue(+0xe8)` 가 있으면 큐 동시성 카운트 +1 (`INC.LOCK [RDX+0x28]`, `Type 0x15` 이면 `[RDX+RAX*4+0x218]`) · **`+0x95~+0x100` 이 스레드를 `WaitPrcb(+0x2c8)` 의 대기 목록에서 뗀다** (`WaitPrcb` 락 `+0xa3 BTS.LOCK [RDI+0x83d0]`, `RemoveEntryList(Thread+0xd8)`, `+0x100 [RBX+0x2c8]=0`) · `+0x114~+0x142` 기다린 시간을 `KernelWaitTime(+0x3e0)`/`UserWaitTime(+0x3e8)` 에 더함 (`WaitMode(+0x187)` 로 갈림) · **`+0x149 MOV [RBX+0x184],7`** (State=DeferredReady) · **`+0x150~+0x161`** `Thread+0xd8` 를 `[R14+0x2d08]` 맨 앞에 push · **`+0x168 MOV [RBX+0xc8],R12`** (`WaitStatus`) · `+0x16f [RBX+0x3d0]=0` · `+0x17a MOV SIL,1`.
- **기다림 블록들을 객체 목록에서 떼는 줄은 이 함수에 없다.** 이 함수는 블록의 `BlockState(+0x11)` 만 만진다 (`+0x30d` 0 으로, `+0x339` 2 로, 끝 `+0x19c~+0x1a7 INC byte [RBP+0x11]` 로 +1). 객체 목록에서 떼는 건 **부른 쪽**이다: `KeSetEvent` 자동 루프 `+0x313`, 수동 루프는 끝에서 머리째 비움 `+0x4fe`, `KeReleaseMutantEx +0x247`. 같은 스레드의 **나머지 블록** (여러 객체 기다림) 은 깨난 스레드가 나가면서 뗀다: `KiCommitThreadWait +0x2b2~+0x32a` (`BlockState < 5` 이고 `== 4` 인 것만). 한 객체 기다림 (`KeWaitForSingleObject`) 은 블록이 하나라 깨우는 쪽이 이미 뗐으니 깨난 쪽은 따로 안 뗀다 (`KeWaitForSingleObject +0x846~+0x867` 에 해당 줄 없음).
- 끝: `+0x194 MOV [RBX+0x40],0` (스레드 락 풀기) → `+0x19c MOVZX ECX,[RBP+0x11]` · `+0x1a5 INC CL` · `+0x1a7 MOV [RBP+0x11],CL` → `+0x1b9 RET`. 락 풀기와 `BlockState+1` 은 깨움 성공이든 지나침이든 **항상** 한다.
- 깨우는 순서 한 줄: 스레드 락 → State==5 확인 → `WaitRegister` 로 갈림 → (커밋됨이면) 대기 목록에서 떼기 → State=7 → 목록에 push → `WaitStatus` 쓰기. 두 CPU 가 동시에 오면 먼저 스레드 락을 잡은 쪽이 State 를 7 로 바꾸므로 뒤에 온 쪽은 `State≠5` 로 지나친다 (줄 근거는 위, 실제 순서는 런타임).

**지금 우리 비교:** 에뮬레이터는 CPU 하나·한 번에 한 스레드라서 스레드 락·`WaitRegister` 의 경쟁 갈래는 필요 없다 `[추측: 에뮬레이터 범위]`. 단 "이미 깨운 스레드 (State≠Waiting) 면 지나친다" 는 필요하다 (한 스레드가 두 객체를 기다릴 때).

### 4. `KeReleaseMutant`

**(a) 몸 함수 — `KeReleaseMutantEx`** (rva `0x2635c0`, 1521B). `KeReleaseMutant +0x26 / ret +0x2b`. `KeReleaseMutant(Mutant, Increment, Abandoned=R8B, Wait=R9B)` 가 하는 일은 `Flags` 만들기뿐: `+0x6 TEST R8B,R8B / +0xd SETNZ AL / +0x10 ADD EAX,4` → `Flags = Abandoned + 4`; `+0x16 OR R8D,2 / +0x1a TEST R9B / +0x22 CMOVZ R8D,EAX` → `Wait` 이면 `|2`. 비트 0 = 버림 (`Abandon`), 비트 1 = `Wait`, 비트 2 = 오류를 반환값 대신 예외로 (`ExRaiseStatus`, `KeReleaseMutantEx +0x509 TEST BPL,4`). 이전 상태를 `[RSP+0x40]` 에 받아 `+0x2b MOV EAX,[RSP+0x40]` 로 돌려준다.

**(b) 횟수가 0 이 되면 기다리는 첫 스레드에게 그 자리에서 주인을 넘기나 — 예.** `OwnerThread` 를 깨운 스레드로 쓰는 일은 **놓는 쪽 (`KeReleaseMutantEx`) 이 `KiWaitSatisfyMutant` 를 불러서 한다.**

- 놓기 전 검사: 락 `+0x6a BTS.LOCK [RDI],7`, `+0x7a MOV EBX,[RDI+4]` (옛 SignalState), `+0x84 TEST BPL,1 / +0x88 JNZ +0x550` (버림 갈래). **주인이 아니면** `+0x93 CMP [RDI+0x28],RAX / +0x97 JNZ +0x4ce` (그리고 `+0x9d~+0xa8` 헤더 `+2` 바이트와 `PRCB+0x38ba` 바이트 비교도 같은 곳으로) → `+0x4fc MOV EAX,0x80` · `+0x501 MOV ECX,0xc0000046` · `+0x506 CMOVZ EAX,ECX` → `Abandoned` 가 서 있으면 `0x80` 아니면 **`0xc0000046` (`STATUS_MUTANT_NOT_OWNED`)** 반환 (비트 2 면 `+0x515 ExRaiseStatus`).
- 주인이면: **`+0xbd LEA EAX,[RCX+1] / +0xc0 MOV [RDI+4],EAX`** (SignalState +1) → `+0xc5 JNZ +0x189` (옛 값이 0 이 아니었으면 = 아직 재귀로 갖고 있음 → 락만 풀고 끝) / `+0xcb TEST EBX,EBX / +0xcd JG +0x189`. 옛 값이 0 이어서 **1 (빔) 이 되면** 계속:
  - 주인의 `MutantListHead` 에서 떼고 (`+0x110` 주인 스레드 락, `+0x134~+0x152` `RemoveEntryList(Mutant+0x18)`) · **`+0x15a MOV [R14],R12` (`OwnerThread = 0`)**.
  - 기다림 블록을 앞에서부터 (`+0x15d MOV R12,[RAX]`, `+0x160 CMP R12,RAX`): 블록을 떼고 (`+0x247`) `WaitType==1` 이면 스레드 락 → State==5 → **`WaitStatus = WaitKey`** 쓰기 (커밋됨 `+0x306 MOV [RBX+0xc8],RAX`, 그 앞 `+0x2d7 KiRemovePrcbWaitEntry`·`+0x2df KiEnterDeferredReadyState`·`+0x2f0~+0x2ff` push; 커밋 전 `+0x4a0`) → **`+0x330 CALL KiWaitSatisfyMutant` (ret `+0x335`; 인자 `RCX`=뮤턴트, `RDX`=깨운 스레드 `+0x32d MOV RDX,RBX`, `R8`=Prcb)** — `+0x324 MOVZX EAX,[RCX]` · `+0x327 AND AL,0x7f` · `+0x329 CMP AL,2 / +0x32b JNZ +0x335` 로 객체가 뮤턴트일 때만.
  - `KiWaitSatisfyMutant` 가 하는 일 (`KiWaitSatisfyMutant.asm`): `+0x4 CMP [RCX+0x31],0 / +0xa DEC word [RDX+0x1e4]` (`ApcDisable` 이면 깨운 스레드 `KernelApcDisable-1`) · **`+0x40 MOV [RCX+0x28],RDX` (`OwnerThread = 깨운 스레드`)** · `+0x3c~+0x4d` `Abandoned` 처리 (→ (c)) · `+0x58~+0x69` `AbEnabled`(비트 1) 면 `AbWaitObject` 에 뮤턴트를 적고 아니면 0 · **`+0x74~+0x93` 깨운 스레드의 `MutantListHead(+0x308)` 맨 뒤에 `MutantListEntry(+0x18)` 를 꿴다.**
  - 한 몫을 넘겼으니 `+0x192 SUB dword [RDI+4],1` (SignalState 1→0) → `+0x196 JNZ` 아니면 `+0x19c` 로 끝. 깨울 스레드가 없으면 SignalState 는 1 (빈 채로) 남는다 → 나중에 오는 `KeWaitForSingleObject` 가 질문 1(a) 의 `+0x3e3~` 줄로 가져간다.
  - 끝 `+0x1ae…` · `+0x201 / ret +0x206 KiExitDispatcher(Prcb, Wait 에 따라 0/3, 1, Increment, 옛 IRQL)` — 질문 2(c) 와 같은 준비 줄 길.
- `WaitType==0` (전부 기다림) 블록은 `+0x472 / ret +0x477 KiTryUnwaitThread(R8D=0x100, R9D=0)` — 주인 안 넘기고 깨난 쪽이 다시 검사 (큐 `==2`: `+0x37c KiInsertQueueInternal`, DPC `==4`: `+0x522 MOV [RDI+4],0` · `+0x530 KeInsertQueueDpc`).

**(c) 버려진 뮤턴트 — 있다.** `WaitStatus |= 0x80` (`STATUS_ABANDONED_WAIT_0`).

- 버리는 쪽: `KeReleaseMutantEx +0x84 TEST BPL,1 / +0x88 JNZ +0x550` → `+0x550 OR byte [RDI+0x30],1` (`Abandoned=1`) · `+0x560 MOV [RDI+4],1` (SignalState=1, 빔) → 질문 4(b) 의 기다리는 쪽 돌기로 이어진다.
- 가져가는 쪽 줄 셋 (같은 모양): **`KiWaitSatisfyMutant +0x4d OR [RDX+0xc8],0x80`** (`+0x44 TEST AL,1` · `+0x48 AND AL,0xfe` · `+0x4a MOV [RCX+0x30],AL` 로 `Abandoned` 를 끄고) · `KeWaitForSingleObject +0x499 OR [RDI+0xc8],0x80` · `KiTryUnwaitThread +0x29a OR [RBX+0xc8],0x80`. `WaitStatus` 를 먼저 `WaitKey` 로 쓴 **뒤** 에 OR 하므로 (`KeReleaseMutantEx +0x306` → `+0x330`) `0x80` 이 남는다. 반환은 `WaitStatus` 하위 32비트 = `0x80`.

**지금 우리 (PE_emulator) 와 비교:** "깨난 쪽이 다시 묻고 가져간다" — **틀리다** (뮤턴트도). 놓는 쪽이 기다리는 첫 스레드에게 주인을 넘긴다. 기다리는 스레드가 없으면 `SignalState=1` 로 두고 다음 기다리는 쪽이 가져간다.

### 5. `KePulseEvent` (`(Event=RCX, Increment=EDX, Wait=R8B)`)

**답. "깨우기 → 0 으로" 순서. 수동: 기다리는 전부 깨움. 자동: 깨움에 성공한 첫 하나만. 끝에 항상 `SignalState=0`.**

- `+0x1f MOV DIL,[RCX]` · `+0x25 AND DIL,0x7f` (Type 저장) · `+0x2c~+0x35` IRQL 2 · `+0x52 / ret +0x57 KiAcquireKobjectLockSafe` (헤더 락) · `+0x57 MOV R12D,[RBX+4]` (옛 SignalState, 돌려줄 값 `+0xda MOV EAX,R12D`).
- **`+0x5b TEST R12D,R12D / +0x5e JNZ +0xaa`**: 옛 값이 0 이 아니면 (이미 신호 = 기다리는 쪽이 없다) 깨우지 않고 `+0xaa` 로.
- 0 이면: **`+0x60 MOV [RBX+4],1`** (신호) → `+0x6e TEST DIL,DIL / +0x71 JNZ +0x11f`.
  - **수동 (Type 0)**: `+0x77~+0x9d` 모든 블록을 돌며 (`WaitType==1`: `+0x8a MOVZX R8D,word [RAX+0x12]` · `+0x92 MOV RDX,RAX` · **`+0x98 / ret +0x9d CALL KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)`**) 반환값을 안 보고 계속; 블록을 하나씩 떼지 않고 끝에서 머리째 비움 (`+0x9f~+0xa7`). `WaitType` 이 1·2·4 가 아니면 (`+0x143 CMP CL,4` · `+0x148 MOV R8D,0x100` 갈래) `0x100` 으로.
  - **자동 (Type≠0)**: `+0x11f` 부터 블록을 하나씩 떼며 (`WaitType==1`: **`+0x170 / ret +0x175 KiTryUnwaitThread`**) **반환 AL≠0 일 때만** `+0x179 SUB dword [RBX+4],1` (SignalState 1→0) → 0 이 되면 그만. AL==0 (이미 다른 쪽이 깨움) 이면 SignalState 를 안 줄이고 다음 블록. 큐 `==2`: `+0x115`/`+0x195 KiInsertQueueInternal` 후 같은 감소. DPC `==4`: `+0x1a3 AND [RBX+4],0` · `+0x1ae`/`+0x1cf KeInsertQueueDpc`.
- **`+0xaa AND dword [RBX+4],0`** (수동·자동·이미신호 모두 지나는 합류점): **SignalState 를 0 으로.** 즉 `KeSetEvent` 와 달리 **기다리는 쪽이 없었어도, 그리고 이미 신호였어도** 끝에 0 이다 (자동 재설정에서 안 깨워도 0).
- `+0xae AND.LOCK [RBX],0xffffff7f` (헤더 락 풀기) → `+0xd0 / ret +0xd5 KiExitDispatcher(Prcb, Wait 에 따라 0/3, 1, Increment, 옛 IRQL)` → `+0xf4 RET`.

**지금 우리 비교:** "기다리는 쪽을 깨우고 이벤트를 다시 0 으로" — **맞다.** 단 (1) 수동은 전부, 자동은 한 스레드만; (2) 깨우는 방식이 `KeSetEvent` 와 같다 (`WaitStatus` 를 그 자리에서 씀).

### 6. syscall 출구까지

**돌려준 값이 지나가는 줄** (모두 `ret` 로 이어지는 일직선):

| 어디서 | call / ret | 하는 일 |
|---|---|---|
| `KiSystemServiceStart` | **`+0x193 CALL RAX` / ret `+0x195`** (서비스 함수 호출, `_calls.tsv`: `indirect:?`) | 서비스 반환값이 `RAX` 로 돌아온다. 이어서 `+0x198 INC dword gs:[0x2eb8]` (호출 횟수) |
| `NtWaitForSingleObject` | `+0x65 / ret +0x6a` `ObWaitForSingleObject` | `+0x6a ADD RSP,0x38` · `+0x6e RET` — **`RAX` 를 건드리지 않는다.** 타임아웃은 유저 포인터에서 읽어 와서 스택에 복사 (`+0x34~+0x49` 에서 `0x7fffffff0000` 이하로 제한하고 값 복사) |
| `ObWaitForSingleObject` | **`+0xba / ret +0xbf` `KeWaitForSingleObject`** (인자 `EDX=6` WaitReason · `R8B`=WaitMode · `R9B`=Alertable) | `+0xbf MOV EBX,EAX` · `+0xc1 MOV [RSP+0x40],EAX` → `+0xda / ret +0xdf ObfDereferenceObjectWithTag` → **`+0xdf MOV EAX,EBX` → `+0xf6 RET`** — `EAX` 에 그대로, `RAX` 윗 32비트는 0 |
| (에러 갈래) | `ObWaitForSingleObject +0x47 / ret +0x4c ObpReferenceObjectByHandleWithTag` 가 음수면 `+0x4e TEST EAX,EAX / +0x50 JS +0xdf` | 핸들이 잘못이면 그 에러가 그대로. `+0x93 CMP [ExCrossVmMutantObjectType],R8 / +0x9a JZ +0x139` → `+0x146 MOV EAX,0xc0000024` (`STATUS_OBJECT_TYPE_MISMATCH`). 그리고 `ObWaitForSingleObject$filt$0` 이 `0xc0000191` (뮤턴트 한도) 만 받는다 (필터 `+0x10 CMP ECX,0xc0000191`) |
| `KeWaitForSingleObject` | 반환 `+0x2c4 MOV EAX,R14D` (질문 1(c)) | `0` · `0x80` · `0x100`(안 나감) · `0x101` · `0x102` · `0xc0` |

→ 그래서 유저가 받는 `NtWaitForSingleObject` 값 = `KeWaitForSingleObject` 가 돌려준 32비트 값. test.exe 의 `WaitForSingleObject(thread)` 가 정상으로 깨면 `0` (`WaitKey=0`).

**`KiSystemServiceStart` 출구 (`KiSystemServiceExit` = `+0x1a0`) 에서 유저 `RAX` — 트랩 프레임 `Rax` 칸에 적었다가 마지막에 그 칸에서 다시 읽는다.** (레지스터를 그대로 두지 않는다.)

- `+0x1a0 MOV RBX,[RBP+0xc0]` · `+0x1a7 MOV RDI,[RBP+0xc8]` · `+0x1ae MOV RSI,[RBP+0xd0]` — **`RBX` `RDI` `RSI` 는 트랩 프레임에서 복원** (`TrapFrame +0x140/+0x148/+0x150`). `RAX` 는 그 사이 그대로.
- 평범한 길: `+0x1be TEST byte [RBP+0xf0],1` (이전 코드 세그먼트 유저?) / `+0x1c5 JZ +0x5be` (아니면 커널로 돌아가는 `RET` 길) → `+0x1cb~+0x1e3` (IRQL·APC 비활성·`PreviousMode==1` 검사, 틀리면 `+0x971`·`+0x9a6` 쪽) → `+0x1f1 CLI` → `+0x1fb TEST byte [RCX+0xc2],3` (유저 APC 대기?) / `+0x202 JZ +0x25d` →
  - **`+0x25d MOV [RBP-0x50],RAX`** = **`TrapFrame.Rax = 서비스 반환값`**.
  - `+0x261 / ret +0x266 KiSynchronizeUserIsolationDomainEntry` · `+0x268 MOV RAX,[RBP-0x50]` (다시 읽음) · `+0x27f KiUpdateStibpPairing` 앞뒤 `+0x279` 저장 / `+0x284` 읽음.
  - `+0x291 TEST dword [RCX],0x8000000` / `+0x297 JZ +0x2d8` — 스레드 헤더의 그 비트 (공개 PDB 이름 없음, `ThreadControlFlags` 옆 `Reserved4`) 가 서 있으면 `+0x299 MOV [RBP-0x50],RAX` · `+0x29d~+0x2cf` 트랩 프레임의 `Rcx…R11`(`[RBP-0x48..+0x40]`) 을 0 으로 · **`+0x2d3 / ret +0x2d8 KiRestoreSetContextState`**. 이 함수는 **돌아오지 않고 `IRETQ`** 로 유저에 간다 (`KiRestoreSetContextState.asm +0x416 IRETQ`, 앞 `+0x401 MOV RAX,[RBP-0x50]`, 조각 `+0x393`/`+0x3cb`). 안에서 `+0x90 KeContextToKframes` 를 부른다 → **`NtContinue` 로 새 문맥을 넣을 때 유저 `RAX` 는 이 함수가 읽는 `TrapFrame.Rax` 칸**이고, 칸이 무엇으로 채워지는지 (`KeContextToKframes` 안) 는 안 읽음 `[공백]` → Q141e.
  - `+0x2e1 TEST dword [RCX],0x10000` (CycleProfiling) / `+0x2e9 MOV [RBP-0x50],RAX` / `+0x2f3 KiCopyCounters` / `+0x301 MOV RAX,[RBP-0x50]`.
  - `+0x30c CMP word [RBP+0x80],0` / `+0x31a KiRestoreDebugRegisterState` 앞 `+0x316 MOV [RBP-0x50],RAX`, 뒤 `+0x353 MOV RAX,[RBP-0x50]`.
  - **`+0x391 MOV [RBP-0x50],RAX`** — 마지막 저장. 이어서 `+0x395 MOV byte gs:[0x85e],0`, `+0x3c2 WRMSR`(0x48/0x49 투기 제어) …
  - 조각 `FUN_1406c20cc` (= `KiSystemServiceStart +0x50c~+0x5bb`): **`+0x540 MOV RAX,qword [RBP-0x50]`** (`TrapFrame.Rax` 읽기) · `+0x544 MOV R8,[RBP+0x100]` (유저 `Rsp`) · `+0x54b MOV R9,[RBP+0xd8]` (유저 `Rbp`) · `+0x56c MOV RCX,[RBP+0xe8]` (유저 `Rip`, SYSRET 이 `RCX` 로 읽음) · `+0x573 MOV R11,[RBP+0xf8]` (`EFlags`) · `+0x552 XOR EDX,EDX` + `XMM0~5` 0 으로 · `+0x5b8 SWAPGS` · **`+0x5bb SYSRET`**.
- 유저 APC 가 대기 중이면 (`+0x202 JZ +0x25d` 를 안 탐): `+0x204 MOV [RBP-0x50],RAX` · `+0x208~+0x23a` `Rcx…` 와 XMM 칸 0 으로 · `+0x23e~+0x247` IRQL 1, STI · **`+0x248 / ret +0x24d KiInitiateUserApc`** · `+0x24d CLI` · `+0x257 MOV RAX,[RBP-0x50]` (다시 읽음) · `+0x25b JMP +0x1f2` (CLI 뒤 검사를 반복). `KiInitiateUserApc` 안은 안 읽음 — 이 함수가 `TrapFrame.Rax` 를 바꾸면 그 값이 유저 `RAX` 가 된다 `[공백]`.
- **결론 `[정적]`**: 서비스 반환 `RAX` → `TrapFrame.Rax`(`[RBP-0x50]`) 에 적고 → 중간 호출 앞뒤로 그 칸에 저장·복원하면서 → **`+0x540` 에서 그 칸을 읽어 `RAX` 에 넣고 `+0x5bb SYSRET`**. 정상 syscall 이면 유저 `RAX` = 서비스 반환값 (`NtWaitForSingleObject` 는 32비트 값, 윗 32비트 0). 커널이 바꾸고 싶으면 서비스 안에서 `TrapFrame.Rax` 에 쓰는 것으로는 안 되고 (반환값이 `+0x25d` 에서 덮어쓴다), `+0x291` 의 `SetContext` 갈래 (`IRETQ` 로 직접 감) 나 `KiInitiateUserApc` 를 통해야 한다.

**지금 우리 비교:** Q141e 에서 "새 `Rip` 로 갈 때 `RAX`" 는 이 줄들로 정한다 — 일반 syscall 은 반환값이 `RAX`, `NtContinue` 계열은 `SYSRET` 이 아니라 `IRETQ` 로 가고 `RAX` 는 `TrapFrame.Rax` 에서 온다 (칸을 채우는 쪽은 `[공백]`).

## PE_emulator 가 흉내 낼 최소 규칙

(단일 CPU · 한 번에 한 스레드 에뮬레이터 기준. IRQL·헤더 락·스레드 락·`WaitRegister`·RCU·자동부스트는 생략 가능 `[추측: 에뮬레이터 범위]`. 우선순위·`Increment` 는 우선순위를 도입할 때.)

**기다리는 쪽 (한 객체)**
1. 먼저 Thread.State=Waiting, WaitReason/WaitTime 적기.
2. 객체가 신호면 **그 자리에서** 가져가고 State=Running, **안 자고** `STATUS_WAIT_0(0)` 반환:
   - 자동 재설정 이벤트 (Type 1, 동기 타이머 같은 모양): `SignalState=0`.
   - 세마포어: `SignalState-1`.
   - 수동 이벤트·스레드 객체: 아무것도 안 바꿈 (신호 유지).
   - 뮤턴트: `SignalState>0` 이면 빔 → `SignalState-1`, `OwnerThread=나`, `Abandoned` 였으면 끄고 `WaitStatus=0x80`, 내 뮤턴트 목록에 꿰기. `SignalState≤0` 인데 주인이 나면 재귀 (`SignalState-1`, 그대로 반환). 주인이 남이면 기다림.
3. 마감이 이미 지났으면 안 자고 `STATUS_TIMEOUT(0x102)`. (같은 시각이면 타이머 쪽에서 곧바로 만기 처리 — Q141c2. 에뮬레이터에서는 "0 이하면 바로 0x102" 로 줄여도 된다 `[추측]`.)
4. 아니면 WaitBlock (`WaitType=1`, `WaitKey=0`, 객체) 을 객체 목록 **맨 뒤** 에 달고 → 잔다 (State 는 이미 Waiting; 마감 있으면 타이머 등록 — Q141c2).
5. 깨어나면 **`Thread.WaitStatus` 를 그대로 반환**한다. 다시 검사하지 않는다. 예외 하나: `WaitStatus==0x100` 이면 1번부터 다시 한다 (APC 로 깬 경우, `WaitAll` 에서 신호가 하나 왔을 때).

**깨우는 쪽**
- 객체 목록의 블록을 **앞에서부터** 보며, 스레드가 `State==Waiting` 인 것만 깨운다 (이미 깨운 스레드는 지나침). 깨움 = 그 자리에서 ① 블록을 객체 목록에서 뗀다 ② `Thread.WaitStatus = 블록.WaitKey` (한 객체면 0) ③ State=DeferredReady 로 하고 "깨운 스레드 목록" 에 넣는다 ④ `AbWaitObject=0`.
- **`KeSetEvent` 자동 재설정**: 이벤트를 먼저 신호(1)로 적고, 깨움에 **성공한 첫 스레드 하나만** 깨운 뒤 `SignalState=0`. 깨울 스레드가 없으면 신호(1) 로 남는다. 반환값 = 옛 `SignalState`.
- **`KeSetEvent` 수동 재설정**: 신호(1) 로 적고 **전부** 깨운다. 신호 유지. (이미 신호였으면 아무도 안 깨우고 끝.)
- **`KePulseEvent`**: 이미 신호가 아니면 신호(1)로 적고 위와 같이 깨운다 (수동 전부 / 자동 성공한 첫 하나). **끝에 항상 `SignalState=0`.** 반환값 = 옛 `SignalState`.
- **뮤턴트 놓기 (`KeReleaseMutant`)**: 주인이 아니면 `0xc0000046` (`Abandoned` 가 서 있으면 `0x80`). 주인이면 `SignalState+1`; 아직 1 미만 (재귀) 이면 끝. 1 이 되면 내 목록에서 떼고 `OwnerThread=0`; 기다리는 스레드를 앞에서부터 보며 **깨움에 성공한 첫 스레드에게 그 자리에서 주인을 넘긴다** (`OwnerThread=그 스레드`, `SignalState-1`=0, 그 스레드의 뮤턴트 목록에 꿰기, `Abandoned` 였으면 `WaitStatus|=0x80`). 없으면 `SignalState=1` 로 둔다.
- **`WaitType==0` (전부 기다림)** 은 신호가 하나 와도 안 채우고 `WaitStatus=0x100` 으로 깨워서 깨난 쪽이 처음부터 다시 검사하게 한다 (이번 test.exe 에는 안 나옴).
- 깨운 스레드들은 `NextThread`/준비 줄로 (sast/71 질문 3 `KiDeferredReadySingleThread`: 보통 뒤). 깨운 쪽이 IRQL<2 이고 `NextThread` 가 정해졌으면 **깨운 쪽이 그 자리에서 자기를 준비 줄에 넣고 넘어간다** (`KiExitDispatcher +0x1ac`) — `KiSwapThread` 를 안 거치는 두 번째 넘기기 길.

**syscall 출구**
- 서비스 반환값 (`NtWaitForSingleObject` 면 위 `WaitStatus` 하위 32비트) = 유저 `RAX`. 트랩 프레임 `Rax` 칸에 쓰고 마지막에 그 칸에서 읽는 것까지 따라 하면 `NtContinue` 와 같은 틀을 쓸 수 있다.
- `NtContinue` 처럼 문맥을 바꾸는 호출은 `SYSRET` 길이 아니라 `KiRestoreSetContextState` 의 `IRETQ` 길 (Q141e).

## 동적분석으로 확인해야 할 것 (대기열 Q104 에 붙임, 이번 묶음엔 VM 안 함)

- **test.exe 가 실제로 타는 기다림 길**: 메인 스레드의 `WaitForSingleObject(thread, INFINITE)` 가 `KeWaitForSingleObject` 에서 ① 신호 안 됨 → `+0x7b7` 로 블록을 달고 `KiCommitThreadWait +0x220 KiSwapThread` 까지 가나 ② `KiCommitThreadWait +0xa3 JNZ +0x251` (커밋 전에 끝남) 은 안 타나 ③ 반환값이 `0x100` 으로 한 번이라도 돌아와 `+0x891` 로 반복하나.
- **누가 `WaitStatus` 를 쓰나**: 스레드 객체가 신호 받는 쪽은 Q141d. 거기서 `KiTryUnwaitThread`/풀어 넣은 줄/`KiWaitTest` 류 중 어느 걸 타는지.
- **`WaitRegister.State` 값**: 커밋된 기다림에서 1 인지 4 인지, 어떤 때 5·3·6 이 나오는지 (`dt nt!_KTHREAD @$thread WaitRegister`).
- **`PRCB+0x2d08` 이 정말 "깨운 스레드 목록" 인지**: 깨우는 쪽 push 직후 값, `KiProcessThreadWaitList` 입구에서 값.
- **`KiExitDispatcher +0x6c JNZ +0x134` 가 실제로 타는지** (run75 "넘김 5번" 단서): `NextThread` 가 비어 있지 않고 IRQL<2 일 때만 `KiSwapContext` 가 `KiExitDispatcher +0x1ac` 에서 불린다.
- **`NtSetEvent` 가 `KeSetEvent` 의 `Wait` 에 무엇을 넘기나** (이번엔 `NtSetEvent` 를 안 읽음): 참이면 `KiExitDispatcher +0x10a KiDirectSwitchThread` 갈래.
- **`TrapFrame.Rax` (+0x30)**: 유저 복귀 직전 `+0x540` 에서 `rax` 와 `[rbp-0x50]` 이 같은지, `KiInitiateUserApc`·`KiRestoreSetContextState` 가 이 칸을 바꾸는지 (Q141e 와 함께).
- 추천 멈춤점 (모두 `nt!` 함수 입구/지점, 조건: test.exe 의 스레드일 때만. WinDbg 이름은 `nt!KeWaitForSingleObject`):
  - `nt!KeWaitForSingleObject+0x1a6` (State=5 쓰기 직전; `rdi`=지금 스레드) · `+0x7b7` (블록 달기) · `+0x841` (`KiCommitThreadWait` 부름) · `+0x846` (반환 직후 `rax`=상태) · `+0x860` (`r14d` 가 `0x100` 인가)
  - `nt!KiCommitThreadWait+0xbf` (`WaitRegister` 쓰기) · `+0x251` (커밋 전에 끝난 길) · `+0x220` (`KiSwapThread` 부름)
  - `nt!KeSetEvent+0x484` / `+0x2ec` / `+0x4d6` / `+0x171` (`WaitStatus` 쓰기, `r13`=WaitKey) · `nt!KeSetEvent+0xd1` (SignalState 를 0 으로 내림) · `nt!KeSetEvent+0x10c` (`KiExitDispatcher`)
  - `nt!KiTryUnwaitThread` 입구 (`r8`=WaitStatus) · `+0x168` (`WaitStatus` 쓰기)
  - `nt!KeReleaseMutantEx+0x330` (`KiWaitSatisfyMutant` 부름, `rdx`=깨운 스레드) · `nt!KiWaitSatisfyMutant+0x40` (`OwnerThread` 쓰기)
  - `nt!KiProcessThreadWaitList+0x101` (`KiDeferredReadySingleThread` 부름) · `nt!KiExitDispatcher+0x1ac` (깨운 쪽이 그 자리에서 `KiSwapContext`)
  - `nt!KiSystemServiceStart+0x193` / ret `+0x195` (서비스 반환 `rax`) · `+0x25d` (`TrapFrame.Rax` 쓰기) · `+0x391` · `+0x540` (유저 `rax` 가 정해지는 곳) · `+0x5bb` (`SYSRET`) · 데이터 멈춤점: `ba w8 @rbp-0x50` (서비스 안에서 `TrapFrame.Rax` 에 쓰는 곳; `rbp` 는 `+0x1a0` 에서 `TrapFrame+0x80`)

## 판정

**분석성공.** 여섯 질문 모두 `.asm` 줄로 닫았다. 핵심 정정: PE_emulator 의 "깨움 = 실행 가능 표시만, 깨난 쪽이 자기 syscall 을 다시 묻는다" 는 **틀림.** 진짜 커널은 **깨우는 쪽이 그 자리에서** (자동 재설정 이벤트를 0 으로, 뮤턴트 주인을 넘기고, 깨난 스레드의 `WaitStatus` 를 `WaitKey` 로 써서) 기다림을 채우고, 깨난 쪽 `KeWaitForSingleObject` 는 `WaitStatus` 를 그대로 돌려준다. 다시 검사하는 건 `WaitStatus==0x100` (`STATUS_KERNEL_APC`) 하나뿐이고, 이 값은 APC 가 깨웠을 때나 "전부 기다림" 블록을 깨울 때 나온다.

정적으로 끝까지 못 닫은 것:
- `WaitRegister.State` 의 이름·값 (1·4·5·3 의 뜻). 줄과 값만 적었다.
- 깨난 스레드가 준비 줄 앞/뒤 어느 쪽으로 가는지 (`AdjustReason==1` 갈래의 `KiDeferredReadySingleThread` 를 안 따라감 — `Preempted==0` 에서 뒤라고 `[추측]`).
- `KeWaitForMultipleObjects` (`WaitKey` 번호, `WaitAll`) — 이번 대상 아님.
- `KiRestoreSetContextState`/`KiInitiateUserApc` 안에서 `TrapFrame.Rax` 를 어떻게 채우나 (Q141e).
- `NtSetEvent` 의 `Wait` 인자.
