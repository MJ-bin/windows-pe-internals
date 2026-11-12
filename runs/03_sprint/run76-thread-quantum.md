# 2026-11-12 run76 — 퀀텀 (Q141b2, PE_emulator 요청 25 ㄴ-2): 시계 틱마다 `KiUpdateRunTime +0x23c ~ +0x244` 가 **`CycleTime ≥ QuantumTarget`** (CPU 사이클) 을 **검사만** 하고, 넘었으면 `+0x16a` PRCB `+0x38b9` (퀀텀 끝 표시 `[추측: 이름]`) 를 켜고 디스패치 인터럽트를 요청한다. 인터럽트 끝에서 `KiDispatchInterrupt` 가 **DPC 다음에** 표시를 지우고 `+0x1c3 KiQuantumEnd` → 부스트 한 단계 깎기 → **퀀텀 다시 채우기** (`+0x49b`, 지금 `CycleTime` 에 더함) → 지금 우선순위 **이상** 의 맨 앞 찾기 → 없으면 그대로 계속 (`+0x30e`), 있으면 옛 스레드를 줄 **맨 뒤** 에 (`+0xba8`) 넣고 `+0xbd6 KiSwapContext`

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "명령 `0x10000` 개마다 `KiQuantumEnd` → 목록의 다음 실행 가능 스레드" 는 **틀림** `[정적]`.
  - 진짜는 **두 단계**다: ① 시계 틱에서 "퀀텀 끝났나" **검사·표시만** (`KiUpdateRunTime`) ② 그다음 디스패치 인터럽트에서 `KiQuantumEnd` 가 실제 일을 한다.
  - 퀀텀은 "남은 양" 이 아니라 **스레드마다 따로 가진 목표선** (`QuantumTarget`) 이고, 재는 단위는 **CPU 사이클** (`CycleTime`) 이다. 명령 수로 대신하는 건 괜찮다 `[추측]`.
  - 퀀텀이 끝나도 **같은 우선순위 이상에 다른 스레드가 없으면 그대로 계속** 돈다 (퀀텀만 새로 채움).
- run44 사진의 test.exe 메인 스레드 값 (`[관찰]`, 새 실행 없음 — 아래 "사진과 맞춰 본 것"):
  - 프로세스·스레드 `QuantumReset` **2**. 첫 `QuantumTarget` **`0x7f2814` = 8,333,332 사이클** → 한 단위 (`KiCyclesPerClockQuantum`) = **4,166,666** (계산).
  - P1a → P1b 사이에 `QuantumTarget` 이 `0x9fa7208` 로 바뀐다 — 첫 퀀텀을 다 쓰고 **`0x24` 단위로 다시 채운 것** 과 맞는다 `[추측: 계산]`.
  - 그래서 퀀텀 길이를 시계 틱으로 바꾸는 읽기가 **둘** 이다 (결과서: 첫 퀀텀 = 틱의 2/3 · flow-writer: 틱의 1/9, 다시 채우면 2틱). 사진 두 값은 뒤쪽과 맞는다. 정하는 건 Q104 (아래 "결과서와 다르게 적은 곳" ⑤).
- 질문마다 "지금 우리" (결과서 sast/72 그대로, 근거 줄은 flow-writer 가 `.asm` 으로 다시 봄):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 사슬 | 시계 주인 CPU: `HalpTimerClockInterrupt +0x103` → `KeClockInterruptNotify +0x2ba KiUpdateTime` → `+0x38b KiUpdateRunTime`. 그 밖 CPU: `KeClockInterruptNotify +0x448 KiUpdateRunTime`. `KeAccumulateTicks` 는 중간 다리가 아니라 `KiUpdateRunTime +0x96` 이 부르는 곁다리 (퀀텀 칸 안 만짐) | "명령 `0x10000` 개마다 `KiQuantumEnd`" → **틀림**. 틱마다 **검사만**, `KiQuantumEnd` 는 그다음 디스패치 인터럽트. "틱 = 명령 N 개" 로 줄이는 건 가능 `[추측]` | `HalpTimerClockInterrupt +0xe0` · `+0xe4` · `+0x103`, `KeClockInterruptNotify +0x259` · `+0x25c` · `+0x2ba` · `+0x448`, `KiUpdateTime +0x38b`, `KiUpdateRunTime +0x96` |
| 2 깎는 자리 | **안 깎는다** — 목표선. `CycleTime (+0x48) ≥ QuantumTarget (+0x20)` 이면 끝 (64비트, **CPU 사이클**). Idle 스레드는 검사 안 함. `CycleTime` 은 인터럽트 들어올 때·스레드 바뀔 때 더해진다. 한 단위 = 전역 `KiCyclesPerClockQuantum` (부팅 때 `KeInitSystem`) | 명령 수 = 사이클 대용은 됨 `[추측]`. 단 **스레드마다 따로**, "쓴 양 ≥ 목표" 꼴. 전역 카운터 하나는 **틀림** | `KiUpdateRunTime +0xb8` · `+0xbc` · `+0xc0` · `+0x23c` · `+0x240` · `+0x244`, `KiEndThreadCycleAccumulation +0x1bc`, `KiCaptureTotalCyclesCurrentThread +0x1d8`, `KeStartThread +0x1c7` · `+0x1dc`, `KeInitSystem +0x2f3` · `+0x314` |
| 3 `QuantumEnd` 켜기 | `KiUpdateRunTime +0x16a` PRCB `+0x38b9 = 1` (이름 `[추측: QuantumEnd]`). 이어서 인터럽트 안 (`NestingLevel ≠ 0`, 보통) 이면 `+0x17b InterruptRequest (+0x6) = 1`, 밖이면 `+0x4a8 HalpInterruptSendIpi` (벡터 `0x2f`) | "표시 → 나중에 디스패치" 두 단계가 없으면 **모자람**. 칸 하나 + "인터럽트 끝에서 읽기" 면 충분 | `KiUpdateRunTime +0x16a` · `+0x171` · `+0x175` · `+0x17b` · `+0x498` · `+0x4a8`, `KiEndInterruptCycleAccumulation +0x246` · `+0x250` · `+0x25c`, `KiInterruptDispatchNoLock +0x5f`, `KiDpcInterruptBypass +0x16` |
| 4 `KiDispatchInterrupt → KiQuantumEnd` | DPC 먼저 (`+0x60`) → `+0x7f / +0x88` 표시 읽기 → `+0x1bc` 지우기 → **`+0x1c3 KiQuantumEnd`** (ret `+0x1c8`) → 바로 끝. 표시가 없고 `NextThread` 가 있으면 선점 (`+0x14d` 넣기, `+0x171 KiSwapContext`) | "`KiQuantumEnd` 를 부른다" 는 **맞음**. DPC 먼저·표시 지우기가 없다면 **추가** | `KiDispatchInterrupt +0x60` · `+0x7f` · `+0x88` · `+0x1bc` · `+0x1c3` · `+0x8e` · `+0x93` · `+0x14d` · `+0x171` |
| 5(a) 퀀텀 채우기 | `QuantumTarget = 그 순간 CycleTime + QuantumReset × 단위` — 0 부터 다시가 아니라 **더한다**. 다음 스레드를 찾기 **전에** (찾든 못 찾든 채워짐) | 없음 → **추가** | `KiQuantumEnd +0x53` · `+0x46a` · `+0x480` · `+0x48d` · `+0x49b` |
| 5(b) 우선순위 내리기 | **있다** — 받은 부스트를 한 단계 깎음 (`+0x375 KiComputeNewPriorityNonStacking` 또는 인라인 + `+0x715 KiComputeThreadPriorityNonStacking`), 적용 `+0x773 KiUpdateThreadPriority`. `DisableQuantum` + 우선순위 ≥ 16 은 빼고 단위 `0x7f` | 없음 → 우선순위를 넣으면 **추가** (부스트가 없으면 `BasePriority` 그대로라 영향 작음 `[추측]`) | `KiQuantumEnd +0x354` · `+0x35c` · `+0x375` · `+0x6a1` · `+0x6e5` · `+0x6ea` · `+0x704` · `+0x715` · `+0x773` · `+0xc8e` · `+0xc9c` |
| 5(c) 다음 스레드 | `NextThread` 가 없으면 (`+0x20c / +0x21a`) `PartialYield` (`+0x5fb`) 로 **`+0x611 KiSearchForNewThreadsForRescheduleContext`** · **`+0x623 KiCommitRescheduleContext`** 를 직접 — 지금 우선순위 **이상** 중 가장 높은 것의 맨 앞. `KiSearchForNewThread` 는 안 부름. 없으면 **`+0x30e RET` 그대로 계속** | "목록의 다음 실행 가능 스레드" → **틀림**: "이상" 에서만 고르고, 없으면 계속 | `KiQuantumEnd +0x20c` · `+0x21a` · `+0x57a` · `+0x5fb` · `+0x5fd` · `+0x611` · `+0x623` · `+0x2a1` · `+0x2a9` · `+0x30e` |
| 5(d) 맨 뒤 | **예** — 퀀텀을 새로 받았으면 `+0xb67 Preempted = 0` → `+0xb98 WaitReason = 0x1e (WrQuantumEnd)` → `+0xba8 KiQueueReadyThread` → 기본 **맨 뒤** (run75 `KiDeferredReadySingleThread +0x1726/+0x172a`) | 없음 `[추측]` → **추가** | `KiQuantumEnd +0xb62` · `+0xb65` · `+0xb67` · `+0xb98` · `+0xba8` |
| 5(e) 바꾸는 줄 | **`+0xbd6 KiSwapContext(옛, 새, 1)`** (ret `+0xbdb`) — `KiSwapThread` 를 안 거친다 | **맞음** (같은 개념) | `KiQuantumEnd +0xbd6` |
| 6 `KiCheckForThreadDispatch` · `KiHandleDeferredPreemption` | 둘 다 **퀀텀 사슬 밖** (아니오). `KiQuantumEnd` 뒤에 오지도 않는다. 선점 길: `+0x10e` / `+0x297` 에서 `KiSwapContext` (`WaitReason` `0x20 WrPreempted` / `0x26 WrDeferredPreempt`) | 지금 요청에선 건너뛰어도 됨 | `KiCheckForThreadDispatch +0xea` · `+0x100` · `+0x10e`, `KiHandleDeferredPreemption +0x274` · `+0x289` · `+0x297` |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/72 끝을 그대로 옮김)

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

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - **규칙 1 의 "틱의 2/3" 은 한 가지 읽기다.** 부팅 때 `KiVelocityFlags` 비트 `0x40000` 이 꺼져 있으면 단위 = 틱의 1/3 → 첫 퀀텀 (2단위) = 틱의 2/3 (결과서). 켜져 있으면 단위를 6으로 한 번 더 나눠 틱의 1/18 → 첫 퀀텀 = **틱의 1/9**, 그리고 다시 채울 때 (`QoS 0`) **`0x24` 단위 = 2틱** 이 된다 `[정적]` 식 (`KeInitSystem +0x2e9 ~ +0x314`, `KiQueryQuantumReset +0x0 ~ +0x3b`).
    - run44 사진 두 값 (K2 `ExpectedRunTime` → `KiShortExecutionCycles`, P1b `QuantumTarget`) 이 **켜진 쪽** 과 맞는다 `[추측: 계산]` (아래 "사진과 맞춰 본 것"). 확정은 Q104 (`KiVelocityFlags`·`KiVariableQuantumEnabled` 두 값).
    - 에뮬레이터는 "단위 · 새 스레드 첫 퀀텀 단위 수 · 다시 채울 단위 수" 를 **따로** 설정으로 두면 두 읽기를 다 흉내 낼 수 있다.
  - **규칙 5 의 "퀀텀이 끝난 게 맞을 때만" 은 5.1·5.2 에 걸린다.** 안 끝났어도 (`+0x81` 을 안 타거나 `+0xc83 JC +0x1c9`) `+0x1c9` 로 와서 `NextThread` 가 이미 있으면 `+0x2a1 / +0x2a9 → +0xb62` 로 바꾼다. 이때는 퀀텀을 새로 안 받았으니 `+0xb65 JZ +0xb6f` 로 **`Preempted` 를 안 지운다** `[정적]` (flow-writer `.asm`). 시계 틱 길은 퀀텀이 끝났을 때만 표시하므로 (`KiUpdateRunTime +0x244`) 이 갈래는 드물다 `[추측]` — QoS·헤테로·그룹 예산으로 `+0x16a` 에 가는 길, `KiIdleLoop +0xaf` 에서 올 때.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, sast/71 의 `KiSwapThread +0xdc/+0xe1 → KiSearchForNewThread` · `+0x243/+0x248 → KiSwapContext` 일치
