# 2026-11-25 (작성일, 정적 분석 2026-11-25) run141 — 비동기 epmapper 연결 뒤 bind 를 미는 길 (Q267, 정적만): 비동기 첫 연결이면 `DriveStateForward` 는 `+0x515 Connect` 가 돌려준 보류 `0xc0021018` 을 받고 **그 자리에서 끝난다** (`+0x51e JNZ → +0x266`). `Bind__74b50` (`LRPC_FAST_BINDING_HANDLE::Bind`) 가 그 보류를 **0 으로 바꿔** 돌려주고, 부른 쪽 (winnsi 주 스레드) 은 이벤트에서 잔다 — **bind 는 아직 안 보냈다.** bind 를 미는 것은 **서버의 연결 응답 (ALPC 메시지 종류 `0xb`) 과 그 완료 패킷** 이다: 일꾼이 `IoCompleted` → `CompleteOnePart` → `SignalAllIos` → `SignalAsync +0x2f` → `TrySubmitWork` → `TrySubmitThreadpoolCallback` → **`ntdll!TpSimpleTryPost` → `TppWorkPost`** 로 일을 맡기고, 풀 일꾼이 그 일을 집어 `ThreadPoolInvokeCallback` → … → `LrpcBHDriveStateForwardRoutine` → `DriveStateForwardAsyncCallback` → `DriveStateForward` 둘째 부름 (상태 6 → 9, `+0x35d OpenSecurityContextWorker`) → **`Bind__93cc0 → BaseBind → LRPC_CASSOCIATION::Bind +0x626 NtAlpcSendWaitReceivePort` = bind** 를 보낸다. 같은 풀 일꾼이 "자기 몫" 으로 맡기면 깨우는 syscall 이 **없다** (`TppWorkPost +0x44c ~ +0x474`, `+0x4d0 JNZ` 가 `+0x4f3 NtReleaseWorkerFactoryWorker` 를 건너뜀)

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("비동기 epmapper 연결이 성공한 뒤 rpcrt4 는 bind 를 무엇으로 · 어느 스레드에서 보내나", 원 요청 `../PE_emulator/_workspace/00_planner/AS02_internals-async-bind.md`, 튜닝 `335_tune15.md` V3 순위 1, 막힌 샘플 탓 220) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 rpcrt4 · winnsi 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 과 같은 줄), `sample/*.c` 에 RPC API 가 없다 `[소스]`. 다만 이번에 읽은 ntdll `TppWorkPost` · `TppWorkerThread` · 커널 일꾼 공장은 test.exe 로더 풀과 **같은 몸** 이다 (run72 · run125). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp · ntdll · rpcrt4 줄 · 근거 줄) · 3장 run125 절 · run133 절 · run137 절 한 줄씩 · run137 절 뒤 새 절 · 4장 (run133 · run137 줄에 표시 · Q267 닫힘 줄 · run141 줄 셋) · 5장 (run141 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q267** (PE_emulator 요청 AS02, 사용자 결정 2026-11-25 — PE_emulator D154 의 첫째, 정적만). 결과서 판정 **분석성공** (질문 1 ~ 5 모두 정적으로 닫음, `[추측]` / `[공백]` 다섯 남김).
> - 이어 쓰는 앞 run: **run129** (winnsi 끝점 풀이 사슬 `DriveStateForward +0x8ca → … → AlpcConnect +0x293` · `+0x515 Connect`), **run133** (LRPC bind 0x70 B · 동기 / 비동기 = `LRPC_BIND_CCALL::Bind` 첫 정수 인자 — `DriveStateForward` 인자 전달은 `[공백]` 이었음), **run137** (ALPC 완료 포트 붙이기 · 패킷 · 일꾼 → `IoCompleted` — 이번 결과의 "연결 응답도 메시지 + 완료 패킷으로 와야 한다" 와 이어짐), **run125** (일꾼 공장 커널 몸 — `0x102` 판정 `ExpWorkerFactoryWantsToCreate`, 종류 3 = 묶음 수), **run72** (`TppWorkPost` 첫 넣기 `+0x4f3 NtReleaseWorkerFactoryWorker`).
> - 남은 동적 → **Q271 (동적: 결과서 동적 멈춤점 — Q258 세션과 합침)**, 남은 정적 → **Q272 (정적: 결과서 다음 요청 후보 1~4 — 커널 비동기 NtAlpcConnectPort 안쪽·연결 응답 0xb·`[…+0x80]=3` 쓰는 줄·`TppWorkerFindTask`·`[바인딩+0x128/+0x180/+0x12c]`·`CompleteOnePart` 거절 갈래)**. **Q259 ③ 일부 닫힘** (종류 3 뜻 · `0x102` 를 정하는 칸 — 아래 4절).

## 요약

- **첫 부름은 bind 를 안 보낸다** `[정적]`: 비동기 첫 연결이면 `DriveStateForward +0x515 Connect` 가 보류 `0xc0021018` → `+0x51e JNZ → +0x266` 반환 → `Bind__74b50 +0x217 ~ +0x212` 가 0 으로 바꿔 돌려줌 (`BaseBind` 건너뜀). 상태 8 (`+0x544 ~ +0x5d4`) 은 **둘째 부름 (연결 응답 뒤, 일꾼)** 에서 처음 돈다.
- **`+0x5d4 JZ` 는 같은 함수 맨 앞 `+0x43`** (루프) 로 간다. `[R13]` = `[바인딩+0x118]` = 요구 서버 SID 포인터 — 0 이면 바로 맨 앞 (8 → 9), 아니면 `+0x61b EqualSid` 로 연결 `+0xc0` 과 비교해 같으면 맨 앞. `DriveStateForward` 안에 **`Tp*` · `TrySubmit*` · `Bind` 는 없다.**
- **맡김은 `SignalAsync +0x2f`** `[정적]`: 연결 응답을 받은 일꾼이 `TrySubmitWork(ThreadPoolInvokeCallback, IO)` → `TrySubmitThreadpoolCallback` → **`ntdll!TpSimpleTryPost +0x107 TppWorkPost`**. `TpPostWork` · `TpSetWait` · `NtSetIoCompletion*` 은 이 길에 없다.
- **syscall 없는 맡김이 코드에 있다** `[정적: 갈래]`: 맡기는 쪽이 같은 풀 일꾼이고 그 기록 상태가 3 이면 3 → 4 · `R8D = 1` → `+0x4cd TEST R8D / +0x4d0 JNZ` 로 `+0x4f3 NtReleaseWorkerFactoryWorker` 를 건너뛴다. 이미 깨워 둔 일꾼이 있어도 (`+0x498`) 건너뜀. 이 길이 정말 그 갈래를 타는지는 `[추측]` → 동적 (Q271).
- **진짜 깨움은 맡김 앞** — 커널이 연결 응답 메시지를 클라이언트 포트 큐에 넣고 완료 패킷 `{키 = TP_ALPC, 0, 0, 0}` 을 넣는 것. 받는 쪽 (`IoCompleted +0x184 CMP ESI,0xb`) 은 `[정적]`, 커널이 넣는 쪽은 `[추측: 커널 안 읽음]`.
- **보안 문맥은 비동기도 같은 코드 · 같은 차례** — `OpenSecurityContextWorker` 에 동기 / 비동기 인자가 없다. 부르는 자리는 `DriveStateForward +0x35d` 하나 (상태 8 → 9). 비동기면 **일꾼 스레드에서, bind 바로 앞**. 실패하면 bind 를 안 보낸다 (`AbortCall`).
- **`NtSetInformationWorkerFactory(공장, 3, &1, 4)`** 는 풀 묶음 수 `[풀+0x1b0]` 가 **0 이하 → 양수** 일 때만 (그 풀의 첫 묶임). 종류 3 = 커널 `공장+0x194` 묶음 수 — 일꾼이 없을 때 패킷이 오면 만들게 하는 칸이지, **`0x102` 에 일꾼을 남기는 칸은 아니다.**
- **`NtAlpcQueryInformation(0)` 버퍼 안 쓴 성공** = `IsPortWithSharedSection` 반환 1 → 받기 버퍼 `0x1000`. 비동기 길은 안 바뀐다. 덤: 일꾼 `IoCompleted +0x6a` 도 같은 부름을 한 번 더 한다.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔아홉째 바퀴. plan **Q267**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** 모두 이미 analyze 된 모듈 — `^이름$` export 만 (결과서 "새로 뽑은 것"). kernelbase · kernel32 의 `TrySubmitThreadpoolCallback` 은 같은 시간에 sast/140 이 kernelbase 를 쓰는 중이라 export 안 하고 **바이트만** 읽었다 (`pefile` + `objdump`, 파일로 안 남김).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 같음 |
| ntdll | `C3093720177DA6851519DCFFE8DF53FE1` | 같음 |
| ntoskrnl | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음 |

- 버전 대조 (결과서): 알려진 사실 3줄 (`LdrpInitializeProcess +0x1bb6` · `LdrpLoadDll +0xfb` · `NtCreateUserProcess +0xc31`) 을 PE 바이트로 맞춤 — **일치**. 이번 핵심 call · jmp · jcc 34개도 같은 방법으로 맞춤 — 전부 일치.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): AS02 에서 epmapper 창구를 열었더니 **동기 손님은 bind 까지 가는데 비동기 손님 (winnsi) 은 연결 · 완료 포트 붙이기 (seq 2191 ~ 2193) 뒤 syscall 없이 `NtWaitForSingleObject` 에서 멈췄다.** 일꾼 둘은 `NtWaitForWorkViaWorkerFactory(0x64) → 0x102 → NtTerminateThread`. 요청서 `_workspace/sast/138_async-bind.md`:
  1. `DriveStateForward +0x589 ~ +0x5d4` 뒤 어디로 가나 — 같은 스레드에서 bind 를 부르나, 일을 맡기나 (무엇에 · 어떤 Tp 함수로), `LrpcBHDriveStateForwardRoutine` · `DriveStateForwardAsyncCallback` 이 그 콜백인가, `[R13]` 은 무엇인가.
  2. 맡긴 일이 일꾼을 깨우는 syscall · 항목 꼴, syscall 없는 길이 있나, `TppWorkerThread` 가 돌아온 뒤 유저 큐를 보는 줄과 `0x102` 뒤 끝나는 줄.
  3. 비동기 보안 문맥 (`OpenSecurityContextWorker`) 은 어디서 하나, 실패하면 bind 를 안 보내나.
  4. `NtSetInformationWorkerFactory(공장, 3, &1, 4)` 가 안 나오는 조건 · 종류 3 의 커널 뜻.
  5. `NtAlpcQueryInformation(0)` 버퍼 안 쓴 성공이 비동기 길을 바꾸나.
- 쓰는 곳 (원문 "답이 오면 쓰는 곳"): PE_emulator `epm.cpp accepts_async_connect` (창구), 팀원 `src/threadpool/` (일꾼 공장 · 완료 포트), `NtSetInformationWorkerFactory` 종류 3 · `NtWaitForWorkViaWorkerFactory` 몸.

