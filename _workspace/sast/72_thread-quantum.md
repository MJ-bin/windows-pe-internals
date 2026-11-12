# 72 thread-quantum — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141b2** (PE_emulator 요청 25 ㄴ-2 — 퀀텀). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 정적만.
- 관련: sast/71 thread-switch (Q141b1 — `KiSwapThread`·`KiSearchForNewThread`·`KiSwapContext`·`NtYieldExecution`, 결과서 `_workspace/sast/71_thread-switch_result.md` 를 먼저 읽고 겹치지 않게), run52 (`KiAbProcessPreContextSwitch` caller 목록)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- PE_emulator 의 nt 엔진은 명령 `0x10000` 개마다 `KiQuantumEnd` 를 부르고, 목록에서 다음 실행 가능한 스레드로 넘긴다. 우선순위 없음 `[추측]`.
- 진짜 Windows 는 시계 인터럽트에서 퀀텀을 깎는다고 알려져 있다 `[추측]`: `KeClockInterruptNotify → KiUpdateRunTime → (퀀텀 끝 표시) → KiDispatchInterrupt → KiQuantumEnd`.
- PE_emulator 쪽 메모: `KiQuantumEnd` `.c` 는 디컴파일 색인에만 있고 asm 이 없었다 → 이번에 뽑는다.
- 이번 바퀴는 **퀀텀 사슬만**. 고르기·넣기 규칙 자체는 sast/71 결과를 가리키기만 한다.

## 알고 싶은 것

1. **사슬 확인**: `KeClockInterruptNotify` 가 `KiUpdateRunTime` 을 부르는 오프셋 (ret 같이). 중간에 `KiUpdateTime`·`KeAccumulateTicks` 를 거치나 (`_index.tsv` 에 둘 다 있음)? 사슬을 함수·오프셋으로.
2. **퀀텀 깎는 자리**: `_KTHREAD` 의 어느 칸 (`QuantumTarget`·`CycleTime` 등 — PDB 칸 이름) 을 무엇과 비교하나. 명령 수가 아니라 **CPU 사이클** 기준인가? 줄.
   - 한 퀀텀 길이를 정하는 전역·칸 (`KiCyclesPerClockQuantum` `[추측]` — 이 이름은 `_index.tsv` 함수 목록엔 없음, 전역이면 PDB 로 이름 확인) 과 그 값을 쓰는 곳 (부팅 때 계산이면 함수 이름만).
3. **`_KPRCB.QuantumEnd` 켜는 자리**: 함수·오프셋, 그리고 소프트웨어 인터럽트 (DPC/디스패치 수준) 를 요청하는 줄.
4. **`KiDispatchInterrupt → KiQuantumEnd`**: 부르는 오프셋, `QuantumEnd` 를 읽고 지우는 줄.
5. **`KiQuantumEnd` 가 다음 스레드를 고르는 꼴**:
   - (a) 퀀텀 다시 채우기 (`QuantumReset`·`QuantumTarget`) 줄.
   - (b) 우선순위 내리기 (`KiComputeNewPriority` 등, 이름만) 가 있나.
   - (c) 다음 스레드 고르기 — sast/71 의 `KiSearchForNewThread`/`KiSelectNextThread` 를 부르나, 같은 우선순위에 다른 스레드가 **없으면 그대로 계속** 도나 (줄).
   - (d) 지금 스레드를 준비 목록 **맨 뒤** 에 넣나 (sast/71 질문 3 의 넣는 함수로 이어지는 줄).
   - (e) 실제로 바꾸는 줄 (`KiSwapContext` 등).
6. `KiCheckForThreadDispatch`·`KiHandleDeferredPreemption` 은 이 사슬 위에 있나 (예/아니오, 이름만 — 선점 길이면 한 줄).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^KeClockInterruptNotify$` (rva `0x267fd0`, 2456B) | 1 | 질문 1 — 사슬 자리만, 본문 전부 풀지 않음 |
| ntoskrnl | `^KiUpdateRunTime$` (rva `0x2691d0`, 1801B) | 1 | 질문 2·3 |
| ntoskrnl | `^KiDispatchInterrupt$` (rva `0x224ff0`, 653B) | 1 | 질문 3·4 |
| ntoskrnl | `^KiQuantumEnd$` (rva `0x225290`, 3393B) | 1 | 질문 5 |
| ntoskrnl | `^(KiUpdateTime|KeAccumulateTicks)$` | 0 | 질문 1 — 사슬 위에 있을 때만 |

- 지도: `run_ghidra.sh map ntoskrnl.exe KeClockInterruptNotify` 로 만들고 시작 (sast/71 이 만든 지도가 있으면 같이 본다).
- 줄기 밖 callee (통계·ETW·전력·타이머 만료) 는 이름만. 타이머 만료 (`KiTimerExpiration`) 는 Q141c 에서 본다.

## 이미 아는 것

- `[정적]` run52: `KiAbProcessPreContextSwitch` caller = `KiSwapThread`, `KiQuantumEnd`, `KiDispatchInterrupt`, `KiExitDispatcher`, `KiCheckForThreadDispatch`, `KiDirectSwitchThread`, `KiHandleDeferredPreemption`.
- `[정적]` run52 (표): `PriorityDecrement` 를 쓰는 곳에 `KiQuantumEnd` 있음.
- sast/71 결과 (이 요청을 처리할 때 이미 있음): 고르기·넣기·`NtYieldExecution`.
- `[정적]` sast/71 (오케스트레이터가 `.asm` 대조): `KiSwapThread +0xdc` (ret `+0xe1`) `KiSearchForNewThread`, `+0x243` (ret `+0x248`) `KiSwapContext`, 돌려주는 값 `_KTHREAD.WaitStatus` (`+0x25c`·`+0x29a`). 준비 목록 넣기 = `KiDeferredReadySingleThread +0x1726/+0x172a` (기본 뒤, `Preempted` + 퀀텀 남음이면 앞 — 퀀텀 비교 `+0x144f CMP R13,[RBX+0x20]`). `NtYieldExecution` 몸 = `KeYieldExecution` (Ghidra 파일 `Ordinal_6.asm`).
- `[정적]` sast/71 지도 (`_map/KiSwapThread_edges.tsv`): **퀀텀 끝·선점은 `KiSwapThread` 를 안 거치고** `KiCheckForThreadDispatch +0x10e`·`KiHandleDeferredPreemption +0x297` 에서 `KiSwapContext` 를 직접 부르는 길이 있다 → 질문 5(e)·6 에서 이 둘이 `KiQuantumEnd` 뒤에 오는지 확인.
- `_KPRCB` 는 공개 PDB 에서 `+0x6d0` 에서 잘려 있다 (sast/71) — 그 뒤 칸은 오프셋 + `[추측]` 이름으로.

## 받고 싶은 것

- 결과서 `_workspace/sast/72_thread-quantum_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- 질문마다 "지금 우리 (명령 `0x10000` 개마다 `KiQuantumEnd`, 우선순위 없음) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"** — 퀀텀 단위 (사이클/틱), 끝났을 때 고르기, 같은 우선순위 없으면 계속.
- 실행 때만 정해지는 값 (퀀텀 길이 숫자, 시계 간격) 은 "동적으로 확인할 것" — Q104 에 붙인다 (이번 묶음엔 VM 안 함). 이미 있는 덤프 로그 (`ms/logs/`, run44 사진 텍스트) 에 그 전역 값이 찍혀 있으면 그걸 근거로 써도 된다.
