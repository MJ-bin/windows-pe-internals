# 89 wait-packet — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q175** (PE_emulator 요청 33 — 기다림 완료 패킷 (wait completion packet) 이 "아직 안 울린 대상" 에 맡겨졌을 때 커널이 하는 일). 사용자 결정 2026-11-15: **지금 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움), 끝나면 다시 Q104 앞에서 멈춤. 정적만 — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 원문: `../PE_emulator/_workspace/05_tuning_planner/256_internals-wait-packet.md` (읽어도 됨). 배경 표: `../PE_emulator/_workspace/05_tuning_planner/256_tune04.md` "T8" 절 (T7 후보 하나).
- 빌드: 26100.9457. **ntoskrnl 은 analyze 됨** (`ms/decomp/ntoskrnl/_index.tsv`). 새 analyze 없이 대상 함수만 뽑는다.
- 관련: run62 (tp-etw-init) · run69 (loader-pool) — 유저 쪽 `TpSetWaitEx` 가 `NtAssociateWaitCompletionPacket` 을 부르는 자리 (test.exe 초기화에서 맡김 6 번). 이번은 그 **커널 쪽 본문**.

## 배경

- PE_emulator 병목 38 개 (Go 런타임 IOCP 31 · `NtSetTimer2` 뒤 끝 7). 받은 샘플 `NtSetTimer2` 820 (악성 224) · `NtAssociateWaitCompletionPacket` 47 (악성 25) · `NtCancelWaitCompletionPacket` 58 (악성 55).
- 막힌 자리 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`:
  - `r04 03d0b177ff78 seq 2253 → 2256 → 2260 → 2267`: 타이머 걸기 (`NtSetTimer2`) → 패킷 맡기기 (`NtAssociateWaitCompletionPacket`) 에 에뮬레이터 기본 대답 `wait_completion_async_unsupported` → `NtRemoveIoCompletionEx` 기본 대답 `wait_would_block` → Go 가 글 찍고 끝.
  - `r04 009d7da84784 seq 37167 · 37191`: `WinHttpOpen` 안 — `NtSetEvent`·`NtCancelWaitCompletionPacket` 기본 대답. `WinHttpOpen` 은 57/57 이 0 을 돌려줌.
- 팀원 syscall 몸의 빈칸 (`../PE_emulator` 쪽 `ext/syscall/src/threadpool/nt_wait_completion_packet.cpp` 머리 주석): "안 울린 대상 연결에 돌려주는 상태 값·AlreadySignaled 를 쓰는지 — 커널 본문 asm 없음", "배달 (대상이 울리면 포트에 넣기) 은 공사 중". 팀원 몸은 지금 **이미 울린 대상** 이면 그 자리에서 IOCP 줄에 넣고, 취소는 **줄에 든 것만** 다루며 `0xc0000120` 을 돌려준다.

## 알고 싶은 것

1. **`NtAssociateWaitCompletionPacket` 본문 — 대상이 아직 안 울렸을 때**
   - 돌려주는 status (0 인가, `STATUS_PENDING` 인가, 다른 값인가). 오프셋 근거.
   - `AlreadySignaled` (마지막 인자, BOOLEAN 포인터) 칸에 쓰는 값 — 안 울렸을 때 0 을 쓰나 / 안 쓰나. 이미 울렸을 때는 1 을 쓰고 곧바로 줄에 넣나 (팀원 몸이 하는 꼴이 맞나 한 줄).
   - 무엇을 걸어 두나 — 대상 객체에 기다림 블록 (`KWAIT_BLOCK`) 을 거나, 그 꼴 (WaitType·BlockState·Thread 칸 대신 무엇이 들어가나 — 칸 이름만). 패킷 객체 안 어느 칸에 무엇을 저장하나 (이름 있으면 이름).
   - 대상이 나중에 울릴 때 IOCP 줄에 들어가는 네 값 (KeyContext · ApcContext · IoStatus · IoStatusInformation) 이 **인자 넷째 ~ 일곱째 그대로** 인가 (가공하나).
   - 이미 다른 대상에 맡겨진 패킷을 또 맡기면 status (작게, 갈림 자리만).
2. **배달 길 — 대상이 울릴 때 걸린 패킷을 IOCP 줄에 넣는 함수 이름·순서**
   - 이벤트 (`KeSetEvent` — 지도 `ms/disassemble/ntoskrnl/_map/KeSetEvent_*` 에 직접 callee `KiInsertQueueInternal`·`KiTryUnwaitThread` 있음), 타이머 만기 (`KiTimerExpiration`·`KiTimerWaitTest` — `.asm` 이미 있음), 세마포어 놓기 (`KeReleaseSemaphore` 류) 에서 패킷 기다림 블록을 만나면 어디로 가나. 함수+오프셋 사슬 (ret 포함).
   - 넣을 때 **대상의 신호를 쓰나** — 자동 리셋 이벤트가 꺼지나, 동기 타이머가 꺼지나, 세마포어 count 가 줄어드나 (일반 스레드 기다림과 같이 "만족" 처리 — `KiWaitSatisfy*` 를 거치나).
   - 패킷은 **한 번 쓰면 다시 맡겨야 하나** (넣은 뒤 기다림 블록을 떼나).
   - IOCP 줄에 넣는 함수가 `KiInsertQueue`·`KiInsertQueueInternal`·`KeInsertQueue` 중 어느 것인가, 패킷 객체를 줄 항목으로 그대로 쓰나.
3. **`NtCancelWaitCompletionPacket` (351 B) · `IopCancelWaitCompletionPacket`**
   - 아직 안 울린 (줄에 안 들어간) 패킷을 취소할 때 status 와 한 일 (기다림 블록 떼기).
   - 이미 줄에 들어간 패킷일 때 `RemoveSignaledPacket` 인자 (TRUE/FALSE) 에 따른 갈래 — 각 status.
   - 맡겨지지 않은 패킷이면 status.
   - 팀원 몸 "줄에 든 것만 다루고 `0xc0000120` (`STATUS_CANCELLED`)" — 어느 갈래와 맞나 / 틀리나 한 줄.
4. (작게) **`IoRemoveIoCompletion` (1889 B) 에서 줄이 비었을 때** — 기다리는 길 (`KeRemoveQueueEx` 류) 과 시간 0 이면 돌려주는 status (`STATUS_TIMEOUT` 0x102 인가) 만. 패킷 항목을 꺼낼 때 다른 항목과 다르게 다루는 갈래가 있나 (갈림 자리만).
5. **최소 조건 표**: "에뮬레이터가 맡김·배달·취소를 진짜와 같게 하려면" — 상황 (이미 울림 / 안 울림 → 나중에 울림 / 취소: 안 울림·줄에 듦×RemoveSignaledPacket 두 값·안 맡김) · status · `AlreadySignaled` · 대상 신호 변화 · 줄에 들어가는 네 값.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtAssociateWaitCompletionPacket$` (0x434dd0, 652 B) + `$filt$0` | 1 | 질문 1 |
| ntoskrnl | `^NtCancelWaitCompletionPacket$` (0x434c60, 351 B) · `^IopCancelWaitCompletionPacket$` (0x435174) | 1 | 질문 3 |
| ntoskrnl | `^ObGetAssociatedWaitObject$` (0x435240) · `^IopCloseWaitCompletionPacket$` · `^IopFreeWaitCompletionPacket$` | 0 ~ 1 | 질문 1·3 — 부르면 |
| ntoskrnl | NtAssociate 가 부르는 "기다림 블록 걸기" 함수 (이름은 callee 로 확인) | 1 | 질문 1 |
| ntoskrnl | `KeSetEvent` · `KiTryUnwaitThread` · `KiSatisfyThreadWait` · `KiInsertQueueInternal` · `KiInsertQueue` | 줄기만 | 질문 2 — 패킷 기다림 블록 갈래만 |
| ntoskrnl | `KiTimerWaitTest` · `KeReleaseSemaphore` (이름 확인) | 줄기만 | 질문 2 — 타이머·세마포어가 같은 길로 가는지만 |
| ntoskrnl | `^IoRemoveIoCompletion$` (0x214d80) | 0 ~ 1 | 질문 4 — 갈림 자리만 |

