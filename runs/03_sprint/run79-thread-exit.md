# 2026-11-12 run79 — 스레드 끝내기 (Q141d, PE_emulator 요청 25 ㄹ): **끝나는 스레드가 자기 문맥에서 정리한다. 커널은 TEB·유저 스택을 먼저 풀고, 맨 끝에 `KeTerminateThread` 가 스레드 객체 신호를 켜고 기다리던 쪽을 모두 깨운다.** `NtTerminateThread +0x41` → `PspTerminateThreadByPointer +0x4a` → `PspExitThread`: `+0x18e ActiveThreads −1` → (마지막 아니면) `+0x3f2 ZwFreeVirtualMemory(TEB.DeallocationStack, MEM_RELEASE)` → `+0x4b8 MmDeleteTeb` → `+0x54c KeTerminateThread` (`+0x116` KPROCESS 목록에서 빼기 · `+0x16c SignalState = 1` · `+0x19b KiTryUnwaitThread` 모두 깨움 · `+0x1b0 State = 4` · `+0x24f KiSwapThread`, 안 돌아옴). 마지막 스레드는 TEB·스택을 따로 안 풀고 프로세스 정리 (`PspExitLastThread`) 로 간다. 남의 스레드는 종료 요청 (APC) 만 건다. 핸들 0 은 "마지막이면 `0xc00000db`" 검사가 붙은 자기 끝내기

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "`PspExitThread` 이름으로 끝내기, 마지막 갈래 있음, TEB·스택 풀기는 공사 중" 은 **줄기는 맞고, 빠진 일이 넷** 이다 `[정적]`.
  - **`ActiveThreads −1`** (`PspExitThread +0x18e`, 프로세스 락 안). 0 이 되면 "마지막" 으로 갈린다 (`+0x1ae` · `+0x1bd`).
  - **TEB·유저 스택 풀기**: 커널이, **끝나는 스레드 자신이**, `KeTerminateThread` 보다 **앞에서** 한다. 유저 스택 = `TEB.DeallocationStack` 을 그 자리에서 읽어 예약 영역을 통째로 `MEM_RELEASE` (`+0x3f2`), TEB = `MmDeleteTeb` (`+0x4b8`). 마지막 스레드는 둘 다 건너뛴다. `SameTebFlags` 는 안 본다.
  - **기다리던 쪽 깨우기는 `KeTerminateThread` 안**: `+0x16c` 신호 1 → `+0x19b` 기다림 블록마다 `KiTryUnwaitThread(…, WaitKey)` → 목록 비움. 수동 이벤트 꼴 (run77) — 모두 깨우고 신호는 남는다. test.exe 메인의 `WaitForSingleObject(t)` 는 `WaitKey 0` 이라 `WaitStatus = 0` `[정적: 길]`.
  - **스레드 목록이 둘**: `KPROCESS` 쪽 (`+0x30`) 은 `KeTerminateThread +0x116` 이 빼고, `EPROCESS` 쪽 (`+0x370`) 은 **객체가 지워질 때** (`PspThreadDelete +0x374`) 까지 남는다. `PspExitThread` 는 어느 목록에서도 안 뺀다.
- **PE_emulator 원문의 `NtTerminateThread(0, 0)`** (`../PE_emulator/_workspace/00_planner/201_deadlock-waiters.md` "가." 표 2줄, seq 384 `by team`):
  - 핸들 **0** 은 그냥 "자기" 가 아니라 **"자기 + 마지막 스레드 검사"** 갈래다: `NtTerminateThread +0x2e` 가 프로세스 `ActiveThreads` 를 읽고 **`+0x34 CMP 1 / +0x37 JZ`** → 1 이면 끝내지 않고 `+0x59` **`0xc00000db`** 를 돌려준다. 1 이 아니면 `+0x41` 자기 끝내기. 핸들 `-2` 와 자기를 가리키는 진짜 핸들은 이 검사 없이 자기 길 (`+0x60/+0x64`, `+0xa5/+0xa8 → +0xd4`).
  - test.exe Worker 가 끝날 때는 메인이 `WaitForSingleObject(t)` 에서 기다리는 중이라 `ActiveThreads ≥ 2` → `+0x41` 자기 길을 탄다 `[추측: 순서]`.
  - 유저 쪽이 먼저 묻는다 `[정적]` (flow-writer, 있던 `ms/disassemble/ntdll/RtlExitUserThread.asm`): `RtlExitUserThread +0x29 NtQueryInformationThread(-2, 0xc, 4바이트)` (원문 seq 357 과 같은 호출) → 0 아니면 (마지막) `+0x3b RtlExitUserProcess` → 아니면 `+0x41 LdrShutdownThread` → `+0x48 TpCheckTerminateWorker(0)` → **`+0x51 NtTerminateThread(0, status)`** → 돌아오면 (예: `0xc00000db`) `+0x56 JMP +0x39` → `RtlExitUserProcess`. 곧 커널 `+0x34` 검사는 "물어본 뒤 그 사이 마지막이 된" 경우를 받는 둘째 겹으로 보인다 `[추측: 뜻]`. 클래스 `0xc` 이름 `ThreadAmILastThread` 는 `[추측: 공개 헤더]`.
- 질문마다 "지금 우리" (결과서 sast/75 그대로, 근거 줄은 오케스트레이터·flow-writer 가 `.asm` 으로 다시 봄):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 사슬 | 핸들 0 (1 아님) · `-2` · 자기 핸들 → `PspTerminateThreadByPointer(자기, status, 직접 1)` → `+0x40` `CrossThreadFlags.Terminated` 켬 → **`+0x4a PspExitThread(status)`** → `+0x4f INT3` (안 돌아옴). 남 핸들 → `PspTerminateThreadByPointer(남, status, 0)` (질문 5) | "`Terminated` 켜고 지금 스레드면 이어서 `PspExitThread`" (197 메모) **맞음**. 남도 곧바로 끝낸다면 **다름** (5) | `NtTerminateThread +0x22/+0x25` · `+0x41/+0x46` · `+0x60/+0x64` · `+0x95/+0x9a` · `+0xa5/+0xa8` · `+0xaf/+0xb4` · `+0xd4`, `PspTerminateThreadByPointer +0x40` · `+0x4a/+0x4f` |
| 1' 핸들 0 (원문 `NtTerminateThread(0, 0)`) | **`ActiveThreads == 1` 이면 `0xc00000db`** 로 돌려보냄 (안 끝냄). `-2`·자기 핸들은 검사 없음. ntdll 은 그 전에 `NtQueryInformationThread(-2, 0xc)` 로 묻고, 마지막이거나 이 실패가 오면 `RtlExitUserProcess` | 원문엔 검사 여부가 안 보임. Worker 는 이 갈래를 안 탈 것 `[추측: 순서]` — 넣는다면 한 줄 | `NtTerminateThread +0x27` · `+0x2e` · **`+0x34/+0x37`** · `+0x59`, ntdll `RtlExitUserThread +0x29` · `+0x3b` · `+0x51` · `+0x56` |
| 2(a) `ActiveThreads` | 프로세스 락 (`ProcessLock +0x1c8`) 안에서 **`+0x184 ~ +0x18e` 1 내림** → `+0x1ae CMOVZ` 마지막 표지 → `+0x1bd JNZ +0x685` (마지막) / 아니면 `+0x1cc LastThreadExitStatus = status` → `+0x1d9` 락 풀기 | **빠짐** — 더해야 함 | `PspExitThread +0x156 ~ +0x176` · `+0x184` · `+0x18b` · **`+0x18e`** · `+0x1a5` · `+0x1ae` · `+0x1bd` · `+0x1cc` · `+0x1d9` |
| 2(b) TEB | **`+0x4b8 / ret +0x4bd MmDeleteTeb(프로세스, TEB)`**. 조건: `KTHREAD.Teb ≠ 0` (먼저 `+0x37d` 에서 0 으로 지움) · 시스템 스레드 아님 (`+0x38f`) · 프로세스 `Flags & (ProcessDelete \| ProcessSelfDelete)` == 0 (`+0x3a3/+0x3a8`) — 마지막 스레드는 `+0x685` 가 먼저 `ProcessDelete` 를 켜서 건너뜀 | "공사 중 (안 품)" — **더해야 함** | `PspExitThread +0x368` · `+0x37d` · `+0x38f/+0x396` · `+0x39c/+0x3a3/+0x3a8` · **`+0x4b8`**, `MmDeleteTeb +0x47` · `+0x56` · `+0x69` · `+0x75` |
| 2(c) 유저 스택 | **있다, 커널이 푼다**: 위 조건 + `CrossThreadFlags.ThreadInserted` (`+0x3b4`) 일 때 `+0x3b8` **TEB 에서 바로** `DeallocationStack` 을 읽어 → `RegionSize 0` · `MEM_RELEASE` (`+0x3dc`) · 핸들 `-1` (`+0x3ef`) → **`+0x3f2 / ret +0x3f7 ZwFreeVirtualMemory`** (예약 영역 통째로). `SameTebFlags` 는 안 봄 (`grep 0x17ee` 0건) | "ntdll `RtlExitUserThread` 는 스택 안 품" **맞음** (ntdll 직접 호출에도 없음). 커널 쪽 **더해야 함** | `PspExitThread +0x3ae/+0x3b4/+0x3b6` · `+0x3b8` · `+0x3dc` · `+0x3ef` · **`+0x3f2`**, 이어서 `+0x400` · `+0x40f` (`TEB+0x16a8` 핸들 닫기) |
| 2(d) 목록에서 빼기 | **`PspExitThread` 에는 없다.** KPROCESS 쪽 = `KeTerminateThread +0x116/+0x119`, EPROCESS 쪽 = `PspThreadDelete +0x374/+0x377` (객체 지울 때) | 197 메모가 `PspExitThread` 안에 "`ThreadListHead` 빼기 (+0x370)" 를 두었다면 **자리가 다름** | `KeTerminateThread +0x116` · `+0x119`, `PspThreadDelete +0x374` · `+0x377` |
| 2(e) 마지막 호출 | **`+0x54c / ret +0x551 KeTerminateThread(Thread)`** — 이 뒤는 맺음 코드뿐 | 순서만 맞추면 됨 | `PspExitThread +0x549` · `+0x54c` |
| 3 신호·깨움 | `+0x16c SignalState = 1` (이미 1 이어도 덮어씀) → 기다림 블록마다 `WaitType == 1` 이면 **`+0x19b KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)`** (전부 기다림은 `0x100`) → `+0x1a2/+0x1a6` 목록 머리 비움 → `+0x1ce KiProcessThreadWaitList(Prcb, 1, 0, 0)`. `KiExitDispatcher` 는 안 부름 | "끝난 스레드 객체는 신호이고 그대로 남음" (192 메모) **맞음**. **잠든 기다림을 그 자리에서 깨우는 일** (194 "잠든 쪽 깨우기") 이 여기여야 함 | `KeTerminateThread +0x16c` · `+0x173` · `+0x184/+0x187` · **`+0x19b`** · `+0x1a2/+0x1a6` · `+0x1b7` · `+0x1ce` |
| 3(c) 끝 상태 | `+0x46 WaitReason = 0x16` · **`+0x1b0 State = 4`** · `+0x1d3 ~ +0x1ee` 청소부 목록 (`PsReaperListHead`) 에 얹기 · `+0x213` 작업 항목 · **`+0x24f KiSwapThread`** — 어느 준비 줄에도 안 넣어 다시 안 뽑힘 | "다시 안 돈다" 면 맞음 | `KeTerminateThread +0x46` · `+0x1b0` · `+0x1d3` · `+0x1ee` · `+0x213` · `+0x24f` |
| 4 마지막 스레드 | `+0x685 Flags \|= ProcessDelete · ProcessRundown` → `PspExitLastThread` 를 **4번** (단계 0 `+0x6a1` · 1 `+0x58d` · 2 `+0x5a6` · 3 `+0x5bf`). 단계 0 = 프로세스 `ExitStatus` 정하기 + **다른 스레드 신호를 기다림** (`+0x248 KeWaitForSingleObject`), 단계 3 = `PspExitProcess` · `PspRundownSingleProcess` (→ `MmCleanProcessAddressSpace`) | "마지막이면 `process_exit` + 상태 + `bLastThreadExited`" **큰 줄기 맞음**. 단계 0 기다림은 CPU 하나·순서 고정이면 없어도 될 수 있음 `[추측: 에뮬레이터 범위]` | `PspExitThread +0x685` · `+0x6a1` · `+0x58d` · `+0x5a6` · `+0x5bf`, `PspExitLastThread +0x122` · `+0x127` · `+0x137` · `+0x157` · `+0x248` · `+0x74` · `+0xe1` |
| 5(a) 남 끝내기 | `Terminated` 를 `BTS` 로 켜고 **처음 켠 쪽만** `ExitStatus` 적고 → **`+0x77 KeRequestTerminationThread`** → 곧 SUCCESS. 실제 끝냄은 그 스레드가 커널 APC 를 받을 때 그 스레드 문맥에서 `KiSchedulerApcTerminate +0x2f PspExitThread(Thread.ExitStatus)` | 남을 곧바로 끝낸다면 **다름** (결과는 같아도 끝나는 때가 "그 스레드가 다음에 돌 때") | `PspTerminateThreadByPointer +0x63` · `+0x6e` · **`+0x77`**, `KeRequestTerminationThread +0x2e` · `+0x78` · `+0x89`, `KiSchedulerApc +0x1d4/+0x1db`, `KiDeliverApc +0x46b`, `KiSchedulerApcTerminate +0x29` · `+0x2f` |
| 5(b) 모두 끝내기 | `PspTerminateAllThreads`: 목록을 뒤에서 앞으로 돌며 부른 쪽만 빼고 (`+0x9e`) **`+0xd4 PspTerminateThreadByPointer(…, 0)`** (APC 길) | "남 먼저, 자기 마지막" (197 메모) **순서 맞음** | `PspTerminateAllThreads +0x9e` · **`+0xd4`** · `+0x221`, `NtTerminateProcess +0x243` · `+0x161` |
| 6 기다림이 스레드 객체를 받는 길 | `ObWaitForSingleObject +0x82 RCX = OBJECT_TYPE.DefaultObject` → 비트 0 꺼짐 · 0 이상이면 **`+0x90 본체 + 오프셋`**. 스레드 형식은 `DefaultObject == 0` 으로 읽힘 → 본체 = `_ETHREAD` = `_KTHREAD.Header` `[정적: 읽은 줄 셋으로 유추]` | "기다림 도우미가 스레드 객체를 받는다" (192 메모) **맞음** | **`ObWaitForSingleObject +0x82`** · `+0x86` · `+0x8e` · **`+0x90`** · `+0xba` |
| 7 객체 지울 때 | `PspThreadDelete` (형식의 삭제 루틴, `PspInitPhase0 +0x510/+0x517`) 는 **TEB·스택을 안 푼다** — `MmDeleteTeb`·`ZwFreeVirtualMemory` 호출 없음. 하는 일: 커널 쪽 정리 · TID 반납 · **EPROCESS 목록에서 빼기** · 프로세스 참조 내림 | TEB·스택은 `PspExitThread` 쪽 **맞음**. 객체 지우기는 건너뛰어도 됨 `[추측: 에뮬레이터 범위]` | `PspThreadDelete +0x17` · `+0x1cd` · `+0x1e8` · **`+0x374/+0x377`** · `+0x3d5` |