- **공개 PDB 의 `_KPRCB` 는 `+0x6d0` 에서 잘려 있다** (sast/71). 그래서 `PRCB+0x38b9` (퀀텀 끝 표시) · `+0x38bc` (DPC 요청 플래그) · `+0x38c0` (이전 틱) · `+0x38c8` · `+0x83e8` 는 **이름이 없다**. 오프셋·동작은 `[정적]`, 붙인 이름은 `[추측: 이름]` (결과서 표시 그대로)
  - 이름 있는 칸 (`_KPRCB`): `InterruptRequest +0x6` · `IdleHalt +0x7` · `CurrentThread +0x8` · `NextThread +0x10` · `IdleThread +0x18` · `NestingLevel +0x20` · `ClockOwner +0x21` · `PendingTick +0x22` (비트 0) · `MHz +0x44`
  - `_KTHREAD`: `QuantumTarget +0x20` · `ThreadLock +0x40` · `CycleTime +0x48` · `ThreadFlags +0x78` (`QuantumDonation` 비트 5 · `DisableQuantum` 비트 8 · `ForceDeferSchedule` 비트 12) · `Priority +0xc3` · `BamQosLevel +0x204` · `PriorityDecrement +0x206` · `BasePriority +0x233` · `Preempted +0x235` · `WaitReason +0x283` · `QuantumReset +0x28b` · `DecayBoost +0x366`. `_KPROCESS`: `QuantumReset +0x91`
  - `WaitReason` 숫자 (PDB 열거형, 결과서): `0x1e WrQuantumEnd` · `0x1f WrDispatchInt` · `0x20 WrPreempted` · **`0x21 WrYieldExecution`** · `0x26 WrDeferredPreempt`
- 전역 (PDB 이름 확인, 결과서): `KiCyclesPerClockQuantum` (rva `0xfc5094`, 4바이트). 같은 부팅 계산에서 `KiShortExecutionCycles` · `KiDirectQuantumTarget` · `KiLockQuantumTarget` 도 쓴다 (`KeInitSystem.asm` 주석 이름)

## 목적

- PE_emulator 요청 25 ㄴ-2 (Q141b2): nt 엔진의 "명령 `0x10000` 개마다 `KiQuantumEnd`" 를 진짜 커널에 맞추기 위해 — 시계 인터럽트에서 `KiQuantumEnd` 까지의 사슬, 퀀텀을 재는 칸과 단위, 퀀텀 끝 표시를 켜는 자리, `KiDispatchInterrupt` 가 그 표시를 읽고 지우는 줄, `KiQuantumEnd` 가 다음 스레드를 고르는 꼴 (채우기·우선순위·고르기·맨 뒤·바꾸기).
- 고르기·줄 세우기 규칙 자체는 run75 (Q141b1) 를 가리키기만 한다. 기다림·깨움·타이머 만료는 Q141c.

## 방법

- 정적: `_workspace/sast/72_thread-quantum.md` — ntoskrnl
  - 새 지도: `run_ghidra.sh map ntoskrnl.exe KeClockInterruptNotify` → `ms/disassemble/ntoskrnl/_map/KeClockInterruptNotify_{edges,nodes}.tsv` (667함수)
    - **`KiDispatchInterrupt`·`KiQuantumEnd` 는 지도에 없다.** 표시 → `KiDispatchInterrupt` 사이가 함수 호출이 아니라 "소프트웨어 인터럽트 + 인터럽트 끝 처리" 라서 (지도는 호출만 따라감). 그 이음새는 결과서가 이름으로 따로 찾았다.
  - 새로 뽑음 23개 (모두 depth 0):
    - 요청 대상 6: `KeClockInterruptNotify` (2456B) · `KiUpdateRunTime` (1801B) · `KiDispatchInterrupt` (653B) · `KiQuantumEnd` (3393B) · `KiUpdateTime` (1859B) · `KeAccumulateTicks` (1804B)
    - 이음새 9: `HalpTimerClockInterrupt` · `KiRequestSoftwareInterrupt` · `KiInterruptDispatchNoLock` · `KiEndInterruptCycleAccumulation` · `KiStartInterruptCycleAccumulation` · `KiEndThreadCycleAccumulation` · `KiDpcInterrupt` (+ 조각 `FUN_1406b5753`) · `KiDpcInterruptBypass`
    - 퀀텀 길이 3: `KiQueryQuantumReset` (96B) · `KeInitSystem` (1553B) · `KiCaptureTotalCyclesCurrentThread`
    - 우선순위 이름 3: `KiComputeNewPriority` · `KiComputeNewPriorityNonStacking` · `KiTryScheduleNextForegroundBoost`
    - 질문 6 둘: `KiCheckForThreadDispatch` (420B) · `KiHandleDeferredPreemption` (854B)
    - 있던 것 읽기만: `KeStartThread` (run52)
  - 부르는 쪽: refs 12개 (`KiCyclesPerClockQuantum` · `KiQuantumEnd` · `KiDispatchInterrupt` · `KiDpcInterruptBypass` 등) → `ms/disassemble/ntoskrnl/_refs/`
