# 74 thread-timer — 정적 분석 결과

- 요청서: `_workspace/sast/74_thread-timer.md` (Q141c2, PE_emulator 요청 25 ㄷ-2 — 기다림의 마감·타이머)
- 앞 바퀴: `_workspace/sast/73_thread-wake_result.md` (기다림 블록·깨움·`WaitStatus`, `KiTryUnwaitThread`), `_workspace/sast/72_thread-quantum_result.md` (시계 사슬 `KeClockInterruptNotify → KiUpdateTime/KiUpdateRunTime → KeAccumulateTicks`, `KiDispatchInterrupt` 가 DPC 를 먼저 돌림), `_workspace/sast/71_thread-switch_result.md` (`KiSwapThread`, `KiDeferredReadySingleThread`). 겹치는 건 "sast/73 질문 N" 으로 가리키기만 한다.
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 이름 정정 (오케스트레이터가 알려줌): 요청서·프롬프트에 "`KeWaitForMutexObject +0x1f2 KiInsertTimerTable`, `+0x23d` TimerActive" 라고 쓰였지만 그 두 줄은 **`KiCommitThreadWait`** 오프셋이다 (`KiCommitThreadWait +0x1f2 / ret +0x1f7 → KiInsertTimerTable`, `+0x23d BTS [RCX+0x74],9`). sast/73 결과서도 같은 말 (질문 1(b)). 질문 1(a) 는 `KiCommitThreadWait` 기준으로 답한다. (`.asm` 줄을 다시 읽어 확인함.)
- 지도: `run_ghidra.sh map ntoskrnl.exe KiTimerExpiration` (311함수), `… KiCheckForTimerExpiration` (29함수) 만들었다 → `ms/disassemble/ntoskrnl/_map/`. 지도로 얻은 것:
  - `KiTimerExpiration` 의 깊이 1 은 7개: 이 중 줄기는 `KiProcessExpiredTimerList` 하나, 나머지는 시간 재기·ETW·락 스핀 도우미 (`KeQueryPerformanceCounter` `EtwTraceKernelEvent` `__security_check_cookie` `KeBugCheckEx` `KiCheckVpBackingLongSpinWaitHypercall` `HvlNotifyLongSpinWait`). 깊이 2 에 `KiTimerWaitTest` · `KiProcessThreadWaitList` · `PopPepIdleTimeoutDpcRoutine`.
  - `KiCheckForTimerExpiration` 의 깊이 1 은 13개: 줄기는 `KiSetDpcRequestFlag` 하나. 나머지는 틱 없는 시계 다시 맞추기 (`KiSetClockTimer` `KiSetClockIntervalToMinimumRequested` `KiResetClockIntervalOneShot` `KiShouldRearmClockTimer` `KiSetNextClockTickDueTime`) · IRQL · ETW → 이름만.
  - **지도에서 안 보이는 이음새**: `KiTimerExpiration` 을 누가 부르나 (caller 2곳) 는 지도 방향(위→아래)과 반대라서 `refs` 로 찾았다 (아래 질문 3(a)).
- 뽑은 함수 (새 **14개**, 전부 depth 0): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/` 에 `.c`/`.asm`.
  - 요청서 대상: `KiTimerExpiration`(2816B) `KiProcessExpiredTimerList`(1260B) `KiTimerWaitTest`(1025B) `KiCheckForTimerExpiration`(1548B) `KiInsertTimerTable`(969B) `KiComputeDueTime`(261B) `KiTimerExpirationDpc`(395B).
  - **depth 1 로 안 뽑은 이유**: 위 지도에서 보듯 줄기 밖 callee 가 ETW·락 스핀·시계 다시 맞추기라서. 줄기 위 것만 이름으로 따로 뽑았다.
  - **요청서 범위를 넓힌 곳 (이유)**:
    1. `KiRetireDpcList`(4163B) `KiSwapStacksAndRetireDpcList`(작은 꼬리점프) `KiSetDpcRequestFlag`(238B) — 질문 2·3(a) 사슬 이음새 ("DPC 요청 비트 → 누가 읽고 `KiTimerExpiration` 을 부르나"). 요청서는 `KiRetireDpcList` 를 "이름만" 이라 했지만 `KiTimerExpiration` 을 부르는 줄이 거기 있어서 그 줄 근처만 읽었다.
    2. `KeInitializeTimerTable`(`KiTimerExpirationDpc` 를 DPC 루틴으로 등록하는 자리 확인) `KiSelectActiveTimerTable`(14줄, 표 고르기).
    3. `KiCancelTimer` — 질문 4 (신호가 이겼을 때 타이머를 어떻게 치우나).
    4. `KiSetTimerEx`(1575B) — **정규식에 실수로 같이 들어가서 뽑았다. 읽지 않았다.** 이번 질문과 상관없음 (`KeSetTimer` 쪽 길).
  - 이미 있던 것을 다시 읽음: `KiCommitThreadWait` `KeWaitForMutexObject`(= PDB `KeWaitForSingleObject`) `KiTryUnwaitThread` `KiProcessThreadWaitList` `KiSwapThread` `KiDeferredReadySingleThread` `KeInitThread` `KeInitializeTimer` `KeAccumulateTicks` `KiDispatchInterrupt`.
  - 안 읽은 것 (이름만): `KiTimer2Expiration` `KiExpireTimer2` `KiInsertTimer2` (고해상도 `Timer2`; 기다림 마감은 이쪽을 안 쓴다 — 아래 질문 1) · `KiSetClockTimer` 류 · `KiRemoveEntryTimer` · `KiSendClockInterruptToTargetProcessor` · `KiInsertQueueInternal`/`KiInsertQueueDpc`(큐·DPC 기다림 블록) · `PsTimerResolutionActive` · `KiTraceSetTimer` · ETW · `KySwapStacksAndRetireDpcList`(`KiRetireDpcList` 를 부르는 얇은 조각).
- refs (누가 읽고 쓰나): `KiTimerExpiration` `KiTimerExpirationDpc` `KiRetireDpcList` → `ms/disassemble/ntoskrnl/_refs/`.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`, sast/71 의 `KiSwapThread +0xdc / ret +0xe1 → KiSearchForNewThread`, sast/73 의 `KiCommitThreadWait +0x220 / ret +0x225 → KiSwapThread` 를 `_calls.tsv` 에서 다시 확인.

## 스크립트 문제

없다. 참고만:

- `_calls.tsv` 에는 `KiRetireDpcList` 를 부르는 줄이 **없다** (`grep` 으로 안 나옴). 호출이 `KySwapStacksAndRetireDpcList` 맨 앞 (`+0x0`) 과 `KiIdleLoop +0x99` 등에 있는데 그 함수들이 이번에 뽑은 목록에 없거나 (`KySwapStacks…`) 아직 `_calls.tsv` 에 안 들어가 있어서다. `refs` (`_refs/KiRetireDpcList.tsv`) 로 찾았다.
- `KiTimerExpiration` 은 `KiTimerExpirationDpc +0x135` 와 `KiRetireDpcList +0xca4` 두 곳에서만 부른다 (`_refs/KiTimerExpiration.tsv` 의 `UNCONDITIONAL_CALL` 두 줄. 나머지는 UNWIND 표 등 데이터 참조).

## 읽는 법 (표기)

- `[정적]` = 코드에 이렇게 쓰여 있다. 실행 때 이 길로 간다는 뜻이 아니다. 이름 뒤 `[추측]` = 코드 동작에서 짐작한 이름.
- 칸 이름은 공개 PDB (`pdb_struct.py`) 로 확인한 것만 이름을 쓴다:
  - `_KTIMER`(0x40B): `Header +0x0` · `DueTime +0x18` · `TimerListEntry +0x20` · `Dpc +0x30` · `Processor +0x38`(16비트) · `TimerType +0x3a`(16비트) · `Period +0x3c`.
  - 타이머 `Header` 칸 (`_DISPATCHER_HEADER` 를 타이머로 본 이름): `TimerType +0x0` · `TimerControlFlags +0x1` (`Absolute` 비트 0 · `Wake` 비트 1 · `EncodedTolerableDelay` 비트 2~7) · `Hand +0x2` · `TimerMiscFlags +0x3` (`Index` 비트 0~5 · `Inserted` 비트 6 · `Expired` 비트 7) · `SignalState +0x4` · `WaitListHead +0x8`. 헤더 락 = `+0x0` 바이트의 비트 7 (sast/73 과 같다). 타이머 `Type`: 8 = 알림형(`TimerNotificationObject`) · 9 = 동기형 (`_KOBJECTS`; sast/73).
  - `_KTHREAD`: `Timer +0x100` · `RelativeTimerBias +0xf8` · `ThreadTimerDelay +0x3b8` · `MiscFlags +0x74`(`TimerActive` 비트 9) · `WaitStatus +0xc8` · `WaitBlock +0x140`(4개) · `State +0x184` · `WaitRegister +0x70` · `AdjustReason +0x236` · `AdjustIncrement +0x237` · `WaitBlockCount +0x24b` · `WaitBlockList +0xd0` (sast/73 의 나머지 칸 그대로).
  - `_KUSER_SHARED_DATA`(주소 `0xFFFFF78000000000` 기준): `InterruptTime +0x8` · `SystemTime +0x14` · `InterruptTimeBias +0x3b0`. 코드에는 이 주소들이 `-0x87ffffffff8`(= `+0x8`), `-0x87fffffffec`(= `+0x14`), `-0x87ffffffc50`(= `+0x3b0`) 같은 음수로 찍힌다 (파이썬으로 `0xfffff78000000008` 등임을 확인).
