# 2026-11-27 (작성일, 정적 분석 2026-11-27) run155 — winnsi 프로시저 6 · 7 형식 · 취소 메시지 · nsi 포트의 종류 0 · 1 밖 메시지 (Q292, 정적만): **6 응답 24 B 는 맞고, 6 요청은 52 B 가 아니라 56 B** (모듈 ID GUID — LUID 면 48, 포인터 NULL 이면 32) 일 것이다 — 모듈 ID 안의 비캡슐 공용체가 **가름값 2 B 를 선 위에 한 번 더** 쓰기 때문 (`NdrNonEncapsulatedUnionMarshall` 의 줄은 `[정적]`, 그 함수가 이 길에서 불린다는 사슬은 `[추측]`). 같은 까닭으로 **프로시저 5 요청도 40 B 가 아니라 44 B** 후보 → run137 3-2 고쳐 읽기. **7 요청 = 컨텍스트 핸들 20 B 하나, 7 완료 응답 = 32 B (끝 4 B 반환값 `0x4c7`)** 이고 진짜 서버는 걸린 7 을 **fault 가 아니라 일반 응답** 으로 끝낸다. 취소 (`RpcAsyncCancelCall`) 가 보내는 것은 **LRPC 종류 4, 0x40 B, Flags `0x10000`, 보내기만** — 서버는 답하지 않는다. 클라이언트의 **2 ms 잠 (`AlpcCancelMessage`) 은 요청 때 커널에 넘긴 Context 속성의 `MessageId` / `CallbackId` 가 0 이 아니게 될 때만 끝난다** (시간 제한 없음). 클라이언트가 보내는 종류 0 · 1 밖은 **4 (취소) 와 5 (예약 메시지 묶음) 둘뿐**이고, 원문 표본 (`svc_lrpc_kind_unsupported`) 은 **5** 쪽이다

(글의 오프셋은 **함수 시작 기준** `+0x…` — call 자리, ret 는 아래 "정적 — 호출 지점" 표. `.rdata` 자리는 각 모듈 rva. 결과서가 갈래 시작 줄이나 그 앞 줄을 call 자리로 적은 곳은 call 줄로 바꿔 적고 결과서 값은 괄호에만 둔다 (아래 "flow-writer 대조"). 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **"덤" 표 세 줄**, 읽기만 — "지금 막는 곳 없음 · 작음 · 한 줄씩이면 됨") 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 rpcrt4 · winnsi 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 · run141 · run150 과 같은 줄), `sample/*.c` 에 RPC API 가 없다 `[소스]`. nsisvc 는 **다른 프로세스** (서비스). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (rpcrt4 · winnsi 줄 한 마디 · 근거 줄) · 3장 run129 · run133 · run137 절 한 줄씩 + 새 절 "nsi 걸린 호출 취소 · 예약 메시지" · 3장 run137 절 "요청 40 B" 옆 표시 · 4장 run137 해결 줄 표시 · run155 줄 셋 · 5장 run155 두 줄만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q292** (380 덤 1 ~ 3, 사용자 결정 2026-11-27 PE_emulator D179 — 380 묶음 맨 끝, 정적만). 결과서 판정 **분석성공 (덤 1 · 2) / 부분성공 (덤 3)** — 덤 3 은 "종류 ≠ 0 · 1 은 4 · 5 뿐" 은 닫았고, 원문 표본이 5 라는 것은 PE_emulator 이유 문자열로 가른 것이라 표본 로그 (지워짐) 로는 확인 못 함.
> - 이어 쓰는 앞 run: **run129** (winnsi 프로시저 5 · 6 · 7 쓰는 곳 · 3절 표), **run133** (LRPC 종류 0 / 1 / 2 / 3 / 6 / 10 / 11 · 요청 머리 `+0x34` 호출 ID), **run137** (3-0 winnsi NDR 2.0 시작 `0x70c2` · 3-2 프로시저 5 요청 40 B — 이번 고쳐 읽기 대상), **run141** (비동기 길 · 풀 일꾼), **run150** (같은 손 — `pefile` 로 NDR 2.0 표 읽기).
> - 앞 run 고쳐 읽기 둘: run137 문서 · 해설서 맨 위 한 줄씩 (모듈 ID 20 B → 24 B, 5 요청 40 B → 44 B — 사슬 `[추측]`), run129 문서 맨 위 한 줄 (3절 표 7 줄 "24 B 모듈 ID 꼴 · DWORD").
> - 남은 것 → 동적은 Q280 · Q271 RPC 묶음에 얹을 꼴 (결과서 "VM 으로 확인할 것 — 디버거 필요" 1 ~ 4), 디버거 없이는 PE_emulator 로그 한 줄뿐, 정적은 결과서 "다음 요청 후보" 1 ~ 5 — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **덤 1 — 6 · 7 형식** `[정적]` (winnsi `.rdata`, `pefile`): 오프셋 표 `0x7310` `[0, 66, 132, 204, 270, 336, 390, 438, 498]` → 6 = `0x7248` (파라미터 3), 7 = `0x7278` (파라미터 5). 둘 다 비동기 (Oi2 `0xc6` · `0xc5`, 비트 7).
  - 6 = {24 B 구조체 (5 의 첫 파라미터와 같은 타입), 컨텍스트 핸들 (입력 · 출력, 깃발 `0xe0`), 반환}.
  - 7 = {컨텍스트 핸들 (입력, **값** — 깃발 `0x41`), 출력 구조체 A 16 B {바이트 배열 포인터, 길이}, 출력 구조체 B 24 B {enum16, 바이트 배열 포인터, LONG, LONG}, 출력 **enum16**, 반환}.
- **길이** (NDR 2.0, 4 정렬): 6 요청 **56 B** (GUID) / 48 (LUID) / 32 (모듈 ID 포인터 NULL) · 6 응답 **24 B** · 7 요청 **20 B** · 7 완료 응답 **32 B** (`+0x1c` 반환 `0x4c7`) · 5 요청 **44 B** (GUID) / 36 (LUID).
  - 칸 · 크기는 `[정적]` (형식 바이트). **모듈 ID 24 B = Length 2 · Type 2 · 가름값 2 · 패딩 2 · GUID 16** — 가름값이 한 번 더 나가는 줄은 `[정적]` (`NdrNonEncapsulatedUnionMarshall +0x5d ~ +0x65` → `NdrpSimpleTypeEnum16Marshall`), 그 함수가 이 구조체를 풀 때 불린다는 사슬은 `[추측]` → 진짜 길이는 VM.
- **7 을 끝내는 서버 줄** `[정적]`: nsisvc `RpcNsiDeregisterChangeNotification +0x1fb MOV EDX,0x4c7` → `+0x207 NsipUnsubscribeAndCompleteAsyncCall` → `+0x6c RpcAsyncCompleteCall(상태, &[RSP+0x38])` — `[RSP+0x38]` 은 EDX 를 저장한 홈 칸 (`+0x5`) 이라 **반환값 자리에 `0x4c7`**. fault 가 아니다.
- **덤 2 — 취소 메시지** `[정적]`: winnsi `+0x302 RpcAsyncCancelCall(7, 1)` → (`[vtbl+0xa8]`) `LRPC_BASE_CCALL::CancelAsyncCall` → 상태 `[부름+0x108]` 가 9 보다 크고 `0xc` 가 아니면 `+0x8a LRPC_CASSOCIATION::AlpcSendCancelMessage` → **`+0x9a NtAlpcSendWaitReceivePort(포트, 0x10000, 0x40 B 메시지, 나머지 전부 NULL)`**. 메시지 = DataLength `0x18` · TotalLength `0x40` · Type `0x4000` · **`+0x28` = 4** · `+0x34` 호출 ID. 반환값은 안 본다.
- **서버는 답하지 않는다** `[정적]` + `[추측: 비트 뜻]`: 서버 `ProcessIO` 는 이 메시지에 `+0xa0e HandleCancelMessage(연결, 호출 ID)` 만 하고, 답은 `Type & 0x2000` 일 때만 (`+0xa13 ~ +0xa44`). nsisvc 는 취소 알림을 구독하지 않아 걸린 7 은 서버에선 안 끝난다.
- **2 ms 잠의 끝 조건** `[정적]`: `CancelAsyncCall +0xab CancelCall` → `[vtbl+0x150]` `ActualCancelCall` → (상태 10 · 9 의 한 갈래) `+0x69 AlpcCancelMessage` 가 Context 속성 (요청 때 넘긴 `호출+0x128` 칸) 의 **`MessageId` (`+0x14`) · `CallbackId` (`+0x18`) 가 둘 다 0 이면 `+0xee Sleep(2)` 하고 다시 읽는다** (`+0xfa JMP +0x27`). 시간 · 횟수 제한 없음. 진짜 커널이 요청 보낼 때 이 칸을 채운다는 것은 `[추측]` (커널 안 읽음).
- **`NtAlpcCancelMessage` 반환** `[정적]` (`AlpcCancelMessage +0x80`, ret `+0x87`): `0` → 1 (취소됨) · `0xc0000702` · `0xc0000703` → 3 · `0x4000002e` → 2 · `0x103` → 0 · **그 밖 = `RpcpReportFatalError(2, 상태)` + `INT3` (치명)**.
- **덤 3 — 종류 0 · 1 밖** `[정적]`: rpcrt4 의 `NtAlpcSendWaitReceivePort` 부르는 곳 15곳 중 클라이언트가 먼저 보내는 종류는 0 · 1 · **4 (취소, `AlpcSendCancelMessage +0x57`)** · **5 (예약 메시지 묶음, `SendReservedMessage +0x2d1`, Flags 0, 보내기만)** 뿐. 2 · 3 · 6 · 9 · 10 · 11 은 서버 쪽.
- **새 스레드의 첫 호출 앞 5** `[정적: 사슬]` (가상 호출 한 칸 `[추측]`): `NegotiateTransferSyntax +0x49c FindOrCreateCausalFlow` → 이 스레드가 이 바인딩에서 처음이면 `+0xec CleanupOrphanedCasaulFlowEntries` → `+0xa9 SendCausalFlowListForCleanup` → 쌓인 항목 **12개 이상** (`+0x56 CMP [연결+0x128],0xc`) 이면 `+0x62 SendReservedMessage`. 서버는 답하고 (`HandleReservedMessageRequest`), 클라이언트는 안 기다린다.
- **PE_emulator 쪽 한 줄씩** → 끝 "PE_emulator 쪽" 절.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열셋째 바퀴. plan **Q292**. **짐 지도 바퀴 아님.**
- 정적: **rpcrt4 프로젝트만 열었다** (analyze 안 함) — `export` 3번 (rva 지정, depth 0, 함수 27개) + `refs` 15번 (`ms/disassemble/rpcrt4/_refs/`). **winnsi 는 프로젝트를 안 열고** `pefile` 로 `.rdata` + 있던 `ms/disassemble/winnsi/*.asm` 만. nsisvc 는 있던 `.asm` 읽기만. 각 export 앞 `free -g` (결과서).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| winnsi | `907CE42FFA71F5085D1468AD2D40D62C1` | 같음 (run129 · run137). 결과서: 바이너리 디버그 디렉터리 CodeView 와 같음 |
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 같음 (run99 · run133 · run137 · run141 · run145 · run149 · run150) |
| nsisvc | `CE94A7C9096AB626B28E58C48D6605591` | 같음 (run133) — 있던 `RpcNsiDeregisterChangeNotification.asm` · `NsipUnsubscribeAndCompleteAsyncCall.asm` · `RpcNsiRequestChangeNotification.asm` 읽기만 |

