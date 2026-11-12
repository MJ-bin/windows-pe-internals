# 2026-11-12 run78 — 기다림의 마감·타이머 (Q141c2, PE_emulator 요청 25 ㄷ-2): **마감은 기다림을 시작할 때 한 번 계산해 스레드 안 타이머 (`_KTHREAD.Timer` `+0x100`) 에 건다.** 상대 마감은 `KeWaitForSingleObject +0x3b6 ~ +0x3d7` → `KiCommitThreadWait +0x194` 에서 `DueTime = 그때 InterruptTime + |Timeout|`, 절대 마감은 `+0x4e3 KiComputeDueTime` 이 인터럽트 시간 기준으로 바꾼 뒤 `+0x1f2 KiInsertTimerTable`. **시계 틱마다** `KeAccumulateTicks +0x28 KiCheckForTimerExpiration` 이 만기를 보면 요청 비트 `8` 만 켜고 (`+0x3d0 KiSetDpcRequestFlag`), 디스패치 인터럽트의 DPC 단계 `KiRetireDpcList +0xca4 KiTimerExpiration` → `+0x20e KiProcessExpiredTimerList` → `+0xba KiTimerWaitTest` → `+0x325 KiTryUnwaitThread` 가 **`WaitStatus = 0x102` (= 타이머 블록 `WaitBlock[3]` 의 `WaitKey`)** 로 깨운다. 신호와 겹치면 `State == 5` 를 먼저 본 쪽이 이긴다

## PE_emulator 쪽에서 쓸 것

- **한 줄**: 지금 우리 nt 엔진의 "스레드를 넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (`0x102`)" 은 **결과값은 맞고, 언제·누가 가 다르다** `[정적]`.
  - 마감은 기다림을 **시작할 때 한 번** 계산해 스레드에 둔다. 상대 = 그때 `InterruptTime` + `|Timeout|`, 절대 = 시스템 시각 기준을 인터럽트 시간 기준으로 바꿈. `0x100` 으로 처음부터 다시 돌 때도 새로 안 늘린다.
  - 확인은 넘길 때가 아니라 **시계 틱마다**. 틱에서는 표시만 하고, 처리는 디스패치 인터럽트의 DPC 단계에서 (퀀텀 끝보다 **앞**). 한 스레드만 돌아도 틱이 마감을 지나면 깬다.
  - `0x102` 는 코드에 박힌 상수가 아니라 스레드마다 미리 달린 **타이머 블록 (`WaitBlock[3]`) 의 `WaitKey`** 다. 신호 쪽 (`WaitKey 0`, run77) 과 같은 방식으로 `WaitStatus` 에 들어간다.
  - 타이머가 깨우면 객체 쪽 **진 블록을 객체 줄에서 뗀다** (`KiProcessThreadWaitList +0x141 ~ +0x269`). 신호가 깨우면 깨난 스레드가 **타이머를 치운다** (`KiSwapThread +0x372 KiCancelTimer`).
  - 둘이 겹치면 "신호 우선" 규칙은 없다 — `State == Waiting` 을 먼저 본 쪽이 이긴다. 들어갈 때만 신호 검사가 마감 검사보다 먼저다.
- run44 사진의 test.exe 메인 스레드 타이머 (`[관찰]`, 새 실행 없음 — 날 바이트 `02c ETHREAD_RAW`, 아래 "사진과 맞춰 본 것"):
  - K2 ~ P1a: 만들 때 모양 그대로 (`Type 8`, `DueTime 0`, `Hand 0`) → 그 사이 커밋까지 간 마감 있는 기다림은 **없었다** `[추측: 읽기]`.
  - P1b · P2: `DueTime 0x3ab67754f` · `Hand 0xd9` (= `DueTime >> 18` 의 아래 8비트, 계산 맞음) · `TimerType 1` · `Absolute 0` · `Inserted 0` · `SignalState 0`. → P1a ~ P1b 에 **상대 마감 약 240초** 기다림이 한 번 걸렸고 `[추측: 계산]`, 마감 **전에** 표에서 빠졌다 (P1b `InterruptTime` 이 아직 `DueTime` 보다 약 240초 앞) — 신호로 깨어 타이머를 취소한 꼴 `[추측: 읽기]`.
  - `TimerActive` 일곱 장 모두 0, `WaitBlock[3]` 일곱 장 모두 `0x01020401` (`WaitKey 0x102`) 이고 타이머 줄에 달린 채.
  - 즉 사진 구간 안에서는 **마감이 실제로 터진 흔적이 없다** `[추측: 읽기]`. 초기화 길을 흉내 내는 데 만기 처리까지는 안 걸려도, 걸었다가 **취소** 하는 길은 한 번 탄다.
- 질문마다 "지금 우리" (결과서 sast/74 그대로, 근거 줄은 오케스트레이터·flow-writer 가 `.asm` 으로 다시 봄):

| 질문 | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1(a) 마감 거는 줄 | 마감이 있을 때만 (`R8D ≠ 0`) **`KiCommitThreadWait` 맨 앞** `+0x23d BTS TimerActive` · `+0x242` 타이머 헤더 락 + `Inserted` → `+0x194` `Timer.DueTime` (상대) / `+0x4e3 KiComputeDueTime` (절대) → 손 (`+0x198`) · `TimerType` (`+0x1c4 ~ +0x1e4`) → **`+0x1f2 KiInsertTimerTable(Prcb, Thread+0x100, 0, 손)`** → `+0x20f` 락 풀기 → `+0x220 KiSwapThread`. 타이머는 스레드 안 `_KTHREAD.Timer` (`+0x100`) 하나, 일반 표 (PRCB `+0x4100`). `Timer2` 안 씀 | "스레드가 마감을 가진다" 면 **맞음**. 진짜는 타이머를 표에 넣지만, "스레드 칸 하나 + 훑기" 로 줄여도 됨 | `KiCommitThreadWait +0x38/+0x3b` · `+0x9a` · `+0x23d` · `+0x242` · `+0x24c` · `+0x167/+0x16a` · `+0x170/+0x174` · `+0x194` · `+0x198` · `+0x1aa` · `+0x1ae` · `+0x1bc` · `+0x1c4 ~ +0x1e4` · `+0x1eb ~ +0x1f2` · `+0x20f` · `+0x4e3` |
| 1(b) 상대 → 절대 | `KeWaitForSingleObject +0x3b6 ~ +0x3d7`: `[RBP-0x51] = InterruptTime − InterruptTimeBias − RelativeTimerBias + \|Timeout\|` (반복 지점 `+0x104` **위** 에서 한 번, `+0x3de JMP +0x104`) → `KiCommitThreadWait +0x17a ~ +0x194`: `DueTime = RelativeTimerBias + InterruptTimeBias + 그 값` → 바이어스가 지워져 **`DueTime = 기다림 시작 때 InterruptTime + \|Timeout\|`**. 절대 (0 이상) 는 `KiComputeDueTime`: `SystemTime ≥ 값` 이면 0 (이미 만기), 아니면 **`DueTime = InterruptTime + (값 − SystemTime)`** (`+0xd8`), `Absolute` 비트 켬 (`+0x35`) | "넘길 때마다 지금과 비교" 만 있으면 **모자람** — 시작 때 한 번 계산, 절대는 바꿔서 같은 칸에 | `KeWaitForSingleObject +0xdf/+0xe2` · `+0xe4/+0xe8` · `+0xee ~ +0xfb` · `+0x100` · `+0x3b6 ~ +0x3d7` · `+0x3de`, `KiCommitThreadWait +0x17a ~ +0x194` · `+0x102` · `+0x139`, `KiComputeDueTime +0x31/+0x33` · `+0x35` · `+0x39 ~ +0x60` · `+0xbf ~ +0xd8` · `+0xe7` |
| 1(c) 이미 지났으면 | **두 겹.** 첫째 (블록 달기 전, **신호 검사 뒤**): `KeWaitForSingleObject +0x240 R14D = 0x102` — 절대 값 0 (`+0x237/+0x23a`), 절대 `SystemTime > 값` (`+0x60c ~ +0x619` → `+0x356/+0x359`), 상대 `지금' > 마감` (`+0x2ff ~ +0x359`, 엄격). 둘째 (블록 달고 커밋한 뒤): `KiInsertTimerTable +0x2a8/+0x2ab` `DueTime ≤ InterruptTime` 이면 (칸 맨 앞일 때만) 도로 빼고 `AL = 0`, 또는 `KiComputeDueTime` 0 → **`KiCommitThreadWait +0x39e KiTimerWaitTest`** 로 자기 만기를 그 자리에서 → `KiSwapThread` 갔다 바로 `0x102` | "안 자고 `0x102`" **맞음**. 순서만: 신호 검사 → 마감 검사 (마감 0 이어도 신호면 0) | `KeWaitForSingleObject +0x211/+0x213` · `+0x224/+0x228` · `+0x22e/+0x231` · `+0x237/+0x23a` · `+0x240` · `+0x2ff ~ +0x326` · `+0x352 ~ +0x359` · `+0x60c ~ +0x619`, `KiCommitThreadWait +0x1f7/+0x1f9` · `+0x4e8/+0x4ea` · `+0x395 ~ +0x3a3`, `KiInsertTimerTable +0x1fe/+0x201` · `+0x29b ~ +0x2ab` · `+0x3b3` |
| 2 알아채기 | **시계 틱마다**: `KeAccumulateTicks +0x28 KiCheckForTimerExpiration` (run76 사슬 곁가지, 6번째 인자 4 면 건너뜀). 칸의 가장 이른 만기 ≤ `InterruptTime` 이면 (`+0x33c/+0x33f`) **`+0x3d0 KiSetDpcRequestFlag(PRCB+0x38bc, 8)`** — 비트만 켜고 `+0x3e7 InterruptRequest = 1` (인터럽트 밖이면 `+0x4f1 HalpInterruptSendIpi` 벡터 `0x2f`). 이미 켜져 있으면 다시 안 훑음 (`+0x5c/+0x66`). `KiTimerExpirationDpc` 는 이 줄기에 **없음** | "넘길 때마다 확인" **틀림** — 틱마다, 표시와 처리가 둘로 (퀀텀과 같은 꼴) | `KeAccumulateTicks +0x23/+0x26` · `+0x28`, `KiCheckForTimerExpiration +0x5c/+0x66` · `+0x2f8` · `+0x32f` · `+0x33c/+0x33f` · `+0x15d/+0x160` · `+0x3c8 ~ +0x3d7` · `+0x3dd/+0x3e1` · `+0x3e7` · `+0x4e1` · `+0x4f1` |
| 3(a) 처리 사슬 | `KiDispatchInterrupt +0x60 KiSwapStacksAndRetireDpcList` → (`+0xc` 꼬리점프, DPC 스택 조각) → `KiRetireDpcList`: `+0x406 XCHG` 요청 비트 가져오며 0 · `+0x40d/+0x411` 비트 8 · `+0xbd4` 지움 · `+0xbd7 KiSelectActiveTimerTable` · `+0xc7c STI` · **`+0xca4 KiTimerExpiration`** → **`+0x20e KiProcessExpiredTimerList`** (표 1 `+0x2ad`, 64칸 찼을 때 `+0x949` · `+0x97c`) → **`+0xba KiTimerWaitTest`**. `KiIdleLoop +0x99` 도 `KiRetireDpcList` 를 부름 | 순서만 맞추면 됨 (틱의 DPC 단계, 퀀텀 끝보다 앞) | `KiSwapStacksAndRetireDpcList +0xc`, `KiRetireDpcList +0x406` · `+0x40d/+0x411` · `+0xbd4` · `+0xbd7` · `+0xc7c` · `+0xca4`, `KiTimerExpiration +0x20e` · `+0x2ad` · `+0x36c ~ +0x37e` · `+0x949` · `+0x95b` · `+0x97c`, `KiProcessExpiredTimerList +0x8d` · `+0x96/+0x9e` · `+0xba` |
| 3(b) `WaitStatus` 와 블록 | `KiTimerWaitTest +0x1d7 SignalState = 1` → 알림형 (스레드 타이머, Type 8) 이면 블록마다 `+0x207 R8D = WaitKey` → **`+0x325 KiTryUnwaitThread`** → `+0x168 WaitStatus = 0x102`. **`0x102` = `WaitBlock[3].WaitKey`** (`KeInitThread +0x19e` `0x01020401`). 타이머 블록은 `+0x341/+0x345` 에서 머리째 비움. 객체 쪽 진 블록은 `KiTryUnwaitThread` 가 안 떼고 **`KiProcessThreadWaitList +0x141 ~ +0x269`** 가 뗀다 (`BlockState == 4` 만) | 결과값 **맞음**. 진 쪽 객체 블록 떼기 **추가** (안 떼면 나중에 그 객체를 신호할 때 죽은 블록을 만남) | `KiTimerWaitTest +0x1c ~ +0x4f` · `+0x66 ~ +0x6c` · `+0x1d7` · `+0x1e2/+0x1e6` · `+0x207` · `+0x325` · `+0x33d ~ +0x345` · `+0x398 ~ +0x3a5`, `KeInitThread +0x197` · `+0x19e` · `+0x1d2 ~ +0x1dd`, `KiTryUnwaitThread +0x168`, `KiProcessThreadWaitList +0x5a ~ +0x7a` · `+0x141` · `+0x148` · `+0x155/+0x159` · `+0x259/+0x25c` · `+0x260` |
| 3(c) 준비 줄 | `KiProcessExpiredTimerList +0xfc KiProcessThreadWaitList(Prcb, 1, 0, 2)` (`PRCB+0x2d08 ≠ 0` 일 때, `+0xe2/+0xea`) → `+0x98 AdjustReason = 1` · **`+0x9f AdjustIncrement = 0`** → `+0x101 KiDeferredReadySingleThread` (run75). 신호 쪽과 다른 건 `Increment = 0` 하나 | 앞/뒤는 신호 쪽과 같게 (뒤 `[추측]`), 올려 주기 0 | `KiProcessExpiredTimerList +0xe2/+0xea` · `+0xfc`, `KiProcessThreadWaitList +0x98` · `+0x9f` · `+0x101` |
| 4 겹칠 때 | 둘 다 `KiTryUnwaitThread` — 스레드 락 (`+0x33`) 을 잡고 **`State == 5` 를 먼저 본 쪽이 이김** (`+0x57 ~ +0x60`), 진 쪽은 아무것도 안 바꿈 (동기형 타이머·자동 이벤트도 `SignalState` 안 줄이고 다음 블록). "신호 우선" 규칙은 **없음** — 들어갈 때 검사 순서뿐. 신호로 깬 스레드는 `KiSwapThread +0x263 BTR TimerActive` → **`+0x372 KiCancelTimer`** (`Inserted` 이고 `Expired` 아니면 떼기 성공), 못 떼면 `+0x37f ~ +0x391` 타이머 블록 되돌림 | "`State == Waiting` 일 때만 깨움" **필요** (신호·타이머 둘 다). 신호로 깨면 **타이머 치우기 필요** | `KiTryUnwaitThread +0x33` · `+0x57 ~ +0x60` · `+0x149` · `+0x168`, `KiTimerWaitTest +0x256/+0x258` · `+0x25a/+0x25e`, `KiSwapThread +0x25c` · `+0x263` · `+0x369` · `+0x372/+0x377/+0x379` · `+0x37f ~ +0x391`, `KiCancelTimer +0x45` · `+0x50/+0x54` · `+0xb4/+0xba` · `+0x11a/+0x11d` · `+0x12b ~ +0x143`, `KiCommitThreadWait +0x27a/+0x27d` · `+0x511` |
| 5 흉내와 비교 | 결과값 같음, **언제·누가** 다름 (아래 "질문 5" 표) | 아래 최소 규칙 | — |

