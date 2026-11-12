# 72 thread-quantum — 정적 분석 결과

- 요청서: `_workspace/sast/72_thread-quantum.md` (Q141b2, PE_emulator 요청 25 ㄴ-2 — 퀀텀)
- 앞 바퀴: `_workspace/sast/71_thread-switch_result.md` (고르기 = 질문 1·2, 넣기 = 질문 3, 넘기기 = 질문 5). 겹치는 건 "sast/71 질문 N" 으로 가리키기만 한다.
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도: `run_ghidra.sh map ntoskrnl.exe KeClockInterruptNotify` 만들었다 → `ms/disassemble/ntoskrnl/_map/KeClockInterruptNotify_{edges,nodes}.tsv`, `_summary.txt` (667함수, 3초). 지도 읽은 결과:
  - `KiUpdateTime` (깊이 1), `KiUpdateRunTime` (깊이 1, caller 2곳), `KeAccumulateTicks` (깊이 2) 는 지도에 있다.
  - **`KiDispatchInterrupt`·`KiQuantumEnd` 는 지도에 없다.** 퀀텀 끝 표시와 `KiDispatchInterrupt` 사이가 함수 호출이 아니라 "소프트웨어 인터럽트 + 인터럽트 끝 처리" 로 이어지기 때문 (아래 질문 3·4). 지도 설명의 "호출이 아닌 길은 지도에 없다" 그대로.
  - `KiCheckForThreadDispatch` (깊이 6), `KiHandleDeferredPreemption` (깊이 9) 는 지도에 있지만 다른 caller (`KeGenericProcessorCallback`, `KiSchedulerApc`) 를 거쳐서다. 퀀텀 사슬 위가 아니다 (질문 6).
- 뽑은 함수 (새 **23개**, 전부 depth 0): `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/`.
  - 요청서 대상: `KeClockInterruptNotify`(2456B) `KiUpdateRunTime`(1801B) `KiDispatchInterrupt`(653B) `KiQuantumEnd`(3393B) `KiUpdateTime`(1859B) `KeAccumulateTicks`(1804B). 둘 다 사슬 위라서 뽑았다.
  - depth 1 로 안 뽑은 이유: 줄기 밖 callee (ETW·락·전력·통계·타이머 만료) 가 수십 개라서. 줄기 위 것만 이름으로 따로 뽑았다.
  - **요청서 범위를 넓힌 곳 (이유)**:
    1. 사슬 이음새 ("퀀텀 끝 표시 → `KiDispatchInterrupt`" 가 지도에 없어서 직접 찾음): `HalpTimerClockInterrupt` `KiRequestSoftwareInterrupt` `KiInterruptDispatchNoLock` `KiEndInterruptCycleAccumulation` `KiStartInterruptCycleAccumulation` `KiEndThreadCycleAccumulation` `KiDpcInterrupt`(+ 조각 `FUN_1406b5753`) `KiDpcInterruptBypass`.
    2. 퀀텀 길이 (질문 2): `KiQueryQuantumReset`(96B) `KeInitSystem`(1553B, `KiCyclesPerClockQuantum` 쓰는 곳) `KiCaptureTotalCyclesCurrentThread`(`CycleTime` 갱신 자리 찾느라).
    3. 질문 5(b) 이름 확인: `KiComputeNewPriority`(172B) `KiComputeNewPriorityNonStacking`(183B) `KiTryScheduleNextForegroundBoost`(66B).
    4. 질문 6: `KiCheckForThreadDispatch`(420B) `KiHandleDeferredPreemption`(854B).
  - 안 읽은 것 (이름만): ETW 함수들 · 락 함수들 (`KiAcquire/ReleasePrcbLocksForIsolationUnit`) · `KiUpdateVPBackingThreadPriority` · `KiUpdateThreadPriority` · `KiComputeThreadPriorityNonStacking` · `KiClearForegroundBoost` · `KiGroupSchedulingQuantumEnd` · `KiCheckPreferredHeteroProcessor` · `KiComputeThreadQos` · `KzRefreshWorkloadProperties` · `KeCheckAndApplyBamQos` · `KiScanSharedReadyThreads`(균형 스캔) · `KiCheckForTimerExpiration`·`KiTimerExpiration` (Q141c) · `KiForwardTick` · `KiSetClockTimer` · 헤테로(`Hetero`) 갈래.
- refs (누가 읽고 쓰나): `KiCyclesPerClockQuantum` `KeClockInterruptNotify` `KiDispatchInterrupt` `KiQuantumEnd` `KiDpcInterrupt` `KiDpcInterruptBypass` `KiCheckForThreadDispatch` `KiHandleDeferredPreemption` `KiCaptureTotalCyclesCurrentThread` `KiUpdateTotalCyclesCurrentThread` `KiStartInterruptCycleAccumulation` `PspForegroundQuantum` → `ms/disassemble/ntoskrnl/_refs/`.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`, 그리고 sast/71 의 `KiSwapThread +0xdc / ret +0xe1 → KiSearchForNewThread`, `+0x243 / ret +0x248 → KiSwapContext` 를 `_calls.tsv` 에서 다시 확인.

## 스크립트 문제

없다.

## 읽는 법 (표기)

- `[정적]` = 코드에 이렇게 쓰여 있다. 실행 때 이 길로 간다는 뜻이 아니다.
- **`_KPRCB` 는 공개 PDB 에서 `+0x6d0` 에서 잘려 있다** (sast/71). 그래서 `PRCB+0x38b9` (퀀텀 끝 표시) · `+0x38bc` (DPC 요청 플래그) · `+0x83d8` (준비 비트맵) 같은 칸은 **이름이 없다.** 오프셋과 동작은 `[정적]`, 붙인 이름은 `[추측: 이름]`.
- 공개된 `_KPRCB` 칸 (이번에 쓴 것): `InterruptRequest +0x6` · `CurrentThread +0x8` · `NextThread +0x10` · `IdleThread +0x18` · `NestingLevel +0x20` · `ClockOwner +0x21` · `PendingTick +0x22 (비트 0)` · `IdleHalt +0x7` · `Number +0x24` · `MHz +0x44` · `ScbOffset +0xd8` · `SelfIpiRequestSummary +0xa8`.
- 공개된 `_KTHREAD` 칸: `QuantumTarget +0x20` · `ThreadLock +0x40` · `CycleTime +0x48` · `CurrentRunTime +0x50` · `SchedulingGroup +0x68` · `ThreadFlags +0x78` (`QuantumDonation` 비트 5 · `DisableQuantum` 비트 8 · `ForceDeferSchedule` 비트 12) · `Priority +0xc3` · `BamQosLevel +0x204` · `PriorityDecrement +0x206` · `Preempted +0x235` · `BasePriority +0x233` · `WaitReason +0x283` · `QuantumReset +0x28b` · `DecayBoost +0x366`. `Header.ThreadControlFlags +0x2` 의 비트 2 = `GroupScheduling`.
- `WaitReason` 숫자는 PDB 열거형으로 확인했다: `0x1e = WrQuantumEnd` · `0x1f = WrDispatchInt` · `0x20 = WrPreempted` · `0x21 = WrYieldExecution` · `0x26 = WrDeferredPreempt`. (sast/71 의 `0x21` `[추측]` 은 이걸로 **확인됨**.)

## 한눈에 (그림)

```
[시계 인터럽트 하나]                                   ← 질문 1
HalpTimerClockInterrupt +0x103 (ret +0x108)            (PRCB.PendingTick 비트 0 이 서 있을 때만 부름 +0xe0/+0xe4)
  └ KeClockInterruptNotify                             (ClockOwner = PRCB+0x21)
       ├ 시계 주인 CPU (ClockOwner≠0):  +0x2ba (ret +0x2bf) KiUpdateTime ──→ +0x38b (ret +0x390) KiUpdateRunTime
       └ 그 밖 CPU     (ClockOwner==0): +0x448 (ret +0x44d) KiUpdateRunTime   (HalpTimerClockIpiRoutine 도 이 함수를 부름)