- 이름 없는 칸 (공개 PDB 가 `_KPRCB` 를 `+0x6d0` 에서 자름 — sast/71): 오프셋과 동작은 `[정적]`, 이름은 `[추측]`.
  - `PRCB+0x38bc` (16비트) = DPC 요청 비트 모음. 비트 `0x8` = **"타이머 만료 처리 요청"** `[추측: 이름]`. 근거: 켜는 쪽 `KiCheckForTimerExpiration +0x3c8~+0x3d0` (아래), 읽고 지우는 쪽 `KiRetireDpcList +0x406~+0x411`, 지운 다음에 `KiTimerExpiration` 을 부른다 (`+0xca4`). 72 에서 나온 `KiDispatchInterrupt +0x4d TEST byte [PRCB+0x38bc],0xbf` 의 `0xbf` 에 이 비트(0x08)가 들어 있다.
  - `PRCB+0x4100` 부터 **타이머 표** `[추측: 이름]`: 앞 64칸(0x200B)은 "만료 목록 머리" 배열, 그 뒤 `+0x4300` 부터 **칸(slot) 2×256개**, 칸 하나 0x20B (`+0x0` 락 · `+0x8` 목록 머리 · `+0x18` 칸에서 가장 이른 만기 시각). 칸 번호 = `(Hand + 0x10) + TimerType×0x100` (`KiInsertTimerTable +0x4e~+0x7f`, `KiCancelTimer +0x72~+0x7a` 가 같은 식). 비어 있는 칸의 만기 시각은 크게(`0xffffffff00000000`) 초기화 (`KeInitializeTimerTable +0x9b OR [RAX+0x14],-1`, `+0x9f AND [RAX+0x10],0`).
  - `PRCB+0x8310`, `+0x8314` (4바이트씩 2개) = 표 종류별 "마지막으로 훑은 손" `[추측: 이름]`. 근거: 읽는 쪽 `KiCheckForTimerExpiration +0x2ec LEA R15,[R8+0x4210]` (`R8 = PRCB+0x4100` 이라 `PRCB+0x8310`), `KiRetireDpcList +0xbe8 MOV R13D,[RAX+0x4210]` (`RAX = KiSelectActiveTimerTable` 결과 `PRCB+0x4100`); 쓰는 쪽 `KiTimerExpirationDpc +0xdf/+0xe5 MOV [RBX+0x8310]/[RBX+0x8314],EAX` (지금의 손). 같은 모양으로 `R8+0x4200`/`+0x4208` (= `PRCB+0x8300`/`+0x8308`) 는 `KiLastPseudoHrTimerExpiration`/`KiLastNonHrTimerExpiration` 과 비교되는 칸 (`KiRetireDpcList +0xc0a/+0xc11`).
  - **손(hand)** = `만기 시각 >> 0x12` 의 하위 8비트. 한 칸 = 2^18 × 100ns ≈ 26.2ms (`[정적]`: `KiCommitThreadWait +0x198 SHR RCX,0x12` · `KiComputeDueTime +0xe7 SHR RDX,0x12`). 한 바퀴 = 256칸 ≈ 6.7초.
- `WaitKey` 숫자: 타이머 기다림 블록은 `WaitKey = 0x102` (아래 질문 1·3). `WaitType` 1 = 한 객체라도 풀리면 깸 (sast/73).

## 한눈에 (그림)

```
[기다림이 시작될 때]   NtWait… → KeWaitForSingleObject(= Ghidra KeWaitForMutexObject)        ← 질문 1
  Timeout 포인터 NULL        → 마감 종류 0 (마감 없음)  → 타이머 안 건다   (sample/test.c:35 `WaitForSingleObject(t, INFINITE)` 는 Win32 가 Timeout 을 NULL 로 넘길 것으로 보여 이 모양 `[소스]+[추측: KernelBase 안 읽음]`)
  Timeout 음수 (상대)        → 종류 2   +0x3b6~+0x3de  "InterruptTime − InterruptTimeBias − RelativeTimerBias + |Timeout|" 을 [RBP-0x51] 에
  Timeout 0 이상 (절대)      → 종류 1   +0xee~+0xfb    값 그대로 [RBP-0x51] 에
  ─ 신호 검사 (+0x211 JG +0x5f0 등) 가 마감 검사 (+0x224~+0x240) 보다 먼저 ─
  마감이 이미 지났나? 상대 +0x2ff~+0x359 (지금 > 마감, 엄격) · 절대 +0x60c→+0x356 (SystemTime > 값) · 절대 값 0 +0x237 → +0x240  R14D=0x102 로 돌아감 (기다리지 않음)
  → 블록 달고 KiCommitThreadWait(마감종류, 마감값)   +0x841 / ret +0x846
       종류≠0 이면  +0x23d TimerActive=1, +0x242 타이머 헤더 락+Inserted 켬 (맨 앞에서)
       … WaitRegister.State=1 (+0xb6)
       종류 2: +0x17a~+0x194  DueTime = RelativeTimerBias + InterruptTimeBias + 마감값 (+ThreadTimerDelay)  → Timer.DueTime
       종류 1: +0x4e3 KiComputeDueTime (SystemTime 기준 → InterruptTime 기준으로 바꿈; 이미 지났으면 0 돌려줌)
       +0x1f2 / ret +0x1f7 KiInsertTimerTable(Prcb, Timer, 0, 손)  → AL=0 이면 (이미 만기) +0x39e KiTimerWaitTest 를 제자리에서 부름
       +0x220 / ret +0x225 KiSwapThread  ← 여기서 잠 (마감 없으면 +0x216 으로 건너뜀)

[시계 틱 하나]                                                                                  ← 질문 2
  KeAccumulateTicks +0x28 / ret +0x2d  KiCheckForTimerExpiration(Prcb)       (sast/72 사슬 곁가지; 6번째 인자==4 면 안 부름)
     InterruptTime 이 어떤 칸의 가장 이른 만기 시각 이상이면 (+0x33c CMP R12,RCX / +0x33f JNC)
       +0x3d0 / ret +0x3d5 KiSetDpcRequestFlag(&PRCB+0x38bc, 8)
       인터럽트 안 (NestingLevel≠0): +0x3e7 InterruptRequest = 1   |   아니면: +0x4f1 HalpInterruptSendIpi(벡터 0x2f)

[인터럽트 끝 → 디스패치 인터럽트]                                                              ← 질문 3
  KiDispatchInterrupt +0x60 / ret +0x65  KiSwapStacksAndRetireDpcList → (꼬리점프) → KySwapStacksAndRetireDpcList +0x0 → KiRetireDpcList
  (KiIdleLoop +0x99 도 KiRetireDpcList 를 부른다 — Idle 스레드가 돌 때)
  KiRetireDpcList  +0x406 XCHG [PRCB+0x38bc]  +0x40d TEST DIL,8 / +0x411 JNZ +0xbca  → 비트 지움 (+0xbd4)
      +0xbd7 / ret +0xbdc KiSelectActiveTimerTable → +0xca4 / ret +0xca9 KiTimerExpiration(Prcb, 시작 손, 지금)
  KiTimerExpiration  손을 하나씩 훑어 (만기 ≤ 지금) 타이머를 칸에서 떼어 Expired 표시하고 목록에 모음
      +0x20e / ret +0x213 (표 0) · +0x2ad / ret +0x2b2 (표 1) · +0x949 · +0x97c (64개 차면 비움)   KiProcessExpiredTimerList
  KiProcessExpiredTimerList  타이머마다  +0xba / ret +0xbf  KiTimerWaitTest(Prcb, 타이머, …)
      끝에서 PRCB+0x2d08 에 깨운 스레드가 있으면  +0xfc / ret +0x101  KiProcessThreadWaitList(Prcb, 1, 0, 2)
  KiTimerWaitTest  SignalState=1 (+0x1d7) → 타이머의 기다림 블록마다 KiTryUnwaitThread(Prcb, 블록, WaitKey=0x102, 0)
      알림형(8): +0x325 / ret +0x32a   동기형(9): +0x24e / ret +0x253 (성공하면 SignalState 1→0 하고 끝)
  KiTryUnwaitThread (sast/73 질문 3)  스레드 락 → State==5 → State=7, WaitStatus=0x102, PRCB+0x2d08 에 push
  KiProcessThreadWaitList  진 쪽 객체 블록 떼기 (+0x141~+0x269) → AdjustReason=1·AdjustIncrement=0 (+0x98/+0x9f) → +0x101 KiDeferredReadySingleThread (sast/71 질문 3)
  KiDispatchInterrupt 는 이 DPC 처리 뒤에 QuantumEnd 를 본다 (sast/72 질문 4)

[잠에서 깬 스레드]                                                                              ← 질문 4
  KiSwapThread  +0x263 BTR TimerActive → 켜져 있었으면 +0x372 / ret +0x377 KiCancelTimer(Thread+0x100, 1)
     (신호가 이긴 경우 타이머를 표에서 뗌. 이미 만기 처리 중이라 못 떼면 +0x37f~+0x391 로 타이머 기다림 블록을 원상 복구)
```

## 질문별 답 [정적]

### 1. 마감을 거는 쪽

**(a) `KiCommitThreadWait` 가 스레드 안 타이머를 거는 줄 — 있다. `Thread+0x100` (`_KTHREAD.Timer`) 가 그 타이머다.** 마감이 있을 때만 (`KiCommitThreadWait(Thread=RCX, WaitBlockList=RDX, 마감종류=R8D, 마감값=R9, …)`; `R12D`=종류, `R13`=값, `RDI`=`Thread+0x100`, `+0x9a LEA RDI,[RBX+0x100]`).

- **종류 ≠ 0 이면 맨 앞에서 켜는 두 줄**: `+0x38 TEST R8D,R8D / +0x3b JNZ +0x23d` → **`+0x23d BTS dword [RCX+0x74],9`** (`MiscFlags.TimerActive`) · **`+0x242 OR dword [RCX+0x100],0x40000080`** (타이머 헤더 락 비트 7 `0x80` + `Inserted` 비트 30 `0x40000000`) → `+0x24c JMP +0x41` (원래 자리로). 헤더 락은 타이머를 표에 넣고 나서 `+0x20f AND.LOCK [RDI],0xffffff7f` 로 푼다. 만료 쪽 `KiProcessExpiredTimerList +0x96 BTS.LOCK [타이머],7` 이 이 락이 풀릴 때까지 기다린다 (`+0x9e JC`).
- 마감 없으면: `+0x167 TEST R12D,R12D / +0x16a JZ +0x216` → 타이머 일을 전부 건너뛰고 바로 `+0x220 KiSwapThread`. 이때 `TimerActive` 도 안 켜졌다.
- **부르는 함수 셋** (`_calls.tsv` 와 대조):
  | 어디서 | call / ret | 부르는 것 | 언제 |
  |---|---|---|---|
  | `KiCommitThreadWait` | **`+0x1f2 / +0x1f7`** | `KiInsertTimerTable(Prcb=RCX, Timer=RDX, R8=0, 손=R9D)` | 종류 2 (상대) 와 종류 1 (절대) 둘 다 합류해서. 인자 설정 `+0x1eb~+0x1ef` |
  | `KiCommitThreadWait` | `+0x4e3 / +0x4e8` | `KiComputeDueTime(Timer, 값, 0, &손)` | 종류 1 (절대) 일 때만 (`+0x170 CMP R12D,2 / +0x174 JNZ +0x4d6`) |
  | `KiCommitThreadWait` | `+0x39e / +0x3a3` | `KiTimerWaitTest(Prcb, Timer, 0)` | 이미 만기일 때 (질문 1(c)) |
  - `KiSetTimerEx`·`KiTraceSetTimer` 는 이 줄기에 없다. `KiTraceSetTimer` 는 ETW 켜져 있을 때만 (`+0x1ff TEST [PerfGlobalGroupMask+8],0x20000 / +0x209 JNZ +0x464`).