### PE_emulator 가 흉내 낼 최소 규칙 (결과서 sast/74 끝을 그대로 옮김)

(단일 CPU · 한 번에 한 스레드 에뮬레이터 기준. 락·CAS·`ThreadTimerDelay`·`Timer2`·표 두 개의 구분·직렬화는 생략 가능 `[추측: 에뮬레이터 범위]`. 우선순위·`Increment` 는 우선순위를 도입할 때.)

**마감 계산 (기다림 시작)**

1. **먼저 신호 검사** (run77 규칙): 신호면 그 자리에서 가져가고 `0` 으로 끝. **마감은 그다음에 본다.**
2. Timeout 이 NULL 이면 마감 없음 (타이머 안 건다). `test.exe` 의 `WaitForSingleObject(t, INFINITE)` (`sample/test.c:35` `[소스]`) 는 NULL 로 넘어올 것으로 보인다 `[추측: KernelBase 쪽 안 읽음]`.
3. 음수 = 상대: **마감 = 지금(InterruptTime) + |Timeout|** 을 **한 번** 계산해서 스레드에 적는다 (`0x100` 으로 다시 돌 때 새로 안 늘린다). 그리고 `지금' > 마감` 이면 (엄격) 안 자고 `0x102`.
4. 0 이상 = 절대 (시스템 시각): 값이 0 이면 안 자고 `0x102`. `시스템 시각 > 값` 이어도 안 자고 `0x102`. 아니면 **마감 = 지금(InterruptTime) + (값 − 시스템 시각)** 으로 바꿔 같은 칸에 둔다.
5. 스레드를 타이머 표에 넣을 때 `마감 ≤ 지금` 이면 (경계) 자는 대신 **그 자리에서 만료 처리** (아래) 한 뒤 바로 돌아온다.
6. 스레드에 `TimerActive` 표시. 타이머의 기다림 블록은 스레드마다 하나 미리 있고 `WaitKey=0x102` (`WaitType=1`).

**만료 알아채기·처리**

7. **시계 틱마다**: 지금 시각이 가장 이른 마감 이상이면 "타이머 만료 요청" 표시를 켜고 디스패치 인터럽트를 요청 (인터럽트 안이면 인터럽트 끝에서). 이미 표시가 켜져 있으면 다시 안 함.
8. **디스패치 인터럽트의 DPC 단계에서** (QuantumEnd 보다 **앞**): 표시를 지우고, 마감 ≤ 지금 인 타이머를 꺼내 **Expired** 로 표시한다. 칸 안은 마감 오름차순이라 앞에서부터 `≤ 지금` 까지 (한 칸에 여러 개일 때 한 번에 다 꺼내는지는 줄로 못 닫음 — 에뮬레이터는 모두 꺼내도 결과가 같다 `[추측]`).
9. 타이머마다 `SignalState=1`, 그리고 **타이머 기다림 블록으로 `Unwait(스레드, WaitStatus=0x102)`**:
   - `스레드.State == Waiting` 일 때만 성공: `State=DeferredReady`, `WaitStatus=0x102`, "깨운 스레드 목록" 에 넣는다. 아니면 (이미 신호로 깨웠음) 지나친다.
   - 스레드 타이머는 알림형이라 목록의 모든 블록을 본다 (사실상 1개). 동기형 타이머라면 첫 성공 하나만 깨우고 `SignalState=0`.
10. 깨운 스레드 목록을 처리: 스레드마다 **진 쪽 객체 블록 (`BlockState<5` 이고 `==4`) 을 객체 목록에서 뗀다** → `AdjustReason=1`, `AdjustIncrement=0` → 준비 줄에 넣는다 (`Preempted=0` 이라 뒤 `[추측]`, 또는 `NextThread`).
11. Idle 스레드가 돌 때도 같은 DPC 처리를 한다 (`KiIdleLoop` 가 `KiRetireDpcList` 를 부름).

**겹칠 때**

12. 신호 쪽·타이머 쪽 둘 다 같은 `Unwait(스레드, 상태)` 를 쓰고, **`State==Waiting` 일 때만 이긴다.** 진 쪽은 아무것도 안 바꾼다 (자동 재설정 이벤트의 `SignalState` 도 안 줄이고 다음 기다리는 스레드로 간다).
13. 신호로 깨난 스레드는 나가면서 `TimerActive` 이면 **타이머를 표에서 뗀다** (이미 만료 처리에 올라갔으면 못 뗀다 → 타이머 기다림 블록을 원래대로 되돌림).
14. 같은 순간이면 **신호 우선 규칙은 없다.** 먼저 `Unwait` 한 쪽이 이긴다. 에뮬레이터에서 순서를 정해 두려면 위 7~10 의 처리 순서가 곧 규칙이다 (틱의 DPC 단계에서 처리).

- flow-writer 덧붙임 (규칙에 넣을지는 PE_emulator 쪽 판단):
  - ① **사진 구간의 마감 기다림은 한 번, 그것도 취소로 끝났다** `[추측: 읽기]` (위 사진 줄, 아래 "사진과 맞춰 본 것"). 초기화 길만 놓고 보면 규칙 7 ~ 11 (만기 처리) 보다 **3 · 6 · 13 (마감 걸기 · 취소)** 이 먼저 쓰인다. 마감 길이는 약 240초 `[추측: 계산]` — 에뮬레이터가 이 기다림을 "신호가 먼저 온다" 로 흉내 내면 만기 처리 없이 지나간다.
  - ② **프로세스 칸 `ThreadTimerSets` (`_EPROCESS +0x700`) 를 올리는 줄이 이 길에 있다** `[정적]` (flow-writer `.asm`): `KiCommitThreadWait +0xf9` `SIL = 0` → 상대 마감 (`+0xfc`) · `WaitMode ≠ 0` (`+0x108`) · `KernelApcDisable == 0` (`+0x111`) · `WaitIrql == 0` (`+0x11a`) · `ApcState` 안 바이트 `+0xc0 == 0` (`+0x127`) 이면 `+0x130 ESI = 1` → `+0x1b1 TEST SIL / +0x1be JNZ +0x3a8` → **`+0x3af ADD.LOCK [Process+0x700],ESI`** (+1). 뽑은 ntoskrnl `.asm` 중 이 칸을 쓰는 줄은 이것 하나 (`grep '+ 0x700]'`). 사진 여덟 장 모두 0 (`[관찰]`) — 위 ① 의 기다림은 이 조건을 안 맞춘 기다림 (커널 모드 기다림 등) 으로 보인다 `[추측: 읽기]`. 결과서엔 없음.
  - ③ **`TimerType` 은 "해상도 요청 없음" 이면 1 이다** `[정적]`: `KiCommitThreadWait +0x1c4 CMP KiGlobalTimerResolutionRequests,0 / +0x1cb JZ +0x35c` → `+0x363 PsTimerResolutionActive(프로세스)` → `+0x368 / +0x36a` 거짓이면 `+0x370 MOVZX EDX,SI` (`SI` = 1, `+0x1b7`) → `+0x1e4 MOV word [Timer+0x3a],DX`. 둘 중 하나라도 있으면 0. 사진 P1b `TimerType 1` 과 맞는다 — 에뮬레이터는 1 하나로 두면 된다 `[추측: 에뮬레이터 범위]`.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서, 기준 GUID 와 같음). 다시 analyze 안 함
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, `KiSwapThread +0xdc/+0xe1 → KiSearchForNewThread` (run75), `KiCommitThreadWait +0x220/+0x225 → KiSwapThread` (run77) 일치
- **이름 주의** (run77 과 같음): PDB `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject` (rva `0x217eb0`). `.asm` 파일 이름은 `KeWaitForMutexObject`, WinDbg 는 `nt!KeWaitForSingleObject`. 이 문서는 PDB 이름으로 쓴다.
- `KiSwapStacksAndRetireDpcList +0xc` 가 꼬리점프로 가는 곳 (rva `0x6b6440`) 은 **DPC 스택에서 `KiRetireDpcList` 를 부르는 조각**이다. 같은 주소 이름 2개, 후보: `KySwapStacksAndRetireDpcList` · `KxSwapStacksAndRetireDpcList` (`pdb_aliases.py --lookup ntoskrnl.exe 0x6b6440`, `_index.tsv` 이름은 앞쪽). `.asm` 은 안 뽑음.
- 칸 이름 (`pdb_struct.py`, 결과서):
  - `_KTIMER` (`0x40`B): `Header +0x0` · `DueTime +0x18` · `TimerListEntry +0x20` · `Dpc +0x30` · `Processor +0x38` (16비트) · `TimerType +0x3a` (16비트) · `Period +0x3c`
  - 타이머 `Header` 바이트: `TimerType +0x0` (8 = 알림형 `TimerNotificationObject`, 9 = 동기형; 비트 7 = 헤더 락) · `TimerControlFlags +0x1` (`Absolute` 비트 0 · `Wake` 비트 1 · `EncodedTolerableDelay` 비트 2 ~ 7) · `Hand +0x2` · `TimerMiscFlags +0x3` (`Index` 비트 0 ~ 5 · `Inserted` 비트 6 · `Expired` 비트 7) · `SignalState +0x4` · `WaitListHead +0x8`
  - `_KTHREAD`: `MiscFlags +0x74` (`TimerActive` 비트 9) · `WaitStatus +0xc8` · `WaitBlockList +0xd0` · `RelativeTimerBias +0xf8` · **`Timer +0x100`** · `WaitBlock +0x140` (4개, 하나 `0x30`B — **`WaitBlock[3]` = `+0x1d0`**) · `State +0x184` · `WaitIrql +0x186` · `WaitMode +0x187` · `KernelApcDisable +0x1e4` · `Process +0x220` · `AdjustReason +0x236` · `AdjustIncrement +0x237` · `WaitBlockCount +0x24b` · `ThreadTimerDelay +0x3b8`
  - `_KWAIT_BLOCK` (`0x30`B, run77): `WaitListEntry +0x0` · `WaitType +0x10` · `BlockState +0x11` · `WaitKey +0x12` · `Thread +0x18` · `Object +0x20`
  - `_KUSER_SHARED_DATA` (`0xFFFFF78000000000`): `InterruptTime +0x8` · `SystemTime +0x14` · `InterruptTimeBias +0x3b0`. 코드에는 `-0x87ffffffff8` (= `+0x8`) · `-0x87fffffffec` (= `+0x14`) · `-0x87ffffffc50` (= `+0x3b0`) 로 찍힌다.
  - `_EPROCESS`: `ThreadTimerSets +0x700` (run44 사진 로그 `01 EPROCESS` 의 이름)
- **이름 없는 칸** (공개 PDB `_KPRCB` 가 `+0x6d0` 에서 잘림, 결과서 — 오프셋·동작 `[정적]`, 이름 `[추측]`):
  - `PRCB+0x38bc` (16비트) = DPC 요청 비트 모음. 비트 `0x8` = **타이머 만료 처리 요청**. 켜는 쪽 `KiCheckForTimerExpiration +0x3c8 ~ +0x3d0`, 읽고 지우는 쪽 `KiRetireDpcList +0x406 ~ +0x411` · `+0xbd4`. run76 의 `KiDispatchInterrupt +0x4d TEST byte [PRCB+0x38bc],0xbf` 에 이 비트가 들어 있다.
  - `PRCB+0x4100` = **타이머 표**. 앞 64칸 (`0x200`B) 은 만료 목록 머리, `+0x4300` 부터 칸 2 × 256개 (하나 `0x20`B: `+0x0` 락 · `+0x8` 줄 머리 · `+0x18` 칸의 가장 이른 만기). 칸 번호 = `Hand + TimerType × 0x100` (`KiInsertTimerTable +0x4e ~ +0x7f`). 빈 칸의 만기는 크게 (`KeInitializeTimerTable +0x9b` · `+0x9f`).
  - `PRCB+0x8310` · `+0x8314` = 표 종류별 "마지막으로 훑은 손". `PRCB+0x83f0` = CPU 마다 하나 있는 `KDPC` (`KiTimerExpirationDpc`).
  - **손 (hand)** = `만기 >> 18` 의 아래 8비트. 한 칸 = `2^18 × 100ns` ≈ 26.2ms, 256칸 한 바퀴 ≈ 6.7초 (`KiCommitThreadWait +0x198 SHR RCX,0x12` · `KiComputeDueTime +0xe7`).

