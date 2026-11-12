# 75 thread-exit — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141d** (PE_emulator 요청 25 ㄹ — 스레드 끝내기). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 정적만.
- 관련: sast/73 thread-wake (Q141c1 — `KeSetEvent` 깨움 꼴, `KeWaitForSingleObject` (= `KeWaitForMutexObject`), `NtWaitForSingleObject +0x65 → ObWaitForSingleObject +0xba`), sast/71 (run75 넘기기), run74 (`MmCreateTeb`·`RtlCreateUserStack` — 만든 쪽), run12·run53 (`PspExitThread` 이름이 나온 곳), run15 (`PspTerminateThreadByPointer`)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- test.exe [소스] `sample/test.c:34-35`: `CreateThread(NULL, 0, Worker, NULL, 0, NULL)` 뒤 `WaitForSingleObject(t, INFINITE)`. 둘째 스레드는 `Worker` 가 돌아오면 `RtlUserThreadStart → BaseThreadInitThunk → RtlExitUserThread → NtTerminateThread` 로 끝난다 (`BaseThreadInitThunk +0x20 RtlExitUserThread` [정적] run25). 메인은 그 스레드 객체가 신호를 받을 때까지 기다린다.
- PE_emulator 의 지금: `PspExitThread` 이름으로 끝내기를 하지만 **TEB·유저 스택 풀기는 공사 중 (안 품)**. PE_emulator 쪽 메모: ntdll `RtlExitUserThread` 는 스택을 안 품 `[정적]` (PE_emulator 원문).
- 알고 싶은 핵심: 커널이 끝날 때 **무엇을 어떤 순서로** 하나 — 스레드 객체 신호·기다리던 쪽 깨우기, TEB·유저 스택 풀기, 마지막 스레드 갈래.

## 알고 싶은 것

1. **사슬**: `NtTerminateThread` (rva `0x8927e0`) 에 핸들 0 (자기 자신) 이 오면 → `PspTerminateThreadByPointer` → `PspExitThread` 로 가나 — 함수·오프셋 (ret). 자기 자신일 때와 남일 때 갈리는 줄.
2. **`PspExitThread` 본문 순서** (줄기만, 오프셋으로):
   - (a) `ActiveThreads` (`_EPROCESS` 칸) 를 내리는 줄, 0 이 되면 `PspExitLastThread` 로 가는 갈래 (`+0x?`).
   - (b) **TEB 풀기**: `MmDeleteTeb` (rva `0x976370`, 이미 있음) 를 부르는 오프셋과 조건 (유저 스레드일 때만? 마지막 스레드면 안 부르나?).
   - (c) **유저 스택 풀기**: 커널이 `TEB.DeallocationStack` (`+0x1478`) 을 `ZwFreeVirtualMemory`/`MmFreeVirtualMemory` 로 푸는 줄이 있나 (예/아니오, 오프셋, 조건 — `SameTebFlags` 의 비트 (`FreeStackOnTermination`?·`InitialThread`) 를 보나). 없으면 "커널은 스택을 안 품" 이라고.
   - (d) 프로세스의 스레드 목록 (`_EPROCESS.ThreadListHead`) 에서 빼는 줄.
   - (e) `KeTerminateThread` 를 부르는 오프셋 (보통 마지막 — 돌아오지 않음).
   - 그 밖 콜백·통지 (`PspCallThreadNotifyRoutines` 등)·APC 정리·LPC/ALPC 정리는 **이름과 오프셋만** (순서 표에 한 줄씩).
3. **`KeTerminateThread`** (rva `0x2cbce8`, 821B):
   - (a) 스레드 객체 (`_DISPATCHER_HEADER.SignalState`) 를 1 로 만드는 줄.
   - (b) 기다리던 쪽 (메인 `WaitForSingleObject(t)`) 을 깨우는 꼴 — sast/73 의 `KeSetEvent` 수동 재설정 깨움과 같은가 (모두 깨우고 신호로 남음), 부르는 함수 이름 (`KiTryUnwaitThread`·`KiProcessThreadWaitList`·`KiExitDispatcher` 등).
   - (c) `State = Terminated` (`_KTHREAD.State` 값) 쓰는 줄, 마지막에 `KiSwapThread` (또는 다른 넘기기) 로 가는 줄 — 돌아오지 않음.
   - (d) 프로세스 쪽 (`_KPROCESS`) 스레드 목록에서 빼는 줄 (있으면).