- 범위: 맡김 · 배달 · 취소 세 갈래. `NtCreateWaitCompletionPacket`·객체 형식 등록은 넣지 않는다 (필요하면 패킷 객체 칸 이름만).
- 배달 길은 **패킷 기다림 블록 갈래만** 따라간다. 일반 스레드 깨우기·디스패처 락·IRQL 은 공용 부품이라 이름만.
- 함수가 많으면 질문 1·3 을 먼저 (작은 함수 둘), 그다음 2, 4 는 갈림 자리만.
- 이름은 `_index.tsv` 로 확인, 없으면 `pdb_aliases.py --where ntoskrnl.exe <이름>`.

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/`)

- `NtAssociateWaitCompletionPacket`·`NtCancelWaitCompletionPacket` 지도 없음. 필요하면 `run_ghidra.sh map` 으로 만들어도 된다.
- `KeSetEvent` 지도 (기준 20): 깊이 1 callee 8 — `KiInsertQueueInternal` (caller 33, helper) · `KiTryUnwaitThread` (caller 32, helper) · `KiExitDispatcher` · `KiEnterDeferredReadyState` (expanded) 외. **`KiInsertQueueInternal` 이 KeSetEvent 에서 바로 불림** — 배달 길 후보 `[추측]`.
- 이미 뽑힌 `.asm`: `KeSetEvent`, `KiTryUnwaitThread`, `KiTimerExpiration`, `KiTimerExpirationDpc`, `KiTimerWaitTest`, `KiInsertQueueApc`, `KiSignalThreadForApc`.

## 이미 아는 것

- `[정적]` 유저 쪽: `TpSetWaitEx` 는 연결 대답의 부호만 봄 (`ms/disassemble/ntdll/TpSetWaitEx.asm +0x16d`), 이미 울림 바이트를 읽음 (`+0x2e5`). test.exe 초기화에서 맡김 6 번 (run62 · run69).
- `[추측]` (팀원 카드, internals 근거 없음) 이미 울린 대상이면 그 자리에서 IOCP 줄에 넣음.
- 커널 함수 주소만 있음 (`_index.tsv`): `NtAssociateWaitCompletionPacket` 0x434dd0 (652 B) · `NtCancelWaitCompletionPacket` 0x434c60 (351 B) · `IopCancelWaitCompletionPacket` 0x435174 (197 B) · `IopFreeWaitCompletionPacket` 0x3a9160 · `IopCloseWaitCompletionPacket` 0x435080 · `ObGetAssociatedWaitObject` 0x435240 · `IoRemoveIoCompletion` 0x214d80 (1889 B) · 후보 `KiSatisfyThreadWait` 0x38ba80 · `KiInsertQueue` 0x308a60 · `KeInsertQueueEx` 0x32d950.

## 받고 싶은 것

- 결과서 `_workspace/sast/89_wait-packet_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- 질문 2 는 사슬 표 (함수+오프셋 · 하는 일 · 갈림).
- 질문 5 최소 조건 표.
- 질문마다 **"지금 팀원 몸 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 = 이미 울림이면 바로 줄에 넣음, 안 울림은 `wait_completion_async_unsupported`, 취소는 줄에 든 것만 `0xc0000120`).
- 끝에 **"동적으로 확인할 것"** (실행 때만 정해지는 것 — `[추측]`) 과 **"다음 요청 후보"**.