## 목적

- PE_emulator 요청 25 ㄷ-2 (Q141c2): nt 엔진의 "넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (`0x102`)" 을 진짜 커널에 맞추기 위해 — 마감을 거는 쪽 (상대 → 절대, 이미 지난 마감), 만료를 알아채는 쪽 (시계 틱), 만료를 처리하는 사슬 (`KiTimerExpiration → KiProcessExpiredTimerList → KiTimerWaitTest`), `WaitStatus` 에 `0x102` 를 쓰는 줄, 신호와 마감이 겹칠 때 규칙.
- 기다림 블록 달기·깨움·`WaitStatus` 자체는 run77 (Q141c1) 을 가리키기만 한다.

## 방법

- 정적: `_workspace/sast/74_thread-timer.md` — ntoskrnl
  - 지도: `run_ghidra.sh map ntoskrnl.exe KiTimerExpiration` (311함수) · `… KiCheckForTimerExpiration` (29함수) → `ms/disassemble/ntoskrnl/_map/`. `KiTimerExpiration` 깊이 1 의 줄기는 `KiProcessExpiredTimerList` 하나, `KiCheckForTimerExpiration` 깊이 1 의 줄기는 `KiSetDpcRequestFlag` 하나 (나머지는 ETW·락 스핀·틱 없는 시계 다시 맞추기 — 이름만).
  - 지도 방향 (위 → 아래) 과 반대인 이음새 — "`KiTimerExpiration` 을 누가 부르나" 는 `refs` (`_refs/KiTimerExpiration.tsv`: `KiRetireDpcList +0xca4` · `KiTimerExpirationDpc +0x135` 둘), "`KiRetireDpcList` 를 누가 부르나" 도 `refs` (`_calls.tsv` 에 없음 — 부르는 쪽 `.asm` 이 없어서).
  - 새로 뽑음 **14개** (모두 depth 0):
    - 요청 대상 7: `KiTimerExpiration` (2816B) · `KiProcessExpiredTimerList` (1260B) · `KiTimerWaitTest` (1025B) · `KiCheckForTimerExpiration` (1548B) · `KiInsertTimerTable` (969B) · `KiComputeDueTime` (261B) · `KiTimerExpirationDpc` (395B)
    - 넓힌 6: `KiRetireDpcList` (4163B — `KiTimerExpiration` 부르는 줄 근처만) · `KiSwapStacksAndRetireDpcList` · `KiSetDpcRequestFlag` (238B) · `KeInitializeTimerTable` · `KiSelectActiveTimerTable` · `KiCancelTimer`
    - 실수로 같이 뽑힘 1: `KiSetTimerEx` (1575B) — **안 읽음**
  - 있던 것 다시 읽음: `KiCommitThreadWait` · `KeWaitForSingleObject` (Ghidra `KeWaitForMutexObject`) · `KiTryUnwaitThread` · `KiProcessThreadWaitList` · `KiSwapThread` · `KiDeferredReadySingleThread` · `KeInitThread` · `KeInitializeTimer` · `KeAccumulateTicks` · `KiDispatchInterrupt`
- 결과서: `_workspace/sast/74_thread-timer_result.md` (**분석성공**. 다섯 질문 모두 `.asm` 줄로 닫음. 정적으로 못 닫은 여섯은 아래 "아직 확인 필요")
- 오케스트레이터가 `.asm` 으로 다시 본 줄: `KiCommitThreadWait +0x194` · `+0x39e` · `+0x4e3`, `KeAccumulateTicks +0x28`, `KiCheckForTimerExpiration +0x3d0`, `KiRetireDpcList +0xca4`, `KiTimerExpiration +0x20e`, `KiProcessExpiredTimerList +0xba` · `+0xfc`, `KiTimerWaitTest +0x24e` · `+0x325`, `KeInitThread +0x19e`, `KiTryUnwaitThread +0x168`. 그리고 결과서 이름 정정 — 요청서·프롬프트의 "`KeWaitForMutexObject +0x1f2` · `+0x23d`" 는 **`KiCommitThreadWait`** 오프셋 (결과서 머리, run77 덧붙임 ① 과 같음)
- flow-writer 도 `.asm` 으로 다시 봤다: `KeInitThread +0x69 ~ +0x7e` · `+0x168 ~ +0x1dd`, `KiCommitThreadWait +0x0 ~ +0x41` · `+0x9a` · `+0xf9 ~ +0x24c` · `+0x27a/+0x27d` · `+0x2a0 ~ +0x332` · `+0x35c ~ +0x3a3` · `+0x3a8/+0x3af` · `+0x464 ~ +0x471` · `+0x4d6 ~ +0x4f5` · `+0x511`, `KeWaitForSingleObject +0xdf ~ +0x100` · `+0x211 ~ +0x240` · `+0x2ff ~ +0x326` · `+0x352 ~ +0x359` · `+0x3b6 ~ +0x3de` · `+0x60c ~ +0x619` · `+0x837` · `+0x841`, `KeAccumulateTicks +0x23 ~ +0x2d`, `KiCheckForTimerExpiration +0x5c/+0x66` · `+0x15d/+0x160` · `+0x2f8` · `+0x32f` · `+0x33c/+0x33f` · `+0x3c8 ~ +0x3e7` · `+0x4e1` · `+0x4f1`, `KiSwapStacksAndRetireDpcList +0xc`, `KiRetireDpcList +0x406 ~ +0x411` · `+0xbd4/+0xbd7` · `+0xc7c` · `+0xca4/+0xca9` · `+0xcc6`, `KiTimerExpiration +0x20e/+0x213` · `+0x2ad` · `+0x36c ~ +0x37e` · `+0x949` · `+0x95b` · `+0x97c`, `KiProcessExpiredTimerList +0x8d ~ +0x9e` · `+0xba/+0xbf` · `+0xe2/+0xea` · `+0xfc/+0x101` · `+0x143` · `+0x483`, `KiTimerWaitTest +0x1c ~ +0x56` · `+0x66 ~ +0x6c` · `+0x1d7 ~ +0x20c` · `+0x24e ~ +0x25e` · `+0x2c7 ~ +0x32a` · `+0x33d ~ +0x34b` · `+0x398 ~ +0x3a5`, `KiTryUnwaitThread +0x33` · `+0x57 ~ +0x60` · `+0x149` · `+0x161` · `+0x168`, `KiSwapThread +0x14/+0x2a` · `+0x25c` · `+0x263` · `+0x369 ~ +0x391` · `+0x508 ~ +0x51b`, `KiCancelTimer +0x45 ~ +0x54` · `+0xb4/+0xba` · `+0x11a/+0x11d` · `+0x12b ~ +0x143` · `+0x321` · `+0x32f` · `+0x350/+0x352`, `KiInsertTimerTable +0x4e ~ +0x7f` · `+0xc4 ~ +0xcb` · `+0xf7 ~ +0xfd` · `+0x1fe/+0x201` · `+0x222 ~ +0x227` · `+0x29b ~ +0x2ab` · `+0x39b ~ +0x3b3`, `KiComputeDueTime +0x31 ~ +0x60` · `+0xbf ~ +0xd8` · `+0xe7`, `KiProcessThreadWaitList +0x1e ~ +0x80` · `+0x98/+0x9f` · `+0x101` · `+0x141 ~ +0x159` · `+0x259 ~ +0x269`, `KeInitializeTimerTable +0x0 ~ +0x66` · `+0x9b/+0x9f`, `KiTimerExpirationDpc +0xdf/+0xe5` · `+0x135` · `+0x161`, `KeInitializeTimer +0x9 ~ +0x1a`. `_calls.tsv` 에서 호출 줄 전부 맞춤. `ThreadTimerSets` 를 쓰는 줄은 뽑은 `.asm` 전체를 `grep '+ 0x700]'`.
- 값 대조 (메인 스레드 `ffff828598445080`, 새 실행 없음): run44 덤프 읽기 로그 `ms/logs/04_baggage-read_<K2 ~ P2>.txt` 의 **날 바이트 구획 `02c ETHREAD_RAW`** (`dq <스레드> L0xf3`) 와 `02b KTHREAD` · `01 EPROCESS`, 보충 로그 `04_baggage-read_supp_<P1a · P1b · P2>.txt` 의 KUSD 날 바이트 — flow-writer

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!KeWaitForSingleObject` | `+0x841` | `+0x846` | `nt!KiCommitThreadWait` | run77. 마감 종류 `+0x837 R8D` (0 없음 · 1 절대 · 2 상대), 마감 값 `R9` = `[RBP-0x51]` | [정적] |
| `nt!KiCommitThreadWait` | `+0x4e3` | `+0x4e8` | `nt!KiComputeDueTime` | 절대 마감만 (`+0x170 CMP R12D,2 / +0x174 JNZ +0x4d6`). 인자 `(Timer, 값, 0, &손)`. `EAX` 0 = 이미 만기 → `+0x4ea JZ +0x395` | [정적] |
| `nt!KiCommitThreadWait` | `+0x363` | `+0x368` | `nt!PsTimerResolutionActive` | `KiGlobalTimerResolutionRequests == 0` 일 때 (`+0x1c4/+0x1cb`) — `TimerType` 0/1 고르기 (덧붙임 ③) | [정적] |
| `nt!KiCommitThreadWait` | **`+0x1f2`** | **`+0x1f7`** | **`nt!KiInsertTimerTable`** | 마감 있을 때 (상대·절대 합류). 인자 `(Prcb, Thread+0x100, 0, 손)` (`+0x1e8 ~ +0x1ef`). `AL` 0 이면 `+0x1f9 JZ +0x395` | [정적] |
| `nt!KiCommitThreadWait` | **`+0x39e`** | **`+0x3a3`** | **`nt!KiTimerWaitTest`** | 이미 만기 — 자기 만기를 그 자리에서 (`R8D = 0`) → `+0x3a3 JMP +0x216` → `KiSwapThread` | [정적] |
| `nt!KiCommitThreadWait` | `+0x46c` | `+0x471` | `nt!KiTraceSetTimer` | ETW 켜짐 (`+0x1ff/+0x209 JNZ +0x464`). 이름만 | [정적] |
| `nt!KiInsertTimerTable` | `+0x3b3` | `+0x3b8` | `nt!KiRemoveEntryTimer` | 넣고 보니 만기 (`+0x2a8 CMP R13,RAX / +0x2ab JBE`, 칸 맨 앞일 때만 `+0x1fe/+0x201`) — 도로 뺌 → `AL = 0` | [정적] |
| `nt!KeAccumulateTicks` | **`+0x28`** | **`+0x2d`** | **`nt!KiCheckForTimerExpiration`** | 틱마다 (run76 사슬 `KiUpdateRunTime +0x96` 의 곁가지). `+0x23 CMP EDX,4 / +0x26 JZ` — 6번째 인자 4 면 건너뜀 | [정적] |
| `nt!KiCheckForTimerExpiration` | **`+0x3d0`** | **`+0x3d5`** | **`nt!KiSetDpcRequestFlag`** | 만료 있음 (`+0x33c/+0x33f` → `+0x15d/+0x160`). `EDX = 8`, `RCX = &PRCB+0x38bc`. 옛 값에 이미 요청 비트가 있으면 (`+0x3d5 TEST AL,0xa9`) 인터럽트 요청 안 함 | [정적] |
| `nt!KiCheckForTimerExpiration` | `+0x4f1` | `+0x4f6` | `nt!HalpInterruptSendIpi` | `NestingLevel == 0` (`+0x3dd/+0x3e1`), 벡터 `0x2f` (`+0x4e1`). 인터럽트 안이면 대신 `+0x3e7 InterruptRequest = 1` | [정적] |
| `nt!KiCheckForTimerExpiration` | `+0x259` | `+0x25e` | `nt!KiSetClockTimer` | 만료 없을 때 다음 틱 시각 (틱 없는 시계). 이름만 | [정적] |
| `nt!KiDispatchInterrupt` | `+0x60` | `+0x65` | `nt!KiSwapStacksAndRetireDpcList` | run76 (DPC 먼저, 그다음 `+0x7f` 퀀텀 끝 표시) | [정적] |
| `nt!KiSwapStacksAndRetireDpcList` | `+0xc` (꼬리점프) | — | DPC 스택에서 `KiRetireDpcList` 를 부르는 조각 (같은 주소 이름 2개, 후보: `KySwapStacksAndRetireDpcList` · `KxSwapStacksAndRetireDpcList`) | 그 조각 `+0x0` 이 `KiRetireDpcList` 를 부름 (`_refs/KiRetireDpcList.tsv`, `.asm` 없음) | [정적] |
| `nt!KiIdleLoop` | `+0x99` | (안 뽑음) | `nt!KiRetireDpcList` | Idle 스레드가 돌 때 (`_refs/KiRetireDpcList.tsv`, `.asm` 없음) | [정적] |
| `nt!KiRetireDpcList` | `+0xbd7` | `+0xbdc` | `nt!KiSelectActiveTimerTable` | `+0x406 XCHG word [RBX+0x38bc],DI` · `+0x40d TEST DIL,8 / +0x411 JNZ +0xbca` · `+0xbd4` 비트 8 지운 뒤. 직렬화 안 하면 자기 표 `PRCB+0x4100` | [정적] |
| `nt!KiRetireDpcList` | **`+0xca4`** | **`+0xca9`** | **`nt!KiTimerExpiration`** | `+0xc7c STI` 뒤. `RCX` = Prcb · `EDX` = `[PRCB+0x8310]` · `R8` = `InterruptTime` | [정적] |
| `nt!KiRetireDpcList` | `+0xcc6` | `+0xccb` | `nt!KiTimer2Expiration` | `ClockOwner ≠ 0` (`+0xca9`). 고해상도 타이머 — 기다림 마감은 안 씀. 이름만 | [정적] |
| `nt!KiTimerExpirationDpc` | `+0x135` | `+0x13a` | `nt!KiTimerExpiration` | 이 DPC 를 큐에 넣는 곳은 못 찾음 `[공백]` (`.asm` 497개 `grep 0x83f0` → `KeInitializeTimerTable` 뿐) | [정적] |
| `nt!KeInitializeTimerTable` | `+0x61` | `+0x66` | `nt!KeInitializeDpc` | `+0x45 LEA RDI,[RBX+0x83f0]` · `+0x4f LEA RDX,[KiTimerExpirationDpc]` — CPU 마다 DPC 하나 등록 (부팅 때) | [정적] |
| `nt!KiTimerExpiration` | **`+0x20e`** · `+0x2ad` · `+0x949` · `+0x97c` | `+0x213` · `+0x2b2` · `+0x94e` · `+0x981` | **`nt!KiProcessExpiredTimerList`** | 표 0 · 표 1 · 만료 목록 64칸 찼을 때 (`+0x95b CMP R8D,0x40`) | [정적] |
| `nt!KiProcessExpiredTimerList` | **`+0xba`** · `+0x483` | `+0xbf` · `+0x488` | **`nt!KiTimerWaitTest`** | 뒤쪽은 ETW 켜진 변형. 앞: `+0x8d XCHG` 목록 머리 가져옴 · `+0x96 BTS.LOCK [타이머],7` 헤더 락 (`+0x9e JC` — 거는 쪽이 락을 쥐고 있으면 기다림) | [정적] |
| `nt!KiProcessExpiredTimerList` | **`+0xfc`** · `+0x143` | `+0x101` · `+0x148` | **`nt!KiProcessThreadWaitList`** | `PRCB+0x2d08 ≠ 0` (`+0xe2/+0xea`). 인자 `(Prcb, 1, 0, 2)` — **`Increment 0`** | [정적] |
| `nt!KiTimerWaitTest` | **`+0x325`** | **`+0x32a`** | **`nt!KiTryUnwaitThread`** | 알림형 (Type 8 — 스레드 타이머, `+0x1e2/+0x1e6`). `R8` = `WaitKey` (`+0x207`, `WaitType 1`) 또는 `0x100` (`+0x316`, 그 밖) · `R9 = 0` (`+0x31c`). 반환값 안 봄 (`+0x32a`) | [정적] |
| `nt!KiTimerWaitTest` | `+0x24e` · `+0x2b9` | `+0x253` · `+0x2be` | `nt!KiTryUnwaitThread` | 동기형 (Type 9). 앞쪽은 `WaitType 1` — 성공하면 `+0x25a SignalState − 1` → 0 이면 끝 (`+0x25e`), 실패면 다음 블록 (`+0x258`). 뒤쪽은 `0x100` | [정적] |
| `nt!KiTimerWaitTest` | `+0x1c7` | `+0x1cc` | `nt!KiInsertTimerTable` | `Period ≠ 0` (반복 타이머) 다시 걸기. 스레드 타이머는 `Period 0` 이라 안 탐 (`+0x66 ~ +0x6c JZ +0x1d7`) | [정적] |
| `nt!KiTimerWaitTest` | `+0x276` · `+0x2e9` / `+0x2a6` · `+0x30f` · `+0x3f9` | `+0x27b` · `+0x2ee` / `+0x2ab` · `+0x314` · `+0x3fe` | `nt!KiInsertQueueInternal` / `nt!KiInsertQueueDpc` | 큐 (`WaitType 2`) · DPC (`WaitType 4`) 블록. 이름만 | [정적] |
| `nt!KiSwapThread` | `+0x372` | `+0x377` | `nt!KiCancelTimer` | 깨난 뒤 `+0x263 BTR [R13+0x74],9` (`TimerActive`) 가 켜져 있었으면 (`JC +0x369`, `RCX = Thread+0x100`). `AL` 1 이면 `+0x379 JNZ` 로 그대로, 0 이면 `+0x37f ~ +0x391` 되돌림 | [정적] |
| `nt!KiSwapThread` | `+0x516` | `+0x51b` | `nt!KiProcessThreadWaitList` | `PRCB+0x2d08 ≠ 0` (`+0x14/+0x2a`) — 자기 만기 처리 (`KiCommitThreadWait +0x39e`) 뒤. 인자 `(1, 0, 2)` | [정적] |
| `nt!KeInitThread` | `+0x172` | `+0x177` | `nt!KeInitializeTimer` | `+0x168 RBX = Thread+0x100`. 스레드 만들 때 (run52) | [정적] |

- 줄기 밖 (이름만): ETW (`EtwTraceKernelEvent` · `EtwpLogKernelEvent` · `KiTraceSetTimer`) · 락 스핀 (`KiCheckVpBackingLongSpinWaitHypercall` · `HvlNotifyLongSpinWait`) · 틱 없는 시계 다시 맞추기 (`KiSetClockTimer` · `KiSetClockIntervalToMinimumRequested` · `KiResetClockIntervalOneShot` · `KiShouldRearmClockTimer` · `KiSetNextClockTickDueTime`) · 고해상도 타이머 (`KiTimer2Expiration` · `KiExpireTimer2` · `KiInsertTimer2`) · 전원 DPC (`PopPepIdleTimeoutDpcRoutine` · `PopFxResidentTimeoutDpcRoutine`) · `KiRemoveEntryTimer` · `KiSetTimerEx` (뽑았지만 안 읽음).

### 마감을 거는 쪽 — `KeWaitForSingleObject` → `KiCommitThreadWait` (질문 1)

- **마감 종류 정하기** (`KeWaitForSingleObject`, 반복 지점 `+0x104` 앞 — 한 번만):
  - `+0xdf TEST RCX,RCX / +0xe2 JZ +0xfd` → Timeout 포인터 NULL 이면 `+0x100` 종류 0 (마감 없음).
  - `+0xe4 CMP dword [RCX+4],0 / +0xe8 JL +0x3b6` → 윗 32비트 음수 = 상대. 아니면 절대: `+0xee` 값 → `+0xf4 [RBP-0x51]` · `+0xf8` 종류 1 → `+0xfb JMP +0x104`.
  - **상대 (`+0x3b6 ~ +0x3de`)**: `+0x3b6` `InterruptTimeBias` · `+0x3c0` `InterruptTime` · `+0x3c3 SUB` · `+0x3c6` 종류 2 · `+0x3ca` `RelativeTimerBias` · `+0x3d1 ADD` Timeout (음수) · `+0x3d4 SUB` → **`+0x3d7 MOV [RBP-0x51],RDX`** = `InterruptTime − InterruptTimeBias − RelativeTimerBias + |Timeout|` → `+0x3de JMP +0x104`. 그래서 `0x100` 으로 `+0x891 JMP +0x104` 다시 돌 때 (run77) 마감이 새로 안 늘어난다.
- **이미 지났나 — 첫째 겹** (블록 달기 전, 신호 검사 `+0x211/+0x213` **뒤**):
  - 종류 2: `+0x224 CMP R14D,2 / +0x228 JZ +0x2ff` → `+0x2ff ~ +0x326` `지금' = InterruptTime − RelativeTimerBias − InterruptTimeBias` · `+0x352 LEA RAX,[R8+RDX]` (마감 + 조건부 허용 지연) → **`+0x356 CMP RCX,RAX / +0x359 JA +0x240`** (엄격히 클 때만).
  - 종류 1: 값 0 이면 `+0x237 TEST R8,R8 / +0x23a JNZ` 안 타고 바로 `+0x240`. 아니면 `+0x60c ~ +0x616` `SystemTime` → `+0x619 JMP +0x356` (`SystemTime > 값`).
  - **`+0x240 MOV R14D,0x102`** → 헤더 락 풀고 `State = 2` → `+0x2c4` 반환 (run77). 같은 시각 (`==`) 은 이 겹을 통과해 둘째 겹으로.