- 버전 대조 (결과서 "버전 대조" — 일치): winnsi `NsiRpcDeregisterChangeNotificationEx +0x123 LEA EDX,[R8+6]` · `+0x12e Ndr64AsyncClientCall` · `+0x302 RpcAsyncCancelCall` (ret `+0x309`), `NsiRpcRegisterChangeNotificationEx +0x543` · `+0x54e` (프로시저 7), rpcrt4 `AlpcSendCancelMessage +0x9a` 바이트 `48 ff 15 8f 6d 08 00` → 가져오기 칸 `0xe94c0` (`NtAlpcSendWaitReceivePort`), winnsi 오프셋 표 `0x7310` (5 = `0x7212` — run137 3-0 과 같음). flow-writer 도 `.asm` 줄과 `pefile` 로 같은 값을 다시 봄.

## 목적

- PE_emulator 쪽 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀. 갈래 표본 로그는 지워짐):
  1. **덤 1** — BG03 이 winnsi 6 · 7 몸을 세울 때 스텁 꼴을 규칙으로 지음 (`nsi.cpp` `TODO(VM)`). 6 · 7 NDR 2.0 파라미터 표 + 요청 · 응답 길이 — "6 요청 52 B · 응답 24 B 가 맞나, 7 완료 응답 칸".
  2. **덤 2** — BG02 판: 6 fault → winnsi `+0x302 RpcAsyncCancelCall(7, 1)` → 같은 스레드 `NtAlpcSendWaitReceivePort(Flags 0x10000, 보내기만)` → `svc_async_unsupported` → `NtDelayExecution(2 ms)` 끝없음 (`rBG02_rv1 8724f2b346f9 seq 2136 ~ 2143`). 취소 메시지 꼴 · 서버가 답하나 · 2 ms 잠의 끝 조건.
  3. **덤 3** — BG03 판 (`038cc0421ef5 seq 90468` · `a5f097f9701d seq 253599`): 새 스레드가 이미 묶인 nsi 포트로 첫 메시지를 종류 ≠ 0 · 1 로 비동기 보냄 → `svc_lrpc_kind_unsupported` (`lrpc.cpp:280 · :328`) → 곧 프로시저 5 로 이어 감. 누가 무엇을 보내나.

## 방법

- 정적 sast/154 (요청서 `_workspace/sast/154_rpc-extras.md`, 결과서 `_workspace/sast/154_rpc-extras_result.md`, 판정 위, 스크립트 문제 없음).
  - winnsi `.rdata` (`pefile`): 프록시 정보 `0x6000` · 문법 표 `0x6150` · 프로시저 문자열 `0x70c2` · 오프셋 표 `0x7310` · 타입 문자열 `0x6eb2`.
  - rpcrt4 `.asm` (새 export 27함수 + 있던 것): `RpcAsyncCancelCall` · `CancelAsyncCall__61f30` · `AlpcSendCancelMessage` · `CancelCall__62554` · `ActualCancelCall__945e8` / `__9b800` · `AlpcCancelMessage` · `ProcessIO` · `HandleCancelMessage__33620` · `SendReservedMessage` · `SendCausalFlowListForCleanup` · `CleanupOrphanedCasaulFlowEntries` · `FindOrCreateCausalFlow__4322c` · `NegotiateTransferSyntax` (rva `0x43a20`) · `HandleReservedMessageReply` · `SignalBrokenFlow` · `IoCompleted` · `AlpcSend` · `AlpcNonPipeSend` · `InitializeRequestAndAttributes` · `NdrNonEncapsulatedUnionMarshall` 등. refs `_refs/0xe94c0.tsv` (부르는 곳 15) · `0x94d20` · `0x42e14` 등.
  - PE_emulator 쪽 (읽기만): `ext/syscall/syscall_layer_cpp/src/rpc/nsi.cpp` · `lrpc.cpp` · `alpc/nt_alpc_cleanup.cpp`.
- flow-writer 가 다시 본 것:
  - `pefile` (스크래치, 레포 밖 — `-I` 없이, MS 바이너리만 읽음): winnsi `0x7310` 9칸 · 6 머리 + 칸 (`0x7248`, 48 B) · 7 머리 + 칸 (`0x7278`, 60 B) · 타입 `0x6efe` · `0x706e` · `0x7072` · `0x6f34` · `0x6f54` · `0x707e` · 모듈 ID `0x6eee` · 공용체 `0x6eb8` · 팔 `0x6ec2`. rpcrt4 가름 종류 표 `0xe33c0 + 0x0d × 8` = `0x1800900d0` 과 그 자리 바이트 (`NdrpSimpleTypeEnum16Marshall`, `pdb_aliases.py --where rpcrt4.dll` 로 rva `0x900d0` 확인). rpcrt4 가상표 `0xe0e18` (`??_7LRPC_CCALL@@6B@` — `.asm` 주석) 의 `+0xa8` · `+0x150` 칸.
  - `.asm` 줄: 위 rpcrt4 함수의 결과서 인용 줄 전부 · winnsi `NsiRpcDeregisterChangeNotificationEx` · `NsiRpcRegisterChangeNotificationEx` · `NsiCompletePendingRpcCall` · `NsiReregisterChangeNotification` · `NsiReregisterNotificationWorker` · `RegisterNsiSvcRestartNotification` · `NsiServiceStatusChangeCallback` · `NsiWorkerThread +0xfe` · nsisvc 셋.
  - `../PE_emulator` `nsi.cpp` (120줄) `:10 ~ :16` · `:34 ~ :38` · `:75 ~ :100`, `lrpc.cpp` (382줄) `:5 ~ :8` · `:135 ~ :140` · `:269 ~ :282` · `:325 ~ :330`, `nt_alpc_cleanup.cpp:42`, 원문 380 "덤" 표 (읽기만, 고치지 않음).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- 6 (해제) · 7 (알림 기다림) 은 같은 창구에 내는 **신청서 두 장**이다. 6 은 "번호표 돌려 드려요" (번호표 20 B + 어느 동아리 (모듈 ID) 인지), 7 은 "소식 오면 알려 주세요" (번호표만).