### 순서 한 장 — `PspExitThread` → `KeTerminateThread` (Worker 같은 "마지막이 아닌 유저 스레드", 결과서 그대로)

| 순 | 한 일 | 줄 |
|---|---|---|
| 1 | `ExitStatus` 적기 | `PspExitThread +0x100` |
| 2 | 스레드 끝 통지 | `+0x142 PspCallThreadNotifyRoutines` |
| 3 | 프로세스 락 → **`ActiveThreads −1`** → 0 아니면 `LastThreadExitStatus` 적고 락 풀기 | `+0x18e` · `+0x1cc` · `+0x1d9` |
| 4 | 디버거·LPC 끝 메시지·Win32k 콜아웃·FLS | `+0x7b9/+0x27dbac` · `+0x6d0/+0x273` · `+0x2f1` · `+0x336` |
| 5 | I/O 취소·타이머·뮤턴트 놓기 | `+0x340 ~ +0x363` |
| 6 | **유저 스택 풀기** (`ZwFreeVirtualMemory`) → 디버그 핸들 닫기 → **`MmDeleteTeb`** | `+0x3f2` · `+0x40f` · `+0x4b8` |
| 7 | `ExitTime` | `+0x4bd` |
| 8 | **APC 큐 비우기** | `+0x4e3 KeRundownApcQueues` |
| 9 | **`KeTerminateThread`** → KPROCESS 목록에서 빼기 → **`SignalState=1`** → 기다리는 모두 깨움 → `State=4` → 청소부 목록에 얹기 → **`KiSwapThread`** | `+0x54c` → `KeTerminateThread +0x116` · `+0x16c` · `+0x19b` · `+0x1b0` · `+0x1d3` · `+0x24f` |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/75 끝을 그대로 옮김)

