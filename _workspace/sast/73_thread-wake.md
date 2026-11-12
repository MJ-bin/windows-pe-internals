# 73 thread-wake — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141c1** (PE_emulator 요청 25 ㄷ-1·ㄷ-3 — 기다림·깨움과 syscall 출구). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 정적만.
- 관련: sast/71 thread-switch (run75 — `KiSwapThread` 가 `_KTHREAD.WaitStatus` 를 돌려줌, caller `KiCommitThreadWait +0x220`), sast/72 thread-quantum (Q141b2 — 결과서 `_workspace/sast/72_thread-quantum_result.md`, 겹치면 가리키기만), run13 (`KeSetEvent`·`KeResumeThread`), run52·run53 (`AbWaitObject`·`Preempted`·`AdjustReason` 쓰는 함수 목록)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- PE_emulator 의 지금 (`[추측]`):
  - 기다림: 스레드를 "잠듦" 으로 표시하고 다른 스레드로 넘김.
  - 깨움 (`NtSetEvent`·`NtReleaseMutant` 등 팀원 syscall 몸): 기다리던 스레드를 **"실행 가능" 표시만** 함. 깨난 스레드가 자기 syscall 을 **다시 물어** (조건 다시 검사) 결과를 받음.
- 알고 싶은 핵심: 진짜 커널은 **깨우는 쪽이 그 자리에서 기다림을 채우나** (자동 재설정 이벤트를 그 자리에서 0 으로, 뮤턴트 주인을 그 자리에서 넘김, 깨난 쪽 `WaitStatus` 를 그 자리에서 씀) — 그래서 깨난 쪽은 다시 묻지 않고 바로 돌아가나.
- test.exe 에서 실제로 쓰는 기다림 [소스] `sample/test.c:35`: `WaitForSingleObject(t, INFINITE)` — 스레드 객체 기다림. 스레드 객체가 신호를 받는 쪽 (끝내기) 은 Q141d 에서 본다. 이번엔 **이벤트·뮤턴트·공통 기다림 틀**.
- 타이머 만료 (`KiTimerExpiration` 등) 는 다음 바퀴 Q141c2 — 이번엔 안 본다. 기다림 블록에 마감 (timeout) 을 거는 줄만 이름·오프셋으로 가리킨다.

## 알고 싶은 것

1. **기다리는 쪽 — `KeWaitForSingleObject` (Ghidra 이름 `KeWaitForMutexObject`, rva `0x217eb0`) → `KiCommitThreadWait`**:
   - (a) 들어가기 전에 이미 신호 상태면 그 자리에서 채우고 돌아가나 (기다리지 않음) — 줄. 이때 자동 재설정 이벤트를 0 으로, 뮤턴트를 가져가는 줄 (`KiWaitSatisfyMutant`·`KiWaitSatisfyOther`).
   - (b) 신호가 아니면: 기다림 블록 (`_KWAIT_BLOCK`) 을 객체 목록에 다는 줄, `_KTHREAD.State` 를 Waiting 으로, 마감이 있으면 타이머를 거는 줄 (이름만), 그리고 `KiCommitThreadWait +0x220 KiSwapThread`.
   - (c) **깨난 뒤**: `KiSwapThread` 가 돌려준 `WaitStatus` 를 그대로 돌려주나? 아니면 조건을 다시 검사하는 반복 (`STATUS_KERNEL_APC` `0x100` 이면 다시 기다림 등) 이 있나 — 반복 조건을 줄로.
2. **깨우는 쪽 — `KeSetEvent`** (rva `0x25b3e0`, 1920B):
   - (a) 기다리는 스레드가 있을 때, 자동 재설정 (`SynchronizationEvent`) 이면: 첫 기다림 하나만 깨우고 이벤트는 **그 자리에서 0** 으로 두나 (신호 상태로 남지 않음)? 수동 재설정 (`NotificationEvent`) 이면 모두 깨우고 신호 상태로 남나?
   - (b) 깨울 때 기다리는 스레드의 `WaitStatus` 를 쓰는 줄 (값 = 기다림 블록 번호? `STATUS_WAIT_0 + n`).
   - (c) 깨우는 함수 이름 — `KiTryUnwaitThread` 인가, `KiProcessThreadWaitList` 인가, 둘 다인가. 거기서 스레드를 준비 줄로 보내는 줄 (sast/71 `KiDeferredReadySingleThread` 로 이어지나 — 이름만).
   - (d) 돌려주는 값 (이전 상태).
3. **`KiTryUnwaitThread`** (rva `0x3a87d0`, 845B): 한 스레드를 깨우는 꼴 — 기다림 블록들을 객체 목록에서 떼는 줄, `WaitStatus` 쓰는 줄, 이미 다른 쪽이 깨웠을 때 (경쟁) 어떻게 지나치나 (줄만, 실제 순서는 런타임).
4. **`KeReleaseMutant`** (rva `0x263540`, 52B — 감싸기로 보임) → 몸:
   - (a) 몸 함수 이름.
   - (b) 놓을 때 횟수 0 이 되면 기다리는 첫 스레드에게 **그 자리에서 주인을 넘기나** (`OwnerThread` 를 깨운 스레드로 쓰는 줄)? 아니면 신호 상태로만 두고 깨난 쪽이 가져가나?
   - (c) 버려진 뮤턴트 (`Abandoned`) 를 넘길 때 `WaitStatus` (`STATUS_ABANDONED_WAIT_0` `0x80`) 줄 — 있으면.
