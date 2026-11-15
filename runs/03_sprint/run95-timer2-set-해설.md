# run95 timer2-set — 해설

> 이 글은 [run95 문서](run95-timer2-set.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q176a** — (Q176 ②) `NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2` 에서 이미 지난 시각 (· 0) 이면 그 자리에서 만기 처리하나, 주기 · 다시 걸기, `NtCreateTimer2` 의 동기 / 알림 인자 (보탬: `TppInitializeTimerSubQueue +0x37 R9D=8` · `+0x44 0x100002` 뜻) (`_workspace/plan.md`, 이번에 **끝**)
  - **한 바퀴로 끝났다.** 사용자 결정 (2026-11-15) "Q176 ② · Q178 ① 먼저" — Q176 을 Q176a (② 지금) 와 Q176b (나머지) 로 나눴다. 정적만.
  - 나란히 도는 것: Q178a (sast/92 bcrypt-interface). 그다음은 plan.md "다음 항목" 줄의 Q180.
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q177 ⑪ ~ ⑰** (Q177 에 더함)
    - 정적 → 새 **Q182 ① ~ ④**, **Q134 (a)** (`TpSetTimerEx`), 그리고 원래 남아 있던 **Q176b**
- 근거:
  - run 문서 [`run95-timer2-set.md`](run95-timer2-set.md) (가장 중요)
  - 요청서 [`_workspace/sast/91_timer2-set.md`](../../_workspace/sast/91_timer2-set.md) · 결과서 [`91_timer2-set_result.md`](../../_workspace/sast/91_timer2-set_result.md)
    - 판정 **분석성공**. 질문 1 ~ 3 을 `.asm` 줄로 답했다.
    - 못 정한 것 셋: (a) 즉시 만기가 `NtSetTimer2` 를 부른 스레드 위에서 도나 (CPU 에 따라) (b) Go 가 주는 `Attributes` (c) 사용자 포인터 오류 status.
    - ntoskrnl · ntdll 둘 다 다시 analyze 하지 않았다.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · 2.4 · 3장 (run78 절 한 줄 · "기다림 완료 패킷" 절 표시 · **새 절 "Timer2 걸기 · 만들기"**) · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸 없음, 메모만 (쉰한째판)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없고, run44 사진도 다시 안 읽었다 (Timer2 는 커널 개체라 사진에 칸이 없다).

---

### 이번 글의 새 말 둘 — 먼저 한 줄씩

이 글 내내 나오는 말이라, 처음 나오는 이 자리에서 푼다.

- **DPC**: 할 일 쪽지를 붙여 두면 CPU 가 손이 빌 때 처리하는 일감.
  - 이 글의 그림으로는 계산대 (CPU) 옆 **잡무 바구니** 다. 이미 [run76 해설](run76-thread-quantum-해설.md) 2.9 에서 이 이름으로 불렀다.
  - 직원 책상에 붙는 쪽지 (APC) 와는 다르다. DPC 는 직원이 아니라 **계산대** 에 딸린다 (run76 해설 2.9).
  - "손이 빌 때" = 그 계산대가 하던 급한 일을 끝내고 방해 금지 단계 ([IRQL](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)) 를 내릴 때. DPC 는 IRQL 2 에서 몰아서 처리한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명, run76 해설 그대로).
- **Timer2 줄 (두 줄) 과 머리**: `KeSetTimer2` 가 Timer2 를 꽂아 두는 **이른 순서 줄**. 줄이 둘이다 `[정적]`.
  - 첫째 줄은 "만기" (`+0x48`) 로, 둘째 줄은 "늦은 만기" (`+0x50`) 로 줄 세운다 (`RtlRbInsertNodeEx` 두 번).
  - 줄 꼴은 **레드블랙 트리** 다 — 넣을 때마다 순서대로 자리를 찾아 끼우는 정렬된 줄 (일반적인 자료 구조 설명).
  - **머리** = 줄 맨 앞 = 가장 이른 알람. 이번 바퀴의 갈림 대부분이 "새 알람이 머리가 됐나" 에서 갈린다.
  - 비유: [run78 해설](run78-thread-timer-해설.md) 2.3 의 **알람 보관대** (칸 256개짜리 둥근 시계판) 는 직원 책상 알람 (KTIMER) 몫이다. Timer2 는 그 보관대를 안 쓰고, 따로 **이른 순으로 줄 선 알람 줄 두 개** 에 선다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 로그가 아니다.** 요청서 배경의 숫자 (`NtSetTimer2` 820, 악성 224, 병목 7) 는 PE_emulator 가 받은 샘플의 로그다 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`. 이 글의 결론은 전부 코드를 읽은 것 `[정적]` 이다.
2. **test.exe 초기화에는 "걸기" 가 없다.** 초기화 길에 `NtSetTimer2` 를 부르는 줄이 지도에 없다 (힙 GC 타이머도 만들기만 — run69). test.exe 길에 있는 건 **만들기** (`NtCreateTimer2`) 쪽뿐이다. 걸기 이야기는 Go 런타임 · 스레드 풀 타이머를 켤 때의 공용 길이다.
3. **"곧바로" 의 뜻을 조심.** 지난 시각으로 걸면 "곧바로 울린다" 가 맞긴 한데, **거는 함수 안에서** 가 아니다. 거는 함수는 잡무 바구니에 요청만 넣고, 울리는 건 DPC 다 (2.6 ~ 2.8).
4. **비유가 둘이다.**
   - flow.md 3장 새 절은 **경비실 · 순찰** 비유다: 시계 담당 CPU = 경비실, DPC = 순찰, 시계 틱 = 정시 순찰, 즉시 요청 = "지금 바로 순찰 한 번" 쪽지.
   - 이 글은 run76 · run78 해설을 이어서 **계산대 · 벽시계 당번 · 잡무 스위치판 · 초인종** 으로 부른다. 그림만 다르고 사실은 같다.
5. **표시가 낮은 곳이 있다.**
   - 요청이 **언제** 배달되나 (IRQL 2 동안 보류됐다가 `KeSetTimer2 +0x49c` 에서) 와 벡터 `0x2f` 의 뜻은 `[추측]` (2.7).
   - `KiTimer2Expiration +0x290` 의 "지금 vs 만기" 읽기는 `[정적 — 결과서 해석]` (2.8).
   - status 상수 이름 (`INVALID_PARAMETER_2` 등) 과 접근 `0x100002` 의 상수 이름은 `[추측]` (2.12 · 2.14).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원** → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- CPU = **계산대**, `_KPRCB` = **계산대 장부** → [run75 해설 2.3](run75-thread-switch-해설.md#23-무대-먼저--계산대-장부-prcb-와-업무-카드의-칸)
- 시계 틱 = **벽시계 째깍**, 시계 주인 CPU = **벽시계 당번**, 디스패치 인터럽트 = **교대 · 잡무 초인종**, `InterruptRequest` = "끝나면 초인종" **메모**, 벡터 · IPI, DPC = **잡무 바구니** → [run76 해설](run76-thread-quantum-해설.md) 2.4 · 2.7 · 2.8 · 2.9
- `PRCB+0x38bc` = 잡무 **스위치판**, 비트 8 = **"알람 확인" 스위치**, `KiRetireDpcList` 가 스위치판을 떼어 와 보는 일 → [run78 해설](run78-thread-timer-해설.md) 2.3 · 2.9 · 2.10
- `InterruptTime` = 공용 게시판의 **스톱워치**, `SystemTime` = **가게 시계**, 상대 마감 ("지금부터 4분 뒤") · 절대 마감 ("오후 3시 정각") → [run78 해설 2.3](run78-thread-timer-해설.md)
- 알람 시계 알림형 (모두 깨움, 수동 신호등 같음) · 동기형 (하나만 깨우고 0, 자동 신호등 같음) → [run78 해설 2.3](run78-thread-timer-해설.md)
- 이벤트 = **신호등** (`SignalState`), 번호표, 어깨 두드리기 → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등) · [run77 해설](run77-thread-wake-해설.md)
- 스레드 풀 부품 — 일꾼 사무실 · **우편함** (IO 완료 포트) · 알람 시계 (`NtCreateTimer2`) · 대기 패킷 → [run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품), 로더 전용 사무실 → [run69 해설](run69-loader-pool-해설.md)
- 기다림 완료 패킷 = **봉투 달린 번호표**, Timer2 배달 두 갈래 (동기 `0x19` · 알림 `0x18`) → [run93 해설](run93-wait-packet-해설.md) 2.3 · 2.7
- 락 = **"작성 중" 자물쇠**, IRQL = **"방해 금지" 표지판** → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)

### 한 줄로 먼저 — 무엇을 얻었나

> **지난 시각이나 0 으로 Timer2 를 걸어도, 거는 함수 (`KeSetTimer2`) 는 알람을 직접 울리지 않는다. 만기를 "지금" 으로 적어 줄에 꽂고, 그 알람이 줄 맨 앞이 됐으면 벽시계 당번의 "알람 확인" 스위치 (비트 8) 를 켜고 초인종을 누른다. 그러면 다음 째깍을 기다리지 않고 잡무 단계 (DPC) 가 알람을 울린다. 동기 / 알림은 만들 때 `Attributes` 의 bit 31 하나로 갈리고, 스레드 풀 타이머는 `8` 이라 동기다.** `[정적]`

| 요청서 질문 | 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 지난 시각 · 0 · 그 자리 만기? | 직접 안 부름. 머리면 DPC 요청 → DPC 가 만기. 0 은 부호로만 | 2.4 ~ 2.10 |
| 2 주기 · 다시 걸기 · status | 주기 = 3번째 인자 (100 ns). 앞 것 빼고 다시 넣음, 신호 0. status 5갈래 | 2.11 · 2.12 |
| 3 동기 / 알림 인자 · TP 두 값 | `Attributes` bit 31. TP = 8 (NoWake) → 동기, 접근 `0x100002` | 2.13 · 2.14 |
| 4 지금 우리 맞나 | 지난 · 0 = 곧바로 (DPC). TP = 동기. Go = 모름 | 2.15 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간도 run93 처럼 **두 군데에 걸쳐 있다**.

- 하나는 test.exe 초기화 안. 스레드 풀이 타이머 큐를 차릴 때 `TppInitializeTimerSubQueue` 가 `NtCreateTimer2` 로 Timer2 를 만든다 (run62 · run69 — 모두 넷). 이번은 그 syscall 의 **커널 안쪽** (만들기) 이다.
- 다른 하나는 흐름도 밖 공용 길. Go 런타임 · 스레드 풀 타이머가 Timer2 를 **거는** 길 (`NtSetTimer2`) 과, 그 뒤 잡무 단계에서 울리는 길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ (2.4) 기본 스레드 풀 (run62) · 로더 전용 풀 (run69) — 타이머 큐 차리기
 │         │     TppInitializeTimerSubQueue
 │         │       ├─ +0x4d  NtCreateTimer2 ──▶ 커널 KiInitializeTimer2       ← 이번 (만들기)
 │         │       └─ +0xa0  NtAssociateWaitCompletionPacket                  (run93)
 │         │       (그 사이 NtSetTimer2 없음)                                  ← 이번 (확인)
 │         └─ … DLL 들이기 · 전원 켜기
 └─ … → NtContinue (은퇴) → exe 진입점 → main

(흐름도 밖, 공용 길 — test.exe 초기화엔 없음)
   NtSetTimer2 → ExpSetTimer2 → KeSetTimer2 → KiRequestTimer2Expiration       ← 이번 (걸기)
   잡무 단계: KiRetireDpcList → KiTimer2Expiration → KiExpireTimer2            ← 이번 (이음새)
                                                    └─ 배달 (run93)
```