- 동아리 이름표 (모듈 ID) 엔 "종류 칸" 이 이미 있는데, 서식 기계가 이름표 안쪽 갈림 칸을 쓸 때 **종류를 한 번 더 도장으로 찍는다** — 그래서 이름표가 4 B 길다 `[추측: 비유]`.
- 취소는 "제 신청 취소요" 쪽지를 창구에 **던지고 돌아서는** 것이다 (답 안 받음). 그다음 클라이언트는 자기 접수증 번호 (`MessageId`) 가 적힐 때까지 2 ms 마다 들여다본다 — 번호가 끝내 안 적히면 영원히 들여다본다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{winnsi,rpcrt4,nsisvc}/`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `winnsi!NsiRpcDeregisterChangeNotificationEx` | `+0x12e` | `+0x135` | `Ndr64AsyncClientCall` (프로시저 **6**, `+0x123 LEA EDX,[R8+6]`) | [정적] |
| 〃 | `+0x1db` | `+0x1e2` | `WaitForSingleObject` (7 의 이벤트, INFINITE) | [정적] |
| 〃 | `+0x1f3` | `+0x1fa` | `RpcAsyncCompleteCall` (결과 안 봄) | [정적] |
| 〃 | **`+0x302`** | **`+0x309`** | **`RpcAsyncCancelCall`** (`+0x2f9 MOV EDX,1`, 6 오류 길 `+0x1b8 JNZ → +0x2ef`) | [정적] |
| `winnsi!NsiRpcRegisterChangeNotificationEx` | `+0x54e` | `+0x555` | `Ndr64AsyncClientCall` (프로시저 **7**, `+0x543`) | [정적] |
| 〃 | `+0x8ac` | `+0x8b1` | `NsiCompletePendingRpcCall` (오류 길) | [정적] |
| `winnsi!NsiCompletePendingRpcCall` | `+0xbc` | `+0xc3` | `RpcAsyncCancelCall` | [정적] |
| 〃 | `+0x42` | `+0x49` | `WaitForSingleObject` | [정적] |
| `winnsi!NsiReregisterNotificationWorker` | `+0x30` | `+0x35` | `NsiReregisterChangeNotification` | [정적] |
| `winnsi!NsiReregisterChangeNotification` | `+0x4d` / `+0x58` | `+0x52` / `+0x5d` | `NsiStopWorkerThread` / `NsiCompletePendingRpcCall` | [정적] |
| 〃 | `+0xe8` | `+0xef` | `Ndr64AsyncClientCall` (프로시저 **5**, `+0xdd LEA EDX,[R8+5]`) | [정적] |
| `winnsi!RegisterNsiSvcRestartNotification` | `+0x5e` / `+0xd2` | `+0x65` / `+0xd9` | `RegisterWaitForSingleObjectEx` (콜백 `+0x54` = `0x180003610` `NsiReregisterNotificationWorker`) / `RtlSubscribeWnfStateChangeNotification` | [정적] |
| `rpcrt4!RpcAsyncCancelCall` | `+0x7f` | `+0x84` | ⇢ `[vtbl+0xa8]` = **`CancelAsyncCall`** (`0x61f30`) — `LRPC_CCALL` 가상표 `0xe0e18 +0xa8` | [정적] (가상표 `pefile`) |
| `rpcrt4!CancelAsyncCall` (`0x61f30`) | **`+0x8a`** | `+0x8f` | **`AlpcSendCancelMessage`** (`+0x6d ~ +0x78` 상태 > 9 · ≠ `0xc`, `+0x7d EDX = [부름+0x124]`) | [정적] |
| 〃 | `+0xab` | `+0xb0` | `CancelCall` (`0x62554`) | [정적] |
| `rpcrt4!AlpcSendCancelMessage` (`0x62690`) | **`+0x9a`** | **`+0xa1`** | **`NtAlpcSendWaitReceivePort`** (`+0x88 MOV EDX,0x10000`) | [정적] |
| `rpcrt4!CancelCall` (`0x62554`) | `+0xcd` | `+0xd2` | ⇢ `[vtbl+0x150]` = `ActualCancelCall` (`LRPC_CCALL` 가상표 `+0x150` = `0x9b800` → 꼬리 점프 `0x945e8`) | [정적] (가상표 `pefile`) |
| `rpcrt4!ActualCancelCall` (`0x945e8`) | `+0x69` | `+0x6e` | **`AlpcCancelMessage`** (`0x94724`, 갈래 `+0x4d`) | [정적] |
| 〃 | `+0x94` | `+0x99` | `SignalBrokenFlow` (`+0x73 CMP [부름+0x230],0`) | [정적] |
| 〃 | `+0xb2` | `+0xb7` | ⇢ `[vtbl+0x128](부름, 0x71a)` (부름 끝냄 `[추측: 가상 대상 이름]`) | [정적] |
| 〃 | `+0xee` | `+0xf5` | `Sleep` (`0x14` = 20 ms — 요청이 아직 안 나간 갈래) | [정적] |
| `rpcrt4!AlpcCancelMessage` (`0x94724`) | `+0x1d` | `+0x22` | `RpcpAlpcpGetHeaderSize` (`[속성] & 0xc0000000`) | [정적] |
| 〃 | **`+0xee`** | **`+0xf5`** | **`Sleep(2)`** (`+0xe9 MOV ECX,2`, 뒤 `+0xfa JMP +0x27`) | [정적] |
| 〃 | **`+0x80`** | **`+0x87`** | **`NtAlpcCancelMessage`** (`+0x7e XOR EDX,EDX`) | [정적] |
| 〃 | `+0x113` | `+0x118` | `RpcpReportFatalError(2, 상태)` → `+0x118 INT3` | [정적] |
| `rpcrt4!ProcessIO` (서버) | `+0xa0e` | `+0xa13` | `HandleCancelMessage` (`0x33620`, `+0x9fa CMP [메시지+2],0x40`) | [정적] |
| 〃 | `+0xa44` | `+0xa4b` | `NtAlpcSendWaitReceivePort` (답 — `+0xa13 MOV EAX,0x2000` 비트 검사 뒤) | [정적] |
| 〃 | `+0xc68` / `+0x8db` | `+0xc6d` / `+0x8e0` | `HandleReservedMessageRequest` (`0x95590`) | [정적] |
| `rpcrt4!NegotiateTransferSyntax` (`0x43a20`) | **`+0x49c`** | `+0x4a1` | **`FindOrCreateCausalFlow`** (`0x4322c`, `+0x47c TEST [부름+0x48],0x2000`) | [정적] |
| `rpcrt4!FindOrCreateCausalFlow` | `+0x1f` | `+0x24` | `RpcpGetThreadPointer` | [정적] |
| 〃 | **`+0xec`** | `+0xf1` | **`CleanupOrphanedCasaulFlowEntries`** (`0x96210`, 새 흐름 `+0x8a` 0x90 B 뒤) | [정적] |
| `rpcrt4!CleanupOrphanedCasaulFlowEntries` | **`+0xa9`** | `+0xae` | **`SendCausalFlowListForCleanup`** (`0x42e14`) | [정적] |
| `rpcrt4!SendCausalFlowListForCleanup` | **`+0x62`** | `+0x67` | **`SendReservedMessage`** (`0x94d20`, `+0x56 CMP [연결+0x128],0xc`) | [정적] |
| `rpcrt4!SendReservedMessage` | (줄) `+0x2d1` · `+0x31c` · `+0x360` | — | `[버퍼+0x28] = 5` · `XOR EDX,EDX` (Flags 0) · `[연결+0xd0] &= ~0x40` | [정적] |
| `rpcrt4!HandleReservedMessageReply` | `+0x46` / `+0x57` | `+0x4b` / `+0x5c` | `SetReservedMessageAvailable` / `SendReservedMessage` | [정적] |
| `rpcrt4!SignalBrokenFlow` | `+0x8f` | `+0x94` | `SendReservedMessage` | [정적] |
| `rpcrt4!IoCompleted` | `+0x394` | `+0x399` | `HandleReservedMessageReply` | [정적] |
| `rpcrt4!NdrNonEncapsulatedUnionMarshall` (`0x15440`) | `+0x20` | `+0x25` | `NdrpComputeConformance` (가름값 구하기) | [정적] |
| 〃 | **`+0x65`** | `+0x6a` | ⇢ `[0xe33c0 + 종류 × 8]` — 종류 `0x0d` (ENUM16) 칸 = **`NdrpSimpleTypeEnum16Marshall`** (`0x900d0`) | [정적] (표 `pefile`) |
| `nsisvc!RpcNsiDeregisterChangeNotification` | `+0x207` | `+0x20c` | `NsipUnsubscribeAndCompleteAsyncCall` (`+0x1fb MOV EDX,0x4c7`) | [정적] |
| `nsisvc!NsipUnsubscribeAndCompleteAsyncCall` | `+0x6c` | `+0x73` | `RpcAsyncCompleteCall(상태, &[RSP+0x38])` (`+0x5 MOV [RSP+0x10],EDX` = 같은 칸) | [정적] |
| `nsisvc!RpcNsiRequestChangeNotification` | `+0xdf` | `+0xe6` | `RpcServerSubscribeForNotification` (`+0xd6 EDX = 1` · `+0xdb R8D = 5` — 클라이언트 끊김만) | [정적] |

- 별칭: 이 표의 rpcrt4 이름들은 `_calls.tsv` 가 `alias:…+1 · ALIASES(1)` 로 적는 것 (PDB 이름 하나 — 무리 아님). `NdrpSimpleTypeEnum16Marshall` 은 `pdb_aliases.py --lookup rpcrt4.dll 0x900d0` → "별칭 무리 아님".

### 1. winnsi 프로시저 6 · 7 (덤 1) [정적]

#### 1-1. 머리 (NDR 2.0 Oif) — 5 · 6 · 7

| 칸 | 5 (비교용, `0x7212`) | **6 (`0x7248`)** | **7 (`0x7278`)** |
|---|---|---|---|
| 앞 두 바이트 · 프로시저 번호 | `00 48` · 5 | `00 48` · 6 | `00 48` · 7 |
| 스택 크기 | `0x30` | `0x28` | `0x38` |
| 핸들 | `FC_BIND_PRIMITIVE`, 스택 자리 8 (자리 0 = 비동기 핸들) | 같음 | 같음 |
| 클라이언트 · 서버 상수 | `0x18` · `0x40` | `0x38` · `0x40` | `0x24` · `0x22` |
| Oi2 깃발 · 파라미터 수 | `0xc6` · 4 | `0xc6` · 3 | `0xc5` · 5 |
| 확장 머리 10 B | `0a 85 00 00 01 …` | 같음 | `0a 03 01 00 00 …` |

- Oi2 비트 이름 `[추측: 이름]` — 비트 7 (비동기 핸들) 이 셋 다 켜짐 = winnsi 가 `Ndr64AsyncClientCall` 로 부르는 것과 맞다 (run137 4절).
- 상수 (`0x38` · `0x18`) 로 선 위 길이를 정하지 않았다 — 5 의 `0x18` 은 44 B 와도, 40 B 와도 같지 않다 (run137 3-2 는 "`0x18` 과 앞 12 + 8 = 20 이 맞아떨어짐" 을 근거 하나로 삼았는데, 이번에 그 셈이 다른 판단을 받는다 — 아래 "앞 run 고쳐 읽기").

#### 1-2. 파라미터 표 (칸 6 B: attr 2 · 스택 2 · 타입 오프셋 또는 기본형 2)

attr 비트 읽는 법 `[추측: 이름]`: `0x0001` 크기 · `0x0002` 해제 · `0x0008` 입력 · `0x0010` 출력 · `0x0020` 반환 · `0x0040` 기본형 · `0x0080` 값 · `0x0100` 단순 참조 · `0x2000 × n` 서버 할당 n × 8 B. "타입 `+0x..`" 는 타입 문자열 `0x6eb2` 에서의 거리.

**프로시저 6** (`0x7260 ~ 0x7277`, 파라미터 3):

| # | attr | 스택 | 타입 → 바이트 | 뜻 `[추측: 이름 대응]` |
|---|---|---|---|---|
| 0 | `0x010b` | `0x10` | `+0x4c` (`0x6efe`) `1a 03 18 00 00 00 08 00 08 40 36 08 40 5b` = `FC_BOGUS_STRUCT` 24 B: LONG · 패딩 · POINTER (`FC_UP` → `+0x3c` 모듈 ID) · LONG · 패딩 | {예약, 모듈 ID 포인터, 객체 번호} — **5 의 첫 파라미터와 같은 타입** |
| 1 | `0x0118` | `0x18` | `+0x1bc` (`0x706e`) `30 e0 00 00` = `FC_BIND_CONTEXT` 깃발 `0xe0` (포인터 경유 · 입력 · 출력) | 컨텍스트 핸들 (입력 · 출력) 20 B |
| 2 | `0x0070` | `0x20` | 기본형 `08` (LONG) | 반환 |

**프로시저 7** (`0x7292 ~ 0x72b3`, 파라미터 5):

| # | attr | 스택 | 타입 → 바이트 | 뜻 |
|---|---|---|---|---|
| 0 | `0x0008` | `0x10` | `+0x1c0` (`0x7072`) `30 41 00 00` = `FC_BIND_CONTEXT` 깃발 `0x41` (입력 · null 불가, **포인터 경유 비트 없음 = 값**) | 컨텍스트 핸들 (입력) 20 B — 요청의 전부 |
| 1 | `0x4113` | `0x18` | `+0x82` (`0x6f34`) `1a 03 10 00 00 00 06 00 36 08 40 5b` = 16 B: POINTER · LONG · 패딩. 포인터 → `+0x76` 바이트 배열 (개수 = 구조체 `+8` LONG) | 출력 A {바이트 배열 포인터, 길이} |
| 2 | `0x6113` | `0x20` | `+0xa2` (`0x6f54`) `1a 03 18 00 00 00 08 00 0d 40 36 08 08 5b` = 24 B: ENUM16 · 패딩 · POINTER · LONG · LONG. 포인터 → `+0x96` 바이트 배열 (개수 칸 `19 00 1c 00` — 구조체 밖 `0x1c`, `[공백]`) | 출력 B {enum16, 바이트 배열 포인터, LONG, LONG} |
| 3 | `0x2010` | `0x28` | `+0x1cc` (`0x707e`) `11 0c 0d 5c` = `FC_RP` → `FC_ENUM16` | 출력 enum16 (선 위 2 B) |
| 4 | `0x0070` | `0x30` | 기본형 `08` | 반환 |

- 끝 자리가 다음 프로시저 시작과 맞는다 (6 끝 `0x7277` + 1 = 7 시작 `0x7278`, 7 끝 `0x72b3` + 1 = 8 시작 `0x72b4`).

#### 1-3. 모듈 ID 의 선 위 꼴 — 길이를 가른 증거

| 자리 | 바이트 | 뜻 `[정적]` (읽는 법은 공개 NDR 이름 대응 `[추측: 이름]`) |
|---|---|---|
| `0x6eee` (타입 `+0x3c`) | `1a 03 18 00 00 00 00 00 06 3e 0d 4c 00 bd ff 5b` | 24 B 구조체: SHORT (`Length`) · 메모리 패딩 2 · ENUM16 (`Type`) · `FC_EMBEDDED_COMPLEX` → `0x6eb8` |
| `0x6eb8` | `2b 0d 06 00 fc ff 01 00 02 00` | `FC_NON_ENCAPSULATED_UNION`, 가름 종류 ENUM16, 가름값 = 구조체 안 4 B 앞 (`fc ff`) 의 `Type`, 팔 서술 `+2` → `0x6ec2` |
| `0x6ec2` | `10 00 02 00 · 01 00 00 00 10 00 · 02 00 00 00 16 00 · 00 00` | 메모리 16 B, 팔 2개: 값 1 → `0x6eda` (16 B — GUID), 값 2 → `0x6ee6` (8 B — LUID `[추측: 이름]`) |

- **가름값을 버퍼에 쓰는 줄** `[정적]`: `NdrNonEncapsulatedUnionMarshall +0x20 NdrpComputeConformance` (가름값) → `+0x5d MOV RAX,[0xe33c0 + 종류 × 8]` · **`+0x65` call**. 종류 `0x0d` 칸 = `0x1800900d0` (`pefile`) = `NdrpSimpleTypeEnum16Marshall` — 바이트를 손으로 읽으면: 값 범위 검사 (`f7 02 00 80 ff ff`) → 버퍼 2 정렬 (`48 ff c0 48 83 e0 fe`) → **2 B 쓰기 · 버퍼 포인터 +2** (`0f b7 02 66 89 01 49 83 40 08 02`). 그 뒤 `+0x6a ~ +0xa3` 팔 정렬, `+0xb7 ~ +0xca` 가름값과 맞는 팔 찾기.
- 그래서 선 위 모듈 ID = `Length` 2 (+0) · `Type` 2 (+2) · **가름값 2 (+4)** · 패딩 2 (+6) · GUID 16 (+8) → **24 B** (LUID 면 16 B). 팔 정렬 니블은 0 (`02 00` 의 위 4비트) 이고 +8 로 가는 4 정렬은 GUID 구조체 (`15 03 …`, 정렬 3) 쪽에서 온다고 읽었다 `[추측: 팔 구조체 함수 안 읽음]`.
- **사슬은 `[추측]`**: 복잡 구조체 마샬러가 `FC_EMBEDDED_COMPLEX` 공용체를 이 함수로 넘기는 호출 줄은 안 따라갔다 (결과서 1-5, 다음 요청 후보 3). 공개 NDR 에서도 비캡슐 공용체는 "가름값 + 팔" 로 선 위에 나간다고 알려져 있다 `[추측: 기억]`. → 진짜 길이는 VM (아래 "아직 확인 필요" 동적 1).

#### 1-4. 스텁 길이 표 (NDR 2.0, 4 정렬)

| 프로시저 · 방향 | 길이 | 칸 차례 (스텁 처음 기준) |
|---|---|---|
| 5 요청 (비교용) | **44 B** (GUID) / 36 (LUID) — run137 3-2 의 40 B 를 고쳐 읽기 | `+0x00` 예약 · `+0x04` 포인터 ID (≠ 0) · `+0x08` 객체 번호 · **`+0x0c` 모듈 ID 24 B** · `+0x24` compartment 8 |
| **6 요청** | **56 B** (GUID) / 48 (LUID) — "52 B" 아님 | `+0x00 ~ +0x0b` 5 와 같은 12 B · **`+0x0c` 모듈 ID 24 B** · `+0x24` 컨텍스트 핸들 20 B |
| 6 요청, 모듈 ID 포인터 NULL | 32 B | 12 B (포인터 ID 0) · `+0x0c` 핸들 20 B |
| **6 응답** | **24 B — 맞다** | `+0x00` 핸들 20 B (서버가 지우면 0) · `+0x14` 반환 |
| **7 요청** | **20 B** | 핸들 20 B 하나 |
| **7 응답, 변화 없이 끝냄** (반환 `0x4c7`, 출력 0, 포인터 NULL) | **32 B** | `+0x00` A 포인터 ID 0 · `+0x04` A 길이 0 · `+0x08` B enum16 0 · `+0x0a` 패딩 · `+0x0c` B 포인터 ID 0 · `+0x10` B LONG 0 · `+0x14` B LONG 0 · `+0x18` 출력 enum16 0 · `+0x1a` 패딩 · **`+0x1c` 반환 `c7 04 00 00`** |
| 7 응답, 배열 포인터 ≠ NULL | 32 B + 구조체 뒤마다 배열 (conf + 바이트, 4 정렬) | A 는 `+0x04` 길이, B 개수 칸은 `[공백]` |

- "출력 0 · 포인터 NULL" 은 서버 스텁이 `[out]` 참조 구조체를 0 으로 채워 돌려준다고 본 것 `[추측: 스텁 초기화]`.
- 6 요청의 끝 20 B 가 문맥이라는 것은 세 경우 (56 / 48 / 32) 모두 같다.
- run129 3절 표의 7 줄 ("16 B 구조체 · 24 B 모듈 ID 꼴 · DWORD · 반환 DWORD") 은 **"16 B 구조체 A · 24 B 구조체 B (바이트 배열 포인터를 담음 — 모듈 ID 꼴 아님) · enum16 · 반환"** 으로 고쳐 읽는다 (아래).

### 2. 취소 메시지 (덤 2) [정적]

#### 2-1. 사슬

| 단계 | 함수 (rpcrt4 rva) | 줄 · 하는 일 |
|---|---|---|
| 0 | winnsi `NsiRpcDeregisterChangeNotificationEx` | 6 이 오류로 돌아오고 (`+0x1b8 JNZ → +0x2ef`) 걸린 7 이 있으면 (`+0x1a9 CMP byte [RDI+0x80],1`) `+0x302 RpcAsyncCancelCall(7 의 상태, 1)` → 그 7 의 이벤트를 `+0x1db WaitForSingleObject(INFINITE)` |
| 1 | `RpcAsyncCancelCall` (`0x61e40`) | `+0x14` 상태 검사 · `+0x3e` 부름 객체 `[상태+0x20]` · `+0x4f InvalidHandle` · **`+0x7f` ⇢ `[vtbl+0xa8]`** · `+0x90` ⇢ `[vtbl+0x20]` |
| 2 | `LRPC_BASE_CCALL::CancelAsyncCall` (`0x61f30`) | `+0x38` 락 · `+0x44` 이미 취소 중이면 끝 · **`+0x65 [부름+0x120] |= 4`** · `+0x6d ~ +0x78` 상태 `[+0x108]` > 9 · ≠ `0xc` 이면 **`+0x8a AlpcSendCancelMessage(연결, [+0x124] 호출 ID)`** · `+0x9e` 둘째 인자 ≠ 0 이면 `+0xab CancelCall` |
| 3 | `LRPC_CASSOCIATION::AlpcSendCancelMessage` (`0x62690`) | 아래 2-2 |
| 4 | `LRPC_BASE_CCALL::CancelCall` (`0x62554`) | `[+0x120] |= 1` (한 번만) · `+0xcd` ⇢ `[vtbl+0x150]` `ActualCancelCall` |
| 5 | `ActualCancelCall` (`0x9b800` → `0x945e8`) | 상태에 따라 (2-4). 10 이고 `[+0x120] & 0x80` 꺼짐 / 9 이고 속성 Valid ≠ 0 → `+0x4d` 갈래 **`+0x69 AlpcCancelMessage`** |
| 6 | `LRPC_CASSOCIATION::AlpcCancelMessage` (`0x94724`) | 2 ms 잠 (2-3) → **`+0x80 NtAlpcCancelMessage(포트, 0, &사본)`** |

#### 2-2. 취소 메시지 0x40 B (`AlpcSendCancelMessage`)

| 칸 | 값 | 줄 |
|---|---|---|
| 전체 | 0 으로 지움 | `+0x1c ~ +0x26` |
| `+0x00` DataLength · `+0x02` TotalLength | `0x18` · `0x40` | `+0x4f MOV [RSP+0x40],0x400018` |
| `+0x04` Type | `0x4000` | `+0x3c` · `+0x4a` |
| **`+0x28` LRPC 종류** | **4** | `+0x57 MOV qword [RSP+0x68],4` |
| `+0x30` · `+0x38` | 0 | `+0x41` 등 |
| **`+0x34` 호출 ID** | `[부름+0x124]` (요청 `+0x34` 와 같은 값, run133 2-1) | `+0x60 MOV [RSP+0x74],EBX` ← `CancelAsyncCall +0x7d` |
| **보내기** | `NtAlpcSendWaitReceivePort([연결+0x88], **0x10000**, &메시지, NULL, NULL, NULL, NULL, NULL)` | `+0x93` · **`+0x88`** · `+0x74` · `+0x7f` · `+0x6e ~ +0x8d` · **`+0x9a`** |
| 반환 | 안 본다 | `+0xa1 ~` 함수 끝 |

- `0x10000` 의 공개 이름은 `[추측: ALPC_MSGFLG_RELEASE_MESSAGE]` — 코드엔 상수만.
- 다른 `0x10000` (`DropMsgAttributes +0x98`) 은 DataView 속성 안 깃발이고 보내기 Flags 는 0 — 이 덤과 무관 (결과서).

#### 2-3. 서버가 답하나 · 2 ms 잠의 끝 조건

- **서버** (`ProcessIO`): ALPC 종류 3 (`[추측: 이름 LPC_DATAGRAM]`) 갈래 `+0x858 ~ +0x866` → `+0x9d9` → `+0x9fa CMP [메시지+2],0x40` (TotalLength ≥ 0x40) → **`+0xa0e HandleCancelMessage(연결, [메시지+0x34])`**. 이 길은 `+0x28` 을 안 읽는다. 그 뒤 `+0xa13 ~ +0xa1f` 에서 `Type & 0x2000` 일 때만 `+0xa44` 답 → 보내기만 한 취소는 이 비트가 꺼져 있을 것이라 **답 없음** `[추측: 비트 뜻]`.
- `HandleCancelMessage` (`0x33620`) 는 호출 ID 가 맞는 부름에 취소 깃발 + `IssueCallStatusChangeNotification` — 취소 알림을 구독한 부름에만 일이 생긴다. nsisvc 구독은 클라이언트 끊김뿐 (`RpcNsiRequestChangeNotification +0xd6 / +0xdb`) → **걸린 7 은 서버에서 안 끝난다.** 끝내는 쪽은 클라이언트 자신 (2-4).
- **클라이언트 잠** (`AlpcCancelMessage`):
  1. `+0x15 ~ +0x24`: 인자 속성 칸 첫 dword `& 0xc0000000` → `+0x1d RpcpAlpcpGetHeaderSize` → Context 속성 위치 = 칸 + 헤더 크기 (Security · DataView 가 있으면 그만큼 뒤).
  2. `+0x27 MOV EDX,[Context+0x14]` (`MessageId`) ≠ 0 → `+0x32` 진행.
  3. 0 이면 `+0xdf CMP [Context+0x18],0` (`CallbackId`) ≠ 0 → `+0xe3 JNZ +0x32` 진행.
  4. 둘 다 0 → **`+0xe9 MOV ECX,2` · `+0xee Sleep(2)` · `+0xfa JMP +0x27`**. 제한 · 탈출 없음.
  5. 진행: 사본 `{[+0], [+8], [+0x10], MessageId, [+0x18]}` 을 `RSP+0x40 ~` 에 (`+0x3c ~ +0x5d`) → `+0x80 NtAlpcCancelMessage`.
- **그 칸을 누가 채우나**: `호출+0x128` 은 비동기 요청 때 보내기 속성으로 넘어간 칸 — `InitializeRequestAndAttributes +0x1b LEA RSI,[RCX+0x128]` · `AlpcNonPipeSend +0x16 LEA R8,[RCX+0x128]` → `AlpcSend +0x10c MOV R9,R14` → `AlpcSend +0x128 NtAlpcSendWaitReceivePort` `[정적]`. 커널이 그 syscall 안에서 Context 속성의 `MessageId` 등을 써 준다는 것은 이 루프와 맞는 짐작 `[추측]` (커널 `AlpcpSendMessage` 안 읽음 — 다음 요청 후보 1).
- 2 ms 잠 자리 둘 (헷갈리지 않게): **`AlpcCancelMessage +0xee`** (이 덤) · `CancelAndWaitToDisappear +0x35` (상태 1 · 3 · 5 쪽, 결과서). `ActualCancelCall +0xee Sleep(0x14)` 는 20 ms (요청이 아직 안 나간 갈래).

#### 2-4. 상태 `[부름+0x108]` 에 따른 길 (상태 이름 `[공백]`)

| 상태 | 종류 4 보냄? (`CancelAsyncCall +0x73 / +0x78`) | `ActualCancelCall` |
|---|---|---|
| ≤ 8 | 안 보냄 | 1 · 3 → IO 취소, 5 → `Cancel`, 7 → `CancelPrepareIteration` (결과서) |
| 9 | 안 보냄 | `+0x4d` 갈래: 속성 Valid ≠ 0 → `AlpcCancelMessage`, 0 → `Sleep(20)` 돌며 기다림 |
| **10** | **보냄** | `[+0x120] & 0x80` 꺼짐 → `+0x4d` 갈래 = **`AlpcCancelMessage` (2 ms 잠 포함)** |
| 11 | 보냄 | 아무것도 안 함 (`+0x3f CMP ECX,1`) |
| 12 | 안 보냄 (`CMP 0xc; JZ`) | 아무것도 안 함 |

- BG02 (취소 메시지가 나가고 2 ms 잠이 이어짐) 와 맞으려면 그때 7 의 상태는 **10** 이어야 한다 `[추측: 로그 대조 — 로그 원본 없음]`.
- `NtAlpcCancelMessage` 반환 (`+0xa6 ~ +0x11e`): `0` → 1 · `0xc0000702` / `0xc0000703` → 3 · `0x4000002e` → 2 · `0x103` → 0 · 그 밖 → `+0x113 RpcpReportFatalError(2, 상태)` · `+0x118 INT3`.
- 반환 1 이면 `ActualCancelCall` 이 (연결 `[+0x230]` 있고 상태 10 이면 `+0x94 SignalBrokenFlow` = 종류 5, 3절) → `+0xb2` ⇢ `[vtbl+0x128](부름, 0x71a)` 로 **부름을 `0x71a` 로 그 자리에서 끝냄** `[추측: 가상 대상 이름 · 0x71a = RPC_S_CALL_CANCELLED]` → winnsi 의 기다림이 풀린다. 반환 2 도 같은 자리, 0 · 3 은 끝내지 않음 (서버를 기다림 — nsisvc 는 안 끝냄).

### 3. nsi 포트의 종류 0 · 1 밖 메시지 (덤 3) [정적]

#### 3-1. 클라이언트가 먼저 보내는 종류 (`_refs/0xe94c0.tsv` 15곳 중 클라이언트 쪽)

| 종류 `+0x28` | 만드는 줄 | Flags | 크기 · 칸 | 서버 | 클라이언트가 기다리나 |
|---|---|---|---|---|---|
| 0 요청 | `InitializeRequestAndAttributes +0x1ab` (run133) | 0 / `0xc00000` | 스텁 + 0x68 | 답 (종류 3) | 동기는 기다림, 비동기는 완료 통지 |
| 1 bind | `Bind__4e330` (run133) | 0 | 0x70 | 답 | 비동기 (완료 통지) |
| **4 취소** | **`AlpcSendCancelMessage +0x57`** | **`0x10000`** | **0x40** (`+0x34` 호출 ID) | **답 안 함** | 안 기다림 (부른 winnsi 는 7 의 이벤트를 INFINITE 로 기다림 — 끝내는 건 클라이언트 자신, 2-4) |
| **5 예약 메시지 묶음** | **`SendReservedMessage +0x2d1`** | **0** (`+0x31c`), 보내기 속성 = `[연결+0x150] + 0x100` | 최대 0x100 B: `+0x00` DataLength = `0xd8 − 남은 칸` · `+0x02` TotalLength = `0x100 − 남은 칸` · `+0x04` Type `0x4000` · `+0x18` MessageId = `[연결+0x148]` · `+0x28` 5 · `+0x30 ~` 항목 (하나 0x10 B) | **답** (`ProcessIO +0xc68` / `+0x8db` `HandleReservedMessageRequest`) | 안 기다림. 답은 `IoCompleted +0x394 HandleReservedMessageReply` 가 받는다 |
| 2 · 3 · 6 · 9 · 10 · 11 | 서버 쪽 (run133 · 결과서 3-1) | — | — | — | 클라이언트가 먼저 보내는 종류 아님 |

- 5 를 보낸 뒤 `+0x360 [연결+0xd0] &= ~0x40` · 다음 5 는 `+0x2b TEST [연결+0xd0],0x40; JZ` 로 건너뜀 — 비트를 다시 켜는 곳은 `HandleReservedMessageReply +0x46 SetReservedMessageAvailable`. 그래서 답이 안 와도 **다음 5 를 안 만들 뿐 막히지 않는다** `[추측: 그 밖 쓰는 곳 안 봄]`.
- `SendReservedMessage` 를 부르는 곳 (`_refs/0x94d20.tsv`) 넷: `SendCausalFlowListForCleanup +0x62` · `HandleReservedMessageReply +0x57` · `SignalBrokenFlow +0x8f` (취소 성공 뒤) · `SignalBrokenPipe +0x5d` (파이프, 이 길 아님). `SendCausalFlowListForCleanup` 를 부르는 곳 (`_refs/0x42e14.tsv`) 여섯: `CleanupOrphanedCasaulFlowEntries +0xa9` · `Unbind +0xe5` · `LRPC_BINDING_HANDLE` 쪽 `+0x1d1` · `SetAuthInformation +0x1bf` · `LrpcCreateBindingHandle +0x1f7` · `ResetBindState +0xae`.

#### 3-2. 새 스레드의 첫 호출 앞 — 사슬 A (종류 5)

1. 부름 → `Ndr64AsyncClientCall` → `Ndr64pClientSetupTransferSyntax` → `I_RpcNegotiateTransferSyntax` (run137 4절) ⇢ 바인딩 `NegotiateTransferSyntax` (`0x43a20`). 마지막 가상 호출 칸은 안 따라감 `[추측]`.
2. `NegotiateTransferSyntax +0x47c` 부름 깃발 `[+0x48] & 0x2000` 꺼짐 · `+0x485 ~ +0x493` 조건 → **`+0x49c FindOrCreateCausalFlow`**.
3. `FindOrCreateCausalFlow`: `+0x1f RpcpGetThreadPointer` → 이 스레드 흐름 목록에서 이 바인딩 것을 찾음. 있으면 `+0x69` 그대로. **없으면** (= 이 스레드가 이 바인딩에서 처음) `+0x8a` 새 흐름 0x90 B → 바인딩 락 안에서 **`+0xec CleanupOrphanedCasaulFlowEntries`**.
4. `CleanupOrphanedCasaulFlowEntries`: 바인딩 흐름 목록 `[+0x1e8]` 에서 상태 `[항목−8] == 1` (스레드가 끝나 버려진 것 `[추측: 뜻]`) 을 떼고, 협상 수준 `[+0x114] ≥ 5` 면 **`+0xa9 SendCausalFlowListForCleanup`** (아니면 `+0xcf` 쪽에서 그냥 놓음).
5. `SendCausalFlowListForCleanup`: 항목을 연결 `[+0x130]` 목록에 붙이며 `[+0x128]` 을 올리고, **`+0x56 [연결+0x128] ≥ 12` 면 `+0x62 SendReservedMessage`**.
6. 그 뒤 같은 스레드의 요청 (종류 0) 이 나간다.

→ 새 스레드의 첫 호출 앞에 종류 5 가 끼는 것은 **"이 바인딩에서 끝난 스레드의 흐름이 12개 이상 쌓인 뒤 처음 온 새 스레드"** 일 때다.

#### 3-3. 사슬 B (종류 4) — winnsi WNF 재등록 길

- `RegisterNsiSvcRestartNotification +0xd2 RtlSubscribeWnfStateChangeNotification` · `+0x5e RegisterWaitForSingleObjectEx(…, 콜백 0x180003610)` → (WNF) `NsiServiceStatusChangeCallback +0x12 SetEvent` → 풀 일꾼 **`NsiReregisterNotificationWorker +0x30 NsiReregisterChangeNotification`** → `+0x4d NsiStopWorkerThread` · **`+0x58 NsiCompletePendingRpcCall`** (걸린 7 이 있으면 `+0xbc RpcAsyncCancelCall` → 2절 · `+0x42 WaitForSingleObject`) → 돌아와 **`NsiReregisterChangeNotification +0xe8` 프로시저 5**.
- 같은 취소가 `NsiRpcRegisterChangeNotificationEx +0x8ac` (→ `NsiCompletePendingRpcCall`) · `NsiRpcDeregisterChangeNotificationEx +0x302` 오류 길에서도 나간다.
- `NsiWorkerThread` (알림이 와서 7 이 끝났을 때) 는 `+0xfe` 에서 프로시저 7 요청 (종류 0) 을 낸다 — 종류 ≠ 0 아님.

#### 3-4. 어느 쪽이 원문 표본인가 (PE_emulator 이유 문자열로 가름)

- 종류 4 는 Flags `0x10000` → `lrpc.cpp:137 ~ :139` `lrpc_shape_error` 에서 `0x20000` (`synchronous`, `:7`) 비트가 없어 **`svc_async_unsupported`** (BG02 때 본 그 이유). `:280` · `:328` 에 못 온다.
- `svc_lrpc_kind_unsupported` 에 오려면 형태 검사를 통과해야 한다 — **Flags 0 · 보내기만 → `:271 ~ :273 service_send_async` → `:328`** = 사슬 A 의 **종류 5**.
- 그래서 **원문 두 표본은 종류 5 쪽**, 사슬 B (종류 4) 는 BG02 때의 것 `[추측: 표본 로그 지워짐 — 이유 문자열과 코드 갈래로만 가름]`.

### 호출 관계

```
winnsi!NsiRpcDeregisterChangeNotificationEx                                        [정적]
 ├─ +0x135  Ndr64AsyncClientCall   프로시저 6   요청 56 B (GUID) · 응답 24 B
 │           (오류 → +0x1b8 → +0x2ef, 걸린 7 있음 +0x1a9)
 ├─ +0x309  rpcrt4!RpcAsyncCancelCall(7, 1)
 │           ├─ +0x84 ⇢ [vtbl+0xa8] CancelAsyncCall   (+0x65 [+0x120]|=4, 상태 > 9 · ≠ 0xc)
 │           │    ├─ +0x8f  AlpcSendCancelMessage
 │           │    │          └─ +0xa1  NtAlpcSendWaitReceivePort   Flags 0x10000 · 0x40 B · +0x28 = 4 · 보내기만
 │           │    │                     ⇒ (서버) ProcessIO +0xa13 HandleCancelMessage   답 없음
 │           │    └─ +0xb0  CancelCall +0xd2 ⇢ [vtbl+0x150] ActualCancelCall
 │           │               ├─ +0x6e  AlpcCancelMessage
 │           │               │          ├─ +0xf5  Sleep(2)   Context MessageId · CallbackId 둘 다 0 인 동안 (끝없음)
 │           │               │          └─ +0x87  NtAlpcCancelMessage   0 · 0x702 · 0x703 · 0x4000002e · 0x103 밖 = 치명
 │           │               ├─ +0x99  SignalBrokenFlow → SendReservedMessage   (반환 1 · 상태 10 · 연결 있음)
 │           │               └─ +0xb7 ⇢ [vtbl+0x128](부름, 0x71a)
 │           └─ +0x95 ⇢ [vtbl+0x20]
 └─ +0x1e2  WaitForSingleObject  INFINITE   (7 의 이벤트)
