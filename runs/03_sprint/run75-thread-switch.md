# 2026-11-12 run75 — 스레드 넘기기 (Q141b1, PE_emulator 요청 25 ㄴ-1): `KiSwapThread +0xdc KiSearchForNewThread` 가 **`NextThread` → 우선순위 31→1 (같은 우선순위는 줄 맨 앞) → 0 → 다른 CPU 훔치기 → `IdleThread`** 순서로 고르고, `+0x243 KiSwapContext` → `+0x71 SwapContext` 가 바꾼다 (같은 프로세스면 CR3 그대로 `+0x365`). 준비 줄에는 **기본 맨 뒤, `Preempted` 면 맨 앞** (`KiDeferredReadySingleThread +0x1726`). `NtYieldExecution` = `KeYieldExecution(0)` — 준비 줄이 비면 `0x40000024`

> **정정 (run76, sast/72)**: 아래 "퀀텀 끝·선점은 `KiSwapContext` 를 직접 부르는 길 (`KiCheckForThreadDispatch +0x10e` · `KiHandleDeferredPreemption +0x297`) 일 것 `[추측]`" (1(b) 줄 · 정적 호출 표 아래 줄) 은 **선점 쪽만 맞다.** 이 둘은 퀀텀 사슬 밖 선점 길이고 (`WaitReason` `0x20` / `0x26`), **퀀텀 끝은 `KiQuantumEnd +0xbd6` 이 `KiSwapContext` 를 직접** 부른다 (`KiDispatchInterrupt +0x1c3` → `KiQuantumEnd`). 그리고 `KeYieldExecution +0x1fc` 의 `WaitReason 0x21` 은 PDB 열거형으로 `WrYieldExecution` 확인 (`[추측]` → `[정적]`). 자세히는 `runs/03_sprint/run76-thread-quantum.md`

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 진짜 Windows 는 **우선순위로** 다음 스레드를 고른다. 지금 우리 nt 엔진의 "목록에서 다음 실행 가능한 것, 우선순위 없음" 은 **틀림** `[정적]`.
  - 고르는 순서: CPU 에 미리 정해 둔 `NextThread` → 준비 줄에서 우선순위 31 → 1 (같은 우선순위는 줄 맨 앞부터) → 우선순위 0 → (CPU 가 여럿이면) 다른 CPU 에서 훔치기 → 그 CPU 의 `IdleThread`.
  - 줄에 다시 세울 땐 **기본 맨 뒤**. "선점당했고 퀀텀이 아직 남은" 스레드만 **맨 앞**.
- run44 사진의 test.exe 메인 스레드 값 (`[관찰]`, 새 실행 없음 — 아래 "사진과 맞춰 본 것"):
  - 우선순위: `BasePriority 8`, `Priority` 8 (K3 ~ P1a) → 9 (P1b ~ P2). `QuantumReset 2`.
  - CPU 는 **2개** (KUSD `ActiveProcessorCount 2`, PEB `NumberOfProcessors 2`).
  - 초기화 동안 이 스레드가 CPU 를 받은 횟수 (`ContextSwitches`) 는 P0 1 → P2 `0x31` (49번). 넘기기는 초기화 중에도 계속 일어난다.
- 질문마다 "지금 우리" (결과서 sast/71 그대로):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1(a) 부르는 순서·고르는 줄 | PRCB 락을 잡고 `+0xdc` (ret `+0xe1`) `KiSearchForNewThread` 를 한 번 부른다. `KiSwapThread` 자신은 `NextThread` 를 **안 본다** — 보는 곳은 `KiSearchForNewThread +0x20` | "목록에서 다음 실행 가능" → **모자라거나 틀림** (`NextThread`·우선순위·Idle 이 없음) | `KiSwapThread +0xdc`, `KiSearchForNewThread +0x20` · `+0x2d` · `+0x110` |
| 1(b) 실제로 바꾸는 줄 | `+0x243` (ret `+0x248`) `KiSwapContext(지금, 새, WaitIrql)`. 새 == 지금이면 `+0x21f/+0x222` 에서 바꾸기를 건너뛴다 | 가정 없음. "같은 스레드면 안 바꿈" 은 흉내 낼 만함. 이름 주의: 진짜 `KiSwapThread` 를 부르는 곳은 기다림·끝내기 쪽 4곳 (`KiCommitThreadWait +0x220` 등). 퀀텀 끝·선점은 `KiSwapContext` 를 직접 부르는 다른 길일 수 있다 `[추측]` → Q141b2 | `KiSwapThread +0x21f` · `+0x222` · `+0x239` · `+0x243`, `_refs/KiSwapThread.tsv` |
| 1(c) 돌아온 뒤 | `+0x25c` `WaitStatus (+0xc8)` 를 읽어 `+0x29a` RAX → `+0x2ac RET`. `KiSwapContext` 반환 AL ≠ 0 (APC 대기) 이면 IRQL 1 로 낮춰 `+0x2ee KiDeliverApc`. 셋째 인자가 있으면 `AbWaitObject (+0x3d0)` 을 적어 줌 | 가정 없음 — `WaitStatus` 를 돌려주면 됨 (값의 뜻은 Q141c) | `KiSwapThread +0x25c` · `+0x29a` · `+0x2ac` · `+0x2ee` · `+0x2bd/+0x2c4` |
| 2(a) 어디서 고르나 | ① `NextThread` (Idle 아니면) ② 이 CPU 줄 + 공유 줄을 **한꺼번에** 우선순위로 ③ 다른 CPU 훔치기 ④ `IdleThread` | "이 CPU → 공유 → 훔치기" 라면 **틀림**. CPU 하나면 공유·훔치기는 건너뛰어도 됨 `[추측: 에뮬레이터 범위]` | `KiSearchForNewThread +0x20` · `+0x69` · `+0x7f` · `+0xdf` |
| 2(b) 높은 우선순위부터 | 예. 받을 수 있는 우선순위 마스크에서 `BSR` (가장 높은 켜진 비트). 단계 ① 1~31 → ② 그룹 스케줄링 줄 → ③ 0 | **틀림** (우선순위 없음) | `KiSearchForNewThreadsForRescheduleContext +0x125` · `+0x131` · `+0x3bc` · `+0x3c8` |
| 2(c) 같은 우선순위면 맨 앞 | 예 — `Flink` (줄 맨 앞) 부터. 첫 후보를 어느 CPU 도 못 받으면 다음 `Flink` | **맞음** (FIFO. 같은 우선순위 안에서만) | `+0x2d4` · `+0x2db` · `+0x2e9` |
| 2(d) 없으면 Idle | 예 — 이 CPU 칸의 기본값을 `IdleThread (+0x18)` 로 깔아 두고, 못 찾으면 그 값이 `NextThread` 로 들어간다 | 없다면 **추가** | `KiSearchForNewThread +0x185` · `+0x19c`, `KiCommitRescheduleContextEntry +0x149` |
| 3 같은 우선순위끼리 차례 | 줄에 꿰는 곳 = `KiDeferredReadySingleThread`. **기본 맨 뒤** (`+0x1730`, 머리 `Blink` 뒤), **`Preempted ≠ 0` 이고 퀀텀이 남았으면 맨 앞** (`+0x1824`, 머리 `Flink` 앞) | 지금 없음 `[추측]` → **추가** | `KiDeferredReadySingleThread +0x144f` · `+0x1453` · `+0x1459` · `+0x1726` · `+0x172a` |
| 4(a) 실제 몸 | `KeYieldExecution(0)` (PDB 이름. Ghidra 이름 `Ordinal_6`). 꼬리 `JMP` 가 아니라 보통 `CALL` | 가정 없음 | `NtYieldExecution +0x6` (ret `+0xb`) |
| 4(b) 같은 우선순위만? | **아니다** — `FullYield` 라 우선순위 1 이상 아무 준비 스레드 (자기보다 낮아도). 그중 가장 높은 것의 맨 앞. 지금 스레드는 새 퀀텀을 받고 준비 줄로 (`Preempted` 0 이면 맨 뒤) | 가정 없음 | `Ordinal_6 +0x2f0` · `+0x1b4` · `+0x211`, `KiStartReadyQueueEnumeratorForRescheduleContext +0xda` · `+0x251 ~ +0x25e` |
| 4(c) `0x40000024` | 넘길 게 없을 때. ① IRQL 올리기 전 두 비트맵이 다 0 (`+0x69`) ② `+0x395` — 올린 뒤 다시 0 (`+0x270`) / 찾았는데 `NextThread` 가 그대로 0 (`+0x157`) / VP 백킹 갈래 (`+0x3c5`) | 가정 없음 | `Ordinal_6 +0x53 ~ +0x69`, `+0x395` |
| 4(d) `0` | 실제로 넘겼다가 다시 이 스레드 차례가 와서 `+0x21f KiSwapContext` 가 돌아온 뒤 (`+0x22c`) | 가정 없음 | `Ordinal_6 +0x21f` · `+0x22c` |
| 5 `KiSwapContext` | 비휘발 레지스터·XMM6~15 저장 → `+0x71 SwapContext`: 새 스레드 `Running` 확인·켜기, FPU 상태 저장, **`+0xf3` 지금 스택 자리 저장 · `+0xf7` 새 스택으로**, **`+0x35e/+0x365` 같은 프로세스면 CR3 그대로** (다르면 `+0x3e0 MOV CR3`), 지금 스레드 `Running = 0`, 새 TEB → GS, `ContextSwitches +1`, APC 대기 표시 반환 | CR3·FPU·MSR 은 흉내 대상이 아님 (유저 공간 에뮬레이터). 흉내 낼 것: 레지스터·스택 바꾸기, 프로세스가 다를 때만 주소 공간, `Running`, `ContextSwitches`, APC 표시 | `KiSwapContext +0x71`, `SwapContext +0x18` · `+0xf3` · `+0xf7` · `+0x35e` · `+0x365` · `+0x3e0` · `+0x4ad` · `+0x5bb` |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/71 끝을 그대로 옮김)

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

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - 규칙 8 의 "한 개" 는 **CPU 마다** 하나다 (`PRCB` 는 CPU 마다 있고 칸은 `PRCB+0x18`). test.exe 가 돈 VM 은 CPU 2개 `[관찰]` run44.
  - 규칙 5 에 새 스레드 **TEB 를 GS 로** 바꾸는 줄도 있다: `SwapContext +0x582` `[RSI+0xf0]` (`Teb`) → `+0x5a8/+0x5ad WRMSR 0xc0000102` (`KERNEL_GS_BASE`) `[정적]`. 유저 에뮬레이터라면 "스레드마다 GS = 그 스레드 TEB" 에 해당.
  - **처음 도는 스레드**는 `SwapContext` 끝 `RET` (`+0x5ee`) 가 `KiSwapContext +0x76` 이 아니라 `KiStartUserThread` 로 간다 (run15 `[정적]`, 2.3). run44 사진의 메인 스레드 `ContextSwitches` 가 P0 (`PspUserThreadStartup` 입구) 에서 1 인 것과 맞는다.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치