### 왜 이 질문이 지금 왔나

- run93 (PE_emulator 요청 33) 이 끝나면서 남긴 정적 ② 다. run93 해설 3절 끝에서 "그다음 막힘 후보는 Timer2 를 '이미 지난 시각' 으로 걸었을 때" 라고 적었던 그 자리다.
- 요청서 배경 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`: Go 런타임과 스레드 풀 타이머가 `NtCreateTimer2` → `NtSetTimer2` → 패킷 맡김 → `NtRemoveIoCompletionEx` 로 기다린다. 병목 중 "`NtSetTimer2` 뒤 끝" 이 7.
- 엔진이 모르는 것 둘:
  - (가) 이미 지난 시각 (또는 0) 으로 걸면 **그 자리에서 울리나, 다음 째깍까지 기다리나**.
  - (나) 만든 Timer2 가 **동기 (`0x19`) 인가 알림 (`0x18`) 인가** 를 정하는 인자.

### 무엇이 궁금했나

요청서 질문 넷.

1. **지난 시각 · 0**: 절대 / 상대를 가르는 줄, 어느 시계를 읽나, 만기 ≤ 지금이면 `KiExpireTimer2` 를 바로 부르나 줄에 넣고 다음 째깍에 맡기나, 0 을 따로 다루나, 바로라면 신호 · 패킷 배달이 부른 스레드 안에서 일어나나.
2. **주기 · 다시 걸기**: 주기 칸, 이미 걸린 걸 다시 걸 때 앞 것을 빼는 줄, status 표.
3. **동기 / 알림 인자**: 어느 인자 · 어느 비트. 그리고 `TppInitializeTimerSubQueue` 의 두 값 (`R9D=8` · `0x100002`) 뜻.
4. **우리 맞나**: 엔진에 한 줄씩.

### 바퀴 전에 알던 것

- `[정적]` (run93) Timer2 가 울리면 `KiTimer2Expiration +0xd7` → `KiExpireTimer2`. 동기 (`0x19`) 는 `+0x3a8` 배달 뒤 `+0x3ad` 에서 신호 1 내림, 알림 (`0x18`) 은 `+0x702` 배달 뒤 신호 그대로.
- `[정적]` (run93) `TppInitializeTimerSubQueue` 가 `+0x4d` 로 만들고 `+0xa0` 으로 패킷을 맡긴다. 그 Timer2 넷은 "안 울림" 갈래일 것 `[추측: KeInitializeTimer2 안 읽음]`.
- `[정적]` (run78) 직원 책상 알람 (KTIMER) 쪽: 째깍마다 `KiCheckForTimerExpiration +0x3d5` 가 스위치 8 을 켜고 → `KiRetireDpcList +0xca4 KiTimerExpiration`. 같은 자리 `+0xcc6 KiTimer2Expiration` 은 **이름만** 봤다.
- `[추측]` (sast/89 결과서 151줄) Go 런타임 타이머는 동기 (자동 리셋).
- 커널 쪽 걸기 함수는 주소만 있었다 (`_index.tsv`: `NtSetTimer2` 26 B · `ExpSetTimer2` 474 B · `KeSetTimer2` 2030 B · `NtCreateTimer2` 458 B · `KeInitializeTimer2` 19 B).

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 를 줄여 옮겼다 `[정적]`.

```
NtSetTimer2 (핸들, DueTime*, Period*, Parameters*)
 │  +0x4 DueTime* == NULL → 0xC00000F0
 └─ +0x9  ExpSetTimer2
      ├─ +0xfb   ObReferenceObjectByHandle (접근 2, ExpIRTimerObjectType)
      ├─ +0x17d  KeSetTimer2 (타이머, DueTime, Period, Parameters)        IRQL 2
      │    │  +0x63/+0x66  > 0 절대 (지났으면 상대 0) · ≤ 0 상대
      │    │  +0x6c ~ +0x9c  만기 = 지금 + |상대|
      │    ├─ +0xfe / +0x10e  이미 걸렸으면 앞 것 빼기 (KiRemoveTimer2)
      │    │  +0x130  신호 = 0 · +0x48 만기 · +0x58 Period
      │    ├─ +0x571 · +0x365  두 줄에 넣기 (RtlRbInsertNodeEx ×2)
      │    │  +0x392 머리? → +0x4e6 만기 > 지금?
      │    ├─ +0x6bd  KiRequestTimer2Expiration        머리 · 만기 ≤ 지금
      │    │    ├─ +0x27  KiSetDpcRequestFlag(소유 PRCB+0x38bc, 8)
      │    │    ├─ +0x42  KiSendSoftwareInterrupt(소유 CPU, 2)     다른 CPU
      │    │    └─ +0xdd  HalpInterruptSendIpi(0x2f)               같은 CPU
      │    └─ +0x7a7  KiSendClockInterruptToTargetProcessor     그 밖 · HighResolution
      └─ +0x18a  ObfDereferenceObjectWithTag  → EAX = 0

[DPC]  KiRetireDpcList +0x40d 비트 8 → +0xcc6 KiTimer2Expiration → +0xd7 KiExpireTimer2

NtCreateTimer2 (핸들*, Reserved1*, Reserved2, Attributes, DesiredAccess)
 └─ +0x10a  KeInitializeTimer2 → +0x9 KiInitializeTimer2
              +0xa 헤더 8B = 0 · +0xd ~ +0x15 형 = bit31 ? 0x18 : 0x19