rpcrt4!NegotiateTransferSyntax (0x43a20)   (부름마다, +0x47c ~ +0x493 조건)
 └─ +0x4a1  FindOrCreateCausalFlow   이 스레드가 이 바인딩에서 처음이면
             └─ +0xf1  CleanupOrphanedCasaulFlowEntries   ([+0x114] ≥ 5)
                        └─ +0xae  SendCausalFlowListForCleanup   ([연결+0x128] ≥ 12)
                                   └─ +0x67  SendReservedMessage   +0x28 = 5 · Flags 0 · 보내기만
                                              ⇒ (서버) ProcessIO +0xc6d HandleReservedMessageRequest   답 있음
nsisvc!RpcNsiDeregisterChangeNotification +0x20c NsipUnsubscribeAndCompleteAsyncCall
 └─ +0x73  RpcAsyncCompleteCall(걸린 7, &0x4c7)   일반 응답 32 B (fault 아님)
```

- (나무 오프셋은 ret 자리, 괄호 안 `+0x..` 는 그 함수 안 줄. `⇢` 는 가상 · 간접 호출, `⇒` 는 ALPC 로 다른 프로세스. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (원문 "받을 꼴" = 덤마다 한 줄 + 결과서 "지금 맞나 · 틀리나")

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/rpc/nsi.cpp` (120줄) · `lrpc.cpp` (382줄) 의 결과서 인용 줄과 `alpc/nt_alpc_cleanup.cpp:42` 를 읽었다 (읽기만). **결과서의 줄 번호는 모두 맞다.** 고칠지는 사용자 · PE_emulator 결정.