- **`KiCommitThreadWait` 안** (`RBX` = 스레드, `R12D` = 종류, `R13` = 값, `RDI` = `Thread+0x100` `+0x9a`):
  - **맨 앞**: `+0x38 TEST R8D,R8D / +0x3b JNZ +0x23d` → **`+0x23d BTS dword [RCX+0x74],9`** (`TimerActive`) · **`+0x242 OR dword [RCX+0x100],0x40000080`** (타이머 헤더 락 비트 7 + `Inserted` 비트 30) → `+0x24c JMP +0x41`.
  - `+0xf9 ~ +0x139`: 종류 2 이고 유저 모드 기다림 등이면 `ThreadTimerDelay` (`+0x102`) 를 `+0x139 ADD R13,RAX` 로 더함 (허용 지연). 같은 조건에서 `+0x130 ESI = 1` (덧붙임 ②).
  - `+0x160 Preempted = 0` (run77) → `+0x167 TEST R12D / +0x16a JZ +0x216` (마감 없으면 타이머 일 전부 건너뜀).
  - **상대**: `+0x17a ~ +0x184` `InterruptTimeBias` · `+0x187` `RelativeTimerBias` · `+0x18e` · `+0x191 ADD RCX,R13` → **`+0x194 MOV [RDI+0x18],RCX`** (`Timer.DueTime`). 두 바이어스가 지워져 **`DueTime = 기다림 시작 때 InterruptTime + |Timeout|`** `[정적: 식]`. → `+0x198 SHR RCX,0x12` · `+0x19c` · `+0x19f` 손 → `+0x1a2 ~ +0x1bc` 헤더 고쳐 쓰기 (`+0x1aa AND …,0xfe` `Absolute` 지움 · `+0x1ae` `Hand` · `+0x1bc MOV [RDI],EAX`).
  - **절대**: `+0x174 JNZ +0x4d6` → **`+0x4e3 KiComputeDueTime(Timer, 값, 0, &손)`**: `+0x31/+0x33` 값이 음수면 상대 식 · 아니면 `+0x35 OR R10B,1` (`Absolute`) · `+0x39 ~ +0x4b` `SystemTime − 값` · `+0x57 JS` — **0 이상이면 이미 만기** (`+0x5c` 헤더 · `+0x60 DueTime` · `+0x5e EAX = 0`). 미래면 `+0xbf ~ +0xd1` → **`+0xd8 MOV [R11+0x18],R8`** (`DueTime = InterruptTime + (값 − SystemTime)`) · `+0xe7` 손 → `EAX = 1`. 돌아와 `+0x4e8/+0x4ea JZ +0x395` (만기) 아니면 `+0x4f0 ESI = 1` → `+0x4f5 JMP +0x1c4` 합류.
  - **`TimerType`**: `+0x1c4 ~ +0x1e4` (덧붙임 ③) → `+0x1e4 MOV word [Timer+0x3a],DX`.
  - **`+0x1f2 KiInsertTimerTable`** → `+0x1f7 TEST AL / +0x1f9 JZ +0x395` → (ETW `+0x1ff/+0x209`) → **`+0x20f AND.LOCK [RDI],0xffffff7f`** (헤더 락 풀기) → `+0x216` → **`+0x220 KiSwapThread`**.
- **이미 지났나 — 둘째 겹** (블록 달고 커밋한 뒤):
  - `KiInsertTimerTable`: 칸 번호 `+0x4e ~ +0x7f` (`(Hand + 0x10) + TimerType × 0x100`, 칸 `0x20`B — PRCB `+0x4100` 기준), 칸 안 자리 찾기 (`+0xc4 ~ +0xcb` 첫 항목과 비교, `+0xf7 ~ +0xfd` 가까우면 앞에서·멀면 뒤에서). 새 타이머가 **칸 맨 앞** 이 될 때만 (`+0x1fe TEST BL,2 / +0x201 JNZ`) `+0x29b ~ +0x2a5` `InterruptTime` → **`+0x2a8 CMP R13,RAX / +0x2ab JBE`** (`DueTime ≤ 지금`) → `+0x3b3 KiRemoveEntryTimer` · `+0x3aa OR EBX,1` → `+0x222 ~ +0x227` `AL = 0`. 칸에 더 이른 타이머가 있으면 검사 없이 `AL = 1` — 다음 틱이 칸을 볼 것 `[추측]`.
  - `AL = 0` (또는 `KiComputeDueTime` 0) → `+0x395 ~ +0x39b` → **`+0x39e KiTimerWaitTest(Prcb, Thread+0x100, 0)`** → 자기 `State` 5 → 7, `WaitStatus = 0x102`, `PRCB+0x2d08` 에 자기 → `+0x3a3 JMP +0x216` → `KiSwapThread` → `+0x14/+0x2a` → `+0x516 KiProcessThreadWaitList(…, 1, 0, 2)` → 고르기 (자기 자신이 고른 스레드면 문맥 전환 없음 — 런타임 `[추측]`) → `+0x25c` `WaitStatus` (`0x102`) 반환.
- **`Timer2` 는 안 쓴다**: `KiCommitThreadWait` 의 `_calls.tsv` 줄에 `KiInsertTimer2` 계열이 없고 표는 PRCB `+0x4100` 의 일반 표. `Timer2` 는 `KiCheckForTimerExpiration` · `KiRetireDpcList +0xcc6` · `KiTimerExpirationDpc +0x161` 이 곁다리로 볼 뿐 (결과서).

### 만료를 알아채는 쪽 — 시계 틱 (질문 2)

