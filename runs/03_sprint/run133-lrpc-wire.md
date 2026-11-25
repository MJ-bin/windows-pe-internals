# 2026-11-22 (작성일, 정적 분석 2026-11-22) run133 — LRPC 선 위 형식 (Q230, 정적만): rpcrt4 LRPC 클라이언트가 보내는 **bind 메시지 (0x70 B, 종류 1)** · **요청 머리 (종류 0, 호출 ID `+0x34` · 슬롯 `+0x38` · 프로시저 `+0x3c`, 스텁 `+0x68`)** 와, 받을 때 검사하는 칸 (bind 답은 **종류 · 수락 마스크 · PID 셋뿐**, 응답은 **종류 3 + 호출 ID 맞추기 + 길이**). winnsi · epmapper 대화는 **비동기** 라 (요청 둘은 `Ndr64AsyncClientCall` 로 `[정적]`, bind 둘은 `[추측]`) 답은 `NtAlpcSendWaitReceivePort` 의 둘째 부름 (받기만) 으로 온다. `ept_map` 질문 탑 4층 · 응답 검사 · 최소 응답 탑 바이트, nsisvc 는 **동적 끝점** (`LRPC-` + 18자) + `RpcEpRegisterW`, 프로시저 5 는 입력 값을 안 본다

> **일부 닫힘 · 고쳐 읽기 (run137, sast/132 — Q248)**: "아직 확인 필요" 정적 2 (완료 포트 연결 + 통지가 `LrpcIoComplete` 까지 오는 길) · 3 (NDR 형식 문자열) · 5 (`NegotiateTransferSyntax`) 와 "비동기 받기에 쌓인 답 없음 일 때의 정상 status" `[공백]` 은 **닫혔다** — `[정적]` `TppAllocAlpcCompletion +0x1d1 NtAlpcSetInformation(포트, 2, {TP_ALPC 포인터, 풀 IO 완료 포트 핸들}, 0x10)` 한 번 (이미 붙음 `0xc0000048` · 길이 틀림 `0xc0000004`), 메시지 하나 = 패킷 하나 `{키, 0, 0, 0}`, 일꾼 → `TppAlpcpExecuteCallback` → `LrpcIoComplete` → 썽크 → `IoCompleted`, 통지당 받기 한 번, **쌓인 메시지 없이 받기만 하면 `0xc0000001` (안 기다림 · 아무것도 안 씀) → rpcrt4 치명**. 5절 남은 `[공백]` 중 NDR 스텁 바이트 (④ · ⑦) 는 NDR 2.0 판으로 닫힘 (EPM 프로시저 8 요청 120 B · 응답 128 B, nsi 프로시저 5 응답 24 B · 요청 40 B), 서버가 고르는 문법은 "클라이언트는 bind 답의 비트 하나를 따름 · 흉내는 마스크 1 권장" 까지 좁혀짐 (진짜 값은 동적 → Q258). **고쳐 읽기**: ① 2-4 · 5절 ③ · PE_emulator 3번의 받기 Flags "0 / `0x1000000`" 은 클라이언트 연결 길에서 **늘 0** — 썽크 `LrpcCAssociationConnectIoHandler` 가 완료 문맥을 NULL 로 넘김 ② `LrpcIoComplete +0xc3` 간접 대상은 `IoCompleted` 가 아니라 그 **썽크** (rva `0x96a70`, `RCX −= 0xe0` · `EDX = 0` 뒤 `IoCompleted` 로 꼬리 점프) — 효과는 같고 `[추측]` → `[정적]` ③ 5절 ⑦ 요청 "24 + 24 + 8 B" `[추측]` → 40 B, 응답 24 B `[정적 + 규칙]` ④ 프로시저 8 은 공개 `ept_map` (3) 과 달리 객체 · 탑이 참조 포인터라 포인터 ID 가 없다 ⑤ PE_emulator `:602` 의 클래스 = **2** ⑥ 1-3 "`Ndr64AsyncClientCall` 이라 NDR64 가 자연스러워 보임" 은 근거가 안 됨 (슬롯이 NDR 2.0 이면 같은 진입에서 DCE 엔진). 정적 1 · 4 · 6 · `DoAsyncSend +0x3ee` 는 그대로 (Q246 ① · ④ · ⑥ · ⑦). 자세한 것은 run137 문서.

> **일부 닫힘 · 고쳐 읽기 (run141, sast/138 — Q267)**: "`DriveStateForward` 의 인자 전달을 안 읽음 `[공백]`" (요약 · 1-2 끝, 결과서 판정 ③) 은 **닫혔다** `[정적]`: `Bind__74b50 +0x5c ~ +0x64` 가 둘째 인자 (비동기 상태) ≠ NULL 이면 1 → `+0x151 EDX` → `DriveStateForward` `fAsync`. 그래서 "bind 둘 (epmapper · nsi) 이 비동기로 불림 `[추측]`" 중 **epmapper bind 는 `[정적]`** — 연결 응답 (종류 `0xb`) 뒤 **풀 일꾼** 이 `DriveStateForwardAsyncCallback +0xe0 → Bind__93cc0 → BaseBind → LRPC_CASSOCIATION::Bind +0x626` (Flags 0) 로 보낸다. nsi bind 는 그대로 `[추측]`. 동적 1 은 남되 보내는 스레드 (일꾼 TID) 도 같이 볼 것 (Q271 3). → `runs/03_sprint/run141-async-bind.md`.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("winnsi 알림 등록 RPC" 의 3 번 — T2 = nsisvc 알림 등록 **성공** 흉내에 필요한 LRPC 선 위 형식, 원 요청 `../PE_emulator/_workspace/05_tuning_planner/303_internals-rpc-nsi-notify.md`, 튜닝 `303_tune11.md` T2, 샘플 212) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 rpcrt4 · winnsi · nsisvc 를 안 올린다 — 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 와 같은 줄), `sample/*.c` 에 RPC API 가 없다 `[소스]`. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (nsisvc PDB 새 줄 · rpcrt4 줄 · 근거 줄) · 3장 run129 절 두 줄 · 3장 run129 절 곁 새 절 하나 · 4장 (Q230 닫힘 줄 · run129 줄 셋에 표시 · run133 줄 셋 · run132 정적 줄 오프셋 하나) · 5장 (run133 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q230** (sast/127 "다음 요청 후보" 1 · 2 · 3 = run129 정적 ① · ② · ③. 사용자 결정 2026-11-22 "Q230 도 지금 같이" — 그 전 결정 "등록 성공이 필요한 샘플이 보일 때만" 을 바꿈). 결과서 판정 **분석성공** (정적으로 닫을 수 있는 만큼, `[공백]` 6개 남김).
> - 이어 쓰는 앞 run: **run129** (sast/127 — winnsi 바인딩 · 끝점 풀이 사슬 · `AlpcConnect` 상태 표 · 서버 최소 **틀**). 그 8절 표 ③ · ④ · ⑥ 의 `[공백]` 을 이번에 채웠다. 아래 "정정" 절.
> - 덤 정정: **run132** 의 smss 오프셋 (파이프 이름 앞 SD = `SmpCreateProtectedPrefixes +0xa44`, `+0xab3` 은 메일슬롯) — 아래 "정정" 절.
> - 남은 동적은 새 번호 없이 **Q231** 에 출처로 붙였다 (Q231 ⑤ "진짜 ALPC 메시지 한 번" 과 같은 세션). 남은 정적은 **Q246**.

## 요약

- **구조 한 줄**: winnsi → epmapper → nsisvc 의 대화는 넷 (epmapper bind · `ept_map`, nsi bind · 프로시저 5) 이다. 태그는 셋으로 나뉜다.
  - **비동기 길 자체** `[정적]`: 보내는 `NtAlpcSendWaitReceivePort` 는 받기 칸 없이 (Flags 0, 받을 메시지 NULL) 곧바로 0 으로 돌아오고, 답은 **스레드풀 ALPC 완료 통지 → `IoCompleted` → 두 번째 `NtAlpcSendWaitReceivePort` (받기만)** 로 온다 (bind `+0x626` · 요청 `AlpcSend +0x128` · 받기 `IoCompleted +0xc8`).
  - **`ept_map` · 프로시저 5 가 비동기로 불림** `[정적]`: 둘 다 `Ndr64AsyncClientCall` 로 부른다 (`ept_map` = `LookupNextChunk +0x11d`, 프로시저 8 · 프로시저 5 = winnsi `NsiRpcRegisterChangeNotificationEx +0x1e8`, run129).
  - **bind 둘 (epmapper · nsi) 이 비동기로 불림** `[추측]`: 동기냐 비동기냐는 `LRPC_BIND_CCALL::Bind` 의 첫 정수 인자에서 오는데, `DriveStateForward` 의 인자 전달을 안 읽었다 (1-1, 결과서 판정 `[공백]` ③) — **동적 1**.
  - 동기 길 (`Flags 0x20000`, 같은 버퍼로 답을 받음) 은 코드에 있고 LSA 같은 다른 클라이언트가 쓴다 `[추측: 결과서 짐작, 동적 1 로 확인]`.
- **bind 보내기** `[정적]`: `LRPC_CASSOCIATION::Bind` (rva `0x4e330`) 가 `PORT_MESSAGE` (0x28) + `+0x28` 종류 **1** + `+0x34` 인터페이스 UUID · 버전 + `+0x48` **제안 전송 문법 비트마스크** (1 = NDR 2.0 · 2 = NDR64 · 4 = 셋째 문법) + `+0x4c` / `+0x4e` / `+0x50` 문법별 슬롯 번호 + `+0x54` 깃발 을 쓴다. `DataLength 0x48` · `TotalLength 0x70` · `Type 0x4000`. winnsi · EPM 둘 다 마스크 **3**.
- **bind 답 검사** `[정적]`: `CompleteBind` (rva `0x3ad68`) 는 `+0x28` (2 면 fault, 오류는 `+0x30`), `+0x48` 수락 마스크 (0 → `0x6c2`, 제안의 부분집합이 아니거나 비트가 정확히 하나가 아니면 `0x6c0`), `+0x08` 서버 PID (검사 없이 보관) **셋만** 본다. **길이 검사 없음.**
- **요청 머리** `[정적]`: `InitializeRequestAndAttributes` (rva `0x40a60`) — 동기 · 비동기 둘 다 이 함수. 종류 0, `+0x30` 깃발, `+0x34` 호출 ID, `+0x38` 슬롯 (bind 때 그 값), `+0x3c` 프로시저 번호 (EPM `ept_map` 8 · nsi 등록 5 · 알림 기다림 7), 스텁은 `+0x68` 부터. `DataLength = 스텁 + 0x40`.
- **응답 검사** `[정적]`: `UnpackResponse` (rva `0x40d90`) — `TotalLength ≥ 0x38`, `+0x28 == 3` 이면 응답 (`+0x34` 가 요청의 호출 ID 와 같아야, 스텁은 `+0x40` 부터), 아니면 fault (`+0x30` 오류). 틀리면 `0x6c0`.
- **ALPC 상태 → RPC 오류** `[정적]`: 보내기 세 자리 (bind · 동기 요청 · 비동기 요청) 가 표를 자기 안에 들고 있다. **`0xc0000008` (핸들 무효) → 셋 다 `0x6bf` (1727)**. `RpcpStrictAlpcToRpcStatus` (`0xc0000008 → 6`) 는 서버 쪽 표라 이 길에 안 쓰인다. `0x6bf` · `0x6ba` 는 다시 시도 종류, 첫 시도 + 최대 3번 다시.
- **`ept_map`** `[정적]`: 질문 탑 = 4층 70 B (인터페이스 · 전송 문법 NDR 2.0 · `0x0c` · `0x10` 끝점 없음). 응답은 층 수 3 ~ 6, 3층 `0x0c` 이면 층 수 정확히 4, 4층 RHS 길이 2 ~ 1000 + 끝 NUL 인 ASCII 이름만 본다 — 1 · 2층 값과 4층 프로토콜 ID 는 **안 본다**. 탑 0개 + 끝점 핸들 NULL → `0x6d9`.
- **nsisvc** `[정적]`: `RpcServerUseProtseqW("ncalrpc", 10, SD)` — 끝점 문자열 없음 = **동적 끝점** (rpcrt4 `ServerSetupAddress` 가 `LRPC-` + 난수 9 B 를 소문자 16진 18자로), 그 뒤 `RpcEpRegisterW(…, "NSI server endpoint")`. 프로시저 5 (`RpcNsiRegisterChangeNotification`) 는 **입력 값을 검사하지 않고** 호출자 (커널 모드 아님) · 흉내내기 · `nsi!NsiRegisterChangeNotificationEx` 0 만 보고, 성공이면 0x98 B 블록 포인터를 컨텍스트 핸들로. 프로시저 7 은 대기 중 변경이 없으면 **완료하지 않고 걸어 둔다**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔한째 바퀴. plan **Q230**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze** 는 nsisvc 하나 (11초, 직전 `free -g` available 30 G, 도는 Ghidra 없음). rpcrt4 (run99 때 analyze) 는 다시 analyze 안 하고 `^이름$` export 만 (8번). sast/128 · 129 모듈 (user32 · combase · npfs · kernelbase · ntoskrnl) 은 안 건드림.

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| nsisvc | `nsisvc.dll` 61,440 B | **`CE94A7C9096AB626B28E58C48D6605591`** (이번에 처음 받음) | 77 |
| rpcrt4 | `rpcrt4.dll` | `0204A8A137C42B7DF1F0811A3A8277B21` (기준 줄과 같음) | — |

- nsisvc PDB GUID 는 `llvm-pdbutil dump --summary` 의 `{CE94A7C9-096A-B626-B28E-58C48D660559}` 와 같음. PDB 안 Age 3, 심볼 서버 키는 바이너리의 Age 1 (run128 ws2_32 · run129 winnsi 와 같은 모양). `ms/ghidra/nsisvc.analyze.log` 에 `SetPdb:` 줄 있음 (`ms/sym/nsisvc.dll.pdb`) — 함수 이름이 PDB 이름 (`NsiStartRpcServer` · `RpcNsiRegisterChangeNotification`) 으로 잡힘.
- 공개 PDB 라 구조체 타입 이름이 없다. 칸 이름은 코드 줄에서 읽은 것.
- 서버 쪽 프로시저 5 · 6 · 7 구현은 **nsisvc 안에 있다** (요청서의 "껍데기뿐이면 멈춤" 은 해당 없음). 실제 NSI 일은 지연 가져오기 `nsi!NsiRegisterChangeNotificationEx` (nsi rva `0x1eb0`, 안 열었음).
- 버전 대조 (결과서): "알려진 사실" 표에 rpcrt4 · nsisvc 줄이 없어 `.asm` `+0x` 를 원본 바이트 (`pefile`) 와 직접 맞춤 — rpcrt4 `Bind__4e330 +0x502` · `+0x509` · `+0x50f` · `+0x626` · `+0x6cb`, `CompleteBind +0x22`, `UnpackResponse +0x37`, `InitializeRequestAndAttributes +0x1ab`, `DoSendReceive +0x14d` · `AlpcSend +0x128` · `IoCompleted +0xc8`, nsisvc `NsiStartRpcServer +0x123` · `+0x1a1`, `RpcNsiRegisterChangeNotification +0x147`, `ServiceMain +0xf6` — **일치**.
- rpcrt4 는 같은 이름 함수가 여럿이라 `.asm` 이 `__<rva>` 로 갈린다. 이 문서의 클래스 ↔ 파일:
  - `LRPC_CASSOCIATION::Bind` = `Bind__4e330.asm` · `LRPC_BIND_CCALL::Bind` = `Bind.asm` (rva `0x4cc20`) · `BaseBind` (`0x4c660`)
  - `HandleReply` 다섯: `__41bb0` · `__417f0` (bind 답도 받는 쪽) · `HandleReply.asm` (`LRPC_BASE_CCALL`, `0x3ee10`) · `__580f0` (`LRPC_CASSOCIATION`) · `__9c6fc` (`LRPC_ADDRESS`, 서버)
  - `SendReceive__3fc00` (`LRPC_CCALL`) · `__40250` (`LRPC_BASE_CCALL`), `AlpcSend.asm` (클라이언트, `0x42a18`) / `AlpcSend__56d44` (서버), `AlpcSendWaitReceivePort.asm` (`LRPC_CASSOCIATION`, `0x3fa50`) / `__6def8` (서버), `GetPresentationContext.asm` (`LRPC_CCALL`, `0x8fcf0`).
  - `pdb_aliases.py --lookup rpcrt4.dll` 로 `0x4e330` · `0x3ad68` · `0x40d90` · `0x40a60` · `0x41bb0` · `0x417f0` · `0x3ee10` · `0x40680` · `0x41f80` · `0x42a18` · `0x3fa50` · `0x57800` · `0x8fcf0` · `0x5a968` · `0x5a350` · `0x971d0` · `0x915f8` · `0x91634` · `0x99d70` · `0x57650` · `0x9c7ec` — **모두 별칭 무리 아님**. `_calls.tsv` 의 `alias:X+1` 은 같은 함수의 C++ 장식 이름 하나 (`.asm` `ALIASES(1)`) 라 PDB 이름을 그대로 썼다 (run129 와 같은 모양).

## 목적

- PE_emulator 에서 샘플 212 개 (악성 203) 가 winnsi 알림 등록에서 멈춘다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님). run129 의 싼 길 (epmapper 연결 `0xc0000034` → 1722 → `WinHttpSendRequest` 0) 은 "깨끗하게 실패" 다. **등록을 한 번 성공시켜 그 뒤 길 (진짜 HTTP 요청) 을 보려면** 서버 흉내 (run129 8절 ① ~ ⑧) 가 필요한데, 그중 LRPC 선 위 형식이 비어 있었다.
- 같은 틀을 LSA (`GetUserName*` 0 반환 189 샘플) · RPCSS (`CoInitializeSecurity` 의 "핸들 0 보내기" `0xc0000008` 뒤 combase `+0x55b5a` 가 주소 `0x4` 읽기, 22 샘플) 도 쓴다 → **공통 몫 (rpcrt4 LRPC 머리 · bind) 과 winnsi / epmapper 몫을 나눠서**.
- 요청서 `_workspace/sast/130_lrpc-wire.md` 의 물음:
  1. LRPC bind (③ · ⑥) — 보내는 bind 메시지 칸 · `NtAlpcSendWaitReceivePort` 인자, 받는 쪽 검사 칸과 틀리면 RPC 오류.
  2. 요청 · 응답 머리 (④ · ⑦) — 요청 칸, 응답 검사 칸 · fault, ALPC 오류 → RPC 오류 표 (핸들 0 `0xc0000008` 포함).
  3. `ept_map` 응답 검사 (④) — 질문 탑 층, 응답에서 보는 것, 서버 흉내가 돌려줄 최소 탑.
  4. nsisvc 서버 쪽 — 끝점 이름, 프로시저 5 의 입력 검사 · 컨텍스트 핸들.