- 결과서: `_workspace/sast/72_thread-quantum_result.md` (**분석성공**. 정적으로 못 닫은 셋 — 시계 인터럽트가 지나는 입구, 전역 실제 값, `+0x38b9` 이름 — 은 아래 "아직 확인 필요")
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KiUpdateRunTime +0x16a` · `+0x23c` · `+0x240` · `+0x244`, `KiUpdateTime +0x38b`, `KeClockInterruptNotify +0x2ba` · `+0x448`, `KiDispatchInterrupt +0x1bc` · `+0x1c3`, `KiQuantumEnd +0x30e` · `+0x46a` · `+0x49b` · `+0x611` · `+0x623` · `+0xba8` · `+0xbd6`. 사진 `ms/logs/04_baggage-read_K3.txt:688` · `_P0.txt:688` (`QuantumTarget 0x7f2814`)
- flow-writer 도 `.asm` 으로 다시 봤다: `HalpTimerClockInterrupt +0xe0` · `+0xe4` · `+0xf9` · `+0x103`, `KeClockInterruptNotify +0x259` · `+0x25c` · `+0x2a1` · `+0x424` · `+0x42a` · `+0x434` · `+0x4d7` · `+0x4dd` · `+0x547`, `KiUpdateTime +0x375` · `+0x378` · `+0x37e` · `+0x42f` · `+0x436` · `+0x46a` · `+0x48e` · `+0x4d0`, `KiUpdateRunTime +0x96` · `+0xb8` · `+0xbc` · `+0xc0` · `+0x171` · `+0x175` · `+0x17b` · `+0x17f` · `+0x22f` · `+0x236` · `+0x433` · `+0x498` · `+0x49d` · `+0x4a8`, `KeAccumulateTicks +0x16` · `+0x23` · `+0x26` · `+0x28`, `KiStartInterruptCycleAccumulation +0x21`, `KiEndThreadCycleAccumulation +0x20` · `+0x1b5` · `+0x1b9` · `+0x1bc`, `KiCaptureTotalCyclesCurrentThread +0x1d1` · `+0x1d5` · `+0x1d8`, `KiInterruptDispatchNoLock +0x37` · `+0x44` · `+0x56` · `+0x5d` · `+0x5f`, `KiEndInterruptCycleAccumulation +0x226` · `+0x246` · `+0x250` · `+0x254` · `+0x256` · `+0x25a` · `+0x25c` · `+0x26f`, `KiDpcInterruptBypass +0x0` · `+0xd` · `+0x16`, 조각 `FUN_1406b5753 +0xf0` · `+0x104` · `+0x10e` · `+0x113` · `+0x127` (= `KiDpcInterrupt +0x363` · `+0x377` · `+0x381` · `+0x386` · `+0x39a`), `KiDispatchInterrupt +0x39` · `+0x3a` · `+0x43` · `+0x47` · `+0x4d` · `+0x60` · `+0x7e` · `+0x7f` · `+0x86` · `+0x88` · `+0x8e` · `+0x93` · `+0x99` · `+0xae` · `+0xc0` · `+0xf7` · `+0x103` · `+0x116` · `+0x13e` · `+0x14d` · `+0x171` · `+0x18e` · `+0x1c8` · `+0x1d2` · `+0x212`, `KiQuantumEnd +0x17` · `+0x22` · `+0x31` · `+0x48` · `+0x4d` · `+0x53` · `+0x57` · `+0x5e` · `+0x70` · `+0x7d` · `+0x81` · `+0x1c5` · `+0x1c9` · `+0x1d7` · `+0x1df` · `+0x1e5` · `+0x1ec` · `+0x1ff` · `+0x20c` · `+0x21a` · `+0x220 ~ +0x2a9` · `+0x2f3` · `+0x2f8` · `+0x354` · `+0x35c` · `+0x362` · `+0x375` · `+0x402` · `+0x46f` · `+0x47b` · `+0x480` · `+0x48d` · `+0x490` · `+0x494` · `+0x56a ~ +0x575` · `+0x57a` · `+0x57d` · `+0x57f` · `+0x587` · `+0x58d` · `+0x5a0` · `+0x5b0` · `+0x5f3` · `+0x5fb` · `+0x5fd` · `+0x630 ~ +0x645` · `+0x656` · `+0x67c` · `+0x6a1` · `+0x6e5` · `+0x6ea` · `+0x6fa` · `+0x704` · `+0x715` · `+0x72e` · `+0x773` · `+0x9ed` · `+0xa13` · `+0xa7b` · `+0xa93` · `+0xb62 ~ +0xbd6` · `+0xbe3` · `+0xbf8` · `+0xc7f` · `+0xc83` · `+0xc8e` · `+0xc96` · `+0xc9c` · `+0xca1` · `+0xcfa ~ +0xd01`, `KeStartThread +0x1ba` · `+0x1c1` · `+0x1c7` · `+0x1dc`, `KeInitSystem +0x2a0 ~ +0x31d`, `KiQueryQuantumReset +0x0 ~ +0x5f`, `KiDeferredReadySingleThread +0x216 ~ +0x260`, `KiQueueReadyThread +0x749` · `+0x74e`, `KeInitThread +0xa0` · `+0xa6` · `+0xa9`, `KiCheckForThreadDispatch +0xd7` · `+0xe2` · `+0xea` · `+0x100` · `+0x10e`, `KiHandleDeferredPreemption +0x46` · `+0x251` · `+0x274` · `+0x289` · `+0x297`
- 값 대조 (메인 스레드 `ffff828598445080`, 새 실행 없음): run44 덤프 읽기 로그 `ms/logs/04_baggage-read_<K2 ~ P2>.txt` — 구획 `02b KTHREAD` (`QuantumTarget` · `CycleTime` · `ExpectedRunTime` · `Priority` · `BamQosLevel` · `QuantumReset` · `DecayBoost` · `ThreadFlags.DisableQuantum`) 와 `01b KPROCESS` (`QuantumReset`) — flow-writer

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!HalpTimerClockInterrupt` | **`+0x103`** | **`+0x108`** | **`nt!KeClockInterruptNotify`** | PRCB `PendingTick` (`+0x22` 비트 0) 이 서 있을 때만 (`+0xe0 TEST` · `+0xe4 JNZ +0xf9`). 셋째 인자 R8D = 0 (`+0xf9`) | [정적] |
| `nt!KeClockInterruptNotify` | **`+0x2ba`** | **`+0x2bf`** | **`nt!KiUpdateTime`** | `ClockOwner (+0x21) ≠ 0` (시계 주인 CPU, `+0x259` · `+0x25c JZ +0x424`) | [정적] |
| `nt!KiUpdateTime` | `+0x46a` | `+0x46f` | `nt!KiForwardTick` | CPU 2개 이상 (`+0x375` · `+0x378 JA +0x42f`) 이고 `KiClockTimerPerCpuTickScheduling == 0` (`+0x42f` · `+0x436`) 일 때 먼저. 다른 CPU 에 틱 넘기기 (이름만) | [정적] |
| `nt!KiUpdateTime` | **`+0x38b`** | **`+0x390`** | **`nt!KiUpdateRunTime`** | 시스템 시간 갱신 뒤 (`+0x48e JMP +0x37e` 로 합쳐짐) | [정적] |
| `nt!KeClockInterruptNotify` | **`+0x448`** | **`+0x44d`** | **`nt!KiUpdateRunTime`** | `ClockOwner == 0` (그 밖 CPU). `+0x424 / +0x42a` → `+0x4d7 / +0x4dd` → `+0x434 RtlGetInterruptTimePrecise` → 여기 | [정적] |
| `nt!KiUpdateRunTime` | `+0x96` | `+0x9b` | `nt!KeAccumulateTicks` | 조건 없음. 틱 장부 — **퀀텀 칸 (`+0x20`·`+0x48`·`+0x28b`) 을 안 만짐** (결과서 `grep`) | [정적] |
| `nt!KeAccumulateTicks` | `+0x28` | `+0x2d` | `nt!KiCheckForTimerExpiration` | 6번째 인자 ≠ 4 일 때 (`+0x16` · `+0x23` · `+0x26`). 타이머 만료 → Q141c | [정적] |
| `nt!KiUpdateRunTime` | `+0x4a8` | `+0x4ad` | `nt!HalpInterruptSendIpi` | 퀀텀 끝 표시 뒤 `NestingLevel == 0` 일 때 (`+0x175 JZ +0x433`), `+0x498 EDX = 0x2f` (DISPATCH 벡터) | [정적] |
| `nt!KiStartInterruptCycleAccumulation` | `+0x21` | `+0x26` | `nt!KiEndThreadCycleAccumulation` | `NestingLevel == 0` 일 때 → `+0x1bc` 지금 스레드 `CycleTime` 에 지난 구간 사이클 더함 | [정적] |
| `nt!KiInterruptDispatchNoLock` | `+0x56` | `+0x5b` | `nt!KiEndInterruptCycleAccumulation` | 인터럽트 끝 (예시 입구). `+0x246` `InterruptRequest` 읽기 · `+0x250` 지우기 · 이전 IRQL < 2 면 `+0x25c AL = 1` | [정적] |
| `nt!KiInterruptDispatchNoLock` | `+0x5f` | `+0x64` | `nt!KiDpcInterruptBypass` | AL ≠ 0 (`+0x5d JZ` 안 탐) | [정적] |
| `nt!KiDpcInterruptBypass` | `+0x16` | `+0x1b` | `nt!KiDispatchInterrupt` | IRQL 2 로 올리고 `STI` (`+0x0 ~ +0xd`) 뒤 | [정적] |
| `nt!KiDpcInterrupt` | `+0x39a` (조각 `FUN_1406b5753 +0x127`) | `+0x39f` (조각 `+0x12c`) | `nt!KiDispatchInterrupt` | 진짜 벡터 `0x2f` 쪽. `+0x381 HalPerformEndOfInterrupt` · `+0x386 STI` 뒤 | [정적] |
| `nt!KiDispatchInterrupt` | `+0x60` | `+0x65` | `nt!KiSwapStacksAndRetireDpcList` | **DPC 먼저** (`+0x4d TEST [PRCB+0x38bc],0xbf`) | [정적] |
| `nt!KiDispatchInterrupt` | **`+0x1c3`** | **`+0x1c8`** | **`nt!KiQuantumEnd`** | `PRCB+0x38b9 ≠ 0` (`+0x7f` · `+0x88 JNZ +0x1bc`), `+0x1bc` 지운 뒤. 돌아오면 `+0x1d2 RET` | [정적] |
| `nt!KiDispatchInterrupt` | `+0x14d` | `+0x152` | `nt!KiQueueReadyThread` | 표시 0 + `NextThread ≠ 0` (선점), `+0x13e WaitReason = 0x1f` | [정적] |
| `nt!KiDispatchInterrupt` | `+0x171` | `+0x176` | `nt!KiSwapContext` | 위와 같은 선점 갈래 | [정적] |
| `nt!KiQuantumEnd` | `+0x375` | `+0x37a` | `nt!KiComputeNewPriorityNonStacking` | 퀀텀 끝 + `Feature_StackingDecayBoosts` 꺼짐 (`+0x362`) — 부스트 깎은 새 우선순위 | [정적] |
| `nt!KiQuantumEnd` | `+0x715` | `+0x71a` | `nt!KiComputeThreadPriorityNonStacking` | 위 기능 켜짐 → `+0x656` 부터 인라인 계산 갈래 | [정적] |
| `nt!KiQuantumEnd` | `+0x773` | `+0x778` | `nt!KiUpdateThreadPriority` | 새 우선순위 적용 (본문 안 읽음) | [정적] |
| `nt!KiQuantumEnd` | **`+0x46a`** | **`+0x46f`** | **`nt!KiQueryQuantumReset`** | 보통 `QuantumReset (+0x28b)` → `+0x480` × 단위 → `+0x49b` `QuantumTarget` | [정적] |
| `nt!KiQuantumEnd` | **`+0x611`** | **`+0x616`** | **`nt!KiSearchForNewThreadsForRescheduleContext`** | `NextThread == 0` + (방금 채움 또는 `ForceDeferSchedule`), 칸 플래그 `PartialYield` (`+0x5fb`) | [정적] |
| `nt!KiQuantumEnd` | **`+0x623`** | **`+0x628`** | **`nt!KiCommitRescheduleContext`** | 찾은 것을 `NextThread` 에 (run75 `…Entry +0x149`) | [정적] |
| `nt!KiQuantumEnd` | `+0x570` | `+0x575` | `nt!KeCheckAndApplyBamQos` | 안 바꾸고 돌아가는 길 (`+0x2f8 JNZ +0x56a`) → `+0x30e RET` (이름만) | [정적] |
| `nt!KiQuantumEnd` | `+0xb85` | `+0xb8a` | `nt!KiCaptureTotalCyclesCurrentThread` | 바꾸기 직전 `CycleTime` 정산 | [정적] |
| `nt!KiQuantumEnd` | `+0xb90` | `+0xb95` | `nt!KiUpdatePriorityMatrixForRunningTransition` | `CurrentThread = 새` · 새 `State = 2` (run75) | [정적] |
| `nt!KiQuantumEnd` | **`+0xba8`** | **`+0xbad`** | **`nt!KiQueueReadyThread`** | 옛 스레드 → 준비 줄 (`WaitReason = 0x1e` `+0xb98`) | [정적] |
| `nt!KiQuantumEnd` | **`+0xbd6`** | **`+0xbdb`** | **`nt!KiSwapContext`** | `(옛, 새, R8B = 1)` (`+0xbcd`) → `SwapContext` (run75) | [정적] |
| `nt!KiCheckForThreadDispatch` | `+0x10e` | `+0x113` | `nt!KiSwapContext` | 선점 길 (`+0xea WaitReason = 0x20`, `+0x100 KiQueueReadyThread`) — 퀀텀 아님 | [정적] |
| `nt!KiHandleDeferredPreemption` | `+0x297` | `+0x29c` | `nt!KiSwapContext` | 미뤄 둔 선점 (`+0x46 BTR [+0x78],0xa`, `+0x274 WaitReason = 0x26`) — 퀀텀 아님. 부르는 곳 `KiSchedulerApc +0xb0` 하나 | [정적] |

