# 138 async-bind — 정적 분석 결과

- 요청서: `_workspace/sast/138_async-bind.md` (원문 `../PE_emulator/_workspace/00_planner/AS02_internals-async-bind.md`)
- 날짜: 2026-11-25, 26100.9457 x64. **정적만** (VM 안 함). 모듈 셋 모두 **이미 analyze 됨** → 다시 analyze 안 함, `^이름$` export 만.
- 스크립트 문제: **없음.** (참고: rpcrt4 는 같은 이름 함수가 여럿이라 `.asm` 이 `__<rva>` 로 갈리고 `_calls.tsv` 의 `Bind` · `CompleteOnePart` 줄은 섞여 있다 — 아래는 `.asm` 줄로 다시 맞췄다.)

| 모듈 | PDB guid+age | 비고 |
|---|---|---|
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 기준 줄과 같음 (`ms/sym/rpcrt4.dll.pdb.info.txt`) |
| ntdll | `C3093720177DA6851519DCFFE8DF53FE1` | 같음 |
| ntoskrnl | `C29EBFB06B78B3C020DCA66D99713F9E1` | **기준 GUID 와 같음** (빌드 맞음) |

- 새로 뽑은 것 (rpcrt4, depth 0~1): `OpenSecurityContextWorker`(+callee 3) · `SignalAllIos` · `SignalAsync` · `SignalSync` · `Complete` · `LRPC_BASE_MINI_THUNK` · `LrpcCreateBindingHandle` · `TrySubmitWork` · `ThreadPoolInvokeCallback` · `InvokeCallback` · `RestoreOldThreadToken` · `Bind`(4개 다시). ntdll: `TpSimpleTryPost` (depth 1, 7개) · `TppAllocThreadData` · `TppCritSetThread` · `TppCallbackEpilog` · `TppCallbackCheckThreadAfterCallback` · `TppPrepareDirectParams`. 나머지 (`DriveStateForward` · `EnableAsyncIfNecessary` · `EnableAsync` · `LrpcBHDriveStateForwardRoutine` · `DriveStateForwardAsyncCallback` · `IsPortWithSharedSection` · `Connect` · `AlpcConnect` · `IoCompleted` · `CompleteOnePart` · `TppWorkPost` · `TppWorkerThread` · `TpAdjustBindingCount` · ntoskrnl `NtSetInformationWorkerFactory` · `NtWaitForWorkViaWorkerFactory` · `ExpTryEnterWorkerFactoryAwayMode` · `ExpWorkerFactoryWantsToCreate`) 는 이미 있는 `.asm` 을 다시 읽었다. 커널베이스 · kernel32 의 `TrySubmitThreadpoolCallback` 은 sast/140 이 kernelbase 를 쓰는 중이라 export 안 하고 **바이트만** 읽었다 (`pefile` + `objdump`, 파일로 안 남김).
- **버전 대조 [일치]**: 알려진 사실 표 3줄을 PE 바이트에서 직접 맞춤 — ntdll `LdrpInitializeProcess +0x1bb6` call → `LdrpInitializeGraphRecurse` (ret `+0x1bbb`) · ntdll `LdrpLoadDll +0xfb` call → `LdrpLoadDllInternal` (ret `+0x100`) · ntoskrnl `NtCreateUserProcess +0xc31` call → `PspAllocateProcess` (ret `+0xc36`). 이번 결과서의 핵심 call · jmp · jcc 34개도 같은 방법으로 목적지를 맞췄다 (전부 일치, 아래 "근거 줄 확인").

## 먼저 읽는 한 줄 답

1. **비동기 bind 를 미는 길** — `DriveStateForward +0x5d4 JZ` 는 **함수 맨 앞 `+0x43` (같은 함수의 루프)** 로 간다. Tp 로 맡기는 줄도, `Bind` 를 부르는 줄도 `DriveStateForward` 안에 **없다**. (같은 스레드에서 `LRPC_BIND_CCALL::Bind` 부르나? **아니오.**)
   - 첫 부름은 `+0x515 Connect` 가 보류 `0xc0021018` 을 돌려주는 자리에서 **그대로 돌아간다** (`+0x51e JNZ → +0x266`). 위 `Bind` 자리 (`Bind__74b50 +0x20b`) 가 그것을 **0 으로 바꿔** 부른 쪽에 돌려준다 — bind 는 아직 안 보냈다.
   - `+0x544 ~ +0x5d4` (상태 8) 는 **첫 연결이면 두 번째 부름** (연결 응답이 온 뒤, 일꾼 스레드) 에서 처음 실행된다. (이미 연결된 연결을 재사용하면 첫 부름 안에서 바로 실행된다 — 1-2 끝 설명.)
   - `[R13]` = `[바인딩 핸들 +0x118]` = 바인딩의 **요구 서버 SID 포인터** (`LPC_NORMALIZED_SID` 의 첫 8 B). 0 이면 확인할 SID 없음 → 바로 루프 맨 앞 (8 → 9). ≠ 0 이면 `+0x61b EqualSid` 로 연결 객체 `+0xc0` (첫 연결 때 `Connect` 가 복사해 둔 것) 와 비교 → 같으면 역시 루프 맨 앞.
   - 콜백이 맞나: **예.** `DriveStateForward` 가 호출 IO 칸 (`호출+0x288`) 을 종류 3 + 콜백 `LrpcBHDriveStateForwardRoutine` (rva `0x3b000`) 으로 채운다 (`+0x8f9 ~ +0x906`, `+0x914 ~ +0x921`, `+0x94b ~ +0x958`) → 그 콜백이 `DriveStateForwardAsyncCallback` 을 부른다 (`+0x5c`).
2. **맡김 · 깨움** — 진짜 맡김은 `DriveStateForward` 가 아니라 **`SignalAsync +0x2f`** 에 있다: 연결 응답을 받은 일꾼이 `RPC_THREAD_POOL::TrySubmitWork(ThreadPoolInvokeCallback, IO)` 로 꼬리 점프 → `TrySubmitThreadpoolCallback` → **`ntdll!TpSimpleTryPost`** (→ `TppWorkPost`). `TpPostWork` · `TpSetWait` 은 이 길에 안 나온다.
   - **syscall 없이 맡기는 길이 진짜로 있다** `[정적: 갈래], [추측: 이 길이 그 갈래를 타는지]`: 맡기는 쪽이 같은 풀의 일꾼이고 그 일꾼 기록의 상태가 3 이면 (`[TEB+0x1778]` → `[…+0x30] == 풀` · `[…+0x80] == 3`) `TppWorkPost +0x44c ~ +0x474` 가 "자기 몫" 표시 (3 → 4, `R8D = 1`) 만 하고 `NtReleaseWorkerFactoryWorker` (`+0x4f3`) 를 **건너뛴다** (`+0x4cd TEST R8D / +0x4d0 JNZ`). 또 이미 깨워 둔 일꾼이 있으면 (`+0x498 TEST ECX,0xffff0000` ≠ 0) 같은 결과. 상태 3 을 **쓰는** 줄은 못 찾아서, 연결 응답을 받은 일꾼이 정말 "자기 몫" 인지는 동적으로 확인해야 한다. 아니면 이 자리에서 `NtReleaseWorkerFactoryWorker` 한 번이 나온다. 에뮬레이터 로그의 "맡김 쪽 syscall 0 개" 는 어느 쪽이어도 모순이 아니다 (그 로그엔 맡김 자리까지 못 갔다).
   - **진짜 깨움은 맡김 앞에 있다**: ALPC 연결 응답이 도착해 커널이 완료 포트에 패킷 `{키 = TP_ALPC 포인터, 0, 0, 0}` 을 넣는 것 (run137 2-1). 받는 쪽 (rpcrt4 `IoCompleted` 가 종류 `0xb` 를 연결 응답으로 처리) 은 `[정적]`, 커널이 그 메시지를 넣는 쪽은 `[추측: 커널 안 읽음]`. 에뮬레이터 로그에는 **이 패킷이 없어서** 일꾼이 깨어날 일이 없었다.
