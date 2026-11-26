# 2026-11-23 (작성일, 정적 분석 2026-11-23) run137 — 비동기 LRPC 답 배달의 나머지 반쪽 (Q248, 정적만): rpcrt4 `CreateAlpc` → ntdll `TpAllocAlpcCompletionEx` → `TppAllocAlpcCompletion` 이 **`NtAlpcSetInformation(포트, 종류 2, {키 = TP_ALPC 포인터, 풀 IO 완료 포트 핸들}, 0x10)` 한 번** 으로 ALPC 포트를 풀의 완료 포트에 붙인다. 커널은 그 포트에 **메시지 하나가 들어올 때마다 패킷 하나** `{키, ApcContext 0, 상태 0, 정보 0}` 를 넣고, 일꾼은 `TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 `LrpcCAssociationConnectIoHandler` → `IoCompleted` 로 가서 **받기만 하는 부름을 정확히 한 번** 한다. 쌓인 메시지 없이 받기만 하면 커널은 기다리지 않고 **`0xc0000001`** — rpcrt4 는 이것을 치명으로 처리한다. EPM 프로시저 8 · nsi 프로시저 5 의 NDR 2.0 바이트 꼴 (요청 120 / 응답 128 · 응답 24 B), bind 문법은 **서버가 고른 비트 하나를 클라이언트가 그대로 따른다**

> **고쳐 읽기 (run141, sast/138 — Q267)**: 5절 "서버 흉내 차례" 표의 ①c (붙이기) 와 ③ (epmapper bind) 사이에 **연결 응답이 빠져 있었다** `[정적: 받는 쪽]`: 서버 수락 → 클라이언트 포트 큐에 ALPC 종류 `0xb` 메시지 + 완료 패킷 `{키, 0, 0, 0}` (커널이 넣는 쪽은 `[추측]`) → 일꾼 `IoCompleted` (`+0x6a` 에서 `NtAlpcQueryInformation(0)` 한 번 더 · `+0xc8` 받기만) → `+0x187 → +0x3e3 CompleteOnePart` → `SignalAsync +0x2f` → `ntdll!TpSimpleTryPost` (자기 몫이면 syscall 없음) → `DriveStateForward` 둘째 부름 → 보안 문맥 4 줄 → ③. **③ bind 를 보내는 건 부른 쪽이 아니라 풀 일꾼** 이다. 1-1 의 `IsPortWithSharedSection` 부름도 `AlpcConnect +0x388` 말고 `IoCompleted +0x6a` 에 하나 더 있다. ①d 조건 (첫 묶임) 은 그대로 맞음. → `runs/03_sprint/run141-async-bind.md`.

> **고쳐 읽기 (run145, sast/143 — Q279)**: 3-1 "탑 0개: 끝점 핸들 0 · `num_towers` 0 · 배열 `4 · 0 · 0` · 상태 → 40 B `[규칙]`" 의 **서버가 값을 정하는 칸은 `[정적]` 으로 닫혔다** — RpcEpMap `GetEntriesNoLock +0x4c3` (핸들 0, `max_towers ≥ 2` 일 때) · `+0x8e` (탑 수 0) · `+0x867 MOV EDX,0x16c9a0d6` → `ept_map_auth_sd +0x2c7` · `+0x38d` (상태). 단 `max_towers ≤ 1` 이면 핸들 −1 (`+0xa36`), 서비스 트리거 키가 있으면 답을 보류. 배열 머리는 그대로 `[규칙]`. 탑 1개 예 (128 B, `LRPC-emu`) 의 진짜 nsi 판은 끝점 23자라 **140 B** (탑 octets 88). → `runs/03_sprint/run145-epm-endpoint.md` 3절.