1. **`NtTerminateThread(핸들, status)`**: 핸들 `0` + (프로세스 `ActiveThreads == 1`) 이면 `0xc00000db` 로 돌려보냄. (`-2`·자기 핸들은 이 검사 없음.) 자기면 `Terminated` 켜고 `PspExitThread(status)` — 돌아오지 않음.
2. **남이면 `PspExitThread` 를 안 부르고** `Terminated` 만 켜고 (이미 켜져 있으면 `ExitStatus` 안 씀), `ExitStatus` 적고, "종료 요청" 만 걸고 `SUCCESS` 로 돌아옴. 실제 끝냄은 **그 스레드가 다음에 돌 때, 그 스레드의 문맥에서** `PspExitThread(Thread.ExitStatus)`.
3. **`PspExitThread` 순서**: ① `ExitStatus` 적기 → ② 스레드 끝 통지 → ③ **`ActiveThreads -= 1`**; 0 이면 "마지막" (`Flags |= ProcessDelete`, 단계 0: 프로세스 `ExitStatus` 정하기·다른 스레드 신호 기다리기), 아니면 `LastThreadExitStatus = status` → ④ (유저 스레드이고 마지막이 아니면) **TEB 블록** → ⑤ `ExitTime` → ⑥ APC 큐 비우기 → ⑦ (마지막이면 단계 3: `PspExitProcess` + `PspRundownSingleProcess` — 이게 주소 공간째 치움) → ⑧ **`KeTerminateThread`**.
4. **TEB·유저 스택은 커널이, 끝나는 스레드 자신이, `KeTerminateThread` 보다 앞에서** 푼다. 순서: 유저 스택 (`TEB.DeallocationStack` 를 그때 TEB 에서 읽어 예약 영역 통째로 `MEM_RELEASE`) → `DbgSsReserved` 둘째 칸 핸들 닫기 → `MmDeleteTeb` (TEB 쪽 하위 할당 영역에 돌려놓음). 조건: `Teb≠0`, 시스템 스레드 아님, 프로세스가 `ProcessDelete/ProcessSelfDelete` 아님, 스택은 `ThreadInserted` 일 때만. **`SameTebFlags` 는 안 본다.** ntdll `RtlExitUserThread` 는 스택을 안 품 (맞음).
5. **마지막 스레드는 TEB·스택을 따로 안 푼다.** 프로세스 주소 공간째 (`MmCleanProcessAddressSpace`) 정리하는 길로 간다 (프로세스 쪽 규칙; 지금 에뮬레이터의 process_exit 모양과 맞음).
6. **기다리던 쪽 깨우기 = `KeTerminateThread` 안에서 한 번에**: 스레드 객체 `SignalState = 1` (이미 1 이어도), 기다림 목록 **전부** (`WaitType==1` 은 `WaitStatus = WaitKey` 로, 전부기다림은 `0x100`), 신호는 **그대로 남음** (수동 이벤트 꼴), 목록은 비움. 깨운 스레드는 준비 줄로 (sast/73 질문 2(c)). 깨난 쪽은 `WaitStatus` 를 그대로 돌려줌 (다시 안 묻는다, sast/73 질문 1(c)) — test.exe 메인은 `0`.
7. **끝난 스레드는 다시 돌지 않음**: `State = Terminated`, 어떤 준비 줄에도 안 넣고 `KiSwapThread` 로 다른 스레드를 골라 넘어감 (sast/71). 커널 스택 해제·객체 참조 내림은 나중 일 (청소부 `PspReaper`), `PspThreadDelete` (프로세스 목록에서 빼기·TID 반납) 은 참조가 0 일 때 — 에뮬레이터는 건너뛰어도 됨.
8. **스레드 목록 둘**: `KeTerminateThread` 가 `KPROCESS` 쪽을 빼고 (`+0x116`), `EPROCESS` 쪽 (`+0x370`) 은 객체 삭제까지 남는다. "끝난 스레드를 목록에서 바로 지운다" 로 만들었다면 `EPROCESS` 쪽은 **틀리다.**

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - ① **유저 쪽 순서** `[정적]` (있던 `ntdll/RtlExitUserThread.asm`, 위 "원문의 `NtTerminateThread(0, 0)`"): 마지막인지 먼저 묻고 (`+0x29`, 클래스 `0xc`) → 마지막이면 `RtlExitUserProcess` → 아니면 `LdrShutdownThread` (DllMain·TLS 이유 3, run28) → `TpCheckTerminateWorker(0)` → `NtTerminateThread(0, status)` → 실패로 돌아오면 `RtlExitUserProcess`. 원문 줄 순서 (seq 357 `NtQueryInformationThread` → seq 384 `NtTerminateThread`) 와 맞다. 규칙 1 의 `0xc00000db` 는 이 마지막 화살표 하나로 이어진다.
  - ② **test.exe 는 핸들 `t` 를 닫는다** `[소스]` `sample/test.c:36` `CloseHandle(t)` (결과서 질문 7 의 "닫지 않으므로" 와 다름 — 아래 "결과서와 다르게 적은 곳" ①). 원문 seq 386 `NtClose(0x70)` 도 같다. 그래서 Worker `ETHREAD` 의 참조는 메인이 깨난 뒤 곧 줄고, `PspThreadDelete` 는 프로세스 끝이 아니라 `NtClose(t)` 또는 청소부 (`PspReaper +0x69`) 중 **늦은 쪽** 에서 불릴 것 `[추측: 참조 수]`. 규칙 7 대로 건너뛰어도 결과는 같다.
  - ③ **메인이 깨날 때 Worker 의 스택·TEB 주소는 이미 없다** `[정적: 순서]` — 6 (스택·TEB) 이 9 (신호) 보다 앞. 에뮬레이터가 "신호 → 정리" 순서로 두면 깨난 메인이 Worker 메모리를 아직 볼 수 있게 되어 진짜와 달라진다 (결과서 질문 3 "지금 우리" (2)).

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, `KiCommitThreadWait +0x220/+0x225 → KiSwapThread` (run77), run75 의 `KeTerminateThread +0x24f → KiSwapThread` 일치
- ntdll 은 새로 안 뽑음. `RtlExitUserThread.asm` 은 전에 뽑혀 있던 것 (ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1`, flow.md 기준과 같음)
- **이름 주의**:
  - PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (run77 과 같음). `ObWaitForSingleObject +0xba` · `PspExitLastThread +0x248` 의 callee.
  - `KeTerminateThread +0x213` 의 callee 는 `_calls.tsv` 에 `alias:Ordinal_36` 로 찍힌다. PDB 이름은 **`ExQueueWorkItemEx` 하나** (`pdb_aliases.py --where ntoskrnl.exe ExQueueWorkItemEx` → rva `0x2cd360`, `_index.tsv` 이름 `Ordinal_36`. `--lookup` 은 "이름 하나뿐").
  - `KeTerminateThread +0xda` 의 callee 는 **큐 스핀락을 놓는 함수** — 같은 주소 이름 2개, 후보: `KxReleaseQueuedSpinLock` · `KeReleaseInStackQueuedSpinLockFromDpcLevel` (`pdb_aliases.py --where`, `_index.tsv` 이름은 뒤쪽).
- 칸 이름 (`pdb_struct.py`, 결과서 "읽는 법"):
  - `_ETHREAD`: `Tcb +0x0` · `ExitTime +0x4c8` · `TerminationPort`·`ReaperLink +0x4e8` (같은 칸) · `ThreadListEntry +0x578` · `RundownProtect +0x588` · `CrossThreadFlags +0x5a0` (`Terminated` 비트 0 · `ThreadInserted` 비트 1 · `BreakOnTermination` 비트 5) · `ExitStatus +0x5d8`
  - `_KTHREAD`: `Header +0x0` (`SignalState +0x4`, 기다림 목록 `+0x8`) · `ThreadLock +0x40` · `MiscFlags +0x74` (`SystemThread` 비트 10 · `ApcQueueable` 비트 14 · `CetUserShadowStack` 비트 20) · `ThreadFlags +0x78` (`TerminationApcRequest` 비트 15) · `Teb +0xf0` · `State +0x184` · `KernelApcDisable +0x1e4` · `Process +0x220` · `WaitReason +0x283` · `SchedulerApc +0x288` · `ThreadListEntry +0x2f8`
  - `_KPROCESS`: `ThreadListHead +0x30` · `ProcessLock +0x40` · `ProcessListEntry +0x110`
  - `_EPROCESS`: `ProcessLock +0x1c8` · `Flags +0x1f4` (`ProcessDelete` 비트 3 `0x8` · `BreakOnTermination` 비트 13 · `ProcessRundown` 비트 25 `0x2000000` · `ProcessSelfDelete` 비트 30 `0x40000000`) · `ThreadListHead +0x370` · **`ActiveThreads +0x380`** · `LastThreadExitStatus +0x38c` · `ExitStatus +0x554` · `ExitTime +0x5c0` · `ThreadListLock +0x5e0`
  - `_TEB`: `DeallocationStack +0x1478` · `DbgSsReserved +0x16a0` (`+0x16a8` = 둘째 칸) · `SameTebFlags +0x17ee`
  - `_OBJECT_TYPE`: `DefaultObject +0x20` · `TypeInfo +0x40` (`DeleteProcedure` = 형식 `+0x88`)
- 숫자만 `[정적]`, 이름은 `[추측]`: `State 4` = Terminated · `WaitReason 0x16` = `WrTerminated` (run77 이 PDB 열거형 `_KTHREAD_STATE`·`_KWAIT_REASON` 을 확인했지만 칸 타입이 바이트라 연결은 `[추측: 이름]`).

## 목적

- PE_emulator 요청 25 ㄹ (Q141d): nt 엔진의 스레드 끝내기를 진짜 커널에 맞추기 위해 — `NtTerminateThread` 에서 `PspExitThread` 까지 사슬 (자기·남·핸들 0), `PspExitThread` 본문 순서 (`ActiveThreads`·TEB·유저 스택·목록·`KeTerminateThread`), `KeTerminateThread` 의 신호·깨움·`State`, 마지막 스레드 (`PspExitLastThread`), 남·모두 끝내기, `ObWaitForSingleObject` 가 스레드 객체를 받는 길, 객체 삭제 (`PspThreadDelete`).
- 깨움 꼴·`WaitStatus`·준비 줄은 run77 (Q141c1), `KiSwapThread` 는 run75 를 가리키기만 한다.

## 방법

- 정적: `_workspace/sast/75_thread-exit.md` — ntoskrnl
  - 지도: `run_ghidra.sh map ntoskrnl.exe NtTerminateThread` (2,451함수) · `… PspExitThread` (3,434함수) → `ms/disassemble/ntoskrnl/_map/`. `NtTerminateThread` 깊이 1 은 3개, `PspExitThread` 깊이 1 은 49개 (거의 ETW·락·LPC·콜백·bugcheck) 라 depth 0 + 이름으로 골랐다.
  - 지도·`_calls.tsv` 에 안 보이는 이음새 둘은 `refs` 로: `PspThreadDelete` (객체 형식 삭제 루틴으로 등록), `PspReaper` (작업 항목 루틴으로 등록). `KiSchedulerApcTerminate +0x2f → PspExitThread` 도 `refs` 에서만.
  - 새로 뽑음 **16개** (모두 depth 0):
    - 요청 대상 7: `NtTerminateThread` (216B) · `PspTerminateThreadByPointer` (216B) · `PspExitThread` (2329B, 줄기만) · `KeTerminateThread` (821B) · `PspExitLastThread` (739B) · `PspTerminateAllThreads` (654B) · `PspThreadDelete` (1314B)
    - 넓힌 9: `KeRequestTerminationThread` (224B) · `KiSchedulerApcTerminate` · `PspReaper` (171B) · `PspExitProcess` · `PspRundownSingleProcess` · `NtTerminateProcess` · `PspTerminateProcess` (넷은 부르는 줄·callee 이름만) · `ObCreateObjectType` · `ObCreateObjectTypeEx` (`DefaultObject` 몇 줄만)
  - 있던 것 다시 읽음: `MmDeleteTeb` · `ObWaitForSingleObject` · `KiSchedulerApc` · `KiDeliverApc` · `KiInsertQueueApc` · `KeInitThread` (몇 줄씩)
  - 안 읽은 것 (이름만): ETW · 감사 · 작업 객체·silo · LPC · Win32k 콜아웃 · 디버거 (`DbgkExitProcess`·`DbgkExitThread`) · FLS · 전력 · 보안 스레드 · `KiSwapThread` 이하 (run75)
- 결과서: `_workspace/sast/75_thread-exit_result.md` (**분석성공**. 일곱 질문 모두 `.asm` 줄로 답함. 못 정한 셋: 질문 5(a) 의 `KiSchedulerApc` ↔ `KiDeliverApc` 사이 한 걸음 `[추측]`, 질문 6 스레드 형식 `DefaultObject == 0` 은 읽은 줄 셋으로 유추, 단계 3 본문은 이름만)
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `NtTerminateThread +0x34/+0x37`, `PspExitThread +0x18e` · `+0x3f2` (`ZwFreeVirtualMemory`) · `+0x4b8` (`MmDeleteTeb`) · `+0x54c` · `+0x685` · `+0x6a1`, `KeTerminateThread +0x116` · `+0x16c` · `+0x19b` · `+0x1b0` · `+0x1ce` · `+0x24f`, `PspTerminateThreadByPointer +0x77` (`KeRequestTerminationThread`), `PspTerminateAllThreads +0xd4`, `PspThreadDelete +0x374/+0x377`, `ObWaitForSingleObject +0x82/+0x90`
- flow-writer 도 `.asm` 으로 다시 봤다: `NtTerminateThread +0x0 ~ +0xd4` (전부), `PspExitThread +0x100` · `+0x122` · `+0x142` · `+0x184` · `+0x18b` · `+0x1a5` · `+0x1ae` · `+0x1bd` · `+0x1cc` · `+0x1d9` · `+0x225` · `+0x368` · `+0x37d` · `+0x38f/+0x396` · `+0x39c ~ +0x3b8` · `+0x3dc` · `+0x3ef` · `+0x400` · `+0x40f` · `+0x4bd ~ +0x4d7` · `+0x4e3` · `+0x549` · `+0x5c9` · `+0x5d3` · `+0x713/+0x718` · `+0x775 ~ +0x790`, `KeTerminateThread +0x46` · `+0x6c` · `+0x7f/+0x82` · `+0xda` · `+0xe2` · `+0xee/+0xf7` · `+0x119` · `+0x11d` · `+0x124` · `+0x12e` · `+0x137` · `+0x13c` · `+0x152` · `+0x157 ~ +0x168` · `+0x173` · `+0x184/+0x187` · `+0x1a2 ~ +0x1b7` · `+0x1d3` · `+0x1e8/+0x1ee` · `+0x1fc/+0x1ff` · `+0x213` · `+0x21c` · `+0x22a` · `+0x23e`, `PspTerminateThreadByPointer +0x40` · `+0x4a` · `+0x63` · `+0x6e`, `PspTerminateAllThreads +0x9e` · `+0x221`, `KeRequestTerminationThread +0x2e` · `+0x4c` · `+0x78` · `+0x89` · `+0xa7` · `+0xb6` · `+0xbe`, `KiSchedulerApc +0x41` · `+0x1d4/+0x1db` · `+0x1f0`, `KiDeliverApc +0x443` · `+0x459` · `+0x46b`, `KiSchedulerApcTerminate +0x17` · `+0x29` · `+0x2f`, `NtTerminateProcess +0x2f` · `+0xb9` · `+0x130 ~ +0x166` · `+0x1ec` · `+0x243`, `PspExitLastThread +0x61/+0x6b` · `+0x74` · `+0xe1` · `+0xe9` · `+0x122` · `+0x127` · `+0x137` · `+0x157` · `+0x248`, `MmDeleteTeb +0x3d` · `+0x47` · `+0x56` · `+0x69` · `+0x75` · `+0xa7`, `PspRundownSingleProcess +0xb5` · `+0xbd` · `+0x1d5`, `PspThreadDelete +0x17` · `+0x13c` · `+0x1cd` · `+0x1e8` · `+0x374/+0x377` · `+0x396` · `+0x3d5` · `+0x45f`, `PspReaper +0x16` · `+0x27` · `+0x69`, `ObWaitForSingleObject +0x86` · `+0x8e` · `+0xba`, `PspInsertThread +0x490` · `+0x65a` · `+0x69c ~ +0x6f8` · `+0x239 ~ +0x266` (스레드 목록 넣기, 아래 "보탠 것" ③), ntdll `RtlExitUserThread +0x0 ~ +0x56` (전부), ntdll `_calls.tsv` 의 `LdrShutdownThread` 직접 호출 25줄. `_calls.tsv` 에서 호출 줄 모두 맞춤. `SameTebFlags` 안 읽음은 결과서의 `grep` 그대로.
- 값 대조: 없다. 끝내기는 `main` 뒤라 run44 사진 구간 (초기화 → 은퇴) 밖이다. 새 동적 실행도 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`, ntdll 은 `ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!RtlExitUserThread` | `+0x29` | `+0x2e` | `ntdll!NtQueryInformationThread` | `(-2, 0xc, &buf, 4, NULL)` (`+0x16 R9D = 4` · `+0x25 EDX = R9 + 8`). 성공이고 buf ≠ 0 이면 `+0x39` 로 | [정적] |
| `ntdll!RtlExitUserThread` | `+0x3b` | `+0x40` | `ntdll!RtlExitUserProcess` | 마지막 스레드, 또는 `+0x56 JMP +0x39` (`NtTerminateThread` 가 돌아왔을 때). `+0x40 INT3` | [정적] |
| `ntdll!RtlExitUserThread` | `+0x41` · `+0x48` | `+0x46` · `+0x4d` | `ntdll!LdrShutdownThread` · `ntdll!TpCheckTerminateWorker(0)` | 마지막 아닐 때 | [정적] |
| `ntdll!RtlExitUserThread` | **`+0x51`** | **`+0x56`** | **`ntdll!NtTerminateThread(0, status)`** | `+0x4d EDX = status` · `+0x4f ECX = 0` | [정적] |
| `nt!NtTerminateThread` | **`+0x41`** | **`+0x46`** | **`nt!PspTerminateThreadByPointer`** | 자기 (핸들 0 이고 `ActiveThreads ≠ 1`, 또는 `-2`, 또는 자기 핸들). `R8B = 1` (직접) | [정적] |
| `nt!NtTerminateThread` | `+0x95` | `+0x9a` | `nt!ObpReferenceObjectByHandleWithTag` | 그 밖 핸들. `EDX = 1` · 형식 `PsThreadType` · 모드 `PreviousMode` · 태그 `0x65547350` | [정적] |
| `nt!NtTerminateThread` | `+0xaf` | `+0xb4` | `nt!PspTerminateThreadByPointer` | 남. `R8D = 0` | [정적] |
| `nt!NtTerminateThread` | `+0xc0` · `+0xcf` | `+0xc5` · `+0xd4` | `nt!ObfDereferenceObjectWithTag` | 참조 내림 (`+0xd4 JMP +0x39` = 자기 길) | [정적] |
| `nt!PspTerminateThreadByPointer` | **`+0x4a`** | **`+0x4f`** | **`nt!PspExitThread`** | 자기 + 직접. 앞 `+0x40 OR.LOCK [RBX+0x5a0],1`. 뒤 `+0x4f INT3` | [정적] |
| `nt!PspTerminateThreadByPointer` | **`+0x77`** | `+0x7c` | **`nt!KeRequestTerminationThread`** | 남 (또는 직접 0). 앞 `+0x63 BTS.LOCK` · 처음이면 `+0x6e ExitStatus`. 뒤 `+0x7c XOR EAX,EAX` | [정적] |
| `nt!PspTerminateThreadByPointer` | `+0xc8` | `+0xcd` | `nt!PspCatchCriticalBreak` | `BreakOnTermination` (`+0x29/+0x2b`). 이름만 | [정적] |
| `nt!PspExitThread` | `+0x66` | `+0x6b` | `nt!PspClearProcessThreadCidRefs` | 맨 앞 | [정적] |
| `nt!PspExitThread` | `+0xdf` | `+0xe4` | `nt!ExWaitForRundownProtectionRelease` | `Thread.RundownProtect (+0x588)` | [정적] |
| `nt!PspExitThread` | **`+0x142`** | **`+0x147`** | **`nt!PspCallThreadNotifyRoutines(Thread, 0, 0)`** | 스레드 끝 통지. `Flags3.Minimal` 이고 `PicoContext == 0` 이면 건너뜀 | [정적] |
| `nt!PspExitThread` | `+0x171` · `+0x1d9` | `+0x176` · `+0x1de` | `nt!ExfAcquirePushLockExclusiveEx` · `nt!PspUnlockProcessExclusive` | 프로세스 락 (`+0x1c8`) — `ActiveThreads` 내림을 감쌈 | [정적] |
| `nt!PspExitThread` | **`+0x6a1`** · `+0x58d` · `+0x5a6` · `+0x5bf` | `+0x6a6` · `+0x592` · `+0x5ab` · `+0x5c4` | **`nt!PspExitLastThread`** | 마지막 (`R12B ≠ 0`). 단계 `ECX` 0 · 1 · 2 · 3 | [정적] |
| `nt!PspExitThread` | `+0x38a` | `+0x38f` | `nt!PspLockUnlockTebAccessExclusive` | TEB 블록 들어갈 때. 이름만 | [정적] |
| `nt!PspExitThread` | **`+0x3f2`** · `+0x457` | **`+0x3f7`** · `+0x45c` | **`nt!ZwFreeVirtualMemory`** | 유저 스택 (앞) · WoW64 32비트 스택 (뒤, test.exe 해당 없음) | [정적] |
| `nt!PspExitThread` | `+0x40f` | `+0x414` | `nt!ObCloseHandle` | `TEB+0x16a8 ≠ 0` 일 때 (`KernelMode`) | [정적] |
| `nt!PspExitThread` | `+0x654` | `+0x659` | `nt!PspFreeCurrentThreadUserShadowStack` | `ThreadInserted` + `CetUserShadowStack`. 이름만 | [정적] |
| `nt!PspExitThread` | **`+0x4b8`** | **`+0x4bd`** | **`nt!MmDeleteTeb(프로세스, TEB)`** | TEB 블록 끝 | [정적] |
| `nt!PspExitThread` | `+0x4d2` | `+0x4d7` | `nt!KeQuerySystemTimePrecise` | `+0x4cb RCX = &Thread.ExitTime`. 앞 `+0x4bd KeQuerySystemTimeUnsafe` 가 0 이면 (`+0x4c2/+0x4c5`) 대신 `+0x5c9 ~ +0x5d3` KUSD `SystemTime` 을 직접 | [정적] |
| `nt!PspExitThread` | **`+0x4e3`** | **`+0x4e8`** | **`nt!KeRundownApcQueues`** | APC 큐 비우기 | [정적] |
| `nt!PspExitThread` | **`+0x54c`** | **`+0x551`** | **`nt!KeTerminateThread(Thread)`** | 맨 끝. 안 돌아옴 | [정적] |
| `nt!KeTerminateThread` | `+0x6c` · `+0x137` | `+0x71` · `+0x13c` | `nt!ExAcquireSpinLockExclusiveAtDpcLevel` · `nt!ExReleaseSpinLockExclusiveFromDpcLevel` | `KPROCESS.ProcessLock (+0x40)` | [정적] |
| `nt!KeTerminateThread` | `+0xda` | `+0xdf` | 큐 스핀락 놓기 (같은 주소 이름 2개, 후보: `KxReleaseQueuedSpinLock` · `KeReleaseInStackQueuedSpinLockFromDpcLevel`) | 마지막 스레드일 때 프로세스 전역 목록에서 뺀 뒤 (`KiProcessListLock`) | [정적] |
| `nt!KeTerminateThread` | `+0xe2` · `+0x163` | `+0xe7` · `+0x168` | `nt!KiAcquireKobjectLockSafe` | 프로세스 헤더 락 · 스레드 헤더 락 | [정적] |
| `nt!KeTerminateThread` | **`+0x19b`** | **`+0x1a0`** | **`nt!KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)`** | 기다림 블록마다, `WaitType == 1`. 그 밖 (`0`) 은 `+0x2fe` `0x100` 으로 같은 함수 | [정적] |
| `nt!KeTerminateThread` | **`+0x1ce`** | **`+0x1d3`** | **`nt!KiProcessThreadWaitList(Prcb, 1, 0, 0)`** | `PRCB+0x2d08 ≠ 0` (`+0x1b7`) | [정적] |
| `nt!KeTerminateThread` | `+0x213` | `+0x218` | `nt!ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)` (Ghidra `Ordinal_36`) | 청소부 목록이 비어 있었을 때 (`+0x1fc/+0x1ff`). 실패면 `+0x21c BTS [PsReaperWorkItem+0x18]` | [정적] |
| `nt!KeTerminateThread` | **`+0x24f`** | **`+0x254`** | **`nt!KiSwapThread(Thread, Prcb, 0)`** | 마지막 넘기기. 안 돌아옴 (run75) | [정적] |
| `nt!KeTerminateThread` | `+0x12e` · `+0x152` | `+0x133` · `+0x157` | `nt!KiRemoveThreadFromSchedulingGroup` · `nt!KiActivateWaiterQueueWithNoLocks` | `SchedulingGroup (+0x68)` · `Queue (+0xe8)` 있을 때. 이름만 | [정적] |
| `nt!KeRequestTerminationThread` | `+0x78` · `+0x89` | `+0x7d` · `+0x8e` | `nt!KiInsertQueueApc` · `nt!KiSignalThreadForApc` | `+0x2e BTS TerminationApcRequest` (이미면 끝) · `ApcQueueable` 일 때. APC = `Thread.SchedulerApc (+0x288)` | [정적] |
| `nt!KeRequestTerminationThread` | `+0xa7` · `+0xb6` · `+0xbe` | `+0xac` · `+0xbb` · `+0xc3` | `nt!KiExitDispatcher` · `nt!KeAlertThread` · `nt!KeForceResumeThread` | 멈춰 있던 스레드도 풀어 APC 가 돌게 | [정적] |
| `nt!KiSchedulerApc` | `+0x1f0` | `+0x1f5` | `nt!KiInsertQueueApc` | `+0x41 ThreadFlags` 의 `0x8000` 을 보고 `+0x1d4/+0x1db` `KernelRoutine = KiSchedulerApcTerminate` 로 바꿔 다시 넣음 | [정적] |
| `nt!KiDeliverApc` | `+0x46b` | `+0x470` | `nt!KiSchedulerApcTerminate` | 루틴이 그 주소면 (`+0x443/+0x459`) 곧장 | [정적] |
| `nt!KiSchedulerApcTerminate` | **`+0x2f`** | `+0x34` | **`nt!PspExitThread(Thread.ExitStatus)`** | `+0x29 ECX = [RCX+0x5d8]`. 프로세스 끝내기 요청이면 먼저 `+0x49 PsTerminateProcess` | [정적] |
| `nt!PspTerminateAllThreads` | **`+0xd4`** · `+0x221` | `+0xd9` · `+0x226` | `nt!PspTerminateThreadByPointer(…, 0)` | 목록 뒤에서 앞으로, 부른 쪽 빼고 (`+0x9e`) · 플래그 `& 2` 면 부른 쪽도 | [정적] |
| `nt!NtTerminateProcess` | `+0x243` · `+0x161` · `+0x1ec` | `+0x248` · `+0x166` · `+0x1f1` | `nt!PspTerminateAllThreads` · `nt!PspExitThread` · `nt!PspTerminateThreadByPointer` | 자기 프로세스면 모두 끝내기 뒤 자기 (`+0x147` `Terminated` 먼저) | [정적] |
| `nt!PspExitLastThread` | `+0x248` | `+0x24d` | `nt!KeWaitForSingleObject` (Ghidra `KeWaitForMutexObject`) | 단계 0 — `EPROCESS.ThreadListHead` 를 돌며 아직 신호 아닌 스레드마다 | [정적] |
| `nt!PspExitLastThread` | `+0x74` · `+0xe1` · `+0xe9` | `+0x79` · `+0xe6` · `+0xee` | `nt!PspExitProcess` · `nt!PspRundownSingleProcess` · `nt!LpcExitProcess` | 단계 3 | [정적] |
| `nt!PspRundownSingleProcess` | `+0xb5` · `+0xbd` · `+0x1d5` | `+0xba` · `+0xc2` · `+0x1da` | `nt!ObKillProcess` · `nt!MmCleanProcessAddressSpace` · `nt!KeSetProcess` | 핸들 표 · 주소 공간 · 프로세스 객체 신호. 이름만 | [정적] |
| `nt!MmDeleteTeb` | `+0x47` · `+0x56` · `+0x69` · `+0x75` | `+0x4c` · `+0x5b` · `+0x6e` · `+0x7a` | `nt!KeStackAttachProcess` · `nt!MiObtainReferencedVadEx` · `nt!MiFreeToSubAllocatedRegion` · `nt!KiUnstackDetachProcess` | 크기 `+0x3d` `0x2000` (WoW64 `+0xa7` `0x4000`) | [정적] |
| `nt!PspReaper` | `+0x27` · `+0x55` · `+0x69` | `+0x2c` · `+0x5a` · `+0x6e` | `nt!KeDeleteThread` · `nt!KeEnumerateKernelStackSegments` · `nt!ObfDereferenceObjectWithTag('Dflt')` | `+0x16 XCHG [PsReaperListHead]` 로 목록째 받아 스레드마다 | [정적] |
| `nt!PspThreadDelete` | `+0x17` · `+0x1cd` · `+0x1e8` · `+0x3d5` | `+0x1c` · `+0x1d2` · `+0x1ed` · `+0x3da` | `nt!KeCleanupThreadState` · `nt!ExMapHandleToPointer` · `nt!ExDestroyHandle` · `nt!ObfDereferenceObjectWithTag` | TID 반납 · 프로세스 참조 내림. `MmDeleteTeb`·`ZwFreeVirtualMemory` 없음 | [정적] |
| `nt!ObWaitForSingleObject` | `+0xba` | `+0xbf` | `nt!KeWaitForSingleObject` (Ghidra `KeWaitForMutexObject`) | run77. 앞 `+0x82 ~ +0x90` 디스패처 헤더 고르기 | [정적] |

- 간접으로만 이어지는 것 (`refs`): `PspInitPhase0 +0x510 LEA [PspThreadDelete]` / `+0x517` (스레드 형식 삭제 루틴), `PspInitPhase0 +0x9ae LEA [PspReaper]` / `+0x9b5 MOV [PsReaperWorkItem+0x10]` (작업 항목 루틴).
- 줄기 밖 (이름만): `EtwTraceThread` · `DbgkExitProcess` · `DbgkExitThread` · `SeAuditProcessExit` · `PsAttachSiloToCurrentThread` · `LpcRequestPort` · `PspUpdateCalloutParameters` · `PsSessionGetWin32Callouts` · `ExCallCallBack` · `RtlpFlsDataCleanup` · `PoDeletePowerRequest` · `KeUnsecureThread` · `IoCancelThreadIo` · `ExTimerRundown` · `CmNotifyRunDown` · `KiRundownMutants` · `PspSchedulerSharedDataRegionSlotFree` · `PspThreadIndexCleanup` · `PspLegoNotifyRoutine` · `PsCaptureExceptionPort` · `KeForceResumeProcess` · `ExpWnfDeleteProcessContext` · `PsTerminateMinimalProcess`.

### 사슬 — `NtTerminateThread` → `PspTerminateThreadByPointer` → `PspExitThread` (질문 1)

- 인자 `NtTerminateThread(RCX = 핸들, EDX = status)` (`+0x20 MOV EBP,EDX`), `+0x17 RSI = 지금 스레드`.

| 경우 | 줄 | 하는 일 |
|---|---|---|
| 핸들 `0` | `+0x22 TEST RCX / +0x25 JNZ +0x60` 을 안 탐 → `+0x27 RAX = [RSI+0xb8]` (붙은 프로세스) · `+0x2e ECX = [RAX+0x380]` (`ActiveThreads`) · **`+0x34 CMP ECX,1 / +0x37 JZ +0x59`** → `+0x59 EAX = 0xc00000db` | 하나 남은 스레드면 **끝내지 않고 실패로** 돌려보냄 (이름 `STATUS_CANT_TERMINATE_SELF` 는 `[추측: 공개 헤더]`) |
| 핸들 `0`, 하나 아님 | `+0x39 R8B = 1` · `+0x3c EDX = status` · `+0x3e RCX = 지금` → **`+0x41 / ret +0x46 PspTerminateThreadByPointer`** | 자기 끝내기 |
| 핸들 `-2` | `+0x60 CMP RCX,-2 / +0x64 JZ +0x39` | **검사 없이** 같은 자기 길 |
| 그 밖 | **`+0x95 / ret +0x9a ObpReferenceObjectByHandleWithTag`** → 실패면 `+0x9e JS` 로 그 코드 | 핸들 → 스레드 객체 |
| …자기를 가리킴 | `+0xa5 CMP RCX,RSI / +0xa8 JZ +0xca` → `+0xcf / ret +0xd4` 참조 내림 → `+0xd4 JMP +0x39` | 자기 길 (검사 없음) |
| …남 | `+0xaa XOR R8D,R8D` → **`+0xaf / ret +0xb4 PspTerminateThreadByPointer(남, status, 0)`** → `+0xc0` 참조 내림 | 남 끝내기 (아래 질문 5) |

- `PspTerminateThreadByPointer(Thread = RBX, ExitStatus = ESI, 직접 = BPL)`:
  - `+0x29 TEST AL,0x20 / +0x2b JNZ +0x9b` — `CrossThreadFlags.BreakOnTermination` 이면 (프로세스가 `ProcessDelete|ProcessSelfDelete` 아닐 때) `+0xc8 PspCatchCriticalBreak` (이름만).
  - **자기 + 직접**: `+0x2d TEST BPL / +0x30 JZ +0x50` 과 `+0x3b CMP RBX,지금 / +0x3e JNZ +0x50` 을 안 타면 → **`+0x40 OR.LOCK [RBX+0x5a0],1`** (`Terminated`) → `+0x48 ECX = status` → **`+0x4a / ret +0x4f PspExitThread`** → `+0x4f INT3`.
  - 그 밖은 `+0x50` (질문 5).
- `PspTerminateThreadByPointer` 를 부르는 곳: `NtTerminateThread +0x41/+0xaf` · `NtTerminateProcess +0x1ec` · `PspTerminateAllThreads +0xd4/+0x221` · `PspUserThreadStartup +0xf0` (2.3 — 끝까지 못 들어간 스레드, `0xC000004B`). `PspExitThread` 를 부르는 곳: 이 함수 `+0x4a` · `NtTerminateProcess +0x161` · `KiSchedulerApcTerminate +0x2f` 셋.

### `PspExitThread` 본문 (질문 2, 줄기만 — 위에서 아래 실행 순서, 결과서 표를 줄임)

인자 `ECX = status` (`+0x15 R13D`), `RDI = 지금 스레드` (`+0x39`), `R14 = 프로세스` (`+0x4a [RDI+0x220]`), `R12B = 마지막 표지` (처음 0).

| # | 줄 (call / ret) | 한 일 | 조건 |
|---|---|---|---|
| 1 | `+0x66 / +0x6b` | `PspClearProcessThreadCidRefs` | 늘 |
| 2 | `+0x75 → +0x740` | 붙은 프로세스가 자기 프로세스와 다르면 bugcheck `5` | 검사 |
| 3 | `+0x90` | IRQL 0 | |
| 4 | `+0xa3` · `+0xb1` · `+0xbe` | `ActiveExWorker` → bugcheck `0xe9` · `KernelApcDisable ≠ 0` → bugcheck `0x20` · `ChargeOnlySession (+0x568)` 정리 (`+0x77a`, `+0x790` 0) | |
| 5 | `+0xcb` · `+0xd3` · **`+0xdf / +0xe4`** | 속성 집합 비우기 · 컨테이너 가장 되돌리기 · **`ExWaitForRundownProtectionRelease(+0x588)`** | |
| 6 | `+0xee → +0x713` | `LegacyPowerObject (+0x570)` 지움 (`+0x718` 0) | 있을 때 |
| 7 | **`+0x100`** | **`Thread.ExitStatus = status`** | |
| 8 | `+0x119` | `EtwTraceThread` | ETW |
| 9 | `+0x122` | `KernelApcDisable −1` (끝날 때까지 커널 APC 안 받음) | |
| 10 | **`+0x142 / +0x147`** | **`PspCallThreadNotifyRoutines(Thread, 0, 0)`** — 스레드 끝 통지 | Minimal + Pico 아님 |
| 11 | `+0x156 ~ +0x176` | 프로세스 락 (`+0x1c8`) 독점 | |
| 12 | **`+0x184 ~ +0x18e`** | **`ActiveThreads −1`** | 락 안 |
| 13 | `+0x1a5 ~ +0x1bd` | 0 이면 `R12B = 1` → `+0x1bd JNZ +0x685` | |
| 14a | **`+0x685`** → **`+0x6a1 / +0x6a6`** | (마지막) **`Flags \|= 0x2000008`** (`ProcessDelete` · `ProcessRundown`) → **`PspExitLastThread` 단계 0** (락도 이 안에서 풂) | `R12B ≠ 0` |
| 14b | `+0x1c3 ~ +0x1cc` | (아님) `LastThreadExitStatus = status` (`0xc000004b` 면 안 씀) | `R12B == 0` |
| 15 | `+0x1d9 / +0x1de` | 프로세스 락 풀기 | 마지막 아닐 때 |
| 16 | `+0x1ef` | silo·작업 객체 정리 (이름만) | `Silo ≠ -3` |
| 17 | `+0x1f5` → `DbgkExitProcess` / `DbgkExitThread` | 디버거에 끝 알림 | `DebugPort` 있을 때 |
| 18 | `+0x202` | 보안 스레드 정리 (이름만) | 보안 프로세스 |
| 19 | `+0x20f` | 중요 스레드 끝 알림 (이름만) | 커널 디버거 + `BreakOnTermination` |
| 20 | `+0x58d / +0x592` | (마지막) **단계 1** | `R12B ≠ 0` |
| 21 | `+0x225` → `+0x6ab` 고리 | `TerminationPort (+0x4e8)` 목록에 LPC 끝 메시지 | 목록 있을 때 |
| 22 | `+0x235` | 예외 포트로 끝 메시지 | `ThreadInserted` |
| 23 | `+0x296` | Win32k 스레드 끝 콜아웃 (이름만) | `Win32Thread` 있을 때 |
| 24 | `+0x5a6 / +0x5ab` | (마지막) **단계 2** | `R12B ≠ 0` |
| 25 | `+0x2ff ~ +0x304` | `EnableStackSwap` 꺼져 있으면 bugcheck `0x94` | |
| 26 | `+0x30a ~ +0x33b` | FLS 첫 정리 (이름만) | |
| 27 | `+0x340 ~ +0x363` | I/O 취소 · 타이머 · 레지스트리 통지 · **뮤턴트 놓기** (`KiRundownMutants`) · 스케줄러 공유 칸 | |
| 28 | **`+0x368 ~ +0x4b8`** | **TEB 블록** (아래) | 아래 |
| 29 | `+0x4bd ~ +0x4d7` · `+0x5c9 ~ +0x5d3` | **`Thread.ExitTime (+0x4c8)`** | |
| 30 | `+0x5bf / +0x5c4` | (마지막) **단계 3** (`PspExitProcess` · `PspRundownSingleProcess`) | `R12B ≠ 0` |
| 31 | **`+0x4e3 / +0x4e8`** | **`KeRundownApcQueues`** | |
| 32 | `+0x4e8` → `+0x844` | 레고 통지 (간접, 이름만) | `LegoData` 있을 때 |
| 33 | `+0x4f5 ~ +0x614` | FLS 끝 정리 | |
| 34 | `+0x525 / +0x52a` | `PspThreadIndexCleanup` | |
| 35 | `+0x52a` → `+0x85a` | `SetContextState (+0x6a8)` 해제 | 있을 때 |
| 36 | `+0x53a ~ +0x547` | `OwnerEntryListHead` 비어 있지 않으면 bugcheck `0x1c6` | |
| 37 | **`+0x549` · `+0x54c / +0x551`** | **`KeTerminateThread(Thread)`** — 안 돌아옴 | 늘 |

- 마지막 표지 쪽은 `Flags3.Minimal` 변종 (`+0x619 ~ +0x64f`) 도 `R12B = 1` 로 합류한다 (이름만).

### TEB·유저 스택 블록 (질문 2(b)(c) — 표 28)

```
+0x368  RBX = KTHREAD.Teb                      0 이면 +0x377 → +0x4bd (블록 건너뜀)
+0x37d  KTHREAD.Teb = 0                        커널 쪽 칸 먼저 지움
+0x38a  PspLockUnlockTebAccessExclusive        (이름만)
+0x38f / +0x396  MiscFlags & 0x400 (SystemThread) → 건너뜀
+0x39c ~ +0x3a8  EPROCESS.Flags & 0x40000008 (ProcessDelete | ProcessSelfDelete) → 건너뜀
                 ← 마지막 스레드는 표 14a (+0x685) 가 먼저 ProcessDelete 를 켜 놔서 여기서 빠진다
+0x3ae ~ +0x3b6  CrossThreadFlags & 2 (ThreadInserted) 아니면 → +0x400 (스택만 건너뜀)
+0x3b8  RAX = [TEB+0x1478]  (DeallocationStack — TEB 에서 그 자리에서 읽음)
+0x3dc  R9D = 0x8000 (MEM_RELEASE) · RegionSize = 0 · +0x3ef RCX = -1 (자기 프로세스)
+0x3f2  ZwFreeVirtualMemory                    ← 유저 스택 예약 영역 통째로
 (WoW64 면 +0x42f [TEB+0x2e0c] 로 한 번 더 +0x457)
+0x400  RCX = [TEB+0x16a8] → ≠ 0 이면 +0x40f ObCloseHandle(…, KernelMode)
+0x496 ~ +0x4ac  ThreadInserted + CetUserShadowStack → +0x654 PspFreeCurrentThreadUserShadowStack
+0x4b8  MmDeleteTeb(프로세스, TEB)              ← TEB 를 하위 할당 영역에 돌려놓음
```

- `[정적]` **`SameTebFlags` (`TEB+0x17ee`) 는 안 본다** — `PspExitThread`·`MmDeleteTeb`·`PspThreadDelete`·`KeTerminateThread`·`PspExitLastThread` `.asm` 에 `0x17ee`/`0x17ec` 0건 (결과서). 조건은 커널 쪽 칸 넷뿐이다.
- `[정적]` `ThreadInserted` 는 `PspInsertThread +0x490 OR.LOCK [R14+0x5a0],2` 가 켠다 (run12). 끝까지 못 들어간 스레드는 스택을 여기서 안 푼다 (만든 쪽 되돌림이 맡음 — `PspInsertThread +0x9ab` (ret `+0x9b0`) 의 `MmDeleteTeb` 등, run12).
- `[정적]` `MmDeleteTeb`: `+0x47 KeStackAttachProcess` → `+0x56 MiObtainReferencedVadEx(TEB)` → `+0x69 MiFreeToSubAllocatedRegion(Vad, TEB, 크기)` (`+0x3d` `0x2000`, WoW64 `+0xa7` `0x4000`) → `+0x75 KiUnstackDetachProcess`. 만들 때 `MmCreateTeb +0xa7 MiAllocateFromSubAllocatedRegion` (run43) 의 짝이다.
- `[정적]` 크기 0 + `MEM_RELEASE` 라서 `RtlCreateUserStack` 이 예약한 영역 (커밋·가드 포함, run74) 이 한 번에 사라진다.
- `[정적]` (flow-writer) ntdll 쪽 `RtlExitUserThread` 직접 호출 5줄 · `LdrShutdownThread` 직접 호출 25줄에 가상 메모리 풀기가 **없다** (`_calls.tsv`). 그 아래 callee 는 안 봤다.

### 스레드 목록 둘 (질문 2(d))

| 목록 | 넣는 곳 | 빼는 곳 | 줄 |
|---|---|---|---|
| `KPROCESS.ThreadListHead (+0x30)` ← `KTHREAD.ThreadListEntry (+0x2f8)` | `KeStartThread +0x2f4 ~ +0x2fe` (run49) | **`KeTerminateThread`** | `+0x6c` 프로세스 스핀락 · `+0xe2` 프로세스 헤더 락 · 안전 검사 `+0xee ~ +0xf7` · **`+0x116 MOV [RAX],RDX` / `+0x119 MOV [RDX+8],RAX`** · `+0x11d` 헤더 락 풀기 |
| `EPROCESS.ThreadListHead (+0x370)` ← `ETHREAD.ThreadListEntry (+0x578)` | `PspInsertThread +0x6ac ~ +0x6f8` → `+0x239 ~ +0x243` (flow-writer — 아래 "보탠 것" ③) | **`PspThreadDelete`** (객체 지울 때) | `+0x13c PspLockProcessExclusive` · `ThreadListLock (+0x5e0)` 푸시락 `+0x155 ~ +0x16e` · `+0x17b KeFoldProcessStatisticsThread` · 안전 검사 `+0x180 ~ +0x190` · **`+0x374 MOV [RAX],RCX` / `+0x377 MOV [RCX+8],RAX`** · `+0x396` 풀기 |

- → **끝난 스레드도 `EPROCESS` 목록에는 객체가 지워질 때까지 남는다.** 그래서 `PspExitLastThread` 단계 0 이 이 목록을 돌며 아직 신호가 아닌 다른 스레드를 기다릴 수 있다 (질문 4).
- `KeTerminateThread` 쪽: 빼고 나서 목록에 자기뿐이었으면 (`+0x7f CMP [RSI],RAX / +0x82 JNZ` 를 안 탐) **프로세스도 전역 목록에서 뺀다** — `KiProcessListLock` (`+0xa5 XCHG`) · `+0xb5 LEA [RDI+0x110]` (`ProcessListEntry`) · `+0xce / +0xd1` · `+0xda` 큐 스핀락 놓기.

### `KeTerminateThread` (질문 3, `Thread = RCX → RBX`, 821B)

```
+0x46   WaitReason = 0x16 (WrTerminated [추측: 이름])
+0x4d ~ +0x57  IRQL 2
+0x6c ~ +0x13c  KPROCESS 스레드 목록에서 빼기 (+0x116 / +0x119), 마지막이면 프로세스 전역 목록에서도 (+0xce / +0xd1)
               SchedulingGroup → +0x12e · Queue → +0x152 (이름만)
+0x157  RBP = PRCB · +0x163 KiAcquireKobjectLockSafe(Thread)    ← 스레드 헤더 락
+0x16c  SignalState = 1                    이미 1 이어도 덮어씀 (KeSetEvent 처럼 옛 값을 보고 빠지지 않음)
+0x173 ~ +0x1a0  기다림 목록 머리 (Thread+8) 를 돌며
         WaitType == 1 (+0x184 / +0x187) → +0x18d R8D = WaitKey → +0x19b KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)
         WaitType 2 (큐) +0x2c4 KiInsertQueueInternal · 4 (DPC) +0x310 SignalState 0 + +0x31b KeInsertQueueDpc
         그 밖 (0 = 전부 기다림) +0x2fe R8D = 0x100 → 같은 KiTryUnwaitThread
+0x1a2 / +0x1a6  목록 머리째 비움 (블록을 하나씩 안 뗌)
+0x1a9  헤더 락 풀기
+0x1b0  State = 4 (Terminated [추측: 이름])
+0x1b7 / +0x1bf  PRCB+0x2d08 ≠ 0 → +0x1ce KiProcessThreadWaitList(Prcb, 1, 0, 0)     ← 깨운 스레드 준비 줄로 (run77)
+0x1d3 ~ +0x1ee  LEA R8,[Thread+0x4e8] (ReaperLink) → CMPXCHG.LOCK [PsReaperListHead]  (잠금 없는 push)
+0x1fc / +0x1ff  목록이 비어 있었으면 → +0x213 ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)  (실패면 +0x21c 표시만)
+0x22a ~ +0x23e  스레드 락 (Thread+0x40) 이 풀릴 때까지 기다렸다 0
+0x24f  KiSwapThread(Thread, Prcb, 0)       ← 안 돌아옴
```

- `[정적]` **깨우는 꼴은 run77 수동 재설정 이벤트와 같은 결과**: 기다리는 블록 **전부** 를 깨우고 신호는 1 로 남는다. 같은 `KiTryUnwaitThread` 를 부르고 반환값은 안 본다 (`+0x1a0 JMP +0x176`). test.exe 메인의 `WaitForSingleObject(t)` 블록은 `WaitKey 0` (run77 `KeWaitForSingleObject +0x1e2`) → 메인 `WaitStatus = 0` `[정적: 길]`.
- `[정적]` **`KiExitDispatcher` 는 안 부른다** — `KeSetEvent` (run77 `+0x10c`) 와 다른 점. 깨운 목록은 `+0x1ce` 로 스스로 비우고 곧 `KiSwapThread` 로 간다 (`KiSwapThread +0x14` 도 같은 목록을 한 번 더 확인 — run77). 까닭은 `[추측]` (IRQL 2 에서 곧 넘어가니까).
- `[정적]` **돌아오지 않는다**: 이 함수엔 준비 줄에 넣는 호출이 없고 (`KiQueueReadyThread` 류 없음, `_calls.tsv` 20줄), `KiSwapThread` 도 지금 스레드를 줄에 넣지 않는다 (run75). `State = 4` 라 다시 뽑힐 길이 없다. `PspExitThread +0x551` 이후는 맺음 코드뿐이다.
- `[정적]` 순서: **목록 빼기 → 신호 → 깨우기 → `State 4` → 청소부 → 넘기기**. TEB·유저 스택은 그보다 앞 (`PspExitThread` 표 28) 에서 이미 풀렸다.

### `PspExitLastThread` — 마지막 스레드 (질문 4, `(ECX = 단계, DL, R8 = 프로세스, R9 = 스레드, [스택] = status)`)

| 단계 | 부르는 곳 (`PspExitThread`) | 하는 일 |
|---|---|---|
| **0** | `+0x6a1 / +0x6a6` (프로세스 락 쥔 채) | `+0x122 KeForceResumeProcess` · `+0x127` 프로세스 `ExitStatus == 0x103` 이면 status 로 (`0xc000004b` 면 `LastThreadExitStatus`) · `+0x137 LEA R13,[RBX+0x370]` 으로 **`EPROCESS.ThreadListHead` 를 돌며** 자기 뺀 스레드의 `SignalState` (`+0x157 CMP byte [R15+4],0`) 가 0 이면 락 풀고 **`+0x248 KeWaitForSingleObject(그 스레드)`** → 다시 락. "활성 수가 0 이어도 다른 스레드가 `KeTerminateThread` 를 끝낼 때까지 기다린다" |
| **1** | `+0x58d / +0x592` | `Flags.BreakOnTermination` 이면 `PspCatchCriticalBreak` (이름만) |
| **2** | `+0x5a6 / +0x5ab` | `Win32Process (+0x298)` 있으면 Win32k **프로세스** 끝 콜아웃 (이름만) |
| **3** | `+0x5bf / +0x5c4` | `+0x61 / +0x6b` **`Process.ExitTime = Thread.ExitTime`** · **`+0x74 PspExitProcess`** · 감사 · `KernelApcDisable` 되올림 · **`+0xe1 PspRundownSingleProcess(Process, 1)`** (→ `+0xb5 ObKillProcess` · **`+0xbd MmCleanProcessAddressSpace`** · `+0x1d5 KeSetProcess`) · `+0xe9 LpcExitProcess` |

- `[정적: 호출 순서]` **마지막 스레드는 TEB·스택을 따로 안 푼다**: 표 14a 가 `ProcessDelete` 를 먼저 켜서 TEB 블록을 건너뛰고, 단계 3 의 `MmCleanProcessAddressSpace` 가 주소 공간째 치운다. 그 안 세부는 안 읽었다.
- 부르는 곳: `PspExitThread` 4곳 + `PsTerminateMinimalProcess` 4곳 (`refs`, 최소 프로세스, 이름만).

### 남의 스레드 끝내기 (질문 5)

- **(a) `PspTerminateThreadByPointer` 남 갈래 (`+0x50 ~ +0x92`)** — 요청서가 짐작한 `PsExitSpecialApc` 같은 이름은 **없다**:
  - `+0x50` 시스템 스레드 → `0xc0000022` · `+0x59 ~ +0x61` `Flags3.Minimal` → `0xc00000bb`.
  - **`+0x63 BTS.LOCK [RBX+0x5a0],0`** → 처음 켠 쪽만 **`+0x6e ExitStatus = status`** (이미 켜져 있으면 안 덮음).
  - **`+0x77 / +0x7c KeRequestTerminationThread`** → `+0x7c XOR EAX,EAX` — **곧 SUCCESS, 끝나길 안 기다린다.**
  - `KeRequestTerminationThread`: `+0x2e BTS.LOCK [RBX+0x78],0xf` (`TerminationApcRequest`, 이미면 끝) → `+0x4c` 스레드 락 → `ApcQueueable` 이면 스레드에 박힌 APC (`SchedulerApc +0x288` — `KeInitThread +0x11c ~ +0x152` 가 미리 초기화, `NormalRoutine = KiSchedulerApc`, 결과서) 를 `+0x78 KiInsertQueueApc` → `+0x89 KiSignalThreadForApc` → `+0xa7 KiExitDispatcher` → `+0xb6 KeAlertThread` · `+0xbe KeForceResumeThread`.
  - 대상 스레드 안에서: `KiSchedulerApc +0x41` 이 `ThreadFlags & 0x8000` 을 보고 → `+0x1d4 / +0x1db` APC 의 `KernelRoutine` 을 **`KiSchedulerApcTerminate`** 로 바꿔 → `+0x1f0 KiInsertQueueApc` 로 다시 넣음 → `KiDeliverApc +0x443 / +0x459` 가 그 주소면 → **`+0x46b KiSchedulerApcTerminate`** → `+0x29 ECX = Thread.ExitStatus` → **`+0x2f PspExitThread`** → `+0x34 INT3`. (두 번 넣는 순서는 세 곳의 줄을 맞춘 것이고 한 번에 따라가 본 건 아님 `[추측: 중간 한 걸음]`.) (→ run80 일부: `KiSignalThreadForApc` 의 깨우기는 `KeInsertQueueApc` 와 같은 몸 — `+0xfc State 7` · `+0x11b WaitStatus 0x100`, 보통 APC 라 `+0x219` · `+0x227` 두 칸 더. 기다리던 대상은 `KiSwapThread +0x2ee KiDeliverApc` 에서 받고, `KiDeliverApc +0x21b KiSchedulerApc` 직접 → `+0x241 JMP +0xac` 목록 다시 봄. 다시 넣은 APC 가 `+0x46b` 에 닿는 갈래는 아직 `[추측]`)
  - → **남 끝내기의 실제 끝냄 (TEB·스택 풀기 포함) 은 대상 스레드 자신의 문맥에서, 그 스레드가 다음에 커널 APC 를 받을 때** 일어난다.
- **(b) `PspTerminateAllThreads(Process, 건너뛸 스레드, status, 플래그)`**: 목록을 **뒤에서 앞으로** (`PspGetPreviousProcessThread`) 돌며 `+0x9e CMP RBP,R14` 로 부른 쪽을 빼고 **`+0xd4 PspTerminateThreadByPointer(스레드, status, 0)`** (APC 길). 플래그 `& 2` 면 부른 쪽도 (`+0x221`). 부르는 쪽 `NtTerminateProcess +0x243` · `PspTerminateProcess +0xf2`. `NtTerminateProcess` 는 자기 프로세스면 이어 `+0x147 Terminated` → **`+0x161 PspExitThread`** 로 자기도 끝낸다. 이 함수가 돌아와도 다른 스레드는 아직 안 끝났을 수 있고, 마지막으로 끝나는 쪽이 단계 0 에서 기다린다.

### `ObWaitForSingleObject` 가 스레드 객체를 받는 길 (질문 6)

- `+0x47 ObpReferenceObjectByHandleWithTag(핸들, SYNCHRONIZE, 형식 0, …)` → 객체 머리에서 `TypeIndex` → `ObTypeIndexTable` 로 형식 → **`+0x82 RCX = OBJECT_TYPE.DefaultObject (+0x20)`**.
  - `+0x86 TEST CL,1 / +0x89 JNZ +0xf8` — 비트 0 = 특수 꼴 (형식의 `WaitObject*` 칸으로 고름).
  - 아니면 `+0x8b / +0x8e JS` — **음수 = 커널 절대 포인터 그대로**, **0 이상 = `+0x90 ADD RCX,RDI` (본체 + 오프셋)**.
  - → `+0xb5 EDX = 6` (UserRequest) → `+0xba KeWaitForSingleObject(RCX, …)` (run77).
- `[정적: 읽은 줄 셋으로 유추]` 스레드 형식은 `DefaultObject == 0` 이라 `RCX = 본체 = _ETHREAD = _KTHREAD.Header` (둘 다 `+0x0`): `PspInitPhase0 +0x504 ~ +0x569` 의 스레드 형식 만들기 → `ObCreateObjectType +0x9 XOR R9D,R9D` (`DefaultObject` 인자 0) → `ObCreateObjectTypeEx` 는 `UseDefaultObject` 비트 (`0x4`) 일 때만 `&ObpDefaultObject` → 스레드 형식 값엔 이 비트가 안 켜짐 (`+0x50b OR [RSP+0x62],R12B` 의 `R12B = 0x80`, 결과서). 그래서 이벤트·뮤턴트·스레드가 같은 `KeWaitForSingleObject` 하나로 처리된다.

### 객체를 지울 때 — `PspReaper` · `PspThreadDelete` (질문 7)

- **청소부 `PspReaper`** (작업 항목 루틴, `PspInitPhase0 +0x9ae/+0x9b5` 로 등록): `KeTerminateThread +0x213` 이 큐에 넣으면 → `+0x16 XCHG [PsReaperListHead],1` 로 목록을 통째로 받아 스레드마다 **`+0x27 KeDeleteThread`** → `+0x3b XADD [Thread+0x5ec],-1` (`KernelStackReference`, 1 이었으면 `+0x55 KeEnumerateKernelStackSegments` = **커널 스택 해제**, `+0x5a InitialStack = 0`) → **`+0x69 ObfDereferenceObjectWithTag(Thread, 'Dflt')`** (만들 때 둔 참조 하나).
- **`PspThreadDelete`** (스레드 형식의 삭제 루틴, `PspInitPhase0 +0x510/+0x517` · `+0x546` 크기 `0x798`) — 참조 수가 0 이 될 때 불린다 `[추측: 객체 관리자 길]`:
  - **`MmDeleteTeb`·`ZwFreeVirtualMemory`·`NtFreeVirtualMemory` 를 부르는 줄이 없다** → TEB·유저 스택은 `PspExitThread` 몫이고 겹치지 않는다.
  - `+0x17 KeCleanupThreadState` · 새는지 검사 (bugcheck `0x94`·`0x13c`·`0x1c6`) · 커널 스택이 아직 있으면 `+0x45f KeEnumerateKernelStackSegments` (청소부를 못 거친 스레드 몫 `[추측]`) · **TID 반납** `+0x1cd ExMapHandleToPointer(PspCidTable)` · `+0x1e8 ExDestroyHandle` · `ThreadName`·가장 토큰 정리 · **EPROCESS 목록에서 빼기 `+0x374/+0x377`** · **프로세스 참조 내림** `+0x3d5`.
- `[소스]+[추측]` test.exe 는 `t` 를 닫는다 (`sample/test.c:36` `CloseHandle(t)`) — Worker 객체의 핸들 참조는 메인이 깨어 `CloseHandle` 할 때 내려간다. `PspThreadDelete` 는 그 `NtClose` 와 청소부 `+0x69` 중 늦은 쪽에서 불릴 것 (그 밖 참조는 안 셈). 결과서의 "프로세스 끝 가까이" 와 다르다 (아래 ①).

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[유저 — Worker 가 돌아옴]  test!Worker return 0                                   [소스] test.c:24
ntdll!RtlUserThreadStart → kernel32!BaseThreadInitThunk +0x20 → ntdll!RtlExitUserThread   (run25 · run29, 01_sprint Worker 콜스택 [관찰])
ntdll!RtlExitUserThread (status)                                                   [정적] (있던 .asm)
 ├─ (1) +0x2e  NtQueryInformationThread(-2, 0xc, 4B)     ≠ 0 (마지막) 이면 → (2)
 ├─ (2) +0x40  RtlExitUserProcess(status)                 안 돌아옴 (+0x40 INT3)
 ├─ (3) +0x46  LdrShutdownThread                          DllMain·TLS 이유 3 (run28)
 ├─ (4) +0x4d  TpCheckTerminateWorker(0)
 └─ (5) +0x56  NtTerminateThread(0, status)  ── syscall ──▶ 아래. 돌아오면 +0x56 JMP +0x39 → (2)

[커널 — 끝나는 스레드 자신]
nt!NtTerminateThread (핸들, status)
 │  +0x22 / +0x25  핸들 0 → +0x2e ActiveThreads · +0x34 / +0x37 == 1 → +0x59 0xc00000db (안 끝냄)
 │  +0x60 / +0x64  핸들 -2 → 검사 없이 +0x39
 ├─ (1) +0x46  PspTerminateThreadByPointer(지금, status, 1)
 │    │  +0x40  CrossThreadFlags.Terminated
 │    └─ +0x4f  PspExitThread(status)                     안 돌아옴
 ├─ (2) +0x9a  ObpReferenceObjectByHandleWithTag          그 밖 핸들 (자기면 +0xd4 → +0x39)
 └─ (3) +0xb4  PspTerminateThreadByPointer(남, status, 0) → +0x7c KeRequestTerminationThread → SUCCESS

PspExitThread (status)
 │  +0x100 ExitStatus · +0x122 KernelApcDisable −1
 ├─ (1)  +0x147  PspCallThreadNotifyRoutines(Thread, 0, 0)
 │  +0x18e  ActiveThreads −1 (프로세스 락 안) · +0x1ae / +0x1bd 0 이면 마지막
 ├─ (2)  +0x6a6  PspExitLastThread(0)       마지막만. 앞 +0x685 Flags |= ProcessDelete · ProcessRundown
 │  +0x1cc  아니면 LastThreadExitStatus
 ├─ (3)  +0x592  PspExitLastThread(1)       마지막만
 ├─ (4)  +0x5ab  PspExitLastThread(2)       마지막만
 │  +0x37d Teb = 0 · +0x38f SystemThread 아님 · +0x3a3 ProcessDelete|ProcessSelfDelete 꺼짐 (마지막은 여기서 빠짐)
 ├─ (5)  +0x3f7  ZwFreeVirtualMemory(-1, &TEB.DeallocationStack, &0, MEM_RELEASE)   ThreadInserted (+0x3b4)
 ├─ (6)  +0x414  ObCloseHandle(TEB+0x16a8)  ≠ 0 일 때
 ├─ (7)  +0x4bd  MmDeleteTeb(프로세스, TEB)
 │    ├─ (1) +0x4c  KeStackAttachProcess
 │    ├─ (2) +0x5b  MiObtainReferencedVadEx
 │    ├─ (3) +0x6e  MiFreeToSubAllocatedRegion   0x2000
 │    └─ (4) +0x7a  KiUnstackDetachProcess
 ├─ (8)  +0x4d7  KeQuerySystemTimePrecise(&ExitTime)
 ├─ (9)  +0x5c4  PspExitLastThread(3)       마지막만 → +0x79 PspExitProcess · +0xe6 PspRundownSingleProcess (→ +0xc2 MmCleanProcessAddressSpace)
 ├─ (10) +0x4e8  KeRundownApcQueues
 └─ (11) +0x551  KeTerminateThread(Thread)                 안 돌아옴
      │  +0x46 WaitReason 0x16 · +0x116 / +0x119 KPROCESS 목록에서 빼기
      │  +0x16c SignalState = 1
      ├─ (1) +0x1a0  KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)   블록마다 (run77). +0x1a2 / +0x1a6 머리째 비움
      │  +0x1b0 State = 4
      ├─ (2) +0x1d3  KiProcessThreadWaitList(Prcb, 1, 0, 0)     PRCB+0x2d08 ≠ 0 → 깨운 메인을 준비 줄로
      │  +0x1d3 ~ +0x1ee  ReaperLink → PsReaperListHead
      ├─ (3) +0x218  ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)   (Ghidra Ordinal_36) 목록이 비어 있었을 때
      └─ (4) +0x254  KiSwapThread                                안 돌아옴 (run75)