**덤마다 한 줄**:

- **덤 1**: `[정적]` 6 요청 56 B (GUID) / 48 (LUID) / 32 (포인터 NULL) — 52 B 아님, 6 응답 24 B 맞음, 7 요청 20 B, **7 완료 = 일반 응답 (종류 3) 32 B · 끝 4 B `0x000004c7`** (fault 아님). 모듈 ID 24 B 의 사슬은 `[추측]` → 진짜 길이는 VM.
- **덤 2**: `[정적]` 취소 = **종류 4 · 0x40 B · Flags `0x10000` · 보내기만, 서버 답 없음**. 2 ms 잠은 요청 때 넘긴 Context 속성의 `MessageId` · `CallbackId` 가 0 아니게 될 때만 끝남 — 비동기 요청을 받을 때 그 칸을 채워야 한다.
- **덤 3**: `[정적]` 클라이언트가 먼저 보내는 종류 0 · 1 밖 = 4 · 5 뿐, 원문 표본 (`:328`) 은 **종류 5 (예약 메시지 묶음, Flags 0)** — 거절해도 클라이언트는 안 막힘 `[추측]`. 로그에 Flags · `+0x28` · `+0x18` 을 남기면 끝.

**`nsi.cpp` 판정**:

| 자리 | 지금 | 판정 |
|---|---|---|
| `:14` `deregister_min_size = 12 + context_size` (= 32) · `:77` 검사 | 모듈 ID 본문을 안 셈 | **하한으로는 맞음** (포인터 NULL 이면 진짜도 32). 진짜는 보통 56 (GUID) / 48 (LUID) |
| `:79` `nsi_context_key(stub, size − 20)` | 끝 20 B 를 문맥으로 | **맞음** — 세 길이 모두 끝 20 B 가 핸들 |
| `:36` 5 `size() < 40` | 40 B 하한 | 진짜 44 B 로 통과하니 **영향 없음** (LUID 36 B 면 걸림 — 하한은 36 이 맞다) |
| `:10` `register_reply_size = context_size + 4` (6 응답 24 B) | 24 B | **맞음** |
| `:98` `TODO(VM)` "… 우리는 끝 20 B 문맥 · 24 B 0 · fault" (7 완료) | 걸린 7 을 fault 로 끝냄 | **틀림** — 진짜는 **일반 응답 (종류 3), 스텁 32 B, 끝 4 B `0x000004c7`** (1-4). 지금은 클라이언트가 RPC 오류 `0x4c7` 을, 진짜는 오류 0 + 반환값 `0x4c7` 을 받는다. 6 길 (`NsiRpcDeregisterChangeNotificationEx +0x1f3`) 은 결과를 안 봐서 결과가 같다. `TODO(VM)` 에 쓸 글 (결과서): "`[정적]` sast/154: 6 요청 56 B (모듈 ID 24 B = Length · Type · 가름값 · 패딩 · GUID — 공용체 가름값이 한 번 더 나감), 응답 24 B; 7 요청 20 B, 완료 응답 32 B (반환 0x4c7). 진짜 길이는 VM." |

