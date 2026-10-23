# run29 continue-start — 해설

> 이 글은 [run29 문서](run29-continue-start.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q17** — 2.10 로더 끝 → 진입점: `LdrInitializeThunk` 뒤 `NtContinue`, `RtlUserThreadStart` callee (`_workspace/plan.md`, 이번에 **끝**)
  - 새 항목 둘:
    - **Q57** (동적) — `NtContinue` 길의 CET·유저 APC 실제 값 (결과서 D5·D6).
    - **Q58** (작은 정적) — `ContextFlags` `0x10001b` 가정, 새 그림자 스택 포인터를 누가 쓰나, `KiInitiateUserApc` 본문, `RtlLeaveCriticalSection` 특례가 있는 이유.
  - 나머지 동적 확인거리(D1 ~ D8)는 새 번호 없이 **Q29·Q30·Q42·Q48** 에 붙었다.
  - 다음 항목: **Q18** — `KERNEL32!BaseThreadInitThunk` callee (정적). plan.md 는 "Q18 까지 끝나면 단계 2 끝" 이라고 적어 두었다.
- 근거:
  - run 문서 [`run29-continue-start.md`](run29-continue-start.md) (가장 중요)
  - 요청서 [`_workspace/sast/24_continue-start.md`](../../_workspace/sast/24_continue-start.md)
  - 결과서 [`_workspace/sast/24_continue-start_result.md`](../../_workspace/sast/24_continue-start_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntoskrnl 과 ntdll 을 같이 봤다. pdb GUID 는 flow.md 기준과 같다 (ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll `C3093720177DA6851519DCFFE8DF53FE1`).
  - 본문을 읽은 건 여섯 함수다:

| 함수 | rva | 크기 | 직접 호출 |
|---|---|---|---|
| `nt!NtContinue` | `0x6adde0` | 5바이트 | 0 (꼬리 `JMP` 1) |
| `nt!NtContinueEx` | `0x6addf0` | 710바이트 | 7곳 + 꼬리 `JMP` 1 |
| `nt!KiContinueEx` | `0x4241b0` | 512바이트 | 9곳 |
| `nt!KiContinuePreviousModeUser` | `0x2bc750` | 1226바이트 | 16곳 + 조각 1곳 (조각 안 2곳) |
| `ntdll!RtlUserThreadStart` | `0xacac0` | 91바이트 | 5곳 + 예외 필터 3곳 |
| `ntdll!NtContinue` | `0x161610` | 24바이트 | 0 (`SYSCALL`) |

  - 곁에서 몇 함수는 필요한 줄만 봤다: `KeTestAlertThread` 전체, `KiVerifyContextIpForUserCet` 앞쪽, `KePopulateContinuationContext`, `KxContextToKframes`·`KeContextToKframes` 의 거르는 줄.
  - 시스템 호출의 **들어가는 문·나가는 문**(`KiSystemCall64`·`KiSystemServiceExit`·`KiKernelExit`)은 본문을 **안 읽었다.** 이름과 위치만 찾았다.
  - 서비스 표(`KiServiceTable`)와 예외 범위 표는 PE 파일에서 직접 읽었다.

이 글은 [run16 해설](run16-thunk-context-해설.md) 의 **뒷이야기**다.

- run16 은 "사물함에 맡긴 메모를 들고 `NtContinue` 를 부른다" 까지 봤다. 그 뒤 **관리사무소 안에서 무슨 일이 일어나 본업 자리에 도착하나** 가 이번 바퀴다.
- run16 해설의 표 2.3 에서 8번 줄("관리사무소가 메모대로 본업 자리로 보내 준다", 안쪽 `[공백]`)이 이번에 채워졌다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 시스템 콜 (관리사무소 **신청 창구**) → [run9 해설 "시스템 콜"](run9-create-user-process-해설.md#시스템-콜)
  - 직접 호출과 간접 호출 → [run9 해설 "직접 호출과 간접 호출"](run9-create-user-process-해설.md#직접-호출과-간접-호출)
- CONTEXT (**업무 시작 메모**) → [run11 해설 2.6](run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다)
- APC (**쪽지**), IRQL (**방해 금지 표지판**) → [run13 해설 2.3](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등), [2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- 비휘발 / 휘발 레지스터 (**개인 서랍**) → [run15 해설 2.4](run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다) 끝 "새 말"
- 트랩 프레임 (**출발 메모**), `IRETQ` (**순간 이동**), `KiKvaShadow` (**보안 검색대 문**), 꼬리 점프 → [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 사물함·오리엔테이션실·본업 자리, CET 그림자 스택 첫 소개 → [run16 해설 2.6](run16-thunk-context-해설.md#26-장면--바꿔-끼우기--pspinitializethunkcontext), [2.7](run16-thunk-context-해설.md#27-장면--받아서-되돌리기--ldrinitializethunk--ntcontinue)
- 예외 보호 구역, 착지점(**착지 매트**) → [run17 해설 2.8](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일), [run27 해설 2.9](run27-init-graph-해설.md)
- 필터 ("이 사고를 여기서 잡을까?") → [run28 해설 2.10](run28-tls-init-call-해설.md#210-핵심--정정--tls-콜백-예외는-켜기-담당까지-안-온다)
- `BaseThreadInitThunk` (**출근 인사 담당**), `Kernel32ThreadInitThunkFunction` 칸 (**벽 주소록 "출근 인사 담당" 칸**) → [run25 해설 2.6](run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다)
- CFG 중개 (**교환원**, 통화 기록에 이름을 안 남김) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**
- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- CONTEXT = **업무 시작 메모**, 트랩 프레임 = 직원 책상에 붙은 **출발 메모**, `IRETQ` = 출발 메모대로 **순간 이동**
- `LdrInitializeThunk` = **오리엔테이션실**, `RtlUserThreadStart` = **본업 자리**, 유저 스택에 맡긴 원래 메모 = **사물함 메모**
  - flow.md 2.10 은 같은 것을 "사물함 쪽지", 출발 메모를 "책상 쪽지" 라고 적었다. 같은 말이다.
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. **`NtContinue` 는 "대답을 들고 돌아오는" 시스템 콜이 아니다.** 성공하면 관리사무소가 직원의 출발 메모를 **사물함 메모로 갈아 끼우고, 대답 없이 바로 순간 이동**시킨다 `[정적]` → 2.3 · 2.11
2. **메모를 갈아 끼우는 손은 `KiContinuePreviousModeUser +0x422` (ret `+0x427`) 다.** run16 이 이름만 본 두 후보 중 다른 하나(`KiContinueEx +0x1e0`)는 **커널 모드 길**이었다 `[정적]` → 2.6 · 2.8
3. **갈아 끼우기 전에 메모를 커널로 복사해서 꼼꼼히 검사한다.** 맞지 않는 칸은 거절 대신 **깎기도** 한다 `[정적]` → 2.7
4. **run16 의 CET 짐작이 맞았다.** 출근 때 그림자 스택에 올려 둔 `RtlUserThreadStart` 한 칸을, 여기서 메모의 `Rip` 와 **맞춰 본다.** 단 CET 비트 둘이 다 켜졌을 때만 `[정적]` → 2.9
5. **`RtlUserThreadStart` 는 출근 인사 담당 칸을 `(0, 진입점, PEB)` 로 부른다 (ret `+0x2c`).** 01_sprint 의 Worker 콜스택 `RtlUserThreadStart+0x2c` `[관찰]` 과 같은 자리다 `[정적]` → 2.13

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)
                                             (사물함에 맡기고 Rip 바꿔 끼우기)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   ├─ +0xe   로더 본체 (가게 세팅, DLL 올리기·켜기, TLS)                          (run17 ~ run28)
   └─ +0x18  NtContinue(사물함 메모, TRUE)                                         (run16)
        ══▶ SYSCALL ══▶ 커널 NtContinue … 출발 메모 갈아 끼우기 … ══▶ IRETQ        ← 이번 (커널 안)
        ══▶ RtlUserThreadStart                                                     ← 이번 (ntdll)
              └─ +0x2c  출근 인사 담당 칸 (= BaseThreadInitThunk 추정)             ← 이번 (부르는 자리)
                    └─ +0x17  진입점 (= mainCRTStartup 추정) → … → main           (다음 Q18)
```

- 지금 목표(프로세스 생성 → `main`)의 **거의 마지막 구간**이다. 로더 일이 다 끝난 뒤, `main` 을 향해 출발하는 순간이다.
- 한 스레드가 커널과 유저를 **한 번 더** 오간다. 처음 들어간 건 run15·run16 의 출근(`KiStartUserThread`)이었고, 이번은 로더 끝의 `NtContinue` 다.
- flow.md 로는 **2.10** 이 중심이다. 2.3, 2.4, 3장(스레드 4개), 4장 공백 표에도 줄이 더해졌다. 그림 2 에는 `nt` 상자 안에 새 상자 묶음 `(2) 로더 끝 NtContinue` 가 생겼다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. 커널 입구 `nt!NtContinue` → `NtContinueEx` 에서 둘째 인자(`TRUE`)는 무엇이 되나? 유저가 부르면 어느 함수로 가나?
2. 사물함 메모를 **어느 함수 어느 줄**에서 출발 메모(트랩 프레임)에 옮겨 적나? run16 후보 둘 중 어느 쪽인가? 그 전에 무엇을 검사하나? CET 는 맞춰 보나?
3. TestAlert(`TRUE`)는 무슨 일을 하나?
4. 유저로 어떻게 돌아가나? `Rcx` = 진입점이 **살아서** 가나? 실패하면 무엇을 돌려주나?
5. `RtlUserThreadStart` 는 무엇을 받고, 출근 인사 담당 칸을 **어떤 인자로** 부르나? ret 이 `+0x2c` 인가? 칸이 0 이면? 예외는?

### 바퀴 전에 알던 것

- `[정적]` (run16) `LdrInitializeThunk +0x13` (ret **`+0x18`**) `NtContinue(사물함 메모, DL = 1)`. 돌아오면 `+0x1a` (ret `+0x1f`) `RtlRaiseStatus`.
- `[정적]` (run16) 사물함 메모 = 첫 스레드의 처음 메모. `Rip` = `RtlUserThreadStart`, `Rcx` = exe 진입점, `Rdx` = PEB.
- `[정적]` (run16) `PspInitializeThunkContext +0x295` `KePopulateContinuationContext(원래 Rip)` — CET 일 때만 일한다. 목적은 `[추측]` "`NtContinue` 로 돌아갈 때 검사 맞추기".
- `[추측]` (run16, 원본 바이트 훑기) 출발 메모에 쓰는 곳 후보: `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0`. 본문은 안 읽음.
- `[정적]` (run19·run20) `RtlUserThreadStart +0x7` 이 칸 `Kernel32ThreadInitThunkFunction` 을 읽는다. 칸 값 = kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소.
- `[정적]` (run25) `BaseThreadInitThunk` (rva `0x2cd70`) 는 첫 인자 0 이면 `+0x12` (ret `+0x17`) 에서 시작 주소를 간접 호출하고, `+0x19` (ret `+0x20`) `RtlExitUserThread`.
- `[관찰]` (01_sprint) Worker 스레드 종료 콜스택: `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x20`. 메인 스레드에선 직접 안 봤다.
- `[추측]` (flow.md 2.10) 로더가 끝나면 `NtContinue` 로 `RtlUserThreadStart` 에 간다 / `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup`.

---

## 2. 찾은 것

### 2.1 한눈에 — 사물함 메모가 본업 자리에 닿기까지

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로. 모두 `[정적]` 이다 (단, 시스템 호출 처리기 안은 안 읽었다).

```
[유저 ntdll]  LdrInitializeThunk
 └─ (2) +0x18  NtContinue(사물함 메모, TRUE)                     (run16)
      │  +0x3 EAX = 0x43
      ══▶ +0x12 SYSCALL  (돌아올 칸 +0x14 — 성공하면 안 돌아옴)
[커널 nt]  (시스템 호출 처리기 — 본문 안 읽음)
      └─ nt!NtContinue          +0x0 MOVZX EDX,DL
          ══▶ +0x3 JMP  NtContinueEx
               ├─ (1) +0x86  KiContinueEx                         +0x5e 유저 길로
               │      ├─ (1) +0xce  KeTestAlertThread(1)
               │      └─ (2) +0xf0  KiContinuePreviousModeUser
               │              ├─ (1) +0x1ce  RtlpReadExtendedContext     유저 메모 → 커널 복사본
               │              ├─ (2) +0x39e  KiVerifyContextIpForUserCet  (CET 둘 다 켜졌을 때만)
               │              └─ (3) +0x427  KeContextToKframes
               │                      └─ +0x7d  KxContextToKframes    ← 출발 메모 갈아 끼우기
               ├─ (2) +0x160  KiInitiateUserApc                   (유저 APC 대기일 때만)
               │  +0x3ea RCX ← 출발 메모의 Rcx …
               ├─ (3) ══▶ +0x426 IRETQ                            (KiKvaShadow 꺼짐)
               └─ (3) ══▶ +0x40c JMP KiKernelExit ══▶ +0xa5 IRETQ (KiKvaShadow 켜짐, run15)
[유저 ntdll]  RtlUserThreadStart(rcx = 진입점, rdx = PEB)
 └─ +0x2c  (교환원 JMP, 프레임 없음) 칸 Kernel32ThreadInitThunkFunction(0, 진입점, PEB)
      = KERNEL32!BaseThreadInitThunk 추정 (칸 값은 Q48)
```

읽을 때 주의:

- `══▶` 는 호출이 아니라 **실행이 넘어감**이다. 위에서 세 번 나온다: `SYSCALL`(유저 → 커널), 꼬리 `JMP`, `IRETQ`(커널 → 유저).
- 그러니 이 그림은 "누가 누구를 불렀다" 의 나무가 **아니다.** `RtlUserThreadStart` 는 `NtContinueEx` 의 자식이 아니다. 순간 이동으로 도착한 곳이다.
- `NtContinueEx` 아래 세 번호 중 (3) 은 둘 중 **하나만** 탄다 (`KiKvaShadow` 값에 따라).

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 둘을 골랐다. "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 한 줄 읽기 ① — 출발 메모를 갈아 끼우는 줄

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `nt!KiContinuePreviousModeUser` | **`+0x422`** | **`+0x427`** | **`nt!KeContextToKframes(트랩, 예외, 복사본, 플래그, 1)`** | 검사 다 통과 | [정적] |

- **누가**: `KiContinuePreviousModeUser`. "유저가 낸 `NtContinue`" 를 처리하는 담당이다. 이름이 길지만 뜻은 "이전 모드가 유저일 때의 Continue".
- **어디서**: `+0x422` 에서 부르고 `+0x427` 로 돌아온다. 이 함수 맨 끝 쪽이다. 앞에서 검사를 다 하고 나서야 여기 온다 (→ 2.7).
- **누구를**: `KeContextToKframes`. 이 함수가 다시 `+0x7d` 에서 `KxContextToKframes` 를 부른다. [run16 해설 2.5](run16-thunk-context-해설.md#25-장면--출발-메모에-처음-옮겨-적기--kxcontexttokframes-run15-추측--정적) 에서 처음 출발 메모를 쓸 때 본 **바로 그 함수**다.
- **무엇을 위해**: 사물함 메모(정확히는 그 **커널 복사본**)의 칸을 **출발 메모(트랩 프레임)** 에 옮겨 적으려고. 마지막 인자 `1` 은 "유저 모드" 라는 뜻이다.
- 눈여겨볼 것: 이 줄이 끝나는 순간 출발 메모의 `Rip` 칸이 `RtlUserThreadStart` 가 된다 (→ 2.8). 이번 바퀴에서 **가장 중요한 한 줄**이다.

#### 한 줄 읽기 ② — 본업 자리에서 출근 인사 담당을 부르는 줄

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!RtlUserThreadStart` | `+0x27` | **`+0x2c`** | CFG 간접: 칸 `Kernel32ThreadInitThunkFunction(0, 시작 주소, 파라미터)` | 칸 ≠ 0 (그리고 `RtlLeaveCriticalSection` 아님) | [정적] + 01_sprint `RtlUserThreadStart+0x2c` [관찰] 과 같은 자리 |

- **누가**: `RtlUserThreadStart` (본업 자리). 순간 이동으로 막 도착한 직원이 처음 하는 일이다.
- **어디서**: `+0x27` 에서 부르고 `+0x2c` 로 돌아온다. 01_sprint 콜스택에 찍힌 `RtlUserThreadStart+0x2c` 가 **이 줄**이다.
- **누구를**: 이름이 명령에 박혀 있지 않다 ([간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)). 벽 주소록의 "출근 인사 담당" 칸에 적힌 주소다. 그게 `KERNEL32!BaseThreadInitThunk` 일 거라는 건 칸 **값**이라 아직 `[추측]` (Q48).
- **무엇을 위해**: `(0, 진입점, PEB)` 를 건네며 "이제 본업(진입점) 시작해 주세요" 를 맡기려고. 첫 인자 `0` 이 [run25 해설 2.6](run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다) 의 "첫 인자 0 = 본업 시작" 갈래와 맞는다.

### 2.3 핵심 ① 보통 시스템 콜은 돌아온다. `NtContinue` 는 안 돌아온다

사용자가 헷갈릴 만한 곳이라 천천히 간다. 먼저 **보통**이 어떤지 보고, 그다음 `NtContinue` 가 **어디서 다른지** 본다.

#### 보통 시스템 콜 — "신청서 내고, 대답 들고, 창구 앞으로"

run9 의 `NtCreateUserProcess` 같은 보통 시스템 콜을 생각해 보자. 흐름은 이렇다. (이 그림은 일반적으로 알려진 모양이다. 이번 바퀴에서 처리기 본문은 **안 읽었다**. 그래서 이 소절의 "보통" 쪽은 `[추측]` 이다.)

1. ntdll 의 작은 함수(스텁)가 번호를 싣고 `SYSCALL` 을 한다. 직원이 신청 창구로 간다.
2. 관리사무소는 들어오는 직원의 **지금 상태**를 출발 메모(트랩 프레임)에 적어 둔다. "다 끝나면 **창구 앞(`SYSCALL` 바로 다음 줄)** 으로 돌려보낼 것."
3. 담당 함수가 일을 하고 **대답(상태값)** 을 돌려준다.
4. 관리사무소의 **보통 출구**가 출발 메모를 보고 직원을 창구 앞으로 돌려보낸다. 직원은 대답을 손에 쥐고 다음 줄(`RET`)로 간다.

- 핵심은 2번이다. **출발 메모의 "돌아갈 곳" 은 들어올 때 적힌 그대로**다. 그래서 보통 시스템 콜은 늘 창구 앞으로 돌아온다.
- 이번 길에서 들어올 때 적히는 곳은 `ntdll!NtContinue +0x14` 일 것이다 `[추측: 처리기 본문은 안 읽음. 동적 Q29 (D1) 에서 확인]`.

#### `NtContinue` — "제 출발 메모를 이걸로 바꿔 주세요"

`NtContinue` 는 신청서 내용이 특이하다. 사물함 메모를 내밀며 이렇게 말한다:

> "이 메모대로 출발하게 해 주세요."

관리사무소가 하는 일 `[정적]`:

1. 사물함 메모를 받아 **커널로 복사하고 검사한다** (→ 2.7).
2. 직원의 출발 메모(트랩 프레임)를 **사물함 메모 내용으로 덮어쓴다** (`KiContinuePreviousModeUser +0x422`, → 2.8).
   - 이 순간 "돌아갈 곳" 이 `ntdll!NtContinue +0x14` 에서 **`RtlUserThreadStart`** 로 바뀐다.
3. 그리고 **대답을 돌려주지 않는다.** `NtContinueEx` 가 반환(`RET`)하지 않고, 자기 몸 안에서 바로 `IRETQ` 를 한다 (`+0x426`, → 2.11).

- 그러니 직원은 창구 앞(`+0x14`)으로 **돌아오지 않는다.** 바뀐 출발 메모대로 **본업 자리로 순간 이동**한다.
- "돌아오지 않는다" 는 말의 정확한 뜻은 이것이다: **`NtContinue` 를 부른 줄(`LdrInitializeThunk +0x13`)의 다음 줄(`+0x18`)로 실행이 다시 오지 않는다.**

#### 왜 굳이 보통 출구를 안 타나 — 단계별로

1. `[정적]` 사물함 메모에는 `Rcx` = 진입점, `Rdx` = PEB 가 들어 있다 ([run16 해설 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값)). `RtlUserThreadStart` 는 이 둘을 **첫째·둘째 인자로 받아야** 한다 (→ 2.13).
2. `rcx`·`rdx` 는 **휘발 레지스터**다 ([run15 해설 2.4 "새 말"](run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다)). "불린 쪽이 마음대로 써도 되는 칸" 이라, 보통 출구가 이 값을 지켜 준다는 보장이 없다.
   - 일반적으로 알려진 설명으로는, `SYSCALL`/`SYSRET` 짝은 `rcx` 칸을 **"돌아올 주소" 보관용**으로 쓴다 `[추측]` (일반적으로 알려진 설명). 그렇다면 보통 출구로 나가면서 `rcx` 에 진입점을 실어 보낼 수가 없다.
   - ntdll 스텁이 맨 앞에서 `+0x0 MOV R10,RCX` 로 첫 인자를 `r10` 으로 옮겨 두는 것도 이 때문으로 보인다 `[추측]` (같은 일반 설명에 기댐).
3. `[정적]` 그래서 `NtContinueEx` 는 `SYSRET` 쪽 보통 출구를 **안 타고**, 출발 메모에서 휘발 레지스터까지 **전부** 되살린 뒤 `IRETQ` 로 나간다 (→ 2.11).
4. `[정적]` 이걸 보장하는 "표시"(스레드 칸·플래그)는 **없다.** **경로 자체가 보장한다.** 출발 메모의 `Rcx` 칸에 쓰는 줄(`KxContextToKframes`)과 그 칸을 읽는 줄(`NtContinueEx +0x3ea`)이 한 길 위에 있을 뿐이다.

- 비유: 보통 창구는 **"대답 봉투"** 하나만 쥐여 주고 창구 앞으로 돌려보낸다. 손에 다른 걸 들려 보내지 않는다. `NtContinue` 창구는 아예 다른 문으로 내보낸다 — 사물함 메모에 적힌 **"왼손에 진입점, 오른손에 안내판(PEB)"** 을 그대로 쥐여서.

#### 실패하면 — 그때만 보통처럼 돌아온다

- `[정적]` 검사에서 걸리면 `NtContinueEx +0x473`·`+0x47a RET` 로 **보통 반환**한다. `EAX` = 상태값.
- 그러면 보통 출구로 `ntdll!NtContinue +0x14 RET` → `LdrInitializeThunk +0x18` → `+0x1a` (ret `+0x1f`) `RtlRaiseStatus(상태)` (run16).
- 그래서 [run16 해설 2.7](run16-thunk-context-해설.md#27-장면--받아서-되돌리기--ldrinitializethunk--ntcontinue) 의 짐작 "`NtContinue` 뒤에 '돌아오면 오류' 를 둔 건, 성공하면 돌아오지 않는다는 뜻" `[추측]` 이 이제 **코드로 뒷받침**된다 `[정적]`.
- 자세한 실패 값은 2.12.

### 2.4 창구까지 — ntdll 스텁과 번호 `0x43`

#### 새 말 — 시스템 콜 번호, 서비스 표

- **시스템 콜 번호** = ntdll 스텁이 `EAX` 에 싣는 숫자. 커널은 이 번호로 **어느 담당 함수**에게 넘길지 정한다 `[추측]` (일반적으로 알려진 설명). [run9 해설](run9-create-user-process-해설.md#시스템-콜) 에서 "번호표 보고 담당자 찾아가는" 모양이라고 짐작했던 그 번호표다.
- **서비스 표 (`KiServiceTable`)** = 커널 안의 **번호 → 담당 함수** 목록. PDB 공개 이름이고, rva `0xdadc0`, 489칸이다 `[정적]`.
  - 비유: 신청 창구 벽에 붙은 **번호표 안내판**. "43번 → ○○ 담당".

#### ntdll 스텁 `[정적]` (`ntdll!NtContinue`, rva `0x161610`, 24바이트)

| 자리 | 명령 | 뜻 |
|---|---|---|
| `+0x0` | `MOV R10,RCX` | 첫 인자(사물함 메모 주소)를 `r10` 으로 |
| **`+0x3`** | **`MOV EAX,0x43`** | **번호표 43** |
| `+0x8` | `TEST [0x7ffe0308],1` | bit0 이 켜져 있으면 `SYSCALL` 대신 `INT 0x2e` 갈래 |
| **`+0x12`** | **`SYSCALL`** | 창구로 |
| `+0x14` | `RET` | (보통이면) 대답 들고 돌아올 자리 |

#### 번호 `0x43` 의 담당은 정말 `nt!NtContinue` 인가 `[정적]`

- 결과서가 PE 파일에서 서비스 표를 직접 읽었다: **`[0x43]` = rva `0x6adde0` = `nt!NtContinue`**.
- 읽는 방법이 맞는지 두 가지로 확인했다:
  - 489칸 **모두** `_index.tsv` 의 함수 시작 주소와 맞았다.
  - 앞뒤 칸도 그럴듯하다: `[0x42]` = `NtDuplicateToken`, `[0x44]` = `NtQueryDefaultUILanguage`.
- 그래서 "ntdll `NtContinue` 가 커널 `NtContinue` 로 간다" 는 이름이 같아서 짐작한 게 아니라 **표로 확인한 것**이다.
- 그 사이 시스템 호출 처리기(들어가는 문)는 **본문을 안 읽었다.** flow.md 그림 2 의 `KiSystemServiceCopyEnd` 상자는 그림 1 에서 본 이름을 빌린 **추측 상자**다 (run3 `[관찰]` 은 다른 시스템 콜에서 본 것).

### 2.5 커널 입구 — 둘째 인자의 두 얼굴

#### `nt!NtContinue` 는 5바이트짜리 다리 `[정적]`

- `+0x0 MOVZX EDX,DL` — 둘째 인자에서 맨 아래 한 바이트(`DL`)만 남기고 위쪽을 0 으로 지운다.
  - 이유는 `[추측]`: 유저(`LdrInitializeThunk`)는 `DL = 1` 만 정했고, 위쪽은 쓰레기일 수 있어서.
- `+0x3 JMP NtContinueEx` — [꼬리 점프](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq). 돌아오지 않으니 콜스택에 `nt!NtContinue` 는 안 남는다.
- 비유: 43번 창구 직원은 신청서의 체크박스 하나만 정리해서 **안쪽 창구(`NtContinueEx`)로 바로 넘긴다.**

#### 새 말 — `TestAlert`, `_KCONTINUE_ARGUMENT`

- **TestAlert** = `NtContinue` 둘째 인자의 이름. run16 이 `TRUE` 라고 읽은 그 값이다. 하는 일은 2.10 에서 본다.
- **`_KCONTINUE_ARGUMENT`** = 둘째 인자 자리에 **작은 설정 묶음의 주소**를 넣을 때 쓰는 구조체 (0x18바이트). 공개 PDB 에 **구조체가 없다.** 열거형 `_KCONTINUE_TYPE` 만 있다. 그래서 칸은 `.asm` 으로 읽은 **오프셋 + 역할**로만 적는다.

#### 한 자리, 두 뜻 `[정적]` (`KiContinueEx +0x71 CMP RDI,0xff` / `+0x78 JA`)

| 둘째 인자 값 | 뜻 | 이번 길? |
|---|---|---|
| `0xff` 이하 | **BOOLEAN `TestAlert`** (0 또는 1) | **예** — `nt!NtContinue` 가 `DL` 만 남겨 0/1 로 넘김 |
| `0xff` 보다 큼 | **`_KCONTINUE_ARGUMENT` 유저 포인터** | 아니오 |

포인터일 때의 묶음 (참고만. 이번 길엔 해당 없음):

| 칸 | 역할 |
|---|---|
| `+0x0` | `ContinueType` (4 미만이어야 함) |
| `+0x4` | 플래그: bit0 TestAlert, bit1 "APC 먼저" `[추측: 이름]`, bit2 이상이면 거절 |
| `+0x8`·`+0x10` | 예약 (0 이어야 함) |

- 비유: 신청서 한 칸에 **체크박스(예/아니오)** 를 해도 되고, **"별지 참조, 몇 쪽"** 을 적어도 된다. 숫자가 작으면(0xff 이하) 체크박스로, 크면 별지 주소로 읽는다. 주소는 0xff 보다 훨씬 크니 헷갈리지 않는다.
- `ContinueType` 은 2.9 의 CET 이야기에서 한 번 더 나온다.

### 2.6 유저 길과 커널 길 — run16 후보 둘의 정체

#### 새 말 — 이전 모드 (`PreviousMode`)

- **이전 모드** = 이 시스템 콜을 **누가 냈나** — 유저 코드인가, 커널 코드인가 — 를 적어 두는 스레드 칸. `_KTHREAD.PreviousMode` (`+0x232`, PDB 타입) `[정적]`.
  - 비유: 신청서 맨 위 **"신청인: 가게 손님 / 관리사무소 직원"** 칸.
  - 커널 코드도 같은 담당 함수를 부를 수 있다. 그때는 이 칸이 "커널" 이다 `[추측]` (일반적으로 알려진 설명).

#### 갈림 `[정적]`

- `NtContinueEx` 는 **늘** `+0x81` (ret **`+0x86`**) `KiContinueEx(CONTEXT, 둘째 인자, 예외 프레임, 트랩 프레임)` 를 부른다. 자기 안엔 갈림이 없다.
- 갈림은 `KiContinueEx` 안에 있다: `+0x55` 현재 스레드 → **`+0x5e CMP [R14+0x232],0`** (`PreviousMode`) → `+0x66 JZ`.

| `PreviousMode` | 가는 곳 | 하는 일 |
|---|---|---|
| 0 (커널) | `+0x1e0` (ret `+0x1e5`) `KeContextToKframes` | 받은 포인터를 **그대로** 씀. 복사·검사 없음 |
| **0 아님 (유저, 이번 길)** | **`+0xeb` (ret `+0xf0`) `KiContinuePreviousModeUser`** | 복사·검사 후 `+0x422` 에서 `KeContextToKframes` |

- 그러니 run16 이 이름만 본 후보 둘은 **둘 다 맞는 "쓰는 자리"** 였다. 다만:
  - `KiContinuePreviousModeUser +0x422` = **유저 길** ← 이번 test.exe 가 타는 쪽.
  - `KiContinueEx +0x1e0` = **커널 길**. 유저가 부르면 안 탄다.
- 비유: 관리사무소 직원이 낸 신청서는 "우리 사람이니까" 그냥 처리한다. 가게 손님이 낸 신청서는 **복사해서 꼼꼼히 검사하는 창구**로 보낸다.
- 덤 `[정적]`: 결과서가 커널 실행 영역 전체에서 찾아보니, `KiContinueEx` 를 부르는 곳은 `NtContinueEx +0x81` 하나, `KiContinuePreviousModeUser` 를 부르는 곳은 `KiContinueEx +0xeb` 하나뿐이다.

### 2.7 갈아 끼우기 전 — 복사하고, 검사하고, 깎는다

유저 길에서 타는 순서 그대로다 (run 문서 "유저 길 검사 순서" 표). `KiContinueEx` 에서 시작해서 `KiContinuePreviousModeUser` 로 이어진다. 모두 `[정적]`.

| 순서 | 무엇 | 자리 | 실패하면 |
|---|---|---|---|
| ① | IRQL 을 APC_LEVEL 로 (낮을 때만) | `KiContinueEx +0x2a~+0x40`, 되돌림 `+0x180~+0x19d` | — |
| ② | CONTEXT 16바이트 정렬 | `KiContinueEx +0x93`/`+0xa9` | 정렬 예외 → ④ |
| ③ | TestAlert 참이면 CONTEXT 첫·끝 바이트 읽고 다시 쓰기 (쓰기 가능 검사) | `+0x82~+0xc1` | 접근 예외 → ④ |
| ④ | `KiContinueEx` 전체 `__try` `+0x6c~+0x177` | `.pdata` | 착지 `+0x177`: 예외 코드를 반환값으로 |
| ⑤ | `KiContinuePreviousModeUser` `__try` `+0x58~+0xb9`: 유저 CONTEXT 의 `ContextFlags` 읽기 + 인자 묶음 모양 | `.pdata` | 착지 `+0xb9` → `+0x2be` (예외 코드), 모양 틀림 `0xc000000d` |
| ⑥ | `ContextFlags` 검사 셋 | `+0xcf`·`+0x106`·`+0x186` | 음수면 반환. 단 **거절 대신 깎기** 갈래 있음 (아래) |
| ⑦ | **커널로 복사** (길이 → 스택 자리 → `memset` → 틀 → `RtlpReadExtendedContext`) | `+0x133`~`+0x1ce` | 음수 반환, 접근 예외는 ④ |
| ⑧ | `SegCs = 0x33` 고정 (WoW64·Pico 아니면) | `+0x27a`, `+0x293` | — |
| ⑨ | `Rsp` 검사 (`CheckStackExtents` 켜졌고 CONTROL 이 있으면) | `+0x212~+0x251` | `0xc000000d` |
| ⑩ | **`Rip` 검사 (CET 일 때만)** | `+0x367~+0x3da` | `0xc000060a` (감사 모드면 로그만 남기고 통과) |
| ⑪ | 확장 기능 막기 마스크 | `+0x403~+0x44e` | `0xc000000d` |
| ⑫ | **`KeContextToKframes`** | `+0x422` (ret `+0x427`) | — |

표가 길다. 꼭 알아 둘 것만 골라 풀어 보자.

#### (가) 왜 "커널로 복사" 하나 — ⑦

- `[정적]` 유저 길은 사물함 메모를 **그대로 쓰지 않는다.** `RtlpReadExtendedContext` (ret `+0x1ce`) 로 커널 쪽 자리에 **복사본**을 만들고, 그 복사본을 검사하고, 그 복사본을 출발 메모에 옮긴다 (2.2 ① 의 셋째 인자가 "복사본").
- 커널 길(2.6)은 복사 없이 바로 쓴다. 차이는 **신청인이 누구냐** 뿐이다.
- 왜 복사하나는 코드에 안 쓰여 있다. 일반적으로는, 검사한 뒤에 유저가 원본을 슬쩍 바꿔치기하지 못하게 하려는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 손님이 낸 메모를 **복사기로 한 장 떠서**, 그 복사본에 도장을 찍는다. 원본이 나중에 바뀌어도 상관없다.

#### (나) 새 말 — `ContextFlags`, 그리고 "깎기" — ⑥

- **`ContextFlags`** = CONTEXT 맨 앞의 **"이 메모에서 어느 칸 묶음이 유효한가"** 체크박스 모음 `[정적]` (칸 이름은 PDB 타입).
  - 이번에 중요한 묶음 둘 (결과서): **CONTROL** = `Rip`·`Rsp` 쪽, **INTEGER** = `Rcx`·`Rdx` 같은 정수 레지스터 쪽.
  - 비유: 업무 시작 메모 맨 위의 체크박스. "☑ 출발 위치 칸 유효, ☑ 손에 쥘 것 칸 유효 …".
- `[정적]` 검사(`RtlpValidateContextFlags2`)에서 음수가 나오면 보통은 그대로 반환(거절)이다.
- `[정적]` 그런데 결과가 `0xc000000d` 이거나 아키텍처 비트가 없으면 **거절하지 않고 깎는다**: `+0x459 AND EDI,0xf810001f` / `+0x45f BTS EDI,0x14` — 알려진 비트만 남기고 계속 간다.
- 비유: 체크박스에 이상한 표시가 섞여 있으면, 돌려보내지 않고 **아는 칸만 남기고 지운 뒤** 처리한다.

#### (다) `SegCs = 0x33` 고정 — ⑧

- `[정적]` WoW64(32비트)·Pico 가 아니면 복사본의 `SegCs` 를 `0x33` 으로 **덮어쓴다** (`+0x293`).
- 메모에 뭐라고 적혀 있든 "64비트 유저 모드로 나간다" 를 관리사무소가 정해 버리는 셈이다. [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 에서 본 대로, `IRETQ` 는 이 `CS` 칸을 꺼내 모드를 바꾼다.
- 32비트·Pico 갈래는 64비트 test.exe 가 안 탈 것 `[추측: test.exe 는 64비트]`.

#### (라) 나머지

- ①의 IRQL 은 [방해 금지 표지판](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)이다. 메모를 다루는 동안 APC 쪽지가 끼어들지 못하게 표지판을 한 단계 올려 두는 것으로 보인다 `[추측]` (목적은 코드에 안 쓰여 있다. 올리고 되돌리는 줄만 `[정적]`).
- ④⑤ 는 [예외 보호 구역](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)이다. 메모를 읽다가 사고(접근 예외 등)가 나면 [착지 매트](run27-init-graph-해설.md)로 떨어져 **예외 코드를 반환값으로** 바꾼다. 착지점 안에 호출은 없다.
- ⑩ 은 따로 길게 본다 (→ 2.9).

### 2.8 출발 메모 덮어쓰기 — 어느 칸이 바뀌나

#### 옮기는 칸 `[정적]` (`KxContextToKframes`, run16 표 + 이번에 본 거르는 줄)

| CONTEXT 칸 | → 출발 메모(트랩 프레임) 칸 | 옮기는 조건 | 이번 길의 값 (run16 `[정적]`) |
|---|---|---|---|
| `Rip` `+0xf8` | `+0x168` | `(플래그 & 0x100001) == 0x100001` (`+0x21`) | `ntdll!RtlUserThreadStart` |
| `Rsp` `+0x98` | `+0x180` | 같음 (`+0x35`) | 맡긴 문맥의 유저 스택 |
| `Rcx` `+0x80` | `+0x38` | `(플래그 & 0x100002) == 0x100002` (`+0xc9`/`+0xce`) | exe 진입점 (`TransferAddress`) |
| `Rdx` `+0x88` | `+0x40` | 같음 | PEB |

- 읽는 법: `0x100001` 의 `1` 이 CONTROL 체크박스, `0x100002` 의 `2` 가 INTEGER 체크박스다 (결과서). 앞의 `0x100000` 은 아키텍처 비트다 (2.7 (나) 의 `BTS EDI,0x14` 가 켜는 그 비트).
- 즉 **CONTROL 이 켜져 있어야 `Rip`·`Rsp` 가, INTEGER 가 켜져 있어야 `Rcx`·`Rdx` 가** 출발 메모로 간다 `[정적]`.

#### 사물함 메모의 체크박스는 켜져 있나

1. `[정적]` 사물함 메모를 만든 `PspInitializeThunkContext` 에 `+0x8d ADD R12D,0x10001b` 라는 줄이 있다 (run16 에서 본 함수).
2. `0x10001b` 에는 CONTROL(`1`)도 INTEGER(`2`)도 들어 있다 (결과서: CONTROL + INTEGER + 둘 더).
3. `[추측]` 이 값이 그대로 사물함 메모의 `ContextFlags` 가 됐다면 → 넷 다 옮겨진다. 단 "`RtlInitializeExtendedContext2` 가 받은 값을 그대로 쓴다" 는 가정에 기댄다.
4. 아직 확인 아님: 정적으로는 **Q58**, 실제 값은 동적 **Q29** (D2 에서 `r9d`).

#### 덤 `[정적]`

- 유저 모드면 `SegSs = 0x2b` 고정, `SegCs` 는 `0x33`/`0x23`.
- `CS = 0x33` 이면 `Rip` 를 48비트 부호 확장한다 (`KxContextToKframes +0x8c~+0xc0`). 목적은 `[추측]` (이상한 주소로 `IRETQ` 가 죽지 않게).

#### 비유로 — run16 의 "바꿔 끼우기" 를 거꾸로

| | 언제 | 출발 메모 `Rip` | 출발 메모 `Rcx` |
|---|---|---|---|
| run16 바꿔 끼우기 | 출근 도장 때 (`PspInitializeThunkContext`) | 본업 자리 → **오리엔테이션실** | 진입점 → **사물함 번호** |
| **이번 되돌리기** | 오리엔테이션 끝 (`KiContinuePreviousModeUser +0x422`) | (창구 앞) → **본업 자리** | → **진입점** |

- run16 에선 원래 메모를 사물함에 넣고 출발 메모를 바꿔 붙였다.
- 이번엔 사물함 메모를 꺼내 **출발 메모를 원래대로 되돌려 붙인다.**
- 그리고 둘 다 **`KxContextToKframes` 계열의 같은 칸 대응**으로 쓴다 (run16 해설 2.6 끝의 "쌍둥이 코드" 이야기와 같은 줄기).

### 2.9 CET 그림자 스택 — 미리 올려 둔 한 칸과 맞춰 보기

사용자가 어려워할 만한 곳이다. 그림 하나로 천천히 간다.

#### 새 말 — CET 그림자 스택 (다시, 조금 더)

- **CET 그림자 스택** = 돌아올 주소를 보통 스택과 **따로 한 벌 더** 적어 두고, 돌아갈 때 둘이 같은지 맞춰 보는 CPU 보안 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명. [run16 해설 2.6 곁가지](run16-thunk-context-해설.md#26-장면--바꿔-끼우기--pspinitializethunkcontext) 에서 한 줄로 봤다).
  - 비유: 직원이 이동할 때마다 **경비실 장부**에도 "다음에 갈 곳" 을 한 줄 적는다. 직원 수첩(보통 스택)은 고쳐 쓸 수 있어도, 경비실 장부는 아무나 못 고친다. 이동할 때 경비가 수첩과 장부를 **맞춰 본다.**
- **그림자 스택 포인터** = 그 장부의 **맨 위 줄 위치**. 결과서는 유저 쪽 것을 MSR `0x6a7` 로 읽고 쓴다고 적었다 `[정적]`.

#### 왜 `NtContinue` 에서 문제가 되나 `[추측]`

- 보통의 `RET` 는 "아까 온 곳으로" 간다. 그래서 장부 맨 위 줄과 자연스럽게 맞는다.
- `NtContinue` 는 **메모에 적힌 아무 곳으로나** 보내 달라는 부탁이다. 맞춰 볼 "아까 온 곳" 이 없다.
- 그러면 누군가 가짜 메모로 엉뚱한 곳에 보내 달라고 할 수도 있다. 그래서 확인이 필요해 보인다. (여기까지는 장치의 목적에 대한 짐작이라 `[추측]`.)

#### 넣는 쪽 — run16 에서 본 것 `[정적]`

- 출근 도장 때 `PspInitializeThunkContext +0x295` `KePopulateContinuationContext(원래 Rip)` 가,
- 스레드 `+0x74` bit20 이면,
- 유저 그림자 스택에 **원래 `Rip`(= `RtlUserThreadStart`) 한 칸을 올려 둔다.**
- 비유: 오리엔테이션실로 보내면서, 경비실 장부 맨 위에 **"이 직원의 다음 목적지 = 본업 자리"** 를 미리 한 줄 적어 둔다. 일종의 **예약 줄**이다.

#### 맞춰 보는 쪽 — 이번에 본 것 `[정적]`

`KiContinuePreviousModeUser +0x399` (ret `+0x39e`) `KiVerifyContextIpForUserCet`:

1. `Rip` 가 `0x10000 ~ 0x7ffffffdffff` 밖이면 **바로 거절** (`+0x2e~+0x42`).
2. `Rip == PsNtdllExports+0x10` (= `RtlUserThreadStart`) 이면 `ContinueType` 을 0 → 1 로 바꿔 **그림자 스택 대조 쪽으로** 보낸다 (`+0x9d`/`+0xa6`). BOOLEAN 길(이번 길)이 이 특례를 탄다.
3. 그림자 스택 포인터를 읽고 (`+0x13a~+0x145`), **그 칸과 `CONTEXT.Rip` 를 비교**한다 (`+0xfc~+0x101`).
4. 같으면 포인터를 한 칸 올리고 성공.

- 비유로 이으면: 직원이 사물함 메모를 내며 "본업 자리로 보내 주세요" 하면, 경비가 장부 맨 위를 본다. **"본업 자리" 예약 줄이 있다 → 통과**, 그 줄은 지운다(한 칸 올림).
- 그래서 run16 의 `[추측]` "`KePopulateContinuationContext` 는 `NtContinue` 로 돌아갈 때 검사 맞추기" 는 **코드로 뒷받침된다** `[정적]`. 넣는 줄(run16)과 맞춰 보는 줄(이번)이 **짝**으로 보였다.
- `PsNtdllExports+0x10` 은 [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다) 의 **벽 주소록** "본업 자리" 칸이다. 넣을 때도, 맞춰 볼 때도 같은 주소록을 본다.

#### 언제 이 검사를 하나 — 두 비트가 **다** 켜졌을 때만 `[정적]`

| 누구의 비트 | 칸 | 뜻 (PDB 이름) |
|---|---|---|
| 스레드 | `_KTHREAD +0x74` bit20 | `CetUserShadowStack` |
| 프로세스 | `_EPROCESS +0x754` bit17 | `UserCetSetContextIpValidation` |

- 둘 중 하나라도 꺼져 있으면 `KiContinuePreviousModeUser +0x2a0` (또는 `+0x378`) 에서 **건너뛴다.** 경비실이 아예 없는 셈이다.
- 거절이면 결과는 `0xc000060a`. 다만 **감사 모드**면 로그만 남기고 통과시킨다 (`+0x3d5`, ret `+0x3da` `KiLogUserCetSetContextIpValidationFailure`).
  - **감사 모드** = run 문서 표현대로 "로그만 남기고 통과" 하는 설정. 비유: 경비가 막지는 않고 **장부에 '이상함' 만 적는** 날.
- 이 VM 에서 두 비트가 켜졌는지는 정적으로 못 정한다 → 동적 **Q57**.

#### 아직 빈 곳 `[공백]`

- 검사가 포인터를 "한 칸 올린" 새 값을, `KiContinuePreviousModeUser` 안에서 **다시 쓰는 줄은 못 찾았다.**
- 그러니 유저 그림자 스택 포인터가 실제로 어떻게 되는지는 모른다 → 정적 **Q58**.
- 비유: 경비가 장부 맨 위 줄을 지웠다고 **메모만** 했는지, 장부에 **실제로 반영**했는지가 아직 안 보인다.

### 2.10 TestAlert — 쪽지를 "배달 대상" 으로 올리기만 한다

#### 무엇을 하나 `[정적]`

- 유저 길은 `KiContinueEx +0xc9` (ret **`+0xce`**) `KeTestAlertThread(1)`. 인자 `1` = 유저 모드.
- 시점: **출발 메모를 덮어쓰기 전**이다 (`+0xeb` 보다 앞).
- `KeTestAlertThread` 안:
  - `Alerted[모드]` 깃발이 켜졌으면 끄고 TRUE.
  - 아니고 유저 모드이며 **유저 APC 큐가 비어 있지 않으면** `+0x83 OR [thread+0xc2],2` — `ApcState.UserApcPending` 을 켠다.
  - 돌려준 값은 `KiContinueEx` 가 **안 쓴다.**

#### 실제 배달은 나가는 문에서 `[정적]`

- `NtContinueEx` 출구에 고리가 있다: `+0x148 TEST byte [thread+0xc2],3` → 0 이 아니면 `+0x15b` (ret **`+0x160`**) `KiInitiateUserApc` 를 하고 처음부터 다시 검사.
- 이 고리는 **TestAlert 가 거짓이어도** 돈다. 비트가 이미 켜져 있으면 배달한다.
- 그러니 TestAlert(`TRUE`) 의 몫은 "**큐에 들어 있던 유저 APC 를 지금 배달 대상으로 올리기**" 다.
- 비유 ([run13 해설](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등) 의 **쪽지**): 직원 앞으로 온 유저용 쪽지가 우편함에 있으면, TestAlert 는 "**나가기 전에 펼쳐 볼 것**" 표시만 붙인다. 실제로 쪽지를 펼치게 하는 건 문 앞의 확인 담당(`+0x148` 고리)이다.
- [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 표 5번의 "나가기 전 유저용 쪽지 확인" 과 **같은 모양의 고리**다.

#### 배달할 쪽지가 있으면 `[추측]`

- 순서상 출발 메모가 사물함 메모로 **먼저 덮이고**, 그 **뒤에** `KiInitiateUserApc` 가 그 위에 APC 진입을 만든다.
- 그러면 원래 문맥은 APC 틀에 보관될 것이다 `[추측: KiInitiateUserApc 본문 안 읽음]` → 정적 **Q58**.

#### test.exe 시동 때는? `[추측]`

- 배달될 유저 APC 는 **보통 없을 것**이다. sast/10 에서 유저 APC 를 넣는 줄을 못 봤다.
- 실제로는 동적 **Q57** (`+0x148` 에서 비트, `KiInitiateUserApc` 가 걸리나).
- 덧: 인자 묶음의 "APC 먼저" 갈래(`KiContinueEx +0x114`, ret `+0x119` `KiDeliverApc`)는 BOOLEAN 길에선 **못 탄다** `[정적]`.

### 2.11 유저로 나가기 — 반환 없이 `IRETQ`, 그래서 `Rcx` 가 산다

2.3 에서 말로 한 것을 이번엔 줄 단위로 따라간다.

#### 출발 메모 다시 보기 (짧게)

- [트랩 프레임](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) = 커널로 들어올 때 유저 쪽 레지스터를 적어 두고, 나갈 때 되살리는 묶음. 비유로 **출발 메모**.
- [`IRETQ`](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) = 스택 맨 위 다섯 칸 `RIP, CS, RFLAGS, RSP, SS` 를 한꺼번에 꺼내 CPU 에 넣는 명령. 비유로 **순간 이동**.
- 이번 이야기에서 중요한 건 하나다: **`IRETQ` 직전에 `RSP` 를 출발 메모의 `Rip` 칸(`+0x168`)에 맞추면, 출발 메모에 적힌 곳으로 순간 이동한다.** run15 에서 출근 첫 발이 그렇게 나갔다.

#### 성공 판정 `[정적]`

- `NtContinueEx +0x86 TEST EAX,EAX` / `+0x88 JLE +0x473`.
- `KiContinueEx` 가 **1** 을 돌려주면 성공 → 아래로. 아니면 `+0x473` 실패 반환 (→ 2.12).

#### 나가는 순서 `[정적]` (모두 `NtContinueEx +`)

| # | 자리 | 하는 일 | 비유 (태그 없음) |
|---|---|---|---|
| 1 | `+0xb7~+0x10b` | 예외 프레임에서 **비휘발** 레지스터 되살림 | 개인 서랍 정리 |
| 2 | `+0x10f CLI`, `+0x112`·`+0x121`, `+0x126` | 인터럽트 끄기, 커널 그림자 스택 한 칸 건너뛰기 (목적 `[추측]`), 새 출발 메모 `SegCs` 가 유저면 계속 | "방해 사절", 나가는 곳 확인 |
| 3 | `+0x133~+0x16a` | 유저 APC 고리 (2.10) | 나가기 전 쪽지 확인 |
| 4 | `+0x16c`~`+0x1d4` 등 | 출구 마무리 호출들, 커널 그림자 스택·MSR·RSB 채우기 사다리 (추측 실행 완화 `[추측]`) | 보안 점검 몇 가지 |
| 5 | **`+0x3be~+0x3ee`** | 출발 메모에서 **휘발** 레지스터 되살림: `XMM0~5`·`R11`·`R10`·`R9`·`R8`·`RDX`·**`RCX`(`+0x3ea`)**·`RAX` | 출발 메모의 "손에 쥘 것" 칸 쥐여 주기 |
| 6 | `+0x3f2`~`+0x3fc` | `RSP` = 출발 메모 `Rip` 칸 | 출발 메모를 읽을 위치에 펼쳐 놓기 |
| 7 | `+0x403` | `KiKvaShadow` 로 갈림 | 나가는 문 고르기 |
| 7가 | `+0x423 SWAPGS` → **`+0x426 IRETQ`** | 꺼짐 | 그냥 나가는 문 |
| 7나 | **`+0x40c JMP KiKernelExit`** (→ 그 안 `+0xa5 IRETQ`, run15) | 켜짐 | 보안 검색대 문 |

- 2번의 "커널 그림자 스택 한 칸 건너뛰기" 는 결과서 짐작으로 "**이 함수가 `RET` 하지 않으니** 반환 주소 칸을 버리는 것" 이다 `[추측]`. 2.9 의 **유저** 그림자 스택과는 다른 이야기다 (커널 쪽 장부).

#### 핵심은 5번 — 왜 `Rcx` = 진입점이 사나

한 단계씩:

1. `[정적]` 2.8: `KxContextToKframes` 가 사물함 메모의 `Rcx`(진입점)를 출발 메모 **`+0x38`** 칸에 쓴다.
2. `[정적]` 5번: `NtContinueEx +0x3ea` 가 바로 그 칸(결과서: `MOV RCX,[RBP−0x48]`)을 읽어 `RCX` 레지스터에 넣는다.
3. `[정적]` 6 ~ 7번: 그 뒤 `RCX` 를 건드리는 줄 없이 `IRETQ` 로 나간다.
4. → `RtlUserThreadStart` 에 도착했을 때 **`rcx` = 진입점, `rdx` = PEB** 다 `[정적]`. 값 자체는 동적 (Q29).

- 2.3 의 결론과 같다: **표시 칸·플래그는 없고, 경로가 보장한다.**
- 비유: 보통 출구는 대답 봉투만 쥐여 준다. 이 출구는 출발 메모의 **"손에 쥘 것" 칸까지 하나하나 쥐여 주고** 순간 이동시킨다.

#### run15 의 출근 첫 발과 모양이 같다

- `[정적]` (flow.md 2.10) 예외 프레임 → APC 고리 → 휘발 레지스터 → `KiKvaShadow` 갈림 → `IRETQ`. [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) 의 표 1 ~ 10번과 같은 순서다.
- 그리고 **같은 변수 `KiKvaShadow`** 로 갈린다. 그러니 이 VM 에서 어느 문으로 나가는지는 Q42 하나로 두 곳이 같이 정해진다.
- flow.md 2.3 에 새로 붙은 말 `[정적]`: **이 스레드는 커널 → 유저 `IRETQ` 를 두 번 탄다.**
  - 처음: `KiStartUserThread` 출구 → `LdrInitializeThunk` (오리엔테이션실로).
  - 두 번째: 로더 끝 `NtContinueEx` → `RtlUserThreadStart` (본업 자리로).

#### 커널 모드로 돌아가는 갈래도 있다 (참고)

- `+0x12d JZ +0x428` → `+0x471 IRETQ`. 신청인이 커널(2.6)일 때 쓰는 문이다. 유저 복귀에선 안 탄다 `[정적]`.

### 2.12 실패하면 — 무엇을 들고 돌아오나 `[정적]`

- `NtContinueEx +0x473 ADD RSP,0x138` / `+0x47a RET`, `EAX` = 상태값.
- 보통 출구로 `ntdll!NtContinue +0x14 RET` → `LdrInitializeThunk +0x18` → `+0x1a` (ret `+0x1f`) `RtlRaiseStatus(상태)` (run16).

코드에 보이는 값:

| 값 | 언제 |
|---|---|
| `0xc000000d` | 인자 묶음 모양 틀림, `Rsp` 검사, 확장 기능 마스크 (2.7 ⑤⑨⑪) |
| `0xc000060a` | CET `Rip` 검사 거절 (2.9, 감사 모드가 아닐 때) |
| 예외 코드 그대로 | 정렬 틀림, 접근 예외 등 (2.7 ②③⑦) |
| 0 | 인자 묶음의 "APC 먼저" 갈래. **BOOLEAN 길은 못 탐** |

- 비유: 메모가 엉터리면 경비·창구가 **"이건 못 보내 드려요" 사유서**를 쥐여 창구 앞으로 돌려보낸다. 직원은 그 사유서를 들고 `RtlRaiseStatus` 로 사고를 알린다.

### 2.13 본업 자리 `RtlUserThreadStart` — 무엇을 받고, 누구를 부르나

#### 받는 것 `[정적]`

- `rcx` = 시작 주소 (`+0x4 MOV R9,RCX` 로 보관), `rdx` = 파라미터 (`+0x1a MOV R8,RDX`).
- 첫 스레드면 **진입점과 PEB** 다 (run16 메모 칸 + 2.11 되살리기).

#### 세 갈래 `[정적]`

`+0x7` 에서 칸 `Kernel32ThreadInitThunkFunction` (rva `0x1d2760`) 을 읽고 `+0xe`/`+0x11 JZ +0x37`:

| 갈래 | 조건 | 하는 일 |
|---|---|---|
| **(1) 보통** | 칸 ≠ 0 | `+0x1d RDX = 시작 주소`, `+0x25 ECX = 0` → **`+0x27` CFG 간접 호출 (ret `+0x2c`)** = `칸(0, 시작 주소, 파라미터)` |
| (2) 특례 | 칸 값 == `RtlLeaveCriticalSection` 주소 | `+0x30` (ret `+0x35`) **직접** 호출 |
| (3) 칸 없음 | 칸 == 0 | `+0x3d` (ret `+0x42`) 시작 주소를 CFG 간접으로 **직접** → `+0x44` (ret `+0x49`) `RtlExitUserThread(결과)` |

- (1) 이 test.exe 가 탈 것으로 보이는 길이다 (칸 값이 `BaseThreadInitThunk` 라면, Q48).
- (2) 는 [run28 해설 2.8](run28-tls-init-call-해설.md#28-핵심--tls-전용-안내원-imagetlscallbackcaller--어댑터) 의 `ImageTlsCallbackCaller` 에서 본 "`RtlAcquireSRWLockExclusive` 직접 호출 갈래" 와 비슷한 모양이다. 컴파일러가 흔한 대상을 직접 호출로 바꾼 것 같다는 짐작 `[추측]` 도 같다 → 이유 정하기는 정적 **Q58**.
- (3) 은 칸이 비어 있을 때 본업을 직접 하고 바로 퇴근하는 길이다. 시작 함수의 반환값이 스레드 종료 코드가 된다 `[정적]` (결과서). 언제 칸이 비어 있는지(출근 인사 담당이 아직 안 정해졌을 때?)는 근거에 없다 `[추측]`.

#### 인자 모양이 run25 와 맞물린다 — 단계별로

1. `[정적]` (run25) `BaseThreadInitThunk` 는 `(ECX, RDX = 시작 주소, R8 = 파라미터)` 를 받는다. 첫 인자 0 이면 `+0x12` (ret `+0x17`) 에서 `RDX` 를 `RCX = R8` 로 부른다.
2. `[정적]` (이번) `RtlUserThreadStart +0x27` 이 싣는 것: `ECX = 0`, `RDX` = 시작 주소, `R8` = 파라미터.
3. → 둘을 이으면 **`BaseThreadInitThunk(0, 진입점, PEB)` → 진입점(PEB)** 모양이 된다 — 칸 값이 `BaseThreadInitThunk` 라면 `[추측]` (Q48).
4. 덤 `[정적]`: 같은 칸을 `LdrpInitializeProcess +0x1b5e` 는 `(1,0,0)` 으로 부른다 (run19·run20). 그건 run25 의 "첫 인자 0 아님" 갈래다. **같은 칸, 첫 인자만 다른 두 번의 호출**이다.

- 비유 (run25 이어서): 본업 자리에 도착한 직원은 벽 주소록의 **"출근 인사 담당" 칸**을 보고, 그 담당에게 `[0 | 진입점 | 안내판]` 서류를 낸다. 0 은 "인사 마치면 본업 시작시켜 주세요" 표시다. 가게 세팅 때(`(1,0,0)`)는 "본업 말고 준비만" 표시였다.

#### 교환원은 통화 기록에 안 남는다 `[정적]`

- `+0x27` 의 `CALL 0x180172020` → `_guard_dispatch_icall` → 칸. **전부 `JMP`** 다 ([run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다) 와 같음).
- kernel32 `BaseThreadInitThunk +0x12` 의 중개(`0x180087010`)도 `JMP` → `JMP [칸]` 둘뿐이다 (`.asm` 확인).
- 그러니 콜스택에는 교환원 프레임이 안 보인다 (→ 2.14).

#### 사고가 나면 — 가게 문을 닫는다 `[정적]`

- `RtlUserThreadStart` 는 **전체가** 예외 보호 구역이다: `__try` `+0x7~+0x4a`.
- [필터](run28-tls-init-call-해설.md#210-핵심--정정--tls-콜백-예외는-켜기-담당까지-안-온다) `RtlUserThreadStart$filt$0`: 전역 `RtlpUnhandledExceptionFilter` 를 풀어 값이 있으면 그걸, 없으면 `RtlUnhandledExceptionFilter2` 를 부른다.
- 착지 `+0x4a` → `+0x50` (ret `+0x55`) **`NtTerminateProcess(-1, 예외 코드)`**.
- 즉 **본업(시작 함수) 안에서 아무도 처리 안 한 예외가 여기까지 올라오면 프로세스가 끝난다.**
- 비유 (run28 의 소화기 이어서): 본업 자리 층에는 **건물 전체 비상벨**이 달려 있다. 위층 어디서도 못 끈 불이 여기까지 오면, 비상벨이 울리고 **가게 문을 닫는다.**
- 주의: 디컴파일 `.c` 는 `NtTerminateProcess` 를 정상 흐름처럼 그렸다. 실제로는 착지점이다 (run 문서 도구 메모). `.asm` 만 믿을 것.

#### 돌아오지 않는 설계 `[정적: run25 + 이번]`

- (1) 갈래는 `+0x2c JMP +0x56` → `RET` 가 있긴 하다.
- 하지만 `BaseThreadInitThunk` 는 첫 인자 0 에서 `RtlExitUserThread` 로 끝나 **돌아오지 않는다** (run25).
- 게다가 어쩌다 돌아와도 `RET` 가 꺼낼 칸은 **아무도 안 썼다** (run16: `Rsp = R15−0x28` 칸에 쓰는 줄 없음) `[추측]`.
- 비유: 본업 자리엔 "끝나면 여기로 돌아오세요" 책갈피가 없다. 대신 출근 인사 담당이 **퇴근(스레드 종료)까지 책임진다.**

### 2.14 `main` 에서 콜스택을 찍으면 — 예측과 01_sprint 관찰

#### 예측 `[정적]` (노선도로 그린 것)

아래부터:

1. `ntdll!RtlUserThreadStart+0x2c` — 2.2 ② 의 줄
2. `KERNEL32!BaseThreadInitThunk+0x17` — run25 의 `+0x12` 간접 호출이 돌아올 자리
3. `test!mainCRTStartup+?` — 이번 범위 밖 (Q18·Q30)
4. … (CRT 함수들, 이번 범위 밖)
5. `test!main`

- 1·2 는 `[정적]`. 그 사이 교환원(CFG 중개) 프레임은 **안 보일 것**이다 (2.13).
- 버스를 타 보기 전 예측이다 → 동적 **Q30** (D8).

#### Worker 관찰과 숫자가 왜 하나 다른가

- 01_sprint Worker 종료 콜스택 `[관찰]`: `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x20`.
- `main` 예측: `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x17`.
- 차이 `[정적]` (run25): `+0x20` 은 `RtlExitUserThread` 호출(`+0x19`)이 돌아올 자리, `+0x17` 은 본업 호출(`+0x12`)이 돌아올 자리다.
  - Worker 는 **본업을 마치고 퇴근 중**이던 순간이었고, `main` 은 **본업 한가운데**다. 같은 담당의 다른 줄이다.
- `+0x2c` 는 둘 다 같다. 이번에 그 자리가 **코드로 확인**됐다 `[정적]`.
- flow.md 3장에 붙은 짐작: 그 Worker 의 바로 위가 `BaseThreadInitThunk+0x20` 이었으니, **이 프로세스에서 칸 값은 `BaseThreadInitThunk` 였다** 고 읽을 수 있다 `[추측: Worker 관찰에서 메인 스레드로 옮겨 짚음, 같은 전역 칸]` (Q48).

#### `RtlUserThreadStart` 입구에서 찍으면 `[추측]`

- 돌아올 칸을 아무도 안 썼으니, `RtlUserThreadStart` 혼자 보이거나 그 아래가 쓰레기일 수 있다 (D4 에서 확인, Q29).
- [run16 해설 2.8](run16-thunk-context-해설.md#28-01_sprint-의-수수께끼-두-개--같은-칸의-전-과-후) 의 "`LdrInitializeThunk` 는 부른 곳 없음" 과 같은 까닭이다. 두 곳 다 `call` 이 아니라 **순간 이동**으로 도착했다.

### 2.15 정리 — 비유로 한 번 더

- 오리엔테이션(로더)이 끝났다. 직원은 사물함 번호(`RBX`)로 **사물함 메모**를 꺼낸다.
- 신청 창구에 **43번 번호표**를 뽑고 "이 메모대로 출발하게 해 주세요(그리고 쪽지 확인도)" 를 낸다 (`SYSCALL 0x43`).
- 43번 창구 직원은 체크박스 하나만 정리해 **안쪽 창구**로 넘긴다 (`nt!NtContinue` → `NtContinueEx`).
- 안쪽 창구는 신청서 맨 위 **"신청인: 가게 손님"** 을 보고 꼼꼼 창구로 보낸다 (`PreviousMode` → `KiContinuePreviousModeUser`).
- 꼼꼼 창구는 메모를 **복사기로 떠서**, 체크박스를 정리(깎기)하고, `CS` 칸을 "64비트 유저" 로 못 박고, 경비실이 있으면 **장부 맨 위 예약 줄**(`RtlUserThreadStart`)과 메모 목적지를 맞춰 본다.
- 다 통과하면 복사본대로 **책상 출발 메모를 갈아 붙인다** (`+0x427`). 목적지 = 본업 자리, 손에 쥘 것 = 진입점·안내판.
- 안쪽 창구는 **대답 봉투를 안 만든다.** 쪽지 확인을 하고, 출발 메모의 "손에 쥘 것" 칸까지 쥐여 준 뒤, 문(그냥 문 / 보안 검색대 문)으로 **순간 이동**시킨다 (`IRETQ`).
- 직원은 창구 앞이 아니라 **본업 자리**에 서 있다. 손에는 진입점과 안내판.
- 본업 자리에서 벽 주소록의 **출근 인사 담당**에게 `[0 | 진입점 | 안내판]` 을 낸다 (`+0x2c`). 교환원을 거치지만 기록엔 안 남는다.
- 위층 어디서도 못 끈 불은 본업 자리 층의 **비상벨**이 받아 가게 문을 닫는다 (`NtTerminateProcess`).

### 2.16 곁가지 — 도구 메모

run 문서 "디버거 메모 / 도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- **WinDbg 오프셋 표시** `[추측]`: `NtContinueEx` 뒤쪽은 `.asm` 에서 "조각 `FUN_1406ae17a`" 로 보이지만, `.pdata` 상 같은 함수 범위(`0x6addf0~0x6ae26b`)다. WinDbg 는 `nt!NtContinueEx+0x426` 로 보여 줄 가능성이 높다. 안 맞으면 `bp nt+0x6ae216` 처럼 rva 로 건다.
- `_calls.tsv` 의 조각 안 호출은 조각 기준으로만 찍힌다 (결과서 스크립트 문제 1·2, 급하지 않음).
- Ghidra 가 `KePopulateContinuationContext +0x30` 을 4바이트 `WRUSSD` 로 읽었지만 원본 바이트는 8바이트 `WRUSSQ` 다. 답(한 칸 = `Rip` 하나)엔 영향 없음.
- **디버거가 붙어서 달라질 수 있는 점** `[추측]`: 디버거가 거는 유저 APC 는 이 길의 `KeTestAlertThread`·`KiInitiateUserApc` 에 안 보일 것. 소프트웨어 중단점은 명령을 `0xcc` 로 바꿀 뿐이라 `IRETQ` 직전 상태를 바꾸지 않는다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run28 뒤) | 후 (run29) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run28 | **run9 ~ run29** (ntoskrnl·ntdll). 서비스 표를 PE 파일에서 읽었다는 줄, PDB 에 없는 `_KCONTINUE_ARGUMENT` 는 오프셋+역할로 적었다는 줄 |
| 두 그림 사이 설명 | "커널 `NtContinue` 안쪽은 아직 `[공백]`" | 그 줄 **취소선**. 새 문단: 굵은 화살표를 코드 사슬로 바꿨다, 커널 쪽은 그림 2 `nt` 상자 안 `(2)`, `KiSystemServiceCopyEnd` 는 추측 상자, `RtlUserThreadStart` → `BaseThreadInitThunk` 는 `?` 그대로 |
| 그림 2 `nt` | `(1) 멈춤 APC 처리` 묶음까지 | **새 묶음 `(2) 로더 끝 NtContinue 시스템 콜 — 유저 길 (정적 run29)`**: `KiSystemServiceCopyEnd`(추측) · `nt!NtContinue` · `NtContinueEx` · `KiContinueEx` · `KeTestAlertThread` · `KiContinuePreviousModeUser` · `RtlpReadExtendedContext` · `KiVerifyContextIpForUserCet` · `KeContextToKframes` · `KxContextToKframes` · `KiInitiateUserApc` · `KiKernelExit` |
| 그림 2 `PH4` 제목 | "NtContinue 호출은 정적, 그 뒤는 추측" | "**코드 사슬은 정적, 실행은 미관찰**" |
| 그림 2 `q1` → `r1` 화살표 | `==>` "실행이 넘어감 — 커널 NtContinue 안은 공백 (추측)" 한 개 | `SYSCALL 0x43` ==> 커널 묶음 ==> **`IRETQ +0x426`** 또는 **`JMP +0x40c` → `KiKernelExit` → `IRETQ +0xa5`** 두 갈래로 `r1` 에 들어감 |
| 그림 2 `q1`·`r1`·`s1` 상자 | `r1` = "맡긴 문맥의 Rip 값. 실제로 가는지는 추측" | `q1` 에 `EAX = 0x43`·"+0x14 로 안 돌아옴", `r1` 에 받는 인자·`(0, rcx, rdx)` CFG 호출·칸 0 갈래·`__except`, `s1` 에 "`+0x2c` 칸 호출의 대상 추정, 칸 값 Q48" |
| 2.3 `KePopulateContinuationContext` | 목적 `[추측]` (`NtContinue` 로 돌아갈 때 검사 맞추기) | 그 `[추측]` **취소선** → **`[정적]` 맞다** (`KiContinuePreviousModeUser +0x399` 가 그 칸과 `CONTEXT.Rip` 를 비교, 특례, 두 비트 조건) |
| 2.3 "01_sprint 관찰 두 개" 끝 | 남은 `[추측]`: 커널 `NtContinue` 가 보내는 부분 | **취소선** + "(run29) 뒷부분도 `[정적]`". **이 스레드는 `IRETQ` 를 두 번 탄다** |
| 2.4 트리 | `+0x18 NtContinue` 에서 끝 | 아래에 `══▶ SYSCALL 0x43 → 커널 NtContinueEx … IRETQ → RtlUserThreadStart ← [정적] run29` 한 줄 |
| 2.4 남은 `[추측]` 줄 | 커널 `NtContinue` 안 `[공백]` | **취소선** + 커널 쪽 `[정적]` 줄 (성공하면 `+0x18` 로 안 돌아옴, 실패할 때만 `RtlRaiseStatus`) |
| **2.10 제목·앞머리** | "`test.exe` — 진입점에서 `main`까지" | "**로더 끝 → `test.exe` 진입점 → `main`**". 코드 사슬 트리 + 비유 한 문단 |
| **2.10 새 절 여섯** | 없음 | `ntdll!NtContinue`, `nt!NtContinue → NtContinueEx`, `nt!KiContinueEx`, `nt!KiContinuePreviousModeUser`, **유저로 돌아가기 — `NtContinueEx` 출구**, `ntdll!RtlUserThreadStart` (모두 `[정적]`) |
| 2.10 `Rcx` 줄 | "이 마지막 단계(커널 `NtContinue` 안)만 `[추측]`" | **취소선** → `[정적]` (`+0x38` 에 쓰고 `+0x3ea` 가 되살림) |
| 2.10 `[추측]` 두 줄 | 판정 없음 | **판정** 셋: `NtContinue` → `RtlUserThreadStart` 는 코드로 맞음 / `RtlUserThreadStart` → `BaseThreadInitThunk` 는 호출 자리만 `[정적]`, 대상은 `[추측]` / `BaseThreadInitThunk` → `mainCRTStartup` 그대로 `[추측]` |
| 2.10 칸 `Kernel32ThreadInitThunkFunction` 줄들 | "`RtlUserThreadStart` 본문은 안 봄 — 정적 Q17" | 그 부분 **취소선** → "run29 `[정적]` `+0x27` 에서 부름". 같은 칸을 `(1,0,0)` / `(0, 진입점, 파라미터)` 로 부르는 두 곳 줄 추가. CFG 스텁 `JMP` 뿐이라 `k` 에서 `+0x17` → `+0x2c` 예상 |
| 3장 (스레드 4개) | — | `NtContinue` 길엔 "첫 스레드인가" 를 보는 갈림이 없다 `[정적]` → 나머지 스레드도 같은 길 `[추측]`. Worker `+0x2c` 의 뜻 → 칸 값 = `BaseThreadInitThunk` 로 읽힘 `[추측]` |
| 4장 공백 표 | 2.4·2.10 "커널 `NtContinue` 안 `[공백]`" | **(해결됨, 정적)**. 2.10 은 "일부 해결". 새 줄 여섯: Q29 실행, Q30 실행, `KiKvaShadow` Q42, 칸 값 Q48, CET·APC Q57, 정적으로 남은 것 넷 |
| 4.1 그림 2 표 | `NtContinue` ==> `RtlUserThreadStart` 는 `[추측]` | **취소선** + 새 줄 둘 (`ntdll ==> nt` 경계, `nt (2)` 묶음) — 각각 디버거로 볼 것 |
| 5장 | run28 까지 | run29 줄, 요청서 번호 표에 run29 줄 |

태그로 보면 핵심 변화는 다섯 가지다:

- **2.10 커널 `NtContinue` 안: `[공백]` → `[정적]`.** 그림 2 의 굵은 "공백" 화살표가 코드 사슬이 됐다.
- **`NtContinue` → `RtlUserThreadStart`: `[추측]` → 코드로는 `[정적]`.** 실행은 아직 (Q29).
- **`KePopulateContinuationContext` 의 목적: `[추측]` → `[정적]`** (짝이 되는 맞춰 보기 줄을 찾음).
- **`RtlUserThreadStart` → 칸 호출: 호출 자리 `[정적]`** (`+0x27`, ret `+0x2c`, 인자 `(0, 진입점, PEB)`). **대상이 `BaseThreadInitThunk` 인지는 `[추측]` 그대로** (Q48). 그래서 그림 화살표도 `?` 그대로다.
- `BaseThreadInitThunk` → `mainCRTStartup`: **변화 없음** `[추측]` (Q18·Q30).

### 큰 그림에서 어떻게 이어지나

- 이번으로 **test.exe 메인 스레드의 길이 `LdrInitializeThunk` 에서 `RtlUserThreadStart` 까지 코드로 끊김 없이 이어졌다.**
  - run16: 커널이 메모를 사물함에 맡기고 오리엔테이션실로 보냄.
  - run17 ~ run28: 오리엔테이션(로더) 안.
  - **run29: 사물함 메모를 다시 출발 메모로 → 본업 자리.**
- 노선도에서 남은 빈칸은 이제 **딱 한 칸 반**이다:
  - `BaseThreadInitThunk` 안에서 진입점을 부르는 칸 (`+0x12`, ret `+0x17`) 의 **뒤쪽** — 정적 **Q18** 이 다음 항목이다.
  - 그 뒤 `mainCRTStartup` → `main` 은 test.exe(CRT) 쪽이다.
- 그다음은 **버스를 타 볼 차례**다. 이번 바퀴가 넘긴 멈춤점들(D1 ~ D8)은 2.14 의 **콜스택 예측**과 함께 들고 가면 한 번에 맞춰 볼 수 있다 (Q29·Q30·Q42·Q48·Q57).
- plan.md 결정 기록: **Q18 까지 끝나면 단계 2 끝 → 멈춤** (사용자에게 알림). 그 뒤 대기열엔 작은 정적이 동적 사이에 섞여 있다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `BaseThreadInitThunk +0x12` (ret `+0x17`) 간접 호출의 대상이 정말 `test!mainCRTStartup` 인가, 그 칸 안쪽 | kernel32 쪽은 이번 범위 밖 (`BaseThreadInitThunk` 는 CFG 중개 모양만 확인) | 정적 **Q18** (다음 항목), 동적 **Q30** |
| 실제로 이 길을 타나: `nt!NtContinue` 입구의 `rcx`(사물함 메모)·`dl` = 1, 메모의 `Rip`·`Rcx`·`Rdx`, 들어올 때 출발 메모 `Rip` = `ntdll!NtContinue+0x14` 인가 | 정적은 "코드에 이렇게 쓰여 있다" 까지. 처리기 본문도 안 읽었다 | 동적 **Q29** (D1) — `nt!NtContinue` (`nt+0x6adde0`) |
| 갈아 끼운 직후 출발 메모 값, 복사본 `ContextFlags` 가 `0x10001b` 인가 | 실행 때 값 | 동적 **Q29** (D2) — `nt+0x2bcb72` / `nt+0x2bcb77` (`KiContinuePreviousModeUser +0x422` 앞뒤) |
| 어느 문으로 나가나 (`+0x426 IRETQ` / `+0x40c KiKernelExit`) | `KiKvaShadow` 는 실행 때 값. run15 출구와 같은 변수 | 동적 **Q29** (D3) · **Q42** — `nt+0x6ae1f3` (`+0x403`), `nt+0x6ae216` / `nt+0x6ae1fc` |
| `RtlUserThreadStart` 입구의 `rcx` = 진입점, `rdx` = PEB, 입구 콜스택 모양 | 실행 때 값 | 동적 **Q29** (D4) — `ntdll+0xacac0`, `ln @rcx` |
| 칸 `Kernel32ThreadInitThunkFunction` 값이 정말 `KERNEL32!BaseThreadInitThunk` 인가 | 칸에 든 **값**은 실행 때 정해진다 | 동적 **Q48** (D7) — `ntdll+0xacae7` (`+0x27`) 에서 `ln @rax` |
| `main` 에서 콜스택이 예측(2.14)대로인가, 교환원 프레임이 정말 없나 | 실행 때 | 동적 **Q30** (D8) |
| 이 VM 에서 CET 비트 둘이 켜졌나, 켜졌다면 `Rip` 검사 결과(0 / `c000060a`)와 그림자 스택 칸이 `RtlUserThreadStart` 인가 | 스레드·프로세스 비트는 실행 때 값 | 동적 **Q57** (D5·D6) — `nt+0x2bc9f0` (`+0x2a0`), `nt+0x2bcae9` / `nt+0x2bcaee` |
| 시동 때 배달될 유저 APC 가 있나, `KiInitiateUserApc` 가 걸리나 | 큐에 무엇이 있는지는 실행 때 `[추측: 보통 없음]` | 동적 **Q57** — `nt+0x6adf38` (`NtContinueEx +0x148`) |
| 맡긴 문맥 `ContextFlags` = `0x10001b` 가정이 맞나 (`RtlInitializeExtendedContext2` 가 값을 그대로 쓰나) | 그 함수 본문을 안 읽었다 `[추측]` | 정적 **Q58** (+ 동적 Q29 D2) |
| CET 검사가 올린 새 그림자 스택 포인터를 누가 유저 쪽에 되돌려 쓰나 | `KiContinuePreviousModeUser` 안엔 그 줄이 없다 `[공백]` | 정적 **Q58** |
| 배달할 APC 가 있을 때 원래 문맥을 어디 보관하나 (`KiInitiateUserApc` 본문) | callee 본문은 안 읽는 범위 `[추측]` | 정적 **Q58** |
| `RtlUserThreadStart` 의 `RtlLeaveCriticalSection` 특례가 왜 있나 | 코드엔 갈림만 있고 이유는 없다 `[추측]` | 정적 **Q58** |
| `KiKernelExit` 를 `NtContinueEx` 가 부를 때도 run15 처럼 `+0xa5 IRETQ` 로 나가나 | 출구 본문은 범위 밖. 같은 입구로 꼬리 점프하니 같을 것 `[추측]` | run 문서 "아직 확인 필요" (번호 없음). 동적으로는 Q29 (D3) 에서 같이 보임 |
| 나머지 스레드(Worker 등)도 같은 길로 `RtlUserThreadStart(시작 주소, 파라미터)` 에 가나 | 갈림에 "첫 스레드" 조건이 없다는 것까지만 `[정적]`, 같은 길이라는 건 `[추측]` | 동적 Q29 를 다른 스레드에도 걸면 확인 (flow.md 3장) |

---

## 5. 더 읽기

- run 문서: [`run29-continue-start.md`](run29-continue-start.md) — 호출 지점 표 전체, 둘째 인자의 두 얼굴, 유저 길 검사 순서 ① ~ ⑫, 옮기는 칸 표, CET 맞춰 보기, TestAlert, 유저로 돌아가는 법, `RtlUserThreadStart`, 서비스 표, `[추측]` 두 줄 판정, 동적 확인 목록
- 정적 결과서: [`_workspace/sast/24_continue-start_result.md`](../../_workspace/sast/24_continue-start_result.md) — 질문 1 ~ 6 답, "사슬 하나" (`LdrInitializeThunk +0x18` 에서 `BaseThreadInitThunk` 까지 `Rip`·`Rcx`·`Rdx` 가 옮겨 가는 그림), 그림 2 반영 제안, D1 ~ D8 표, 스크립트 문제 넷
- 요청서: [`_workspace/sast/24_continue-start.md`](../../_workspace/sast/24_continue-start.md)
- flow.md: [`flow.md`](flow.md)
  - "두 그림 사이 설명" 의 run29 문단
  - 그림 2 `nt` 상자 안 새 묶음 `(2) 로더 끝 NtContinue`, `PH4` 묶음 (`q1`·`r1`), `s1`
  - **2.10** 전체 (코드 사슬 트리, 새 절 여섯, 판정)
  - 2.3 `PspInitializeThunkContext` 7번 줄, "01_sprint 관찰 두 개" 끝, 2.4 트리와 남은 `[추측]` 줄
  - 3장 (스레드 4개, Worker 콜스택), 4장 공백 표 run29 줄들, 4.1 그림 2 표
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 앞 해설:
  - [run16 해설](run16-thunk-context-해설.md) — 2.3 이야기 표 (8번 줄이 이번 바퀴), 2.6 바꿔 끼우기·CET 곁가지, 2.7 `NtContinue` 호출, 2.8 "부른 곳 없음" 수수께끼
  - [run15 해설](run15-user-thread-startup-해설.md) — 2.4 휘발/비휘발, 2.6 트랩 프레임·`IRETQ`·`KiKvaShadow` 두 문 (이번 출구와 같은 모양)
  - [run25 해설](run25-dll-entry-해설.md) — 2.6 `BaseThreadInitThunk` 두 갈래, `+0x17`·`+0x20`
  - [run28 해설](run28-tls-init-call-해설.md) — 2.9 교환원(CFG 중개)은 프레임을 안 남긴다, 2.10 필터
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q17 (끝), Q18 (다음), Q29, Q30, Q42, Q48, Q57, Q58, 결정 기록 2026-10-23 "Q17 끝"