- `KiQuantumEnd` 를 부르는 곳 (`_refs/KiQuantumEnd.tsv`): `KiDispatchInterrupt +0x1c3` · `KiIdleLoop +0xaf` **둘뿐**. `KiDispatchInterrupt` 를 부르는 곳: `KiDpcInterruptBypass +0x16` · `KiDpcInterrupt +0x39a` (조각).
- `KiCyclesPerClockQuantum` 을 쓰는 곳 (`_refs/KiCyclesPerClockQuantum.tsv`): 쓰기 `KeInitSystem +0x2f3` · `+0x314` (둘뿐). 읽기 (`IMUL`) 15곳 — `KeStartThread +0x1c7` · `KiQuantumEnd +0x480` · `KiDeferredReadySingleThread +0x247` · `KiQueueReadyThread +0x74e` · `KeYieldExecution` (Ghidra `Ordinal_6`) `+0x19b` · `KiDirectSwitchThread +0x54f` · `KiWakePriQueueWaiter +0x1ca` · `KeSetPriorityThread +0xd5` · `KeSetBasePriorityThread +0x1c0` · `KeSetActualBasePriorityThread +0x169` · `KeBoostPriorityThread +0x18a` · `KeRemovePriQueue +0x529` · `KeSetPriorityAndQuantumProcess +0x238` (+ 조각 하나) · `KiNormalPriorityReadyScan +0xa5` · `KiScanSharedReadyThreads +0x168`.
- 줄기 밖 (이름만): ETW · 락 (`KiAcquire/ReleasePrcbLocksForIsolationUnit`) · `KiUpdateVPBackingThreadPriority` · `KiClearForegroundBoost` · `KiGroupSchedulingQuantumEnd` · `KiCheckPreferredHeteroProcessor` · `KiComputeThreadQos` · `KzRefreshWorkloadProperties` · `KiScanSharedReadyThreads` (굶은 스레드 스캔) · `KiForwardTick` · `KiSetClockTimer` · 헤테로 갈래.

### 시계 틱 사슬 (질문 1)

- **시계 주인 CPU 는 `KiUpdateTime` 을 거친다**: `KeClockInterruptNotify +0x259 CMP [RDI+0x21],BL` (`ClockOwner`) → 0 이 아니면 `+0x2ba KiUpdateTime` → 시스템 시간 갱신 뒤 `+0x38b KiUpdateRunTime`.
  - `+0x2a1` `KiClockOwnerOneShotRequestState == 1` 갈래는 조각을 돌아 같은 `+0x2ba` 로 온다 (결과서).
  - CPU 가 둘 이상이면 (`+0x375 CMP EDI,1` · `+0x378 JA +0x42f`) `KiClockTimerPerCpuTickScheduling == 0` 일 때 먼저 `+0x46a KiForwardTick` (다른 CPU 에 틱 넘기기) 를 하고 `+0x48e JMP +0x37e` 로 와서 `KiUpdateRunTime` 에 닿는다.
- **그 밖 CPU 는 바로**: `+0x25c JZ +0x424` → `+0x448 KiUpdateRunTime`. `HalpTimerClockIpiRoutine +0x1f` 도 `KeClockInterruptNotify` 를 부른다 (refs). 주인 CPU 의 `KiForwardTick` 이 그쪽으로 이어진다는 건 `[추측]` (`KiForwardTick` 안 읽음).
- **`KeAccumulateTicks` 는 곁다리**: `KiUpdateRunTime +0x96` 이 조건 없이 부른다. 틱 장부 (`PRCB+0x8644` 등) · 스레드 틱 칸 · DPC 감시 · `+0x28 KiCheckForTimerExpiration` (Q141c). 퀀텀 칸은 안 만진다 (결과서).
- 맨 앞 `HalpTimerClockInterrupt +0xe0 TEST [RAX+0x22],1` (PRCB `PendingTick` 비트 0) — 서 있을 때만 `+0x103 KeClockInterruptNotify`. 인자 이름은 `[공백]` (결과서).

### 퀀텀 검사와 길이 (질문 2)

- **검사 줄** (`KiUpdateRunTime`, `RBX` = PRCB):
  - `+0xb8 MOV RSI,[RBX+0x8]` (지금 스레드) · `+0xbc CMP RSI,[RBX+0x18]` · `+0xc0 JNZ +0x226` — **Idle 이 아닐 때만** 아래로.
  - `+0x22f CMP [RBX+0x83e8],R14D` · `+0x236 JS +0x4f4` 는 퀀텀이 아니라 굶은 스레드 스캔 시간 확인 (이름만).
  - **`+0x23c MOV RAX,[RSI+0x48]` (`CycleTime`) · `+0x240 CMP RAX,[RSI+0x20]` (`QuantumTarget`) · `+0x244 JNC +0x16a`** — 같거나 크면 퀀텀 끝. 둘 다 64비트.
  - 안 끝났어도 `+0x16a` 로 가는 길 (결과서): QoS 단계가 바뀜 (`+0x24a ~ +0x25b`, `KiComputeThreadQos` `+0x254`), 헤테로 코어 (`+0x26d KiCheckPreferredHeteroProcessor`), 스케줄링 그룹 예산 (`+0x107 ~ +0x164`, `+0x14f JLE`). 일반 스레드엔 해당 없음 `[추측]`.
- **`CycleTime` 이 느는 자리** — `KiUpdateRunTime` 은 읽기만 한다:
  - 인터럽트 들어올 때: `KiInterruptSubDispatch*` `+0x33` → `KiStartInterruptCycleAccumulation` (결과서) → `+0x21 KiEndThreadCycleAccumulation` → **`+0x1b5 ~ +0x1bc`** `CycleTime += 지난 구간` (같은 함수 `+0x20` `NestingLevel = 1`).
  - 스레드 바뀔 때: `KiCaptureTotalCyclesCurrentThread +0x1d1 ~ +0x1d8` (같은 더하기). 부르는 곳: `KiSwapThread +0x40` · `KiQuantumEnd +0xb85` · `KiDispatchInterrupt +0xae` · `KiCheckForThreadDispatch +0xd7` · `KiHandleDeferredPreemption +0x251` · `KeYieldExecution +0x1e9` 등.
  - 시계 인터럽트가 이 입구 (`KiInterruptDispatch*`) 를 지나는지는 IDT 를 안 읽어서 `[추측]` → Q104.
- **한 퀀텀 길이**:
  - `QuantumReset`: `KeStartThread +0x1ba MOVZX EAX,[RDI+0x91]` (프로세스 `QuantumReset`) → `+0x1c1 MOV [RBX+0x28b],AL`. 프로세스 값의 뿌리는 `PspComputeQuantum` · `PspForegroundQuantum` 표 (이름만, `.asm` 없음 — 짐 지도 3.12 `+0x91` 줄).
  - 한 단위 = **`KiCyclesPerClockQuantum`**. 새 스레드 첫 퀀텀: `KeStartThread +0x1c7 IMUL EAX,[KiCyclesPerClockQuantum]` → `+0x1dc MOV [RBX+0x20],RAX` (`CycleTime` 은 0 이라 안 더함).
  - **부팅 때 `KeInitSystem` 이 계산** (flow-writer 가 곱셈 상수를 다시 풀어 맞춤, 결과서와 같음): `+0x2aa` `KeMaximumIncrement` × `+0x2b0` `KiProcessorBlock[0]->MHz (+0x44)` (`+0x2b4 IMUL`) = **T**.
    - `+0x2b8 ~ +0x2c8` → T/10 (= 시계 틱 한 번의 사이클 수 — `KeMaximumIncrement` 는 100ns 단위, `MHz` 는 1µs 사이클 수).
    - `+0x2cc ~ +0x2dd` → **`KiShortExecutionCycles = T/2400`** (flow-writer 보탬 — 결과서엔 없음).
    - `+0x2e3 ~ +0x2e6` → T/30 → `+0x2f3 KiCyclesPerClockQuantum` · `+0x2f9 KiDirectQuantumTarget` · `+0x302 KiLockQuantumTarget` (3배).
    - `+0x2e9 TEST [KiVelocityFlags],0x40000` → 서 있으면 (`+0x308 JZ` 안 탐) `+0x30a ~ +0x311` **6 으로 한 번 더 나눠** `+0x314` 다시 씀 (`+0x31d KiLockQuantumTarget` 은 2배로).
    - 그래서 단위는 **틱의 1/3** (플래그 꺼짐) 또는 **틱의 1/18** (켜짐).
  - 다시 채울 때 몇 단위인가 — `KiQueryQuantumReset`: `KiVelocityFlags 0x40000` (`+0x0`) **이고** `KiVariableQuantumEnabled ≠ 0` (`+0x13`) 이면 `BamQosLevel (+0x204)` 에 따라 `0x24` (`+0x3b`, QoS 0·3·4) · `0x12` (`+0x5a`, QoS 1) · `2` (`+0x53`, QoS 2·5·6·7), 그 밖이나 플래그가 하나라도 꺼져 있으면 `QuantumReset` 그대로 (`+0xa` → `+0x40`). `KiDeferredReadySingleThread +0x216 ~ +0x247` 에도 같은 모양이 인라인으로 있다 (flow-writer).
  - `DisableQuantum` + 우선순위 ≥ 16 → 단위 `0x7f` (`KiQuantumEnd +0xc9c`, 사실상 끝없음).

### 퀀텀 끝 표시 → `KiDispatchInterrupt` (질문 3·4)

- **표시 켜기**: **`KiUpdateRunTime +0x16a MOV byte [RBX+0x38b9],1`**. 이름이 PDB 에 없어 `[추측: QuantumEnd]` — 이 칸을 쓰는 곳은 뽑은 `.asm` 467개 중 여기와 `KiDispatchInterrupt` (읽고 지움) 둘뿐 (결과서).
- **디스패치 요청** (`KiRequestSoftwareInterrupt(Prcb, 2)` 를 펼친 꼴, 결과서):
  - `+0x171 CMP [RBX+0x20],R13B` (`NestingLevel`) — 0 이 아니면 (시계 인터럽트 처리 안, 보통) **`+0x17b MOV byte [RBX+0x6],1`** (`InterruptRequest`) → `+0x17f JMP +0x294` 끝.
  - 0 이면 `+0x175 JZ +0x433` → `+0x498 MOV EDX,0x2f` → **`+0x4a8 HalpInterruptSendIpi`** — 자기 CPU 에 벡터 `0x2f` (DISPATCH_LEVEL). AMD 우회 갈래 (`+0x433 ~ +0x497`) 는 건너뜀.