4. **`PspExitLastThread`** (rva `0x9b1500`, 739B) — 마지막 스레드일 때 하는 일: 프로세스 종료 길 (`PspExitProcess` 등, 이름만) 로 이어지나, TEB·스택 처리가 다른가.
5. **남의 스레드 끝내기**:
   - (a) `PspTerminateThreadByPointer` (rva `0x8928c0`, 216B) — 남이면 APC 를 걸고 (`PsExitSpecialApc`? 이름 확인) 돌아오나, 자기면 바로 `PspExitThread`.
   - (b) `PspTerminateAllThreads` (rva `0x935e48`, 654B) — 프로세스의 모든 스레드에 무엇을 하나 (자기 빼고 APC?), 부르는 쪽 (`NtTerminateProcess`·`PspTerminateProcess`, 이름만).
6. **`ObWaitForSingleObject` 가 스레드 객체를 받는 길** (rva `0x8712f0`, sast/73 에서 이미 뽑힘): 스레드 객체 (`_ETHREAD`) 에서 기다릴 디스패처 헤더를 어떻게 고르나 (객체 형식의 `DefaultObject` 오프셋? 바로 `_KTHREAD.Header`?) — 줄.
7. **스레드 객체가 지워질 때** `PspThreadDelete` (rva `0xa7da30`, 1314B): TEB·스택을 여기서 푸나 (2(b)·(c) 와 겹치면 어느 쪽인지) — 해당 줄만. 이 함수는 객체 참조가 0 이 될 때 간접으로 불린다 `[추측]` — 등록하는 곳은 이름만.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtTerminateThread$` | 1 | 질문 1 |
| ntoskrnl | `^PspTerminateThreadByPointer$` | 1 | 질문 1·5(a) |
| ntoskrnl | `^PspExitThread$` (rva `0x8929a0`, 2329B) | 1 | 질문 2 — 줄기만 |
| ntoskrnl | `^KeTerminateThread$` (rva `0x2cbce8`, 821B) | 1 | 질문 3 |
| ntoskrnl | `^PspExitLastThread$` (rva `0x9b1500`, 739B) | 1 | 질문 4 |
| ntoskrnl | `^PspTerminateAllThreads$` (rva `0x935e48`, 654B) | 1 | 질문 5(b) |
| ntoskrnl | `^(MmDeleteTeb|ObWaitForSingleObject)$` | 0 | 질문 2(b)·6 — 이미 있음 |
| ntoskrnl | `^PspThreadDelete$` | 0 | 질문 7 — TEB·스택 줄만 |

- 지도: `run_ghidra.sh map ntoskrnl.exe NtTerminateThread` (와 필요하면 `PspExitThread`).
- 줄기 밖 (ETW·감사·작업 객체·전력·Win32k 콜백) 은 이름만. **함수가 많으니 질문에 답하는 줄만** 읽는다.

## 이미 아는 것

- `[정적]` run25: `BaseThreadInitThunk +0x20` → ntdll `RtlExitUserThread`.
- `[정적]` run74: TEB 칸 `+8/+0x10/+0x1478` (StackBase/StackLimit/DeallocationStack) 은 `MmCreateTeb +0x12a ~ +0x13a` 가 채움. 유저 스택은 커널 `RtlCreateUserStack` 이 잡음 (예약 = `ZwSetInformationProcess(0x29)` `[추측]`, 커밋 `+0x170`, 가드 `+0x1c6`).
- `[정적]` run74: 둘째 스레드 `SameTebFlags` 는 커널이 안 씀 (기대 0 `[추측]`), 첫 스레드는 `0x400` (`InitialThread`).
- `[정적]` sast/73: 수동 재설정 깨움 = 모두 깨우고 신호로 남음, 단일 객체 기다림의 `WaitStatus` = 0, `KeSetEvent +0x10c → KiExitDispatcher`. 기다림 출구 `NtWaitForSingleObject +0x65 → ObWaitForSingleObject +0xba → KeWaitForSingleObject`.
- `[추측]` (PE_emulator) TEB·스택 풀기 공사 중.

## 받고 싶은 것

- 결과서 `_workspace/sast/75_thread-exit_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **`PspExitThread` → `KeTerminateThread` 순서 표** (한 줄 = 한 일, 오프셋).
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"** — 끝날 때 할 일 순서, TEB·스택을 누가 언제 푸나, 기다리던 쪽 깨우기, 마지막 스레드.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다 (이번 묶음엔 VM 안 함).