KiUpdateRunTime (Prcb=gs:[0x20])
  ├ +0x96 (ret +0x9b)  KeAccumulateTicks   ← 틱 장부 (KiCheckForTimerExpiration +0x28 = Q141c). 퀀텀과 무관
  ├ +0xb8~+0xc0        현재 스레드 == Idle 이면 퀀텀 검사 건너뜀
  ├ +0x23c~+0x244      CycleTime(+0x48) >= QuantumTarget(+0x20) ?      ← 질문 2
  │        예 → +0x16a  PRCB+0x38b9 = 1  ("퀀텀 끝" 표시)               ← 질문 3
  │             +0x171  NestingLevel(+0x20) 가 0 이면 → +0x498~+0x4a8 HalpInterruptSendIpi(벡터 0x2f)
  │                     0 이 아니면            → +0x17b InterruptRequest(+0x6) = 1
  └ 끝

[인터럽트 끝 처리]  (KiInterruptDispatchNoLock 등)                         ← 질문 3·4
  KiEndInterruptCycleAccumulation: InterruptRequest 읽고 지움 → 이전 IRQL < 2 면 AL=1
  → KiDpcInterruptBypass → KiDispatchInterrupt            (또는 0x2f 벡터 = KiDpcInterrupt → KiDispatchInterrupt)

KiDispatchInterrupt
  ├ +0x4d~+0x65  DPC 먼저 (KiSwapStacksAndRetireDpcList)
  ├ +0x7f~+0x88  PRCB+0x38b9 != 0 ?  → +0x1bc 지우고 → +0x1c3 (ret +0x1c8) KiQuantumEnd → 끝
  └ 아니면 NextThread != 0 이면 선점 (+0x14d 넣기, +0x171 KiSwapContext)    ← 질문 6 쪽

KiQuantumEnd                                                              ← 질문 5
  ├ +0x7d~+0x81  CycleTime >= QuantumTarget 인가 (또 확인)
  ├ 끝났으면: 우선순위 내리기 (+0x375 / +0x715 / +0x773)   (b)
  │           퀀텀 채우기  +0x46a QuantumReset → +0x480 × KiCyclesPerClockQuantum → +0x49b QuantumTarget = CycleTime + 값   (a)
  ├ NextThread == 0 이면 (+0x20c/+0x21a) 직접 찾기: 현재 우선순위 "이상" 중 가장 높은 것의 맨 앞       (c)
  │           +0x611 KiSearchForNewThreadsForRescheduleContext, +0x623 KiCommitRescheduleContext
  ├ 못 찾으면 → +0x30e RET (그대로 계속)
  └ 찾았으면 → +0xb62 Preempted=0 → +0xba8 KiQueueReadyThread (맨 뒤)  (d) → +0xbd6 KiSwapContext   (e)