- **표시가 `KiDispatchInterrupt` 로 이어지는 길** `[정적]` (예시 입구 `KiInterruptDispatchNoLock`):
  1. `+0x37 KiInterruptSubDispatchNoLock` (ISR 본체) → `+0x44 HalPerformEndOfInterrupt` → `+0x56 KiEndInterruptCycleAccumulation(Prcb, 이전 IRQL)`.
  2. `KiEndInterruptCycleAccumulation`: `+0x226 NestingLevel = 0` → **`+0x246 CMP [RBX+0x6],0` (읽기) · `+0x250 MOV [RBX+0x6],0` (지우기)** → `+0x256 CMP R15B,2` · `+0x25a JNC +0x26d` — **이전 IRQL < 2 면 `+0x25c AL = 1`**, 아니면 `+0x26f HalRequestSoftwareInterrupt(2)` 로 미룸. `IdleHalt ≠ 0` 이면 AL = 0 (`+0x254`).
  3. `+0x5d JZ` 를 안 타면 **`+0x5f KiDpcInterruptBypass`** → IRQL 2 · `STI` → **`+0x16 KiDispatchInterrupt`**.
  4. 진짜 벡터 `0x2f` 로 오면 `KiDpcInterrupt` (조각 `FUN_1406b5753` 안): `+0x363 ~ +0x377` IRQL 2 · `+0x381 HalPerformEndOfInterrupt` · `+0x386 STI` · **`+0x39a KiDispatchInterrupt`**.
  - `KiDpcInterruptBypass` 를 부르는 입구는 여럿 (`KiInterruptDispatch` · `…NoLock` · `…NoLockNoEtw` · `KiChainedDispatch` · `KiHvInterruptDispatch` 등, 결과서 refs). 시계 인터럽트가 어느 입구를 지나는지, 벡터 `0x2f` → `KiDpcInterrupt` 인지는 IDT 를 안 읽어 `[추측]` → Q104.
- **`KiDispatchInterrupt` 순서 = DPC → 퀀텀 끝 → (아니면) 선점**:
  - `+0x39 CLI` · `+0x3a` PRCB · `+0x43` 지금 스레드 · `+0x47` 이전 IRQL 을 `WaitIrql (+0x186)` 에.
  - `+0x4d TEST [RBX+0x38bc],0xbf` → `+0x60 KiSwapStacksAndRetireDpcList` — **DPC 먼저 다 돌림** (타이머 만료 DPC 포함 `[추측]`).
  - `+0x7e STI` → **`+0x7f MOVZX EAX,[RBX+0x38b9]` · `+0x86 TEST` · `+0x88 JNZ +0x1bc`** → **`+0x1bc MOV byte [RBX+0x38b9],0`** → **`+0x1c3 CALL KiQuantumEnd`** → `+0x1c8` → `+0x1d2 RET` (이 뒤로 `NextThread` 를 안 본다).
  - 표시가 0 이면 `+0x8e CMP [RBX+0x10],0` · `+0x93 JZ +0x1c8` (`NextThread` 없으면 끝). 있으면 선점: `+0x99` 그룹 스케줄링이면 `+0x212 KiDeferGroupSchedulingPreemption` 로 미룰 수 있음 · `+0xae KiCaptureTotalCyclesCurrentThread` · `+0xf7 / +0x103` `NextThread` → `CurrentThread` · `+0x116 State = 2` · `+0x13e WaitReason = 0x1f` · `+0x14d KiQueueReadyThread` · `+0x171 KiSwapContext`.
- 틱 사슬에는 DISPATCH 요청이 퀀텀 말고도 있다 (이름만): `KeAccumulateTicks +0x171` · `KiUpdateRunTime +0x557 KiSetDpcRequestFlag` · `+0x561 KiRequestSoftwareInterrupt` (스캔) · `KiUpdateTime +0x4d0 KeInsertQueueDpc`.

### `KiQuantumEnd` — 다음 스레드를 고르는 꼴 (질문 5)

| 순서 | 줄 | 하는 일 |
|---|---|---|
| 0 | `+0x17` PRCB · `+0x31` 지금 스레드 · `+0x48 CMP R14,[R12+0x18]` · `+0x4d JZ +0x1c9` | **Idle 이면 퀀텀 일 건너뜀** |
| 1 | `+0x53 MOV R13,[R14+0x48]` | 이 순간 `CycleTime` (새 목표에 더할 값) |
| 2 | `+0x57 Feature_HeteroRescheduleFixes…` → 켜짐 `+0x70` `ThreadLock` 잡고 `+0x7d CMP R13,[R14+0x20]` · `+0x81 JNC +0x354` / 꺼짐 `+0x5e JZ` → `+0xc7f CMP` · `+0xc83 JC +0x1c9` | **퀀텀이 정말 끝났나 다시 확인.** 안 끝났으면 `+0x1c9` (채우기·우선순위 건너뜀) |
| 3 | `+0x354 TEST [R14+0x78],0x100` · `+0x35c JNZ +0xc8e` | `DisableQuantum` 이면 `+0xc8e` 우선순위 ≥ 16 → 안 내리고 단위 `0x7f` (`+0xc9c`, `+0xca1 JMP +0x480`) |
| 4 | **(b)** `+0x362 Feature_StackingDecayBoosts…` → 꺼짐 `+0x375 KiComputeNewPriorityNonStacking` / 켜짐 `+0x656` 인라인 (`+0x6a1` `PriorityDecrement` 읽기 · `+0x6e5 ~ +0x6ea` `AND 0xf00f` · `+0x6fa ~ +0x704` `DecayBoost − 1`, `+0x67c KiClearForegroundBoost` 또는 `+0x715 KiComputeThreadPriorityNonStacking`) | 받은 부스트를 한 단계 깎은 새 우선순위 (최대 15) |
| 5 | `+0x402 KiUpdateVPBackingThreadPriority` → **`+0x773 KiUpdateThreadPriority`** | 새 우선순위 적용 (본문 안 읽음) |
| 6 | **(a)** **`+0x46a KiQueryQuantumReset`** | 보통 `QuantumReset (+0x28b)` |
| 7 | `+0x46f MOV R15B,1` · `+0x47b KiTryScheduleNextForegroundBoost` · **`+0x480 IMUL EBX,[KiCyclesPerClockQuantum]`** · **`+0x48d ADD RCX,R13`** · `+0x494 BTR [R14+0x78],5` · **`+0x49b MOV [R14+0x20],RCX`** | **`QuantumTarget = 그 순간 CycleTime + QuantumReset × 단위`**. `QuantumDonation` 끔. `R15B = 1` = "이번에 새로 받음" (`+0x22` 에서 0 으로 시작) |
| 8 | `+0x1c5` `ThreadLock` 풀기 → `+0x1c9` · `+0x1d7 / +0x1df` 굶은 스레드 스캔 (`+0x9ed` · `+0xa13`) · `+0x1e5 / +0x1ec KiGroupSchedulingEnabled` → `+0x72e KiGroupSchedulingQuantumEnd` · `+0x1ff` PRCB 락 | 이름만 |
| 9 | **(c)** `+0x20c CMP [R12+0x10],0` · `+0x21a JZ +0x57a` → `+0x57a TEST R15B` · `+0x57d JNZ +0x58d` / `+0x57f TEST [R14+0x78],0x1000` · `+0x587 JZ +0x220` | `NextThread` 가 없고, 방금 채웠거나 `ForceDeferSchedule` 일 때만 직접 찾는다 |
| 10 | `+0x58d` 문맥 버퍼 `PRCB+0x37e8` · `+0x5a0 memset` · `+0x5b0 KiStartRescheduleContext` · `+0x5f3 OR [R9+0x22],2` · **`+0x5fb OR AL,8` · `+0x5fd MOV [R9+0x20],AL`** | 칸 플래그 `0x8` = **`PartialYield`** → 받는 마스크 = 지금 우선순위 **이상** (run75 질문 4(b): `…EnumeratorForRescheduleContext +0xf6 / +0xfa` 로 같은 비트를 안 지움) |
| 11 | **`+0x611 KiSearchForNewThreadsForRescheduleContext`** · **`+0x623 KiCommitRescheduleContext`** | 직접 부른다. `KiSearchForNewThread` 는 안 부름 |
| 12 | `+0x630 ~ +0x645` | 밀려난 스레드 정리 (`+0xa1d` 락 · `+0xa7b KiCompleteRescheduleContextEntry` · `+0xa93 KiFlushSoftwareInterruptBatch` · `+0xd01 KiReadyDeferredReadyList`), `+0x645 JMP +0x220` |
| 13 | **`+0x2a1 MOV RBX,[R12+0x10]` · `+0x2a9 JNZ +0xb62`** | `NextThread` 를 다시 읽어 있으면 바꾸러 감 |
| 14 | **없으면**: `+0x2f3 CMP R14,[R12+0x18]` · `+0x2f8 JNZ +0x56a` → `+0x570 KeCheckAndApplyBamQos` → `+0x575 JMP` → **`+0x30e RET`** | **그대로 계속.** 퀀텀은 이미 새로 채워졌고 우선순위는 내려갔을 수 있다 |
| 15 | **(d)** `+0xb62 TEST R15B,R15B` · `+0xb65 JZ +0xb6f` · **`+0xb67 MOV byte [R14+0x235],0`** | 퀀텀을 새로 받았으면 `Preempted = 0` → run75 "`Preempted == 0` 이면 맨 뒤" |
| 16 | `+0xb6f / +0xb74` Idle 이면 `+0x2af` · `+0xb7d NextThread = 0` · `+0xb85 KiCaptureTotalCyclesCurrentThread` · `+0xb90 KiUpdatePriorityMatrixForRunningTransition` · **`+0xb98 MOV byte [R14+0x283],0x1e`** | `CurrentThread = 새` · 새 `State = 2` · 옛 스레드 `WaitReason = WrQuantumEnd` |
| 17 | **(d)** **`+0xba8 KiQueueReadyThread(Prcb, &락, 옛)`** | 옛 스레드를 준비 줄에 — `Preempted == 0` 이고 방금 채워 `CycleTime < QuantumTarget` → `KiDeferredReadySingleThread +0x1726 / +0x172a` 를 안 타서 **맨 뒤**, 새 (내린) 우선순위 줄 |
| 18 | `+0xbb5 KiAbProcessPreContextSwitch` · `+0xbbc KeIsUserVaAccessAllowed` | 이름만 |
| 19 | **(e)** **`+0xbd6 KiSwapContext(옛, 새, R8B = 1)`** | 실제로 바꾸는 줄 → `SwapContext` (run75) |
| 20 | `+0xbe3 KiAbProcessPostContextSwitch` · `+0xbf8 RET` | 이 스레드 차례가 다시 오면 여기서 끝 |

