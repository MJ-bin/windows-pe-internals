# run17 ldr-init-entry — 해설

> 이 글은 [run17 문서](run17-ldr-init-entry.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q8** — 2.4 `ntdll!LdrInitializeThunk`, `LdrpInitializeInternal`, `LdrpInitialize` callee (`_workspace/plan.md`)
  - **단계 2(ntdll 지도)의 첫 항목**이다. 단계 1(nt 지도)은 run16 에서 끝났다.
- 근거:
  - run 문서 [`run17-ldr-init-entry.md`](run17-ldr-init-entry.md) (가장 중요)
  - 요청서 [`_workspace/sast/12_ldr-init-entry.md`](../../_workspace/sast/12_ldr-init-entry.md)
  - 결과서 [`_workspace/sast/12_ldr-init-entry_result.md`](../../_workspace/sast/12_ldr-init-entry_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q8, Q9, Q43
  - 바퀴 전 관찰: [`runs/01_sprint/run7-user.md`](../01_sprint/run7-user.md) (콜스택), [`runs/01_sprint/run6-symcheck.md`](../01_sprint/run6-symcheck.md) (`x` 검색)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.
  - 처음으로 **ntdll** 을 본격적으로 읽었다 (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`).
  - 함수 이름은 **PDB 공개 이름 표**로, 구조체 칸 이름(`_TEB`, `_PEB` …)은 **PDB 타입 레코드**로 맞췄다.
  - `.asm` 에 없는 곳은 결과서가 **원본 바이트**로 읽었다. 이 글에서 "(원본 바이트)" 라고 붙인 곳이 그것이다.

이 글은 [run16 해설](run16-thunk-context-해설.md) 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그 …)은 [run9 해설](run9-create-user-process-해설.md) 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 프로세스 = **가게**, 스레드 = **직원**, PEB = 가게 **안내판**, TEB = 직원 **개인 안내판** (run10·run11)
  - 콜스택 = **책갈피 더미** (run9)
  - 이벤트 = **신호등** (run13)
  - CONTEXT = **업무 시작 메모**, 유저 스택에 맡긴 원래 메모 = **사물함** (run11·run16)
  - `LdrInitializeThunk` = **오리엔테이션실**, `RtlUserThreadStart` = **본업 자리** (run16)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번 run 문서엔 새 비유가 하나 붙었다. 이 글도 그 말을 쓴다.
  - 같은 가게에 새 직원들이 온다. 문 앞 **칠판**(`LdrpProcessInitialized`)에 처음엔 "0" 이 적혀 있다.
  - **제일 먼저 칠판을 1 로 고친 직원**이 가게 전체 세팅을 맡는다. 다 끝나면 2 로 고치고 **종**(이벤트)을 친다.
  - 늦게 온 직원은 칠판이 1 이면 종이 울릴 때까지 기다리고, 2 면 자기 자리 정리만 한다.
  - "종" 은 run13 의 **신호등**과 같은 이벤트다. 신호등이 초록으로 바뀌는 걸 "종이 울린다" 고 부를 뿐이다.

한 줄로 먼저 말하면:

- 오리엔테이션실(`LdrInitializeThunk`) 안의 로더 입구는 함수 **넷**이다. 그중 셋의 이름이 비슷해서 헷갈렸다.
- 입구 `LdrpInitialize`(0xe1448) 는 잡일 3개만 하고 **꼬리 점프**로 비켜선다. 그래서 콜스택에 **안 보였다** `[정적]`.
- 콜스택에 `LdrpInitialize+0x16c` 로 보인 함수는 사실 **`_LdrpInitialize`(0x8bf8c, 앞에 밑줄)** 이다 `[정적]`. 디버거가 왜 밑줄을 뗐는지만 `[추측]`.
- "첫 스레드인가" 는 ntdll 전역 깃발 `LdrpProcessInitialized` 를 **`LOCK CMPXCHG` 로 0 → 1 바꾸는 데 성공했나**로 가른다 `[정적]`. flow.md 2.4 의 `[추측]` 이 코드로 맞았다.
- 커널이 넘긴 `RDX` 는 네 함수를 그대로 지나 `LdrpInitializeProcess` 의 둘째 인자가 된다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 각 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext          (run15·run16)
  ═▶ IRETQ

 test.exe 각 스레드 (유저, ntdll)
  LdrInitializeThunk          Rcx = 사물함 메모, Rdx = ntdll 칸                  (run16)
   ├─ +0xe  로더 입구 네 함수 → 첫 스레드 / 나중 스레드 갈림길                   ← 이번 (run17)
   │         ├─ LdrpInitializeProcess   (첫 스레드, 가게 전체 세팅)              ← 다음 Q9
   │         └─ LdrpInitializeThread    (나중 스레드, 자기 자리 정리)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- run16 이 오리엔테이션실 **문 앞**(받는 값 `Rcx`·`Rdx`, 끝의 `NtContinue`)까지 왔다.
- 이번엔 문을 열고 **첫 방 몇 개**를 지나, 첫 스레드와 나중 스레드가 **갈라지는 곳**까지 갔다.
- flow.md 로는 **2.4** 와 그림 2 의 ntdll 로더 시작 부분이다.

### 무엇이 궁금했나

run 문서 "목적" 그대로 두 가지, 덤으로 두 가지.

1. **이름 사슬**: 01_sprint 콜스택은 `LdrInitializeThunk+0xe` ← `LdrpInitializeInternal+0x5a` ← `LdrpInitialize+0x16c` 였다. 그런데 정적으로 보니 `LdrpInitialize`(0xe1448) 가 꼬리 점프를 하고, `_LdrpInitialize`(0x8bf8c) 라는 함수도 따로 있다. **이 이름들이 어떻게 맞나?**
2. **갈림길**: `[추측]` "첫 스레드인가를 보고 고른다" — 실제로 **어느 줄이 무엇을 보고** 가르나?
3. 덤: 커널이 넘긴 `RDX` 는 어디까지 가나 (run16 이 Q8 로 미룸).
4. 덤: 디버거가 붙었을 때만 타는 길이 있나.

### 바퀴 전에 알던 것

- `[관찰]` (run7) 메인 스레드: `LdrInitializeThunk+0xe` ← `LdrpInitializeInternal+0x5a` ← `LdrpInitialize+0x16c` → `LdrpInitializeProcess`.
- `[관찰]` (run7) Worker 스레드: `LdrpInitialize+0xa7` → `LdrpInitializeThread`.
- `[관찰]` (run6) `x` 로 찾았을 때 `ntdll!LdrpInitialize` 와 `ntdll!_LdrpInitialize` 가 **둘 다** 나왔다. 표엔 "같은 이름으로 검색됨, 별개 함수" 라고 적혀 있다.
- `[정적]` (sast/02) 실제 사슬: `LdrInitializeThunk +0x9`(ret `+0xe`) → `LdrpInitialize`(0xe1448) → `+0x3c` JMP → `LdrpInitializeInternal` → (ret `+0x5a`) → `_LdrpInitialize` → (ret `+0x16c`) → `LdrpInitializeProcess`.
- `[정적]` (run16) 커널이 넘기는 `Rcx` = 사물함에 맡긴 원래 메모 주소, `Rdx` = 커널 `PspSystemDlls[0]` 칸 `+0x20` 값. `LdrInitializeThunk` 는 `Rdx` 를 안 건드리고 로더 본체로 넘긴다.
- `[추측]` (run16 해설 2.10) 꼬리 점프는 자기 책갈피를 안 남겨서, 콜스택에서 `LdrpInitialize` 가 빠졌을 수 있다.
- `[추측]` (flow.md 2.4) `LdrpInitialize` 는 "이 프로세스의 첫 스레드인가?" 를 보고 갈림길을 고른다.

---

## 2. 찾은 것

### 2.1 한눈에 — 로더 입구 네 함수

run 문서 "호출 관계" 를 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

```
LdrInitializeThunk (0xe1420, 32바이트)             ← Rcx = 사물함 메모, Rdx = ntdll 칸 [추측: 이름]
 ├─ (1) +0xe   LdrpInitialize (0xe1448, 99바이트)          [관찰] ret +0xe (run7) + [정적] 이름
 │      ├─ 잡일 3개 (+0x46, +0x27, +0x2c)                   [정적]
 │      └─ ═▶ +0x3c JMP  LdrpInitializeInternal (0x8be88, 251바이트)   ← 꼬리 점프
 │              ├─ 핫패치 준비 (+0x93 ~ +0xdf, 처음 온 스레드만)       [정적]
 │              └─ (5) +0x5a  _LdrpInitialize (0x8bf8c, 883바이트)    [관찰] 표시 이름 LdrpInitialize + [정적]
 │                      │  +0x3a LOCK CMPXCHG 전역 LdrpProcessInitialized → 옛 값 0 / 1 / 2
 │                      ├─ 옛 값 0 → +0x16c  LdrpInitializeProcess    [관찰] run7 + [정적]
 │                      ├─ 옛 값 2 → +0xa7   LdrpInitializeThread     [관찰] Worker + [정적]
 │                      └─ 옛 값 1 → +0x278  기다렸다가 다시 검사      [정적]
 └─ (2) +0x18  NtContinue(사물함 메모, TRUE)                           [정적] run16
```

읽을 때 주의:

- `═▶` 는 **호출이 아니라 점프**다. 돌아올 자리가 없다. 그래서 `+0x3c` 옆에 ret 오프셋이 없다 (run 문서 표에도 "(꼬리 JMP)").
- 함수 넷 중 **이름이 비슷한 셋**(`LdrpInitialize`, `LdrpInitializeInternal`, `_LdrpInitialize`)이 이번 바퀴의 핵심이다. 2.3 에서 천천히 푼다.
- `_LdrpInitialize` 안의 호출은 23줄 남짓인데 대표만 남겼다. 나머지는 2.8 · 2.9 와 run 문서 표에.

### 2.2 한 줄씩 읽는 법

**줄 1: `└─ ═▶ +0x3c JMP  LdrpInitializeInternal`**

- 누가: `LdrpInitialize` (rva `0xe1448`, 99바이트) — `LdrInitializeThunk` 가 부른 첫 함수.
- 어디서: `+0x3c`. **`call` 이 아니라 `JMP`** 다. 그래서 "돌아올 자리" 가 없다.
- 누구를: `LdrpInitializeInternal` (rva `0x8be88`).
- 무엇을 위해: 잡일 3개를 끝내고, **남은 로더 일을 통째로 넘기려고**. 자기는 여기서 끝이고 다시 안 돌아온다 `[정적]`.
  - 그 결과로 콜스택에서 이 함수가 사라진다. 원리는 2.4.

**줄 2: `└─ (5) +0x5a  _LdrpInitialize (0x8bf8c)`**

- 누가: `LdrpInitializeInternal`.
- 어디서: 부르는 자리 `+0x55`, 돌아올 자리 **`+0x5a`**. run7 콜스택의 `LdrpInitializeInternal+0x5a` 가 바로 이 책갈피다 `[관찰]+[정적]`.
- 누구를: `_LdrpInitialize` (rva `0x8bf8c`, **앞에 밑줄**).
- 무엇을 위해: 받은 `RCX`·`RDX` 를 **그대로** 넘기며 진짜 갈림길로 보낸다 `[정적]`. `LdrpInitializeInternal` 은 이 함수가 돌려준 값(`EAX`)을 안 본다.

**줄 3: `├─ 옛 값 0 → +0x16c  LdrpInitializeProcess`**

- 누가: `_LdrpInitialize` — 디버거 콜스택엔 `LdrpInitialize` 로 찍혔던 그 함수.
- 어디서: 부르는 자리 `+0x167`, 돌아올 자리 **`+0x16c`**. run7 콜스택의 `LdrpInitialize+0x16c` 다.
- 누구를: `LdrpInitializeProcess` — 가게 전체 세팅 (본문은 Q9).
- 무엇을 위해: 칠판(`LdrpProcessInitialized`)을 **0 → 1 로 먼저 고친 직원**, 즉 첫 스레드만 이 줄에 온다 `[정적]`.

### 2.3 이름이 비슷한 세 함수 — 누가 누구인가

이번 바퀴에서 제일 헷갈리기 쉬운 곳이다. 천천히 가 보자.

**새 말**

- **PDB 공개 이름** = 마이크로소프트가 함께 내놓는 심볼 파일(PDB)에 적힌 **함수 이름표**. 주소 하나에 어떤 이름이 붙어 있는지 알려 준다.
  - 비유: 건물 **출석부**. "몇 호에 누가 산다" 가 적혀 있다.
  - 결과서는 `llvm-pdbutil-14 dump --publics` 로 이 출석부를 직접 읽었다.

**출석부에 적힌 그대로** `[정적]`

| rva | PDB 공개 이름 (정확히) | 크기 | 하는 일 (한 줄) | 콜스택에서는 |
|---|---|---|---|---|
| `0xe1420` | `LdrInitializeThunk` | 32 | 오리엔테이션실 입구 | `LdrInitializeThunk+0xe` |
| `0xe1448` | `LdrpInitialize` | 99 | 잡일 3개 → 꼬리 점프 | **안 보임** |
| `0x8be88` | `LdrpInitializeInternal` | 251 | 핫패치 준비 → `_LdrpInitialize` 부름 | `LdrpInitializeInternal+0x5a` |
| `0x8bf8c` | **`_LdrpInitialize`** (앞 밑줄 포함) | 883 | 첫 스레드 판정 → 두 갈래 | **`LdrpInitialize+0x16c`** / `+0xa7` (밑줄 없이) |

- 네 주소 모두 **이름이 딱 하나씩**이다. 한 주소에 이름이 여럿 붙는 [ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 이야기가 **아니다** `[정적]`.
- 그러니 함정은 딱 하나다: **콜스택에서 본 `LdrpInitialize` 와 출석부의 `LdrpInitialize` 는 다른 함수다.**

**왜 "콜스택의 `LdrpInitialize+0x16c` = `_LdrpInitialize`" 라고 확신하나** — 단계별로

1. 근거 `[정적]`: 출석부의 `LdrpInitialize`(0xe1448) 는 **99바이트**짜리다.
2. 그래서: `+0x16c` 는 16진수라 99 보다 크다. 99바이트 함수 안에 `+0x16c` 자리는 **없다**. 함수 밖이다.
3. 근거 `[정적]`: 반대로 `_LdrpInitialize`(0x8bf8c) 의 `+0x16c` 는 `LdrpInitializeProcess` 를 부른 **바로 다음 자리**다 (call `+0x167`). 콜스택이 말하는 "여기서 `LdrpInitializeProcess` 를 불렀다" 와 딱 맞는다.
4. Worker 쪽 `+0xa7` 도 같다: `_LdrpInitialize` 의 `LdrpInitializeThread` 부른 다음 자리 (call `+0xa2`) `[정적]`.
5. 그래서 **주소는 확정**이다. 같은 함수를 디버거는 `LdrpInitialize` 로, 출석부는 `_LdrpInitialize` 로 부른 것이다.

**밑줄은 진짜 이름의 일부다** `[정적]`

- 링커가 붙이는 장식이 아니라 **MS 소스의 함수 이름**이다.
- 근거: `_LdrpInitialize` 안 `+0x1ba`·`+0x2da` 에서 로그 함수 `LdrpLogInternal` 에 "내 이름은 이것" 하고 넘기는 문자열이 `"_LdrpInitialize"` 다 (원본 바이트).
  - 비유: 출석부만 그런 게 아니라, 본인이 쓰는 **명찰**에도 밑줄이 있다.

**그럼 디버거는 왜 밑줄을 뗐나** — 이건 `[추측]`

- 짐작: 디버거의 심볼 엔진이 공개 이름 맨 앞 `_` 를 떼고 보여 준다.
- 편드는 정황: run6 에서 `LdrpInitialize` 를 찾을 때 `_LdrpInitialize` 도 "같은 이름으로 검색됨".
- 반대 정황:
  - 바로 그 run6 표엔 `ntdll!_LdrpInitialize` 로 **밑줄이 붙어** 적혀 있다.
  - run8 의 `dll1!_DllMainCRTStartup` 도 밑줄이 그대로 보였다 (다만 sample 자체 PDB 라 같은 규칙인지 모름).
- 그래서 명령(`x` / `k`)이나 PDB 마다 다를 수 있다 `[추측]`. 확인은 동적 **Q43** (`ln ntdll+0x8c0f8`, `k`).
- run 문서는 "고르지 않고 **둘 다 적는다**" 로 정했다. flow.md 도 "PDB 이름을 쓰고, 표시 이름을 괄호로" 로 바꿨다 (→ 3절).

**세 함수의 관계를 한 문장씩**

- `LdrpInitialize` = **안내 데스크**. 처음 온 사람에게만 필요한 잡일 몇 개를 하고, "저 안으로 가세요" 하며 자리를 비켜 준다 (꼬리 점프).
- `LdrpInitializeInternal` = **중간 방**. 핫패치 준비를 먼저 하고, 갈림길 방(`_LdrpInitialize`)으로 보낸다. 끝나면 오리엔테이션실 입구로 바로 돌아간다.
- `_LdrpInitialize` = **갈림길 방**. 칠판을 보고 "가게 전체 세팅(`LdrpInitializeProcess`)" 이냐 "자기 자리 정리(`LdrpInitializeThread`)" 냐를 고른다.

### 2.4 꼬리 점프 — 콜스택에서 함수 하나가 지워지는 원리

꼬리 점프 뜻은 [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 에서 한 줄로 봤다 ("함수 끝에서 `call` 대신 `JMP` 로 넘어감, 콜스택에 안 쌓임"). 이번엔 **왜 안 쌓이는지**를 책갈피 한 장씩 따라가 본다.

**먼저 기억할 것 하나** ([콜스택](run9-create-user-process-해설.md#콜스택))

- `call` 은 두 가지를 한다: ① 스택 맨 위에 **책갈피(돌아올 주소)** 를 하나 꽂고 ② 대상 함수로 간다.
- `ret` 은 거꾸로: 맨 위 책갈피를 **뽑아서** 그 자리로 간다.
- `JMP` 는 ② 만 한다. **책갈피를 안 꽂는다.**
- 그리고 콜스택의 한 줄 `함수X+오프셋` 은 "**함수 X 안에 꽂힌 책갈피**" 다. 함수 X 가 누군가를 부르고 아직 안 돌아왔다는 표시다.

**한 걸음씩** (작업 칸은 빼고 책갈피만 그린다. 맨 위 = 가장 최근)

① `LdrInitializeThunk +0x9` 에서 `call LdrpInitialize` `[정적]`

```
[ LdrInitializeThunk+0xe ]   ← 방금 꽂힌 책갈피
```

② `LdrpInitialize` 가 잡일 3개를 한다 (`+0x46`, `+0x27`, `+0x2c`) `[정적]`

- 잡일마다 `call` 이니 책갈피가 잠깐 꽂혔다가, 잡일이 끝나면서 `ret` 으로 **다 뽑힌다**.
- 그러니 잡일이 끝난 뒤 맨 위 책갈피는 다시 `LdrInitializeThunk+0xe` 하나다.

③ `+0x32` ~ `+0x3b`: 들어올 때 모양으로 **스택 정리** `[정적]`

- `+0x32` 맡겨 둔 `RBX` 되찾기 → `+0x37` 빌려 쓴 작업 칸 돌려놓기 → `+0x3b` 맡겨 둔 `RDI` 꺼내기.
- 이제 스택은 `LdrpInitialize` 에 **처음 들어왔을 때와 똑같다.** 맨 위 = `LdrInitializeThunk+0xe`.

④ `+0x3c`: `JMP LdrpInitializeInternal` `[정적]`

```
[ LdrInitializeThunk+0xe ]   ← 그대로. JMP 는 아무것도 안 꽂는다
```

- `LdrpInitializeInternal` 입장에선 맨 위 책갈피가 `LdrInitializeThunk+0xe` 다.
- 그래서 마치 **`LdrInitializeThunk` 가 자기를 직접 부른 것처럼** 일한다.
- `LdrpInitialize` 의 흔적(책갈피)은 스택 어디에도 없다.

⑤ `LdrpInitializeInternal +0x55`: `call _LdrpInitialize` `[정적]`

```
[ LdrpInitializeInternal+0x5a ]
[ LdrInitializeThunk+0xe ]
```

⑥ `_LdrpInitialize +0x167`: `call LdrpInitializeProcess` (첫 스레드) `[정적]`

```
[ _LdrpInitialize+0x16c ]        ← 디버거 표시: LdrpInitialize+0x16c
[ LdrpInitializeInternal+0x5a ]
[ LdrInitializeThunk+0xe ]
```

- 이 세 장이 run7 에서 `LdrpInitializeProcess` 입구에 멈췄을 때 본 콜스택과 **같은 모양**이다 `[관찰]` (run7) `+` `[정적]`.
- `LdrpInitialize`(0xe1448) 의 줄이 없는 이유: **그 함수 안에 꽂힌 책갈피가 하나도 안 남아 있어서**다.

⑦ 돌아가는 길 `[정적]`

- `LdrpInitializeProcess` 가 끝나면 `_LdrpInitialize+0x16c` 를 뽑고, `_LdrpInitialize` 가 끝나면 `LdrpInitializeInternal+0x5a` 를 뽑는다.
- `LdrpInitializeInternal` 이 `+0x75 RET` 하면 맨 위에 남은 건 **`LdrInitializeThunk+0xe`** 다.
- 그래서 **곧장 `LdrInitializeThunk` 로** 간다. `LdrpInitialize` 로는 안 돌아간다. 돌아갈 책갈피가 애초에 없으니까.
- 거기서 `+0xe MOV DL,1` … `+0x18 NtContinue` 로 이어진다 (run16).

**비유로**

- 레시피 A(`LdrInitializeThunk`) 12째 줄에 "레시피 B 보세요" → A 의 13째 줄에 책갈피를 꽂고 B 로 간다.
- B(`LdrpInitialize`) 는 재료 손질 몇 개를 하고, 마지막 줄에 "**이제 C 를 보세요. 끝나면 B 로 오지 말고 그냥 원래 책갈피로 가세요**" 라고 적혀 있다.
- B 는 자기 페이지에 책갈피를 안 꽂고 넘긴다. C(`LdrpInitializeInternal`) 를 다 읽으면 A 의 13째 줄로 바로 간다.
- 누가 중간에 "지금 책갈피 더미 보여 줘" 하면 A 와 C 의 책갈피만 보인다. **B 를 거쳤다는 흔적은 없다.**

**덧붙여 두 가지**

- `[추측]` 그러니 `LdrpInitialize` 가 콜스택에 **절대 안 나오는** 건 아니다. 잡일(②) 도중에 멈추면 그때는 그 함수 안에 책갈피(예: `+0x46`)가 꽂혀 있으니 보일 것이다. 사라지는 건 `JMP` 뒤부터다.
- `[추측]` 왜 컴파일러가 이렇게 만드나: 함수의 **마지막 일이 "다른 함수 부르고 그 결과 그대로 돌려주기" 뿐**이면, `call` + `ret` 대신 `JMP` 하나로 줄이는 최적화가 흔하다 (일반적으로 알려진 설명). 이번 코드가 그 이유로 이렇게 됐다고 적힌 근거는 없다.

**결론** — run16 의 `[추측]` 이 `[정적]` 이 됐다

- "콜스택에 0xe1448 이 안 보인 이유 = `+0x3c` 꼬리 점프" → **예** `[정적]`.
- 관찰(콜스택에 `LdrpInitializeInternal` 이 `+0xe` 바로 위)과 정적(`+0xe` 의 대상은 `LdrpInitialize`)은 **모순이 아니었다.**

### 2.5 `LdrpInitialize`(0xe1448) 가 비켜서기 전에 하는 잡일

안내 데스크가 하는 일 세 가지다 `[정적]`.

| 순서 | 자리 (ret) | 하는 일 | 조건 |
|---|---|---|---|
| 1 | `+0x46` | `InitSecurityCookie`. 이어서 `PEB.ProcessParameters.Flags` 맨 윗 비트가 켜져 있으면 `LdrpIsSecureProcess = 1` | 전역 `SecurityCookieInitialized == 0` 일 때만 |
| 2 | `+0x27` | `InitSpecialMachineFrames` | 전역 `SpecialMachineFramesInitialized == 0` 일 때만 |
| 3 | `+0x2c` | `__cpu_features_init` | 조건 없이 **늘** |
| 4 | `+0x3c` | **`JMP LdrpInitializeInternal`** | — |

- 1번 맨 윗 비트의 이름은 PDB 에 없다 `[공백]`.
- 세 함수가 안에서 무엇을 하는지는 이번에 안 읽었다. 이름으로만 보면 보안 쿠키, 특수 프레임, CPU 기능 준비 같은데 그건 `[추측]` 이다.

**"처음 한 번만" 인 이유** `[정적]`

**새 말**

- **`.data` 0 채움 구간** = ntdll 파일 안에 값이 **아예 적혀 있지 않은** 전역 변수 칸들. 메모리에 올라올 때 0 으로 채워진다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 결과서는 두 깃발이 이 구간에 있으니 "처음 값 0" 으로 읽었다.
  - 비유: 새 칠판. 아무것도 안 적혀 있으면 "0" 이다.
  - 전역 변수 자체는 [run16 해설 2.4](run16-thunk-context-해설.md) 의 "벽 주소록" 과 같은 종류다. 이번 것은 ntdll 안의 전역이다.

한 걸음씩:

1. 두 깃발(`SecurityCookieInitialized`, `SpecialMachineFramesInitialized`) 은 **처음 0** 이다 (0 채움 구간).
2. 1 로 쓰는 곳은 **각각 하나뿐**이다: `InitSecurityCookie +0x42`, `InitSpecialMachineFrames +0x57` (`_refs/`, 이번에 새로 쓴 "누가 읽고 쓰나" 찾기 도구 결과).
3. 그래서 **새 프로세스의 첫 스레드는 1·2번을 다 타고**, 깃발이 1 이 된 뒤 오는 **나중 스레드는 건너뛴다** `[정적]`.
4. 아직 확인 아님: 커널이 이 칸에 미리 뭘 써 두는지는 안 봤다 → 동적 Q43 (`db ntdll+0x1d23ce L1`, `db ntdll+0x1d23d0 L1`).

**`RCX`·`RDX` 는 받은 그대로 넘긴다** `[정적]`

- `+0x11` 에서 `RDX` 를 `RBX` 에, `+0x14` 에서 `RCX` 를 `RDI` 에 **피신**시킨다.
  - `RBX`·`RDI` 는 [비휘발 레지스터](run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다)라, 잡일 함수들을 다녀와도 값이 남는다 (호출 규약 기준이라 이 해석은 `[추측]`).
- `+0x2c`/`+0x2f` 에서 원래 자리로 되돌린 뒤 `+0x3c` 에서 점프한다.
- 비유: 안내 데스크가 손님의 짐(사물함 번호, ntdll 칸)을 잠깐 맡아 뒀다가, 다음 방으로 보낼 때 **그대로 돌려준다.**

### 2.6 `LOCK CMPXCHG` — "첫 스레드" 를 가리는 한 줄

이번 바퀴의 두 번째 큰 이야기다. flow.md 2.4 의 `[추측]` "첫 스레드인가를 보고 고른다" 가 **어느 줄에서** 일어나는지 찾았다.

**새 말**

- **`CMPXCHG`** (compare-and-exchange, 비교 후 바꾸기) = CPU 명령 하나로 아래 일을 한 번에 한다 `[추측]` (일반적으로 알려진 설명):
  1. 메모리 칸 값과 `EAX` (= "내가 기대하는 값") 를 비교한다.
  2. **같으면**: 그 칸에 새 값을 쓴다. (성공)
  3. **다르면**: 칸은 안 건드리고, 칸에 있던 값을 `EAX` 로 가져온다. (실패)
  4. 어느 쪽이든 끝나고 나면 **`EAX` = 그 칸의 옛 값**이다. (성공이면 기대값 = 옛 값이었으니까)
- **`LOCK`** = 그 앞에 붙는 꼬리표. "이 명령을 하는 동안 **다른 CPU 가 이 칸을 못 건드리게**" 한다 `[추측]` (일반적으로 알려진 설명).
  - 그래서 "읽고 → 비교하고 → 쓰기" 가 **쪼개지지 않는 한 동작**이 된다.
- 비유: 칠판 앞에 **한 사람만 설 수 있는 발판**이 있다. 발판에 올라서서 칠판을 보고, "0 이면 1 로 고친다" 를 한 번에 하고 내려온다. 그동안 다른 사람은 발판에 못 올라온다.

**왜 그냥 "읽고, 0 이면 1 로 쓰기" 로는 안 되나** `[추측]` (일반적인 설명)

- 두 직원이 **거의 동시에** 칠판 앞에 왔다고 해 보자.
- 둘 다 "0 이네" 하고 읽는다 → 둘 다 "내가 첫 번째구나" 하고 1 로 쓴다 → **세팅을 두 명이 한다.** 사고다.
- `LOCK CMPXCHG` 로 하면 발판에 먼저 오른 한 명만 0 을 보고 1 로 고친다. 다음 사람은 이미 1 인 걸 본다.
- test.exe 엔 실제로 스레드가 4개 있다 `[관찰]` (run4·run5, flow.md 3장). 그중 첫 스레드만 가게 전체 세팅을 해야 하니 이런 장치가 필요한 것으로 보인다 `[추측]`.

**코드에서 한 줄씩** (`_LdrpInitialize`) `[정적]`

| 자리 | 하는 일 | 칠판 비유 |
|---|---|---|
| `+0x38` | `XOR EAX,EAX` → `EAX = 0` | "나는 칠판에 0 이 적혀 있길 기대한다" |
| — | `R15D` 에는 1 이 들어 있다 | "고쳐 쓸 값은 1" |
| **`+0x3a`** | **`LOCK CMPXCHG [LdrpProcessInitialized], R15D`** | 발판에 올라 "0 이면 1 로" 를 한 번에. 끝나면 `EAX` = 원래 적혀 있던 값 |
| `+0x43` | `EAX` 를 1 과 비교 | "원래 1 이었나? (누가 세팅 중인가?)" → 그렇다면 `+0x257` 로 |
| `+0x58` | `TEST EAX,EAX` | "원래 0 이었나?" |
| **`+0x5a`** | **`JZ +0xcf`** | 0 이었으면 → **첫 스레드 길**(`+0xcf`)로 |

- 여기서 갈리는 기준은 "**`EAX` 에 남은 옛 값**" 하나다. 그 값으로 세 갈래가 나온다.

**옛 값 0 / 1 / 2** `[정적]`

| 옛 값 | 뜻 | 이 스레드가 가는 길 | 칠판 비유 |
|---|---|---|---|
| **0** | 아무도 안 했다 → **이 스레드가 첫 스레드**. 칸은 1 로 바뀜 | `+0xcf` → … → `+0x167` (ret **`+0x16c`**) **`LdrpInitializeProcess`** | 내가 처음으로 1 을 썼다 → 내가 가게 전체 세팅 |
| 1 | 다른 스레드가 **하는 중** | `+0x273` (ret `+0x278`) `LdrpWaitForInitializationComplete` 로 기다린 뒤 `+0x38` 로 가서 **다시 검사**. 단 `TEB.SameTebFlags` 의 `LoaderWorker`(`0x2000`) 비트가 켜진 스레드는 안 기다리고 나중 스레드 길로 | 누가 세팅 중 → 종 울릴 때까지 기다렸다 다시 칠판 보기 |
| 2 | **끝났다** | 나중 스레드 길 → `+0xa2` (ret **`+0xa7`**) **`LdrpInitializeThread`** | 세팅 끝 → 내 자리 정리만 |

- 칠판의 원래 이름: **ntdll 전역 변수 `LdrpProcessInitialized`** (rva `0x1d2988`). `.data` 0 채움 구간이라 **처음 0** 이다.
  - **PEB·TEB 칸이 아니다.** 가게 안내판이 아니라 ntdll 안에 있는 칠판이다.
- "2 = 끝남" 의 근거 (결과서) `[정적]`:
  - `LdrpInitializationComplete +0x44` 가 그 칸을 `LOCK INC` (1 → 2) 한다.
  - `LdrFastFailInLoaderCallout +0x9` 가 그 칸을 2 와 비교한다.
  - 복제 길 `+0x28b` 가 `MOV EAX,2` 를 쓴다.
- `[정적]` `LdrpWaitForInitializationComplete` 안은: 신호(이벤트)가 있으면 그걸 기다리고(`NtWaitForSingleObject`), 없으면 30ms 씩 쉬면서(`NtDelayExecution`) **칸이 1 인 동안** 되풀이한다.

**test.exe 에서 그려 보면** — 태그를 나눠서

| 장면 | 무엇 | 태그 |
|---|---|---|
| 메인 스레드가 먼저 온다 → 칠판 0 → 1 로 고침 → `LdrpInitializeProcess` | 메인 스레드 콜스택에 `+0x16c` 가 보였다 | `[관찰]` run7 (`+0x16c`) + `[정적]` (갈림 줄) |
| 메인 스레드가 세팅을 끝냄 → `LdrpInitializationComplete` (ret `+0x252`) 로 1 → 2, 종 치기 | | `[정적]` |
| Worker 스레드가 나중에 온다 → 칠판 2 → `LdrpInitializeThread` | Worker 콜스택에 `+0xa7` 이 보였다 | `[관찰]` run7 (`+0xa7`) + `[정적]` |
| 실제 옛 값이 메인 0, Worker 2 였나 | `_LdrpInitialize +0x43` 에서 `r eax` | **아직 안 봄** → Q43 (`ntdll+0x8bfcf`) |

- 관찰된 두 자리(`+0x16c`, `+0xa7`)가 **이 갈림의 두 갈래와 정확히 맞는다.** 그래서 flow.md 2.4 의 `[추측]` 이 `[정적]` 으로 올라갔다.
- 다만 "그때 칠판에 정말 0 / 2 가 적혀 있었나" 는 실행 값이라 아직 버스를 안 탔다.

**덤 — 칠판 다음에 한 번 더 보는 것** `[정적]`

- 옛 값이 0 이 아니어서 `+0x5a` 에서 점프하지 않으면, `+0x62` 에서 `PEB.InheritedAddressSpace` 를 본다.
- 0 이 아니면 **복제(fork) 프로세스의 첫 스레드** 길(`+0x284`)이다. [복제 길](run10-allocate-process-해설.md#보통-길과-복제-길) 은 run10 에서 본 "옆 가게를 통째로 베껴 짓기" 다.
- 보통 새 프로세스(test.exe)는 0 이라 이 길을 안 탄다.

### 2.7 같은 모양이 한 번 더 — `LdrpInitializeInternal` 의 핫패치 깃발

`_LdrpInitialize` 로 가기 **전에**, `LdrpInitializeInternal` 이 똑같은 0/1/2 칠판을 **하나 더** 쓴다. 이번엔 핫패치 준비용이다.

**새 말**

- **핫패치** = 프로그램을 **끄지 않고** 고친 코드를 끼워 넣는 기능이라고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 가게 문을 안 닫고 **선반 하나만 새것으로 갈아 끼우기**.
  - 이번 코드에서 확인된 건 "핫패치용 깃발·함수가 있고, 어떤 조건에서 `LdrpLoadPatchedNtdll` 을 부른다" 까지다. 그 함수가 실제로 무엇을 하는지는 이름으로만 짐작한다 (`[추측]`: 고친 ntdll 을 불러오기).

**들어가자마자 — 건너뛰기 비트** `[정적]`

- `+0x1a` ~ `+0x32`: [TEB](run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)(직원 개인 안내판) `+0x17ee` 칸 `SameTebFlags` 의 **`SkipLoaderInit`(`0x4000`)** 비트를 본다 (비트 이름은 PDB 타입).
- 켜져 있으면 **아무것도 안 하고 돌아간다.** `_LdrpInitialize` 도 안 부른다.
- 비유: 개인 안내판에 "**오리엔테이션 면제**" 도장이 찍힌 직원은 그냥 통과.
- 어떤 스레드에 켜지는지는 안 봤다 `[추측]`. flow.md 3장은 ntdll 일꾼 스레드 2개가 이 비트일 수 있다고 `[추측]` 을 달았다 (→ 3절).

**처음 온 스레드의 핫패치 준비** `[정적]`

| 순서 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0x3b` | **`LOCK CMPXCHG [LdrpHotPatchInitialized], 1`** (기대 0). 옛 값 0 이면 "처음 온 스레드" 길 (`+0x45`) |
| 2 | ret `+0x93` | `NtCreateEvent` — 핫패치 끝을 알릴 종 만들기 |
| 3 | ret `+0x98` | `LdrpInitializeHotPatching` — 여기서 `LdrpIsHotPatchingEnabled` 가 정해진다 |
| 4 | ret `+0xc2` | `LdrpIsHotPatchingEnabled != 0` 일 때만 `LdrpLoadPatchedNtdll(RDX 원래 값, &지역 칸)` |
| 5 | ret `+0xdf` | `LdrpInitializationComplete` — 핫패치 상태 1 → 2, 종 치기 |
| 6 | ret **`+0x5a`** | **`_LdrpInitialize(RCX 그대로, RDX 그대로)`** |
| 7 | `+0x75` | 1번 옛 값이 0 이었으면 `LdrpNtdllHotPatchContext` 를 비우고 **`RET`** → `LdrInitializeThunk +0xe` (2.4 의 ⑦) |

- 옛 값 1 (다른 스레드가 하는 중) 이면 ret `+0xf7` `LdrpWaitForInitializationComplete` 로 기다렸다가 6번으로. 옛 값 2 (끝남) 면 곧장 6번으로.
- 즉 `_LdrpInitialize` 와 **같은 0/1/2 모양**이다. 칠판이 두 개 있는 셈이다: 핫패치 칠판(`LdrpHotPatchInitialized`)과 가게 세팅 칠판(`LdrpProcessInitialized`).
  - 그래서 `LdrpInitializationComplete` 가 장부에 두 번 나온다 (ret `+0xdf`, ret `+0x252`). **같은 도우미를 칠판만 바꿔서** 부른다.

**`LdrpIsHotPatchingEnabled` 는 어떻게 정해지나** `[정적]` (`LdrpInitializeHotPatching`)

1. `PEB.ProcessParameters.Flags` 의 `0x2000000` 비트가 켜져 있으면 → **꺼짐**.
2. 아니면 `NtManageHotPatch(9, …)` (ret `+0x45`) 를 부른다.
   - 결과가 `0xc00000bb` 또는 `0xc0000002` 면 → **꺼짐**.
   - 그 밖이면 → **켜짐**.
- 두 상태 값의 이름은 확인 안 했다 `[추측: NOT_SUPPORTED / NOT_IMPLEMENTED]`. `0x2000000` 비트 이름도 `[공백]`.
- **정적으로는 test.exe 때 켜졌는지 못 정한다.** 실행 값이라서다 → 동적 Q43.

### 2.8 첫 스레드 길 — `LdrpInitializeProcess` 앞뒤로 하는 일

칠판 옛 값이 0 이었던 직원(첫 스레드)이 `+0xcf` 부터 하는 일이다. 하는 일 단위로 줄였다 `[정적]`.

| 순서 | 자리 (ret) | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0xea` | `NtCreateEvent(&LdrpInitCompleteEvent, …)` | 늦게 온 직원들이 기다릴 **종** 달기 |
| 2 | `+0xea` ~ `+0x13e` | 잡일 7줄 남짓: 내 TEB 에 `RanProcessInit`(`0x20`) 표시, `PEB.LoaderLock` 연결, `LdrInitState = 0`, `PEB.CrossProcessFlags` 에 `ProcessInitializing`(`2`) 켜기, 동적 함수 표 머리 초기화 | 안내판에 "**세팅 중**" 팻말 |
| 3 | `+0x14a` | `LdrInitializeMrdata` (음수면 실패 길) | 이름으로만 `[추측: .mrdata 구역 준비]` |
| 4 | `+0x154` | `LdrpIsSecureProcess != 0` 이면 `LdrpDetourExist = 1` | (2.5 의 1번에서 정해진 값) |
| 5 | **`+0x16c`** | **`LdrpInitializeProcess(RCX 그대로, RDX 그대로)`** — 예외 보호 구역 안 | **가게 전체 세팅** (Q9) |
| 6 | `+0x190` | 성공이면 `LdrpLogDllState`. 음수면 로그 후 실패 길 | |
| 7 | `+0x1a1` | `PEB.MinimumStackCommit != 0` 이면 `LdrpTouchThreadStack` | |
| 8 | `+0x205`, `+0x20f` | `LdrInitState = 3`, `ProcessInitializing` 끄기 | "세팅 중" 팻말 떼기 |
| 9 | `+0x282` | `LdrpThreadPool != 0` 이면 `RtlpHpGCTimerEnable` | |
| 10 | **`+0x252`** | **`LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, …)`** — 칠판 1 → 2, 종 치기(`NtSetEvent`) | **칠판에 2 쓰고 종 울리기** |
| 11 | `+0xbe` → `+0xcd` | `LoaderWorker` 아니면 `NtTestAlert`, 그리고 `RET` | |

- 실패 길: `LdrpInitializationFailure` (ret `+0x2fe`) → `NtTerminateProcess(-1, 상태)` (ret `+0x309`) → `RtlRaiseStatus`. 즉 세팅이 실패하면 **가게 문을 닫는다** `[정적]`.
- 표의 자리 숫자가 순서대로 커지지 않는다 (9번 `+0x282`, 10번 `+0x252`, 11번 `+0xbe`). 코드 안에서 점프로 앞뒤를 오가기 때문으로 보인다 `[추측]`. 표는 run 문서의 **하는 일 순서**를 따랐다.
- `LdrInitializePerThreadRng` 는 이 길엔 없다. 첫 스레드는 `LdrpInitializeProcess +0x153` 안에서 부른다 (`_calls.tsv`) `[정적]`.

**새 말: 예외 보호 구역**

- = 안에서 사고(예외)가 나도 그냥 죽지 않고, **정해진 착지점으로 오게** 감싼 코드 구간 `[추측]` (일반적으로 알려진 설명, try/except).
  - 비유: [run14 해설](run14-create-process-internal-해설.md#23-kernelbase-를-처음-열었다--어떤-함수들인가) 의 **비상구**. 평소엔 안 쓰다가 사고 날 때만 그쪽으로 나간다.
- 이번에 본 것 `[정적]` (원본 바이트, `UNWIND_INFO`):
  - 보호 구역 `+0x154` ~ `+0x1e5` (5번 `LdrpInitializeProcess` 호출이 이 안에 있다)
  - 필터 `_LdrpInitialize$filt$0` (안에서 `LdrpInitializeProcessWrapperFilter` 를 부름)
  - 착지점 `+0x1e5` → `+0x205` 로 흘러가 상태가 음수면 프로세스 종료.
- 이 부분은 Ghidra `.c` 에는 **안 나온다.** `.asm` 끝 "읽지 않은 틈" 목록(`+0x1e5` ~ `+0x204`)에 있던 곳을 결과서가 바이트로 풀었다. 지난 하네스 묶음(O1b)에서 새로 생긴 "틈 목록" 이 바로 쓰인 경우다.

### 2.9 나중 스레드 길 — 짧게

칠판 옛 값이 2 였던 직원(예: Worker)의 길이다 `[정적]`.

```
+0x62 (InheritedAddressSpace 0) → +0x6a (ClonedThread 0) → +0x74 (LdrpForkInProgress 0)
 → ret +0x88 LdrInitializePerThreadRng(TEB) → [UseWOW64 != 0 이면 +0x9a CALL RAX]
 → ret +0xa7 LdrpInitializeThread(RCX 그대로) → NtTestAlert → RET
```

- `RDX`(ntdll 칸)는 이 길에서 **안 쓴다** (2.10).
- **`+0x9a CALL RAX`** — 대상을 레지스터로 받는 간접 호출이다.
  - 대상 = 전역 `g_LdrpWow64LdrpInitialize` 칸의 값 (`+0x90` 에서 꺼냄, `.mrdata`, 파일 처음 값 0) `[정적]`.
  - 그 칸 주소를 넘기는 곳은 `LdrpLoadWow64 +0xe8` (`_refs/`) `[정적]`. 가리키는 함수는 wow64 쪽일 것 `[추측]`.
  - **WoW64** = 64비트 Windows 에서 32비트 프로그램을 돌리는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - test.exe 는 x64 빌드(`sample/build.bat` `[소스]`) 라 `UseWOW64` = 0 일 것 → 이 줄은 안 탈 것 `[추측]`. 확인은 Q43.
- **SRW 잠금·조건 변수 기다림** (`+0x363` ~ `+0x38a`) 도 이 길에 있다. 하지만 로더 일을 기다리는 게 아니라 **`LdrpForkInProgress != 0` (프로세스 복제 중)** 인 동안 기다리는 곳이다 `[정적]`.
  - 첫 스레드의 세팅을 기다리는 곳은 2.6 의 `LdrpWaitForInitializationComplete` 하나다. 헷갈리지 말자.

### 2.10 커널이 넘긴 `RDX` 는 어디까지 가나

run16 에서 커널이 오리엔테이션실에 들어가는 직원에게 쥐여 준 두 번째 짐이 `Rdx` (= 커널 `PspSystemDlls[0]` 칸 `+0x20` 값) 였다. 그 짐이 어디서 풀리는지 따라갔다 `[정적]`.

| 함수 | 짐(`RDX`)을 어떻게 다루나 |
|---|---|
| `LdrInitializeThunk` | 안 건드리고 `+0x9` 에서 그대로 부름. `+0xe MOV DL,1` 은 `NtContinue` 인자를 **새로** 쓰는 것이라 커널 값과 무관 |
| `LdrpInitialize` | `RBX` 에 맡겼다가 (`+0x11`) `RDX` 로 되돌려 점프 (`+0x2c`) |
| `LdrpInitializeInternal` | `RDI` 에 쥠 (`+0x28`). ① 핫패치가 켜졌으면 `LdrpLoadPatchedNtdll` **첫 인자** (`+0xba`), ② `_LdrpInitialize` **둘째 인자** (`+0x4f`) |
| `_LdrpInitialize` | 집 칸에 저장 (`+0x0`). **첫 스레드 길에서만** `+0x15c` 로 꺼내 `LdrpInitializeProcess` **둘째 인자**로. 나중 스레드 길은 안 씀 |

- 한 줄로: **네 함수를 그대로 지나 `LdrpInitializeProcess` 안으로 들어간다.** 그 안에서 무엇에 쓰나는 Q9.
- 이름은 여전히 `[추측]` 이다. 공개 PDB 라 인자 이름이 없다. Ghidra 는 `param_2` 로만 적는다. "ntdll 이 올라간 주소" 라는 뜻 이름은 이 네 함수 안에선 안 나왔다.
- 첫 번째 짐 `RCX`(사물함 메모 주소)는 두 갈래로 간다 `[정적]`:
  - `LdrInitializeThunk` 의 `RBX` 에 남아 끝에 `NtContinue` 인자가 된다 (run16).
  - 로더 쪽으로는 `LdrpInitializeInternal` `RSI` → `_LdrpInitialize` `R12` 를 거쳐 `LdrpInitializeProcess`·`LdrpInitializeThread`·`CALL RAX` 의 첫 인자가 된다.

### 2.11 디버거가 붙어서 달라지는 길 — `LdrpDoDebuggerBreak`

CLAUDE.md 가 "디버거가 붙어서 달라지는 점은 따로 적는다" 고 했다. 이번에 하나 찾았다 `[정적]`.

- `_LdrpInitialize` 안 `LdrpDoDebuggerBreak` (ret `+0x354`) 는 **복제 프로세스 첫 스레드 길** + **`PEB.BeingDebugged`(PEB `+0x2`) 가 켜졌을 때만** 탄다.
  - 길: `+0x62` (InheritedAddressSpace ≠ 0) → `+0x284` → … → `+0x346` 에서 `BeingDebugged` 검사 → `+0x34f` 호출.
- 그래서 test.exe 같은 **보통 새 프로세스 길에선 닿지 않는다** `[정적]`.
- 커널 디버거만 붙었을 때 `BeingDebugged` 가 켜지는지는 안 봤다 `[추측: 안 켜짐]`.
- `LdrpInitializeProcess +0x17d8`·`+0x1b3b` 에도 같은 호출이 있다. 그쪽은 Q9 몫이다.

### 2.12 정정 두 개

**1. flow.md 2.4·그림 2 의 이름** (run 문서 "정정")

| | 전 | 후 |
|---|---|---|
| `LdrpInitializeInternal +0x5a` 가 부르는 상자 | `ntdll!LdrpInitialize` | **`ntdll!_LdrpInitialize` (0x8bf8c)**, 콜스택 표시 이름 `LdrpInitialize` |
| `LdrInitializeThunk +0xe` 바로 다음 | (없음, 바로 `LdrpInitializeInternal`) | **`ntdll!LdrpInitialize` (0xe1448)** 상자 + 꼬리 점프 굵은 화살표 |

- 01_sprint 기록은 WinDbg 가 보여 준 이름 그대로라 **틀린 게 아니다.** 그래서 01_sprint 문서는 안 고쳤다.

**2. run16 참조 훑기 오프셋** (O2 refs 시험 때 `objdump` 로 확인)

- `NtCreateThreadEx +0x1c1` → **`+0x1bf`**, `PspWow64InitThread +0x73` → **`+0x71`** (명령 시작 위치).
- 고친 곳: flow.md 2.1 `PspCreateUserContext` 절·3장 머리, [run16 해설 2.11](run16-thunk-context-해설.md#211-그-밖에-확인된-것) 세 줄. run16 본문엔 이 오프셋이 없다.

### 2.13 그 밖에 확인된 것

- **버전 일치** `[정적]` — `_calls.tsv` 의 `LdrInitializeThunk +0xe`, `LdrpInitialize +0x3c tail`, `LdrpInitializeInternal +0x5a`, `_LdrpInitialize +0x16c` 가 01_sprint 관찰과 같다. 손계산 두 개도 `CALL` 줄 주소와 같았다 (run 문서 "환경").
- **멈춤점 주의** (run 문서 "디버거 메모") — WinDbg 에서 `LdrpInitialize` 라는 이름이 두 함수에 걸쳐 보이니, `bp ntdll!LdrpInitialize` 처럼 **이름으로 걸면 어느 쪽에 걸릴지 모른다** `[추측]`. 그래서 Q43 은 `ntdll+rva` **주소로** 건다. windbg-kd-runbook pitfalls 에 넣자는 제안이 `plan.md` 하네스 후보 (17) 로 들어갔다.
- **도구 제안** (결과서) — `+0x9a CALL RAX` 처럼 평범한 `CALL reg` 도 바로 앞 `MOV RAX,[전역]` 을 거슬러 찾아 `indirect:g_LdrpWow64LdrpInitialize` 로 찍자. 급하지 않다. `plan.md` 하네스 후보 (16).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run16 뒤) | 후 (run17) |
|---|---|---|
| 기준 | ntoskrnl·kernelbase pdb, 정적 근거 run9 ~ run16 | **ntdll.pdb GUID 줄 추가**, 정적 근거 **run9 ~ run17** (run17 ntdll). 새 규칙 한 줄: "디버거 표시 이름과 PDB 이름이 다를 수 있다 → 글에선 PDB 이름, 표시 이름은 괄호" |
| 두 그림 사이 설명 | run16 문단까지 | 문단 추가: 로더 입구도 코드로 이름을 정리했다 (`LdrpInitialize` 꼬리 점프, 콜스택의 `LdrpInitialize+0x16c` = `_LdrpInitialize`) |
| 그림 2 제목 | run4 ~ run8, run13, run14 메모, run15, run16 | **+ run17** |
| 그림 2 ntdll | `LdrInitializeThunk` → `+0xe` `LdrpInitializeInternal` → `+0x5a` `LdrpInitialize` → `+0x16c` `LdrpInitializeProcess` | `LdrInitializeThunk` → `+0xe` **`LdrpInitialize (0xe1448)`** (정적 상자) **==> `+0x3c 꼬리 JMP`** → `LdrpInitializeInternal` → (1) `+0x98` `LdrpInitializeHotPatching` / (2) `+0xdf` `LdrpInitializationComplete` / (3) `+0x5a` **`_LdrpInitialize (0x8bf8c)`** → (1) `+0x14a` `LdrInitializeMrdata` / (2) `+0x16c` `LdrpInitializeProcess` / (3) `+0x252` `LdrpInitializationComplete`. 새 정적 상자 5개 |
| 2.1 · 3장 | `NtCreateThreadEx +0x1c1`, `PspWow64InitThread +0x73` | **`+0x1bf`**, **`+0x71`** (정정) |
| 2.4 트리 | `+0xe LdrpInitializeInternal` → `+0x5a LdrpInitialize` → `+0x16c`/`+0xa7` | 2.1 의 트리 모양 (꼬리 점프, 핫패치 두 줄, `_LdrpInitialize`, `+0x3a CMPXCHG` 옛 값 0/1/2) |
| 2.4 `LdrInitializeThunk` | `RDX` 를 본체에서 어떻게 쓰나는 Q8. `+0xe` 대상 이름 다름 `[추측]` 꼬리 점프 | 둘 다 **취소선** → **`[정적]`**: `RDX` 는 `LdrpInitializeProcess` 둘째 인자로. 꼬리 점프 맞음, 관찰과 정적은 모순 아님 |
| 2.4 `LdrpInitialize` 절 | `[관찰]` `+0x16c`/`+0xa7`, `[추측]` 첫 스레드 갈림 | **0xe1448 의 절로 새로 씀** (`[정적]` 잡일 3개, 꼬리 점프, 처음 한 번 깃발) |
| 2.4 새 절 | — | **`LdrpInitializeInternal`** (`SkipLoaderInit`, 핫패치 깃발 0/1/2) · **`_LdrpInitialize`** (콜스택 표시 이름, 갈림 줄, 첫/나중 스레드 길, 예외 보호 구역, 디버거 길) |
| 2.4 첫 스레드 갈림 | `[추측]` "첫 스레드인가?" 를 보고 고른다 | **취소선 → `[정적]`**: `+0x3a LOCK CMPXCHG` 전역 `LdrpProcessInitialized`, 옛 값 0/1/2 |
| 3장 ntdll 일꾼 2개 | `[추측]` 스레드 시작 알림을 건너뛰도록 | 아래에 `[추측]` 한 줄: 그 "건너뛰기" 가 `SameTebFlags` 의 `SkipLoaderInit`/`LoaderWorker` 비트일 수 있다 (검사 줄만 `[정적]`) |
| 3장 `test!Worker` | `[관찰]` `LdrpInitialize+0xa7` | `[정적]` 한 줄: = `_LdrpInitialize +0xa7`, 칠판 옛 값 2 일 때 |
| 4장 공백 표 | 2.4 `+0xe` 대상 이름 | **해결됨 3줄** (이름 사슬, 첫 스레드 판정, `RDX`). 새 줄 5개: WinDbg 밑줄 (Q43), 로더 입구 실행 값 (Q43), 이름 못 정한 비트·상태 값, `BeingDebugged`, 일꾼 스레드 `SameTebFlags` |
| 4.1 | — | **"그림 2 ntdll 로더 입구"** 줄: 이름이 겹치니 **주소로** 멈추기 (`ntdll+0xe1448`, `+0xe1484`, `+0x8bfcf`, `+0x8c0f8` …) |
| 5장 | run16 까지 | run17 줄 ("단계 2 첫 항목"), 요청서 번호 표 run17 줄 |

태그로 보면 핵심 변화는 셋이다:

- **첫 스레드 판정: `[추측]` → `[정적]`** (`_LdrpInitialize +0x3a`, 전역 `LdrpProcessInitialized`, 옛 값 0/1/2).
- **`+0xe` 대상 이름과 꼬리 점프: `[추측]` → `[정적]`**. 콜스택의 `LdrpInitialize+0x16c`/`+0xa7` 주인 = `_LdrpInitialize` `[정적]`. 디버거가 밑줄을 뗀 이유만 `[추측]`.
- **커널 `RDX` 의 행방: 처음 다룸 → `[정적]`** (`LdrpInitializeProcess` 둘째 인자). 뜻 이름은 `[추측]` 그대로.

### 큰 그림에서 어떻게 이어지나

- **단계 2(ntdll)의 첫 방을 지났다.** run16 이 커널 쪽 "바통" (`Rip`, `Rcx`, `Rdx`) 을 정리했고, 이번엔 그걸 받은 ntdll 이 **어떤 문을 지나 어디서 갈라지는지**를 봤다.
  - 메인 스레드: 칠판 0 → **`LdrpInitializeProcess`** (가게 전체 세팅).
  - 나머지 스레드: 칠판 2 → `LdrpInitializeThread` (자기 자리 정리).
- 그리고 이제 flow.md 그림·글에서 **같은 이름 두 함수**를 헷갈리지 않게 됐다. 앞으로 run 문서는 PDB 이름을 쓰고 표시 이름을 괄호로 붙인다.
- **다음 구간** (`plan.md` "다음 항목"): **Q9 — `LdrpInitializeProcess` callee**. 가게 전체 세팅의 **안쪽**이다.
  - 이번에 Q9 로 넘긴 연결점: `LdrInitializePerThreadRng` `+0x153`, `UseWOW64` 쓰기 `+0x5f2`, `LdrpIsHotPatchingEnabled` 쓰기 `+0x587`, `LdrpNtdllHotPatchContext` 읽기 `+0x1233`·`+0x18ef`, `LdrpDoDebuggerBreak` `+0x17d8`·`+0x1b3b`, `LdrInitState` 쓰기 `+0x17dd`·`+0x1a4c`·`+0x1b1e`, `g_LdrpWow64LdrpInitialize` 읽기 `+0x17ed`. 그리고 `RDX`(ntdll 칸)를 그 안에서 어떻게 쓰나.
  - run7 에서 본 `LdrpInitializeProcess+0x19a2` (kernel32 준비), `+0x1bbb` (import DLL 초기화) 가 모두 이 함수 안이다 `[관찰]`. main 까지 가는 길의 큰 덩어리가 여기 있다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| WinDbg 가 0x8bf8c 를 `_LdrpInitialize` 가 아니라 `LdrpInitialize` 로 보인 이유 | 이름 표(PDB)로는 설명이 안 된다. 디버거 동작이라 `[추측]` (밑줄을 뗌). run6 `x` 표엔 밑줄이 붙어 있었다 | **Q43** (동적): `ln ntdll+0x8c0f8`, `x ntdll!*LdrpInitialize` |
| 칠판의 실제 옛 값 (메인 0 / Worker 2 인가), `LdrpInitializeProcess` 반환값 | 실행 때 값이다 | **Q43**: `ntdll+0x8bfcf` (`_LdrpInitialize +0x43`) 에서 `r eax`, `ntdll+0x8c0f8` 도착 때 `eax` |
| 처음 한 번 깃발(`SecurityCookieInitialized`·`SpecialMachineFramesInitialized`)이 첫 스레드 때 정말 0 인가, 커널이 미리 써 두나 | 파일 처음 값만 봤다. 실행 때 값은 정적으로 못 본다 | **Q43**: `db ntdll+0x1d23ce L1`, `db ntdll+0x1d23d0 L1` |
| `LdrpInitialize` 입구와 `JMP` 직전에 `rcx`·`rdx` 가 같은가, `rdx` 가 정말 ntdll 시작 주소인가 | 코드상 "그대로" 까지만. 값과 이름은 실행 때 | **Q43**: `ntdll+0xe1448`, `ntdll+0xe1484` 에서 `r rcx rdx`, `lm m ntdll` |
| test.exe 첫 스레드의 `SameTebFlags` (`SkipLoaderInit` `0x4000` / `LoaderWorker` `0x2000`), 핫패치 칠판 처음 값 | 실행 때 값이다 | **Q43**: `LdrpInitializeInternal` 입구 `dw @$teb+0x17ee L1`, `dd ntdll+0x1d2928 L1` |
| 핫패치가 켜지나 → `LdrpLoadPatchedNtdll` 이 불리나 | `NtManageHotPatch` 결과와 `PEB.ProcessParameters.Flags` 비트가 실행 값이다 | **Q43**: `LdrpInitializeHotPatching +0x45` 에서 `r eax`, `+0x98` 에서 `db ntdll+0x1d26f0 L1` |
| `+0x9a CALL RAX`·`+0x354` 가 test.exe 에서 정말 안 타나 | `UseWOW64` 값은 실행 때. x64 빌드라 0 일 것은 `[추측]` | **Q43**: `dd ntdll+0x1d2868 L1` |
| `LdrInitializeThunk +0xe` 로 돌아오는 때가 `LdrpInitializeProcess` 가 다 끝난 뒤인가 | 꼬리 점프 덕에 그렇게 돼야 하지만 (2.4 ⑦), 실제로 본 적은 없다 | **Q43**: `ntdll+0xe142e` 에서 `k` |
| ntdll 일꾼 스레드 2개·Worker 의 `SameTebFlags` 값 — 일꾼이 로더 일을 건너뛰는 게 이 비트 때문인가 | 검사 줄만 `[정적]`, 그 스레드들의 비트 값은 안 봤다 `[추측]` | flow.md 4장 공백 줄 (동적) |
| 이름·뜻 못 정한 것: `PEB.ProcessParameters.Flags` 맨 윗 비트·`0x2000000` 비트 이름, `0xc00000bb`·`0xc0000002` 이름, `LdrInitializeMrdata` 본문, `g_LdrpWow64LdrpInitialize` 가 가리키는 함수 | PDB 에 이름이 없거나, 본문을 이번 범위에서 안 읽었다 | flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| 커널 디버거만 붙었을 때 `PEB.BeingDebugged` 가 켜지나 | 실행 값 `[추측: 안 켜짐]` | flow.md 4장 공백 줄 |
| `LdrpInitializeProcess` 안: `RDX` 를 무엇에 쓰나, 위 연결점들 | 이번 범위 밖 (depth 0) | **Q9** (정적, 다음 항목) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run17-ldr-init-entry.md`](run17-ldr-init-entry.md) — 특히 "① 이름 사슬", "④ `_LdrpInitialize`", "정적과 동적이 다른 곳"
- 정적 요청서: [`_workspace/sast/12_ldr-init-entry.md`](../../_workspace/sast/12_ldr-init-entry.md)
- 정적 결과서 (PDB 이름 표, 원본 바이트로 읽은 예외 착지점, 질문별 답, 동적 확인 목록·추천 멈춤점, Q9 로 넘길 점): [`_workspace/sast/12_ldr-init-entry_result.md`](../../_workspace/sast/12_ldr-init-entry_result.md)
- 바퀴 전 관찰:
  - [`runs/01_sprint/run7-user.md`](../01_sprint/run7-user.md) — 메인·Worker 콜스택
  - [`runs/01_sprint/run6-symcheck.md`](../01_sprint/run6-symcheck.md) — `LdrpInitialize` / `_LdrpInitialize` 둘 다 검색됨
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "콜스택", "부르는 자리와 돌아올 자리", "한 주소에 이름 여럿 ICF 별칭"
  - [`run10-allocate-process-해설.md`](run10-allocate-process-해설.md) — "PEB", "보통 길과 복제 길"
  - [`run13-resume-thread-해설.md`](run13-resume-thread-해설.md) — 2.3 이벤트(신호등)
  - [`run15-user-thread-startup-해설.md`](run15-user-thread-startup-해설.md) — 2.6 꼬리 점프 첫 소개, 2.4 비휘발 레지스터
  - [`run16-thunk-context-해설.md`](run16-thunk-context-해설.md) — 2.7 `LdrInitializeThunk` 받는 쪽, 2.10 이번 질문의 출발점
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 머리말 (ntdll.pdb, 표시 이름 규칙), "두 그림 사이 설명" 의 run17 문단
  - "그림 2" ntdll 로더 입구 (`c0`, `c2a`, `c2b`, `c3`, `c3a`, `c3b`)
  - "2.4" 트리와 `LdrInitializeThunk`, `LdrpInitialize`, `LdrpInitializeInternal`, `_LdrpInitialize` 절
  - "3" (ntdll 일꾼 2개, `test!Worker`), "4. 공백 목록", "4.1" 그림 2 ntdll 로더 입구 줄
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q8, Q9, Q43, 하네스 후보 (16)·(17)
