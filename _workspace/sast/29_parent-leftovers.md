# 29 parent-leftovers — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run34)
- 대기열: Q41 (`_workspace/plan.md`) — 2.2 부모 유저 쪽 남은 정적 확인 (작음) 넷. 단계 3 "작은 정적부터 몰아서" (사용자 결정 2026-10-23).
  - 함께: Q65 의 "`PspAllocateProcess` 인자 `[RSP+0x90]` bit 9" 가 이번 질문 4 의 ProcessFlags bit9 와 같은 비트인지 (사용자에게 알리고 같이 보기로 함). 같으면 Q65 의 그 부분이 풀린다.
- 관련: flow.md 그림 1 `kb1` "KERNELBASE!CreateProcessW (정적: 부모가 이 입구로 오는지는 추측)", 2.2 부모 쪽 절(run14), 4장 2.2 줄들 / 이전 요청 sast/09 (run14 — 4장 6번·결과서 134줄), sast/24 (run29 — ntdll `NtContinue` 스텁 SYSCALL 번호와 `KiServiceTable` 대조 방법), sast/28 (run33 — `PspAllocateProcess +0x703~+0x710` OwnerProcessId `|2`, `[RSP+0x90]` bit 9)

## 범위

- 작은 확인 넷. 각각 줄 몇 개로 답이 나는 수준. 깊이 내려가지 않는다.
- 모듈: kernel32 (`CreateProcessW` 94B), ntdll (스텁 둘, 각 24B), ntoskrnl (`NtCreateUserProcess` 에서 ProcessFlags 가 흘러가는 줄, `PspAllocateProcess +0x6f1 ~ +0x710`), **basesrv.dll** (csrss 쪽 — 처음 분석. `ms/bin/.../System32/basesrv.dll` 에 있음, PDB 는 `fetch_pdb.py` 로).
- 오프셋은 call·ret 둘 다, 점프 목적지는 `절대 주소 − 함수 시작`.

## 알고 싶은 것

1. **`kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW`**
   - kernel32 `CreateProcessW` (rva `0x3bdd0`, 94B) 가 KERNELBASE `CreateProcessW` 를 부르나 (IAT 칸 이름, call·ret). 인자를 그대로 넘기나, 바꾸나. 다른 일(검사·ETW 등)을 하나.
   - 그러면 flow.md 그림 1 `kb1` 앞에 `kernel32!CreateProcessW` 를 둘 수 있나. "부모(cmd.exe)가 이 입구로 오나" 는 cmd.exe 쪽이라 정적 범위 밖 — cmd.exe import 표에 `CreateProcessW` 가 kernel32 에서 오는지만 보면 됨 (cmd.exe 가 `ms/bin` 에 없으면 생략하고 "동적 Q19 쪽 k" 로).
2. **ApiNumber `0x1001d` 의 정체** (run14: `CreateProcessInternalW +0x4655` 에서 싣고 `+0x4672` `CsrClientCallServer`, 길이 `0x218`)
   - CSR ApiNumber 의 짜임 (위 16비트 = 서버 DLL 번호, 아래 = API 번호 `[추측]`) 을 csrss 쪽 코드로 확인. 서버 번호 1 이 `basesrv.dll` 인가.
   - `basesrv.dll` 의 API 처리 표 (`BaseServerApiDispatchTable` 류) 의 `0x1d` 번째 칸 함수 이름 (PDB). 그 함수가 하는 일 한두 줄 (대표만 — 새 프로세스를 csrss 목록에 등록? SxS 정보 저장?).
   - 표 이름·짜임을 찾는 데 `csrsrv.dll` (`CsrApiRequestThread` 등) 이 필요하면 줄만 보기.
