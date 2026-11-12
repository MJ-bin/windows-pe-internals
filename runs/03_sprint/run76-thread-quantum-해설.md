# run76 thread-quantum — 해설

> 이 글은 [run76 문서](run76-thread-quantum.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q141b2**: Q141b 의 둘째 조각 — 퀀텀 (PE_emulator 요청 25 ㄴ-2) (`_workspace/plan.md`, 이번에 **끝**)
  - 첫 조각 Q141b1 (넘기기) 은 run75 에서 끝났다.
  - 다음 항목: **Q141c1** (깨움) → Q141c2 (타이머) → Q141d → Q141e → Q142. 그다음 **Q104 VM 단계** 앞에서 멈춘다 (사용자 결정 2026-11-12 "vm 제외하고 Q142 까지").
  - 동적 몫 다섯은 **Q104** 에 붙였다. 작은 정적 다섯은 새 **Q144** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run76-thread-quantum.md`](run76-thread-quantum.md) (가장 중요)
  - 요청서 [`_workspace/sast/72_thread-quantum.md`](../../_workspace/sast/72_thread-quantum.md) · 결과서 [`72_thread-quantum_result.md`](../../_workspace/sast/72_thread-quantum_result.md) (판정 **분석성공**. 정적으로 못 닫은 셋 — 시계 인터럽트가 지나는 입구, 전역 실제 값, `+0x38b9` 이름)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.2 제목과 `KeStartThread` 절 한 줄 · 3장 "스레드 넘기기" 절 세 줄 + 새 절 "퀀텀" · 4장 run75 줄 둘 고침 + run76 새 줄 넷 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): 3.14 머리 퀀텀 줄 · `ExpectedRunTime` 근거만 (새 짐 칸 없음)
  - run75 문서: 맨 위에 정정 한 줄
- 이번 바퀴는 **정적만**이다. 값 대조는 run44 때 찍어 둔 **메인 스레드** 사진 일곱 장으로만 했다 (flow-writer, 새 실행 없음).
- **이번 바퀴에서 가장 조심할 곳**: 퀀텀 길이를 **두 가지로** 읽었다 (2.16).
  - 결과서 읽기와 flow-writer 읽기. **둘 다 `[추측: 계산]`** 이고, 아직 어느 쪽도 확정이 아니다.
  - Q104 에서 전역 값 몇 개를 찍으면 갈린다.

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- `_KPROCESS` = 가게 **운영 카드**, 우선순위 = **줄 서는 순번 등급**, 퀀텀 = **모래시계** → [run49 해설 "새 말: 우선순위와 퀀텀"](run49-kprocess-token-htable-해설.md#새-말-우선순위와-퀀텀)
- CPU = **계산대**, `_KPRCB` = 계산대마다 붙은 **계산대 장부**, 공개 PDB 의 `_KPRCB` 가 `+0x6d0` 에서 잘린 이야기 → [run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸)
- `NextThread` = **"다음 차례" 자리** (Standby) → [run75 해설 2.4](run75-thread-switch-해설.md)
- 준비 줄 32개 · **전광판** · **배정표** (다시 고르기 문맥) → [run75 해설 2.5](run75-thread-switch-해설.md)
- Idle 스레드 = **자리 지킴이** → [run75 해설 2.6](run75-thread-switch-해설.md)
- `KiSwapContext` → `SwapContext` = 계산대 **교대**, `call` 이 "한참 뒤에" 돌아오는 이유 → [run75 해설 2.8](run75-thread-switch-해설.md)
- 줄에 다시 세우기 (기본 맨 뒤, **"자리 뺏김" 도장** + 몫 남음이면 맨 앞), 선점 → [run75 해설 2.10](run75-thread-switch-해설.md#210-줄에-다시-세우기--기본-맨-뒤-선점당했고-몫이-남은-직원만-맨-앞)
- `PartialYield` · `FullYield` (배정표 칸 플래그 — 누구까지 받나) → [run75 해설 2.11](run75-thread-switch-해설.md#211-양보--ntyieldexecution--keyieldexecution0)
- IRQL = 직원 머리 위 **"방해 금지" 단계 표지판**, 소프트웨어 인터럽트 = **초인종 예약**, APC = 직원 책상 **쪽지** → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- `KeStartThread` (새 스레드를 가게에 다는 쪽) → [run12 해설 2.9](../02_sprint/run12-insert-thread-해설.md#29-kestartthread-와-kereadythread-는-뭐가-다른가)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)
- 사진 일곱 장 (메인 스레드 업무 카드) → [run75 해설 2.12](run75-thread-switch-해설.md#212-사진과-맞춰-보기--메인-스레드-업무-카드-일곱-장)

**비유 한 가지 주의 — 모래시계 그림을 조금 고친다.**

- run49 · run75 해설은 퀀텀을 **모래시계** 라고 했다. 모래가 위에서 **줄어드는** 그림이다.
- 이번에 코드를 보니 퀀텀은 "줄어드는 남은 양" 이 아니었다 (2.5). 그래서 그림을 이렇게 고쳐서 이어 쓴다.
  - **뒤집지 않는 모래시계.** 직원마다 하나씩 있다.
  - 직원이 계산대에 서 있는 동안 아랫칸에 모래가 **쌓인다**. 이게 `CycleTime` 이다.
  - 유리벽에 **눈금** 이 하나 그어져 있다. 이게 `QuantumTarget` 이다.
  - 쌓인 모래가 눈금에 닿으면 이번 몫이 끝난다.
  - 몫을 새로 줄 때는 모래를 쏟지 않는다. 지금 모래 높이 위로 **눈금을 새로 긋는다** (2.11).
- flow.md 3장 새 절 "퀀텀" 도 같은 그림 ("눈금", "벽시계가 째깍", "초인종 예약") 을 쓴다. 이 글도 그 말을 그대로 쓴다.
- flow.md 3장 "스레드 넘기기" 절 비유도 이번에 "손님" 에서 **"직원"** 으로 바뀌었다. 이제 해설과 flow.md 가 같은 그림이다.

### 한 줄로 먼저

- 퀀텀은 **깎지 않는다** `[정적]`.
  - 직원마다 쌓이는 CPU 사이클 (`CycleTime`) 이 눈금 (`QuantumTarget`) **이상** 이 되면 끝이다.
- "끝났나" 를 보는 건 **벽시계가 째깍할 때만** (시계 인터럽트, 2.4) 이다. 그때는 **보고 깃발만 꽂는다** (`KiUpdateRunTime`) `[정적]`.
- 실제 일은 그다음 **초인종** (디스패치 인터럽트, 2.7) 이 울릴 때 `KiDispatchInterrupt` → `KiQuantumEnd` 가 한다 `[정적]`.
  - 순서: 가산점 (부스트, 2.10) 한 칸 떼기 → 눈금 새로 긋기 → 지금 등급 **이상** 에서 다음 직원 찾기 → 없으면 **그대로 계속**, 있으면 옛 직원을 줄 **맨 뒤** 에 세우고 교대.
- PE_emulator 의 "명령 `0x10000` 개마다 `KiQuantumEnd` → 목록의 다음 스레드" 는 **틀림** `[정적]`.
- run75 의 짐작 "퀀텀 끝은 `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption` 쪽" 은 **반만 맞음** `[정적]`.
  - 그 둘은 선점 길이다. 퀀텀 끝은 `KiQuantumEnd +0xbd6` 이 교대를 **직접** 부른다.
- 사진 (메인 스레드):
  - 첫 눈금 `0x7f2814` → 눈금 한 칸 = **4,166,666 사이클** `[관찰 값, 계산]`.
  - P1b 에서 눈금이 새로 그어졌다 `[관찰]`. `0x24` 칸으로 다시 그은 것과 맞는다 `[추측: 계산]`.
- 그런데 **"눈금 한 칸이 벽시계 째깍 몇 분의 몇이냐"** 는 읽기가 둘이다 (째깍의 1/3 vs 1/18). 둘 다 `[추측: 계산]`, Q104 에서 가른다 (2.16).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 만들기               (run9 ~ run12)
 └─ PspInsertThread → KeStartThread      업무 카드에 첫 눈금 적기             (run12, 이번에 한 줄 더)
 └─ PspInsertThread +0x5a1 KeReadyThread  출근 대기 줄에 세운다               (run12)
      ⋮  계산대 주고받기 — 기다림으로 내놓기 (KiSwapThread)                 (run75)
      ⋮                  — 양보 (NtYieldExecution)                          (run75)
      ⋮                  — 몫이 다 돼서 내놓기 (KiQuantumEnd)               ← 이번
      ⋮                  — 깨움 · 타이머                                    (Q141c, 다음)
 └─ SwapContext ─RET─▶ KiStartUserThread → … → main                         (run15)
```

- 이번 주제도 run75 처럼 지도의 **한 단계가 아니다.**
  - 모든 스레드가 초기화 내내 몇 번이고 다시 타는 길이다.
  - 그래서 flow.md 그림엔 안 넣고, 3장에 절 하나 ("퀀텀") 로 적었다.
- 지도와 맞물리는 곳은 둘이다.
  - **`KeStartThread`**: 새 스레드의 **첫 눈금** 을 여기서 긋는다. flow.md 2.2 `KeStartThread` 절에 한 줄이 붙었다 (2.6).
  - **교대를 누가 부르나**: run75 에서 "기다림 (`KiSwapThread`)·양보 (`KeYieldExecution`) 가 부른다" 까지 봤다. 이번에 **몫 끝 (`KiQuantumEnd`)** 과 선점 길이 더해졌다 (2.14).

### 무엇이 궁금했나

한 문장으로: **계산대에 선 직원의 몫 (퀀텀) 이 언제·어떻게 끝나고, 끝나면 무슨 일이 일어나나.**

- 계기: `../PE_emulator` 의 nt 엔진은 지금 "명령 `0x10000` 개마다 `KiQuantumEnd` 를 부르고 목록의 다음 실행 가능 스레드로 넘긴다. 우선순위 없음" 이다 `[추측]` (요청서). 진짜에 맞춰 달라는 요청 25 ㄴ-2.
- 요청서가 짐작한 사슬 `[추측]`: `KeClockInterruptNotify → KiUpdateRunTime → (퀀텀 끝 표시) → KiDispatchInterrupt → KiQuantumEnd`.
- 질문은 여섯 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | 시계 사슬 — `KeClockInterruptNotify` 가 `KiUpdateRunTime` 을 부르는 자리. 중간에 `KiUpdateTime`·`KeAccumulateTicks` 를 거치나 | 2.4 |
| 2 | 퀀텀 깎는 자리 — 어느 칸을 무엇과 비교하나, 명령 수냐 CPU 사이클이냐, 한 퀀텀 길이 | 2.5 · 2.6 · 2.16 |
| 3 | `QuantumEnd` 켜는 자리, 소프트웨어 인터럽트를 요청하는 줄 | 2.7 · 2.8 |
| 4 | `KiDispatchInterrupt → KiQuantumEnd` — 부르는 자리, 표시를 읽고 지우는 줄 | 2.9 |
| 5 | `KiQuantumEnd` — (a) 다시 채우기 (b) 우선순위 내리기 (c) 다음 고르기·없으면 계속? (d) 맨 뒤? (e) 바꾸는 줄 | 2.10 ~ 2.13 |
| 6 | `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption` 은 이 사슬 위에 있나 | 2.14 |

### 바퀴 전에 알던 것

- `[정적]` run52: `KiAbProcessPreContextSwitch` 를 부르는 곳에 `KiQuantumEnd`·`KiDispatchInterrupt`·`KiCheckForThreadDispatch`·`KiHandleDeferredPreemption` 이 있다. 직원이 계산대에서 내려가는 때들이다.
- `[정적]` run52 표: `PriorityDecrement` 를 쓰는 곳에 `KiQuantumEnd` 가 있다.
- `[정적]` run75: 줄에 다시 세우는 곳은 `KiDeferredReadySingleThread +0x1726 / +0x172a`. 기본 맨 뒤, "자리 뺏김" 도장 + 몫 남음이면 맨 앞. 몫 비교 줄은 `+0x144f CMP R13,[RBX+0x20]`.
- `[정적]` run75 지도: `KiSwapThread` 를 안 거치고 `KiCheckForThreadDispatch +0x10e`·`KiHandleDeferredPreemption +0x297` 에서 `KiSwapContext` 를 직접 부르는 길이 있다. 이게 **퀀텀 끝 길인지** 는 `[추측]` 이었다.
- `[관찰]` run75 사진 (메인 스레드): `QuantumReset 2`, `QuantumTarget` K3 ~ P1a `0x7f2814` · P1b ~ P2 `0x9fa7208` (run75 문서 표), `Priority` 8 → 9 (P1b ~) — 9 는 `[추측: 부스트]`.
- 요청서 메모: PE_emulator 쪽에선 `KiQuantumEnd` 가 디컴파일 색인에만 있고 `.asm` 이 없었다 → 이번에 뽑았다.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 번호 `(n)` 은 run 문서 번호 그대로. ★ 는 이 글의 절.

```
[벽시계 째깍 — 시계 인터럽트 하나]   (어느 인터럽트 입구를 지나는지는 [추측] → Q104)
HalpTimerClockInterrupt
 └─ (1) +0x108  KeClockInterruptNotify                  PendingTick 이 서 있을 때만 (+0xe0 / +0xe4)
      ├─ (1) +0x2bf  KiUpdateTime → +0x390 KiUpdateRunTime        시계 주인 CPU            ★ 2.4
      └─ (2) +0x44d  KiUpdateRunTime                              그 밖 CPU
           ├─ (1) +0x9b   KeAccumulateTicks                       곁다리 — 퀀텀 칸 안 만짐
           │  +0xb8 ~ +0xc0     지금 직원 == 자리 지킴이면 건너뜀                         ★ 2.5
           │  +0x23c ~ +0x244   CycleTime (+0x48) ≥ QuantumTarget (+0x20) ?
           │  +0x16a            PRCB+0x38b9 = 1      [추측: 이름 QuantumEnd]              ★ 2.7
           │  +0x17b            InterruptRequest (+0x6) = 1   (NestingLevel ≠ 0, 보통)
           └─ (2) +0x4ad  HalpInterruptSendIpi (EDX = 0x2f)       NestingLevel == 0 일 때

[인터럽트 끝]  예: KiInterruptDispatchNoLock                                           ★ 2.8
 ├─ (3) +0x5b   KiEndInterruptCycleAccumulation   +0x246 읽기 · +0x250 지우기 · 이전 IRQL < 2 → AL = 1
 └─ (4) +0x64   KiDpcInterruptBypass
      └─ +0x1b  KiDispatchInterrupt
(또는 벡터 0x2f → KiDpcInterrupt +0x39f KiDispatchInterrupt)

KiDispatchInterrupt                                                                  ★ 2.9
 ├─ (1) +0x65   KiSwapStacksAndRetireDpcList          DPC 먼저
 │  +0x7f ~ +0x88 · +0x1bc   깃발 읽고 지움
 ├─ (2) +0x1c8  KiQuantumEnd                          → +0x1d2 RET (NextThread 안 봄)
 └─ (깃발 0 + NextThread 있음) (3) +0x152 KiQueueReadyThread · (4) +0x176 KiSwapContext   선점 ★ 2.14

KiQuantumEnd           (부르는 곳: KiDispatchInterrupt +0x1c3 · KiIdleLoop +0xaf)
 │  +0x7d / +0x81 (또는 +0xc7f / +0xc83)   정말 끝났나 다시 — 아니면 +0x1c9            ★ 2.10
 ├─ (1) +0x37a  KiComputeNewPriorityNonStacking   (켜짐이면 +0x656 인라인 → +0x71a)    가산점 떼기
 ├─ (2) +0x778  KiUpdateThreadPriority
 ├─ (3) +0x46f  KiQueryQuantumReset    → +0x49b 새 눈금 = CycleTime + 단위 수 × 단위   ★ 2.11
 ├─ (4) +0x616  KiSearchForNewThreadsForRescheduleContext   (PartialYield)            ★ 2.12
 ├─ (5) +0x628  KiCommitRescheduleContext
 │  +0x2a1 / +0x2a9  NextThread 없으면 → +0x30e RET (그대로 계속)
 │  +0xb67 Preempted = 0 · +0xb98 WaitReason 0x1e                                    ★ 2.13
 ├─ (8) +0xbad  KiQueueReadyThread(옛)       → 맨 뒤 (run75)
 └─ (9) +0xbdb  KiSwapContext(옛, 새, 1)     → SwapContext (run75)

(모래가 쌓이는 곳)  KiEndThreadCycleAccumulation +0x1bc · KiCaptureTotalCyclesCurrentThread +0x1d8   ★ 2.5
(첫 눈금)          KeStartThread +0x1c7 / +0x1dc                                                  ★ 2.6
(눈금 한 칸, 부팅)  KeInitSystem +0x2f3 (플래그면 +0x314) → KiCyclesPerClockQuantum               ★ 2.6 · 2.16
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- 이번에 **처음 뽑은 함수가 23개**다. 새 지도 `_map/KeClockInterruptNotify_*` (함수 667개) 도 만들었다.
- 그런데 **`KiDispatchInterrupt`·`KiQuantumEnd` 는 그 지도에 없다.**
  - 깃발 → `KiDispatchInterrupt` 사이가 함수 호출이 아니라 "소프트웨어 인터럽트 + 인터럽트 끝 처리" 로 이어지기 때문이다 (2.8).
  - 지도는 호출만 따라간다. 그래서 결과서가 이음새 함수를 **이름으로** 따로 뽑아 이었다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ (2) +0x1c8 KiQuantumEnd` (`KiDispatchInterrupt` 아래)

```
KiDispatchInterrupt +0x7f   MOVZX EAX,byte ptr [RBX + 0x38b9]
KiDispatchInterrupt +0x1bc  MOV byte ptr [RBX + 0x38b9],0x0
KiDispatchInterrupt +0x1c3  CALL KiQuantumEnd          (ret +0x1c8)
```

- **누가**: `KiDispatchInterrupt`. 디스패치 인터럽트 (초인종, 2.7) 가 울리면 도는 함수다.
  - `RBX` = 계산대 장부 (`+0x3a` 에서 `gs:[0x20]` 으로 잡음).
- **어디서**: 부르는 자리 `+0x1c3`, 돌아올 자리 `+0x1c8`.
  - 멈추면 콜스택에 `nt!KiDispatchInterrupt+0x1c8` 로 보일 자리다. 아직 안 봤다.
  - 바로 앞 `+0x7f` 에서 장부의 `+0x38b9` 칸 (**"몫 끝" 깃발**) 을 읽고, 서 있으면 `+0x1bc` 에서 **0 으로 지운다.**
- **누구를**: `KiQuantumEnd` — 몫 끝 처리 담당.
- **무엇을 위해**: 벽시계 째깍 때 꽂아 둔 깃발을 보고, 실제 몫 끝 처리를 한다.
  - 돌아오면 `+0x1d2 RET` 로 바로 끝난다. 이 뒤로 "다음 차례" 자리를 **안 본다** `[정적]`.

#### 줄 2: `KiUpdateRunTime +0x23c ~ +0x244` — 명령 세 줄

호출이 아니라 명령 세 줄이다. 그런데 "퀀텀 깎는 자리가 어디냐" 의 답이 여기 있다.

```
KiUpdateRunTime +0x23c  MOV RAX,qword ptr [RSI + 0x48]
KiUpdateRunTime +0x240  CMP RAX,qword ptr [RSI + 0x20]
KiUpdateRunTime +0x244  JNC +0x16a
```

- **`RSI` 에 든 것**: 지금 계산대에 선 직원의 업무 카드. `+0xb8 MOV RSI,[RBX+0x8]` (`CurrentThread`) 에서 잡았다.
- **`+0x23c`**: 업무 카드 `+0x48` (`CycleTime`, 쌓인 모래) 를 읽는다.
- **`+0x240`**: 업무 카드 `+0x20` (`QuantumTarget`, 눈금) 과 비교한다.
- **`+0x244 JNC`**: 같거나 크면 `+0x16a` (깃발 꽂기, 2.7) 로 뛴다. 둘 다 64비트.
- **그래서**: "쌓인 사이클 ≥ 눈금" 이면 몫 끝 `[정적]`.
- 눈여겨볼 것: **빼는 줄이 없다.** "남은 몫 − 1" 같은 게 아니라 **비교만** 한다. 이게 2.5 의 "깎지 않는다" 다.
- 비유: 벽시계가 째깍하면 계산대 장부 담당이 직원 모래시계를 **힐끗 본다.** 모래가 눈금에 닿았으면 깃발.

### 2.3 무대 — 이번에 쓰는 칸

뒤에서 오프셋이 나오면 이 표로 돌아오면 된다.

#### 계산대 장부 (`_KPRCB`)

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x6` | `InterruptRequest` | "인터럽트 끝나면 초인종" **메모** (2.7) |
| `+0x7` | `IdleHalt` | (2.8 에 한 번) |
| `+0x8` | `CurrentThread` | 지금 선 직원 |
| `+0x10` | `NextThread` | "다음 차례" 자리 |
| `+0x18` | `IdleThread` | 자리 지킴이 |
| `+0x20` | `NestingLevel` | 인터럽트 처리 중 표시 (0 이면 아님) |
| `+0x21` | `ClockOwner` | **벽시계 당번** 표시 (2.4) |
| `+0x22` 비트 0 | `PendingTick` | 째깍 대기 표시 |
| `+0x44` | `MHz` | 1µs 동안의 사이클 수 (2.6) |
| `+0x38b9` | (이름 없음) `[추측: QuantumEnd]` | **"몫 끝" 깃발** |
| `+0x38bc` | (이름 없음) `[추측: DPC 요청 플래그]` | 잡무 바구니 표시 (2.9) |

- 이름 있는 칸은 PDB 이름이다. `+0x38b9` · `+0x38bc` 는 공개 PDB 의 `_KPRCB` 가 `+0x6d0` 에서 잘려서 **이름이 없다.** 오프셋·쓰임은 `[정적]`, 붙인 이름은 `[추측: 이름]`.

#### 업무 카드 (`_KTHREAD`)

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x20` | `QuantumTarget` | **눈금** |
| `+0x48` | `CycleTime` | **쌓인 모래** (쓴 CPU 사이클) |
| `+0x78` | `ThreadFlags` | 비트 5 `QuantumDonation` · 비트 8 `DisableQuantum` · 비트 12 `ForceDeferSchedule` |
| `+0xc3` | `Priority` | 지금 등급 (우선순위) |
| `+0x204` | `BamQosLevel` | QoS 단계 (2.11) |
| `+0x206` | `PriorityDecrement` | 낮은 4비트 = `ForegroundBoost` (2.10) |
| `+0x233` | `BasePriority` | 기본 등급 |
| `+0x235` | `Preempted` | "자리 뺏김" 도장 |
| `+0x283` | `WaitReason` | **내려간 사유 번호** (2.13) |
| `+0x28b` | `QuantumReset` | 눈금 간격 (몇 칸) |
| `+0x366` | `DecayBoost` | 가산점 (2.10) |

- 운영 카드 (`_KPROCESS`) 는 `+0x91 QuantumReset` 하나만 나온다. 이 가게 직원들의 **기본 눈금 간격**이다.

### 2.4 벽시계 째깍 — 시계 인터럽트에서 `KiUpdateRunTime` 까지 (질문 1)

#### 새 말 — 시계 인터럽트, 틱

- **시계 인터럽트** = 하드웨어 타이머가 정해진 간격마다 CPU 를 두드리는 인터럽트로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번에 찾은 사슬의 맨 앞 함수가 `HalpTimerClockInterrupt` 다 `[정적]`.
- **틱** = 시계 인터럽트 한 번. 그 간격을 커널은 `KeMaximumIncrement` (100ns 단위) 로 쓴다 (run 문서의 `KeInitSystem` 식 풀이, 2.6).
- 비유: 가게 **벽시계가 째깍** 하는 것. 한 번 째깍 = 한 틱.

#### 사슬 — 두 갈래 `[정적]`

1. `HalpTimerClockInterrupt +0xe0 TEST [PRCB+0x22],1` · `+0xe4 JNZ +0xf9` — 째깍 대기 표시 (`PendingTick`) 가 서 있을 때만 → `+0x103` (ret `+0x108`) **`KeClockInterruptNotify`**.
   - 셋째 인자 `R8D = 0` (`+0xf9`). 인자 이름은 `[공백]`.
2. `KeClockInterruptNotify +0x259 CMP [RDI+0x21],BL` (`ClockOwner`) · `+0x25c JZ +0x424`:
   - **0 이 아니면 (시계 주인 CPU)** → `+0x2ba` (ret `+0x2bf`) **`KiUpdateTime`** → 시스템 시간 갱신 뒤 → `+0x38b` (ret `+0x390`) **`KiUpdateRunTime`**.
     - CPU 가 둘 이상 (`+0x375` · `+0x378 JA +0x42f`) 이고 `KiClockTimerPerCpuTickScheduling == 0` 이면, 먼저 `+0x46a KiForwardTick` (다른 CPU 에 틱 넘기기, 이름만) 을 하고 `+0x48e JMP +0x37e` 로 와서 같은 `KiUpdateRunTime` 에 닿는다.
   - **0 이면 (그 밖 CPU)** → `+0x424` · `+0x4d7` · `+0x434 RtlGetInterruptTimePrecise` 를 거쳐 → `+0x448` (ret `+0x44d`) **`KiUpdateRunTime`**.
- `HalpTimerClockIpiRoutine +0x1f` 도 `KeClockInterruptNotify` 를 부른다 (refs). 주인 CPU 의 `KiForwardTick` 이 그쪽으로 이어진다는 건 `[추측]` (`KiForwardTick` 은 안 읽음).

#### 새 말 — 시계 주인 CPU (`ClockOwner`)

- 계산대 장부 `+0x21`. 이 값이 0 이 아닌 CPU 만 `KiUpdateTime` (시스템 시간 갱신) 을 거친다 `[정적]`.
- 비유: 계산대 여럿 가운데 **벽시계 당번** 하나. 당번은 가게 시계 (시스템 시간) 까지 맞추고, 나머지 계산대는 곧장 자기 직원 모래시계만 본다.
- 어느 쪽이든 결국 **`KiUpdateRunTime`** 에 닿는다. 몫 검사는 거기서 한다.
- test.exe VM 은 CPU 가 **2개** `[관찰]` (run75). 그래서 당번 쪽에서 `KiForwardTick` 갈래를 탈 것이다 (run 문서 동적 2). 실행으로는 안 봤다.

#### `KeAccumulateTicks` — 중간 다리가 아니라 곁다리 `[정적]`

요청서는 "중간에 `KiUpdateTime`·`KeAccumulateTicks` 를 거치나" 를 물었다.

- `KiUpdateTime` — **거친다** (시계 주인 CPU 만).
- `KeAccumulateTicks` — 사슬 중간이 아니다. `KiUpdateRunTime +0x96` (ret `+0x9b`) 이 **조건 없이** 부르는 곁다리다.
  - 하는 일: 틱 장부 (`PRCB+0x8644` 등) · 스레드 틱 칸 · DPC 감시 · `+0x28 KiCheckForTimerExpiration` (타이머 만료 → Q141c2).
  - **퀀텀 칸 (`+0x20`·`+0x48`·`+0x28b`) 은 안 만진다** (결과서가 `grep` 으로 확인).
- 비유: 째깍마다 하는 **출근부 기록**. 모래시계와는 따로 논다.

### 2.5 퀀텀은 깎지 않는다 — 쌓이는 모래와 눈금 (질문 2)

#### 요청서의 "퀀텀 깎는 자리" → 깎는 자리가 없다 `[정적]`

- 2.2 줄 2 그대로다. 비교만 있고, 빼는 줄이 없다.
- 퀀텀은 **목표선** 이다. `CycleTime ≥ QuantumTarget` 이면 끝.
- 재는 단위는 **명령 수가 아니라 CPU 사이클** 이다.

#### 새 말 — CPU 사이클

- CPU 안의 시계가 한 번 뛰는 **박자** 로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드에서 보이는 것:
  - 계산대 장부 `MHz (+0x44)` = 1µs 동안의 사이클 수로 쓰인다 (run 문서의 식 풀이).
  - `CycleTime` 은 그 사이클을 **더해 간다** `[정적]` (아래).
- 비유: 계산대 안의 아주 빠른 **메트로놈 박자**. 모래 한 알 = 박자 하나.
- 명령 하나가 박자 몇 개인지는 명령마다 다르다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 그래서 에뮬레이터가 "명령 수" 로 세는 건 **대용품** 이다 — 결과서도 "비례로 대신해도 된다 `[추측]`" 고 적었다.

#### 자리 지킴이는 검사 안 함 `[정적]`

- `+0xb8 MOV RSI,[RBX+0x8]` · `+0xbc CMP RSI,[RBX+0x18]` · `+0xc0 JNZ +0x226` — 지금 직원이 자리 지킴이가 **아닐 때만** 검사로 간다.
- 비유: 자리 지킴이 모래시계는 안 본다.

#### 모래는 누가 쌓나 — `KiUpdateRunTime` 은 읽기만 한다 `[정적]`

| 언제 | 줄 | 하는 일 |
|---|---|---|
| 인터럽트 들어올 때 | `KiInterruptSubDispatch* +0x33` → `KiStartInterruptCycleAccumulation` → `+0x21` (ret `+0x26`) `KiEndThreadCycleAccumulation` → **`+0x1b5 ~ +0x1bc`** | `CycleTime += 지난 구간` (같은 함수 `+0x20` 에서 `NestingLevel = 1`) |
| 직원 바뀔 때 | `KiCaptureTotalCyclesCurrentThread +0x1d1 ~ +0x1d8` | 같은 더하기. 부르는 곳: `KiSwapThread +0x40` · `KiQuantumEnd +0xb85` · `KiDispatchInterrupt +0xae` · `KeYieldExecution +0x1e9` 등 |

- 그러니 그림은 이렇다: 째깍 (인터럽트) 이 들어오는 순간 그때까지의 박자가 모래로 부어지고, 그다음 `KiUpdateRunTime` 이 눈금과 비교한다.
  - 다만 **시계 인터럽트가 이 입구 (`KiInterruptDispatch*`) 를 정말 지나는지** 는 IDT 를 안 읽어서 `[추측]` 이다 → Q104.
- 비유: 직원이 계산대에 선 동안 흐른 박자를, 비상 호출 (인터럽트) 이 들어오거나 교대할 때 **한꺼번에 모래로 부어 넣는다.**

#### 사진도 그렇다 `[관찰]`

- 메인 스레드 `CycleTime`: K3 0 → P0 24,000 → P1 3,082,560 → P1a 7,587,360 → P1b 17,517,600 → P2 21,113,760. **늘기만 한다.**
- 같은 동안 K3 ~ P1a 눈금은 `0x7f2814` 그대로다. 퀀텀은 **스레드마다 따로**, 사이클로 쌓인다 (결과서).

#### 몫이 안 끝났어도 깃발로 가는 길 (줄기 밖)

- QoS 단계가 바뀜 (`+0x24a ~ +0x25b`), 헤테로 코어 (`+0x26d`), 스케줄링 그룹 예산 (`+0x107 ~ +0x164`). 일반 스레드엔 해당 없음 `[추측]`.
- `+0x22f` · `+0x236` 은 퀀텀이 아니라 굶은 스레드 스캔 시간 확인이다 (이름만).

### 2.6 눈금 한 칸은 얼마 — 첫 눈금과 `KiCyclesPerClockQuantum` (질문 2 이어서)

#### 첫 눈금은 `KeStartThread` 가 긋는다 `[정적]`

1. `+0x1ba MOVZX EAX,[RDI+0x91]` — 운영 카드 (프로세스) 의 `QuantumReset` 을 읽는다.
2. `+0x1c1 MOV [RBX+0x28b],AL` — 업무 카드 `QuantumReset` 에 복사.
3. `+0x1c7 IMUL EAX,[KiCyclesPerClockQuantum]` — × 눈금 한 칸.
4. `+0x1dc MOV [RBX+0x20],RAX` — 눈금 (`QuantumTarget`) 에. `CycleTime` 은 0 이라 안 더한다.

- 식: **첫 눈금 = `QuantumReset` × 한 칸.**
- 프로세스 쪽 `QuantumReset` 의 뿌리 (`PspComputeQuantum` · `PspForegroundQuantum` 표) 는 이름만 봤다 → Q144 ④.

#### 사진으로 한 칸을 거꾸로 센다 `[관찰]` + 계산

- K2 → K3:
  - 업무 카드 `QuantumReset` 0 → **2** (운영 카드 값 2. 운영 카드는 K2 부터 2).
  - 눈금 0 → **`0x7f2814`** (8,333,332).
  - `CycleTime` 0.
- 위 `KeStartThread` 줄이 K2 ~ K3 사이에 탔다는 것과 맞다.
- 그래서 한 칸 = 8,333,332 / 2 = **4,166,666** (`0x3f940a`) `[관찰 값, 계산]`.
- 비유: 눈금 **두 칸** 짜리 모래시계. 한 칸 = 4,166,666 박자.

#### 한 칸은 부팅 때 `KeInitSystem` 이 정한다 `[정적]`

곱셈 상수를 풀어서 읽은 식이다 (결과서가 파이썬으로 풀고, flow-writer 가 다시 풀고, 오케스트레이터가 `.asm` 으로 대조했다).

1. `+0x2aa` `KeMaximumIncrement` × `+0x2b0` CPU 0 계산대 장부의 `MHz (+0x44)` (`+0x2b4 IMUL`) = **T**.
2. `+0x2b8 ~ +0x2c8` → **T/10** = 째깍 한 번 동안의 박자 수.
   - 째깍 간격이 100ns 단위이고 `MHz` 가 1µs 당 박자 수라서, 10 으로 나누면 째깍 한 번의 박자 수가 된다 (run 문서 풀이).
3. `+0x2cc ~ +0x2dd` → **T/2400** → `KiShortExecutionCycles` (flow-writer 보탬 — 결과서엔 없음. 2.16 에서 쓴다).
4. `+0x2e3 ~ +0x2e6` → **T/30** → `+0x2f3 KiCyclesPerClockQuantum`. 같은 값이 `KiDirectQuantumTarget` (`+0x2f9`), 3배가 `KiLockQuantumTarget` (`+0x302`) 로도 간다.
5. `+0x2e9 TEST [KiVelocityFlags],0x40000` → 서 있으면 (`+0x308 JZ` 를 안 타면) `+0x30a ~ +0x311` 에서 **6 으로 한 번 더 나눠** `+0x314` 에 다시 쓴다 (`+0x31d KiLockQuantumTarget` 은 2배로).

- 그래서 한 칸은 째깍의 **1/3** (플래그 꺼짐) 또는 **1/18** (켜짐) 이다.
- 비유: 가게 문 열 때 (부팅) 딱 한 번, "모래시계 눈금 한 칸 = 벽시계 째깍의 1/3" 로 정한다. 그런데 **속도 스위치** (`KiVelocityFlags` 비트 `0x40000`) 가 켜져 있으면 칸을 6배 더 잘게 (1/18) 판다.
- 이 VM 이 부팅할 때 스위치가 켜져 있었나? → **사진에 없다.** 2.16 에서 다룬다.

#### 이 한 칸을 곱하는 곳 15군데 `[정적]`

- `KeStartThread +0x1c7` · **`KiQuantumEnd +0x480`** · `KiDeferredReadySingleThread +0x247` · `KiQueueReadyThread +0x74e` · `KeYieldExecution +0x19b` · … (run 문서 목록).
- **쓰는** 곳은 `KeInitSystem +0x2f3` · `+0x314` 둘뿐이다.

### 2.7 째깍 때는 깃발만 — "몫 끝" 깃발과 초인종 (질문 3)

#### 깃발 꽂기 `[정적]`

- **`KiUpdateRunTime +0x16a MOV byte [RBX+0x38b9],1`** (`RBX` = 계산대 장부).
- 이름은 PDB 에 없다 → `[추측: QuantumEnd]`. 그렇게 부르는 근거 (결과서):
  - 몫 끝 (`+0x244 JNC +0x16a`) 일 때 켜진다.
  - `KiDispatchInterrupt` 가 `KiQuantumEnd` 를 부르기 바로 앞에서 읽고 지운다 (2.9).
  - 뽑은 `.asm` 467개 가운데 `0x38b9` 를 쓰는 곳이 이 둘뿐이다.
- 비유: 계산대 장부에 **"몫 끝" 깃발** 을 꽂는다.

#### 새 말 — 디스패치 인터럽트

- IRQL **2** (DISPATCH_LEVEL) 단계의 소프트웨어 인터럽트다. run13 해설의 **초인종** 가운데 "방해 금지 2단계" 짜리라고 보면 된다.
  - 결과서: 여기서 하는 일은 `KiRequestSoftwareInterrupt(Prcb, 2)` 를 펼쳐 쓴 꼴이다. "2" 가 그 단계다.
- 이 초인종이 울리면 `KiDispatchInterrupt` 가 돈다 `[정적]` (2.8 의 두 길). 단 시계 인터럽트가 실제로 어느 길로 가는지는 `[추측]`.
- 비유: 계산대의 **교대·잡무 초인종**. 울리면 잡무 (DPC, 2.9) 를 먼저 하고, 그다음 깃발을 본다.

#### 초인종 거는 법 — 두 갈래 `[정적]`

| 조건 | 줄 | 하는 일 | 비유 |
|---|---|---|---|
| `NestingLevel ≠ 0` — 인터럽트 처리 중 (**시계 인터럽트 안, 보통 이쪽**) | **`+0x17b MOV byte [RBX+0x6],1`** → `+0x17f JMP +0x294` (끝) | `InterruptRequest = 1` — "인터럽트 끝 처리 때 디스패치 하라" 표시 | "이 비상 호출 끝나면 초인종 울려 줘" **메모** |
| `NestingLevel == 0` (`+0x171` · `+0x175 JZ +0x433`) | `+0x498 MOV EDX,0x2f` → **`+0x4a8 HalpInterruptSendIpi`** | 자기 CPU 에 벡터 `0x2f` (DISPATCH_LEVEL) 인터럽트 | 초인종 버튼을 **직접 누름** |

- AMD 우회 갈래 (`+0x433 ~ +0x497`) 는 건너뛴다.
- **새 말 — 벡터, IPI**:
  - 벡터 = 인터럽트 **번호** 다 (`0x2f` 가 DISPATCH_LEVEL 번호, 결과서).
  - IPI = CPU 끼리 보내는 인터럽트로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 여기선 **자기 자신** 에게 보낸다.
- 왜 두 갈래인가 — 인터럽트 처리 중이면 그 처리가 끝날 때 메모를 어차피 확인하니 (2.8) 메모만 남기는 것으로 보인다 `[추측: 이 해설의 짐작 — 2.8 의 KiEndInterruptCycleAccumulation 이 그 메모를 읽는다는 것에 기댄 것]`.

#### 왜 여기서 바로 교대하지 않나

- 코드가 이유를 말해 주진 않는다.
- 보이는 것만 `[정적]`: 째깍 처리 (`KiUpdateRunTime`) 는 깃발·메모를 남기고 `+0x17f JMP +0x294` 로 **끝난다.** 실제 일은 초인종이 울린 뒤 `KiDispatchInterrupt` 에서 한다.
- 비유: 벽시계 당번은 "몫 끝" 깃발만 꽂고 자기 일 (째깍 처리) 을 마저 한다. 교대는 초인종이 울린 **다음** 이다.
- PE_emulator 쪽 (run 문서 표 3): "표시 → 나중에 디스패치" 두 단계가 없으면 **모자람**. 칸 하나 + "인터럽트 끝에서 읽기" 면 충분하다.

#### 째깍 사슬의 다른 초인종 (이름만)

- 퀀텀 말고도 DISPATCH 요청이 있다: `KeAccumulateTicks +0x171` · `KiUpdateRunTime +0x557 KiSetDpcRequestFlag` · `+0x561 KiRequestSoftwareInterrupt` (스캔) · `KiUpdateTime +0x4d0 KeInsertQueueDpc`.

### 2.8 깃발이 `KiDispatchInterrupt` 까지 가는 길 — 호출이 아니라 "인터럽트 끝 처리"

#### 왜 지도에 없나

- `run_ghidra.sh map` 지도는 **호출 (`CALL`)** 만 따라간다.
- `KiUpdateRunTime` 은 `KiDispatchInterrupt` 를 **부르지 않는다.** 메모를 남기고 끝날 뿐이다.
- 그 메모를 읽고 `KiDispatchInterrupt` 를 부르는 건 **인터럽트를 끝내는 쪽** 이다.
- 그래서 `KeClockInterruptNotify` 뿌리 지도 (667개) 에 `KiDispatchInterrupt`·`KiQuantumEnd` 가 없다. 결과서가 이음새 9개를 이름으로 따로 뽑아 이었다.
- 비유: 깃발을 꽂는 사람과 초인종에 답하는 사람이 **다르다.** "누가 누구를 부르나" 명부엔 둘 사이에 줄이 없다.

#### 이음새 — 한 걸음씩 `[정적]` (예시 입구 `KiInterruptDispatchNoLock`)

1. `+0x37 KiInterruptSubDispatchNoLock` (ISR 본체 — 이번 인터럽트의 원래 일) → `+0x44 HalPerformEndOfInterrupt` → `+0x56` (ret `+0x5b`) **`KiEndInterruptCycleAccumulation(장부, 이전 IRQL)`**.
2. `KiEndInterruptCycleAccumulation` 안:
   - `+0x226` `NestingLevel = 0` — 인터럽트 처리 끝.
   - **`+0x246 CMP [RBX+0x6],0`** — 메모 (`InterruptRequest`) 읽기.
   - **`+0x250 MOV [RBX+0x6],0`** — 메모 지우기.
   - `+0x256 CMP R15B,2` · `+0x25a JNC +0x26d` — **이전 IRQL 이 2 미만이면 `+0x25c AL = 1`**. 2 이상이면 `+0x26f HalRequestSoftwareInterrupt(2)` 로 미룬다.
   - `IdleHalt ≠ 0` 이면 `AL = 0` (`+0x254`).
3. `KiInterruptDispatchNoLock +0x5d JZ` 를 안 타면 → **`+0x5f`** (ret `+0x64`) **`KiDpcInterruptBypass`** → IRQL 2 로 올리고 `STI` (`+0x0 ~ +0xd`) → **`+0x16`** (ret `+0x1b`) **`KiDispatchInterrupt`**.
4. 또는 진짜 벡터 `0x2f` 로 오면: `KiDpcInterrupt` (Ghidra 조각 `FUN_1406b5753` 안) 이 `+0x363 ~ +0x377` IRQL 2 · `+0x381 HalPerformEndOfInterrupt` · `+0x386 STI` · **`+0x39a KiDispatchInterrupt`**.

- "이전 IRQL < 2" 를 풀면: 인터럽트가 끼어들기 **전에** 그 계산대 방해 금지 표지판이 2단계 아래였다는 뜻이다. 그러면 초인종을 지금 울려도 된다. 2 이상이었으면 표지판이 내려갈 때까지 **예약만** 한다 — run13 해설의 "단계가 높으면 울리지 않고 기다렸다가 내려가면 울린다" 와 같은 그림이다.
- 비유: 비상 호출을 다 처리하고 나오면서 장부 메모 ("끝나면 초인종") 를 확인하고 지운다. 원래 하던 일이 방해 금지 2단계 아래였으면 **바로 울리고**, 아니면 **예약** 한다.

#### 새 말 — IDT

- 벡터 (인터럽트 번호) 마다 처리 함수를 적어 둔 표로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 초인종 **번호별 담당자표**.
- 이번엔 이 표를 **안 읽었다.** 그래서 아래 둘은 `[추측]` 이다 → Q104 동적 4 · Q144 ①.
  - 시계 인터럽트가 위 입구들 (`KiInterruptDispatch` · `…NoLock` · `…NoLockNoEtw` · `KiChainedDispatch` · `KiHvInterruptDispatch` 등 — `KiDpcInterruptBypass` 를 부르는 곳) 가운데 어디를 지나는지.
  - 벡터 `0x2f` 가 `KiDpcInterrupt` 로 가는지.

### 2.9 초인종이 울리면 — `KiDispatchInterrupt`: 잡무 먼저, 그다음 깃발 (질문 4)

#### 새 말 — DPC

- DPC (Deferred Procedure Call) = 인터럽트 처리 중에 당장 안 하고 **미뤄 둔 커널 일감** 으로, IRQL 2 에서 몰아서 처리한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드에서 보이는 것 `[정적]`: `KiDispatchInterrupt +0x4d TEST [PRCB+0x38bc],0xbf` → `+0x60 KiSwapStacksAndRetireDpcList`. "일 있음" 표시가 **계산대 장부** 에 있다.
- 비유: 계산대 옆 **잡무 바구니**. 직원 책상의 쪽지 (APC) 와 달리, 바구니는 **계산대에** 딸려 있다.

#### 순서 `[정적]`

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x39 CLI` · `+0x3a` 장부 · `+0x43` 지금 직원 · `+0x47` 이전 IRQL → `WaitIrql (+0x186)` | 준비 | |
| `+0x4d` · **`+0x60` (ret `+0x65`) `KiSwapStacksAndRetireDpcList`** | **DPC 를 먼저 다 돌린다** (타이머 만료 DPC 포함 `[추측]`) | 잡무 바구니부터 비우기 |
| `+0x7e STI` | 인터럽트 다시 켬 | |
| **`+0x7f` · `+0x86` · `+0x88 JNZ +0x1bc`** | 깃발 읽기 | 깃발 확인 |
| **`+0x1bc`** | 깃발 지우기 | 깃발 뽑기 |
| **`+0x1c3` (ret `+0x1c8`) `KiQuantumEnd`** → `+0x1d2 RET` | 몫 끝 처리. 돌아오면 바로 끝 | |
| (깃발 0) `+0x8e` · `+0x93 JZ +0x1c8` | "다음 차례" 자리가 비었으면 그냥 끝 | |
| (깃발 0 + "다음 차례" 있음) `+0xae` · `+0xf7 / +0x103` · `+0x116 State = 2` · `+0x13e WaitReason 0x1f` · `+0x14d KiQueueReadyThread` · `+0x171 KiSwapContext` | **선점** 으로 교대 | "다음 차례" 직원이 자리를 넘겨받음 |

- `+0x99` 에서 그룹 스케줄링 직원이면 `+0x212 KiDeferGroupSchedulingPreemption` 으로 미룰 수 있다 (이름만).
- 한 줄로: **DPC → 몫 끝 → (아니면) 선점.**
- `KiQuantumEnd` 를 부르는 곳은 **`KiDispatchInterrupt +0x1c3` · `KiIdleLoop +0xaf` 둘뿐** 이다 (`_refs`).
- `KiDispatchInterrupt` 를 부르는 곳: `KiDpcInterruptBypass +0x16` · `KiDpcInterrupt +0x39a` (조각).
- PE_emulator (run 문서 표 4): "`KiQuantumEnd` 를 부른다" 는 **맞음**. DPC 먼저 · 깃발 지우기가 없다면 **추가**.

### 2.10 `KiQuantumEnd` ① — 정말 끝났나, 그리고 가산점 한 칸 떼기 (질문 5(b))

#### 맨 앞 — 두 번 거른다 `[정적]`

1. `+0x48 CMP R14,[R12+0x18]` · `+0x4d JZ +0x1c9` — 자리 지킴이면 몫 일을 건너뛴다.
2. `+0x53 MOV R13,[R14+0x48]` — **이 순간의 쌓인 모래** 를 잡아 둔다 (2.11 에서 새 눈금에 더함).
3. **정말 끝났나 다시 확인**:
   - `Feature_HeteroRescheduleFixes` 켜짐 → `+0x70` 업무 카드 자물쇠 (`ThreadLock`) 잡고 `+0x7d CMP R13,[R14+0x20]` · `+0x81 JNC +0x354`.
   - 꺼짐 → `+0xc7f CMP` · `+0xc83 JC +0x1c9`.
   - 안 끝났으면 `+0x1c9` 로 — **가산점 떼기와 눈금 긋기를 건너뛴다.**
4. `+0x354 TEST [R14+0x78],0x100` · `+0x35c JNZ +0xc8e` — `DisableQuantum` 이고 등급 ≥ 16 이면 안 내리고 칸 수를 `0x7f` 로 (`+0xc9c`, 사실상 끝없는 몫).

- 비유: 초인종을 받고 모래시계를 **한 번 더** 확인한다. 깃발이 잘못 꽂혔을 수도 있으니까.

#### 새 말 — 부스트

- **기본 등급 (`BasePriority`) 위에 잠깐 얹어 준 몫** 이다. 이번 코드에 나오는 건 둘 `[정적]`:
  - `ForegroundBoost` — `PriorityDecrement (+0x206)` 의 낮은 4비트.
  - `DecayBoost` — `+0x366`.
- 지금 등급 = `BasePriority` + 남은 부스트 (최대 15) (결과서 최소 규칙 5.1, 계산 본체는 안 읽음).
- 누가 왜 얹어 주는지는 이번 범위 밖이다 (깨움 쪽 `[추측]` → Q141c).
- 비유: 줄 등급에 붙는 **임시 가산점**. 몫이 끝날 때마다 **한 칸씩 뗀다.**

#### 떼는 줄 `[정적]`

- `+0x362 Feature_StackingDecayBoosts…` 로 갈린다.
  - 꺼짐 → `+0x375` (ret `+0x37a`) `KiComputeNewPriorityNonStacking`.
  - 켜짐 → `+0x656` 부터 인라인: `+0x6a1` `PriorityDecrement` 읽기 · `+0x6e5 ~ +0x6ea` `AND 0xf00f` · `+0x6fa ~ +0x704` **`DecayBoost − 1`** · `+0x67c KiClearForegroundBoost` 또는 `+0x715 KiComputeThreadPriorityNonStacking`.
- 적용: `+0x402 KiUpdateVPBackingThreadPriority` → **`+0x773`** (ret `+0x778`) **`KiUpdateThreadPriority`** (본문 안 읽음).
- `KiComputeNewPriority` (172B) 라는 함수도 있지만, `KiQuantumEnd` 는 안쪽 `NonStacking` 을 **직접** 부른다.
- run52 표 "`PriorityDecrement` 를 쓰는 곳에 `KiQuantumEnd`" 와 맞는다.
- 가산점이 없으면 `BasePriority` 그대로라 등급이 안 바뀐다 (결과서).
  - 비유: 가산점 0 이면 뗄 게 없다.
- 사진과의 연결: P1b `Priority 9` = `BasePriority 8` + `DecayBoost 1` `[관찰]` (2.15). 다음 몫 끝에 8 로 돌아갈 것 `[추측]`.
- PE_emulator (run 문서 표 5(b)): 우선순위를 넣으면 **추가**. 가산점이 없으면 영향 작음 `[추측]`.

### 2.11 `KiQuantumEnd` ② — 눈금 새로 긋기: 비우지 않고 위로 (질문 5(a))

#### 줄 `[정적]`

```
KiQuantumEnd +0x480  IMUL EBX,dword ptr [KiCyclesPerClockQuantum]
KiQuantumEnd +0x48d  ADD RCX,R13
KiQuantumEnd +0x49b  MOV qword ptr [R14 + 0x20],RCX
```

- 바로 앞 `+0x46a` (ret `+0x46f`) `KiQueryQuantumReset(Thread)` → `EBX` = **몇 칸** (보통 `QuantumReset`, 아래).
- `+0x480`: 칸 수 × 한 칸.
- `+0x48d`: + `R13` — 2.10 의 `+0x53` 에서 잡아 둔 **그 순간의 `CycleTime`**.
- `+0x49b`: 눈금 (`QuantumTarget`) 에 쓴다.
- 곁줄: `+0x46f R15B = 1` ("이번에 새로 받음" 표시, `+0x22` 에서 0 으로 시작) · `+0x47b KiTryScheduleNextForegroundBoost` · `+0x494 BTR` 로 `QuantumDonation` 끔.
- 식: **새 눈금 = 그 순간 쌓인 모래 + 칸 수 × 한 칸.**

#### 0 부터가 아니라 "더한다" — 단계별로

1. `[정적]` `+0x48d` 가 **`ADD`** 다. 결과서: "0 에서 다시 세는 게 아니라 지금 값에 더한다."
2. `[관찰]` 사진의 `CycleTime` 은 늘기만 한다 (2.5).
3. 그래서 → 모래를 쏟지 않고 **지금 높이 위에 눈금을 새로 긋는** 그림이다.

- 비유: **뒤집지 않는 모래시계.** 몫을 새로 줄 때 모래를 버리지 않는다. 지금 쌓인 높이에서 칸 수만큼 위에 새 금을 긋는다.

#### 찾기보다 먼저 `[정적]`

- 눈금 긋기 (`+0x49b`) 가 다음 직원 찾기 (`+0x611`, 2.12) 보다 **앞** 이다.
- 그래서 다음 직원을 **못 찾아도** 눈금은 이미 새로 그어져 있다.

#### 몇 칸으로 긋나 — `KiQueryQuantumReset` `[정적]`

| 조건 | 돌려주는 칸 수 | 줄 |
|---|---|---|
| `KiVelocityFlags 0x40000` (`+0x0`) **그리고** `KiVariableQuantumEnabled ≠ 0` (`+0x13`), `BamQosLevel` 0 · 3 · 4 | **`0x24`** | `+0x3b` |
| 위 둘 켜짐, QoS 1 | `0x12` | `+0x5a` |
| 위 둘 켜짐, QoS 2 · 5 · 6 · 7 | `2` | `+0x53` |
| 플래그가 **하나라도** 꺼짐 | 업무 카드 `QuantumReset` 그대로 | `+0xa` → `+0x40` |

- 같은 모양이 `KiDeferredReadySingleThread +0x216 ~ +0x247` 에도 인라인으로 있다 (flow-writer). `KiQueueReadyThread +0x749` 는 `KiQueryQuantumReset` 을 부른다.
- **여기가 2.16 의 열쇠다.** 플래그 둘이 다 켜져 있으면 다시 그을 때 첫 눈금 (2칸) 과 **다른** 칸 수 (QoS 0 이면 `0x24`) 를 쓴다.
- 비유: 속도 스위치 두 개가 다 켜져 있으면, 눈금을 다시 그을 때 간격을 직원의 QoS 단계에 따라 고른다. 하나라도 꺼져 있으면 업무 카드에 적힌 기본 간격 그대로.

### 2.12 `KiQuantumEnd` ③ — 다음 직원 찾기: "지금 등급 이상" 에서만, 없으면 그대로 (질문 5(c))

#### 언제 직접 찾나 `[정적]`

- `+0x20c CMP [R12+0x10],0` · `+0x21a JZ +0x57a` — "다음 차례" 자리가 **비었을 때**,
- 그리고 `+0x57a TEST R15B` · `+0x57d JNZ +0x58d` (**방금 눈금을 그었음**) 또는 `+0x57f` · `+0x587` (`ForceDeferSchedule`) 일 때만 직접 찾는다.
- "다음 차례" 에 이미 누가 서 있으면 찾지 않고 **그 직원** 을 쓴다.
- (그 사이 `+0x1c9 ~ +0x1ff` 는 굶은 스레드 스캔 · 그룹 스케줄링 · 장부 자물쇠 — 이름만.)

#### 찾는 법 — 배정표에 `PartialYield` `[정적]`

1. `+0x58d` 배정표 버퍼 (`PRCB+0x37e8`) · `+0x5a0 memset` (지우개) · `+0x5b0 KiStartRescheduleContext`.
2. `+0x5f3 OR [R9+0x22],2` · **`+0x5fb OR AL,8` · `+0x5fd MOV [R9+0x20],AL`** — 칸 플래그 `0x8` = **`PartialYield`**.
   - run75 해설 2.11 표: `PartialYield` = 지금 등급과 **같은 것까지** 받는다 (`…EnumeratorForRescheduleContext +0xf6 / +0xfa` 로 같은 비트를 안 지움).
3. **`+0x611`** (ret `+0x616`) **`KiSearchForNewThreadsForRescheduleContext`** · **`+0x623`** (ret `+0x628`) **`KiCommitRescheduleContext`** — **직접** 부른다.
   - `KiSearchForNewThread` 는 안 부른다. (요청서가 든 `KiSelectNextThread` 는 이 빌드 PDB 에 이름이 없다.)
4. `+0x630 ~ +0x645` 밀려난 직원 정리 → `+0x645 JMP +0x220`.

- 뽑히는 직원: (가산점 뗀 뒤) 지금 등급 **이상** 가운데 가장 높은 줄의 **맨 앞** (run75 해설 2.5).
- 비유: 배정표에 **"나랑 같은 등급 이상만"** 이라고 적어 두고 줄을 훑는다.

#### 있으면 교대, 없으면 그대로 `[정적]`

- **`+0x2a1 MOV RBX,[R12+0x10]` · `+0x2a9 JNZ +0xb62`** — "다음 차례" 를 다시 읽어, 있으면 교대하러 간다 (2.13).
- **없으면**: `+0x2f3` · `+0x2f8 JNZ +0x56a` → `+0x570 KeCheckAndApplyBamQos` → `+0x575 JMP` → **`+0x30e RET`**.
  - 직원을 안 바꾸고 돌아간다. 눈금은 이미 새로 그어졌고, 등급은 내려갔을 수 있다.
- 비유: 같은 등급 이상 줄이 다 비어 있으면, 지금 직원이 **새 눈금을 받고 계속 선다.** 낮은 등급 직원에겐 안 넘긴다.

#### 양보 (run75) 와 나란히

| | 받는 후보 (칸 플래그) | 넘길 직원이 없을 때 |
|---|---|---|
| 몫 끝 (`KiQuantumEnd`) | `PartialYield` — 같거나 높은 등급 | 그대로 계속 (`+0x30e RET`) |
| 양보 (`NtYieldExecution` → `KeYieldExecution`) | `FullYield` — 1 ~ 31 아무나 | `0x40000024` |

- 요청서의 "목록에서 다음 실행 가능한 스레드" 는 **틀림** `[정적]` (run 문서 표 5(c)).

### 2.13 `KiQuantumEnd` ④ — 있으면: 도장 지우고 맨 뒤, 그리고 교대 (질문 5(d)(e))

#### 걸음 `[정적]`

| # | 줄 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0xb62 TEST R15B` · `+0xb65 JZ +0xb6f` · **`+0xb67 MOV byte [R14+0x235],0`** | 눈금을 새로 받았으면 `Preempted = 0` | "자리 뺏김" 도장 지우기 |
| 2 | `+0xb6f` · `+0xb74` | 자리 지킴이면 `+0x2af` 로 (Idle 루프가 직접 처리) | |
| 3 | `+0xb7d` | `NextThread = 0` | "다음 차례" 자리 비우기 |
| 4 | `+0xb85 KiCaptureTotalCyclesCurrentThread` | 바꾸기 직전 모래 정산 | |
| 5 | `+0xb90 KiUpdatePriorityMatrixForRunningTransition` | `CurrentThread = 새` · 새 `State = 2` | 장부 "지금 선 직원" 고쳐 쓰기 |
| 6 | **`+0xb98 MOV byte [R14+0x283],0x1e`** | 옛 직원 `WaitReason = WrQuantumEnd` | 내려간 사유: "몫 끝" |
| 7 | **`+0xba8`** (ret `+0xbad`) **`KiQueueReadyThread(장부, &락, 옛)`** | 옛 직원을 준비 줄에 | 줄 **맨 뒤** 로 |
| 8 | `+0xbb5 KiAbProcessPreContextSwitch` · `+0xbbc KeIsUserVaAccessAllowed` | 이름만 | |
| 9 | **`+0xbd6`** (ret `+0xbdb`) **`KiSwapContext(옛, 새, R8B = 1)`** | 교대 → `SwapContext` (run75 해설 2.8) | 교대 |
| 10 | `+0xbe3 KiAbProcessPostContextSwitch` · `+0xbf8 RET` | 이 직원 차례가 **다시 오면** 여기서 끝 | |

- `KiSwapThread` 를 **안 거친다.** 몫 끝은 `KiQuantumEnd` 가 교대 담당을 직접 부른다.
- 9번의 `call` 은 run75 해설 2.8 처럼 "한참 뒤에" 돌아온다. 옛 직원이 다시 계산대를 받는 순간이다.

#### 왜 맨 뒤인가 — 단계별로

1. `[정적]` run75: 줄에 세울 때 `KiDeferredReadySingleThread +0x1726 / +0x172a` 가 앞/뒤를 가른다. **맨 앞** 은 "도장 (`Preempted ≠ 0`) **그리고** 몫 남음" 일 때만이다.
2. `[정적]` 걸음 1 에서 도장을 **0 으로 지웠다.**
3. `[정적]` 2.11 에서 눈금을 방금 새로 그었다 → `CycleTime < QuantumTarget` (몫 남음).
4. 그래서 → 도장이 0 이니 맨 앞 조건이 안 된다 → **맨 뒤**. 그것도 새 (내린) 등급 줄이다 (결과서·run 문서 같은 결론).
5. 도장을 왜 굳이 지우나 — 코드는 이유를 말해 주지 않는다.
   - 지우지 않았다면 "도장 있음 + 새 눈금 (= 몫 남음)" 이 돼서 맨 앞 조건이 맞아 버린다.
   - 그러니 "몫을 다 쓴 직원이 맨 앞으로 가는 걸 막는 줄" 로 보인다 `[추측: 이 해설의 짐작 — 1 과 3 을 이어 읽은 것]`.

- 비유: 몫을 다 쓴 직원은 새 눈금을 받고 **줄 맨 뒤** 로 간다. 혹시 남아 있던 "자리 뺏김" 도장은 지워서, 맨 앞 새치기를 못 하게 한다.

#### 새 말 — 내려간 사유 번호 (`WaitReason`) — 이번에 PDB 로 확인 `[정적]`

- 업무 카드 `+0x283`. 직원이 계산대에서 내려갈 때 **왜 내려갔는지** 를 적는 번호다. 숫자는 PDB 열거형으로 확인했다 (결과서).
- 비유: 계산대에서 내려간 직원 업무 카드에 남기는 **"사유" 칸**.

| 번호 | 이름 | 누가 쓰나 |
|---|---|---|
| `0x1e` | `WrQuantumEnd` | `KiQuantumEnd +0xb98` (몫 끝) |
| `0x1f` | `WrDispatchInt` | `KiDispatchInterrupt +0x13e` (선점) |
| `0x20` | `WrPreempted` | `KiCheckForThreadDispatch +0xea` (선점) |
| `0x21` | `WrYieldExecution` | `KeYieldExecution +0x1fc` (양보 — run75 `[추측]` → **`[정적]`**) |
| `0x26` | `WrDeferredPreempt` | `KiHandleDeferredPreemption +0x274` (미룬 선점) |

#### 드문 갈래 — 안 끝났는데 불렸을 때 (flow-writer 보탬) `[정적]`

- 몫이 안 끝났으면 (`+0x81` 을 안 타거나 `+0xc83 JC +0x1c9`) `+0x1c9` 로 온다.
- 그때 "다음 차례" 에 이미 누가 서 있으면 `+0x2a1 / +0x2a9 → +0xb62` 로 **교대는 한다.**
- 눈금을 새로 안 받았으니 `R15B = 0` → `+0xb65 JZ +0xb6f` 로 **도장을 안 지운다.**
- 째깍 길은 몫이 끝났을 때만 깃발을 꽂으니 (`+0x244`) 이 갈래는 드물다 `[추측]`. 들어올 수 있는 곳: QoS·헤테로·그룹 예산으로 `+0x16a` 에 가는 길, `KiIdleLoop +0xaf` 에서 올 때.
- `R15B` 를 쓰는 다른 갈래 (`+0x3a0` · `+0x8b3` · `+0x97e` · `+0xc72`) 는 다 안 따라갔다 → Q144 ⑤.

### 2.14 선점 길 둘 — 퀀텀 사슬 밖 (질문 6) · run75 짐작 고침

#### 둘 다 "아니오" `[정적]`

| 함수 | 언제 | 교대 줄 | 부르는 곳 |
|---|---|---|---|
| `KiCheckForThreadDispatch` | 자물쇠를 풀고 IRQL 이 내려갈 때 "다음 차례" 가 있으면 | `+0xd7` · `+0xe2` · `+0xea WaitReason 0x20` · `+0x100 KiQueueReadyThread` · **`+0x10e KiSwapContext`** | `KiAbProcessPreContextSwitch` · `KiProcessDeferredReadyList` · `KeGenericProcessorCallback` · `KeSetPriorityBoost` · `KiFastReadyThread +0xa4` 등 — **째깍·몫 길은 없다** |
| `KiHandleDeferredPreemption` | 선점을 미뤄 놨다가 (`ThreadFlags` 비트 10 `DeferPreemption`, `+0x46 BTR`) 처리할 때 | `+0x251` · `+0x274 WaitReason 0x26` · `+0x289 KiQueueReadyThread` · **`+0x297 KiSwapContext`** | `KiSchedulerApc +0xb0` **하나** |

- 지도로는 `KeClockInterruptNotify` 뿌리에서 깊이 6 / 9 에 나온다. 하지만 사이에 `KeGenericProcessorCallback` · `KiSchedulerApc` 같은 **다른 caller** 를 거친다. 퀀텀 사슬 (깊이 1 ~ 2) 과 다른 가지다.
- `KiQuantumEnd` 는 이 둘을 부르지 않는다. 이 둘이 `KiQuantumEnd` **뒤에** 오지도 않는다.

#### run75 짐작 → 반만 맞음

- run75 `[추측]`: "퀀텀 끝·선점은 `KiCheckForThreadDispatch +0x10e`·`KiHandleDeferredPreemption +0x297` 에서 `KiSwapContext` 를 직접."
- 이번 `[정적]`: **선점 쪽만 맞다.** 퀀텀 끝은 `KiQuantumEnd +0xbd6` 이 직접 부른다.
- run75 문서 맨 위에 정정 한 줄이 붙었다.

#### 교대 담당을 부르는 문 — 지금까지 본 것

| 문 | 줄 | 사유 번호 | 근거 |
|---|---|---|---|
| 기다림 | `KiSwapThread +0x243` | — | run75 |
| 양보 | `KeYieldExecution +0x21f` | `0x21` | run75 |
| **몫 끝** | **`KiQuantumEnd +0xbd6`** | `0x1e` | 이번 |
| 초인종 선점 | `KiDispatchInterrupt +0x171` | `0x1f` | 이번 |
| 선점 | `KiCheckForThreadDispatch +0x10e` | `0x20` | 이번 |
| 미룬 선점 | `KiHandleDeferredPreemption +0x297` | `0x26` | 이번 |

- 모두 `[정적]`.
- 비유: 교대 담당 (`KiSwapContext`) 을 부르는 **문이 여럿** 이다. 몫 끝 문은 `KiQuantumEnd` 다.

### 2.15 사진 일곱 장과 맞춰 보기 — 메인 스레드 퀀텀 칸

flow-writer 가 run44 덤프 읽기 로그 (`02b KTHREAD` · `01b KPROCESS` 구획) 를 다시 읽었다. 새 실행은 없다.

- 일곱 장 모두 `dt nt!_KTHREAD ffff828598445080` — **같은 스레드** 다 (run 문서).
- 사진은 커널 디버거가 **멈춘 순간** 이다.

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 |
|---|---|---|---|---|---|---|---|
| 운영 카드 `QuantumReset (+0x91)` | 2 | 2 | 2 | 2 | 2 | 2 | 2 |
| 업무 카드 `QuantumReset (+0x28b)` | 0 | 2 | 2 | 2 | 2 | 2 | 2 |
| 눈금 `QuantumTarget (+0x20)` | 0 | `0x7f2814` | 같음 | 같음 | 같음 | **`0x9fa7208`** | 같음 |
| 쌓인 모래 `CycleTime` (십진) | 0 | 0 | 24,000 | 3,082,560 | 7,587,360 | 17,517,600 | 21,113,760 |
| `ExpectedRunTime (+0x54)` | `0x4c4b40` | 같음 | 같음 | `0x3ccc10` | 같음 | `0x22642b` | `0x94889` |
| `BamQosLevel (+0x204)` | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| `Priority (+0xc3)` | 0 | 8 | 8 | 8 | 8 | **9** | **9** |
| `DecayBoost (+0x366)` | 0 | 0 | 0 | 0 | 0 | **1** | **1** |

모두 `[관찰]`. 로그 줄 번호는 run 문서 표에 있다.

#### 읽은 것 하나씩

- **첫 눈금은 `KeStartThread` 식 그대로** `[관찰]` + `[정적]` (2.6).
- **K3 ~ P1a 는 눈금이 그대로다** → 그 사이 이 직원은 새 눈금을 받은 적이 없다 `[추측: 읽기]`.
  - 그러니 run75 가 본 P1 → P1a 의 교대 5번 (`ContextSwitches` `0x17` → `0x1c`) 은 **몫 끝 길이 아닌 쪽** (선점·기다림) 으로 보인다 `[추측]`.

#### P1a ~ P1b: 첫 몫을 다 쓰고 다시 그었다 — 단계별로

1. `[관찰]` P1a `CycleTime` 7,587,360 < 첫 눈금 8,333,332 (아직 남음). P1b 엔 눈금이 `0x9fa7208` (167,408,136) 으로 바뀌었다.
2. `[정적]` 다시 긋는 줄은 모두 "`CycleTime` + n × 한 칸" 꼴이다 (`KiQuantumEnd +0x48d` · `KiDeferredReadySingleThread +0x253 ADD RCX,R13`).
3. 계산: n 을 넣어 보면 **`0x24` (36) 만 맞는다.**
   - 167,408,136 − 36 × 4,166,666 = **17,408,160** — P1a (7,587,360) 와 P1b (17,517,600) 사이다.
   - n = 2 나 `0x12` 를 넣으면, 그은 순간 `CycleTime` 이 P1b 값보다 커야 해서 안 맞는다.
4. 그 순간 `CycleTime` 17,408,160 > 옛 눈금 8,333,332 → 첫 몫은 **이미 넘었다.**
5. `[정적]` `0x24` 는 플래그 **둘이 다 켜져야** 나온다 (`KiQueryQuantumReset +0x0` · `+0x13`, `KiDeferredReadySingleThread +0x216` · `+0x229`). 이 직원은 `BamQosLevel 0` `[관찰]` 이라 `KiQueryQuantumReset` 은 `+0x25 JZ +0x3b` → `0x24` 를 돌려준다. 맞아떨어진다.
6. 그래서 → "P1a ~ P1b 사이 한 번, `0x24` 칸으로 다시 그었다" `[추측: 계산]`.
7. 아직 확인 아님: **어느 줄이** 그었는지는 모른다 (후보 `KiQuantumEnd +0x49b`, `KiDeferredReadySingleThread +0x260` 등) → Q104 동적 3.

- 비유: 눈금이 갑자기 **아주 높이** 그어졌다. 거꾸로 재 보면 "그때 모래 높이 + 36칸" 일 때만 그 높이가 나온다.
- 결과서는 이 `0x9fa7208` 을 "같은 스레드가 아닌 것 같다" 며 근거에서 뺐다. flow-writer 가 일곱 장 모두 같은 스레드임을 확인해 근거로 썼다 (결과서와 다르게 ①).

#### P1b 의 등급 9 — 가산점이었다 `[관찰]`

- P1b `Priority 9` = `BasePriority 8` + `DecayBoost 1`.
- run75 의 `[추측: 부스트]` 는 **`DecayBoost`** 쪽이었다.
- `KiQuantumEnd` 는 몫 끝마다 `DecayBoost` 를 1 뗀다 (`+0x6fa ~ +0x704`) → 다음 몫 끝에 8 로 돌아갈 것 `[추측]`.
- 누가 1 로 올렸는지는 모른다 (깨움 가산점 쪽 `[추측]` → Q141c, Q104 동적 5).

### 2.16 퀀텀 길이 — 읽기가 둘이다 (이번 바퀴에서 가장 조심할 곳)

천천히 가자. 먼저 결론 한 줄:

- **사이클로 센 눈금 한 칸 (4,166,666) 은 두 읽기 모두 같다.**
- 다른 건 **"벽시계 째깍 한 번이 몇 박자냐 (T/10)"** 와 **"다시 그을 때 몇 칸이냐"** 다.
- 두 읽기 모두 `[추측: 계산]` 이다. **아직 어느 쪽도 확정이 아니다.**

#### 먼저 — 확실한 것과 모르는 것

| 무엇 | 상태 |
|---|---|
| `KeInitSystem` 식 (T/30, 플래그면 ÷6 더), T/2400 → `KiShortExecutionCycles` (`+0x2dd`) | `[정적]` |
| `KiQueryQuantumReset` 갈래 (플래그 둘 다 켜짐 + QoS 0 → `0x24`, 아니면 `QuantumReset`) | `[정적]` |
| `KeStartThread` 첫 눈금 = `QuantumReset` × 한 칸 | `[정적]` |
| `KeInitThread +0xa0 ~ +0xa9`: `ExpectedRunTime` = `KiShortExecutionCycles << 4` | `[정적]` |
| K3 눈금 `0x7f2814`, K2 `ExpectedRunTime 0x4c4b40`, P1b 눈금 `0x9fa7208`, `BamQosLevel 0` | `[관찰]` |
| 한 칸 = 4,166,666 | `[관찰 값, 계산]` |
| **`KiVelocityFlags` 비트 `0x40000`, `KiVariableQuantumEnabled`, `KeMaximumIncrement`, `MHz`** | **사진에 없다 — 모른다** |

- 코드에는 **갈림길** 이 있다:

```
KeInitSystem +0x2e9  TEST dword ptr [KiVelocityFlags],0x40000
KeInitSystem +0x308  JZ   (플래그가 0 이면 아래 ÷6 을 건너뜀)
KeInitSystem +0x314  MOV  dword ptr [KiCyclesPerClockQuantum],EDX
```

- 정적으로 아는 건 "갈림길이 있다" 까지다. **이 VM 이 부팅할 때 어느 쪽으로 갔는지** 는 그때의 플래그 값에 달렸다. 그건 코드만 봐서는 못 정한다.
- 오케스트레이터가 `.asm` 으로 다시 봤다: **두 읽기 모두 코드와 맞는다** (plan.md 결정 기록).

#### 읽기 A — 결과서: "스위치 꺼짐" 으로 읽었다

1. 가정: `KiVelocityFlags 0x40000` **꺼짐**.
2. 그러면 한 칸 = T/30. 4,166,666 이 나오려면 **T = 125,000,000** (결과서: 플로어 계산까지 맞음).
3. 한 칸 = 째깍의 **1/3** → 첫 눈금 2칸 = 째깍의 **2/3**.
4. 이 읽기에선 다시 그을 때도 `KiQueryQuantumReset` 이 `QuantumReset` (2) 를 돌려준다 — 플래그가 하나라도 꺼져 있으면 `+0xa → +0x40` 이니까.

- 결과서는 P1b 를 다른 스레드로 보고 뺐다. `KiShortExecutionCycles` 도 안 봤다. 그래서 이 읽기와 부딪치는 사진이 결과서 안에는 없었다.
- 비유: 벽시계 째깍 한 번 = 눈금 **3칸**. 첫 모래시계 2칸 = 째깍의 2/3.

#### 읽기 B — flow-writer: "스위치 켜짐" 으로 읽었다

출발점이 다르다. **K2 의 `ExpectedRunTime`** 에서 시작한다.

1. `[정적]` `KeInitThread +0xa0 ~ +0xa9`: `ExpectedRunTime` = `KiShortExecutionCycles << 4`.
2. `[관찰]` K2 `ExpectedRunTime 0x4c4b40` → `KiShortExecutionCycles` = **`0x4c4b4` = 312,500** (짐 지도 3.14 `ExpectedRunTime` 줄, D1).
3. `[정적]` `KeInitSystem +0x2dd`: `KiShortExecutionCycles` = T/2400. 거꾸로 풀면 **T ≈ 750,000,000**.
   - "≈" 인 건, 나눗셈에서 나머지를 버리니 거꾸로 풀면 한 값이 아니라 좁은 범위가 나오기 때문으로 보인다 `[추측: 이 해설의 풀이]`.
4. 이 T 로 **스위치 꺼짐** 이면: 한 칸 = T/30 = 25,000,000 → 첫 눈금은 50,000,000 이어야 한다. K3 `0x7f2814` 와 **안 맞는다.**
5. **스위치 켜짐** (÷6 더) 이면: 한 칸 4,166,666 → 첫 눈금 8,333,332 = `0x7f2814`. **맞는다.**
6. P1b: `0x24` 칸으로 다시 그은 것만 맞는다 (2.15). `0x24` 는 플래그 **둘 다** 켜져야 나온다 → `KiVariableQuantumEnabled` 도 켜짐.
7. 이 읽기의 시간: 째깍 한 번 = T/10 = **75,000,000** 박자, 한 칸 = 째깍의 **1/18**, 첫 눈금 = 째깍의 **1/9**, 다시 그은 `0x24` 칸 = **째깍 2번**.
   - 메인 스레드가 P2 까지 쓴 박자 (21,113,760) 는 째깍 하나 (75,000,000) 에 못 미친다.

- 비유: 벽시계 째깍 한 번 = 눈금 **18칸**. 첫 모래시계 2칸 = 째깍의 1/9. 다시 그을 땐 **36칸** = 째깍 2번.

#### 나란히 놓으면

| | 읽기 A (결과서) | 읽기 B (flow-writer) |
|---|---|---|
| `KiVelocityFlags 0x40000` | 꺼짐 (가정) | 켜짐 |
| `KiVariableQuantumEnabled` | 상관없음 (첫 플래그가 꺼지면 `QuantumReset`) | 켜짐 |
| T = `KeMaximumIncrement` × `MHz` | 125,000,000 | ≈ 750,000,000 |
| 한 칸 (사이클) | 4,166,666 | 4,166,666 (**같음**) |
| 한 칸 (째깍으로) | 1/3 | 1/18 |
| 첫 눈금 2칸 (째깍으로) | 2/3 | 1/9 |
| 다시 그을 때 칸 수 (QoS 0) | `QuantumReset` (2) | `0x24` (= 째깍 2번) |
| K3 눈금 `0x7f2814` 와 | 맞음 | 맞음 |
| K2 `ExpectedRunTime 0x4c4b40` 과 | **안 맞음** (`KiShortExecutionCycles` 가 52,083 `0xcb73` 이어야 함) | 맞음 |
| P1b 눈금 `0x9fa7208` 과 | **안 맞음** (2칸으로는 안 나옴) | 맞음 (`0x24`) |

#### 그럼 B 로 정해도 되나? → 아직 아니다

- 사진 세 값 (K2 `ExpectedRunTime` · K3 눈금 · P1b 눈금) 이 B **하나로 다 맞는** 건 사실이다 (run 문서).
- 그래도 B 는 `[추측: 계산]` 이다. 이유 (run 문서): **전역 값은 사진에 없다.** 숫자를 거꾸로 풀어 맞춘 것이지, 플래그를 직접 본 게 아니다.
- 그래서 결과서 읽기 (A) 도 **지우지 않고 둘 다 둔다** (flow.md 3장 "퀀텀" 절 · 4장 run76 줄).
- 비유: 범인 (스위치 상태) 을 직접 본 사람은 없다. 발자국 셋이 B 쪽을 가리킬 뿐이다.

#### Q104 에서 어떻게 가르나

- 직접 보는 둘: **`dd nt!KiVelocityFlags L1`** (비트 `0x40000`) · **`db nt!KiVariableQuantumEnabled L1`**.
- T 를 보는 셋: `dd nt!KiShortExecutionCycles L1` (B 면 `0x4c4b4`) · `dd nt!KeMaximumIncrement L1` · `?? @$prcb->MHz` (CPU 0) — 곱이 125,000,000 이냐 ≈ 750,000,000 이냐.
- `dd nt!KiCyclesPerClockQuantum L1` 은 역산 `0x3f940a` 를 기대한다. 그런데 **두 읽기 모두** 이 값을 기대하니, 이것만으로는 못 가른다 (위 표 "한 칸 (사이클)" 줄).
- run 문서: "Q104 에서 `dd` 몇 줄로 닫힌다."

#### PE_emulator 에게는

- "한 칸 · 새 직원 첫 눈금 칸 수 · 다시 그을 칸 수" 를 **따로** 설정으로 두면 두 읽기를 다 흉내 낼 수 있다 (flow-writer 덧붙임).
- 사이클로만 세면 첫 눈금은 두 읽기 모두 8,333,332 로 같다. 갈리는 건 **"다시 그을 때 몇 칸"** 과 **"째깍이 몇 사이클마다냐"** 다 (위 표).

### 2.17 PE_emulator 에게 — 최소 규칙 여섯 + 덧붙임 둘

결과서 끝 "흉내 낼 최소 규칙" 을 run 문서가 그대로 옮겼다. 짧게:

1. **단위 = CPU 사이클.** 직원마다 `CycleTime` (쌓인 모래) 과 `QuantumTarget` (눈금) 을 따로 가진다. 사이클 = 실행한 명령 수 (비례) 로 대신해도 된다 `[추측]`. 숫자는 설정 값으로.
2. **새 직원**: `CycleTime = 0`, `QuantumReset = 프로세스 값`, 눈금 = `QuantumReset × 한 칸` (`KeStartThread +0x1ba ~ +0x1dc`).
3. **검사는 째깍에서만** (`KiUpdateRunTime`): 자리 지킴이가 아니고 `CycleTime ≥ QuantumTarget` 이면 깃발 + 디스패치 초인종. 째깍 사이엔 검사 안 함 (그래서 눈금을 조금 넘겨서 돈다).
4. **초인종** (`KiDispatchInterrupt`): ① DPC 먼저 ② 깃발이면 지우고 `KiQuantumEnd` ③ 아니면 "다음 차례" 가 있으면 선점.
5. **`KiQuantumEnd`** (몫이 끝난 게 맞을 때만):
   1. 가산점 한 칸 떼고 등급 = `BasePriority` + 남은 가산점 (최대 15). `DisableQuantum` + 등급 ≥ 16 이면 안 건드리고 `0x7f` 칸.
   2. **눈금 새로 긋기**: 지금 `CycleTime` + 칸 수 × 한 칸. 찾기 **전에**.
   3. **다음 직원**: 지금 등급 **이상** 가운데 가장 높은 줄의 **맨 앞**. "다음 차례" 가 이미 있으면 그 직원.
   4. **없으면 그대로 계속.**
   5. **있으면**: 도장 지우고 → 옛 직원 (내린) 등급 줄 **맨 뒤** (`WrQuantumEnd`) → 새 직원 `Running` → 교대.
6. **자리 지킴이** 는 퀀텀 검사 대상이 아니다.

flow-writer 덧붙임 (넣을지는 PE_emulator 판단):

- 규칙 1 의 "틱의 2/3" 은 **한 가지 읽기** 다 (2.16). 칸 수를 따로 설정하면 둘 다 흉내 낼 수 있다.
- 규칙 5 의 "몫이 끝난 게 맞을 때만" 은 5.1 · 5.2 에 걸린다. 안 끝났어도 "다음 차례" 가 있으면 교대는 하고, 그땐 도장을 안 지운다 (2.13 드문 갈래).

"지금 우리" 판정 (run 문서 표):

| 무엇 | 판정 |
|---|---|
| 명령 `0x10000` 개마다 `KiQuantumEnd` | **틀림** — 째깍마다 검사만, `KiQuantumEnd` 는 그다음 초인종. "째깍 = 명령 N 개" 로 줄이는 건 가능 `[추측]` |
| 명령 수로 세기 | 사이클 대용은 됨 `[추측]`. 단 **직원마다 따로**, "쓴 양 ≥ 눈금" 꼴. 전역 카운터 하나는 **틀림** |
| 깃발 → 나중에 디스패치 | 두 단계가 없으면 **모자람** |
| `KiDispatchInterrupt → KiQuantumEnd` | **맞음**. DPC 먼저·깃발 지우기가 없다면 **추가** |
| 눈금 새로 긋기 | 없음 → **추가** |
| 가산점 떼기 | 없음 → 우선순위를 넣으면 **추가** |
| "목록의 다음 실행 가능 스레드" | **틀림** — "이상" 에서만 고르고, 없으면 계속 |
| 옛 직원 맨 뒤 | 없음 `[추측]` → **추가** |
| 교대 줄 | **맞음** (같은 개념) |
| 선점 길 둘 | 이번 요청에선 건너뛰어도 됨 |

- **이름 주의**: 진짜 `KiQuantumEnd` 를 부르는 곳은 `KiDispatchInterrupt +0x1c3` · `KiIdleLoop +0xaf` 둘뿐이다. 지금 nt 엔진이 명령 수로 `KiQuantumEnd` 를 직접 부른다면, 호출관계가 진짜와 다르다는 뜻이다.

### 2.18 정정과 "결과서와 다르게 적은 곳" (짧게)

요청서 짐작이 틀렸거나 고쳐진 곳 `[정적]`:

1. "퀀텀 깎는 자리" → 깎지 않는다. 목표선 (2.5).
2. "`KeClockInterruptNotify → KiUpdateRunTime`, 중간에 둘?" → `KiUpdateTime` 은 거친다 (시계 주인만), `KeAccumulateTicks` 는 곁다리 (2.4).
3. "`KiSearchForNewThread`/`KiSelectNextThread` 를 부르나" → 안 부른다. `KiSearchForNewThreadsForRescheduleContext` 를 `PartialYield` 로 직접 (2.12).
4. 사슬 뼈대 (`… → KiDispatchInterrupt → KiQuantumEnd`) 는 맞다. 다만 깃발 → `KiDispatchInterrupt` 는 호출이 아니라 인터럽트 끝 처리다 (2.8).

결과서와 다르게 적은 곳 (run 문서 ① ~ ⑧):

- ① P1b·P2 의 `0x9fa7208` → **같은 스레드**. `0x24` 칸 다시 긋기와 맞음 `[추측: 계산]` (2.15).
- ② "T = 125,000,000" → **스위치 꺼짐을 가정한 읽기**. 둘 다 둔다 (2.16).
- ③ `KiCheckForThreadDispatch` 의 `KiUpdatePriorityMatrixForRunningTransition` 부르는 줄 `+0xe7` → **`+0xe2`** (ret `+0xe7`).
- ④ `KiQuantumEnd` 의 `KiReadyDeferredReadyList` `+0xcfa` → **`+0xd01`** (ret `+0xd06`). `+0xcfa` 는 인자 `LEA`.
- ⑤ 안 끝난 채 불릴 때의 갈래 (2.13).
- ⑥ 칸 수를 고르는 같은 모양이 `KiDeferredReadySingleThread +0x216 ~ +0x247` 에도 인라인 (2.11).
- ⑦ 사진 대조 전체 (`BamQosLevel 0`, `DecayBoost 1`) — 결과서엔 K3 ~ P1 눈금·모래만.
- ⑧ `_calls.tsv` 에 `alias:` 로 나온 callee 셋 (`KiComputeNewPriorityNonStacking` · `KiComputeThreadPriorityNonStacking` · `KiCompleteRescheduleContextEntry`) 은 이름이 하나뿐이라 그대로 썼다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | … run75 까지 | + run76 (퀀텀 함수 23개 처음 뽑음, `KeStartThread` 읽기) |
| 기준 — 사진 메모 | run75 줄까지 | + run76 줄: 메인 스레드 퀀텀 칸 · 운영 카드 `QuantumReset` 다시 읽음. 커널 전역은 사진에 없음. 새 실행 없음 |
| 2.2 제목 | … run75 | + run76 |
| 2.2 `nt!KeStartThread` 절 | 프로세스 칸도 여기서 쓴다 (run49) 까지 | + **"첫 퀀텀도 여기서 정한다"** `[정적]` (`+0x1ba` ~ `+0x1dc`) + K2 → K3 `[관찰]` |
| 3장 "스레드 넘기기" 절 비유 | 손님 · 점원 | **직원** · 자리 지킴이 (해설과 같은 그림) |
| 3장 "스레드 넘기기" 절 `[추측]` 줄 | 퀀텀 끝·선점은 두 함수일 것 `[추측]` → Q141b2 | + (해결됨, **반만 맞음** → run76 `[정적]`) 선점 길만. 퀀텀 끝은 `KiQuantumEnd +0xbd6` |
| 3장 "스레드 넘기기" 사진 줄 | `Priority` 8 → 9 `[추측: 부스트]` | + → run76 `[관찰]` P1b `DecayBoost 1` (8 + 1) |
| 3장 **새 절** "퀀텀" | — | 비유 + 트리 + `[정적]` 넷 + `[관찰]` 사진 + **읽기 둘** + 동적 → Q104 |
| 4장 run75 줄 둘 | 동적 1 ~ 6 · 정적 남은 것 (번호 없음) | 동적 2 에 `DecayBoost` 출처 · 동적 6 에 "몫 끝 길은 아닌 쪽 `[추측: 읽기]`" · 정적 남은 것에 **Q143** 번호 + "(해결됨 → run76)" |
| 4장 새 줄 넷 (run76) | — | 동적 1 ~ 5 · **퀀텀 길이 정적 읽기 둘** · 정적 남은 것 ① ~ ⑤ · 결과서와 다르게 ① ~ ⑦ |
| 5장 | — | run76 줄 (두 표) |

태그가 바뀐 곳:

- PE_emulator `[추측]` "명령 `0x10000` 개마다 `KiQuantumEnd`, 목록의 다음" → **틀림** `[정적]`.
- 요청서 `[추측]` 사슬 `KeClockInterruptNotify → KiUpdateRunTime → (표시) → KiDispatchInterrupt → KiQuantumEnd` → 뼈대 **맞음** `[정적]`. 단 표시 → `KiDispatchInterrupt` 는 인터럽트 끝 처리이고, 시계 인터럽트가 지나는 입구는 그대로 `[추측]`.
- run75 `[추측]` "퀀텀 끝·선점은 `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption`" → **선점만 맞음** `[정적]`.
- run75 `[추측: 부스트]` (P1b `Priority 9`) → **`[관찰]`** `DecayBoost 1`.
- run75 `[추측]` `WaitReason 0x21` = `WrYieldExecution` → **`[정적]`** (PDB 열거형).
- 짐 지도 `ExpectedRunTime` 의 뿌리 `KiShortExecutionCycles` 를 쓰는 곳 → **`[정적]`** `KeInitSystem +0x2dd` (T/2400). 곱의 값은 `[추측: 계산]`.
- **새로 생긴 `[추측: 계산]`** 둘: P1a ~ P1b 의 `0x24` 칸 다시 긋기, 퀀텀 길이 읽기 B.

### 짐 지도 · run75 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음**, 2장 숫자 그대로. 짐 지도 바퀴가 아니다 — 주제가 특정 단계의 짐이 아니라 계산대 주고받기라서.
  - 3.14 `_KTHREAD` 머리에 "Q78d4 에서 볼 것 — 퀀텀 줄" 한 줄: `QuantumReset` ← `KeStartThread +0x1ba / +0x1c1`, 눈금 첫 값 ← `+0x1c7 / +0x1dc`, 다시 긋기 ← `KiQuantumEnd +0x49b` 등, 모래 ← `KiEndThreadCycleAccumulation +0x1bc` · `KiCaptureTotalCyclesCurrentThread +0x1d8`.
  - 3.14 `ExpectedRunTime` 줄 근거 칸 · 7.4 실마리에 "`KiShortExecutionCycles` 를 쓰는 곳 = 부팅 `KeInitSystem +0x2dd`".
  - 7.2 `_KTHREAD` 가리킴 줄에 한 마디.
- run75 문서: 맨 위 정정 한 줄 (퀀텀 끝 길 · `WrYieldExecution`), "정적 — 다음 바퀴 후보" 에 **Q143** 번호.

### 큰 그림에서

- **지도 쪽**:
  - run75 에서 "교대 (`SwapContext`) 를 누가 부르나" 의 한쪽 (기다림·양보) 이 채워졌다. 이번에 **몫 끝** 과 **선점** 세 문이 더해졌다 (2.14 표).
  - 남은 큰 문은 **깨움·타이머** 쪽이다.
  - 새 스레드의 **첫 눈금** 이 `KeStartThread` 에서 그어진다는 게 지도 (2.2) 에 들어갔다.
- **PE_emulator 쪽**:
  - "명령 수마다 `KiQuantumEnd`" 를 **"째깍에서 검사·깃발 → 초인종에서 `KiQuantumEnd`"** 두 단계로 고칠 근거가 생겼다.
  - 눈금 긋기·"이상" 에서 찾기·없으면 계속·맨 뒤 — 규칙 여섯으로 정리됐다.
  - 길이 숫자는 두 읽기라, 설정 값으로 두라는 덧붙임이 있다.
- **사진 쪽**:
  - run75 의 "P1 → P1a 교대 5번" 이 몫 끝 길은 아닌 쪽으로 좁혀졌다 `[추측: 읽기]`. 남은 후보는 선점·기다림이다.
- **다음** (같은 요청 25):
  - **Q141c1** (깨움): `KiCommitThreadWait` · `KeSetEvent` 등. P1b 의 `DecayBoost 1` 을 누가 얹었는지 실마리가 거기서 나올 수 있다 `[추측]`.
  - **Q141c2** (타이머): 이번에 곁다리로만 본 `KeAccumulateTicks +0x28 KiCheckForTimerExpiration` 쪽. 초인종에서 먼저 도는 DPC 안의 타이머 만료도 그쪽이다 `[추측]`.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **퀀텀 길이 두 읽기 중 어느 쪽인가** (`KiVelocityFlags 0x40000` · `KiVariableQuantumEnabled` · T = `KeMaximumIncrement × MHz`) | 부팅 때 정해지는 전역 값. 사진에 없다. 코드엔 갈림길만 있다 | **Q104** 동적 1 (`dd nt!KiVelocityFlags L1` · `db nt!KiVariableQuantumEnabled L1` · `dd nt!KiShortExecutionCycles L1` · `dd nt!KeMaximumIncrement L1` · `?? @$prcb->MHz`) |
| 째깍 간격·방식 (`KeTimeIncrement` · `KeMinimumIncrement` · `KiClockTimerPerCpuTickScheduling` — 1 이면 `KiUpdateRunTime +0x1fe` · `+0x4ea KiSetClockTimer` 갈래 · `KiGroupSchedulingEnabled`) | 실행 때 값 | **Q104** 동적 2 |
| 초기화 동안 몫이 실제로 끝나나, P1a ~ P1b 에서 **누가** 눈금을 다시 그었나 | 사진은 숫자만 보여 준다 | **Q104** 동적 3 (`ba w8 <메인 스레드>+0x20` 에서 `k` — `KiQuantumEnd+0x49b` 냐 `KiDeferredReadySingleThread+0x260` 이냐) · 정적 **Q144 ②** (다시 긋는 다른 길: `KiDeferredReadySingleThread +0x211 / +0x214` · `+0x23c JNZ +0x1108`) |
| 시계 인터럽트가 지나는 입구, 모래 쌓기가 `KiStartInterruptCycleAccumulation` 길인지, 메모 → `KiDpcInterruptBypass` 길을 타는지, 벡터 `0x2f` → `KiDpcInterrupt` 인지, WinDbg 가 조각을 `nt!KiDpcInterrupt+0x39a` 로 보여주는지 | IDT 를 안 읽었다 `[추측]` | **Q104** 동적 4 (`!idt`) · 정적 **Q144 ①** (IDT 표) |
| P1b `DecayBoost` 를 1 로 올린 곳 | 이번 범위 밖 (깨움 쪽 `[추측]`) | **Q104** 동적 5 (`ba w1 <메인 스레드>+0x366`) · **Q141c** |
| 가산점 계산 본체 (`KiComputeNewPriorityNonStacking` · `KiComputeThreadPriorityNonStacking` · `KiUpdateThreadPriority`) | 안 읽었다 | 정적 **Q144 ③** (PE_emulator 가 우선순위를 넣을 때만) |
| 운영 카드 `QuantumReset 2` 의 뿌리 (`PspComputeQuantum` · `PspForegroundQuantum` · `PsPrioritySeparation`) | `.asm` 이 없다 | 정적 **Q144 ④** |
| `KiQuantumEnd` 의 `R15B` 다른 갈래 · `KiIdleLoop +0xaf` 에서 올 때의 길 | 다 안 따라갔다 | 정적 **Q144 ⑤** (작음) |
| 타이머 만료 (`KeAccumulateTicks +0x28 KiCheckForTimerExpiration`), 초인종 때 먼저 도는 DPC 안의 일 | 이번 범위 밖 | **Q141c2** |
| `KiForwardTick` 이 `HalpTimerClockIpiRoutine` 으로 이어지나 | `KiForwardTick` 안 읽음 `[추측]` | 대기열 없음 (run 문서에 따로 항목 없음) |
| `HalpTimerClockInterrupt` 가 넘기는 인자 이름 | `[공백]` | 대기열 없음 |
| `PRCB+0x38b9` · `+0x38bc` 등 칸의 진짜 이름 | 공개 PDB 의 `_KPRCB` 가 `+0x6d0` 에서 잘림 | 대기열 없음 (`[추측: 이름]` 으로 둠) |

- 추천 멈춤점 (run 문서, 모두 `nt!`): `KiUpdateRunTime+0x23c` (`rsi`) · `+0x16a` (여기까지 오면 몫 끝) · `KiDispatchInterrupt+0x1c3` · `KiQuantumEnd` 입구 · `+0x49b` (`rcx` = 새 눈금) · `+0x773` · `+0x30e` (안 바꿈) · `+0xbd6` (`rcx` 옛, `rdx` 새) · `KeStartThread+0x1dc` (`rax`) · 데이터 `ba w1 @$prcb+0x38b9`.
  - 커널 멈춤점은 모든 프로세스에 걸린다. test.exe 스레드일 때만 서게 조건을 붙여야 한다.

---

## 5. 더 읽기

- run 문서: [`run76-thread-quantum.md`](run76-thread-quantum.md) — "PE_emulator 쪽에서 쓸 것" 표·최소 규칙, 호출 지점 표, 시계 틱 사슬 · 퀀텀 검사와 길이 · 깃발 → `KiDispatchInterrupt` · `KiQuantumEnd` 표 · 선점 길 둘, 호출 관계 그림, **사진과 맞춰 본 것**, 결과서와 다르게 적은 곳
- 결과서: [`_workspace/sast/72_thread-quantum_result.md`](../../_workspace/sast/72_thread-quantum_result.md) — "한눈에" 그림, 질문별 답, 최소 규칙, 동적 확인 목록·추천 멈춤점
- 요청서: [`_workspace/sast/72_thread-quantum.md`](../../_workspace/sast/72_thread-quantum.md)
- flow.md: [3장](flow.md) 새 절 "퀀텀 — 시계 틱 → `nt!KiUpdateRunTime` → `KiDispatchInterrupt` → `KiQuantumEnd`" · 같은 장 "스레드 넘기기" 절의 run76 줄 · [2.2](flow.md) `nt!KeStartThread` 절 run76 줄 · 4장 run76 줄 넷 (특히 "퀀텀 길이 — 정적 읽기 둘")
- 짐 지도: [`baggage.md`](baggage.md) 3.14 머리 "Q78d4 에서 볼 것 — 퀀텀 줄", 3.14 `ExpectedRunTime` 줄, 7.4 실마리
- 앞 이야기: [run75 해설](run75-thread-switch-해설.md) (고르기·줄 세우기·교대·양보) · [run49 해설](run49-kprocess-token-htable-해설.md#새-말-우선순위와-퀀텀) (운영 카드의 등급·모래시계) · [run52 해설](run52-kthread-create-해설.md) (업무 카드) · [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) (IRQL·초인종) · [run12 해설 2.9](../02_sprint/run12-insert-thread-해설.md#29-kestartthread-와-kereadythread-는-뭐가-다른가) (`KeStartThread`)
