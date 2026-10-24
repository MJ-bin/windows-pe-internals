# run43 baggage-checklist — 해설

> 이 글은 [run43 문서](run43-baggage-checklist.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q76** — 짐 지도 A, 체크리스트: PDB 칸 목록 + ntdll 전역 목록 + 초기화 함수들의 callee 목록, 그리고 칸마다 이미 문서에 있는 "누가 씀" 대조 (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q77** — 짐 지도 B, 사진 (동적). 그다음 **Q78** — 짐 지도 C, 쓴 자리·줄기 (정적).
  - 이번에 나온 빈 곳은 새 번호 없이 **Q77·Q78 에 출처로** 붙었다.
- 근거:
  - run 문서 [`run43-baggage-checklist.md`](run43-baggage-checklist.md) (가장 중요)
  - 짐 지도 첫판 [`baggage.md`](baggage.md) — 이번 바퀴의 주 산출물
  - 요청서 [`_workspace/sast/38_baggage-checklist.md`](../../_workspace/sast/38_baggage-checklist.md)
  - 결과서 [`_workspace/sast/38_baggage-checklist_result.md`](../../_workspace/sast/38_baggage-checklist_result.md) (판정: **분석성공**)
  - flow.md: `runs/03_sprint/` 는 이번에 새로 생긴 폴더라 (아직 커밋 전) `git diff` 대신 `runs/02_sprint/flow.md` 와 나란히 놓고 바뀐 줄을 봤다.
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntoskrnl·ntdll 두 PDB 의 GUID 는 flow.md 기준과 같다. 두 Ghidra 프로젝트 다 다시 analyze 하지 않았다.
  - **새로 코드를 읽은 건 아주 조금이다.** `MmCreateTeb` 한 함수를 처음 뽑았고, 문서에 적힌 오프셋 몇 줄을 `.asm` 로 다시 맞춰 봤을 뿐이다.
  - 나머지 "누가 씀" 은 **이미 있는 문서**(run9 ~ run42, sast/01 ~ 37 결과서, ntdll `_refs`)에서 **옮겨 온 것**이다 (결과서 "동적분석으로 확인해야 할 것" 의 한계 줄).

이 바퀴는 **03_sprint 의 첫 바퀴**라서, 하는 일의 **종류**가 바뀐다.

- 지금까지(01·02_sprint)는 "**누가 누구를 어떤 순서로 부르나**" 를 그렸다 = 직원들의 **동선**. 결과물은 흐름도 `flow.md`.
- 이제부터(03_sprint ~)는 "**그 동선이 끝났을 때 어디에 무엇이 놓였나**" 를 센다 = **짐**. 결과물은 짐 지도 `baggage.md`.
- 이번 바퀴는 그 첫걸음이다. 짐을 하나하나 찾기 전에, **"세어야 할 것의 빈 목록"** 부터 만들고, 이미 아는 것에 체크했다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, [직접 호출과 간접 호출](../02_sprint/run9-create-user-process-해설.md#직접-호출과-간접-호출), [비트와 플래그](../02_sprint/run9-create-user-process-해설.md#비트와-플래그) (신청서의 체크 칸), [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) (손잡이) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) (**안내판**), `_EPROCESS` (관리사무소의 **가게 장부**), VAD (땅의 **구획 장부**), [남의 땅에 잠깐 들어가기](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기) (붙기·떼기), [구조체 안의 칸 오프셋](../02_sprint/run10-allocate-process-해설.md#구조체-안의-칸-오프셋) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절
- `_ETHREAD` (**직원 인사 카드**), TEB (**개인 안내판**), 장부 `_calls.tsv` → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2·2.3·2.7
- ntdll 전역 변수 = 관리실 **칠판**, `.data` 0 채움 구간 → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.5·2.6. 꼬리 점프 `⇒` → 같은 글 2.4
- 건물 **명부**(`PEB.Ldr`) → [run18 해설 2.6](../02_sprint/run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
- 모듈 항목 = **명부 한 줄**, DDAG 노드 = **연결 카드**, 노드 `State` = **진행 도장** → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.6, [run22 해설](../02_sprint/run22-map-snap-dep-해설.md)
- 트랩 프레임 = 직원 책상의 **출발 메모** → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.6. 맡긴 문맥 = **사물함 메모** → [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.6
- 프로세스 매개변수 블록 = 부모가 써 준 **개업 서류**, `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.3·2.8
- 컨트롤 영역 = 설계도 서류함의 **서랍**, 섹션 객체 = **주문 꾸러미** → [run31 해설](../02_sprint/run31-image-section-해설.md) 2.5·2.6
- `KUSER_SHARED_DATA` = 건물 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 지도 (`_map/`, "누가 누구를 부르나" 표), 이름 사슬 → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, 시스템 콜 = 관리사무소의 **신청 창구**
- 위 링크의 안내판·장부·인사 카드·칠판·명부·서류·수첩·게시판도 그대로.
- **이번에 더하는 비유**:
  - **짐** = 개업 날 아침까지 가게 안팎에 놓여 있어야 하는 **개업 준비물** 하나하나 (안내판 칸 하나, 칠판 하나, 명부 줄 한 칸 …)
  - **짐 지도** = 그 준비물의 **점검표** + "누가, 어느 길로, 어떻게 갖다 놓았나" 기록
  - PDB 의 구조체 서식 = 관리사무소가 쓰는 **빈 양식 견본집**. 칸 이름이 인쇄돼 있다.
  - **상태 사진** (Q77) = 개업 직전, 가게 안을 **사진**으로 찍어 두기
  - **줄기** (Q78) = 준비물 하나가 제자리에 놓이기까지 거쳐 간 **배달 경로**
- run 문서는 "집(프로세스)에 들어갈 가구 칸 목록을 설계도(PDB)에서 베껴 왔다" 고 비유했다.
  - 이 글은 앞 해설들과 맞추려고 **집 → 가게**, **설계도(PDB) → 양식 견본집** 으로 바꿔 부른다.
  - 앞 해설에서 **설계도 = exe 파일**이었기 때문이다 (run10·run25·run31). 둘이 섞이면 헷갈린다.

### 한 줄로 먼저 — 무엇을 몇 개 셌나

| 무엇을 셌나 | 몇 개 | 쓴 자리를 이미 아는 것 | 아직 모름 |
|---|---|---|---|
| 구조체 21개 (커널 14 + 유저 7) 의 직접 칸 | **1,456** (그중 비트 칸 330) | **181** (정확 138 · 짐작 43) | **1,275** |
| ntdll 쓰기 가능 전역 (`.data` · `.mrdata` · `.00cfg`) | **493** | **54** | **439** |
| 초기화 함수 15개가 부르는 줄 (`_calls.tsv`) | **637** (조각 점프 줄 포함) | — (아래 2.10 처럼 세 칸으로 가름) | 문서 **어디에도 없는** callee **57** |
| `MmCreateTeb` 첫 export | 호출 5줄, callee **4개** | TEB 에 직접 쓰는 대입 **약 47줄** | 그 47줄이 **어느 칸**인지는 아직 안 가름 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 바퀴는 길 위의 **한 점**이 아니다. 짐 지도 범위 **전체**(프로세스 생성 → `NtContinue` 은퇴)를 한 번에 훑었다.

```
cmd.exe 의 부탁 (CreateProcess)
 │
 ├─ 단계 ① 커널 ──────────────────────────────────────────────┐
 │   부모 스레드: NtCreateUserProcess                            │
 │     가게 장부·안내판·개업 서류·관리실 수첩                       │
 │     직원 인사 카드·개인 안내판·출발 메모                         │
 │   부모의 다음 시스템 콜: NtResumeThread (풀어 주기)              │   ← 이번 바퀴:
 │   test.exe 스레드: PspUserThreadStartup → IRETQ                │     이 구간 전체에서
 │                                                             │     "놓이는 짐" 의
 ├─ 단계 ② ntdll ──────────────────────────────────────────────│     빈 점검표를 만들고,
 │   LdrInitializeThunk → … → LdrpInitializeProcess               │     이미 아는 것에 체크
 │     건물 명부·칠판·DLL 들이기·켜기                               │
 │   NtContinue (은퇴)                                            │
 ├─────────────────────────────────────────────────────────────┘
 └─ RtlUserThreadStart → (CRT) → main      (짐 지도 범위 밖)
```

- 단계 ①·② 의 경계는 짐 지도 "쓰는 말" 그대로다: ① = `NtCreateUserProcess` ~ 유저로 넘어가기, ② = `LdrInitializeThunk` ~ `NtContinue`.

### 왜 이 일을 하나 — 목표가 바뀌었다

- 이 지도는 `../PE_emulator` 의 바탕이다 (CLAUDE.md).
- 사용자가 정한 지금 목표 (2026-10-24, plan 결정 기록):
  1. 초기화 동안 nt·ntdll 이 **두 공간**에 세팅하는 짐을 **빠짐없이** 센다.
  2. 짐마다 **공간 · 단계 · 놓는 법 · 경로**(지도 기준 함수 사슬)를 적는다.
  3. **어디까지 구현할지는 목록이 다 된 뒤** 사용자와 정한다. 그 전엔 "구현할지" 로 거르지 않는다.
- 짐 지도는 세 바퀴로 나눴다 (plan Q76 → Q77 → Q78):

| 바퀴 | 하는 일 | 비유 |
|---|---|---|
| **A** (Q76, **이번**) | 체크리스트 — 셀 칸·전역·함수 목록 + 이미 아는 것에 표시 | 빈 **점검표** 를 만들고, 기억나는 것에 체크 |
| B (Q77) | 사진 — 두 순간에 멈춰 상태를 뜸 | 개업 직전 가게 안 **사진** 찍기 |
| C (Q78) | 쓴 자리·줄기 — 칸마다 누가 어느 길로 놓나 | 놓인 물건마다 **배달 경로** 추적 |

### 무엇이 궁금했나

짐을 빠짐없이 세려면, 먼저 "**무엇을 세야 하나**" 의 목록이 있어야 한다. 요청서의 질문은 넷이다.

1. **칸 목록** — PDB 에 있는 구조체 21개 (커널 14 + 유저 7) 의 칸이 몇 개, 어떤 이름인가?
2. **ntdll 전역 목록** — ntdll 안 쓰기 가능한 전역 변수는 몇 개인가?
3. **함수 목록** — 초기화 함수 15개가 직접 부르는 함수는 무엇무엇인가? (`MmCreateTeb` 는 장부에 줄이 하나도 없어서 **뽑기부터**)
4. **대조** — 1·2 의 칸·전역마다, 이미 있는 문서에 "누가 쓴다" 가 적혀 있나?

### 바퀴 전에 알던 것

- `[정적]` (run10) `MmCreatePeb` 가 PEB 자리를 만들고 exe 헤더 값을 채운다. PEB 주소는 프로세스 객체 `+0x2e0`.
  - 단 PEB 칸 **이름**은 `[추측]` 이었다 (일반적으로 알려진 PEB 배치에 맞춰 본 것).
- `[정적]` (run32) `PspCopyAndFixupParameters` 가 PEB `+0x20`, `PspPrepareSystemDllInitBlock` 이 `LdrSystemDllInitBlock` (`+0xa0`, `+0xb8` 등) 에 쓴다.
  - run32 는 "**이 블록의 타입은 PDB 에 없다**" 고 했다.
- `[정적]` (run15) `PspUserThreadStartup` 이 TEB `+0x108` (`CurrentLocale`) 에 쓴다.
- `[정적]` (run18) `LdrpInitializeProcess +0x1c1` 이 PEB.Ldr, 그리고 PEB TLS 비트맵 칸 (`+0x78`·`+0x80`·`+0x238`·`+0x240`).
- `[공백]` (flow.md 2.1) `MmCreateTeb` 본문은 안 봤다. TEB 에 무엇을 쓰는지 모른다.
- 오케스트레이터가 미리 센 수 (요청서): `_EPROCESS` 263 · `_KPROCESS` 65 · `_ETHREAD` 149 · `_KTHREAD` 237 · `_PEB` 124 · `_TEB` 137 (직접 칸), ntdll 전역 `.data` 438 · `.mrdata` 50.
- 그리고 하나 더 — **02_sprint 문서는 흐름도용이었다.** 흐름도는 "단계마다 **대표 지점만**" 적는다 (CLAUDE.md). 칸 하나하나의 주인을 적으려고 쓴 문서가 아니다. 이게 2.7 의 열쇠다.

---

## 2. 찾은 것

### 2.1 한눈에 — 점검표 네 장, 그리고 짐 지도 한 장

```
sast/38 (정적, 결과서)                        짐 지도 baggage.md (flow-writer)
 ├─ (1) 구조체 칸 목록   21개 · 직접 칸 1,456  ─┐
 ├─ (2) ntdll 전역 목록  493개                ─┤   1장 공간 그림
 ├─ (3) callee 목록     15함수 · 637줄        ─┼─→ 2장 구조체별 요약 (숫자)
 └─ (4) "누가 씀" 대조   (칸, 쓰는 곳) 361쌍   ─┘   3장 짐 표 (아는 칸만)
                                                  4장 두 공간 짝
  + `MmCreateTeb` 첫 export                        5장 단계 순서
                                                  6장 공용 부품 (아직 없음)
                                                  7장 빈 곳 (모르는 칸 이름 전부)
                                                  8장 근거 run
```

- 표 원본은 `ms/disassemble/<모듈>/_baggage/` 에 있다 (커밋 안 함). 칸마다 오프셋·타입·비트·쓴 자리가 한 줄씩.
  - (칸, 쓰는 곳) 쌍 표 `known_writers.tsv` 는 nt 72줄 + ntdll 289줄 = 361줄.
- 짐 지도는 그 원본을 **사람이 읽을 수 있게** 줄인 것이다.
  - 3장에는 **쓴 자리를 아는 칸만** 둔다. 모르는 칸은 7장에 **이름만** 나열한다.
  - 1,456줄 표를 다 펼치는 대신 이렇게 나눴다 — 오케스트레이터 결정. 그래도 **모든 칸 이름은 문서 안에 남는다** (plan 결정 기록).

### 2.2 읽기 0단계 — 짐 지도 맨 위 "쓰는 말"

짐 지도는 맨 위에 "쓰는 말" 다섯 가지를 정해 두고, 3장 표의 열이 다 이 말로 채워진다. 먼저 이걸 손에 쥐자.

| 말 | 뜻 (`baggage.md` "쓰는 말") | 비유 |
|---|---|---|
| **공간** `유저` | 에뮬레이터 가상 주소 공간. 게스트가 직접 읽는다 | 가게 **땅 안**. 에뮬레이터가 직접 쥐고 보여 주는 곳 |
| **공간** `커널` | 에뮬레이터 바깥. nt 엔진·syscall 로만 닿는다 | 가게 땅 **밖의 관리사무소**. 신청 창구로만 대한다 |
| **단계** `①` | 커널 단계 (`NtCreateUserProcess` ~ 유저로 넘어가기) | 관리사무소가 가게를 차려 주는 시간 |
| **단계** `②` | ntdll 단계 (`LdrInitializeThunk` ~ `NtContinue`) | 관리실 직원이 출근해 안을 정리하는 시간 |
| **놓는 법** `직접` | 함수가 칸에 바로 쓴다 (커널은 새 프로세스에 **붙은** 상태에서 유저 칸에 바로 쓴다) | 자기 손으로 써 붙이기 |
| **놓는 법** `주소 넘김` | 칸 주소를 넘기고, 받은 함수가 쓴다 | "이 칸 좀 채워 줘" 하고 빈칸 위치를 남에게 건네기 |
| **놓는 법** `붙이기` | 파일·섹션 내용을 주소 공간에 붙인다 | 조립식 **건물 갖다 놓기** |
| **놓는 법** `syscall` | ntdll 이 syscall 을 부르면 커널이 채운다 | 창구에 신청하면 사무소가 칸을 채워 돌려줌 |
| **놓는 법** `입력` | 부모·부팅·파일·레지스트리에서 온 값 | 밖에서 온 서류 값을 그대로 옮겨 적기 |
| **쓴 주인** | `nt` / `ntdll` / `다른 DLL` (KERNELBASE·KERNEL32 등, 범위 밖·표시만) / `사용자 코드` (TLS·DllMain) | 누가 갖다 놓았나 |

- 헷갈리기 쉬운 것: **공간과 주인은 따로 논다.**
  - 유저 공간 칸이라도 주인이 `nt` 일 수 있다 (예: PEB 칸 22개는 커널이 단계 ① 에 씀).
  - 단계 ② 에 놓인 칸이라도 주인이 `nt` 일 수 있다 (예: ntdll 이 syscall 을 부르면 커널이 칸을 채움 — 2.5 한 줄 읽기 ③).

### 2.3 칸은 어떻게 셌나 — 양식 견본집의 "직접 칸"

#### PDB 안의 양식

- [run9 해설](../02_sprint/run9-create-user-process-해설.md) 에서 PDB = 마이크로소프트가 주는 **함수 이름표** 파일이라고 했다.
- 그 PDB 안에는 함수 이름 말고 **구조체 서식**(타입 레코드)도 들어 있다. 구조체마다 칸의 이름·오프셋·타입·크기가 적혀 있다 `[정적]`.
  - 비유: 관리사무소의 **빈 양식 견본집**. "안내판 양식(`_PEB`)", "인사 카드 양식(`_ETHREAD`)" … 칸 이름이 인쇄돼 있다.
- 결과서는 두 PDB 의 타입 레코드를 `llvm-pdbutil` 로 덤프해서, 임시 스크립트로 표를 만들었다 (하네스 밖 `ms/ghidra/_tmp_sast38/`).

#### 직접 칸 = 양식에 인쇄된 칸 하나하나

- **직접 칸** = 구조체 필드 목록의 `LF_MEMBER` 하나. 짐 지도에서 "칸 하나" 는 이걸 말한다.
- **비트 칸도 한 칸**으로 센다 ([비트와 플래그](../02_sprint/run9-create-user-process-해설.md#비트와-플래그)의 체크 칸 하나 = 한 칸).
  - 예 `[정적]`: `_EPROCESS` 의 `Flags3` 은 정수 칸 하나다. 그 뒤에 비트 칸들이 **같은 오프셋으로** 줄지어 나온다. `ProcessFirstResume` 은 `Flags3` 안 `15:1` (15번 비트, 길이 1).
  - 그래서 1,456칸 중 **330칸이 비트 칸**이다.
- 이 PDB 는 이름 없는 union/struct 를 **이미 평평하게 펴서** 담았다 `[정적]`.
  - 그래도 `<unnamed-tag>` 형식으로 남은 몇 곳은 안쪽을 펴서 "펼친 칸" 으로 따로 적었다. **펼친 칸은 1,456 에 안 셌다.**

#### 어느 PDB 로 셌나

- **커널 14개 = ntoskrnl PDB.** ntdll PDB 에도 커널 구조체가 있지만 칸이 적다 `[정적]`:
  - `_EPROCESS` 261 (커널 PDB 263), `_KTHREAD` 235 (237). 일부 칸이 `Reserved`·`Spare` 칸 하나로 **가려져** 있다. 크기는 같다.
- **유저 7개 = ntdll PDB.** 두 PDB 에 다 있고, 크기·칸 수·모든 줄이 **완전히 같다** `[정적]`.
- 요청서가 짐작한 타입 이름 (`_HANDLE_TABLE`, `_MMVAD`/`_MMVAD_SHORT`, `_TOKEN`, `_SECTION`/`_CONTROL_AREA`/`_SEGMENT`, `_FILE_OBJECT`) 은 **전부 그대로** 있었다 `[정적]`.
- 오케스트레이터가 미리 센 6개 칸 수와 크기도 **모두 맞았다.**

### 2.4 읽기 1단계 — 2장 숫자 (구조체별 요약)

짐 지도를 펴면 제일 먼저 2장 표를 본다. **"어디가 얼마나 차 있고, 얼마나 비어 있나"** 를 한눈에 보는 표다.

#### 표 (대표만, `baggage.md` 2장과 같은 숫자)

| 구조체·구역 | 공간 | 칸 수 | nt 가 씀 | ntdll 이 씀 | 다른 DLL | 0 그대로 | 아직 모름 |
|---|---|---|---|---|---|---|---|
| `_PEB` | 유저 | 124 | 22 | 9 | 0 | 사진 전 | 93 |
| `_RTL_USER_PROCESS_PARAMETERS` | 유저 | 40 | 40 | 1 | 0 | 사진 전 | 0 |
| `_TEB` | 유저 | 137 | 1 | 3 | 0 | 사진 전 | 133 |
| `_LDR_DATA_TABLE_ENTRY` | 유저 | 65 | 1 | 31 | 0 | 사진 전 | 34 |
| `_KUSER_SHARED_DATA` | 유저 | 103 | 0 | 0 | 0 | 사진 전 | 103 |
| ntdll `.data` 전역 | 유저 | 438 | 1 | 46 | 0 | 사진 전 | 391 |
| `_EPROCESS` | 커널 | 263 | 22 | 0 | 0 | 사진 전 | 241 |
| `_KTHREAD` | 커널 | 237 | 10 | 0 | 0 | 사진 전 | 227 |
| `_HANDLE_TABLE` | 커널 | 17 | 0 | 0 | 0 | 사진 전 | 17 |
| `_SECTION_IMAGE_INFORMATION` | 커널 | 26 | 16 | 0 | 0 | 사진 전 | 10 |
| **구조체 유저 7개 합** | | **488** | 64 | 57 | 0 | 사진 전 | **369** |
| **구조체 커널 14개 합** | | **968** | 62 | 0 | 0 | 사진 전 | **906** |
| **ntdll 전역 3구역 합** | | **493** | 2 | 51 | 0 | 사진 전 | **439** |

#### 한 줄 읽기 — `_PEB` 줄

- **칸 수 124** = 안내판 양식에 인쇄된 직접 칸 수.
- **nt 가 씀 22** = 관리사무소가 써 넣은 칸 (전부 단계 ①, `MmCreatePeb`·`PspCopyAndFixupParameters`).
- **ntdll 이 씀 9** = 관리실이 써 넣은 칸 (전부 단계 ②, `Ldr`·`ProcessHeap`·`FastPebLock`·TLS 비트맵 4칸·`ProcessInitializing`·`LoaderLock`).
- 22 + 9 = **31** = 3.1 절 머리 줄의 "**아는 칸 31** (정확 19 · 짐작 12)".
- **다른 DLL 0** = KERNELBASE·KERNEL32 등이 이 칸에 쓰는 자리를 문서에서 하나도 못 찾았다는 뜻.
- **0 그대로 "사진 전"** = 아직 셀 수 없다 (아래 조심할 것 4).
- **아직 모름 93** = 124 − 31. 이 93칸의 이름은 7장에 있다.
- 비유로: 안내판 양식 124칸 중 31칸은 "누가 썼는지" 기억이 있고, 93칸은 빈칸이다.

#### 숫자 읽을 때 조심할 것 다섯

1. **한 칸을 둘이 쓰면 양쪽에 센다.**
   - 매개변수 블록은 nt 40 + ntdll 1 인데, 아는 칸은 41 이 아니라 **40** 이다. `LoaderThreads` 를 커널(단계 ①)과 ntdll(단계 ②)이 둘 다 쓰기 때문이다.
2. **syscall 로 커널이 채운 칸은 `nt` 로 셌다.**
   - `LdrpInitCompleteEvent` (ntdll `.data`), DLL 항목의 `DllBase`. 그래서 유저 구조체 `_LDR_DATA_TABLE_ENTRY` 에도 "nt 가 씀 1" 이 있다.
3. **"모름 0" 이라고 다 아는 건 아니다.**
   - 매개변수 블록은 모름 0 이지만, 40칸 중 **22칸은 "블록 통째 복사" 짐작**이다. 칸 하나하나를 말한 문서는 없다.
4. **"0 그대로" 는 전부 "사진 전"이다.**
   - "아무도 이 칸을 안 쓴다" 는 정적으로는 못 보인다. 증거는 사진뿐이다 (Q77).
   - 비유: 점검표만 봐서는 "이 칸이 비어 있다" 를 확신 못 한다. 가게 안을 **직접 찍어 봐야** 안다.
5. **`ntdll 이 씀` 에는 범위 밖 길에서만 쓰는 전역 2개가 섞여 있다.**
   - `LdrpDllDirectory` (DLL 폴더 API), `LdrpForkInProgress`. 초기화 동안엔 처음 값 그대로일 후보다 → 사진으로 가린다 (7.1).

#### 합계는 이렇게 맞는다

- 구조체 21개 직접 칸 **1,456** = 아는 칸 **181** (정확 138 · 짐작 43) + 모름 **1,275**.
  - 유저 488칸 중 모름 369, 커널 968칸 중 모름 906. 369 + 906 = 1,275.
- ntdll 전역 **493** = 쓴 자리 아는 것 **54** (`.data` 47 · `.mrdata` 6 · `.00cfg` 1) + 모름 **439**.
- 여기서 벌써 눈에 띄는 게 있다. **커널 쪽은 968칸 중 906칸이 모름**이다. 왜 그런지는 2.7 에서.

#### 같은 숫자를 그림으로 — 1장 공간 그림

2장 표를 두 공간으로 나눠 그린 게 1장이다. 줄여서 옮기면 (숫자는 `baggage.md` 1장 그대로):

```
유저 공간 (가게 땅 — 에뮬레이터 가상 주소 공간)
├─ 안내판        _PEB 0x7d8 .......................... ① nt 22 · ② ntdll 9      [모름 93]
│   └─ 개업 서류  _RTL_USER_PROCESS_PARAMETERS 0x450 .. ① nt 40 · ② ntdll 1      [모름 0]
├─ 개인 안내판    _TEB 0x1878 (메인 스레드) ............ ① nt 1 · ② ntdll 3       [모름 133]
├─ 관리실 칠판    ntdll .data (전역 438) .............. ② ntdll 46 · nt 1 (syscall)  [모름 391]
├─ 명부 줄       _LDR_DATA_TABLE_ENTRY 0x138 ......... ② ntdll 31 · ② nt 1 (DllBase, syscall)  [모름 34]
└─ 로비 게시판    _KUSER_SHARED_DATA 0xa80 ............ [공백] (ntdll 은 읽기만)  [모름 103]

커널 공간 (관리사무소 — 에뮬레이터 바깥)
├─ 가게 장부      _EPROCESS 0x840 ..................... ① nt 22                 [모름 241]
│   ├─ 핸들 표    _HANDLE_TABLE 0x80 .................. [공백]                  [모름 17]
│   ├─ 구획 장부   _MMVAD 0x88 / _MMVAD_SHORT 0x40 ...... [공백]                  [모름 9 / 14]
│   ├─ 토큰       _TOKEN 0x5c8 ........................ [공백]                  [모름 55]
│   └─ exe 섹션 묶음 … 이미지 정보 _SECTION_IMAGE_INFORMATION .. ① nt 16           [모름 10]
└─ 직원 인사 카드  _ETHREAD 0x798 (메인 스레드) ......... ① nt 5                  [모름 144]
    ├─ 앞장       _KTHREAD 0x4c0 ...................... ① nt 10                 [모름 227]
    └─ 출발 메모   _KTRAP_FRAME 0x190 .................. ① nt 6 · ② 끝 nt 4       [모름 60]
```

- `[모름 N]` = 7장으로 간 칸 수. 숫자 = 3장에서 쓴 자리를 아는 칸 수 (주인별).
- **커널 쪽 그림에 `[공백]` 이 줄줄이** 붙어 있다. 2.7 에서 이 그림이 왜 이렇게 생겼는지 본다.

### 2.5 읽기 2단계 — 3장 표 한 줄

2장이 "얼마나" 를 말한다면, 3장은 "**어느 칸에, 누가, 어떻게**" 를 말한다. 구조체·구역마다 절이 하나씩 있다 (3.1 `_PEB` ~ 3.24 `_FILE_OBJECT`).

#### 열 일곱 개

| 열 | 뜻 |
|---|---|
| 칸 | PDB 오프셋 + 칸 이름. 같은 근거의 칸 여럿은 `·` 로 한 줄에 묶음 |
| 단계 | `①` / `②`, 그리고 `② 끝` = `NtContinue` 안 (커널), `② 일꾼` = ntdll 일꾼 스레드, `—` = 초기화 범위 밖 길 (종료·복제·API), `?` = 지도 경로 모름 |
| 무엇을 (값·모양) | 그 칸에 들어가는 값 |
| 놓는 법 | 2.2 의 다섯 가지 |
| 쓴 주인 | `nt` / `ntdll` / … |
| 경로 (지도 기준) | 함수 이름 사슬. `→` 부름, `⇒` 꼬리 JMP. 오프셋은 **마지막 함수 뒤** (그 함수 안 쓰는 줄. `호출 +0x..` 은 부르는 자리) |
| 근거 | `[정적]` = 확실도 **정확**, `[추측]` = **짐작**. run 번호·결과서·`_refs` |

- **확실도** 세 가지 (결과서 4번 기준):
  - **정확** = 근거 문서에 "쓰는 함수 + 칸 오프셋 또는 이름" 이 같이 있고, PDB 칸과 맞음.
  - **짐작** = 범위로만 말했거나 (예: `+0xb8 ~ +0xe0`), 칸 이름이 문서에서 `[추측]` 이거나, 쓰기가 간접 (주소 넘김·복사) 인 것.
  - **모름** = 근거에서 못 찾음 → 3장에 없고 7장에 있다.
- **`_refs`** = ntdll 전역 이름마다 "누가 읽고 쓰나" 를 뽑아 둔 표 (run17 때 생긴 찾기 도구, `ms/disassemble/ntdll/_refs/`).
  - WRITE·READ_WRITE 줄 = 그 칸에 쓰는 명령이 있다 → 정확.
  - DATA 줄 = 칸 **주소만** 집어 간다 (`LEA`) → 받은 쪽이 쓰니까 짐작.

#### 경로 줄임말 — 사슬 머리가 길어서 줄였다

| 줄임 | 뜻 |
|---|---|
| `①…` | `NtCreateUserProcess` — 부모 cmd.exe 스레드의 시스템 콜 안 (flow.md 2.1) |
| `①T…` | `KiStartUserThread → PspUserThreadStartup` — test.exe 스레드, 유저로 가기 전 (2.3) |
| `②…` | `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (2.4) |
| `②M…` | DLL 하나를 붙여 명부에 올리는 `LdrpMapDllWithSectionHandle` 까지 (kernel32·kernelbase 길 / dll1·dll2 길, 2.5·2.6) |
| `{ntdll · exe · DLL}` | 모듈 항목마다 부르는 자리가 달라서 따로 표로 뺐다 (3장 머리의 "모듈마다 부르는 자리" 표) |

- 그러니 `②… +0x1c1` 은 **`LdrpInitializeProcess +0x1c1`** 이라는 뜻이다. 사슬 맨 끝 함수 안의 줄.

#### 한 줄 읽기 ① — 가장 단순한 줄: `_PEB +0x18 Ldr`

`| +0x18 Ldr | ② | ntdll 전역 PebLdr 주소 (빈 목록 3개로 채운 뒤) | 직접 | ntdll | ②… +0x1c1 | [정적] run18 · 정확 |`

- **누가**: `LdrpInitializeProcess` (사슬 `②…` 의 끝). 관리실 직원이 출근해서 가게 전체를 세팅하는 방이다.
- **어디서**: 그 함수 `+0x1c1` 줄.
- **무엇을**: 안내판(PEB) 시작에서 `0x18` 바이트째 칸 `Ldr` 에, 관리실 칠판 `PebLdr` 의 **주소**를 적는다. 그 칠판은 바로 앞에서 빈 목록 3개로 채워 둔다.
- **무엇을 위해**: 안내판에 "**건물 명부는 여기**" 를 적으려고 ([run18 해설 2.6](../02_sprint/run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1) 의 "건물 명부가 생기는 순간").
- **놓는 법 직접** — 이 함수가 칸에 바로 쓴다. **근거 정확** — run18 에 함수·오프셋·칸이 같이 있다.

#### 한 줄 읽기 ② — 커널 쪽 흔한 꼴: `_EPROCESS +0x300 ObjectTable`

`| +0x300 ObjectTable | ① | 핸들 표 준비 (ObInitProcess 호출 — "핸들 표 준비" 는 문서 [추측]) | 주소 넘김 | nt | ①… → PspAllocateProcess → ObInitProcess (호출 +0x1991) | [추측] sast04 · run10 · 짐작 |`

- **누가**: `PspAllocateProcess` (가게 차리기 큰 함수, run10).
- **어디서**: `+0x1991` 에서 `ObInitProcess` 를 부른다 ("호출" = 부르는 자리).
- **무엇을**: 가게 장부(`_EPROCESS`)의 `ObjectTable` 칸 = 핸들 표 자리. 하지만 **칸에 쓰는 줄은 `PspAllocateProcess` 안에 없다.** 칸을 받은 `ObInitProcess` 가 쓸 것이다.
- **무엇을 위해**: 새 가게의 손잡이 목록(핸들 표)을 마련하려고 — 이것도 **이름으로 본 짐작**이다.
- 그래서 놓는 법 **주소 넘김**, 근거 **`[추측]` · 짐작**.
- 이 꼴이 중요하다. "부르는 줄은 아는데 **받은 함수 안은 안 열어 봤다**." 커널 쪽 줄이 대개 이렇다 → 2.7.

#### 한 줄 읽기 ③ — 유저 칸인데 주인이 커널: `_LDR_DATA_TABLE_ENTRY +0x30 DllBase` (DLL 항목)

`| +0x30 DllBase | ② | DLL 항목: 기준 주소 칸 = 이 칸 → 커널이 직접 채운다 (0 으로 시작) | syscall (NtMapViewOfSection) | nt | ②M… → LdrpMinimalMapModule +0x174 | [정적] run38 (flow.md 2.6) · 정확 · flow-writer 더함 |`

- **누가**: ntdll 의 `LdrpMinimalMapModule` (DLL 하나를 붙이는 길 `②M…` 안, run38).
- **어디서**: `+0x174` 에서 `NtMapViewOfSection` 을 부른다.
- **무엇을**: 명부 줄(`_LDR_DATA_TABLE_ENTRY`)의 `DllBase` 칸 주소를 "기준 주소를 받을 칸" 으로 넘긴다. 칸은 0 으로 시작하고, **관리사무소가 직접 채워 돌려준다.**
- **무엇을 위해**: DLL 건물을 땅에 갖다 놓고(붙이기), 놓은 번지를 명부 줄에 받아 적으려고.
- 그래서 **공간은 유저인데 주인은 `nt`**, 놓는 법은 **syscall**. 2.2 의 "공간과 주인은 따로 논다" 가 이 줄이다.
- 끝의 "**flow-writer 더함**" = 결과서 대조엔 없었는데, flow-writer 가 짐 지도를 쓰다 flow.md 에서 보고 더한 줄 (2.11).

#### 같은 칸을 여러 번 쓰면 — 줄을 나눈다

- 3장은 같은 칸을 다른 함수가 또 쓰면 **줄을 나눈다.**
- 대표 예가 연결 카드의 **진행 도장** `_LDR_DDAG_NODE +0x38 State` 다 (3.6). 칸 하나에 줄이 줄줄이 있다 `[정적]`.
  - 표에 나오는 값: 2 (`Mapped`), 3 (`WaitingForDependencies`), 4 (`Snapping`), 5 (`Snapped`), 6 (`Condensed`), 7 (`ReadyToInit`), 8 (`Initializing`), 9 (ntdll 노드는 처음부터 9 = `ReadyToRun`), 실패면 -4 (`InitError`).
  - 도장을 찍는 함수가 값마다 다르다 (`LdrpProcessMappedModule`, `LdrpMapAndSnapDependency`, `LdrpLoadDependentModuleInternal`, `LdrpCondenseGraphRecurse`, `LdrpNotifyLoadOfGraph`, `LdrpInitializeNode` …).
- 비유: 서류 한 장의 "진행 상태" 칸에 부서마다 차례로 도장을 덧찍는 것. 점검표엔 도장 하나하나를 따로 적는다.

### 2.6 읽기 3단계 — 7장 빈 곳

3장이 "아는 것" 이라면, 7장은 "**모르는 것 전부**" 다. 다음 바퀴(Q77·Q78)의 일감 목록이기도 하다.

#### 7장 일곱 절

| 절 | 무엇이 있나 | 크기 | 누가 채우나 |
|---|---|---|---|
| 7.1 0 그대로 — 사진 전 | 2장 "0 그대로" 열. 정적으로 못 셈 | 전부 비어 있음 | **Q77** 사진 |
| 7.2 구조체별 모르는 칸 | 칸 이름 나열 (오프셋 순) | **1,275칸** | Q77 (바뀌나) → Q78 (누가) |
| 7.3 ntdll 전역 모르는 것 | 이름 나열 (rva 순) | **439개** | Q77 · Q78 |
| 7.4 짐작이라 다시 볼 것 | 3장에 있지만 근거가 약한 줄 | 매개변수 22칸, PEB 12칸 … | Q77 · Q78 |
| 7.5 실마리 있는 모름 | 모름으로 셌지만 문서에 힌트가 있는 칸 | TEB 대입 47줄 등 | Q78 |
| 7.6 칸 표가 아직 없는 짐 | 표를 안 만든 이웃 타입, PDB 에 없는 타입, 칸이 아닌 "구역" 짐 | — | Q77 (덤프) · Q78 |
| 7.7 결과서가 넘긴 공백 | sast/38 "새로 생긴 공백" 을 Q 번호와 함께 다시 적음 | — | Q77 · Q78 |

- 7장 머리 줄에 "이번 바퀴에 생긴 빈 곳은 **새 항목 없이 Q77·Q78 에 출처로** 붙는다" 고 적혀 있다 (오케스트레이터 결정).

#### 7.2 읽는 법 — 괄호 표기 셋

7.2 는 칸 이름을 쉼표로 줄줄이 늘어놓는다. 비트 칸이 섞여서 괄호 표기 세 가지를 쓴다. `_PEB` 줄에서 하나씩 보자.

1. **`이름 [ … ]`** = 그 정수 칸과 **안의 비트 칸들 다** 모름.
   - 예: `TracingFlags [HeapTracingEnabled, CritSecTracingEnabled, LibLoaderTracingEnabled, SpareTracingBits]`
2. **`이름 (모르는 비트만) [ … ]`** = 정수 칸은 모르고, **비트 몇 개는 앎** (아는 비트는 3장).
   - 예: `CrossProcessFlags (모르는 비트만) [ProcessInJob, ProcessUsingVEH, …]`
   - `CrossProcessFlags` 안 비트 `ProcessInitializing` 은 3.1 에 있다 (ntdll `_LdrpInitialize +0x107` 이 켜고 `+0x20f` 이 끔).
3. **`(X 의 나머지 비트) [ … ]`** = 정수 칸 X 는 **아는데**, 그 안 비트 몇 개는 모름.
   - 예: `(BitField 의 나머지 비트) [ImageUsesLargePages, IsProtectedProcess, …]`
   - `BitField` (`+0x3`) 자체는 `MmCreatePeb` 가 견본을 복사한다 (3.1). 그런데 비트 칸도 한 칸씩 세니까, 비트마다의 주인은 따로 "모름" 이다.
- 비유: 체크 칸 여러 개가 든 네모 하나를 통째로 베껴 적은 건 알지만, "이 중 어느 체크를 누가 했나" 는 모르는 상태.

#### 7.5 — "모름" 숫자엔 실마리 있는 칸도 섞여 있다

- 7.5 는 "모르는 칸인데 문서에 실마리가 있는 것" 이다. **3장으로 옮기지 않았다** — 결과서 숫자를 그대로 두려고 (7.5 제목).
- 예 (`baggage.md` 7.5):
  - TEB 칸 다수 ← `MmCreateTeb` 의 TEB 대입 약 47줄 (2.8)
  - `_TEB` `LoadOwner` (`SameTebFlags` `0x1000`) ← `LdrpDrainWorkQueue +0xd3` 이 켬 (flow.md 2.6, run24)
  - `_LDR_DATA_TABLE_ENTRY` `ProcessAttachFailed` ← `LdrpInitializeNode +0x29e` (flow.md 2.7, run27)
  - `_PEB.ApiSetMap` (`+0x68`) ← `MmMapApiSetView` 가 올리는 뷰 (run32). 칸에 쓰는 줄은 모름.
- 그러니 2장의 "아직 모름" 은 "**아무 단서도 없다**" 가 아니라 "**칸 단위 근거로 셀 만한 줄이 없다**" 에 가깝다. Q78 이 이 실마리부터 잡을 수 있다.

### 2.7 왜 커널 쪽은 거의 비어 있나

#### 먼저 숫자

- 커널 구조체 968칸 중 **모름 906칸**.
- 아는 칸이 **0** 인 구조체: `_KPROCESS` · `_HANDLE_TABLE` · `_MMVAD` · `_MMVAD_SHORT` · `_TOKEN` · `_SECTION` · `_FILE_OBJECT`.
- 큰 둘도 거의 비었다: `_EPROCESS` 모름 241 / 263, `_KTHREAD` 모름 227 / 237.

#### 근거를 하나씩 쌓으면

1. **대조는 새로 읽은 게 아니라 옮긴 것이다.**
   - 결과서: "이 결과서의 '누가 씀' 은 문서에 이미 적힌 것을 옮긴 것이다. 새로 코드를 읽은 건 `MmCreateTeb` 와 `.asm` 로 다시 맞춰 본 줄뿐이다."
   - 그러니 이번 숫자는 "**우리 문서가 칸 단위로 얼마나 적어 뒀나**" 를 잰 것이다.
2. **그 문서는 흐름도용이었다.**
   - 흐름도는 "단계마다 대표 지점만 (수십 개). 명령 하나하나를 다 따라가는 건 안 한다" (CLAUDE.md).
3. **커널 쪽 문서는 줄 단위로 다 대조했다.**
   - 결과서는 flow.md 2.1 (커널 생성) · 2.3 (스레드 출발) 을 **줄 단위로** 읽었다. 검색으로 대충 본 게 아니다.
   - 그러니 "대조가 허술해서 빈 것" 은 아니다.
4. **가장 큰 함수를 묶음으로만 읽었다.**
   - `PspAllocateProcess` 는 호출 144줄, 서로 다른 callee 105개. 이걸 **14묶음**으로만 읽었다 (결과서 4번).
   - callee 105개 중 **문서에 이름도 없는 것 40개**, 본문을 뽑아 둔 것 **12개**.
5. **커널 객체 칸은 "받은 함수 안" 에서 채워지는 줄이 많다.**
   - 3장 `_EPROCESS` 의 짐작 줄 `Pcb` · `Token` · `ObjectTable` 은 다 "**부르는 줄만 앎** (callee 본문 안 봄)" 이다 (7.4).
   - 칸이 0 인 구조체들도 "**만드는 함수 이름**" 까지는 안다 (아래 표). 그 함수 안에서 어느 칸을 쓰는지를 안 읽었다.
   - 안 뽑은 본문 (결과서 공백 1): `KeStartThread` (1,182바이트) · `KeInitializeProcess` · `ObInitProcess` · `PspInitializeProcessSecurity` · `PsAssignThreadId` · `PspApplyMitigationOptions` · `MiAllocateProcessVads` · `MiInsertProcessVads`. `KeInitThread` 는 일부만.

| 구조체 | 만들거나 붙이는 곳 — 이름까지는 앎 (`baggage.md` 3장, 구조체 단위) | 칸 단위 근거 |
|---|---|---|
| `_KPROCESS` | `PspAllocateProcess +0x171e KeInitializeProcess` (sast04 · run10) | 없음 |
| `_HANDLE_TABLE` | `PspAllocateProcess +0x1991 ObInitProcess` (이름으로 본 `[추측]`) | 없음 |
| `_MMVAD` | `MmInitializeProcessAddressSpace +0x161 MiAllocateProcessVads`, `+0x1a8 MiInsertProcessVads` | 없음 |
| `_TOKEN` | 붙이는 곳 `[추측]`: `PspAllocateProcess +0x1927 PspInitializeProcessSecurity` (sast04), `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange` (run15) | 없음 |
| `_SECTION` | `NtCreateUserProcess +0x93c MmCreateSpecialImageSection → …` (run31) | 없음 |
| `_FILE_OBJECT` | `NtCreateUserProcess +0x6cb IopCreateFile` (run9) | 없음 |

6. **반대 증거 — 칸 단위로 읽은 바퀴가 있던 곳은 찼다.**
   - `_SECTION_IMAGE_INFORMATION` 26칸 중 **16칸** — run31 (sast26) 이 칸별 표를 만들었다.
   - `_RTL_USER_PROCESS_PARAMETERS` 모름 **0** — run32 가 Fixup 하는 안쪽 포인터 칸 등을 칸 이름으로 적었고, 나머지는 "블록 통째 복사" 로 묶어 말했다 (그래서 22칸은 짐작).
   - `_LDR_DATA_TABLE_ENTRY` **31칸** — run22·run35 가 새 항목에 쓰는 칸을 줄줄이 적었다.
   - `_PEB_LDR_DATA` 9칸 중 **7칸** — 이게 ntdll 전역 `PebLdr` 한 벌이라 `_refs` WRITE 줄로 잡혔다.

#### 그래서

- 커널 쪽이 빈 건 "**커널이 짐을 적게 놓아서**" 가 아니라 "**우리가 아직 그 칸들을 칸 단위로 안 읽어서**" 로 보인다 `[추측]` — 결과서가 든 이유 (4·5) 와 위 대비 (6) 에서 끌어낸 것이다.
- 비유: 관리사무소 안에는 장부 담당자가 여럿 있고, 각자 자기 서랍에서 장부 칸을 채운다. 우리는 지금까지 "**누가 어느 담당자를 불렀나**" (동선) 만 적었고, 담당자 **서랍 안**은 안 열어 봤다.

#### 아직 확인 아님

- 그 칸들이 초기화 동안 **실제로 바뀌는지** → 사진 Q77 (커널 객체 `dt`, `!vad`, `!handle`, 토큰).
- **누가** 쓰는지 → Q78 (위 8개 본문, `PspAllocateProcess` callee 중 문서에 없는 40개).

#### 유저 쪽도 빈 곳이 있다 — 이유는 조금 다르다

- 유저 쪽 뒤 절 (flow.md 2.5 ~ 2.10, DLL 항목·TLS·kernel32 준비) 은 줄 단위가 아니라 **검색으로만** 대조했다 (결과서 판정 "한계").
- 그래서 유저 쪽엔 "문서엔 있는데 대조가 못 건진" 쓴 자리가 더 있을 수 있다. 7.5 의 `ProcessAttachFailed` · `LoadOwner` · `EntryPointActivationContext` 가 그 예다 (run 문서 "아직 확인 필요").
- 정리하면:
  - 커널 쪽 빈 곳 = 주로 "**원래 안 읽은 서랍**"
  - 유저 쪽 빈 곳 = 그것에 더해 "**읽었지만 이번 대조가 못 건진 줄**" 도 섞임

### 2.8 `MmCreateTeb` — 이번에 처음 연 함수

#### 호출 관계 (run 문서 그대로)

```
PspAllocateThread
 └─ +0x631  MmCreateTeb (rva 0x9318c4, 811바이트)
      ├─ (1) +0x7f   KiStackAttachProcess              새 프로세스에 붙기
      ├─ (2) +0xac   MiAllocateFromSubAllocatedRegion  TEB 자리 [추측: 이름]
      │        실패 (+0xb0 JS) → +0x2d8 KiUnstackDetachProcess → 끝
      │      (TEB 대입 약 47줄 — 64비트 갈래 + WOW64 갈래, 디컴파일 기준)
      ├─ (3) +0x29e  KiUnstackDetachProcess            떼기 (성공 길)
      └─ (4) +0x2b1  __security_check_cookie
```

- 숫자는 콜스택에 찍히는 **ret 오프셋**이다. call 자리는 `+0x7a` · `+0xa7` · `+0x299` · `+0x2ac` · `+0x2d3`.
- 부르는 쪽: `PspAllocateThread +0x62c` (ret `+0x631`) — run11 과 같다.
- 모두 `[정적]` (`.asm`, `_calls.tsv`). 지도 (`_map/NtCreateUserProcess_edges.tsv`) 의 5줄과도 같다.
- 정규식 `^MmCreateTeb$`, depth 1 로 뽑았다. `MmCreateTeb` 와 그 callee 넷이 같이 나와 모두 5개다. 장부 `_calls.tsv` 는 **57줄 늘고, 줄어든 줄은 없다** (`MmCreateTeb` 5줄 + 도우미 셋 52줄).

#### 한 줄 읽기 — `MmCreateTeb +0xa7` (ret `+0xac`) → `MiAllocateFromSubAllocatedRegion` `[정적]`

- **누가**: `MmCreateTeb`. 첫 직원의 **개인 안내판(TEB)** 을 거는 함수 (run11 의 다섯 걸음 중 (라)).
- **어디서**: `+0xa7` 에서 부른다. 콜스택엔 돌아올 자리 `+0xac` 로 찍힌다.
- **누구를**: `MiAllocateFromSubAllocatedRegion`.
- **무엇을 위해**: 새 가게 땅에 **TEB 자리**를 받으려고 — 이건 이름에서 짐작한 것이다 `[추측: 이름]`.
- 실패하면 바로 뒤 `+0xb0 JS` 로 끝 쪽 `+0x2d3` 에 가서 `KiUnstackDetachProcess` 로 떼고 끝난다 `[정적]`.

#### `MmCreatePeb` 와 같은 모양이다

- 차례가 **붙기 → 자리 → (대입) → 떼기** 다 `[정적]`. run10 의 `MmCreatePeb` 와 같은 모양이다.
- [남의 땅에 잠깐 들어가기](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기) 그대로다. 관리사무소 직원(부모 cmd.exe 의 스레드)이 새 가게 땅에 들어가서, 자리를 잡고, 안내판에 글씨를 쓰고, 나온다.
- 짐 지도 "놓는 법" 으로 말하면 이 47줄은 **`직접`** 이다 — "커널은 새 프로세스에 붙은 상태에서 유저 칸에 바로 쓴다" (쓰는 말).

#### 47줄 — 칸별로는 아직 `[공백]`

- 새로 안 것: 붙어 있는 동안 **TEB 에 직접 쓰는 대입이 약 47줄** 있다 (디컴파일 `MmCreateTeb.c` 기준, 64비트 갈래 + WOW64 갈래 **합**) `[정적]`.
- 하지만 **어느 칸에 무엇을 쓰는지**는 이번 범위가 아니라서 안 갈랐다 `[공백]` → Q78.
  - 47줄은 두 갈래를 합친 수라, 한 번 실행에서 다 도는 줄은 아닐 것이다 `[추측: "두 갈래 합산" 이라는 결과서 문장에서]`.
- 그래서 짐 지도는:
  - 3.3 `_TEB` 는 여전히 아는 칸 **4** (`NtTib` · `CurrentLocale` · `RanProcessInit` · `SchedulerSharedDataSlot`).
  - 1장 그림에 "(① `MmCreateTeb` 의 TEB 대입 약 47줄 — 칸별로 안 갈라 `[공백]`)" 한 줄, 7.5 첫 줄에 실마리로.
- 비유: 관리사무소가 직원 개인 안내판에 **글씨를 47줄쯤 써 준다**는 건 이제 안다. 어느 칸에 무엇을 썼는지는 아직 안 읽었다.

### 2.9 ntdll 전역 — 관리실 칠판 493개

#### 어디서 셌나

- ntdll 전역 변수 = 관리실 안 **칠판**들 ([run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6 의 `LdrpProcessInitialized` 같은 것).
- PDB **공개 이름** (`S_PUB32` 6,930개) 중 함수가 아닌 **데이터 이름 2,066개**를, PE 머리의 구역 표로 갈랐다 `[정적]`.

| 구역 | rva | 크기 | 데이터 이름 | 쓴 자리 아는 것 (정확/짐작) |
|---|---|---|---|---|
| `.data` | `0x1cc000` | `0x8f58` | **438** | 47 (45/2) |
| `.mrdata` | `0x1e7000` | `0x35b8` | **50** | 6 (3/3) |
| `.00cfg` | `0x1e6000` | `0x28` | 5 | 1 (0/1) |

- 오케스트레이터가 센 `.data` 438 · `.mrdata` 50 이 **맞다.**
- `.rdata` 데이터 이름 1,546개는 읽기 전용이라 **짐이 아니어서** 안 셌다.

#### 구역 새 말 둘

- **`.mrdata`** = PE 머리엔 쓰기 가능이지만, 실행 중엔 ntdll 이 **필요할 때만 열고 닫는** 구역 (run18 묶음 3).
  - 비유: **유리 덮개를 씌운 칠판.** 평소엔 잠가 두고, 고칠 때만 덮개를 연다.
  - 유명한 칠판이 여기 있다: 관리실 업무 수첩 `LdrSystemDllInitBlock` (rva `0x1ea450`).
- **`.00cfg`** = PE 머리엔 **읽기 전용**인데, 로더가 값을 넣는 **CFG 함수 포인터 5칸**이 있어서 따로 셌다 (CFG 중개는 [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.9).
  - 맨 앞 `__guard_dispatch_icall_fptr` 는 실행 중 쓰는 코드 줄이 없다. 짐작 1 로 셌지만 주인은 사실상 모름 (7.4).

#### 크기는 전부 `[추측]`

- 공개 PDB 에는 전역 변수의 **형식·크기 줄이 하나도 없다** `[정적]`.
- 그래서 크기 = "**다음 이름까지 거리**" 로 짐작했다. 위로 막힌 값(상한)이다.
- 이 방법이 맞는지 시험한 칸이 하나 있다: `LdrSystemDllInitBlock` 에서 다음 이름까지 `0x128` = 296 = run32 가 말한 블록 크기. **맞았다.**

#### 주소만 넘기는 전역 6개

- `_refs` 에 **DATA 줄만** 있는 전역 6개가 있다: `Kernel32ThreadInitThunkFunction` · `g_LdrpWow64LdrpInitialize` · `LdrpKnownDllDirectoryHandle` · `LdrpLoadCompleteEvent` · `LdrpWorkCompleteEvent` · `__guard_dispatch_icall_fptr`.
- 코드가 칸 **주소만** 넘긴다 → 받은 함수가 쓴다. 정적으로는 "무슨 값이 들어갔나" 를 못 본다 → 실제 값은 사진 (Q77).
  - 하나는 예외다: `__guard_dispatch_icall_fptr` 는 PE 로드 구성이 가리키는 칸이고, 실행 중 쓰는 코드 줄이 없다 (= 모름, 결과서 4번).

#### 아직 넓은 빈 곳

- `.data` 438개 중 **문서에 이름도 없는 것 343개**, 이름은 나오는데 쓴 자리를 못 찾은 것 48개 (주로 읽기만 나옴).
- 도구 메모 (run 문서): `_refs` **WRITE 줄만으로는** API 에 주소를 넘겨 쓰는 전역 (락·이벤트) 이 안 잡힌다.
  - 예: `LdrpWorkQueueLock` 은 복제 길 WRITE 줄만 있었다. 만들기는 flow.md 2.6 에서 찾아 더했다 (2.11).
  - 그래서 Q78 은 DATA (`LEA`) 줄도 같이 본다.

### 2.10 callee 목록 — 15함수, 637줄

#### 왜 함수 목록이 필요한가

- 짐을 놓는 건 결국 **함수**다. 초기화 함수들이 부르는 함수 목록 = **짐을 놓는 손의 후보 목록**이다.
- 이번엔 그 후보마다 "우리 문서에 나온 적 있나" 만 표시했다. "짐을 놓나 / 잡일 / 안 타는 갈래" 로 가르는 건 Q78 이다.

#### 표 (대표만, run 문서 표와 같은 숫자)

| 함수 | 호출 줄 | 서로 다른 callee | flow.md 에 나옴 | run·결과서에 이름만 | **어디에도 없음** |
|---|---|---|---|---|---|
| nt `NtCreateUserProcess` | 83 | 61 | 29 | 32 | 0 |
| nt `PspAllocateProcess` | 144 | 105 | 25 | 40 | **40** |
| nt `PspSetupUserProcessAddressSpace` | 20 | 18 | 14 | 3 | 1 |
| nt `MmCreateTeb` | 5 | 4 | 2 | 1 | 1 |
| nt `PspInitializeThunkContext` | 20 | 15 | 9 | 5 | 1 |
| ntdll `LdrpInitializeProcess` | 169 | 109 | 67 | 28 | **14** |
| 나머지 9개 | | | | | 다 0 |

- 장부 줄 합: nt **432** + ntdll **205** = **637** (조각 점프 줄 포함).
- 겹침 빼고 서로 다른 이름: nt 252 / ntdll 133. 그중 flow.md 에 나옴 99 + 87, 이름만 110 + 32, **어디에도 없음 43 + 14 = 57**.
- "어디에도 없는" 57개는 `callees.tsv` 의 `doc=none` 줄에 이름이 있다. **커널 쪽 43개 중 40개가 `PspAllocateProcess`** 다 — 2.7 의 근거 4 와 같은 이야기다.

#### 세는 법 — 오케스트레이터 수와 다른 세 곳

- **조각 점프** = 함수 몸통의 일부가 멀리 떨어진 **조각**으로 가는 `JMP` (run11·run12 해설의 "조각"). 호출이 아니라서 "호출 줄" 에서 뺐다.
- 서로 다른 callee 를 셀 때는 `alias:` 접두사를 떼고, 간접 호출과 `FUN_…` 조각 이름은 뺐다.
- 오케스트레이터 수와 다른 3곳 (`MmCreatePeb` 16 · `PspAllocateThread` 40 · `PspInsertThread` 42) 은 **조각 점프 대상 `FUN_…` 을 callee 로 센 차이**다. 결과서 수가 1 ~ 3 작다.

#### 간접 호출 10줄 — 대상은 실행해 봐야 안다

- [간접 호출](../02_sprint/run9-create-user-process-해설.md#직접-호출과-간접-호출) ("냉장고 메모 보세요") 은 코드만 봐선 어디로 가는지 모른다.
- nt 5곳: `NtCreateUserProcess +0x5b2`·`+0x11bb`, `PspAllocateProcess +0x1215`·`+0x17bf`, `PspInitializeThunkContext +0x35b`.
- ntdll 5곳: `_LdrpInitialize +0x9a`, `LdrpInitializeProcess +0x17fc`·`+0x1b59`·`+0x1cdf`·`+0x1d16`.
- → Q77 에서 실제 대상을 본다.

#### 지도에 없는 함수 둘

- `PspUserThreadStartup`, `PspInitializeThunkContext` 는 **지도(`_map/`)에 없다** `[정적]`.
- 이유: `SwapContext` 의 `RET` 로 들어오는 길이라, `NtCreateUserProcess` 를 뿌리로 그린 지도에서 **안 닿는다** (run15).
  - 비유: 노선도를 "A 역 출발" 로만 그렸는데, 이 둘은 다른 문으로 들어오는 역이라 노선도에 안 잡힌다.
- 장부 줄은 예전 export 로 있어서 callee 목록은 만들 수 있었다. 지도 뿌리를 `PspUserThreadStartup` 으로 하나 더 그리는 건 Q78.

### 2.11 그 밖에 확인된 것

- `[정적]` **run10 PEB 표 이름 7줄이 PDB `_PEB` 칸 이름과 모두 맞는다.** → flow.md 2.1 의 PEB 칸 이름은 이제 `[정적]` (PDB).
  - 단 범위로 적은 두 줄 (`+0xb8 ~ +0xe0`, `+0x118 ~ +0x124`) 은 "그 안 칸마다 정말 쓰나" 가 아직 짐작이다 → 3.1 짐작 12칸.
- `[정적]` **`LdrSystemDllInitBlock` 블록 타입은 두 PDB 어디에도 없다** (`InitBlock` · `SystemDll` · `DLL_INIT` 로 찾음). run32 말이 맞았다.
  - 있는 것: `_PS_NTDLL_EXPORTS` 의 첫 칸 (블록 **주소**를 담는 `void*`), 열거형 `_SYSTEM_DLL_TYPE` · `_WOW64_SHARED_INFORMATION`.
  - 그래서 업무 수첩의 칸 이름 (`+0x98` 난수, `+0xa0` 완화 옵션, `+0xb8` CFG 비트맵 …) 은 **계속 `[추측]`** 이다.
  - 그 밖에 PDB 에 없는 타입 (오프셋으로만 적어야 함): `InitialTeb` · `PS_CREATE_INFO` · `PS_ATTRIBUTE_LIST` · 간선 칸 0x20바이트 (run23) 등 (7.6).
- `[정적]` **오프셋이 PDB 와 안 맞는 문서 줄은 없었다.**
  - 이름이 애매했던 곳 하나: 문서의 "`LoadCount` = -1". PDB 에서 항목(`_LDR_DATA_TABLE_ENTRY`)엔 `ObsoleteLoadCount` (`+0x6c`) 뿐이고, `LoadCount` 는 **연결 카드** (`_LDR_DDAG_NODE +0x18`) 칸이다. 그래서 짐작으로 붙였다. 문서가 틀렸다기보다 칸 이름이 애매했던 것 (정정 아님).
- `[정적]` **문서 오프셋을 `.asm` 로 다시 본 줄들** — "부르는 자리" 와 "칸에 쓰는 줄" 이 다른 곳이 있었다.
  - 예: `_EPROCESS.UniqueProcessId` (`+0x1d0`). 문서는 `ExCreateHandleEx` 를 부르는 자리 (ret `+0x2703`) 를 적었는데, **칸에 실제로 저장하는 줄은 `PspAllocateProcess +0x27e4`** 다.
  - 비유: "번호표 기계를 누른 자리" 와 "받은 번호를 장부에 적은 자리" 가 다르다. 짐 지도는 **적은 자리**를 쓴다.
  - 같은 식으로 맞춘 것: `CreateTime` `+0x27c4`/`+0x27dd`, PEB `FastPebLock` `+0x9e1`, `ProcessHeap` `+0xa39`, `LoaderLock` `_LdrpInitialize +0xfa`, `ProcessInitializing` `+0x107`/`+0x20f`, TEB `SameTebFlags` `+0xea`.
- **flow-writer 가 더한 6줄** — 결과서 대조엔 없었는데 flow.md 에 있던 쓴 자리 (이미 아는 칸에만 더해서 칸 수는 안 바뀜):
  - `_LDR_DATA_TABLE_ENTRY.DllBase` (DLL) ← 커널 `NtMapViewOfSection` (`LdrpMinimalMapModule +0x174`, run38)
  - `_LDR_DDAG_NODE.State` ← `LdrpNotifyLoadOfGraph +0x19` (7), `LdrpInitializeNode +0x1f`·`+0x2ba` (8 → 9), `LdrpInitializeGraphRecurse +0x6e` (-4) (run27)
  - `LdrpMapAndSnapWork` ← `LdrpEnableParallelLoading +0xe2 TpAllocWork`, `LdrpWorkQueueLock` ← `LdrpInitParallelLoadingSupport` 가 만듦 (run24)
- 하네스 제안 (고치지 않음): PDB 타입 → 칸 목록 TSV 를 만드는 스크립트 (`pdb_types.py` 같은 것) 를 스킬 쪽에 두자. 지금은 임시 스크립트라 다음 빌드에서 다시 짜야 한다 (plan 하네스 후보 (56)).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

- `runs/03_sprint/flow.md` 는 02_sprint 것을 **복사한 뒤** 몇 곳만 고쳤다 (plan Q76 끝 기록). 그림 1·2 는 그대로다.

| 절 | 전 (02_sprint) | 후 (03_sprint, run43) |
|---|---|---|
| 기준 | 정적 근거 run9 ~ run42 | `runs/03_sprint/` run43 추가. 칸 목록·전역 목록이 PDB 타입 레코드·공개 이름에서 나왔다는 메모, 전역 크기는 `[추측]`, 짐 지도 `baggage.md` 링크 |
| 2.1 `MmCreatePeb` PEB 표 | 오프셋 `[정적]`, 이름 `[추측]` (알려진 PEB 배치) | 이름 7줄이 PDB `_PEB` 와 **모두 맞음** → 칸 이름 `[정적]` (PDB). 범위 두 줄은 아직 짐작 |
| 2.1 `LdrSystemDllInitBlock` | run32: "PDB 에 이 타입이 없다" | run43 `[정적]`: 두 PDB 다 다시 찾아도 없음. 다음 이름까지 거리 `0x128` = 크기. 칸 이름은 **계속 `[추측]`** |
| 2.1 `MmCreateTeb` | `[공백]` 본문은 안 봤다 | `[정적]` 본문 처음 뽑음 (811바이트), callee 4개, call/ret 4줄 + 실패 길, TEB 대입 약 47줄. **칸별은 `[공백]`** → Q78 |
| 4장 공백 표 | 2.1 "`MmCreateTeb` 본문: TEB 에 무엇을 쓰나" | "(일부 해결, 정적)" + 남은 것 = 칸별 (Q78). 새 줄: 초기화 함수 callee 중 **어디에도 없는 57개**, 지도에 없는 함수 둘 → Q78 |
| 5장 근거 run | run42 까지 | run43 줄, "03_sprint 요청서 번호" 표 (run43 = sast/38, dast 없음) |

### 새 문서 — 짐 지도 `baggage.md`

- 이번 바퀴의 **주 산출물**이다. 흐름도(`flow.md`) 옆에 나란히 서는 두 번째 지도다.
  - 흐름도 = **직원 동선** (누가 누구를 어떤 순서로)
  - 짐 지도 = **개업 준비물 점검표** (무엇이 어디에, 누가 어떻게)
- 첫판이라 아는 칸만 3장에 있고, 나머지는 7장에 이름으로만 있다. 6장 공용 부품은 "아직 없음".
- 3장의 **경로** 칸은 흐름도의 함수 이름을 그대로 쓴다. 그래서 두 지도는 **이름으로 이어진다.**
  - 짐 지도에서 "이 칸은 `LdrpInitializeProcess +0x1c1`" 을 보면, 흐름도 2.4 에서 그 함수를 찾아가면 된다.
  - 양식 메모: 빌드끼리 비교하는 열쇠는 **이름** (함수 이름, PDB 칸 이름) 이고, 오프셋은 덧붙이는 정보다 (`baggage-template.md`).

### 큰 그림에서 어떻게 이어지나 — 다음 두 바퀴가 채우는 것

이번 바퀴로 **점검표의 줄(칸 이름)은 다 생겼다.** 남은 건 줄마다 체크하는 일이다. 2장 표의 열을 기준으로 보면 이렇다.

| 2장 열 | 지금 (run43) | Q77 사진이 하는 일 | Q78 쓴 자리·줄기가 하는 일 |
|---|---|---|---|
| 칸 수 | PDB 직접 칸 — **끝** | — | — |
| nt 가 씀 / ntdll 이 씀 | 문서에서 옮긴 181칸 · 54개 | 어느 칸이 **바뀌었나** 를 보여 줌 (주인은 안 알려 줌) | 쓰는 줄·경로를 찾아 **늘린다** |
| 다른 DLL | 0 | ② − ① 차이에서 **생길 수 있다** | 섞인 것의 **주인을 가른다** |
| 0 그대로 | 전부 "사진 전" | 처음 값과 같은 칸을 **센다** | — |
| 아직 모름 | 1,275칸 · 439개 | "바뀌었는데 주인 모름" 으로 **좁힌다** | **줄여 간다** |

- 출처: 2장 각주 ("다른 DLL 0 … 사진의 ② − ① 차이에서 생길 수 있다", "0 그대로 … 사진 전"), plan Q77 ("② − ① … KERNELBASE·KERNEL32·dll·TLS 가 쓴 것이 섞임 → Q78 에서 주인을 가름"), 결과서 ("바뀐 칸이 모름 줄이면 Q78 에서 주인을 가른다").

#### Q77 — 사진 (동적, 사용자가 VM 에서)

- **새 말 — 상태 사진**: 멈춘 순간의 덤프 (CLAUDE.md). 그 순간 메모리·객체에 무엇이 들어 있나를 통째로 떠 둔 것.
  - 비유: 개업 직전 가게 안을 **사진**으로 찍어 두기.
- **왜 필요한가**: 정적으로는 포인터로 쓰는 칸·간접 호출을 놓칠 수 있다. "빠짐없나" 는 사진으로 확인한다 (CLAUDE.md "순서").
- **언제 찍나** (plan Q77, 결과서 추천):
  - 사진 ① = `ntdll!LdrInitializeThunk` 입구 (rva `0xe1420`) — 유저 첫 명령 직전
  - 사진 ② = `ntdll!NtContinue` 입구 (rva `0x161610`, `LdrInitializeThunk +0x13` 이 부르는 것) — 은퇴 직전
  - **이름 주의**: 단계 ①·② 와 사진 ①·② 는 번호가 같아서 헷갈린다. 사진 ① = 단계 ① 이 **끝난** 순간, 사진 ② = 단계 ② 가 **끝나기 직전**. 그래서 **② − ① = ntdll 단계에서 바뀐 것**이다.
  - 커널 멈춤점은 모든 프로세스에 걸리니, test.exe 일 때만 서게 조건을 붙인다 (CLAUDE.md, dast-mentor 몫).
- **무엇을 찍나** — 가게 장부 한 장에서 시작해, **칸에 적힌 주소를 따라가며** 찍는다 (결과서 "Q77 에서 떠야 할 것", 대표만):
  - 커널: `_EPROCESS` → `ObjectTable`(`+0x300`) 핸들 표, `Token`(`+0x248`, 하위 4비트 지움), `VadRoot`(`+0x558`) 구획 장부, `SectionObject`(`+0x2a8`) → exe 섹션 묶음, `ImageFilePointer`(`+0x330`) 파일 객체. 첫 스레드는 `ThreadListHead`(`+0x370`), 트랩 프레임은 `KTHREAD.InitialStack`(`+0x28`) − 0x190.
  - 유저: `_EPROCESS.Peb`(`+0x2e0`) → PEB → `ProcessParameters`(`+0x20`) · `Ldr`(`+0x18`) → 명부 줄·연결 카드. TEB 는 `KTHREAD.Teb`(`+0xf0`). KUSD 는 고정 주소 `0x7ffe0000`. `PEB.ApiSetMap`(`+0x68`).
  - ntdll 구역: `.data` · `.mrdata` · `.00cfg` (rva = **ntdll 베이스 + 값**, 베이스는 실행마다 바뀜).
  - 비유: 장부 첫 장의 "안내판은 몇 번지" 칸을 보고 안내판으로, 안내판의 "서류는 이 방" 칸을 보고 서류로 … 줄줄이 찾아가 찍는다.
- **같이 볼 것**: 간접 호출 10줄의 실제 대상, 주소만 넘기는 전역 6개의 실제 값, 7.4 의 "블록 통째 복사" 22칸 = 부모 블록 값인지.
- **조심할 것**:
  - KUSD 의 시각 칸은 계속 변해서 ②에서 ①을 빼면 **소음**이다 → 한 번만 뜬다 (결과서).
  - 사진 ② 는 `NtContinue` **입구**라서, 그 안(`② 끝`)에서 커널이 트랩 프레임 4칸 (`Rip`·`Rsp`·`Rcx`·`Rdx`) 을 다시 쓰는 건 사진 ② 에 아직 안 들어 있을 것이다 `[추측: 멈추는 자리가 syscall 보다 앞이라서]`.
  - 덤프 원본은 커밋하지 않는다 (`ms/dump/`, CLAUDE.md).

#### Q78 — 쓴 자리·줄기 (정적)

- **새 말 — 줄기**: 짐 하나를 정하는 함수 사슬. 짐 지도에선 **줄기 위는 끝까지** 판다 — 더 부르는 게 없는 함수, 또는 syscall 까지 (CLAUDE.md "자세함").
  - 비유: 준비물 하나가 제자리에 놓이기까지 거쳐 간 **배달 경로**를, 마지막으로 손댄 사람(또는 창구)까지 따라가기.
  - **줄기 밖** callee 는 이름만 적는다 = 배달 길에 잠깐 들른 곁가지.
- **새 말 — 공용 부품**: 여러 줄기에 나오는 복사·할당·문자열·락 같은 함수. **한 번만** 끝까지 보고, 3장에서는 이름으로 가리킨다 (짐 지도 6장).
  - 비유: 여러 배달에 같이 쓰는 **공용 수레**. 수레 구조는 한 번만 살펴 두면 된다.
  - 후보는 6장에 이름만 있다: `RtlInitializeCriticalSectionEx`, `RtlRbInsertNodeEx`, `ObDuplicateObject`, `KeInitializeApc`, `KeInitializeEvent`, `LdrGetProcedureAddressForCaller`, `TpAllocWork` 등.
- **무엇을 채우나** (plan Q78, 짐 지도 7.7):
  - 2.7 의 커널 서랍들 — `PspAllocateProcess` callee 중 문서에 없는 40개, 안 뽑은 8개 본문
  - 2.8 의 `MmCreateTeb` TEB 대입 47줄 → 칸별 표
  - `LdrInitializeMrdata` 본문 (`_LdrpInitialize +0x14a`, 109바이트) — `.mrdata` 50개 중 44개 모름의 후보 주인
  - `.data` 문서에 없는 343개
  - 어디에도 없는 callee 57개를 "짐을 놓나 / 잡일 / 안 타는 갈래" 로 가르기
  - 지도 뿌리 `PspUserThreadStartup` 하나 더
  - flow.md 2.5 ~ 2.10 을 **줄 단위로** 다시 대조 (2.7 끝의 유저 쪽 빈 곳)
- 한 바퀴에 너무 크면 그 자리에서 나눈다 (예: 구조체별 Q78a · Q78b …, plan Q78).

#### 왜 사진이 먼저인가

- plan 은 Q76 → **Q77** → Q78 순서다.
- 결과서는 "② − ① 에서 바뀌는 ntdll 전역은 쓴 자리를 아는 54개가 먼저 후보다. 바뀐 칸이 `모름` 줄이면 Q78 에서 주인을 가른다" 고 썼다.
- 그래서 → 사진이 "**바뀐 칸 / 그대로인 칸**" 을 먼저 갈라 주면, Q78 은 "**바뀌었는데 주인을 모르는 칸**" 부터 팔 수 있다 `[추측: plan 순서와 결과서 문장에서 읽은 이유]`.
- 비유: 서랍 1,275개를 다 열어 보기 전에, 사진으로 "물건이 든 서랍" 부터 표시해 두는 것.

#### 그다음

- 세 바퀴로 목록이 다 차면, **어디까지 구현할지**를 사용자와 정한다 (CLAUDE.md "지금 목표"). 그 전엔 "구현할지" 로 칸을 빼지 않는다.
- 남은 대기열 Q59 ~ Q75 는 짐 지도 뒤 원래 순서대로 간다. 겹치는 항목 (Q62 · Q63 · Q67 · Q72 등) 은 짐 지도 바퀴에서 만날 때 사용자에게 묻는다 (plan 결정 기록).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| "0 그대로" 칸이 몇 개인가 (2장 열 전부 "사진 전") | "아무도 안 쓴다" 는 정적으로 못 보인다 | **Q77** (동적) 사진 ①·② |
| 모름 1,275칸 · 439개 중 실제로 **바뀌는** 것 | 정적 대조는 문서에 적힌 것만 옮겼다 | **Q77** (바뀌나) → **Q78** (누가) |
| 커널 객체 칸의 쓴 자리 (`_KPROCESS` · `_HANDLE_TABLE` · `_MMVAD*` · `_TOKEN` · `_SECTION` · `_FILE_OBJECT` 아는 칸 0, `_EPROCESS` 241, `_KTHREAD` 227) | `PspAllocateProcess` 를 14묶음으로만 읽음, callee 본문 대부분 안 뽑음 (2.7) | **Q78** (정적) |
| `MmCreateTeb` 의 TEB 대입 약 47줄이 어느 칸인가 | 이번 범위는 체크리스트까지 (요청서 "범위 밖") | **Q78** (정적) |
| `.mrdata` 50개 중 44개의 주인 (`LdrpMrdataBase` 등) | `LdrInitializeMrdata` 본문을 아직 안 읽음 (run17 `[공백]`) | **Q78** (정적) |
| `.data` 문서에 이름도 없는 343개, 이름만 나오는 48개 | 문서·`_refs` 에 쓴 줄이 없음. 락·이벤트처럼 주소 넘기는 전역은 WRITE 줄로 안 잡힘 | **Q78** (줄 찾기) · **Q77** (바뀌나) |
| 어디에도 없는 callee 57개가 짐을 놓나 / 잡일인가 | 이번엔 "문서에 나왔나" 만 표시 | **Q78** (정적) |
| 간접 호출 10줄의 실제 대상 | 대상이 실행 때 값 | **Q77** (동적) |
| 주소만 넘기는 전역 6개의 실제 값 | 받은 함수가 쓴다 — 값은 실행 때 | **Q77** (동적) |
| "블록 통째 복사" 22칸이 부모 블록 값 그대로인가 | 칸 단위로 말한 문서가 없음 (7.4) | **Q77** (동적) |
| 유저 쪽 뒤 절 (flow.md 2.5 ~ 2.10) 에서 빠진 쓴 자리 | 이번 대조는 그 절을 검색으로만 봄 | **Q78** (정적) |
| `PspUserThreadStartup` · `PspInitializeThunkContext` 아래 깊이·caller | 지도 뿌리가 `NtCreateUserProcess` 하나뿐 | **Q78** (지도 뿌리 하나 더) |
| PDB 에 없는 타입의 칸 이름 (`LdrSystemDllInitBlock` 블록 등) | 공개 PDB 에 타입이 없다 | 오프셋으로만 적는다 (7.6). plan Q78 의 출처 줄에 있음 |
| KUSD 를 누가 쓰나, 커널 쪽 별칭 주소 | ntdll 은 읽기만, 쓰는 쪽 근거 문서 없음 `[공백]` | 따로 번호 없음 (결과서 Q77 추천 표에 `[공백]` 으로 남음, 짐 지도 7.6) |
| 다른 DLL (KERNELBASE·KERNEL32·dll·TLS) 이 쓴 칸 | 문서에 그런 쓴 자리가 아직 없음 (2장 "다른 DLL 0") | **Q77** ② − ① → **Q78** 이 주인 가름 |
| 공용 부품 (6장) | 끝까지 본 공용 부품이 아직 없음 | **Q78** |

---

## 5. 더 읽기

- run 문서: [`run43-baggage-checklist.md`](run43-baggage-checklist.md) — "결과" 1 ~ 4, "아직 확인 필요", "도구 메모"
- 짐 지도 첫판: [`baggage.md`](baggage.md)
  - "쓰는 말" (이 글 2.2), 1장 공간 그림 · 2장 구조체별 요약 (2.4), 3장 짐 표 (2.5), 7장 빈 곳 (2.6)
  - 4장 두 공간 짝 (유저 칸 ↔ 커널 칸이 맞아야 하는 짝), 5장 단계 순서 (코드 순서로 본 짐 놓기 차례)
- 결과서: [`_workspace/sast/38_baggage-checklist_result.md`](../../_workspace/sast/38_baggage-checklist_result.md) — 질문별 답, "표 파일 경로와 열 설명", "Q77(사진)에서 떠야 할 것 — 추천", "새로 생긴 공백"
- 요청서: [`_workspace/sast/38_baggage-checklist.md`](../../_workspace/sast/38_baggage-checklist.md) — 질문 넷, 범위 밖
- 흐름도: [`flow.md`](flow.md) 2.1 `MmCreatePeb` · `PspPrepareSystemDllInitBlock` · `MmCreateTeb`, 4장 공백 표, 5장 근거 run · "03_sprint 요청서 번호"
- 진행표: `_workspace/plan.md` Q76 (끝) · Q77 · Q78, 결정 기록 2026-10-24
- 앞 해설 (이번 칸들이 처음 나온 곳): [run10](../02_sprint/run10-allocate-process-해설.md) (PEB) · [run11](../02_sprint/run11-allocate-thread-해설.md) (TEB, `MmCreateTeb` 가 불리는 자리) · [run31](../02_sprint/run31-image-section-해설.md) (이미지 정보) · [run32](../02_sprint/run32-params-initblock-해설.md) (개업 서류, 업무 수첩) · [run35](../02_sprint/run35-ntdll-entry-해설.md) (명부 줄)