## 방법

- 정적 sast/130:
  - nsisvc: `fetch_pdb.py` 로 PDB 받고 처음 analyze → depth 0 22개 (`ServiceMain` · `NsiServiceInitialize` · `NsiServiceNotifyStart` · `NsiStartRpcServer` · `NsiStopRpcServer` · `NsiSecurityCallBack` · `NsiGetRpcCallAttributes` · `RpcNsiRegisterChangeNotification` · `RpcNsiRequestChangeNotification` · `RpcNsiDeregisterChangeNotification` · `RpcNsiParameterChange` · `NOTIFICATION_HANDLE_rundown` · `Nsip*` 다섯 · `CleanupNotificationThread` 등). 로그 `ms/ghidra/nsisvc.export.sast130a.stdout`.
  - rpcrt4 `^이름$` depth 0 (로그 `ms/ghidra/rpcrt4.export.sast130{a..h}.stdout`): `Bind` (×4) · `BaseBind` · `CompleteBind` · `HandleReply` (×5) · `SendReceive` (×10) · `AlpcSendWaitReceivePort` (×2) · `AlpcSend` (×2) · `PreinitRequestAlpcHeaders` (×2) · `RpcpAlpcpGetHeaderSize` · `RpcpStrictAlpcToRpcStatus` · `AsyncSend` (×9) · `HandleSendReceiveFailure` · `HandleAsyncSendFailure` · `NegotiateTransferSyntax` (×7) · `DoSendReceive` · `DoAsyncSend` · `AlpcNonPipeSend` · `UnpackResponse` · `IoCompleted` · `ValidateCallbackReply` · `SanitizeErrorCodeFromServer` · `MessageReceived` (×2) · `LrpcIoComplete` · `OsfTowerConstruct` · `InitializeRequestAndAttributes` · `ServerSetupAddress` · `GetPresentationContext` (×3) · `AttemptRetry` · `RetryCall` · `GetErrorAndErrorClass` (×2) · `GetErrorClass` 등. sast/127 때 있던 `LookupNextChunk` · `ProcessNextChunk` · `ReturnLookupValues` · `TowerConstruct` · `LookupNotifyRoutine` 은 읽기만. refs `ms/disassemble/rpcrt4/_refs/0xeced8.tsv` (`LRPC-` 문자열 → `ServerSetupAddress`).
  - 일회용 읽기 (`pefile`, 파일로 안 남김): rpcrt4 문법 GUID 셋 (rva `0xebaf8` · `0xebb10` · `0xebb88`) · 비트 표 (`0xeb9c0`) · 엄격 상태 표 (`0xed0d0`, 15쌍) · EPM 인터페이스 (`0xe1220`) · 프록시 정보 (`0xe11e8`) · EPM 문법 표 (`0xe7560`, 2줄); winnsi 인터페이스 (`0x67b0`) · 문법 표 (`0x6150`, 2줄); nsisvc 서버 인터페이스 (`0x6420`) · 디스패치 표 (`0x64f0`, 9개) · 서버 정보 (`0x6600`).
