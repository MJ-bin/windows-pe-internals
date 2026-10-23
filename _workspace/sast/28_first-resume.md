# 28 first-resume — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run33)
- 대기열: Q40 (`_workspace/plan.md`) — 2.2·2.3 작은 정적 확인 둘: `EPROCESS.Flags3.ProcessFirstResume` (`+0x5fc` bit15, `PsMultiResumeThread` 가 켬) 를 **읽는 곳**, 조각 `FUN_1406b2db3` 의 정체 (`KiApcInterrupt` 추정). 단계 3 "작은 정적부터 몰아서" (사용자 결정 2026-10-23).
- 관련: flow.md 2.2 `nt!PsMultiResumeThread` 절 `[공백]` "`ProcessFirstResume` 를 읽는 곳은 안 봤다", 2.2 "멈춘 첫 스레드가 서는 곳" 6번 `[추측]` "`FUN_1406b2db3` … `KiApcInterrupt` 의 떨어진 조각", 그림 2 `nt` 상자 `h0` "nt!KiApcInterrupt 추정 (장부: 조각 FUN_1406b2db3)", 4장 2.2 두 줄, 4.1 그림 2 nt 줄 / 이전 요청 sast/08 (run13)

## 범위

- 작은 확인 둘. **읽는 함수 본문은 그 칸을 읽는 줄 앞뒤만** (무엇을 위해 읽는지 한두 줄). 깊이 내려가지 않는다.
- 오프셋은 call·ret 둘 다, 점프 목적지는 `절대 주소 − 함수 시작`.

## 알고 싶은 것

1. **`ProcessFirstResume` 를 읽는 곳**
   - ntoskrnl 전체에서 `_EPROCESS +0x5fc` (`Flags3`) 의 **bit 15** (`0x8000`) 를 **읽는** 명령 (예: `TEST dword [r+0x5fc],0x8000`, `BT dword [r+0x5fc],0xf`, `MOV eax,[r+0x5fc]` 뒤 `0x8000` 마스크 등) 을 찾아 함수 이름+오프셋으로. 쓰는 곳(`PsMultiResumeThread +0x4e` `OR.LOCK`)은 이미 안다 — 다른 쓰는 곳도 있으면 같이.
   - 찾는 법은 decompiler 판단 (Ghidra 스크립트로 피연산자 훑기, 원본 바이트 패턴 등). 범위·한계(놓칠 수 있는 꼴 — 레지스터에 0x5fc 를 더한 뒤 읽기 등)를 결과서에 적기.
   - 읽는 곳마다: 그 비트가 켜졌을 때/꺼졌을 때 무엇이 달라지나 (한두 줄). test.exe 시동 길(부모의 `NtResumeThread` 뒤)에서 그 함수가 불릴 것인지 정적으로 알 수 있으면.
   - 하나도 없으면 "커널 안에선 안 읽음 (쓰기만)" 이라고, 그리고 다른 곳(ETW, `NtQueryInformationProcess` 로 유저에 내보내는 길 등)으로 나가는지.
2. **`FUN_1406b2db3` 의 정체**
   - 이 주소를 감싸는 `.pdata` (RUNTIME_FUNCTION) 범위와 그 범위의 함수 이름 (PDB). `KiApcInterrupt` (rva `0x6b2b40`, 348B → 끝 `0x6b2c9c`) 의 떨어진 조각인가, 다른 함수(예: 다른 인터럽트 처리기, `KiApcInterrupt` 의 다른 이름)인가.
   - unwind 정보에 `UNW_FLAG_CHAININFO` 로 부모 함수가 이어져 있으면 그 부모.
   - `pdb_aliases.py --lookup ntoskrnl.exe 0x6b2db3` (또는 범위 시작 주소) 로 PDB 이름.
   - 이 조각에서 `KiDeliverApc` 를 부르는 줄 (call·ret) — flow.md 2.2 6번과 그림 2 `h0` 상자 이름을 무엇으로 쓰면 되나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | (전체 훑기) `_EPROCESS +0x5fc` bit 15 읽기 | - | 질문 1 |
| ntoskrnl | `^KiApcInterrupt$` | 0 | 질문 2 |
| ntoskrnl | rva `0x6b2db3` 를 감싸는 함수/조각 | 0 | 질문 2 |
| ntoskrnl | `^PsMultiResumeThread$` | 0 | 질문 1 (쓰는 줄 대조, 이미 있으면 다시 안 뽑아도 됨) |

## 이미 아는 것

- `[정적]` (run13) `PsMultiResumeThread` (`NtResumeThread +0x9b`) 가 `KeResumeThread` 결과로 "이번에 완전히 풀림" 이면 `_EPROCESS +0x5fc` bit 15 `Flags3.ProcessFirstResume` 를 켠다 (`+0x4e` `OR.LOCK`, 이미 켜져 있으면 건너뜀). bit 20 `EnableThreadSuspendResumeLogging` 이면 `EtwTiLogSuspendResumeThread` (`+0x6f`).
- `[정적]` (run13) `NtAlertResumeThread` 길은 `PsMultiResumeThread` 를 안 거쳐 이 표시를 안 한다. (run14) 부모 KERNELBASE 는 `NtResumeThread` 를 쓴다.
- `[정적]` (run13) `KiDeliverApc.c` 머리 `callers:` 에 `FUN_1406b2db3` 가 있다. `[추측]` `KiApcInterrupt` 의 떨어진 조각.
- 비트 이름은 PDB `_EPROCESS.Flags3` 비트필드에서 (sast/08).

## 받고 싶은 것

- 질문별 답 + 근거 (함수+오프셋, call·ret). 태그 `[정적]` / `[추측]`.
- flow.md 2.2 `[공백]` 한 줄, `[추측]` 한 줄, 그림 2 `h0` 상자 글, 4장 2.2 두 줄 판정.
- 동적으로 확인할 것 + 멈춤점 (`nt+rva`, 짧게). 이미 대기 중인 Q19 (첫 스레드가 풀리는 시점) 와 겹치면 그 번호를.
- 스크립트 문제 (전체 훑기 도구가 없으면 그 불편) 는 "스크립트 문제" 절에.