## 방법

- 정적 sast/138 (결과서 `_workspace/sast/138_async-bind_result.md`, **분석성공**, 스크립트 문제 없음):
  - rpcrt4 `^이름$` 새로 (depth 0 ~ 1): `OpenSecurityContextWorker` (+ callee 3) · `SignalAllIos` · `SignalAsync` · `SignalSync` · `Complete` · `LRPC_BASE_MINI_THUNK` · `LrpcCreateBindingHandle` · `TrySubmitWork` · `ThreadPoolInvokeCallback` · `InvokeCallback` · `RestoreOldThreadToken` · `Bind` (같은 이름 4개 다시 — `.asm` 이 `Bind__<rva>` 로 갈림). 있던 `DriveStateForward` · `EnableAsyncIfNecessary` · `EnableAsync` · `LrpcBHDriveStateForwardRoutine` · `DriveStateForwardAsyncCallback` · `IsPortWithSharedSection` · `Connect` · `AlpcConnect` · `IoCompleted` · `CompleteOnePart` 읽기.
  - ntdll 새로: `TpSimpleTryPost` (depth 1, 7개) · `TppAllocThreadData` · `TppCritSetThread` · `TppCallbackEpilog` · `TppCallbackCheckThreadAfterCallback` · `TppPrepareDirectParams`. 있던 `TppWorkPost` · `TppWorkerThread` · `TpAdjustBindingCount` 읽기.
  - ntoskrnl: 있던 `NtSetInformationWorkerFactory` · `NtWaitForWorkViaWorkerFactory` · `ExpTryEnterWorkerFactoryAwayMode` · `ExpWorkerFactoryWantsToCreate` 읽기 (새 export 없음).
  - PE 데이터 (`pefile`, 파일로 안 남김): 가상표 `LRPC_FAST_BIND_CCALL` (`.rdata` `0xe2590`) · `LRPC_FAST_BINDING_HANDLE` (`0xe4440`) · `LRPC_BINDING_HANDLE` (`0xe2380`) 슬롯, kernelbase `TrySubmitThreadpoolCallback` (rva `0xe90d0`) · kernel32 (`0x3ce00`) 바이트.
  - Ghidra 로그는 꼬리표 없는 기본 이름 (`ms/ghidra/rpcrt4.export.log` · `ntdll.export.log`, 다음 export 가 덮음).
- 같은 시간에 decompiler 가 gdiplus · gdi32 · user32 (sast/141) 를 돈다 (`ms/` 만) — flow-writer 는 이번 바퀴 `.asm` 을 읽기만 했다. sast/139 · 140 결과서는 이번 바퀴가 아니라 안 읽었다.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - rpcrt4 `DriveStateForward +0x43` · `+0x69` · `+0x82` · `+0x1f1 / +0x1f4` · `+0x239` · `+0x253` · `+0x266` · `+0x2fe` · `+0x327 / +0x32a` · `+0x35d` · `+0x364 / +0x366` · `+0x3c6` · `+0x480` · `+0x4a8 / +0x4b3` · `+0x4be / +0x4c0` · `+0x4cd` · `+0x515 ~ +0x52b` · `+0x551` · `+0x55b` · `+0x57d / +0x584` · `+0x589` · `+0x5b2 ~ +0x5d4` · `+0x5da` · `+0x607 ~ +0x629` · `+0x64a` · `+0x799` · `+0x8ca` · `+0x8f1 ~ +0x906` · `+0x90f ~ +0x925` · `+0x93a` · `+0x946 ~ +0x958` · `+0xac5 ~ +0xacb`; `EnableAsyncIfNecessary` 전부; `LrpcBHDriveStateForwardRoutine +0xf` · `+0x5c`; `DriveStateForwardAsyncCallback +0x1b` · `+0x21` · `+0x43` · `+0x6b` · `+0x78` · `+0x7a` · `+0xa3` · `+0xb0` · `+0xbd` · `+0xe0` · `+0x104`; `SignalAsync` · `TrySubmitWork` · `ThreadPoolInvokeCallback` 전부; `Complete +0x7f` · `+0x93` · `+0xa4` · `+0xc6` · `+0x119` · `+0x1ae`; `LrpcBaseProviderComplete +0x44`; `Bind__74b50 +0x34` · `+0x5c ~ +0x6a` · `+0x145 ~ +0x166` · `+0x19a` · `+0x1a4 / +0x1a6` · `+0x20b ~ +0x21a` · `+0x2c3`; `Bind__93cc0 +0x30`; `BaseBind +0x1e4`; `Bind__4e330 +0x626`; `IoCompleted +0x6a ~ +0x76` · `+0xc8` · `+0x13e / +0x142` · `+0x184 / +0x187` · `+0x3bb` · `+0x3e3`; `CompleteOnePart (0x3b148) +0x1c7` · `+0x26e`; `SignalAllIos +0x11c`; `Connect +0x4b ~ +0x54` · `+0xd2` · `+0x12a / +0x138` · `+0x198 ~ +0x1c5` · `+0x2c0` · `+0x328 ~ +0x3b6`; `AlpcConnect +0x293` · `+0x379` · `+0x381 ~ +0x39f` · `+0x743` · `+0x752` · `+0x761`; `IsPortWithSharedSection +0x8 ~ +0x3f`; `OpenSecurityContextWorker` (3절 표 줄 전부); `RestoreOldThreadToken +0x3f` · `+0x5b` · `+0x69` · `+0x84`.
  - ntdll `TpSimpleTryPost +0xc1` · `+0x107`; `TppWorkPost +0x440 ~ +0x4f8` 전부; `TppAllocThreadData` 전부; `TppWorkerThread` (2-2 표 줄 전부); `TpAdjustBindingCount` 전부.
  - ntoskrnl `NtSetInformationWorkerFactory +0x3d5 ~ +0x44f` · `+0x565 ~ +0x59f` · `+0x7a8` · `+0x85f / +0x862`; `ExpTryEnterWorkerFactoryAwayMode` 전부; `ExpWorkerFactoryWantsToCreate` 전부; `ExpWorkerFactoryManagerThread +0x98 ~ +0xc3`; `NtReleaseWorkerFactoryWorker +0xea / +0xfb`; `NtWaitForWorkViaWorkerFactory +0x588` · `+0x979`; `NtQueryInformationWorkerFactory +0x165`.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **비동기 연결 = 식당에 전화로 예약하고 끊기.** rpcrt4 는 "자리 나면 연락 주세요" 만 남기고 (보류 `0xc0021018`) 전화를 끊는다. 부른 사람 (winnsi 주 스레드) 은 소파에서 잔다 (이벤트 기다림).
