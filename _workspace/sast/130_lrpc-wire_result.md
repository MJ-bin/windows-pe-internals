# 130 lrpc-wire — 정적 분석 결과

- 요청서: `_workspace/sast/130_lrpc-wire.md` (Q230, sast/127 질문 3 의 남은 몫 — LRPC 선 위 형식)
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`.
- 날짜: 2026-11-22. 새로 analyze 한 것은 **nsisvc 하나** (11초, 직전 `free -g` = 전체 46 / 쓰는 중 14 / available 30, 그때 도는 Ghidra java 없음). rpcrt4 는 다시 analyze 하지 않고 `^이름$` export 만 했다 (depth 0, 8번). sast/128·129 모듈 (user32·combase·npfs·kernelbase·ntoskrnl) 은 건드리지 않았다.
- 근거 표기: `[정적] ms/disassemble/rpcrt4/<파일>.asm +오프셋` (부르기는 call 자리, 괄호가 돌아올 자리 `ret`). **rpcrt4 는 같은 이름 함수가 여럿이라 파일 이름이 갈린다** — 이 문서가 쓰는 파일:

| 파일 | 함수 (PDB 이름) | rva |
|---|---|---|
| `Bind__4e330` | `LRPC_CASSOCIATION::Bind` (**bind 메시지를 만들어 보내는 곳**) | 0x4e330 |
| `Bind` · `BaseBind` | `LRPC_BIND_CCALL::Bind` · `::BaseBind` (위를 부르는 쪽) | 0x4cc20 · 0x4c660 |
| `CompleteBind` | `LRPC_CASSOCIATION::CompleteBind` (bind 답 검사) | 0x3ad68 |
| `HandleReply__41bb0` · `__417f0` · `HandleReply` (=BASE_CCALL) · `__580f0` (=CASSOCIATION) · `__9c6fc` (=LRPC_ADDRESS, 서버) | `HandleReply` 다섯 | 0x41bb0 · 0x417f0 · 0x3ee10 · 0x580f0 · 0x9c6fc |
| `SendReceive__3fc00` · `SendReceive__40250` | `LRPC_CCALL::SendReceive` · `LRPC_BASE_CCALL::SendReceive` | 0x3fc00 · 0x40250 |
| `DoSendReceive` · `DoAsyncSend` · `AlpcNonPipeSend` | 동기 보내기 · 비동기 보내기 | 0x40680 · 0x41f80 · (AlpcNonPipeSend) |
| `InitializeRequestAndAttributes` | **요청 머리를 쓰는 곳** | 0x40a60 |
| `UnpackResponse` | **응답 머리 검사** | 0x40d90 |
| `AlpcSend` (=BASE_CCALL, `AlpcSend__56d44` 는 서버) | 비동기 보내기 (`NtAlpcSendWaitReceivePort`) | 0x42a18 |
| `AlpcSendWaitReceivePort` (=CASSOCIATION, `__6def8` 는 서버) | 얇은 감싸개 (상태 안 바꿈) | 0x3fa50 |
| `IoCompleted` · `ValidateCallbackReply` | 비동기 답이 오는 길 | 0x57800 · 0x9c7ec |
| `RpcpAlpcpGetHeaderSize` · `RpcpStrictAlpcToRpcStatus` | ALPC 속성 칸 크기 · 엄격 상태 표 | 0x57650 · 0x99d70 |
| `GetPresentationContext` (=LRPC_CCALL, `__91e50`·`__c0050` 는 다른 클래스) | 요청 머리 `+0x38` 의 값 | 0x8fcf0 |
| `TowerConstruct` · `OsfTowerConstruct` · `ProcessNextChunk` · `ReturnLookupValues` · `LookupNextChunk` · `LookupNotifyRoutine` | 탑 만들기·응답 검사 | 0x5a350 · 0x66290 · 0x5a968 · 0x5b9f8 · 0x5a070 · 0x5c578 |
| `ServerSetupAddress` | 동적 끝점 이름 만들기 (서버, 참고) | 0x971d0 |
| `GetErrorClass` · `AttemptRetry` | 다시 시도 규칙 | 0x915f8 · 0x91634 |

## 스크립트 문제

없음. 참고 (고칠 것은 아님): `_calls.tsv` 는 같은 이름 함수의 호출을 한 줄 묶음에 섞는다 (예: `AlpcSend +0xdc` 는 서버 `AlpcSend__56d44` 쪽, `+0x128` 이 클라이언트 쪽 `AlpcSend.asm`). 이 문서의 오프셋은 `.asm` 파일에서 직접 확인한 것.

## PDB GUID 확인 (새로 analyze 한 모듈)

| 모듈 | 바이너리 | PDB guid+age | 함수 수 | 로그 |
|---|---|---|---|---|
| nsisvc | `nsisvc.dll` 61,440 B | `CE94A7C9096AB626B28E58C48D6605591` (`llvm-pdbutil dump --summary` 의 GUID `{CE94A7C9-096A-B626-B28E-58C48D660559}` 와 같음. PDB 안 Age 3, 심볼 서버 키는 바이너리의 Age 1 — sast/126 ws2_32·sast/127 winnsi 와 같은 모양) | 77 (`_index.tsv`) | `ms/ghidra/nsisvc.analyze.log` — `SetPdb:` 줄 있음, 함수 이름이 PDB 이름 (`NsiStartRpcServer`, `RpcNsiRegisterChangeNotification`) 으로 잡힘 |

- 커널 GUID (`C29EBFB0…`) 는 ntoskrnl 만 해당이라 이번엔 해당 없음. 공개 PDB 라 구조체 타입 이름이 없다 (이름은 모두 코드 줄에서 읽은 것).
- 서버 쪽 프로시저 5·6·7 구현은 **nsisvc 안에 있다** (아래 4 절). 멈출 필요 없었다.

## 버전·스크립트 대조

"알려진 사실" 표에 rpcrt4·nsisvc 줄이 없다. 대신 `.asm` 의 `+0x` 를 **원본 바이트** (`pefile`) 와 맞췄다 — 전부 일치:
- `rpcrt4 Bind__4e330 +0x502` = `48 c7 03 48 00 70 00` (`MOV qword [RBX],0x700048`), `+0x509` = `66 c7 43 04 00 40`, `+0x50f` = `4c 89 73 28`, `+0x626` = `48 ff 15 …` (`CALL [NtAlpcSendWaitReceivePort]`), `+0x6cb` = `e8 …` (`CALL CompleteBind`).
- `CompleteBind +0x22` = `48 39 4a 28` (`CMP [RDX+0x28],RCX`), `UnpackResponse +0x37` = `48 83 7a 28 03` (`CMP qword [RDX+0x28],3`), `InitializeRequestAndAttributes +0x1ab` = `4c 89 6f 28`.
- `DoSendReceive +0x14d` · `AlpcSend +0x128` · `IoCompleted +0xc8` = `48 ff 15 …`.
- `nsisvc NsiStartRpcServer +0x123` · `+0x1a1` = `48 ff 15 …` (`RpcServerUseProtseqW` · `RpcServerRegisterIf3`), `RpcNsiRegisterChangeNotification +0x147` = `48 ff 15 …`, `ServiceMain +0xf6` = `e8 0d 09 00 00`.
- **판정: 일치.** 스크립트 문제 없음.

---

## 먼저 읽는 한 줄 답

**가장 큰 한 줄 (구조): winnsi → epmapper → nsisvc 의 세 대화 (epmapper bind·`ept_map`, nsi bind·프로시저 5) 가 전부 "비동기" 라서, 서버 흉내는 `NtAlpcSendWaitReceivePort` 한 번으로 끝나지 않는다.** 보내는 쪽 호출은 받기 칸 없이 (Flags 0, 받는 메시지 NULL) 곧바로 0 으로 돌아오고, 답은 **스레드풀 ALPC 완료 통지 → `IoCompleted` → 두 번째 `NtAlpcSendWaitReceivePort` (받기만)** 으로 온다 (2-4). 동기 길 (`Flags 0x20000`, 같은 버퍼로 답을 받음) 은 LSA 같은 다른 클라이언트가 쓴다.

**질문 1 (bind)**
1. 클라이언트가 보내는 bind 메시지는 **0x70 바이트**: `PORT_MESSAGE` (0x28) + `+0x28` 종류 = **1** + `+0x34` 인터페이스 UUID·버전 (20바이트) + `+0x48` **제안하는 전송 문법 비트마스크** (1 = NDR 2.0, 2 = NDR64, 4 = 세째 `b4537da9-3d03-4f6b-b594-52b2874ee9d0`) + `+0x4c/+0x4e/+0x50` 문법별 슬롯 번호 (u16) + `+0x54/+0x58/+0x60/+0x68` 꼬리 칸. `Type = 0x4000`, `DataLength 0x48`, `TotalLength 0x70` (1-2 표).
2. bind 답 (`CompleteBind`) 이 보는 칸은 **셋뿐**: `+0x28` (== 2 면 fault, `+0x30` 에 오류), `+0x48` 수락 마스크 (0 이면 `0x6c2`, 제안의 부분집합이 아니거나 비트가 정확히 하나 (1/2/4) 가 아니면 `0x6c0`), `+0x08` 서버 프로세스 ID (검사 없이 연결 객체 `+0x238` 에 보관). **길이 검사 없음.** 최소 답 = 종류 1 · 마스크 하나 (1-3 표).
3. winnsi (인터페이스 `7ea70bcf…`) 도, epmapper (`e1af8308…`) 도 제안하는 마스크는 **3 (NDR 2.0 + NDR64)**. ⚠ sast/127 2-3 의 8단계는 EPM 쪽을 "전송 문법 NDR 2.0" 하나로만 적었다 (3-1 ③ 은 "둘" 이라 적음). **실제로 둘이다**: EPM 클라이언트 인터페이스 (rpcrt4 rva 0xe1220, 깃발 `0x2000000`) 의 전송 문법 표 (0xe7560) 도 두 줄 (NDR 2.0, NDR64, `nCount 2`).

**질문 2 (요청·응답 머리)**
1. 요청 머리 (`InitializeRequestAndAttributes`): `PORT_MESSAGE` 뒤 `+0x28` 종류 **0** · `+0x30` 깃발 (0 / 4 = 큰 데이터는 view / 8 / 1) · `+0x34` **호출 ID** · `+0x38` 슬롯 번호 (bind 때 서버가 본 그 u16) · `+0x3c` **프로시저 번호** · 스텁 데이터는 `+0x68` 부터. `DataLength = 스텁 길이 + 0x40`, `TotalLength = DataLength + 0x28`, `Type = 0`.
2. 응답 머리 검사 (`UnpackResponse`): `TotalLength < 0x38` → `0x6c0`. `+0x28 == 3` 이면 응답: **`+0x34` 가 요청의 호출 ID 와 같아야** 하고 (아니면 `0x6c0`), `TotalLength ≥ 0x40`, 스텁 데이터는 `+0x40` 부터, 길이 = `TotalLength − 0x40`. `+0x28 ≠ 3` 은 전부 fault 로 읽혀 `+0x30` 이 오류 (0 이거나 치명 14종이면 `0x6c0`, 그 밖은 그대로).
3. ALPC 오류 → RPC 오류: 보내기 3 자리 (bind·동기 요청·비동기 요청) 표가 약간씩 다르다 (2-5). **`0xc0000008` (핸들 무효) = 셋 다 `0x6bf` (RPC_S_CALL_FAILED_DNE, 1727)**, 그리고 `0x6bf`·`0x6ba` 는 "다시 시도" 종류 (`GetErrorClass`), 상한은 4번째 (`AttemptRetry`). `RpcpStrictAlpcToRpcStatus` 의 `0xc0000008 → 6` 은 서버 쪽 표라 이 길에 안 쓰인다.

**질문 3 (`ept_map` 응답 검사)**
1. 묻는 탑 = **4층**: ① 인터페이스 UUID + 버전 (프로토콜 ID `0x0d`) ② 전송 문법 (NDR 2.0 `8a885d04-…`, 버전 2.0, `0x0d`) ③ `0x0c` (ncalrpc 의 "RPC" 층, 버전 0) ④ `0x10` (ncalrpc, 끝점 없음 = RHS 2바이트 0). 총 70바이트 (u32 길이 4 + 66).
2. 응답 검사 (`ProcessNextChunk`): 상태 0, 탑 `[0..개수)` 각각 `tower_length` 안에 들고, **층 수 3~6**, 3층의 프로토콜 ID `0x0c` + 층 수 **정확히 4** (그 밖 `0x0a/0x0b` 는 TCP 갈래), 4층 RHS 길이 **2~1000** 이고 **끝이 NUL** 인 ASCII 끝점 이름 → 와이드로 바꿔 저장. 1·2층의 값과 4층의 프로토콜 ID 는 **안 본다**. 탑 0개 + 끝점 핸들 NULL → `0x6d9`, 이미 하나 얻은 뒤의 `0x6d9` 는 성공으로 바뀐다.
3. 서버 흉내가 돌려줄 최소 탑 하나의 **바이트 꼴 (3-3 표)** 은 코드로 닫혔다. NDR 틀 (entry_handle 20바이트 · 개수 · 포인터 배열 · 상태) 은 `[추측]`.

**질문 4 (nsisvc 서버)**
1. **끝점은 동적**: `RpcServerUseProtseqW("ncalrpc", 10, SD)` (끝점 문자열 없음) → 이름은 rpcrt4 가 `LRPC-` + 난수 9바이트의 소문자 16진 18자 (`ServerSetupAddress`), 그 뒤 `RpcServerInqBindings` + `RpcEpRegisterW(인터페이스, 바인딩들, NULL, "NSI server endpoint")` 로 epmapper 에 올린다 (`NsiStartRpcServer +0x123/+0x1a1/+0x2de/+0x337`).
2. 프로시저 5 (`RpcNsiRegisterChangeNotification`): **입력 값 (모듈 ID·객체 번호·compartment) 을 nsisvc 가 검사하지 않는다.** 보는 것은 (a) 호출 속성 조회 성공 + **커널 모드 호출자 아님** (아니면 `0x490`), (b) 클라이언트 흉내내기 성공, (c) `nsi!NsiRegisterChangeNotificationEx` (nsi.dll → 커널 NSI) 가 0. 인터페이스 콜백 `NsiSecurityCallBack` 은 호출 속성 `AuthenticationLevel ≥ 6` 을 요구한다 (아니면 `5`, 4-1 마지막 항목). 성공이면 **컨텍스트 핸들 = 0x98 바이트 블록 포인터** + 반환 0. 실패값: `0x490` · `8` · `0x139f` · 그 밖 호출이 준 값.
3. 프로시저 7 은 NDR **비동기 서버 호출**: 블록이 "취소됨 (`+0x10 == 1`)" 이면 `0x4c7`, 대기 중인 변경이 없으면 완료하지 않고 걸어 둠 (클라이언트 끊김 알림만 구독). → "답하지 않는다" 가 서버 코드와 맞다.

**지금 우리 (PE_emulator)**: 지금 파일 (읽은 시점) 의 `:190~:192` · `:305~:307` 은 이미 rpcrt4 의 연결을 `0xc0000034` 로 돌려보내는 **"서버 없음" 쪽으로 고쳐져 있어** (sast/127 T1 과 맞음) **깨끗하게 실패하는 길로는 맞다.** 다만 "등록 성공" 흉내 (T2) 를 하려면 ① `live_server_required` 가 rpcrt4 의 빈 `a[9]` 버퍼 때문에 항상 참인 것, ② `NtAlpcSetInformation` (`:602` 가 `kMessageZoneInformation` 말고는 `stop`), ③ `:612~:626` 의 메시지 전달 전부가 필요하다 (6 절).

---

## 1. LRPC bind (질문 1, 3-1 ③·⑥)

### 1-1. `NtAlpcSendWaitReceivePort` 를 부르는 6 자리와 인자

`LRPC_CASSOCIATION::Bind` (`Bind__4e330.asm`) 의 부르기: `+0x626` (ret `+0x62d`). 인자 (x64: RCX, EDX, R8, R9, `[RSP+0x20]` … `[RSP+0x38]`):

| 인자 | 동기 bind (`param_2 == 0`) | 비동기 bind (`param_2 != 0`) | 줄 |
|---|---|---|---|
| PortHandle | `[연결객체+0x88]` | 같음 | `+0x5fd MOV RCX,[R13+0x88]` |
| Flags | **`0x20000`** (`ALPC_MSGFLG_SYNC_REQUEST` 로 읽힘 `[이름 추측]`) | **`0`** | `+0x540 MOV R15D,0x20000` (비동기는 R15D 가 0 그대로), `+0x61e MOV EDX,R15D` |
| SendMessage | bind 메시지 (아래 1-2), 0x1000 버퍼에 앞 0x70 바이트만 | 같음 | `+0x616 MOV R8,RBX` |
| SendMessageAttributes | 호출 객체의 ALPC 속성 칸 (`this+0x128`): **Allocated = Valid = `0x20000000` (Context 하나)**, Context 속성의 `MessageContext` (속성 시작 + 0x10) = **bind 를 부른 `LRPC_BIND_CCALL*`**, 그 뒤 8바이트 (`+0x14`) = 0 | 같음 | `+0x587 MOV qword [R10],0x20000000`, `+0x597 CALL RpcpAlpcpGetHeaderSize(0)`, `+0x5a7 MOV [R9+R10+8],R14`, `+0x5ac MOV qword [R9+R10+0x14],0`, `+0x5ba MOV dword [R10+4],0x20000000`, `+0x60e MOV R9,R10` |
| ReceiveMessage | **보낸 버퍼와 같은 버퍼** | **NULL** | `+0x55c MOV RBP,RBX` (동기) / `+0x77d MOV EBP,R11D` (비동기), `+0x621 MOV [RSP+0x20],RBP` |
| BufferLength | `&[RSP+0x78]` = `0x1000` | 같음 | `+0x5c9 MOV qword [RSP+0x78],0x1000`, `+0x619` |
| ReceiveMessageAttributes | 보낸 속성과 같은 칸 | NULL | `+0x55f MOV RSI,R10` / `+0x77a MOV ESI,R11D`, `+0x611` |
| Timeout | 호출자 ms: 0 → NULL, `0xffffffff` → `0x8000000000000000` (무한), 그 밖 `−(ms×10000)` | **NULL** | `+0x546 TEST EAX,EAX`, `+0x7ec CMP EAX,-1`, `+0x851 IMUL RCX,RAX,-0x2710`, `+0x609 MOV [RSP+0x38],RDI` |

- 비동기 bind 는 보내기가 0 이면 **bind 메시지 버퍼를 곧바로 풀고** (`+0x7cd ~ +0x7d0 BCACHE::Free`) 보류 값 `0xc0021018` 을 돌려준다 (`+0x7c5`). 답은 2-4 의 길로 온다.
- 비동기냐 동기냐 (`CASSOCIATION::Bind` 의 둘째 인자 = `R8D`) 는 `LRPC_BIND_CCALL::Bind` 의 **첫 정수 인자 (`EDX` → `R12D`, `Bind.asm +0x42 MOV R12D,EDX` → `+0x1d7 MOV R8D,R12D`; `+0x1fb` ret `+0x200`)** · `BaseBind +0x1bc` (ret `+0x1e9`) · `NegotiateTransferSyntax +0x769` (ret `+0x76e`) 에서 넘어온다. epmapper bind (`BindToEpMapper` 가 비동기 상태를 넘김) 와 nsi bind 가 비동기인지는 `[추측: DriveStateForward 의 인자 전달은 안 읽음]` — **동적 1 로 확인.**
- 그 밖 부르는 자리 (표 맨 아래 줄은 2-4): `DoSendReceive +0x14d` (ret `+0x154`), `AlpcSend +0x128` (ret `+0x12f`), `IoCompleted +0xc8` (ret `+0xcf`), `AlpcSendWaitReceivePort +0xbb` (ret `+0xc2`, 감싸개), `ValidateCallbackReply +0x146` (ret `+0x14d`, 쓸모없는 속성을 버릴 때).

### 1-2. bind 메시지 칸 (보내는 쪽) — `Bind__4e330.asm`

버퍼는 `BCACHE::Allocate(0x1000)` (`+0x46c ~ +0x471`) 에서 오고 **지우지 않으므로**, 아래에서 "안 씀" 이라 한 칸은 이전 값이 남는다.

| 칸 | 오프셋 | 크기 | 클라이언트가 넣는 값 | 줄 |
|---|---|---|---|---|
| `DataLength` | `+0x00` | u16 | **`0x48`** | `+0x502 MOV qword [RBX],0x700048` |
| `TotalLength` | `+0x02` | u16 | **`0x70`** | 〃 (같은 qword 의 위 두 바이트) |
| `Type` | `+0x04` | u16 | **`0x4000`** (상위 바이트 깃발, `LPC_NO_IMPERSONATE` 로 읽힘 `[이름 추측]`) | `+0x509 MOV word [RBX+4],0x4000` |
| `DataInfoOffset` | `+0x06` | u16 | 0 | `+0x502` |
| `ClientId` | `+0x08` · `+0x10` | u64 ×2 | 0 · 0 | `+0x51b` · `+0x51f` |
| `MessageId` | `+0x18` | u32 | 0 | `+0x517` |
| (`ClientViewSize`/`CallbackId`) | `+0x20` | u64 | 0 | `+0x513` |
| **LRPC 종류** | `+0x28` | u64 | **1** (= bind) | `+0x50f MOV [RBX+0x28],R14` (R14 = 1, `+0x52 LEA R14D,[R15+1]`) |
| (안 씀) | `+0x30` | u32 | — | — |
| **인터페이스 UUID** | `+0x34` | 16 | 클라이언트 인터페이스의 `InterfaceId` (EPM `e1af8308-5d1f-11c9-91a4-08002b14a0fa`, nsi `7ea70bcf-48af-4f6a-8968-6a440754d5fa`) | `+0x48b MOVUPS [RBX+0x34],XMM0` |
| **인터페이스 버전** | `+0x44` | u32 (u16 주, u16 부) | EPM `3.0` · nsi `1.0` | `+0x492 MOV [RBX+0x44],EAX` |
| **전송 문법 마스크** | `+0x48` | u32 | 인터페이스 표의 문법마다 OR: **1 = NDR 2.0 (`8a885d04-1ceb-11c9-9fe8-08002b104860` v2)**, **2 = NDR64 (`71710533-beba-4937-8319-b5dbef9ccc36` v1)**, **4 = `b4537da9-3d03-4f6b-b594-52b2874ee9d0` v1**. 표에 없는 문법 → 오류 `0x6c2`. winnsi·EPM 둘 다 **3** | `+0x495` (0 으로 시작), `+0x4e0` · `+0x713` · `+0x8a5` (OR 1·2·4); 문법 GUID 는 rpcrt4 rva 0xebaf8 · 0xebb10 · 0xebb88, 비트 표는 0xeb9c0 (`[3]=2, [5]=4, [15]=1`, 인덱스 = GUID 8번째 바이트 & 0xf) |
| 문법 1 의 **슬롯 번호** | `+0x4c` | u16 | 연결 객체 표 (`this+0x58`) 에서 이 문법 결합의 칸 번호 (`[결합+0x48]`) | `+0x4eb MOV [RBX+0x4c],CX` |
| 문법 2 의 슬롯 번호 | `+0x4e` | u16 | 〃 | `+0x71e` |
| 문법 4 의 슬롯 번호 | `+0x50` | u16 | 〃 | `+0x8b0` |
| (안 씀) | `+0x52` | u16 | — | — |
| 깃발 | `+0x54` | u32 | 동기 **0** / 비동기 **`인터페이스.Flags & 0x2000000`** (winnsi·EPM 둘 다 `0x2000000`) | `+0x53c MOV [RBX+0x54],R15D` · `+0x780 ~ +0x78b` |
| 호출자 인자 6 · 7 | `+0x58` (u32) · `+0x60` (u64) | | `LRPC_BIND_CCALL::Bind` 는 0 · 0 (`Bind.asm +0x1e8 MOV [RSP+0x38],RDI`, `+0x1ed MOV [RSP+0x30],EDI`) | `+0x569` · `+0x574` |
| 연결 객체 칸 | `+0x68` | u32 | `[연결객체+0x244]` | `+0x57f MOV EAX,[R13+0x244]` → `+0x57f..` |

- 인터페이스 구조체 `Length` 가 `0x60` 이 아니면 `0x6b5` (`+0x91 CMP [R8],0x60`, `+0x7e2 MOV ESI,0x6b5`).
- **슬롯 번호의 쓰임**: 나중에 요청 머리 `+0x38` 에 같은 값 (수락된 문법의 슬롯) 이 들어간다 (2-1). 서버는 "이 호출이 어느 전송 문법인지" 를 이걸로 안다.

### 1-3. bind 답 (받는 쪽) — `CompleteBind.asm` + `HandleReply__41bb0`/`__417f0`

경로: **동기** = `Bind__4e330 +0x6aa MessageReceived` → `+0x6cb CompleteBind` (같은 버퍼가 답으로 덮어써져 있음). **비동기** = `IoCompleted` → 종류 1·2·3 → 호출 객체의 `HandleReply` (`+0x130` 가상) → 호출 상태 `[this+0x108] == 9` (bind 보냄) 이면 `CompleteBind` (`HandleReply__41bb0` · `__417f0`).

| 칸 | 오프셋 | 크기 | 기대하는 값 | 틀리면 (RPC 오류) | 줄 |
|---|---|---|---|---|---|
| ALPC 메시지 종류 (비동기만) | `+0x04` 의 아래 바이트 | | **2** (reply). `0xb` 는 따로 (`IoCompleted +0x184`), 5 = 포트 닫힘 → `HandleConnectionClose`, 1 = 서버의 콜백 요청 | 2 가 아니면 bind 답으로 안 읽힘 | `IoCompleted +0x12f ~ +0x19d` |
| LRPC 종류 | `+0x28` | u64 | **2 = fault** (`+0x30` 로) / 그 밖 = bind 성공으로 진행. 비동기는 `1·2·3` 만 `HandleReply` 로 감 (`0`=파이프 조각, `10·11`=콜백 답, 그 밖=`[+0x138]` 가상 호출 (`IoCompleted +0x52e ~ +0x575`)) | — | `CompleteBind +0x22 CMP [RDX+0x28],RCX(2)`; `IoCompleted +0x1e7 ~ +0x1f7`, `+0x52e` |
| fault 오류 | `+0x30` | u32 | (종류 2 일 때) 서버 오류 코드 | `SanitizeErrorCodeFromServer`: 코드가 **0** 이거나 **치명 14종 (`RpcpIsFatalError`: `0xc0000005 0xc0000194 0xc00000aa 0x80000002 0xc0000096 0xc000001d 0x80000003 0xc00000fd 0xc0000235 0xc0000006 0xc0000420 0xc0000409 0x80000001 0xc00002c9`, sast/127)** 이면 `0x6c0`, 그 밖은 코드 그대로 | `CompleteBind +0xed ~ +0xf5`, `SanitizeErrorCodeFromServer` (rva 0x3c6ec) |
| **수락 마스크** | `+0x48` | u32 | 0 이 아님 · **제안 마스크 (`tagLRPC_BIND_DATA+0x20` = 보낸 `+0x48`) 의 부분집합** · **비트가 정확히 하나 (1, 2, 4 중)** | 0 → **`0x6c2`**; 부분집합 아님 → `0x6c0` (기록 `0x33f`); 둘 이상 / 모르는 비트 → `0x6c0` (기록 `0x340 / 0x341 / 0x342 / 0x343`) | `+0x2c`, `+0x3a ~ +0x44`, `+0x4e ~ +0x5f`, `+0x14d ~ +0x16a`, `+0x126 MOV EDI,0x6c2` |
| 서버 프로세스 ID | `+0x08` | u64 | 검사 없음. **`[연결객체+0x238]` 이 0 이면 여기 값을 보관** | — | `+0x6e CMP [RBP+0x238],R10`, `+0x77 MOVUPS XMM0,[RSI+0x8]` / `+0x7b MOVQ [RBP+0x238],XMM0` |
| 길이 (`TotalLength`) | `+0x02` | u16 | **검사 없음** (코드가 `+0x4c` 까지 읽음) | — | (읽는 줄 없음) |
| 고른 문법 | (마스크의 비트) | | 마스크 1 → 슬롯 `[bind_data+0x24]`, 2 → `+0x28`, 4 → `+0x2c` 의 결합이 **쓰임**으로 선택됨 (`[결합+0x50] = 1`, `*출력 = 결합`) | — | `+0x84 ~ +0xac` |

- 성공이면 호출 객체 상태 `10` 으로 (`SetState(10)`), 비동기 완료는 `IssueNotification`. 오류면 동기 `Bind` 는 `AbortAssociation(오류)` (`+0x870`) 를 불러 **연결 객체가 닫힌다**.
- **최소 bind 답** = `+0x28` 종류 1 (보낸 그대로) + `+0x48` 마스크 하나 (제안에 든 것) + `+0x08` 0 이 아닌 PID (권장). ALPC 쪽: 종류 2 (reply), **Context 속성의 `MessageContext` = 클라이언트가 보낸 값** (2-2).
- 어느 문법을 골라야 하나 → **둘 다 가능**. 코드는 `Ndr64AsyncClientCall` 을 부르므로 NDR64 (마스크 2) 가 자연스러워 보이지만 `[추측]`, 실제 윈도 서버의 선택은 **동적 2 (`+0x48` 의 값)** 로 확인.

---

## 2. 요청·응답 머리 (질문 2, 3-1 ④·⑦)

### 2-1. 요청 머리 칸 — `InitializeRequestAndAttributes.asm` (동기 `DoSendReceive` 도 비동기 `DoAsyncSend → AlpcNonPipeSend` 도 같은 함수)

| 칸 | 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|---|
| `DataLength` | `+0x00` | u16 | 스텁 길이 `[호출+0x20c]` **+ 0x40** (큰 데이터는 view 로 보내면 고정 **`0x48`**) | `+0x192 ~ +0x1a1`, `+0x2ea MOV word [RDI],CX(0x48)` |
| `TotalLength` | `+0x02` | u16 | `DataLength + 0x28` | `+0x1a7 ADD CX,0x28`, `+0x1af` |
| `Type` · `DataInfoOffset` | `+0x04` · `+0x06` | u16 ×2 | 0 · 0 (`PORT_MESSAGE` 0x28 바이트를 지움; **bind 와 달리 `0x4000` 아님**) | 위쪽 `psVar1[0..0x13]=0` |
| **LRPC 종류** | `+0x28` | u64 | **0** (= 요청) | `+0x1ab MOV [RDI+0x28],R13` (R13 = 0) |
| **깃발** | `+0x30` | u32 | 0 / **4** (스텁 데이터를 데이터 view 로 보냄) / `\|= 8` (`[호출+0x120] & 4`) / `\|= 1` (`[호출+0xe4] != 0` = 인과 흐름 GUID 있음) | `+0x19d`, `+0x2c7`, `+0x206`, `+0x319` |
| **호출 ID** | `+0x34` | u32 | `[호출+0x124]` | `+0x1db`, `+0x1ea MOV [RDI+0x34],EAX` |
| **슬롯 번호** | `+0x38` | u32 | `GetPresentationContext` = `[호출+0x278]→[+0x48]` = **bind 때 서버가 본 그 슬롯 (u16)** | `+0x1b6 ~ +0x1cf` (가상 `+0x168`), `GetPresentationContext.asm +0x0 ~ +0xa` |
| **프로시저 번호** | `+0x3c` | u32 | `RPC_MESSAGE.ProcNum` (`SendReceive` 가 `[호출+0x240]` 에 보관) — EPM 비동기 `ept_map` = **8**, nsi 등록 = **5**, 알림 기다림 = **7** | `+0x1d2 ~ +0x1d8`, `SendReceive__3fc00` (`*(this+0x240) = ProcNum`) |
| (0) | `+0x40` | u32 | 0 | `+0x1e1` |
| (0) | `+0x48` (u64) · `+0x50` (u32) | | 0 · 0 (`DoSendReceive`/`DoAsyncSend` 가 지움) | `DoSendReceive +0x50 ~ +0x54` |
| 인과 흐름 GUID | `+0x58` | 16 | 깃발 1 일 때 `[호출+0xe8]` 16바이트 | `+0x20a ~ +0x211` |
| **스텁 데이터** | **`+0x68`** | | NDR 로 마샬한 입력 | `TotalLength = 0x28 + 0x40 + 길이` 로 맞아떨어짐 |
| 큰 데이터 | `+0x68` | u64 | view 일 때 데이터 길이 | `+0x2f3 MOV [RDI+0x68],RAX` |

- `+0x44`, `+0x54` 는 안 쓴다 (서버가 안 읽는다고 짐작 `[추측]`).
- **서버가 응답에 그대로 되돌려야 하는 것**: `+0x34` 호출 ID (2-3).

### 2-2. 요청에 붙는 ALPC 속성 — 서버 흉내가 "답에 되돌려야 하는 것"

보내는 속성 칸 (`this+0x128`, 클래스 `RPCP_ALPC_MESSAGE_ATTRIBUTES`): 맨 앞 8바이트 `{AllocatedAttributes, ValidAttributes}`, 이어서 켜진 속성이 **Security (0x80000000) → DataView (0x40000000) → Context (0x20000000) → Handle (0x10000000) → Token (0x08000000) → Direct (0x04000000) → WorkOnBehalf (0x02000000)** 순서로 놓인다. `RpcpAlpcpGetHeaderSize(마스크)` = **8 + (마스크에 든 속성의 크기 합)**; 크기: Security 0x18 · DataView 0x20 · Context 0x20 · Handle 0x18 · Token 0x18 · Direct 8 · WorkOnBehalf 8 (`RpcpAlpcpGetHeaderSize.asm` +0x0 ~ +0x62).

| 요청 | Allocated | Valid | 비고 | 줄 |
|---|---|---|---|---|
| 일반 (큰 데이터 view 없음) | `0xfe000000` | `0xa0000000` \| `0x02000000` (`[호출+0x120]` 의 7번 비트가 0 이면) = **`0xa2000000`** (Security + Context + WorkOnBehalf) | 속성 칸 크기 `0xa0` | `InitializeRequestAndAttributes.asm +0x222 ~ +0x2a6`, 디컴파일 줄 `uVar16 \|= 0x2000000` |
| 큰 데이터 view 있음 | 〃 | `0xe0000000` \| `0x02000000` | `+0x2a8 ~ +0x2f7`: DataView 속성 `{Flags 0x40000, …, ViewBase, ViewSize}` | |
| "빠른" 길 (`[바인딩+0x178] & 0x40` 이고 첫 인자 `EDX == 0` 이고 `[호출+0x120] & 2 == 0`) | `0x7e000000` | `0x20000000` (\| `0x02000000`) (큰 데이터 view 가 있으면 `0x60000000`) | Security 없음, 크기 `0x88` | `+0x34 ~ +0x85` (`+0x5e MOV EBP,0x60000000`, `+0x6a R10D=0x20000000`, `+0x70 ECX=0x7e000000`, `+0x79 EDX=0x88`) |
| bind | `0x20000000` | `0x20000000` | Context 하나 (1-1) | |

- **Context 속성의 `MessageContext` 칸에 클라이언트가 자기 호출 객체 (`LRPC_BASE_CCALL*`) 포인터를 넣는다** (일반 요청: 속성 칸 `+0x28`; 큰 view 있으면 `+0x48`; bind: `+0x10`) — `InitializeRequestAndAttributes.asm` 디컴파일 `*(this + lVar12 + uVar15) = this`, `Bind__4e330 +0x5a7`.
- 답이 올 때 **커널이 이 `MessageContext` 를 그대로 되돌려 주어야** 한다: 비동기 `IoCompleted` 는 받기 속성 (Allocated `0xfa000000`) 의 Context 칸 (**속성 칸 `+0x48`**, `RpcpAlpcpGetHeaderSize(0xc0000000) = 0x40` + 8) 에서 호출 객체를 꺼내 쓰고 (`IoCompleted +0x1ae ~ +0x1b6`, `+0x4c0 ~ +0x4c8`), 0 이면 그 메시지를 버린다 (`+0x4cd TEST RSI,RSI` / `+0x4d0 JZ`). **에뮬레이터가 값을 지어내면 안 된다 — 보낸 값을 그대로.**
- 또 받는 쪽 객체의 `+0xc` 칸에 `0x1000` 비트가 있어야 호출 객체로 취급한다 (`IoCompleted +0x1b6 TEST [R15+0xc],0x1000`) — 진짜 포인터를 돌려줘야 하는 이유.

### 2-3. 응답 머리 — 클라이언트가 검사하는 칸 (`UnpackResponse.asm`)

`SendReceive` (동기, `SendReceive__3fc00`/`__40250`) 와 `HandleReply` (비동기, `HandleReply__417f0`/`.asm`/`__41bb0`) 모두 같은 `UnpackResponse(this, 응답, 속성, &오류정보)` 를 부른다.

| 단계 | 검사 | 틀리면 (RPC 오류) | 줄 |
|---|---|---|---|
| 1 | `TotalLength` (`+0x02`) **≥ `0x38`** | **`0x6c0`** (기록 `0x3d0`) | `+0x2d CMP R10D,0x38`, `+0x31 JC` |
| 2 | **`+0x28 == 3`** (= 응답). 아니면 fault 갈래 (아래) | | `+0x37 CMP qword [RDX+0x28],3` |
| 3a | **깃발 `+0x30` 의 4 비트가 0 (작은 응답)**: 받기 속성의 `Valid & 0x40000000` 가 **꺼져 있어야** | 켜져 있으면 `0x6c0` (기록 `0x3cf`) | `+0x42 ~ +0x57` |
| 3b | `TotalLength ≥ 0x40` **그리고 `[호출+0x124] == [응답+0x34]`** (호출 ID 맞추기) | `0x6c0` (기록 `0x3cd`) | `+0x5d ~ +0x70` |
| 3c | 스텁 데이터 포인터 = **`응답 + 0x40`**, 길이 = `TotalLength − 0x40` | — | `+0x7d LEA R14,[RDX+0x40]`, `+0x8b SUB EAX,0x40`, `+0x95 MOV [RDI+0x210],EAX` |
| 4a | **큰 응답 (깃발 4 비트 켜짐)**: `Valid & 0x40000000` 이 켜져 있어야 | 꺼져 있으면 **`0xe`** (기록 `0x3cc`) | `+0x11a ~` |
| 4b | `TotalLength ≥ 0x48`, 호출 ID 맞음, view 의 `ViewSize` (속성의 DataView `+0x18`) ≥ 응답 `+0x40` 의 데이터 크기 | `0x6c0` (기록 `0x3ca` · `0x3cb`) | |
| 4c | view 에 `Flags & 0x40000` 이 없으면 `BCACHE::Allocate` + `memcpy` 로 사본 (실패 시 `0xe`) | `0xe` (기록 `0x3d1`) | |
| 5 | 받기 속성의 `Handle` 비트 (`0x10000000`) 가 켜져 있으면 시스템 핸들 복원 (`FromAlpcHandleAttribute`) | 실패 → 기록 `0x3d2` | `+0xa5 TEST [R12+4],0x10000000` |
| fault 갈래 (`+0x28 ≠ 3`) | 오류 = **`+0x30`** (u32) → `SanitizeErrorCodeFromServer`; `+0x34 & 1` 이면 `+0x38` 부터 확장 오류 정보 (`TotalLength−0x38 < 0x1000`) | 오류 코드가 **0 · 치명 14종 → `0x6c0`**, 그 밖 → 코드 그대로 (이게 호출의 RPC 오류가 됨) | `+0x1ff` (ret `+0x204`) `SanitizeErrorCodeFromServer` 호출, 위쪽 갈래는 `+0x3c JNZ +0x1fc` |

- **그러므로 최소 응답 (작은 응답)**: `+0x00 DataLength` = `0x18 + 스텁길이`, `+0x02 TotalLength` = `0x40 + 스텁길이`, `+0x28` = **3**, `+0x30` = 0, `+0x34` = **요청의 `+0x34` 그대로**, `+0x38 ~ +0x3f` 는 안 읽음, 스텁 데이터는 **`+0x40`** 부터. 받기 속성 `Valid` 에 DataView 비트 없음, Context 속성 있음 (`MessageContext` = 요청 때 보낸 값).
- 동기 `SendReceive` 는 `UnpackResponse` 앞에서 **`+0x28 == 6`** (서버가 보낸 "콜백 요청") 이면 콜백 갈래로 간다 (`SendReceive__3fc00` 디컴파일 `*(longlong*)(p_Var1+0x28) == 6`). 일반 흉내는 안 만나는 길.
- 종류 값 모음 (+0x28): **0 요청 · 1 bind (요청과 답이 같음) · 2 fault · 3 응답 · 6 콜백 · 10 · 11 (내부: `ValidateCallbackReply` 가 콜백 답의 3·2 를 10·11 로 바꿔 씀)**. 이름은 `[이름 추측]`, 값은 코드에서 읽은 것.

### 2-4. 비동기 답이 오는 길 (winnsi·EPM 은 전부 이쪽) — `IoCompleted.asm`

1. 연결이 비동기이면 `EnableAsync → CreateAlpc → TpAllocAlpcCompletionEx(포트 핸들, LrpcIoComplete, …)` (sast/127 2-2). 이 완료 통지 (스레드풀 콜백 `LrpcIoComplete(인스턴스, 문맥, TP_ALPC, 완료 문맥 속성)`) 가 오면 `IoCompleted(완료 문맥)` 이 **받기만 하는** `NtAlpcSendWaitReceivePort` 를 부른다 (`IoCompleted +0xc8`, ret `+0xcf`).
   - `NtAlpcSendWaitReceivePort(포트, Flags, NULL, NULL, 새 버퍼 (BCACHE 0x1000, 못 얻으면 비상 버퍼), &버퍼길이, &받기속성, NULL)`.
   - Flags = **완료 문맥이 있으면 `0x1000000`**, 없으면 0. 완료 문맥이 있으면 받기 버퍼의 `MessageId` (`+0x18`) 에 문맥의 `[+0x14]`, `CallbackId` (`+0x20`) 에 `[+0x18]` 을 미리 써서 "이 메시지" 를 달라고 한다 (`IoCompleted +0x277 MOV EAX,[RSI+0x14]` / `+0x27a MOV EDX,0x1000000` / `+0x27f MOV [RBX+0x18],EAX` / `+0x282 MOV EAX,[RSI+0x18]` / `+0x285 MOV [RBX+0x20],EAX`).
   - 버퍼 길이 = `IsPortWithSharedSection(포트)` 가 참이면 `0x1000`, 거짓이면 `0xff00` (`+0x6a ~ +0x76`).
   - 받기 속성 Allocated = **`0xfa000000`** (Security + DataView + Context + Handle + Token + WorkOnBehalf).
2. 받은 메시지의 **ALPC 종류 (`+0x04` 의 아래 바이트) == 2** 일 때만 답으로 처리 → `[+0x28]` (LRPC 종류) 가 **1 · 2 · 3** 이면 호출 객체의 `HandleReply` 가상 (`[RAX+0x130]`) 호출 (`IoCompleted +0x206 ~ +0x21c`, ret `+0x221`). 이 호출이 `UnpackResponse` (일반 응답) / `CompleteBind` (bind) 로 간다.
3. 받기 호출이 오류면: `0xc0` → 버리고 끝, `0xc0000041` → 따로 (`lVar15 = 0xe`), **그 밖은 `RpcpReportFatalError(2, 상태)` = 치명 (INT3)** (`IoCompleted +0x486 ~ +0x4ab`). 에뮬레이터가 이 자리에서 알 수 없는 오류를 돌려주면 프로세스가 죽는다.
4. 비동기 요청 쪽 (`AlpcSend.asm`): `NtAlpcSendWaitReceivePort(포트, Flags, msg, attrs, 0, 0, 0, 0)` — **Flags = `[호출+0x244] & 0x800000` 이면 `0xc00000`, 아니면 0** (`+0x92 MOV EAX,[RDI+0x244]` / `+0x9f AND EAX,0x800000` / `+0xa8 AND ESI,0xc00000`, `+0x11c MOV EDX,ESI`; `0xc00000` 의 상수 이름은 모름). 받기 칸이 전부 0 이라 **돌아오는 순간 답이 없다.** 오류면 2-5 표 + 호출 객체 깃발 `[호출+0x12c] & 0x4000000` 일 때 비동기 상태를 오류 (`AsyncCliCallStateTransition(..., 2, 0)`) 로 (`+0x256 ~ +0x276`).

### 2-5. ALPC status → RPC 오류 표 (보내기 3 자리)

값은 `.asm` 의 `CMP` 와 착지 `MOV` 에서 읽은 것 (`Bind__4e330 +0x632 ~ +0x84c`, `DoSendReceive +0x15b ~`, `AlpcSend +0x134 ~ +0x302`). 이름은 일반 NTSTATUS 이름 `[이름 추측]`. RPC 오류는 숫자 우선: `0x6ba` 1722 서버 없음 · `0x6be` 1726 호출 실패 · `0x6bf` 1727 **실행 안 되고 실패** · `0x6c0` 1728 프로토콜 오류 · `0x71a` 1818 취소 · `0x6f7` 1783 · `0xe` 메모리 부족 · `5` 접근 거부.

| NTSTATUS | bind (`CASSOCIATION::Bind`, 동기·비동기 공통) | 동기 요청 (`DoSendReceive`) | 비동기 요청 (`AlpcSend`) |
|---|---|---|---|
| `0` | 성공 (동기: `CompleteBind` / 비동기: `0xc0021018`) | 성공 | 성공 |
| **`0xc0000008` (INVALID_HANDLE)** | **`0x6bf`** (기본 착지 `+0x823 MOV ESI,0x6bf`) | **`0x6bf`** | **`0x6bf`** (`+0x297 CMP EAX,0xc0000008`, `+0x29c JZ +0x224`) |
| `0xc0000037` (PORT_DISCONNECTED) | `0x6bf` | `0x6bf` | `0x6bf` (`+0x134 ~ +0x13b` 같은 착지 `+0x224`) |
| `0xc0000017` (NO_MEMORY) | `0xe` | `0xe` | `0xe` |
| `0xc0000044` (QUOTA_EXCEEDED) | `0xe` | `0xe` | `0xe` |
| `0xc000009a` (INSUFFICIENT_RESOURCES) | `0xe` | `0xe` | `0xe` |
| `0xc0000022` (ACCESS_DENIED) | `0x6bf` (표에 없음) | `0x6f7` | `0x6f7` |
| `0xc0000024` (OBJECT_TYPE_MISMATCH) | `0x6bf` | `0x72c` | `0x72c` |
| `0xc0000036` | `0x72a` | `0x72a` | `0x72a` |
| `0xc00000a5` (BAD_IMPERSONATION_LEVEL) | `0x6bf` | `5` | `5` |
| `0xc0000701` | 호출 객체 `[+0x120] & 1` (취소됨) 이면 `0x71a`, 아니면 `0x6bf` | 〃 이면 `0x71a`, 아니면 **`0x6be`** | `0x6be` |
| `0x102` (TIMEOUT) | `0x71a` | `0x71a` | `0x71a` |
| 그 밖 | `0x6bf` (`+0x823`) | **`0x6be`** (`LAB_180040a4a`) | **`0x6be`** (`+0x2e9`) |

- 오류가 나면 모두 `RpcpErrorAddRecord` 를 남기고, 동기 요청은 호출 상태 `[+0x10c] = 2`, 동기 bind 는 `AbortAssociation(오류)` 로 연결을 닫는다. 연결 (`NtAlpcConnectPort`) 쪽 표는 sast/127 2-1.
- `AlpcSendWaitReceivePort` (CASSOCIATION, `+0x3fa50`) 는 **상태를 바꾸지 않는 감싸개** (`+0xbb` 부르고 `+0xc2` 에서 `Flags & 0x20000` 이면 `MessageReceived`).
- `RpcpStrictAlpcToRpcStatus` (rva 0x99d70, 표 rva 0xed0d0 의 15쌍): `0 → 0` · `0xc0000037 → 0x6be` · `0xc0000703 → 0x6be` · `0xc0000017 → 0xe` · `0xc0000040 → 0xe` · `0xc0000097 → 0xe` · `0xc000009a → 0xe` · `0xc00000a1 → 0xe` · `0xc0000007 → 0xe` · `0xc000012d → 0xe` · `0xc0000702 → 0x6be` · `0xc0000022 → 0x6f7` · **`0xc0000008 → 6`** · `0xc0000024 → 0x72c` · `0xc0000023 → 0x7a`. **표에 없는 값이면 `RpcpReportFatalError(2, 상태)` (치명).** 호출자는 서버 쪽 (`HandleBindRequest`, `ReplyWithSmallData`, `AsyncSendFailure`, `FaultAlpcPipeAck` …) 이고, 클라이언트 `AlpcSend`·`DoSendReceive`·`Bind` 는 이 표를 쓰지 않는다 (위 표가 자기 안에 있다). 요청서가 말한 "핸들 0 → `0xc0000008`" 의 클라이언트 값은 **`6` 이 아니라 `0x6bf`**.

### 2-6. RPCSS "핸들 0 보내기" (`0xc0000008`) 뒤 rpcrt4 반환값 (combase 는 안 열었음)

- 보내는 자리가 bind 든 (`Bind__4e330 +0x62d`), 동기 요청이든 (`DoSendReceive +0x154`), 비동기 요청이든 (`AlpcSend +0x12f`) **RPC 오류는 `0x6bf`**. 동기 bind 는 거기에 더해 연결 객체를 닫는다 (`AbortAssociation(0x6bf)`).
- **다시 시도**: `LRPC_CASSOCIATION::GetErrorClass(0x6bf)` 와 `(0x6ba)` 는 "다시 시도 가능" 종류 (`GetErrorClass.asm +0x9 ~ +0x1d`, `+0x1f ~ +0x25`; `0x6be` 는 불가), `GetErrorAndErrorClass__91530` (`LRPC_BINDING_HANDLE` 의 가상 함수) 이 그걸 받아 `LRPC_BIND_CCALL::AttemptRetry` 를 부르고, `AttemptRetry` 는 카운터 둘 (`+0x2d8` < 4, `+0x2da` < 0x101) 이 넘으면 `0` 으로 포기 (`AttemptRetry.asm`, 디컴파일 `uVar3 < 4 && uVar2 < 0x101`) → **최대 첫 시도 + 3번 다시.** 재시도 실행은 `HandleSendReceiveFailure → RetryCall → DriveStateForward → DoSendReceive` (동기) 로 읽힌다 `[추측: HandleSendReceiveFailure 안의 가상 호출이 GetErrorAndErrorClass 인지는 안 확인]`. **이 길이 "핸들 0" 경우에 실제로 도는지·몇 번 도는지는 `[공백]`** (동적 6).
- combase `+0x55b5a` 가 주소 `0x4` 를 읽는 것과의 연결은 **rpcrt4 쪽에서는 알 수 없다** (rpcrt4 가 `0x6bf` 를 돌려줄 뿐). sast/128 (combase) 몫.
- 참고: 지금 PE_emulator 의 plain `NtAlpcConnectPort` 는 `0xc0000034` 를 돌려주므로 (`:192`) 연결 객체에 핸들 0 이 생기는 옛 길은 닫혔다. "핸들 0 보내기" 샘플은 옛 모습에서 나온 것일 수 있다 (`[추측]`).

---

## 3. epmapper `ept_map` 응답 검사 (질문 3, 3-1 ④)

### 3-1. 묻는 탑 (클라이언트가 만드는 것) — `TowerConstruct.asm`

호출: `EP_LOOKUP_DATA::ResolveEndpoint (__59d38) +0xba` (ret `+0xbf`) → `TowerConstruct(인터페이스 ID = [조회+0x44], 전송 문법 = [조회+0x58], "ncalrpc", 끝점 = NULL (R9 = 0), 네트워크 주소, &[조회+0x2d0])`. 조회 객체의 `+0x40 ~ +0xa0` 은 클라이언트 인터페이스 구조체 (0x60) 의 **복사** (`EpAllocateLookupHandle`) 라서 `+0x44` = 인터페이스의 `InterfaceId`, `+0x58` = 인터페이스의 **`TransferSyntax` (NDR 2.0 `8a885d04-…` v2.0 — winnsi 도 pefile 로 확인)**.

탑 한 개 = u32 길이 + 층들 (오프셋은 출력 버퍼 기준; 줄 `TowerConstruct.asm` 의 디컴파일 `*_Dst` 쓰기들):

| 위치 | 크기 | 값 | 뜻 |
|---|---|---|---|
| `+0x00` | u32 | `local_34 + 0x3b` = **0x42** (끝점 없을 때) | `tower_length` (뒤 바이트 수) |
| `+0x04` | u16 | **4** | 층 수 |
| **1층** `+0x06` | u16 | `0x13` | LHS 길이 (19) |
| `+0x08` | u8 | **`0x0d`** | 프로토콜 ID = UUID |
| `+0x09` | 16 | 인터페이스 GUID | (예: nsi `7ea70bcf-48af-4f6a-8968-6a440754d5fa`) |
| `+0x19` | u16 | 인터페이스 **주** 버전 | (nsi 1) |
| `+0x1b` | u16 | `2` | RHS 길이 |
| `+0x1d` | u16 | 인터페이스 **부** 버전 | (nsi 0) |
| **2층** `+0x1f` | u16 | `0x13` | LHS 길이 |
| `+0x21` | u8 | **`0x0d`** | UUID |
| `+0x22` | 16 | 전송 문법 GUID | NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` |
| `+0x32` | u16 | 전송 문법 주 버전 | 2 |
| `+0x34` | u16 | `2` | RHS 길이 |
| `+0x36` | u16 | 전송 문법 부 버전 | 0 |
| **3층** `+0x38` | u16 | `1` | LHS 길이 |
| `+0x3a` | u8 | **`0x0c`** (ncalrpc) / `0x0b` (ncacn_*) / `0x0a` (ncadg_*) | 프로토콜 ID |
| `+0x3b` | u16 | `2` | RHS 길이 |
| `+0x3d` | u16 | `0` | 부 버전 |
| **4층** `+0x3f` | u16 | `1` | LHS 길이 |
| `+0x41` | u8 | **`0x10`** (ncalrpc) | 프로토콜 ID |
| `+0x42` | u16 | **2** (끝점 없을 때) / 끝점 길이+1 | RHS 길이 |
| `+0x44` | | `00 00` (끝점 없을 때) / ASCII 끝점 + NUL | RHS |