- **공개 PDB 의 `_KPRCB` 는 `+0x6d0` 에서 잘려 있다** (`pdb_struct.py` sizeof `0x700`). 그래서 `PRCB+0x83d8` · `+0x83dc` · `+0x8440` · `+0x8674` · `+0x8e48` · `+0x8e58` · `+0x9418` · `+0x9420` · `+0x2d08` · `+0x37e8` 같은 칸은 **이름이 없다**. 오프셋·쓰임은 `[정적]`, 붙인 이름 (`ReadySummary`·`DispatcherReadyListHead` 등) 은 `[추측: 이름]` (결과서 — 같은 모양의 공개 구조체 `_KSHARED_READY_QUEUE` `ReadySummary +0x8`·`ReadyListHead +0x40` 에 맞춤)
  - 이름 있는 칸 (`_KPRCB`): `CurrentThread +0x8` · `NextThread +0x10` · `IdleThread +0x18` · `IdleState +0x23` · `Number +0x24` · `PrcbLock +0x30`
  - `_KTHREAD`: `QuantumTarget +0x20` · `ThreadLock +0x40` · `CycleTime +0x48` · `CurrentRunTime +0x50` · `ExpectedRunTime +0x54` · `KernelStack +0x58` · `Running +0x71` · `ReadyTime +0x84` · `ApcState.Process +0xb8` · `Priority +0xc3` · `WaitStatus +0xc8` · `WaitListEntry +0xd8` (준비 줄 고리도 이 칸) · `Teb +0xf0` · `ContextSwitches +0x154` · `State +0x184` · `WaitIrql +0x186` · `Preempted +0x235` · `AdjustReason +0x236` · `WaitReason +0x283` · `AbWaitObject +0x3d0`
  - `State` 값: 1 Ready · 2 Running · 3 Standby · 5 Waiting · 7 DeferredReady (코드에서 쓰는 숫자만)

## 목적

- PE_emulator 요청 25 ㄴ-1 (Q141b1): nt 엔진의 "다음 스레드로 넘기기" 를 진짜 커널 본문에 맞추기 위해 — `KiSwapThread` 의 부르는 순서, 다음 스레드를 고르는 규칙 (`KiSearchForNewThread`), 같은 우선순위끼리 차례 (줄 앞·뒤), 고를 게 없을 때 (Idle), `NtYieldExecution` 의 `0x40000024`, `KiSwapContext` 가 하는 일.
- 퀀텀 (시계 → `KiQuantumEnd`) 은 Q141b2, 기다림·깨움은 Q141c — 이번엔 안 본다.

## 방법

- 정적: `_workspace/sast/71_thread-switch.md` — ntoskrnl
  - 새 지도: `run_ghidra.sh map ntoskrnl.exe KiSwapThread` → `ms/disassemble/ntoskrnl/_map/KiSwapThread_{edges,nodes}.tsv` (2,938함수)
  - 새로 뽑음 21개 (`NtYieldExecution` 만 depth 1, 나머지 depth 0):
    - 요청 대상: `KiSwapThread` (1637B) · `KiSearchForNewThread` (675B) · `KiSwapContext` (214B) · `KiInsertDeferredReadyList` (90B) · `KiDeferredReadyThread` (166B) · `KiDeferredReadySingleThread` (9230B — 줄 앞/뒤 줄만) · `NtYieldExecution` (16B) → `KeYieldExecution` (1290B)
    - 범위를 넓힌 것 (고르는 일이 두 단계 아래라서, 이름으로): `KiSearchForNewThreadsForRescheduleContext` (4580B) · `KiCommitRescheduleContext` · `KiCommitRescheduleContextEntry` · `KiStartRescheduleContext` · `KiSearchForNewThreadsWithinRestOfSystem` · `KiStartReadyQueueEnumeratorForRescheduleContext` · `KiStartReadyListEnumerator` · `KiAdvanceReadyListEnumerator` · `KiQueueReadyThread` · `KiCanLocalReadyThreadBeScheduledToRescheduleContextEntry` · `KiReadyDeferredReadyList` · `KiUpdatePriorityMatrixForRunningTransition` + 딸려 나온 `KiFindRescheduleContextEntryForPrcb` (48B)
    - 있던 것 읽기만: `SwapContext` (584B, 조각 `FUN_1406b686f` 가 `+0x32f` 부터)
  - 부르는 쪽: `run_ghidra.sh refs ntoskrnl.exe KiSwapThread` → `_refs/KiSwapThread.tsv`
  - 이름 주의: PDB `KeYieldExecution` 을 Ghidra 가 `Ordinal_6` (내보내기 번호 6) 로 잡았다. 파일 이름은 `Ordinal_6.asm`/`.c`, 글에는 PDB 이름을 쓴다 (`pdb_aliases.py --where ntoskrnl.exe KeYieldExecution` → rva `0x2137a0`, `_index.tsv` 이름 `Ordinal_6`)