- **식당이 "자리 났어요" 하고 다시 전화하는 것 = 연결 응답 (종류 `0xb`).** 그 전화를 받는 건 소파의 사람이 아니라 **당번 (풀 일꾼)** 이다.
- 당번은 "주문 넣기 (bind)" 를 **할 일 쪽지로 적어 일감 바구니에 넣는다** (`TpSimpleTryPost`). 자기가 바로 집을 거면 아무도 안 깨운다 (syscall 없음).
- 그다음 당번이 신분증 확인 (보안 문맥) → 주문 (bind) 을 한다 `[추측: 비유]`.
- 그러니 **식당이 다시 전화를 안 하면 (연결 응답 · 완료 패킷이 없으면) 아무 일도 안 일어난다** — 에뮬레이터 로그가 멈춘 이유다.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{rpcrt4,ntdll,ntoskrnl}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `rpcrt4!BindToEpMapper` | `+0x141` | `+0x146` | `RpcBindingBind(비동기 상태, 핸들, …)` → 가상 `[+0x128]` = `Bind__74b50` (run129) | [정적] |
| `rpcrt4!LRPC_FAST_BINDING_HANDLE::Bind` (`Bind__74b50`, rva `0x74b50`) | **`+0x157`** | **`+0x15c`** | `DriveStateForward(this, EDX = fAsync, R8D, 호출)` (rva `0x44e00`) — `+0x151 MOV EDX,R14D` | [정적] |
| 〃 | `+0x19a` | `+0x19f` | `LRPC_BIND_CCALL::BaseBind` (rva `0x4c660`) — `DriveStateForward` 가 0 일 때만 (`+0x166 JNZ` 안 탐) | [정적] |
| `rpcrt4!DriveStateForward` | `+0x515` | `+0x51a` | `LRPC_CASSOCIATION::Connect` (rva `0x48018`) — 비동기 첫 연결이면 `0xc0021018` → `+0x51e JNZ → +0x266` | [정적] |
| 〃 | `+0x589` | `+0x58e` | `EnableAsyncIfNecessary` (rva `0x45d20`) — `+0x57d TEST [바인딩+0x178],1 / +0x584 JNZ` 면 건너뜀 | [정적] |
| 〃 | `+0x61b` | `+0x622` | `EqualSid` (가져오기) — `[R13] ≠ 0` 일 때만 | [정적] |
| 〃 | `+0x35d` | `+0x362` | `LRPC_CASSOCIATION::OpenSecurityContextWorker` (rva `0x4599c`) — 상태 9 몸 | [정적] |
| 〃 | `+0x8f4` · `+0x90f` · `+0x946` | `+0x8f9` · `+0x914` · `+0x94b` | `LRPC_CLIENT_IO::ReinitCommon` 뒤 IO `[+0]=3` · `[+8]=0x18003b000` (콜백 `LrpcBHDriveStateForwardRoutine`) | [정적] |
| `rpcrt4!EnableAsyncIfNecessary` | `+0x4e` | `+0x53` | `EnableAsync` — 연결 `[+0xd0] & 8` 이 꺼졌을 때만 (`+0x45 / +0x4c`) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::IoCompleted` (rva `0x57800`) | `+0x6a` | `+0x6f` | `IsPortWithSharedSection` → `NtAlpcQueryInformation(포트, 0, …)` | [정적] |
| 〃 | `+0xc8` | `+0xcf` | `NtAlpcSendWaitReceivePort` (받기만, run137) | [정적] |
| 〃 | **`+0x3e3`** | **`+0x3e8`** | **`CompleteOnePart` (rva `0x3b148`)** — 종류 `0xb` (`+0x184 CMP ESI,0xb / +0x187 JZ → +0x3bb`) | [정적] |
| `rpcrt4!CompleteOnePart` (`0x3b148`) | `+0x1c7` | `+0x1cc` | `CLIENT_IO_PROVIDER::SignalAllIos` (rva `0x3b528`) | [정적] |
| `rpcrt4!SignalAllIos` | `+0x11c` | `+0x121` | `LRPC_CLIENT_IO::SignalAsync` (rva `0x3c1a4`) | [정적] |
| `rpcrt4!SignalAsync` | **`+0x2f` (JMP)** | — (꼬리 점프) | **`RPC_THREAD_POOL::TrySubmitWork`** (rva `0x2f2c0`) — `RCX` = `ThreadPoolInvokeCallback` (`+0x1b LEA RCX,[0x180098830]`), `RDX` = IO | [정적] |
| `rpcrt4!TrySubmitWork` | `+0x10` | `+0x15` | `InitializeCallbackEnvironmentIfNecessary` | [정적] |
| 〃 | **`+0x26`** | **`+0x2d`** | **`TrySubmitThreadpoolCallback`** (api-ms 가져오기 칸 `0x1800e9060`, 환경 = `CallbackEnvironment`) → kernelbase `+0x4` (kernel32 `+0x6`) → `ntdll!TpSimpleTryPost` | [정적] |
| `ntdll!TpSimpleTryPost` (rva `0x84e00`) | `+0xc1` | `+0xc6` | `TppWorkInitialize` | [정적] |
| 〃 | **`+0x107`** | **`+0x10c`** | **`TppWorkPost`** (rva `0x39430`) | [정적] |
| `ntdll!TppWorkPost` | `+0x4f3` | `+0x4f8` | `NtReleaseWorkerFactoryWorker([풀+0x38])` — **자기 몫 (`+0x4cd / +0x4d0`) 이나 깨워 둔 일꾼 (`+0x498 / +0x49e`) 이면 안 옴** | [정적] |
| `ntdll!TppWorkerThread` (rva `0x1a2e0`) | `+0x6fd` | `+0x702` | `TppWorkerFindTask` (rva `0x192c0`) — 콜백 뒤 줄 카운터 ≠ 0 이면 | [정적] |
| 〃 | `+0x814` | `+0x819` | `TppSimplepExecuteCallback` → (간접) `ThreadPoolInvokeCallback` | [정적] |
| `rpcrt4!ThreadPoolInvokeCallback` (rva `0x98830`) | `+0x11` | `+0x16` | `LRPC_CLIENT_IO::InvokeCallback` (rva `0x430f8`) → IO 콜백 = `LrpcBaseProviderComplete` (`0x96a10`) | [정적] |
| `rpcrt4!LrpcBaseProviderComplete` | `+0x44` | `+0x49` | `CLIENT_IO_PROVIDER::Complete` (rva `0x42ed4`) | [정적] |
| `rpcrt4!Complete` | `+0x119` | `+0x11e` | `InvokeCallback` — IO 칸을 보관해 둔 원래 값 (종류 3 · `LrpcBHDriveStateForwardRoutine`) 으로 되돌린 뒤 (`+0x93 ~ +0xa4`) | [정적] |
| `rpcrt4!LrpcBHDriveStateForwardRoutine` (rva `0x3b000`) | `+0x5c` | `+0x61` | `DriveStateForwardAsyncCallback` (rva `0x3c264`) — `+0xf ESI = [IO+0x38]` (상태) | [정적] |
| `rpcrt4!DriveStateForwardAsyncCallback` | **`+0xb0`** | **`+0xb5`** | **`DriveStateForward(this, 1, 0, 호출)`** — 둘째 부름 | [정적] |
| 〃 | **`+0xe0`** | **`+0xe5`** | 가상 `[호출 vtbl+0x188]` = **`Bind__93cc0`** (`LRPC_FAST_BIND_CCALL` 가상 Bind) | [정적] |
| 〃 | `+0x104` | `+0x109` | 가상 `[vtbl+0x190]` = `CallBound(호출, 1)` (rva `0x76630`) | [정적] |
| 〃 | `+0x43` · `+0x6b` | `+0x48` · `+0x70` | 가상 `[바인딩 vtbl+0x1b8]` (`0x914d0`) · `[+0x1c0]` = `PrepareForRetry` (`0x93ea0`) — 실패 갈래 | [정적] |
| `rpcrt4!Bind__93cc0` | `+0x30` | `+0x35` | `BaseBind` | [정적] |
| `rpcrt4!BaseBind` | `+0x1e4` | `+0x1e9` | `LRPC_CASSOCIATION::Bind` (`Bind__4e330`) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::Bind` (`0x4e330`) | **`+0x626`** | **`+0x62d`** | **`NtAlpcSendWaitReceivePort` = bind** (run133) | [정적] |
| `rpcrt4!OpenSecurityContextWorker` | `+0x52` | `+0x59` | `NtOpenThreadToken(스레드, 0xc, 1, &토큰)` | [정적] |
| 〃 | `+0xb4` | `+0xbb` | `NtSetInformationThread(현재, 5, &토큰, 8)` | [정적] |
| 〃 | `+0x1c3` | `+0x1ca` | `NtImpersonateAnonymousToken` (토큰값 `0xfffffffe` 일 때) | [정적] |
| 〃 | **`+0x124`** | **`+0x12b`** | **`NtAlpcCreateSecurityContext(포트 [연결+0x88], 0, [문맥+8])`** | [정적] |
| 〃 | `+0x14d` | `+0x152` | `RestoreOldThreadToken` (rva `0x6f56c`) → `+0x3f NtSetInformationThread(-2, 5)` (ret `+0x46`) · `+0x5b CloseHandle` | [정적] |
| `ntdll!TpAdjustBindingCount` (rva `0xc9b90`) | `+0x3b` | `+0x40` | `NtSetInformationWorkerFactory([풀+0x38], 3, &±1, 4)` — 묶음 수가 0 ↔ 양수로 바뀔 때만 | [정적] |
| `nt!NtSetInformationWorkerFactory` | `+0x582` | `+0x587` | `ExpTryEnterWorkerFactoryAwayMode` (rva `0x32e460`) — 종류 3 옛 0 → 새 ≠ 0 | [정적] |
| 〃 | `+0x59a` | `+0x59f` | `ExpWorkerFactoryCheckCreate` (TryEnter 참일 때) | [정적] |
| 〃 | `+0x44f` | `+0x454` | `ExpLeaveWorkerFactoryAwayMode` (옛 ≠ 0 → 새 0 · `[공장+0x198] & 0x200`) | [정적] |
| `nt!ExpTryEnterWorkerFactoryAwayMode` | `+0x97` | `+0x9c` | `KeRegisterObjectNotification(포트, ExpWorkerFactoryManagerQueue, 공장+0x268)` | [정적] |

### 1. 비동기 bind 를 미는 길 (질문 1) [정적]

#### 1-1. `DriveStateForward` (rva `0x44e00`) 칸 · 첫 부름 · 둘째 부름

`DriveStateForward(this = 바인딩 핸들, fAsync, 제한 시간, 호출)`. `fAsync` 는 `[RSP+0x60]` (= EDX), 호출 = `R14`.

| 칸 | 뜻 `[추측: 이름]` | 줄 |
|---|---|---|
| `[바인딩+0x114]` | 바인딩 상태 (1 ~ 9 …) | `+0x480` (6) · `+0x551` (8) 등 |
| `[호출+0x2d0]` · `+0x2d4` | "이 상태면 저 상태로" 짝 (옛, 새). 함수 맨 앞 `+0x69 ~ +0x82` 가 바인딩 상태 == 옛 이면 새로 바꿈 | 쓰기 `+0x4a8 / +0x4b3` (6 → 7) · `+0x5b9 / +0x5c4` (8 → 9) |
| `[호출+0x288]` = `LRPC_CLIENT_IO` | 종류 `[+0]` (1 동기 · 3 콜백형) · 콜백 `[+8]` · 상태 `[+0x38]` | `+0x4cd` (1) · `+0x900 / +0x906` (3, 콜백) |
| `[바인딩+0x118]` (= `R13`) | `LPC_NORMALIZED_SID` — 첫 8 B = **요구 서버 SID 포인터** | `+0x5b2 LEA R13,[RDI+0x118]`, `Connect` 다섯째 인자 (`+0x4f2`) |
| `[연결+0xc0]` | 연결이 들고 있는 서버 SID (첫 연결 때 `Connect +0x12a DuplicateSID → +0x138`) | `+0x607 LEA R15,[R12+0xc0]` · `+0x60f` |

- **첫 부름 (부른 쪽 스레드)**: 상태 6 몸 (`+0x480 [바인딩+0x114] = 6` 뒤 `+0x48a ~ +0x52b`).
  - `+0x4a8 / +0x4b3` 짝 6 → 7.
  - `+0x4be TEST ESI / +0x4c0 JNZ → +0x8f1`: 비동기면 `ReinitCommon` 뒤 IO 를 **콜백형 (종류 3 · `LrpcBHDriveStateForwardRoutine`)** 으로 (`+0x8f9 ~ +0x906`). 동기면 `+0x4cd [IO] = 1`.
  - `+0x515 Connect`. **`+0x51e JNZ → +0x266` 반환** — 비동기 첫 연결은 `0xc0021018` 이라 **여기서 함수가 끝난다.** 동기는 0 이라 `+0x524 / +0x52b JMP +0x43` 으로 같은 호출 안에서 계속 돈다.
- **둘째 부름 (연결 응답 뒤, 일꾼)**: 맨 앞 `+0x69 ~ +0x82` 가 6 → 7 → `+0x551 [바인딩+0x114] = 8` → **상태 8 몸 (`+0x55b ~ +0x5d4`)**:
  - `+0x55b R15D = fAsync` · `+0x57d / +0x584` (이미 켜졌으면 건너뜀) · **`+0x589 EnableAsyncIfNecessary`** — 연결 `[+0xd0] & 8` 이 이미 켜져 있으면 (`Connect +0x3b1` 이 첫 연결 때 켬) `EnableAsync` 를 안 부르고 바인딩 깃발 `[+0x178] |= 1` 만 (`+0x59`). **syscall 없음, 새 `CreateAlpc` 없음.**
  - `+0x5b2 R13 = 바인딩+0x118` · `+0x5b9 / +0x5c4` 짝 8 → 9 · **`+0x5cf CMP [R13],0` · `+0x5d4 JZ → +0x43`** (함수 맨 앞, `RtlEnterCriticalSection` 다시).
  - `[R13] ≠ 0`: `+0x5da ~ +0x607` (비동기면 IO 를 다시 콜백형 `+0x90f ~ +0x925`) → `+0x60f [연결+0xc0]` (0 이면 `+0x615 JZ → +0x799 AlpcVerifyServerSid`) → **`+0x61b EqualSid`** → `+0x629 JNZ → +0x43` (같으면). 다르면 `+0x64a PrepareIterationInternal` → `+0xacb IterateAndVerify`.
  - 맨 앞으로 돌아가면 8 → 9 → `+0x82 JZ → +0x2fe` 상태 9 몸 → **`+0x35d OpenSecurityContextWorker`** → `+0x3c6` 가상 `[바인딩 vtbl+0x1e0]` → 상태 9 → `+0x1f1 CMP EDX,9 / +0x1f4 JZ → +0x239` → 반환 (`+0x266`).
- **이 함수의 callee 에 `Bind` · `Tp*` · `TrySubmit*` 는 없다** (`ms/decomp/rpcrt4/DriveStateForward.c` 머리 목록 — 결과서).

#### 1-2. 비동기 bind 차례표 (동기 손님 로그 5617 ~ 5623 과 나란히)