3. **ntdll 스텁 둘**: `NtCreateUserProcess` (rva `0x1627c0`, 24B), `NtResumeThread` (rva `0x1617f0`, 24B) 가 바로 `SYSCALL` 하나 (`MOV EAX, imm` 번호). 그 번호가 ntoskrnl `KiServiceTable` 의 `nt!NtCreateUserProcess`·`nt!NtResumeThread` 칸인가 (sast/24 와 같은 방법).
4. **ProcessFlags bit9 (`0x200`)**: run14 — KERNELBASE `+0x800 BTS [RSP+0x110],9` (`CREATE_SUSPENDED` 가 **없을 때** 켬), `NtCreateUserProcess` 의 ProcessFlags 인자로 감.
   - 커널 `NtCreateUserProcess` 안에서 이 인자가 어디로 흘러가나 (검사 마스크, 저장 칸) — 그 흐름이 `PspAllocateProcess` 의 인자 `[RSP+0x90]` 로 가나.
   - 가면: sast/28 이 본 `PspAllocateProcess +0x703 ~ +0x70c` 의 "bit 9 면 OwnerProcessId 에 `|2` 를 안 붙임" 이 **이 비트**인가 (예/아니오, 줄). 그러면 `CREATE_SUSPENDED` 없이 만든 test.exe 는 `OwnerProcessId` = 부모 PID 그대로 (`|2` 없음) 가 되나 — `PspProcessClose` (run33) 조건과의 관계를 한두 줄로.
   - 이 비트를 읽는 다른 곳이 커널에 있으면 대표만.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernel32 | `^CreateProcessW$` | 1 | 질문 1 |
| ntdll | `^(NtCreateUserProcess|NtResumeThread)$` | 0 | 질문 3 |
| ntoskrnl | `^NtCreateUserProcess$` (이미 있음 — ProcessFlags 인자 흐름 줄만) | 0 | 질문 4 |
| ntoskrnl | `^PspAllocateProcess$` (이미 있음 — `+0x6f1 ~ +0x710` 과 인자 받는 줄만) | 0 | 질문 4 |
| basesrv.dll | API 처리 표, `0x1d` 칸 함수 | 1 | 질문 2 (처음 analyze) |
| csrsrv.dll | (필요하면) ApiNumber 나누는 줄 | 0 | 질문 2 |

## 이미 아는 것

- `[정적]` (run14) `KERNELBASE!CreateProcessW` (107B) 는 `CreateProcessInternalW` 를 `+0x61` (ret `+0x66`) 에서 부르는 얇은 껍데기.
- `[정적]` (run14) `CreateProcessInternalW`: `+0x7f0 ~ +0x800` 사본 `& 4 == 0` 이면 ProcessFlags `|= 0x200`. `+0x30e7` `NtCreateUserProcess` (ProcessFlags = `[RSP+0x30]` ← `[RSP+0x110]`, ThreadFlags = 1). `+0x4672` `CsrClientCallServer` (ApiNumber `0x1001d` `+0x4655`, 길이 `0x218` `+0x464f`) — `CREATE_SECURE_PROCESS` 가 아니면 모든 길이 지남. `CsrClientCallServer` 는 ntdll 것 (import 표).
- `[정적]` (run29) ntdll `NtContinue` 스텁 `MOV EAX,0x43` → `KiServiceTable[0x43]` = `nt!NtContinue` (489 항목 대조 방법).
- `[정적]` (run33) `PspAllocateProcess +0x710` 이 `OwnerProcessId`(+0x2d8) 를 쓴다: 부모 PID `|2`, 인자 `[RSP+0x90]` bit 9 면 안 붙임 (`+0x703 ~ +0x70c`). `PspProcessClose +0xf`: `ProcessFirstResume` 꺼져 있고 "닫는 쪽 PID == OwnerProcessId" 이고 ProcessDelete/SelfDelete 면 `PsTerminateProcess(0xc000010a)`.
- `[소스]` sample 은 `CreateProcess` 를 안 쓴다 — 부모는 cmd.exe.

## 받고 싶은 것

- 질문별 답 + 근거 (함수+오프셋, call·ret). 태그 `[정적]` / `[추측]`.
- flow.md 그림 1 `kb1` 글과 앞 상자(kernel32) 를 넣을지, 2.2 부모 쪽 절·4장 2.2 줄 판정.
- 질문 4 가 Q65 의 bit 9 부분을 풀었는지 (예/아니오) — 풀었으면 Q65 에 남는 것 (`NtSetInformationProcess +0x1ecd`, `ObCloseHandleTableEntry +0x7a9` 인자).
- 동적으로 확인할 것 + 멈춤점 (짧게). 이미 대기 중인 Q19 (부모 `NtCreateUserProcess` 입구, `ThreadFlags`, csrss 순서) 와 겹치면 그 번호를.
- 스크립트 문제 (basesrv.dll 처음 analyze 때 불편) 는 "스크립트 문제" 절에.