**`lrpc.cpp` 판정**:

| 자리 | 지금 | 판정 |
|---|---|---|
| `:137 ~ :139` `lrpc_shape_error` (`:7 synchronous = 0x20000`) | Flags `0x10000` (비트 `0x20000` 없음) → `:139` **`svc_async_unsupported`** | **빠짐** — 종류 4 · Flags `0x10000` · 속성 NULL · 받기 NULL 이면 **상태 0, 답 없음, 큐에 아무것도 안 넣음** 이 진짜와 같다. `+0x34` 호출 ID 로 걸린 호출에 취소를 알릴지는 우리 고름 (nsisvc 는 안 끝냄) |
| 비동기 요청 받기 (`service_send_async` · `service_read_message_context` — 결과서) | Context 속성의 `MessageContext` (`+8`) 만 읽음 | **빠짐** — 보낸 쪽 Context 속성의 **`MessageId` (`+0x14`) · `Sequence` (`+0x10`) 를 0 아닌 값으로 써 줘야** `AlpcCancelMessage` 의 2 ms 잠이 안 돈다. 안 쓰면 BG02 의 `NtDelayExecution(2 ms)` 끝없음과 같은 꼴 `[추측: 로그 원본 없음]` |
| `NtAlpcCancelMessage` (`nt_alpc_cleanup.cpp:42` — `message_id == 0` → `0xc0000109`) | `0xc0000109` | **치명으로 이어짐** — rpcrt4 가 받아들이는 반환은 `0` · `0xc0000702` · `0xc0000703` · `0x4000002e` · `0x103` 뿐, 그 밖 (`0xc0000109` 포함) 은 `RpcpReportFatalError(2, …)` + `INT3` (2-4). 반환 `0` 이면 rpcrt4 가 부름을 `0x71a` 로 직접 끝내므로 서버 쪽 일 없음 |
| `:271 ~ :273` (Flags 0) · `:279 ~ :280` · `:327 ~ :328` `svc_lrpc_kind_unsupported` | 모르는 종류 거절 | **자리는 맞음, 해석을 고친다** — `:328` 에 오는 "종류 ≠ 0 · 1" 은 **종류 5**. 거절해도 클라이언트는 안 막힘 `[추측: 3-1 의 `0x40` 비트]` — 급하지 않다. 받아 주려면 답 (ALPC 종류 2, 본문 `[공백]`) 을 큐에 + 완료 패킷 하나. 로그에 먼저 남길 것: **Flags (`a[1]`) · `+0x28` · `+0x18` MessageId** |
| 보너스 (결과서) | Flags ≠ 0 인 비동기 요청 (`AlpcSend +0x92 ~ +0xa8`, `[부름+0x244] & 0x800000` 이면 `0xc00000`) | `:271` 의 `a[1]==0` 검사로 동기 갈래로 가는지 점검 거리 `[추측]` |

