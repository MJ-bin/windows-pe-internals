# run49 kprocess-token-htable — 해설

> 이 글은 [run49 문서](run49-kprocess-token-htable.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78c1** — Q78c (프로세스 객체, 모르는 칸 약 378) 를 나눈 **첫 조각**: `_KPROCESS`·`_TOKEN`·`_HANDLE_TABLE` 137칸 (`_workspace/plan.md`, 이번에 **끝**)
  - 나눈 법 (오케스트레이터 기본값 2026-10-29): 구조체별, `PspAllocateProcess` 안 호출 순서대로. Q78c1 = 이번 셋, Q78c2 = `_EPROCESS` 241칸.
  - 다음 항목: **Q78c2** (`_EPROCESS`) → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 동적 질문: D1·D4·D5 는 **이미 있는 덤프로 답할 수 있어서** 원래 있던 **Q79** 에 출처로 붙였다. D2·D3·D6 은 실행 중 멈춤이 필요해서 새 항목 **Q96** 으로 맨 뒤에 붙었다.
- 근거:
  - run 문서 [`run49-kprocess-token-htable.md`](run49-kprocess-token-htable.md) (가장 중요)
  - 요청서 [`_workspace/sast/43_kprocess-token-htable.md`](../../_workspace/sast/43_kprocess-token-htable.md)
  - 결과서 [`_workspace/sast/43_kprocess-token-htable_result.md`](../../_workspace/sast/43_kprocess-token-htable_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: "쓰는 말" (`입력 (부모 토큰 복제)`) · 2장 · 3.11 · **3.12 · 3.16 · 3.19 새로** · 5장 · 7.2 · 7.4 · 7.6 · 7.7 · 7.8 · 새 7.12
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 트리와 `PspAllocateProcess`·`PspInsertProcess` 절 · 2.2 `KeStartThread` 절 · 2.3 `PspUserThreadStartup` 절 · 4장 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값을 맞춰 보려고 run44 K1 덤프 읽기 로그 (`K1 01c`·`K1 04`·`K1 05s`) 와 EPROCESS raw 8장 비교를 **다시 읽었다**. 새로 VM 을 돌린 건 없다.
  - 그래서 이 글의 "사진" 은 전부 run44 때 찍어 둔 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 핸들(**손잡이**) → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절 · [핸들](../02_sprint/run9-create-user-process-해설.md#핸들)
- `_EPROCESS` (관리사무소의 **가게 장부**), [PEB](../02_sprint/run10-allocate-process-해설.md#peb) → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- 상가 명부 (`PsActiveProcessHead`, `PspInsertProcess` 가 손으로 직접 적는 목록) → [run11 해설 2.8](../02_sprint/run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- `KeStartThread` (**근무지·근무 시간 정하기**) → [run12 해설 2.9](../02_sprint/run12-insert-thread-해설.md#29-kestartthread-와-kereadythread-는-뭐가-다른가)
- `PspDisablePrimaryTokenExchange` (출근 도장 창구의 "**신분증 바꿔치기 금지**") → [run15 해설 2.8](../02_sprint/run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸)
- 토큰 = 가게의 **신분증** ("부모가 새 가게의 신분증과 보안 등급을 대신 들고 온다") → [run31 해설 2.6](../02_sprint/run31-image-section-해설.md#26-mmcreatespecialimagesection-이-특별한-이유)
- 짐 · 짐 지도 · 줄기 · 공용 부품, "쓰는 말" (공간 · 단계 · 놓는 법 · 쓴 주인 · `입력`) → [run43 해설 2.2](run43-baggage-checklist-해설.md#22-읽기-0단계--짐-지도-맨-위-쓰는-말)
- 사진 지점 K1 · K2 · K3 · P0 …, "처음 값을 갖는 구간" (`K1 이전` · `끝까지 0`) → [run44 해설 2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)
- 핸들 표 주인 PID 가 K2~K3 에 바뀐다는 사진 → [run44 해설 2.14 (2)](run44-baggage-snap-해설.md#2-핸들-표의-주인-pid-가-k2k3-에-바뀐다)
- "0" 의 세 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀) → [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀)
- 객체 만들기 `ObCreateObjectEx` (**공용 서류철 발급 창구**) → [run47 해설 2.5](run47-section-chain-해설.md#새-말-객체-만들기-obcreateobjectex)
- 빠른 참조 자리 (`객체 | 0xf`, **번호표 15장**) → [run48 해설 2.6](run48-system-dll-boot-해설.md#새-말-빠른-참조-자리)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 짐 = **개업 준비물**.
- `_EPROCESS` = 관리사무소가 가게마다 두는 **가게 장부** (run10).
- 프로세스 목록 = **상가 명부** (run11), 스레드 = **직원**, `KeStartThread` = 근무지·근무 시간 정하기 (run12).
- 토큰 = 가게의 **신분증** (run31), 핸들 = **손잡이**, 핸들 표 = 가게의 **손잡이 목록** (run9 · run43 · run44).
- cmd.exe = test.exe 를 낳는 **부모 가게**.
- run 문서가 이번에 쓴 비유 ("가게 운영 카드", "출입증", "열쇠 꾸러미") 는 이 글에서 이렇게 바꿔 부른다:
  - **출입증 → 신분증.** run15·run31 해설이 이미 토큰을 신분증이라고 불렀다. 그리고 run24 해설에선 "출입증" 이 ntdll 안 다른 것이었다. 섞이면 헷갈린다.
  - **열쇠 꾸러미 → 손잡이 목록.** run24 해설에선 "열쇠" 가 잠금이었다. 핸들은 처음부터 손잡이였다.
  - **가게 운영 카드** (`_KPROCESS`) 는 그대로 쓴다. 다만 이건 따로 떨어진 카드가 아니라 **가게 장부의 맨 앞 몇 쪽**이다. `_EPROCESS` 맨 앞 칸 `Pcb` 가 바로 `_KPROCESS` 다 (`0x1c8` 바이트, 짐 지도 3.12 제목).
- 이 글이 더하는 비유는 새 말이 나올 때 하나씩 붙인다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 세 구조체 모르는 칸 | **137 → 여전히 모름 0** (짐 지도로 nt 가 씀 97 · 아무도 안 씀 40) |
| 토큰 (신분증) 은 부모 것을 **같이 쓰나, 복사하나** | **복사한다.** `SeSubProcessToken +0x43c → SepDuplicateToken` 이 **새 토큰 객체**를 만들고 칸을 옮겨 적는다 `[정적]` |
| 핸들 표 (손잡이 목록) 는 **새로 만드나, 부모 것을 복사하나** | **부모 표 복사 길.** `ObInitProcess +0xcc ExDupHandleTable`. 사진 `Flags` = `0x18` 로 **확정** `[정적]` + `[관찰]` |
| 핸들 표 "주인 PID" 가 부모 → 자기로 바뀌는 줄 | 처음 = `ExpAllocateHandleTable +0x7c` (지금 도는 프로세스 = 부모 cmd), 바꾸기 = **`PspInsertProcess +0x4c`** — run44 의 `[공백]` 닫힘 `[정적]` |
| `_KPROCESS` (운영 카드) 를 채우는 곳 | 거의 **`KeInitializeProcess` 한 함수** (`PspAllocateProcess +0x1719`). 우선순위·퀀텀·`DirectoryTableBase` 는 따로 덮어 씀 `[정적]` |
| K2~K3 에 바뀌는 운영 카드 칸 | 첫 직원을 달 때 `KeStartThread` 가 쓴다 (`ProcessListEntry`·`ThreadListHead`·`StackCount`) `[정적]` |
| K3~P0 의 `PpmPolicy` 7 | 쓰는 줄 `KeSetProcessPpmPolicy +0x43`·`+0x4b` `[정적]`. 부르는 쪽은 nt 밖 BAM `[추측]` |
| 정정 | `PspDisablePrimaryTokenExchange` 는 토큰 칸을 **안 쓴다** (짐 지도 3.19). 짐 지도의 `+0x171e`·`+0x1927`·`+0x1991` 은 ret 이라 call 로 고침 |

그리고 이 글에서 가장 붙잡아 둘 한 줄:

> **새 가게는 부모 cmd 의 신분증과 손잡이 목록을 "복사해서" 받는다.**
> 복사하는 손은 nt 지만, **값은 부모에게서 온다.**
> 그래서 부모가 없는 에뮬레이터는 이 값들을 **입력으로 넣어 줘야 할 것 같다** `[추측]` (이유는 2.14).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간은 **부모 cmd 스레드 안, 커널**이다. `NtCreateUserProcess` 가 새 가게 장부를 만드는 `PspAllocateProcess` 의 한가운데다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0x41c  PspReferenceTokenForNewProcess    어느 신분증을 베낄지 고르기             ← 이번
   ├─ +0xc36  PspAllocateProcess                가게 장부 만들기
   │    ├─ +0x564  memset                       장부를 통째로 0 으로 밀기               ← 이번
   │    ├─ +0x163c MmCreateProcessAddressSpace  (땅 대장 첫 장 → DirectoryTableBase)   ← 이번 (한 칸)
   │    ├─ +0x171e KeInitializeProcess          운영 카드 채우기 (_KPROCESS)           ← 이번
   │    ├─ +0x1927 PspInitializeProcessSecurity 신분증 복사해 붙이기 (_TOKEN)          ← 이번
   │    ├─ +0x1991 ObInitProcess                손잡이 목록 복사 (_HANDLE_TABLE)       ← 이번
   │    └─ +0x1a64 …                            우선순위·퀀텀 덮어 쓰기                ← 이번
   ├─ +0xfc2  PspInsertProcess                  상가 명부에 올리기 (+0x4c 주인 PID 고침) ← 이번
   └─ +0x101a PspInsertThread → KeStartThread   첫 직원 달기 (운영 카드 칸 셋)         ← 이번
      ⋮  (그 뒤 어디쯤 BAM 이 PpmPolicy 를 씀 [추측])
 test.exe 메인 스레드: PspUserThreadStartup → ntdll 로더 … NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (콜스택에서 보이는 숫자). 표는 run 문서 "호출 관계" 그대로.
- 사진 지점으로 보면 거의 다 **`K1 이전`** 이다. K1 = `PspAllocateThread` 입구, 즉 `PspAllocateProcess` 가 끝난 직후다. 몇 칸만 `K2~K3` (명부·직원 달기 때), `K3~P0` (그 뒤) 에 바뀐다.

### 무엇이 궁금했나

run44 사진은 세 구조체에 대해 **"언제"** 만 알려 줬다.

- 칸 대부분이 `K1 이전` 에 값이 있다.
- 토큰 주소가 사진 8장 모두 **같다** (`ffffb689ae6b6060`).
- 핸들 표의 주인 PID 가 `K2~K3` 에 부모 `0x19a8` → 자기 `0x1dd0` 로 바뀐다.

그래서 요청서 질문은 다섯이었다.

1. `_KPROCESS` 65칸: 칸마다 누가 쓰나. `DirectoryTableBase` 는 어디서 오나. 뒤에 바뀌는 칸은 누가 쓰나.
2. `_TOKEN` 55칸: **새 가게 신분증은 부모 것을 복사하나, 같은 걸 같이 쓰나?** (사진 8장이 같은 주소라서 생긴 궁금증)
3. `_HANDLE_TABLE` 17칸: **새 표인가, 부모 표 복사인가?** 주인 PID 를 바꾸는 줄은 어디인가.
4. 칸마다 짐 지도 3장 양식 (단계 · 무엇을 · 놓는 법 · 쓴 주인 · 경로).
5. 갈림길과 입력: 부모·시스템 상태에 따라 갈리는 곳, 그리고 test.exe 는 어느 쪽인가.

- 범위 밖: `_EPROCESS` 칸 (Q78c2), 핸들 표 **안의 항목** 내용, 토큰 안 SID·권한 배열 내용, 페이지 표 내용.

### 바퀴 전에 알던 것

- `[정적]` (sast/04 · run10) `PspAllocateProcess` 가 `+0x1719 KeInitializeProcess`, `+0x1922 PspInitializeProcessSecurity`, `+0x198c ObInitProcess` 를 부른다. **이름과 자리만**, 본문은 안 봤다.
- `[정적]` (run47) "0 그대로" 를 셋으로 가르는 방법 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀).
- `[관찰]` (run44) 위 "무엇이 궁금했나" 의 세 가지. 핸들 수 4·4·4·4·4·26·44·46.
- `[추측]` 토큰은 부모 것을 복제한다 — 함수 이름 `SeSubProcessToken` 으로 본 짐작.
- 짐 지도에서 세 구조체는 **아는 칸 0** 이었다 (3.12·3.16·3.19 가 비어 있었다).

---

## 2. 찾은 것

### 2.1 한눈에: 세 덩어리, 세 사슬

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret, 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄. 모두 `[정적]`.

```
NtCreateUserProcess
 ├─ +0x41c  PspReferenceTokenForNewProcess         입력 신분증 = 부모 cmd 주 토큰 (+0x64)
 ├─ +0xc36  PspAllocateProcess
 │    │  +0x539 ObCreateObjectEx → +0x564 memset   (장부 몸통 + 운영 카드 확장 메모리 0)
 │    ├─ +0x171e KeInitializeProcess                운영 카드 거의 다
 │    ├─ +0x1927 PspInitializeProcessSecurity
 │    │    ├─ +0xfc  SeSubProcessToken
 │    │    │    └─ +0x441 SepDuplicateToken          새 토큰 객체 + 칸 복사 + 새 TokenId
 │    │    └─ +0x115 ObInitializeFastReference       EPROCESS.Token = 새 토큰 | 0xf
 │    ├─ +0x1991 ObInitProcess
 │    │    └─ +0xd1  ExDupHandleTable                부모 표 복사 (+0xa6 Duplicated 비트)
 │    │         └─ +0x64 ExpAllocateHandleTable     +0x7c 주인 PID = 지금 도는 프로세스 (cmd)
 │    └─  +0x1a64 · +0x1a72                         BasePriority · QuantumReset 덮어 씀
 ├─ +0xfc2  PspInsertProcess                         +0x4c 주인 PID = 자기 PID
 └─ +0x101a PspInsertThread
      └─ +0x5e2 KeStartThread                        ProcessListEntry · ThreadListHead · StackCount
```

비유로 한 번:

- 관리사무소가 새 가게 장부를 **빈 종이로** 받는다 (`ObCreateObjectEx` + `memset`).
- 장부 맨 앞 **운영 카드**를 한 사람 (`KeInitializeProcess`) 이 거의 다 채운다.
- 부모 가게 신분증을 **복사기로 한 장 새로 뽑는다** (`SepDuplicateToken`). 일련번호는 새로 매긴다.
- 부모 손잡이 목록에서 **"물려줘도 됨" 표시가 있는 손잡이만** 골라 새 목록에 옮겨 적는다 (`ExDupHandleTable`).
- 목록 맨 위 "주인" 칸엔 처음에 **베껴 적던 사람 (부모)** 번호가 들어가고, 상가 명부에 올릴 때 자기 번호로 고친다 (`PspInsertProcess`).
- 첫 직원이 들어오면 운영 카드의 직원 칸이 채워진다 (`KeStartThread`).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 신분증을 복사하는 줄

`| nt!SeSubProcessToken | +0x43c | +0x441 | nt!SepDuplicateToken (새 토큰 객체 + 칸 복사) | [정적] |`

- **누가**: `SeSubProcessToken` (자식 가게용 신분증을 만드는 함수)
- **어디서**: 자기 몸 `+0x43c` 에서 부른다. 돌아올 자리는 `+0x441` — 콜스택엔 `SeSubProcessToken+0x441` 로 보일 것이다.
- **누구를**: `SepDuplicateToken`
- **무엇을 위해**: 부모 신분증을 **새 종이에** 옮겨 적으려고. 이 함수가 `+0x158 ObCreateObjectEx` 로 새 토큰 객체를 받는다.
- 이 줄 앞에 갈림길이 둘 있다 (2.4). test.exe 는 둘 다 통과해서 이 줄로 온다.

#### 한 줄 읽기 ②: 주인 PID 를 자기 것으로 고치는 줄

run 문서가 인용한 명령 한 줄 (`PspInsertProcess +0x4c`):

```
MOV dword [R10+0x28],EAX
```

- **누가**: `PspInsertProcess` (상가 명부에 올리는 함수, run11)
- **어디서**: `+0x4c`. 함수 입구 바로 뒤, **분기보다 앞**이다.
- **무엇을**: `R10` = 이 가게 장부의 `ObjectTable` 칸 (`+0x300`) 이 가리키는 핸들 표 (`+0x27` 에서 읽음). 그 표의 `+0x28` 칸 = `UniqueProcessId`. 거기에 `EAX` = 장부의 `UniqueProcessId` (`+0x1d0`, 자기 PID, `+0x31` 에서 읽음) 를 쓴다.
- **무엇을 위해**: 손잡이 목록 "주인" 칸을 부모 번호에서 **자기 번호**로 고치려고.
- 사진과 맞는 까닭: K2 는 `PspInsertProcess` **입구**라 아직 이 줄 전 → `0x19a8`. K3 는 지난 뒤 → `0x1dd0`.

#### 한 줄 읽기 ③: 복사 길이라는 "도장" 을 찍는 줄

`ExDupHandleTable +0xa6` `OR byte [RCX+0x2c],8` `[정적]`

- **누가**: `ExDupHandleTable` (부모 핸들 표를 복사하는 함수)
- **어디서**: `+0xa6`.
- **무엇을**: 새 핸들 표 `Flags` 칸 (`+0x2c`) 의 bit 3 (`Duplicated` = "복사로 만든 표") 을 켠다.
- **무엇을 위해**: 이 표가 복사본이라는 표시. 그리고 **이 비트를 켜는 줄은 여기 하나뿐**이다 (빈 새 표를 만드는 `ExCreateHandleTable` 길엔 없다).
- 그래서 이 줄 하나가 "test.exe 는 어느 길이었나" 를 사진으로 가르는 열쇠가 된다 → 2.7.

### 2.3 137칸은 어디로 갔나

#### 표 (run 문서·결과서 맨 앞 표 그대로)

| 구조체 | 모르는 칸 | 아는 칸 (nt) | 0 그대로 · 줄 있음 | 아무도 안 씀 | 여전히 모름 | (참고) 부모 토큰에서 복사 |
|---|---|---|---|---|---|---|
| `_KPROCESS` (운영 카드) | 65 | 19 | 10 (0 을 씀 3 · 조건부 6 · 되돌림 1) | 36 | 0 | — |
| `_HANDLE_TABLE` (손잡이 목록 머리) | 17 | 10 | 3 (0 을 씀 2 · 조건부 1) | 4 | 0 | — |
| `_TOKEN` (신분증) | 55 | 29 | 26 (0 을 씀 15 · 값 0 인 복사·계산 4 · 조건부 7) | 0 | 0 | 16 (+ 값 0 인 복사 3) |
| **합** | **137** | **58** | **39** | **40** | **0** | |

- "아는 칸 (nt)" = 사진에서 값이 있거나 바뀐 칸이고, 쓰는 줄을 찾았다.
- "0 그대로 · 줄 있음" = 사진 값은 0 인데 쓰는 줄은 있다. "되돌림" 은 [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀) 의 셋에 더해진 넷째 꼴이다: **잡았다 놓아서 0 으로 돌아온 칸** (`ProcessLock`, 2.11).
- "부모 토큰에서 복사" 는 따로 센 열이 아니다. "아는 칸" 안에 들어 있다.

#### 한 줄 읽기: `_TOKEN` 줄

- 55칸 중 **"아무도 안 씀" 이 0** 이다. 신분증은 칸 하나하나에 쓰는 줄이 다 있다.
- 사진에서 값이 있던 29 = 부모 신분증에서 **옮겨 적은 16** + nt 가 **새로 정하거나 계산한 13**.
- 사진에서 0 이던 26 도 전부 쓰는 줄이 있다 (일부러 0 을 씀 15 · 옮겨 적었는데 원래 0 이던 4 · 조건이 안 맞아 지나친 7).

#### 짐 지도로 옮길 때 (run47 과 같은 규칙)

- "아는 칸" 58 + "0 그대로 · 줄 있음" 39 = **97** → 짐 지도 "nt 가 씀".
- "아무도 안 씀" **40** → 짐 지도 2장 표 아래 줄.

| 구조체 | nt 가 씀 | 아무도 안 씀 | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|
| `_KPROCESS` | 0 → **29** | **36** | 65 → **0** | 27 + 2 |
| `_HANDLE_TABLE` | 0 → **13** | **4** | 17 → **0** | 13 + 0 |
| `_TOKEN` | 0 → **55** | 0 | 55 → **0** | 53 + 2 |

- 짐작 넷은 이렇다 (run 문서):
  - `_KPROCESS` `ProcessFlags`·`ProcessFlags.PpmPolicy`: 쓰는 줄은 `[정적]` 인데 **부르는 쪽이 `[추측]`** (2.11).
  - `_TOKEN` `LogonSession`·`SidHash`: 쓰는 함수 **이름만** 봤다.
- 결과서는 `_KPROCESS` 를 "정확 18 · 짐작 1 (`Header.WaitListHead`)" 이라 했다. flow-writer 가 짐 지도 3장 규칙 ("**한 칸에 정확 줄이 하나라도 있으면 정확**") 으로 다시 세서 숫자가 조금 다르다. `Header` 칸은 `KeInitializeProcess +0x48` 줄이 정확이라 정확으로 셌다.
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 249 → **346** (정확 195 → **291** · 짐작 54 → **55**), 아무도 안 씀 11 → **51**, 모름 1,196 → **1,059**.
  - 늘어난 정확·짐작이 97 이 아니라 +96 · +1 인 까닭: `_EPROCESS` 의 이미 알던 칸 셋 (`Pcb`·`Token`·`ObjectTable`) 이 **짐작 → 정확**으로 올라갔다 (2.15).

### 2.4 신분증 (1): 어느 신분증을 베끼나, 그리고 어느 길로 가나

#### 새 말: 토큰 "복제" 와 "공유"

- **토큰** = 이 가게가 **누구 이름으로** 일하는지 적힌 신분증. 사용자·그룹·권한 같은 게 들어 있다 `[추측]` (일반적으로 알려진 설명. 이번 근거에선 `Privileges`·`UserAndGroups` 같은 칸 이름으로만 보인다).
- 자식 가게에 신분증을 주는 법은 두 가지가 있을 수 있다:
  - **공유** = 부모 신분증 **그 한 장을** 둘이 같이 들고 다닌다. 주소가 같다. 한쪽이 바꾸면 다른 쪽도 바뀐다.
  - **복제** = 복사기로 **새 종이 한 장**을 뽑는다. 내용은 거의 같지만 종이는 따로다. 주소가 다르다.
- 이번 질문은 "어느 쪽이냐" 였고, 답은 **복제**다 `[정적]`.

#### 입력: 베낄 원본을 고르는 곳 (`PspReferenceTokenForNewProcess`)

`NtCreateUserProcess +0x417` (ret `+0x41c`) 가 원본 신분증을 고른다 `[정적]`. 갈래가 셋이다.

1. 부르는 쪽이 **토큰 핸들 속성**을 줬으면 → 그 토큰 (`+0x3b ObReferenceObjectByHandle`).
2. 없으면 → **부모의 `EPROCESS.Token`** (`+0x64 PsReferencePrimaryTokenWithTag`, 태그 `'Dflt'`).
3. 부모도 없으면 → 전역 `PspBootAccessToken` (`+0x6e`).

- test.exe 는 **둘째 갈래 (부모 cmd 의 신분증)** 로 보인다 `[정적 + 추측]`.
  - "핸들 속성이 없었다" 는 건 아직 확인 아님 → 동적 D1.
- 비유: 새 가게 주인이 "이 신분증으로 해 주세요" 하고 따로 내밀면 그걸 쓴다. 안 내밀면 **부모 가게 신분증**을 원본으로 쓴다. 부모도 없는 가게 (부팅 때 생기는 것) 는 관리사무소 보관용 기본 신분증을 쓴다.

#### 복제 길로 가는 조건 (`SeSubProcessToken`)

두 관문을 통과하면 `+0x43c SepDuplicateToken` 으로 간다 `[정적 조건]`.

| 관문 | 줄 | 막히면 | test.exe |
|---|---|---|---|
| 자식 금지 제한이 없나 | `+0x117 ~ +0x12b` | (다른 길) | 통과 `[추측]` — `TokenFlags` `0x2a00` 로는 못 가림 |
| AppContainer 속성 (`[R15+0x20]`) 이 없나 | `+0x2f8 ~ +0x305` | `+0x347 SepCreateAppContainerToken` | 통과 — 사진 토큰 `Package` = 널 `[관찰 간접]` |

- `[추측]` 일반적으로 알려진 설명: AppContainer 는 스토어 앱 같은 **격리된 가게**용 신분증이다. 이번 근거에선 함수 이름과 `Package` 칸만 보인다.

### 2.5 신분증 (2): 왜 "새 종이" 라고 말할 수 있나

#### 근거 → 그래서 → 아직 확인 아님

1. 근거 `[정적]`: `SepDuplicateToken +0x158` 이 `ObCreateObjectEx` 로 **새 몸통**을 받는다. 크기 = 원본 `VariableLength` + `0x5c0`. 원본과 새 토큰을 **다른 레지스터로 따로** 다룬다 (`RDI` = 원본, `R14`/`RBX` = 새 토큰).
2. 근거 `[정적]`: 새 `TokenId` 를 **전역 번호 기계**에서 새로 뽑는다 (`+0x1a1 XADD.LOCK [ExpLuid]`) → 아래 "새 말: LUID".
3. 근거 `[정적]`: 새 자물쇠·새 서랍 셋을 따로 받는다 — `TokenLock` (풀 태그 `SeTl` + `+0x1d8 ExInitializeResourceLite`), `pSecurityAttributes` (풀 `SeAt`), `DynamicPart` (풀 `SeDT`).
4. 사진과 맞다 `[관찰]` (run44 K1): `TokenId` = `0x24c39e`. 원본 쪽에서 온 LUID 들 (`0x21d70 ~ 0x21d7c`) 과 **크게 다르다**. `TokenLock`·`pSecurityAttributes`·`DynamicPart` 도 서로 다른 풀 주소다.
5. 그래서 → **부모 cmd 의 신분증과 같은 주소일 수 없다** `[정적]`.
6. 그러면 사진 8장이 같은 주소 (`ffffb689ae6b6060`) 인 건? → **이 가게 안에서 신분증이 안 바뀐다**는 뜻이다. 부모와 같다는 뜻이 아니다.
7. 아직 확인 아님: 부모 cmd 신분증을 **실제로 열어서** 값을 맞춰 본 적은 없다 → 동적 D1 (덤프로, Q79).

#### 새 말: LUID 번호표

- **LUID** = "이 컴퓨터 안에서만 겹치지 않는 번호" `[추측]` (일반적으로 알려진 설명: Locally Unique ID. 결과서도 이 줄을 "`ExAllocateLocallyUniqueId` 를 인라인한 것" 이라고 적었다).
- 이번 근거에서 보이는 모양 `[정적]`: 전역 칸 `ExpLuid` 를 잠금 더하기 (`XADD.LOCK`) 로 하나 올리고 (`+0x1a1`), `ExpLuidIncrement` 를 더해 (`+0x1b1`·`+0x1bb`) 새 `TokenId` 로 쓴다.
- 비유: 관리사무소 창구의 **번호표 기계**. 누가 뽑든 한 장씩 올라가니 같은 번호가 두 번 안 나온다. 새 신분증 일련번호는 이 기계에서 뽑는다.
- 그래서 `TokenId` 는 부모와 **다르고**, 숫자도 원본 쪽 번호들보다 한참 뒤다.

#### 칸 셋 갈래 (결과서 2.2·2.3)

| 갈래 | 개수 | 칸 (대표) | 비유 |
|---|---|---|---|
| **부모에서 옮겨 적음** | 16 | `TokenSource` (`"User32"`) · `AuthenticationId` (`0x21d70`) · `ParentTokenId` (`0x21d73`) · `ModifiedId` (`0x21d7c`) · `Privileges` (5개) · `IntegrityLevelIndex` · `LogonSession` · `VariablePart` … | 이름·소속·권한을 그대로 베낌 |
| **새로 정함·계산** | 13 | `TokenId` (`0x24c39e`) · `TokenLock` · `SessionId` (`1`) · `TokenType` (`1`, Primary) · `TokenInUse` (`1`) · `SessionObject` · `UserAndGroups` (새 종이 안 자리로 고친 포인터) … | 일련번호·자물쇠·"사용 중" 표시는 새것 |
| **사진에서 0** | 26 | 0 을 씀 15 (`Package`·`Capabilities`·`TrustLevelSid` …) · 값 0 인 복사·계산 4 (`RestrictedSidCount` …) · 조건부 안 탐 7 (`AntiTamper*` 넷 · `TokenFlags2`·`IsInstaller`·`IsAgentic`) | 빈칸도 다 누가 비워 둔 건지 안다 |

조심할 것 둘 (run 문서):

- **`ParentTokenId` 는 원본의 `ParentTokenId` 를 그대로 옮긴다.** 원본의 `TokenId` 가 아니다. 이름만 보면 "부모 신분증 번호" 같지만, 실제로는 부모 신분증에 적혀 있던 "그 부모의 번호" 를 베낀다.
- **`LogonSession`·`IndexEntry` 는 같은 객체를 참조만 늘려 함께 쓴다.** 신분증 종이는 새것이지만, 거기 붙은 "로그인 기록 카드" 는 부모와 **같은 카드**를 가리킨다. 그러니 "복제" 안에도 **공유하는 부분**이 조금 있다.

#### 몸통은 0 으로 채워져 온다

- `ObpAllocateObject +0x191` 이 풀 종류에 "0 채움" 비트를 켜고, `ExAllocatePoolWithTag +0x48` 이 그 비트가 있으면 "초기화 안 함" 을 안 붙인다 `[정적]` (`.asm` 없음, `objdump` 로 읽음).
- 그래서 `SepDuplicateToken` 이 안 쓰는 칸은 0 이다. 신분증 칸 중 "아무도 안 씀" 이 0 인 건, 0 이 되는 칸마다 **일부러 0 을 쓰는 줄**까지 찾았기 때문이다.

### 2.6 신분증 (3): 장부에 붙이기 — 빠른 참조 `| 0xf`

- 새 신분증을 가게 장부에 붙이는 건 `PspInitializeProcessSecurity +0x110` (ret `+0x115`) `ObInitializeFastReference` 다 `[정적]`.
  - `+0x109` 가 `RCX` = 장부의 `+0x248` (`EPROCESS.Token`) 자리를 넘긴다.
  - 받은 함수가 신분증에 참조를 **15개 더** 얹고 (`+0x1e`), 그 칸에 **"토큰 주소 `| 0xf`"** 를 쓴다 (`+0x23 ~ +0x30`).
- 빠른 참조 자리는 [run48 해설 2.6](run48-system-dll-boot-해설.md#새-말-빠른-참조-자리) 에서 이미 풀었다 (**번호표 15장**). 이번에 새로 보이는 건 그 번호표가 **사진에서 줄어드는 모습**이다:
  - `EPROCESS.Token` 값: K1 `…606f` → P0 `…6066` → P2 `…6063` `[관찰]`.
  - 주소 부분 `…6060` 은 그대로고, **아래 4비트만** 줄었다. 결과서는 이걸 "빠른 참조 카운트가 줄어든 것 — 읽기 쪽" 이라고 적었다.
  - 그러니 짐 지도 3.19 "바뀜 없음" 은 맞다. 신분증 자체는 안 바뀌었다. 장부의 **꽂아 둔 번호표 장수**만 줄었다.
- 비유: 장부 신분증 칸에 신분증 **위치**와 함께 "**빌려 갈 수 있는 사본 확인증 15장**" 을 꽂아 둔다. 누가 신분증을 확인하러 오면 확인증 한 장을 뜯어 간다. 그래서 장수 (`f` → `6` → `3`) 만 줄어든다.
- 이 칸 (`EPROCESS.Token`) 자체는 `_EPROCESS` 몫이라 Q78c2 로 넘겼다. 다만 짐작 → 정확으로는 이번에 올랐다 (2.15).

### 2.7 손잡이 목록 (1): 새 표인가, 부모 표 복사인가

#### 새 말: 핸들 상속

- **핸들 상속** = 부모 가게가 쥔 손잡이 중 **"물려줘도 됨" 표시**가 있는 것만, 자식 가게 손잡이 목록에 **똑같이 옮겨 적는** 것.
  - 근거 `[정적]`: `ExDupHandleTable` 은 부모 표 항목마다 상속 비트 (`BT RAX,0x12`) 를 보고, **켜진 것만** 복사한다 (`+0x337 ~ +0x33f`).
  - `[추측]` 일반적으로 알려진 설명: `CreateProcess` 의 "핸들 상속" 인자를 켜면 이 길로 간다. 콘솔 프로그램이 부모의 입출력 손잡이를 물려받는 게 흔한 예다.
- 비유: 부모 가게 열쇠고리에 손잡이가 여럿 걸려 있다. 그중 **"자식에게 줘도 됨" 스티커**가 붙은 것만 똑같은 손잡이를 하나 더 만들어 자식 목록에 건다. 스티커 없는 건 안 준다.

#### 가르는 줄 `[정적]`

- `PspAllocateProcess +0x1978 ~ +0x1986` 이 **`ProcessFlags` 비트 2** 를 본다.
  - `ProcessFlags` = `NtCreateUserProcess` 의 7번째 인자.
  - 비트 2 이름 `PROCESS_CREATE_FLAGS_INHERIT_HANDLES` 는 알려진 값이라 `[추측]`.
- 비트가 꺼져 있으면 → `ObInitProcess` 첫 인자 = 0 → `+0x40 ExCreateHandleTable` (**빈 새 표**).
- 켜져 있으면 → 첫 인자 = 부모 → `+0xaa ObReferenceProcessHandleTable` (부모 표 잡기) → `+0xcc ExDupHandleTable` (**복사**).
- 두 길 다 끝에서 `ObInitProcess +0x53` 이 표 주소를 장부 `EPROCESS.ObjectTable` (`+0x300`) 에 넣는다.

#### test.exe 는 복사 길이다 — 단계별로

1. 근거 `[관찰]` (run44 K1): 새 핸들 표 `Flags` = `0x18`. bit 3 (`Duplicated`) = 1.
2. 근거 `[정적]`: 이 비트를 켜는 줄은 **`ExDupHandleTable +0xa6` 하나뿐**이다 (한 줄 읽기 ③). 빈 새 표 길엔 없다.
3. 그래서 → test.exe 는 **복사 길을 탔다.** 사진 + 코드로 **확정**.
4. 덤 `[정적]`: 손잡이 하나 옮길 때마다 `+0x3b1 INC [RCX+0x58]` 이 개수를 1 올린다. K1 핸들 수 4 = 이 줄이 4번 = **부모 cmd 표에서 스티커 붙은 손잡이 4개**.
5. 아직 확인 아님: 부르는 쪽이 "**이 손잡이들만**" 하고 목록 (개수·배열) 을 따로 줬는지. 목록이 비었으면 스티커 붙은 것 **전부**, 있으면 그중 **목록의 것만**이다 (`ExDupHandleTable +0x43`). 사진에선 인자가 이미 사라져서 못 가른다 → 동적 D2 (Q96). cmd 는 목록을 안 줄 것 `[추측]`.

- 4개 손잡이 **안의 내용** (무엇을 가리키는 손잡이인가) 은 범위 밖이다 (짐 지도 7.6 C11, Q79).

### 2.8 손잡이 목록 (2): "주인" 칸에 왜 처음엔 부모 번호가 들어가나

#### 처음 값을 쓰는 줄 `[정적]`

- `ExpAllocateHandleTable +0x7c` `MOV dword [RDI+0x28],EAX` (표 머리를 할당하고 칸을 채우는 함수).
- `EAX` 는 "**지금 도는 스레드의 프로세스**" 의 PID 다 (`GS:[0x188] → +0xb8 → +0x1d0`).

#### 왜 부모 번호인가 — 단계별로

1. 이 코드가 도는 곳은 **부모 cmd 스레드 안**이다. `NtCreateUserProcess` 는 cmd 가 부른 시스템 콜이니까 (1절 그림).
2. 그러니 "지금 도는 스레드의 프로세스" = **cmd**.
3. 그래서 → 새 표 "주인" 칸에 cmd PID `0x19a8` 가 들어간다 `[정적]`. run44 사진 K1·K2 값과 맞다 `[관찰]`.
4. 나중에 `PspInsertProcess +0x4c` 가 자기 PID `0x1dd0` 로 고친다 (한 줄 읽기 ②). K3 부터 그 값이다 `[관찰]`.

- 비유: 새 가게 손잡이 목록을 **부모 가게 직원이 대신 만들어 준다.** 목록 맨 위 "작성자" 칸에 무심코 **자기 가게 번호**를 적는다. 그리고 상가 명부에 올릴 때 관리사무소가 "주인은 이 가게" 로 고쳐 쓴다.
- 이 바퀴로 run44 이후 남아 있던 `[공백]` (flow.md 2.1 `PspInsertProcess` 절 · 4장 · 짐 지도 7.8) 이 닫혔다.

#### 나머지 머리 칸 (대표, 결과서 3.2)

| 칸 | 값 | 쓰는 줄 `[정적]` |
|---|---|---|
| `QuotaProcess` | 새 가게 자신 | `ExpAllocateHandleTable +0x5c` |
| `HandleTableList` | 전역 `HandleTableListHead` 꼬리에 달림 | `ExDupHandleTable +0x493 ~ +0x49d` |
| `NextHandleNeedingPool` | `0x400` (낮은 단계 표 하나 = 1024 핸들) | `ExpInsertLowLevelTableIntoFreeList +0xab` |
| `Flags` bit 4 (`RaiseUMExceptionOnInvalidHandleClose`) | 1 | `ExpAllocateHandleTable +0xc2` |
| `HandleTableLock`·`HandleContentionEvent` | 0 을 씀 | `ExpAllocateHandleTable +0xc6`·`+0xce` |

- 핸들 수가 4 → 26 → 44 → 46 으로 늘 때 `FreeLists`·`ActualEntry` 를 바꾸는 건 **핸들 만들기 공용 부품** (`ExCreateHandleEx`·`ObpCreateHandle`·`ObDuplicateObject` 등) 이다. 이름만 두었고, 어느 것이 어느 칸인지는 `[추측]`.

### 2.9 운영 카드 (`_KPROCESS`): 한 사람이 거의 다 채우고, 셋은 덮어 쓴다

#### 순서 `[정적]`

1. `PspAllocateProcess +0x55f memset` 이 장부 몸통 + 운영 카드 **확장 메모리**를 한꺼번에 0 으로 민다.
   - 확장 메모리 = 운영 카드 뒤에 덧붙인 추가 공간 (크기는 `KeSizeOfKProcessExtension` 결과). 친화도 (`_KAFFINITY_EX`) 같은 덩어리가 여기 산다.
2. `+0x1719 KeInitializeProcess` 가 칸 대부분을 쓴다. 인자 9개 (객체 · 처음 우선순위 `8` · 주 그룹 · 친화도 · 노드 · 스케줄링 그룹 · (안 씀) · 플래그 · 확장 메모리 시작).
3. 값이 정해진 뒤 **덮어 쓰는 곳**이 셋 있다 (아래).

- 사진 대조 `[정적]` + `[관찰]`: `KeInitializeProcess` 가 `KiMaximumGroups` (1) 기준으로 깐 확장 메모리 자리 5개 (`ActiveProcessors`·`Affinity`·`AvailableCpuState`·`IdealProcessorAssignmentBlock`·`AutoBoostState` 포인터) 가 K1 값과 **전부 같다.** 코드로 계산한 자리와 사진이 칸마다 맞았다.
- 확장 메모리 안 블록 셋 (`_KPROCESS_AVAILABLE_CPU_STATE`·`_KI_IDEAL_PROCESSOR_ASSIGNMENT_BLOCK`·`_KAB_UM_PROCESS_TREE`) 은 운영 카드 **칸이 아니다** → 짐 지도 7.6 (칸 표 없는 짐).

#### 새 말: 우선순위와 퀀텀

- **우선순위 (`BasePriority`)** = 스케줄러가 "누구 먼저 CPU 를 쓰게 할까" 정할 때 보는 **기본 점수** `[추측]` (일반적으로 알려진 설명).
- **퀀텀 (`QuantumReset`)** = 한 번 CPU 를 잡으면 **얼마 동안** 쓸 수 있나, 그 시간 조각의 기본값 `[추측]` (일반적으로 알려진 설명).
- 비유: 가게 직원들이 공용 작업대 (CPU) 를 돌아가며 쓴다. 우선순위 = **줄 서는 순번 등급**, 퀀텀 = **한 번에 쓸 수 있는 모래시계 크기**. 운영 카드엔 이 가게 직원들의 **기본 등급과 기본 모래시계**가 적힌다.

#### 덮어 쓰는 셋 `[정적]`

| 칸 | 처음 값 (`KeInitializeProcess`) | 덮어 쓰는 줄 | 최종 (K1) |
|---|---|---|---|
| `BasePriority` | 상수 `8` (`+0x6e`) | `PspAllocateProcess +0x1a64` ← `PspPriorityTable[PriorityClass]` | `8` (`PspPriorityTable[2]` = 8, PE 에서 읽음) |
| `QuantumReset` | 상수 `6` (`+0x1bf`) | `PspAllocateProcess +0x1a72` ← `PspComputeQuantum` 결과 | `2` |
| `DirectoryTableBase` | (안 씀) | `MiCreateNewProcessTopLevelMappings +0x6e` (`.asm` 없음) | `0x192c2e000` |

- `BasePriority` 는 처음 `8` 과 최종 `8` 이 같아서 사진만 보면 덮은 줄이 안 보인다. 코드로 봐야 "한 번 덮었다" 가 보인다.
- `QuantumReset` 의 표 `PspForegroundQuantum` 은 PE 파일 안에선 0 이고 K1 값 `2` 는 실행 때 값이다 (결과서). 그래서 정확히 왜 2 인지는 덤프로 전역을 읽어 봐야 한다 → D5.

#### 새 말: 페이지 표 맨 위 주소 `DirectoryTableBase`

- **`DirectoryTableBase`** = 이 가게 주소 공간의 **최상위 페이지 표가 놓인 물리 주소** `[정적]` (결과서: "최상위 페이지 쪽 번호 `<< 12`").
  - `[추측]` 일반적으로 알려진 설명: CPU 가 이 가게 코드를 돌릴 때 이 값을 CR3 에 넣고, 가상 주소 → 실제 메모리 길 찾기를 여기서부터 시작한다.
- 비유: 가게 **빈 땅의 지번 대장 첫 장**이 관리사무소 서고 몇 번 칸에 있나. 운영 카드엔 그 **칸 번호**만 적는다.
- 쓰는 곳: `PspAllocateProcess +0x1637 MmCreateProcessAddressSpace → +0x203 MiCreateNewProcessTopLevelMappings +0x6e` `[정적]`. 대장 첫 장을 받는 건 `MiAllocateTopLevelPage`.
- K1 값의 bit 1 이 꺼져 있다 → `KiFlushPcid` 0 `[관찰 간접]`. 페이지 표 **내용**은 범위 밖이다.

### 2.10 첫 직원이 올 때 바뀌는 칸 (`KeStartThread`, K2~K3)

- run12 해설은 `KeStartThread` 를 **근무지·근무 시간 정하기**라고 불렀다 (본문은 안 봤고 `[추측]`). 이번에 본문을 읽었더니, **가게 쪽 칸도 여기서 쓴다** `[정적]`:

| 칸 | 줄 | 하는 일 | 사진 |
|---|---|---|---|
| `ProcessLock` | `+0x99` 잡기 · `+0x38d` 놓기 | 아래 셋을 하는 동안 운영 카드 잠금 | 끝까지 0 (놓은 뒤라 0 으로 돌아옴) |
| `ProcessListEntry` | `+0x247 ~ +0x270` | **첫 직원**이고 `KiInitialProcess` 가 아니면 전역 `KiProcessListHead` 꼬리에 가게를 단다 | `K2~K3` |
| `ThreadListHead` | `+0x2f4 ~ +0x2fe` | 직원을 가게 직원 명단 꼬리에 단다 | `K2~K3`, `P1a~P1b` |
| `StackCount` | `+0x3e9 ADD.LOCK [RDI+0x108],8` | 직원 하나 몫 (+8) | `K2~K3` `0 → 8`, `P1a~P1b` `8 → 0x18` |

- 비유: 첫 직원이 출근하면 관리사무소가 운영 카드를 잠그고 → 가게를 **근무 배정용 가게 목록**에 올리고 → 직원 명단에 이름 적고 → "근무자 수" 칸을 올린 뒤 → 잠금을 푼다.
- `KiProcessListHead` 는 run11 의 상가 명부 (`PsActiveProcessHead`, `PspInsertProcess` 가 씀) 와 **머리 이름이 다른 목록**이다. 무엇에 쓰는 목록인지는 근거에 없다.
- `P1a~P1b` 의 `StackCount` `+0x10` (= 직원 둘) 은 ntdll 이 만드는 **일꾼 스레드** 몫으로 보인다 `[추측]` → Q78d.
- flow.md 4장 2.1 줄 "`KeStartThread` 본문 `[추측]`" 은 이걸로 **일부 해결**. 스레드 쪽 스케줄링 칸은 Q78d 에서 본다.

### 2.11 K3~P0 에 바뀌는 칸: `PpmPolicy` 와 대기 블록

#### 새 말: PPM 정책과 BAM

- **PPM** = 프로세서 전력 관리 (Processor Power Management) `[추측]` (일반적으로 알려진 설명). 결과서는 이 칸 값을 "전력 정책 값" 이라고 불렀다.
- **BAM** = 백그라운드 활동 조정자 (Background Activity Moderator), 드라이버 `bam.sys` `[추측]` (일반적으로 알려진 설명: 뒤에서 도는 앱이 전력·CPU 를 덜 쓰게 조절한다).
- 비유: 가게 운영 카드에 "**전기 절약 등급**" 칸이 있다. 관리사무소 본관이 아니라, 옆 건물 **절전 관리팀**이 와서 적는 것 같다.

#### 쓰는 줄은 확실하고, 부르는 쪽은 짐작이다 — 단계별로

1. 근거 `[정적]`: `ProcessFlags.PpmPolicy` (`0x380` = 정책 7) 를 쓰는 줄은 `KeSetProcessPpmPolicy +0x43 AND ECX,0xfffff87f` · `+0x4b CMPXCHG.LOCK [R14+0x88]`. PE 전체에서 이 마스크로 쓰는 곳은 **여기 하나** `[정적 스캔]`.
2. 근거 `[정적]`: 그걸 부르는 `PspSetProcessPpmPolicy` 는 **직접 부르는 곳이 0** 이다. 함수 표 `PspBamHostInterface +0x8` 에서만 가리켜진다.
3. 근거 `[정적]` (`.asm` 없음): 그 표는 `PspInitPhase1 +0xb3` 이 `ExRegisterHost(PspBamExtensionHost, …)` 에 넘긴다.
4. 그래서 → 표를 받아 가서 부르는 쪽은 **nt 밖 BAM 확장 (bam.sys)** 으로 보인다 `[추측]`.
5. 아직 확인 아님: 정말 bam.sys 인가, 어느 시점인가 (`PspCallProcessNotifyRoutines` 알림 안?) → 동적 D3 (Q96).

- 그래서 짐 지도에서 이 두 칸은 "정확" 이 아니라 **짐작** 이다. 쓰는 줄은 알지만 경로 머리가 `[추측]` 이라서다 (run47 `ReadAccess` 와 같은 꼴).
- 같은 함수 `+0xa5` 가 장부의 `BamPpmPolicy` (`+0x820`) 도 쓴다. 사진에서도 같은 `K3~P0` 구간이다 → Q78c2.

#### `Header.WaitListHead` 의 대기 블록

- `K3~P0` 에 이 칸이 자기 자신 대신 **대기 블록** (`ffff8285'98adc1c0`) 을 가리키게 된다 `[관찰]`.
- 뜻: 누군가 이 가게 객체를 **기다리기 시작했다**. 기다리는 쪽은 부모 cmd 스레드로 보인다 `[추측]`.
- 비유: 가게 문 앞 "**기다리는 사람 명단**" 에 한 명이 이름을 올렸다.
- 줄기 밖이다. 누구의 블록인지는 P0 덤프로 확인할 수 있다 → D4 (Q79).

### 2.12 끝까지 0 인 운영 카드 46칸

[run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀) 의 셋 + "되돌림" 하나 (결과서 1.6):

| 가름 | 개수 | 칸 (대표) | 까닭 `[정적]` |
|---|---|---|---|
| 0 을 씀 | 3 | `SchedulingGroup`·`PrimaryGroup`·`IdealGlobalNode` | 인자 값이 0 이라 0 을 적음 |
| 조건부 안 탐 | 6 | `AutoAlignment`·`ExtendedFeatureDisableMask`·`SecureState`·`AddressPolicy`·`UserCetLogging`·`UserDirectoryTableBase` | 조건이 안 맞아 지나침 (2.13 갈림길) |
| 되돌림 | 1 | `ProcessLock` | 잡았다 놓으면 0 으로 돌아오는 잠금 칸. 사진은 놓은 뒤 |
| 아무도 안 씀 | 36 | `CycleTime`·`KernelTime`·`UserTime`·`InstrumentationCallback`·`Spare*` … | 만들기 길에 쓰는 줄 없음. `memset` 0 그대로 |

- "아무도 안 씀" 36 중엔 **나중에 (실행 중) 스케줄러나 다른 시스템 콜이 쓸 칸**이 섞여 있다 `[추측]` — 이름만 봐도 `KernelTime`·`UserTime` 같은 시간 누적 칸이다. 사진 8장에선 0 이라 "**초기화 동안** 아무도 안 쓴다" 까지만 말한다.
- `ExtendedFeatureDisableMask` 는 "안 탔다" 와 "탔는데 읽은 값이 0" 을 아직 못 가른다 → D5.
- `PerProcessorCycleTimes` 값 `0x9300` 은 포인터가 아니라 `ExSaAllocate` 가 돌려준 "**슬랩 핸들**" 로 보인다 `[추측]` (그 값에서 비트를 꺼내 쓰는 줄 `+0x2eb ~ +0x335`).

### 2.13 갈림길: test.exe 는 어느 쪽 (대표)

run 문서 갈림길 표에서 대표만.

| 갈림길 | test.exe | 근거 |
|---|---|---|
| 핸들 상속 (복사) / 빈 표 (`ProcessFlags` 비트 2) | **복사** | 사진 `Flags.Duplicated` = 1 + 코드 |
| 상속 목록 속성 있음 (일부만) / 없음 (전부) | 전부 `[추측]` | 동적 D2 |
| 토큰 핸들 속성 / 부모 토큰 / 부팅 토큰 | 부모 토큰 `[추측]` | 동적 D1 |
| AppContainer 토큰 | **아님** (복제 길) | 사진 `Package` 널 (간접) |
| 작업 (Job) | **없음** | 사진 `Job` = 0 |
| CET 로그 | **아님** | 사진 raw `+0x754` = `0x40000000` |
| VBS 보안 프로세스 | **아님** | 사진 `SecureState` = 0 |
| WoW64 x86 | **아님** | 사진 raw `0x8664` |
| 커널 주소 격리 / PCID | 둘 다 꺼짐 `[관찰 간접]` | `UserDirectoryTableBase` = 0, `DirectoryTableBase` bit 1 = 0 |
| 캐시 인지 스케줄링 | **켜짐** | 사진 `PerProcessorCycleTimes` 값 있음 |
| CPU 기능 비트 `0x37` | **못 가림** | 동적 D5 (덤프로) |
| 부모 없는 프로세스 | 아님 | — |

- 한 줄 읽기 — "CET 로그" 줄: `PspAllocateProcess +0x1ff8` 이 장부의 `MitigationFlags2 & 0x4000` 을 본다. 사진 raw `+0x754` = `0x40000000` 이라 그 비트가 꺼져 있다. 그래서 `+0x2008 KeInitializeProcessUserCetLogging` 을 안 탔고, `UserCetLogging` 칸이 0 이다 (2.12 "조건부 안 탐").
- `PspAllocateProcess` 안 다른 조건부 호출 (`KeSetCpuSetsProcess +0x1a00`·`KeSetExpectedConcurrencyCountProcess +0x1a19`·`PspSetProcessPriorityClass +0x1a3b`·`KeInitializeProcessUserCetLogging +0x2008`·`PspSetProcessAffinitySafe +0x235f`·`KeSecureProcess +0x279b`) 은 test.exe 에선 **모두 안 탄 것**으로 보인다 (사진 값과 조건).

### 2.14 핵심: 새 가게는 부모 것을 복사해 받는다 — 에뮬레이터에는 무엇을 뜻하나 (내 짐작)

여기서부터는 run 문서에 다 적힌 게 아니라 **내 짐작**이 섞인다. 짐작은 `[추측]` 으로 쓰고 이유를 같이 붙인다.

#### 먼저 사실만 (모두 근거 있음)

1. 신분증: 원본 = **부모 cmd 의 토큰** (`PspReferenceTokenForNewProcess +0x64`, `[추측]` 핸들 속성 없을 때). 복사한 칸 **16 + 값 0 인 복사 3** 은 값이 부모에게서 온다 `[정적]`.
2. 짐 지도 "쓰는 말" 이 이번에 이 꼴을 **`입력 (부모 토큰 복제)`** 라고 이름 붙였다. 복사하는 손은 nt (`SepDuplicateToken`) 라 "쓴 주인" 은 nt 로 세고, "**에뮬레이터에선 부모 토큰 값이 입력이다**" 라고 적었다 (짐 지도 쓰는 말 · 3.19).
3. 손잡이 목록: **부모 cmd 표**에서 스티커 붙은 손잡이 4개를 옮겨 적는다 `[정적]` + `[관찰]`.
4. 손잡이 목록 "주인" 칸 처음 값 = **지금 도는 프로세스 (부모 cmd)** 의 PID 다 `[정적]`.
5. run 문서 "읽는 입력" 은 이 조각이 읽는 것으로 부모 쪽 셋 (핸들 표 원본 · 주인 PID 처음 값 · 토큰 원본), `ProcessFlags`·속성 모음, 부모 장부 칸 몇 개 (`DefaultHardErrorProcessing`·`Flags2`·`IdealGlobalNode`·`MitigationFlags*`), 커널 전역 여럿 (`ExpLuid`·`PspPriorityTable`·`KiProcessListHead` …) 을 적었다.
6. 이 조각 안에는 **`syscall` 이 없다.** 전부 부모의 `NtCreateUserProcess` 시스템 콜 안이다 (`PpmPolicy` 만 그 뒤 `[추측]` BAM).

#### 그래서: 에뮬레이터에는 부모가 없다

- `[추측]` 이유:
  1. CLAUDE.md: 에뮬레이터는 진짜 nt·ntdll 과 **같은 이름·호출 관계**로 짓는다. 흐름도 범위는 "프로세스 생성 → 진입점" 이다.
  2. 진짜 Windows 에선 위 값들이 **부모 cmd 의 실제 상태** (신분증, 손잡이 목록) 에서 온다.
  3. 에뮬레이터 안에서 cmd.exe 를 진짜로 돌리지 않는다면, 그 "부모 상태" 가 **어디에도 없다.**
  4. 그래서 → 에뮬레이터는 이 값들을 **시작할 때 넣어 줄 입력**으로 받아야 할 것 같다. 짐 지도 말로 `입력` 이다.
- 비유: 모형 사무소 (에뮬레이터) 에는 **부모 가게가 없다.** 그러니 "부모 신분증 사본" 과 "물려받을 손잡이 4개" 를 **미리 준비해 가지고** 들어가야 한다. 복사기 (`SepDuplicateToken`) 는 사무소에 있어도, 복사할 원본이 없으면 소용없다.

#### 넣어 줘야 할 것으로 보이는 것 (근거 줄)

| 입력 | 왜 필요해 보이나 (`[추측]`) | 근거 |
|---|---|---|
| 부모 신분증 값 (`TokenSource` `"User32"` · `AuthenticationId` `0x21d70` · `ParentTokenId` `0x21d73` · `ModifiedId` `0x21d7c` · `Privileges` 5개 · `IntegrityLevelIndex` · `VariablePart` …) | 복사 16 + 값 0 인 복사 3칸의 원본 | 2.5 `[정적]`, 값은 run44 K1 `[관찰]` |
| 부모와 함께 쓰는 객체 (`LogonSession`·`IndexEntry` 가 가리키는 것) | 새 신분증이 참조만 늘려 **같은 것**을 가리킨다 | 2.5 `[정적]` |
| 번호표 기계 (`ExpLuid`·`ExpLuidIncrement`) | 새 `TokenId` 를 여기서 뽑는다 | 2.5 `[정적]` |
| 물려받을 손잡이 4개 (부모 표의 상속 항목) | `ExDupHandleTable` 이 이걸 옮겨 적는다. 내용은 아직 모름 (C11, Q79) | 2.7 `[정적]` + `[관찰]` |
| `ProcessFlags` 비트 2 (핸들 상속 켬) | 이게 꺼지면 빈 표 길로 간다 | 2.7 `[정적]` |
| 부모 PID `0x19a8` (주인 칸 처음 값) | 아래 "아직 정할 수 없는 것" | 2.8 `[정적]` |
| 커널 전역 (`PspPriorityTable`·`PspForegroundQuantum`·`PsPrioritySeparation`·`KiMaximumGroups` …) | 우선순위·퀀텀·확장 메모리 배치가 이 값으로 정해진다 | 2.9 `[정적]`, 실행 때 값은 D5 |

#### 없어도 될 것으로 보이는 것

- `[추측]` 이유: 쓰는 줄이 없거나, 남는 게 없다.
  - 운영 카드 "아무도 안 씀" 36 · 손잡이 목록 머리 4: 메모리를 0 으로 받기만 하면 될 것 같다 (run47 해설 2.4 의 같은 짐작).
  - `ProcessLock`: 잡았다 놓아서 0 으로 돌아온다.
  - 조건부 안 탐 칸 (CET 로그·VBS·WoW64 등): test.exe 와 같은 조건이면 안 탄다. 다른 조건을 흉내 낼 때만 필요하다.

#### 아직 정할 수 없는 것

- **주인 PID 의 처음 값**: 진짜 Windows 에선 잠깐 (K1 ~ K2) 부모 번호였다가 `PspInsertProcess +0x4c` 에서 자기 번호로 고쳐진다. ① 이 끝날 때는 이미 자기 번호다. 그러니 에뮬레이터가 이 "잠깐" 까지 흉내 낼지, 처음부터 자기 번호를 넣을지는 정할 일로 보인다 `[추측]`.
- **`PpmPolicy` 7**: 쓰는 손이 nt 밖 BAM 이라면 (D3), run47 의 `다른 드라이버 (ntfs.sys)` 칸처럼 "바깥 몫" 이 된다 `[추측]`. 바깥 몫이면 입력으로 넣을지, 비울지는 정할 일이다.
- 어디까지 흉내 낼지는 **목록이 다 된 뒤 사용자와 정한다** (CLAUDE.md "지금 목표"). 여기선 "이 값들은 부모에게서 온다" 를 **표시만** 한 것이다.

### 2.15 정정 셋 (run 문서 "정정")

#### (1) `PspDisablePrimaryTokenExchange` 는 신분증을 "붙이는" 함수가 아니다

- 전: 짐 지도 3.19 "붙이는 곳 `PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange`" (run43 이 run15 의 이름 짐작을 토큰 절에 붙였다).
- 후 `[정적]`: 이 함수는 신분증을 **읽기만** 한다 (`+0x4e PsReferencePrimaryTokenWithTag` → `+0x59 SeTokenIsAdmin`).
  - 쓰는 건 **장부 쪽** 표시들: `Flags2.PrimaryTokenFrozen` (`+0x22`), `Flags3` 의 얼림 비트 (`+0xc4`·`+0xbb`·`+0x111`), 조건부 `SecurityDomain`·`ParentSecurityDomain` 지움 (`+0x94`·`+0xa2`).
  - 사진 `[관찰]` EPROCESS `Flags2` raw `0x5000 → 0xd000` (`P0~P1`) 와 맞다.
- 비유: run15 해설의 "**신분증 바꿔치기 금지**" 가 딱 맞는 그림이었다. 신분증에 뭘 적는 게 아니라, 장부에 "**이제 신분증 교체 금지**" 도장을 찍는 일이다.
- run15 문서는 "이름으로 본 것" 이라고 적었으니 틀린 말은 아니라서 안 고쳤다.

#### (2) 짐 지도의 `+0x171e`·`+0x1927`·`+0x1991` 은 ret 이었다

- 짐 지도 3장 경로 칸 규칙은 "`호출 +0x..` = 부르는 자리" 다. 그래서 call `+0x1719`·`+0x1922`·`+0x198c` 로 고치고 ret 은 괄호에 남겼다. 값이 틀린 건 아니다.

#### (3) `_EPROCESS` 세 칸이 짐작 → 정확

- `Pcb` (`KeInitializeProcess` 첫 인자 = 새 프로세스, `+0x16df`) · `Token` (`ObInitializeFastReference`, 2.6) · `ObjectTable` (`ObInitProcess +0x53`, 2.7). 셋 다 "부르는 줄만 앎" 이었다 (run43 해설 2.7).
- 보탬 (정정 아님): flow.md 2.2 `KeStartThread` 절의 "스케줄링 준비 `[추측]`" 은 틀린 말이 아니라 `[정적]` 줄 셋을 **더했다** (2.10).

### 2.16 정리: 비유로 한 번 더

1. 부모 가게 (cmd) 가 관리사무소에 "새 가게 내 주세요" 한다 (`NtCreateUserProcess`).
2. 관리사무소는 먼저 **어느 신분증을 베낄지** 고른다. 따로 내민 게 없으니 **부모 가게 신분증**이다 (`PspReferenceTokenForNewProcess`).
3. 빈 가게 장부를 받아 **통째로 지우고** (`memset`), 맨 앞 **운영 카드**를 한 사람이 채운다 (`KeInitializeProcess`). 등급·모래시계 칸은 계산해서 다시 덮고, 땅 대장 첫 장 위치도 적는다.
4. **복사기로 신분증을 새로 한 장** 뽑는다 (`SepDuplicateToken`). 이름·권한은 그대로, 일련번호는 번호표 기계에서 새로, 자물쇠도 새것. 로그인 기록 카드만 부모와 같이 쓴다.
5. 장부 신분증 칸에 그 위치와 **확인증 15장**을 꽂는다 (`ObInitializeFastReference`).
6. 부모 열쇠고리에서 **"줘도 됨" 스티커** 붙은 손잡이 4개를 골라 새 손잡이 목록에 옮긴다 (`ExDupHandleTable`). 목록 "주인" 칸엔 무심코 **부모 번호**가 들어간다.
7. 상가 명부에 올릴 때 "주인" 칸을 **자기 번호**로 고친다 (`PspInsertProcess +0x4c`).
8. 첫 직원이 오면 직원 명단·근무 배정 목록·근무자 수 칸이 채워진다 (`KeStartThread`).
9. 그 뒤 어느 때인가 절전 관리팀이 와서 **전기 절약 등급 7** 을 적고 간다 (`KeSetProcessPpmPolicy`, BAM `[추측]`). 문 앞 대기 명단엔 누군가 (부모 `[추측]`) 이름을 올린다.

### 2.17 곁가지: 도구 메모

run 문서 "도구 메모" 에서 — 하네스 제안이라 오케스트레이터가 사용자에게 묻는다 (미결).

- **제안 1 — "이 구조체 칸을 누가 쓰나" 도구** (`field_writers.py`): 지금 `refs` 는 전역·함수 주소만 찾는다. 이번엔 `PpmPolicy`·`UserDirectoryTableBase`·핸들 표 `UniqueProcessId`·`DirectoryTableBase` 를 `objdump -d` + 정규식으로 훑었다 (레포 밖 임시 스크립트). 오탐이 많으니 "후보 줄" 로만 쓰고 `.asm` 으로 확인하자는 약속과 함께.
- **제안 2 — 함수 포인터 표 경유 호출자** (`refs --through-ptr`): `PspSetProcessPpmPolicy` 처럼 직접 호출자 0 이고 표로만 닿는 함수를 한 번에 이어 보자는 것 (2.11).
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run49 없음 | run49 추가. "(run49) 정적 바퀴, K1 덤프 읽기 로그·EPROCESS raw 8장 다시 읽음" 한 줄. 정적 근거 줄에 run49 |
| 2.1 제목 | … run47, run48 부팅 메모 | … **run49** |
| 2.1 트리 (`PspAllocateProcess` 아래) | 없음 | `+0x171e KeInitializeProcess` · `+0x1927 PspInitializeProcessSecurity → +0xfc SeSubProcessToken → +0x441 SepDuplicateToken` · `+0x1991 ObInitProcess → +0xd1 ExDupHandleTable` (모두 `[정적]`, "그림엔 없음") |
| 2.1 트리 (`PspInsertProcess` 아래) | 없음 | `+0x4c HANDLE_TABLE.UniqueProcessId = 자기 PID` `[정적]` |
| 2.1 `PspAllocateProcess` 절 | — | 새 묶음 "**프로세스 객체 앞부분 세 덩어리**" `[정적]`: `memset` · 운영 카드 · 신분증 **복제** · 손잡이 목록 **복사 길** (사진 + 코드로 확정) · 조건부 호출 안 탐 |
| 2.1 `PspInsertProcess` 절 | 주인 PID 쓰는 줄 `[공백]` | **`[정적]` `+0x4c`** (`+0x27` ObjectTable, `+0x31` UniqueProcessId), 사진 K2 / K3 와 맞음 |
| 2.2 `KeStartThread` 절 | "스케줄링 준비" `[추측]` 만 | + `[정적]` 프로세스 락 안에서 `ProcessListEntry`·`ThreadListHead`·`StackCount` |
| 2.3 `PspUserThreadStartup` 절 | `PspDisablePrimaryTokenExchange` 하는 일 = 이름으로 `[추측]` | + `[정적]` **토큰 칸은 안 씀**, 장부 `Flags2`·`Flags3` 얼림 비트 |
| 4장 | 2.1 `KeStartThread` 줄 · 2.3 줄 · 2.1 (짐 지도) 주인 PID 줄 | 앞 둘은 **(일부 해결)**, 주인 PID 줄은 **(해결됨)**. 새 줄 셋: (해결됨) Q78c1 · (run49 새로) 실행 확인 D1 ~ D6 → Q79 / Q96 · Q78c2 로 넘길 `_EPROCESS` 줄과 칸 표 없는 짐 7개 |
| 5장 | — | run49 줄 둘 |

- 그림 (mermaid) 은 안 바뀌었다. 새 함수들은 "대표 지점" 이라기보다 짐 지도 줄기라서 트리에만 "(그림엔 없음)" 으로 넣었다.

태그 변화 대표:

- 핸들 표 `UniqueProcessId` 쓰는 줄: `[공백]` → **`[정적]` `PspInsertProcess +0x4c`**.
- 토큰 "복제" : 함수 이름으로 본 `[추측]` → **`[정적]`** (`SepDuplicateToken +0x158 ObCreateObjectEx`).
- 핸들 표 길: 정해진 적 없음 → **`[정적]` + `[관찰]` 복사 길 확정**.
- 짐 지도 `_EPROCESS` `Pcb`·`Token`·`ObjectTable`: `[추측]` 짐작 → **`[정적]` 정확**.

### 짐 지도 `baggage.md` 에서 바뀐 것 (일곱째판)

- 3.12 `_KPROCESS` · 3.16 `_HANDLE_TABLE` · 3.19 `_TOKEN` 을 새로 썼다 (0 → 66줄). 줄 끝 "**Q78c1 로 닫힘**".
- "쓰는 말" 에 **`입력 (부모 토큰 복제)`** 가 생겼다 (2.14).
- 경로 줄임 넷: `①P…` (`PspAllocateProcess` 까지) · `①H…` (`ObInitProcess → ExDupHandleTable`) · `①Tok…` (`PspInitializeProcessSecurity → SeSubProcessToken`) · `①TD…` (`→ SepDuplicateToken`).
- 3.11 `_EPROCESS` 머리에 "Q78c2 에서 볼 것" 메모.
- 7.8 `UniqueProcessId` 닫힘, 7.6 에 칸 표 없는 짐 7개 기록, 새 7.12 (동적 질문 D1 ~ D6).
- 2장 숫자는 2.3 표 그대로.

### 큰 그림에서 어떻게 이어지나

- **가게 장부 안쪽이 처음 칸 단위로 열렸다.** run10 이 `PspAllocateProcess` 가 **무엇을 부르는지**를, run44 가 칸이 **언제** 생기는지를 줬다면, 이번엔 장부 앞부분 세 덩어리의 "**누가, 몇 번째 줄에서, 무엇을 원본으로**" 가 채워졌다.
- **"부모에서 온다" 는 꼬리표가 처음 생겼다.** 지금까지 `입력` 은 부팅·파일·레지스트리 쪽이 많았다 (run48 `Ⓑ` 등). 이번엔 **부모 가게** 가 입력의 출처로 또렷이 들어왔다.
- 다음 조각 **Q78c2 (`_EPROCESS`, 241칸)** 로 바로 이어진다. 이번에 지나가며 본 장부 줄을 넘겼다:
  - `SecurityDomain`·`ParentSecurityDomain`, `Flags2.PrimaryTokenFrozen`·`Flags3` 얼림 비트 (`PspDisablePrimaryTokenExchange`), `PpmPolicyLock`·`BamPpmPolicy` (`PspSetProcessPpmPolicy`), `PriorityClass` (`PspSetProcessPriorityClass +0x72`, 기본값 줄은 못 찾음), `Flags.AddressSpaceInitialized` bit 11, 이름 모를 `+0x540`·`+0x3d8`·`+0x410`, `PspAllocateProcess +0x564 ~ +0x5fc` 초기화 묶음.
- 첫 스레드 쪽 칸 (`KeStartThread` 의 스레드 쪽, 일꾼 스레드 몫 `StackCount`) 은 **Q78d** 다.

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 하는 조건은 dast-mentor 몫이다 (run 문서).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** 새 신분증이 정말 부모 cmd 신분증의 복제인가 — 값으로 (`TokenId`·`ParentTokenId`·`ModifiedId`·`AuthenticationId` 어느 칸이 같고 다른가, 주소는 다른가). 토큰 핸들 속성이 없었나 | 정적으로 "새 객체" 는 확정이지만, 부모 신분증을 열어 맞춰 본 적은 없음 | 덤프 **Q79**: K1 덤프에서 `!process 19a8 0` → cmd `EPROCESS.Token & ~0xf` → `dt nt!_TOKEN`. 덤프에 커널 풀이 없으면 `nt!SepDuplicateToken` 입구 `rcx` 로 (Q96 쪽) |
| **D2** 상속 목록 속성 (개수 `r8d`·배열 `r9`) 이 비었나 | 인자는 사진에 안 남음. 복사 길 자체는 이미 확정 | 멈춤 **Q96**: `nt!ObInitProcess` 입구, 조건 = 지금 도는 프로세스가 cmd.exe (새 가게는 아직 이름이 없음) |
| **D3** `PpmPolicy` 7 을 누가, 언제 부르나 (bam.sys 인가, 알림 안인가) | 직접 호출자 0, 함수 표 경유라 정적으로 끝까지 못 감 | 멈춤 **Q96**: `nt!KeSetProcessPpmPolicy` 입구 `k`, `rdx` = 7, 조건 `ImageFileName` = test.exe |
| **D4** `Header.WaitListHead` 대기 블록 `ffff8285'98adc1c0` 은 누구 것인가 | 줄기 밖. 기다리는 쪽 `[추측]` | 덤프 **Q79**: P0 에서 `dt nt!_KWAIT_BLOCK ffff828598adc1c0` (`.Thread`·`.Object`) |
| **D5** 조건 전역 값 (`KiCacheAwareScheduling`·`KeFeatureBits` bit `0x37`·`MiFlags`·`KiFlushPcid`·`KiMaximumGroups`·KUSD `+0x710`·`PspForegroundQuantum[0]`·`PsPrioritySeparation`) | 실행 때 값. `ExtendedFeatureDisableMask` 가 "안 탐" 인지 "0 씀" 인지도 여기서 닫힘 | 덤프 **Q79**: `dd`/`dq`/`db` 한 번씩 |
| **D6** (급하지 않음) `TokenFlags` `0x2a00` 의 단계별 분해 | 여러 단계가 켜고 끈다. 사진은 최종 값만 | 멈춤 **Q96**: `nt!SepDuplicateToken` 반환 직후, `nt!SeSubProcessToken` 반환 직후 |
| 물려받은 손잡이 4개의 **내용** | 핸들 표 안 항목은 범위 밖 | 덤프 **Q79** (C11 `!handle` 비교) |
| 핸들 수 4 → 26 → 44 → 46 때 어느 공용 부품이 어느 칸을 바꾸나 | 공용 부품은 이름만 둠 | `[추측]` 으로 둠 |
| 장부 (`_EPROCESS`) 쪽 줄들 (2.15 · 3장 끝 목록) | 이번 범위 밖 | 정적 **Q78c2** |
| 칸 표 없는 짐 7개 (확장 메모리 안 블록 셋 · `_HANDLE_TABLE_FREE_LIST` · 핸들 표 낮은 단계 페이지 · `_SEP_LOGON_SESSION_REFERENCES` · `_SEP_LUID_TO_INDEX_MAP_ENTRY`) | 체크리스트 21종 밖 | 짐 지도 7.6 에 **기록만** |
| 운영 카드 "아무도 안 씀" 36 이 실행 중에도 0 인가 | 사진은 초기화 동안만 | 범위 밖 (런타임) |

- 대기열 순서 (plan): **Q78c2** → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q96 은 동적 차례에.

---

## 5. 더 읽기

- run 문서: [`run49-kprocess-token-htable.md`](run49-kprocess-token-htable.md) — 구조체별 요약, 호출 지점 표, 호출 관계 트리, 토큰 (복제), 핸들 표 (복사 길), `_KPROCESS`, 갈림길 표, 읽는 입력, 확인된 것, 정정, 아직 확인 필요 (D1 ~ D6), 도구 메모
- 결과서: [`_workspace/sast/43_kprocess-token-htable_result.md`](../../_workspace/sast/43_kprocess-token-htable_result.md) — 질문 1 (`KeInitializeProcess` 인자 표, 확장 메모리 배치, 칸마다 줄, 뒤에 바뀐 칸, 끝까지 0 46칸), 질문 2 (토큰 사슬·칸 셋 갈래·0 인 26칸·`PspDisablePrimaryTokenExchange`), 질문 3 (핸들 표 길·칸마다), 질문 4 (3장 양식 표), 질문 5 (갈림길·입력), 동적 질문, Q78c2 로 넘길 것
- 요청서: [`_workspace/sast/43_kprocess-token-htable.md`](../../_workspace/sast/43_kprocess-token-htable.md)
- 짐 지도: [`baggage.md`](baggage.md) — "쓰는 말" (`입력 (부모 토큰 복제)`), 2장 (일곱째판 숫자), 3.11 · 3.12 · 3.16 · 3.19, 7.2 · 7.6 · 7.8 · 7.12
- 흐름도: [`flow.md`](flow.md) — 2.1 트리, `nt!PspAllocateProcess` 절 ("프로세스 객체 앞부분 세 덩어리"), `nt!PspInsertProcess` 절, 2.2 `nt!KeStartThread` 절, 2.3 `nt!PspUserThreadStartup` 절, 4장 run49 줄, 5장
- 진행표: `_workspace/plan.md` Q78c · Q78c1 (끝) · Q78c2 · Q79 · Q96, 결정 기록 2026-10-29 · 2026-10-30
- 앞 해설:
  - [run10](../02_sprint/run10-allocate-process-해설.md) — `PspAllocateProcess`, 가게 장부 (이번 글의 바탕)
  - [run11](../02_sprint/run11-allocate-thread-해설.md) 2.8 — 상가 명부 (`PspInsertProcess`)
  - [run12](../02_sprint/run12-insert-thread-해설.md) 2.9 — `KeStartThread`
  - [run15](../02_sprint/run15-user-thread-startup-해설.md) 2.8 — 신분증 바꿔치기 금지
  - [run31](../02_sprint/run31-image-section-해설.md) 2.6 — 신분증을 대신 들고 오는 부모
  - [run44](run44-baggage-snap-해설.md) 2.10 · 2.14 — 사진 구간, 주인 PID 바뀜
  - [run47](run47-section-chain-해설.md) 2.4 · 2.5 — 0 의 세 가지, `ObCreateObjectEx`
  - [run48](run48-system-dll-boot-해설.md) 2.6 — 빠른 참조 자리
