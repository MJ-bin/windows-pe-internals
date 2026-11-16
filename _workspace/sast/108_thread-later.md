# 108 thread-later — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q78d4** — Q78d (첫 스레드) 의 넷째·마지막 조각: 스레드 그 뒤 구간 (커널). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련 (**같은 방식으로** — run52·53·54 와 sast/46·47·48 결과서의 네 가지 가름, 셈 기준: 명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만 "0 을 씀", 몸통 0 채움만으로는 안 셈):
  - `runs/03_sprint/baggage.md` **7.2** `_ETHREAD`·`_KTHREAD`·`_KTRAP_FRAME` 모르는 칸 목록 (아래 "범위" 에 옮김), **3.13 머리 "Q78d4 에서 볼 것"** (run53 `PspInsertThread +0x2cd` `KernelStackReference`, **`+0x617 ~ +0x649` `CrossThreadFlags` 비트 9~14** — DWORD 되쓰기 기준으로 비트 칸 19개 가름이 "nt 가 씀" 으로 옮겨질 수 있음, run54 `UpdateTebApc` 큐 길 `KiQueueTebUpdateApc` ← `KiSetIdealProcessorThread +0x10e`·`KiSetAffinityThread +0xf1`), **3.14 머리** (run52 `KeStartThread` 쓰는 줄 — `K2~K3` 10 중 8 + sast/46 2.4 의 5칸·친화도 블록 비트맵, run75 스케줄러 줄, run76 퀀텀 줄, run77 기다림·깨움 줄, run78 타이머 줄), **3.15 머리** (run53 `P1~P1b` 25칸 중 만들기 길이 쓰는 것 — `Rax` `①KX… +0xdd`, `R8~R11` 등), 3.3 머리 run54 (`P0~P1` 다섯 칸을 `KiUpdateTebApc` 가 다시 쓰는 길 — `UserAffinity`·`UserIdealProcessor` 최종 출처)
  - sast/46·47·48 결과서, sast/71·72·73·74·75 결과서 (스케줄러 — 쓰는 줄이 이미 나온 칸은 그 줄을 근거로 재사용, 다시 안 뽑음)
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/107) 을 돌린다 — ntdll 쪽은 건드리지 않는다. **이미 `.c`·`.asm` 이 있는 함수는 다시 뽑지 않는다** (`PspInsertThread`·`KeStartThread`·`PspUserThreadStartup`·`KiSwapThread`·`KiCommitThreadWait` 류 대부분 있음). 필요한 것만 `^이름$`.

## 범위 — 모르는 칸 82

- **`_ETHREAD` 10**:
  - `K2~K3` (5): ThreadListEntry*, CrossThreadFlags, CrossThreadFlags.ThreadIoPriority, CrossThreadFlags.ThreadPagePriority, KernelStackReference
  - `K3~P0` (4): LastExpectedRunTime*, LastSoftParkElectionGroupAffinity, LastSoftParkElectionQos*, LastSoftParkElectionRunningType
  - `P1~P1a` (1): SchedulerSharedDataObject
- **`_KTHREAD` 47**:
  - `K2~K3` (10): QuantumTarget*, ThreadFlags.SharedReadyQueueAffinity, Priority*, UserIdealProcessor, BasePriority, IdealProcessor, SavedApcState*, QuantumReset, ThreadListEntry*, WobPriority
  - `K3~P0` (9): CycleTime*, CurrentRunTime*, Running, WaitListEntry*, SwapListEntry*, ContextSwitches*, State, WaitTime*, AffinityVersion
  - `P0~P1` (9): WaitRegister*, WaitBlockList, WaitBlockFill4*, WaitBlockCount, SavedApcStateFill, ReadOperationCount, OtherOperationCount*, ReadTransferCount, KernelWaitTime*
  - `P1~P1a` (7): SystemCallNumber*, FirstArgument*, TrapFrame, WaitReason*, SchedulerSharedSystemSlot, ResourceIndex, SchedulerSharedSwappablePage
  - `P1a~P1b` (6): ThreadFlags.AutoBoostEntriesExhausted, ReadyTime, WaitMode*, AdjustIncrement*, DecayBoost, UserWaitTime
  - `P1b~P2` (6): MiscFlags.Alertable, WaitStatus, NextProcessor, NextProcessor.NextProcessorNumber, WriteOperationCount, WriteTransferCount
- **`_KTRAP_FRAME` 25**:
  - `P1~P1a` (24): P1Home*, P2Home*, P3Home, PreviousMode, InterruptRetpolineState, FaultIndicator*, NmiMsrIbrs*, Rax*, R8*, R9*, R10*, R11*, GsBase, GsSwap, Xmm1*, FaultAddress*, ContextRecord*, NmiPreviousSpecCtrl*, NmiPreviousSpecCtrlPad*, Rbx*, Rdi*, Rsi*, ErrorCode*, ExceptionFrame*
  - `P1a~P1b` (1): P4Home*
  - (Q78d4 줄 원문) 그중 **24 는 시스템 콜·`int3` 트랩 프레임 덮임** — 짐이 아니라 그때그때 덮이는 칸이라 **가름만** (어느 입구 — `KiSystemCall64`·`KiBreakpointTrap` 류 — 가 어느 칸을 쓰나, 이름만으로 충분).