- 결과서: `_workspace/sast/130_lrpc-wire_result.md` (**분석성공**, 스크립트 문제 없음 — `_calls.tsv` 가 같은 이름 함수의 호출을 섞는다는 참고만).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` call · ret 짝 — rpcrt4 `Bind` (`+0x626` · `+0x6ad` · `+0x6cb` · `+0x7d0` · `+0x870` · `+0x1fb`), `BaseBind +0x1e4`, `NegotiateTransferSyntax +0x769`, `SendReceive__3fc00` · `__40250`, `DoSendReceive`, `DoAsyncSend`, `AlpcNonPipeSend`, `AlpcSend`, `AlpcSendWaitReceivePort`, `IoCompleted`, `LrpcIoComplete`, `HandleReply__41bb0` · `__417f0` · `HandleReply`, `CompleteBind`, `UnpackResponse`, `ValidateCallbackReply`, `ResolveEndpoint__59d38 +0xba`, `LookupNextChunk +0x11d`, `LookupNotifyRoutine`, `ResolveEndpointComplete +0x3c`, `ServerSetupAddress +0xe8`; nsisvc `ServiceMain` · `NsiStartRpcServer` · `NsiSecurityCallBack` · `RpcNsiRegisterChangeNotification` · `RpcNsiRequestChangeNotification`.
  - `.asm` 줄: `Bind__4e330 +0x91` · `+0x48b` · `+0x492` · `+0x4e0` · `+0x502` · `+0x509` · `+0x50f` · `+0x53c` · `+0x540` · `+0x587` · `+0x5ba` · `+0x69c ~ +0x6d0` · `+0x7c5` · `+0x7e2` · `+0x823`, `Bind +0x42` · `+0x1d7` · `+0x1e8` · `+0x1ed`, `BaseBind +0x1bc ~ +0x1ed`, `CompleteBind +0x22` · `+0x2c` · `+0x6e` · `+0x7b` · `+0xf0` · `+0x126`, `UnpackResponse +0x2d` · `+0x31` · `+0x37` · `+0x7d` · `+0x8b` · `+0xa5` · `+0x1ff`, `InitializeRequestAndAttributes +0x1a7` · `+0x1ab` · `+0x1ea` · `+0x2ea` · `+0x2f3`, `IoCompleted +0x1b6` · `+0x206` · `+0x277 ~ +0x27f` · `+0x486` · `+0x4a6` · `+0x4ab` · `+0x4cd` · `+0x4d0`, `AlpcSend +0x92` · `+0x9f` · `+0xa8` · `+0x11c` · `+0x297` · `+0x29c` · `+0x2e9`, `DoSendReceive +0x15b`, `GetErrorClass +0x9 ~ +0x25`, `AttemptRetry +0x37 ~ +0x57`, `ProcessNextChunk +0x47` · `+0x93` · `+0x1e3` · `+0x391` · `+0x39e` · `+0x3e8` · `+0x443`, `LookupNotifyRoutine +0x7c` · `+0x83`, `LookupNextChunk +0xe6`, `ServerSetupAddress +0xc4` · `+0xd1`; nsisvc `NsiSecurityCallBack +0x66` · `+0x83` · `+0x88` · `+0x8a`, `RpcNsiRegisterChangeNotification +0x5c` · `+0x9e` · `+0xa2` · `+0x153` · `+0x1e4` · `+0x228` · `+0x22a` · `+0x33b` · `+0x345`, `RpcNsiRequestChangeNotification +0x7d` · `+0x87` · `+0x18b`. 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유 (run129 의 "114 안내에 전화해 nsi 사무실 번호를 묻는다" 이어서):

- 전화 한 통마다 맨 앞에 **"저 누구고, 이 말 (전송 문법) 할 줄 알아요"** 를 먼저 한다 (bind). 상대는 "그 말 중 **이것 하나** 로 해요" 라고만 답하면 된다. rpcrt4 는 그 답에서 세 군데만 본다 `[추측: 비유]`.
- 그다음 본론 (요청) 엔 **번호표 (호출 ID)** 를 붙인다. 답장에 같은 번호표가 없으면 "엉뚱한 답장" 으로 버린다 `[추측: 비유]`.
- 이 전화들은 다 **"끝나면 벨 울려 주세요" 방식** 이다. 말을 건네자마자 수화기를 내려놓고, 벨 (완료 통지) 이 울리면 그때 **답을 따로 받으러 간다** `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{rpcrt4,nsisvc}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `rpcrt4!LRPC_BIND_CCALL::Bind` (`0x4cc20`) | `+0x1fb` | `+0x200` | `LRPC_CASSOCIATION::Bind` (`0x4e330`) — 비동기 / 동기 = `R8D` (`+0x42` · `+0x1d7`) | [정적] |
| `rpcrt4!…::BaseBind` (`0x4c660`) · `NegotiateTransferSyntax` (`0x43a20`) | `+0x1e4` · `+0x769` | `+0x1e9` · `+0x76e` | 같은 `LRPC_CASSOCIATION::Bind` | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::Bind` | `+0x471` | `+0x476` | `BCACHE::Allocate` (`0x1000` 버퍼, 안 지움) | [정적] |
| 〃 | **`+0x626`** | **`+0x62d`** | **`NtAlpcSendWaitReceivePort`** — bind 메시지 0x70 B | [정적] |
| 〃 | `+0x6ad` · `+0x6cb` | `+0x6b2` · `+0x6d0` | `MessageReceived` · **`CompleteBind`** (동기만) | [정적] |
| 〃 | `+0x7d0` | `+0x7d5` | `BCACHE::Free` (비동기 — 보내자마자 버퍼 풂, `+0x7c5` `0xc0021018`) | [정적] |
| 〃 | `+0x870` | `+0x875` | `AbortAssociation` (동기 bind 오류 → 연결 닫음) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::CompleteBind` (`0x3ad68`) | `+0xf0` | `+0xf5` | `SanitizeErrorCodeFromServer` (fault 오류 거름) | [정적] |
| `rpcrt4!LRPC_CCALL::SendReceive` (`0x3fc00`) | `+0x71` · `+0xa0` | `+0x76` · `+0xa5` | `DoSendReceive` · `UnpackResponse` | [정적] |
| `rpcrt4!LRPC_BASE_CCALL::SendReceive` (`0x40250`) | `+0x51` · `+0x80` | `+0x56` · `+0x85` | `DoSendReceive` · `UnpackResponse` | [정적] |
| `rpcrt4!DoSendReceive` (`0x40680`) | `+0x48` | `+0x4d` | `InitializeRequestAndAttributes` (`0x40a60`) | [정적] |
| 〃 | **`+0x14d`** | **`+0x154`** | **`NtAlpcSendWaitReceivePort`** — 동기 요청 | [정적] |
| `rpcrt4!DoAsyncSend` (`0x41f80`) | `+0x6e` · `+0x94` · `+0x2b6` | `+0x73` · `+0x99` · `+0x2bb` | `InitializeRequestAndAttributes` | [정적] |
| 〃 | `+0x87` · `+0x2dc` | `+0x8c` · `+0x2e1` | `AlpcNonPipeSend` | [정적] |
| `rpcrt4!AlpcNonPipeSend` | `+0x2b` | `+0x30` | `AlpcSend` (`0x42a18`) | [정적] |
| `rpcrt4!LRPC_BASE_CCALL::AlpcSend` | **`+0x128`** | **`+0x12f`** | **`NtAlpcSendWaitReceivePort`** — 비동기 요청, 받기 칸 0 | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::AlpcSendWaitReceivePort` (`0x3fa50`) | `+0xbb` | `+0xc2` | `NtAlpcSendWaitReceivePort` (감싸개, 상태 안 바꿈) | [정적] |
| `rpcrt4!LrpcIoComplete` | `+0xc3` | `+0xc8` | 간접 `[R14]` (대상 `IoCompleted` 로 읽힘 `[추측: 간접 호출]`) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::IoCompleted` (`0x57800`) | **`+0xc8`** | **`+0xcf`** | **`NtAlpcSendWaitReceivePort`** — 받기만 (비동기 답) | [정적] |
| 〃 | `+0x21c` | `+0x221` | 간접 `[RAX+0x130]` = 호출 객체의 `HandleReply` (ALPC 종류 2 · LRPC 종류 1 · 2 · 3 일 때) | [정적] |
| 〃 | `+0x4a6` | `+0x4ab` | `RpcpReportFatalError` (받기 오류가 `0xc0` · `0xc0000041` 밖이면 — `+0x4ab` INT3) | [정적] |
| `rpcrt4!HandleReply__41bb0` · `__417f0` | `+0x5f` · `+0x7d` | `+0x64` · `+0x82` | `UnpackResponse` | [정적] |
| 〃 | `+0xdf` · `+0xfd` | `+0xe4` · `+0x102` | `CompleteBind` (호출 상태 `[+0x108] == 9` 일 때) | [정적] |
| `rpcrt4!LRPC_BASE_CCALL::HandleReply` (`0x3ee10`) | `+0x53` | `+0x58` | `UnpackResponse` | [정적] |
| `rpcrt4!UnpackResponse` (`0x40d90`) | `+0x1ff` | `+0x204` | `SanitizeErrorCodeFromServer` (fault 갈래) | [정적] |
| `rpcrt4!ValidateCallbackReply` (`0x9c7ec`) | `+0x146` | `+0x14d` | `NtAlpcSendWaitReceivePort` (쓸모없는 속성 버림) | [정적] |
| `rpcrt4!EP_LOOKUP_DATA::ResolveEndpoint` (`0x59d38`) | `+0xba` | `+0xbf` | `TowerConstruct` (`0x5a350`) — 질문 탑 | [정적] |
| `rpcrt4!LookupNextChunk` (`0x5a070`) | `+0x11d` | `+0x122` | `Ndr64AsyncClientCall` — EPM 프로시저 8, `max_towers` 4 (`+0xe6`) | [정적] |
| `rpcrt4!LookupNotifyRoutine` (`0x5c578`) | `+0x4d` · `+0x69` | `+0x52` · `+0x6e` | `RpcAsyncCompleteCall` · **`ProcessNextChunk`** (`0x5a968`) | [정적] |
| 〃 | `+0xf9` · `+0x184` | `+0xfe` · `+0x189` | `FatalLookupErrorEncountered` | [정적] |
| `rpcrt4!ResolveEndpointComplete` | `+0x3c` | `+0x41` | `ReturnLookupValues` (`0x5b9f8`) | [정적] |
| `rpcrt4!LRPC_ADDRESS::ServerSetupAddress` (`0x971d0`, 서버) | `+0xe8` | `+0xef` | `ProcessPrng` (난수 9 B → 끝점 이름) | [정적] |
| `nsisvc!ServiceMain` | `+0xed` · `+0xf6` · `+0x166` | `+0xf2` · `+0xfb` · `+0x16b` | `NsiServiceInitialize` · **`NsiStartRpcServer`** · `NsiServiceNotifyStart` | [정적] |
| `nsisvc!NsiStartRpcServer` | `+0xac` | `+0xb3` | `ConvertStringSecurityDescriptorToSecurityDescriptorW` | [정적] |
| 〃 | **`+0x123`** | **`+0x12a`** | **`RpcServerUseProtseqW("ncalrpc", 10, SD)`** — 끝점 없음 | [정적] |
| 〃 | `+0x1a1` | `+0x1a8` | `RpcServerRegisterIf3` (Flags 9, MaxCalls 1234, 콜백 `NsiSecurityCallBack`) | [정적] |
| 〃 | `+0x1fb` · `+0x28a` | `+0x202` · `+0x291` | `CreateEventW` · `RegisterWaitForSingleObjectEx` (정리 스레드) | [정적] |
| 〃 | `+0x2de` · **`+0x337`** | `+0x2e5` · **`+0x33e`** | `RpcServerInqBindings` · **`RpcEpRegisterW(…, "NSI server endpoint")`** | [정적] |
| `nsisvc!NsiSecurityCallBack` (`0x2e80`) | `+0x5a` | `+0x61` | `RpcServerInqCallAttributesW` (인증 수준 ≥ 6 검사) | [정적] |
| `nsisvc!RpcNsiRegisterChangeNotification` (`0x27c0`, 프로시저 5) | `+0x88` | `+0x8f` | `RpcServerInqCallAttributesW` | [정적] |
| 〃 | `+0xe6` · `+0x12c` | `+0xeb` · `+0x133` | `MALLOC` (0x98) · `RpcImpersonateClient` | [정적] |
| 〃 | **`+0x147`** | **`+0x14e`** | **`nsi!NsiRegisterChangeNotificationEx`** (지연 가져오기) | [정적] |
| 〃 | `+0x155` · `+0x1d8` · `+0x21c` | `+0x15c` · `+0x1df` · `+0x223` | `RpcRevertToSelf` · `EnterCriticalSection` · `LeaveCriticalSection` | [정적] |
| 〃 | `+0x365` | `+0x36a` | `NsipDeregisterAndFreeNotificationContext` (서버 초기화 안 됨 → `0x139f`) | [정적] |
| `nsisvc!RpcNsiRequestChangeNotification` (`0x1010`, 프로시저 7) | `+0x30` · `+0xdf` | `+0x37` · `+0xe6` | `RpcImpersonateClient` · `RpcServerSubscribeForNotification` (끊김 알림) | [정적] |
| 〃 | `+0x19b` · `+0x256` · `+0x3fb` | `+0x1a2` · `+0x25d` · `+0x402` | `RpcAsyncCompleteCall` (오류 · 변경이 쌓였을 때만) | [정적] |