> **고쳐 읽기 (run149, sast/144 — Q257a)**: "PE_emulator 쪽" 절 끝 "같은 비동기 틀을 RPCSS (`CoInitializeSecurity`) 도 쓴다 (원 요청 말) `[추측: RPCSS 쪽 연결 객체가 같은 썽크를 쓰는지는 안 읽음]`" → combase 의 RPCSS 길 (bind 넷 + `Connect`) 은 **동기** 다 `[정적]`: `CFastBH::BindWithTimeout +0xc RpcBindingBind(NULL, 핸들, 인터페이스)` (비동기 상태 NULL), `Connect +0x17b NdrClientCall2`. 이 길에선 비동기 완료 포트 틀을 안 쓴다 (연결 깃발 자체는 rpcrt4 안이라 안 읽음). → `runs/03_sprint/run149-com-rpcss.md`

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("비동기 LRPC 답이 클라이언트에 닿는 나머지 반쪽 (ALPC 완료 포트) 과 `ept_map` (프로시저 8) · nsi 프로시저 5 의 NDR 본문", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/309_internals-alpc-completion.md`, 튜닝 `309_tune12.md` V3, 병목 샘플 199 (악성 197) · `WinHttpSendRequest` 0 225) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 rpcrt4 · winnsi 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 과 같은 줄), `sample/*.c` 에 RPC API 가 없다 `[소스]`. 다만 이번에 읽은 ntdll 일꾼 (`TppWorkerThread`) · 커널 완료 포트는 test.exe 의 로더 풀 일꾼과 같은 몸이다 (run125 · run69). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (ntkrnlmp · ntdll · rpcrt4 · winnsi 줄 · 근거 줄) · 3장 run125 절 · run127 절 · run133 절 한두 줄 · 3장 끝 새 절 · 4장 (run129 정적 줄 ④ · run133 줄 셋에 표시 · Q248 닫힘 줄 · run137 줄 둘) · 5장 (run137 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q248** (= run133 남은 정적 **Q246 의 ② · ③ · ⑤** 를 "필수" 로 당긴 것. 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 둘째 — 문서 차례는 디컴파일이 가장 늦게 끝나 맨 뒤). 결과서 판정 **분석성공** (정적으로 닫을 수 있는 만큼, `[공백]` 5개 남김).
> - 이어 쓰는 앞 run: **run133** (sast/130 — LRPC 선 위 형식. 2-4 "비동기 답이 오는 길" 과 5절 "서버 흉내 차례" 의 `[공백]` 을 이번에 채우고, 결과서 5절이 그 표를 고쳐 다시 적음 → 아래 "정정 (고쳐 읽기)"), **run129** (winnsi · `EnableAsync → CreateAlpc → TpAllocAlpcCompletionEx` 까지 — 그 "아직 확인 필요" 4 = Q232 ① 도 여기서 닫힘), **run125** (일꾼 공장 · `NtWaitForWorkViaWorkerFactory` 커널 쪽), **run127** (파일을 완료 포트에 묶기 — 이번 ALPC 판과 비교), **run93** (기다림 완료 패킷 — 같은 큐에 다른 종류 패킷).
> - 남은 동적 → **Q258 (동적: CompleteBind 진짜 문법 · TppAllocAlpcCompletion +0x1d6 인자·반환 · TppAlpcpExecuteCallback 입구 패킷 · AlpcpReceiveMessagePort +0x1ba 안 걸림 · 패킷 시각 · 스텁 길이)**, 남은 정적 → **Q259 (정적: 결과서 다음 요청 후보 1~5 — NDR64 판 · AlpcpSendMessage→AlpcpDispatchReplyToPort · NtWaitForWorkViaWorkerFactory 접점 · AlpcpSignalAndWait · NsiRegisterChangeNotificationEx·WNF 이월)**. Q246 은 ② · ③ · ⑤ 가 닫히고 ① · ④ · ⑥ · ⑦ 은 그대로.

## 요약

- **붙이기는 syscall 하나** `[정적]`: `TppAllocAlpcCompletion +0x1d1` `NtAlpcSetInformation(포트 핸들, 2, &입력, 0x10)`. 입력 16 B = `+0x00` 키 (새 TP_ALPC 객체 포인터) · `+0x08` 풀의 IO 완료 포트 핸들 (`[풀+0x40]`). 같은 포트 · 완료 포트에 다른 syscall (`NtAlpcQueryInformation` · `NtSetIoCompletion*`) 은 **이 함수 안에 없다**. 덤으로 풀의 첫 묶임이면 `TpAdjustBindingCount` 안 `NtSetInformationWorkerFactory(공장, 3, &1, 4)`.
  - 종류 번호 2 는 코드 값, 이름 (`AlpcAssociateCompletionPortInformation`) 은 공개 phnt 이름 `[추측: 이름]`.
- **커널 검사 차례** `[정적]` (`NtAlpcSetInformation` → `AlpcpAssociateIoCompletionPort`): 포트 핸들 0 → `0xc000000d` · 입력 NULL → `0xc000000d` · 길이 > 0x48 → `0xc0000004` · 포트 핸들 참조 실패 → 그 status · **길이 ≠ 0x10 → `0xc0000004`** (종류 5 는 `0xc000000d` — 다름) · 완료 포트 핸들 0 → `0xc000000d` · **이미 붙음 → `0xc0000048`** · 완료 목록 있음 → `0xc00000bb` · 완료 포트 참조 (접근 2) 실패 → 그 status · 패킷 쌈 못 만듦 → `0xc000009a`. 성공이면 포트 `+0x20 CompletionPort` (객체) · `+0x28 CompletionKey` · `+0x30 CompletionPacketLookaside` (PDB `_ALPC_PORT` 칸 이름).
- **패킷 한 장** `[정적]` (`AlpcpQueueIoCompletionPort`): `KeyContext` = 포트 `+0x28` (TP_ALPC 포인터) · `ApcContext` = 0 (완료 목록 갈래만 −1) · `IoStatus` = 0 · `Information` = 0. **메시지 하나 = 패킷 하나**, 합치기 · 건너뛰기 · "이미 알림" 표시 칸 없음. 메시지를 큐 (`MainQueue +0x90`) 에 넣은 **뒤** 넣는다. 붙이기 전에 쌓인 메시지는 붙이는 순간 개수만큼 한꺼번에.
- **일꾼 → 콜백** `[정적]`: `TppWorkerThread +0x379 NtWaitForWorkViaWorkerFactory` → 항목 0x20 B `{키, ApcContext, 상태, 정보}` → `[키+0x38] == TppAlpcpExecuteCallback` 이면 `+0x55e TppAlpcpExecuteCallback(인스턴스, 키, ApcContext, &상태)` → `+0x449` **`LrpcIoComplete(인스턴스, [TP_ALPC+0xa0] = &연결객체+0xe0, TP_ALPC, ApcContext)`** → `+0xc3` `[문맥]` = 썽크 `LrpcCAssociationConnectIoHandler` (**`this = RCX − 0xe0`, `RDX = 0`**, `IoCompleted` 로 꼬리 점프). 콜백 뒤 알림을 다시 켜는 syscall **없음**.
- **빈 받기** `[정적]`: 완료 포트가 붙은 포트에 쌓인 메시지가 없을 때 받기만 하면 `AlpcpReceiveMessagePort +0x1ba` **`0xc0000001`, 기다리지 않음, 버퍼 · 길이 · 속성 안 씀**. rpcrt4 `IoCompleted` 는 `0xc0` · `0xc0000041` 밖이면 `RpcpReportFatalError` → **치명**. 통지 하나에 받기 부름은 **정확히 한 번** (`+0xc8` 하나, 도는 줄 없음).
- **NDR 2.0 바이트** `[정적: 형식 문자열]` + `[규칙: 공개 NDR 2.0]`: EPM 프로시저 8 요청 스텁 120 B (탑 octets 66 일 때) · 응답 128 B (탑 1개, octets 73) / 탑 0개 40 B. nsi 프로시저 5 응답 24 B (컨텍스트 핸들 20 + 반환 DWORD), 요청 40 B. **프로시저 8 은 공개 `ept_map` (3) 과 달리 객체 · 탑이 참조 포인터라 포인터 ID 가 없다.**
- **문법** `[정적]`: 클라이언트는 고르지 않고 **bind 답 `+0x48` 의 비트 하나를 그대로 쓴다** (둘 다 켜면 `CompleteBind` 가 거부). 슬롯이 NDR 2.0 이면 DCE 엔진, 아니면 NDR64 엔진 — 둘 다 코드가 있다. 이번에 바이트로 푼 쪽은 NDR 2.0 이라 서버 흉내는 **마스크 1** 이 안전 (결과서 권고). 진짜 서버가 무엇을 고르는지는 정적으로 못 정함 (Q258).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔다섯째 바퀴. plan **Q248**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** 모두 이미 analyze 된 모듈 — `^이름$` export (depth 0) 만: ntdll 11 · ntoskrnl 15 · rpcrt4 3 (결과서 "뽑은 것"). winnsi · rpcrt4 형식 문자열은 `pefile` 로 PE 데이터만.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| ntoskrnl | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음. 공개 PDB 인데 `_ALPC_PORT` 등 구조체 타입이 들어 있어 칸 이름을 `pdb_struct.py` 로 읽음 |
| ntdll | `C3093720177DA6851519DCFFE8DF53FE1` | 같음 |
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 같음 |
| winnsi | `907CE42FFA71F5085D1468AD2D40D62C1` | 같음 (export 안 함, PE 데이터만) |

- 버전 대조 (결과서): "알려진 사실" 표에 이 함수들 줄이 없어 `.asm` `+0x` 를 원본 바이트 (`pefile`) 와 맞춤 — `TpAllocAlpcCompletionEx +0x13` · `TppAllocAlpcCompletion +0x1bf / +0x1ca / +0x1d1 / +0x1e4` · `TppWorkerThread +0x379 / +0x55e` · `TppAlpcpExecuteCallback +0x438 / +0x449` · `nt!NtAlpcSetInformation +0x127 / +0x13b` · `AlpcpAssociateIoCompletionPort +0x1e5 / +0x212` · `AlpcpQueueIoCompletionPort +0x1b7` · `AlpcpCompleteDispatchMessage +0xeb4` · `AlpcpReceiveMessagePort +0x19c / +0x1ba` · rpcrt4 `CreateAlpc +0x66` · `IoCompleted +0xc8` — **일치**.
- flow-writer 가 다시 한 대조: rpcrt4 프로시저 8 형식 (`0xf003e` = `00 49 … 08 00 50 00 32 00 08 00 84 00 70 00 c3 08 …`), 오프셋 표 `0xefe98` (`…, 0x18c` = 396), 썽크 바이트 `0x96a70` (`48 81 c1 20 ff ff ff 33 d2 e9 …`), winnsi 프로시저 5 형식 `0x7212` (`00 48 … 05 00 30 00 … c6 04`) 와 오프셋 표 `0x7310` (`…, 0x150` = 336) 를 `pefile` 로 다시 읽음 — 결과서와 같음.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님): `WinHttpSendRequest` 가 0 인 샘플 225 · 병목 199. 에뮬레이터는 epmapper 연결을 `0xc0000034` 로 답해 이 길에 안 들어간다. 서버 흉내 (epmapper `ept_map` · nsi 프로시저 5) 를 돌리려면 run133 이 남긴 **비동기 답 배달의 나머지 반쪽** 과 **NDR 본문 바이트** 가 필요하다. 요청서 `_workspace/sast/132_alpc-completion.md`:
  1. 완료 포트 붙이기 — syscall 차례 · `NtAlpcSetInformation` 종류 · 입력 칸 · 커널 검사 · 실패 status · 포트에 남는 칸.
  2. 통지 한 번의 꼴 — (a) 커널 패킷 칸 · 메시지당 몇 개 (b) 일꾼 → `LrpcIoComplete` 인자 · 다시 켜는 syscall (c) 빈 받기 status · 쓰는 것 · 통지당 받기 횟수.
  3. NDR 본문 바이트 — EPM 프로시저 8 요청 / 응답, nsi 프로시저 5 요청 / 응답.
  4. bind 에서 고르는 문법 — 서버 흉내가 하나만 고를 때 어느 쪽이 안전한가.
- 쓰는 곳: PE_emulator `ext/syscall` — `src/alpc/nt_alpc_create_port.cpp:581 ~ :602` (`NtAlpcSetInformation`) · `:612 ~ :626` (`NtAlpcSendWaitReceivePort`) · `src/threadpool/nt_io_completion.cpp` (패킷 넣기) · 새 `src/rpc/` 창구.

## 방법

- 정적 sast/132 (결과서 `_workspace/sast/132_alpc-completion_result.md`, **분석성공**, 스크립트 문제 없음):
  - ntdll `^이름$` depth 0 (로그 `ms/ghidra/ntdll.export.sast132{a,b,c}.stdout`): `TpAllocAlpcCompletionEx` · `TppAllocAlpcCompletion` · `TppAlpcpExecuteCallback` · `TppWorkerThread` · `TppAlpcpValidateAlpc` · `TpReleaseAlpcCompletion` · `NtAlpcSetInformation` · `NtWaitForWorkViaWorkerFactory` (스텁) · 넓힌 것 `TpAdjustBindingCount` · `TppCleanupGroupAddMember` · `TppPrepareDirectParams`. 있던 `TppAlpcpFree` · `TppCleanupGroupMemberInitialize` · `TpAllocPoolInternal` 읽기.
  - ntoskrnl `^이름$` depth 0 (로그 `ms/ghidra/ntoskrnl.export.sast132{a,b,c,d}.stdout`): `NtAlpcSetInformation` · `AlpcpAssociateIoCompletionPort` · `AlpcpQueueIoCompletion` · `AlpcpQueueIoCompletionPort` · `AlpcpCompleteDispatchMessage` · `AlpcpDispatchNewMessage` · `AlpcpReceiveMessage` · `AlpcpReceiveMessagePort` · `NtAlpcSendWaitReceivePort` · 넓힌 것 `AlpcpSignal` · `AlpcpSignalPortAndUnlock` · `AlpcpAllocateCompletionPacketLookaside` · `AlpcpLookasidePacketCallbackRoutine` · `AlpcpReceiveDirectMessagePort` · `AlpcpLookupMessage`.
  - rpcrt4 `^이름$` depth 0 (로그 `ms/ghidra/rpcrt4.export.sast132{a,b}.stdout`): `InitializeCallbackEnvironmentIfNecessary` · `Ndr64pClientSetupTransferSyntax` · `I_RpcNegotiateTransferSyntax`. 있던 `CreateAlpc` · `EnableAsync` · `LrpcIoComplete` · `IoCompleted` · `IsPortWithSharedSection` · `LrpcCAssociationConnectIoHandler` 읽기.
  - PE 데이터 (`pefile`, 파일로 안 남김): rpcrt4 프록시 `0xe11e8` · 인터페이스 `0xe1220` · 문법 표 `0xe7560` · NDR 2.0 프로시저 3 · 7 · 8 · 타입 문자열 `0xefb62` · NDR64 프로시저 8 머리 `0xe7870`, winnsi 프록시 `0x6000` · 문법 표 `0x6150` · NDR 2.0 프로시저 5 · 6 · 7 · 타입 문자열 `0x6eb2` · NDR64 프로시저 5 머리 `0x66e0`.
  - PDB 구조체 (`pdb_struct.py ntoskrnl.exe`): `_ALPC_PORT` · `_ALPC_COMPLETION_PACKET_LOOKASIDE(_ENTRY)` · `_IO_MINI_COMPLETION_PACKET_USER` · `_ALPC_DISPATCH_CONTEXT` · `_KALPC_MESSAGE`, `_ALPC_PORT.u1` 비트 (4 `ConnectionRefused` · 6 `Closed` · 9 `Waitable` · 16 `EnableCompletionList`).
- 같은 시간에 decompiler 둘이 ntoskrnl · wow64 를 돈다 (`ms/` 만) — flow-writer 는 `.asm` 을 읽기만 했다.
- flow-writer 가 다시 본 것:
  - `.asm` 줄: ntdll `TpAllocAlpcCompletionEx +0x9 / +0x13`, `TppAllocAlpcCompletion +0x3f ~ +0x2ba` 전부, `TpAdjustBindingCount +0xa ~ +0x3b`, `TppWorkerThread +0x379 · +0x46e · +0x485 · +0x49e · +0x536 ~ +0x55e`, `TppAlpcpExecuteCallback +0x34 · +0x4d · +0x52 ~ +0x5e · +0x19c · +0x423 ~ +0x449 · +0x657 ~ +0x684 · +0x7d2 ~ +0x7e0`, `TpAllocPoolInternal +0x283 · +0x292 · +0x2f4 · +0x303`; ntoskrnl `NtAlpcSetInformation` (1-3 표 줄 전부 + `+0x472 ~ +0x482`), `AlpcpAssociateIoCompletionPort` (1-3 표 줄 전부), `AlpcpQueueIoCompletionPort +0x91 ~ +0x97 · +0x17b ~ +0x1bf · +0x2ff ~ +0x31b`, `AlpcpCompleteDispatchMessage +0x470 · +0x474 · +0x541 · +0x560 · +0x5d1 ~ +0x5e8 · +0x9d2 · +0xb5e ~ +0xb71 · +0xe92 ~ +0xeb4 · +0xeca ~ +0xf3b`, `AlpcpDispatchNewMessage +0x2e3`, `NtAlpcSendWaitReceivePort +0x112 ~ +0x176 · +0x21b`, `AlpcpSignal +0x2c · +0xa31`, `AlpcpReceiveMessagePort` (2-3 표 줄 전부), `AlpcpReceiveMessage +0x10f · +0x2da · +0x402 · +0x506 · +0x5dc · +0x630`, `AlpcpReceiveDirectMessagePort +0x232 ~ +0x240`, `AlpcpLookupMessage +0x20e · +0x2f1 · +0x30e · +0x331 · +0x34d`; rpcrt4 `EnableAsync +0x0 ~ +0xcb`, `CreateAlpc +0x26 ~ +0x87 · +0xb6`, `Connect +0x398 · +0x3b1`, `AlpcConnect +0x293 · +0x388`, `IsPortWithSharedSection +0x25 · +0x35`, `InitializeCallbackEnvironmentIfNecessary +0x46`, `LrpcIoComplete +0x25 ~ +0xc3 · +0x11b`, `LrpcCAssociationConnectIoHandler` 전부, `IoCompleted +0xc8 ~ +0x12f · +0x187 · +0x19d · +0x22c · +0x277 · +0x288 · +0x481 ~ +0x4a6 · +0x6b2 ~ +0x6ce`.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **ALPC 포트 = 전화 수신함, 완료 포트 = 사무실 벨, 일꾼 = 벨이 울리면 일어나는 직원.** rpcrt4 는 수신함을 만든 뒤 경비실 (커널) 에 "이 수신함에 쪽지가 오면 저 벨을 울려 주세요, 울릴 때 이 꼬리표 (키) 를 붙여서" 하고 **한 번** 맡긴다.
- 경비실은 **쪽지 한 장마다 벨 한 번**. "아까 울렸으니 이번엔 건너뛰자" 는 없다.
- 직원은 벨 한 번에 수신함을 **딱 한 번** 연다. 쪽지가 없는데 열면 경비실은 "아무것도 없음 (`0xc0000001`)" 만 말하고, rpcrt4 는 그걸 "큰일 났다" 로 보고 프로그램을 멈춘다 `[추측: 비유]`.
- 그래서 흉내 서버가 지킬 약속은 하나: **쪽지 수 = 벨 수 = 수신함 여는 수.**

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{ntdll,ntoskrnl,rpcrt4}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `rpcrt4!LRPC_CASSOCIATION::Connect` | `+0x3b1` (다른 길 `+0x398`) | `+0x3b6` (`+0x39d`) | `EnableAsync` (rva `0x2ef70`) | [정적] |
| `rpcrt4!EnableAsync` | `+0x6a` (다른 길 `+0xb6`) | `+0x6f` (`+0xbb`) | `RPC_THREAD_POOL::CreateAlpc` (rva `0x2f158`) — 앞서 `+0x4c LEA R14,[RBX+0xe0]` · `+0x53` 썽크 주소 · `+0x64 MOV [R14],RAX` · `+0x5a MOV R8,R14` (문맥) | [정적] |
| `rpcrt4!CreateAlpc` | `+0x26` · `+0x34` | `+0x2b` · `+0x39` | `InitializeCallbackEnvironmentIfNecessary` · `AllocWrapper(0x10)` | [정적] |
| 〃 | **`+0x66`** | **`+0x6d`** | `ntdll!TpAllocAlpcCompletionEx` (가져오기 칸 `0x1800e95e0`) — 콜백 `+0x42 LEA R8,[0x180053510]` = `LrpcIoComplete`, 환경 `+0x57` `CallbackEnvironment` | [정적] |
| 〃 | `+0x76` | `+0x7d` | `RtlNtStatusToDosError` (실패면 `+0x87 JS` → `+0xb6 MOV EBX,0xe`) | [정적] |
| `rpcrt4!AlpcConnect` | `+0x388` | `+0x38d` | `IsPortWithSharedSection` → `+0x25` (ret `+0x2c`) `NtAlpcQueryInformation(포트, 0, &버퍼, 0x10, NULL)` | [정적] |
| `ntdll!TpAllocAlpcCompletionEx` (rva `0xc9580`, 29 B) | `+0x13` | `+0x18` | `TppAllocAlpcCompletion` (rva `0xc95a4`) — `+0x9` 여섯째 인자 (Ex) = 1 | [정적] |
| `ntdll!TppAllocAlpcCompletion` | `+0xa4` | `+0xa9` | `RtlAllocateHeap(프로세스 힙, 태그, 0x128)` | [정적] |
| 〃 | `+0xea` | `+0xef` | `TppCleanupGroupMemberInitialize(객체+0x48, 문맥, 환경, 플래그, TppAlpcpCleanupGroupMemberVFuncs)` | [정적] |
| 〃 | **`+0x1d1`** | **`+0x1d6`** | **`NtAlpcSetInformation(포트, 2, &{키, 풀+0x40}, 0x10)`** | [정적] |
| 〃 | `+0x1e4` | `+0x1e9` | `TpAdjustBindingCount(풀, +1)` | [정적] |
| 〃 | `+0x20f` | `+0x214` | `TppCleanupGroupAddMember` (정리 그룹 있을 때만) | [정적] |
| 〃 | `+0x22a` · `+0x27b` | `+0x22f` · `+0x280` | `TppRaiseInvalidParameter` (→ `0xc000000d`) | [정적] |
| 〃 | `+0x23d` · `+0x2ad` | `+0x242` · `+0x2b2` | `TppCleanupGroupMemberDestroy` · `RtlFreeHeap` (실패 정리) | [정적] |
| `ntdll!TpAdjustBindingCount` | `+0x3b` | `+0x40` | `NtSetInformationWorkerFactory([풀+0x38], 3, &1, 4)` — 묶음 수 `+0x1b0` 가 0 이하 → 1 이상일 때만 (`+0xa XADD` · `+0x16 ~ +0x1d`) | [정적] |
| `ntdll!TpAllocPoolInternal` (run125 몫) | `+0x292` · `+0x303` | `+0x297` · `+0x308` | `NtCreateIoCompletion(&풀+0x40, …)` · `NtCreateWorkerFactory(…, [풀+0x40], …)` | [정적] |
| `ntdll!TppWorkerThread` (rva `0x1a2e0`) | `+0x379` | `+0x37e` | `NtWaitForWorkViaWorkerFactory` | [정적] |
| 〃 | `+0x46e` | `+0x473` | `TppPrepareDirectParams` (마지막 키 ≠ 0 항목을 스택 `+0x298 ~ +0x2b8` 에) | [정적] |
| 〃 | **`+0x55e`** | **`+0x563`** | **`TppAlpcpExecuteCallback`** (rva `0x19aa0`) — `+0x536` `[키+0x38]` · `+0x53a` · `+0x551 CMP` | [정적] |
| `ntdll!TppAlpcpExecuteCallback` | **`+0x449`** | **`+0x44e`** | 간접 `[TP_ALPC+0x98]` (CFG) = 콜백 `LrpcIoComplete` — 4 인자 (Ex 비트 켜짐) | [정적] |
| 〃 | `+0x7e0` | `+0x7e5` | 같은 간접, 3 인자 (Ex 비트 꺼짐 — 이 길 아님) | [정적] |
| 〃 | `+0x684` | `+0x689` | `NtAlpcSetInformation(포트, 8, &값, 4)` — `[TP_ALPC+0x120] & 3 == 3` 일 때만 (이 길 아님) | [정적] |
| `rpcrt4!LrpcIoComplete` (rva `0x53510`) | `+0xc3` | `+0xc8` | 간접 `[R14]` = 문맥 첫 칸 = 썽크 `LrpcCAssociationConnectIoHandler` (rva `0x96a70`) | [정적] |
| `rpcrt4!LrpcCAssociationConnectIoHandler` | `+0x9` (JMP) | — (꼬리 점프) | `LRPC_CASSOCIATION::IoCompleted` (rva `0x57800`) — `+0x0 ADD RCX,-0xe0` · `+0x7 XOR EDX,EDX` | [정적] |
| `rpcrt4!IoCompleted` | **`+0xc8`** | **`+0xcf`** | `NtAlpcSendWaitReceivePort` (받기만, 이 함수 안 하나뿐) | [정적] |
| 〃 | `+0x22c` | `+0x231` | `MessageReceived` | [정적] |
| 〃 | `+0x4a6` | `+0x4ab` | `RpcpReportFatalError(2, 상태)` | [정적] |
| `nt!NtAlpcSetInformation` (rva `0xa285b0`) | `+0x108` | `+0x10d` | `ObReferenceObjectByHandle(포트, 1, AlpcPortObjectType)` | [정적] |
| 〃 | **`+0x13b`** | **`+0x140`** | **`AlpcpAssociateIoCompletionPort(포트 객체, [입력+8], [입력+0])`** | [정적] |
| `nt!AlpcpAssociateIoCompletionPort` (rva `0xa28a7c`) | `+0xae` | `+0xb3` | `ObReferenceObjectByHandle(완료 포트, 2, IoCompletionObjectType)` | [정적] |
| 〃 | `+0xfa` | `+0xff` | `AlpcpAllocateCompletionPacketLookaside` | [정적] |
| 〃 | `+0x1b8` | `+0x1bd` | `AlpcpQueueIoCompletionPort(포트, 0, 0, 0, 0)` (이미 쌓인 메시지마다) | [정적] |
| `nt!AlpcpDispatchNewMessage` | `+0x2e3` | `+0x2e8` | `AlpcpCompleteDispatchMessage` (rva `0x8ae700`) | [정적] |
| `nt!AlpcpCompleteDispatchMessage` | **`+0xeb4`** | **`+0xeb9`** | **`AlpcpQueueIoCompletionPort(대상 포트, DIL, 1, 0, …)`** | [정적] |
| 〃 | `+0xf3b` | `+0xf40` | `KeReleaseSemaphoreEx` (완료 포트 없는 `Waitable` 포트 — 이 길 아님) | [정적] |
| `nt!AlpcpQueueIoCompletionPort` (rva `0x32dd10`) | `+0x31b` | `+0x320` | `AlpcpQueueIoCompletion` (쌈이 가득일 때 새 패킷) | [정적] |
| `nt!NtAlpcSendWaitReceivePort` | `+0x141` | `+0x146` | `AlpcpReceiveMessage` (받을 메시지 포인터 ≠ 0 일 때만 — `+0x112 ~ +0x11d`) | [정적] |
| 〃 | `+0x176` | `+0x17b` | `AlpcpSignal` (미룬 패킷 — `+0x150 BTR` 맥락 Flags 비트 2) | [정적] |
| `nt!AlpcpSignal` | `+0xa31` | `+0xa36` | `AlpcpQueueIoCompletion` (`+0x2c CMP [맥락+0x3a],0`) | [정적] |
| `nt!AlpcpReceiveMessage` | `+0x2da` | `+0x2df` | `AlpcpReceiveMessagePort` (rva `0x8b2ae0`) | [정적] |
| 〃 | `+0x402` · `+0x506` · `+0x630` | `+0x407` · `+0x50b` · `+0x635` | `AlpcpExposeAttributes` · `AlpcpReturnMessageOnInsufficientBuffer` · `AlpcpReceiveDirectMessagePort` | [정적] |

### 1. 완료 포트 붙이기 (질문 1) [정적]

#### 1-1. 차례

1. `AlpcConnect +0x293 NtAlpcConnectPort` (비동기, run129) → 연결 직후 `+0x388 IsPortWithSharedSection` → `NtAlpcQueryInformation(포트, 0, &버퍼, 0x10, NULL)`. 이건 붙이기가 아니라 **"공유 섹션 있나" 묻기** — 성공이고 `Flags & 0x1000000` (`+0x35`) 이면 "없음" (받기 버퍼 `0xff00`), 그 밖 (실패 포함) 은 "있음" (`0x1000`).
2. `Connect +0x3b1 EnableAsync` → 문맥 칸 `[연결객체+0xe0]` 에 썽크 주소를 먼저 쓰고 `CreateAlpc(포트 = [연결객체+0x88], …, 문맥 = &[연결객체+0xe0], 출력 = &[연결객체+0xe8])`.
3. `CreateAlpc +0x66 TpAllocAlpcCompletionEx(&결과, 포트, LrpcIoComplete, 문맥, CallbackEnvironment)`. `RPC_THREAD_POOL::PrivatePool` (rva `0x1044fc`) 이 0 이면 환경 = NULL → **전역 풀** (켜는 함수는 안 읽음 `[추측: 안 켜면 NULL]`). 붙이기가 어떤 이유로든 실패하면 `CreateAlpc` 는 **14** 로 바꿔 돌려준다 (`+0x87` → `+0xb6`).
4. `TpAllocAlpcCompletionEx` = Ex 깃발 1 만 얹은 썽크 → `TppAllocAlpcCompletion`:
   - 들어올 때 검사: 결과 포인터 · 콜백 · 포트 핸들 0, 환경 플래그 `& ~3` ≠ 0, PEB `Ldr+0x48` (종료 중) ≠ 0 → `TppRaiseInvalidParameter` + `0xc000000d` (`+0x3f ~ +0x77` → `+0x27b`).
   - `RtlAllocateHeap(0x128)` → TP_ALPC 객체 (실패 `0xc0000017`).
   - `TppCleanupGroupMemberInitialize` — 환경 NULL 이면 여기서 전역 풀 참조 (처음이면 풀 만들기 = run125).
   - 객체에 남기는 칸: `+0x38` = `TppAlpcpExecuteCallback` · `+0x98` = 콜백 · `+0xa0` = 문맥 · `+0xd8` = 풀 · `+0x110` = 포트 핸들 · `+0x120` 비트 0 = Ex.
   - 풀 NULL · 종료 중을 **한 번 더** 본다 (`+0x18d · +0x190`, `+0x1a3 · +0x1a7` → `+0x22a`, `0xc000000d`) — flow-writer 보탬.
   - **`+0x1d1 NtAlpcSetInformation(포트, 2, &{키 = 객체, [풀+0x40]}, 0x10)`.**
   - 성공이면 `+0x1e4 TpAdjustBindingCount(풀, +1)` → 첫 묶임일 때만 `NtSetInformationWorkerFactory(공장, 3, &1, 4)` (종류 3 = 묶음 수 `[추측: 이름]`).
   - 실패면 정리 + 상태, 성공이면 `*결과 = 객체` (`+0x251`).
- **같은 포트 · 같은 완료 포트에 대한 syscall 은 `+0x1d1` 하나뿐.** `TpReleaseAlpcCompletion` → `TppAlpcpFree` 도 연결을 푸는 `NtAlpcSetInformation` 없이 `TpAdjustBindingCount(풀, −1)` 만 (연결은 포트를 닫을 때 사라진다 `[추측: 닫는 쪽 커널 안 읽음]`).

#### 1-2. 입력 16 B (종류 2)

| 칸 | 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|---|
| CompletionKey | `+0x00` | 8 | TP_ALPC 객체 포인터 (일꾼이 패킷 `KeyContext` 로 돌려받음) | `TppAllocAlpcCompletion +0x1ba` |
| CompletionPort | `+0x08` | 8 | 풀의 IO 완료 포트 **핸들** (`[풀+0x40]`, `TpAllocPoolInternal +0x292` 가 만든 것) | `+0x1a9` · `+0x1ad` |
| 길이 | | | `0x10` | `+0x1bf` |
| 종류 | | | `2` | `+0x1ca LEA EDX,[R9-0xe]` |

#### 1-3. 커널 검사 (호출한 쪽에서 본 차례)

| 차례 | 검사 | 실패 status | 줄 |
|---|---|---|---|
| 1 | 포트 핸들 0 | `0xc000000d` | `NtAlpcSetInformation +0x5f / +0x62` → `+0x243` |
| 2 | 입력 NULL (종류 7 · 10 밖) | `0xc000000d` | `+0x68 / +0x6d`, `+0x239` |
| 3 | 사용자 모드 · 길이 > 0x48 | `0xc0000004` | `+0x95 ~ +0xa3` → `+0x3c7` |
| 4 | 포트 핸들 참조 (접근 1, `AlpcPortObjectType`) | 그 status (`0xc0000008` · `0xc0000024` · `0xc0000022`) | `+0xe0` · `+0xfe` · `+0x108` · `+0x111` |
| 5 | **길이 ≠ 0x10** | **`0xc0000004`** (종류 5 는 `0xc000000d` — `+0x472 ~ +0x482`) | `+0x123 / +0x127` → `+0x3a6` |
| 6 | 완료 포트 핸들 0 | `0xc000000d` | `AlpcpAssociateIoCompletionPort +0x21 / +0x24` → `+0x1a1` |
| 7 | 포트 잠금 아래 **`[포트+0x20] ≠ 0` (이미 붙음)** | **`0xc0000048`** | `+0x60 / +0x65` → `+0x1e5` |
| 8 | `[포트+0x168]` 완료 목록 있음 | `0xc00000bb` | `+0x6b / +0x73` → `+0x212` |
| 9 | 완료 포트 핸들 참조 (**접근 2**, `IoCompletionObjectType`) — 직후 `[포트+0x20]` 에 결과 (실패면 0) | 그 status | `+0x82` · `+0x87` · `+0xae` · `+0xbc` · `+0xc4` |
| 10 | `[포트+0x28] = 키`, 쌈 `min(4, 동시성)` 개 → `[포트+0x30]` | 못 만들면 `0xc000009a` | `+0xd1` · `+0xfa` · `+0xff` · `+0x106` |
| 11 | 이미 쌓인 메시지마다 `AlpcpQueueIoCompletionPort(포트, 0, 0, 0, 0)` | | `+0x10c ~ +0x14e`, `+0x1b8` |
| 12 | 성공 | 0 | `+0x16e` → `+0x185` |

- 뜻: "이미 붙은 포트에 틀린 완료 포트 핸들을 줘도 `0xc0000048`", "완료 포트 핸들 0 은 그보다 앞 `0xc000000d`".
- 포트 객체 칸 (PDB `_ALPC_PORT`): `+0x20 CompletionPort` (`_KQUEUE*` — 핸들 아니고 객체) · `+0x28 CompletionKey` · `+0x30 CompletionPacketLookaside` (쌈 — `Size` · `ActiveCount` · `PendingNullCount` · `PendingCheckCompletionListCount` · `FreeListHead` · 항목 0x18 B; 쌈 속 패킷 = `_IO_MINI_COMPLETION_PACKET_USER` 0x50 B, `PacketType 4`). 유저에게 안 보이는 칸이다 — 에뮬레이터가 따라 할 건 **"포트에 (완료 포트, 키) 두 칸"** 뿐 (결과서).

### 2. 통지 한 번의 꼴 (질문 2) [정적]

#### 2-1. (a) 커널이 패킷을 넣는 곳 · 값

- `AlpcpCompleteDispatchMessage` (보내기 · 답 · 닫기 · 연결 요청 처리의 끝) 가 **먼저 메시지를 대상 포트 `MainQueue` 에 넣고** (`+0x541 INC [포트+0x1c0]` = `MainQueueLength`), 대상 포트에 완료 포트가 있으면 (`+0x470 CMP [포트+0x20]`) 패킷을 넣는다:
  - 맥락이 동기 요청 (`Flags & 0x20000`) 도 보내기 길 (`Flags & 4`) 도 아니면 → **바로** `+0xeb4 AlpcpQueueIoCompletionPort(대상 포트, DIL, 1, 0, …)`.
  - 그 둘 중 하나면 → 맥락에 표시만 (`+0x5e3 [맥락+0x3a] SignalCompletion = 1`, `+0x5e8 [+0x3b] = DIL`) 하고, `NtAlpcSendWaitReceivePort` 끝 (`+0x150` · `+0x176 AlpcpSignal`) 에서 **같은 패킷** 을 넣는다 (`AlpcpSignal +0xa31 AlpcpQueueIoCompletion`). 보내는 syscall 이 돌아가기 직전.
  - 취소 · 끊기 `AlpcpSignalPortAndUnlock` 도 `[포트+0x20] ≠ 0` 이면 패킷 하나 (결과서, 디컴파일 첫 갈래).
  - 완료 포트가 없으면 `Waitable` 포트는 세마포어 (`+0xf3b KeReleaseSemaphoreEx`) — 이 길 아님.

| 패킷 칸 (PDB `_IO_MINI_COMPLETION_PACKET_USER`) | 값 | 어디서 | 줄 |
|---|---|---|---|
| `KeyContext` `+0x18` | 포트 `CompletionKey` (= TP_ALPC 포인터) | `[포트+0x28]` | `AlpcpQueueIoCompletionPort +0x17b` · `+0x1af` |
| `ApcContext` `+0x20` | **0** (메시지를 완료 목록에 올렸을 때만 `−1`) | 둘째 인자 `DL` → `NEG` · `SBB` | `+0x17f` · `+0x182` · `+0x1b7`; `AlpcpCompleteDispatchMessage +0x560 XOR DIL,DIL` / `+0x9d2 MOV DIL,1` (완료 목록 길) |
| `IoStatus` `+0x28` | **0** | 상수 | `+0x1bb` |
| `IoStatusInformation` `+0x30` | **0** | 상수 | `+0x1bf` |

- **메시지 하나 = 패킷 하나.** `AlpcpQueueIoCompletionPort` 는 부를 때마다 쌈에서 꺼내 넣고 (`+0x91 ~ +0x97` `ActiveCount < Size`), 가득이면 `AlpcpQueueIoCompletion` 으로 새 패킷 (`+0x2ff ~ +0x31b`). 새 패킷마저 못 만들면 셈 칸 (`PendingNullCount` 등) 을 올려 두었다가 패킷이 반납될 때 대신 넣는다 — 건너뛰는 일은 없다. "이미 넣은 패킷이 안 꺼졌으면" 검사는 **없다**.
- 메시지 종류와 상관없다 — 답 (종류 2) · 요청 (1) · 닫기 (5, `HandleConnectionClose`) 각각 하나.

#### 2-2. (b) 일꾼 → 콜백 인자

| 단계 | 하는 일 | 인자 | 줄 |
|---|---|---|---|
| 1 | `NtWaitForWorkViaWorkerFactory(공장 = [풀+0x38], 배열, 개수, &받은 수)` | 항목 0x20 B: `+0x00` 키 · `+0x08` ApcContext · `+0x10` 상태 · `+0x18` 정보 | `TppWorkerThread +0x379` |
| 2 | `TppPrepareDirectParams` — 마지막 (키 ≠ 0) 항목을 이 일꾼 몫으로 스택 `+0x298 ~ +0x2b8` 에, 나머지는 풀 일감 줄에 | | `+0x46e` |
| 3 | **`[키+0x38]` 함수 주소로 종류 가름** — `TppAlpcpExecuteCallback` (`0x19aa0`) 이면 ALPC 갈래 (그 밖 `TppIopExecuteCallback` · `TppWaitCompletion` · 간접) | `RCX = &[RSP+0x198]` (인스턴스) · `RDX = 키` · `R8 = ApcContext` · `R9 = &상태` | `+0x485` · `+0x49e` · `+0x536 ~ +0x556` · **`+0x55e`** |
| 4 | `TppAlpcpExecuteCallback` — 참조 · 카운터 (`+0x4d`) · 활성화 문맥 · ETW 뒤 **콜백** | Ex 비트 (`[TP_ALPC+0x120]` 비트 0) 켜짐 → **`콜백(인스턴스, [TP_ALPC+0xa0], TP_ALPC, ApcContext)`** · 꺼짐 → 3 인자 | `+0x34` · `+0x19c` · `+0x423 / +0x42b` · `+0x438 MOV R9,R13` · `+0x43b` · `+0x442` · **`+0x449`**; 3 인자 `+0x7d2 ~ +0x7e0` |
| 5 | `LrpcIoComplete(인스턴스, 문맥, TP_ALPC, ApcContext)` — 스레드 객체 `[+0xc0]` 에 인스턴스를 적고, **`[문맥]` 함수를 `(문맥, ApcContext)` 로** | 문맥 = `&[연결객체+0xe0]`, 첫 칸 = 썽크 | `+0x25` · `+0x28` · `+0x2b` · `+0x93` · `+0xba ~ +0xc3` · `+0x11b` |
| 6 | 썽크 `LrpcCAssociationConnectIoHandler` — **`RCX −= 0xe0`, `EDX = 0`**, `IoCompleted` 로 꼬리 점프 | `IoCompleted(연결객체, RPCP_ALPC_CONTEXT_ATTR* = NULL)` | `+0x0` · `+0x7` · `+0x9` |

- 그래서 **클라이언트 연결 길에선 ApcContext 가 버려지고 `IoCompleted` 는 늘 "완료 문맥 없음" = Flags 0 으로 받는다.** `Flags 0x1000000` 갈래 (`IoCompleted +0x277 ~ +0x288`) 는 이 길에서 안 쓰인다. (서버 쪽 `ServerStartingToListen` 의 문맥은 다른 썽크일 수 있다 `[공백: 안 읽음]`.)
- 둘째 인자 형 이름 `RPCP_ALPC_CONTEXT_ATTR*` 은 PDB 이름. 그 `+0x14` · `+0x18` 이 ALPC 컨텍스트 속성 (`MessageId` · `CallbackId`) 모양과 맞다 `[추측: 모양 대조]`.
- **콜백 뒤 알림을 다시 켜는 syscall 은 없다.** `TppAlpcpExecuteCallback` 안 `NtAlpcSetInformation` 은 `+0x684` (종류 8, 길이 4) 하나이고 `[TP_ALPC+0x120] & 3 == 3` 일 때만 (`+0x52 ~ +0x5e`) — `TppAllocAlpcCompletion` 은 비트 0 만 쓰므로 이 길에선 안 간다 (비트 1 을 켜는 건 완료 목록 쪽 함수 `[추측]`). 커널에도 켜고 끄는 칸이 없다.
- 항목이 여럿 와도 하나씩 와도 결과는 같다 (일꾼이 마지막 것을 자기가, 나머지는 풀에).

#### 2-3. (c) 받기만 하는 부름 — 쌓인 메시지가 없을 때

부름 꼴 (`IoCompleted +0xc8`): `(포트, Flags 0, NULL, NULL, 새 BCACHE 버퍼, &길이 0x1000 / 0xff00, &받기 속성 (Allocated 0xfa000000), 타임아웃 NULL)`.

| 경우 | status | 쓰는 것 | 줄 |
|---|---|---|---|
| 큐에 메시지 있음 | 0 | 헤더 0x28 + 데이터 · 받기 속성 (`AlpcpExposeAttributes`) | `NtAlpcSendWaitReceivePort +0x141` → `AlpcpReceiveMessage +0x2da` · `+0x402` |
| **큐 빔 + 완료 포트 붙음 (이 길)** | **`0xc0000001`**, 기다리지 않음 | **아무것도 안 씀** | `AlpcpReceiveMessagePort +0xd4 ~ +0x107` (큐 셋 빔) · `+0x10d ~ +0x117` (완료 목록 없음) · **`+0x197 / +0x19c` → `+0x1ba MOV EDI,0xc0000001`** |
| 큐 빔 + 완료 포트 없음 | 기다림 (타임아웃 NULL 이면 무한), 끝나면 `0x102` 등 | — | `+0x19e` · `+0x1c3 ~ +0x1d9` · `+0x78a` — 이 길 아님 |
| 포트 닫힘 (`u1` 비트 6) | `0xc0000700` | 안 씀 | `+0x8b / +0x8d` → `+0x66c` |
| 연결 거절 (`u1` 비트 4) | `0xc0000041` | 안 씀 | `+0x93 / +0x95` → `+0x676` |
| 버퍼 < `0x28 + DataLength` | `0xc0000023`, 메시지는 큐에 남음 | `*길이` 에 필요한 크기 | `AlpcpReceiveMessage +0x506` |
| Flags `0x1000000` (직접 받기) | `0xc000000d` · `0xc0000702` · `0xc0000708` · `0xc0000700` · `0xc0000041` 등 | 안 씀 | `AlpcpReceiveMessage +0x5dc` · `+0x630` → `AlpcpReceiveDirectMessagePort +0x232 ~ +0x240`, `AlpcpLookupMessage` — **이 길엔 안 옴** |

- **rpcrt4 가 다루는 법** (`IoCompleted`): `+0xd4 MOVSXD RSI,EAX` · `+0xd9 JNZ` → … → `+0x486 CMP ESI,0xc0` (버리고 끝) · `+0x492 CMP ESI,0xc0000041` (연결 끊김 처리) · 그 밖 `+0x49e ~ +0x4a6 RpcpReportFatalError(2, 상태)` + `INT3`. **`0xc0000001` 은 셋 어디에도 없다 → 치명.**
- **통지 하나에 받기 한 번**: `IoCompleted` 안 받기 부름은 `+0xc8` 하나. `+0x481 JMP +0x98` 은 버퍼를 못 얻었을 때 비상 버퍼로 같은 받기를 하러 가는 길이라 횟수는 그대로 1. 메시지 하나를 다 처리하면 (`+0x22c MessageReceived`) 끝 — 더 꺼내려고 도는 줄은 없다.
- 받은 메시지 ALPC 종류 (`+0x04` 아래 바이트) 가 2 면 답, 5 면 포트 닫힘, 1 이면 서버가 보낸 콜백 요청 (run133 1-3). 종류 2 의 `Type` 에 `0x2000` 비트가 없으면 (`+0x6bc ~ +0x6c8`) 확인 응답을 안 되보낸다 `[추측: 비트 이름 · 뜻 안 읽음]`.

### 3. NDR 본문 바이트 (질문 3) — NDR 2.0 판

#### 3-0. 어디서 읽었나 (`pefile`)

| 모듈 | 프록시 정보 | 문법 표 (항목 2개) | NDR 2.0 프로시저 | NDR64 프로시저 (머리만 읽음) |
|---|---|---|---|---|
| rpcrt4 EPM (`e1af8308…` v3.0) | `0xe11e8` | `0xe7560` (NDR 2.0 `8a885d04…` · NDR64 `71710533…`) | 시작 `0xefeb2` + 오프셋 `[0, 30, 60, 144, 216, 258, 288, 318, 396]` → **프로시저 8 = `0xf003e`** (3 = `0xeff42`, 7 = `0xefff0`), 타입 문자열 `0xefb62` | `0xe7e40` → 8 = `0xe7870` |
| winnsi (`7ea70bcf…` v1.0) | `0x6000` | `0x6150` | 시작 `0x70c2` + `[0, 66, 132, 204, 270, 336, …]` → **프로시저 5 = `0x7212`**, 타입 문자열 `0x6eb2` | `0x6940` → 5 = `0x66e0` · 6 = `0x6590` · 7 = `0x6360` |

- 짚을 것: run133 이 적은 EPM "`0xefeb2`" 는 NDR 2.0 프로시저 문자열 **시작** (프로시저 8 은 +396), winnsi `0x66e0` · `0x6590` · `0x6360` 은 **NDR64** 프로시저 5 · 6 · 7 이다 (결과서 3-0).

#### 3-1. EPM 프로시저 8 (`ept_map_auth_async` `[추측: 이름 대응]`)

- 형식 머리 `[정적]`: 프로시저 번호 8 · 스택 0x50 · 명시적 핸들 `FC_BIND_PRIMITIVE` (스택 8) · 클라이언트 상수 0x84 · 서버 0x70 · Oi2 깃발 `0xc3` (비동기 핸들 있음) · 파라미터 8개.
- 파라미터 (스택 `0x10 ~ 0x48`) `[정적]` — 이름은 인자 순서로 대응 `[추측]`: ① 객체 UUID (입력 · 참조, 16 B 구조체) ② 질문 탑 (입력 · 참조, `FC_CSTRUCT` + 1바이트 배열, 범위 0 ~ 2000) ③ SID (입력, **`FC_UP`**) ④ 끝점 핸들 (입력 · 출력, 컨텍스트 핸들 20 B) ⑤ `max_towers` (값, `FC_RANGE` 0 ~ 500) ⑥ 탑 개수 (출력 LONG) ⑦ 탑 배열 (출력, `FC_BOGUS_ARRAY` — 크기 = `max_towers`, 실제 = `*num_towers`, 원소 `FC_UP` → 탑) ⑧ 상태 (출력 LONG).
- `LookupNextChunk +0x11d` 의 인자 순서 (run133 3-1) 가 위 스택 자리와 그대로 맞는다.
- **공개 `ept_map` (프로시저 3) 과 다르다**: 3 은 객체 · 탑이 전체 포인터 (`FC_FP`) 라 포인터 ID 4 B 가 앞에 붙고, 8 은 참조 포인터라 **ID 가 없다** — 공개 명세 그대로 쓰면 안 된다.

요청 스텁 (요청 머리 `+0x68` 부터, 질문 탑 octets 66 일 때 **120 B**) — 칸 · 순서 · 크기 `[정적]`, 패딩 자리 `[규칙]`:

| 스텁 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 16 | 객체 UUID (없으면 16 B 0) |
| `+0x10` | 4 | 질문 탑 conformance (= 66) |
| `+0x14` | 4 | `tower_length` (= 66) |
| `+0x18` | 66 | 탑 octets (층 수 u16 + 4층, run133 3-1) |
| `+0x5a` | 2 | 패딩 |
| `+0x5c` | 4 | SID 포인터 ID (NULL = 0) |
| `+0x60` | 20 | 끝점 핸들 (처음엔 20 B 0) |
| `+0x74` | 4 | `max_towers` (= 4) |

응답 스텁 (응답 머리 `+0x40` 부터, 탑 1개 · octets 73 일 때 **128 B**):

| 스텁 오프셋 | 크기 | 뜻 |
|---|---|---|
| `+0x00` | 20 | 끝점 핸들 — **20 B 0 이면 "더 없음"** (클라이언트가 다음 조각을 안 물음, run133 3-2) |
| `+0x14` | 4 | `num_towers` |
| `+0x18` | 4 | 배열 max_count (= 요청의 `max_towers`) |
| `+0x1c` | 4 | 배열 offset (0) |
| `+0x20` | 4 | 배열 actual_count (= `num_towers`) |
| `+0x24` | 4 × n | 원소 포인터 ID (0 이 아닌 값) |
| `+0x28` | 4 | 탑 1 conformance (`0x49`) |
| `+0x2c` | 4 | 탑 1 `tower_length` (`0x49`) |
| `+0x30` | 73 | 탑 1 octets (run133 3-3 의 `LRPC-emu` 탑) |
| `+0x79` | 3 | 패딩 |
| `+0x7c` | 4 | 상태 (0) |

- 탑 0개: 끝점 핸들 0 · `num_towers` 0 · 배열 `4 · 0 · 0` · 상태 → **40 B** `[규칙]`. 응답 머리 `TotalLength = 0x40 + 스텁`.
- `[공백]` 클라이언트가 [out] 배열 max_count 를 자기 `max_towers` 와 맞춰 보는지 (NDR 해석기 본문 안 읽음) — **같게 보내는 게 안전.**

#### 3-2. nsi 프로시저 5 (`RpcNsiRegisterChangeNotification`)

- 형식 머리 `[정적]`: 프로시저 5 · 스택 0x30 · 핸들 스택 8 · 클라이언트 상수 0x18 · 서버 0x40 · `0xc6` (반환값 있음 · 비동기 핸들) · 파라미터 4개 (입력 구조체 · compartment 8 B · 출력 컨텍스트 핸들 · 반환 LONG).
- **응답 스텁 24 B**: `+0x00` 컨텍스트 핸들 20 B (속성 u32 0 + 0 이 아닌 GUID) · `+0x14` 반환 DWORD (0 = 성공). 실패 대답은 핸들 20 B 0 + 반환에 오류 값 (run133 4-2 의 `0x490` · `8` · `0x139f` …).
- 요청 스텁 40 B (서버 흉내는 안 읽어도 됨): `+0x00` 예약 u32 · `+0x04` 모듈 ID 포인터 ID · `+0x08` 객체 번호 · `+0x0c` 모듈 ID `Length` u16 · `+0x0e` 형 u16 (1 = GUID) · `+0x10` GUID 16 · `+0x20` compartment 8. (해석은 "`FC_STRUCTPAD` 는 선 위에 안 나감" · "비캡슐 공용체 가름값은 앞 멤버" 같은 NDR 규칙에 기댐 `[규칙]` — 클라이언트 상수 0x18 과 앞 12 + 8 = 20 이 맞아떨어짐.)

#### 3-3. NDR64 판

- 읽은 것 `[정적]`: 프로시저 머리만 — EPM 8 (`0xe7870`) 스택 0x50 · 클라이언트 0x8c · 서버 0x7c · 파라미터 8 (속성 · 스택은 NDR 2.0 판과 같음), winnsi 5 (`0x66e0`) 스택 0x30 · 클라이언트 0x18 · 서버 0x44 · 파라미터 4.
- **`[공백]`**: 타입 서술자를 바이트로 안 풀었다. 규칙상 포인터 ID · conformance 류가 8 B (정렬 8) 라 EPM 8 은 위치가 모두 바뀐다. nsi 5 응답은 NDR 2.0 과 같은 24 B 일 가능성이 크다 `[추측]`. → Q259 ①.

### 4. bind 에서 고르는 문법 (질문 4) [정적]

- **클라이언트는 고르지 않는다.** `CompleteBind` (run133 1-3) 가 수락 마스크 0 → `0x6c2`, 제안의 부분집합 아님 · 비트가 정확히 하나가 아님 → `0x6c0`. 그래서 **둘 다 켠 답은 거부**. 제안은 비트 1 → 2 → 4 순으로 OR (`Bind__4e330 +0x4e0` · `+0x713` · `+0x8a5`).
- 고른 비트 → 연결 객체 슬롯 (`+0x4c` · `+0x4e`) → 요청 머리 `+0x38`. 그 슬롯이 NDR 2.0 이면 `Ndr64pClientSetupTransferSyntax` 가 문법 정보 첫 dword `0x8a885d04` 를 알아보고 **DCE 엔진** (`AsyncDceClient` …), 아니면 **NDR64 엔진** (`AsyncNdr64Client` …) — 디컴파일 `if (*param_5 == -0x7577a2fc)` 갈래 (`param_5` 가 그 정보라는 것은 `[추측: 인자 이름]`).
- 서버 흉내가 하나만 고를 때 **마스크 1 (NDR 2.0)** 이 안전하다 — 코드가 두 길 다 받고, 이번에 바이트로 푼 쪽이 NDR 2.0 이고, 공개 명세 (C706 14장) 와 비교할 수 있어서. 진짜 epmapper · nsisvc 가 무엇을 고르는지는 정적으로 못 정한다 (Q258).

### 5. 서버 흉내 차례 — run133 5절을 고쳐 다시 적은 판 (결과서 5절)

**불변식: 포트에 쌓이는 메시지 수 = 완료 포트에 들어가는 패킷 수 = 받기 부름 수.** 단계 번호는 run133 5절 (= sast/130 5절).

| 단계 | 클라이언트가 하는 것 | 서버 흉내가 할 일 | 근거 |
|---|---|---|---|
| ① 연결 | `NtAlpcConnectPort(\RPC Control\epmapper, Flags 0, 빈 8 B 속성 버퍼)` | 핸들 쓰고 0 (run133 그대로) | run129 |
| ①b | `NtAlpcQueryInformation(핸들, 0, &버퍼, 0x10, NULL)` | 실패해도 됨 (→ 받기 버퍼 `0x1000`). 성공이면 `Flags & 0x1000000` 0 으로 (같은 결과). 켜면 `0xff00` | 1-1 |
| **①c [새]** | **`NtAlpcSetInformation(핸들, 2, &{키, 풀 IO 완료 포트 핸들}, 0x10)`** | 길이 0x10 확인 → 핸들을 IO 완료 객체로 (접근 2) → **포트에 `completion_port` · `completion_key` 보관**, 0. 실패 status 는 1-3. 붙이기 전 쌓인 메시지 수만큼 패킷 (연결 직후라 0) | 1-3 |
| **①d [새]** | (풀 첫 묶임이면) `NtSetInformationWorkerFactory(공장, 3, &1, 4)` | run125 몸 (묶음 수 칸만) | 1-1 |
| ② 핸들 질문 | ①b 로 흡수 | | |
| ③ epmapper bind | `NtAlpcSendWaitReceivePort(핸들, 0, bind 0x70, 속성, NULL …)` | 보내기 **즉시 0**. 답 (종류 2 · LRPC `+0x28` 1 · `+0x48` 1 또는 2 · `+0x08` PID · Context `MessageContext` = 보낸 값) 을 **그 포트 받기 큐 끝에 넣고, 같은 동작 안에서 패킷 하나** `{키 = completion_key, 0, 0, 0}` | 2-1 |
| **③b [새]** | 일꾼이 `NtWaitForWorkViaWorkerFactory` 로 패킷 → `TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted` (유저 — 에뮬레이터가 할 일 없음) | 이미 있는 0x20 B 항목 출력에 위 4칸만 | 2-2 |
| **③c [새]** | `IoCompleted +0xc8`: `NtAlpcSendWaitReceivePort(핸들, **0**, NULL, NULL, 버퍼, &0x1000 / 0xff00, &{0xfa000000}, NULL)` | **큐 머리 하나** → 버퍼에 헤더 0x28 + 데이터, 받기 속성 (Context `MessageContext` = 요청 때 값, DataView · Handle 끔), 0. 큐가 비면 (완료 포트 붙은 포트) **`0xc0000001`, 아무것도 안 씀, 안 기다림** — rpcrt4 가 치명 처리하므로 이 상황이 생기면 안 됨 | 2-3 |
| ④ `ept_map` | 요청 (`+0x28` 0 · `+0x3c` 8 · 스텁 = 3-1 요청 표) → 보내기 즉시 0 | 답 (종류 2 · LRPC `+0x28` 3 · `+0x34` 호출 ID · 스텁 = **3-1 응답 표**) 을 큐에 + 패킷 하나 → ③b · ③c | 3-1 |
| ⑤ 두 번째 연결 | `\RPC Control\<탑 4층 끝점>` | ① 과 같음. **새 핸들이라 ①b · ①c 가 다시 온다** (포트마다 따로 붙음). ①d 는 풀 묶음 수가 0 → 1 일 때만이라 안 옴 | 1-1 |
| ⑥ nsi bind | ③ 과 같은 꼴 (`+0x34` `7ea70bcf…`, `+0x48` 3) | ③ 과 같음 | |
| ⑦ 프로시저 5 | 요청 (`+0x3c` 5, 스텁 = 3-2 요청 40 B) | 답 (스텁 = **3-2 응답 24 B**) 을 큐에 + 패킷 하나 | 3-2 |
| ⑧ 프로시저 7 | 요청 (`+0x3c` 7, 컨텍스트 핸들 20 B) | **큐에도 완료 포트에도 아무것도 안 넣는다** | run133 4-3 |
| ⑨ 끊기 | | 서버가 닫으면 **종류 5 메시지 + 패킷 하나** (`HandleConnectionClose`). 클라이언트가 닫으면 완료 포트 객체 참조를 놓는다 `[추측: 닫는 쪽 커널 안 읽음]` | 2-1 |

- **패킷 넣는 시각**: 진짜 커널은 서버가 답할 때 넣는다 — 클라이언트 보내기가 돌아오기 전일 수도 뒤일 수도 (서버는 따로 도는 스레드). rpcrt4 는 보내기 전에 호출 상태를 먼저 써 두니 어느 쪽이든 견딘다 `[추측: 상태 쓰는 순서는 run133 1-3]`. 에뮬레이터는 "보내는 스레드가 syscall 에서 나간 뒤 일꾼이 받게" 가 결정적이다 (결과서).
- 메시지 k 개 → 패킷 k 개 (합치지 않음).

### 호출 관계

```
rpcrt4!LRPC_CASSOCIATION::Connect +0x3b6 EnableAsync                                   [정적]
 │   +0x64 [연결객체+0xe0] = 썽크 LrpcCAssociationConnectIoHandler
 └─ +0x6f  CreateAlpc  +0x6d  ntdll!TpAllocAlpcCompletionEx (Ex = 1) +0x18 TppAllocAlpcCompletion
            ├─ +0xa9   RtlAllocateHeap (0x128)            TP_ALPC +0x38 · +0x98 · +0xa0 · +0xd8 · +0x110 · +0x120
            ├─ +0xef   TppCleanupGroupMemberInitialize     (환경 NULL → 전역 풀, run125)
            ├─ +0x1d6  NtAlpcSetInformation(포트, 2, {키, 풀+0x40}, 0x10)  ── SYSCALL
            │            ⇒ nt!NtAlpcSetInformation +0x140 AlpcpAssociateIoCompletionPort
            │                 포트 +0x20 완료 포트 객체 · +0x28 키 · +0x30 쌈 (이미 붙음 0xc0000048)
            └─ +0x1e9  TpAdjustBindingCount(+1) +0x40 NtSetInformationWorkerFactory(3)  (첫 묶임만)

nt!AlpcpDispatchNewMessage +0x2e8 AlpcpCompleteDispatchMessage                           [정적]
 │   +0x541 MainQueue 에 먼저
 ├─ +0xeb9  AlpcpQueueIoCompletionPort   {키, 0, 0, 0}  — 메시지 하나 = 패킷 하나
 └─ (동기 · 보내기 길) 표시만 → NtAlpcSendWaitReceivePort +0x17b AlpcpSignal +0xa36 AlpcpQueueIoCompletion

ntdll!TppWorkerThread                                                                    [정적]
 ├─ +0x37e  NtWaitForWorkViaWorkerFactory   항목 0x20 B {키, ApcContext, 상태, 정보}  (커널 쪽 run125)
 ├─ +0x473  TppPrepareDirectParams
 └─ +0x563  TppAlpcpExecuteCallback          ← [키+0x38] == 0x19aa0
             └─ +0x44e  ⇢ LrpcIoComplete(인스턴스, [TP_ALPC+0xa0], TP_ALPC, ApcContext)
                          └─ +0xc8  ⇢ LrpcCAssociationConnectIoHandler  (RCX −= 0xe0, RDX = 0) ⇢ IoCompleted
                                       ├─ +0xcf  NtAlpcSendWaitReceivePort  받기만 · Flags 0 · 한 번
                                       │           ⇒ AlpcpReceiveMessage +0x2df AlpcpReceiveMessagePort
                                       │                (빈 큐 + 완료 포트 → +0x1ba 0xc0000001)
                                       └─ +0x4ab RpcpReportFatalError   (0xc0 · 0xc0000041 밖)
```

- (나무 오프셋은 ret 자리. `⇢` 는 간접 호출 · 꼬리 점프, `⇒` 는 syscall → 커널. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` 완료 포트 붙이기는 `TppAllocAlpcCompletion +0x1d1` `NtAlpcSetInformation(포트, 2, {TP_ALPC 포인터, [풀+0x40]}, 0x10)` 한 번. 같은 포트 · 완료 포트에 다른 syscall 없음.
- `[정적]` 커널 종류 2 검사 차례 · 실패 status (1-3 표) — 길이 틀림 `0xc0000004`, 이미 붙음 `0xc0000048`, 완료 목록 `0xc00000bb`, 완료 포트 핸들은 접근 2 로 참조.
- `[정적]` 패킷 = `{포트 +0x28 키, ApcContext 0, 0, 0}`, 메시지 하나에 하나, 메시지를 큐에 넣은 뒤. 동기 · 보내기 길은 `AlpcpSignal` 로 미뤄 넣음.
- `[정적]` 일꾼 → `TppAlpcpExecuteCallback` → `LrpcIoComplete(인스턴스, &연결객체+0xe0, TP_ALPC, ApcContext)` → 썽크 → `IoCompleted(연결객체, NULL)`. **run133 의 "`LrpcIoComplete +0xc3` 간접 대상 = `IoCompleted` `[추측]`" 은 이제 `[정적]` — 직접 대상은 썽크, 썽크가 꼬리 점프** (`EnableAsync +0x53 · +0x64` 가 쓰는 칸 · `TppAllocAlpcCompletion` `+0xa0` · `TppAlpcpExecuteCallback +0x43b` · `LrpcIoComplete +0xba` 의 데이터 흐름).
- `[정적]` 콜백 뒤 다시 켜는 syscall 없음.
- `[정적]` 빈 받기 (완료 포트 붙은 포트) = `0xc0000001`, 안 기다림, 아무것도 안 씀 → rpcrt4 치명. 통지당 받기 한 번.
- `[정적]` EPM 프로시저 8 · nsi 프로시저 5 의 NDR 2.0 형식 문자열 (파라미터 · 형 · 스택) — 바이트 위치 · 패딩은 `[규칙]`.
- `[정적]` 문법은 서버가 고른 비트 하나를 그대로, 두 엔진 다 있음.

## 정정 (고쳐 읽기 — run133)

run133 은 틀린 걸 단정한 줄은 없고, `[공백]` · `[추측]` 으로 둔 곳이 이번에 닫히거나 좁혀졌다. run133 문서 맨 위에 "일부 닫힘 · 고쳐 읽기 (run137)" 한 줄을 넣었다.

- **닫힘**: "아직 확인 필요" 정적 2 (완료 포트 연결 + 통지가 `LrpcIoComplete` 까지) · 3 (NDR 형식 문자열 — EPM 8 · nsi 5 의 NDR 2.0 판) · 5 (`NegotiateTransferSyntax` — 문법 좁히기), 그 밖 `[공백]` "비동기 받기에 쌓인 답 없음 일 때 정상 status" (→ `0xc0000001`, 정상이 아니라 치명). 5절 남은 `[공백]` 둘 중 "NDR 스텁 본문 바이트 (④ · ⑦)" 는 NDR 2.0 판으로 닫힘, "서버가 ③ · ⑥ 에서 고르는 문법" 은 "클라이언트는 따름 · 흉내는 1 권장" 까지 좁혀지고 진짜 값은 동적 (Q258).
- **고쳐 읽기**:
  1. 2-4 · 5절 ③ · PE_emulator 3번의 받기 Flags "0 / `0x1000000`" — **클라이언트 연결 길은 늘 0** (썽크가 완료 문맥을 NULL 로 넘김). `0x1000000` 갈래와 "`MessageId` / `CallbackId` = 완료 문맥 것" 은 이 길에서 안 쓰인다.
  2. 호출 지점 표 · 2-4 · 나무의 `LrpcIoComplete +0xc3` 간접 대상 — `IoCompleted` 가 아니라 **썽크 `LrpcCAssociationConnectIoHandler`** (rva `0x96a70`) 이고, 썽크가 `IoCompleted` 로 꼬리 점프. 효과는 같다 (`[추측]` → `[정적]`).
  3. 5절 ⑦ 요청 스텁 "입력 24 B + 모듈 ID 24 B + compartment 8 B" `[추측]` → NDR 2.0 판 **40 B** (12 + 20 + 8). 응답 "컨텍스트 핸들 20 B + DWORD 0 (정렬로 24 B `[추측]`)" → **24 B** (`[정적]` 형식 + `[규칙]`).
  4. 5절 ④ "질문 탑 70 B 포함 `[추측: NDR 틀]`" · 3-3 "NDR 틀은 공개 EPM IDL 순서로 짐작" → 순서는 맞았고, **프로시저 8 은 객체 · 탑이 참조 포인터라 포인터 ID 가 없다** (공개 `ept_map` 3 과 다름). 요청 120 B 표 (3-1).
  5. PE_emulator `:602` "다른 클래스 (완료 포트 연결로 짐작 `[추측]`)" → **종류 2** `[정적]`.
  6. 1-3 "클라이언트가 `Ndr64AsyncClientCall` 을 쓰니 NDR64 가 자연스러워 보이지만 `[추측]`" → 같은 비동기 진입이어도 슬롯이 NDR 2.0 이면 DCE 엔진으로 간다 — 진입 함수 이름으로 문법을 짐작할 근거는 없어졌다.
- run129 "아직 확인 필요" 4 (= Q232 ①, `CreateAlpc → TpAllocAlpcCompletionEx → NtAlpcSetInformation`) 도 이번에 닫혔다 — 이전 문서는 안 고치고 flow.md 4장 run129 정적 줄에만 표시.

## PE_emulator 쪽 (결과서 6절 "지금 우리 맞나·틀리나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 곳 | 판정 | 까닭 · 할 것 |
|---|---|---|
| `nt_alpc_create_port.cpp:581 ~ :602` 공통 앞부분 | **맞다** | 핸들 NULL · 입력 NULL · 길이 > 0x48 → `0xc0000004` · 종류 · 접근 1 — 커널 1-3 표 1 ~ 4행과 같은 차례 · 값 |
| 같은 함수, 종류 5 | **맞다** | `NtAlpcSetInformation +0x472 ~ +0x482` (길이 0x10 → 0, 아니면 `0xc000000d`) |
| 같은 함수, **종류 2 (`:602` `stop`)** | **틀림 (빠짐)** | ① 길이 ≠ 0x10 → **`0xc0000004`** ② 입력 `{키 @0, 핸들 @8}` ③ 핸들 0 → `0xc000000d` ④ **이미 붙음 → `0xc0000048`** (핸들 참조보다 앞) ⑤ 완료 목록 → `0xc00000bb` ⑥ IO 완료 객체로 참조 (**접근 2**, 실패 status 그대로) ⑦ 포트에 `completion_port` · `completion_key` → 0. `AlpcPort` 에 두 칸이 없으면 늘린다 |
| `:612 ~ :626` 비동기 보내기 ("받기 칸 0 이면 곧바로 0") | **맞다** | 커널도 받을 메시지 포인터 0 이면 `AlpcpReceiveMessage` 를 건너뜀 (`+0x112 ~ +0x11d`). 단 **답을 큐에 넣을 때 패킷 하나를 같이** (빠짐, 5절 ③) |
| 같은 함수, 받기만 ("쌓인 답 넘김") | **절반 맞다** | 큐 머리 하나 → 0 은 맞음. 더 필요: (i) 큐가 비면 완료 포트 붙은 포트는 **`0xc0000001` 즉시, 아무것도 안 씀** (ii) 닫힘 `0xc0000700` · 거절 `0xc0000041` (iii) 버퍼 작음 `0xc0000023` + 길이 칸 · 메시지 남김 (iv) Flags `0x1000000` 은 이 길에 안 옴 |
| `src/threadpool/nt_io_completion.cpp` (패킷 넣기) | **맞는 틀** | 같은 큐에 `{키 = completion_key, apc 0, status 0, information 0}` 한 줄을 넣는 길만 새로. `NtWaitForWorkViaWorkerFactory` 출력 0x20 stride (`nt_wait_for_work_via_worker_factory.cpp:135 ~ :139`) 는 커널과 맞다 (결과서) |
| `src/rpc/lrpc.cpp:108` (`svc_async_unsupported`) | 이번 결과가 필요한 자리 | 비동기 갈래를 열려면 5절 ③ · ③b · ③c |
| `src/rpc/lrpc.cpp:68` 문법 선택 (마스크 3 → NDR64) | `CompleteBind` 엔 유효 | NDR64 바이트는 아직 없음 → EPM · nsi 는 `(mask & 1) ? 1 : 2` 로 NDR 2.0 우선 권고 |
| 새 `src/rpc/` — epmapper `ept_map` | (미구현) | 3-1 응답 표 (128 B / 탑 0개 40 B), 요청 `+0x3c == 8`, 응답 `+0x34` = 요청 호출 ID, 끝점 핸들은 늘 NULL 이면 한 번에 끝 |
| 새 `src/rpc/` — nsi 프로시저 5 | (미구현) | 3-2 응답 24 B. 요청은 안 읽어도 됨. 프로시저 7 은 답 안 함 |

- 같은 비동기 틀을 RPCSS (`CoInitializeSecurity`, 요청 쪽 21 샘플) 도 쓴다 (원 요청 말) — 붙이기 · 패킷 · 빈 받기 규칙은 rpcrt4 LRPC 클라이언트 공통 몫이다 `[추측: RPCSS 쪽 연결 객체가 같은 썽크를 쓰는지는 안 읽음]`.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 9 — 대기열 **Q258 (동적: CompleteBind 진짜 문법 · TppAllocAlpcCompletion +0x1d6 인자·반환 · TppAlpcpExecuteCallback 입구 패킷 · AlpcpReceiveMessagePort +0x1ba 안 걸림 · 패킷 시각 · 스텁 길이)**. Q231 ④ · ⑤ 와 같은 세션 — nsisvc 가 도는 VM + winnsi 등록을 부르는 프로그램, test.exe 는 안 부름, 고르기는 사용자 결정. 유저 멈춤점은 그 프로세스 조건):
1. 진짜 서버가 bind 답에서 고르는 문법 — `rpcrt4!LRPC_CASSOCIATION::CompleteBind` 입구 `db @rdx L0x70` `+0x48` (1 / 2) · `+0x28` · `+0x08`, epmapper · nsi 둘 다 (run133 동적 2 와 같은 줄).
2. 붙이기 인자 — `ntdll!TppAllocAlpcCompletion+0x1d6` (call `+0x1d1`) 또는 `NtAlpcSetInformation` 입구 + 반환 주소 조건: `rcx` 포트 · `edx` 2 · `r9d` 0x10 · `dq @r8 L2` = {TP_ALPC, 풀 IO 완료 포트 핸들}, 반환 `eax` 0. `[TP_ALPC+0xd8]` = 풀, `[풀+0x40]` = 핸들 대조.
3. 첫 묶임 `NtSetInformationWorkerFactory(…, 3, …)` 가 가나 — `ntdll!TpAdjustBindingCount+0x40` `edx == 3`.
4. 패킷 진짜 값 — `ntdll!TppAlpcpExecuteCallback` 입구, 조건 `poi(@rdx+0x98) == rpcrt4!LrpcIoComplete`: `r8` (0 일 것) · `dq @r9 L2` ({0, 0}). 커널 쪽 `nt!AlpcpQueueIoCompletionPort` 입구 (`dl` 0 · `[rcx+0x28]` 키).
5. 빈 받기가 실제로 오나 (오면 안 됨) — `nt!AlpcpReceiveMessagePort+0x1ba`. 걸리면 `rpcrt4!IoCompleted+0xcf` 의 `rsi`.
6. 패킷이 보내기 syscall 안에서 먼저냐 나중이냐 — `AlpcSend+0x12f` · `Bind__4e330+0x62d` 반환과 `TppAlpcpExecuteCallback` 입구의 로그 순서.
7. `NtAlpcQueryInformation` 결과 `Flags` — `rpcrt4!IsPortWithSharedSection+0x2c` `[rsp+0x30]` (`0x1000000` → 받기 버퍼 `0xff00`).
8. EPM 8 · nsi 5 진짜 스텁 바이트 — 요청 `AlpcSend` (`+0x68` 부터, 120 / 40) · 응답 `IoCompleted+0xcf` (`+0x40` 부터, 128 / 24) · `+0x38` 슬롯 문법 (run133 동적 3 · 4 · 5).
9. [out] 배열 conformance 대조 — 응답 `+0x18` 을 `max_towers` 와 다르게 준 실험 (`RPC_X_INVALID_BOUND` 류).

정적 (결과서 "다음 요청 후보" 1 ~ 5 — 대기열 **Q259 (정적: 결과서 다음 요청 후보 1~5 — NDR64 판 · AlpcpSendMessage→AlpcpDispatchReplyToPort · NtWaitForWorkViaWorkerFactory 접점 · AlpcpSignalAndWait · NsiRegisterChangeNotificationEx·WNF 이월)**):
1. NDR64 형식 풀기 — EPM 8 (`0xe7870`) · nsi 5 (`0x66e0`) 타입 서술자 (3-3 `[공백]`). 서버 흉내가 NDR64 를 고를 때만.
2. 서버 쪽 답 보내기 `AlpcpSendMessage` → `AlpcpDispatchReplyToPort` — 답 메시지 `Type` · `MessageId` · Context 속성을 커널이 어떻게 만드나.
3. `NtWaitForWorkViaWorkerFactory` 커널 쪽 + `TppPoolpReferenceGlobalPool` 과 1-1 의 접점 (run125 이후 대조).
4. `AlpcpSignalAndWait` — ApcContext 에 −1 · 0 이 아닌 값이 들어가는 길 (서버 쪽 `ServerStartingToListen` 썽크와 함께).
5. run133 후보 4 · 6 이월 (`nsi!NsiRegisterChangeNotificationEx` · `RegisterNsiSvcRestartNotification` WNF) — Q246 ④ · ⑥ 과 같은 것.

- 그 밖 `[공백]`: 클라이언트가 포트를 닫을 때 완료 포트 연결을 푸는 커널 줄 (닫는 쪽 안 읽음), `PrivatePool` 을 켜는 함수, `[TP_ALPC+0x120]` 비트 1 을 켜는 함수 (완료 목록 쪽), 종류 2 메시지 `Type 0x2000` 비트 뜻, `AlpcpLookupMessage +0x331` 의 `0xc00002f0` (아래 대조).

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- (보탬) `TppAllocAlpcCompletion` 은 **syscall 바로 앞에서 한 번 더** 검사한다: 풀 `[객체+0xd8]` NULL (`+0x18d · +0x190`) · PEB `Ldr+0x48` ≠ 0 (`+0x1a3 · +0x1a7` → `+0x222`) → `+0x22a TppRaiseInvalidParameter` (ret `+0x22f`) → `0xc000000d`, 객체는 정리 (`+0x23d` · `+0x2ad`). 결과서 1-1 차례 5 는 들어올 때 검사 (`+0x3f ~ +0x77` → `+0x27b`) 만 적었다.
- (보탬) `CreateAlpc +0x42 LEA R8,[0x180053510]` = `LrpcIoComplete` 시작 (rva `0x53510`) — 콜백 인자가 코드 줄로 보인다. `EnableAsync` 첫 길은 `+0x4c LEA R14,[RBX+0xe0]` · `+0x5a MOV R8,R14` 로 문맥을 넘긴다 (둘째 길 `+0x9e`).
- `AlpcpLookupMessage +0x331` 은 `MOV EAX,0xc00002f0` — 결과서 2-3 직접 받기 행의 status 목록 (`0xc0000702` · `0xc0000708` …) 에 이 값이 빠졌다. 이 길 (Flags 0) 과 무관해서 표에만 적는다 (이름은 안 붙임).
- `IoCompleted +0x6bc ~ +0x6ce` 는 `MOV EAX,0x2000` · `TEST word [RBX+4],AX` (결과서 "`TEST [RBX+4],0x2000`" 과 같은 뜻).
- 그 밖 결과서 오프셋 · 바이트는 `.asm` · `pefile` 과 **일치** (위 "방법" 의 줄 전부).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- `TppWorkerThread` 디컴파일은 스택 프레임이 뒤섞여 읽기 어렵다 — 결과서가 `.asm` 줄로만 확인 (기록만, 하네스 제안 아님).
- `ms/disassemble/` 을 같은 시간에 다른 decompiler 둘 (ntoskrnl · wow64) 이 쓰고 있었다. flow-writer 가 읽은 ntoskrnl `.asm` (`Alpc*` · `NtAlpc*`) 은 이번 sast/132 가 뽑은 것 그대로였다 (결과서 줄과 일치).