```

- **`NtSetTimer2` · `ExpSetTimer2` · `KeSetTimer2` 의 호출 목록 어디에도 `KiTimer2Expiration` · `KiExpireTimer2` · `KiInsertQueueInternal` 이 없다** `[정적]` (`_calls.tsv`). 이 한 줄이 질문 1 의 큰 답이다.
- `KeSetTimer2` 가 부르는 나머지 아홉 (`KiTraceSetTimer2` · `KiUpdateTimer2Collections` · `PsTimerResolutionActive` 등) 은 줄기 밖이라 이름만 (run 문서).

### 2.2 한 줄씩 읽는 법

트리에서 세 줄을 골라 "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 줄 1: `├─ +0x6bd  KiRequestTimer2Expiration` (`KeSetTimer2` 아래)

- **누가**: `KeSetTimer2` (Timer2 를 줄에 꽂는 본체, 2030 B).
- **어디서**: 자기 시작에서 `+0x6bd` 바이트 (ret `+0x6c2`). 앞에서 `+0x435 TEST R14B` · `+0x438 JNZ` 로 이 자리로 뛰어 온다.
- **누구를**: `KiRequestTimer2Expiration` (rva `0x26c778`). 인자 없음.
- **무엇을 위해**: 새로 꽂은 알람이 **줄 머리이고 만기가 이미 지금 이하** 일 때 (`R14B = 1`), 벽시계 당번에게 "지금 바로 알람 확인" 을 요청하려고 `[정적]`.
- 이 줄이 이번 바퀴의 **한가운데** 다. "지난 시각이면 어떻게 되나" 의 답이 "이 줄로 요청만 건다" 이다.

#### 줄 2: `├─ +0x27  KiSetDpcRequestFlag(소유 PRCB+0x38bc, 8)` (`KiRequestTimer2Expiration` 아래)

- **누가**: `KiRequestTimer2Expiration`.
- **어디서**: `+0x27` (ret `+0x2c`). 바로 앞에서 인자를 준비한다 — `+0xa EDI = KiClockTimerOwner` (벽시계 당번 번호) · `+0x1c RBX = KiProcessorBlock[EDI]` (그 계산대 장부) · `+0x20 RCX = RBX+0x38bc` · `+0x17 EDX = 8`.
- **누구를**: `KiSetDpcRequestFlag` — 스위치판에 비트를 켜고 옛 값을 돌려주는 작은 도우미 (run78 해설 2.2 줄 2 와 같은 함수).
- **무엇을 위해**: **벽시계 당번 계산대** 의 잡무 스위치판에서 **"알람 확인" 스위치 (비트 8)** 를 켜려고 `[정적]`.
- 바로 뒤 `+0x2e JNZ` — 옛 값이 `& 0xa9` 면 이미 요청이 있는 것이라 거기서 끝 (초인종을 또 안 누름).
- 비유: run78 에서는 **째깍 담당** 이 이 스위치를 켰다. 이번엔 **알람을 맞추는 사람이 직접** 켠다. 같은 스위치판, 같은 스위치다 (2.7 끝 표).

#### 줄 3: `+0xd ~ +0x15  형 = bit31 ? 0x18 : 0x19` (`KiInitializeTimer2` 안)

호출이 아니라 **명령 네 줄** 이다. 그런데 "동기냐 알림이냐" 의 답이 이 네 줄이다.

- **누가**: `KiInitializeTimer2` — `NtCreateTimer2 +0x10a` → `KeInitializeTimer2 +0x9` 로 한 단계 내려온 곳. (`KeInitializeTimer2` 는 19 B 껍데기라 형을 쓰는 줄이 여기 있다.)
- **무엇을**: `R9D` 에는 `NtCreateTimer2` 의 4번째 인자 `Attributes` 가 그대로 들어 있다 (`NtCreateTimer2 +0x107 MOV R9D,EDI`).
  - `+0xd TEST R9D,R9D` — `Attributes` 를 부호 있는 32비트 수로 본다. bit 31 이 부호 비트다.
  - `+0x10 SETNS AL` — 음수가 **아니면** (bit 31 없음) `AL = 1`, 음수면 (bit 31 있음) `AL = 0`.
  - `+0x13 ADD AL,0x18` — 그래서 `AL` 은 `0x19` 또는 `0x18`.
  - `+0x15 MOV [RCX],AL` — 타이머 머리 첫 바이트 (형) 에 쓴다.
- **그래서**: bit 31 이 있으면 **`0x18` 알림**, 없으면 **`0x19` 동기** `[정적]`. 기본 (아무것도 안 주면) 이 동기다.
- 바로 앞 `+0xa MOV qword [RCX],0` 이 머리 첫 8 바이트를 0 으로 지운다. 신호 칸 (`+0x4`) 이 그 안에 있어서 **만들 때 신호 = 0** 이다 (2.14).

### 2.3 무대 — 이번에 쓰는 칸

뒤에서 오프셋이 나오면 이 절로 돌아오면 된다.

#### Timer2 몸 (`_KTIMER2`, `0x88` B — PDB, flow-writer `pdb_struct.py`)

| 칸 | 이름 / 쓰임 | 이 글에서 부르는 말 |
|---|---|---|
| `+0x0` 바이트 | 형 — **`0x18` 알림 / `0x19` 동기** (`KiInitializeTimer2 +0x15`). bit 7 = 타이머 잠금 (결과서) | 알람 종류 · 알람 자물쇠 |
| `+0x1` 바이트 | bit 0 = 걸려 있음 (Inserted), `0x20` = 꺼둠 (Disabled), 하위 4비트 ≠ 0 = 만기 처리 중 (결과서) | "꽂혀 있음" · "꺼둠" 딱지 |
| `+0x4` | `SignalState` | **울렸음** (1) |
| `+0x18` · `+0x30` | 줄 고리 둘 (48 B) — 첫째 줄 · 둘째 줄 | 두 줄에 꿰는 고리 |
| `+0x48` | **만기** (첫째 줄 열쇠) | 울릴 스톱워치 값 |
| `+0x50` | **늦은 만기** (둘째 줄 열쇠) — 만기 + `NoWakeTolerance` | "이때까지는 울려야" 값 |
| `+0x58` | `Period` | 반복 간격 |
| `+0x80` | `AbsoluteSystemTime` — 절대로 걸었나 | "가게 시계로 맞춤" 표시 |
| `+0x81` | `TypeFlags` (IdleResilient · HighResolution · NoWake …) | 알람 성질 |
| `+0x82` · `+0x83` | `CollectionIndex` 2 B — 줄 번호 | 어느 줄 묶음 |

- 겉 객체 (Timer2 몸을 감싼 커널 개체, 본문 `0xa8` B — `NtCreateTimer2 +0xd6`) 에는 `+0x88` ← 0 · `+0x90` ← DueTime · `+0x98` ← NoWakeTolerance · `+0xa0` ← Attributes 가 들어간다 (결과서 · run 문서).
- run78 의 직원 책상 알람 (`_KTIMER`, `0x40` B) 과 **다른 구조체** 다. 형 숫자도 다르다 (KTIMER `8`/`9` ↔ Timer2 `0x18`/`0x19`). 동기 · 알림이라는 뜻은 같다.

#### `NtSetTimer2` 인자 넷 (이름은 공개 헤더 관례, 뜻은 `.asm`) `[정적]`

| # | 레지스터 | 뜻 | 근거 |
|---|---|---|---|
| 1 | RCX | 타이머 핸들 — 접근 `0x2`, 형 `ExpIRTimerObjectType` | `ExpSetTimer2 +0xd8` · `+0xf3` · `+0xfb` |
| 2 | RDX | `DueTime*` (int64). NULL → `0xC00000F0` | `NtSetTimer2 +0x4 ~ +0x14` |
| 3 | R8 | `Period*` (int64, 100 ns). NULL → 주기 0 (한 번) | `ExpSetTimer2 +0x79` · `+0x7c JNZ` |
| 4 | R9 | `Parameters*` (`_EXT_SET_PARAMETERS_V0` 16 B), NULL 가능 | `ExpSetTimer2 +0x86 JZ` · `+0x9a` 복사 |

- 부른 쪽이 유저면 (`PreviousMode`, 스레드 `+0x232` — `ExpSetTimer2 +0x41 ~ +0x51`) 포인터를 `0x7fffffff0000` 으로 눌러 읽고, 셋 (DueTime 8 · Period 8 · Parameters 16) 을 **커널 쪽 지역 변수로 복사** 한다 (`+0x53 ~ +0xa9`). 커널이 부르면 `+0x51 JZ +0xc7` 로 건너뛴다.
- `+0x9a` 의 복사 함수는 `_calls.tsv` 에 `alias:RtlCopyVolatileMemory+1` 로 찍혔다. 같은 주소 이름이 둘 (`RtlCopyVolatileMemory` · `RtlpCopyDeviceMemory`) 이라 run 문서는 "복사" 로만 쓴다. 이 글도 그렇다.

### 2.4 시각 → 만기: 부호 하나로 가르고, 스톱워치로 맞춘다 (질문 1) `[정적]`

run 문서 "`KeSetTimer2` 차례 표" 9 ~ 13번.

1. **`+0x25/+0x28` → `+0x6df ~ +0x6ec`** — Period 가 0 이 아니면, `KeMinimumIncrement` 보다 작을 때 그 값으로 올린다 (부호 있는 비교라 음수도 올라감). (2.11)
2. **`+0x2e ~ +0x4f`** — `TypeFlags` (`+0x81`) 를 읽어 두고, **IRQL 을 2 로 올린다** (`MOV CR8`). 방해 금지 표지판을 건다.
3. **`+0x63 TEST RSI,RSI` · `+0x66 JG`** — **DueTime 의 부호만 본다.**
   - `> 0` → `+0x678` **절대** 갈래.
   - `≤ 0` (0 포함) → `+0x6c` **상대** 갈래.
4. **절대 갈래 `+0x678 ~ +0x6a5`** — 절대 표시 `R15B = 1`. 가게 시계 (`SystemTime`, KUSD `+0x14`, `+0x685`) 를 읽어 비교한다.
   - `DueTime > SystemTime` (아직 미래) → 상대 = `SystemTime − DueTime` (음수).
   - **아니면 (이미 지남) → 상대 = 0** (`+0x698 SUB` · `+0x69b CMP` · `+0x69e CMOVG` · `+0x6a2`).
   - 그리고 `+0x6c` 상대 갈래로 합류한다.
5. **`+0x6c ~ +0x9c`** — **만기 = 지금 (`InterruptTime`, KUSD `+0x8`) − 상대** (`+0x86 SUB RDI,RSI`). 상대가 0 이하라 실제로는 더해진다.
   - 넘치면 (`+0x8c JC` · `+0x96 JZ`) → `+0x23b` 만기 `0xFFFFFFFFFFFFFFFE` — 영영 안 오는 시각.
   - HighResolution 타이머면 "지금" 을 `+0x24c RtlGetInterruptTimePrecise` 로 더 정밀하게 읽는다.

- 그래서 → 지난 절대 시각도, `DueTime = 0` 도 **만기 = 지금** 이 된다. **0 을 따로 다루는 줄은 없다.** 부호 검사 하나에 실려 상대 0 이 될 뿐이다 `[정적]`.
- 비유: 알람을 "오후 3시 정각" (가게 시계) 으로 맞추라는데 이미 3시 반이면, 관리사무소는 따지지 않고 "지금부터 0분 뒤" 로 바꿔 적는다. 그리고 알람은 늘 **스톱워치 눈금** 으로 적는다 (run78 해설 2.3 과 같은 규칙).

#### 작은 비교 — run78 의 스레드 기다림과 0 을 넣는 쪽이 다르다

- run78 해설 2.3: `KeWaitForSingleObject` 는 마감 값이 **0 이상이면 절대** 로 본다. 그래서 0 은 절대 쪽에서 "값이 0 이면 무조건" 으로 처리된다 (run78 해설 2.8 첫째 겹).
- 이번 `KeSetTimer2` 는 **`> 0` 만 절대**, 0 은 **상대** 쪽이다 (`+0x66 JG`).
- 둘 다 코드에 쓰인 것이다 `[정적]`. 같은 "0" 이 두 함수에서 다른 문으로 들어간다는 것만 짚어 둔다.

### 2.5 꽂기 — 잠금, 앞 것 빼기, 칸 쓰기, 두 줄 `[정적]`

차례 표 14 ~ 18번.

1. **`+0x9f ~ +0xcc`** — 늦은 만기 = 만기 + `NoWakeTolerance` (Parameters 가 있고 `+0x82 ≠ 0x17` 일 때. −1 = 끝 없음). 아니면 늦은 만기 = 만기.
2. **`+0xee` (ret `+0xf3`) `KiAcquireTimer2LockUnlessDisabled`** — 알람 자물쇠를 잡는다. "꺼둠" (`[+1] & 0x20`) 이면 TRUE → `+0xf5 JNZ +0x4b6` **아무것도 안 하고** 끝.
3. **`+0xfe` (ret `+0x103`) `KiAcquireTimer2CollectionLockIfInserted`** — 이미 걸려 있으면 (`[+1]` bit 0) TRUE.
   - TRUE → **`+0x10e` (ret `+0x113`) `KiRemoveTimer2`** — 앞 것을 줄에서 뺀다 (2.11).
   - FALSE → `+0x105 JZ +0x311` (만기 처리 중이던 타이머인지 확인 — 2.11 끝).
4. **`+0x130 ~ +0x147` 칸 쓰기** — `+0x4` 신호 ← **0** · `+0x48` 만기 · `+0x50` 늦은 만기 · `+0x58` Period · `+0x80` 절대 표시.
5. **`+0x1b2 ~ +0x596` 두 줄에 넣기** — 첫째 줄 (`+0x571` `RtlRbInsertNodeEx`, 고리 `+0x18`, 열쇠 `+0x48`) · 둘째 줄 (`+0x365`, 고리 `+0x30`, 열쇠 `+0x50`). `+0x82 == 0x17` 이면 첫째 줄은 건너뛴다 (`+0x1b5 JZ +0x264`).
   - 줄 맨 앞이 되면 `ESI` / `R12D` = 1 (`+0x593` · `+0x383`).

- 비유: 알람 자물쇠를 잡고 → 전에 꽂혀 있던 같은 알람을 뽑고 → 새 시각을 적고 "울렸음" 표시를 지우고 → 이른 순 줄 두 개에 끼운다. 끼우다 보니 맨 앞에 섰는지 기억해 둔다.

### 2.6 핵심 갈림 — 머리인가, 만기가 지났나 (질문 1) `[정적]`

차례 표 19 ~ 23번. 여기서 "지난 시각이면 어떻게 되나" 가 정해진다.

1. **`+0x38a OR ESI,R12D` · `+0x392 JNZ +0x4bf`** — 두 줄 중 하나라도 **머리가 됐을 때만** 아래로. 아니면 `+0x398` (요청 안 함).
2. **`+0x4bf ~ +0x4ec`**:
   - `KiNextTimer2DueTime` (다음 Timer2 만기 — 전역 값) 이 내 만기보다 크면 내 만기로 낮춘다 (`+0x4c3` · `+0x4cc`).
   - **`+0x4d8` 에서 지금 (`InterruptTime`) 을 다시 읽고**, **`+0x4e2 CMP [타이머+0x48],지금`** · **`+0x4e6 JA +0x398`**.
     - 만기 > 지금 (미래) → 뛴다 → `R14B = 0`.
     - **만기 ≤ 지금** → 안 뛴다 → **`+0x4ec R14B = 1`**.
3. `+0x3c6 ~ +0x3f4` — 머리 상태 바이트에 "걸림" 을 쓰고 자물쇠를 푼다.
4. `+0x421 ~ +0x433` — 예외 갈래: `KiForceIdleDisabled == 0` 이고 `KiForceIdleState == 4` 면 `+0x47c` (요청 안 함).
5. **`+0x435 TEST R14B` · `+0x438 JNZ` → `+0x6bd` (ret `+0x6c2`) `KiRequestTimer2Expiration()`** — 즉시 요청 (2.7).
6. `+0x47c ~ +0x4b4` — IRQL 을 되돌리고 (`+0x49c MOV CR8`) 돌아간다. `AL` = 앞 것이 걸려 있었나.

#### 세 경우의 길 (run 문서 표 그대로)

| 경우 | `KeSetTimer2` 안에서 | 만기 (`+0x48`) | 즉시 요청? | 울리는 곳 |
|---|---|---|---|---|
| **지난 절대** (`DueTime > 0`, `≤ SystemTime`) | `+0x66 JG` → `+0x678` · `+0x69e CMOVG` 안 탐 → 상대 **0** | = 지금 | **예** — 머리면 `+0x4e6 JA` 안 뜀 → `+0x6bd` | DPC (`KiRetireDpcList +0xcc6` → `KiTimer2Expiration +0xd7` → `KiExpireTimer2`), 째깍 안 기다림 |
| **`DueTime = 0`** | `+0x66 JG` 안 탐 → `+0x6c` 상대. **0 을 따로 다루는 줄 없음** | = 지금 | **예** (위와 같음) | 위와 같음 |
| **미래 상대** (`< 0`) | `+0x6c ~ +0x9c` 지금 + \|DueTime\| | > 지금 | **아니오** — `+0x4e6 JA +0x398` (HighResolution 이면 `+0x7a7` 시계 인터럽트만 앞당김) | 째깍 뒤 `KiTimer2Expiration` 이 `지금 ≥ 만기` 일 때 |
| **미래 절대** | `+0x678 ~ +0x6a5` 로 상대로 바꾼 뒤 위와 같음 | > 지금 | 아니오 | 위와 같음 |

#### 왜 "거는 함수가 직접 울리지 않는다" 고 말할 수 있나 — 단계별로

1. `KeSetTimer2` 가 부르는 함수 목록 (`_calls.tsv`) 에 `KiExpireTimer2` · `KiTimer2Expiration` · `KiInsertQueueInternal` 이 **없다**. `NtSetTimer2` · `ExpSetTimer2` 도 마찬가지다 `[정적]`.
2. 만기 ≤ 지금일 때 가는 곳은 `+0x6bd KiRequestTimer2Expiration` 하나다 `[정적]`.
3. 그 함수가 하는 일은 스위치 켜기 + 초인종 누르기뿐이다 (2.7). 알람을 꺼내는 줄이 없다 `[정적]`.
4. 그래서 → **신호 켜기 · 패킷 배달은 `NtSetTimer2` 의 호출 사슬 안이 아니라 DPC 몫** 이다 `[정적]`.

- 비유: 알람을 "이미 지난 시각" 으로 맞추면, 맞추는 사람이 그 자리에서 알람을 울리지는 않는다. 대신 벽시계 당번 계산대의 **"알람 확인" 스위치를 올리고 초인종을 누른다.** 울리는 건 잡무 바구니를 비우러 온 사람이다.

#### run78 의 직원 책상 알람과 다른 점

- run78 해설 2.8 둘째 겹: 직원 책상 알람 (KTIMER) 은 꽂으려는 칸 맨 앞이고 이미 지났으면, 꽂은 걸 도로 빼고 `KiCommitThreadWait +0x39e KiTimerWaitTest` 로 **자기 손으로** 울렸다 `[정적]`.
- 이번 Timer2 는 꽂은 채로 두고 **요청만** 한다 `[정적]`.
- 둘 다 "맨 앞일 때만 검사" 라는 꼴은 같다 (run78 `+0x1fe TEST BL,2` · 이번 `+0x392`).

### 2.7 즉시 요청의 몸 — `KiRequestTimer2Expiration` (rva `0x26c778`) `[정적]`

run 문서가 `+0x0 ~ +0xe2` 전부를 다시 봤다.

```
KiRequestTimer2Expiration ()
 │  +0xa EDI = KiClockTimerOwner · +0x1c RBX = KiProcessorBlock[EDI] · +0x20 RCX = RBX+0x38bc · +0x17 EDX = 8
 ├─ (1) +0x27  KiSetDpcRequestFlag(소유 PRCB+0x38bc, 8)     옛 값 & 0xa9 → +0x2e JNZ 끝
 │  +0x30/+0x39  현재 PRCB (GS:[0x20]) == 소유 PRCB ?
 ├─ (2) +0x42  KiSendSoftwareInterrupt(소유 CPU, 2)          다르면
 │  같으면 +0x53 [PRCB+0x20] ≠ 0 → +0x59 [PRCB+6] = 1 (InterruptRequest) 로 끝
 └─ (3) +0xdd  HalpInterruptSendIpi(…, 0x2f)                  같고 0 이면 — 자기 자신에게
