# run52 kthread-create — 해설

> 이 글은 [run52 문서](run52-kthread-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.
>
> **고쳐 읽기 (run112)**: 2.5 "`*` 칸 20개" 의 "`KeStartThread` — `ThreadFlags` 비트 13 (`+0xb9`·`+0x1b7`)" 은 틀렸다. `+0xb9` 는 비트 3 `DisableBoost`, `+0x1b7` 은 비트 8 `DisableQuantum` 이고, 비트 13 `SharedReadyQueueAffinity` 는 `KeStartThread +0x344 → KiUpdateSharedReadyQueueAffinityThread +0x55` 가 쓴다. 2.4 의 "실제 목록이 들어가는 때는 안 봤다" 와 4절 표 "친화도 블록 비트맵이 채워지는 때" `[공백]` 도 닫혔다 (`KeStartThread +0xec` · `+0x134` · `+0x147` `KiCopyAffinityEx`). → [run112 문서](run112-thread-later.md) "정정" · [run112 해설 2.4 · 2.15](run112-thread-later-해설.md#215-정정과-결과서와-다르게-적은-곳).

- 대기열: **Q78d1**: Q78d (첫 스레드, 모르는 칸 564) 를 시간 순서 + 구조체별로 나눈 **첫 조각**. `_KTHREAD` "만들 때" 몫 180칸 (`K1~K2` 42 + 끝까지 0 138) (`_workspace/plan.md`, 이번에 **끝**)
  - 나눈 법 (오케스트레이터 기본값 2026-10-30): Q78d1 `_KTHREAD` 만들 때 180 → Q78d2 `_ETHREAD`·`_KTRAP_FRAME` 만들 때 169 → Q78d3 `_TEB` 133 → Q78d4 스레드 그 뒤 구간 (커널) 82. 합 564.
  - 다음 항목: **Q78d2** → Q78d3 → Q78d4 → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 질문이 간 곳: D1 (덤프로 답할 수 있음) → **Q79** 출처 · D2·D3 (실행 중 멈춤, 낮음) → 새 **Q100** (맨 뒤) · D4 는 안 함.
- 근거:
  - run 문서 [`run52-kthread-create.md`](run52-kthread-create.md) (가장 중요)
  - 요청서 [`_workspace/sast/46_kthread-create.md`](../../_workspace/sast/46_kthread-create.md)
  - 결과서 [`_workspace/sast/46_kthread-create_result.md`](../../_workspace/sast/46_kthread-create_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 3.14 (`_KTHREAD` 다시 씀) · 7.15 (새 절)
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 트리와 `PspAllocateThread`·`KeInitThread` 절 · 새 `KiInitializeContextThread` 문단 · 4장 · 4.1 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값 대조는 run44 때 찍어 둔 사진 (`ms/logs/04_baggage-read_K2.txt`, K2 ~ P2 일곱 장) 으로만 했다. 그래서 이 글의 "사진" 은 전부 run44 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 첫 직원 뽑기 다섯 걸음 (`PspAllocateThread`), 인사 카드 (`_ETHREAD`) · 작업대 (유저 스택) · 개인 안내판 (TEB), 시스템 스레드 갈림길, CET 그림자 스택 (책갈피 더미의 **사본**) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음) · [2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나)
- 멈춤 APC (`SchedulerApc`) = **펼쳐 볼 쪽지**, `SuspendEvent` = **신호등**, `ApcQueueable` → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md)
- 트랩 프레임 = **출발 메모**, `KiStartUserThread`, 첫 커널 스택에 쌓는 값 → [run15 해설 2.4 · 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 사물함 (유저 스택 맨 위에 맡긴 `_CONTEXT`) → [run16 해설 2.3](../02_sprint/run16-thunk-context-해설.md)
- 남의 땅에 잠깐 들어가기 (`KiStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 푸시락 (서랍 하나에 거는 **작은 걸쇠**) → [run12 해설](../02_sprint/run12-insert-thread-해설.md)
- AutoBoost, **락 소유 칸 표** (`KernelAbEntries`·`UserAbEntries`) → [run41 해설 2.5](../02_sprint/run41-tls-lock-detail-해설.md)
- 사진 지점 K1 · K2 · K3 · P0 … 와 "처음 값을 갖는 구간" → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가) · [2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)
- "0" 의 세 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀) → [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀)
- 몸통 0 채움 → [run49 해설](run49-kprocess-token-htable-해설.md#몸통은-0-으로-채워져-온다)
- "범위 밖 길에서만 씀 `[추측]`", 오프셋 훑기의 오탐 → [run50 해설 2.4](run50-eprocess-create-해설.md#24-새-셈-범위-밖-길에서만-씀-추측) · [2.5](run50-eprocess-create-해설.md#25-새-말-오프셋-훑기의-오탐)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, `_ETHREAD` = 직원 **인사 카드** (run11).
- 유저 스택 = 가게 안 **작업대**, 그 맨 위 `_CONTEXT` 자리 = **사물함** (run16), 트랩 프레임 = **출발 메모** (run15).
- 이번 run 문서 비유는 그대로 가져온다 ("새 직원 책상 차리기", flow.md 2.1 을 이어서):
  - `_KTHREAD` = 새 직원의 **업무 카드** (아래 새 말).
  - `PspAllocateThread` = 빈 카드를 받아 오는 **접수 창구** (run 문서는 "본사"), `KeInitThread` = 카드를 채우는 **팀장**, `KiInitializeContextThread` = 마무리하는 **비서**.
- 하나만 바꿔 부른다:
  - run 문서는 커널 스택을 "**사물함**" 이라고 불렀다. 그런데 run16 해설부터 사물함은 **유저 스택 위 `_CONTEXT` 자리**였다.
  - 헷갈리지 않게 이 글은 커널 스택을 **"관리사무소 안 작업대"** 라고 부르겠다. 가게 안 작업대 (유저 스택) 와 짝이다.

**새 말: `_KTHREAD` (업무 카드)**

- `_KTHREAD` = `_ETHREAD` 맨 앞에 붙은 **스레드 객체 앞부분** (`Tcb`, `0x4c0` 바이트, 짐 지도 3.14 제목). 칸 237개다.
- 비유: 인사 카드 (`_ETHREAD`) 의 **첫 장**이 업무 카드다. 인사팀 정보 (사번·시작 주소 …) 는 뒤쪽 장에, "지금 어디서 일하나 · 무엇을 기다리나 · 어느 작업대를 쓰나" 같은 **스케줄러가 매일 보는 칸**은 이 첫 장에 있다.
  - 이 구분은 칸 이름을 보고 붙인 비유다. 칸마다의 뜻은 아래에서 하나씩 근거와 같이 본다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| `_KTHREAD` 만들 때 몫 모르는 칸 | **180 → 여전히 모름 0** |
| `K1~K2` 42칸 | **42칸 모두** 처음 값을 쓰는 줄을 찾음 — `KeInitThread`·`PspAllocateThread`·`KiInitializeContextThread` + 도우미 셋 `[정적]`. 사진 K2 값과 칸마다 맞음 |
| 끝까지 0 138칸 | 0 을 씀 **35** · 조건부 안 탐 **12** · 아무도 안 씀 **21** · 범위 밖 길 **70** `[추측]` |
| 몸통 0 채움 | **`PspAllocateThread +0x307 memset`** (몸통 + 확장 영역 전부) `[정적]` |
| 0 을 씀 35 의 정체 | 모두 **같은 DWORD 되쓰기** — `MiscFlags` 15비트 + `ThreadFlags` 20비트 `[정적]` |
| test.exe 첫 스레드가 탄 길 | **사용자 스레드 · 새 커널 스택 · 커널 CET 꺼짐 · 사용자 CET 꺼짐** `[정적]` + 사진 비트 `[관찰 간접]` |
| `Teb` (+0xf0) 칸 | 짐작 → **정확** (`KeInitThread +0x1e4`) |
| `+0x172` 가 부르는 함수 | **`KeInitializeTimer`** (요청서의 `KeInitializeTimerEx` 아님) |
| 간접 호출 | 만들기 길 안에 **없음** `[정적]` |
| 사진 함정 | **K1 의 이 스레드 주소는 옛 객체 값** — 끝까지 0 판정은 K2 ~ P2 일곱 장 기준 |

이 글에서 붙잡아 둘 세 줄:

> **(1) 업무 카드는 지우개로 한 번 민 다음, 팀장 한 사람이 거의 다 채운다.**
> `+0x307 memset` 이 카드 전체를 0 으로 밀고, `KeInitThread` 와 그 도우미가 `K1~K2` 칸 대부분을 쓴다.
>
> **(2) "끝까지 0" 은 네 가지로 갈린다. 그리고 넷 중 둘 (아무도 안 씀 · 범위 밖 길) 은 경계가 약하다.**
> 둘 다 "초기화 동안 쓰는 줄이 없다" 는 것만 `[정적]` 이다. 어느 쪽이냐는 커널 전체를 **오프셋 숫자**로 훑은 결과라 틀리거나 놓칠 수 있다.
>
> **(3) 표시 묶음을 "통째로 되쓰는" 줄 하나가 35비트에 0 을 쓴다.**
> 펜이 한 번도 안 닿은 칸과는 다르다. 그래서 "아무도 안 씀" 이 아니라 "0 을 씀" 이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간도 **부모 cmd 스레드 안, 커널**이다. run50·run51 이 가게 장부 (`_EPROCESS`) 를 닫았고, 이번엔 같은 `NtCreateUserProcess` 안 **다음 상자**, 첫 직원의 업무 카드를 본다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0xc36  PspAllocateProcess              가게 장부 (run49·run50)
   ├─ +0xe5c  PspAllocateThread               첫 직원 뽑기                      ← 이번
   │    ├─ +0x2e7  ObCreateObjectEx            빈 카드 받기
   │    ├─ +0x30c  memset                      지우개로 싹 밀기               ← 이번
   │    ├─ +0x532 · +0x5ef · +0x631            사번 · 작업대 · 개인 안내판 (run11)
   │    └─ +0x6f9  KeInitThread                팀장이 업무 카드 채우기        ← 이번 (거의 다)
   │         ├─ +0x177  KeInitializeTimer
   │         ├─ +0x278  MmCreateKernelStack    관리사무소 안 작업대 받기     ← 이번
   │         ├─ +0x2c5  KeAbInitializeThreadState
   │         └─ +0x301  KiInitializeContextThread  비서의 마무리            ← 이번
   ├─ +0xfc2  PspInsertProcess                상가 명부 (run51)
   └─ +0x101a PspInsertThread                 (Q78d4 쪽)
 (그 뒤 KeStartThread·스케줄러 — 남은 47칸, Q78d4)
 test.exe 메인 스레드: PspUserThreadStartup → ntdll 로더 → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (콜스택에서 보이는 숫자). run 문서 "호출 관계" 와 flow.md 2.1 트리에서 대표만 골랐다.
- 사진 지점으로 보면 이번 칸은 모두 **`K1~K2`** 또는 **끝까지 0** 이다. K1 = `PspAllocateThread` 입구, K2 = 그 뒤 (run44).

### 무엇이 궁금했나

짐 지도 3.14 `_KTHREAD` 는 237칸 중 아는 칸이 **10** 뿐이었다 (모름 227). 그중 "만들 때" 몫 180 을 이번에 닫는다.

요청서 질문은 넷이었다.

1. `K1~K2` 42칸: 칸마다 쓰는 줄 (함수+오프셋) 과 **값의 출처** (프로세스에서 복사 / 인자 / 계산 / 상수 / 전역).
2. 끝까지 0 138칸: 만들기 길 안에서 **0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길** 로 가르기.
3. 갈림길 (시스템 스레드·보안 스레드·CET·UMS·GUI) 과 간접 호출.
4. 칸마다 짐 지도 3장 양식.

- 범위 밖: `_ETHREAD` 나머지·`_KTRAP_FRAME` (Q78d2), `_TEB` (Q78d3), `K2` 뒤 구간 47칸 (Q78d4), XSAVE 영역 안쪽 내용.

### 바퀴 전에 알던 것

- `[정적]` (run11) `PspAllocateThread` 다섯 걸음 (카드 → 사번 → 작업대 → 개인 안내판 → `KeInitThread`).
- `[정적]` (run13) `KeInitThread` 가 멈춤 쪽지 (`SchedulerApc`, `+0x152`) 와 신호등 (`SuspendEvent`, `+0x163`) 을 미리 만든다. `ApcQueueable` 을 기본으로 켠다 (`+0xed`).
- `[정적]` (run15) `KiInitializeContextThread` 가 `InitialStack` (`+0x2ca`)·`KernelStack` (`+0x2ce`) 을 쓴다.
- `[관찰]` (run44) `K1~K2` 42칸의 값이 K2 덤프에 있다. `Teb` = TEB 주소 `9b2fa67000`.
- 짐 지도 3.14 아는 칸 10 = 정확 9 + 짐작 1 (`Teb` — 칸 이름이 run11 부터 짐작이었다).

---

## 2. 찾은 것

### 2.1 한눈에: 업무 카드를 누가 채우나

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret, 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄. 모두 `[정적]`.

```
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread
         │  +0x2e7 ObCreateObjectEx → +0x30c memset (몸통 + 확장 영역 0)
         │  +0x352 KernelAbEntries · +0x36c Affinity · +0x3ab UserAffinity · +0x3ff RealtimePriorityFloor
         │  +0x8e1 UserAbEntries (ThreadContext ≠ 0 일 때)
         └─ +0x6f9  KeInitThread
                 │  +0x54~+0x7e Header 줄 머리·WaitBlock 넷 · +0x9d ThreadFlags · +0xa9 ExpectedRunTime
                 │  +0xd7 Process · +0xfd MiscFlags · +0x10c WaitIrql
                 ├─ +0x157  KeInitializeApc            SchedulerApc
                 ├─ +0x168  KeInitializeEvent          SuspendEvent
                 ├─ +0x177  KeInitializeTimer          Timer
                 │  +0x1e4 Teb · +0x1eb Header.Type = 6
                 ├─ +0x278  MmCreateKernelStack        → +0x29b InitialStack(첫) · +0x29f StackBase · +0x2ac StackLimit
                 ├─ +0x2c5  KeAbInitializeThreadState  MiscFlags 비트 0 · 부스트 줄 둘
                 │  +0x333 ModeHistory
                 └─ +0x301  KiInitializeContextThread
                         │  +0x25b NpxState · +0x25e PreviousMode = 1
                         │  +0x2ca InitialStack · +0x2ce KernelStack · +0x2d2 StateSaveArea
```

비유로 한 번 (run 문서 비유):

- 접수 창구 (`PspAllocateThread`) 가 빈 업무 카드를 받아 (`ObCreateObjectEx`) **지우개로 한 번 싹 민다** (`memset`). 카드 뒤에 붙은 **별지 몇 장** (확장 영역, 2.4) 도 같이 민다.
- 별지 자리를 카드에 적어 두고 (`Affinity`·`…AbEntries` 포인터), 팀장 (`KeInitThread`) 에게 넘긴다.
- 팀장이 칸을 채운다:
  - **기본 서식** — 종류 (`Type` 6), 기다림 줄 넷, 타이머, 멈춤 쪽지·신호등, 빈 목록 머리들.
  - **관리사무소 안 작업대** (커널 스택) — 새로 하나 받아서 (`MmCreateKernelStack`) 위·아래 끝을 적는다.
  - **"어느 가게 소속"** — `Process` 칸.
  - **표시 묶음 두 줄** (`MiscFlags`·`ThreadFlags`) — 묶음을 통째로 읽어 두세 칸만 체크하고 통째로 되쓴다 (2.7).
- 마지막으로 비서 (`KiInitializeContextThread`) 가 작업대 맨 위에 **개인 짐 상자 자리** (`StateSaveArea`, 2.5) 를 잡고, "**가게 쪽 직원**" 표시 (`PreviousMode` 1) 를 단다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 카드를 지우개로 미는 줄

`PspAllocateThread +0x307` `memset(스레드, 0, 크기)` (call `+0x307` / ret `+0x30c`) `[정적]`

- **누가**: `PspAllocateThread` (첫 직원 뽑는 함수, run11)
- **어디서**: `+0x307`. 바로 앞 `+0x2e2` 에서 `ObCreateObjectEx` 로 스레드 객체를 받았다 (ret `+0x2e7`).
- **누구를**: 공용 부품 `memset`. 넘기는 크기는 객체를 만들 때 넘긴 크기 (`[RSP+0x98]`) **그대로**다.
- **무엇을 위해**: 몸통 `0x798` + 에너지 칸 `0xc8` + 친화도 블록 둘 + 잠금 칸 블록 둘을 **전부 0** 으로 만들려고.
- 그래서 **모든 칸은 0 에서 시작한다.** 뒤에서 나올 "아무도 안 씀" 의 0 은 바로 이 줄에서 온다.
  - run49·run50 은 할당 쪽 0 채움 비트 (`ObpAllocateObject`) 로 0 을 설명했다. 이번엔 이 `memset` 이 있어서 그쪽은 안 봤다 (결과서).

#### 한 줄 읽기 ②: 표시 묶음을 통째로 되쓰는 줄

`KeInitThread +0x9d` `MOV [RDI+0x78],EDX` `[정적]`

- **누가**: `KeInitThread` (팀장)
- **어디서**: `+0x9d`. 앞에서 `+0x80` 이 같은 자리를 읽고, `+0x97` `OR EDX,0x20040` 으로 두 비트를 켰다.
- **무엇을**: `RDI` = 새 스레드. `+0x78` = `ThreadFlags` 묶음. 레지스터 `EDX` 의 **32비트를 한 번에** 거기 쓴다.
- **무엇을 위해**: 비트 6 `EnableStackSwap` · 비트 17 `KernelStackResident` 를 켜려고 (사진 K2 `0x20040`).
- 덤으로 생기는 일: 나머지 비트 자리도 이 줄이 **같이 썼다.** 값은 0 이다. 이게 "0 을 씀" 20비트다 → 2.7 에서 천천히.

#### 한 줄 읽기 ③: 개인 안내판 주소를 카드에 적는 줄

`KeInitThread +0x1e4` `MOV [RDI+0xf0],RAX` `[정적]` (flow-writer `.asm` 확인)

- **누가**: `KeInitThread`
- **어디서**: `+0x1e4`. 바로 앞 `+0x1e0` 이 시작 정보 묶음 (2.11) 의 `+0x28` 칸을 읽어 `RAX` 에 담았다. 그 칸 = `MmCreateTeb` 결과 = 새 TEB.
- **무엇을**: 업무 카드 `+0xf0` 칸에 TEB 주소를 쓴다. PDB 칸 이름도 `Teb` 다 (`struct__KTHREAD.tsv`).
- **무엇을 위해**: "이 직원의 개인 안내판은 여기" 를 업무 카드에 적어 두려고.
- 왜 중요했나: run11 부터 이 칸은 "칸 이름 `[추측]`" 인 **짐작**이었다. 사진 값 (`9b2fa67000`) 은 맞았지만 쓰는 줄을 몰랐다. 이 한 줄로 **짐작 → 정확**.
  - run11 문서는 안 고쳤다. 그때 근거로는 `[추측]` 이 맞는 표시였다 (run 문서 "정정").

### 2.3 180칸은 어디로 갔나

#### 표 (run 문서·결과서 맨 앞 표 그대로)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1~K2` | 42 | **42** | — | — | — | — | 0 |
| 끝까지 0 | 138 | — | **35** | **12** | **21** | **70** | 0 |
| **합** | **180** | **42** | **35** | **12** | **21** | **70** | **0** |

#### 한 줄 읽기: 끝까지 0 줄

- 138칸 모두 사진 K2 ~ P2 일곱 장 내내 0 이었던 칸이다 (왜 여덟 장이 아니라 일곱 장인지는 2.10).
- 35칸은 만들기 길이 **0 을 쓴다** (전부 표시 묶음 되쓰기). 12칸은 켜는 줄이 있는데 test.exe 에선 **조건이 안 맞는다**.
- 21칸은 **아무도 안 쓴다**. 70칸은 만들기 길에선 안 쓰고 **다른 길이 쓰는 것 같다** `[추측]`.
- "여전히 모름" 이 0 이다. 끝까지 0 칸은 다 어딘가로 갔다.

#### 짐 지도로 옮길 때 (run50 과 같은 규칙)

- "아는 칸" 42 + "0 을 씀" 35 + "조건부 안 탐" 12 = **89** → 짐 지도 "nt 가 씀" (줄이 있는 칸).
- "아무도 안 씀" **21** → 2장 표 아래 줄.
- "범위 밖 길에서만 씀 `[추측]`" **70** → 모름에서 빼고 "아무도 안 씀" 옆에 따로 한 줄.
- 그리고 이미 아는 `Teb` 1칸이 짐작 → 정확 (2.2 ③).

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_KTHREAD` (237칸) | 10 → **99** | 0 → **21** | 0 → **70** | 227 → **47** | 9 + 1 → **99 + 0** |

- 새 아는 칸 89 는 모두 정확 (쓰는 줄 `[정적]`). 옛 짐작 `Teb` 1 → 정확. 그래서 정확 +90 · 짐작 −1.
  - `ExtendedFeatureDisableMask` 도 정확으로 셌다. 줄 (`KeInitThread +0x486`) 과 조건은 `[정적]` 이고, 그 갈래를 탔는지만 동적이라서다 (2.8).
- 남은 모름 47 = `K2~K3` 10 · `K3~P0` 9 · `P0~P1` 9 · `P1~P1a` 7 · `P1a~P1b` 6 · `P1b~P2` 6 → **Q78d4** (`KeStartThread`·스케줄러).
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 507 → **596** (정확 449 → **539** · 짐작 58 → **57**) + 아무도 안 씀 77 → **98** + 범위 밖 길에서만 씀 53 → **123** + 모름 819 → **639**.

### 2.4 출발점: 카드 + 별지 몇 장, 전부 0

#### 새 말: 확장 영역

- **확장 영역** = 스레드 객체 몸통 (`0x798`) **뒤에 덧붙여 받은 공간**. 객체를 만들 때 크기를 더 크게 불러서 한 번에 받는다 (run11 해설 2.3 의 "인사 카드 뒤에 붙일 덤 공간 계산").
  - 비유: 업무 카드 뒤에 **스테이플러로 찍어 둔 별지**. 카드 칸에는 "별지 몇 쪽 참고" 처럼 **별지 자리 (포인터)** 만 적는다.

#### 무엇이 붙어 있나 `[정적]` (사진 K2 포인터 값과 맞음)

| 객체 + | 무엇 | 카드 칸 (포인터) | 쓰는 줄 |
|---|---|---|---|
| `0x798` | 에너지 칸 (`0xc8`B) | — | — |
| `0x860` | `_KAFFINITY_EX` (커널용 친화도) | `Affinity` | `PspAllocateThread +0x36c` |
| `0x870` | `_KAFFINITY_EX` (사용자용) | `UserAffinity` | `+0x3ab` |
| `0x880` | `_KLOCK_ENTRIES` 6칸 블록 (`0x220`B, 커널용) | `KernelAbEntries` | `+0x352` |
| `0xaa0` | `_KLOCK_ENTRIES` 8칸 블록 (`0x2d0`B, 사용자용) | `UserAbEntries` | `+0x8e1` (`ThreadContext ≠ 0` 일 때만) |

- **친화도** = 이 스레드가 돌아도 되는 CPU 목록으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: "이 직원은 1번·3번 계산대에서만 일함" 표.
  - 지금은 블록 머리 (`Count` = 1, `Size` = 그룹 수) 만 채우고 비트맵은 `memset` 0 이다. 실제 목록이 들어가는 때는 안 봤다 `[공백]` (Q78d4).
- **잠금 칸 블록** (`_KLOCK_ENTRIES`) = [run41 해설 2.5](../02_sprint/run41-tls-lock-detail-해설.md) 의 **락 소유 칸 표** (AutoBoost 용) 를 커널 쪽에서 담는 블록이다. run41 이 본 `+0x470 KernelAbEntries`·`+0x478 UserAbEntries` 가 바로 이 블록을 가리키는 칸이다.
  - 이번에 새로 본 것 `[정적]`: 블록이 **카드 뒤 별지 자리**에 있고 (`+0x880` / `+0xaa0`), 블록 앞 `+0x0` = 스레드, `+0xc` = 칸 수 (6 / 8). 블록 안 칸 상태 초기화는 `KeAbInitializeThreadState` (`+0x4b ~ +0xad`).
- 이 별지들은 칸 표 (PDB 칸 목록) 가 체크리스트 21종 밖이라 짐 지도 7.6 에 **기록만** 했다.

### 2.5 `K1~K2` 42칸: 네 사람 + 도우미 셋

#### 누가 무엇을 (run 문서 "묶어 보면") `[정적]`

| 쓰는 사람 | 칸 (대표) |
|---|---|
| `KeInitThread` (팀장) | `Header` (`Type` 6·줄 머리), 스택 둘, `ExpectedRunTime`, `ThreadFlags` 두 비트, `ApcState` 앞쪽, `Timer` 의 `Dpc`·줄 연결, `WaitBlock` 묶음 9칸, `Process`, `MutantListHead`, 리스트 머리 넷, `ModeHistory` |
| `PspAllocateThread` (접수 창구) | `Header` 에너지 비트 (`+0x321`), `Affinity`·`UserAffinity`, `RealtimePriorityFloor` `0x20`, `KernelAbEntries`·`UserAbEntries` |
| `KiInitializeContextThread` (비서) | `StateSaveArea`, `PreviousMode` 1, `NpxState` |
| 도우미 셋 (줄기 끝 — 더 부르는 게 없음, `_calls.tsv` 0줄) | `KeAbInitializeThreadState` (`MiscFlags.AutoBoostActive`·`PropagateBoostsEntry`·`IoSelfBoostsEntry`), `KeInitializeApc` (`SchedulerApcFill1~5`), `KeInitializeTimer` (`Timer` 안쪽) |

- 값의 출처는 칸마다 짐 지도 3.14. 몇 개만 보면:
  - **상수**: `Type` 6, `RealtimePriorityFloor` `0x20`, `PreviousMode` 1, `ModeHistory` `0xaaaaaaaa` (사용자 스레드일 때).
  - **인자**: `Process` (시작 정보 묶음 `+0x30`), `Teb` (`+0x28`).
  - **전역에서 계산**: `ExpectedRunTime` = `KiShortExecutionCycles` 를 4비트 왼쪽으로 민 값 (사진 `0x4c4b40`), `StackLimit` = `StackBase − KeKernelStackSize`.
  - **도우미가 돌려준 값**: `StackBase` = `MmCreateKernelStack` 결과.
- 이 숫자들이 **사진 K2 와 칸마다 맞았다** (run 문서 "환경"): `StackBase − StackLimit` = `0x7000`, `StateSaveArea` = `StackBase − 0x9c0`, `InitialStack` = 그 − `0x50`, `KernelStack` = `InitialStack − 0x310`.
  - 그래서 결과서는 "바이너리와 타깃 VM 이 같다고 볼 수 있다" 고 했다.

#### 새 말: 겹치는 칸 (`*Fill*`)

- `WaitBlockFill5~11`, `SchedulerApcFill1~5` 같은 칸은 **다른 칸과 같은 바이트를 가리키는 자리**다. PDB 칸 목록에 이름이 따로 있을 뿐, 메모리는 겹친다 (공용체).
  - 비유: 카드 한 줄을 "1~3번 칸", "1~5번 칸", "1~8번 칸" 처럼 **범위만 다르게 부르는 이름표**를 여러 개 붙여 둔 것.
- 그래서 **같은 줄이 여러 칸의 쓰는 줄이다** `[정적]`.
  - `WaitBlockFill5~11` 은 대기 블록 4개의 `Thread` 칸 (`+0x158 · +0x188 · +0x1b8 · +0x1e8`) 과 마지막 블록의 줄 연결·`+0x1e0` DWORD 를 **어디까지 덮느냐**만 다르다. 쓰는 줄은 `KeInitThread +0x69 ~ +0x7e` 루프 하나다.
  - 증거 하나: 사진 K2 에서 0 이 아닌 8바이트 덩어리 수가 코드로 센 것과 같았다 (예: `WaitBlock` 8, `Timer` 4, `SchedulerApcFill3` 6).

#### `*` 칸 20개: 처음 값만 닫았다

- `*` = 사진에서 K2 뒤에 **또 바뀌는** 칸. 이번엔 **처음 값을 쓰는 줄만** 닫았다. 뒤에 바꾸는 쪽은 이름만:
  - `KeStartThread` — `Header` 비트 23 (`+0x353`), `ThreadFlags` 비트 13 (`+0xb9`·`+0x1b7`) (Q78d4)
  - 시계 틱 — `ModeHistory` ← `KeAccumulateTicks +0x58` (`.asm` 없음) `[추측]`
  - 대기·스케줄러 — `KiCommitThreadWait`·`KiSwapThread` 쪽 `[추측]`

#### `InitialStack` 은 두 번 쓴다 `[정적]`

1. `KeInitThread +0x29b` — 새 커널 스택 **맨 위** (`StackBase` 와 같은 값).
2. `KiInitializeContextThread +0x2ca` — XSAVE 영역 **아래로 내린** 값으로 덮는다.

- 사진 K2 에 찍힌 건 둘째 값이다. 짐 지도 3.14 의 옛 줄 (`+0x2ca`) 은 맞았고, 첫 쓰기 줄을 덧붙였다.

#### 새 말: `StateSaveArea` (XSAVE 영역)

- **XSAVE 영역** = CPU 의 확장 레지스터 (부동소수·벡터 등) 를 스레드가 쉴 때 저장해 두는 자리로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이번에 본 것 `[정적]`: 커널 스택 맨 위에서 전역 `KiXSaveAreaLength` 만큼 내려와 `0x40` 단위로 내린 주소다 (`KiInitializeContextThread +0x39 SUB`, `+0x3f AND`, 쓰기 `+0x2d2`). 사진 K2 = `StackBase − 0x9c0`.
- 비유: 관리사무소 안 작업대 **맨 위 칸에 둔 개인 짐 상자**. 쉬는 동안 손에 들고 있던 도구를 여기 넣어 둔다.
- 안쪽 내용은 칸 표가 없는 짐이라 짐 지도 7.6 에 기록만.

### 2.6 핵심 (1): "끝까지 0" 을 네 가지로 가르는 법

이번 바퀴의 뼈대다. 천천히 가 보자.

#### 왜 가르나

- 사진만 보면 138칸은 다 "0" 이다. 그런데 **0 이 된 까닭**은 여러 가지다 ([run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀) 의 셋 + [run50](run50-eprocess-create-해설.md#24-새-셈-범위-밖-길에서만-씀-추측) 에서 생긴 넷째).
- 에뮬레이터 쪽에서 보면 까닭마다 할 일이 다르다 (run47 해설 2.4 의 짐작 `[추측]`): 맡을 함수가 있나 / 갈림길을 흉내 내야 하나 / 0 으로 받기만 하면 되나.

#### 가르는 순서 (결과서 "판단 기준" 한 줄, run50 과 같음)

```
그 칸에, 만들기 길 함수의 .asm 안에 "쓰는 줄" 이 있나?
 ├─ 있다
 │   ├─ test.exe 에서 그 줄을 지난다 (값 0, 또는 같은 DWORD 를 읽어 일부만 바꿔 되씀) → ① 0 을 씀
 │   └─ 켜는 줄인데 test.exe 조건이 안 맞아 안 지난다                              → ② 조건부 안 탐
 └─ 없다 → 커널 전체를 칸 오프셋 숫자로 훑는다
     ├─ 이름 있는 스레드·스케줄러 쪽 함수가 쓰는 게 보인다                           → ④ 범위 밖 길 [추측]
     └─ 안 보인다 (또는 오탐뿐)                                                    → ③ 아무도 안 씀
```

- **만들기 길** = `NtCreateUserProcess → PspAllocateThread` 본문 + 새 스레드 포인터를 넘겨받는 callee 줄기 (`KeInitThread`, `KiInitializeContextThread`, 도우미 셋, `KiAllocateXStateStack`, `KiCreateKernelShadowStack`, `KeCopyXfdMaskToTeb`, `MmCreateKernelStack`) (결과서).

#### 업무 카드 빈칸 비유로 (태그 없음)

| | 0 이 된 까닭 | 업무 카드 비유 | 이번 수 | 예 |
|---|---|---|---|---|
| ① | 0 을 씀 | 팀장이 그 칸에 **펜을 댔는데** 적은 게 빈칸 | 35 | `MiscFlags.WaitNext`, `ThreadFlags.GuiThread` |
| ② | 조건부 안 탐 | "**해당자만 기입**" 칸. 이 직원은 해당이 아님 | 12 | `MiscFlags.SystemThread` (시스템 스레드만) |
| ③ | 아무도 안 씀 | 지우개로 민 뒤 **아무도 펜을 안 댄** 칸 | 21 | `Spare13`, `InGlobalForegroundList` |
| ④ | 범위 밖 길 `[추측]` | 출근 뒤에 **다른 부서**가 채우는 칸 (근무 시간·휴가 기록 같은 것) | 70 | `KernelTime`·`UserTime` ← `KeAccumulateTicks` 등 |

- ① 과 ③ 은 사진으로는 똑같이 0 이다. 차이는 **그 자리를 쓰는 명령이 있느냐**다. 이게 2.7 의 이야기다.
- ③ 과 ④ 의 차이는 "**다른 길**에 쓰는 함수가 보이느냐" 다. 이건 훑기 근거라 약하다. 이게 2.9 의 이야기다.

#### ③ 의 좋은 예: 머리는 쓰고 꼬리는 안 쓴 칸 `[정적]`

- `KeInitThread +0x2c5` 가 `GlobalForegroundListEntry` (`+0x370`) 의 **앞 8바이트**에 1 을 쓴다 ("줄에 안 들어 있음" 표시).
- 같은 16바이트의 **뒤 8바이트** `InGlobalForegroundList` (`+0x378`) 는 **안 쓴다**. 사진 K2 `[1 - 0]`.
- 그래서 앞은 `K1~K2` 아는 칸, 뒤는 "아무도 안 씀" 이다. `+0x3f0`/`+0x3f8` (`…UpdateVpThreadPriority…`) 짝도 똑같다.
- 바로 옆 바이트라도, 쓰는 줄이 **앞 8바이트만** 쓰면 뒤 8바이트엔 펜이 안 닿는다. 다음 절의 "DWORD 되쓰기" (안 바꿀 비트까지 같이 쓰는 줄) 와 정반대 경우다.

### 2.7 핵심 (2): DWORD 하나를 되쓰면 왜 다른 비트에 "0 을 쓴" 것이 되나

사용자가 헷갈리기 쉬운 곳이다. 한 걸음씩.

#### 새 말: DWORD 와 표시 묶음

- **DWORD** = 4바이트 = **비트 32개** 한 덩어리.
- **표시 묶음** (`MiscFlags` `+0x74`, `ThreadFlags` `+0x78`) = DWORD 하나를 비트마다 다른 뜻으로 쪼개 쓰는 칸. PDB 에는 비트마다 이름이 있다 (`ApcQueueable` = 비트 14 등). 짐 지도는 이 비트 이름 하나를 **한 칸**으로 센다.
  - 비유: 업무 카드에 인쇄된 **체크박스 32개짜리 띠 한 줄**.

#### 걸음별로 (`ThreadFlags`, 결과서 2.1·run 문서) `[정적]`

1. `PspAllocateThread +0x307 memset` 이 카드 전체를 0 으로 민다. 이때 `ThreadFlags` 32비트도 모두 0.
2. `KeInitThread +0x80` 이 `ThreadFlags` DWORD 를 **통째로 읽어** 레지스터 `EDX` 에 담는다. 담긴 값은 0 이다 (1번 때문).
   - 그 사이 `+0x83 ~ +0x95` 가 프로세스 `KPROCESS.ProcessFlags.AutoAlignment` 를 비트 2 자리에 복사한다. test.exe 는 이 값이 0 이다 (사진).
3. `+0x97 OR EDX,0x20040` — **레지스터 안에서** 비트 6·17 만 켠다.
4. `+0x9d MOV [RDI+0x78],EDX` — 레지스터 32비트를 카드에 **한 번에** 쓴다.
5. 그러니 4번 줄은 비트 6·17 만 쓴 게 아니라 **32비트 자리 전부를 썼다.** 나머지 비트에 들어간 값은 2번에서 읽어 둔 0 그대로다.

`MiscFlags` 도 같은 꼴이다:

- `+0xde LEA R12,[RDI+0x74]` → `+0xe7` 읽기 → `+0xed`/`+0xf1` 비트 14 (`ApcQueueable`) 만 바꿈 → **`+0xfd MOV [R12],EDX`** 되쓰기.

#### 그래서 왜 "아무도 안 씀" 이 아닌가

- "아무도 안 씀" = 그 바이트에 **어떤 쓰기 명령도 안 닿았다**. 0 은 순전히 `memset` 덕이다.
- "0 을 씀" = **이 함수의 이 줄이 그 비트 자리를 썼다.** 값이 우연히 0 일 뿐이다.
- 비유: 팀장이 체크박스 띠를 **오려 내서** (읽기), 두세 칸에 체크하고 (OR), 카드에 **통째로 다시 붙였다** (되쓰기). 나머지 칸도 "빈칸인 채로 **새로 붙여진**" 것이다. 지우개로 민 뒤 아무도 손 안 댄 칸과는 다르다.
- 짐 지도 쪽 뜻 (run47·run50 규칙): 줄이 있는 칸은 "nt 가 씀" 으로 센다. 이 35비트를 정하는 줄은 `KeInitThread +0x9d`·`+0xfd` 다.
  - 내 짐작 `[추측]`: 에뮬레이터의 `KeInitThread` 가 같은 줄을 흉내 내면 이 35비트도 저절로 정해진다. 따로 "0 으로 둘 칸" 목록이 필요 없다는 뜻이다. 판단은 사용자 몫 (CLAUDE.md "지금 목표").

#### 숫자 맞추기

- 끝까지 0 인 표시 묶음 비트는 `MiscFlags` 21 + `ThreadFlags` 20 = **41** (결과서 "한눈에"). 138칸의 약 3분의 1이다.
- 그중 `MiscFlags` 6비트는 **켜는 줄이 따로 있어서** "조건부 안 탐" 으로 셌다 (2.8). 남은 `MiscFlags` 15 + `ThreadFlags` 20 = **35** 가 "0 을 씀" 이다.
- `MiscFlags` 15비트: `ReadyTransition`·`WaitNext`·`SystemAffinityActive`·`Reserved1`·`ApcInterruptRequest`·`QuantumEndMigrate`·`TimerActive`·`ProcessDetachActive`·`Reserved2`·`ScbReadyQueue`·`Reserved3`·`WaitNextClearWobPriorityFloor`·`TimerSuspended`·`SuspendedWaitMode`·`Reserved` (24:8).
  - `Reserved` (24:8) 는 8비트짜리지만 PDB 이름 하나라 한 칸이다.
- 이 비트들을 **나중에** 켜는 곳 (대기·APC·친화도·붙이기·퀀텀·GUI·얼림·종료·ETW) 은 이름만 `[추측]` (결과서 2.1).

#### 덤: `ApcQueueable` 을 켜는 조건 `[정적]`

- 시작 정보 묶음 플래그 (`+0x3c`) **비트 1 이 꺼져 있을 때** 켠다 (`+0xf5 ~ +0xfd`). test.exe 는 플래그 0 이라 켜짐. run13 때 "기본으로 켠다" 던 줄의 조건이 구체화됐다.
- 비트 0 `AutoBoostActive` 는 나중에 `KeAbInitializeThreadState +0xb2` 가 `OR` 로 켠다. 그래서 사진 K2 `MiscFlags` = `0x4001` (비트 0 + 비트 14).

### 2.8 조건부 안 탐 12: test.exe 첫 스레드가 탄 길

#### 갈림길 넷 `[정적]`

| 갈림길 | 가르는 곳 | test.exe |
|---|---|---|
| 시스템 스레드 | `ThreadContext == 0` (`KeInitThread +0x112`) | **사용자 스레드** — `NtCreateUserProcess +0xe3a` 가 `_CONTEXT` 포인터를 넘김 |
| 커널 스택 | `param_2+0x00 == 0` (`+0x234`) | **새로 만듦** → `+0x273 MmCreateKernelStack` |
| 커널 CET (그림자 스택) | 전역 `KiKernelCetEnabled` (`+0x2b0`) | **꺼짐** (사진 `MiscFlags` 비트 22 = 0) `[관찰 간접]` |
| 사용자 CET | 프로세스 `MitigationFlags2.CetUserShadowStacks` (비트 14, `PspAllocateThread +0x5b9`) | **꺼짐** (sast/44 K1 `0x40000000`, 사진 비트 20 = 0) |

- 시스템 스레드·CET 그림자 스택의 뜻은 [run11 해설 2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나) (관리사무소 안에서만 일하는 직원 / 책갈피 더미의 사본).
- `[관찰 간접]` = 전역 값을 직접 읽은 게 아니라, 그 전역이 켜졌으면 켜졌을 **사진 속 비트**가 0 인 것을 본 것이다. 전역 값 자체는 D1 (Q79).

#### 12칸이 어디서 갈리나 (run 문서 표에서)

- 시스템 스레드 갈래 (안 탐): `MiscFlags.SecureThread` (`+0x4d8`), `MiscFlags.SystemThread` (`+0x1c5`).
- CET 갈래 (안 탐): `MiscFlags.CetUserShadowStack` (`PspAllocateThread +0xb3f`), `MiscFlags.CetKernelShadowStack` (`+0x391 ~ +0x397`), `KernelShadowStack` 4칸 (`+0x40c · +0x417 · +0x423 · +0x44f`, `+0x3ec KiCreateKernelShadowStack` 뒤).
- 만들기 플래그 `0x200` (안 줌): `MiscFlags.BypassProcessFreeze` (`PspAllocateThread +0xb10`).
- 전역 조건: `MiscFlags.StateSaveAreaDecoupled` (`KiAllocateXStateStack +0x52`), `ExtendedFeatureDisableMask` (`+0x486`), `IptSaveArea` (`KiInitializeContextThread +0x61`, 전역 `KiIptSaveAreaLength` ≠ 0 일 때 — 사진 0 `[관찰 간접]`).
- 합: 2 + 1 + 1 + 4 + 1 + 1 + 1 + 1 = 12.

#### 하나만 못 가른 칸: `ExtendedFeatureDisableMask` `[공백]`

단계별로:

1. 근거 `[정적]`: 켜는 줄은 `KeInitThread +0x486`. 그 갈래로 들어가는 조건은 `KeFeatureBits` 비트 `0x37` (`+0x2d3`).
2. 근거 `[정적]`: 갈래 안에서 쓰는 값 = `~(param_2+0x40) & KUSD+0x3d8 & 0x40000` (AMX 기능 비트만).
3. 그래서 사진이 0 인 까닭이 둘일 수 있다: (가) **갈래를 안 탔다**, (나) **탔는데 계산한 값이 0** 이었다.
4. 사진은 둘 다 0 으로 보인다. 정적으로는 못 가른다 → `[공백]`.
5. 전역 두 개 (`KeFeatureBits`, KUSD `+0x3d8`) 를 덤프에서 읽으면 닫힌다 → D1 (Q79).

- 비유: "해당자만 기입" 칸이 비어 있다. 해당이 아니어서 비었는지, 해당이었는데 적을 값이 0 이었는지 카드만 봐서는 모른다.
- 그래도 짐 지도에선 정확으로 셌다. 줄과 조건은 `[정적]` 이고, 탔는지만 동적이라서다 (run50 `MitigationFlags` 후보 ⓑ 와 같은 셈).

#### 그 밖에 안 타는 갈래 (run 문서)

- 보안 스레드 (플래그 `0x10`, `PspAllocateThread +0xba7` 가 `SecureProcess` 의 시스템 스레드에만), 복제·피코 (`+0x5b3 → +0x2ca9a9` — test.exe 는 TEB 를 새로 만듦, run44 `[관찰]`), AMX 따로 스택 (`KiAllocateXStateStack`), GUI 스레드 (만들기 길에 없음, `PsConvertToGuiThread` 범위 밖 `[추측]`).

### 2.9 핵심 (3): "아무도 안 씀" 21 · "범위 밖 길" 70 — 그리고 이 둘의 한계

#### 대표

- **아무도 안 씀 21** (만들기 길 안): `Spare6`·`Spare13`·`Spare18`·`Spare24`·`Spare26`·`Spare27`·`Padding` 같은 예비 칸, `SListFaultAddress`·`SListFaultCount`, `RelativeTimerBias`, `QueueListEntry`, `LegoData`, `InGlobalForegroundList` 등 (전체 목록은 run 문서).
- **범위 밖 길 70** `[추측]` (run 문서 표에서 몇 줄):

| 칸 | 쓰는 곳 (범위 밖) `[추측]` | 어떤 길 |
|---|---|---|
| `ThreadLock` (KTHREAD `+0x40`) | `KiBeginThreadWait`, `KeWaitForMultipleObjects` 등 | 대기 (잡았다 놓음) |
| `KernelTime` · `UserTime` | `KeAccumulateTicks`, `KeUpdateTotalCyclesCurrentThread` 등 | 시간 셈 |
| `FreezeFlags` 4칸 | `KiFreezeSingleThread`, `KiThawSingleThread`, `KeForceResumeThread` | 얼림 |
| `Win32Thread` | `PsSetThreadWin32Thread` | GUI |
| `KernelApcDisable` · `CombinedApcDisable` | 크리티컬 리전 들어가기·나오기 (인라인) | 썼다 되돌림 (D3) |
| `SchedulingGroup` 등 5칸 | `KeStartThread` (Q78d4) | 출발 |

- 마지막 줄: `KeStartThread` 는 Q78d4 몫이다. 이 5칸 (`SchedulingGroup`·`HardwareFeedbackClass`·`UserAffinityPrimaryGroup`·`AffinityPrimaryGroup`·`ThreadTimerDelay`) 은 지금 "범위 밖 길" 로 셌지만, Q78d4 가 `KeStartThread` 줄을 읽으면 "nt 가 씀" 으로 옮길 수 있다 (run 문서).

#### 둘 다 확실한 것 하나 `[정적]`

- 만들기 길 함수 (`PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`) 의 `.asm` 에서 스레드 포인터 (`RSI`·`RDI`·`R12`·`RBX`) 로 쓰는 줄을 **전부** 봤는데, 이 91칸을 쓰는 줄은 없다. 도우미 callee 쓰기도 안 건드린다.
- 그래서 "**초기화 (만들기) 동안 쓰는 줄이 없다**" 는 둘 다 `[정적]` 이다 (run 문서).

#### 둘을 가른 근거는 약하다

- 21 과 70 을 가른 건 **커널 전체를 칸 오프셋 숫자로 훑은 결과**다 (`objdump -d`, `.asm` 없음, 임시 스크립트). 이 방법엔 구멍이 둘 있다.

**구멍 1 — 잘못 걸림 (오탐)**

- 숫자만 같으면 걸린다. 그 레지스터가 **어느 구조체**를 가리키는지는 명령만 봐선 모른다.
- 실제 예 (run 문서): `RelativeTimerBias +0xf8` 을 쓰는 줄이 걸렸는데, `ExpSetTimerObject` 의 **타이머 객체** 칸이었다. 그래서 이 칸은 "아무도 안 씀" 으로 갔다.
- 비유는 [run50 해설 2.5](run50-eprocess-create-해설.md#25-새-말-오프셋-훑기의-오탐) 그대로: "12쪽 셋째 줄" 에 적는 손을 동네 서류에서 다 모았더니, 다른 서류의 12쪽 셋째 줄도 섞였다.
- 그러니 "범위 밖 길" 70 중에도 **사실은 다른 구조체 칸**이었던 줄이 섞여 있을 수 있다.

**구멍 2 — 못 잡음 (놓침)**

- 훑기는 `[레지스터+칸오프셋]` 꼴로 **칸 번호를 직접 부르는** 쓰기만 잡는다. 이런 건 빠진다 (결과서 정정 10):
  - `LEA` 로 칸 **주소**를 먼저 만들고, 그 포인터로 쓰는 줄 (리스트 삽입 등).
  - `SHL [mem]`·`SHR [mem]`·`ROL [mem]` 같은 이동 명령.
- 실제 후보: `QueueListEntry` 는 "아무도 안 씀" 으로 셌지만, `KeRemoveQueueEx` 가 리스트에 매달 수 있다 `[추측]`.
- `KernelApcDisable`·`CombinedApcDisable` 도 인라인 크리티컬 리전 줄은 훑기에 안 잡힐 수 있다 → D3.
- 비유: 칸 번호를 직접 부르지 않고, **"이 칸 자리" 를 쪽지에 적어 다른 사람에게 건네서** 그 사람이 적는 경우. 손 모으기를 "12쪽 셋째 줄" 로만 했으니 이 손은 안 잡힌다.

**그래서**

- 오탐이면 "범위 밖 길" 이 너무 많고, 놓침이면 "아무도 안 씀" 이 너무 많다. 두 묶음 사이 **경계가 약하다**.
- 단단하게 하려면 쓰는 함수를 `.asm` 로 한 줄씩 확인해야 한다 → 작은 정적 (Q100 의 정적 대안). 결과서 제안 `field_writers.py` 가 있으면 쉬워진다 (2.16).
- 위의 `[정적]` 한 줄 ("초기화 동안 쓰는 줄은 없다") 은 훑기가 아니라 만들기 길 함수 `.asm` 을 직접 본 결과다 (run 문서). 그래서 run 문서는 두 묶음의 **경계**만 약하다고 했다.
  - 그래도 그 `.asm` 확인을 실행으로 한 번 더 보자는 질문이 하나 있다: D3 (`KernelApcDisable`·`CombinedApcDisable` — 인라인 크리티컬 리전 줄).
  - 내 짐작 `[추측]`: 짐 지도 목표 (초기화 동안 세팅되는 짐) 에서 보면 두 묶음은 "초기화 동안엔 0 그대로" 라는 점이 같다. 그래서 경계가 흔들려서 바뀌는 건 주로 "나중에 누가 쓰나" 라는 덧붙인 정보 쪽일 것 같다. 이유: 두 묶음을 가른 기준이 바로 그 "나중에 쓰는 함수가 보이나" 였기 때문이다.

### 2.10 함정: K1 사진의 "옛 객체"

#### 무엇이 이상했나

- run44 는 K1 (`PspAllocateThread` **입구**) 에서도 이 스레드 주소의 메모리를 사진으로 갖고 있다.
  - 사진은 멈춘 순간의 VM 메모리 전체라서 ([run44 해설 2.2](run44-baggage-snap-해설.md)), 같은 주소를 K1 사진에서도 읽을 수 있었다.
- 그런데 K1 은 `ObCreateObjectEx` (`+0x2e2`) **전**이다. 이 스레드 객체가 **아직 없다**.
- 그 자리에 있던 값은 이 주소를 **앞서 쓰던 다른 스레드**의 것이었다 (run 문서·결과서 "사진 주의"):
  - `Process` 가 다른 EPROCESS (`…714c080`) 를 가리킨다. test.exe 의 것 (`…908b080`) 이 아니다.
  - 스택 값도 다르다.

#### 새 말: 옛 객체

- **옛 객체** = 메모리 자리가 재사용될 때, 앞 주인이 남긴 내용이 아직 지워지지 않고 남아 있는 것.
  - 커널은 돌려받은 메모리를 바로 지우지 않는 경우가 많다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 그래서 새 주인이 받은 뒤 직접 지운다. 이번엔 `+0x307 memset` 이 그 지우개다 `[정적]`.
  - 왜 하필 그 자리에 앞 스레드가 있었는지는 안 본다 (D4, 판정에 안 씀).
- 비유: 새 직원 업무 카드를 꽂을 **카드 꽂이 칸**을 미리 들여다봤더니, 퇴사한 직원의 카드가 아직 꽂혀 있었다. 새 카드는 그 뒤에 지우개로 밀고 다시 쓴다.

#### 왜 함정인가

- K1 값을 그대로 믿으면 엉뚱한 결론이 나온다. 예 (짐 지도 3.14):
  - `MiscFlags.CetUserShadowStack` 이 **K1 에선 1** 이다. 이걸 보면 "test.exe 첫 스레드는 사용자 CET 가 켜져 있다" 고 읽기 쉽다.
  - 실제로는 **옛 객체의 비트**다. K2 부터 0 이고, 코드로도 test.exe 는 사용자 CET 갈래를 안 탄다 (2.8).
- K1 에서 0 이 아니던 끝까지 0 칸은 5개다: `MiscFlags.CetUserShadowStack`, `SpecCtrl.RunningNonRetpolineCode`, `SpecCtrl`, `BamQosLevel`, `QueueListEntry`. 모두 K2 부터 0.

#### 그래서 어떻게 셌나

- `_KTHREAD`·`_ETHREAD` 의 `K1~K2` 는 사실상 "**K2 에 처음 값이 보이는 칸**" 이다.
- 끝까지 0 판정은 **K2 ~ P2 일곱 장** 기준이다 (여덟 장 아님).
- run44 숫자는 안 바뀐다. run44 가 이미 K1 의 스레드 쪽 메모리를 "스레드 생기기 전 메모리, 쓰레기" 로 빼고 셌기 때문이다 (run44 문서). 이번엔 그 까닭이 **옛 객체**라는 것을 구체적으로 본 것이다. 짐 지도 2.1 아래에 한 줄 붙었다.
- 옛 객체 잔재 없이 "`KeInitThread` 가 막 쓴 카드" 만 보고 싶으면 D2 (`PspAllocateThread+0x6f9` 에서 `dt nt!_KTHREAD @rsi`) 다. 다만 K2 값이 이미 코드와 칸마다 맞아서 꼭 필요하진 않다 (낮은 우선순위, Q100).

### 2.11 시작 정보 묶음: 칸 자리를 셌다

- `KeInitThread` 의 둘째 인자 = `PspAllocateThread` 가 `[RSP+0x120]` 부터 채운 **시작 정보 묶음** (`+0x679 ~ +0x6e9`). run11 해설의 "첫 출근하면 이 창구부터" 쪽지가 이 묶음의 `+0x08` 칸이다.
- PDB 타입이 없어서, 칸 자리는 **`KeInitThread` 가 쓰는 자리로 셌다** `[정적]` (이름은 결과서가 붙인 것). 대표만:

| 자리 | 뜻 | test.exe |
|---|---|---|
| `+0x00` | 이미 있는 커널 스택 (0 = 새로 만듦) | 0 → `MmCreateKernelStack` 탐 |
| `+0x08` | 시작 루틴 | `PspUserThreadStartup` (run11) |
| `+0x20` | **`ThreadContext`** — 0 이면 시스템 스레드 | 0 아님 |
| `+0x28` | TEB (`MmCreateTeb` 결과) → `+0x1e4` `Teb` 칸 | TEB `9b2fa67000` (run44) |
| `+0x30` | 프로세스 | test.exe EPROCESS |
| `+0x3c` | 플래그 (비트 0·1·4) | 0 |
| `+0x40` | 확장 기능 마스크 | — (D1) |

- 비유: 접수 창구가 팀장에게 건네는 **인수인계 봉투**. 봉투 칸마다 이름표는 없지만, 팀장이 "봉투 셋째 칸을 꺼내 여기 적는다" 는 걸 보면 그 칸이 무엇인지 안다.
- flow.md 2.1 `KeInitThread` 절의 앞 바퀴 줄 ("칸 이름(SystemRoutine, Teb 등)은 알려진 인자 순서에 맞춘 것이라 `[추측]`") 은 그대로 있고, 그 아래에 run52 `[정적]` 칸 자리 줄이 붙었다. 묶음 칸에 PDB 이름이 없다는 사정은 그대로라서 둘이 부딪히지 않는다.

### 2.12 부모 스레드 칸은 새 스레드 짐이 아니다

#### 무엇이 섞일 뻔했나 `[정적]`

- `PspAllocateThread` 는 새 가게 땅에 TEB·스택을 쓰려고 `KiStackAttachProcess` 를 부른다 (`+0x78d`, 그리고 `MmCreateTeb +0x7a`·`PspSetupUserStack +0x70`). → [남의 땅에 잠깐 들어가기](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 이 함수는 `GS:[0x188]` (`KiStackAttachProcess +0xe`) 로 **지금 도는 스레드**를 잡는다. 지금 도는 건 **부모 cmd 스레드**다.
- 그래서 이 함수가 썼다 되돌리는 `ApcState`·`ApcStateIndex` (`+0x162`)·`MiscFlags.ProcessDetachActive` (`+0x184` → `+0x238`) 는 **부모 업무 카드**의 칸이다.
- `KeAbPreAcquire +0x52` (`ThreadFlags` 비트 16)·`KxContextToKframes +0x3ba` (`Header` 비트 24) 도 호출한 쪽 (부모) 스레드 칸이다.

#### 그래서

- 이 줄들은 짐 지도 3.14 (새 스레드) 에 **안 넣었다**.
- 비유: 접수 담당 (부모 cmd 직원) 이 새 가게에 들어갈 때 **자기 카드**에 "출장 중" 도장을 찍었다가 돌아와서 지운다. 같은 칸 이름이지만 새 직원 카드가 아니다.
- 같은 함정을 이름으로도 피했다: 칸 오프셋 훑기에서 `ApcStateIndex` 를 쓰는 곳 (`KiStackAttachProcess +0x162` 등) 은 "범위 밖 길 — 부모 스레드 쪽" 으로 적혀 있다.

#### 같은 이름 다른 칸: `ThreadLock`

- 이번 `ThreadLock` = **KTHREAD `+0x40`**, 스케줄러 스핀락. 끝까지 0, 범위 밖 길 (대기 코드가 잡았다 놓음).
- 짐 지도 3.13 의 `ThreadLock` 두 줄 (`PspAllocateThread +0x51c`, `PspInsertThread +0x499`) = **ETHREAD `+0x590`** 푸시락. run11 해설의 "작성 중 자물쇠" 다.
- 이름은 같지만 **다른 칸**이다. 3.13 줄은 틀리지 않았다 (오프셋이 `+0x590`). 두 절 머리에 메모만 달았다.
  - **스핀락** = 잡을 때까지 그 자리에서 계속 다시 시도하며 기다리는 자물쇠로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 걸쇠 (푸시락) 와 달리, 문 앞에서 **손잡이를 계속 돌려 보는** 자물쇠.

### 2.13 간접 호출은 없다

- 만들기 길 함수 `.asm` **14개**에서 `CALL reg`·`CALL [mem]`·`JMP reg` 를 찾았더니 **0개** `[정적]`. → Q35 와 무관.
- `KeInitThread` 의 `INT 0x29` 하나 (`+0x1b7`) 는 연결 리스트 망가짐 검사 (fastfail) 다.
- 함수 주소를 **값으로** 넘기는 줄은 있다 — `KeInitializeApc` 에 멈춤 쪽지 내용을 적으려고:
  - `+0x12d` `KiSchedulerApc` (rva `0x2c8950`), `+0x139`·`+0x145` 하는 일 없는 빈 함수 (rva `0x4a10c0`, 같은 주소 이름 100개).
  - 둘째 `KeInitializeApc` (`+0x368`) 는 `KiUpdateTebApc` (rva `0x312820`, ETHREAD 칸 — Q78d2).
  - 이건 쪽지에 "펼치면 이 일을 하라" 를 **적어 두기만** 하는 것이다. 이 스레드 만들기 길에서 부르지는 않는다 (run13 해설 2.3 의 쪽지).

### 2.14 정정 (run 문서 "정정")

| 무엇 | 전 | 후 |
|---|---|---|
| 짐 지도 3.14 `Teb` | 짐작 (칸 이름 `[추측]`, run11) | **정확** — `KeInitThread +0x1e4` (2.2 ③) |
| `KeInitThread +0x172` 의 callee | 요청서에 `KeInitializeTimerEx` | **`KeInitializeTimer`** (rva `0x45b9d0`). `Ex` 는 뽑아 놓기만 |
| 짐 지도 3.13 `Cid` 줄 | "`PsAssignThreadId` (호출 `+0x532`)" | `+0x532` 는 ret 자리, call 은 **`+0x52d`** (`_calls.tsv`). 바로 고침 |
| `ThreadLock` 두 이름 | — | KTHREAD `+0x40` ≠ ETHREAD `+0x590` (2.12). 메모만 |
| 사진 K1 의 스레드 칸 | "스레드 생기기 전 메모리" | 구체적으로 **옛 객체** (2.10). run44 숫자는 그대로 |

- 이전 run 문서 맨 위 정정 안내: **없음** (틀린 이전 run 없음 — 위는 보탬·짐 지도 안 표기).

### 2.15 정리: 비유로 한 번 더

1. 부모 가게 (cmd) 직원이 관리사무소 접수 창구 (`PspAllocateThread`) 에 와서 "새 가게 첫 직원 뽑아 주세요" 한다.
2. 창구가 카드 꽂이에서 빈 업무 카드를 받는다 (`ObCreateObjectEx`). 꽂이에는 퇴사한 직원 카드 흔적이 남아 있다 (K1 옛 객체).
3. 카드와 **별지 넷** (친화도 둘·락 소유 칸 표 둘, 그리고 에너지 칸) 을 **지우개로 한 번에 민다** (`memset`).
4. 창구는 별지 자리를 카드에 적고, "실시간 우선순위 바닥 `0x20`" 을 적고, 인수인계 봉투 (시작 정보 묶음) 를 채워 팀장에게 넘긴다.
5. 팀장 (`KeInitThread`) 은 체크박스 띠 두 줄을 **오려 내서 두세 칸 체크하고 통째로 다시 붙인다** (`+0x9d`, `+0xfd`). 나머지 35칸은 빈칸으로 새로 붙는다.
6. 기다림 줄 넷에 "이 직원" 을 적고, 멈춤 쪽지와 신호등을 넣고, 타이머를 단다.
7. 봉투에서 개인 안내판 주소를 꺼내 카드에 적는다 (`Teb`, `+0x1e4`).
8. 관리사무소 안 작업대 (커널 스택) 를 새로 받아 위·아래 끝을 적는다 (`MmCreateKernelStack`).
9. "해당자만" 칸들 — 시스템 스레드·보안 스레드·CET 그림자 스택 — 은 이 직원이 해당이 아니라 비운다.
10. 비서 (`KiInitializeContextThread`) 가 작업대 맨 위에 개인 짐 상자 자리를 잡고, "가게 쪽 직원 (`PreviousMode` 1)" 표시를 단다.
11. 출근 뒤에 다른 부서가 채울 칸 (근무 시간·얼림·GUI …) 과 아무도 안 쓰는 예비 칸은 빈칸 그대로 둔다.
12. 그동안 접수 담당 자신의 카드에는 "출장 중" 도장이 찍혔다 지워진다 — 새 직원 카드와 상관없다.

### 2.16 곁가지: 도구 메모

run 문서 "도구 메모" 에서 — 하네스 제안이라 오케스트레이터가 사용자에게 묻는다 (미결).

- **제안 1 `field_writers.py`** (sast/43·44 제안을 다시, 더 구체적으로): 이번에 더 필요했던 것 둘.
  - (a) **DWORD 안 비트별 분리** — `BTS`/`BTR`/`OR imm`/`AND imm` 즉시값을 풀어 "비트마다 쓰는 곳". 2.7 같은 표시 묶음 때문이다.
  - (b) **칸이 중첩 구조체의 시작일 때** (`SchedulerApc` = `_KAPC`, `Timer` = `_KTIMER`, `WaitBlock` = `_KWAIT_BLOCK[4]` …) 호출 지점의 `LEA RCX,[RDI+칸]` 과 callee 의 구조체 오프셋 쓰기를 짝짓기. 2.9 의 "구멍 2" 와 같은 뿌리다.
- 제안 2 (사소): `pdb_aliases.py --lookup` 이 이름 하나뿐인 주소의 그 이름도 알려 주기.
- `pdb_struct.py --expand` (앞 바퀴 제안) 는 이번에도 `MiscFlags`·`ThreadFlags`·`Header` 비트에 필요했다.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run52 없음 | run52 추가. "(run52) 정적 바퀴, K2 덤프 읽기 로그와 K2 ~ P2 일곱 장 다시 읽음. **K1 의 스레드 주소는 옛 객체 값이라 대조에 안 씀**" 한 줄 |
| 2.1 제목 | … run49, run50 | … **run52** |
| 2.1 트리 (`PspAllocateThread` 아래) | `+0x2e7 ObCreateObjectEx` 다음 바로 `+0x532` | `·` 줄 `+0x30c memset` (몸통 + 확장 영역 0) `[정적]`, "그림엔 없음" |
| 2.1 트리 (`KeInitThread` 아래) | `+0x301 KiInitializeContextThread` 만 | `·` 줄 `+0x177 KeInitializeTimer` (`Ex` 아님) · `+0x278 MmCreateKernelStack` · `+0x2c5 KeAbInitializeThreadState` `[정적]` |
| 2.1 `PspAllocateThread` 절 | 다섯 걸음 (run11) | + `[정적]` 몸통 0 채움 `+0x307 memset` 과 이 함수가 직접 쓰는 `_KTHREAD` 칸 · 부모 스레드 칸 주의 (`KiStackAttachProcess`) · ETHREAD·TEB 줄은 Q78d2·Q78d3 로 |
| 2.1 `KeInitThread` 절 — 시작 정보 묶음 | 칸 이름 `[추측]` (앞 바퀴) | 그대로 + 아래에 `[정적]` (run52) **칸 자리** (`+0x00` ~ `+0x40`, `KeInitThread` 가 쓰는 자리로 셈) |
| 2.1 `KeInitThread` 절 — 본문 | run13 의 `+0x152`·`+0x163`·`+0xed` | + `[정적]` (run52) 새 묶음: 칸 대부분을 이 함수가 씀 · 표시 묶음 통째 되쓰기 · `+0x172` = `KeInitializeTimer` · test.exe 가 탄 길 · 간접 호출 없음 |
| 2.1 `KiInitializeContextThread` | run13·run15·run16 의 시작 스택·트랩 프레임 줄 | + `[정적]` (run52) 새 문단: 이 함수가 쓰는 `_KTHREAD` 칸 (`IptSaveArea` 안 탐 · `NpxState` · `PreviousMode` · `InitialStack` 둘째 · `KernelStack` · `StateSaveArea`) + 트랩 프레임 `+0x187 memset` → `+0x198`·`+0x1f1` (Q78d2 몫) |
| 4장 | — | (해결됨) `_KTHREAD` 만들 때 칸 (Q78d1) 줄 · (run52 새로) 실행 확인 D1 → Q79, D2·D3 · (run52 새로) 다음 조각에 넘김 (Q78d2·Q78d3·Q78d4) |
| 4.1 | 시작 정보 `+0x681` LEA (run11) | + (run52 `[정적]`) 칸 자리 |
| 5장 | — | run52 줄 둘 |

- 그림 (mermaid) 은 안 바뀌었다. 새 단계·새 caller 가 없고, `PspAllocateThread`·`KeInitThread` 안 대표 지점이 늘었을 뿐이다 (run 문서 "그림").

태그 변화 대표:

- `_KTHREAD.Teb` (+0xf0): 칸 이름 짐작 (run11 `[추측]`, 값은 run44 `[관찰]`) → **`[정적]` 쓰는 줄 `KeInitThread +0x1e4`**.
- `KeInitThread +0x172` callee 이름: (적힌 곳 없음 / 요청서 `KeInitializeTimerEx`) → **`[정적]` `KeInitializeTimer`**.
- `_KTHREAD` 180칸: 모름 → `[정적]` 줄 (89) / 부재 (21) / `[추측]` (70).
- `ApcQueueable` 켜는 줄: 기본으로 켬 (run13) → 조건 `+0x3c` 비트 1 꺼짐 (`[정적]` 보탬).
- `ExtendedFeatureDisableMask`: 모름 → 줄·조건 `[정적]`, 탔는지 **`[공백]`** (D1).

### 짐 지도 `baggage.md` 에서 바뀐 것 (열째판)

- **3.14 `_KTHREAD` 를 다시 썼다**: 14 → **50줄**. 줄 끝 "**Q78d1 로 닫힘**". 머리에 "Q78d4 에서 볼 것" 메모 (`KeStartThread` 쓰는 줄).
- 3.13·3.15·3.3 머리에 다음 조각 메모 (Q78d2 `_ETHREAD`·트랩 프레임, Q78d3 TEB `SameTebFlags`).
- `_KTHREAD` = nt 99 + 안 씀 21 + 범위 밖 70 + 모름 47 = 237.
- 2.1 아래에 K1 옛 객체 한 줄. 7.4 `Teb` 줄 `(해결됨 → run52)`. 7.6 에 칸 표 없는 짐 후보 기록만 (확장 영역 `_KAFFINITY_EX`·`_KLOCK_ENTRIES`·에너지 칸, 커널 스택 안 XSAVE 영역·트랩 프레임·시작 프레임).
- 새 **7.15** = 이번 동적 질문 (D1 ~ D4).
- 2장 숫자는 2.3 표 그대로.

### 큰 그림에서 어떻게 이어지나

- **첫 직원 카드의 "만들 때" 몫이 다 열렸다.** run11 이 다섯 걸음 (누가 무엇을 부르나) 을 봤고, 이번엔 그중 업무 카드 칸 하나하나가 **어느 줄에서** 정해지는지 봤다.
- **"부모에서 온다" 는 이번엔 거의 안 보인다.** 프로세스 쪽 (run49·run50) 은 부모 cmd 것을 많이 베꼈다. 결과서 1.1 표의 값 출처를 보면 이번 카드는 상수·인자·전역·도우미 결과가 대부분이고, 프로세스 칸 값을 베끼는 줄은 `AutoAlignment` 비트 (`+0x83 ~ +0x95`, test.exe 0) 하나다. (`Process` 칸은 값을 베끼는 게 아니라 프로세스 **주소**를 적는 것이다.)
  - 오히려 조심할 건 반대 방향이다: 만드는 동안 **부모 스레드 카드**에 찍혔다 지워지는 칸이 섞이지 않게 (2.12).
- **전역 값이 갈림길을 정한다.** CET·XSAVE·IPT·스택 크기 같은 갈래는 커널 전역 (`KiKernelCetEnabled`·`KeFeatureBits`·`KiXSaveAreaLength`·`KeKernelStackSize` …) 이 정한다. 이 값들은 D1 로 덤프에서 한 번에 읽을 수 있다 (Q79).
  - 내 짐작 `[추측]`: 에뮬레이터에선 이 전역들이 "이 기계는 CET 꺼짐, XSAVE 크기 얼마" 같은 **기계 설정값**으로 들어가야 같은 갈래를 탈 것 같다. 이유: 같은 이름의 `KeInitThread` 가 같은 조건 줄로 갈래를 고르기 때문이다.
- 다음 조각 **Q78d2** 는 같은 직원의 인사 카드 나머지 (`_ETHREAD`) 와 출발 메모 (`_KTRAP_FRAME`) 다. 이번에 지나가며 본 줄을 넘겼다:
  - `PspAllocateThread` 의 ETHREAD 쓰는 줄 목록 (`CreateTime` `+0x4bf`, 자기 연결 리스트 머리 여덟, 락 칸 0 다섯, `Silo` −3 `+0x49f` …).
  - 트랩 프레임: **`KiInitializeContextThread +0x187 memset(트랩 프레임 = R15 − 0x190, 0x190)`** → `+0x198` `ExceptionActive` = 1 · `+0x1f1` `MxCsr` = `0x1f80`. 이 `memset` 이 트랩 프레임 끝까지 0 32칸을 가를 실마리다 (이번 `+0x307` 과 같은 역할).
- 그 뒤 **Q78d3** (TEB, `SameTebFlags` `+0x793`) → **Q78d4** (`KeStartThread`·스케줄러, 남은 47칸).

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 하는 조건은 dast-mentor 몫이다. **급한 것은 없다** — 이것들을 안 닫아도 3.14 칸 표는 (Q78d4 몫 47 빼고) 완성돼 있다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** 커널 전역 값: `KiKernelCetEnabled` · `KeFeatureBits` (비트 `0x37`) · `KiIptSaveAreaLength` · `KiSchedulerAssistThreadFlagEnabled` · `KiShortExecutionCycles` (기대 `0x4c4b4`) · `KeKernelStackSize` (기대 `0x7000`) · `KiXSaveAreaLength` · `KeEnabledSupervisorXStateFeatures` · KUSD `+0x3d8` | 실행 때 값. 지금은 사진 속 비트·차이로 거꾸로 센 것뿐 (`[관찰 간접]`) | 덤프 **Q79**: `dq`/`dd`/`db` 한 번씩. 조건부 12칸의 "왜 안 탔나" 와 **`ExtendedFeatureDisableMask` `[공백]`** 이 닫힘. `KeFeatureBits` 비트 `0x37` 은 run49 D5 와 같은 값 |
| **D2** `KeInitThread` 가 막 쓴 카드 한 장 (옛 객체 잔재 없이) | K1 사진은 옛 객체 (2.10) | 멈춤 **Q100** (낮음): `nt!PspAllocateThread+0x6f9` 에서 `dt nt!_KTHREAD @rsi`. 조건: 지금 도는 프로세스 = cmd.exe. K2 값이 이미 맞아 꼭 필요하진 않음 |
| **D3** `KernelApcDisable`·`CombinedApcDisable` 이 만들기 길에서 정말 안 쓰이나 | 인라인 크리티컬 리전 줄은 훑기에 안 잡힐 수 있음 (2.9 구멍 2). 썼다 되돌린 것도 사진엔 0 | 멈춤 **Q100** (낮음): 새 스레드 주소가 정해진 뒤 `ba w2 <스레드>+0x1e4` |
| "범위 밖 길" 70 · "아무도 안 씀" 21 의 쓰는 함수가 맞나 | 근거가 오프셋 숫자 훑기 (오탐·놓침, 2.9) | 작은 정적 — Q100 의 정적 대안 (`.asm` 로 한 줄씩). `field_writers.py` 가 생기면 쉬워짐 |
| 친화도 블록 비트맵이 채워지는 때 | 만들기 길에선 0 (`memset`) 그대로 `[공백]` | 정적 **Q78d4** (`KeStartThread` 쪽) |
| `_KTHREAD` 남은 47칸 (`K2~K3` 10 · `K3~P0` 9 · `P0~P1` 9 · `P1~P1a` 7 · `P1a~P1b` 6 · `P1b~P2` 6) | 이번 범위 밖 (만든 뒤 구간) | 정적 **Q78d4** — `KeStartThread` 쓰는 줄 목록 (`K2~K3` 10칸 중 8칸 + 범위 밖 길 5칸) 은 이미 넘김 |
| `ModeHistory` 틱마다 밀기 · `ExpectedRunTime` 갱신 | 이름만 봄 (`KeAccumulateTicks +0x58`, `KiSwapThread` 쪽) `[추측]` | 정적 **Q78d4** |
| `_ETHREAD` 나머지 · 트랩 프레임 | 이번 범위 밖 (구조체별 조각) | 정적 **Q78d2** (줄 목록 넘김) |
| TEB `SameTebFlags` (`PspAllocateThread +0x793`) | 이번 범위 밖 | 정적 **Q78d3** |
| 칸 표 없는 짐 (확장 영역 `_KAFFINITY_EX` 2 · `_KLOCK_ENTRIES` 2 · 에너지 칸, 커널 스택 안 XSAVE 영역 · 시작 프레임) | 체크리스트 21종 밖 | 짐 지도 7.6 에 **기록만** |
| **D4** K1 자리의 옛 객체가 왜 남아 있나 | 판정에 안 씀 | 안 함 |

- 대기열 순서 (plan): **Q78d2** → Q78d3 → Q78d4 → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q100 은 맨 뒤, 낮은 우선순위.

---

## 5. 더 읽기

- run 문서: [`run52-kthread-create.md`](run52-kthread-create.md) — 요약 표 (분류 뜻·한계), 환경 (값 대조), 호출 지점 표, 호출 관계 트리, 시작 정보 묶음 칸 자리, 몸통 0 채움, `K1~K2` 42칸, 끝까지 0 138칸 가르기 (조건부 표·범위 밖 길 표), test.exe 가 탄 길, 간접 호출, 부모 스레드 칸, 확인된 것, 정정, 아직 확인 필요 (D1 ~ D4, Q78d2·d3·d4 넘김), 도구 메모
- 결과서: [`_workspace/sast/46_kthread-create_result.md`](../../_workspace/sast/46_kthread-create_result.md) — 판단 기준 한 줄, 1.1 칸마다 표 (3장 양식), 2.1 ~ 2.4 끝까지 0 가르기, 3 간접 호출·입력, 4 줄기와 경로, 동적 질문, 넘길 것, 정정 1 ~ 10
- 요청서: [`_workspace/sast/46_kthread-create.md`](../../_workspace/sast/46_kthread-create.md)
- 짐 지도: [`baggage.md`](baggage.md) — 2장 (열째판 숫자) · 2.1 아래 run52 줄 · 3.13·3.15·3.3 머리 메모 · **3.14 `_KTHREAD`** · 7.2 · 7.4 · 7.6 · **7.15**
- 흐름도: [`flow.md`](flow.md) — 기준, 2.1 트리, `nt!PspAllocateThread` 절, `nt!KeInitThread` 절 (시작 정보 칸 자리, run52 묶음, `KiInitializeContextThread` 문단), 4장 run52 줄, 4.1, 5장
- 진행표: `_workspace/plan.md` Q78d · Q78d1 (끝) · Q78d2 · Q78d3 · Q78d4 · Q79 · Q100, 결정 기록 2026-10-30
- 앞 해설:
  - [run11](../02_sprint/run11-allocate-thread-해설.md) — 첫 직원 뽑기 다섯 걸음, 시스템 스레드 갈림길, CET
  - [run13](../02_sprint/run13-resume-thread-해설.md) 2.3 — 멈춤 쪽지·신호등을 `KeInitThread` 가 미리 만든다
  - [run15](../02_sprint/run15-user-thread-startup-해설.md) — 첫 커널 스택, 출발 메모
  - [run44](run44-baggage-snap-해설.md) 2.2 · 2.8 · 2.10 — 사진, 지점, 처음 값 구간
  - [run47](run47-section-chain-해설.md) 2.4 — 0 의 세 가지
  - [run50](run50-eprocess-create-해설.md) 2.4 · 2.5 · 2.13 — 범위 밖 길, 오탐, 같은 DWORD 되쓰기 (가게 장부 쪽)
