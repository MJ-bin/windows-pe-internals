# run18 proc-init-setup — 해설

> 이 글은 [run18 문서](run18-proc-init-setup.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **정정 (run19)**: 2.4 의 "exe 폴더(`[RSP+0x130]`)" 는 틀렸다. exe **파일 이름 부분**(`BaseDllName`, test.exe)이다. 4절의 "`RCX`(로더 문맥)" 은 정확히는 **CONTEXT 레코드**(사물함에 맡긴 원래 메모)다. 본문은 그대로 두었다 → [run19 해설 2.1](run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름) · [2.2](run19-proc-init-modules-해설.md#22-run18-정정-둘--입구-rcx-는-context-레코드)

- 대기열: **Q9a** — 2.4 `ntdll!LdrpInitializeProcess` 앞쪽 `+0x0 ~ +0x12dd` callee, 입구에서 RCX·RDX 쓰는 곳 (`_workspace/plan.md`)
  - 원래는 Q9 한 줄이었다. 함수가 너무 커서 **그 자리에서 둘로 나눴다** (Q9a 앞쪽 / Q9b 뒤쪽). 이유는 2.1.
- 근거:
  - run 문서 [`run18-proc-init-setup.md`](run18-proc-init-setup.md) (가장 중요)
  - 요청서 [`_workspace/sast/13_proc-init-setup.md`](../../_workspace/sast/13_proc-init-setup.md)
  - 결과서 [`_workspace/sast/13_proc-init-setup_result.md`](../../_workspace/sast/13_proc-init-setup_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q9a, Q9b, Q44, Q45 와 결정 기록
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 과 같은 파일이다.
  - 문자열(`\KnownDlls`, `KnownDllPath`, IFEO 값 이름)은 결과서가 **원본 바이트**로 읽었다.
  - 구조체 칸 이름(`_PEB_LDR_DATA`, `_TEB.WowTebOffset`)은 **PDB 타입**으로 맞췄다.

이 글은 [run17 해설](run17-ldr-init-entry-해설.md) 다음 편이다.

- 기초 말(오프셋, call/ret, 콜스택, 태그 …)은 [run9 해설](run9-create-user-process-해설.md) 2절, PEB·구조체 칸 오프셋은 [run10 해설](run10-allocate-process-해설.md) 2절에 있다. 여기선 **링크만** 건다.
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - PEB = 가게 **안내판**, TEB = 직원 **개인 안내판** (run10·run11)
  - ntdll = 가게 안 **관리실**. 관리사무소는 test.exe 건물과 관리실까지만 놓아 주고, **나머지 건물은 관리실 직원이 들여온다** (run10)
  - 커널이 넘긴 `Rdx` = "**ntdll 건물 번지**" 쯤 (run16)
  - 칠판(`LdrpProcessInitialized`)을 0 → 1 로 먼저 고친 직원 = 첫 스레드 = **가게 전체 세팅 담당** (run17)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번 run 문서는 "이사 들어가기 전 집 안 정리" 라는 비유를 썼다. 이 글은 그 내용을 **가게 그림 그대로** 옮겨서 쓴다 (가리키는 것은 같다).
  - 주소록 공책 → 가게 **건물 명부** (PEB.Ldr)
  - 집주인 메모 → **가게 이름별 특별 지시** (IFEO. run10 해설 3.4 에서 쓴 말)
  - 창고 → 가게 **창고** (프로세스 힙)
  - 동네 공용 창고 위치 → **동네 공용 창고 위치** (`\KnownDlls`)
  - 주소록 첫 줄에 집주인 자기 이름 → 건물 명부 첫 줄에 **관리실 자기 이름** (ntdll 모듈 항목)

한 줄로 먼저 말하면:

- `LdrpInitializeProcess` 는 **호출 169곳**짜리 거대한 함수라서, 이번엔 **앞쪽 94곳**(`+0x0 ~ +0x12dd`)만 봤다. 뒤쪽 75곳은 다음 바퀴(Q9b)다.
- 앞쪽은 **"살림 차리기"** 다. 건물 명부를 펴 두고, 특별 지시를 읽고, 창고를 만들고, 공용 창고 위치를 적어 두고, 명부 첫 줄에 관리실 이름을 쓴다 `[정적]`.
- 그리고 **아무것도 메모리에 올리지 않는다** `[정적]`. 새 건물(DLL)을 들여오는 일은 전부 뒤쪽이다.
- 덤: 커널이 넘긴 `RDX` 는 **ntdll 시작 주소로 쓰인다** `[정적]`. run16·17 의 `[추측]` 을 코드 쓰임으로 뒷받침했다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ +0xe  로더 입구 네 함수 → 첫 스레드 판정                                   (run17)
             └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)
                  ├─ 앞쪽 +0x0 ~ +0x12dd    살림 차리기 (94곳)                  ← 이번 (run18)
                  └─ 뒤쪽 +0x12de ~ 끝      kernel32·import DLL·TLS (75곳)      ← 다음 Q9b
                       (+0x19a2 / +0x1bbb / +0x1c99 는 01_sprint 에서 [관찰])
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- run17 이 "첫 스레드만 `LdrpInitializeProcess` 로 간다" 까지 왔다.
- 이번엔 그 **함수 안으로 처음 들어갔다.** 단 앞쪽 절반만.
- flow.md 로는 **2.4 `LdrpInitializeProcess` 절**과 그림 2 의 ntdll 상자 안 `(0) 프로세스 준비` 상자다.

### 무엇이 궁금했나

run 문서 "목적" 그대로.

1. **앞쪽 구간이 무엇을 어떤 순서로 하나** — 묶음으로 정리하기. flow.md 2.4 의 이 함수 절은 `[관찰]` 3줄(전부 뒤쪽)과 `[추측]` 1줄("프로세스 준비 총괄")뿐이었다.
2. 덤으로 네 가지:
   - 입구 인자 `RCX`·`RDX` 를 어디에 쓰나 (run17 이 Q9 로 미룸)
   - 이 구간에서 DLL 을 메모리에 올리나
   - PEB.Ldr(건물 명부)는 언제 생기나
   - 디버거 때문에 갈리는 길이 있나

### 바퀴 전에 알던 것

- `[관찰]` (run7·run8) 이 함수 안에서 세 가지가 이 순서로 일어난다: `+0x19a2` kernel32 준비 → `+0x1bbb` import DLL 초기화 → `+0x1c99` exe 의 TLS 콜백.
  - 셋 다 **뒤쪽**이다. 그러니 앞쪽은 이번이 **처음 보는 곳**이다.
- `[관찰]` (run7) 메인 스레드에서 딱 한 번 불린다.
- `[추측]` (flow.md 2.4) 프로세스 하나당 한 번만 하는 유저 쪽 준비의 총괄 함수.
- `[정적]` (run17) 커널이 넘긴 `RDX` 가 네 함수를 그대로 지나 이 함수의 **둘째 인자**가 된다. 뜻은 `[추측: ntdll 주소]`.
- `[정적]` (run17) 이 함수 안에 넘겨받은 연결점: `+0x153` `LdrInitializePerThreadRng`, `+0x587` 핫패치 깃발 쓰기, `+0x5f2` `UseWOW64` 쓰기, `+0x1233` 핫패치 컨텍스트 읽기.
- `[추측]` (flow.md 2.5) "이 함수가 시작될 때는 test.exe 와 ntdll 만 올라와 있다."

---

## 2. 찾은 것

### 2.1 먼저 — 왜 반으로 잘랐나

**크기** `[정적]`

- `LdrpInitializeProcess` (rva `0x8c404`) 는 **7750바이트**, 직접 호출 **169곳**이다 (`plan.md` 결정 기록: 함수 113개).
- 지금까지 본 함수 중 제일 컸던 `PspAllocateProcess` 가 144곳이었다 (같은 기록). 그보다 크다.
- 그래서 `plan.md` 규칙대로 **그 자리에서 둘로 나눴다.**

**어디서 잘랐나**

- 경계 = **`+0x12de LdrpInitParallelLoadingSupport`**. 이름으로 보면 "병렬 로딩 준비" 다 `[추측]`. `plan.md` 결정 기록은 이 자리를 "모듈 목록 쪽 시작" 으로 보고 잘랐다.
- 앞쪽 = `+0x0 ~ +0x12dd` → **Q9a (이번)**
- 뒤쪽 = `+0x12de ~ 끝` → **Q9b (다음)**

**숫자 하나 고쳤다** (run 문서 "정정")

| | 요청서·plan 처음 | 실제 (`_calls.tsv`·지도·`.asm` 셋 다) |
|---|---|---|
| 앞쪽 | 95곳 | **94곳** |
| 뒤쪽 | 74곳 | **75곳** |
| 합 | 169 | 169 |

- 왜 하나가 옮겨 갔나: 경계에 있는 호출 하나(`+0x12de LdrpInitParallelLoadingSupport`)를 **어느 쪽에 세느냐** 의 차이다.
- `+0x12dd` 까지만 세면 94곳. 경계 `+0x12de` 는 뒤쪽 첫 호출로 센다.
- 비유: 책을 반으로 나눠 읽기로 했는데, 딱 경계에 걸친 한 문장을 앞 권에 넣을지 뒤 권에 넣을지. 이번엔 **뒤 권 첫 문장**으로 정했다.

**한 걸음 물러서서 — 이 함수의 앞쪽·뒤쪽은 어떤 느낌인가**

- 앞쪽 (이번): **살림 차리기.** 가게 안에서 쓸 공책·창고·위치 메모를 준비한다. 새 건물은 안 들인다.
- 뒤쪽 (다음): `[관찰]` 된 세 점이 다 여기 있다. kernel32 준비, import DLL, TLS 콜백 — **건물 들이기와 개업**.
  - 뒤쪽이 실제로 무엇을 어떤 순서로 하는지는 아직 **안 봤다** `[공백]`.

### 2.2 한눈에 — 앞쪽 대표 호출

run 문서 "호출 관계" 를 그대로 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)). ①~⑩ 은 2.4 의 묶음 번호다.

