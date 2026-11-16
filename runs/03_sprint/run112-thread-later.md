# 2026-11-16 (작성일) run112 — 첫 스레드 그 뒤 구간 (Q78d4, 정적만): 스레드 모르는 칸 82 → **여전히 모름 0** — 아는 칸 55 · 범위 밖 길 2 · **트랩 프레임 덮임 25** (짐 아님). `K2~K3` 15칸은 `PspInsertThread` (`_ETHREAD` 5) 와 `KeStartThread` (`_KTHREAD` 9) + `KeSuspendThread` (`SuspendCount` 바이트), `K3~P0` 은 부모 `NtResumeThread` 가 새 스레드를 준비 줄에 세울 때 (`KiDeferredReadySingleThread`) 와 처음 CPU 를 받을 때 (`KiSwapThread` · `SwapContext`), 그 뒤는 기다림 · syscall 입구 · 스케줄러 공유 표 (`PspSchedulerSharedDataRegionSlotAllocate`). `CrossThreadFlags` 비트 칸 17개는 `PspInsertThread +0x632 / +0x649` 의 DWORD 통째 되쓰기라 **"0 을 씀" 으로 다시 셈** (오케스트레이터 결정). run52 의 `SharedReadyQueueAffinity` 줄 정정.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림은 안 고쳤다 (run52 · run53 과 같은 까닭). 새 단계 · 새 caller 가 없고, `PspInsertThread` · `KeStartThread` · 스케줄러 · syscall 입구 **안쪽** 대표 지점만 늘었다. 글은 flow.md 2.1 · 2.2 · 2.3 · 4장 · 5장, 짐 지도는 3.13 · 3.14 · 3.15 를 다시 썼다.

## 요약 표 (모르는 칸 82 → ?)

결과서 맨 앞 표 그대로. 가름 뜻은 run50 · run52 · run53 과 같고, 이번에 **"트랩 프레임 덮임"** 이 새로 생겼다.

| | 칸 | 아는 칸 (nt 가 씀) | 범위 밖 길 `[추측]` | 트랩 프레임 덮임 | 여전히 모름 |
|---|---|---|---|---|---|
| `_ETHREAD` `K2~K3` | 5 | 5 | — | — | 0 |
| `_ETHREAD` `K3~P0` | 4 | 4 | — | — | 0 |
| `_ETHREAD` `P1~P1a` | 1 | 1 | — | — | 0 |
| `_KTHREAD` `K2~K3` | 10 | 10 | — | — | 0 |
| `_KTHREAD` `K3~P0` | 9 | 9 | — | — | 0 |
| `_KTHREAD` `P0~P1` | 9 | 8 | 1 (`SavedApcStateFill`) | — | 0 |
| `_KTHREAD` `P1~P1a` | 7 | 6 | 1 (`ResourceIndex`) | — | 0 |
| `_KTHREAD` `P1a~P1b` | 6 | 6 | — | — | 0 |
| `_KTHREAD` `P1b~P2` | 6 | 6 | — | — | 0 |
| `_KTRAP_FRAME` `P1~P1a` | 24 | — | — | 24 | 0 |
| `_KTRAP_FRAME` `P1a~P1b` | 1 | — | — | 1 | 0 |
| **합** | **82** | **55** | **2** | **25** | **0** |

- **트랩 프레임 덮임** = 입구 (`int3` · 시스템 콜 · 페이지 오류) 가 그때그때 같은 자리를 덮는 칸. 짐이 아니라 "어느 입구가 어느 칸을 쓰나" 만 갈랐다 (아래 3).
- **`CrossThreadFlags` 비트 칸 19개 다시 셈** (오케스트레이터 결정 — run52 · run53 기준 "같은 DWORD 되쓰기는 0 을 씀" 그대로):

  | 가름 | 칸 수 | 바뀜 |
  |---|---|---|
  | 조건부 안 탐 (`HideFromDebugger` · `IndirectCpuSets`) | 2 | 그대로 |
  | 아무도 안 씀 (`RundownFail` · `UmsForceQueueTermination` · `ReservedCrossThreadFlags`) | 3 | → **0 을 씀** |
  | 범위 밖 길 (`Terminated` · `ActiveImpersonationInfo` … `ImpersonationSchedulingGroup`, 14) | 14 | → **0 을 씀** |

  - 근거: `PspInsertThread +0x617` 이 DWORD 를 읽고 `+0x632` · `+0x649` 가 통째로 두 번 되쓴다. 바꾸는 비트는 9 ~ 14 (`ThreadIoPriority` · `ThreadPagePriority`) 뿐이고 나머지는 읽은 0 을 그대로 쓴다 `[정적]`.
  - 사진: K3 ~ P2 일곱 장 모두 `0x5402` (켜진 비트 = 아는 칸 셋 `ThreadInserted` · `ThreadIoPriority` · `ThreadPagePriority`) `[관찰]` run44.
- 그래서 짐 지도 숫자 (2장):
  - `_ETHREAD` 아는 칸 48 → **75** (이 요청 10 + 다시 셈 17) · 아무도 안 씀 22 → **19** · 범위 밖 69 → **55** · 모름 10 → **0**.
  - `_KTHREAD` 아는 칸 99 → **149** (이 요청 45 + `KeStartThread` 5칸 — 셈 결정 (결정 → run52 기준으로 옮김)) · 아무도 안 씀 21 · 범위 밖 70 → **67** · 모름 47 → **0**.
  - `_KTRAP_FRAME` 아는 칸 23 · 아무도 안 씀 12 · 범위 밖 6 · **덮임 25** · 모름 25 → **0**.
  - 구조체 21개 1,456 = 아는 칸 739 → **816** (정확 708 → **785** · 짐작 31) + 아무도 안 씀 160 → **157** + 범위 밖 257 → **240** + **덮임 25** + 모름 300 → **218**.
  - 아는 칸 중 끝까지 0 (짐 지도 7.1): 304 → **327** (run111 몫 1 + `CrossThreadFlags` 17 + `KeStartThread` 5).

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음 — 결과서, 사진 로그 `ms/logs/04_baggage-read_*.txt` 의 `!lmi nt` 와도 같음).
  - 다시 analyze 안 함. 새로 뽑은 것 10개 (아래 "방법"), 나머지는 이미 있던 `.asm` 읽기만.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. 사진 값과 코드가 칸마다 맞는다 (아래 표 "사진" 열).
- 사진 값 대조: 결과서가 run44 덤프 읽기 로그 일곱 장 (`ms/logs/04_baggage-read_{K2,K3,P0,P1,P1a,P1b,P2}.txt` 의 `02c ETHREAD_RAW` · `02` · `02b` · `03`) 을 칸마다 다시 읽었다. 새 동적 실행은 없다.
- **sast/108 결과만 넣었다.** sast/109 ~ 120 결과는 이 문서에 안 넣었다 (다음 바퀴들 몫).

## 목적

- **Q78d4** (Q78d 첫 스레드의 넷째 · 마지막 조각 — 스레드 그 뒤 구간, 커널): 짐 지도 7.2 의 `_ETHREAD` 10 · `_KTHREAD` 47 · `_KTRAP_FRAME` 25 칸이 언제 · 누가 처음 쓰나.
  - 3.13 · 3.14 · 3.15 머리 "Q78d4 에서 볼 것" 메모 (run52 · run53 · run54 · run75 ~ run78 · run81) 를 줄로 옮기기.
  - `CrossThreadFlags` 비트 칸 19개를 "0 을 씀" 으로 다시 셀 수 있나.
  - `UserIdealProcessor` · `UserAffinity` 최종 출처, `KiQueueTebUpdateApc` 길이 첫 스레드에서 타나 (run54 넘김).

