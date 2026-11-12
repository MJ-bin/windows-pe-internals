# run79 thread-exit — 해설

> 이 글은 [run79 문서](run79-thread-exit.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q141d**: PE_emulator 요청 25 의 ㄹ — 스레드 끝내기 (커널) (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q141e** (기다리는 남의 스레드에 문맥 넣기) → Q142. 그다음 **Q104 VM 단계** 앞에서 멈춘다 (사용자 결정 2026-11-12 "vm 제외하고 Q142 까지").
  - 동적 몫 아홉은 **Q104** 에 붙였다. 작은 정적 여섯은 새 **Q147** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run79-thread-exit.md`](run79-thread-exit.md) (가장 중요)
  - 요청서 [`_workspace/sast/75_thread-exit.md`](../../_workspace/sast/75_thread-exit.md) · 결과서 [`75_thread-exit_result.md`](../../_workspace/sast/75_thread-exit_result.md) (판정 **분석성공**. 못 정한 셋 — 남 끝내기 쪽지의 중간 한 걸음, 스레드 형식 `DefaultObject == 0` 은 읽은 줄 셋으로 유추, 마지막 스레드 단계 3 본문은 이름만)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 (PDB 목록 · 사진 메모 한 줄) · 2.1 제목과 `RtlCreateUserStack`·`MmCreateTeb`·`PspCallThreadNotifyRoutines` 절 한 줄씩 · 2.3 제목과 `PspUserThreadStartup` 한 줄 · 3장 `test!Worker` 스레드 두 줄 + **새 절 "스레드 끝내기"** · 4장 run77·run78 줄 셋 고침 + 새 줄 셋 · 5장 두 줄
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음). `ThreadListHead` 한 줄이 짐작 → 정확
  - run53 문서 맨 위 **정정** 한 줄, run77 · run78 문서에 번호·출처 몇 줄
- 이번 바퀴는 **정적만**이다. 값 대조도 없다.
  - 끝내기는 `main` 뒤라 run44 사진 구간 (초기화 → 은퇴) 밖이다.
- **이번 바퀴에서 조심할 곳 셋**:
  1. 결과서 질문 7 의 "test.exe 는 핸들 `t` 를 안 닫는다" 는 **틀렸다**. `sample/test.c:36` 에 `CloseHandle(t)` 가 있다 (2.17).
     - 오케스트레이터가 요청서에 소스를 `test.c:34-35` **두 줄만** 옮겨 적어서 생긴 실수다 (plan.md 결정 기록).
  2. ntdll 쪽 `RtlExitUserThread` 는 결과서가 **안 읽었다**. run 문서의 그 줄들은 flow-writer 가 이미 있던 `.asm` 으로 보탠 것이다 (2.4).
  3. run53 의 "퇴근한 스레드를 청소부 목록에 매다는 쪽은 `PspExitThread`" 는 틀렸다. `KeTerminateThread` 다 (2.11).

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드**, 커널 스택 = **관리사무소 안 작업대** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- `_EPROCESS` = **가게 장부**, `_KPROCESS` = 장부 맨 앞 몇 쪽 **운영 카드** → [run49 해설 "비유"](run49-kprocess-token-htable-해설.md#비유-그대로-이어-쓴다)
- 유저 스택 = 가게 안 **작업대**, TEB = 직원 **개인 안내판** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)
- 목수 (`RtlCreateUserStack`) 가 땅에 **말뚝** (예약) 박고, 맨 위에 **바닥** (커밋) 깔고, 그 아래 **줄** (가드) 친다. 쪽지 (`INITIAL_TEB`) 의 `A` 가 안내판 `DeallocationStack` 으로 → [run74 해설 2.5](run74-thread-create-해설.md) (바) · [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정)
- 서브할당 구역 (큰 자리를 잡아 두고 잘라 나눠 주는 곳, TEB 자리) → [run54 해설](run54-teb-fill-해설.md) 2.5
- 남의 땅에 잠깐 들어가기 (`KeStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 핸들 = **손잡이** → [run12 해설](../02_sprint/run12-insert-thread-해설.md)
- `ThreadInserted` 를 켜는 `PspInsertThread +0x490`, 직원 알림 (`PspCallThreadNotifyRoutines`) → [run12 해설 2.6](../02_sprint/run12-insert-thread-해설.md#26-가게-알림은-첫-직원-때만-직원-알림은-매번) · [run74 해설 2.6](run74-thread-create-해설.md) (바)
- 출근 인사 담당 (`BaseThreadInitThunk`) 이 본업 뒤 **퇴근** 시킨다 (`RtlExitUserThread`) → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.2 "한 줄 읽기 ②"
- DllMain·TLS 이유 3 = **퇴근** 인사 → [run28 해설](../02_sprint/run28-tls-init-call-해설.md)
- 출근 도장 창구 (`PspUserThreadStartup`) → [run15 해설 2.8](../02_sprint/run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸)
- APC = 직원 책상 **쪽지** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등), `KeSuspendThread` = **대기 팻말** → [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나)
- 신호등 **명판** (`_DISPATCHER_HEADER` = 종류 · 불 · 번호표 줄), 종류 6 = 스레드 객체, 수동/자동 신호등, 번호표 칸 (`WaitType`·`WaitKey`), 깨운 직원 명단 (`PRCB+0x2d08`), 퇴장 담당 (`KiExitDispatcher`), `0x100` = "처음부터 다시 확인" 도장, 창고 열쇠 (뮤턴트) → [run77 해설](run77-thread-wake-해설.md) 2.3 · 2.8 · 2.10
- 번호표 = **"나 여기서 기다려요"**, `KiTryUnwaitThread` = **어깨 두드리기** → [run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적)
- `KeWaitForMutexObject` 로 찍혔지만 `KeWaitForSingleObject` → [run13 해설 2.10](../02_sprint/run13-resume-thread-해설.md#210-이름-하나--kewaitformutexobject-로-찍혔지만-kewaitforsingleobject)
- CPU = **계산대**, PRCB = **계산대 장부**, 준비 줄, `State` 번호 (2 · 5 · 7) → [run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸) · [2.10](run75-thread-switch-해설.md#210-줄에-다시-세우기--기본-맨-뒤-선점당했고-몫이-남은-직원만-맨-앞), `KiSwapThread` → `SwapContext` = 계산대 **교대**, `call` 이 "한참 뒤에" 돌아온다 → [run75 해설](run75-thread-switch-해설.md) 2.8
- `WaitReason` = 업무 카드 **사유 칸** → [run76 해설](run76-thread-quantum-해설.md) 2.13
- IRQL = **"방해 금지" 단계 표지판**, 락 = **"작성 중" 자물쇠** → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) · [run11 해설](../02_sprint/run11-allocate-thread-해설.md)
- VAD = 땅의 **구획 장부** → [run44 해설](run44-baggage-snap-해설.md)
- 오프셋 순서 ≠ 실행 순서 ("부록 보고 오기") → [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)

**비유 한 가지 주의.**

- 이 글은 [run74 해설](run74-thread-create-해설.md) (직원 뽑기) 의 **짝** 이다. 그래서 같은 그림으로 **퇴근** 을 그린다.
  - 뽑을 때: 목수가 땅에 말뚝 박고, 바닥 깔고, 줄 쳐서 **작업대** 를 짓는다. 담당이 **개인 안내판** 을 건다.
  - 퇴근할 때: 같은 작업대를 **말뚝째 뽑고**, 안내판을 **떼어 반납** 한다.
- 기다리는 쪽은 run77 해설 그림 그대로다. 신호등 · 번호표 · 어깨 두드리기.
  - 새로: 직원 업무 카드 맨 앞에도 **신호등 명판** 이 붙어 있다 (스레드 객체, 종류 6). 이 글에선 그 불을 **"퇴근함" 불** 이라고 부른다.
- flow.md 3장 새 절의 비유는 조금 다르다: 사물함 (유저 스택) · 책상 (TEB) · 출근부 (`ActiveThreads`).
  - 같은 일을 다른 그림으로 본 것이다. 이 글은 run74 해설처럼 **작업대 · 개인 안내판** 으로 이어 쓴다.
  - "출근부" 는 run76 · run78 해설에서 `KeAccumulateTicks` (째깍마다 **출근부 기록**) 몫으로 이미 썼다. 겹치지 않게 `ActiveThreads` 는 가게 장부의 **"근무 중 인원" 칸** 이라고 부른다.
  - "쪽지" 는 앞 해설들처럼 APC 몫이다.

### 한 줄로 먼저

- 직원 (스레드) 의 퇴근은 **본인이** 한다 `[정적]`.
  - 남이 "퇴근해" 라고 해도 쪽지 (APC) 만 붙이고 돌아간다. 실제 정리는 그 직원이 자기 문맥에서 한다.
- 순서 (Worker 같은 "마지막이 아닌" 직원) `[정적]`:
  1. 가게 장부의 **"근무 중 인원"** (`ActiveThreads`, `_EPROCESS +0x380`) 을 1 내린다 — `PspExitThread +0x18e`.
  2. **작업대를 말뚝째 뽑는다** (유저 스택, `MEM_RELEASE`) — `+0x3f2`.
  3. **개인 안내판을 반납한다** (TEB, `MmDeleteTeb`) — `+0x4b8`.
  4. 맨 끝 `KeTerminateThread` (`+0x54c`): 운영 카드 직원 목록에서 빠지기 → **"퇴근함" 불 켜기** (스레드 객체의 신호, 2.3) → 기다리던 동료 **모두** 어깨 두드리기 → `State 4` → 청소부 수거함에 넣기 (커널 스택 같은 뒷정리를 나중에 몰아서 하는 `PspReaper` 의 목록, 2.11) → 계산대 내놓기 (`KiSwapThread`). **돌아오지 않는다.**
- 그래서 test.exe 메인이 깰 때 Worker 의 작업대·안내판은 **이미 없다** `[정적: 순서]`.
- **마지막 직원** (근무 중 인원을 내렸더니 0 이 된 직원, 2.13) 이면 작업대·안내판을 따로 안 치운다. 가게 문 닫기 (`PspExitLastThread`) 쪽으로 가서 땅째 치운다 `[정적]`.
- 인사 카드 자체는 더 나중에 **파쇄 담당** (객체 지우기 루틴 `PspThreadDelete`, 2.16) 이 지운다. 불리는 때는 아무도 그 카드를 손에 안 쥐고 있을 때 (참조 0) 로 보인다 `[추측: 객체 관리자 길]`. 이 담당은 작업대·안내판을 안 건드린다 `[정적]`.
- **손잡이 0** 은 그냥 "나" 가 아니다. "나 + 마지막이면 거절 (`0xc00000db`)" 이다 `[정적]`. ntdll 은 그 전에 "내가 마지막?" 부터 묻는다.
- PE_emulator 의 끝내기는 **줄기는 맞고 빠진 일이 넷** 이다 `[정적]` — 인원 내리기, 작업대·안내판 치우기, 깨우는 자리, 목록 둘.
- 결과서의 "test.exe 는 핸들 `t` 를 안 닫는다" 는 틀렸다. `test.c:36 CloseHandle(t)` 가 있다 `[소스]` (2.17).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 → … → NtContinue 은퇴 → 진입점 → main
 └─ main (메인 스레드)
     ├─ CreateThread(NULL, 0, Worker, NULL, 0, NULL)   [소스] test.c:34   직원 뽑기 (run74)
     ├─ WaitForSingleObject(t, INFINITE)                 [소스] test.c:35   기다림 (run77 · run78)
     │     ⋮   그동안 Worker 가 계산대를 받아 일한다 (run75 · run76)
     │     Worker: test!Worker  return 0                 [소스] test.c:24
     │        → BaseThreadInitThunk +0x20 → RtlExitUserThread → NtTerminateThread   ← 이번 (퇴근)
     │        → "퇴근함" 불 → 메인 어깨 두드리기                                    ← 이번
     ├─ (메인이 깸)
     └─ CloseHandle(t)                                   [소스] test.c:36   ← 이번 (2.17)
```

- 흐름도 (`flow.md` 그림) 의 범위는 **진입점까지**다. 끝내기는 `main` 뒤라 범위 **밖**이다.
  - 그래서 그림엔 안 넣고, flow.md 3장에 절 하나로 적었다.
- 그럼 왜 봤나?
  - `../PE_emulator` 가 둘째 스레드까지 돌리면서 "끝내기" 를 `PspExitThread` 이름으로 만들어 두었다. 그런데 **작업대·안내판 풀기는 공사 중** 이었다.
  - 요청 25 다섯 조각 중 넷째다:
    - ㄱ 만들기 (run74) · ㄴ 넘기기·퀀텀 (run75 · run76) · ㄷ 기다림·깨움·타이머 (run77 · run78) · **ㄹ 끝내기 (이번)** · ㅁ 남의 스레드 문맥 (Q141e).

### 무엇이 궁금했나

한 문장으로: **스레드가 끝날 때 커널은 무엇을 어떤 순서로 하나.** 특히 "퇴근함" 불과 기다린 쪽 깨우기, 작업대·안내판 치우기, 마지막 직원 갈래.

질문은 이렇게 쪼갰다 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | `NtTerminateThread` → `PspTerminateThreadByPointer` → `PspExitThread` 사슬, 자기/남 갈림 | 2.5 · 2.6 |
| 1' | (run 문서가 더함) 손잡이 0 은 무슨 뜻인가 — PE_emulator 원문 `NtTerminateThread(0, 0)` | 2.4 · 2.5 |
| 2(a) | `ActiveThreads` 내림, 마지막 갈래 | 2.7 |
| 2(b)(c) | TEB·유저 스택을 푸는 자리와 조건 (`SameTebFlags` 를 보나) | 2.8 |
| 2(d) | 가게의 스레드 목록에서 빼는 줄 | 2.12 |
| 2(e)·그 밖 | `KeTerminateThread` 부르는 자리, 통지·쪽지 정리 | 2.9 |
| 3 | `KeTerminateThread`: 신호, 깨우는 꼴, `State`, 넘기기 | 2.10 · 2.11 |
| 4 | 마지막 스레드 `PspExitLastThread` | 2.13 |
| 5 | 남 끝내기 · 모두 끝내기 | 2.14 |
| 6 | 기다리는 쪽이 스레드 객체를 받는 길 (`ObWaitForSingleObject`) | 2.15 |
| 7 | 객체가 지워질 때 (`PspThreadDelete`) | 2.16 · 2.17 |

### 바퀴 전에 알던 것

- `[관찰]` 01_sprint: Worker 가 끝날 때 `RtlExitUserThread` → `LdrShutdownThread` → DllMain·ExeTls (이유 3) (flow.md 3장 `test!Worker` 스레드).
- `[정적]` run25: `BaseThreadInitThunk +0x20` → ntdll `RtlExitUserThread`.
- `[정적]` run74: TEB 칸 `+8` · `+0x10` · `+0x1478` (`StackBase`·`StackLimit`·`DeallocationStack`) 은 `MmCreateTeb +0x12a ~ +0x13a` 가 채운다. 유저 스택은 커널 `RtlCreateUserStack` 이 잡는다 (예약 `ZwSetInformationProcess(0x29)` `[추측]`, 커밋 `+0x170`, 가드 `+0x1c6`).
- `[정적]` run74: 둘째 스레드 `SameTebFlags` 는 커널이 안 쓴다. 첫 스레드는 `0x400`.
- `[정적]` run77: 수동 재설정 깨움 = 모두 깨우고 신호로 남음. 한 물건 기다림의 `WaitStatus` = 0. `KeSetEvent +0x10c → KiExitDispatcher`. 기다림 입구 `NtWaitForSingleObject +0x65 → ObWaitForSingleObject +0xba → KeWaitForSingleObject`.
- `[정적]` run75: `KiSwapThread` 를 부르는 4곳 중 하나가 `KeTerminateThread +0x24f`.
- `[정적]` run15: `PspUserThreadStartup` 에 `PspTerminateThreadByPointer(스레드, 0xC000004B, TRUE)` 갈래가 있다 (끝까지 못 들어간 스레드). 그 안은 몰랐다.
- `[추측]` (PE_emulator) TEB·스택 풀기는 공사 중. ntdll `RtlExitUserThread` 는 스택을 안 품 (PE_emulator 원문에선 `[정적]`).

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다. 오프셋은 **돌아올 자리 (ret)** 다. 괄호 번호는 run 문서 그대로라 중간이 빈다.

```
[유저 — Worker 가 돌아옴]
ntdll!RtlExitUserThread (status)                                [정적] (있던 .asm, flow-writer)
 ├─ (1) +0x2e  NtQueryInformationThread(-2, 0xc, 4B)   "내가 마지막?" ≠ 0 이면 → (2)       ★ 2.4
 ├─ (2) +0x40  RtlExitUserProcess                       가게 문 닫기 쪽. 안 돌아옴
 ├─ (3) +0x46  LdrShutdownThread                        퇴근 인사 (이유 3, run28)
 ├─ (4) +0x4d  TpCheckTerminateWorker(0)
 └─ (5) +0x56  NtTerminateThread(0, status)  ── syscall ──▶ 아래. 돌아오면 → (2)

[커널 — 끝나는 직원 자신]
nt!NtTerminateThread (핸들, status)
 │  +0x34 / +0x37  핸들 0 이고 ActiveThreads == 1 → +0x59 0xc00000db (안 끝냄)           ★ 2.5
 ├─ (1) +0x46  PspTerminateThreadByPointer(지금, status, 1)
 │    │  +0x40  Terminated 도장                                                         ★ 2.6
 │    └─ +0x4f  PspExitThread(status)                    안 돌아옴
 └─ (3) +0xb4  PspTerminateThreadByPointer(남, status, 0) → +0x7c KeRequestTerminationThread  ★ 2.14

PspExitThread (status)
 ├─ (1)  +0x147  PspCallThreadNotifyRoutines(Thread, 0, 0)    퇴근 알림                  ★ 2.9
 │  +0x18e  ActiveThreads −1 · 0 이면 마지막 → PspExitLastThread 단계 0 · 1 · 2 · 3       ★ 2.7 · 2.13
 ├─ (5)  +0x3f7  ZwFreeVirtualMemory(-1, &TEB.DeallocationStack, &0, MEM_RELEASE)        ★ 2.8
 ├─ (7)  +0x4bd  MmDeleteTeb(프로세스, TEB)                                              ★ 2.8
 ├─ (10) +0x4e8  KeRundownApcQueues                                                      ★ 2.9
 └─ (11) +0x551  KeTerminateThread(Thread)                    안 돌아옴
      │  +0x116 / +0x119  KPROCESS 목록에서 빼기                                         ★ 2.12
      │  +0x16c  SignalState = 1                                                         ★ 2.10
      ├─ (1) +0x1a0  KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)   블록마다                 ★ 2.10
      │  +0x1b0  State = 4                                                               ★ 2.11
      ├─ (2) +0x1d3  KiProcessThreadWaitList(Prcb, 1, 0, 0)
      ├─ (3) +0x218  ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)   청소부 부르기            ★ 2.11
      └─ (4) +0x254  KiSwapThread                                  안 돌아옴

[나중]  PspReaper → … → (참조 0) PspThreadDelete                                         ★ 2.16
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- 이번에 새로 뽑은 함수 **16개** (모두 depth 0). 새 지도 둘: `_map/NtTerminateThread_*` (2,451개) · `_map/PspExitThread_*` (3,434개).
- `PspExitThread` 깊이 1 이 **49개** 라 통째로 안 뽑았다. 거의 ETW·락·LPC·콜백·bugcheck 라서 이름으로 골랐다.
- 지도·`_calls.tsv` 에 **안 보이는 이음새 셋** 은 `refs` (누가 이 함수를 가리키나) 로 찾았다.
  - `PspThreadDelete` (객체 형식의 삭제 루틴으로 등록), `PspReaper` (작업 항목 루틴으로 등록), `KiSchedulerApcTerminate +0x2f → PspExitThread`.
  - 셋 다 "부르기" 가 아니라 "주소를 어딘가에 적어 두기" 로 이어져서다. run78 의 `KiRetireDpcList` 와 같은 꼴이다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ (5) +0x3f7 ZwFreeVirtualMemory(-1, &TEB.DeallocationStack, &0, MEM_RELEASE)`

- **누가**: `PspExitThread`. 끝나는 직원 **자신** 이 커널에서 밟는 퇴근 절차다.
- **어디서**: 부르는 자리 `+0x3f2`, 돌아올 자리 `+0x3f7`.
- **누구를**: `ZwFreeVirtualMemory` — 가상 메모리 풀기.
- **인자 넷**:
  - `-1` = 자기 프로세스 (`+0x3ef RCX = R13`, `R13` 은 `+0x318` 에서 `-1`).
  - `&TEB.DeallocationStack` = 작업대 땅의 시작 주소 `A`. 바로 앞 `+0x3b8` 이 TEB `+0x1478` 에서 **그 자리에서** 읽었다.
  - `&0` = 크기 0.
  - `MEM_RELEASE` (`+0x3dc R9D = 0x8000`).
- **무엇을 위해**: 크기 0 + `MEM_RELEASE` 라서 예약 영역을 **통째로** 돌려준다 `[정적]`.
  - run74 의 목수가 박은 말뚝 (예약) · 깐 바닥 (커밋) · 친 줄 (가드) 이 한 번에 사라진다.
- 실제로 Worker 에서 불리는지는 동적이다 (Q104 동적 3).

#### 줄 2: `│ +0x16c SignalState = 1` — 호출이 아니라 **쓰기** 줄

```
KeTerminateThread +0x16c  MOV dword ptr [RBX+4],0x1
```

- **누가**: `KeTerminateThread`. `RBX` = 끝나는 스레드 (`Thread`).
- **어디에**: `Thread + 4` = 업무 카드 맨 앞 명판의 **불** (`_KTHREAD.Header.SignalState`).
- **무엇을**: 1 = 초록. "이 직원 퇴근함".
- **무엇을 위해**: 바로 다음 줄들 (`+0x173 ~ +0x1a0`) 이 이 명판 앞에 줄 선 번호표를 돌며 기다리던 직원 (test.exe 메인) 을 깨운다 (2.10).
- **눈여겨볼 것**: 앞에 옛 값을 보는 검사가 **없다**. 이미 1 이어도 그냥 덮어쓴다 `[정적]`.
  - `KeSetEvent` 는 "이미 초록이면 아무도 안 깨움" 이었다 (run77 해설 2.8). 다른 모양이다.

#### 줄 3: `│ +0x34 / +0x37 핸들 0 이고 ActiveThreads == 1 → 0xc00000db` — 갈림길

```
NtTerminateThread +0x2e  MOV ECX,[RAX+0x380]
NtTerminateThread +0x34  CMP ECX,1
NtTerminateThread +0x37  JZ  +0x59
```

- **누가**: `NtTerminateThread` — 관리사무소의 **퇴근 신청 창구**.
- **무엇을 보나**: `RAX` = 지금 직원이 붙은 가게 (`+0x27 [RSI+0xb8]`), `+0x380` = `ActiveThreads` (근무 중 인원).
- **갈림**: 1 이면 `+0x59` 로 → `EAX = 0xc00000db` → **안 끝내고** 돌려보낸다. 1 이 아니면 `+0x39` 로 → 자기 퇴근 (`+0x41`).
- 손잡이가 **0 일 때만** 이 줄에 온다 (`+0x22 / +0x25` — 0 이 아니면 `+0x60` 으로 빠진다). 뜻은 2.5.

### 2.3 무대 — 이번에 쓰는 칸과 새 말

뒤에서 오프셋이 나오면 이 절로 돌아오면 된다.

| 카드 | 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|---|
| 인사 카드 `_ETHREAD` | `+0x4c8` | `ExitTime` | 퇴근 시각 |
| | `+0x4e8` | `TerminationPort` = `ReaperLink` (같은 칸) | 청소부 수거함 고리 (2.11) |
| | `+0x578` | `ThreadListEntry` | 가게 장부 **직원 명부** 고리 (2.12) |
| | `+0x588` | `RundownProtect` | |
| | `+0x5a0` | `CrossThreadFlags` (`Terminated` 비트 0 · `ThreadInserted` 비트 1 · `BreakOnTermination` 비트 5) | **퇴근 예정 도장** · **입사 완료 도장** · 중요 직원 표시 |
| | `+0x5d8` | `ExitStatus` | 퇴근 사유 |
| 업무 카드 `_KTHREAD` | `+0x0` | `Header` (`SignalState +0x4`, 기다림 목록 `+0x8`) | 신호등 **명판** (불 · 번호표 줄) |
| | `+0x74` | `MiscFlags` (`SystemThread` 비트 10 · `ApcQueueable` 비트 14 · `CetUserShadowStack` 비트 20) | |
| | `+0x78` | `ThreadFlags` (`TerminationApcRequest` 비트 15) | "퇴근 요청 있음" 표시 (2.14) |
| | `+0xf0` | `Teb` | 안내판 위치 |
| | `+0x184` | `State` | 상태 번호 |
| | `+0x283` | `WaitReason` | 사유 칸 |
| | `+0x288` | `SchedulerApc` | 카드에 박힌 쪽지 한 장 (2.14) |
| | `+0x2f8` | `ThreadListEntry` | 운영 카드 **근무표** 고리 (2.12) |
| 운영 카드 `_KPROCESS` | `+0x30` | `ThreadListHead` | 근무표 머리 |
| 가게 장부 `_EPROCESS` | `+0x1c8` | `ProcessLock` | 장부 자물쇠 |
| | `+0x1f4` | `Flags` (`ProcessDelete` `0x8` · `ProcessRundown` `0x2000000` · `ProcessSelfDelete` `0x40000000`) | **"가게 닫는 중"** 표시 |
| | `+0x370` | `ThreadListHead` | 직원 명부 머리 |
| | **`+0x380`** | **`ActiveThreads`** | **근무 중 인원** |
| | `+0x38c` | `LastThreadExitStatus` | 마지막으로 나간 직원 사유 |
| | `+0x554` | `ExitStatus` | 가게 끝 사유 |
| 안내판 `_TEB` | `+0x1478` | `DeallocationStack` | 작업대 땅의 시작 `A` |
| | `+0x16a0` | `DbgSsReserved` (`+0x16a8` = 둘째 칸) | |
| | `+0x17ee` | `SameTebFlags` | 안내판 표시 칸 (이번엔 **안 읽힌다**) |

- 이름은 PDB (`pdb_struct.py`, run 문서 "칸 이름"), 쓰임은 `[정적]`.
- **숫자만 `[정적]`, 이름은 `[추측: 이름]`** 인 둘:
  - `State 4` = Terminated · `WaitReason 0x16` = `WrTerminated`.
  - run77 이 PDB 열거형 (`_KTHREAD_STATE`·`_KWAIT_REASON`) 을 확인했지만, 칸 타입이 바이트라 둘을 잇는 건 짐작이다.

#### 새 말 — `ActiveThreads` (근무 중 인원)

- 가게 장부 `+0x380` 의 숫자 칸 (PDB 이름).
- 코드에서 보이는 것 `[정적]`:
  - 퇴근 절차 `PspExitThread +0x18e` 가 **1 내린다**. 0 이 되면 "마지막" 으로 갈린다 (2.7).
  - 퇴근 신청 창구 `NtTerminateThread +0x2e` 가 **읽는다** (손잡이 0 일 때, 2.5).
- 비유: 가게 장부의 **"근무 중 인원"** 칸. 퇴근하는 직원이 **자기 손으로** 1 지운다. 지웠더니 0 이면 "내가 마지막".

#### 새 말 — "퇴근함" 불 (스레드 객체의 `SignalState`)

- [run77 해설](run77-thread-wake-해설.md) 2.3 의 명판 종류 표에서 **6 = `ThreadObject`** (직원 자신) 였다.
- 업무 카드 맨 앞 (`+0x0`) 이 바로 그 명판이다. 그래서 다른 직원이 이 직원을 **신호등처럼** 기다릴 수 있다 (`WaitForSingleObject(t)`).
  - 정말 "카드 맨 앞" 을 기다리는지는 2.15.
- 불이 켜지는 때 = 퇴근 맨 끝 `KeTerminateThread +0x16c` `[정적]`.
- 비유: 직원 명판의 **"퇴근함" 불**. 켜지면 그 앞에 줄 선 동료가 모두 지나간다 (2.10).

#### 새 말 — 퇴근 예정 도장 (`Terminated`) · 입사 완료 도장 (`ThreadInserted`)

- `Terminated` (`CrossThreadFlags` 비트 0): "이 직원은 퇴근 처리 중".
  - 켜는 곳 `[정적]`: `PspTerminateThreadByPointer +0x40` (자기) · `+0x63` (남), `NtTerminateProcess +0x147`.
  - 출근 도장 창구 (`PspUserThreadStartup`) 가 보던 그 비트다 (flow.md 2.3).
- `ThreadInserted` (비트 1): "가게에 끝까지 등록됨".
  - 켜는 곳: `PspInsertThread +0x490 OR.LOCK [R14+0x5a0],2` (run12, run74 해설 2.6 (바)).
  - 이번엔 **작업대 철거 조건** 으로 쓰인다 (2.8).

#### 새 말 — "가게 닫는 중" 표시 (`ProcessDelete`)

- 가게 장부 `Flags` 의 `0x8` 비트. 마지막 직원이 `PspExitThread +0x685` 에서 `ProcessRundown` (`0x2000000`) 과 같이 켠다 `[정적]`.
- 작업대·안내판 블록은 `+0x3a3` 에서 이 비트 (와 `ProcessSelfDelete`) 를 본다 (2.8).

### 2.4 유저 쪽 먼저 — `RtlExitUserThread`: "내가 마지막?" 부터 묻는다

**출처 주의.** 결과서는 ntdll 을 안 읽었다 ("ntdll 쪽은 이번에 안 읽음 `[추측]`"). 이 절은 flow-writer 가 **이미 있던** `ms/disassemble/ntdll/RtlExitUserThread.asm` 으로 보탠 것이다 (오케스트레이터도 `.asm` 으로 대조). 태그는 `[정적]`.

#### 순서 `[정적]`

| 걸음 | 줄 (call / ret) | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x29 / +0x2e` `NtQueryInformationThread(-2, 0xc, &buf, 4, NULL)` (`+0x16 R9D = 4` · `+0x25 EDX = R9 + 8`) | "내가 마지막?" 묻기. 성공이고 `buf ≠ 0` 이면 `+0x39` 로 | 관리사무소에 전화 |
| 2a | `+0x3b / +0x40` `RtlExitUserProcess` (`+0x40 INT3`) | **마지막이면** → 가게 문 닫기 쪽. 안 돌아옴 | |
| 2b | `+0x41 / +0x46` `LdrShutdownThread` | **아니면** → DllMain·TLS 이유 3 (run28) | 건물마다 퇴근 인사 |
| 3 | `+0x48 / +0x4d` `TpCheckTerminateWorker(0)` | 하는 일은 안 봤다 (Q147 ⑥) | |
| 4 | **`+0x51 / +0x56` `NtTerminateThread(0, status)`** (`+0x4d EDX = status` · `+0x4f ECX = 0`) | 커널에 "퇴근합니다" | 퇴근 신청 창구 |
| 5 | `+0x56 JMP +0x39` → `RtlExitUserProcess` | 커널이 **돌아오면** (예: `0xc00000db`) 가게 문 닫기 쪽 | |

- 클래스 `0xc` 의 이름 `ThreadAmILastThread` 는 공개 헤더에 맞춘 것이다 `[추측: 공개 헤더]`.
  - 커널이 이 질문에 무슨 칸을 보고 답하는지는 안 읽었다 (Q147 ⑤).
- PE_emulator 원문 Worker 줄 (seq 357 `NtQueryInformationThread` → seq 384 `NtTerminateThread`) 과 **순서가 맞는다**.

#### ntdll 은 작업대를 안 푼다 `[정적]`

- `RtlExitUserThread` 직접 호출 5줄, `LdrShutdownThread` 직접 호출 25줄에 가상 메모리 풀기가 **없다** (`_calls.tsv`).
  - 그 아래 callee 까지는 안 봤다.
- PE_emulator 메모 "ntdll `RtlExitUserThread` 는 스택을 안 품" 과 같은 결론이다. 푸는 건 커널이다 (2.8).

#### 비유

- [run25 해설](../02_sprint/run25-dll-entry-해설.md) 의 **출근 인사 담당** (`BaseThreadInitThunk`) 이 본업 뒤에 직원을 퇴근시킨다 (`+0x20`).
- 퇴근 담당 (`RtlExitUserThread`) 은:
  1. 먼저 관리사무소에 "저 마지막이에요?" 를 묻는다.
  2. 마지막이면 가게 문 닫는 쪽으로 간다.
  3. 아니면 동료 건물마다 퇴근 인사를 돈다 (이유 3).
  4. 그리고 관리사무소 창구에 "퇴근합니다" 를 낸다.
  5. 창구가 퇴근을 안 받아 주면 (돌아오면) 가게 문 닫는 쪽으로 간다.

### 2.5 입구 — `NtTerminateThread`: 손잡이 0 은 "나 + 마지막 검사" (질문 1 · 1')

- 인자: `RCX` = 핸들, `EDX` = status (`+0x20 MOV EBP,EDX`). `+0x17 RSI` = 지금 스레드.
- 손잡이 (핸들) 에 따라 네 갈래 `[정적]`:

| 손잡이 | 줄 | 어디로 | 마지막 검사 |
|---|---|---|---|
| `0` | `+0x22 / +0x25` 를 안 탐 → `+0x27` · `+0x2e` · **`+0x34 / +0x37`** | 1 이면 `+0x59 0xc00000db` (안 끝냄). 아니면 `+0x39` → **`+0x41 PspTerminateThreadByPointer(지금, status, 1)`** | **있음** |
| `-2` | `+0x60 / +0x64 JZ +0x39` | 같은 자기 길 | 없음 |
| 나를 가리키는 진짜 손잡이 | `+0x95` 손잡이 → 물건 → `+0xa5 / +0xa8` → `+0xcf` 참조 내림 → `+0xd4 JMP +0x39` | 같은 자기 길 | 없음 |
| 남을 가리키는 손잡이 | `+0x95` → `+0xaa R8D = 0` → **`+0xaf PspTerminateThreadByPointer(남, status, 0)`** → `+0xc0` 참조 내림 | 남 퇴근시키기 (2.14) | — |

- `+0x95` (ret `+0x9a`) `ObpReferenceObjectByHandleWithTag`: `EDX = 1`, 형식 `PsThreadType`, 모드 `PreviousMode`, 태그 `0x65547350`. 실패면 `+0x9e JS` 로 그 코드를 돌려준다.
- `0xc00000db` 의 이름 `STATUS_CANT_TERMINATE_SELF` 는 `[추측: 공개 헤더]`.

#### 왜 손잡이 0 만 검사하나 — 짐작의 단계

1. 근거 `[정적]`: ntdll 은 `NtTerminateThread` 를 부르기 **전에** `+0x29` 에서 "마지막?" 을 묻는다. 마지막이면 프로세스 끝내기로 간다 (2.4).
2. 근거 `[정적]`: ntdll 이 넘기는 손잡이가 바로 **0** 이다 (`+0x4f ECX = 0`).
3. 근거 `[정적]`: 커널이 `0xc00000db` 로 돌려보내면 ntdll 은 `+0x56 JMP +0x39` 로 **프로세스 끝내기** 로 간다.
4. 그래서: 물어본 뒤 그 **사이에** 동료가 다 나가서 "내가 마지막" 이 된 경우를, 커널 `+0x34` 가 한 번 더 받아 주는 **둘째 겹** 으로 보인다.
5. 아직 확인 아님: 이 뜻은 `[추측: 뜻]` 이다. 클래스 `0xc` 커널 쪽이 같은 칸을 보는지도 안 읽었다 (Q147 ⑤).

- 비유:
  - 퇴근 담당이 먼저 "저 마지막이에요?" 를 물었다.
  - 그래도 창구는 **"이름 없는" (손잡이 0) 퇴근 신청** 이 오면 장부를 한 번 더 본다.
  - 근무 중 인원이 1 이면 "당신이 마지막이니 가게 문 닫는 쪽으로 가세요" 하고 신청서를 돌려준다.
  - "나 (`-2`)" 라고 이름을 대고 오거나, 내 인사 카드 손잡이를 들고 오면 이 확인을 안 한다.
- test.exe Worker: 메인이 `WaitForSingleObject(t)` 에서 기다리는 중이라 `ActiveThreads ≥ 2` → `+0x41` 자기 길일 것이다 `[추측: 순서]` (Q104 동적 1).
- PE_emulator 원문 `NtTerminateThread(0, 0)` 의 손잡이 0 은 그냥 "자기" 가 아니라 **"자기 + 마지막 검사"** 갈래다. 넣는다면 한 줄이다 (run 문서 표 1').

### 2.6 자기 퇴근 — `PspTerminateThreadByPointer`: 도장 찍고 곧장 `PspExitThread`

- 인자 `(Thread = RBX, ExitStatus = ESI, 직접 = BPL)`.

#### 자기 + 직접 `[정적]`

1. `+0x2d TEST BPL / +0x30 JZ +0x50` — 직접 (1) 이면 안 탄다.
2. `+0x3b CMP RBX,지금 / +0x3e JNZ +0x50` — 자기면 안 탄다.
3. **`+0x40 OR.LOCK [RBX+0x5a0],1`** — 퇴근 예정 도장 (`Terminated`).
4. `+0x48 ECX = status` → **`+0x4a` (ret `+0x4f`) `PspExitThread`**.
5. `+0x4f INT3` — 돌아올 일이 없는 호출 바로 뒤에 놓인 줄이다 (결과서 "돌아오지 않는 호출").

- 그 밖 (남, 또는 직접 = 0) 은 `+0x50` 갈래로 간다 (2.14).
- 그 앞 `+0x29 / +0x2b` — `BreakOnTermination` 이면 `+0xc8 PspCatchCriticalBreak` (중요 직원이 끝날 때 멈춤, 이름만).

#### 부르는 곳 `[정적]`

- `PspTerminateThreadByPointer` 를: `NtTerminateThread +0x41 / +0xaf` · `NtTerminateProcess +0x1ec` · `PspTerminateAllThreads +0xd4 / +0x221` · `PspUserThreadStartup +0xf0`.
- `PspExitThread` 를: 이 함수 `+0x4a` · `NtTerminateProcess +0x161` · `KiSchedulerApcTerminate +0x2f` **셋**.
- 비유: 퇴근 절차 (`PspExitThread`) 로 들어가는 문은 셋뿐이다. 스스로 퇴근 · 가게 닫으며 퇴근 · 쪽지 받고 퇴근.

#### 덤 — 출근 도장 창구의 "못 들어간 직원" (flow.md 2.3 한 줄)

- [run15 해설 2.8](../02_sprint/run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸) 의 출근 도장 창구엔 이런 갈래가 있었다:
  - `ThreadInserted = 0` (끝까지 못 들어간 직원) 이면 `+0xf0` (ret `+0xf5`) `PspTerminateThreadByPointer(스레드, 0xC000004B, TRUE)`.
- 이번에 그 안이 보였다 `[정적]`: 위 자기 + 직접 길 → `PspExitThread`.
  - `ThreadInserted = 0` 이라 `PspExitThread +0x3b4 / +0x3b6` 에서 **작업대 철거는 건너뛴다**.
  - 안내판은 조건이 맞으면 `+0x4b8` 에서 반납한다 (2.8).
- run 문서는 "끝까지 못 들어간 스레드" 의 작업대는 만든 쪽 되돌림이 맡는다고 적었다 (`PspInsertThread +0x9ab` 의 `MmDeleteTeb` 등, run12). 이 출근 도장 창구 길에서 작업대를 **누가** 치우는지는 이번 근거에 없다 `[공백]`.

### 2.7 `PspExitThread` ① — 근무 중 인원 −1, 그리고 "내가 마지막?" (질문 2(a))

#### 레지스터

- `ECX` = status (`+0x15 R13D`), `RDI` = 지금 스레드 (`+0x39`), `R14` = 프로세스 (`+0x4a [RDI+0x220]`).
- **`R12B` = 마지막 표지** (처음 0). 이 뒤로 여러 번 다시 본다.

#### 앞부분 `[정적]`

- `+0x100` `ExitStatus = status` — 인사 카드에 퇴근 사유.
- `+0x122` `KernelApcDisable −1` — 끝날 때까지 커널 쪽지를 안 받는다.
- `+0x142` 퇴근 알림 (2.9).

#### 인원 내리기 `[정적]`

```
PspExitThread +0x184  MOV EAX,[R14+0x380]
PspExitThread +0x18b  SUB EAX,R15D
PspExitThread +0x18e  MOV [R14+0x380],EAX
```

| 걸음 | 줄 | 하는 일 |
|---|---|---|
| 1 | `+0x156 ~ +0x176` | 가게 장부 자물쇠 (`ProcessLock +0x1c8`) 를 혼자 잡기 (`+0x171 ExfAcquirePushLockExclusiveEx`) |
| 2 | **`+0x184 ~ +0x18e`** | **`ActiveThreads −1`** (위 세 줄) |
| 3 | `+0x1a5 ~ +0x1ae` | 다시 읽어 0 이면 `+0x1ae CMOVZ` → 마지막 표지 `R12B = 1` |
| 4 | **`+0x1bd JNZ +0x685`** | 마지막이면 → 5a |
| 5a (마지막) | **`+0x685 OR.LOCK [R14+0x1f4],0x2000008`** → **`+0x6a1 / +0x6a6 PspExitLastThread` 단계 0** | "가게 닫는 중" (`ProcessDelete`·`ProcessRundown`) → 가게 문 닫기 첫 단계 (2.13). 자물쇠도 그 안에서 푼다 |
| 5b (아님) | `+0x1c3 ~ +0x1cc` | `LastThreadExitStatus = status` (단 `0xc000004b` 면 안 씀) |
| 6 | `+0x1d9 / +0x1de` | 자물쇠 풀기 (마지막이 아닐 때만 여기서) |

- 모두 **자물쇠 안** 이다. 그래서 계산대가 둘이어도 "내가 마지막" 은 한 명만 된다. **누가** 될지는 실행 때 정해진다 (run 문서 동적 9).
- 비유:
  - 퇴근하는 직원이 가게 장부 자물쇠를 잠근다.
  - "근무 중 인원" 을 1 지운다.
  - 0 이 되면 "내가 마지막" 쪽지를 주머니에 넣고 (`R12B`), 장부에 "가게 닫는 중" 을 적는다.
  - 아니면 "마지막으로 나간 직원 사유" 칸에 자기 사유를 적고 자물쇠를 푼다.
- PE_emulator: 이 줄이 **빠져 있다** — 더해야 함 (run 문서 표 2(a)).

### 2.8 `PspExitThread` ② — 작업대 철거, 안내판 반납 (질문 2(b)(c))

이번 바퀴의 가운데 이야기다. [run74 해설](run74-thread-create-해설.md) 2.5 (바) "말뚝, 바닥, 줄" 을 **거꾸로** 돌린다.

#### 블록 한 장 — 위에서 아래로 실행 순서 `[정적]`

| 걸음 | 줄 | 하는 일 | 여기서 빠지는 직원 |
|---|---|---|---|
| 1 | `+0x368` | 업무 카드 `Teb` 칸 읽기. 0 이면 `+0x377` 에서 블록 통째로 건너뜀 (→ `+0x4bd`) | 안내판이 없는 직원 |
| 2 | `+0x37d` | 업무 카드 `Teb` 칸을 **먼저 0 으로** | |
| 3 | `+0x38a` | `PspLockUnlockTebAccessExclusive` (이름만) | |
| 4 | `+0x38f / +0x396` | `SystemThread` (`MiscFlags 0x400`) 면 건너뜀 | 시스템 스레드 |
| 5 | `+0x39c ~ +0x3a8` | 가게 `Flags & 0x40000008` (`ProcessDelete \| ProcessSelfDelete`) 면 건너뜀 | **마지막 직원** (2.7 의 `+0x685` 가 먼저 켰다) |
| 6 | `+0x3ae ~ +0x3b6` | `ThreadInserted` 가 꺼져 있으면 → `+0x400` (작업대만 건너뜀) | 입사 완료 도장이 없는 직원 |
| 7 | **`+0x3b8`** | `DeallocationStack` (`TEB+0x1478`) 을 **안내판에서 그 자리에서** 읽음 | |
| 8 | `+0x3dc` · `+0x3ef` | `MEM_RELEASE (0x8000)` · 크기 0 · 자기 프로세스 (`-1`) | |
| 9 | **`+0x3f2 / +0x3f7`** | **`ZwFreeVirtualMemory`** — 작업대 땅 통째로 | |
| (WoW64) | `+0x42f` → `+0x457` | 32비트 안내판의 작업대 하나 더 (`TEB+0x2e0c`) | test.exe (64비트) 는 해당 없음 |
| 10 | `+0x400` → `+0x40f / +0x414` | `TEB+0x16a8` (`DbgSsReserved` 둘째 칸) 이 0 이 아니면 `ObCloseHandle(…, KernelMode)` — 디버그 쪽 손잡이로 보인다 `[추측: 뜻]` | |
| 11 | `+0x496 ~ +0x4ac` → `+0x654` | `ThreadInserted` + `CetUserShadowStack` 이면 `PspFreeCurrentThreadUserShadowStack` (이름만) | |
| 12 | **`+0x4b8 / +0x4bd`** | **`MmDeleteTeb(프로세스, TEB)`** — 안내판 반납 | |

- 조건은 커널 쪽 칸 **넷뿐** 이다 `[정적]`: `Teb ≠ 0`, 시스템 스레드 아님, 가게 `Flags`, `ThreadInserted` (작업대만).
- `+0x46f ~ +0x495` 는 `.asm` 에 "읽지 않은 틈" 으로 나온다. 블록 안 예외 착지점이고 흐름은 `+0x46d JMP +0x496` 으로 이어진다 (결과서).

#### 핵심 ① — 치우는 건 커널, 그것도 **퇴근하는 직원 자신**

- 이 블록은 `PspExitThread` 안이다. `PspExitThread` 는 끝나는 스레드의 문맥에서 돈다 (2.6 · 2.14) `[정적]`.
- ntdll 은 안 푼다 (2.4). 인사 카드 파쇄 담당 `PspThreadDelete` 도 안 푼다 (2.16).
- 그래서 **한 곳에서만** 푼다. 겹치지 않는다 `[정적]`.

#### 핵심 ② — 작업대 주소는 안내판에서 **그때** 읽는다

- `+0x3b8` 은 커널 쪽 사본이 아니라 유저 메모리 (TEB) 의 칸을 그 시점에 읽는다 `[정적]` (결과서).
- 그 칸의 값 `A` 는 run74 의 목수가 쪽지 (`INITIAL_TEB`) 에 적은 예약 시작이다. `MmCreateTeb` 가 그걸 안내판에 옮겼다 (run74 해설 2.5 (바)).
- 비유: 철거반은 관리사무소 서류를 안 본다. 직원 안내판에 적힌 **"작업대 땅 시작 번지"** 를 그 자리에서 읽고, 그 땅을 통째로 반납한다.

#### 핵심 ③ — 크기 0 + `MEM_RELEASE` = 말뚝째 뽑기

- `MEM_RELEASE` 는 예약까지 통째로 돌려주는 풀기 꼴로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- run 문서: 크기 0 + `MEM_RELEASE` 라서 `RtlCreateUserStack` 이 예약한 영역 (커밋·가드 포함, run74) 이 한 번에 사라진다 `[정적]`.
- 뽑을 때와 나란히 놓으면:

| | 뽑을 때 (run74, `RtlCreateUserStack`) | 퇴근할 때 (이번, `PspExitThread`) |
|---|---|---|
| 말뚝 (예약 `M`) | `+0x104` `ZwSetInformationProcess(0x29)` `[추측: 예약]` | |
| 바닥 (커밋 `C`) | `+0x170` `ZwAllocateVirtualMemory` | `+0x3f2` `ZwFreeVirtualMemory(-1, &A, &0, MEM_RELEASE)` **한 번에** |
| 줄 (가드 `3P`) | `+0x1c6` `ZwAllocateVirtualMemory` | |

- 비유: 큰 땅에 말뚝 박고, 맨 위 한 칸에 바닥 깔고, 그 아래 세 칸에 줄 쳤던 것을 — **말뚝째 뽑아** 한 번에 치운다. 바닥·줄도 같이 사라진다.

#### 핵심 ④ — `SameTebFlags` 는 안 본다

- 요청서 짐작: `SameTebFlags` 의 어떤 비트 (`FreeStackOnTermination`? `InitialThread`?) 로 "풀지 말지" 를 가르나?
- 답 `[정적]`: **안 본다.**
  - 다섯 함수 (`PspExitThread`·`MmDeleteTeb`·`PspThreadDelete`·`KeTerminateThread`·`PspExitLastThread`) `.asm` 에 `0x17ee`/`0x17ec` 가 0건 (결과서 `grep`).
  - `FreeStackOnTermination` 같은 비트는 공개 PDB `SameTebFlags` 에 없다.
- 그래서 run74 의 "둘째 스레드 `SameTebFlags` 는 커널이 안 씀" 과 부딪칠 일이 없다. 퇴근 절차는 그 칸을 아예 안 읽는다.

#### 핵심 ⑤ — 안내판 반납 `MmDeleteTeb` 는 `MmCreateTeb` 의 짝 `[정적]`

| 걸음 | 줄 (call) | 하는 일 | 만들 때 짝 |
|---|---|---|---|
| 1 | `+0x47` | `KeStackAttachProcess` — 남의 땅에 잠깐 들어가기 | `MmCreateTeb` 도 붙는다 (run54) |
| 2 | `+0x56` | `MiObtainReferencedVadEx(TEB)` — 이름으로는 안내판이 놓인 구획 찾기 `[추측: 이름]` | |
| 3 | `+0x69` | `MiFreeToSubAllocatedRegion(Vad, TEB, 크기)` — 크기 `+0x3d` `0x2000` (WoW64 `+0xa7` `0x4000`) | `MmCreateTeb +0xa7 MiAllocateFromSubAllocatedRegion` (run43) |
| 4 | `+0x75` | `KiUnstackDetachProcess` — 나오기 | |

- 비유: run54 해설의 **서브할당 구역** 에서 잘라 받아 온 안내판 자리를, 같은 구역에 **돌려놓는다**.
- `MmDeleteTeb` 를 부르는 곳은 셋이다. 이 퇴근 줄 하나 + 만들다 실패했을 때 되돌림 둘 (`PspAllocateThread` · `PspInsertThread +0x9ab`) — run74 와 짝.
- flow.md 2.1 `RtlCreateUserStack` 절 · `MmCreateTeb` 절에 각각 **"푸는 쪽"** 한 줄이 붙었다. 이제 두 절이 만들기 ↔ 풀기 짝으로 읽힌다.

#### test.exe 로 읽으면

- Worker: 마지막 아님 · 유저 스레드 · 입사 완료 도장 있음 → 작업대·안내판 **둘 다 푼다** 일 것이다 `[추측]` (Q104 동적 3).
- 메인 (마지막): `+0x3a8` 에서 블록을 건너뛸 것이다 (Q104 동적 3, 2.13).
- PE_emulator: "공사 중 (안 품)" → **더해야 함**. 순서까지 맞추면: 작업대 → 손잡이 닫기 → 안내판. 그리고 모두 `KeTerminateThread` **앞** (run 문서 규칙 4).

### 2.9 `PspExitThread` ③ — 나머지 순서 한 장

run 문서 "순서 한 장" 그대로 (Worker 같은 "마지막이 아닌 유저 스레드"):

| 순 | 한 일 | 줄 | 비유 |
|---|---|---|---|
| 1 | `ExitStatus` 적기 | `PspExitThread +0x100` | 인사 카드에 퇴근 사유 |
| 2 | 스레드 끝 통지 | `+0x142 PspCallThreadNotifyRoutines` | "직원 퇴근" 알림 |
| 3 | 프로세스 락 → **`ActiveThreads −1`** → 0 아니면 `LastThreadExitStatus` 적고 락 풀기 | `+0x18e` · `+0x1cc` · `+0x1d9` | 근무 중 인원 (2.7) |
| 4 | 디버거·LPC 끝 메시지·Win32k 콜아웃·FLS | `+0x7b9/+0x27dbac` · `+0x6d0/+0x273` · `+0x2f1` · `+0x336` | (이름만) |
| 5 | I/O 취소·타이머·뮤턴트 놓기 | `+0x340 ~ +0x363` | 들고 있던 창고 열쇠 내려놓기 |
| 6 | **유저 스택 풀기** → 디버그 핸들 닫기 → **`MmDeleteTeb`** | `+0x3f2` · `+0x40f` · `+0x4b8` | 작업대 철거·안내판 반납 (2.8) |
| 7 | `ExitTime` | `+0x4bd` | 퇴근 시각 |
| 8 | **APC 큐 비우기** | `+0x4e3 KeRundownApcQueues` | 책상 쪽지 다 떼기 |
| 9 | **`KeTerminateThread`** | `+0x54c` | 불 켜고 계산대 내놓기 (2.10 · 2.11) |

- 모두 `[정적]`.
- 4 의 `+0x7b9`·`+0x27dbac` 같은 큰 숫자는 함수 뒤로 빼 둔 조각이다.
  - `PspExitThread` `.asm` 끝에는 같은 함수의 떨어진 범위 (`+0x27db2e` 이후) 가 붙어 있다 (결과서).
  - [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다) 의 "부록 보고 오기" 와 같다. 오프셋이 커도 실행은 이 순서다.

#### 2 — 퇴근 알림은 입사 알림과 같은 함수 `[정적]`

- `PspCallThreadNotifyRoutines` 는 만들 때 `PspInsertThread` 가 `(Thread, 1, 0)` 으로 부르던 그 함수다 ([run12 해설 2.6](../02_sprint/run12-insert-thread-해설.md#26-가게-알림은-첫-직원-때만-직원-알림은-매번)).
- 끝날 때는 `PspExitThread +0x142` (ret `+0x147`) 가 `(Thread, 0, 0)` 으로 부른다 (flow.md 2.1 `PspCallThreadNotifyRoutines` 절에 한 줄).
  - 가게가 `Flags3.Minimal` 이고 `PicoContext == 0` 이면 건너뛴다.
- 둘째 칸 1 / 0 이 "입사 / 퇴근" 을 가르는 것으로 보인다 `[추측: 뜻]`.
- 비유: 입사 때 "새 직원 왔어요" 를 돌리던 알림판에, 이번엔 "직원 퇴근" 을 돌린다.

#### 5 — 뮤턴트 놓기는 이름만

- `KiRundownMutants` (`+0x35b`) 는 결과서가 "뮤턴트 놓기 (`Thread+0x308` 목록, sast/73 질문 4)" 로 적었다.
- `+0x308` 은 [run77 해설](run77-thread-wake-해설.md) 2.3 의 **창고 열쇠 꾸러미** (`MutantListHead`) 다. 본문은 이름만 봤다.

#### 8 — 쪽지 다 떼기

- `KeRundownApcQueues` (`+0x4e3`) = APC 큐 비우기 (커널·유저 APC 정리) — 결과서.
- 앞에서 (`+0x122`) 이미 "끝날 때까지 커널 쪽지 안 받음" 으로 돌려 두었다.

### 2.10 `KeTerminateThread` ① — "퇴근함" 불 켜고, 기다리던 동료 모두 깨우기 (질문 3(a)(b))

인자 `KeTerminateThread(Thread = RCX → RBX)`, 821바이트.

#### 걸음 `[정적]`

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x157` · `+0x163` (ret `+0x168`) | `RBP` = 계산대 장부 · 스레드 헤더 락 `KiAcquireKobjectLockSafe(Thread)` | 명판 자물쇠 |
| **`+0x16c`** | **`SignalState = 1`** (이미 1 이어도) | **"퇴근함" 불 초록** |
| `+0x173 ~ +0x179` | 번호표 줄 머리 (`Thread+8`) 부터 돌기. 비면 `+0x1a2` 로 | |
| `+0x181` · `+0x184 / +0x187` | 번호표 종류 (`WaitType`) 가 1 인가 | |
| `+0x18d` · `+0x192` | `R8D = WaitKey` · `R9D = 0` | 번호표에 적힌 번호 |
| **`+0x19b` (ret `+0x1a0`)** | **`KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)`** → `+0x1a0 JMP +0x176` (다음 표). 반환값은 안 본다 | 어깨 두드리기, 한 명씩 **전부** |
| **`+0x1a2 / +0x1a6`** | 번호표 줄 **머리째** 비움 (표를 하나씩 안 뗌) | 줄을 통째로 걷기 |
| `+0x1a9` | 헤더 락 풀기 | |
| `+0x1b7 / +0x1bf` → **`+0x1ce` (ret `+0x1d3`)** | `PRCB+0x2d08 ≠ 0` 이면 `KiProcessThreadWaitList(Prcb, 1, 0, 0)` | 깨운 직원 명단 → 준비 줄 |

- 다른 번호표 종류 `[정적]`:
  - `0` (전부 기다림) → `+0x2fe R8D = 0x100` 으로 같은 `KiTryUnwaitThread` (깨난 쪽이 처음부터 다시 확인 — run77 해설의 `0x100` 도장).
  - `2` (큐) → `+0x2c4 KiInsertQueueInternal`.
  - `4` (DPC) → `+0x310` 불을 **0 으로** + `+0x31b KeInsertQueueDpc`.
  - test.exe 메인의 표는 `1` 이다 (한 물건 기다림, run77).

#### 수동 신호등과 같은 결과 — 그런데 모양이 셋 다르다

- **같은 점** `[정적]`: 번호표를 **전부** 깨우고, 불은 **1 로 남는다**.
  - run77 해설 2.8 의 **수동 재설정 루프** ("머리째 비운다", "`SignalState` 를 줄이는 줄이 없다") 와 같은 결과다.
  - 그래서 메인이 깬 뒤 다시 `WaitForSingleObject(t)` 를 해도 바로 지나간다. PE_emulator 192 메모 "끝난 스레드 객체는 신호이고 그대로 남음" 이 **맞다** (run 문서 표 3).
- **다른 점 ① — 옛 값을 안 본다**: `KeSetEvent` 는 "이미 초록이면 아무도 안 깨움" (`+0x94 / +0x96`, run77). 여기는 그 검사 없이 덮어쓰고 줄을 돈다.
- **다른 점 ② — 퇴장 담당 (`KiExitDispatcher`) 을 안 부른다**: `KeSetEvent` 는 끝에 늘 `+0x10c KiExitDispatcher` (run77). 여기는 `+0x1ce` 에서 명단을 **스스로** 비우고 곧 교대로 간다.
  - 까닭은 "IRQL 2 에서 곧 `KiSwapThread` 로 넘어가니까" 로 보인다 `[추측: 까닭]`.
  - 이어지는 `KiSwapThread` 도 `+0x14` 에서 같은 명단을 한 번 더 본다 (run77).
- **다른 점 ③ — 몸에 풀어 넣지 않는다**: `KeSetEvent` 는 `WaitType 1` 표의 깨움을 자기 몸에 풀어 넣었다 (run77 해설 2.8 "몸에 풀어 넣기"). 여기는 `KiTryUnwaitThread` 를 **진짜로 부른다**. 그래서 `_calls.tsv` 에 `+0x19b` 로 나온다.

#### test.exe 메인은 `WaitStatus = 0` 으로 깬다 `[정적: 길]`

1. 메인 `WaitForSingleObject(t)` 는 `KeWaitForSingleObject` 로 **0번 표** 를 `WaitKey = 0` 으로 건다 (run77 `+0x1e2`).
2. Worker 퇴근 때 `+0x18d` 가 그 0 을 `R8D` 로 담고 → `+0x19b` 가 넘긴다.
3. `KiTryUnwaitThread` 가 메인 결과 칸에 0 을 적는다 (`+0x168`, run77).
4. 메인은 다시 안 묻고 0 (`STATUS_WAIT_0`) 을 그대로 돌려준다 (run77 해설 2.7).

- 실행으로 본 건 아니다. `[정적: 길]` 이고 Q104 동적 5 에서 본다.
- flow.md 4장 run77 동적 6 ("누가 `WaitStatus` 를 쓰나 (스레드 객체)") 에 이 답이 `[정적]` 으로 붙었다.

#### 비유

- 직원이 퇴근 맨 끝에 자기 명판의 **"퇴근함" 불** 을 켠다.
- 그 앞에 줄 선 번호표를 **한 장씩 전부** 집어, 주인 어깨를 두드린다 ("0번 물건 받음").
- 다 두드린 뒤 줄을 **통째로** 걷는다. 불은 켠 채 둔다. 나중에 온 사람도 그냥 지나간다.
- 깨운 사람 명단은 퇴장 담당에게 안 넘긴다. 자기가 직접 준비 줄로 보낸다. 곧 계산대를 내놓을 거라서로 보인다 `[추측: 까닭]`.

#### PE_emulator 에게

- "끝난 스레드 객체는 신호, 그대로 남음" — **맞음**.
- "잠든 쪽 깨우기" (194 메모) 는 **이 자리** 여야 한다. `WaitStatus = WaitKey` (보통 0), 모두 깨움, 깨난 쪽은 다시 안 묻는다 (규칙 6).
- 그리고 이 자리는 작업대·안내판 철거 **뒤** 다 (2.18).

### 2.11 `KeTerminateThread` ② — `State 4`, 청소부 수거함, 계산대 내놓기 (질문 3(c))

#### 걸음 `[정적]` (2.10 앞뒤까지 실행 순서로)

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x46` | `WaitReason = 0x16` (`WrTerminated` `[추측: 이름]`) | 사유 칸에 "퇴근" |
| `+0x4d ~ +0x57` | IRQL 2 로 | "방해 금지" 표지판 올리기 |
| `+0x6c ~ +0x13c` | 운영 카드 근무표에서 빼기 (2.12) | |
| `+0x157 ~ +0x1a9` | 불 켜고 모두 깨우기 (2.10) | |
| **`+0x1b0`** | **`State = 4`** (Terminated `[추측: 이름]`) | 상태 "퇴근 끝" |
| `+0x1ce` | 깨운 직원 → 준비 줄 (2.10) | |
| **`+0x1d3 ~ +0x1ee`** | `ReaperLink` (`Thread+0x4e8`) 를 `PsReaperListHead` 에 잠금 없이 push (`+0x1e8` · `+0x1ee CMPXCHG.LOCK`) | 청소부 **수거함** 에 이름표 넣기 |
| `+0x1fc / +0x1ff` → **`+0x213` (ret `+0x218`)** | 수거함이 **비어 있었으면** `ExQueueWorkItemEx(&PsReaperWorkItem, 2, -1)` (Ghidra 이름 `Ordinal_36`). 실패면 `+0x21c BTS [PsReaperWorkItem+0x18]` (표시만) | 청소부 부르기 |
| `+0x22a ~ +0x23e` | 스레드 락 (`Thread+0x40`) 이 풀릴 때까지 기다렸다 0 | |
| **`+0x24f` (ret `+0x254`)** | **`KiSwapThread(Thread, Prcb, 0)`** | 계산대 내놓기 |

- 순서 한 줄로 `[정적]`: **목록 빼기 → 불 → 깨우기 → `State 4` → 청소부 → 넘기기.**
- `+0x213` 의 callee 이름: `_calls.tsv` 엔 `alias:Ordinal_36` 로 찍힌다. PDB 이름은 `ExQueueWorkItemEx` **하나** 다 (`pdb_aliases.py --where`). 둘째 인자 `2` 의 큐 종류 이름은 `[추측]`.

#### 왜 "안 돌아온다" 인가 — 단계로

1. 근거 `[정적]`: 이 함수엔 준비 줄에 넣는 호출 (`KiQueueReadyThread` 류) 이 없다 (`_calls.tsv` 20줄).
2. 근거 `[정적]`: `KiSwapThread` 는 지금 직원을 줄에 넣지 않는다 (run75).
3. 근거 `[정적]`: `State = 4` 다. 준비 줄에도, "다음 차례" 자리에도 없다.
4. 그래서: 다시 뽑힐 길이 없다. `KiSwapThread` 가 계산대를 다른 직원에게 넘기면, 이 직원의 `call` 은 영영 안 돌아온다.
   - run75 해설 2.8 에서 교대의 `call` 은 "한참 뒤에" 돌아온다고 했다. 여기선 그 "한참 뒤" 가 **오지 않는다**.
5. `PspExitThread +0x551` 이후는 맺음 코드뿐이다 `[정적]`.

#### 새 말 — 청소부 `PspReaper` 와 수거함 `PsReaperListHead`

- `PsReaperListHead` = 퇴근 끝난 직원을 쌓아 두는 전역 목록, `PsReaperWorkItem` = 그 목록을 비우라고 맡기는 일감. 둘 다 PDB 에 이름이 있다.
- `PspReaper` = 그 일감이 돌 때 불리는 루틴이다 `[정적]`.
  - 부팅 때 `PspInitPhase0 +0x9ae LEA [PspReaper]` / `+0x9b5 MOV [PsReaperWorkItem+0x10]` 로 등록된다 (`refs` 로만 보임).
- 작업 항목 (work item) 은 커널이 시스템 일꾼 스레드에게 나중에 시키는 일감으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유:
  - 퇴근 끝난 직원은 **청소부 수거함** 에 자기 이름표를 넣고 간다.
  - 수거함이 비어 있었으면 관리사무소 일꾼에게 "수거 와 주세요" 를 부탁한다.
  - 이미 이름표가 있었으면 따로 안 부탁한다. 앞 사람이 부탁해 둔 수거가 같이 가져갈 것으로 보인다 `[추측: 까닭]`.
- 청소부가 실제로 하는 일은 2.16.
- 왜 직원이 스스로 다 못 치우나?
  - 일반적으로, 끝나는 스레드는 아직 자기 커널 스택 위에서 돌고 있어서 그 스택을 스스로 풀 수 없다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 실제로 커널 스택 해제 줄은 청소부 쪽 (`PspReaper +0x55`) 에 있다 `[정적]`.

#### run53 정정 — 수거함에 넣는 쪽은 `KeTerminateThread`

- run53 의 "범위 밖 길에서 씀" 표 `ReaperLink` 줄: "reaper 줄에 매다는 쪽 (PspExitThread) 은 훑기에 안 잡힘".
- → 매다는 쪽은 **`KeTerminateThread`** 다 (`+0x1d3 LEA R8,[RBX+0x4e8]` → `+0x1e8` → `+0x1ee CMPXCHG.LOCK [PsReaperListHead]`) `[정적]`.
- `PspExitThread` 는 같은 칸 (`+0x4e8`) 을 `+0x225` 에서 **읽기만** 한다. 같은 칸의 다른 이름 `TerminationPort` 목록으로 읽는다 (`.asm` 에서 `0x4e8]` 1건).
- run53 문서 맨 위에 정정 한 줄이 들어갔다. run53 해설엔 이 줄 이야기가 없어서 고칠 곳이 없다.

#### PE_emulator 에게

- "다시 안 돈다" 로 만들었다면 **맞음** (run 문서 표 3(c)).
- 커널 스택 해제·참조 내림은 나중 일 (청소부) 이라 건너뛰어도 된다 (규칙 7).

### 2.12 직원 목록이 둘 — 운영 카드 근무표와 가게 장부 명부 (질문 2(d) · 3(d))

| 목록 | 고리 | 넣는 곳 | 빼는 곳 |
|---|---|---|---|
| 운영 카드 근무표 `KPROCESS.ThreadListHead (+0x30)` | 업무 카드 `KTHREAD.ThreadListEntry (+0x2f8)` | `KeStartThread +0x2f4 ~ +0x2fe` (run49) | **`KeTerminateThread +0x116 / +0x119`** (퇴근 때) |
| 가게 장부 명부 `EPROCESS.ThreadListHead (+0x370)` | 인사 카드 `ETHREAD.ThreadListEntry (+0x578)` | `PspInsertThread +0x6ac ~ +0x6f8` → `+0x239 ~ +0x243` (flow-writer) | **`PspThreadDelete +0x374 / +0x377`** (인사 카드 지울 때) |

- 모두 `[정적]`.
- 근무표 쪽 빼기 앞뒤: `+0x6c` 가게 스핀락 (`KPROCESS.ProcessLock +0x40`) · `+0xe2` 가게 헤더 락 · 안전 검사 `+0xee ~ +0xf7` · **`+0x116 / +0x119`** · `+0x11d` 헤더 락 풀기.
- **`PspExitThread` 는 어느 목록에서도 안 뺀다** `[정적]`.

#### 그래서 무엇이 달라지나

- 퇴근한 직원도 **가게 장부 명부엔 남아 있다**. 인사 카드가 지워질 때까지.
- 그 덕에 마지막 직원이 명부를 돌며 "아직 불이 안 켜진 동료" 를 기다릴 수 있다 (2.13 단계 0).
- 비유: 운영 카드의 **"오늘 근무표"** 에선 퇴근하자마자 지운다. 가게 장부의 **"직원 명부"** 에선 인사 카드를 파쇄할 때 지운다.

#### 마지막이면 가게도 "운영 중 가게" 목록에서 뺀다 `[정적]`

- `KeTerminateThread +0x7f / +0x82` 에서 근무표에 자기뿐이면 (`Flink == Blink`):
  - `KiProcessListLock` (`+0xa5`) → `+0xb5` `KPROCESS.ProcessListEntry (+0x110)` → `+0xce / +0xd1` 빼기 → `+0xda` 큐 스핀락 놓기.
- `+0xda` callee 는 같은 주소 PDB 이름이 둘 (`KxReleaseQueuedSpinLock` · `KeReleaseInStackQueuedSpinLockFromDpcLevel`) 이라 run 문서는 "큐 스핀락 놓기" 로 적었다.
- 비유: 마지막 직원이 근무표에서 빠지면, 관리사무소의 **"운영 중인 가게"** 목록에서도 그 가게를 지운다.

#### PE_emulator 에게

- 197 메모가 `PspExitThread` 안에 "`ThreadListHead` 빼기 (+0x370)" 를 두었다면 **자리가 다르다**.
- "끝난 스레드를 목록에서 바로 지운다" 로 만들었다면 가게 장부 쪽 (`+0x370`) 은 **틀리다** (규칙 8).

### 2.13 마지막 직원 — 가게 문 닫기 `PspExitLastThread` (질문 4)

#### 새 말 — 마지막 스레드

- `PspExitThread +0x18e` 에서 근무 중 인원을 내렸더니 0 이 된 직원이다 `[정적]` (2.7). 그 순간 마지막 표지 `R12B = 1`.
- 비유: 가게의 **마지막 퇴근자**. 자기 작업대 대신 **가게 문을 닫는** 일을 맡는다.

#### 한 함수, 네 단계 `[정적]`

`PspExitLastThread` 하나가 단계 번호 (`ECX`) 를 받아 `PspExitThread` 사이사이에서 **네 번** 불린다.

| 단계 | 부르는 곳 (`PspExitThread`, call / ret) | 하는 일 |
|---|---|---|
| 0 | `+0x6a1 / +0x6a6` (장부 자물쇠 쥔 채) | `+0x122 KeForceResumeProcess` · `+0x127` 가게 `ExitStatus == 0x103` 이면 status 로 (`0xc000004b` 면 `LastThreadExitStatus`) · `+0x137` 가게 장부 명부를 돌며 자기 뺀 동료의 불 (`+0x157`) 이 0 이면 락 풀고 **`+0x248 KeWaitForSingleObject(그 스레드)`** → 다시 락 |
| 1 | `+0x58d / +0x592` | `BreakOnTermination` 이면 `PspCatchCriticalBreak` (이름만) |
| 2 | `+0x5a6 / +0x5ab` | `Win32Process (+0x298)` 있으면 Win32k **프로세스** 끝 콜아웃 (이름만) |
| 3 | `+0x5bf / +0x5c4` | `+0x61 / +0x6b` 가게 `ExitTime = 직원 ExitTime` · **`+0x74 PspExitProcess`** · 감사 · `KernelApcDisable` 되올림 · **`+0xe1 PspRundownSingleProcess(Process, 1)`** (→ `+0xb5 ObKillProcess` · **`+0xbd MmCleanProcessAddressSpace`** · `+0x1d5 KeSetProcess`) · `+0xe9 LpcExitProcess` |

- 단계 3 안 함수들은 **부르는 줄과 이름만** 봤다. 본문은 안 읽었다.
- `+0x248` 의 callee 는 Ghidra 이름 `KeWaitForMutexObject`, PDB 이름 `KeWaitForSingleObject` 다 (run77 과 같은 꼴).

#### 실행 순서로 다시 (마지막 직원)

1. `+0x18e` 인원 0 → `+0x685` "가게 닫는 중" → **단계 0** (동료 불 기다리기).
2. … **단계 1** (`+0x58d`) → … **단계 2** (`+0x5a6`) → …
3. 작업대·안내판 블록 — `+0x3a8` 에서 **건너뜀** ("가게 닫는 중" 이 이미 켜져 있어서).
4. `ExitTime` → **단계 3** (`+0x5bf`, 가게 땅째 치우기).
5. `KeRundownApcQueues` → `KeTerminateThread` — 마지막 직원도 퇴근 끝맺음은 같다.

#### 핵심 — 마지막 직원은 작업대·안내판을 따로 안 치운다

1. 근거 `[정적]`: `+0x685` 가 `ProcessDelete` 를 작업대·안내판 블록 (`+0x3a3 / +0x3a8`) 보다 **먼저** 켠다.
2. 근거 `[정적]`: 그래서 `+0x3f2` 도 `+0x4b8` 도 안 부른다.
3. 근거 `[정적: 이름·순서]`: 대신 단계 3 의 `PspRundownSingleProcess → MmCleanProcessAddressSpace` 가 불린다.
4. 그래서: 가게 땅 (주소 공간) 전체를 치울 때 마지막 직원의 작업대·안내판도 같이 사라질 것이다.
5. 아직 확인 아님: `MmCleanProcessAddressSpace` 본문은 안 읽었다 (Q147 ③).

- 비유: 마지막 퇴근자는 자기 작업대를 따로 철거하지 않는다. 가게 땅을 **통째로** 정리하는 쪽에 맡긴다.

#### 단계 0 — 인원이 0 인데 왜 또 기다리나

- 근무 중 인원은 퇴근 절차를 **시작할 때** 내린다 (`+0x18e`). "퇴근함" 불은 **끝날 때** 켠다 (`KeTerminateThread +0x16c`).
- 그 사이에 있는 동료가 있을 수 있다. 인원은 이미 빠졌는데 불은 아직 안 켜진 동료다.
- 그래서 마지막 직원이 명부를 돌며 "불이 아직 0 인" 동료를 하나씩 기다린다 `[정적]`.
  - 결과서: "활성 스레드 수가 0 이어도, 다른 스레드가 `KeTerminateThread` 를 끝낼 때까지 기다린다."
- 이게 되는 건 2.12 의 "명부엔 퇴근 뒤에도 남는다" 덕이다.
- 비유: 마지막 퇴근자가 문을 닫기 전에, 명부를 보며 아직 "퇴근함" 불이 안 켜진 동료가 있으면 그 불이 켜질 때까지 문 앞에서 기다린다.

#### PE_emulator 에게

- "마지막이면 `process_exit` 이벤트 + `dwExitStatus` + `bLastThreadExited`" — 큰 줄기 **맞음**.
- 단계 0 기다림은 CPU 하나·순서 고정이면 없어도 될 수 있다 `[추측: 에뮬레이터 범위]`.
- 가게 `ExitStatus` 규칙 (`0xc000004b` 면 `LastThreadExitStatus`) 은 필요하다 (결과서).

#### test.exe 의 마지막은?

- 메인 스레드는 run7 `[관찰]` 에서 `ExitProcessImplementation → RtlExitUserProcess` 로 끝났다.
  - 그 안에서 `NtTerminateProcess` 를 어떻게 부르는지 (그래서 `PspTerminateAllThreads` → `+0x161 PspExitThread` 길인지) 는 안 봤다 → Q147 ④.
  - 메인이 끝날 때 블록 조건 (`+0x3a8`) 을 정하는 `ProcessSelfDelete` 를 누가 켜는지도 → Q147 ③.
- 동적: 메인이 끝날 때 1 → 0 이고 `+0x6a1` 로 가나 (Q104 동적 2), 메인은 `+0x3f2`·`+0x4b8` 을 안 부르나 (동적 3).

### 2.14 남을 퇴근시키기 — 쪽지만 붙이고 돌아온다 (질문 5)

#### (a) `PspTerminateThreadByPointer` 의 남 갈래 (`+0x50 ~ +0x92`) `[정적]`

1. `+0x50` 시스템 스레드면 `0xc0000022`. `+0x59 ~ +0x61` 가게가 `Minimal` 이면 `0xc00000bb`.
2. **`+0x63 BTS.LOCK [RBX+0x5a0],0`** — 퇴근 예정 도장. **처음 찍은 쪽만** `+0x6e ExitStatus = status` (이미 찍혀 있으면 사유를 안 덮는다).
3. **`+0x77` (ret `+0x7c`) `KeRequestTerminationThread`** → `+0x7c XOR EAX,EAX` — **곧 SUCCESS.** 끝나길 안 기다린다.

- 요청서가 짐작한 `PsExitSpecialApc` 같은 이름은 **없다**.

#### 쪽지를 붙이는 `KeRequestTerminationThread` `[정적]`

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x2e` | `ThreadFlags.TerminationApcRequest` (비트 15) 켜기. 이미 켜져 있으면 끝 | "퇴근 요청 있음" 표시 |
| `+0x4c` | 스레드 락 | |
| `+0x78` | `ApcQueueable` 이면 업무 카드에 박힌 쪽지 (`SchedulerApc +0x288`) 를 `KiInsertQueueApc` | 책상에 쪽지 붙이기 |
| `+0x89` · `+0xa7` | `KiSignalThreadForApc` · `KiExitDispatcher` | "쪽지 왔어요" 알리기 |
| `+0xb6` · `+0xbe` | `KeAlertThread` · `KeForceResumeThread` — 멈춰 있던 직원도 풀어 쪽지가 돌게 (결과서) | 대기 팻말 건 직원도 쪽지를 보게 |

- 그 쪽지 (`SchedulerApc`) 는 직원을 뽑을 때 미리 만들어 둔 것이다. `KeInitThread +0x11c ~ +0x152`, `NormalRoutine = KiSchedulerApc` (결과서).

#### 쪽지를 받은 직원 안에서 `[정적]` + 한 걸음 `[추측]`

1. `KiSchedulerApc +0x41` — `ThreadFlags & 0x8000` ("퇴근 요청 있음") 을 본다.
2. `+0x1d4 / +0x1db` — 쪽지의 `KernelRoutine` 을 `KiSchedulerApcTerminate` 로 **바꿔 쓴다**.
3. `+0x1f0` — `KiInsertQueueApc` 로 다시 넣는다.
4. `KiDeliverApc +0x443 / +0x459` — 루틴이 그 주소면 → **`+0x46b KiSchedulerApcTerminate`**.
5. `KiSchedulerApcTerminate +0x29 ECX = Thread.ExitStatus` → **`+0x2f PspExitThread`** → `+0x34 INT3`.

- 1 ~ 4 의 "바꿔 써서 다시 넣는" 순서는 세 함수의 줄을 맞춘 것이다. 한 번에 따라가 본 건 아니다 `[추측: 중간 한 걸음]` (Q147 ①, Q104 동적 8).
- `KiSchedulerApcTerminate` 는 먼저 `+0x17 KiIsProcessTerminationRequested` 를 보고, 참이면 `+0x49 PsTerminateProcess` 로 간다 (결과서).

#### 그래서

- 남을 퇴근시켜도, 실제 퇴근 절차 (`PspExitThread` — 작업대·안내판 철거 포함) 는 **그 직원 자신의 문맥에서**, 그 직원이 **다음에 커널 쪽지를 받을 때** 일어난다 `[정적]`.
- 비유:
  - 관리사무소는 그 직원 책상에 **"퇴근하세요" 쪽지** 를 붙이고 바로 돌아온다.
  - 대기 팻말이 걸려 있으면 팻말도 떼서, 쪽지를 볼 수 있게 해 둔다.
  - 그 직원은 쪽지를 펴는 순간 **스스로** 퇴근 절차를 밟는다.
- run13 해설 2.3 의 "멈춤 = 쪽지 + 신호등" 에서 쓰던 쪽지 루틴과 같은 `KiSchedulerApc` 다 (멈춘 스레드는 `KiSchedulerApc +0xf2` 에서 잠든다 — run77 해설 2.9). 이번엔 그 안의 "퇴근" 갈래 (`+0x41` `0x8000`) 를 탄다.

#### (b) 모두 끝내기 `PspTerminateAllThreads` `[정적]`

- 명부를 **뒤에서 앞으로** 돈다 (`PspGetPreviousProcessThread`).
- `+0x9e` 에서 부른 쪽은 빼고, 나머지에 **`+0xd4 PspTerminateThreadByPointer(…, 0)`** — 위 (a) 의 쪽지 길.
- 플래그 `& 2` 면 부른 쪽도 (`+0x221`).
- 부르는 쪽: `NtTerminateProcess +0x243` · `PspTerminateProcess +0xf2`.
  - `NtTerminateProcess` 는 자기 가게면 이어서 `+0x147 Terminated` → `+0x161 PspExitThread` 로 **자기도** 퇴근한다.
- 이 함수가 돌아와도 동료들은 아직 안 끝났을 수 있다 (쪽지만 붙였으니). 마지막으로 끝나는 쪽이 단계 0 에서 기다린다 (2.13).

#### PE_emulator 에게

- "남 먼저, 자기 마지막" (197 메모) — 순서 **맞음**.
- 다만 남을 그 자리에서 곧바로 끝낸다면 **다르다**. 결과 (다시 안 돈다) 는 같아도, 끝나는 때가 "그 직원이 다음에 돌 때" 다 `[추측: 어디까지 흉내 낼지]`.
- test.exe 엔 남 끝내기가 없다 (run 문서 동적 8). `NtTerminateProcess` 길에서 볼 수 있다.

### 2.15 기다리는 쪽은 스레드 객체를 어떻게 받나 — `DefaultObject` (질문 6)

#### 새 말 — 객체 형식의 `DefaultObject`

- 커널 물건마다 **형식** (`_OBJECT_TYPE`) 이 있다. 스레드면 `PsThreadType`.
- 형식 `+0x20` 칸이 `DefaultObject` 다 (PDB). `ObWaitForSingleObject` 는 이 칸으로 "이 물건의 **어디가** 신호등 명판인가" 를 고른다 `[정적]`.
- 비유: 물건 종류마다 붙은 **종류 설명서** 의 "명판 위치" 칸.

#### 길 `[정적]`

1. `+0x47` 손잡이 → 물건 (`ObpReferenceObjectByHandleWithTag`, `SYNCHRONIZE`).
2. 물건 머리의 `TypeIndex` → `ObTypeIndexTable` → 형식.
3. **`+0x82 RCX = DefaultObject`**.
4. `+0x86 / +0x89` — 비트 0 이 서 있으면 특수 꼴 (`+0xf8`).
5. 아니면 `+0x8b / +0x8e` — 음수면 커널 절대 주소 그대로, **0 이상이면 `+0x90` 본체 + 그 값**.
6. `+0xb5 EDX = 6` (UserRequest) → `+0xba KeWaitForSingleObject` (run77).

#### 스레드 형식은 0 — 읽은 줄 셋으로 유추 `[정적: 유추]`

1. `PspInitPhase0 +0x504 ~ +0x569` 의 스레드 형식 만들기 → `ObCreateObjectType +0x9 XOR R9D,R9D` — `DefaultObject` 인자 0.
2. `ObCreateObjectTypeEx` 는 `UseDefaultObject` 비트 (`0x4`) 일 때만 `&ObpDefaultObject` 를 쓴다.
3. 스레드 형식 값엔 이 비트가 안 켜진다 (`+0x50b OR [RSP+0x62],R12B` 의 `R12B = 0x80`).

- 그래서: `DefaultObject == 0` → 본체 + 0 = `_ETHREAD` = `_KTHREAD.Header` (둘 다 `+0x0`).
- 아직 확인 아님: 3 의 `R12B` 가 `+0x26f ~ +0x50b` 사이에 정말 안 바뀌는지 (Q147 ②), 실제 값 (Q104 동적 7).
- 의미: 이벤트·뮤턴트·스레드가 **같은** `KeWaitForSingleObject` 하나로 처리된다. 스레드는 인사 카드 맨 앞이 바로 신호등 명판이라서다.
- 비유: 직원 카드 종류 설명서엔 "명판 위치: 카드 맨 앞 (0)" 이라고 적혀 있다. 기다리는 쪽은 신호등을 기다리듯 직원 카드 맨 앞 명판을 기다린다.
- PE_emulator: "기다림 도우미가 스레드 객체를 받는다 (끝났으면 신호)" (192 메모) — **맞음**.

### 2.16 나중 일 — 청소부와 인사 카드 파쇄 (질문 7)

#### 청소부 `PspReaper` 가 하는 일 `[정적]`

`KeTerminateThread +0x213` 이 맡긴 일감이 돌면:

1. `+0x16 XCHG [PsReaperListHead],1` — 수거함을 **통째로** 받아 온다.
2. 직원마다 `+0x27 KeDeleteThread`.
3. `+0x3b XADD [Thread+0x5ec],-1` (`KernelStackReference`) — 1 이었으면 `+0x55 KeEnumerateKernelStackSegments` = **커널 스택 해제**, `+0x5a InitialStack = 0`.
4. **`+0x69 ObfDereferenceObjectWithTag(Thread, 'Dflt')`** — 만들 때 둔 참조 하나를 내린다.

- 비유: 청소부가 수거함을 통째로 비운다. 직원마다 관리사무소 안 작업대 (커널 스택, run52 해설) 를 치우고, 인사 카드를 쥐고 있던 **손 하나** 를 뗀다.

#### 새 말 — 참조

- 커널 물건을 "지금 손에 쥐고 있는 쪽의 수" 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 손잡이 (핸들) 도 쥐고 있는 손 하나다.
- [run23 해설](../02_sprint/run23-load-dependent-해설.md) 의 "명부 줄 참조 수" 와 같은 생각이다.
- 0 이 되면 물건을 지운다. 그때 형식에 등록된 **객체 지우기 루틴** 이 불린다 `[추측: 객체 관리자 길]`.

#### 새 말 — 객체 지우기 루틴 `PspThreadDelete`

- 스레드 형식의 삭제 루틴 (`DeleteProcedure`) 으로 등록돼 있다 `[정적]` (`refs` 로만 보임):
  - `PspInitPhase0 +0x510 LEA [PspThreadDelete]` / `+0x517`.
  - 같은 자리 `+0x546` 크기 `0x798` = `sizeof(_ETHREAD)`.
- 불리는 때 = 참조 수가 0 이 될 때 `[추측: 객체 관리자 길]`.
- 비유: 인사 카드 **파쇄 담당**. 아무도 그 카드를 손에 쥐고 있지 않을 때 온다.

#### 파쇄 담당이 하는 일 `[정적]`

- `+0x17 KeCleanupThreadState` · 새는지 검사 (bugcheck `0x94`·`0x13c`·`0x1c6`).
- 커널 스택이 아직 있으면 `+0x45f KeEnumerateKernelStackSegments`. 청소부를 못 거친 직원 몫으로 보인다 `[추측]`.
- **사번 (TID) 반납**: `+0x1cd ExMapHandleToPointer(PspCidTable)` · `+0x1e8 ExDestroyHandle`.
- 이름·가장 토큰 정리.
- **가게 장부 명부에서 빼기** `+0x374 / +0x377` (2.12).
- **가게 참조 내림** `+0x3d5`.
- **`MmDeleteTeb`·`ZwFreeVirtualMemory`·`NtFreeVirtualMemory` 호출이 없다** → 작업대·안내판은 퇴근 절차 (2.8) 몫이고 **겹치지 않는다**.

#### PE_emulator 에게

- TEB·스택을 "누가 푸나" — **`PspExitThread` 쪽이 맞다**.
- 파쇄는 손잡이·참조 계산이 있어야 하는 길이라 지금은 건너뛰어도 된다 `[추측: 에뮬레이터 범위]`.

### 2.17 결과서 정정 — "test.exe 는 핸들을 안 닫는다" 는 틀렸다

이번 바퀴에서 꼭 짚을 곳이다. 천천히 보자.

#### 결과서가 적은 것

- 질문 7 끝: "test.exe 는 `CreateThread` 로 받은 핸들 `t` 를 **닫지 않으므로** (`sample/test.c:34-35`, 프로세스 끝까지) 핸들 참조가 남는 동안 Worker 의 `ETHREAD` 는 지워지지 않는다 `[소스+추측]` — 곧 `PspThreadDelete` 는 프로세스 끝 (핸들 정리) 쪽에서 불리는 모양이다."
- 동적 항목도 "`PspThreadDelete` 가 Worker 에 대해 **프로세스 끝 가까이에만** 불리는가 (test.exe 가 `t` 를 안 닫음)" 로 적었다.

#### 실제 소스 `[소스]`

```
34      t = CreateThread(NULL, 0, Worker, NULL, 0, NULL);
35      WaitForSingleObject(t, INFINITE);
36      CloseHandle(t);
```

- **36 줄에 `CloseHandle(t)` 가 있다.**
- PE_emulator 원문도 같다: seq 386 `NtClose(0x70)` (201 메모 "가." 표 3줄).

#### 왜 생겼나

- 오케스트레이터가 요청서 "배경" 에 소스를 `test.c:34-35` **두 줄만** 옮겨 적었다.
- 결과서는 요청서만 보고 일한다. 그래서 36 줄을 못 봤다.
- plan.md 결정 기록에 "오케스트레이터 실수" 로 남았다.
  - 하네스 제안 ⑧ (보류): 요청서·결과서에서 sample 소스는 인용 줄만 말고 **그 함수를 통째로** 열어 보기.
  - run 문서 "도구 메모" 에도 같은 말이 있다.

#### 고친 결론 — 단계로

1. 근거 `[소스]`: 메인은 깨어나서 곧 `CloseHandle(t)` 를 한다 (36 줄).
2. 근거 `[정적]`: 청소부는 `+0x69` 에서 만들 때 둔 참조 하나를 내린다 (2.16).
3. 그래서: Worker 인사 카드를 쥔 손이 이 둘뿐이라면, **`NtClose(t)` 와 `PspReaper +0x69` 중 늦은 쪽** 에서 참조가 0 이 되고 `PspThreadDelete` 가 불릴 것이다 `[추측: 참조 수]`. 그 밖 참조는 세지 않았다.
4. 아직 확인 아님: Q104 동적 6 — `bp nt!PspThreadDelete` (첫 인자 = Worker `ETHREAD` 일 때) 에서 `k` 로 `NtClose` 안인지 `PspReaper +0x69` 안인지 본다.

- 결론: "프로세스 끝 가까이" 가 **아니다**.
- 에뮬레이터는 규칙 7 대로 파쇄를 건너뛰어도 결과가 같다 (run 문서).
- 비유: 메인이 Worker 인사 카드 **손잡이를 내려놓는 줄** 이 소스에 분명히 있었다. 요청서가 그 줄을 안 옮겨 와서, 결과서는 "손잡이를 끝까지 쥐고 있다" 고 읽었던 것이다.

### 2.18 test.exe 에 대 보면 — Worker 퇴근 한 장면

| # | 장면 | 줄 | 태그 |
|---|---|---|---|
| 1 | `Worker` 가 `return 0` | `test.c:24` | `[소스]` |
| 2 | 출근 인사 담당이 퇴근 담당을 부름 | `BaseThreadInitThunk +0x20` → `RtlExitUserThread` | `[정적]` run25 · `[관찰]` 01_sprint 콜스택 |
| 3 | "내가 마지막?" → 아님 (메인이 기다리는 중) | `RtlExitUserThread +0x29` | 길 `[정적]` · 값 `[추측: 순서]` |
| 4 | 퇴근 인사 (이유 3) → `TpCheckTerminateWorker(0)` | `+0x41` · `+0x48` | `[관찰]` 01_sprint (이유 3) · `[정적]` |
| 5 | 손잡이 0 으로 퇴근 신청 (원문 `NtTerminateThread(0, 0)`) | `+0x51` → `NtTerminateThread` | `[정적]` |
| 6 | `ActiveThreads ≥ 2` → 자기 길 | `+0x34 / +0x37` 안 탐 → `+0x41` | `[추측: 순서]` |
| 7 | 퇴근 예정 도장 → 퇴근 절차 | `PspTerminateThreadByPointer +0x40` → `+0x4a` | `[정적]` |
| 8 | 인원 −1 (마지막 아님) · 사유 기록 | `PspExitThread +0x18e` · `+0x1cc` | `[정적]` |
| 9 | 작업대 철거 → 안내판 반납 | `+0x3f2` · `+0x4b8` | 길 `[정적]` · 실제는 Q104 |
| 10 | "퇴근함" 불 → 메인 어깨 두드리기 (`WaitKey 0`) | `KeTerminateThread +0x16c` · `+0x19b` | `[정적]` |
| 11 | `State 4` → 수거함 → 교대, 안 돌아옴 | `+0x1b0` · `+0x1d3 ~ +0x1ee` · `+0x24f` | `[정적]` |
| 12 | 메인이 깨서 `WaitStatus 0` 을 들고 돌아감 | run77 | `[정적: 길]` |
| 13 | 메인이 손잡이를 내려놓음 | `test.c:36` `CloseHandle(t)` | `[소스]` |
| 14 | 참조 0 → 파쇄 | `PspThreadDelete` | `[추측: 참조 수]` |

- **메인이 깰 때 Worker 의 작업대·안내판 주소는 이미 없다** `[정적: 순서]` — 9 가 10 보다 앞이다.
  - 에뮬레이터가 "불 → 정리" 순서로 두면, 깬 메인이 Worker 메모리를 아직 볼 수 있게 되어 진짜와 달라진다 (run 문서 덧붙임 ③).
- 3 · 6 · 9 · 10 · 12 · 14 는 실행으로 본 게 아니다 → Q104.

### 2.19 PE_emulator 에게 — 한 장으로

run 문서 "PE_emulator 쪽에서 쓸 것" 과 표를 한 장으로 줄였다.

| 지금 우리 | 진짜 | 판정 |
|---|---|---|
| `PspExitThread` 이름으로 끝내기, 마지막 갈래 있음 | 같음 | 줄기 **맞음** |
| `Terminated` 켜고 지금 스레드면 이어서 `PspExitThread` | `+0x40` → `+0x4a` | **맞음** |
| (없음) | `ActiveThreads −1` (`+0x18e`), 0 이면 마지막 | **더해야 함** |
| TEB·스택 풀기 공사 중 | 작업대 (`+0x3f2`) → 손잡이 닫기 (`+0x40f`) → 안내판 (`+0x4b8`). 모두 `KeTerminateThread` 앞. 마지막은 건너뜀 | **더해야 함** |
| `ThreadListHead` 빼기 (`+0x370`) 를 `PspExitThread` 안에? | 근무표 쪽은 `KeTerminateThread +0x116`, 명부 쪽은 파쇄 때 | 자리 **다름** |
| 끝난 스레드 객체는 신호, 그대로 남음 | `+0x16c` 1, 남음 | **맞음** |
| 잠든 쪽 깨우기 | `KeTerminateThread` 안 `+0x19b`, 블록 전부, `WaitStatus = WaitKey` | 이 **자리** 여야 함 |
| 남도 곧바로 끝냄? | 쪽지만 걸고 SUCCESS. 실제 끝은 그 직원이 다음에 돌 때 | **다름** (결과는 같음) |
| 마지막이면 `process_exit` + 상태 + `bLastThreadExited` | 단계 0 ~ 3 | 큰 줄기 **맞음** |
| 원문 `NtTerminateThread(0, 0)` | 손잡이 0 = 자기 + "`ActiveThreads == 1` 이면 `0xc00000db`" | 넣는다면 한 줄 |
| 기다림 도우미가 스레드 객체를 받음 | `DefaultObject == 0` → 카드 맨 앞 명판 | **맞음** |

- 최소 규칙 여덟은 run 문서 그대로 읽으면 된다: 1 입구 갈래 · 2 남 · 3 `PspExitThread` 순서 · 4 작업대·안내판 · 5 마지막 · 6 깨우기 · 7 다시 안 돎 (파쇄는 건너뛰어도 됨) · 8 목록 둘.
- flow-writer 덧붙임 셋: ① 유저 쪽 순서 (2.4) ② `CloseHandle(t)` (2.17) ③ 메인이 깰 때 Worker 메모리는 이미 없음 (2.18).

### 2.20 결과서와 다르게 적은 곳 · 보탠 것 · 정정 (짧게)

- ① 결과서 질문 7 "핸들 안 닫음" → `test.c:36 CloseHandle(t)` (2.17).
- ② 보탬: ntdll `RtlExitUserThread` 본문. 결과서는 "ntdll 안 읽음" (2.4).
- ③ 보탬: 가게 장부 명부에 **넣는** 줄 전체 — `PspInsertThread +0x6ac ~ +0x6f8 → +0x239 ~ +0x243`. 결과서는 `+0x6e3 ~ +0x6ea` 만 적었다 (2.12, 3절 짐 지도).
- ④ 결과서 3(a) "`+0x160 / ret +0x168 KiAcquireKobjectLockSafe(Thread)`" → 부르는 줄은 **`+0x163`** 이다. `+0x160` 은 `MOV RCX,RBX`.
- ⑤ 결과서 3(d) "`+0xda … KxReleaseQueuedSpinLock`" → `_calls.tsv` 는 `alias:KeReleaseInStackQueuedSpinLockFromDpcLevel` 로 찍는다. 같은 주소 PDB 이름이 둘이라 "큐 스핀락 놓기" 로 적었다.
- ⑥ 보탬: run53 훑기 표의 다른 줄 (`ChargeOnlySession` ← `PspExitThread +0x790` · `LegacyPowerObject` ← `+0x718` · `ExitStatus` ← `+0x100` · `PspTerminateThreadByPointer +0x6e`) 은 **맞다**.
- 정정: run53 `ReaperLink` 줄 (2.11).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | — | ntoskrnl·ntdll PDB 목록에 run79 (ntdll 은 있던 `RtlExitUserThread` `.asm`·`LdrShutdownThread` 호출 목록 읽기만). 사진 메모에 run79 한 줄 (값 대조 없음, `test.c:36` 만 다시 읽음) |
| 2.1 제목 | … run78 | + run79 |
| 2.1 `RtlCreateUserStack` 절 | 만드는 쪽만 (말뚝·바닥·줄) | + **푸는 쪽** `PspExitThread +0x3b8` → `+0x3f2 ZwFreeVirtualMemory(MEM_RELEASE)` `[정적]` |
| 2.1 `MmCreateTeb` 절 | 만드는 쪽만 | + **푸는 쪽** `MmDeleteTeb` (`+0x4b8`), `+0x69 MiFreeToSubAllocatedRegion` = `+0xa7` 의 짝 `[정적]` |
| 2.1 `PspCallThreadNotifyRoutines` 절 | "다른 caller 는 `PspExitThread`" (자리 없음) | + 자리 `PspExitThread +0x142` (ret `+0x147`), 인자 `(Thread, 0, 0)` `[정적]` |
| 2.3 제목 | … run75 | + run79 |
| 2.3 `PspUserThreadStartup` `+0xf5` 갈래 | 이름만 | + 안은 자기 + 직접 길, `ThreadInserted = 0` 이라 유저 스택 풀기는 건너뜀 `[정적]` |
| 3장 `test!Worker` 스레드 | 끝날 때 `RtlExitUserThread` → `LdrShutdownThread` `[관찰]` | + `RtlExitUserThread` 가 먼저 마지막인지 묻는 순서 `[정적]` (있던 `.asm`), "끝나는 커널 길은 아래" |
| 3장 **새 절** "스레드 끝내기" | — | 트리 + 일곱 줄 (`[정적]` · `[정적: 유추]` · `[소스]+[추측]`) + 동적 남은 것 |
| 4장 run77 줄 둘 | "스레드 객체 신호는 **Q141d**" (답 없음) | "(해결됨 → run79: `+0x16c` 신호 → `+0x19b` … 수동 재설정 꼴)" · 동적 6 에 run79 `[정적]` 답 (실행 확인은 그대로) |
| 4장 run78 정적 줄 | 대기열 번호 없음 | **Q146** |
| 4장 새 줄 셋 | — | 끝내기 실행 확인 (→ Q104) · 정적 남은 것 (→ Q147) · 결과서와 다르게 적은 곳·정정 |
| 5장 | — | run79 줄 둘 |

- 태그가 **올라간** 곳 (전 → 후):
  - 4장 "누가 `WaitStatus` 를 쓰나 (스레드 객체)": 대기열만 → `[정적]` (`KeTerminateThread +0x19b`).
  - 2.3 `PspUserThreadStartup +0xf5` 안: 이름만 → `[정적]`.
  - 3장 Worker 끝: `[관찰]` 순서에 `[정적]` 근거 (`RtlExitUserThread +0x29 ~ +0x56`) 가 붙음.

### 짐 지도 · run53 · run77 · run78 문서에서 바뀐 것

- 짐 지도: **새 짐 칸 없음.** 끝내기는 은퇴 뒤 (범위 밖) 라서다. 이미 있는 줄에 "푸는 쪽·내리는 쪽" 근거만 붙었다.
  - 3.3 `DeallocationStack` — 푸는 쪽 `+0x3b8` (읽기) → `+0x3f2`.
  - 3.11 `ActiveThreads` — 내리는 쪽 `+0x18e`, 읽는 쪽 `NtTerminateThread +0x2e`.
  - 3.11 `ThreadListHead` — 넣는 줄이 **`[추측]` 짐작 → `[정적]` 정확** (2.20 ③).
    - 3.11 머리 "정확 178 · 짐작 5" → **정확 179 · 짐작 4**.
    - 2장 "정확 675 · 짐작 55" → **정확 676 · 짐작 54** (아는 칸 730 은 그대로). 7.4 `ThreadListHead` 실마리 닫힘.
  - 3.11 `Flags`·`Flags.ProcessDelete` — `+0x685` 켜기 · `+0x3a3` 읽기.
  - 3.11 범위 밖 52 — `LastThreadExitStatus` (`+0x1cc`) · `ExitTime` (`PspExitLastThread +0x6b`).
  - 3.12 `ThreadListHead` 둘째 줄 — 빼는 쪽 `KeTerminateThread +0x116 / +0x119`.
  - 3.13 범위 밖 69 — `ReaperLink` (run53 정정) · `Terminated` · `ExitStatus` 쓰는 줄.
  - 3.14 머리 "끝내기 줄" 메모 (`State 4` ← `+0x1b0` 등. 은퇴 뒤라 표엔 안 넣음).
  - 판 목록 서른다섯째판, 기준 근거 목록·8장 두 표에 한 줄씩.
- run53 문서: 맨 위 정정 한 줄 (`ReaperLink` 매다는 쪽).
- run77 문서: Q141d 를 말하던 세 곳 ("결과" 머리 줄 · 동적 6 · 다음 바퀴 후보 끝 줄) 에 "(→ run79)".
- run78 문서: "정적 — 다음 바퀴 후보" 머리에 **Q146**.

### 큰 그림에서

- **지도 쪽**:
  - 끝내기는 지도 밖 (`main` 뒤) 이다.
  - 그래도 flow.md 2.1 의 작업대 (`RtlCreateUserStack`) · 안내판 (`MmCreateTeb`) 절이 이번에 **만들기 ↔ 풀기 짝** 으로 닫혔다. 같은 짐을 누가 놓고 누가 치우는지가 한 자리에서 읽힌다.
- **요청 25 의 줄기**:
  - 직원이 들어오고 (run74), 계산대를 받고 (run75 · run76), 기다리고 깨고 (run77 · run78), **퇴근한다** (run79).
  - run77 에서 "어깨 두드리기" 를 **받는** 쪽 (메인) 을 봤다면, 이번엔 그걸 **부르는** 쪽 (Worker 의 "퇴근함" 불) 을 봤다. test.exe 의 두 직원 이야기가 한 바퀴 이어졌다.
- **PE_emulator 쪽**:
  - 줄기는 맞았다. 보탤 곳은 넷 — 인원 내리기, 작업대·안내판 (순서까지), 깨우는 자리, 목록 둘.
- **다음**: Q141e (요청 25 ㅁ) — 기다리는 **남의** 직원에게 업무 메모 (문맥) 를 바꿔 넣기 (`PspSetContextThreadInternal`).
  - 남에게 쪽지 (APC) 를 거는 길이라, 이번 2.14 의 쪽지 길과 닿을 수 있다. Q147 ① 도 그쪽과 겹칠 수 있다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| Worker 가 정말 손잡이 0 · "마지막 아님" 으로 오나 | 실행 때 값 (`ActiveThreads`) | **Q104** 동적 1 (`ntdll!RtlExitUserThread+0x2e` `eax`·`dd @rsp+0x48 L1`, `nt!NtTerminateThread` 입구 `rcx` · `+0x2e` 뒤 `ecx`) |
| 인원 내림의 실제 값, 메인이 끝날 때 1 → 0 이고 `PspExitLastThread` 로 가나 | 실행 때 값 | **Q104** 동적 2 (`nt!PspExitThread+0x18e` 앞뒤 `dd <프로세스>+0x380 L1`, `r12b`) |
| Worker 에서만 `+0x3f2` · `+0x4b8` 이 불리고 인자가 맞나, 메인은 안 불리나 | 실행 때 길 | **Q104** 동적 3 |
| `TEB+0x16a8` 이 test.exe 에서 0 인가 (`+0x40f` 가 불리나) | 실행 때 값 | **Q104** 동적 4 |
| 불·깨움 실제 (`+0x16c` 뒤 1, `+0x19b` 횟수·`r8`, 메인 `WaitStatus 0`, `State 4`, Worker 가 다시 안 도나) | 실행 때 값 | **Q104** 동적 5 |
| `PspReaper`·`PspThreadDelete` 가 언제 도나 (`NtClose` 안 vs `PspReaper +0x69` 안) | 참조 수는 실행 때 | **Q104** 동적 6 (결과서에서 고친 항목) |
| `PsThreadType` 의 `DefaultObject` 실제 값 | 정적은 읽은 줄 셋으로 유추 | **Q104** 동적 7 · 정적 **Q147 ②** |
| 남 끝내기 쪽지의 중간 한 걸음 (`KiSchedulerApc +0x1d4` → `KiDeliverApc +0x46b`) | 세 곳의 줄만 맞춤 `[추측]`. test.exe 엔 남 끝내기가 없음 | **Q104** 동적 8 · 정적 **Q147 ①** (Q141e 와 겹칠 수 있음) |
| 계산대 둘에서 두 직원이 동시에 인원을 내릴 때 누가 마지막인가 | 런타임 | **Q104** 동적 9 (동적 2 와 같이) |
| 마지막 직원의 작업대·안내판이 정말 땅째 사라지나, `ProcessSelfDelete` 를 켜는 곳 | 단계 3 본문은 이름만, `NtTerminateProcess` 자기 길은 부르는 줄만 | 정적 **Q147 ③** (PE_emulator `process_exit` 와 닿음) |
| 메인의 `RtlExitUserProcess` 가 `NtTerminateProcess` 를 어떻게 부르나 | ntdll 쪽 안 읽음 | 정적 **Q147 ④** |
| `NtQueryInformationThread` 클래스 `0xc` 의 커널 쪽 — `ActiveThreads == 1` 을 보나 | 안 읽음 | 정적 **Q147 ⑤** |
| `TpCheckTerminateWorker(0)` 가 하는 일 | 안 읽음 | 정적 **Q147 ⑥** |
| 출근 도장 창구 길 (`ThreadInserted = 0`) 에서 작업대를 누가 치우나 | 이번 근거에 없음 `[공백]` | 대기열 항목 없음 |
| 줄기 밖 (ETW · 감사 · 작업 객체·silo · LPC · Win32k 콜아웃 · 디버거 · FLS · 전력 · 보안 스레드) | 이름만 봤다 | 대기열 항목 없음 |

- 추천 멈춤점 (run 문서, **모두 test.exe 스레드일 때만** 조건):
  - `ntdll!RtlExitUserThread+0x2e` · `+0x51`
  - `nt!NtTerminateThread+0x34` · `+0x41`
  - `nt!PspExitThread+0x18e` · `+0x1bd` · `+0x3a8` · `+0x3f2` · `+0x40f` · `+0x4b8` · `+0x54c`
  - `nt!KeTerminateThread+0x16c` · `+0x19b` · `+0x1b0` · `+0x24f`
  - `nt!PspReaper`, `nt!PspThreadDelete`, `nt!ObWaitForSingleObject+0x82`
- WinDbg 에서 기다림 함수 이름은 `nt!KeWaitForSingleObject` 다 (Ghidra 이름 `KeWaitForMutexObject` 아님).

---

## 5. 더 읽기

- run 문서: [`run79-thread-exit.md`](run79-thread-exit.md) — 질문별 표 ("지금 우리" · 근거 줄), 순서 한 장, 최소 규칙 여덟 + 덧붙임 셋, 호출 지점 표, `PspExitThread` 본문 37줄 표, TEB·유저 스택 블록, 스레드 목록 둘, `KeTerminateThread` 한 장, `PspExitLastThread` 단계 표, 남 끝내기, 호출 관계 그림, 결과서와 다르게 적은 곳, 정정, 짐 지도 반영, 동적 아홉·정적 여섯
- 결과서: [`_workspace/sast/75_thread-exit_result.md`](../../_workspace/sast/75_thread-exit_result.md) — "한눈에" 그림, 질문별 답, 최소 규칙, 동적 확인 목록 (질문 7 의 "핸들 안 닫음" 과 그 동적 항목은 run 문서 "결과서와 다르게 적은 곳" ① 로 고쳐 읽기)
- 요청서: [`75_thread-exit.md`](../../_workspace/sast/75_thread-exit.md)
- flow.md: [3장 새 절 "스레드 끝내기"](flow.md) · 2.1 `RtlCreateUserStack`·`MmCreateTeb`·`PspCallThreadNotifyRoutines` 절의 run79 줄 · 2.3 `PspUserThreadStartup` run79 줄 · 3장 `test!Worker` 스레드 · 4장 run79 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.3 `DeallocationStack` · 3.11 `ActiveThreads`·`ThreadListHead`·`Flags` · 3.12 `ThreadListHead` · 3.13 범위 밖 · 3.14 머리
- 정정이 들어간 문서: [`run53-ethread-create.md`](run53-ethread-create.md) 맨 위
- 앞 이야기:
  - [run74 해설](run74-thread-create-해설.md) — 직원 뽑기 (이번의 짝: 목수·말뚝·바닥·줄, 안내판, 입사 완료 도장)
  - [run77 해설](run77-thread-wake-해설.md) — 신호등 명판·번호표·어깨 두드리기·수동 루프·퇴장 담당 (2.9 끝 줄과 2.16 첫머리는 run78 해설 2.16 으로 고쳐 읽기)
  - [run78 해설](run78-thread-timer-해설.md) — 청소부와 같은 꼴의 "지도에 안 보이는 이음새" (`refs`)
  - [run75 해설](run75-thread-switch-해설.md) — 계산대 교대·준비 줄·`State` 번호
  - [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) — 출근 도장 창구
  - [run25 해설](../02_sprint/run25-dll-entry-해설.md) — 출근 인사 담당이 퇴근시키는 줄
  - [run13 해설](../02_sprint/run13-resume-thread-해설.md) — 쪽지·신호등·대기 팻말