- 끝점 없는 질문 탑의 총 크기 = 4 + 0x42 = **70바이트**. 끝점 문자열이 길이 `0xffff` 를 넘으면 `0xe`, 프로토콜 문자열이 `ncalrpc`·`ncacn_`·`ncadg_` 가 아니면 `0x6a8`.
- 값은 전부 `TowerConstruct` 의 쓰는 줄에서 읽었다 (GUID 는 `RPC_SYNTAX_IDENTIFIER` 의 메모리 모양 그대로 — 16바이트 + 주·부 u16). `[이름 추측]` 은 프로토콜 ID `0x0c` 의 이름만 (코드는 값만 쓴다).
- 호출 (`ept_map`) 의 입력 칸: `LookupNextChunk +0x11d` (ret `+0x122`) `Ndr64AsyncClientCall(프록시 0xe11e8, **프로시저 8**, NULL, 비동기상태 &[조회+0x308], [조회+0x2c0]=바인딩, &[조회+0x1c]=객체 UUID 16바이트, [조회+0x2d0]=질문 탑, SID(NULL), &[조회+0x2c8]=끝점 핸들(in/out), **max_towers = 4** (`+0xe6 MOV [RSP+0x48],4`), &[조회+0x2f8]=탑 개수, &[조회+0x2d8]=탑 포인터 4개 배열, &[조회+0xa0]=상태)`. 동기 갈래는 프로시저 3 (SID 없음) / 7 (SID 있음). MS-RPCE 의 EPM 연산 번호 (`ept_map` 3 · `ept_map_auth` 7 · **`ept_map_auth_async` 8**) 와 맞아 보인다 `[추측: 이름 대응]`.

