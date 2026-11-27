# run154 dbgk-events — 해설

> 이 글은 [run154 문서](run154-dbgk-events.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q291** — 디버그 속성 `0x60001` 으로 지은 자식: `DebugPort` 가 붙는 길 · `DbgkCreateThread` 첫 이벤트 차례 · 메시지 칸 · 큐 · 멈춤 · `NtWaitForDebugEvent` / `NtDebugContinue` 고르기 · 기다림 · 출력 구조 칸 (`_workspace/plan.md`, 이번에 **끝 (run154)**)
  - PE_emulator 380 묶음 **정적 차례 ⑥** (원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` ⑥ 절). 사용자 결정 2026-11-27 (PE_emulator D179). **정적만.**
  - 결과서 판정 **분석성공** — 물음 1 · 3 · 2 를 `.asm` 줄로 닫았다.
  - 380 정적 차례에서 plan 표가 아직 "끝" 이 아닌 것: **Q282** (sast/151) · **Q290 + Q276a** (sast/152) · 덤 **Q292** (sast/154).
- 근거:
  - run 문서 [`run154-dbgk-events.md`](run154-dbgk-events.md) (가장 중요)
  - 요청서 [`_workspace/sast/153_dbgk-events.md`](../../_workspace/sast/153_dbgk-events.md) · 결과서 [`153_dbgk-events_result.md`](../../_workspace/sast/153_dbgk-events_result.md)
    - **처음 analyze 없음.** ntoskrnl 에 export 두 번 (17 + 3), ntdll 에 `DbgUi*` 3개, kernelbase 는 있던 `.asm` 읽기만.
  - flow.md 변경: docs 커밋 `bf823e6` (`git show bf823e6 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 는 셈을 안 바꾸고 두 줄에 경로 한 마디씩.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 cmd 가 디버그 속성 없이 띄우고, cmd 도 디버거 아래가 아니다.
   - 사진 여덟 장 모두 `DebugPort : (null)` · `BeingDebugged : 0` `[관찰]` (`ms/logs/04_baggage-read_K1.txt:156` · `:1548` 등, run44).
   - 그래서 test.exe 는 이번 길에서 **딱 두 줄만** 지나간다 — `DbgkCopyProcessDebugPort +0x27` (0 쓰기) 와 `DbgkCreateThread +0x9c` (`DebugPort == 0` → 끝) (2.14).
   - 이번 이야기는 **"디버거 프로그램이 `CreateProcess(…, DEBUG_PROCESS)` 로 자식을 띄울 때"** 의 일이다.
2. **"r19 8 샘플 · seq 849 ~ 888" 은 에뮬레이터 로그다.** 진짜 Windows 를 본 게 아니라 `[관찰]` 이 아니다.
3. **오프셋 읽는 법** (앞 해설과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x2f3 ObReferenceObjectByHandle`" · "`+0x9c CMP`" 처럼 명령 이름과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - **이번엔 "조각" 줄이 많다.** 조각 안 줄도 **함수 시작 기준** 으로 적고, 조각 기준 수는 괄호에만 둔다 (2.4 에서 천천히).
   - 콜스택으로 본 적은 없다 (정적만).
4. **구조체 넷은 이 PDB 에 없다.**
   - 디버그 객체 (0x68 B) · 큐 항목 (0x168 B) · 메시지 (0x110 B) · 출력 구조 (0xB8 B).
   - 그래서 이 넷의 **오프셋 · 크기 · 값은 `[정적]` (asm 에서 셈), 칸 이름은 `[추측: 이름]`** 이다.
   - EPROCESS · ETHREAD · KTHREAD · PEB · TEB 칸 이름은 PDB 에 있어 `[정적] PDB` 다.
5. **status 이름 · SDK 이름도 짐작이다.**
   - `DEBUG_PROCESS` · `DEBUG_PROCESS_ASSIGN` · `STATUS_TIMEOUT` · `DBG_REPLY_LATER` 같은 이름은 `[추측: 이름]`. **값은 asm 상수** 라 `[정적]`.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- `_EPROCESS` = 관리사무소의 **가게 장부**, `PspInsertProcess` = **상가 명부에 올리기** → [run50 해설](run50-eprocess-create-해설.md) "비유" · 1절 (그 안에서 run10 · run11 해설로 이어짐)
- `PspUserThreadStartup` = 첫 직원의 **출근 도장 창구**, `DbgkCreateThread +0x85` = **첫 출근 도장 두 개** → [run51 해설](run51-eprocess-later-해설.md) 2.3 한 줄 읽기 ② · 2.6
- PEB = 가게 문 앞 **안내판**, `BeingDebugged` 칸과 우리 커널 디버거 (KDNET) 는 상관없다는 것 → [run113 해설](run113-ntdll-user-structs-해설.md) 2.2 줄 ③ · 2.7 "`BeingDebugged` 와 우리 디버거"
- 디버그 포트 = 가게 **감독관 연락선** → [run81 해설](run81-exception-kernel-해설.md) 2.9
- **이벤트** = 커널의 켜짐/꺼짐 **신호등**, **얼림** = 멈춤과 따로 있는 표시 → [run13 해설](../02_sprint/run13-resume-thread-해설.md) 2.3 · 2.7
- 장부에 `KeWaitForMutexObject` 로 찍힌 기다림 = **`KeWaitForSingleObject`** (같은 주소의 두 이름) → [run13 해설](../02_sprint/run13-resume-thread-해설.md) 2.10
- 커널 스택 = **관리사무소 작업대** → [run81 해설](run81-exception-kernel-해설.md) 2.8
- 핸들 = **번호표** → [run86 해설](run86-gdi32-init-해설.md) 2.10 · [run144 해설](run144-gdi-startup-해설.md) "비유"

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, `_EPROCESS` = **가게 장부**, PEB = **안내판**.
- 디버그 포트 (`EPROCESS.DebugPort`) = 장부의 **감독관 연락선** 칸 (run81).
- 이번에 새로 붙는 그림은 run 문서 "결과" 비유를 그대로 가져온다:
  - **디버그 객체 = 우편함.** 부모 (디버거) 가 우편함을 하나 만들고, 아이 (자식 프로세스) 를 낳을 때 "이 우편함으로 편지 보내" 하고 주소를 쥐여 준다.
  - **디버그 이벤트 = 편지.** 아이는 **눈을 뜨자마자, 밖에 나가기 전에** 편지를 쓴다: "태어났어요" → "ntdll 챙겼어요".
  - 편지 한 장을 넣을 때마다 **답장이 올 때까지 문 앞에 서서 기다린다.**
  - 부모는 우편함을 열어 한 장씩 읽고, "읽었다, 계속해" 하고 답장을 넣는다.
- 그러니까 "감독관 연락선" 의 **선 끝에 달린 것이 우편함** 이다. 연락선 칸에 우편함 주소가 적히면 "감독관이 붙은 가게" 가 된다.
- 이 글에서 하나 더 쓰는 그림:
  - 우편함 **깃발** = 우편함에 편지가 있다는 표시 (`EventsPresent`). 깃발이 서면 부모가 와서 연다.
  - 편지마다 붙은 **개인 신호등** = 그 편지를 보낸 직원이 앞에 서서 기다리는 신호등 (`ContinueEvent`). 부모가 답장을 넣으면 초록불이 된다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 속성 `0x60001` 은 뭔가 | **디버그 객체 핸들.** `PspBuildCreateProcessContext` 는 크기 8 만 보고 **저장만** 한다 `[정적]` (2.3) |
| 누가 열고 누가 붙이나 | `PspInsertProcess +0x2f3` 이 접근 **2** 로 열고, `+0x13e DbgkCopyProcessDebugPort` 가 붙인다 `[정적]` (2.3) |
| `DebugPort` · `BeingDebugged` 는 언제 | `+0x2c89e1` 이 `DebugPort` = 객체, `DbgkpMarkProcessPeb +0x84` 가 `BeingDebugged` = 1. **`PspInsertThread` 앞** `[정적]` (2.4) |
| 첫 이벤트는 누가 | **자식 첫 스레드가 유저로 가기 전에 스스로** (`PspUserThreadStartup +0x9c → … → DbgkCreateThread`) `[정적]` (2.5) |
| 차례는 | **① CreateProcess → ② LoadDll ntdll** (WoW64 면 32비트 ntdll 하나 더). exe 는 LoadDll 이 따로 없다 `[정적]` (2.6) |
| 보낸 스레드는 어디서 기다리나 | 자기 커널 스택의 0x168 B 이벤트를 큐 꼬리에 끼우고, **커널 모드 · 비경보 · 무한** 으로 `DbgkpQueueMessage +0x1f0` 에서 `[정적]` (2.8) |
| 디버거는 무엇을 받나 | 한 프로세스는 **한 번에 이벤트 하나만** 보인다. 출력 **0xB8 B 맞음** `[정적]` (2.9 · 2.10) |
| 이어가기는 | ContinueStatus **여섯 값** 만 받고 (핸들 열기 전에 검사), READ 인 이벤트를 찾아 깨운다 `[정적]` (2.11) |
| 팀원 주석은 | `0xB8` · 여섯 값 **둘 다 맞다** `[정적]` (2.13) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**test.exe 흐름 밖의 옆길** 이다. 디버거 프로그램이 자식을 "디버그 속성" 으로 띄울 때만 탄다.

```
(부모 = 디버거 프로그램) CreateProcessA(자식, DEBUG_PROCESS)
 │  kernelbase!CreateProcessInternalW  +0x809 → 속성 {0x60001, 8, 핸들}          ← 이번 (2.3)
 │  ═▶ 커널 NtCreateUserProcess
 │       ├─ PspBuildCreateProcessContext   핸들을 저장만                         ← 이번 (2.3)
 │       ├─ PspAllocateProcess  (안에서 MmCreatePeb — 안내판 만들기)
 │       ├─ PspAllocateThread   (첫 직원 틀)
 │       ├─ PspInsertProcess    +0x2f3 열기 → +0x13e 붙이기                       ← 이번 (2.3 · 2.4)
 │       │     → DebugPort = 우편함 · BeingDebugged = 1
 │       └─ PspInsertThread
 │
 │  (자식 첫 스레드, 커널) PspUserThreadStartup
 │       ├─ +0x9c 알림 → DbgkCreateThread                                        ← 이번 (2.5 ~ 2.8)
 │       │     ① CreateProcess 편지 → (답장 기다림) → ② LoadDll ntdll 편지 → (답장 기다림)
 │       └─ +0xae 유저로 → ntdll LdrInitializeThunk → … → NtContinue → 진입점 → main
 │
(부모의 디버거 스레드)
 │  ntdll!DbgUiWaitStateChange ─JMP▶ NtWaitForDebugEvent   우편함 열어 한 장 고르기  ← 이번 (2.9 · 2.10)
 └  NtDebugContinue                                         답장 넣기 · 깨우기       ← 이번 (2.11)

(test.exe 는? 위 길에서 두 줄만 지나간다 — DbgkCopyProcessDebugPort +0x27 · DbgkCreateThread +0x9c → 2.14)
```

### 왜 이 질문이 지금 왔나

PE_emulator 쪽이 여기서 막혔다 (에뮬레이터 로그, `[관찰]` 아님):

- 어떤 프로그램이 `CreateProcessA(…WUDFHost.exe, 플래그 1)` 로 자식을 디버그 속성으로 띄운다.
- `NtCreateDebugObject` 는 성공하는데, `NtCreateUserProcess` 가 `0x60001` 속성을 모른다며 실패한다.
- 속성만 받아 주면 그다음엔 부모의 `NtWaitForDebugEvent` 가 팀원 멈춤 (`debug_event_queue_unmodeled`) 에 닿는다.
- 그래서 "속성을 받으면 그 뒤로 진짜 커널은 무엇을 하나" 를 끝까지 알아야 했다.

### 무엇이 궁금했나

요청서의 물음은 셋이다.

1. 자식에 디버그 객체가 **어떻게 붙나**, 그리고 **첫 이벤트는 누가 · 언제 · 어떤 차례로** 보내나. 메시지 칸은.
2. 보낸 쪽은 **큐에 어떻게 넣고, 어디서 무엇을 기다리고, 누가 깨우나.**
3. 디버거 쪽 `NtWaitForDebugEvent` 는 **어떤 규칙으로 고르고**, 출력 0xB8 B 칸은 무엇이며, `NtDebugContinue` 는 **무엇을 찾아 깨우나.**

### 바퀴 전에 알던 것

- `[정적]` (run50) `PspInsertProcess +0x13e DbgkCopyProcessDebugPort` 가 `+0x27` 에서 `DebugPort` 에 0 을 쓴다. `+0x2f3` 핸들 갈래는 test.exe 가 "안 탐".
- `[정적]` (run51) `DbgkCreateThread +0x85` 가 프로세스 `Flags |= 0x400001` (첫 출근 도장 둘) 을 찍는다. "디버그 포트가 있으면 CreateProcess · 모듈 메시지" 는 **이름만** 알았다.
- `[정적: objdump]` (run113) `DbgkpMarkProcessPeb +0x84` 가 안내판 `BeingDebugged` 를 쓴다.
- `[정적]` (run15) `PspUserThreadStartup` 정상 길의 차례.
- `[관찰]` (run44) test.exe 사진 여덟 장 모두 `DebugPort (null)` · `BeingDebugged 0`.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계 (대표만)

run 문서 "호출 관계" 를 줄였다. 전체는 run 문서. 숫자는 ret 자리.

```
(부모 스레드)
nt!NtCreateUserProcess
 ├─ +0x29a  PspBuildCreateProcessContext   +0x17a3 크기 8 (아니면 0xc000000d) · +0x17b2 문맥+0x90 = 핸들 (안 엶)
 └─ +0xfc2  PspInsertProcess
          ├─ +0x2f8  ObReferenceObjectByHandle(핸들, 2, DbgkDebugObjectType)   (실행은 +0x13e 앞)
          ├─ +0x143  DbgkCopyProcessDebugPort    +0x27 DebugPort = 0 → +0x2c89e1 DebugPort = 객체
          │        └─ +0x2c8a11 DbgkpMarkProcessPeb   +0x84 BeingDebugged = 1
          └─ +0x313  ObfDereferenceObject
(자식 첫 스레드 — 유저로 가기 전)
nt!PspUserThreadStartup
 └─ +0xa1  PspNotifyThreadCreation
          └─ +0x21  DbgkCreateThread           +0x9c DebugPort 0 이면 끝 · +0x2b3 옛 CreateReported
                   ├─ +0x207adb DbgkpSendApiMessage(P, 0, ① CreateProcess)
                   └─ +0x207b00 DbgkSendSystemDllMessages
                            └─ +0x2e7 DbgkpSendApiMessage(P, 3, ② LoadDll ntdll)
nt!DbgkpSendApiMessage                          +0x79 ReturnedStatus = 0x103
 └─ +0x8c  DbgkpQueueMessage                    +0x270 객체 = DebugPort · 꼬리에 끼움
          ├─ +0x183 KeSetEvent(EventsPresent)
          └─ +0x1f5 기다림 (이벤트+0x10, 커널 · 비경보 · 무한)   ◀── 깨움: DbgkpWakeTarget +0x56
(디버거 스레드)
nt!NtWaitForDebugEvent
 ├─ +0x131 기다림 (객체, Alertable, Timeout)
 ├─ +0x28f DbgkpConvertKernelToUserStateChange  → READ 표시
 └─ +0x2bb DbgkpOpenHandles
nt!NtDebugContinue                              여섯 값 검사
 └─ +0x14d DbgkpWakeTarget  → +0x56 KeSetEvent(이벤트+0x10)
```

모두 `[정적]`. 기다림 두 곳의 장부 이름은 `alias:KeWaitForMutexObject+1` (같은 주소 이름 `KeWaitForSingleObject`, run13 해설 2.10 과 같은 사정).

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0x2f8  ObReferenceObjectByHandle(핸들, 2, DbgkDebugObjectType)   (실행은 +0x13e 앞)`

- **누가**: `PspInsertProcess`. 부모 스레드가 커널 안에서 돌고 있다.
- **어디서**: call `+0x2f3` (ret `+0x2f8`).
- **누구를**: `ObReferenceObjectByHandle` — 번호표 (핸들) 를 내밀고 진짜 물건 (객체) 을 받아 오는 함수 `[추측]` (일반적으로 알려진 설명).
- **무엇을 위해**: 속성으로 받은 번호표가 정말 **디버그 객체** 인지 (`DbgkDebugObjectType`), "자식에 붙여도 됨" 권한 (접근 **2**) 이 있는지 보고, 우편함 자체를 손에 쥔다.
- **꼬리표 "실행은 `+0x13e` 앞"**: 오프셋 숫자는 `+0x13e` 보다 크지만, 실제로는 이 줄이 먼저 돈다 (2.3 d).
- `[정적]`.

#### 줄 ②: `└─ +0x1f5 기다림 (이벤트+0x10, 커널 · 비경보 · 무한)   ◀── 깨움: DbgkpWakeTarget +0x56`

- **누가**: `DbgkpQueueMessage`. 이번엔 **자식 첫 스레드** 가 커널 안에서 돌고 있다.
- **어디서**: call `+0x1f0` (ret `+0x1f5`).
- **누구를**: `KeWaitForSingleObject` — 신호등 하나를 정해 초록불이 될 때까지 그 앞에서 자는 함수 `[추측]` (일반적으로 알려진 설명, run13 해설 2.3).
- **무엇을 위해**: 방금 우편함에 넣은 편지의 **개인 신호등** (이벤트 `+0x10`, `ContinueEvent` `[추측: 이름]`) 앞에서, 부모가 답장을 넣을 때까지 선다.
- 끝의 화살표 `◀──` 는 "누가 이 기다림을 끝내 주나" 다. `DbgkpWakeTarget` 의 `+0x51` call (ret `+0x56`) 이 그 신호등을 초록으로 바꾼다.
- `[정적]`.

#### 줄 ③: `+0x9c DebugPort 0 이면 끝` (`DbgkCreateThread` 옆 글)

- 호출이 아니라 **비교 줄** 이다. `+0x9c CMP [RDI+0x308],0` → `+0xa4 JNZ +0x2b3`.
- `RDI` = 이 프로세스의 가게 장부, `+0x308` = `DebugPort` 칸 `[정적] PDB`.
- 0 이 **아니면** `+0x2b3` 으로 뛰어 편지 쓰는 쪽으로 간다. 0 이면 그냥 지나간다.
- test.exe 는 여기서 0 이라 지나간다 — **이번 길에서 test.exe 가 밟는 두 줄 중 하나** (2.14).

### 2.3 붙는 길 — 속성 `0x60001` 에서 `DebugPort` 까지 `[정적]`

#### 새 말: 디버그 객체

- **디버그 객체** = 커널이 디버거와 디버그 받는 프로세스 사이에 두는 **이벤트 상자**. 이벤트가 쌓이는 줄과 깃발을 갖고 있다 (2.9 의 칸 표).
  - 비유: **우편함.** 부모 (디버거) 가 만들고 (`NtCreateDebugObject`), 그 번호표를 쥐고 있다.

#### 새 말: 속성 칸

- `NtCreateUserProcess` 는 마지막 쪽에 **속성 목록** 을 받는다 (run14 해설: 11번째 인자). 목록의 한 칸 = **{번호, 크기, 값}**.
  - 비유: 개업 신청서 뒤에 붙이는 **추가 항목 쪽지**. 쪽지마다 "항목 번호 · 글자 수 · 내용" 이 적혀 있다.
- 이번 쪽지는 번호 `0x60001`, 크기 8, 내용 = **우편함 번호표 (디버그 객체 핸들)** 다.

#### 다섯 걸음

**a. 쪽지를 만든다 (kernelbase)** `[정적]` (결과서, 기존 asm)

- `CreateProcessInternalW +0x809` 가 `CreationFlags & 3` 을 본다. 이 두 비트 이름은 `DEBUG_PROCESS` · `DEBUG_ONLY_THIS_PROCESS` `[추측: 이름]`.
- 켜져 있으면 `DbgUiConnectToDbg` · `DbgUiGetThreadDebugObject` 로 번호표를 얻고, `+0x8ec ~ +0x908` 에서 쪽지 `{0x60001, 8, 핸들}` 을 짓는다.

**b. 받기만 한다 (`PspBuildCreateProcessContext`)** `[정적]`

- `+0x17a3 CMP [RDI+8],8` — 크기가 8 이 아니면 `+0x17a8 JNZ → +0x1cd` 에서 `0xc000000d`.
- 맞으면 `+0x17ae/+0x17b2` 에서 문맥 (지을 때 쓰는 메모장) `+0x90` 에 번호표를 **적어 두기만** 한다.
- **여기서는 번호표가 진짜인지 열어 보지 않는다.** 이게 PE_emulator 에게 중요한 점이다 (2.13).

**c. 넘긴다 (`NtCreateUserProcess`)** `[정적]`

- `+0xfa3/+0xfaa` 가 문맥 `+0x90` (`[RBP+0x230]`) 을 `PspInsertProcess` **5번째 인자** 로 넣는다.
- 차례: `PspAllocateProcess` (`+0xc36`, 안에서 `MmCreatePeb`) → `PspAllocateThread` → **`PspInsertProcess` (`+0xfbd`)** → `PspInsertThread`.

**d. 연다 (`PspInsertProcess +0x2f3`)** `[정적]`

- `+0x124 ~ +0x12b` 에서 5번째 인자가 0 이 아니면 `+0x2c5` 로 뛴다.
- `+0x2ce MOV EDX,2` (접근 2) · `+0x2d3` `DbgkDebugObjectType` · `+0x2e3` 현재 스레드의 `PreviousMode` → **`+0x2f3 ObReferenceObjectByHandle`**.
- 실패면 `+0x300 JS → +0x24d` 로 그 status 를 돌려준다 (짓기 실패). 성공이면 `+0x306 JMP +0x131`.
- 접근 2 의 이름은 `DEBUG_PROCESS_ASSIGN` `[추측: 이름]`.

**왜 "오프셋은 뒤인데 실행은 앞" 인가** — 단계별로:

1. 함수 안 코드는 실행 순서대로만 놓여 있지 않다 — 아래 2 · 3 의 점프가 그 증거다 `[정적]`. (자주 안 타는 덩어리를 뒤로 빼 두는 일이 흔하다는 건 일반적으로 알려진 설명 `[추측]`.)
2. `+0x12b` 에서 "핸들 있음" 이면 뒤쪽 `+0x2c5` 덩어리로 **뛰어가서** 연다 (`+0x2f3`).
3. 다 열고 `+0x306` 에서 다시 앞쪽 `+0x131` 로 **돌아온다**.
4. 그다음 `+0x13e` 에서 붙이는 함수를 부른다.
5. 그래서 실행 순서는 `+0x2f3` (열기) → `+0x13e` (붙이기) 다 `[정적]`.

- 비유: 서류철 맨 뒤에 붙은 "특별 확인" 쪽지. 특별한 손님일 때만 맨 뒤로 넘겨 확인하고, 다시 앞 장으로 돌아와 이어 쓴다.

**e. 붙인다 (`PspInsertProcess +0x13e DbgkCopyProcessDebugPort`)** `[정적]`

- 인자: (자식 장부, **만든 쪽 프로세스** `R13`, 우편함 또는 0, &붙었나).
- 뒤처리: `+0x30e ObfDereferenceObject` — `+0x2f3` 에서 번호표로 연 참조 하나를 내려놓는다.
- 붙었고 `PspInsertProcess` 4번째 인자 bit 1 이 켜졌으면 `+0x322` 에서 자식 `Flags.NoDebugInherit` (bit 1) 을 켠다.

#### "부모" 를 좁혔다 — 만든 쪽 (현재) 프로세스

- `R13` = `[현재 스레드 + 0xb8]` = `KTHREAD.ApcState.Process` `[정적] PDB` (`ApcState +0x98` + `Process +0x20`).
- 즉 "부모" 가 아니라 **`NtCreateUserProcess` 를 부른 지금 그 프로세스** 다.
- cmd → test.exe 에선 부모 = 부른 프로세스라 **값은 같다.** 그래서 앞 run 이 틀린 게 아니라 **좁힌** 것이다.
- run50 해설 2.8 "세션 — 부모가 아니라 서류 쓰는 사람" · run51 해설 2.10 "`OwnerProcessId` 는 부모 PID 가 아니다" 와 같은 종류의 이야기다.

#### 번호표가 없을 때도 이 함수는 불린다 `[정적]`

- 디버그 속성이 없으면 `R8 = 0` 으로 `+0x13e` 가 그대로 불린다 (test.exe 가 이 경우).
- `+0x37 TEST R8` · `+0x3a JNZ` 가 안 뛰면 `+0x3c CMP [만든 쪽+0x308],0` — 만든 쪽 가게에 감독관 연락선이 있나.
- 있으면 `+0xb2 ~ +0xe0` 에서 전역 `DbgkpProcessDebugPortMutex` 를 잡고 그 우편함을 가져온다 (`+0xbe`).
- 다만 만든 쪽 `Flags & 2` (`NoDebugInherit`) 가 켜졌으면 안 물려받는다 (`+0xca ~ +0xd2`).
- 번호표가 **있으면** 만든 쪽 연락선은 보지도 않는다 (`+0x37 TEST R8 · +0x3a JNZ +0x6b`).

- 비유: 아이에게 우편함 주소를 쥐여 줬으면 그걸 쓴다. 안 쥐여 줬으면 "엄마 가게에 감독관이 있으면 같은 우편함을 쓴다" — 단, 엄마 가게에 "물려주지 말 것" 딱지가 붙어 있으면 안 쓴다.

### 2.4 `DebugPort` · `BeingDebugged` 를 누가 언제 쓰나 `[정적]`

| 칸 (`[정적] PDB`) | 쓰는 줄 (함수 기준) | 값 · 조건 |
|---|---|---|
| 장부 `DebugPort` (`+0x308`) | `DbgkCopyProcessDebugPort +0x27` | **늘 먼저 0.** test.exe 는 여기까지만 |
| 〃 | **`+0x2c89e1`** (조각 `FUN_140845053 +0x2c8942`) | = 우편함. 우편함 `Flags` (`+0x60`) bit 0 (닫는 중) 이 꺼졌을 때만, 우편함 뮤텍스 (`+0x18`) 를 쥔 채 |
| 안내판 `BeingDebugged` (`+0x2`) | **`DbgkpMarkProcessPeb +0x84`** (WoW64 면 `+0xca` PEB32 도) | `DebugPort ≠ 0` 이면 1. 장부 `Peb` (`+0x2e0`) 가 0 이면 아무것도 안 함 (`+0x4a`) |
| 장부 `Flags.NoDebugInherit` (bit 1) | `PspInsertProcess +0x322` | 붙었고 4번째 인자 bit 1 일 때 |

#### 한 줄씩: 우편함이 "닫는 중" 이면

- `+0x2c89da ~ +0x2c89df` 에서 우편함 `Flags` bit 0 을 본다.
- 켜져 있으면 연락선에 안 적고 `+0x2c89ea` 에 표시 → 손잡이를 내려놓고 **status 0 으로 돌아간다.**
- 즉 "**안 붙은 채 성공**" 이다. 자식은 디버거 없이 그냥 지어진다 `[정적]`.

#### 언제 — 자식 직원이 한 번도 안 돈 때

- 안내판은 이미 있다 (`PspAllocateProcess` 안 `MmCreatePeb`).
- 첫 직원 틀 (`PspAllocateThread`) 도 있다.
- 직원을 가게에 끼우기 (`PspInsertThread`) **앞** 이다. 그래서 자식 스레드는 아직 한 번도 안 돌았다.
- → 자식의 유저 코드가 `BeingDebugged` 를 처음 읽을 때 이미 1 일 것이다 `[추측: 줄 순서에서 — 쓰기가 직원 끼우기 앞이라]`. 읽는 쪽은 ntdll 의 앞 run 줄들 (flow.md 2.4 `LdrpInitializeProcess +0x1b35` 등 · 2.10 `RtlRaiseException +0x17e`).

#### 거절되는 경우 `[정적]`

- 우편함 `Flags` bit 2 (우편함을 만든 프로세스가 32비트 WoW) 가 켜졌는데 자식이 32비트 WoW 가 아니면 `0xc00000bb` (`+0x2c8948 ~ +0x2c8978`).
- `PsTestProtectedProcessIncompatibility` 가 참이면 `0xc0000712` (`+0x95`).
- 거절이면 `PspInsertProcess +0x150 JS` 로 **프로세스 짓기 자체가 실패** 한다.

#### 새 말: 조각, 그리고 두 기준의 오프셋

- **조각** = 함수 본문에서 멀리 떨어진 곳에 놓인, 같은 함수의 코드 덩어리. Ghidra 는 여기에 `FUN_140845053` 같은 이름을 따로 붙인다.
  - 비유: 서류철 본문과 따로, 철 맨 끝 부록 칸에 끼워 둔 **부록 쪽지**.
- 그래서 `.asm` 의 조각 줄엔 **수가 두 개** 찍힌다 (run 문서 "도구 메모"):
  - 조각 시작 기준: `frag:FUN_140845053+0x2c896d`
  - 함수 시작 기준: `(= DbgkCopyProcessDebugPort +0x2c8a0c)`
- 같은 call 하나가 **두 숫자** 를 갖는다. 그래서 섞이면 헷갈린다.

#### 정정 — run113 의 `+0x2c8972` 는 조각 기준이었다

1. run113 은 `DbgkpMarkProcessPeb` 을 부르는 자리를 "ret `+0x2c8972`" 로 적었다.
2. 그런데 같은 줄의 `+0x3c` · `+0x43` 은 **함수 기준** 이었다. 한 줄에 두 기준이 섞였다.
3. 이번에 `.asm` 을 보니 `+0x2c8972` 는 **조각 기준 ret** 이다.
4. 함수 기준으로 고치면 **call `+0x2c8a0c` · ret `+0x2c8a11`** 이다 `[정적]`.
5. 뜻이 바뀐 건 없다. 숫자 **읽는 기준** 만 맞췄다.

- run113 문서 맨 위에 한 줄, flow.md 2.1 글머리 수, 그리고 [run113 해설](run113-ntdll-user-structs-해설.md) 맨 위에도 "고쳐 읽기 (run154)" 한 줄이 들어갔다 (본문은 그대로).
- 조각 줄은 WinDbg 가 이름으로 못 보여 줄 수 있어, 멈춤점은 절대 주소 `nt+0xb0d995` (DebugPort 쓰기) · `nt+0xb0d9c0` (MarkPeb 부르기) 로 적어 뒀다 (결과서, `[추측]` 은 "이름으로 못 보여 줄 수 있음" 쪽).

### 2.5 첫 이벤트 — 자식 첫 스레드가 스스로 보낸다 `[정적]`

#### 새 말: 디버그 이벤트

- **디버그 이벤트** = 디버그 받는 프로세스에 일이 생겼을 때 커널이 디버거에게 보내는 **소식 한 건**. "프로세스 생김" · "DLL 올림" · "스레드 생김" · "예외" · "끝남" 같은 종류가 있다 (2.7 의 ApiNumber 표).
  - 비유: 우편함에 넣는 **편지 한 장**.

#### 누가 보내나 — 부모가 아니라 아이 자신

사슬: `KiStartUserThread` → `PspUserThreadStartup +0x9c/+0xa1` → `PspNotifyThreadCreation +0x1c/+0x21` → `DbgkCreateThread`.

- 이 사슬은 **자식 첫 스레드** 가 처음 깨어나 커널 안에서 도는 길이다 (run15 · run51 의 "출근 도장 창구").
- 그러니 편지를 쓰는 건 부모가 아니라 **아이 (자식 첫 직원) 자신** 이다.
- 알림 (`+0x9c`) 은 유저로 가기 (`+0xae PspInitializeThunkContext`) **앞** 이다. 유저 코드가 한 줄도 돌기 전이다.
- 비유: 아이가 **눈을 뜨자마자, 현관문을 나서기 전에** 편지를 쓴다.

갈래 둘 (이번 길과 다른 길):

- `+0x5a ~ +0x6a`: 장부 `Flags3.Minimal` (bit 0) 이 켜졌으면 `+0xc5 DbgkCreateMinimalThread` 로 간다 (이번 길 아님).
- `+0x48 ~ +0x4d`: `CrossThreadFlags.ThreadInserted` 가 0 이면 스레드를 끝내는 쪽 (`PspTerminateThreadByPointer(…, 0xc000004b, 1)`) 을 거친 뒤 `+0x33` 으로 돌아와 **알림은 그대로 지나간다** (run15 의 "갈래 3개" 와 같은 줄).

#### `DbgkCreateThread` 앞부분 — 도장 찍고, 연락선 보고, 처음인지 보고

1. `+0x77 ~ +0x8d` `CMPXCHG.LOCK` 으로 장부 `Flags |= 0x400001` (`CreateReported` · `ImageNotifyDone`). 옛 값은 `+0x8f R12D` 에 남긴다 (run51 의 "첫 출근 도장 둘").
2. `+0x96 BT EAX,0x16` — 옛 `ImageNotifyDone` 이 0 이면 이미지 알림 (run51). 디버거와 무관.
3. **`+0x9c CMP [RDI+0x308],0` → `+0xa4 JNZ +0x2b3`** — 감독관 연락선이 있을 때만 편지 쪽으로.
4. `+0x2b3 TEST R12B,1` · `+0x2b7 JZ` — 옛 `CreateReported` 가 0 이면 = **이 가게의 첫 직원** → 조각 `+0x2079e6` (CreateProcess 편지 쓰기).
   - 옛 값이 1 이면 = 둘째 직원부터 → 스레드 길 (CreateThread 편지).

- 비유: 출근 도장을 찍을 때 "**이 가게에 내가 처음 찍는 사람인가**" 를 도장 찍기 전 옛 장부로 본다. 처음이면 "가게가 생겼어요" 편지, 아니면 "직원이 하나 늘었어요" 편지.

### 2.6 차례 — ① CreateProcess → ② LoadDll ntdll, 사이에 멈춤 `[정적]`

| 차례 | 편지 종류 (ApiNumber) | 보내는 줄 | `Flags` | 안 보내는 조건 |
|---|---|---|---|---|
| 0 | (사전) 도장 | `DbgkCreateThread +0x85` | — | `+0x9c DebugPort == 0` 이면 아무것도 안 보냄 (test.exe) |
| **1** | **CreateProcess (2)** | `DbgkCreateThread +0x207ad6` | **0** (안 얼림) | 옛 `CreateReported` 1 (그땐 CreateThread 길) |
| **2** | **LoadDll ntdll (5)** | `DbgkSendSystemDllMessages +0x2e2` | **3** (얼림 · `0x40`) | `PsQuerySystemDllInfo(0)` 이 0 |
| 3 | LoadDll 32비트 ntdll (5) — **WoW64 자식만** | 같은 루프 (`+0x2b0 PsWow64GetProcessNtdllType`) | 3 | 64비트 자식 |
| 4 | 모듈마다 LoadDll (5) — **클론 스레드만** | `DbgkCreateThread +0x338 → DbgkpPostModuleMessages → DbgkPostModuleMessage +0x14e` | 3 | 보통 자식 (첫 스레드는 클론 아님 `[추측]`) |
| — | (편지 아님) 유저로 감 | `PspUserThreadStartup +0xae` | | |

#### 왜 ② 는 ① 을 이어 준 뒤에야 생기나 — 단계별로

1. `[정적]` ① 을 보내면 보낸 직원은 `DbgkpQueueMessage +0x1f0` 에서 **답장이 올 때까지 막힌다** (2.8).
2. `[정적]` ② 를 만드는 줄 `+0x207afb DbgkSendSystemDllMessages` 는 ① 보내기 (`+0x207ad6`) **뒤** 에 있다.
3. 그래서 ① 에 답장이 와야 `+0x207afb` 까지 가서 ② 를 만든다.
4. → 디버거 쪽에서 보면 **① 을 이어 주기 전엔 ② 가 우편함에 없다** `[정적]`.

- 비유: 아이는 편지를 한 장 넣고 문 앞에서 답장을 기다린다. 답장이 와야 다음 편지를 쓰러 들어간다. 그러니 우편함엔 늘 이 아이 편지가 **많아야 한 장** 이다.

#### exe 는 LoadDll 편지가 없다

- exe 정보 (파일 · 기준 주소 · 진입점) 는 ① CreateProcess 편지 **안에 들어 있다** (2.7).
- 그래서 "exe 를 올렸어요" 편지는 따로 없다 `[정적]`.

#### 답장은 안 읽는다

- `DbgkCreateThread` 는 돌아온 답 (`ReturnedStatus`) 을 **안 본다** — `+0x207adb` · `+0x320` 뒤에 비교 줄이 없다 (결과서).
- 답을 보는 곳은 `DbgkpSendApiMessage` 의 `0x40010001` 하나뿐이다 (2.11 끝).

#### 처음 둘 뒤 (이번 요청 밖)

- 이 자식이 새 스레드를 만들면 → CreateThread (ApiNumber 1, Flags 1). `+0x2c4` `HideFromDebugger` 면 건너뜀.
- 나중 DLL → `DbgkMapViewOfSection +0x164` (LoadDll, Flags 1).
- 예외 · 끝냄 (`DbgkForwardException` · `DbgkExitThread` · `DbgkExitProcess`) 은 이번 요청 밖.

### 2.7 편지 안 — 메시지 칸과 깃발 셋 `[정적]` 오프셋 · `[추측: 이름]` 칸 이름

#### 새 말: 메시지

- **메시지** = 편지 **본문**. 크기 0x110 B (`DbgkCreateThread +0x3e/+0x45 memset(.., 0, 0x110)`). 이름은 `DBGKM_APIMSG` `[추측: 이름]`.
- 맨 앞은 `_PORT_MESSAGE` (0x28 B, `[정적] PDB` 크기) 꼴의 머리다.

대표 칸만 (전체는 run 문서 5절):

| 칸 | 이름 `[추측: 이름]` | 값 |
|---|---|---|
| `+0x00` | DataLength · TotalLength | CreateProcess `0x38` · `0x60` / LoadDll `0x28` · `0x50` / CreateThread `0x18` · `0x40` |
| `+0x04` | Type | 늘 8 |
| `+0x28` | **ApiNumber** (편지 종류) | 0 예외 · **1 CreateThread · 2 CreateProcess** · 3 ExitThread · 4 ExitProcess · **5 LoadDll** · 6 UnloadDll |
| `+0x2c` | **ReturnedStatus** (답장 칸) | 보낼 때 `0x103`. 이어 줄 때 디버거의 ContinueStatus 로 덮임 |

#### 한 줄씩: ① CreateProcess 편지의 진입점 칸

- 줄: `+0x207a49 ~ +0x207a67` → 메시지 `+0x58` (InitialThread.StartAddress `[추측: 이름]`).
- **무엇을**: exe PE 헤더의 ImageBase (`[NT+0x30]`) + 진입점 RVA (`[NT+0x28]`). 32비트 WoW 면 `[NT+0x34]` + `[NT+0x28]`.
- 즉 **이미지 진입점** 이다. 이 프로젝트 지도의 끝점 (PE `AddressOfEntryPoint`) 과 같은 자리다.
- `ETHREAD.Win32StartAddress` 가 **아니다** — 그건 스레드 길 (CreateThread 편지) 에서 쓴다 (`+0x2db/+0x2e2`).
- 그 밖 칸: `+0x38` FileHandle (exe 파일을 **경로 이름으로 다시 연** 커널 핸들, 실패면 0) · `+0x40` BaseOfImage (`SectionBaseAddress`) · `+0x48` / `+0x4c` 헤더 `PointerToSymbolTable` / `NumberOfSymbols`.

#### 한 줄씩: ② LoadDll 편지의 이름 칸

- 칸: 메시지 `+0x48` NamePointer `[추측: 이름]`.
- **무엇을**: 자식 TEB 의 `NtTib.ArbitraryUserPointer` 칸 **주소** (`TEB+0x28`, `+0x175 LEA RAX,[R15+0x28]`).
- 그 칸에는 `TEB+0x1268` (`StaticUnicodeBuffer`, `[정적] PDB`) 를 가리키는 포인터를 넣고, `+0x170 RtlStringCbCopyW` 로 시스템 DLL 경로를 복사해 둔다.
- 보낸 뒤 `+0x109cfe` 가 `[TEB+0x28] = 0` 으로 지운다.
- → 디버거는 이 포인터를 따라가 **자식 메모리에서** 이름 글자를 읽는다.
- 비유: 편지에 DLL 이름을 직접 안 적고 "**아이 책상 위 메모지를 봐**" 라고만 적는다. 부모가 그 메모지를 읽고 나면 아이가 메모지를 치운다.

#### 깃발 셋 — PDB 이름으로 닫혔다 `[정적] PDB`

요청서가 "뭔지 모름" 으로 물었던 줄이다.

| 줄 | 뜻 | 하는 일 |
|---|---|---|
| `+0x2c4 TEST AL,0x4` (`MOV` 은 `+0x2bd`) | `ETHREAD.CrossThreadFlags.HideFromDebugger` (bit 2) | 켜졌으면 CreateThread 편지를 통째로 건너뜀. **스레드 길에서만** |
| `+0x327 TEST AL,0x10` (`MOV` 은 `+0x320`) | `ETHREAD.SameThreadPassiveFlags.ClonedThread` (bit 4) | 켜졌으면 `+0x338 DbgkpPostModuleMessages` (모듈마다 LoadDll) |
| `+0x2b3 TEST R12B,1` | `EPROCESS.Flags.CreateReported` (bit 0) 의 **옛 값** | 0 = 첫 직원 (CreateProcess 조각), 1 = 스레드 길 |

- 원문 · 요청서가 "`+0x2c4 MOV …`" 로 적은 것은 flow-writer 가 `MOV` = `+0x2bd`, `+0x2c4` = `TEST` 로 맞췄다 (run 문서 "flow-writer 대조" 3).

### 2.8 우편함에 넣고 문 앞에서 기다린다 `[정적]`

#### 새 말: 큐

- **큐** = 먼저 온 것이 앞에 서는 **줄**. 디버그 객체 안 `EventList` (`+0x50`) 가 편지 줄이다. 새 편지는 **꼬리** 에 선다.
  - 비유: 우편함 안에 편지가 **들어온 순서대로 겹쳐** 쌓인다.

#### 보내기 (`DbgkpSendApiMessage`) — 여섯 걸음

1. `+0x3f ~ +0x45` Flags bit 1 → 큐에 넘길 값 `0` 또는 `0x40`.
2. `+0x53 ~ +0x6a` 보내는 프로세스가 **자기 프로세스이고** `Flags & 1` 이면 `+0x65 DbgkpSuspendProcess` → `PsFreezeProcess` (같은 가게 다른 직원 **얼림**).
3. **`+0x79` 답장 칸 (`ReturnedStatus`) = `0x103`** — "아직 답 없음" 자리 채움.
4. **`+0x87 DbgkpQueueMessage`** — 넣고 기다리기 (아래).
5. `+0x8e ~ +0x95` 얼렸으면 `DbgkpResumeProcess` 로 풂.
6. `+0x9a ~ +0xa6` 답이 `0x40010001` 이면 처음으로 돌아가 **같은 편지를 다시** 보낸다 (2.11 끝).

`Flags` 정리: CreateProcess **0** · CreateThread **1** · `DbgkSendSystemDllMessages` **3** · `DbgkMapViewOfSection` **1**.

#### 넣고 기다리기 (`DbgkpQueueMessage`, 기다림 길)

- 큐 항목 = **0x168 B** (`DEBUG_EVENT` `[추측: 이름]`, 크기는 `+0x3a ~ +0x4c memset 0x168`).
- 기다림 길에선 이 항목을 **보낸 직원 자기 커널 스택** (`RSP+0x40`) 에 둔다 — 관리사무소 작업대 위의 봉투다.
- 항목 `+0x10` 은 `+0x29a KeInitializeEvent` 로 만든 **동기 이벤트, 꺼진 채** — 이게 편지의 **개인 신호등** 이다.

대표 줄 (run 문서 6절 표 차례 그대로):

| 줄 | 하는 일 |
|---|---|
| `+0x25a ~ +0x266` | `Flags` → 항목 `+0x4c` · 전역 `DbgkpProcessDebugPortMutex` 잡음 |
| `+0x270` | 우편함 = 장부 `DebugPort` (`[R13+0x308]`) |
| `+0x12b ~ +0x15b` | 우편함이 0 이면 **`0xc0000353`** |
| `+0x137 ~ +0x141` | 우편함 뮤텍스 · "닫는 중" 이면 `0xc0000354` |
| **`+0x147 ~ +0x16c`** | **편지 줄 꼬리에 끼움** (링크가 틀리면 `+0x159 INT 0x29`) |
| **`+0x17e`** | **`KeSetEvent(EventsPresent)`** — 우편함 **깃발** 세움 |
| **`+0x1df ~ +0x1f0`** | **`KeWaitForSingleObject(이벤트+0x10, 0, 0, 0, NULL)`** — Executive · KernelMode · 비경보 · 무한 |
| `+0x1f5` · `+0x1fe ~ +0x24e` | 깨면 `EDI = 이벤트.Status`, 편지 0x110 B 를 보낸 쪽으로 되복사 (**답장 칸이 돌아옴**) |

- "동기 이벤트" 는 신호등 중에서도 **한 사람을 지나가게 하면 저절로 다시 빨강이 되는** 종류로 일반적으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 우편함 깃발 `EventsPresent` 는 **알림 이벤트** (2.9 칸 표) 라서 끌 때까지 켜져 있다 `[추측]` (같은 일반 설명).

#### 왜 "커널 모드 · 비경보 · 무한" 이 중요한가 — 단계별로

1. `[정적]` 기다림 인자: 이유 Executive, 모드 **KernelMode**, Alertable **0**, Timeout **NULL**.
2. Timeout NULL = **시간이 다 돼서 깨는 일이 없다.**
3. 비경보 · 커널 모드 = run 문서 표대로 **유저 APC · alert 로도 안 깨어난다** `[정적]`.
4. 깨우는 길은 **둘뿐** 이다 `[정적]`:
   - `NtDebugContinue +0x148 → DbgkpWakeTarget +0x51 KeSetEvent(이벤트+0x10)` (status 0)
   - 디버거가 우편함을 닫을 때 `DbgkpCloseObject +0xe6 → DbgkpWakeTarget` (`0xc0000354`) (2.12)
5. → 디버거가 답장을 안 넣으면 자식 첫 직원은 **영원히 문 앞에 서 있다.** 유저 코드는 한 줄도 안 돈다.

- 비유: 아이는 편지를 넣고 **자기 손에 봉투 사본을 쥔 채** (자기 커널 스택 = 관리사무소 작업대) 문 앞 개인 신호등 앞에 선다. 시계도 안 보고, 누가 어깨를 두드려도 안 움직인다. 부모가 답장을 넣어 신호등을 초록으로 바꾸거나, 부모가 우편함을 아예 치워 버려야만 움직인다.

#### 기다림 / 깨움 표

| 누가 | 어디서 | 무엇을 | 누가 깨우나 |
|---|---|---|---|
| 보낸 직원 (자식 첫 스레드) | `DbgkpQueueMessage +0x1f0/+0x1f5` | 자기 편지의 개인 신호등, 커널 모드 · 비경보 · 무한 | `DbgkpWakeTarget +0x51` ← `NtDebugContinue +0x148` · `DbgkpCloseObject +0xe6` |
| 디버거 직원 | `NtWaitForDebugEvent +0x12c/+0x131` | 우편함 깃발, 호출자 모드 · Alertable · Timeout | `DbgkpQueueMessage +0x17e` · `NtDebugContinue +0x114` · `DbgkpCloseObject +0x52` |
| 같은 가게 다른 직원 | (기다림이 아니라 **얼림**) `DbgkpSuspendProcess +0x19 PsFreezeProcess` | `Flags & 1` 인 보냄만 (② LoadDll · CreateThread · 나중 DLL) | `DbgkpResumeProcess +0xa PsThawMultiProcess` |

#### ② 는 얼림을 거는데, 얼릴 직원이 있나

- `[정적]` ② LoadDll 은 Flags 3 이라 bit 0 이 켜져 얼림을 건다. ① CreateProcess 는 0 이라 안 건다.
- `[추측]` 그런데 이 순간 가게엔 **첫 직원 하나뿐** 일 것이다 — 일꾼 스레드는 ntdll 초기화 뒤에 생긴다 (flow.md 3장). 밖에서 원격 스레드를 넣는 경우는 안 봤다.
- `[추측]` `PsFreezeProcess` 가 부른 직원 자신은 안 얼린다는 것도 안 읽었다.

### 2.9 디버거가 우편함을 연다 — 한 아이 편지는 한 번에 한 장 `[정적]`

#### 우편함 칸 (0x68 B — `DEBUG_OBJECT` `[추측: 이름]`)

크기: `NtCreateDebugObject +0x93 MOV [RSP+0x28],0x68`.

| 칸 | 이름 `[추측: 이름]` | 뜻 |
|---|---|---|
| `+0x00` | EventsPresent | **알림 이벤트**, 꺼진 채 — 우편함 **깃발** |
| `+0x18` | Mutex | 우편함 자물쇠 |
| `+0x50` | EventList | 편지 줄 (빈 원형 목록으로 시작) |
| `+0x60` | Flags | **bit 0 닫는 중** · **bit 1 KillOnClose** · **bit 2 만든 프로세스가 32비트 WoW** |

#### 새 말: 편지 위 표시 (READ · INACTIVE)

- 큐 항목 `+0x4c` Flags 에 디버거 쪽이 붙이는 표시 `[추측: 이름]`:
  - **1 READ** = 디버거에게 **이미 건넨** 편지.
  - **4 INACTIVE** = 같은 아이 편지가 앞에 있어서 **지금은 안 보여 줄** 편지.
  - (2 NOWAIT · 8 RELEASE · 0x20 SUSPEND · 0x40 은 run 문서 6절 표.)
- 비유: 편지 봉투에 붙이는 **"읽음" 도장** 과 **"대기" 딱지**.

#### 고르는 규칙 — 단계별로 (`NtWaitForDebugEvent`)

1. `+0x223 ~ +0x22b` 줄 앞에서부터 하나씩. `Flags & 5` (READ · INACTIVE) 면 건너뛴다.
2. `+0x231 ~ +0x25a` **앞쪽에 같은 `UniqueProcess` 편지가 있으면** 이 편지에 INACTIVE (`+0x24b`) 를 달고 건너뛴다.
3. 둘 다 아니면 고른다 → `+0x28a DbgkpConvertKernelToUserStateChange` 로 디버거용 모양으로 바꾼다.
4. **`+0x28f` READ 를 단다. 줄에서는 안 뺀다.**
5. `+0x2b6 DbgkpOpenHandles` 로 디버거 쪽 번호표를 연다.

- → **한 프로세스는 한 번에 이벤트 하나만** 디버거에게 보인다 `[정적]`.
- 비유: 부모는 한 아이의 편지를 **맨 앞 한 장만** 본다. 읽은 편지는 "읽음" 도장만 찍고 우편함에 그대로 둔다. 답장을 넣을 때 비로소 꺼낸다 (2.11).

#### 디버거가 받는 번호표 `[정적]`

- 번호표는 **`NtWaitForDebugEvent` 를 부른 디버거 프로세스의 번호표 대장** 에 생긴다.
- CreateProcess: 프로세스 **`0x12067b`** (`+0xeab32`) · 스레드 **`0x12007b`** (`+0xfe`) · 파일 = 커널 핸들을 `ObDuplicateObject` 로 복제한 뒤 **원본은 닫음**.
- CreateThread: 스레드 `0x12007b` (`+0xb9`).
- LoadDll: 파일 복제 (`+0x7c`) · 원본 닫음.
- 그래서 ① 조각 `+0x207ae3 ~ +0x207aea` 의 "남았으면 `ObCloseHandle`" 은, 디버거가 이미 읽었으면 원본 칸이 0 이 돼 있어서 **안 닫는다** (`DbgkpConvertKernelToUserStateChange +0x11d`).

### 2.10 빈 우편함과 0xB8 — `NewState` `[정적]`

#### 빈 우편함일 때

- `+0x4d ~ +0x58` 먼저 지역 0xB8 B 를 0 으로 민다.
- `+0x12c` 우편함 깃발을 기다린다 — 이번엔 **호출자 모드 · Alertable · Timeout** (보낸 직원과 다르다).
- `+0x1d5 ~ +0x1e9` 기다림이 `0x102` (시간 끝) · `0x101` (alert) · `0xc0` (유저 APC) 이면 바로 끝 — 출력엔 **0 으로 찬 구조** 를 복사해 준다.
- `+0x214 ~ +0x221` 골라 보니 줄이 비었으면 `+0x21c` 에서 깃발을 내린다 (`KeResetEvent`).
- 전부 READ · INACTIVE 라 못 골랐으면 다시 기다린다. 상대 시간이면 지난 만큼 줄이고, 다 지나면 `+0x321 EBX = 0x102`.

#### 유저 쪽은 늘 Alertable 1

- `ntdll!DbgUiWaitStateChange`: `+0xf MOV DL,1` (Alertable 1) · `+0x11` 핸들 = `TEB+0x16a8` → `+0x18 JMP NtWaitForDebugEvent`.
  - `TEB+0x16a8` = `DbgSsReserved` (`+0x16a0`, 16 B) 의 둘째 칸 `[정적] PDB` 칸 · 뜻은 `[추측]`.
- 그래서 `0x101` · `0xc0` 도 **유저까지 올라올 수 있다** `[정적]`.

#### 새 말: `NewState`

- **`NewState`** = 디버거가 받는 0xB8 B 출력 구조 맨 앞 (`+0x00`, u32) 의 **"무슨 일이 생겼나" 번호**. 커널 안 ApiNumber 와 **다른 번호 체계** 다.
  - 비유: 아이가 쓴 편지를 부모가 읽기 좋게 **옮겨 적은 사본** 의 맨 위 **분류 번호**. 원본 편지의 종류 번호 (ApiNumber) 와 번호가 다르다.
- 출력 구조 이름은 `DBGUI_WAIT_STATE_CHANGE` `[추측: 이름]`. 크기 **0xB8 = 머리 0x18 + `_DBGKM_EXCEPTION64` 0xa0** (`[정적] PDB` 크기).
- 공통 칸: `+0x00` NewState · `+0x08` UniqueProcess · `+0x10` UniqueThread.

대표만 (전체는 run 문서 "PE_emulator 쪽 — `0xB8` 칸 표"):

| `NewState` | 원본 (ApiNumber) | 대표 칸 | ntdll → `dwDebugEventCode` |
|---|---|---|---|
| **0** | 없음 (`0x102` · `0x101` · `0xc0`) | 전부 0 | (Worker `+0x11e` 가 `0xc0000001`) |
| **2** | CreateThread (1) | `+0x18` HandleToThread · `+0x28` StartAddress | 2 |
| **3** | CreateProcess (2) | `+0x18` 프로세스 · `+0x20` 스레드 · `+0x30` 파일 · `+0x38` BaseOfImage · `+0x50` 진입점 | 3 |
| **9** | LoadDll (5) | `+0x18` 파일 · `+0x20` BaseOfDll · `+0x30` NamePointer | 6 |

#### 커널과 ntdll 을 따로 읽었는데 칸마다 맞았다

- 커널 쪽: `DbgkpConvertKernelToUserStateChange` 가 칸을 **쓰는** 줄.
- 유저 쪽: `ntdll!DbgUiConvertStateChangeStructureWorker` 가 같은 칸을 **읽는** 줄 (`+0x22d` NewState 3 → 3, `+0x125` 9 → 6, `+0x157` 2 → 2 …).
- 두 쪽을 따로 읽어 오프셋이 **칸마다 같다** (결과서 3-3b) `[정적]`. 이름이 PDB 에 없어도 0xB8 칸 표를 믿을 만한 이유다.

### 2.11 이어가기 — `NtDebugContinue` 와 ContinueStatus `[정적]`

#### 새 말: 이어가기와 ContinueStatus

- **이어가기** = 디버거가 "이 편지 다 봤다, 아이 계속 움직여도 돼" 라고 커널에 알리는 일. `NtDebugContinue(우편함 핸들, 어느 아이의 어느 직원, ContinueStatus)` 꼴이다.
- **ContinueStatus** = 그 답장에 적는 **답 한 줄 (숫자)**. 커널은 이걸 편지의 답장 칸 (`ReturnedStatus`) 에 적어 보낸 직원에게 돌려준다.
  - 비유: 답장에 적는 **한 마디**. 값마다 뜻은 이번에 안 봤고, 하나 (`0x40010001`) 만 동작을 봤다 (이 절 끝).

#### 여섯 값만 받는다 — 핸들 열기 전에

- `+0x5c` · `+0x19c ~ +0x1d2`: ContinueStatus 가 다음 여섯 중 하나인지 본다.
  - `0x10002` · `0x80010001` · `0x10001` · `0x40010001` · `0x40010003` · `0x40010004`
- 아니면 `+0x1d8` **`0xc000000d`** — 우편함 번호표를 **열어 보기도 전에** 거절한다.
- 그다음에 `+0x90 ObReferenceObjectByHandle(RCX, 1, …)` (접근 1).
  - 읽을 때 주의: `NtDebugContinue.c` (Ghidra 디컴파일) 는 이 첫 인자를 잘못 그렸다. **`.asm` 이 맞다** — `+0x0 ~ +0x90` 사이에 RCX 를 바꾸는 줄이 없다.

#### 찾고 · 빼고 · 깨운다 — 단계별로

1. `+0xcb` · `+0xda ~ +0xe9`: `UniqueProcess` 같고 · `UniqueThread` 같고 · **READ 가 켜진** 편지를 찾는다.
   - → 아직 디버거에게 **건넨 적 없는** 편지는 이어 줄 수 없다.
2. `+0xef ~ +0x17c`: 그 편지를 줄에서 **뺀다** (링크가 틀리면 `INT 0x29`).
3. `+0xd5 ~ +0xd8` · `+0x108 ~ +0x119`: 같은 PID 의 **다음 편지 하나** 의 INACTIVE 를 풀고 `+0x114 KeSetEvent` 로 깃발을 다시 세운다. **하나만 풀고 끝.**
4. **`+0x13b`** 편지 답장 칸 (`이벤트+0x84`) = ContinueStatus · **`+0x141`** `Status` (`이벤트+0x48`) = 0.
5. **`+0x148 DbgkpWakeTarget`** → `+0x51 KeSetEvent(이벤트+0x10)` — 보낸 직원의 개인 신호등을 초록으로.
6. 못 찾으면 `+0x16b` **`0xc000000d`**.

- `0xc000000d` 는 "값이 여섯 밖" 과 "READ 인 그 편지 없음" 이 **같은 코드** 라서 부른 쪽은 둘을 구분 못 한다 `[정적]`.
- 비유: 답장을 넣으면 우편함에서 그 편지를 꺼내 버리고, 아이 문 앞 신호등을 초록으로 바꾸고, 같은 아이의 다음 편지 "대기" 딱지를 한 장 떼 준다.

#### 덤: `0x40010001` 은 "다시 보내"

- 보낸 쪽 `DbgkpSendApiMessage +0x9e ~ +0xa6`: status ≥ 0 이고 답장 칸이 **`0x40010001`** 이면 **같은 편지를 다시 우편함에 넣는다** `[정적]`.
- 그러니 이 값은 "계속해" 가 아니라 "**나중에 답할게**" 에 가깝다. 이름은 `DBG_REPLY_LATER` `[추측: 이름]`.

### 2.12 디버거가 우편함을 닫으면 `[정적]`

`DbgkpCloseObject` (마지막 번호표를 닫을 때 — `R9 ≤ 1` 일 때만이라는 건 `[추측]`):

1. `+0x2f` 우편함 `Flags |= 1` (닫는 중).
2. 편지 줄을 통째로 떼어 비운다.
3. `+0x52 KeSetEvent` (깃발).
4. 모든 프로세스를 훑어 `DebugPort == 이 우편함` 이면 → 연락선 0 · `DbgkpMarkProcessPeb` (`+0x26d2da`, `BeingDebugged` 0).
   - `Flags & 2` (KillOnClose) 면 `PsTerminateProcess(프로세스, 0xc0000354)` (`+0x26d2ec`).
5. 떼어 낸 편지마다 `Status = 0xc0000354` + `DbgkpWakeTarget` (`+0xe6`) — 문 앞에 서 있던 직원들이 **"우편함 없어짐"** 답을 받고 깬다.

- 비유: 부모가 우편함을 치우면, 문 앞에 서 있던 아이들은 "우편함 없어짐" 쪽지를 받고 움직인다. 다만 "우편함 치우면 가게도 닫아" 딱지 (KillOnClose) 가 있었으면 가게가 통째로 문을 닫는다.

### 2.13 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

#### 팀원 주석은 맞다 `[정적]`

| 주석 | 판정 |
|---|---|
| 출력 구조 `0xB8` B | **맞다** (`NtWaitForDebugEvent +0x4d` · `+0xb6 ~ +0xcc` · `+0x145 ~ +0x1ad`) |
| ContinueStatus 여섯 값 · 그 밖 invalid | **맞다** (값 · 순서 · 거절이 핸들 참조 **앞**) |
| 시간 초과일 때도 0 으로 채워 써 줌 | **맞다** (`0x101` · `0xc0` 도 같음) |
| "붙은 프로세스가 있으면 큐가 비었다고 말 못 함" (`:94` 멈춤) | **맞다** (진짜는 첫 스레드가 돌면 큐에 ① 이 있다) |

나머지 넷 (핸들 접근 1 · ClientId 로 찾기 · 순서 · `NtCreateDebugObject` flags 검사) 도 맞다 — run 문서 "팀원 주석이 맞나".

#### "기다림 가짜 성공" (D155) 은 진짜엔 없는 조합이다 — 단계별로

1. `[정적]` 진짜 `NtWaitForDebugEvent` 의 status 0 은 **늘** `NewState ≥ 2` · ClientId · 번호표가 찬 구조다.
2. `[정적]` 0 으로 찬 구조는 `0x102` · `0x101` · `0xc0` 일 때만 나온다.
3. 그러니 "status 0 + 빈 구조" 는 진짜 Windows 에는 없다.
4. 게다가 그 뒤 `NtDebugContinue` 는 찾을 READ 편지가 없어 `0xc000000d` 가 된다.

#### stub 이 해야 할 최소 — 큰 줄기만

- **엔진 몫** (자식 짓기):
  1. 속성 `0x60001` — 크기 8 아니면 `0xc000000d`, 값은 **열지 말고 저장만**.
  2. 짓기 끝 (`PspInsertProcess` 자리) — 접근 2 로 참조, `DebugPort` · `BeingDebugged` (WoW64 면 PEB32 도) · 조건부 `NoDebugInherit`. 닫는 중이면 안 붙이고 성공.
  3. 첫 스레드 알림 자리 — ① → ② (WoW64 면 ③) 를 **하나씩**, 넣을 때마다 보낸 쪽을 멈춰 두고 답이 와야 다음. 가상 자식이 실제로 안 돈다면 **부모가 처음 기다릴 때 ① 이 이미 큐에 있어야** 진짜와 같다.
- **팀원 몫** (`nt_debug_more.cpp`):
  - `NtWaitForDebugEvent` (`:94`) 를 2.9 고르기 규칙으로, `NtDebugContinue` (`:119`) 를 2.11 의 찾기 · 깨우기 · 다음 하나 풀기로.
- **`debug.hpp:8` flags 비트 메모**: 팀원 `flags` 는 **사용자 쪽 값** (bit 0 = KillOnClose) 이다. 진짜 커널 칸은 bit 0 닫는 중 · bit 1 KillOnClose · bit 2 WoW. 지금은 내부 표현이라 문제없지만, "닫는 중" · "WoW" 를 새로 모델할 때 **bit 0 에 섞지 말 것**.
- 엔진 쪽 "가상 자식에 TEB 가 없으면 ② 이름을 못 읽는다" 는 `[공백: 엔진 설계]`.
- 고칠지는 PE_emulator 팀 결정이다 (flow-writer 는 줄 번호가 있는지 · 맞는지만 봤다).

### 2.14 그래서 test.exe 에선? — 두 줄만

1. `[관찰]` (run44 사진) test.exe 여덟 장 모두 `DebugPort (null)` · `BeingDebugged 0`.
2. `[정적]` 디버그 속성이 없으니 `PspInsertProcess` 5번째 인자 = 0 → `+0x2f3` 갈래 안 탐.
3. `[정적]` `DbgkCopyProcessDebugPort +0x27` 이 0 을 쓰고, 만든 쪽 cmd 에도 연락선이 없어 (`+0x3c`) 그대로 끝.
4. `[정적]` 자식 첫 스레드 `DbgkCreateThread +0x9c` 에서 `DebugPort == 0` → 편지 없이 지나간다.
5. → test.exe 가 이번 길에서 밟는 건 **이 두 줄뿐** 이다. 그래서 지도 그림 1 · 2 에 Dbgk 상자를 넣을 실행 길이 없다.

- 우리 커널 디버거 (KDNET) 는 이 연락선을 안 쓴다는 것 → [run113 해설 2.7](run113-ntdll-user-structs-해설.md) "`BeingDebugged` 와 우리 디버거". **디버거가 붙어서 달라지는 점: 없음** (이번에 VM 안 돌림).

### 2.15 정리 — 비유로 한 번 더

1. 부모 (디버거) 가 **우편함** 을 하나 만들고, 아이를 낳는 신청서에 **"이 우편함" 쪽지** 를 붙인다 (`0x60001`).
2. 관리사무소는 신청서를 받을 때 쪽지를 **적어 두기만** 하고, 상가 명부에 올릴 때 비로소 쪽지가 진짜 우편함인지 열어 본다.
3. 진짜면 아이 가게 장부의 **감독관 연락선** 칸에 우편함 주소를 적고, 안내판에 **"감독관 있음"** (`BeingDebugged`) 을 켠다. 아이 직원은 아직 출근 전이다.
4. 아이 첫 직원이 출근 도장을 찍다가 연락선을 보고, **현관문을 나서기 전에** "가게 생겼어요" 편지를 우편함에 넣는다. 깃발을 세우고 **문 앞 신호등 앞에 선다.**
5. 부모가 깃발을 보고 우편함을 열어 맨 앞 한 장에 "읽음" 도장을 찍는다. 같은 아이의 뒤 편지엔 "대기" 딱지.
6. 부모가 답장을 넣으면 편지를 꺼내고, 아이 신호등을 초록으로 바꾼다. 아이는 들어가서 "ntdll 챙겼어요" 편지를 쓰고 또 선다.
7. 답장이 다 오면 아이 직원은 비로소 현관문을 나선다 (유저로 감).
8. test.exe 는 쪽지도 없고 엄마 가게에도 감독관이 없어서, 연락선 칸에 **빈칸** 을 적고 편지 없이 바로 나간다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

docs 커밋 `bf823e6`. 그림 1 · 2 에 Dbgk 노드가 없고, test.exe 가 새 노드를 넣을 실행 길도 없어서 **그림은 안 바꿨다.**

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (ntkrnlmp · ntdll 줄 · 근거 줄) | run154 없음 | run154 정적 바퀴 한 마디 + 사진 대조 줄 (`DebugPort (null)` · `BeingDebugged 0` `[관찰]` run44, 새 동적 없음) |
| 2.1 `PspInsertProcess` 디버그 포트 | "부모의 디버그 포트를 복사" | **좁힘** — "부모" = `NtCreateUserProcess` 를 부른 현재 프로세스, 핸들 속성이 없을 때만 `[정적]` + 디버그 속성 갈래 (`+0x2f3` 먼저 열기 → `+0x2c89e1` → `+0x2c8a0c` MarkPeb → `+0x322`) `[정적]` 새 글머리 |
| 2.1 run113 글머리 | `DbgkpMarkProcessPeb` (ret `+0x2c8972`) | 그 수에 줄 긋고 **함수 기준 call `+0x2c8a0c` · ret `+0x2c8a11`** (`+0x2c8972` 는 조각 기준) |
| 2.3 나무 · `DbgkCreateThread` | "디버그 포트가 있으면 CreateProcess · 모듈 메시지 (test.exe 는 없음)" — 본문 모름 | 나무에 한 줄 (`+0x9c DebugPort 0 → 이벤트 없음`) + 글머리 `[정적]` — ① → ② 차례 · 멈춤 · 깨움 · 깃발 둘 PDB 이름 |
| 3장 | 없음 | 새 절 **"디버그 속성으로 지은 자식 — 첫 이벤트 · 기다림 · 이어가기"** `[정적]` (나무 + 글머리 다섯, 표는 run 문서로) |
| 4장 | Q291 없음 | Q291 **해결됨** 한 줄 + run154 새 셋 (동적 · 정적 남은 것 · PE_emulator 쪽) |
| 5장 | — | run154 두 줄 |

- 태그 변화의 요점: 2.3 의 "디버그 포트가 있으면 …" 은 **이름만 아는 갈래** 였는데, 이번에 그 갈래 **본문** 이 `[정적]` 으로 열렸다. 다만 test.exe 실행 흐름은 그대로다.

### 짐 지도 `baggage.md` — 셈은 그대로, 두 줄에 경로 한 마디

- 3.1 안내판 `+0x2 BeingDebugged` 줄: (run154 경로) 디버그 속성 자식이면 `+0x2f3` 이 먼저 열고 · "부모" = 만든 쪽 (현재) 프로세스 · 뗄 때 `DbgkpCloseObject +0x26d2da` · `+0x2c8972` 은 조각 기준 (함수 기준 `+0x2c8a0c` / `+0x2c8a11`).
- 3.11 장부 `+0x308 DebugPort` 줄: (run154 경로) 우편함을 쓰는 `+0x2c89e1` 의 조건 (닫는 중 아님 · 우편함 뮤텍스) · 읽는 쪽 `DbgkCreateThread +0x9c` · `DbgkpQueueMessage +0x270`.
- test.exe 는 디버그 속성 없이 뜨므로 2장 숫자 · 3장 셈은 **안 바뀐다.**

### 앞 run 고쳐 읽기 (run 문서 그대로)

- **run113 `+0x2c8972`** → 조각 기준 수. 함수 기준 call `+0x2c8a0c` · ret `+0x2c8a11` (2.4). run113 문서 · run113 해설 맨 위에 한 줄씩.
- **"부모 `DebugPort` 복사"** (run50 · run113 · flow.md 2.1 · baggage.md) → "만든 쪽 (현재) 프로세스, 핸들 속성이 없을 때만". 틀린 게 아니라 좁힘이라 이전 문서 맨 위 줄은 안 넣었다 (flow.md 2.1 · 짐 지도 두 줄에 한 마디).
- **run50 "test.exe 가 안 타는 큰 갈래" 표의 "디버그 포트"** → 그대로 맞다. 이번엔 두 갈래의 **실행 순서** 와 그 뒤 일이 보태졌다.
- **baggage.md 3.11 `NoDebugInherit` 줄의 "`PspInsertProcess` 인자 6 비트 1"** → `PspInsertProcess` 로 세면 **4번째 인자**. 그 값이 `NtCreateUserProcess` 의 7번째 (0 부터 6) 인자라서 "인자 6" 은 그 셈일 수 있다 `[추측: 셈법]`. 짐 지도 셈과 무관 — 오케스트레이터 판단 거리로 보고만 됐다.

### 큰 그림에서 어떻게 이어지나

- test.exe 지도 (프로세스 생성 → `main`) 는 안 바뀐다. 이번 것은 **같은 길 위의 "만약 디버거가 있다면" 갈래** 를 끝까지 열어 본 것이다.
- 특히 "자식 첫 스레드가 **유저로 가기 전에** 커널 안에서 멈출 수 있다" 는 점은 지도 2.3 (`PspUserThreadStartup`) 의 한 자리에 붙는 사실이다.
- PE_emulator 는 이걸로 `0x60001` 속성 · 첫 이벤트 · 큐 · 이어가기를 **진짜 차례대로** 흉내 낼 근거를 얻었다. 380 정적 차례가 다 끝나면 (plan 표에서 Q282 · Q290 + Q276a · Q292 가 아직 대기) 그 뒤가 **2 단계 VM 수집 스크립트** 다 (`_workspace/plan.md` 다음 항목).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 실제 이벤트 차례 (2 → 5 → …) · 0xB8 실제 바이트 · 번호표 접근 `0x12067b` / `0x12007b` · 보낸 직원이 정말 유저 전에 서 있나 | 정적만 — 줄은 읽었지만 실행 때 값은 안 봤다. test.exe 는 이 길을 안 탄다 | **동적, 새 시험 exe 필요** (`CreateProcessA(자식, DEBUG_PROCESS)` + `WaitForDebugEvent` 루프). 멈춤점 9개는 run 문서 "아직 확인 필요" · flow.md 4장 run154 줄. 대기열 자리는 오케스트레이터가 정함 |
| 이벤트 코드 차례 · `lpStartAddress` = 기준 + 진입점인가 · `lpImageName` · 받은 핸들 `GrantedAccess` · 자식 PEB `+0x2` = 1 · 값별 `ContinueDebugEvent` 결과 · 우편함 닫으면 자식이 끝나나 | 같은 이유 | **디버거 없이** 유저 모드 시험 exe 로 떠 올 수 있음 → 380 **2 단계 VM 수집 스크립트** 재료 |
| attach 길 (`NtDebugActiveProcess` · `DbgkpSetProcessDebugObject`) — NOWAIT 편지의 INACTIVE 를 처음 푸는 곳 | 요청 범위 밖 (자식 짓기 길만 봄) | 정적 — 결과서 "다음 요청 후보" 1 |
| 첫 편지 뒤 (`DbgkForwardException` · `DbgkExitThread` · `DbgkExitProcess` · `DbgkUnMapViewOfSection` · `NtRemoveProcessDebug` · `DbgkClearProcessDebugObject`) | 요청 범위 밖 | 정적 — 후보 2 |
| 스레드 깃발 (`ClonedThread` · `HideFromDebugger` · `SkipCreationMsg`) 을 **켜는 곳** | 읽는 줄만 봤다 | 정적 — 후보 3 (`NtCreateThreadEx` 생성 플래그 → `PspAllocateThread`) |
| ntdll 나머지 (`DbgUiContinue` · `DbgUiConnectToDbg` · `DbgUiGetThreadDebugObject` · `DbgUiRemoteBreakin` · `LdrpInitializeProcess` 의 `BeingDebugged` 분기 = 시작 브레이크포인트) | ntdll 은 `DbgUi*` 셋만 뽑았다 | 정적 — 후보 4 |
| 자식 첫 스레드가 **언제 처음 스케줄되나** (`CREATE_SUSPENDED` · `NtResumeThread`) · 편지 `Flags 0x10` · `0x40` 을 읽는 곳 | 이번에 안 봄 | `[공백]` |
| `PsFreezeProcess` 가 부른 직원 자신을 안 얼리나 · `DbgkpCloseObject` 의 `R9` = 번호표 수인가 · kernelbase 내부 플래그 bit 1 → `NtCreateUserProcess` 7번째 인자 bit 1 인가 | 그 줄을 안 읽었다 | `[추측]` 으로 남음 (run 문서) |

---

## 5. 더 읽기

- run 문서: [`run154-dbgk-events.md`](run154-dbgk-events.md)
  - "정적 — 호출 지점" 표 (call · ret 전부) · 1 ~ 9절 · "호출 관계" 나무
  - "PE_emulator 쪽" — 이벤트 차례 표 · `0xB8` 칸 표 (NewState 전부) · stub 최소 · 팀원 주석 판정 · `debug.hpp` 비트 메모
  - "flow-writer 대조" (조각 기준 → 함수 기준으로 바꾼 곳들) · "앞 run 고쳐 읽기"
- 요청서 · 결과서: [`_workspace/sast/153_dbgk-events.md`](../../_workspace/sast/153_dbgk-events.md) · [`153_dbgk-events_result.md`](../../_workspace/sast/153_dbgk-events_result.md)
- flow.md ([`flow.md`](flow.md)):
  - 2.1 `PspInsertProcess` 디버그 포트 글머리 (run154 둘 · run113 수 정정)
  - 2.3 `PspNotifyThreadCreation` / `DbgkCreateThread` 글머리
  - 3장 "디버그 속성으로 지은 자식 — 첫 이벤트 · 기다림 · 이어가기"
  - 4장 run154 줄 넷 · 5장 run154 두 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.1 `BeingDebugged` · 3.11 `DebugPort`
- 앞 해설:
  - [run50 해설](run50-eprocess-create-해설.md) — `DebugPort` 0 쓰기 (`+0x27`), 가게 장부 · 상가 명부
  - [run51 해설](run51-eprocess-later-해설.md) 2.3 · 2.6 — `DbgkCreateThread +0x85` 첫 출근 도장
  - [run113 해설](run113-ntdll-user-structs-해설.md) 2.2 줄 ③ · 2.7 — `BeingDebugged` (맨 위 "고쳐 읽기 (run154)")
  - [run81 해설](run81-exception-kernel-해설.md) 2.9 — 감독관 연락선 (`DbgkForwardException`)
  - [run13 해설](../02_sprint/run13-resume-thread-해설.md) 2.3 · 2.10 — 신호등 (이벤트) · `KeWaitForSingleObject` 이름
