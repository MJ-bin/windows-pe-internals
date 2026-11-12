# 76 thread-setcontext — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141e** (PE_emulator 요청 25 ㅁ — 기다리는 **남의** 스레드에 문맥 넣기). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 정적만.
- 관련: run16 (`PspSetContextThreadInternal +0xf7` 다른 스레드면 APC 걸고 기다림, `+0x131` 자기면 `PspGetSetContextSpecialApc` 직접, `PspGetSetContextSpecialApc +0x64 → PspGetSetContextInternal`), sast/73 (Q141c1 — `KeWaitForSingleObject` (= `KeWaitForMutexObject`) `+0x860 CMP R14D,0x100` → `+0x891 JMP` 다시 기다림; syscall 출구 `KiSystemServiceStart +0x25d` 반환값 → 트랩 프레임 `Rax` 칸 `[RBP-0x50]`, `+0x540` 그 칸 읽어 `SYSRET`; 새 문맥 길 `+0x2d3 KiRestoreSetContextState` → `IRETQ`), sast/74 (타이머 — 마감 기다림)
- 결과서 `_workspace/sast/73_thread-wake_result.md`·`74_thread-timer_result.md` 를 먼저 읽고 겹치면 가리키기만.
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- 상황: 스레드 A 가 `NtSetContextThread(B, ctx)` 를 부른다. B 는 지금 **syscall 안에서 기다리는 중** (예: `NtWaitForSingleObject`).
- PE_emulator 의 지금 (`[추측]`): "기다림은 이어짐 · 기다림 결과는 버림" — B 의 `Rip` 등을 바꿔 두고, B 가 나중에 깨면 결과값 없이 새 `Rip` 로 감. 그리고 B 가 **신호로** 깨면 우리는 그 신호를 안 먹음 (진짜는 먹을 것 `[추측]`).
- test.exe 는 이 일을 안 한다 [소스] `sample/test.c` (`SetThreadContext` 없음) → VM 으로도 못 보고 **정적이 유일한 길**이다.

## 알고 싶은 것

1. **A 쪽 — `PspSetContextThreadInternal`** (이미 있음, 834B): 다른 스레드일 때 APC 를 거는 줄 (`KeInitializeApc`·`KeInsertQueueApc` 오프셋, APC 종류 — 특별 커널 APC 인가, 우선순위 올려 주기), 그리고 A 가 끝날 때까지 기다리는 줄 (무엇을 기다리나 — 이벤트?). B 가 끝나지 않으면 A 는 계속 기다리나.
2. **APC 가 B 의 기다림을 깨는가**: `KeInsertQueueApc` → `KiInsertQueueApc` → `KiSignalThreadForApc` (이미 있음) 에서, 대상 B 가 기다리는 중 (`State == 5`) 이고 특별 커널 APC 면:
   - (a) B 의 기다림을 끝내는 줄 — `KiTryUnwaitThread` (sast/73) 를 `0x100` (`STATUS_KERNEL_APC`) 으로 부르나? 조건 (`WaitIrql`·`KernelApcDisable`·`SpecialApcDisable`).
   - (b) B 가 이미 다른 이유로 깨어나고 있으면 (경쟁) — 지나치는 줄.
3. **B 쪽 — 깨난 뒤**:
   - (a) `KeWaitForSingleObject +0x860` 에서 `0x100` 이면 다시 기다리기 전에 APC 를 배달하나 (`KiDeliverApc` 를 부르는 곳 — IRQL 내릴 때 소프트웨어 인터럽트 `KiApcInterrupt`? 직접?) — 길.
   - (b) APC 안 (`PspGetSetContextSpecialApc` → `PspGetSetContextInternal` → `KeContextToKframes`) 이 B 의 **트랩 프레임** 을 바꾸는 칸: `Rip`·`Rsp`·**`Rax`** 를 바꾸나 (문맥 플래그 `CONTEXT_INTEGER` 일 때만 `Rax`?).
   - (c) APC 가 끝나면 B 는 **다시 기다림으로 돌아가나** (`+0x891 JMP` — 같은 객체·같은 마감으로? 마감이 상대 시간이면 남은 시간으로 다시 계산하나 — sast/74 `+0x3b6 ~ +0x3d7` 과 이어서).