### 3-2. 응답에서 클라이언트가 검사하는 것 — `ProcessNextChunk.asm` + `LookupNotifyRoutine.asm`

경로: 비동기 호출 완료 → `LookupNotifyRoutine +0x4d` (ret `+0x52`) `RpcAsyncCompleteCall` → `+0x69` (ret `+0x6e`) `ProcessNextChunk`.

| 검사 | 값 | 틀리면 | 줄 |
|---|---|---|---|
| 서버가 준 상태 (`[조회+0xa0]`) | **0** 이면 탑 처리 | `0x6bb` (RPC_S_SERVER_TOO_BUSY) → 카운터 `+0x2fc` ++, **3 미만이면 같은 요청 다시** (`+0x3e8 ~ +0x3fe`); 그 밖 → `FatalLookupErrorEncountered` (DCE 상태 `0x16c9a0cd → 0x6d8`, `0x16c9a0d6 → 0x6d9` 등 sast/127 표) | `+0x47 CMP [RAX],0x6bb`, `+0x53 CMP [RAX],R15D` |
| 탑 개수 (`[조회+0x2f8]`) | 0 이상, 탑 포인터 **NULL 이면 안 됨** | NULL 포인터 → `0x6c0` (`+0x93 JZ +0x443`, `+0x443 MOV EBX,0x6c0`) | `+0x78 ~ +0x93` |
| 탑 길이 | `[탑+0]` = `tower_length`, **`끝 = 탑+4+tower_length > 탑+6`** | `0x16c9a0cd` | `+0x99 ~ +0xa9` |
| **층 수** (`+0x04`) | **3 ≤ 층 수 ≤ 6** | `0x16c9a0cd` | `+0xaf ~ +0xc0` (`SUB AX,R12W(3); CMP AX,3; JA`) |
| 1·2층 | 길이 칸 (LHS·RHS u16) 이 `끝` 안에 맞게 걸어 넘어감. **값은 안 본다.** | 넘치면 `0x16c9a0cd` | `+0xc6 ~ +0x1b0` |
| **3층 프로토콜 ID** (`층3+2`) | `0x0a` / `0x0b` → TCP 갈래 (`OsfTowerExplode`), **`0x0c` → LRPC 갈래**, 그 밖 → 오류 | `0x16c9a0cd` | `+0x1bf ~ +0x1d8` |
| LRPC 갈래: **층 수 == 4** | | `0x16c9a0cd` | `+0x1e3 CMP R11D,4` |
| **4층 RHS (끝점 이름)** | u16 길이 (`층4+3`) **2 ≤ 길이 ≤ 1000** (`길이−2 ≤ 0x3e6`), 데이터 `층4+5` 가 `끝` 안, **마지막 바이트 NUL** (ASCII). 4층의 프로토콜 ID (`0x10`) 는 **안 본다**. | `0x16c9a0cd` | `+0x23d ~ +0x26a` |
| 이름 저장 | `AllocWrapper` + `memcpy` → `RtlMultiByteToUnicodeN` (ANSI → 와이드) → `[조회+0xc0+n*8]` 에 저장, `n = [조회+0xb4]++`; `n == [조회+0xb8]` (원하는 개수) 면 멈춤 | 메모리 `0xe` | `+0x2ea`, `+0x31b ~ +0x327` |
| 루프가 끝난 뒤 | `[조회+0x2c8]` (**끝점 핸들 out**) 이 **0** 이면 `0x6d9` 를 `[조회+0xa0]` 에 (= "더 없음" 표시), 0 이 아니면 **다음 조각을 요청** (`LookupNextChunk` 다시) | — | `+0x391 CMP [RDI+0x2c8],R15`, `+0x39e MOV EBX,0x6d9` |
| `0x6d9` 처리 (`LookupNotifyRoutine`) | **이미 얻은 이름이 있으면 (`[조회+0xb4] != 0`) 상태를 0 (성공) 으로 바꿈**, 없으면 `FatalLookupErrorEncountered` → 최종 **`0x6d9` (EPT_S_NOT_REGISTERED)** | | `LookupNotifyRoutine +0x7c CMP EAX,0x6d9`, `+0x83 CMP [RBX+0xb4],0` |
| 이름 꺼내기 | `ReturnLookupValues`: 꺼낸 개수 `[조회+0xbc]` 가 저장 개수 `+0xb4` 와 같으면 `0x6d9`, 아니면 문자열 복사 후 `+0xbc++` | `0x6d9` / 메모리 `0xe` | `ResolveEndpointComplete +0x3c` (ret `+0x41`), `ReturnLookupValues` |