## 방법

- 정적: `_workspace/sast/108_thread-later.md` — ntoskrnl, 모두 depth 0 `^이름$`.
  - 새로 뽑음 10개 (두 번): `KiSetIdealProcessorThread` · `KiSetAffinityThread` (run54 넘김 — `KiQueueTebUpdateApc` 의 호출자 둘), `KiUpdateSharedReadyQueueAffinityThread` · `KiUpdateSoftParkElectionStatisticsOnInsertion` · `KiComputeThreadAffinity` · `PspSchedulerSharedDataRegionSlotAllocate` · `KeSelectInitialIdealProcessorForThread` · `KiCopyAffinityEx` · `KiInitializeForegroundBoostThread` · `KiAdjustProcessIdealProcessorSetsForThreadCreation`.
  - 읽기만: `PspInsertThread` · `KeStartThread` · `KiSwapThread` · `KiCommitThreadWait` · `KiDeferredReadySingleThread` · `SwapContext` · `KeWaitForSingleObject` (Ghidra 이름 `KeWaitForMutexObject`) · `KiTryUnwaitThread` · `KiProcessThreadWaitList` · `KiInsertDeferredReadyList` · `KeSuspendThread` · `KeSetEvent` · `KiSystemServiceUser` · `KiSystemServiceStart` · `KiBreakpointTrap` · `KiPageFault` · `KiExceptionDispatch` · `KxContextToKframes` · `PspUserThreadStartup` · `KiQueueTebUpdateApc` · `KiUpdateTebApc` 등. sast/71 ~ 75 결과서 줄은 근거를 옮기고 `.asm` 줄을 다시 대조했다.
  - **`KiSystemCall64` (rva `0x6c1840`) · `KiSystemCall64Shadow` (rva `0xbcb200`) 는 Ghidra 함수가 아니라 `objdump` 로만** 봤다 (run56 과 같은 사정 — `pdb_aliases.py --where` 가 "어느 함수 범위에도 안 듦").
  - 커널 전체 `[reg+칸]` 쓰기 훑기는 결과서 스크래치 (커밋 안 함). 훑기 한계: 레지스터가 `gs:[0x188]` (지금 스레드) 에서 왔는지 인자 스레드인지 안 가린다 — `.asm` 을 직접 읽은 곳만 "누구 스레드 칸" 이라고 적었다.
- 결과서: `_workspace/sast/108_thread-later_result.md` (**분석성공** — 스크립트 문제 없음, 제안 셋).
- flow-writer 가 다시 본 것:
  - `.asm` 줄: `PspInsertThread +0x2c8` · `+0x2cd` · `+0x2e0` · `+0x617` · `+0x61e` · `+0x632` · `+0x63b` · `+0x649` · `+0x6e3`, `KeStartThread +0x99` · `+0xa1` · `+0xb9` · `+0xc2` · `+0xc8` · `+0xce` · `+0xec` · `+0x134` · `+0x147` · `+0x14f` · `+0x156` · `+0x173` · `+0x17c` · `+0x187` · `+0x1b7` · `+0x1c1` · `+0x1d3` · `+0x1dc` · `+0x1e0` · `+0x1e9` · `+0x20f` · `+0x32b` · `+0x339` · `+0x344` · `+0x353` · `+0x36c` · `+0x380` · `+0x38d`, `KiUpdateSharedReadyQueueAffinityThread +0x55`, `KiDeferredReadySingleThread +0x2a9` · `+0x3b5` · `+0x11f8` · `+0x122e` · `+0x13cf` · `+0x151d` · `+0x152b` · `+0x171f` · `+0x1877`, `KiComputeThreadAffinity +0x212`, `KiUpdateSoftParkElectionStatisticsOnInsertion +0x1f` · `+0x41` · `+0x5b` · `+0x69` · `+0x7e`, `SwapContext +0x18` (+ 조각 `FUN_1406b686f +0x28c` = `SwapContext +0x5bb`, `SwapContext.asm` 291줄 `frag:` 표시), `KiSwapThread +0x5a` · `+0x190` · `+0x317`, `KeSuspendThread +0x60`, `KeWaitForSingleObject +0x112` · `+0x116` · `+0x11a` · `+0x126` · `+0x1b0` · `+0x1c5` · `+0x1d8` · `+0x1dd` · `+0x1e2` · `+0x7d2`, `KiCommitThreadWait +0x5b` · `+0xbf`, `KiTryUnwaitThread +0x142` · `+0x1bb`, `KiProcessThreadWaitList +0x9f`, `KeAbPreAcquire +0x52`, `KiSystemServiceUser +0x4` · `+0xcb` · `+0xd2`, `KiSystemServiceStart +0x0` · `+0xfa` · `+0x193`, `KiBreakpointTrap +0x18`, `PspSchedulerSharedDataRegionSlotAllocate +0x5b` · `+0x1a4` · `+0x1ca` · `+0x1d1`.
  - `_calls.tsv` (10,770줄): 아래 표의 call · ret 줄 전부.
  - `pdb_aliases.py --lookup ntoskrnl.exe 0x3bc560` → 이 주소엔 이름 하나 (`KiCopyAffinityEx`). `.asm` 의 `alias:KiCopyAffinityEx+1` 표시는 별칭 무리가 아니다.

## 결과

비유 (새 직원 = 첫 스레드):