("부른 쪽" = winnsi 주 스레드, "일꾼" = 풀 일꾼. 로그 seq 는 에뮬레이터 로그 — 비교용.)

| 차례 | 스레드 | 함수 · 줄 | syscall | 동기 손님은 |
|---|---|---|---|---|
| A1 | 부른 쪽 | winnsi `NsiRpcRegisterChangeNotificationEx +0x168 CreateEventW` (run129) | `NtCreateEvent` (2185) | — |
| A2 | 부른 쪽 | `BindToEpMapper +0x141 RpcBindingBind(비동기 상태, …)` ⇢ `Bind__74b50 +0x157 DriveStateForward(this, EDX = 1, …)` | — | `EDX = 0` |
| A3 | 부른 쪽 | `DriveStateForward +0x515 Connect` (IO 콜백형 `+0x8f9 ~ +0x906`) | — | 같음 (IO 종류 1) |
| A4 | 부른 쪽 | `Connect` 첫 연결: `+0x2c0 LRPC_BASE_MINI_THUNK` (원래 콜백 보관) · `+0x371 ~ +0x37f` IO 콜백을 `LrpcBaseProviderComplete` (`0x96a10`) 로 · `+0xd2 Register` · `+0x198 AlpcConnect` | **`NtAlpcConnectPort(Flags 0, RequiredServerSid = [연결+0xc0], 들어오는 속성)`** (`AlpcConnect +0x293`, 2191) | Flags `0x20000` (5617) |
| A5 | 부른 쪽 | `AlpcConnect +0x388 IsPortWithSharedSection` → `+0x395 / +0x39f JNZ → +0x752 ~ +0x761`: `*출력 = 1`, 보류 `0xc0021018` | `NtAlpcQueryInformation(포트, 0, 버퍼, 0x10)` (2192) | 5618 |
| A6 | 부른 쪽 | `Connect +0x19f → +0x328 → +0x32d JZ → +0x3a4 → +0x3b1 EnableAsync` → `CreateAlpc` → … → `TppAllocAlpcCompletion +0x1d1` (run137) → `+0x35f EBX = 0xc0021018` | `NtAlpcSetInformation(포트, 2, 16 B, 0x10)` (2193) · 풀 첫 묶임이면 `NtSetInformationWorkerFactory(공장, 3, &1, 4)` | 없음 (동기는 `EnableAsync` 안 부름) |
| A7 | 부른 쪽 | `DriveStateForward +0x51e JNZ → +0x266` 반환 `0xc0021018` → `Bind__74b50 +0x217 / +0x21a JZ → +0x1a4 / +0x1a6 JNZ → +0x20b CMP / +0x210 XOR EBX,EBX` (**0 으로 바꿔 반환**, `BaseBind` 건너뜀) → winnsi `WaitForSingleObject` | `NtWaitForSingleObject` (2194 뒤) | 같은 호출 안에서 6 → 9 를 돌고 (`Connect` 가 막고 기다림) `Bind__74b50 +0x19a BaseBind` |
| — | 커널 | 서버 (epmapper) 가 연결을 받음 → 클라이언트 포트에 연결 응답 메시지 + 완료 패킷 `[추측: 커널 안 읽음]` | | |
| B1 | 일꾼 W | `TppWorkerThread +0x379 NtWaitForWorkViaWorkerFactory` 0 반환 (항목 `{키, 0, 0, 0}`) → `+0x55e TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted` (run137 2-2) | (`NtWaitForWorkViaWorkerFactory` 반환) | — |
| B2 | 일꾼 W | `IoCompleted +0x6a IsPortWithSharedSection` → `+0xc8 NtAlpcSendWaitReceivePort` (받기만) | **`NtAlpcQueryInformation(포트, 0, …)`** → **`NtAlpcSendWaitReceivePort(포트, 0, NULL, NULL, 버퍼, &길이, &속성, NULL)`** → 메시지 종류 **`0xb`** | — (동기는 5617 이 곧바로 연결 끝) |
| B3 | 일꾼 W | `IoCompleted +0x13e ~ +0x142` 종류 = `[+4] & 0xff` · **`+0x184 CMP ESI,0xb / +0x187 JZ → +0x3bb`** → `+0x3e3 CompleteOnePart(연결, 0, 0, NULL, 1)` → `+0x26e [연결+0x4c] = 4` (연결됨) · **`+0x1c7 SignalAllIos`** → `+0x11c SignalAsync` (IO 종류 3) | — | `Connect +0x1c5` 가 `CompleteOnePart` 를 바로 (같은 스레드) |
| B4 | 일꾼 W | `SignalAsync`: `+0x22 [IO+0x38] = 상태` · `+0x1b` 콜백 `ThreadPoolInvokeCallback` · **`+0x2f JMP TrySubmitWork`** → `+0x26 TrySubmitThreadpoolCallback` (환경 NULL = 전역 풀) → **`ntdll!TpSimpleTryPost +0xc1 TppWorkInitialize` · `+0x107 TppWorkPost`** | **없음** (자기 몫이면) · 아니면 `NtReleaseWorkerFactoryWorker` 1 | — |
| B5 | 일꾼 W | 콜백 끝난 뒤 `TppWorkerThread +0x65f ~ +0x68d` 줄 카운터 ≠ 0 → `+0x845` 1 줄임 → `+0x6fd TppWorkerFindTask` → `+0x814 TppSimplepExecuteCallback` → `ThreadPoolInvokeCallback +0x11 InvokeCallback` → `LrpcBaseProviderComplete +0x44 Complete` → `+0x119 InvokeCallback` → **`LrpcBHDriveStateForwardRoutine`** → `+0x5c DriveStateForwardAsyncCallback` → 상태 0 이라 `+0x1b JZ → +0xa3` → **`+0xb0 DriveStateForward(this, 1, 0, 호출)`** | 없음 | — |
| B6 | 일꾼 W | `DriveStateForward` 둘째 부름 (1-1): 6 → 7 → 8 → (`EnableAsyncIfNecessary` 깃발만) → `[R13] ≠ 0` → `EqualSid` 같음 → 8 → 9 | — | 같은 일을 부른 쪽이 한 호출 안에서 |
| B7 | 일꾼 W | **`+0x35d OpenSecurityContextWorker`** (3절) | `NtOpenThreadToken` · `NtSetInformationThread(-2, 5)` · `NtAlpcCreateSecurityContext(포트, 0, …)` · `NtSetInformationThread(-2, 5)` | 5619 ~ 5622 |
| B8 | 일꾼 W | 반환 0 → **`DriveStateForwardAsyncCallback +0xe0`** 가상 `[vtbl+0x188]` = `Bind__93cc0` → `+0x30 BaseBind` → `+0x1e4 LRPC_CASSOCIATION::Bind` → **`+0x626`** | **`NtAlpcSendWaitReceivePort(포트, Flags 0, bind 0x70 B, …, 받기 NULL)`** = **bind** | 5623 (Flags `0x20000`, 같은 버퍼로 답) |
| B9 | 일꾼 W | `DriveStateForwardAsyncCallback +0x104` 가상 `[vtbl+0x190]` = `CallBound(호출, 1)` | — | — |
| C | 일꾼 | bind 답 · 요청 · 응답 | run137 5절 ③ ~ ⑨ (다시 안 함) | |

- **bind 를 보내는 스레드 = 풀 일꾼** `[정적]` — 연결 응답 패킷을 받은 일꾼이 자기 몫으로 이어서 하거나, 아니면 `NtReleaseWorkerFactoryWorker` 로 깨운 다른 일꾼. 부른 쪽 스레드는 아니다. **첫 연결 때 한정.**
- **이미 연결된 (`[연결+0x4c] = 4`) 연결을 재사용하면** `Connect +0x4b CMP R13D,4 / +0x4f / +0x54 JNZ → +0x388` 이 곧바로 0 을 돌려주므로 `DriveStateForward` 첫 부름이 0 으로 끝나 `Bind__74b50 +0x19a BaseBind` 로 **부른 쪽 스레드가 bind 를 보낸다** `[정적: 갈래만, 실제로 타는 쪽은 동적]`. winnsi 의 이번 길은 첫 연결이라 해당 없음.
- `Complete` (`+0x7f ~ +0xc6`) 는 IO 칸을 보관해 둔 원래 값으로 되돌린 뒤 (`+0x93 ~ +0xa4`) `+0x119 InvokeCallback`. 그 사이 `+0x1ae IsRetryErrorCode` 갈래는 상태 0 에서 안 탄다고 읽음 `[추측: 본문 안 읽음]`.
- `Bind__74b50` = `LRPC_FAST_BINDING_HANDLE::Bind` — PDB 가상표 `??_7LRPC_FAST_BINDING_HANDLE@@6B@` (`0xe4440`) 슬롯 `+0x128` = `0x74b50`. 비빠른 `LRPC_BINDING_HANDLE` (`0xe2380`) 의 같은 슬롯은 6 B 기본 몸 (`0xadd70`). epmapper 핸들이 빠른 핸들이라는 건 `[추측]`. `[vtbl+0x188]` = `0x93cc0` (Bind) · `+0x190` = `0x76630` (`CallBound`) · `+0x128` = `0x77210` (`AbortCall`) 은 `LRPC_FAST_BIND_CCALL` 가상표 (`0xe2590`) — `[정적: PE]`.

### 2. 맡김 → 깨움 (질문 2) [정적]

#### 2-1. 표

| 무엇이 | 누가 | syscall | 항목 · 패킷 꼴 | 줄 |
|---|---|---|---|---|
| **① 일꾼 W 를 깨움 (연결 응답)** | 커널 (메시지가 클라이언트 포트 큐에 들어갈 때 패킷 — run137 2-1 은 보내기 · 답 · 닫기 · 연결 요청 처리까지 읽음. **연결 응답이 같은 길인지는 `[추측]`**) | 유저 쪽 없음 | `{KeyContext = TP_ALPC, 0, 0, 0}`, 일꾼은 0x20 B 항목으로 받음 | run137 2-1 · 2-2; `TppWorkerThread +0x379` |
| **② 맡기기 (`TpSimpleTryPost`)** | 같은 일꾼 W (콜백 도는 중) | **없음** | 풀 일감 줄에 한 항목 + `[풀+8]` 위 dword `+1` (`+0x48a ~ +0x490` · `+0x4a8 CMPXCHG`) | `TppWorkPost +0x440 ~ +0x474` (`GS:[0x30]` → `[TEB+0x1778]` → `[…+0]` → `+0x30 == 풀` · `+0x80` 3 → 4 · `R8D = 1`), `+0x4cd TEST R8D / +0x4d0 JNZ → +0x4a0` |
| ②′ 다른 스레드가 맡길 때 (또는 상태가 3 이 아닐 때) | 맡기는 스레드 | **`NtReleaseWorkerFactoryWorker([풀+0x38])`** | 커널이 "네 칸 0" 항목 (run125) | `+0x498 TEST ECX,0xffff0000 / +0x49e JZ → +0x4cd` (깨워 둔 일꾼 수 ≠ 0 이면 건너뜀) → 둘 다 아니면 `+0x4d2 ~ +0x4ed` 그 수 `+1` (`+0x4dd ADD EDX,0x10000`) · `+0x4b7 JNZ → +0x4ef` → **`+0x4f3`** |
| ③ 일꾼이 일을 집음 | W | 없음 | — | `TppWorkerThread +0x65f ~ +0x6c6` · `+0x845` · `+0x6fd TppWorkerFindTask` |