### 3-3. 서버 흉내가 돌려줄 최소 탑 하나 (바이트 꼴, 끝점 이름 `LRPC-emu` 예)

`tower_length = 0x40 + 끝점길이(NUL 포함)`: 끝점 `LRPC-emu` (8자 + NUL = 9) → `0x49`. 클라이언트가 실제로 보는 칸은 굵게 (나머지는 "값은 안 본다" 이지만 진짜 서버가 쓰는 모습으로 채우면 안전).

```
+0x00  49 00 00 00                     tower_length = 0x49
+0x04  04 00                           층 수 = 4              ← 3~6, 0x0c 갈래는 정확히 4
+0x06  13 00  0d  cf 0b a7 7e af 48 6a 4f 89 68 6a 44 07 54 d5 fa  01 00   02 00  00 00      1층 nsi 7ea70bcf… v1.0
+0x1f  13 00  0d  04 5d 88 8a eb 1c c9 11 9f e8 08 00 2b 10 48 60  02 00   02 00  00 00      2층 NDR 2.0 v2.0
+0x38  01 00  0c  02 00  00 00                                                              3층 ← 프로토콜 ID 0x0c
+0x3f  01 00  10  09 00  4c 52 50 43 2d 65 6d 75 00                                          4층 ← RHS 길이 9, "LRPC-emu\0"
```