- 결과서: `_workspace/sast/71_thread-switch_result.md` (**분석성공**. 질문 3 의 "선점 → 맨 앞" 은 스택 칸 하나가 다른 갈래와 겹쳐 부분적으로만 닫음 — 아래 "아직 확인 필요")
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KiSwapThread +0xdc` · `+0x243` · `+0x25c` · `+0x29a`, `Ordinal_6.asm` (= `KeYieldExecution`) `+0x69` · `+0x21f` · `+0x22c`, `KiDeferredReadySingleThread +0x144f` · `+0x1453` · `+0x1459` · `+0x1726` · `+0x172a`, `KiSwapContext +0x71`, `SwapContext +0x35e` · `+0x3e0` (조각 `FUN_1406b686f`)
- flow-writer 도 `.asm` 으로 다시 봤다: `KiSwapThread +0x14` · `+0x45 ~ +0x5a` · `+0x179` · `+0x17d` · `+0x181 ~ +0x190` · `+0x21f` · `+0x222` · `+0x239` · `+0x248` · `+0x24b` · `+0x263` · `+0x2bd` · `+0x2c4` · `+0x2ee` · `+0x305 ~ +0x317` · `+0x4f3` · `+0x516`, `KiSearchForNewThread +0x20` · `+0x2a` · `+0x2d` · `+0x33` · `+0x58` · `+0x5b` · `+0x69` · `+0x7f` · `+0xa7` · `+0xb4` · `+0xc7` · `+0xdf` · `+0x109` · `+0x110` · `+0x14b` · `+0x151` · `+0x185` · `+0x18f` · `+0x19c` · `+0x287`, `KiSearchForNewThreadsForRescheduleContext +0x125` · `+0x131` · `+0x2d4` · `+0x2db` · `+0x2e9` · `+0x3c8` · `+0x622` · `+0x783` · `+0x992` · `+0x9cd ~ +0x9e2` · `+0xc61` · `+0xd92 ~ +0xdab` · `+0xe9d` · `+0xed8` · `+0xeef`, `KiStartReadyQueueEnumeratorForRescheduleContext +0xda` · `+0xde` · `+0xf6 ~ +0xfc` · `+0x1cf ~ +0x1dc` · `+0x251`, `KiCommitRescheduleContextEntry +0x149` · `+0x179` · `+0x208`, `KiSearchForNewThreadsWithinRestOfSystem +0x12 ~ +0x41`, `KiDeferredReadySingleThread +0x2b` · `+0x63` · `+0xd5` · `+0x260` · `+0x26d` · `+0x1460` · `+0x1711` · `+0x171f` · `+0x1730` · `+0x1824` · `+0x183f` · `+0x1847` · `+0x1857` · `+0x1de0` · `+0x1de6`, `KiQueueReadyThread +0x3a` · `+0x1ec` · `+0x1f9`, `KiInsertDeferredReadyList +0x33 ~ +0x47`, `KiUpdatePriorityMatrixForRunningTransition +0x3` · `+0x11`, `Ordinal_6 +0x25` · `+0x4a ~ +0x6e` · `+0x79` · `+0x82` · `+0xf4` · `+0x150` · `+0x157` · `+0x1b4` · `+0x1e5` · `+0x1fc` · `+0x207` · `+0x216` · `+0x270` · `+0x2f0` · `+0x2f6` · `+0x395` · `+0x39a` · `+0x3b8 ~ +0x3c7`, `KiSwapContext +0x0 ~ +0x5f` · `+0xce` · `+0xd5`, `SwapContext +0xe` · `+0x12` · `+0x18` · `+0x3f` · `+0x4b` · `+0xf3` · `+0xf7` · `+0x357` · `+0x365` · `+0x387` · `+0x3cd` · `+0x3d9` · `+0x42a` · `+0x4ad` · `+0x504` · `+0x582` · `+0x5a8` · `+0x5ad` · `+0x5bb` · `+0x5c1 ~ +0x5ee`
- 값 대조 (메인 스레드, 새 실행 없음): run44 덤프 읽기 로그 `ms/logs/04_baggage-read_<K2 ~ P2>.txt` 구획 `02b KTHREAD` (`dt nt!_KTHREAD ffff828598445080`, 680 ~ 840줄) 와 CPU 수 (`_P1a.txt:1769` PEB `NumberOfProcessors`, `_P1a.txt:2981` KUSD `ActiveProcessorCount`) — flow-writer

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!KiSwapThread` | `+0x516` | `+0x51b` | `nt!KiProcessThreadWaitList` | `PRCB+0x2d08 ≠ 0` 일 때만 먼저 (`+0x14` · `+0x2a`), 끝나면 `+0x30` 으로 | [정적] |
| `nt!KiSwapThread` | `+0x35` | `+0x3a` | `nt!KiAbProcessPreContextSwitch` | 이름만 (run52 caller 목록) | [정적] |
| `nt!KiSwapThread` | `+0x40` | `+0x45` | `nt!KiCaptureTotalCyclesCurrentThread` | 이름만. 바로 뒤 `+0x45 ~ +0x5a` 실행 시간 칸 | [정적] |
| `nt!KiSwapThread` | `+0xc2` | `+0xc7` | `nt!KiUpdateGroupSchedulingRank` | 이름만 | [정적] |
| `nt!KiSwapThread` | **`+0xdc`** | **`+0xe1`** | **`nt!KiSearchForNewThread`** | `(Prcb, 0, &락)` → RBP = 다음 스레드 | [정적] |
| `nt!KiSwapThread` | `+0xe4` | `+0xe9` | `nt!Feature_HeteroRescheduleFixes__private_IsEnabledNoReportingNoInline` | 기능 표시 확인 (헤테로 갈래) — 줄기 영향 없음 | [정적] |
| `nt!KiSwapThread` | **`+0x243`** | **`+0x248`** | **`nt!KiSwapContext`** | `(지금, 새, WaitIrql)`. 새 == 지금이면 안 탐 (`+0x222`) | [정적] |
| `nt!KiSwapThread` | `+0x4f3` | `+0x4f8` | `nt!KiSwapContext` | 곁가지 — 지금 스레드가 VP 백킹 (`[+0x78] & 0x400000`) 이고 `State == 5` 일 때 | [정적] |
| `nt!KiSwapThread` | `+0x24e` | `+0x253` | `nt!KiAbProcessPostContextSwitch` | 넘기고 돌아온 뒤 | [정적] |
| `nt!KiSwapThread` | `+0x3d3` · `+0x3db` | `+0x3d8` · `+0x3e0` | `nt!KiResumeThreadCycleAccumulation` · `nt!KiAbProcessPostContextSwitch` | 새 == 지금 (안 바꿈) 갈래 | [정적] |
| `nt!KiSwapThread` | `+0x372` | `+0x377` | `nt!KiCancelTimer` | `MiscFlags.TimerActive` (`+0x263 BTR`) 가 켜져 있었으면 | [정적] |
| `nt!KiSwapThread` | `+0x2ee` | `+0x2f3` | `nt!KiDeliverApc` | `KiSwapContext` 반환 AL ≠ 0 일 때, IRQL 1 로 | [정적] |
| `nt!KiSearchForNewThread` | `+0x53` | `+0x58` | `nt!KiStartRescheduleContext` | 문맥 버퍼 `PRCB+0x37e8` 비우고 (`+0x45 memset`) | [정적] |
| `nt!KiSearchForNewThread` | `+0x16c` | `+0x171` | `nt!KiFindRescheduleContextEntryForPrcb` | 이 CPU 칸 찾기 → `+0x185` · `+0x19c` Idle 기본값 (`EDX = 0` 일 때) | [정적] |
| `nt!KiSearchForNewThread` | **`+0x69`** | **`+0x6e`** | **`nt!KiSearchForNewThreadsForRescheduleContext`** | 준비 줄 뒤지기 (우선순위·맨 앞) | [정적] |
| `nt!KiSearchForNewThread` | `+0x7f` | `+0x84` | `nt!KiCommitRescheduleContext` | → `+0x56/+0x5b KiCommitRescheduleContextEntry` (`NextThread` 쓰기) | [정적] |
| `nt!KiSearchForNewThread` | `+0xdf` | `+0xe4` | `nt!KiSearchForNewThreadsWithinRestOfSystem` | 고른 게 Idle 일 때 다른 CPU 훔치기 → L0 `+0x1b/+0x20` · L1 `+0x41/+0x46` | [정적] |
| `nt!KiSearchForNewThread` | `+0x12c` | `+0x131` | `nt!KiReparkIdlingProcessorIfNecessary` | 훔치기도 못 했을 때 | [정적] |
| `nt!KiSearchForNewThread` | `+0x287` | `+0x28c` | `nt!KiReadyDeferredReadyList` | 밀려난 스레드를 줄로 되돌림 → `+0x46/+0x4b KiDeferredReadySingleThread` | [정적] |
| `nt!KiSearchForNewThreadsForRescheduleContext` | `+0xd8` | `+0xdd` | `nt!KiStartReadyQueueEnumeratorForRescheduleContext` | 칸마다 받을 우선순위 마스크 (yield 갈래 포함) | [정적] |
| `nt!KiSearchForNewThreadsForRescheduleContext` | `+0x69c` | `+0x6a1` | `nt!KiAdvanceReadyQueueEnumeratorToNextList` | 한 우선순위 줄을 다 봤을 때 다음 줄로 | [정적] |
| `nt!NtYieldExecution` | `+0x6` | `+0xb` | `nt!KeYieldExecution` (Ghidra `Ordinal_6`) | `ECX = 0`. 꼬리 JMP 아님 | [정적] |
| `nt!KeYieldExecution` | `+0x30b` | `+0x310` | `nt!KiSearchForNewThreadsForRescheduleContext` | `NextThread == 0` 일 때만 (`+0xf4` → `+0x2a0`), 칸 플래그 `FullYield` (`+0x2f0`) | [정적] |
| `nt!KeYieldExecution` | `+0x31d` | `+0x322` | `nt!KiCommitRescheduleContext` | 위와 같은 갈래 | [정적] |
| `nt!KeYieldExecution` | `+0x196` | `+0x19b` | `nt!KiQueryQuantumReset` | → `+0x1b4` 지금 스레드 `QuantumTarget` 새로 | [정적] |
| `nt!KeYieldExecution` | `+0x1f4` | `+0x1f9` | `nt!KiUpdatePriorityMatrixForRunningTransition` | `+0x3` `CurrentThread = 새` · `+0x11` 새 `State = 2` | [정적] |
| `nt!KeYieldExecution` | **`+0x211`** | **`+0x216`** | **`nt!KiQueueReadyThread`** | 지금 스레드를 준비 줄로 → `+0x1f9/+0x1fe KiDeferredReadySingleThread` | [정적] |
| `nt!KeYieldExecution` | **`+0x21f`** | **`+0x224`** | **`nt!KiSwapContext`** | `(지금, 새, R8B = 1)` | [정적] |
| `nt!KeYieldExecution` | `+0x3b8` | `+0x3bd` | `nt!KiVpBackingThreadYieldExecution` | VP 백킹 스레드 갈래 (AL = 0 → `+0x3c5` → `+0x395`) | [정적] |
| `nt!KiDeferredReadyThread` | `+0x23` · `+0x5d` | `+0x28` · `+0x62` | `nt!KiDeferredReadySingleThread` | 껍데기 — 본 스레드 + 딸린 밀린 목록 | [정적] |
| `nt!KiDeferredReadySingleThread` | `+0x1de6` | `+0x1deb` | `nt!KiComputeNewPriority` | 퀀텀이 끝난 갈래 (`+0x1453 JNC +0x1de0`) | [정적] |
| `nt!KiSwapContext` | **`+0x71`** | **`+0x76`** | **`nt!SwapContext`** | 몸 | [정적] |
| `nt!SwapContext` | 조각 `+0xaa` (= `+0x3d9`) | 조각 `+0xaf` | `nt!HvlSwitchVirtualAddressSpace` | 프로세스가 다르고 `HvlEnlightenments` 비트 0 일 때 (아니면 `+0x3e0 MOV CR3`) | [정적] |
| `nt!SwapContext` | 조각 `+0x2af` (= `+0x5de`) | 조각 `+0x2b4` | `nt!HalRequestSoftwareInterrupt` | `ECX = 1` — APC 를 지금 처리 못 할 때 요청만 | [정적] |