- 사슬 (run76): `HalpTimerClockInterrupt +0x103 KeClockInterruptNotify` → (시계 주인) `+0x2ba KiUpdateTime` → `+0x38b KiUpdateRunTime` / (그 밖) `+0x448 KiUpdateRunTime` → `+0x96 KeAccumulateTicks` → **`+0x28 KiCheckForTimerExpiration(Prcb, 6번째 인자)`** (`+0x23/+0x26` 4 면 건너뜀; `HalpTimerClockInterrupt` 는 0 을 넘김 — run76).
- `KiCheckForTimerExpiration` (`R14 = &PRCB+0x38bc`, `R12 = InterruptTime`):
  - **`+0x5c TEST byte [R14],8 / +0x66 JNZ`** — 이미 요청했으면 다시 안 훑음.
  - `+0x6c ~ +0x89` 직렬화 (`KiSerializeTimerExpiration`) 안 하면 자기 CPU 표. `+0xd7 ~ +0xe3` 훑을지 정하는 비교는 **안 풀었다** `[공백]`.
  - 훑기: `+0x2f8 SHR R9,0x12` (지금 손) · 마지막으로 훑은 손 (`PRCB+0x8310`) 부터 최대 256칸 · `+0x32f` 칸의 가장 이른 만기 → 표 0 **`+0x33c CMP R12,RCX / +0x33f JNC`** (지금 ≥ 만기 → 만료 있음). 표 1 은 `KeNonHrTimeIncrement` 등을 더해 더 늦게 본다 (`+0x381 ~ +0x3a8`, 값은 동적).
  - **요청**: `+0x15d TEST SIL / +0x160 JNZ +0x3c8` → `+0x3c8 EDX = 8` · `+0x3cd RCX = R14` · **`+0x3d0 KiSetDpcRequestFlag`** (16비트 `CMPXCHG.LOCK` 로 OR, 옛 값 반환) → `+0x3d5 TEST AL,0xa9 / +0x3d7 JNZ` (이미 요청 비트가 있었으면 그만) → `+0x3dd/+0x3e1` `NestingLevel`: ≠ 0 (시계 인터럽트 안, 보통) 이면 **`+0x3e7 InterruptRequest = 1`** (run76 퀀텀 끝 표시와 같은 칸), == 0 이면 `+0x4e1 / +0x4f1 HalpInterruptSendIpi` (벡터 `0x2f`).
  - 만료 없으면 `+0x259 KiSetClockTimer` 등 (이름만).
- `KiTimerExpirationDpc` (rva `0x5c7bd0`) 는 이 줄기에 **없다**: CPU 마다 `PRCB+0x83f0` 에 DPC 로 등록만 확인 (`KeInitializeTimerTable +0x45` · `+0x4f` · `+0x61`). 하는 일은 지금 손을 `PRCB+0x8310` · `+0x8314` 에 적고 (`+0xdf` · `+0xe5`) `+0x135 KiTimerExpiration`. **누가 큐에 넣는지 못 찾음** `[공백]`.

### 만료를 처리하는 쪽 — `KiRetireDpcList` → `KiTimerExpiration` → `KiProcessExpiredTimerList` → `KiTimerWaitTest` (질문 3)

- **DPC 단계로**: `KiDispatchInterrupt +0x60 KiSwapStacksAndRetireDpcList` → `+0xc` 꼬리점프 → (DPC 스택 조각) → `KiRetireDpcList`. Idle 스레드면 `KiIdleLoop +0x99`. `KiDispatchInterrupt` 는 이 처리 **뒤에** 퀀텀 끝 표시를 본다 (`+0x7f`, run76) → 같은 인터럽트 안에서 **타이머 → 퀀텀 끝 고르기** 순서. 타이머가 깨운 스레드는 이미 준비 줄이나 `NextThread` 에 올라간 채 고르기를 맞는다.
- **`KiRetireDpcList`**: **`+0x406 XCHG word [RBX+0x38bc],DI`** (요청 비트 전부 가져오며 0) · **`+0x40d TEST DIL,8 / +0x411 JNZ +0xbca`** · `+0xbd4 AND DI,AX` (비트 8 지움) · `+0xbd7 KiSelectActiveTimerTable` · `+0xc7c STI` (인터럽트 켜고) · **`+0xca4 KiTimerExpiration(Prcb, 시작 손, InterruptTime, …)`**.
- **`KiTimerExpiration`** (줄기만, 2816B 전부는 안 풀었다): 손을 하나씩 → 칸이 비었으면 지나감 → 칸 락 → 첫 타이머 만기 **`+0x36c MOV RAX,[RCX-8]` / `+0x379 CMP RAX,RSI / +0x37c JBE +0x3f7`** (만기 ≤ 지금) → 헤더 바이트 3 을 `Inserted | Expired | 만료 목록 번호` 로 · `PRCB+0x4100` 의 64칸 머리에 달고 · 칸 줄에서 뗌. 아니면 `+0x37e` 칸의 가장 이른 만기를 고쳐 놓고 끝. → **`+0x20e KiProcessExpiredTimerList`** (표 1 `+0x2ad`, 64칸 찼을 때 `+0x949` · `+0x97c`). 한 칸에 지난 타이머가 여럿일 때 한 번에 다 꺼내는지는 못 정했다 `[공백]`.
- **`KiProcessExpiredTimerList`**: `+0x8d XCHG` 목록 머리 가져옴 · `+0x96 BTS.LOCK [타이머],7` (헤더 락 — 거는 쪽 `KiCommitThreadWait +0x242` 가 쥐고 있으면 `+0x9e JC` 로 기다림) → **`+0xba KiTimerWaitTest`** → 끝에 `+0xe2 CMP [RDI+0x2d08],0 / +0xea JZ` → **`+0xfc KiProcessThreadWaitList(Prcb, 1, 0, 2)`**.
- **`KiTimerWaitTest(Prcb, Timer, 인자)`** (`RDI` = 타이머):
  - `+0x1c ~ +0x4f` `Timer.Dpc` 를 푼다 (`XOR KiWaitNever` · `ROL` · `XOR Timer` · `BSWAP` · `XOR KiWaitAlways`). 스레드 타이머는 `KeInitThread +0x177 ~ +0x193` 이 같은 키로 0 을 섞어 넣었으므로 **풀면 0** `[정적: 식]` → `+0x348 TEST RSI / +0x34b JZ` DPC 일 안 함.
  - `+0x66 MOVSXD RAX,[RDX+0x3c] / +0x6c JZ +0x1d7` — `Period 0` (스레드 타이머) 이면 다시 걸기 건너뜀.
  - **`+0x1d7 MOV [RDI+4],R11D`** (`SignalState = 1`) → `+0x1e2 CMP R12B,8 / +0x1e6 JNZ`:
    - **알림형 (8)**: 블록마다 `WaitType 1` 이면 **`+0x207 MOVZX R8D,word [RAX+0x12]`** (`WaitKey`) → `+0x20c JMP +0x31c` → **`+0x325 KiTryUnwaitThread(Prcb, 블록, WaitKey, 0)`** (반환값 안 봄). 끝에 **`+0x33d ~ +0x345`** `WaitListHead` 머리째 비움.
    - 동기형 (9): 블록을 하나씩 떼며 `+0x24e KiTryUnwaitThread` → 실패면 다음 (`+0x258`), 성공이면 `+0x25a SignalState − 1` → 0 이면 끝 — `KeSetEvent` 자동 재설정과 같은 꼴 (run77).
  - 끝 **`+0x398 ~ +0x3a5`**: 한 번짜리면 (`R13B = 0xff`, `+0x56`) 헤더 락 비트와 바이트 3 (`Index`·`Inserted`·`Expired`) 을 지운다 — 표에서 완전히 빠짐.
- **`0x102` 의 정체** `[정적]`: 스레드 만들 때 `KeInitThread +0x172 KeInitializeTimer(Thread+0x100)` (Type 8 · `DueTime`·`Period`·`Processor` 0) → `+0x177 ~ +0x193` `Dpc` 자리 → **`+0x197 LEA RAX,[RDI+0x1d0]`** (`WaitBlock[3]`) · **`+0x19e MOV dword [RAX+0x10],0x1020401`** (`WaitType 1` · `BlockState 4` · **`WaitKey 0x0102`**) → `+0x1a5 ~ +0x1dd` 이 블록을 `Timer.WaitListHead` 에 매단다. `WaitBlock[0..3].Thread` 는 `+0x69 ~ +0x7e` 가 미리 채움 (run52). 만기 때 `KiTimerWaitTest +0x207` 이 이 `WaitKey` 를 꺼내 `KiTryUnwaitThread +0x168 MOV [RBX+0xc8],R12` → **`WaitStatus = 0x102`** → `KeWaitForSingleObject` 가 그대로 반환 (run77). 타이머 블록은 `WaitBlockCount` 에 안 든다 (한 객체 기다림 = 1, `WaitBlockList` = `WaitBlock[0]`).
- **진 쪽 객체 블록 떼기**: `KiTryUnwaitThread` 는 블록 `BlockState` 만 +1 (run77). 타이머가 이기면 객체 줄에 남은 `WaitBlock[0]` 은 **`KiProcessThreadWaitList`** 가 뗀다: `+0x1e` 깨운 스레드 목록 → 스레드마다 `+0x5a WaitBlockCount` · `+0x62 WaitBlockList` → **`+0x74 MOVZX EAX,[RSI+0x11] / +0x78 CMP AL,5 / +0x7a JC +0x141`** → `+0x141` 객체 · `+0x148` 객체 헤더 락 · **`+0x155 CMP byte [RSI+0x11],4 / +0x159 JNZ`** · **`+0x259 / +0x25c` `RemoveEntryList`** · `+0x260` 락 풀기 · `+0x269 JMP +0x80` (다음 블록). `BlockState` 는 안 쓴다 (`+0x11` 을 쓰는 줄 없음, flow-writer `grep`).
- **준비 줄로** (`KiProcessThreadWaitList`, run75·run77 과 같은 줄): `+0x98 AdjustReason = DL (1)` · **`+0x9f AdjustIncrement = R8B (0)`** · `+0x101 KiDeferredReadySingleThread`. 신호 쪽은 `KeSetEvent` 의 `Increment` 가 가지만 타이머 길은 **0**. 이 값 0 일 때 `KiDeferredReadySingleThread +0x6d` 갈래가 뭘 하는지는 안 읽음 `[공백]`. 줄 앞/뒤는 `Preempted = 0` (`KiCommitThreadWait +0x160`) 이라 뒤로 보이지만 run75 한계 그대로 `[추측]`.

### 신호와 마감이 겹칠 때 (질문 4)

| 순서 | 줄 (`KiTryUnwaitThread`, run77) | 하는 일 |
|---|---|---|
| 1 | `+0x1e MOV RBX,[RDX+0x18]` | 스레드 = 블록 `Thread` (타이머 블록도 `KeInitThread +0x73` 이 채움) |
| 2 | **`+0x33 BTS.LOCK [RBX+0x40],0`** | 스레드 락 (스핀) |
| 3 | **`+0x57 MOVZX EAX,[RBX+0x184] / +0x5e CMP AL,5 / +0x60 JNZ`** | **`State ≠ 5` 면 지나침** — 이미 다른 쪽이 `State = 7` 로 바꿈 |
| 4 | `WaitRegister.State` 로 갈림 (run77 표) | 이긴 쪽이 `+0x149 State = 7` · `+0x161` `PRCB+0x2d08` push · `+0x168 WaitStatus` |
| 5 | `+0x194` 락 풀기 · `+0x19c ~ +0x1a7` `BlockState + 1` | 이기든 지든 늘 |

- **타이머가 졌을 때** (신호가 먼저): 알림형이라 `KiTimerWaitTest` 는 `AL` 을 안 보고 계속 (`+0x32a`). 깨난 스레드가 **`KiSwapThread +0x25c` (`WaitStatus`) → `+0x263 BTR [R13+0x74],9` → `JC +0x369` → `+0x372 KiCancelTimer(Thread+0x100, 1)`**:
  - `KiCancelTimer`: `+0x45 BTS.LOCK [타이머],7` · **`+0x50 TEST byte [RDI+3],0xc0 / +0x54 JZ`** (`Inserted`·`Expired` 둘 다 아님 = 표에 없음 → `AL = 0`) · `+0xb4 / +0xba JS` (`Expired` 면 다른 길) · 아니면 칸 줄에서 뗌 (`+0x11a / +0x11d`) · `+0x12b ~ +0x143 AND.LOCK …,0xbfffff7f` (`Inserted`·락 지움) → **`AL = 1`**.
  - `Expired` 갈래 (`+0x321 ~ +0x352`, 이미 만료 목록에 올라감): 그 목록을 가로채 보고 기다리는 스핀 — 본체 안 풀었다. `AL = 0` 으로 끝날 수 있다.
  - `AL = 0` 이면 `+0x379 JNZ` 를 안 타고 **`+0x37f ~ +0x391`**: `WaitBlock[3]` `BlockState = 4` · `Timer.WaitListHead` 를 다시 이 블록 하나로 (만료 쪽 `KiTimerWaitTest +0x33d ~ +0x345` 가 머리째 비웠으므로).
- **신호가 졌을 때** (타이머가 먼저): 깨우는 쪽이 객체 줄에서 블록을 떼고 (`KeSetEvent +0x313`, run77) `State ≠ 5` 를 보고 실패 → 자동 재설정 이벤트·동기형 타이머·뮤턴트는 **`SignalState` 를 안 줄이고 다음 스레드로** (`KeSetEvent +0x4ab/+0x4ae`, `KePulseEvent +0x179`, `KiTimerWaitTest +0x258`). 아무도 못 깨우면 신호 1 로 남음.
- **같은 순간** 이면 "신호 우선" 은 **없다** — 락을 먼저 잡은 쪽. 예외는 **들어갈 때** 뿐: `KeWaitForSingleObject` 신호 검사 (`+0x211`) 가 마감 검사 (`+0x224 ~`) 보다 앞이라 마감 0 이어도 신호가 이긴다.
- **커밋 전에 겹치면** (`WaitRegister.State == 0`): 먼저 온 쪽이 `State = 2` 로 바꾸고 `WaitStatus` 를 써 두면 `KiCommitThreadWait +0xa3 JNZ +0x251` 로 안 잔다 (run77). 타이머를 넣기 **전** 이면 `+0x27a TEST R12D / +0x27d JNZ +0x511` → **`+0x511 BTR [RBX+0x74],9`** (`TimerActive` 끔) 으로 타이머 일을 아예 안 한다.
- 타이머를 표에 넣는 동안은 헤더 락 (`+0x242` ~ `+0x20f`) 을 쥐고 있어 다른 CPU 의 만료 처리가 못 건드린다 (`KiProcessExpiredTimerList +0x96/+0x9e`).