- 총 `4 + 0x49 = 77` 바이트. 클라이언트는 4층 RHS 의 ASCII 이름을 와이드로 바꿔 **`\RPC Control\` + 이름** 으로 두 번째 `NtAlpcConnectPort` 를 한다 (`TransformLrpcEndpointToAlpcPortName`, sast/127 2-3 단계 7). 이름은 서버 흉내가 마음대로 고를 수 있다 (코드가 문자열 모양을 따로 안 본다: 길이 2~1000 + NUL 만).
- `[정적]` 위 층 값 (`0x0d`·`0x0c`·`0x10`, 길이 칸 `0x13`·`0x02`·`0x01`) 은 `TowerConstruct` 가 쓰는 값이라 진짜 서버 (epmapper) 가 보관한 탑과 같은 꼴일 가능성이 높다 `[추측: epmapper 가 등록 때 받은 탑 그대로 돌려준다]`.
- **NDR 틀 (이 탑이 들어가는 `ept_map` 출력 본문) 은 안 읽었다 `[공백]`**: 공개 EPM IDL 의 `ept_map` 출력 순서 (`entry_handle` 컨텍스트 핸들 20바이트 → `num_towers` u32 → `towers[]` (conformant varying 배열: max_count, offset, actual_count, 포인터 id 들, 그리고 각 탑 `max_count = tower_length` + `tower_length` + 8의 배수로 채운 바이트) → `status` u32) 로 짐작되지만 **프로시저 8 의 형식 문자열 (프록시 `0xe11e8`) 은 안 풀었다.** 동적 4 의 진짜 바이트와 대조 필요.
- 끝점 핸들을 NULL (20바이트 0) 로 주면 한 번에 끝난다 (3-2 마지막 두 줄). NULL 이 아니면 클라이언트가 **다음 `ept_map` 요청을 또 보낸다.**

---

## 4. nsisvc 서버 쪽 (질문 4)

### 4-1. 시작과 끝점 이름 — `nsisvc/ServiceMain.asm`, `NsiStartRpcServer.asm`

| 단계 | 호출 | 값 | 줄 |
|---|---|---|---|
| 서비스 시작 | `ServiceMain` → `NsiServiceInitialize` (힙·잠금·`NsiServiceStopEvent`·우선순위 `0x8000`) → **`NsiStartRpcServer`** → `NsiServiceUpdateStatus`/`NsiServiceNotifyStart` (`RtlPublishWnfStateData`) | | `ServiceMain +0xed` (ret `+0xf2`), `+0xf6` (ret `+0xfb`), `+0x166` (ret `+0x16b`) |
| 보안 설명자 | `ConvertStringSecurityDescriptorToSecurityDescriptorW` | `D:(A;;GRGWGX;;;WD)(A;;GRGWGX;;;RC)(A;;GA;;;BA)(A;;GA;;;OW)(A;;GR;;;AC)(A;;GR;;;S-1-15-3-1)(A;;GR;;;S-1-15-3-2)(A;;GR;;;S-1-15-3-3)(A;;GR;;;S-1-15-3-4214768333-1334025770-122408079-3919188833)` | `NsiStartRpcServer +0xac` (ret `+0xb3`) |
| **프로토콜·끝점** | `RpcServerUseProtseqW(L"ncalrpc", 10, SD)` — **끝점 인자 없음 = 동적 끝점** | `ncalrpc`, `MaxCalls 10` | `+0x123` (ret `+0x12a`) |
| 인터페이스 등록 | `RpcServerRegisterIf3(&인터페이스 0x6420, NULL, NULL, **Flags 9**, **MaxCalls 0x4d2 (1234)**, MaxRpcSize 0, **IfCallback = `NsiSecurityCallBack` (rva 0x2e80)**, SD)` | Flags 9 = `RPC_IF_AUTOLISTEN`(1) + `RPC_IF_ALLOW_SECURE_ONLY`(8) `[이름 추측]` | `+0x169 ~ +0x1a1` (ret `+0x1a8`) |
| 정리 이벤트 | `CreateEventW` + `RegisterWaitForSingleObjectEx(NotificationCleanupEvent, CleanupNotificationThread, …, 0x10)` | | `+0x1fb`, `+0x28a` |
| **epmapper 등록** | `RpcServerInqBindings(&벡터)` → **`RpcEpRegisterW(&인터페이스, 벡터, NULL, L"NSI server endpoint")`** | 성공이면 `RpcServerInitialized = 1` | `+0x2de` (ret `+0x2e5`), `+0x337` (ret `+0x33e`) |
| 동적 끝점 이름 | `LRPC_ADDRESS::ServerSetupAddress` (rpcrt4 0x971d0): `L"LRPC-"` + 난수 9바이트를 소문자 16진 **18자** (`ProcessPrng` 9바이트) → **`LRPC-` + 18자 = 23자**; 이미 있으면 (`0x6cc`) 다시 | 예: `LRPC-0123456789abcdef01` → 포트 `\RPC Control\LRPC-0123456789abcdef01` | `ServerSetupAddress +0xc4 ~ +0xd1` (문자열 rva 0xeced8), `ProcessPrng` 호출 |

- 인터페이스 구조체 (nsisvc rva 0x6420, `pefile`): `Length 0x60`, `InterfaceId 7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, 전송 문법 NDR 2.0 v2.0, 깃발 **`0x6000000`** (비트 25·26 — 클라이언트 `Bind` 가 읽는 "문법 표 있음"·"표 선택" 비트와 같은 자리 `[추측: 서버 구조체 모양이 같다고 본 것]`), 기본 끝점 0개. 디스패치 표 (rva 0x64f0, 프로시저 9개): **프로시저 0~6·8 = `NdrServerCall2` 썽크 (0x3ac0, 동기 서버)**, **프로시저 7 = `NdrAsyncServerCall` 썽크 (0x3a90, 비동기 서버)**. 서버 정보 (`MIDL_SERVER_INFO`, rva 0x6600).
- 서버 쪽 함수는 **nsisvc 안에 있다**: `RpcNsiRegisterChangeNotification` (0x27c0, 프로시저 5), `RpcNsiRequestChangeNotification` (0x1010, 프로시저 7), `RpcNsiDeregisterChangeNotification` (0x16f0, 프로시저 6), `RpcNsiParameterChange` (0x1e10), `RpcNsiGetParameter` 등 (0 ~ 4). 실제 NSI 작업은 **`nsi.dll` 의 `NsiRegisterChangeNotificationEx`** (지연 로드, `DelayLoad_NsiRegisterChangeNotificationEx` rva 0x3a0f) 가 한다 (nsi 쪽 rva 0x1eb0, 72바이트 — 안 열었음).
- ⚠ 클라이언트는 `RpcBindingSetAuthInfoW(…, 인증 수준 6, 서비스 10)` 를 걸지만 (sast/127 1-2), LRPC 의 bind·요청 경로에서 **SSPI 협상 왕복 같은 별도 메시지는 코드에서 못 찾았다** (보안은 ALPC Security 속성 (0x80000000, 요청 속성 `Valid` 에 항상 들어감) 으로 간다 `[추측: 보안 협상 본문은 범위 밖이라 안 읽음]`). 서버 쪽 인터페이스 콜백 `NsiSecurityCallBack` (rva 0x2e80) 은 **디컴파일이 빠뜨린 검사가 있다**: `RpcServerInqCallAttributesW(문맥, {Version 2, Flags 0})` 가 실패하면 그 오류를, 성공이면 **호출 속성의 `AuthenticationLevel` (속성 `+0x28` = `[RSP+0x48]`) 이 6 미만이면 `5` (접근 거부), 6 이상이면 `0` (허용)** 을 돌려준다 (`NsiSecurityCallBack.asm +0x5a` (ret `+0x61`) `CALL RpcServerInqCallAttributesW`, `+0x66 TEST EAX,EAX`, `+0x83 CMP [RSP+0x48],6`, `+0x88 JNC`, `+0x8a MOV EAX,5`). 클라이언트가 이 수준 (6) 을 서버에 **어떻게 알리는지는 안 읽었다** `[공백]` (bind·요청 머리에는 인증 칸이 안 보임 — 서버 쪽 rpcrt4 가 ALPC 보안 속성에서 정하는 것으로 짐작 `[추측]`). 서버 흉내는 이 검사를 흉내낼 필요 없이 "허용" 으로 두면 된다.