- `KiSwapThread` 를 부르는 곳 (`_refs/KiSwapThread.tsv`): `KiCommitThreadWait +0x220` (기다림 → Q141c) · `KiInSwapSingleProcess +0x120` · `KeTerminateThread +0x24f` (ret `+0x254`) · `KiExecuteDpcDelegate +0x8b`, 그리고 데이터 참조 2곳 (`0xe786c`, 언윈드 정보).
- `KiSwapContext` 를 직접 부르는 곳이 지도 간선에 더 있다: `KiCheckForThreadDispatch +0x10e` · `KiHandleDeferredPreemption +0x297` (결과서). 퀀텀 끝·선점 길일 것 `[추측]` → Q141b2.
- 락·ETW·통계·헤테로·VP 백킹 helper (`KiAcquire/ReleasePrcbLocksForIsolationUnit` · `EtwpLogContextSwapEvent` 등) 는 줄기 밖이라 이름만.

### 다음 스레드 고르기 (질문 1(a) · 2)

- **`NextThread` 를 먼저 본다 — 단 `KiSearchForNewThread` 안에서** `[정적]`:
  - `KiSearchForNewThread +0x20 MOV RBX,[RCX+0x10]` → `+0x2d JNZ +0x110` → `+0x110 CMP RBX,[RCX+0x18]` (`IdleThread` 와 같나). 있고 Idle 이 아니면 `+0x11f KiDowngradeIsolationUnitLockHandle` 만 하고 그 값을 돌려준다.
  - `KiSwapThread` 자신이 `[R14+0x10]` 을 만지는 줄은 `+0x179` (0 으로 **씀**) 와 `+0x360` (헤테로 갈래에서 **읽음**) 뿐이다.
- 없거나 Idle 이면 이 CPU 의 "다시 고르기 문맥" 을 만들고 한 번에 찾는다:
  - `+0x33` 문맥 버퍼 `PRCB+0x37e8` → `+0x45 memset(0xf0)` → `+0x53 KiStartRescheduleContext`.
  - `EDX = 0` (`KiSwapThread` 가 부른 꼴) 이면 `+0x5b` 가 `+0x15d` 로 가서 이 CPU 칸 (`_KI_RESCHEDULE_CONTEXT_ENTRY`) 의 두 스레드 칸을 **`IdleThread` 로 깔아 둔다** (`+0x185` · `+0x19c`), 플래그는 `+0x18f` `(flags & 0xc5) | 5`.
  - **`+0x69 KiSearchForNewThreadsForRescheduleContext`** 가 준비 줄을 뒤져 칸에 적고, **`+0x7f KiCommitRescheduleContext`** → `KiCommitRescheduleContextEntry +0x149 MOV [RBX+0x10],RDI` 가 **칸의 스레드를 `PRCB.NextThread` 에** 쓰고 `+0x179` 그 스레드 `State = 3` (Standby).
  - 다시 `+0xa7` 에서 `NextThread` 를 읽는다. Idle 이 아니면 그대로 돌려준다 (`+0x151` → `+0x15b`).
- **준비 줄은 "이 CPU 것" 과 "공유 것" 을 한꺼번에 본다** (순서가 "이 CPU → 공유" 가 아님):
  - 이 CPU: `PRCB+0x83d8` (32비트 비트맵) + `PRCB+0x8440 + 우선순위 × 0x10` (줄 머리) `[추측: 이름 ReadySummary / DispatcherReadyListHead]`.
  - 공유: `PRCB+0x8e48` 이 가리키는 `_KSHARED_READY_QUEUE` (`+0x8` 비트맵, `+0x40 + 우선순위 × 0x10` 줄 머리).
  - 같은 우선순위에 둘 다 있으면 **번갈아** 고른다: `+0xc61 BT [R10+0x83dc],EBX` (1 = 이 CPU 차례). 이 CPU 에서 꺼내면 비트를 지우고 (`+0x992 BTR`), 공유에서 꺼내면 세운다 (`+0x783 BTS`). 뜻은 "공평하게 번갈아" `[추측]`.
- **가장 높은 우선순위부터** (질문 2(b)): 칸마다 받을 수 있는 마스크를 만들고 (`KiStartReadyQueueEnumeratorForRescheduleContext +0x1cf ~ +0x1dc` — 두 비트맵과 AND), `+0x125 AND EAX,0xfffffffe` · **`+0x131 BSR R11D,EAX`** (1~31 중 가장 높은 것).
  - 단계 셋 (`+0xf6 MOV DIL,1` … `+0x2b5 INC DIL` · `+0x2b8 CMP DIL,4`): ① 우선순위 1~31 → ② 그룹 스케줄링 줄 (SCB, 전역 `KiGroupSchedulingEnabled` 를 `+0x622` 가 읽음) → ③ **우선순위 0** (`+0x3bc AND EAX,1` · `+0x3c8 BSR`). 0 은 맨 마지막.
- **같은 우선순위 줄에서는 맨 앞부터** (질문 2(c)): `+0x2d4 MOV RDI,[RAX]` (`Flink`) → `+0x2db` 머리로 돌아왔으면 `+0x69c` 다음 줄 → `+0x2e9 LEA RSI,[RDI-0xd8]` (고리 → `_KTHREAD`).
  - 첫 후보를 어느 칸도 못 받으면 다음 `Flink` 로. 받는지 보는 검사: 우선순위 비교 `+0xd92 ~ +0xdb3` (더 높으면 받음 `+0xd9f JG`), 그룹 스케줄링 (`+0x647 KiEvaluateGroupSchedulingPreemption` · `+0x955 KiIsThreadConstrainedBySchedulingGroup`), 다른 CPU 칸은 `+0x1081 KiCanLocalReadyThreadBeScheduledToRescheduleContextEntry`.
  - 뽑으면 줄에서 빼고, 줄이 비면 비트맵 비트를 끈다 (`+0x9cd ~ +0x9db`, 공유 쪽 `+0x8f9 ~ +0x918`). `+0x9e2` `PRCB+0x8674` −1 (이 CPU 준비 스레드 수 `[추측: 이름]`). 칸에 적기 `+0xed8` · `+0xeef`.
- **그래도 Idle 이면 다른 CPU 에서 훔치기**: `+0xd4` 락 풀고 `+0xdf KiSearchForNewThreadsWithinRestOfSystem` (`PRCB+0x9418 ≠ 0` → L0, 못 찾고 `+0x9420 ≠ 0` → L1). 본체 (`…OnTarget` 등) 는 안 읽음. 못 찾으면 `+0x12c KiReparkIdlingProcessorIfNecessary`, 락 다시 (`+0x146`), `+0x14b` `NextThread` (보통 Idle) 를 돌려줌.
  - 건너뛰는 조건: `+0xc7 TEST [RDI+0x23],0x10` (`IdleState` 비트 4). `+0xb4` 비트 0 이 서 있는데 고를 게 없으면 `+0x109 MOV ECX,0x21` → `INT 0x29` (fastfail) — 뜻은 안 풀었다.
- 고른 뒤 `KiSwapThread` 가 하는 바꿔치기: `+0x179` `NextThread = 0` · `+0x17d` **`CurrentThread = 새`** · `+0x181 ~ +0x190` 새 스레드 `State` 가 1 (Ready) 이면 `ReadyTime (+0x84)` 에 기다린 몫을 더하고 (`+0x305 ~ +0x317`) `State = 2`. 그 뒤 PRCB 락들을 푼다.

### 같은 우선순위끼리 차례 — 넣는 쪽 (질문 3)

| 함수 | 크기 | 하는 일 |
|---|---|---|
| **`KiDeferredReadySingleThread`** | 9230B | 스레드를 **준비 줄에 꿰는 곳** (이 CPU 줄 또는 공유 줄) |
| `KiDeferredReadyThread` | 166B | 껍데기. `+0x23` 에서 본 스레드, `+0x5d` 에서 딸린 밀린 목록을 하나씩 위 함수에 |
| `KiInsertDeferredReadyList` | 90B | **준비 줄이 아니다.** 밀린 스레드 단일 목록 맨 앞에 달고 `State = 7` (`+0x33`). 나중에 `KiReadyDeferredReadyList +0x46` 이 위 함수로 넣는다 |
| `KiQueueReadyThread` | 2974B | 양보 (와 퀀텀 끝 `[결과서]`) 에서 지금 스레드를 넣는 길. `+0x3a` `Preempted` 읽기 → `+0x1ec` 그대로 되돌려 두고 `+0x1f9` 위 함수 |

- **갈림 줄** (`KiDeferredReadySingleThread`) `[정적]`:
  - `+0x63 MOV [RSP+0x58],EDI` (`EDI = 0`) — 칸의 처음 값 0.
  - `+0xd5 JZ +0x144f` (`AdjustReason == 0`) → **`+0x144f CMP R13,[RBX+0x20]`** (`R13` = `CycleTime`, `+0x2b` 에서 읽음) · **`+0x1453 JNC +0x1de0`** — `CycleTime < QuantumTarget` (퀀텀이 남음) 일 때만 아래로.
  - **`+0x1459 MOVZX EAX,[RBX+0x235]`** (`Preempted`) → `+0x1460 MOV [RSP+0x58],EAX`.
  - 퀀텀이 끝났으면 `+0x1de0` 쪽 (`+0x1de6 KiComputeNewPriority` — 우선순위 다시 계산) 으로 가고 칸은 0 그대로.
  - **`+0x1726 CMP [RSP+0x58],EDI` · `+0x172a JNZ +0x1824`**:
    - 0 이면 `+0x1730 MOV RDX,[RAX+8]` (머리 `Blink`) 뒤에 → **맨 뒤** (`+0x173d ~ +0x1747`).
    - 0 이 아니면 `+0x1824 MOV RDX,[RAX]` (머리 `Flink`) 앞에 → **맨 앞** (`+0x1831 ~ +0x183c`), `+0x183f BTS [RSI+0x83dc]` (번갈아 칸에 "이 CPU 먼저").
    - 둘 다 이어서 `+0x1847 ~ +0x1857` `PRCB+0x83d8 |= 1 << 우선순위` (비트맵 켜기). 줄 머리 계산은 `+0x1711 ~ +0x171f` (`Prcb + (우선순위 + 0x844) × 0x10`, 고리 `Thread + 0xd8`).
  - 공유 줄도 같은 칸으로 갈린다: 앞 `+0x14c9 ~ +0x14dd`, 뒤 `+0x1bb1 ~ +0x1bbb`.