- 부르는 순서 요약: **(b) 우선순위 → (a) 채우기 → (c) 찾기 → 없으면 계속 / 있으면 (d) 맨 뒤 → (e) 바꾸기.** 채우기가 찾기보다 앞이라, 못 찾아도 퀀텀은 새로 생긴다.
- `KiComputeNewPriority` (172B) 라는 함수도 있지만 `KiQuantumEnd` 는 안쪽 `NonStacking` 을 직접 부른다 (결과서).

### 선점 길 둘 — 퀀텀 사슬 밖 (질문 6)

- **`KiCheckForThreadDispatch`**: 락을 풀고 IRQL 이 내려갈 때 `NextThread` 가 있으면 바꾼다. `+0xd7 KiCaptureTotalCyclesCurrentThread` · `+0xe2 KiUpdatePriorityMatrixForRunningTransition` · `+0xea WaitReason = 0x20 (WrPreempted)` · `+0x100 KiQueueReadyThread` · **`+0x10e KiSwapContext`**. 부르는 곳 (결과서 refs): `KiAbProcessPreContextSwitch` · `KiProcessDeferredReadyList` · `KeGenericProcessorCallback` · `KeSetPriorityBoost` · `KiFastReadyThread +0xa4` 등 — **시계 틱·퀀텀 길은 없다**.
- **`KiHandleDeferredPreemption`**: 스레드가 선점을 미뤄 놨다가 (`ThreadFlags` 비트 10 `DeferPreemption`, `+0x46 BTR.LOCK`) `KiSchedulerApc +0xb0` 에서 처리할 때. `+0x251 KiCaptureTotalCyclesCurrentThread` · `+0x274 WaitReason = 0x26 (WrDeferredPreempt)` · `+0x289 KiQueueReadyThread` · **`+0x297 KiSwapContext`**.
- 지도로는 `KeClockInterruptNotify` 뿌리에서 깊이 6 / 9 에 나오지만, 사이에 `KeGenericProcessorCallback` · `KiSchedulerApc` 같은 다른 caller 를 거친다 — 퀀텀 사슬 (깊이 1~2) 과 다른 가지.
- → run75 의 `[추측]` "퀀텀 끝·선점은 이 둘에서 `KiSwapContext` 를 직접" 은 **선점 쪽만 맞다.** 퀀텀 끝은 `KiQuantumEnd +0xbd6` 이 직접 부른다 (아래 "run75 에 반영한 것").

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[시계 인터럽트 하나]   (어느 인터럽트 입구를 지나는지는 [추측] → Q104)
HalpTimerClockInterrupt
 │  +0xe0 / +0xe4  PRCB PendingTick (+0x22 비트 0) 이 서 있을 때만
 └─ (1) +0x108  KeClockInterruptNotify
      │  +0x259 / +0x25c  ClockOwner (+0x21) ≠ 0 ?
      ├─ (1) +0x2bf  KiUpdateTime                                시계 주인 CPU
      │    ├─ (1) +0x46f  KiForwardTick                          CPU 2개 이상 + KiClockTimerPerCpuTickScheduling == 0
      │    └─ (2) +0x390  KiUpdateRunTime                        (아래와 같은 함수)
      └─ (2) +0x44d  KiUpdateRunTime                             그 밖 CPU
           ├─ (1) +0x9b   KeAccumulateTicks                      틱 장부 · +0x2d KiCheckForTimerExpiration (Q141c) — 퀀텀 칸 안 만짐
           │  +0xb8 ~ +0xc0     지금 스레드 == IdleThread 면 건너뜀
           │  +0x23c ~ +0x244   CycleTime (+0x48) ≥ QuantumTarget (+0x20) → +0x16a
           │  +0x16a            PRCB+0x38b9 = 1                    [추측: 이름 QuantumEnd]
           │  +0x171 / +0x175   NestingLevel ≠ 0 → +0x17b InterruptRequest (+0x6) = 1
           └─ (2) +0x4ad  HalpInterruptSendIpi (EDX = 0x2f)      NestingLevel == 0 일 때

(CycleTime 이 느는 곳)
KiStartInterruptCycleAccumulation (인터럽트 들어올 때)
 └─ +0x26  KiEndThreadCycleAccumulation                         +0x1bc CycleTime += 지난 구간
KiCaptureTotalCyclesCurrentThread (스레드 바뀔 때)              +0x1d8 CycleTime += 지난 구간

[인터럽트 끝]   예: KiInterruptDispatchNoLock
 ├─ (1) +0x3c   KiInterruptSubDispatchNoLock                   ISR 본체
 ├─ (2) +0x49   HalPerformEndOfInterrupt
 ├─ (3) +0x5b   KiEndInterruptCycleAccumulation                +0x246 InterruptRequest 읽기 · +0x250 지우기 · 이전 IRQL < 2 → +0x25c AL = 1
 └─ (4) +0x64   KiDpcInterruptBypass                           AL ≠ 0 (+0x5d)
      └─ +0x1b  KiDispatchInterrupt
(또는 벡터 0x2f → KiDpcInterrupt +0x39f KiDispatchInterrupt — Ghidra 조각 FUN_1406b5753 +0x12c)

KiDispatchInterrupt
 ├─ (1) +0x65   KiSwapStacksAndRetireDpcList                   DPC 먼저
 │  +0x7f ~ +0x88   PRCB+0x38b9 ≠ 0 → +0x1bc 지우고
 ├─ (2) +0x1c8  KiQuantumEnd                                   → +0x1d2 RET (NextThread 안 봄)
 │  (표시 0) +0x8e / +0x93  NextThread == 0 이면 끝
 ├─ (3) +0x152  KiQueueReadyThread(지금)                        선점 — +0x13e WaitReason 0x1f
 └─ (4) +0x176  KiSwapContext(지금, NextThread, 1)

KiQuantumEnd                (부르는 곳: KiDispatchInterrupt +0x1c3 · KiIdleLoop +0xaf)
 │  +0x48 / +0x4d    지금 == Idle → +0x1c9
 │  +0x53            R13 = CycleTime
 │  +0x7d / +0x81 (또는 +0xc7f / +0xc83)  안 끝났으면 → +0x1c9 (아래 (1)~(3) 건너뜀)
 │  +0x354 / +0x35c  DisableQuantum + 우선순위 ≥ 16 → 단위 0x7f (+0xc9c)
 ├─ (1) +0x37a  KiComputeNewPriorityNonStacking                 (기능 켜짐이면 +0x656 인라인 → +0x71a KiComputeThreadPriorityNonStacking)
 ├─ (2) +0x778  KiUpdateThreadPriority                         새 우선순위 적용
 ├─ (3) +0x46f  KiQueryQuantumReset                            보통 QuantumReset (+0x28b)
 │  +0x480 × KiCyclesPerClockQuantum · +0x48d + CycleTime · +0x49b QuantumTarget = 그 값 · R15B = 1
 │  +0x20c / +0x21a  NextThread == 0 이고 (방금 채움 또는 ForceDeferSchedule) → 찾기, +0x5fb PartialYield
 ├─ (4) +0x616  KiSearchForNewThreadsForRescheduleContext      지금 우선순위 이상, 가장 높은 것의 맨 앞
 ├─ (5) +0x628  KiCommitRescheduleContext                      → NextThread
 │  +0x2a1 / +0x2a9  NextThread 없으면 → +0x30e RET (그대로 계속)
 │  +0xb65 / +0xb67  방금 채웠으면 Preempted = 0
 │  +0xb7d NextThread = 0
 ├─ (6) +0xb8a  KiCaptureTotalCyclesCurrentThread
 ├─ (7) +0xb95  KiUpdatePriorityMatrixForRunningTransition     CurrentThread = 새 · 새 State = 2
 │  +0xb98 옛 WaitReason = 0x1e (WrQuantumEnd)
 ├─ (8) +0xbad  KiQueueReadyThread(옛)                          → KiDeferredReadySingleThread → 맨 뒤 (run75)
 └─ (9) +0xbdb  KiSwapContext(옛, 새, 1)                        → SwapContext (run75)
```

## 사진과 맞춰 본 것 (run44 메인 스레드 `ffff828598445080`, 새 실행 없음)

- 일곱 장 모두 `dt nt!_KTHREAD ffff828598445080` (같은 스레드) 다. 사진은 커널 디버거가 멈춘 순간이다.

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 | 로그 줄 (`ms/logs/04_baggage-read_<P>.txt`) |
|---|---|---|---|---|---|---|---|---|
| 프로세스 `QuantumReset (+0x91)` | 2 | 2 | 2 | 2 | 2 | 2 | 2 | K2 353 · K3~P1 358 · P1a~P2 356 |
| 스레드 `QuantumReset (+0x28b)` | 0 | 2 | 2 | 2 | 2 | 2 | 2 | K2 834 · K3~P1 839 · P1a~P2 837 |
| `QuantumTarget (+0x20)` | 0 | `0x7f2814` | 같음 | 같음 | 같음 | **`0x9fa7208`** | 같음 | K2 683 · K3~P1 688 · P1a~P2 686 |
| `CycleTime (+0x48)` | 0 | 0 | `0x5dc0` | `0x2f0940` | `0x73c620` | `0x10b4c20` | `0x1422ba0` | K2 688 · K3~P1 693 · P1a~P2 691 |
| (십진) `CycleTime` | 0 | 0 | 24,000 | 3,082,560 | 7,587,360 | 17,517,600 | 21,113,760 | |
| `ExpectedRunTime (+0x54)` | `0x4c4b40` | 같음 | 같음 | `0x3ccc10` | 같음 | `0x22642b` | `0x94889` | K2 690 · K3~P1 695 · (run75 표) |
| `BamQosLevel (+0x204)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K2 800 · K3~P1 805 · P1a~P2 803 |
| `Priority (+0xc3)` | 0 | 8 | 8 | 8 | 8 | **9** | **9** | K2 763 · K3~P1 768 · P1a~P2 766 |
| `DecayBoost (+0x366)` | 0 | 0 | 0 | 0 | 0 | **1** | **1** | K2 864 · K3~P1 869 · P1a~P2 867 |
| `ThreadFlags.DisableQuantum` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K2 731 · K3~P1 736 · P1a~P2 734 |