## 확인된 것

- `[정적]` winnsi 프로시저 6 (`0x7248`) = {24 B 구조체 (5 와 같은 타입), 컨텍스트 핸들 in/out, 반환}, 7 (`0x7278`) = {컨텍스트 핸들 (값, in), 16 B 출력 A, 24 B 출력 B, enum16 출력, 반환}. 둘 다 비동기 (Oi2 비트 7).
- `[정적]` 6 응답 24 B, 7 요청 20 B, 7 "변화 없이 끝냄" 응답 32 B (`+0x1c` 반환). 6 요청은 모듈 ID 길이에 따라 56 / 48 / 32 B — 모듈 ID 24 B 는 `NdrNonEncapsulatedUnionMarshall` 의 가름값 쓰기 `[정적]` + 사슬 `[추측]`.
- `[정적]` nsisvc 는 걸린 7 을 `RpcAsyncCompleteCall(…, &0x4c7)` (반환값 자리) 로 끝낸다 — fault 아님.
- `[정적]` 취소 메시지 = 종류 4 · 0x40 B · Flags `0x10000` · 보내기만 (`AlpcSendCancelMessage +0x57 / +0x88 / +0x9a`), 반환 안 봄. 서버 `ProcessIO` 는 `HandleCancelMessage` 만 (답은 `Type & 0x2000` 일 때만).
- `[정적]` `AlpcCancelMessage` 는 Context 속성 `MessageId` · `CallbackId` 둘 다 0 이면 `Sleep(2)` 를 끝없이 되풀이한다. `NtAlpcCancelMessage` 반환 다섯 값 밖은 치명.
- `[정적]` `RpcAsyncCancelCall [vtbl+0xa8]` = `CancelAsyncCall`, `CancelCall [vtbl+0x150]` = `ActualCancelCall` — `LRPC_CCALL` 가상표 `0xe0e18` (`pefile`, flow-writer).
- `[정적]` 클라이언트가 먼저 보내는 종류는 0 · 1 · 4 · 5. 5 는 `SendReservedMessage` (Flags 0, 보내기만, 서버 답함), 새 스레드 길은 `NegotiateTransferSyntax +0x49c → FindOrCreateCausalFlow +0xec → CleanupOrphanedCasaulFlowEntries +0xa9 → SendCausalFlowListForCleanup +0x62` (12개 이상).
- `[관찰]` test.exe 초기화 끝 모듈 6개에 rpcrt4 · winnsi 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`) — 이번 길은 test.exe 에선 안 생긴다.

## flow-writer 대조 (결과서와 다르게 적은 곳 — 결론 바꾸는 어긋남 없음)

결과서 오프셋을 `.asm` · `pefile` 로 맞춰 봤다. **결론을 바꾸는 어긋남은 없다.** call 자리 넷 · 함수 이름 하나를 바꿔 적었고, 보탬 셋:

1. **`AlpcCancelMessage` 의 `NtAlpcCancelMessage` call** — 결과서 2-1 단계 6 · 2-3 ⑤ "`+0x7e`" → `.asm` 은 `+0x7e XOR EDX,EDX` (둘째 인자 0) 이고 **call 은 `+0x80` (ret `+0x87`)**. 결과서 동적 3 ④ "`+0x87` 뒤 `eax`" 는 맞다.
2. **`ProcessIO` 의 `HandleReservedMessageRequest`** — 결과서 3-1 "`+0xa88 → HandleReservedMessageRequest`" → `+0xa88` 은 갈래 시작 줄 (`MOV R8,[RSP+0x40]` · `+0xa8d TEST [R8+4],0x40000000` · `+0xa95 JZ`) 이고 **call 은 `+0xc68` (ret `+0xc6d`)**. 다른 갈래 `+0x8db` (ret `+0x8e0`) 도 같은 함수를 부른다.
3. **`ActualCancelCall` (`0x945e8`) 의 call 자리** — 결과서 2-4 "`+0xa0` 가상 `[vtbl+0x128]`" → `+0xa0` 은 `MOV RAX,[RBX]` (가상표 읽기), **call 은 `+0xb2` (ret `+0xb7`)**. "`+0x4d` 갈래 = `AlpcCancelMessage`" 의 call 은 **`+0x69` (ret `+0x6e`)**.
4. **사슬 B 둘째 단계의 함수 경계** — 결과서 3-2 사슬 B 2 "`… → +0x42 WaitForSingleObject(INFINITE) → +0xe8 프로시저 5`" 의 `+0x42` 는 `NsiCompletePendingRpcCall` 안, **`+0xe8` 은 `NsiReregisterChangeNotification` 안** (`+0xdd LEA EDX,[R8+5]`) — 다른 함수다. 이 문서는 3-3 에서 나눠 적었다. 또 `NsiCompletePendingRpcCall +0x13` 은 `.asm` 상 `CMP byte [RDI],1` — 결과서의 "`[블록+0x80] == 1`" 은 RDI 가 `블록+0x80` 을 가리킨다고 본 것 (flow-writer 는 RDI 출처를 안 봄).
- 보탬 1: **가상 호출 대상 둘을 가상표로 확인** — `.asm` 주석 `LEA RAX,[0x1800e0e18] ; ??_7LRPC_CCALL@@6B@` 의 가상표 `0xe0e18` 에서 `+0xa8` = `0x180061f30` (`CancelAsyncCall`), `+0x150` = `0x18009b800` (`ActualCancelCall` LRPC_CCALL) (`pefile`). 결과서 사슬 1 → 2 · 4 → 5 와 맞다. 같은 칸 값은 `??_7LRPC_CCALL_AVRF@@6B@` (`0xe1530`) 등 가상표 여러 곳에 있다.
- 보탬 2: **`NdrpSimpleTypeEnum16Marshall` 자리** — `[0xe33c0 + 0x0d × 8]` = `0x1800900d0` (`pefile`), `pdb_aliases.py --where rpcrt4.dll NdrpSimpleTypeEnum16Marshall` → rva `0x900d0`, `--lookup` → 별칭 무리 아님. 결과서가 인용한 바이트 열 (`… 0f b7 02 66 89 01 49 83 40 08 02`) 은 이 함수 `+0x29` 에 있다.
- 보탬 3: **팔 정렬 니블** — 공용체 팔 서술 `0x6ec2` 의 `02 00` 위 4비트 = 0 이라 `NdrNonEncapsulatedUnionMarshall +0x71 ~ +0xa3` 의 정렬은 아무것도 안 한다. 가름값 뒤 2 B 패딩 (+6) 은 GUID 구조체 (`15 03` — 정렬 3) 가 스스로 4 정렬할 때 생긴다고 읽었다 `[추측: 팔 구조체 마샬러 안 읽음]` — 결과서 "공용체 몸은 4 정렬" 과 길이 결론은 같다.
- 그 밖 결과서 표의 줄 (winnsi 아홉 함수 · rpcrt4 스무 함수 · nsisvc 셋의 인용 줄) · `.rdata` 값 (winnsi 오프셋 표 · 6 · 7 머리 · 파라미터 8칸 · 타입 아홉 곳) · PE_emulator 줄 번호 (`nsi.cpp :10 · :14 · :36 · :77 · :79 · :98`, `lrpc.cpp :7 · :137 ~ :139 · :271 ~ :273 · :280 · :328`, `nt_alpc_cleanup.cpp:42`) 는 모두 결과서와 같았다.

## 앞 run 고쳐 읽기

원문은 그대로 두고 문서 맨 위에 한 줄씩 넣었다. **태그를 지킨다**: 길이 칸 · 형식 바이트 · 가름값을 쓰는 함수 줄은 `[정적]`, 그 함수가 이 길에서 불린다는 사슬은 `[추측]` — 그래서 "확정 정정" 이 아니라 **"고쳐 읽기 (VM 확인 전)"**.

- **run137** (`runs/03_sprint/run137-alpc-completion.md`):
  - 틀렸을 곳: 요약 `:25` (지금 `:27`) "nsi 프로시저 5 … 요청 40 B" · 3-2 `:276` (지금 `:278`) "요청 스텁 40 B … `+0x0c` 모듈 ID `Length` u16 · `+0x0e` 형 u16 (1 = GUID) · `+0x10` GUID 16 · `+0x20` compartment 8 (해석은 … '비캡슐 공용체 가름값은 앞 멤버' 같은 NDR 규칙에 기댐 `[규칙]` …)" · 5절 ⑦ `:306` (지금 `:308`) "스텁 = 3-2 요청 40 B" · 정정 3 `:364` (지금 `:366`) "→ NDR 2.0 판 **40 B** (12 + 20 + 8)". (정정 3 이 고쳐 쓴 run133 의 옛 짐작 "모듈 ID 24 B" 가 오히려 이번 값과 같다.)
  - 고쳐 읽기: 모듈 ID 선 위 = Length 2 · Type 2 · **가름값 2** · 패딩 2 · GUID 16 = **24 B** (LUID 16 B), 5 요청 = **44 B** (`+0x0c` 모듈 ID 24 · `+0x24` compartment 8) / LUID 36 B. 근거: sast/154 결과서 1-5 · 1-6, `NdrNonEncapsulatedUnionMarshall +0x5d ~ +0x65` → `NdrpSimpleTypeEnum16Marshall` 이 가름값 2 B 를 쓴다 `[정적]`, 그 함수가 이 구조체에서 불린다는 사슬은 `[추측]` → 진짜 길이는 VM. 응답 24 B 는 그대로 맞다.
- **run137 해설서** (`runs/03_sprint/run137-alpc-completion-해설.md`): 같은 수가 `:115` (표 "nsi 5 … 요청 40 B") · 2.13 제목 `:614` · `:623` · `:626` · `:675` · `:697` 에 있다 (맨 위 한 줄을 넣어 지금은 각 +2) → 맨 위 한 줄.
- **run129** (`runs/03_sprint/run129-winnsi-rpc.md`): 20 B / 40 B 는 run129 에 없다 (그 수는 run137 이 처음 적음). 대신 3절 표 7 줄 `:152` (지금 `:154`) "나옴: 16 B 구조체 · 24 B 모듈 ID 꼴 · DWORD · 반환 DWORD" → **"16 B 구조체 A {바이트 배열 포인터, 길이} · 24 B 구조체 B {enum16, 바이트 배열 포인터, LONG, LONG} (모듈 ID 꼴 아님) · enum16 (선 위 2 B) · 반환"** `[정적]` (sast/154 결과서 1-4). 6 줄 `:151` (지금 `:153`) "24 B 구조체 · 컨텍스트 핸들 (in/out) → 반환" 은 맞다. run129 해설서는 7 의 출력 칸을 안 적어서 고칠 곳 없음.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (rpcrt4 · winnsi 안 올림) 이고, 읽은 것은 winnsi 의 읽기 전용 형식 문자열과 rpcrt4 · nsisvc 코드 줄이다.

## 아직 확인 필요

동적 (결과서 "VM 으로 확인할 것 — 디버거 필요" — Q280 · Q271 RPC 묶음에 얹을 꼴, 멈춤점은 test.exe 가 아니라 winnsi 를 쓰는 프로세스 조건):

1. **선 위 요청 길이 (덤 1)** — winnsi 5 · 6 스텁이 44 / 56 B 인가 (run137 의 40, 요청서의 52 와 갈림). `rpcrt4!LRPC_BASE_CCALL::AlpcSend+0x128` (call 직전) `db @r8 L0x80`: `+0x00` DataLength = 스텁 + 0x40 (5 는 `0x6c` 면 44, `0x68` 면 40), `+0x3c` 프로시저 5 / 6, 스텁 `+0x68 ~`. 이게 1-3 의 `[추측]` 사슬을 닫는 가장 빠른 길.
2. **7 완료 응답 (덤 1)** — 진짜 스텁 32 B · 끝 `c7 04 00 00` 인가. `rpcrt4!IoCompleted+0xcf` 뒤 (run133 동적 5 자리) 받은 메시지 `+0x40 ~`.
3. **취소 (덤 2)** — `AlpcSendCancelMessage+0x9a` `edx` · `db @r8 L0x40` / `CancelAsyncCall+0x6d` `dd @rdi+0x108 L1` (상태 10 인가) / `AlpcCancelMessage+0x27` `dd @rbx+0x14 L2` (진짜 `MessageId` · `CallbackId`) · `AlpcSend+0x12f` 뒤 `호출+0x128` 칸 / `AlpcCancelMessage+0x87` `eax`.
4. **새 스레드 첫 메시지 (덤 3)** — `NtAlpcSendWaitReceivePort` 입구 `edx` · `poi(@r8+0x28)` · 스레드, 또는 `rpcrt4!LRPC_CASSOCIATION::SendReservedMessage` 입구 `dd @rcx+0x128 L1` · 반환 주소 (`SendCausalFlowListForCleanup+0x67` / `CleanupOrphanedCasaulFlowEntries+0xae`).

디버거 없이: VM 쪽 없음. PE_emulator 로그에 Flags · `+0x28` · `+0x18` 을 남기는 한 줄 (덤 3).

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. ntoskrnl — `NtAlpcSendWaitReceivePort` 가 보내는 쪽 Context 속성에 `MessageId` · `Sequence` 를 쓰는 줄 (`AlpcpSendMessage` · 속성 되돌려 쓰기) 와 `NtAlpcCancelMessage` 반환 표 → 2-3 의 `[추측]` 닫기 (Q280 커널 몫).
2. rpcrt4 — 호출 상태 9 · 10 · 11 · 12 의 이름 · 전이 (`DriveStateForward` 등) → 2-4 "10 이어야 한다" 를 `[정적]` 으로.
3. rpcrt4 — 복잡 구조체 마샬러가 `FC_EMBEDDED_COMPLEX` 공용체를 `NdrNonEncapsulatedUnionMarshall` 로 넘기는 줄 → 모듈 ID 24 B 를 `[정적]` 으로 (동적 1 이 더 빠르면 그쪽).
4. (급하지 않음) 종류 5 본문 항목 칸과 서버 답 (`HandleReservedMessageRequest` `0x95590` · `ParseReservedMessage`) — 5 를 받아 줄 때만.
5. (미룸) 7 출력 구조체 B 의 배열 개수 칸 (`19 00 1c 00`, `[공백]`) · 5 · 6 · 7 의 NDR64 판.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`. 단 `AlpcCancelMessage` 의 `Sleep(2)` 루프는 VM 에서 멈춤점으로 오래 붙잡으면 커널이 칸을 채우는 시점과 겹쳐 횟수가 달라질 수 있다 `[추측]` — 동적 3 은 "잤나 / 안 잤나" 만 본다.
- **읽을 때 조심** (하네스 제안 거리 아님, 기록만): 결과서가 "갈래 시작 줄" 을 call 자리처럼 적은 곳이 셋 있었다 (`ProcessIO +0xa88` · `ActualCancelCall +0xa0` · `+0x4d`) — 멈춤점에 쓸 땐 call 줄 (`+0xc68` · `+0xb2` · `+0x69`) 을 쓴다.
- 스크립트 문제 없음 (결과서). flow-writer 의 `pefile` 스크래치는 레포 밖, `-I` 없이 (run150 과 같은 까닭).