- **명부에 올리기** (`K2~K3`): 인사과 (`PspInsertThread`) 가 회사 명부 (`_EPROCESS` 스레드 목록) 끝에 이름을 적고, 사원증 칸 (`CrossThreadFlags`) 에 "입출입 우선순위 · 쪽 우선순위" 를 회사 기본값으로 찍는다. 근무 조건 담당 (`KeStartThread`) 은 근무 등급 (`Priority`) · 근무 시간 (`QuantumTarget`) · 앉을 자리 (`IdealProcessor`) 를 정한다.
- **대기 줄에 서기 · 처음 출근** (`K3~P0`): 부모가 "이제 일해" (`NtResumeThread`) 하면 줄 관리인 (`KiDeferredReadySingleThread`) 이 줄에 세우며 시각 (`WaitTime`) · 자리표 판 (`AffinityVersion`) 을 적고, 처음 자리에 앉을 때 (`SwapContext`) "근무 중" (`Running`) · 출근 횟수 (`ContextSwitches`) 가 생긴다.
- **그 뒤** (`P0 ~ P2`): 기다리고 깨는 칸 · 파일 일 횟수 · 시스템 콜 번호 · 스케줄러 공유 표 자리.
- **트랩 프레임** 은 사물함이 아니라 **출입문 기록지** 다. 문 (`int3` · 시스템 콜 · 페이지 오류) 을 지날 때마다 새로 적히고, 그 문이 안 적는 칸은 앞 문이 적은 게 남는다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x1015` | `+0x101a` | `PspInsertThread` | 부모 cmd 스레드 (2.1) | [정적] (run9 그대로) |
| `nt!PspInsertThread` | `+0x5dd` | `+0x5e2` | `KeStartThread` | 흐름상 제일 먼저 (run12) | [정적] |
| `nt!KeStartThread` | `+0x99` · `+0x38d` | `+0x9e` · `+0x392` | `KiAcquireProcessLockExclusive` · `KiReleaseProcessLockExclusive` | 프로세스 락 (이름만) | [정적] (run49 그대로) |
| `nt!KeStartThread` | `+0xa1` | `+0xa6` | `KiAdjustProcessIdealProcessorSetsForThreadCreation` | 이상적 프로세서 묶음 고치기 (이름만) | [정적] |
| `nt!KeStartThread` | `+0xec` · `+0x134` · `+0x147` | `+0xf1` · `+0x139` · `+0x14c` | `KiCopyAffinityEx` | 친화도 비트맵 복사: 프로세스 `[RDI+0x50]` → 지역 사본 → `Affinity` · `UserAffinity` | [정적] |
| `nt!KeStartThread` | `+0x173` | `+0x178` | `KeSelectInitialIdealProcessorForThread` | 첫 이상적 프로세서 계산 → `+0x3f` (ret `+0x44`) `KeSelectIdealProcessor` (이름만) | [정적] |
| `nt!KeStartThread` | `+0x1e0` | `+0x1e5` | `KiAcquireKobjectLockSafe` | 프로세스 스레드 목록 잠금 (`ThreadListEntry` 달기 앞) | [정적] |
| `nt!KeStartThread` | `+0x344` | `+0x349` | `KiUpdateSharedReadyQueueAffinityThread` | `ThreadFlags` 비트 13 (`+0x55 XOR.LOCK [RDX+0x78],0x2000`) | [정적] |
| `nt!PspInsertThread` | `+0x2c8` | `+0x2cd` | `ObReferenceObjectExWithTag(스레드, 2, 'Dflt')` | 바로 뒤 `+0x2cd` `KernelStackReference = 1` | [정적] |
| `nt!PspInsertThread` | `+0x2e0` | `+0x2e5` | `KeSuspendThread` | 만들기 플래그 비트 0 (`CREATE_SUSPENDED` 길) → `+0x60` `SuspendCount` | [정적] (run12 그대로) |
| `nt!NtResumeThread` | `+0x96` | `+0x9b` | `PsMultiResumeThread` | 부모의 다음 시스템 콜 (2.2) | [정적] (run13 그대로) |
| `nt!PsMultiResumeThread` | `+0x20` | `+0x25` | `KeResumeThread` | | [정적] (run13 그대로) |
| `nt!KeResumeThread` | `+0x82` / `+0xa0` | `+0x87` / `+0xa5` | `KiResumeThread` / `KiExitDispatcher` | 멈춤 횟수 0 이면 깨움 → 끝에 디스패처 | [정적] (run13 · run77 그대로) |
| `nt!KiExitDispatcher` | `+0x41` | `+0x46` | `KiProcessThreadWaitList` | 깨운 스레드 목록 처리 | [정적] |
| `nt!KiProcessThreadWaitList` | `+0x101` | `+0x106` | `KiDeferredReadySingleThread` | **새 스레드를 준비 줄에** — `K3~P0` 칸 대부분 | [정적] |
| `nt!KiDeferredReadySingleThread` | `+0x152b` | `+0x1530` | `KiUpdateSoftParkElectionStatisticsOnInsertion` | `_ETHREAD` `LastSoftParkElection*` 셋 | [정적] |
| `nt!KiDeferredReadySingleThread` | `+0x1877` | `+0x187c` | `KiComputeThreadAffinity` | `+0x2a9 CMP [RBX+0x238],RAX` (스레드 `AffinityVersion` ≠ 전역) 일 때 | [정적] |
| `nt!KiSwapThread` | `+0x243` · `+0x4f3` | `+0x248` · `+0x4f8` | `KiSwapContext` → `+0x71` (ret `+0x76`) `SwapContext` | 처음 CPU 받기 — `Running` · `ContextSwitches` | [정적] (run75 그대로) |
| `nt!PspUserThreadStartup` | `+0x94` | `+0x99` | `KeUpdateTebSchedulingPropertiesCurrentThread` | `KeStartThread` 가 정한 값을 TEB 로 (run54) | [정적] |
| `nt!PspNotifyThreadCreation` | `+0x1c` · `+0xda` | `+0x21` · `+0xdf` | `DbgkCreateThread` · `PfProcessCreateNotification` | `P0~P1` 커널 파일 읽기 후보 `[추측]` (아래 2.7) | [정적] |
| `nt!NtSetInformationThread` | `+0xc2c` | `+0xc31` | `PspSchedulerSharedDataRegionSlotAllocate` | 클래스 `0x39` (ntdll `LdrpAcquireSchedulerSharedDataSlot +0x84`, sast/36) | [정적] |
| `nt!PspSchedulerSharedDataRegionSlotAllocate` | `+0x10e` · `+0x1b7` · `+0x26b` | `+0x113` · `+0x1bc` · `+0x270` | `RtlFindClearBitsAndSet` · `KeSwappablePageGetLockedAddress` · `ZwAllocateVirtualMemory` | 빈 칸 고르기 · 시스템 주소 · (안 붙은 쪽이면) 쪽 예약 — 이름만 | [정적] |
| `nt!KiSystemServiceStart` | `+0x193` · `+0xa24` · `+0xa7f` | `+0x195` · … | `CALL RAX` (간접) | 시스템 서비스 본체 (`KeServiceDescriptorTable` 에서 `SystemCallNumber` 로 고름) | [정적] |

### 호출 관계

```
[부모 cmd 스레드 — NtCreateUserProcess (2.1), K2~K3]
NtCreateUserProcess
 └─ +0x101a  PspInsertThread                                  (새 스레드 = R14)
     ├─ (1) +0x5e2  KeStartThread                             (새 스레드 = RBX, 프로세스 = RDI)
     │    ├─ +0x9e   KiAcquireProcessLockExclusive
     │    ├─ +0xa6   KiAdjustProcessIdealProcessorSetsForThreadCreation
     │    │          +0xb9  ThreadFlags (DisableBoost ← 프로세스)   +0xc2 BasePriority   +0xc8 Priority   +0xce WobPriority = 0x20
     │    ├─ +0xf1 · +0x139 · +0x14c  KiCopyAffinityEx         친화도 비트맵 (UserAffinity · Affinity)
     │    ├─ +0x178  KeSelectInitialIdealProcessorForThread  → +0x17c UserIdealProcessor · +0x187 IdealProcessor
     │    │          +0x1b7 ThreadFlags (DisableQuantum ← 프로세스)   +0x1c1 QuantumReset   +0x1dc QuantumTarget
     │    ├─ +0x1e5  KiAcquireKobjectLockSafe                → +0x1e9 ~ +0x2fe ThreadListEntry 꼬리 달기
     │    ├─ +0x349  KiUpdateSharedReadyQueueAffinityThread  → +0x55 ThreadFlags 비트 13
     │    └─ +0x392  KiReleaseProcessLockExclusive
     ├─ +0x2cd  (ObReferenceObjectExWithTag 뒤) KernelStackReference = 1
     ├─ +0x2e5  KeSuspendThread  → +0x60 SuspendCount 1      (CREATE_SUSPENDED 길)
     ├─ +0x617 ~ +0x649  CrossThreadFlags DWORD 되쓰기 (비트 9~14 ← EPROCESS +0x1f4 · +0x1f0)
     └─ +0x6e3 · +0x239 ~ +0x243  ThreadListEntry (_EPROCESS +0x370 꼬리)