3. **보안 문맥** — 비동기도 같은 코드 (`OpenSecurityContextWorker`) 를 **같은 차례** (`NtOpenThreadToken` → `NtSetInformationThread(-2,5)` → `NtAlpcCreateSecurityContext` → `NtSetInformationThread(-2,5)`) 로 하는데, **연결 응답을 받은 일꾼 스레드에서**, bind 를 보내기 직전에 한다 (`DriveStateForward +0x35d`, 상태 8 → 9). 비동기 로그에 이 줄이 없는 건 거기까지 못 갔기 때문. 실패하면 bind 를 **안 보낸다** (`AbortCall` 로 호출을 끝냄).
4. **`NtSetInformationWorkerFactory(공장, 3, &1, 4)`** — ① 풀의 묶음 수 `[풀+0x1b0]` 가 **0 이하에서 양수로 바뀔 때만** 나온다 (그 풀의 첫 묶임). 이미 1 이상이면 안 나온다. ② 종류 3 = **묶음 수** (`공장+0x194` 에 부호 있는 값을 더함). 0 이 아니면 "일꾼이 하나도 안 기다리고 포트가 비었을 때" 공장이 일꾼을 **미리 안 만들고**, 포트에 패킷이 들어올 때 관리 스레드가 만든다. **쉼 시간 끝 (`0x102`) 에 일꾼을 남기는 칸은 아니다** (`공장+0x194` 는 `0x102` 결정에 안 쓰임).
5. **`NtAlpcQueryInformation(0)`** — 버퍼를 안 쓴 성공 (= 앞 4 B 가 0) 은 `IsPortWithSharedSection` 이 **1 ("공유 섹션 있음")** 로 읽는다. 갈림은 **받기 버퍼 크기** (`연결+0x240` 을 `0xff00` 으로 바꾸느냐 · `IoCompleted` 받기 크기 `0x1000` / `0xff00`) 하나뿐이고, 비동기 길 (보류 `0xc0021018`) 은 안 바뀐다. 덤: `IoCompleted +0x6a` 가 **연결 응답을 받는 일꾼에서도 같은 부름을 또 한다.**

## 정정 (요청서 · 원문의 짐작 틀린 곳)

- 요청서 1 의 "`+0x5d4` 에서 Tp 로 맡기나" → **안 맡긴다.** `DriveStateForward` 의 callee 목록 (`ms/decomp/rpcrt4/DriveStateForward.c` 머리) 에 `Tp*` · `TrySubmit*` · `Bind` 가 없다. 맡김은 연결 응답을 처리하는 쪽 (`SignalAsync`) 이다.
- "비동기 손님은 보안 문맥 줄이 없다" 는 **비동기가 안 하는 게 아니라 아직 못 간 것**이다.
- "`LrpcBHDriveStateForwardRoutine` 이 Tp 콜백" → 아니다. Tp 가 부르는 콜백은 `ThreadPoolInvokeCallback` (rva `0x98830`, 34 B) 이고, 그것이 IO 의 콜백 칸 (`InvokeCallback`) 을 두 번 거쳐 `LrpcBHDriveStateForwardRoutine` 에 닿는다.

## 1. 비동기 bind 를 미는 길 [정적]

### 1-1. `DriveStateForward` (rva `0x44e00`) — 칸 · 상태 읽기

`DriveStateForward(this = 바인딩 핸들, fAsync, 제한 시간, 호출)`. `fAsync` 는 `[RSP+0x60]` (= 인자 EDX), 호출 = `R14`. (`rpcrt4/DriveStateForward.asm`)

| 칸 | 뜻 `[추측: 이름]` | 줄 |
|---|---|---|
| `[바인딩+0x114]` | 바인딩 상태 (1 ~ 9 …) | 도처 |
| `[호출+0x2d0]` · `+0x2d4` | "이 상태가 되면 저 상태로" **짝** (옛, 새). 함수 맨 앞이 바인딩 상태 == 옛 이면 새로 바꾼다 | 적용 `+0x69 ~ +0xcb` · 쓰기 `+0x4a8/+0x4b3` (6 → 7) · `+0x5b9/+0x5c4` (8 → 9) |
| `[호출+0x288]` = `LRPC_CLIENT_IO` | 종류 (`[+0]` 1 = 동기, 3 = 콜백형) · 콜백 `[+8]` · 상태 `[+0x38]` | |
| `[바인딩+0x118]` | `LPC_NORMALIZED_SID` — 첫 8 B = **요구 서버 SID 포인터** | `Connect` 인자 `+0x4f2 LEA R8,[RDI+0x118]` → 5번째 인자 |
| `[연결+0xc0]` | 연결이 들고 있는 서버 SID (첫 연결 때 복사) | `Connect +0x12a DuplicateSID → +0x138` |

- 첫 부름 (일꾼 아님, 부른 쪽 스레드) 의 길: 바인딩 상태가 `+0x480 [바인딩+0x114] = 6` 으로 바뀐 뒤 상태 6 몸 `+0x48a ~ +0x52b`.
  - `+0x4a8` · `+0x4b3` 짝 6 → 7 쓰기.
  - `+0x4be TEST ESI / +0x4c0 JNZ` : `ESI = fAsync` ≠ 0 이면 `+0x8f1 ReinitCommon` (ret `+0x8f9`) 뒤 **`+0x8f9 LEA RAX,[0x18003b000]` · `+0x900 [IO]=3` · `+0x906 [IO+8]=RAX`** — 콜백 `LrpcBHDriveStateForwardRoutine`. 동기면 `+0x4cd [IO]=1`.
  - `+0x515 Connect` (ret `+0x51a`). **`+0x51e JNZ → +0x266 (반환)`**: 비동기 첫 연결은 `0xc0021018` 이라 여기서 **함수가 끝난다**. 동기는 0 이라 `+0x524 / +0x52b JMP +0x43` 으로 같은 호출 안에서 루프를 계속 돈다.
- 두 번째 부름 (콜백 뒤): 맨 앞 `+0x69 ~ +0xcb` 가 6 → 7 을 적용 → `case 7` 이 `+0x551 [바인딩+0x114] = 8` → **`caseD_8` (`+0x55b ~ +0x5d4`)**:
  - `+0x55b R15D = fAsync` ; `+0x57d TEST [바인딩+0x178],1 / +0x584 JNZ` (이미 켜졌으면 건너뜀) ; **`+0x589 EnableAsyncIfNecessary` (ret `+0x58e`)**.
  - `+0x5b2 LEA R13,[RDI+0x118]` · `+0x5b9` `[호출+0x2d0] = 8` · `+0x5c4` `[호출+0x2d4] = 9` · **`+0x5cf CMP [R13],0`** · **`+0x5d4 JZ 0x180044e43` = `+0x43`** (함수 맨 앞, `RtlEnterCriticalSection` 다시).
  - `[R13] ≠ 0` : `+0x5da` ~ `+0x607` (비동기면 IO 를 다시 콜백형으로 `+0x90f ~ +0x925` — 같은 `0x18003b000`), `+0x60f MOV RCX,[R15]` (`R15 = 연결+0xc0`) · `+0x615 JZ → +0x799` (연결 SID 가 비어 있으면 `AlpcVerifyServerSid`) · **`+0x61b EqualSid` (ret `+0x622`)** · `+0x629 JNZ → +0x43`. 다르면 `+0x64a PrepareIterationInternal` → `+0xac5 IterateAndVerify`.
  - 맨 앞으로 돌아가면 `+0x69 ~ +0x82` 가 8 → 9 를 적용 → `+0x82 JZ 0x1800450fe (= +0x2fe)` 상태 9 몸 → **`+0x35d OpenSecurityContextWorker`** (ret `+0x362`) → `+0x3c6` 가상 `[바인딩 vtbl+0x1e0]` → `+0x3cb JMP +0x88` → 상태 = 9 → `+0x1f1 CMP EDX,9 / +0x1f4 JZ → +0x239` → 반환 (`+0x266`).
- `EnableAsyncIfNecessary` (`.asm +0x4e`) : 연결의 `[+0xd0] & 8` 가 이미 켜졌으면 (`Connect +0x3b1` 이 첫 연결 때 켬) `EnableAsync` 를 안 부르고 바인딩 깃발 `[+0x178] |= 1` 만 쓴다 (`+0x59`). **syscall 없음.** 이 길에서 새 `CreateAlpc` 도 없다.
- 이 함수가 부르는 것 (callee): `Connect` · `ResolveEndpoint` · `PickAssociation` · `OpenSecurityContextWorker` · `EnableAsyncIfNecessary` · `EqualSid` · `AlpcVerifyServerSid` · `PrepareIterationInternal` · `IterateAndVerify` · `ReinitCommon` · `DuplicateSID` · 로그 · 가상 (`호출 vtbl+0x180`, `바인딩 vtbl+0x1e0`). **`Bind` · `Tp*` 없음.**

### 1-2. 비동기 bind 차례표 (동기 손님 로그 5617 ~ 5623 과 나란히)

(스레드 "부른 쪽" = winnsi 주 스레드, "일꾼" = 풀 일꾼.)