- **첫 퀀텀이 `KeStartThread` 식과 맞다** `[관찰]` + `[정적]`: `K2 → K3` 에서 스레드 `QuantumReset` 0 → 2 (= 프로세스 값 2, `+0x1ba / +0x1c1`), `QuantumTarget` 0 → `0x7f2814` (`+0x1c7 / +0x1dc`), `CycleTime` 0. → **`KiCyclesPerClockQuantum` = 8,333,332 / 2 = 4,166,666 (`0x3f940a`)** `[관찰 값, 계산]` (결과서와 같음).
- **`CycleTime` 은 스레드마다 사이클로 쌓인다** `[관찰]`: 0 → 24,000 → … → 21,113,760. 사진 사이마다 늘기만 한다.
- **첫 퀀텀은 P1a ~ P1b 사이에 다 썼다** `[추측: 계산]`:
  - P1a `CycleTime` 7,587,360 < 8,333,332 (아직 남음), P1b 엔 `QuantumTarget` 이 `0x9fa7208` (167,408,136) 으로 바뀌었다.
  - 다시 채우는 줄은 모두 `CycleTime + n × 단위` 꼴이다 (`KiQuantumEnd +0x48d` · `KiDeferredReadySingleThread +0x253 ADD RCX,R13`). n 을 넣어 보면 `0x24` 만 맞는다: 167,408,136 − 36 × 4,166,666 = **17,408,160** — P1a (7,587,360) 와 P1b (17,517,600) 사이. (n = 2 · `0x12` 는 채운 순간 `CycleTime` 이 P1b 값보다 커야 해서 안 맞음.)
  - 그 순간 `CycleTime` 17,408,160 > 옛 목표 8,333,332 → 첫 퀀텀은 이미 넘었다. 어느 줄이 채웠는지는 모른다 (후보: `KiQuantumEnd +0x49b`, `KiDeferredReadySingleThread +0x260` 등) → Q104.
- **`0x24` 는 플래그 둘이 다 켜져야 나온다** `[정적]`: `KiQueryQuantumReset` (`+0x0` `KiVelocityFlags 0x40000` · `+0x13` `KiVariableQuantumEnabled`) · `KiDeferredReadySingleThread +0x216` · `+0x229` 같은 조건. 이 스레드는 `BamQosLevel 0` `[관찰]` 이라 `KiQueryQuantumReset` 은 `+0x25 JZ +0x3b` → `0x24` 를 돌려준다 — 맞아떨어진다.
- **K2 `ExpectedRunTime` 도 같은 쪽을 가리킨다** `[추측: 계산]`:
  - `KeInitThread +0xa0 ~ +0xa9` = `KiShortExecutionCycles << 4` → K2 `0x4c4b40` 이면 `KiShortExecutionCycles` = `0x4c4b4` = 312,500 (짐 지도 3.14 `ExpectedRunTime` 줄, D1).
  - `KeInitSystem +0x2dd` 는 이 전역을 **T/2400** 으로 쓴다 → T ≈ **750,000,000**.
  - T = 750,000,000 이면 T/30 = 25,000,000. 플래그가 꺼져 있었다면 첫 `QuantumTarget` 은 50,000,000 이어야 한다. **6 으로 나눈 4,166,666** 일 때만 `0x7f2814` 가 나온다.
  - 결과서의 "`KeMaximumIncrement × MHz = 125,000,000`" 은 플래그 꺼짐을 가정한 읽기다. 그 값이면 `KiShortExecutionCycles` 는 52,083 (`0xcb73`) 이라 K2 `ExpectedRunTime` 과 안 맞는다.
  - 정리: 사진 세 값 (K2 `ExpectedRunTime` · K3 `QuantumTarget` · P1b `QuantumTarget`) 이 **"`KiVelocityFlags 0x40000` 켜짐 · `KiVariableQuantumEnabled` 켜짐 · T ≈ 750,000,000"** 하나로 다 맞는다. 둘 다 `[추측: 계산]` 이고 전역 값은 사진에 없다 → Q104 에서 `dd` 몇 줄로 닫힌다.
  - 이 읽기면 틱 한 번 = T/10 = 75,000,000 사이클, 단위 = 틱의 1/18, 첫 퀀텀 = 틱의 1/9, 다시 채운 `0x24` 단위 = 2틱. 메인 스레드가 P2 까지 쓴 사이클 (21,113,760) 은 틱 하나 (75,000,000) 에 못 미친다.
- **P1b `Priority 9` = `BasePriority 8` + `DecayBoost 1`** `[관찰]` (`_P1b.txt:766` · `:867`): run75 의 `[추측: 부스트]` 는 **`DecayBoost`** 쪽이다. `KiQuantumEnd` 는 퀀텀 끝마다 `DecayBoost` 를 1 깎는다 (`+0x6fa ~ +0x704`) → 다음 퀀텀 끝에 8 로 돌아갈 것 `[추측]`. 누가 1 로 올렸는지는 모른다 (깨움 부스트 쪽 `[추측]` → Q141c).
- **K3 ~ P1a 는 `QuantumTarget` 이 그대로다** → 그 사이 이 스레드는 퀀텀을 새로 받은 적이 없다 `[추측: 읽기]`. run75 가 본 P1 → P1a 의 넘김 5번 (`ContextSwitches` `0x17` → `0x1c`) 은 퀀텀 끝 길이 아닌 쪽 (선점·기다림) 으로 보인다 `[추측]`.

## 확인된 것

- `[정적]` 시계 틱 사슬: `HalpTimerClockInterrupt +0x103` → `KeClockInterruptNotify` → (시계 주인) `+0x2ba KiUpdateTime` → `+0x38b KiUpdateRunTime` / (그 밖) `+0x448 KiUpdateRunTime`. `KeAccumulateTicks` 는 `KiUpdateRunTime +0x96` 곁다리.
- `[정적]` 퀀텀은 깎는 게 아니라 목표선: `KiUpdateRunTime +0x23c ~ +0x244` `CycleTime ≥ QuantumTarget` (CPU 사이클). Idle 은 검사 안 함 (`+0xb8 ~ +0xc0`). `CycleTime` 은 `KiEndThreadCycleAccumulation +0x1bc` · `KiCaptureTotalCyclesCurrentThread +0x1d8` 에서 는다.
- `[정적]` 퀀텀 끝 표시 `PRCB+0x38b9` (`KiUpdateRunTime +0x16a` 켬, `KiDispatchInterrupt +0x1bc` 지움) — 이름은 `[추측: QuantumEnd]`. 디스패치 요청은 `+0x17b InterruptRequest = 1` 또는 `+0x4a8 HalpInterruptSendIpi(0x2f)`.
- `[정적]` `KiDispatchInterrupt`: DPC (`+0x60`) → 퀀텀 끝 (`+0x1c3 KiQuantumEnd`, 돌아오면 끝) → 아니면 선점 (`+0x171 KiSwapContext`).
- `[정적]` `KiQuantumEnd`: 우선순위 (`+0x375`/`+0x715` → `+0x773`) → 채우기 (`+0x49b`, `CycleTime` 에 더함) → `PartialYield` 로 찾기 (`+0x611` · `+0x623`) → 없으면 `+0x30e RET` / 있으면 `Preempted = 0` (`+0xb67`) · `WaitReason 0x1e` · `+0xba8 KiQueueReadyThread` (맨 뒤) · **`+0xbd6 KiSwapContext`**. 부르는 곳은 `KiDispatchInterrupt +0x1c3` · `KiIdleLoop +0xaf` 둘.
- `[정적]` `KiCheckForThreadDispatch +0x10e` · `KiHandleDeferredPreemption +0x297` 는 선점 길. 퀀텀 사슬 위에 없다.
- `[정적]` 한 단위 `KiCyclesPerClockQuantum` 은 부팅 때 `KeInitSystem +0x2f3` (플래그면 `+0x314`) 가 `KeMaximumIncrement × MHz / 30` (플래그면 `/180`) 으로 쓴다.
- `[관찰]` run44: 프로세스·스레드 `QuantumReset 2`, 첫 `QuantumTarget 0x7f2814` (K3), `CycleTime` 0 → 21,113,760 (P2), P1b 에 `QuantumTarget 0x9fa7208`, `BamQosLevel 0`, P1b `DecayBoost 1` · `Priority 9`.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① 결과서 "P1b·P2 의 `QuantumTarget 0x9fa7208` 은 같은 스레드가 아닌 것 같아 근거로 안 썼다 `[추측]`" → **같은 스레드다** (일곱 장 모두 `dt nt!_KTHREAD ffff828598445080`, 로그 각 장 `02b KTHREAD` 첫 줄). 그 값은 `0x24` 단위 다시 채우기와 맞는다 `[추측: 계산]` (위 "사진과 맞춰 본 것").
- ② 결과서 "`T = KeMaximumIncrement × MHz = 125,000,000`, 단위 = 틱의 1/3, 첫 퀀텀 = 틱의 2/3" → **플래그 꺼짐을 가정한 읽기**. flow-writer 가 `KeInitSystem +0x2dd` (`KiShortExecutionCycles = T/2400`) 를 보태 K2 `ExpectedRunTime` 과 맞춰 보면 T ≈ 750,000,000 · 플래그 켜짐 · 단위 = 틱의 1/18 이 사진 세 값과 다 맞는다 `[추측: 계산]`. 결과서 읽기도 지우지 않고 둘 다 둔다 → Q104 로 가른다 (flow.md 4장).
- ③ 결과서 질문 6 "`+0xe7 KiUpdatePriorityMatrixForRunningTransition`" (`KiCheckForThreadDispatch`) → 부르는 줄은 **`+0xe2`** (ret `+0xe7`) (flow-writer `.asm`).
- ④ 결과서 질문 5 줄 14 "`+0xcfa / +0xd01 KiReadyDeferredReadyList`" → 부르는 줄은 **`+0xd01`** (ret `+0xd06`), `+0xcfa` 는 인자 `LEA` (flow-writer `.asm`).
- ⑤ flow-writer 보탬: `KiQuantumEnd` 는 퀀텀이 안 끝난 채 불려도 `NextThread` 가 이미 있으면 바꾼다 (`+0x1c9 → +0x2a1 / +0x2a9 → +0xb62`). 그때는 `+0xb65 JZ +0xb6f` 로 `Preempted` 를 안 지운다 `[정적]` (`R15B` 를 다른 갈래 `+0x3a0` · `+0x8b3` · `+0x97e` · `+0xc72` 에서도 쓰는지는 다 안 따라감).
- ⑥ flow-writer 보탬: 다시 채울 단위 수를 고르는 같은 모양이 `KiDeferredReadySingleThread +0x216 ~ +0x247` 에도 인라인으로 있다 (QoS 4 → `0x24` `+0x242`, 아니면 `+0x23c JNZ +0x1108` 쪽 — 안 따라감). `KiQueueReadyThread +0x749` 는 `KiQueryQuantumReset` 을 부른다.
- ⑦ flow-writer 보탬: 사진 대조 전체 (`BamQosLevel 0`, `DecayBoost 1` = `Priority 9` 의 출처) — 결과서엔 K3 ~ P1 `QuantumTarget`·`CycleTime` 만.
- ⑧ `_calls.tsv` 에 `alias:` 로 나온 callee 셋은 이름이 하나뿐이다 (`pdb_aliases.py --lookup ntoskrnl.exe` 셋 다 "별칭 무리 아님 — 이름이 하나뿐"): `KiQuantumEnd +0x375` → `KiComputeNewPriorityNonStacking` (rva `0x5c9bfc`), `+0x715` → `KiComputeThreadPriorityNonStacking` (rva `0x5c9cbc`), `+0xa7b` → `KiCompleteRescheduleContextEntry` (rva `0x2ccee0`) — PDB 는 C++ 꾸밈 이름 하나. 그래서 글에는 그 이름을 그대로 썼다.

