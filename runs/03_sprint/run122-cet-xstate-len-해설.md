# run122 cet-xstate-len — 해설

> 이 글은 [run122 문서](run122-cet-xstate-len.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q105**: CET 스레드의 맡긴 문맥 길이 (작은 정적, 원래 "필요할 때만"). `RtlpGetEntireXStateAreaLength2` (rva `0x4eea6c`, 77바이트) 를 풀어서 CET 길의 `0x32f + 이 함수 값` 을 숫자로 (`_workspace/plan.md`). 이번에 **끝났다**.
  - [run55](run55-context-length2-해설.md) 가 넘긴 항목이다. run55 "아직 확인 필요" ② 가 이번에 **닫혔다**.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 남은 것이 간 곳 (4절): 동적 → 새 **Q213 ① ~ ③** (모두 선택), 정적 → 새 **Q214 ① ②** (둘 다 낮음).
  - 다음 문서 차례: **Q112 + Q113 + Q115** (sast/119). plan.md "다음 항목" 줄 순서다.
- 근거:
  - run 문서 [`run122-cet-xstate-len.md`](run122-cet-xstate-len.md) (가장 중요)
  - 요청서 [`_workspace/sast/118_cet-xstate-len.md`](../../_workspace/sast/118_cet-xstate-len.md) · 결과서 [`118_cet-xstate-len_result.md`](../../_workspace/sast/118_cet-xstate-len_result.md). 판정은 **분석성공**, 스크립트 문제 없음, 동적 필수 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글만 바뀌었다: 기준 · 2.3 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **기준 줄 한 줄만** 더했다. 2장 숫자 그대로, 새 짐 칸 없음.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 사진에 이미 있던 값**이다 (KUSD `XState` 칸). 새로 찍은 사진이 아니다.
- **짐 지도 바퀴가 아니다.** 길이는 짐 칸이 아니라 `PspInitializeThunkContext` 지역 `[RBP]` 값이다 (run55, baggage.md 기준 줄).

---

### 이번 글의 말: 이미 푼 것은 링크만

이번 바퀴의 두 주인공 말, **CET** 와 **XState** 는 앞 해설에서 이미 풀었다. 링크만 건다.

- **CET 그림자 스택** = 함수가 돌아갈 주소를 CPU 가 따로 한 벌 더 적어 두고 맞나 보는 보호 기능. 비유는 **경비실 장부** → [run29 해설 2.9](../02_sprint/run29-continue-start-해설.md#29-cet-그림자-스택--미리-올려-둔-한-칸과-맞춰-보기)
- **XState** = 스레드가 멈출 때 레지스터 말고도 더 담아 둬야 하는 큰 계산 장치 상태들. 비유는 업무 시작 메모 뒤에 붙는 **별지** → [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md) ("핵심 ① 체크박스는 정말 `0x10001b`")
- **XState 성분** = 별지 안을 기능별로 나눈 **칸 하나하나**. CET_U 는 **11번** 성분 (마스크 bit 11 = `0x800`) → [run111 해설 2.7](run111-cet-context-small-해설.md) ("사물함으로 옮길 때는 복사만 한다")
- **XSAVE 영역** (스레드가 쉴 때 CPU 확장 상태를 넣어 두는 자리 = **개인 짐 상자**) → [run52 해설 2.5](run52-kthread-create-해설.md) "새 말: `StateSaveArea`"
- `KUSER_SHARED_DATA` (KUSD) = **공용 게시판**. 그 안 `XState` 칸 = **"별지 양식 안내"** → [run55 해설 2.4](run55-context-length2-해설.md) ("`r9` 상수의 뜻")
  - 게시판의 `XState` 칸을 부팅 때 누가 적나 (`KiInitializeXSaveConfiguration`, 840B 통째 복사) → [run115 해설](run115-kusd-boot-해설.md) 호출 나무 `ⒷX`
- `ContextFlags` = 메모 맨 위 **체크박스** (`0x10001b` / `0x10005b`) → [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md)
- **분기 칸** (`Length3` 의 `[RSP+0x20]`, 검사원이 찍어 주는 **확인 도장**) → [run55 해설 2.7](run55-context-length2-해설.md) ("비-CET 길: `0x4ff` 가 나오는 순서")
- run44 사진, KUSD 를 커널 주소 `fffff78000000000` 로 다시 읽은 **보충 명령 파일** → [run44 해설 2.9](run44-baggage-snap-해설.md) ("읽다 막힌 것과 보충")

이번에 **처음 나오는 말** 은 셋이다. 본문에서 처음 나올 때 한 줄씩 푼다.

- **기능 마스크** → 2.3
- **압축 (`CompactionEnabled`)** → 2.4
- **64바이트 올림 (`AlignedFeatures`)** → 2.5

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 스레드 = **직원**, CONTEXT = **업무 시작 메모**, `ContextFlags` = 메모 맨 위 **체크박스**.
- 유저 스택에 맡긴 원래 메모 = **사물함 메모** (run16).
- CET 그림자 스택 = **경비실 장부** (run29), XState = 메모 뒤 **별지** (run42), XState 성분 = 별지의 **칸** (run111).
- KUSD = **공용 게시판**, 그 안 `XState` = **"별지 양식 안내"** (run55).
- run55 에서 본 장면: 관리사무소가 원래 메모를 사물함에 넣기 전에 **메모 크기**를 잰다. 장부를 안 쓰는 직원은 늘 `0x4ff`.
- **이번에 더하는 그림**:
  - 장부 쓰는 직원 (CET) 의 메모엔 별지가 붙는다. 별지 크기는 게시판의 **"칸별 크기 표"** (`AllFeatures`) 를 보고 정한다.
  - 이번에 그 **표 읽는 법** (`RtlpGetEntireXStateAreaLength2`) 을 풀었다.
  - 이 VM 게시판으로 재면 별지 몫이 `0x80` 더 붙어서 **`0x57f`** 다.
- run 문서는 이번에 별지를 "**CPU 상태 붙임 종이**", 게시판을 "**회사 공용 게시판**" 으로 불렀다. 이 글은 앞 해설들과 맞추려고 **별지 · 공용 게시판** 으로 부른다. 그림은 같다.

### 한 줄로 먼저: 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 태그 |
|---|---|---|
| 1. `RtlpGetEntireXStateAreaLength2` 는 무엇을 돌려주나 | `ControlFlags` bit 1 (`CompactionEnabled`) **꺼짐** → `XState.Size` 그대로. **켜짐** → `0x240` 에서 시작해 마스크 비트 `i` (2 ~ 63) 마다 (정렬 비트면 64 올림) `AllFeatures[i]` 를 더함. 읽는 칸은 **넷뿐** | `[정적]` |
| 2. 이 VM 에서 CET 스레드 길이는 | 마스크 `0x800` → `F = 0x240 + 0x10 = 0x250` → **`0x32f + 0x250 = 0x57f`**. 비-CET 는 run55 그대로 **`0x4ff`** (차이 `0x80`) | `[정적 계산 + 관찰 입력]` |
| 3. 누가 이 함수를 부르나 | caller **8곳**. 시동 길에서 닿는 건 **둘** (`Length3 +0xd1` · `RtlInitializeExtendedContext2 +0x15f`), 둘 다 XState 요청 비트가 켜질 때만 | `[정적]` |
| test.exe 첫 스레드는 | 비-CET 라 이 함수를 **아예 안 부른다** | `[정적]` (+ run44 `ContextFlags 0x10001b`) |
| 우리 문서 정정 | **없다.** run55 의 "CET 면 `0x32f + …`" 은 그대로 맞고, 이번에 그 안이 풀렸다 | — |

이 글에서 붙잡아 둘 세 줄:

> **(1) 별지 크기는 "켜진 칸 크기의 합" 이다. 시작값 `0x240` 에 칸마다 크기를 더한다 (게시판이 "압축" 이라고 할 때).**
>
> **(2) CET 직원은 칸 하나 (11번, CET_U) 만 더 붙이고, 그 칸은 이 VM 에서 `0x10` 바이트다. 그래서 `0x57f`.**
>
> **(3) test.exe 첫 스레드는 장부를 안 써서 이 셈을 아예 안 탄다. 그림도, 짐 지도 숫자도 그대로다.**

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → NtResumeThread
 test.exe 첫 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup                     출근 도장 (run15)
   └─ +0xb3  PspInitializeThunkContext                         바꿔 끼우기 (run16)
        ├─ +0xa7  RtlGetExtendedContextLength2                 메모 크기 재기 (run55)
        │       └─ +0x13  RtlGetExtendedContextLength3
        │               └─ (CET 갈래만) +0xd6  RtlpGetEntireXStateAreaLength2   ← 이번
        ├─ (+0x17a ~ +0x187)  사물함 자리 = 원래 Rsp − 크기
        └─ +0xf5  RtlInitializeExtendedContext2                사물함 메모 틀 채우기
                └─ (CET 갈래만) +0x164  RtlpGetEntireXStateAreaLength2       ← 이번 (2.11)
  ═▶ IRETQ
 test.exe 첫 스레드 (유저)
  ntdll!LdrInitializeThunk → 로더 … → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다. 괄호 친 `(+0x17a ~ +0x187)` 은 호출이 아니라 함수 안 줄이다.
- flow.md 로는 **2.3 `PspInitializeThunkContext` 절 3번** ("맡길 자리 = 원래 `Rsp` − 문맥 길이") 의 **CET 갈래 안쪽**이다.
- 중요한 점: **test.exe 첫 스레드는 이 갈래를 안 탄다** (비-CET). 그래서 "← 이번" 두 줄은 "CET 스레드였다면" 의 이야기다.
  - 그림 (mermaid) 에 없는 글 속 함수라서 **그림은 안 바꿨다** (run 문서 "지도 범위").

### 왜 이 질문이 지금 왔나

- run55 에서 "길이 = `0x4ff`" 를 정적으로 셌다. 단 **비-CET 일 때만**이었다.
- CET 스레드면 `0x32f + RtlpGetEntireXStateAreaLength2(…)` 라는 것까지만 봤다. 그 함수 안은 범위 밖이라 안 풀었다.
- test.exe 는 비-CET 로 보여서 "필요할 때만" 으로 Q105 에 미뤄 뒀다.
- 이번엔 사용자 결정 (2026-11-15) "남은 정적 분석 전부" 의 차례가 와서 돌았다. 함수 하나 (77바이트) 짜리 작은 바퀴다.

### 무엇이 궁금했나

요청서 질문 셋:

1. `RtlpGetEntireXStateAreaLength2` 가 돌려주는 값의 **식**. 게시판 `XState` 의 어느 칸을 읽고, 어떻게 더하나.
2. 그 식에 **이 VM 의 게시판 값** (run44 사진) 을 넣으면 CET 스레드 길이가 몇인가. 비-CET `0x4ff` 와 나란히.
3. 이 함수를 **부르는 자리 전부**와, CET 갈래로 들어가는 조건 한 줄.

### 바퀴 전에 알던 것

- `[정적]` (run55) 비-CET (`ecx = 0x10001b`, `r8 = 0`) 길이 = **`0x4ff`**. 게시판을 안 읽는 상수.
- `[정적]` (run55) CET (`ecx = 0x10005b`, `r8 = 0x800`) 면 `Length3` 가 XState 블록을 타서 `0x32f + RtlpGetEntireXStateAreaLength2(…)`. 함수 안은 안 풂.
- `[정적]` (run55) `Length2` 는 넷째 인자 `r9` 를 상수 `0xFFFFF780000003D8` (= `&SharedUserData->XState`) 로 바꿔 끼운다.
- `[관찰]` (run44) 사진 8장에 KUSD 가 통째로 찍혀 있다 (보충 로그 `dq fffff78000000000 L0x150`). 그 안에 `XState` 840B 도 있다.
- `[관찰]` (run44) test.exe 첫 스레드의 맡긴 문맥 `ContextFlags` = `0x10001b` → 비-CET 로 보인다.
- `[정적]` (run111) 맡긴 문맥의 CET_U 칸은 두 qword (`[+0]` · `[+8]`).

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 "호출 관계" 그대로, 조금 줄였다. **CET 스레드일 때** 의 모양이다. `(*)` 줄은 test.exe 첫 스레드가 **안 타는** 줄이다.

```
PspUserThreadStartup
 └─ +0xb3  PspInitializeThunkContext
         │  +0x69 ~ +0x8d  _KTHREAD+0x74 bit20 → R14D 0x800 · R12D 0x10005b
         ├─ +0xa7  RtlGetExtendedContextLength2   (run55)
         │       └─ +0x13  RtlGetExtendedContextLength3
         │               ├─ +0x2f  RtlpValidateContextFlags2   분기 칸 3 (비-CET 1)
         │               │  +0x81  bit 1 켜짐 → XState 블록 (*)
         │               ├─ +0xc6  RtlpRemoveArchDisallowedXStateFeatures   (*) 마스크 0x800 그대로
         │               ├─ +0xd6  RtlpGetEntireXStateAreaLength2   (*) F = 0x240 + AllFeatures[11] = 0x250
         │               │  +0xf1  *rdx = 0x32f + F = 0x57f   (비-CET 0x4ff)
         │  +0x17a ~ +0x187  R15 = (원래 Rsp − 길이) & ~0xf
         └─ +0xf5  RtlInitializeExtendedContext2
                 ├─ +0x154  RtlpRemoveArchDisallowedXStateFeatures   (*)
                 ├─ +0x164  RtlpGetEntireXStateAreaLength2   (*) 0x250
                 └─ +0x178  memset   (*) 0x50 바이트
```

- 모두 `[정적]` 이다. 숫자 (`0x250`, `0x57f`, `0x50`) 는 `[정적 계산]` 이다. 디버거로 본 적은 없다.
- 호출 기호 (`├─`, `└─`) 옆 `+0x..` 는 ret 자리, 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄이다.
- 새로 뽑은 함수는 **둘**: `RtlpGetEntireXStateAreaLength2` (질문 1), `RtlpRemoveArchDisallowedXStateFeatures` (58바이트 — 숫자를 닫으려면 CET 비트가 걸러지는지 봐야 해서 결과서가 더함). 둘 다 안에서 부르는 함수가 없다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: `├─ +0xd6  RtlpGetEntireXStateAreaLength2   (*) F = 0x240 + AllFeatures[11] = 0x250`

- **누가**: `RtlGetExtendedContextLength3` — 메모 크기를 실제로 재는 사람 (run55).
- **어디서**: `+0xd1` 에서 부르고, 돌아올 자리가 `+0xd6` 이다. XState 블록 안이다.
- **누구를**: `RtlpGetEntireXStateAreaLength2` — 이번에 푼 함수. 별지 크기를 게시판 표로 셈한다.
- **무엇을 위해**: CET 직원의 메모에 붙는 **별지 크기 `F`** 를 알려고. 넘기는 것은 둘 (`+0xcb ~ +0xd1`):
  - `rcx` = 기능 마스크 (이번엔 `0x800`)
  - `rdx` = `RSI` = 게시판 `XState` 칸 주소 (run55 의 `r9` 상수가 여기서 쓰인다)
- `(*)` 라서 test.exe 첫 스레드는 이 줄에 **안 온다.** 태그 `[정적]`, 값 `0x250` 은 `[정적 계산]`.

#### 한 줄 읽기 ②: `│  +0xf1  *rdx = 0x32f + F = 0x57f   (비-CET 0x4ff)`

- **누가**: 또 `Length3`.
- **어디서**: 자기 안 `+0xf1` 줄. 부르는 줄이 아니라 **쓰는** 줄이다.
- **누구를**: 아무도 안 부른다. `PspInitializeThunkContext` 가 건네준 메모지 (`rdx` 가 가리키는 `[RBP]`) 에 숫자를 적는다.
- **무엇을 위해**: 관리사무소가 사물함을 **얼마나 비울지** 알려 주려고. CET 면 `0x57f`, 비-CET 면 `0x4ff` 가 적힌다.
- 길이는 반환값이 아니라 메모지에 적힌다는 건 run55 에서 본 그대로다 ([run55 해설 2.5](run55-context-length2-해설.md) "길이는 반환값이 아니다").

### 2.3 새 말 — 기능 마스크: "이번 별지에 넣을 칸 목록"

- **기능 마스크** = 64비트 숫자 하나로 "어느 XState 성분을 넣을까" 를 표시한 것. 비트 `i` 가 켜져 있으면 `i` 번 성분을 넣는다.
- 비유: 별지 칸 64개 앞에 붙은 **체크 표**. 체크된 칸만 별지에 들어간다.
- 이번 함수의 `rcx` 가 이 마스크다 (`+0x3` 에서 `R10` 에 보관) `[정적]`.
- CET 직원이면 마스크 = **`0x800`** (= bit 11 하나, CET_U 성분) 이다 ([run111 해설 2.7](run111-cet-context-small-해설.md)).

### 2.4 식 ① — 첫 갈림길: 게시판이 "압축" 이라고 하나

함수는 맨 처음 게시판 `XState` 의 **`ControlFlags`** (`+0x14`) 를 읽는다 (`+0x0`) `[정적]`. 그리고 **bit 1** 을 본다 (`+0x6 ~ +0xd`).

**새 말 — 압축 (`CompactionEnabled`)**

- `ControlFlags` bit 1 의 이름이다 (`pdb_struct.py _XSTATE_CONFIGURATION`). 별지를 **어떤 양식으로** 쓰는지 정하는 스위치로 읽힌다.
- 일반적으로 알려진 설명 `[추측]`: 압축 꺼짐 = 모든 칸이 **정해진 자리**에 있는 양식 (안 쓰는 칸도 자리는 비워 둠). 압축 켜짐 = **쓰는 칸만 앞에서부터 붙여 쓰는** 양식.
- 비유: 꺼짐 = 칸마다 자리가 인쇄된 **고정 양식 종이**. 켜짐 = 필요한 칸만 차례로 붙이는 **이어 붙이는 메모지**.
- 이 비유는 코드가 보여 주는 동작 (아래) 과 앞뒤가 맞는다. 다만 "양식" 이라는 뜻 자체는 코드에 쓰여 있지 않다.

갈림길 둘 `[정적]`:

| bit 1 | 가는 곳 | 돌려주는 값 | 마스크를 보나 |
|---|---|---|---|
| **꺼짐** | `+0xd JZ` → `+0x4a` | `XState.Size` (`+0x10`) 그대로 (`+0x4a`, `+0x4d` RET) | **안 본다** |
| **켜짐** | `+0xf` 부터 계속 | 칸별 크기의 합 (2.5) | 본다 |

- 꺼짐 갈래가 마스크를 안 보는 이유는 위 비유로 읽으면 자연스럽다: 고정 양식이면 어떤 칸을 쓰든 **종이 크기는 늘 같다**. (이 "이유" 는 비유로 하는 해석이다.)
- **이 VM 은 켜짐이다** (`ControlFlags = 3`, 2.7) `[관찰]`. 그래서 아래 2.5 갈래를 탄다.

### 2.5 식 ② — 켜짐 갈래: `0x240` 에 칸 크기를 하나씩 더한다

`[정적]` (`.asm` `+0xf ~ +0x48`, run 문서 표 그대로 줄임):

| 줄 | 하는 일 | 읽는 XState 칸 |
|---|---|---|
| `+0xf` | `R9` ← 정렬 표시 모음 | `AlignedFeatures` (`+0x220`) |
| `+0x16` | 시작값 `F = 0x240` | (상수) |
| `+0x1b` | 표 읽기 손가락을 `AllFeatures[2]` (`+0x234` = `0x22c + 2×4`) 에 | `AllFeatures` |
| `+0x22 ~ +0x2e` | `i` 번 비트가 마스크에 **없으면** 건너뜀 (`+0x3d` 로) | |
| `+0x30 ~ +0x38` | `AlignedFeatures` 에 `i` 가 **있으면** `F` 를 64 의 배수로 올림 | |
| `+0x3b` | `F += AllFeatures[i]` | `AllFeatures[i]` (`+0x22c + 4i`) |
| `+0x3d ~ +0x46` | `i++`, 다음 칸, `i < 0x40` 이면 다시 | |
| `+0x48` | RET (`EAX = F`) | |

순서로 풀면:

1. `F = 0x240` 에서 시작한다.
2. `i = 2` 부터 `63` 까지 하나씩 본다.
3. 마스크에 `i` 가 체크돼 있지 않으면 → 다음 `i`.
4. 체크돼 있으면:
   - 그 칸이 "64바이트 경계에서 시작해야 하는 칸" 이면 먼저 `F` 를 올림한다.
   - 그 칸 크기 `AllFeatures[i]` 를 `F` 에 더한다.
5. 다 돌면 `F` 를 돌려준다.

**새 말 — 64바이트 올림 (`AlignedFeatures`)**

- `AlignedFeatures` 는 비트 모음이다. `i` 번 비트가 켜져 있으면 `i` 번 칸은 **64 의 배수 자리에서 시작**해야 한다는 뜻으로 읽힌다.
- 계산은 `(F + 0x3f) & ~0x3f` — `F` 를 64 의 배수로 **올림**한다 `[정적]`.
- 비유: 어떤 칸은 꼭 **새 줄 맨 앞**에서 써야 한다. 앞 칸이 줄 중간에서 끝났으면, 그 줄 남은 자리는 비우고 다음 줄로 넘어간다.
- (run55 해설 2.11 의 "16바이트 맞춤 `& ~0xf`" 은 **내림**이었다. 이번 것은 `+0x3f` 를 먼저 더하니 **올림**이다. 단위도 16 이 아니라 64.)
- **이 VM 은 `AlignedFeatures = 0`** 이라 올림이 한 번도 안 일어난다 (2.7) `[관찰]`.

이 함수가 읽는 게시판 칸은 **넷뿐**이다 `[정적]`:

- `Size` (`+0x10`) · `ControlFlags` (`+0x14`) · `AlignedFeatures` (`+0x220`) · `AllFeatures[i]` (`+0x22c + 4i`)
- `EnabledFeatures` · `EnabledSupervisorFeatures` · `AllFeatureSize` 는 **안 읽는다.** 마스크를 거르는 건 부르는 쪽 몫이다 (2.6).
- 비유: 이 함수는 **"칸별 크기 표" 를 보고 더하기만** 한다. 어느 칸을 체크할지는 이미 정해서 들고 와야 한다.

왜 `0x240` 에서 시작하고 `i` 는 2 부터인가:

- 코드는 "그렇게 한다" 까지만 보여 준다. 뜻은 이름에 없다.
- 일반적으로 알려진 설명 `[추측]`: XSAVE 영역 맨 앞에는 옛 기능 (성분 0 · 1) 을 담는 고정 구역 `0x200` 과 머리 `0x40` 이 늘 있어서, 이 둘을 합친 `0x240` 이 시작값이고 성분 0 · 1 은 따로 더하지 않는다.
- 이 짐작과 앞뒤가 맞는 근거 하나: 같은 값을 받은 `RtlInitializeExtendedContext2` 가 `F − 0x200` 을 쓴다 (2.11). 다만 run 문서는 이 뜻을 `[추측]` 으로만 둔다.

### 2.6 마스크는 어디서 오나 — `0x800` 이 그대로 남는다

이 함수는 마스크를 받기만 한다. 그럼 `Length3` 는 무엇을 넘기나 `[정적]`:

1. **출발점**: `PspInitializeThunkContext` 가 CET 직원이면 `r8 = 0x800` 을 준다 (`+0x72 ~ +0x79` `R14D = 0x800`, `+0x98` `r8d ← R14D`). run55 에서 본 그대로다.
2. **검사원 도장**: `RtlpValidateContextFlags2` (`ecx = 0x10005b`)
   - `+0x4d ~ +0x8e`: `0x10005b & 0x100040 = 0x100040` → "별지 요청" 으로 본다.
   - `+0x90`: 게시판 `XState.EnabledFeatures` 를 읽는다. 이 VM `0xe7` ≠ 0 → `+0x96` 도장 = 3.
   - `+0x9c ~ +0xa7`: `0x10005b & 0x100080 = 0x100000` ≠ `0x100080` → CET 커널 검사 (`+0xa9` `KiKernelCetEnabled`) 는 안 탄다.
   - `+0xc2`: `Length3` 의 분기 칸 `[RSP+0x20] = 3`.
3. **`Length3` 가 블록을 탄다**: `+0x81` 분기 칸 bit 1 켜짐 → XState 블록.
4. **첫 번째 거르기** (`Length3 +0x9b ~ +0xb7`, 압축 켜짐일 때만):
   - 마스크 = `r8 & (EnabledUserVisibleSupervisorFeatures | EnabledFeatures | bit63)`
5. **두 번째 거르기** (`+0xc1` `RtlpRemoveArchDisallowedXStateFeatures`):
   - `+0x0 BT ECX,0x10` 꺼짐 → `+0x15 BT ECX,0x14` 켜짐 → `RAX = 0x4000000000060dff` → `+0x10 AND [RDX],RAX`.
   - `0x800 & 0x4000000000060dff = 0x800` — bit 11 이 그대로 남는다.
6. **부름**: `+0xcb ~ +0xd1` `rcx = 마스크`, `rdx = RSI` (게시판 `XState` 주소) 로 이번 함수.

비유로:

- 직원이 "장부 칸 (11번) 하나 붙여 주세요" 하고 체크 표를 낸다 (`0x800`).
- 관리사무소는 그 체크 표를 **두 번 걸러** 본다. 첫째, "이 컴퓨터에서 켜진 칸인가" (게시판). 둘째, "이 종류 메모에 붙여도 되는 칸인가" (`RtlpRemoveArch…` 의 고정 목록).
- 이 VM 에선 11번이 두 번 다 통과한다. 그래서 그대로 `0x800`.

### 2.7 이 VM 의 게시판 값 — run44 사진에서

run44 덤프 읽기 보충 로그 `ms/logs/04_baggage-read_supp_P1.txt` 의 `:286` `dq fffff78000000000 L0x150` 에서 읽었다 `[관찰]`. **8장 모두 줄 번호 · 값이 같다** (flow-writer 가 맞춤).

대표만 (셈에 쓰는 칸 + 검산에 쓰는 칸):

| XState 칸 | 값 | 무엇에 쓰나 |
|---|---|---|
| `EnabledFeatures` (+0x0) | `0xe7` (비트 0 · 1 · 2 · 5 · 6 · 7) | 검사원 도장 (≠ 0) · 첫 거르기 · 검산 |
| `Size` (+0x10) | `0x980` | 꺼짐 갈래 값 · 검산 |
| `ControlFlags` (+0x14) | `3` (`OptimizedSave` 1 · `CompactionEnabled` 1) | 2.4 갈림길 → **켜짐** |
| `AlignedFeatures` (+0x220) | `0` | 2.5 올림 → **없음** |
| `AllFeatureSize` (+0x228) | `0x990` | 검산 |
| `AllFeatures[2 · 5 · 6 · 7]` | `0x100 · 0x40 · 0x200 · 0x400` | 검산 |
| **`AllFeatures[11]`** (CET_U) | **`0x10`** | **셈의 핵심** |
| `EnabledUserVisibleSupervisorFeatures` (+0x330) | `0x800` | 첫 거르기 (bit 11 통과) |

- 전체 표 (KUSD 주소 · 로그 줄 번호까지) 는 [run 문서 2절](run122-cet-xstate-len.md) 에 있다.
- 결과서는 사진을 KUSD 주소로만 적었고, 로그 줄 번호는 flow-writer 가 로그로 맞췄다. 칸 이름과 값 짝은 결과서와 **다 같다** (run 문서 "도구 메모").

#### 한 줄 읽기 ③: 게시판 한 줄에서 두 칸을 꺼내기

run 문서 표의 `Size` / `ControlFlags` 줄: 로그 `:349` 둘째 qword `00000003'00000980`.

- `dq` 는 8바이트씩 찍는다. 그 8바이트 안에 **4바이트 칸 둘** 이 들어 있다.
- 아래 4바이트 (`+0x10`) = `00000980` → `Size = 0x980`.
- 위 4바이트 (`+0x14`) = `00000003` → `ControlFlags = 3` → bit 0 · bit 1 둘 다 켜짐.
- 그래서 한 qword 에서 "꺼짐 갈래 값" 과 "갈림길 스위치" 를 같이 얻는다.

### 2.8 셈 — `0x57f`

`[정적 계산]` (입력은 2.7 의 `[관찰]`):

1. **마스크** = `0x800 & (0x800 | 0xe7 | bit63)` = `0x800` → 두 번째 거르기 후에도 `0x800` (2.6).
2. **`F`**:
   - 시작 `0x240`.
   - `i = 2 … 63` 중 마스크에 있는 건 **`i = 11` 하나**뿐.
   - `AlignedFeatures = 0` 이라 올림 없음.
   - `+ AllFeatures[11] = 0x10` → **`F = 0x250`**.
3. **길이** = `0x32f + 0x250` = **`0x57f`** (1407 바이트).

나란히 (이 VM, run 문서 표 그대로):

| | 비-CET (test.exe 첫 스레드로 보임) | CET 스레드 |
|---|---|---|
| `ecx` / `r8` | `0x10001b` / `0` | `0x10005b` / `0x800` |
| 분기 칸 `[RSP+0x20]` | 1 (XState 블록 건너뜀) | 3 (탐) |
| 이번 함수 호출 | 없음 | `rcx = 0x800`, `rdx = &XState` → `0x250` |
| 맡긴 문맥 길이 | **`0x4ff`** (run55) | **`0x57f`** |

- 차이 `0x80` (= `F − 0x1d0`).

### 2.9 `0x32f` 는 어디서 오나 — run55 길에 블록 하나가 끼는 것

`Length3` 가 하는 셈을 단계로 `[정적]`:

| 단계 | 줄 | 값 |
|---|---|---|
| 기본 크기 | `+0x4d ~ +0x7e` (run55 와 같은 길) | `EBX = 0x4f0`, `R11D = 0x10` |
| 블록 들어가며 16 맞춤 | `+0x88 ~ +0x92` | `(0x4f0 − 1 + 0x10) & ~0xf = 0x4f0` (그대로) |
| 이번 함수 결과 더함 | `+0xd6 ~ +0xdf` | `0x4f0 − 0x10 − 0x1c0 + F = 0x320 + F` |
| 분기 칸 bit 2 꺼짐 (`3 & 4 = 0`) | `+0xe1`, `+0xe9 CMOVZ` | `0x320 + F` |
| `− 1 + 0x10` | `+0xec ~ +0xee` | **`0x32f + F`** |
| 씀 | `+0xf1` | `*rdx` |

- 끝의 `− 1 + 0x10` 은 run55 비-CET 길에서도 봤던 그 단계다 (그때는 `0x4f0` → `0x4ff`). [run55 해설 2.9](run55-context-length2-해설.md) 의 조각 뜻 (`0x4d0` = CONTEXT 크기 등) 은 여전히 `[추측]` 이다.
- `0x320` 은 다른 caller 넷도 쓰는 같은 기본값이다 (2.12).

### 2.10 다른 CPU 면 숫자가 달라진다

`0x57f` 는 **이 VM 게시판 값에 기대는** 숫자다. run 문서가 적은 가정 넷:

- `ControlFlags` bit 1 켜짐
- `AlignedFeatures = 0`
- `AllFeatures[11] = 0x10`
- `EnabledUserVisibleSupervisorFeatures` 에 bit 11

가정이 깨지면 (run 문서 · 결과서 그대로) `[정적 계산]`:

| 다른 경우 | `F` | 길이 |
|---|---|---|
| 마스크에 bit 11 이 안 남음 | `0x240` | **`0x56f`** |
| `ControlFlags` bit 1 꺼짐 (이 VM `Size` 로 치면) | `Size` = `0x980` | **`0xcaf`** |

- 비유: 별지 양식은 **동네 (CPU) 마다 다르게 인쇄**된다. 같은 직원이라도 다른 동네 게시판을 보면 크기가 달라진다.
- 반대로 비-CET `0x4ff` 는 게시판을 안 보니까 어느 CPU 에서도 같다 (run55 2.8).

### 2.11 같은 함수를 부르는 시동 길이 하나 더 — 메모 틀 채우기

`PspInitializeThunkContext` 는 크기를 잰 뒤 `+0xf0` (ret `+0xf5`) 에서 `RtlInitializeExtendedContext2` 를 부른다 (사물함 메모 **틀**을 채우는 함수). 그 안에서도 이번 함수가 불린다 `[정적]`:

- **문**: `+0xa3 TEST R15B,2` / `+0xa7 JNZ → +0x104`. 검사원 도장 bit 1 (XState 요청) 이 켜질 때만 이 길로 간다.
- **게시판 주소**: `+0x11f` `R8 = 0xfffff780000003d8`.
- **마스크**: 호출 인자 `r9`. 시동 길에선 `PspInitializeThunkContext +0xe3 MOV R9,R14` → CET 면 `0x800`.
  - `+0x129 ~ +0x14f` 압축 켜짐이면 `Length3` 와 같은 꼴로 거른다 (`+0x14f` 에서 `RtlpRemoveArchDisallowedXStateFeatures`).
- **부름**: `+0x15f` (ret `+0x164`) → `0x250`.
- **돌아와서**: `+0x169 LEA R14D,[RAX−0x200]` → `+0x173` (ret `+0x178`) `memset(RSI, 0, R14D)`.
  - CET 면 `0x250 − 0x200` = **`0x50`** 바이트를 0 으로 채운다.
  - `RSI` 는 `+0x10d ~ +0x111` `(RBX + 0x5f) & ~0x3f` — 64 단위로 맞춘 자리다 (무엇이 시작되는 자리인지는 run 문서가 이름을 대지 않는다).
  - 이 `0x50` 이 "XSAVE 머리 `0x40` + CET_U `0x10`" 이라는 뜻은 `[추측]` (run 문서).

비유로: 크기를 잰 사람 (`Length3`) 과 틀을 채우는 사람 (`RtlInitializeExtendedContext2`) 이 **같은 표를 따로 한 번씩** 본다. 틀 채우는 사람은 표에서 얻은 크기에서 `0x200` 을 뺀 만큼 (CET 면 `0x50`) 을 0 으로 깨끗이 지운다.

### 2.12 caller 8곳 — 시동 길 둘, 나머지 여섯은 다른 길

`run_ghidra.sh refs` 로 이 함수를 가리키는 참조를 모두 뽑았다 (9줄) `[정적]`.

- **8줄** = 이 함수를 부르는 call (caller 8곳). 지도 `_nodes.tsv` 의 `callers_total = 8` 과 같다.
- **1줄** = `from_rva 0x101264` (함수 밖) DATA. `.pdata` 풀기 표 항목으로 보인다 `[추측: 결과서]`.

8곳을 둘로 나누면:

| 무리 | caller (call 자리) | 무엇을 하는 길 |
|---|---|---|
| **시동 길** | `RtlGetExtendedContextLength3 +0xd1` · `RtlInitializeExtendedContext2 +0x15f` | 2.6 · 2.11. 둘 다 검사원 도장 bit 1 이 켜질 때만 |
| **다른 길** | `RtlpGetEntireXStateAreaLength +0xe` (24바이트 감싸개, `rdx` 고정 `0xfffff780000003d8`) · `RtlpGetNonLegacyXStateAreaLength +0x3b` (돌아와서 `− 0x200`) · `RtlpWalkFrameChain +0xef6` · `RtlDispatchException +0x3e7` · `RtlUnwind +0x127` · `RtlUnwindEx +0x630` (넷은 돌아와서 `+ 0x320`) | 문맥 크기 묻기 · 예외 풀기 · 스택 걷기. 시동 문맥과 따로 |

- 아래 여섯 중 다섯은 `.asm` 이 없어서, 결과서가 `objdump` 로 call 앞 0x40 바이트만 읽었다 (파일로 안 남김). 누가 이 여섯을 부르는지는 이번에 안 따라갔다.
- 넷의 `+ 0x320` 은 `Length3` 의 `0x4f0 − 0x10 − 0x1c0 = 0x320` 과 **같은 기본값**이다 `[정적]`. "문맥 + 확장 문맥 머리" 라는 이름은 `[추측]`.
- `RtlDispatchException` · `RtlUnwind` · `RtlUnwindEx` 는 게시판 `ControlFlags` (`[KUSD+0x3ec]`) bit 1 이 켜지면 먼저 거르고 같은 call 자리로 온다 → 압축 켜짐 · 꺼짐 **모두** 이 함수를 거친다 `[정적]`.
- 비유: 같은 "칸별 크기 표 읽는 법" 을 출근 담당만 쓰는 게 아니다. **사고 처리 담당** (예외 풀기) 도 같은 법으로 별지 크기를 잰다.

### 2.13 왜 "test.exe 첫 스레드는 이 함수를 안 부른다" 인가 — 단계별로

이번 바퀴에서 그림이 안 바뀐 이유다. 천천히 간다.

1. **근거** `[정적]`: 시동 길에서 이 함수에 닿는 자리는 **둘뿐**이다 (2.12). 나머지 여섯은 예외 · 스택 걷기 길이다.
2. **근거** `[정적]`: 그 둘은 모두 검사원 도장 (`RtlpValidateContextFlags2` 가 낸 칸) 의 **bit 1** 이 켜져야 들어간다 (`Length3 +0x81 / +0x86`, `RtlInitializeExtendedContext2 +0xa3 / +0xa7`).
3. **근거** `[정적]` (run55): 체크박스 `0x10001b` 면 도장 = **1** (bit 1 꺼짐).
4. **근거** `[관찰]` (run44): test.exe 첫 스레드의 맡긴 문맥 `ContextFlags` = `0x10001b`.
5. **그래서**: test.exe 첫 스레드는 두 자리 모두 건너뛴다 → 이 함수를 **안 부른다** `[정적]`.
6. **아직 확인 아님**: 4 는 "맡긴 메모의 체크박스" 를 본 것이다. 그 체크박스를 정한 **스레드 비트 (`_KTHREAD +0x74` bit 20) 자체** 는 아직 읽은 적이 없다 → **Q57** (동적, run55 부터 그대로).

### 2.14 run111 의 CET_U 칸과 크기가 맞는다 — 하지만 거기까지

- `[관찰]` 사진: `AllFeatures[11] = 0x10` (16바이트).
- `[정적]` run111: 맡긴 문맥의 CET_U 칸은 두 qword (`[+0]` · `[+8]`) = 16바이트.
- 크기가 **같다** → 어긋나지 않는다 (run 문서 "확인된 것", `[관찰]` + `[정적]`).
- 다만 칸 하나하나를 짝 맞춘 건 아니다. 그건 남은 정적 **Q214 ②** (4절).

### 2.15 정정과 PE_emulator 쪽

- **우리 문서엔 틀린 곳이 없다.** run55 의 "CET 면 `0x32f + RtlpGetEntireXStateAreaLength2(…)`" 은 그대로 맞고, 이번에 그 안이 풀렸다.
  - run55 문서 맨 위에 "**닫힘 (run122)**" 줄이 붙었다. [run55 해설](run55-context-length2-해설.md) 맨 위에도 "고쳐 읽기 (run122)" 한 줄을 붙였다 (본문은 그대로).
- **PE_emulator 쪽** (결과서 "지금 우리 맞나". `../PE_emulator` 코드는 flow-writer 가 안 읽었다):
  - 비-CET (`0x10001b`, `r8 = 0`) 만 쓰면 `0x4ff` 로 **맞음 · 해당 없음**. 이 함수를 아예 안 부르니까.
  - CET 스레드까지 흉내 낸다면, 게시판 `_XSTATE_CONFIGURATION` 을 **이 VM 값으로 채워야** `0x57f` 가 나온다. 다른 CPU 값이면 2.10 처럼 달라진다.
  - 할지는 사용자 결정이다 (flow.md 4장 run122 PE_emulator 줄).

### 2.16 정리: 비유로 한 번 더

1. 관리사무소가 새 직원의 원래 메모를 사물함에 넣으려 한다. 먼저 메모 크기를 잰다 (run55).
2. 직원 카드를 본다: "경비실 장부 써요?" 안 쓰면 크기는 늘 `0x4ff`. **게시판은 안 본다.** test.exe 첫 직원이 이쪽이다.
3. 장부 쓰는 직원이면 "장부 칸 (11번) 붙여 주세요" 체크 표 (`0x800`) 가 같이 온다.
4. 관리사무소는 체크 표를 두 번 거른다 (게시판에 켜진 칸인가, 이 메모에 붙여도 되는 칸인가). 이 VM 에선 그대로 통과.
5. 이제 **"칸별 크기 표" 읽는 사람** (`RtlpGetEntireXStateAreaLength2`) 차례:
   - 게시판이 "고정 양식" 이라고 하면 → 종이 전체 크기 (`Size`) 를 그대로 댄다.
   - "이어 붙이는 양식" 이라고 하면 → 머리 몫 `0x240` 에서 시작해 체크된 칸 크기를 하나씩 더한다. 새 줄에서 시작해야 하는 칸이면 줄을 넘긴다.
6. 이 VM 은 이어 붙이는 양식이고, 11번 칸은 `0x10` 이다 → `0x250`.
7. 재는 사람은 메모지에 `0x32f + 0x250` = **`0x57f`** 를 적는다.
8. 틀 채우는 사람도 같은 표를 다시 보고, `0x250 − 0x200` = `0x50` 바이트를 0 으로 지운다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 · ntkrnlmp.pdb 목록 | 앞 바퀴들 | **run122 추가** (GUID 같음. 새로 둘 depth 0, 넷 읽기만, `refs` 1장, `objdump` 5곳) |
| 기준 · 동적 근거 메모 | — | **(run122) 줄 추가**: 입력 값은 run44 보충 로그 8장의 KUSD `XState` 줄. 새 동적 실행 없음 |
| 2.3 제목 | … run115 부팅 메모 | **run122 추가** |
| 2.3 `PspInitializeThunkContext` 절 3번 CET 줄 | "`0x32f + RtlpGetEntireXStateAreaLength2(…)` (CPU 별 값, **안 풂** — 4장)" | "안 풂" 에 취소선 → **풀림**. 아래 다섯 줄: 식 `[정적]` · 마스크 거르기 `[정적]` · 이 VM `0x57f` `[정적 계산]` (입력 `[관찰]`) · `RtlInitializeExtendedContext2 +0x15f` 도 부름 + `memset 0x50` `[정적]` (뜻은 `[추측]`) · 비-CET 는 안 부름 `[정적]` |
| 4장 run55 줄 ② | CET 스레드 길이 안 풂 | **해결됨 → run122** (취소선) |
| 4장 새 줄 셋 | — | **(run122 새로)** 동적 ① ~ ③ (선택) · 정적 ① ② · PE_emulator 쪽 |
| 5장 근거 run · 실행 표 | — | **run122 줄 둘** |
| 그림 (mermaid) | — | **안 바뀜** (`Length2` 아래 글 속 함수) |

- 태그 변화를 한 줄로: 2.3 의 CET 길이는 run55 때 **"`0x32f + 함수 값`, 안 풂"** 이었다. 이제 **함수 식이 `[정적]`, 이 VM 숫자 `0x57f` 가 `[정적 계산]`** 이다.
- `baggage.md` 는 기준 줄 한 줄만: "짐 지도 바퀴 아님, 2장 · 3장 숫자 그대로, 새 짐 칸 없음. 3.7 `+0x3d8` `XState` 줄은 읽기만".
- run55 문서 맨 위 "닫힘 (run122)" 한 줄 (본문 그대로).

### 큰 그림에서 어떻게 이어지나

- 맡긴 문맥 길이는 **사물함 자리 `R15`** 를 정한다 (run16, run55). `R15` 는 곧 `LdrInitializeThunk` 가 받는 사물함 번호이고, 로더가 끝난 뒤 `NtContinue` 에 넘기는 메모다.
- 그러니 **CET 스레드라면** 사물함이 `0x80` 바이트 더 아래에 열린다. 첫 유저 `Rcx` · `Rsp` 도 그만큼 밀린다 `[추측: 같은 셈 (run16 `R15` 식) 에 넣으면]`.
- 하지만 **test.exe 첫 스레드는 비-CET** 다. 그래서 우리 흐름도 (프로세스 생성 → `main`) 의 숫자는 **하나도 안 바뀐다.**
- 이번 바퀴의 쓸모는 "모른다" 칸 하나를 지운 것이다. 에뮬레이터가 나중에 CET 스레드를 흉내 낼지 정할 때, 식과 이 VM 숫자가 이미 손에 있다.
- 다음 문서 차례는 **Q112 + Q113 + Q115** (sast/119 — run58 · run59 · run60 이 남긴 작은 정적).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| CET 스레드에서 정말 `rcx = 0x800`, ``rdx = fffff780`000003d8`` 로 불리고 `0x250` 이 돌아오나 | 정적 바퀴다. `[관찰]` 은 입력 값 (사진) 뿐이고, **CET 스레드 길이를 디버거로 본 적은 없다** | **Q213 ①** (동적, 선택). `nt+0x4eea6c` 입구 · `nt+0x4ee95e` (Length3 `+0xd6`) `r eax`. CET 스레드가 없으면 안 걸리고, 예외 길 caller 여섯도 부르니 프로세스 · 스레드 조건 필수 |
| 게시판 `XState` 값이 지금도 같나 | run44 사진 8장은 같았다. 새로 찍은 사진은 없다 (값이 바뀌었다는 증거도 없음) | **Q213 ②** (동적, 선택). 사진 찍을 일이 있으면 ``dq fffff780`000003d8 L0x69`` 한 줄 |
| caller 여덟이 실행 때 **정말 불리나** | 정적으로는 "부르는 코드가 있다" 까지만 말할 수 있다 | **Q213 ③** (동적, 선택) |
| `RtlpWalkFrameChain` · `RtlDispatchException` 이 넘기는 `rdx` (`RtlDispatchException` 은 `r15`) 가 어디서 오나 | 요청서 범위 밖. 예외 길이라 시동 길과 따로다 | **Q214 ①** (정적, 낮음). 예외 길 (run81 · run82) 에서 필요해지면 |
| 사진 `AllFeatures[11] = 0x10` 과 맡긴 문맥 CET_U 칸 (run111 두 qword) 을 **칸마다** 짝 맞추기 | 이번엔 크기만 같다는 것까지 (2.14) | **Q214 ②** (정적, 낮음). `KeVerifyContextXStateCetU` / `KiVerifyContextXStateCetUEnabled` (이미 `.asm` 있음) |
| test.exe 첫 스레드의 `_KTHREAD +0x74` bit 20 이 **정말 꺼져 있나** | 맡긴 메모 체크박스 `0x10001b` 로 앞뒤는 맞지만, 비트 자체는 읽은 적 없다 (2.13 의 6) | **Q57** (동적, run55 부터 그대로) |
| `0x240` · `0x320` · `0x50` 의 조각 뜻 (XSAVE 고정 구역 · 머리 · CET_U 등) | 코드엔 숫자만 있고 이름이 없다 `[추측]` | 대기열 번호 없음 |

- 동적 셋은 모두 **선택**이다. CET 스레드가 실제로 생기는 환경에서만 뜻이 있다.

---

## 5. 더 읽기

- run 문서: [`run122-cet-xstate-len.md`](run122-cet-xstate-len.md) — "결과" 1 (식 표) · 2 (입력이 어디서 오나, 사진 표, 셈) · 3 (caller), "도구 메모" (로그 줄 번호)
- 요청서: [`_workspace/sast/118_cet-xstate-len.md`](../../_workspace/sast/118_cet-xstate-len.md)
- 결과서: [`_workspace/sast/118_cet-xstate-len_result.md`](../../_workspace/sast/118_cet-xstate-len_result.md) — "한 줄 요약", "질문별 답" 1 ~ 3, "지금 우리 (PE_emulator) 맞나"
- flow.md: [`flow.md`](flow.md) — 2.3 `nt!PspInitializeThunkContext` 절 3번, 4장 run55 줄 ② · run122 줄 셋, 5장 run122 줄
- 앞 해설:
  - [run55 해설](run55-context-length2-해설.md) — 2.7 비-CET `0x4ff` 순서, 2.10 CET 길 (이번에 풀린 곳)
  - [run44 해설 2.9](run44-baggage-snap-해설.md) — 사진, KUSD 보충 읽기
  - [run111 해설](run111-cet-context-small-해설.md) — XState 성분, CET_U 칸
  - [run115 해설](run115-kusd-boot-해설.md) — 게시판 `XState` 를 부팅 때 적는 `KiInitializeXSaveConfiguration`
  - [run29 해설 2.9](../02_sprint/run29-continue-start-해설.md#29-cet-그림자-스택--미리-올려-둔-한-칸과-맞춰-보기) — 경비실 장부 (CET)
  - [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md) — 체크박스 `0x10001b` / `0x10005b`, 별지 (XState)