- `Preempted` 를 켜는 곳: `KiSearchForNewThreadsForRescheduleContext +0xe9d` · `+0x10db` · `+0x1114` · `+0x1143` · `+0x1172` (바로 뒤 `KiInsertDeferredReadyList`) — 스탠바이로 잡혀 있던 스레드를 더 높은 것이 밀어낼 때. `KiCommitRescheduleContextEntry +0x3da` · `+0x403` 도 쓴다. 끄는 곳: `KiDeferredReadySingleThread +0x26d MOV word [RBX+0x235],DI` — 퀀텀을 새로 받을 때 (`+0x260` `QuantumTarget` 와 함께) `Preempted`·`AdjustReason` 둘 다 0.
- → **답: 기본은 맨 뒤. "선점당했고 (`Preempted ≠ 0`) 퀀텀이 아직 남은" 스레드만 맨 앞.** 퀀텀 다 쓴 스레드·양보한 스레드·깨어난 스레드는 뒤 (요청서의 `[추측]` "선점 = 앞 / 퀀텀 끝·양보 = 뒤" 와 맞다).
- 한계 (결과서): 스택 칸 `[RSP+0x58]` 은 다른 갈래 (`+0x437` · `+0x46e` · `+0x13f7`, `MOV [RSP+0x58],R9D`) 에서 락 개수 같은 값으로도 쓰인다 (flow-writer `.asm` 확인). 그 갈래가 `+0x170c` 로 이어지는지는 안 따라갔다 → 동적 (아래).

### `NtYieldExecution` (질문 4)

- `NtYieldExecution` (16B): `+0x4 XOR ECX,ECX` → **`+0x6 CALL KeYieldExecution`** (ret `+0xb`) → `+0xb ADD RSP,0x28` → `+0xf RET`. 꼬리 JMP 가 아니라 **보통 CALL** (요청서 짐작 "꼬리 호출" 은 틀림). 인자 `Flags = 0`.
- `KeYieldExecution` 줄기 `[정적]`:
  1. `+0x25 TEST ECX,0xfffffffc` — 0~3 밖 비트면 `+0x2d` `0xc000000d`.
  2. **미리 보기** `+0x4a ~ +0x69`: `gs:[0x20]` (PRCB) 의 `+0x83d8` 가 0 **이고** `[[+0x8e48]+8]` (공유 비트맵) 도 0 이면 **`+0x69 MOV EBX,0x40000024`** → `+0x6e` 반환.
  3. `+0x79 ~ +0x82` IRQL 2 로. 같은 검사를 한 번 더 (`+0x9c ~ +0xac` → `+0x25f ~ +0x270`) — 둘 다 0 이면 `+0x270 JMP +0x395`.
  4. `+0xbf` `ThreadLock` 스핀 · `+0xef` PRCB 락.
  5. `+0xf4 CMP [RSI+0x10],R13` — `NextThread == 0` 이면 `+0x2a0` 찾아보기: `+0x2c1 KiStartRescheduleContext` → `+0x2d8 KiFindRescheduleContextEntryForPrcb` → **`+0x2f0 OR CL,0x10` (`FullYield`)** · `+0x2f6 OR [RAX+0x22],2` (`DoNotSetPreemptionBit`) → `+0x30b KiSearchForNewThreadsForRescheduleContext` → `+0x31d KiCommitRescheduleContext`. 다시 `NextThread` 를 읽고 (`+0x150`), 여전히 0 이면 `+0x157 JZ +0x380` → `+0x395`.
  6. 찾았으면: 지금 스레드 새 퀀텀 — `+0x196 KiQueryQuantumReset` · `+0x19b IMUL EAX,[KiCyclesPerClockQuantum]` · **`+0x1b4 MOV [RDI+0x20],RCX`** (`QuantumTarget`), `+0x1be KiComputeNewPriority`, `+0x1e5` **`NextThread = 0`**, `+0x1f4 KiUpdatePriorityMatrixForRunningTransition` (`CurrentThread = 새`, 새 `State = 2`).
  7. `+0x1fc` 지금 스레드 `WaitReason = 0x21` (`WrYieldExecution` — ~~`[추측]`~~ → run76 `[정적]`, PDB 열거형 sast/72) · `+0x207` `WaitIrql = 이전 IRQL`.
  8. **`+0x211 KiQueueReadyThread`** — 지금 스레드를 준비 줄에. 퀀텀을 방금 새로 받아 퀀텀 끝 갈래가 아니므로 `Preempted` 로 갈림 (0 이면 맨 뒤). 이 함수는 `Preempted` 를 따로 안 지운다 (`Ordinal_6.asm` 에 `+0x235` 쓰기 없음, 결과서).
  9. **`+0x21f KiSwapContext(지금, 새, 1)`** (`+0x216 MOV R8B,1`) → `+0x227 KiAbProcessPostContextSwitch` → **`+0x22c MOV EBX,R13D` (= 0)** → `+0x24c JMP +0x32` 반환.
- **무엇을 받나 (4(b))**: `FullYield` 면 `KiStartReadyQueueEnumeratorForRescheduleContext +0xda TEST R11B,0x10` → `+0xde JNZ +0x251` → `+0x251 ~ +0x25e` (`NEG CL; SBB; DEC`) 로 마스크가 **우선순위 1~31 전부** (지금 우선순위가 0 이면 0~31). 그중 가장 높은 것의 맨 앞 (2(b)(c)). 그러니 실제로는 "자기와 같거나 낮은 것 중 가장 높은 것" 까지 받는다.
  - 비교: 플래그 없음 → 지금보다 **높은** 것만 (`+0xed ~ +0xfc`, `+0xfc BTR` 로 같은 비트도 끔). `PartialYield (0x8)` 만 → 같은 것까지 (`+0xf6/+0xfa` 로 `BTR` 건너뜀).
  - `Flags` 비트 0 이 1 이면 `+0x4e5 CMP [RDI+0xc3],0x10` — 우선순위 16 미만 `PartialYield` (`+0x3c7`), 이상 `FullYield`. `NtYieldExecution` 은 0 이라 안 탄다.
- **`0x40000024` (4(c))**: 세 갈래 모두 "넘길 스레드가 없다":
  - `+0x69` — IRQL 올리기 전, 두 비트맵 다 0.
  - `+0x395` (`+0x39a JMP +0x22f`) — 들어오는 길 셋: `+0x270` (올린 뒤 다시 0) · `+0x157 JZ +0x380` (찾았는데 `NextThread` 0 → `+0x380 ~ +0x390` 락 풀기·`KiAbProcessPostContextSwitch`) · `+0x3c5 JMP +0x395` (VP 백킹 스레드에서 `+0x3b8 KiVpBackingThreadYieldExecution` 이 0 을 돌려줌. 0 이 아니면 `+0x3bf JNZ` 로 본 줄기 `+0xb2`).
  - `+0x22f` 에서 IRQL 을 원래대로 (`+0x244 ~ +0x248 MOV CR8`) 하고 반환.
- **`0` (4(d))**: `+0x21f KiSwapContext` 가 돌아온 뒤 `+0x22c`. 즉 실제로 넘겼다가 다시 이 스레드 차례가 와서 돌아왔을 때 (한참 뒤일 수 있다).

### 바꾸는 줄 — `KiSwapContext` → `SwapContext` (질문 5)

- **`KiSwapContext`** (214B, 인자 RCX = 지금, RDX = 새, R8 = 지금 스레드 `WaitIrql`):
  - `+0x0 SUB RSP,0x138`, `XMM6 ~ XMM15` (`+0xf ~ +0x3f`), `RBX RDI RSI R12 R13 R14 R15` (`+0x44 ~ +0x5b`) 를 **지금 스레드의 스택에** 저장 (비휘발만).
  - `+0x5f RBX = gs:[0x20]` (PRCB), RDI = 지금, RSI = 새, ECX = WaitIrql → **`+0x71 CALL SwapContext`** (ret `+0x76`).
  - 돌아오면 `+0x76 ~ +0xca` 복원, `+0xce ADD RSP,0x138`, `+0xd5 RET`. 반환 AL 은 `SwapContext` 것 그대로.
- **`SwapContext`** (584B) `[정적]` — 줄기만:

| 오프셋 | 하는 일 |
|---|---|
| `+0xe` · `+0x12` | 새 스레드 `Running (+0x71)` 이 아직 1 이면 (다른 CPU 가 막 내려놓는 중) `+0x65e` 에서 `PAUSE` 하며 기다림 |
| `+0x18` | 새 스레드 `Running = 1` |
| `+0x3f` · `+0x46` · `+0x4b` | `CLI`, `KiStartThreadCycleAccumulationContextSwap`, `PRCB+0x2d3c` +1 (전환 횟수 `[추측: 이름]`) |
| `+0x9a ~ +0xd7` | 지금 스레드 FPU/XSAVE 상태를 `StateSaveArea (+0x60)` 에 저장 |
| **`+0xf3`** | **지금 스레드 `KernelStack (+0x58)` = RSP** |
| **`+0xf7`** | **RSP = 새 스레드 `KernelStack`** (스택 바꾸기) |
| `+0x34a ~ +0x356` (조각) | `LFENCE`, `STI` |
| **`+0x357` · `+0x35e` · `+0x365`** | **`ApcState.Process (+0xb8)` 둘 비교 — 같은 프로세스면 `+0x42a` 로 건너뜀 (CR3 그대로)** |
| (다르면) `+0x36b ~ +0x380` · `+0x407 ~ +0x423` | 새 프로세스 `ActiveProcessors` 에 이 CPU 비트 켬 / 옛 프로세스에서 끔 |
| (다르면) `+0x387` · `+0x3cd ~ +0x3e0` | `DirectoryTableBase (+0x28)` → `HvlEnlightenments` 비트 0 이면 `+0x3d9 HvlSwitchVirtualAddressSpace`, 아니면 **`+0x3e0 MOV CR3,RCX`** |
| `+0x42a ~ +0x44b` | 새 스레드 `InitialStack (+0x28)` 을 TSS `RSP0` 자리와 `PRCB.RspBase (+0x28)` 에 |
| `+0x4ad` | **지금 스레드 `Running = 0`** (스택을 바꾼 뒤) |
| `+0x4b1 ~ +0x4e8` | 새 스레드 FPU 상태 복원 |
| `+0x504 ~ +0x5ad` | (`[RSI+0x74]` 비트 10 이 꺼져 있을 때 — `MiscFlags.SystemThread` 로 보임 `[추측]`) FS base·세그먼트, **`+0x582` `Teb (+0xf0)` → `+0x5a8/+0x5ad WRMSR 0xc0000102`** (`KERNEL_GS_BASE`) |
| `+0x5bb` | **새 스레드 `ContextSwitches (+0x154)` +1** |
| `+0x5c1 ~ +0x5e6` | `KernelApcPending (+0xc1)` 이 1 이고 (스택 `[RSP+0x28]` 바이트 OR `SpecialApcDisable (+0x1e6)`) 가 0 이면 AL = 1 (APC 를 지금 처리), 아니면 `+0x5de HalRequestSoftwareInterrupt(1)` 하고 AL = 0 |
| `+0x5e9 ~ +0x5ee` | `ADD RSP,0x30; POP RBP; RET` — **새 스레드 스택에서** 일어난다. 예전에 잠든 자리 `KiSwapContext +0x76` 으로, 처음 도는 스레드면 `KiStartUserThread` 로 (run15) |

- `[RSP+0x28]` 바이트는 run15 가 본 "CL 인자" 와 같은 칸이다: 새 스레드는 `KiInitializeContextThread +0x26f` 가 넣은 1 (2.2, 4장 run15 줄). 결과서는 이 칸을 "WaitIrql" 로 적었다 — 새 스레드가 예전에 내려갈 때 저장한 자기 값으로 읽힌다 `[추측]`.

### 돌아온 뒤 (질문 1(c))

- `KiSwapContext` 가 돌아왔다 = 나중에 이 스레드 차례가 와서 다시 이 스택으로 들어왔다.
- `+0x24e KiAbProcessPostContextSwitch` → **`+0x25c MOV RDI,[R13+0xc8]`** (`WaitStatus`) → `+0x263 BTR [R13+0x74],9` (`TimerActive`; 켜져 있었으면 `+0x372 KiCancelTimer(&Timer, 1)`) → 셋째 인자 R12 ≠ 0 이면 `+0x2bd/+0x2c4` `AbWaitObject (+0x3d0)` 를 출력 칸에 → `+0x274` APC 표시 (SIL) 가 서 있으면 IRQL 1 로 `+0x2ee KiDeliverApc(0,0,0)` → `+0x289/+0x28e` IRQL 을 `WaitIrql` 로 → **`+0x29a MOV RAX,RDI`** → `+0x2ac RET`.
- `WaitStatus` 를 누가 어떤 값으로 쓰는지는 Q141c.

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
(부르는 곳: KiCommitThreadWait +0x220 · KiInSwapSingleProcess +0x120 · KeTerminateThread +0x24f · KiExecuteDpcDelegate +0x8b)
KiSwapThread (Thread, Prcb, 출력 칸)
 │  +0x14   PRCB+0x2d08 ≠ 0 이면 먼저 +0x51b KiProcessThreadWaitList
 ├─ (1) +0x3a   KiAbProcessPreContextSwitch
 ├─ (2) +0x45   KiCaptureTotalCyclesCurrentThread
 │  +0x45 ~ +0x5a  ExpectedRunTime = Expected/2 + CurrentRun/2 · CurrentRunTime = 0
 │  +0x6d ~ +0xa5  PRCB 락
 ├─ (3) +0xe1   KiSearchForNewThread(Prcb, 0, &락)                  → 다음 스레드
 │    │  +0x20 / +0x110  NextThread 가 있고 Idle 아니면 그대로
 │    │  +0x185 · +0x19c  이 CPU 칸 기본값 = IdleThread
 │    ├─ (1) +0x58   KiStartRescheduleContext
 │    ├─ (2) +0x6e   KiSearchForNewThreadsForRescheduleContext     +0x131 BSR (1~31) · +0x3c8 BSR (0) · +0x2d4 Flink 부터
 │    ├─ (3) +0x84   KiCommitRescheduleContext
 │    │       └─ +0x5b  KiCommitRescheduleContextEntry             +0x149 NextThread = 고른 것 · +0x179 State = 3
 │    ├─ (4) +0xe4   KiSearchForNewThreadsWithinRestOfSystem       고른 게 Idle 일 때 — L0 +0x20 / L1 +0x46
 │    └─ (5) +0x28c  KiReadyDeferredReadyList                      밀려난 스레드 → +0x4b KiDeferredReadySingleThread
 │  +0x179 NextThread = 0 · +0x17d CurrentThread = 새 · +0x190 새 State = 2
 │  +0x21f / +0x222  새 == 지금 → 안 바꿈 (+0x3ba)
 ├─ (4) +0x248  KiSwapContext(지금, 새, WaitIrql)
 │    └─ +0x76  SwapContext
 │         │  +0x18 새 Running = 1 · +0xf3 지금 KernelStack 저장 · +0xf7 RSP = 새 KernelStack
 │         │  +0x35e / +0x365  같은 프로세스 → CR3 그대로 / 다르면 +0x3e0 MOV CR3 (또는 +0x3d9 HvlSwitchVirtualAddressSpace)
 │         │  +0x4ad 지금 Running = 0 · +0x5a8/+0x5ad GS ← 새 Teb · +0x5bb 새 ContextSwitches +1
 │         └─ +0x5ee RET  (새 스레드 스택에서 — 예전 KiSwapContext +0x76 / 처음이면 KiStartUserThread, run15)
 │    … (다시 이 스레드 차례)
 ├─ (5) +0x253  KiAbProcessPostContextSwitch
 │  +0x25c RDI = WaitStatus (+0xc8)
 ├─ (6) +0x2f3  KiDeliverApc                                        KiSwapContext 반환 AL ≠ 0 일 때 (IRQL 1)
 └─ +0x29a RAX = WaitStatus · +0x2ac RET

NtYieldExecution
 └─ (1) +0xb   KeYieldExecution(0)                                  (Ghidra 이름 Ordinal_6)
      │  +0x4a ~ +0x69  PRCB+0x83d8 · 공유 비트맵 둘 다 0 → 0x40000024
      │  +0x79 ~ +0x82  IRQL 2
      ├─ (1) +0xbc   KiAbProcessPreContextSwitch
      │  +0xf4  NextThread == 0 이면 찾아보기:
      ├─ (2) +0x2c6  KiStartRescheduleContext
      ├─ (3) +0x2dd  KiFindRescheduleContextEntryForPrcb             → +0x2f0 FullYield · +0x2f6 DoNotSetPreemptionBit
      ├─ (4) +0x310  KiSearchForNewThreadsForRescheduleContext
      ├─ (5) +0x322  KiCommitRescheduleContext                       (그래도 NextThread 0 → +0x157 → +0x395 0x40000024)
      ├─ (6) +0x19b  KiQueryQuantumReset                             → +0x1b4 QuantumTarget 새로
      ├─ (7) +0x1c3  KiComputeNewPriority
      │  +0x1e5 NextThread = 0
      ├─ (8) +0x1f9  KiUpdatePriorityMatrixForRunningTransition      +0x3 CurrentThread = 새 · +0x11 State = 2
      │  +0x1fc WaitReason = 0x21 · +0x207 WaitIrql
      ├─ (9) +0x216  KiQueueReadyThread(지금)
      │       └─ +0x1fe  KiDeferredReadySingleThread                 Preempted 0 → 맨 뒤
      ├─ (10) +0x224 KiSwapContext(지금, 새, 1)                      → SwapContext (위와 같음)
      ├─ (11) +0x22c KiAbProcessPostContextSwitch
      └─ +0x22c EBX = 0 → 0 반환

(준비 줄에 꿰기)
KiQueueReadyThread +0x1fe · KiDeferredReadyThread +0x28 / +0x62 · KiReadyDeferredReadyList +0x4b
 └→ KiDeferredReadySingleThread
      +0x63    [RSP+0x58] = 0
      +0xd5    AdjustReason == 0 → +0x144f
      +0x144f / +0x1453  CycleTime < QuantumTarget 일 때만 → +0x1459 [RSP+0x58] = Preempted
               (퀀텀 끝 → +0x1de0 … +0x1deb KiComputeNewPriority, 칸 0 그대로)
      +0x1726 / +0x172a  0 → +0x1730 맨 뒤 / ≠ 0 → +0x1824 맨 앞 (+0x183f BTS PRCB+0x83dc)
      +0x1847 ~ +0x1857  PRCB+0x83d8 |= 1 << 우선순위