4. **B 가 결국 깨어날 때** (신호 또는 마감):
   - (a) 신호로 깨면 신호를 **먹나** — 자동 이벤트를 0 으로 내리고 뮤턴트를 가져가나 (sast/73 의 깨우는 쪽 규칙이 그대로 적용되나). 즉 "문맥을 바꿨다" 는 사실이 기다림을 채우는 데 영향이 있나 (예/아니오, 줄).
   - (b) syscall 출구: 서비스 반환값 (`WaitStatus`) 이 트랩 프레임 `Rax` 칸에 쓰이는 `KiSystemServiceStart +0x25d` 와, 문맥을 넣은 표시가 있을 때 가는 `+0x2d3 KiRestoreSetContextState` → `IRETQ` 길 — **어느 쪽이 나중에 쓰나**: 유저로 돌아갔을 때 `RAX` = 기다림 결과인가, 넣은 문맥의 `Rax` 인가? 갈리는 조건 (`CONTEXT_INTEGER` 넣었나, 트랩 프레임 표시 비트) 과 줄.
   - (c) 새 `Rip` 로 갈 때 `Rcx`·`R11` (`SYSRET` 이 쓰는 레지스터) 은 어떻게 되나 — `IRETQ` 길이면 트랩 프레임 전체 복원인가.
5. **A 쪽 결과**: A 의 `NtSetContextThread` 는 무엇을 돌려주나 (B 가 APC 를 돌린 뒤 성공), B 가 끝나는 중이면 실패하나 — 상태값.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^PspSetContextThreadInternal$` | 1 | 질문 1·5 — 이미 있음 |
| ntoskrnl | `^KeInsertQueueApc$` (rva `0x263e70`, 1401B) | 1 | 질문 2 — APC 넣기·대상 깨우기 줄만 |
| ntoskrnl | `^(KiInsertQueueApc|KiSignalThreadForApc)$` | 0 | 질문 2 — 이미 있음 |
| ntoskrnl | `^KiDeliverApc$` | 0 | 질문 3(a) — 이미 있음, 특별 커널 APC 부르는 줄만 |
| ntoskrnl | `^(PspGetSetContextSpecialApc|PspGetSetContextInternal|KeContextToKframes)$` | 0 | 질문 3(b) — 이미 있음 |
| ntoskrnl | `^KiRestoreSetContextState$` | 0 | 질문 4(b)(c) — 이미 있음 |
| ntoskrnl | `^KiSystemServiceStart$` | 0 | 질문 4(b) — `+0x1a0 ~` 출구만, 조각 포함 |

- 새로 뽑을 것은 `KeInsertQueueApc` 정도. 나머지는 이미 `.asm` 이 있다 (오케스트레이터 확인).
- `KeContextToKframes` 가 `KxContextToKframes` 로 이어지면 (run16 목록에 있음) 그것까지.
- 줄기 밖 (CET·XSTATE·WoW64·디버그 레지스터) 은 이름만.

## 이미 아는 것

- `[정적]` run16: `PspSetContextThreadInternal +0xf7 CMP R13,R14` — 현재 스레드면 `+0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` 직접, 다른 스레드면 APC 를 걸고 기다림. `PspGetSetContextSpecialApc +0x64` (ret `+0x69`) → `PspGetSetContextInternal`.
- `[정적]` sast/73: 깨난 뒤 `WaitStatus` 를 그대로 돌려줌, 반복은 `0x100` 일 때뿐 (`KeWaitForMutexObject +0x860 CMP R14D,0x100` → `+0x891 JMP +0x104`). syscall 출구: `+0x25d MOV [RBP-0x50],RAX` (트랩 프레임 `Rax`), 조각 `FUN_1406c20cc` 의 `+0x540 MOV RAX,[RBP-0x50]` → `+0x5bb SYSRET`; 새 문맥 길 `+0x2d3 KiRestoreSetContextState` (`IRETQ`).
- `[정적]` sast/74: 상대 마감은 `KeWaitForSingleObject +0x3b6 ~ +0x3d7` 에서 한 번 계산.
- `[소스]` test.exe 는 `SetThreadContext` 를 안 부름.

## 받고 싶은 것

- 결과서 `_workspace/sast/76_thread-setcontext_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **A·B 시간 순서 표** (A 가 APC 넣기 → B 깨기 → B APC 배달 → 문맥 바꾸기 → A 깨우기 → B 다시 기다림 → … → B 유저로).
- 질문마다 "지금 우리 (PE_emulator: 기다림 이어짐 · 결과 버림 · 신호 안 먹음) 맞나·틀리나" 한 줄.
- 끝에 **"PE_emulator 가 흉내 낼 최소 규칙"**.
- 정적으로 못 닫는 것은 그렇다고 적는다 (test.exe 로는 VM 으로도 못 봄 — 새 샘플이 있어야 함).