- `*` 칸은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만.

## 알고 싶은 것

1. **`K2~K3` (`_ETHREAD` 5 · `_KTHREAD` 10)** — `PspInsertThread` (스레드를 프로세스 목록에 넣기 — `ThreadListEntry` 둘) · `KeStartThread` (우선순위·퀀텀·이상적 프로세서·친화도) 쓰는 줄. 칸마다 함수+오프셋, 값의 출처 (프로세스 칸 / 상수 / 입력). **`CrossThreadFlags` 비트 9~14 (`+0x617 ~ +0x649`)** 는 DWORD 되쓰기면 비트 칸 19개 (조건부 2 · 안 씀 3 · 범위 밖 14) 를 "0 을 씀" 으로 다시 셀 수 있는지 판단 — 바뀌는 칸 이름 목록.
2. **`K3~P0` 이후 (`_ETHREAD` 5 · `_KTHREAD` 37)** — 첫 스레드가 처음 CPU 를 받고 (`KiSwapThread` 류), 기다리고 (`KiCommitThreadWait`), 깨어나고, syscall 을 들어가는 (`KiSystemCall64` — `SystemCallNumber`·`FirstArgument`·`TrapFrame`) 길에서 칸을 쓰는 줄. **sast/71~75 에서 이미 나온 줄은 그 결과서 줄을 근거로 옮기기만** (다시 안 뽑음). 칸마다 "어느 단계 (사진 구간) 에 처음 값이 생기나" 를 맞춰 본다 (run44 사진 구간과 어긋나면 적기).
   - `UserIdealProcessor`·`UserAffinity` 최종 출처 (run54 넘김), `KiQueueTebUpdateApc` 길이 첫 스레드에서 타나.
   - 카운터 칸 (`ReadOperationCount` 류, `CycleTime`, `ContextSwitches`, `*WaitTime`) 은 "쓰는 함수 이름 + 단계" 정도면 충분 (짐 지도에서 "값이 계속 바뀌는 칸" 으로 묶임).
3. **`_KTRAP_FRAME` 25** — 가름만: 어느 입구가 어느 칸을 쓰나 (syscall 입구 / `int3` 입구 / 예외 입구), 첫 스레드의 트랩 프레임이 처음 덮이는 때 (첫 syscall — 사진 `P1` 직전).
4. **간접 호출** — 이 길에서 위 칸을 쓰는 간접 호출이 있으면 대상을 좁히고, 못 정하면 동적 질문으로. 없으면 "0개" 와 본 `.asm` 수.
5. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 커널 쪽 줄임 (`①A…`, `①K…`, `①KC…`, `①KX…`) 은 run52·53 그대로. 공용 부품은 이름만.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준. **있는 `.asm` 은 다시 뽑지 않는다.**

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `PspInsertThread` (있음) | 0 | 질문 1 — 읽기만 |
| ntoskrnl | `KeStartThread` (있음) | 0 | 질문 1 — 읽기만 |
| ntoskrnl | `KiSwapThread`·`KiCommitThreadWait`·`KiDeferredReadySingleThread`·`KiReadyThread` 류 (대부분 있음) | 0 | 질문 2 — 읽기만 |
| ntoskrnl | `KiSystemCall64`·`KiSystemServiceUser`·`KiBreakpointTrap` (없으면 뽑기, 이름은 `_index.tsv`·`pdb_aliases.py`) | 0 | 질문 2·3 |
| ntoskrnl | (필요하면) `KiQueueTebUpdateApc`·`KiUpdateTebApc`·`KiSetIdealProcessorThread`·`KiSetAffinityThread` | 0 | 질문 2 — run54 넘김 |

- 칸 이름·오프셋은 `ms/disassemble/ntoskrnl/_baggage/struct__ETHREAD.tsv`·`struct__KTHREAD.tsv`·`struct__KTRAP_FRAME.tsv` (sast/38) 또는 `pdb_struct.py`.
- 훑기 한계 (스레드 포인터를 얻는 방식 — `gs:[0x188]`·인자) 를 결과서에 한 줄로.

## 받고 싶은 것

- 결과서 `_workspace/sast/108_thread-later_result.md`. sast/46·47·48 과 같은 꼴:
  - 칸 82 전부의 가름 표 (구조체 · 구간 · 칸 · 가름 (아는 칸 / 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길에서만 씀 `[추측]` / 트랩 프레임 덮임) · 쓰는 줄 · 경로)
  - 셈 요약 (가름별 수, `CrossThreadFlags` 다시 셈이 있으면 그 변화)
  - baggage.md 3.13·3.14·3.15 에 넣을 줄 (3장 양식)
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** (사진 구간과 어긋나는 칸) 과 **"다음 요청 후보"**.