KiInsertDeferredReadyList — 준비 줄 아님: 밀린 스레드 단일 목록 맨 앞 + State = 7 (+0x33)
```

## 사진과 맞춰 본 것 (run44 메인 스레드 `ffff828598445080`, 새 실행 없음)

- 대상은 test.exe **메인 스레드 하나**다. 일꾼·둘째 스레드 칸은 사진에 없다.
- 사진은 커널 디버거가 멈춘 순간이다. 멈출 때마다 스레드 넘기기 숫자가 조금 달라질 수 있다 `[추측: 디버거 영향]`.

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 | 로그 줄 (`ms/logs/04_baggage-read_<P>.txt`) |
|---|---|---|---|---|---|---|---|---|
| `Priority (+0xc3)` | 0 | 8 | 8 | 8 | 8 | **9** | **9** | K2 763 · K3~P1 768 · P1a~P2 766 |
| `BasePriority (+0x233)` | 0 | 8 | 8 | 8 | 8 | 8 | 8 | K2 814 · K3~P1 819 · P1a~P2 817 |
| `QuantumReset (+0x28b)` | 0 | 2 | 2 | 2 | 2 | 2 | 2 | K2 834 · K3~P1 839 · P1a~P2 837 |
| `QuantumTarget (+0x20)` | 0 | `0x7f2814` | 같음 | 같음 | 같음 | `0x9fa7208` | 같음 | K2 683 · K3~P1 688 · P1a~P2 686 |
| `State (+0x184)` | 0 | 0 | 2 | 2 | 2 | 2 | 2 | K2 777 · K3~P1 782 · P1a~P2 780 |
| `Running (+0x71)` | 0 | 0 | 1 | 1 | 1 | 1 | 1 | K2 695 · K3~P1 700 · P1a~P2 698 |
| `ContextSwitches (+0x154)` | 0 | 0 | **1** | `0x17` | `0x1c` | `0x2a` | `0x31` | K2 775 · K3~P1 780 · P1a~P2 778 |
| `Preempted (+0x235)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | K2 816 · K3~P1 821 · P1a~P2 819 |
| `CycleTime (+0x48)` | 0 | 0 | `0x5dc0` | `0x2f0940` | `0x73c620` | `0x10b4c20` | `0x1422ba0` | K2 688 · K3~P1 693 · P1a~P2 691 |
| `CurrentRunTime (+0x50)` | 0 | 0 | `0x5dc0` | `0x1bc60` | `0x467940` | `0x1ab80` | `0x14640` | K2 689 · K3~P1 694 · P1a~P2 692 |
| `ExpectedRunTime (+0x54)` | `0x4c4b40` | 같음 | 같음 | `0x3ccc10` | 같음 | `0x22642b` | `0x94889` | K2 690 · K3~P1 695 · P1a~P2 693 |
| `KernelStack (+0x58)` | `…9ad072e0` | 같음 | 같음 | `…9ad06b20` | `…9ad06f80` | `…9ad07040` | `…9ad06d90` | K2 691 · K3~P1 696 · P1a~P2 694 |

- **우선순위** `[관찰]`: 메인 스레드는 초기화 내내 `BasePriority 8`. `Priority` 는 P1b 부터 9 — 잠깐 올라간 값으로 보인다 `[추측: 부스트]`. → 결과서 동적 "test.exe 스레드 우선순위" 의 **메인 스레드 몫은 사진으로 닫힘**. 일꾼·`test!Worker` 는 그대로 동적.
- **CPU 수** `[관찰]`: 2개 — PEB `NumberOfProcessors 2` (`_P1a.txt:1769`, `_K1.txt:1594`), KUSD `ActiveProcessorCount 2` (`_P1a.txt:2981`, 커널 별칭 `04_baggage-read_supp_P1a.txt:273`). 그래서 훔치기 갈래 (`KiSearchForNewThreadsWithinRestOfSystem`) 는 **탈 수 있는 환경**이다. 실제로 탔는지는 그대로 동적.
- **처음 들어올 때** `[관찰]` + `[정적]`: `ContextSwitches` 가 K3 0 → **P0 (`PspUserThreadStartup` 입구) 1**. 한 번 CPU 를 받아 `SwapContext +0x5bb` 로 1 이 되고, `RET` 이 `KiStartUserThread` 로 가서 (run15) P0 에 닿은 것과 맞다. `Running`·`State` 가 K3 0/0 → P0 1/2 인 것도 맞다 (`Running = 1` 은 `SwapContext +0x18`. `State = 2` 를 쓰는 줄은 `KiSwapThread +0x190` · `KiUpdatePriorityMatrixForRunningTransition +0x11` 등인데, 처음엔 어느 길로 골랐는지 모름).
- **기다림 넘기기 한 번이 숫자로 맞는다** (P0 → P1) — `[관찰]` 값 + `[정적]` 식, 읽기는 `[추측]`:
  - `KiSwapThread +0x45 ~ +0x56` 식 `Expected/2 + CurrentRun/2` 에 P0 `ExpectedRunTime 0x4c4b40` 를 넣으면, P1 값 `0x3ccc10` 이 나오려면 그때 `CurrentRunTime` = `0x2d4ce0`.
  - 그 뒤 `+0x5a` 가 0 으로 만들고 P1 까지 `0x1bc60` 이 쌓였다면, P0 → P1 사이에 쌓인 몫은 `(0x2d4ce0 − 0x5dc0) + 0x1bc60 = 0x2eab80` — **`CycleTime` 이 는 양 (`0x2f0940 − 0x5dc0 = 0x2eab80`) 과 딱 같다.**
  - 그래서 "P0 → P1 사이 메인 스레드는 `KiSwapThread` (기다림 쪽) 를 딱 한 번 탔다" 로 읽힌다 `[추측: CurrentRunTime 과 CycleTime 이 같이 는다고 보고]`. 어떤 기다림이었는지는 모른다.
- **P1 → P1a 는 다르다**: `CycleTime`·`CurrentRunTime` 이 똑같이 `0x44bce0` 늘었고 `ExpectedRunTime` 은 그대로다 → 이 사이 `KiSwapThread` 는 안 탔다 `[추측: 같은 읽기]`. 그런데 `ContextSwitches` 는 5 늘었다 (`0x17` → `0x1c`). 이 다섯 번은 `KiSwapThread` 가 아닌 길로 내려갔다 들어온 것으로 보인다 `[추측]` — 후보: `KiSwapContext` 를 직접 부르는 `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption`, run52 목록의 `KiDirectSwitchThread`·`KiExitDispatcher` 등 (Q141b2·Q141c).
  - 같은 구간 `QuantumTarget` 은 `0x7f2814` 그대로이고 P1a `CycleTime 0x73c620` 이 그보다 작다 → `KiDeferredReadySingleThread +0x144f` 기준으로 **퀀텀이 아직 남은 상태** 다 `[추측]`.
- **`Preempted` 는 일곱 장 모두 0** — 사진 순간엔 "선점당해 맨 앞에 설 스레드" 표시가 없었다. 사이에 켜졌다 꺼졌는지는 모른다.
- **`KernelStack`** 은 P0 → P1 부터 장마다 바뀐다 — CPU 를 내려놓을 때마다 `SwapContext +0xf3` 이 그 순간 스택 자리를 적는 것과 어긋나지 않는다.

## 확인된 것