- `Timer2` (고해상도): 기다림 마감은 **쓰지 않는다.** `KiCommitThreadWait` 에 `KiInsertTimer2`/`KiTimer2…` 호출이 없고 (`_calls.tsv` 의 `KiCommitThreadWait` 줄에 없음), 쓰는 표는 `PRCB+0x4100` 의 일반 표 (`KiInsertTimerTable`). `Timer2` 는 `KiCheckForTimerExpiration`·`KiRetireDpcList`·`KiTimerExpirationDpc` 가 **곁다리로** 같이 확인할 뿐이다 (`KiRetireDpcList +0xcc6 KiTimer2Expiration`, `KiTimerExpirationDpc +0x161`, `KiCheckForTimerExpiration +0xfd KiTimer2Collections+0x70`).
- 타이머를 어느 칸에 넣나: `KiCommitThreadWait +0x1c4~+0x1d3` 에서 `Timer.TimerType (+0x3a)` 을 정한다 — `KiGlobalTimerResolutionRequests` 가 0 이면 (`+0x1c4 CMP / +0x1cb JZ +0x35c`) `+0x363 PsTimerResolutionActive(프로세스)` 가 참이면 0, 아니면 1 (`+0x370 MOVZX EDX,SI`; `SI` 는 두 갈래 모두 1 — 종류 2 는 `+0x1b7 LEA ESI,[R12-1]`, 종류 1 은 `+0x4f0 MOV ESI,1`); `KiGlobalTimerResolutionRequests≠0` 이면 0. 이 값이 표 두 개 중 어느 쪽인지 고른다 (`KiInsertTimerTable +0x67 MOVZX EAX,word [R14+0x3a]` → `SHL RAX,8`). 두 표의 이름은 `[추측]` — `KiLastNonHrTimerExpiration`/`KiLastPseudoHrTimerExpiration` 이름에서 "0 = 정확한(HR) 표, 1 = 뭉쳐 터지는 비(非)HR 표" 로 보인다. 실제 값은 동적.

**(b) 상대 시간 → 절대 시간. 쓰는 것: `KUSER_SHARED_DATA.InterruptTime` (+0x8) 과 `InterruptTimeBias` (+0x3b0) 과 스레드 `RelativeTimerBias` (+0xf8).** 결과는 **`Timer.DueTime` (`_KTIMER +0x18`, 타이머 `RDI+0x18`)**. 두 단계로 나뉜다:

1. **`KeWaitForSingleObject` 에서 "기준 맞춘 마감" 을 한 번 계산** (`[RBP-0x51]`, 상대 마감 종류 `[RBP-0x5d]`=2):
   - `+0xdf TEST RCX,RCX / +0xe2 JZ +0xfd` → Timeout 포인터가 NULL 이면 종류 0 (`+0x100 MOV [RBP-0x5d],R10D`).
   - `+0xe4 CMP dword [RCX+4],0 / +0xe8 JL +0x3b6` → 윗 32비트가 음수 = 상대. 안 음수면 절대: `+0xee MOV RAX,[RCX]` · `+0xf4 MOV [RBP-0x51],RAX` · `+0xf8 MOV [RBP-0x5d],EBX` (EBX=1) · `+0xfb JMP +0x104`.
   - **상대 (`+0x3b6~+0x3de`)**: `+0x3b6 MOV RAX,[R11]` (`InterruptTimeBias`) · `+0x3c0 MOV RDX,[RDX]` (`InterruptTime`) · `+0x3c3 SUB RDX,RAX` → `+0x3ca MOV RAX,[RDI+0xf8]` (`RelativeTimerBias`) · `+0x3d1 ADD RAX,[RCX]` (Timeout, 음수) · `+0x3d4 SUB RDX,RAX` → **`+0x3d7 MOV [RBP-0x51],RDX`**. 즉 `[RBP-0x51] = (InterruptTime − InterruptTimeBias) − (RelativeTimerBias + Timeout) = InterruptTime − InterruptTimeBias − RelativeTimerBias + |Timeout|`. 그리고 `+0x3c6 MOV [RBP-0x5d],R15D` (종류 = 2).
   - 이 계산은 `+0x104` 반복 지점 **위에서 한 번만** 한다 (`+0x3de JMP +0x104`) → 반복해도 새로 늘어나지 않는다 (sast/73 질문 1(c)).
2. **`KiCommitThreadWait` 가 DueTime 을 쓴다** — 종류 2 (`+0x170 CMP R12D,2`, 같으면 `+0x17a` 로):
   - `+0x17a MOV RAX,-0x87ffffffc50 / +0x184 MOV RAX,[RAX]` (`InterruptTimeBias`) · `+0x187 MOV RCX,[RBX+0xf8]` (`RelativeTimerBias`) · `+0x18e ADD RCX,RAX` · `+0x191 ADD RCX,R13` (R13 = 위 `[RBP-0x51]` 값) → **`+0x194 MOV [RDI+0x18],RCX`** (`Timer.DueTime`).
   - 두 바이어스가 서로 지워져서 **`DueTime = InterruptTime(기다림 시작 때 읽은 값) + |Timeout|`** 이다 (`[정적]` 식 정리. 바이어스 값이 그 사이 안 변한다는 가정이 필요 없는 식이라는 점이 이렇게 두 번 나눠 계산하는 이유로 보이나 `[추측]`).
   - `+0x102 MOV EAX,[RBX+0x3b8]` (`ThreadTimerDelay`) 를 `+0x139 ADD R13,RAX` 로 더하는 갈래가 있다: 종류 2 이고 `WaitMode≠0` (`+0x108`) · `KernelApcDisable==0` (`+0x111`) · `WaitIrql==0` (`+0x11a`) · `+0xc0` 바이트 0 (`+0x127`) · `ThreadTimerDelay≠0` (`+0x135`) 일 때만 (유저 모드 기다림에 "타이머 뭉치기 허용 지연" 을 더함). 에뮬레이터에선 0 으로 둬도 된다 `[추측: 에뮬레이터 범위]`. 같은 계산이 `KeWaitForSingleObject +0x320~+0x352` 의 "이미 지났나" 검사에도 있다.
   - 그 다음 손 (hand): `+0x198 SHR RCX,0x12` · `+0x19c MOVZX EAX,CL` · `+0x19f MOV [RBP+0x48],EAX`. 헤더 바이트를 `+0x1a2~+0x1bc` 에서 고쳐 쓴다: `Absolute` 비트 0 지움 (`+0x1aa AND byte [RBP-0x27],0xfe`), `Hand` 바이트에 손 (`+0x1ae MOV [RBP-0x26],AL`), `+0x1bc MOV [RDI],EAX`.
3. **절대 시간 (양수) 은 다른 갈래다** — `KiComputeDueTime` (`KiCommitThreadWait +0x4d6~+0x4f5`):
   - `KiComputeDueTime(Timer=RCX, 값=RDX, 플래그=R8B, &손=R9)` → `AL`: **0 = 이미 만기, 1 = 넣어도 됨**.
   - `+0x31 TEST EAX,EAX / +0x33 JS +0x6e` (값이 음수 = 상대 → 아래 그대로) · 양수면 `+0x35 OR R10B,1` (`Absolute` 표시) → `+0x39 MOV RAX,-0x87fffffffec / +0x48 MOV RAX,[RAX]` (**`SystemTime`**) · `+0x4b SUB RAX,RDX` (SystemTime − 값) · `+0x57 JS +0x69`: **결과가 0 이상이면 (SystemTime ≥ 값) 이미 만기** → `+0x5c MOV [RCX],EAX` (헤더만 씀) · `+0x60 MOV [RCX+0x18],R9` (`DueTime=0`) · `+0x5e XOR EAX,EAX` → 0 반환.
   - 아직 미래면: `RDX = SystemTime − 값` (음수) 으로 바꿔 두고 상대와 같은 식으로: `+0xbf MOV R8,[…ff8]` (`InterruptTime`) · `+0xd1 SUB R8,RDX` · **`+0xd8 MOV [R11+0x18],R8`** (`Timer.DueTime = InterruptTime + (값 − SystemTime)`) · `+0xe7 SHR RDX,0x12` (손) · `+0xeb OR byte [RSP+3],0x40` (`Inserted`) · `+0xf3 MOV byte [RSP+2],CL` (`Hand`) · `+0xf7 MOV [RBX],ECX` (손을 호출자에게) → `EAX=1`.
   - 허용 지연 (`EncodedTolerableDelay` = `R8B>>2`) 갈래 `+0x6e~+0xb8` 은 `R8B=0` 으로 불러서 이 경로에선 안 탄다 `[정적: 인자 0]`.
   - 호출 뒤 `+0x4e8 TEST EAX,EAX / +0x4ea JZ +0x395` (0 이면 이미 만기 → `KiTimerWaitTest`), 아니면 `+0x4f0 MOV ESI,1 / +0x4f5 JMP +0x1c4` 로 합류.
   - 즉 **절대 마감은 "시스템 시각 기준" 에서 "InterruptTime 기준" 으로 바꿔서 같은 `Timer.DueTime` 칸에 쓴다.** 시스템 시각이 나중에 바뀌어도 이미 건 타이머는 인터럽트 시간 기준으로 남는다.
   - `KeWaitForSingleObject` 쪽 절대 마감의 앞 검사는 질문 1(c).

**(c) 마감이 이미 지났으면 기다리지 않고 그 자리에서 `0x102` 를 돌려주나 — 두 겹이다.**

- **첫째 겹 (`KeWaitForSingleObject`, 블록을 달기 전)**: `+0x240 MOV R14D,0x102` → `+0x246 AND.LOCK [R12],~0x80` (헤더 락 풀기) → `+0x251 State=2` → … `+0x2c4 MOV EAX,R14D` → `RET` (sast/73 질문 1(a)). 들어가는 길 셋:
  | 마감 | 줄 | 조건 |
  |---|---|---|
  | 절대 값 `0` (Timeout 포인터가 0 을 가리킴: `WaitForSingleObject(h, 0)` 모양) | `+0x224 CMP R14D,2 / +0x228 JZ +0x2ff` 안 탐 · `+0x22e TEST R14D / +0x231 JZ +0x35f` 안 탐 (종류 1) · **`+0x237 TEST R8,R8 / +0x23a JNZ +0x60c` 안 탐** → `+0x240` | 값이 0 이면 무조건 |
  | 절대 값 ≠ 0 | `+0x23a JNZ +0x60c` → `+0x60c MOV RCX,-0x87fffffffec / +0x616 MOV RCX,[RCX]` (`SystemTime`) → `+0x619 JMP +0x356` → **`+0x356 CMP RCX,RAX / +0x359 JA +0x240`** | `SystemTime > 값` (엄격히 큼) |
  | 상대 | `+0x228 JZ +0x2ff` → `+0x2ff~+0x34c` 에서 `지금' = InterruptTime − RelativeTimerBias − InterruptTimeBias` (`+0x313 MOV RAX,[바이어스]` · `+0x316 MOV RCX,[InterruptTime]` · `+0x319 SUB RCX,[RDI+0xf8]` · `+0x326 SUB RCX,RAX`), 비교 대상 `RAX=R8` (미리 계산한 `[RBP-0x51]`) 에 조건부로 `+ThreadTimerDelay` (`+0x352 LEA RAX,[R8+RDX]`) → **`+0x356 CMP RCX,RAX / +0x359 JA +0x240`** | `지금' > 마감 (+허용지연)` (엄격히 큼) |
  - 이 검사들은 **신호 검사 뒤**다: 이벤트·뮤턴트·세마포어가 신호면 (`+0x211 TEST EDX,EDX / +0x213 JG +0x5f0`, 뮤턴트 `+0x20b JZ +0x3e3`) 마감 비교 (`+0x224~`) 에 오기 전에 가져가고 `0` 으로 끝난다 → **마감이 0 이어도 신호면 `0x102` 가 아니라 `STATUS_WAIT_0(0)`.**
  - 같은 시각 (`지금' == 마감`, 엄격 비교라서) 은 이 겹을 통과해서 블록을 달고 둘째 겹으로 간다.