## run75 에 반영한 것

- run75 문서 맨 위에 정정 한 줄: `[추측]` "퀀텀 끝·선점은 `KiCheckForThreadDispatch +0x10e` · `KiHandleDeferredPreemption +0x297` 에서 `KiSwapContext` 를 직접" → 이 둘은 **선점 길만**. 퀀텀 끝은 `KiQuantumEnd +0xbd6`.
- run75 `KeYieldExecution +0x1fc` 의 `WaitReason = 0x21` (`WrYieldExecution` 로 보임 `[추측]`) → **`[정적]`** (PDB 열거형, 결과서 sast/72 "읽는 법").
- run75 "정적 — 다음 바퀴 후보" 에 대기열 번호 **Q143** (오케스트레이터가 plan.md 에 붙인 번호).

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- **새 짐 칸 없음, 2장 숫자 그대로.** 3.14 `_KTHREAD` 머리에 "Q78d4 에서 볼 것 — 퀀텀 줄" 한 줄 (run52 · run75 줄과 같은 꼴): `QuantumReset (+0x28b)` ← `KeStartThread +0x1ba / +0x1c1` (프로세스 `+0x91` 복사) · `QuantumTarget (+0x20)` 첫 값 ← `+0x1c7 / +0x1dc` (`2 × KiCyclesPerClockQuantum`, 사진 K3 `0x7f2814`), 다시 채움 ← `KiQuantumEnd +0x49b` 등 · `CycleTime (+0x48)` ← `KiEndThreadCycleAccumulation +0x1bc` · `KiCaptureTotalCyclesCurrentThread +0x1d8`.
- 3.14 `ExpectedRunTime` 줄 근거 칸과 7.4 실마리에 "전역 `KiShortExecutionCycles` 를 쓰는 곳 = 부팅 `KeInitSystem +0x2dd` (`T/2400`)" 덧붙임 (값은 그대로 D1).
- 7.2 `_KTHREAD` 가리킴 줄에 run76 한 마디. 전역 `KiCyclesPerClockQuantum` (부팅 `Ⓑ`) 과 PRCB `+0x38b9` 는 체크리스트 밖이라 표로 안 옮김.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만.

1. **전역 값 — 퀀텀 길이 두 읽기를 가른다**: `dd nt!KiCyclesPerClockQuantum L1` (사진 역산 `0x3f940a`), `dd nt!KiShortExecutionCycles L1` (역산 `0x4c4b4`), `dd nt!KeMaximumIncrement L1`, `?? @$prcb->MHz` (CPU 0), **`dd nt!KiVelocityFlags L1` (비트 `0x40000`)**, **`db nt!KiVariableQuantumEnabled L1`**, `dd nt!KiDirectQuantumTarget L1` · `dd nt!KiLockQuantumTarget L1`. 결과서 읽기 (꺼짐, T = 125,000,000) 와 flow-writer 읽기 (켜짐, T ≈ 750,000,000) 중 어느 쪽인가.
2. **시계 간격**: `dd nt!KeTimeIncrement L1` · `dd nt!KeMinimumIncrement L1` · `db nt!KiClockTimerPerCpuTickScheduling L1` (1 이면 `KiUpdateRunTime +0x1fe` · `+0x4ea KiSetClockTimer` 틱리스 갈래) · `db nt!KiGroupSchedulingEnabled L1`. CPU 2개라 `KiUpdateTime +0x378` 다중 CPU 갈래 (`KiForwardTick`) 를 탄다.
3. **초기화 동안 퀀텀이 실제로 끝나나 · 누가 다시 채우나**: 사진은 "P1a ~ P1b 사이 한 번" 으로 읽힌다 `[추측: 계산]`. `ba w8 <메인 스레드>+0x20` (`QuantumTarget` 쓰기) 에서 `k` — `KiQuantumEnd+0x49b` 인가, `KiDeferredReadySingleThread+0x260` 인가. 그때 `rbx`/`r14` 가 메인 스레드인지.
4. **시계 인터럽트가 지나는 입구**: 시계 벡터가 `KiInterruptDispatchNoLock` 계열인지 (`!idt`), `CycleTime` 갱신이 `KiStartInterruptCycleAccumulation → KiEndThreadCycleAccumulation +0x1bc` 로 되는지, `InterruptRequest` 가 `KiEndInterruptCycleAccumulation +0x25c` (`AL = 1`) → `KiDpcInterruptBypass` 를 타는지, 벡터 `0x2f` → `KiDpcInterrupt` 인지. WinDbg 가 조각 `FUN_1406b5753 +0x127` 을 `nt!KiDpcInterrupt+0x39a` 로 보여주는지도 `[추측]`.
5. **`DecayBoost` 를 1 로 올린 곳** (P1a ~ P1b): `ba w1 <메인 스레드>+0x366` 에서 `k` (깨움 부스트면 Q141c 와 같이).
   - (run77) 같은 구간 P1b 에 `AdjustIncrement 1` `[관찰]` (`_P1b.txt:821`) — 깨우는 쪽이 `Increment` 를 적는 `KiProcessThreadWaitList +0x9f` 와 맞아 "`Increment 1` 로 깨워짐" 으로 읽힌다 `[추측: 읽기]`. 깨움 부스트가 출처일 가능성이 커졌지만 계산 본체 (`KiDeferredReadySingleThread +0x6d` 갈래) 는 안 읽음 (`runs/03_sprint/run77-thread-wake.md`).
- 추천 멈춤점 (결과서, 모두 `nt!` — 조건: test.exe 스레드): `KiUpdateRunTime+0x23c` (`rsi` = 지금 스레드, `[rsi+0x48]` · `[rsi+0x20]`) · `KiUpdateRunTime+0x16a` (여기까지 오면 퀀텀 끝) · `KiDispatchInterrupt+0x1c3` · `KiQuantumEnd` 입구 · `KiQuantumEnd+0x49b` (`rcx` = 새 `QuantumTarget`) · `+0x773` · `+0x30e` (안 바꿈) · `+0xbd6` (`rcx` 옛, `rdx` 새) · `KeStartThread+0x1dc` (`rax`) · 데이터 `ba w1 @$prcb+0x38b9`.

정적 — 다음 바퀴 후보 — 대기열 **Q144** (오케스트레이터가 붙인 번호):

- ① **IDT 표 읽기**: 시계 벡터 · 벡터 `0x2f` 가 어느 입구 (`KiInterruptDispatchNoLock` 계열 · `KiDpcInterrupt`) 로 가나 — 결과서가 의심한 표 `PTR_KiDivideErrorFaultShadow_141008280+0x280`. 동적 4 의 절반을 정적으로 닫을 수 있음.
- ② **다시 채우는 다른 길**: `KiDeferredReadySingleThread +0x211 / +0x214` (`SIL` — 채울지 정하는 값) 과 `+0x23c JNZ +0x1108` 갈래, `KiQueueReadyThread +0x749` 앞 조건. P1a ~ P1b 의 `0x24` 채우기가 어느 길인지 정적으로 좁힘 (동적 3 으로도 닫힘).
- ③ **우선순위 계산 본체**: `KiComputeNewPriorityNonStacking` · `KiComputeThreadPriorityNonStacking` · `KiUpdateThreadPriority` — PE_emulator 가 우선순위를 넣을 때만.
- ④ **프로세스 `QuantumReset = 2` 의 뿌리**: `PspComputeQuantum` · `PspForegroundQuantum` 표 · `PsPrioritySeparation` (`.asm` 없음, 짐 지도 3.12) — 에뮬레이터가 "프로세스마다 다른 첫 퀀텀" 을 흉내 낼 때만.
- ⑤ `KiQuantumEnd` 의 `R15B` 다른 갈래 (`+0x3a0` · `+0x8b3` · `+0x97e` · `+0xc72`) 와 `KiIdleLoop +0xaf` 에서 올 때의 길 — 작음.
- 기다림·깨움 (`KiCommitThreadWait`, `WaitStatus`) 과 타이머 만료 (`KiCheckForTimerExpiration` · `KiTimerExpiration`) 는 이미 **Q141c1 · Q141c2**.

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구: `run_ghidra.sh map` 은 호출만 따라가서 "표시 → 소프트웨어 인터럽트 → 인터럽트 끝 처리" 로 이어지는 길 (`KiUpdateRunTime` → `KiDispatchInterrupt`) 은 지도에 안 나온다. 이번엔 결과서가 이음새 함수를 이름으로 뽑아 이었다 (지도 설명의 "호출이 아닌 길은 지도에 없다" 그대로).
- 도구: `KiDpcInterrupt` 본문 뒤쪽은 Ghidra 조각 `FUN_1406b5753` (rva `0x6b5753` = `KiDpcInterrupt +0x273`) 에 있다. `_calls.tsv` 는 `frag:FUN_1406b5753+0x127` 로 적는다. 함수 기준 오프셋은 `+0x273` 을 더한 값 (`+0x39a`).