- 이름 `[추측]`: `[풀+8]` 아래 dword 의 위 16 b = "깨워 둔 일꾼 수", 아래 16 b = "쉬는 일꾼 수" (`TppWorkerThread +0x696 MOVSX ECX,AX / +0x699 INC ECX`), 위 dword = "줄에 쌓인 일감 수". 칸 이름은 PDB 에 없고 쓰는 줄에서만 읽었다. `[TEB+0x1778]` 은 PDB `_TEB.ThreadPoolData`.
- **상태 3 을 쓰는 줄은 못 찾았다** `[공백]` — 그래서 "연결 응답을 받은 일꾼이 정말 자기 몫인지" 는 동적 (Q271 3). 아니면 이 자리에서 `NtReleaseWorkerFactoryWorker` 가 한 번 나온다.
- 에뮬레이터 로그의 "맡김 쪽 syscall 0 개" 는 어느 쪽이어도 **모순이 아니다** — 그 로그는 맡김 자리 (연결 응답 처리) 까지 못 갔다.
- run72 의 "첫 넣기 = `+0x4f3` 1번" 과 어긋나지 않는다: 새 풀이라 깨워 둔 수는 0 이고, 넣는 스레드가 메인 스레드면 `[TEB+0x1778]` 이 0 이라 (`+0x453 / +0x456` 건너뜀) 두 건너뛰기 갈래 모두 안 탄다 — 메인 스레드 값 0 은 run72 동적 조건 4 가 볼 값이라 `[추측]`. 로더 풀 일꾼이 다음 일감을 넣을 때 자기 몫 갈래를 타는지는 안 봤다 `[공백]`.

#### 2-2. 일꾼이 `NtWaitForWorkViaWorkerFactory` 에서 돌아온 뒤 (`TppWorkerThread`, rva `0x1a2e0`)

| 상황 | 줄 | 하는 일 |
|---|---|---|
| 상태 0, 항목 있음 (키 ≠ 0) | `+0x37e ~ +0x402` → `+0x46e TppPrepareDirectParams` → `+0x485 ~ +0x490` → `+0x536 ~ +0x55e` 키 `[+0x38]` 로 가름 (`TppAlpcpExecuteCallback` · `TppIopExecuteCallback` · `TppWaitCompletion`) | 콜백 실행 |
| 콜백 뒤 | `+0x5eb TppCallbackEpilog` → **`+0x65f ~ +0x68d`**: `[풀+8]` 위 dword ≠ 0 이면 `+0x68d JNZ → +0x845` (`-1`, `+0x851 DEC ECX` · `+0x85a JMP → +0x6ae` · `+0x6b6 CMPXCHG`) → `+0x6c6` → **`+0x6ea → +0x6fd TppWorkerFindTask`**. 0 이면 쉬는 수 `+1` · **`+0x6e5 JMP → +0x2ce`** (다시 `NtWaitForWorkViaWorkerFactory`) | **유저 큐 확인 = 이 줄들** |
| 상태 0, 항목 없음 (`[RSP+0x298]` = 0, 풀어주기 패킷) | `+0x485 ~ +0x490 JZ → +0x6ea` | 곧바로 `TppWorkerFindTask` |
| 상태 `0x80` (마감) | `+0xc03 SUB ECX,0x80 / +0xc09 JZ → +0xe13` | 끝내기 길 |
| 상태 `0xc0` (사용자 APC) | `+0xc0f SUB ECX,0x40 / +0xc12 JZ → +0xdc0` → `TppCallbackCheckThreadAfterCallback` → `+0xdff JMP → +0x2e9` | 다시 기다림 |
| **상태 `0x102` (쉼 시간 끝)** | **`+0xc18 CMP ECX,0x42 / +0xc1b JZ → +0xda4`** `[RSP+0x46] = 1` → `+0xda9 JMP → +0xe13` → `+0xec4 TppPoolRemoveWorker` · `+0xed3 TppPoolUpdateTrimmedWorker` · `+0xf58 TppCritResetThread` · `+0xf65 TppFreeThreadData` → **`+0xfce RtlExitUserThread`** | **유저 큐를 안 보고 끝난다** |

- "유저 큐에 일이 있으면 안 끝나나" → 유저 쪽은 `0x102` 를 받으면 **큐를 안 보고 끝난다.** 일이 있을 때 `0x102` 를 **안 주는 건 커널**: `ExpWorkerFactoryWantsToCreate(공장, 1)` 이 참이면 쉼 시간 끝에도 안 줄인다 (run125 `0x102` ②). 참 조건: `+0x3 ~ +0xa` `[공장+0x190] ≠ 0` 이면 거짓 · `+0x12 ~ +0x25` `[공장+0x180] ≥ [공장+0x17c] (+1)` 이면 거짓 · `+0x27 ~ +0x2f` 기다리는 일꾼 `[P+0x1c]` > 1 이면 거짓 · **`+0x35 ~ +0x3f` 포트 큐 `[포트+4] ≠ 0` 이면 참** · **`+0x43 ~ +0x47` 풀어주기 수 `[P+0x18] > 0` 이면 참**. `NtReleaseWorkerFactoryWorker +0xea ~ +0xfb` 가 `[P+0x18]` 을 올린다.
- 자기 몫 맡김은 `NtRelease` 도 포트 항목도 안 쓰므로 이 보호가 필요 없다 — 맡긴 일꾼이 **콜백 끝나자마자** 카운터를 보고 집기 때문 (`+0x65f ~ +0x68d`).

### 3. 비동기 보안 문맥 (질문 3) [정적]

`OpenSecurityContextWorker` (rva `0x4599c`, 645 B) 는 **동기 · 비동기 인자가 없다.** 동기냐 비동기냐는 "어느 스레드가 부르느냐" 일 뿐이고, 부르는 자리는 `DriveStateForward +0x35d` (상태 8 → 9) 하나다. 인자: `(연결 = [바인딩+0x170], 가장 종류 = [바인딩+0x12c], 유효만 = ([바인딩+0x178] >> 1) & 1, 문맥 = 바인딩+0x130, 토큰값 = [바인딩+0x180])` (`+0x2fe ~ +0x35d`).

| 갈림 | 조건 | 줄 | syscall |
|---|---|---|---|
| ① 만들기 자체 | `[바인딩+0x128] == 1` 이면 **안 만든다** (칸만 채움) | `DriveStateForward +0x327 CMP EDX,1 / +0x32a JNZ → +0x355` | 없음 |
| ② 현재 스레드 토큰 열기 | 항상 | `+0x39 GetCurrentThread` · **`+0x52 NtOpenThreadToken(스레드, 0xc, 1, &토큰)`** | `NtOpenThreadToken` (5619) |
| ②-a `0xc0000022` | `+0x5e / +0x63 JZ → +0x27c` | 오류 5, bind 안 감 | — |
| ②-b `0xc000007c` (토큰 없음) | `+0x73 / +0x78` → `+0x7e [저장] = 0xfffffffd` (`+0x69`) | 계속 | — |
| ②-c `0xc00000a6` | `+0x1e4 / +0x1e9 JZ → +0x22d` `[저장] = 0xfffffffe` (익명) | 계속 | — |
| ②-d 성공 | `+0x1eb / +0x1ed JZ → +0x82` (핸들 저장) | 계속 | — |
| ②-e 그 밖 | `+0x1f3 EBX = 0xe` → 오류 기록 | bind 안 감 | — |
| ③ 가장 토큰 지정 | 토큰값 `R15`: `0xfffffffd` → NULL (`+0x89 ~ +0x95`) · `0xfffffffe` → **`NtImpersonateAnonymousToken`** (`+0x1ab ~ +0x1c3`) · 그 밖 → 그 핸들 | **`+0xb4 NtSetInformationThread(현재, 5, &토큰, 8)`** (5 = `ThreadImpersonationToken` `[추측: 이름]`) — 실패 `+0xc2 JS → +0x173` | `NtSetInformationThread(-2, 5)` (5620) |
| ④ ALPC 보안 문맥 | ③ 성공 뒤 | `+0xc8 ~ +0xdd` 칸 채움 (`[문맥+8]` 의 `+0` = `0x20000`, `+0x10` = 0) · **`+0x124 NtAlpcCreateSecurityContext(포트 [연결+0x88], 0, [문맥+8])`** | `NtAlpcCreateSecurityContext` (5621) |
| ④-a 실패 | `+0x138 JS → +0x236`: `0xc0000022` · `0xc00000a5` → 5, 그 밖 → 0xe | 오류 기록 후 반환 | — |
| ⑤ 되돌리기 | 항상 | **`+0x14d RestoreOldThreadToken`**: `0xfffffffe` → `NtImpersonateAnonymousToken` (`+0x69 ~ +0x84`) · 그 밖 → **`NtSetInformationThread(-2, 5, &(저장 == 0xfffffffd ? 0 : 저장), 8)`** (`+0x3f`) + 핸들이면 `CloseHandle` (`+0x5b`) | `NtSetInformationThread(-2, 5)` (5622) |

- **실패하면 bind 를 안 보낸다**: 0 이 아니면 `DriveStateForward +0x364 / +0x366 JNZ → +0x253` (CS 풀고 `+0x266` 반환) → `DriveStateForwardAsyncCallback +0xbd JNZ → +0x21` → `+0x43` 가상 `[바인딩 vtbl+0x1b8]` (오류 종류) → 다시 시도 가능이면 `+0x6b [+0x1c0] PrepareForRetry` 뒤 `+0x78 JZ → +0x1d` 다시 `DriveStateForward`, 아니면 `+0x7a ~ +0x93` `[호출 vtbl+0x128]` = **`AbortCall`** 로 호출을 오류로 끝냄. `+0xe0` bind 로 못 간다.
- **차례는 동기와 같다** — 로그 5619 → 5622 ↔ ② → ③ → ④ → ⑤. 비동기는 **일꾼 (B7) 에서, 연결 응답 뒤, bind (B8) 바로 앞.**
- 어느 갈래를 타는지 (`[바인딩+0x180]` · `[바인딩+0x128]` 실제 값) 는 정적으로 못 정함 `[공백]` — 동기 로그가 `NtOpenThreadToken` 뒤 곧바로 `NtSetInformationThread(-2, 5)` 이라 "토큰 없음 (`0xfffffffd`)" 갈래로 읽힌다 `[추측]`.

### 4. `NtSetInformationWorkerFactory(공장, 3, &1, 4)` (질문 4) [정적]

#### 4-1. 나오는 · 안 나오는 조건 (`ntdll!TpAdjustBindingCount(풀, 증감)`, rva `0xc9b90`)