```

세 갈래를 한 걸음씩:

1. **스위치부터 켠다** — 벽시계 당번 (`KiClockTimerOwner`) 의 계산대 장부 `+0x38bc` 에 비트 8. 이미 요청이 있었으면 (`& 0xa9`) 여기서 끝.
2. **내가 벽시계 당번이 아니면** (`+0x39 CMP` · `+0x3c JZ` 안 탐) → `+0x42 KiSendSoftwareInterrupt(소유 CPU, 2)` — 당번 계산대에 초인종을 보낸다.
3. **내가 벽시계 당번이면** (`+0x53` · `+0x57 JZ +0x5f`):
   - `[PRCB+0x20]` (결과서는 `NestingLevel`) 이 0 이 아니면 → `+0x59 [PRCB+6] = 1` — **"끝나면 초인종" 메모** 만 남기고 끝.
   - 0 이면 → `+0xcb EDX = 0x2f` … **`+0xdd HalpInterruptSendIpi`** — 자기 계산대에 초인종 (IPI) 을 한 번.
   - 그 사이 `+0x5f ~ +0xc8` 에 AMD TPR 우회 갈래가 끼어 있다 (`+0x64` 검사 → `+0x7b HalpDisableInterrupts` · MSR `0xc0010015`). 결론 (자기 IPI `0x2f`) 에는 영향 없다 (run 문서 보탬 ①).

- 비유: 스위치를 올린 다음, 벽시계 당번이 **다른 계산대** 면 그쪽으로 초인종 줄을 당긴다. **자기 자신** 이면 지금 인터럽트 처리 중인지 보고, 중이면 "끝나면 초인종" 메모만, 아니면 자기 초인종을 직접 누른다.

#### 언제 울리나 — 여기부터는 `[추측]`

- 벡터 `0x2f` 가 DISPATCH 단계 인터럽트라는 것 `[추측]` (결과서).
- `KeSetTimer2` 는 IRQL 2 를 걸어 둔 채 이 요청을 한다 (2.4 의 2번). 그래서 자기 초인종은 IRQL 2 동안 **보류** 됐다가, `+0x49c MOV CR8` 로 IRQL 을 내리는 순간 배달된다 `[추측]` (결과서 — 그때 돌아갈 자리 = `KeSetTimer2 +0x4a0` 근처).
- 그래서 run 문서는 "다음 째깍을 기다리지 않는다" 를 `[정적]` 으로, "정확히 언제 배달되나" 는 `[추측: 인터럽트 배달 시점]` 으로 나눠 적었다.

#### run78 과 같은 꼴 — 째깍이 할 일을 거는 쪽이 대신 한다

| | 누가 스위치를 켜나 | 스위치 | 초인종 거는 법 | 받는 쪽 |
|---|---|---|---|---|
| run78 (KTIMER, 째깍 때) | `KiCheckForTimerExpiration +0x3d5` | `PRCB+0x38bc` 비트 8 | `+0x3e7 InterruptRequest = 1` / `+0x4f1 HalpInterruptSendIpi 0x2f` | `KiRetireDpcList +0x40d` |
| **run95 (Timer2, 걸 때)** | **`KiRequestTimer2Expiration +0x27`** | **같은 칸 · 같은 비트** | `+0x59 [PRCB+6] = 1` / `+0xdd HalpInterruptSendIpi 0x2f` (다른 CPU 면 `+0x42`) | **같은 `KiRetireDpcList +0x40d`** |

- 어긋나는 줄은 없다 — **정정 없음** (run 문서 보탬 ⑤).

### 2.8 받는 쪽 — 잡무 단계에서 Timer2 줄을 훑는다 `[정적]`

run78 해설 2.10 의 `KiRetireDpcList` 표에서 **이름만** 봤던 마지막 줄 (`+0xcc6 KiTimer2Expiration`) 이 이번에 이어졌다.

1. **`KiRetireDpcList +0x406 XCHG [PRCB+0x38bc]`** — 스위치판을 통째로 떼어 오며 0 으로 (run78).
2. **`+0x40d TEST DIL,8` · `+0x411 JNZ +0xbca`** — "알람 확인" 스위치가 있었나.
3. … **`+0xca4` (ret `+0xca9`) `KiTimerExpiration`** — 직원 책상 알람 보관대 훑기 (run78).
4. **`+0xca9 CMP byte [PRCB+0x21],0`** — 이 계산대가 벽시계 당번인가 (run76 의 `ClockOwner` 칸).
5. **`+0xcc6` (ret `+0xccb`) `KiTimer2Expiration`** — 당번이면 Timer2 줄도 본다.
6. `KiTimer2Expiration +0x4b` — 지금 vs `KiNextTimer2DueTime`. 아직이면 `+0x4e JC +0x16e` 로 바로 끝.
7. `+0x290` — 이른 순으로 꺼낸다 → **`+0xd7` (ret `+0xdc`) `KiExpireTimer2`** → 신호 · 패킷 배달 (run93 해설 2.7 "배달 길 표").

- 이 사이 `KiRetireDpcList +0xbca ~ +0xca4` 의 갈림 (`+0xc51` · `+0xc5d` · `+0xc76`) 은 안 따라갔다 `[공백]` (→ Q182 ④).
- **표시 하나 낮춤** (run 문서 보탬 ②): `KiTimer2Expiration +0x290` 의 실제 줄은 `CMP R13,[RSI+0x30]` 이다. 결과서는 `RSI` 가 첫째 줄 고리 (`+0x18`) 를 가리킨다고 보고 `0x18 + 0x30 = 0x48` (만기 칸) 로 읽었다. 그래서 이 한 줄은 **`[정적 — 결과서 해석]`** 이다.
- 비유: 잡무 바구니를 비우러 온 사람이 스위치판을 떼어 보고 "알람 확인" 이 켜져 있으면, 먼저 직원 책상 알람 보관대를 훑고 (run78), **이 계산대가 벽시계 당번이면** Timer2 줄도 앞에서부터 훑어 지난 알람을 울린다.

### 2.9 같은 직원 위에서 울리나? — 정적으로는 못 정한다

요청서 질문 1 의 마지막 ("바로 만기라면, 부른 스레드 안에서 일어나나").

- `[정적]` 갈림 자리는 `KiRequestTimer2Expiration +0x39` 의 비교 하나다 — 지금 계산대 == 벽시계 당번 계산대?
- `[추측]` (결과서 · flow.md) 같으면 자기 초인종이 `KeSetTimer2 +0x49c` 에서 배달돼, `NtSetTimer2` 를 부른 **그 직원 위에서** 잡무 단계가 돌고 알람이 울린다. 그러면 `KiExpireTimer2` 콜스택 아래에 `KeSetTimer2+0x4a0` 근처가 보일 것이다.
- `[추측]` 다르면 당번 계산대에서 따로 돈다. 그러면 `NtSetTimer2` 가 `STATUS_SUCCESS` 를 돌려주는 때와 우편함에 패킷이 들어가는 때의 **앞뒤가 경쟁** 이 된다.
- 그래서 → "돌려주기 전 / 후" 순서는 진짜 Windows 에서도 **CPU 에 따라 다르다** (run 문서 "에뮬레이터 모양 제안"). → Q177 ⑫.

### 2.10 미래 값은 아무리 작아도 다음 째깍 이후 `[정적]`

- 상대 `−1` (100 ns) 이라도 만기 = 지금 + 1 > 지금 → `+0x4e6 JA +0x398` → 즉시 요청 없음.
- 그다음은 run78 과 같은 길이다: 째깍 담당이 `KiTimer2Expiration +0x4b` 비교 (`지금 ≥ KiNextTimer2DueTime`) 에 걸릴 때까지 기다린다.
- 그래서 **"0" 과 "아주 작은 음수" 는 길이 다르다.** 0 은 곧바로 (DPC), −1 은 다음 째깍 이후 (해상도에 따라 몇 ms — 동적 Q177 ⑭).
- 예외 하나 — **HighResolution** 타이머 (`Attributes` `0x4`):
  - 만기는 정밀 시계로 계산하지만 `+0x4d8` 비교는 공용 게시판 `InterruptTime` 이다. 그래서 상대 0 이어도 `R14B = 0` 이 될 수 있다.
  - 그때는 `+0x7a7 KiSendClockInterruptToTargetProcessor(KiClockTimerOwner)` 가 **벽시계 째깍을 앞당긴다** (만기가 `지금 + KeMaximumIncrement` 안쪽 등).
- 비유: 4분 뒤로 맞춘 알람은 물론, "0.0000001초 뒤" 로 맞춘 알람도 **다음 째깍** 이 와야 당번이 알아챈다. "지금" 으로 맞춘 알람만 스위치를 바로 올린다.

### 2.11 주기 · 다시 걸기 (질문 2) `[정적]`

#### 주기는 3번째 인자 하나

- 구조체 칸이 아니라 **`NtSetTimer2` 의 3번째 인자** (`Period*`, int64) 다.
- `ExpSetTimer2 +0x108 MOV R8,[RSP+0x40]` → `+0x17d` → `KeSetTimer2 +0x143 MOV [RBX+0x58],R14` (`_KTIMER2.Period`).
- `Parameters` 구조체에는 주기 칸이 **없다** (Version · Reserved · NoWakeTolerance 뿐, 2.12).
- 단위는 100 ns 로 읽힌다 — 울릴 때 `KiExpireTimer2` 가 `Period` 를 그대로 더한다 (`+0xad CMP` · `+0xb1 JNZ +0x2b4` 주기 갈래 → `+0x2cf MOV R11,[RBX+0x58]` · `+0x2d6 ADD R11,R10` → `+0x353 KiInsertTimer2WithCollectionLockHeld` 다시 넣기).
- 다만 다음 만기가 **"울린 때의 지금 + Period"** 인지 **"이전 만기 + Period"** 인지는 `R10` 이 무엇이냐에 달렸다. 결과서는 "지금 + Period" 로 읽었고 run 문서는 `[확인 필요]` 로 뒀다 (→ Q182 ③).
- 작은 주의: 주기가 `KeMinimumIncrement` 보다 작으면 그 값으로 올린다 (2.4 의 1번). 부호 있는 비교라 음수 주기도 올라간다.

#### 다시 걸기 = 같은 호출 안에서 빼고 넣기

1. `KeSetTimer2 +0xfe KiAcquireTimer2CollectionLockIfInserted` — 이미 걸려 있나.
2. `+0x10e KiRemoveTimer2` — 앞 것을 뺀다.
3. `+0x120` 잠금 풀기 → `+0x128` 반환 값 = TRUE ("앞 것이 걸려 있었음").
4. 곧바로 `+0x130 ~` 새 값으로 다시 넣는다.

- **걸 때마다 신호를 0 으로 지운다** (`+0x130`). 이미 울려서 초록이던 알람도 다시 걸면 빨강이 된다.
- `KeSetTimer2` 가 돌려준 "앞 것 있었나" (`AL`) 를 `ExpSetTimer2` 는 **버린다** (`+0x182` 뒤 `+0x18f MOV EAX,EDI`). 그래서 `NtSetTimer2` 를 부른 쪽은 그걸 알 수 없다.

#### 울리는 중에 다시 걸 때

- `[+1]` 하위 4비트 ≠ 0 인데 걸려 있지 않으면 = 지금 울리는 중인 알람.
- 그때는 `+0x105 JZ +0x311` → `+0x31f EBP = 8` (SetPending 표시) → 줄 넣기 · 요청을 **건너뛴다** (`+0x16e` · `+0x171 JNZ +0x72f` · `+0x72f XOR R14B,R14B`).
- 대신 울리는 쪽 `KiTimer2Expiration +0x382 TEST AL,8` → `+0x399 KiInsertTimer2` 가 **나중에** 넣는다.
- 이때도 신호는 `+0x130` 에서 0.
- 비유: 지금 막 울리고 있는 알람을 누가 다시 맞추면, 맞추는 사람은 "다시 맞춤" 딱지만 붙여 두고, 울리기를 끝낸 당번이 그 딱지를 보고 줄에 다시 꽂는다.

### 2.12 `Parameters` 와 status 표 (질문 2) `[정적]`

#### `Parameters` (`_EXT_SET_PARAMETERS_V0`, `0x10` B, PDB)

| 오프셋 | 이름 | 코드에서 |
|---|---|---|
| `+0x0` | Version | **0 이어야** 한다. 아니면 `0xC00000F1` (`ExpSetTimer2 +0x125` · `+0x129 JNZ +0x1ca`) |
| `+0x4` | Reserved | 안 읽음 |
| `+0x8` | NoWakeTolerance | int64, **−1 이상** (`< −1` 이면 `0xC00000F1`, `+0x12f` · `+0x134 JL`). −1 = 끝 없음. 그 외 = 늦은 만기 = 만기 + 값. 형 바이트 `+0x82 == 0x17` (플래그 없는 보통 타이머) 이면 안 씀 |

#### `NtSetTimer2` status — 값은 `.asm`, 상수 이름은 `[추측]`

| 상황 | status | 근거 |
|---|---|---|
| 성공 (꺼둔 타이머라 아무것도 안 해도) | `0` | `ExpSetTimer2 +0x26 XOR EDI,EDI` · `+0x18f MOV EAX,EDI` |
| `DueTime` 포인터 NULL | `0xC00000F0` | `NtSetTimer2 +0x7 → +0x14` |
| 핸들 오류 | `ObReferenceObjectByHandle` 값 그대로 | `ExpSetTimer2 +0x102 JS +0x191` |
| Period ≠ 0 인데 IdleResilient (`[+0x81]` bit 1) | `0xC000000D` | `+0x115 JNZ +0x1ad` · `+0x1ad` · `+0x1ba` |
| Version ≠ 0 · NoWakeTolerance < −1 | `0xC00000F1` | `+0x129` · `+0x134` → `+0x1ca` |
| 사용자 포인터 읽다 틀림 | **모름** — `.asm` 에 예외 처리기 줄이 안 보임 `[공백]` | `+0x6a` · `+0xb6` 읽는 줄 |

- 눈여겨볼 곳: `Parameters` (4번째 인자) 가 틀렸는데 나오는 값이 `0xC00000F1` 이다. 결과서는 이 값의 이름을 "INVALID_PARAMETER_3" 으로 보고, 그래서 "4번째 인자 오류인데 이 값이 나온다" 고 적었다 (이름은 `[추측]`).
- 옛 `NtCreateTimer` 로 만든 핸들을 넣으면 형이 안 맞아 실패할 것이다 — 값은 `[추측: 0xC0000024]` (run 문서 PE_emulator 표).

### 2.13 만들기 — 동기 / 알림은 bit 31 하나 (질문 3) `[정적]`

#### `NtCreateTimer2` 인자 다섯

| # | 레지스터 | 이름 (공개 헤더 관례) | 뜻 |
|---|---|---|---|
| 1 | RCX → R14 | TimerHandle (출력) | 끝에 `+0x156` 에서 핸들을 씀 |
| 2 | RDX → R10 | Reserved1* | NULL 이 아니려면 `Attributes & 2` 필요 (아니면 `0xC00000F0`). 4 B 읽어 `+0x195 ExpCheckIRTimerAccess` |
| 3 | R8 → R11 | Reserved2 | NULL 이어야 함, 아니면 `0xC00000F1` (`+0x3b` · `+0x3e JNZ +0x1cd`) |
| 4 | R9D → EDI | **Attributes** | 아래 두 표 |
| 5 | 스택 | DesiredAccess | `+0x142 ObInsertObjectEx` 의 접근 |

#### 형을 고르는 길

`NtCreateTimer2 +0x107 MOV R9D,EDI` → `+0x10a KeInitializeTimer2(객체, 0, 0, Attributes)` → `+0x9 KiInitializeTimer2` → `+0xd ~ +0x15` (2.2 줄 3).

| `Attributes` bit 31 (`0x80000000`) | 형 바이트 `[타이머+0]` | 뜻 |
|---|---|---|
| 있음 | **`0x18`** | 알림 — 울린 뒤 신호 그대로 (`KiExpireTimer2 +0x702/+0x707`, run93) |
| 없음 (기본) | **`0x19`** | 동기 — 배달 뒤 신호 1 내림 (`KiExpireTimer2 +0x3ad`, run93) |

- IdleResilient 길 (`+0x1ad KeInitializeIRTimer`, 그 안 `+0x21 OR R9D,2`) 도 같은 `KiInitializeTimer2` 로 가서 bit 31 규칙은 같다 (결과서).
- 비유: 알람 시계를 살 때 주문서 (`Attributes`) 맨 왼쪽 칸에 체크하면 **수동 신호등 같은 알람** (알림, 울리면 계속 초록), 비워 두면 **개찰구 같은 알람** (동기, 한 번 통과하면 빨강) 이 온다. 기본은 개찰구다.

#### 다른 `Attributes` 비트 — 이름만 (PDB `_KTIMER2.TypeFlags`)

`NtCreateTimer2 +0x2e ExpExTimerAttributesAreValid` 가 먼저 거른다: `(A & 0x8000000e) == A` 아니면 `0xC00000F2`, 이어 `A & 0xe` 가 `{0, 2, 4, 8, 6, 0xc}` 중 하나여야 한다 (결과서).

| 비트 | 값 | 이름 | 효과 (코드에서) |
|---|---|---|---|
| 31 | `0x80000000` | (PDB 이름 없음. `EX_TIMER_NOTIFICATION` 은 `[추측]`) | 형 `0x18` |
| 1 | `0x2` | IdleResilient | `KeInitializeIRTimer` 길 · Reserved1 검사 · **주기 불가** (`ExpSetTimer2 +0x1ad`) |
| 2 | `0x4` | HighResolution | 정밀 시계 · 째깍 앞당김 (2.10) |
| 3 | `0x8` | NoWake | `[+0x81]` 에 들어감. 줄 번호 `+0x82/+0x83` 은 표 `KiTimer2Combinations` 에서 — `0x8` → `{0x20, 0x24}`, 0 → `{0x17, 0x04}` (결과서) |
| 4 · 5 | `0x10` · `0x20` | PseudoHighRes · AusterityResilient | 인자로 못 줌. 안에서 자동 |
| — | `0xa` · `0xe` 등 | — | 표에 없는 조합 → `0xC00000F2` |

- 각 성질 (NoWake 등) 이 **실제로 무엇을 바꾸나** 는 이번에 보지 않았다. 이 글은 코드에 보이는 효과만 옮겼다.

### 2.14 스레드 풀 타이머 큐의 두 값 — 동기, 그리고 "만들 때 꺼짐" (질문 3 보탬) `[정적]`

`TppInitializeTimerSubQueue` (ntdll) 가 `+0x4d NtCreateTimer2` 를 부르기 전에 채우는 인자:

| 줄 | 무엇 | 뜻 |
|---|---|---|
| `+0x37 MOV R9D,0x8` | 4번째 `Attributes = 8` | NoWake 만. bit 31 없음 → **동기 `0x19`**. bit 1 없음 → `KeInitializeTimer2` 길 (주기 허용) |
| `+0x44 MOV dword [RAX-0x38],0x100002` | 5번째 `DesiredAccess` (`RAX` = 들어올 때 RSP → `RSP+0x20`) | `0x100000` (SYNCHRONIZE) + `0x2` (TIMER_MODIFY_STATE) `[추측: 상수 이름]` |
| `+0x4b XOR EDX,EDX` · `+0x41 XOR R8D,R8D` · `+0x34 MOV RCX,RBX` | Reserved1 = 0 · Reserved2 = 0 · RCX = 서브큐 `+0x18` (핸들 칸) | — |

- 그 뒤 `+0x65 NtCreateWaitCompletionPacket` → `+0xa0 NtAssociateWaitCompletionPacket` (대상 = 방금 만든 타이머).
- 이 함수의 CALL 은 **다섯뿐** 이다 (`+0x4d` · `+0x65` · `+0xa0` · `+0xbe TppGetCurrentThreadNumaNode` · `+0x109 NtClose`). **만들기와 맡기기 사이에 `NtSetTimer2` 가 없다.**

#### 그래서 run93 의 `[추측]` 하나가 반쯤 올라간다 — 단계별로

1. run93: test.exe 타이머 큐 Timer2 넷은 맡길 때 "안 울림" 갈래일 것 `[추측: KeInitializeTimer2 안 읽음]`.
2. 이번: `KiInitializeTimer2 +0xa` 가 머리 첫 8 B 를 0 으로 쓴다. 신호 칸 (`+0x4`) 이 그 안에 있다 → **만들 때 신호 0** `[정적]`.
3. 이번: 만들기 (`+0x4d`) 와 맡기기 (`+0xa0`) 사이에 이 함수는 거는 호출이 없다 `[정적]`.
4. 그래서 → "만들 때 꺼져 있고, 이 함수가 맡기기 전에 켜지 않는다" 는 **`[정적]`** 으로 오른다.
5. 남는 것: "그 사이 **다른 스레드** 가 그 핸들로 켜지 않았다" 만 `[추측]`.
- run93 결론 (안 울림) 은 그대로다. **정정이 아니라 받침** 이다 (run 문서 보탬 ③). run93 문서는 안 고쳤다.

#### 그리고 배달 갈래도 정해진다

- run93 "배달 길 표" 의 Timer2 두 갈래 중, test.exe 타이머 큐 Timer2 는 **동기 갈래** (`KiExpireTimer2 +0xca == 0x19` → `+0x3a8` → `+0x3ad` 신호 −1) 를 탄다 `[정적]` (보탬 ④).
- 비유: 일꾼 사무실이 사 오는 알람은 모두 **개찰구 알람** 이고, 사 올 때 빨강으로 오며, 봉투 번호표를 걸기 전에 아무도 맞추지 않는다.

### 2.15 PE_emulator 쪽 — 우리 맞나 · 틀리나 (질문 4)

run 문서 표에서 줄만 골랐다. 진짜 쪽은 `[정적]` (26100.9457). 에뮬레이터 코드는 flow-writer 도 안 읽었다.

| 갈래 | 진짜 | 엔진이 할 것 |
|---|---|---|
| 지난 절대 시각 | 만기 = 지금 → 머리면 즉시 DPC 요청 (`+0x6bd`) | 곧바로 만기 처리 (다음 째깍 기다리지 않음) |
| `DueTime = 0` | 상대 0 과 같음 | 위와 같음 |
| 미래 상대 · 절대 | 줄에 넣고 끝 | 시각이 올 때까지 기다림 |
| `DueTime` 포인터 NULL | `0xC00000F0` | 같은 값 |
| `Parameters` 틀림 | `0xC00000F1` | 같은 값 (4번째 인자인데 이 값) |
| 주기 + IdleResilient | `0xC000000D` | 같은 값 |
| 스레드 풀 타이머 형 | 동기 `0x19` (`Attributes = 8`) | 동기 — 배달 뒤 신호 0 |
| Go 타이머 형 | 부르는 쪽 `Attributes` bit 31 | **모름** — Q177 ⑮ |

- 엔진 모양 제안 (run 문서): 지난 시각 · 0 이면 `NtSetTimer2` 를 끝낸 **직후** 에 만기 처리 (신호 1 → 맡긴 패킷 배달 → 동기면 신호 0) 를 한 번 돌리면 된다. `STATUS_SUCCESS` 반환과 패킷 들어감의 순서는 진짜에서도 CPU 따라 다르다 (2.9).
- `NtSetTimer2` 는 "앞 것 있었나" 를 안 돌려주고, 걸 때마다 신호를 0 으로 지운다 (2.11).

### 2.16 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 ① ~ ⑧.

- ① (자리 바로) `KiSendSoftwareInterrupt` call 은 `+0x42/+0x47` (결과서의 `+0x3e ~ +0x47` 은 인자 준비부터 묶은 범위). AMD TPR 우회 갈래 (`+0x5f ~ +0xc8`) 가 결과서엔 빠져 있었다. 결론 같음 (2.7).
- ② (표시만 낮춤) `KiTimer2Expiration +0x290` → `[정적 — 결과서 해석]` (2.8).
- ③ (보탬) test.exe 타이머 큐 Timer2 넷 "만들 때 꺼짐" `[정적]` — run93 받침, 정정 아님 (2.14).
- ④ (보탬) 그 넷은 동기 배달 갈래 (2.14).
- ⑤ (보탬) run78 과 같은 비트 · 같은 칸 · 같은 받는 쪽, 정정 없음 (2.7).
- ⑥ (결과서 고쳐 적음) 결과서는 "kernelbase PDB 가 없다 → 먼저 받기" 라 했지만, 서버엔 **`ms/sym/KernelBase.dll.pdb`** (대문자 K · B) 가 이미 있다. 파일 이름 대소문자 때문에 못 찾은 것으로 보인다 `[추측]`. 다만 `ms/disassemble/kernelbase/` 에 `_index.tsv` 는 없다.
- ⑦ (자리 바로) 결과서 "`TppInitializeTimerSubQueue +0x33 XOR EDX,EDX`" — `+0x33` 은 명령 경계가 아니다. `XOR EDX,EDX` 는 **`+0x4b`** (2.14 표).
- ⑧ (보탬) `KeSetTimer2` 의 줄기 밖 callee 아홉은 이름만 (2.1).

### 2.17 디버거 · 도구 메모

- **디버거가 붙어서 달라지는 점** `[추측]`: `KeSetTimer2` 는 "지금" 을 **두 번** 읽는다 (`+0x6c` 만기 계산 · `+0x4d8` 비교).
  - 그 사이에 멈춤점을 걸면, 멈춘 동안 `InterruptTime` 이 흐른다.
  - 그러면 **미래 (작은 상대) 값이 "지난 값" 갈래 (`R14B = 1`) 로 바뀔 수 있다.**
  - 그래서 `+0x4e6` 을 보려면 그 자리에만 걸고, `+0x6c ~ +0x4d8` 사이엔 걸지 말 것.
  - 비유: 알람 시각을 적고 나서 "지났나" 를 다시 볼 때까지 사이에 누가 붙잡고 있으면, 스톱워치는 그동안에도 간다.
- **함정 후보** (flow-writer 가 windbg-kd-runbook pitfalls 에 추가 제안 — 사용자 확인 대기): `KiExpireTimer2` · `KiTimer2Expiration` · `KiRetireDpcList` 는 **DPC 안에서** 돈다.
  - 그 순간의 프로세스는 우연히 그 계산대에 있던 프로세스라 `$proc` (test.exe) 조건이 안 맞는다 `[추측: DPC 일반 성질]`.
  - test.exe 타이머만 보려면 `NtCreateTimer2` 쪽에서 타이머 주소를 먼저 적어 두고, 그 주소로 조건을 건다.
- 도구 문제는 없다. `alias:RtlCopyVolatileMemory+1` 은 같은 주소 이름 둘이라 "복사" 로만 썼다 (2.3).

### 2.18 정리 — 비유로 한 번 더

- 일꾼 사무실 (스레드 풀) 은 알람 시계 (Timer2) 를 살 때 주문서 맨 왼쪽 칸 (bit 31) 을 비워 둔다 → **개찰구 알람** (동기 `0x19`). 알람은 빨강으로 온다.
- 알람을 맞추면 (`NtSetTimer2`) 관리사무소는:
  - 방해 금지 표지판을 걸고,
  - 시각을 **스톱워치 눈금** 으로 바꿔 적고 (가게 시계 시각이 이미 지났으면 "지금", 0 이면 그대로 "지금"),
  - 전에 꽂혀 있던 같은 알람을 뽑고, "울렸음" 을 빨강으로 지우고,
  - 이른 순 **알람 줄 두 개** 에 끼운다.
- 끼운 알람이 **줄 맨 앞** 이고 시각이 **이미 지금 이하** 면:
  - 벽시계 당번 계산대의 **"알람 확인" 스위치** 를 올리고,
  - 당번이 남이면 그쪽 초인종을, 자기면 "끝나면 초인종" 메모나 자기 초인종을 누른다.
  - 알람은 **여기서 안 울린다.**
- 초인종이 울리면 당번 계산대의 잡무 단계 (`KiRetireDpcList`) 가 스위치판을 보고, 직원 책상 알람 보관대를 훑은 다음 **Timer2 줄** 을 훑어 지난 알람을 울린다 (`KiExpireTimer2`). 봉투 번호표가 걸려 있으면 우편함으로 (run93).
- 시각이 아직 미래면 (0.0000001초 뒤라도) 스위치를 안 올린다. **다음 째깍** 이 와야 당번이 알아챈다.
- 울리는 계산대가 알람을 맞춘 그 직원의 계산대인지는 그때그때 다르다 — 그래서 "맞췄어요 (0)" 답과 "봉투 들어감" 의 순서도 그때그때 다르다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — ntkrnlmp · ntdll PDB 줄 | — | run95 몫 추가 |
| 기준 — 근거 로그 | — | (run95) 줄: 정적 바퀴, Timer2 · wait packet 은 커널 개체라 run44 사진에 칸이 없음 → 값 대조 없음. 에뮬레이터 숫자는 이 문서의 `[관찰]` 아님 |
| 흐름도 앞 글 | — | run95 문단: **그림은 안 고쳤다** — test.exe 초기화 길에 `NtSetTimer2` 가 없고, `NtCreateTimer2` 는 이미 2.4 run62 절에 글로만 있는 syscall 의 커널 안쪽 |
| 그림 | — | **그대로** |
| 2.4 제목 · run62 절 | run 목록 run93 까지 | run95 추가, **(run95) 줄 하나** — 타이머 큐 Timer2 는 동기 `0x19` · NoWake, 만들 때 신호 0, 만들기와 맡기기 사이 `NtSetTimer2` 없음 `[정적]` |
| 3장 run78 절 ("마감 · 타이머") | 째깍 쪽 (`KiCheckForTimerExpiration +0x3d5`) 요청 줄 | **(run95) 한 줄** — Timer2 를 걸 때도 같은 요청 비트 (`KiRequestTimer2Expiration +0x27`), 받는 쪽 같은 `+0x40d`, 시계 소유 CPU 면 `+0xcc6` 도 돈다 `[정적]` |
| 3장 "기다림 완료 패킷" 절 | 맡김 6번 "안 울림" `[추측]` · 남은 정적에 `KeSetTimer2` | `[추측]` 줄에 (run95) — Timer2 넷이 만들 때 꺼져 있다는 건 `[정적]`. 남은 것 줄에서 `KeSetTimer2` 지움 (해결됨 → run95) |
| 3장 끝 | "기다림 완료 패킷" 절이 끝 | **새 절 "Timer2 걸기 · 만들기"** — 경비실 · 순찰 비유 + 트리 + 줄 (지난 시각 · 0 · 주기 · status · 동기 / 알림 `[정적]`, 같은 CPU 갈래 `[추측]`, 남은 것) |
| 4장 | run93 정적 ② · 동적 4 열림 | run93 정적 ② **해결됨 → run95**, run93 동적 4 일부 → run95, run95 새 줄 셋 (동적 · 정적 · PE_emulator 와 결과서와 다르게 적은 곳) |
| 5장 | — | run 표 · 03_sprint 요청서 번호에 run95 줄 |

태그가 바뀐 것 (전 → 후):

- "지난 시각이면 `KeSetTimer2` 가 그 자리에서 `KiExpireTimer2` 로 가나" (run93 남은 질문) → **아니다. 머리면 DPC 요청만 하고, 울리는 건 `KiRetireDpcList +0xcc6` 쪽** `[정적]`. 같은 CPU 에서 부른 스레드 위로 도는지는 `[추측]`.
- test.exe 타이머 큐 Timer2 넷의 "안 울림" `[추측: KeInitializeTimer2 안 읽음]` (run93) → **만들 때 꺼짐 · 맡기기 전 거는 호출 없음** `[정적]`. "다른 스레드가 안 켬" 만 `[추측]`.
- "Go 런타임 타이머는 동기" `[추측]` (sast/89) → **스레드 풀 타이머 = 동기** `[정적]`. Go 는 여전히 **모름** — 부르는 쪽 `Attributes` 에 달렸다.
- `KiRetireDpcList +0xcc6 KiTimer2Expiration` "이름만" (run78) → 비트 8 · 시계 소유 CPU 조건 (`+0xca9`) · 그 뒤 `+0xd7 KiExpireTimer2` 까지 이어진 사슬 `[정적]` (`+0x290` 한 줄은 `[정적 — 결과서 해석]`).

### 짐 지도 `baggage.md` 에서 바뀐 것 (쉰한째판)

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** — Timer2 는 커널 개체라 체크리스트 밖이다.
- 이미 있는 줄에 (run95) 메모만:
  - 4장 `TppPoolpGlobalPool` 짝 줄 — 타이머 2 (그리고 로더 풀 것 2) 의 안쪽 칸: 형 `0x19` · 신호 0 · `TypeFlags` NoWake · 줄 번호.
  - 7.6 run62 "커널 개체 10 핸들" 아래 Timer2 메모, 7.6.9 커널 개체 줄.
- 기준 run95 줄 · 판 목록 · 8장 두 표.

### 큰 그림에서 어떻게 이어지나

- **우리 지도 (test.exe) 의 길은 안 바뀐다.** 만들기는 원래 2.4 에 있던 syscall 이고, 이번엔 그 **커널 안** 을 채웠다. 걸기는 test.exe 초기화 길에 없다.
- 짐 지도 쪽에서는 은퇴 (P2) 때 커널에 남는 Timer2 넷의 모양이 또렷해졌다: **동기 · NoWake · 신호 0**, 그 위에 패킷이 하나씩 걸린 채 `[정적]` + `[추측]` (다른 스레드가 안 켬). 맞는지는 Q177 ⑪ · ⑥ (test.exe 로 됨) 이 닫는다. Q104 VM 단계에 붙일 후보다.
- "알람이 울리는 길" 이 이제 둘로 정리된다:
  - 직원 책상 알람 (KTIMER) → 째깍 때 스위치 → 잡무 단계 `KiTimerExpiration` (run78). 지난 걸 꽂으면 **자기 손으로** 울림.
  - **Timer2** → 째깍 때 **또는 걸 때** 스위치 → 같은 잡무 단계의 `KiTimer2Expiration` (run95). 지난 걸 꽂아도 **요청만**.
- PE_emulator 쪽: run93 이 "안 울린 대상에 맡기기 → 나중에 배달" 을 줬고, 이번에 "지난 시각 · 0 이면 곧바로 울림 (DPC)" 과 "스레드 풀 = 동기" 를 줬다. Go 가 막힌 자리 (`NtSetTimer2` 뒤 7) 의 남은 물음은 **Go 가 주는 `Attributes`** 하나로 좁혀졌다 (→ Q177 ⑮ 동적 · Q182 ① 정적 kernelbase 길).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q177 ⑪ ~ ⑰** (동적, Q177 에 더함)

| 무엇을 모르나 | 왜 정적으로 못 닫나 | 샘플 |
|---|---|---|
| ⑪ 타이머 큐 Timer2 넷의 형 바이트가 정말 `0x19` · `+0x81` `0x08` · `+0x82/+0x83` `{0x20, 0x24}` 인가 | 코드는 값을 정하지만 실제 개체를 본 적은 없다 | **test.exe 로 됨** — Q177 ⑥ (맡김 6번) 과 같은 멈춤 묶음 후보 |
| ⑫ 즉시 만기 DPC 가 `NtSetTimer2` 를 부른 스레드 위에서 도나 | 지금 CPU == 벽시계 당번인지는 실행 때 일 (2.9) | 다른 샘플 — test.exe 초기화엔 `NtSetTimer2` 가 없다 (지난 시각으로 거는 샘플이 필요할 수 있음, 사용자 결정) |
| ⑬ 지난 시각 · 0 에서 `+0x4e6 JA` 가 정말 안 뛰나, 미래면 뛰나 | 갈림 값은 실행 때 시각 | 다른 샘플 |
| ⑭ 상대 −1 이 정말 다음 째깍까지 걸리나 (몇 ms) | 타이머 해상도는 실행 때 값 | 다른 샘플 |
| ⑮ Go 가 `NtCreateTimer2` 에 주는 `Attributes` · DesiredAccess, 만든 직후 형 바이트 (= ⑦) | 값은 Go 가 정한다 | Go 샘플 (사용자 결정) |
| ⑯ 사용자 포인터가 틀렸을 때 `NtSetTimer2` status | `.asm` 에 예외 처리기 줄이 안 보임 `[공백]` | 새 샘플 |
| ⑰ `KiForceIdleState == 4` 일 때 요청이 빠지는 것 | 정상 부팅에선 드묾 `[추측]` | — |

추천 멈춤점 (run 문서, 커널 멈춤점이라 **test.exe 조건** 필요. DPC 안 자리는 2.17 함정 참고):

- `nt!KiInitializeTimer2` 진입 `r9d`, 돌아온 뒤 `db @r11 L1` · `db @r11+0x81 L3` (콜스택 ret `TppInitializeTimerSubQueue+0x52`) — ⑪.
- `nt!KeSetTimer2+0x4e6`: `poi(@rbx+0x48)` vs `poi(0xfffff78000000008)` — ⑬.
- `nt!KiRequestTimer2Expiration` 진입 (ret `KeSetTimer2+0x6c2`) + `dd nt!KiClockTimerOwner L1` 과 현재 CPU 번호 — ⑫.
- `nt!KiExpireTimer2` 진입 (ret `KiTimer2Expiration+0xdc`): `k` · `dd @rcx+4 L1` — ⑫.

### 정적으로 남은 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| kernelbase `CreateWaitableTimerExW` · `SetWaitableTimerEx` 가 `NtCreateTimer2` / `NtSetTimer2` 로 무엇을 넘기나 — 옛 `NtCreateTimer` 를 쓰는지부터 (Go 의 길 후보) | 이번 범위 밖 (kernelbase). PDB 는 이미 있고 `_index.tsv` 는 없음 | **Q182 ①** — PE_emulator 질문에 가장 가까움 |
| `NtCancelTimer2` · `KeCancelTimer2` — 취소 · 꺼둠, 맡긴 패킷은 어떻게 되나 | 이번엔 걸기 · 만들기만 (`ExpSetTimer2 +0x1c3 KeCancelTimer2` 는 자리만 봤다) | **Q182 ②** |
| 주기 타이머의 다음 만기 = "이전 만기 + Period" 인가 "지금 + Period" 인가 | `KiExpireTimer2 +0x2d6` 의 `R10` 출처를 안 따라감 (2.11) | **Q182 ③** |
| `KiRetireDpcList +0xbca ~ +0xcc6` 사이 갈림 (`+0xc51` · `+0xc5d` · `+0xc76`) 과 `KiSendSoftwareInterrupt` — 요청 한 번으로 Timer2 잡무가 정말 도는 조건 | 이번엔 호출 자리만 읽음 `[공백]` (2.8) | **Q182 ④** |
| 스레드 풀이 타이머를 켤 때 (`TpSetTimerEx` → `NtSetTimer2`) 넘기는 `DueTime` · `Period` · `Parameters` 모양 | ntdll 켜기 쪽은 범위 밖 (run69 남은 정적과 같은 자리) | **Q134 (a)** |
| `NtCreateWaitCompletionPacket` · `NtRemoveIoCompletionEx` · `ObGetAssociatedWaitObject` · 닫기 처리기 | Q176 을 나눌 때 뒤로 미룸 | **Q176b** |

### 대기열 밖 — 이름 · 짐작으로 남은 것

- 벡터 `0x2f` 가 DISPATCH 단계라는 것, IRQL 2 동안 보류됐다가 `+0x49c` 에서 배달된다는 것 — `[추측]` (2.7).
- status 상수 이름 (`INVALID_PARAMETER_2` 등), 옛 타이머 핸들일 때 `0xC0000024`, 접근 `0x100002` 의 상수 이름, bit 31 의 공개 이름 `EX_TIMER_NOTIFICATION` — 모두 `[추측]`.
- `KiTimer2Expiration +0x290` 이 만기 칸을 본다는 해석 — `[정적 — 결과서 해석]` (2.8).
- 두 줄 (만기 · 늦은 만기) 을 왜 따로 두는지, NoWake · IdleResilient 같은 성질이 실제로 무엇을 바꾸는지 — 이번에 안 봄.
- 함정 후보 둘 (DPC 안 `$proc` 조건 · `KeSetTimer2` 시각 두 번 읽기) 을 pitfalls 에 넣을지 — 사용자 확인 대기 (2.17).

---

## 5. 더 읽기

- run 문서: [`run95-timer2-set.md`](run95-timer2-set.md) — "PE_emulator 쪽에서 쓸 것" (Q176a 결론 · 맞나 틀리나 표), "정적 — 호출 지점", "`KeSetTimer2` 차례 표", "`KiRequestTimer2Expiration`", "세 경우의 길", "주기 · 다시 걸기", "status 표", "`NtCreateTimer2` — 동기 / 알림", "스레드 풀 타이머 큐", "호출 관계", "결과서와 다르게 적은 곳 · 보탠 것", "디버거 메모 / 도구 메모"
- 결과서: [`_workspace/sast/91_timer2-set_result.md`](../../_workspace/sast/91_timer2-set_result.md) — "한눈에", 질문 1 ~ 4, "동적으로 확인해야 할 것", "다음 요청 후보"
- 요청서: [`_workspace/sast/91_timer2-set.md`](../../_workspace/sast/91_timer2-set.md) — 배경 (에뮬레이터 로그), 알고 싶은 것
- flow.md: [3장 "Timer2 걸기 · 만들기"](flow.md) 절, 3장 run78 절 · "기다림 완료 패킷" 절의 (run95) 표시, 2.4 run62 절의 (run95) 줄, 4장 run95 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 4장 `TppPoolpGlobalPool` 짝 줄, 7.6 · 7.6.9
- 대기열: [`_workspace/plan.md`](../../_workspace/plan.md) Q176a (끝) · Q176b · Q177 · Q182 · Q134
- 앞 해설:
  - [run93](run93-wait-packet-해설.md) — 봉투 달린 번호표, Timer2 배달 두 갈래 (2.7), test.exe 맡김 6번 (2.15)
  - [run78](run78-thread-timer-해설.md) — 스톱워치 · 가게 시계 · 상대 / 절대 (2.3), 이미 지난 마감 두 겹 (2.8), 째깍 때 스위치 (2.9), 잡무 단계 `KiRetireDpcList` (2.10)
  - [run76](run76-thread-quantum-해설.md) — 벽시계 째깍 · 벽시계 당번, 초인종 거는 두 갈래 (2.7), DPC = 잡무 바구니 (2.9)
  - [run62](run62-tp-etw-init-해설.md) 2.2 — 스레드 풀 부품 (알람 시계 `NtCreateTimer2`), [run69](run69-loader-pool-해설.md) — 로더 전용 풀 · 힙 GC 타이머
  - [run13](../02_sprint/run13-resume-thread-해설.md) 2.6 — IRQL "방해 금지" 표지판
