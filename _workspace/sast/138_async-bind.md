# 138 async-bind — 정적 분석 요청

- 날짜: 2026-11-25
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q267** (PE_emulator 요청 `../PE_emulator/_workspace/00_planner/AS02_internals-async-bind.md` — **원문을 먼저 읽을 것**. 에뮬레이터 로그 seq 2185 ~ 2201 과 동기 손님 seq 5617 ~ 5623 대조가 거기 있다). 사용자 결정 2026-11-25 (PE_emulator D154) 의 첫째. **정적만.** VM 은 이번에 안 함 (동적 확인거리는 결과서 끝에 적기만).
- **먼저 읽는 것** (이미 답한 칸은 다시 풀지 말고 가리키기만):
  - `runs/03_sprint/run129-winnsi-rpc.md` 4절·5절 (끝점 풀이 사슬 `DriveStateForward +0x8ca → … → AlpcConnect +0x293`, `+0x515 LRPC_CASSOCIATION::Connect`)
  - `runs/03_sprint/run133-lrpc-wire.md` 1-1·1-2 (동기/비동기 = `LRPC_BIND_CCALL::Bind` 첫 정수 인자 `+0x42`→`+0x1d7`; `DriveStateForward` 인자 전달은 `[공백]`)
  - `runs/03_sprint/run137-alpc-completion.md` 1-1·5절 (`Connect +0x3b1 EnableAsync → CreateAlpc → TpAllocAlpcCompletionEx → TppAllocAlpcCompletion +0x1d1 NtAlpcSetInformation(2)`, `TpAdjustBindingCount +0x3b NtSetInformationWorkerFactory(공장, 3, &1, 4)`) + `_workspace/sast/132_alpc-completion_result.md`
  - `runs/03_sprint/run125-worker-factory.md` (일꾼 공장 커널 몸 — `NtWaitForWorkViaWorkerFactory`, 쉼 시간 끝 `0x102`)
- 대상 빌드: Windows 11 24H2 26100.9457 x64. **모두 이미 analyze 됨** — 다시 analyze 하지 말고 `^이름$` export 만.

## 알고 싶은 것

1. **비동기 bind 를 미는 길** — rpcrt4 `DriveStateForward` 가 비동기 바인딩일 때 `+0x589 CALL EnableAsyncIfNecessary` 뒤 `+0x5b9`·`+0x5c4` 에서 상태 8/9 (`[R14+0x2d0]`·`+0x2d4`) 를 쓰고 `+0x5cf CMP [R13],0` · `+0x5d4 JZ` 에서 어디로 가나.
   - 같은 스레드에서 `LRPC_BIND_CCALL::Bind` (bind 보내기) 를 부르나? **예/아니오.**
   - 아니면 일을 맡기고 돌아가나? 맡긴다면 **무엇에 · 어떤 ntdll Tp 함수로** (`TpPostWork` · `TpSimpleTryPost` · `TppWorkPost` · `TpSetWait` 등 — 이름은 디컴파일에서) 맡기나.
   - `LrpcBHDriveStateForwardRoutine` · `DriveStateForwardAsyncCallback` 이 그 콜백인가 (어디서 주소를 꺼내 넘기나).
   - `[R13]` 은 무엇인가 (0 이면 되돌아가는 조건).
2. **그 일을 일꾼이 받는 길** — 1 에서 맡긴 일이 일꾼을 깨우는 syscall: `NtSetIoCompletion(Ex)` (풀 완료 포트에 패킷) · `NtReleaseWorkerFactoryWorker` · 없음 (유저 큐만) 중 무엇인가, 그 패킷 / 항목 꼴.
   - 에뮬레이터 로그엔 맡김 쪽 syscall 이 **0 개**였다. syscall 없이 맡기는 길이 진짜로 있나 (예: 이미 깨어 있는 일꾼 수 · 대기 수 칸을 보고 건너뜀 — 그 조건 줄).
   - 있다면 `TppWorkerThread` 가 `NtWaitForWorkViaWorkerFactory` 에서 돌아온 뒤 유저 큐를 보는 줄과, 쉼 시간 끝 (`0x102`) 뒤 일꾼이 끝나는 줄의 차례 (유저 큐에 일이 있으면 안 끝나나).
