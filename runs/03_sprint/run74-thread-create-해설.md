# run74 thread-create — 해설

> 이 글은 [run74 문서](run74-thread-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **고쳐 읽기 (run90)**: 유저 쪽 `[추측]` 은 run90 에서 `[정적]` 이 됐다 — "한 줄로 먼저" 끝 · 1절 그림 · 2.2 줄 1 · 2.3 끝 · 2.4 "그다음은 run11 대로" 의 "유저 `CreateThread` 가 `NtCreateThreadEx` 로 오나 · `Worker` 가 그대로 가나", 2.5 (사) 와 4절 표 셋째 줄의 "(0, 0) 을 넘긴다고 **치면**" 은 맞다 (`kernel32!CreateThread +0x36` → `CreateRemoteThreadEx +0x2e2`, 스택 (0, 0) `+0x280 ~ +0x291`, 쪽지 늘 2장 `TotalLength 0x48` — kernel32 콘센트 → KERNELBASE 풀이만 `[추측]`). 2.5 (바) 의 "예약" `[추측]` 은 **예약만** (`MmAllocateUserStack` → `MiReserveUserMemory`, 시작 64KB 정렬), 같은 절 끝 "노드 보통 0 `[추측]`" 은 이상적 프로세서 속성이 있을 때만 노드 + 1 · test.exe 0 으로 닫혔다. 2.7 끝 "'둘만' 은 좁다" 는 커널 기준이고 `CreateThread` 길엔 딱 둘이다. 단 CreateFlags 는 "늘 0" 이 아니라 조건 셋에 따라 0 / 1 이라, 2.6 (바) 의 "`+0x2e0` 은 보통 안 설 것 `[추측]`" 은 그대로 `[추측]` 이다. 4절 표의 Q142 ④ · ⑤ 는 닫혔고 ③ 은 못 찾음 (→ Q168 ②). → [run90 해설](run90-thread-leftovers-해설.md) 2.3 ~ 2.11.

- 대기열: **Q141a**: PE_emulator 요청 25 의 ㄱ — 둘째 스레드 만들기 (커널) (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q141b** (넘기기·퀀텀) → Q141c (기다림·깨움) → Q141d (끝내기) → Q141e (남의 스레드 문맥). 바퀴마다 멈춘다.
  - 동적 몫은 **Q42** 에 출처로 더했다. 작은 정적 다섯은 새 **Q142** (맨 뒤). 4절에서 다룬다.
- 근거:
  - run 문서 [`run74-thread-create.md`](run74-thread-create.md) (가장 중요)
  - 요청서 ① [`_workspace/sast/69_thread-create.md`](../../_workspace/sast/69_thread-create.md) · 결과서 ① [`69_thread-create_result.md`](../../_workspace/sast/69_thread-create_result.md) — 만들기 길 전체 (판정 **분석성공**)
  - 요청서 ② [`_workspace/sast/70_thread-stack.md`](../../_workspace/sast/70_thread-stack.md) · 결과서 ② [`70_thread-stack_result.md`](../../_workspace/sast/70_thread-stack_result.md) — `RtlCreateUserStack` 하나 (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.1 트리 한 줄·세 절 + 새 절 하나 · 3장 줄 셋 + 새 절 하나 · 4장 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): 첫 스레드 스택 줄만 고침 (새 짐 칸 없음)
- 이번 바퀴는 **정적만**. 그런데 결과서가 **둘**이다.
  - sast/69 가 "크기 0 → exe 헤더" 하나를 못 닫았다. 사용자가 "이번 바퀴에 하나 더" 로 정해서, sast/70 이 이어서 닫았다. 둘을 run74 하나로 썼다.
  - 값 대조는 run44 때 찍어 둔 **첫 스레드** 사진으로만 했다. 둘째 스레드는 `main` 뒤라 사진 범위 밖이다.

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) · 첫 직원 뽑기 다섯 걸음 → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- 유저 스택 = 가게 안 **작업대**, TEB = 직원 **개인 안내판** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)
- CONTEXT = **업무 시작 메모** ("어디서부터 (`Rip`)", "처음 손에 쥘 것 (`Rcx`)") → [run11 해설 2.6](../02_sprint/run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다)
- CET 섀도 스택 = 책갈피 더미의 **사본** → [run11 해설 2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나)
- `RtlUserThreadStart` = **본업 자리**, `PspCreateUserContext` 가 첫 메모를 쓴다 → [run16 해설 2.4](../02_sprint/run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값)
- `PsNtdllExports` = 관리사무소 **벽 주소록** → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- 신청서 체크 칸 → 관리사무소 **내부 양식** (만들기 플래그), `0x60` = "자물쇠는 이미 잠가 왔음" 도장 → [run12 해설 2.5](../02_sprint/run12-insert-thread-해설.md#25-flags-는-어디서-오나--전제)
- `KeSuspendThread` = **대기 팻말**, `KeReadyThread` = **출근 대기 줄** (팻말 건 직원도 줄엔 선다) → [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나)
- 오프셋 순서 ≠ 실행 순서 ("부록 보고 오기") → [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)
- 목수 (`RtlCreateUserStack`) 가 작업대 경계를 **쪽지 (`INITIAL_TEB`)** 에 적어 안내판 담당에게 넘긴다 → [run54 해설 2.4](run54-teb-fill-해설.md#24-핵심-1-같은-안내판을-커널이-만들고-ntdll-이-채운다)
- TEB 가 처음부터 0 인 까닭 (공장에서 막 나온 **새 안내판**) → [run54 해설 2.5](run54-teb-fill-해설.md#25-핵심-2-왜-이번엔-몸통-memset-이-없어도-0-인가)
- `SameTebFlags` = 입사 서류 체크박스를 **안내판 표시 칸**으로 옮겨 적기, 첫 스레드는 늘 `0x400` → [run54 해설 2.8](run54-teb-fill-해설.md#28-sametebflags-첫-스레드는-늘-0x400-뿐)
- 예약 = 빈 땅에 **말뚝**, 확정(커밋) = 그 위에 **바닥 깔기** → [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정)
- IFEO = 관리사무소 서랍의 **가게 이름별 특별 지시** 폴더 → [run46 해설 2.6](run46-process-heap-해설.md#26-ifeo-가-이-구간에서-처음-열린다)
- 남의 땅에 잠깐 들어가기 (`KiStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 사진 지점 K2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)
- `memset` = **지우개** → [run54 해설](run54-teb-fill-해설.md) (run52·run53 부터)

### 한 줄로 먼저

- 둘째 직원은 다른 **신청 창구** (`NtCreateThreadEx`) 로 들어온다. 그래도 뒤에서 몸을 만드는 담당은 첫 직원 때와 **같다** `[정적]`.
  - 인사 카드·작업대·안내판: `PspAllocateThread` → `PspSetupUserStack` → `RtlCreateUserStack`, `MmCreateTeb`.
- 다른 건 **셋**이다 `[정적]`:
  1. **업무 시작 메모**를 창구가 자기 안에서 직접 쓴다 (`PspCreateUserContext` 안 부름). 양식·`Rip` 은 같고, 손에 쥘 것 두 칸만 다르다.
  2. **작업대 크기**는 신청서 값이 그대로 내려간다. 0 이면 맨 끝 `RtlCreateUserStack` 이 **0 인 쪽만** exe 헤더 값으로 메운다.
  3. **"첫 직원" 체크 (`0x40`) 가 절대 안 켜진다.** 그래서 `CreateFlags = 0` 이면 커널이 안내판 표시 칸 (`SameTebFlags`) 에 펜을 아예 안 댄다.
- test.exe 숫자: 예약 `0x100000` · 커밋 `0x1000` · 가드 `0x3000` — **첫 스레드와 같은 값** `[정적]`.
  - 단 `CreateThread(…, 0, …)` 가 커널에 정말 (0, 0) 을 넘기는지는 `[추측]` 이다. 유저 쪽을 안 봤다 (Q42).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드          (run9 ~ run16, run54)
 └─ test.exe 첫 스레드: ntdll 로더 … → NtContinue 은퇴 → 진입점 → main
      └─ main: CreateThread(NULL, 0, Worker, NULL, 0, NULL)       [소스] test.c:34
           → … → ntdll!NtCreateThreadEx (시스템 콜)                [추측] 유저 쪽 길은 안 봄
           → nt!NtCreateThreadEx → PspCreateThread → …            ← 이번
                (그 뒤 출발은 PspUserThreadStartup 쪽 — run11·run15, flow.md 3장)
```

- 흐름도 (`flow.md` 그림) 의 범위는 **진입점까지**다. 둘째 스레드는 `main` 안에서 만들어지니 범위 **밖**이다.
  - 그래서 그림엔 안 넣고, flow.md 3장에 절 하나로만 적었다.
- 그럼 왜 봤나?
  - `../PE_emulator` 가 둘째 스레드까지 돌려 보면서, 근거 없이 `[추측]` 으로 둔 자리가 있었다.
  - 그걸 닫아 달라는 요청 (요청 25) 이 왔고, 사용자가 "맨 앞, 바로 시작" 으로 정했다 (plan.md).

### 무엇이 궁금했나

한 문장으로: **둘째 스레드를 만드는 커널 길은 첫 스레드 길과 어디서 같고 어디서 다른가.**

질문은 이렇게 쪼갰다 (요청서를 줄임):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 69-1 | `NtCreateThreadEx` → `PspCreateThread` → … 부르는 순서 | 2.1 · 2.2 |
| 69-2 | 새 스레드 첫 문맥 (`Rip`·`Rcx`·`Rdx`, `PspCreateUserContext` 를 쓰나), 스택 (크기 0 이면?) | 2.4 · 2.5 |
| 69-3 | `CreateFlags` 비트 → 내부 플래그 → `SameTebFlags`, `CREATE_SUSPENDED` | 2.6 |
| 69-4·5 | 속성 목록 검사 (누가·언제·모르는 것·나쁜 출력 주소), `PspBuildCreateProcessContext` | 2.7 |
| 70 | `RtlCreateUserStack` 안 — "크기 0 → exe 헤더" 와 다듬기·놓는 꼴 | 2.5 |

### 바퀴 전에 알던 것

- `[정적]` run9 · run16: 첫 스레드의 업무 시작 메모는 `NtCreateUserProcess +0xd22` `PspCreateUserContext` 가 쓴다.
  - `Rip` = `RtlUserThreadStart` (`PsNtdllExports+0x10`), `Rcx` = exe 진입점, `Rdx` = PEB.
- `[정적]` run11: `PspAllocateThread` 가 `CONTEXT.Rip` → `StartAddress`, `Rcx` → `Win32StartAddress` 로 옮겨 적는다 (`+0x640 ~ +0x655`). 작업대 `+0x5ea`, 안내판 `+0x62c`.
- `[정적]` run12: `PspInsertThread` 를 부르는 곳은 `NtCreateUserProcess`·`PspCreateThread`·`PspCreatePicoThread` 셋.
- `[정적]` run54: `SameTebFlags` 는 `PspAllocateThread +0x54a ~ +0x5a5` 에서 값을 만들고 `+0x793` 에서 통째로 쓴다. 첫 스레드는 늘 `0x400`.
- `[추측]` run11: 일꾼 스레드들과 `test!Worker` 는 `PspCreateThread` → `PspAllocateThread` 길일 것.
- `[추측]` run16: 다른 스레드도 `Rip = RtlUserThreadStart` 일 것.
- `[추측]` PE_emulator: 둘째 스레드도 첫 스레드 사슬 (`MmCreateTeb`·`RtlCreateUserStack`) 재사용, 스택 크기 0 → exe 헤더.
- `[관찰]` run44 K2: 첫 스레드 TEB 의 스택 칸 값 (2.5 끝에서 다시 본다).

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다. 오프셋은 **돌아올 자리 (ret)** 다.

```
NtCreateThreadEx                                   (인자 11개, 이름은 [추측])
 │  +0x70   CreateFlags 검사 → 어기면 0xC00000F5         (맨 먼저)
 ├─ (1) +0xe3   PspBuildCreateProcessContext(…, 1, …)   속성 목록 ≠ NULL 일 때만    ★ 2.7
 ├─ (2) +0x12c  ObpReferenceObjectByHandleWithTag      프로세스 잡기
 │  +0x154 ~ +0x175  스택 주문서 ← 사용자 크기 그대로                              ★ 2.5
 │  +0x1d8 ~ +0x21c  업무 시작 메모를 직접 → Rip · Rcx · Rdx                       ★ 2.4
 ├─ (3) +0x282  ObfDereferenceObjectWithTag
 └─ (4) +0x2d2  PspCreateThread
      ├─ (1) +0x283  PspAllocateThread          ← 첫 스레드와 같은 함수            ★ 2.3
      │    │  +0x66f/+0x673  내부 플래그가 0 이면 +0x793 건너뜀                     ★ 2.6
      │    ├─ (1) +0x5ef  PspSetupUserStack
      │    │    └─ +0x8e  RtlCreateUserStack    크기 0 → exe 헤더, 예약·커밋·가드  ★ 2.5
      │    └─ (2) +0x631  MmCreateTeb
      └─ (2) +0x2ea  PspInsertThread
           ├─ +0x93   KeAbPreAcquire     프로세스 락을 직접                        ★ 2.6
           ├─ +0x2e5  KeSuspendThread    CreateFlags 0x1 일 때만
           └─ +0x5a6  KeReadyThread      늘
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- `NtCreateThreadEx`·`PspCreateThread` 는 이번에 **처음 뽑은** 함수다. 새 지도 `_map/NtCreateThreadEx_*` (함수 2620개) 도 이번에 만들었다.
- 순서 `[정적]`:
  - `NtCreateThreadEx` 직접 호출 12줄 중 **10번째**가 `PspCreateThread`.
  - 그 안 11줄 중 **5번째** `PspAllocateThread`, **7번째** `PspInsertThread`.
  - 정상 길에서 각각 한 번씩.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `└─ (4) +0x2d2 PspCreateThread`

- **누가**: `NtCreateThreadEx`.
  - 가게 안 직원 (`main`) 이 "직원 한 명 더" 신청서를 들고 온 **신청 창구**다.
  - 유저 쪽이 정말 이 창구로 오는지는 `[추측]`.
- **어디서**: 부르는 자리 `+0x2cd`, 돌아올 자리 `+0x2d2`.
  - 실행 중 멈추면 콜스택에 `nt!NtCreateThreadEx+0x2d2` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `PspCreateThread` (인자 13개).
- **무엇을 위해**: 직원을 **실제로 만든다** `[정적]`.
  - 앞 9줄은 준비다 — 지우개 (`memset`), 속성 쪽지 검사, 프로세스 잡기, 업무 시작 메모 쓰기, 프로세스 놓기.
  - 진짜 몸 만들기는 이 안에서 `PspAllocateThread` → `PspInsertThread` 로 한다.

#### 줄 2: `│ +0x66f/+0x673 내부 플래그가 0 이면 +0x793 건너뜀`

호출이 아니라 **갈림길** 줄이다.

```
PspAllocateThread +0x66f  TEST R12W,R12W
PspAllocateThread +0x673  JNZ  +0x780
```

- **누가**: `PspAllocateThread`.
- **무엇을 보나**: `R12W` = 안내판 `SameTebFlags` 에 쓸 16비트 값. 바로 앞 `+0x54a ~ +0x5a5` 에서 내부 플래그로 계산해 둔 값이다 (run54).
- **갈림**:
  - 0 이 **아니면** → `+0x780` 쪽으로 간다 → `+0x793` 에서 TEB `+0x17ee` 에 16비트를 통째로 쓴다.
  - 0 **이면** → 그쪽으로 안 간다 → **안 쓴다.**
- **무엇을 위해**: "쓸 게 없으면 펜을 안 댄다". 이 두 줄이 2.6 의 결론 ("둘째 스레드는 커널이 `SameTebFlags` 를 안 쓴다") 의 근거다 `[정적]`.

### 2.3 같은 점 — 몸 만드는 담당은 그대로

먼저 **같은** 것부터. 이번 결과의 바탕이다.

| 무엇 | 첫 스레드 | 둘째 스레드 | 같나 |
|---|---|---|---|
| 몸 만들기 함수 | `PspAllocateThread` (`NtCreateUserProcess +0xe57/+0xe5c` 가 부름) | `PspAllocateThread` (`PspCreateThread +0x27e/+0x283` 가 부름) | **같은 함수** |
| 작업대 | `PspAllocateThread +0x5ea` → `PspSetupUserStack` → `RtlCreateUserStack` | 같은 자리 | **같음** |
| 안내판 | `PspAllocateThread +0x62c` `MmCreateTeb` | 같은 자리 | **같음** |
| 등록·대기 줄 | `PspInsertThread` (`NtCreateUserProcess +0x1015/+0x101a`) | `PspInsertThread` (`PspCreateThread +0x2e5/+0x2ea`) | 같은 함수, 부르는 곳만 다름 |
| `CONTEXT.Rip` | `PsNtdllExports+0x10` (`+0xd10`) | 같은 칸 (`+0x1bf`) = `RtlUserThreadStart` | **같음** |
| 메모 양식 (세그먼트·`MxCsr` …) | `PspCreateUserContext` | 같은 값 세트 | **같음** (CET 일 때 하나만 다름, 2.4) |
| 작업대 크기 다듬기·놓는 법 | `RtlCreateUserStack` | 같은 함수 | **같음** (들어오는 크기만 다름, 2.5) |

- 모두 `[정적]` (run 문서 "첫 스레드 길과 비교").
- 비유:
  - 첫 직원은 **가게 개업 신청** (`NtCreateUserProcess`) 때 같이 뽑혔다.
  - 둘째 직원은 이미 일하는 직원이 **"한 명 더" 신청서** (`NtCreateThreadEx`) 를 낸 것이다.
  - 창구는 다르다. 하지만 뒤에서 인사 카드를 만들고, 작업대를 짜고, 안내판을 거는 **담당은 같은 사람**이다.
- 그래서 PE_emulator 의 "첫 스레드 사슬 재사용" 은 **맞다** (run 문서 표 69-2(d)).
- run11 의 `[추측]` "`test!Worker` 는 `PspCreateThread` → `PspAllocateThread` 길" 은 **커널 쪽은** 코드로 맞았다 `[정적]`.
  - 유저 `CreateThread` → `NtCreateThreadEx` 는 그대로 `[추측]`.

이제 **다른** 셋을 하나씩 본다.

### 2.4 다른 점 ① — 업무 시작 메모를 창구가 직접 쓴다

#### `PspCreateUserContext` 를 안 부른다 `[정적]`

- `NtCreateThreadEx` 의 호출 12줄 어디에도 없다. `PspCreateThread` 에도 없다.
- 새 지도 `_map/NtCreateThreadEx_*` (깊이 10까지, 2620개) 에 이 함수가 **아예 없다**.
- 이 함수를 부르는 곳은 커널 전체에서 `NtCreateUserProcess +0xd1d` **하나뿐**이다 (`callers_total` 1).

#### 대신 `NtCreateThreadEx` 가 자기 안에서 쓴다 `[정적]`

1. `+0x1d3` `RtlGetExtendedContextLength2` — 메모 길이 재기
2. `+0x1f3` 스택 탐침 — 스택에 메모 자리 잡기
   - 이 callee 는 `_calls.tsv` 에 별칭으로만 나온다. 후보 이름 `__chkstk`·`_alloca_probe`.
3. `+0x205` `memset` — 메모를 지우개로 비우기
4. `+0x217` `RtlInitializeExtendedContext2` — `ContextFlags` 채우기
5. `+0x220 ~ +0x26b` — 칸 쓰기

쓰는 칸:

| 칸 | 첫 스레드 (`PspCreateUserContext`) | 둘째 스레드 (`NtCreateThreadEx`) |
|---|---|---|
| `Rip` (+0xf8) | `RtlUserThreadStart` | **같음** — `+0x1bf` 에서 `PsNtdllExports+0x10` 을 읽고 `+0x220` 에 씀 |
| `Rcx` (+0x80) | exe 진입점 | **사용자 시작 함수** (StartRoutine, `+0x22b`) |
| `Rdx` (+0x88) | PEB | **인자** (Argument, `+0x236`) |
| 세그먼트·`MxCsr`·`ControlWord` | `SegCs 0x33` 등 | **같음** (`SegCs 0x33` `+0x254` 등, run 문서) |
| `ContextFlags` | `0x10000b` 고정 | `0x10000b`. 단 프로세스에 CET 섀도 스택 (`MitigationFlags2` +0x754 비트 14) 이 켜져 있으면 `0x40` 을 더하고 확장 마스크 `0x800` 을 붙임 (`+0x196 ~ +0x1bc`) |

- 차이는 `ContextFlags` 의 CET 갈래 **하나뿐**이다 `[정적]`.
- 인자 이름 (StartRoutine, Argument) 은 커널 PDB 에 함수 원형이 없어서, 널리 알려진 순서에 맞춘 것이다 `[추측: 이름]`. 자리는 `[정적]`.

#### 그다음은 run11 대로

- `PspAllocateThread +0x640 ~ +0x655` 가 메모를 인사 카드로 옮겨 적는다 (run11).
- 그래서 둘째 스레드는 `StartAddress` = `RtlUserThreadStart`, `Win32StartAddress` = 사용자 시작 함수 `[정적]`.
- test.exe 라면 그 시작 함수는 `Worker` 일 것이다.
  - `CreateThread` 셋째 인자가 `Worker` 다 `[소스]`.
  - 그 값이 그대로 StartRoutine 으로 가는지는 유저 쪽을 안 봐서 `[추측]`.
- run16 의 `[추측]` "다른 스레드도 `Rip = RtlUserThreadStart`" 는 이 길에선 `[정적]` 이 됐다.

#### `Rsp` 는 여기서 안 쓴다

- 첫 스레드와 같다. `PspSetupUserStack +0xdc` 가 쓴다 `[정적]`:
  - `StackBase − ((ExGenRandom(1) & 0x7f) << 4) − 0x28`
- `EPROCESS.MitigationFlags` (+0x750) 비트 6 이 켜져 있으면 랜덤 몫이 0 (`+0x9f/+0xa1`).

#### 비유로

- 첫 직원은 **메모 작성 담당** (`PspCreateUserContext`) 이 업무 시작 메모를 써 줬다.
- 둘째 직원은 **창구 직원이 같은 양식에 직접** 쓴다.
- 양식도, "본업 자리" 칸도 똑같다. 다른 건 "처음 손에 쥘 것" 두 칸뿐이다.
  - 첫 직원: 가게 입구 번지 (진입점) + 가게 안내판 (PEB).
  - 둘째 직원: 신청서에 적힌 **업무 함수** + 그 함수에 줄 **인자**.

#### 조건 하나 — 보안 프로세스면 다른 길

- 이 메모는 프로세스 `SecureProcess` (`+0x170` 비트 0) 가 **0 일 때만** 만든다 (`+0x138`·`+0x13f JNZ`) `[정적]`.
- 1 이면 메모 없이 `PspCreateThread +0x366` 이 내부 플래그 `0x400` 을 켜고 다른 길로 간다 `[정적]`.
- test.exe 는 일반 프로세스라 앞쪽 길일 것 `[추측]` (Q42 동적 6).

#### PE_emulator 에게

- `PspCreateUserContext` 를 재사용해도 **레지스터 값은 같게** 나온다.
- 다만 커널 **이름·호출관계는 다르다** — 이 길에선 `NtCreateThreadEx` 안이다 (run 문서 69-2(b)).
- "`Rip` = 시작 주소 그대로" 로 하고 있다면 **틀림** (69-2(c), 확인용 한 줄).

### 2.5 다른 점 ② — 작업대 크기: 신청서 값 그대로, 0 이면 exe 헤더

이번 바퀴에서 제일 긴 이야기다. 천천히 가자.

#### 새 말

- **예약 / 커밋** — [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정) 에서 푼 말이다. 한 줄로 다시:
  - 예약 = 빈 땅에 **말뚝**만 박아 두기 (주소 범위만 "내 자리").
  - 커밋 (확정) = 말뚝 박은 땅 일부에 **바닥을 깔아** 실제로 쓰게 하기.
- **가드 페이지** = 보호값에 `PAGE_GUARD` 가 붙은 페이지. 처음 건드리면 운영체제가 알아채는 **표시 페이지**로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 스택이 아래로 자랄 때 다음 칸을 이어 붙이는 데 쓰인다고들 한다 `[추측]` (일반적으로 알려진 설명).
  - 비유 (flow.md 비유를 그대로): 바닥 깐 곳 **바로 아래**에 친 **"여기 밟으면 알려 줘" 줄**.
- **스택 정보 (StackInfo)** = `NtCreateThreadEx` 가 만들어 `PspCreateThread` → `PspAllocateThread` 로 그대로 넘기는 0x20 바이트 칸 `[정적]` (결과서 ①):
  - `+0` 바이트 표시 (비트 0 = "이미 있음"), `+8` ZeroBits, `+0x10` 커밋 크기, `+0x18` 예약 크기.
  - 비유: 목수에게 넘기는 작업대 **주문서**. "바닥은 얼마, 말뚝은 얼마".
- `INITIAL_TEB` 는 run54 해설의 그 **쪽지**다. 목수가 작업대 경계 세 칸을 적어 `MmCreateTeb` 에 넘긴다.

#### (가) 크기가 내려가는 길 — 중간에 아무도 손대지 않는다 `[정적]`

`NtCreateThreadEx` 가 주문서를 채운다:

- `+0x154` 주문서 `+0` ← 0. "이미 있음" 비트가 늘 0 → 늘 새로 만든다.
- `+0x15f` 주문서 `+0x10` (커밋) ← 9번째 인자 (StackSize)
- `+0x16a` 주문서 `+0x18` (예약) ← 10번째 인자 (MaximumStackSize)
- `+0x175` 주문서 `+0x8` ← 8번째 인자 (ZeroBits)

그다음 손에서 손으로:

1. `PspCreateThread` 가 주문서를 `PspAllocateThread` 11번째 인자로 **그대로** 넘긴다.
2. `PspAllocateThread +0x5ea` → `PspSetupUserStack`.
3. `PspSetupUserStack +0x28 TEST [R9],1` — "이미 있음" 이 0 이니 만든다.
4. `+0x80` `RCX` ← 커밋 · `+0x7c` `RDX` ← 예약 · `+0x75` `R8` ← ZeroBits · `R9` ← `0x1000` (| 노드 << 56).
5. `+0x89` → `RtlCreateUserStack`.

- **중간에 0 검사·바꾸기가 하나도 없다** `[정적]`. 신청서에 0 이 적혀 오면 0 이 그대로 맨 끝까지 간다.
- 인자 이름 (StackSize 등) 은 `[추측: 이름]`, 자리는 `[정적]`.

첫 스레드는 다르다 `[정적]`:

- `NtCreateUserProcess +0xd92 ~ +0xdbc` 가 주문서를 **이미지 정보** (`_SECTION_IMAGE_INFORMATION`) 로 미리 채운다.
  - 예약 = `MaximumStackSize`. 단 **최소 `0x40000`** 으로 올린다 (`+0xd99 MOV ECX,0x40000` · `+0xd9e CMP` · `+0xda1 CMOVC` · `+0xda5`).
  - 커밋 = `CommittedStackSize` (`+0xdaa/+0xdb1`).
- 이미지 정보 두 칸은 원래 **exe 헤더 값**이다. `MiBuildImageControlArea` 가 헤더 `SizeOfStackReserve`·`SizeOfStackCommit` 으로 채웠다 (run31, 짐 지도 3.23) `[정적]`.
- 비유:
  - 첫 직원 주문서는 **설계도 기본값을 미리 베껴 온** 채로 온다.
  - 둘째 직원 주문서는 신청한 직원이 쓴 그대로다. 빈칸도 빈칸 그대로.

#### (나) 그럼 0 은 어디서 메워지나 — 목수 (`RtlCreateUserStack`) 안 `[정적]`

- sast/69 는 넷 (`NtCreateThreadEx`·`PspCreateThread`·`PspAllocateThread`·`PspSetupUserStack`) 을 읽고 "여기엔 없다" 까지 왔다.
- 남은 후보는 맨 끝 `RtlCreateUserStack` (rva `0x938db0`, 738바이트) 하나. sast/70 이 그 본문을 읽었다.

**먼저 주의 — 오프셋 순서 ≠ 실행 순서.**

- `_calls.tsv` 는 오프셋 순서라 `+0x202 RtlImageNtHeader` 가 **맨 뒤**에 보인다. 할당 (`+0x104`·`+0x170`·`+0x1c6`) 보다 뒤다.
- 그런데 실제로는 **할당보다 먼저** 탄다. 잘 안 쓰는 길을 함수 뒤쪽에 빼 둔 코드 배치일 뿐이다 (결과서 ②).
- [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다) 의 "부록 보고 오기" 와 같은 모양이다. 부록은 책 뒤에 있지만 먼저 읽는다.

**실행 순서** (run 문서 그대로):

| 걸음 | 하는 일 | 줄 |
|---|---|---|
| 1 | 노드 검사 — `R9` 위 8비트가 `0x40` 보다 크면 `0xC000000D` | `+0x42`·`+0x46` |
| 2 | **0 검사** — 커밋·예약 중 하나라도 0 이면 헤더 블록으로 | `+0x6e` (커밋) · `+0x7b` (예약) → `+0x1eb` |
| 3 | **헤더 블록** — exe 헤더를 찾아 0 인 쪽만 메움 | `+0x1eb ~ +0x250` |
| 4 | 다듬기 ① ~ ⑤ | `+0x81 ~ +0xce`, `+0x299 ~ +0x2e4` |
| 5 | 예약 (말뚝) | `+0x104` `ZwSetInformationProcess(0x29)` |
| 6 | 쪽지 두 칸 | `+0x128` · `+0x130` |
| 7 | 커밋 (바닥) → 쪽지 셋째 칸 | `+0x170` → `+0x187` |
| 8 | 가드 (줄, 조건부) | `+0x1c6` |
| 9 | (7·8 실패 때) 통째로 풀기 | `+0x28d` |

- 모두 `[정적]`. 걸음 5 의 "예약" 이라는 **뜻**만 `[추측]` (아래 (바)).

#### (다) 한 줄 읽기 — "0 일 때만 메우기"

```
RtlCreateUserStack +0x22d  CMOVZ RAX,RCX    ; 커밋이 0 이면 헤더 커밋 값으로
RtlCreateUserStack +0x244  CMOVZ RBX,RDX    ; 예약이 0 이면 헤더 예약 값으로
```

(오른쪽 `;` 설명은 이 해설이 붙였다.)

- **`CMOVZ`** = 바로 앞 검사 결과가 0 일 때만 값을 옮기고, 아니면 그대로 두는 명령. 여기선 "내 칸이 0 이면 헤더 값으로, 아니면 내 값 그대로" 다 `[정적]` (결과서 ② 2(b)).
- **누가**: 목수 `RtlCreateUserStack`.
- **어디서**: 헤더 블록 안.
  - 커밋: `+0x222 ~ +0x22a` 가 커밋 칸을 꺼내 0 인지 보고 → `+0x22d` → `+0x231` 에서 다시 넣는다.
  - 예약: `+0x239 ~ +0x248` 이 같은 일을 한다.
- **무엇을 위해**: 주문서 빈칸을 **설계도 기본값으로 메우기**.
- **핵심**: 두 줄이 **따로따로**다. "둘 다 0 일 때만" 이 아니라 **"0 인 쪽만"**.
  - 결과서 예: 커밋만 0 이고 예약이 `0x200000` 이면 → 커밋은 헤더 값, 예약은 `0x200000` 그대로.
- 끝나면 `+0x250 JMP +0x81` 로 본 계산 (다듬기) 에 돌아간다.

#### (라) 헤더는 어떻게 찾나 — 가게 장부의 "건물 위치" 칸 `[정적]`

사슬 (결과서 ② 2(d)):

1. `+0x1eb` `GS:[0x188]` — 지금 스레드
2. `+0x1f4` `[RAX+0xb8]` — 그 스레드가 **붙은 프로세스** (`_KTHREAD.ApcState.Process`)
3. `+0x1fb` `[RCX+0x2b0]` — 그 프로세스의 `_EPROCESS.SectionBaseAddress` (exe 가 유저 주소 공간에 놓인 시작 주소)
4. `+0x202` (ret `+0x207`) `RtlImageNtHeader` — 거기서 헤더를 찾는다

- 헤더를 찾는 곳은 **PEB 의 `ImageBaseAddress` 가 아니다.** 관리사무소 쪽 **프로세스 객체 칸**이다.
- "어느 프로세스" = **붙은 프로세스**.
  - 부른 쪽 `PspSetupUserStack` 이 `+0x70` `KiStackAttachProcess` 로 대상 프로세스에 붙은 **뒤에** 이 함수를 부르고, 끝나면 `+0xe3` 에서 뗀다.
  - 그래서 **원격 스레드** (다른 프로세스에 만드는 스레드) 도 **그 프로세스의** exe 헤더를 읽는다.
  - 붙어야 하는 까닭은 "대상 프로세스의 유저 메모리를 직접 읽어서" 로 보인다 `[추측: 이유]`.
- 헤더를 찾은 뒤:
  - `RtlImageNtHeader` 가 NULL 이면 → `0xC000007B` 로 끝 (스택 안 만듦).
  - `Magic` (NT `+0x18`) 이 `0x20b` (PE32+) 면 → NT `+0x68` = `SizeOfStackCommit`, NT `+0x60` = `SizeOfStackReserve` (8바이트씩).
  - 아니면 → 4바이트 칸 (`+0x255`·`+0x258`).
- 비유:
  - 주문서 빈칸을 메우려고 목수가 **그 가게 설계도 첫 장** (exe 헤더) 의 "기본 작업대 크기" 를 본다.
  - 설계도를 찾을 때 가게 문 앞 안내판 (PEB) 을 안 본다. 관리사무소 **가게 장부** (`_EPROCESS`) 의 "건물 위치" 칸을 본다.
  - 목수는 지금 **들어가 있는 가게** ([남의 땅에 잠깐 들어가기](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)) 의 장부를 본다. 그래서 남의 가게에 직원을 보내도, 그 가게 설계도로 맞춘다.

#### (마) 다듬기 ① ~ ⑤ `[정적]`

기호: `c` 커밋, `m` 예약 (헤더로 메운 뒤), `P` 페이지 크기 (`R9` 아래 56비트, `0x1000`), `C`·`M` 최종.

| 순서 | 규칙 | 줄 |
|---|---|---|
| ① | 커밋이 **그래도** 0 이면 `0x4000` | `+0x81 ~ +0x89` |
| ② | `c ≥ m` 이면 `m = c` 를 1MB 로 올림 (같을 때도) | `+0x8d/+0x90` → `+0x2c0 ~ +0x2c7` |
| ③ | `C` = `c` 를 `P` 단위로 올림 | `+0x96 ~ +0xa4` |
| ④ | `M` = `m` 을 64KB 로 올림 | `+0xa7 ~ +0xb5` |
| ⑤ | `X = PEB.MinimumStackCommit` 이 0 이 아니고 `C < X` 면 `C = X` (`P` 로 올림). 그래서 `C ≥ M` 이면 `M = C` 를 1MB 로 | `+0xb8` `[R13+0x318]`, `+0x299 ~ +0x2e4` |
| 없음 | 예약 최소값·위 한도 | 이 함수엔 **없다** |

- ① 이 실제로 쓰이려면 헤더 커밋도 0 이어야 한다. 커밋 0 은 늘 헤더 블록을 거쳐 오니까 (결과서 ②).
- 첫 스레드의 "예약 최소 `0x40000`" 은 이 함수가 아니라 `NtCreateUserProcess` 에만 있다 `[정적]`.
- `X` (`PEB+0x318`) 의 출처 `[정적]`:
  - 커널 `MmCreatePeb +0x4c6/+0x4cc` — 레지스트리 `MinimumStackCommitInBytes` (run70).
  - ntdll `LdrpInitializeExecutionOptions +0x406 ~ +0x413` — [IFEO](run46-process-heap-해설.md#26-ifeo-가-이-구간에서-처음-열린다) 값이 더 크면 키운다.
  - 그래서 첫 스레드는 레지스트리 값만, 둘째부터는 IFEO 값까지 볼 수 있다 `[추측: 순서로만]`.

#### (바) 놓기 — 말뚝, 바닥, 줄

**예약 (말뚝)** — `+0x104` (ret `+0x109`) `ZwSetInformationProcess(-1, 0x29, 정보 40바이트, 0x28)`

- 정보 칸 `[정적]`: `+0` 노드 · `+0x10` 예약 크기 `M` · `+0x18` ZeroBits · `+0x20` **돌려받는 시작 주소 `A`**.
- 요청서가 짐작한 "`ZwAllocateVirtualMemory` 두 번 = 예약 + 커밋" 이 **아니었다**. 둘 다 커밋이고, 예약은 이 줄이 한다 (결과서 ② 5번).
- 왜 "예약" 이라고 읽나 — 단계별로:
  1. 근거 `[정적]`: 함수 어디에도 `MEM_RESERVE` 가 없다.
  2. 근거 `[정적]`: 그런데 뒤에서 `A + M − C` 를 커밋하고, 실패하면 `A` 를 `MEM_RELEASE` 로 푼다.
  3. 그래서: 이 줄이 `[A, A + M)` 를 예약한다고 읽을 수 있다.
  4. 아직 확인 아님: `ZwSetInformationProcess` 본문은 안 봤다 → "예약" 은 `[추측]` (Q42 동적 5). 클래스 이름 `ProcessThreadStackAllocation` 도 공개 헤더 값 맞춤 `[추측: 이름]`.
- 왜 굳이 이 함수로? 정보 칸이 {노드, 크기, ZeroBits} 를 넘기고 {시작 주소} 를 받는 꼴이다. 그래서 **노드를 정해서 예약**하려는 것으로 보인다 `[추측]` (결과서 ②). `ZwAllocateVirtualMemory` 인자엔 노드 칸이 없다.
- 실패하면 쪽지 (`INITIAL_TEB`) 를 건드리기 전에 바로 돌아간다.

**쪽지 두 칸** — 예약이 성공한 직후

- `+0x128` `AllocationBase = A`
- `+0x130` `StackBase = A + M` (예약 끝 = 작업대 꼭대기)

**커밋 (바닥)** — `+0x170` (ret `+0x175`) `ZwAllocateVirtualMemory(-1, A + M − C, 0, C, MEM_COMMIT, PAGE_READWRITE)`

- 예약 범위의 **맨 위쪽 `C` 바이트**에만 바닥을 깐다.
- 성공하면 `+0x187` `StackLimit = A + M − C` (커밋 바닥).

**가드 (줄)** — `+0x1c6` (ret `+0x1cb`) `ZwAllocateVirtualMemory(…, 커밋 바닥 − 3P, 3P, MEM_COMMIT, 0x104)`

- `0x104` = `PAGE_READWRITE | PAGE_GUARD`.
- 커밋 바로 **아래 3쪽** (`P = 0x1000` 이면 `0x3000`) 을 한 번에.
- 조건: `M − C ≥ 3P` 일 때만 (`+0x18b CMP` · `+0x18e JC`). 아니면 가드 없이 성공.
- 쪽지는 안 바꾼다.

**실패** — 커밋·가드 중 하나가 실패하면 `+0x28d` (ret `+0x292`) `ZwFreeVirtualMemory(A, 0, MEM_RELEASE)` 로 예약을 통째로 푼다. 이미 쓴 쪽지 칸은 지우지 않는다.

그림 (위가 높은 주소, 결과서 ② 6번):

```
StackBase = A + M        ┬
                         │  커밋 C          바닥 깐 곳
StackLimit = A + M − C   ┼
                         │  가드 3P         "밟으면 알려 줘" 줄 (있을 때만)
                         ┼
                         │  나머지 예약      말뚝만
AllocationBase = A       ┴
```

- 비유 (flow.md 그림 그대로): 큰 땅 (`M`) 에 말뚝을 먼저 박는다. **맨 위 한 칸** (`C`) 에만 바닥을 깐다. 그 **바로 아래 세 칸**에 "여기 밟으면 알려 줘" 줄을 친다.
- 쪽지 세 칸은 `MmCreateTeb +0x12a ~ +0x13a` 가 TEB `StackBase`·`StackLimit`·`DeallocationStack` 으로 옮긴다 (run54).

**덤 — `R9` 의 `0x1000` 은 `MEM_COMMIT` 이 아니다** `[정적]`

- `R9` 아래 56비트는 **페이지 크기 `P`** 다. 커밋 올림 (③) 과 가드 크기 (`3P`) 에 쓴다.
- `MEM_COMMIT` 도 `0x1000` 이라 헷갈리기 쉽다. 그런데 **우연히 같은 숫자**일 뿐이다. `MEM_COMMIT` 은 `+0x152` 에서 따로 만든다.
- `R9` 위 8비트는 선호 노드다 (걸음 1 검사, 예약 정보 칸 `+0`). 보통 0 일 것 `[추측]` (Q42 동적 5 · Q142 ④).

#### (사) test.exe 에 대 보면 — 첫 스레드와 같은 값

- `[소스]` `CreateThread(NULL, 0, Worker, NULL, 0, NULL)` — 둘째 인자 (스택 크기) 가 0.
- 이게 커널에 (커밋 0, 예약 0) 으로 간다고 **치면** `[추측]` (유저 쪽 `KERNELBASE!CreateRemoteThreadEx` 를 안 봄), 규칙을 차례로 대 보면:
  1. 걸음 2: 둘 다 0 → 헤더 블록.
  2. 헤더 (`sample/bin/test.exe`, PE32+, 오케스트레이터가 파일에서 읽음): `SizeOfStackReserve 0x100000`, `SizeOfStackCommit 0x1000` → 둘 다 헤더 값으로.
  3. 다듬기: ① 커밋이 0 이 아니라 안 탐 · ② `0x1000 ≥ 0x100000` 이 아니라 안 탐 · ③ ④ 이미 맞는 단위 · ⑤ `X = 0` 이라 안 탐.
  4. 결과 `M = 0x100000` (1MB), `C = 0x1000` (1쪽), `M − C ≥ 3P` 라 가드 `0x3000` (`PEB.MinimumStackCommit = 0` 일 때) `[정적]`.
- **첫 스레드와 같은 값이다.**
  - 첫 스레드는 이미지 정보로 미리 채워서 헤더 블록을 안 탄다.
  - 하지만 이미지 정보 = 헤더 값이고, 예약 `0x100000` 은 최소 `0x40000` 보다 커서 그대로다.
  - 그래서 같은 다듬기를 거쳐 같은 `M`·`C` 가 된다 (run 문서 "결과서와 다르게 적은 곳" ①).
- **다른 경우** (test.exe 는 아님):
  - 헤더 예약이 `0x40000` 보다 작은 exe 면 → 첫 스레드만 `0x40000` 으로 올라간다. 둘째는 헤더 값 그대로 (64KB 올림만).
  - `MinimumStackCommit` 은 둘째부터 IFEO 값까지 쓰일 수 있다 `[추측]`.

#### (아) 사진과 맞춰 보면 — 첫 스레드 (run44 K2)

둘째 스레드는 사진 범위 밖이다. 대신 **같은 목수를 쓰는 첫 스레드** 값으로 식을 맞춰 봤다 (flow-writer, 새 실행 없음).

| 무엇 | 사진 `[관찰]` | 식으로 읽으면 |
|---|---|---|
| TEB `StackBase` | `0x9b2fd00000` | `A + M` |
| TEB `StackLimit` | `0x9b2fcff000` | `A + M − C` → `C = 0x1000` |
| TEB `DeallocationStack` | `0x9b2fc00000` | `A` → `M = 0x100000` |
| PEB `MinimumStackCommit` | 0 | ⑤ 안 탐 |
| 스택 VAD `9b2fc00 ~ 9b2fcff` Commit | **4쪽** (K3·P0·P1 도 4) | 커밋 1쪽 + 가드 3쪽 |
| 트랩 프레임 `Rsp` | `0x9b2fcfff48` | `StackBase − 0x90 − 0x28` → 랜덤 몫 `0x90` = 9 × 16 |

- 식과 **맞는다**. `Rsp` 는 랜덤이라 "어긋나지 않는다" 까지다 (범위 0 ~ `0x7f0` 안).
- 덤: P1a 5쪽 · P1b·P2 7쪽이 되고, `StackLimit` 도 `0x9b2fcfe000` → `0x9b2fcfc000` 으로 같이 내려간다.
  - 커밋 2·4쪽 + 가드 3쪽으로 숫자가 맞는다.
  - **스택을 쓰면서 가드 줄이 아래로 옮겨진 것**으로 보인다 `[추측: 가드를 옮기는 Mm 쪽은 안 봄]`.
- 단 VAD 줄의 보호 칸 `READWRITE` 은 쪽마다 값이 아니다 `[추측]`. 가드 3쪽이 정말 `PAGE_GUARD` 인지는 동적이다.

#### PE_emulator 에게

- "스택 크기 0 → exe 헤더" 는 **맞다** — 칸·오프셋까지 (70-2).
- 단 둘을 보탤 수 있다 (run 문서 표):
  - "둘 다 0 일 때만" 이 아니라 **"0 인 쪽만"**.
  - 그 뒤 다듬기 ① → ② → ③④ → ⑤ 가 붙는다 (`X = 0` 이면 ⑤ 빠짐).
- 호출 이름까지 맞추려면: 예약은 `ZwSetInformationProcess(0x29)`, 커밋·가드는 `ZwAllocateVirtualMemory` (70-5).

### 2.6 다른 점 ③ — "첫 직원" 체크가 안 켜진다

#### 새 말 — `CreateFlags`

- `NtCreateThreadEx` 의 **7번째 인자**. 비트 묶음이다 (자리 `[RBP+0x330]` → `R13D`) `[정적]`. 이름은 `[추측: 이름]`.
  - 비유: "한 명 더" 신청서의 **체크 칸** ([비트와 플래그](../02_sprint/run9-create-user-process-해설.md#비트와-플래그) 의 그 체크 칸).
- 이걸 `PspCreateThread` 가 관리사무소 **내부 양식** (만들기 플래그) 으로 옮겨 적는다.
  - [run12 해설 2.5](../02_sprint/run12-insert-thread-해설.md#25-flags-는-어디서-오나--전제) 의 "신청서 체크 칸 → 내부 양식" 과 같은 그림이다.

#### (가) 맨 먼저 하는 검사 — 허용 `0x7f` `[정적]`

- `NtCreateThreadEx +0x70 TEST R13D,0xffffff80` → `+0x77 JNZ` → `+0x303` `0xC00000F5`.
- 비트 0 ~ 6 (`0x7f`) 말고 다른 체크가 있으면 신청서를 바로 돌려보낸다.
- 속성 쪽지 검사 (`+0xde`) 보다도 앞이다.

#### (나) 체크 칸 → 내부 양식 → 쓰이는 곳 `[정적]` (`PspCreateThread +0x197 ~ +0x47c`)

| `CreateFlags` (이름 `[추측]`) | → 내부 양식 | 그 뒤 쓰이는 곳 |
|---|---|---|
| `0x1` CREATE_SUSPENDED | `0x1` | `PspInsertThread +0x2e0` `KeSuspendThread` (대기 팻말) |
| `0x2` SKIP_THREAD_ATTACH | `0x2` | `SameTebFlags 0x8` |
| `0x4` HIDE_FROM_DEBUGGER | `0x4` | `CrossThreadFlags` 비트 2 |
| `0x8` | — | **안 읽힘** (통과만) |
| `0x10` LOADER_WORKER | `0x80` | `SameTebFlags 0x2000` |
| `0x20` SKIP_LOADER_INIT | `0x100` | `SameTebFlags 0x4000` |
| `0x40` BYPASS_PROCESS_FREEZE | `0x200` | `KTHREAD.MiscFlags` 비트 21 |
| (없음) | `0x20`·`0x40` **안 만듦** | `0x40` → `SameTebFlags 0x400` (InitialThread) 는 첫 스레드 길만 |

- run54 가 본 `PspAllocateThread +0x54a ~ +0x5a5` 사슬의 **입구**가 이걸로 이어졌다: 내부 `0x2` ← `0x2`, `0x80` ← `0x10`, `0x100` ← `0x20`.
- 숫자 대응 자체는 run12 에서 첫 스레드 길의 `PspMapThreadCreationFlags` 로 본 것과 똑같다 (두 문서의 숫자를 나란히 놓아 본 것).
  - 둘째 길은 같은 규칙을 `PspCreateThread` 안에서 직접 하는 것으로 보인다 `[추측]` (이 해설의 짐작, 근거는 숫자가 같다는 것뿐).

#### (다) 왜 `0x40` 이 이 길에선 절대 안 켜지나

- 첫 스레드 길에서 내부 `0x40` (과 `0x20`) 을 켜는 건 `NtCreateUserProcess +0xdfd` (`0x60` 도장) **하나**다 (run12).
- 둘째 길엔 그 줄이 없다. 그리고 `CreateFlags` 의 어느 비트도 내부 `0x40` 으로 가지 않는다 (위 표).
- 그래서 둘째 스레드 안내판엔 **InitialThread (`0x400`) 가 켜질 길이 없다** `[정적]`.

#### (라) `CreateFlags = 0` 이면 — 안내판 표시 칸에 펜을 안 댄다

`SameTebFlags` 는 [run54 해설 2.8](run54-teb-fill-해설.md#28-sametebflags-첫-스레드는-늘-0x400-뿐) 의 그것이다. TEB `+0x17ee` 의 16비트 표시 칸 묶음이고, 첫 스레드는 늘 `0x400` 이었다.

단계별로:

1. 근거 `[정적]`: `CreateFlags` 0 → 내부 양식 0 (`PspCreateThread +0x1a1`, 켜는 줄 전부 안 탐).
2. 근거 `[정적]`: 그러면 `PspAllocateThread +0x54a ~ +0x5a5` 의 계산 결과 `R12W = 0`.
3. 근거 `[정적]`: `+0x66f TEST R12W,R12W` · `+0x673 JNZ +0x780` 를 **안 탄다** → `+0x793` (TEB 쓰기) 를 **건너뛴다** (2.2 줄 2).
4. 근거 `[정적]`: `MmCreateTeb` 에도 `+0x17ee` 쓰기가 없다.
5. 그래서 → **커널은 둘째 스레드의 `SameTebFlags` 를 안 쓴다** `[정적]`.
6. 그럼 값은? 새 TEB 페이지 그대로일 테니 `0` 일 것 `[추측]` ([run54 해설 2.5](run54-teb-fill-해설.md#25-핵심-2-왜-이번엔-몸통-memset-이-없어도-0-인가) 의 "새 안내판" 이야기).
7. 아직 확인 아님: 실제 값은 동적 **Q42**.

- 비유:
  - run54 해설에선 접수 창구 (`PspAllocateThread`) 가 첫 직원 안내판에 "첫 직원" 표시 (`0x400`) 를 16칸 띠째로 붙였다.
  - 둘째 직원은 입사 서류 체크박스가 **전부 비어** 있다. 그러면 담당은 표시 띠를 **아예 안 붙인다**.
  - 공장에서 막 나온 새 안내판이라 그 칸은 처음부터 빈칸일 것이다.
- `CreateFlags` 에 `0x2`·`0x10`·`0x20` 이 있으면 얘기가 다르다. 그땐 `R12W` 가 0 이 아니라 커널이 쓴다 (위 표의 `0x8`·`0x2000`·`0x4000`).
- PE_emulator: "첫 스레드 값 `0x400` 을 둘째에도" 라면 **틀림** (69-3(c)).

#### (마) `0x20` 도 안 켜진다 → 프로세스 락을 직접 잡는다 `[정적]`

- 첫 스레드 길: 내부 `0x20` = "프로세스 락은 이미 잡아 왔음" 도장. 그래서 `PspInsertThread +0x77` 에서 락을 새로 안 잡는다 (run12).
- 둘째 길: 도장이 없다 → **`PspInsertThread +0x8e` (ret `+0x93`) `KeAbPreAcquire` 로 직접 잡는다**.
- 디버거 쪽 쓸모 (결과서 ①): `nt!PspInsertThread+0x8e` 에서 멈추면 "첫 스레드 길이 아니다" 라는 표시가 된다.

#### (바) `CREATE_SUSPENDED` (`0x1`) — 팻말은 결과 쓰기보다 먼저 `[정적]`

- `PspInsertThread +0x2d4` 가 내부 양식 비트 0 을 본다 → 켜져 있으면 `+0x2e0` (ret `+0x2e5`) `KeSuspendThread`.
- 켜든 안 켜든 `+0x5a1` (ret `+0x5a6`) `KeReadyThread` 는 **늘** 불린다.
  - [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나) 의 "팻말 건 직원도 대기 줄엔 선다" 와 같다.
- 신청한 쪽에 결과를 써 주는 순서:
  - `ObInsertObjectEx +0x3e5` → TEB 주소 `+0x40c` → ClientId `+0x450` → `+0x490` (`ThreadInserted` 켜기) → 직원 알림 `PspCallThreadNotifyRoutines +0x4f5` → 핸들 만들기 `PspCreateObjectHandle +0x520` → 핸들 쓰기 `+0x573` → `KeReadyThread +0x5a1`.
- 그러니 팻말 (`+0x2e0`) 은 핸들·결과 쓰기보다 **먼저** 걸린다.
- test.exe 의 `CreateThread` 다섯째 인자는 0 이다 `[소스]`. 그래서 `+0x2e0` 은 보통 안 설 것 `[추측]` (Q42 동적 6).

### 2.7 그 밖에 새로 본 것 — 속성 목록은 만들기 "전"에 검사한다

첫·둘째의 큰 차이는 아니다. 그래도 PE_emulator 가 물어서 꼼꼼히 봤다.

#### 새 말 — 속성 목록

- `NtCreateThreadEx` 의 **11번째 인자** (이름 `AttributeList` `[추측]`).
  - 첫 8바이트 = 전체 길이 (`TotalLength`), 그 뒤로 **0x20 바이트짜리 항목**이 줄줄이 온다 `[정적]`.
  - 항목마다 번호·크기 (`Size`)·값.
- 비유: "한 명 더" 신청서 뒤에 붙이는 **추가 요청 쪽지 묶음**. 예: "사번 (ClientId) 은 여기 적어 주세요", "안내판 위치는 여기 적어 주세요".
- 첫 스레드 길에도 같은 검사 함수가 있다 (`PspBuildCreateProcessContext`, run9). 다른 건 **셋째 인자** 하나다 `[정적]`:
  - 스레드 1 (`NtCreateThreadEx +0xd5`) / 프로세스 0 (`NtCreateUserProcess +0x28c`).

#### 누가 · 언제 `[정적]`

- `PspBuildCreateProcessContext` 가, `NtCreateThreadEx +0xde` 에서. 목록이 NULL 이면 아예 안 부른다.
- **스레드를 만들기 전**이다. `PspCreateThread` (`+0x2cd`) 보다, 프로세스 잡기 (`+0x127`) 보다도 앞.

#### 규칙 — 어기면 모두 `0xC000000D` `[정적]`

- `TotalLength ≥ 0x28` (`+0x12a`) — 빈 목록도 거절, 최소 항목 1개.
- `(TotalLength − 8) & 0x1f == 0` (`+0x17a`) — 항목 크기 0x20 의 배수.
- 항목 `Size` 가 속성마다 정해진 값.
- 같은 속성 두 번 (`+0x1f8 ~ +0x200`).
- INPUT 비트가 있는 항목은 `ReturnLength` 칸이 NULL (`+0x1d9 ~ +0x1e4`).
- **스레드 모드면 항목마다 THREAD 비트 (`0x10000`)** (`+0x1c1 ~ +0x1cd`).
- (목록 주소가 4바이트 정렬이 아니면 정렬 예외.)

#### 받는 속성은 다섯 — 모르는 건 거절

| 값 | 이름 `[추측]` | Size | 하는 일 |
|---|---|---|---|
| `0x10003` | CLIENT_ID | `0x10` | 출력 → `PspInsertThread +0x450` 에서 씀 |
| `0x10004` | TEB_ADDRESS | `8` | 출력 → `PspInsertThread +0x40c` 에서 씀 |
| `0x3000c` | GROUP_AFFINITY | `0x10` | 스레드 전용, `KeVerifyGroupAffinity` 통과해야 |
| `0x3000e` | IDEAL_PROCESSOR | `4` | 스레드 전용 |
| `0x3001e` | ENABLE_OPTIONAL_XSTATE_FEATURES | `8` | 스레드 전용 |

- 값은 asm 상수 `[정적]`, 이름은 공개 헤더 맞춤 `[추측]`.
- 그 밖의 번호는 `+0x1690` 기본 갈래로 간다. 기능 게이트·번호 `< 0x25`·값 비트 `0x80000`·크기가 다 맞아야 받는다. 아니면 **거절** (무시 아님).
- 프로세스용 속성 (`PARENT_PROCESS`·`TOKEN` 등) 은 THREAD 비트가 없어서, 스레드 모드에선 모두 거절.
- 비유: 창구는 직원을 만들기 **전에** 쪽지를 한 장씩 본다. "직원 추가용" 도장 (THREAD 비트) 이 없는 쪽지가 하나라도 있으면 신청 전체를 돌려보낸다.
- PE_emulator: "그 밖은 멈춤" 은 방향이 **맞다**. 단 "`CLIENT_ID`·`TEB_ADDRESS` 둘만" 은 **좁다** — 커널은 셋을 더 받는다. 어디까지 구현할지는 사용자 결정 (69-4(c)).

#### 출력 칸 주소가 나쁘면 — 보통은 만들기 전에 끝 `[정적]`

- **보통**: 쪽지를 읽을 때 출력 칸에 **읽고-다시-쓰기 시험**을 한다 (CLIENT_ID `+0x5e6 ~ +0x5ed`, TEB_ADDRESS `+0x558 ~ +0x55f`).
  - 실패하면 예외 → 함수 전체를 감싼 try `[+0xee,+0x181b)` 의 착지점 `+0x181b` → 정리 → `NtCreateThreadEx +0xe5 JS` 로 끝.
  - **스레드도 핸들도 없다.** 돌려주는 값은 예외 코드.
  - 단 `ReturnLength` 칸은 이미 써 버린 뒤다 (`+0x5af`·`+0x63d`).
- **경쟁** (시험 뒤에 주소가 나빠진 경우): `PspInsertThread` 의 쓰기 셋 (`+0x40c`·`+0x450`·`+0x573`) 이 각각 try 안이다.
  - TEB·ClientId 쓰기 실패 → `ThreadInserted` 를 건너뛰고도 `KeReadyThread` 는 부른다. 그 직원이 처음 움직일 때 출근 도장 창구 (`PspUserThreadStartup`) 가 `ThreadInserted = 0` 을 보고 `0xC000004B` 로 **스스로 끝난다** (sast/10).
  - 핸들 쓰기 실패 → 방금 만든 핸들을 닫고 `KeRequestTerminationThread`.
  - 어느 쪽이든 신청한 쪽은 실패를 받고, 핸들은 안 남는다.
- 비유: 창구가 "사번 적어 줄 칸" 에 미리 **한 번 써 보고**, 안 되면 거기서 접수를 끝낸다. 드물게 그 사이에 칸이 없어지면, 직원은 뽑혔다가 첫 출근 도장에서 바로 그만둔다.
- PE_emulator: "만들기 전 실패" 만 흉내 내도 거의 충분 (69-4(d)).
- (`.pdata` 의 try 범위는 스크립트가 안 보여 줘서, 결과서가 임시 스크립트 + `objdump` 로 읽었다 — 레포 밖, 읽기 전용.)

### 2.8 정정과 "결과서와 다르게 적은 곳" (짧게)

- **run54 정정** `[정적]`:
  - run54 는 "`INITIAL_TEB` 는 `RtlCreateUserStack` 이 채운다 (… `+0x128`)" 라고 적었다.
  - `+0x128` 은 **`AllocationBase` 한 칸**만 쓴다. `StackBase` 는 `+0x130`, `StackLimit` 는 `+0x187` 이다 (2.5 (바)).
  - run54 맨 위에 정정 한 줄을 넣고, 짐 지도 3.3 `NtTib` 줄을 고쳤다. `DeallocationStack` 줄의 `+0x128` 은 맞다.
- **sast/70 의 `[추측]` 하나는 이미 `[정적]`**: "이미지 정보 두 칸 = 헤더 값은 안 확인" → run31 (`MiBuildImageControlArea +0x175/+0x179` · `+0x17d/+0x181`) 에 이미 있었다. run44 K2 값도 맞는다.
- **`+0x1f3` callee**: `_calls.tsv` 엔 별칭으로만 나와서, 하는 일 (스택 탐침) + 후보 이름 둘로 적었다.
- **`PspInsertThread +0x490`**: 결과서는 이 비트를 `ThreadInserted` 라 부르고, run12 는 같은 줄을 "스레드 락 풀기" 단계로 적었다. 서로 어긋나지 않는다 (run 문서).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | — | PDB 목록에 run74. "사진" 메모에 run74 (정적, run44 로그 다시 읽음) |
| 2.1 트리 `PspSetupUserStack` 아래 | 끝 | `+0x8e RtlCreateUserStack` 줄 더함 `[정적]` (그림엔 없음) |
| 2.1 `PspCreateUserContext` 절 | 첫 스레드 이야기만 | `NtCreateThreadEx` 길은 이 함수를 **안 부르고** 같은 값 세트를 직접 쓴다 `[정적]` |
| 2.1 `PspAllocateThread` 절 | 부르는 곳은 `NtCreateUserProcess` | + 둘째 길 자리 `PspCreateThread +0x27e`, `CreateFlags` → 내부 양식 사슬, 0 이면 `+0x793` 건너뜀 `[정적]` |
| 2.1 **새 절** `PspSetupUserStack → RtlCreateUserStack` | — | 다듬기·예약·커밋·가드·`INITIAL_TEB` `[정적]` + run44 K2 대조 `[관찰]` |
| 2.1 `PspInsertThread` 절 | 첫 스레드 길 (락은 호출자가) | + 둘째 길 자리 `+0x2e5`, 락 직접 (`+0x8e`) `[정적]` |
| 3장 `NtCreateThreadEx` 길의 `Rip` | `RtlUserThreadStart` `[추측]` (run16) | `[정적]` (`+0x1bf` → `+0x220`) |
| 3장 `test!Worker` 줄 | `PspCreateThread → PspAllocateThread` 길 `[추측]` (run11) | 커널 쪽 `[정적]`, 유저 쪽은 그대로 `[추측]` |
| 3장 **새 절** "둘째 스레드를 만드는 커널 길" | — | 트리 + 첫·둘째 비교표 + `CreateFlags`·`SameTebFlags`·속성·스택 요약 |
| 4장 `SameTebFlags` 줄 (3번) | 일꾼·`Worker` 값은 동적 | + `NtCreateThreadEx` 길 대응 `[정적]`, `CreateFlags = 0` 이면 커널이 안 씀 (값 `0` 은 `[추측]`) |
| 4장 새 줄 셋 | — | Q42 동적 1 ~ 6 · 정적 남은 것 (→ Q142) · 결과서와 다르게 적은 곳·정정 |
| 5장 | — | run74 줄 |

- 짐 지도 `baggage.md`: **새 짐 칸 없음**, 2장 숫자 그대로.
  - 둘째 스레드는 은퇴 (`NtContinue`) 뒤라 짐 지도 범위 밖이다.
  - 다만 같은 목수를 쓰는 **첫 스레드 스택 줄**의 근거를 고쳤다: 3.1 `MinimumStackCommit` (읽는 곳 `RtlCreateUserStack +0xb8`), 3.3 `NtTib` (정정), 3.3 `DeallocationStack` (`A` 의 출처), 7.6 C8 (유저 스택 모양).

### 큰 그림에서

- **지도 쪽**:
  - 둘째 스레드는 지도 밖이다.
  - 하지만 첫 스레드 **스택 줄**이 이번에 맨 끝 (`RtlCreateUserStack` → `Zw*` 호출) 까지 닿았다.
  - 짐 지도 규칙 ("줄기 위에서는 끝까지") 으로 보면, 유저 스택이라는 짐의 **놓는 법**이 코드로 닫힌 셈이다. 꼬리표는 하나 남았다 — 예약 (`ZwSetInformationProcess`) 은 `[추측: 예약]`.
- **PE_emulator 쪽**:
  - "첫 스레드 사슬을 재사용" 은 맞았다.
  - 보탤 수 있는 곳은 셋 — 문맥 만드는 곳의 이름, "0 인 쪽만 + 다듬기", `SameTebFlags` 를 `0x400` 으로 안 쓰기.
- **다음**: 같은 요청 25 의 나머지 넷 — 넘기기 (Q141b) · 기다림 (Q141c) · 끝내기 (Q141d) · 남의 스레드 문맥 (Q141e).
  - 이번에 만든 직원이 **일하고, 기다리고, 그만두는** 쪽이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 둘째 스레드 `SameTebFlags` 실제 값 (기대 `0`) | 커널이 안 쓴다까지만 정적. 새 페이지가 0 이라는 건 `[추측]` | **Q42** 동적 1 (`nt!PspAllocateThread+0x661` `dx ((nt!_TEB*)@rbx)->SameTebFlags` · `+0x66f` `r12w` · `+0x6f4`) |
| 첫 CONTEXT 실제 값 (`ContextFlags`·`Rip`·`Rcx`·`Rdx`·`Rsp`) | 실행 때 값 | **Q42** 동적 2 (`nt!NtCreateThreadEx+0x2cd` `rbx`, `nt!PspSetupUserStack+0xe3`) |
| `CreateThread(…, 0, …)` 가 커널에 정말 (0, 0)·`CreateFlags 0` 을 넘기나 | 유저 쪽 `KERNELBASE!CreateRemoteThreadEx` 를 안 읽음 | **Q42** 동적 3 (`nt!PspSetupUserStack+0x89` `rcx`·`rdx`·`r8`·`r9`) · 정적으로는 **Q142 ①** |
| 헤더 블록을 정말 타나, 읽은 헤더 값 | 실행 때 값. 타깃 VM 의 test.exe 가 로컬 파일과 같은 빌드인지도 확인 필요 (결과서 ②) | **Q42** 동적 3 (`nt!RtlCreateUserStack+0x1eb` 가 서나 · `+0x207` `dq @rax+0x60 L2`) |
| `PEB+0x318` 이 둘째 스레드 때도 0 인가 | run44 사진은 P2 까지만 0 | **Q42** 동적 4 |
| `ZwSetInformationProcess(0x29)` 가 정말 예약하나, `A` 가 64KB 정렬인가, 가드 3쪽이 `PAGE_GUARD` 인가, 노드가 0 인가 | 본문 안 봄 `[추측: 예약]` | **Q42** 동적 5. 이상하게 나오면 **Q142 ②** (Mm 쪽 정적) |
| 속성 목록이 NULL 인가, `SecureProcess` 0 인가, `PspInsertThread +0x8e` 가 서나·`+0x2e0` 은 안 서나, 기능 게이트 값 | 실행 때 값 | **Q42** 동적 6 (선택) |
| `ctx+0x10` 비트 0 을 누가 읽나 | 안 따라감 | **Q142 ③** |
| 노드 갈래 (`PspAllocateThread +0x8c7`) 조건 | 안 읽음 | **Q142 ④** |
| ntdll `RtlCreateUserStack` (rva `0xf5130`, 709B) 이 커널 것과 같은 꼴인가 | 크기가 비슷해서 `[추측]`, 본문 안 봄 | **Q142 ⑤** |
| 스택을 쓰며 가드가 내려가는 쪽 (Mm) | 이번 범위 밖 `[추측]` | 대기열 항목 없음 |
| ntdll 일꾼이 어떤 `CreateFlags` 로, 이 길로 만들어지나 | 일꾼 공장 (run62) 쪽 길은 안 봄 | **Q79** (flow.md 3장) |

- Q42 의 거르기: `NtCreateThreadEx` 는 일꾼 스레드에도 걸린다. 그래서 프로세스 = test.exe **그리고** `r13d == 0` (CreateFlags) **그리고** `poi(@rbp+0x320)` = `test!Worker` 로 거른다 `[추측: 거르기]`.
- `nt!RtlCreateUserStack` 은 `PAGE` 구역이라 `bp` 가 미해결일 수 있다. 첫 스레드가 이미 거쳐서 올라와 있을 것 `[추측]`.
- 결과서 ① 의 도구 제안 (`.asm` 머리에 "try 범위 → 착지점" 줄 붙이기) 은 하네스라 사용자 검토 몫이다.

---

## 5. 더 읽기

- run 문서: [`run74-thread-create.md`](run74-thread-create.md) — 호출 지점 표, 첫·둘째 비교표, `CreateFlags` 표, 속성 표, `RtlCreateUserStack` 실행 순서, 호출 관계 그림, 사진 대조
- 결과서 ①: [`_workspace/sast/69_thread-create_result.md`](../../_workspace/sast/69_thread-create_result.md) — 인자 자리 표, 질문 1 ~ 5, 출력 쓰기 try 범위 표, 추천 멈춤점
- 결과서 ②: [`_workspace/sast/70_thread-stack_result.md`](../../_workspace/sast/70_thread-stack_result.md) — `RtlCreateUserStack` 인자 표, 다듬기 표, 할당 꼴 표, `INITIAL_TEB` 계산식, "PE_emulator ㄱ-2 최종 답"
- 요청서: [`69_thread-create.md`](../../_workspace/sast/69_thread-create.md) · [`70_thread-stack.md`](../../_workspace/sast/70_thread-stack.md)
- flow.md: [2.1](flow.md) `PspCreateUserContext`·`PspAllocateThread`·`PspInsertThread` 절의 run74 줄 + 새 절 `PspSetupUserStack → RtlCreateUserStack` · [3장](flow.md) 새 절 "둘째 스레드를 만드는 커널 길" · 4장 run74 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.1 `MinimumStackCommit` · 3.3 `NtTib`·`DeallocationStack` · 7.6 C8
- 앞 이야기: [run11 해설](../02_sprint/run11-allocate-thread-해설.md) (몸 만들기 다섯 걸음) · [run12 해설](../02_sprint/run12-insert-thread-해설.md) (등록·팻말·대기 줄, 내부 양식) · [run16 해설](../02_sprint/run16-thunk-context-해설.md) (업무 시작 메모, 본업 자리) · [run54 해설](run54-teb-fill-해설.md) (목수·쪽지·안내판, `SameTebFlags`) · [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정) (말뚝·바닥)
