# 46 kthread-create — 정적 분석 요청

- 날짜: 2026-10-30
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run52)
- 대기열: **Q78d1** — Q78d (첫 스레드, 모르는 칸 564) 를 그 자리에서 나눈 첫 조각 (오케스트레이터 기본값 2026-10-30: 시간 순서 + 구조체별 — Q78d1 `_KTHREAD` 만들 때 180 → Q78d2 `_ETHREAD`·`_KTRAP_FRAME` 만들 때 169 → Q78d3 `_TEB` 133 → Q78d4 스레드 그 뒤 구간 (커널) 82)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.14 `_KTHREAD` (아는 칸 10 — 다시 보지 않는다), 7.2 `_KTHREAD` 모르는 칸 이름 (구간별), 2.1 사진 구간 표
  - `runs/02_sprint/run11`·`run13`·`run15`·`run16` (`PspAllocateThread`·`KeInitThread`·`KiInitializeContextThread`·`KxContextToKframes` 본문 요약)
  - 이전 요청 sast/44 (Q78c2a `_EPROCESS` 만들 때 — **같은 방식으로**: 쓰는 줄 + 끝까지 0 가르기)

## 범위

- `_KTHREAD` 모르는 칸 중 **180칸**:
  - `K1~K2` 42 (7.2 그대로): Header*, StackLimit, StackBase, ExpectedRunTime*, StateSaveArea, MiscFlags.AutoBoostActive, MiscFlags*, ThreadFlags.EnableStackSwap, ThreadFlags.KernelStackResident, ThreadFlags*, ApcStateFill*, Timer*, WaitBlock*, WaitBlockFill5*, WaitIrql*, WaitBlockFill6*, WaitBlockFill7*, WaitBlockFill8*, WaitBlockFill9*, WaitBlockFill10*, WaitBlockFill11*, Process, UserAffinity, PreviousMode, Affinity, NpxState, SchedulerApcFill1, SchedulerApcFill2*, SchedulerApcFill3*, SchedulerApcFill4*, SchedulerApcFill5*, MutantListHead, PropagateBoostsEntry, IoSelfBoostsEntry, GlobalForegroundListEntry, ForegroundDpcStackListEntry, GlobalUpdateVpThreadPriorityListEntry, UpdateVpThreadPriorityDpcStackListEntry, RealtimePriorityFloor, ModeHistory*, KernelAbEntries, UserAbEntries
  - `끝까지 0` 138 (7.2 그대로 — 대부분 `MiscFlags`·`ThreadFlags` 비트, 락, 카운터, 종료·UMS·보안 칸)