### PE_emulator 흉내와 비교 (질문 5, 결과서 표 그대로)

| 항목 | 진짜 `[정적]` | 우리 | 같은가 |
|---|---|---|---|
| 결과값 | 타이머 블록 `WaitKey=0x102` → `WaitStatus=0x102` → `KeWaitForSingleObject` 가 하위 32비트 그대로 반환 | `0x102` | **같다** |
| 언제 확인 | **시계 틱마다 (CPU 마다)**: `KiCheckForTimerExpiration` 이 `InterruptTime ≥ 칸의 가장 이른 만기` 를 보고 비트만 켬. 처리는 디스패치 인터럽트 DPC 단계 | 스레드를 넘길 때마다 | **다르다** (틱이 안 오면 안 깨우고, 한 스레드만 돌아도 틱에서 깨운다) |
| 확인 단위 | 손 = `만기>>18` (26.2ms 칸). 비(非)HR 표는 `KeNonHrTimeIncrement` 만큼 더 늦게 터질 수 있음 (값 동적) | 정확한 시각 비교로 보임 | **다르다** (에뮬레이터는 정확한 비교로 두어도 결과 같음 — 값만 다름) |
| 처리 순서 | `KiDispatchInterrupt`: DPC(타이머 만료) **먼저** → QuantumEnd → 고르기 | "깨운 뒤 고름" | **같은 방향** (타이머로 깬 스레드가 고르기 후보에 이미 들어 있음) |
| 깨우는 조건 | `State==5` (스레드 락 안에서). 이미 신호로 깨웠으면 지나침 | (모름) | 필요 |
| 깨운 뒤 일 | 진 쪽 객체 블록 떼기 · `AdjustReason=1` · **`AdjustIncrement=0`** · `KiDeferredReadySingleThread` | "깸 표시" | **모자랄 수 있다** (객체 블록 떼기) |
| 준비 줄 위치 | `Preempted=0` 이라 뒤 `[추측]` | (모름) | 신호 쪽과 같게 |
| 우선순위 올림 | 타이머 쪽 `Increment=0` | 없음 | 우선순위 도입 전엔 건너뛰어도 됨 |
| 깨난 뒤 | `WaitStatus` 를 그대로 반환, 다시 안 묻음 | — | run77 규칙 |
| 신호로 깨날 때 타이머 | `TimerActive` 이면 `KiCancelTimer` | (모름) | 필요 |

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리.

```
[기다림 시작 — 마감 걸기]
KeWaitForSingleObject  (Ghidra KeWaitForMutexObject)
 │  +0xdf ~ +0xfb   Timeout NULL → 종류 0 · 0 이상 → 종류 1 (절대) · 음수 → +0x3b6 ~ +0x3d7 종류 2 (상대, [RBP-0x51] 한 번)
 │  +0x211 / +0x213  신호 검사 먼저 (run77)
 │  +0x224 ~ +0x359  이미 지났으면 → +0x240 R14D = 0x102 (안 잠)
 └─ +0x846  KiCommitThreadWait (종류, 값)
      │  +0x23d TimerActive · +0x242 타이머 헤더 락 + Inserted     (종류 ≠ 0, 맨 앞)
      │  +0x194 DueTime = 시작 InterruptTime + |Timeout| (상대) · +0x198 / +0x1ae 손
      ├─ (1) +0x4e8  KiComputeDueTime                절대만. DueTime = InterruptTime + (값 − SystemTime), 지났으면 0
      │  +0x1c4 ~ +0x1e4  TimerType (해상도 요청 없으면 1)
      ├─ (2) +0x368  PsTimerResolutionActive         KiGlobalTimerResolutionRequests == 0 일 때
      ├─ (3) +0x1f7  KiInsertTimerTable(Prcb, Thread+0x100, 0, 손)
      │    └─ +0x3b8  KiRemoveEntryTimer              칸 맨 앞인데 DueTime ≤ 지금 (+0x2a8 / +0x2ab) → AL = 0
      │  +0x20f  헤더 락 풀기
      ├─ (4) +0x3a3  KiTimerWaitTest(Prcb, Timer, 0)  AL = 0 일 때만 — 자기 만기 (WaitStatus = 0x102)
      └─ (5) +0x225  KiSwapThread                      잠 (또는 바로 돌아옴)
           ├─ (1) +0x51b  KiProcessThreadWaitList      PRCB+0x2d08 ≠ 0 (자기 만기 뒤)
           │  +0x25c  WaitStatus · +0x263 BTR TimerActive → 켜져 있었으면
           └─ (2) +0x377  KiCancelTimer(Thread+0x100, 1)   AL = 0 이면 +0x37f ~ +0x391 타이머 블록 되돌림

[시계 틱 — 알아채기]
KiUpdateRunTime +0x9b KeAccumulateTicks                                     (run76 사슬)
 └─ +0x2d  KiCheckForTimerExpiration(Prcb)              +0x23 / +0x26 6번째 인자 4 면 건너뜀
      │  +0x5c / +0x66  비트 8 이미 켜져 있으면 끝
      │  +0x33c / +0x33f  지금 ≥ 칸의 가장 이른 만기
      ├─ (1) +0x3d5  KiSetDpcRequestFlag(&PRCB+0x38bc, 8)
      │  +0x3e7  InterruptRequest = 1      (인터럽트 안)
      ├─ (2) +0x4f6  HalpInterruptSendIpi(…, 0x2f)          (인터럽트 밖)
      └─ (3) +0x25e  KiSetClockTimer                         (만료 없을 때, 이름만)

[디스패치 인터럽트 — 처리]
KiDispatchInterrupt
 └─ +0x65  KiSwapStacksAndRetireDpcList                    (run76 — DPC 먼저, 그다음 퀀텀 끝)
      ══▶ +0xc 꼬리점프 → DPC 스택 조각 (Ky/KxSwapStacksAndRetireDpcList) +0x0 → KiRetireDpcList
KiRetireDpcList                                            (KiIdleLoop +0x99 도 부름)
 │  +0x406 XCHG 요청 비트 · +0x40d / +0x411 비트 8 · +0xbd4 지움
 ├─ (1) +0xbdc  KiSelectActiveTimerTable
 │  +0xc7c STI
 ├─ (2) +0xca9  KiTimerExpiration(Prcb, 시작 손, InterruptTime)
 │    │  +0x36c ~ +0x37c  칸 첫 타이머 만기 ≤ 지금 → 떼어 Expired, 만료 목록에
 │    └─ +0x213 / +0x2b2 / +0x94e / +0x981  KiProcessExpiredTimerList
 │         │  +0x8d 목록 머리 · +0x96 타이머 헤더 락
 │         ├─ (1) +0xbf   KiTimerWaitTest(Prcb, Timer, …)
 │         │    │  +0x1c ~ +0x4f Dpc 풀기 (스레드 타이머 = 0) · +0x6c Period 0 → 다시 안 걸기
 │         │    │  +0x1d7 SignalState = 1 · +0x207 R8D = WaitKey (0x102)
 │         │    └─ +0x32a  KiTryUnwaitThread(Prcb, WaitBlock[3], 0x102, 0)
 │         │         │  +0x57 ~ +0x60 State ≠ 5 → 지나침 · +0x149 State = 7 · +0x161 push · +0x168 WaitStatus = 0x102
 │         │    │  +0x33d ~ +0x345 WaitListHead 머리째 비움 · +0x398 ~ +0x3a5 Inserted·Expired 지움
 │         └─ (2) +0x101  KiProcessThreadWaitList(Prcb, 1, 0, 2)      PRCB+0x2d08 ≠ 0
 │              │  +0x74 ~ +0x7a / +0x141 ~ +0x269  진 쪽 객체 블록 (BlockState 4) 떼기
 │              │  +0x98 AdjustReason 1 · +0x9f AdjustIncrement 0
 │              └─ +0x106  KiDeferredReadySingleThread                  (run75)
 └─ (3) +0xccb  KiTimer2Expiration                          ClockOwner ≠ 0, 이름만

[CPU 마다 DPC — 부팅 때 등록, 줄기 밖]
KeInitializeTimerTable  +0x45 RDI = PRCB+0x83f0 · +0x4f RDX = KiTimerExpirationDpc
 └─ +0x66  KeInitializeDpc
KiTimerExpirationDpc  +0xdf / +0xe5 PRCB+0x8310 / +0x8314 = 지금 손
 └─ +0x13a  KiTimerExpiration                               누가 이 DPC 를 넣나 [공백]
```

## 사진과 맞춰 본 것 (run44 메인 스레드 `ffff828598445080`, 새 실행 없음)

- `02b KTHREAD` (`dt`) 는 `Timer` 를 `_KTIMER` 한 줄로만 보여 주고 안쪽을 안 편다. 그래서 같은 로그의 **날 바이트 구획 `02c ETHREAD_RAW`** (`dq ffff828598445080 L0xf3`) 에서 `+0x100 ~ +0x13f` (`Timer`) 와 `+0x1d0 ~ +0x1ef` (`WaitBlock[3]`) 를 읽었다. K1 은 옛 객체라 뺐다.
- 일곱 장 (K2 ~ P2) 모두 사진을 찍은 순간 이 스레드는 돌고 있었다 (`State 2`, run77).

| 칸 (스레드 + 오프셋) | K2 · K3 · P0 · P1 · P1a | P1b · P2 | 로그 줄 (`ms/logs/04_baggage-read_<P>.txt`) |
|---|---|---|---|
| `Timer` 머리 `+0x100` (`Type` · `TimerControlFlags` · `Hand` · `TimerMiscFlags` · `SignalState`) | `0x8` → `Type 8`, 나머지 0 | **`0xd90008`** → `Type 8` · `Absolute 0` · **`Hand 0xd9`** · `Inserted`·`Expired` 0 · `SignalState 0` | K2 937 · K3~P1 942 · P1a~P2 940 |
| `Timer.WaitListHead` `+0x108` · `+0x110` | 둘 다 `ffff8285'98445250` (= 스레드 + `0x1d0` = `WaitBlock[3]`) | 같음 | K2 937 ~ 938 · K3~P1 942 ~ 943 · P1a~P2 940 ~ 941 |
| `Timer.DueTime` `+0x118` | 0 | **`0x3ab67754f`** | K2 938 · K3~P1 943 · P1a~P2 941 |
| `Timer.TimerListEntry` `+0x120` · `+0x128` | 0 · 0 | 둘 다 **`fffff805'42d73fa8`** | K2 939 · K3~P1 944 · P1a~P2 942 |
| `Timer.Dpc` `+0x130` | `0x50bc04891b743c38` | 같음 | K2 940 · K3~P1 945 · P1a~P2 943 |
| `Timer.Processor` `+0x138` · `TimerType` `+0x13a` · `Period` `+0x13c` | 0 · 0 · 0 | 0 · **1** · 0 | 같은 줄 |
| `WaitBlock[3]` 줄 연결 `+0x1d0` · `+0x1d8` | 둘 다 `ffff8285'98445188` (= `Timer.WaitListHead`) | 같음 | K2 950 · K3~P1 955 · P1a~P2 953 |
| `WaitBlock[3]` `+0x1e0` (`WaitType` · `BlockState` · `WaitKey`) · `+0x1e8` `Thread` | `0x01020401` (1 · 4 · `0x102`) · 스레드 자신 | 같음 | K2 951 · K3~P1 956 · P1a~P2 954 |
| `MiscFlags.TimerActive` (`+0x74` 비트 9) | 0 | 0 | K2 706 · K3~P1 711 · P1a~P2 709 |
| `RelativeTimerBias` `+0xf8` | 0 | 0 | K2 771 · K3~P1 776 · P1a~P2 774 |
| `ThreadTimerDelay` `+0x3b8` | P1a 0 | 0 | `_P1a`·`_P1b`·`_P2.txt` 880 |
| KUSD `InterruptTime` (`+0x8`) | P1a `0x31c54e93b` | P1b `0x31c695971` · P2 `0x31c698fa5` | `04_baggage-read_supp_<P1a·P1b·P2>.txt:287` |
| KUSD `InterruptTimeBias` (`+0x3b0`) | P1a 0 | 0 | 같은 파일 `:346` |
| `_EPROCESS.ThreadTimerSets` (`+0x700`) | 0 (K1 도 0) | 0 | K1·K2 282 · K3~P1 287 · P1a~P2 285 |