| 차례 | 스레드 | 함수 · 줄 (call / ret) | syscall | 동기 손님은 |
|---|---|---|---|---|
| A1 | 부른 쪽 | winnsi `NsiRpcRegisterChangeNotificationEx +0x168 CreateEventW` (run129) | `NtCreateEvent` (로그 2185) | — |
| A2 | 부른 쪽 | `BindToEpMapper +0x141/+0x146 RpcBindingBind(비동기 상태, 핸들, …)` → 가상 `[+0x128]` = `Bind__74b50` (`LRPC_FAST_BINDING_HANDLE::Bind`) `+0x157/+0x15c DriveStateForward(this, EDX = 1, …)` | — | `EDX = 0` |
| A3 | 부른 쪽 | `DriveStateForward +0x515/+0x51a Connect` (콜백 칸은 `+0x8f9 ~ +0x906`) | — | 같음 (IO 종류 1) |
| A4 | 부른 쪽 | `Connect` (첫 연결): `+0x2c0` `LRPC_BASE_MINI_THUNK` 로 원래 콜백 보관 · `+0x371 ~ +0x37f` IO 콜백을 `LrpcBaseProviderComplete` (`0x96a10`) 로 · `+0xd2` `Register` · `+0x198/+0x19d AlpcConnect` | **`NtAlpcConnectPort(Flags 0, RequiredServerSid = [연결+0xc0], 들어오는 속성 있음)`** (`AlpcConnect +0x293/+0x29a`, 로그 2191) | Flags `0x20000` (5617) |
| A5 | 부른 쪽 | `AlpcConnect +0x388/+0x38d IsPortWithSharedSection` → `+0x395` 비동기면 `+0x752 ~ +0x761` : `*출력 = 1`, 보류 `0xc0021018` | `NtAlpcQueryInformation(포트, 0, 버퍼, 0x10)` (2192) | 5618 |
| A6 | 부른 쪽 | `Connect +0x19f → +0x328 → +0x32d → +0x3a4 → +0x3b1/+0x3b6 EnableAsync` → `CreateAlpc +0x66 TpAllocAlpcCompletionEx` → `TppAllocAlpcCompletion +0x1d1` (run137) → `+0x35f EBX = 0xc0021018` | `NtAlpcSetInformation(포트, 2, {키, 완료포트}, 0x10)` (2193) · 풀의 첫 묶임이면 `NtSetInformationWorkerFactory(공장, 3, &1, 4)` | 없음 (동기는 `EnableAsync` 안 부름) |
| A7 | 부른 쪽 | `DriveStateForward +0x51e JNZ → +0x266` 반환 `0xc0021018` → `Bind__74b50 +0x217/+0x21a/+0x1a4/+0x1a6/+0x20b/+0x210/+0x212` 가 **0 으로 바꿔 반환** (`BaseBind` 건너뜀) → winnsi `+0x239 WaitForSingleObject(이벤트)` | `NtWaitForSingleObject` (2194 뒤) | 같은 호출 안에서 `+0x524/+0x52b JMP → +0x43` 으로 상태 6 → 9 를 돌고 (`Connect` 가 막고 기다림) 이어서 `Bind__74b50 +0x19a BaseBind` |
| — | 커널 | 서버 (epmapper) 가 연결을 받음 → 연결 응답 메시지 · 완료 패킷 (2절 ①) | | |
| B1 | 일꾼 W | `TppWorkerThread +0x379 NtWaitForWorkViaWorkerFactory` 가 0 으로 돌아옴 (항목 `{키, 0, 0, 0}`) → `+0x55e TppAlpcpExecuteCallback` → `+0x449 LrpcIoComplete` → 썽크 `LrpcCAssociationConnectIoHandler` → `IoCompleted` (run137 2-2) | (`NtWaitForWorkViaWorkerFactory` 반환) | — |
| B2 | 일꾼 W | `IoCompleted +0x6a/+0x6f IsPortWithSharedSection` → `+0xc8/+0xcf NtAlpcSendWaitReceivePort` (받기만) | **`NtAlpcQueryInformation(포트, 0, …)`** → **`NtAlpcSendWaitReceivePort(포트, 0, NULL, NULL, 버퍼, &길이, &속성, NULL)`** → 0, 메시지 종류 **`0xb`** | — (동기는 5617 이 곧바로 연결 끝) |
| B3 | 일꾼 W | `IoCompleted +0x13e ~ +0x142` 종류 = `[+4] & 0xff` · **`+0x184 CMP ESI,0xb / +0x187 JZ → +0x3bb`** → `+0x3e3/+0x3e8 CompleteOnePart(연결, 0, 0, NULL, 1)` → `CompleteOnePart +0x26e [연결+0x4c] = 4` (연결됨) · **`+0x1c7/+0x1cc SignalAllIos`** → `+0x11c/+0x121 SignalAsync` (IO 종류 3) | — | `Connect +0x1c5` 가 `CompleteOnePart` 를 바로 부름 (같은 스레드) |
| B4 | 일꾼 W | `SignalAsync` : `[IO+0x38] = 상태 0` (`+0x22`) · 콜백 `ThreadPoolInvokeCallback` (`+0x1b LEA RCX,[0x180098830]`) · **`+0x2f JMP TrySubmitWork`** → `TrySubmitWork +0x26/+0x2d` (`InitializeCallbackEnvironmentIfNecessary +0x10` — 환경 NULL = 전역 풀) → `TrySubmitThreadpoolCallback` → **`KernelBase +0x4/+0xb` (또는 kernel32 `+0x6/+0xd`) → `ntdll!TpSimpleTryPost`** → `+0xc1 TppWorkInitialize` → **`+0x107/+0x10c TppWorkPost`** | **없음** (`TppWorkPost +0x44c ~ +0x474` 자기 몫) | — |
| B5 | 일꾼 W | 콜백 끝난 뒤 `TppWorkerThread +0x65f ~ +0x68d` : `[풀+8]` 위 dword (줄 카운터) ≠ 0 → `+0x845` 에서 1 줄이고 `+0x6ea → +0x6fd TppWorkerFindTask` → `+0x814 TppSimplepExecuteCallback` → `ThreadPoolInvokeCallback +0x11/+0x16 InvokeCallback` → `LrpcBaseProviderComplete +0x44/+0x49 CLIENT_IO_PROVIDER::Complete` → `Complete +0x119/+0x11e InvokeCallback` → **`LrpcBHDriveStateForwardRoutine`** (`+0xf ESI = [IO+0x38]` 상태) → `+0x5c/+0x61 DriveStateForwardAsyncCallback` → 상태 0 이라 `+0x1b JZ → +0xa3` → **`+0xb0/+0xb5 DriveStateForward(this, 1, 0, 호출)`** | 없음 | — |
| B6 | 일꾼 W | `DriveStateForward` 두 번째 부름 (1-1) : 6 → 7 → 8 → (`EnableAsyncIfNecessary` 깃발만) → `[R13] ≠ 0` → `EqualSid` 같음 → 8 → 9 | — | 같은 일을 부른 쪽이 한 호출 안에서 |
| B7 | 일꾼 W | **`+0x35d/+0x362 OpenSecurityContextWorker`** (3절) | `NtOpenThreadToken` · `NtSetInformationThread(-2, 5)` · `NtAlpcCreateSecurityContext(포트, 0, …)` · `NtSetInformationThread(-2, 5)` | 5619 ~ 5622 |
| B8 | 일꾼 W | 반환 0 → **`DriveStateForwardAsyncCallback +0xc3 ~ +0xe0/+0xe5`** 가 호출 가상 `[vtbl+0x188]` = **`Bind__93cc0`** (`LRPC_FAST_BIND_CCALL` 의 가상 Bind) → `+0x30/+0x35 BaseBind` → **`BaseBind +0x1e4/+0x1e9 LRPC_CASSOCIATION::Bind`** → `Bind__4e330 +0x626/+0x62d` | **`NtAlpcSendWaitReceivePort(포트, Flags 0, bind 0x70 B, …, 받기 NULL)`** = **bind** | 5623 (Flags `0x20000`, 같은 버퍼로 답) |
| B9 | 일꾼 W | `DriveStateForwardAsyncCallback +0xf3 ~ +0x104/+0x109` 가상 `[vtbl+0x190]` = `CallBound(호출, 1)` | — | — |
| C | 일꾼 | bind 답 · 요청 · 응답 | run137 ③ ~ ⑨ (다시 안 함) | |