### 4-2. 프로시저 5 (`RpcNsiRegisterChangeNotification`) 가 입력을 어떻게 다루나

호출 약속 (x64): `RCX = 바인딩 핸들`, `RDX = 입력 구조체 포인터 (24바이트: {예약 u64, 모듈ID 포인터, 객체 번호 u32})`, `R8 = compartment (8바이트 값)`, `R9 = 출력 컨텍스트 핸들 포인터`. 반환 DWORD.

| 단계 | 하는 일 | 실패값 | 줄 (`RpcNsiRegisterChangeNotification.asm`) |
|---|---|---|---|
| 0 | `*컨텍스트 = 0` | | `+0x5c MOV [R9],R12` |
| 1 | `RpcServerInqCallAttributesW(0, {Version 2, Flags 0x10 = 클라이언트 PID 요청})` | 실패 → 그 오류 (`+0x300 MOV EAX,EDI`) | `+0x69`, `+0x88` (ret `+0x8f`), `+0x94 ~ +0x98` |
| 2 | **호출 속성 `KernelModeCaller` (속성 구조체 `+0x34`) 가 0 이어야** | 0 이 아니면 **`0x490`** (1168, WPP 기록 `0x14`) | `+0x9e CMP [RBP-0x1c],R12D`, `+0xa2 JNZ +0x307`, `+0x33b MOV EAX,0x490` |
| 3 | 입력 24바이트 + compartment + 클라이언트 PID 를 스택 구조체로 모음 (**내용 검사 없음**) | | `+0xa8 ~ +0xe3` |
| 4 | `MALLOC(0x98)` (컨텍스트 블록, 0 으로 채움) | 실패 → **`8`** | `+0xe6` (ret `+0xeb`), `+0x345 MOV EAX,8` |
| 5 | `RpcImpersonateClient(바인딩)` | 실패 → 그 RPC 오류 (블록 `HeapFree`) | `+0x12c` (ret `+0x133`), `+0x13c JNZ +0x24c` |
| 6 | **`nsi!NsiRegisterChangeNotificationEx(&구조체)`** — 지연 로드 import (`DelayLoad_NsiRegisterChangeNotificationEx`). 반환 0 = 성공, 출력 `[RSP+0x50]` = **nsi 쪽 커널 등록 핸들** | 0 이 아니면 **그 값 (Win32 오류)** 이 반환값, 블록 해제 | `+0x142 LEA RCX,[RSP+0x60]`, `+0x147` (ret `+0x14e`), `+0x153 MOV ESI,EAX`, `+0x15c RpcRevertToSelf` |
| 7 | 블록 채우기: `+0x14` PID, `+0x18` 0, `+0x20` 대기 중 변경 목록 머리 (자기 자신), `+0x58` 입력 24바이트 복사, `+0x70` 모듈 ID 24바이트 복사 (`+0x60` 이 그 주소), `+0x88` compartment, `+0x90` nsi 핸들 | | `+0x169 ~ +0x1ba` |
| 8 | `NsiKernelClientNotificationListLock` 잡고, **`RpcServerInitialized == 0` 이면** 블록 해제 (`NsipDeregisterAndFreeNotificationContext`) + **`0x139f` (5023)** | `0x139f` | `+0x1e4 CMP [RpcServerInitialized]`, `+0x34f ~ +0x36a` |
| 9 | 전역 목록 (`NsiKernelClientNotificationList`) 끝에 블록을 이어 붙이고 (`+0x1f1 ~ +0x215`) **`*컨텍스트 = 블록 포인터`** (`+0x22a MOV [R15],RDI`), **반환 0** (`+0x228 XOR EAX,EAX`) | | `+0x1d1 ~ +0x22a` |

- **컨텍스트 핸들 = 블록 (0x98 바이트) 포인터**. NDR 가 이걸 "컨텍스트 핸들" 로 싸서 네트워크 위 20바이트 (`u32 속성 0` + `GUID`) 로 내보낸다 (sast/127 3-2 와 같은 값) — 서버 포인터가 직접 나가는 게 아니다. **클라이언트는 그 20바이트를 그대로 되돌려 보낼 뿐 해석하지 않는다.** 흉내는 0 이 아닌 20바이트면 된다 (속성 0 + 새 GUID, 짐작 `[추측]`).
- 프로시저 7 (알림 기다림) 은 컨텍스트 블록 (`+0x10` 깃발, `+0x30` 걸린 호출) 의 상태를 본다 (4-3). 프로시저 6 (해제, `RpcNsiDeregisterChangeNotification` 0x16f0) 은 **이번엔 안 읽었다** `[공백]`. 컨텍스트 핸들 자체의 유효성 (서버가 발급한 것인지) 은 서버 쪽 NDR 런타임이 대조하므로, 서버 흉내가 프로시저 6·7 의 입력을 검사해야 하면 발급한 20바이트를 보관해 두는 게 안전 `[추측]`.
- **흉내가 같은 대답을 내려면 "무엇을 봐야 하나"**: 입력 값 자체는 안 봐도 된다 (nsisvc 가 안 봄; 커널 NSI 가 봄). 성공 대답은 **{컨텍스트 핸들 20바이트, 반환 0}**. 서버 코드를 흉내하려면 "호출 속성 (커널 모드 호출자 아님)" 과 "RPC 서버 초기화됨" 만 참으로 두면 된다. 입력이 틀린 경우의 대답 (모듈 ID 가 없는 모듈 등) 은 **nsi.dll → 커널 NSI 가 돌려주는 Win32 오류** 라서 정적으로 못 정함 `[공백]` (다음 요청).

### 4-3. 프로시저 7 (`RpcNsiRequestChangeNotification`) — "답하지 않는다" 가 서버 코드와 맞다

- 인자: `(비동기 상태, 바인딩, 컨텍스트 블록, 출력 16바이트, 출력 24바이트 모듈ID, 출력 DWORD)`.
- 먼저 `RpcImpersonateClient(바인딩)` (실패하면 그 오류로 완료). 그 뒤: **컨텍스트가 NULL → `0x57`**, **서버 초기화 안 됨 (`RpcServerInitialized == 0`) 또는 블록 `+0x10 == 1` (이미 닫힘) → `0x4c7`** (1223), **블록에 이미 걸린 호출이 있으면 (`+0x30 != 0`) → `0x57`** (`RpcNsiRequestChangeNotification.asm +0x7d CMP [RBX+0x10],1`, `+0x87 CMP [RBX+0x30],0`, `+0x18b MOV [RSP+0x50],0x4c7`, `+0x29a`·`+0x383 … 0x57`). 그 밖에 **대기 중인 변경이 없으면** 비동기 상태·출력 포인터를 블록 (`+0x30 ~ +0x50`) 에 보관하고, `RpcServerSubscribeForNotification(호출, 1, 5, NsipClientDisconnectCallback)` (클라이언트 끊김 알림) 만 걸고 **`RpcAsyncCompleteCall` 을 부르지 않은 채 돌아간다** (= 호출은 걸려 있음). 변경이 이미 쌓여 있으면 하나 꺼내 즉시 완료.
- 서버 흉내가 이걸 안 끝내면 클라이언트 (winnsi) 는 프로시저 7 을 걸어 둔 채 등록 성공으로 끝난다 (sast/127 3-1 ⑧).

---

## 5. sast/127 3-1 표를 고쳐 다시 적은 것 (서버 흉내가 **차례대로** 할 일)