### 1. bind (질문 1, run129 8절 ③ · ⑥) [정적]

#### 1-1. `NtAlpcSendWaitReceivePort` 인자 — `LRPC_CASSOCIATION::Bind +0x626`

| 인자 | 동기 bind | 비동기 bind | 줄 |
|---|---|---|---|
| PortHandle | `[연결객체+0x88]` | 같음 | `+0x5fd` |
| Flags | **`0x20000`** (`ALPC_MSGFLG_SYNC_REQUEST` 로 읽힘 `[이름 추측]`) | **`0`** | `+0x540 MOV R15D,0x20000`, `+0x61e` |
| SendMessage | bind 메시지 (0x1000 버퍼의 앞 0x70 B) | 같음 | `+0x616` |
| SendMessageAttributes | Allocated = Valid = **`0x20000000`** (Context 하나), `MessageContext` = **bind 를 부른 `LRPC_BIND_CCALL*`** | 같음 | `+0x587` · `+0x597 RpcpAlpcpGetHeaderSize(0)` · `+0x5a7` · `+0x5ba` |
| ReceiveMessage · ReceiveAttrs | **보낸 버퍼 · 속성과 같음** | **NULL · NULL** | `+0x55c` · `+0x55f` / `+0x77d` · `+0x77a` |
| BufferLength | `&0x1000` | 같음 | `+0x5c9` |
| Timeout | 0 → NULL, `0xffffffff` → 무한, 그 밖 `−(ms × 10000)` | **NULL** | `+0x546` · `+0x7ec` · `+0x851` |

- 비동기 bind 는 보내기가 0 이면 버퍼를 곧바로 풀고 (`+0x7d0`) 보류 `0xc0021018` 을 돌려준다 (`+0x7c5`). 답은 2-3 의 길로.
- 동기냐 비동기냐는 `LRPC_BIND_CCALL::Bind` 의 첫 정수 인자 (`+0x42 MOV R12D,EDX` → `+0x1d7 MOV R8D,R12D`) 에서 온다. epmapper bind · nsi bind 가 정말 비동기인지는 `DriveStateForward` 의 인자 전달을 안 읽어 `[추측]` — **동적 1**.

#### 1-2. bind 메시지 칸 (보내는 쪽) — `Bind__4e330.asm`

| 칸 | 오프셋 | 크기 | 값 | 줄 |
|---|---|---|---|---|
| `DataLength` · `TotalLength` | `+0x00` · `+0x02` | u16 ×2 | **`0x48` · `0x70`** | `+0x502 MOV qword [RBX],0x700048` |
| `Type` | `+0x04` | u16 | **`0x4000`** (`LPC_NO_IMPERSONATE` 로 읽힘 `[이름 추측]`) | `+0x509` |
| `ClientId` · `MessageId` · `+0x20` | `+0x08 ~ +0x27` | | 0 | `+0x513 ~ +0x51f` |
| **LRPC 종류** | `+0x28` | u64 | **1** (bind) | `+0x50f` |
| **인터페이스 UUID** · **버전** | `+0x34` · `+0x44` | 16 · u32 | EPM `e1af8308-5d1f-11c9-91a4-08002b14a0fa` **v3.0** · nsi `7ea70bcf-48af-4f6a-8968-6a440754d5fa` **v1.0** | `+0x48b` · `+0x492` |
| **전송 문법 마스크** | `+0x48` | u32 | 인터페이스 문법 표마다 OR — **1 = NDR 2.0** (`8a885d04-1ceb-11c9-9fe8-08002b104860` v2) · **2 = NDR64** (`71710533-beba-4937-8319-b5dbef9ccc36` v1) · **4** = `b4537da9-3d03-4f6b-b594-52b2874ee9d0` v1. 표에 없는 문법 → `0x6c2`. **winnsi · EPM 둘 다 3** | `+0x495` · `+0x4e0` · `+0x713` · `+0x8a5`; GUID rva `0xebaf8` · `0xebb10` · `0xebb88`, 비트 표 `0xeb9c0` |
| 문법 1 · 2 · 4 의 **슬롯 번호** | `+0x4c` · `+0x4e` · `+0x50` | u16 ×3 | 연결 객체 표 (`this+0x58`) 에서 그 문법 결합의 칸 번호 | `+0x4eb` · `+0x71e` · `+0x8b0` |
| 깃발 | `+0x54` | u32 | 동기 0 / 비동기 **`인터페이스.Flags & 0x2000000`** (winnsi · EPM 둘 다 `0x2000000`) | `+0x53c` · `+0x780 ~ +0x78b` |
| 호출자 인자 6 · 7 | `+0x58` · `+0x60` | u32 · u64 | `LRPC_BIND_CCALL::Bind` 는 0 · 0 | `Bind.asm +0x1e8` · `+0x1ed` |
| 연결 객체 칸 | `+0x68` | u32 | `[연결객체+0x244]` | `+0x57f` |

- 버퍼를 0 으로 지우지 않으므로 (`BCACHE::Allocate` `+0x471`) 안 쓰는 칸 (`+0x30` · `+0x52`) 은 이전 값이 남는다.
- 인터페이스 구조체 `Length` 가 `0x60` 이 아니면 `0x6b5` (`+0x91` · `+0x7e2`).
- **슬롯 번호** 는 나중에 요청 머리 `+0x38` 에 그대로 간다 (2-1) — 서버는 이걸로 호출의 전송 문법을 안다.

#### 1-3. bind 답 (받는 쪽) — `CompleteBind.asm`

경로: 동기 = `Bind +0x6ad MessageReceived` → `+0x6cb CompleteBind` (같은 버퍼가 답으로 덮어써짐). 비동기 = `IoCompleted` → `+0x21c` `HandleReply` → 상태 `[+0x108] == 9` 면 `CompleteBind` (`HandleReply__41bb0 +0xdf` · `__417f0 +0xfd`).

| 칸 | 오프셋 | 기대 | 틀리면 | 줄 |
|---|---|---|---|---|
| ALPC 메시지 종류 (비동기) | `+0x04` 아래 바이트 | **2** (reply). 5 = 포트 닫힘, 1 = 서버의 콜백 요청, `0xb` 따로 | bind 답으로 안 읽힘 | `IoCompleted +0x12f ~ +0x19d` |
| LRPC 종류 | `+0x28` | **2 면 fault** (`+0x30` 로), 그 밖 = 성공으로 진행. 비동기는 1 · 2 · 3 만 `HandleReply` 로 | — | `CompleteBind +0x22`; `IoCompleted +0x1e7 ~ +0x1f7` |
| fault 오류 | `+0x30` | 서버 오류 코드 | **0 이거나 치명 14종이면 `0x6c0`**, 그 밖은 그대로 | `+0xf0 SanitizeErrorCodeFromServer` |
| **수락 마스크** | `+0x48` | 0 아님 · **제안 (`+0x48`) 의 부분집합** · **비트 정확히 하나 (1 / 2 / 4)** | 0 → **`0x6c2`**, 나머지 → `0x6c0` | `+0x2c` · `+0x3a ~ +0x5f` · `+0x14d ~ +0x16a` · `+0x126` |
| 서버 PID | `+0x08` | 검사 없음 — `[연결객체+0x238]` 이 0 이면 보관 | — | `+0x6e` · `+0x7b` |
| `TotalLength` | `+0x02` | **검사 없음** (코드는 `+0x4c` 까지 읽음) | — | (읽는 줄 없음) |

- 성공이면 호출 상태 10. 동기 bind 오류는 `AbortAssociation` (`+0x870`) 로 **연결 객체가 닫힌다**.
- **최소 bind 답** = `+0x28` 1 (보낸 그대로) + `+0x48` 마스크 하나 (제안에 든 것) + `+0x08` 0 아닌 PID (권장). ALPC 쪽은 종류 2 + 받기 속성의 Context `MessageContext` = **보낸 값 그대로** (2-2).
- 서버가 NDR 2.0 (1) 을 고르나 NDR64 (2) 를 고르나 → 코드로는 **둘 다 받는다**. 클라이언트가 `Ndr64AsyncClientCall` 을 쓰니 NDR64 가 자연스러워 보이지만 `[추측]` — **동적 2**.

### 2. 요청 · 응답 머리 (질문 2, run129 8절 ④ · ⑦) [정적]

#### 2-1. 요청 머리 — `InitializeRequestAndAttributes.asm` (동기 `DoSendReceive +0x48` · 비동기 `DoAsyncSend +0x6e` 다 이 함수)

| 칸 | 오프셋 | 값 | 줄 |
|---|---|---|---|
| `DataLength` · `TotalLength` | `+0x00` · `+0x02` | 스텁 길이 **+ 0x40** (큰 데이터를 view 로 보내면 고정 `0x48`) · `DataLength + 0x28` | `+0x192 ~ +0x1a1` · `+0x1a7` · `+0x2ea` |
| `Type` · `DataInfoOffset` | `+0x04` · `+0x06` | 0 · 0 (**bind 와 달리 `0x4000` 아님**) | `PORT_MESSAGE` 0x28 B 지움 |
| **LRPC 종류** | `+0x28` | **0** (요청) | `+0x1ab` |
| 깃발 | `+0x30` | 0 / **4** (스텁을 데이터 view 로) / `\|= 8` / `\|= 1` (인과 흐름 GUID 있음) | `+0x19d` · `+0x2c7` · `+0x206` · `+0x319` |
| **호출 ID** | `+0x34` | `[호출+0x124]` | `+0x1db` · `+0x1ea` |
| **슬롯 번호** | `+0x38` | `GetPresentationContext` = bind 때 그 슬롯 | `+0x1b6 ~ +0x1cf` |
| **프로시저 번호** | `+0x3c` | `RPC_MESSAGE.ProcNum` — EPM `ept_map` 비동기 **8** · nsi 등록 **5** · 알림 기다림 **7** | `+0x1d2 ~ +0x1d8` |
| (0) | `+0x40` · `+0x48` · `+0x50` | 0 | `+0x1e1`, `DoSendReceive +0x50 ~ +0x54` |
| 인과 흐름 GUID | `+0x58` | 깃발 1 일 때 16 B | `+0x20a ~ +0x211` |
| **스텁 데이터** | **`+0x68`** | NDR 로 마샬한 입력 (view 일 때는 데이터 길이 u64) | `+0x2f3` |

- 서버가 응답에 **그대로 되돌려야 하는 것** = `+0x34` 호출 ID (2-3).

#### 2-2. 요청에 붙는 ALPC 속성

- 속성 칸 맨 앞 8 B = `{Allocated, Valid}`, 이어서 켜진 속성이 **Security (`0x80000000`) → DataView (`0x40000000`) → Context (`0x20000000`) → Handle → Token → Direct → WorkOnBehalf (`0x02000000`)** 순서. `RpcpAlpcpGetHeaderSize(마스크)` = 8 + 켜진 속성 크기 합 (Security · Handle · Token `0x18`, DataView · Context `0x20`, Direct · WorkOnBehalf 8).
- 보통 요청: Allocated `0xfe000000`, Valid **`0xa2000000`** (Security + Context + WorkOnBehalf). 큰 view 있으면 Valid 에 DataView 더함. "빠른" 길 (`[바인딩+0x178] & 0x40` 등) 은 Security 없이 `0x20000000` (`+0x34 ~ +0x85`). bind 는 Context 하나 (1-1).
- **Context 속성의 `MessageContext` 에 클라이언트가 자기 호출 객체 포인터를 넣는다.** 답이 올 때 이 값이 그대로 돌아와야 한다 — `IoCompleted` 가 받기 속성 Context 칸 (속성 칸 `+0x48`) 에서 호출 객체를 꺼내고 (`+0x1ae ~ +0x1b6` · `+0x4c0 ~ +0x4c8`), 0 이면 메시지를 버리며 (`+0x4cd` · `+0x4d0`), 객체 `+0xc` 에 `0x1000` 비트가 있어야 호출 객체로 친다 (`+0x1b6`). **에뮬레이터가 값을 지어내면 안 된다.**