- **bind 를 보내는 스레드 = 풀 일꾼** (연결 응답 패킷을 받은 그 일꾼이 자기 몫으로 이어서 하거나, 자기 몫이 아니면 `NtReleaseWorkerFactoryWorker` 로 깨운 일꾼이 한다). 부른 쪽 스레드는 아니다. `[정적]` 단 첫 연결 때 한정 — 아래 재사용 설명 참고.
- `Complete` (`+0x7f ~ +0xc6`) 는 IO 칸을 보관해 둔 원래 값 (종류 3 · 콜백 `LrpcBHDriveStateForwardRoutine`) 으로 되돌린 뒤 (`+0x93 ~ +0xa4`) `+0x119 InvokeCallback` 한다. 그 사이 `+0x1ae IsRetryErrorCode` (연결 실패 재시도 판정) 갈래는 상태 0 에서 안 타는 것으로 읽었다 `[추측: IsRetryErrorCode 본문은 안 읽음]`.
- 첫 부름에서 `BaseBind` 를 안 가는 근거: `Bind__74b50` 가 `R13D = 0xc0021018` 을 미리 두고 (`+0x145`) `DriveStateForward` 결과와 비교 (`+0x217`) 해 같으면 0 으로 바꿔 끝낸다. `BaseBind` 는 `DriveStateForward` 가 0 을 돌려줬을 때만 (`+0x166 JNZ` 를 안 탈 때, `+0x19a`).
- 이미 연결이 `4` (연결됨) 인 연결을 재사용하는 비동기 바인딩은 `Connect +0x4b CMP 4 / +0x4f JNZ 안 탐 / +0x54 JNZ → +0x388` 이 곧바로 0 을 돌려주므로 `DriveStateForward` 가 **첫 부름에서 0 으로 끝나** `Bind__74b50 +0x19a BaseBind` 로 **부른 쪽 스레드가 bind 를 보낸다.** (winnsi 의 이번 길은 첫 연결이라 해당 없음.) `[정적: 갈래만 읽음, 실제로 타는 쪽은 동적]`
- `Bind__74b50` 이 `LRPC_FAST_BINDING_HANDLE::Bind` 인 근거: PDB 공개 심볼 `??_7LRPC_FAST_BINDING_HANDLE@@6B@` (`.rdata` rva `0xe4440`) 의 가상 슬롯 `+0x128` = `0x74b50`. `LRPC_BINDING_HANDLE` (비빠른) 의 같은 슬롯은 6 B 짜리 기본 몸 (`BINDING_HANDLE::Bind`, rva `0xadd70`) 이라 `RpcBindingBind` 는 빠른 핸들에서만 의미가 있다 `[추측: epmapper 핸들이 빠른 핸들]`. 두 슬롯 (`0x188` · `0x190`) 은 `LRPC_FAST_BIND_CCALL` 가상표 (`.rdata` rva `0xe2590`) 에서 읽었다.

## 2. 맡김 → 깨움 [정적]

### 2-1. 표

| 무엇이 | 누가 | syscall | 항목 · 패킷 꼴 | 줄 |
|---|---|---|---|---|
| **① 일꾼 W 를 깨움 (연결 응답)** | **커널** (메시지가 클라이언트 포트 큐에 들어갈 때 `AlpcpQueueIoCompletionPort` — run137 2-1 은 보내기 · 답 · 닫기 · 연결 요청 처리의 끝까지 읽음. **연결 응답이 정확히 이 길로 가는지는 커널 안 읽음 `[추측]`**) — 유저 코드의 syscall 아님 | (유저 쪽 없음) | `{KeyContext = TP_ALPC 포인터, ApcContext 0, IoStatus 0, Information 0}` · 일꾼은 `NtWaitForWorkViaWorkerFactory` 항목 0x20 B 로 받음 | run137 2-1 · 2-2; `TppWorkerThread +0x379` |
| **② 맡기기 (`TpSimpleTryPost`)** | 같은 일꾼 W (콜백 도는 중) | **없음** | 풀 일감 줄에 한 항목 + `[풀+8]` 위 dword `+1` (`CMPXCHG`) | `TppWorkPost +0x44c ~ +0x474` (`[TEB+0x1778] = ThreadPoolData` → `[…+0x30] == 풀` · `[…+0x80] 3 → 4` · `R8D = 1`), `+0x47e ~ +0x4a8` (카운터 `+1`), `+0x4cd TEST R8D / +0x4d0 JNZ → +0x4a0` (release 건너뜀) |
| ②′ 다른 스레드가 맡길 때 (이 길 아님) | 맡기는 스레드 | **`NtReleaseWorkerFactoryWorker([풀+0x38])`** | 커널이 "네 칸 모두 0" 항목을 포트에 넣음 (run125) | `TppWorkPost +0x498 TEST ECX,0xffff0000 / +0x49e JZ → +0x4cd` (이미 깨워 둔 일꾼 수 `[풀+8]` 하위 dword 의 위 16 b ≠ 0 이면 **건너뜀**), 둘 다 아니면 `+0x4d2 ~ +0x4ed` 에서 그 16 b `+1` · `+0x4b7 JNZ → +0x4ef` → **`+0x4f3/+0x4f8`** |
| ③ 일꾼이 일을 집음 | W | 없음 | — | `TppWorkerThread +0x65f ~ +0x6c4` · `+0x845` · `+0x6fd TppWorkerFindTask` |

- 이름 `[추측]`: `[풀+8]` 하위 dword 의 위 16 b = "깨워 둔 일꾼 수", 아래 16 b = "쉬는 일꾼 수 (`TppWorkerThread +0x696 MOVSX ECX,AX / +0x699 INC ECX`)", 위 dword = "줄에 쌓인 일감 수". 칸 이름은 PDB 에 없고 쓰는 줄에서만 읽었다. `[TEB+0x1778]` 은 PDB `_TEB.ThreadPoolData`. `[…+0x80] = 3` 을 **쓰는** 줄은 이번에 못 찾았다 (`TppWorkPost` 안의 `3 → 4` 만 확인; `TppAllocThreadData` 는 `[TEB+0x1778]` 에 0x88 B 기록을 달고 `+0x8 |= 3` 만 함 — 이 `+0x8` 과 `[…+0x80]` 은 **다른 칸**) `[공백]`. 비교가 거짓이고 깨워 둔 일꾼도 0 이면 (`+0x498 JZ → +0x4cd`, `R8D = 0`) **`NtReleaseWorkerFactoryWorker` 가 한 번 나온다** (②′ 줄) — 그 경우 에뮬레이터는 이 syscall 도 받아야 한다 (run125 몸).
- 에뮬레이터 로그의 "맡김 줄 syscall 0 개" 와 **모순 없다**: 그 로그는 맡김 자리 (연결 응답 처리) 까지 못 갔다. 맡김 자리에서 syscall 이 0 개일지 1 개 (`NtReleaseWorkerFactoryWorker`) 일지는 동적 확인 3번.

### 2-2. 일꾼이 NtWait 에서 돌아온 뒤 (`TppWorkerThread`, rva `0x1a2e0`) — 유저 큐 보는 줄과 끝나는 줄

| 상황 | 줄 | 하는 일 |
|---|---|---|
| 상태 0, 항목 있음 (키 ≠ 0) | `+0x37e ~ +0x402 JNZ 안 탐` → `+0x46e TppPrepareDirectParams` → `+0x485 ~ +0x490` → `+0x536 ~ +0x55e` 키의 `[+0x38]` 로 가름 (`TppAlpcpExecuteCallback` `0x19aa0` · `TppIopExecuteCallback` · `TppWaitCompletion`) | 콜백 실행 |
| 콜백 뒤 | `+0x5eb TppCallbackEpilog` → **`+0x65f ~ +0x68d`**: `[풀+8]` 읽고 위 dword ≠ 0 이면 `+0x845` 로 가서 `-1` (`+0x851 DEC ECX` · `+0x85a JMP → +0x6ae` → `+0x6b6 CMPXCHG.LOCK`) → `+0x6c6 R8B = 0` → **`+0x6ea → +0x6fd TppWorkerFindTask`** (유저 큐에서 일감 집기). 0 이면 쉬는 수 `+1` 하고 **`+0x6e5 JMP +0x2ce`** 로 다시 `NtWaitForWorkViaWorkerFactory` | **유저 큐 확인 = 이 줄들** |
| 상태 0, 항목 없음 (`[RSP+0x298]` = 0, 풀어주기 패킷) | `+0x485 ~ +0x490 JZ → +0x6ea` | 곧바로 `TppWorkerFindTask` |
| 상태 `0x80` (마감) | `+0xc03 SUB ECX,0x80 / +0xc09 JZ → +0xe13` | 끝내기 길 |
| 상태 `0xc0` (사용자 APC) | `+0xc0f SUB ECX,0x40 / +0xc12 JZ → +0xdc0` → `TppCallbackCheckThreadAfterCallback` → `+0xdff JMP +0x2e9` 다시 기다림 | 안 끝남 |
| **상태 `0x102` (쉼 시간 끝)** | **`+0xc18 CMP ECX,0x42 / +0xc1b JZ → +0xda4`**: `[RSP+0x46] = 1` (`+0xda4`) → `+0xda9 JMP +0xe13` → `+0xec4 TppPoolRemoveWorker` · `+0xed3 TppPoolUpdateTrimmedWorker` · `+0xf58 TppCritResetThread` · `+0xf65 TppFreeThreadData` → **`+0xfce RtlExitUserThread`** | **유저 큐를 안 보고 끝난다** |

