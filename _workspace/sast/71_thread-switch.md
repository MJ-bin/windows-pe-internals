# 71 thread-switch — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141b1** (PE_emulator 요청 25 ㄴ-1 — 스레드 넘기기). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 이번 묶음은 정적만.
- 관련: run52 (`KiAbProcessPreContextSwitch` caller 목록에 `KiSwapThread`·`KiQuantumEnd`·`KiDispatchInterrupt`·`KiExitDispatcher`·`KiDirectSwitchThread` …), run74 (`PspInsertThread +0x5a1 KeReadyThread` — 새 스레드가 실행 가능해지는 곳)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- PE_emulator (`../PE_emulator`) 의 nt 엔진은 `KiSwapThread` 라는 이름으로 "다음 스레드로 넘기기" 를 한다. 지금 하는 것: 스레드 목록에서 다음 실행 가능한 스레드를 고름, **우선순위 없음** `[추측]`.
- 이번 바퀴는 **넘기기 본문만** 본다. 퀀텀 (시계 → `KiQuantumEnd`) 은 다음 바퀴 Q141b2, 기다림·깨움은 Q141c — 이번엔 안 본다.
- 질문마다 "지금 우리 (PE_emulator) 가 맞나·틀리나" 한 줄.

## 알고 싶은 것

1. **`KiSwapThread`** (기다림 등으로 지금 스레드가 CPU 를 내놓을 때):
   - (a) 부르는 함수를 순서대로 (CALL 오프셋 + ret). 다음 스레드를 고르는 줄은 어디인가 — `KiSearchForNewThread` 를 부르나, 그 전에 `_KPRCB.NextThread` 를 먼저 보나?
   - (b) 실제로 바꾸는 줄 — `KiSwapContext` 를 부르는 오프셋.
   - (c) 돌아온 뒤 (다시 이 스레드 차례가 와서 깨어난 뒤) 하는 일 — 무엇을 돌려주나 (`WaitStatus` 를 읽어 돌려주나 — 칸 이름·오프셋만, 해석은 Q141c).
2. **`KiSearchForNewThread`** (고를 것을 찾는 쪽):
   - (a) 어디서 고르나 — 이 CPU 의 준비 목록 (`_KPRCB.DispatcherReadyListHead` + `ReadySummary` 비트맵 `[추측]`), 공유 준비 목록, 다른 CPU 에서 훔치기 — 순서대로.
   - (b) 가장 높은 우선순위부터 고르나 (비트맵에서 가장 높은 비트) — 근거 줄.
   - (c) 같은 우선순위 목록 안에서는 **맨 앞** 을 꺼내나?
   - (d) 고를 게 없으면 — `_KPRCB.IdleThread` 를 돌려주나, 줄.
3. **같은 우선순위끼리 차례** — 준비 목록에 넣을 때 **맨 뒤** 에 붙이나 **맨 앞** 에 붙이나, 무엇이 갈라나 (선점당함 = 앞 / 퀀텀 다 씀·양보 = 뒤 `[추측]`).
   - 넣는 함수 이름 (`KiDeferredReadyThread`·`KiDeferredReadySingleThread`·`KiInsertDeferredReadyList` 중 어느 것, `_index.tsv` 로 확인) 과 앞/뒤를 가르는 줄만. `KiDeferredReadySingleThread` 는 9230B 로 크다 — **목록에 넣는 줄 (앞/뒤)** 만 찾고 나머지는 풀지 않는다.
4. **`NtYieldExecution`** (16B — 꼬리 호출로 보임):
   - (a) 실제 몸은 어느 함수인가 (이름, `_index.tsv` 확인).
   - (b) 같은 우선순위의 다른 스레드가 있을 때만 넘기나, 지금 스레드를 준비 목록 맨 뒤로 보내나.
   - (c) `0x40000024` (`STATUS_NO_YIELD_PERFORMED`) 를 돌려주는 줄 — 조건 (넘길 스레드가 없을 때?).
   - (d) `0` (넘김) 은 언제.
5. **`KiSwapContext`** (214B, 손으로 쓴 asm 으로 보임): 하는 일만 짧게 — 저장하는 레지스터, 스택 바꾸는 줄, 주소 공간 (CR3) 바꾸는 갈래 (같은 프로세스면 안 바꾸나), 부르는 함수 (`SwapContext` 등).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^KiSwapThread$` (rva `0x2cc1f0`, 1637B) | 1 | 질문 1 |
| ntoskrnl | `^KiSearchForNewThread$` (rva `0x2cc848`, 675B) | 1 | 질문 2 |
| ntoskrnl | `^NtYieldExecution$` (rva `0x213780`, 16B) | 1 | 질문 4 — 꼬리 호출 대상까지 |
| ntoskrnl | `^KiSwapContext$` (rva `0x6b6460`, 214B) | 0 | 질문 5 |
| ntoskrnl | 질문 3 의 넣는 함수 (이름 확인 뒤) | 0 | 앞/뒤 줄만 |

- 넷 다 아직 `.asm`·`.c` 없음 (오케스트레이터 확인). 지도도 없다 → 먼저 `run_ghidra.sh map ntoskrnl.exe KiSwapThread` (와 필요하면 `NtYieldExecution`) 로 지도를 만들고 시작.
- 줄기 밖 callee (락·통계·ETW·`KiAbProcessPreContextSwitch` 등) 는 이름만.

## 이미 아는 것

- `[정적]` run52: `KiAbProcessPreContextSwitch` 를 부르는 곳 = `KiSwapThread`, `KiQuantumEnd`, `KiDispatchInterrupt`, `KiExitDispatcher`, `KiCheckForThreadDispatch`, `KiDirectSwitchThread`, `KiHandleDeferredPreemption` — 스레드가 CPU 에서 내려가는 때.
- `[정적]` run74: 새 스레드는 `PspInsertThread +0x5a1` (ret `+0x5a6`) `KeReadyThread` 에서 실행 가능해진다.
- `[추측]` (PE_emulator) 다음 스레드 = 목록에서 다음 실행 가능, 우선순위 없음.

## 받고 싶은 것

- 결과서 `_workspace/sast/71_thread-switch_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret 오프셋), `[정적]` 태그.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"** 짧은 목록 (예: 고르는 순서, 같은 우선순위 차례, idle, yield 결과값).
- 정적으로 못 닫는 것 (실행 때 값 — 예: 실제 우선순위 숫자) 은 "동적으로 확인할 것" 으로 — 대기열 Q104 에 붙인다 (이번 묶음엔 VM 안 함).
