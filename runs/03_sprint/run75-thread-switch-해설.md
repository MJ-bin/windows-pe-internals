# run75 thread-switch — 해설

> 이 글은 [run75 문서](run75-thread-switch.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q141b1**: Q141b 의 첫 조각 — 스레드 넘기기 (PE_emulator 요청 25 ㄴ-1) (`_workspace/plan.md`, 이번에 **끝**)
  - Q141b 는 함수가 많아서 시작할 때 둘로 나눴다. 넘기기 (이번, Q141b1) · 퀀텀 (다음, **Q141b2**).
  - 다음 항목: **Q141b2** → Q141c1 → Q141c2 → Q141d → Q141e → Q142. 그다음 **Q104 VM 단계** 앞에서 멈춘다 (사용자 결정 2026-11-12 "vm 제외하고 Q142 까지").
  - 동적 몫 여섯은 **Q104** 에 출처로 붙였다. 작은 정적 다섯은 새 **Q143** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run75-thread-switch.md`](run75-thread-switch.md) (가장 중요)
  - 요청서 [`_workspace/sast/71_thread-switch.md`](../../_workspace/sast/71_thread-switch.md) · 결과서 [`71_thread-switch_result.md`](../../_workspace/sast/71_thread-switch_result.md) (판정 **분석성공**. 단 질문 3 "선점 → 맨 앞" 은 **부분적으로만** 닫힘)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.2 제목·한 줄 · 2.3 제목·두 줄 · 3장 새 절 하나 · 4장 줄 셋 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): `_KTHREAD` 두 줄에 근거만 (새 짐 칸 없음)
- 이번 바퀴는 **정적만**이다. 값 대조는 run44 때 찍어 둔 **메인 스레드** 사진 일곱 장으로만 했다 (flow-writer, 새 실행 없음).

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드**, 커널 스택 = **관리사무소 안 작업대** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- TEB = 직원 **개인 안내판** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)
- 주소 공간 = 가게마다 받은 **빈 땅** → [run10 해설](../02_sprint/run10-allocate-process-해설.md#주소-공간)
- `KeReadyThread` = **출근 대기 줄**에 세우기 → [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나)
- APC = 직원 책상에 붙여 두는 **쪽지** (맡겨 둔 일감) → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- IRQL = 직원 머리 위 **"방해 금지" 단계 표지판**, `SwapContext` = 계산대 **교대**, 소프트웨어 인터럽트 = **초인종 예약** → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- 새 스레드는 교대의 `RET` 로 "떨어져서" 출발한다, 비휘발 레지스터 = 직원 **개인 서랍** → [run15 해설 2.4](../02_sprint/run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다)
- 락 = **"작성 중" 자물쇠** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md)
- 오프셋 순서 ≠ 실행 순서 ("부록 보고 오기") → [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

**비유 한 가지 주의.**

- 이 글은 앞 해설들처럼 **CPU = 계산대, 스레드 = 직원** 으로 이어 쓴다. run13 의 "계산대 교대", run52 의 "이 직원은 1번·3번 계산대에서만" 과 같은 그림이다.
- 계산대는 관리사무소 것이고, **여러 가게 직원이 돌아가며 선다** 고 생각하면 된다.
- flow.md 3장 새 절은 스레드를 "손님", CPU 를 "계산대" 로 비유했다. 같은 일을 다른 자리에서 본 그림일 뿐이다. 이 글에선 "직원" 으로 통일한다.

### 한 줄로 먼저

- 계산대 (CPU) 를 다음 직원 (스레드) 에게 넘길 때, 진짜 Windows 는 **우선순위로** 고른다 `[정적]`.
  - PE_emulator 의 "목록에서 다음 실행 가능한 것, 우선순위 없음" 은 **틀림**.
- 고르는 순서 `[정적]`: 미리 정해 둔 "다음 차례" → 우선순위 31 → 1 줄 (같은 줄 안에선 **맨 앞**부터) → 우선순위 0 → 옆 계산대에서 데려오기 → 아무도 없으면 **자리 지킴이** (Idle 스레드).
- 줄에 다시 세울 땐 **기본 맨 뒤**. "선점당했고 몫이 아직 남은" 직원만 **맨 앞** `[정적]` (한 칸이 겹쳐서 부분적으로만 닫힘).
- 교대 자체는 "작업대 (스택) 바꿔 끼우기" 다. 같은 가게 직원끼리면 **땅 지도 (CR3) 는 그대로** `[정적]`.
- 스스로 양보 (`NtYieldExecution`) 하면 우선순위 **1 이상 아무나** 에게 넘긴다. 넘길 사람이 없으면 `0x40000024` `[정적]`.
- 사진: test.exe 메인 스레드는 우선순위 8 (잠깐 9), 계산대는 **2개**, 초기화 동안 계산대를 **49번** 받았다 `[관찰]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 만들기                  (run9 ~ run12)
 └─ PspInsertThread +0x5a1 KeReadyThread  "출근 대기 줄에 세운다"               (run12)
      ⋮   ← 줄에 선 직원이 언제·어떻게 계산대를 받나?                         ← 이번
 └─ SwapContext ─RET─▶ KiStartUserThread → PspUserThreadStartup → … → main     (run15)
      └─ main: CreateThread → NtCreateThreadEx → 둘째 직원도 같은 줄에          (run74)
```

- 이번 주제는 지도의 **한 단계가 아니다.**
  - 모든 스레드가 계산대를 주고받는 길이다. 초기화 내내 몇 번이고 다시 탄다.
  - 그래서 flow.md 그림엔 안 넣고, 3장에 절 하나로 적었다.
- 그래도 지도와 딱 한 군데 맞물린다.
  - run15 에서 "첫 스레드는 **교대 (`SwapContext`) 의 `RET`** 로 `KiStartUserThread` 에 떨어진다" 까지 봤다.
  - 그런데 **누가 교대를 부르나** 는 비어 있었다. 이번에 그 앞쪽이 채워졌다.

### 무엇이 궁금했나

한 문장으로: **계산대를 넘길 때 다음 직원을 어떻게 고르고, 실제로 어떻게 바꾸나.**

- 계기: `../PE_emulator` 의 nt 엔진이 `KiSwapThread` 라는 이름으로 "다음 스레드로 넘기기" 를 하는데, 지금은 "목록에서 다음 실행 가능, 우선순위 없음" 이다 `[추측]` (요청서). 진짜 본문에 맞춰 달라는 요청 25 ㄴ-1.
- 질문은 다섯 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | `KiSwapThread` 가 부르는 순서, 고르는 줄, 바꾸는 줄, 돌아온 뒤 | 2.1 · 2.2 · 2.4 · 2.7 · 2.9 |
| 2 | `KiSearchForNewThread` — 어디서 고르나, 높은 우선순위부터?, 같은 우선순위면 맨 앞?, 없으면 Idle? | 2.4 · 2.5 · 2.6 |
| 3 | 줄에 다시 세울 때 맨 앞이냐 맨 뒤냐, 무엇이 가르나 | 2.10 |
| 4 | `NtYieldExecution` — 몸, 누구에게 넘기나, `0x40000024` 와 `0` 은 언제 | 2.11 |
| 5 | `KiSwapContext` — 무엇을 저장하고, 스택·주소 공간을 어떻게 바꾸나 | 2.8 |

### 바퀴 전에 알던 것

- `[정적]` run52: `KiAbProcessPreContextSwitch` 를 부르는 곳 = `KiSwapThread`·`KiQuantumEnd`·`KiDispatchInterrupt`·`KiExitDispatcher`·`KiCheckForThreadDispatch`·`KiDirectSwitchThread`·`KiHandleDeferredPreemption`. 스레드가 계산대에서 내려가는 때들이다.
- `[정적]` run12 · run74: 새 스레드는 `PspInsertThread +0x5a1` (ret `+0x5a6`) `KeReadyThread` 에서 실행 가능해진다.
- `[정적]` run15: 새 스레드 첫 커널 스택 맨 위에 `KiInitializeContextThread +0x1e1/+0x1e8` 이 `KiStartUserThread` 주소를 둔다. 교대의 `RET` 가 그걸 꺼낸다.
- `[추측]` 짐 지도 3.14: `_KTHREAD.ExpectedRunTime` 이 사진 사이에 바뀌는 건 스케줄러 `KiSwapThread` 쪽일 것.
- `[추측]` PE_emulator: 다음 스레드 = 목록에서 다음 실행 가능, 우선순위 없음.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 번호 `(n)` 은 run 문서 번호 그대로.

```
(부르는 곳 4곳: KiCommitThreadWait +0x220 · KiInSwapSingleProcess +0x120 · KeTerminateThread +0x24f · KiExecuteDpcDelegate +0x8b)
KiSwapThread (Thread, Prcb, 출력 칸)
 │  +0x45 ~ +0x5a  ExpectedRunTime = Expected/2 + CurrentRun/2 · CurrentRunTime = 0          ★ 2.12
 ├─ (3) +0xe1   KiSearchForNewThread(Prcb, 0, &락)          → 다음 스레드                     ★ 2.4 ~ 2.6
 │    │  +0x20 / +0x110  NextThread 가 있고 Idle 아니면 그대로
 │    │  +0x185 · +0x19c  이 CPU 칸 기본값 = IdleThread
 │    ├─ (2) +0x6e   KiSearchForNewThreadsForRescheduleContext   +0x131 BSR (1~31) · +0x3c8 BSR (0) · +0x2d4 Flink 부터
 │    ├─ (3) +0x84   KiCommitRescheduleContext
 │    │       └─ +0x5b  KiCommitRescheduleContextEntry           +0x149 NextThread = 고른 것 · +0x179 State = 3
 │    └─ (4) +0xe4   KiSearchForNewThreadsWithinRestOfSystem     고른 게 Idle 일 때 — 다른 CPU 훔치기
 │  +0x179 NextThread = 0 · +0x17d CurrentThread = 새 · +0x190 새 State = 2                   ★ 2.7
 │  +0x21f / +0x222  새 == 지금 → 안 바꿈
 ├─ (4) +0x248  KiSwapContext(지금, 새, WaitIrql)                                             ★ 2.8
 │    └─ +0x76  SwapContext    +0xf3/+0xf7 스택 · +0x35e/+0x365 CR3 갈림 · +0x5bb ContextSwitches +1
 │              +0x5ee RET  (새 스레드 스택에서)
 │    … (다시 이 스레드 차례)
 └─ +0x25c → +0x29a  WaitStatus 를 돌려줌 (APC 있으면 +0x2f3 KiDeliverApc)                    ★ 2.9

(줄에 세우기)  KiQueueReadyThread · KiDeferredReadyThread · KiReadyDeferredReadyList
 └→ KiDeferredReadySingleThread   +0x1726/+0x172a   0 → +0x1730 맨 뒤 / ≠ 0 → +0x1824 맨 앞      ★ 2.10

NtYieldExecution
 └─ (1) +0xb   KeYieldExecution(0)      (Ghidra 이름 Ordinal_6)                               ★ 2.11
      +0x69 / +0x395   넘길 게 없으면 0x40000024
      +0x2f0           FullYield — 우선순위 1 이상 아무나
      (9) +0x216       KiQueueReadyThread(지금) → 맨 뒤
      (10) +0x224      KiSwapContext → 돌아오면 +0x22c 에서 0
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- 이번에 **처음 뽑은 함수가 21개**다. 새 지도 `_map/KiSwapThread_*` (함수 2,938개) 도 이번에 만들었다.
  - 고르는 본체 (`KiSearchForNewThreadsForRescheduleContext`, 4580B) 는 `KiSwapThread` 보다 **두 단계 아래**에 있다.
  - 지도에서 이 함수는 "공용 도구 (caller 22곳)" 라 안 펼쳐져 있었다. 그래서 결과서가 줄기 위의 것만 **이름으로** 직접 뽑았다.
- 이름 하나 주의: PDB 이름 `KeYieldExecution` 을 Ghidra 는 **`Ordinal_6`** (내보내기 번호 6) 로 잡았다. 파일 이름만 `Ordinal_6.asm` 이고, 글과 WinDbg 에선 `KeYieldExecution` 을 쓴다.
- 오프셋 순서 ≠ 실행 순서 하나: `KiSwapThread +0x516` `KiProcessThreadWaitList` 는 오프셋은 뒤인데 **맨 먼저** 탈 수 있다. `+0x14` 에서 `PRCB+0x2d08 ≠ 0` 이면 그리로 갔다가 `+0x30` 으로 돌아온다 ([run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다) 의 "부록 보고 오기").

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ (3) +0xe1 KiSearchForNewThread(Prcb, 0, &락)`

- **누가**: `KiSwapThread`.
  - 지금 직원이 **기다림 등으로 계산대를 내놓을 때** 불리는 교대 준비 담당이다.
  - 부르는 곳은 4곳뿐이다: 기다림 (`KiCommitThreadWait +0x220`), 끝내기 (`KeTerminateThread +0x24f`), `KiInSwapSingleProcess +0x120`, `KiExecuteDpcDelegate +0x8b` (`_refs/KiSwapThread.tsv`).
- **어디서**: 부르는 자리 `+0xdc`, 돌아올 자리 `+0xe1`.
  - 멈추면 콜스택에 `nt!KiSwapThread+0xe1` 로 보일 자리다. 아직 안 봤다.
  - 바로 앞 `+0x6d ~ +0xa5` 에서 **계산대 장부의 "작성 중" 자물쇠** (PRCB 락) 를 먼저 잡는다.
- **누구를**: `KiSearchForNewThread`. 인자는 (계산대 장부, **0**, 자물쇠 손잡이).
  - 둘째 인자 0 이 뜻이 있다. "못 찾으면 자리 지킴이를 기본으로" 갈래를 타게 한다 (2.6).
- **무엇을 위해**: 다음 직원을 고른다. 결과 `RAX` 가 `RBP` 로 간다 `[정적]`.

#### 줄 2: `KiSearchForNewThreadsForRescheduleContext +0x131`

호출이 아니라 **명령 한 줄**이다. 그런데 이번 바퀴의 핵심 질문 "높은 우선순위부터?" 의 답이 이 줄이다.

```
+0x125  AND EAX,0xfffffffe
+0x131  BSR R11D,EAX
```

- **`EAX` 에 든 것**: "이 계산대가 받을 수 있는 우선순위" 표시 32칸 (마스크).
  - `KiStartReadyQueueEnumeratorForRescheduleContext +0x1cf ~ +0x1dc` 가 **두 줄의 "사람 있음" 표시와 AND** 해서 만든다 (아래 2.5).
- **`+0x125`**: 맨 아래 칸 (우선순위 0) 을 지운다. 0 은 맨 마지막에 따로 본다.
- **`+0x131 BSR`** = 켜진 칸 중 **가장 높은 자리 번호** 를 찾는 명령. 그 번호가 `R11D` 로 간다.
- **그래서**: `R11D` = 지금 사람이 서 있는 줄 가운데 **가장 높은 우선순위 (1~31)** `[정적]`.
- 비유: 계산대 위 **전광판 불 32개** 중, 켜져 있는 것 가운데 **번호가 가장 큰 불**을 고른다.

### 2.3 무대 먼저 — 계산대 장부 (PRCB) 와 업무 카드의 칸

이번 이야기에 나오는 칸을 먼저 한 번 보자. 뒤에서 오프셋이 나오면 이 표로 돌아오면 된다.

#### 새 말 — PRCB

- `_KPRCB` = **CPU 마다 하나씩 있는** 커널 칸 묶음. 코드에선 `gs:[0x20]` 으로 찾는다 `[정적]` (`KeYieldExecution +0x4a ~ +0x69`, `KiSwapContext +0x5f`).
  - CPU 마다 하나라는 건 run 문서 flow-writer 덧붙임 ("`PRCB` 는 CPU 마다 있고 칸은 `PRCB+0x18`").
- 비유: **계산대마다 붙어 있는 장부**. "지금 선 직원 · 다음 차례 · 자리 지킴이 · 대기 줄 현황" 이 적혀 있다.
- **조심**: 공개 PDB 의 `_KPRCB` 는 **`+0x6d0` 에서 잘려 있다** (`pdb_struct.py` sizeof `0x700`).
  - 그래서 `+0x83d8` 처럼 그 뒤 칸은 **이름이 없다.**
  - 오프셋·쓰임은 코드에서 읽었으니 `[정적]`. 붙인 이름 (`ReadySummary` 등) 은 `[추측: 이름]` 이다. 같은 모양의 공개 구조체 `_KSHARED_READY_QUEUE` (`ReadySummary +0x8` · `ReadyListHead +0x40`) 에 맞춘 이름이다.

| 계산대 장부 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x8` | `CurrentThread` | 지금 계산대에 선 직원 |
| `+0x10` | `NextThread` | **다음 차례** 자리 |
| `+0x18` | `IdleThread` | **자리 지킴이** (2.6) |
| `+0x23` | `IdleState` | 자리 지킴이 상태 표시 |
| `+0x30` | `PrcbLock` | 장부 "작성 중" 자물쇠 |
| `+0x83d8` | (이름 없음) `[추측: ReadySummary]` | 이 계산대 줄의 **전광판** (32비트) |
| `+0x83dc` | (이름 없음) | "번갈아" 칸 (2.5) |
| `+0x8440 + 우선순위 × 0x10` | (이름 없음) `[추측: DispatcherReadyListHead]` | 이 계산대 **줄 머리** 32개 |
| `+0x8e48` | (이름 없음) | **같이 쓰는 줄** (`_KSHARED_READY_QUEUE`) 을 가리킴 |

#### 업무 카드 (`_KTHREAD`) 에서 이번에 쓰는 칸

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x20` | `QuantumTarget` | 이번 몫의 끝 표시 (2.10) |
| `+0x48` | `CycleTime` | 쓴 시간 (사진에서 장마다 늘어난다, 2.12) |
| `+0x50` · `+0x54` | `CurrentRunTime` · `ExpectedRunTime` | 이번에 쓴 시간 · 예상 시간 (2.12) |
| `+0x58` | `KernelStack` | 관리사무소 안 작업대 **위치** (2.8) |
| `+0x71` | `Running` | "지금 어느 계산대에 서 있음" 표시 |
| `+0xc3` | `Priority` | 우선순위 |
| `+0xc8` | `WaitStatus` | 돌아올 때 들고 가는 값 (2.9) |
| `+0xd8` | `WaitListEntry` | 줄에 꿰는 **고리** (준비 줄도 이 칸) |
| `+0x154` | `ContextSwitches` | 계산대를 받은 횟수 |
| `+0x184` | `State` | 상태 번호 (아래) |
| `+0x235` | `Preempted` | "선점당함" 표시 (2.10) |

- 모두 이름은 PDB, 쓰임은 `[정적]`.
- `State` 값 (코드에서 쓰는 숫자만): **1** Ready (줄에 섬) · **2** Running (계산대에 섬) · **3** Standby (다음 차례 자리) · **5** Waiting (기다림) · **7** DeferredReady (아직 줄에 안 꿴 "밀린 명단", 2.10).

### 2.4 고르기 ① — 고르는 건 `KiSearchForNewThread`, 맨 먼저 "다음 차례" 자리를 본다

#### 요청서의 짐작: "`KiSwapThread` 가 `NextThread` 를 먼저 보나?" → 아니다 `[정적]`

- `KiSwapThread` 는 자물쇠를 잡고 `+0xdc` 에서 **곧바로** `KiSearchForNewThread` 를 부른다.
- `KiSwapThread` 자신이 `NextThread` 칸 (`[R14+0x10]`) 을 만지는 줄은 둘뿐이다:
  - `+0x179` — 0 으로 **씀** (고른 뒤 비우기, 2.7)
  - `+0x360` — 헤테로 갈래에서 **읽음** (줄기 밖)

#### `NextThread` 를 보는 곳은 `KiSearchForNewThread +0x20` `[정적]`

1. `+0x20 MOV RBX,[RCX+0x10]` — "다음 차례" 자리를 읽는다.
2. `+0x2d JNZ +0x110` — 누가 있으면 `+0x110` 으로.
3. `+0x110 CMP RBX,[RCX+0x18]` — 그 사람이 **자리 지킴이 (Idle) 인가?**
4. 있고 Idle 이 **아니면** → `+0x11f KiDowngradeIsolationUnitLockHandle` 만 하고 그 직원을 그대로 돌려준다.
5. 없거나 Idle 이면 → `+0x33` 부터 **줄을 뒤진다** (2.5).

#### 새 말 — "다음 차례" 자리 (`NextThread`, Standby)

- 계산대 장부의 `+0x10` 칸. 이미 골라 둔 직원이 **계산대 바로 옆에서 기다리는 자리**다.
- 여기 앉은 직원의 `State` 는 **3 (Standby)** 이다. 줄에서 골라 이 자리에 앉힐 때 `KiCommitRescheduleContextEntry +0x179` 가 3 을 쓴다 `[정적]`.
- 비유: 줄 맨 앞에서 한 발 더 나와 **"다음 분" 자리에 미리 서 있는** 직원.
- 이미 누가 서 있으면 줄을 뒤질 필요가 없다. 그래서 맨 먼저 본다.

### 2.5 고르기 ② — 준비 줄: 우선순위 높은 줄부터, 같은 줄 안에선 맨 앞부터

이번 바퀴에서 제일 긴 이야기다. 천천히 가자.

#### 새 말 — 준비 줄, 우선순위, 전광판 (비트맵)

- **준비 줄** = 계산대를 받으려고 기다리는 직원들이 서는 줄. 업무 카드의 고리 칸 (`+0xd8`) 으로 꿴다 `[정적]`.
  - 비유: 계산대 앞 **대기 줄**. run12 의 "출근 대기 줄" 과 같은 줄일 것이다 `[추측]` — `KeReadyThread` 에서 이 줄까지 이어지는 길은 아직 안 봤다 (`[공백]`, Q143 ①).
- **우선순위** = 0 ~ 31 숫자. 줄이 **우선순위마다 따로** 32개 있고, 숫자가 클수록 먼저 불린다 `[정적]` (`BSR` 로 가장 높은 것부터).
  - test.exe 메인 스레드는 8 이었다 (2.12).
  - 비유: 계산대 앞에 **줄이 32개** 나란히. 31번 줄이 가장 급한 줄.
- **전광판 (비트맵)** = 32비트 칸 하나. **비트 n = "n번 줄에 사람 있음"** `[정적]`.
  - 줄에 세울 때 `|= 1 << 우선순위` 로 켜고 (`KiDeferredReadySingleThread +0x1847 ~ +0x1857`), 줄이 비면 끈다 (`KiSearchForNewThreadsForRescheduleContext +0x9cd ~ +0x9db`).
  - 비유: 줄마다 위에 달린 **"사람 있음" 불**. 32줄을 하나하나 들여다보지 않고 불만 보면 된다.

#### 줄은 두 벌 — "이 계산대 줄" 과 "같이 쓰는 줄" 을 **한꺼번에** 본다 `[정적]`

| 줄 | 전광판 | 줄 머리 |
|---|---|---|
| 이 계산대 줄 | `PRCB+0x83d8` | `PRCB+0x8440 + 우선순위 × 0x10` |
| 같이 쓰는 줄 (`_KSHARED_READY_QUEUE`) | `[PRCB+0x8e48] + 0x8` | `[PRCB+0x8e48] + 0x40 + 우선순위 × 0x10` |

- 순서가 "이 계산대 줄 → 같이 쓰는 줄" 이 **아니다.** 두 전광판을 합쳐서 가장 높은 우선순위를 고른다.
  - 그래서 PE_emulator 가 "이 CPU → 공유 → 훔치기" 순서라면 **틀림** (run 문서 표 2(a)).
- 같은 우선순위에 두 줄 모두 사람이 있으면 **번갈아** 고른다:
  - `+0xc61 BT [R10+0x83dc],EBX` — 그 우선순위 비트가 1 이면 이 계산대 줄 차례.
  - 이 계산대 줄에서 꺼내면 비트를 지우고 (`+0x992 BTR`), 같이 쓰는 줄에서 꺼내면 세운다 (`+0x783 BTS`).
  - 뜻은 "공평하게 번갈아" 로 보인다 `[추측]` (결과서).
- 비유: 계산대마다 **전용 줄**이 있고, 계산대 여러 개가 **같이 쓰는 줄**도 있다. 같은 번호 줄이면 전용 한 번, 공용 한 번.

#### 높은 우선순위부터 — 단계 셋 `[정적]`

`KiSearchForNewThreadsForRescheduleContext` 가 `+0xf6 MOV DIL,1` … `+0x2b5 INC DIL` · `+0x2b8 CMP DIL,4` 로 단계를 돈다.

| 단계 | 무엇 | 근거 |
|---|---|---|
| ① | 우선순위 **1 ~ 31** — 가장 높은 것부터 | `+0x125` · `+0x131 BSR` (2.2 줄 2) |
| ② | 그룹 스케줄링 줄 (SCB) | 전역 `KiGroupSchedulingEnabled` 를 `+0x622` 가 읽음 |
| ③ | 우선순위 **0** — 맨 마지막 | `+0x3bc AND EAX,1` · `+0x3c8 BSR` |

- ② 는 이번에 풀지 않았다. 그 전역 값이 0 이면 PE_emulator 는 무시해도 된다 (Q104 동적 3).
- 칸마다 "받을 수 있는 마스크" 는 `KiStartReadyQueueEnumeratorForRescheduleContext +0x1cf ~ +0x1dc` 가 두 전광판과 AND 해서 만든다.

#### 같은 우선순위 줄 안에서는 맨 앞부터 `[정적]`

- `+0x2d4 MOV RDI,[RAX]` — 줄 머리의 **`Flink`** = **줄 맨 앞** 사람.
  - **`Flink` / `Blink`** = 줄 머리에서 "맨 앞 사람" / "맨 뒤 사람" 을 가리키는 두 칸 (run 문서: `Flink` = 줄 맨 앞, 머리 `Blink` 뒤 = 맨 뒤).
  - 비유: 줄 머리 팻말에 적힌 **"맨 앞: 누구 · 맨 뒤: 누구"**.
- `+0x2db` — 머리로 돌아왔으면 이 줄은 끝 → `+0x69c` 다음 줄로.
- `+0x2e9 LEA RSI,[RDI-0xd8]` — 고리 자리에서 `0xd8` 을 빼면 업무 카드 시작이다 (고리가 카드 `+0xd8` 칸이니까).
- 맨 앞 직원을 **어느 칸도 못 받으면** 다음 `Flink` (그다음 사람) 로 간다. 받는지 보는 검사:
  - 우선순위 비교 `+0xd92 ~ +0xdb3` (후보가 더 높으면 받음, `+0xd9f JG`)
  - 그룹 스케줄링 (`+0x647 KiEvaluateGroupSchedulingPreemption` · `+0x955 KiIsThreadConstrainedBySchedulingGroup`)
  - 다른 CPU 칸 쪽 `+0x1081 KiCanLocalReadyThreadBeScheduledToRescheduleContextEntry`
- 뽑으면 줄에서 빼고, 줄이 비면 전광판 불을 끈다 (`+0x9cd ~ +0x9db`, 같이 쓰는 줄은 `+0x8f9 ~ +0x918`).
- PE_emulator 의 "앞에서부터 꺼냄 (FIFO)" 은 **맞다** — 단 **같은 우선순위 안에서만** (run 문서 표 2(c)).

#### 고른 사람을 "다음 차례" 자리로 — 배정표에 적었다가 옮겨 적기 `[정적]`

`KiSearchForNewThread` 는 줄을 바로 건드리지 않고 **메모지 한 장** 위에서 일한다.

- **새 말 — 다시 고르기 문맥** (reschedule context) = 고르는 동안 쓰는 작업용 칸. 계산대 장부 `+0x37e8` 에 버퍼가 있고, 계산대마다 칸 (`_KI_RESCHEDULE_CONTEXT_ENTRY`) 이 하나씩 있다.
  - 비유: **배정표**. "이 계산대에 누구를 세울까" 를 연필로 적는 종이.
- 걸음:
  1. `+0x33` 배정표 버퍼 → `+0x45 memset` (지우개) → `+0x53 KiStartRescheduleContext`.
  2. 둘째 인자가 0 이면 (`KiSwapThread` 가 부른 꼴) 이 계산대 칸에 **자리 지킴이 이름을 먼저 적어 둔다** (`+0x185` · `+0x19c`, 2.6).
  3. `+0x69` `KiSearchForNewThreadsForRescheduleContext` 가 줄을 뒤져 더 나은 사람을 찾으면 칸에 적는다 (`+0xed8` · `+0xeef`).
  4. `+0x7f` `KiCommitRescheduleContext` → `KiCommitRescheduleContextEntry +0x149 MOV [RBX+0x10],RDI` — **배정표 칸의 직원을 장부 "다음 차례" 자리에** 옮겨 적는다. `+0x179` 그 직원 `State = 3`.
  5. `+0xa7` 에서 "다음 차례" 를 다시 읽는다. Idle 이 아니면 그대로 돌려준다 (`+0x151` → `+0x15b`).
- 밀려난 사람이 있으면 `+0x287 KiReadyDeferredReadyList` 가 줄로 되돌린다 (2.10 의 "밀린 명단").

### 2.6 고르기 ③ — 그래도 없으면: 옆 계산대에서 데려오기, 마지막엔 자리 지킴이

#### 새 말 — 자리 지킴이 (Idle 스레드)

- 계산대 장부 `+0x18` `IdleThread`. **고를 직원이 아무도 없을 때 계산대에 세우는 스레드**다 `[정적]` (아래 걸음).
- 계산대마다 한 명이다 (run 문서 flow-writer 덧붙임: "PRCB 는 CPU 마다 있고 칸은 `PRCB+0x18`").
- 이 스레드가 계산대에서 실제로 무엇을 하는지는 이번에 안 봤다.
- 비유: 손님도 직원도 없을 때 계산대를 **비워 두지 않고 지키는 사람**. "빈 자리" 대신 늘 누군가 서 있게 하는 장치다.

#### 자리 지킴이가 나오는 길 — 직접 돌려주는 게 아니라 "기본값이 남아서" `[정적]`

1. `KiSearchForNewThread +0x185` · `+0x19c` — 배정표의 이 계산대 칸 두 자리를 **`IdleThread` 로 깔아 둔다.**
2. 줄을 뒤져도 못 찾으면 이 기본값이 **그대로 남는다.**
3. 커밋 (`KiCommitRescheduleContextEntry +0x149`) 이 그걸 "다음 차례" 자리에 옮겨 적는다.
4. `KiSearchForNewThread +0xa7` (또는 `+0x14b`) 가 읽어서 돌려준다.

- 비유: 배정표에 연필로 **"지킴이"** 라고 먼저 써 둔다. 줄에서 더 나은 사람을 못 찾으면 그 글씨가 지워지지 않고 남는다.
- `KiSwapThread` 는 자리 지킴이도 보통 직원과 **똑같이** `CurrentThread` 로 놓고 넘어간다 (`+0x17d`).
- PE_emulator: 고를 게 없을 때 Idle 이 없다면 **추가** (run 문서 표 2(d)). 규칙 8 "Idle 스레드는 항상 한 개" 는 **CPU 마다** 한 개다.

#### 그 전에 — 옆 계산대에서 데려오기 (훔치기) `[정적]`

- 고른 게 Idle 이면 바로 끝내지 않는다.
  - `+0xd4` 자물쇠를 풀고 → **`+0xdf` `KiSearchForNewThreadsWithinRestOfSystem`** — 다른 CPU 쪽에서 찾아본다.
  - 안에서 `PRCB+0x9418 ≠ 0` 이면 L0 쪽, 못 찾고 `+0x9420 ≠ 0` 이면 L1 쪽을 부른다. 본체 (`…OnTarget` 등) 는 안 읽었다.
- 그래도 없으면 `+0x12c KiReparkIdlingProcessorIfNecessary`, 자물쇠 다시 (`+0x146`), `+0x14b` "다음 차례" (보통 Idle) 를 돌려준다.
- 건너뛰는 조건: `+0xc7 TEST [RDI+0x23],0x10` (`IdleState` 비트 4).
- 하나 못 푼 줄: `+0xb4` 비트 0 이 서 있는데 고를 게 없으면 `+0x109 MOV ECX,0x21` → `INT 0x29` (fastfail). 뜻은 안 풀었다 (Q143 ④).
- 비유: 내 줄 32개가 다 비었으면, 자리 지킴이를 세우기 전에 **옆 계산대 줄**을 한번 둘러본다.
- test.exe 가 돈 VM 은 CPU 가 **2개**다 `[관찰]` (2.12). 그러니 이 갈래는 **탈 수 있는 환경**이다. 실제로 탔는지는 동적 (Q104 동적 4).
- PE_emulator 가 CPU 하나라면 같이 쓰는 줄·훔치기는 건너뛰어도 된다 `[추측: 에뮬레이터 범위]` (결과서).

### 2.7 고른 뒤 — 장부 고쳐 쓰기, 같은 사람이면 교대 안 함 `[정적]`

`KiSearchForNewThread` 가 돌아온 뒤 `KiSwapThread` 가 하는 일:

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x179` | `NextThread = 0` | "다음 차례" 자리 비우기 |
| `+0x17d` | **`CurrentThread = 새`** | 장부의 "지금 선 직원" 을 새 사람으로 |
| `+0x181 ~ +0x190` | 새 직원 `State` 가 1 (Ready) 이면 `ReadyTime (+0x84)` 에 기다린 몫을 더하고 (`+0x305 ~ +0x317`), `State = 2` | 줄에서 기다린 시간 적고 "근무 중" 으로 |
| 그 뒤 | 자물쇠들 풀기 | |
| **`+0x21f` / `+0x222`** | **새 == 지금** 이면 교대 안 하고 `+0x3ba` 쪽 | 뽑고 보니 나 자신 |

- 마지막 줄이 재밌다. 줄을 다 뒤졌는데 **방금 계산대를 내놓으려던 그 직원**이 다시 뽑히면, 교대 (`KiSwapContext`) 를 아예 안 부른다.
  - 그때는 `+0x3d3 KiResumeThreadCycleAccumulation`, `+0x3db KiAbProcessPostContextSwitch` 만 하고 "돌아온 뒤" (`+0x25c`) 로 간다.
- PE_emulator: "같은 스레드면 안 바꿈" 은 **흉내 낼 만하다** (run 문서 표 1(b)).

### 2.8 교대 — `KiSwapContext` → `SwapContext`

#### 새 말 — 문맥 전환 (교대)

- 지금 스레드의 하던 일 상태를 치워 두고, 새 스레드의 상태를 꺼내 계산대에 올리는 일. run13 해설부터 **계산대 교대** 라고 불렀다.
- 이번에 처음으로 교대 담당 **안쪽**을 봤다.

#### `KiSwapContext` — 껍데기: 개인 서랍에 넣고 `SwapContext` 부르기 `[정적]`

인자: `RCX` = 지금 직원, `RDX` = 새 직원, `R8` = 지금 직원의 `WaitIrql`.

1. `+0x0 SUB RSP,0x138` — 스택에 자리를 만든다.
2. `XMM6 ~ XMM15` (`+0xf ~ +0x3f`), `RBX RDI RSI R12 R13 R14 R15` (`+0x44 ~ +0x5b`) 를 **지금 직원의 스택에** 저장한다. 비휘발 레지스터만이다.
   - [run15 해설](../02_sprint/run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다) 의 **개인 서랍**이다. 다시 돌아오면 꺼내 써야 하는 물건만 넣는다.
3. `+0x5f` `RBX` = 계산대 장부 (`gs:[0x20]`) → **`+0x71 CALL SwapContext`** (ret `+0x76`).
4. 돌아오면 `+0x76 ~ +0xca` 에서 서랍을 다 꺼내고, `+0xd5 RET`.

#### `SwapContext` — 몸: 작업대 바꿔 끼우기 `[정적]`

줄기만 (run 문서 표를 줄임):

| 오프셋 | 하는 일 | 비유 |
|---|---|---|
| `+0xe` · `+0x12` | 새 직원 `Running` 이 아직 1 이면 (다른 CPU 가 막 내려놓는 중) `PAUSE` 하며 기다림 | 옆 계산대에서 아직 안 내려왔으면 잠깐 기다리기 |
| `+0x18` | 새 직원 `Running = 1` | "계산대에 섬" 표시 |
| `+0x9a ~ +0xd7` | 지금 직원 FPU 상태를 `StateSaveArea (+0x60)` 에 저장 | |
| **`+0xf3`** | **지금 `KernelStack (+0x58)` = RSP** | 내 작업대 위치를 업무 카드에 적기 |
| **`+0xf7`** | **RSP = 새 `KernelStack`** | 새 직원 작업대로 바꿔 끼우기 |
| **`+0x357` ~ `+0x365`** | **같은 프로세스면 CR3 그대로** (아래) | 같은 가게면 땅 지도 그대로 |
| `+0x42a ~ +0x44b` | 새 `InitialStack (+0x28)` 을 TSS `RSP0` 자리와 `PRCB.RspBase` 에 | |
| `+0x4ad` | **지금 직원 `Running = 0`** (스택을 바꾼 **뒤**) | "계산대에 섬" 표시 떼기 |
| `+0x4b1 ~ +0x4e8` | 새 직원 FPU 상태 복원 | |
| `+0x582` → `+0x5a8/+0x5ad` | 새 `Teb (+0xf0)` → `WRMSR 0xc0000102` (`KERNEL_GS_BASE`). `+0x504 ~ +0x5ad` 묶음은 `[RSI+0x74]` 비트 10 이 꺼져 있을 때만 (`MiscFlags.SystemThread` 로 보임 `[추측]`) | 새 직원 **개인 안내판**을 계산대 게시판에 꽂기 |
| `+0x5bb` | **새 직원 `ContextSwitches +1`** | "계산대 받은 횟수" +1 |
| `+0x5c1 ~ +0x5e6` | APC 쪽지가 있고 지금 처리해도 되면 AL = 1, 아니면 `HalRequestSoftwareInterrupt(1)` (초인종 예약) 하고 AL = 0 | |
| `+0x5e9 ~ +0x5ee` | `RET` — **새 직원 스택에서** | (아래) |

- `Running` 을 켜는 건 맨 앞 (`+0x18`), 끄는 건 스택을 바꾼 **뒤** (`+0x4ad`) 다. 결과서는 "다른 CPU 가 가져갈 수 있게 풀어 준다" 고 적었다.
  - 그래서 `+0xe` 의 기다림이 있는 것으로 읽힌다. 옆 계산대가 아직 `+0x4ad` 에 못 닿았으면, 이 직원은 아직 거기 서 있는 셈이다 `[추측: 두 줄을 이어 읽은 것]`.
- `+0x5c1 ~ +0x5e6` 의 스택 `[RSP+0x28]` 바이트는 run15 가 "CL 인자" 로 풀어 둔 칸과 같다 (run 문서 ④, [run15 해설 2.9](../02_sprint/run15-user-thread-startup-해설.md#29-run13-의-쪽지는-0x22-직후-펼쳐진다--고리-3개가-굳었다)).

#### 새 말 — CR3 바꾸기

- **CR3** = CPU 가 지금 **어느 주소 공간 (땅) 의 지도** 를 쓰고 있는지 가리키는 레지스터로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 코드에선 새 프로세스의 `DirectoryTableBase (+0x28)` 를 읽어 `MOV CR3` 에 넣는다 `[정적]`.
- 비유: 계산대 벽에 걸린 **땅 지도**. 가게 A 직원이 서면 가게 A 땅 지도, 가게 B 직원이 서면 가게 B 땅 지도를 건다 ([빈 땅](../02_sprint/run10-allocate-process-해설.md#주소-공간) 은 가게마다 다르니까).

```
SwapContext +0x357  MOV R14,[RSI+0xb8]
SwapContext +0x35e  CMP R14,[RDI+0xb8]
SwapContext +0x365  JZ  +0x42a
```

- **누가**: `SwapContext` (조각 `FUN_1406b686f`, `+0x32f` 부터).
- **무엇을 보나**: `RSI` = 새 직원, `RDI` = 지금 직원. 둘의 `ApcState.Process (+0xb8)` (붙은 프로세스) 를 비교한다.
- **갈림**:
  - **같으면** → `+0x42a` 로 건너뛴다. **CR3 를 안 바꾼다** `[정적]`.
  - 다르면 → 새 프로세스 `ActiveProcessors` 에 이 CPU 비트를 켜고 (`+0x36b ~ +0x380`), `+0x387` `DirectoryTableBase` 를 읽어, `HvlEnlightenments` 비트 0 이면 `+0x3d9 HvlSwitchVirtualAddressSpace`, 아니면 **`+0x3e0 MOV CR3,RCX`**. 옛 프로세스 쪽 비트는 끈다 (`+0x407 ~ +0x423`).
- 비유: 같은 가게 직원끼리 교대하면 지도를 **그대로** 둔다. 다른 가게 직원이 올 때만 지도를 바꿔 건다.
- test.exe 메인 스레드 → test.exe 일꾼 스레드라면 같은 가게라 안 바꿀 것이다 `[추측]` (실행 때 어떤 쌍이 교대하는지는 안 봄).

#### 왜 `KiSwapContext` 는 "한참 뒤에" 돌아오나 — 단계별로

이게 이번 바퀴에서 제일 낯선 그림이다.

1. `[정적]` 직원 A 가 `KiSwapContext` 를 `call` 한다. "돌아올 자리" (`KiSwapThread +0x248` 등) 가 **A 의 스택**에 쌓인다.
2. `[정적]` 서랍 (비휘발 레지스터) 도 **A 의 스택**에 넣는다.
3. `[정적]` `SwapContext +0xf3` 이 A 의 작업대 위치를 A 의 업무 카드에 적고, `+0xf7` 이 **B 의 작업대**로 바꿔 끼운다.
4. `[정적]` 마지막 `RET` (`+0x5ee`) 는 **B 의 스택** 맨 위를 꺼낸다. 그러니 B 가 예전에 교대를 불렀던 자리로 "돌아간다".
5. 그래서 → A 의 `call` 은 이 순간 돌아오지 **않는다.** 나중에 누군가 다시 A 로 교대할 때, 그 교대의 `RET` 가 A 의 스택을 꺼내면서 비로소 돌아온다.

- 비유: A 가 교대 담당에게 "나 잠깐 빠질게" 하고 서랍을 잠근다. 교대 담당은 B 의 작업대를 펼쳐 "하던 일 계속하세요" 한다. A 가 그 말을 듣는 건 **다음에 A 차례가 왔을 때**다.
- **처음 도는 직원은?** 예전에 교대를 부른 적이 없다. 그래서 run15 대로 첫 스택 맨 위에 **`KiStartUserThread` 주소가 미리 꾸며져** 있고, `RET` 가 거기로 떨어진다 (run 문서 "결과서와 다르게 적은 곳" ①).
  - 결과서는 "`KiSwapContext +0x76` 으로 돌아간다" 로만 적었다. 이건 **돌았던** 직원 이야기다.

### 2.9 돌아온 뒤 — `WaitStatus` 를 들고 돌아간다 `[정적]`

`KiSwapThread +0x248` 에 돌아왔다 = 나중에 이 직원 차례가 와서 다시 이 스택으로 들어왔다.

1. `+0x24e KiAbProcessPostContextSwitch`.
2. **`+0x25c MOV RDI,[R13+0xc8]`** — 업무 카드의 **`WaitStatus`** 를 읽는다.
3. `+0x263 BTR [R13+0x74],9` (`TimerActive`). 켜져 있었으면 `+0x372 KiCancelTimer`.
4. 셋째 인자 (출력 칸) 가 있으면 `+0x2bd/+0x2c4` `AbWaitObject (+0x3d0)` 를 적어 준다.
5. 교대가 돌려준 AL (APC 쪽지 표시) 이 서 있으면 IRQL 을 **1** 로 낮춰 `+0x2ee KiDeliverApc(0,0,0)` — 쪽지를 펼친다.
6. `+0x289/+0x28e` IRQL 을 `WaitIrql` 로 → **`+0x29a MOV RAX,RDI`** → `+0x2ac RET`.

- 그러니 `KiSwapThread` 가 부른 쪽 (예: 기다림 `KiCommitThreadWait`) 에 돌려주는 값은 **`WaitStatus`** 다.
- 비유: 자리 비웠던 직원이 돌아올 때 들고 오는 **"왜 깨어났나" 한 줄 결과**.
- 그 값을 **누가, 어떤 값으로** 쓰는지는 이번 범위 밖이다 → **Q141c** (기다림·깨움).
- PE_emulator: `WaitStatus` 를 돌려주면 된다. 값의 뜻은 Q141c (run 문서 표 1(c)).

### 2.10 줄에 다시 세우기 — 기본 맨 뒤, "선점당했고 몫이 남은" 직원만 맨 앞

#### 새 말 — 퀀텀, 선점

- **퀀텀** = 한 번 계산대를 받았을 때 쓸 수 있는 **시간 몫**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 코드에서 보이는 건 이것뿐이다 `[정적]`: 업무 카드 `CycleTime (+0x48)` 이 `QuantumTarget (+0x20)` 보다 **작으면** "몫이 남음" 으로 갈린다 (`KiDeferredReadySingleThread +0x144f/+0x1453`).
  - 몫을 어디서 깎고 언제 끝내는지는 다음 바퀴 **Q141b2**.
  - 비유: 계산대에 설 때마다 받는 **모래시계**.
- **선점** = 스스로 내려놓기 전에, 더 급한 (우선순위 높은) 쪽에게 자리를 **빼앗기는 것**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 코드에서 `Preempted (+0x235)` 표시를 켜는 곳 `[정적]`: `KiSearchForNewThreadsForRescheduleContext +0xe9d` · `+0x10db` · `+0x1114` · `+0x1143` · `+0x1172` — **"다음 차례" 자리 (Standby) 에 있던 직원을 더 높은 직원이 밀어낼 때**. `KiCommitRescheduleContextEntry +0x3da` · `+0x403` 도 쓴다.
  - 끄는 곳 `[정적]`: `KiDeferredReadySingleThread +0x26d` — 퀀텀을 새로 받을 때 (`+0x260` `QuantumTarget` 와 함께) `Preempted`·`AdjustReason` 둘 다 0.
  - 비유: 업무 카드에 찍는 **"자리 뺏김" 도장**.

#### 누가 줄에 꿰나 — 넷 중 하나뿐 `[정적]`

| 함수 | 크기 | 하는 일 |
|---|---|---|
| **`KiDeferredReadySingleThread`** | 9230B | **실제로 준비 줄에 꿰는 곳** (이 계산대 줄 또는 같이 쓰는 줄) |
| `KiDeferredReadyThread` | 166B | 껍데기. 본 직원 하나 + 딸린 밀린 명단을 위 함수에 하나씩 |
| `KiInsertDeferredReadyList` | 90B | **준비 줄이 아니다.** "밀린 명단" 맨 앞에 달고 `State = 7` (`+0x33`). 나중에 `KiReadyDeferredReadyList +0x46` 이 위 함수로 넣는다 |
| `KiQueueReadyThread` | 2974B | 양보 (와 퀀텀 끝 `[결과서]`) 에서 지금 직원을 넣는 길. `+0x3a` `Preempted` 읽고 → `+0x1ec` 그대로 되돌려 두고 → `+0x1f9` 위 함수 |

- 요청서는 넣는 함수 후보로 셋을 들었다. 실제로 꿰는 건 **`KiDeferredReadySingleThread` 하나**다 (run 문서 "요청서 짐작 틀림" ②).
- **밀린 명단** (DeferredReady, `State` 7) — 바로 줄에 꿰지 않고 잠깐 모아 두는 단일 목록. 비유: 줄 세우기 전 **"줄 설 사람" 메모**. 나중에 한꺼번에 줄에 세운다.

#### 갈림 줄 — 한 걸음씩 `[정적]` (`KiDeferredReadySingleThread`)

1. `+0x63 MOV [RSP+0x58],EDI` (`EDI = 0`) — 스택 칸 하나를 **0** 으로 시작한다. 이 칸이 앞/뒤를 가른다.
2. `+0xd5 JZ +0x144f` — `AdjustReason == 0` 이면 `+0x144f` 로.
3. **`+0x144f CMP R13,[RBX+0x20]`** · **`+0x1453 JNC +0x1de0`** — `R13` = `CycleTime` (`+0x2b` 에서 읽음). `CycleTime < QuantumTarget` (**몫이 남음**) 일 때만 아래로.
   - 몫이 끝났으면 `+0x1de0` 쪽으로 가서 `+0x1de6 KiComputeNewPriority` (우선순위 다시 계산). 칸은 **0 그대로**.
4. **`+0x1459 MOVZX EAX,[RBX+0x235]`** → `+0x1460 MOV [RSP+0x58],EAX` — 몫이 남았으면 칸에 **`Preempted` 도장 값**을 넣는다.
5. 갈림:

   ```
   KiDeferredReadySingleThread +0x1726  CMP [RSP+0x58],EDI
   KiDeferredReadySingleThread +0x172a  JNZ +0x1824
   ```

   - 칸이 **0** 이면 → `+0x1730 MOV RDX,[RAX+8]` (머리 `Blink`, 맨 뒤 사람) 뒤에 붙인다 → **맨 뒤**.
   - 칸이 **0 이 아니면** → `+0x1824 MOV RDX,[RAX]` (머리 `Flink`, 맨 앞 사람) 앞에 붙인다 → **맨 앞**. 그리고 `+0x183f BTS [RSI+0x83dc]` — "번갈아" 칸에 "이 계산대 줄 먼저" 표시.
6. 둘 다 이어서 `+0x1847 ~ +0x1857` 전광판 불 켜기 (`PRCB+0x83d8 |= 1 << 우선순위`).

- 줄 머리 계산은 `+0x1711 ~ +0x171f` (`Prcb + (우선순위 + 0x844) × 0x10`). 같이 쓰는 줄도 같은 칸으로 갈린다 (앞 `+0x14c9 ~ +0x14dd`, 뒤 `+0x1bb1 ~ +0x1bbb`).

#### 그래서 답

| 직원 | 줄 자리 |
|---|---|
| 선점당함 (`Preempted ≠ 0`) **그리고** 몫이 남음 | **맨 앞** |
| 몫을 다 씀 (칸이 0 그대로) | 맨 뒤 |
| 양보함 (2.11, `Preempted` 가 0 일 때) | 맨 뒤 |
| 깨어남 | 맨 뒤 |

- 요청서의 `[추측]` "선점 = 앞 / 퀀텀 끝·양보 = 뒤" 와 **맞다** `[정적]`.
- 왜 빼앗긴 사람만 앞으로 보내나 — 코드는 이유를 말해 주지 않는다. "빼앗긴 몫을 돌려주려는 것" 으로 보인다 `[추측]` (이 해설의 짐작. 근거는 "몫이 남았을 때만" 이라는 조건 하나뿐).
- 비유: "자리 뺏김" 도장이 찍혀 있고 모래시계가 아직 남았으면 **줄 맨 앞**으로 돌려보낸다. 나머지는 모두 **줄 맨 뒤**.

#### 한계 — 그래서 "부분적으로만" 닫혔다

- 스택 칸 `[RSP+0x58]` 은 이 함수의 **다른 갈래**에서도 쓰인다 (`+0x437` · `+0x46e` · `+0x13f7`, `MOV [RSP+0x58],R9D`). 거기선 락 개수 같은 값이다 (flow-writer `.asm` 확인).
- 그 갈래들이 갈림 줄 (`+0x170c` 근처) 로 이어지는지는 끝까지 안 따라갔다.
- 즉 "칸 ≠ 0 → 맨 앞" 은 `[정적]` 이지만, "칸 ≠ 0 **이면 늘** `Preempted` 때문" 인지는 아직 아니다.
- 확인: 동적 **Q104 동적 1** (`+0x1726` 에서 칸과 `Preempted` 를 같이 보기), 또는 정적 **Q143 ②**.

### 2.11 양보 — `NtYieldExecution` = `KeYieldExecution(0)`

#### 새 말 — yield (양보)

- 스레드가 **스스로** 계산대를 내놓고, 줄에 선 다른 직원에게 넘기는 일. 커널 쪽 입구가 `NtYieldExecution` 이다 (하는 일은 아래 줄기 `[정적]`).
- 기다릴 게 있어서가 아니라 "먼저 하세요" 하고 비켜 주는 것.
- 비유: 계산대에서 "**잠깐, 먼저 하세요**" 하고 한 발 물러나 줄 맨 뒤로 가기.

#### 몸은 `KeYieldExecution` — 꼬리 `JMP` 가 아니라 보통 `CALL` `[정적]`

- `NtYieldExecution` (16B): `+0x4 XOR ECX,ECX` → **`+0x6 CALL KeYieldExecution`** (ret `+0xb`) → `+0xb ADD RSP,0x28` → `+0xf RET`.
- 요청서 짐작 "꼬리 호출" 은 **틀림** (run 문서 ①). 돌아와서 정리하고 `RET` 한다.
- 인자 하나, `Flags = 0`.

#### 줄기 (`KeYieldExecution`) `[정적]`

| 걸음 | 줄 | 하는 일 |
|---|---|---|
| 1 | `+0x25` | `Flags` 에 0~3 밖 비트면 `0xc000000d` |
| 2 | **`+0x4a ~ +0x69`** | **미리 보기**: 이 계산대 전광판 (`+0x83d8`) 도, 같이 쓰는 줄 전광판도 0 이면 → **`+0x69` `0x40000024`** 로 바로 반환 |
| 3 | `+0x79 ~ +0x82` | IRQL 2 로 올림. 같은 검사를 한 번 더 (`+0x270` → `+0x395`) |
| 4 | `+0xbf` · `+0xef` | 업무 카드 자물쇠 · 장부 자물쇠 |
| 5 | `+0xf4` → `+0x2a0 ~` | "다음 차례" 가 비었으면 찾아보기: 배정표 → **`+0x2f0 OR CL,0x10` (`FullYield`)** → `+0x30b` 고르기 → `+0x31d` 커밋. 그래도 비었으면 `+0x157` → `+0x395` |
| 6 | `+0x196 ~ +0x1b4` | 찾았으면 지금 직원 **새 퀀텀** (`+0x1b4` `QuantumTarget`), `+0x1e5` `NextThread = 0`, `+0x1f4` `CurrentThread = 새`·새 `State = 2` |
| 7 | `+0x1fc` · `+0x207` | 지금 직원 `WaitReason = 0x21` (`WrYieldExecution` 로 보임 `[추측]`), `WaitIrql` |
| 8 | **`+0x211`** | **`KiQueueReadyThread`** — 지금 직원을 준비 줄에 |
| 9 | **`+0x21f`** | **`KiSwapContext(지금, 새, 1)`** — 교대 |
| 10 | **`+0x22c`** | 돌아오면 `EBX = 0` → **0 반환** |

#### 누구에게 넘기나 — 같은 우선순위만이 아니다 `[정적]`

배정표 칸 플래그에 따라 받는 후보가 다르다 (`KiStartReadyQueueEnumeratorForRescheduleContext`):

| 칸 플래그 | 받는 후보 | 근거 |
|---|---|---|
| 없음 | 지금보다 **높은** 것만 | `+0xed ~ +0xfc` (`+0xfc BTR` 로 같은 비트도 끔) |
| `PartialYield` (`0x8`) | **같은 것까지** | `+0xf6/+0xfa` 로 `BTR` 건너뜀 |
| **`FullYield`** (`0x10`) | **우선순위 1 ~ 31 전부** (지금이 0 이면 0 ~ 31) | `+0xda TEST R11B,0x10` → `+0xde JNZ +0x251` → `+0x251 ~ +0x25e` |

- `NtYieldExecution` 은 `Flags = 0` 이라 **`FullYield`** 다 (`+0x2f0`).
  - `Flags` 비트 0 이 1 이면 `+0x4e5` 에서 우선순위 16 미만은 `PartialYield`, 이상은 `FullYield` 로 가른다. `NtYieldExecution` 은 안 탄다.
- 그 가운데 **가장 높은 우선순위의 맨 앞** (2.5) 이 뽑힌다.
- 그러니 실제로는 "**자기와 같거나 낮은 것 중 가장 높은 것**" 까지 받는다 (run 문서 표 4(b)). 자기보다 낮은 직원에게도 넘긴다.
- 비유: "먼저 하세요" 를 **아무에게나** 한다. 줄에 1번 줄 직원밖에 없어도 그 사람에게 넘긴다. 다만 (지금 우선순위가 0 이 아니면) 0번 줄은 안 본다.

#### 지금 직원은 어디로 — 새 모래시계 받고 줄 맨 뒤 `[정적]`

- `+0x1b4` 에서 퀀텀을 **방금 새로** 받는다. 그래서 2.10 의 "몫이 끝남" 갈래는 아니다.
- `+0x211 KiQueueReadyThread` → `KiDeferredReadySingleThread` 로 줄에 선다. 앞/뒤는 2.10 규칙대로 `Preempted` 가 가른다 — **0 이면 맨 뒤**.
- `KeYieldExecution` 은 `Preempted` 를 따로 지우지 않는다 (`Ordinal_6.asm` 에 `+0x235` 쓰기 없음, 결과서).

#### `0x40000024` 와 `0` — 언제 무엇을 돌려주나 `[정적]`

`0x40000024` 는 요청서가 `STATUS_NO_YIELD_PERFORMED` 라고 부른 값이다. 세 갈래 모두 **"넘길 직원이 없다"** 다:

1. `+0x69` — IRQL 을 올리기 **전**, 두 전광판이 다 꺼져 있음.
2. `+0x395` — 들어오는 길 셋:
   - `+0x270` — 올린 **뒤** 다시 봤더니 다 꺼져 있음
   - `+0x157 JZ +0x380` — 찾아봤는데 "다음 차례" 가 그대로 비어 있음
   - `+0x3c5 JMP +0x395` — VP 백킹 스레드 갈래에서 `KiVpBackingThreadYieldExecution` 이 0 을 돌려줌 (줄기 밖)
3. `+0x22f` 에서 IRQL 을 원래대로 (`+0x244 ~ +0x248 MOV CR8`) 하고 반환.

- **`0`** 은 `+0x21f` 교대가 돌아온 뒤 `+0x22c`. 즉 **실제로 넘겼다가, 다시 내 차례가 와서 돌아왔을 때**다. 2.8 처럼 한참 뒤일 수 있다.
- 비유: "먼저 하세요" 했는데 줄에 아무도 없으면 **"넘길 사람 없음"** (`0x40000024`). 누가 있어서 넘겼으면, 다시 계산대에 돌아온 순간 **"넘겼다 왔음"** (`0`).

### 2.12 사진과 맞춰 보기 — 메인 스레드 업무 카드 일곱 장

flow-writer 가 run44 덤프 읽기 로그 (`02b KTHREAD` 구획) 를 다시 읽었다. 새 실행은 없다.

- 대상은 test.exe **메인 스레드 하나**다 (`ffff828598445080`). 일꾼·둘째 스레드는 사진에 없다.
- 사진 지점 K2 ~ P2 는 [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가). P0 = `PspUserThreadStartup` 입구.

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 |
|---|---|---|---|---|---|---|---|
| `Priority (+0xc3)` | 0 | 8 | 8 | 8 | 8 | **9** | **9** |
| `BasePriority (+0x233)` | 0 | 8 | 8 | 8 | 8 | 8 | 8 |
| `QuantumReset (+0x28b)` | 0 | 2 | 2 | 2 | 2 | 2 | 2 |
| `State (+0x184)` | 0 | 0 | 2 | 2 | 2 | 2 | 2 |
| `Running (+0x71)` | 0 | 0 | 1 | 1 | 1 | 1 | 1 |
| `ContextSwitches (+0x154)` | 0 | 0 | **1** | `0x17` | `0x1c` | `0x2a` | `0x31` |
| `Preempted (+0x235)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

모두 `[관찰]`. 줄 번호는 run 문서 표에 있다.

#### 읽은 것 하나씩

- **우선순위** `[관찰]`: 초기화 내내 `BasePriority 8`. `Priority` 는 P1b 부터 9 — 잠깐 올라간 값으로 보인다 `[추측: 부스트]`.
  - 결과서가 동적으로 남긴 "test.exe 스레드 우선순위" 중 **메인 스레드 몫은 사진으로 닫혔다.** 일꾼·`test!Worker` 는 그대로 동적.
- **계산대 수** `[관찰]`: **2개** — PEB `NumberOfProcessors 2`, KUSD `ActiveProcessorCount 2`. 그래서 훔치기 갈래 (2.6) 는 **탈 수 있는 환경**이다.
- **처음 계산대를 받는 순간** `[관찰]` + `[정적]`:
  - `ContextSwitches` 가 K3 **0** → P0 **1**. 한 번 교대를 받아 `SwapContext +0x5bb` 로 1 이 되고, `RET` 가 `KiStartUserThread` 로 가서 (run15) P0 에 닿은 것과 맞다.
  - `Running`·`State` 가 K3 0/0 → P0 1/2 인 것도 맞다 (`Running = 1` 은 `SwapContext +0x18`).
  - 다만 처음에 `State = 2` 를 쓴 줄이 어느 길이었는지는 모른다 (`KiSwapThread +0x190` · `KiUpdatePriorityMatrixForRunningTransition +0x11` 등 후보).
- **초기화 동안 49번**: `ContextSwitches` P2 `0x31` (49). 넘기기는 초기화 중에도 계속 일어난다 `[관찰]`.
- **`Preempted` 는 일곱 장 모두 0** — 사진 순간엔 "자리 뺏김" 도장이 없었다. 그 사이에 켜졌다 꺼졌는지는 모른다.
- **`KernelStack`** 은 P0 → P1 부터 장마다 바뀐다 (run 문서 표). 계산대를 내려놓을 때마다 `SwapContext +0xf3` 이 그 순간 작업대 위치를 적는 것과 어긋나지 않는다.

#### P0 → P1: "기다림 교대 한 번" 이 숫자로 맞는다 — 단계별로

숫자는 모두 run 문서에서 옮겼다.

1. `[정적]` 식: `KiSwapThread +0x45 ~ +0x56` 이 `ExpectedRunTime = Expected/2 + CurrentRun/2` 로 덮고, `+0x5a` 가 `CurrentRunTime = 0` 으로 만든다.
2. `[관찰]` 값:

| 칸 | P0 | P1 |
|---|---|---|
| `ExpectedRunTime` | `0x4c4b40` | `0x3ccc10` |
| `CurrentRunTime` | `0x5dc0` | `0x1bc60` |
| `CycleTime` | `0x5dc0` | `0x2f0940` |

3. 거꾸로 풀면: P1 의 `0x3ccc10` 이 나오려면 그 교대 순간 `CurrentRunTime` 이 `0x2d4ce0` 이어야 한다.
4. 그 순간 0 이 되고, P1 까지 다시 `0x1bc60` 이 쌓였다. 그러면 P0 → P1 사이에 쌓인 시간은 `(0x2d4ce0 − 0x5dc0) + 0x1bc60 = 0x2eab80`.
5. 그런데 `CycleTime` 이 는 양도 `0x2f0940 − 0x5dc0 = 0x2eab80`. **딱 같다.**
6. 그래서 → "P0 → P1 사이 메인 스레드는 `KiSwapThread` (기다림 쪽) 를 **딱 한 번** 탔다" 로 읽힌다 `[추측: CurrentRunTime 과 CycleTime 이 같이 는다고 보고]`.
7. 아직 확인 아님: **어떤** 기다림이었는지는 모른다.

- 짐 지도의 `ExpectedRunTime` 줄 "바뀜 원인 `KiSwapThread` 쪽" 이 이걸로 `[추측]` → `[정적]` 이 됐다 (3절).

#### P1 → P1a: 교대는 5번인데 `KiSwapThread` 는 안 탔다 `[추측]`

- `[관찰]` `CycleTime`·`CurrentRunTime` 이 똑같이 `0x44bce0` 늘었고, `ExpectedRunTime` 은 그대로다.
  - `KiSwapThread` 를 탔다면 `+0x5a` 가 `CurrentRunTime` 을 0 으로 만들었을 테니 → 이 사이 `KiSwapThread` 는 **안 탔다** `[추측: 같은 읽기]`.
- `[관찰]` 그런데 `ContextSwitches` 는 `0x17` → `0x1c`, **5 늘었다.**
- 그래서 이 다섯 번은 **`KiSwapThread` 가 아닌 길**로 내려갔다 들어온 것으로 보인다 `[추측]`.
  - 후보: `KiSwapContext` 를 직접 부르는 `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption`, run52 목록의 `KiDirectSwitchThread`·`KiExitDispatcher` 등 → **Q141b2·Q141c**, 동적 **Q104 동적 6**.
- 같은 구간 `QuantumTarget` 은 `0x7f2814` 그대로이고 P1a `CycleTime 0x73c620` 이 그보다 작다 → 2.10 기준으로 **몫이 아직 남은 상태**다 `[추측]`.

#### 디버거 영향

- 사진은 커널 디버거가 **멈춘 순간**이다. 멈출 때마다 교대 숫자가 조금 달라질 수 있다 `[추측: 디버거 영향]` (run 문서).

### 2.13 PE_emulator 에게 — 최소 규칙 여덟 + 덧붙임 셋

결과서 끝 "흉내 낼 최소 규칙" 을 run 문서가 그대로 옮겼다. 짧게:

1. **고르기**: "다음 차례" (Idle 아님) → 우선순위 31 → 1 (같은 우선순위는 맨 앞, 못 돌면 다음) → 0 → (CPU 여럿이면) 훔치기 → Idle.
2. **고른 뒤**: 줄에서 빼고 (비면 불 끔), `CurrentThread = 새`, `NextThread = 0`, 새 `State = 2`.
3. **같은 직원이 다시 뽑히면** 교대 없이 돌아온다.
4. **줄에 넣기**: 기본 맨 뒤. `Preempted ≠ 0` 이고 몫이 남았으면 맨 앞. 새 몫을 받으면 `Preempted = 0`. 넣을 때 불 켬.
5. **교대**: 상태·작업대 위치 저장 → 새 것 복원. 같은 프로세스면 주소 공간 그대로. `Running`: 새 1, 지금 0.
6. **돌아온 직원**: `WaitStatus (+0xc8)` 를 돌려준다. APC 가 있으면 IRQL 낮춰 처리.
7. **양보**: 두 줄이 다 비었으면 바로 `0x40000024`. 아니면 우선순위 1 이상 아무나 중 가장 높은 것, 지금 직원은 새 몫 받고 줄 맨 뒤, 넘겼다 돌아오면 `0`. 고를 게 없으면 `0x40000024`.
8. **Idle 스레드는 항상** 있어야 한다.

flow-writer 덧붙임 (넣을지는 PE_emulator 판단):

- 규칙 8 의 "한 개" 는 **CPU 마다** 하나 (`PRCB+0x18`). test.exe VM 은 CPU 2개 `[관찰]`.
- 규칙 5 에 **TEB → GS** 줄도 있다 (`SwapContext +0x582` → `+0x5a8/+0x5ad`) `[정적]`. 유저 에뮬레이터라면 "스레드마다 GS = 그 스레드 TEB".
- **처음 도는 스레드**는 `SwapContext` 끝 `RET` 가 `KiStartUserThread` 로 간다 (run15) — 사진 P0 `ContextSwitches 1` 과 맞다.

"지금 우리" 판정 (run 문서 표):

| 무엇 | 판정 |
|---|---|
| 우선순위 없음 | **틀림** |
| "이 CPU → 공유 → 훔치기" 순서 | **틀림** (한꺼번에 본다) |
| 같은 우선순위 안에서 앞부터 (FIFO) | **맞음** |
| 고를 게 없을 때 Idle | 없다면 **추가** |
| 줄 앞/뒤 규칙 | 지금 없음 `[추측]` → **추가** |
| 같은 스레드면 안 바꿈 | 흉내 낼 만함 |
| CR3·FPU·MSR | 흉내 대상 아님 (유저 공간 에뮬레이터) |

- **이름 주의** (run 문서 표 1(b)): 진짜 `KiSwapThread` 를 부르는 곳은 기다림·끝내기 쪽 4곳뿐이다. 퀀텀 끝·선점은 `KiSwapContext` 를 **직접** 부르는 다른 길일 수 있다 `[추측]` → Q141b2.
  - 지금 nt 엔진이 모든 넘기기를 `KiSwapThread` 라는 이름 하나로 하고 있다면, 이름·호출관계가 진짜와 다를 수 있다는 뜻이다.

### 2.14 정정과 "결과서와 다르게 적은 곳" (짧게)

요청서 짐작이 틀린 곳 셋 `[정적]`:

1. "`NtYieldExecution` 꼬리 호출로 보임" → 보통 `CALL` (`+0x6`, ret `+0xb`).
2. "넣는 함수 후보 셋" → 준비 줄에 꿰는 건 `KiDeferredReadySingleThread` 하나. `KiInsertDeferredReadyList` 는 밀린 명단 (`State = 7`).
3. "`KiSwapThread` 가 `NextThread` 를 먼저 보나" → 안 본다. `KiSearchForNewThread +0x20` 이 본다.

결과서와 다르게 적은 곳 (flow-writer `.asm` 확인):

4. 결과서 "`SwapContext` RET → `KiSwapContext +0x76`" → **처음 도는 스레드는 `KiStartUserThread`** (run15). 돌았던 스레드만 `+0x76`.
5. 결과서 "`+0x1de0` 에서 `KiComputeNewPriority`" → `+0x1de0` 은 `MOV R8B,1`, 부르는 줄은 **`+0x1de6`** (ret `+0x1deb`).
6. 결과서 "`+0x3bf/+0x3c5 JMP +0x395`" → `+0x3bf JNZ` 는 본 줄기 (`+0xb2`) 로 가는 줄. `+0x395` 로 가는 건 **`+0x3c5 JMP`** 하나.

그 밖에:

- `SwapContext` 의 `[RSP+0x28]` 바이트를 결과서는 "WaitIrql" 로, run15 는 "CL 인자" 로 적었다. 같은 칸이고 서로 어긋나지 않는다 (run 문서 ④).
- `_calls.tsv` 에 `alias:` 로 나온 callee 셋은 이름이 하나뿐이라 그 이름을 그대로 썼다 (`KeYieldExecution` · `KiFindRescheduleContextEntryForPrcb` · `KiCommitRescheduleContextEntry`, run 문서 ⑥).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | — | PDB 목록에 run75 (21개 처음 뽑음, `SwapContext` 읽기). "사진" 메모에 run75 (run44 `02b KTHREAD` 구획·CPU 수 다시 읽음, 새 실행 없음) |
| 2.2 · 2.3 제목 | … run56 / … run70 부팅 메모 | + run75 |
| 2.2 `KeReadyThread` 절 | 출근 대기 줄에 세운다 (run12·run13) | + 준비 줄에 실제로 꿰는 함수는 `KiDeferredReadySingleThread` — 기본 맨 뒤, `Preempted` + 몫 남음이면 맨 앞 `[정적]`. `KeReadyThread +0x26 KiFastReadyThread` 에서 거기까지는 `[공백]` |
| 2.3 `KiStartUserThread` 절 | 교대의 `RET` 로 들어온다 (run15). 교대를 누가 부르는지는 없음 | + 교대를 부르는 쪽 `KiSwapContext +0x71` ← `KiSwapThread +0x243` · `KeYieldExecution +0x21f` 등 `[정적]`. 돌았던 스레드는 `+0x76`, **처음만** `KiStartUserThread`. `ContextSwitches` K3 0 → P0 1 `[관찰]` |
| 3장 **새 절** "스레드 넘기기" | — | 트리 + 고르는 순서·줄 세우기·교대·양보·부르는 곳 `[정적]`, 사진 `[관찰]`, P0→P1 읽기 `[추측: 읽기]` |
| 4장 새 줄 셋 | — | Q104 동적 1 ~ 6 · 정적 남은 것 ① ~ ⑤ · 요청서 짐작 틀림·결과서와 다르게 적은 곳 ① ~ ⑥ |
| 5장 | — | run75 줄 (두 표) |

태그가 바뀐 곳:

- PE_emulator `[추측]` "우선순위 없음" → **틀림** `[정적]`.
- 요청서 `[추측]` "선점 = 앞 / 퀀텀 끝·양보 = 뒤" → **맞음** `[정적]` (단 칸 하나가 겹쳐 부분).
- 결과서 동적 "test.exe 스레드 우선순위" → **메인 스레드 몫은 `[관찰]`** (사진). 일꾼·`Worker` 는 그대로.

### 짐 지도 `baggage.md` 에서 바뀐 것

- **새 짐 칸 없음**, 2장 숫자 그대로. 짐 지도 바퀴가 아니다 — 주제가 특정 단계의 짐이 아니라 계산대 주고받기라서.
- 3.14 `_KTHREAD` 두 줄에 **바뀌는 쪽** 근거만:
  - `ExpectedRunTime` — 바뀜 원인 "`KiSwapThread` 쪽" `[추측]` → **`[정적]`** (`+0x45 ~ +0x56`, `+0x5a`). P0 → P1 값이 식 한 번과 맞음 (2.12).
  - `KernelStack` — 바뀜 원인 `SwapContext +0xf3` (교대마다).
- 3.14 머리에 "Q78d4 에서 볼 것 — 스케줄러가 쓰는 줄" 한 줄 (`State`·`Running`·`ContextSwitches`·`CurrentRunTime`·`ReadyTime`·`QuantumTarget`·`WaitListEntry` 등). 표로는 안 옮겼다.

### 큰 그림에서

- **지도 쪽**:
  - run15 가 "첫 스레드는 교대의 `RET` 로 떨어져서 출발한다" 까지 그렸다. 이번에 **그 교대가 어디서 불리는지** 의 한쪽 (`KiSwapThread`·`KeYieldExecution` → `KiSwapContext`) 이 채워졌다.
  - 다만 **처음** 교대를 받은 길이 어느 쪽이었는지는 아직 모른다 (2.12, Q143 ①).
- **PE_emulator 쪽**:
  - "우선순위 없음" 을 고칠 근거가 생겼다. 고르기·줄 세우기·Idle·양보 결과값까지 규칙 여덟 개로 정리됐다.
- **다음** (같은 요청 25):
  - **Q141b2** (퀀텀): 모래시계를 **누가 깎고 언제 끝내나**. P1 → P1a 의 다섯 번 교대가 그쪽 길일 수 있다.
  - **Q141c** (기다림·깨움): `KiSwapThread` 를 부르는 `KiCommitThreadWait +0x220` 쪽, 그리고 이번에 돌려주기만 본 **`WaitStatus` 를 누가 쓰나**.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| "칸 ≠ 0 → 맨 앞" 이 실제로 `Preempted` 때문에 타나 | 스택 칸 `[RSP+0x58]` 이 다른 갈래 (`+0x437`·`+0x46e`·`+0x13f7`) 와 겹침. 그 갈래를 끝까지 안 따라감 | **Q104** 동적 1 (`nt!KiDeferredReadySingleThread+0x1726` 에서 칸과 `@rbx+0x235`, `+0x1824` 가 서나) · 정적 **Q143 ②** |
| 실제 전광판·줄 머리 값, 일꾼·`test!Worker` 우선순위 | 실행 때 값 (메인 스레드는 사진으로 닫힘) | **Q104** 동적 2 |
| `KiGroupSchedulingEnabled` 값 (그룹 스케줄링 단계를 무시해도 되나) | 전역 값. 그 단계도 안 풀었다 | **Q104** 동적 3 · 정적 **Q143 ⑤** |
| 훔치기 갈래를 실제로 타나, `PRCB+0x9418`·`+0x9420` | 실행 때 값. 본체 (`KiSearchForNewThreadsOnTarget` 등) 안 읽음 | **Q104** 동적 4 · 본체 **Q143 ③** (CPU 하나 에뮬레이터면 불필요) |
| test.exe 에서 `NtYieldExecution` 이 불리나, 불리면 `+0x69`·`+0x395`·`+0x224` 중 어디가 서나 | 실행 때 일 | **Q104** 동적 5 (WinDbg 는 `nt!KeYieldExecution`) |
| P1 → P1a 의 교대 5번은 어느 길인가 | 사진은 숫자만 보여 준다 | **Q104** 동적 6 (`nt!KiSwapContext` 입구 `k`) · **Q141b2** · **Q141c** |
| 새 스레드가 준비 줄에 들어가는 길 (`KeReadyThread +0x26 KiFastReadyThread` → `KiDeferredReadySingleThread`), 첫 교대를 받은 길 | `KiFastReadyThread` 본문을 안 뽑음 `[공백]` | 정적 **Q143 ①** |
| `KiSearchForNewThread +0xb4`·`+0x109` (`INT 0x29`, `ECX = 0x21`) 의 뜻 | 안 풀었다 | 정적 **Q143 ④** |
| 퀀텀 끝·선점 때의 길 (`KiQuantumEnd`, `KiCheckForThreadDispatch +0x10e`·`KiHandleDeferredPreemption +0x297` 의 `KiSwapContext`) | 이번 범위 밖 `[추측]` | **Q141b2** (다음 바퀴) |
| `WaitStatus` 를 누가 어떤 값으로 쓰나 | 이번엔 돌려주는 쪽만 봄 | **Q141c** |
| `PRCB+0x83d8` 등 칸의 진짜 이름 | 공개 PDB 의 `_KPRCB` 가 `+0x6d0` 에서 잘림 | 대기열 없음 (`[추측: 이름]` 으로 둠) |
| `WaitReason 0x21` = `WrYieldExecution`? `[RSI+0x74]` 비트 10 = `MiscFlags.SystemThread`? `PRCB+0x2d3c`·`+0x8674` 이름 | 숫자만 보고 맞춘 이름 `[추측]` | 대기열 없음 |

- 추천 멈춤점 (run 문서): `nt!KiSwapThread` 입구 · `+0xe1` `rax` · `+0x248` `rax`, `nt!KiSearchForNewThread+0x2a` (`rbx`), `nt!KiSwapContext` 입구, `nt!SwapContext+0x3e0` (프로세스가 다를 때만 닿음).
  - 커널 멈춤점은 모든 프로세스에 걸린다. test.exe 스레드일 때만 서게 조건을 붙여야 한다.

---

## 5. 더 읽기

- run 문서: [`run75-thread-switch.md`](run75-thread-switch.md) — "PE_emulator 쪽에서 쓸 것" 표·최소 규칙, 호출 지점 표, 고르기·줄 세우기·양보·교대 절, 호출 관계 그림, 사진 대조, 결과서와 다르게 적은 곳
- 결과서: [`_workspace/sast/71_thread-switch_result.md`](../../_workspace/sast/71_thread-switch_result.md) — "한눈에" 그림, 질문별 답, `SwapContext` 표, 최소 규칙, 동적 확인 목록·추천 멈춤점
- 요청서: [`_workspace/sast/71_thread-switch.md`](../../_workspace/sast/71_thread-switch.md)
- flow.md: [3장](flow.md) 새 절 "스레드 넘기기 — `nt!KiSwapThread` · …" · [2.2](flow.md) `KeReadyThread` 절의 run75 줄 · [2.3](flow.md) `KiStartUserThread` 절의 run75 두 줄 · 4장 run75 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.14 `ExpectedRunTime`·`KernelStack` 줄, 3.14 머리 "Q78d4 에서 볼 것"
- 앞 이야기: [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나) (출근 대기 줄) · [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) (IRQL·교대·초인종) · [run15 해설 2.4](../02_sprint/run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다) (교대의 `RET` 로 출발, 개인 서랍) · [run52 해설](run52-kthread-create-해설.md) (업무 카드) · [run74 해설](run74-thread-create-해설.md) (둘째 직원 뽑기)