[부모의 다음 시스템 콜 — NtResumeThread (2.2), K3~P0]
NtResumeThread ─ +0x9b PsMultiResumeThread ─ +0x25 KeResumeThread
                                              ├─ +0x87  KiResumeThread                      (SuspendEvent = 1, run13)
                                              └─ +0xa5  KiExitDispatcher ─ +0x46 KiProcessThreadWaitList
                                                                           └─ +0x106 KiDeferredReadySingleThread
                                                                                ├─ +0x3b5  NextProcessor   +0x11f8 WaitTime = TickCount
                                                                                ├─ +0x122e State = 1       +0x151d LastExpectedRunTime
                                                                                ├─ +0x1530 KiUpdateSoftParkElectionStatisticsOnInsertion
                                                                                ├─ +0x171f ~ WaitListEntry (준비 줄 고리)
                                                                                └─ +0x187c KiComputeThreadAffinity → +0x212 AffinityVersion
[처음 CPU — 새 스레드]
KiSwapThread  +0x5a CurrentRunTime = 0 · +0x190 State = 2
 └─ +0x248  KiSwapContext ─ +0x76 SwapContext   +0x18 Running = 1 · +0x5bb ContextSwitches +1

[test.exe 스레드 — P0 ~ P2]
PspUserThreadStartup  (P0~P1: 기다림 한 번 · 파일 읽기 한 번 — 누가인지 D2)
KiSystemCall64 ─(+0x2a2 이어짐)─ KiSystemServiceUser  +0xcb FirstArgument · +0xd2 SystemCallNumber
                                  └─ KiSystemServiceStart  +0x0 TrapFrame · +0x193 CALL RAX (Nt*)
                                                            └─ NtSetInformationThread (0x39) ─ +0xc31 PspSchedulerSharedDataRegionSlotAllocate
                                                                                                 +0x1a4 · +0x1ca · +0x1d1