```

## 질문별 답 [정적]

### 1. 사슬 확인

**답.** 중간에 `KiUpdateTime` 을 **거친다 (시계 주인 CPU 에서)**. `KeAccumulateTicks` 는 사슬의 "중간" 이 아니라 `KiUpdateRunTime` 이 부르는 곁다리다 (`KiUpdateTime` 은 안 부른다).

사슬 (모두 `.asm` 의 `CALL … ret=` 줄, `_calls.tsv` 에서 다시 확인):

| 어디서 | call / ret | 부르는 것 | 조건·뜻 |
|---|---|---|---|
| `HalpTimerClockInterrupt` | `+0x103 / +0x108` | `KeClockInterruptNotify` | `+0xe0 TEST [PRCB+0x22],1` / `+0xe4 JNZ +0xf9` — PRCB `PendingTick`(비트 0) 이 서 있을 때만. 인자: RCX=`[ctx+0x88]` · DL=그 구조의 `+0x29` 바이트 · R8D=0 (`+0xf9~+0x100`). 이 인자들 이름은 `[공백]` |
| `KeClockInterruptNotify` | **`+0x2ba / +0x2bf`** | `KiUpdateTime` | `+0x259 CMP [RDI+0x21],BL` / `+0x25c JZ +0x424` — `ClockOwner(+0x21)` 가 0 이 아닐 때 (시계 주인 CPU). `+0x2a1` 의 `KiClockOwnerOneShotRequestState==1` 갈래는 조각 `FUN_1406d56dc` 를 거쳐 `+0x5b7`→`+0x2ae` 로 되돌아와 같은 `+0x2ba` 에 닿는다 |
| `KiUpdateTime` | **`+0x38b / +0x390`** | `KiUpdateRunTime` | 시스템 시간 갱신 뒤. CPU 가 둘 이상이고 (`+0x375 CMP EDI,1 / +0x378 JA +0x42f`) `KiClockTimerPerCpuTickScheduling==0` 이면 (`+0x42f / +0x436 JNZ +0x37e`) 먼저 `+0x46a / +0x46f KiForwardTick` (다른 CPU 에 틱 넘기기) 하고 `+0x48e JMP +0x37e` 로 와서 이 call 에 닿는다. 한 CPU 면 바로 `+0x37e` |
| `KeClockInterruptNotify` | **`+0x448 / +0x44d`** | `KiUpdateRunTime` | `ClockOwner==0` 인 CPU. `+0x424 CMP [KiSerializeTimerExpiration],0` / `+0x42a JZ +0x4d7` → `+0x4d7 CMP [KiClockTimerPerCpuTickScheduling]` / `+0x4dd JZ +0x430` 로 모여 `+0x434 RtlGetInterruptTimePrecise` → `+0x448`. `KiClockTimerPerCpuTickScheduling≠0` 갈래도 `+0x547 JMP +0x43c` 로 합쳐진다 |
| `KiUpdateRunTime` | **`+0x96 / +0x9b`** | `KeAccumulateTicks` | 조건 없이 부른다 (`+0x5f KeDisableInterrupts` 결과 `+0x7a JNZ +0x184` 로 STI 만 하고 `+0x80` 로 돌아옴). 인자: RCX=PRCB, EDX=`[PRCB+0x38c0]`(이전 틱), R8D=`KUSER_SHARED_DATA+0x320` 틱 수 |

- 다른 CPU 가 `KeClockInterruptNotify` 에 오는 길: refs 에 `HalpTimerClockIpiRoutine +0x1f` · `HalpTimerAlwaysOnClockInterrupt +0xf3` · `KiCallInterruptServiceRoutine +0x32f` 도 부르는 곳으로 나온다 (`_refs/KeClockInterruptNotify.tsv`). 시계 주인이 `KiForwardTick` 으로 다른 CPU 에 틱을 넘기면 그쪽이 `HalpTimerClockIpiRoutine` 으로 온다는 연결은 `[추측]` (`KiForwardTick` 은 안 읽음).
- `KeAccumulateTicks` 안에는 퀀텀 칸 (`QuantumTarget`·`CycleTime`·`QuantumReset`) 을 읽거나 쓰는 줄이 **없다** (`grep` 으로 `+0x20`·`+0x48` 열어 봄: 모두 PRCB 칸 `NestingLevel` 이거나 스택). 하는 일은 PRCB 틱 장부 (`+0x8644`·`+0x8648`·`+0x864c` 등), 스레드 `+0x28c`·`+0x2dc` 에 틱 더하기, DPC 감시, 맨 앞 `+0x28 / +0x2d KiCheckForTimerExpiration` (Q141c). 6번째 인자 (`+0x16 MOV EDX,[RSP+0x98]`) 가 4 이면 `KiCheckForTimerExpiration` 을 건너뜀 (`+0x23 CMP EDX,4 / +0x26 JZ`). 이 인자는 `KeClockInterruptNotify` 의 R8D 가 `KiUpdateRunTime` 을 거쳐 온 것이고, `HalpTimerClockInterrupt` 는 0 을 넘긴다 (`+0xf9 XOR R8D,R8D`).

**지금 우리와 비교.** "명령 `0x10000` 개마다 `KiQuantumEnd`" 는 **틀리다**: 진짜는 시계 틱 하나마다 `KiUpdateRunTime` 이 "퀀텀 끝났나" 를 **검사만** 하고, `KiQuantumEnd` 는 그다음 디스패치 인터럽트에서 불린다. 에뮬레이터에서는 "틱 = 명령 N 개" 로 줄여도 되지만 검사와 실행은 나누는 게 맞다.

### 2. 퀀텀 깎는 자리

**답.** 깎지 **않는다.** 퀀텀은 "남은 양을 줄이는" 게 아니라 **목표선** 이다: 스레드가 가진 `CycleTime`(+0x48, 지금까지 쓴 **CPU 사이클**) 이 `QuantumTarget`(+0x20, 사이클 목표값) 이상이 되면 끝난 것이다. **명령 수가 아니라 CPU 사이클 기준이다.**

- 검사하는 줄 (`KiUpdateRunTime.asm`):
  - `+0xb8 MOV RSI,[RBX+0x8]` (CurrentThread) · `+0xbc CMP RSI,[RBX+0x18]` / `+0xc0 JNZ +0x226` — **Idle 스레드가 아닐 때만** 아래로 간다.
  - **`+0x23c MOV RAX,[RSI+0x48]` (CycleTime) → `+0x240 CMP RAX,[RSI+0x20]` (QuantumTarget) → `+0x244 JNC +0x16a`** (`JNC` = 같거나 크면). 둘 다 64비트.
  - 이 앞에 `+0x22f CMP [RBX+0x83e8],R14D` / `+0x236 JS +0x4f4` 가 있다: 이건 퀀텀이 아니라 "오래 굶은 스레드 올려주기" 스캔 시간 확인 (`KiShouldScanSharedReadyQueue` `+0x4f7`, `KiSetDpcRequestFlag(…,0x40)` `+0x557`). 이름만.
  - 퀀텀이 안 끝났어도 `+0x16a` 로 가는 다른 길 둘 (일반 스레드엔 해당 없음 `[추측]`): ① `+0x24a~+0x25b` QoS 단계가 바뀜 (`KiComputeThreadQos` 결과가 `BamQosLevel(+0x204)` 와 다름), ② `+0x26d` `KiCheckPreferredHeteroProcessor` 가 0 이 아님 (헤테로 코어). 그리고 스케줄링 그룹 (`Thread+0x68 ≠ 0`, CPU 비율 제한) 예산 비교 `+0x107~+0x164` 에서도 `+0x16a` 로 간다 (`+0x14f JLE`).
- **`CycleTime` 이 틱 사이에 늘어나는 자리**: `KiUpdateRunTime` 은 `CycleTime` 을 새로 고치지 않고 그대로 읽는다. 갱신은 **인터럽트 들어올 때** 일어난다:
  - `KiInterruptSubDispatch* +0x33` 이 `KiStartInterruptCycleAccumulation(Prcb)` 를 부르고 (refs: `KiInterruptSubDispatch` · `…NoLock` · `…NoLockNoEtw` · `KiHvInterruptSubDispatch` 등), 이 함수가 `NestingLevel==0` 이면 `+0x21 / +0x26 KiEndThreadCycleAccumulation(Prcb, CurrentThread, 0, 0)` 을 부른다.
  - `KiEndThreadCycleAccumulation +0x1b5 MOV R13,[R14+0x48]` · `+0x1b9 ADD R13,RDI` · **`+0x1bc MOV [R14+0x48],R13`** — 지난 구간 사이클을 `CycleTime` 에 더한다. 같은 함수 `+0x20 MOV byte [RCX+0x20],1` 이 `NestingLevel = 1` 로 올린다.
  - 스레드가 바뀔 때는 `KiCaptureTotalCyclesCurrentThread +0x1d1~+0x1d8` (`ADD RCX,RDI`) 가 같은 일을 한다 (부르는 곳: `KiSwapThread +0x40` · `KiQuantumEnd +0xb85` · `KiDispatchInterrupt +0xae` · `KiCheckForThreadDispatch +0xd7` · `KiHandleDeferredPreemption +0x251` · `KeYieldExecution +0x1e9` 등).
  - 시계 인터럽트가 위 인터럽트 문(`KiInterruptDispatch*` 같은 입구) 을 거쳐서 오는지는 IDT 표를 안 읽어서 `[추측]` → 동적 확인.
- **한 퀀텀 길이를 정하는 것**:
  - `QuantumReset`: 스레드 `+0x28b` (바이트). `KeStartThread +0x1ba MOVZX EAX,[RDI+0x91]` (프로세스 `QuantumReset`) → `+0x1c1 MOV [RBX+0x28b],AL`. 프로세스 쪽 값은 `PspForegroundQuantum` 표 (rva `0xfd9adc`) 를 `PspComputeQuantum +0x2f` 가 읽고 `PsChangeQuantumTable +0x83/+0x94` 가 쓴다 (이름만 `[추측: 프로세스 퀀텀 계산]`, 안 읽음).
  - 한 단위의 사이클 수: 전역 **`KiCyclesPerClockQuantum`** (rva `0xfc5094`, 4바이트, **PDB 이름 확인됨**). 새 스레드 첫 퀀텀: `KeStartThread +0x1c7 IMUL EAX,[KiCyclesPerClockQuantum]` → `+0x1dc MOV [RBX+0x20],RAX` (`QuantumTarget = QuantumReset × 단위`, `CycleTime` 은 0 이라 더하지 않음).
  - **값은 부팅 때 `KeInitSystem` 이 계산한다**: `+0x2f3 MOV [KiCyclesPerClockQuantum],EDX` (그리고 조건부로 `+0x314`). 산식 (`+0x2a0~+0x2e6`, 파이썬으로 곱셈 상수 풀어서 맞춰 봄): `T = KeMaximumIncrement × KiProcessorBlock[0]->MHz(+0x44)`, 단위 = `T / 30`. 즉 **"시계 틱 한 번 (`KeMaximumIncrement`, 100ns 단위) 동안의 사이클 수의 1/3"**. `KiVelocityFlags` 비트 `0x40000` 이 서 있으면 (`+0x2e9 TEST` / `+0x308 JZ`) `+0x30a~+0x314` 에서 6 으로 한 번 더 나눈다. 같은 값이 `KiDirectQuantumTarget`(`+0x2f9`), 3배가 `KiLockQuantumTarget`(`+0x302`) 로도 들어간다.
  - 이 단위를 곱하는 곳 (refs): `KeStartThread +0x1c7` · **`KiQuantumEnd +0x480`** · `KiDeferredReadySingleThread +0x247` · `KiQueueReadyThread +0x74e` · `KeYieldExecution(Ordinal_6) +0x19b` · `KiDirectSwitchThread +0x54f` · `KiWakePriQueueWaiter +0x1ca` · `KeSetPriorityThread +0xd5` · `KeSetBasePriorityThread +0x1c0` · `KeSetActualBasePriorityThread +0x169` · `KeBoostPriorityThread +0x18a` · `KeRemovePriQueue +0x529` · `KeSetPriorityAndQuantumProcess +0x238` · `KiNormalPriorityReadyScan +0xa5` · `KiScanSharedReadyThreads +0x168`.
- **이미 있는 사진으로 맞춰 본 값** (`ms/logs/04_baggage-read_K3.txt`, `P0`·`P1`, run44 의 test.exe 첫 스레드): `QuantumReset = 2`, `CycleTime = 0`, **`QuantumTarget = 0x7f2814 = 8,333,332`** → `8,333,332 / 2 = 4,166,666` = 이 VM 의 `KiCyclesPerClockQuantum` `[관찰, 계산]`. 이 값이 위 산식에 맞으려면 `KeMaximumIncrement × MHz = 125,000,000` (플로어 계산까지 맞음). `KeMaximumIncrement`·`MHz` 숫자 자체는 사진에 안 찍혀 있다 → 동적. 같은 사진에서 `QuantumTarget` 은 그대로 (`0x7f2814`) 인데 `CycleTime` 은 `0` → `0x5dc0` → `0x2f0940` (3,082,560) 으로 늘었다 `[관찰]`: 퀀텀은 스레드마다 따로, 사이클로 쌓인다.
  - `P1b`·`P2` 사진의 `QuantumTarget 0x9fa7208` 은 같은 스레드가 아닌 것 같아 (CycleTime 이 그보다 작음) 근거로 안 썼다 `[추측]`.
- 퀀텀 길이를 바꾸는 갈래 둘: `KiQueryQuantumReset +0x0 TEST [KiVelocityFlags],0x40000` / `+0x13 CMP [KiVariableQuantumEnabled],0` 두 개가 다 켜져 있으면 `QoS(+0x204)` 에 따라 `0x24`·`0x12`·`2` 단위를 돌려준다 (`+0x3b`·`+0x5a`·`+0x53`). 아니면 그냥 `Thread.QuantumReset(+0x28b)` (`+0xa MOVZX EDX,[RCX+0x28b]` → `+0x40 MOV EAX,EDX`). `DisableQuantum` + 우선순위 ≥ 16 인 스레드는 단위 **`0x7f`** 를 쓴다 (`KiQuantumEnd +0xc9c MOV EBX,0x7f`, 사실상 끝없는 퀀텀).

**지금 우리와 비교.** "명령 수" 로 세는 건 **사이클의 대용품으로는 맞다 `[추측]`.** 하지만 ① 스레드마다 `QuantumTarget` 이 따로 있고 ② 목표선이 "쓴 양 ≥ 목표" 라서 전역 카운터 하나로 세면 틀리고 ③ 새 스레드는 `CycleTime=0` 에서 `QuantumReset × 단위` 를 받는다. 숫자는 이 VM 사진 기준 단위 4,166,666 사이클 × 2 = 8,333,332 사이클.

### 3. `_KPRCB.QuantumEnd` 켜는 자리

**답.** **`KiUpdateRunTime +0x16a MOV byte [RBX+0x38b9],1`** (RBX = PRCB). 이름이 PDB 에 없어서 `[추측: QuantumEnd]` — 근거: 이 칸은 `CycleTime >= QuantumTarget` 일 때만 켜지고 (`+0x244 JNC +0x16a`), `KiDispatchInterrupt` 가 `KiQuantumEnd` 를 부르기 바로 앞에서 읽고 지운다 (질문 4). 지금까지 뽑은 ntoskrnl `.asm` 전부(467개)에서 `0x38b9` 를 쓰는 곳은 이 둘뿐이다.

바로 이어서 **디스패치 수준 소프트웨어 인터럽트를 요청**한다 (`KiRequestSoftwareInterrupt(Prcb, 2)` 를 직접 펼쳐 쓴 꼴 — 비교: `KiRequestSoftwareInterrupt +0xac~+0xc4` 와 `+0x8f~+0xa1`):

| 조건 | 줄 | 하는 일 |
|---|---|---|
| `+0x171 CMP byte [RBX+0x20],R13B(0)` — `NestingLevel==0` (인터럽트 안이 아님) | `+0x175 JZ +0x433` → `+0x498 MOV EDX,0x2f` · `+0x49d MOV dword [RBP-0x78],5` · `+0x4a8 / +0x4ad CALL HalpInterruptSendIpi` | **자기 CPU 에 벡터 `0x2f` 인터럽트** (DISPATCH_LEVEL = IRQL 2 → 벡터 `0x2f`). `KiAmdTprLowerInterruptDelayDynamicWorkaround` 가 켜져 있으면 `+0x433~+0x497` 에서 `[PRCB+0xa8] |= 4` 와 MSR `0xc0010015` 를 만지고 간다 (AMD 전용, 건너뜀) |
| `NestingLevel != 0` (시계 인터럽트 처리 안, 보통 이쪽) | **`+0x17b MOV byte [RBX+0x6],1`** → `+0x17f JMP +0x294` (끝) | **`InterruptRequest(PRCB+0x6) = 1`** — "인터럽트 끝 처리 때 디스패치 하라" 표시 (PDB 에 이름 있음) |

그 표시가 `KiDispatchInterrupt` 로 이어지는 길 `[정적]`:

1. 인터럽트 입구 `KiInterruptDispatchNoLock` (예시): `+0x37 / +0x3c KiInterruptSubDispatchNoLock` (ISR 본체) → `+0x44 / +0x49 HalPerformEndOfInterrupt` → **`+0x56 / +0x5b KiEndInterruptCycleAccumulation(Prcb, 이전 IRQL)`**.
2. `KiEndInterruptCycleAccumulation`: `+0x226 NestingLevel = 0` → `+0x246 CMP [RBX+0x6],0` (**InterruptRequest 읽기**) → `+0x250 MOV [RBX+0x6],0` (**지우기**) → `+0x256 CMP R15B,2` / `+0x25a JNC +0x26d`: **이전 IRQL 이 2 미만이면 `+0x25c AL=1`**, 2 이상이면 `+0x26f / +0x274 HalRequestSoftwareInterrupt(2)` 로 나중에 (AL=0). (`IdleHalt(+0x7)` 이 0 이 아니면 AL=0, `+0x254 JNZ +0x23b`.)
3. `KiInterruptDispatchNoLock +0x5d JZ` 를 안 타고 **`+0x5f / +0x64 KiDpcInterruptBypass`** → `KiDpcInterruptBypass +0x0~+0xd` IRQL 을 2 로 올리고 `STI` → **`+0x16 / +0x1b KiDispatchInterrupt`**.
4. 대신 진짜 벡터 `0x2f` 가 오면: `KiDpcInterrupt` (조각 `FUN_1406b5753` 안) 이 `+0x363~+0x377` IRQL 2, `+0x381 HalPerformEndOfInterrupt`, `+0x386 STI`, **`+0x39a / +0x39f KiDispatchInterrupt`** (`.asm` 의 `(= KiDpcInterrupt +0x39a)` 표기. WinDbg 가 이 조각을 어떻게 보여줄지는 확인 전 `[추측]`).
- `KiDpcInterruptBypass` 를 부르는 곳 (refs): `KiChainedDispatch +0x5f` · `KiInterruptDispatch +0x5f` · `KiInterruptDispatchNoLock +0x5f` · `…NoLockNoEtw +0x5f` · `KiInterruptDispatchNoEOI +0x57` · `KiSpuriousDispatchNoEOI +0x4b` · `KiHvInterruptDispatch +0x6c` · `KiVmbusInterruptDispatch +0x58` 등. 벡터 `0x2f` 가 IDT 에서 `KiDpcInterrupt` 로 이어지는지 (표 `PTR_KiDivideErrorFaultShadow_141008280+0x280` 로 의심) 는 표를 안 읽었다 `[추측]`.
- 시계 인터럽트 하나가 위 입구들 중 어느 걸 지나는지는 정적으로 못 정함 → 동적.
- 틱 사슬에는 DISPATCH 요청이 퀀텀 말고도 있다 (이름만): `KeAccumulateTicks +0x171` (DPC 비율 조정) · `KiUpdateRunTime +0x557 / +0x561` (균형 스캔, DPC 요청 플래그 `0x40`) · `KiUpdateTime +0x4d0` (`KiBalanceSetManagerPeriodicDpc` 를 `KeInsertQueueDpc`).

**지금 우리와 비교.** 지금 우리에게 "표시 → 나중에 디스패치" 두 단계가 없으면 **모자라다.** 최소로는 `QuantumEnd` 칸 하나 + "인터럽트 끝에서 칸을 읽는다" 로 충분.

### 4. `KiDispatchInterrupt → KiQuantumEnd`

**답.** 부르는 줄: **`KiDispatchInterrupt +0x1c3 CALL KiQuantumEnd` (ret `+0x1c8`)**. 읽고 지우는 줄:

| 줄 | 하는 일 |
|---|---|
| `+0x39 CLI` · `+0x3a MOV RBX,gs:[0x20]` · `+0x43 MOV RSI,[RBX+0x8]` | PRCB, CurrentThread |
| `+0x47 MOV [RSI+0x186],CL` | 이전 IRQL 을 `WaitIrql` 에 저장 (인자 RCX) |
| `+0x4d TEST byte [RBX+0x38bc],0xbf` / `+0x60 / +0x65 KiSwapStacksAndRetireDpcList` | **DPC 를 먼저 다 돌린다** (타이머 만료 DPC 포함 `[추측]`) |
| `+0x7e STI` | 인터럽트 다시 켬 |
| **`+0x7f MOVZX EAX,[RBX+0x38b9]` · `+0x86 TEST AL,AL` · `+0x88 JNZ +0x1bc`** | **QuantumEnd 읽기** (→ 서 있으면 `+0x1bc`) |
| **`+0x1bc MOV byte [RBX+0x38b9],0`** | **지우기** |
| **`+0x1c3 / +0x1c8 CALL KiQuantumEnd`** | 부르고, 돌아오면 `+0x1c8` → `+0x1d2 RET` 로 바로 끝낸다 (이 뒤로 `NextThread` 를 안 본다) |
| (QuantumEnd 가 0 이면) `+0x8e CMP [RBX+0x10],0` / `+0x93 JZ +0x1c8` | `NextThread == 0` 이면 아무것도 안 하고 끝 |
| (`NextThread != 0` 이면) `+0x99 TEST byte [RSI+0x2],4` → `+0x212 / +0x217 KiDeferGroupSchedulingPreemption` | 스레드가 `GroupScheduling` (헤더 비트 2) 이면 미룰 수 있음. 이름만 |
| 선점 길: `+0xae KiCaptureTotalCyclesCurrentThread` · `+0xc0 KiAbProcessPreContextSwitch` · `+0xf7 RDI=NextThread; NextThread=0` · `+0x103 CurrentThread=RDI` · `+0x116 State=2` · `+0x13e WaitReason=0x1f (WrDispatchInt)` · `+0x14d / +0x152 KiQueueReadyThread` · `+0x171 / +0x176 KiSwapContext(old,new,R8B=1)` · `+0x18e / +0x193 KiAbProcessPostContextSwitch` | 퀀텀이 아니라 **선점** 으로 바꾸는 줄 (sast/71 질문 1(b) 의 `KiSwapContext` 와 같은 함수) |

- `KiQuantumEnd` 를 부르는 곳 (refs): `KiDispatchInterrupt +0x1c3` · `KiIdleLoop +0xaf` (Idle 스레드가 돌다가 부름). 이 둘뿐이다. `KiDispatchInterrupt` 를 부르는 곳: `KiDpcInterruptBypass +0x16` · `KiDpcInterrupt +0x39a` (조각).
- `KiDispatchInterrupt` 의 순서는 **DPC → QuantumEnd → (그게 아니면) NextThread 선점.**

**지금 우리와 비교.** "`KiQuantumEnd` 를 부른다" 까진 맞다. 다만 부르기 전에 **DPC 를 먼저 돌리고**, `QuantumEnd` 칸을 **지우고** 부르는 게 빠져 있을 수 있다.

### 5. `KiQuantumEnd` 가 다음 스레드를 고르는 꼴

`KiQuantumEnd.asm` 줄기 (오프셋은 call 자리 / ret 자리, 점프는 목적지 − 함수 시작):

| 순서 | 줄 | 하는 일 |
|---|---|---|
| 0 | `+0x17 MOV R12,gs:[0x20]` · `+0x31 MOV R14,[R12+0x8]` · `+0x48 CMP R14,[R12+0x18]` / `+0x4d JZ +0x1c9` | PRCB, CurrentThread. **Idle 이면 퀀텀 일은 건너뜀** (`+0x1c9` 로) |
| 1 | `+0x53 MOV R13,[R14+0x48]` | 이 순간의 `CycleTime` 을 잡는다 (뒤에서 새 목표에 더함) |
| 2 | `+0x57 / +0x5c Feature_HeteroRescheduleFixes…` | 꺼져 있으면 `+0xc7f CMP R13,[R14+0x20]` / `+0xc83 JC +0x1c9` (퀀텀 안 끝났으면 건너뜀). 켜져 있으면 항상 `+0x64` 로 와서 `+0x70 BTS.LOCK [R14+0x40],0` (`ThreadLock`) 잡고 `+0x7d CMP R13,[R14+0x20]` / `+0x81 JNC +0x354` — 같은 확인을 다시 |
| 3 | `+0x354 TEST [R14+0x78],0x100` / `+0x35c JNZ +0xc8e` | `DisableQuantum` 이면 `+0xc8e CMP byte [R14+0xc3],0x10` / `+0xc96 JL +0x362`, 우선순위 ≥ 16 이면 **우선순위 안 내리고** 단위 `0x7f` (`+0xc9c`, `+0xca1 JMP +0x480`) |
| 4 | **(b) 우선순위 내리기** `+0x362 / +0x367 Feature_StackingDecayBoosts…` | 꺼져 있으면 `+0x375 / +0x37a KiComputeNewPriorityNonStacking(Thread, ?, 1)` → R12D = 새 우선순위. 켜져 있으면 `+0x656` 부터 **인라인으로** 계산 (`BasePriority(+0x233)` 에 남은 부스트를 더함: `PriorityDecrement(+0x206)` 낮은 4비트 = `ForegroundBoost`, `DecayBoost(+0x366)` − 1 (0 아래는 0); 15 로 막음; `KiSchedulerForegroundBoostDecayPolicy≠0` 이면 `+0x67c / +0x681 KiClearForegroundBoost`; 아니면 `+0x715 / +0x71a KiComputeThreadPriorityNonStacking`). 이어서 `+0x6e5~+0x6ea AND word [R14+0x206],0xf00f` (UnusualBoost 지움) · `+0x6fa~+0x704 DecayBoost -1`. 안쪽 `KiComputeNewPriorityNonStacking` 도 같은 뜻 (`.asm` `+0x60~+0x76` 최소값 = `BasePriority + DecayBoost − 1`, `+0x7e~+0x9f` 같은 지움·줄임). `KiComputeThreadPriority` 본체는 안 읽음 |
| 5 | **우선순위 적용** `+0x402 / +0x407 KiUpdateVPBackingThreadPriority` → `JZ +0x755` → **`+0x773 / +0x778 KiUpdateThreadPriority(Prcb, Thread, 새우선순위, …)`** | 실제로 `Thread.Priority(+0xc3)` 를 바꾸는 쪽 (안 읽음). 앞서 `+0x38e~+0x3dd` 에서 PRCB 락들을 잡고 `+0x40f~+0x462` 에서 풀음 |
| 6 | **(a) 퀀텀 다시 채우기** `+0x46a / +0x46f KiQueryQuantumReset(Thread)` | 돌려주는 값 = 보통 `QuantumReset(+0x28b)` |
| 7 | `+0x46f MOV R15B,1` · `+0x47b / +0x480 KiTryScheduleNextForegroundBoost` · **`+0x480 IMUL EBX,[KiCyclesPerClockQuantum]`** · **`+0x48d ADD RCX,R13`** · `+0x490 TEST AL,0x20` / `+0x494 BTR.LOCK [R14+0x78],5` · **`+0x49b MOV [R14+0x20],RCX`** | **`QuantumTarget = (이 순간 CycleTime) + QuantumReset × 단위`**. 0 에서 다시 세는 게 아니라 **지금 값에 더한다.** `QuantumDonation`(비트 5) 이 있으면 끈다. `R15B = 1` 이 "이번에 퀀텀을 새로 받았다" 표시 |
| 8 | `+0x1c5 MOV [R14+0x40],RDI` (ThreadLock 해제) → `+0x1c9`: `+0x1d7 CMP [R12+0x38c8],ESI` / `+0x1df JS +0x9e8` | 틱 수가 `[PRCB+0x38c8]` 를 넘었으면 굶은 스레드 스캔 (`KiShouldScanSharedReadyQueue` `+0x9ed` / `KiScanSharedReadyThreads` `+0xa13`). 이름만 |
| 9 | `+0x1e5 CMP [KiGroupSchedulingEnabled],0` / `+0x1ec JNZ +0x71f` → `+0x72e / +0x733 KiGroupSchedulingQuantumEnd` | 그룹 스케줄링 켜져 있을 때. 이름만 |
| 10 | `+0x1ff / +0x204 KiAcquirePrcbLocksForIsolationUnit(Prcb,1,…)` | 락 |
| 11 | **(c)** `+0x20c CMP [R12+0x10],0` / `+0x21a JZ +0x57a` | **`NextThread == 0` 이면** `+0x57a TEST R15B,R15B` / `+0x57d JNZ +0x58d`, 아니면 `+0x57f TEST [R14+0x78],0x1000` (`ForceDeferSchedule`) / `+0x587 JZ +0x220` → **퀀텀을 방금 새로 받았거나 `ForceDeferSchedule` 일 때만 직접 찾는다** |
| 12 | `+0x58d MOV RSI,[R12+0x37e8]` · `+0x5a0 / +0x5a5 memset(0xf0)` · `+0x5b0 / +0x5b5 KiStartRescheduleContext` · `+0x5d0~+0x5e8` 이 CPU 칸 찾기 · `+0x5f3 OR [R9+0x22],2` · **`+0x5fb OR AL,8` → `+0x5fd MOV [R9+0x20],AL`** | sast/71 질문 2·4 의 reschedule 문맥과 같은 모양. 칸 플래그 `0x8` = **`PartialYield`** → sast/71 질문 4(b): 마스크 = **현재 우선순위 "이상"** (같은 것까지, `KiStartReadyQueueEnumeratorForRescheduleContext +0xf6 TEST R11B,8 / +0xfa JNZ` 로 `BTR` 건너뜀) |
| 13 | **`+0x611 / +0x616 KiSearchForNewThreadsForRescheduleContext`** · **`+0x623 / +0x628 KiCommitRescheduleContext`** | **직접 부른다.** `KiSearchForNewThread` / `KiSelectNextThread` 는 안 부른다 (`_calls.tsv` 에 없고, `KiSelectNextThread` 는 이 빌드 PDB 에 이름이 없다). 찾으면 `KiCommitRescheduleContextEntry` 가 `PRCB.NextThread` 에 적는다 (sast/71 질문 2(a)) |
| 14 | `+0x630 TEST AL,AL` / `+0x632 JNZ +0xa1d`, `+0x638 TEST RBX,RBX` / `+0x63b JNZ +0xa1d` | 밀려난 스레드 정리: 락 풀기 (`+0xa1d / +0xa27 KiReleasePrcbLocksForIsolationUnit`), `+0xa7b KiCompleteRescheduleContextEntry`, `+0xa93 KiFlushSoftwareInterruptBatch`, `+0xcfa / +0xd01 KiReadyDeferredReadyList`, 락 다시 `+0xa3f`. 그리고 `+0x641 / +0x645 → +0x220` |
| 15 | `+0x2a1 MOV RBX,[R12+0x10]` · `+0x2a9 JNZ +0xb62` | **다시 `NextThread` 를 읽어서 있으면 바꾸러 간다** |
| 16 | **없으면 (c) "그대로 계속"**: 락 풀고 (`+0x2b2~+0x2f1`) `+0x2f3 CMP R14,[R12+0x18]` / `+0x2f8 JNZ +0x56a` → `+0x570 / +0x575 KeCheckAndApplyBamQos` → **`+0x30e RET`** | 스레드 안 바꾸고 돌아간다. 이때 이미 `QuantumTarget` 은 새로 채워졌고 우선순위는 내려갔을 수 있다 |
| 17 | **(d)** `+0xb62 TEST R15B,R15B` / `+0xb67 MOV byte [R14+0x235],0` | **퀀텀을 새로 받았으면 `Preempted = 0`** (선점 표시 지움) — sast/71 질문 3 의 "`Preempted==0` 이면 맨 뒤" 로 이어진다 |
| 18 | `+0xb6f CMP R14,[R12+0x18]` / `+0xb74 JZ +0x2af` · `+0xb7d MOV [R12+0x10],RDI(0)` · `+0xb85 / +0xb8a KiCaptureTotalCyclesCurrentThread` · **`+0xb90 / +0xb95 KiUpdatePriorityMatrixForRunningTransition(Prcb, NextThread)`** · **`+0xb98 MOV byte [R14+0x283],0x1e`** | Idle 이면 안 바꾸고 `+0x2af` 로 (Idle 루프가 직접 처리). 아니면 `NextThread=0`, `CurrentThread=새 스레드`·`State=2` (sast/71 질문 4(a)), 옛 스레드 `WaitReason = 0x1e (WrQuantumEnd)` |
| 19 | **(d)** `+0xba8 / +0xbad KiQueueReadyThread(Prcb, &락핸들, 옛 스레드)` | **옛 스레드를 준비 목록에 넣는다.** `Preempted==0` 이고 방금 `QuantumTarget` 이 새로 채워졌으니 (`CycleTime < QuantumTarget`) → `KiDeferredReadySingleThread +0x1726 CMP [RSP+0x58],EDI` / `+0x172a JNZ` 를 안 타서 **맨 뒤** (sast/71 질문 3). 새 우선순위(내려간 쪽) 목록에 들어간다 |
| 20 | `+0xbb5 / +0xbba KiAbProcessPreContextSwitch(old,1)` · `+0xbbc KeIsUserVaAccessAllowed` | 주석 없이 이름만 |
| 21 | **(e)** **`+0xbd6 / +0xbdb CALL KiSwapContext`** (RCX=옛 스레드, RDX=새 스레드, R8B=1) | **실제로 바꾸는 줄.** `KiSwapContext` → `SwapContext` 는 sast/71 질문 5 |
| 22 | `+0xbe3 / +0xbe8 KiAbProcessPostContextSwitch(old)` · `+0xbf8 RET` | 이 스레드 차례가 다시 오면 여기서 이어서 끝 |

**한 줄씩 답:**
- (a) 퀀텀 다시 채우기: `QuantumTarget = (그 순간 CycleTime) + QuantumReset × KiCyclesPerClockQuantum` — `+0x46a` ~ `+0x49b`. 이 단계는 **다음 스레드를 찾기 전에** 일어난다 (그래서 찾아도 못 찾아도 채워짐).
- (b) 우선순위 내리기 (= 부스트 한 단계 깎기): **있다.** `KiComputeNewPriorityNonStacking`(`+0x375`) 또는 인라인 + `KiComputeThreadPriorityNonStacking`(`+0x715`), 적용은 `KiUpdateThreadPriority`(`+0x773`). `PriorityDecrement` 를 읽고 지운다 (`+0x6a1`, `+0x6e5~+0x6ea`) — run52 표의 "`PriorityDecrement` 를 쓰는 곳에 `KiQuantumEnd`" 와 맞음. 예외: `DisableQuantum` + 우선순위 ≥ 16 (`+0xc8e`). `KiComputeNewPriority`(172B) 라는 함수도 있는데 `KiQuantumEnd` 는 이걸 안 거치고 안쪽 `NonStacking` 을 직접 부른다. 계산식 자세히는 안 읽음.
- (c) 다음 스레드 고르기: **`KiSearchForNewThread`/`KiSelectNextThread` 를 안 부르고** `KiSearchForNewThreadsForRescheduleContext`+`KiCommitRescheduleContext` 를 `PartialYield` 로 직접 부른다 → 후보 = **(내린 뒤) 현재 우선순위 이상** 의 준비 스레드 중 **가장 높은 우선순위의 맨 앞** (sast/71 질문 2(b)(c)). 이미 `NextThread` 가 정해져 있으면 (`+0x20c` 에서 0 이 아님) 찾지 않고 그걸 쓴다. **같은 우선순위에 다른 스레드가 없으면 `NextThread` 가 0 으로 남아 `+0x30e RET` — 그대로 계속 돈다.**
- (d) 맨 뒤: **예.** `Preempted=0` (`+0xb67`) → `KiQueueReadyThread`(`+0xba8`) → 기본 "맨 뒤" (sast/71 질문 3).
- (e) 바꾸는 줄: `+0xbd6 / +0xbdb KiSwapContext`.

**지금 우리와 비교.**
- (a) 없음 → **추가.**
- (b) 없음 → 에뮬레이터에서는 우선순위가 같게 유지돼도 흉내는 가능 (내림 폭이 작음). 우선순위를 도입하면 **추가.**
- (c) "목록에서 다음 실행 가능한 스레드로 넘긴다" 는 **틀리다**: 현재 우선순위 이상에서 고르고, 없으면 그대로 계속.
- (d) 맨 뒤: 지금 없음 `[추측]` → **추가.**
- (e) 맞음 (같은 개념).

### 6. `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption`

**답.** 둘 다 **퀀텀 사슬 위에 없다** (아니오). 둘 다 `KiQuantumEnd` **뒤에** 오지도 않는다 — `KiQuantumEnd` 는 이 둘을 안 부르고 자기가 직접 `KiSwapContext` 를 부른다 (질문 5 줄 21).

- `KiCheckForThreadDispatch` (`KiCheckForThreadDispatch.asm`): **선점 길.** 락 풀고 IRQL 이 내려갈 때 `NextThread` 가 있으면 바꾼다. `+0xe7 KiUpdatePriorityMatrixForRunningTransition` · `+0xea MOV [RSI+0x283],0x20` (WrPreempted) · `+0x100 / +0x105 KiQueueReadyThread` · **`+0x10e / +0x113 KiSwapContext`** (sast/71 이 찾은 줄). 부르는 곳 (refs): `KiAbThreadRemoveBoostsSlow` · `KiAbProcessPreContextSwitch +0x1e6` · `KiProcessDeferredReadyList +0xaf` · `KeGenericProcessorCallback` · `KeSetPriorityBoost` · `KeRevertToUserGroupAffinityThread` · `KeSetSystemGroupAffinityThread` · `HalpTimerStallExecutionProcessor` · `ExpApplyPriorityBoost` · `KiFastReadyThread +0xa4` · `KeSetIdealProcessorThreadEx` · `KiReadyOutSwappedThreads`. **시계 틱·퀀텀 길은 하나도 없다.**
- `KiHandleDeferredPreemption` (`KiHandleDeferredPreemption.asm`): 스레드가 `DeferPreemption`(`ThreadFlags` 비트 10, `+0x46 BTR.LOCK [RSI+0x78],0xa`) 로 선점을 미뤄 놨다가 `KiSchedulerApc +0xb0` 에서 처리할 때. `+0x274 MOV [RSI+0x283],0x26` (WrDeferredPreempt) · `+0x289 KiQueueReadyThread` · **`+0x297 / +0x29c KiSwapContext`**. 부르는 곳: `KiSchedulerApc +0xb0` **하나뿐**.
- 지도 (`KeClockInterruptNotify` 뿌리): 깊이 6 / 9, 중간에 `KeGenericProcessorCallback` · `KiSchedulerApc` 같은 다른 caller 를 거친다. 퀀텀 사슬 (깊이 1~2 의 `KiUpdateTime`·`KiUpdateRunTime`·`KeAccumulateTicks`) 과는 다른 가지.

**지금 우리와 비교.** 퀀텀과는 별개의 길 (선점). 지금 요청 25 ㄴ-2 에서는 건너뛰어도 된다.

## PE_emulator 가 흉내 낼 최소 규칙

1. **퀀텀 단위 = CPU 사이클.** 스레드마다 `CycleTime` (쓴 사이클) 과 `QuantumTarget` (목표선) 을 따로 가진다. 에뮬레이터에서는 사이클 = 실행한 명령 수 (비례) 로 대신해도 된다 `[추측]`. 숫자는 설정 값으로 둔다 (진짜: 이 VM 사진 기준 단위 4,166,666 사이클, `QuantumReset=2` → 8,333,332 사이클 = 틱의 2/3).
2. **새 스레드**: `CycleTime = 0`, `QuantumReset = 프로세스.QuantumReset`, `QuantumTarget = QuantumReset × 단위` (`KeStartThread +0x1ba~+0x1dc`).
3. **검사는 시계 틱에서만** (`KiUpdateRunTime`): 현재 스레드가 Idle 이 아니고 `CycleTime >= QuantumTarget` 이면 PRCB 의 `QuantumEnd = 1` 로 표시하고 디스패치 수준 인터럽트를 요청한다. 틱 사이에는 검사 안 함 (그래서 목표선을 조금 넘겨서 돈다).
4. **디스패치 인터럽트** (`KiDispatchInterrupt`): ① DPC 를 먼저 돌린다 ② `QuantumEnd` 가 서 있으면 지우고 `KiQuantumEnd` ③ 아니면 `NextThread` 가 있으면 선점.
5. **`KiQuantumEnd`** (퀀텀이 끝난 게 맞을 때만):
   1. (일반 스레드) 올려 받은 부스트 (`ForegroundBoost`·`DecayBoost`) 를 한 단계 깎고 우선순위를 `BasePriority + 남은 부스트` (최대 15) 로 다시 정한다. 부스트가 없으면 `BasePriority` 그대로라서 우선순위는 안 바뀐다 `[정적, 계산 본체는 안 읽음]`. `DisableQuantum` 이고 우선순위 ≥ 16 이면 안 건드리고 퀀텀을 `0x7f` 단위로 크게.
   2. **퀀텀 채우기**: `QuantumTarget = 지금 CycleTime + QuantumReset × 단위`. 다음 스레드를 찾기 **전에** 한다.
   3. **다음 스레드 고르기**: 현재 우선순위 **이상** 중 가장 높은 우선순위의 **맨 앞** (같은 우선순위는 FIFO). `NextThread` 가 이미 있으면 그것.
   4. **없으면 그대로 계속** (이때도 퀀텀은 이미 새로 채워짐).
   5. **있으면**: `Preempted = 0` → 옛 스레드를 준비 목록의 (내린) 우선순위 **맨 뒤** 에 넣고 (`WaitReason = WrQuantumEnd`) → 새 스레드를 `Running` 으로 → 문맥 전환 (sast/71 최소 규칙 5·2번).
6. **Idle 스레드**는 퀀텀 검사 대상이 아니다.

## 동적분석으로 확인해야 할 것 (대기열 Q104 에 붙임, 이번 묶음엔 VM 안 함)

- **퀀텀 길이 숫자**: `dd nt!KiCyclesPerClockQuantum L1` (사진 역산값 `0x3f940a` = 4,166,666 이 맞나), `dd nt!KeMaximumIncrement L1`, `dd nt!KeMinimumIncrement L1`, `dd nt!KeTimeIncrement L1`, `?? @$prcb->MHz` (CPU 0), `dd nt!KiVelocityFlags L1` (비트 `0x40000`), `db nt!KiVariableQuantumEnabled L1`. 산식 (`KeMaximumIncrement × MHz / 30`, 속도 플래그면 `/6` 더) 이 맞는지.
- **시계 간격**: `KeMaximumIncrement` · `KeTimeIncrement` · `db nt!KiClockTimerPerCpuTickScheduling L1` (1 이면 `KiUpdateRunTime` 의 `KiSetClockTimer` 틱리스 갈래를 탄다: `+0x1fe / +0x203` (`KeMaximumIncrement`), `+0x4ea / +0x4ef` (`KeQuantumEndTimerIncrement`)), `db nt!KiGroupSchedulingEnabled L1`. 이 VM 은 `MP (2 procs)` (사진 헤더) → `KiUpdateTime +0x378 JA +0x42f` 의 다중 CPU 갈래 (`KiForwardTick`) 도 탄다.
- **test.exe 초기화 동안 퀀텀이 실제로 끝나는지**: 사진 P1 은 `CycleTime 3.08M < QuantumTarget 8.33M`. 퀀텀이 한 번도 안 끝나면 `KiQuantumEnd` 는 정적 지식으로만 남는다 `[추측]`.
- **시계 인터럽트가 지나는 입구**: 시계 벡터가 `KiInterruptDispatchNoLock` 계열인지, `CycleTime` 갱신이 `KiStartInterruptCycleAccumulation → KiEndThreadCycleAccumulation +0x1bc` 로 되는지, `InterruptRequest` 가 `KiEndInterruptCycleAccumulation +0x25c` 의 `AL=1` 로 이어져 `KiDpcInterruptBypass` 를 타는지.
- 추천 멈춤점 (모두 `nt!` 함수 입구/지점, 조건: test.exe 의 스레드일 때만. 실제 `nt!KiUpdateRunTime+0x..` 는 `.asm` 의 `+0x` 그대로):
  - `nt!KiUpdateRunTime+0x23c` (`rsi`=지금 스레드, `[rsi+0x48]` 과 `[rsi+0x20]` 비교) · `nt!KiUpdateRunTime+0x16a` (QuantumEnd 켜는 줄; 여기까지 오면 퀀텀이 끝난 것)
  - `nt!KiDispatchInterrupt+0x1c3` (QuantumEnd 읽고 지운 뒤 `KiQuantumEnd` 직전)
  - `nt!KiQuantumEnd` 입구, `nt!KiQuantumEnd+0x49b` (`rcx` = 새 `QuantumTarget`), `nt!KiQuantumEnd+0x773` (우선순위 적용 `r9d`=새 우선순위), `nt!KiQuantumEnd+0x30e` (안 바꾸고 돌아감), `nt!KiQuantumEnd+0xbd6` (`rcx`=옛, `rdx`=새, 바꿈)
  - `nt!KeStartThread+0x1dc` (새 스레드 `QuantumTarget` 쓰기, `rax`)
  - 데이터 멈춤점: `ba w1 @$prcb+0x38b9` (QuantumEnd 칸 쓰기. PRCB 주소는 `!pcr`/`@$prcb`).

## 판정

**분석성공.** 여섯 질문 모두 `.asm` 줄로 닫았다. 단 아래 셋은 정적으로 끝까지 못 닫았다:
- 시계 인터럽트가 `CycleTime` 갱신 문 (`KiStartInterruptCycleAccumulation`) 과 인터럽트 끝 처리 (`KiEndInterruptCycleAccumulation` → `KiDpcInterruptBypass`) 를 실제로 지나는지 (IDT 표 / 시계 벡터를 안 읽음 `[추측]`).
- `KiCyclesPerClockQuantum` 의 산식은 `KeInitSystem` 곱셈 상수를 풀어서 `KeMaximumIncrement × MHz / 30` 으로 읽었고 (파이썬으로 확인), 사진의 `QuantumTarget = 0x7f2814` 역산값 4,166,666 과 `T=125,000,000` 일 때 일치한다. 하지만 `KeMaximumIncrement`·`MHz` 실제 값은 사진에 없다.
- `_KPRCB.QuantumEnd` 의 **이름** 은 PDB 에 없어 `[추측]` (오프셋 `+0x38b9` 와 동작은 `[정적]`).

핵심 정정: PE_emulator 의 "명령 `0x10000` 개마다 `KiQuantumEnd` 를 부르고 목록의 다음 스레드로 넘김" 은 **틀림.** 진짜는 **시계 틱에서 `CycleTime >= QuantumTarget` 검사 → `QuantumEnd` 표시 → 디스패치 인터럽트 → `KiQuantumEnd`: (우선순위 조금 내리기 → 퀀텀 새로 채우기 → 현재 우선순위 이상의 맨 앞 스레드 찾기 → 있으면 옛 스레드를 맨 뒤에 넣고 바꿈, 없으면 그대로 계속).**
