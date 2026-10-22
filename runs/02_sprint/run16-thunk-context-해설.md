# run16 thunk-context — 해설

> 이 글은 [run16 문서](run16-thunk-context.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q7** — 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk` 로 정해지는 곳, `RtlUserThreadStart` 와의 관계 (`_workspace/plan.md`)
  - **단계 1(nt 지도)의 마지막 항목**이다. 이번 바퀴로 Q1 ~ Q7 이 다 끝났다 (→ 3절).
- 근거:
  - run 문서 [`run16-thunk-context.md`](run16-thunk-context.md) (가장 중요. 끝에 "단계 1 정리" 절)
  - 요청서 [`_workspace/sast/11_thunk-context.md`](../../_workspace/sast/11_thunk-context.md)
  - 결과서 [`_workspace/sast/11_thunk-context_result.md`](../../_workspace/sast/11_thunk-context_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q7, Q8, Q17, Q29, Q34, Q42
  - 바퀴 전 관찰: [`runs/01_sprint/run4-thread-start.md`](../01_sprint/run4-thread-start.md) (콜스택, "부른 곳 없음")
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.
  - 커널은 ntoskrnl, 받는 쪽(유저)은 이미 뽑아 둔 `ms/disassemble/ntdll/LdrInitializeThunk.asm` 을 근거로 썼다.
- 읽는 방법 세 가지가 섞여 있다. 글에서 밝혀 둔다.
  - **(원본 바이트)** = Ghidra `.asm` 에 없는 곳을 결과서가 바이너리에서 직접 읽은 것.
  - **(PDB 타입)** = 구조체 칸 이름을 PDB 타입 레코드로 맞춘 것.
  - 전역 변수 이름(`PsNtdllExports` 등)은 **PDB 공개 심볼 + PE 구역표**로 맞췄다. 처음 쓰는 방법이다.

이 글은 [run9](run9-create-user-process-해설.md) ~ [run15](run15-user-thread-startup-해설.md) 해설 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그 …)은 [run9 해설](run9-create-user-process-해설.md) 2절에, 구조체 칸 오프셋·PEB 는 [run10 해설](run10-allocate-process-해설.md) 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구** (run14)
  - 프로세스 = **가게**, 스레드 = **직원**, `_ETHREAD` = 직원 **인사 카드**, PEB = 가게 **안내판**
  - 유저 스택 = 가게 안 **작업대** (run11)
  - CONTEXT = 직원에게 주는 **업무 시작 메모** ("어디서부터(`Rip`)", "처음 손에 쥘 것(`Rcx`)") (run11)
  - 트랩 프레임 = 직원 책상에 붙은 **출발 메모**, `IRETQ` = 그 메모대로 **순간 이동**, `PspUserThreadStartup` = **출근 도장** 창구 (run15)
  - 시작 정보 묶음 = 관리사무소 **서류철** (run15)
  - 멈춤 APC = **멈춤 쪽지**, `SuspendEvent` = **신호등** (run13)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번에 flow.md 2.3 에 새 비유가 붙었다. 이 글도 그 말을 쓴다.
  - `RtlUserThreadStart` = **본업 자리**, `LdrInitializeThunk` = **오리엔테이션실**, 원래 메모를 맡겨 두는 유저 스택 자리 = **사물함**
  - [run15 해설 2.7](run15-user-thread-startup-해설.md#27-그-rip-칸에-누가-ldrinitializethunk-를-넣나--다음-q7) 의 "본 업무 창구"·"신입 교육실" 과 같은 것이다. 이름만 flow.md 쪽으로 맞춘다.
  - 사물함은 run11 의 **작업대(유저 스택) 맨 위 한 칸**이라고 생각하면 된다.

한 줄로 먼저 말하면:

- 새 직원의 업무 시작 메모엔 **처음부터 "본업 자리(`RtlUserThreadStart`)"** 가 적혀 있다 `[정적]`.
- 그런데 출근 직전에 관리사무소가 그 메모를 **사물함(유저 스택)에 맡겨 두고**, 출발 메모만 **"먼저 오리엔테이션실(`LdrInitializeThunk`)로"** 바꿔 끼운다 `[정적]`. 사물함 번호는 `Rcx` 에 적어 준다.
- 오리엔테이션(ntdll 로더)이 끝나면 `LdrInitializeThunk +0x18` 에서 사물함 메모를 들고 **`NtContinue`** 를 부른다 `[정적]`. 그 메모대로 본업 자리로 가는 **마지막 한 걸음만** 아직 `[추측]` 이다 (Q29).
- 그래서 01_sprint 의 수수께끼 두 개(콜스택의 `RtlUserThreadStart`, 부른 곳 없는 `LdrInitializeThunk`)는 **같은 칸의 바꾸기 전·후**였다.
- 커널은 이 두 주소를 **부팅 때 ntdll 이름표에서 찾아 적어 둔 것**을 쓴다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
(부팅 때, 훨씬 앞) PspInitializeSystemDlls   ntdll 주소를 적어 두기        ← 이번 ⑤

cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW                   (run14)
     ├─ 시스템 콜 nt!NtCreateUserProcess                      (run9 ~ run12)
     │    ├─ PspCreateUserContext        업무 시작 메모 쓰기   ← 이번 ①
     │    └─ PspAllocateThread … KxContextToKframes
     │                                   출발 메모에 옮기기    ← 이번 ②
     └─ 시스템 콜 nt!NtResumeThread                           (run13, run14)

 test.exe 첫 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup                    (run15)
       └─ +0xb3 PspInitializeThunkContext  바꿔 끼우기         ← 이번 ③
  ═▶ IRETQ                                                    (run15)

 test.exe 첫 스레드 (유저)
  ntdll!LdrInitializeThunk → 로더 …                            ← 이번 ④ (앞머리와 끝만)
       └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main    ← ④ 마지막 고리는 추측
```

- 이번 바퀴는 한 구간이 아니라 **한 값이 지나가는 길**을 따라갔다. 그래서 그림 1(부모)과 그림 2(test.exe) 양쪽에 걸친다.
- flow.md 로는 **2.1** (`PspCreateUserContext`, `KeInitThread`), **2.3** (바꿔 끼우기, 부팅 때 주소), **2.4·2.10** (`NtContinue` 앞부분) 이다.

### 무엇이 궁금했나

run 문서 "목적" 그대로 네 가지다.

1. 트랩 프레임 `Rip` 칸(+0x168)에 `LdrInitializeThunk` 를 **넣는 줄**은 어디고, 그 값은 **어디서** 오나.
2. 그 전엔 `Rip` 칸에 **무엇**이 있었나. 처음 채우는 길(`PspCreateUserContext` → `KxContextToKframes`)이 맞나 (run15 `[추측]`).
3. `RtlUserThreadStart` 와는 **무슨 관계**인가. 01_sprint 의 `[관찰]` 두 개가 코드로 어떻게 맞아떨어지나.
4. 커널은 `LdrInitializeThunk` 주소를 **어떻게 아나**.

### 바퀴 전에 알던 것

- `[관찰]` (run4) `nt!PspUserThreadStartup` **입구**에서 찍은 콜스택 맨 아래 유저 자리에 `ntdll!RtlUserThreadStart` 가 보였다.
- `[관찰]` (run4·run7) 그런데 유저에서 처음 멈춘 곳은 `ntdll!LdrInitializeThunk` 이고, **부른 곳이 없었다**. 스레드 4개 모두 그랬다.
- `[관찰]` (run5) 메인 스레드 Win32 Start Address = `test!mainCRTStartup`.
- `[정적]` (run11) `PspAllocateThread` 는 `CONTEXT.Rip` → `_ETHREAD.StartAddress`, `CONTEXT.Rcx` → `Win32StartAddress` 로 **옮겨 적기만** 한다 ([run11 해설 2.6](run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다)). CONTEXT 를 누가 채우는지는 몰랐다.
- `[추측]` (run11) `StartAddress` 쪽 값은 `RtlUserThreadStart` 일 수 있다.
- `[정적]` (run15) 유저 첫 `RIP` = `_KTRAP_FRAME.Rip`(+0x168) 칸. `IRETQ` 가 거기서 꺼낸다. `PspInitializeThunkContext` 는 **모든 스레드**가 부른다.
- `[추측]` (run15) 그 칸을 처음 채우는 쪽은 `KiInitializeContextThread +0x1b1` `KxContextToKframes`, 다시 정하는 쪽은 `PspInitializeThunkContext` (이름만).
- `[추측]` (flow.md 2.10, PELdr 기록) 로더가 끝나면 `NtContinue` 로 `RtlUserThreadStart` 에 간다.
- `[공백]` 이 모든 걸 잇는 "바꿔 끼우는 손" 이 실제로 무엇을 하는지.

---

## 2. 찾은 것

### 2.1 한눈에 — 한 장의 메모가 지나가는 길

run 문서 "호출 관계" 를 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

```
(부모 cmd.exe 스레드, 커널)
NtCreateUserProcess
 ├─ (5) +0xd22  PspCreateUserContext        ① 메모: Rip = RtlUserThreadStart,
 │                                             Rcx = exe 진입점, Rdx = PEB        [정적]
 └─ (7) +0xe5c  PspAllocateThread
                 └─ +0x6f9 KeInitThread
                     └─ +0x301 KiInitializeContextThread
                         └─ +0x1b1 KxContextToKframes   ② 메모 → 출발 메모(처음)  [정적]

(test.exe 스레드, 커널)
KiStartUserThread
 ├─ (1) +0x28  PspUserThreadStartup
 │       └─ (5) +0xb3  PspInitializeThunkContext  ③ 바꿔 끼우기                   [정적]
 │               ├─ (1) +0x16d  PspGetContextThreadInternal   원래 메모 읽기
 │               ├─ (2) +0x285  RtlCopyContext                원래 메모 → 사물함
 │               └─ (4) +0x2db  PspSetContextThreadInternal   새 출발 메모 쓰기
 └─ (2) ═▶ 유저  IRETQ  (run15)
          ─▶ ntdll!LdrInitializeThunk  (Rcx = 사물함 번호)       [관찰] run4 + [정적]
              ├─ (1) +0xe   로더 본체                            [관찰] run7
              └─ (2) +0x18  NtContinue(사물함 메모, TRUE)  ④     [정적]
                      ═▶ (커널 NtContinue 안 [공백]) ═▶ RtlUserThreadStart   [추측]
```

읽을 때 주의:

- 위 덩어리와 아래 덩어리는 **다른 스레드**다. 위는 부모 cmd.exe 직원이, 아래는 test.exe 새 직원 자신이 한다. 그 사이에 멈춰 두기·풀어 주기(run12 ~ run14)가 끼어 있다.
- `PspInitializeThunkContext` 아래 호출은 다섯 개인데 대표 셋만 남겼다. 나머지 둘(`+0x29a`, `+0x2fa`)은 2.6 에서.
- 맨 아래 `═▶` 두 개는 호출이 아니라 **실행이 넘어감**이다. 그중 커널 `NtContinue` 안은 아직 아무도 안 읽었다 `[공백]`.

### 2.2 한 줄씩 읽는 법

**줄 1: `├─ (5) +0xd22  PspCreateUserContext`**

- 누가: `NtCreateUserProcess` — 부모 cmd.exe 직원이 신청 창구에 와서 관리사무소가 대신 일하는 중.
- 어디서: 부르는 자리 `+0xd1d`, 돌아올 자리 `+0xd22`. run9 에서 이미 본 자리다.
- 누구를: `PspCreateUserContext` (91바이트, 안에서 부르는 함수 없음).
- 무엇을 위해: 새 직원의 **업무 시작 메모(CONTEXT)** 에 세 칸을 적으려고 — `Rip` = `RtlUserThreadStart`, `Rcx` = exe 진입점, `Rdx` = PEB (→ 2.4). 태그 `[정적]`.

**줄 2: `└─ (4) +0x2db  PspSetContextThreadInternal`**

- 누가: `PspInitializeThunkContext` — test.exe 새 직원이 출근 도장 창구 마지막 단계에 와 있다.
- 어디서: 부르는 자리 `+0x2d6`, 돌아올 자리 `+0x2db`.
- 누구를: `PspSetContextThreadInternal`. 이 함수가 몇 단계 안쪽(`PspSetContext +0x68`)에서 출발 메모 칸을 실제로 고쳐 쓴다 (→ 2.6).
- 무엇을 위해: 출발 메모를 **"`Rip` = `LdrInitializeThunk`, `Rcx` = 사물함 번호"** 로 바꾸려고. 태그 `[정적]`.

**줄 3: `└─ (2) +0x18  NtContinue(사물함 메모, TRUE)`**

- 누가: `ntdll!LdrInitializeThunk` — 이제 유저 쪽이다.
- 어디서: 부르는 자리 `+0x13`, 돌아올 자리 `+0x18`. flow.md 그림 2 에서 `(2) ?` 이던 라벨이 이 숫자로 바뀌었다.
- 누구를: `ntdll!NtContinue`.
- 무엇을 위해: 사물함에 맡겨 둔 원래 메모대로 실행을 옮겨 달라고 (→ 2.7). 호출이 코드에 있다는 것까지 `[정적]`. 실행은 아직 안 봤다.

### 2.3 비유 하나로 처음부터 끝까지

자세히 들어가기 전에, 한 이야기로 먼저 이어 보자. 오른쪽 태그는 **코드 쪽 사실**에 붙은 것이다. 비유 자체엔 태그가 없다.

| # | 이야기 | 코드 | 태그 |
|---|---|---|---|
| 1 | 부모 가게 직원이 신청 창구에 오면, 관리사무소가 새 직원의 **업무 시작 메모**를 쓴다. "본업 자리는 여기(`RtlUserThreadStart`), 처음 손에 쥘 건 가게 입구 번지(진입점), 다른 손엔 가게 안내판(PEB)." | `NtCreateUserProcess +0xd22` `PspCreateUserContext` | `[정적]` |
| 2 | 관리사무소는 그 메모를 서류철에 끼워, 새 직원 책상에 **출발 메모**로 옮겨 붙인다. 이때 출발 메모도 "본업 자리" 다. | `KiInitializeContextThread +0x1b1` `KxContextToKframes` | `[정적]` |
| 3 | (새 직원은 대기 줄에 서고, 멈춤 쪽지·신호등으로 잠깐 멈췄다가 풀린다 — run12 ~ run14) | | |
| 4 | 새 직원이 출근 도장 창구에 온다. 마지막 담당이 출발 메모를 **베껴서 사물함에 넣고**, 책상 출발 메모는 **"먼저 오리엔테이션실로"** 로 바꿔 붙인다. 메모 한구석에 **사물함 번호**도 적어 준다. | `PspUserThreadStartup +0xb3` `PspInitializeThunkContext` | `[정적]` |
| 5 | 직원은 출발 메모대로 **순간 이동**한다. 도착한 곳은 오리엔테이션실이다. 누가 데려다준 게 아니라 "그냥 거기서 시작" 한다. | `IRETQ` → `LdrInitializeThunk` | `[정적]` (run15) + `[관찰]` run4 |
| 6 | 오리엔테이션(ntdll 로더)을 받는다. | `LdrInitializeThunk +0xe` 로더 본체 | `[관찰]` run7 (안쪽은 단계 2) |
| 7 | 끝나면 사물함 번호로 원래 메모를 꺼내, 신청 창구에 **"이 메모대로 보내 주세요"** 라고 낸다. | `LdrInitializeThunk +0x18` `NtContinue(사물함 메모, TRUE)` | `[정적]` (실행 안 봄) |
| 8 | 관리사무소가 메모대로 **본업 자리**로 보내 준다. | 커널 `NtContinue` 안 → `RtlUserThreadStart` | 안쪽 `[공백]`, 도착은 `[추측]` (Q17·Q29) |

핵심은 4번이다.

- 원래 메모를 **버리지 않고 맡겨 둔다.**
- 그래서 오리엔테이션을 먼저 받고도, 나중에 **원래 가려던 곳**으로 정확히 갈 수 있다.

왜 굳이 이렇게 하나? 코드가 "이유" 를 적어 두진 않는다. 근거에서 짐작할 수 있는 만큼만:

1. `[정적]` 바꿔 끼우기엔 "첫 스레드만" 조건이 없다 → **모든 유저 스레드**가 오리엔테이션실부터 간다 (→ 2.6).
2. `[관찰]` flow.md 2.4 트리에서 로더 안쪽은 첫 스레드면 `LdrpInitializeProcess`, 나중 스레드면 `LdrpInitializeThread` 로 갈린다 (run7).
3. 그러니 짐작 `[추측]`: 어떤 스레드든 **유저 코드보다 먼저 ntdll 로더를 한 번 거치게** 하려는 장치로 보인다. 첫 스레드는 가게 전체 준비, 나중 스레드는 자기 몫 준비.
4. 아직 확인 아님: 로더 안에서 무엇을 하는지는 단계 2(Q8 ~)에서 본다.

### 2.4 장면 ① 처음 메모 — `PspCreateUserContext` 가 채우는 값

**새 말**

- **전역 변수** = 함수 하나에 묶이지 않고, 커널 어디서든 같은 이름으로 읽는 칸 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소 벽에 붙은 **주소록**. 누구든 와서 본다.
- **`PsNtdllExports`** = 커널 전역 변수 하나 (rva `0xfc74b0`). 형은 `_PS_NTDLL_EXPORTS` (168바이트, PDB 타입) — **ntdll 안 함수 주소를 칸마다 하나씩** 적어 둔 묶음이다.
  - `+0x8` 칸 = `LdrInitializeThunk`, `+0x10` 칸 = `RtlUserThreadStart` (PDB 타입).
  - 공개 PDB 엔 전역 변수의 형이 없다. 그래서 크기(168바이트)와, 부팅 때 쓰는 표의 19개 {이름 ↔ 칸 오프셋} 이 멤버와 **모두 맞는 것**으로 이었다 `[정적]` (→ 2.9).
- **exe 진입점** = exe 가 "여기서부터 실행하세요" 라고 정해 둔 첫 자리 `[추측]` (일반적으로 알려진 설명). 코드에선 `_SECTION_IMAGE_INFORMATION.TransferAddress` (+0x0) 칸 값이다 `[정적]`.
  - `_SECTION_IMAGE_INFORMATION` = [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션)의 이미지 정보 묶음 (PDB 타입).

**무엇을 적나** `[정적]`

`NtCreateUserProcess` 가 `+0xd1d` 앞에서 인자를 채우고, `PspCreateUserContext` 가 CONTEXT 칸에 그대로 적는다.

| CONTEXT 칸 | 값 | 값을 꺼내는 줄 (`NtCreateUserProcess`) | 적는 줄 (`PspCreateUserContext`) |
|---|---|---|---|
| **`Rip` +0xf8** | **`RtlUserThreadStart`** | `+0xd10` — `PsNtdllExports+0x10` 칸 | `+0x28` |
| **`Rcx` +0x80** | **exe 진입점** (`TransferAddress`) | `+0xd09` | `+0x2f` |
| **`Rdx` +0x88** | **PEB** (`EPROCESS.Peb` +0x2e0, WoW64 면 32비트 값) | `+0xce9 ~ +0xd04` | `+0x36` |

- 첫 줄의 실제 명령은 이렇다 (`NtCreateUserProcess.asm`, run 문서 인용):
  ```
  +0xd10  MOV R8,[0x140fc74c0]     ; = PsNtdllExports + 0x10
  ```
  `0x140fc74c0` 는 `PsNtdllExports`(rva `0xfc74b0`) 에서 `0x10` 뒤다. 이름은 `.asm` 엔 없고, 결과서가 PDB 공개 심볼로 붙였다.
- `SegCs` `0x33`, `SegSs` `0x2b`, `MxCsr` `0x1f80` 같은 칸도 이 함수가 적는다 (`+0x11 ~ +0x51`).
- **`Rsp` 는 이 함수가 안 쓴다.** `PspAllocateThread +0x5ef` 에서 부르는 `PspSetupUserStack` 이 `+0xdc` 에서 적는다. 값 = 새 유저 스택 위쪽 − 임의 오프셋 − 0x28 (칸 이름은 PDB 에 없어 `[추측: StackBase]`).
  - 비유: 메모의 "작업대 위치" 칸은 작업대를 마련하는 담당이 따로 적는다.

**진입점 값이 오는 길** `[정적]` (결과서 2-1)

1. `PspAllocateProcess +0x991` (call `+0x98c`) `MmGetSectionInformation` — 섹션의 이미지 정보를 복사한다. 첫 8바이트가 `TransferAddress`.
2. 이미지가 다른 주소에 올라가면 `PspAllocateProcess +0x20b3 ~ +0x20ce` 에서 차이만큼 더한다.
3. 그 값이 `NtCreateUserProcess +0xd09` 에서 `PspCreateUserContext` 로 간다.

**그래서 무엇이 맞아떨어지나**

- `[정적]` run11 이 본 "옮겨 적기" 와 합치면: `_ETHREAD.StartAddress` = **`RtlUserThreadStart`**, `Win32StartAddress` = **exe 진입점**.
- run11 `[추측]` "`StartAddress` 는 `RtlUserThreadStart` 쪽" 이 **코드로 맞았다** `[정적]`.
- run5 `[관찰]` "Win32 Start Address = `test!mainCRTStartup`" 과도 맞는다 (진입점 = `mainCRTStartup`).
- flow.md 2.1 의 옛 짐작 "`PspCreateUserContext` 가 `LdrInitializeThunk` 를 정하는 곳일 수도" 는 **지워졌다** (취소선). 여기서 정하는 건 `RtlUserThreadStart` 이고, `LdrInitializeThunk` 는 나중에 바꿔 끼운다.

**갈림길 하나** `[정적]`, 실행은 동적

- `NtCreateUserProcess +0xce0` 에서 섹션 참조가 0 이면, `PspCreateUserContext` 대신 `PspGetContextThreadInternal` (ret `+0xd3e`) 로 **부모 스레드 문맥을 복사**한다.
- test.exe 실행 때 어느 쪽을 타는지는 아직 안 봤다 → **Q34**.

### 2.5 장면 ② 출발 메모에 처음 옮겨 적기 — `KxContextToKframes` (run15 `[추측]` → `[정적]`)

업무 시작 메모(CONTEXT)는 **종이 한 장**이다. 직원이 실제로 순간 이동할 때 읽는 건 **책상 출발 메모(트랩 프레임)** 다 ([run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)). 그래서 한 번 옮겨 적어야 한다.

**길** `[정적]`

1. `PspAllocateThread +0x6af` — 서류철(시작 정보 묶음) `+0x20` 칸에 **CONTEXT 주소**를 끼운다.
2. 서류철이 `KeInitThread +0x2fc` → `KiInitializeContextThread` 로 넘어간다 (run15 에서 본 그 길).
3. `KiInitializeContextThread +0xb5` 가 CONTEXT 를 꺼내 자기 쪽에 복사하고, `ContextFlags`·`SegCs`·`SegSs` 를 손본다.
4. **`+0x1ac` (ret `+0x1b1`) `KxContextToKframes`** — 트랩 프레임(`InitialStack − 0x190`)으로 옮긴다.

**옮기는 칸** `[정적]` (칸 이름은 PDB 타입)

| CONTEXT 칸 | → 트랩 프레임 칸 | `KxContextToKframes` 줄 |
|---|---|---|
| `Rip` +0xf8 | `Rip` +0x168 | `+0x66` / `+0x6d` |
| `Rsp` +0x98 | `Rsp` +0x180 | `+0x74` / `+0x7b` |
| `Rcx` +0x80 | `Rcx` +0x38 | `+0xe1` / `+0xe8` |
| `Rdx` +0x88 | `Rdx` +0x40 | `+0xec` / `+0xf3` |

- 그러니 **스레드가 만들어진 직후 출발 메모는 `Rip = RtlUserThreadStart`, `Rcx = exe 진입점`, `Rdx = PEB`** 다 `[정적]`.
- run15 에서 "이름·인자 모양만 봤다" 던 `KxContextToKframes` 가 이제 줄 단위로 확인됐다.

### 2.6 장면 ③ 바꿔 끼우기 — `PspInitializeThunkContext`

이번 바퀴의 한가운데다. `PspUserThreadStartup +0xb3` (call `+0xae`) 에서 불린다. 이 순간 출발 메모는 아직 2.5 그대로다.

**코드 순서** `[정적]` (자리는 `PspInitializeThunkContext` 기준)

| # | 자리 | 하는 일 | 비유 (태그 없음) |
|---|---|---|---|
| 1 | `+0x113` ~ `+0x2de` | 특수 APC 를 잠깐 막는 구간 (`_KTHREAD.SpecialApcDisable` −1 … +1) | "작업 중, 쪽지는 잠깐 뒤에" |
| 2 | `+0x168` (ret `+0x16d`) | `PspGetContextThreadInternal` — **지금 출발 메모의 원래 내용**을 커널 버퍼로 읽어 옴 (`Rip` = `RtlUserThreadStart`, `Rcx` = 진입점 …) | 책상 메모를 베낀다 |
| 3 | `+0x17a ~ +0x187` | 맡길 자리 `R15` = 원래 `Rsp` − 문맥 길이, 16바이트로 맞춤 → **원래 유저 스택 바로 아래** | 작업대 맨 위 한 칸을 사물함으로 비운다 |
| 4 | `+0x18b ~ +0x208` | 새 메모(`ctx2`) 채우기 (아래 표) | 새 메모를 쓴다 |
| 5 | `+0x20e ~ +0x228` | 계측 콜백이 있으면 그쪽으로 (아래 "곁가지") | — |
| 6 | `+0x280` (ret `+0x285`) | **`RtlCopyContext` — 원래 메모를 유저 스택 `R15` 에 맡김**. 앞 `+0x23e` 부터 유저 메모리를 검사하고, 쓰기에 실패하면 오류 길 (착지점 `+0x2ab`, 원본 바이트) | 베낀 메모를 사물함에 넣는다 |
| 7 | `+0x295` (ret `+0x29a`) | `KePopulateContinuationContext(원래 Rip)` — CET 일 때만 일함 (아래 "곁가지") | — |
| 8 | `+0x2d6` (ret `+0x2db`) | **`PspSetContextThreadInternal(현재 스레드, ctx2, …)`** — 새 메모를 출발 메모 칸에 씀 | 책상 메모를 바꿔 붙인다 |
| 9 | `+0x2f5` (ret `+0x2fa`) | 막기를 푼 뒤 커널 APC 가 걸려 있으면 `KiCheckForKernelApcDelivery` (조건부) | 미뤄 둔 쪽지 확인 |

- 순서가 중요하다: **베끼기(2) → 맡기기(6) → 바꿔 붙이기(8)**. 원래 내용을 먼저 안전한 곳에 두고 나서야 덮어쓴다.

**새 메모 `ctx2` 에 적는 것** `[정적]` (`ctx2` 는 이 함수 안 로컬, 0 으로 채운 뒤 적음)

| 칸 | 넣는 값 | 줄 | → 트랩 프레임 칸 (`PspSetContext` 줄) |
|---|---|---|---|
| **`Rip`** | **`LdrInitializeThunk`** = `PsNtdllExports+0x8` | `+0x1a1`, `+0x1b2` | `+0x168` (`+0x61` / **`+0x68`**) |
| **`Rcx`** | **`R15`** = 유저 스택에 맡긴 원래 메모의 **주소** | `+0x1b9` | `+0x38` (`+0xdc` / `+0xe3`) |
| `Rsp` | `R15 − 0x28` (맡긴 메모 바로 아래) | `+0x18b`, `+0x18f` | `+0x180` (`+0x6f` / `+0x76`) |
| `Rdx` | `PspSystemDlls[0]` 칸 `+0x20` (ntdll 헤더 `ImageBase` `[추측: 이름]`) | `+0x196`, `+0x19d`, `+0x1c0` | `+0x40` (`+0xe7` / `+0xee`) |
| `ContextFlags` | `0x10000b` (어느 칸을 옮길지) | `+0x1a8` | — |
| `EFlags` | 안 적음 → 사용자 모드라 `0x200` (IF 만) | — | `+0x178` |

- 한 줄씩 비유로:
  - `Rip` = "**먼저 오리엔테이션실로**"
  - `Rcx` = "**사물함 번호**". 64비트 Windows 에서 `Rcx` 는 첫 번째 인자 칸이라 ([run11 해설 2.6](run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다)), 오리엔테이션실이 첫 인자로 사물함 번호를 받는 모양이 된다.
  - `Rsp` = 작업대 위치를 사물함 **바로 아래**로. 결과서는 `0x28` 을 "그림자 공간 0x20 + 돌아올 주소 칸 8" 로 읽었다.
  - `Rdx` = "ntdll 건물 번지" 쯤으로 보인다. 이름은 `[추측]`, 쓰임은 Q8.

**실제로 칸에 쓰는 손은 `PspSetContext +0x68`** `[정적]`

`PspInitializeThunkContext` 는 출발 메모(트랩 프레임)를 **직접 건드리지 않는다.** 새 메모를 만들어 건네기만 한다. 실제 쓰기까지 사슬은 이렇다 (현재 스레드라 APC 없이 바로).

1. `PspSetContextThreadInternal +0xf7 CMP R13,R14` — 대상이 **지금 이 스레드**면 `+0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` 를 직접 부른다. (다른 스레드였다면 APC 를 걸고 기다린다.)
2. → `+0x64` (ret `+0x69`) `PspGetSetContextInternal` — 트랩 프레임 = `[스레드 +0x28] − 0x190` (`+0xa4`, `+0xb2`). run15 의 "`InitialStack − 0x190`" 과 같다. 쓰기면 `+0x3ea` (ret `+0x3ef`) `PspSetContext`.
3. → **`PspSetContext +0x61 ~ +0x68`** 이 `ctx2.Rip` → `_KTRAP_FRAME.Rip`(+0x168).

- 비유: 출근 도장 마지막 담당은 새 메모를 써서 **게시 담당**에게 넘기고, 책상에 실제로 붙이는 건 게시 담당이다.
- 덤: 2.5 의 `KxContextToKframes` 와 이 `PspSetContext` 는 칸 대응이 **똑같다** (결과서: "같은 일을 하는 쌍둥이 코드").

**이 함수가 하지 않는 것 세 가지** `[정적]`

이번 결과를 읽을 때 "있는 것" 만큼 "없는 것" 이 중요하다.

- **"첫 스레드만" 을 가르는 줄이 없다.** 갈림 조건은 계측 콜백·CET·알림 마스크뿐이다.
  - run15 "`PspInitializeThunkContext` 는 모든 스레드가 부른다" 와 합치면 → **모든 유저 스레드가 같은 바꿔 끼우기를 거친다** `[정적]`.
  - run4 `[관찰]` "스레드 4개 모두 `LdrInitializeThunk` 에서 출발" 과 모양이 같다.
- **돌아올 주소 칸을 안 쓴다.** 유저 메모리에 쓰는 건 맡긴 메모 `[R15, R15+길이)` 뿐이다. 새 `Rsp`(`R15 − 0x28`)가 가리키는 칸엔 쓰는 줄이 없다.
  - 그 칸의 값은 `[추측: 새 스택이라 0]`, 동적 (Q42). 이게 2.8 수수께끼 2 의 열쇠다.
- **인사 카드는 안 고친다.** `_ETHREAD.StartAddress`(`RtlUserThreadStart`)·`Win32StartAddress`(진입점)는 그대로다. 바뀌는 건 트랩 프레임 `Rip` 하나다.
  - 비유: 인사 카드엔 여전히 "본업 자리" 가 적혀 있고, 책상 메모만 "먼저 오리엔테이션실" 이다.

**곁가지 세 개** (이야기 줄기 밖. 조건만 `[정적]`, 실제로 타는지는 동적 Q42)

- **계측 콜백** (`_KPROCESS.InstrumentationCallback` +0x168, PDB 타입) — 프로세스에 걸어 두는 "유저로 넘어갈 때 먼저 들를 곳" 주소로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - `[정적]` 0 이 아니면 `ctx2.Rip` 를 그 값으로 바꾸고, `R10` 에 `LdrInitializeThunk` 를 넣는다 (`+0x20e ~ +0x228`).
  - 보통 0 `[추측]`. 그러면 위 표대로 간다.
- **CET 그림자 스택** — 돌아올 주소를 따로 한 벌 더 적어 두고 맞춰 보는 CPU 보안 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - `[정적]` 그게 켜진 스레드면 `+0x295` `KePopulateContinuationContext` 가 원래 `Rip` 를 그림자 스택에 한 칸 넣는다.
  - 목적은 `[추측]`: 나중에 `NtContinue` 로 돌아갈 때 검사를 맞추려는 것으로 보인다.
- **스레드 만들기 알림 콜백 루프** — `+0x11b ~ +0x153`, `+0x337 ~ +0x360` (`PspNotifyEnableMask` bit4 일 때, CFG 간접 호출).
  - 어느 알림인지, `PspNotifyThreadCreation`(`PspUserThreadStartup +0xa1`)과 어떻게 나누는지는 안 읽었다 `[추측]`.

### 2.7 장면 ④ 받아서 되돌리기 — `LdrInitializeThunk` → `NtContinue`

**새 말**

- **`NtContinue`** = CONTEXT 하나를 건네며 "**이 메모대로 실행을 이어 가 줘**" 하고 커널에 부탁하는 시스템 콜 `[추측]` (일반적으로 알려진 설명. run 문서도 "맡긴 문맥으로 실행을 옮겨 달라고 함" 이라고 읽었다).
  - 비유: 신청 창구에 사물함 메모를 내밀며 "이대로 보내 주세요".

**받는 쪽 코드** `[정적]` (`ms/disassemble/ntdll/LdrInitializeThunk.asm`, 기존 export)

| 자리 | 명령 | 뜻 |
|---|---|---|
| `+0x6` | `MOV RBX,RCX` | 커널이 `Rcx` 로 준 **사물함 번호**를 `RBX` 에 쥐고 있음 |
| `+0x9` (ret `+0xe`) | `CALL` | 로더 본체 (이름은 2.10) |
| `+0xe`, `+0x10` | `MOV DL,1` / `MOV RCX,RBX` | 인자 = (사물함 메모, TRUE) |
| `+0x13` (ret **`+0x18`**) | `CALL NtContinue` | **사물함 메모대로 실행을 옮겨 달라고 함** |
| `+0x1a` (ret `+0x1f`) | `CALL RtlRaiseStatus` | `NtContinue` 가 **돌아오면** (= 실패) 예외 |

- `RBX` 는 [비휘발 레지스터](run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다)다. 그래서 로더 본체를 다녀와도 사물함 번호가 그대로 남아 있다 — 이 해석은 일반 호출 규약에 기댄 것이라 `[추측]`.
- 마지막 줄이 재미있다. `NtContinue` 뒤에 "돌아오면 오류" 를 둔 건, **성공하면 돌아오지 않는다**는 뜻으로 읽힌다 `[추측]`.
- `RDX`(ntdll 칸)는 `+0x0 ~ +0x9` 에서 안 건드리고 로더 본체로 넘어간다. 본체가 어떻게 쓰나는 Q8.

**마지막 고리 — 왜 "`RtlUserThreadStart` 로 간다" 고 짐작하나**

1. 근거 `[정적]`: 사물함 메모는 2.6 의 2번에서 베낀 **원래 출발 메모**다. 그 `Rip` = `RtlUserThreadStart`, `Rcx` = exe 진입점 (2.4 ~ 2.5).
2. 근거 `[정적]`: `LdrInitializeThunk +0x18` 이 바로 그 메모 주소로 `NtContinue` 를 부른다.
3. 그래서 `[추측]`: **`NtContinue` 가 메모대로 돌려준다면** `RtlUserThreadStart(진입점, PEB)` 모양으로 이어진다.
4. 아직 확인 아님: 커널 `NtContinue` 가 메모의 `Rip` 로 실제로 보내는 부분은 **안 읽었다** `[공백]`.
   - 이름만 본 후보 `[추측]`: `KeContextToKframes` 를 부르는 곳 중에 `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` 이 있다 (원본 바이트 훑기, 본문 안 읽음). `KeContextToKframes` 는 `KxContextToKframes` 를 부르는 다른 한 곳이다.
   - 정적 → **Q17**, 동적 → **Q29** (`RtlUserThreadStart` 에서 `k`).

- 그 뒤 `RtlUserThreadStart` → … → `mainCRTStartup` 연결은 flow.md 2.10 에서 Q18·Q30 몫으로 남아 있다.

### 2.8 01_sprint 의 수수께끼 두 개 — 같은 칸의 "전" 과 "후"

run15 해설 3절에서 "수수께끼 두 개 중 하나가 풀렸다, 남은 건 왜 `LdrInitializeThunk` 인가" 라고 했다. 이제 둘 다 설명된다. 다만 **어디까지가 코드로 본 것이고 어디부터 짐작인지** 나눠서 보자.

**수수께끼 1 — 커널 콜스택 맨 아래 유저 자리에 `ntdll!RtlUserThreadStart`** (run4)

| 단계 | 내용 | 태그 |
|---|---|---|
| 언제 찍었나 | run4 는 `bp nt!PspUserThreadStartup` — 그 함수 **입구**에서 찍었다 | `[관찰]` |
| 그때 출발 메모 `Rip` | `KxContextToKframes` 가 적은 그대로 = `RtlUserThreadStart`. 바꿔 끼우기(`+0xae`)는 **그보다 뒤** | `[정적]` |
| 왜 콜스택에 그게 보였나 | WinDbg 가 그 칸을 유저 자리로 읽어 보여 준 것으로 보인다 ([run15 해설 2.5](run15-user-thread-startup-해설.md#25-kistartuserthreadreturn--실제로-안-도는-가짜-돌아올-주소) 의 풀어 보기 짐작) | `[추측]` |

**수수께끼 2 — 첫 유저 멈춤은 `ntdll!LdrInitializeThunk`, 부른 곳 없음** (run4·run7)

| 단계 | 내용 | 태그 |
|---|---|---|
| 왜 거기서 시작하나 | `PspInitializeThunkContext` 가 `Rip` 를 `LdrInitializeThunk` 로 바꾼 뒤 `IRETQ` 로 간다 (run15) | `[정적]` |
| 돌아올 주소 칸 | 새 `Rsp` 칸에 돌아올 주소를 **쓰지 않는다** | `[정적]` |
| 왜 "부른 곳 없음" 으로 보였나 | 그 칸이 0 이라 콜스택이 혼자로 보였다 | `[추측]` (값은 동적, Q42) |

- [콜스택](run9-create-user-process-해설.md#콜스택)은 책갈피 더미다. `call` 로 왔다면 맨 아래에 "어디서 왔음" 책갈피가 있었을 것이다. 이번엔 `call` 없이 **순간 이동**으로 왔고, 커널이 그 자리에 책갈피를 꽂아 두지도 않았다. 그래서 더미가 비어 보였다 — 여기까지의 연결이 `[추측]` 이다.

**둘을 합치면**

- 두 관찰은 모순이 아니다. **같은 칸(`_KTRAP_FRAME.Rip`)을 바꾸기 전·후에 본 것**이다.
  - 앞 시각(입구) = `RtlUserThreadStart`, 뒤 시각(유저 첫 멈춤) = `LdrInitializeThunk`.
- 태그로 나누면:
  - **시각 차이**(입구는 바꾸기 전, 유저 첫 멈춤은 바꾼 뒤)는 `[정적]`.
  - **"그래서 WinDbg 에 그렇게 보였다"** 는 연결은 `[추측]`.
- 닫는 방법: `PspUserThreadStartup+0xae` / `+0xb3` 앞뒤에서 트랩 프레임 `Rip` 값을 찍으면 된다 (Q42).
- 그리고 원래 메모는 버려진 게 아니다. 사물함에 맡겨져, 로더가 끝난 뒤 `NtContinue` 로 다시 쓰인다 (`NtContinue` 안쪽만 `[공백]`).

### 2.9 커널은 ntdll 주소를 어떻게 아나 — 부팅 때 이름으로 찾아 적어 둔다

2.4 와 2.6 에서 커널은 `PsNtdllExports` 의 칸에서 주소를 **꺼내기만** 했다. 그 칸은 누가 채웠을까?

**새 말**

- **내보내기 표(export table)** = DLL 이 "내 안의 **이 이름** 함수는 **여기** 있다" 고 바깥에 공개하는 목록 `[추측]` (일반적으로 알려진 설명).
  - 비유: ntdll 건물 입구의 **호실 이름표**. "오리엔테이션실 → 몇 호" 같은 것.

**길** `[정적]` (자리는 run 문서 그대로)

```
PspInitPhase2 +0x177                                     (원본 바이트 훑기)
 └─ PspInitializeSystemDlls      종류 0..5 반복 (+0x26)
     ├─ (1) +0x32  PsQuerySystemDllInfo   PspSystemDlls[k] 칸 (+0x20 ≠ 0 일 때만)
     └─ (2) +0x5b  PspLookupEntryPoint    표의 이름마다
          └─ +0x20  RtlFindExportedRoutineByName   ntdll 내보내기 표에서 이름 찾기
         → +0x6b  표가 가리키는 칸에 주소 기록 (실패면 KeBugCheckEx 0x6b)
```

한 걸음씩:

1. 커널 안에 **"찾을 이름 목록"** 표가 있다. `NtdllExportInformation` 0번 = {`NtdllExports`, 19개} (원본 바이트).
2. `NtdllExports` 의 항목은 {이름 문자열, 적을 칸} 쌍이다.
   - 첫 항목 = {`"LdrInitializeThunk"`, `PsNtdllExports+0x8`}
   - 둘째 = {`"RtlUserThreadStart"`, `+0x10`}
3. 부팅 때 `PspInitializeSystemDlls` 가 이 목록을 돌며, `PspLookupEntryPoint` → `RtlFindExportedRoutineByName` 로 ntdll 내보내기 표에서 **이름으로** 찾는다.
4. 찾은 주소를 **유저 주소로 환산**한다 (`PspLookupEntryPoint +0x2f ~ +0x3a`, 정보 칸 `+0x18`·`+0x20` 차이).
   - 커널이 보는 ntdll 자리와 가게(유저) 쪽에서 보이는 ntdll 자리가 다르니, 그 차이만큼 고쳐 적는 것이다.
5. `+0x6b` 에서 목록이 가리키는 칸(`PsNtdllExports+0x8` 등)에 적는다. 못 찾으면 `KeBugCheckEx(0x6b, …)` 로 간다 `[정적]`. 시스템을 멈추는(블루스크린) 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- 비유: 관리사무소가 **문 여는 날(부팅)** 에 ntdll 건물 호실 이름표를 보고, "오리엔테이션실 = 몇 호, 본업 자리 = 몇 호" 를 **벽 주소록(`PsNtdllExports`)** 에 한 번 적어 둔다. 그 뒤로 새 직원이 올 때마다 주소록만 본다.
- 2.4 에서 "`PsNtdllExports` 형이 `_PS_NTDLL_EXPORTS` 다" 라고 한 근거가 여기다. 이 19개 {이름 ↔ 칸 오프셋} 이 `_PS_NTDLL_EXPORTS` 의 멤버 이름·오프셋과 **19개 모두** 맞았다 `[정적]`.

**`Rdx` 로 넘긴 ntdll 칸도 부팅 때 정해진다** `[정적]`

- 2.6 의 `Rdx` 값(`PspSystemDlls[0]` 칸 `+0x20`)은 부팅 때 `PspMapSystemDll` 이 **인자 1** 로 불릴 때 채운다 (`PspLocateSystemDll +0x238`, `PsLocateSystemDlls +0x89` 에서).
- 프로세스마다 ntdll 을 붙일 땐(`PsMapSystemDlls`, 인자 0) `PspMapSystemDll +0x172` 가 **그 주소에 붙었는지만** 검사한다.
- 그래서 flow.md 2.1 의 옛 `[추측]` "ntdll 섹션은 부팅 때 한 번 만들고, 프로세스마다 붙이기만 한다" 에 **"모양은 맞다"** `[정적]` 근거가 붙었다.

**정리** `[정적]`

- `PspCreateUserContext` 가 쓰는 `RtlUserThreadStart`, `PspInitializeThunkContext` 가 쓰는 `LdrInitializeThunk` 와 ntdll 칸 — 모두 **부팅 때 한 번 정해진 전역**이다. 프로세스마다 다시 찾지 않는다.
- `[추측]` 이름 목록 표와 문자열은 INIT 구역(부팅 뒤 버려지는 곳)에 있어서, 나중엔 메모리에 없을 수 있다. 남는 건 `PsNtdllExports` 안 주소뿐. (비유: 개업 준비물 상자는 개업 뒤 치우고, 벽 주소록만 남는다.)
- `[공백]` 부팅 안에서 `PsLocateSystemDlls`(← `IoInitSystemPreDrivers +0x10f5`)와 `PspInitPhase2` 의 앞뒤 순서는 안 따졌다 (범위 밖).

### 2.10 정적과 동적이 다른 곳 — `LdrInitializeThunk +0xe` 의 이름 (짧게)

- `[관찰]` run7 콜스택: `LdrInitializeThunk+0xe` 바로 위가 **`LdrpInitializeInternal`**.
- `[정적]` `.asm` 에서 `+0x9` (ret `+0xe`) 가 부르는 대상: Ghidra 이름 **`LdrpInitialize`** (rva `0xe1448`). 그리고 그 함수가 `+0x3c` 에서 `LdrpInitializeInternal` 로 **[꼬리 점프](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)** 한다 (장부: `LdrpInitialize +0x3c tail LdrpInitializeInternal`).
- `[추측]` 꼬리 점프는 자기 책갈피를 안 남긴다. 그래서 콜스택엔 `LdrpInitialize` 가 빠지고 `LdrpInitializeInternal` 이 바로 위로 보였을 수 있다.
- 이번 질문(커널 쪽) 밖이라 **고치지 않고 둘 다 적어 뒀다.** 이름 정리는 **Q8** 몫이다. `plan.md` Q8 줄에도 sast/02 가 본 비슷한 이름 문제(`_LdrpInitialize`, 꼬리 호출)가 이미 적혀 있다.

### 2.11 그 밖에 확인된 것

- **다른 스레드의 처음 메모** — `NtCreateThreadEx +0x1c1`, `PspWow64InitThread +0x73` 도 같은 `PsNtdllExports+0x10` 칸을 읽는다 `[정적]` (참조 훑기만, 코드는 안 읽음).
  - `[추측]` 그래서 `CreateThread` 로 만든 스레드도 처음 메모는 `Rip = RtlUserThreadStart` 이고, 그게 똑같이 사물함에 맡겨진다 (동적 Q42).
- **멈춤 쪽지가 펼쳐질 수 있는 자리 후보가 하나 늘었다** `[정적]+[추측]`.
  - [run15 해설 2.9](run15-user-thread-startup-해설.md#29-run13-의-쪽지는-0x22-직후-펼쳐진다--고리-3개가-굳었다) 에선 첫 기회가 `PspUserThreadStartup +0x26` 이라고 짐작했다.
  - 이번에 `PspInitializeThunkContext` 가 **특수 APC 막기 구간**(`+0x113 ~ +0x2de`) 안에서 일하고, 끝에 커널 APC 가 걸려 있으면 `+0x2f5` `KiCheckForKernelApcDelivery` 를 부른다는 게 보였다 `[정적]`.
  - 멈춘 첫 스레드가 앞서 `+0x26` 에서 이미 잠드는지, 이 자리에서 잠드는지는 동적 (Q42·Q19).
- **버전 일치** `[정적]` — `NtCreateUserProcess +0xc36`, `PspInsertThread +0x807`, `NtCreateUserProcess +0xd22`, `PspUserThreadStartup +0xb3` 이 장부와 같다.
- **결과서 "스크립트 문제" 제안 4개** (`.claude/` 는 안 고침, 제안만):
  1. `.asm` 에 "읽지 않은 틈" 표시 (이번엔 SEH 착지점 `+0x2ab ~ +0x2b9`)
  2. `.asm` 의 전역 변수 주소에 PDB 이름 붙이기 (`[0x140fc74b8]` → `PsNtdllExports+0x8`)
  3. "이 전역을 누가 읽고 쓰나" 를 찾는 도구 (`PsNtdllExports` 는 표 안 포인터로 **간접** 기록돼서 코드 참조로 안 나왔다)
  4. `indirect:cfg:?` 대신 레지스터+오프셋 표기
  - `plan.md` 는 다음 할 일을 이런 하네스 후보 묶음으로 잡아 두었다 (→ 3절 끝).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run15 뒤) | 후 (run16) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run15 | **run9 ~ run16** (+ ntdll `LdrInitializeThunk.asm`). 전역 변수 이름은 PDB 공개 심볼 + PE 구역표로 맞췄다는 줄 |
| 두 그림 사이 설명 | 커널 → 유저 전환 = `IRETQ` | 한 문단 추가: 유저 첫 위치가 `LdrInitializeThunk` 가 되는 까닭도 **코드로 봤다** (처음 `RtlUserThreadStart` → 맡기고 바꿔 끼움 → 끝에 `NtContinue`. 커널 `NtContinue` 안은 `[공백]`) |
| 그림 1 | `PspCreateUserContext` (정적: 갈림길 한쪽) | 라벨에 **`Rip = RtlUserThreadStart, Rcx = exe 진입점`** |
| 그림 2 `nt` | `PspInitializeThunkContext` (모든 스레드) | 라벨에 "원래 문맥을 유저 스택에 맡기고 Rip 를 LdrInitializeThunk 로". **새 상자 둘**: `(1) +0x16d PspGetContextThreadInternal`, `(2) +0x2db PspSetContextThreadInternal` |
| 그림 2 유저 | `LdrInitializeThunk` (부른 함수 없음) → `(2) ?` → `NtContinue` (추측, PELdr 기록) ==> `RtlUserThreadStart` (추측) | `LdrInitializeThunk` (… **Rcx = 맡긴 원래 문맥**) → **`(2) +0x18`** → `NtContinue` **(정적: 맡긴 문맥, TRUE)** ==> `RtlUserThreadStart` (정적: 맡긴 문맥의 Rip 값. **실제로 가는지는 추측**). 묶음 제목도 "NtContinue 호출은 정적, 그 뒤는 추측" |
| 2.1 트리 | `+0xd22 PspCreateUserContext [정적]` | 값 표시 추가. `KeInitThread` 아래 `+0x301 KiInitializeContextThread` → `+0x1b1 KxContextToKframes [정적]` 두 줄 |
| 2.1 `PspMapSystemDll` | `[추측]` ntdll 섹션은 부팅 때 한 번 | `[정적]` "모양은 맞다" 근거 (→ 2.9) |
| 2.1 `PspCreateUserContext` | `[추측]` `LdrInitializeThunk` 를 정하는 곳일 수도 | 그 짐작 **취소선**. `[정적]` 세 칸(`Rip`·`Rcx`·`Rdx`), `Rsp` 는 `PspSetupUserStack`, `StartAddress`/`Win32StartAddress` 값, 다른 스레드 참조 `[추측]` |
| 2.1 `PspAllocateThread`·`KeInitThread` | CONTEXT 를 누가 채웠나는 함수 밖. 처음 옮기기는 `[추측]` | 채운 곳 = `PspCreateUserContext`. `+0x6af` 서류철. **`KxContextToKframes` 칸 대응 `[정적]`** |
| 2.3 앞머리 | run4 콜스택 | "그 콜스택은 **바꿔 끼우기 전**" 한 줄 |
| 2.3 트리 | `+0xb3 PspInitializeThunkContext` 한 줄, `LdrInitializeThunk` 줄에 "(누가 정하나는 Q7)" | 아래 다섯 호출 + "들어올 때 Rip = `RtlUserThreadStart` → 나갈 때 `LdrInitializeThunk`". `LdrInitializeThunk` 줄에 "Rcx = 맡긴 원래 문맥 주소 `[정적]`" |
| 2.3 비유 | 출근 도장 + 순간 이동, "쪽지에 무엇을 적는지는 Q7" | 사물함·오리엔테이션실·본업 자리 비유 추가 |
| 2.3 새 절 셋 | — | `PspInitializeThunkContext` (정적) 9걸음 · "01_sprint 관찰 두 개가 맞아떨어지는 방법" · "커널이 `LdrInitializeThunk` 주소를 아는 법 — 부팅 때" |
| 2.3 트랩 프레임 `Rip` 채우는 쪽 | 처음 = `KxContextToKframes` `[추측]`, 다시 = `PspInitializeThunkContext` (Q7) | **둘 다 `[정적]`**. run11 `[추측]` `StartAddress` = `RtlUserThreadStart` → `[정적]` |
| 2.4 트리 | `└─ ? NtContinue ← 추측 (PELdr 기록)` | **`└─ (2) +0x18 NtContinue(맡긴 문맥, TRUE) ← [정적]`**. 커널이 넘기는 `Rcx`·`Rdx`, `+0xe` 이름 차이 |
| 2.10 | 로더 끝 → 진입점 `[추측]` | 앞부분 `[정적]` (`NtContinue(맡긴 문맥)`, 그 `Rip` = `RtlUserThreadStart`, `Rcx` = 진입점). **남은 `[추측]`** = 커널 `NtContinue` 안, 그 뒤 |
| 3장 (스레드 4개) | — | 네 스레드 모두 바꿔 끼우기를 거친다 `[정적]`, 다른 스레드도 `RtlUserThreadStart` 로 시작 `[추측]` |
| 4장 공백 표 | 2.1 `PspCreateUserContext`, 2.3 `RtlUserThreadStart` 관계, 2.3 CONTEXT 누가 채우나 | 셋 다 **해결됨 (정적)**. 2.10 **일부 해결**. 새 줄 6개: 2.3 실제 값(Q42), 곁가지, 멈춤 APC 후보, 부팅 순서, 2.4 `+0xe` 이름, 2.4·2.10 커널 `NtContinue` 안 |
| 4.1 | — | `PspCreateUserContext` 줄 보강, `PspInitializeThunkContext` 새 줄, 로더 끝 줄 보강 (각각 디버거로 무엇을 보면 되는지) |
| 5장 | run15 까지 | run16 줄 ("단계 1 끝"), 요청서 번호 표에 run16 줄 |

태그로 보면 핵심 변화는 다섯 가지다:

- **2.3 "유저 첫 위치가 `LdrInitializeThunk` 로 정해지는 곳": `[공백]` → `[정적]`** (`PspInitializeThunkContext` → `PspSetContext +0x68`).
- 처음 출발 메모를 채우는 `KxContextToKframes`: `[추측]` → **`[정적]`**.
- 처음 메모의 값 (`StartAddress` = `RtlUserThreadStart`): `[추측]` → **`[정적]`**.
- `LdrInitializeThunk` → `NtContinue`: `[추측]` (PELdr 기록) → **`[정적]` `+0x18`**. 단 `NtContinue` ==> `RtlUserThreadStart` 는 **`[추측]` 그대로** (값만 `[정적]`).
- 커널이 ntdll 주소를 아는 법: 처음 다룸 → **`[정적]`** (부팅 때 이름으로).

### 단계 1(nt 지도)이 끝났다 — 한눈에

run 문서 "단계 1 정리" 절을 따랐다. "핵심 태그 변화" 칸은 각 바퀴 해설의 3절에서 옮겼다. 새 사실은 없다.

| 대기열 | run | 그림 | 채운 flow.md 절 (run 문서) | 핵심 태그 변화 (그 바퀴 해설) |
|---|---|---|---|---|
| Q1 | [run9](run9-create-user-process-해설.md) | 1 | 2.1 `NtCreateUserProcess` 아래 대표 12개, 2.2 멈춤 갈림길 | 2.1 관찰 7줄 → 대표 12개. 2.2 전체 `[공백]` → **`[정적]` 일부** |
| Q2 | [run10](run10-allocate-process-해설.md) | 1 | 2.1 `PspAllocateProcess` 아래 (주소 공간 → exe → ntdll → PEB), `MmCreatePeb` PEB 오프셋 | `PspAllocateProcess` `[추측]` → **`[정적]`**. `MmCreatePeb` `[관찰]` → **`[관찰]+[정적]`** |
| Q3 | [run11](run11-allocate-thread-해설.md) | 1 | 2.1 `PspAllocateThread` 아래 (객체 → ID → 스택 → TEB → `KeInitThread`), `PspInsertProcess` | 두 함수 역할 `[추측]` → **`[정적]`** |
| Q4 | [run12](run12-insert-thread-해설.md) | 1 | 2.1 `PspInsertThread` 아래, 알림 (첫 스레드만 / 모든 스레드) | 흐름 대표 3 → **12단계 `[정적]`**. "알림은 첫 스레드만" `[추측]` → **`[정적]`** |
| Q5a | [run13](run13-resume-thread-해설.md) | 1·2 | 2.2 멈춰 두기(멈춤 APC + `SuspendEvent`), 풀어 주는 nt 사슬 | 멈춘 스레드가 서는 곳, 풀어 주는 사슬 `[공백]` → **`[정적]`** |
| Q5b | [run14](run14-create-process-internal-해설.md) | 1 (+2 메모) | 2.2 부모 KERNELBASE `CreateProcessInternalW` | 부모가 풀어 주나 `[추측]` → **`[정적]`** |
| Q6 | [run15](run15-user-thread-startup-해설.md) | 2 | 2.3 `KiStartUserThread` → `PspUserThreadStartup` → `IRETQ` | 커널 → 유저 전환 `[공백]` → **`[정적]`** |
| Q7 | run16 (이 글) | 1·2 | 2.1 `PspCreateUserContext`, 2.3 바꿔 끼우기, 2.4·2.10 `NtContinue` 앞부분 | 유저 첫 위치 정하는 곳 `[공백]` → **`[정적]`**. `NtContinue` 호출 `[추측]` → **`[정적]`** |

- "그림" 칸은 flow.md 그림 제목에 적힌 run 번호를 따랐다. 그림 1 = 부모 cmd.exe 스레드, 그림 2 = test.exe 메인 스레드.
- 이렇게 보면 **그림 1·2 의 커널(nt) 쪽이 코드로 처음부터 끝까지 이어졌다.**
  - 부모: 신청 창구(`NtCreateUserProcess`) → 가게 차리기 → 첫 직원 뽑기 → 명부 등록 → 멈춰 두기 → (부모 KERNELBASE) 풀어 주기.
  - test.exe: 교대의 `RET` → 출근 도장 → **바꿔 끼우기** → `IRETQ` → `LdrInitializeThunk`.
- 그런데 **단계 1 은 모두 `[정적]` 이다.** 노선도는 다 읽었지만, 버스는 한 번도 안 탔다.
  - 실행으로 맞춰 볼 동적 항목 (run 문서): **Q19, Q34, Q35, Q38, Q39, Q42** (nt 쪽), **Q29** (`NtContinue` → `RtlUserThreadStart`).
- `plan.md` 기준 단계 1 쪽에 남은 정적 후보: Q32·Q33·Q36·Q40·Q41 (나중에 맨 아래 붙은 것들).

### 큰 그림에서 어떻게 이어지나

- **커널이 유저에게 넘겨주는 "바통" 이 정확히 무엇인지 알게 됐다.**
  - `Rip` = `LdrInitializeThunk` (어디서 시작할지)
  - `Rcx` = 사물함 메모 주소 (끝나고 어디로 갈지 적힌 원래 메모)
  - `Rdx` = ntdll 칸 (이름은 `[추측]`)
  - 단계 2(ntdll)는 이 세 값을 **받는 쪽**부터 시작한다.
- **다음 구간** (`plan.md` "다음 항목"):
  - 먼저 사용자 결정대로 **하네스 후보 묶음** O1·O2·O3 (+(12) ~ (15)) 을 처리한다. 이번 바퀴에서 붙은 후보 (15) 는 "`PspInitializeThunkContext` 는 모든 프로세스의 모든 유저 스레드가 거친다" 를 멈춤점 주의 예로 넣자는 것이다.
  - 그다음 **Q8 — 단계 2 시작**: `ntdll!LdrInitializeThunk`, `LdrpInitializeInternal`, `LdrpInitialize` callee. 이번에 남긴 두 가지(`+0xe` 대상 이름, 로더 본체가 `RDX` 를 어떻게 쓰나)가 거기서 풀린다.
  - 오리엔테이션실 **안**으로 들어가는 셈이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 커널 `NtContinue` 가 사물함 메모의 `Rip`(`RtlUserThreadStart`)로 **실제로** 보내나 | 커널 `NtContinue` 본문을 안 읽었다 `[공백]`. 이름만 본 후보 `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` | **Q17** (정적) · **Q29** (동적, `RtlUserThreadStart` 에서 `k`) |
| 바꿔 끼우기 앞뒤의 실제 값: `PspUserThreadStartup+0xae`/`+0xb3` 앞뒤 트랩 프레임 `Rip`·`Rcx`·`Rdx`·`Rsp` | 실행 때 값이다 | **Q42** (동적) |
| `LdrInitializeThunk` 입구의 `Rcx`(사물함 메모)·`[Rsp]`(돌아올 주소 칸이 정말 0 인가) | 정적으론 "쓰는 줄이 없다" 까지만. 칸 값은 실행 때 | **Q42** (동적): 입구 `r rcx rdx rsp`, `dq @rsp L2`, `dt ntdll!_CONTEXT @rcx Rip Rcx` |
| `PsNtdllExports` 실제 값, `PspSystemDlls[0]+0x20` 이 ntdll 시작 주소와 같은가 (`Rdx` 칸 이름 `[추측: ImageBase]`) | 부팅 때 채우는 값이다 | **Q42** (동적): `dq nt!PsNtdllExports L3`, `lm m ntdll` |
| test.exe 때 `PspCreateUserContext` 길인가, `PspGetContextThreadInternal`(부모 문맥 복사) 길인가 | 갈림 조건(섹션 참조)이 실행 때 값이다 | **Q34** (동적) |
| 곁가지가 실제로 도나: 계측 콜백 값, CET 그림자 스택이 켜지나(`KePopulateContinuationContext` 목적도 `[추측]`), 알림 마스크 bit4 | 조건만 읽었다 | **Q42** (동적, 갈림 값) |
| 스레드 만들기 알림 콜백 루프가 어느 알림이고, `PspNotifyThreadCreation` 과 어떻게 나누나 | 루프 본문·대상을 안 읽었다 `[추측]` | flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| 멈춘 첫 스레드가 `PspUserThreadStartup +0x26` 에서 잠드나, `PspInitializeThunkContext +0x2f5` 에서 잠드나 | 인터럽트·APC 가 실제로 들어오는 자리는 정적으로 못 정한다 | **Q42**·**Q19** (동적) |
| 네 스레드 모두 같은 길인가, 사물함 메모의 `Rcx` = 그 스레드의 `Win32StartAddress` 인가 | 다른 스레드 쪽은 참조 훑기만 했다 `[추측]` | **Q42** (동적) |
| `LdrInitializeThunk +0xe` 의 대상 이름 (`LdrpInitialize` 꼬리 점프 → `LdrpInitializeInternal`), 로더 본체가 `RDX` 를 어떻게 쓰나 | 이번 질문(커널 쪽) 밖이다 | **Q8** (정적, 단계 2 첫 항목) |
| 부팅 안에서 `PsLocateSystemDlls` 와 `PspInitPhase2` 의 앞뒤 순서 | 범위 밖 `[공백]` | flow.md 4장 공백 줄 (대기열에 따로 줄은 없다) |
| `RtlUserThreadStart` 뒤 → `mainCRTStartup` 까지 | 이번 범위 밖 | flow.md 2.10 (Q18·Q30) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run16-thunk-context.md`](run16-thunk-context.md) — 특히 "③ 바꿔 끼우기", "01_sprint `[관찰]` 두 개", "단계 1 (nt 지도) 정리"
- 정적 요청서: [`_workspace/sast/11_thunk-context.md`](../../_workspace/sast/11_thunk-context.md)
- 정적 결과서 (PDB 자료형 표, 원본 바이트로 읽은 곳, 질문별 답, 동적 확인 목록·추천 멈춤점, 스크립트 제안 4개): [`_workspace/sast/11_thunk-context_result.md`](../../_workspace/sast/11_thunk-context_result.md)
- 바퀴 전 관찰: [`runs/01_sprint/run4-thread-start.md`](../01_sprint/run4-thread-start.md) (콜스택, 부른 곳 없음, 스레드 4개)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "콜스택", "부르는 자리와 돌아올 자리", "이미지 섹션"
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.6 CONTEXT (업무 시작 메모)
  - [`run15-user-thread-startup-해설.md`](run15-user-thread-startup-해설.md) — 2.6 트랩 프레임·`IRETQ`·꼬리 점프, 2.7 이번 질문의 출발점, 2.9 멈춤 쪽지 고리
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" 의 run16 문단
  - "그림 1" `PspCreateUserContext` 상자, "그림 2" `PspInitializeThunkContext` 아래 두 상자와 "로더 끝 → 진입점" 묶음
  - "2.1" 의 `PspCreateUserContext`, `PspAllocateThread`, `KeInitThread`, `PspMapSystemDll` 절
  - "2.3" 의 `nt!PspInitializeThunkContext` 절, "01_sprint 관찰 두 개가 맞아떨어지는 방법", "커널이 `LdrInitializeThunk` 주소를 아는 법"
  - "2.4", "2.10", "3" (스레드 4개), "4. 공백 목록", "4.1"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q7, Q8, Q17, Q19, Q29, Q34, Q42, 결정 기록 (2026-10-22 "단계 1 끝")