```

### 1. `K2~K3` — `_ETHREAD` 5 · `_KTHREAD` 10 (결과서 질문 1)

**답: 15칸 모두 쓰는 줄을 찾았다** `[정적]`. 사진 K3 값과 칸마다 맞다 `[관찰]` run44.

| 칸 | 쓰는 줄 | 값의 출처 | 사진 (K3) |
|---|---|---|---|
| `_ETHREAD +0x578` `ThreadListEntry*` | `PspInsertThread +0x6e3` (`LEA`) → `+0x239` · `+0x23c` · `+0x240` · `+0x243` (꼬리 달기, 앞 `+0x6bb ~ +0x6d5` 프로세스 푸시락) | 프로세스 `_EPROCESS +0x370 ThreadListHead` | 자기 연결 한 칸 (첫 스레드) |
| `+0x5a0` `CrossThreadFlags*` | `PspInsertThread +0x617` 읽기 → `+0x632` · `+0x649` DWORD 통째 되쓰기 (앞 `+0x490` `ThreadInserted`) | 아래 두 줄 | `0x5402` |
| `CrossThreadFlags.ThreadIoPriority` (비트 9~11) | `+0x61e ~ +0x632` | 프로세스 `+0x1f4 Flags.DefaultIoPriority` | 2 |
| `CrossThreadFlags.ThreadPagePriority` (비트 12~14) | `+0x63b ~ +0x649` | 프로세스 `+0x1f0 Flags2.DefaultPagePriority` | 5 |
| `+0x5ec` `KernelStackReference` | `PspInsertThread +0x2cd` | 상수 1 (`+0xbc MOV R13D,1`) | 1 |
| `_KTHREAD +0x20` `QuantumTarget*` | `KeStartThread +0x1dc` (`+0x1c7 IMUL`) | 프로세스 `QuantumReset` × 전역 `KiCyclesPerClockQuantum` | `0x7f2814` |
| `+0x78` `ThreadFlags.SharedReadyQueueAffinity` (비트 13) | `KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` | 계산 (스레드 친화도가 준비 줄 CPU 마스크를 다 덮으면 1) | 1 |
| `+0xc3` `Priority*` · `+0x233` `BasePriority` | `KeStartThread +0xc8` · `+0xc2` | 프로세스 `BasePriority` | 8 · 8 |
| `+0xc4` `UserIdealProcessor` · `+0x24c` `IdealProcessor` | `KeStartThread +0x17c` · `+0x187` (값 `+0x173 KeSelectInitialIdealProcessorForThread`) | 계산 (입력 `R8` ≠ 0 이면 입력값 + `+0x20f` 비트 0 — test.exe 는 0) | 1 · 1 |
| `+0x258` `SavedApcState*` | 꼬리 바이트 `SuspendCount` (+0x284) 만: `PspInsertThread +0x2e0 → KeSuspendThread +0x60` | 상수 (+1) | 바이트 +0x284 = 1 |
| `+0x28b` `QuantumReset` | `KeStartThread +0x1c1` | 프로세스 `+0x91 QuantumReset` | 2 |
| `+0x2f8` `ThreadListEntry*` | `KeStartThread +0x1e9` (`LEA`) → `+0x2f4` · `+0x2f7` · `+0x2fb` · `+0x2fe` (목록 잠금 `+0x1e0` 안) | 프로세스 `_KPROCESS +0x30 ThreadListHead` | 자기 연결 한 칸 |
| `+0x31b` `WobPriority` | `KeStartThread +0xce` | 상수 `0x20` | 32 |

- 같은 함수의 나머지 쓰기 (요청서 칸 밖, flow-writer 가 `.asm` 대조): `+0xb9` `ThreadFlags` 비트 3 `DisableBoost` · `+0x1b7` 비트 8 `DisableQuantum` (둘 다 DWORD 통째 쓰기, 값 = 프로세스 비트), `+0x1d3` 비트 5 지움, `+0x20f` 비트 0 켬 (입력이 있을 때만), `+0x14f` `AffinityPrimaryGroup` · `+0x156` `UserAffinityPrimaryGroup` · `+0x32b` `SchedulingGroup` · `+0x36c` `ThreadTimerDelay` · `+0x380` `HardwareFeedbackClass` (sast/46 이 "범위 밖 길" 로 센 5칸 — **쓰는 줄 확인**, 셈 결정 (결정 → run52 기준으로 옮김) → 아래 "결과서와 다르게 적은 곳"), `+0x339` · `+0x353` `Header` 비트 18 · 23. 친화도 블록 비트맵 `+0xec` → `+0x134` · `+0x147` — **run52 의 `[공백]` "친화도 블록 비트맵이 채워지는 때" 가 닫혔다** `[정적]`.

### 1.3 `CrossThreadFlags` 비트 칸 다시 셈

- 위 "요약 표" 아래 표 그대로. **17칸이 "0 을 씀"** 이 됐고, 바뀌는 비트 칸 이름은 `ThreadIoPriority` · `ThreadPagePriority` 뿐 (`ThreadInserted` 는 `+0x490 OR.LOCK`).
- 같은 방식이 `SameThreadPassiveFlags` · `SameThreadApcFlags` 에는 안 먹는다 — 그 DWORD 를 되쓰는 줄은 만들기 길에 없다 (sast/47 1장 그대로, 결과서).
- 범위 밖 14칸을 켜는 쪽 (`NtSetInformationThread` · `PsImpersonateClient` · `Dbgk…` 등) 은 이름만 그대로 남긴다 `[추측]` (sast/47 2.4).

### 2. `K3~P0` 이후 — `_ETHREAD` 5 · `_KTHREAD` 37 (결과서 질문 2)

**답: 42칸 중 40칸은 쓰는 줄, 2칸은 "범위 밖 길 `[추측]`"** `[정적]`. 칸마다 줄 · 값 · 사진은 짐 지도 3.13 · 3.14 의 새 줄 (근거 칸 `sast108`). 여기엔 구간마다 큰 줄기만.

- **`K3~P0`** (부모가 풀고 새 스레드가 처음 CPU 를 받음):
  - 준비 줄에 세울 때 (`KiDeferredReadySingleThread`, 경로 `①R…`): `WaitTime` (`+0x11f8`, KUSD `TickCount` 아래 32비트 `[0xfffff78000000320]`) · `AffinityVersion` (`+0x2a9` 비교 → `+0x1877 KiComputeThreadAffinity +0x212`, 전역 `KiCpuSetSequence`) · `WaitListEntry` = `SwapListEntry` (`+0x171f ~`, 준비 줄 고리) · `NextProcessor` (`+0x3b5`, 첫 값 0) · `State` 1 (`+0x122e`) · `_ETHREAD` `LastExpectedRunTime` (`+0x151d`) · `LastSoftParkElection*` 셋 (`+0x152b → +0x1f · +0x41 · +0x5b ~ +0x7e`).
  - 처음 CPU 받을 때: `KiSwapThread +0x5a` `CurrentRunTime` 0 · `+0x190` `State` 2 → `SwapContext +0x18` `Running` 1 · `+0x5bb` `ContextSwitches` +1.
  - 사진: `WaitTime` P0 `0x14dff` (부팅 22분 16초), `AffinityVersion` P0 `0x18`, `ContextSwitches` P0 1, `State` P0 2 `[관찰]` run44.
- **`P0~P1`** (새 스레드가 `PspUserThreadStartup` 에서 `LdrInitializeThunk` 로 가는 구간): 기다림 한 번 (`KeWaitForSingleObject +0x116 · +0x1d8 · +0x1dd · +0x1e2 · +0x7d2` → `KiCommitThreadWait +0x5b · +0xbf`) · 깸 (`KiTryUnwaitThread +0x1bb` `KernelWaitTime`) · **파일 일** (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2 — `IopReadFile` 꼬리 · `IopUpdateIrpTransferCount` · `NtQueryInformationFile` 등). 누가 이 길을 탔는지는 정적으로 못 정함 → 아래 2.7 · D2.
- **`P1~P1a`** (첫 시스템 콜들): `KiSystemServiceUser +0xd2` `SystemCallNumber` · `+0xcb` `FirstArgument` · `KiSystemServiceStart +0x0` `TrapFrame` · 스케줄러 공유 표 (`NtSetInformationThread +0xc2c → PspSchedulerSharedDataRegionSlotAllocate +0x1a4` `SchedulerSharedSwappablePage` · `+0x1ca` `SchedulerSharedSystemSlot` · `+0x1d1` `_ETHREAD.SchedulerSharedDataObject`). `WaitReason` 첫 0 아닌 값 (`0x1f`, `KiDispatchInterrupt +0x13e`).
- **`P1a~P1b`** · **`P1b~P2`**: `AutoBoostEntriesExhausted` (`KeAbPreAcquire +0x52`) · `ReadyTime` · `WaitMode` · `AdjustIncrement` · `DecayBoost` · `UserWaitTime` · `Alertable` · `WaitStatus` · `NextProcessor` 1 · 쓰기 카운터.
- **범위 밖 길 `[추측]` 2**:
  - `SavedApcStateFill` (`+0x258`, 43B) — 사진 `P0~P1` 에 목록 머리 둘이 자기 연결, `Process` 0. `KiStackAttachProcess` · `KiAttachProcess` 가 `ApcState` 를 옮겼다 되돌린 흔적으로 보임. 이 구간에서 누가 붙었는지 못 정함 → D2.
  - `ResourceIndex` (`+0x460`) — 실행 리소스 (`ERESOURCE`) 주인 표 안 이 스레드 자리 번호 (`ExpFindCurrentThread +0xf7` 등). 이웃 `+0x464 SchedulerSharedOffset` 과 이름만 비슷하고 **다른 일** → D4.

#### 2.6 `UserIdealProcessor` · `UserAffinity` 최종 출처, `KiQueueTebUpdateApc` 길

- 최종 출처 = **`KeStartThread`** (`+0x17c` · `+0x147 KiCopyAffinityEx`) `[정적]`. 첫 스레드 초기화 줄기에 다시 바꾸는 호출이 없다. 사진 `UserIdealProcessor` K3 1 → P2 1 `[관찰]` run44.
- **`KiQueueTebUpdateApc` 길은 이 줄기에서 안 탄다** `[정적: 호출 길 없음]`:
  - 호출자는 `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` 둘뿐.
  - 그 위 유저에서 들어오는 길은 `NtSetInformationThread` 의 친화도 · 이상적 프로세서 갈래 (`+0x12ac` · `+0xb29`) 뿐.
  - 초기화 줄기 ntdll `.c` 에서 본 `NtSetInformationThread` 클래스는 `5` · `0x38` · `0x39` 뿐 (결과서. 한계: `.c` 가 있는 11개 파일만 봄, `NtSetInformationProcess` 친화도 갈래는 안 봄) → 확인은 D5.
- 그래서 TEB `P0~P1` 다섯 칸은 `PspUserThreadStartup +0x94 → KeUpdateTebSchedulingPropertiesCurrentThread` 가 `KeStartThread` 값을 **읽어서** 한 번 쓴 그대로다 (run54).

#### 2.7 사진 구간과 어긋나는 것

1. **`P0~P1` 에 커널 파일 읽기 · 기다림이 있다** (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2 · `KernelWaitTime` 1 · `WaitBlockCount` 1, `WaitReason` 0) `[관찰]` run44.
   - 짐 지도 5.0 이 이 구간을 "`PspUserThreadStartup` → `PspInitializeThunkContext`" 로만 적었는데, 그 길의 `PspSetContextThreadInternal` 은 **지금 스레드면 APC 없이 바로 처리** (`+0xf7 CMP R13,R14` → `+0x131`) 라 기다림이 안 생긴다 `[정적]` (결과서).
   - 후보: `PspNotifyThreadCreation +0xda PfProcessCreateNotification` (프리페처) `[추측]`. 정적으로 못 정함 → **D2**.
2. **`SavedApcStateFill` 도 같은 구간에 처음 쓰인다** — 같은 사건 (프리페처가 프로세스에 붙어 읽기) 일 수 있음 `[추측]` → D2.
3. **`NextProcessor`**: 사진은 `P1b~P2` 인데 코드 첫 쓰기는 `K3~P0` (값 0 이라 사진엔 0).
4. **`WaitReason` · `WaitMode` · `WaitStatus` · `MiscFlags.Alertable`**: 첫 쓰기 (0) 는 `P0~P1` 의 첫 기다림이고, 사진이 읽은 구간은 **0 이 아닌 첫 값**이다.
5. **`WaitTime` P0 `0x14dff`** = 준비 때 `TickCount` (`KiDeferredReadySingleThread +0x11f8`) — run77 의 `[추측]` 이 닫혔다.
6. 나머지는 사진 구간과 코드 순서가 맞다.

### 3. `_KTRAP_FRAME` 25 — 가름만 (결과서 질문 3)

**답: 25칸 모두 "트랩 프레임 덮임"** `[정적]`. 입구마다 쓰는 칸이 다르고, **마지막 입구가 안 쓰는 칸은 앞 입구 값이 남는다**.

| 칸 | 어느 입구가 쓰나 | 사진 (처음 값) |
|---|---|---|
| `P1Home` · `P2Home` · `P3Home` · `P4Home` | 입구 코드는 안 씀. `KiSystemServiceStart +0x90c ~ +0x917` (win32k 첫 호출 앞뒤 임시 보관) + 아래 함수의 인자 홈 `[추측]` | P1a (P4Home 은 P1b) |
| `PreviousMode` · `InterruptRetpolineState` (같은 바이트) | `KiSystemServiceUser +0x4` (= `KiSystemCall64 +0x2a6`, 1) · `KiServiceInternal +0x49` | P1a 1 |
| `FaultIndicator` · `NmiMsrIbrs` (같은 바이트) | `KiPageFault +0x383` (읽기 0 · 쓰기 1 · 실행 8). `NmiMsrIbrs` 이름으로 쓰는 줄 못 찾음 `[공백]` | P1a 1 → P1b 8 |
| `Rax` · `R8` ~ `R11` | **모든 입구** (`KiSystemCall64 +0x7f` · `KiSystemServiceUser +0x2c ~ +0x38` · `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` · `KiPageFault +0x14 · +0x20 ~ +0x2c`). 만들 때 처음 값 0 은 `KxContextToKframes +0xdd · +0xfe ~ +0x11f` (run53) | P1a 부터 유저 값 |
| `GsBase` · `GsSwap` | `KiPageFault +0x2de · +0x2e1` · `KiNmiInterrupt +0x54` | P1a (= TEB 주소) |
| `Xmm1` | 모든 입구 (`KiSystemServiceUser +0x40` · `KiBreakpointTrap +0x2f8` · `KiPageFault +0x30b` …) | `dt` 가 `_M128A` 로만 보여 못 읽음 `[공백]` |
| `FaultAddress` · `ContextRecord` (같은 8B) | `KiPageFault +0x358` (`CR2`) · `KiNmiInterrupt +0xd2`. `ContextRecord` 이름으로 쓰는 줄 못 찾음 `[공백]` | P1a (= 매개변수 블록 주소) |
| `Rbx` · `Rdi` · `Rsi` · `NmiPreviousSpecCtrl` · `NmiPreviousSpecCtrlPad` | `KiSystemCall64 +0x55 · +0x5c · +0x63` (그림자 변형 `+0x6d · +0x74 · +0x7b`). `NmiPreviousSpecCtrl*` 이름으로 쓰는 줄 못 찾음 `[공백]` | P1a |
| `ErrorCode` · `ExceptionFrame` (같은 8B) | CPU 가 #PF · #GP 때 밀어 넣음. `KiSystemCall64` · `KiBreakpointTrap` 은 자리만 잡고 안 씀 | P1a 7 → P1b `0x14` |

- **첫 덮임 = `P1` 의 `int3` (`KiBreakpointTrap`)**, 24칸의 첫 값 = `P1~P1a` 의 syscall · 페이지 오류 입구 `[정적]` + 사진 `[관찰]` run44.
  - 요청서는 "첫 syscall — 사진 `P1` 직전" 이라 했는데 사진은 다르다: P1 프레임은 이미 `int3` 입구가 덮은 것 (`Rip` = `LdrInitializeThunk +1`) 이고, 이 24칸은 P1 에서도 0 이다.
  - (run113 보탬) 위 표 `Rax` · `R8 ~ R11` 줄과 안 어긋난다: `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` 은 **P1 에서도 이 다섯 칸을 쓰고, 쓴 값이 첫 유저 레지스터 0** 이다 (`.asm`). 같은 입구가 쓴 `Rcx` (`+0x1c`) · `Rdx` (`+0x20`) · `ExceptionActive` (`+0x14`, 1) 는 P1 사진에서 0 이 아니다 (`ms/logs/04_baggage-read_P1.txt:1061 ~ 1069`). 그래서 다섯 칸은 "안 써서 0" 이 아니라 "0 을 써서 0", 표의 "P1a 부터 유저 값" 은 "0 이 아닌 첫 값" — run113 "정정".
  - `P1a` · `P1b` · `P2` 도 `int3` 로 멈춘 사진이라, 그 사진의 `P1Home` · `PreviousMode` · `GsBase` · `FaultAddress` · `Rbx` · `ErrorCode` 등은 **`int3` 입구가 안 쓰는 칸에 남은 syscall · 페이지 오류 흔적**이다.
- LSTAR 가 `KiSystemCall64` 인지 `KiSystemCall64Shadow` (KVA 그림자 변형) 인지는 정적으로 못 정함 (`KiKvaShadow` 전역 · MSR 값) → D1.

### 4. 간접 호출 (결과서 질문 4)

- **칸을 직접 쓰는 간접 호출 0개** `[정적]`. 읽은 `.asm` 32개 안 `CALL reg` · `JMP reg` 는 5개, 모두 "고르는" 코드다.
  - `KiSystemServiceStart +0xfa` `JMP R11` — 인자 복사 점프표 (칸 안 씀).
  - `KiSystemServiceStart +0x193` · `+0xa24` · `+0xa7f` `CALL RAX` — **시스템 서비스 본체** (`KeServiceDescriptorTable` · `…Shadow` · `…Filter` 에서 고름). 칸을 쓰는 건 고른 `Nt*` 몫. 첫 스레드가 부른 `Nt*` 목록은 D3 (사진에 마지막 번호 `0xd` · `0xe` · `0x1d3` 만).
  - `KiDeferredReadySingleThread +0x17f7` `JMP RCX` — 함수 안 `switch` (밖으로 안 나감).

## 확인된 것

- `[정적]` `_ETHREAD` `K2~K3` 5칸 = `PspInsertThread` (`+0x2cd` · `+0x617 ~ +0x649` · `+0x6e3` · `+0x239 ~ +0x243`), `_KTHREAD` `K2~K3` 10칸 = `KeStartThread` 9 (`+0xc2` · `+0xc8` · `+0xce` · `+0x17c` · `+0x187` · `+0x1c1` · `+0x1dc` · `+0x1e9 ~ +0x2fe` · `+0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55`) + `KeSuspendThread +0x60`.
- `[정적]` `CrossThreadFlags` DWORD 는 `PspInsertThread +0x632` · `+0x649` 가 통째로 되쓴다 → 비트 칸 17개 "0 을 씀" (오케스트레이터 셈 결정).
- `[정적]` `UserIdealProcessor` · `UserAffinity` 최종 출처 = `KeStartThread`. `KiQueueTebUpdateApc` 는 첫 스레드 초기화 줄기에서 안 탄다 (호출 길 없음 — 실행 확인은 D5).
- `[정적]` `K3~P0` 칸은 `NtResumeThread → … → KiProcessThreadWaitList +0x101 → KiDeferredReadySingleThread` (준비) 와 `KiSwapThread → KiSwapContext → SwapContext` (첫 CPU) 가 쓴다.
- `[정적]` `WaitTime` P0 값 = 준비 때 `TickCount` (`KiDeferredReadySingleThread +0x11f8`) — run77 `[추측]` 닫힘.
- `[정적]` `SystemCallNumber` · `FirstArgument` · `TrapFrame` = `KiSystemServiceUser +0xd2` · `+0xcb` · `KiSystemServiceStart +0x0`.
- `[정적]` 스케줄러 공유 표 칸 셋 = `PspSchedulerSharedDataRegionSlotAllocate +0x1a4` · `+0x1ca` · `+0x1d1` (`NtSetInformationThread` 클래스 `0x39`). `KTHREAD +0x468` 과 `ETHREAD +0x6e0` 이 사진에서 같은 주소 (`…995fbe10`) `[관찰]` — 영역 객체와 그 안 첫 `_KSWAPPABLE_PAGE` 가 같은 주소로 보임 `[추측]`.
- `[정적]` 트랩 프레임 25칸 = 입구가 덮는 칸 (짐 아님). 첫 덮임은 `P1` `int3` `[관찰]` run44.
- `[정적]` 이 길에서 칸을 직접 쓰는 간접 호출 0개.

## 결과서와 다르게 적은 곳 · 보탠 것

- **경로 줄임을 바꿨다**: 결과서의 `①P` (= `PspInsertThread`) · `①S` (= `KeStartThread`) 는 짐 지도에 이미 다른 뜻으로 있다 (`①P…` = `PspAllocateProcess` run49, `①S…` = exe 섹션 사슬 run47). 그래서 짐 지도에선 **`①IT…`** (`①… → PspInsertThread (호출 +0x1015)`) · **`①ST…`** (`①IT… → KeStartThread (호출 +0x5dd)`) 로 적었다. 결과서 `R` · `SC` 는 **`①R…`** · **`②SC…`** 로 (3장 줄임 표).
- **`KeStartThread` 가 쓰는 "범위 밖 길" 5칸** (`SchedulingGroup` · `HardwareFeedbackClass` · `UserAffinityPrimaryGroup` · `AffinityPrimaryGroup` · `ThreadTimerDelay`): 결과서가 쓰는 줄을 확인했고 flow-writer 도 `.asm` (`+0x32b` · `+0x380` · `+0x156` · `+0x14f` · `+0x36c`) 을 봤다. 만들기 길 (`①ST…`) 안 줄이라 run52 기준이면 "nt 가 씀 (0 을 씀 / 입력값)" 으로 옮길 수 있다. 결과서 셈 (범위 밖 72) 은 `CrossThreadFlags` 만 다시 셌다. **(결정 → run52 기준으로 옮김)** — 오케스트레이터 기본값으로 3.14 새 줄 `+0x68` (0 을 씀 / 입력값 5칸) 로 옮겼다: `_KTHREAD` 아는 칸 144 → 149 · 범위 밖 72 → 67, 구조체 아는 칸 811 → 816 · 범위 밖 245 → 240, 아는 끝까지 0 322 → 327.
- 결과서 `SwapContext +0x5bb` 는 `.asm` 엔 조각 줄 `frag:FUN_1406b686f+0x28c` (`SwapContext.asm` 291줄) 로 있다. `FUN_1406b686f.asm` 파일은 따로 없다.
- 결과서의 `KiCopyAffinityEx` 는 `.asm` 표시가 `alias:KiCopyAffinityEx+1` 이지만 `pdb_aliases.py --lookup` 결과 이름 하나뿐이라 이름 그대로 썼다.
- `KeStartThread +0xb9` · `+0x1b7` 은 비트 OR 가 아니라 **`ThreadFlags` DWORD 통째 쓰기** (`MOV [RBX+0x78],ECX` / `EDX`) 다 (flow-writer `.asm`). 비트 3 · 8 은 run52 의 "0 을 씀 20" 칸이라 가름은 그대로.

## 정적과 동적이 다른 곳

- 위 2.7 (사진 구간과 어긋나는 것) 1 ~ 4 와 3 (첫 덮임). 고치지 않고 둘 다 적었다 — 짐 지도 2.1 표 (사진 구간) 는 그대로, 3장 줄 근거 칸에 "코드 첫 쓰기" 를 같이 적었다.
- **디버거가 붙어서 달라지는 점**: `P1` 이 `int3` 라 `KiBreakpointTrap` 이 첫 트랩 프레임을 만든다. 디버거 없이는 첫 syscall (`KiSystemCall64`) 이 첫 덮임일 수 있다 `[추측]`. `P1a` · `P1b` · `P2` 사진의 트랩 프레임 칸도 `int3` 입구 + 앞 입구 잔재다.

## 정정

- **run52** (`runs/03_sprint/run52-kthread-create.md` 167줄 · 295줄, 짐 지도 3.14 머리 run52 메모): "`ThreadFlags` 비트 13 (`SharedReadyQueueAffinity`) 은 `KeStartThread +0xb9` · `+0x1b7`" 은 틀렸다. `+0xb9` 는 비트 3 `DisableBoost`, `+0x1b7` 은 비트 8 `DisableQuantum` (둘 다 프로세스 값, DWORD 통째 쓰기), **비트 13 은 `KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55`** (`XOR.LOCK [RDX+0x78],0x2000`). → run52 문서 맨 위에 한 줄.
- **짐 지도 7.1 셈 (run111 몫)**: run111 이 `_PEB.NtGlobalFlag2` (끝까지 0) 를 아는 칸으로 옮기며 "아는 칸 중 끝까지 0" 을 304 그대로 뒀다 → 305 로 고치고 이번 17칸 · 셈 결정 5칸을 더해 327 (짐 지도 7.1). run111 문서엔 이 숫자가 없어 맨 위 줄은 안 넣었다.
- **요청서 전제** "트랩 프레임 첫 덮임 = 첫 syscall (사진 `P1` 직전)" 은 사진과 다르다 — 첫 덮임은 `P1` 의 `int3` (위 3). 요청서는 고치지 않는다 (중간 산출물).

## 이전 run 에서 닫은 것

- run52 "아직 확인 필요" **Q78d4** (`KeStartThread` 쓰는 줄 · 범위 밖 5칸 · 친화도 블록 비트맵 `[공백]`) → 닫힘 (5칸 셈은 위 "결과서와 다르게 적은 곳").
- run53 "아직 확인 필요" **Q78d4** (`PspInsertThread +0x2cd` · `+0x617 ~ +0x649` · `+0x6e3`, 트랩 프레임 `Rax` · `R8 ~ R11`) → 닫힘. 실패 경로 `+0x96f` · `+0x9d6` · `+0x2ca1d6` 는 범위 밖 (그대로).
- run54 "아직 확인 필요" **Q78d4** (`KiQueueTebUpdateApc` 길 · `UserAffinity` 등 마지막 값) → 닫힘 (안 탐 `[정적]`, 실행 확인 D5).
- run75 ~ run78 의 "Q78d4 에서 볼 것" (스케줄러 · 퀀텀 · 기다림 · 타이머 줄) → 처음 값 줄로 3.14 에 옮김. run77 `WaitTime` "P0 값은 다른 쓰는 줄 `[추측]`" → `[정적]` `KiDeferredReadySingleThread +0x11f8`.
- run81 "Q78d4 에서 볼 것" (`FaultAddress` · `FaultIndicator` ← `KiPageFault`) → 3.15 덮임 줄로.

## 아직 확인 필요

### 동적 (결과서 "동적분석으로 확인해야 할 것" D1 ~ D5 — 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건)

- **D2 (급함)** `P0~P1` 에 누가 파일을 읽고 (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2) 누가 attach 했나 (`SavedApcStateFill`): P0 에서 새 스레드 주소를 얻고 `ba w8 <스레드>+0x380` · `ba w8 <스레드>+0x258` → `k`. 기대 `PspUserThreadStartup → PspNotifyThreadCreation → PfProcessCreateNotification …` `[추측]`. 사진으로는 불가.
- **D1** 커널 전역 값 — `KiCpuSetSequence` (nt+`0xfc6c38`, 기대 `0x18`) · `KiCyclesPerClockQuantum` (nt+`0xfc5094`, 기대 `0x3f940a`) · `KiKvaShadow` (nt+`0x1201840`) · `KiDynamicHeteroCpuPolicyExpectedCycles` (nt+`0xfc5374`) · `KeSoftParkedQueueThreshold` (nt+`0xf22998`): 덤프에 있음 → Q79 사진 읽기에 추가. LSTAR (`rdmsr c0000082` — `KiSystemCall64` rva `0x6c1840` 인가 `…Shadow` rva `0xbcb200` 인가) 는 라이브만.
- **D3** `SystemCallNumber` `0xd` · `0xe` · `0x1d3` 은 어느 `Nt*` 인가 + `P1~P1a` 첫 스레드 syscall 목록: `ba w8 <스레드>+0x80` 또는 `nt!KiSystemServiceStart` 조건부 멈춤점, `KiServiceTable` 로 풀기 (sast/51 `ms/ghidra/ntoskrnl.refs.sast51_KiServiceTable.stdout` 가 표 위치). 사진은 마지막 셋만.
- **D4** (낮음) `ResourceIndex` 가 처음 1 이 되는 리소스: `ba w1 <스레드>+0x460` → `k`.
- **D5** (낮음) `KiQueueTebUpdateApc` 가 `P0 ~ P2` 사이 한 번도 안 불리는지: `bp nt!KiQueueTebUpdateApc` (현재 프로세스 = test.exe 조건).

### 정적 (결과서 "다음 요청 후보")

- ① `PfProcessCreateNotification` 본문 (rva `0x944508`) — 프로세스에 붙기 · 파일 읽기가 있나 (D2 의 정적 짝). 이미 `PfSnPopulateReadList +0x85` · `PfSnAppLaunchScenarioControl +0xb9` 가 `KiStackAttachProcess` 를 부른다는 것은 앎 (결과서 훑기).
- ② `KiResumeThread` + `KiTryUnwaitThread` 의 초기화 상태 갈래 — `K3~P0` 의 `State` 0 → (1/7) → 2 중간 줄.
- ③ `PspSchedulerSharedDataRegionSlotAllocate` 마무리 (run110 정적 ① 과 겹침) — 표 한 칸 크기 · 스레드마다 칸 나누기 · `Retrieve` (rva `0x703db4`).
- ④ (낮음) `ExpFindCurrentThread` · `ExpFindEmptyEntry` — `ResourceIndex` 첫 줄.
- ⑤ `KeSelectIdealProcessor` — `UserIdealProcessor` 1 의 계산 본문 (에뮬레이터가 CPU 수를 어떻게 줄지).
- ⑥ 동적 D1 · D2 를 한 dast 로 묶기.
- (flow-writer) **셈 결정 하나** (결정 → run52 기준으로 옮김): `KeStartThread` 가 쓰는 "범위 밖 길" 5칸을 "nt 가 씀" 으로 (위 "결과서와 다르게 적은 곳").

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- 질문 1: `ThreadListEntry` = **맞음** (`Src/nt/Process.cpp` 줄 1687). `KeStartThread` · `KeSuspendThread` 는 "안 함" (줄 1682 · 1692) → `Priority` · `QuantumTarget` · `UserIdealProcessor` … 아홉 칸 · `CrossThreadFlags` 우선순위 비트 · `KernelStackReference` 는 **아직 없음 (빈 칸, 틀린 건 아님)**. `Process.cpp` 줄 2046 ~ 2056 이 이 요청 (Q78d4) 을 기다리고 있다 — `UserIdealProcessor` 1 · `UserAffinity` = 프로세스 `Affinity` 사본이 그 값.
- 질문 2: **확인 필요** (`Scheduler.h` 줄 9 의 `QuantumTarget 0x7f2814` 는 맞음, `Scheduler.cpp` 는 결과서가 안 읽음).
- 질문 3: **맞음 (범위 안)** — `Src/nt/Syscall.cpp` 줄 925 ~ 949 `KiSystemCall64` 모델. `KTHREAD +0x80 / +0x88 / +0x90` 은 `Src/nt` 에 없음 (읽는 쪽이 생기면 `KiSystemServiceUser +0xcb · +0xd2`, `KiSystemServiceStart +0x0` 을 따라야 함 — 확인 필요).
- 질문 4: **맞음** (시스템 서비스 고르기를 자기 표로).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntoskrnl pdb 줄, 근거 로그 run112, Ghidra 가 함수로 안 잡은 곳 `KiSystemCall64Shadow`), 2.1 머리 · `PspInsertThread` (run44 `[관찰]` 줄의 `KeStartThread` 몫 `[추측]` → `[정적]`), 2.2 머리 · 시스템 콜 입구 절 · `KeStartThread` (본문 `[추측]` 줄 → `[정적]`) · `KiResumeThread`, 2.3 머리 · `PspUserThreadStartup`, 4장 Q78d4 넷 줄 `(해결됨)` + 새 줄 넷 (동적 · 정적 · PE_emulator · 셈 결정), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): 기준 · 쓰는 말 ("트랩 프레임 덮임"), 1장 · 2장 · 2.1, 3장 줄임 넷 (`①IT…` · `①ST…` · `①R…` · `②SC…`), 3.3 머리 메모 `(해결됨)`, **3.13 · 3.14 · 3.15 다시 씀** (새 줄 11 · 30 · 5 — 3.14 는 `KeStartThread` 5칸 0 을 씀 줄 포함, 고친 줄 3 · 3 · 0 — 3.13 아무도 안 씀 · 범위 밖 · `UpdateTebApc` 바뀜 바이트 `[추측]` → `[정적]`, 3.14 `Header*` · `ThreadFlags` 20비트 · 범위 밖 70), 머리 메모 `(해결됨 → run112)` (3.13 둘 · 3.14 다섯 · 3.15 둘), 4장 짝 1줄 새로 (TEB 이상적 프로세서 · 친화도 ↔ `_KTHREAD`) + 스케줄러 공유 칸 짝 근거, 5장 ① · ②, 6장 부품 2 (`KiCopyAffinityEx` · `KeAbPreAcquire`), 7장 머리 · 7.1 (아는 끝까지 0 304 → 327 — run111 몫 1 을 안 더한 것 정정 포함) · 7.2 · 7.4, 새 7.43, 8장.

## 디버거 메모 / 도구 메모

- **`KiSystemCall64` · `KiSystemCall64Shadow` 는 PDB 에만 함수로 있고 Ghidra 함수가 아니다** — `export ^KiSystemCall64$` 가 0개. 요청서에 "없으면 뽑기" 대신 "`objdump` 로 본다" 로 쓸 것 (결과서). → ghidra-headless-export 쪽 함정 목록에 넣자고 보고 (run56 과 같은 사정이 두 번째).
- 결과서 제안 (하네스, 직접 안 고침): ① PDB 에만 있는 함수 범위를 `.asm` 으로 만드는 옵션, ② `field_writers.py` (칸 오프셋으로 쓰는 줄 TSV, 트랩 프레임 입구 `RBP = 프레임 + 0x80` 보정, callers 표), ③ `04_baggage-read_*.txt` 의 `02c ETHREAD_RAW` 를 칸 이름별 구간 표로 읽는 작은 스크립트.
- 사진 쪽 함정 (windbg-kd-runbook 후보): **`int3` 로 멈춘 사진의 트랩 프레임은 `int3` 입구가 쓴 칸 + 앞 입구 (syscall · 페이지 오류) 잔재가 섞여 있다.** 트랩 프레임 칸을 "이 순간의 짐" 으로 읽지 말 것.