#### 2-3. 응답 검사 — `UnpackResponse.asm` (동기 `SendReceive +0xa0` · 비동기 `HandleReply` 다 이 함수)

| 단계 | 검사 | 틀리면 | 줄 |
|---|---|---|---|
| 1 | `TotalLength ≥ 0x38` | **`0x6c0`** | `+0x2d` · `+0x31` |
| 2 | **`+0x28 == 3`** (응답) — 아니면 fault 갈래 | | `+0x37` |
| 3 (작은 응답, 깃발 4 없음) | 받기 `Valid` 에 DataView 없어야 · `TotalLength ≥ 0x40` · **`[호출+0x124] == [응답+0x34]`** | `0x6c0` | `+0x42 ~ +0x70` |
| 3c | 스텁 = **`응답 + 0x40`**, 길이 `TotalLength − 0x40` | — | `+0x7d` · `+0x8b` |
| 4 (큰 응답, 깃발 4) | DataView 있어야 (없으면 `0xe`) · 길이 · 호출 ID · `ViewSize` | `0xe` / `0x6c0` | `+0x11a ~` |
| 5 | 받기 `Handle` 비트가 켜져 있으면 시스템 핸들 복원 | 실패 기록 | `+0xa5` |
| fault (`+0x28 ≠ 3`) | 오류 = `+0x30` → `SanitizeErrorCodeFromServer`, `+0x34 & 1` 이면 `+0x38` 부터 확장 오류 정보 | 0 · 치명 14종 → `0x6c0`, 그 밖 → 그대로 (호출의 RPC 오류) | `+0x1ff` |

- **최소 응답** = `DataLength 0x18 + 스텁` · `TotalLength 0x40 + 스텁` · `+0x28` **3** · `+0x30` 0 · `+0x34` **요청 그대로** · 스텁 `+0x40` 부터. 받기 `Valid` 에 DataView 없음 · Context 있음.
- 종류 값 (`+0x28`): 0 요청 · 1 bind · 2 fault · 3 응답 · 6 콜백 (동기 `SendReceive` 가 `UnpackResponse` 앞에서 따로) · 10 · 11 (내부 바꿔 씀). 이름은 `[이름 추측]`, 값은 코드.
- **요청 쪽 `+0x30` 깃발과 응답 `+0x30` 의 뜻이 다르다** (요청 = 깃발, fault = 오류 코드). 응답 (3) 에선 `+0x30` 의 4 비트만 "큰 응답" 표시로 본다.

#### 2-4. 비동기 답이 오는 길 — `IoCompleted.asm` (winnsi · EPM 은 전부 이쪽)

1. 연결이 비동기면 `EnableAsync → CreateAlpc → TpAllocAlpcCompletionEx(포트, LrpcIoComplete, …)` (run129 6절). 완료 통지가 오면 `LrpcIoComplete +0xc3` 간접 호출로 `IoCompleted` 에 가고 (`[추측: 간접 대상]`), `IoCompleted +0xc8` 이 **받기만 하는** `NtAlpcSendWaitReceivePort(포트, Flags, NULL, NULL, 새 버퍼, &길이, &받기속성, NULL)` 을 부른다.
   - Flags = 완료 문맥이 있으면 **`0x1000000`** (그때 받기 버퍼 `MessageId` `+0x18` · `CallbackId` `+0x20` 에 문맥 값을 미리 씀 — `+0x277 ~ +0x285`), 없으면 0.
   - 버퍼 길이 = `IsPortWithSharedSection` 참이면 `0x1000`, 거짓이면 `0xff00` (`+0x6a ~ +0x76`). 받기 속성 Allocated = **`0xfa000000`**.
2. 받은 메시지 ALPC 종류 (`+0x04` 아래 바이트) == 2 이고 LRPC 종류 1 · 2 · 3 이면 `+0x21c` `HandleReply` 가상 호출 → `UnpackResponse` / `CompleteBind`.
3. 받기 호출이 오류면: `0xc0` → 버리고 끝, `0xc0000041` → 따로, **그 밖은 `RpcpReportFatalError(2, 상태)` = 치명 (`+0x4a6`, `+0x4ab` INT3)**. 에뮬레이터가 이 자리에서 모르는 오류를 돌려주면 프로세스가 죽는다.
4. 비동기 요청 (`AlpcSend +0x128`): `NtAlpcSendWaitReceivePort(포트, Flags, msg, attrs, 0, 0, 0, 0)` — Flags = `[호출+0x244] & 0x800000` 이면 **`0xc00000`** 아니면 0 (`+0x92` · `+0x9f` · `+0xa8` · `+0x11c`; 상수 이름 모름). 돌아오는 순간 답은 없다.

#### 2-5. ALPC 상태 → RPC 오류 (보내기 세 자리)

| NTSTATUS | bind (`Bind__4e330`) | 동기 요청 (`DoSendReceive`) | 비동기 요청 (`AlpcSend`) |
|---|---|---|---|
| `0` | 성공 (비동기 `0xc0021018`) | 성공 | 성공 |
| **`0xc0000008`** | **`0x6bf`** (`+0x823` 기본 착지) | **`0x6bf`** | **`0x6bf`** (`+0x297` · `+0x29c`) |
| `0xc0000037` | `0x6bf` | `0x6bf` | `0x6bf` |
| `0xc0000017` · `0xc0000044` · `0xc000009a` | `0xe` | `0xe` | `0xe` |
| `0xc0000022` | `0x6bf` | `0x6f7` | `0x6f7` |
| `0xc0000024` | `0x6bf` | `0x72c` | `0x72c` |
| `0xc0000036` | `0x72a` | `0x72a` | `0x72a` |
| `0xc00000a5` | `0x6bf` | `5` | `5` |
| `0xc0000701` | 취소됨이면 `0x71a`, 아니면 `0x6bf` | 취소됨이면 `0x71a`, 아니면 `0x6be` | `0x6be` |
| `0x102` | `0x71a` | `0x71a` | `0x71a` |
| 그 밖 | `0x6bf` | **`0x6be`** | **`0x6be`** (`+0x2e9`) |

- NTSTATUS 이름은 일반 이름 `[이름 추측]`. RPC 오류: `0x6ba` 1722 서버 없음 · `0x6be` 1726 · `0x6bf` 1727 (실행 안 되고 실패) · `0x6c0` 1728 프로토콜 오류 · `0x71a` 1818 취소.
- `RpcpStrictAlpcToRpcStatus` (rva `0x99d70`, 표 `0xed0d0` 15쌍) 는 `0xc0000008 → 6` 이고 **표에 없으면 치명** — 그러나 부르는 쪽이 서버 (`HandleBindRequest` · `ReplyWithSmallData` …) 라 클라이언트 길엔 안 쓰인다. 요청서가 말한 "핸들 0 → `0xc0000008`" 의 클라이언트 값은 **`6` 이 아니라 `0x6bf`**.
- **다시 시도**: `GetErrorClass` 가 `0x6ba` · `0x6bf` 를 "다시 시도 가능" 으로 (`+0x9 ~ +0x25`, `0x6be` 는 불가), `AttemptRetry` 는 카운터 `+0x2d8` < 4 · `+0x2da` < `0x101` 일 때만 (`+0x37 ~ +0x57`) → **첫 시도 + 최대 3번 다시**. 재시도 실행 길 (`HandleSendReceiveFailure → RetryCall → DriveStateForward → DoSendReceive`) 은 `[추측: 가상 호출 대상 안 확인]`, 실제로 도는지는 `[공백]` (동적 6).
- RPCSS 22 샘플: rpcrt4 는 `0x6bf` 를 돌려줄 뿐 — combase `+0x55b5a` 의 주소 `0x4` 읽기와의 연결은 rpcrt4 쪽에서 알 수 없다 (combase 몫).

### 3. `ept_map` 탑 (질문 3, run129 8절 ④) [정적]

#### 3-1. 질문 탑 — `TowerConstruct.asm` (`ResolveEndpoint__59d38 +0xba`)

- 인자: 인터페이스 ID = `[조회+0x44]`, 전송 문법 = `[조회+0x58]` (조회 객체 `+0x40 ~ +0xa0` 은 클라이언트 인터페이스 구조체 0x60 의 복사 → 인터페이스의 `TransferSyntax` = **NDR 2.0** v2.0, winnsi 도 `pefile` 로 확인), `"ncalrpc"`, 끝점 NULL.
- 꼴: u32 `tower_length` (끝점 없을 때 **`0x42`**) + u16 층 수 **4** + 층 넷:
  - 1층 — LHS `0x13` · 프로토콜 ID **`0x0d`** (UUID) · 인터페이스 GUID 16 B · 주 버전 u16 / RHS 2 · 부 버전 u16.
  - 2층 — LHS `0x13` · **`0x0d`** · 전송 문법 GUID (`8a885d04-…`) · 2 / RHS 2 · 0.
  - 3층 — LHS 1 · **`0x0c`** (ncalrpc; `0x0b` = ncacn_*, `0x0a` = ncadg_*) / RHS 2 · 0.
  - 4층 — LHS 1 · **`0x10`** / RHS 길이 **2** + `00 00` (끝점 없음; 있으면 ASCII + NUL).
- 총 **70 B** (4 + `0x42`). 끝점 문자열이 `0xffff` 를 넘으면 `0xe`, 프로토콜 문자열이 `ncalrpc` · `ncacn_` · `ncadg_` 가 아니면 `0x6a8`. 프로토콜 ID `0x0c` 이름만 `[이름 추측]`.
- 호출 (`LookupNextChunk +0x11d`): `Ndr64AsyncClientCall(프록시 0xe11e8, 프로시저 8, …, 바인딩, 객체 UUID, 질문 탑, SID NULL, 끝점 핸들 in/out, **max_towers 4** (`+0xe6`), &탑 개수, 탑 포인터 4개 배열, &상태)`. 동기는 프로시저 3 (SID 없음) / 7 (SID 있음). MS-RPCE EPM 번호 (`ept_map` 3 · `ept_map_auth` 7 · `ept_map_auth_async` 8) 와 맞아 보인다 `[추측: 이름 대응]`.

#### 3-2. 응답 검사 — `ProcessNextChunk.asm` (`LookupNotifyRoutine +0x69`)

| 검사 | 값 | 틀리면 | 줄 |
|---|---|---|---|
| 서버 상태 `[조회+0xa0]` | **0** | `0x6bb` → 3번 미만이면 같은 요청 다시 (`+0x3e8`); 그 밖 → `FatalLookupErrorEncountered` (DCE 상태 → `0x6d8` / `0x6d9` 등, run129 4절 표) | `+0x47` · `+0x53` |
| 탑 포인터 | NULL 아님 | `0x6c0` | `+0x93` · `+0x443` |
| 탑 길이 | `탑+4+tower_length > 탑+6` | `0x16c9a0cd` | `+0x99 ~ +0xa9` |
| 층 수 | **3 ~ 6** | 〃 | `+0xaf ~ +0xc0` |
| 1 · 2층 | 길이 칸만 걸어 넘어감 — **값은 안 봄** | 넘치면 〃 | `+0xc6 ~ +0x1b0` |
| 3층 프로토콜 ID | `0x0a` / `0x0b` → TCP 갈래, **`0x0c` → LRPC 갈래**, 그 밖 → 오류 | 〃 | `+0x1bf ~ +0x1d8` |
| LRPC 갈래 층 수 | **정확히 4** | 〃 | `+0x1e3` |
| 4층 RHS (끝점 이름) | u16 길이 **2 ~ 1000**, `끝` 안, **마지막 바이트 NUL** (ASCII). 4층 프로토콜 ID 는 **안 봄** | 〃 | `+0x23d ~ +0x26a` |
| 이름 저장 | ANSI → 와이드 (`RtlMultiByteToUnicodeN`) 해서 `[조회+0xc0+n*8]` | 메모리 `0xe` | `+0x2ea` · `+0x31b ~ +0x327` |
| 루프 뒤 | 끝점 핸들 out `[조회+0x2c8]` 이 **0** 이면 `0x6d9` ("더 없음"), 0 아니면 **다음 조각을 또 요청** | — | `+0x391` · `+0x39e` |
| `0x6d9` (`LookupNotifyRoutine`) | 이미 얻은 이름이 있으면 (`[+0xb4] != 0`) **성공 (0) 으로** 바꿈, 없으면 최종 `0x6d9` | | `+0x7c` · `+0x83` |