- **둘째 겹 (`KiCommitThreadWait` → 표 넣기 단계)**: 블록은 이미 달았고 커밋도 끝난 뒤 (`WaitRegister.State=1`). **같은 스레드가 자기 만기를 직접 처리**한다.
  - 절대: `KiComputeDueTime` 이 0 (SystemTime ≥ 값) → `+0x4ea JZ +0x395`.
  - 상대·절대 공통: `KiInsertTimerTable` 이 `AL=0` → **`+0x1f7 TEST AL,AL / +0x1f9 JZ +0x395`**. `KiInsertTimerTable` 이 0 을 돌려주는 줄: **`+0x2a5 MOV RAX,[InterruptTime] / +0x2a8 CMP R13,RAX / +0x2ab JBE +0x39b`** (R13 = `DueTime`; **`DueTime ≤ 지금` 이면 만기**) → `+0x3b3 / ret +0x3b8 KiRemoveEntryTimer` (방금 넣은 걸 뺌) · `+0x3aa OR EBX,1` → 끝 `+0x222 NOT BL / +0x224 AND BL,1 / +0x227 MOVZX EAX,BL` = `!(BL&1)` = 0.
  - **주의**: 이 `≤ 지금` 검사는 새 타이머가 칸의 **맨 앞** 이 될 때만 탄다 (`+0x1fe TEST BL,2 / +0x201 JNZ +0x243` → `+0x2a8`). 칸에 더 이른 타이머가 이미 있으면 (`+0x203` 쪽) 검사 없이 `AL=1` 로 돌아온다 — 그 이른 타이머도 지났을 테니 다음 틱의 `KiCheckForTimerExpiration` 이 칸을 보고 처리한다 `[추측]`.
  - `+0x395 XOR R8D,R8D / +0x398 MOV RDX,RDI / +0x39b MOV RCX,R14` → **`+0x39e / ret +0x3a3 KiTimerWaitTest(Prcb, Thread+0x100, 0)`** → `+0x3a3 JMP +0x216` → `+0x220 KiSwapThread`.
  - `KiTimerWaitTest` 가 하는 일은 질문 3(b): `KiTryUnwaitThread` 로 자기 `State` 5 → 7 (`WaitStatus = 0x102`, `PRCB+0x2d08` 에 자기를 push). 그 뒤 `KiSwapThread +0x14 CMP [RDX+0x2d08],0 / +0x2a JNZ +0x508` → `+0x516 / ret +0x51b KiProcessThreadWaitList(Prcb, EDX=1, R8D=0, R9D=2)` → `+0x51b JMP +0x220` → `KiSearchForNewThread +0xdc / ret +0xe1`. 새로 고른 게 자기 자신이면 (`+0xfa CMP RBP,R13 / +0xfd JZ +0x179`) 문맥 전환 없이 `+0x190 State=2` 로 계속 (sast/71 질문 2 의 고르기; 자기 자신이 고른 스레드가 되는지는 런타임 `[추측]`). 마지막에 `+0x25c MOV RDI,[R13+0xc8]` (`WaitStatus`) 을 돌려준다 → 0x102.
  - 정리: **"만기가 지난 걸 알면 기다리지 않고 0x102"는 맞지만, 같은 시각·인터럽트 시간 한 칸 이내의 경계는 `KiSwapThread` 까지 갔다가 바로 돌아오는 길로 처리한다.**

**지금 우리 (PE_emulator) 와 비교:**
- (a) 마감을 스레드가 가진다 (`DueTime`) — 있으면 **맞다.** 진짜는 스레드 안 타이머 하나 (`Thread+0x100`) 를 표에 넣는 모양이다. 에뮬레이터는 "스레드 필드 하나 + 훑기" 로 줄여도 된다.
- (b) 상대 시간은 **"기다림 시작 때의 지금(InterruptTime) + |Timeout|"** 으로 한 번만 계산해 둔다 (`0x100` 으로 다시 돌 때도 새로 안 늘린다). 절대 시간은 시스템 시각 기준이라 **지금 + (값 − 시스템 시각)** 으로 바꿔서 같은 칸에 둔다. "넘길 때마다 지금 시각과 비교" 만 있다면 상대/절대 변환이 없어 **모자랄** 수 있다.
- (c) "이미 지났으면 안 자고 0x102" — 들어가는 쪽 검사가 있으면 **맞다.** 단 순서를 틀리면 안 된다: **신호 검사 → 마감 검사.** (`지금 > 마감` 은 엄격, 표 넣을 때는 `≤`).

### 2. 만료를 알아채는 쪽

**답. `KiCheckForTimerExpiration` 은 sast/72 사슬 위에 있다 — 곁가지로.** 틱마다 불리고, 만료가 있으면 **DPC 를 직접 돌리지 않고 "요청 비트 `0x8`" 만 켜고 디스패치 인터럽트를 요청**한다. 그 비트를 `KiRetireDpcList` 가 읽는다. `KiTimerExpirationDpc` 는 이 줄기에 **없다.**

- 사슬 (`_calls.tsv` 와 대조): `HalpTimerClockInterrupt +0x103 / ret +0x108 → KeClockInterruptNotify` → (주인 CPU) `+0x2ba / ret +0x2bf KiUpdateTime` → `+0x38b / ret +0x390 KiUpdateRunTime`, (그 밖) `KeClockInterruptNotify +0x448 / ret +0x44d KiUpdateRunTime` → `KiUpdateRunTime +0x96 / ret +0x9b KeAccumulateTicks` → **`KeAccumulateTicks +0x28 / ret +0x2d KiCheckForTimerExpiration(RCX=Prcb, EDX=6번째 인자)`** (`+0x23 CMP EDX,4 / +0x26 JZ +0x2d` — 6번째 인자가 4 면 건너뜀. sast/72 질문 1: `HalpTimerClockInterrupt` 는 0 을 넘긴다). 앞쪽 셋은 sast/72 질문 1 표 그대로.
- `KiCheckForTimerExpiration` 안 (`R14 = &PRCB+0x38bc`, `RBX = &PRCB+0x21` (ClockOwner), `R12 = InterruptTime` (`+0x3b~+0x55`)):
  - `+0x5c TEST byte [R14],8 / +0x66 JNZ +0x155`: **이미 비트 8 이 켜져 있으면 다시 훑지 않고** 끝으로 (이미 요청했음).
  - `+0x6c CMP [KiSerializeTimerExpiration],0 / +0x73 JZ +0x474` → `+0x474 MOV RAX,RDI / +0x477 JMP +0x89`: 직렬화 안 하는 보통 길은 **자기 CPU 의 표** (`+0x89 LEA R8,[RAX+0x4100]`). 직렬화 켜면 CPU 0 의 표 (`KiProcessorBlock[0]`) 만 시계 주인 CPU 가 본다 (`+0x79 CMP [RBX],SIL / +0x7c JZ +0x155`).
  - `+0xd7~+0xe3` 훑을지 정하는 비교 (`[R8+0x4200]`/`[R8+0x4208]` 대 `KiLastPseudoHrTimerExpiration`/`KiLastNonHrTimerExpiration`, `KiGlobalTimerResolutionRequests`) 는 **풀어 읽지 않았다** `[공백]`. 훑기 본체는 `+0x2e9~+0x377`.
  - **훑기 본체**: `+0x2e9 MOV R9,R12` · `+0x2f8 SHR R9,0x12` (지금의 손) · 표 종류 `EDX=0,1` 마다 `[PRCB+0x8310+4·EDX]` (마지막으로 훑은 손) 부터 지금 손까지 최대 256칸 (`+0x30d CMP EAX,0x100`) 을 돌며 `+0x32f MOV RCX,[RCX+R8+0x218]` (**칸의 가장 이른 만기 시각**) 을 읽는다. 표 0: **`+0x33c CMP R12,RCX / +0x33f JNC +0x3f0`** → `+0x3f0 MOV SIL,1` (**지금 ≥ 만기 → 만료 있음**). 표 1 은 `+0x381 CMP R12,RCX / +0x384 JC +0x345` 뒤에 `KeNonHrTimeIncrement`·`KePseudoHrTimeIncrement` 를 더해 `+0x3a5 CMP RAX,RCX / +0x3a8 JBE +0x345` 로 더 늦게 만료로 본다 (뭉쳐서 터지는 표 `[추측]`; 값은 동적).
  - **요청하는 줄**: `+0x15d TEST SIL,SIL / +0x160 JNZ +0x3c8` → **`+0x3c8 MOV EDX,8 / +0x3cd MOV RCX,R14 / +0x3d0 / ret +0x3d5 CALL KiSetDpcRequestFlag(&PRCB+0x38bc, 8)`** (`KiSetDpcRequestFlag.asm`: 16비트 `CMPXCHG.LOCK` 로 `OR`, 옛 값을 `AL` 에 돌려줌). 이어서 `+0x3d5 TEST AL,0xa9 / +0x3d7 JNZ +0x166` (옛 값에 DPC 요청 비트가 이미 서 있으면 → 또 인터럽트 요청 안 함) → `+0x3dd CMP byte [RDI+0x20],0` (`NestingLevel`) / `+0x3e1 JZ +0x47c`:
    - `NestingLevel ≠ 0` (시계 인터럽트 처리 안, 보통): **`+0x3e7 MOV byte [RDI+0x6],1`** (`InterruptRequest = 1`, sast/72 질문 3 의 그 칸) → `+0x3eb JMP +0x166` (끝).
    - `NestingLevel == 0`: `+0x4e1 MOV EDX,0x2f` · `+0x4f1 / ret +0x4f6 HalpInterruptSendIpi` (자기 CPU 에 벡터 `0x2f`). 퀀텀 끝 표시와 같은 모양 (sast/72 질문 3).
  - 만료가 없으면 `+0x259 / ret +0x25e KiSetClockTimer` 등으로 다음 틱 시각을 다시 맞춘다 (틱 없는 시계; 이름만).
