# run13 resume-thread — 해설

> 이 글은 [run13 문서](run13-resume-thread.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q5a** — 2.2 첫 스레드를 풀어 주는 nt 쪽 + 멈춰 두는 쪽 `KeSuspendThread`·`KiSuspendThread` 가 하는 일 (멈춘 스레드가 실제로 어디서 서나) (`_workspace/plan.md`)
  - 원래 Q5 하나였는데, 두 모듈(ntoskrnl, KERNELBASE)에 걸쳐 커서 이번 바퀴 시작 때 **Q5a(nt) / Q5b(KERNELBASE)** 로 나눴다 (`plan.md` 결정 기록).
- 근거:
  - run 문서 [`run13-resume-thread.md`](run13-resume-thread.md) (가장 중요)
  - 요청서 [`_workspace/sast/08_resume-thread.md`](../../_workspace/sast/08_resume-thread.md)
  - 결과서 [`_workspace/sast/08_resume-thread_result.md`](../../_workspace/sast/08_resume-thread_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q5a, Q5b, Q19, Q40
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.

이 글은 [run9](run9-create-user-process-해설.md) · [run10](run10-allocate-process-해설.md) · [run11](run11-allocate-thread-해설.md) · [run12](run12-insert-thread-해설.md) 해설 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그, 간접 호출, 별칭 …)은 run9 해설 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - `_ETHREAD` = 직원 **인사 카드** (flow.md 에선 "사원증")
  - 멈춘 채 만들기 = **"대기" 팻말**, `KeReadyThread` = **출근 대기 줄**에 세우기 (run9, run12)
  - `PspUserThreadStartup` 주소 = 인사 카드에 붙인 "**첫 출근하면 이 창구부터**" 쪽지 (run11)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번에 새로 붙는 비유 (flow.md 2.2 에 쓴 것과 같다):
  - 멈춤 APC = 출근하자마자 **펼쳐 볼 쪽지** ("출근하면 이것부터")
  - `SuspendEvent` = 직원 앞 **신호등** (초록 = 가도 됨, 빨강 = 기다려)
  - `KiTryUnwaitThread` = 기다리던 직원 **어깨 두드려 깨우기**
  - 주의: run11 의 "첫 출근 창구" 쪽지와 이번 "펼쳐 볼 쪽지" 는 **다른 쪽지**다. 앞의 것은 "어디로 출근하나", 이번 것은 "출근하자마자 무엇부터 하나" 다.

한 줄로 먼저 말하면:

- run12 까지 "첫 직원에게 대기 팻말을 걸고, 그래도 대기 줄에는 세운다" 까지 알았다.
- 이번엔 그 팻말의 **정체**를 열어 봤다. 팻말은 "줄에서 빼 두기" 가 아니라 **"쪽지 넣어 두기 + 신호등 빨간불"** 이었다.
- 그래서 멈춘 첫 직원은 **출근은 하고**, 커널 시작 코드 맨 앞에서 쪽지를 펼쳐 본 뒤 **빨간불 앞에서 잔다** (서는 자리 `[정적]`, 언제 거기 가나는 `[추측]`).
- 그리고 나중에 누가 신호등을 초록으로 바꾸고 어깨를 두드리는지, 그 **커널 쪽 사슬**을 찾았다: `NtResumeThread` → `PsMultiResumeThread` → `KeResumeThread` → `KiResumeThread` → `KiTryUnwaitThread` `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (유저 쪽): "새 프로그램 띄워 줘"
     ├─ 시스템 콜 ① nt!NtCreateUserProcess                  (run9 ~ run12)
     │    └─ +0x101a  PspInsertThread                        (run12)
     │         ├─ +0x2e5  KeSuspendThread                    ← 이번 바퀴: 팻말의 안쪽 (멈춰 두기)
     │         └─ +0x5a6  KeReadyThread                      (run12) 대기 줄
     └─ (나중) 시스템 콜 ② nt!NtResumeThread ?               ← 이번 바퀴: 풀어 주기 (nt 쪽만)
                                                               부모가 정말 부르나는 Q5b
 ─ ─ (2.2: 첫 직원은 언제 일을 시작하나?) ─ ─
 test.exe 첫 스레드 (커널)
  └─ KiStartUserThread → PspUserThreadStartup
       └─ (멈춘 스레드면) 멈춤 APC 에서 잠듦                ← 이번 바퀴: 서는 곳
  └─ ntdll!LdrInitializeThunk → 로더 → main
```

- 이번 바퀴는 **세 장면**에 걸쳐 있다.
  1. 부모 cmd.exe 스레드가 `NtCreateUserProcess` 안에서 **멈춰 두는** 장면 (run12 의 `+0x2e5` 안쪽)
  2. test.exe 첫 스레드가 처음 달리기 시작해서 **멈춰 서는** 장면
  3. 부모 cmd.exe 스레드가 **나중에 다른 시스템 콜로 풀어 주는** 장면
- 1·3 은 부모 스레드, 2 는 test.exe 스레드다. 서로 다른 직원이 각자 하는 일이라, flow.md 에서도 그림 1(부모)과 그림 2(test.exe)에 나눠 그렸다 (→ 3절).

### 무엇이 궁금했나

run 문서 "목적" 그대로, 네 가지다.

1. 풀어 주는 사슬: `NtResumeThread` → … → `KeResumeThread`. 이름이 비슷한 `PsResumeThread` 가 사이에 끼나.
2. `KeResumeThread` 는 멈춤 횟수를 깎고, 0 이 되면 **무엇을 해서** 스레드를 다시 달리게 하나.
3. 멈춰 두는 쪽(`KeSuspendThread`/`KiSuspendThread`)은 무엇을 하나. 멈춘 첫 스레드는 **실제로 어디에 서 있나**. run12 의 "멈춘 스레드도 `KeReadyThread` 로 줄에 선다" 와 어떻게 맞물리나.
4. `NtAlertResumeThread` 와 `NtResumeThread` 는 뭐가 다른가 (Q5b 준비).

### 바퀴 전에 알던 것

- `[관찰]` (01_sprint run4) test.exe 의 모든 새 스레드는 콜스택에서 `nt!KiStartUserThread+0x28` → `nt!PspUserThreadStartup` 에서 출발했다.
- `[정적]` (run9) `NtCreateUserProcess` 안에는 첫 스레드를 **풀어 주는 호출이 없다**. 그러니 푸는 건 그 밖의 일이다.
- `[정적]` (run9, run12) `PspInsertThread` 는 flags bit0 가 켜져 있으면 `+0x2e5` `KeSuspendThread` 로 멈춰 두고, 멈춤 여부와 상관없이 끝 `+0x5a6` 에서 `KeReadyThread` 로 대기 줄에 세운다.
- `[정적]` (run11) `PspAllocateThread +0x681` 이 `PspUserThreadStartup` 주소를 `KeInitThread` 에 건넨다 ([run11 해설 2.5](run11-allocate-thread-해설.md#25-pspuserthreadstartup-은-불리는-게-아니라-주소가-건네진다)).
- `[추측]` (flow.md 2.2, run12 바퀴) "멈춤 팻말이 걸려 있으면 줄에 서도 바로 일하진 못한다." → 이번에 **모양이 고쳐진다** (2.13).
- `[공백]` (run12) 멈춘 스레드가 `KeReadyThread` 뒤 **실제로 서는 시점**, 팻말을 **누가·언제** 떼 주나 ([run12 해설 2.10](run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나)).

---

## 2. 찾은 것

### 2.1 한눈에 — 세 장면

run 문서 "호출 관계" 를 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

**장면 1 — 부모 cmd.exe 스레드: 멈춰 두기** (run12 의 `+0x2e5` 아래)

```
PspInsertThread
 └─ (2) +0x2e5  KeSuspendThread                 [정적] flags bit0 일 때만
                 │  (+0x60 SuspendCount+1 저장)
                 └─ +0x6b  KiSuspendThread       [정적]
                      │  (+0x6c SuspendEvent.SignalState 1 → 0)
                      ├─ (1) +0x88   KiInsertQueueApc      [정적] 멈춤 APC 걸기
                      └─ (2) +0x1fb  KiSignalThreadForApc  [정적] 새 스레드 쪽 갈래는 [추측]
```

**장면 2 — test.exe 첫 스레드: 서는 곳**

```
KiStartUserThread                                (Ghidra 함수 아님, 원본 바이트)
 └─ +0x28  PspUserThreadStartup                  [관찰] run4 + [정적]
            │  +0x22  IRQL 을 0 으로 내림          [정적]
            ═▶ APC 인터럽트                         [추측] 배달 시점
                 └─ ?  KiDeliverApc
                        └─ +0x220  KiSchedulerApc                         [정적]
                             └─ +0xf7  KeWaitForSingleObject(SuspendEvent)  [정적] ← 여기서 잠듦
```

**장면 3 — 부모 cmd.exe 스레드: 풀어 주기** (나중의 다른 시스템 콜, 부모가 정말 부르는지는 Q5b)

```
NtResumeThread
 └─ +0x9b  PsMultiResumeThread                   [정적]
      └─ +0x25  KeResumeThread                   [정적] SuspendCount - 1
           └─ +0x87  KiResumeThread              [정적] 횟수가 0 이 될 때만, SignalState = 1
                └─ +0xab  KiTryUnwaitThread      [정적] 기다리던 스레드 깨우기
```

읽을 때 주의:

- 장면 2 의 `═▶` 는 **호출이 아니라 끼어들기**(인터럽트)다. flow.md 범례의 굵은 화살표와 같은 뜻이다 (→ 3절).
- `?` 한 칸(인터럽트 조각 → `KiDeliverApc`)은 이름이 `[추측]` 이다 (→ 2.6).
- 오른쪽 태그는 **그 줄의 호출이 코드에 있다**는 것까지다. test.exe 실행 때 정말 이 길을 탔는지는 아직 안 봤다 (Q19).

### 2.2 한 줄씩 읽는 법

트리 한 줄을 "**누가 / 어디서 / 누구를 / 무엇을 위해**" 로 풀어 읽어 보자. 이번 바퀴의 두 핵심 줄이다.

**장면 2 의 `KiSchedulerApc +0xf7` → `KeWaitForSingleObject` `[정적]`**

- 누가: `KiSchedulerApc`. **test.exe 첫 스레드 자신**이 돌리는 코드다 (← `KiDeliverApc +0x220` 이 부름).
- 어디서: 부르는 자리 `+0xf2`, 돌아올 자리 `+0xf7`.
- 누구를: `KeWaitForSingleObject`
  - 넘기는 것: 기다릴 대상 = 자기 `SuspendEvent`(`스레드+0x2e0`), 기다리는 이유 = **5 (`_KWAIT_REASON` 의 Suspended)**, Alertable = FALSE, Timeout 없음 (결과서).
- 무엇을 위해: 신호등(`SuspendEvent`)이 초록이 될 때까지 **그 자리에서 잔다.** 시간 제한도 없다.
- 디버거로 보면: 잠든 첫 스레드의 콜스택에 `KeWaitForSingleObject` ← `nt!KiSchedulerApc+0xf7` ← `nt!KiDeliverApc+0x220` 순으로 찍힐 것이다 (run 문서. 아직 안 봤다 → Q19).

**장면 3 의 `NtResumeThread +0x9b` → `PsMultiResumeThread` `[정적]`**

- 누가: `NtResumeThread`. "스레드 하나의 멈춤을 풀어 줘" 시스템 콜의 커널 쪽 입구다.
- 어디서: 부르는 자리 `+0x96`, 돌아올 자리 `+0x9b`.
- 누구를: `PsMultiResumeThread(스레드, &이전횟수, 1)`.
  - 세 번째 인자 = **풀 횟수, 상수 1**. 한 번 부르면 팻말 한 장만 뗀다는 뜻이다.
- 무엇을 위해: 바로 앞(`+0x82`)에서 핸들을 스레드로 바꿔 왔고([핸들](run9-create-user-process-해설.md#핸들) = 손잡이), 그 스레드의 멈춤 횟수를 하나 깎으러 간다.
- 눈여겨볼 점: 이름이 더 그럴듯한 `PsResumeThread` 를 **거치지 않는다** (→ 2.7).

### 2.3 "멈춤" 은 줄에서 빼는 게 아니다 — 쪽지 + 신호등

이번 바퀴의 제일 큰 이야기다. 천천히 가 보자.

**먼저 새 말 두 개**

- **APC** (비동기 프로시저 호출) = 다른 스레드에 "다음에 돌 때 이것 좀 해" 하고 **맡겨 두는 일감**. 그 스레드가 **직접** 처리한다 (flow.md 2.2 의 풀이).
  - 비유: 직원 책상에 붙여 두는 **"출근하면 이것부터" 쪽지**. 쪽지를 붙인 사람이 일을 하는 게 아니라, 직원이 와서 펼쳐 보고 자기가 한다.
- **이벤트** = 커널의 **켜짐/꺼짐 깃발**. 꺼져 있으면 그걸 기다리는 스레드는 그 앞에서 잔다 `[추측]` (일반적으로 알려진 설명).
  - "켜짐" 을 **신호 상태**라고 부른다. 칸 이름은 `SignalState` 다 `[정적]` (PDB).
  - 비유: **신호등**. 초록(1) = 지나가도 됨, 빨강(0) = 그 앞에서 대기.

**코드에서 본 것** — 멈춰 두기는 세 가지를 한다 `[정적]`

| 하는 일 | 어디서 | 비유 |
|---|---|---|
| 멈춤 횟수 `SuspendCount`(`+0x284`) 를 1 올리기 | `KeSuspendThread +0x60` | 팻말 장수 세기 |
| 스레드 안의 멈춤 APC `SchedulerApc`(`+0x288`) 를 걸기 | `KiSuspendThread` → `KiInsertQueueApc` (ret `+0x88`) | 책상에 쪽지 붙이기 |
| `SuspendEvent.SignalState`(`+0x2e4`) 를 1 → 0 | `KiSuspendThread +0x6c` | 신호등을 빨강으로 |

- 칸 이름은 결과서가 PDB 타입(`_KTHREAD` 등)으로 맞췄다. 그래서 이름도 `[정적]` 이다.

**쪽지와 신호등은 미리 준비돼 있다** `[정적]` (`KeInitThread`)

직원을 뽑을 때(run11 의 `KeInitThread`) 이미 둘을 만들어 둔다.

- `+0x152` `KeInitializeApc`: 쪽지 `SchedulerApc` 를 만든다.
  - 쪽지 내용(NormalRoutine) = **`KiSchedulerApc`**. 커널 모드 쪽지다. 쪽지가 넘기는 값(NormalContext) = 그 스레드 자신.
  - 나머지 두 칸(KernelRoutine·RundownRoutine)은 **아무것도 안 하는 함수**다. 그 주소엔 이름이 100개 붙어 있어 이름 하나로 못 박지 않았다 (→ 2.10).
- `+0x163` `KeInitializeEvent`: 신호등 `SuspendEvent` 를 **초록(1)** 으로 만든다.
  - 그래서 읽는 법이 정해진다: **1 = 안 멈춤, 0 = 멈춤.**
- `+0xed`: "이 직원에게 쪽지를 붙일 수 있음"(`ApcQueueable`, `+0x74` bit 14) 을 기본으로 켠다.
- 덤: 이 쪽지는 멈춤 전용이 아니다. 스레드를 끝낼 때(`KeRequestTerminationThread`) 등도 **같은 쪽지**를 쓴다 `[정적]` (`KiInsertQueueApc` 의 `callers:`).

비유로 (태그 없음): 인사 카드를 만들 때 책상 서랍에 **빈 쪽지 한 장**과 **초록불 신호등**을 미리 넣어 둔다. 멈춰 둘 때는 그 쪽지를 책상 위에 꺼내 붙이고, 신호등을 빨강으로 돌린다.

**그래서 run12 와 모순이 없다** — 단계별로

1. run12 `[정적]`: 멈춘 스레드도 `PspInsertThread +0x5a6` `KeReadyThread` 로 **대기 줄에 선다.** 멈춤 여부로 갈리는 분기가 없다.
2. 이번 `[정적]`: 멈춰 두기는 "횟수 + 쪽지 + 빨간불" 이고, 그 셋 중 어디에도 **"줄에서 빼기"** 는 없다.
3. 그래서 1 과 2 는 부딪히지 않는다. 줄에 서는 것과 멈추는 것은 **다른 장치**다.
4. 그럼 멈춘 직원은 줄에서 차례가 오면 **출근은 한다.** 출근하자마자 쪽지를 펼쳐 보고, 빨간불이면 그 앞에서 잔다 → 2.5, 2.6.
5. 이 연결 전체는 `[추측]` 으로 남았다. 고리 하나하나는 `[정적]` 인데, **마지막 연결(쪽지가 정말 그 자리에서 펼쳐지나)** 은 실행 때 일이라서다 (run 문서 "추측으로 남긴 연결").
   - 전제도 하나: test.exe 때 flags bit0 가 정말 켜져서 `KeSuspendThread` 가 불렸어야 한다. 안 불렸다면 이 그림은 test.exe 얘기가 아니다 (Q19).

> 한 문장으로: 첫 스레드는 "한 번도 안 달림" 이 아니라 **"커널 시작 코드 맨 앞에서 잠든 상태"** 일 가능성이 크다 `[추측]` (run 문서).

### 2.4 멈춰 두기 한 걸음씩 — `KeSuspendThread` → `KiSuspendThread`

2.3 의 표를 코드 순서로 펼친 것이다. 전부 부모 cmd.exe 스레드에서 돈다.

**`KeSuspendThread`** `[정적]` (222바이트, `PspInsertThread +0x2e5` 가 부름)

1. 신호등(`SuspendEvent`)에 자물쇠를 건다 (`KiAcquireKobjectLockSafe`, ret `+0x4a`).
2. 팻말이 이미 최대 `0x7f` 장이면 상태 `0xc000004a` 를 던지고 끝 (`+0xd2`, [조각](run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나) `FUN_1406f517a` 로 tail).
3. 아니면 **`+0x60` 에서 `SuspendCount + 1` 을 먼저 적는다.**
4. 그다음 `KiSuspendThread` (ret `+0x6b`).
5. `KiSuspendThread` 가 0(실패)을 돌려주면 `+0xd7` 에서 횟수를 **도로 내린다.**
6. `KiExitDispatcher` (ret `+0x8b`) → 이전 횟수를 돌려준다.

- 비유: 팻말 장수를 먼저 하나 올려 적고, 실제로 쪽지를 못 붙이면 장부를 원래대로 고친다.

**`KiSuspendThread`** `[정적]` (849바이트)

1. 스레드 잠금(`+0x40` bit0)을 잡는다 (`+0x27`).
2. "쪽지 붙일 수 있음"(`ApcQueueable`)이 꺼져 있으면 **아무것도 안 하고 0** (`+0x4b`). → 위 5번으로 횟수가 되돌려진다.
3. 신호등이 **이미 빨강(0)** 이면 "이미 멈춤" 으로 보고 쪽지를 또 붙이지 않고 1 (`+0x58` → `+0x5f`).
4. 아니면 **신호등을 빨강으로** (`+0x6c`). 쪽지가 아직 안 붙었으면(`+0x2da` = 0) 붙었다고 표시하고 **`KiInsertQueueApc(스레드+0x288)`** (ret `+0x88`).
   - `KiInsertQueueApc` 는 이 쪽지를 **커널 모드 일반 APC 줄 맨 뒤**에 넣는다 `[정적]` (flow.md 2.2).
5. 두 갈래로 나뉜다.

| 갈래 | 언제 | 하는 일 | 비유 (태그 없음) |
|---|---|---|---|
| **가벼운 멈춤** | 스레드가 이미 다른 걸 기다리는 중 등, 조건이 **모두** 맞을 때 | 깨워서 쪽지를 읽히지 않고, 기다림 표(블록)를 원래 줄에서 떼어 둔다. 타이머가 있으면 `KiCancelTimer` (ret `+0x26b`). 끝에 `KiSignalThread` (ret `+0x1e3`) | 이미 다른 줄에서 졸고 있는 직원은 깨우지 않고, 그 줄 번호표만 슬쩍 빼 둔다 |
| **그 밖** | 조건이 하나라도 안 맞을 때 | 쪽지를 방금 붙였으면 **`KiSignalThreadForApc`** (ret **`+0x1fb`**) | 직원에게 "쪽지 있어요" 표시를 해 둔다 |

- `[추측]` 아직 한 번도 안 달린 새 스레드는 **"그 밖"** 갈래다.
  - 근거: 가벼운 멈춤 조건 중 "이미 Waiting(기다리는 중)" 이 새 스레드엔 안 맞는다.
  - 아직 확인 아님: 실제로 `+0x1f6`(call)에 서는지 → Q19.

**`KiSignalThreadForApc` 가 새 스레드에 하는 일** `[정적]` (`.c` 읽은 만큼)

- 대상이 **지금 이 CPU 에서 도는 스레드가 아니고** 커널 모드 APC 면, 먼저 **`ApcState.KernelApcPending`(`+0xc1`) = 1** 을 쓴다.
  - 뜻: "커널 쪽지가 줄 서 있음" 표시. 비유: 책상 위 **"쪽지 있음" 빨간 깃발.**
- 대상 상태가 Running(달리는 중)·Waiting(기다리는 중)이 아니면 **표시만 켜고 끝**이다.
- `[추측]` 새 스레드의 상태는 0 (Initialized, "막 만들어짐"). `KeInitThread` 에서 그 값을 쓰는 줄은 못 찾았다.

정리하면, 부모가 `NtCreateUserProcess` 에서 돌아올 무렵 첫 직원은 이런 모습이다 (각 칸은 `[정적]`, 새 스레드가 이 갈래를 탄다는 건 `[추측]`):

- 팻말 1장 (`SuspendCount` = 1)
- 책상에 쪽지 (`SchedulerApc` 줄에 걸림) + "쪽지 있음" 깃발 (`KernelApcPending` = 1)
- 신호등 빨강 (`SignalState` = 0)
- 그리고 대기 줄에 서 있음 (`KeReadyThread`, run12)

### 2.5 멈춘 첫 스레드는 어디에 서 있나 `[정적]`

이제 장면 2, **test.exe 첫 스레드 자신**의 코드다.

**쪽지를 펼치는 쪽 — `KiDeliverApc`**

- `KiDeliverApc` = 줄 선 APC(쪽지)를 꺼내 처리하는 함수로 보인다 `[추측]` (이름과 일반적으로 알려진 설명). 아래 호출 자리들은 `[정적]`.
- 보통 쪽지는 "쪽지에 적힌 주소" 를 꺼내 부른다. 그런 [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출) 자리가 `+0x748`, `+0x755`, `+0x767`, `+0x774` 네 곳 있다 `[정적]`.
- 그런데 우리 쪽지는 그 길을 안 탄다. `+0x1ff` 에서 "쪽지 내용이 `KiSchedulerApc` 인가?" 를 비교해서, 맞으면 **직접** 부른다 (call `+0x21b` / ret **`+0x220`**) `[정적]`.
  - 그 앞 `+0x1f7` 에서 IRQL 을 내린다 (flow.md 2.2). IRQL 은 2.6 에서 푼다.
- 비유: 쪽지함을 뒤지다가 "**멈춤 쪽지**" 는 따로 알아보고 곧장 그 담당 창구로 간다.

**쪽지 내용 — `KiSchedulerApc`** `[정적]`

1. 신호등 `SignalState` 가 **0(빨강)** 이고,
2. "끝내라" 는 요청(`+0x78` bit 15 `TerminationApcRequest`)이 없으면,
3. "멈춤 쪽지 안에서 기다리는 중"(`+0x74` bit 19 `SuspendSchedulerApcWait`) 표시를 켜고,
4. **`KeWaitForSingleObject(SuspendEvent, 이유 5 Suspended, …)`** (call `+0xf2` / ret **`+0xf7`**) — **여기서 잠든다.**
5. 깨어나 돌아오면 `+0xf7` 에서 3번 표시를 끈다.

그래서 **서는 곳 = `KiSchedulerApc +0xf2` 의 기다림**이다 `[정적]`. 잠든 스레드의 스택은 이런 모양일 것이다 (run 문서):

```
KeWaitForSingleObject
 ← KiSchedulerApc+0xf7
 ← KiDeliverApc+0x220
 ← …   (그 아래가 PspUserThreadStartup 근처인지는 Q19)
```

- 비유 (태그 없음): 직원이 출근해서 쪽지를 펼치니 "**신호등이 빨강이면 그 앞에서 기다릴 것**". 신호등을 보니 빨강이다. 의자에 앉아 잔다. 대기 사유란엔 "Suspended(멈춤)" 라고 적어 둔다.

### 2.6 쪽지는 언제 펼쳐지나 — 추측의 고리

2.5 의 "**서는 곳**" 은 코드에 있다. 하지만 "**언제** 그 자리에 가나" 는 `[추측]` 이다. 왜 그렇게 짐작하는지 고리를 하나씩 보자.

**먼저 새 말**

- **IRQL** = CPU 가 지금 "**얼마나 급한 일 중인지**" 를 나타내는 단계 숫자. 단계가 높으면 그보다 낮은 끼어들기(APC 같은 것)는 **미뤄 둔다** `[추측]` (일반적으로 알려진 설명).
  - 0 = 보통, 1 = APC 단계(APC_LEVEL), 2 = 더 높은 단계 (run 문서·결과서가 쓰는 숫자).
  - 비유: 직원 머리 위 **"방해 금지" 단계 표지판.** 1 이면 "쪽지는 나중에", 0 으로 내리면 미뤄 둔 쪽지가 바로 들어온다.
- **소프트웨어 인터럽트** = CPU 에게 "이 단계 일거리 있음" 하고 **초인종을 예약**해 두는 것 `[추측]` (일반적으로 알려진 설명). 지금 단계가 높으면 울리지 않고 기다렸다가, 단계가 내려가면 울린다.
- **`SwapContext`** = CPU 에서 지금 스레드를 내리고 다음 스레드를 올리는 커널 코드 `[추측]` (이름과 일반적으로 알려진 설명). 비유: 계산대 **교대**.

**고리** (각 고리는 `[정적]`, 잇는 마지막 연결이 `[추측]`)

1. `[정적]` 부모 쪽 `KiSignalThreadForApc` 가 "쪽지 있음" 깃발(`KernelApcPending` = 1)을 켜 뒀다 (2.4).
2. `[정적]` 부모 쪽 `KeReadyThread` 가 대기 줄에 세웠다 (run12).
3. `[정적]` 첫 스레드가 처음 CPU 에 올라갈 때, `SwapContext` 끝 조각(`frag:FUN_1406b686f+0x292` 근처)에 이런 코드가 있다: "다음 스레드에 `KernelApcPending` 이 있고 조건이 맞으면 **APC 단계 초인종 예약**(`HalRequestSoftwareInterrupt(1)`)".
4. `[정적]` 첫 커널 코드 `KiStartUserThread` 는 IRQL 을 **1** 로 올린 채 `PspUserThreadStartup` 을 부른다 (2.12). → 초인종은 아직 안 울린다.
5. `[정적]` `PspUserThreadStartup` 의 **첫 일**이 `+0x22` `MOV CR8,RDI` (`RDI` = 0) — IRQL 을 **0** 으로 내리기다.
6. `[정적]` APC 인터럽트 → `KiDeliverApc`. `KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 이 `callers:` 에 있다. 이게 `KiApcInterrupt` 의 떨어진 조각이라는 **이름은 `[추측]`**.
7. `[정적]` `KiDeliverApc +0x220` → `KiSchedulerApc +0xf7` 에서 잠든다 (2.5).

**그래서 짐작은**

- `[추측]` 쪽지는 **`PspUserThreadStartup +0x22` 의 IRQL 내림 직후**에 펼쳐진다. (끼어든 자리에서 돌아올 곳은 `+0x26` 근처.)
- 그러면 `PspUserThreadStartup` 의 다음 세 호출 — `PspDisablePrimaryTokenExchange`(ret `+0x3c`), `PspNotifyThreadCreation`(ret `+0xa1`), `PspInitializeThunkContext`(ret `+0xb3`) — 보다 **먼저** 잠든다. 셋은 **풀어 준 뒤에** 돈다.
- 비유: 직원이 첫 창구에 와서 "방해 금지" 표지판을 0 으로 내리자마자 예약된 초인종이 울리고, 쪽지를 펼쳐 보고, 빨간불 앞에서 잔다. 창구의 나머지 일은 깨어난 뒤에 한다.

**아직 확인 아님** (run 문서 "못 정한 것")

- 3번 조건의 인자(CL)가 정말 새 스레드의 `WaitIrql`(`KeInitThread` 가 1 을 넣음)인지.
- 실제로 `+0x22` 그 자리에서 인터럽트가 들어오는지.
- `FUN_1406b2db3` 이 정말 `KiApcInterrupt` 인지 (정적 확인 → Q40).
- 동적으로는 `nt!KiSchedulerApc` 에 멈춰서 `k` 를 찍으면 된다 (→ 4절, Q19).

### 2.7 풀어 주는 사슬 — 그리고 끼지 않는 `PsResumeThread`

장면 3 이다. 다시 **부모 cmd.exe 스레드**, 그런데 `NtCreateUserProcess` 가 끝난 **뒤의 다른 시스템 콜**이다.

**사슬** `[정적]`

| 걸음 | 함수 | 하는 일 | 다음으로 (ret) |
|---|---|---|---|
| 1 | `NtResumeThread` | 핸들 → 스레드 (`ObpReferenceObjectByHandleWithTag`, ret `+0x82`, 접근 권한 인자 `0x1000`) | `+0x9b` |
| 2 | `PsMultiResumeThread` | `KeResumeThread(스레드, 횟수)` 를 부르고, 완전히 풀렸으면 프로세스에 표시 (아래) | `+0x25` |
| 3 | `KeResumeThread` | 멈춤 횟수를 깎고, **0 이 되면** 다음으로 | `+0x87` |
| 4 | `KiResumeThread` | 신호등 초록 + 기다리던 스레드 찾기 (2.8) | `+0xab` |
| 5 | `KiTryUnwaitThread` | 그 스레드를 기다림에서 풀어 준다 | — |

- 비유 (태그 없음): 부모가 관리사무소 창구에 손잡이(핸들)를 내밀며 "이 직원 팻말 한 장 떼 주세요" → 팻말 장부 담당 → 장수 깎기 담당 → 0 장이 되면 신호등 담당 → 어깨 두드리기.

**`NtResumeThread` 앞뒤 자잘한 일** `[정적]`

- 부른 쪽이 유저 모드(`_KTHREAD.PreviousMode` `+0x232` ≠ 0)면, "이전 횟수를 돌려받을 칸" 을 한 번 읽고 되써 본다 (`+0x32 ~ +0x4a`). 써도 되는 칸인지 미리 확인하는 모양이다.
- 끝에서 참조를 풀고(`+0xaa`), 그 칸에 이전 횟수를 쓰고(`+0xb4`), 0 을 돌려준다.

**`KeResumeThread` 순서** `[정적]`

1. IRQL 을 2 로 올린다 (`+0x26`) → 신호등 자물쇠 (ret `+0x4f`).
2. 이전 `SuspendCount` 를 읽어 둔다 (돌려줄 값). 요청 횟수가 더 크면 이전 횟수로 줄인다.
3. `SuspendCount -= 횟수` (`+0x68`).
4. **새 횟수가 0 이고, 얼림 표시(`+0x78` bit 14 FreezeCount)가 꺼져 있으면** `KiResumeThread` (ret `+0x87`).
   - 얼림 = 멈춤과 **따로 있는** 표시다. 얼린 스레드는 여기서 안 깨우고, 얼림을 푸는 `KiThawSingleThread` 가 `KiResumeThread` 를 따로 부른다 `[정적]` (`callers:`).
5. `KiExitDispatcher` → 이전 횟수 반환.

- 그러니까 팻말이 2장이면 한 번 불러서는 안 깨어난다. **0 장이 되는 그 한 번**만 4번을 탄다.

**`PsMultiResumeThread` 가 덧붙이는 표시** `[정적]`

- `KeResumeThread` 가 돌려준 이전 횟수가 0 이 아니고 요청 횟수 이하면 (= 이번에 **완전히** 풀림):
  - 프로세스의 `_EPROCESS +0x5fc` bit 15 **`Flags3.ProcessFirstResume`** 를 켠다 (`+0x4e`, 이미 켜져 있으면 건너뜀).
  - "스레드 멈춤·풀어줌 기록" 이 켜져 있으면(bit 20) `EtwTiLogSuspendResumeThread` (ret `+0x6f`). ([ETW](run12-insert-thread-해설.md#212-그-밖에-확인된-것) = 커널의 사건 기록 장치.)
- `[공백]` 이 "첫 풀어줌" 표시를 **누가 읽는지**는 안 봤다 → Q40.
- 비유: 가게 장부에 "**이 가게 첫 직원 출근 허가 났음**" 도장. 누가 그 도장을 보는지는 아직 모른다.

**`PsResumeThread` 는 이 사슬에 없다** `[정적]`

- 이름만 보면 `NtResumeThread` → **`PsResumeThread`** → `KeResumeThread` 가 그럴듯하다. 요청서도 그렇게 짐작했다 (`plan.md` Q5a 줄).
- 실제 코드는 다르다.
  - `NtResumeThread` 는 `PsMultiResumeThread` 를 **바로** 부른다 (`+0x9b`).
  - `PsResumeThread`(20바이트)도 `PsMultiResumeThread(…, 1)` 을 부르는 **얇은 껍데기**(ret `+0xf`, 꼬리호출 아님)지만, `NtResumeThread` 쪽에선 안 거친다.
  - `PsResumeThread` 를 부르는 곳은 Ghidra 가 못 찾았다 (`callers:` 빈칸). 드라이버용으로 내보낸 함수로 보인다 `[추측]`.
- 그래서 결과서가 범위를 넓혀 `PsMultiResumeThread` 를 뽑았다 ("범위를 넓힌 이유"). 사슬이 이름 짐작대로 이어지지 않아서다.
- 비유: 같은 창구로 가는 문이 둘 있다. 바깥 손님(`NtResumeThread`)은 옆문으로 곧장 들어오고, 정문(`PsResumeThread`)은 드라이버 손님용으로 보인다.

### 2.8 깨우는 방식 — 신호등 초록 + 어깨 두드리기 `[정적]`

`KiResumeThread` 가 실제로 깨우는 법이다.

1. **`+0x20` 에서 `SignalState = 1`** 을 **직접** 쓴다. 이벤트를 켜는 보통 함수(`KeSetEvent`)를 부르지 않는다.
   - 비유: 신호등 담당이 스위치를 손으로 직접 초록으로 돌린다.
2. 신호등 앞 **기다림 줄**(`SuspendEvent +0x2e8`)을 훑는다.
   - **기다림 블록** = 기다리는 스레드가 이벤트 앞에 걸어 두는 "**나 여기서 기다려요**" 번호표 `[추측]` (일반적으로 알려진 설명). 결과서는 PDB 의 `_KWAIT_BLOCK` 이라고 맞췄다 `[정적]`.
   - 번호표마다 종류(`+0x10`)를 본다. **1 이면 `KiTryUnwaitThread`** (ret **`+0xab`**) 로 그 스레드를 기다림에서 풀어 준다. **멈춘 스레드를 깨우는 호출이 이것이다.**
   - (종류 2 → `KiInsertQueueInternal` ret `+0x241`, 4 → `KeInsertQueueDpc` ret `+0x3e6`. 다른 종류의 기다림이다.)
   - 비유: 줄 선 번호표를 보고, 사람이 기다리는 표면 **어깨를 두드려** 깨운다.
3. "깨움 쪽지" 같은 걸 **따로 붙이지 않는다.** 신호등 + 어깨 두드리기, 그게 전부다.

- 다른 갈래 하나: 2.4 의 **가벼운 멈춤**을 되돌릴 때는, 떼어 놨던 번호표를 원래 줄에 다시 끼우고, 타이머가 멈췄으면 다시 걸고, `KiSignalThreadForApc`(ret `+0xcf`) 또는 `KiSignalThread`(ret `+0x19e`)로 민다. 번호표 상태 숫자(4, 5, 6)의 이름은 PDB 에 없어 `[추측]`.

깨어난 첫 직원은? `[추측]` (run 문서 "맞물림")

- `KiSchedulerApc` 의 기다림에서 돌아와 → `KiDeliverApc` 로 돌아가 → `PspUserThreadStartup` 의 나머지 → 유저 쪽(2.3)으로 간다.

### 2.9 경주 — 부모가 먼저 초록불을 켜면 `[정적]`

**경주(race)** = 두 스레드가 같은 칸을 거의 동시에 만질 때, **누가 먼저냐에 따라 결과가 달라질 수 있는 상황**. 비유: 둘이 동시에 문손잡이를 잡는 것.

여기서 경주는 이렇다.

- 쪽 A: 첫 스레드가 출근해서 `KiSchedulerApc` 에 도착한다 (2.5).
- 쪽 B: 부모가 `NtResumeThread` 를 불러 `KiResumeThread +0x20` 에서 신호등을 초록으로 바꾼다 (2.8).
- 둘은 다른 스레드라, 어느 쪽이 먼저일지 코드만으로는 정해져 있지 않다.

코드가 두 순서를 다 처리한다 `[정적]`:

| 순서 | 무슨 일이 생기나 |
|---|---|
| A 먼저 → B | 첫 스레드가 빨간불을 보고 잔다. 나중에 B 가 초록 + 어깨 두드리기로 깨운다 (2.8). |
| B 먼저 → A | B 가 이미 `SignalState = 1` 을 써 놨다. 첫 스레드가 `KiSchedulerApc` 에 와서 `+0x66 CMP [R15+4],0` 검사로 초록인 걸 보고 **기다리지 않고 지나간다.** 기다린 적이 없으니 깨울 것도 없다 (결과서). |

- 핵심은 **신호등이 "상태" 로 남는다**는 것이다. 깨우는 쪽이 먼저 와도 초록불이 켜진 채 남아 있으니, 나중에 온 직원이 그걸 보고 그냥 지나간다.
- 비유: 문자로 "출발해" 를 보내는 게 아니라 **신호등을 켜 두는** 방식이라, 직원이 늦게 와도 신호를 놓치지 않는다.
- 실제로 test.exe 때 어느 순서였는지는 모른다. `nt!KiSchedulerApc` 입구에서 `SignalState` 가 0 이면 곧 기다림, 1 이면 이미 풀림이다 (→ 4절, Q19).

### 2.10 이름 하나 — `KeWaitForMutexObject` 로 찍혔지만 `KeWaitForSingleObject`

2.5 의 잠드는 호출은 장부(`_calls.tsv`)에 이렇게 찍혀 있다.

```
KiSchedulerApc	+0xf2	+0xf7	alias:KeWaitForMutexObject+1
```

그런데 run 문서·flow.md 는 `KeWaitForSingleObject` 라고 쓴다. 왜인가.

- [한 주소에 이름 여럿 (ICF 별칭)](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 이야기와 같은 종류다. 한 집에 문패가 여럿이다.
- 다만 이번엔 **"전혀 상관없는 38가족"** 이 아니라 **같은 함수의 공개 이름 둘**이다.
  - `[정적]` asm 줄 주석은 `ALIASES(1): KeWaitForSingleObject`. PDB 공개 심볼 이름은 **`KeWaitForSingleObject` 하나**다. 별칭 표에서도 이 주소는 무리가 아니다.
  - `[정적]` `KeWaitForMutexObject` 는 **내보내기 표**에 있는, 같은 주소의 다른 이름이다.
  - 그래서 run 문서는 PDB 이름 `KeWaitForSingleObject` 로 쓰고, 장부 표시는 괄호로 남겼다.
- 비유: 한 사람이 **본명**(PDB 공개 이름)과 **가게 간판 이름**(내보내기 이름)을 같이 쓴다. 장부엔 간판 이름이 찍혔지만 같은 사람이다.
- 참고: 결과서는 "이름을 하나로 못 박지 않는다" 고 적었고, run 문서가 PDB 근거로 하나를 골랐다. 이 해설은 run 문서를 따른다.

같은 종류 하나 더 `[정적]`:

- 쪽지(`SchedulerApc`)의 나머지 두 칸이 가리키는 `0x1404a10c0` 은 이름이 **100개** 붙은 무리다. 이건 38가족 쪽에 가깝다.
- 그래서 이름 하나로 안 쓰고 **"아무것도 안 하는 함수"** 라고 하는 일로만 적었다 (후보: `KiSchedulerApcNop`, `KiSchedulerApcRundown` 등).

### 2.11 `NtAlertResumeThread` 와 다른 점 — Q5b 준비 `[정적]`

부모가 풀어 줄 때 쓸 수 있는 시스템 콜이 하나 더 있다. 둘을 나란히 보면:

| | `NtResumeThread` | `NtAlertResumeThread` |
|---|---|---|
| 접근 권한 인자 | `0x1000` | `0x2` |
| 추가 검사 | 없음 | 유저가 부르면 시스템 스레드 대상은 `0xc0000022` 로 거절 |
| 부르는 것 | `PsMultiResumeThread` (ret `+0x9b`) | `KeAlertResumeThread` (ret `+0xb4`) → `KeAlertThread` (`+0x34`) 다음 `KeResumeThread` (`+0x41`) |
| `ProcessFirstResume` 표시·ETW 기록 | **한다** | **안 한다** (`PsMultiResumeThread` 를 안 거침) |

- **알림(alert)** = 스레드를 콕 찔러 기다림에서 깨울 수 있게 하는 커널 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 어깨 두드리기와 별개로 거는 **호출 벨**. 그래서 `NtAlertResumeThread` 는 "**알림 켜고 풀기**" 다 (run 문서).
- `[추측]` `0x1000`·`0x2` 가 SDK 의 `THREAD_RESUME`·`THREAD_SUSPEND_RESUME` 라는 건 헤더 상식으로 맞춘 것이다.
- 왜 중요한가: Q5b 에서 부모(KERNELBASE)가 **어느 쪽**을 부르는지 볼 때, 마지막 줄(`ProcessFirstResume` 표시가 붙나)이 둘을 가르는 표지가 된다 (run 문서).

### 2.12 `KiStartUserThread +0x28` — 관찰과 코드가 맞았다

2.6 고리 4번의 근거다.

- `[관찰]` (run4) 콜스택에서 `PspUserThreadStartup` 바로 위가 `nt!KiStartUserThread+0x28`.
- `[정적]` (원본 바이트, objdump) `KiStartUserThread` 는 Ghidra 가 함수로 안 잡아서 장부(`ms/decomp`·`ms/disassemble`)에 **없다.** 그래서 결과서가 바이트를 직접 읽었다.
  - IRQL 을 1 로 올리고 (`MOV CR8,1`), 저장해 둔 시작 루틴을 [CFG](run9-create-user-process-해설.md#직접-호출과-간접-호출) 검사 함수(`nt+0x6b77f0`)를 거쳐 `+0x23` 에서 부른다 → 돌아올 자리 **`+0x28`**.
  - 위 `[관찰]` 과 **오프셋까지 같다.**
  - `[추측]` CFG 검사 함수는 콜스택에 안 보였으니 넘겨주기만 하는 모양이다.
- `[정적]` 새 스레드 첫 커널 스택에 `KiStartUserThread` 주소를 넣는 곳: `KiInitializeContextThread +0x1e1` `LEA` (`KeInitThread` 가 부름, flow.md 2.1).
- `[추측]` run11 의 "첫 출근 창구" 쪽지(`PspUserThreadStartup`, 시작 정보 묶음 `+0x8` 칸)가 `KiInitializeContextThread` 를 거쳐 이 "저장해 둔 시작 루틴" 이 된다. 사이 전달은 한 줄씩 안 따라갔다 (Q6·Q7).
- 비유: run11 에서 "첫 출근하면 이 창구부터" 쪽지를 인사 카드에 붙였다. 이번엔 직원이 **출근 첫 발을 떼는 곳**(`KiStartUserThread`)이 그 쪽지를 꺼내 들고 창구로 가는 장면을 바이트로 봤다.

### 2.13 정정 — "팻말이면 바로 일 못 한다" 의 모양

run 문서 "정정" 절이다.

- 무엇이: run12 바퀴에 **flow.md 2.2 `KeReadyThread` 절에만** 쓴 `[추측]` — "멈춤 팻말이 걸려 있으면 줄에 서도 바로 일하진 못한다."
- 틀린 건 아니다. 다만 **모양이 다르다.**
  - 전 그림: 줄에 서 있되 출근을 못 하고 서 있다.
  - 새 그림 `[추측]`: 줄에 서 있다가 **출근은 한다.** 커널 시작 코드 맨 앞에서 쪽지를 펼치고 빨간불 앞에서 잔다 (2.3 ~ 2.6).
- 왜 생겼나: run12 때는 `KeSuspendThread` 본문을 안 뽑아서, 팻말이 **어떤 장치인지** 몰랐다 ([run12 해설 2.10](run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나) "모른다"). "팻말" 이라는 비유가 "줄에서 못 나감" 처럼 읽힌 것이다.
- 고친 곳: flow.md 2.2 를 고쳐 썼다. run 문서에 쓴 문장이 아니라서 이전 run 문서는 그대로 둔다 (run 문서).
- 참고: 같은 문장이 [run9 해설 "스레드를 멈춘 채로 만들기"](run9-create-user-process-해설.md#스레드를-멈춘-채로-만들기) 에도 flow.md 인용으로 남아 있다. 그 해설은 그때 기준이라 고치지 않았다. **지금 그림은 이 글 2.3 ~ 2.6 쪽이다.**

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run12 뒤) | 후 (run13) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run12 | 정적 근거 **run9 ~ run13** |
| 두 그림 사이 설명 | 공백이 남아 있다. 누가·언제 풀어 주는지 모른다 | **nt 쪽만 코드로 채웠다.** 멈춰 두기 = APC + `SuspendEvent`, 풀어 주기 = `NtResumeThread` → … → `KiResumeThread` `[정적]`. 부모가 정말 부르는지는 Q5b |
| 범례 | 굵은 화살표 = 커널 → 유저 전환 | 굵은 화살표 = **커널 → 유저, 유저 → 커널 시스템 콜, 인터럽트.** 여러 개면 `(1)` `(2)` 가 시간 순서. `nt` 안 상자 = 같은 스레드에서 **따로 일어나는 단계** |
| 그림 1 (부모) | `KeSuspendThread` 상자에서 끝 | 아래에 `KiSuspendThread` → (1) `KiInsertQueueApc` / (2) `KiSignalThreadForApc` 추가. **KERNELBASE (추정)** 상자에서 `(1)` 시스템 콜(기존 진입 `KiSystemServiceCopyEnd` 로), `(2)` **나중 시스템 콜** → 새 묶음 "**(2) 풀어 주기**" (`NtResumeThread` → `PsMultiResumeThread` → `KeResumeThread` → `KiResumeThread` → `KiTryUnwaitThread`) + 노란 메모 "같은 무렵 test.exe 첫 스레드는 기다리는 중 (추측)" |
| 그림 2 (test.exe) | `PspUserThreadStartup` ==> "커널 → 유저 전환 (공백)" | `(1)` **APC 인터럽트** 갈래 새 묶음 "**(1) 멈춤 APC 처리 — 멈춰 둔 스레드일 때만**" (`KiApcInterrupt` 추정 → `KiDeliverApc` → `KiSchedulerApc` → `KeWaitForSingleObject`) + 노란 메모 (부모가 깨움, 추측). 전환 화살표는 `(2)` 가 됨 |
| 2.1 `KeInitThread` | `[공백]` 본문 안 봄 | **`[정적]`** `+0x152` 멈춤 APC, `+0x163` `SuspendEvent` 초록, `+0xed` `ApcQueueable`, `KiInitializeContextThread +0x1e1` 이 `KiStartUserThread` 를 첫 스택에. `+0x8` 칸 전달은 `[추측]` |
| 2.2 제목 | `[정적]` 일부, 나머지 `[공백]` | **nt 쪽 `[정적]`**, 부모 유저 쪽 `[공백]` |
| 2.2 앞머리 | — | 비유(쪽지·신호등·어깨 두드리기), 트리 세 장면, 필드 표, APC 한 줄 풀이 |
| 2.2 `NtCreateUserProcess` 안 | `[공백]` 누가 푸는지 → Q5 | nt 사슬은 **해결** → run13. 남은 `[공백]` 은 부모가 정말 부르나, 어느 쪽인가 → **Q5b** |
| 2.2 `KeSuspendThread` | 조건(flags bit0)과 자리만 | **`[정적]`** 하는 일 다섯 걸음 (2.4) |
| 2.2 새 절 | — | `KiSuspendThread`, 멈춤 APC 와 `SuspendEvent`, **멈춘 첫 스레드가 서는 곳**, `NtResumeThread`, `PsMultiResumeThread`, `KeResumeThread`, `KiResumeThread`, **run12 와 맞물림** (한 문단 `[추측]`) |
| 2.2 `KeReadyThread` | `[공백]` 실제로 서는 시점. `[추측]` "팻말이면 바로 일 못 한다" | 서는 곳 **해결** (`KiSchedulerApc +0xf7` `[정적]`, 시점은 `[추측]`). 문장 고쳐 씀 (2.13) |
| 2.3 새 절 `KiStartUserThread` | — | `[관찰]` run4 + **`[정적]`** (원본 바이트) `+0x28` 일치 |
| 2.3 `PspUserThreadStartup` | `[관찰]`·run11 `[정적]` | 추가 **`[정적]`** 첫 일 = `+0x22` IRQL 0, 그 뒤 세 호출. **`[추측]`** 멈춘 스레드면 여기서 잠듦 |
| 4장 공백 표 | 2.2 "누가·언제 풀어 주나 (Q5)", "실제로 서는 시점 — 못 정함", 2.3 "`KeInitThread` 가 주소를 어떻게 쓰나" | 해결·일부 해결 3줄 (nt 사슬, 서는 곳, 첫 스택). 새 줄: 배달 시점, `FUN_1406b2db3` 정체, `KiSuspendThread` 갈래·State, `ProcessFirstResume` 읽는 곳, `+0x8` 칸 전달 |
| 4.1 | — | 5줄 추가 (멈춰 두기 안쪽, 부모 시스템 콜, 풀어 주는 사슬, APC 갈래, 순서) |
| 5장 | run12 까지 | run13 줄, 요청서 번호 표에 run13 줄 |

태그로 보면 핵심 변화는 네 가지다:

- 멈춘 첫 스레드가 서는 곳: `[공백]` → **`[정적]`** (`KiDeliverApc +0x220` → `KiSchedulerApc +0xf7`). 언제 거기 가나는 `[추측]`.
- 풀어 주는 nt 사슬: `[공백]` → **`[정적]`**. 부모가 부르는지는 여전히 `[추측]` (Q5b).
- 멈춰 두기의 정체: "팻말" 이라는 말뿐 → **`[정적]`** (횟수 + 멈춤 APC + `SuspendEvent` 0).
- `KiStartUserThread +0x28`: `[관찰]` 만 → **`[관찰]` + `[정적]`** (원본 바이트).

### 큰 그림에서 어떻게 이어지나

- **두 그림 사이(2.2)의 다리가 커널 쪽에서는 놓였다.**
  - 그림 1(부모)의 끝 → "(2) 풀어 주기" 묶음 → 그림 2(test.exe)의 "(1) 멈춤 APC 처리" 묶음.
  - 두 그림은 여전히 화살표로 안 이어진다. **다른 스레드**라 호출 관계가 없기 때문이다. 대신 양쪽에 노란 메모("같은 무렵 다른 스레드에서")를 붙여 짝을 보였다.
- **run12 의 질문 둘이 답을 얻었다.**
  - "멈춘 스레드가 줄에 선 뒤 실제로 어디 서나" → `KiSchedulerApc +0xf7` `[정적]`.
  - "팻말을 누가 떼나" → 커널 쪽 사슬은 `[정적]`. 그걸 부르는 **부모 유저 쪽**만 남았다.
- **그림 2 의 출발점이 한 칸 더 선명해졌다.** `KiStartUserThread +0x28` 이 코드로도 맞았고, `PspUserThreadStartup` 의 첫 일(IRQL 0)이 보였다. "커널 → 유저 전환(공백)" 바로 앞까지 다가갔다.
- **다음 구간** (`plan.md`: 다음 항목 **Q5b**):
  - Q5b — KERNELBASE `CreateProcessInternalW` 가 `NtCreateUserProcess` 뒤에 `NtResumeThread`(또는 `NtAlertResumeThread`)를 부르나. KERNELBASE 분석은 이번 바퀴에 미리 걸어 뒀고 이미 끝났다 (결과서: 함수 6448개, `_index.tsv` 에 `CreateProcessInternalW`·`ResumeThread` 있음).
  - Q19 — 이번 그림 전체를 디버거로 확인 (아래 4절).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 부모(cmd.exe) 유저 쪽이 `NtCreateUserProcess` 뒤에 **정말** 풀어 주는 시스템 콜을 부르나, `NtResumeThread` 인가 `NtAlertResumeThread` 인가, 언제인가 | 이번엔 커널(ntoskrnl)만 봤다. 부르는 쪽은 KERNELBASE 다 | **Q5b** (정적, KERNELBASE) → 그다음 동적으로 두 시스템 콜 입구 중 어디 서나 |
| test.exe 때 `KeSuspendThread` 가 **정말 불리나** (flags bit0) — 이번 그림 전체의 전제 | 실행 때 값이다 | **Q19** (동적), Q39 |
| 첫 스레드가 **정말** `KiSchedulerApc +0xf2` 에서 잠드나, 아니면 경주로 그냥 지나가나 | 서는 자리는 코드로 봤지만, 실제로 섰는지·신호등 값은 실행 때 일이다 | **Q19** (동적): `nt!KiSchedulerApc` 입구에서 `dd @rcx+0x2e4 L1` (0 → 곧 기다림, 1 → 이미 풀림), `+0xf2` 에서 `k` |
| 쪽지가 **언제** 펼쳐지나 (`PspUserThreadStartup +0x22` 직후인가). `SwapContext` 조건의 CL 인자가 `WaitIrql` 인가 | 인터럽트가 실제로 들어오는 자리는 정적으로 못 정한다 | **Q19** (동적): `KiDeliverApc+0x220` 아래가 `PspUserThreadStartup+0x26` 근처인지 |
| **순서**: `PspUserThreadStartup` 입구·`+0x22`·`+0x9c`·`+0xae`, `KiSchedulerApc`, (부모) `NtResumeThread` 중 누가 먼저인가 | 두 스레드 사이 순서는 실행해 봐야 안다 | **Q19** (동적) |
| 새 스레드가 `KiSuspendThread` 의 **어느 갈래**를 타나 (`[추측]` `+0x1fb` 쪽), 그때 State 가 0 인가 | 갈래는 실행 때 조건, State 0 을 쓰는 줄은 못 찾았다 | 갈래는 **Q19** (동적: `+0x83`·`+0x1f6` 에 서나). State 는 flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| 값이 바뀌는 순간 (`+0x284` 0→1→0, `+0x2e4` 1→0→1) | 실행 때 값이다 | **Q19** (동적) |
| `KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 이 정말 `KiApcInterrupt` 인가 | Ghidra 가 떨어진 조각으로만 잡았다 | **Q40** (정적) |
| `Flags3.ProcessFirstResume` (`_EPROCESS +0x5fc` bit 15) 를 **누가 읽나** | 이번 범위 밖 | **Q40** (정적) |
| `PspUserThreadStartup` 주소(시작 정보 `+0x8` 칸)가 `KiInitializeContextThread` 를 거쳐 첫 스택까지 가는 길 `[추측]` | 사이 전달을 한 줄씩 안 따라갔다 | **Q6·Q7** (정적) |
| `PspUserThreadStartup` 뒤 세 호출(`PspDisablePrimaryTokenExchange`·`PspNotifyThreadCreation`·`PspInitializeThunkContext`)이 하는 일 | 이번엔 자리만 봤다 | **Q6·Q7** (정적) |
| `PsResumeThread` 를 누가 부르나 (`[추측]` 드라이버용 내보내기) | 바이너리 안에 `CALL` 이 없다 | 대기열에 따로 줄은 없다 |
| 가벼운 멈춤의 번호표 상태 숫자(4, 5, 6) 이름 `[추측]` | PDB 에 열거형이 없다 | 대기열에 따로 줄은 없다 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run13-resume-thread.md`](run13-resume-thread.md)
- 정적 요청서: [`_workspace/sast/08_resume-thread.md`](../../_workspace/sast/08_resume-thread.md)
- 정적 결과서 (필드 표 전체, 질문별 답, 3-b 고리 8개, `NtAlertResumeThread` 비교, 동적 확인 목록, KERNELBASE 준비 상태): [`_workspace/sast/08_resume-thread_result.md`](../../_workspace/sast/08_resume-thread_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/`](../01_sprint/) run4 (`KiStartUserThread+0x28` 콜스택)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "스레드를 멈춘 채로 만들기", "한 주소에 이름 여럿 ICF 별칭", 3.6 멈춤 갈림길
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.5 `PspUserThreadStartup` 주소 건네기
  - [`run12-insert-thread-해설.md`](run12-insert-thread-해설.md) — 2.10 멈춘 직원도 대기 줄에 선다
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 범례 (굵은 화살표, `nt` 안 상자)
  - "그림 1" 의 "(2) 풀어 주기" 묶음, "그림 2" 의 "(1) 멈춤 APC 처리" 묶음
  - "2.1" 의 `KeInitThread` 절
  - "2.2" 전체 — 특히 "멈춘 첫 스레드가 서는 곳", "run12 `KeReadyThread` 와 맞물림"
  - "2.3" 의 `KiStartUserThread`, `PspUserThreadStartup` 절
  - "4. 공백 목록", "4.1 추측·정적으로 둔 호출 관계"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q5a, Q5b, Q19, Q40