- **`Hand` = `DueTime` 의 손** `[관찰]` + `[정적]`: `0x3ab67754f >> 18` 의 아래 8비트 = `0xd9` — 머리 바이트 2 와 같다. `KiCommitThreadWait +0x198 SHR RCX,0x12` · `+0x19c` · `+0x1ae` (상대) 또는 `KiComputeDueTime +0xe7` (절대) 가 쓴 그대로다.
- **상대 마감이었다** `[관찰]` + `[정적: 줄]`: `TimerControlFlags` 0 → `Absolute` 비트 0. 상대 길은 `KiCommitThreadWait +0x1aa AND …,0xfe` 로 끄고, 절대 길은 `KiComputeDueTime +0x35 OR R10B,1` 로 켠다.
- **마감 길이 ≈ 240초** `[추측: 계산]`: 바이어스 둘 (`RelativeTimerBias` · `InterruptTimeBias`) 과 `ThreadTimerDelay` 가 0 이라 `DueTime = 기다림 시작 때 InterruptTime + |Timeout|`. P1a 에는 아직 `DueTime 0` 이므로 기다림은 P1a 뒤에 시작했다. `DueTime − InterruptTime` 이 P1a 기준 240.04초 · P1b 기준 239.90초 → `|Timeout|` 은 그 사이, 약 **240초 (4분)**. 그 기다림이 어느 함수 것인지는 모른다.
- **마감 전에 표에서 빠졌다 → 신호가 이긴 꼴** `[추측: 읽기]`:
  - P1b · P2 의 `InterruptTime` 은 아직 `DueTime` 보다 약 240초 앞인데 `Inserted 0` · `Expired 0` — 만기 처리 (`KiTimerExpiration`) 로는 빠질 수 없고 **취소** (`KiSwapThread +0x372 KiCancelTimer` → `+0x12b ~ +0x143` `Inserted` 지움) 로 빠진 모양이다.
  - 같은 쪽을 가리키는 것: `SignalState 0` (만기라면 `KiTimerWaitTest +0x1d7` 이 1 로 씀 — 그 뒤 0 으로 되돌리는 줄은 이번에 못 봤다), `TimerListEntry` 양쪽이 같은 커널 주소 (칸 줄에 혼자 있다가 떼어진 꼴 — 떼기는 이웃만 고치고 자기 연결은 안 지움), **P1b `WaitBlock[0]` `BlockState 5`** (`_P1b.txt:945` `0000002a'00000501` 의 바이트 `+0x151`; 타이머가 이겼다면 4 로 남는다 — `KiProcessThreadWaitList` 는 떼기만 하고 `BlockState` 를 안 씀), run77 P1b `AdjustIncrement 1` (타이머 길이면 0 — `KiProcessExpiredTimerList +0xfc` `R8D = 0`).
  - 다만 이 넷이 모두 **같은 기다림** 의 흔적인지는 모른다 (`WaitBlock[0]` · `AdjustIncrement` 는 마지막 기다림 값, 타이머 칸은 마지막 **마감 있는** 기다림 값).
- **`TimerType 1`** `[관찰]`: 덧붙임 ③ 의 갈래대로면 그때 `KiGlobalTimerResolutionRequests == 0` 이고 `PsTimerResolutionActive` 가 거짓이었다 `[추측: 읽기]`. 사진 `_EPROCESS` `RequestedTimerResolution 0` · `SmallestTimerResolution 0` (`_P1a.txt:199 · 200`) 과 어긋나지 않는다. "1 = 비 HR 표" 는 결과서 `[추측: 이름]`.
- **칸 주소로 PRCB 거꾸로 세기** `[추측: 계산]`: `TimerListEntry` 가 칸 줄 머리 (`칸 + 0x8`) 를 가리킨다고 보면 칸 번호 = `Hand 0xd9 + TimerType 1 × 0x100` = `0x1d9` 이고, `fffff805'42d73fa8 − 8 − (0x10 + 0x1d9) × 0x20 − 0x4100` = **`fffff805'42d6c180`**. 이 값은 K1 사진의 옛 트랩 프레임 `Rcx` (`_K1.txt:1059`) 와 같다 — 우연이 아니면 CPU 0 의 PRCB 이고, 결과서의 칸 식 (`KiInsertTimerTable +0x4e ~ +0x7f`) 과 맞는다. PRCB 주소는 사진에 따로 없어 확인은 아니다.
- **`Dpc` 칸** `[관찰]` + `[정적: 식]`: 일곱 장 같은 값 `0x50bc04891b743c38`. `KeInitThread +0x177 ~ +0x193` 이 `KiWaitAlways` · 타이머 주소 · `KiWaitNever` 로 **0 을** 섞은 값이고, `KiTimerWaitTest +0x2f ~ +0x4f` 가 같은 키를 거꾸로 써서 풀면 0 이 된다 (식을 이어 보면 `KiWaitAlways` 끼리 지워짐). 짐 지도 3.14 `Timer` 둘째 줄의 "`Dpc` 값의 뜻 `[추측]`" 이 닫힌다.
- **`WaitBlock[3]` 는 일곱 장 같다** `[관찰]`: 만들 때 값 (`KeInitThread +0x19e` · `+0x1d2 ~ +0x1dd`) 그대로. P1b 처럼 타이머를 한 번 쓴 뒤에도 같은 건, 취소 성공 길 (타이머 블록을 안 건드림) 이든 만기 뒤 되돌림 (`KiSwapThread +0x37f ~ +0x391`) 이든 같은 모양이라 이 칸으로는 못 가른다.
- **`TimerActive` 일곱 장 0** `[관찰]`: 깨난 뒤 `KiSwapThread +0x263 BTR` 로 끈다 (P1b). 나머지 장은 마감 기다림이 없었던 구간.
- **K2 ~ P1a, P1b → P2 에는 커밋까지 간 마감 기다림이 없었다** `[추측: 읽기]`: 타이머 칸이 그대로다. 단 마감 0 같은 **첫째 겹** (`KeWaitForSingleObject +0x240`, 블록 달기 전) 에서 끝난 기다림은 타이머를 안 건드려 흔적이 없다.
- **`ThreadTimerSets` 여덟 장 0** `[관찰]`: 덧붙임 ② — 위 240초 기다림이 `+0x108 ~ +0x130` 조건 (상대 · `WaitMode ≠ 0` · `KernelApcDisable == 0` · `WaitIrql == 0` · `+0xc0 == 0`) 을 다 맞췄다면 1 이 됐어야 한다. 그래서 그 기다림은 커널 모드 기다림이거나 APC 를 막은 채 한 기다림으로 보인다 `[추측: 읽기]`. P1b 의 `WaitMode 1` (run77) 은 그 뒤 다른 기다림 값일 수 있다.

## 확인된 것

- `[정적]` 마감 거는 줄: `KiCommitThreadWait +0x23d BTS TimerActive` · `+0x242` 헤더 락 + `Inserted` (마감 있을 때만, 맨 앞) → `+0x194` `Timer.DueTime` (상대) / `+0x4e3 KiComputeDueTime` (절대) → `+0x1f2 KiInsertTimerTable` → `+0x220 KiSwapThread`. 타이머는 `_KTHREAD.Timer` (`+0x100`), `Timer2` 안 씀.
- `[정적]` 상대 마감 = `KeWaitForSingleObject +0x3b6 ~ +0x3d7` 에서 한 번 계산 (`+0x104` 위) → `KiCommitThreadWait +0x17a ~ +0x194` → `DueTime = 기다림 시작 때 InterruptTime + |Timeout|`. 절대 마감 = `KiComputeDueTime +0xd8` `InterruptTime + (값 − SystemTime)`, 지났으면 0.
- `[정적]` 이미 지난 마감: 첫째 겹 `KeWaitForSingleObject +0x240` (`0x102`, 신호 검사 뒤), 둘째 겹 `KiInsertTimerTable +0x2a8/+0x2ab` → `KiCommitThreadWait +0x39e KiTimerWaitTest` (자기 만기).
- `[정적]` 알아채기: `KeAccumulateTicks +0x28 KiCheckForTimerExpiration` → `+0x33c/+0x33f` → `+0x3d0 KiSetDpcRequestFlag(PRCB+0x38bc, 8)` → `+0x3e7 InterruptRequest = 1` 또는 `+0x4f1` IPI `0x2f`.
- `[정적]` 처리: `KiDispatchInterrupt +0x60` → (꼬리점프) → `KiRetireDpcList +0x40d/+0x411` → `+0xca4 KiTimerExpiration` → `+0x20e KiProcessExpiredTimerList` → `+0xba KiTimerWaitTest` → `+0x325 KiTryUnwaitThread` → `+0x168 WaitStatus = 0x102`. 그리고 `KiProcessExpiredTimerList +0xfc KiProcessThreadWaitList(…, 1, 0, 2)` → `+0x9f AdjustIncrement 0` → `+0x101 KiDeferredReadySingleThread`.
- `[정적]` `0x102` = `WaitBlock[3].WaitKey` (`KeInitThread +0x19e` `0x01020401`), 블록은 스레드 만들 때 `Timer.WaitListHead` 에 매달림 (`+0x1d2 ~ +0x1dd`).
- `[정적]` 진 쪽 객체 블록은 `KiProcessThreadWaitList +0x74 ~ +0x7a` · `+0x141 ~ +0x269` 가 뗀다 (`BlockState == 4` 만, `BlockState` 는 안 씀).
- `[정적]` 겹침: `KiTryUnwaitThread +0x33` 락 → `+0x57 ~ +0x60` `State ≠ 5` 면 지나침. 신호로 깬 스레드는 `KiSwapThread +0x263` → `+0x372 KiCancelTimer`, 못 떼면 `+0x37f ~ +0x391`. 커밋 전 겹침은 `KiCommitThreadWait +0x511` 로 `TimerActive` 끔.
- `[정적]` (flow-writer) `_EPROCESS.ThreadTimerSets` (`+0x700`) 를 쓰는 줄은 `KiCommitThreadWait +0x3af` 하나 (조건 `+0x108 ~ +0x130` → `+0x1b1 / +0x1be`).
- `[관찰]` run44: 메인 스레드 타이머는 K2 ~ P1a 만들 때 모양, P1b · P2 `DueTime 0x3ab67754f` · `Hand 0xd9` · `TimerType 1` · `Absolute 0` · `Inserted 0` · `SignalState 0` · `TimerListEntry` 양쪽 `fffff805'42d73fa8`. `WaitBlock[3]` 일곱 장 `0x01020401`·스레드 자신·타이머 줄에 달림. `TimerActive` · `RelativeTimerBias` · `ThreadTimerDelay` · KUSD `InterruptTimeBias` · `ThreadTimerSets` 모두 0.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① 결과서 질문 2 "`KeInitializeTimerTable +0x4f LEA RDX,[KiTimerExpirationDpc]` → `+0x5d / ret +0x66 KeInitializeDpc`" → 부르는 줄은 **`+0x61`** (ret `+0x66`) (`_calls.tsv`, flow-writer `.asm`). DPC 자리는 `+0x45 LEA RDI,[RBX+0x83f0]`.
- ② 결과서 질문 3(b) "큐 (`WaitType 2`) … (`+0x2de KiInsertQueueInternal`, …)" → `+0x2de` 는 `MOV byte [RAX+0x11],5` (`BlockState`), 부르는 줄은 **`+0x2e9`** (ret `+0x2ee`). 동기형 쪽은 `+0x276` (flow-writer `.asm`).
- ③ 결과서 질문 3(b) "`+0x1d2 ~ +0x1dd` 에서 이 블록을 `Timer.WaitListHead` 에 … (`InsertTail`)" → 줄 꼴은 **머리에 꿰기** (`+0x1a9` 머리 `Flink` 를 읽어 `+0x1ac` 검사 → `+0x1dd MOV [RBX],RAX` 머리 `Flink = 블록`). 그때 줄이 비어 있어 결과는 같다 (flow-writer `.asm`).
- ④ 결과서 질문 1(a) "`KiTraceSetTimer` 는 ETW 켜져 있을 때만 (`… +0x209 JNZ +0x464`)" → 부르는 줄은 `+0x46c` (ret `+0x471`) (`_calls.tsv`).
- ⑤ 결과서 지도 "`KiSwapStacksAndRetireDpcList` → (꼬리점프) → `KySwapStacksAndRetireDpcList +0x0`" → 그 주소 (rva `0x6b6440`) 는 PDB 이름이 둘 (`KySwapStacksAndRetireDpcList` · `KxSwapStacksAndRetireDpcList`, `pdb_aliases.py --lookup`). 이 문서는 "DPC 스택에서 `KiRetireDpcList` 를 부르는 조각" 으로 적었다.
- ⑥ flow-writer 보탬: `ThreadTimerSets` 쓰는 줄 `KiCommitThreadWait +0x3af` 와 조건 `+0x108 ~ +0x130` → `+0x1b1 / +0x1be` (덧붙임 ②). 결과서는 `+0x102 ~ +0x139` 를 `ThreadTimerDelay` 갈래로만 적었다.
- ⑦ flow-writer 보탬: `TimerType` 이 1 이 되는 갈래를 사진 값과 맞춤 (덧붙임 ③).
- ⑧ flow-writer 보탬: 사진 대조 전체 (위 "사진과 맞춰 본 것"). 결과서 동적 3 "스레드 타이머의 실제 모양" 중 만들 때 모양과 한 번 쓴 뒤 모양은 run44 날 바이트로 **이미 닫혔다** — 남은 건 만기 순간의 비트 변화.
- ⑨ flow-writer 보탬: `Dpc` 섞은 값이 0 으로 풀린다는 결과서 문장 ("NULL 을 난독화한 값") 을 식으로 확인 (`KeInitThread +0x177 ~ +0x193` ↔ `KiTimerWaitTest +0x2f ~ +0x4f`).

## 정정

- **run77 "깨우는 쪽" `KiTryUnwaitThread` 끝 줄** — "여러 객체 기다림의 **남은 블록** 은 깨난 쪽이 나가면서 뗀다 (`KiCommitThreadWait +0x2b2 ~ +0x32a`). 한 객체 기다림은 블록이 하나라 깨난 쪽이 따로 안 뗀다."
  - → `KiCommitThreadWait +0x2b2 ~ +0x332` 는 **커밋 전에 끝난 길** (`+0xa3 JNZ +0x251` → `+0x2a0`) 에서만 탄다 (run77 질문 1(c) 표의 그 줄).
  - 잠들었다 깬 (커밋된) 기다림의 진 쪽 블록은 **깨운 쪽** 이 부르는 `KiProcessThreadWaitList` 가 뗀다: `+0x74 ~ +0x7a` (`BlockState < 5`) → `+0x141 ~ +0x269` (`BlockState == 4` 만 `RemoveEntryList`) `[정적]` (결과서 sast/74 3(b), flow-writer `.asm`). 신호 쪽은 `KiExitDispatcher +0x41`, 타이머 쪽은 `KiProcessExpiredTimerList +0xfc` 에서 부른다.
  - 그리고 한 객체 기다림도 **마감이 있으면 블록이 둘** (`WaitBlock[0]` + 타이머 `WaitBlock[3]`) 이다. 타이머가 이기면 `WaitBlock[0]` 이 객체 줄에 남고 이 줄이 뗀다.