[커널 — 남 끝내기의 뒤쪽, 대상 스레드 안]
KeRequestTerminationThread  +0x2e TerminationApcRequest → +0x7d KiInsertQueueApc(SchedulerApc) · +0x8e KiSignalThreadForApc
KiSchedulerApc  +0x41 ThreadFlags & 0x8000 → +0x1d4 / +0x1db KernelRoutine = KiSchedulerApcTerminate → +0x1f5 KiInsertQueueApc   [추측: 중간 한 걸음]
KiDeliverApc  +0x443 / +0x459 → +0x470 KiSchedulerApcTerminate
 └─ +0x34  PspExitThread(Thread.ExitStatus)  → 위와 같은 길

[나중 — 작업 항목 · 참조 0]
PspReaper  +0x16 XCHG PsReaperListHead
 ├─ (1) +0x2c  KeDeleteThread
 ├─ (2) +0x5a  KeEnumerateKernelStackSegments   커널 스택 해제 (KernelStackReference 1 → 0)
 └─ (3) +0x6e  ObfDereferenceObjectWithTag('Dflt')   … 참조 0 이면 (형식 삭제 루틴) PspThreadDelete
PspThreadDelete  +0x1d2 ExMapHandleToPointer · +0x1ed ExDestroyHandle (TID) · +0x374 / +0x377 EPROCESS 목록에서 빼기 · +0x3da 프로세스 참조 내림
```

## 확인된 것

- `[정적]` 핸들 0 갈래: `NtTerminateThread +0x2e` `ActiveThreads` → `+0x34/+0x37` 1 이면 `+0x59 0xc00000db`, 아니면 `+0x41 PspTerminateThreadByPointer(지금, status, 1)`. `-2` (`+0x60/+0x64`) · 자기 핸들 (`+0xa5/+0xa8 → +0xd4`) 은 검사 없음.
- `[정적]` 자기 끝내기: `PspTerminateThreadByPointer +0x40 Terminated` → `+0x4a PspExitThread` → `+0x4f INT3`.
- `[정적]` `PspExitThread`: `+0x100 ExitStatus` → `+0x142 PspCallThreadNotifyRoutines(Thread, 0, 0)` → 락 안 `+0x18e ActiveThreads −1` → 0 이면 `+0x685 Flags |= 0x2000008` · `+0x6a1 PspExitLastThread(0)`, 아니면 `+0x1cc LastThreadExitStatus` → … → TEB 블록 (`+0x3f2 ZwFreeVirtualMemory` · `+0x40f ObCloseHandle` · `+0x4b8 MmDeleteTeb`) → `ExitTime` → `+0x4e3 KeRundownApcQueues` → `+0x54c KeTerminateThread`.
- `[정적]` TEB 블록 조건: `Teb ≠ 0` (`+0x368`, `+0x37d` 에서 0 으로) · 시스템 스레드 아님 (`+0x38f`) · `Flags & 0x40000008 == 0` (`+0x3a3`) · 스택은 `ThreadInserted` (`+0x3b4`). `SameTebFlags` 는 안 봄. 스택 주소는 `+0x3b8` 이 TEB 에서 바로 읽음.
- `[정적]` `KeTerminateThread`: `+0x116/+0x119` KPROCESS 목록 빼기 → `+0x16c SignalState 1` → `+0x19b KiTryUnwaitThread` 블록마다 → `+0x1a2/+0x1a6` 비움 → `+0x1b0 State 4` → `+0x1ce KiProcessThreadWaitList` → `+0x1d3 ~ +0x1ee` 청소부 목록 → `+0x213 ExQueueWorkItemEx` → `+0x24f KiSwapThread`.
- `[정적]` EPROCESS 목록은 `PspThreadDelete +0x374/+0x377` 이 뺀다 (`PspExitThread` 는 안 뺌). 넣는 줄은 `PspInsertThread +0x6e3/+0x6ea` → `+0x239 ~ +0x243` (flow-writer).
- `[정적]` 남 끝내기는 `PspTerminateThreadByPointer +0x77 KeRequestTerminationThread` → 곧 SUCCESS, 실제 끝냄은 대상 안 `KiSchedulerApcTerminate +0x2f PspExitThread`. 모두 끝내기는 `PspTerminateAllThreads +0xd4`.
- `[정적]` 마지막 스레드: `PspExitLastThread` 단계 0 (`+0x248` 다른 스레드 신호 기다림) · 3 (`+0x74 PspExitProcess` · `+0xe1 PspRundownSingleProcess` → `+0xbd MmCleanProcessAddressSpace`). TEB·스택은 따로 안 풂.
- `[정적]` `ObWaitForSingleObject +0x82` `DefaultObject` → `+0x90` 본체 + 오프셋. 스레드 형식 `DefaultObject == 0` 은 유추.
- `[정적]` `PspThreadDelete` 엔 TEB·스택 풀기 없음. `PspReaper +0x55` 커널 스택 해제 · `+0x69` 참조 내림.
- `[정적]` (flow-writer) ntdll `RtlExitUserThread`: `+0x29 NtQueryInformationThread(-2, 0xc)` → 마지막이면 `+0x3b RtlExitUserProcess`, 아니면 `+0x41 LdrShutdownThread` → `+0x48 TpCheckTerminateWorker(0)` → `+0x51 NtTerminateThread(0, status)` → 돌아오면 `+0x56 → +0x39 RtlExitUserProcess`.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① 결과서 질문 7 "**test.exe 는 `CreateThread` 로 받은 핸들 `t` 를 닫지 않으므로** (`sample/test.c:34-35`, 프로세스 끝까지) … `PspThreadDelete` 는 프로세스 끝(핸들 정리) 쪽에서 불리는 모양" → `sample/test.c:36` 에 **`CloseHandle(t);`** 가 있다 `[소스]`. PE_emulator 원문도 seq 386 `NtClose(0x70)` (201 메모 "가." 표 3줄). 그래서 Worker `ETHREAD` 를 지우는 때는 `NtClose(t)` 와 청소부 `PspReaper +0x69` 중 늦은 쪽 `[추측: 참조 수]`. 결과서 동적 항목 ("프로세스 끝 가까이에만 불리는가") 은 아래 "아직 확인 필요" 동적 6 처럼 고쳐 적었다.
- ② flow-writer 보탬: ntdll **`RtlExitUserThread` 본문** (있던 `.asm`, 직접 호출 5줄) — 결과서는 "ntdll 쪽은 이번에 안 읽음 `[추측]`". 위 "PE_emulator 쪽에서 쓸 것" 과 "호출 관계" 맨 위. 이 함수와 `LdrShutdownThread` 직접 호출에 가상 메모리 풀기가 없다는 것도 (`_calls.tsv`).
- ③ flow-writer 보탬: **`EPROCESS.ThreadListHead` 넣는 줄 전체** — `PspInsertThread +0x6ac LEA R12,[R15+0x5e0]` (`ThreadListLock`) → `+0x6bb KeAbPreAcquire` · `+0x6c3 BTS.LOCK` → `+0x6e3 LEA RCX,[R14+0x578]` · `+0x6ea LEA RAX,[R15+0x370]` → 안전 검사 `+0x6f1 ~ +0x6f8 JZ +0x239` → **`+0x239 ~ +0x243` 꼬리에 달기** → `+0x24e XADD` · `+0x261 KeAbPostRelease`. 결과서는 `+0x6e3 ~ +0x6ea` 만 적었다. 짐 지도 3.11 `ThreadListHead` 줄이 이것으로 `[추측]` → `[정적]` (아래 "짐 지도에 반영한 것").
- ④ 결과서 질문 3(a) "`+0x160 / ret +0x168 KiAcquireKobjectLockSafe(Thread)`" → 부르는 줄은 **`+0x163`** (ret `+0x168`). `+0x160` 은 `MOV RCX,RBX` (`_calls.tsv`, flow-writer `.asm`).
- ⑤ 결과서 질문 3(d) "`+0xda … KxReleaseQueuedSpinLock`" → `_calls.tsv` 는 `alias:KeReleaseInStackQueuedSpinLockFromDpcLevel` 로 찍는다. 같은 주소 PDB 이름 2개라 이 문서는 "큐 스핀락 놓기" 로 적었다 (`pdb_aliases.py --where`).
- ⑥ flow-writer 보탬: run53 의 범위 밖 훑기 표 `ChargeOnlySession` ← `PspExitThread +0x790` · `LegacyPowerObject` ← `+0x718` 은 **맞다** (둘 다 `MOV [RDI+…],RSI(0)`, 앞 줄이 참조 내림 `+0x78b` · `PoDeletePowerRequest +0x713`). `ExitStatus` ← `+0x100` · `PspTerminateThreadByPointer +0x6e` 도 맞다. `ReaperLink` 만 틀림 (아래 "정정").

## 정정

- **run53 범위 밖 훑기 표 `ReaperLink` 줄** — "reaper 줄에 매다는 쪽 (PspExitThread) 은 훑기에 안 잡힘"
  - → 매다는 쪽은 **`KeTerminateThread`** 다: `+0x1d3 LEA R8,[RBX+0x4e8]` → `+0x1e8 MOV [R8],RAX` → `+0x1ee CMPXCHG.LOCK [PsReaperListHead],R8` `[정적]` (결과서 질문 3, flow-writer `.asm`). `PspExitThread` 는 같은 칸을 `+0x225` 에서 **읽기만** (`TerminationPort` 목록, `.asm` 에서 `0x4e8]` 1건).
  - run53 문서 맨 위에 정정 안내를 한 줄 넣었다.

## run77 · run78 에 반영한 것

- run77: "결과" 머리 줄 (`… 스레드 객체가 신호 받는 쪽 (끝내기) 은 Q141d`) · "아직 확인 필요" 동적 6 · "정적 — 다음 바퀴 후보" 끝 줄 의 Q141d 에 "(→ run79)".
- run78: "정적 — 다음 바퀴 후보" 머리에 대기열 번호 **Q146** (오케스트레이터가 plan.md 에 붙인 번호).

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- 끝내기는 은퇴 뒤 (범위 밖) 라 **새 짐 칸 없음.** 이미 있는 줄에 "푸는 쪽·내리는 쪽" 근거만:
  - 3.3 `DeallocationStack` — 푸는 쪽 `PspExitThread +0x3b8` (읽기) → `+0x3f2 ZwFreeVirtualMemory(MEM_RELEASE)`, 조건.
  - 3.11 `ActiveThreads` — 내리는 쪽 `PspExitThread +0x18e`, 읽는 쪽 `NtTerminateThread +0x2e` (핸들 0).
  - 3.11 `ThreadListHead` — 넣는 줄 `PspInsertThread +0x6ac ~ +0x6f8 → +0x239 ~ +0x243` 로 **`[추측]` 짐작 → `[정적]` 정확** (위 ③). 빼는 쪽 `PspThreadDelete +0x374/+0x377`. 그래서 3.11 머리 "정확 178 · 짐작 5" → **정확 179 · 짐작 4**, 2장 "정확 675 · 짐작 55" → **정확 676 · 짐작 54** (아는 칸 730 은 그대로), 7.4 `ThreadListHead` 실마리 닫힘.
  - 3.11 `Flags` 16비트 줄 · `Flags.ProcessDelete` 줄 — 끝내기 길이 켜고 (`PspExitThread +0x685` `0x2000008`) 읽는 (`+0x3a3` `0x40000008`) 줄. `ProcessDelete` 줄의 "이름이 어색" 은 끝내기 길 쓰임과는 맞는다 — 만들 때 줄이 이 칸인지는 그대로 짐작.
  - 3.11 "범위 밖 52" 의 `LastThreadExitStatus` (`PspExitThread +0x1cc`) · `ExitTime` (`PspExitLastThread +0x6b`).
  - 3.12 `ThreadListHead` 둘째 줄 — 빼는 쪽 `KeTerminateThread +0x116/+0x119`.
  - 3.13 "범위 밖 69" — `ReaperLink` (`KeTerminateThread +0x1d3 ~ +0x1ee`, run53 정정) · `CrossThreadFlags.Terminated` (`PspTerminateThreadByPointer +0x40` · `+0x63`, `NtTerminateProcess +0x147`) · `ExitStatus` (`PspExitThread +0x100` · `PspTerminateThreadByPointer +0x6e`) 쓰는 줄 `[정적]`.
  - 3.14 머리에 "끝내기 줄" 메모 (`State 4` ← `KeTerminateThread +0x1b0` 등 — 은퇴 뒤라 표에 안 넣음).
  - 기준 근거 목록·판 목록 (서른다섯째판)·8장 두 표에 한 줄씩.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만. WinDbg 이름은 `nt!KeWaitForSingleObject`.

1. **Worker 가 핸들 0 으로 오나·마지막 아님인가** — `ntdll!RtlExitUserThread+0x2e` 에서 `eax`·`dd @rsp+0x48 L1` (기대 0 = 마지막 아님), `nt!NtTerminateThread` 입구 `rcx` (기대 0) · `+0x2e` 뒤 `ecx` (`ActiveThreads`, 기대 ≥ 2).
2. **`ActiveThreads` 내림** — `nt!PspExitThread+0x18e` 직전·직후 `dd <프로세스>+0x380 L1`. Worker 는 2 이상 → 하나 덜, `r12b` 0. 메인이 끝날 때는 1 → 0 이고 `+0x6a1 PspExitLastThread` 로 가나.
3. **스택·TEB 풀기** — Worker 에서 `nt!PspExitThread+0x3f2` 가 불리고 `rcx == -1`, `poi(rdx)` = Worker `TEB+0x1478`, `[스레드+0x5a0] & 2` 켜짐. 같은 스레드 `+0x4b8 MmDeleteTeb` 의 `rdx` = Worker TEB. **메인 (마지막) 은 둘 다 안 불리나** (`+0x3a8 JNZ`).
4. **`TEB+0x16a8`** (`DbgSsReserved` 둘째 칸) 이 test.exe 에서 0 인가 — `+0x40f ObCloseHandle` 가 불리나.
5. **신호·깨움** — `nt!KeTerminateThread+0x16c` 직후 `dd <Worker>+4 L1` = 1, `+0x19b` 호출 수·`r8` (기대 0), 메인 `WaitForSingleObject(t)` 가 `WaitStatus 0` 으로 깨나 (`nt!KiTryUnwaitThread+0x168` `rbx` = 메인), `+0x1b0` 뒤 `State 4`, `+0x24f` 뒤 Worker 가 다시 안 도나.
6. (결과서에서 고침) **`PspReaper`·`PspThreadDelete` 시점** — `bp nt!PspReaper`, `bp nt!PspThreadDelete` (첫 인자 = Worker `ETHREAD` 일 때). test.exe 는 `CloseHandle(t)` (`test.c:36`) 를 하므로 **`NtClose` 안인가 `PspReaper +0x69` 안인가** (`k`), `+0x374` 에서 목록에서 빠지는 시점.
7. **`PsThreadType` `DefaultObject`** — `dt nt!_OBJECT_TYPE poi(nt!PsThreadType) DefaultObject TypeInfo.UseDefaultObject` (기대 0 · 0), 또는 `nt!ObWaitForSingleObject+0x82` 뒤 `rcx`.
8. **남 끝내기 중간 한 걸음** (이 test.exe 엔 없음, `NtTerminateProcess` 길에서 볼 수 있음) — `nt!KiSchedulerApc+0x1d4` 가 `KernelRoutine` 을 바꾸나, `nt!KiDeliverApc+0x46b` 로 오나.
9. 두 스레드가 같은 때 `ActiveThreads` 를 내릴 때 (CPU 둘 이상) 누가 마지막인지는 런타임 — 2 와 같이.
- 추천 멈춤점 (결과서 + flow-writer, 모두 test.exe 조건): `ntdll!RtlExitUserThread+0x2e` · `+0x51`, `nt!NtTerminateThread+0x34` · `+0x41`, `nt!PspExitThread+0x18e` · `+0x1bd` · `+0x3a8` · `+0x3f2` · `+0x40f` · `+0x4b8` · `+0x54c`, `nt!KeTerminateThread+0x16c` · `+0x19b` · `+0x1b0` · `+0x24f`, `nt!PspReaper`, `nt!PspThreadDelete`, `nt!ObWaitForSingleObject+0x82`.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ① **`KiSchedulerApc` ↔ `KiDeliverApc` 중간 한 걸음** — 남 끝내기 APC 가 `KernelRoutine` 을 갈아 끼우고 다시 들어가 `KiSchedulerApcTerminate` 에 닿는 순서를 한 줄씩 (결과서 `[추측]`). Q141e (남의 스레드 문맥, sast/76) 의 APC 길과 겹칠 수 있음. (→ **Q147 ①**, run80 에서 일부 닫힘 — 깨우기 규칙·`KiDeliverApc +0x21b / +0x241`. 남은 것: `KiSchedulerApc +0x1df` 의 `ApcMode` 값과 `KiDeliverApc +0x392 ~ +0x443` 갈래)
- ② **스레드 형식 `DefaultObject == 0`** — `PspInitPhase0 +0x26f ~ +0x50b` 사이 `R12B` 가 정말 안 바뀌는지 (결과서 유추의 셋째 줄). 동적 7 로도 닫힘.
- ③ **프로세스 끝내기 쪽** — `PspExitProcess` · `PspRundownSingleProcess` · `MmCleanProcessAddressSpace` 본문 (마지막 스레드의 TEB·스택이 정말 주소 공간째 사라지나), 그리고 `NtTerminateProcess` 자기 프로세스 길 (`+0x147` · `+0x161` · `+0x243`) 에서 `ProcessSelfDelete` 를 켜는 곳 — 메인이 끝날 때 TEB 블록 조건 (`+0x3a8`) 을 정한다. PE_emulator `process_exit` 와 닿음.
- ④ (flow-writer) **ntdll `RtlExitUserProcess` → 커널** — 메인 스레드는 run7 `[관찰]` `ExitProcessImplementation → RtlExitUserProcess` 로 끝난다. 그 안에서 `NtTerminateProcess` 를 어떤 인자로 몇 번 부르나 (그래서 메인이 `PspTerminateAllThreads` → `+0x161 PspExitThread` 길인지).
- ⑤ (flow-writer) **`NtQueryInformationThread` 클래스 `0xc`** 커널 쪽 — `RtlExitUserThread +0x29` 가 묻는 값이 `ActiveThreads == 1` 인지 (`NtTerminateThread +0x34` 와 같은 칸을 보나).
- ⑥ (flow-writer) **`TpCheckTerminateWorker(0)`** (`RtlExitUserThread +0x48`) 가 하는 일 — ntdll 일꾼이 끝날 때와 갈리는 곳.

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구: 간접으로만 이어지는 이음새 (객체 형식 삭제 루틴 `PspThreadDelete`, 작업 항목 루틴 `PspReaper`, APC 루틴 `KiSchedulerApcTerminate`) 는 지도·`_calls.tsv` 에 안 나오고 `refs` 로만 나온다 (결과서 "스크립트 문제", run78 과 같은 꼴).
- 도구: `PspExitThread` `.asm` 끝에 같은 함수의 떨어진 범위 (`+0x27db2e` 이후) 가 붙어 있고, `+0x46f ~ +0x495` 는 읽지 않은 틈 (TEB 블록 안 예외 착지점, 흐름은 `+0x46d JMP +0x496`) — 결과서 그대로.
- 도구: 소스와 맞춰 볼 때는 `sample/*.c` 를 직접 연다. 결과서 질문 7 은 요청서의 `test.c:34-35` 두 줄만 보고 `:36` `CloseHandle` 을 놓쳤다.