5. **`KePulseEvent`** (rva `0x3a9600`, 497B): 기다리는 쪽을 깨우고 이벤트를 다시 0 으로 — 수동/자동 각각 몇을 깨우나, 순서 (깨우기 → 0 으로) 줄.
6. **syscall 출구까지**: `NtWaitForSingleObject` (rva `0x871270`) → `ObWaitForSingleObject` → `KeWaitForSingleObject` 가 돌려준 값이 syscall 결과 (`RAX`) 로 가는 길. `KiSystemServiceStart` 의 출구 쪽 (PDB 이름 `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0` — `pdb_aliases.py` 확인) 에서 유저 `RAX` 를 무엇으로 채우나 (트랩 프레임 `Rax` 를 다시 읽나, 레지스터 그대로인가) — 줄. (Q141e 의 "새 `Rip` 로 갈 때 `RAX`" 에 쓰인다.)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^KeWaitForMutexObject$` (= PDB `KeWaitForSingleObject`, rva `0x217eb0`, 4086B) | 1 | 질문 1 — 신호 검사·블록 달기·깨난 뒤 반복만 |
| ntoskrnl | `^KiCommitThreadWait$` (rva `0x2177e0`, 1413B) | 1 | 질문 1 |
| ntoskrnl | `^KeSetEvent$` (rva `0x25b3e0`, 1920B) | 1 | 질문 2 |
| ntoskrnl | `^KiTryUnwaitThread$` (rva `0x3a87d0`, 845B) | 1 | 질문 3 |
| ntoskrnl | `^KeReleaseMutant$` (rva `0x263540`, 52B) | 2 | 질문 4 — 몸까지 |
| ntoskrnl | `^KePulseEvent$` (rva `0x3a9600`, 497B) | 1 | 질문 5 |
| ntoskrnl | `^(KiWaitSatisfyMutant|KiWaitSatisfyOther|KiProcessThreadWaitList)$` | 0 | 질문 1(a)·2(c) |
| ntoskrnl | `^(NtWaitForSingleObject|ObWaitForSingleObject)$` | 0 | 질문 6 — 돌려준 값이 지나가는 줄만 |
| ntoskrnl | `^KiSystemServiceStart$` | 0 | 질문 6 — 이미 있음. `+0x1a0` 출구 쪽만 |

- 함수가 많다. **질문에 답하는 줄만** 읽고, 본문 전체를 풀지 않는다. 락·통계·ETW·우선순위 올려 주기 (`KiBoost*`) 는 이름만 (올려 주는 양은 한 줄로만).
- 지도: `run_ghidra.sh map ntoskrnl.exe KeSetEvent` (와 필요하면 `KeWaitForMutexObject`).
- `ObWaitForSingleObject` 가 **스레드 객체**를 받는 길 자체는 Q141d (끝내기) 에서 본다 — 여기선 돌려준 값만.

## 이미 아는 것

- `[정적]` sast/71 (run75): `KiCommitThreadWait +0x220` → `KiSwapThread`. `KiSwapThread` 는 `_KTHREAD.WaitStatus` (`+0xc8`) 를 돌려준다 (`+0x25c`·`+0x29a`), 셋째 인자가 있으면 `AbWaitObject` (`+0x3d0`) 를 적어 준다. 준비 줄 넣기 = `KiDeferredReadySingleThread`.
- `[정적]` run52·run53 표: `AbWaitObject` 쓰는 곳 = `KiCommitThreadWait`, `KeWaitForMutexObject`, `KiWaitForAllObjects`, `KeInsertQueueEx` · `Preempted`·`AdjustReason` 쓰는 곳 = `KiDeferredReadySingleThread`, `KiDirectSwitchThread`, `KiCommitThreadWait`, `KiProcessThreadWaitList`.
- `[정적]` `pdb_aliases.py`: `KeWaitForSingleObject` = Ghidra `KeWaitForMutexObject`, `KiSystemServiceExit` = `KiSystemServiceStart +0x1a0` (함수로 안 잡힘).
- `[관찰]` run75 (run44 사진): 메인 스레드 P1 → P1a 사이 넘김 5번이 `KiSwapThread` 식과 안 맞음 `[추측]` — 다른 길 단서.
- `[추측]` (PE_emulator) 깨움 = 표시만, 깨난 쪽이 다시 물음.

## 받고 싶은 것

- 결과서 `_workspace/sast/73_thread-wake_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"** — 깨우는 쪽이 할 일 (이벤트 종류별·뮤턴트), 깨난 쪽 결과값, `PulseEvent`.
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다 (이번 묶음엔 VM 안 함).