- `KiTimerExpirationDpc` (rva `0x5c7bd0`): 이 줄기에 **없다.** 하는 일: `KiTimerExpiration` 을 `+0x135 / ret +0x13a` 로 부름 (R9B=1, 앞에서 `PRCB+0x8310`/`+0x8314` 에 지금의 손을 적고, ETW). 이 함수는 **`KeInitializeTimerTable +0x4f LEA RDX,[KiTimerExpirationDpc]` → `+0x5d / ret +0x66 KeInitializeDpc(PRCB+0x83f0, …)`** 로 CPU 마다 하나 있는 `KDPC` 의 루틴으로 등록돼 있다 (`_refs/KiTimerExpirationDpc.tsv`: 코드 쪽 참조는 이 하나뿐). **그 DPC 를 누가 큐에 넣는지는 못 찾았다** (지금까지 뽑은 ntoskrnl `.asm` 497개 전부를 `grep '0x83f0'` 해도 `KeInitializeTimerTable` 뿐) `[공백]`. 다른 길 (`KeInsertQueueDpc(PRCB+0x83f0)`) 이 있을 수 있다.

**지금 우리와 비교:** "넘길 때마다 확인" — **틀리다.** 진짜는 **시계 틱마다 (CPU 마다)**, 그리고 확인(`KiCheckForTimerExpiration`)과 처리(`KiTimerExpiration`)가 **둘로 나뉜다**: 틱에서는 비트만 켜고, 처리는 디스패치 인터럽트의 DPC 단계에서 (퀀텀과 같은 2단계 모양, sast/72 질문 3·4). 에뮬레이터에서는 "틱 = 명령 N 개" 로 줄이더라도 "표시 → 인터럽트 끝에서 처리" 를 나눠 두는 게 퀀텀 쪽과 맞다.

### 3. `KiTimerExpiration → KiProcessExpiredTimerList → KiTimerWaitTest`

**(a) 부르는 사슬 (오프셋, ret):**

| 어디서 | call / ret | 부르는 것 | 근거 |
|---|---|---|---|
| `KiDispatchInterrupt` | `+0x60 / +0x65` | `KiSwapStacksAndRetireDpcList` (DPC 스택으로 바꿈) | sast/72 질문 4. 이 함수는 `+0xc JMP` 꼬리점프로 `KySwapStacksAndRetireDpcList` 로 가서 그 `+0x0` 에서 `KiRetireDpcList` 를 부른다 (`_refs/KiRetireDpcList.tsv`). `KiSwapStacksAndRetireDpcList.asm` |
| `KiIdleLoop` | `+0x99` (call 자리; ret 는 안 뽑음) | `KiRetireDpcList` | `_refs/KiRetireDpcList.tsv` — Idle 스레드가 돌 때 |
| `KiRetireDpcList` | **`+0xca4 / +0xca9`** | **`KiTimerExpiration`** | 앞: `+0x406 XCHG word [RBX+0x38bc],DI` (요청 비트 전부 가져가며 0 으로) · **`+0x40d TEST DIL,8 / +0x411 JNZ +0xbca`** · `+0xbd4 AND DI,0xfff7` (비트 8 지움) · `+0xbd7 / ret +0xbdc KiSelectActiveTimerTable(Prcb, DL=1)` (→ `Prcb+0x4100`, 직렬화 안 하면 자기 표) · `+0xc7c STI` (인터럽트 켜고 부름) · 인자: `RCX`=Prcb · `EDX`=`[PRCB+0x8310]` (마지막으로 훑은 손) · `R8`=`InterruptTime` · `R9B`=표 1 도 훑을지 (`R14B`) |
| `KiRetireDpcList` | `+0xcc6 / +0xccb` | `KiTimer2Expiration` | `ClockOwner(+0x21)≠0` 일 때 (`+0xca9 CMP [RBX+0x21],0`). 이름만 |
| `KiTimerExpirationDpc` | `+0x135 / +0x13a` | `KiTimerExpiration` | (위 질문 2의 `[공백]` 갈래) |
| `KiTimerExpiration` | `+0x20e / +0x213` (표 0) · `+0x2ad / +0x2b2` (표 1) · `+0x949 / +0x94e` · `+0x97c / +0x981` | `KiProcessExpiredTimerList(Prcb=RCX, 인자=[RSP+0x80], 표=R8, 개수=R9D)` | 뒤 둘은 만료 목록이 64칸 (`+0x95b CMP R8D,0x40`) 차서 중간에 비우는 자리 |
| `KiProcessExpiredTimerList` | **`+0xba / +0xbf`** · `+0x483 / +0x488` | **`KiTimerWaitTest(Prcb, 타이머, 인자+8)`** | 뒤쪽은 `PerfGlobalGroupMask` 의 ETW 켜진 변형 (`+0xb1 JNZ +0x450`). 앞 줄: `+0x8d XCHG qword [R8+RAX*8],RBX` (목록 머리 하나 가져옴) · `+0x96 BTS.LOCK dword [RBX],7` (타이머 헤더 락) |
| `KiProcessExpiredTimerList` | `+0xfc / +0x101` · `+0x143 / +0x148` | `KiProcessThreadWaitList(Prcb, EDX=1, R8D=0, R9D=2)` | `+0xe2 CMP [RDI+0x2d08],0 / +0xea JZ` — `PRCB+0x2d08` (깨운 스레드 목록, sast/73) 이 비어 있지 않을 때 |

- `KiTimerExpiration` 자신이 하는 일 (줄기만; 2816B 전부는 안 풀었다):
  - 시작: `+0x21 CMP [KiSerializeTimerExpiration],0`, `+0x8d~+0xcc` 최근 16번 만료 기록 (`[Prcb+0x9440]` 번호, `+0x9448`/`+0x9450` 에 시각·성능 카운터 `+0xc7 / ret +0xcc KeQueryPerformanceCounter`), `+0xd7 SHR RBX,0x12` (지금의 손).
  - `+0x13d~+0x155` 훑을 손 개수 `EDI = 지금 손 − 시작 손 + 1` (256 넘으면 줄임).
  - **칸마다**: 목록 비었으면 지나감 (`+0x1d1 CMP R13,[R13] / +0x1d5 JNZ +0x32e`), 아니면 칸 락 (`+0x331 BTS.LOCK [RBX],0`) → 첫 타이머의 만기 시각 `[RCX-8]` 이 지금 이하인가 (`+0x36c MOV RAX,[RCX-8]` / **`+0x379 CMP RAX,RSI / +0x37c JBE +0x3f7`**, RSI = 지금) → 맞으면 `+0x3f7` 에서: **헤더 바이트 3 을 `0xc0 | 목록번호(6비트)` 로 쓴다** (`+0x406~+0x423 XOR.LOCK [RDX],EAX` — `Inserted`·`Expired` 둘 다 켜고 `Index` 에 만료 목록 번호) · `+0x43f XCHG [R9+RAX*8],R11` (`Prcb+0x4100` 의 64칸 머리에 달음) · 칸의 목록에서 떼기 (`+0x483~+0x4a2`, 틀리면 `INT 0x29`). 표 1 도 같은 모양 (`+0x392~`, `+0x4af~`).
  - 끝에서 `+0x2c1 TEST byte [R14+0x38bc],8` (훑는 동안 또 요청됐나) 등 마무리.
- **정렬 규칙**: 칸 안 목록은 만기 시각 오름차순. `KiInsertTimerTable` 이 첫 항목과 비교하고 (`+0xc4 MOV R8,[RDX-8]` / `+0xc8 CMP R13,R8` / `+0xcb JA +0xea`: 새 시각이 첫 항목 이하면 맨 앞), 아니면 첫 항목에서 가까울 때 (`+0xf7 SUB RAX,R8` / `+0xfa CMP RAX,RCX` (`KeMaximumIncrement>>2`) / `+0xfd JBE +0x187`) 앞에서부터, 멀면 (`+0x103`) 뒤에서부터 자리를 찾는다. 같은 시각의 앞뒤는 갈래마다 다르다 (앞에서 찾으면 같은 시각 앞, 뒤에서 찾으면 같은 시각 뒤) `[정적, 줄 읽기]`. `KiTimerExpiration` 은 **칸의 첫 항목 만기 시각을 보고 `≤ 지금` 이면 꺼내고** (`+0x36c MOV RAX,[RCX-8]` / **`+0x379 CMP RAX,RSI / +0x37c JBE +0x3f7`**), 아니면 칸의 `+0x18` (가장 이른 만기) 을 그 값으로 고쳐 놓고 끝 (`+0x37e MOV [RBX+0x18],RAX`). 한 칸에 이미 지난 타이머가 여러 개일 때 한 번에 다 꺼내는지, 칸을 다시 도는지는 `+0x483~+0x5ce` 만 봐서는 못 정했다 `[공백]` (안 꺼내진 것은 칸의 `+0x18` 이 아직 지난 값이라 다음 틱의 `KiCheckForTimerExpiration` 이 다시 본다 `[추측]`).

**(b) `KiTimerWaitTest` (`(Prcb=RCX, Timer=RDX, 인자=R8)` → `RAX` = 타이머의 DPC 포인터 또는 0):**

- 시작에 DPC 포인터를 푼다: `+0x1c MOV RSI,[RDX+0x30]` (`Timer.Dpc`, 난독화 저장) → `+0x39~+0x4f XOR … KiWaitNever, ROL, XOR Timer, BSWAP, XOR KiWaitAlways`. **스레드 타이머는 DPC 가 `NULL`** (`KeInitThread +0x177~+0x193` 에서 같은 식으로 "NULL 을 난독화한 값" 을 `[Timer+0x30]` 에 적음) → `RSI=0` → 끝 `+0x348 TEST RSI,RSI / +0x34b JZ +0x398` (DPC 일 안 함, `AL`/`RAX`=0).
- `+0x66 MOVSXD RAX,[RDX+0x3c] / +0x6a TEST EAX,EAX / +0x6c JZ +0x1d7`: `Period == 0` (한 번짜리, 스레드 타이머) 면 다시 걸기 (`+0x72~+0x1d1 … KiInsertTimerTable`) 를 건너뜀.
- **`+0x1d7 MOV dword [RDI+4],R11D(1)`** — `Timer.SignalState = 1` (신호 켬). 이어서 `+0x1db LEA RBX,[RDI+8]` (`WaitListHead`) · `+0x1e2 CMP R12B,8 / +0x1e6 JNZ +0x2c7`:
  - **알림형 (Type 8, 스레드 타이머가 이쪽)**: 목록의 **모든** 블록을 앞에서부터. `WaitType==1`: `+0x207 MOVZX R8D,word [RAX+0x12]` (**`WaitKey`**) → `+0x20c JMP +0x31c` → **`+0x31c~+0x325 / ret +0x32a CALL KiTryUnwaitThread(Prcb=RBP, 블록, R8=WaitKey, R9=0)`**. 블록을 하나씩 떼지 않고 끝에서 **머리째 비움** (`+0x33d LEA RBX,[RDI+8]` · `+0x341 MOV [RBX+8],RBX` · `+0x345 MOV [RBX],RBX`). 큐 (`WaitType 2`) · DPC (`4`) · 전부 기다림 (그 밖) 은 KeSetEvent 와 같은 모양 (`+0x2de KiInsertQueueInternal`, `+0x30f KiInsertQueueDpc`, `+0x316 MOV R8D,0x100` 로 `KiTryUnwaitThread` — sast/73 질문 2(c)).
  - **동기형 (Type 9)**: 블록을 **하나씩 떼며** (`+0x211~+0x234`, 링크 검사 틀리면 `INT 0x29`) `WaitType==1` → **`+0x24e / ret +0x253 KiTryUnwaitThread`** → `+0x256 TEST AL,AL / +0x258 JZ +0x2b1` (실패 = 이미 다른 쪽이 깨움 → 다음 블록, SignalState 안 줄임) · 성공이면 **`+0x25a SUB dword [RDI+4],1` / `+0x25e JZ +0x348`** (SignalState 1→0 하고 **끝**). 규칙이 `KeSetEvent` 자동 재설정과 똑같다 (sast/73 질문 2(a)). 이 길로 `WaitType` 그 밖 (0 = 전부 기다림) 은 `+0x2ad MOV R8D,0x100` → `+0x2b9 / ret +0x2be KiTryUnwaitThread`.