- "유저 큐에 일이 있으면 안 끝나나" → 유저 쪽에서는 `0x102` 를 받으면 **큐를 안 보고 끝난다.** 일이 있을 때 `0x102` 를 **안 받게 하는 것은 커널**: `ExpWorkerFactoryWantsToCreate(공장, 1)` 이 참이면 일꾼이 쉼 시간 끝에도 안 줄여진다 (run125 `0x102` ②). 참 조건 줄: `+0x0 ~ +0xa` (`[공장+0x190] ≠ 0` 이면 거짓) · `+0x12 ~ +0x25` (`[공장+0x180] ≥ [공장+0x17c] (+1)` 이면 거짓) · `+0x27 ~ +0x2f` (기다리는 일꾼 `[P+0x1c]` 가 `1` 초과면 거짓) · **`+0x35 ~ +0x3f` 포트 큐 `[포트+4] ≠ 0` 이면 참** · **`+0x43 ~ +0x47` 풀어주기 수 `[P+0x18] > 0` 이면 참**. `NtReleaseWorkerFactoryWorker +0xea ~ +0xfb` 가 `[P+0x18]` 을 올린다.
- 자기 몫 맡김은 `NtRelease` 도 포트 항목도 안 쓰므로 이 보호가 필요 없다 — 맡긴 일꾼이 **콜백 끝나자마자** 카운터를 보고 집기 때문 (`+0x65f ~ +0x68d`).
- 에뮬레이터 로그의 "일꾼 `NtWaitForWorkViaWorkerFactory(0x64) → 0x102 → NtTerminateThread`" 는 진짜와 다르다 (아래 "지금 우리 맞나 · 틀리나").

## 3. 비동기 보안 문맥 [정적]

`OpenSecurityContextWorker` (rva `0x4599c`, 645 B) 는 **동기 · 비동기 인자가 없다.** 동기냐 비동기냐는 "어느 스레드가 부르느냐" 일 뿐이고, 부르는 자리는 `DriveStateForward +0x35d` (상태 8 → 9) 하나다. 인자: `(연결 = [바인딩+0x170], 가장 종류 = [바인딩+0x12c], 유효만 = ([바인딩+0x178] >> 1) & 1, 문맥 = 바인딩+0x130, 토큰값 = [바인딩+0x180])` (`DriveStateForward +0x2fe ~ +0x35d`).

| 갈림 | 조건 | 줄 | 일 · syscall |
|---|---|---|---|
| ① 보안 문맥 만들기 자체 | `[바인딩+0x128] == 1` 이면 **안 만든다** (칸만 채움) `[추측: 이름 — 정체 추적 "정적" 쪽]` | `DriveStateForward +0x327 CMP EDX,1 / +0x32a JNZ → +0x355` · 채움 `+0x32c ~ +0x353` | 없음 |
| ② 현재 스레드 토큰 열기 | 항상 | `OpenSecurityContextWorker +0x39/+0x40 GetCurrentThread` · **`+0x52/+0x59 NtOpenThreadToken(스레드, 접근 0xc, OpenAsSelf 1, &토큰)`** | `NtOpenThreadToken` (로그 5619) |
| ②-a 접근 거부 `0xc0000022` | `+0x5e / +0x63 JZ → +0x27c` | 오류 5, bind 안 감 | — |
| ②-b 토큰 없음 `0xc000007c` | `+0x73 / +0x78 JNZ 안 탐` → `+0x7e [저장 토큰] = 0xfffffffd` ("토큰 없음" 표시, 32비트 값을 0 확장해 넣음 `+0x69`) | 계속 | — |
| ②-c `0xc00000a6` | `+0x1e4 / +0x1e9 JZ → +0x22d` `[저장 토큰] = 0xfffffffe` (익명 표시) | 계속 | — |
| ②-d 성공 | `+0x1eb TEST / +0x1ed JZ → +0x82` (열린 핸들을 저장) | 계속 | — |
| ②-e 그 밖 오류 | `+0x1f3 EBX = 0xe` → `+0x1f8 ~ +0x21d` 오류 기록 | bind 안 감 | — |
| ③ 가장 토큰 지정 | 토큰값 `R15` = `0xfffffffd` → 토큰 **없음 (NULL 핸들)** 으로 (`+0x89 / +0x8c / +0x92 / +0x95`) · `0xfffffffe` → **`NtImpersonateAnonymousToken`** (`+0x1ab / +0x1ae / +0x1b4 ~ +0x1c3`) · 그 밖 → 그 핸들 | `+0xa5 ~ +0xb4` **`NtSetInformationThread(현재 스레드, 5, &토큰, 8)`** (5 = `ThreadImpersonationToken` `[추측: 이름]`) | `NtSetInformationThread(-2, 5)` (5620) · 실패 (`+0xc2 JS → +0x173`) 면 `0xc0000022` → 5, 그 밖 → 0xe |
| ④ ALPC 보안 문맥 | 항상 (③ 성공 뒤) | `+0xc8 ~ +0xdd` 칸 채움 (`[문맥+8]` 의 `+0` = `0x20000`, `+0x10` = 0) · **`+0x124/+0x12b NtAlpcCreateSecurityContext(포트 [연결+0x88], 0, [문맥+8])`** | `NtAlpcCreateSecurityContext(포트, 0, …)` (5621) |
| ④-a 실패 | `+0x138 JS → +0x236` : `0xc0000022` · `0xc00000a5` → 5, 그 밖 → 0xe (`+0x236 ~ +0x24b`) | 오류 기록 후 반환 | — |
| ⑤ 되돌리기 | 항상 | **`+0x14d/+0x152 RestoreOldThreadToken`** (`0x6f56c`): 저장값 `0xfffffffe` → `NtImpersonateAnonymousToken` (`+0x69 ~ +0x84`) · 그 밖 → **`NtSetInformationThread(-2, 5, &(저장값 == 0xfffffffd ? 0 : 저장값), 8)`** (여기 `-2` 는 "현재 스레드" 가짜 핸들) (`+0x3f/+0x46`) + 핸들이면 `CloseHandle` (`+0x5b`) | `NtSetInformationThread(-2, 5)` (5622) |

- **실패하면 bind 를 안 보낸다** `[정적]`: `OpenSecurityContextWorker` 가 0 이 아니면 `DriveStateForward +0x364/+0x366 JNZ → +0x253` (CS 풀고 `+0x266` 반환 = 오류) → `DriveStateForwardAsyncCallback +0xb5 ~ +0xbd JNZ → +0x21` → `+0x43/+0x48` `[바인딩 vtbl+0x1b8]` (`GetErrorAndErrorClass__91530`) → 다시 시도 가능 종류면 `+0x6b/+0x70` `[+0x1c0]` (`PrepareForRetry__93ea0`) 뒤 `+0x78 JZ → +0x1d` 다시 `DriveStateForward`, 아니면 `+0x7a ~ +0x93` `[호출 vtbl+0x128]` = **`AbortCall`** (rva `0x77210`) 로 호출을 오류로 끝낸다. `BaseBind` (`+0xe0`) 로 못 간다.
- **차례는 동기와 같다**: 로그 5619 → 5620 → 5621 → 5622 ↔ ② → ③ → ④ → ⑤ 가 한 줄 한 줄 맞는다. 비동기 손님의 같은 4줄은 **일꾼 스레드 (B7) 에서, 연결 응답 뒤, bind (B8) 바로 앞** 에 나온다.
- 어느 갈래를 타는지 (토큰값 `[바인딩+0x180]` · `[바인딩+0x128]` 의 실제 값) 는 정적으로 못 정함 `[공백]` — 동기 로그가 `NtOpenThreadToken` 뒤 `NtSetInformationThread(-2, 5)` 이므로 "토큰 없음 (`0xfffffffd`)" 갈래로 읽힌다 `[추측]`.

