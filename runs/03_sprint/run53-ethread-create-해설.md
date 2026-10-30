# run53 ethread-create — 해설

> 이 글은 [run53 문서](run53-ethread-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78d2**: Q78d (첫 스레드) 넷 중 **둘째 조각**. `_ETHREAD`·`_KTRAP_FRAME` "만들 때" 몫 169칸 (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q78d3** (`_TEB`) → Q78d4 (스레드 그 뒤 구간) → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 질문이 간 곳: D1 (덤프로 답할 수 있음) → **Q79** 출처 · D2 ~ D4 (낮음) → **Q100**.
- 근거:
  - run 문서 [`run53-ethread-create.md`](run53-ethread-create.md) (가장 중요)
  - 요청서 [`_workspace/sast/47_ethread-create.md`](../../_workspace/sast/47_ethread-create.md)
  - 결과서 [`_workspace/sast/47_ethread-create_result.md`](../../_workspace/sast/47_ethread-create_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 3.13 (`_ETHREAD`) · 3.15 (`_KTRAP_FRAME`) · 7.16 (새 절)
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 트리와 `PspAllocateThread`·`KeInitThread` 절 · 4장 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값 대조는 run44 때 찍어 둔 사진 (K2 ~ P2 일곱 장) 으로만 했다. 이 글의 "사진" 은 전부 run44 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길), DWORD 되쓰기, 겹치는 칸 (`*Fill*`), 오탐·놓침, K1 옛 객체, 확장 영역 (별지), `[관찰 간접]` → [run52 해설](run52-kthread-create-해설.md) 2.4 · 2.5 · 2.6 · 2.7 · 2.8 · 2.9 · 2.10
- 인사 카드 (`_ETHREAD`), 첫 직원 뽑기 다섯 걸음 → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- APC = 펼쳐 볼 **쪽지**, 신호등 → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md)
- 트랩 프레임 = **출발 메모**, 나갈 때 꺼내는 칸 다섯 → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- CONTEXT = 업무 시작 메모, 출발 메모로 옮겨 적기, 유저 스택 위 사물함 → [run16 해설 2.4 · 2.5](../02_sprint/run16-thunk-context-해설.md)
- 자기를 가리키는 빈 목록 → [run18 해설 2.6](../02_sprint/run18-proc-init-setup-해설.md)
- 사진 지점 K1 · K2 · K3 · P0 … → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 비유: 그대로 이어 쓴다

- 첫 직원 책상 차리기를 run52 에서 그대로 이어 간다. `PspAllocateThread` = **접수 창구**, `KeInitThread` = **팀장**, `KiInitializeContextThread` = **비서**, `memset` = **지우개**.
- `_ETHREAD` 중 이번 몫 = 인사 카드 **뒷면** (앞면 `_KTHREAD` 업무 카드는 run52 에서 끝났다).
- 자기 연결 리스트 머리 = 아직 빈 **서류함** (run 문서 비유).
- 하나만 바꿔 부른다:
  - run 문서는 트랩 프레임을 "첫 출근 때 쓸 **쪽지**" 라고 불렀다. 그런데 이 글엔 APC **쪽지** (`UpdateTebApc`) 가 같이 나온다.
  - 그래서 트랩 프레임은 run15·run16 해설대로 **출발 메모** 라고 부르겠다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| `_ETHREAD`·`_KTRAP_FRAME` 만들 때 몫 모르는 칸 | **169 → 여전히 모름 0** |
| `K1~K2` 30칸 | **30칸 모두** 처음 값을 쓰는 줄을 찾음 `[정적]`. 사진 K2 값과 칸마다 맞음 |
| 끝까지 0 139칸 | 0 을 씀 **7** · 조건부 안 탐 **23** · 아무도 안 씀 **34** · 범위 밖 길 **75** `[추측]` |
| 셈 방식 | 몸통 `memset` 만으로는 "0 을 씀" 으로 **안 센다** (run50·run52 와 같음). 그렇게 세면 139 |
| 몸통 0 채움 | `_ETHREAD` `PspAllocateThread +0x307`, 트랩 프레임 `KiInitializeContextThread +0x187` `[정적]` |
| 가장 큰 발견 | `KiInitializeContextThread +0x157 ~ +0x15f` 가 **`ContextFlags` 를 줄여 넘긴다** → FP·디버그 블록은 이 길에서 **늘 안 탄다** `[정적]` |
| `MxCsr` | CONTEXT 값이 아니라 **상수** `0x1f80` (`+0x1f1`) `[정적]` |
| `EFlags` | `(CONTEXT.EFlags & 0x210dd5) \| 0x200` → **`0x200`** `[정적]`, 사진 K2 와 같음 |
| `UpdateTebApc` | run52 의 둘째 `KeInitializeApc` 와 **같은 호출**. 88바이트 안 빈자리에 **다른 칸이 박혀 있다** `[정적]` |
| 간접 호출 | 만들기 길 안에 **없음** (`.asm` 26개) `[정적]` |
| 정정 | 짐 지도 3.15 `SegCs`·`SegSs` **짐작 → 정확** |

이 글에서 붙잡아 둘 네 줄:

> **(1) 지우개는 세지 않고, 펜만 센다.**
> `memset` 은 모든 칸을 똑같이 민다. 그걸 세면 139칸이 다 "0 을 씀" 이 되어 구별이 사라진다.
>
> **(2) `UpdateTebApc` 88바이트는 쪽지 + 여백 세입자다.**
> 쪽지 쓰는 함수는 정해진 칸만 쓴다. 여백에 적힌 다른 칸의 값은 그대로 남는다.
>
> **(3) 비서가 "옮겨 적기 체크표" 를 고쳐서 넘긴다.**
> 그래서 FP·디버그 칸이 빈 것은 test.exe 사정이 아니라 **이 호출 자리의 코드 모양** 때문이다.
>
> **(4) `EFlags` 의 `0x200` 은 메모에서 온 값이 아니다.**
> 메모의 값은 0 이고, `0x200` 은 옮겨 적는 함수가 사용자 모드라서 켠 비트다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

여전히 **부모 cmd 스레드 안, 커널**이다. run52 가 본 같은 `PspAllocateThread` 안에서, 이번엔 인사 카드 뒷면과 출발 메모를 본다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0xc36  PspAllocateProcess               가게 장부 (run49·run50)
   ├─ +0xe5c  PspAllocateThread                첫 직원 뽑기
   │    ├─ +0x2e7  ObCreateObjectEx             빈 카드 받기
   │    ├─ +0x30c  memset                       지우개 (run52 에서도 봄)
   │    │  +0x31a ~ +0x4e7                      인사 카드 뒷면 칸들          ← 이번
   │    ├─ +0x432 · +0x4c4                      세마포어 · 입사 시각          ← 이번
   │    ├─ +0x6f9  KeInitThread                 팀장 (업무 카드는 run52)
   │    │    ├─ +0x301  KiInitializeContextThread  비서
   │    │    │    ├─ +0x18c  memset             출발 메모 지우개              ← 이번
   │    │    │    └─ +0x1b1  KxContextToKframes 메모를 출발 메모로 옮겨 적기  ← 이번
   │    │    └─ +0x36d  KeInitializeApc         둘째 쪽지 UpdateTebApc        ← 이번
   │    ├─ +0x710  PsInitializeThreadRngState   난수 상태                     ← 이번
   │    └─ +0x71f  ObInitializeFastReference    소속 가게 참조                ← 이번
   ├─ +0xfc2  PspInsertProcess                 상가 명부 (run51)
   └─ +0x101a PspInsertThread                  (Q78d4 쪽)
 test.exe 메인 스레드: PspUserThreadStartup → ntdll 로더 → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다. 호출 기호 없이 들여쓴 줄은 그 함수 안 쓰는 줄이다.
- 사진 지점으로 보면 이번 칸은 모두 **`K1~K2`** 또는 **끝까지 0** 이다.

### 무엇이 궁금했나

짐 지도에서 `_ETHREAD` 는 149칸 중 아는 칸이 **5**, `_KTRAP_FRAME` 은 66칸 중 **6** 뿐이었다. 그중 "만들 때" 몫 169칸을 닫는다.

1. `K1~K2` 30칸: 칸마다 쓰는 줄과 **값의 출처**.
2. 끝까지 0 139칸: 네 가지로 가르기.
3. 갈림길과 간접 호출.
4. 칸마다 짐 지도 3장 양식.

- 범위 밖: `_ETHREAD` 뒤 구간 10칸 · `_KTRAP_FRAME` `P1~P1b` 25칸 (Q78d4), `_TEB` (Q78d3), 리스트에 나중에 달리는 항목 내용.

### 바퀴 전에 알던 것

- `[정적]` (run11) `StartAddress`·`Win32StartAddress` 를 CONTEXT 에서 옮겨 적는다.
- `[정적]` (run16) `KxContextToKframes` 가 `Rip`·`Rsp`·`Rcx`·`Rdx` 를 출발 메모로 옮긴다. `+0xb5` 쪽에서 `ContextFlags` 를 "손본다" 는 요약만 있었다.
- `[정적]` (run52) 몸통 `memset` `+0x307`. 둘째 `KeInitializeApc` (`+0x368`) 의 대상이 `KiUpdateTebApc` 쪽이고 ETHREAD 칸이라는 것.
- `[정적]` (run52 넘김) `+0x187 memset` → `+0x198`·`+0x1f1`.
- `[관찰]` (run44) 이번 칸들의 K2 값. `SegCs 0x33`·`SegSs 0x2b`·`EFlags 0x200` 은 값만 알고, `SegCs`·`SegSs` 쓰는 줄은 짐작이었다.

---

## 2. 찾은 것

### 2.1 한눈에: 두 사람, 두 장

run 문서 "호출 관계" 를 그대로 옮겼다. 모두 `[정적]`.

```
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread
         │  +0x2e7 ObCreateObjectEx → +0x30c memset
         │  +0x31a EnergyValues · +0x3cc RundownProtect · +0x410 ReadClusterSize · +0x41a MmSlabIdentity
         ├─ +0x432  KeInitializeSemaphore      KeyedWaitSemaphore·AlpcWaitSemaphore
         │  +0x439 ~ +0x4e7 리스트 머리 여덟·락 칸 다섯·Silo
         ├─ +0x4c4  KeQuerySystemTimePrecise   CreateTime
         ├─ +0x6f9  KeInitThread
         │       ├─ +0x301  KiInitializeContextThread
         │       │       │  +0x157 ~ +0x15f ContextFlags 줄이기 · +0x166/+0x16e 로컬 SegCs·SegSs
         │       │       │  +0x18c memset(트랩 프레임) → +0x198 ExceptionActive
         │       │       ├─ +0x1b1  KxContextToKframes  EFlags·Rip·Rsp·SegSs·SegCs·Rax·Rcx·Rdx·R8~R11·Rbp
         │       │       │  +0x1f1 MxCsr
         │       └─ +0x36d  KeInitializeApc   UpdateTebApc
         ├─ +0x710  PsInitializeThreadRngState → RtlInitializeTRng   RngState·UsedByRngState
         └─ +0x71f  ObInitializeFastReference  ProcessFastRef
```

비유로 한 번:

- **인사 카드 뒷면**: 접수 창구가 지우개로 민 다음, 빈 서류함 여덟 개와 입사 시각, 기본값 몇 개를 적는다.
- **출발 메모**: 비서가 메모 자리를 지우고, 출근 위치·레지스터만 옮겨 적게 한다. 이때 "옮겨 적을 칸 체크표" 에서 수학 도구·디버그 칸 체크를 **미리 지워서** 넘긴다. 그래서 그 칸들은 늘 빈칸이다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 펜으로 0 을 적는 줄

`PspAllocateThread +0x3cc` `MOV [RSI+0x588],RDI` `[정적]`

- **누가**: `PspAllocateThread` (접수 창구). `RSI` = 새 스레드, `RDI` = 값 0 인 레지스터.
- **어디서**: `+0x3cc`. 몸통 지우개 `+0x307` 보다 **뒤**다. (바로 앞 `+0x3c7` `memset` 은 몸통 지우개와 다른 호출이다.)
- **무엇을**: `+0x588` = `RundownProtect` 칸에 0 을 쓴다.
- **왜 눈여겨보나**: 지우개로 이미 0 인 칸에 **또 0 을 적는다.** 이런 줄이 있는 칸만 "0 을 씀" 이다 → 2.4.

#### 한 줄 읽기 ②: 체크표를 고치는 줄

`KiInitializeContextThread +0x157` `AND EAX,6` `[정적]` (오케스트레이터 `.asm` 확인)

- **누가**: `KiInitializeContextThread` (비서).
- **어디서**: `+0x157`. 바로 뒤 `+0x15a` 가 `OR EAX,0x100001`, `+0x15f` 가 저장한다.
- **무엇을**: 복사해 둔 CONTEXT 의 `ContextFlags` 에서 `0x2`·`0x4` 비트만 남기고 다 지운다.
- **무엇을 위해**: 이 값이 `KxContextToKframes` 로 넘어가 **어느 블록을 탈지** 정한다 → 2.7.

#### 한 줄 읽기 ③: 메모 값이 아니라 상수를 적는 줄

`KiInitializeContextThread +0x1f1` `MOV [R11+0x2c],0x1f80` `[정적]`

- **누가**: 비서. `R11` = `R15 − 0x190` = 트랩 프레임 (`+0x1b5` 가 계산).
- **어디서**: `+0x1f1`. `KxContextToKframes` (`+0x1ac`) 에서 돌아온 **뒤**다.
- **무엇을**: 트랩 프레임 `+0x2c` `MxCsr` 에 상수 `0x1f80`.
- **왜 눈여겨보나**: `KxContextToKframes` 에도 `MxCsr` 를 쓰는 줄 (`+0x310`) 이 있지만, 그 줄은 FP 블록 안이라 이 길에선 안 탄다. 그래서 값은 CONTEXT 가 아니라 이 상수에서 온다.
  - CONTEXT 에도 `MxCsr` `0x1f80` 이 적혀 있긴 하다 ([run16 해설 2.4](../02_sprint/run16-thunk-context-해설.md)). 값이 같아서 사진만으론 출처를 못 가른다. 출처를 가른 건 코드다.

### 2.3 169칸은 어디로 갔나

#### 표 (run 문서·결과서 맨 앞 표 그대로)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_ETHREAD` `K1~K2` | 27 | **27** | — | — | — | — | 0 |
| `_ETHREAD` `끝까지 0` | 107 | — | **6** | **10** | **22** | **69** | 0 |
| `_KTRAP_FRAME` `K1~K2` | 3 | **3** | — | — | — | — | 0 |
| `_KTRAP_FRAME` `끝까지 0` | 32 | — | **1** | **13** | **12** | **6** | 0 |
| **합** | **169** | **30** | **7** | **23** | **34** | **75** | **0** |

#### 한 줄 읽기: `_KTRAP_FRAME` 끝까지 0 줄

- 32칸 모두 사진 K2 ~ P2 일곱 장 내내 0 이었다.
- 1칸 (`Rbp`) 은 입력값 0 을 옮겨 적는다. 13칸은 쓰는 줄이 있는데 안 탄다 (2.7).
- 12칸은 쓰는 줄이 없다. 6칸은 다른 길이 쓰는 것 같다 `[추측]`.

#### 짐 지도로 옮길 때 (run52 와 같은 규칙)

- "아는 칸" 30 + "0 을 씀" 7 + "조건부 안 탐" 23 = **60** → "nt 가 씀".
- "아무도 안 씀" **34** → 2장 표 아래 줄.
- "범위 밖 길에서만 씀 `[추측]`" **75** → 모름에서 빼고 따로 한 줄.
- 그리고 이미 아는 `SegCs`·`SegSs` 가 짐작 → 정확 (2.14).

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_ETHREAD` (149칸) | 5 → **48** | 0 → **22** | 0 → **69** | 144 → **10** | 5 + 0 → **48 + 0** |
| `_KTRAP_FRAME` (66칸) | 6 → **23** | 0 → **12** | 0 → **6** | 60 → **25** | 4 + 2 → **23 + 0** |

- 남은 모름 35 = `_ETHREAD` 10 (`K2~K3` 5 · `K3~P0` 4 · `P1~P1a` 1) + `_KTRAP_FRAME` 25 (`P1~P1a` 24 · `P1a~P1b` 1) → **Q78d4**.
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 596 → **656** (정확 539 → **601** · 짐작 57 → **55**) + 아무도 안 씀 98 → **132** + 범위 밖 길에서만 씀 123 → **198** + 모름 639 → **470**.

### 2.4 핵심 (1): `memset` 이 칸을 0 으로 밀었는데 왜 "0 을 씀" 이 아닌가

이 질문이 자연스럽다. 천천히 가 보자.

#### 어디가 이상해 보이나

- `+0x307 memset` 은 몸통 `0x798` + 확장 영역 전부를 민다 (크기는 `ObCreateObjectEx` 에 넘긴 객체 크기 그대로. 사용자 스레드 `0xd70`) `[정적]`.
- `+0x187 memset` 은 트랩 프레임 `0x190` 바이트를 민다 `[정적]`.
- 그러면 끝까지 0 139칸은 **전부** 누군가 0 을 넣은 셈이다. 그런데 왜 "0 을 씀" 이 7칸뿐인가?

#### 규칙: 지우개는 안 세고, 펜만 센다

"0 을 씀" 은 **그 칸을 쓰는 줄이 따로 있는 것**만 친다. 세 가지다 (run 문서 "셈 방식").

| 종류 | 뜻 | 이번 예 | 이번 수 |
|---|---|---|---|
| 명시 0 저장 | 값 0 을 그 칸에 콕 집어 쓴다 | `RundownProtect` `+0x3cc`, 락 칸 다섯 | 6 |
| 입력값 쓰기 | 입력을 옮겨 적는데 입력이 0 이었다 | `Rbp` `KxContextToKframes +0x12a` (`CONTEXT.Rbp`) | 1 |
| 같은 DWORD 되쓰기 | 묶음을 읽어 일부만 고쳐 통째 쓴다 ([run52 해설 2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나)) | 이번엔 만들기 길 안에 없음 | 0 |

- `memset` 은 칸을 가리지 않고 통째로 민다. 그래서 "모든 칸의 **출발점**" 으로 보고 칸마다의 쓰는 줄로 안 친다.
- 비유: 지우개는 카드 전체를 지나간다. 우리가 세는 건 그 뒤에 **펜이 닿았느냐**다.

`Rbp` 를 보면 왜 펜이 중요한지 보인다.

- 값은 0 이다. 하지만 이 줄은 `CONTEXT.Rbp` 를 옮겨 적는 줄이라, 메모에 다른 값이 있었으면 다른 값이 들어갔을 자리다.
- 지우개로만 0 인 칸은 그렇지 않다.

#### 다르게 세면 숫자가 어떻게 되나

요청서엔 "몸통 `memset` 이 덮으면 0 을 씀" 이라는 주의 문구가 있었다. 결과서는 그 문구대로 센 숫자도 같이 적었다.

| | 지금 셈 (결과서 주 표) | `memset` 도 세면 (대안) |
|---|---|---|
| 0 을 씀 | **7** | **139** (`_ETHREAD` 107 · 트랩 프레임 32) |
| 조건부 안 탐 | **23** | 따로 안 셈. "조건부 갈래도 있음" 으로 덧붙임 (23칸) |
| 아무도 안 씀 | **34** | 없어짐 (139 안으로) |
| 범위 밖 길 `[추측]` | **75** | 따로 안 셈. "범위 밖 길에서도 씀" 으로 이름만 덧붙임 (75칸, 같은 칸) |
| 아는 칸에 들어가는 수 | `30 + 7 + 23` = **60** | `30 + 139` = **169** |

- 바뀌는 건 **이름표**다. 칸의 값 (0), 코드에 있는 줄, 사진은 그대로다.
- 그런데 이름표가 바뀌면 잃는 게 있다 (결과서): **네 가지 가름이 사라진다.**
  - "0 을 씀 139" 라고만 적으면, 펜을 댄 `RundownProtect` 와 아무도 안 건드린 `MmReserved` 가 같은 줄에 선다.

#### 왜 이쪽을 골랐나

1. run50·run52 가 이미 이 방식으로 셌다 (결과서).
2. 요청서 문구는 그 앞 바퀴들과 어긋났던 것이다 (run 문서).
3. 그래서 오케스트레이터가 결과서 주 표를 따르기로 했다 (2026-10-30, 오케스트레이터 기본값, plan 결정 기록). 대안 숫자는 run 문서 맨 앞에 남아 있다.

#### 조심할 점: "아무도 안 씀" 은 "안 지워도 된다" 가 아니다

- 트랩 프레임 "아무도 안 씀" 12칸의 0 은 **`+0x187 memset` 에서 온다** `[정적]`. 이 줄이 0 을 보장하는 **유일한 줄**이다.
  - 스택은 새것이지만, 앞서 0 이었는지는 정적으로 모른다.
  - K1 사진의 같은 주소엔 옛 내용이 있었다 (`ExceptionActive` `0xb3`, `Dr3` `0x200001` …). K2 에서 한꺼번에 0 이다.
- `_ETHREAD` 쪽도 "아무도 안 씀" 22칸의 값 0 은 `+0x307 memset` 에서 온다.
- 그러니 "아무도 안 씀" 은 정확히는 **"그 칸을 골라 쓰는 줄이 없음"** 이다.
- 내 짐작 `[추측]`: 에뮬레이터가 이 칸들을 흉내 낼 때 두 `memset` 은 빼면 안 될 것 같다. 이유: 지우개가 없으면 그 자리에 있던 값이 남을 수 있어서다. 판단은 사용자 몫이다.

### 2.5 `_ETHREAD` `K1~K2` 27칸: 인사 카드 뒷면에 적는 것

칸마다 줄·값·경로는 **짐 지도 3.13**. 여기선 묶어서 본다. 모두 `[정적]` 이고 사진 K2 와 칸마다 맞는다.

#### 쓰는 순서 (결과서)

`memset +0x307` → `EnergyValues +0x31a` → `RundownProtect +0x3cc` → `ReadClusterSize +0x410` → `MmSlabIdentity +0x41a` → `KeInitializeSemaphore +0x42d` → 리스트 머리들 `+0x439 ~ +0x4e7` (그 안 `CreateTime +0x4bf`) → `PsAssignThreadId +0x52d` → `KeInitThread +0x6f4` (`UpdateTebApc`) → `PsInitializeThreadRngState +0x70b` → `ObInitializeFastReference +0x71a`

- 여기 `+0x..` 는 ret 가 아니라 **그 줄 자리·call 자리**다. 2.1 트리의 ret 숫자와 5씩 다른 건 그래서다.
- 난수 상태가 뒤쪽인 데엔 까닭이 있다. 씨앗을 만들 때 `CreateTime`·`Cid`·`Teb`·`InitialStack` 을 쓰기 때문이다.

#### 묶음별로

**빈 서류함 8개** (자기 연결 리스트 머리) — `KeyedWaitChain`·`ActiveTimerListHead`·`IrpList`·`BoostList`·`DeboostList`·`PropertySet`·`OwnerEntryListHead`·`DisownedOwnerEntryListHead`

- 값 = **스레드 주소 + 칸 오프셋**. 머리가 자기를 가리키면 빈 목록이다 ([run18 해설 2.6](../02_sprint/run18-proc-init-setup-해설.md)).
- 사진으로 바로 맞춰 볼 수 있다: `KeyedWaitChain` `…5548` = `…5080 + 0x4c8`.

**상수 3개**

| 칸 | 값 | 줄 | 뜻 |
|---|---|---|---|
| `ReadClusterSize` | `0xf` | `+0x410` | 읽기 앞당김 크기 기본값 |
| `MmSlabIdentity` | `0xff` | `+0x41a` | 2.6 에서 다시 본다 |
| `Silo` | `−3` | `+0x49f` | "아직 실로 안 정해짐" 표시로 보임 `[추측]` |

**도우미가 쓰는 칸**

- `CreateTime` (`KeQuerySystemTimePrecise`): 입사 시각. 공유 데이터 시각 + 성능 카운터 보정.
- `KeyedWaitSemaphore`·`AlpcWaitSemaphore` (`KeInitializeSemaphore`): 이름은 둘이지만 같은 32바이트다.
  - **세마포어** = 횟수를 세는 신호등으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 켜짐·꺼짐만 있는 [신호등](../02_sprint/run13-resume-thread-해설.md) 에 "몇 번 통과 가능" 숫자가 붙은 것.
  - 이번 값 `[정적]`: `SignalState` 0, `Limit` 1.
- `RngState`·`UsedByRngState` (`PsInitializeThreadRngState → RtlInitializeTRng`): 스레드 몫 **난수 상태**.
  - 스레드 자기 칸들·`RDTSC` 등을 XOR 해 씨앗을 만들고, splitmix64 로 8바이트 둘을 낸다 `[정적]`.
  - 비유: 직원마다 따로 가진 **제비뽑기 통**. 뽑을 때마다 상태가 밀린다 (`P0~P1` 이후 사진에서 바뀜. 소비 쪽은 `[추측]`, 이름만).
- `ProcessFastRef` (`ObInitializeFastReference`): `프로세스 주소 | 0xf`. 사진 `…b08f` = 프로세스 `…b080` + `0xf`.
  - **빠른 참조** = 포인터의 아래 4비트에 "미리 확보해 둔 참조 수" 를 같이 담는 값 `[정적]`. 프로세스 객체에 참조 15개를 미리 더한다 (`ObInitializeFastReference +0x1e`).
  - 비유: 소속 가게 번호 옆에 **미리 받아 둔 이용권 15장** 을 같이 적어 둔 것.
- `EnergyValues` (`+0x31a`): 켜져 있으면 스레드 + `0x798` (몸통 바로 뒤 [별지](run52-kthread-create-해설.md#24-출발점-카드--별지-몇-장-전부-0)) 를 가리킨다. 사진 `…5818` → 켜져 있음 `[관찰]`. 전역 값 자체는 D1.

**겹쳐서 같이 닫힌 칸**

- `ExitTime` = `KeyedWaitChain.Flink` 와 같은 8바이트. 그래서 스레드가 안 끝났는데도 `ExitTime` 자리에 포인터가 보인다.
- `PostBlockList` 뒤 8바이트 = `StartAddress`. 앞 8바이트 `ForwardLinkShadow` 는 쓰는 줄이 없어 0 이다 (아무도 안 씀 쪽).
- `Tcb` = 통째 `_KTHREAD`. 쓰는 줄이 따로 없고 안쪽 칸 (3.14, run52) 으로 셈.

#### `*` 칸 8개: 처음 값만 닫았다

- `Tcb`·`UpdateTebApc`·`Fill3~5`·`RngState`·`UsedByRngState`·`EFlags` 는 K2 뒤에 또 바뀐다.
- 이번엔 **처음 값 줄만** 닫았다. 뒤에 바꾸는 쪽은 이름만 (`KiUpdateSoftParkElectionStatisticsOnInsertion` `[추측]`, 풀 할당 쪽 난수 소비 `[추측]`, `PspSetContext +0x5a`·시스템 호출 진입 `[추측]`).

### 2.6 핵심 (2): `UpdateTebApc` 88바이트 안에 다른 칸이 겹쳐 있다

#### 먼저, 이 쪽지는 run52 의 그 호출이다

- `KeInitThread +0x339 LEA RCX,[RDI+0x720]` → `+0x368 KeInitializeApc` `[정적]`. run52 가 "둘째 `KeInitializeApc`" 라고 본 줄이다.
- 쪽지에 적는 내용:
  - `KernelRoutine` = 하는 일 없는 빈 함수 (rva `0x4a10c0`, 같은 주소 이름 100개)
  - `NormalRoutine` = `KiUpdateTebApc` (rva `0x312820`)
  - `RundownRoutine` = 0, `NormalContext` = 새 스레드
- 함수 주소를 **값으로 적어 둘 뿐**, 이 길에서 부르지는 않는다 `[정적]`. [run13 의 쪽지](../02_sprint/run13-resume-thread-해설.md) 와 같다.

#### 왜 칸 이름이 그렇게 많나

PDB 에서 `+0x720 ~ +0x777` 88바이트에 이름이 잔뜩 붙어 있다. 두 종류다.

**종류 1 — 범위만 다른 이름표 (`Fill`)**

[run52 해설 2.5](run52-kthread-create-해설.md#25-k1k2-42칸-네-사람--도우미-셋) 의 `*Fill*` 와 같은 꼴이다. 같은 바이트를 앞에서부터 **어디까지 덮느냐**만 다르다.

| 이름 | 크기 | 어디까지 |
|---|---|---|
| `UpdateTebApcFill1` | 3B | 앞 3바이트 (값 `0x580012`) |
| `UpdateTebApcFill2` | 4B | + `SpareByte1` |
| `UpdateTebApcFill3` | 64B | `NormalContext` 까지 |
| `UpdateTebApcFill4` | 72B | + `SystemArgument1` |
| `UpdateTebApcFill5` | 83B | + `SystemArgument2`·`ApcStateIndex`·`ApcMode`·`Inserted` |

**종류 2 — 쪽지가 안 쓰는 자리에 사는 칸**

이게 이번 절의 핵심이다. `_KAPC` 양식엔 `KeInitializeApc` 가 **안 쓰는 자리**가 있다. 그 자리에 ETHREAD 의 다른 칸이 산다 `[정적]`.

| 바이트 | 쪽지 양식에서 이 자리는 | 실제 사는 칸 | 누가 쓰나 |
|---|---|---|---|
| `0x720 ~ 0x722` | `Type`·`Size`·`AllFlags` | — | `KeInitializeApc` |
| `0x723` | `SpareByte1` (안 씀) | `Win32kPriorityFloor` | 만들기 길엔 없음. `PsAdjustWin32kPriorityFloor +0x4d` `[추측]` |
| `0x724 ~ 0x726` | `SpareLong0` 앞쪽 (안 씀) | `LastSoftParkElection*` 셋 | 만들기 길엔 없음. `KiUpdateSoftParkElectionStatisticsOnInsertion` `[추측]` |
| `0x727` | `SpareLong0` 맨 위 (안 씀) | **`MmSlabIdentity`** | `PspAllocateThread +0x41a` `0xff` |
| `0x728 ~ 0x75f` | `Thread`·`ApcListEntry` (안 씀)·루틴들·`NormalContext` | — | `KeInitializeApc` |
| `0x760 ~ 0x76f` | `SystemArgument1/2` (안 씀) | **`RngState`** (`0x768` = `UsedByRngState`) | `RtlInitializeTRng` |
| `0x770 ~ 0x772` | `ApcStateIndex`·`ApcMode`·`Inserted` | — | `KeInitializeApc` (값 0) |
| `0x773` | — | `UpdateTebSpareByte2` | 아무도 안 씀 |
| `0x774 ~ 0x777` | — | `HeapData` | 범위 밖 길 `[추측]` (오탐 가능) |

- 비유: 쪽지 양식에 **안 쓰는 여백**이 있다. 그 여백에 다른 담당자들이 자기 메모를 적어 둔다. 쪽지 쓰는 사람은 정해진 칸만 채우니까 여백 메모를 안 지운다.
- 왜 이렇게 겹쳐 놨나: 설계 이유는 근거에 없다. 안 쓰는 자리를 재활용해 공간을 아끼려는 것으로 보인다 `[추측]` (공용체의 일반적인 쓰임).

#### 순서가 맞아떨어지는지 보자

1. `+0x41a`: `MmSlabIdentity` 에 `0xff` — **쪽지보다 먼저** 적는다.
2. `+0x6f4 KeInitThread` → `+0x368 KeInitializeApc`: 쪽지 칸을 채운다. `SpareLong0` 은 안 쓰니 `0xff` 가 **살아남는다.**
3. `+0x70b`: `RngState` 를 쓴다. 쪽지가 `SystemArgument1/2` 를 안 쓰니 부딪히지 않는다.

#### 그래서 사진이 설명된다

- **덩어리 수**: 사진 K2 에서 `UpdateTebApc` 88바이트 중 0 이 아닌 8바이트 덩어리가 **7개**다.
  - 코드로 센 5 (`Type·Size` + `MmSlabIdentity`, `Thread`, `KernelRoutine`, `NormalRoutine`, `NormalContext`) + `RngState` 2 = 7.
- **`*` 표시**: 사진에서 `UpdateTebApc` 가 K2 뒤에 또 바뀐다. 얼핏 쪽지가 고쳐진 것 같지만, 바뀌는 바이트를 보면 **세입자 쪽**이다 (결과서).
  - `0x724 ~ 0x726` (`K3~P0`·`P0~P1`) = `LastSoftParkElection*` 자리
  - `0x760 ~ 0x76f` (`P1~P1a`·`P1a~P1b`·`P1b~P2`) = `RngState` 자리
- **`Fill3` 이 증거 하나를 더 준다**: `Fill3` 은 64B 라 `0x760` 앞에서 끝난다. 그래서 `Fill3` 의 바뀜은 `K3~P0`·`P0~P1` (`0x724 ~ 0x726` 만) 뿐이다. `Fill4`·`Fill5` 는 `RngState` 까지 덮어서 뒤 구간도 바뀐다.

#### 짐 지도 쪽 뜻

- `UpdateTebApc` 묶음 6칸은 칸 수로는 여섯이지만 **쓰는 줄은 `KeInitializeApc` 호출 하나**다.
- 내 짐작 `[추측]`: 에뮬레이터의 `KeInitializeApc` 가 88바이트를 통째로 0 으로 밀면 `MmSlabIdentity` `0xff` 가 지워질 것 같다. 이유: 진짜 함수는 `SpareLong0` 을 안 쓰고, 그 덕에 먼저 적은 값이 남기 때문이다.

### 2.7 핵심 (3): `ContextFlags` 를 줄여 넘겨서 FP·디버그 갈래가 안 탄다

이번 바퀴의 가장 큰 발견이다.

#### 새 말: `ContextFlags` (옮겨 적기 체크표)

- `ContextFlags` = CONTEXT 안 칸 하나. **어느 묶음을 옮길지** 비트로 표시한다 ([run16 해설](../02_sprint/run16-thunk-context-해설.md) 표의 "어느 칸을 옮길지").
- 비유: 업무 시작 메모 맨 위에 있는 **옮겨 적기 체크표**.

#### 옮겨 적는 함수는 체크표대로만 움직인다

`KxContextToKframes` 안엔 블록이 나뉘어 있고, 비트가 켜져 있을 때만 탄다 `[정적]`.

| 블록 | 조건 | 트랩 프레임 칸 |
|---|---|---|
| CONTROL | `& 0x100001` | `EFlags`·`Rip`·`Rsp`·`SegSs`·`SegCs` |
| INTEGER | `& 0x100002` | `Rax`·`Rcx`·`Rdx`·`R8~R11`, `Rbp` |
| XSTATE | `& 0x100040` + 모드 1 | — |
| CET | `& 0x100080` | `ShadowStackFrame` 읽기 |
| FLOATING_POINT | `& 0x100008` | `Xmm0~5`, `MxCsr` |
| DEBUG_REGISTERS | `& 0x100010` | `Dr0~3`·`Dr6`·`Dr7` |
| SEGMENTS (`0x4`) | **검사하지 않음** | `SegDs ~ SegGs` 안 씀 |

- 조건마다 `0x100000` 이 같이 붙어 있다. 코드는 "`0x100000` + 블록 비트" 가 **둘 다** 켜졌는지 본다 (`+0x21 ~ +0x35` 등).
  - `0x100000` 은 이 CONTEXT 가 64비트용이라는 표시로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

#### 그런데 비서가 넘기기 전에 체크표를 고친다

```
+0x157  AND EAX,6
+0x15a  OR  EAX,0x100001
+0x15f  저장
```

`KiInitializeContextThread` 안 줄이다 `[정적]` (오케스트레이터 `.asm` 확인). 식으로 쓰면 `(원래 & 6) | 0x100001`.

한 걸음씩:

1. **원래 값**: `PspCreateUserContext +0x5` 가 적은 `0x10000b`.
   - 비트로 풀면 `0x100000` + `0x8` (FP) + `0x2` (INTEGER) + `0x1` (CONTROL). 메모는 FP 도 옮겨 달라고 **체크해 뒀다.**
2. **`AND EAX,6`**: `0x2`·`0x4` 두 비트만 통과시키고 **나머지는 다 지운다.** FP 체크도 여기서 지워진다.
3. **`OR EAX,0x100001`**: `0x100000` 과 CONTROL `0x1` 을 **무조건** 켠다. 이 줄이 더하는 건 이 둘뿐이다.
4. **결과**: `0x100003`. 이 값이 `KxContextToKframes` 의 `R9D` 로 들어간다.

| 비트 | 블록 | 원래 `0x10000b` | 줄인 뒤 `0x100003` | 이 호출에서 |
|---|---|---|---|---|
| `0x1` | CONTROL | 켜짐 | **켜짐** (`OR` 가 켬) | **탐** |
| `0x2` | INTEGER | 켜짐 | **켜짐** (`AND` 가 통과) | **탐** |
| `0x4` | SEGMENTS | 꺼짐 | 꺼짐 | 검사 안 함 |
| `0x8` | FLOATING_POINT | **켜짐** | **꺼짐** (`AND` 가 지움) | **안 탐** |
| `0x10` | DEBUG_REGISTERS | 꺼짐 | 꺼짐 | **안 탐** |
| `0x40` | XSTATE | 꺼짐 | 꺼짐 | 안 탐 |
| `0x80` | CET | 꺼짐 | 꺼짐 | 안 탐 |

#### 왜 "입력과 상관없이" 인가

여기가 핵심이다.

- `AND EAX,6` 을 지나면 `0x8`·`0x10`·`0x40`·`0x80` 은 **입력이 뭐였든** 0 이 된다.
- 그 뒤 `OR` 가 더하는 건 `0x100001` 뿐이다. 네 비트가 다시 켜질 길이 없다.
- 입력에 비트가 다 켜져 있어도, 하나도 안 켜져 있어도 네 비트는 꺼진다.
- 그래서 이 호출 자리에선 네 블록이 **늘** 안 탄다. 실행 값을 안 봐도 코드 모양만으로 닫힌다 `[정적]`.
  - 값 확인 질문 D3 (`KxContextToKframes` 입구 `r r9d`, 기대 `0x100003`) 이 낮은 우선순위인 까닭이다.
- 반대로 INTEGER 는 **입력에 달려 있다.** 원래 값의 비트 1 이 켜져 있어서 탄다. 사진 K2 의 `Rcx` (진입점)·`Rdx` (PEB) 가 그 결과다.

비유 (태그 없음):

- 메모엔 "수학 도구 칸도 옮겨 적을 것" 에 체크돼 있다.
- 비서가 사본을 만들어, 그 체크를 **지우개로 지우고** 옮겨 적는 사람에게 건넨다.
- 옮겨 적는 사람은 받은 사본의 체크만 보니, 수학 도구 칸은 쳐다보지도 않는다.

#### 조심: "이 호출 자리" 에서만이다

- `KxContextToKframes` 함수 자체엔 FP·디버그 블록이 **있다.** 지워진 건 함수가 아니라 **이 호출에 넘긴 체크표**다.
- 다른 호출자는 `0x8` 을 켠 채 부를 수 있다 (`NtContinue`·`NtSetContextThread`) `[추측]`.

#### 그래서 무엇이 정해지나 (트랩 프레임 끝까지 0 32칸)

| 가름 | 칸 | 까닭 |
|---|---|---|
| **0 을 씀 1** | `Rbp` | INTEGER 블록이 탄다. `+0x12a` 가 `CONTEXT.Rbp` (0) 를 옮겨 적음 |
| **조건부 안 탐 13** | `Xmm0`·`Xmm2~5` (5) · `Dr0~3`·`Dr6`·`Dr7` (6) · `ShadowStackFrame`·`Spare` (2, 겹침) | 켜는 줄은 있는데 비트가 지워져 **늘** 안 탐 |
| **아무도 안 씀 12** | `P5`·`DebugControl`·`SegDs`·`SegEs`·`SegFs`·`SegGs`·`Fill0`·`Logging`·`Fill1`·`Fill2`·`Fill3`·`Fill4` | 만들기 길에 쓰는 줄 자체가 없음. 0 은 `memset` 에서 옴 |
| **범위 밖 길 6** `[추측]` | `PreviousIrql` · `LastBranchToRip`·`LastBranchFromRip`·`LastExceptionToRip`·`LastExceptionFromRip` · `TrapFrame` | `KiInterruptSubDispatch +0x20` 등 · `KiSaveDebugRegisterState +0x9e ~ +0xe3` · `KiServiceInternal +0x5b` |

세 가지만 짚자.

- **`Xmm*`·`Dr*` 의 "조건부" 는 뜻이 살짝 다르다.** 2.9 의 `_ETHREAD` 조건부는 "test.exe 입력이 그래서" 안 탄다. 여긴 **test.exe 와 무관하게** 이 자리에선 안 탄다.
- **`SegDs ~ SegGs` 는 왜 조건부가 아닌가.** `Xmm*`·`Dr*` 는 쓰는 줄이 있는데 안 지나는 것이고, `SegDs ~ SegGs` 는 `KxContextToKframes` 에 **쓰는 줄이 아예 없다.** `0x4` 를 검사하지도 않는다. CONTEXT 의 세그먼트 값 (`PspCreateUserContext +0x3d · +0x44`) 은 출발 메모로 안 간다.
- **`Xmm1` 이 왜 표에 없나.** 짐 지도 7.2 에서 `Xmm1` 은 `P1~P1a` 칸이라 이번 범위 밖이다 (Q78d4). `Rax`·`R8~R11` 도 같은 이유로 여기 안 들어간다. 이 칸들도 같은 INTEGER 블록이 쓰긴 한다.

#### `ExceptionActive`·`MxCsr` 와 코드 순서

- 순서 `[정적]`: `+0x187 memset` → `+0x198` `ExceptionActive` = 1 → `+0x1ac KxContextToKframes` → `+0x1f1` `MxCsr` = `0x1f80`.
- 사진: K1 은 옛 값 (`ExceptionActive` `0xb3`), K2 는 1 과 `0x1f80`.

#### 왜 이렇게 설계했나

- **모른다.** 코드는 "지운다" 는 것만 보여 준다. 왜 지우는지는 근거에 없다.

### 2.8 핵심 (4): 트랩 프레임 `EFlags` `0x200` 이 나오는 길

**사진 K2 의 `0x200` 은 메모에 적혀 있던 값이 아니다.** 입력은 0 이고, 비트 하나가 옮겨 적는 함수 안에서 켜진다.

식: `(CONTEXT.EFlags & 0x210dd5) | 0x200` `[정적]`

한 걸음씩:

1. **입력은 0 이다.** `NtCreateUserProcess +0xcc7 memset` 이 CONTEXT 를 민다. `PspCreateUserContext` 는 `EFlags` (+0x44) 를 안 건드린다.
   - run16 해설 표의 "안 적음 → 사용자 모드라 `0x200`" 이 바로 이 줄이다. 이번에 줄 단위로 닫혔다.
2. **CONTROL 블록을 탄다.** 2.7 의 `OR` 가 `0x1` 을 무조건 켰으니까.
3. **`KxContextToKframes +0x40`**: `CONTEXT.EFlags` 를 `R8D` 로 읽는다. 값 0.
4. **`+0x47`** `AND R8D,0x210dd5`: 허용된 비트만 남긴다. 0 이니 그대로 0.
5. **`+0x53`** `BTS R8D,9`: 비트 9 를 켠다. 이제 `0x200`.
6. **`+0x58`** `TEST R14B,R14B` / **`+0x5b`** `CMOVZ`: 모드 인자를 본다. 0 이면 다른 값으로 바꿔 끼우는 줄이다.
   - 이 호출의 모드는 1 이다 (`KiInitializeContextThread +0x193` 이 1 로 넘김). 그래서 안 바꾼다.
7. **`+0x5f`**: 트랩 프레임 `+0x178` 에 쓴다. → **`0x200`**. 사진 K2 와 같다.

왜 이 비트만 켜나:

- 결과서는 이 줄을 "사용자 모드: IF 비트 켬" 이라고 읽었다.
- **IF** = CPU 가 인터럽트를 받아도 되는지 나타내는 표시로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 출발 메모의 "근무 중 **전화 받기 켜 둠**" 칸. 메모를 쓴 사람이 뭘 적었든, 가게로 나가는 직원이면 옮겨 적는 사람이 켜 둔다.

그래서 읽는 법:

- 값이 `0x200` 인 까닭은 "입력 0 + 이 비트 하나" 다. 입력에 다른 비트가 있었으면 `0x210dd5` 안의 비트는 같이 들어갔을 것이다.
- `EFlags` 는 `*` 칸이다. 처음 값만 닫았다. 사진은 그 뒤 `P0~P1` `0x202` · `P1~P1a` `0x246` · `P1a~P1b` `0x202` 로 바뀐다. 바꾸는 쪽은 `PspSetContext +0x5a`·시스템 호출 진입 `[추측]`, 이름만.

### 2.9 `_ETHREAD` 끝까지 0 107칸

#### 0 을 씀 6: 값 0 인 `RDI` 를 명시 저장 `[정적]`

| 칸 | 줄 |
|---|---|
| `RundownProtect` | `+0x3cc` |
| `BoostListLock` | `+0x45c` |
| `IrpListLock` | `+0x483` |
| `ActiveTimerListLock` | `+0x48a` |
| `DisownedOwnerEntryListLock` | `+0x4d2` |
| `Win32kThreadLock` | `+0x4e7` |

- 비트 묶음 DWORD 를 되쓰는 줄은 만들기 길 안에 없다. 그래서 run52 처럼 묶음 전체가 "0 을 씀" 으로 넘어가는 일은 이번엔 없다.
- K1 의 `RundownProtect` 1 은 옛 객체 값이다 ([run52 해설 2.10](run52-kthread-create-해설.md#210-함정-k1-사진의-옛-객체)).

#### 조건부 안 탐 10: test.exe 가 탄 길

갈림길 다섯 `[정적]`. 값은 사진으로 맞춰 본 것 `[관찰 간접]`. 입력값 자체는 D1·D2 다.

| 갈림길 | 가르는 곳 | test.exe | 그래서 안 켜지는 칸 |
|---|---|---|---|
| 숨김 플래그 | 만들기 플래그 비트 2 (`PspAllocateThread +0x3eb`) | 꺼짐 | `CrossThreadFlags.HideFromDebugger` (`+0xb04`) |
| 프로세서 그룹 | `KeQueryMaximumGroupCount() > 1` (`+0x252 ~ +0x259`) | 1개 | `CrossThreadFlags.IndirectCpuSets`·`SelectedCpuSets`·`SelectedCpuSetsIndirect` (`+0xaea`·`+0xaf8`) |
| 피코·복제 | 사용자 스택 인자 = 0 (`+0x5ab ~ +0x5b3`) | 인자 ≠ 0 → 일반 길 | `SameThreadPassiveFlags.ClonedThread`·`UserFsBase`·`UserGsBase`·`SetContextState` |
| 시스템 스레드 | `ThreadContext == 0` (`+0x541`) | 사용자 스레드 | `SameThreadPassiveFlags.SecureContext` (`+0xbb2`) |
| PASID | `NpxState` 비트 10 (`KiInitializeContextThread +0x3f7 ~ +0x403`) | 없음 (`0xe5`) | `SameThreadApcFlags.PasidMsrValid` (`ExBuildPasidMsrForThread +0x1c`) |

- 칸 수: 1 + 3 + 4 + 1 + 1 = 10.
- 비유: "해당자만 기입" 칸 ([run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)). 이 직원은 숨김 대상도, 복제 직원도, 관리사무소 안 직원도 아니다.
- `SkipCreationMsg` 는 요청서에 조건부 후보로 적혀 있었지만, 만들기 길엔 켜는 줄이 없고 `DbgkpSetProcessDebugObject` 쪽에서 보여 "범위 밖 길" 로 갔다 `[추측]`.

#### 아무도 안 씀 22 · 범위 밖 길 69 `[추측]`

- 22칸 전체 목록과 69칸 표는 run 문서에 있다. 대표만:

| 칸 | 쓰는 곳 (범위 밖) `[추측]` | 어떤 길 |
|---|---|---|
| `ExitStatus` | `PspExitThread +0x100`, `PspTerminateThreadByPointer +0x6e` | 종료 |
| `ClientSecurity` | `PsImpersonateClient +0x14c`, `PsRestoreImpersonation +0x8f` | 가장하기 |
| `ThreadName` | `NtSetInformationThread +0x9df` | 이름 설정 |
| `SameThreadApcFlags.OwnsVadExclusive` | `MiLockVad`, `MiUnlockVad` | 주소 공간 락 |
| `AlpcMessage` | `AlpcpReceiveMessagePort` | ALPC |

- 아무도 안 씀 대표: `ForwardLinkShadow`, `LockOrderState`, `MmReserved`, `VerifierContext`, `CmThreadInfo`, `UpdateTebSpareByte2`. 값 0 은 `+0x307 memset` (2.4).
- 비트 묶음의 셈: `CrossThreadFlags` 19비트 = 조건부 2 · 아무도 안 씀 3 · 범위 밖 14. `SameThreadPassiveFlags` 칸 15 = 조건부 2 · 아무도 안 씀 4 · 범위 밖 9. `SameThreadApcFlags` 영역 16 = 조건부 1 · 아무도 안 씀 4 · 범위 밖 11.

#### 다음 조각에서 숫자가 바뀔 수 있는 곳

- `PspInsertThread +0x617 ~ +0x649` 가 `CrossThreadFlags` 를 읽어 비트 9~14 만 갈아 끼우고 **DWORD 째 되쓴다** `[정적]`.
- 이 줄은 만들기 길 밖이라 이번엔 안 셌다. Q78d4 에서 비트 칸 19개의 가름이 바뀔 수 있다.
  - run52 해설 2.7 의 "체크박스 띠를 오려 내서 통째로 다시 붙이기" 가 여기서 또 나오는 셈이다.

### 2.10 "아무도 안 씀" 34 · "범위 밖 길" 75 의 한계

방법과 비유는 [run52 해설 2.9](run52-kthread-create-해설.md#29-핵심-3-아무도-안-씀-21--범위-밖-길-70--그리고-이-둘의-한계) 그대로다. 결론도 같다: **"초기화 동안 쓰는 줄이 없다" 만 `[정적]` 이고, 두 묶음의 경계는 약하다.**

이번에 달라진 것만 적는다.

#### 나아진 것: 먼 조각을 따라갔다

- **먼 조각** = 함수 본문에서 멀리 떨어진 자리에 있는 코드 조각. 오프셋이 `PspAllocateThread +0x2ca9a9` 처럼 크게 나온다.
  - 비유: 부서 서류철 맨 뒤가 아니라 **다른 창고에 따로 보관한 몇 장**.
- sast/46 훑기는 함수 크기 안만 봐서 이런 데의 쓰기를 놓쳤다. 이번엔 점프를 따라가 주인 함수에 붙였다. 덕분에 `PspInsertThread +0x9d6` (`SelfTerminate`) 같은 줄이 잡혔다.
- 다만 주인이 불확실한 `FUN_…` 조각은 남는다.

#### 이번에 특히 약한 곳

- `_ETHREAD` 오프셋은 EPROCESS·Job·ALPC 와 숫자가 많이 겹친다 (예: `0x4e8`·`0x5b8`·`0x6e8`). → 오탐.
  - 실제 사례: `MmCreateTeb +0x175 · +0x17e` 를 훑기가 `EtwSupport` 로 오인했다. 실은 TEB 쪽 기준이다. `MmLockOrdering` ← `MmStoreRegister`, `MmReserved`·`ForwardLinkShadow` ← Job 쪽 함수도 다른 구조체 오탐으로 봤다.
- 트랩 프레임은 기준 레지스터가 `rbp = 프레임 + 0x80` 이라 보정해서 훑었다. 오탐이 많아서 (`P5` 45곳 등) 이름이 칸 뜻과 맞는 것만 "범위 밖" 에 넣었다.
- `LEA` 로 주소를 만든 뒤 쓰는 줄은 여전히 못 잡는다. 후보: `AlpcWaitListEntry`·`CmCallbackListHead` `[추측]` (지금은 "아무도 안 씀" 쪽).

### 2.11 간접 호출은 없다 · 부모 칸은 안 센다

- 만들기 길 `.asm` **26개**에서 `CALL reg`·`CALL [mem]`·`JMP reg` **0개** `[정적]`. → Q35 와 무관.
- `KeInitThread +0x34a` (`KiUpdateTebApc`)·`+0x35b` (빈 함수) 는 함수 주소를 **값으로** 넘길 뿐이다 (2.6).
- `KxContextToKframes +0x3ba` (`BTR.LOCK [RCX],0x18`, `RCX` = `gs:[0x188]`) 는 **지금 도는 부모 스레드** 칸이라 안 셌다. [run52 해설 2.12](run52-kthread-create-해설.md#212-부모-스레드-칸은-새-스레드-짐이-아니다) 와 같은 함정이다.

### 2.12 곁가지: `PsAssignThreadId` 는 한 칸만 쓴다

- `PsAssignThreadId` 는 `Cid.UniqueThread` 하나만 쓴다 (`+0x18`) `[정적]`.
- `Cid.UniqueProcess` 는 `PspAllocateThread +0x3da` 가 쓴다 `[정적]`.
- 사번을 받는 창구에서 가게 번호까지 같이 적는 줄 알았다면, 그건 아니라는 뜻이다. 가게 번호는 접수 창구가 직접 적는다.

### 2.13 줄임말 주의

- 결과서의 `①C`·`①KC` 는 짐 지도의 `①KC…` (`KiInitializeContextThread`)·**`①KX…`** (`KxContextToKframes`, 새 줄임) 다.
- 짐 지도의 `①C…` 는 run50 의 `MmCreateProcessAddressSpace` 라서, 결과서 줄임을 그대로 옮기면 겹친다. 이 글은 함수 이름으로 적었다.

### 2.14 정정 (run 문서 "정정")

| 무엇 | 전 | 후 |
|---|---|---|
| 짐 지도 3.15 `SegCs`·`SegSs` | 짐작. 경로 칸이 `KiInitializeContextThread +0xb5` | **정확**. 트랩 프레임에 쓰는 줄 `KxContextToKframes +0xa7`·`+0x91`, 로컬 CONTEXT 에 미리 쓰는 줄 `KiInitializeContextThread +0x166`·`+0x16e` |
| `KiInitializeContextThread +0xb5` | CONTEXT 를 꺼내 손본다 (요약) | `+0xb5` 는 `ThreadContext` 읽기 줄 (`MOV R12,[R13+0x20]`). 손보는 줄은 `+0x157 ~ +0x16e` |
| 짐 지도 6장 `ObCreateObjectEx` | `+0x2e7` | 호출 `+0x2e2`, ret `+0x2e7` 로 적음. flow.md 의 `+0x2e7` 은 ret 규칙이라 그대로 |
| 결과서 줄임 | `①C`·`①KC` | 짐 지도 줄임과 겹쳐서 옮길 때 바꿈 (2.13) |

- run16 문서는 안 고쳤다. 그 문장은 요약으로 틀리지 않아서다 (run 문서). 이전 run 문서 맨 위 정정 안내: **없음**.

### 2.15 정리: 비유로 한 번 더

1. 접수 창구 (`PspAllocateThread`) 가 빈 카드를 받아 **뒷면까지 지우개로 민다** (`+0x307`).
2. 몇 칸은 펜으로 0 을 또 적고 (`RundownProtect`, 락 칸 다섯), 기본값을 적는다 (`0xf`, `−3`).
3. 쪽지 자리의 **여백**에 `0xff` 를 미리 적어 둔다 (`MmSlabIdentity`).
4. 빈 서류함 여덟 개를 세우고, 입사 시각을 적는다.
5. 팀장 (`KeInitThread`) 을 부른다. 그 안에서 비서 (`KiInitializeContextThread`) 가 움직인다.
6. 비서가 업무 시작 메모의 사본을 만들어 **체크표를 고친다.** 수학 도구 칸 체크가 여기서 지워진다.
7. 출발 메모 자리를 지우개로 밀고, "예외 활성" 칸에 1 을 적는다.
8. 옮겨 적는 사람 (`KxContextToKframes`) 이 체크된 묶음만 옮긴다. `EFlags` 엔 "전화 받기 켜 둠" 을 직접 켠다.
9. 비서가 `MxCsr` 에 기본값 `0x1f80` 을 적는다.
10. 팀장이 둘째 쪽지 (`UpdateTebApc`) 를 쓴다. 정해진 칸만 쓰니 여백의 `0xff` 는 남는다.
11. 창구로 돌아와 제비뽑기 통 (`RngState`) 을 채운다. 이 통도 쪽지 여백에 산다.
12. 소속 가게 번호 + 이용권 15장 (`ProcessFastRef`) 을 적는다.
13. 숨김·복제·시스템 스레드 칸은 해당이 아니라 비운다.
14. 나머지 칸은 나중에 다른 부서가 채우거나 (`[추측]`), 아무도 안 쓴다.

### 2.16 곁가지: 도구 메모

run 문서 "도구 메모" 에서 — 하네스 제안이라 오케스트레이터가 사용자에게 묻는다.

- **제안 1 `field_writers.py`** (다시, 더 구체적으로): 먼 조각 주인 붙이기, 비트 묶음 풀기, 기준 레지스터 보정.
- 제안 2: `pdb_struct.py` 의 겹치는 칸을 "같은 바이트 묶음" 으로 보여 주는 옵션. 2.6 의 표를 이번엔 손으로 맞췄다.
- 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주기.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run53 없음 | run53 추가. "(run53) 정적 바퀴, K2 ~ P2 일곱 장 다시 읽음. K1 의 스레드·트랩 프레임 자리는 옛 값이라 대조에 안 씀" 한 줄 |
| 2.1 트리 (`PspAllocateThread` 아래) | `(5) +0x6f9 KeInitThread` 앞뒤에 ETHREAD 도우미 없음 | `·` 줄 `+0x432 KeInitializeSemaphore`·`+0x4c4 KeQuerySystemTimePrecise`·`+0x710 PsInitializeThreadRngState`·`+0x71f ObInitializeFastReference` `[정적]`, "그림엔 없음" |
| 2.1 트리 (`KeInitThread` 아래) | `+0x301` 아래 `+0x1b1 KxContextToKframes` 만 | `·` 줄 `+0x18c memset` (트랩 프레임) · `+0x36d KeInitializeApc` (`UpdateTebApc`) `[정적]` |
| 2.1 `PspAllocateThread` 절 | ETHREAD 줄은 Q78d2 로 넘김 메모 | `(해결됨 → run53)` + `[정적]` 새 묶음: `_ETHREAD` 칸 대부분을 이 함수가 쓴다 · test.exe 가 안 타는 갈래 · `PsAssignThreadId` 는 한 칸 |
| 2.1 `KeInitThread` 절 | 둘째 `KeInitializeApc` 대상만 | `[정적]` 둘째 호출 = ETHREAD `UpdateTebApc` |
| 2.1 `KiInitializeContextThread` | `+0xb5` 가 손본다 (요약), 트랩 프레임 줄 넘김 메모 | `[정적]` 손보는 줄 `+0x157 ~ +0x15f` · `+0x166` · `+0x16e`, `EFlags`·`SegSs`·`SegCs` 줄, **FP·디버그 블록은 입력과 무관하게 늘 안 탄다**, `MxCsr` 상수, 코드 순서 |
| 4장 | run52 "다음 조각에 넘김" | Q78d2 부분 `(해결됨 → run53)` + 해결 줄 + (run53 새로) 실행 확인 D1 → Q79, D2 ~ D4 → Q100 + (run53 새로) 다음 조각에 넘김 (Q78d3·Q78d4) |
| 5장 | — | run53 줄 둘 |

- 그림 (mermaid) 은 안 바뀌었다. 새 단계·새 caller 가 없고 대표 지점만 늘었다 (run 문서 "그림").

태그 변화 대표:

- `SegCs`·`SegSs` 쓰는 줄: 짐작 (경로 칸 틀림) → **`[정적]` `KxContextToKframes +0xa7`·`+0x91`**.
- `EFlags` 쓰는 줄: "안 읽음" → **`[정적]` `KxContextToKframes +0x5f`**.
- `MxCsr` 값의 출처: 줄만 앎 → **`[정적]` CONTEXT 값이 아니라 상수**.
- `_ETHREAD`·`_KTRAP_FRAME` 169칸: 모름 → `[정적]` 줄 (60) / 부재 (34) / `[추측]` (75).
- 조건부 갈림길 입력: `[관찰 간접]` (D1·D2 로 직접 확인 가능).

### 짐 지도 `baggage.md` 에서 바뀐 것 (열한째판)

- **3.13 `_ETHREAD` 를 다시 썼다**: 줄 6 → **31**. 줄 끝 "**Q78d2 로 닫힘**".
- **3.15 `_KTRAP_FRAME` 을 다시 썼다**: 줄 11 → **19**. 머리에 `ContextFlags` 줄이기.
- 머리 메모 "Q78d4 에서 볼 것" (3.13·3.15), Q78d3 넘김 (3.3 머리).
- 새 줄임 `①KX…`.
- 7.4 `SegCs`·`SegSs` 줄 `(해결됨 → run53)`. 7.6 에 칸 표 없는 짐 후보 **기록만** (에너지 칸 `+0x798 ~ +0x860`, 그룹 2개 이상일 때 붙는 CPU 집합 칸).
- 새 **7.16** = 이번 동적 질문 (D1 ~ D4).
- 2장 숫자는 2.3 표 그대로.

### 큰 그림에서 어떻게 이어지나

- **출발 메모가 어떻게 만들어지는지 끝까지 보였다.** run15 가 "나갈 때 이 메모를 읽는다", run16 이 "`Rip`·`Rcx`·`Rdx` 를 이렇게 적는다" 를 봤다. 이번엔 **어떤 칸은 왜 안 옮겨지나**까지 닫혔다.
  - 그리고 이 메모의 `Rip`·`Rcx`·`Rdx` 등은 뒤에 `PspSetContext` 가 바꿔 끼운다 (run16 해설 2.6). 사진에서 `EFlags` 가 `P0~P1` 에 바뀌는 것도 그 쪽 이름이 보인다 `[추측]`.
- **첫 스레드 커널 쪽 만들 때 몫이 거의 닫혔다.** `_KTHREAD` (run52) + `_ETHREAD`·`_KTRAP_FRAME` (이번). 남은 건 TEB 와 그 뒤 구간이다.
- **값의 출처가 또 부모 쪽이 아니다.** 상수·자기 주소 계산·전역 시각·난수·CONTEXT 다. run52 해설 3절에서 본 것과 같은 모습이다.
- 내 짐작 `[추측]`: 에뮬레이터 쪽에서 `KxContextToKframes` 를 짓는다면, 호출 전 비서의 `(원래 & 6) | 0x100001` 도 같이 흉내 내야 `Xmm*`·`Dr*` 가 같은 값으로 남을 것 같다. 이유: 체크표를 안 줄이면 원래 값 `0x10000b` 의 `0x8` 때문에 FP 블록을 타게 되기 때문이다.
- 다음 조각 **Q78d3** 은 같은 직원의 개인 안내판 (`_TEB`) 이다. `SameTebFlags` (`PspAllocateThread +0x793`) 등이 넘어가 있다.
- 그 뒤 **Q78d4** 에서 `PspInsertThread` 의 `CrossThreadFlags` 되쓰기, `KeStartThread` 쪽을 본다.

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** 조건은 dast-mentor 몫이다. **급한 것은 없다** — 안 닫아도 3.13·3.15 칸 표는 (Q78d4 몫 빼고) 완성돼 있다.

번호 D1 ~ D4 는 결과서 그대로다. run52 의 D1 ~ D4 와 **다른 질문**이다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** 커널 전역 값: `PopEnergyEstimationEnabled` (기대 1) · `KiMaximumGroups` (기대 1) · `KeFeatureBits` 비트 `0x38` · KUSD `+0x3d8` · KUSD `+0x14` | 실행 때 값. 지금은 사진 값으로 거꾸로 맞춘 것뿐 (`[관찰 간접]`) | 덤프 **Q79**. `IndirectCpuSets`·`PasidMsrValid`·`EnergyValues` 의 까닭이 닫힘 |
| **D2** 만들기 플래그 (기대 `0x70`) · 사용자 스택 인자 (기대 ≠ 0) | 사진은 호출 뒤 값만 있음 | 멈춤 **Q100** (낮음): `nt!NtCreateUserProcess+0xe57`. 조건: 지금 도는 프로세스 = cmd.exe |
| **D3** `KxContextToKframes` 입구 `r9d` 가 `0x100003` 인가 | 코드로 이미 닫힘. 값만 확인 | 멈춤 **Q100** (낮음) |
| **D4** 사진 K1 자리 옛 객체·옛 스택 값이 왜 남아 있나 | 판정에 안 씀 | **Q100** (낮음) |
| 왜 `ContextFlags` 를 줄이나 (설계 이유) | 코드는 "지운다" 만 보여 줌 | 근거 없음. 지금 대기열에 없는 질문이다 |
| "범위 밖 길" 75 · "아무도 안 씀" 34 의 쓰는 함수가 맞나 | 오프셋 숫자 훑기 (오탐·놓침, 2.10) | 작은 정적 — `.asm` 로 한 줄씩 |
| `_ETHREAD` 뒤 구간 10칸 · `_KTRAP_FRAME` `P1~P1b` 25칸 | 이번 범위 밖 | 정적 **Q78d4** — `PspInsertThread` 줄 넘김 (`+0x2cd`, `+0x617 ~ +0x649`, `+0x6e3`, `+0x96f`, `+0x9d6`, `+0x2ca1d6`) |
| `CrossThreadFlags` 비트 칸 19개의 가름 | `PspInsertThread` 되쓰기는 만들기 길 밖이라 안 셈 (2.9) | 정적 **Q78d4** |
| `UpdateTebApc` 바이트 `0x724 ~ 0x726` · `LastSoftParkElection*` 쓰는 곳 | 이름만 봄 `[추측]` | 정적 **Q78d4** |
| `UpdateTebApc` 쪽지가 언제 펼쳐지나 | 이 길에선 부르지 않음. 이번 질문 밖 | 근거 없음. 지금 대기열 항목으로 적혀 있지 않음 |
| `_TEB` (`SameTebFlags` `+0x793`, 복제 갈래 TEB 쓰기) | 이번 범위 밖 | 정적 **Q78d3** |
| 칸 표 없는 짐 (에너지 칸 `+0x798 ~ +0x860`, CPU 집합 칸) | 체크리스트 21종 밖 | 짐 지도 7.6 에 **기록만** |

- 대기열 순서 (plan): **Q78d3** → Q78d4 → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q100 은 맨 뒤, 낮은 우선순위.

---

## 5. 더 읽기

- run 문서: [`run53-ethread-create.md`](run53-ethread-create.md) — 요약 표와 셈 방식, 호출 지점 표, 호출 관계 트리, 몸통 0 채움, `K1~K2` 30칸, 끝까지 0 가르기 (조건부 표·범위 밖 길 표·`KxContextToKframes` 블록 표), test.exe 가 탄 길, 간접 호출, 정정, 아직 확인 필요
- 결과서: [`_workspace/sast/47_ethread-create_result.md`](../../_workspace/sast/47_ethread-create_result.md) — 판단 기준과 셈 방식, 1.1 · 1.2 칸마다 표, 2.1 ~ 2.5 끝까지 0 가르기, 3 간접 호출, 4 줄기와 경로, 동적 질문, 넘길 것, 정정 1 ~ 9
- 요청서: [`_workspace/sast/47_ethread-create.md`](../../_workspace/sast/47_ethread-create.md)
- 짐 지도: [`baggage.md`](baggage.md) — 2장 · **3.13 `_ETHREAD`** · **3.15 `_KTRAP_FRAME`** · 3장 줄임 `①KX…` · 7.2 · 7.6 · **7.16**
- 흐름도: [`flow.md`](flow.md) — 기준, 2.1 트리, `nt!PspAllocateThread` 절, `nt!KeInitThread` 절 (`KiInitializeContextThread` 문단), 4장 run53 줄, 5장
- 진행표: `_workspace/plan.md` Q78d2 (끝) · Q78d3 · Q78d4 · Q79 · Q100, 결정 기록 2026-10-30
- 앞 해설:
  - [run52](run52-kthread-create-해설.md) 2.4 ~ 2.10 — 별지, 겹치는 칸, 네 가름, DWORD 되쓰기, 훑기 한계, 옛 객체
  - [run16](../02_sprint/run16-thunk-context-해설.md) 2.4 · 2.5 — CONTEXT 와 옮겨 적기
  - [run15](../02_sprint/run15-user-thread-startup-해설.md) 2.6 — 출발 메모와 `IRETQ`
  - [run13](../02_sprint/run13-resume-thread-해설.md) 2.3 — 쪽지와 신호등
  - [run11](../02_sprint/run11-allocate-thread-해설.md) — 첫 직원 뽑기 다섯 걸음