#### 3-3. 서버 흉내가 돌려줄 최소 탑 (끝점 `LRPC-emu` 예, 결과서 3-3)

- `tower_length = 0x40 + 끝점 길이 (NUL 포함)` → `LRPC-emu` 는 `0x49`, 총 77 B. 층 수 4 · 1층 nsi `7ea70bcf…` v1.0 · 2층 NDR 2.0 v2.0 · 3층 `0x0c` · 4층 `0x10` + RHS 길이 9 + `"LRPC-emu\0"`. 바이트 배열 그대로는 결과서 3-3.
- 클라이언트는 이 이름을 와이드로 바꿔 `\RPC Control\` + 이름 으로 두 번째 `NtAlpcConnectPort` 를 한다 (run129 4절, `TransformLrpcEndpointToAlpcPortName` — sast/127). **이름 모양은 안 본다** (길이 2 ~ 1000 + NUL 만) → 흉내가 마음대로 고를 수 있다.
- 1 · 2층 값 · 길이 칸은 `TowerConstruct` 가 쓰는 꼴 그대로 두면 안전 — 진짜 epmapper 가 등록 때 받은 탑을 그대로 돌려준다고 짐작 `[추측]`.
- **NDR 틀 (`entry_handle` 20 B → 개수 → 탑 배열 → 상태) 은 안 읽었다 `[공백]`** — 공개 EPM IDL 순서로 짐작 `[추측]`, 프로시저 8 형식 문자열 (프록시 `0xe11e8` → 형식 `0xefeb2`) 은 안 풂 (Q246). 끝점 핸들을 NULL (20 B 0) 로 주면 한 번에 끝난다.

### 4. nsisvc 서버 (질문 4, run129 8절 ⑦ · ⑧) [정적]

#### 4-1. 시작 · 끝점 — `ServiceMain.asm` · `NsiStartRpcServer.asm`

- `ServiceMain +0xed NsiServiceInitialize` → **`+0xf6 NsiStartRpcServer`** → `+0x166 NsiServiceNotifyStart` (`RtlPublishWnfStateData`).
- `NsiStartRpcServer`: `+0xac` 보안 설명자 (문자열 SDDL — `WD` · `RC` 읽기쓰기실행, `BA` · `OW` 모두, AppContainer 셋 · 기능 SID 하나 읽기; 원문은 결과서 4-1) → **`+0x123 RpcServerUseProtseqW(L"ncalrpc", 10, SD)` — 끝점 인자 없음 = 동적 끝점** → `+0x1a1 RpcServerRegisterIf3(&인터페이스 0x6420, …, Flags 9, MaxCalls 1234, 0, NsiSecurityCallBack, SD)` (Flags 9 = `AUTOLISTEN` + `ALLOW_SECURE_ONLY` `[이름 추측]`) → 정리 이벤트 · 대기 (`+0x1fb` · `+0x28a`) → `+0x2de RpcServerInqBindings` → **`+0x337 RpcEpRegisterW(&인터페이스, 벡터, NULL, L"NSI server endpoint")`** → 성공이면 전역 `RpcServerInitialized = 1`.
- 동적 끝점 이름 = rpcrt4 `LRPC_ADDRESS::ServerSetupAddress` (서버): `L"LRPC-"` (rva `0xeced8`, `+0xc4` · `+0xd1`) + `+0xe8 ProcessPrng` 9 B 를 소문자 16진 **18자** → 23자. 이미 있으면 (`0x6cc`) 다시.
- 서버 인터페이스 (rva `0x6420`): `7ea70bcf…` v1.0, NDR 2.0, 깃발 `0x6000000`, 기본 끝점 0개. 디스패치 표 (`0x64f0`, 9개): 프로시저 0 ~ 6 · 8 = `NdrServerCall2` (동기), **7 = `NdrAsyncServerCall` (비동기)**. 서버 함수: 5 = `RpcNsiRegisterChangeNotification` (`0x27c0`), 6 = `RpcNsiDeregisterChangeNotification` (`0x16f0`, 안 읽음), 7 = `RpcNsiRequestChangeNotification` (`0x1010`).
- **인터페이스 콜백** `NsiSecurityCallBack` (`0x2e80`): `+0x5a RpcServerInqCallAttributesW` 실패면 그 오류, 성공이면 `AuthenticationLevel` (`[RSP+0x48]`) **< 6 → `5`**, ≥ 6 → 0 (`+0x83` · `+0x88` · `+0x8a`). 결과서: 디컴파일이 이 검사를 빠뜨려 `.asm` 으로 읽음. 클라이언트 (`RpcBindingSetAuthInfoW(…, 6, 10)`, run129) 가 수준을 서버에 어떻게 알리는지는 `[공백]` — bind · 요청 머리엔 인증 칸이 없고, ALPC Security 속성 (요청 `Valid` 에 늘 있음) 으로 간다고 짐작 `[추측]`. 흉내는 "허용" 으로 두면 된다.

#### 4-2. 프로시저 5 — `RpcNsiRegisterChangeNotification.asm`

| 단계 | 하는 일 | 실패값 | 줄 |
|---|---|---|---|
| 0 | `*컨텍스트 = 0` | | `+0x5c` |
| 1 | `RpcServerInqCallAttributesW(0, {Version 2, Flags 0x10 = PID})` | 그 오류 | `+0x88` |
| 2 | **`KernelModeCaller` (속성 `+0x34`) == 0** | **`0x490`** | `+0x9e` · `+0xa2` · `+0x33b` |
| 3 | 입력 24 B + compartment + PID 를 모음 — **내용 검사 없음** | | `+0xa8 ~ +0xe3` |
| 4 | `MALLOC(0x98)` | **`8`** | `+0xe6` · `+0x345` |
| 5 | `RpcImpersonateClient` | 그 오류 | `+0x12c` |
| 6 | **`nsi!NsiRegisterChangeNotificationEx`** → 0 = 성공, 출력 = nsi 쪽 커널 등록 핸들 | 그 값 (Win32 오류) | `+0x147` · `+0x153` · `+0x155 RpcRevertToSelf` |
| 7 | 블록 채움 (`+0x14` PID · `+0x58` 입력 24 B · `+0x70` 모듈 ID 24 B · `+0x88` compartment · `+0x90` nsi 핸들) | | `+0x169 ~ +0x1ba` |
| 8 | 목록 잠금 + **`RpcServerInitialized == 0` 이면** 블록 해제 + **`0x139f`** | `0x139f` | `+0x1e4` · `+0x365` |
| 9 | 전역 목록에 잇고 **`*컨텍스트 = 블록 포인터`**, **반환 0** | | `+0x228` · `+0x22a` |

- 컨텍스트 핸들 = 0x98 B 블록 포인터. 선 위로는 NDR 이 20 B (`u32 속성 0` + GUID) 로 싸서 보낸다 — 클라이언트는 그 20 B 를 해석 없이 되돌려 보낼 뿐 (run129 3절). 흉내는 0 이 아닌 20 B 면 된다 `[추측]`.
- **흉내가 같은 대답을 내려면**: 입력 값은 안 봐도 된다 (nsisvc 가 안 봄; 커널 NSI 가 봄). 성공 대답 = **{컨텍스트 핸들 20 B, 반환 0}**. 입력이 틀린 경우의 대답은 nsi.dll → 커널 NSI 가 주는 Win32 오류라 `[공백]` (Q246).

#### 4-3. 프로시저 7 — `RpcNsiRequestChangeNotification.asm`

- 먼저 `+0x30 RpcImpersonateClient`. 컨텍스트 NULL → `0x57`; **`RpcServerInitialized == 0` 이거나 블록 `+0x10 == 1` (닫힘) → `0x4c7`** (`+0x7d` · `+0x18b`); 이미 걸린 호출 (`+0x30 != 0`) → `0x57` (`+0x87`).
- 그 밖에 **대기 중인 변경이 없으면** 비동기 상태 · 출력 포인터를 블록 (`+0x30 ~ +0x50`) 에 두고 `+0xdf RpcServerSubscribeForNotification(…, NsipClientDisconnectCallback)` 만 건 채 **`RpcAsyncCompleteCall` 없이 돌아간다**. 변경이 쌓여 있으면 하나 꺼내 바로 완료 (`+0x19b` · `+0x256` · `+0x3fb`).
- → run129 8절 ⑧ "프로시저 7 은 답하지 않는다" 가 **서버 코드와 맞다**.

### 5. 서버 흉내 차례 — run129 8절 표를 채운 판 (결과서 5절)

| 단계 | 클라이언트가 보내는 것 | 서버 흉내가 돌려줄 것 | 근거 |
|---|---|---|---|
| ① epmapper 연결 | `NtAlpcConnectPort(\RPC Control\epmapper)`, 들어오는 속성 = 빈 8 B 버퍼 | 핸들 쓰고 0, 빈 버퍼 `ValidAttributes` 0. 비동기면 이어서 `NtAlpcSetInformation` (`TpAllocAlpcCompletionEx` 안) | run129 5절 |
| ② 핸들 질문 | `NtAlpcQueryInformation(핸들, 0, 0x10)` | 오류여도 됨 (→ 받기 버퍼 `0x1000`) | run129 8절 |
| **③ epmapper bind** | 비동기: Flags **0**, bind 0x70 B (`+0x28` 1 · `+0x34` **`e1af8308…` v3.0** · **`+0x48` 3** · 슬롯 둘 · `+0x54` `0x2000000`), 속성 Context 하나 (`MessageContext` = BIND_CCALL\*), 받기 칸 NULL | 보내기 **즉시 0** → 완료 통지 → 받기 (Flags 0 / `0x1000000`, 버퍼 `0x1000`, 속성 `0xfa000000`) 에 **ALPC 종류 2 · `+0x28` 1 · `+0x48` = 1 또는 2 · `+0x08` PID · Context `MessageContext` 그대로**. 길이 검사 없음 | 1절 |
| **④ `ept_map`** | 요청 머리 `+0x28` 0 · `+0x34` 호출 ID · `+0x38` ③ 슬롯 · **`+0x3c` 8**, 스텁 `+0x68` (질문 탑 70 B 포함 `[추측: NDR 틀]`), Flags 0 / `0xc00000`, 속성 Valid `0xa2000000` | 보내기 즉시 0 → 완료 통지 + 받기에 **응답**: 종류 2 · `+0x28` 3 · `+0x30` 0 · **`+0x34` 요청 그대로** · `TotalLength 0x40 + 스텁` · 스텁 `+0x40` = 상태 0 + 탑 1개 (3-3) + 끝점 핸들 NULL, DataView 없음 · Context 있음 | 2 · 3절 |
| ⑤ 두 번째 연결 | `\RPC Control\<4층 이름>` | ① 과 같음 | run129 |
| **⑥ nsi bind** | ③ 과 같은 꼴, `+0x34` **`7ea70bcf…` v1.0**, `+0x48` 3 | ③ 과 같은 꼴 | 1절 |
| **⑦ 프로시저 5** | `+0x3c` 5, `+0x38` ⑥ 슬롯, 스텁 = 입력 24 B + 모듈 ID 24 B + compartment 8 B (`[추측: NDR 본문]`) | 응답 `+0x28` 3 · `+0x34` 그대로 · 스텁 = **컨텍스트 핸들 20 B + DWORD 0** (정렬로 24 B `[추측]`) | 4-2 |
| ⑧ 프로시저 7 | `+0x3c` 7, 스텁 = 컨텍스트 핸들 20 B | **답하지 않음** (서버도 걸어 둠) | 4-3 |
| ⑨ 성공 뒤 | — | 가짜로 답할 것 아님 (run129 그대로) | run129 |

- 남은 `[공백]`: NDR 스텁 본문의 정확한 바이트 (④ · ⑦), 서버가 ③ · ⑥ 에서 고르는 문법.

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖. 위쪽 사슬 winnsi → DriveStateForward 는 run129 4절)

[bind]
LRPC_BIND_CCALL::Bind +0x200 · BaseBind +0x1e9 · NegotiateTransferSyntax +0x76e
 └─ LRPC_CASSOCIATION::Bind (0x4e330)
      ├─ +0x476  BCACHE::Allocate(0x1000)
      ├─ +0x62d  NtAlpcSendWaitReceivePort   bind 0x70 B · Flags 동기 0x20000 / 비동기 0 · 속성 Context 하나
      ├─ (동기)   +0x6b2 MessageReceived → +0x6d0 CompleteBind (+0xf5 SanitizeErrorCodeFromServer)
      │           오류 → +0x875 AbortAssociation
      └─ (비동기) +0x7d5 BCACHE::Free → 0xc0021018 (보류)

[요청]
LRPC_CCALL::SendReceive (0x3fc00)                                    동기
 ├─ +0x76  DoSendReceive ├─ +0x4d  InitializeRequestAndAttributes   머리 · 속성
 │                       └─ +0x154 NtAlpcSendWaitReceivePort        0x20000 · 같은 버퍼
 └─ +0xa5  UnpackResponse
DoAsyncSend (0x41f80)                                                비동기
 ├─ +0x73  InitializeRequestAndAttributes
 └─ +0x8c  AlpcNonPipeSend → +0x30 AlpcSend → +0x12f NtAlpcSendWaitReceivePort   받기 칸 0

[비동기 답]
LrpcIoComplete +0xc8 ⇢ IoCompleted (0x57800)
 ├─ +0xcf   NtAlpcSendWaitReceivePort   받기만 · Flags 0 / 0x1000000 · 속성 0xfa000000
 ├─ +0x221  ⇢ HandleReply (가상 +0x130)   ALPC 종류 2 · LRPC 1 / 2 / 3
 │           HandleReply__41bb0 +0x64 / __417f0 +0x82  UnpackResponse
 │           HandleReply__41bb0 +0xe4 / __417f0 +0x102 CompleteBind
 └─ +0x4ab  RpcpReportFatalError        받기 오류 (0xc0 · 0xc0000041 밖) → 치명

[ept_map]
EP_LOOKUP_DATA::ResolveEndpoint (0x59d38) +0xbf TowerConstruct      질문 탑 4층 70 B
LookupNextChunk +0x122 Ndr64AsyncClientCall                          EPM 프로시저 8 · max_towers 4
LookupNotifyRoutine ├─ +0x52 RpcAsyncCompleteCall
                    ├─ +0x6e ProcessNextChunk                       층 검사 · 이름 저장
                    └─ +0xfe / +0x189 FatalLookupErrorEncountered
ResolveEndpointComplete +0x41 ReturnLookupValues

[nsisvc 서버]
nsisvc!ServiceMain ├─ +0xf2  NsiServiceInitialize
                   ├─ +0xfb  NsiStartRpcServer ├─ +0x12a RpcServerUseProtseqW("ncalrpc", 10)  ⇒ rpcrt4 ServerSetupAddress +0xef ProcessPrng  "LRPC-" + 18자
                   │                           ├─ +0x1a8 RpcServerRegisterIf3(Flags 9, 1234, NsiSecurityCallBack)
                   │                           ├─ +0x2e5 RpcServerInqBindings
                   │                           └─ +0x33e RpcEpRegisterW("NSI server endpoint")
                   └─ +0x16b NsiServiceNotifyStart
NsiSecurityCallBack +0x61 RpcServerInqCallAttributesW               수준 < 6 → 5
RpcNsiRegisterChangeNotification (프로시저 5)
 ├─ +0x8f  RpcServerInqCallAttributesW   커널 모드 호출자 → 0x490
 ├─ +0xeb  MALLOC(0x98) · +0x133 RpcImpersonateClient
 ├─ +0x14e nsi!NsiRegisterChangeNotificationEx   0 아니면 그 값
 └─ +0x15c RpcRevertToSelf → (RpcServerInitialized 0 → +0x36a 해제 · 0x139f) → *컨텍스트 = 블록 · 0
RpcNsiRequestChangeNotification (프로시저 7)
 ├─ +0x37  RpcImpersonateClient
 ├─ +0xe6  RpcServerSubscribeForNotification   변경 없음 → 걸어 둔 채 돌아감
 └─ +0x1a2 · +0x25d · +0x402 RpcAsyncCompleteCall   (오류 · 변경 있을 때만)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). `⇢` 는 간접 · 가상 호출, `⇒` 는 rpcrt4 서버 런타임 안을 건너뜀 (`RpcServerUseProtseqW` → `ServerSetupAddress` 사이는 안 읽음).

## 확인된 것

- `[정적]` bind 메시지 = 0x70 B, `+0x28` 1, `+0x34` 인터페이스, `+0x48` 제안 마스크 (1 / 2 / 4), `+0x4c ~ +0x50` 슬롯, `+0x54` 깃발 (`Bind__4e330 +0x48b ~ +0x8b0`). winnsi · EPM 마스크 3.
- `[정적]` bind `NtAlpcSendWaitReceivePort` (`+0x626`) — 동기 Flags `0x20000` + 같은 버퍼, 비동기 Flags 0 + 받기 칸 NULL + 보내자마자 버퍼 풂.
- `[정적]` `CompleteBind` 는 `+0x28` · `+0x48` · `+0x08` 만 본다. 길이 검사 없음. 마스크 0 → `0x6c2`, 부분집합 아님 · 둘 이상 → `0x6c0`.
- `[정적]` 요청 머리 = 종류 0 · `+0x34` 호출 ID · `+0x38` 슬롯 · `+0x3c` 프로시저 · 스텁 `+0x68` (`InitializeRequestAndAttributes +0x1ab ~ +0x2f3`). 동기 · 비동기가 같은 함수.
- `[정적]` 응답 = `TotalLength ≥ 0x38` · `+0x28 == 3` · 호출 ID 맞음 · 스텁 `+0x40` (`UnpackResponse +0x2d ~ +0x95`). 아니면 fault (`+0x30`).
- `[정적]` Context 속성 `MessageContext` = 클라이언트 호출 객체 포인터. 답에 그대로 돌아와야 하고 0 이면 버림 (`IoCompleted +0x4cd`).
- `[정적]` 비동기 답 = `IoCompleted +0xc8` 의 받기만 하는 부름 (Flags 0 / `0x1000000`, 속성 `0xfa000000`). 받기 오류가 `0xc0` · `0xc0000041` 밖이면 `RpcpReportFatalError` 치명 (`+0x4a6`).
- `[정적]` `0xc0000008` → bind · 동기 · 비동기 셋 다 `0x6bf`. 엄격 표 `RpcpStrictAlpcToRpcStatus` 는 서버 쪽만.
- `[정적]` `0x6ba` · `0x6bf` 는 재시도 종류, 상한 첫 시도 + 3 (`GetErrorClass` · `AttemptRetry`).
- `[정적]` 질문 탑 4층 70 B. 응답에서 보는 것은 층 수 · 3층 ID · 4층 RHS (길이 · NUL) 뿐 (`ProcessNextChunk +0xaf ~ +0x26a`). 끝점 핸들 0 이면 `0x6d9`, 이름이 하나라도 있으면 성공 (`LookupNotifyRoutine +0x7c` · `+0x83`).
- `[정적]` nsisvc 끝점 = 동적 (`RpcServerUseProtseqW` 끝점 없음, `+0x123`) + `RpcEpRegisterW` (`+0x337`). 이름 = `LRPC-` + 18자 (`ServerSetupAddress +0xc4` · `+0xe8`).
- `[정적]` 프로시저 5 는 입력 값을 안 보고 `0x490` · `8` · `0x139f` · 흉내내기 · nsi 오류만. 성공 = 블록 포인터 + 0 (`+0x22a` · `+0x228`).
- `[정적]` 프로시저 7 은 변경이 없으면 완료하지 않음 (`RpcNsiRequestChangeNotification +0xdf` 뒤 `RpcAsyncCompleteCall` 없음).
- `[정적]` nsisvc PDB `CE94A7C9096AB626B28E58C48D6605591`, 함수 77.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 1-3 "동기 = `Bind__4e330 +0x6aa MessageReceived`" — `+0x6aa` 는 인자 (`MOV RCX,R13`), call 은 **`+0x6ad`** (ret `+0x6b2`).
- 결과서 1-1 "`BaseBind +0x1bc` (ret `+0x1e9`)" — `+0x1bc` 는 비동기 깃발을 넘기는 줄 (`MOV R8D,R12D`), call 은 **`+0x1e4`** (ret `+0x1e9`).
- (보탬) 결과서 2-4 는 "완료 통지가 오면 `IoCompleted` 가 …" 까지만 — `_calls.tsv` 로 보면 `LrpcIoComplete` 에서 `IoCompleted` 로 가는 줄은 **간접 호출 하나** (`+0xc3` `[R14]`, ret `+0xc8`). 그 대상이 `IoCompleted` 인지는 `[추측]` (동적 4 의 `kv` 로 확인).
- (보탬) 요청을 쓰는 함수까지의 사슬: 동기 `SendReceive__3fc00 +0x71 DoSendReceive → +0x48 InitializeRequestAndAttributes`, 비동기 `DoAsyncSend +0x6e InitializeRequestAndAttributes · +0x87 AlpcNonPipeSend → +0x2b AlpcSend`. bind 답 쪽: `HandleReply__41bb0 +0xdf` · `__417f0 +0xfd` 가 `CompleteBind`.
- (보탬) `DoAsyncSend +0x3ee` 에도 `RpcpReportFatalError` 부름이 있다 (ret `+0x3f3`). 어떤 조건인지는 안 읽음 `[공백]`.

## 정정

- **run129** (`runs/03_sprint/run129-winnsi-rpc.md`) — **일부 닫힘 + 고쳐 읽기 하나**. 맨 위에 한 줄:
  - **닫힘**: 8절 표 ③ (epmapper bind) · ④ (`ept_map` 탑 층 검사) · ⑥ (nsi bind) 의 `[공백]` → Q230 과 "nsisvc 가 프로시저 5 입력을 어떻게 검사하는지 `[공백]`" → 이 문서 1 ~ 5절. "아직 확인 필요" 정적 1 · 2 · 3 (→ Q230) 닫힘.
  - **고쳐 읽기**: sast/127 결과서 2-3 단계 8 은 EPM 인터페이스를 "전송 문법 NDR 2.0" 하나로 적었다 — **EPM 도 전송 문법 둘** (NDR 2.0 · NDR64, rpcrt4 문법 표 rva `0xe7560` `nCount 2`) 이고 bind 제안 마스크는 **3** 이다 (결과서 1-2 · 5절). run129 문서는 8절 ③ 에 "전송 문법" 이라고만 써서 글이 틀린 곳은 없지만, 그 단계를 sast/127 2-3 으로 읽으면 틀린다. 질문 탑 2층의 문법은 인터페이스 구조체의 `TransferSyntax` (NDR 2.0) 하나라 따로다 (3-1).
  - 그대로 맞는 것: 8절 ④ 의 "비동기 프로시저 8" (요청 머리 `+0x3c` 에 8 이 간다), ⑦ "컨텍스트 핸들 (선 위 20 B) + 0", ⑧ "답하지 않는다".
- sast/127 결과서는 `_workspace/` 중간 산출물이라 고치지 않는다 (결과서 sast/130 5절 맨 위가 바로잡음을 적었다).
- **run132** (`runs/03_sprint/run132-npfs-pipes.md`) — **오프셋 하나 정정** (이번 바퀴 내용과는 별개, 오케스트레이터 요청). 이어 쓰는 앞 run 줄 · 2(a) 끝 · "아직 확인 필요" 끝 · flow.md 4장 run132 정적 줄이 파이프 이름 앞 SD 를 `smss!SmpCreateProtectedPrefixes +0xab3` 으로 가리켰는데, run98 호출 지점 표 (`+0xa44` · `+0xab3` = `\Device\NamedPipe\` · `\Device\mailslot\`) 와 나무 기준으로 **파이프 쪽은 `+0xa44`** (ret `+0xa4b`) 다. `+0xab3` 은 메일슬롯. 세 곳 · flow.md 한 곳을 `+0xa44` 로 고치고 run132 맨 위에 "고쳐 읽기 (run133)" 한 줄. "npfs 루트 SD 가 이 줄 것인지" 는 그대로 `[추측]`. 해설서는 오프셋을 안 써서 그대로.

## PE_emulator 쪽 (결과서 6절 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

결과서는 `ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp` 가 **sast/127 때와 바뀌어** 줄 번호 · 내용이 다르다고 적었다 (읽은 시점 기준).

| 줄 (결과서) | 판정 | 까닭 |
|---|---|---|
| `:190 ~ :192` (`NtAlpcConnectPort`) | **"서버 없음" 흉내로는 맞다** | 이미 `0xc0000034` 로 돌려보내게 고쳐져 있음 (run129 T1 과 맞음, → `0x6ba`). 다만 `live_server_required = a[5]…a[9]` 가 rpcrt4 의 **늘 비-NULL 인 빈 8 B `a[9]`** 때문에 **늘 참** → 서버 모델을 둔 이름도 `0xc0000034` 로만 간다. T2 를 하려면 `a[9]` 의 `AllocatedAttributes == 0` 이면 속성 없음으로 보고, 서버 모델 이름 (`\RPC Control\epmapper` · `\RPC Control\LRPC-…`) 은 핸들 쓰고 성공 |
| `:305 ~ :307` (`NtAlpcConnectPortEx`) | 같은 이야기 | Ex 길은 `gDefaultSecurityDescriptor` 가 있을 때만 (run129) — 요청 쪽 로그가 plain 이라 T2 엔 해당 없음 |
| `:346` (`alpc_server_accept_unsupported`) | 영향 없음 | 샘플이 직접 만든 포트에 붙을 때만 |
| `:602` (`NtAlpcSetInformation`) | **비동기 길에서 멈출 것** | `kMessageZoneInformation` 밖이면 `stop` — `TpAllocAlpcCompletionEx` 가 다른 클래스 (완료 포트 연결로 짐작 `[추측]`) 로 부를 것. ntdll 쪽은 안 읽음 (Q232 ① · Q246) |
| `:612 ~ :626` (`NtAlpcSendWaitReceivePort`, 요청서의 `:623`) | **서버 흉내를 넣을 자리가 맞다** | 인자 꼴 (`a[0]` 핸들 · `a[1]` Flags · `a[2]` 보낼 · `a[3]` 보낼 속성 · `a[4]` 받을 · `a[5]` 길이 · `a[6]` 받을 속성 · `a[7]` 타임아웃) 이 rpcrt4 부름과 같음 |

`:612 ~ :626` 에 넣을 것 (결과서 6절, 이번에 닫힌 만큼):
1. 보냄 (`a[2] != 0`) 인 호출: `Flags & 0x20000` → 동기 (같은 버퍼에 답을 덮어쓰고 `a[6]` 갱신, 0). 아니면 비동기 → **즉시 0**, 답은 나중.
2. 보낸 메시지: `+0x28` 1 = bind (`+0x34` UUID 로 EPM / nsi 가름, `+0x48` 비트 하나를 골라 씀), 0 = 요청 (`+0x3c` 프로시저 · `+0x34` 호출 ID 보관).
3. 비동기 답 배달: 완료 통지를 올려 클라이언트 `IoCompleted` 가 받기만 하는 둘째 부름 (`a[2] == 0`, `a[4]` 새 버퍼, `a[5]` `0x1000` / `0xff00`, `a[6]` Allocated `0xfa000000`, `a[1]` 0 / `0x1000000`) 을 하게 하고, 그 부름에 쌓인 답 (ALPC 종류 2, `MessageId` / `CallbackId` = 완료 문맥 것, Context `MessageContext` = 요청 때 받은 값) 을 준다. **받기 부름에 모르는 오류를 주면 치명** — "쌓인 답 없음" 일 때 무엇을 돌려줘야 하는지는 `[공백]` (동적).
4. 답 `Valid`: Context 켜고, DataView · Handle 끄기.
5. 오류를 주고 싶으면 2-5 표의 NTSTATUS 만. 대화 오류는 "응답 대신 `+0x28` 2 fault + `+0x30` RPC 오류" 가 가장 안전.
6. T1 (서버 모델 없는 포트 `0xc0000034` → `0x6ba`) 은 그대로 — 다만 **재시도 종류라 한 호출에서 연결 요청이 최대 4번** 올 수 있다 (실제로 도는지는 동적 6).

- **공통 몫** (LSA `GetUserName*` · RPCSS `CoInitializeSecurity` 도 같음): rpcrt4 LRPC 클라이언트 전부 — 연결 인자 · 상태 표 (run129 5절), bind (1절), 요청 · 응답 머리 (2-1 · 2-3), 속성 규칙 (2-2), ALPC 상태 → RPC 오류 (2-5), 재시도, 비동기 답 배달 (2-4). 다른 점: 고정 끝점 서비스는 `ept_map` 단계가 없을 수 있고, 동기 호출이면 `Flags 0x20000` + 같은 버퍼 갈래.
- **winnsi / epmapper 몫**: 인터페이스 둘의 UUID · 버전 · 마스크 3 (1-2), `ept_map` 탑 (3절), nsi 프로시저 5 · 7 (4절), 등록 최대 4번 (run129).

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 새 번호 없이 **Q231 (⑤ 와 같은 세션)** 에 출처로 붙임. nsisvc 가 도는 VM + winnsi 등록을 부르는 프로그램 — test.exe 는 안 부름, 고르기는 사용자 결정. 유저 멈춤점, test.exe 가 아닌 그 프로세스 조건, 모두 ret 자리. `ntdll!NtAlpcSendWaitReceivePort` 입구에 "반환 주소 = rpcrt4 + 아래 ret" 조건을 걸어도 같은 값):
1. **bind 진짜 바이트 · 동기냐 비동기냐** — `rpcrt4!LRPC_CASSOCIATION::Bind+0x62d` (call `+0x626`). 입구에서 `edx` = Flags (0 = 비동기 / `0x20000` = 동기), `db @r8 L0x70`, `dq @r9 L5`, 스택 `[rsp+0x28]` · `[rsp+0x38]` · `[rsp+0x40]` (비동기면 0). `+0x34` UUID 로 EPM / nsi 가름.
2. **서버가 고른 전송 문법** — `rpcrt4!LRPC_CASSOCIATION::CompleteBind` 입구 `db @rdx L0x70` 의 `+0x48` (1 / 2) · `+0x08` · `+0x28` · `+0x30`.
3. **요청 머리 진짜 바이트** — `rpcrt4!DoSendReceive+0x154` (동기, `rdi` 메시지) / `rpcrt4!LRPC_BASE_CCALL::AlpcSend+0x12f` (비동기, `rbp` 메시지 · `r14` 속성 · `esi` Flags 0 / `0xc00000`). `+0x28 +0x30 +0x34 +0x38 +0x3c`, `+0x68` 부터 스텁.
4. **응답 · `ept_map` NDR 본문** — `rpcrt4!LRPC_CASSOCIATION::IoCompleted+0xcf` `db @rbx L0x90`, 받기 속성 `dd @rbp-0x59 L8`, Context `MessageContext` `dq @rbp-0x11 L1` (요청 때 보낸 호출 객체와 같아야). 3-3 의 NDR 틀 `[공백]` 을 닫음. 그 프레임의 `kv` 로 `LrpcIoComplete+0xc8` 이 부른 쪽인지 (flow-writer 대조 셋째).
5. **프로시저 5 요청 · 응답 본문** — 3 · 4 와 같은 자리 (nsi 호출일 때): 요청 `+0x68` 부터 · 응답 `+0x40` 부터.
6. **재시도가 도는지** — `rpcrt4!LRPC_BIND_CCALL::AttemptRetry` 입구 `[rcx+0x2d8]` 카운터, `AlpcConnect+0x29a` 몇 번 오나 (서버 없음 `0x6ba` · 핸들 0 `0x6bf`).
7. plain 인지 Ex 인지 — `AlpcConnect+0x27a` `rdx` (= Q231 ④, run129 이월).

정적 (결과서 "다음 요청 후보" — 대기열 **Q246**):
1. 서버 쪽 rpcrt4 (`LRPC_ADDRESS`) `HandleBindRequest` · `ReplyWithSmallData` · `SendReply` — 진짜 서버가 bind 답 · 응답의 "클라이언트가 안 보는 칸" (`+0x30` · `+0x38 ~ +0x3f`) 을 무엇으로 채우나.
2. `ntdll!TpAllocAlpcCompletionEx` → `NtAlpcSetInformation` (완료 포트 연결) + 통지가 `LrpcIoComplete` 까지 오는 길 (Q232 ① 과 겹침, run125 일꾼 공장 이음) — 비동기 답 배달의 나머지 반쪽. 서버 흉내를 **지금 코드 수준에서 돌게** 하려면 이게 가장 큰 덩어리 (결과서 판정).
3. NDR 형식 문자열 — EPM 프로시저 8 (프록시 `0xe11e8` → `0xefeb2`) · nsi 프로시저 5 · 6 · 7 (winnsi `0x66e0` · `0x6590` · `0x6360`, nsisvc 서버 정보 `0x6600`) 의 입출력 바이트 순서 · 정렬.
4. `nsi!NsiRegisterChangeNotificationEx` (rva `0x1eb0`) → `\Device\Nsi` IOCTL + 오류 값 (4-2 `[공백]`).
5. `NegotiateTransferSyntax` (rva `0x43a20`) — bind 답 뒤 NDR 2.0 쪽인가 NDR64 쪽인가 (1-3 "둘 다 가능" 좁히기).
6. `RegisterNsiSvcRestartNotification` WNF 이름 (Q232 ② 와 겹침).

- 그 밖 `[공백]`: 클라이언트 인증 수준 (6) 이 서버 `AuthenticationLevel` 로 가는 길 (4-1), 프로시저 6 (`RpcNsiDeregisterChangeNotification`) 본문, 비동기 받기에 "쌓인 답 없음" 일 때의 정상 status, `DoAsyncSend +0x3ee` 치명 조건.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: `_calls.tsv` 는 같은 이름 함수의 호출을 한 묶음에 섞는다 (예: `AlpcSend +0xdc` 는 서버 `AlpcSend__56d44`, `+0x128` 이 클라이언트 `AlpcSend.asm`). 오프셋은 `.asm` 에서 직접 확인할 것 — run128 · run129 와 같은 메모, 기록만 (하네스 제안 아님).
- 결과서가 "call 자리" 대신 그 앞 인자 줄 (`+0x6aa` · `+0x1bc`) 을 적은 곳이 둘 있었다 (위 "flow-writer 대조"). 표기 규칙 ("부르기는 call 자리") 은 이미 있으니 하네스 제안 아님.
