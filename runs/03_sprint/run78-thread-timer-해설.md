# run78 thread-timer — 해설

> 이 글은 [run78 문서](run78-thread-timer.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q141c2**: Q141c 의 둘째 조각 — 기다림의 마감·타이머 (PE_emulator 요청 25 ㄷ-2) (`_workspace/plan.md`, 이번에 **끝**)
  - 첫 조각 Q141c1 (깨움·출구) 은 run77 에서 끝났다.
  - 다음 항목: **Q141d** (끝내기) → Q141e → Q142. 그다음 **Q104 VM 단계** 앞에서 멈춘다 (사용자 결정 2026-11-12 "vm 제외하고 Q142 까지").
  - 동적 몫 열둘은 **Q104** 에 붙였다. 작은 정적 아홉은 새 **Q146** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run78-thread-timer.md`](run78-thread-timer.md) (가장 중요)
  - 요청서 [`_workspace/sast/74_thread-timer.md`](../../_workspace/sast/74_thread-timer.md) · 결과서 [`74_thread-timer_result.md`](../../_workspace/sast/74_thread-timer_result.md) (판정 **분석성공**. 정적으로 못 닫은 여섯 — `KiTimerExpirationDpc` 를 누가 넣나, `KiCheckForTimerExpiration +0xd7 ~ +0xe3` 비교의 뜻, `TimerType` 두 표의 이름·값, `KiDeferredReadySingleThread +0x6d` 갈래·줄 앞/뒤, `KiCancelTimer` `Expired` 갈래·`Timer2`, `KeWaitForMultipleObjects` 의 마감)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 (PDB 목록 · run77 사진 메모 고침 · run78 사진 메모 새 줄) · 2.1 제목과 `KeInitThread` 절 한 줄 · 3장 "퀀텀"·"기다림·깨움" 트리 한 줄씩 + **새 절 "기다림의 마감·타이머"** · 4장 고친 줄 넷 + 새 줄 셋 · 5장 두 줄
  - 짐 지도 [`baggage.md`](baggage.md): 3.14 `Timer*`·`WaitBlock*`·`MiscFlags`·`RelativeTimerBias`, 3.11 `ThreadTimerSets` 근거만 (새 짐 칸 없음, 서른넷째판)
  - run75 · run76 · run77 문서에 한 줄씩 (run77 은 **맨 위 정정 안내**)
- 이번 바퀴는 **정적만**이다. 값 대조는 run44 때 찍어 둔 메인 스레드 사진의 **날 바이트 구획** (`02c ETHREAD_RAW`) 으로만 했다 (flow-writer, 새 실행 없음).
- **이번 바퀴에서 조심할 곳 셋**:
  - `0x102` 는 "알람이 울려서 깨우는" 길에선 코드에 박힌 상수가 아니라 **번호표에 미리 적힌 값** 이다. 그런데 "들어갈 때 이미 지났음" 길 (`KeWaitForSingleObject +0x240`) 은 상수를 직접 넣는다. 둘을 섞지 말자 (2.13).
  - 사진에서 새로 읽은 **"약 240초 기다림"** 은 `[추측: 계산]`, "신호로 깨어 알람을 끈 꼴" 은 `[추측: 읽기]` 다. 실행으로 본 게 아니다 (2.17).
  - run 문서가 **run77 을 두 군데 고쳤다** (run77 문서 맨 위 안내). run77 해설 2.9 마지막 줄과 2.16 첫머리도 같이 고쳐 읽어야 한다 (2.16).

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `_KTHREAD` = 인사 카드 첫 장 **업무 카드**, 번호표 4장 (`WaitBlock`) 을 만들 때 미리 단다 → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- 이벤트 = **신호등**, APC = 직원 책상 **쪽지** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 기다림 블록 = **번호표**, `KiTryUnwaitThread` = **어깨 두드리기** → [run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적), 경주 → [run13 해설 2.9](../02_sprint/run13-resume-thread-해설.md#29-경주--부모가-먼저-초록불을-켜면-정적)
- `KeWaitForMutexObject` 로 찍혔지만 `KeWaitForSingleObject` (본명과 간판 이름) → [run13 해설 2.10](../02_sprint/run13-resume-thread-해설.md#210-이름-하나--kewaitformutexobject-로-찍혔지만-kewaitforsingleobject)
- CPU = **계산대**, `_KPRCB` = **계산대 장부** (공개 PDB 가 `+0x6d0` 에서 잘림) → [run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸)
- 준비 줄 · `NextThread` · 줄 앞/뒤 규칙 (`Preempted`) → [run75 해설 2.10](run75-thread-switch-해설.md#210-줄에-다시-세우기--기본-맨-뒤-선점당했고-몫이-남은-직원만-맨-앞), `WaitStatus` 를 들고 돌아간다 → [run75 해설](run75-thread-switch-해설.md) 2.9
- 시계 인터럽트·틱 = **벽시계 째깍**, 시계 주인 CPU = 벽시계 당번, 시스템 시간 = **가게 시계**, `KeAccumulateTicks` = 째깍마다의 **출근부 기록** (곁다리), 디스패치 인터럽트 = **교대·잡무 초인종**, `InterruptRequest` = "끝나면 초인종" **메모**, 벡터·IPI, DPC = 계산대 옆 **잡무 바구니**, "몫 끝" **깃발** (`PRCB+0x38b9`) → [run76 해설](run76-thread-quantum-해설.md) 2.4 · 2.7 · 2.8 · 2.9
- 번호표 칸 (`WaitType`·`BlockState`·`WaitKey`), `WaitKey` → `WaitStatus` (**번호표에 적힌 번호가 결과 칸으로**), 커밋 = **"정말 잔다" 도장**, 기다림 진행 단계 (`WaitRegister.State`), 깨운 직원 명단 (`PRCB+0x2d08`), 퇴장 담당 (`KiExitDispatcher`), 자동/수동 신호등, `0x100` = "처음부터 다시 확인" 도장 → [run77 해설](run77-thread-wake-해설.md) 2.3 · 2.6 · 2.7 · 2.8 · 2.10
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- `dt` 와 날 바이트 `dq` 의 차이 (`dt` 는 안쪽 구조체 칸 값을 안 펴 준다) → [run44 해설 2.9](run44-baggage-snap-해설.md#29-새-말--raw-dq-와-dt-의-차이)
- 사진 지점 K2 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가), 메인 스레드 사진 일곱 장 → [run75 해설 2.12](run75-thread-switch-해설.md#212-사진과-맞춰-보기--메인-스레드-업무-카드-일곱-장)

**비유 한 가지 주의.**

- 이 글도 run75 ~ run77 처럼 **CPU = 계산대, 스레드 = 직원** 으로 이어 쓴다.
- 기다리는 물건은 run13 · run77 해설 그림 그대로다. 이벤트 = **신호등**, 기다림 블록 = **번호표**, 깨우기 = **어깨 두드리기**.
- 이번에 새로 나오는 그림은 **알람 시계** 다. 직원 책상마다 하나씩 놓여 있다 (스레드 안 타이머, 2.3).
  - flow.md 3장 새 절도 "알람 시계" 로 쓴다. 같은 그림이다.
- flow.md 새 절은 기다리는 물건을 **택배**, `0x102` 를 **"시간 초과 쪽지"** 라고 부른다.
  - 이 글은 run77 해설처럼 신호등·번호표로 이어 쓴다. 그리고 `0x102` 는 **3번 표 (네 번째 번호표) 에 적힌 번호** 라고 부른다.
  - 이 글에서 "쪽지" 는 APC 몫이라서 겹치지 않게 했다.

### 한 줄로 먼저

- 마감은 기다림을 **시작할 때 한 번** 계산해서, 직원 책상의 **알람 시계** (`_KTHREAD.Timer` `+0x100`) 에 맞춘다 `[정적]`.
  - 상대 마감 = 그때의 `InterruptTime` + `|Timeout|` (`KiCommitThreadWait +0x194`).
  - 절대 마감 = 시스템 시각 기준을 `InterruptTime` 기준으로 바꿔 같은 칸에 (`KiComputeDueTime +0xd8`).
  - `0x100` 으로 처음부터 다시 돌아도 마감은 새로 안 늘어난다.
- 알람이 지났는지는 **벽시계 째깍마다** 본다 (`KeAccumulateTicks +0x28 KiCheckForTimerExpiration`). 째깍 때는 **스위치 (비트 `8`) 만 켠다** `[정적]`.
- 실제로 깨우는 건 그다음 **초인종** (디스패치 인터럽트) 의 **잡무 단계 (DPC 단계)** 다. 같은 초인종 안의 **퀀텀 끝보다 먼저** `[정적]`.
  - `KiRetireDpcList +0xca4 KiTimerExpiration` → `+0x20e KiProcessExpiredTimerList` → `+0xba KiTimerWaitTest` → `+0x325 KiTryUnwaitThread` → `WaitStatus = 0x102`.
- `0x102` 는 직원마다 처음부터 알람에 매달아 둔 **3번 표** (`WaitBlock[3]`) 의 `WaitKey` 다 (`KeInitThread +0x19e`) `[정적]`.
  - 신호등 쪽 (`WaitKey 0`, run77) 과 **같은 길** 로 결과 칸에 들어간다.
- 신호와 알람이 겹치면 **"신호 우선" 규칙은 없다** `[정적]`. 직원 자물쇠를 먼저 잡고 `State == 5` 를 본 쪽이 이긴다.
  - "신호 먼저" 는 **들어갈 때 검사 순서** 하나뿐이다.
- 그래서 PE_emulator 의 "넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (`0x102`)" 은 **결과값은 맞고, 언제·누가 가 다르다** `[정적]`.
- 사진 (메인 스레드): P1a ~ P1b 에 알람이 **한 번** 맞춰졌고 (상대 마감, 약 240초 `[추측: 계산]`), 울리기 **전에** 꺼진 꼴이다 `[추측: 읽기]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 만들기                 (run9 ~ run12)
 └─ KeInitThread: 직원 책상에 알람 시계 + 3번 표를 미리 단다                    (run52, 이번에 뜻이 닫힘)
 └─ … ntdll 초기화 … P1a ~ P1b 에 메인 스레드가 알람을 한 번 맞춤 [추측: 읽기]
 └─ main: WaitForSingleObject(t, INFINITE)    (sample/test.c:35, 지도 범위 밖)

 계산대 주고받기 — 모든 직원이 몇 번이고
   기다림으로 내놓기 → KiSwapThread                     (run75)
   양보 · 몫 끝 · 선점                                  (run75 · run76)
   기다림 안쪽 · 깨우기 (신호등)                         (run77)
   마감 지난 기다림 (알람)                               ← 이번
   스레드 끝내기                                         (Q141d, 다음)
```

- 이번 주제도 run75 ~ run77 처럼 지도의 **한 단계가 아니다.**
  - 마감 있는 기다림은 어느 직원이든, 초기화 내내 몇 번이고 탈 수 있는 길이다.
  - 그래서 flow.md 그림엔 안 넣고, 3장에 절 하나 ("기다림의 마감·타이머") 로 적었다.
- 지도와 맞물리는 곳은 셋이다.
  - **run52 `KeInitThread`**: "타이머를 달고, `WaitBlock[3]` 을 타이머에 매단다" 까지만 봤다. 그게 **기다림 마감 알람** 이고, 매단 표의 번호가 `0x102` 라는 게 이번 답이다 (2.13).
  - **run76 시계 사슬**: `KeAccumulateTicks` 곁다리 안의 `+0x28 KiCheckForTimerExpiration` 을 이름만 봤다. 이번에 펼쳤다 (2.9). 초인종 때 "DPC 먼저" 안에 타이머 일이 들었나 (`[추측]`) 도 닫혔다 (2.10).
  - **run77 기다림 틀**: `KiCommitThreadWait +0x1f2 KiInsertTimerTable` · `+0x39e KiTimerWaitTest` 를 이름만 봤다. 이번에 펼쳤다 (2.5 ~ 2.8).

### 무엇이 궁금했나

한 문장으로: **마감 (timeout) 이 있는 기다림은 언제·누가 끝내나. 결과 `0x102` 는 어디서 오나. 신호와 마감이 겹치면 어떻게 되나.**

- 계기: `../PE_emulator` 의 nt 엔진은 지금 이렇다 `[추측]` (요청서).
  - 스레드를 **넘길 때마다**, 마감이 지난 잠든 스레드를 **먼저 깨운다**. 결과는 `0x102` (`STATUS_TIMEOUT`).
- 진짜에 맞춰 달라는 요청 25 ㄷ-2.
- 이번 바퀴는 **알람 쪽만** 본다. 번호표 걸기·어깨 두드리기·`WaitStatus` 자체는 run77 을 가리키기만 한다.
- 질문은 다섯 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | 마감을 거는 쪽: (a) 스레드 안 타이머를 거는 줄 (b) 상대 마감을 무엇에 더하나, 절대는 다른 갈래인가 (c) 이미 지났으면 그 자리에서 `0x102` 인가 | 2.7 · 2.5 · 2.6 · 2.8 |
| 2 | 만료를 알아채는 쪽: 시계 인터럽트에서 보는 줄, DPC 를 요청하나 | 2.9 · 2.10 |
| 3 | `KiTimerExpiration → KiProcessExpiredTimerList → KiTimerWaitTest`: (a) 사슬 (b) `WaitStatus = 0x102` 쓰는 줄, 번호표 떼기 (c) 준비 줄로 | 2.10 ~ 2.14 |
| 4 | 신호와 마감이 겹칠 때 규칙 | 2.15 |
| 5 | PE_emulator 흉내와 비교 | 2.4 · 2.18 |

- test.exe 자신의 기다림 `WaitForSingleObject(t, INFINITE)` (`sample/test.c:35` `[소스]`) 은 `Timeout` 을 NULL 로 넘길 것으로 보인다 `[추측: KernelBase 쪽 안 읽음]`.
  - 그렇다면 **마감 없음** 이라 알람을 안 맞춘다 (2.5). 사진에서 본 알람은 초기화 동안 **다른 누군가의** 기다림이다 (2.17).

### 바퀴 전에 알던 것

- `[정적]` run75: `KiSwapThread` 는 `WaitStatus` (`+0xc8`) 를 돌려준다 (`+0x25c` · `+0x29a`). 깨어난 뒤 `+0x263 BTR TimerActive` → 켜져 있었으면 `+0x372 KiCancelTimer` (run75 해설 2.9 에 한 줄).
- `[정적]` run76: 시계 사슬 `HalpTimerClockInterrupt → KeClockInterruptNotify → (KiUpdateTime →) KiUpdateRunTime`, 곁다리 `KiUpdateRunTime +0x96 KeAccumulateTicks`. 그 안에 `+0x28 KiCheckForTimerExpiration` (이름만). `KiDispatchInterrupt` 는 `+0x60` 에서 DPC 를 먼저, 그다음 `+0x7f` 에서 몫 끝 깃발.
- `[정적]` run77: 깨우는 쪽이 `WaitStatus = 번호표 WaitKey` 를 그 자리에서 쓴다. `KiTryUnwaitThread` 는 `State ≠ 5` 면 지나친다. `KiCommitThreadWait +0x23d TimerActive` · `+0x1f2 KiInsertTimerTable` · 이미 만기면 `+0x39e KiTimerWaitTest` (이름만). 상대 마감은 `KeWaitForSingleObject +0x3b6 ~ +0x3d7` 에서 반복 지점 `+0x104` 위에서 한 번.
- `[정적]` run52: `KeInitThread +0x172 KeInitializeTimer` (`Timer` 칸), `+0x1d2 ~ +0x1dd` `WaitBlock[3]` 을 타이머에 매단다. 번호표 네 장의 `Thread` 칸은 `+0x69 ~ +0x7e` 가 미리 적는다.
- `[추측]` 짐 지도 3.14: 알람의 `Dpc` 칸 값의 뜻.
- `[추측]` PE_emulator: 넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 번호 `(n)` 은 run 문서 번호 그대로 (몇 가지는 뺐다). ★ 는 이 글 절.

```
[기다림 시작 — 알람 맞추기]
KeWaitForSingleObject  (Ghidra KeWaitForMutexObject)
 │  +0xdf ~ +0xfb   Timeout NULL → 마감 없음 · 0 이상 → 절대 · 음수 → +0x3b6 ~ +0x3d7 상대 (한 번)    ★ 2.5 · 2.6
 │  +0x211 / +0x213  신호 검사 먼저 (run77)
 │  +0x224 ~ +0x359  이미 지났으면 → +0x240 R14D = 0x102 (안 잠)                                  ★ 2.8
 └─ +0x846  KiCommitThreadWait (종류, 값)
      │  +0x23d TimerActive · +0x242 타이머 헤더 락 + Inserted   (마감 있을 때, 맨 앞)              ★ 2.7
      │  +0x194 DueTime = 시작 InterruptTime + |Timeout| (상대) · +0x198 / +0x1ae 손
      ├─ (1) +0x4e8  KiComputeDueTime              절대만                                         ★ 2.6
      ├─ (3) +0x1f7  KiInsertTimerTable(Prcb, Thread+0x100, 0, 손)
      │    └─ +0x3b8  KiRemoveEntryTimer            칸 맨 앞인데 DueTime ≤ 지금 → AL = 0              ★ 2.8
      │  +0x20f  헤더 락 풀기
      ├─ (4) +0x3a3  KiTimerWaitTest               AL = 0 일 때만 — 자기 만기
      └─ (5) +0x225  KiSwapThread                   잠
           │  +0x25c WaitStatus · +0x263 BTR TimerActive → 켜져 있었으면
           └─ (2) +0x377  KiCancelTimer(Thread+0x100, 1)   AL = 0 이면 +0x37f ~ +0x391 3번 표 되돌림  ★ 2.15

[벽시계 째깍 — 알아채기]
KiUpdateRunTime +0x9b KeAccumulateTicks                                       (run76)
 └─ +0x2d  KiCheckForTimerExpiration(Prcb)                                                        ★ 2.9
      │  +0x5c / +0x66  비트 8 이미 켜져 있으면 끝 · +0x33c / +0x33f 지금 ≥ 칸의 가장 이른 만기
      ├─ (1) +0x3d5  KiSetDpcRequestFlag(&PRCB+0x38bc, 8)
      │  +0x3e7  InterruptRequest = 1                 (인터럽트 안)
      └─ (2) +0x4f6  HalpInterruptSendIpi(…, 0x2f)    (인터럽트 밖)

[초인종 — 잡무 단계에서 처리]
KiDispatchInterrupt
 └─ +0x65  KiSwapStacksAndRetireDpcList            (이것 뒤에 +0x7f 몫 끝, run76)                 ★ 2.10
      ══▶ +0xc 꼬리점프 → DPC 스택 조각 +0x0 → KiRetireDpcList
KiRetireDpcList                                     (KiIdleLoop +0x99 도 부름)
 │  +0x406 XCHG 요청 비트 · +0x40d / +0x411 비트 8 · +0xbd4 지움 · +0xc7c STI
 └─ (2) +0xca9  KiTimerExpiration(Prcb, 시작 손, InterruptTime)                                   ★ 2.11
      │  +0x36c ~ +0x37c  칸 첫 타이머 만기 ≤ 지금 → 떼어 Expired, 만료 목록에
      └─ +0x213 (표 0) / +0x2b2 (표 1) / +0x94e / +0x981  KiProcessExpiredTimerList
           │  +0x8d 목록 머리 · +0x96 타이머 헤더 락
           ├─ (1) +0xbf   KiTimerWaitTest(Prcb, Timer, …)                                         ★ 2.12
           │    │  +0x1d7 SignalState = 1 · +0x207 R8D = WaitKey (0x102)                            ★ 2.13
           │    └─ +0x32a  KiTryUnwaitThread(Prcb, WaitBlock[3], 0x102, 0)
           │         +0x57 ~ +0x60 State ≠ 5 → 지나침 · +0x149 State = 7 · +0x168 WaitStatus = 0x102  ★ 2.15
           └─ (2) +0x101  KiProcessThreadWaitList(Prcb, 1, 0, 2)                                  ★ 2.14
                │  +0x141 ~ +0x269  진 표 (BlockState 4) 떼기 · +0x9f AdjustIncrement 0
                └─ +0x106  KiDeferredReadySingleThread   (run75)
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- 이번에 **처음 뽑은 함수가 14개** 다 (모두 depth 0).
  - 요청 대상 7: `KiTimerExpiration` · `KiProcessExpiredTimerList` · `KiTimerWaitTest` · `KiCheckForTimerExpiration` · `KiInsertTimerTable` · `KiComputeDueTime` · `KiTimerExpirationDpc`.
  - 넓힌 6: `KiRetireDpcList` (`KiTimerExpiration` 부르는 줄 근처만) · `KiSwapStacksAndRetireDpcList` · `KiSetDpcRequestFlag` · `KeInitializeTimerTable` · `KiSelectActiveTimerTable` · `KiCancelTimer`.
  - 실수로 같이 뽑힘 1: `KiSetTimerEx` — **안 읽었다.**
- 새 지도: `_map/KiTimerExpiration_*` (311함수) · `_map/KiCheckForTimerExpiration_*` (29함수).
- 이름 주의 둘:
  - PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (rva `0x217eb0`). run77 과 같다. 이 글은 PDB 이름으로 쓴다.
  - `KiSwapStacksAndRetireDpcList +0xc` 가 꼬리점프로 가는 곳 (rva `0x6b6440`) 은 PDB 이름이 둘이다: `KySwapStacksAndRetireDpcList` · `KxSwapStacksAndRetireDpcList`. 그래서 run 문서는 **"DPC 스택에서 `KiRetireDpcList` 를 부르는 조각"** 이라고만 적었다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `│    └─ +0x32a  KiTryUnwaitThread(Prcb, WaitBlock[3], 0x102, 0)` (`KiTimerWaitTest` 아래)

- **누가**: `KiTimerWaitTest`. 울린 알람 하나를 처리하는 담당이다 (2.12).
- **어디서**: 부르는 자리 `+0x325`, 돌아올 자리 `+0x32a`.
  - 바로 앞 **`+0x207 MOVZX R8D,word [RAX+0x12]`** 에서 번호표의 `+0x12` 칸 (`WaitKey`) 을 셋째 인자로 꺼냈다. 그리고 `+0x20c JMP +0x31c` 로 와서 부른다.
  - 멈추면 콜스택에 `nt!KiTimerWaitTest+0x32a` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `KiTryUnwaitThread` — run77 의 **어깨 두드리기** 담당.
- **무엇을 위해**: 알람에 매달린 번호표 (3번 표) 의 주인을 깨우고, 결과 칸에 **번호표에 적힌 번호** (`0x102`) 를 적게 한다.
- 돌아와서 반환값을 **안 본다** (`+0x32a`). 이미 다른 쪽이 깨운 직원이어도 그냥 다음 표로 간다 (2.15).
- 비유: 알람이 울리면, 알람 담당이 매달린 번호표를 떼어 들고 어깨 두드리기 담당에게 "이 번호로 깨워 줘" 하고 넘긴다. 신호등 담당 (`KeSetEvent`) 이 하던 것과 같은 일이다.

#### 줄 2: `KiCheckForTimerExpiration +0x3d0` (ret `+0x3d5`) `KiSetDpcRequestFlag(&PRCB+0x38bc, 8)`

- **누가**: `KiCheckForTimerExpiration`. 벽시계가 째깍할 때마다 곁다리로 알람 보관대를 보는 담당이다 (2.9).
- **어디서**: `+0x3d0`, ret `+0x3d5`. 바로 앞 `+0x3c8 MOV EDX,8` · `+0x3cd MOV RCX,R14` (`R14` = `&PRCB+0x38bc`).
- **누구를**: `KiSetDpcRequestFlag` — 16비트 칸에 비트를 `OR` 로 켜는 작은 도우미 (`CMPXCHG.LOCK`, 옛 값을 돌려줌).
- **무엇을 위해**: 계산대 장부의 **"알람 확인" 스위치** (비트 `8`) 를 켠다. **알람은 아직 안 꺼낸다.**
- 바로 뒤 `+0x3d5 TEST AL,0xa9` / `+0x3d7 JNZ` — 옛 값에 이미 요청 비트가 있었으면 초인종을 또 부르지 않는다. 아니면 `+0x3e7 InterruptRequest = 1` (또는 IPI).
- 비유: 째깍 담당은 보관대를 훑고 "지난 알람 있음" 스위치만 올린 뒤 "끝나면 초인종" 메모를 남기고 자기 일로 돌아간다.

#### 줄 3: `KiCommitThreadWait +0x194  MOV [RDI+0x18],RCX`

호출이 아니라 **명령 한 줄**이다. 그런데 "알람 시각을 어디에 적나" 의 답이 이 줄이다.

- **누가**: `KiCommitThreadWait` — 기다림 확정 담당 (run77 해설 2.6).
- **무엇을**: `RDI` = `Thread+0x100` (`+0x9a LEA RDI,[RBX+0x100]`) 이라 `[RDI+0x18]` = **`Timer.DueTime`** 이다.
  - `RCX` = `RelativeTimerBias + InterruptTimeBias + R13` (`+0x17a ~ +0x191`).
  - `R13` = `KeWaitForSingleObject` 가 넘긴 마감 값 (`[RBP-0x51]`, 2.5).
- **언제**: 상대 마감일 때만 이 줄로 온다 (`+0x170 CMP R12D,2`). 절대 마감은 `KiComputeDueTime +0xd8` 이 같은 칸에 쓴다 (2.6).
- **그래서**: 알람 시각은 기다림을 시작할 때 **이 한 번** 적힌다. 그 뒤엔 `+0x198 SHR RCX,0x12` 로 꽂을 칸 번호 (손) 를 뽑는다.

### 2.3 무대 — 이번에 쓰는 칸과 새 말

뒤에서 오프셋이 나오면 이 절로 돌아오면 된다.

#### 새 말 — 스레드 타이머 (알람 시계)

- 직원마다 업무 카드 안에 **타이머 하나** 가 들어 있다: `_KTHREAD.Timer` (`+0x100`, `_KTIMER` `0x40`B) `[정적]`.
  - 직원을 뽑을 때 `KeInitThread +0x172 KeInitializeTimer` 가 만든다 (run52).
  - 마감 있는 기다림은 **새 타이머를 만들지 않고 이것 하나를 다시 쓴다** (`KiCommitThreadWait +0x9a` → `Thread+0x100`) `[정적]`.
- 비유: 직원 책상마다 놓인 **알람 시계**. 쉬러 갈 때 맞추고, 깨면 끈다.

#### 새 말 — 상대 마감 · 절대 마감

- `Timeout` 은 64비트 값 하나를 가리키는 포인터다. `KeWaitForSingleObject` 가 이걸 셋으로 가른다 `[정적]` (`+0xdf ~ +0xfb`):
  - 포인터 **NULL** → 마감 없음 (종류 0). 알람을 안 맞춘다.
  - 값 **음수** → **상대** 마감 (종류 2). "지금부터 이만큼 뒤".
  - 값 **0 이상** → **절대** 마감 (종류 1). "가게 시계로 이 시각에".
- 단위는 100ns 다 (요청서 예: `-10000000` = 1초 뒤).
- 비유: 상대 = "지금부터 4분 뒤 울려 줘". 절대 = "오후 3시 정각에 울려 줘".

#### 새 말 — `InterruptTime` 과 `SystemTime` (공용 게시판의 스톱워치와 가게 시계)

- 둘 다 KUSD (로비 공용 게시판) 에 있는 칸이다 `[정적]` (PDB):
  - `InterruptTime +0x8` · `SystemTime +0x14` · `InterruptTimeBias +0x3b0`.
  - 코드에는 `0xFFFFF78000000000` 기준 주소가 음수로 찍힌다: `-0x87ffffffff8` (= `+0x8`) · `-0x87fffffffec` (= `+0x14`) · `-0x87ffffffc50` (= `+0x3b0`).
- 일반적으로 알려진 설명으로는 `InterruptTime` 은 부팅 뒤 흐른 시간, `SystemTime` 은 날짜·시각이다 `[추측]`.
- 이번 코드에서 보이는 것 `[정적]`:
  - 알람 시각 (`DueTime`) 과 째깍마다의 비교는 **모두 `InterruptTime` 기준** 이다.
  - 절대 마감 (`SystemTime` 기준) 은 걸 때 `InterruptTime` 기준으로 **바꿔서** 적는다 (2.6). 그래서 가게 시계가 나중에 바뀌어도 이미 건 알람은 그대로다 (결과서).
- 비유: 게시판에 **스톱워치** (가게 문 연 뒤 흐른 시간) 와 **가게 시계** (날짜·시각) 가 나란히 걸려 있다. 알람은 늘 **스톱워치 눈금** 으로 맞춘다.

#### 알람 시계 (`_KTIMER`) — 칸

| 칸 | 이름 (PDB) | 이 글에서 부르는 말 |
|---|---|---|
| `+0x0` 바이트 | `Type` (8 = 알림형 · 9 = 동기형, 비트 7 = **헤더 락**) | 알람 종류 · 알람 **자물쇠** |
| `+0x1` 바이트 | `TimerControlFlags` (`Absolute` 비트 0 · `Wake` 비트 1 · `EncodedTolerableDelay` 비트 2 ~ 7) | "가게 시계로 맞춤" 표시 |
| `+0x2` 바이트 | `Hand` | 꽂힌 **칸 번호** (손, 아래) |
| `+0x3` 바이트 | `TimerMiscFlags` (`Index` 비트 0 ~ 5 · **`Inserted`** 비트 6 · **`Expired`** 비트 7) | **"꽂혀 있음"** · **"울려서 꺼냄"** |
| `+0x4` | `SignalState` | **울렸음** (1) |
| `+0x8` | `WaitListHead` | 알람에 매단 **번호표 줄** |
| `+0x18` | `DueTime` | **울릴 스톱워치 값** |
| `+0x20` | `TimerListEntry` | 보관대 칸 줄에 꿰는 고리 |
| `+0x30` | `Dpc` | (스레드 알람은 "없음", 2.12) |
| `+0x38` | `Processor` (16비트) | |
| `+0x3a` | `TimerType` (16비트) | **어느 보관대** (0 / 1, 아래) |
| `+0x3c` | `Period` | 반복 간격 (스레드 알람은 0 = 한 번짜리) |

- 이름이 헷갈리는 곳: PDB 는 머리 `+0x0` 바이트도 `TimerType` 이라고 부른다. 이 글은 머리 쪽을 **`Type`** (8/9), `+0x3a` 쪽을 **`TimerType`** (0/1) 으로 나눠 쓴다 (run 문서 사진 표와 같다).
- **알림형 / 동기형** `[정적]` (`KiTimerWaitTest`, 2.12):
  - 알림형 (`Type 8`) = 울리면 매단 번호표 **모두** 를 깨운다. run77 의 **수동** 신호등 같다.
  - 동기형 (`Type 9`) = 첫 성공 **하나만** 깨우고 0 으로. run77 의 **자동** 신호등 같다.
  - 스레드 알람은 **알림형** 이다 (`KeInitializeTimer +0x9 MOV byte [RCX],8`).

#### 새 말 — 타이머 표와 손 (알람 보관대)

- 계산대 장부 `+0x4100` 부터가 **타이머 표** 다. 오프셋과 동작은 `[정적]`, 이름은 `[추측: 이름]` (공개 PDB `_KPRCB` 가 `+0x6d0` 에서 잘림).
  - 앞 64칸 (`0x200`B) = **만료 목록 머리** (울려서 꺼낸 알람을 모으는 곳, 2.11).
  - `+0x4300` 부터 **칸 2 × 256개**. 칸 하나 `0x20`B: `+0x0` 락 · `+0x8` 줄 머리 · `+0x18` **칸의 가장 이른 만기**.
  - 칸 번호 = `Hand + TimerType × 0x100` (`KiInsertTimerTable +0x4e ~ +0x7f`). `+0x4100` 에서 세면 `0x10` 이 더 붙는다 (결과서 식 `(Hand + 0x10) + TimerType × 0x100`).
  - 빈 칸의 "가장 이른 만기" 는 크게 채워 둔다 (`KeInitializeTimerTable +0x9b` · `+0x9f`).
- **손 (hand)** = `만기 >> 18` 의 아래 8비트 `[정적]` (`KiCommitThreadWait +0x198 SHR RCX,0x12` · `KiComputeDueTime +0xe7`).
  - 한 칸 = `2^18 × 100ns` ≈ **26.2ms**, 256칸 한 바퀴 ≈ **6.7초**.
- `PRCB+0x8310` · `+0x8314` = 표 종류별 **"마지막으로 훑은 손"** `[추측: 이름]`.
- `TimerType` 0/1 두 표의 이름·차이는 `[추측]` 이다. 결과서는 "0 = 정확한 (HR) 표, 1 = 뭉쳐 터지는 비 HR 표" 로 보았다 (전역 이름 `KiLastNonHrTimerExpiration`·`KiLastPseudoHrTimerExpiration` 에서).
- 비유: 계산대 장부에 붙은 **알람 보관대** 두 개.
  - 보관대마다 칸 256개가 시계판처럼 둥글게 붙어 있다. 칸 하나가 약 26.2ms 몫이다.
  - 알람은 "울릴 스톱워치 값" 에서 뽑은 칸 번호 (손) 에 꽂는다. 칸 안은 이른 순서로 줄 선다.
  - 칸 겉에는 **"이 칸에서 가장 이른 시각" 메모** 가 붙어 있다. 째깍 담당은 이 메모만 훑는다 (2.9).

#### 새 말 — DPC 요청 비트 (잡무 스위치판)

- 계산대 장부 `+0x38bc` (16비트). DPC 쪽 **요청 비트 모음** 이다. 비트 `0x8` = **타이머 만료 처리 요청** `[추측: 이름]`.
  - 켜는 쪽: `KiCheckForTimerExpiration +0x3c8 ~ +0x3d0` (2.9).
  - 읽고 지우는 쪽: `KiRetireDpcList +0x406 ~ +0x411` · `+0xbd4` (2.10).
  - run76 의 `KiDispatchInterrupt +0x4d TEST byte [PRCB+0x38bc],0xbf` 에 이 비트가 들어 있다.
- 비유: 계산대 옆 잡무 바구니에 딸린 **스위치판**. 스위치 하나하나가 "이런 잡무가 있다" 표시다. 비트 8 = **"알람 확인"** 스위치.
- run76 의 **"몫 끝" 깃발** (`+0x38b9`) 과는 **다른 칸** 이다.

#### 업무 카드 (`_KTHREAD`) 에서 이번에 쓰는 칸

| 칸 | 이름 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x74` | `MiscFlags` (`TimerActive` 비트 9) | **"알람 켜 둠"** 표시 |
| `+0xc8` | `WaitStatus` | 결과 칸 (run77) |
| `+0xd0` | `WaitBlockList` | 지금 쓰는 번호표 |
| `+0xf8` | `RelativeTimerBias` | 상대 마감 **보정값** (2.5) |
| `+0x100` | **`Timer`** | **알람 시계** |
| `+0x140` | `WaitBlock` ×4 (`0x30`B 씩) — **`WaitBlock[3]` = `+0x1d0`** | 번호표 4장, **3번 표** = 알람 몫 |
| `+0x184` | `State` | 2 Running · 5 Waiting · 7 DeferredReady |
| `+0x186` · `+0x187` | `WaitIrql` · `WaitMode` | |
| `+0x1e4` | `KernelApcDisable` | |
| `+0x220` | `Process` | |
| `+0x236` · `+0x237` | `AdjustReason` · `AdjustIncrement` | 깨움 가산점 메모 (run77) |
| `+0x24b` | `WaitBlockCount` | 번호표 몇 장 썼나 |
| `+0x3b8` | `ThreadTimerDelay` | "허용 지연" (2.5) |

- 이름은 PDB (`pdb_struct.py`, 결과서), 쓰임은 `[정적]`.
- 가게 운영 카드 (`_EPROCESS`) 에서는 `ThreadTimerSets +0x700` 하나가 나온다 (2.7 끝).

### 2.4 이번 바퀴의 핵심 — "넘길 때마다" vs "째깍마다 보고, 잡무 단계에서 깨움"

이게 이번 바퀴에서 가장 중요한 그림이다. 장면 하나로 나란히 보자.

> 장면: 직원 B 가 신호등 E 를 **4분 마감** 으로 기다리며 잔다. 아무도 E 를 켜지 않는다. 그동안 직원 A 가 계산대에서 일한다.

#### PE_emulator 지금 `[추측]` (요청서)

1. B: "잠듦" 표시, 마감을 기억.
2. 스레드를 **넘길 때마다**, 마감 지난 잠든 스레드를 **먼저 깨운다** (`0x102`).

- 비유: 계산대 교대 때마다 교대 담당이 쉬는 직원들 마감을 하나하나 확인한다.

#### 진짜 커널 `[정적]`

1. B 가 기다림을 **시작할 때**: 알람 시각 = 지금 스톱워치 + 4분 → 알람 보관대 칸에 꽂는다 → 잔다 (2.5 · 2.7).
2. **벽시계가 째깍할 때마다** (계산대마다): 째깍 담당이 보관대 칸 겉 메모 ("가장 이른 시각") 를 훑는다 (2.9).
   - 아직 아무것도 안 지났으면 → 아무 일도 없다.
3. 4분이 지난 첫 째깍에: **"알람 확인" 스위치 (비트 8)** 만 켜고 "끝나면 초인종" 메모를 남긴다. 알람은 아직 안 꺼낸다.
4. **초인종** (디스패치 인터럽트) 이 울리면 `KiDispatchInterrupt` → 잡무 단계 `KiRetireDpcList` 가 스위치 8 을 보고 → 보관대에서 지난 알람을 꺼내 울린다 (2.10 ~ 2.12).
   - B: `State = 7`, `WaitStatus = 0x102` (3번 표 번호), 깨운 직원 명단 → 준비 줄 (2.13 · 2.14).
5. **같은 초인종 안에서 그다음** 에 A 의 "몫 끝" 깃발을 본다 (run76). B 는 이미 고르기 후보 안에 들어 있다.

- 비유: 교대 담당이 아니라 **벽시계** 가 알람을 챙긴다. 째깍 때는 스위치만 올리고, 실제 깨우기는 초인종이 울렸을 때 **잡무 먼저** 하면서 한다.

#### 나란히 (run 문서 "질문 5" 표에서)

| 무엇 | PE_emulator 지금 `[추측]` | 진짜 커널 `[정적]` | 같은가 |
|---|---|---|---|
| 결과값 | `0x102` | 3번 표 `WaitKey 0x102` → `WaitStatus` → 그대로 반환 | **같다** |
| 언제 확인 | 스레드를 넘길 때마다 | **째깍마다 (계산대마다)**. 째깍엔 표시만, 처리는 초인종의 잡무 단계 | **다르다** |
| 확인 단위 | 정확한 시각 비교로 보임 | 손 = 26.2ms 칸. 비 HR 표는 더 늦게 터질 수 있음 (값 동적) | **다르다** (에뮬레이터는 정확한 비교여도 결과 같음, 값만 다름) |
| 처리 순서 | "깨운 뒤 고름" | 잡무 (알람) **먼저** → 몫 끝 → 고르기 | **같은 방향** |
| 깨우는 조건 | (모름) | `State == 5` 일 때만. 이미 신호로 깼으면 지나침 | 필요 |
| 깨운 뒤 일 | "깸 표시" | 진 표 떼기 · `AdjustReason 1` · `AdjustIncrement 0` · 준비 줄 | **모자랄 수 있다** (진 표 떼기) |
| 신호로 깼을 때 알람 | (모름) | `TimerActive` 면 `KiCancelTimer` | 필요 |

#### 왜 이 차이가 결과를 바꿀 수 있나

결과서·run 문서가 짚은 것만:

1. `[정적]` 진짜는 **째깍이 와야** 알아챈다. 째깍이 안 오면 안 깨운다.
2. `[정적]` 거꾸로, **한 직원만 계속 돌아도** 째깍이 마감을 지나는 순간 깨워서 준비 줄에 넣는다. 그다음 고르기 때 후보가 된다.
   - "넘길 때만 확인" 이면 넘기기 전엔 못 깨운다. 진짜는 넘기기와 **상관없이** 깨워서 줄에 세워 둔다.
3. `[정적]` 같은 초인종 안에서 알람이 몫 끝보다 **먼저** 다. 그래서 "몫 끝 고르기" 때 알람으로 깬 직원이 이미 줄에 서 있다.

- 결과서 권고 `[추측: 에뮬레이터 범위]`: "째깍 = 명령 N 개" 로 줄이더라도, **"표시 → 인터럽트 끝에서 처리"** 두 단계로 나눠 두는 게 퀀텀 쪽 (run76) 과 맞다.
- 비유: 교대 때만 마감을 챙기면, 교대가 안 일어나는 동안 알람이 **울리지 못한다.** 진짜는 벽시계가 따로 챙기니 교대와 상관없이 울린다.

### 2.5 마감 걸기 ① — 상대 마감: 시작 때 **한 번** (질문 1(b))

#### 두 단계로 나눠 계산한다 `[정적]`

1. **`KeWaitForSingleObject`** — 반복 지점 `+0x104` **위** 에서 한 번:
   - `+0xdf TEST RCX,RCX` / `+0xe2 JZ +0xfd` → 포인터 NULL 이면 `+0x100` 종류 0 (마감 없음).
   - `+0xe4 CMP dword [RCX+4],0` / `+0xe8 JL +0x3b6` → 위 32비트가 음수면 상대.
   - 상대 **`+0x3b6 ~ +0x3d7`**:
     - `+0x3b6` `InterruptTimeBias` · `+0x3c0` `InterruptTime` · `+0x3c3 SUB` · `+0x3c6` 종류 2 · `+0x3ca` `RelativeTimerBias` · `+0x3d1 ADD` Timeout (음수) · `+0x3d4 SUB`.
     - → **`+0x3d7 MOV [RBP-0x51],RDX`** = `InterruptTime − InterruptTimeBias − RelativeTimerBias + |Timeout|`.
   - `+0x3de JMP +0x104` — 그다음에 반복 지점으로 들어간다.
2. **`KiCommitThreadWait`** — `+0x17a ~ +0x194`:
   - `+0x17a ~ +0x184` `InterruptTimeBias` · `+0x187` `RelativeTimerBias` · `+0x18e ADD` · `+0x191 ADD RCX,R13` (`R13` = 위 값).
   - → **`+0x194 MOV [RDI+0x18],RCX`** (`Timer.DueTime`, 2.2 줄 3).

#### 식을 정리하면

- 1 에서 뺀 보정값 둘을 2 에서 다시 더한다. 그래서 **둘이 지워진다.**
- **`DueTime = 기다림 시작 때 InterruptTime + |Timeout|`** `[정적: 식]`.
- 새 말 — **보정값 (바이어스)**: `RelativeTimerBias` (업무 카드) · `InterruptTimeBias` (공용 게시판). 이번 식에선 넣었다 뺐다 하는 값이라 결과엔 안 남는다.
  - 왜 두 번에 나눠 하는지는 코드가 말해 주지 않는다 (결과서도 `[추측]` 으로만 적었다).
  - 사진에선 둘 다 0 이다 `[관찰]` (2.17).
- 비유: 쉬러 가는 순간 스톱워치를 보고 **"지금 + 4분"** 을 알람에 적는다.

#### 반복해도 안 늘어난다 `[정적]`

- 계산 (`+0x3b6 ~ +0x3d7`) 이 반복 지점 `+0x104` **앞** 에 있다.
- 그래서 `0x100` ("처음부터 다시 확인" 도장, run77) 으로 `+0x891 JMP +0x104` 다시 돌아도, 알람 시각은 처음 값 그대로다.
- 비유: 자다가 쪽지 (APC) 때문에 잠깐 깼다 다시 자도, 알람을 **다시 맞추지 않는다.** "4분" 이 새로 시작되지 않는다.

#### 곁가지 — "허용 지연" (`ThreadTimerDelay`) `[정적]`

- `KiCommitThreadWait +0x102 MOV EAX,[RBX+0x3b8]` → `+0x139 ADD R13,RAX` 로 알람 시각에 더하는 갈래가 있다.
- 조건 (모두 맞을 때만): 상대 마감 · `WaitMode ≠ 0` (`+0x108`) · `KernelApcDisable == 0` (`+0x111`) · `WaitIrql == 0` (`+0x11a`) · `+0xc0` 바이트 0 (`+0x127`) · `ThreadTimerDelay ≠ 0` (`+0x135`).
- 결과서는 이걸 "유저 모드 기다림에 '타이머 뭉치기 허용 지연' 을 더함" 으로 읽었다.
- 에뮬레이터에선 0 으로 둬도 된다 `[추측: 에뮬레이터 범위]`. 사진에서도 0 이다 `[관찰]`.

### 2.6 마감 걸기 ② — 절대 마감: 가게 시계 → 스톱워치로 바꿔 같은 칸에 (질문 1(b))

#### 갈래 `[정적]`

- `KeWaitForSingleObject +0xee ~ +0xfb`: 값을 그대로 `[RBP-0x51]` 에, 종류 1.
- `KiCommitThreadWait +0x170 CMP R12D,2` / `+0x174 JNZ +0x4d6` → **`+0x4e3 KiComputeDueTime(Timer, 값, 0, &손)`** (ret `+0x4e8`).
  - 돌려주는 `AL`: **0 = 이미 만기**, **1 = 넣어도 됨**.

#### `KiComputeDueTime` 안 — 한 걸음씩 `[정적]`

1. `+0x31 TEST EAX,EAX` / `+0x33 JS +0x6e` — 값이 음수 (상대) 면 상대 식으로. 양수면 아래로.
2. `+0x35 OR R10B,1` — **`Absolute` 표시** 를 켠다.
3. `+0x39 ~ +0x4b` — `SystemTime − 값` 을 구한다.
4. `+0x57 JS +0x69` — 결과가 **0 이상** (`SystemTime ≥ 값`) 이면 **이미 만기**:
   - `+0x5c` 머리만 쓰고 · `+0x60` `DueTime = 0` · `+0x5e` `EAX = 0` → 반환.
5. 아직 미래면:
   - `+0xbf ~ +0xd1` → **`+0xd8 MOV [R11+0x18],R8`** — **`DueTime = InterruptTime + (값 − SystemTime)`**.
   - `+0xe7` 손 · `+0xeb` `Inserted` · `+0xf3` `Hand` · `+0xf7` 손을 호출자에게 → `EAX = 1`.

- 허용 지연 갈래 (`+0x6e ~ +0xb8`) 는 셋째 인자를 0 으로 불러서 이 길에선 안 탄다 `[정적: 인자 0]`.
- 돌아와서: `+0x4e8 TEST EAX` / `+0x4ea JZ +0x395` (만기 → 2.8) 아니면 `+0x4f0 ESI = 1` → `+0x4f5 JMP +0x1c4` 로 상대 쪽과 **합류**.

#### 그래서

- 절대 마감도 결국 **스톱워치 눈금** 으로 바뀌어 같은 `DueTime` 칸에 들어간다.
- 비유: "오후 3시에 깨워 줘" 라고 하면, 지금 가게 시계를 보고 **3시까지 남은 시간** 을 계산해 스톱워치 알람으로 바꿔 맞춘다. 이미 3시 (또는 그 뒤) 면 알람을 맞추지 않고 "이미 지남" 으로 돌아온다.
- 조심: 여기서는 `SystemTime ≥ 값` (같아도) 이 만기다. 2.8 의 "들어갈 때 검사" 는 `SystemTime > 값` (엄격) 이다. 그래서 딱 같은 시각은 2.8 첫째 겹을 지나 여기 (둘째 겹) 에서 걸린다.

### 2.7 마감 걸기 ③ — `KiCommitThreadWait` 에서 보관대에 꽂기 (질문 1(a))

#### 순서 `[정적]` (`RBX` = 직원, `R12D` = 마감 종류, `R13` = 값, `RDI` = 알람)

run77 해설 2.6 표의 **알람 줄만** 펼친 것이다.

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x38 TEST R8D,R8D` / `+0x3b JNZ +0x23d` → **`+0x23d BTS dword [RCX+0x74],9`** | `TimerActive = 1` (마감 있을 때만, 함수 **맨 앞**) | 업무 카드에 "알람 켜 둠" |
| **`+0x242 OR dword [RCX+0x100],0x40000080`** → `+0x24c JMP +0x41` | 알람 **헤더 락** (비트 7) + **`Inserted`** (비트 30) 를 미리 켬 | 알람 자물쇠 잠그고 "꽂힘" 표시부터 |
| `+0x9a LEA RDI,[RBX+0x100]` | `RDI` = 알람 | |
| (run77 줄: `+0x5b` · `+0x97 ~ +0xa3` · `+0xbf` 도장 · `+0x160 Preempted = 0`) | | |
| `+0x167 TEST R12D` / `+0x16a JZ +0x216` | 마감 없으면 알람 일 **전부 건너뜀** (이때 `TimerActive` 도 안 켜졌다) | |
| **`+0x194`** (상대, 2.5) / **`+0x4e3`** (절대, 2.6) | `DueTime` | 울릴 시각 적기 |
| `+0x198 SHR RCX,0x12` · `+0x19c` · `+0x19f` | 손 | 칸 번호 뽑기 |
| `+0x1a2 ~ +0x1bc` (`+0x1aa` `Absolute` 지움 · `+0x1ae` `Hand` · `+0x1bc MOV [RDI],EAX`) | 머리 고쳐 쓰기 | |
| `+0x1c4 ~ +0x1e4` | `TimerType` (어느 보관대, 아래) | |
| `+0x1eb ~ +0x1ef` → **`+0x1f2`** (ret `+0x1f7`) **`KiInsertTimerTable(Prcb, Thread+0x100, 0, 손)`** | 표에 넣기 | **보관대 칸에 꽂기** |
| `+0x1f7 TEST AL,AL` / `+0x1f9 JZ +0x395` | `AL = 0` 이면 이미 만기 (2.8) | |
| `+0x1ff` / `+0x209` → `+0x46c KiTraceSetTimer` | ETW 켜졌을 때만 (이름만) | |
| **`+0x20f AND.LOCK [RDI],0xffffff7f`** | 헤더 락 풀기 | 알람 자물쇠 풀기 |
| `+0x216` → **`+0x220`** (ret `+0x225`) **`KiSwapThread`** | 잔다 | |

- 왜 자물쇠를 맨 앞에서부터 쥐나: 꽂는 동안 다른 계산대의 만료 처리가 이 알람을 못 건드리게 한다 `[정적]`. 만료 쪽 `KiProcessExpiredTimerList +0x96 BTS.LOCK [타이머],7` 이 이 락이 풀릴 때까지 기다린다 (`+0x9e JC`).
- 칸 안 자리 `[정적]` (`KiInsertTimerTable`): 칸 안은 **만기 오름차순** 이다.
  - 새 시각이 첫 항목 이하면 맨 앞 (`+0xc4 ~ +0xcb`).
  - 아니면 첫 항목에서 가까우면 앞에서부터, 멀면 뒤에서부터 자리를 찾는다 (`+0xf7 ~ +0xfd`).
- 비유: 쉬러 가기 전에 알람에 자물쇠를 채우고 → 울릴 시각을 적고 → 보관대 칸에 꽂고 → 자물쇠를 풀고 → 계산대를 내놓는다.

#### `TimerType` 은 "해상도 요청 없음" 이면 1 (flow-writer 덧붙임 ③) `[정적]`

1. `+0x1c4 CMP KiGlobalTimerResolutionRequests,0` / `+0x1cb JZ +0x35c`.
2. `+0x363 PsTimerResolutionActive(프로세스)` → `+0x368` / `+0x36a`.
3. 거짓이면 `+0x370 MOVZX EDX,SI` (`SI` = 1, `+0x1b7`) → `+0x1e4 MOV word [Timer+0x3a],DX`.
4. 둘 중 하나라도 있으면 0.

- 새 말 — **해상도 요청**: 두 이름으로 보아 "타이머를 더 촘촘하게 해 달라" 는 요청 수·상태로 보인다 `[추측: 이름]`.
- 사진 P1b `TimerType 1` 과 맞는다 (2.17). 에뮬레이터는 1 하나로 둬도 된다 `[추측: 에뮬레이터 범위]`.

#### `Timer2` 는 안 쓴다 `[정적]`

- `KiCommitThreadWait` 의 `_calls.tsv` 줄에 `KiInsertTimer2` 계열이 없다. 쓰는 표는 계산대 장부 `+0x4100` 의 일반 표다.
- `Timer2` (고해상도 타이머로 보이는 쪽) 는 `KiCheckForTimerExpiration` · `KiRetireDpcList +0xcc6` · `KiTimerExpirationDpc +0x161` 이 **곁다리로** 같이 확인할 뿐이다.

#### 가게 운영 카드 `ThreadTimerSets` 를 올리는 줄 (flow-writer 덧붙임 ②) `[정적]`

- `KiCommitThreadWait +0xf9` `SIL = 0` → 상대 마감 (`+0xfc`) · `WaitMode ≠ 0` (`+0x108`) · `KernelApcDisable == 0` (`+0x111`) · `WaitIrql == 0` (`+0x11a`) · `ApcState` 안 바이트 `+0xc0 == 0` (`+0x127`) 이면 `+0x130 ESI = 1`.
- → `+0x1b1 TEST SIL` / `+0x1be JNZ +0x3a8` → **`+0x3af ADD.LOCK [Process+0x700],ESI`** (+1).
- 뽑은 ntoskrnl `.asm` 가운데 이 칸을 쓰는 줄은 **이것 하나** 다 (`grep '+ 0x700]'`).
- 이름 그대로 "스레드 타이머를 건 횟수" 로 보이는 칸이다 `[추측: 이름]`. 사진에선 여덟 장 모두 0 `[관찰]` — 뜻은 2.17 에서.

### 2.8 이미 지난 마감 — 두 겹 (질문 1(c))

"이미 지났으면 안 자고 `0x102`" 는 맞다. 다만 검사가 **두 군데** 있다.

#### 첫째 겹 — 번호표를 걸기 **전** (`KeWaitForSingleObject`) `[정적]`

- 모이는 곳: **`+0x240 MOV R14D,0x102`** → 헤더 락 풀고 `State = 2` → `+0x2c4` 반환 (run77). **안 잔다.**
- 들어가는 길 셋:

| 마감 | 줄 | 조건 |
|---|---|---|
| 절대 값 `0` (`WaitForSingleObject(h, 0)` 모양) | `+0x237 TEST R8,R8` / `+0x23a JNZ` 안 탐 → `+0x240` | 값이 0 이면 무조건 |
| 절대 값 ≠ 0 | `+0x60c ~ +0x616` `SystemTime` → `+0x619 JMP +0x356` → **`+0x356 CMP` / `+0x359 JA +0x240`** | `SystemTime > 값` (엄격) |
| 상대 | `+0x228 JZ +0x2ff` → `+0x2ff ~ +0x326` `지금' = InterruptTime − RelativeTimerBias − InterruptTimeBias` · `+0x352 LEA RAX,[R8+RDX]` (마감 + 조건부 허용 지연) → **`+0x356` / `+0x359 JA`** | `지금' > 마감` (엄격) |

- **신호 검사 뒤** 다. 신호등이 이미 초록이면 (`+0x211 TEST EDX,EDX` / `+0x213 JG +0x5f0`, 뮤턴트 `+0x20b`) 마감 비교 (`+0x224 ~`) 에 오기 전에 가져가고 `0` 으로 끝난다.
  - 그래서 **마감이 0 이어도 신호면 `0x102` 가 아니라 `0`** 이다.
- **같은 시각** (`==`) 은 엄격 비교라 이 겹을 **통과** 해서 번호표를 걸고 둘째 겹으로 간다.
- 비유: 쉬러 가기 전에 먼저 신호등을 본다. 초록이면 그냥 지나간다. 빨강인데 알람 시각이 **이미 확실히 지났으면** 쉬러 가지 않고 "시간 초과" 로 돌아간다.

#### 둘째 겹 — 번호표 걸고 커밋한 **뒤**, 보관대에 꽂을 때 `[정적]`

1. 절대: `KiComputeDueTime` 이 0 (2.6) → `+0x4ea JZ +0x395`.
2. 상대·절대 공통: `KiInsertTimerTable` 이 `AL = 0` → `+0x1f9 JZ +0x395`.
   - `AL = 0` 이 되는 줄: **`+0x2a5` `InterruptTime` · `+0x2a8 CMP R13,RAX` / `+0x2ab JBE +0x39b`** (`R13` = `DueTime`) — **`DueTime ≤ 지금`** 이면 → `+0x3b3 KiRemoveEntryTimer` (방금 꽂은 걸 도로 뺌) → `AL = 0`.
   - **주의**: 이 검사는 새 알람이 칸 **맨 앞** 이 될 때만 탄다 (`+0x1fe TEST BL,2` / `+0x201 JNZ`). 칸에 더 이른 알람이 이미 있으면 검사 없이 `AL = 1`. 그 이른 알람도 지났을 테니 다음 째깍이 칸을 보고 처리한다 `[추측]`.
3. `+0x395 ~ +0x39b` → **`+0x39e KiTimerWaitTest(Prcb, Thread+0x100, 0)`** (ret `+0x3a3`) — **자기 알람을 자기가 울린다.**
   - 2.12 의 같은 함수다 → 자기 `State` 5 → 7, `WaitStatus = 0x102`, 깨운 직원 명단에 **자기** 를 올림.
4. `+0x3a3 JMP +0x216` → `KiSwapThread` → `+0x14` / `+0x2a` 명단이 비어 있지 않음 → `+0x516 KiProcessThreadWaitList(…, 1, 0, 2)` → 고르기.
   - 새로 고른 게 자기 자신이면 문맥 전환 없이 `State = 2` 로 계속 (`KiSwapThread +0xfa` / `+0xfd`). 정말 자기 자신이 골리는지는 런타임 일이다 `[추측]`.
5. `+0x25c` `WaitStatus` (`0x102`) 를 돌려준다.

- 비유: 알람을 칸에 꽂으려는데, 그 칸 맨 앞 자리이고 시각이 이미 지났다. 그러면 꽂은 걸 도로 빼고 **자기 손으로** 알람을 울린 뒤 (자기 어깨를 두드림), 교대 담당에게 갔다가 곧바로 돌아온다.
- 정리 (결과서): "지난 걸 알면 안 자고 `0x102`" 는 맞다. 다만 **같은 시각 같은 경계** 는 `KiSwapThread` 까지 갔다가 바로 돌아오는 길로 처리한다.

### 2.9 알아채기 — 째깍마다 보고, 스위치만 켠다 (질문 2)

#### 사슬 — run76 사슬의 곁가지 `[정적]`

- `HalpTimerClockInterrupt +0x103 KeClockInterruptNotify` → (시계 주인) `+0x2ba KiUpdateTime` → `+0x38b KiUpdateRunTime` / (그 밖) `+0x448 KiUpdateRunTime` → `+0x96 KeAccumulateTicks` → **`+0x28` (ret `+0x2d`) `KiCheckForTimerExpiration(Prcb, 6번째 인자)`**.
- `KeAccumulateTicks +0x23 CMP EDX,4` / `+0x26 JZ` — 6번째 인자가 4 면 건너뛴다. `HalpTimerClockInterrupt` 는 0 을 넘긴다 (run76).
- 비유: 째깍마다 하는 **출근부 기록** (run76) 에 "알람 보관대 훑기" 가 곁다리로 붙어 있다.

#### `KiCheckForTimerExpiration` 안 — 한 걸음씩 `[정적]` (`R14` = `&PRCB+0x38bc`, `R12` = `InterruptTime`)

1. **`+0x5c TEST byte [R14],8` / `+0x66 JNZ`** — "알람 확인" 스위치가 **이미 켜져 있으면** 다시 안 훑는다.
2. `+0x6c ~ +0x89` — 직렬화 (`KiSerializeTimerExpiration`) 를 안 하면 **자기 계산대 보관대** 를 본다.
3. `+0xd7 ~ +0xe3` — 훑을지 정하는 비교. **안 풀었다** `[공백]` → Q146 ②.
4. 훑기:
   - `+0x2f8 SHR R9,0x12` — **지금의 손**.
   - 마지막으로 훑은 손 (`PRCB+0x8310`) 부터 지금 손까지 최대 256칸.
   - `+0x32f` 칸의 **가장 이른 만기** 를 읽고 → 표 0: **`+0x33c CMP R12,RCX` / `+0x33f JNC`** — **지금 ≥ 만기** 면 "지난 것 있음".
   - 표 1 은 `KeNonHrTimeIncrement` 등을 더해 **더 늦게** 지난 것으로 본다 (`+0x381 ~ +0x3a8`, 값은 동적).
5. 지난 것이 있으면 → `+0x15d` / `+0x160 JNZ +0x3c8` → `+0x3c8 EDX = 8` · `+0x3cd RCX = R14` · **`+0x3d0 KiSetDpcRequestFlag`** (2.2 줄 2).
6. `+0x3d5 TEST AL,0xa9` / `+0x3d7 JNZ` — 옛 값에 요청 비트가 이미 있었으면 그만.
7. `+0x3dd` / `+0x3e1` `NestingLevel` 로 초인종 거는 법을 고른다 (run76 해설 2.7 의 두 갈래와 같다):
   - ≠ 0 (시계 인터럽트 안, 보통) → **`+0x3e7 InterruptRequest = 1`** — run76 몫 끝 표시와 **같은 칸**.
   - == 0 → `+0x4e1 EDX = 0x2f` · `+0x4f1 HalpInterruptSendIpi` (자기 계산대에 벡터 `0x2f`).
8. 지난 게 없으면 `+0x259 KiSetClockTimer` 등으로 다음 째깍 시각을 다시 맞춘다 (이름만).

- **알람은 여기서 하나도 안 꺼낸다.** 표시만 한다.
- 비유: 째깍 담당은 "지난번 본 칸" 부터 "지금 바늘 위치" 까지 칸 겉 메모만 훑는다. 하나라도 지났으면 **"알람 확인" 스위치** 를 켜고 "끝나면 초인종" 메모를 남긴다. 스위치가 이미 켜져 있으면 훑지도 않는다.

#### 퀀텀과 같은 꼴

| | 째깍 때 (표시) | 초인종 때 (일) |
|---|---|---|
| 몫 끝 (run76) | `KiUpdateRunTime +0x16a` 깃발 (`+0x38b9`) | `KiDispatchInterrupt +0x1c3 KiQuantumEnd` |
| 알람 (이번) | `KiCheckForTimerExpiration +0x3d0` 스위치 8 (`+0x38bc`) | `KiRetireDpcList +0xca4 KiTimerExpiration` |
| 초인종 거는 법 | `+0x17b` / `+0x4a8` | `+0x3e7` / `+0x4f1` |

- PE_emulator: "넘길 때마다 확인" 은 **틀림** — 째깍마다, 그리고 표시와 처리가 둘로 (run 문서 표 2).

### 2.10 처리 ① — 초인종의 잡무 단계로 (질문 3(a))

#### 사슬 — 지도에 안 보이는 이음새 `[정적]`

1. `KiDispatchInterrupt +0x60` (ret `+0x65`) `KiSwapStacksAndRetireDpcList` — run76 의 "DPC 먼저".
2. 그 함수 `+0xc` 에서 **꼬리점프** — `CALL` 이 아니라 `JMP` 로 넘긴다 → DPC 스택에서 `KiRetireDpcList` 를 부르는 조각 (rva `0x6b6440`) 의 `+0x0` → **`KiRetireDpcList`**.
3. 자리 지킴이 (Idle 스레드) 가 돌 때는 `KiIdleLoop +0x99` 도 `KiRetireDpcList` 를 부른다.

- 왜 지도에 없나: `_calls.tsv` 에는 `KiRetireDpcList` 를 부르는 줄이 **없다**. 부르는 쪽 (`KiIdleLoop` · DPC 스택 조각) 의 `.asm` 을 안 뽑아서다.
  - 그래서 **`refs`** (누가 이 함수를 가리키나) 로 찾았다: `_refs/KiRetireDpcList.tsv`. `KiTimerExpiration` 을 부르는 곳 둘 (`KiRetireDpcList +0xca4` · `KiTimerExpirationDpc +0x135`) 도 `refs` 로.

#### `KiRetireDpcList` — 한 걸음씩 `[정적]`

| 줄 | 하는 일 | 비유 |
|---|---|---|
| **`+0x406 XCHG word [RBX+0x38bc],DI`** | 요청 비트를 **전부 가져오며 0** 으로 | 스위치판을 통째로 떼어 와 내리기 |
| **`+0x40d TEST DIL,8` / `+0x411 JNZ +0xbca`** | 비트 8 이 있었나 | "알람 확인" 스위치 봤나 |
| `+0xbd4 AND DI,…` | 가져온 값에서 비트 8 지움 | |
| `+0xbd7` (ret `+0xbdc`) `KiSelectActiveTimerTable` | 쓸 표 → 직렬화 안 하면 자기 표 `PRCB+0x4100` | 우리 계산대 보관대 |
| `+0xc7c STI` | 인터럽트를 다시 켜고 | |
| **`+0xca4`** (ret `+0xca9`) **`KiTimerExpiration`** | 인자: `RCX` Prcb · `EDX` = `[PRCB+0x8310]` (마지막으로 훑은 손) · `R8` = `InterruptTime` · `R9B` 표 1 도 훑을지 | 보관대 훑기 (2.11) |
| `+0xcc6` (ret `+0xccb`) `KiTimer2Expiration` | `ClockOwner ≠ 0` 일 때 (이름만) | |

#### 순서 — 알람이 몫 끝보다 먼저 `[정적]`

- `KiDispatchInterrupt` 는 이 DPC 처리 **뒤에** `+0x7f` 에서 몫 끝 깃발을 본다 (run76).
- 그래서 같은 초인종 안에서 **알람 → 몫 끝 고르기** 순서다. 알람으로 깬 직원은 이미 준비 줄이나 `NextThread` 에 올라간 채 고르기를 맞는다.

#### run76 해설의 `[추측]` 하나가 닫혔다 — 반은 맞고 반은 다르다

- run76 해설 2.9 표: "DPC 를 먼저 다 돌린다 (**타이머 만료 DPC 포함 `[추측]`**)".
- 이번 답 `[정적]`:
  - **잡무 단계 (`KiRetireDpcList`) 에서 처리하는 건 맞다.**
  - 그런데 바구니에 넣어 둔 **DPC 일감 하나** 가 도는 게 아니다. **스위치 8** 을 보고 `KiRetireDpcList` 가 **직접** `KiTimerExpiration` 을 부른다.
- 비유: 잡무 바구니를 비우는 사람이, 바구니 안 카드가 아니라 **스위치판** 을 보고 "아, 알람 확인" 하며 보관대로 간다.

#### 줄기 밖 — 알람용 DPC 카드 (`KiTimerExpirationDpc`) `[정적]` + `[공백]`

- 계산대마다 `PRCB+0x83f0` 에 DPC (`KDPC`) 가 하나씩 부팅 때 등록돼 있다: `KeInitializeTimerTable +0x45 LEA RDI,[RBX+0x83f0]` · `+0x4f LEA RDX,[KiTimerExpirationDpc]` · **`+0x61`** (ret `+0x66`) `KeInitializeDpc`.
- 이 DPC 가 하는 일: 지금의 손을 `PRCB+0x8310` · `+0x8314` 에 적고 (`+0xdf` · `+0xe5`) → `+0x135 KiTimerExpiration`.
- **누가 이 DPC 를 바구니에 넣는지는 못 찾았다** `[공백]`. 뽑은 `.asm` 497개를 `grep 0x83f0` 해도 `KeInitializeTimerTable` 뿐이다.
- 비유: 바구니용 "알람 훑기" 카드가 계산대마다 한 장씩 미리 만들어져 있다. 그런데 이번 줄기에선 그 카드를 안 쓰고, 카드를 바구니에 넣는 사람도 아직 못 찾았다.
- → Q146 ① (정적), Q104 동적 4.

### 2.11 처리 ② — 보관대 훑기: `KiTimerExpiration` → `KiProcessExpiredTimerList` (질문 3(a))

#### `KiTimerExpiration` — 줄기만 `[정적]` (2816B 전부는 안 풀었다)

1. `+0xd7 SHR RBX,0x12` — 지금의 손.
2. `+0x13d ~ +0x155` — 훑을 칸 수 = 지금 손 − 시작 손 + 1 (256 넘으면 줄임).
3. **칸마다**:
   - 칸 줄이 비었으면 지나감 (`+0x1d1` / `+0x1d5`).
   - 칸 락 (`+0x331 BTS.LOCK [RBX],0`).
   - 칸 **첫 알람** 의 만기 ≤ 지금인가: **`+0x36c MOV RAX,[RCX-8]` / `+0x379 CMP RAX,RSI` / `+0x37c JBE +0x3f7`** (`RSI` = 지금).
   - 맞으면 `+0x3f7` 부터:
     - 머리 바이트 3 을 `0xc0 | 목록 번호` 로 (`+0x406 ~ +0x423`) — **`Inserted`·`Expired` 둘 다 켜고** `Index` 에 만료 목록 번호.
     - `+0x43f XCHG` — `PRCB+0x4100` 의 64칸 **만료 목록 머리** 에 단다.
     - 칸 줄에서 뗀다 (`+0x483 ~ +0x4a2`).
   - 아니면 `+0x37e` 칸의 "가장 이른 만기" 를 그 값으로 고쳐 놓고 끝.
4. → **`+0x20e`** (ret `+0x213`) **`KiProcessExpiredTimerList`** (표 0). 표 1 은 `+0x2ad`. 만료 목록이 64칸 차면 중간에 비운다 (`+0x95b CMP R8D,0x40` → `+0x949` · `+0x97c`).

- 한 칸에 지난 알람이 **여럿** 일 때 한 번에 다 꺼내는지는 못 정했다 `[공백]`. 안 꺼내진 것은 칸 메모가 아직 지난 값이라 다음 째깍이 다시 본다 `[추측]` → Q146 ⑤.

#### `KiProcessExpiredTimerList` `[정적]`

1. `+0x8d XCHG` — 만료 목록 머리 하나를 가져온다.
2. `+0x96 BTS.LOCK [타이머],7` — 알람 자물쇠. 거는 쪽 (2.7, `KiCommitThreadWait +0x242`) 이 쥐고 있으면 `+0x9e JC` 로 기다린다.
3. **`+0xba`** (ret `+0xbf`) **`KiTimerWaitTest`** — 알람 하나 울리기 (2.12). (`+0x483` 은 ETW 켜진 변형.)
4. 끝에 `+0xe2 CMP [RDI+0x2d08],0` / `+0xea JZ` — 깨운 직원 명단이 비어 있지 않으면 → **`+0xfc`** (ret `+0x101`) **`KiProcessThreadWaitList(Prcb, 1, 0, 2)`** (2.14).

- 비유: 보관대 칸을 "지난번 본 칸" 부터 지금 바늘까지 하나씩 연다. 칸 맨 앞 알람 시각이 지났으면 꺼내서 **"울린 알람" 상자** (64개) 에 담고 "울려서 꺼냄" 표시를 한다. 그다음 상자 안 알람을 하나씩 울리고, 마지막에 깨운 직원 명단을 정리한다.

### 2.12 처리 ③ — `KiTimerWaitTest`: 알람 울림 → 번호표마다 어깨 두드리기 (질문 3(b))

인자: `KiTimerWaitTest(Prcb, Timer, 인자)` (`RDI` = 알람).

#### 한 걸음씩 `[정적]`

1. **`Dpc` 풀기** (`+0x1c ~ +0x4f`): `Timer.Dpc` 는 그대로 적혀 있지 않고 **섞어 둔 값** 이다 (`XOR KiWaitNever` · `ROL` · `XOR Timer` · `BSWAP` · `XOR KiWaitAlways`).
   - 새 말 — **난독화 저장** (결과서 말): 주소를 비밀 값 둘 (`KiWaitNever`·`KiWaitAlways`) 로 섞어 적어 두는 것. 비유: 주소를 **암호로** 적어 둔 메모.
   - 스레드 알람은 `KeInitThread +0x177 ~ +0x193` 이 같은 키로 **0 을** 섞어 넣었다 → **풀면 0** `[정적: 식]` → 끝에 `+0x348 TEST RSI` / `+0x34b JZ` 로 DPC 일을 안 한다.
2. `+0x66 MOVSXD RAX,[RDX+0x3c]` / `+0x6c JZ +0x1d7` — `Period 0` (한 번짜리) 이면 다시 걸기를 건너뛴다. (반복 알람이면 `+0x1c7 KiInsertTimerTable` 로 다시 꽂음.)
3. **`+0x1d7 MOV [RDI+4],R11D`** — **`SignalState = 1`** (울렸음).
4. `+0x1e2 CMP R12B,8` / `+0x1e6 JNZ` — 종류로 갈린다:
   - **알림형 (8, 스레드 알람)**: 매단 번호표를 **모두** 앞에서부터.
     - `WaitType 1` 이면 **`+0x207 MOVZX R8D,word [RAX+0x12]`** (`WaitKey`) → `+0x20c JMP +0x31c` → **`+0x325 KiTryUnwaitThread(Prcb, 표, WaitKey, 0)`** (2.2 줄 1). 반환값은 안 본다.
     - 표를 하나씩 떼지 않고, 끝에 **`+0x33d ~ +0x345`** 에서 `WaitListHead` 를 **머리째 비운다.**
   - **동기형 (9)**: 표를 하나씩 떼며 `+0x24e KiTryUnwaitThread` → 실패면 다음 표 (`+0x258`), 성공이면 **`+0x25a SignalState − 1`** → 0 이면 끝 (`+0x25e`). run77 `KeSetEvent` 자동 루프와 같은 꼴.
5. `KiTryUnwaitThread` 안 (run77 해설 2.9): `+0x33` 직원 자물쇠 → **`+0x57 ~ +0x60` `State ≠ 5` 면 지나침** → (깨움) **`+0x149 State = 7`** · `+0x161` 깨운 직원 명단 push · **`+0x168 WaitStatus = R12`** (= `0x102`).
6. 끝 **`+0x398 ~ +0x3a5`**: 한 번짜리 (`R13B = 0xff`, `+0x56`) 면 헤더 락 비트와 바이트 3 (`Index`·`Inserted`·`Expired`) 을 **전부 지운다** — 보관대에서 완전히 빠졌다는 표시.

- 스레드 알람에 매단 번호표는 사실상 **한 장** (3번 표) 이다 (결과서 규칙 9).
- 비유: 알람이 울리면 (`SignalState 1`), 알람 담당이 매달린 번호표를 보고 **표에 적힌 번호** 를 들고 어깨 두드리기 담당에게 맡긴다. 알림형이라 매단 표 **모두**. 다 하고 나면 표 줄을 통째로 비운다.

### 2.13 `0x102` 의 정체 — 3번 표에 미리 적힌 번호 (질문 3(b))

#### 직원을 뽑을 때 이미 적혀 있다 `[정적]` (`KeInitThread`)

1. `+0x172` (ret `+0x177`) `KeInitializeTimer(Thread+0x100)` — `Type 8` (알림형), 번호표 줄 비움, `DueTime`·`Period`·`Processor` 0.
2. `+0x177 ~ +0x193` — `Dpc` 자리에 "0 을 섞은 값" (2.12).
3. **`+0x197 LEA RAX,[RDI+0x1d0]`** — `WaitBlock[3]` (= `0x140 + 3 × 0x30`, **네 번째 번호표**).
4. **`+0x19e MOV dword [RAX+0x10],0x1020401`** — 표의 `+0x10 ~ +0x13` 네 바이트를 한 번에 쓴다.
5. `+0x1a5 ~ +0x1dd` — 이 표를 알람의 `WaitListHead` (`Thread+0x108`) 에 매단다.

`0x01020401` 을 칸으로 나누면 (결과서·run 문서 풀이 "1 · 4 · `0x102`"):

| 자리 | 칸 | 값 | 뜻 |
|---|---|---|---|
| `+0x10` | `WaitType` | 1 | 하나라도 풀리면 깸 (run77) |
| `+0x11` | `BlockState` | 4 | 걸려 있음 (run77) |
| `+0x12 ~ +0x13` | **`WaitKey`** (16비트) | **`0x102`** | `STATUS_TIMEOUT` |

#### 울릴 때 이 번호가 결과 칸으로 `[정적]`

- 만기 → `KiTimerWaitTest +0x207` 이 이 `WaitKey` 를 꺼낸다 → `KiTryUnwaitThread +0x168 MOV [RBX+0xc8],R12` → **`WaitStatus = 0x102`**.
- → `KiSwapThread` 가 돌려주고 → `KeWaitForSingleObject` 가 **그대로** 반환 (run77).
- 신호등 쪽은 0번 표에 `WaitKey = 0` 을 적어 건다 (`KeWaitForSingleObject +0x1e2`) → `WaitStatus = 0`.
- **같은 길, 번호만 다르다.** 비유: 0번 표엔 그때그때 "0번 물건" 이라고 적어 신호등에 걸고, **3번 표엔 처음부터 "시간 초과" 가 인쇄돼서** 알람에 매달려 있다. 어깨 두드리기 담당은 어느 쪽이든 **표에 적힌 번호를 옮겨 적을 뿐** 이다.

#### 번호표 장수에 안 든다 `[정적]`

- 3번 표는 `WaitBlockCount` (`+0x24b`) 에 안 든다. 한 물건 기다림이면 `WaitBlockCount = 1` (`KeWaitForSingleObject +0x7d2`), `WaitBlockList` 는 0번 표만 가리킨다.
- 알람 몫은 **늘 3번 표** 다. 그래서 flow.md 4장 Q145 ② (여러 물건 기다림) 에 "번호표 4장 중 3번 표는 알람 몫이라 자기 표는 3장" 이 붙었다. `KeWaitForMultipleObjects` 본문은 안 읽어서 이건 `[추측]` 이다 (3절).

#### 헷갈리기 쉬운 곳 — `0x102` 가 상수로 쓰인 곳도 있다

| 길 | `0x102` 는 어디서 오나 | 근거 |
|---|---|---|
| 자다가 알람이 울려 깸 (2.12) | 3번 표 `WaitKey` | `KiTimerWaitTest +0x207` → `KiTryUnwaitThread +0x168` |
| 꽂는 순간 이미 만기 — 자기 알람 (2.8 둘째 겹) | 3번 표 `WaitKey` (같은 `KiTimerWaitTest`) | `KiCommitThreadWait +0x39e` |
| 걸기 전에 이미 지남 (2.8 첫째 겹) | **코드에 박힌 값** | `KeWaitForSingleObject +0x240 MOV R14D,0x102` |

- 그러니 "`0x102` 는 상수가 아니다" 는 **알람 길** 얘기다. 첫째 겹은 번호표를 거치지 않는다.
- PE_emulator 규칙 6 (2.18) 의 "타이머 기다림 블록은 스레드마다 하나 미리 있고 `WaitKey=0x102`" 가 이 표 얘기다.

### 2.14 깨운 다음 — 진 표 떼기, 가산점 0, 준비 줄 (질문 3(b)(c))

#### 진 쪽 번호표는 누가 떼나 `[정적]`

- `KiTryUnwaitThread` 는 표의 `BlockState` 만 +1 하고 **줄에서 안 뗀다** (run77).
- 알람이 이기면: 신호등 E 의 줄에 B 의 **0번 표가 남아 있다.** 이걸 떼는 게 **`KiProcessThreadWaitList`** 다 — 깨운 쪽 (`KiProcessExpiredTimerList +0xfc`) 이 부른다.

| 줄 | 하는 일 |
|---|---|
| `+0x1e` | 깨운 직원 명단 (`PRCB+0x2d08`) 을 가져옴 |
| `+0x5a` · `+0x62` | 직원마다 `WaitBlockCount` · `WaitBlockList` |
| **`+0x74 MOVZX EAX,[RSI+0x11]` / `+0x78 CMP AL,5` / `+0x7a JC +0x141`** | `BlockState < 5` (이긴 표가 아님) 이면 |
| `+0x141` · `+0x148` | 그 표의 물건 · 물건 헤더 락 |
| **`+0x155 CMP byte [RSI+0x11],4` / `+0x159 JNZ`** | `BlockState == 4` 인 것만 |
| **`+0x259` / `+0x25c`** | 물건 줄에서 **뗀다** (`RemoveEntryList`) |
| `+0x260` · `+0x269 JMP +0x80` | 락 풀고 다음 표 |

- `BlockState` 를 쓰는 줄은 없다 (flow-writer `grep`).
- 왜 꼭 떼야 하나 (결과서): 안 떼면 나중에 그 물건을 신호할 때 **죽은 번호표** 를 만난다.
- 3번 표 자신은 `KiTimerWaitTest` 가 줄을 머리째 비우면서 떨어지고, 깨난 직원이 `KiSwapThread +0x37f ~ +0x391` 에서 다시 매단다 (결과서, 2.15).
- 비유: 알람으로 깬 직원의 번호표가 신호등 줄에 아직 걸려 있다. 깨운 직원 명단을 정리하는 쪽이 그 표를 **떼어 준다.** 안 떼면 나중에 신호등이 켜졌을 때 빈자리 번호표를 두드리게 된다.

#### 준비 줄로 `[정적]`

- `KiProcessThreadWaitList(Prcb, 1, 0, 2)` → 직원마다 `+0x98 AdjustReason = 1` · **`+0x9f AdjustIncrement = 0`** (`R8B`) → **`+0x101 KiDeferredReadySingleThread`** (run75).
- 신호 쪽과 다른 건 **`Increment = 0` 하나** 다.
  - 신호 쪽은 `KeSetEvent` 의 `Increment` 가 `KiExitDispatcher +0x41` 를 거쳐 온다 (run77).
  - 알람 길은 `R8D = 0` 으로 부른다.
- 이 0 일 때 `KiDeferredReadySingleThread +0x6d` 갈래가 정확히 뭘 하는지는 안 읽었다 `[공백]` → Q146 ④ (= Q145 ③ 과 같은 함수).
- 줄 앞/뒤: `Preempted` 가 `KiCommitThreadWait +0x160` 에서 이미 0 이라 **뒤** 로 보이지만, run75 한계 그대로 `[추측]`.
- 비유: 알람으로 깬 직원에게 붙는 "깨움 가산점" 메모는 **+0**. 신호등으로 깨면 담당이 정한 만큼 붙는다.

### 2.15 겹칠 때 — 먼저 잠근 쪽이 이긴다 (질문 4)

#### 겨루는 자리는 하나 — `KiTryUnwaitThread` `[정적]` (run77 해설 2.9)

| 순서 | 줄 | 하는 일 |
|---|---|---|
| 1 | `+0x1e MOV RBX,[RDX+0x18]` | 직원 = 번호표 `Thread` (3번 표도 `KeInitThread +0x73` 이 채움) |
| 2 | **`+0x33 BTS.LOCK [RBX+0x40],0`** | **직원 자물쇠** (스레드 락) |
| 3 | **`+0x57 MOVZX EAX,[RBX+0x184]` / `+0x5e CMP AL,5` / `+0x60 JNZ`** | **`State ≠ 5` 면 지나침** — 이미 다른 쪽이 `State = 7` 로 바꿈 |
| 4 | `WaitRegister.State` 로 갈림 (run77 표) | 이긴 쪽이 `+0x149 State = 7` · `+0x161` 명단 push · `+0x168 WaitStatus` |
| 5 | `+0x194` 락 풀기 · `+0x19c ~ +0x1a7 BlockState + 1` | **이기든 지든 늘** |

- 신호등 담당도 알람 담당도 **같은 길** (같은 함수, 또는 신호등 쪽이 몸에 풀어 넣은 같은 순서) 로 들어온다.
- 비유: 신호등 담당과 알람 담당이 같은 직원 어깨로 동시에 손을 뻗어도, **직원 자물쇠를 먼저 잡은 쪽만** 깨운다. 뒤에 온 쪽은 "이미 일어났네" 하고 아무것도 안 바꾸고 지나간다.

#### 알람이 졌을 때 (신호가 먼저) `[정적]`

1. 알림형이라 `KiTimerWaitTest` 는 `AL` 을 안 보고 그냥 계속한다 (`+0x32a`). 직원은 건드려지지 않는다.
2. 깨난 직원이 `KiSwapThread` 에서 나가면서:
   - `+0x25c` `WaitStatus` 읽기 → **`+0x263 BTR [R13+0x74],9`** (`TimerActive` 끔) → 켜져 있었으면 `JC +0x369` → **`+0x372` (ret `+0x377`) `KiCancelTimer(Thread+0x100, 1)`**.
3. `KiCancelTimer`:
   - `+0x45` 알람 자물쇠.
   - **`+0x50 TEST byte [RDI+3],0xc0` / `+0x54 JZ`** — `Inserted`·`Expired` 둘 다 아님 (= 보관대에 없음) → `AL = 0`.
   - `+0xb4` / `+0xba JS` — `Expired` 면 다른 길 (`+0x321 ~ +0x352`, 만료 목록을 가로채 보고 기다리는 줄 — 본체 안 풀었다) → `AL = 0` 으로 끝날 수 있다.
   - 아니면 칸 줄에서 뗀다 (`+0x11a` / `+0x11d`) · `+0x12b ~ +0x143` `Inserted`·락 지움 → **`AL = 1`**.
   - 즉 **`Inserted` 이고 아직 `Expired` 가 아니면 취소 성공.**
4. `AL = 0` 이면 `+0x379 JNZ` 를 안 타고 **`+0x37f ~ +0x391`**: 3번 표 `BlockState = 4` · 알람 `WaitListHead` 를 다시 이 표 하나로. 다음 기다림에 쓸 수 있게 하는 **원상 복구** 다.
   - 결과서는 **알람이 이겨서 깬 경우** 에도 깨난 직원이 여기서 3번 표를 다시 매단다고 적었다 (`KiTimerWaitTest` 가 줄을 머리째 비웠으므로).

- 왜 꼭 끄나 (결과서): 안 끄면 나중에 같은 직원이 **다른 기다림** 에 들어간 뒤 **옛 마감이 터진다.**
- 비유: 신호등으로 일어난 직원은 자리를 뜨면서 **자기 알람을 끈다** (보관대에서 뺀다). 이미 울려서 꺼내진 뒤라 못 빼면, 3번 표만 원래대로 알람에 다시 매달아 둔다.

#### 신호가 졌을 때 (알람이 먼저) `[정적]`

- 신호등 담당이 줄에서 표를 떼고 (`KeSetEvent +0x313`, run77) `State ≠ 5` 를 보고 실패한다.
- 자동 신호등·동기형 알람·뮤턴트는 실패하면 **`SignalState` 를 안 줄이고 다음 직원으로** 간다 (`KeSetEvent +0x4ab` / `+0x4ae`, `KePulseEvent +0x179`, `KiTimerWaitTest +0x258`). 아무도 못 깨우면 신호는 1 로 남는다.
- 비유: 개찰구 신호등이 "이 사람 이미 일어났네" 하고 빨강으로 안 돌리고 다음 사람을 본다. 아무도 없으면 초록인 채 남는다.

#### 같은 순간이면 — "신호 우선" 은 없다 `[정적]`

- 규칙에 "신호가 이긴다" 같은 건 **없다.** 자물쇠를 먼저 잡은 쪽이다.
- 예외는 **들어갈 때 검사** 하나다: `KeWaitForSingleObject` 신호 검사 (`+0x211`) 가 마감 검사 (`+0x224 ~`) 앞이라, 기다림을 **시작하는 순간** 신호가 이미 있으면 마감이 0 이어도 신호가 이긴다 (2.8).
- 실제 순서는 런타임 일이다. 정적으로는 **규칙** 만 봤다.

#### 커밋 전에 겹치면 `[정적]`

- `WaitRegister.State == 0` 일 때 먼저 온 쪽이 `State = 2` 로 바꾸고 `WaitStatus` 를 써 두면 → `KiCommitThreadWait +0xa3 JNZ +0x251` 로 **안 잔다** (run77 해설 2.6 "커밋 전에 이미 끝난 기다림").
- 알람을 꽂기 **전** 이면 `+0x27a TEST R12D` / `+0x27d JNZ +0x511` → **`+0x511 BTR [RBX+0x74],9`** (`TimerActive` 끔) 으로 알람 일을 아예 안 한다.

#### 꽂는 동안은 알람 자물쇠

- 꽂는 동안 (`+0x242` ~ `+0x20f`) 헤더 락을 쥐고 있어서, 다른 계산대의 만료 처리가 이 알람을 못 건드린다 (`KiProcessExpiredTimerList +0x96` / `+0x9e`) `[정적]`.

- PE_emulator (run 문서 표 4): CPU 하나·한 번에 한 직원이면 자물쇠·CAS 는 필요 없다 `[추측: 에뮬레이터 범위]`. 단 **"`State == Waiting` 일 때만 깨움"** (신호·알람 둘 다) 과 **"신호로 깨면 알람 치우기"** 는 **필요** 하다.

### 2.16 헷갈리기 쉬운 곳 — run77 정정 둘

run 문서 "정정" 절. run77 문서 맨 위에 안내 한 줄이 들어갔다. **run77 해설에도 같은 줄이 있으니 같이 고쳐 읽자.**

#### ⓐ 진 번호표를 떼는 건 깨난 쪽이 아니라 **깨운 쪽**

- run77 문서 (그리고 [run77 해설](run77-thread-wake-해설.md) 2.9 "번호표를 신호등 줄에서 떼는 건 부른 쪽" 마지막 줄) 에 이렇게 적혀 있었다:
  - "여러 물건 기다림의 **남은 표** 는 깨난 쪽이 나가면서 뗀다 (`KiCommitThreadWait +0x2b2 ~ +0x32a`). 한 물건 기다림은 표가 하나라 깨난 쪽이 따로 안 뗀다."
- 고친 것 `[정적]` (결과서 3(b), flow-writer `.asm`):
  1. `KiCommitThreadWait +0x2b2 ~ +0x332` 는 **커밋 전에 끝난 길** (`+0xa3 JNZ +0x251` → `+0x2a0`) 에서만 탄다. run77 해설 2.6 "커밋 전에 이미 끝난 기다림" 의 그 줄이다.
  2. 잠들었다 깬 (커밋된) 기다림의 진 표는 **깨운 쪽** 이 부르는 `KiProcessThreadWaitList` 가 뗀다: `+0x74 ~ +0x7a` (`BlockState < 5`) → `+0x141 ~ +0x269` (`BlockState == 4` 만). 2.14 그 표다.
     - 신호 쪽은 `KiExitDispatcher +0x41`, 알람 쪽은 `KiProcessExpiredTimerList +0xfc` 에서 부른다.
  3. 한 물건 기다림도 **마감이 있으면 표가 둘** 이다 (0번 표 + 알람 3번 표). 알람이 이기면 0번 표가 신호등 줄에 남고, 2 의 줄이 뗀다.
- 그림으로 다시:
  - 신호등 담당이 깨울 때, **자기 신호등 줄** 의 표는 자기가 뗀다 (`KeSetEvent +0x313`, run77).
  - **다른 줄** (다른 신호등 · 알람) 에 걸린 표는 → 깨운 직원 명단을 정리하는 쪽 (`KiProcessThreadWaitList`) 이 뗀다.
  - 깨난 직원이 나가면서 떼는 게 아니다.

#### ⓑ "로그에 없다" → `dt` 구획에 없을 뿐, 날 바이트 구획에 있다

- run77 문서 "사진과 맞춰 본 것" (run77 해설 2.16 첫머리) 에 "`WaitRegister` 안쪽 비트와 번호표 안쪽 (`WaitType`·`BlockState`·`WaitKey`·`Object`) 은 **로그에 없다**" 고 적었다.
- 고친 것: 그건 `02b KTHREAD` (`dt`) 구획 얘기다. 같은 로그의 **`02c ETHREAD_RAW`** (`dq` 날 바이트) 구획에 있다.
  - `dt` 가 안쪽 구조체를 한 줄로 접어 버려서 놓쳤다. [run44 해설 2.9](run44-baggage-snap-해설.md#29-새-말--raw-dq-와-dt-의-차이) 의 바로 그 차이다.
- run 문서는 **값만** 옮겼다 `[관찰]`. 뜻 풀이는 안 했다 (→ Q146 ⑨):

| 칸 | K2 | K3 | P0 | P1 | P1a | P1b | P2 |
|---|---|---|---|---|---|---|---|
| `WaitRegister` 바이트 (`+0x70`) | 0 | 0 | 0 | 1 | 1 | 1 | 0 |
| `WaitBlock[0]` `+0x150` (`WaitType` · `BlockState` · `WaitKey`) | 0 · 0 · 0 | 0 · 0 · 0 | 0 · 0 · 0 | 1 · 5 · 0 | 1 · 5 · 0 | 1 · 5 · 0 | 1 · 4 · 0 |

- 로그 줄: `WaitRegister` K2 928 · K3 ~ P1 933 · P1a ~ P2 931줄, `WaitBlock[0]` K2 942 · K3 ~ P1 947 · P1a ~ P2 945줄 (`ms/logs/04_baggage-read_<P>.txt`).
- `+0x154` 뒤 4바이트는 `ContextSwitches` 다 (run75 값과 같음).
- 도구 교훈 (run 문서 "도구 메모"): `dt` 로 안 펴지는 안쪽 칸은 **날 바이트 구획에서 오프셋으로** 읽으면 된다. 이번 바퀴의 사진 대조 (2.17) 도 전부 이 방법이다.

### 2.17 사진과 맞춰 보기 — 메인 스레드 알람 시계

flow-writer 가 run44 덤프 읽기 로그의 **날 바이트 구획 `02c ETHREAD_RAW`** (`dq ffff828598445080 L0xf3`) 를 다시 읽었다. 새 실행은 없다.

- 같은 직원 (메인 스레드 `ffff828598445080`), 일곱 장 (K2 ~ P2). K1 은 옛 객체라 뺐다.
- 일곱 장 모두 찍은 순간 이 직원은 **돌고 있었다** (`State 2`, run77).

| 칸 (직원 + 오프셋) | K2 · K3 · P0 · P1 · P1a | P1b · P2 |
|---|---|---|
| 알람 머리 `+0x100` | `0x8` → `Type 8`, 나머지 0 | **`0xd90008`** → `Type 8` · `Absolute 0` · **`Hand 0xd9`** · `Inserted`·`Expired` 0 · `SignalState 0` |
| `WaitListHead` `+0x108` · `+0x110` | 둘 다 직원 + `0x1d0` (= 3번 표) | 같음 |
| `DueTime` `+0x118` | 0 | **`0x3ab67754f`** |
| `TimerListEntry` `+0x120` · `+0x128` | 0 · 0 | 둘 다 **`fffff805'42d73fa8`** |
| `Dpc` `+0x130` | `0x50bc04891b743c38` | 같음 |
| `Processor` · `TimerType` · `Period` | 0 · 0 · 0 | 0 · **1** · 0 |
| 3번 표 `+0x1e0` (`WaitType`·`BlockState`·`WaitKey`) · `+0x1e8` `Thread` | `0x01020401` · 직원 자신 | 같음 |
| `TimerActive` · `RelativeTimerBias` · `ThreadTimerDelay` | 0 · 0 · 0 | 0 · 0 · 0 |
| KUSD `InterruptTime` (`+0x8`) | P1a `0x31c54e93b` | P1b `0x31c695971` · P2 `0x31c698fa5` |
| KUSD `InterruptTimeBias` | P1a 0 | 0 |
| `_EPROCESS.ThreadTimerSets` (`+0x700`) | 0 (K1 도 0) | 0 |

- 모두 `[관찰]`. 로그 줄 번호는 run 문서 표에 있다 (`ThreadTimerDelay` 는 P1a ~ P2 만, KUSD 는 P1a · P1b · P2 보충 로그).

#### P1b 의 손 `0xd9` 는 `DueTime` 의 손이다 `[관찰]` + `[정적]`

- `0x3ab67754f >> 18` 의 아래 8비트 = `0xd9` — 머리 바이트 2 와 같다.
- `KiCommitThreadWait +0x198 SHR RCX,0x12` · `+0x19c` · `+0x1ae` (상대) 또는 `KiComputeDueTime +0xe7` (절대) 가 쓴 그대로다.

#### 상대 마감이었다 `[관찰]` + `[정적: 줄]`

- `TimerControlFlags` 0 → `Absolute` 비트 0.
- 상대 길은 `KiCommitThreadWait +0x1aa AND …,0xfe` 로 **끄고**, 절대 길은 `KiComputeDueTime +0x35 OR R10B,1` 로 **켠다** (2.6 · 2.7).

#### 마감 길이 약 240초 — 단계별로 `[추측: 계산]`

1. `[관찰]` 보정값 둘 (`RelativeTimerBias` · `InterruptTimeBias`) 과 `ThreadTimerDelay` 가 0 이다.
2. `[정적: 식]` 그래서 `DueTime = 기다림 시작 때 InterruptTime + |Timeout|` 이다 (2.5).
3. `[관찰]` P1a 에는 아직 `DueTime 0` → 기다림은 **P1a 뒤** 에 시작했다. P1b 엔 값이 있으니 **P1b 앞** 이다.
4. 계산: `DueTime − InterruptTime` 이 P1a 기준 **240.04초**, P1b 기준 **239.90초**.
5. 시작 시각은 P1a 와 P1b 사이니까, `|Timeout|` (= `DueTime − 시작`) 은 이 두 값 **사이** 다 → **약 240초 (4분)**.

- 이건 **계산값** 이다. 실행으로 마감 값을 본 게 아니다 (→ Q104 동적 1: `nt!KeWaitForSingleObject+0x3d7` `rdx` · `+0x841` `r9`).
- 그 기다림이 **어느 함수 것인지는 모른다.**

#### 마감 전에 보관대에서 빠졌다 → 신호가 이긴 꼴 — 단계별로 `[추측: 읽기]`

1. `[관찰]` P1b · P2 의 `InterruptTime` 은 아직 `DueTime` 보다 약 240초 **앞** 인데, 알람은 `Inserted 0` · `Expired 0` 이다.
2. `[정적]` 만기 처리 (`KiTimerExpiration`) 는 `만기 ≤ 지금` 일 때만 꺼낸다 (2.11). 그러니 이걸로는 빠질 수 없다.
3. `[정적]` 남는 길은 **취소** 다: `KiSwapThread +0x372 KiCancelTimer` → `+0x12b ~ +0x143` `Inserted` 지움 (2.15).
4. 같은 쪽을 가리키는 것 넷:
   - `SignalState 0` — 만기였다면 `KiTimerWaitTest +0x1d7` 이 1 로 쓴다. (단 그 뒤 0 으로 되돌리는 줄은 이번에 못 봤다 → Q146 ⑦.)
   - `TimerListEntry` 양쪽이 같은 커널 주소 — 칸 줄에 혼자 있다가 떼어진 꼴. 떼기는 이웃만 고치고 자기 고리는 안 지운다.
   - **P1b 0번 표 `BlockState 5`** (`_P1b.txt:945`, 2.16 ⓑ 표). 결과서 읽기로 5 는 "이긴 표" 다 (`KiProcessThreadWaitList +0x74 ~ +0x7a` 가 `< 5` 를 "이긴 표가 아님" 으로 거른다). 알람이 이겼다면 0번 표는 **4 로 남는다** — `KiProcessThreadWaitList` 는 떼기만 하고 `BlockState` 를 안 쓴다.
   - run77 P1b `AdjustIncrement 1` — 알람 길이면 0 이다 (2.14).
5. 한계: 이 넷이 모두 **같은 기다림** 의 흔적인지는 모른다. `WaitBlock[0]` · `AdjustIncrement` 는 **마지막 기다림** 값이고, 알람 칸은 **마지막 마감 있는 기다림** 값이다.

- 비유: 직원 책상 알람에 "4분 뒤" 가 적혀 있는데, 4분이 되기 한참 전에 이미 보관대에서 빠져 있다. 울려서 빠진 게 아니라, 신호등으로 먼저 일어나서 **직원이 끈** 모양이다.
- → Q104 동적 9 (`nt!KiSwapThread+0x372` 반환 `al`).

#### `TimerType 1` `[관찰]`

- 2.7 의 갈래대로면 그때 `KiGlobalTimerResolutionRequests == 0` 이고 `PsTimerResolutionActive` 가 거짓이었다 `[추측: 읽기]`.
- 사진 `_EPROCESS` `RequestedTimerResolution 0` · `SmallestTimerResolution 0` (`_P1a.txt:199 · 200`) 과 어긋나지 않는다.
- "1 = 비 HR 표" 는 결과서 `[추측: 이름]` 이다.

#### 칸 주소로 계산대 장부 거꾸로 세기 `[추측: 계산]`

1. `TimerListEntry` 가 칸 줄 머리 (`칸 + 0x8`) 를 가리킨다고 **보면**,
2. 칸 번호 = `Hand 0xd9 + TimerType 1 × 0x100` = `0x1d9`,
3. `fffff805'42d73fa8 − 8 − (0x10 + 0x1d9) × 0x20 − 0x4100` = **`fffff805'42d6c180`**.
4. 이 값은 K1 사진의 옛 트랩 프레임 `Rcx` (`_K1.txt:1059`) 와 같다.

- 우연이 아니면 **CPU 0 의 계산대 장부** 이고, 결과서의 칸 식 (`KiInsertTimerTable +0x4e ~ +0x7f`) 과 맞는다.
- 장부 주소가 사진에 따로 없어서 **확인은 아니다.**

#### `Dpc` 칸 `[관찰]` + `[정적: 식]`

- 일곱 장 같은 값 `0x50bc04891b743c38`.
- `KeInitThread +0x177 ~ +0x193` 이 `KiWaitAlways` · 알람 주소 · `KiWaitNever` 로 **0 을** 섞은 값이고, `KiTimerWaitTest +0x2f ~ +0x4f` 가 같은 키를 거꾸로 써서 풀면 0 이다 (식을 이어 보면 `KiWaitAlways` 끼리 지워짐).
- 짐 지도 3.14 `Timer` 둘째 줄의 "`Dpc` 값의 뜻 `[추측]`" 이 닫혔다.

#### 3번 표는 일곱 장 같다 `[관찰]`

- 만들 때 값 (`KeInitThread +0x19e` · `+0x1d2 ~ +0x1dd`) 그대로다.
- P1b 처럼 알람을 한 번 쓴 뒤에도 같다. 취소 성공 길 (3번 표를 안 건드림) 이든 만기 뒤 되돌림 (`KiSwapThread +0x37f ~ +0x391`) 이든 **같은 모양** 이라 이 칸으로는 못 가른다.

#### `TimerActive` 일곱 장 0 `[관찰]`

- 깨난 뒤 `KiSwapThread +0x263 BTR` 로 끈다 (P1b). 나머지 장은 마감 기다림이 없던 구간이다.

#### 다른 구간엔 알람을 쓴 흔적이 없다 `[추측: 읽기]`

- K2 ~ P1a, P1b → P2 는 알람 칸이 그대로다 → 그 사이 **커밋까지 간** 마감 기다림은 없었다.
- 단 마감 0 같은 **첫째 겹** (`KeWaitForSingleObject +0x240`, 표 걸기 전) 에서 끝난 기다림은 알람을 안 건드려 흔적이 없다.

#### `ThreadTimerSets` 여덟 장 0 `[관찰]`

- 2.7 끝의 줄 (`KiCommitThreadWait +0x3af`) 은 상대 · `WaitMode ≠ 0` · `KernelApcDisable == 0` · `WaitIrql == 0` · `+0xc0 == 0` 일 때 +1 한다.
- 위 240초 기다림이 이걸 다 맞췄다면 1 이 됐어야 한다.
- 그래서 그 기다림은 **커널 모드 기다림이거나 APC 를 막은 채 한 기다림** 으로 보인다 `[추측: 읽기]`.
- P1b 의 `WaitMode 1` (run77) 은 그 **뒤의 다른 기다림** 값일 수 있다.
- → Q104 동적 11 (`nt!KiCommitThreadWait+0x1b1` `sil`).

#### 디버거 영향

- 사진은 커널 디버거가 멈춘 순간이다. 멈출 때마다 기다림·교대 숫자가 조금 달라질 수 있다 `[추측: 디버거 영향]` ([run75 해설 2.12](run75-thread-switch-해설.md#212-사진과-맞춰-보기--메인-스레드-업무-카드-일곱-장) 와 같은 주의).

### 2.18 PE_emulator 에게 — 최소 규칙 열넷 + 덧붙임 셋

결과서 끝 "흉내 낼 최소 규칙" 을 run 문서가 그대로 옮겼다. CPU 하나·한 번에 한 직원 에뮬레이터 기준이다. 락·CAS·`ThreadTimerDelay`·`Timer2`·표 두 개의 구분·직렬화는 생략 가능 `[추측: 에뮬레이터 범위]`. 짧게:

**마감 계산 (기다림 시작)**

1. **먼저 신호 검사** (run77 규칙). 신호면 가져가고 `0`. 마감은 그다음.
2. `Timeout` NULL → 마감 없음, 알람 안 맞춤. test.exe `WaitForSingleObject(t, INFINITE)` 는 NULL 로 올 것 `[추측: KernelBase 쪽 안 읽음]`.
3. 음수 = 상대: **마감 = 지금 (`InterruptTime`) + `|Timeout|`** 을 **한 번** 계산해 직원에 적는다 (`0x100` 으로 다시 돌 때 안 늘린다). `지금' > 마감` (엄격) 이면 안 자고 `0x102`.
4. 0 이상 = 절대: 값 0 이면 안 자고 `0x102`. `시스템 시각 > 값` 이어도 `0x102`. 아니면 **마감 = 지금 + (값 − 시스템 시각)** 으로 바꿔 같은 칸에.
5. 보관대에 넣을 때 `마감 ≤ 지금` (경계) 이면 자는 대신 **그 자리에서 만료 처리** 하고 바로 돌아온다.
6. `TimerActive` 표시. 알람 번호표는 직원마다 하나 미리 있고 `WaitKey = 0x102` (`WaitType 1`).

**만료 알아채기·처리**

7. **째깍마다**: 지금 ≥ 가장 이른 마감이면 "알람 확인" 표시 + 디스패치 인터럽트 요청. 이미 표시가 있으면 다시 안 함.
8. **디스패치 인터럽트의 DPC 단계** (몫 끝보다 **앞**): 표시를 지우고 `마감 ≤ 지금` 인 알람을 꺼내 `Expired`. (한 칸에 여럿일 때 한 번에 다 꺼내도 결과는 같다 `[추측]`.)
9. 알람마다 `SignalState = 1`, 그리고 **알람 번호표로 `Unwait(직원, WaitStatus = 0x102)`**:
   - `State == Waiting` 일 때만 성공: `State = DeferredReady`, `WaitStatus = 0x102`, 깨운 직원 목록에. 아니면 지나침.
   - 스레드 알람은 알림형이라 매단 표 모두 (사실상 1장). 동기형이면 첫 성공 하나 + `SignalState = 0`.
10. 깨운 직원 목록 처리: **진 쪽 물건 번호표 (`BlockState < 5` 이고 `== 4`) 를 물건 줄에서 뗀다** → `AdjustReason = 1`, `AdjustIncrement = 0` → 준비 줄 (뒤 `[추측]`, 또는 `NextThread`).
11. 자리 지킴이가 돌 때도 같은 DPC 처리 (`KiIdleLoop` 가 `KiRetireDpcList` 를 부름).

**겹칠 때**

12. 신호 쪽·알람 쪽 둘 다 같은 `Unwait`. **`State == Waiting` 일 때만 이긴다.** 진 쪽은 아무것도 안 바꾼다 (자동 신호등도 안 줄이고 다음 직원으로).
13. 신호로 깬 직원은 나가면서 `TimerActive` 면 **알람을 보관대에서 뗀다** (이미 만료 처리에 올라갔으면 못 뗀다 → 알람 번호표를 원래대로).
14. 같은 순간이면 **신호 우선 규칙은 없다.** 에뮬레이터에서 순서를 정하려면 7 ~ 10 의 처리 순서가 곧 규칙이다.

flow-writer 덧붙임 (넣을지는 PE_emulator 판단):

- ① **사진 구간의 마감 기다림은 한 번, 그것도 취소로 끝났다** `[추측: 읽기]` (2.17). 초기화 길만 보면 7 ~ 11 (만기 처리) 보다 **3 · 6 · 13 (걸기 · 취소)** 이 먼저 쓰인다. 마감 약 240초 `[추측: 계산]`. 이 기다림을 "신호가 먼저 온다" 로 흉내 내면 만기 처리 없이 지나간다.
- ② **`ThreadTimerSets` (`_EPROCESS +0x700`) 를 올리는 줄** `KiCommitThreadWait +0x3af` `[정적]` (2.7). 사진 0 → 그 기다림은 조건을 안 맞춘 기다림 `[추측: 읽기]`. 결과서엔 없다.
- ③ **`TimerType` 은 "해상도 요청 없음" 이면 1** `[정적]` (2.7). 에뮬레이터는 1 하나로 `[추측: 에뮬레이터 범위]`.

"지금 우리" 판정 (run 문서 표):

| 무엇 | 판정 |
|---|---|
| 1(a) 스레드가 마감을 가진다 | **맞음**. "직원 칸 하나 + 훑기" 로 줄여도 됨 |
| 1(b) 넘길 때마다 지금과 비교만 | **모자람** — 시작 때 한 번 계산, 절대는 바꿔서 같은 칸에 |
| 1(c) 이미 지났으면 안 자고 `0x102` | **맞음**. 순서만: 신호 검사 → 마감 검사 |
| 2 넘길 때마다 확인 | **틀림** — 째깍마다, 표시와 처리가 둘로 |
| 3(a) 처리 사슬 | 순서만 맞추면 됨 (째깍의 DPC 단계, 몫 끝보다 앞) |
| 3(b) 결과값 `0x102` | **맞음**. 진 쪽 물건 표 떼기 **추가** |
| 3(c) 준비 줄 | 앞/뒤는 신호 쪽과 같게 (뒤 `[추측]`), 가산점 0 |
| 4 겹칠 때 | "`State == Waiting` 일 때만 깨움" **필요**, 신호로 깨면 **알람 치우기 필요** |

### 2.19 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 ① ~ ⑨ (오케스트레이터·flow-writer `.asm`):

- ① 결과서 "`KeInitializeTimerTable +0x5d` → `KeInitializeDpc`" → 부르는 줄은 **`+0x61`** (ret `+0x66`). DPC 자리는 `+0x45` (2.10).
- ② 결과서 "`KiTimerWaitTest +0x2de KiInsertQueueInternal`" → `+0x2de` 는 `BlockState = 5`, 부르는 줄은 **`+0x2e9`** (ret `+0x2ee`). 동기형 쪽은 `+0x276`.
- ③ 결과서 "`KeInitThread +0x1d2 ~ +0x1dd` InsertTail" → **머리에 꿰는 꼴** (`+0x1a9` · `+0x1ac` · `+0x1dd`). 그때 줄이 비어 있어 결과는 같다 (2.13).
- ④ 결과서 "`KiTraceSetTimer` … `+0x209 JNZ +0x464`" → 부르는 줄은 `+0x46c` (ret `+0x471`) (2.7).
- ⑤ 꼬리점프 대상 (rva `0x6b6440`) 은 PDB 이름이 둘 → "DPC 스택에서 `KiRetireDpcList` 를 부르는 조각" (2.1 · 2.10).
- ⑥ 보탬: `ThreadTimerSets` 를 쓰는 줄 `KiCommitThreadWait +0x3af` 와 조건 (2.7, 덧붙임 ②). 결과서는 `+0x102 ~ +0x139` 를 허용 지연 갈래로만 적었다.
- ⑦ 보탬: `TimerType 1` 갈래를 사진과 맞춤 (2.7 · 2.17, 덧붙임 ③).
- ⑧ 보탬: 사진 대조 전체 (2.17). 결과서 동적 3 "알람의 실제 모양" 가운데 **만들 때·한 번 쓴 뒤 모양은 날 바이트로 이미 닫혔다**. 남은 건 만기 순간의 비트 변화.
- ⑨ 보탬: `Dpc` 섞은 값이 0 으로 풀린다 — 결과서 문장을 식으로 확인 (2.12 · 2.17).

- 정정: run77 둘 (2.16). run75 · run76 문서에는 "(→ run78)" 한 마디씩만 붙었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | … run77 까지 | + run78 (마감·타이머 14개 처음 뽑음, `KiSetTimerEx` 는 뽑기만, `KiCommitThreadWait`·`KeWaitForSingleObject`·`KiSwapThread`·`KeInitThread` 다시 읽기) |
| 기준 — 사진 메모 run77 줄 | `WaitRegister`·`WaitBlock` 안쪽은 로그에 없다 | + "(run78 정정) `02b` 에 없을 뿐, 같은 로그 날 바이트 `02c ETHREAD_RAW` 에 있다" |
| 기준 — 사진 메모 | — | 새 run78 줄: 날 바이트에서 알람·3번 표·`WaitRegister`·0번 표, `02b` 의 `TimerActive`·`RelativeTimerBias`·`ThreadTimerDelay`, `01 EPROCESS` `ThreadTimerSets`, KUSD 줄 번호 |
| 2.1 제목 | … run74 | + run78 |
| 2.1 `KeInitThread` 절 `KeInitializeTimer` 줄 아래 | 타이머 칸 · `WaitBlock[3]` 을 타이머에 매단다 (run52) | + `[정적]` 이 타이머가 **기다림 마감 타이머**, 3번 표 `WaitKey 0x102` (만기 때 `KiTimerWaitTest +0x207` → `WaitStatus`), `Dpc` = 0 섞은 값 `[정적: 식]`, 머리에 꿰는 꼴, 사진 일곱 장 `[관찰]` |
| 3장 "퀀텀" 트리 `KeAccumulateTicks` 줄 | 곁다리 — 퀀텀 칸 안 만짐 | + 타이머 만료 검사 `+0x2d KiCheckForTimerExpiration` → 아래 "마감·타이머" |
| 3장 "기다림·깨움" 트리 `KiInsertTimerTable` 줄 | (마감 있을 때, Q141c2) | (마감 있을 때 — 아래 "마감·타이머", run78) |
| 3장 **새 절** "기다림의 마감·타이머" | — | 알람 시계 비유 + 트리 세 덩이 (기다림 시작 · 시계 틱 · 디스패치 인터럽트) + `[정적]` 여섯 + run77 정정 + 사진 `[관찰]` + 동적 → Q104 |
| 4장 run75 정적 줄 끝 | 타이머 만료는 Q141c2 | Q141c2 → run78 |
| 4장 run76 정적 줄 끝 | 타이머 만료는 이미 Q141c2 | + (해결됨 → run78: 틱에선 `+0x3d0` 비트 8 만, 처리는 DPC 단계) |
| 4장 run77 동적 줄 | 1 ~ 7, 스레드 객체 쪽은 번호 없이 끝에 | 1 ~ 8 로 맞춤 (스레드 객체 쪽이 6), 5 에 날 바이트 `WaitRegister` 값 한 줄 |
| 4장 run77 정적 줄 | 번호 없음 · ② "블록 4개 넘을 때" · 타이머는 Q141c2 | **Q145** · ② "객체 수 > 3 (3번 표는 알람 몫)" · 타이머 (해결됨 → run78) |
| 4장 새 줄 셋 (run78) | — | 동적 1 ~ 12 · 정적 ① ~ ⑨ (번호 없음) · 다르게 ① ~ ⑨ + run77 정정 ⓐ ⓑ |
| 5장 | — | run78 두 줄 |

태그가 바뀐 곳:

- PE_emulator `[추측]` "넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (`0x102`)" → **결과값 맞음, 언제·누가 다름** `[정적]`.
- run76 해설 `[추측]` "초인종 때 먼저 도는 DPC 안에 타이머 만료" → **잡무 단계는 맞음, DPC 일감이 아니라 요청 비트 8 → `KiRetireDpcList +0xca4`** `[정적]` (2.10).
- run76 · flow.md `KeAccumulateTicks +0x28 KiCheckForTimerExpiration` (이름만) → 펼침 `[정적]`.
- run77 `KiCommitThreadWait +0x1f2 KiInsertTimerTable` · `+0x39e KiTimerWaitTest` (이름만) → 펼침 `[정적]`.
- run52 "`WaitBlock[3]` 을 타이머에 매단다" → **번호 `0x102` 가 만기 때 결과 칸으로** `[정적]`.
- 짐 지도 3.14 `Timer` `Dpc` 값의 뜻 `[추측]` → **0 (DPC 없음)** `[정적: 식]`.
- run77 "진 표는 깨난 쪽이 나가면서 뗀다" → **깨운 쪽 `KiProcessThreadWaitList`** `[정적]` (정정 ⓐ).
- run77 "`WaitRegister`·번호표 안쪽은 로그에 없다" → **날 바이트에 있다** `[관찰]` (정정 ⓑ).
- 사진 새 값 `[관찰]`: P1b 알람 (`DueTime 0x3ab67754f` · `Hand 0xd9` · `TimerType 1` · `Inserted 0`) → 상대 약 240초 `[추측: 계산]`, 취소된 꼴 `[추측: 읽기]`.

### 짐 지도 · run75 ~ run77 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음, 2장 숫자 그대로** (서른넷째판). 짐 지도 바퀴가 아니다 — 주제가 특정 단계의 짐이 아니라 마감 있는 기다림이라서.
  - 3.14 `Timer*` 첫 줄 ("바뀜 `P1a~P1b` (타이머 걸림)"): 쓰는 줄 (`KiCommitThreadWait +0x242` · `+0x194` · `+0x1bc` · `+0x1e4` · `+0x20f`, `KiCancelTimer +0x143`) `[정적]` + 사진 P1b 값 `[관찰]` (상대 ≈ 240초, 취소된 꼴 `[추측: 읽기]`).
  - 3.14 `Timer*` 둘째 줄: `Dpc` 값의 뜻 `[추측]` → `[정적: 식]` (풀면 0).
  - 3.14 `WaitBlock*` 줄: `+0x1e0` `0x1020401` 의 뜻 (`WaitKey 0x102` = 만기 때 `WaitStatus`), 사진 일곱 장 같음.
  - 3.14 `MiscFlags` 줄: `TimerActive` 를 켜고 끄는 줄 `[정적]` (`KiCommitThreadWait +0x23d` 켬 · `+0x511`, `KiSwapThread +0x263` 끔).
  - 3.14 "아무도 안 씀 21" 의 `RelativeTimerBias`: **읽는** 줄은 있다 (`KeWaitForSingleObject +0x319` · `+0x3ca`, `KiCommitThreadWait +0x187`). 쓰는 줄은 여전히 없다.
  - 3.11 "범위 밖 길에서만 씀 52" 의 `ThreadTimerSets`: 쓰는 줄 `KiCommitThreadWait +0x3af` `[정적]`, 사진 0.
  - 3.14 머리에 "Q78d4 에서 볼 것 — 타이머 줄" (쓰는 줄 후보 모음). 마감 있는 기다림마다 다시 쓰는 칸이라 지나가며 본 줄로만 모아 두고 아직 표에 안 넣었다. 칸 수는 그대로다.
- run77 문서: 맨 위 정정 안내 한 줄. "정적 — 다음 바퀴 후보" 머리에 **Q145**, ② 에 "(3번 표는 알람 몫 — run78)", 끝 줄 Q141c2 에 "(→ run78)".
- run76 · run75 문서: "다음 바퀴 후보" 끝 줄에 "(→ run78)".

### 큰 그림에서

- **지도 쪽**:
  - PE_emulator 요청 25 ㄷ (기다림·깨움·타이머) 이 둘 다 닫혔다 (run77 · 이번). 다음 ㄹ (**Q141d**, 끝내기) 은 test.exe `WaitForSingleObject(thread)` 를 실제로 깨우는 쪽 — 스레드 객체가 신호를 받는 길이다.
  - run76 시계 사슬에서 이름만 봤던 곁가지 (`KiCheckForTimerExpiration`) 가 펼쳐졌다. "몫 끝" 깃발과 **같은 꼴** (째깍 땐 표시, 초인종에서 일) 이고, 초인종 안에서는 **알람 → 몫 끝** 순서다.
  - run52 에서 이름만 봤던 업무 카드 칸 (`Timer` · `WaitBlock[3]` · `TimerActive`) 의 쓰임이 닫혔다.
- **PE_emulator 쪽**:
  - "넘길 때마다 확인" 을 **"째깍마다 표시 → 잡무 단계에서 처리"** 로 고칠 근거가 생겼다. 결과값 `0x102` 는 그대로 써도 된다.
  - 깨운 뒤 **진 표 떼기** 와 신호로 깼을 때 **알람 치우기** 가 빠져 있으면 더해야 한다.
  - 사진대로라면 초기화 길에선 **걸기 · 취소** 가 먼저 쓰인다 (덧붙임 ①).
- **사진 쪽**:
  - 메인 스레드가 P1a ~ P1b 에 마감 있는 기다림을 **한 번** 했다는 단서가 생겼다 `[추측: 읽기]`. 누구 것인지는 Q104 에서.
  - 날 바이트 구획을 쓰면 `dt` 가 접어 버린 칸도 읽힌다. run77 사진 대조도 다시 할 수 있다 (Q146 ⑨).
- **다음** (같은 요청 25): **Q141d** (끝내기) → Q141e (남의 문맥) → Q142. 그다음 Q104 VM 단계 앞에서 멈춘다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 초기화 동안 마감 있는 기다림이 실제로 몇 번, 누구 것인가 (사진의 약 240초 기다림의 주인과 진짜 마감 값) | 실행 때 일. 사진은 마지막 마감 기다림 값만 남김 | **Q104** 동적 1 (`nt!KiCommitThreadWait+0x23d` `r8d ≠ 0` 횟수·`k`, `KeWaitForSingleObject+0x3d7` `rdx` · `+0x841` `r8d`/`r9`) |
| 시간 전역 값 (`KeMaximumIncrement` · `KeMinimumIncrement` · `KeTimeIncrement` · `KeNonHrTimeIncrement` · `KePseudoHrTimeIncrement` · `KiGlobalTimerResolutionRequests` · `KiSerializeTimerExpiration` · `KiClockTimerPerCpuTickScheduling`) — 알람이 26.2ms 칸보다 얼마나 늦게 터지나 | 실행 때 값 | **Q104** 동적 2 |
| 만기 **순간** 알람의 비트 변화 (`Inserted`·`Expired`·`SignalState`) | 사진 구간엔 만기가 없었음 `[추측: 읽기]`. 만들 때·한 번 쓴 뒤 모양은 날 바이트로 닫힘 | **Q104** 동적 3 (`dt nt!_KTIMER @$thread+0x100` · `dt nt!_KWAIT_BLOCK @$thread+0x1d0` 만기 전후) |
| `KiTimerExpirationDpc` (`PRCB+0x83f0`) 를 누가 바구니에 넣나 | 뽑은 `.asm` 엔 넣는 줄 없음 `[공백]` | 정적 **Q146 ①** · **Q104** 동적 4 (`bp nt!KiTimerExpirationDpc` `k`) |
| 스위치 8 이 실제로 서나, 초인종을 메모로 거나 IPI 로 거나 | 실행 때 일 | **Q104** 동적 5 (`ba w2 @$prcb+0x38bc`, `KiCheckForTimerExpiration+0x3c8` · `+0x3e7` / `+0x4f1`) |
| `KiRetireDpcList +0xca4` 가 `KiDispatchInterrupt` 경유인가 `KiIdleLoop` 경유인가 | 실행 때 일 | **Q104** 동적 6 (`nt!KiRetireDpcList+0xca4` 에서 `k`) |
| 정말 test.exe 직원이 `0x102` 로 깨나 | 실행 때 일 | **Q104** 동적 7 (`nt!KiTryUnwaitThread+0x168` 에서 `r12 == 0x102` 일 때 `rbx`) |
| 알람 길 `Increment 0` 이 실제인가, `KiDeferredReadySingleThread +0x6d` 갈래가 0 일 때 하는 일, 깨난 직원 줄 앞/뒤 | 실행 때 값 / 계산 본체 안 읽음 `[공백]` / run75 한계 `[추측]` | **Q104** 동적 8 (`nt!KiProcessExpiredTimerList+0xfc` `r8d`) · 정적 **Q146 ④** (= Q145 ③) |
| 신호가 이긴 뒤 취소가 성공하나 (사진 P1b 는 성공 쪽으로 읽힘) | 실행 때 일 | **Q104** 동적 9 (`nt!KiSwapThread+0x372` 반환 `al`, 실패면 `+0x37f`) |
| 자기 만기 갈래 (2.8 둘째 겹) 가 실제로 타나 | 실행 때 일 (경계 마감) | **Q104** 동적 10 (`nt!KiCommitThreadWait+0x1f9` `al == 0`) |
| 사진 `ThreadTimerSets` 0 의 이유 | 그 기다림의 조건 값은 사진에 없음 | **Q104** 동적 11 (`nt!KiCommitThreadWait+0x1b1` `sil`, `WaitMode`·`KernelApcDisable`·`WaitIrql`·`+0xc0`) |
| test.exe `main` 의 기다림이 정말 마감 없음인가 | KernelBase 쪽 안 읽음 `[추측]` | **Q104** 동적 12 (`nt!KeWaitForSingleObject+0x837` `r14d == 0`) · 정적 **Q146 ⑧** |
| `KiCheckForTimerExpiration +0xd7 ~ +0xe3` 훑을지 정하는 비교의 뜻 | 안 풀었다 `[공백]` | 정적 **Q146 ②** |
| `TimerType` 0/1 두 표의 이름·차이 (표 1 이 얼마나 늦게 터지나) | 이름은 `[추측]`, 값은 동적 | 정적 **Q146 ③** · Q104 동적 2 |
| `KiCancelTimer` 의 `Expired` 갈래 본체, 한 칸에 지난 알람 여럿을 한 번에 꺼내나, `Timer2` | 본체 안 풀었다 `[공백]` | 정적 **Q146 ⑤** (작음) |
| `KeWaitForMultipleObjects` 의 마감 | 안 읽음 (같은 `KiCommitThreadWait` 를 부를 것 `[추측]`) | 정적 **Q146 ⑥** (Q145 ② 와 묶을 만함) |
| 알람 `SignalState` 를 0 으로 되돌리는 줄 (사진 P1b `SignalState 0` 읽기가 여기에 기댐) | 못 봤다 | 정적 **Q146 ⑦** |
| run77 사진 값 (`WaitRegister` · 0번 표) 의 뜻 | 값만 옮겼다 (2.16 ⓑ) | **Q146 ⑨** (덤프 로그 다시 읽기, VM 없음) |
| 계산대 장부 칸 (`+0x38bc` · `+0x4100` · `+0x8310` · `+0x83f0` 등) 의 진짜 이름 | 공개 PDB `_KPRCB` 가 `+0x6d0` 에서 잘림 | 대기열 없음 (`[추측: 이름]` 으로 둠) |

- 대기열 번호: Q146 은 `_workspace/plan.md` 에 붙은 번호다. run 문서 "다음 바퀴 후보" 와 flow.md 4장 run78 정적 줄에는 아직 "대기열 번호 없음 — 오케스트레이터가 붙임" 으로 남아 있다.
- 추천 멈춤점 (run 문서, 모두 `nt!`, WinDbg 이름은 `nt!KeWaitForSingleObject`):
  - 마감 계산 `KeWaitForSingleObject+0x3d7` (`rdx`) · `+0x240` · `+0x841` (`r8d`·`r9`)
  - 거는 쪽 `KiCommitThreadWait+0x23d` · `+0x194` (`rcx`) · `+0x1f2` (`r9d` 손) · `+0x1f7` (`al`) · `+0x39e` · `+0x4e3`, 표 넣기 `KiInsertTimerTable+0x2a8` (`r13`·`rax`)
  - 알아채기 `KiCheckForTimerExpiration+0x3c8` · `+0x3e7` / `+0x4f1`
  - 처리 `KiRetireDpcList+0x411` · `+0xca4` · `KiTimerExpiration+0x20e` · `KiProcessExpiredTimerList+0xba` (`rdx`) · `+0xfc` · `KiTimerWaitTest+0x1d7` · `+0x325` (`r8`) · `KiTryUnwaitThread+0x168`
  - 치우기 `KiSwapThread+0x263` · `+0x372` · `+0x37f`
  - 데이터 `ba w8 @$thread+0xc8` (`WaitStatus`) · `ba w1 @$thread+0x74` (`MiscFlags`, 비트 9 `TimerActive`)
  - 커널 멈춤점은 모든 프로세스에 걸린다. test.exe 스레드일 때만 서게 조건을 붙여야 한다.

---

## 5. 더 읽기

- run 문서: [`run78-thread-timer.md`](run78-thread-timer.md) — "PE_emulator 쪽에서 쓸 것" 표·최소 규칙·**flow-writer 덧붙임 ① ~ ③**, 호출 지점 표, 마감 거는 쪽 · 알아채는 쪽 · 처리하는 쪽 · 겹칠 때 절, 호출 관계 그림, **사진과 맞춰 본 것**, 결과서와 다르게 적은 곳, **정정** (run77 둘)
- 결과서: [`_workspace/sast/74_thread-timer_result.md`](../../_workspace/sast/74_thread-timer_result.md) — "한눈에" 그림, 질문별 답, 최소 규칙, 동적 확인 목록·추천 멈춤점 (줄 몇 개는 run 문서 "다르게" ① ~ ④ 로 고쳐 읽기)
- 요청서: [`_workspace/sast/74_thread-timer.md`](../../_workspace/sast/74_thread-timer.md)
- flow.md: [3장](flow.md) 새 절 "기다림의 마감·타이머 — `nt!KiCommitThreadWait` 마감 걸기 · …" · 같은 장 "퀀텀"·"기다림·깨움" 트리 run78 줄 · [2.1](flow.md) `KeInitThread` 절 run78 줄 · 4장 run78 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.14 머리 "Q78d4 에서 볼 것 — 타이머 줄", 3.14 `Timer*` · `WaitBlock*` · `MiscFlags` 줄, 3.11 `ThreadTimerSets`
- 앞 이야기: [run77 해설](run77-thread-wake-해설.md) (번호표·`WaitKey`·어깨 두드리기·커밋 — **2.9 마지막 줄과 2.16 첫머리는 이 글 2.16 으로 고쳐 읽기**) · [run76 해설](run76-thread-quantum-해설.md) (벽시계 째깍·초인종·잡무 바구니·몫 끝 깃발) · [run75 해설](run75-thread-switch-해설.md) (준비 줄·`WaitStatus` 돌려주기·사진 일곱 장) · [run52 해설](run52-kthread-create-해설.md) (업무 카드·번호표 4장·타이머 달기) · [run44 해설 2.9](run44-baggage-snap-해설.md#29-새-말--raw-dq-와-dt-의-차이) (`dt` 와 날 바이트) · [run13 해설](../02_sprint/run13-resume-thread-해설.md) (신호등·번호표·경주)