## 4. `NtSetInformationWorkerFactory(공장, 3, &1, 4)` [정적]

### 4-1. 이 줄이 나오는 · 안 나오는 조건 (`ntdll!TpAdjustBindingCount`, rva `0xc9b90`, `(풀, 증감)`)

| 줄 | 하는 일 |
|---|---|
| `+0x4 TEST EDX / +0x6 JZ` | 증감 0 이면 아무것도 안 함 |
| `+0xa XADD.LOCK [풀+0x1b0], 증감` | **옛 값 → EAX, 새 값 = `R8D` (`+0x12`)** |
| `+0x16 TEST EAX / +0x18 JLE → +0x46` | **옛 값 ≤ 0**: `+0x46 TEST R8D / +0x49 JLE` (새 값 ≤ 0 이면 끝) · 아니면 `+0x4b EAX = 1` → `+0x50 JMP → +0x24` → `+0x3b` **`NtSetInformationWorkerFactory([풀+0x38], 3, &1, 4)`** (`+0x37 LEA EDX,[R9-1]` = 종류 3, `+0x2d R9D = 4`) |
| `+0x1a TEST R8D / +0x1d JG → 끝` | **옛 값 > 0 이고 새 값 > 0** → **syscall 없음** |
| `+0x1f EAX = -1` → 같은 syscall | 옛 값 > 0 → 새 값 ≤ 0 (마지막 묶임이 풀릴 때) 만 `-1` (`0xffffffff`) |

- ① **이 줄이 안 나오는 조건**: 같은 풀의 묶음 수 `[풀+0x1b0]` 가 이미 1 이상일 때. 풀은 `HEAP_ZERO_MEMORY` (`TpAllocPoolInternal +0xa3 OR EDX,8`) 로 만들어 0 에서 시작하니, **프로세스의 풀에 처음 묶일 때 한 번만** 나온다. 풀에 묶이는 것: ALPC 완료 (`TppAllocAlpcCompletion +0x1e4` +1 · `TppAlpcpFree +0x20` −1) · 파일 IO (`TpBindFileToDirect` · `TppIopFree`) · 잡 알림 (`TpAllocJobNotification` · `TppJobpFree`) · `RtlpTpIoDllProcessUnloads` (`ms/decomp/ntdll/TpAdjustBindingCount.c` 머리).
  - 에뮬레이터 로그에서 이 줄이 안 나온 이유는 둘 중 하나: (a) 같은 프로세스 앞쪽에 이미 묶임 (ALPC · 파일 IO · 잡) 이 있어서 그때 한 번 나왔다 → 로그 앞쪽에서 `NtSetInformationWorkerFactory(…, 3, …)` 를 찾아 보라, (b) 풀이 0 으로 시작하지 않았다 (에뮬레이터가 `RtlAllocateHeap` 의 0 채움을 안 지킴) `[추측]`.
- ② **종류 3 의 뜻** (ntoskrnl `NtSetInformationWorkerFactory`, run125 5절 표 "C[1] `+0x3ff`" 와 같음 — 읽은 줄만 다시):

| 줄 | 뜻 |
|---|---|
| `+0x3d5 ~ +0x3ef` 종류 → 길이 검사 점프 표 (`0x3a77ac`) 를 거쳐, 몸은 `+0x3ff` (run125 의 표 C[1]) | 종류 3 몸 |
| `+0x404 ECX = [공장+0x194]` (옛) · `+0x40a EDX = 입력 (부호 있음)` | **`공장+0x194` = 묶음 수 `[추측: 이름]`** |
| `+0x410 JNS → +0x565` : 더하기 · `+0x568 CMP ECX,EAX / +0x56a JNC → +0x7a8` (넘치면 `0xffffffff` 로 붙임) · `+0x570 [공장+0x194] = 새 값` | 올리기 |
| `+0x416 ~ +0x41c` : 뺄 때 옛 값 > 입력의 절댓값이면 `+0x85f → +0x862` 빼기, 아니면 `+0x422` **0 으로** | 내리기 (0 밑으로 안 감) |
| `+0x42c TEST ECX / +0x42e JZ → +0x57b` : **옛 0 → 새 ≠ 0** : `+0x582 ExpTryEnterWorkerFactoryAwayMode` (ret `+0x587`) 가 참이면 `+0x59a ExpWorkerFactoryCheckCreate` (ret `+0x59f`) | 일꾼 만들기 시도 |
| `+0x434 ~ +0x44f` : **옛 ≠ 0 → 새 0** 이고 `[공장+0x198] & 0x200` 이면 `ExpLeaveWorkerFactoryAwayMode` | "멀리 있기" 끝 |

  - `ExpTryEnterWorkerFactoryAwayMode` (rva `0x32e460`): `[공장+0x180] ≥ [공장+0x17c]` (움직이는 수가 최대 이상) 이거나 기다리는 일꾼 `[P+0x1c] ≠ 0` 이면 **거짓** (`+0x0 ~ +0x25`). 아니면 **묶음 수 `+0x194` 가 0 이면 참** (`+0x33 ~ +0x3c` → 곧바로 만들게 함). **0 이 아니면**: `+0x4a ~ +0x56` `[공장+0x198]` bit 9 켬 → 포트 큐 `[포트+4] ≠ 0` 이면 참 (`+0x5c ~ +0x64`) → 아니면 bit 10 이 이미 켜졌으면 거짓 → 안 켜졌으면 bit 9 · 10 켜고 **`KeRegisterObjectNotification(포트, ExpWorkerFactoryManagerQueue, 공장+0x268)`** (`+0x97`) 해 놓고 **거짓**.
  - 그 알림이 오면 관리 스레드가 `ExpWorkerFactoryManagerThread +0x98 BTR bit 10 → +0xa0 TEST 0x200 → +0xaf TryEnter(이제 포트가 비지 않아 참) → +0xc3 CheckCreate` — **패킷이 들어온 뒤에야 일꾼을 만든다.**
  - **결론**: 묶음 수 ≠ 0 = "이 풀의 포트에 **커널이 직접 패킷을 넣는 것** (ALPC 완료 · 파일 IO · 잡) 이 묶여 있다 → 일꾼이 하나도 없는 때 패킷이 오면 일꾼을 만들라". **쉼 시간 끝 (`0x102`) 에 일꾼을 남기는 칸은 아니다**: `[공장+0x194]` 를 읽는 곳은 `Set` 종류 3 · `ExpTryEnterWorkerFactoryAwayMode +0x33` · `NtWaitForWorkViaWorkerFactory +0x979` (일을 집은 뒤 연쇄, run125 177 줄) · `+0x588` (ALPC 보내기와 합친 다른 갈래 — 이 길 아님 `[공백]`) · `NtQueryInformationWorkerFactory +0x165` 뿐이고, `0x102` 를 정하는 `ExpWorkerFactoryWantsToCreate` 는 안 읽는다 (2-2).

## 5. `NtAlpcQueryInformation(포트, 0, 버퍼, 0x10)` [정적]

`IsPortWithSharedSection` (rva `0x58018`, 83 B):

| 줄 | 하는 일 |
|---|---|
| `+0x8 XORPS / +0xb MOVUPS [RSP+0x30]` | 버퍼 16 B 를 **0 으로 먼저 채움** |
| `+0x10 TEST RCX / +0x13 JZ → +0x4d` | 포트 핸들 0 이면 반환 1 |
| `+0x25/+0x2c NtAlpcQueryInformation(포트, 0, &버퍼, 0x10, NULL)` | |
| `+0x31 TEST EAX / +0x33 JS → +0x3f` | **실패면 반환 1** |
| `+0x35 TEST [버퍼], 0x1000000 / +0x3d JNZ → +0x5c` | **bit 24 켜졌으면 반환 0**, 아니면 `+0x3f EBX = 1` → **반환 1** |