- 끝 (`+0x398 MOVZX EAX,R13B / SHL EAX,0x18 / BTS EAX,7 / NOT / +0x3a5 AND.LOCK [RDI],EAX`): 한 번짜리 (`R13B=0xff`, 초깃값 `+0x56`) 는 헤더 락 비트와 바이트 3 (`Index`·`Inserted`·`Expired`) 을 **전부 지운다** → 타이머가 표에서 완전히 빠졌다는 표시. 반복 타이머는 `R13B` 가 0 (`+0x7f`) 이라 바이트 3 을 안 지움.
- **스레드 타이머의 기다림 블록과 `0x102` 의 정체** `[정적]`:
  - 스레드 만들 때 (`KeInitThread`): `+0x168~+0x172 / ret +0x177 KeInitializeTimer(Thread+0x100)` — `KeInitializeTimer` 가 헤더 첫 8바이트를 0 으로 만든 뒤 `+0x9 MOV byte [RCX],8` → **타입 8 (알림형)**, `WaitListHead` 비움 (`+0xc`/`+0x10`), `DueTime +0x18 = 0` (`+0x13`), `Period +0x3c = 0` (`+0x17`), `Processor +0x38 = 0` (`+0x1a`).
  - **`+0x197 LEA RAX,[RDI+0x1d0]`** (= `WaitBlock[3]`, 네 번째 블록 `0x140+3×0x30`) · **`+0x19e MOV dword [RAX+0x10],0x1020401`** → 바이트로 `WaitType=1` (`+0x10`), `BlockState=4` (`+0x11`), **`WaitKey=0x0102`** (`+0x12`, 16비트) = `STATUS_TIMEOUT`. `+0x1d2~+0x1dd` 에서 이 블록을 **`Timer.WaitListHead` (`Thread+0x108`) 에 미리 달아 놓는다** (`InsertTail`).
  - `WaitBlock[0..3].Thread (+0x18)` 도 여기서 채운다: `+0x69 LEA RAX,[RCX+0x158]` · `+0x73 MOV [RAX],RDI` · `+0x76 LEA RAX,[RAX+0x30]` · `+0x7e JNZ +0x73` (`ECX = RSI+4 = 4`, `+0x70`) → sast/73 의 `[추측: KeInitThread]` 가 **`[정적]` 으로 확인됨**.
  - 그래서 타이머가 만기가 되면 `KiTimerWaitTest` 가 이 블록을 `KiTryUnwaitThread(Prcb, WaitBlock[3], WaitStatus=블록.WaitKey=0x102, 0)` 로 넘긴다 → `KiTryUnwaitThread +0x168 MOV [RBX+0xc8],R12` (sast/73 질문 3) → **`WaitStatus = 0x102`**. `KeWaitForSingleObject` 는 그 값을 그대로 돌려준다 (sast/73 질문 1(c)). 즉 **`0x102` 는 코드에 박힌 상수가 아니라 타이머 기다림 블록의 `WaitKey`** 이고, 신호 쪽 `WaitKey` (`KeWaitForSingleObject +0x1e2`, 0) 와 같은 방식으로 `WaitStatus` 에 들어간다.
  - 타이머 블록은 `WaitBlockCount` (`+0x24b`) 에 안 든다 — 한 객체 기다림이면 `WaitBlockCount=1` (`KeWaitForSingleObject +0x7d2`) 이고 `WaitBlockList` 는 `WaitBlock[0]` 만 가리킨다. 타이머 블록은 항상 `WaitBlock[3]`.
- **기다림 블록을 객체 목록에서 떼는 줄 — `KiTimerWaitTest` 도 `KiTryUnwaitThread` 도 객체 쪽 블록은 안 뗀다.** `KiTryUnwaitThread` 는 블록의 `BlockState` 만 +1 한다 (sast/73 질문 3). 타이머가 이겨서 깨운 경우 **객체 목록에 남은 진 쪽 블록은 `KiProcessThreadWaitList` 가 뗀다**:
  - `+0x1e MOV RBX,[RCX+0x2d08]` (깨운 스레드 목록) → 스레드마다 `+0x5a MOVZX EAX,[R13+0x24b]` · `+0x62 MOV RSI,[R13+0xd0]` (그 스레드의 `WaitBlockList`) 에서 블록을 보며 **`+0x74 MOVZX EAX,[RSI+0x11] / +0x78 CMP AL,5 / +0x7a JC +0x141`** — `BlockState < 5` 인 블록 (= 이긴 블록이 아님) 이면 `+0x141 MOV R12,[RSI+0x20]` (객체) · `+0x148 BTS.LOCK [R12],7` (객체 헤더 락) · **`+0x155 CMP byte [RSI+0x11],4 / +0x159 JNZ +0x260`** (4 인 것만) · **`+0x259 MOV [RAX],RCX / +0x25c MOV [RCX+8],RAX`** (`RemoveEntryList`) · `+0x260 AND.LOCK [R12],~0x80` (락 풀기) · `+0x269 JMP +0x80` (다음 블록).
  - 신호 쪽이 이겼을 때는 이긴 블록의 `BlockState` 가 5 라서 `JC` 를 안 타고 지나감. 신호가 이겼는데 진 블록이 남는 경우(여러 객체)도 이 줄이 뗀다 (sast/73 에서 `KiCommitThreadWait +0x2b2~+0x32a` 라고 한 줄은 "커밋 전에 끝난 길" 용).
  - 타이머 블록 자신(`WaitBlock[3]`)은 `KiTimerWaitTest` 가 `WaitListHead` 를 비우면서 떼고, 나중에 깨난 스레드가 `KiSwapThread +0x37f~+0x391` (아래 질문 4) 에서 다시 달아 놓는다.

**(c) 깨운 스레드를 준비 줄로 — 예, sast/71 의 `KiDeferredReadySingleThread` 로 이어진다.**

- `KiProcessExpiredTimerList +0xfc / ret +0x101 KiProcessThreadWaitList(Prcb, EDX=1, R8D=0, R9D=2)` → 스레드마다 `+0x98 MOV [R13+0x236],DL` (`AdjustReason = 1`) · `+0x9f MOV [R13+0x237],R8B` (**`AdjustIncrement = 0`**) → **`+0x101 / ret +0x106 KiDeferredReadySingleThread`** (이미 sast/73 질문 2(c) 에서 같은 줄).
- **차이**: 신호 쪽 (`KeSetEvent`) 은 `KiExitDispatcher` 를 거쳐 `Increment` 인자가 `AdjustIncrement` 로 간다 (sast/73). **타이머 길은 `R8D = 0`** → 올려 줄 양이 0 (아니면 `KiSwapThread +0x508~+0x516` 도 같은 인자 `(1, 0, 2)` 로 부르는 줄이 있음). 이 값이 0 일 때 `KiDeferredReadySingleThread +0x6d` 갈래가 정확히 뭘 하는지는 읽지 않았다 `[공백]`.
- 준비 줄 앞/뒤: `Preempted(+0x235)` 가 `KiCommitThreadWait +0x160` 에서 이미 0 이므로 **뒤** 가 되리라 보이지만 sast/71·73 한계 그대로 `[추측]`.
- 이 처리 뒤 `KiDispatchInterrupt` 는 `QuantumEnd` 를 본다 (`+0x7f`, sast/72 질문 4) — 같은 인터럽트 안에서 **DPC (타이머) → QuantumEnd 고르기** 순서다. 타이머가 깨운 스레드가 이미 준비 줄이나 `NextThread` 에 올라가 있는 상태로 고르기를 한다.

**지금 우리와 비교:**
- 결과값 `0x102` — **맞다.** 진짜도 `WaitStatus` 에 `0x102` 를 써서 그대로 돌려준다 (타이머 블록의 `WaitKey`).
- "마감 지난 잠든 스레드를 깨움" — 방향은 **맞다**, 하는 방식이 다르다: 진짜는 **`State==Waiting` 을 확인하고** (스레드 락) 깨우며 (이미 신호로 깨웠으면 지나침), 깨운 뒤 **진 쪽 객체 블록을 객체 목록에서 뗀다.** 우리가 객체 목록에서 안 떼면 나중에 그 객체를 신호할 때 죽은 블록을 만난다.
- "깨운 뒤 차례" — 앞/뒤는 신호 쪽과 같게 (뒤, `[추측]`). 올려 주기는 타이머 쪽이 `Increment=0`.

### 4. 신호와 마감이 겹칠 때

**규칙 (정적).** 신호 쪽과 타이머 쪽은 **같은 함수 `KiTryUnwaitThread` 를 부르고, 먼저 스레드 락을 잡아 `State==Waiting(5)` 를 본 쪽이 이긴다.** 진 쪽은 `State≠5` 를 보고 **아무것도 안 하고 `AL=0`** 을 돌려받는다. 겨루기 줄은 sast/73 질문 3 과 같다:

| 순서 | 줄 (`KiTryUnwaitThread`) | 하는 일 |
|---|---|---|
| 1 | `+0x1e MOV RBX,[RDX+0x18]` | 스레드 = `WaitBlock.Thread` (타이머 블록도 `KeInitThread +0x73` 이 채워 둠) |
| 2 | **`+0x33 BTS.LOCK [RBX+0x40]`** | 스레드 락을 잡는다 (스핀) |
| 3 | **`+0x57~+0x60 MOVZX EAX,[RBX+0x184] / CMP AL,5 / JNZ +0x186`** | **`State≠5` 면 지나침** (이미 다른 쪽이 `State=7` 로 바꿈 · 깨어 있음) |
| 4 | `WaitRegister.State` 로 갈림 (1·4 깨움 / 0 커밋 전 `State=2` / 5 → 6 으로 / 3·그 밖) | 이긴 쪽이 `+0x149 State=7` · `+0x168 WaitStatus` · `PRCB+0x2d08` push |
| 5 | `+0x194 MOV [RBX+0x40],0` · `+0x1a7 MOV [RBP+0x11],CL(BlockState+1)` | 락 풀기, **블록 `BlockState` +1 은 이기든 지든 항상** |