- **run77 "사진과 맞춰 본 것"** — "`WaitRegister` 는 … 로그에 안쪽 비트가 없다. 블록 안 (`WaitType`·`BlockState`·`WaitKey`·`Object`) 도 로그에 없다."
  - → `02b KTHREAD` (`dt`) 에는 없지만 같은 로그의 **`02c ETHREAD_RAW` 날 바이트** 에 있다. 값만 옮긴다 (`[관찰]`, 뜻 풀이는 안 함 — 아래 "아직 확인 필요" 정적 ⑨):
    - `WaitRegister` 바이트 (`+0x70`): K2 · K3 · P0 **0** · P1 · P1a · P1b **1** · P2 **0** (K2 928 · K3~P1 933 · P1a~P2 931줄).
    - `WaitBlock[0]` `+0x150` (`WaitType` · `BlockState` · `WaitKey`): K2 · K3 · P0 0 · 0 · 0 · P1 · P1a · P1b **1 · 5 · 0** · P2 **1 · 4 · 0** (K2 942 · K3~P1 947 · P1a~P2 945줄). 뒤 4바이트 (`+0x154`) 는 `ContextSwitches` (run75 값과 같음).
- 두 줄 모두 run77 문서 맨 위에 정정 안내를 한 줄 넣었다.

## run75 · run76 · run77 에 반영한 것

- run77: 맨 위 정정 안내 한 줄 (위 "정정"). "정적 — 다음 바퀴 후보" 머리에 대기열 번호 **Q145** (오케스트레이터가 plan.md 에 붙인 번호), ② 에 "(스레드 블록 4개 중 `WaitBlock[3]` 은 타이머 몫 — run78)", 끝 줄 "타이머 만료 … **Q141c2**" 에 "(→ run78)".
- run76: "정적 — 다음 바퀴 후보" 끝 줄 "… 이미 **Q141c1 · Q141c2**" 에 "(→ run77 · run78)".
- run75: "정적 — 다음 바퀴 후보" 끝 줄 "타이머 만료는 Q141c2" 에 "(→ run78)".

## 짐 지도에 반영한 것 (짐 지도 바퀴 아님)

- **새 짐 칸 없음, 2장 숫자 그대로.** 이미 있는 줄에 근거만:
  - 3.14 `Timer*` 첫 줄 — "바뀜 `P1a~P1b` (타이머 걸림)" 에 run78: 쓰는 줄 (`KiCommitThreadWait +0x242` · `+0x194` · `+0x1bc` · `+0x1e4` · `+0x20f`, `KiCancelTimer +0x143`) `[정적]` + 사진 P1b 값 `[관찰]` (상대 ≈ 240초, 취소된 꼴 `[추측: 읽기]`).
  - 3.14 `Timer*` 둘째 줄 — `Dpc` 값의 뜻 `[추측]` → `[정적: 식]` (풀면 0, DPC 없음).
  - 3.14 `WaitBlock*` 줄 — `+0x1e0` `0x1020401` 의 뜻 (`WaitKey 0x102` = 만기 때 `WaitStatus`), 사진 일곱 장 같음.
  - 3.14 `MiscFlags` 줄 — 범위 밖 `[추측]` 이름 중 `TimerActive` 를 켜고 끄는 줄 `[정적]` (`KiCommitThreadWait +0x23d` 켬 · `+0x511`, `KiSwapThread +0x263` 끔).
  - 3.14 "아무도 안 씀 21" 의 `RelativeTimerBias` — 읽는 줄은 있다 (`KeWaitForSingleObject +0x319` · `+0x3ca`, `KiCommitThreadWait +0x187`), 쓰는 줄은 여전히 없음.
  - 3.11 "범위 밖 길에서만 씀 52" 의 `ThreadTimerSets` — 쓰는 줄 `KiCommitThreadWait +0x3af` `[정적]`, 사진 0.
  - 3.14 머리에 "Q78d4 에서 볼 것 — 타이머 줄" (쓰는 줄 후보 모음), 7.2 `_KTHREAD` 가리킴 한 마디, 7.4 `Dpc` 실마리 닫힘 표시, 기준 근거 목록·판 목록 (서른넷째판)·8장 두 표에 한 줄씩.

## 아직 확인 필요

동적 — **Q104 VM 단계** 에 붙인다 (오케스트레이터. 이번 묶음은 VM 안 함). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 스레드일 때만. WinDbg 이름은 `nt!KeWaitForSingleObject`.

1. **초기화 동안 마감 있는 기다림** — `nt!KiCommitThreadWait+0x23d` (`r8d ≠ 0`) 횟수와 `k`. 사진으로는 P1a ~ P1b 에 메인 스레드 한 번 (상대 ≈ 240초) `[추측: 읽기]` — 그 기다림이 어느 함수 것인지, `nt!KeWaitForSingleObject+0x3d7` `rdx` · `+0x841` `r8d`/`r9` 로 마감 값.
2. **시간 전역 값** — `dd nt!KeMaximumIncrement L1` · `KeMinimumIncrement` · `KeTimeIncrement` · `KeNonHrTimeIncrement` · `KePseudoHrTimeIncrement` · `KiGlobalTimerResolutionRequests` (사진 `TimerType 1` 이면 0 이었을 것) · `KiSerializeTimerExpiration` · `db nt!KiClockTimerPerCpuTickScheduling L1`. 만료 정밀도 (손 26.2ms 보다 늦게 터지나) 가 이 값으로 정해진다.
3. **만기 순간의 타이머 모양** — `dt nt!_KTIMER @$thread+0x100` · `dt nt!_KWAIT_BLOCK @$thread+0x1d0` 를 만기 전후로 (`Inserted`·`Expired` 비트, `SignalState`). 만들 때·한 번 쓴 뒤 모양은 run44 날 바이트로 닫힘.
4. **`PRCB+0x83f0` DPC (`KiTimerExpirationDpc`) 를 누가 넣나** — `bp nt!KiTimerExpirationDpc` 에 걸리나, 걸리면 `k`.
5. **만료 요청 비트 `0x8` 이 실제로 서나** — `ba w2 @$prcb+0x38bc` (비트 8), `nt!KiCheckForTimerExpiration+0x3c8` 횟수, `+0x3e7` (인터럽트 안) / `+0x4f1` (IPI) 중 어느 쪽.
6. **`KiRetireDpcList +0xca4` 가 어디서 오나** — `nt!KiRetireDpcList+0xca4` 에서 `k` (`KiDispatchInterrupt +0x60` 경유 / `KiIdleLoop +0x99` 경유).
7. **정말 `0x102` 로 깨나** — `nt!KiTryUnwaitThread+0x168` 에서 `r12 == 0x102` 일 때 `rbx` 가 test.exe 스레드인지.
8. **타이머 길 `Increment 0`** — `nt!KiProcessExpiredTimerList+0xfc` 에서 `r8d` (기대 0), `KiDeferredReadySingleThread +0x6d` 갈래에서 `Priority` 전후.
9. **신호가 이긴 뒤 취소** — `nt!KiSwapThread+0x372` 에 걸리나, 반환 `al` (1 성공 / 0 → `+0x37f` 되돌림). 사진 P1b 는 성공 쪽으로 읽힘 `[추측: 읽기]`.
10. **자기 만기 갈래** — `nt!KiCommitThreadWait+0x1f9` 에서 `al == 0` (경계 마감) 이 타나, 타면 `nt!KiSwapThread+0xfa/+0xfd`.
11. (사진에서 새로) **`ThreadTimerSets` 가 0 인 이유** — 1 의 기다림에서 `nt!KiCommitThreadWait+0x1b1` `sil` (0 이면 조건 불만족), 그때 `WaitMode (+0x187)` · `KernelApcDisable (+0x1e4)` · `WaitIrql (+0x186)` · `+0xc0`.
12. (사진에서 새로) **test.exe `main` 의 기다림은 마감 없음인가** — `WaitForSingleObject(t, INFINITE)` `[소스]` `sample/test.c:35` 가 `nt!KeWaitForSingleObject+0x837` 에서 `r14d == 0` 인지 (정적 ⑧ 로도 닫힘).
- 추천 멈춤점 (결과서, 모두 `nt!` — 조건: test.exe 스레드): 마감 계산 `KeWaitForSingleObject+0x3d7` (`rdx`) · `+0x240` · `+0x841` (`r8d`·`r9`), 거는 쪽 `KiCommitThreadWait+0x23d` · `+0x194` (`rcx`) · `+0x1f2` (`r9d` 손) · `+0x1f7` (`al`) · `+0x39e` · `+0x4e3`, 표 넣기 `KiInsertTimerTable+0x2a8` (`r13`·`rax`), 알아채기 `KiCheckForTimerExpiration+0x3c8` · `+0x3e7` / `+0x4f1`, 처리 `KiRetireDpcList+0x411` · `+0xca4` · `KiTimerExpiration+0x20e` · `KiProcessExpiredTimerList+0xba` (`rdx`) · `+0xfc` · `KiTimerWaitTest+0x1d7` · `+0x325` (`r8`) · `KiTryUnwaitThread+0x168`, 치우기 `KiSwapThread+0x263` · `+0x372` · `+0x37f`, 데이터 `ba w8 @$thread+0xc8` (`WaitStatus`) · `ba w1 @$thread+0x74` (`MiscFlags`, 비트 9 `TimerActive`).

정적 — 다음 바퀴 후보 (대기열 **Q146** — 오케스트레이터가 plan.md 에 붙인 번호, run79 에서 적음):

- ① **`KiTimerExpirationDpc` 를 누가 큐에 넣나** — `PRCB+0x83f0` 을 `KeInsertQueueDpc` 등에 넘기는 줄 (뽑은 `.asm` 497개엔 없음). `KiTimerExpiration` 의 두 번째 caller 길.
- ② **`KiCheckForTimerExpiration +0xd7 ~ +0xe3`** — 훑을지 정하는 비교 (`PRCB+0x8300` · `+0x8308` 대 `KiLastPseudoHrTimerExpiration` · `KiLastNonHrTimerExpiration`, `KiGlobalTimerResolutionRequests`) 의 뜻.
- ③ **`TimerType` 0/1 두 표의 이름·차이** — 표 1 이 얼마나 늦게 터지나 (`+0x381 ~ +0x3a8`, 값은 동적 2).
- ④ **`KiDeferredReadySingleThread +0x6d` 갈래가 `AdjustIncrement 0` 일 때 하는 일**, 준비 줄 앞/뒤 — Q145 ③ (깨움 부스트) 과 같은 함수.
- ⑤ `KiCancelTimer` 의 `Expired` 갈래 본체 (`+0x321 ~ +0x352` 이후 스핀), `KiTimerExpiration` 이 한 칸에 지난 타이머 여럿을 한 번에 꺼내나 (`+0x483 ~ +0x5ce`), `Timer2` 쪽 — 작음.
- ⑥ **`KeWaitForMultipleObjects` 의 마감** — `KeWaitForSingleObject` 와 같은 `KiCommitThreadWait` 를 부를 것 `[추측]`. Q145 ② (여러 객체 기다림) 과 묶을 만함.
- ⑦ **스레드 타이머 `SignalState` 를 0 으로 되돌리는 줄** — 만기 때 `KiTimerWaitTest +0x1d7` 이 1 로 쓰는데 다음 기다림 전에 누가 0 으로 하나 (`KiCommitThreadWait +0x1a2 ~ +0x1bc` 는 머리 4바이트만 씀). 사진 해석 (P1b `SignalState 0`) 이 이것에 기댄다.
- ⑧ (flow-writer) **kernelbase `WaitForSingleObject(h, INFINITE)` → `NtWaitForSingleObject` 의 `Timeout` 이 NULL 인가** — 마감 시간 바꾸는 도우미 (`BaseFormatTimeOut` 류 `[추측: 이름]`) 를 거치나. Q142 (sast/77, `CreateRemoteThreadEx`) 는 이걸 안 본다.
- ⑨ (flow-writer) **run44 날 바이트 `02c ETHREAD_RAW` 로 run77 사진 대조 다시** — `WaitRegister` (`+0x70`) · `WaitBlock[0]` 칸 (위 "정정" 의 값) 의 뜻 풀이. run77 동적 5 (`WaitRegister.State` 실제 값) 의 반은 사진 값 (P1 ~ P1b 1) 으로 볼 수 있다. Q145 나 Q78d4 에 붙일 만함.

## 디버거 메모 / 도구 메모

- 정적만이라 디버거 메모 없음.
- 도구: **`dt` 로 안 펴지는 안쪽 칸은 날 바이트 구획에 있다.** run44 덤프 읽기 로그는 구조체마다 `dt` 구획 (`02b KTHREAD` 등) 과 `dq` 날 바이트 구획 (`02c ETHREAD_RAW` 등) 을 같이 남겼다. `_KTIMER`·`_KWAIT_BLOCK` 처럼 `dt` 가 한 줄로 접는 칸은 날 바이트에서 오프셋으로 읽으면 된다 (run77 은 이걸 놓쳤다).
- 도구: 부르는 쪽 `.asm` 이 없는 이음새 (`KiRetireDpcList` 를 부르는 `KiIdleLoop` · DPC 스택 조각) 는 `_calls.tsv` 에 안 나온다 — 지도 방향 반대라 `refs` 로 찾는다 (결과서 "스크립트 문제").