```
_LdrpInitialize +0x16c
 └─ LdrpInitializeProcess (0x8c404)        ← RCX: 저장만 (앞쪽 안 씀) / RDX: ntdll 주소로 씀
      │  앞쪽 +0x0 ~ +0x12dd  [정적] run18
      ├─ +0x158  LdrInitializePerThreadRng          ① 입구
      │  +0x1c1  PEB.Ldr = &PebLdr (빈 목록 3개)    ② (호출 아님, 직접 코드)
      ├─ +0x1cd  LdrpInitializeNlsInfo              ②
      ├─ +0x41a  RtlInsertInvertedFunctionTable     ③
      ├─ +0x536  LdrpInitializeExecutionOptions     ④ IFEO
      ├─ +0x6c5  RtlNormalizeProcessParams          ⑤
      ├─ +0x9d2  RtlInitializeCriticalSectionEx     ⑥ FastPebLock
      ├─ +0xa50  LdrpInitializeProcessHeap          ⑦ 프로세스 힙
      ├─ +0xb88  TpInitializePackage                ⑧
      ├─ +0xdbd  NtOpenDirectoryObject (\KnownDlls) ⑨
      ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry  ⑩ ntdll 모듈 항목 (조건 없음)
      ├─ +0x126f LdrpInitializeNtdllDataTableEntry     (핫패치 컨텍스트가 있을 때만)
      │  뒤쪽 +0x12de ~ 끝  [아직 안 봄, Q9b]
      ├─ +0x19a2 LdrpInitializeKernel32Functions    [관찰] run7
      ├─ +0x1bbb LdrpInitializeGraphRecurse         [관찰] run7·run8
      └─ +0x1c99 LdrpCallTlsInitializers            [관찰] run7·run8
```

읽을 때 주의:

- 앞쪽 94곳 중 **대표만** 남겼다. 전부는 `_calls.tsv` 의 `LdrpInitializeProcess` 줄 (커밋 안 됨).
- `+0x1c1` 줄은 **호출이 아니다.** `MOV` 한 줄로 PEB 칸에 값을 직접 쓴다. 이번 바퀴에서 제일 중요한 줄 중 하나라 트리에 넣었다 (2.6).
- 맨 아래 세 줄은 **01_sprint 에서 디버거로 본 것**이다. 앞쪽 정적 트리와 태그가 다르다.

### 2.3 한 줄씩 읽는 법

**줄 1: `├─ +0xa50  LdrpInitializeProcessHeap   ⑦`**

- 누가: `LdrpInitializeProcess`.
- 어디서: 부르는 자리 `+0xa4b`, 돌아올 자리 **`+0xa50`**.
- 누구를: `LdrpInitializeProcessHeap`.
- 무엇을 위해: **프로세스 힙을 만든다.** 결과가 PEB `+0x30` 칸 `ProcessHeap` 에 들어간다 `[정적]`.
  - **새 말 — 프로세스 힙**: 프로그램이 실행 중에 "메모리 조금만 빌려 줘" 할 때 꺼내 쓰는 **공용 메모리 창고**라고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 가게 **창고**. 직원들이 필요한 만큼 물건 칸을 빌려 쓰고 돌려놓는다.
- 이 줄이 왜 중요한가: 이 함수가 직접 부르는 `RtlAllocateHeap` 4곳은 **모두 이 뒤**(묶음 9)에 있다 `[정적]`. 창고를 만들기 전엔 창고에서 꺼내 쓸 수 없으니 순서가 맞다.

**줄 2: `├─ +0x11a2 LdrpInitializeNtdllDataTableEntry   ⑩`**

