# 74 thread-timer — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141c2** (PE_emulator 요청 25 ㄷ-2 — 기다림의 마감·타이머). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 정적만.
- 관련: sast/73 thread-wake (Q141c1 — 기다림 블록 달기·깨움·`WaitStatus`, 결과서 `_workspace/sast/73_thread-wake_result.md` 를 먼저 읽고 겹치면 가리키기만), sast/72 thread-quantum (run76 — 시계 인터럽트 `KeClockInterruptNotify → KiUpdateTime`, `KiCheckForTimerExpiration` 이 사슬 위에 있으면 거기서 이어짐), sast/71 (run75 — `KiSwapThread` 가 `WaitStatus` 돌려줌)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- PE_emulator 의 지금 (`[추측]`): 스레드를 넘길 때마다, 마감이 지난 잠든 스레드를 **먼저 깨운다** (결과 `0x102` = `STATUS_TIMEOUT`).
- 알고 싶은 핵심: 진짜 커널에서 마감 (timeout) 이 있는 기다림이 **언제·누구에 의해** 끝나나, 결과값을 어디에 쓰나, 신호와 마감이 겹치면 어떻게 하나.
- 이번 바퀴는 **타이머 만료 길만**. 기다림 블록·깨움 자체는 sast/73 결과를 가리킨다.

## 알고 싶은 것

1. **마감을 거는 쪽** (기다림이 시작될 때):
   - (a) `KiCommitThreadWait` (또는 sast/73 이 찾은 자리) 가 스레드 안 타이머 (`_KTHREAD.Timer` `[추측]`) 를 거는 줄 — 부르는 함수 (`KiInsertTimerTable`·`KiSetTimerEx`·`KiComputeDueTime` 중) 와 오프셋.
   - (b) **상대 시간** (음수 마감, 예: `-10000000` = 1초 뒤) 을 절대 시간으로 바꾸는 줄 — 무엇에 더하나 (`InterruptTime`? `_KUSER_SHARED_DATA` 칸?), 결과가 남는 칸 (`_KTIMER.DueTime` `[추측]`). 절대 시간 (양수) 이면 다른 갈래가 있나.
   - (c) 마감이 이미 지났으면 (0 이나 지난 시각) 기다리지 않고 그 자리에서 `0x102` 를 돌려주나 — 줄.
2. **만료를 알아채는 쪽**: 시계 인터럽트에서 "만료된 타이머가 있다" 를 보는 줄 — `KiCheckForTimerExpiration` (rva `0x269ec0`) 이 sast/72 사슬 (`KeClockInterruptNotify`/`KiUpdateTime`) 위에 있나, 거기서 DPC (`KiTimerExpirationDpc`?) 를 요청하나 — 함수·오프셋.
3. **`KiTimerExpiration → KiProcessExpiredTimerList → KiTimerWaitTest`**:
   - (a) 부르는 사슬 (오프셋, ret). `KiRetireDpcList` 에서 오나 (이름만).
   - (b) `KiTimerWaitTest`: 기다리던 스레드의 `WaitStatus` (`_KTHREAD +0xc8`) 에 `0x102` 를 쓰는 줄. 기다림 블록을 떼는 줄 (sast/73 `KiTryUnwaitThread` 를 부르나).
   - (c) 깨운 스레드를 준비 줄로 보내는 길 (sast/71 `KiDeferredReadySingleThread` 로 이어지나 — 이름만).
4. **신호와 마감이 겹칠 때**: 같은 스레드를 깨우려고 신호 쪽 (`KiTryUnwaitThread`) 과 타이머 쪽 (`KiTimerWaitTest`) 이 겨룰 때 — 먼저 잡은 쪽이 이기고 다른 쪽은 지나치는 줄 (락·상태 비교·CAS). 정적으로는 **규칙** 만 (실제 순서는 런타임).
5. **PE_emulator 흉내와 비교**: "스레드를 넘길 때마다 마감 지난 것을 먼저 깨움" 과 진짜 ("시계 틱 → DPC → 만료 처리") 의 차이 — 결과값 (`0x102`) 과 깨운 뒤 차례 (준비 줄 앞/뒤, 우선순위 올려 주기) 가 같은가.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^KiTimerExpiration$` (rva `0x2ed8a0`, 2816B) | 1 | 질문 3 — 사슬 자리만, 본문 전부 풀지 않음 |
| ntoskrnl | `^KiProcessExpiredTimerList$` (rva `0x2ed260`, 1260B) | 1 | 질문 3 |
| ntoskrnl | `^KiTimerWaitTest$` (rva `0x2ec830`, 1025B) | 1 | 질문 3·4 |
| ntoskrnl | `^KiCheckForTimerExpiration$` (rva `0x269ec0`, 1548B) | 1 | 질문 2 |
| ntoskrnl | `^(KiInsertTimerTable|KiComputeDueTime)$` | 0 | 질문 1 — 마감 계산·다는 줄만 |
| ntoskrnl | `^KiTimerExpirationDpc$` | 0 | 질문 2·3 — 사슬 위에 있을 때만 |

- `KiCommitThreadWait`·`KeWaitForMutexObject` 는 sast/73 에서 이미 뽑힘 — 마감 거는 줄만 다시 본다.
- `Timer2` (`KiTimer2Expiration`·`KiExpireTimer2`) 는 고해상도 타이머 쪽 `[추측]` — 기다림 마감이 그쪽을 쓰지 않으면 이름만.
- 지도: `run_ghidra.sh map ntoskrnl.exe KiTimerExpiration` (와 필요하면 `KiCheckForTimerExpiration`).
- 줄기 밖 (전력·통계·ETW·DPC 관리) 은 이름만.

## 이미 아는 것

- `[정적]` sast/71 (run75): `KiSwapThread` 는 `_KTHREAD.WaitStatus` (`+0xc8`) 를 돌려준다 (`+0x25c`·`+0x29a`).
- `[정적]` sast/72 (run76): 시계 사슬 `HalpTimerClockInterrupt +0x103 → KeClockInterruptNotify`; 주인 CPU 는 `+0x2ba → KiUpdateTime`, 아니면 `+0x448 → KiUpdateRunTime`. 디스패치 인터럽트에서 DPC 를 먼저 돌림 (`KiDispatchInterrupt +0x60`).
- sast/73 결과 (이 요청을 처리할 때 이미 있음): 기다림 블록·깨움·`WaitStatus` 쓰는 줄.
- `[추측]` (PE_emulator) 넘길 때마다 마감 지난 잠든 스레드를 먼저 깨움 (`0x102`).

## 받고 싶은 것

- 결과서 `_workspace/sast/74_thread-timer_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"** — 마감 계산, 만료 때 할 일, 겹칠 때 규칙.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다 (이번 묶음엔 VM 안 함).