⚠ 바로잡음: (a) EPM 인터페이스의 제안 마스크는 **3** (127 2-3 의 8단계는 NDR 2.0 하나만 적었음; 3-1 ③ 의 "둘" 이 맞다). (b) 127 의 "`ept_map` 은 비동기 프로시저 8 (또는 3·7)" 은 맞다 — 요청 머리 `+0x3c` 에는 `LookupNextChunk` 가 `Ndr64AsyncClientCall` 에 넘긴 **8** 이 그대로 들어간다 (동기 3·7 은 SID 없음/있음 동기 갈래). (c) 모든 "보내기" 가 `NtAlpcSendWaitReceivePort` 한 번으로 끝나는 것처럼 읽히지만 **비동기 대화는 "보내기 (받기 칸 없음)" + "완료 통지 + 받기 한 번 더"** 두 단계다 (2-4).

| 단계 | 클라이언트가 보내는 것 | 서버 흉내가 돌려줄 것 | 근거 |
|---|---|---|---|
| ① epmapper 연결 | `NtAlpcConnectPort(\RPC Control\epmapper, …)`, **InMessageAttributes = 빈 8바이트 버퍼 (항상 비-NULL)**, Flags `0` (비동기) / `0x20000` (동기) | **핸들을 `*PortHandle` 에 쓰고 상태 0**. 빈 버퍼의 `ValidAttributes (+4)` 에는 0 을 써 줌 (받은 속성 없음). 비동기이면 이어서 완료 통지 연결 (`NtAlpcSetInformation`, `TpAllocAlpcCompletionEx` 안) | sast/127 2-1 · `AlpcConnect +0x293` |
| ② 핸들 질문 | `NtAlpcQueryInformation(핸들, 0, 0x10)` | 오류여도 됨 (1 = "공유 섹션 있음" 취급 → 이후 받기 버퍼 길이 `0x1000`) | sast/127 |
| **③ epmapper bind** | **비동기**: `NtAlpcSendWaitReceivePort(핸들, **0**, bind 메시지 0x70바이트, 속성 {Allocated=Valid=`0x20000000`, **MessageContext = BIND_CCALL\***}, NULL, &0x1000, NULL, NULL)`. 메시지: `+0x28 = 1`, `+0x34` = **`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0**, **`+0x48 = 3`**, `+0x4c`·`+0x4e` = 슬롯 두 개, `+0x54 = 0x2000000` (1-2 표). **동기**였다면 Flags `0x20000`, 같은 버퍼로 받기 | 보내기 호출은 **즉시 0** (대기 없음). 그다음 **완료 통지** 를 띄우고, 클라이언트의 받기 (`Flags 0 또는 0x1000000`, 새 버퍼 `0x1000`, 받기 속성 Allocated `0xfa000000`) 에 **ALPC 종류 2 · LRPC `+0x28 = 1` · `+0x48` = 3 의 부분집합 중 하나 (1 또는 2) · `+0x08` PID · 받기 속성 Context 의 `MessageContext` = 보낸 값 그대로** 의 메시지를 줌. (동기였다면 같은 버퍼에 덮어씀.) **길이 검사 없음.** | 1-1 ~ 1-3 |
| **④ `ept_map` 요청 → 응답** | 요청 머리 (2-1): `+0x28 = 0`, `+0x34` = 호출 ID, `+0x38` = ③ 에서 서버가 받은 슬롯 (수락한 문법의 것), **`+0x3c = 8`**, 스텁 `+0x68` 부터 (질문 탑 70바이트 포함 `[추측: NDR 틀]`). `NtAlpcSendWaitReceivePort(핸들, **Flags `[호출+0x244] & 0x800000 ? 0xc00000 : 0`**, msg, attrs {Allocated `0xfe000000`, Valid `0xa2000000` 보통}, NULL ×4)` | 보내기는 **0 으로 즉시**. 완료 통지 + 받기에 **응답**: ALPC 종류 2, `+0x28 = 3`, `+0x30 = 0`, **`+0x34` = 요청의 호출 ID**, `TotalLength = 0x40 + 스텁길이`, 스텁 `+0x40` 부터 = 상태 0 + 탑 개수 1 + **3-3 의 탑** + 끝점 핸들 NULL, 받기 속성 Valid 에 DataView **없음**, Context 있음. 탑이 없으면 클라이언트는 `0x6d9` | 2-1 ~ 2-4, 3-1 ~ 3-3 |
| ⑤ 두 번째 ALPC 연결 | `\RPC Control\<탑 4층의 끝점 이름>` | ① 과 같은 응답 | 같은 `AlpcConnect` |
| **⑥ nsi bind** | ③ 과 같은 꼴. `+0x34` = **`7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0**, **`+0x48 = 3`**, `+0x54 = 0x2000000` | ③ 과 같은 꼴 (수락 마스크 하나) | 1 절, winnsi 인터페이스 (rva 0x67b0 + 전송 문법 표 0x6150 두 줄: NDR 2.0 · NDR64 순) |
| **⑦ 프로시저 5** | 요청: `+0x3c = 5`, `+0x38` = ⑥ 의 슬롯, 스텁 = 입력 24바이트 구조체 + 모듈ID 24바이트 + compartment 8바이트 (**NDR 본문은 형식 문자열 0x66e0 으로 짐작 `[추측]`**) | 응답: `+0x28 = 3`, `+0x34` 호출 ID 그대로, 스텁 = **컨텍스트 핸들 20바이트 + 반환 DWORD 0** (NDR 정렬로 24바이트 `[추측]`) | 4-2, sast/127 3-2 |
| ⑧ 프로시저 7 | 요청: `+0x3c = 7`, 스텁 = 컨텍스트 핸들 20바이트 | **답하지 않는다** (서버 코드도 완료하지 않고 걸어 둠) | 4-3 |
| ⑨ 성공 뒤 클라이언트의 시스템 호출 | — | 가짜로 답할 것 아님 (127 그대로) | sast/127 |

- ③·⑥ 의 `[공백]` 은 **채웠다** (1 절). ④ 의 `[공백]` (탑 안 층 번호·검사 칸) 도 **채웠다** (3 절). 남은 `[공백]`: **NDR 스텁 본문 (④ 요청·응답, ⑦ 요청·응답) 의 정확한 바이트**, 서버가 ③·⑥ 에서 **NDR 2.0 을 고르는지 NDR64 를 고르는지**.

---

## 6. 지금 우리 (PE_emulator) 맞나·틀리나

(참고: `PE_emulator/ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp`, 읽기만. **sast/127 때와 파일이 바뀌어** 줄 번호·내용이 다르다. 아래는 읽은 시점 파일 기준.)

| 줄 | 지금 코드 | 맞나·틀리나 |
|---|---|---|
| **`:190 ~ :192`** (`NtAlpcConnectPort`) | `live_server_required = a[5] \|\| a[6] \|\| a[7] \|\| a[8] \|\| a[9]`; `if (!known \|\| live_server_required) return err(0xc0000034)` | **"서버 없음" 흉내로는 맞다** (`0xc0000034` → rpcrt4 `0x6ba`, 연결 객체·바인딩 핸들 상태가 풀리고 호출이 끝남, sast/127 2-2). 그러나 **rpcrt4 의 plain 연결은 a[9] (InMessageAttributes) 에 항상 빈 8바이트 버퍼를 줘서 `live_server_required` 가 항상 참** → 이 줄은 **어떤 이름이든 `0xc0000034`** 로만 간다 (서버 모델을 켜 놓은 포트도). T2 를 하려면 "a[9] 가 가리키는 8바이트의 `AllocatedAttributes` 가 0 이면 속성 없음" 으로 보고, 서버 모델이 있는 이름 (`\RPC Control\epmapper`, 두 번째 연결의 `\RPC Control\LRPC-…`) 은 **핸들을 쓰고 성공** 하도록 고쳐야 한다. a[5]·a[6]·a[7]·a[8] 은 rpcrt4 가 항상 NULL/0 (서버 SID 는 `0xc00002a0` 다음에만 채워짐, sast/127 2-1). |
| **`:305 ~ :307`** (`NtAlpcConnectPortEx`) | 같은 규칙 + `if (a[5]) stopped(...)` | 같은 이야기. Ex 길 (`AlpcConnect +0x3b9`) 은 `gDefaultSecurityDescriptor` 가 비-NULL 일 때만 (sast/127 2-1 마지막), a[5] = SD 가 채워져 `:308` 에서 `stopped`. **로그가 plain 이었으니 T2 에는 해당 없음.** |
| **`:346`** (`if (local) return stopped("alpc_server_accept_unsupported")`) | 샘플이 직접 만든 포트에 연결할 때만 | 영향 없음 (rpcrt4 → epmapper/nsisvc 는 시스템 포트). |
| **`:602`** (`NtAlpcSetInformation`) | `information_class != kMessageZoneInformation → stop("alpc_set_information_class_unsupported")` | 비동기 길이 `TpAllocAlpcCompletionEx` 안에서 `NtAlpcSetInformation` 을 **다른 클래스 (완료 포트 연결로 짐작 `[추측]`)** 로 부를 것이라 **여기서 멈출 것** — 이번 요청 범위 밖이라 ntdll 은 안 읽음 (sast/127 다음 요청 4). |
| **`:612 ~ :626`** (`NtAlpcSendWaitReceivePort`, 요청서의 `:623`) | 메시지 전달을 모델하지 않고 `alpc_service_protocol_unverified` / `alpc_message_transfer_unsupported` | **"서버 흉내를 넣을 자리" 가 맞다.** 인자 모양 (`a[0]` 핸들, `a[1]` Flags, `a[2]` 보낼 메시지, `a[3]` 보낼 속성, `a[4]` 받을 메시지, `a[5]` 버퍼 길이 포인터, `a[6]` 받을 속성, `a[7]` 타임아웃) 은 rpcrt4 의 부름과 같다. 이 자리에서 할 일 (아래). |

**`:612 ~ :626` 에 서버 흉내를 넣을 때 필요한 것 (이번 정적 분석에서 닫힌 만큼)**

1. **받는 쪽 분류** — `a[2] != 0` (보냄) 인 호출: `Flags & 0x20000` → **동기**: 같은 버퍼 (`a[4] == a[2]`) 에 답을 덮어쓰고 `a[6]` 속성도 갱신, 반환 0. `Flags & 0x20000 == 0` → **비동기**: 받기 칸 (`a[4]`, `a[6]`, `a[7]`) 이 0 이므로 **즉시 0 반환**, 답은 나중 (아래 3).
2. **보낸 메시지 해석**: `+0x28` 이 **1 = bind**, **0 = 요청**. bind 는 `+0x34` UUID 로 어느 서비스인지 안다 (EPM / nsi), 마스크 `+0x48` 의 비트 하나를 골라 `+0x48` 에 써서 답. 요청은 `+0x3c` 프로시저 번호, `+0x34` 호출 ID 를 보관.
3. **비동기 답 배달** (`a[2] != 0 && a[4] == 0`): `NtAlpcSetInformation` 으로 연결된 완료 통지 (스레드풀 ALPC 객체) 를 올려, 클라이언트의 `IoCompleted` 가 **받기만 하는 두 번째 `NtAlpcSendWaitReceivePort`** (`a[2] == 0`, `a[3] == 0`, `a[4]` 새 버퍼, `a[5]` = `0x1000` 또는 `0xff00`, `a[6]` 받기 속성 Allocated `0xfa000000`, `a[1]` = `0` 또는 `0x1000000`) 를 부르게 한다. 그 호출 (`a[2] == 0 && a[4] != 0`) 은 쌓인 답을 꺼내 준다: **PORT_MESSAGE 종류 2, `MessageId`/`CallbackId` 는 완료 문맥의 것, Context 속성 (`+0x48` 쪽 `MessageContext`) 은 요청 때 받은 것 그대로.** 받기 호출이 모르는 오류를 받으면 클라이언트는 **치명 종료** (`RpcpReportFatalError`, 2-4) 하니 처음엔 "쌓인 답 없음" 도 정상 상태 (0) 로 돌려주지 말고 (알 수 없음 `[공백]`) 동적으로 확인.
4. **`Valid` 속성**: 답에 Context (`0x20000000`) 를 켜고 DataView (`0x40000000`) 는 **끄고** (작은 응답), Handle (`0x10000000`) 도 끈다 (켜면 시스템 핸들 복원으로 감).
5. **상태 → RPC 오류**: 서버 흉내가 오류를 돌려주고 싶으면 **2-5 표의 NTSTATUS 만** (표에 없으면 `IoCompleted` 받기에서 치명). 대화 오류는 보통 "응답 대신 `+0x28 = 2` fault + `+0x30` = RPC 오류" 로 (2-3) — 이게 가장 안전.
6. **T1 ("서버 모델 없는 포트") 는 지금 그대로 둔다**: `0xc0000034` → `0x6ba`. **다만 `0x6ba`·`0x6bf` 는 재시도 종류 (최대 3번 다시)** 라서 같은 연결이 **한 호출에서 최대 4번** 올 수 있다 (2-6, 정적으로 코드는 확인, 실제 도는지는 동적 6). 에뮬레이터가 "연결 요청 횟수" 를 세면 놀라지 말 것.