- 부르는 곳 (`ms/decomp/rpcrt4/IsPortWithSharedSection.c` 머리 + `_calls.tsv`): **`AlpcConnect +0x388/+0x38d`** · **`IoCompleted +0x6a/+0x6f`** · `ValidateCallbackRequest` (서버가 받은 콜백 요청 검사 — 이 길 아님, asm 안 읽음).
- 결과가 쓰이는 곳:
  - `AlpcConnect +0x38f TEST EAX / JZ → +0x743` : **반환 0 일 때만** `[연결+0x240] = 0xff00` (`+0x743`), 아니면 안 건드림 (기본값 쪽). 그 뒤 `+0x395 CMP [RSP+0x70],0 / +0x39f JNZ → +0x752` 의 **비동기 판정은 반환값과 무관** (보류 `0xc0021018` + `*출력 = 1`).
  - `IoCompleted +0x6f MOV ECX,0x1000 / +0x76 JZ → +0x6e7 EDX = 0xff00` : 반환 0 이면 받기 버퍼 크기 `0xff00`, 1 이면 `0x1000`.
  - `[연결+0x240]` 을 읽는 곳: `DoSendReceive +0x5f` · `InitializeRequestAndAttributes +0x1d2` (요청 보낼 때 받기 크기).
- **결론**: 에뮬레이터가 "0 돌려주고 버퍼 안 씀" 하면 반환 1 (**공유 섹션 있음, 버퍼 크기 `0x1000`**) → **비동기 길은 안 바뀐다.** 실패 (`status < 0`) 를 돌려줘도 같은 1. 갈림은 **받기 버퍼 크기 하나뿐** (`0x1000` vs `0xff00`). 진짜 Windows 가 bit 24 를 켜서 주는지는 정적으로 못 정함 → 동적. `NtAlpcSendWaitReceivePort` 의 `BufferLength` 가 크기 차이만큼 다르다.
- 새로 안 것: **일꾼 (B2) 도 연결 응답 · 모든 통지마다 `IoCompleted` 맨 앞에서 이 `NtAlpcQueryInformation(0)` 을 한 번 더 한다** (`+0x6a`). 에뮬레이터는 일꾼 스레드에서 이 줄도 받아야 한다.

## 지금 우리 (PE_emulator) 맞나 · 틀리나

원문 로그 (`events.jsonl`, 에뮬레이터 관찰) 와 비교. "맞음" 은 이 정적 근거와 줄이 맞는다는 뜻이다.

| 로그 줄 | 판단 | 근거 · 해야 할 일 |
|---|---|---|
| seq 2185 `NtCreateEvent` (비동기 완료 이벤트) | **맞음** | winnsi `+0x168` (run129) |
| 2191 `NtAlpcConnectPort(Flags 0, RequiredServerSid 있음, 들어오는 속성 있음)` | **맞음** | `AlpcConnect +0x1f8 ~ +0x211` (Flags `ECX = 0`), `+0x283 [RSP+0x28] = R14 = [연결+0xc0]`, `+0x248 ~ +0x24f` 속성 |
| 2192 `NtAlpcQueryInformation(0)` → 0, 버퍼 안 씀 | **맞음, 길 안 바뀜** | 5절. 반환 1 (`0x1000`). **일꾼에서도 같은 부름이 한 번 더 나온다.** |
| 2193 `NtAlpcSetInformation(포트, 2, 16 B, 0x10)` | **맞음** | `Connect +0x3b1` (run137) |
| 맡김 쪽 syscall 0 개 · `NtSetIoCompletion*` 0 줄 | **`NtSetIoCompletion*` 은 맞음** (이 길에 없다). `NtReleaseWorkerFactoryWorker` 는 **맡김 자리에서 0 ~ 1 번** | 2절 ②·②′: 자기 몫이면 0, 아니면 1. 에뮬레이터는 둘 다 받을 준비 |
| `NtSetInformationWorkerFactory(…, 3, …)` 없음 | **조건부로 맞음** | 4절 ①: 풀의 첫 묶임 때만. 앞쪽에 있었는지 확인 |
| 비동기 손님에 보안 문맥 줄 없음 | **지금은 맞고, 고치면 나와야 함** | 3절 B7 — 연결 응답을 받은 일꾼에서 4줄 |
| **연결 응답 · 완료 패킷이 없다** | **틀림 (빠진 것)** | 비동기 `NtAlpcConnectPort` 는 곧바로 0 만 돌려주고 (`AlpcConnect +0x379 ~ +0x381`) **서버 수락은 메시지로 온다고 rpcrt4 가 기대한다** `[정적: 받는 쪽만, 커널 쪽은 추측]`: 클라이언트 포트 큐에 종류 `0xb` 메시지 (`IoCompleted +0x13e ~ +0x187`) 1 개 + 완료 패킷 1 개 (`{키 = SetInformation(2) 로 받은 키, 0, 0, 0}`). 붙이기 (`SetInformation(2)`) 전에 이미 쌓인 메시지는 붙이는 순간 개수만큼 패킷이 나간다 (run137 1-3 의 11 단계) → **큐에 메시지를 먼저 넣어 두면 둘 다 맞는다.** |
| 일꾼 `NtWaitForWorkViaWorkerFactory(0x64) → 0x102 → NtTerminateThread` | **틀림** | 진짜는 시간 제한 없이 잔다 (run125). `0x102` 는 놀기 제한 (약 67 초 `[파일 초기값]`) 이 지나고 `WantsToCreate` 거짓 · 움직이는 수 > 최소일 때만 (2-2). 에뮬레이터가 빈 포트에서 바로 `0x102` 를 돌려주는 것이 일꾼이 다 죽는 직접 원인 |
| 부른 쪽이 `NtWaitForSingleObject(0x220, 기한 없음)` 에서 멈춤 | **맞음** | 진짜도 bind · 요청 · 응답이 끝날 때까지 여기서 기다린다 (winnsi `+0x239`) |

에뮬레이터 어느 몸이 무엇을 해야 하나 (원문 "답이 오면 쓰는 곳"):

- **`epm.cpp accepts_async_connect`** (창구): 비동기 연결 (Flags 에 `0x20000` 없음) 을 받으면 포트 핸들 · 0 을 돌려주고, **클라이언트 포트 수신 큐에 "연결 응답" 메시지 하나 (PORT_MESSAGE 헤더 `0x28` B, `[+0x04] & 0xff == 0xb`, 나머지 0 으로 충분 — `IoCompleted` 는 이 종류에서 길이 · 본문을 안 본다)** 를 넣는다. 받기 속성은 0 으로 충분 (`+0x3bb AND R10D,0x50000000 → 0`). 진짜 커널이 넣는 헤더 칸 값 (길이 · 클라이언트 ID · 메시지 ID) 은 이번에 안 읽음 `[공백]`.
- **`src/threadpool/` 완료 포트**: `NtAlpcSetInformation(2)` 가 포트에 (완료 포트, 키) 를 달고, 이미 큐에 있는 메시지 수만큼 (그리고 이후 메시지가 들어올 때마다) 풀 완료 포트에 `{키, 0, 0, 0}` 패킷을 넣는다. 일꾼 `NtWaitForWorkViaWorkerFactory` 는 패킷이 올 때까지 **잔다** (빈 포트에서 바로 `0x102` 금지). 일꾼이 하나도 없는데 패킷이 오고 묶음 수 ≠ 0 이면 일꾼 하나를 만든다 (4절 "away mode").
- **일꾼 쪽 syscall 차례** (연결 응답 뒤, 이 차례를 그대로): `NtWaitForWorkViaWorkerFactory`(0, 항목 1) → `NtAlpcQueryInformation(0)` → `NtAlpcSendWaitReceivePort(받기만)`(종류 0xb) → (맡김: syscall 없음, 또는 `NtReleaseWorkerFactoryWorker` 한 번) → `NtOpenThreadToken` → `NtSetInformationThread(-2, 5)` → `NtAlpcCreateSecurityContext` → `NtSetInformationThread(-2, 5)` → **`NtAlpcSendWaitReceivePort(Flags 0, bind 0x70 B, 받기 NULL)`**. 그 뒤는 run137 ③ ~ ⑨ 와 같다 (bind 답도 완료 패킷으로 온다).
- **`NtSetInformationWorkerFactory`**: 종류 3 (4 B, 부호 있음) = `[공장+0x194]` 에 더하기 (0 아래로 안 내려가고 넘치면 `0xffffffff`). 옛 0 → 새 ≠ 0 이면 "멀리 있기" 판정, 옛 ≠ 0 → 새 0 이면 해제 (4절).

## 동적으로 확인할 것

(원문 짐작: `DriveStateForward +0x5d4` 가 탄 쪽 · 첫 bind 를 보내는 스레드 TID — Q258 세션과 합치게.)

