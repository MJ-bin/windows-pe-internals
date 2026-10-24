# run42 continue-detail — 해설

> 이 글은 [run42 문서](run42-continue-detail.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q58** — 2.10 `NtContinue` 길 작은 정적 넷: 맡긴 문맥 `ContextFlags` = `0x10001b` 가정, 새 그림자 스택 포인터를 누가 유저 SSP 로 쓰나, `KiInitiateUserApc` 본문, `RtlLeaveCriticalSection` 특례 이유 (`_workspace/plan.md`, 이번에 **끝**)
  - 넷째(`RtlLeaveCriticalSection` 특례)는 이미 run41 로 답한 것으로 보고 요청에서 뺐다. 이번에 푼 건 **셋**이다.
  - 실행 때 확인할 거리는 새 번호 없이 **Q29** (`ContextFlags` 실제 값) 와 **Q57** (CET·SSP·유저 APC) 에 출처로 붙었다.
  - 새 정적 공백 셋은 **Q75** (a)·(b)·(c) 로 맨 아래에 붙었다.
  - 다음 항목: **Q59** — kernel32 `BasepInitializeTermsrvFpns` 안쪽 (정적).
- 근거:
  - run 문서 [`run42-continue-detail.md`](run42-continue-detail.md) (가장 중요)
  - 요청서 [`_workspace/sast/37_continue-detail.md`](../../_workspace/sast/37_continue-detail.md)
  - 결과서 [`_workspace/sast/37_continue-detail_result.md`](../../_workspace/sast/37_continue-detail_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntoskrnl 하나만 봤다. pdb GUID 는 flow.md 기준과 같다 (`C29EBFB06B78B3C020DCA66D99713F9E1`).
  - 요청서 대상 6개는 **이미 뽑혀 있던 파일**을 그대로 읽었다.
  - 답을 닫으려고 6개를 더 뽑았다: `RtlCopyContext`, `RtlpCopyLegacyContext`, `RtlpGetContextFlagsLocation`, `KiInitializeUserApc`, `KeVerifyContextIpForUserCet`, `KeVerifyContextRecord`.
  - "유저 SSP 를 쓰는 명령이 어디 있나" 는 커널 파일 전체를 **바이트로 훑어서** 찾았다 (하네스 밖 임시 파이썬). 그래서 **"찾은 꼴 기준"** 이다 (2.5 에서 자세히).

이 글은 [run29 해설](run29-continue-start-해설.md) 의 **곁가지 정리판**이다.

- run29 는 로더가 끝난 뒤 `NtContinue` 로 본업 자리(`RtlUserThreadStart`)에 가는 큰 길을 봤다.
- 그때 "작은 거라 나중에" 하고 남긴 질문이 이번 바퀴다. **큰 길(지도의 굵은 화살표)은 안 바뀐다.** 짐작 셋을 코드로 닫은 바퀴다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절
- CONTEXT (**업무 시작 메모**) → [run11 해설 2.6](run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다)
- APC (**쪽지**), IRQL (**방해 금지 표지판**) → [run13 해설 2.3](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- 트랩 프레임 (**출발 메모**), 예외 프레임 (비휘발 레지스터를 넣어 두는 **개인 서랍**), `IRETQ` (**순간 이동**) → [run15 해설 2.6](run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 사물함 메모·오리엔테이션실·본업 자리, 바꿔 끼우기 → [run16 해설 2.6](run16-thunk-context-해설.md#26-장면--바꿔-끼우기--pspinitializethunkcontext)
- 벽 주소록 `PsNtdllExports` → [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- `ContextFlags` (**메모 맨 위 체크박스**), 깎기 → [run29 해설 2.7](run29-continue-start-해설.md#27-갈아-끼우기-전--복사하고-검사하고-깎는다)
- 출발 메모 덮어쓰기, CONTROL·INTEGER 체크박스 → [run29 해설 2.8](run29-continue-start-해설.md#28-출발-메모-덮어쓰기--어느-칸이-바뀌나)
- CET 그림자 스택 (**경비실 장부**), 예약 줄, 맞춰 보기 → [run29 해설 2.9](run29-continue-start-해설.md#29-cet-그림자-스택--미리-올려-둔-한-칸과-맞춰-보기)
- TestAlert, 나가는 문의 쪽지 확인 고리 → [run29 해설 2.10](run29-continue-start-해설.md#210-testalert--쪽지를-배달-대상-으로-올리기만-한다)

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- CONTEXT = **업무 시작 메모**, `ContextFlags` = 그 메모 맨 위 **체크박스**
- 트랩 프레임 = 직원 책상에 붙은 **출발 메모**, 유저 스택 = 직원 **수첩**
- 유저 스택에 맡긴 원래 메모 = **사물함 메모** (run16)
- CET 그림자 스택 = **돌아갈 주소를 몰래 한 벌 더 적어 두는 보안 장부** (= run29 의 **경비실 장부**. 직원 수첩은 고쳐 쓸 수 있어도 이 장부는 아무나 못 고친다)
- 유저 APC = **스레드에 끼워 넣는 "잠깐 이것부터 해" 쪽지** (= run13·run29 의 **쪽지**)
- **이번에 더하는 비유**:
  - 유저 그림자 스택 포인터(SSP)를 담는 CPU 칸 MSR `0x6a7` = 경비실 장부에 꽂힌 **책갈피** ("지금 맨 위 줄은 여기")
  - 업무 시작 메모의 확장 칸(XState) = 메모 뒤에 붙는 **별지**. 그 안 CET_U 칸 = 별지의 **"책갈피 위치" 칸**
- run 문서는 `ContextFlags` 를 "택배 상자 체크리스트", APC 를 "택배 기사의 메모지" 로 비유했다. 이 글은 앞 해설들과 맞추려고 **상자 → 업무 시작 메모, 택배 → 쪽지** 로만 바꿔 부른다. 그림은 같다.

### 한 줄로 먼저 — 짐작 셋, 어떻게 닫혔나

| | run29 의 짐작 | 이번 답 | 결정적인 줄 |
|---|---|---|---|
| 질문 1 | 사물함 메모 체크박스 = `0x10001b` 로 보인다 `[추측: RtlInitializeExtendedContext2 가 그대로 쓴다고 가정]` | **맞다** `[정적]`. 단 CET 스레드면 **`0x10005b`** | `RtlInitializeExtendedContext2 +0xf5 MOV [RCX+0x30],EDI` |
| 질문 2 | 검사가 올린 새 SSP 를 누가 쓰나 `[공백]` | **아무도 안 쓴다** `[정적]`. 유저 SSP 를 바꿀 수 있는 곳은 `XRSTORS` 하나 | `KiVerifyContextIpForUserCet +0x12c` 로 돌려준 칸을 `+0x39e` 뒤로 아무도 안 읽음 / `KxContextToKframes +0x211 XRSTORS` |
| 질문 3 | `KiInitiateUserApc` 가 APC 진입을 만들고, 원래 문맥은 APC 틀에 보관될 것 `[추측]` | 방향은 맞다. 다만 `KiInitiateUserApc` 는 **심부름만** 하고, **보관은 `KiInitializeUserApc`** 가 **유저 스택 `CONTEXT`** 에 한다 `[정적]` | `KiInitiateUserApc +0x6b` (ret `+0x70`) / `KiInitializeUserApc +0x1fb` (ret `+0x200`) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 질문은 길 위의 **두 곳**에 걸쳐 있다. 사물함 메모를 **만드는 곳**(2.3)과, 그 메모를 **꺼내 쓰는 곳**(2.10)이다.

```
cmd.exe 의 부탁 (CreateProcess)
 └─ 커널: 프로세스·스레드 만들기 (2.1 ~ 2.2)
     └─ 커널: test.exe 첫 스레드 출발 (2.3)
          PspInitializeThunkContext — 사물함 메모 만들기, 체크박스 값 정하기      ← 질문 1
          (CET 면) 경비실 장부에 예약 줄 RtlUserThreadStart 한 줄
     └─ ntdll: 로더 (오리엔테이션실, 2.4 ~ 2.9)
     └─ 로더 끝: NtContinue(사물함 메모) (2.10)
          커널: 메모 검사 → 장부 맞춰 보기 → 출발 메모 갈아 끼우기               ← 질문 2
          커널: 나가는 문의 쪽지 확인 고리 → KiInitiateUserApc                    ← 질문 3
          IRETQ → RtlUserThreadStart (본업 자리)
     └─ BaseThreadInitThunk → (CRT) → main
```

### 무엇이 궁금했나

1. 사물함 메모 맨 위 체크박스(`ContextFlags`)는 **정말** `0x10001b` 인가? 메모 틀을 만드는 함수가 값을 몰래 고치지는 않나?
2. 경비가 장부 맨 위 줄을 맞춰 본 뒤 "다음 줄은 여기(+8)" 라고 적은 새 SSP 는 **누가 책갈피(MSR `0x6a7`)에 꽂나?**
3. 나가는 문에서 "잠깐 이것부터 해" 쪽지가 있으면, 직원이 원래 하던 일(원래 문맥)은 **어디에 적어 두나?**

### 바퀴 전에 알던 것

- `[정적]` (run16·run29) `PspInitializeThunkContext +0x8d ADD R12D,0x10001b`. CET 스레드면 `0x40` 을 더한다.
- `[추측]` (run29) 이 값이 그대로 사물함 메모 체크박스가 된다 — "`RtlInitializeExtendedContext2` 가 그대로 쓴다" 는 가정에 기댐.
- `[정적]` (run29) `KiVerifyContextIpForUserCet` 는 장부 맨 위 줄과 메모의 `Rip` 를 맞춰 보고, 맞으면 `+0x128 ADD RAX,8` 로 포인터를 옮겨 `[R15]` 에 쓴다.
- `[공백]` (run29) 그 새 포인터를 다시 쓰는 줄을 `KiContinuePreviousModeUser` 안에서 못 찾았다.
- `[정적]` (run29) `NtContinueEx +0x148 TEST byte [thread+0xc2],3` → `+0x15b` (ret `+0x160`) `KiInitiateUserApc`.
- `[추측]` (run29) 원래 문맥은 APC 틀에 보관될 것 — `KiInitiateUserApc` 본문을 안 읽었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 메모를 만들 때와 꺼낼 때

run 문서 "호출 관계" 를 줄인 것이다. 숫자는 콜스택에 찍히는 **ret 오프셋**이다.

```
PspInitializeThunkContext                                  (2.3, 출발 때 한 번)
 ├─ +0x8d    R12D = 0x10001b  (+0x40 if CET)                 체크박스 값 정하기
 ├─ +0x26e   RtlInitializeExtendedContext2(사물함 R15, R12D)   +0xf5 받은 값 그대로 적음
 ├─ +0x285   RtlCopyContext(R15, R12D, R13)                    체크박스 그대로 남음
 └─ +0x29a   KePopulateContinuationContext                     CET: 장부에 예약 줄
                     ...  (로더)  ...
NtContinueEx
 ├─ +0x86  KiContinueEx ─ +0xf0 KiContinuePreviousModeUser
 │           ├─ +0x31a RDMSR 0x6a7 → [RBP+0x8]              (CET 칸 있을 때)
 │           ├─ +0x39e KiVerifyContextIpForUserCet(…, &[RBP+0x8])
 │           │           └─ +0x12c [RBP+0x8] = SSP + 8      ← 이후 아무도 안 읽음
 │           └─ +0x427 KeContextToKframes ─ +0x7d KxContextToKframes
 │                        └─ +0x211 XRSTORS                 (XState 있을 때만)
 ├─ +0x160 KiInitiateUserApc                                (유저 APC 대기일 때만)
 │           └─ +0x70 KiDeliverApc
 │                     └─ +0x5f3 KiInitializeUserApc
 │                                ├─ +0x200 KeContextFromKframes   원래 문맥 → 유저 스택 CONTEXT
 │                                └─ +0x283/+0x28a  트랩 Rsp = 그 자리, Rip = KiUserApcDispatcher
 └─ IRETQ / KiKernelExit
```

- 모두 `[정적]` 이다. 실제로 이 순서로 도는지는 아직 아무도 안 봤다 (동적 Q29·Q57).
- `+0x211 XRSTORS` 가 유저 SSP 를 되돌린다는 부분만 `[추측: CPU 설명서 지식]` 이다 (2.5).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 사물함 메모 틀을 만드는 줄

`nt!PspInitializeThunkContext +0x269` (ret `+0x26e`) → `RtlInitializeExtendedContext2(R15, R12D, …)` `[정적]`

- **누가**: `PspInitializeThunkContext`. 첫 스레드가 출발할 때 출발 메모를 오리엔테이션실(`LdrInitializeThunk`)로 바꿔 끼우는 함수 (run16).
- **어디서**: `+0x269` 에서 부른다. 콜스택에는 돌아올 자리 `+0x26e` 로 찍힌다. 바로 앞 `+0x263 MOV EDX,R12D` 가 둘째 인자를 넣는다.
- **누구를**: `RtlInitializeExtendedContext2`. 빈 업무 시작 메모(CONTEXT) **틀**을 만들어 주는 함수.
- **무엇을 위해**: **유저 스택 위(`R15`)** 에 사물함 메모 틀을 만들려고. 둘째 인자 `R12D` 가 "체크박스에 무엇을 켤까" 다.
- 눈여겨볼 것: 같은 함수를 `+0xf0` (ret `+0xf5`) 에서도 한 번 부른다. 그건 **커널 스택 위 임시 메모**(`R13`) 다. 둘 다 같은 `R12D` 를 받는다 (2.3).

#### 한 줄 읽기 ② — 호출이 아니라 "쓰기" 줄

`nt!KiVerifyContextIpForUserCet +0x12c MOV [R15],RAX` `[정적]`

- **누가**: `KiVerifyContextIpForUserCet`. 경비실 장부 맨 위 줄과 메모의 `Rip` 를 맞춰 보는 경비 (run29).
- **어디서**: `+0x12c`. 바로 앞 `+0x128 ADD RAX,8` 이 포인터에 8 을 더했다.
- **누구에게**: `[R15]`. `R15` 는 이 함수의 **5번째 인자** 다 (`+0x69 MOV R15,[RSP+0x90]`). 부른 쪽이 "결과는 여기 적어 줘" 하고 건넨 칸의 주소다.
- **무엇을 위해**: "맞춰 봤더니 맞다. 다음 줄은 여기(+8)" 를 부른 쪽에 **돌려주려고**.
- 눈여겨볼 것: 이 함수가 `[R15]` 에 쓰는 줄은 **이것 하나뿐**이다. 책갈피(MSR)에 쓰는 줄은 없다 (`0x6a7` 은 `+0x13a` 의 **읽기**(`RDMSR`) 뿐). 그러니 "책갈피에 꽂느냐" 는 **부른 쪽이 이 칸을 어떻게 쓰느냐** 에 달렸다 → 2.4.

#### 한 줄 읽기 ③ — 원래 문맥을 수첩에 베끼는 줄

`nt!KiInitializeUserApc +0x1fb` (ret `+0x200`) → `KeContextFromKframes(트랩, 예외, RDI)` `[정적]`

- **누가**: `KiInitializeUserApc`. 유저 APC 쪽지를 실제로 직원에게 건넬 준비를 하는 함수. `KiDeliverApc +0x5ee` (ret `+0x5f3`) 가 부른다.
- **어디서**: `+0x1fb`. 바로 앞 `+0x1ec` 에서 `[RDI+0x30]` (= 그 메모의 체크박스 칸) 에 플래그를 적었다.
- **누구를**: `KeContextFromKframes`. 이름 그대로 **출발 메모·개인 서랍(트랩·예외 프레임) → 업무 시작 메모(CONTEXT)** 방향으로 옮겨 적는 함수. run29 에서 본 `KeContextToKframes`(메모 → 출발 메모)의 **반대 방향**이다.
- **무엇을 위해**: 직원이 원래 하던 일을 **수첩(유저 스택) 위 `RDI` 자리**에 업무 시작 메모 꼴로 베껴 두려고. 이게 질문 3 의 답 "어디에 보관하나" 다 → 2.7.

### 2.3 핵심 ① 체크박스는 정말 `0x10001b` — CET 스레드면 `0x10005b`

#### 체크박스 풀이 `[정적]`

| 스레드 | 사물함 메모 `ContextFlags` | 풀이 | XState 마스크 (`R14`) |
|---|---|---|---|
| CET 아님 | **`0x10001b`** | `0x100000`(AMD64) + `0x1`(제어) + `0x2`(정수) + `0x8`(부동소수) + `0x10`(디버그 레지스터). 세그먼트 `0x4`·XState `0x40` 없음 | 0 |
| CET (`[thread+0x74]` bit 20) | **`0x10005b`** | 위 + `0x40` (XState) | `0x800` (CET_U 칸 하나) |

- 비트 이름은 공개 헤더 값이다 (run 문서). `0x40` = XState 인 건 이 바이너리의 `RtlpValidateContextFlags2` 가 `0x100040` 을 XState 로 다루는 것으로도 맞다.
- 새 말 — **XState** = 업무 시작 메모 **뒤에 덧붙는 확장 칸**. 부동소수 같은 기본 칸 말고, CPU 의 덧붙은 기능 상태를 담는 자리다. 체크박스 `0x40` 이 "확장 칸도 있음" 표시다.
  - 비유: 메모 뒤에 스테이플러로 찍은 **별지**. 체크박스 "☑ 별지 있음" 이 `0x40`.
- 새 말 — **CET_U 칸** = 그 별지 안에서 **유저 그림자 스택** 상태를 담는 칸. 기능 번호 `0xb` (= 11, 마스크 `0x800`) 다. 모양은 `[0] == 1`, `[8]` = SSP (2.5 에서 다시).
  - 비유: 별지의 **"책갈피 위치" 칸**.

#### 값이 정해지는 순서 — 단계별로 `[정적]`

1. `PspInitializeThunkContext +0x6c AND ECX,0x100000` — 스레드 `+0x74` 의 CET 칸을 본다.
2. 켜졌으면 `0x40` 을 만든다. 그리고 **`+0x8d ADD R12D,0x10001b`**. 그래서 `R12D` = `0x10001b` 또는 `0x10005b`.
   - 같은 곳(`+0x72~+0x79`)에서 XState 마스크 `R14` 도 정한다: CET 면 `0x800`, 아니면 0.
3. 이 `R12D` 를 **메모 둘**이 똑같이 받는다:
   - 임시 메모 (커널 스택, `R13`): `+0xf0` (ret `+0xf5`). 여기에 `PspGetContextThreadInternal` (`+0x168`) 로 지금 스레드 상태를 받는다.
   - **사물함 메모 (유저 스택, `R15`)**: `+0x263 MOV EDX,R12D` → `+0x269` (ret `+0x26e`).
   - 중간의 알림 루프가 `R12` 를 카운터로 써도, `+0x153` 에서 `[RBP+0x8]` 로 다시 읽어 와서 같은 값이다.

#### 틀 만드는 함수는 값을 고치나? — 아니다 `[정적]`

- `RtlInitializeExtendedContext2` 가 체크박스 칸에 쓰는 줄은 **`+0xf5 MOV [RCX+0x30],EDI`** 하나다.
  - `EDI` = 받은 `ContextFlags`, `RCX` = 16바이트에 맞춘 메모 시작.
  - 이 칸을 다시 쓰는 줄은 없다. `EDI` 에 `OR`/`AND` 하는 줄도 없다.
- 하는 일은 **검사뿐**: `+0x3c` `RtlpValidateContextFlags2` → 음수면 그대로 반환 (`+0x43 JS`).
  - 그 검사 함수는 값을 **읽기만** 한다. `0x10001b`·`0x10005b` 둘 다 통과하는 모양이다 (`& 0x7ffff20 == 0x100000`, `+0x1e~+0x23`).
- 헷갈리기 쉬운 곳 — `0x40` 이 켜지면 이 함수가 뭔가 **더** 하긴 한다:
  - `KUSER_SHARED_DATA` 의 `EnabledFeatures` 가 0 이면 `0xc00000bb` 로 거절 (`+0x90~+0xb2`).
  - 아니면 `CONTEXT_EX` 뒤에 XState **칸을 만든다** (`+0x9a~+0xa7`, `+0x104~+0x1a8`).
  - XState 마스크(4번째 인자, 여기선 `0x800`)는 켜진 기능으로 **깎인다** (`+0x12d~+0x14f`, ret `+0x154` `RtlpRemoveArchDisallowedXStateFeatures`).
  - 그런데 이건 **별지를 붙이는 일**과 **마스크** 얘기다. **체크박스 자체는 안 건드린다.**
- 비유: 메모 틀을 찍어 주는 직원은 체크박스를 **받은 그대로** 옮겨 적는다. "☑ 별지 있음" 이면 별지를 **붙여 줄** 뿐, 체크박스에 표시를 더하거나 지우지 않는다.

#### 그 뒤에 누가 바꾸나? — 아무도 `[정적]`

- `PspInitializeThunkContext` 안에서 `+0x30` 에 쓰는 줄은 `+0x38a MOV [RBP+0x30],EBX` 하나. 이건 **오류 갈래의 예외 레코드 칸**이라 사물함 메모와 상관없다.
- `RtlCopyContext` (`+0x280`, 임시 메모 → 사물함 메모 복사) 는 체크박스를 **건드리긴 한다**. 그래도 **결과는 같다**. 단계별로:
  1. `RtlpCopyLegacyContext +0x22` 가 대상 체크박스에 `(원본 & 요청) & 0x67ffffff` 를 쓴다.
  2. 돌아와서 `RtlCopyContext +0xc7 OR [대상 플래그],R12D` 로 대상이 **원래 갖던 값을 다시 합친다**.
  3. 대상(사물함 메모)은 원래 `0x10001b`/`0x10005b` 이고, 1번에서 들어간 값은 그 **부분집합**이다.
  4. 그래서 2번이 끝나면 **원래 값 그대로** 남는다.
  - 비유: 복사기가 체크박스를 "원본에도 있고 요청에도 있는 것" 만 남겨 한 번 적고, 그다음 원래 있던 체크를 **덧칠**한다. 덧칠할 체크가 원래 전부라서, 결과는 처음과 같다.

#### 헷갈리지 말 것 — 메모가 둘이다 (run16 에서 이미 정리)

| | 어디 | 체크박스 | 누가 쓰나 |
|---|---|---|---|
| (가) **사물함 메모** | 유저 스택 `R15` | **`0x10001b`** / CET `0x10005b` | `LdrInitializeThunk` 가 `Rcx` 로 받아 나중에 `NtContinue` 에 넘김 |
| (나) **작은 메모** | `[RBP+0xd0]` | **`0x10000b`** (`+0x1a8 MOV [RBP+0x100],0x10000b`, 디버그 레지스터·XState 없음) | `PspSetContextThreadInternal` (`+0x2d6`) 로 스레드를 오리엔테이션실로 보낼 때 |

- 질문은 (가) 였다. (나) 는 run16 표 그대로다.

#### 그래서 run29 의 "넷 다 옮겨진다" 가 가정 없이 섰다 `[정적]`

- run29 에서 본 대로, 출발 메모로 `Rip`·`Rsp` 가 가려면 CONTROL(`0x100001`), `Rcx`·`Rdx` 가 가려면 INTEGER(`0x100002`) 가 켜져 있어야 한다.
- 사물함 메모 체크박스는 CET 든 아니든 `0x1`·`0x2` 가 다 켜져 있다.
- 그러니 `KxContextToKframes` 가 넷 다 옮긴다. run29 에서 `[추측]` 에 기대던 고리가 이제 `[정적]` 이다.

#### 겹치는 증거 — 쪽지 쪽도 같은 값을 쓴다

- `[정적]` 유저 APC 가 문맥을 유저 스택에 맡기는 `KiInitializeUserApc` 도 같은 값을 쓴다:
  - `+0x81 MOV R15D,0x10001b`
  - CET 면 `+0x32c OR R15D,R12D` (`R12D` = `0x100040`, `+0xb3`) 또는 `+0x374 MOV R15D,0x10005b`
  - 그리고 `+0x1ec [RDI+0x30] = R15D`
- `[추측]` (run 문서) "유저 스택에 맡기는 문맥엔 `0x10001b` (+CET 면 `0x40`)" 가 이 커널의 관례로 보인다. 두 곳에서 같은 값을 쓰는 걸 봤을 뿐, 규칙으로 적힌 건 없어서 `[추측]` 이다.

#### 덧붙임 — `NtContinue` 가 받을 때

- `[정적]` `KiContinuePreviousModeUser +0x58` 이 유저 메모리의 `[문맥+0x30]` 을 `EDI` 로 읽는다.
- 값이 틀리면 `+0x459 AND EDI,0xf810001f` / `BTS EDI,0x14` 로 고쳐 쓰는 갈래가 있다 (`+0xe2~+0xeb`, `+0x453`). run29 해설 2.7 (나) 의 **깎기** 다.
- `[추측]` 시동 사물함 메모는 멀쩡하니 이 갈래는 안 탈 것이다.

### 2.4 핵심 ② 올린 새 SSP 는 그냥 버려진다 — 지역 칸의 일생

사용자가 헷갈리기 쉬운 곳이라 천천히 간다.

#### 먼저 장면 다시 보기 (run29)

1. 출발 때 `PspInitializeThunkContext +0x295` `KePopulateContinuationContext` 가 (CET 스레드면) 경비실 장부 맨 위에 **예약 줄 `RtlUserThreadStart`** 를 적는다. 그 꼴은 `RDMSR` → −8 → `WRUSSQ` → `WRMSR` 다 (아래 표 첫 줄).
2. 로더가 끝나고 `NtContinue` 가 오면, 경비(`KiVerifyContextIpForUserCet`)가 장부 맨 위 줄과 메모의 `Rip` 를 맞춰 본다.
3. 맞으면 포인터에 **+8** 을 해서 돌려준다 (2.2 ②).

- 새 말 — **MSR** = CPU 안에 있는 번호 붙은 특수 칸. `RDMSR` 로 읽고 `WRMSR` 로 쓴다. 몇 번 칸인지는 `ECX` 로 정한다 `[추측: 일반적으로 알려진 설명]`. 결과서는 유저 그림자 스택 포인터를 `0x6a7` 번 칸으로 읽고 쓴다고 적었다 `[정적]`.
  - 비유: 경비실 장부에 꽂힌 **책갈피**. CPU 는 이 책갈피를 보고 "장부 맨 위 줄이 어디인지" 안다.
- 새 말 — **`WRUSSQ`** = 그림자 스택에 한 칸(8바이트)을 직접 적는 명령 `[추측: 일반적으로 알려진 설명]`. 보통 명령으로는 그림자 스택에 못 쓴다.
  - 비유: 장부에 줄을 적는 **경비 전용 펜**.
- 숫자 방향 하나 — 주소 **−8** 은 장부 위에 줄을 **하나 더 적은** 자리, **+8** 은 맨 위 줄을 **하나 넘긴(지운)** 자리다.
  - run 문서는 "올림" 이라는 말을 두 방향으로 다 쓴다 (질문 2 의 "한 칸 올린 새 SSP" = +8, 표의 `KePopulateContinuationContext` "칸 하나 올림" = −8). 이 글은 헷갈리지 않게 **+8 / −8** 로만 쓴다.

질문은 3번의 **"+8 이 된 새 값"** 이 책갈피에 꽂히느냐다.

#### 단계 1 — 그 값은 어디로 돌려주나 `[정적]`

- `KiVerifyContextIpForUserCet` 의 `R15` = 5번째 인자 (`+0x69`). 처음 값을 `[R15]` 에서 읽고 (`+0x71`), 맞으면 `+0x128 ADD RAX,8` → `+0x12c MOV [R15],RAX`.
- 부른 쪽 `KiContinuePreviousModeUser` 는 그 5번째 인자로 **자기 지역 칸 `[RBP+0x8]` 의 주소**를 넘겼다: `+0x382 LEA RAX,[RBP+0x8]` → `[RSP+0x20]` → `+0x399` call.
- 새 말 — **지역 칸** = 함수가 자기 커널 스택에 잠깐 잡아 둔 칸. 함수가 끝나면 사라진다.
  - 비유: 경비가 손에 든 **메모지 한 장**.
- 그러니 +8 은 **경비의 메모지 `[RBP+0x8]`** 에 적힌다. 책갈피(MSR)가 아니다.

#### 단계 2 — 그 메모지는 어디서 쓰이나 (전부) `[정적]`

`KiContinuePreviousModeUser` 의 `.asm` 전체에서 `[RBP+0x8]` 이 나오는 곳을 다 모은 것이다.

| 자리 | 하는 일 |
|---|---|
| `+0x47` | 0 으로 시작 |
| `+0x1a0` | 문맥 틀 출력 칸으로 `LEA` |
| `+0x1c3` | 읽음 |
| `+0x1d6` | 0 으로 되돌림 |
| **`+0x31a RDMSR` → `+0x323 MOV [RBP+0x8],RAX`** | 현재 SSP (책갈피 값)를 메모지에 적음. XState 에 CET 칸이 있을 때만 (`+0x315~+0x323`) |
| `+0x382` | 위의 `LEA` — 경비에게 메모지 건넴 |
| `+0x39e` 뒤 | **읽는 줄 없음** |

- `+0x39e` 뒤에는 결과 비교만 있다. 성공이면 `+0x422 KeContextToKframes`, 실패(`0xc000060a`)면 `+0x3ad` 로그.
- `KeContextToKframes` (`+0x422`) 의 인자는 `(트랩, 예외, 커널 복사본, 플래그, 1)` 이다. **SSP 를 받는 칸이 없다.**
- 새 말 — **죽은 값** = 써 놓고 그 뒤로 아무도 안 읽는 값. run 문서 표현이다.
- 그래서 결론 `[정적]`: **+8 은 `KiContinuePreviousModeUser` 지역 칸에서 사라지고 MSR 로 안 간다.**
- 비유: 경비가 "다음 줄은 여기" 를 메모지에 적어 건넸는데, 받은 사람이 그 메모를 **안 보고 버린다.** 책갈피는 그대로다.

#### 단계 3 — 혹시 다른 곳에서 책갈피를 꽂나? — 커널 전체를 훑었다 `[정적]`

메모지를 안 읽어도, 누가 **같은 값을 따로 계산해서** 책갈피에 꽂을 수는 있다. 그래서 `WRMSR 0x6a7` 을 하는 곳을 커널 파일 전체에서 찾았다.

- 새 말 — **바이트 검색** = 디컴파일 결과가 아니라 `ntoskrnl.exe` 코드 영역의 **원래 바이트**를 처음부터 끝까지 훑는 것. 함수 경계를 몰라도 다 볼 수 있다.
- 찾은 꼴 (run 문서 그대로):

  ```
  B9 A7 06 00 00   = MOV ECX,0x6a7    뒤 40바이트 안에 WRMSR
  ```

- **한계**: 이 꼴만 잡는다. `ECX` 를 다른 식으로 채우는 `WRMSR`, 그리고 `XRSTORS`/`XSAVES` 로 MSR 을 건드리는 건 안 잡힌다. 그래서 `NtContinue` 길 함수의 `.asm` 을 **따로 읽은 것**이 둘째 근거다.

찾은 곳 (run 문서 표를 줄임):

| `WRMSR 0x6a7` 이 있는 곳 | 하는 일 | `NtContinue` 길? |
|---|---|---|
| `KePopulateContinuationContext +0x17~+0x36` (`nt+0x3ecac7`) | `RDMSR` → −8 → `WRUSSQ` → `WRMSR` | 아니오 (출발 때 `+0x295`, APC 때 `KiInitializeUserApc +0x21d`) |
| `RtlPrepareEnclaveCall +0x60` (`nt+0x46d890`) | `RDMSR` → **+8** → `WRMSR` | 아니오 (엔클레이브 호출) |
| `KeUserModeCallback +0x37f` (`nt+0x8e7edf`) | 저장해 둔 SSP (`R12`) 를 `WRMSR` | 아니오 (유저 콜백에서 돌아올 때) |
| `KiProcessControlProtection` 조각 등 몇 곳 | `WRMSR` | 아니오 |
| 트랩 진입 조각 약 40개 (`KiApcInterrupt` 등) | SSP 윗 32비트가 비정상이면 고쳐 씀 | 아니오 (트랩 **들어올 때**) |

- 그리고 `NtContinue` 길 함수 여덟(`NtContinueEx`, `KiContinueEx`, `KiContinuePreviousModeUser`, `KiVerifyContextIpForUserCet`, `KeContextToKframes`, `KxContextToKframes`, `KiRestoreSetContextState`, `KiKernelExit`) 모두 **`WRMSR` 이 없다** `[정적]`.
- 이 길의 `0x6a7` 은 **읽기만** 있다: `KiContinuePreviousModeUser +0x315` 쪽과 `KiVerifyContextIpForUserCet +0x13a`.
- 재밌는 곳: `RtlPrepareEnclaveCall` 은 정말로 **+8 을 해서 책갈피에 꽂는다.** 그러니 "+8 을 꽂는 코드" 자체는 커널에 있다. 다만 **`NtContinue` 길이 아니다.**

#### 단계 4 — 그럼 유저 SSP 를 바꿀 수 있는 곳은? `XRSTORS` 하나 `[정적]`

- 이 길에서 유저 SSP 를 기계에 쓸 수 있는 줄은 **`KxContextToKframes +0x211 XRSTORS [R8]`** (`nt+0x3ea1d1`) 하나다.
- 새 말 — **`XRSTORS`** = 메모리에 적어 둔 CPU 확장 상태(XState) 묶음을 **한꺼번에 CPU 로 되돌리는** 명령 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 별지를 통째로 CPU 에 **되돌려 끼우는** 일. 별지에 "책갈피 위치" 칸이 있으면 책갈피도 그 값으로 다시 꽂힌다.
- 이 줄을 타는 조건 `[정적]`:
  - 메모 체크박스에 XState 가 있고 (`+0x193~+0x19f`, `0x100040`),
  - 이전 모드가 유저이고 (`+0x1a1 CMP R14B,1`).
  - `R8` = 메모의 XState 영역 (`+0x1b0~+0x1be`). 되돌릴 기능 마스크는 영역 안 마스크 `[R8+0x208]` 과 켜진 기능을 AND (`+0x1c0~+0x207`).
- 그 별지에 정말 "책갈피 위치" 칸이 있다는 증거 `[정적]`:
  - `KiContinuePreviousModeUser` 가 기능 번호 `0xb` 칸을 찾는다 (`+0x201` ret `+0x206` `RtlLocateExtendedFeature2`).
  - 칸 모양을 검사한다: `+0x355` (ret `+0x35a`) `KiVerifyContextXStateCetUEnabled` 가 `[RCX] == 1`, `[RCX+8]` 이 8의 배수·현재 SSP 이상·그림자 스택 영역 안인지 본다. 아니면 `0xc000060a`.
- `XRSTORS` 가 이 칸으로 `IA32_PL3_SSP` (MSR `0x6a7`) 를 되돌린다는 건 `[추측: CPU 설명서 지식, 코드에선 안 보임]` (Intel SDM). 유저로 `IRETQ` 할 때 CPU 가 그 값을 SSP 로 올리는 것도 같은 종류의 지식이다.
- 다른 갈래도 있다 (이 길 밖일 가능성 `[추측]`): 스레드 `+0x198` 칸이 0 이 아니면 `XRSTORS` 대신 `+0x55c` (ret `+0x561`) `RtlCopyXStateArea`, `KUSER` `ControlFlags` 압축 비트가 꺼져 있으면 `+0x3e5` (ret `+0x3ea`) `RtlXRestore`. 어느 쪽인지는 동적.

#### 단계 5 — 별지의 "책갈피 위치" 칸에는 +8 이 들어가나? — 아니다 `[정적]`

- 칸 값은 **경비가 맞춰 보기(`+0x399`) 전에** 이미 정해진다. 두 경우다:
  - (가) 메모가 별지 칸을 **이미 가져왔으면**: 그 값이 쓰인다. `+0x355` 는 검사만 한다.
  - (나) 없으면: `KiContinuePreviousModeUser` 가 칸을 채운다 — `+0x46b BTS R9,0xb`, `+0x478 [R8] = 1`, `+0x47f [R8+8] = RAX`. 이 `RAX` 는 `+0x31a RDMSR` 로 읽은 **현재 SSP** 다.
- `+0x399` 뒤에 `[R8+8]` 을 고치는 줄은 없다.
- 그러니 별지 칸에도 **+8 은 안 들어간다.** 메모지와 별지는 서로 다른 종이다.

#### 정리 — 유저 SSP 는 결국 무엇이 되나

- `[정적]` 올린 +8 은 지역 칸에서 사라진다.
- `[정적]` + `[추측: 위 CPU 지식]` 유저 SSP 는
  - (가) XState 가 없는 메모(체크박스에 `0x40` 없음)면 **책갈피 그대로**,
  - (나) XState + CET_U 칸이 있으면 **그 칸의 `[8]` 값** 으로 `XRSTORS` 때 바뀐다.
- 비유: 책갈피를 실제로 옮기는 건 **경비의 메모지가 아니라, 업무 시작 메모 별지를 되돌려 끼우는 순간**뿐이다.

#### 시동 사물함 메모에 대 보면 — 왜 요청서 가정이 CET 스레드엔 안 맞을 수 있나 `[추측]`

요청서는 "아무도 안 쓰면 SSP 는 `RtlUserThreadStart` 칸을 가리킨 채로 돌아간다" 를 결론 후보로 적었다. run 문서는 이렇게 따져 봤다:

1. `[정적]` 그 가정은 **XState 없는 메모**(책갈피 그대로)일 때 맞는 이야기다.
2. `[정적]` 그런데 CET 아닌 스레드는 장부에 예약 줄을 **애초에 안 적는다** (`KePopulateContinuationContext` 조건). 그 경우엔 "가리킨 채로" 할 예약 줄이 없다.
3. `[정적]` CET 스레드의 사물함 메모는 `0x10005b` 라서 **XState 가 있다.** 그러면 (나) 길, 즉 별지 칸 값으로 바뀐다.
4. `[정적]` 그 별지 칸은 `RtlCopyContext` (`+0x280`) 가 임시 메모에서 복사한다. 이 복사는 예약 줄 적기(`KePopulateContinuationContext`, `+0x295`) 보다 **앞**이다.
5. `[추측]` 그래서 칸 값이 "예약 줄 적기 **전**" 의 SSP 라면, `NtContinue` 가 끝났을 때 SSP 는 `RtlUserThreadStart` 칸을 **지나 그 위**(예약 줄을 적기 전 자리)에 있게 된다. 적었던 한 칸을 되돌린 셈이다.
6. 아직 확인 아님: 임시 메모의 별지 칸을 누가 무슨 값으로 채우는지(`PspGetContextThreadInternal` / `RtlpCopyXStateChunk`)는 **안 읽었다** → 정적 **Q75** (a). 동적 **Q57** `rdmsr` 한 번으로도 풀린다.

- 비유: 경비 메모지는 버려졌지만, 직원이 낸 사물함 메모 **별지에 "예약 줄 적기 전 책갈피 위치"** 가 적혀 있었다면, 별지를 되돌려 끼울 때 책갈피가 그 자리로 돌아간다. 결과만 보면 +8 을 꽂은 것과 같은 자리일 수도 있다 `[추측]`. 다만 그건 칸 값을 봐야 안다.

#### 곁가지 — 경비를 부르는 다른 곳 `[정적]`

- `KiVerifyContextIpForUserCet` 는 `NtContinue` 길 밖에서도 불린다: `KeVerifyContextIpForUserCet` (`+0x5d`), `KeVerifyContextRecord` (`+0xdc`) — `SetThreadContext`·예외 올리기 길.
- 둘 다 5번째 인자를 **받은 대로 넘길 뿐**이다. 그 위(`PspGetSetContextInternal`)가 그 칸을 쓰는지는 안 봤다 → **Q75** (b).

### 2.5 핵심 ③ `KiInitiateUserApc` 는 심부름꾼, 보관은 `KiInitializeUserApc`

"잠깐 이것부터 해" 쪽지가 있을 때, 직원이 **원래 하던 일**을 어디에 적어 두느냐가 질문이었다.

#### 심부름꾼 — `KiInitiateUserApc` (208바이트) `[정적]`

자기는 문맥을 **보관하지 않는다.** 하는 일은 이것뿐이다:

1. `+0x0 SUB RSP,0x138` — 커널 스택에 자리를 잡는다.
2. XMM6~15, `RBX`·`RDI`·`RSI`·`R12~R15` 를 그 자리에 저장한다. = **예외 프레임**(개인 서랍) 칸을 채우는 것.
3. 인자 셋을 준비한다: `+0x5f ECX = 1` (이전 모드 = 유저), `+0x64 RDX = RSP` (예외 프레임), `+0x67 R8 = [RBP−0x80]` (트랩 프레임).
4. **`+0x6b CALL KiDeliverApc`** (ret `+0x70`). 이 함수의 **유일한 호출**이다.
5. 서랍을 되돌리고 `RET`.

- 트랩 프레임(출발 메모)이 **이미 "원래 문맥"** 이라 주소만 넘긴다.
- 비유: 심부름꾼은 직원의 **개인 서랍을 정리해 두고**, 출발 메모와 서랍 위치만 들고 **배달 담당**에게 "이 직원한테 쪽지 있어요" 라고 넘긴다. 메모를 베끼진 않는다.

#### 배달 담당 → 보관 담당 `[정적]`

- `KiDeliverApc +0x5ee` (ret `+0x5f3`) → **`KiInitializeUserApc`** (`nt+0x3ec4a8`).
- `KiDeliverApc` 본문은 **안 읽었다** (범위 밖, `_calls.tsv` 줄만).
- flow.md 그림 상자에는 "(1) 멈춤 APC 상자의 것과 같은 함수, 다른 때" 라고 적혔다 — flow.md 2.2 멈춤 APC 길(`KiApcInterrupt +0x3ab → KiDeliverApc`)의 그 함수다. 그때는 멈춤 쪽지를, 이번엔 유저 쪽지를 배달한다.

#### 보관 담당 — `KiInitializeUserApc` 가 하는 일, 순서대로 `[정적]`

| 순서 | 자리 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x81` / `+0x374` | 플래그 `0x10001b` (CET 면 `0x10005b`) — 질문 1 과 같은 값 | 베낄 메모 체크박스 정하기 |
| 2 | `+0x14b~+0x16e`, `+0x16e` (ret `+0x173`) `ProbeForWrite` | 트랩 프레임 `Rsp` (`[RSI+0x180]`) 에서 `0x4d0`바이트 (+XState 길이) 내린 자리 `RDI` 를 잡고, 쓸 수 있나 확인 | 직원 수첩 아래쪽에 **빈 쪽**을 잡고, 찢어진 데 없나 보기 |
| 3 | **`+0x1ec`, `+0x1fb` (ret `+0x200`) `KeContextFromKframes(트랩, 예외, RDI)`** | `[RDI+0x30]` 에 플래그, 그다음 원래 문맥을 **유저 스택 `CONTEXT`** 로 복사 | 원래 하던 일을 그 쪽에 **업무 시작 메모 꼴로 베끼기** |
| 4 | `+0x21d` (ret `+0x222`) `KePopulateContinuationContext`, CET 면 `+0x3e5` (ret `+0x3ea`) `KiUnwindUserSspForApcContextCopyBypass(RDI)` | CET 면 원래 `Rip` 를 그림자 스택에 (출발 때와 **같은 도우미**) | 보안 장부에 "쪽지 다 보면 여기로" **예약 줄** |
| 5 | `+0x260~+0x27f` | APC 인자 (`NormalContext`, `SystemArgument1/2`, `NormalRoutine`, 플래그) 를 그 `CONTEXT` 앞 홈 칸 `[RDI+0x0~0x20]` 에 | 베낀 메모 **바로 앞에 쪽지 내용**을 끼우기 |
| 6 | **`+0x283 [RSI+0x180] = RDI`**, **`+0x28a~+0x291 [RSI+0x168] = PsNtdllExports+0x28`**, `+0x29d SegCs = 0x33` | 출발 메모 `Rsp` = 그 자리, `Rip` = `KiUserApcDispatcher` | 출발 메모를 **"쪽지 읽는 창구로"** 바꿔 붙이기 |

- 새 말 — **`ProbeForWrite`** = 커널이 유저 메모리에 쓰기 전에 "여기 써도 되는 자리인가" 를 확인하는 함수. run 문서 표현대로 "유저 스택에 쓸 수 있나".
- 새 말 — **`KiUserApcDispatcher`** = ntdll 안에서 유저 APC 쪽지를 받아 처리하는 입구. `PsNtdllExports+0x28` 칸이 이것이라는 건 sast/11 에서 정리됐다.
  - 비유: 가게 안 관리실의 **"쪽지 읽는 창구"**. 벽 주소록(`PsNtdllExports`)의 `+0x28` 칸에 적혀 있다.

#### 한 줄로, 그리고 run16 과 닮은 점

- `[정적]` **원래 문맥은 유저 스택에 `CONTEXT` 로 복사해 두고, 트랩 프레임 `Rsp`/`Rip` 를 그 자리와 `KiUserApcDispatcher` 로 바꿔 유저로 보낸다.**
- 이 모양은 run16 의 **바꿔 끼우기**와 닮았다:

| | 원래 메모를 맡기는 곳 | 체크박스 | CET 예약 줄 | 출발 메모 `Rip` 를 바꾸는 곳 |
|---|---|---|---|---|
| run16 출발 (`PspInitializeThunkContext`) | 유저 스택 (사물함 메모) | `0x10001b` / `0x10005b` | `KePopulateContinuationContext` | 오리엔테이션실 `LdrInitializeThunk` |
| 이번 APC (`KiInitializeUserApc`) | 유저 스택 `CONTEXT` | `0x10001b` / `0x10005b` | `KePopulateContinuationContext` | 쪽지 읽는 창구 `KiUserApcDispatcher` |

- 표의 칸은 모두 run 문서 사실이다. "닮았다" 는 이 글의 정리일 뿐이다.
- 쪽지를 다 읽은 뒤 베껴 둔 메모로 돌아가는 건 **유저 쪽 `KiUserApcDispatcher` 몫**이라, 이번에 읽은 커널 함수들 안에서는 **안 보인다** (run 문서). run 문서 비유는 이걸 `NtContinue` 로 돌아간다고 그렸다. 이번 코드에선 안 본 부분이라 `[추측]` 으로 둔다 (run16 의 오리엔테이션실이 `NtContinue` 로 돌아가는 것과 같은 꼴일 것이라는 짐작).
- 비유로 이으면: 쪽지가 있으면 관리사무소는 직원이 **원래 하던 일을 수첩 한 쪽에 베껴** 두고, 그 앞에 쪽지를 끼우고, 출발 메모를 "쪽지 읽는 창구" 로 바꿔 붙인다. 직원은 창구에서 쪽지를 먼저 처리하고, 그다음 수첩의 베낀 메모를 보고 원래 일로 돌아간다.

#### `NtContinue` 길에서는 "원래 일" 이 무엇이 되나 `[추측]`

- `[정적]` (run29) `NtContinue` 길에서는 출발 메모가 사물함 메모로 **먼저** 덮이고, 그 **뒤에** 나가는 문 고리가 `KiInitiateUserApc` 를 부른다.
- `[정적]` (이번) `KiInitiateUserApc` 는 그 순간의 트랩 프레임을 "원래 문맥" 으로 넘긴다.
- `[추측]` 두 사실을 이으면, 이 길에서 쪽지가 있었다면 수첩에 베껴지는 "원래 일" 은 **본업 자리(`RtlUserThreadStart`)로 가는 메모**다. 실제로 쪽지가 있을 때만 해당하고, 값으로 본 적은 없다.

#### 나가는 문 고리 다시 보기 `[정적]`

- `NtContinueEx +0x148 TEST byte [thread+0xc2],3`
- → `+0x151~+0x15a` IRQL 1 + `STI`
- → `+0x15b` (ret `+0x160`) `KiInitiateUserApc`
- → `CLI`, IRQL 0 → `+0x16a JMP` 로 `+0x13f` (`nt+0x6adf2f`) 돌아가 **다시 검사**.
- 비유: 문 앞 확인 담당이 쪽지가 있으면 방해 금지 표지판을 한 단계 올리고 쪽지 배달을 시킨 뒤, **또 쪽지가 없나** 한 번 더 본다.
- `KiContinueEx +0x114` 도 `KiDeliverApc` 를 직접 부르는 갈래가 있지만, BOOLEAN 길(이번 길)은 못 탄다 (run29).
- 덧: flow.md 2.3 의 출근 첫 발(run15 `Kx +0xfa ~ +0x132`, ret `+0x11b`) 고리도 같은 `KiInitiateUserApc` 를 부른다. 거기에도 이번 결과 한 줄이 붙었다.
- test.exe 시동 때 이 길을 실제로 타는지는 정적으로 못 정한다 → 동적 **Q57**.

### 2.6 run29 정정 — 전과 후

run29 문서 맨 위에 정정 한 줄이 들어갔다.

| run29 에 적힌 것 | 이번 (run42) |
|---|---|
| 맡긴 문맥 `ContextFlags` = `0x10001b` 로 보인다 `[추측: …가정]` | 틀린 건 아니고 **`[정적]` 으로 확정**. 단 CET 스레드는 `0x10005b`. D2 의 "기대 `0x10001b`?" 도 CET 면 `0x10005b` |
| 검사가 올린 새 그림자 스택 포인터를 다시 쓰는 줄은 못 찾았다, 유저 SSP 는 `[공백]` | **아무도 안 쓴다** `[정적]`. XState 메모면 `KxContextToKframes +0x211 XRSTORS` 로 칸 값이 된다 (`[추측: CPU 지식]`) |
| 배달할 APC 가 있으면 … `KiInitiateUserApc` 가 APC 진입을 만든다 `[추측]` | 방향은 맞다. 만드는 곳은 **`KiDeliverApc` → `KiInitializeUserApc`**, 원래 문맥은 **유저 스택 `CONTEXT`** `[정적]` |

- run29 해설 2.9 끝 "아직 빈 곳" 의 비유 — "경비가 장부 맨 위 줄을 지웠다고 **메모만** 했는지, 장부에 **실제로 반영**했는지" — 의 답은 **"메모만 했고, 그 메모는 버려졌다"** 다.

### 2.7 곁가지 — 도구 메모

run 문서 "도구 메모" 를 옮긴다. 답에는 영향이 없다.

- 결과서의 `KiInitializeUserApc` "`+0x32c OR R15D,0x100040`" 은 `.asm` 으로 보면 **`+0x32c OR R15D,R12D`** 이고, `R12D` 는 `+0xb3 MOV R12D,0x100040` 에서 온 값이다. 뜻은 같다. run 문서와 이 글은 `.asm` 을 따랐다.
- 결과서의 `KxContextToKframes` "`+0x1cb JNZ +0x4e2`" 의 `+0x4e2` 는 점프 목적지 **주소 끝자리** (`0x1403ea4e2`) 다. 함수 시작 `0x1403e9fc0` 기준으로는 **`+0x522`**. run 문서는 그 줄을 인용하지 않고 호출 자리 `+0x55c` (ret `+0x561`) 만 썼다.
  - 비유: 버스 정류장 번호를 "노선 기준 몇 번째" 가 아니라 "도시 전체 번호 끝 세 자리" 로 적은 셈이다.
- 바이트 검색은 하네스 밖 임시 스크립트다. "길에 없다" 의 근거는 (1) 그 꼴 기준 검색 + (2) `NtContinue` 길 함수 `.asm` 을 따로 읽은 것, 두 가지다.
- **디버거가 붙어서 달라질 수 있는 점** `[추측]`: Q57 확인거리는 `rdmsr 6a7`·`dq` 로 **읽기만** 하니 상태를 바꾸지 않을 것.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만 적는다.

| 자리 | 전 (run41 뒤) | 후 (run42) |
|---|---|---|
| 기준 | ntkrnlmp.pdb 줄 run41 까지, 정적 근거 run9 ~ run41 | **run42** 추가 (ntoskrnl). 새 줄: "유저 SSP 를 누가 쓰나" 는 바이트 검색, `XRSTORS`/`XSAVES` 는 `.asm` 으로 따로 |
| 그림 2 `nc7` (`KiVerifyContextIpForUserCet`) | "Rip 를 그림자 스택 칸과 대조 — 2.3 에서 넣은 칸" | + "**올린 SSP +8 은 아무도 안 씀** — run42" |
| 그림 2 `nca` (`KiInitiateUserApc`) | "유저 APC 대기 비트가 있을 때만. 시동 때 있을지는 추측: 없음" | + "**자기는 문맥 보관 안 함** — run42" |
| 그림 2 새 상자 둘 | 없음 | **`ncb` `nt!KiDeliverApc`**, **`ncc` `nt!KiInitializeUserApc`** (원래 문맥을 유저 스택 CONTEXT 로, 트랩 Rsp·Rip). 화살표 `nca -->|+0x70| ncb`, `ncb -->|+0x5f3| ncc` |
| 2.3 6번 줄 (`RtlCopyContext`) 아래 | — | 새 `[정적]` 줄: 사물함 메모 `ContextFlags` = `0x10001b` / CET `0x10005b`. 4번의 `0x10000b` 는 작은 메모 것이라 다름 |
| 2.3 run15 출근 첫 발 3번 (`KiInitiateUserApc`) 아래 | — | 새 `[정적]` 줄: 본문은 `KiDeliverApc` 하나 부르기, 복사는 `KiInitializeUserApc` |
| 2.10 제목 | run41 까지 | **run42** 추가 |
| 2.10 코드 사슬 트리 | `+0x39e KiVerifyContextIpForUserCet  CET 비트 둘 다일 때만` | + "올린 SSP(+8)는 버려짐". 새 줄 `+0x211 XRSTORS — XState 문맥일 때만`, `+0x70 KiDeliverApc ─ +0x5f3 KiInitializeUserApc` |
| 2.10 CET 맞춰 보기 | "누가 유저 SSP 로 쓰나 `[공백]`" | **취소선** → "아무도 안 쓴다" `[정적]`. 하위 세 줄: `XRSTORS` 하나 `[정적]` / MSR 되돌림 `[추측: CPU 지식]` / 시동 칸 값 이야기 `[추측]` (Q75·Q57) |
| 2.10 `ContextFlags` 줄 | "`0x10001b` 로 보인다 `[추측: …가정]`" | **취소선** → `[정적]` `0x10001b` 맞음, CET `0x10005b`. "어느 쪽이든 CONTROL·INTEGER 가 켜져 넷 다 옮겨진다" |
| 2.10 유저 APC 고리 | "원래 문맥은 APC 틀에 보관될 것 `[추측]`" | **취소선** → `[정적]` 세 줄 (심부름 / `KiInitializeUserApc` 복사 / 돌려받기는 유저 쪽, 동적 Q57) |
| 4장 공백 표 Q29·Q57 줄 | run29 멈춤점 | run42 멈춤점 덧붙임 (`nt+0x910982` `dd @r15+0x30 L1`, `rdmsr 6a7` 앞뒤 비교 자리들, `nt+0x6adf4b`, `nt+0x3ec6a3`) |
| 4장 "정적으로 남은 것 (run29)" | Q58 넷 | **(해결됨, 정적)**. 넷 다 취소선 + 답 (셋은 run42, 넷째는 run41) |
| 4장 새 줄 | 없음 | **"정적으로 남은 것 (run42)" — Q75** (a)·(b)·(c) |
| 4.1 그림 2 표 | `KiInitiateUserApc` 까지 | `→ KiDeliverApc → KiInitializeUserApc (run42)`. 멈춤점은 입구 대신 `nt+0x6adf4b` 나 `nt+0x3ec6a3` (입구는 호출자가 많아 시끄러움) |
| 5장 | run41 까지 | run42 줄 두 개 (결과 요약, 요청서 번호 표) |

태그로 보면 핵심 변화는 셋이다:

- **사물함 메모 `ContextFlags`: `[추측]` → `[정적]`** (`0x10001b`, CET `0x10005b`). 실제 값은 아직 (Q29).
- **새 SSP 를 누가 쓰나: `[공백]` → `[정적]` "아무도 안 씀"**. 대신 `XRSTORS` 가 유저 SSP 를 되돌린다는 건 **`[추측: CPU 지식]`** 으로 새로 생겼다.
- **APC 때 원래 문맥 보관: `[추측]` → `[정적]`** (`KiInitializeUserApc` 가 유저 스택 `CONTEXT` 로). 그림에 상자 둘이 늘었다.

### 큰 그림에서 어떻게 이어지나

- **굵은 길은 그대로다.** `LdrInitializeThunk` → `NtContinue` → `IRETQ` → `RtlUserThreadStart` 사슬(run29)에 화살표가 새로 끊기거나 바뀐 곳은 없다.
- 바뀐 건 **조건부 곁가지 둘**이다:
  - CET 곁가지: "경비가 맞춰 보고 끝" 이 맞다. 책갈피는 경비가 아니라 **별지 되돌리기**가 정한다.
  - APC 곁가지: 실제로 쪽지가 있으면 어디로 새는지가 상자 둘(`KiDeliverApc` → `KiInitializeUserApc`)로 그려졌다.
- 둘 다 **"이 VM·이 실행에서 정말 켜지나"** 가 남았다. 그건 버스를 타 봐야 안다 (동적 Q29·Q57). 이번 바퀴가 그 버스에 들고 갈 멈춤점을 더 날카롭게 만들었다:
  - 체크박스: `nt+0x910982` 한 곳에서 `dd @r15+0x30 L1`.
  - 책갈피: 맞춰 보기 앞뒤(`nt+0x2bcae9`/`nt+0x2bcaee`)와 `XRSTORS` 앞뒤(`nt+0x3ea1d1`/`nt+0x3ea1d5`)에서 `rdmsr 6a7` 이 **바뀌나 안 바뀌나** 만 보면 된다.
- 대기열로는 단계 3 "작은 정적 몰아서" 가 이어진다. 다음은 **Q59** (kernel32 `BasepInitializeTermsrvFpns`, 정적) 이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 사물함 메모 `ContextFlags` 의 **실제 값** (`0x10001b` / `0x10005b`) | 정적은 "코드에 이렇게 쓰여 있다" 까지. CET 칸은 실행 때 값 | 동적 **Q29** — `nt+0x910982` (`PspInitializeThunkContext +0x29a`) `dd @r15+0x30 L1`. (선택) `nt+0x2b86b5` (`RtlInitializeExtendedContext2 +0xf5`) `edi`, 두 번 걸림 |
| test.exe 스레드가 CET 스레드인가 (`[thread+0x74]` bit 20) | 실행 때 값. 꺼져 있으면 질문 1 은 `0x10001b`, 질문 2 이야기는 전부 해당 없음 | 동적 **Q57** |
| 예약 줄 적기가 정말 −8 인가 | 실행 때 값 | 동적 **Q57** — `nt+0x91097d` / `nt+0x910982` (`+0x295` / `+0x29a`) 양쪽 `rdmsr 6a7` |
| 맞춰 보기 뒤 책갈피가 정말 안 바뀌나 ("아무도 안 씀" 확정) | 정적 근거는 "읽는 줄 없음 + 찾은 꼴 기준 검색" | 동적 **Q57** — `nt+0x2bcae9` / `nt+0x2bcaee` (`+0x399` / `+0x39e`) `dq @rbp+8 L1`·`rdmsr 6a7`. `nt+0x411ca0` (`KiVerifyContextIpForUserCet +0x128`) 에 닿나 |
| `XRSTORS` 가 정말 책갈피를 되돌리나, 그 별지 칸 값 | 코드엔 명령만 있고 효과는 CPU 설명서 지식 `[추측]` | 동적 **Q57** — `nt+0x3ea1d1` / `nt+0x3ea1d5` 양쪽 `rdmsr 6a7`, `+0x211` 에서 `@r8` CET_U 칸 |
| `XRSTORS` 대신 `RtlCopyXStateArea` / `RtlXRestore` 갈래를 타나 | 스레드 `+0x198` 칸, `KUSER` 압축 비트는 실행 때 값 | run 문서 "어느 쪽인지는 동적" (번호 없음). Q57 의 `+0x211` 멈춤점에 서는지로 같이 보일 것 `[추측]` |
| 시동 별지 CET_U 칸의 SSP 는 어디서 오나 (예약 줄 **전** 값인가) | `PspGetContextThreadInternal`·`KeContextFromKframes`·`RtlpCopyXStateChunk`·`RtlpCopyKernelCetChunk` 본문을 안 읽었다 | 정적 **Q75** (a). 동적 Q57 `rdmsr` 한 번으로도 풀림 |
| `PspGetSetContextInternal` 이 5번째 칸 값을 쓰나 | `SetThreadContext` 길, 범위 밖 | 정적 **Q75** (b) |
| 이름 없는 조각 `FUN_1406f9e12` (`+0xf4` `WRUSSQ` + `WRMSR 0x6a7`) 가 어느 함수 조각인가 | `pdb_aliases.py --where` 를 안 돌렸다. `NtContinue` 길과는 무관 | 정적 **Q75** (c) |
| 시동 때 유저 APC 쪽지가 정말 오나, `KiInitializeUserApc` 가 걸리나 | 큐에 무엇이 있는지는 실행 때 | 동적 **Q57** — `nt+0x6adf4b` (`NtContinueEx +0x15b`), `nt+0x3ec6a3` (`KiInitializeUserApc +0x1fb`, `r8` = 유저 스택 문맥 자리). 입구 `nt+0x6b3260` 은 호출자가 20곳 넘어 시끄러움 |
| `KiDeliverApc` 안에서 `KiInitializeUserApc` 까지 가는 조건 | `KiDeliverApc` 본문은 범위 밖 (`_calls.tsv` 줄만) | 대기열 번호 없음 |
| 쪽지 처리 뒤 `KiUserApcDispatcher` 가 베낀 메모로 어떻게 돌아가나 | 유저 쪽 함수, 이번 범위 밖 | 대기열 번호 없음 |
| `NtContinue` 가 받을 때 체크박스 고침 갈래(`+0x459`)를 안 타나 | 시동 메모가 멀쩡하면 안 탈 것 `[추측]` | 동적 Q29 에서 체크박스 값을 보면 같이 판단 |

모든 커널 멈춤점은 모든 프로세스가 지나니 **test.exe 조건이 꼭 필요하다** (run 문서). 오프셋은 `nt + rva` 이고, 부팅마다 바뀌는 주소가 아니다.

---

## 5. 더 읽기

- run 문서: [`run42-continue-detail.md`](run42-continue-detail.md) — 호출 지점 표 전체, 질문 1 의 값 정하기·두 메모·`RtlCopyContext` 풀이, 질문 2 의 `[RBP+0x8]` 쓰임 전부·`WRMSR 0x6a7` 표 전체·`XRSTORS` 조건, 질문 3 의 `KiInitializeUserApc` 여섯 단계, 호출 관계 그림, 동적 확인 목록
- 정적 결과서: [`_workspace/sast/37_continue-detail_result.md`](../../_workspace/sast/37_continue-detail_result.md) — 질문별 답, 동적 확인 표, 새 정적 공백, 추가로 뽑은 것과 이유
- 요청서: [`_workspace/sast/37_continue-detail.md`](../../_workspace/sast/37_continue-detail.md) — 질문 셋, 넷째를 뺀 이유
- flow.md: [`flow.md`](flow.md)
  - 그림 2 `nt` (2) 묶음의 `nc7`·`nca`·`ncb`·`ncc`
  - 2.3 `PspInitializeThunkContext` 6번 줄 아래 run42 줄, run15 출근 첫 발 3번 아래 run42 줄
  - **2.10** 코드 사슬 트리, `KiContinuePreviousModeUser` CET 맞춰 보기, `ContextFlags` 줄, "유저로 돌아가기" 2 (유저 APC 고리)
  - 4장 공백 표 Q29·Q57 줄, "정적으로 남은 것" run29·run42 줄, 4.1 그림 2 표
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 앞 해설:
  - [run29 해설](run29-continue-start-해설.md) — 2.7 `ContextFlags`·깎기, 2.8 옮기는 칸, **2.9 CET 경비실 장부** (이번 질문 2 의 출발점), **2.10 TestAlert·쪽지** (이번 질문 3 의 출발점)
  - [run16 해설](run16-thunk-context-해설.md) — 2.6 바꿔 끼우기 (사물함 메모·작은 메모 둘), 2.9 벽 주소록
  - [run15 해설](run15-user-thread-startup-해설.md) — 2.6 트랩 프레임·예외 프레임·`IRETQ`, 출근 첫 발의 쪽지 고리
  - [run13 해설](run13-resume-thread-해설.md) — 2.3 쪽지와 신호등
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q58 (끝), Q59 (다음), Q29, Q57, Q75, 결정 기록 2026-10-24 "Q58 시작"·"Q58 끝"