| 줄 | 하는 일 |
|---|---|
| `+0x4 TEST EDX / +0x6 JZ` | 증감 0 이면 아무것도 안 함 |
| `+0xa XADD.LOCK [풀+0x1b0], 증감` · `+0x12 LEA R8D,[RAX+RDX]` | 옛 값 → `EAX`, 새 값 → `R8D` |
| `+0x16 TEST EAX / +0x18 JLE → +0x46` | **옛 ≤ 0**: `+0x49 JLE` (새 ≤ 0 이면 끝) · 아니면 `+0x4b EAX = 1` → `+0x50 JMP → +0x24` → **`+0x3b NtSetInformationWorkerFactory([풀+0x38], 3, &1, 4)`** (`+0x37 LEA EDX,[R9-1]` = 3, `+0x2d R9D = 4`) |
| `+0x1a TEST R8D / +0x1d JG → 끝` | **옛 > 0 이고 새 > 0 → syscall 없음** |
| `+0x1f EAX = -1` → 같은 syscall | 옛 > 0 → 새 ≤ 0 (마지막 묶임이 풀릴 때) 만 `-1` |

- **안 나오는 조건**: 같은 풀 묶음 수 `[풀+0x1b0]` 가 이미 1 이상. 풀은 `HEAP_ZERO_MEMORY` (`TpAllocPoolInternal +0xa3 OR EDX,8`) 로 0 에서 시작하니 **그 풀의 첫 묶임에 한 번만** 나온다. 묶이는 것: ALPC 완료 (`TppAllocAlpcCompletion +0x1e4` +1 · `TppAlpcpFree +0x20` −1) · 파일 IO (`TpBindFileToDirect` · `TppIopFree`) · 잡 알림 (`TpAllocJobNotification` · `TppJobpFree`) · `RtlpTpIoDllProcessUnloads` (결과서 — `TpAdjustBindingCount.c` 머리).
- 에뮬레이터 로그에 이 줄이 없는 까닭 후보 `[추측]`: (a) 같은 프로세스 앞쪽 묶임 (ALPC · 파일 IO · 잡) 때 이미 한 번 나왔다 — 로그 앞쪽에서 `(…, 3, …)` 를 찾아 볼 것 (b) 풀이 0 으로 시작하지 않았다 (힙 0 채움을 안 지킴).

#### 4-2. 종류 3 의 커널 뜻 (ntoskrnl `NtSetInformationWorkerFactory`, run125 5절 표 "C[1] `+0x3ff`" 와 같은 몸)

| 줄 | 뜻 |
|---|---|
| `+0x3d5 ~ +0x3ef` 종류 → 점프 표 → `+0x3ff` | 종류 3 몸 |
| `+0x404 ECX = [공장+0x194]` (옛) · `+0x40a EDX = 입력 (부호 있음)` | **`공장+0x194` = 묶음 수** `[추측: 이름]` |
| `+0x410 JNS → +0x565` 더하기 · `+0x568 / +0x56a JNC → +0x7a8` (넘치면 `0xffffffff`) · `+0x570` 저장 | 올리기 |
| `+0x416 ~ +0x41c JA → +0x85f` 빼기 (`+0x862` 저장), 아니면 `+0x422` **0 으로** | 내리기 (0 밑으로 안 감) |
| `+0x42c TEST ECX / +0x42e JZ → +0x57b`: **옛 0 → 새 ≠ 0** → `+0x582 ExpTryEnterWorkerFactoryAwayMode` 참이면 `+0x59a ExpWorkerFactoryCheckCreate` | 일꾼 만들기 시도 |
| `+0x434 ~ +0x44f`: **옛 ≠ 0 → 새 0** 이고 `[공장+0x198] & 0x200` 이면 `ExpLeaveWorkerFactoryAwayMode` | "멀리 있기" 끝 |

- `ExpTryEnterWorkerFactoryAwayMode`: `[공장+0x180] ≥ [공장+0x17c]` (`+0x13 / +0x19`) 이거나 기다리는 일꾼 `[P+0x1c] ≠ 0` (`+0x1f / +0x23`) 이면 **거짓**. 아니면 **묶음 수 0 이면 참** (`+0x33 ~ +0x3c`). **0 이 아니면** `+0x4a ~ +0x56` bit 9 켬 → 포트 큐 `[포트+4] ≠ 0` 이면 참 (`+0x5c ~ +0x64`) → bit 10 이 이미 켜졌으면 거짓 → 아니면 bit 9 · 10 켜고 **`+0x97 KeRegisterObjectNotification(포트, ExpWorkerFactoryManagerQueue, 공장+0x268)`** 해 두고 **거짓**.
- 그 알림이 오면 관리 스레드 `ExpWorkerFactoryManagerThread +0x98 BTR bit 10 → +0xa0 TEST 0x200 → +0xaf TryEnter (이제 포트가 안 비어 참) → +0xc3 CheckCreate` — **패킷이 들어온 뒤에야 일꾼을 만든다.**
- **결론**: 묶음 수 ≠ 0 = "이 풀의 포트에 **커널이 직접 패킷을 넣는 것** 이 묶여 있다 → 일꾼이 하나도 없을 때 패킷이 오면 일꾼을 만들라". **`0x102` 에 일꾼을 남기는 칸은 아니다**: `[공장+0x194]` 를 읽는 곳은 종류 3 · `ExpTryEnterWorkerFactoryAwayMode +0x33` · `NtWaitForWorkViaWorkerFactory +0x979` (일을 집은 뒤 연쇄, run125) · `+0x588` (`XADD` — ALPC 보내기와 합친 다른 갈래, 이 길 아님 `[공백]`) · `NtQueryInformationWorkerFactory +0x165` 뿐이고, `0x102` 를 정하는 `ExpWorkerFactoryWantsToCreate` 는 안 읽는다 (2-2).

### 5. `NtAlpcQueryInformation(포트, 0, 버퍼, 0x10)` (질문 5) [정적]

`IsPortWithSharedSection` (rva `0x58018`, 83 B):

| 줄 | 하는 일 |
|---|---|
| `+0x8 XORPS / +0xb MOVUPS [RSP+0x30]` | 버퍼 16 B 를 **0 으로 먼저** |
| `+0x10 TEST RCX / +0x13 JZ` | 포트 핸들 0 이면 반환 1 |
| `+0x25 NtAlpcQueryInformation(포트, 0, &버퍼, 0x10, NULL)` | |
| `+0x31 TEST EAX / +0x33 JS → +0x3f` | **실패면 반환 1** |
| `+0x35 TEST [버퍼],0x1000000 / +0x3d JNZ` | **bit 24 켜졌으면 반환 0**, 아니면 `+0x3f EBX = 1` → **반환 1** |

- 결과를 쓰는 곳: `AlpcConnect +0x38d TEST EAX / +0x38f JZ → +0x743` — 반환 0 일 때만 `[연결+0x240] = 0xff00`. 그 뒤 **`+0x395 / +0x39f` 비동기 판정은 반환값과 무관** (보류 + `*출력 = 1`). `IoCompleted +0x6f MOV ECX,0x1000 / +0x76 JZ → +0x6e7` — 반환 0 이면 받기 크기 `0xff00`, 1 이면 `0x1000`.
- **결론**: "0 돌려주고 버퍼 안 씀" = 반환 1 (**공유 섹션 있음, 받기 크기 `0x1000`**) → 비동기 길은 안 바뀐다. 실패를 돌려줘도 같은 1. 갈림은 **받기 버퍼 크기 하나** (`0x1000` / `0xff00`). 진짜 Windows 가 bit 24 를 켜 주는지는 동적 (Q271 7).
- 새로 안 것: **일꾼 (B2) 도 통지마다 `IoCompleted` 맨 앞에서 이 부름을 한 번 더 한다** (`+0x6a`). 다른 부르는 곳 `ValidateCallbackRequest` (서버 쪽) 는 이 길 아님.

### 호출 관계

```
winnsi 주 스레드 (첫 부름)                                                               [정적]
rpcrt4!BindToEpMapper +0x146 RpcBindingBind ⇢ Bind__74b50 (LRPC_FAST_BINDING_HANDLE::Bind)
 │   +0x5c ~ +0x64  비동기 상태 ≠ NULL → R14D = 1   (+0x6a 아니면 0)
 ├─ +0x15c  DriveStateForward(this, EDX = R14D = 1, …)
 │           ├─ +0x8f9 ~ +0x906  IO = {종류 3, 콜백 LrpcBHDriveStateForwardRoutine}
 │           └─ +0x51a  Connect  → AlpcConnect +0x29a NtAlpcConnectPort (Flags 0)   ── SYSCALL
 │                        +0x3b6 EnableAsync → … NtAlpcSetInformation(2)             (run137)
 │                        ← 0xc0021018   +0x51e JNZ → +0x266 반환
 └─ +0x217 ~ +0x212  0xc0021018 → 0 으로   (+0x19a BaseBind 건너뜀)   → winnsi WaitForSingleObject

(커널: 연결 응답 종류 0xb 메시지 + 완료 패킷 {키, 0, 0, 0}  [추측: 커널 안 읽음])

ntdll!TppWorkerThread  (일꾼 W)                                                           [정적]
 ├─ +0x37e  NtWaitForWorkViaWorkerFactory
 ├─ +0x563  TppAlpcpExecuteCallback ⇢ LrpcIoComplete ⇢ 썽크 ⇢ IoCompleted                (run137)
 │           ├─ +0x6f   IsPortWithSharedSection +0x2c NtAlpcQueryInformation(0)          ── SYSCALL
 │           ├─ +0xcf   NtAlpcSendWaitReceivePort  받기만 → 종류 0xb                     ── SYSCALL
 │           └─ +0x3e8  CompleteOnePart  (+0x26e [연결+0x4c] = 4)
 │                       └─ +0x1cc  SignalAllIos +0x121 SignalAsync
 │                                   └─ +0x2f JMP TrySubmitWork(ThreadPoolInvokeCallback, IO)
 │                                        └─ +0x2d  TrySubmitThreadpoolCallback ⇢ ntdll!TpSimpleTryPost
 │                                                   ├─ +0xc6   TppWorkInitialize
 │                                                   └─ +0x10c  TppWorkPost   자기 몫 (+0x44c ~ +0x474) → syscall 없음
 │                                                               (아니면 +0x4f8 NtReleaseWorkerFactoryWorker)
 ├─ +0x702  TppWorkerFindTask        (+0x65f ~ +0x68d 줄 카운터 ≠ 0)
 └─ +0x819  TppSimplepExecuteCallback ⇢ ThreadPoolInvokeCallback
             └─ +0x16  InvokeCallback ⇢ LrpcBaseProviderComplete +0x49 Complete +0x11e InvokeCallback
                        ⇢ LrpcBHDriveStateForwardRoutine
                           └─ +0x61  DriveStateForwardAsyncCallback
                                      ├─ +0xb5  DriveStateForward(this, 1, 0, 호출)   둘째 부름
                                      │          ├─ +0x58e  EnableAsyncIfNecessary   (깃발만)
                                      │          ├─ +0x5d4  JZ +0x43  / +0x622 EqualSid   (8 → 9)
                                      │          └─ +0x362  OpenSecurityContextWorker
                                      │                      ├─ +0x59   NtOpenThreadToken                ── SYSCALL
                                      │                      ├─ +0xbb   NtSetInformationThread(-2, 5)    ── SYSCALL
                                      │                      ├─ +0x12b  NtAlpcCreateSecurityContext      ── SYSCALL
                                      │                      └─ +0x152  RestoreOldThreadToken +0x46 NtSetInformationThread(-2, 5)
                                      ├─ +0xe5  ⇢ Bind__93cc0 +0x35 BaseBind +0x1e9 LRPC_CASSOCIATION::Bind
                                      │                                  └─ +0x62d NtAlpcSendWaitReceivePort  bind ── SYSCALL
                                      └─ +0x109 ⇢ CallBound(호출, 1)
```

