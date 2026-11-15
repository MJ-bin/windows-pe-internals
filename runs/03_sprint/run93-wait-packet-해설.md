# run93 wait-packet — 해설

> 이 글은 [run93 문서](run93-wait-packet.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q175** — PE_emulator 요청 33 (기다림 완료 패킷이 "아직 안 울린 대상" 에 맡겨졌을 때 커널이 하는 일) (`_workspace/plan.md`, 이번에 **끝**)
  - **한 바퀴로 끝났다.** 사용자 결정 (2026-11-15): Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만.
  - 다음 항목: **[멈춤 — Q104 VM 단계 앞, 사용자 확인 대기]**.
  - 이번에 남은 것이 간 곳 (4절):
    - 정적 → 새 **Q176** (작은 것 다섯)
    - 동적 → 새 **Q177** (test.exe 로 볼 수 있는 것 + 다른 샘플이 필요한 것. Q104 에 붙일지는 사용자 결정)
- 근거:
  - run 문서 [`run93-wait-packet.md`](run93-wait-packet.md) (가장 중요)
  - 요청서 [`_workspace/sast/89_wait-packet.md`](../../_workspace/sast/89_wait-packet.md) · 결과서 [`89_wait-packet_result.md`](../../_workspace/sast/89_wait-packet_result.md)
    - 판정 **분석성공**. 요청서 질문 1 ~ 5 모두 `.asm` 줄로 답했다.
    - ntoskrnl 은 다시 analyze 하지 않았다 (`export` 5번).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · 2.4 · 3장 (run77 절 · run78 절 한 줄씩 + **새 절 "기다림 완료 패킷"**) · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸 없음, 메모만 (마흔아홉째판)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없고, run44 사진도 쓰지 않았다 (패킷 · Timer2 · 이벤트는 커널 개체라 사진에 칸이 없다).

---

### 이번 글의 새 말 셋 — 먼저 한 줄씩

이 글 내내 나오는 말이라, 처음 나오는 이 자리에서 푼다. 이미 쓴 비유 (신호등 · 번호표 · 우편함) 에 그대로 붙인다.

- **기다림 블록 (`KWAIT_BLOCK`, `0x30` 바이트)**: "누가, 무엇을 기다린다" 를 적어서 기다리는 대상의 줄에 거는 작은 표.
  - 비유: 신호등 앞에 거는 **"나 여기서 기다려요" 번호표**. 이미 [run13 해설 2.8](../02_sprint/run13-resume-thread-해설.md#28-깨우는-방식--신호등-초록--어깨-두드리기-정적) 과 [run77 해설 2.3](run77-thread-wake-해설.md#23-무대--이번에-쓰는-칸과-새-말) 에서 쓴 그 번호표다.
  - 칸: `WaitListEntry` (줄에 꿰는 고리) `+0x0` · `WaitType` (번호표 종류) `+0x10` · `BlockState` (번호표 상태) `+0x11` · `Thread` (누구 표인가) `+0x18` · `Object` (무엇을 기다리나) `+0x20` (run77).
- **IOCP 줄**: IO 완료 포트 (IOCP) 안에 쌓이는 **항목 줄**. 포트의 `EntryListHead` 꼬리에 항목을 달고, 항목 수는 포트의 `SignalState` 로 센다 `[정적]` (`KiInsertQueueInternal`, 2.9).
  - 비유: IO 완료 포트 = 일꾼 사무실 앞 **우편함** ([run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품)). IOCP 줄 = 그 우편함 안에 **차례로 쌓인 봉투 줄**. 일꾼은 맨 앞 봉투부터 꺼내 간다.
- **기다림 완료 패킷 (wait completion packet)**: "이 대상이 울리면, 내 IO 완료 포트 줄에 이 쪽지 넷을 넣어 줘" 라고 커널에 맡겨 두는 커널 개체.
  - run62 해설에서는 "대기 패킷" 으로 부르고 `[추측: 이름]` 으로 "벨에 매단 줄" 이라 했다. 이번에 그 **안쪽** 을 처음 봤다.
  - 비유: **봉투 달린 번호표**. 신호등 줄에 거는 번호표인데, "누구 표인가" 칸에 직원 이름 대신 **우편함 주소** 가 적혀 있고, 뒤에 쪽지 넷이 든 **봉투** 가 붙어 있다 (2.3).

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 로그가 아니다.** 요청서의 "막힌 자리" 는 PE_emulator 가 흉내 낸 실행의 로그다 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`. 이 글의 결론은 전부 코드를 읽은 것 `[정적]` 이다.
2. **패킷 칸 이름은 "우리 이름" 이다.** PDB 에 패킷 객체 타입이 **없다** (`pdb_struct.py` 로 찾아 0개). 그래서 칸의 **오프셋 · 쓰임은 `[정적]`**, **이름은 `[추측: 이름]`** 이다 (2.3 표).
3. **비유가 둘이다.**
   - flow.md 3장 새 절은 **택배** 비유다: 택배함 (대상) · 우편 봉투 (패킷) · 우체통 (IO 완료 포트) · 택배 기사 (`KeSetEvent` 등).
   - 이 글은 run13 · run77 · run62 해설을 이어서 **신호등 (대상) · 봉투 달린 번호표 (패킷) · 우편함 (포트) · 신호등 담당 (`KeSetEvent` 등)** 으로 부른다. 그림만 다르고 사실은 같다.
4. **"맡김 6번은 모두 `TpSetWaitEx` 에서" 가 아니다.** 요청서는 그렇게 적었지만 run 문서가 고쳤다 — `TpSetWaitEx` 를 거치는 건 2번, 나머지 4번은 타이머 큐 만들기가 직접 부른다 (2.15).
5. **"`AlreadySignaled` 를 틀린 주소에 쓰면 그래도 status 0" 은 표시가 한 단계 낮다** — `[정적 — 결과서 바이트 해석]` (flow-writer 가 바이트를 다시 안 풀었다, 2.6).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원** → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- 이벤트 = **신호등** (초록 1 / 빨강 0, 칸 `SignalState`) → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 자동 재설정 (`Type 1`, 한 명 통과하면 바로 빨강) / 수동 재설정 (`Type 0`) 이벤트, 기다릴 수 있는 물건의 **머리** (`_DISPATCHER_HEADER` — `SignalState` `+4`, 번호표 줄 머리 `WaitListHead` `+8`), 어깨 두드리기 (`KiTryUnwaitThread`), `KiExitDispatcher` → [run77 해설](run77-thread-wake-해설.md) 2.3 · 2.9 · 2.10
- 타이머 = **알람 시계**, 알림형 (`Type 8`, 매단 표 모두) / 동기형 (`Type 9`, 하나만 깨우고 0) → [run78 해설 2.3](run78-thread-timer-해설.md#23-무대--이번에-쓰는-칸과-새-말)
- 세마포어 = 횟수를 세는 신호등 → [run53 해설](run53-ethread-create-해설.md)
- 스레드 풀 부품 — 일꾼 사무실 (기본 풀) · 파견소 (일꾼 공장) · **우편함** (IO 완료 포트) · 알람 시계 (`NtCreateTimer2`) · 대기 패킷 → [run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품), 로더 전용 사무실 → [run69 해설](run69-loader-pool-해설.md)
- 락 = **"작성 중" 자물쇠**, IRQL = "방해 금지" 표지판 → [run13 해설 2.6](../02_sprint/run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- `STATUS_PENDING` (`0x103`) → [run92 해설 2.7](run92-bcrypt-ksecdd-해설.md)
- 예외 보호 구역 · **착지점** → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)
- "최소 조건 표" 라는 꼴 → [run85 해설](run85-user32-init-해설.md) 2.6
- 팀원 몸 = PE_emulator 팀원이 짠 syscall 흉내 코드 (요청서가 적은 모양 기준. 우리는 그 코드를 직접 안 읽었다)

### 한 줄로 먼저 — 무엇을 얻었나

> **패킷의 앞 `0x30` 바이트가 그대로 번호표 (`KWAIT_BLOCK`) 다. 안 울린 대상에 맡기면 그 번호표를 대상 줄 꼬리에 걸고 "성공 (0), 아직 안 울림 (0)" 을 돌려준다. 나중에 대상이 울리면 신호등 담당이 직원을 깨우는 대신 번호표째 우편함 줄에 넣는다.** `[정적]`

| 요청 33 질문 | 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 안 울림 status · `AlreadySignaled` · 거는 꼴 · 네 값 · 재맡기 | **0 / 0**. 패킷 = 기다림 블록 (`WaitType 2` · `BlockState 4` · `Thread` 칸 = 포트). 네 값 그대로. 재맡기 `0xC00000EF` `[정적]` | 2.4 ~ 2.6, 2.12 |
| 2 배달 함수 · 순서 · 신호 · 한 번 쓰기 · 넣는 함수 | 신호 함수가 직접 → `KiInsertQueueInternal`. 신호 1 씀. 한 번 쓰기. 패킷 = 줄 항목 `[정적]` | 2.7 ~ 2.10 |
| 3 취소 갈래별 status | 안 울림 0 · 줄에 듦 TRUE 0 / FALSE `0x103` · 안 맡김 `0xC0000120` `[정적]` | 2.13 |
| 4 `IoRemoveIoCompletion` 빈 줄 · 패킷 갈래 | 시간 0 → `0x102` + 0개. 항목 `+0x10` 바이트 2 = 패킷 `[정적]` | 2.11 |
| 5 최소 조건 표 | run 문서 "최소 조건 표" | 2.1 · 2.16 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간은 **두 군데에 걸쳐 있다**.

- 하나는 test.exe 초기화 안. 스레드 풀을 차릴 때 ntdll 이 `NtAssociateWaitCompletionPacket` 을 **6번** 부른다 (run62 · run69). 이번은 그 syscall 의 **커널 안쪽** 이다.
- 다른 하나는 흐름도 밖 공용 길. 대상이 울릴 때 배달하는 일은 `KeSetEvent` · 타이머 만기 같은 **모든 프로세스가 되풀이하는 길** 안에서 일어난다 (run77 · run78 절).

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ (2.4) 기본 스레드 풀 · WNF · ETW (run62)
 │         │     맡김 4번: 타이머 큐 둘 + WNF + ETW Once
 │         │       └─ syscall NtAssociateWaitCompletionPacket ──▶ 커널 본문   ← 이번 (run93)
 │         ├─ 로더 전용 풀 (run69)
 │         │     맡김 2번: 타이머 큐 둘
 │         │       └─ syscall NtAssociateWaitCompletionPacket ──▶ 커널 본문   ← 이번 (run93)
 │         └─ … DLL 들이기 · 전원 켜기
 └─ … → NtContinue (은퇴) → exe 진입점 → main

(흐름도 밖, 공용 길)  KeSetEvent · KiTimerWaitTest · KiExpireTimer2 · KeReleaseSemaphoreEx
                     번호표 줄을 돌다 "패킷 번호표" 를 만나면 → IOCP 줄로                ← 이번 (run93)
```

### 왜 이 질문이 지금 왔나

- PE_emulator 요청 33 이다. 병목 38개 (Go 런타임 IOCP 31 · `NtSetTimer2` 뒤 끝 7) 가 걸려 있다.
- 막힌 자리 (요청서) `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`:
  - Go 가 타이머를 걸고 (`NtSetTimer2`) → 패킷을 맡기면 (`NtAssociateWaitCompletionPacket`) 엔진이 기본 대답 `wait_completion_async_unsupported` → `NtRemoveIoCompletionEx` 에 `wait_would_block` → Go 가 글 찍고 끝.
  - 즉 엔진이 **"안 울린 대상에 맡기기"** 를 모른다. 그다음 **"나중에 울리면 배달"** 도 공사 중이다.

### 무엇이 궁금했나

요청서 질문 다섯.

1. **맡기기 — 대상이 아직 안 울렸을 때**: 돌려주는 status (0 인가, `STATUS_PENDING` 인가), `AlreadySignaled` 칸에 무엇을 쓰나, 무엇을 어떻게 걸어 두나, 네 값 (Key · Apc · IoStatus · Information) 이 인자 그대로인가, 이미 맡겨진 패킷을 또 맡기면.
2. **배달**: 대상이 울릴 때 누가 어떤 함수로 IOCP 줄에 넣나, 대상 신호를 쓰나, 한 번 쓰면 다시 맡겨야 하나.
3. **취소**: 갈래별 status (안 울림 · 줄에 듦 × `RemoveSignaledPacket` TRUE/FALSE · 안 맡김).
4. (작게) **꺼내기**: 빈 줄에서 시간 0 이면 무엇을 돌려주나, 패킷 항목을 따로 다루나.
5. **최소 조건 표**.

### 바퀴 전에 알던 것

- `[정적]` (run62 · run69) test.exe 초기화에서 맡김이 6번 있다.
- `[정적]` (요청서 "이미 아는 것") 유저 쪽 `TpSetWaitEx` 는 맡기기 대답의 **부호만** 보고 (`+0x16d`), "이미 울림" 바이트를 읽는다 (`+0x2e5`).
- `[추측: 이름]` (run62 해설) 대기 패킷 = "벨이 울리면 우편함에 쪽지를 떨어뜨리는 줄".
- `[정적]` (run77 해설 2.3, 결과서 읽기) 번호표 `WaitType` 숫자 중 **2 = 큐 (완료 포트) 알림** 이 있다. 이름은 `[추측]`. 그 갈래 안을 따라간 적은 없다.
- `[추측]` (팀원 카드, internals 근거 없음) 이미 울린 대상이면 그 자리에서 IOCP 줄에 넣는다.
- 커널 함수 주소만 있었다 (`_index.tsv`). 커널 본문 asm 은 우리 repo 에 없었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 맡김 → 배달 → 꺼냄 / 취소

패킷 하나가 겪는 일을 **자리 셋** 으로 나눠 그리면 이렇다. 자리 이름 (가 · 나 · 다) 은 이 글이 붙인 것이고, 화살표마다 근거는 run 문서 줄이다 `[정적]`.

```
            ┌──────────────────────────────────────────────────────────────┐
            │ (가) 안 맡김       연결 표시 +0x68 = 0 · 포트 칸 +0x58 = 0        │
            └──────────────────────────────────────────────────────────────┘
               │ 맡김 (NtAssociateWaitCompletionPacket) → status 0
               │
     대상이 ───┼─── 안 울림 ─▶ AlreadySignaled = 0
     이미 울림 │                 │
     ↓         │                 ▼
  Already-     │   ┌──────────────────────────────────────────────────────┐
  Signaled = 1 │   │ (나) 대상 번호표 줄에 걸림   BlockState 4 · +0x68 = 1    │
     │         │   └──────────────────────────────────────────────────────┘
     │         │        │ 대상이 울림 (배달)               │ 취소 → status 0
     │         │        │ KeSetEvent · KiTimerWaitTest ·   │ (줄에서 떼고 +0x68 = 0)
     │         │        │ KiExpireTimer2 · KeReleaseSema…  └──────────▶ (가)
     │         │        │ → BlockState 5
     ▼         ▼        ▼
   KiInsertQueueInternal(포트, 패킷)  ─── 기다리는 일꾼이 있으면 그 손에 바로
            │
            ▼
   ┌──────────────────────────────────────────────────────────────────────┐
   │ (다) IOCP 줄에 듦     BlockState 5 · +0x68 = 1 (아직 켜진 채)            │
   └──────────────────────────────────────────────────────────────────────┘
        │ 꺼냄 (IoRemoveIoCompletion)         │ 취소 TRUE → 0 (줄에서 빼고 +0x68 = 0)
        │ 네 값 출력 · +0x68 = 0 · +0x58 = 0   │ 취소 FALSE → 0x103 (그대로 줄에)
        │ · 참조 셋 풀기                        │
        └──────────▶ (가)                     └──────────▶ (가) / (다)

   (가) 에서 취소           → 0xC0000120
   (나)·(다) 에서 또 맡김    → 0xC00000EF
```

같은 내용을 표로 (run 문서 "최소 조건 표" 에서 줄만 골랐다):

| 무엇을 하나 | 지금 자리 | status | 패킷 · 대상에 생기는 일 | 근거 |
|---|---|---|---|---|
| 맡김 — 이미 울림 | (가) | 0 · `AlreadySignaled` **1** | 그 자리에서 IOCP 줄로 (→ 다). 대상 신호를 씀 | `KeRegisterObjectNotification +0x4a ~ +0xa8` |
| 맡김 — 안 울림 | (가) | 0 · `AlreadySignaled` **0** | 네 값 저장 · 대상 줄 꼬리에 걸림 (→ 나) | `KeRegisterObjectNotification +0xac ~ +0xde` |
| 배달 (대상이 울림) | (나) | (신호 쪽 반환 — 패킷과 무관) | 줄에서 떨어져 IOCP 줄로 (→ 다). 대상 신호를 씀 | 배달 길 표 (2.7) |
| 꺼냄 | (다) | 0 (항목 1) | 네 값 그대로 출력 · 연결 표시 끄기 · 참조 셋 풀기 (→ 가) | `IoRemoveIoCompletion +0x2e3 ~ +0x43d` |
| 취소 | (나) | **0** | 줄에서 떼기 · 연결 끄기 · 네 값 버림 (→ 가) | `IopCancel +0x35 ~ +0x90` |
| 취소 TRUE | (다) | **0** | IOCP 줄에서 빼기 · 연결 끄기 (→ 가) | `IopCancel +0xb9`, `KeRemoveQueueEntry +0x3b` |
| 취소 FALSE | (다) | **`0x103`** | 아무것도 안 바꿈 (다) | `NtCancel +0x14e ~ +0x158` |
| 취소 | (가) | **`0xC0000120`** | — | `NtCancel +0x11c` / `+0x132` |
| 또 맡김 | (나) · (다) | **`0xC00000EF`** | 안 씀 | `NtAssoc +0x16c → +0x24c` |
| 빈 줄에서 꺼냄, 시간 0 | — | **`0x102`** (항목 0) | — | `IoRemoveIoCompletion +0xe4 ~ +0x109`, `KeRemoveQueueEx +0x3d3` |

- 비유로 한 줄: 봉투 달린 번호표를 신호등에 걸어 둔다 (나). 초록이 되면 신호등 담당이 번호표째 우편함에 넣는다 (다). 일꾼이 우편함에서 꺼내 가면 번호표는 빈손으로 돌아온다 (가). 다시 쓰려면 **다시 걸어야** 한다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 관계" 트리에서 세 줄을 골라 "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 줄 1: `├─ (4) +0x1d0  KeRegisterObjectNotification(헤더, 포트, 패킷)` (`NtAssociateWaitCompletionPacket` 아래)

- **누가**: `NtAssociateWaitCompletionPacket` (맡기기 syscall 의 커널 본문).
- **어디서**: 자기 시작에서 `+0x1d0` 바이트 (ret `+0x1d5`).
- **누구를**: `KeRegisterObjectNotification` (245B). 인자 셋 = 대상의 기다림 머리 · IO 완료 포트 · 패킷.
- **무엇을 위해**: 패킷을 **번호표 꼴로 다듬고**, 대상이 이미 울렸는지 보고, 갈래를 고른다. 이미 울렸으면 곧장 IOCP 줄로 (`AL = 1`), 아니면 대상 줄 꼬리에 걸기 (`AL = 0`) `[정적]`.
- 이 줄이 이번 바퀴의 **한가운데** 다. 질문 1 의 답 (0 / 0) 이 여기서 정해진다.

#### 줄 2: `KeSetEvent  +0x54e (자동, 뒤 SUB)` (`[나중에 대상이 울림]` 아래)

- **누가**: `KeSetEvent` (신호등을 초록으로 켜는 함수, run77 의 그 신호등 담당).
- **어디서**: `+0x54e` (ret `+0x553`). 자동 재설정 이벤트의 번호표 줄을 돌다가, 번호표 종류 (`+0x10`) 가 **2** 인 걸 만난 갈래다 (`+0x53b CMP AL,2`).
- **누구를**: `KiInsertQueueInternal(포트, 패킷)`.
- **무엇을 위해**: 직원을 깨우는 대신 **패킷째 IOCP 줄에 넣으려고**. 그리고 바로 뒤 `+0x553 SUB [SignalState],1` — 신호등을 다시 빨강 (1 → 0) 으로 돌린다 `[정적]`.
- 이 줄 하나로 질문 2 의 셋이 한꺼번에 보인다: 누가 넣나 (신호 함수가 직접), 무엇으로 넣나 (`KiInsertQueueInternal`), 신호를 쓰나 (쓴다).

#### 줄 3: `│  +0x22d  *AlreadySignaled = AL` (`NtAssociateWaitCompletionPacket` 아래)

- **누가 / 어디서**: `NtAssociateWaitCompletionPacket +0x22d MOV [RDX],SIL`.
- **무엇을**: 줄 1 이 돌려준 `AL` (`+0x1d5 MOV SIL,AL` 로 옮겨 둔 것) 을 8번째 인자가 가리키는 바이트에 쓴다 `[정적]`.
- **언제**: 연결을 다 끝내고 패킷 자물쇠를 푼 **뒤** (`+0x1e0 KeReleaseSpinLock` 다음).
- 그래서 "안 울렸으면 `AlreadySignaled` 를 안 쓰나?" 의 답은 **"쓴다, 0 을"** 이다.

### 2.3 무대 — 패킷 안의 칸

뒤에서 오프셋이 나오면 이 표로 돌아오면 된다. 오프셋 · 쓰임은 `[정적]`, 이름은 우리 이름 `[추측: 이름]` (run 문서 "패킷 칸 표").

| 오프셋 | 내용 | 비유 |
|---|---|---|
| `+0x00 ~ +0x2f` | **`KWAIT_BLOCK` 그대로** — `WaitListEntry` `+0x0` · `WaitType` `+0x10` = **2** · `BlockState` `+0x11` = **4** · `Thread` 칸 `+0x18` = **IO 완료 포트** · `Object` `+0x20` = 대상 머리 | 번호표 몸. "누구 표" 칸에 직원 대신 **우편함 주소** |
| `+0x30` | KeyContext (인자 4) | 봉투 속 쪽지 ① |
| `+0x38` | ApcContext (인자 5) | 쪽지 ② |
| `+0x40` | IoStatusInformation (인자 7) | 쪽지 ④ |
| `+0x48` | IoStatus (인자 6, **4바이트만**) | 쪽지 ③ |
| `+0x50` | 대상 객체 (참조 1 쥠) | 어느 신호등에 걸었나 |
| `+0x58` | IO 완료 포트 객체 (참조 1 쥠) | 어느 우편함으로 가나 |
| `+0x60` | 패킷 스핀락 | 번호표 **"작성 중" 자물쇠** |
| `+0x68` | BOOLEAN **연결됨** | "지금 어딘가 걸려 있음" 딱지 |
| `+0x69` | BOOLEAN **닫힘** (맡기기 거부) | "폐기됨" 딱지 |

- 쪽지 순서가 오프셋 순서와 다르다는 데 주의 — 인자 6 (IoStatus) 이 `+0x48`, 인자 7 (Information) 이 `+0x40` 이다 `[정적]` (`+0x1a9` · `+0x1b5`).
- `WaitKey` (`+0x12`) · `SpareLong` · `SparePtr` 칸은 맡기기 때 **안 건드린다** `[정적]`. 처음 값이 무엇인지는 모른다 (→ Q176 ①).
- `BlockState` 숫자 뜻 (코드로 읽은 뜻): **4 = 걸려 있음**, **5 = 울려서 넘김 / 떼어짐**. run77 은 4 를 `WaitBlockActive` 로 적었다. 칸 타입이 바이트라 그 이름과의 연결은 `[추측: 이름]` (run77 그대로).

#### 왜 "앞 `0x30` = 번호표" 라고 말할 수 있나 — 단계별로

1. `KeRegisterObjectNotification +0x17` 이 `MOV word [R8+0x10],0x402` 를 한다 (`R8` = 패킷). 한 번에 두 바이트 — `+0x10` 에 2, `+0x11` 에 4 `[정적]`.
2. `+0x1e` 가 패킷 `+0x18` 에 포트를, `+0x28` 이 `+0x20` 에 대상 머리를 쓴다 `[정적]`.
3. 이 오프셋들 (`+0x10` · `+0x11` · `+0x18` · `+0x20`) 은 run77 에서 연 번호표 칸 (`WaitType` · `BlockState` · `Thread` · `Object`) 과 **딱 겹친다**.
4. 그리고 안 울렸을 때 걸리는 고리가 패킷 `+0x0` (`WaitListEntry`) 이다 (`+0xac ~ +0xb6`).
5. 그래서 → **패킷 자체가 번호표 노릇을 한다.** 새 번호표를 따로 만들지 않는다 (결과서 "먼저 보는 그림").

- 비유: 직원 번호표는 직원 업무 카드에 붙어 있는 네 장 중 하나다 (run77). 패킷은 **번호표가 맨 앞 장으로 붙은 봉투** 다. 봉투를 걸면 번호표가 걸린다.

### 2.4 맡기기 ① — 안 울린 대상: "걸고 성공" (질문 1)

run 문서 "맡기기 차례 표" 를 한 걸음씩 (인자 1 패킷 · 2 포트 · 3 대상 · 4 ~ 7 네 값 · 8 `AlreadySignaled`) `[정적]`.

1. **`+0x2c ~ +0x3f`** — 부른 쪽이 유저인지 커널인지 (`PreviousMode`, `KTHREAD +0x232`) 를 기억해 둔다.
2. **손잡이 셋을 물건으로 바꾼다** (`ObReferenceObjectByHandle` × 3):
   - `+0x62` (ret `+0x67`) 패킷 — 접근 1, 형 `IopWaitCompletionPacketObjectType`
   - `+0xdc` (ret `+0xe1`) 포트 — 접근 2, 형 `IoCompletionObjectType`
   - `+0x115` (ret `+0x11a`) 대상 — 접근 `0x100000`, 형 없음 (아무 형이나)
   - 하나라도 실패하면 그 status 로 끝.
3. **`+0x130` (ret `+0x135`) `ObGetAssociatedWaitObject`** — 대상의 "기다릴 수 있는 머리" 를 찾는다.
   - 못 찾음 (NULL) · 뮤턴트 (형 2) · 큐 (형 4) 면 → `+0x242` **`0xC00000F1`**.
   - 비유: 창고 열쇠 (뮤턴트) 나 줄 그 자체 (큐) 에는 봉투 번호표를 못 건다.
4. **`+0x160` (ret `+0x165`)** — 패킷 자물쇠 (`+0x60`) 를 잠근다.
5. **`+0x16c ~ +0x171`** — 연결됨 딱지 (`+0x68`) 가 이미 1? → `+0x24c` **`0xC00000EF`**.
6. **`+0x17c ~ +0x180`** — 폐기됨 딱지 (`+0x69`) 가 1? → `+0x207` **`0xC000A003`** (이름은 모름).
7. **`+0x186 ~ +0x1c7`** — 연결됨 딱지 = 1, 쪽지 넷 · 대상 · 포트를 패킷에 적는다.
8. **`+0x1d0` (ret `+0x1d5`) `KeRegisterObjectNotification(머리, 포트, 패킷)`** — 아래.
9. **`+0x1e0` (ret `+0x1e5`)** — 패킷 자물쇠를 푼다.
10. **`+0x22d`** — 8번째 인자가 NULL 이 아니면 `AlreadySignaled` 에 `AL` 을 쓴다 (2.6).
11. **`+0x1fa ~ +0x202` → `+0x72` → `+0x96 ~ +0xad`** — `EAX = EDI` (0) → **status 0**.

#### `KeRegisterObjectNotification` 안 — 안 울림 갈래 `[정적]`

1. `+0x17` · `+0x1e` · `+0x28` — 번호표 꼴 다듬기 (`WaitType 2` · `BlockState 4` · `Thread` 칸 = 포트 · `Object` = 머리).
2. `+0x2c ~ +0x45` (ret `+0x4a`) — IRQL 2 로 올리고 **대상 자물쇠** (`KiAcquireKobjectLockSafe`).
3. `+0x4a CMP [머리+4],0` · `+0x4e JG +0x6e` — `SignalState > 0` 이면 "이미 울림" 갈래로 (2.5). 아니면 아래.
4. `+0x50 ~ +0x5b` — 신호등 번호표 줄 꼬리가 깨지지 않았나 검사 (깨졌으면 `INT 0x29`).
5. **`+0xac ~ +0xb6`** — **패킷 `WaitListEntry` 를 머리 `+8` (`WaitListHead`) 꼬리에 꿴다.**
6. `+0xba` 자물쇠 풀기 → IRQL 되돌림 → **`+0xde XOR AL,AL`** (AL = 0).

- 비유: 직원 번호표를 거는 자리 (신호등 번호표 줄 맨 뒤) 에 **봉투 달린 번호표** 를 똑같이 건다. 신호등 담당 눈에는 번호표 하나가 늘었을 뿐이다. 다른 점은 "누구 표" 칸에 직원 대신 우편함이 적혀 있다는 것 하나다.

#### 쪽지 넷은 가공 없이 그대로 `[정적]`

| 들어올 때 | 패킷에 적는 줄 | 패킷 칸 | 꺼낼 때 나가는 칸 (출력 항목 `0x20`B) |
|---|---|---|---|
| 인자 4 Key | `+0x192` | `+0x30` | `+0` |
| 인자 5 Apc | `+0x19e` | `+0x38` | `+8` |
| 인자 6 IoStatus | `+0x1a9` (4바이트) | `+0x48` | `+0x10` (4B) |
| 인자 7 Information | `+0x1b5` | `+0x40` | `+0x18` |

- 꺼낼 때 읽는 줄은 `IoRemoveIoCompletion +0x2e3 ~ +0x2fb`, 출력에 쓰는 줄은 `+0x204` · `+0x212` (2.11).
- 그래서 → 엔진은 **맡길 때 넷을 저장해 둬야** 나중에 배달할 수 있다. 팀원 몸의 "쪽지 넷은 기억하지 않음" 은 여기서 틀린다 (2.16).

#### 참조 셋은 연결이 풀릴 때까지 쥔다 `[정적]`

- 성공 길 `+0x1fa ~ +0x200` 이 `R12` · `R14` · `ESI` 를 0 으로 지운다. 그래서 끝의 "참조 풀기" (`+0x7b ~ +0x90`) 를 **건너뛴다**.
- 즉 패킷 · 포트 · 대상 참조 셋을 **쥔 채로** 돌아간다.
- 푸는 곳은 둘뿐: 취소 (`IopCancel +0x77 ~ +0x8b`) · 꺼내기 (`IoRemoveIoCompletion +0x3bc` · `+0x403` · `+0x43d`).
- 비유: 봉투 번호표가 걸려 있는 동안은 신호등도 우편함도 **철거 못 하게** 붙잡아 둔다.

### 2.5 맡기기 ② — 이미 울린 대상: 그 자리에서 IOCP 줄로 `[정적]`

`KeRegisterObjectNotification +0x4a` 에서 `SignalState > 0` 이면:

1. **`+0x71` (ret `+0x76`) `KiWaitSatisfyOther(머리)`** — 대상 신호를 "한 명 통과" 만큼 쓴다 (2.10).
2. **`+0x79`** — `BlockState = 5`.
3. **`+0x80` (ret `+0x85`) `KiInsertQueueInternal(포트, 패킷)`** — IOCP 줄로 (2.9).
4. `+0x85` 대상 자물쇠 풀기 → **`+0xa3` (ret `+0xa8`) `KiExitDispatcher`** → **`+0xa8 MOV AL,1`**.

- 그래서 status 는 똑같이 0, `AlreadySignaled` 는 **1** 이다.
- 이 갈래는 팀원 몸이 이미 하는 꼴과 **같다** (2.16 표 "맞음").
- 비유: 신호등이 이미 초록이면 번호표를 걸 필요가 없다. 담당이 그 자리에서 봉투를 우편함에 넣고, 개찰구 신호등이면 빨강으로 돌린다.

### 2.6 왜 `STATUS_PENDING` 이 아닌가, 그리고 `AlreadySignaled` 를 쓰는 때 — 단계별로

요청서가 가장 궁금해한 곳이다. "아직 안 끝났으니 `0x103` (PENDING) 이겠지" 라는 짐작이 자연스럽기 때문이다.

1. 맡기기 본문의 성공 status 는 레지스터 `EDI` 에 있다.
2. `EDI` 는 셋째 손잡이 바꾸기 (`+0x115`, ret `+0x11a`) 의 결과 0 이 그대로 남은 것이다 (`[RSP+0x34]` 에도 적어 둠, `+0x121`).
3. 안 울림 · 이미 울림 **둘 다** `+0x1fa ~ +0x202` 로 나가서, `+0x96 MOV EAX,EDI` → `+0xad RET`.
4. 그 사이에 `EDI` 를 `0x103` 으로 바꾸는 줄이 없다.
5. 그래서 → **안 울려도 status 0** `[정적]`. "안 울렸다" 는 status 가 아니라 **`AlreadySignaled` 바이트 0** 으로 알린다.

- 비유: 우편함 직원에게 "봉투 걸어 뒀어요 (성공)" 라고만 답하고, "벌써 초록이라 넣었어요?" 는 따로 **체크 칸** 하나로 알린다.

#### `AlreadySignaled` 쓰기의 순서 `[정적]`

- `+0x1ed ~ +0x1f8` → `+0x211 ~ +0x22d`: 8번째 인자가 NULL 이 아니면,
  - 부른 쪽이 유저면 그 주소를 1바이트 읽고-쓰기로 확인한다 (`+0x229` · `+0x22b`). 위쪽 주소는 `0x7fffffff0000` 으로 눌러 일부러 틀리게 만든다.
  - 그다음 `+0x22d MOV [RDX],SIL`.
- 예외가 나면 → `$filt$0` (유저 모드일 때만 잡음) → 착지 `+0x232 ~ +0x241`.

#### 틀린 주소일 때 — 표시가 한 단계 낮은 곳

- 결과서는 원본 바이트로 착지 `+0x232 ~ +0x241` 을 읽었다: 저장해 둔 값 셋을 되돌리고 (`[RSP+0x34] → EDI` 등) `+0x1fa` 로 돌아간다.
- 그러면 → `EDI` 는 0 그대로 → **연결은 이미 걸렸고 status 0** (예외를 삼킴).
- 다만 `.asm` 은 이 구간을 "읽지 않은 틈" 으로만 보여서 flow-writer 가 바이트를 다시 풀지 않았다. 그래서 이 결론은 **`[정적 — 결과서 바이트 해석]`** 이다 (→ Q177 ⑧).
- 팀원 몸은 이때 접근 위반을 돌려준다고 한다 → "틀림 2 (작음)" (2.16).

### 2.7 배달 — 대상이 나중에 울릴 때, 신호 함수가 그 자리에서 (질문 2)

공통 꼴 `[정적]`:

- 신호 함수는 자기 번호표 줄을 돌며 **번호표 종류 (`+0x10`, `WaitType`)** 를 본다.
  - 1 = 직원 (하나라도 풀리면 깸) → 어깨 두드리기 (`KiTryUnwaitThread`)
  - 0 = 직원 (전부 풀려야 깸) → `KiTryUnwaitThread`
  - 4 = DPC
  - **2 = 패킷** → `BlockState = 5` → `KiInsertQueueInternal(번호표 +0x18 의 포트, 번호표)`
- 넣는 함수는 **`KiInsertQueueInternal` 하나** 다 (`KiInsertQueue` · `KeInsertQueueEx` 아님). **줄 항목 = 패킷 객체 자체** (복사 없음).

신호 내는 길 일곱 (run 문서 "배달 길 표"):

| 신호 내는 길 | 갈래 자리 | 넣는 call / ret | 신호 · 번호표 줄 |
|---|---|---|---|
| 이벤트 **자동** (형 1) | `KeSetEvent +0x31a` → `+0x53b` (`== 2`) → `+0x543` | `+0x54e` / `+0x553` | **`+0x553 SUB [SignalState],1`** (1 → 0) → **첫 번호표 하나로 끝** |
| 이벤트 **수동** (형 0) | `KeSetEvent +0x1b3/+0x1ba` → `+0x587` (`== 2`) → `+0x58f` | `+0x59b` / `+0x5a0` | 신호 그대로. 줄은 끝에 통째로 비움 (`+0x4fe ~ +0x506`) |
| 알람 (KTIMER) **동기** (형 9) | `KiTimerWaitTest +0x1e6` → `+0x2c7` → `+0x211 ~ +0x238` → `+0x266` (`== 2`) → `+0x26b` | `+0x276` / `+0x27b` | **`+0x27b SUB [SignalState],1`** → 0 이면 끝 |
| 알람 (KTIMER) **알림** (형 8) | `KiTimerWaitTest +0x201` → `+0x2d9` (`== 2`) → `+0x2de` | `+0x2e9` / `+0x2ee` | 신호 그대로. 줄 비움 `+0x341 ~ +0x345` |
| Timer2 **동기** (형 `0x19`) | `KiExpireTimer2 +0xca` → `+0xd2` → `+0x394` (`== 2`) → `+0x39d` | `+0x3a8` / `+0x3ad` | **`+0x3ad SUB [SignalState],1`** |
| Timer2 **알림** (형 `0x18`) | `KiExpireTimer2 +0xcc JNZ` → `+0x6f2` (`== 2`) → `+0x6f7` | `+0x702` / `+0x707` | 신호 그대로 |
| 세마포어 놓기 | `KeReleaseSemaphoreEx +0x233` (`== 2`) → `+0x23c` | `+0x248` / `+0x24d` | **`+0x24d SUB [count],1`** — 패킷 하나가 횟수 1 을 먹음 |

- 드라이버용 `KeReleaseSemaphore +0x22c` (ret `+0x231`) 도 같은 꼴이다.
- 입구 (결과서 그대로, flow-writer 가 다시 안 읽음): `NtSetEvent` → `NtSetEventEx +0xb9` → `KeSetEvent` · `NtReleaseSemaphore +0x94` → `KeReleaseSemaphoreEx` · `KiTimer2Expiration +0xd7` → `KiExpireTimer2` · `KiTimerExpiration` → `KiProcessExpiredTimerList` → `KiTimerWaitTest` (run78).
- **Timer2 는 KTIMER 길이 아니다** — `NtCreateTimer2` 로 만든 알람은 `KiTimerWaitTest` 가 아니라 `KiExpireTimer2` 의 같은 꼴 갈래로 간다 (flow.md run78 절 (run93) 줄).

- 비유: 신호등 담당이 초록을 켜고 번호표 줄을 훑는다. 직원 번호표면 어깨를 두드리고, **봉투 번호표** 면 표를 떼어 그 길로 **우편함에 넣는다**. 개찰구 신호등 (자동) 이면 봉투 하나 넣고 바로 빨강 — 뒤 번호표는 그대로 남는다.

#### run77 · run78 과 이어짐 — 정정은 없다

- run77 은 `KeSetEvent` 에서 "`WaitType 0` 블록만 `KiTryUnwaitThread`" 를, run78 은 `KiTimerWaitTest +0x32a KiTryUnwaitThread` 만 적었다.
- 이번에 **같은 함수 안 `WaitType 2` 갈래** (`KeSetEvent +0x53b` · `+0x587`, `KiTimerWaitTest +0x266` · `+0x2d9`) 가 더해진다.
- 두 run 과 어긋나는 줄은 없다 — **정정 없음** (run 문서 보탬 ③).

### 2.8 왜 패킷은 어깨 두드리기로 안 가나 — 단계별로

1. 어깨 두드리기 (`KiTryUnwaitThread` · `KiSatisfyThreadWait`) 는 번호표 `+0x18` 을 **직원 (스레드)** 으로 쓴다 (run77).
2. 패킷 번호표의 `+0x18` 에는 **포트** 가 들어 있다 (2.3).
3. 그대로 넘기면 포트를 직원으로 읽게 된다.
4. 그래서 신호 함수들이 **그 앞에서** `WaitType` 을 보고 갈라 둔다 (`== 2` 비교 자리, 2.7 표의 "갈래 자리").
5. 그래서 → **패킷은 `KiTryUnwaitThread` · `KiSatisfyThreadWait` 로 안 간다** `[정적]`.

- 비유: 어깨 두드리기 담당은 "누구 표" 칸에 적힌 사람에게 간다. 거기 우편함 주소가 적혀 있으면 갈 데가 없다. 그래서 신호등 담당이 미리 골라 낸다.

### 2.9 넣기 — `KiInsertQueueInternal(포트, 항목)`: 손에 바로, 아니면 줄 꼬리 `[정적]`

834B 함수에서 결과서 줄만.

1. **`+0x14 AND [항목],0`** — 항목 첫 칸 (`Flink`) 을 0 으로. 뜻은 **"줄에 안 달림"** 표시. 나중에 `KeRemoveQueueEntry +0x3b/+0x3f` 가 이 칸으로 "줄에 있나" 를 가른다 (2.13).
2. **`+0x60`** — 포트 자물쇠.
3. **`+0x74 ~ +0x8a`** — 우편함 앞에서 기다리는 일꾼이 있고, 지금 일하는 수 (`CurrentCount`) 가 정원 (`MaximumCount`) 보다 적으면 → **`+0x139` 그 일꾼 `+0xc8` 칸에 항목을 바로 넘긴다.** 이때 항목은 **줄에 안 달린다.**
4. 아니면 → `+0x175` 근처에서 항목 수 (`SignalState`) +1, **`+0x191 ~ +0x19b` 포트 `EntryListHead` 꼬리에** 단다 = IOCP 줄.
5. 줄이 비어 있었고 기다리는 일꾼이 있으면 → `+0x1af` (ret `+0x1b4`) `KiWakeOtherQueueWaiters`.

- 비유: 봉투를 우편함에 넣으러 갔는데 일꾼이 **우편함 앞에서 손을 내밀고** 서 있으면, 줄에 안 꽂고 **손에 바로 쥐여 준다** (run77 의 "물건을 손에 쥐여 주고 간다" 와 같은 꼴). 아무도 없으면 우편함 안 봉투 줄 맨 뒤에 꽂는다.
- 작은 주의: run 문서 보탬 ④ — `+0x175` 자체는 `MOV R8D,[RDI+4]` 다. "항목 수 +1" 은 결과서가 "그 근처에서 올린다" 고 적은 것이고, 올리는 명령 줄은 flow-writer 가 따로 안 찾았다. 결론은 결과서 그대로.

### 2.10 신호를 쓴다 — 직원을 깨울 때와 결과가 같다 `[정적]`

요청서 질문 "넣을 때 대상의 신호를 쓰나 (스레드 기다림처럼 '만족' 처리 하나)" 의 답.

- **쓴다.** 결과는 직원을 깨울 때와 같다.
- 다만 길이 조금 다르다:
  - 나중에 울릴 때 — `KiWaitSatisfy*` 를 **안 거치고**, 신호 함수가 넣은 직후 **직접** `SUB [SignalState],1` 한다 (`KeSetEvent +0x553` · `KiTimerWaitTest +0x27b` · `KiExpireTimer2 +0x3ad` · `KeReleaseSemaphoreEx +0x24d`).
  - 맡길 때 이미 울림 — `KiWaitSatisfyOther` (45B) 가 같은 일을 한다.

`KiWaitSatisfyOther(머리)` 가 형에 따라 하는 일:

| 대상 형 | 하는 일 | 자리 |
|---|---|---|
| `& 7 == 1` — 자동 재설정 이벤트 1 · 동기 알람 9 · Timer2 동기 `0x19` | `SignalState = 0` | `+0x1e` |
| `& 0x7f == 5` — 세마포어 | `SignalState--` | `+0x29` |
| 그 밖 — 수동 이벤트 · 알림 알람 · 프로세스 · 스레드 등 | 그대로 | `+0x1a` |

- 비유: 개찰구 신호등은 **직원이 지나가든 봉투가 지나가든** 한 번 통과면 빨강이다. 세마포어 (횟수 세는 신호등) 는 봉투 하나마다 횟수 1 을 깎는다.

### 2.11 꺼내기 — `IoRemoveIoCompletion`: 패킷 항목을 알아보는 법 (질문 4) `[정적]`

갈림 자리만.

1. **`+0x8d` (ret `+0x92`) `KeRemoveQueueEx`** — 줄에서 하나 꺼낸다. 비었고 기다려야 하면 그 안에서 잠든다 (`KeRemoveQueueEx +0x891/+0x896 KiCommitThreadWait`). 깨우는 쪽은 2.9 의 "손에 바로".
2. **빈 줄 + 시간 0**:
   - `KeRemoveQueueEx +0x356/+0x35a` → `+0x3d3` 값 **`0x102`** → `+0x3e9` 출력 첫 칸에.
   - (Alertable 아닌 길 `+0x786 ~ +0x78d` 도 `+0x3d3` 로, `+0xb36` 도 `0x102`.)
   - 유저 APC 가 와 있고 유저 모드 기다림이면 대신 `0xc0` (`+0x793 ~ +0x7a4`).
   - `IoRemoveIoCompletion` 은 첫 칸이 `0x80` · `0xc0` · `0x101` · `0x102` 면 "항목 아님" 으로 본다 (`+0xe4 ~ +0x109` → `+0x28c`) → 항목 수 0 (`+0xb7`), **status = 그 값**.
   - 그래서 빈 줄 · 시간 0 = **`0x102` (`STATUS_TIMEOUT`) + 항목 0개**.
3. **항목 가르기 `+0x10f MOVZX EAX,byte [항목+0x10]`** — 패킷이면 이 칸이 `WaitType` 이다:
   - 0 → IRP (`+0x11b` 항목 − `0xa8` → `IopCompleteRequest`)
   - **2 → 기다림 완료 패킷** (`+0x1c9/+0x1cb JZ +0x2e3`)
   - 그 밖 (1) → 작은 완료 패킷 (`IopFreeMiniCompletionPacket`)
4. **패킷 갈래**: 쪽지 넷 읽기 (`+0x2e3 ~ +0x2fb`) → 패킷 자물쇠 → **`+0x342 [+0x68] = 0` · `+0x346 [+0x58] = 0`** → 자물쇠 풀기 → **참조 셋 풀기** (대상 `+0x3bc` · 포트 `+0x403` · 패킷 `+0x43d`).
5. 출력 항목 하나 `0x20`B: `Key +0` · `Apc +8` · `Status +0x10` (4B) · `Information +0x18` (`+0x204` · `+0x212`).

- 비유: 일꾼이 우편함에서 봉투를 꺼내면, 봉투 맨 앞 장 (번호표 종류 칸) 을 보고 "아, 신호등에서 온 봉투구나" 를 안다. 쪽지 넷을 옮겨 적고, 번호표의 "걸려 있음" 딱지를 떼고, 붙잡아 두던 신호등 · 우편함을 놓아준다.

### 2.12 한 번 쓰면 끝 — 연결 표시 `+0x68` (질문 1 · 2) `[정적]`

1. 맡길 때 `+0x186` 에서 `+0x68` = 1.
2. 배달 (IOCP 줄에 들어감) 때 이 칸을 **아무도 안 끈다** — 줄에 들어간 뒤에도 1 이다.
3. 끄는 곳은 **딱 둘**: 취소 (`IopCancel +0x4b`) · 꺼내기 (`IoRemoveIoCompletion +0x342`).
4. 그 전에 또 맡기면 → 2.4 의 5번 걸음 (`+0x16c`) 에 걸려 **`0xC00000EF`**.
5. 그래서 → 패킷은 **한 번 배달되면, 꺼내거나 취소한 뒤 다시 맡겨야** 또 쓸 수 있다.

- 번호표 쪽에서 봐도 같다: 배달 때 `BlockState` 는 5 가 되고 번호표는 신호등 줄에서 **떨어진다**. 신호등이 또 울려도 이 봉투는 다시 안 간다.
- 비유: 봉투 번호표는 **일회용** 이다. 우편함에서 꺼낸 사람이 다시 신호등에 걸어야 다음 초록에 또 온다. flow.md 비유로는 "우체통에서 꺼낸 사람이 다시 붙여야 한다".
- PE_emulator 에게 중요한 이유: 엔진의 꺼내기가 **연결 표시를 안 끄면**, Go 가 같은 패킷을 다시 맡길 때 `0xC00000EF` 가 나올 것이다 `[추측: Go 가 패킷을 다시 쓰는지는 안 봄]` (run 문서 "PE_emulator 확인거리").

### 2.13 취소 — `NtCancelWaitCompletionPacket(패킷, RemoveSignaledPacket)` (질문 3) `[정적]`

#### 새 말 — `RemoveSignaledPacket`

- 취소 syscall 의 둘째 인자 (BOOLEAN). 이미 IOCP 줄에 든 패킷이면 **"줄에서도 빼라 (TRUE)" / "줄에 든 건 두라 (FALSE)"** 를 고른다.
- 비유: "신호등에 걸린 봉투는 떼 주세요. 이미 우편함에 들어간 것도 꺼내 버릴까요?" 에 대한 예 / 아니오.

#### 먼저 문 앞 검사 (`NtCancelWaitCompletionPacket`)

- `+0x57` (ret `+0x5c`) 손잡이 → 패킷 (접근 1).
- `+0x75` 패킷 자물쇠 → 포트 칸 (`+0x58`) 읽기 · 참조.
  - `+0x9e` 포트 칸이 0 → `+0xa1 JZ +0x11c` → **`0xC0000120`** (안 맡김).
- `+0xac` 포트 쪽 자물쇠 → `+0xb4` 패킷 자물쇠 다시.
  - `+0xc1` 연결됨 (`+0x68`) 이 0 → `+0x132` → **`0xC0000120`** (이미 꺼냄 등).
  - `+0xcb` 두 자물쇠 사이에 포트가 바뀜 → `+0x13c` → `0xC000A003`.
- **`+0xda` (ret `+0xdf`) `IopCancelWaitCompletionPacket(패킷, RemoveSignaledPacket, 이전 IRQL)`**.

#### `IopCancelWaitCompletionPacket` 안 — 두 번 시도

1. `+0x2a` 대상 머리 (`[+0x50]` → `ObGetAssociatedWaitObject`).
2. **`+0x35` (ret `+0x3a`) `KiDeregisterObjectWaitBlock(대상, 패킷)`** — 대상 자물쇠 안에서 `BlockState == 4` (= 아직 신호등에 걸림) 일 때만 줄에서 떼고 (`+0x42 ~ +0x61`) 5 로 바꿔 (`+0x65`) TRUE.
   - TRUE (`+0x3e`) → `+0x42` 정리 (아래).
3. 아니면 (이미 줄에 듦) — `RemoveSignaledPacket` ≠ 0 일 때만 (`+0xae` · `+0xb1 JZ +0xc2`) **`+0xb9` (ret `+0xbe`) `KeRemoveQueueEntry(포트, 패킷)`**.
   - `KeRemoveQueueEntry +0x3b/+0x3f`: 항목 첫 칸이 0 (줄에 안 달림, 2.9 의 `+0x14`) 이면 FALSE.
   - 아니면 `+0x41` 항목 수 −1 · `+0x5d ~ +0x63` 고리 풀기 → TRUE → `+0xc0 JNZ +0x42` 정리.
4. **정리 (`+0x42`)**: `+0x58` = 0 (`+0x42`) · `+0x68` = 0 (`+0x4b`) · 자물쇠 풀기 (`+0x4f`) · 참조 셋 풀기 (`+0x77 ~ +0x8b`) · TRUE (`+0x90`).
5. FALSE 로 돌아오면 → `NtCancel +0x14e ~ +0x158`: `[+0x68] ≠ 0` 이면 `CMOVNZ EDI,0x103` → **`0x103`**.

#### 갈래별 status (run 문서 "취소 표")

| 상황 | status | 한 일 |
|---|---|---|
| (나) 신호등에 걸림 (`BlockState 4`) | **0** (`RemoveSignaledPacket` 무관) | 줄에서 떼기 · 정리 · 네 값 버림. 대상 신호 안 건드림 |
| (다) 줄에 듦 + TRUE | **0** | IOCP 줄에서 빼기 + 정리. 네 값 버림 |
| (다) 줄에 듦 + TRUE 인데 줄에 안 달림 (일꾼 손에 바로 넘겨짐 등) | **`0x103`** | 아무것도 안 바꿈 |
| (다) 줄에 듦 + FALSE | **`0x103`** | 아무것도 안 바꿈 (나중에 꺼내기가 꺼냄) |
| (가) 안 맡김 (또는 이미 꺼냄) | **`0xC0000120`** | 참조만 풀고 끝 |
| 두 자물쇠 사이 포트가 바뀜 | `0xC000A003` (`+0x142`) | 자물쇠만 풀고 끝 |
| 손잡이 문제 | `ObReferenceObjectByHandle` 값 | — |

- 핵심 둘:
  - **`0xC0000120` 은 "안 맡김" 갈래 하나뿐** 이다. 신호등에 걸린 걸 취소하면 **0** 이다.
  - **`0x103` 은 "아직 우편함에 있어서 못 뺐다" 는 뜻** 이다.
- 비유: 걸린 봉투를 떼면 "뗐어요 (0)". 우편함에 들어간 걸 "빼지 마" 했거나 이미 일꾼 손에 있으면 "아직 거기 있어요 (`0x103`)". 걸린 적도 없으면 "그런 봉투 없어요 (`0xC0000120`)".

### 2.14 패킷 손잡이를 닫으면 — 조용한 취소 `[정적]` + `[추측]`

- `IopCloseWaitCompletionPacket` 이 `+0x42` 에서 폐기됨 딱지 (`+0x69`) = 1, 그리고 `+0xbe` (ret `+0xc3`) `IopCancelWaitCompletionPacket(…, TRUE)` `[정적]`.
- 그래서 신호등에 걸려 있든 우편함 줄에 있든, 걸린 패킷이 **조용히 취소된다**. 이후 맡기기는 `0xC000A003` (2.4 의 6번 걸음).
- 몸이 도는 조건 `+0x0 CMP R9,1` 의 뜻 ("그 프로세스의 마지막 손잡이") 과, 이 함수가 객체 형식의 **닫기 처리기** 로 등록됐는지는 `[추측]` (형식 등록은 범위 밖 → Q176 ⑤).

### 2.15 test.exe 초기화의 맡김 6번에 대입 (run 문서 보탬 ①)

#### 먼저 정정 — 6번 모두 `TpSetWaitEx` 가 아니다

- 요청서: "유저 쪽 `TpSetWaitEx` 가 `NtAssociateWaitCompletionPacket` 을 부르는 자리 (맡김 6번)".
- run 문서: `TpSetWaitEx` 를 거치는 건 **2번** (WNF · ETW) 이고, **4번은 타이머 큐 만들기 (`TppInitializeTimerSubQueue +0xa0`) 가 직접** 부른다. run62 표 6 · run69 표 7 ~ 12 와 같다.

#### 여섯을 하나씩 (run 문서 "대입" 표)

| # | 부르는 자리 | 대상 | 맡길 때 대상 상태 | 갈래 |
|---|---|---|---|---|
| 1 · 2 | 기본 풀 `TpAllocPoolInternal +0x38e` → `TppInitializeTimerQueue +0x2c` · `+0x44` → **`TppInitializeTimerSubQueue +0xa0/+0xa5`** | 바로 앞 `+0x4d NtCreateTimer2` 가 만든 Timer2 (`+0x74 MOV R8,[RBX]`, RBX = `sub+0x18`) `[정적]`. 포트 = `[풀+0x40]` (`+0x9c`) | 만들고 아직 안 켬 → 안 울림 `[추측: KeInitializeTimer2 안 읽음]` | 안 울림 `[추측]` |
| 3 | WNF `RtlpWnfRegisterTpNotification +0x92` → `TpSetWaitEx +0x168/+0x16d` | `+0x28 NtCreateEvent(0x1f0003, 종류 1, 처음 0)` `[정적]` | 만들 때 꺼짐. 다만 `+0x77 NtSetWnfProcessNotificationEvent` 로 **커널에 먼저 넘긴 뒤** 맡긴다 — 그 사이 커널이 켤 수 있음 `[추측]` | 안 울림일 것 `[추측]` |
| 4 | ETW Once `EtwpRegisterTpNotificationOnce +0x64` → `TpSetWaitEx +0x168/+0x16d` | `+0x32 NtCreateEvent(0x1f0003, 종류 1, 처음 0)` `[정적]` | 꺼짐. 커널에 넘기는 `NtTraceControl(0x1b)` 는 맡긴 **뒤** (`+0x8c`) `[정적: 순서]` | **안 울림** `[정적+추측: 그 사이 이 핸들을 쓰는 곳 없음]` |
| 5 · 6 | 로더 풀 `TpAllocPoolInternal +0x38e` → … → `TppInitializeTimerSubQueue +0xa0/+0xa5` | Timer2 (1 · 2 와 같은 함수) | 안 울림 `[추측]` | 안 울림 `[추측]` |

#### 왜 "모두 안 울림" 이라고 짐작하나 — 단계별로

1. 대상 여섯은 모두 **맡기기 바로 앞에서 막 만든 것** 이다: Timer2 넷 · 이벤트 둘 `[정적]`.
2. 이벤트 둘은 `NtCreateEvent` 의 "처음 상태" 인자가 0 이다 → 만들 때 **꺼짐** `[정적]`.
3. Timer2 넷은 만들고 **켜는 syscall (`NtSetTimer2` 류) 없이** 바로 맡긴다 (run69 해설 "켜지는 않는다").
4. 그래서 → 맡기는 순간 `SignalState > 0` 이 아닐 것 → 2.4 의 "안 울림" 갈래 `[추측]`.
5. 아직 확인 아님:
   - Timer2 가 만들 때 꺼진 상태인지는 `KeInitializeTimer2` 를 안 읽어서 모른다.
   - WNF 이벤트는 커널에 먼저 넘긴 뒤 맡겨서, 그 사이에 켜질 틈이 있다.
   - → 동적으로 `nt!NtAssociateWaitCompletionPacket+0x1d5` 에서 `al` 이 6번 다 0 인지 보면 닫힌다 (Q177 ⑥).

#### 그래서 초기화가 끝날 때 커널에는

- **패킷 6개가 대상 6개 (Timer2 4 · 동기 이벤트 2) 의 번호표 줄에 걸린 채** 남을 것이다 `[추측]`. 패킷마다 연결 표시 `+0x68` = 1, 참조 셋을 쥔 채.
- 은퇴 (P2) 까지 대상이 울렸는지는 **모른다** (예: 힙 GC 타이머가 타이머 큐 Timer2 를 켬 — run69 `RtlpHpGCTimerSchedule`).
- 유저 쪽은 대답을 거의 안 본다:
  - `TppInitializeTimerSubQueue` 는 맡기기 status 를 **안 본다** (sast/64 결과서).
  - `TpSetWaitEx` 는 부호만 (`+0x16d`) · "이미 울림" 바이트를 읽는다 (`+0x2e5`).
- 맡김 1 · 2 · 5 · 6 의 쪽지 넷 `[정적]`: Key = `sub+0x28` (`+0x89`), Apc = `풀+0x70` (`+0x81` · `+0x97`), IoStatus = 0 (`+0x92`), Information = 셋째 인자 바이트 (`+0x30` · `+0x8d`), `AlreadySignaled` = 지역 `[RSP+0x60]` (`+0x77` · `+0x7c`).

- 비유: 가게를 열 때 일꾼 사무실 둘을 차리면서, 알람 시계 넷과 신호등 둘에 **봉투 번호표를 하나씩** 걸어 둔다. 문을 여는 순간 (은퇴) 그 여섯 봉투는 아마 아직 **걸려만 있다**.

### 2.16 팀원 몸과 맞는 것 · 틀린 것 — 한눈에

run 문서 표를 판정별로 다시 묶었다. 진짜 쪽은 모두 `[정적]` (26100.9457) 이다. 팀원 몸은 결과서 · 요청서가 적은 모양 기준이다 (flow-writer 도 `../PE_emulator` 코드는 안 읽음).

plan.md 결정 기록의 셈: **맞음 7 · 틀림 4 · 빈 곳 넷**.

#### 맞음 (7) — 그대로 두면 된다

| 갈래 | 팀원 몸 = 진짜 | 근거 |
|---|---|---|
| 맡기기 — 손잡이 참조 순서 · 접근 | 패킷 1 → 포트 2 → 대상 `0x100000` | `+0x5d` · `+0xcf` · `+0x108` |
| 맡기기 — 뮤턴트 · 큐 · 기다릴 수 없는 대상 | `0xC00000F1` | `+0x13d` · `+0x14b` → `+0x242` |
| 맡기기 — 이미 연결된 패킷 | `0xC00000EF` | `+0x16c/+0x171` → `+0x24c` |
| 맡기기 — 이미 울림 | `AlreadySignaled` 1 · 신호 씀 · 바로 줄에 | `KeRegisterObjectNotification +0x4a ~ +0xa8` |
| 취소 — 안 맡김 (또는 이미 꺼냄) | `0xC0000120` | `NtCancel +0x11c` / `+0x132` |
| 취소 — 줄에 듦, TRUE | 0 | `IopCancel +0xb9 KeRemoveQueueEntry` → `+0x42` |
| 취소 — 줄에 듦, FALSE | `0x103` | `NtCancel +0x14e ~ +0x158` |

#### 틀림 (4) — 고쳐야 한다

| 갈래 | 지금 팀원 몸 | 진짜 | 메모 |
|---|---|---|---|
| 맡기기 — 안 울림 | 기본 대답 `wait_completion_async_unsupported` (코드엔 `TODO(VM)`) | **status 0 · `AlreadySignaled` 0 · 대상 번호표 줄에 패킷을 닮** | `TODO` 의 답은 **"0 / 0"** (2.4 · 2.6). **Go 가 막힌 바로 그 자리** |
| 맡기기 — 네 값 | "쪽지 넷은 기억하지 않음" | 패킷에 **저장** (`+0x192 ~ +0x1b5`) | 배달하려면 저장해야 함 (2.4) |
| 맡기기 — `AlreadySignaled` 쓰기 실패 (틀린 유저 주소) | 접근 위반을 돌려줌 | 연결은 이미 걸렸고 status 0 (예외를 삼킴) | **작음**. `[정적 — 결과서 바이트 해석]` 이라 동적 확인 후보 (2.6) |
| 꺼내기 — 빈 줄 · 시간 0 | `wait_would_block` | **`0x102`** (`STATUS_TIMEOUT`) + 항목 0개 | 기다림이 있으면 재우는 건 맞음 (2.11) |

#### 빈 곳 (4) — 새로 만들어야 한다

| 갈래 | 지금 팀원 몸 | 진짜 | 메모 |
|---|---|---|---|
| 배달 — 이벤트 · 알람 · Timer2 · 세마포어 | "공사 중" | 신호 함수가 `WaitType 2` 를 보고 `KiInsertQueueInternal(포트, 패킷)`, 신호 씀 | **빈 곳 전부** (2.7 ~ 2.10) |
| 꺼내기 — 패킷 항목 | (`nt_io_completion` 쪽, 요청서에 없음) | 연결 표시 끄기 (`+0x342`) · 포트 칸 0 (`+0x346`) · 참조 셋 풀기 | **해야 함** — 안 하면 다음 맡기기가 `0xC00000EF` (2.12) |
| 취소 — 안 울림 (번호표 줄에 있음) | 멈춤 (`wait_completion_waiting_state_unsupported`) | **0** — 줄에서 떼고 참조 셋 풀기 | **`0xC0000120` 을 주면 틀림** (2.13) |
| 맡기기 — 닫힌 패킷 (`+0x69`) | 멈춤 | `0xC000A003` (`+0x17c/+0x180` → `+0x207`) | 값 확정, 이름은 모름 (2.14) |

#### 그래서 엔진이 먼저 할 일 (run 문서 "PE_emulator 확인거리", 우리 대기열 아님)

1. `NtAssociateWaitCompletionPacket` 기본 대답을 **"0 + `AlreadySignaled` 0 + 대상에 걸어 둠"** 으로.
2. 걸어 둔 패킷을 대상이 울리는 syscall (`NtSetEvent` · `NtReleaseSemaphore` · Timer2 만기) 에서 IOCP 줄로 옮기기 — 그래야 Go 의 `NtRemoveIoCompletionEx` 가 깬다 (막힌 자리 `seq 2256 → 2260`).
3. `NtRemoveIoCompletionEx` 가 패킷 항목을 꺼낼 때 연결 표시를 끄는지.
4. Go 타이머 (`NtCreateTimer2`) 가 동기인지 알림인지에 따라 만기 뒤 신호가 꺼지는지가 갈린다 (`KiExpireTimer2 +0x3ad` / `+0x707`) → Q177 ⑦.

### 2.17 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것" 넷.

- ① (보탬) **test.exe 맡김 6번의 자리 · 대상** — 요청서의 "`TpSetWaitEx` 6번" 을 "2번 + `TppInitializeTimerSubQueue +0xa0` 4번" 으로 (2.15). 결론 (안 울림) 은 `[추측]`.
- ② (결과서 그대로, 표시만 낮춤) **`AlreadySignaled` 쓰기 예외의 착지** → `[정적 — 결과서 바이트 해석]` (2.6).
- ③ (보탬) **run77 · run78 과 이어짐** — 같은 함수 안 `WaitType 2` 갈래가 더해짐, 정정 없음 (2.7).
- ④ (보탬) `KiInsertQueueInternal +0x175` 는 `MOV R8D,[RDI+4]` — "항목 수 +1" 은 결과서 그대로 (2.9).
- 이름 하나: `IopCancelWaitCompletionPacket +0x35` 의 callee 는 `_calls.tsv` 에 `alias:KiDeregisterObjectWaitBlock+1` 로 찍혔다. 같은 주소의 이름은 C++ 꾸민 이름 `?KiDeregisterObjectWaitBlock@@YAEPEAXPEAU_KWAIT_BLOCK@@@Z` **하나뿐** 이라 (`pdb_aliases.py --where`), 이 글도 `KiDeregisterObjectWaitBlock` 으로 쓴다. run92 와 같은 꼴이다.

### 2.18 디버거 · 도구 메모

- 이번에 읽은 줄 안에서 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`.
- 다만 `KiInsertQueueInternal` · `KeSetEvent` 처럼 **모든 프로세스가 지나는 함수** 에 커널 멈춤점을 세우면, 멈춘 사이 Timer2 만기 · 일꾼 깨움 순서가 바뀔 수 있다 `[추측]` — 조건부 멈춤점 + `g` 로 빨리 지나갈 것.
- **함정 후보** (flow-writer 가 windbg-kd-runbook pitfalls 에 추가 제안 — 사용자 확인 대기):
  - `KiInsertQueueInternal` 의 항목이 패킷인지 가르는 조건은 `by(@rdx+0x10) == 2` 다.
  - 그런데 **IRP 항목도 같은 함수로 들어오고**, 그 자리 바이트 뜻이 다르다 (`IoRemoveIoCompletion +0x10f` — 0 = IRP, 1 = 작은 완료 패킷).
  - 그래서 프로세스 조건 (`$proc`) 과 바이트 조건을 **같이** 걸어야 한다. 안 그러면 IOCP 를 쓰는 모든 프로세스에서 선다.

### 2.19 정리 — 비유로 한 번 더

- 가게 (프로세스) 의 일꾼 사무실 (스레드 풀) 은 **우편함** (IO 완료 포트) 하나를 두고, 일꾼은 우편함 앞에서 봉투를 기다린다.
- 사무실은 관리사무소 (커널) 에 **봉투 달린 번호표** (기다림 완료 패킷) 를 맡긴다: "이 신호등 (대상) 이 초록이 되면, 이 봉투를 우리 우편함에 넣어 주세요."
- 관리사무소는:
  - 신호등이 **이미 초록** 이면 → 그 자리에서 봉투를 우편함에 넣고, "넣었어요" 칸에 1 (`AlreadySignaled` 1).
  - **아직 빨강** 이면 → 번호표를 신호등 번호표 줄 맨 뒤에 걸고, "넣었어요" 칸에 0. 답은 둘 다 **"성공 (0)"**.
- 나중에 신호등 담당 (`KeSetEvent` · 알람 · 세마포어) 이 초록을 켜고 줄을 훑다 봉투 번호표를 만나면, 어깨를 두드리는 대신 **번호표째 우편함에 넣는다**. 개찰구 신호등이면 바로 빨강.
- 우편함 앞에 일꾼이 손을 내밀고 있으면 **손에 바로**, 아니면 우편함 안 봉투 **줄 맨 뒤** 에.
- 일꾼이 꺼내 가면 "걸려 있음" 딱지를 떼고 신호등 · 우편함을 놓아준다. 봉투 번호표는 **일회용** — 다시 쓰려면 다시 건다.
- 취소하면: 걸린 것은 떼고 0, 우편함에 있는데 "두라" 하면 `0x103`, 걸린 적 없으면 `0xC0000120`.
- test.exe 는 가게 문을 열 때까지 봉투 번호표 **여섯** 을 걸어 두고, 아마 그대로 문을 연다 `[추측]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — ntkrnlmp PDB 줄 | run92 까지 | run93 몫 추가 |
| 기준 — 근거 로그 | — | run93 줄: 정적 바퀴, 패킷 · Timer2 · 이벤트는 커널 개체라 run44 사진에 칸이 없음 → 값 대조 없음, 사진 다시 안 읽음, 맡김 6번 갈래는 정적 + `[추측]` |
| 흐름도 앞 글 | — | run93 문단: **그림은 안 고쳤다** — 맡김 6번은 이미 2.4 (run62 · run69 절) 에 글로 적은 syscall 이고, 이번은 그 커널 안쪽 |
| 그림 | — | **그대로** |
| 2.4 제목 · run62 절 | run 목록 run89 까지 | run93 추가, **(run93) 줄 하나** — 맡김 6번의 커널 쪽 (자리 · 대상 · "안 울림" 갈래 `[추측]`) |
| 3장 run77 절 ("기다림·깨움") | `KeSetEvent` 순회는 직원 번호표 갈래만 | **(run93) 한 줄** — 같은 순회에 `WaitType 2` (패킷) 갈래 (`+0x53b` · `+0x587`) `[정적]` |
| 3장 run78 절 ("마감·타이머") | `KiTimerWaitTest +0x32a KiTryUnwaitThread` 만 | **(run93) 한 줄** — `KiTimerWaitTest` 의 `WaitType 2` 갈래 (`+0x266` · `+0x2d9`), Timer2 는 `KiExpireTimer2` 길 `[정적]` |
| 3장 끝 | bcrypt 절이 끝 | **새 절 "기다림 완료 패킷"** — 택배 비유 한 줄 + 트리 하나 + 줄 여덟 (맡기기 · 배달 · 한 번 쓰기 · 취소 · 빈 줄 꺼내기 · PDB 타입 없음 · test.exe 6번 `[추측]` · 남은 것) |
| 4장 | — | run93 새 줄 셋: 동적 (test.exe 로 볼 수 있는 몫 / 다른 샘플 몫) · 정적 (Q176 몫) · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것 |
| 5장 | — | run 표 · 03_sprint 요청서 번호에 run93 줄 |

태그가 바뀐 것 (전 → 후):

- 대기 패킷이 하는 일: "대상이 울리면 우편함에 소식을 넣어 주는 장치" `[추측: 이름]` (run62 해설) → **패킷 자체가 번호표로 대상 줄에 걸리고, 울리면 신호 함수가 `KiInsertQueueInternal` 로 패킷째 IOCP 줄에 넣는다** `[정적]`.
- 번호표 `WaitType 2` = "큐 (완료 포트) 알림": 숫자만 알던 것 (run77, 이름 `[추측]`) → **그 갈래가 어디서 어떻게 가는지** `[정적]` (일곱 길).
- "이미 울린 대상이면 그 자리에서 IOCP 줄에" `[추측]` (팀원 카드) → `[정적]` (`KeRegisterObjectNotification +0x4a ~ +0xa8`).
- test.exe 맡김 6번의 커널 쪽 갈래: 없음 → "안 울림" `[추측]` (ETW Once 1번은 `[정적+추측]`).

### 짐 지도 `baggage.md` 에서 바뀐 것 (마흔아홉째판)

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** — 패킷 · Timer2 · 이벤트는 커널 개체라 체크리스트 밖이다.
- 이미 있는 줄에 (run93) 메모만:
  - 4장 `TppPoolpGlobalPool` · `LdrpThreadPool` 짝 줄 — 대기 완료 패킷이 대상 번호표 줄에 걸림.
  - 6장 "TP 개체 만들기" 부품 줄 — 맡기기의 커널 쪽 사슬, 패킷 안쪽 칸.
  - 7.6 run62 "커널 개체 10 핸들", 7.6.9 커널 개체 줄.
- 기준 run93 줄 · 판 목록 · 8장 두 표.

### 큰 그림에서 어떻게 이어지나

- **우리 지도 (test.exe) 의 길은 안 바뀐다.** 맡김 6번은 원래 2.4 에 있던 syscall 이고, 이번엔 그 **커널 안** 을 채웠다.
- 짐 지도 쪽에서 보면, 은퇴 (P2) 때 커널에 남는 것이 하나 더 또렷해졌다: **대상 여섯에 걸린 패킷 여섯** (연결 표시 1, 참조 셋) `[추측]`.
  - 이게 맞는지는 Q177 ⑥ (test.exe 로 됨) 이 닫는다. Q104 VM 단계에 붙일 후보다.
- 배달 길은 run77 (깨움) · run78 (알람) 에서 본 **같은 신호 함수 안의 다른 갈래** 다. 그래서 "기다림" 이야기가 이제 셋으로 정리된다:
  - 직원 번호표 (`WaitType 1` · `0`) → 어깨 두드리기 (run77)
  - DPC (`WaitType 4`) → (이번엔 안 따라감)
  - **봉투 번호표 (`WaitType 2`) → 우편함 줄 (run93)**
- PE_emulator 쪽에선 Go 런타임이 막힌 자리 (`seq 2256 → 2260`) 를 지나려면 2.16 "엔진이 먼저 할 일" 1 · 2 가 필요하다. 그다음 막힘 후보는 Timer2 를 "이미 지난 시각" 으로 걸었을 때다 (→ Q176 ②).

---

## 4. 아직 모르는 것

### 정적으로 남은 것 → **Q176** (작음)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| 패킷 객체 크기, 만들 때 `KWAIT_BLOCK` 앞 칸 · `WaitKey` (`+0x12`) · `+0x60` 자물쇠 · `+0x68` / `+0x69` 처음 값 (팀원 몸 `TODO` 의 `GenericAll` 값도) | `NtCreateWaitCompletionPacket` · `IopFreeWaitCompletionPacket` 은 범위 밖 (만들기 · 객체 형식 등록) | Q176 ① |
| `NtSetTimer2` → `ExpSetTimer2` → `KeSetTimer2` — 이미 지난 시각이면 그 자리에서 `KiExpireTimer2` 로 가나 (Go: 타이머 걸고 바로 맡김). `NtCreateTimer2` 동기 / 알림 인자, 그리고 `TppInitializeTimerSubQueue +0x37 R9D = 8` · `+0x44 0x100002` 이 어느 쪽인지 | 거는 쪽은 입구만 봄 (결과서) | Q176 ② — **PE_emulator Go 막힘 (`NtSetTimer2` 뒤 7) 에 가장 가까움** |
| `NtRemoveIoCompletionEx` → `IoRemoveIoCompletion` 인자 대응, `KeRemoveQueueEx` 기다림 몸 (APC · Alertable · 깨울 때 항목 건네기) | 질문 4 는 갈림 자리만 | Q176 ③ |
| `ObGetAssociatedWaitObject` 가 대상 형 (파일 · 프로세스 · 스레드 · IOCP 등) 마다 어떤 머리를 고르나 (`+0x2b ~ +0x88`) | 이번엔 NULL · 뮤턴트 · 큐 거르기만 | Q176 ④ |
| `IopCloseWaitCompletionPacket` 이 닫기 처리기로 등록되는 자리, `0xC000A003` 이름 | 형식 만들기는 범위 밖. 결과서는 동적으로 넘겼지만 등록 자리는 정적으로 찾을 수 있을 것 `[추측]` | Q176 ⑤ |

### 실행해 봐야 아는 것 → **Q177** (동적)

| 무엇을 모르나 | 왜 정적으로 못 닫나 | 샘플 |
|---|---|---|
| ⑥ 초기화 맡김 6번이 정말 "안 울림" 인지 — `nt!NtAssociateWaitCompletionPacket+0x1d5` 에서 `al` 이 6번 다 0, 패킷 덤프 `+0x30 ~ +0x69`, 은퇴 전 `KiInsertQueueInternal` 에 `by(@rdx+0x10)==2` 항목이 드나 | Timer2 처음 상태 · WNF 틈 · 은퇴 전 울림은 실행 때 일 | **test.exe 로 됨** — Q104 에 붙일 후보 (사용자 결정) |
| ⑦ Go Timer2 가 동기인지 알림인지 (`KiExpireTimer2 +0x3ad` / `+0x707`) | 인자 값은 Go 가 정함 | Go 샘플 (사용자 결정) |
| ⑧ `AlreadySignaled` 가 틀린 주소일 때 정말 status 0 인지 (`+0x232`) | 착지 바이트를 flow-writer 가 다시 안 풂 (2.6) | 새 샘플 |
| ⑨ 패킷 실제 크기 · `WaitKey` 처음 값 | 정적 ① 이 먼저 | — |
| ⑩ 일꾼 손에 바로 넘겨진 항목을 취소가 못 빼서 `0x103` 이 되는 경우 | 정적으론 갈래만 (2.13) | — |

추천 멈춤점 (run 문서, 커널 멈춤점이라 **test.exe 조건** 필수):

- `nt!KeRegisterObjectNotification` 반환 (`AL`) · 들어올 때 `rdx` (포트) · `r8` (패킷).
- `nt!KiInsertQueueInternal` 에서 `[rdx+0x10]` 바이트가 2 일 때만 — 콜스택 ret 이 `KeSetEvent+0x553` / `+0x5a0` · `KiTimerWaitTest+0x27b` / `+0x2ee` · `KiExpireTimer2+0x3ad` / `+0x707` · `KeReleaseSemaphoreEx+0x24d` · `KeRegisterObjectNotification+0x85` 중 어느 것인지.
- `nt!IopCancelWaitCompletionPacket` 반환 (`AL`) · `nt!NtCancelWaitCompletionPacket` 반환 (`eax`).
- 패킷 덤프 `dq @r8 L0xe` — `+0x30 ~ +0x5f` · `+0x68` / `+0x69` 가 2.3 표와 같은지.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- 패킷 칸 이름 전부 (KeyContext · 연결됨 · 닫힘 등) — PDB 에 타입이 없어 `[추측: 이름]`. `BlockState 4` = `WaitBlockActive` 연결도 `[추측: 이름]`.
- `KiInsertQueueInternal` 의 "항목 수 +1" 명령 줄 자체 — flow-writer 가 안 찾음 (2.9). 결론은 결과서 그대로.
- `IopCloseWaitCompletionPacket +0x0 CMP R9,1` 의 뜻 ("마지막 손잡이") — `[추측]`.
- `KiInsertQueueInternal` 을 부르는 다른 caller (`KeSetProcess +0xed` · `KeTerminateThread +0x2c4` · `KeSignalGate +0x10f` · `KeReleaseMutantEx +0x37c` · `KiResumeThread +0x23c`) — 따라가지 않았다.
- 멈춤점 함정 (`by(@rdx+0x10)==2` + 프로세스 조건) 을 pitfalls 에 넣을지 — 사용자 확인 대기 (2.18).

---

## 5. 더 읽기

- run 문서: [`run93-wait-packet.md`](run93-wait-packet.md) — "PE_emulator 쪽에서 쓸 것" (요청 33 결론 · 팀원 몸과 맞나 · 틀리나 · 최소 조건 표), "패킷 칸 표", "맡기기 차례 표", "배달 길 표", "취소 표", "꺼내기", "호출 관계", "test.exe 초기화의 맡김 6번에 대입", "결과서와 다르게 적은 곳 · 보탠 것"
- 결과서: [`_workspace/sast/89_wait-packet_result.md`](../../_workspace/sast/89_wait-packet_result.md) — "먼저 보는 그림", 질문별 답 1 ~ 5, "동적분석으로 확인해야 할 것", "다음 요청 후보"
- 요청서: [`_workspace/sast/89_wait-packet.md`](../../_workspace/sast/89_wait-packet.md) — 배경 (막힌 자리 에뮬레이터 로그 · 팀원 몸의 빈칸)
- flow.md: [3장 "기다림 완료 패킷"](flow.md) 절, 3장 run77 절 · run78 절의 (run93) 줄, 2.4 run62 절의 (run93) 줄, 4장 run93 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 4장 `TppPoolpGlobalPool` · `LdrpThreadPool` 짝 줄, 6장 "TP 개체 만들기", 7.6 · 7.6.9
- 대기열: [`_workspace/plan.md`](../../_workspace/plan.md) Q175 (끝) · Q176 · Q177
- 앞 해설:
  - [run77](run77-thread-wake-해설.md) — 번호표 칸 (2.3), `KeSetEvent` 자동 / 수동 순회 (2.8), 어깨 두드리기 (2.9), `KiExitDispatcher` (2.10)
  - [run78](run78-thread-timer-해설.md) — 알람 시계 알림형 / 동기형 (2.3), `KiTimerWaitTest` (2.12)
  - [run62](run62-tp-etw-init-해설.md) 2.2 — 스레드 풀 부품 (우편함 · 대기 패킷), WNF · ETW Once
  - [run69](run69-loader-pool-해설.md) — 로더 전용 풀, 타이머 큐 둘 (맡김 5 · 6)
  - [run13](../02_sprint/run13-resume-thread-해설.md) 2.3 · 2.8 — 신호등 · 번호표 · 어깨 두드리기
  - [run92](run92-bcrypt-ksecdd-해설.md) 2.7 — `STATUS_PENDING` (`0x103`)