- **타이머가 졌을 때** (신호가 먼저): `KiTimerWaitTest` 의 `KiTryUnwaitThread` 가 `AL=0` → 동기형이면 `+0x258 JZ +0x2b1` 로 다음 블록 (`SignalState` 안 줄임). 알림형(스레드 타이머)은 `AL` 을 안 보고 계속 (`+0x32a MOV R10D,0`). 스레드는 이미 신호로 깨어났으므로 건드려지지 않는다. 그 뒤 깨난 스레드가 **`KiSwapThread +0x25c MOV RDI,[R13+0xc8]` → `+0x263 BTR dword [R13+0x74],9` (`TimerActive` 끔) → `JC +0x369`** (켜져 있었으면) → **`+0x372 / ret +0x377 KiCancelTimer(Thread+0x100, DL=1)`**.
  - `KiCancelTimer` 줄기: `+0x45 BTS.LOCK [타이머],7` (헤더 락) · `+0x50 TEST byte [RDI+3],0xc0 / +0x54 JZ +0x10a` (`Inserted`·`Expired` 둘 다 아님 = 표에 없음 → `AL=0`) · `+0xb4 MOVZX EAX,[RDI+3] / +0xba JS +0x321` (`Expired` 면 다른 길) · 아니면 칸의 목록에서 뗀다 (`+0x11a MOV [RAX],RCX / +0x11d MOV [RCX+8],RAX`) · `+0x12b~+0x143` 로 `Inserted` 지움 → `BPL=1`. **즉 `Inserted` 이고 아직 `Expired` 가 아니면 취소 성공 (`AL=1`).**
  - `Expired` 가 서 있으면 (다른 CPU/지금 DPC 가 이 타이머를 만료 목록에 이미 올림): `+0x321 AND.LOCK [RBX],0` · `+0x32f XCHG [R12+Index*8],0` (그 만료 목록을 가로채 봄) · … · `+0x350 TEST AL,AL / +0x352 JNS` 로 `Expired` 가 풀리길 기다리는 스핀 `+0x360~` — 끝내 `AL=0` 으로 돌아갈 수 있다 (줄만, 본체 안 풀었다).
  - `AL=0` (취소 못 함) 이면 `KiSwapThread +0x379 JNZ` 를 안 타고 **`+0x37f LEA RAX,[R13+0x1d0] / +0x386 MOV byte [RAX+0x11],4 / +0x38a MOV [R13+0x108],RAX / +0x391 MOV [R13+0x110],RAX`** — 타이머 기다림 블록(`WaitBlock[3]`) 을 `BlockState=4` 로 되돌리고 `Timer.WaitListHead` 를 다시 그 블록 하나로 채운다 (만료 쪽이 머리째 비웠으므로, `KiTimerWaitTest +0x33d~+0x345`). 다음 기다림에 쓸 수 있게 하는 원상 복구.
- **신호가 졌을 때** (타이머가 먼저): `KeSetEvent` 등 깨우는 쪽이 객체 목록에서 그 블록을 이미 떼고 (`KeSetEvent` 자동 루프 `+0x313 RemoveEntryList`, sast/73) `KiTryUnwaitThread` (또는 풀어 넣은 줄의 `State==5` 검사) 에서 `State≠5` → 실패.
  - 자동 재설정 이벤트·동기형 타이머·뮤턴트: 실패하면 **`SignalState` 를 줄이지 않고 다음 기다리는 스레드로** 간다 (`KeSetEvent +0x4ab TEST R14B,R14B / +0x4ae JZ +0xa8`, `KePulseEvent +0x179`, `KiTimerWaitTest +0x258 JZ`). 아무도 못 깨우면 `SignalState` 는 1 로 남는다.
  - 수동 이벤트: 전부 훑고 신호가 남는다.
  - 타이머가 이긴 스레드의 객체 쪽 블록(아직 목록에 있는 것) 은 `KiProcessThreadWaitList +0x141~+0x269` 가 떼고, 신호 쪽이 이미 뗀 것은 `BlockState` 가 5 로 올라가 있어서 (위 표 5번) `+0x155 CMP byte [RSI+0x11],4 / +0x159 JNZ` 로 건드리지 않는다.
- **같은 순간 겹치면** 규칙에 "신호 우선" 같은 건 **없다.** 스레드 락을 먼저 잡은 쪽이 이긴다. 예외 하나는 **들어갈 때 검사** 다: `KeWaitForSingleObject` 에서 신호 검사 (`+0x211`) 가 마감 검사 (`+0x224~`) 앞이라, **기다림을 시작하는 순간** 신호가 이미 있으면 마감이 0 이어도 신호가 이긴다.
- 커밋 전에 겹치는 갈래 (`WaitRegister.State==0`): 먼저 온 쪽이 `State=2` (`WaitRegister`) 로 바꾸고 `WaitStatus` 를 써 놓으면, `KiCommitThreadWait +0xa3 JNZ +0x251` 이 이를 보고 **자지 않고** 그 `WaitStatus` 로 돌아간다 (sast/73 질문 1(c)). 타이머를 넣기 **전**이라면 `+0x27d JNZ +0x511 BTR [RBX+0x74],9` 로 `TimerActive` 를 끄고 타이머 일은 아예 안 한다 (`KiCommitThreadWait +0x27a TEST R12D,R12D / +0x27d JNZ +0x511`).
- 타이머를 표에 넣는 동안 타이머 헤더 락 (`+0x242`) 을 잡고 있어서, 그 사이 다른 CPU 의 만료 처리가 이 타이머를 건드릴 수 없다 (`KiProcessExpiredTimerList +0x96` 가 기다림).

**지금 우리와 비교:** 에뮬레이터는 CPU 하나·한 번에 한 스레드라서 스레드 락·CAS 는 필요 없다 `[추측: 에뮬레이터 범위]`. 단 **"State 가 Waiting 일 때만 깨운다"** (이미 깨운 스레드는 지나침) 는 신호 쪽·타이머 쪽 둘 다에 필요하다. 그리고 신호로 깨난 스레드는 **타이머를 치워야** 한다 (안 치우면 나중에 같은 스레드가 다른 기다림에 들어간 뒤 옛 마감이 터진다).

### 5. PE_emulator 흉내와 비교

현재 흉내 `[추측]`: "스레드를 넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (결과 `0x102`)".

| 항목 | 진짜 `[정적]` | 우리 | 같은가 |
|---|---|---|---|
| 결과값 | 타이머 블록 `WaitKey=0x102` → `WaitStatus=0x102` → `KeWaitForSingleObject` 가 하위 32비트 그대로 반환 | `0x102` | **같다** |
| 언제 확인 | **시계 틱마다 (CPU 마다)**: `KiCheckForTimerExpiration` 이 `InterruptTime ≥ 칸의 가장 이른 만기` 를 보고 비트만 켬. 처리는 디스패치 인터럽트 DPC 단계 | 스레드를 넘길 때마다 | **다르다** (틱이 안 오면 안 깨우고, 한 스레드만 돌아도 틱에서 깨운다) |
| 확인 단위 | 손 = `만기>>18` (26.2ms 칸). 비(非)HR 표는 `KeNonHrTimeIncrement` 만큼 더 늦게 터질 수 있음 (값 동적) | 정확한 시각 비교로 보임 | **다르다** (에뮬레이터는 정확한 비교로 두어도 결과 같음 — 값만 다름) |
| 처리 순서 | `KiDispatchInterrupt`: DPC(타이머 만료) **먼저** → QuantumEnd → 고르기 | "깨운 뒤 고름" | **같은 방향** (타이머로 깬 스레드가 고르기 후보에 이미 들어 있음) |
| 깨우는 조건 | `State==5` (스레드 락 안에서). 이미 신호로 깨웠으면 지나침 | (모름) | 필요 |
| 깨운 뒤 일 | 진 쪽 객체 블록 떼기 · `AdjustReason=1` · **`AdjustIncrement=0`** · `KiDeferredReadySingleThread` | "깸 표시" | **모자랄 수 있다** (객체 블록 떼기) |
| 준비 줄 위치 | `Preempted=0` 이라 뒤 `[추측]` | (모름) | 신호 쪽과 같게 |
| 우선순위 올림 | 타이머 쪽 `Increment=0` | 없음 | 우선순위 도입 전엔 건너뛰어도 됨 |
| 깨난 뒤 | `WaitStatus` 를 그대로 반환, 다시 안 묻음 | — | sast/73 규칙 |
| 신호로 깨날 때 타이머 | `TimerActive` 이면 `KiCancelTimer` | (모름) | 필요 |

- 결과값은 같고 **언제·누가** 가 다르다. 한 스레드만 도는 중에도 틱이 마감을 지나는 순간 깨워서, 준비 줄에 넣고 다음 고르기 때 후보가 된다.

## PE_emulator 가 흉내 낼 최소 규칙

(단일 CPU · 한 번에 한 스레드 에뮬레이터 기준. 락·CAS·`ThreadTimerDelay`·`Timer2`·표 두 개의 구분·직렬화는 생략 가능 `[추측: 에뮬레이터 범위]`. 우선순위·`Increment` 는 우선순위를 도입할 때.)

**마감 계산 (기다림 시작)**
1. **먼저 신호 검사** (sast/73 규칙): 신호면 그 자리에서 가져가고 `0` 으로 끝. **마감은 그다음에 본다.**
2. Timeout 이 NULL 이면 마감 없음 (타이머 안 건다). `test.exe` 의 `WaitForSingleObject(t, INFINITE)` (`sample/test.c:35` `[소스]`) 는 NULL 로 넘어올 것으로 보인다 `[추측: KernelBase 쪽 안 읽음]`.
3. 음수 = 상대: **마감 = 지금(InterruptTime) + |Timeout|** 을 **한 번** 계산해서 스레드에 적는다 (`0x100` 으로 다시 돌 때 새로 안 늘린다). 그리고 `지금' > 마감` 이면 (엄격) 안 자고 `0x102`.
4. 0 이상 = 절대 (시스템 시각): 값이 0 이면 안 자고 `0x102`. `시스템 시각 > 값` 이어도 안 자고 `0x102`. 아니면 **마감 = 지금(InterruptTime) + (값 − 시스템 시각)** 으로 바꿔 같은 칸에 둔다.
5. 스레드를 타이머 표에 넣을 때 `마감 ≤ 지금` 이면 (경계) 자는 대신 **그 자리에서 만료 처리** (아래) 한 뒤 바로 돌아온다.
6. 스레드에 `TimerActive` 표시. 타이머의 기다림 블록은 스레드마다 하나 미리 있고 `WaitKey=0x102` (`WaitType=1`).