3. **비동기 보안 문맥** — 동기 손님은 bind 앞에서 `NtSetInformationThread(5)` · `NtAlpcCreateSecurityContext` 를 같은 스레드에서 부른다. 비동기 손님은 이것을 어디서 하나 (`OpenSecurityContextWorker` 의 동기/비동기 갈림 — asm 이 아직 없음, 이번에 export). 맡긴 일 안에서 하나. 실패하면 bind 를 안 보내나.
4. **`NtSetInformationWorkerFactory(공장, 3, &1, 4)` (run137 ①d)** — 에뮬레이터 로그엔 이 줄이 없었다.
   - ① 이 줄이 안 나오는 조건 (`TpAdjustBindingCount +0xa XADD` · `+0x16 ~ +0x1d` — 같은 풀에 앞서 묶인 것이 있으면 안 나오나).
   - ② 종류 3 이 커널 일꾼 공장에 주는 뜻 (칸 이름 · 묶음 수가 0 아니면 `NtWaitForWorkViaWorkerFactory` 가 쉼 시간 끝에 일꾼을 남기나) — ntoskrnl `NtSetInformationWorkerFactory` 종류 3 갈래 + `NtWaitForWorkViaWorkerFactory` 의 그 칸 읽는 줄. run125 에 이미 있으면 가리키기만.
5. (작게) **`NtAlpcQueryInformation(포트, 0, 버퍼, 0x10)`** 에 버퍼를 안 쓰고 0 을 돌려준 성공이 비동기 길을 바꾸나 — run137 ①b 는 "실패해도 됨 · 성공이면 `Flags & 0x1000000` 0". `IsPortWithSharedSection +0x25 · +0x35` 뒤에서 받기 버퍼 크기 말고 다른 갈림이 있나.

## 대상

| 모듈 | 함수 (rva, 크기) | depth | 이유 |
|---|---|---|---|
| rpcrt4 | `DriveStateForward` (0x44e00, 2880 B — asm 있음, 다시 읽기. **`+0x500 ~ +0x600` 언저리와 되돌아가는 곳만**) | 1 | 질문 1 |
| rpcrt4 | `EnableAsyncIfNecessary` (0x45d20, 181 B) · `LrpcBHDriveStateForwardRoutine` (0x3b000, 113 B) · `DriveStateForwardAsyncCallback` (0x3c264, 307 B) | 1 | 질문 1 (asm 있으면 읽기, 없으면 export) |
| rpcrt4 | `OpenSecurityContextWorker` (0x4599c, 645 B — asm 없음) | 1 | 질문 3 |
| rpcrt4 | `IsPortWithSharedSection` (0x58018, 83 B) + 그 caller 갈림 | 0 | 질문 5 |
| ntdll | `TpPostWork` (0xe24e0) · `TpSimpleTryPost` (0x84e00) · `TppWorkPost` (0x39430, 1346 B) · `TpSetWait` (0xf2cb0) — **1 에서 실제로 부르는 것만** | 1 | 질문 2 |
| ntdll | `TppWorkerThread` (0x1a2e0, 3824 B — 크다. 돌아온 뒤 유저 큐 보는 줄 · `0x102` 뒤 끝나는 줄만) · `TpAdjustBindingCount` (0xc9b90, 81 B) | 0~1 | 질문 2·4 |
| ntoskrnl | `NtSetInformationWorkerFactory` (0x3a6e10 — 종류 3 갈래만) · `NtWaitForWorkViaWorkerFactory` (0x32ae60 — 그 칸 읽는 줄만) | 0 | 질문 4 ② |

- 범위: bind 를 미는 자리 · 맡김 · 깨움 · 보안 문맥 · 종류 3 뜻까지. bind 뒤 (run137 ③ ~ ⑨) 는 다시 안 함. ALPC 커널 본문·NDR 은 이름만.
- 크면 **1 → 2 → 3 → 4 → 5** 차례로 쓰고 나머지는 "다음 요청으로".

## 받고 싶은 것

- 결과서 `_workspace/sast/138_async-bind_result.md`.
  1. asm 을 남겨 줌 (`ms/disassemble/<모듈>/<함수>.asm`). 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) 비동기 bind 차례 표 (`차례 · 스레드 (부른 쪽/일꾼) · 함수 · syscall · 줄`) — 동기 손님 차례와 나란히 · (2) 맡김 → 깨움 표 · (3) 보안 문맥 갈림 표 · (4) 종류 3 뜻 한 줄 + 조건 줄.
  4. **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 에뮬레이터 로그 (원문 seq 2191 ~ 2201) 와 비교: 어느 syscall 이 나왔어야 하나 / 에뮬레이터 어느 몸 (원문 "답이 오면 쓰는 곳": `epm.cpp accepts_async_connect`, 팀원 `src/threadpool/` 일꾼 공장 · 완료 포트) 이 무엇을 해야 하나.
  5. PDB GUID 확인 줄 (쓴 모듈마다).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문 짐작: `DriveStateForward +0x5d4` 가 탄 쪽 · 첫 bind 를 보내는 스레드 TID — Q258 세션과 합치게, 멈춤점 `함수+0x…`) 과 **"다음 요청 후보"**.
