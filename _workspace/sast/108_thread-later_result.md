# 108 thread-later — 정적 분석 결과 (Q78d4)

- 요청서: `_workspace/sast/108_thread-later.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) — **기준과 같음.** 사진 로그 `ms/logs/04_baggage-read_*.txt` 의 `!lmi nt` 와도 같다 (sast/46 에서 확인한 줄 그대로).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음, 시작할 때 Ghidra 프로세스 없음). **ntdll 은 건드리지 않았다** (sast/107 몫). ntdll 쪽은 이미 있는 `ms/decomp/ntdll/*.c` 를 grep 만 했다 (`NtSetInformationThread` 호출 자리).
- **새로 뽑은 함수 10개, 전부 depth 0, 두 번에 나눠** (`ms/ghidra/ntoskrnl.export.sast108a.stdout` 6개 · `…108b.stdout` 4개, 각 8초 안팎, `ms/decomp/ntoskrnl/`·`ms/disassemble/ntoskrnl/`):
  - 요청서 표 "필요하면" 줄 (run54 넘김): `KiSetIdealProcessorThread` · `KiSetAffinityThread` (`KiQueueTebUpdateApc` 의 호출자 둘).
  - 요청서 질문 1·2 가 가리키는 callee: `KiUpdateSharedReadyQueueAffinityThread` (`KeStartThread +0x344`, `ThreadFlags` 비트 13) · `KiUpdateSoftParkElectionStatisticsOnInsertion` (`_ETHREAD` `K3~P0` 넷) · `KiComputeThreadAffinity` (`AffinityVersion`) · `PspSchedulerSharedDataRegionSlotAllocate` (`P1~P1a` 의 `SchedulerShared*`) · `KeSelectInitialIdealProcessorForThread` · `KiCopyAffinityEx` · `KiInitializeForegroundBoostThread` · `KiAdjustProcessIdealProcessorSetsForThreadCreation` (넷 모두 `KeStartThread` 가 부름, 값 출처 확인용).
  - **요청서가 "없으면 뽑기" 라 한 `KiSystemCall64` 는 못 뽑았다**: PDB 에는 함수로 있으나 (rva `0x6c1840`) Ghidra 가 함수로 안 잡아 `_index.tsv` 에 없고 `pdb_aliases.py --where ntoskrnl.exe KiSystemCall64` 가 "어느 함수 범위에도 안 듦" 이라 한다. 같은 사정: `KiSystemCall64Shadow` (rva `0xbcb200`). → **objdump 로만 봤다** (아래).
- 이미 `.c`/`.asm` 이 있어서 **읽기만 한 것** (다시 안 뽑음): `PspInsertThread` · `KeStartThread` · `KiSwapThread` · `KiCommitThreadWait` · `KiDeferredReadySingleThread` · `SwapContext` · `KeWaitForMutexObject` (= PDB `KeWaitForSingleObject`) · `KiTryUnwaitThread` · `KiProcessThreadWaitList` · `KiInsertDeferredReadyList` · `KeSuspendThread` · `KeSetEvent` · `KiSystemServiceUser` · `KiSystemServiceStart` · `KiBreakpointTrap` · `KiPageFault` · `KiExceptionDispatch` · `KxContextToKframes` · `PspUserThreadStartup` · `KiQueueTebUpdateApc`·`KiUpdateTebApc` (호출자만 봄).
- **Ghidra 로 안 뽑고 objdump 로만 본 곳** (함수+오프셋으로만 적는다): `KiSystemCall64 +0x0 ~ +0x2a2` (`KiSystemServiceUser` 가 `KiSystemCall64 +0x2a2` 에서 이어짐), `KiSystemCall64Shadow +0x6d ~ +0x97`, `KiNmiInterrupt`·`KiMcheckAbort` 입구 쪽, `KeAbPreAcquire +0x52`, `IopUpdateIrpTransferCount`, `IopWriteFile +0x1ed`, `IopSynchronousServiceTail`, 그리고 커널 전체 `[reg+칸]` 쓰기 훑기. 근거 파일은 scratchpad `nt.dis` (= `objdump -d -M intel` ntoskrnl 전체 — 이번에 새로 안 만들고 읽기만 함. 맞는 파일인지 `KiSystemCall64` 20줄을 `objdump --start-address` 로 새로 뽑아 같은 줄인지 확인함).
- **이름 확인**: 이 결과서의 함수 이름 전부를 `ms/decomp/ntoskrnl/_index.tsv` 로 한 번 더 확인했다 (끝에 "이름 확인 결과"). 전역 변수는 `pdb_aliases.py --where`·`.asm` 주석 주소로 확인.
- **버전 확인: 일치.** `_calls.tsv` — `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`, `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines` (사진 K3 의 자리). 그리고 **사진 값과 코드가 칸마다 맞는다** (아래 표의 "사진" 열): `QuantumTarget` K3 `0x7f2814` = 프로세스 `QuantumReset` 2 × 전역 `KiCyclesPerClockQuantum` (코드 `+0x1c7 IMUL`), `Priority`·`BasePriority` 8 = 프로세스 `BasePriority`, `CrossThreadFlags` K3 `0x5402` = `ThreadInserted` + 우선순위 두 칸 (IO 2 · 페이지 5), `ThreadListEntry` K3 = `_EPROCESS +0x370` 자기 연결 한 칸, `SuspendCount` K3 1, `WaitTime` P0 `0x14dff` = KUSD `TickCount` (부팅 22분 16초와 맞음), `AffinityVersion` P0 `0x18` = 전역 `KiCpuSetSequence`. 바이너리와 타깃 VM 이 같다고 볼 수 있다.
- **훑기 한계 (한 줄)**: 이번 `[reg+칸]` 훑기는 그 레지스터가 `gs:[0x188]` (지금 도는 스레드) 에서 온 건지 인자로 받은 스레드인지 가리지 않는다 — `.asm` 줄을 직접 읽은 곳만 "누구 스레드 칸" 을 적었고 (`KeStartThread`·`PspInsertThread` = 인자 `RCX`/`R14` 새 스레드, `KeWaitForSingleObject`·`KiSwapThread`·`IopUpdateIrpTransferCount` = `gs:[0x188]` 지금 스레드), 나머지는 오프셋 숫자 일치라 오탐이 있을 수 있다 `[추측]`. `LEA` 로 칸 주소를 만들고 그 포인터로 쓰는 줄 (리스트 삽입) 은 훑기에 안 잡혀서 해당 칸은 `.asm` 을 읽어 따로 찾았다.

## 스크립트 문제

없다. (`run_ghidra.sh export` 두 번, `pdb_struct.py`·`pdb_aliases.py --where` 여러 번 — 모두 문제없이 돌았다.) 제안 하나만: `export` 는 `_index.tsv` 에 있는 함수만 뽑는다. PDB 에 `[function]` 으로 있는데 Ghidra 가 함수로 안 잡은 입구 (`KiSystemCall64`·`KiSystemCall64Shadow`) 는 `^이름$` 으로 0개라, 요청서에 "없으면 뽑기" 라 써도 못 뽑는다. `pdb_aliases.py --where` 가 "어느 함수 범위에도 안 듦" 이라 하면 그 주소부터 크기만큼 `.asm` 을 만들어 주는 옵션 (또는 `ExportFunctions` 가 PDB 공개 심볼 시작 주소로 함수를 새로 만드는 길) 이 있으면 좋겠다.

## 판단 기준 (한 줄)

run52·53 (sast/46·47·48) 과 같은 가름이다. 셈 기준: **명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만 "0 을 씀"**, 몸통 `memset` 만으로는 안 센다.

- **아는 칸** = 쓰는 줄 (함수 + `+0x`) 을 `.asm` 에서 찾았고 사진 값과 맞춰 본 칸. 카운터·상태 칸은 "처음 값 줄 + 뒤에 바꾸는 쪽 이름" 까지.
- **범위 밖 길에서만 씀 `[추측]`** = 이 길 (만들기 → 처음 CPU → 기다림 → 깨움 → 첫 syscall) 에서 쓰는 줄을 못 찾았고, 이름 있는 다른 함수가 쓰는 것으로 보이는 칸.
- **트랩 프레임 덮임** = 입구 (`KiSystemCall64`·`KiBreakpointTrap`·`KiPageFault` 류) 가 그때그때 덮는 칸. 짐이 아니라 가름만.
- (이 요청 82칸에는 "0 을 씀"·"조건부 안 탐"·"아무도 안 씀" 이 없다 — 전부 K2 뒤에 값이 생긴 칸이라서. 다만 `CrossThreadFlags` 비트 칸 19개를 다시 세는 문제가 있어 아래 1.3 에서 따로 센다.)

## 요약 표 (82칸 → ?)

| | 칸 | 아는 칸 (nt 가 씀) | 범위 밖 길 `[추측]` | 트랩 프레임 덮임 | 여전히 모름 |
|---|---|---|---|---|---|
| `_ETHREAD` `K2~K3` | 5 | **5** | — | — | 0 |
| `_ETHREAD` `K3~P0` | 4 | **4** | — | — | 0 |
| `_ETHREAD` `P1~P1a` | 1 | **1** | — | — | 0 |
| `_KTHREAD` `K2~K3` | 10 | **10** | — | — | 0 |
| `_KTHREAD` `K3~P0` | 9 | **9** | — | — | 0 |
| `_KTHREAD` `P0~P1` | 9 | **8** | **1** (`SavedApcStateFill`) | — | 0 |
| `_KTHREAD` `P1~P1a` | 7 | **6** | **1** (`ResourceIndex`) | — | 0 |
| `_KTHREAD` `P1a~P1b` | 6 | **6** | — | — | 0 |
| `_KTHREAD` `P1b~P2` | 6 | **6** | — | — | 0 |
| `_KTRAP_FRAME` `P1~P1a` | 24 | — | — | **24** | 0 |
| `_KTRAP_FRAME` `P1a~P1b` | 1 | — | — | **1** | 0 |
| **합** | **82** | **55** | **2** | **25** | **0** |

- `_ETHREAD` 10 → 모름 **0**, 아는 칸 48 → **58**. `_KTHREAD` 47 → 모름 **0**, 아는 칸 99 → **144** (= 99 + 45), 범위 밖 70 → **72** (+2 — 이 둘은 지금까지 "모름" 이던 칸이라 다른 줄에서 옮기는 게 아니라 새로 더하는 것). `_KTRAP_FRAME` 모름 25 → **0** (트랩 프레임 덮임 25, 짐 아님).
- 아는 칸 55 중 **카운터·"값이 계속 바뀌는" 칸** 은 14개: `CycleTime` · `CurrentRunTime` · `ContextSwitches` · `WaitTime` · `ReadOperationCount` · `OtherOperationCount` · `ReadTransferCount` · `WriteOperationCount` · `WriteTransferCount` · `KernelWaitTime` · `UserWaitTime` · `ReadyTime` · `LastExpectedRunTime` · `QuantumTarget` (뒤 값). 이 칸들은 "쓰는 함수 이름 + 단계" 까지만 적었다 (요청서 질문 2 마지막 줄).
- **`CrossThreadFlags` 비트 칸 19개를 다시 세면** (1.3): 조건부 2 (그대로) · **0 을 씀 17** (= 아무도 안 씀 3 + 범위 밖 14). 바뀌는 칸 이름 = **`ThreadIoPriority` (비트 9~11) · `ThreadPagePriority` (비트 12~14)** 둘뿐 (`ThreadInserted` 는 앞 줄 `+0x490`). 어느 쪽으로 셀지는 오케스트레이터·flow-writer 가 정한다 (run47·run49·run50·run52 방식대로면 17칸은 "0 을 씀").

## 한눈에

- **K2~K3 열다섯 칸은 두 함수가 거의 다 쓴다**: `PspInsertThread` (`_ETHREAD` 다섯 — 스레드 목록 꼬리 달기·`CrossThreadFlags` 우선순위·`KernelStackReference`) 와 `KeStartThread` (`_KTHREAD` 열 중 아홉 — 우선순위·퀀텀·이상적 프로세서·`WobPriority`·목록 달기). 열째 `SavedApcState*` 는 `SuspendCount` 한 바이트 (`KeSuspendThread`).
- **run52 의 `KeStartThread` 줄 하나가 틀렸다**: `ThreadFlags.SharedReadyQueueAffinity` (비트 13) 는 `KeStartThread +0xb9 · +0x1b7` 이 아니라 `+0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` 가 쓴다. `+0xb9` 는 비트 3 `DisableBoost` (프로세스 `DisableBoost` 값), `+0x1b7` 은 비트 8 `DisableQuantum` (프로세스 `DisableQuantum` 값) 이다 (정정 1).
- **`UserIdealProcessor`·`UserAffinity` 의 최종 출처는 `KeStartThread`** (`+0x17c` · `+0x134 / +0x147 KiCopyAffinityEx`). 첫 스레드 초기화 줄기에는 이 값을 다시 바꾸는 호출이 없다 (사진 `UserIdealProcessor` K3 1 → P2 1). **`KiQueueTebUpdateApc` 길은 이 줄기에서 타지 않는다** — 호출자 `KiSetIdealProcessorThread`·`KiSetAffinityThread` 로 들어오는 길이 `NtSetInformationThread` 의 친화도·이상적 프로세서 갈래뿐이고, 초기화 줄기의 `NtSetInformationThread` 호출 클래스는 `5`·`0x38`·`0x39` 만 봤다 (질문 2 답 참고).
- **K3~P0 는 둘로 나뉜다**: 부모가 `NtResumeThread` 로 풀면 (`KeResumeThread → KiResumeThread / KiExitDispatcher → KiProcessThreadWaitList → KiDeferredReadySingleThread`) 새 스레드 칸 `WaitTime`·`AffinityVersion`·`WaitListEntry`·`NextProcessor`·`LastExpectedRunTime`·`LastSoftParkElection*` 가 쓰이고, 그다음 CPU 를 받을 때 (`KiSwapThread → SwapContext`) `State`·`Running`·`ContextSwitches` 가 쓰인다.
- **사진과 어긋나는 칸이 셋 있다** (아래 "사진 구간과 어긋나는 것"): ① `P0~P1` 에 **커널이 파일을 한 번 읽었다** (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2 · 첫 기다림 `KernelWaitTime` 1) — 누가 읽었는지 정적으로 못 정함 (`PspNotifyThreadCreation +0xda → PfProcessCreateNotification` 후보 `[추측]`), ② `SavedApcStateFill` 이 같은 구간에 처음 쓰임 (attach 계열 `[추측]`), ③ 트랩 프레임은 **첫 syscall 이 아니라 `P1` 의 `int3` (`KiBreakpointTrap`) 에서 처음 덮이고**, 24칸의 첫 값은 `P1~P1a` 사이 syscall·페이지 오류 입구에서 온다.
- **간접 호출**: 이 길의 `.asm` 32개에서 `CALL reg`·`JMP reg` 는 **5개** — `KiSystemServiceStart +0xfa` (`JMP R11`, 인자 복사 점프표), `+0x193`·`+0xa24`·`+0xa7f` (`CALL RAX`, 시스템 서비스 본체를 `KeServiceDescriptorTable` 에서 골라 부름), `KiDeferredReadySingleThread +0x17f7` (`JMP RCX`, 함수 안 `switch`). 칸을 쓰는 건 syscall 표 안의 `Nt*` 몫 (질문 4).

## 질문별 답 [정적]

경로 약어 (`(호출 +0x..)` = 부르는 자리, ret 아님):

- `①` = `NtCreateUserProcess`
- `①P` = `① → PspInsertThread (호출 +0x1015)`
- `①S` = `①P → KeStartThread (호출 +0x5dd)`
- `R` = 부모 `NtResumeThread → PsMultiResumeThread (+0x96) → KeResumeThread (+0x20) → KiResumeThread (+0x82)` · `KeResumeThread +0xa0 → KiExitDispatcher +0x41 → KiProcessThreadWaitList +0x101 → KiDeferredReadySingleThread` (`_calls.tsv`)
- `U` = 새 스레드 `PspUserThreadStartup` (⓪, 사진 `P0`)
- `SC` = 새 스레드 시스템 호출 `KiSystemCall64 → KiSystemServiceUser (+0x2a2) → KiSystemServiceStart`

### 1. `K2~K3` — `_ETHREAD` 5 · `_KTHREAD` 10

**답: 15칸 모두 쓰는 줄을 찾았다 (`_KTHREAD` 하나는 `SuspendCount` 바이트).** `PspInsertThread` 가 `_ETHREAD` 칸 넷 (`ThreadListEntry`·`CrossThreadFlags` 와 우선순위 비트 둘) + `KernelStackReference`, `KeStartThread` 가 `_KTHREAD` 아홉.

#### 1.1 `_ETHREAD` 5 (PspInsertThread, 인자 `R14` = 새 스레드)

| 칸 | 쓰는 줄 | 값의 출처 | 사진 |
|---|---|---|---|
| `+0x578` `ThreadListEntry*` | `①P +0x6e3` (`LEA RCX,[R14+0x578]`, 칸 주소) → `+0x239` (`[RCX]` = 머리) · `+0x23c` (`[RCX+8]` = 옛 꼬리) · `+0x240` (옛 꼬리 `Flink` 를 이 칸으로) · `+0x243` (머리 `Blink` 를 이 칸으로). 앞서 `+0x6bb ~ +0x6d5` 로 프로세스 푸시락 (`_EPROCESS +0x5e0`) 잡음 | **프로세스 칸** `_EPROCESS +0x370 ThreadListHead` (`LEA RAX,[R15+0x370]` `+0x6ea`) 꼬리에 달기 | K3 `[…b3f0 − …b3f0]` = `EPROCESS +0x370` 자기 연결 (첫 스레드라 한 칸짜리) ✓. 뒤에 바뀜 `P1a~P1b` (둘째 스레드가 달리며 `+0x240` 이 이 칸의 `Flink` 를 고침 — `…d15f8`) |
| `+0x5a0` `CrossThreadFlags*` | `①P +0x632` (`MOV [R14+0x5a0],ECX`) · `+0x649` (`MOV [R14+0x5a0],EAX`) — DWORD 를 읽어 (`+0x617`) 비트 9~14 만 바꿔 **통째로 두 번 되씀**. 앞서 `+0x490 OR.LOCK 0x2` (`ThreadInserted`, 아는 칸) | 아래 두 줄 | K3 `0x5402` (= 비트 1 + 비트 10 + 비트 12 + 비트 14), P2 까지 그대로 ✓ |
| `ThreadIoPriority` (비트 9~11) | `+0x61e` `MOV ECX,[R15+0x1f4]` → `+0x625 SHR 0x12` → `+0x62a AND 0xe00` → `+0x632` | **프로세스 칸** `_EPROCESS +0x1f4 Flags.DefaultIoPriority` (27:3) 를 9 로 옮김 | K3 `0y010` (= 2) ✓ |
| `ThreadPagePriority` (비트 12~14) | `+0x63b XOR EAX,[R15+0x1f0]` → `+0x642 AND 0x7000` → `+0x649` | **프로세스 칸** `_EPROCESS +0x1f0 Flags2.DefaultPagePriority` (12:3) | K3 `0y101` (= 5) ✓ |
| `+0x5ec` `KernelStackReference` | `①P +0x2cd` (`MOV [R14+0x5ec],R13D`) — 바로 앞 `+0x2c8 ObReferenceObjectExWithTag(스레드, 2, 'Dflt')` | **상수 1** (`R13D` = `+0xbc MOV R13D,1`) | K3 1 ✓ (P2 까지 1). 뒤에 바꾸는 쪽 (이름만): `PsReferenceKernelStack` (`lock inc`), `PsDereferenceKernelStack` · `PspReaper +0x3b` (`lock xadd`) |

#### 1.2 `_KTHREAD` 10 (KeStartThread, 인자 `RBX` = 새 스레드, `RDI` = 프로세스)

`①S` 안에서: `+0x99 KiAcquireProcessLockExclusive` → `+0xa1 KiAdjustProcessIdealProcessorSetsForThreadCreation` → 아래 쓰기들 → `+0x1e0 KiAcquireKobjectLockSafe` (프로세스 목록 잠금) → `+0x38d KiReleaseProcessLockExclusive`.

| 칸 | 쓰는 줄 | 값의 출처 | 사진 |
|---|---|---|---|
| `+0x20` `QuantumTarget*` | `KeStartThread +0x1dc` (`MOV [RBX+0x20],RAX`) | **프로세스 칸 × 전역**: `[RDI+0x91]` (`_KPROCESS.QuantumReset`, `+0x1ba`) × 전역 `KiCyclesPerClockQuantum` (`+0x1c7 IMUL`, nt+`0xfc5094`) | K3 `0x7f2814` = 2 × 4,166,666 ✓. 뒤에 바뀜 `P1a~P1b` (`KiQuantumEnd +0x49b` · `KiDeferredReadySingleThread +0x260` · `KeYieldExecution +0x1b4`, run76) |
| `+0x78` `ThreadFlags.SharedReadyQueueAffinity` (비트 13) | `KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` (`XOR.LOCK [RDX+0x78],0x2000`, 지금 값과 계산 값이 다를 때만 뒤집음) | **계산**: 스레드 `Affinity` 비트맵 (`[+0x240] → [그룹]`) 이 준비 줄의 CPU 마스크 (`PRCB +0x8e40`, 이름 없는 칸) 를 모두 덮으면 1 (`+0x23 JZ`·`+0x3d AND`·`+0x43 SETZ`) | K3 1 ✓ (P2 까지 1) |
| `+0xc3` `Priority*` | `+0xc8` (`MOV [RBX+0xc3],AL`) | **프로세스 칸** `[RDI+0x90]` (`_KPROCESS.BasePriority`, `+0xbc`) | K3 8 ✓. 뒤에 바뀜 `P1a~P1b` (9): `KiDeferredReadySingleThread +0x1ae` · `KiUpdateThreadPriority +0x41` · `KiDirectSwitchThread +0x2c8 / +0x5aa` |
| `+0xc4` `UserIdealProcessor` | `+0x17c` (`MOV [RBX+0xc4],R15D`) — 입력 `R8` ≠ 0 이면 `+0x20c R15D=[R15]` 로 입력값, 그때 `+0x20f BTS.LOCK ThreadFlags 비트 0` (`UserIdealProcessorFixed`) | **계산**: `+0x173 KeSelectInitialIdealProcessorForThread(프로세스, &그룹)` 의 결과 (프로세스 `+0xb8` 의 이상적 프로세서 묶음 → `KeSelectIdealProcessor`, 이름만) | K3 1 ✓. test.exe 는 입력 `R8` = 0 (사진 `ThreadFlags` 비트 0 = 0, K3 `0x22040`). P2 까지 그대로 → **이 값이 첫 스레드의 최종** |
| `+0x233` `BasePriority` | `+0xc2` (`MOV [RBX+0x233],AL`) | **프로세스 칸** `_KPROCESS.BasePriority` (위와 같은 `AL`) | K3 8 ✓ |
| `+0x24c` `IdealProcessor` | `+0x187` (`MOV [RBX+0x24c],R15D`) | 위 `UserIdealProcessor` 와 같은 `R15D` | K3 1 ✓. 뒤에 쓰는 쪽 (이름만): `KiComputeThreadAffinity +0x2ad` (처음 준비될 때 다시 계산 — 값은 그대로), `KiSetAffinityThread +0x14b`, `KiSetIdealProcessorThread +0xac` |
| `+0x258` `SavedApcState*` (48B 공용체) | **K2~K3 에 바뀐 바이트는 꼬리의 `SuspendCount` (+0x284) 하나**: `①P +0x2e0 → KeSuspendThread +0x60` (`MOV [RDI+0x284],AL`, 앞 `+0x4a` 읽기) — 조건: 만들기 플래그 비트 0 (`PspInsertThread +0x2d4 TEST R13B,AL`, 플래그 포인터 `[RSP+0xe8]`). 목록 머리 쪽 43바이트 (`SavedApcStateFill`) 는 `P0~P1` (1.2 아래 표 밖 — 질문 2) | **상수** (+1 → 1, `CREATE_SUSPENDED` 길) | K3 `[0x280]` = `0x1_00000000` → 바이트 +0x284 = 1 ✓ (P0 에서 0 — `NtResumeThread` 가 내림) |
| `+0x28b` `QuantumReset` | `+0x1c1` (`MOV [RBX+0x28b],AL`, `+0x1ba MOVZX EAX,[RDI+0x91]`) | **프로세스 칸** `_KPROCESS +0x91 QuantumReset` | K3 2 ✓ |
| `+0x2f8` `ThreadListEntry*` | `+0x1e9` (`LEA RAX,[RBX+0x2f8]`) → `+0x2f4` (`[RAX]` = 머리) · `+0x2f7` (`[RAX+8]` = 옛 꼬리) · `+0x2fb` (옛 꼬리 `Flink`) · `+0x2fe` (머리 `Blink`) — 프로세스 목록 잠금 `+0x1e0 KiAcquireKobjectLockSafe` 안 | **프로세스 칸** `_KPROCESS +0x30 ThreadListHead` (`LEA RSI,[RDI+0x30]`, `+0x183`) 꼬리에 달기. `+0x1f9 INT 0x29` 는 연결 망가짐 검사 | K3 `[…b0b0 − …b0b0]` = `KPROCESS +0x30` 자기 연결 ✓. 뒤에 바뀜 `P1a~P1b` (둘째 스레드 달림) |
| `+0x31b` `WobPriority` | `+0xce` (`MOV [RBX+0x31b],0x20`) | **상수** `0x20` | K3 32 ✓ |

- 같은 함수의 나머지 쓰기 (요청서 칸 밖): `+0xb9` ThreadFlags 비트 3 `DisableBoost` ← 프로세스 `DisableBoost` (`[RDI+0x88]` 비트 1, `SHL 2`), `+0x1b7` 비트 8 `DisableQuantum` ← 프로세스 비트 2 (`SHL 6`), `+0x1d3` 비트 5 `QuantumDonation` 지움, `+0x20f` 비트 0 켬 (입력이 있을 때만), `+0x14f`·`+0x156` `AffinityPrimaryGroup`·`UserAffinityPrimaryGroup`, `+0x32b` `SchedulingGroup`, `+0x339 / +0x353` `Header` 비트 18·23, `+0x36c` `ThreadTimerDelay`, `+0x380` `HardwareFeedbackClass` (sast/46 이 "범위 밖 길 5칸" 으로 센 것 — 여기서 **쓰는 줄 확인**), 친화도 블록 비트맵 `+0xec` (지역 사본 ← 프로세스 `[RDI+0x50]`) → `+0x134` (`Affinity`) · `+0x147` (`UserAffinity`) `KiCopyAffinityEx` ← **sast/46 의 `[공백]` ("친화도 블록 비트맵이 채워지는 때") 닫힘** `[정적]`.
- `+0x1e0 ~ +0x1f9`: 목록 끝 연결이 깨졌는지 검사 (`CMP [RCX],RSI`) 후 `INT 0x29` — 정상 길은 안 탄다.

#### 1.3 `CrossThreadFlags` 비트 칸 19개 — "0 을 씀" 으로 다시 셀 수 있나

**답: 셀 수 있다 — 17칸.** 근거: `PspInsertThread +0x617` (DWORD 읽기) → `+0x632` · `+0x649` (같은 DWORD 통째 되쓰기) 가 **만들기 길 (`①` 안, `①P`) 의 `[R14+0x5a0]` DWORD 쓰기** 이고, 이 줄이 바꾸는 비트는 9~14 (`ThreadIoPriority`·`ThreadPagePriority`) 뿐이라 나머지 비트는 읽은 값을 그대로 되쓴다. 사진: K3 ~ P2 일곱 장에서 이 DWORD 가 `0x5402` 로 그대로 (바뀌는 비트 = 아는 칸 3개뿐 `ThreadInserted`·`ThreadIoPriority`·`ThreadPagePriority`).

| 가름 | 칸 (19) | 근거 |
|---|---|---|
| **조건부 안 탐 2 (그대로)** | `HideFromDebugger` (비트 2) · `IndirectCpuSets` (비트 17) | 켜는 줄 `PspAllocateThread +0xb04` · `+0xaea` 가 따로 있고 test.exe 는 안 탐 (sast/47 2.2). 되쓰기도 0 을 읽어 0 을 쓰므로 값은 그대로 |
| **0 을 씀 17 (다시 셈)** = 아무도 안 씀 3 + 범위 밖 14 | `RundownFail` (15) · `UmsForceQueueTermination` (16) · `ReservedCrossThreadFlags` (25:7) + `Terminated` (0) · `ActiveImpersonationInfo` (3) · `HardErrorsAreDisabled` (4) · `BreakOnTermination` (5) · `SkipCreationMsg` (6) · `SkipTerminationMsg` (7) · `CopyTokenOnOpen` (8) · `DisableDynamicCodeOptOut` (18) · `ExplicitCaseSensitivity` (19) · `PicoNotifyExit` (20) · `DbgWerUserReportActive` (21) · `ForcedSelfTrimActive` (22) · `SamplingCoverage` (23) · `ImpersonationSchedulingGroup` (24) | 같은 DWORD 되쓰기 (`+0x632`·`+0x649`), 읽은 값이 0 → 0 을 씀. 범위 밖 14 칸을 켜는 쪽 `[추측]` (이름만) 은 sast/47 2.4 표 그대로 (`NtSetInformationThread`·`PsImpersonateClient`·`Dbgk…` 등) |

- **바뀌는 칸 이름 = `ThreadIoPriority`·`ThreadPagePriority`.** `ThreadInserted` (비트 1) 는 앞 줄 `+0x490 OR.LOCK` 이 쓴다 (둘 다 아는 칸).
- 다시 셀 때의 숫자 (flow-writer 용): `_ETHREAD` 3.13 — 아는 칸 48 → **58** (이 요청 10) → **75** (CrossThreadFlags "0 을 씀" 17 더하면), 아무도 안 씀 22 → **19**, 범위 밖 69 → **55**, 모름 10 → **0**. 안 다시 세면 아는 칸 58 · 아무도 안 씀 22 · 범위 밖 69 · 모름 0 (합 149).
- 한계: **같은 방식이 `SameThreadPassiveFlags`·`SameThreadApcFlags` 에는 안 먹는다** — 그 DWORD 를 되쓰는 줄은 만들기 길에 없다 (sast/47 1장 그대로). 이번엔 `CrossThreadFlags` 만 다시 셌다.
- 지금 우리 (PE_emulator): **확인 필요.** `Src/nt/Process.cpp` 의 `PspInsertThread` (줄 1682 ~ 1700) 는 `ThreadListEntry` 꼬리 달기 (`+0x6e3`, `+0x239 ~ +0x243`) 만 하고 `KeStartThread`·`KeSuspendThread` 는 "안 함" 으로 적혀 있다 → **`ThreadListEntry` 는 맞음**, `CrossThreadFlags` 우선순위 비트·`KernelStackReference`·`KeStartThread` 의 아홉 칸은 에뮬레이터에 아직 없다 (틀린 게 아니라 빈 칸 — 이 결과서가 채울 줄). `Src/nt` 안 grep 만 했고 `Scheduler.cpp` 안은 안 읽었다.

### 2. `K3~P0` 이후 — `_ETHREAD` 5 · `_KTHREAD` 37

**답: 42칸 중 40칸은 쓰는 줄을 찾았고 (sast/71~75 에서 나온 줄은 근거를 옮기고 `.asm` 줄을 다시 대조), 2칸 (`SavedApcStateFill` · `ResourceIndex`) 은 "범위 밖 길에서만 씀 `[추측]`".** 칸마다 "어느 사진 구간에 처음 값이 생기나" 를 맞췄고, 어긋나는 것은 표 끝 "사진 구간과 어긋나는 것" 에 모았다. 사진 값은 `ms/logs/04_baggage-read_{K2,K3,P0,P1,P1a,P1b,P2}.txt` 의 `02c ETHREAD_RAW`·`02`·`02b`·`03` 을 칸마다 뽑아 비교 (scratchpad `snap.py`·`vals.py`).

#### 2.1 `K3~P0` — `_ETHREAD` 4 + `_KTHREAD` 9 (부모가 `NtResumeThread` 로 풀고, 새 스레드가 처음 CPU 를 받는 구간)

| 칸 | 쓰는 줄 (함수 + `+0x`) | 값 · 출처 | 사진 (K3 → P0 → 뒤) |
|---|---|---|---|
| `_ETHREAD +0x6f8` `LastExpectedRunTime*` | `KiDeferredReadySingleThread +0x151d` (`[R15+0x6f8]` = `[R15+0x54]`, 호출 `R`). 같은 줄 계열: `KiAddThreadToPrcbQueue +0x154 / +0x283` · `KiQueueReadyThread +0x41d` · `KiUpdateLocalReadyQueueStatisticsOnInsertion +0x22` | 스레드 `ExpectedRunTime` (`_KTHREAD +0x54`) 사본 | 0 → `0x4c4b40` → 준비될 때마다 (`0x3ccc10` P1 · `0x192eae` P1b · `0xa8832` P2 — `ExpectedRunTime` 을 따라감) ✓ |
| `_ETHREAD +0x708` `LastSoftParkElectionGroupAffinity` (16B) | `KiUpdateSoftParkElectionStatisticsOnInsertion +0x5b` (16B 0 채움) · `+0x69` (그룹 `[RDX+0x710]` ← PRCB `+0x2c6`) · `+0x7e` (마스크 `[RDX+0x708]` ← 스레드 `Affinity` 비트맵 `[+0x240] → [그룹]`). 호출 `KiDeferredReadySingleThread +0x152b` (ret `+0x1530`) | 계산 (스레드 친화도) | 0 → `3` (마스크 두 CPU) ✓, 뒤 그대로 |
| `_ETHREAD +0x724` `LastSoftParkElectionQos` | 같은 함수 `+0x1f` (`MOV [RDX+0x724],AL`, `+0xf MOVZX EAX,[RDX+0x204]`) | `_KTHREAD +0x204 BamQosLevel` 사본 | 0 → **6** (P0) → 0 (P1~) — 준비될 때마다 다시 씀 |
| `_ETHREAD +0x726` `LastSoftParkElectionRunningType` | 같은 함수 `+0x41` (`SETNC` — `max(CurrentRunTime, ExpectedRunTime) ≥ KiDynamicHeteroCpuPolicyExpectedCycles`, 전역 nt+`0xfc5374`) | 계산 | 0 → 1 ✓ |
| `_KTHREAD +0x48` `CycleTime*` (카운터) | `KiEndThreadCycleAccumulation +0x1bc` (`[R14+0x48]`) · `KiCaptureTotalCyclesCurrentThread +0x1d8` · `KiUpdateTotalCyclesCurrentThread +0x134` · `KeUpdateTotalCyclesCurrentThread +0x134` | 누적 사이클 (run76 줄, `.asm` 이름 확인) | 0 → `0x5dc0` → 계속 늚 |
| `_KTHREAD +0x50` `CurrentRunTime*` (카운터) | 0 으로: `KiSwapThread +0x5a` (`[R13+0x50]` = `R15D` = 0). 늘리는 쪽: `KiEndThreadCycleAccumulation +0x1e7` · `KiCaptureTotalCyclesCurrentThread +0x203` · `KiUpdateTotalCyclesCurrentThread +0x163` · `KeUpdateTotalCyclesCurrentThread +0x166` · `KiEndIdleCycleAccumulation +0x1ee` | 사이클 | 0 → `0x5dc0` → `0x1bc60` … 계속 바뀜 |
| `_KTHREAD +0x71` `Running` | `SwapContext +0x18` (`MOV [RSI+0x71],1`, `RSI` = 새 스레드, `KiSwapThread → KiSwapContext → SwapContext`) · 옛 스레드 쪽 `SwapContext +0x4ad` (0, run75) | 상수 1 | 0 → 1 ✓ |
| `_KTHREAD +0xd8` `WaitListEntry*` (16B) = `SwapListEntry` (앞 8B, 같은 바이트) | `KiDeferredReadySingleThread +0x171f` (`LEA RCX,[RBX+0xd8]`) → `+0x173d · +0x1740 · +0x1744 · +0x1747` (준비 줄 꼬리에 달기) · 또는 `+0x1824 ~ +0x183c` (머리에 달기). `SwapListEntry` 이름으로 쓰는 쪽은 `KiInSwapSingleProcess +0x98` (스택 swap-in 목록) `[추측: 이름]` | 준비 줄 (PRCB 안 `ReadyListHead[우선순위]`, 이름 없는 칸) 의 이웃 포인터 | 0 → `[…560e358 − …bd1f80]` (P0, 준비 줄 고리) → `[…a8dd158 − …]` (P1 ~ P1a) → `[0 − …969a1198]` (P1b) → `[0 − …bd1f90]` (P2) — 줄에서 빠진 뒤에도 옛 값이 남고 준비될 때마다 다시 씀 |
| `_KTHREAD +0x154` `ContextSwitches*` (카운터) | `SwapContext +0x5bb` (`INC [RSI+0x154]`, 조각 `FUN_1406b686f +0x28c`) | +1 | 0 → 1 (P0) → `0x17` → `0x31` (P2) ✓ |
| `_KTHREAD +0x184` `State` | 처음 CPU 를 받으면 `KiSwapThread +0x190` (`MOV [RBP+0x184],2`) · `+0x11d`. 그 앞 단계 (준비): `KiDeferredReadySingleThread +0x122e` (1) · `KiInsertDeferredReadyList +0x33` (7). 기다림·깨움 쪽은 2.2 | 상수 (`KTHREAD_STATE`: 1 Ready · 2 Running · 7 DeferredReady) | 0 → 2 (P0 ~ P2 그대로) ✓ |
| `_KTHREAD +0x1b4` `WaitTime*` (카운터) | `KiDeferredReadySingleThread +0x11f8` (`MOV [RBX+0x1b4],EAX`, `EAX` = `[0xfffff78000000320]` = KUSD `TickCount` 아래 32비트, `+0x11e8 ~ +0x11f2`). 기다릴 때: `KeWaitForSingleObject +0x1c5` (`KeWaitForMutexObject`) · `KiBeginThreadWait +0x11e` · `KeDelayExecutionThread +0x20a` · `KeWaitForMultipleObjects +0x2ea` · `KiQueueReadyThread +0x2a5` | 공유 시계 (`TickCount`) | 0 → `0x14dff` (P0, 부팅 22분 16초 ≈ 85,248 틱) → `0x14e01` … ✓ (**run77 의 `[추측]` "P0 값은 다른 쓰는 줄" 닫힘** — `KiDeferredReadySingleThread +0x11f8` 이 맞다) |
| `_KTHREAD +0x238` `AffinityVersion` | `KiComputeThreadAffinity +0x212` (`MOV [R14+0x238],RSI`, `RSI` = 전역 `KiCpuSetSequence` nt+`0xfc6c38`, `+0x202 MOV RAX,[KiCpuSetSequence]`). 호출: `KiDeferredReadySingleThread +0x1877` — 앞 `+0x2a9 CMP [RBX+0x238],RAX` 가 다르면 (새 스레드 0) 부름 | 전역 사본 | 0 → `0x18` (P0) 그대로 ✓ |

#### 2.2 `P0~P1` — `_KTHREAD` 9 (새 스레드가 `PspUserThreadStartup` 에서 `LdrInitializeThunk` 로 가는 구간)

사진: 이 구간에 **한 번 기다리고 (`KernelWaitTime` 1, `WaitBlockCount` 1) 파일을 한 번 읽었다 (`ReadOperationCount` 1, `ReadTransferCount` 1862, `OtherOperationCount` 2)**. 기다림은 `WaitReason` 0 (`Executive`, 동기 파일 I/O 의 대기) 과 맞는다 `[추측: 같은 사건]`.

| 칸 | 쓰는 줄 | 값 · 출처 | 사진 |
|---|---|---|---|
| `+0x70` `WaitRegister*` | 기다릴 때 `KeWaitForMutexObject +0x116` (0) · 확정 `KiCommitThreadWait +0xbf` (`[RBX+0x70]` = `State` 1) · 깨움 `KiTryUnwaitThread +0x2f8` (2) · `+0x321` (6) · `KeSetEvent +0x168 ~ +0x16e` · `+0x4ca ~ +0x4d3` (run77) | `_KWAIT_STATUS_REGISTER` 바이트 (`State`·`Affinity`·`Priority`·`Apc`·`UserApc`·`Alert` 비트 — 값 읽는 법은 사진 `0x…100` = `WaitCommitted` `[추측: 이름]`) | 바이트 +0x70: 0 (K2 ~ P0) → 1 (P1 ~ P1b) → 0 (P2) ✓ (같은 8바이트 안 `Running` +0x71 · `MiscFlags` +0x74 는 따로) |
| `+0xd0` `WaitBlockList` | `KiCommitThreadWait +0x5b` (`MOV [RCX+0xd0],RSI`) | 스레드 + `0x140` (`WaitBlock[0]`) | 0 → `…51c0` (P1) ✓ |
| `+0x140` `WaitBlockFill4*` (20B) | `KeWaitForMutexObject +0x1d8` (`[R9+0x10]` `WaitType` = 1) · `+0x1dd` (`[R9+0x11]` `BlockState` = 4) · `+0x1e2` (`[R9+0x12]` `WaitKey`) · 객체 줄에 꿰기 `+0x7b7 ~ +0x7c1`; `BlockState` 는 `KiTryUnwaitThread +0x30d · +0x339 · +0x1a7` 도 | 상수·인자 | 0 → 값 있음 (P1, 줄 연결 포인터·`WaitType`·`BlockState`) → 바뀜 `P1b~P2` (한 번 더 기다림) |
| `+0x24b` `WaitBlockCount` | `KeWaitForMutexObject +0x7d2` (`MOV [RDI+0x24b],1`) | 상수 1 (객체 하나) | 0 → 1 (P1) ✓ |
| `+0x380` `ReadOperationCount` · `+0x398` `ReadTransferCount` (카운터) | 횟수: **`IopReadFile` 꼬리 `FUN_1408e868f +0x25e`** (`inc [thread+0x380]`, `RAX` = `gs:[0x188]`, `IopReadFile +0x284` 가 점프) · `IopSynchronousServiceTail +0x228`. 바이트: `IopUpdateIrpTransferCount +0x86 · +0xd0` (`add [thread+0x398],…` — IRP 완료 때 지금 스레드 칸에 더함) | 파일 읽기 한 번 (1862 바이트) | 0 → 1 · 1862 (P1) 그대로 ✓ (뒤에 안 바뀜) |
| `+0x390` `OtherOperationCount*` (카운터) | `inc [thread+0x390]`: `NtQueryInformationFile +0x3a9 / +0xc16` · `NtSetInformationFile +0x7c6` · `IopCloseFile +0x129` · `NtQueryAttributesFile +0x171` · `NtQueryFullAttributesFile +0x17b` · `IoQueryInformationByName +0x1d3` · `IopGetSetSecurityObject +0x183` · `IopSynchronousServiceTail +0x1e4` · `NtCancelIoFile +0x7e` · `NtDeleteFile +0xcf` 등 | 읽기·쓰기 말고 파일 일 (열기·조회·닫기) 한 번당 +1 | 0 → 2 (P1) → 3 (P1a) → 9 (P1b) ✓ |
| `+0x3e0` `KernelWaitTime*` (카운터) | 깨울 때: `KiTryUnwaitThread +0x1bb` (`ADD [RBX+0x3e0],RCX`, `RCX` = 지금 틱 − `WaitTime`, `WaitMode` = 0 일 때) · `KeSetEvent +0x4f2` (풀어 넣은 쪽) | 기다린 틱 수 | 0 → 1 (P1) → 5 (P1b) |
| `+0x258` `SavedApcStateFill` (43B) | **`범위 밖 길 [추측]`** — 정적으로 쓰는 줄을 못 정함. 값이 "목록 머리 둘이 각자 자기 자신 가리킴 (`+0x258`·`+0x268`), `Process` (+0x278) 는 0" 이라 **`KiAttachProcess`·`KiStackAttachProcess` 가 `ApcState` 를 `SavedApcState` 로 옮기고 되돌린 흔적** (이 함수들이 `LEA [thread+0x258]` 을 한다: `KiStackAttachProcess +0xb6` · `KiAttachProcess +0xa1` · `KiDetachProcess +0xc3` …). 이 구간에서 붙인 쪽은 못 가림 | — | 0 → 머리 둘 자기 연결 (P1) 그대로 — 아래 "어긋나는 것" 2 |

#### 2.3 `P1~P1a` — `_ETHREAD` 1 + `_KTHREAD` 7 (첫 시스템 호출들 + 스케줄러 공유 표 받기)

| 칸 | 쓰는 줄 | 값 · 출처 | 사진 |
|---|---|---|---|
| `_KTHREAD +0x80` `SystemCallNumber*` | `KiSystemServiceUser +0xd2` (`MOV [RBX+0x80],EAX`, `SC`) | 서비스 번호 (`EAX`, 시스템 호출마다 덮음) | 0 → `0xd` (P1a) → `0xe` (P1b) → `0x1d3` (P2) |
| `+0x88` `FirstArgument*` | `KiSystemServiceUser +0xcb` (`MOV [RBX+0x88],RCX`, `RCX` = `+0xc2` 에서 `[RBP-0x48]` — `KiSystemCall64 +0x83` 이 `R10` 을 `RCX` 로 저장한 첫 인자) | 첫 인자 (`R10`) | 0 → `0xfffffffffffffffe` (P1a, 현재 프로세스 핸들 −2) → … |
| `+0x90` `TrapFrame` | `KiSystemServiceStart +0x0` (`MOV [RBX+0x90],RSP`, 프레임 시작) · 되돌릴 때 `+0x5c5` (`MOV [R11+0x90],RDX`) · `+0x939` | 지금 트랩 프레임 주소 (`InitialStack − 0x190`) | 0 → `…7460` (P1a) 그대로 ✓ |
| `+0x283` `WaitReason*` | 기다릴 때 `KeWaitForMutexObject +0x1b0` (`MOV [RDI+0x283],AL`, 인자 — `PspUserThreadStartup +0xd7` 6 · `ObWaitForSingleObject +0xb5` 6 · `KiSchedulerApc +0xee` 5) · 넘기기 쪽 `KiDispatchInterrupt +0x13e` (`0x1f`) · `KiExitDispatcher +0x187` (`0x20`) · `KiQuantumEnd +0xb98` (`0x1e`) | 인자·상수 | 0 → `0x1f` (P1a) → 6 (P1b) → 0 (P2). **첫 쓰기는 `P0~P1` (0 = `Executive`)**, 0 이 아닌 첫 값이 `P1~P1a` |
| `+0x320` `SchedulerSharedSystemSlot` | `PspSchedulerSharedDataRegionSlotAllocate +0x1ca` (`MOV [RBP+0x320],RCX`) — `+0x1b7 KeSwappablePageGetLockedAddress` 가 돌려준 시스템 주소 + 슬롯 안 위치 (`[RBP+0x464]`) | 시스템 공간 주소 (`…ee09f4ac0000`) | 0 → `…f4ac0000` (P1a) 그대로 ✓ |
| `+0x468` `SchedulerSharedSwappablePage` | 같은 함수 `+0x1a4` (`MOV [RBP+0x468],RCX`, `RCX = 영역 + R14×0x50`) — 영역 객체 안 `_KSWAPPABLE_PAGE` 배열의 한 칸 | 영역 객체 (`RDI`) 안 주소 | 0 → `…995fbe10` (P1a) ✓ |
| `+0x460` `ResourceIndex` | **`범위 밖 길 [추측]`**: `ExpFindCurrentThread +0xf7` · `ExpFindEmptyEntry +0x38` · `ExpExpandResourceOwnerTable +0xc7` · `ExpAcquireResourceSharedLite +0x4fb / +0xa53 / +0x1131` · `ExAcquireResourceExclusiveLite +0x8cd` · `ExpAcquireSharedStarveExclusive +0x334 / +0x3bb` · `ExReleaseResourceLite +0x968` 등 (`mov BYTE [thread+0x460],…`) — **실행 리소스 (`ERESOURCE`) 의 주인 표 안 이 스레드 자리 번호**. 이웃 칸 `+0x464 SchedulerSharedOffset` (`SlotAllocate +0x19e`) 와 이름만 비슷하고 **다른 일** | 주인 표 인덱스 | 0 → 1 (P1a) 그대로. 어느 리소스가 처음인지는 정적으로 못 정함 `[공백]` |
| `_ETHREAD +0x6e0` `SchedulerSharedDataObject` | `PspSchedulerSharedDataRegionSlotAllocate +0x1d1` (`MOV [RBP+0x6e0],RDI`, `RBP` = 이 스레드, `RDI` = 영역 객체) — 앞에 `+0x5b CMP [RBP+0x6e0],0` (이미 있으면 `0xc00000bb` 로 거절) | **입력** (두 번째 인자 = 프로세스에 붙은 `PspSchedulerSharedDataType` 객체) | 0 → `…995fbe10` (P1a) — `KTHREAD +0x468` 과 **같은 주소** ✓ |

경로: `SC → NtSetInformationThread (호출 +0xc2c) → PspSchedulerSharedDataRegionSlotAllocate` (`_calls.tsv`; 클래스 `0x39`). ntdll 쪽은 `LdrpAcquireSchedulerSharedDataSlot +0x84 NtSetInformationThread(-2, 0x39, 버퍼, 0x18)` (sast/36 · sast/107 이 읽음 — 이번에 안 건드림). 슬롯을 받으려면 지금 스레드 = 대상 스레드, 대상 프로세스 = 영역 소유 프로세스, 이 스레드에 아직 객체 없음 (`+0x33 ~ +0x62`). 값: `[RBP+0x464]` `SchedulerSharedOffset` = `(슬롯 번호 & 0x3f) << 6`, 영역 객체의 비트맵 `+0xd0` 에서 빈 비트 고름 (`RtlFindClearBitsAndSet +0x10e`), 안 붙은 쪽이면 `ZwAllocateVirtualMemory +0x26b` 로 쪽 하나 예약.

#### 2.4 `P1a~P1b` — `_KTHREAD` 6

| 칸 | 쓰는 줄 | 값 · 출처 | 사진 |
|---|---|---|---|
| `+0x78` `ThreadFlags.AutoBoostEntriesExhausted` (비트 16) | `KeAbPreAcquire +0x52` (`lock bts [RBX+0x78],0x10` — 앞 `+0x31 MOV RSI,[RBX+0x470]` `KernelAbEntries` 의 빈 칸이 없을 때 `+0x38 ~ +0x47`) | 계산 (칸이 다 찼음) | 0 → 1 (P1b) 그대로 ✓ (푸시락 잡는 곳 어디서든 부름 — 공용 부품, 이름만) |
| `+0x84` `ReadyTime*` (카운터) | `KiSwapThread +0x317 · +0x3af` (`ADD [RBP+0x84],EAX`) · `KiDispatchInterrupt +0x283` · `KiUpdatePriorityMatrixForRunningTransition +0x53` (run75) | 준비 줄에서 기다린 시간 더함 | 0 → 2 (P1b) |
| `+0x187` `WaitMode*` | `KeWaitForMutexObject +0x11a` (`MOV [RDI+0x187],R14B`, 인자 `WaitMode`) | 인자 (0 `KernelMode` / 1 `UserMode`) | 0 → 1 (P1b) → 0 (P2). **첫 쓰기는 `P0~P1` (0)** |
| `+0x237` `AdjustIncrement*` | `KiProcessThreadWaitList +0x9f` (`MOV [R13+0x237],R8B`, 깨움 `Increment`) · 이웃 `+0x98` `AdjustReason` | 인자 (`KeSetEvent` 등의 `Increment`) | 0 → 1 (P1b) → 0 (P2) |
| `+0x366` `DecayBoost` | `KiDeferredReadySingleThread +0x13cf` (`MOV [RAX+0x366],BL`, `BL` = 깨움 부스트 − 기본, 앞 `+0x13a6` · `Feature_StackingDecayBoosts` 검사) · 퀀텀 끝 `KiQuantumEnd +0x704 / +0x81b` · `KiComputeNewPriority +0x7b / +0x86` · `KiSetForegroundBoost +0x7b` · `KeSetPriorityThread +0x100 / +0x1d4` | 계산 (`Priority` 8 → 9 의 1) | 0 → 1 (P1b) ✓ (`Priority` 가 같은 때 8 → 9) |
| `+0x3e8` `UserWaitTime` (카운터) | `KiTryUnwaitThread +0x142` (`ADD [RBX+0x3e8],RCX`, `WaitMode` ≠ 0 일 때) · `KeSetEvent +0x456` | 사용자 모드 기다림 틱 | 0 → 2 (P1b) |

#### 2.5 `P1b~P2` — `_KTHREAD` 6

| 칸 | 쓰는 줄 | 값 · 출처 | 사진 |
|---|---|---|---|
| `+0x74` `MiscFlags.Alertable` (비트 4) | `KeWaitForMutexObject +0x112` (`AND [RDI+0x74],0xffffffef` — 끔) · `+0x126` (`OR [RDI+0x74],0x10` — 켬, 인자 `Alertable`) | 인자 | 0 → 1 (P2) |
| `+0xc8` `WaitStatus` | 깨우는 쪽 `KeSetEvent +0x484 · +0x2ec · +0x4d6 · +0x171` · `KiTryUnwaitThread +0x168 · +0x2fb` · `KeReleaseMutantEx +0x306 · +0x4a0` · `KiWaitSatisfyMutant +0x4d`; 기다리는 쪽 `KeWaitForMutexObject +0x416` (0) · `+0x499` · `KiCommitThreadWait +0x4fa` (`MOV [RBX+0xc8],0x100`) · `+0x451` | 상태 값 (`0x100` = 사용자 APC 로 깨움 `[추측: 이름]`) | 0 → `0x100` (P2). 읽는 쪽 `KiSwapThread +0x25c` (쓰는 줄 아님). 갈래에 따라 옛 값이 남을 수 있음 |
| `+0x218` `NextProcessor` · `NextProcessorNumber` (31비트) | `KiDeferredReadySingleThread +0x3b5` (`[RBX+0x218]` = `[RSI+0x24]`) · `+0x69e`·`+0x11b4`·`+0x1502`·`+0x1e7d` … · `KiScheduleThreadToRescheduleContext +0xed`·`+0x217` · `KiSearchForNewThreadsForRescheduleContext +0x8b3` · `KiQueueReadyThread +0x402` · `KiAddThreadToPrcbQueue +0x13b` | 고른 PRCB 의 `Number` (이름: 비트 31 `SharedReadyQueue` 는 sast/46 범위 밖 칸) | **첫 쓰기 K3~P0 (값 0)**; 사진 첫 0 아닌 값 `P1b~P2` (1 = CPU 1 로 준비됨) |
| `+0x388` `WriteOperationCount*` · `+0x3a0` `WriteTransferCount*` (카운터) | 횟수 `IopWriteFile +0x1ed` (`inc [thread+0x388]`) · `IopSynchronousServiceTail +0x236`. 바이트 `IopUpdateIrpTransferCount +0xb2 · +0xee` (`add [thread+0x3a0],…`) | 파일 쓰기 | 0 → 5 · 205 (P2) |

#### 2.6 질문 2 의 나머지 답

- **`UserIdealProcessor`·`UserAffinity` 최종 출처**: `KeStartThread +0x17c` (`UserIdealProcessor`) · `+0x147 KiCopyAffinityEx` (`UserAffinity` 비트맵, 지역 사본은 `+0xec` 가 프로세스 `[RDI+0x50]` 에서 복사). 이 줄기 안에서 다시 바꾸는 줄이 없다 — 사진 `UserIdealProcessor` K3 1 → P2 1.
- **`KiQueueTebUpdateApc` 길이 첫 스레드에서 타나**: **안 탄다 `[정적: 호출 길 없음]`.** 호출자는 `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` 둘뿐이고 (`callers` 훑기), 이들의 호출자는 `KeSetIdealProcessorThreadEx` (← `MiSetIdealProcessorThread`·`KeSetIdealProcessorThread`·`KeSetIdealProcessorThreadByNumber`·`SmCompressCtxUpdateThreadTopology`·`MiRevertProcessorMove`), `KeSetUserAffinityThread` (← **`NtSetInformationThread +0x12ac`**·`ExpWorkerThread`·`SmCompressCtxUpdateThreadTopology`), `KeSetUserGroupAffinityThread` (← `MiMoveBackgroundZeroThreads`·`MiAssignBackgroundZeroThreadToProcessor`), `KeSetAffinityProcess` (← `PspSetProcessAffinitySafe`·`KiInitializeDynamicProcessorDpc`·`Phase1InitializationDiscard`), `KiSetLegacyAffinityThread` (← `NtSetInformationThread +0xb29`). 유저에서 들어오는 길은 `NtSetInformationThread` 의 친화도·이상적 프로세서 클래스뿐인데, 초기화 줄기의 ntdll `.c` 에서 본 `NtSetInformationThread` 호출 클래스는 **5 (`LdrpThreadTokenSetImpersonationToken`) · 0x38 (`RtlEnterCriticalSection`·`RtlLeaveCriticalSection`·`RtlReleaseSRWLock*`·`RtlAbPostRelease`·`RtlpAbFreeKernelEntry`·`LdrpCallTlsInitializers`·`RtlpxLookupFunctionTable`) · 0x39 (`LdrpAcquireSchedulerSharedDataSlot`)** 뿐이다. (한계: 읽은 건 `ms/decomp/ntdll` 에 `.c` 가 있는 11개 파일뿐 — 초기화 줄기의 모든 호출자라는 보장은 없음. `NtSetInformationProcess` 쪽 친화도 갈래는 안 봄.)
- **`TEB P0~P1` 다섯 칸 (`UserAffinity`·`UserIdealProcessor` 최종 출처)** 은 `PspUserThreadStartup +0x94 → KeUpdateTebSchedulingPropertiesCurrentThread` 가 `KeStartThread` 가 쓴 이 칸들을 **읽어서** TEB 에 쓰는 것 (sast/48) — `KiUpdateTebApc` 로 다시 쓰는 길은 위 이유로 안 탄다.

#### 2.7 사진 구간과 어긋나는 것 (run44 사진 ↔ 코드)

1. **`P0~P1` 에 커널 파일 읽기가 있다** — `ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2 · `KernelWaitTime` 1 · `WaitBlockCount` 1 (`WaitReason` 0). 7.2 가 이 구간을 "`PspUserThreadStartup` → `PspInitializeThunkContext`" 로만 보았는데, 그 길은 `PspSetContextThreadInternal` 이 **지금 스레드면 APC 없이 안에서 바로 처리** (`+0xf7 CMP R13,R14` 대상 = `gs:[0x188]` 이면 `+0x131 PspGetSetContextSpecialApc` 를 바로 부름, 아니면 `+0xfa JNZ → +0x954` 쪽에서 `KeInsertQueueApc +0x2bf`·`KeWaitForSingleObject +0x2dd`) 라 기다림이 안 생긴다. 이 구간의 커널 길 중 파일 I/O 를 할 만한 곳은 `PspNotifyThreadCreation +0x1c DbgkCreateThread` · `+0xda PfProcessCreateNotification` (프리페처) · `+0x59 KeQuerySystemTimePrecise` 정도 `[추측: PfProcessCreateNotification]`. 정적으로 못 정한다 → **동적 질문 D2**.
2. **`SavedApcStateFill` 이 `P0~P1` 에 처음 쓰인다** (목록 머리 둘 자기 연결, `Process` 는 0). attach 계열 흔적 `[추측]` (위 2.2). 같은 사건 (파일 읽기 + attach, 프리페처가 프로세스에 붙어 읽기) 일 가능성이 있다 `[추측]` → D2.
3. **`NextProcessor`**: 7.2 는 `P1b~P2` 인데 코드의 첫 쓰기는 `K3~P0` (값 0 이라 사진엔 0 으로 보임). `P1b~P2` 의 1 은 나중 쓰기.
4. **`WaitReason`·`WaitMode`·`WaitStatus`·`MiscFlags.Alertable`**: 첫 쓰기 (0) 는 `P0~P1` 의 첫 기다림이고, 사진이 "처음 값" 으로 읽은 `P1~P1a`·`P1a~P1b`·`P1b~P2` 는 **0 이 아닌 첫 값**이다 (0 을 쓴 줄은 사진으로 못 가림).
5. **`WaitTime` P0 `0x14dff`** 는 기다림 전 값이 맞다 — `KiDeferredReadySingleThread +0x11f8` (준비 때 `TickCount` 적음). run77 의 `[추측]` 이 닫힘.
6. 나머지 칸은 처음 값이 나오는 사진 구간과 코드 순서가 맞다 (`K2~K3` 15칸 = `PspInsertThread +0x807` 앞, `AffinityVersion`·`WaitListEntry`·`LastSoftPark*`·`Running`·`State`·`ContextSwitches` = `K3~P0`, `SystemCallNumber`·`TrapFrame`·`SchedulerShared*` = 첫 syscall 뒤 `P1~P1a`).

### 3. `_KTRAP_FRAME` 25 — 가름만

**답: 25칸 모두 "트랩 프레임 덮임".** 입구마다 쓰는 칸이 다르고, **마지막 입구가 안 쓰는 칸은 이전 입구가 쓴 값이 그대로 남는다** — 그래서 `int3` 로 멈춘 사진 (`P1a`·`P1b`·`P2`) 에 syscall·페이지 오류 입구가 남긴 잔재가 보인다.

입구 약어 (`S1` = `KiSystemCall64` (rva `0x6c1840`) · `S2` = `KiSystemServiceUser` · `S3` = `KiSystemServiceStart` · `S4` = `KiSystemCall64Shadow` (rva `0xbcb200`, KVA 그림자 변형) · `B` = `KiBreakpointTrap` · `PF` = `KiPageFault` · `GP` = `KiGeneralProtectionFault` · `N` = `KiNmiInterrupt`/`KiMcheckAbort`). 프레임은 모두 같은 자리 (`InitialStack − 0x190`, 입구 코드의 `RBP = 프레임 + 0x80`, 쓰기는 `[RBP − 0x80 … +0x110]`). 쓰는 오프셋은 입구 어셈블리 전수 (`objdump`, Ghidra 조각 포함) 에서 `[RBP+…]` 쓰기를 프레임 칸으로 바꿔 짝지음.

| 칸 (프레임 `+0x`) | 어느 입구가 쓰나 (줄) | 사진 (처음 값) |
|---|---|---|
| `P1Home` (+0x0) · `P2Home` (+0x8) · `P3Home` (+0x10) · `P4Home` (+0x18, `P1a~P1b`) | **입구 코드는 안 쓴다 (`S1`·`S2`·`B`·`PF`·`GP` 모두 +0x0~+0x20 쓰기 없음)**. 쓰는 곳: `S3 KiSystemServiceStart +0x90c · +0x90f · +0x913 · +0x917` (win32k 첫 호출 `KiConvertToGuiThread` 앞뒤로 `EAX`·`RCX`·`RDX`·`R8` 임시 보관, 갈래 `+0x907 CMP EDI,0x20`) + 프레임 바로 아래 호출된 함수의 인자 홈 칸 `[추측]` | P1a: `…98440000` · `…96cb0000` · `…2fcffa40` / P1b: `…98445080` (= 이 스레드 주소) … 계속 바뀜 |
| `PreviousMode` · `InterruptRetpolineState` (+0x28, 같은 바이트) | `S2 KiSystemServiceUser +0x4` (= `KiSystemCall64 +0x2a6`, `MOV [RBP-0x58],1`) · 커널 서비스 입구 `KiServiceInternal +0x49` (`[RBP-0x58]` = `DIL`). `B`·`PF` 는 안 씀 | P1a 1 (syscall 이 씀) · `B` 만 거치면 0 이던 칸 |
| `FaultIndicator` · `NmiMsrIbrs` (+0x2a) | `PF KiPageFault +0x383` (`MOV [RBP-0x56],AL`, 오류 코드 `& 9` — 읽기 0 · 쓰기 1 · 실행 8). `NmiMsrIbrs` 이름으로 쓰는 쪽은 `N` 에서 못 찾음 `[공백]` | P1a 1 (쓰기 폴트) → P1b 8 (실행 폴트) |
| `Rax` (+0x30) · `R8`·`R9`·`R10`·`R11` (+0x48 ~ +0x60) | **모든 입구**: `S1 +0x7f` (`Rax`, `S4 +0x97`) · `S2 +0x2c`·`+0x30`·`+0x38` (`R10`)·`+0x34` (`R11` 자리에 `R10` 값) · `B +0x18`·`+0x24 ~ +0x30` · `PF +0x14`·`+0x20 ~ +0x2c` · `GP +0x14`·`+0x20 ~ +0x2c`. **만들 때 처음 값** (CONTEXT 값 0): `KiInitializeContextThread → KxContextToKframes +0xdd` (`Rax`) · `+0xfe · +0x109 · +0x114 · +0x11f` (`R8 ~ R11`) (run53) | K2 ~ P1 0 (스레드 시작 때 유저 값도 0), P1a 부터 유저 값 (`R8` `0x60` …) |
| `GsBase` · `GsSwap` (+0x68) | `PF KiPageFault +0x2de · +0x2e1` (두 조각, 앞 dword · 뒤 dword) · `N KiNmiInterrupt +0x54` · `KiMcheckAbort +0x54`. `S`·`B` 는 안 씀 | P1a `0x9b2fa67000` (= TEB, 유저 GS 베이스) — 페이지 오류 흔적 |
| `Xmm1` (+0x80, 16B) | `S2 +0x40` · `B +0x2f8` · `PF +0x30b` · `GP +0x2f4` · `N +0x179` (`MOVAPS [RBP],XMM1`) · `S3 +0x22a / +0x2bf` (0 채움 갈래) · 만들 때: `KxContextToKframes` FP 갈래 (안 탐) | **`dt` 가 `_M128A` 로만 보여 사진 값을 못 읽음 `[공백]`** |
| `FaultAddress` · `ContextRecord` (+0xd0) | `PF KiPageFault +0x358` (`MOV [RBP+0x50],RCX`, `RCX` = `CR2`) · `N +0xd2`. `ContextRecord` 이름으로 쓰는 쪽은 못 찾음 `[공백]` | P1a `…96cb0000` (= 매개변수 블록 `ProcessParameters` 주소) → P1b `…fa665f10` → P2 `…1a3712a0` |
| `Rbx` (+0x140) · `Rdi` (+0x148) · `Rsi` (+0x150) · `NmiPreviousSpecCtrl` (+0x140 dword) · `NmiPreviousSpecCtrlPad` (+0x144) | `S1 KiSystemCall64 +0x55 · +0x5c · +0x63` (`S4 +0x6d · +0x74 · +0x7b`). `B`·`PF`·`GP` 는 안 저장 (예외 길은 `KiExceptionDispatch +0x44 ~ +0x5b` 가 자기 예외 프레임에 저장). `NmiPreviousSpecCtrl`·`Pad` 는 `Rbx` 와 같은 바이트 — `N` 에서 이 이름으로 쓰는 줄 못 찾음 `[공백]` | P1a `Rbx` `…96f33940` (= 하위 dword `NmiPreviousSpecCtrl` `0x96f33940`, 상위 `0x259` = Pad) |
| `ErrorCode` · `ExceptionFrame` (+0x160, 같은 8바이트) | **CPU 가 #PF·#GP 때 밀어 넣는다** (입구 코드 쓰기 줄 없음). `S1`·`B` 는 `SUB RSP,8` 로 자리만 잡고 **안 씀** (스택 잔재). `ExceptionFrame` 이름으로 쓰는 줄 못 찾음 (`KiDispatchException +0x20` 은 자기 `RBP` 라 다른 칸) | P1a 7 (= 쓰기·유저·존재 폴트) → P1b `0x14` (실행·유저) |

- **첫 스레드의 트랩 프레임이 처음 덮이는 때**: 요청서는 "첫 syscall — 사진 `P1` 직전" 이라 했는데 **사진은 다르게 말한다.** `P1` (`LdrInitializeThunk` 입구) 의 프레임은 이미 `int3` 입구 (`B KiBreakpointTrap`) 가 덮은 것 — `Rip` = `LdrInitializeThunk +1`, `Rcx`·`Rdx` = 바꿔 끼운 값, 이때 `B` 가 쓰는 칸 (`Rax`·`Rcx`~`R11`·`Xmm0 ~ 5`·`ExceptionActive`=1·`MxCsr`·`Rbp`·하드웨어 5개) 만 바뀌었고, **이 요청의 24칸 (`P1Home`·`PreviousMode`·`Rbx`·`GsBase`·`FaultAddress`·`ErrorCode` …) 은 `P1` 에서도 0** 이다. 그 24칸에 처음 값이 생기는 건 `P1` 과 `P1a` 사이 첫 syscall (`S1 → S2`, `SystemCallNumber` `0xd`) 과 페이지 오류 (`PF`, `ErrorCode` 7·`FaultAddress` `…96cb0000`) 때문 (`ExceptionActive` 값도 `S2` 는 2, `B`·`PF` 는 1 — 사진 P1a 1 = 마지막이 `int3`). 정리: **첫 덮임 = `P1` (`B`)**, **24칸의 첫 값 = `P1~P1a` (`S1`/`S2`/`PF`)**.
- **LSTAR 가 어느 쪽인가** (`S1` 직행 vs `S4` KVA 그림자 변형) 는 정적으로 못 정한다 — `B`·`PF` 코드가 `[KiKvaShadow]` (nt+`0x1201840`) 를 읽어 `SWAPGS` 를 건너뛰는 갈래 (`B +0x75`) 가 있어서 둘 다 있고, 어느 쪽이 도는지는 전역·MSR 값에 달렸다 → D1.
- 지금 우리 (PE_emulator): **맞음 (범위 안).** `Src/nt/Syscall.cpp` 줄 925 ~ 949 의 `KiSystemCall64` 모델이 "`KiSystemCall64 +0x15 ~ +0x23` 저장, `KiSystemServiceUser` 는 MXCSR·R8·R9·R10·XMM0 ~ 5 저장뿐" 이라고 적었고 이 결과서 (`S1 +0x15 ~ +0x23` 의 `SS`·`RSP`·`R11`·`CS`·`RCX` push, `S2 +0x2c ~ +0x50`) 와 같다. 이 25칸을 에뮬레이터가 칸으로 만들 필요는 없다 (짐이 아니라 덮이는 칸). **`KTHREAD.SystemCallNumber`·`FirstArgument`·`TrapFrame` (+0x80·+0x88·+0x90) 를 쓰는 곳은 `Src/nt` grep 에 안 나온다** — 읽는 코드가 없으면 무방하나 읽는 쪽이 생기면 `S2 +0xcb · +0xd2`, `S3 +0x0` 줄을 따라야 한다 (확인 필요).

### 4. 간접 호출

**답: 이 길에서 위 칸을 직접 쓰는 간접 호출은 0개. 간접 호출 5개가 있으나 모두 칸을 쓰는 코드가 아니라 "고르는" 코드다.** 읽은 `.asm` 32개 (`PspInsertThread` · `KeStartThread` · `KiSwapThread` · `KiCommitThreadWait` · `KiDeferredReadySingleThread` · `SwapContext` · `KeWaitForMutexObject` · `KiTryUnwaitThread` · `KiSystemServiceUser` · `KiSystemServiceStart` · `KiBreakpointTrap` · `KiPageFault` · `KiUpdateSoftParkElectionStatisticsOnInsertion` · `KiUpdateSharedReadyQueueAffinityThread` · `KiComputeThreadAffinity` · `PspSchedulerSharedDataRegionSlotAllocate` · `KeSelectInitialIdealProcessorForThread` · `KiCopyAffinityEx` · `KiInitializeForegroundBoostThread` · `KiProcessThreadWaitList` · `KeSuspendThread` · `KeSetEvent` · `KiSetIdealProcessorThread` · `KiSetAffinityThread` · `KiQueueTebUpdateApc` · `KiUpdateTebApc` · `KiInsertDeferredReadyList` · `KiExitDispatcher` · `KiAdjustProcessIdealProcessorSetsForThreadCreation` · `KeInitThread` · `KiInitializeContextThread` · `KxContextToKframes`) 의 `CALL reg`·`CALL [mem]`·`JMP reg`·`cfg:` 를 찾았다.

| 자리 | 모양 | 대상 | 칸과의 관계 |
|---|---|---|---|
| `KiSystemServiceStart +0xfa` | `JMP R11` | 인자 복사 점프표 (인자 개수별 꼬리, `KiSystemServiceCopyEnd` 류) | 트랩 프레임 칸 안 씀 (사용자 스택 인자를 커널 스택으로 복사) |
| `KiSystemServiceStart +0x193` · `+0xa24` · `+0xa7f` | `CALL RAX` | **시스템 서비스 본체** — `KeServiceDescriptorTable`·`…Shadow`·`…Filter` 에서 `SystemCallNumber` 로 고른 `Nt*` (표 고르기 `+0x14 ~ +0x5b`, `+0x22 / +0x2b` 가 `ThreadFlags` 비트 7 `GuiThread`·비트 21 로 표를 가름) | **쓰는 칸은 고른 `Nt*` 몫**: `ReadOperationCount` 류 (`NtReadFile`·`NtWriteFile`·`NtQueryInformationFile`…), `SchedulerShared*` (`NtSetInformationThread`), 기다림 칸 (`NtWaitForSingleObject` …). 어느 `Nt*` 가 첫 스레드에서 불리는지는 `SystemCallNumber` 값 (사진 `0xd`·`0xe`·`0x1d3`) 으로 → 동적 D3 |
| `KiDeferredReadySingleThread +0x17f7` | `JMP RCX` | 함수 안 `switch` (`CMP EAX,6` `+0x17e3` → `[R11+RAX*4+0x30df6c]`) — 목적지는 같은 함수 안 | 안 좁혀도 됨 (밖으로 안 나감) |

**대상을 좁힌 것**: 사진 `SystemCallNumber` 가 P1a `0xd` · P1b `0xe` · P2 `0x1d3` — 마지막 syscall 세 개의 번호 (서비스 표 index, `& 0xfff`, `EDI = (번호 >> 7) & 0x20` 이 0 이면 기본 표). 이름은 `KeServiceDescriptorTable` 의 `KiServiceTable` 로 풀어야 하고 이 요청에서는 안 풀었다 (sast/51 의 `ms/ghidra/ntoskrnl.refs.sast51_KiServiceTable.stdout` 가 표 위치를 가진다). → D3.

### 5. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지): `PspInsertThread` 본문 (`+0x2c8 ~ +0x2e5`, `+0x617 ~ +0x649`, `+0x239 ~ +0x243`), `KeStartThread` 본문 + `KiUpdateSharedReadyQueueAffinityThread` · `KiCopyAffinityEx` · `KeSelectInitialIdealProcessorForThread`, `KeSuspendThread`, 스케줄러 (`KiDeferredReadySingleThread` · `KiUpdateSoftParkElectionStatisticsOnInsertion` · `KiComputeThreadAffinity` · `KiSwapThread` · `SwapContext` · `KiProcessThreadWaitList` · `KiTryUnwaitThread`), 기다림 (`KeWaitForMutexObject` · `KiCommitThreadWait` · `KeSetEvent`), syscall 입구 (`KiSystemCall64` · `KiSystemServiceUser` · `KiSystemServiceStart`) 와 `int3`·페이지 오류 입구 (`KiBreakpointTrap` · `KiPageFault`), `PspSchedulerSharedDataRegionSlotAllocate`.
- **줄기 밖 (이름만)**: `KiAcquireProcessLockExclusive`·`KiReleaseProcessLockExclusive`·`KiAcquireKobjectLockSafe` (락), `KiAdjustProcessIdealProcessorSetsForThreadCreation`·`KeSelectIdealProcessor` (이상적 프로세서 고르기 — 값 `1` 의 계산 본문), `KiInitializeForegroundBoostThread` (프로세스 `ForegroundProcess` 비트가 서야 타는 갈래, 안 탐), `KiCommitRescheduleContext…`·`KiSearchForNewThread…` (다음 스레드 고르기, run71), `KiStackAttachProcess`·`KiAttachProcess` 계열 (`SavedApcStateFill`), `PfProcessCreateNotification` (프리페처 `[추측]`), 파일 I/O 함수들 (`IopReadFile`·`IopWriteFile`·`IopSynchronousServiceTail`), `ExpFindCurrentThread` 등 실행 리소스 함수 (`ResourceIndex`), `KeSwappablePageReference`·`KeSwappablePageGetLockedAddress` (공유 표 쪽), `ZwAllocateVirtualMemory`, `KiExceptionDispatch`·`KiDispatchException` (예외 길).
- 칸마다 경로·근거는 위 1.1·1.2·2.1~2.5·3 표. 3장 양식 줄은 아래 "baggage.md 에 넣을 줄".

## 질문마다 "지금 우리 (PE_emulator) 맞나"

| 질문 | 한 줄 |
|---|---|
| 1. `K2~K3` 15칸 | `ThreadListEntry` = **맞음** (`Process.cpp` 줄 1687 이 `_ETHREAD +0x578` 을 `_EPROCESS +0x370` 꼬리에 단다 — 이 결과서 `+0x6e3`·`+0x239 ~ +0x243` 와 같음). `KeStartThread`·`KeSuspendThread` 는 에뮬레이터가 "안 함" (줄 1682·1692) → `Priority`·`QuantumTarget`·`UserIdealProcessor` … 아홉 칸·`CrossThreadFlags` 우선순위 비트·`KernelStackReference` 는 **아직 없음 (빈 칸, 틀린 건 아님)**. `Src/nt/Process.cpp` 줄 2046 ~ 2056 이 "입력 칸을 쓰는 `KeInitThread`·`KeStartThread` 쪽 (internals Q78d4) 을 안 해 값 고르는 곳이 마스크·그룹을 준다" 고 이 요청을 기다리고 있다 — `UserIdealProcessor` 1 · `UserAffinity` 는 프로세스 `Affinity` 사본이라는 이 결과서 줄이 그 값. |
| 2. `K3~P0` 이후 42칸 | **확인 필요.** `Src/nt/Scheduler.h` 줄 9 가 `QuantumTarget 0x7f2814` 를 언급 (맞음 — 이 결과서 1.2). 스케줄러 칸 (`State`·`Running`·`ContextSwitches`·`WaitTime`·`WaitBlock*`) 을 에뮬레이터가 칸으로 가지는지는 `Scheduler.cpp` 를 안 읽어서 모름. |
| 3. `_KTRAP_FRAME` 25 | **맞음 (범위 안)** — 위 3장 끝. 다만 `KTHREAD +0x80/+0x88/+0x90` 은 `Src/nt` 에 없음 (확인 필요). |
| 4. 간접 호출 | **맞음.** 에뮬레이터는 시스템 서비스 고르기를 자기 표로 한다 (`Syscall.cpp` `KiSystemCall64` 훅 → 팀 syscall) — 이 결과서의 `CALL RAX` (`KiSystemServiceStart +0x193`) 자리에 해당. |
| 5. 줄기와 경로 | **확인 필요** (3장 양식 줄은 flow-writer 가 옮긴 뒤 에뮬레이터 문서와 대조). |

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지.

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| D1 | 커널 전역 값: `KiCpuSetSequence` (nt+`0xfc6c38`, 기대 `0x18` — `AffinityVersion` P0 `0x18` 과 같아야 함), `KiCyclesPerClockQuantum` (nt+`0xfc5094`, 기대 `4166666` = `0x3f940a`), `KiKvaShadow` (nt+`0x1201840`), `KiDynamicHeteroCpuPolicyExpectedCycles` (nt+`0xfc5374`), LSTAR (`rdmsr c0000082`, `KiSystemCall64` rva `0x6c1840` 인지 `KiSystemCall64Shadow` rva `0xbcb200` 인지), `KeSoftParkedQueueThreshold` (nt+`0xf22998`) | `dd`·`dq` 한 번씩, `rdmsr` 한 번 | **전역은 가능** (덤프에 있음, Q79 에 읽기 추가). LSTAR 는 레지스터라 **불가** (라이브) |
| D2 | **`P0~P1` 에 누가 파일을 한 번 읽었나** (`ReadOperationCount` 1 · `ReadTransferCount` 1862 · `OtherOperationCount` 2) + **누가 attach 했나** (`SavedApcStateFill`) | 새 스레드 주소 (`P0` 의 `!thread -1 0`) 를 얻은 뒤 `ba w8 <스레드>+0x380` (읽기 횟수) · `ba w8 <스레드>+0x258` (`SavedApcState` 머리) — 걸리면 `k` 로 호출 스택. 기대: `PspUserThreadStartup → PspNotifyThreadCreation → PfProcessCreateNotification …` 아니면 다른 길 | **불가** (사진은 구간 끝 값만) |
| D3 | **`SystemCallNumber` `0xd`·`0xe`·`0x1d3` 은 어느 `Nt*` 인가** + 첫 스레드가 `P1~P1a` 에 부르는 syscall 목록 | `ba w8 <스레드>+0x80` (또는 `nt!KiSystemServiceStart` 에 조건부 멈춤점) 로 번호를 모으고 `KiServiceTable` 로 풀기 | **일부 가능**: 사진에 마지막 세 개 번호만. 목록 전체는 불가 |
| D4 | `ResourceIndex` (`+0x460`) 가 처음 1 이 되는 순간의 리소스 (`P1~P1a`) | `ba w1 <스레드>+0x460` → `k` | **불가** (낮은 우선순위 — 칸 표는 `[추측]` 로 닫혀 있다) |
| D5 | `KiQueueTebUpdateApc` 가 첫 스레드 초기화 동안 한 번도 안 불리는지 (정적: 호출 길 없음) | `bp nt!KiQueueTebUpdateApc` (조건: 현재 프로세스 = test.exe) 를 `P0 ~ P2` 사이에 걸고 안 걸리면 확정 | **불가**. 낮은 우선순위 |

- 급한 것은 **D2** (사진이 말하는데 정적으로 못 닫음). D1 은 사진 읽기 추가로 닫힌다.
- 디버거가 붙어서 달라지는 점: `P1` 이 `int3` 라 `KiBreakpointTrap` 이 첫 프레임을 만든다 — 디버거 없이는 첫 syscall (`S1`) 이 첫 덮임일 수 있다 (사진은 디버거가 붙은 길). 또 `P1a`·`P1b`·`P2` 도 `int3`.
- Q35 (간접 호출) 에 새로 넣을 것: **`KiSystemServiceStart +0x193 / +0xa24 / +0xa7f CALL RAX`** (시스템 서비스 본체 고르기 — 첫 스레드가 부르는 `Nt*` 목록은 D3).

## 다음 요청 후보

1. **정적 `PfProcessCreateNotification` 본문** (`PspNotifyThreadCreation +0xda` 가 부름, rva `0x944508`) — 이 안에 프로세스에 붙기 (`KiStackAttachProcess`) 와 파일 읽기가 있는지. D2 의 정적 대응. (이미 `PfSnPopulateReadList +0x85`·`PfSnAppLaunchScenarioControl +0xb9` 가 `KiStackAttachProcess` 를 부른다는 것은 이번 훑기로 앎.)
2. **정적 `KiResumeThread` + `KiTryUnwaitThread` 의 초기화 (`Initialized`) 상태 갈래** — `K3~P0` 에서 `State` 가 0 → (1/7) → 2 로 가는 중간 줄 (run75·77 이 일부) — 새 스레드를 처음 풀 때의 `State`·`WaitRegister`·`WaitBlock*` 쓰기를 한 줄로.
3. **정적 `PspSchedulerSharedDataRegionSlotAllocate` 마무리** (sast/107 후보와 겹침) — 표 한 칸 크기·스레드마다 다른 칸을 나누는 법·`Retrieve` (`rva 0x703db4`) 가 읽는 줄. 이번엔 `_KTHREAD`·`_ETHREAD` 칸 쓰는 줄만 봤다.
4. **정적 `ExpFindCurrentThread`·`ExpFindEmptyEntry`** — `ResourceIndex` 첫 줄 (낮은 우선순위).
5. **정적 `KeSelectIdealProcessor`** — `UserIdealProcessor`·`IdealProcessor` 값 `1` 의 계산 본문 (프로세스 이상적 프로세서 묶음에서 어떻게 고르나, 에뮬레이터가 CPU 수를 어떻게 줄지).
6. 동적 D1·D2 를 한 번에 묶은 dast (Q79 읽기 + D2 `ba w8`).

## baggage.md 에 넣을 줄 (3장 양식)

3.13 · 3.14 · 3.15 머리의 "Q78d4 에서 볼 것" 메모들은 이 줄로 옮기고 "(해결됨 → run NN)" 으로 바꾸면 된다. 근거 칸 `sast108 · Q78d4 로 닫힘`.

### 3.13 `_ETHREAD` 에 넣을 줄 (10)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x578` `ThreadListEntry*` | ① | 프로세스 스레드 목록 (`_EPROCESS +0x370 ThreadListHead`) 꼬리에 달기: 칸 주소 `LEA`, 자기 `Flink` = 머리 · `Blink` = 옛 꼬리, 옛 꼬리 `Flink`·머리 `Blink` 를 이 칸으로 | 직접 | nt | `①P… +0x6e3 (LEA) · +0x239 · +0x23c · +0x240 · +0x243` (`①P` = `NtCreateUserProcess → PspInsertThread (호출 +0x1015)`) | `[정적]` sast108 · 정확 · 사진 `K2~K3` (K3 = `EPROCESS+0x370` 자기 연결) → 바뀜 `P1a~P1b` (둘째 스레드 달림) · Q78d4 로 닫힘 |
| `+0x5a0` `CrossThreadFlags*` | ① | DWORD 통째 되씀: 읽고 (`+0x617`) 비트 9~14 만 바꿔 두 번 씀. 앞에서 `+0x490` 이 `ThreadInserted` (비트 1) 켬. 사진 `0x5402` | 직접 | nt | `①P… +0x632 · +0x649` | `[정적]` sast108 · 정확 · 사진 `K2~K3` (P2 까지 그대로) · Q78d4 로 닫힘 (비트 칸 19개 다시 셈은 아래 2장 줄) |
| `+0x5a0` `CrossThreadFlags.ThreadIoPriority` (9:3) | ① | `_EPROCESS +0x1f4 Flags.DefaultIoPriority` (27:3) 를 9 로 옮김. 사진 2 | 직접 (프로세스 칸 복사) | nt | `①P… +0x61e ~ +0x632` | `[정적]` sast108 · 정확 · 사진 `K2~K3` |
| `+0x5a0` `CrossThreadFlags.ThreadPagePriority` (12:3) | ① | `_EPROCESS +0x1f0 Flags2.DefaultPagePriority` (12:3) 복사. 사진 5 | 직접 (프로세스 칸 복사) | nt | `①P… +0x63b ~ +0x649` | `[정적]` sast108 · 정확 · 사진 `K2~K3` |
| `+0x5ec` `KernelStackReference` | ① | 상수 1 (`R13D = 1`). 바로 앞 `+0x2c8` 에서 스레드 객체 참조 2 더함 (`'Dflt'`) | 직접 | nt | `①P… +0x2cd` | `[정적]` sast108 · 정확 · 사진 `K2~K3` → 뒤에 바꾸는 쪽 (이름만) `PsReferenceKernelStack`·`PsDereferenceKernelStack`·`PspReaper +0x3b` |
| `+0x6f8` `LastExpectedRunTime*` | ① | 준비될 때 스레드 `ExpectedRunTime` (`+0x54`) 사본. 사진 P0 `0x4c4b40` | 직접 | nt | `R… KiDeferredReadySingleThread +0x151d` (같은 계열 `KiAddThreadToPrcbQueue +0x154 · +0x283`, `KiQueueReadyThread +0x41d`, `KiUpdateLocalReadyQueueStatisticsOnInsertion +0x22`) | `[정적]` sast108 · 정확 · 사진 `K3~P0` → 바뀜 `P0~P1`·`P1a~P1b`·`P1b~P2` (준비될 때마다) |
| `+0x708` `LastSoftParkElectionGroupAffinity` (16B) | ① | 16B 0 → 그룹 (PRCB `+0x2c6`) → 마스크 (스레드 `Affinity` 비트맵). 사진 P0 `3` | 직접 | nt | `R… KiDeferredReadySingleThread +0x152b → KiUpdateSoftParkElectionStatisticsOnInsertion +0x5b · +0x69 · +0x7e` | `[정적]` sast108 · 정확 · 사진 `K3~P0` |
| `+0x724` `LastSoftParkElectionQos*` | ① | 스레드 `BamQosLevel` (`+0x204`) 사본. 사진 P0 6 → P1 0 | 직접 | nt | 같은 함수 `+0x1f` | `[정적]` sast108 · 정확 · 사진 `K3~P0` → 바뀜 `P0~P1` |
| `+0x726` `LastSoftParkElectionRunningType` | ① | `max(CurrentRunTime, ExpectedRunTime) ≥ KiDynamicHeteroCpuPolicyExpectedCycles` (전역). 사진 1 | 직접 | nt | 같은 함수 `+0x41` | `[정적]` sast108 · 정확 · 사진 `K3~P0` |
| `+0x6e0` `SchedulerSharedDataObject` | ② | 스케줄러 공유 표 영역 객체 포인터 (입력: 프로세스에 붙은 `PspSchedulerSharedDataType` 객체). 이미 있으면 거절 (`0xc00000bb`) | syscall (test.exe 가 부름) | nt | `SC → NtSetInformationThread (호출 +0xc2c, 클래스 0x39) → PspSchedulerSharedDataRegionSlotAllocate +0x1d1` | `[정적]` sast108 · 정확 · 사진 `P1~P1a` (`…995fbe10` = `KTHREAD +0x468` 과 같은 주소) · Q78d4 로 닫힘 |

**3.13 머리에 넣을 숫자**: 칸 149 · 아는 칸 **58** (정확 58) · 아무도 안 씀 22 · 범위 밖 길 `[추측]` 69 · 모름 **0** · (옵션: `CrossThreadFlags` 되쓰기로 다시 세면 아는 칸 75 · 아무도 안 씀 19 · 범위 밖 55).

### 3.14 `_KTHREAD` 에 넣을 줄 (47칸 — 아는 칸 45 + 범위 밖 2 — 을 같은 줄끼리 묶어 29줄)

(`①S` = `①P → KeStartThread (호출 +0x5dd)`, `R`·`U`·`SC` 는 위 약어.)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x20` `QuantumTarget*` | ① | `KPROCESS +0x91 QuantumReset` × 전역 `KiCyclesPerClockQuantum` (`0x7f2814`) | 직접 (프로세스 칸 × 전역) | nt | `①S… +0x1dc` (`+0x1c7 IMUL`) | `[정적]` sast108 · 정확 · 사진 `K2~K3` → 바뀜 `P1a~P1b` (`KiQuantumEnd +0x49b` 등, run76) · D1 (`KiCyclesPerClockQuantum`) |
| `+0x78` `ThreadFlags.SharedReadyQueueAffinity` (비트 13) | ① | 스레드 `Affinity` 가 준비 줄 CPU 마스크를 모두 덮으면 1 (`XOR.LOCK 0x2000`) | 직접 (계산) | nt | `①S… +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` | `[정적]` sast108 · 정확 · 사진 `K2~K3` · **정정: run52 줄 `+0xb9 · +0x1b7` 은 이 비트가 아님** |
| `+0xc3` `Priority*` · `+0x233` `BasePriority` | ① | 프로세스 `BasePriority` (`+0x90`) 복사 (8) | 직접 (프로세스 칸 복사) | nt | `①S… +0xc8` (`Priority`) · `+0xc2` (`BasePriority`) | `[정적]` sast108 · 정확 · 사진 `K2~K3` → `Priority` 바뀜 `P1a~P1b` (9, `KiDeferredReadySingleThread +0x1ae`·`KiUpdateThreadPriority +0x41`) |
| `+0xc4` `UserIdealProcessor` · `+0x24c` `IdealProcessor` | ① | `KeSelectInitialIdealProcessorForThread(프로세스)` 결과 (입력 `R8` ≠ 0 이면 입력값 + `ThreadFlags` 비트 0). 사진 1 | 직접 (계산) | nt | `①S… +0x173 (계산) → +0x17c (User…) · +0x187 (…)` | `[정적]` sast108 · 정확 · 사진 `K2~K3` (P2 까지 그대로 — 첫 스레드의 최종) · 값 계산 본문은 이름만 (`KeSelectIdealProcessor`) |
| `+0x28b` `QuantumReset` | ① | 프로세스 `QuantumReset` (`+0x91`) 복사 (2) | 직접 | nt | `①S… +0x1c1` | `[정적]` sast108 · 정확 · 사진 `K2~K3` |
| `+0x2f8` `ThreadListEntry*` | ① | 프로세스 `_KPROCESS +0x30 ThreadListHead` 꼬리에 달기 (프로세스 목록 잠금 안) | 직접 | nt | `①S… +0x1e9 (LEA) · +0x2f4 · +0x2f7 · +0x2fb · +0x2fe` | `[정적]` sast108 · 정확 · 사진 `K2~K3` → 바뀜 `P1a~P1b` (둘째 스레드) |
| `+0x31b` `WobPriority` | ① | 상수 `0x20` | 직접 | nt | `①S… +0xce` | `[정적]` sast108 · 정확 · 사진 `K2~K3` |
| `+0x258` `SavedApcState*` (공용체 꼬리 `SuspendCount` +0x284) | ① | `CREATE_SUSPENDED` 길에서 `SuspendCount` = 1 (목록 머리 쪽 43B 는 아래 `SavedApcStateFill`) | 직접 | nt | `①P… +0x2e0 → KeSuspendThread +0x60` | `[정적]` sast108 · 정확 · 사진 `K2~K3` (K3 바이트 +0x284 = 1, P0 0) |
| `+0x48` `CycleTime*` · `+0x50` `CurrentRunTime*` · `+0x154` `ContextSwitches*` | ① | **카운터 (계속 바뀜)**: 사이클 누적 (`KiEndThreadCycleAccumulation +0x1bc / +0x1e7` · `KiCaptureTotalCyclesCurrentThread +0x1d8 / +0x203` · `KiUpdateTotalCyclesCurrentThread +0x134 / +0x163`), 0 으로 `KiSwapThread +0x5a`, `+1` `SwapContext +0x5bb` | 직접 | nt | `R·스케줄러` | `[정적]` sast108 · sast71/72 · 정확 · 사진 `K3~P0` → 계속 바뀜 |
| `+0x71` `Running` · `+0x184` `State` | ① | 처음 CPU 를 받을 때 `Running` = 1 (`SwapContext +0x18`), `State` = 2 (`KiSwapThread +0x190`). 준비 중엔 `State` 1 (`KiDeferredReadySingleThread +0x122e`)·7 (`KiInsertDeferredReadyList +0x33`) | 직접 | nt | `R → KiExitDispatcher → … KiSwapThread → KiSwapContext → SwapContext` | `[정적]` sast108 · sast71 · 정확 · 사진 `K3~P0` |
| `+0xd8` `WaitListEntry*` · `SwapListEntry` (겹침) | ① | 준비 줄 (PRCB 안 `ReadyListHead[우선순위]`) 에 달 때 쓰는 고리 — 꼬리 `+0x173d~+0x1747`, 머리 `+0x1831~+0x183c`. 줄에서 빠진 뒤에도 옛 값이 남음 | 직접 | nt | `R… KiProcessThreadWaitList +0x101 → KiDeferredReadySingleThread +0x171f` | `[정적]` sast108 · sast71 · 정확 · 사진 `K3~P0` → 바뀜 `P0~P1`·`P1a~P1b`·`P1b~P2` (`SwapListEntry` 이름으로 쓰는 쪽 `KiInSwapSingleProcess +0x98` `[추측]`) |
| `+0x1b4` `WaitTime*` | ① | 준비 때 KUSD `TickCount` 아래 32비트 (`[0xfffff78000000320]`) 적음, 기다릴 때도 | 직접 | nt | `R… KiDeferredReadySingleThread +0x11f8` · `KeWaitForSingleObject +0x1c5` | `[정적]` sast108 · 정확 · 사진 `K3~P0` (`0x14dff`) → 계속 바뀜 (run77 `[추측]` 닫음) |
| `+0x238` `AffinityVersion` | ① | 전역 `KiCpuSetSequence` 사본 (`0x18`) — 스레드 값 ≠ 전역 이면 `KiComputeThreadAffinity` 호출 | 직접 | nt | `R… KiDeferredReadySingleThread +0x2a9 (비교) · +0x1877 → KiComputeThreadAffinity +0x212` | `[정적]` sast108 · 정확 · 사진 `K3~P0` · D1 (`KiCpuSetSequence`) |
| `+0x70` `WaitRegister*` · `+0xd0` `WaitBlockList` · `+0x140` `WaitBlockFill4*` · `+0x24b` `WaitBlockCount` | ① | 기다림 한 번 (`P0~P1`): `WaitRegister` 0 → `State` 1 (`KiCommitThreadWait +0xbf`) → 깨움 2·6, `WaitBlockList` = 스레드 + `0x140`, `WaitBlock[0]` 줄 연결·`WaitType` 1·`BlockState` 4·`WaitKey`, `WaitBlockCount` 1 | 직접 | nt | `U 또는 커널 I/O 길 → KeWaitForSingleObject (+0x116 · +0x1d8 · +0x1dd · +0x1e2 · +0x7d2) → KiCommitThreadWait (+0x5b · +0xbf)` | `[정적]` sast108 · sast73 · 정확 · 사진 `P0~P1` → 계속 바뀜 (누가 기다렸나는 D2) |
| `+0x380` `ReadOperationCount` · `+0x398` `ReadTransferCount` · `+0x390` `OtherOperationCount*` · `+0x388` `WriteOperationCount*` · `+0x3a0` `WriteTransferCount*` | ① → ② | **카운터 (계속 바뀜)**: 파일 일 한 번마다 `inc` / `add` — 읽기 `IopReadFile` 꼬리 `FUN_1408e868f +0x25e`, 쓰기 `IopWriteFile +0x1ed`, 그 밖 `NtQueryInformationFile +0x3a9` 등 12곳, 바이트 `IopUpdateIrpTransferCount +0x86 / +0xd0 / +0xb2 / +0xee` | 직접 (syscall 이 쓰는 쪽 `syscall (test.exe 가 부름)` + 커널 I/O) | nt | `SC → Nt{Read,Write,Query…}File → IopSynchronousServiceTail …` · 첫 읽기는 `P0~P1` 커널 길 | `[정적]` sast108 · 정확 (함수·줄) · 사진 `P0~P1` (읽기 1·1862·기타 2) → `P1~P1a`·`P1a~P1b`·`P1b~P2` · **`P0~P1` 읽기의 호출자는 D2** |
| `+0x3e0` `KernelWaitTime*` · `+0x3e8` `UserWaitTime` | ① | **카운터**: 깨울 때 기다린 틱 더함 (`WaitMode` 0 → `KernelWaitTime`, ≠0 → `UserWaitTime`) | 직접 | nt | `KiTryUnwaitThread +0x142 (User) · +0x1bb (Kernel)` · `KeSetEvent +0x456 · +0x4f2` | `[정적]` sast108 · sast73 · 정확 · 사진 `P0~P1` (`KernelWaitTime`) · `P1a~P1b` (`UserWaitTime`) |
| `+0x80` `SystemCallNumber*` · `+0x88` `FirstArgument*` | ② | 시스템 호출마다 서비스 번호 (`EAX`)·첫 인자 (`R10`) 를 덮음 | 직접 | nt | `SC… KiSystemServiceUser +0xd2 (번호) · +0xcb (첫 인자)` | `[정적]` sast108 · 정확 · 사진 `P1~P1a` (`0xd`·`-2`) → `P1a~P1b`·`P1b~P2` |
| `+0x90` `TrapFrame` | ② | 지금 트랩 프레임 주소 (`InitialStack − 0x190`) | 직접 | nt | `SC… KiSystemServiceStart +0x0` (+ 되돌림 `+0x5c5 · +0x939`) | `[정적]` sast108 · 정확 · 사진 `P1~P1a` |
| `+0x283` `WaitReason*` | ①·② | 기다림 이유 (인자): 첫 기다림 0 `Executive`, 그 뒤 `0x1f` (`KiDispatchInterrupt +0x13e`) · 6 (`UserRequest`) · `0x20` · `0x1e` | 직접 (인자·상수) | nt | `KeWaitForSingleObject +0x1b0` · `KiDispatchInterrupt +0x13e` · `KiExitDispatcher +0x187` · `KiQuantumEnd +0xb98` | `[정적]` sast108 · sast73 · 정확 · 사진 `P1~P1a` (0 이 아닌 첫 값; 첫 쓰기는 `P0~P1`) → 계속 바뀜 |
| `+0x320` `SchedulerSharedSystemSlot` · `+0x468` `SchedulerSharedSwappablePage` | ② | 스케줄러 공유 표 한 칸의 시스템 주소와 `_KSWAPPABLE_PAGE` 포인터 (`SchedulerSharedOffset` `+0x464` 도 같은 줄이 씀) | syscall (test.exe 가 부름) | nt | `SC → NtSetInformationThread (호출 +0xc2c, 클래스 0x39) → PspSchedulerSharedDataRegionSlotAllocate +0x1ca (SystemSlot) · +0x1a4 (SwappablePage)` | `[정적]` sast108 · 정확 · 사진 `P1~P1a` · Q78d4 로 닫힘 |
| `+0x78` `ThreadFlags.AutoBoostEntriesExhausted` (비트 16) | ② | `KernelAbEntries` 빈 칸이 없을 때 켬 (`lock bts`) | 직접 | nt | `KeAbPreAcquire +0x52` (푸시락 잡는 길 어디서든) | `[정적]` sast108 · 정확 · 사진 `P1a~P1b` |
| `+0x84` `ReadyTime*` | ② | 준비 줄에서 기다린 시간 더함 (**카운터**) | 직접 | nt | `KiSwapThread +0x317 · +0x3af` · `KiDispatchInterrupt +0x283` · `KiUpdatePriorityMatrixForRunningTransition +0x53` | `[정적]` sast108 · sast71 · 정확 · 사진 `P1a~P1b` |
| `+0x187` `WaitMode*` · `+0x237` `AdjustIncrement*` | ② | 기다림 인자 `WaitMode` (0/1), 깨움 `Increment` | 직접 (인자) | nt | `KeWaitForSingleObject +0x11a` (`WaitMode`) · `KiProcessThreadWaitList +0x9f` (`AdjustIncrement`) | `[정적]` sast108 · sast73 · 정확 · 사진 `P1a~P1b` (0 이 아닌 첫 값; 첫 쓰기 0 은 `P0~P1`) → 바뀜 `P1b~P2` |
| `+0x366` `DecayBoost` | ② | 깨움 부스트 (`Priority` 8 → 9 의 1), 퀀텀 끝에 줄어듦 | 직접 (계산) | nt | `KiDeferredReadySingleThread +0x13cf` · `KiQuantumEnd +0x704 · +0x81b` · `KiComputeNewPriority +0x7b · +0x86` | `[정적]` sast108 · 정확 · 사진 `P1a~P1b` |
| `+0x74` `MiscFlags.Alertable` (비트 4) | ② | 기다림 인자 `Alertable` (켬 `+0x126`, 끔 `+0x112`) | 직접 (인자) | nt | `KeWaitForSingleObject +0x112 · +0x126` | `[정적]` sast108 · sast73 · 정확 · 사진 `P1b~P2` |
| `+0xc8` `WaitStatus` | ② | 기다림 결과 (깨우는 쪽이 씀, `0x100` = 사용자 APC 로 깸 `[추측: 이름]`) | 직접 | nt | `KeSetEvent +0x484 …` · `KiTryUnwaitThread +0x168 · +0x2fb` · `KiCommitThreadWait +0x4fa` | `[정적]` sast108 · sast73 · 정확 · 사진 `P1b~P2` (읽는 쪽 `KiSwapThread +0x25c`) |
| `+0x218` `NextProcessor*` (비트 0~30 `NextProcessorNumber`) | ① | 고른 CPU 번호 (`PRCB +0x24` `Number`). 첫 쓰기는 `K3~P0` (CPU 0) | 직접 | nt | `R… KiDeferredReadySingleThread +0x3b5` (그 밖 `KiScheduleThreadToRescheduleContext`·`KiSearchForNewThreadsForRescheduleContext`·`KiQueueReadyThread`·`KiAddThreadToPrcbQueue`) | `[정적]` sast108 · 정확 · 사진 `P1b~P2` (사진 첫 0 아닌 값 1) — 코드의 첫 쓰기는 `K3~P0` |
| `+0x258` `SavedApcStateFill` (43B) | ① | 범위 밖 길에서만 씀 `[추측]` — 목록 머리 둘 자기 연결 (`ApcState` 를 `SavedApcState` 로 옮겼다 되돌린 흔적, `KiStackAttachProcess`·`KiAttachProcess` 계열). 이 구간에서 누가 붙었는지 못 정함 | — | nt (이름만) | — | `[추측]` sast108 · 사진 `P0~P1` · D2 |
| `+0x460` `ResourceIndex` | ② | 범위 밖 길에서만 씀 `[추측]` — 실행 리소스 (`ERESOURCE`) 주인 표 안 이 스레드의 자리 번호 (`Exp*Resource*`·`ExpFindCurrentThread +0xf7` 이 `mov byte [thread+0x460]`). `SchedulerSharedOffset` (+0x464) 와 다른 일 | — | nt (이름만) | — | `[추측]` sast108 · 사진 `P1~P1a` · D4 |

**3.14 머리에 넣을 숫자**: 칸 237 · 아는 칸 **144** (정확 144) · 아무도 안 씀 21 · 범위 밖 길 `[추측]` **72** (70 + `SavedApcStateFill` + `ResourceIndex`) · 모름 **0**. (줄 50 + 이 요청 새 줄 29 = 79.)

### 3.15 `_KTRAP_FRAME` 에 넣을 줄 (25칸, "트랩 프레임 덮임" — 짐 아님, 가름만)

3.15 머리에 한 줄: "**P1 이후는 `int3` (`KiBreakpointTrap`)·시스템 콜 (`KiSystemCall64 → KiSystemServiceUser`)·페이지 오류 (`KiPageFault`) 입구가 같은 자리 (`InitialStack − 0x190`) 를 그때그때 덮는다. 입구마다 쓰는 칸이 달라 (아래) 마지막 입구가 안 쓴 칸은 이전 입구의 값이 남는다. 첫 덮임은 `P1` 의 `int3` (사진), 24칸의 첫 값은 `P1~P1a` 의 syscall·페이지 오류 입구.**" 그리고 칸 줄 (5줄 — 칸 표가 아니라 입구별 표):

| 칸 | 단계 | 무엇을 | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `+0x0` `P1Home*` · `+0x8` `P2Home*` · `+0x10` `P3Home` · `+0x18` `P4Home*` | ② | 입구가 안 쓰는 인자 홈 칸 — 쓰는 곳: 첫 win32k 호출 앞뒤 임시 보관 (`EAX`·`RCX`·`RDX`·`R8`) + 아래 호출된 함수의 홈 `[추측]` | 덮임 | nt | `SC… KiSystemServiceStart +0x90c · +0x90f · +0x913 · +0x917` | `[정적]` sast108 · 트랩 프레임 덮임 · 사진 `P1~P1a` (`P4Home` `P1a~P1b`) |
| `+0x28` `PreviousMode` · `InterruptRetpolineState` · `+0x2a` `FaultIndicator*` · `NmiMsrIbrs*` | ② | 시스템 호출: `PreviousMode` 1. 페이지 오류: `FaultIndicator` (읽기 0·쓰기 1·실행 8) | 덮임 | nt | `KiSystemServiceUser +0x4` (= `KiSystemCall64 +0x2a6`) · `KiServiceInternal +0x49` · `KiPageFault +0x383` | `[정적]` sast108 · 트랩 프레임 덮임 · 사진 `P1~P1a` → `P1a~P1b` |
| `+0x30` `Rax*` · `+0x48 ~ +0x60` `R8*`·`R9*`·`R10*`·`R11*` | ①·② | 만들 때 CONTEXT 값 0 (`KxContextToKframes +0xdd · +0xfe · +0x109 · +0x114 · +0x11f`, run53) → 이후 모든 입구가 유저 레지스터로 덮음 | 덮임 | nt | `KiSystemCall64 +0x7f` · `KiSystemServiceUser +0x2c ~ +0x38` · `KiBreakpointTrap +0x18 · +0x24 ~ +0x30` · `KiPageFault +0x14 · +0x20 ~ +0x2c` | `[정적]` sast108 · 트랩 프레임 덮임 · 사진 `K1~K2` 0 → `P1~P1a` |
| `+0x68` `GsBase` · `GsSwap` · `+0x80` `Xmm1*` · `+0xd0` `FaultAddress*` · `ContextRecord*` | ② | 페이지 오류·NMI 입구가 유저 GS 베이스·`CR2` 를 저장, `Xmm1` 은 모든 입구가 저장 | 덮임 | nt | `KiPageFault +0x2de · +0x2e1 · +0x358` · `KiNmiInterrupt +0x54 · +0xd2` · `KiSystemServiceUser +0x40` · `KiBreakpointTrap +0x2f8` | `[정적]` sast108 · 트랩 프레임 덮임 · 사진 `P1~P1a` (`Xmm1` 값은 `dt` 로 안 읽힘 `[공백]`) |
| `+0x140` `Rbx*`·`NmiPreviousSpecCtrl*` · `+0x144` `NmiPreviousSpecCtrlPad*` · `+0x148` `Rdi*` · `+0x150` `Rsi*` · `+0x160` `ErrorCode*` · `ExceptionFrame*` | ② | 시스템 호출 입구만 `Rbx`·`Rdi`·`Rsi` 저장. `ErrorCode` 는 CPU 가 #PF·#GP 때 밀어 넣음 (int3·syscall 은 자리만 잡고 안 씀) | 덮임 | nt | `KiSystemCall64 +0x55 · +0x5c · +0x63` (`KiSystemCall64Shadow +0x6d · +0x74 · +0x7b`) · 하드웨어 | `[정적]` sast108 · 트랩 프레임 덮임 · 사진 `P1~P1a` → `P1a~P1b` |

**3.15 머리에 넣을 숫자**: 칸 66 · 아는 칸 23 · 아무도 안 씀 12 · 범위 밖 길 `[추측]` 6 · **트랩 프레임 덮임 25** · 모름 **0**. (7.2 에선 `_KTRAP_FRAME` 모름 25 → 0 으로 옮기고 "덮임" 열 신설.)

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. **run52 3.14 머리 줄 정정**: "`ThreadFlags` DWORD (`+0xb9 · +0x1b7` …) → `SharedReadyQueueAffinity`" 는 틀렸다. `KeStartThread +0xb9` 는 비트 3 `DisableBoost` (프로세스 `DisableBoost` 값, `SHL 2`), `+0x1b7` 은 비트 8 `DisableQuantum` (프로세스 `DisableQuantum` 값, `SHL 6`). 비트 13 `SharedReadyQueueAffinity` 는 **`KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55`**. (`DisableBoost`·`DisableQuantum` 은 이미 sast/46 의 "0 을 씀 20" 안 칸이고, 이제 입력값 쓰기 줄이 따로 있다 — 가름은 그대로.)
2. **`SavedApcState` 와 `SavedApcStateFill` 은 한 공용체 (48B)**: `K2~K3` 의 `SavedApcState*` 는 꼬리 바이트 `SuspendCount` (+0x284), `P0~P1` 의 `SavedApcStateFill` 은 앞쪽 목록 머리. 7.2 의 두 칸이 구간이 다른 이유.
3. **`ResourceIndex` (`+0x460`) ≠ `SchedulerSharedOffset` (`+0x464`)**: 앞은 실행 리소스 주인 표 자리, 뒤는 스케줄러 공유 표 칸 (`SlotAllocate +0x19e`). 이름만 보고 `P1~P1a` 에 한 줄로 묶지 말 것.
4. **`SchedulerSharedSwappablePage` (`KTHREAD +0x468`) 와 `SchedulerSharedDataObject` (`ETHREAD +0x6e0`) 는 같은 주소** (`…995fbe10`): 영역 객체 (`RDI`) 와 그 안 `_KSWAPPABLE_PAGE` 배열 (+`R14×0x50`) 의 첫 칸이 같은 주소로 보임 `[추측]`.
5. **`WaitTime` P0 `0x14dff` = 준비 때 `TickCount`** (`KiDeferredReadySingleThread +0x11f8`) — run77 의 "다른 쓰는 줄 `[추측]`" 이 `[정적]` 으로.
6. **`NextProcessor` 첫 쓰기는 `K3~P0`** (CPU 0) — 7.2 의 `P1b~P2` 는 값이 0 이 아니게 된 때.
7. **`P0~P1` 에 커널이 파일을 읽었다 (사진)**: 3.14 머리의 "P0~P1 = `PspUserThreadStartup` → `PspInitializeThunkContext`" 설명이 이 구간의 기다림·파일 읽기를 못 설명한다 (`PspSetContextThreadInternal` 는 지금 스레드면 안에서 바로 처리 — 기다림 없음). 7.2 의 `P0~P1` 칸 설명에 "프리페처 `[추측]`" 후보와 D2 를 적을 것.
8. **트랩 프레임 사진 해석**: `P1a`·`P1b`·`P2` 의 `P1Home ~ P4Home`·`PreviousMode`·`GsBase`·`FaultAddress`·`Rbx`·`Rdi`·`Rsi`·`ErrorCode` 는 **`int3` 입구가 안 쓰는 칸에 남은 syscall·페이지 오류 흔적**이다. 3.15 사진 줄 (run44) 에 "P1a 이후 칸 값은 이전 입구 잔재" 를 덧붙일 것.
9. **`KiSystemCall64` 는 Ghidra 함수가 아니다** (PDB `[function]` 만) — 이후 요청서에 "없으면 뽑기" 를 쓰지 말고 "objdump 로 본다" 로 쓸 것.
10. **숫자 (baggage 7.2)**: `_ETHREAD` 모름 10 → **0**, `_KTHREAD` 모름 47 → **0**, `_KTRAP_FRAME` 모름 25 → **0** — 세 구조체의 "모름" 이 이 요청으로 모두 닫힘 (총 모름 82 → 0). 아는 칸 합: `_ETHREAD` 58, `_KTHREAD` 144, `_KTRAP_FRAME` 23 (+ 덮임 25).

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 두 번 (8초), `pdb_struct.py`·`pdb_aliases.py --where` 모두 문제없이 돌았다.
- **제안 1 (스크립트 문제 절과 같음)**: PDB 에만 있는 함수 (`KiSystemCall64`·`KiSystemCall64Shadow`) 를 뽑는 길.
- **제안 2 (sast/43·44·46·47 제안을 다시)**: `field_writers.py` — 칸 오프셋으로 쓰는 줄을 `함수 + 오프셋 + 명령` TSV 로 뽑고 (이번엔 scratchpad `wscan.py` 로 했다), **트랩 프레임 입구용 `rbp` 보정** (`RBP = 프레임 + 0x80`, 쓰기 `[RBP+d]` → 프레임 칸 `d + 0x80`, 조각 포함) 과 **callers 표** (`callers.py`: "누가 이 함수를 부르나" 가 `_calls.tsv` 에는 안 나옴 — 호출 명령 `call 0x…` 로 훑음) 가 하네스에 있으면 이번 같은 요청이 한결 빠르다. 이번에 쓴 임시 스크립트: `snap.py`·`vals.py` (사진 7장 칸 값 뽑기), `wscan.py`·`rbpscan2.py`·`callers.py`·`dis.py`·`tfw.py` (scratchpad `s108`).
- **제안 3**: `ms/logs/04_baggage-read_*.txt` 의 `02c ETHREAD_RAW` 를 칸 이름별로 읽어 주는 작은 스크립트 (구간별 값 표 TSV) — sast/46·47·48·108 이 매번 따로 짰다.

## 이름 확인 결과

결과서의 backtick·표 안 함수 이름 후보 중 함수처럼 생긴 것을 `ms/decomp/ntoskrnl/_index.tsv` (150개 일치) · PDB 공개 심볼 (`pubs`, `llvm-pdbutil -publics`) · `ms/decomp/ntdll/_index.tsv` 와 맞춰 봤다 (scratchpad 스크립트). **함수 이름이 빠진 것은 0개다.** `_index.tsv` 에 없는 것은 모두 다음 중 하나다.
  - 구조체 칸 이름·레지스터·명령어·사진 지점 (`Rax`·`State`·`P1a` · `SWAPGS` …).
  - **PDB 에만 있는 이름 (Ghidra 가 함수로 안 잡음 또는 별칭)**: `KiSystemCall64` (rva `0x6c1840`)·`KiSystemCall64Shadow` (rva `0xbcb200`) — PDB `[function]`, `pdb_aliases.py --where` 가 "어느 함수 범위에도 안 듦" 이라 함; `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject`, `KeYieldExecution` = Ghidra `Ordinal_6` (rva `0x2137a0`) — 문서에는 PDB 이름을 쓰고 Ghidra 이름을 위에서 한 번 밝힘; `KiSystemServiceCopyEnd` (PDB `none` 라벨, 점프표 꼬리 — 이름만 가리킴).
  - **전역 변수** (PDB 에 있음, `.asm` 주석 주소와 같음): `KiCyclesPerClockQuantum` (nt+`0xfc5094`)·`KiCpuSetSequence` (nt+`0xfc6c38`)·`KiDynamicHeteroCpuPolicyExpectedCycles` (nt+`0xfc5374`)·`KeSoftParkedQueueThreshold` (nt+`0xf22998`)·`KiKvaShadow` (nt+`0x1201840`)·`PspSchedulerSharedDataType` (nt+`0xfc7058`)·`KeServiceDescriptorTable`·`KiServiceTable`.
  - **ntdll 함수** (nt 가 아님, `ms/decomp/ntdll/_index.tsv` 에서 확인): `LdrInitializeThunk`·`LdrpAcquireSchedulerSharedDataSlot`·`LdrpCallTlsInitializers`·`LdrpThreadTokenSetImpersonationToken`·`RtlAbPostRelease`·`RtlEnterCriticalSection`·`RtlLeaveCriticalSection`·`RtlReleaseSRWLockShared`·`RtlReleaseSRWLockExclusive`·`RtlpAbFreeKernelEntry` (본문 읽기 아님, 호출 자리 grep 만).
  - `FUN_1408e868f` (= `IopReadFile` 꼬리 조각, `_index.tsv` 에 있음, `IopReadFile +0x284` 가 점프), `Feature_StackingDecayBoosts` 는 `Feature_StackingDecayBoosts__private_IsEnabledNoReportingNoInline` 의 줄임.

## 판정

**분석성공.** 요청서 질문 1~5 모두 답했다. 82칸 → 아는 칸 (nt) **55** · 범위 밖 길 `[추측]` **2** (`SavedApcStateFill`·`ResourceIndex`) · 트랩 프레임 덮임 **25** · 여전히 모름 **0**. `CrossThreadFlags` 비트 칸 19개 다시 셈: 조건부 2 · 0 을 씀 17 (DWORD 되쓰기 `PspInsertThread +0x632 / +0x649`, 바뀌는 칸은 `ThreadIoPriority`·`ThreadPagePriority`). PDB 일치 (`C29EBFB06B78B3C020DCA66D99713F9E1`), `_calls.tsv` 호출 지점 (`+0xc36`·`+0x807`) 과 사진 값이 칸마다 일치 → 버전 일치. 정적으로 못 닫은 것: `P0~P1` 의 커널 파일 읽기·attach 의 호출자 (D2), `ResourceIndex` 첫 리소스 (D4), 첫 스레드가 부르는 `Nt*` 목록 (D3), LSTAR 가 `KiSystemCall64` 인지 `…Shadow` 인지 (D1). 사진 구간과 어긋나는 것: `P0~P1` 파일 읽기 (2.7 의 1·2), `NextProcessor` 첫 쓰기 구간, 트랩 프레임 첫 덮임 (`P1` 의 `int3`, 요청서 "첫 syscall" 과 다름).