**만료 알아채기·처리**
7. **시계 틱마다**: 지금 시각이 가장 이른 마감 이상이면 "타이머 만료 요청" 표시를 켜고 디스패치 인터럽트를 요청 (인터럽트 안이면 인터럽트 끝에서). 이미 표시가 켜져 있으면 다시 안 함.
8. **디스패치 인터럽트의 DPC 단계에서** (QuantumEnd 보다 **앞**): 표시를 지우고, 마감 ≤ 지금 인 타이머를 꺼내 **Expired** 로 표시한다. 칸 안은 마감 오름차순이라 앞에서부터 `≤ 지금` 까지 (한 칸에 여러 개일 때 한 번에 다 꺼내는지는 줄로 못 닫음 — 에뮬레이터는 모두 꺼내도 결과가 같다 `[추측]`).
9. 타이머마다 `SignalState=1`, 그리고 **타이머 기다림 블록으로 `Unwait(스레드, WaitStatus=0x102)`**:
   - `스레드.State == Waiting` 일 때만 성공: `State=DeferredReady`, `WaitStatus=0x102`, "깨운 스레드 목록" 에 넣는다. 아니면 (이미 신호로 깨웠음) 지나친다.
   - 스레드 타이머는 알림형이라 목록의 모든 블록을 본다 (사실상 1개). 동기형 타이머라면 첫 성공 하나만 깨우고 `SignalState=0`.
10. 깨운 스레드 목록을 처리: 스레드마다 **진 쪽 객체 블록 (`BlockState<5` 이고 `==4`) 을 객체 목록에서 뗀다** → `AdjustReason=1`, `AdjustIncrement=0` → 준비 줄에 넣는다 (`Preempted=0` 이라 뒤 `[추측]`, 또는 `NextThread`).
11. Idle 스레드가 돌 때도 같은 DPC 처리를 한다 (`KiIdleLoop` 가 `KiRetireDpcList` 를 부름).

**겹칠 때**
12. 신호 쪽·타이머 쪽 둘 다 같은 `Unwait(스레드, 상태)` 를 쓰고, **`State==Waiting` 일 때만 이긴다.** 진 쪽은 아무것도 안 바꾼다 (자동 재설정 이벤트의 `SignalState` 도 안 줄이고 다음 기다리는 스레드로 간다).
13. 신호로 깨난 스레드는 나가면서 `TimerActive` 이면 **타이머를 표에서 뗀다** (이미 만료 처리에 올라갔으면 못 뗀다 → 타이머 기다림 블록을 원래대로 되돌림).
14. 같은 순간이면 **신호 우선 규칙은 없다.** 먼저 `Unwait` 한 쪽이 이긴다. 에뮬레이터에서 순서를 정해 두려면 위 7~10 의 처리 순서가 곧 규칙이다 (틱의 DPC 단계에서 처리).

## 동적분석으로 확인해야 할 것 (대기열 Q104 에 붙임, 이번 묶음엔 VM 안 함)

- **test.exe 초기화 동안 마감 있는 기다림이 실제로 있나**: `sample/test.c` 의 기다림은 `INFINITE` 하나뿐인데, ntdll/kernelbase 내부(로더 락, `RtlpWait…`) 가 마감 있는 기다림을 하나라도 만드는가. `nt!KiCommitThreadWait+0x23d` (`TimerActive` 켜는 줄; 조건 = test.exe 의 스레드, `r8d≠0` 이면 마감 있음) 에 걸리는 횟수.
- **값들**: `dd nt!KeMaximumIncrement L1` · `dd nt!KeMinimumIncrement L1` · `dd nt!KeTimeIncrement L1` · `dd nt!KeNonHrTimeIncrement L1` · `dd nt!KePseudoHrTimeIncrement L1` · `dd nt!KiGlobalTimerResolutionRequests L1` · `dd nt!KiSerializeTimerExpiration L1` · `db nt!KiClockTimerPerCpuTickScheduling L1`. 이 값으로 만료 정밀도(손 크기 26.2ms 이상으로 늦게 터지는지)가 정해진다.
- **스레드 타이머의 실제 모양**: `dt nt!_KTIMER @$thread+0x100` (Type 8, `TimerType` 0/1, `Period 0`, `Dpc` 난독화 값), `dt nt!_KWAIT_BLOCK @$thread+0x1d0` (`WaitType 1`, `BlockState 4`, `WaitKey 0x102`), 타이머가 들어간 칸 (`Hand`, `TimerType`) 과 `Inserted`/`Expired` 비트가 만료 순간 어떻게 바뀌는지.
- **누가 `PRCB+0x83f0` 의 DPC (`KiTimerExpirationDpc`) 를 큐에 넣나**: `bp nt!KiTimerExpirationDpc` 에 걸리는가, 걸리면 콜스택.
- **만료 요청 비트 `0x8` 이 실제로 서나**: `ba w2 @$prcb+0x38bc` (조건 = 비트 8). `nt!KiCheckForTimerExpiration+0x3c8` 에 걸리는 틱 수. 인터럽트 안이라 `InterruptRequest=1` 갈래 (`+0x3e7`) 로 가는지, IPI (`+0x4f1`) 갈래인지.
- **`KiRetireDpcList +0x411 JNZ +0xbca` 가 `KiDispatchInterrupt +0x60` 경유인지 `KiIdleLoop +0x99` 경유인지**: `nt!KiRetireDpcList+0xca4` 콜스택.
- **정말 그 스레드가 `0x102` 로 깨나**: `nt!KiTryUnwaitThread+0x168` 에서 `r12 == 0x102` 일 때 `rbx` 가 test.exe 스레드인지 (`r8` = WaitStatus). `WaitKey` 로 쓰이는 값이 `0x102` 인지.
- **`KiProcessThreadWaitList(…, 1, 0, 2)` 의 `r8d=0`** (타이머) 이 실제인지, 그리고 `AdjustIncrement=0` 이 `KiDeferredReadySingleThread +0x6d` 갈래에서 우선순위를 안 올리는지 (`Thread.Priority` 전후).
- **신호가 이긴 뒤 타이머 취소**: `nt!KiSwapThread+0x372` (`KiCancelTimer`) 에 걸리는가, 반환 `al` (취소 성공 1/실패 0), 실패 때 `+0x37f` 복구.
- **자기 자신을 깨우는 갈래** (`KiCommitThreadWait +0x1f9 JZ +0x395`): 경계 마감 (`|Timeout|` 아주 작음) 에서 이 줄이 타는가. 타면 `KiSwapThread +0xfa/+0xfd JZ +0x179` 도 같이.
- 추천 멈춤점 (모두 `nt!` 함수 지점, 조건: test.exe 의 스레드일 때만; WinDbg 이름은 `nt!KeWaitForSingleObject`):
  - 마감 계산: `nt!KeWaitForSingleObject+0x3d7` (`rdx` = 상대 마감 `[rbp-0x51]`) · `+0x240` (이미 지나서 `0x102` 로 나감) · `+0x841` (`r8d` = 마감종류, `r9` = 마감값)
  - 거는 쪽: `nt!KiCommitThreadWait+0x23d` · `+0x194` (`rcx` = `DueTime`) · `+0x1f2` (`KiInsertTimerTable` 부름: `r9d` = 손) · `+0x1f7` (`al` = 0 이면 이미 만기) · `+0x39e` (자기 만기 처리) · `+0x4e3` (`KiComputeDueTime`: 절대)
  - 표에 넣기: `nt!KiInsertTimerTable+0x2a8` (`r13` = DueTime 과 `rax` = InterruptTime 비교)
  - 알아채기: `nt!KiCheckForTimerExpiration+0x3c8` (만료 있음) · `+0x3e7` / `+0x4f1` (요청 방식)
  - 처리: `nt!KiRetireDpcList+0x411` (비트 8) · `+0xca4` (`KiTimerExpiration` 부름) · `nt!KiTimerExpiration+0x20e` · `nt!KiProcessExpiredTimerList+0xba` (`rdx` = 타이머) · `+0xfc` · `nt!KiTimerWaitTest+0x1d7` (`SignalState=1`) · `+0x325` (`KiTryUnwaitThread`, `r8` = WaitKey) · `nt!KiTryUnwaitThread+0x168` (`WaitStatus` 쓰기)
  - 치우기: `nt!KiSwapThread+0x263` (`TimerActive` 끔) · `+0x372` (`KiCancelTimer`) · `+0x37f`
  - 데이터 멈춤점: `ba w8 @$thread+0xc8` (`WaitStatus`), `ba w1 @$thread+0x74` (비트 9 `TimerActive`; MiscFlags)

## 판정

**분석성공.** 다섯 질문 모두 `.asm` 줄로 닫았다. 핵심 정정: PE_emulator 의 "스레드를 넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움" 은 **결과값 (`0x102`) 은 맞지만 시점과 방식이 다르다.** 진짜는 **기다림 시작 때 마감을 한 번 계산** (상대 = 그때 InterruptTime + |Timeout|, 절대 = 시스템 시각 기준을 InterruptTime 기준으로 변환) 해 **스레드 안 타이머 (`Thread+0x100`) 를 표에 넣고**, **시계 틱마다** `KiCheckForTimerExpiration` 이 "만료 요청 비트" 를 켜면 **디스패치 인터럽트의 DPC 단계에서** `KiRetireDpcList → KiTimerExpiration → KiProcessExpiredTimerList → KiTimerWaitTest → KiTryUnwaitThread` 로 `WaitStatus=0x102` (타이머 기다림 블록의 `WaitKey`) 를 써서 깨운다. 신호와 겹치면 **스레드 락 안에서 `State==Waiting` 을 먼저 본 쪽이 이기고** 진 쪽은 지나친다.

정적으로 끝까지 못 닫은 것:
- `KiTimerExpirationDpc` (CPU 마다 있는 `KDPC`) 를 **누가 큐에 넣는지.** 등록 자리 (`KeInitializeTimerTable +0x4f/+0x5d`) 만 확인.
- `KiCheckForTimerExpiration +0xd7~+0xe3` 의 "훑을지 정하는 비교" (`KiLastPseudoHrTimerExpiration`/`KiLastNonHrTimerExpiration` 대 PRCB 칸) 의 뜻. 표 0 의 만료 판정 (`+0x33c CMP R12,RCX / JNC`) 은 닫았다.
- `TimerType` 두 값 (표 0/1) 의 **이름과 실제 값**, 표 1 이 얼마나 늦게 터지는지 (`KeNonHrTimeIncrement` 등 값은 동적).
- `KiDeferredReadySingleThread +0x6d` 갈래가 `AdjustIncrement=0` 일 때 뭘 하는지, 준비 줄 앞/뒤 (sast/71·73 한계 그대로).
- `KiCancelTimer` 의 `Expired` 갈래 (만료 목록을 가로채고 기다리는 줄) 본체, `KiTimer2Expiration`·`Timer2` 쪽 (고해상도 타이머; 기다림 마감은 안 쓰는 것만 확인).
- `KeWaitForMultipleObjects` 의 마감 (이번에 안 읽음, `KeWaitForSingleObject` 와 같은 `KiCommitThreadWait` 를 부를 것으로 보이나 `[추측]`).