- `[정적]` `KiSwapThread` 의 다음 스레드 고르기는 `+0xdc` (ret `+0xe1`) `KiSearchForNewThread` 한 번이다. `NextThread` 를 먼저 보는 줄은 `KiSearchForNewThread +0x20`.
- `[정적]` 고르는 순서: `NextThread` (Idle 아님) → 이 CPU 줄 + 공유 줄을 우선순위로 (1~31 `BSR` → 그룹 스케줄링 줄 → 0) → 다른 CPU 훔치기 → `IdleThread` (칸 기본값 `+0x185`·`+0x19c` 가 커밋됨). 같은 우선순위 안에선 줄 맨 앞부터.
- `[정적]` 바꾸는 줄: `KiSwapThread +0x243` → `KiSwapContext +0x71` → `SwapContext`. 새 == 지금이면 안 바꾼다 (`+0x21f/+0x222`). 같은 프로세스면 CR3 를 안 바꾼다 (`SwapContext +0x35e/+0x365`). 스택은 `+0xf3`/`+0xf7`.
- `[정적]` 돌아온 스레드는 `WaitStatus (+0xc8)` 를 돌려준다 (`+0x25c` → `+0x29a`).
- `[정적]` 준비 줄에 꿰는 함수는 `KiDeferredReadySingleThread`. 기본 맨 뒤, `Preempted ≠ 0` + 퀀텀 남음이면 맨 앞 (`+0x1726/+0x172a`). `KiInsertDeferredReadyList` 는 준비 줄이 아니라 밀린 스레드 목록.
- `[정적]` `NtYieldExecution +0x6` = `KeYieldExecution(0)` (보통 CALL). `FullYield` 라 우선순위 1 이상 아무나 받음. 지금 스레드는 새 퀀텀 + `KiQueueReadyThread` (`+0x211`). 넘길 게 없으면 `0x40000024` (`+0x69` · `+0x395`), 넘겼다 돌아오면 0 (`+0x22c`).
- `[관찰]` run44: 메인 스레드 `BasePriority 8` · `Priority 8 → 9` · `QuantumReset 2` · CPU 2개 · `ContextSwitches` P0 1 → P2 `0x31`. P0 의 1 은 run15 "새 스레드는 `SwapContext` RET 로 `KiStartUserThread`" 와 맞는다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① 결과서 5번 표 끝 "이 RET 은 … `KiSwapContext +0x76` 으로 돌아간다" → **처음 도는 스레드는 `KiStartUserThread` 로** 간다 (run15 `[정적]`: `KiInitializeContextThread +0x1e1/+0x1e8` 이 첫 스택 맨 위에 그 주소를 둠). 돌았던 스레드만 `KiSwapContext +0x76`.
- ② 결과서 "`+0x1de0` 에서 `KiComputeNewPriority`" → `+0x1de0` 은 `MOV R8B,1`, 부르는 줄은 **`+0x1de6`** (ret `+0x1deb`) (flow-writer `.asm`).
- ③ 결과서 4(c) "`+0x3bf/+0x3c5 JMP +0x395`" → `+0x3bf JNZ` 는 AL ≠ 0 일 때 본 줄기 (`+0xb2`) 로 가는 줄, `+0x395` 로 가는 건 **`+0x3c5 JMP`** (AL = 0) 하나 (flow-writer `.asm`).
- ④ `SwapContext +0x5ca` 의 스택 `[RSP+0x28]` 바이트는 run15 가 "CL 인자" 로 풀어 둔 칸과 같다 (새 스레드 = `KiInitializeContextThread +0x26f` 의 1). 결과서는 "WaitIrql" 로 적었다 — 어긋나지 않는다.
- ⑤ flow-writer 보탬: run44 사진 대조 (위 "사진과 맞춰 본 것") — 결과서엔 없음. 결과서 동적 "우선순위 숫자" 의 메인 스레드 몫과 "CPU 개수" 는 사진으로 답이 나왔다.
- ⑥ `_calls.tsv` 에 `alias:` 로 나온 callee 셋은 이름이 하나뿐인 경우다 (`pdb_aliases.py --lookup` 이 셋 다 "별칭 무리 아님 — 이름이 하나뿐"): `NtYieldExecution +0x6` → PDB `KeYieldExecution` (rva `0x2137a0`, Ghidra `Ordinal_6`), `KiSearchForNewThread +0x16c` · `KeYieldExecution +0x2d8` → `KiFindRescheduleContextEntryForPrcb` (rva `0x2c9c10`, PDB 는 C++ 꾸밈 이름 하나), `KiCommitRescheduleContext +0x56` → `KiCommitRescheduleContextEntry` (rva `0x228dd0`, 같음). 그래서 글에는 그 이름을 그대로 썼다.

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- 새 짐 칸 없음, 2장 숫자 그대로. 이미 있는 3.14 `_KTHREAD` 두 줄에 **바뀌는 쪽** 근거만 더했다:
  - `ExpectedRunTime` — 바뀜 원인 `KiSwapThread` 쪽 `[추측]` → `[정적]` (`+0x45 ~ +0x56`, `+0x5a` 와 함께). P0 → P1 값이 식 한 번과 맞음.
  - `KernelStack` — 바뀜 원인 `SwapContext +0xf3` (넘기기마다).
- 3.14 머리에 "Q78d4 에서 볼 것 — 스케줄러가 쓰는 줄" 한 줄 (7.2 의 `State`·`Running`·`ContextSwitches`·`CurrentRunTime`·`ReadyTime`·`QuantumTarget`·`WaitListEntry`, 범위 밖 70 줄의 `Preempted`·`AdjustReason`). 표로 옮기지는 않았다 (칸 수 그대로).

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만 (조건은 run 마다의 test.exe EPROCESS).

1. **`Preempted` → 맨 앞이 실제로 타나**: `nt!KiDeferredReadySingleThread+0x1726` 에서 `dd @rsp+0x58 L1` 과 `db @rbx+0x235 L1` (`Preempted`), 맨 앞 갈래 `+0x1824` 가 서는지. 스택 칸이 다른 갈래 (`+0x437` · `+0x46e` · `+0x13f7`) 값인지도 이걸로 가른다.
2. **실제 우선순위·비트맵**: `PRCB+0x83d8` (32비트) · `PRCB+0x8440` 이후 줄 머리, `PRCB+0x8e48` → `dt nt!_KSHARED_READY_QUEUE`. 일꾼·`test!Worker` 의 `Priority (+0xc3)`·`BasePriority (+0x233)` (메인 스레드는 run44 사진으로 닫힘 — 8). 상태 사진으로 가능.
3. **`KiGroupSchedulingEnabled`** (`KiSearchForNewThreadsForRescheduleContext +0x622` 가 읽는 전역) 값 — 0 이면 PE_emulator 는 ② 그룹 스케줄링 단계를 무시해도 된다.
4. **훔치기 갈래**: CPU 는 2개 (run44 `[관찰]`). `nt!KiSearchForNewThread+0xdf` 가 test.exe 스레드에서 서는지, `PRCB+0x9418` · `+0x9420` 값.
5. **test.exe 에서 `NtYieldExecution` 이 불리나**: `nt!KeYieldExecution` (WinDbg 는 PDB 이름. Ghidra 파일 이름만 `Ordinal_6`) `+0x69` · `+0x395` · `+0x224` 중 어디가 서나. 안 불리면 정적 읽기로 충분.
6. (run44 사진에서 생긴 것) **P1 → P1a 의 넘김 5번이 어느 길인가** — `nt!KiSwapContext` 입구에서 `k` (test.exe 메인 스레드일 때, `rcx` = 그 스레드). `KiSwapThread` 가 아닌 caller 가 나오면 Q141b2·Q141c 와 같이 본다.
   - (run77) 후보 하나 더: 깨운 쪽이 그 자리에서 넘어가는 `KiExitDispatcher +0x6c` → `+0x1ac KiSwapContext` (`WaitReason 0x20`) `[추측]` (sast/73). 단 P1a 사진 `WaitReason 0x1f` (`_P1a.txt:831`) 는 상수로 쓰는 줄이 `KiDispatchInterrupt +0x13e` (디스패치 인터럽트 선점) 하나뿐이라 **다섯 번 중 마지막 한 번은 그쪽** 으로 읽힌다 `[추측: 읽기]`. 앞 네 번은 그대로 모름 (`runs/03_sprint/run77-thread-wake.md` "사진과 맞춰 본 것").
- 추천 멈춤점 (결과서): `nt!KiSwapThread` 입구 (`rcx` 지금 스레드, `rdx` PRCB) · `+0xe1` `rax` (고른 스레드) · `+0x248` `rax`, `nt!KiSearchForNewThread+0x2a` (`rbx` = `NextThread` 가 있었나), `nt!KiSwapContext` 입구 (`rcx`·`rdx`·`r8b`), `nt!SwapContext+0x3e0` (프로세스가 다를 때만 닿음).

정적 — 다음 바퀴 후보 — 대기열 **Q143** (① ~ ⑤, 오케스트레이터가 붙인 번호):

- ① **새 스레드가 준비 줄에 들어가는 길**: run12 의 `PspInsertThread +0x5a1 KeReadyThread` → `+0x26 KiFastReadyThread` (본문 안 뽑음) 에서 `KiDeferredReadySingleThread` 까지 이어지는지. 첫 스레드가 처음 CPU 를 받는 길 (P0 `ContextSwitches 1`) 을 닫는다.
- ② `KiDeferredReadySingleThread` 의 `[RSP+0x58]` 다른 갈래 (`+0x437` · `+0x46e` · `+0x13f7`) 가 `+0x170c` 로 이어지나 — 위 동적 1 로도 닫힘.
- ③ 다른 CPU 훔치기 본체 (`KiSearchForNewThreadsWithinL0SearchContext` · `…L1SearchContext` · `KiSearchForNewThreadsOnTarget` 7609B) — CPU 하나 에뮬레이터면 필요 없음.
- ④ `KiSearchForNewThread +0xb4` · `+0x109` (`IdleState` 비트 0 + 고를 게 없음 → `INT 0x29`, `ECX = 0x21`) 의 뜻.
- ⑤ 그룹 스케줄링 단계 ② (SCB 줄, `KiAdvanceReadyQueueEnumeratorToNextScb` 등) — 동적 3 이 0 이면 필요 없음.
- 퀀텀 끝·선점 길 (`KiQuantumEnd`, `KiCheckForThreadDispatch +0x10e` · `KiHandleDeferredPreemption +0x297` 의 `KiSwapContext`) 은 이미 **Q141b2** — (해결됨 → run76: 퀀텀 끝은 `KiQuantumEnd +0xbd6` 직접, 두 함수는 선점 길), 기다림·깨움 (`KiCommitThreadWait +0x220`, `WaitStatus` 를 쓰는 쪽) 은 **Q141c** — (해결됨, Q141c1 → run77: 깨우는 쪽이 `WaitStatus = 블록 WaitKey` 를 그 자리에서 씀, `KeSetEvent +0x484` 등. 타이머 만료는 Q141c2).

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구: Ghidra 가 PDB `KeYieldExecution` 을 내보내기 번호 이름 `Ordinal_6` 으로 잡았다. WinDbg 멈춤점은 PDB 이름 `nt!KeYieldExecution` 으로 건다 (결과서 "스크립트 문제" — 스크립트 문제는 아님).
- 도구: "누가 `KiSwapThread` 를 부르나" 는 `_calls.tsv` 에 없고 `run_ghidra.sh refs` 로 봐야 한다 (결과서).