**공통 몫 (LSA `GetUserName*`·RPCSS `CoInitializeSecurity` 도 같다)**: rpcrt4 LRPC 클라이언트의 모든 것 — 연결 인자·상태 표 (sast/127 2-1), bind 메시지·답 (1 절), 요청·응답 머리 (2-1·2-3), 속성 규칙 (2-2), ALPC 상태 → RPC 오류 표 (2-5), 재시도 (2-6), 비동기 답 배달 (2-4). **다른 점**: LSA·RPCSS 는 고정 끝점 (`lsasspirpc` 류·`epmapper` 자체) 이라 `ept_map` 단계가 없을 수 있다 — 그 서비스의 bind 는 같은 1 절 그대로. 동기 호출이면 `Flags 0x20000` + 같은 버퍼 갈래 (2-1·1-1 표의 "동기" 열).

**winnsi/epmapper 몫**: 인터페이스 두 개의 UUID·버전·마스크 3 (1-2), `ept_map` 탑 (3 절), nsi 프로시저 5·6·7 (4 절), 등록 최대 4번 (sast/127) 과 클라이언트 쪽 성공 후 시스템 호출.

---

## 동적으로 확인할 것 (정적으로 못 닫은 것 — Q231 ⑤ 와 합치기 좋게, `caller+0x…` = `ret_off`)

유저 모드 (test.exe 프로세스). 모두 함수 입구가 아니라 **돌아온 자리** 에 건다. `ntdll!NtAlpcSendWaitReceivePort` 입구에 조건 (프로세스 = test.exe, 반환 주소 = rpcrt4 + 아래 ret) 을 걸어도 같은 값을 얻는다.

| # | 볼 것 | 자리 (rpcrt4) | 레지스터·메모리 |
|---|---|---|---|
| 1 | **bind 메시지 진짜 바이트·동기냐 비동기냐** | `LRPC_CASSOCIATION::Bind +0x62d` (ret, call `+0x626`). 입구에서 보려면 `ntdll!NtAlpcSendWaitReceivePort` 입구 + 반환 주소 `poi(@rsp)` = rpcrt4 기준 `+0x62d` | 입구: `r8` = 메시지 (`db @r8 L0x70`), `edx` = Flags (**0 이면 비동기, 0x20000 이면 동기**), `r9` = 속성 (`dq @r9 L5`: Allocated·Valid 한 qword, 뒤 `MessageContext` = BIND_CCALL\*), 스택 `[rsp+0x28]` = ReceiveMessage · `[rsp+0x30]` = BufferLength 포인터 · `[rsp+0x38]` = ReceiveAttrs · `[rsp+0x40]` = Timeout (**비동기면 `[rsp+0x28]`·`[rsp+0x38]`·`[rsp+0x40]` 이 0**). 돌아온 뒤 `rbx` 는 같은 메시지 (동기이면 답으로 덮어써짐), `eax` = 상태. **bind 가 epmapper 용인지 nsi 용인지는 `+0x34` 의 UUID 로.** |
| 2 | **bind 답 (진짜 서버의 선택)** | `CompleteBind` 입구 (`rcx` = 연결 객체, `rdx` = 메시지) 또는 `HandleReply__41bb0`/`__417f0` 의 그 호출 (`ret_off` 는 `_calls.tsv` 에서 `CompleteBind` 줄) | `db @rdx L0x70`: **`+0x28`, `+0x48` (수락 마스크: 1 인가 2 인가), `+0x08` (PID), `+0x30`**. 이것이 "NDR 2.0 vs NDR64" 의 답. |
| 3 | **요청 머리 진짜 바이트** | `DoSendReceive +0x154` (동기, call `+0x14d`) / `AlpcSend +0x12f` (비동기, call `+0x128`) | 동기: `rdi` = 메시지 (`db @rdi L0x80`), `r9`/`rsi` = 속성 (`dd @rsi L12`); 비동기: `rbp` = 메시지, `r14` = 속성, `esi` = Flags (**0 인가 `0xc00000` 인가**). 머리: `+0x28 +0x30 +0x34 +0x38 +0x3c` 와 `+0x68` 부터 스텁 (EPM `ept_map` = `+0x3c == 8`, nsi = `5`). |
| 4 | **응답 머리·`ept_map` 응답 NDR 본문** | `IoCompleted +0xcf` (받기 호출의 ret) | `rbx` = 받은 메시지 (`db @rbx L0x90`), `rbp-0x59` = 받기 속성 시작 (`dd @rbp-0x59 L8`), 그 안 Context 칸의 `MessageContext` = `rbp-0x11` (`dq @rbp-0x11 L1` — 요청 때 보낸 `LRPC_BASE_CCALL*` 와 같아야), `eax`/`rsi` = 받은 호출의 반환 (0 이 아니면 치명 갈래로 감). **`+0x28`, `+0x34`, `+0x40` 부터 스텁 = 진짜 `ept_map` 본문 (3-3 의 NDR 틀 `[공백]` 을 닫음).** |
| 5 | **프로시저 5 요청·응답 본문** | 같은 자리 (nsi 의 호출일 때: 요청은 3, 응답은 4) | 요청 `+0x68` 부터, 응답 `+0x40` 부터 = 입력 24바이트·모듈 ID·compartment / 컨텍스트 핸들 20바이트 + DWORD. |
| 6 | **재시도가 도는지** | `LRPC_BIND_CCALL::AttemptRetry` (rva 0x91634) 입구, `GetErrorClass +0x…` | 서버 없음 (`0x6ba`)·핸들 0 (`0x6bf`) 일 때 `[rcx+0x2d8]` 카운터가 1·2·3 으로 올라가는지, 연결이 몇 번 시도되는지 (`AlpcConnect +0x29a` 반복). |
| 7 | **plain 인지 Ex 인지** (sast/127 이월) | `AlpcConnect +0x27a` 의 `rdx` | 이월 항목 (`gDefaultSecurityDescriptor`). |

## 다음 요청 후보

1. **서버 쪽 rpcrt4 (`LRPC_ADDRESS`)** — `HandleBindRequest`·`ReplyWithSmallData`·`SendReply`·`LRPC_SCALL::…`: 진짜 서버가 bind 답과 응답을 **어떻게 쓰는지** (이번엔 "클라이언트가 검사하는 칸" 까지만 → 검사 안 하는 칸 `+0x30`, `+0x38 ~ +0x3f` 등을 진짜 서버가 무엇으로 채우는지). 바이트 단위 대조용. (`AlpcSend__56d44` · `HandleReply__9c6fc` · `SendReceive__6d810` 는 이미 export 됨.)
2. **`ntdll!TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` (완료 포트 연결) + 완료 통지가 `LrpcIoComplete` 까지 오는 길** (워커 팩토리, sast/122·127 후보 4). 비동기 답 배달 (2-4) 의 나머지 반쪽.
3. **NDR 형식 문자열 풀기**: EPM 프로시저 8 (`ept_map_auth_async`, 프록시 rva 0xe11e8 → 프로시저 형식 0xefeb2) 과 nsi 프로시저 5·6·7 (winnsi rva 0x66e0·0x6590·0x6360, nsisvc 서버 정보 rva 0x6600) 의 입출력 NDR 바이트 순서·정렬.
4. **`nsi.dll!NsiRegisterChangeNotificationEx`** (rva 0x1eb0) → 어떤 IOCTL 을 `\Device\Nsi` 에 보내나 + 오류 값 (4-2 의 `[공백]`), 필요하면 nsiproxy 쪽.
5. `LRPC_BASE_BINDING_HANDLE::NegotiateTransferSyntax`/`LRPC_BINDING_HANDLE::NegotiateTransferSyntax` (rva 0x43a20) — bind 답 뒤 호출이 NDR 2.0 쪽을 쓰는지 NDR64 쪽을 쓰는지 (1-3 의 "둘 다 가능" 을 정적으로 좁힘).
6. `RegisterNsiSvcRestartNotification` WNF 상태 이름 (sast/127 후보 5, 이월).

## 뽑은 것

- **nsisvc** (`ms/decomp/nsisvc/`, `ms/disassemble/nsisvc/`, depth 0, 22개): `ServiceMain NsiServiceInitialize NsiServiceNotifyStart NsiStartRpcServer NsiStopRpcServer NsiServiceControlHandler NsiServiceCleanup NsiSecurityCallBack NsiGetRpcCallAttributes RpcNsiRegisterChangeNotification RpcNsiRequestChangeNotification RpcNsiDeregisterChangeNotification RpcNsiParameterChange NOTIFICATION_HANDLE_rundown MALLOC MIDL_user_allocate NsipTraceNotificationContext NsipClientDisconnectCallback NsipDeregisterAndFreeNotificationContext NsipUnsubscribeAndCompleteAsyncCall NsipFreePendingChangeList CleanupNotificationThread`. 로그 `ms/ghidra/nsisvc.export.sast130a.stdout`.
- **rpcrt4** (depth 0, `^이름$`, 로그 `ms/ghidra/rpcrt4.export.sast130{a..h}.stdout`): `Bind`(×4) `BaseBind CompleteBind HandleReply`(×5) `SendReceive`(×10) `AlpcSendWaitReceivePort`(×2) `AlpcSend`(×2) `PreinitRequestAlpcHeaders`(×2) `RpcpAlpcpGetHeaderSize RpcpStrictAlpcToRpcStatus AsyncSend`(×9) `HandleSendReceiveFailure HandleAsyncSendFailure HandleCallbackReplyResponse CallBound NegotiateTransferSyntax`(×7) `DoSendReceive DoAsyncSend AlpcNonPipeSend UnpackResponse IoCompleted ValidateCallbackReply DropMsgAttributes SanitizeErrorCodeFromServer MessageReceived`(×2) `LrpcIoComplete OsfTowerConstruct InitializeRequestAndAttributes ServerSetupAddress GetPresentationContext`(×3) `IsRetryErrorCode AttemptRetry RetryCall BasePrepareForRetry PrepareForRetry`(×2) `GetErrorAndErrorClass`(×2) `GetErrorClass`. (sast/127 에서 이미 있던 `LookupNextChunk ProcessNextChunk ReturnLookupValues TowerConstruct LookupNotifyRoutine` 는 읽기만.) `refs`: `ms/disassemble/rpcrt4/_refs/0xeced8.tsv` (`LRPC-` 문자열을 쓰는 곳 = `ServerSetupAddress`).
- PE 데이터 읽기 (`pefile`, 파일로 안 남김): rpcrt4 문법 GUID 셋 (rva 0xebaf8 · 0xebb10 · 0xebb88), 비트 표 (0xeb9c0), 엄격 상태 표 (0xed0d0, 15쌍), EPM 인터페이스 (0xe1220) + 프록시 정보 (0xe11e8) + 문법 표 (0xe7560, 2줄); winnsi 인터페이스 (0x67b0) + 문법 표 (0x6150, 2줄: NDR 2.0 → NDR64); nsisvc 서버 인터페이스 (0x6420) + 디스패치 표 (0x64f0, 9개) + 서버 정보 (0x6600).

## 판정

**분석성공 (정적으로 닫을 수 있는 만큼, `[공백]` 6개 남김).** 질문 1 (bind 메시지·답·오류), 질문 2 (요청·응답 머리, 속성 규칙, 비동기 답 배달, ALPC 상태 → RPC 오류 표, 핸들 0 → `0x6bf`), 질문 3 (질문 탑 4층·응답 검사·최소 응답 탑 바이트), 질문 4 (nsisvc 동적 끝점 + `RpcEpRegisterW`, 프로시저 5 의 검사, 프로시저 7 의 "답하지 않음") 를 코드 줄 근거로 답했다. sast/127 의 3-1 표 ③·④·⑥ 의 `[공백]` 을 채웠다 (5 절). **고친 것**: EPM 인터페이스의 제안 마스크는 3 (NDR 2.0 + NDR64).

**`[공백]` 으로 남긴 것**: ① **NDR 스텁 본문의 정확한 바이트** (`ept_map` 응답 틀, 프로시저 5 입출력) — 형식 문자열을 안 풀었다 (다음 요청 3); ② **서버가 bind 에서 NDR 2.0 을 고르는지 NDR64 를 고르는지** (동적 2); ③ **bind 가 동기냐 비동기냐** (코드는 둘 다 있고 winnsi 의 비동기 EP 조회는 비동기로 짐작 `[추측]`, 동적 1); ④ 재시도가 실제로 도는지 (동적 6); ⑤ 비동기 답을 **받기 호출이 어떻게 꺼내는지** (ntdll `TpAllocAlpcCompletionEx` 쪽, 다음 요청 2); ⑥ 입력이 틀린 프로시저 5 호출의 서버 대답 (nsi.dll → 커널 NSI, 다음 요청 4). 서버 흉내를 **지금 코드 수준에서 동작하게** 하려면 ⑤ (완료 통지) 가 가장 큰 덩어리다.