- (나무 오프셋은 ret 자리. `⇢` 는 간접 · 가상 호출 · 꼬리 점프 · 가져오기. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` `DriveStateForward +0x5d4 JZ` = `+0x43` (같은 함수 맨 앞). `[R13]` = `[바인딩+0x118]` (요구 서버 SID 포인터). 이 함수의 callee 에 `Bind` · `Tp*` · `TrySubmit*` 없음.
- `[정적]` 비동기 첫 연결은 `+0x515 Connect` 보류 → `+0x51e JNZ → +0x266` 로 끝나고, `Bind__74b50` 가 보류를 0 으로 바꿔 `BaseBind` 를 건너뛴다. 상태 8 몸은 둘째 부름에서 처음 돈다.
- `[정적]` `fAsync` 는 `Bind__74b50` 의 둘째 인자 (비동기 상태) 가 NULL 이 아니면 1 (`+0x5c ~ +0x64`, `+0x151`) — run133 의 "`DriveStateForward` 인자 전달 `[공백]`" 이 닫힘.
- `[정적]` 맡김 = `SignalAsync +0x2f` → `TrySubmitWork +0x26` → `TrySubmitThreadpoolCallback` → `ntdll!TpSimpleTryPost +0x107 TppWorkPost`. Tp 콜백은 `ThreadPoolInvokeCallback` (`0x98830`) 이고 IO 콜백 칸을 두 번 거쳐 `LrpcBHDriveStateForwardRoutine` → `DriveStateForwardAsyncCallback` 에 닿는다.
- `[정적]` `TppWorkPost` 에 `NtReleaseWorkerFactoryWorker` 를 건너뛰는 갈래 둘 (자기 몫 `+0x44c ~ +0x474` · 깨워 둔 일꾼 `+0x498`).
- `[정적]` `TppWorkerThread` 는 `0x102` 면 유저 큐를 안 보고 끝남 (`+0xc1b → … → +0xfce RtlExitUserThread`), 콜백 뒤에는 줄 카운터를 보고 `TppWorkerFindTask`.
- `[정적]` bind (`LRPC_CASSOCIATION::Bind +0x626`) 는 `DriveStateForwardAsyncCallback +0xe0` 가상 호출 → `Bind__93cc0` → `BaseBind` 로 일꾼이 보낸다. 바로 앞에 `OpenSecurityContextWorker` 4 syscall (동기와 같은 차례).
- `[정적]` `TpAdjustBindingCount` 는 묶음 수가 0 ↔ 양수로 바뀔 때만 syscall. 커널 종류 3 = `공장+0x194` 묶음 수, "멀리 있기" 판정에만 쓰이고 `0x102` 판정엔 안 쓰임.
- `[정적]` `IsPortWithSharedSection` 반환은 받기 크기만 가른다. 일꾼 `IoCompleted +0x6a` 도 같은 묻기를 한다.

## 정정 (요청서 · 원문의 짐작이 틀린 곳)

결과서 "정정" 절 그대로 — 앞 run 이 틀린 게 아니라 **요청서 · 원문 짐작** 이 틀렸다:

1. **"`+0x5d4` 에서 Tp 로 맡기나" → 안 맡긴다.** `+0x5d4` 는 같은 함수 맨 앞 (`+0x43`) 으로 도는 줄이고, `DriveStateForward` 에는 `Tp*` · `TrySubmit*` · `Bind` 부름이 없다. 맡김은 연결 응답을 처리하는 **`SignalAsync`** 쪽이다.
2. **"비동기 bind 를 같은 스레드가 보내나" → 아니오, 풀 일꾼이 보낸다** (첫 연결 때). 부른 쪽 스레드는 보류를 0 으로 받고 이벤트에서 잔다.
3. **"`LrpcBHDriveStateForwardRoutine` 이 Tp 콜백" → 아니다.** Tp 가 부르는 콜백은 `ThreadPoolInvokeCallback` (rva `0x98830`, 34 B) 이고, 그것이 IO 콜백 칸 (`InvokeCallback`) 을 두 번 거쳐 `LrpcBHDriveStateForwardRoutine` 에 닿는다.
4. **"비동기 손님은 보안 문맥 줄이 없다"** → 비동기가 안 하는 게 아니라 **아직 못 간 것** (연결 응답이 없어서).

### 고쳐 읽기 (앞 run)

이번에 닫히거나 좁혀진 앞 run 의 `[공백]` · `[추측]`. 각 문서 맨 위에 "고쳐 읽기 (run141)" 한 줄을 넣었다.

- **run133** — "`DriveStateForward` 의 인자 전달을 안 읽음 `[공백]` (결과서 판정 ③)" → **닫힘** `[정적]`: `Bind__74b50 +0x5c ~ +0x64` (비동기 상태 ≠ NULL → 1) → `+0x151 EDX` → `DriveStateForward` `fAsync`. 그래서 "bind 둘 (epmapper · nsi) 이 비동기로 불림 `[추측]`" 중 **epmapper bind 는 `[정적]`** (일꾼에서 `LRPC_CASSOCIATION::Bind +0x626` Flags 0). nsi bind 는 이번에 안 따라감 — 그대로 `[추측]`. 동적 1 (Flags 확인) 은 그대로 남되 "보내는 스레드 = 일꾼" 도 함께 볼 것 (Q271 3).
- **run129** — "`RpcBindingBind +0x4b` 가상 `[RAX+0x128]` 구현 이름 `[추측]`" → `LRPC_FAST_BINDING_HANDLE::Bind` (`Bind__74b50`, 가상표 `0xe4440` 슬롯 `+0x128`) `[정적: PE]`. epmapper 핸들이 빠른 핸들이라는 건 `[추측]` 으로 남음.
- **run137** — 5절 "서버 흉내 차례" 표가 ①c (붙이기) 바로 뒤에 ③ (epmapper bind) 를 놓았는데, **그 사이에 연결 응답이 빠져 있었다**: 서버 수락 → 클라이언트 포트에 종류 `0xb` 메시지 + 완료 패킷 → 일꾼 `IoCompleted` (+ `NtAlpcQueryInformation(0)` 한 번 더) → `CompleteOnePart` → 맡김 → 보안 문맥 4 줄 → ③ bind. ③ 의 "보내는 쪽" 은 부른 쪽이 아니라 **일꾼** 이다. 또 1-1 의 `IsPortWithSharedSection` 부름은 `AlpcConnect +0x388` 말고 **`IoCompleted +0x6a`** 에도 있다. (①d 첫 묶임 조건은 run137 그대로 맞음.)

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 로그 줄 (에뮬레이터) | 판단 | 근거 · 할 것 |
|---|---|---|
| seq 2185 `NtCreateEvent` | **맞음** | winnsi `+0x168` (run129) |
| 2191 `NtAlpcConnectPort(Flags 0, SID 있음, 들어오는 속성 있음)` | **맞음** | `AlpcConnect +0x1f8 ~ +0x211` · `+0x283 [RSP+0x28] = [연결+0xc0]` · `+0x248 ~ +0x24f` |
| 2192 `NtAlpcQueryInformation(0)` → 0, 버퍼 안 씀 | **맞음, 길 안 바뀜** | 5절 — 반환 1 (`0x1000`). **일꾼에서 한 번 더 나온다.** |
| 2193 `NtAlpcSetInformation(포트, 2, 16 B, 0x10)` | **맞음** | `Connect +0x3b1` (run137) |
| 맡김 쪽 syscall 0 · `NtSetIoCompletion*` 0 | **`NtSetIoCompletion*` 은 맞음** (이 길에 없음). `NtReleaseWorkerFactoryWorker` 는 맡김 자리에서 **0 ~ 1 번** | 2절 ② · ②′ — 에뮬레이터는 둘 다 받을 준비 |
| `NtSetInformationWorkerFactory(…, 3, …)` 없음 | **조건부로 맞음** | 4-1 — 풀 첫 묶임 때만. 로그 앞쪽에 있었는지 확인 |
| 비동기 손님에 보안 문맥 줄 없음 | **지금은 맞고, 고치면 나와야 함** | B7 — 연결 응답을 받은 일꾼에서 4 줄 |
| **연결 응답 · 완료 패킷 없음** | **틀림 (빠진 것) — 멈춤의 직접 원인** | 비동기 `NtAlpcConnectPort` 는 곧바로 0 만 돌려주고 (`AlpcConnect +0x379 ~ +0x381`) rpcrt4 는 **서버 수락이 메시지로 오기를 기다린다** `[정적: 받는 쪽만, 커널 쪽 추측]`: 클라이언트 포트 큐에 종류 `0xb` 메시지 1 + 완료 패킷 1 `{키 = SetInformation(2) 로 받은 키, 0, 0, 0}`. 붙이기 전에 쌓인 메시지는 붙이는 순간 개수만큼 패킷이 나간다 (run137 1-3 의 11) → **큐에 먼저 넣어 두면 둘 다 맞는다** |
| 일꾼 `NtWaitForWorkViaWorkerFactory(0x64) → 0x102 → NtTerminateThread` | **틀림** | 진짜는 시간 제한 없이 잔다 (run125). `0x102` 는 놀기 제한 (파일 값 약 67 초) 이 지나고 `WantsToCreate` 거짓 · 움직이는 수 > 최소일 때만 (2-2). 빈 포트에서 바로 `0x102` 를 주는 것이 일꾼이 다 죽는 직접 원인 |
| 부른 쪽 `NtWaitForSingleObject(0x220, 기한 없음)` | **맞음** | 진짜도 bind · 요청 · 응답이 끝날 때까지 여기서 기다린다 (winnsi `+0x239`) |

할 것 (결과서 요약):

- **`epm.cpp accepts_async_connect`**: 비동기 연결 (Flags 에 `0x20000` 없음) 을 받으면 포트 핸들 · 0 을 돌려주고 **클라이언트 포트 수신 큐에 연결 응답 메시지 하나** (PORT_MESSAGE 헤더 `0x28` B, `[+0x04] & 0xff == 0xb`, 나머지 0 으로 충분 — `IoCompleted` 는 이 종류에서 길이 · 본문을 안 봄, 받기 속성 0 으로 충분 `+0x3bb AND R10D,0x50000000`). 진짜 커널이 넣는 헤더 칸 값은 `[공백]` (Q272 1).
- **`src/threadpool/` 완료 포트**: `NtAlpcSetInformation(2)` 가 포트에 (완료 포트, 키) 를 달고, 이미 큐에 있는 메시지 수만큼 (이후 들어올 때마다) 풀 완료 포트에 `{키, 0, 0, 0}`. 일꾼 `NtWaitForWorkViaWorkerFactory` 는 패킷이 올 때까지 **잔다** (빈 포트에서 바로 `0x102` 금지). 일꾼이 하나도 없는데 패킷이 오고 묶음 수 ≠ 0 이면 일꾼 하나를 만든다 (4-2).
- **일꾼 쪽 syscall 차례** (연결 응답 뒤): `NtWaitForWorkViaWorkerFactory` (0, 항목 1) → `NtAlpcQueryInformation(0)` → `NtAlpcSendWaitReceivePort` (받기만, 종류 `0xb`) → (맡김: 없음 또는 `NtReleaseWorkerFactoryWorker` 1) → `NtOpenThreadToken` → `NtSetInformationThread(-2, 5)` → `NtAlpcCreateSecurityContext` → `NtSetInformationThread(-2, 5)` → **`NtAlpcSendWaitReceivePort(Flags 0, bind 0x70 B, 받기 NULL)`**. 그 뒤는 run137 5절 ③ ~ ⑨.
- **`NtSetInformationWorkerFactory` 종류 3** (4 B, 부호 있음) = `[공장+0x194]` 에 더하기 (0 아래로 안 감, 넘치면 `0xffffffff`). 옛 0 → 새 ≠ 0 이면 "멀리 있기" 판정, 옛 ≠ 0 → 새 0 이면 해제.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 8 — 대기열 **Q271 (동적: 결과서 동적 멈춤점 — Q258 세션과 합침)**. nsisvc 가 도는 VM + winnsi 등록을 부르는 프로그램, test.exe 는 안 부름, 고르기는 사용자 결정. 유저 멈춤점은 그 프로세스 조건, ret 자리):

1. 첫 부름이 보류로 끝나나 — `rpcrt4!LRPC_BASE_BINDING_HANDLE::DriveStateForward+0x51a` `eax == 0xc0021018` · TID (= 부른 쪽).
2. 연결 응답의 진짜 꼴 — `rpcrt4!LRPC_CASSOCIATION::IoCompleted+0x184` (`esi == 0xb`) 에서 `db @rbx L0x28` (`TotalLength` · `DataLength` · `Type` · `ClientId` · `MessageId` · `CallbackId`), `[rbp-0x55]` (받기 속성 유효 비트).
3. 일꾼 TID 와 자기 몫 — `rpcrt4!LRPC_CLIENT_IO::SignalAsync+0x2f` TID · `ntdll!TppWorkPost+0x4cd` `r8d` (1 = 자기 몫) · `+0x4f3` 이 걸리나 · `rpcrt4!…::DriveStateForwardAsyncCallback+0xb0` TID · `+0xe0` · `rpcrt4+0x4e330+0x626` (bind, 같은 TID 인지).
4. `+0x5d4` 가 탄 쪽 — `DriveStateForward+0x5cf` 의 `[r13]` · `+0x622` (`EqualSid` 반환) · 둘째 부름 `+0x69 ~ +0x82` 의 `[호출+0x2d0] / [+0x2d4]`.
5. 보안 문맥 갈림 — `OpenSecurityContextWorker+0x59` `eax` · `+0x89` `r15` (`-3` / `-2` / 핸들) · `DriveStateForward+0x327` `edx` (`[바인딩+0x128]` 1 이면 4 줄 통째로 빠짐).
6. 첫 묶임 — `ntdll!TpAdjustBindingCount+0x16` `eax` (옛 묶음 수). run137 동적 3 (Q258 3) 과 같은 자리.
7. 버퍼 플래그 — `rpcrt4!IsPortWithSharedSection+0x35` `dd @rsp+0x30 L1` (bit 24). run137 동적 7 (Q258 7) 과 같은 자리.
8. `TrySubmitThreadpoolCallback` 이 kernelbase 인지 kernel32 인지 — `rpcrt4!RPC_THREAD_POOL::TrySubmitWork+0x26` 대상. 어느 쪽이든 `ntdll!TpSimpleTryPost`.

정적 (결과서 "다음 요청 후보" 1 ~ 4 — 대기열 **Q272 (정적: 결과서 다음 요청 후보 1~4 — 커널 비동기 NtAlpcConnectPort 안쪽·연결 응답 0xb·`[…+0x80]=3` 쓰는 줄·`TppWorkerFindTask`·`[바인딩+0x128/+0x180/+0x12c]`·`CompleteOnePart` 거절 갈래)**):

1. ntoskrnl 비동기 `NtAlpcConnectPort` 안쪽 (`AlpcpConnectPort` · `AlpcpProcessConnectionRequest` · `AlpcpDispatchConnectionRequest` · `AlpcpReceiveLegacyConnectionReply` · `NtAlpcAcceptConnectPort`) — 서버 수락 뒤 클라이언트 큐에 들어가는 종류 `0xb` 메시지의 헤더 칸 · 붙이기 앞 / 뒤 어느 쪽이든 패킷이 하나만 나가는지.
2. ntdll `[…+0x80] = 3` 을 쓰는 줄 (자기 몫 상태) · 그 `[[TEB+0x1778]+0]` 을 채우는 줄 (아래 대조) · `TppWorkerFindTask` · `TppPrepareDirectParams` (asm 만 뽑아 둠) 의 풀어주기 패킷 처리.
3. rpcrt4 `BindToEpMapper → RpcBindingCreateW` 가 채우는 `[바인딩+0x128]` · `+0x180` · `+0x12c` 값 (epmapper 바인딩의 보안 문맥 갈림 확정) — 3절 `[공백]`.
4. `CompleteOnePart` 실패 갈래 (연결 거절 `0xc0000041` → 오류 `0xe`) 와 `AbortCall` 뒤 winnsi 쪽 결과.

- **Q259 ③ 일부 닫힘** — "`NtWaitForWorkViaWorkerFactory` 커널 쪽 ↔ 기본 풀" 중 종류 3 (묶음 수) 의 뜻 · `0x102` 를 정하는 칸 (`ExpWorkerFactoryWantsToCreate`, 묶음 수 안 읽음) 은 이번에 닫힘. `TppPoolpReferenceGlobalPool` 과 run137 1-1 접점은 그대로.
- 그 밖 `[공백]` / `[추측]`: epmapper 바인딩이 빠른 핸들이라는 것 (간접), `[바인딩+0x128]` · `+0x180` 실제 값, 풀 `[+8]` 칸 이름들, 커널이 넣는 연결 응답 헤더 칸, `[…+0x80] = 3` 을 쓰는 줄, `NtWaitForWorkViaWorkerFactory +0x588` 갈래, nsi bind 의 비동기 여부.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- `DriveStateForward +0x8f1 ReinitCommon (ret +0x8f9)` — `+0x8f1` 은 `+0x4c0 JNZ` 가 가는 자리 (`MOV RCX,RBX`) 이고 **call 은 `+0x8f4`** (ret `+0x8f9`). 다른 두 자리도 call 은 `+0x90f` (ret `+0x914`) · `+0x946` (ret `+0x94b`).
- `TppWorkPost +0x44c ~ +0x474` 의 "`[TEB+0x1778]` → `[…+0x30]`" 는 **한 번 더 따라간다**: `+0x44c RAX = [TEB+0x1778]` · `+0x453 / +0x456` 0 이면 건너뜀 · **`+0x458 MOV RAX,[RAX]`** · `+0x45b CMP [RAX+0x30],RDI` · `+0x461 CMP [RAX+0x80],3` · `+0x46a` 4. 즉 칸은 `[[TEB+0x1778]+0]` 이 가리키는 것의 `+0x30` · `+0x80` 이다. `TppAllocThreadData` 는 0x88 B 기록을 0 채움 (`+0x35 OR EDX,8`) 으로 만들어 `[TEB+0x1778]` 에 달고 (`+0x6e`) `+0x8 |= 3` (`+0x49`) · `+0x18` (`+0x6a`) 만 쓴다 — **`+0` 은 안 쓴다.** 그래서 결과서 "이 `+0x8` 과 `[…+0x80]` 은 다른 칸" 은 맞고, 더 정확히는 **다른 객체** 다. `+0` 을 채우는 줄도 `[공백]` (Q272 2 에 얹음).
- (보탬) `fAsync` 출처: `Bind__74b50 +0x34 MOV R13,RDX` (둘째 인자 = 비동기 상태) · `+0x5c MOV EAX,1` · `+0x61 TEST R13,R13` · `+0x64 JNZ → +0x2c3 MOV R14D,EAX` (아니면 `+0x6a XOR R14D,R14D`) → `+0x151 MOV EDX,R14D` → `+0x157`. 결과서 A2 의 "`EDX = 1`" 이 코드 줄로 보인다.
- `NtWaitForWorkViaWorkerFactory +0x588` 은 `XADD.LOCK [RAX+0x194],EDI` — 결과서 4-2 "`[공장+0x194]` 를 읽는 곳" 목록에 있지만 읽기 + 더하기 (쓰기) 다. 이 길 아님이라 뜻은 그대로.
- `ExpWorkerFactoryWantsToCreate` 첫 줄 범위 `+0x0 ~ +0xa` 는 `+0x3 CMP [RCX+0x190],R9D / +0xa JNZ` 가 실제 비교 (`+0x0` 은 `XOR R9D,R9D`).
- kernelbase · kernel32 `TrySubmitThreadpoolCallback` 바이트는 결과서가 파일로 안 남겨 flow-writer 는 다시 못 봤다 (rpcrt4 쪽 가져오기 줄 `TrySubmitWork +0x26 → TrySubmitThreadpoolCallback` 만 확인).
- 그 밖 결과서 오프셋 · 점프 목적지는 `.asm` 과 **일치** (위 "방법" 의 줄 전부).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- rpcrt4 는 같은 이름 함수가 여럿이라 `.asm` 이 `Bind__<rva>` · `CompleteOnePart__<rva>` 로 갈리고 `_calls.tsv` 의 `Bind` · `CompleteOnePart` 줄이 섞인다 — 결과서 · 이 문서는 `.asm` 줄로 맞춤 (`CompleteOnePart.asm` = rva `0x3b148`, 꼬리표 없는 파일). 기록만, 하네스 제안 아님.
- `ms/disassemble/` 을 같은 시간에 decompiler (sast/141 — gdiplus · gdi32 · user32) 가 쓰고 있었다. flow-writer 가 읽은 rpcrt4 · ntdll · ntoskrnl `.asm` 은 sast/138 이 남긴 것 그대로였다 (결과서 줄과 일치).
