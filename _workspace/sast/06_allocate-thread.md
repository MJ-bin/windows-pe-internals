# 06 allocate-thread — 정적 분석 요청

- 날짜: 2026-10-21
- 스프린트: 02_sprint
- 대기열: **Q3** — 2.1 `nt!PspAllocateThread`, `nt!PspInsertProcess` callee (`_workspace/plan.md`)
- 관련: `runs/02_sprint/flow.md` 그림 1, 2.1 (`PspAllocateThread`·`PspInsertProcess` 절은 지금 `[관찰]` 호출 관계 + `[추측]` 역할뿐) / 이전 요청 sast/03 (run9), sast/04 (run10)

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`, 2026-10-21 하네스판)

- `PspAllocateThread`: 깊이 1, caller 3곳, expanded. 직접 호출 34줄(서로 다른 이름 30). caller 는 `NtCreateUserProcess +0xe5c` 외 2곳.
  - 눈에 띄는 것: `ObCreateObjectEx`, `PsAssignThreadId`, `PspSetupUserStack`, **`MmCreateTeb`** (points.md `[확인 필요]` 이름, 깊이 2·caller 1곳), **`KeInitThread`**, `PsQueryThreadStartAddress`, 조각 2개(`FUN_140843198`, `FUN_140b0d265`: `MmDeleteTeb`, `PspDeleteUserStack` 등 실패 정리로 보임).
  - **오프셋이 `+0x2caa17` 처럼 아주 큰 줄 6개**가 있다 (`RtlGetExtendedContextLength`, `ExAllocatePool2`, `PsQueryThreadStartAddress`, `RtlLocateExtendedFeature`, `KiStackAttachProcess`, `MmSecureVirtualMemory`). `frag:` 표시가 없으니 Ghidra 가 멀리 떨어진 블록을 **함수 본체에 포함**시킨 것으로 보인다 `[추측]`.
- `PspInsertProcess`: 깊이 1, caller 3곳 (`NtCreateUserProcess +0xfc2`, `PsCreateMinimalProcess +0x2ed` 등), expanded. 직접 호출 21줄(서로 다른 이름 20).
  - 눈에 띄는 것: `SeAuditProcessCreation`, `PspLockProcessListExclusive` / `PspUnlockProcessListExclusive`, `PspImplicitAssignProcessToJob`, `DbgkCopyProcessDebugPort`, `ObInsertObjectEx`.
- `PspUserThreadStartup` 은 `_index.tsv` 에 함수로 있지만 **지도에 없다** (호출로 닿지 않음). 함수 주소를 값으로 넘겨 나중에 "스레드 시작 함수"로 쓰이는 것 같다 `[추측]`.

## 알고 싶은 것

1. `PspAllocateThread` 가 직접 부르는 함수를 **하는 일 단위로** 묶고, 단위마다 대표 1~3개 (이름, ret_off, 역할 한 줄). 특히 아래가 일어나는지와 순서 (예/아니오 + ret_off):
   - (가) 스레드 객체(`_ETHREAD`) 만들기
   - (나) 스레드 ID 받기
   - (다) **유저 스택** 만들기
   - (라) **TEB 만들기** (`MmCreateTeb` — points.md 이름 확인)
   - (마) 커널 쪽 스레드 초기화 (`KeInitThread`)
2. **첫 스레드가 어디서 출발하게 되나** (2.3 과 이어짐, 짧게):
   - `PspAllocateThread` 가 `KeInitThread` 에 넘기는 인자 중에 `PspUserThreadStartup` 의 **주소**(데이터 참조)가 있나? (예/아니오 + 근거 줄). 없으면 `PspUserThreadStartup` 주소를 참조하는 곳이 이 함수 안 어디인지, 없으면 "이 함수엔 없음".
   - 유저 쪽 시작 주소(01_sprint 관찰: Win32 Start Address = `test!mainCRTStartup`)가 이 함수에 어떤 경로로 들어오나 (인자 / `PsQueryThreadStartAddress` 등). 코드로 보이는 만큼만.
   - 깊이 파지 말 것. `KeInitThread` 본문은 뽑지 않는다 (Q6·Q7 몫).
3. `PspInsertProcess` 가 직접 부르는 함수를 같은 방식으로 묶고 대표 1~3개. 특히:
   - (가) **프로세스 목록에 올리기**: 잠금(`PspLockProcessListExclusive`)과 풀기 사이에서 목록에 넣는 일이 호출인지, 함수 안에서 직접 하는지 (코드로 보이는 만큼).
   - (나) 핸들 만들기 (`ObInsertObjectEx`) 를 이 함수가 하나? 부모(cmd.exe)에게 돌려줄 프로세스 핸들은 run9 에서 `NtCreateUserProcess +0x10a7 PspCreateObjectHandle` 이었다 — 둘의 관계(같은 일인지 다른 일인지)를 코드로 보이는 만큼.
   - (다) 감사·잡(job)·디버그 포트 처리 — 한 줄씩.
4. 큰 오프셋 줄(`+0x2caa17` 등): 진짜로 `PspAllocateThread` 본체의 일부인가 (Ghidra 함수 본체 범위로 확인), 어떤 경우에 그리로 가나 (조건 한 줄). 이 줄들의 오프셋은 WinDbg 콜스택과 같은 기준으로 쓸 수 있나 (`caller+0x2caa1c` 처럼 보일지) — 모르면 "모름, 동적 확인 거리".
5. 간접 호출(`indirect:`, `indirect:cfg:`), 꼬리호출(`tail`), 별칭(`alias:`), 조각(`frag:`)은 reference.md 규칙대로. `alias:` 이름은 대표로 고르지 않는다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(PspAllocateThread\|PspInsertProcess)$` | 0 | Q3 본체 |

- depth 0. 다른 함수 본문이 꼭 필요하면 지도로 먼저 찾고, 넓힌 이유를 결과서에 쓴다.
- analyze 다시 하지 않는다. `_calls.tsv` 는 이어서 쌓는다.

## 이미 아는 것

- `[관찰]` (run3) `NtCreateUserProcess+0xe5c` → `PspAllocateThread` (프로세스를 목록에 올리기 **전에** 불림), `+0xfc2` → `PspInsertProcess`. 부모 cmd.exe 스레드.
- `[관찰]` (run3) `PspCallProcessNotifyRoutines` 시점에 `!process 0 0 test.exe` 로 test.exe 가 보였다 (= 그 전에 목록에 올라감).
- `[관찰]` (run4, run5) 메인 스레드 Win32 Start Address = `test!mainCRTStartup`, 모든 새 스레드가 `nt!PspUserThreadStartup` 에서 출발.
- `[정적]` (run9) `NtCreateUserProcess` 는 `PspAllocateThread` 앞에서 `PspCreateUserContext`(+0xd22, 첫 스레드 유저 시작 문맥)와 `PspMapThreadCreationFlags`(+0xdd3)를 부른다.

## 받고 싶은 것

- 질문 1~5 답 + 근거 (`.asm` 의 `+0x` 줄, `.c` 요약). 대표 지점은 **ret_off**, call_off 는 괄호.
- 동적으로 확인할 것 + 추천 멈춤점 (있으면).
- 결과서: `_workspace/sast/06_allocate-thread_result.md`