- 누가: `LdrpInitializeProcess`.
- 어디서: 부르는 자리 `+0x119d`, 돌아올 자리 **`+0x11a2`**.
- 누구를: `LdrpInitializeNtdllDataTableEntry` (633바이트. 본문은 안 읽었다).
- 무엇을 위해: **ntdll 자기 자신의 모듈 항목**(건물 명부의 한 줄)을 만든다 `[정적]`. **조건 없이** 부른다.
  - 넘기는 것: 첫째 인자 = ntdll 주소(`[RSP+0xd8]`, 입구에서 받은 `RDX`), 둘째 = 만든 항목을 받을 전역 `&LdrpNtDllDataTableEntry`, 넷째(R9) = `<NtSystemRoot>\System32\` 문자열 `[정적]`.
  - 실패(음수)면 `+0x11a7 JS` 로 함수가 끝난다 `[정적]`.

**줄 3: `│  +0x1c1  PEB.Ldr = &PebLdr (빈 목록 3개)`**

- 누가: `LdrpInitializeProcess` 가 **호출 없이 직접**.
- 어디서: `+0x1c1` 한 줄. `MOV [RDI+0x18], &PebLdr` `[정적]`.
  - `RDI` = PEB 다 (`+0x14c` 에서 `RDI = [TEB+0x60]` 으로 꺼내 둠) `[정적]`.
  - 그러니 "PEB `+0x18` 칸에 ntdll 전역 `PebLdr` 의 주소를 쓴다".
- 무엇을 위해: 가게 안내판에 "**건물 명부는 여기 있음**" 이라고 적는다. 이 순간 PEB.Ldr 가 생긴다 (2.6).

### 2.4 10묶음 표 — 천천히

이번 바퀴의 중심이다. 94곳을 **하는 일별로 10묶음**으로 나눈 표다 (run 문서 "앞쪽 큰 순서").

#### 표 읽는 법 — 칸 하나씩

| 칸 | 뜻 | 예 (묶음 7) |
|---|---|---|
| **#** | 묶음 번호. 2.2 트리의 ①~⑩ 과 같다 | 7 |
| **범위** | 이 묶음이 차지하는 **코드 구간**. 함수 시작에서 몇 바이트째부터 몇 바이트째까지. 묶음 경계는 호출과 호출 **사이**에서 잘랐다 | `+0x9e5 ~ +0xb7d` |
| **요약** | 그 구간이 하는 일. 굵은 글씨 = 그 묶음의 핵심 | **프로세스 힙 만들기** + 기능 스위치, 로더 스냅 버퍼 … |
| **대표 호출** | 흐름도 상자로 쓸 만한 호출 **하나**. `call 자리 / ret 자리` 두 숫자 | `LdrpInitializeProcessHeap` `+0xa4b / +0xa50` |
| **호출 수 (잡일)** | 이 범위 안 `call` 개수. 괄호 안은 그중 **잡일** 개수 | 13 (2) → 13곳 중 잡일이 2곳 |

- **잡일** = 거의 모든 함수에 흔히 나오는 도우미 호출 7종. 하나씩 풀지 않고 개수만 센다 (run 문서 정의):
  - `memset`, `RtlAppendUnicodeStringToString`, `RtlAppendUnicodeToString`, `LdrpLogInternal`, `RtlAllocateHeap`, `RtlFreeHeap`, `__security_check_cookie`
  - 비유: 칸 비우기, 글자 이어 붙이기, 일지 쓰기, 창고에서 빌리고 돌려주기, 마지막 자물쇠 확인. **어느 일에나 따라붙는 손놀림**이다.
- 주의 하나: 묶음은 **오프셋 순서로** 자른 것이다. 실제로 이 순서대로 서는지는 버스를 타 봐야 안다 (flow.md 4.1: Q44 에서 "서는 순서가 이대로인지" 확인). 오프셋 순서와 실행 순서가 다를 수 있다는 건 [run9 해설 3.7](run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서) 에서 봤다.

#### 표 (run 문서와 같은 숫자)

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) |
|---|---|---|---|---|
| 1 | `+0x0 ~ +0x157` | 입구 정리, TEB·PEB 꺼내기, 첫 스레드 난수 준비 | `LdrInitializePerThreadRng` `+0x153 / +0x158` | 4 (2) |
| 2 | `+0x158 ~ +0x28d` | **PEB.Ldr 만들기**, NLS 준비, 스핀 횟수, 비휘발 플러시 준비 | `LdrpInitializeNlsInfo` `+0x1c8 / +0x1cd` | 6 (4) |
| 3 | `+0x292 ~ +0x4a1` | exe 경로·NT 헤더, 보안 프로세스 검사, `LdrpHashTable` 비우기, **ntdll 을 역함수 표에 등록**, `.mrdata`, CFG | `RtlInsertInvertedFunctionTable` `+0x415 / +0x41a` | 12 (1) |
| 4 | `+0x4a8 ~ +0x579` | **실행 옵션(IFEO) 읽기** | `LdrpInitializeExecutionOptions` `+0x531 / +0x536` | 2 (1) |
| 5 | `+0x57a ~ +0x773` | 전역 켜고 끄기 (핫패치·`UseWOW64`·.NET 검사·매개변수 정규화·LoadConfig·`LdrpDebugFlags`) | `RtlNormalizeProcessParams` `+0x6c0 / +0x6c5` | 5 (1) |
| 6 | `+0x774 ~ +0x9e4` | PEB 의 TLS 비트맵·FLS 자리, 스택 추적 DB 옵션, **`FastPebLock`** | `RtlInitializeCriticalSectionEx` `+0x9cd / +0x9d2` | 8 (2) |
| 7 | `+0x9e5 ~ +0xb7d` | **프로세스 힙 만들기** + 그 뒤 준비 몇 가지 | `LdrpInitializeProcessHeap` `+0xa4b / +0xa50` | 13 (2) |
| 8 | `+0xb83 ~ +0xc41` | 스레드 풀, ETW 등록 3곳, 힙 로깅, **환경 블록·매개변수 블록** | `TpInitializePackage` `+0xb83 / +0xb88` | 11 (0) |
| 9 | `+0xc47 ~ +0x1168` | 시스템 경로 문자열, **`\KnownDlls` 열기 → `KnownDllPath` 읽기**, DLL 정책 | `NtOpenDirectoryObject` `+0xdb8 / +0xdbd` | 29 (17) |
| 10 | `+0x1169 ~ +0x12dd` | exe 폴더, **ntdll 모듈 항목**, (있으면) 패치 ntdll 항목, 초기화 순서 목록 연결 | `LdrpInitializeNtdllDataTableEntry` `+0x119d / +0x11a2` | 4 (0) |
| 합 | | | | **94 (30)** |

- 전부 `[정적]` 이다.
- 잡일 30 = `memset` 7, `RtlAppendUnicodeStringToString` 7, `LdrpLogInternal` 6, `RtlAllocateHeap` 4, `RtlAppendUnicodeToString` 4, `RtlFreeHeap` 1, `__security_check_cookie` 1.
- `LdrProtectMrdata` 5번(묶음 3 에 3번, 묶음 9 에 2번)은 잡일로 **안 셌다.** `.mrdata` 쓰기 보호를 껐다 켜는 **쌍**이라서다 (run 문서).

#### 한 줄을 끝까지 읽어 보기 — 묶음 9

표에서 제일 숫자가 큰 줄이다.

```
| 9 | +0xc47 ~ +0x1168 | 시스템 경로 문자열, \KnownDlls 열기 → KnownDllPath 읽기, DLL 정책 | NtOpenDirectoryObject +0xdb8 / +0xdbd | 29 (17) |
```

- **범위**: 함수 시작에서 `0xc47` 바이트째부터 `0x1168` 바이트째까지.
- **호출 수 29 (17)**: 이 안에 call 이 29곳. 그중 **17곳이 잡일**이다.
  - 왜 잡일이 이렇게 많나: 이 묶음은 경로 **문자열을 이어 붙이는** 일이 많다 (`<NtSystemRoot>` + `\System32` …). 그래서 `RtlAppendUnicode*` 와, 그 문자열을 담을 버퍼를 창고에서 빌리는 `RtlAllocateHeap` 이 여기 몰린다 `[정적]` (`RtlAllocateHeap` 4곳은 모두 묶음 9).
  - 그러니 이 묶음은 숫자는 크지만, 잡일을 빼고 보면 **하는 일 자체는 "경로 만들기 + 공용 창고 위치 적기 + 정책"** 정도다.
- **대표 호출** `NtOpenDirectoryObject` `+0xdb8 / +0xdbd`:
  - call 자리 `+0xdb8`, ret 자리 `+0xdbd`. 디버거 콜스택엔 `LdrpInitializeProcess+0xdbd` 로 찍힐 자리다.
  - 넘기는 이름은 **`\KnownDlls`** (원본 바이트로 확인) `[정적]`.
  - 뒤이어 `NtOpenSymbolicLinkObject` `+0xe47 / +0xe4c` (링크 이름 `KnownDllPath`), `NtQuerySymbolicLinkObject` `+0xe90 / +0xe95`, `LdrpInitializePolicy` `+0xfc5 / +0xfca` `[정적]`.
- **요약 한 줄로**: "시스템 폴더 경로를 글자로 만들어 두고, 공용 창고(`\KnownDlls`)가 어디 있는지 열어 보고 적어 둔다."

#### 묶음 하나씩 — 무엇을, 비유로

한 번에 한 묶음씩. 대표 호출은 위 표에 있으니 여기선 **하는 일**만 푼다. 모두 `[정적]` 이고, 따로 적은 것만 태그가 다르다.

**묶음 1 — 입구 (`+0x0 ~ +0x157`, 4곳)**

- 저장 레지스터 5개, 지역 변수 `0x410` 바이트, 보안 쿠키, 지역 구조체 0 채우기.
- 예외 필터 자리에 "빈 값" 을 암호화해서 넣어 둔다 (`+0x12f RtlEncodePointer`).
- `+0x13b R13 = GS:[0x30]` (TEB), `+0x14c RDI = [TEB+0x60]` (PEB). **이 뒤로 `RDI` 는 계속 PEB 다.** 아래 "PEB `+0x..`" 가 나오면 `RDI` 를 통해 쓰는 것이다.
- `LdrInitializePerThreadRng(TEB)` — 첫 스레드용 난수 준비. **조건 없이** 부른다.
- 비유: 세팅 담당 직원이 가게에 들어와 **자기 책상을 비우고**, 가게 안내판과 개인 안내판을 손에 쥔다.

**묶음 2 — 건물 명부 펴기 (`+0x158 ~ +0x28d`, 6곳)**

- **PEB.Ldr 만들기** — 2.6 에서 따로 천천히 본다.
- NLS 준비 (`LdrpInitializeNlsInfo`, 인자 = PEB). NLS 는 언어·문자 변환 같은 지역 설정 쪽 준비로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- CPU 가 2개 이상이면 스핀 횟수 3곳 설정, 비휘발 플러시 준비.
- 비유: **건물 명부 공책을 펴서 안내판에 걸고**, 가게 언어 설정을 맞춘다.

**묶음 3 — 자기 건물 확인 (`+0x292 ~ +0x4a1`, 12곳)**

- exe 경로 앞의 `\??\` 떼기 (호출 없음).
- exe NT 헤더 찾기 (`+0x35e` → 전역 `LdrpAppHeaders`). 보안 프로세스 검사.
- `LdrpHashTable` 32칸 비우기 (호출 없음).
- **ntdll 을 역함수 표에 등록** (`RtlInsertInvertedFunctionTable` `+0x415`).
  - **새 말 — 역함수 표**(inverted function table): "이 코드 주소는 어느 모듈 것인가" 를 거꾸로 찾는 표라고 알려져 있다. 예외가 났을 때 그 자리의 처리 정보를 찾는 데 쓴다고 한다 `[추측]` (일반적으로 알려진 설명).
  - 비유: "**이 번지는 어느 건물?**" 을 찾는 동네 지번 색인. 관리실 건물부터 먼저 올린다.
- `.mrdata` 쓰기 열고 닫기, 중요 thunk 잡기, CFG 확인.
- 비유: 가게 본 건물(exe)의 설계도를 펴 보고, 관리실 건물을 지번 색인에 올린다.

**묶음 4 — 특별 지시 읽기 (`+0x4a8 ~ +0x579`, 2곳)**

- 무결성 설정 비트 읽기 (`LdrSystemDllInitBlock+0xa0`).
- **실행 옵션(IFEO) 읽기** — `LdrpInitializeExecutionOptions`.
  - IFEO 뜻은 [run10 해설 3.4](run10-allocate-process-해설.md#34-144줄-중-어디쯤인가) 에서 한 줄로 봤다: exe 이름별로 실행 옵션을 적어 두는 레지스트리 자리 `[추측]` (일반적으로 알려진 설명).
- 비유: "**가게 이름별 특별 지시**" 서류를 읽는다. 커널(관리사무소)도 run10 에서 같은 서류를 봤는데, 이번엔 관리실이 자기 쪽에서 다시 읽는 것이다.
- 이 묶음은 호출이 2곳뿐이지만 중요하다. 뒤 묶음들의 갈림길(핫패치 끄기, 스택 추적 DB, 디버그 페이지 힙)이 **여기서 읽은 값**을 쓴다.

**묶음 5 — 스위치 켜고 끄기 (`+0x57a ~ +0x773`, 5곳)**

- 핫패치 끄기 (`+0x587`), `UseWOW64` 정하기 (`+0x5f2`) — 2.9 에서 따로.
- .NET(COR) 이미지 검사, 프로세스 매개변수 정규화(`RtlNormalizeProcessParams`).
- exe 의 LoadConfig 에 적힌 `NtGlobalFlag`·타임아웃 반영, `LdrpDebugFlags` 정하기.
- 비유: 특별 지시를 보고 **가게 벽의 스위치들을 켜고 끈다.**

**묶음 6 — 칸 만들기와 열쇠 (`+0x774 ~ +0x9e4`, 8곳)**

- 임계 구역 디버그 정보 목록, **PEB 의 TLS 비트맵**(PEB `+0x78`/`+0x238`)·FLS 자리.
  - TLS 비트맵은 나중에 TLS 를 쓸 때 바탕이 되는 칸이다 (Q16 으로 넘김).
- 스택 추적 DB 옵션 (IFEO 값 `StackTraceDatabaseSizeInMb`).
- **`FastPebLock`** 만들어 PEB `+0x38` 에 연결 (`RtlInitializeCriticalSectionEx`).
  - **새 말 — 임계 구역**(critical section): 유저 쪽에서 쓰는 [잠금](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)의 한 종류로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: **안내판 열쇠.** 여러 직원이 안내판을 동시에 고치지 않게, 이 열쇠를 든 사람만 고친다. 이름이 "PEB 잠금" 이니 안내판용 열쇠로 보인다 `[추측]`.

**묶음 7 — 창고 만들기 (`+0x9e5 ~ +0xb7d`, 13곳)**

- **프로세스 힙 만들기**: 힙 관리자 → 힙 정보 → `LdrpInitializeProcessHeap`. 결과가 PEB `+0x30` `ProcessHeap` (2.3 줄 1).
- 이어서: 기능 스위치 2개, 로더 스냅 버퍼, 활성화 컨텍스트 스택, `LdrpHeap` = PEB 힙, 시스템 정보 조회(클래스 0xc5), 태그 힙.
- 비유: **가게 창고를 짓고**, 관리실도 "내 물건은 이 창고에" 라고 정한다 (`LdrpHeap` = 같은 힙).

**묶음 8 — 일꾼 대기실과 기본 서류 (`+0xb83 ~ +0xc41`, 11곳, 잡일 0)**

- 스레드 풀 패키지 초기화 (`TpInitializePackage`). 이름으로 보면 일꾼 스레드들을 쓰는 장치 준비다 `[추측]`.
  - flow.md 3장에 "일꾼은 이 뒤(뒤쪽 `+0x12de` 준비 뒤)에 만들어진다" 는 `[추측]` 이 붙었다. 만드는 줄은 아직 안 봤다.
- ETW 공급자 등록 3곳 ([ETW](run12-insert-thread-해설.md#212-그-밖에-확인된-것) = 사건 기록 장치 `[추측]`), 힙 로깅.
- **환경 블록·매개변수 블록 준비** (`RtlpInitEnvironmentBlock` `+0xc32`, `RtlpInitParameterBlock` `+0xc3f`).
- 비유: 일꾼 **대기실**을 마련하고, 가게 운영 **기본 서류철**(환경·매개변수)을 정리한다.

**묶음 9 — 동네 공용 창고 위치 적기 (`+0xc47 ~ +0x1168`, 29곳)**

- 위에서 한 줄 끝까지 읽은 그 묶음이다.
- 디버그 페이지 힙 옵션 (IFEO 값 `DebugProcessHeapOnly`).
- **시스템 경로 문자열**: `<NtSystemRoot>\System32` → `LdrpSystem32`, `RtlpSystem32Dirs`.
- **`\KnownDlls` 디렉터리 열기 → `KnownDllPath` 링크 열기·읽기** (보호 프로세스가 아닐 때, `+0xd57`).
  - **새 말 — `\KnownDlls`**: 자주 쓰는 시스템 DLL 들을 미리 준비해 두는 커널 쪽 **이름 디렉터리**로 알려져 있다 `[추측]` (일반적으로 알려진 설명). `KnownDllPath` 는 그 안의 **링크**(심볼릭 링크) 하나이고, 코드는 거기서 경로 문자열을 읽는다 `[정적]`.
  - 비유: **동네 공용 창고**. 자주 쓰는 건물 세트를 동네가 미리 보관해 둔다. 이 묶음은 창고 **문패와 주소만** 적어 둔다. 안의 물건은 안 꺼낸다 (2.8).
- DLL 정책 (`LdrpInitializePolicy`), 대체 forwarder 경로.

**묶음 10 — 명부 첫 줄 쓰기 (`+0x1169 ~ +0x12dd`, 4곳, 잡일 0)**

- exe 폴더 종류 검사, exe 폴더를 잘라 `[RSP+0x130]` 에 두기 (호출 없음).
- **ntdll 모듈 항목 만들기** (2.3 줄 2), (있으면) 핫패치 ntdll 항목, 히스토리 표.
- ntdll 항목을 **초기화 순서 목록**에 연결 (호출 없음) — 2.7.
- 비유: 펴 둔 건물 명부 **첫 줄에 관리실 자기 이름**을 쓴다. 가게 본 건물(exe) 이름은 **다음 장(뒤쪽)** 에서 쓴다.

#### 비유로 다시 한 번 — 앞쪽 10묶음

1. 책상 비우고 안내판 쥐기
2. **건물 명부 펴서 안내판에 걸기**, 언어 설정
3. 본 건물 설계도 보기, 관리실을 지번 색인에
4. **특별 지시 읽기**
5. 스위치 켜고 끄기
6. 칸 만들기, 안내판 열쇠
7. **창고 짓기**
8. 일꾼 대기실, 기본 서류철
9. 시스템 폴더 경로, **공용 창고 위치 적기**
10. **명부 첫 줄에 관리실 이름**

→ 전부 **안에서 쓸 것을 준비하는** 일이다. 바깥에서 건물을 들여오는 줄은 없다. 이게 "살림 차리기" 다.

### 2.5 입구의 두 짐 — `RDX` 는 쓰고, `RCX` 는 보관만

run16 에서 커널이 직원에게 쥐여 준 두 짐이 있었다: `Rcx` = 사물함에 맡긴 원래 메모, `Rdx` = "ntdll 건물 번지" 쯤. run17 은 둘 다 이 함수까지 그대로 온다는 것까지 봤다. 이번엔 **이 함수가 짐을 풀어 무엇에 쓰나**를 봤다.

**`RDX` — 단계별로** `[정적]`

1. 받기: `+0x2d MOV RSI,RDX` (손에 쥠), `+0x30 MOV [RSP+0xd8],RDX` (책상 서랍에 하나 더).
2. 처음 쓰기: **`+0x3f9 MOV RDX,RSI`** → `+0x400 RtlImageNtHeaderEx` 에 넘겨 **이 주소의 이미지 헤더를 찾는다.**
   - 곧이어 `+0x415 RtlInsertInvertedFunctionTable(RSI, SizeOfImage)` — 묶음 3 의 "ntdll 을 역함수 표에" 가 바로 이 값이다.
3. 전역에 적기: `+0x684` → `+0x68c` 에서 서랍 값을 전역 **`LdrpSystemDllBase`** 에 저장.
4. ntdll 항목 만들 때: `+0x1195` 에서 서랍 값을 꺼내 `+0x119d LdrpInitializeNtdllDataTableEntry` 의 **첫째 인자**로.

- 그래서: 이 값으로 **헤더를 찾고**, "시스템 DLL 시작" 이라는 이름의 전역에 넣고, **ntdll 모듈 항목**을 만든다. 세 쓰임이 다 "ntdll 이미지 시작 주소" 를 가리킨다.
- 결론: 본문은 `RDX` 를 **ntdll 이미지 시작 주소로 쓴다** `[정적]`. run16·17 의 `[추측: ntdll 주소]` 를 **코드의 쓰임**으로 뒷받침했다.
- 아직 확인 아님: 실제 값이 `lm m ntdll` 의 시작 주소와 **같은지**는 버스를 타 봐야 안다 → **Q44** 1번.
- 비유: "ntdll 건물 번지" 라고 짐작했던 쪽지를, 직원이 실제로 **관리실 건물 설계도 찾기·지번 색인·명부 첫 줄**에 쓰고 있었다. 쪽지 내용은 아직 직접 펴 보지 않았다.

**`RCX` — 앞쪽에선 안 쓴다** `[정적]`

- `+0x38 MOV [RSP+0xe0],RCX` 로 서랍에 넣기만 한다.
- 앞쪽 `+0x0 ~ +0x12dd` 에서 그 서랍을 여는 줄은 없다. **처음 읽는 곳은 뒤쪽 `+0x17f4`** (Q9b).

### 2.6 PEB.Ldr — 건물 명부가 생기는 순간 (`+0x1c1`)

**새 말 — 모듈 목록 (PEB.Ldr)**

- 이 프로세스에 올라와 있는 모듈(exe, DLL)들을 줄줄이 이어 둔 목록으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- PEB `+0x18` 칸이 그 목록 정보 묶음(`_PEB_LDR_DATA`, PDB 타입 이름)을 가리킨다 `[정적]`.
- 비유: 가게의 **건물 명부**. 안내판 `+0x18` 칸에 "명부는 저기" 라고 적혀 있다.
- 명부의 **한 줄** = **모듈 항목** (모듈 하나의 정보). 2.7 에서 ntdll 항목이 첫 줄로 들어온다.

**명부 공책의 모양** `[정적]`

| 자리 (`PebLdr` 안) | 내용 |
|---|---|
| `Length` | `0x58` |
| `Initialized` | `1` |
| `+0x10` | 목록 머리 ① **적재 순서** |
| `+0x20` | 목록 머리 ② **메모리 순서** |
| `+0x30` | 목록 머리 ③ **초기화 순서** |

- 같은 건물들을 **세 가지 순서로** 줄 세운 줄이 셋 있다는 뜻이다. 명부 한 권에 색인이 세 개 달린 셈이다.
- 셋 다 처음엔 **"자기를 가리키는 빈 목록"** 이다 `[정적]`.
  - 왜 "자기를 가리키면 빈 목록" 인가: 이런 목록은 줄마다 "**앞 칸 / 뒤 칸**" 을 가리키는 손을 갖고 둥글게 이어진다. 머리 혼자 있으면 앞도 뒤도 자기 자신이다. 그게 "아무도 없음" 표시다 `[추측]` (일반적으로 알려진 설명).
  - 비유: [run11 의 상가 명부](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다) 처럼 "내 앞 가게, 내 뒤 가게" 를 적는 방식. 첫 장에 이름이 하나도 없으면 머리는 "앞도 나, 뒤도 나" 라고 적어 둔다.

**순서대로** `[정적]`

1. `+0x158 ~ +0x1b7`: ntdll 안의 전역 **`PebLdr`** (rva `0x1d2880`) 를 위 모양으로 채운다.
2. **`+0x1c1`**: `MOV [RDI+0x18], &PebLdr` — PEB(`RDI`) `+0x18` 칸에 그 주소를 건다. **여기서 PEB.Ldr 가 생긴다.**
3. 이 순간 세 목록은 **전부 비어 있다.**

**여기서 짚을 것 둘**

- **커널이 아니라 ntdll 이 채운다.** run10 에서 커널 `MmCreatePeb` 가 안내판을 만들었지만, 명부 칸은 관리실(ntdll)이 출근해서 직접 건다 `[정적]`.
- **PEB 는 ntdll 안의 전역을 가리킨다.** 명부 공책 자체가 관리실 건물(ntdll) 안에 있고, 안내판엔 그 위치만 적혀 있다 `[정적]`.

### 2.7 명부 첫 줄 — ntdll 항목, 그리고 "두 번 부르는" 이유

묶음 10 이다. `LdrpInitializeNtdllDataTableEntry` 가 **두 번** 나온다 (`+0x119d`, `+0x126a`). 왜 두 번인지 단계별로.

**첫째 호출 — 조건 없음** `[정적]`

- `+0x119d / +0x11a2`. 넘기는 것은 2.3 줄 2 에 적었다 (ntdll 주소, `&LdrpNtDllDataTableEntry`, System32 문자열).
- 모든 프로세스가 탄다. 이게 **명부 첫 줄 = ntdll** 이다.

**둘째 호출 — 핫패치 컨텍스트가 있을 때만**

1. 근거 `[정적]`: `+0x1233` 에서 `LdrpNtdllHotPatchContext` 를 읽는다. 그 포인터가 가리키는 칸 `[ctx+0]` 이 **0 이 아닐 때만** `+0x126a / +0x126f` 둘째 호출로 간다.
   - 둘째 호출 인자: 첫째 인자 = `[ctx+0]`, R8 = 첫째 호출이 만든 ntdll 항목. 실패면 `+0x1271 JS` 로 끝.
2. 근거 `[정적]` (run17): `LdrpNtdllHotPatchContext` 는 `LdrpInitializeInternal` 의 지역 칸(0 으로 비움)을 가리킨다. 그 칸을 채우는 건 **핫패치가 켜졌을 때만** 부르는 `LdrpLoadPatchedNtdll` 이다.
3. 그래서: 핫패치가 꺼진 보통 프로세스는 칸이 0 → **한 번만**. 켜진 프로세스는 "**패치된 ntdll**" 항목을 하나 더 `[추측]`.
4. 아직 확인 아님: test.exe 때 칸 값 → **Q44** 6번. 둘째 호출이 정확히 무엇을 만드는지 → **Q45** (그 함수 본문).

- 비유: 명부 첫 줄에 관리실 이름을 쓴다. 만약 오늘 **[선반 하나 갈아 끼운](run17-ldr-init-entry-해설.md#27-같은-모양이-한-번-더--ldrpinitializeinternal-의-핫패치-깃발) 새 관리실**이 따로 와 있으면, 그 이름도 둘째 줄에 쓴다.

**초기화 순서 목록에 끼우기 — 호출 없이 직접** `[정적]`

- `+0x1283 ~ +0x12d7`: ntdll 항목의 `+0x20` 칸을 **초기화 순서 목록**(`PebLdr+0x30`) 맨 앞에 끼운다.
- 끼우기 전에 연결이 맞는지 검사한다. 어긋나 있으면 `__fastfail(3)` (2.10).
- 둘째 항목이 있으면 그것도 끼운다. 없으면 `+0x12bc JZ` 로 바로 경계 `+0x12de` 로 간다.

**나머지 두 목록(적재·메모리 순서)은?**

- 이 구간엔 그 두 칸(`+0x10`, `+0x20`)에 넣는 줄이 **없다** `[정적]`.
- `refs PebLdr` 로 보면 그 두 칸을 쓰는 건 `LdrpInsertDataTableEntry` 뿐이고, 지도상 `LdrpInitializeNtdllDataTableEntry +0xf2` 가 그것을 부른다 `[정적]`.
- 그래서 **첫째 호출 안에서** 넣는 것으로 보인다 `[추측]`. 그 함수 본문은 안 읽었다 → **Q45**.

**exe 항목은?** `[정적]`

- 이 구간에서 **안 만든다.** 뒤쪽 `+0x1359 LdrpAllocateModuleEntry` → `LdrpImageEntry` 에 저장 (`refs` 로 본 첫 쓰기).
- 앞쪽에서 exe 는 PEB 의 `ImageBaseAddress`(`[RDI+0x10]`)로 **헤더만 읽는다** (`+0x35e`, `+0x627`, `+0x662`, `+0x6ef`).
- 그러니 **앞쪽이 끝날 무렵 명부에 있는 건 ntdll 뿐**이다 (핫패치 때 패치 ntdll 하나 더 `[추측]`).
- 비유: 본 건물(exe)은 이미 땅 위에 서 있다(커널이 놓아 줌, run10). 설계도도 펴 봤다. 하지만 **명부에 이름을 쓰는 건 다음 장**이다.

### 2.8 "아무것도 메모리에 올리지 않는다" — 왜 그렇게 말할 수 있나

이번 바퀴에서 사용자에게 제일 짚어 주고 싶은 결론이다. 단계별로.

**근거 1 — 직접 호출에 없다** `[정적]`

- `+0x0 ~ +0x12dd` 직접 호출 94곳에 아래가 **하나도 없다** (`_calls.tsv`):
  - `NtMapViewOfSection`, `NtCreateSection`, `NtOpenSection`, `LdrpMapDll*`, `LdrpMinimalMapModule`
- 이 이름들은 "섹션을 만들거나 열고, 그걸 주소 공간에 올리는" 쪽 이름들이다. run10 의 [매핑 올리기](run10-allocate-process-해설.md#매핑-올리기)(건물 세트를 땅에 갖다 놓기) 와 같은 종류다.

**근거 2 — 한 단계 아래에도 없다** `[정적]`

- 지도로 본 앞쪽 callee 61개의 한 단계 아래에도 섹션 만들기·올리기 호출이 없다 (결과서).
- 한계: 지도가 **공용 도구 함수 안까지는 안 펼친다.** 그 안은 못 봤다.

**헷갈리기 쉬운 것 — 비슷해 보이지만 올리기가 아님** `[정적]`

| 자리 | 하는 일 | 왜 올리기가 아닌가 |
|---|---|---|
| `+0xdb8`·`+0xe47`·`+0xe90`·`+0xf04` | `\KnownDlls` 디렉터리 열기, `KnownDllPath` 링크 열기·읽기, 닫기 | **경로 문자열만** 읽는다. 공용 창고 안의 섹션을 여는 호출은 없다 |
| `+0x627` | `LdrpCorFixupImage` (.NET 보정) | `PE32(0x10b)` 헤더 + `TEB.WowTebOffset <= 0` 일 때만 (`+0x602 ~ +0x611`). x64 exe(`PE32+`)면 건너뛴다 |

- 비유: 공용 창고 **문패를 확인하고 주소를 수첩에 적는** 것과, 창고에서 **건물 세트를 꺼내 내 땅에 놓는** 것은 다른 일이다. 앞쪽은 앞의 것만 한다.

**그래서** — 태그를 나눠서

- 앞쪽은 DLL·이미지를 하나도 메모리에 올리지 않는다 → `[정적]`.
- flow.md 2.5 의 `[추측]` "이 함수가 시작될 때는 exe·ntdll 만 올라와 있다" 중 **"이 함수 앞쪽에선 더 안 올린다"** 까지를 뒷받침한다.
- 그러니 kernel32·dll1·dll2 를 올리는 일은 **뒤쪽** (`+0x12de` 뒤, `+0x1bb6` 전) 이다 → `[추측]` (flow.md 2.5·2.6). 이 스레드가 직접 올리는지, 병렬 로딩 일꾼이 올리는지는 모른다 (Q9b·Q10).
- 예외 하나 `[추측]`: 핫패치가 켜진 프로세스는 이 함수 **앞**(`LdrpInitializeInternal +0xc2 LdrpLoadPatchedNtdll`, run17)에서 이미지를 하나 더 올렸을 수 있다.

**비유로 정리**

- run10: 관리사무소가 test.exe 건물과 관리실까지만 놓아 줬다.
- run18 앞쪽: 관리실 직원이 출근해서 **가게 안 살림을 차렸다.** 명부·창고·열쇠·서류·공용 창고 주소.
- 나머지 건물(kernel32, dll1, dll2)을 **들여오는 건 아직**이다. 그건 뒤쪽에서.

### 2.9 run17 이 넘긴 연결점 네 개

run17 이 "이 함수 안에서 보자" 하고 넘긴 자리들이다.

**① `+0x587` — 핫패치 끄기** `[정적]`

- `+0x57f` 에서 `LdrpImageExpansionMitigation == 2` 일 때만, `+0x587` 에서 `LdrpIsHotPatchingEnabled = 0`.
- 그 값(`LdrpImageExpansionMitigation`)은 어디서 오나: `LdrpInitializeExecutionOptions +0x4d0` 이 IFEO 값 `ImageExpansionMitigation` 을 읽어서 쓴다 (읽기 실패면 안 씀 = 0). `refs` 로 보면 쓰기 한 곳, 읽기 한 곳.
- 즉 **특별 지시(묶음 4)가 "2" 라고 적혀 있으면 핫패치 스위치를 끈다.**
- IFEO 에 그 값이 없으면 이 줄은 안 탄다 `[추측]`.
- run17 해설 2.7 에서 본 "핫패치 켜고 끄기" 는 이 함수 **전**(`LdrpInitializeHotPatching`)에 정해졌다. 여기는 그걸 **한 번 더 끌 수 있는** 자리다.

**② `+0x5f2` — `UseWOW64` 정하기** `[정적]`

- `+0x5e3` 에서 `TEB+0x180c` (PDB 이름 `_TEB.WowTebOffset`) 를 읽고 → **`UseWOW64 = (WowTebOffset > 0)`**.
- 이 전역을 **쓰는 곳은 여기 하나**다 (`refs UseWOW64`).
- 앞쪽에서 6곳(`+0x63b`, `+0x696`, `+0x6d5`, `+0x875`, `+0xbd0`, `+0xf0c`)이 이 값으로 갈린다.
- run17 해설 2.9 의 `_LdrpInitialize +0x9a` 간접 호출(`UseWOW64` 면 탐)의 **갈림 값이 여기서 온다.**
- x64 test.exe 면 0 `[추측]`. [WoW64](run17-ldr-init-entry-해설.md#29-나중-스레드-길--짧게) = 64비트 Windows 에서 32비트 프로그램을 돌리는 장치라고 알려져 있다 `[추측]`.

**③ `+0x1233` — 핫패치 컨텍스트 읽기** `[정적]`

- 2.7 의 "둘째 호출" 갈림이 이것이다. 하나 더:
  - `[ctx+8] < 0` 이고 핫패치가 켜져 있으면 `__fastfail(0x45)` (2.10).
  - `[ctx+8]` = `LdrpLoadPatchedNtdll` 의 반환값으로 보인다 `[추측]` ("패치 ntdll 올리기 결과").
- 뒤쪽 `+0x18ef` 에서 한 번 더 읽는다 (Q9b).

**④ `+0x153` — `LdrInitializePerThreadRng(TEB)`** `[정적]`

- 조건 없이 부른다.
- `_calls.tsv` 상 이걸 부르는 곳은 여기와 `_LdrpInitialize +0x83`(나중 스레드) **둘뿐**. run17 결과와 맞다.
- 즉 첫 스레드는 여기서, 나중 스레드는 `_LdrpInitialize` 에서 부른다.

### 2.10 일찍 끝나는 길 — 19 + 5

정상이면 안 타는 길이지만, "어디서 실패할 수 있나" 를 알아 두면 흐름이 더 잘 보인다.

**새 말 둘**

- **에필로그** = 함수 끝에서 빌린 스택을 돌려놓고 `ret` 하는 **마무리 구간**. 여러 갈림길이 같은 마무리로 모인다.
  - 비유: 가게 **뒷문**. 어디서 일을 접든 여기로 나간다.
- **`__fastfail`** = 그 자리에서 **프로세스를 바로 끝내는** 장치라고 알려져 있다. 보통 "이러면 안 되는데" 하는 상태를 봤을 때 쓴다 `[추측]` (일반적으로 알려진 설명).
  - 비유: **비상 정지 버튼.** 뒷문으로 정리하고 나가는 게 아니라 그 자리에서 멈춘다.

**상태값을 돌려주고 끝나는 길 — 19개** `[정적]`

- 모두 같은 에필로그 `+0xa7b ~ +0xaab` 로 간다.
- 대표 셋:
  1. `+0x37f ~ +0x3b3`: 보안 프로세스(`LdrpIsSecureProcess != 0`)인데 exe 헤더에 "큰 주소 인식"(`Characteristics & 0x20`)이 없음 → 로그 `Secure processes must be large address aware` 후 `0xc00000bb`.
  2. `+0x53c`: `LdrpInitializeExecutionOptions`(특별 지시 읽기) 실패 → 로그 후 그 상태.
  3. `+0xa55`: `LdrpInitializeProcessHeap`(창고 짓기) 실패 → 로그 후 `0xc0000017`.
- 나머지 16개 자리는 run 문서에 목록이 있다. 대부분 "바로 앞 호출이 실패하면 끝" 모양이다 (결과서).
- 돌아간 뒤: `_LdrpInitialize +0x172 JS` → 실패 길 (run17). run17 해설 2.8 에서 본 "세팅이 실패하면 가게 문을 닫는다" 로 이어진다.

**강제 종료 — 5개** (함수 끝 조각 `+0x1e34`, `+0x1e3b`) `[정적]`

| 자리 | 가는 곳 | 조건 |
|---|---|---|
| `+0x784`, `+0xb22` | `RtlRaiseStatus(0x80000002)` | 상수 주소 정렬 검사. 평소엔 안 탈 것 `[추측]` |
| `+0x1299`, `+0x12c6` | `__fastfail(3)` | 초기화 순서 목록에 끼우기 전 연결 검사 실패 (2.7) |
| `+0x1249 ~ +0x124e` | `__fastfail(0x45)` | `[ctx+8] < 0` 이고 핫패치 켜짐 (2.9 ③) |

- 뒤쪽으로 바로 뛰는 갈림길은 `+0x12bc JZ → +0x12de` (둘째 항목 없으면 경계로) **하나뿐**이다 `[정적]`.
- test.exe 는 정상 흐름이라 이 길들은 안 탈 것이다 `[추측]` → Q44 8번 (`+0xa7b` 에 안 서야 정상).

### 2.11 디버거 때문에 갈리는 길 (CLAUDE.md "따로 적기")

우리가 커널 디버거를 붙여 돌리니, "디버거가 있어서 길이 달라지나" 는 늘 따로 적는다.

**앞쪽은 이 둘을 안 읽는다** `[정적]`

- `PEB.BeingDebugged` (PEB `+0x2`) — 앞쪽에서 안 읽는다. 읽는 곳은 뒤쪽 `+0x17d2`, `+0x1b35` (Q9b).
- `KUSER_SHARED_DATA` — 앞쪽에서 안 읽는다. 뒤쪽 `+0x188c`, `+0x1cb0` 의 `0x7ffe0330` (Q9b).
  - **새 말 — `KUSER_SHARED_DATA`**: 커널이 모든 프로세스에 **읽기 전용으로 보여 주는 공용 정보 페이지**로 알려져 있다. 커널 디버거가 켜져 있는지(`KdDebuggerEnabled`)도 여기 있다고 한다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소가 동네 모든 가게 앞에 붙여 두는 **공용 게시판**.
  - 그래서 앞쪽에선 "커널 디버거가 붙었나" 를 보는 줄이 없다 `[정적]`.

**대신 `NtGlobalFlag` 는 읽는다** `[정적]` (비트 이름은 공개 문서 기준 `[추측]`)

- **새 말 — `NtGlobalFlag`** (PEB `+0xbc`): 디버깅·진단용 기능을 켜는 **깃발 묶음**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비트 하나하나가 스위치다 ([비트와 플래그](run9-create-user-process-해설.md#비트와-플래그)).
- 앞쪽이 보는 비트:

| 비트 | 이름 `[추측]` | 자리 | 켜져 있으면 |
|---|---|---|---|
| `& 0x2` | 로더 스냅 보이기 | `+0x58e`, LoadConfig 반영 뒤 `+0x74b` 한 번 더 | `LdrpDebugFlags` 에 1 켜기 |
| `& 0x1000` | 사용자 스택 추적 DB | `+0x8a3`, `+0x989` | `+0x979 RtlControlStackTraceDataBase` |
| `& 0x800000` | 예외 로그 | `+0xaf4` | `+0xb00 RtlInitializeExceptionLog` |

- `+0x58e` 와 `+0x74b` 는 같은 검사를 두 번 한다. 사이에 exe 의 LoadConfig 값을 PEB `+0xbc` 에 반영(`+0x70d`, `+0x71d`)하기 때문이다 (결과서: 앞은 반영 전, 뒤는 반영 후).

**IFEO 로 갈리는 것** `[정적]`

- 디버그 페이지 힙(`+0xcae DebugProcessHeapOnly`), `BreakOnInitializeProcessFailure`(`LdrpInitializeExecutionOptions +0x462`) 는 **IFEO 값**에 따른다. 디버거 부착을 직접 보지 않는다.

**그래서**

- 앞쪽에서 디버거 부착 여부로 **직접** 갈리는 길은 없다. `NtGlobalFlag`·IFEO 로만 갈린다 `[정적]`.
- `NtGlobalFlag` 값은 커널이 PEB 를 만들 때(flow.md 2.1 `MmCreatePeb`) 넣는 값이라, 이 구간 코드로는 정할 수 없다.
- 커널 디버거만 붙어서는 안 바뀔 것 `[추측]` → Q44 3번 (`+0x58e` 에서 `dd @rdi+0xbc L1`).

### 2.12 정정 두 개

**1. 호출 개수** — 2.1 에서 봤다. 앞쪽 95 → **94**, 뒤쪽 74 → **75**. 요청서는 그대로 두고, run 문서·flow.md 에 실제 수를 쓴다.

**2. flow.md 의 `+0x1bb6` 라벨 → `+0x1bbb`**

- 이 프로젝트의 오프셋 읽는 법: 라벨은 **돌아올 자리(ret)** = 콜스택에 찍히는 숫자.
- 그런데 flow.md 그림 2 `(2) +0x1bb6`, 2.4·2.7 트리의 `+0x1bb6` 은 **부르는 자리(call)** 였다.

| | call 자리 | ret 자리 | 근거 |
|---|---|---|---|
| `LdrpInitializeProcess` → `LdrpInitializeGraphRecurse` | `+0x1bb6` | **`+0x1bbb`** | run7 콜스택 `LdrpInitializeProcess+0x1bbb` `[관찰]`, `_calls.tsv` `+0x1bb6 → ret +0x1bbb` `[정적]` |

- 어떻게 섞였나: run8 이 멈춤점을 `+0x1bb6`(call 자리, `ub +0x1bbb` 로 확인)에 걸었는데, 그 숫자가 라벨로 옮겨진 것으로 보인다 `[추측]`.
- 고친 것: 그림 2·2.4·2.7 의 라벨을 `+0x1bbb` 로. 2.6 의 "`+0x1bb6` 에서 `lm`" 은 **멈춘 자리**라 그대로 두고 "(call 자리)" 를 붙였다.
- 01_sprint 의 run7·run8 문서는 각자 맞게 썼으니 안 고쳤다.
- 이 일로 하네스 후보 (18) 이 `plan.md` 에 올랐다: "라벨·멈춤점 오프셋이 call 자리인지 ret 자리인지 섞지 않는다".

### 2.13 그 밖에 확인된 것

- **이름 확인** `[정적]` — `pdb_aliases.py --where` 로 `LdrpInitializeProcess` = rva `0x8c404`, 이름 **하나뿐**. run17 의 `LdrpInitialize` / `_LdrpInitialize` 같은 겹침은 없다.
- **버전 일치** `[정적]` — `_calls.tsv` 의 `+0x1bb6 → ret +0x1bbb LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167 → ret +0x16c LdrpInitializeProcess` 가 run7 콜스택과 같다.
- **이름 주의 — 같은 주소에 이름 둘** ([ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 과 비슷한 이야기) `[정적]`:
  - `+0x7c4` (ret `+0x7c9`): Ghidra 는 내보내기 이름 `RtlInterlockedPushListSList`, PDB 이름은 `InterlockedPushListSList`. 잠금 없는 목록에 한 묶음 넣기.
  - `+0xb4d` (ret `+0xb52`): PDB `NtQuerySystemInformation`·`ZwQuerySystemInformation`, 내보내기 `RtlGetNativeSystemInformation`. 시스템 정보 묻는 시스템 콜 (클래스 0xc5, 8바이트).
  - 둘 다 별칭 규칙대로라 도구 문제는 아니다 (결과서).
- **범위를 딱 한 곳 넓혔다** — `LdrpInitializeExecutionOptions` depth 0. 2.9 ① "`+0x587` 조건 값이 어디서 오나" 를 보려고. 그 함수 본문은 그 한 곳만 읽었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run17 뒤) | 후 (run18) |
|---|---|---|
| 기준 | ntdll.pdb (run17), 정적 근거 run9 ~ run17 | ntdll.pdb (run17, **run18**), 정적 근거 **run9 ~ run18** |
| 두 그림 사이 설명 | — | 문단 추가: `LdrpInitializeProcess` **앞쪽 절반**도 코드로 훑었다. 그림 2 의 `(0) 프로세스 준비` 상자가 이것. 앞쪽은 DLL 을 하나도 안 올린다 |
| 그림 읽는 법 | ntdll 상자 안 = `(1)` `(2)` `(3)` | `(0)` `(1)` `(2)` `(3)`. **상자 번호는 단계 이름이고, 화살표 `(n)` 호출 순서와는 따로** |
| 그림 2 `c4` 상자 | `ntdll!LdrpInitializeProcess` | + "(정적: +0x1c1 에서 PEB.Ldr = 전역 PebLdr, 빈 목록)" |
| 그림 2 새 상자 | — | **`(0) 프로세스 준비`** 묶음 상자 안 정적 상자 5개: `LdrpInitializeExecutionOptions` / `LdrpInitializeProcessHeap` / `NtOpenDirectoryObject` (`\KnownDlls`) / `LdrpInitializeNtdllDataTableEntry` / 같은 함수 둘째 호출 (조건부) |
| 그림 2 화살표 | `(1) +0x19a2`, `(2) +0x1bb6`, `(3) +0x1c99` | 새 `(1) +0x536` ~ `(5) +0x126f`, 그 뒤 **`(6) +0x19a2`, `(7) +0x1bbb`, `(8) +0x1c99`** (번호 밀림 + 라벨 정정) |
| 2.4 `LdrInitializeThunk` 의 `Rdx` | `[추측: ntdll 주소]` | "본문이 ntdll 시작 주소로 씀 `[정적]` run18". "본문에서 무엇에 쓰나는 Q9" 는 **취소선** → 헤더 찾기·`LdrpSystemDllBase`·ntdll 항목 |
| 2.4 `LdrpInitializeProcess` 절 | `[관찰]` 3줄 + `[추측]` 1줄 | 제목에 rva·7750바이트·169곳. `[관찰]` 3줄이 **"셋 다 뒤쪽"**. `+0x1bb6` → `+0x1bbb` (call `+0x1bb6`). 새로: 앞쪽 트리, 10묶음 표, `[정적]` 줄 여러 개 (입구 인자, PEB.Ldr, 모듈 목록 = ntdll 뿐, 안 올림, 전역 갈림 값, 일찍 끝나는 길, 디버거 길) |
| 2.4 `_LdrpInitialize` 디버거 길 | "`LdrpInitializeProcess` 안의 같은 호출 2곳은 Q9" | "뒤쪽이라 **Q9b**. 앞쪽은 `BeingDebugged` 를 안 읽는다 `[정적]`" |
| 2.5 | `[정적]` (run10) 커널이 exe·ntdll 올리는 코드 | + `[정적]` (run18) 앞쪽 직접 호출과 한 단계 아래에 올리는 호출 없음. `[추측]` kernel32 는 뒤쪽에서 |
| 2.6 | `[관찰]` `+0x1bb6` 에서 dll1·dll2 이미 올라와 있음 | "(call 자리 — run8 멈춤점)" 붙임 + `[정적]`/`[추측]` 한 줄: 올리는 일은 `+0x12de` 뒤 `+0x1bb6` 전 |
| 2.7 트리 | `(2) +0x1bb6 LdrpInitializeGraphRecurse` | `(2) +0x1bbb` (call `+0x1bb6`, 라벨 정정 run18) |
| 3장 | — | `[정적]` 한 줄: 앞쪽 `+0xb83` 스레드 풀 준비, 뒤쪽 첫 호출 `+0x12de` 병렬 로딩 준비. `[추측]` 일꾼은 그 뒤에 |
| 4장 공백 표 | "`RDX` 본문에서 쓰는 법은 Q9" 등 | `RDX` 줄 **해결**. 새 줄: 이 함수 순서 (일부 해결, 뒤쪽 남음), 앞쪽 실행 값 (Q44), `LdrpInitializeNtdllDataTableEntry` 본문 (Q45), 이름·뜻 못 정한 것, `+0x1bbb` 라벨 정정 (해결됨). 2.5 "exe·ntdll 뿐" 줄에 "(일부) 앞쪽은 안 올림" |
| 4.1 | — | **"그림 2 (0) 프로세스 준비"** 줄: `ntdll+0x8c935`·`+0x8ce4f`·`+0x8d1bc`·`+0x8d5a1` 에 멈춰 서는 순서 확인, `ntdll+0x8d66e` 에 안 서는지 |
| 5장 | run17 까지 | run18 줄, 요청서 번호 표 run18 줄 |

태그로 보면 핵심 변화는 넷이다:

- **`LdrpInitializeProcess` 가 하는 일: `[추측]` "준비 총괄" → 앞쪽 절반 `[정적]`** (10묶음). 뒤쪽은 그대로 `[관찰]` 3점 + `[공백]`.
- **커널 `RDX` 의 뜻: `[추측: ntdll 주소]` → 쓰임 `[정적]`** (헤더·`LdrpSystemDllBase`·ntdll 항목). 실제 값은 여전히 동적.
- **PEB.Ldr 생기는 곳: 처음 다룸 → `[정적]`** (`+0x1c1`, ntdll 이 채움).
- **2.5 "exe·ntdll 만 올라와 있다": `[추측]` 에 `[정적]` 근거 일부** ("앞쪽에선 더 안 올린다" 까지).

그림 2 를 볼 때 하나만 주의:

- 상자 `(1) kernel32 준비` 로 가는 화살표가 이제 `(6) +0x19a2` 다. **상자 번호(단계 이름)와 화살표 번호(호출 순서)가 따로** 놀기 때문이다 (flow.md "그림 읽는 법"). 상자 `(0)` 이 새로 들어오면서 화살표만 다섯 칸 밀렸다.

### 큰 그림에서 어떻게 이어지나

- run17 에서 칠판을 0 → 1 로 고친 직원이 **가게 전체 세팅 방**에 들어갔다. 이번엔 그 방의 **앞쪽 절반**을 봤다.
  - 결과: 명부를 펴고(PEB.Ldr), 특별 지시를 읽고(IFEO), 창고를 짓고(힙), 공용 창고 주소를 적고(`\KnownDlls`), 명부 첫 줄에 관리실 이름을 썼다(ntdll 항목).
  - 그리고 **새 건물은 하나도 안 들였다.**
- 그러니 main 까지 가는 길에서 진짜 "건물 들이기" 는 **뒤쪽**에 있다.
  - `[관찰]` 된 kernel32 준비(`+0x19a2`), import DLL 초기화(`+0x1bbb`), exe TLS 콜백(`+0x1c99`) 이 다 뒤쪽이다.
  - 앞쪽이 준비해 둔 것들(`LdrpKnownDllDirectoryHandle`, `LdrpKnownDllPath`, `LdrpSystem32`, `RtlpSystem32Dirs`, `LdrpHashTable`, `LdrpHeap`, `LdrpSystemDllBase`, `LdrpAppHeaders`)을 DLL 로드 코드가 쓰는지는 Q10 에서 `refs` 로 본다.
- **다음 구간** (`plan.md` "다음 항목"): **Q9b — 같은 함수 뒤쪽 `+0x12de ~ 끝` (75곳)**.
  - 넘긴 연결점: `+0x12de LdrpInitParallelLoadingSupport`, `+0x1302 LdrpDrainWorkQueue`, `+0x1313 LdrpInitializeDllPath`, `+0x1359 LdrpAllocateModuleEntry`(exe 항목 → `LdrpImageEntry`), `RCX` 처음 쓰는 곳 `+0x17f4`, `BeingDebugged` `+0x17d2`·`+0x1b35`, `KUSER_SHARED_DATA+0x330` `+0x188c`·`+0x1cb0`, `LdrpNtdllHotPatchContext` `+0x18ef`, `NtGlobalFlag & 0x100` `+0x16b7`.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 뒤쪽 `+0x12de ~ 끝` 이 무엇을 어떤 순서로 하나 (exe 항목, 병렬 로딩, DLL 경로, kernel32, import, TLS) | 함수가 너무 커서 이번 범위를 앞쪽으로 잘랐다 | **Q9b** (정적, 다음 항목) |
| `RCX`(로더 문맥)를 무엇에 쓰나 | 앞쪽에선 저장만 한다. 처음 읽는 곳이 뒤쪽 `+0x17f4` | **Q9b** |
| 입구 `RDX` 실제 값이 `lm m ntdll` 시작 주소와 같은가 | 코드 쓰임까지만 `[정적]`. 값은 실행 때 | **Q44** 1번 (`ntdll+0x8c404` 에서 `r rcx rdx`) |
| test.exe 때 `UseWOW64` 가 0 인가, `+0x587` 을 타나 | 실행 값. x64 라 0 일 것은 `[추측]`, IFEO 값 없으면 안 탈 것도 `[추측]` | **Q44** 2번 (`ntdll+0x8c9f6`, `ntdll+0x8c98b`) |
| 커널 디버거만 붙었을 때 `NtGlobalFlag`(PEB `+0xbc`) 값 | 커널이 PEB 만들 때 넣는 값이라 이 구간 코드로 못 정함 | **Q44** 3번 (`ntdll+0x8c992` 에서 `dd @rdi+0xbc L1`) |
| 프로세스 힙·`\KnownDlls` 열기가 실제로 성공하나, `KnownDllPath`·`LdrpSystem32` 문자열 값 | 실행 값 | **Q44** 4·5번 |
| 둘째 `LdrpInitializeNtdllDataTableEntry`(`+0x126a`) 를 타나 (`[ctx]`·`[ctx+8]`) | 핫패치 켜짐 여부가 실행 값 | **Q44** 6번 (`ntdll+0x8d637`) |
| 앞쪽 끝(`+0x12de`)에서 명부 세 목록에 정말 ntdll 만 있나 | 목록 내용은 실행 때만 보인다 | **Q44** 7번 (`ntdll+0x8d6e2`, `PebLdr` rva `0x1d2880`) |
| 실패 길을 정말 안 타나 | 정상 흐름이라 안 탈 것 `[추측]` | **Q44** 8번 (`ntdll+0x8ce7f` 에 안 서야 정상) |
| 10묶음이 정말 이 순서로 실행되나 | 묶음은 오프셋 순서로 자른 것 | **Q44** / flow.md 4.1 "그림 2 (0)" 줄 (대표 4곳에 멈춰 서는 순서) |
| `LdrpInitializeNtdllDataTableEntry` 안: 적재·메모리 순서 목록에 넣는 곳(`LdrpInsertDataTableEntry` `[추측]`), 색인, 둘째 호출이 만드는 것의 정체 | callee 본문은 이번 범위 밖 (이름과 지도만 봄) | **Q45** (정적, 새 항목) |
| 앞쪽이 준비한 전역(`LdrpKnownDllDirectoryHandle` 등 8개)을 DLL 로드 코드가 쓰나 | 이번엔 쓰는 쪽(Q10 대상)을 안 봤다 | **Q10** (정적, `refs`) |
| PEB TLS 비트맵 칸(PEB `+0x78`, `+0x80`, `+0x238`, `+0x240`)을 `LdrpInitializeTls` 가 어떻게 쓰나 | 앞쪽은 칸을 걸어 두기만 한다 | **Q16** (정적) |
| 이름·뜻 못 정한 것: `NtGlobalFlag` 비트 `0x2`·`0x1000`·`0x800000` 이름, `LdrSystemDllInitBlock+0xa0` 무결성 비트 두 개, `[ctx+8]` 의 뜻, `+0x784`·`+0xb22` 정렬 검사가 평소 안 타는지 | PDB 에 이름이 없거나 공개 문서 기준이라 `[추측]` | flow.md 4장 공백 줄 (대기열 줄은 따로 없다) |

- Q44 는 `plan.md` 에 "Q43 과 같은 기준점 — 연달아 있어 한 run 으로 묶을 수 있음" 이라고 적혀 있다. 버스를 한 번 탈 때 run17·run18 의 실행 값을 같이 볼 수 있다는 뜻이다.

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run18-proc-init-setup.md`](run18-proc-init-setup.md) — 특히 "앞쪽 큰 순서", "PEB.Ldr (모듈 목록)", "메모리에 올리는 호출: 없다", "일찍 끝나는 길"(19개 자리 전부), "정정"
- 정적 요청서: [`_workspace/sast/13_proc-init-setup.md`](../../_workspace/sast/13_proc-init-setup.md)
- 정적 결과서 (질문별 답, 흐름도 상자 후보 5개, Q9b·Q10 ~ Q16 에 넘길 연결점, 추천 멈춤점): [`_workspace/sast/13_proc-init-setup_result.md`](../../_workspace/sast/13_proc-init-setup_result.md)
- 앞 바퀴 해설:
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — "부르는 자리와 돌아올 자리", "비트와 플래그", "한 주소에 이름 여럿 ICF 별칭", 3.7 오프셋 순서 ≠ 실행 순서
  - [`run10-allocate-process-해설.md`](run10-allocate-process-해설.md) — "PEB", "매핑 올리기", "ntdll 이 먼저 와 있어야 하는 이유", 3.4 IFEO
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.8 상가 명부(앞·뒤 칸으로 잇는 목록), 잠금
  - [`run16-thunk-context-해설.md`](run16-thunk-context-해설.md) — 커널이 넘기는 `Rcx`·`Rdx`
  - [`run17-ldr-init-entry-해설.md`](run17-ldr-init-entry-해설.md) — 2.7 핫패치 깃발, 2.8 첫 스레드 길, 2.9 WoW64·`UseWOW64`, 2.10 `RDX` 의 행방
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" 의 run18 문단, "그림 읽는 법" (상자 번호 ≠ 화살표 번호)
  - "그림 2" ntdll 상자 안 `(0) 프로세스 준비` (`PH0`: `p1` ~ `p5`)
  - "2.4" `LdrInitializeThunk` 절의 `Rdx` 줄, `#### ntdll!LdrpInitializeProcess` 절 (트리·10묶음 표)
  - "2.5"·"2.6" run18 줄, "2.7" 트리 라벨, "3" 스레드 풀 줄, "4. 공백 목록", "4.1" 그림 2 (0) 줄
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q9a, Q9b, Q10, Q16, Q44, Q45, 결정 기록 (Q9 나누기), 하네스 후보 (18)