1. **첫 부름이 보류로 끝나는지**: `rpcrt4!LRPC_BASE_BINDING_HANDLE::DriveStateForward+0x51a` (ret, `EAX == 0xc0021018`) · 그 스레드 TID (= 부른 쪽).
2. **연결 응답의 진짜 꼴**: `rpcrt4!IoCompleted+0x184` (`ESI == 0xb` 인지) 에서 `RBX` (메시지) 첫 `0x28` B 덤프 (`TotalLength` · `DataLength` · `Type` · `ClientId` · `MessageId` · `CallbackId`) 와 `[RBP-0x55]` (받기 속성 유효 비트).
3. **일꾼 TID 와 자기 몫 맡김**: `rpcrt4!SignalAsync+0x2f` 의 TID · `ntdll!TppWorkPost+0x4cd` (`R8D` — 1 이면 자기 몫) · `+0x4f3` 이 **걸리는지 안 걸리는지** (걸리면 `NtReleaseWorkerFactoryWorker` 가 실제로 나옴) · `rpcrt4!DriveStateForwardAsyncCallback+0xb0` TID (같은 TID 인지) · `rpcrt4!DriveStateForwardAsyncCallback+0xe0` (bind 가상 호출) · **`rpcrt4+0x4e330+0x626`** (`LRPC_CASSOCIATION::Bind` 의 `NtAlpcSendWaitReceivePort`, 같은 TID 인지).
4. **`DriveStateForward +0x5d4` 가 탄 쪽 · `[R13]`**: `+0x5cf` 에서 `[R13]` 값 · `+0x61b EqualSid` 반환 (같음) 인지. 두 번째 부름의 `+0x69 ~ +0x82` 에서 `[호출+0x2d0] / [+0x2d4]` = 6 / 7 → 8 / 9.
5. **보안 문맥 갈림**: `OpenSecurityContextWorker+0x59` (NtOpenThreadToken 반환 EAX) · `+0x89` 의 `R15` (`[바인딩+0x180]` 이 `-3` / `-2` / 핸들) · `DriveStateForward+0x327` 의 `EDX` (`[바인딩+0x128]` 가 1 이면 이 4줄이 통째로 빠짐).
6. **첫 묶임**: `ntdll!TpAdjustBindingCount+0x16` 의 `EAX` (옛 묶음 수). 로그에서 `(…,3,&1,4)` 가 나오는 첫 자리.
7. **버퍼 플래그**: `rpcrt4!IsPortWithSharedSection+0x35` 에서 `[RSP+0x30]` (진짜 포트 플래그의 bit 24).
8. **`kernelbase!TrySubmitThreadpoolCallback` 인지 kernel32 인지**: `rpcrt4!TrySubmitWork+0x26` 의 call 대상 (api-ms 가 어디로 풀리는지). 어느 쪽이든 `ntdll!TpSimpleTryPost` 이다.

## 다음 요청 후보

1. **ntoskrnl 비동기 `NtAlpcConnectPort` 안쪽** (`AlpcpConnectPort` · `AlpcpProcessConnectionRequest` · `AlpcpDispatchConnectionRequest` · `AlpcpReceiveLegacyConnectionReply` · `NtAlpcAcceptConnectPort`): 서버 수락 뒤 클라이언트 큐에 들어가는 연결 응답 메시지 (종류 `0xb`) 의 헤더 칸 · 연결 응답이 `SetInformation(2)` 앞 · 뒤 어느 쪽에서도 패킷이 하나만 나가는지. (이번 요청서에서 "ALPC 커널 본문은 이름만" 이라 안 열었다.)
2. ntdll `[…+0x80] = 3` 을 쓰는 줄 (자기 몫 맡김 상태) · `TppWorkerFindTask` · `TppPrepareDirectParams` (이번에 asm 만 뽑아 둠, 안 읽음) 의 풀어주기 패킷 (네 칸 0) 처리.
3. rpcrt4 `BindToEpMapper → RpcBindingCreateW` 가 채우는 `[바인딩+0x128]` · `+0x180` · `+0x12c` 의 값 (epmapper 바인딩의 보안 문맥 갈림 확정) — 3절의 `[공백]`.
4. `CompleteOnePart` 의 실패 갈래 (연결 거절 `0xc0000041` → 오류 `0xe`) 와 `AbortCall` 이후 winnsi 쪽 결과 (예전 `0xc0000034` 대신 거절을 돌려줄 때 필요하면).

## 판정

**분석성공** — 질문 1 ~ 5 모두 정적으로 닫음. 요청서의 짐작 (`+0x5d4` 에서 Tp 로 맡김) 은 **정정**: 맡김은 `SignalAsync` → `TpSimpleTryPost` 에 있고, 일꾼 자기 몫이면 syscall 이 없다 (자기 몫 여부는 동적 확인). `[추측]` / `[공백]` 로 남긴 것: epmapper 바인딩이 빠른 핸들이라는 것 (간접), `[바인딩+0x128]` · `+0x180` 의 실제 값, 풀 칸 이름들, 커널이 넣는 연결 응답 헤더 칸, `[…+0x80] = 3` 을 쓰는 줄.

## 근거 줄 확인 (자체 검증)

- call · jmp · jcc 목적지를 PE 바이트 (`E8` / `E9` / `0F 8x` / `FF 15` rel32 풀이) 로 맞춘 것 — 전부 일치:
  - rpcrt4 `DriveStateForward +0x589 → 0x45d20` · `+0x35d → 0x4599c` · `+0x515 → 0x48018` · **`+0x5d4 JZ → 0x44e43` (= `+0x43`)** · `DriveStateForwardAsyncCallback +0xb0 → 0x44e00` · `LrpcBHDriveStateForwardRoutine +0x5c → 0x3c264` · `SignalAsync +0x2f JMP → 0x2f2c0` · `ThreadPoolInvokeCallback +0x11 → 0x430f8` · `Bind__74b50 +0x157 → 0x44e00` · `+0x19a → 0x4c660` · `Bind__93cc0 +0x30 → 0x4c660` · `BaseBind +0x1e4 → 0x4e330` · `IoCompleted +0x3e3 → 0x3b148` · `+0x6a → 0x58018` · `CompleteOnePart +0x1c7 → 0x3b528` · `SignalAllIos +0x11c → 0x3c1a4` · `Complete +0x119 → 0x430f8` · `LrpcBaseProviderComplete +0x44 → 0x42ed4` · `Connect +0x3b1 → 0x2ef70` · `EnableAsync +0x6a → 0x2f158` · `EnableAsyncIfNecessary +0x4e → 0x2ef70`.
  - IAT 대상: `TrySubmitWork +0x26 → api-ms-win-core-threadpool-l1-2-0!TrySubmitThreadpoolCallback` · `KernelBase!TrySubmitThreadpoolCallback (rva 0xe90d0) +0x4 → ntdll!TpSimpleTryPost` · kernel32 `(0x3ce00) +0x6 → ntdll!TpSimpleTryPost`.
  - ntdll `TpSimpleTryPost +0x107 → 0x39430 (TppWorkPost)` · `TppWorkPost +0x4f3 → NtReleaseWorkerFactoryWorker` · `TpAdjustBindingCount +0x3b → NtSetInformationWorkerFactory` · `TppWorkerThread +0x379 → NtWaitForWorkViaWorkerFactory` · `+0x55e → 0x19aa0` · `+0x6fd → 0x192c0`.
- 위치가 `+0x` 인 점프 목적지는 "절대 주소 − 함수 시작" 으로 셈 (예: `DriveStateForward +0x4c0 JNZ 0x1800456f1 = +0x8f1`, `Bind__74b50 +0x217 = 0x180074d67`, `TppWorkerThread +0xc1b JZ 0x18001b084 = +0xda4`, `TppWorkPost +0x49e JZ 0x1800398fd = +0x4cd`).
- 가상표 슬롯은 `.rdata` 에서 읽음 (`pefile`, 파일로 안 남김): `LRPC_FAST_BIND_CCALL` (`0xe2590`) `+0x128 = 0x77210 (AbortCall)` · `+0x188 = 0x93cc0 (Bind)` · `+0x190 = 0x76630 (CallBound)` ; `LRPC_FAST_BINDING_HANDLE` (`0xe4440`) `+0x128 = 0x74b50` · `+0x1b8 = 0x914d0` · `+0x1c0 = 0x93ea0 (PrepareForRetry)` ; `LRPC_BINDING_HANDLE` (`0xe2380`) `+0x128 = 0xadd70`.
- 사진 로그 줄 번호 인용 없음.
- 디컴파일 코드는 붙이지 않았다. 근거는 `ms/disassemble/{rpcrt4,ntdll,ntoskrnl}/*.asm` 줄 번호 (`+0x…`) 와 `ms/decomp/rpcrt4/DriveStateForward.c` 의 갈래 요약.