- **끝까지 0 138칸은 만들기 길 (`NtCreateUserProcess → PspAllocateThread` 와 그 callee, `KeInitThread`·`KiInitializeContextThread` 까지) 안에서만 본다**: 쓰는 줄이 있으면 (0 을 씀 / 조건부 안 탐 + 조건) 줄과 조건, 없으면 "아무도 안 씀 (만들기 길 안)", 종료·디버그 같은 범위 밖 길에서만 쓰면 이름만 ("범위 밖 길에서 씀 `[추측]`"). run50 과 같은 네 가지.
- 몸통 0 채움: 스레드 객체는 `PspAllocateThread +0x2e7 ObCreateObjectEx` (baggage 6장). 그 뒤 `memset`·`RtlZeroMemory` 가 있는지 확인하고, 0 채움 근거 (run49·run50 의 `ObpAllocateObject +0x191` → 할당) 를 그대로 쓸 수 있으면 쓴다.
- 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _KTHREAD` (이름 없는 공용체 안쪽 — `MiscFlags`·`ThreadFlags` 비트, `*Fill*` 칸 — 는 sast/38 `ms/disassemble/ntoskrnl/_baggage/struct__KTHREAD.tsv` 로). `*Fill*` 칸은 다른 칸과 같은 바이트를 덮는 자리 (공용체) 일 수 있다 — 겹치는 칸을 쓰는 줄이 그 칸의 쓰는 줄이면 그렇게 적는다.
- **범위 밖**: `_ETHREAD` 나머지·`_KTRAP_FRAME` (Q78d2), `_TEB` (Q78d3), `K2` 이후 구간 47칸 (Q78d4 — `KeStartThread`·`PspInsertThread`·스케줄러), `StateSaveArea`·`NpxState` 가 가리키는 XSAVE 영역 안쪽 내용 (칸 표 없는 짐 — 이름만).

## 알고 싶은 것

1. **`K1~K2` 42칸 쓰는 줄** — 칸마다 함수+오프셋, 값의 출처 (프로세스 `_KPROCESS` 에서 복사 / 인자 / 계산 / 상수 / 전역).
   - 후보: `KeInitThread` 본문 (`Header` 타입·크기, `Process`, `Affinity`·`UserAffinity` (`_KPROCESS` 친화도 복사), `PreviousMode`, `Timer` (`KeInitializeTimerEx` 류), `WaitBlock` 묶음, `MutantListHead`, 리스트 머리들 (`PropagateBoostsEntry` 등), `RealtimePriorityFloor`, `KernelAbEntries`·`UserAbEntries` (AutoBoost)), `KiInitializeContextThread` (`StackLimit`·`StackBase`·`StateSaveArea`·`NpxState`·`ThreadFlags.KernelStackResident`), 커널 스택 만들기 (`MmCreateKernelStack` — 이름만, 줄기 위면 결과를 받는 줄까지).
   - `*` 칸 (사진에서 뒤에 또 바뀜) 은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만 (Q78d4 또는 범위 밖 길).
2. **끝까지 0 138칸 가르기** — 만들기 길 안에서 네 가지. 비트 칸은 같은 바이트·DWORD 를 통째로 쓰는 줄 (예: `MiscFlags`·`ThreadFlags` 를 `MOV dword` 로 쓰는 줄) 이 있으면 그 줄이 0 을 쓴 것으로 본다 — 판단 기준을 결과서에 한 줄로 (run50 기준과 같으면 "같음").
   - 조건부 갈래 (시스템 스레드, 보안 스레드 (`SecureThread`), CET 그림자 스택 (`CetUserShadowStack`·`CetKernelShadowStack` — test.exe 에서 켜지는지는 사진 `MiscFlags` 값과 대조), UMS, GUI 스레드 등) 는 조건과 test.exe 가 왜 안 타는지.
3. **간접 호출·입력** — 만들기 길 안에 `_KTHREAD` 칸을 쓰는 간접 호출 (`cfg:@…`) 이 있으면 대상을 정적으로 좁히고, 못 정하면 동적 질문으로 (Q35 와 같은 질문이면 Q35 로).
4. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 경로는 `NtCreateUserProcess` 부터 지도 기준 (`①… → PspAllocateThread → KeInitThread → …`). 줄기 위는 끝까지, 줄기 밖·공용 부품 (`memset`, 할당, 락, `KeInitializeApc`·`KeInitializeEvent` 같은 초기화 도우미 — 이미 6장에 있으면 이름만) 은 이름만.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준 (2026-10-30 확인). **이미 `.c`·`.asm` 이 있는 함수는 다시 뽑지 않는다.**

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `PspAllocateThread` | `0x84296c` (`.c`/`.asm` 있음) | 0 | `KeInitThread` 앞뒤, 몸통 0 채움 — 읽기만 |
| ntoskrnl | `KeInitThread` | `0xb76490` (`.c`/`.asm` 있음) | 0 | 본문 직접 쓰기 — 읽기만 |
| ntoskrnl | `KiInitializeContextThread` | `0x3e9b70` (`.c`/`.asm` 있음) | 0 | 스택·상태 저장 영역 — 읽기만 |
| ntoskrnl | `KeInitializeTimerEx` | `0x456020` (36B) | 0 | `Timer` 칸 (필요하면) |
| ntoskrnl | `MmCreateKernelStack` | `0x253df0` | 0 | `StackBase`·`StackLimit` 값의 출처 (필요하면, 결과 받는 줄까지만) |
| ntoskrnl | (필요하면) `KeInitThread` 의 다른 callee 중 `_KTHREAD` 칸을 쓰는 것 | `_index.tsv` / `pdb_aliases.py --where` | 0 | 1·2번 답에 따라 |

- 위 목록이 다 필요하지 않으면 줄인다. objdump 로만 본 근거는 함수+오프셋으로 적고 "`.asm` 없음" 표시.

## 이미 아는 것

- `[정적]` baggage 3.14 아는 칸 10 (`InitialStack` `KiInitializeContextThread +0x2ca`, `KernelStack` `+0x2ce`, `ApcQueueable` `KeInitThread +0xed`, `Teb`, `SchedulerApc` `KeInitThread +0x152`, `SuspendEvent` `+0x163` 등) — 다시 보지 않는다.
- `[관찰]` run44: `K1~K2` 42칸 값은 K2 덤프에 있음 (값 대조에 써도 된다 — `ms/logs/04_baggage-read*` 와 run44 문서).

## 받고 싶은 것 (결과서 `_workspace/sast/46_kthread-create_result.md`)

- 맨 앞 요약 표: "180칸 → 아는 칸 (nt) / 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 (만들기 길 안) / 범위 밖 길에서 씀 / 여전히 모름" 숫자, `K1~K2`·끝까지 0 따로.
- 질문 1~4 답 + 근거 (`.asm` 의 +0x 줄 — 오프셋은 `.asm` 의 +0x 열에서만 읽는다). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3.14 양식, 바로 옮길 수 있게). 138칸 쪽은 묶어서 (예: "같은 DWORD `MiscFlags` 를 `+0x…` 에서 통째로 씀") 줄 수를 줄여도 된다.
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지 — 덤프면 Q79 로).
- Q78d2·Q78d4 로 넘길 것 (지나가며 본 `_ETHREAD`·`_KTRAP_FRAME`·뒤 구간 쓰는 줄).
- 코드는 길게 붙이지 않고 우리 말로.
