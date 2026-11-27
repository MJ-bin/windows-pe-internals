# 2026-11-26 (작성일, 정적 분석 2026-11-26) run149 — COM 이 RPCSS 에 거는 첫 전화: combase `Connect` 의 bind · 첫 프로시저 · 응답 검사 · 그다음 부름 (Q257a, 정적만): combase 는 바인딩 글 **`ncalrpc:[epmapper,Security=Impersonation Dynamic False]`** 로 끝점을 박아 넣어 **`ept_map` 을 안 묻고** `\RPC Control\epmapper` 로 곧장 간다. 한 번의 `GetConnection` 안에서 **bind 가 네 번** — `ILocalObjectExporter` (`e60c73e6-88f9-11cf-9af1-0020af6e72f4` v2.0, PDB 이름으로 확정) → `IMachineActivatorControl` → `IROT` → `ISCM` — 모두 동기 (`RpcBindingBind(NULL, …)`), 전송 문법 NDR 2.0 하나. 하나라도 fault 면 거기서 `0x8007NNNN` 으로 끝난다. 첫 프로시저는 **`ILocalObjectExporter` 0 번 = `Connect`** (스텁 `NdrClientCall2`, 입력 6 · 출력 25 · 상태). 상태 0 이어도 **컨텍스트 핸들 · 로컬 해결사 DUALSTRINGARRAY 포인터 · rpcss PID · 프로세스 서명 가운데 하나라도 0 / NULL 이면 `0x8000ffff`**. 실패 쪽은 **`RetryRPC` 가 상태 `0x6bb` 일 때만 100 ms 쉬고 끝없이 다시 걸고**, 그 밖 양수 N 은 `CheckStatus` 가 `0x80070000 | N` 으로 바꿔 깨끗이 끝낸다. 진짜에서 이 인터페이스는 **rpcss.dll** 이 받고, `epmapper` 끝점은 rpcss.dll 과 RpcEpMap.dll 이 같은 표 칸 (16) 으로 함께 연다

> **정정 · 고쳐 읽기 (run151, sast/148 — Q288) (a) SD 크기 0**: 2-2 응답 칸 표 칸 28 줄 (원래 `:201`, 지금 `:205`) 의 "가능 (라) · 0 ×5" 와 "최소 대답" 3 (원래 `:315`, 지금 `:319`) 의 "SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL" 은 **틀렸다** `[정적]` — (라) 는 combase `InitializeSystemSecurityDescriptors` **안에서만** 맞다. 뒤에서 `CoInitializeSecurity +0x385 GetDefaultAccessRestrictionsSD` 가 다섯째 (`_pDefaultAccessRestrictionsSD`, 없으면 넷째 `_pNoRegAccessRestrictionsSD`) 를 `GetSecurityDescriptorLength` 로 읽는다 → 넷째가 0 B 덩어리면 `ntdll!RtlLengthSecurityDescriptor +0x2d` 접근 위반. 고쳐 읽기: 칸 28 = `{124, 124, 232, 228, 0}` · 29 = 708 · 30 = 자기 상대 SD 넷 (rpcss 코드 상수 SDDL, 바이트는 `[추측: 계산]`) — run151.

> **정정 · 고쳐 읽기 (run151, sast/148 — Q284a) (b) 프로시저 차례**: "첫 COM 사용은 프로시저 7 → 6" (원래 `:21` · `:238` · `:284` 나무 · `:332` · `:353`, 지금 `:25` · `:242` · `:288` · `:336` · `:357`) 의 **차례가 반대** `[정적]` — `CallTheResolver +0x9d JZ` 가 아파트 OXID 미등록 (`[..+0xd8] == 0`) 이면 `+0x1b7 RegisterOXIDAndOIDs` 로 가므로 **6 이 먼저**, 7 (`+0x106 NdrClientCall2`) 은 등록된 뒤 OID 를 더 받을 때 (첫 부름 때 그 칸이 0 인 것은 `[추측]`). 그래서 "그다음 벽" 은 프로시저 6 (`ServerRegisterOXID +0x30 StartListen` 먼저). call 자리 · ret 는 그대로 맞다 — run151.

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-com-rpcss.md`, 튜닝 `366_tune18.md` OR1 · r18 — 이 거절을 받은 샘플 566) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 combase · rpcrt4 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run131 · run136 · run145 와 같은 줄), `sample/*.c` 에 COM API 가 없다 `[소스]` (`sample/log.h:3` "user32(wsprintf)도 안 쓴다" — import 를 늘리지 않음). RPCSS 자체는 **다른 프로세스** (`svchost.exe -k rpcss -p` 의 `rpcss.dll` — 원 요청의 `[수집] reg-users`) 다. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (combase · kernelbase · rpcrt4 · RpcEpMap 줄에 run149 한 마디 · 새 rpcss 줄 · 근거 줄) · 3장 run136 절 한 줄 · run136 절 뒤 새 절 · run145 절 한 줄 · 4장 (run136 정적 줄 표시 · Q257a 닫힘 줄 · run149 줄 셋) · 5장 (run149 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q257a** (= run136 남은 정적 **Q257 의 ①** combase `Connect` 만 앞으로. Q257 을 그 자리에서 Q257a / Q257b 로 나눔 — ② 창 스테이션 · 데스크톱 커널 본문 · ③ `GetConnection +0xc5` `E_ACCESSDENIED` 는 **Q257b** 로 그대로 대기). 사용자 결정 2026-11-26 "이것도 부탁해" (365 둘 다음). 결과서 판정 **분석성공** — 요청 1 · 2 · 3 의 작은 물음마다 줄 근거로 답함 (3 의 (b) `CoGetObject` 와 "클래스 없음" HRESULT 길은 `[공백]`).
> - 이어 쓰는 앞 run: **run136** (`CoInitializeSecurity +0x47d GetConnection` → 람다 `operator()` → `+0x82e Connect`, `Connect` 안은 안 읽음 — 이번에 엶), **run133** (LRPC 연결 · bind `0x70` B 칸 · 문법 마스크 `+0x48` · `CompleteBind` 의 "비트 하나" 검사 · ALPC → RPC 상태 표), **run137** (MIDL 형식 문자열 · 프로시저 표 읽는 법), **run145** (같은 `epmapper` 포트의 EPM 쪽 — RpcEpMap).
> - 앞 run 고쳐 읽기 둘: run136 · run137 맨 위 한 줄씩 (아래 "앞 run 고쳐 읽기").
> - 남은 동적 → 결과서 "동적으로 확인할 것" D1 ~ D11 → 대기열 **Q283** (D2 · D3 · D5 · D7 필수, D8 ~ D11 선택). 남은 정적 → 결과서 "다음 요청 후보" 1 ~ 6 → 대기열 **Q284** (Q257b 에 보태지 않음).

## 요약

- **바인딩 글 · `ept_map` 안 묻기** `[정적]`: `CFastBH::CreateFromBindingString +0x2f` 가 combase `.rdata` rva `0x2cc510` 의 UTF-16 글 `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` 를 잡고, `+0x79 RpcStringBindingParseW` 로 끝점 `epmapper` 를 뽑아 `+0xc8 RpcBindingCreateW` 의 틀에 **정적 끝점 · 프로토콜 번호 3 (LRPC, `+0xb2`)** 으로 넣는다. combase 가 가져오는 RPC 함수에 `RpcEpResolveBinding` · `RpcMgmtEpEltInq*` 가 없다 (결과서 `llvm-readobj --coff-imports`). bind 는 `BindWithTimeout +0xc RpcBindingBind(NULL, 핸들, 인터페이스)` — 비동기 상태 NULL = **동기**.
- **bind 넷** `[정적]`: 람다가 `CFastBH` 넷 (각 `HeapAlloc(0x18)`) 에 인터페이스 구조체를 하나씩 단다 (`+0x15d` · `+0x1a0` · `+0x1e3` · `+0x222`). 1 번 `ILocalObjectExporter` v2.0 (`+0x33c CreateFromBindingString`) → 2 번 `IMachineActivatorControl` v1.0 (`+0x3ca`) → 3 번 `IROT` v0.2 (`+0x3e9`) → 4 번 `ISCM` v0.2 (`+0x468`) — 2 ~ 4 는 `CreateFromBindingHandle` (1 번 핸들 복사 → 풀기 → 다른 인터페이스로 다시 bind). 넷 다 `TransferSyntax` = NDR 2.0 v2.0, `Flags` 비트 25 꺼짐 → bind 마스크 **1** `[추측: 짐작 — 마스크 만드는 줄까지 안 따라감]`.
- **보안** `[정적]`: 요구 서버 SID = **NetworkService `S-1-5-20`** (`+0x2b0 CreateWellKnownSid(24)`), QOS V4 (능력 1 · 동적 추적 · 가장 3), 인증 수준 6 · 서비스 10. 이 값들은 **연결 (`NtAlpcConnectPort` 의 `RequiredServerSid` · `SecurityQos`) 쪽** 으로만 가고, bind 메시지 `0x70` B 에는 칸이 없다 (run133 1-2).
- **받는 모듈** `[정적]`: `rpcss!StartObjectExporter +0xb65 RpcServerRegisterIf3` 가 `ILocalObjectExporter` 서버 구조체 (rva `0x129500`, `Flags 0x20`, 콜백 `LocalInterfaceOnlySecCallback`) 를 등록. 끝점은 `rpcss!UseProtseqIfNecessary +0x23e RpcServerUseProtseqEpExW` 가 표 `gaProtseqInfo` 칸 **16 = {`ncalrpc`, `epmapper`}** 로 열고, `RpcEpMap!InitializeService +0x23` 도 `UseProtseqIfNecessary(16)` — **한 포트를 두 모듈이 나눠 쓴다** (run145 의 EPM 과 한 포트의 다른 인터페이스).
- **`Connect` 스텁** `[정적]`: combase `Connect` (rva `0x12f318`) `+0x17b NdrClientCall2(ILocalObjectExporter_StubDesc, ProcFormatString+0x2)` — 프로시저 **0**, 스택 `0x108`, 파라미터 32 (반환 포함). 출력 열 칸은 스텁이 전역 주소를 직접 박는다 (`_ph` · `gdwScmProcessID` · `_ProcessSignature` · `giPingPeriod` · `gLocalMid` …), 입력 상수 `+0x11b` 예약 OID 요청 수 `10`. 서버 `rpcss!_Connect` (rva `0x76d80`) 의 프로시저 문자열이 클라이언트 것과 바이트까지 같다 (결과서 `pefile`).
- **응답 검사** `[정적]`: 람다 `+0x8c6` `_ph` · `+0x8de` DSA 포인터 · `+0x8e8` `gdwScmProcessID` · `+0x900` `_ProcessSignature` — 0 / NULL 이면 텔레메트리 호출 뒤 **`0x8000ffff`** (줄 `0x387` ~ `0x38a`). 레거시 보안 글은 NULL 이면 건너뛰고, 있으면 2 글자 이상 (`+0x983`). **예약 OID 개수 상한 · SD 크기 합 검사는 없다** (어기면 넘쳐 쓰기 · 넘쳐 읽기). 다 지나면 `+0xa83 gGotSecurityData = 1` · `+0xae4 _dwFlags |= 1` · `+0xb30` 0 반환.
- **실패 쪽** `[정적]`: `RetryRPC` = `+0x4 MOV EDX,0x6bb` 비교, 같으면 `Sleep(100)` 뒤 1 → 람다 `+0x899 JNZ +0x6b4` (횟수 한도 없음). `CheckStatus` = 0 → 0, 양수 N → `0x80070000 | (N & 0xffff)`, 음수 → 그대로. bind 단계 fault 는 `RetryRPC` 에 안 닿고 `CreateFromBindingString +0x16b ~ +0x16e` 가 같은 규칙으로 바꿔 끝. **진짜 rpcss 는 winsta 글이 NULL 이면 상태 `0x57`** (`_Connect +0x186` → `+0x733`).
- **그다음** `[정적]`: `CoInitializeSecurity` 는 `GetConnection` 뒤 이 포트로 **`+0x43c SetAppID` (프로시저 1) 만** — `g_AppId ≠ GUID_NULL` 일 때. `CoInitializeEx` 는 초기화 길에서 이 포트로 안 간다 (지도 깊이 10). **첫 COM 사용 (`CoCreateInstance`, in-proc 이어도)** 은 `InitRemoting +0xb6 InitializeSecurity` → `CoInitializeSecurity` 암묵 호출로 위 연결을 만들고, `+0x29d CallTheResolver` 가 **프로시저 7 → (`RegisterOXIDAndOIDs`) 프로시저 6** 을 보낸다. `GetConnection` 을 부르는 곳은 combase 안 **46곳**.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백일곱째 바퀴. plan **Q257a**. **짐 지도 바퀴 아님.**
- 정적: **rpcss 처음 analyze** (`System32\rpcss.dll`, 약 1분, `MAXMEM=8G` — 시작 전 `free -g` total 46 / available 27, `ms/decomp/rpcss/_index.tsv` 4,784줄 · `FUN_` 1, `ms/ghidra/rpcss.analyze.log` 의 `SetPdb: rpcss.dll <- …/rpcss.dll.pdb` 줄). combase · kernelbase 는 다시 analyze 안 함 (`^이름$` export · rva export · refs · 지도). rpcrt4 · RpcEpMap 은 **있던 `.asm` 읽기만** (sast/143 이 rpcrt4 프로젝트를 쓰는 중이라 안 엶).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| **rpcss** (`rpcss.pdb`) | **`316298244AA2A8A6A520F13E7355EC211`** | **새 줄** (처음 analyze). `fetch_pdb.py` · `llvm-pdbutil-14 dump --summary` `{31629824-4AA2-A8A6-A520-F13E7355EC21}` · 바이너리 디버그 디렉터리 앞 16자 모두 같음. PDB 안 Age 3 · 바이너리 Age 1 (RpcEpMap · nsisvc 와 같은 모양) |
| combase | `DA1DC0C4F35CEE58F672CBB20460D11D1` | 같음 (run131 · run136) |
| kernelbase | `72CCE506944DE0267C172AD64287F9BD1` | 같음 — `IsDeveloperModeEnabled` depth 1 export 만 |
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 같음 (run133 · run137 · run141 · run145) — 있던 `.asm` 읽기만 |
| RpcEpMap | `928AE9EA93768986C32C5500323376111` | 같음 (run145) — 있던 `InitializeService.asm` 읽기만 |

- 버전 대조 (결과서): sast/134 의 오프셋 (`GetConnection +0xbe` · 람다 `+0x82e` · `CoInitializeSecurity +0x47d`) 이 새 `.asm` 에서 그대로 나옴. 핵심 호출 · 상수 60여 줄은 원본 바이트 (`pefile`, 첫 바이트 `e8` / `48 ff 15` · 상수) 와 맞춤 — 일치. "알려진 사실" 표엔 combase · rpcss 줄이 없다.
- 람다 `operator()` 는 combase 안 같은 이름이 70개라 정규식 export 로 못 뽑아, 결과서가 스크래치 복사본 `ExportFunctionsRva.java` (rva 선택) 로 뽑았다 → `ms/disassemble/combase/operator____18b9c0.asm` (함수 시작 `0x18018b9c0`, 3558 B). `Connect` 도 같은 이름이 5개 — **우리 RPC 스텁은 `Connect__12f318.asm`**, `Connect.asm` 은 rva `0xaef40` 의 다른 함수.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): r18 의 샘플 566 이 `\RPC Control\epmapper` 에 **동기** 연결한 뒤 곧바로 인터페이스 `e60c73e6…` 로 bind → 에뮬레이터가 fault `0x6b5` → `CoInitializeSecurity` = `0x800706b5`. 예약 작업 (`schtasks`) · 프로세스 죽이기 (`taskkill`) · 그림자 복사본 지우기 (`vssadmin`) · WMI 묻기 (`CoGetObject`) 가 그 바로 앞에서 끝난다. 정책은 "꼴만 맞춘 최소 대답 · 값 지어내지 않음 · 모르는 부탁은 정직한 실패". bind 를 받아 주면 첫 프로시저가 오는데 그 응답 꼴과 combase 검사를 몰라 못 채웠다. 요청서 `_workspace/sast/144_com-rpcss.md`:
  1. bind — 바인딩 글 (`ept_map` 을 안 묻는 게 코드로 맞나) · 인터페이스 UUID · 버전 · 전송 문법 · 연결 / bind 꼴을 바꾸는 보안 · 진짜에서 받는 모듈.
  2. 첫 프로시저 — 번호 · 스텁 꼴 (요청 · 응답 칸 차례 · 타입) · **응답에서 combase 가 검사하는 것** · out 값 가름 (가) ~ (라) · 실패 쪽 (`RetryRPC` · `CheckStatus`, 깨끗이 끝나는 상태 · 끝없이 다시 거는 상태).
  3. 그다음 — `CoInitializeSecurity` 나머지 · `CoInitializeEx` · 첫 COM 사용 (a) (b) (c) 가 이 포트로 부르는 것 · `GetConnection` 을 부르는 곳.
- 쓰는 곳 (원문): `ext/syscall/syscall_layer_cpp/src/rpc/lrpc.cpp` · `epm.cpp` — 같은 포트 창구에 인터페이스를 늘릴지, 늘린다면 stub 이 돌려줄 최소 대답.

## 방법

- 정적 sast/144 (결과서 `_workspace/sast/144_com-rpcss_result.md`, **분석성공**, 스크립트 문제 둘 — 아래 "도구 메모"):
  - combase (rva export): 람다 `operator()` (rva `0x18b9c0`) → `operator____18b9c0.asm` · `.c`.
  - combase (`^이름$` depth 0): `Connect` (5개 — 우리 것 `Connect__12f318`) · `RetryRPC` · `CheckStatus` · `GetConnection` · `CreateFromBindingString` · `CreateFromBindingHandle` · `BindWithTimeout` · `RegisterWithPsmIfNecessary` · `SuspendImpersonate` · `ResumeImpersonate` · `InitializeSystemSecurityDescriptors` · `CoInitializeSecurity` · `Return_Hr` · ILocalObjectExporter 스텁 열한 개 (`SetAppID` · `GetDefaultSecurityPermissions` · `ServerGetReservedID` · `ClientBulkUpdateOIDWithPingServer` · `ClientResolveOXID` · `ClientResolveMachineLocalOXID` · `ServerRegisterOXID` · `CallTheResolver` · `ServerFreeOXIDAndOIDs` · `SetServerOIDFlags` · `Disconnect` · `GetUpdatedResolverBindings`) · `InitChannelIfNecessary` · `InitRemoting` (`InitRemoting.asm` = rva `0x36ad4`) · `InitializeSecurity` · `CoGetDefaultContext` · `IsDeveloperLicenseInstalled` · `BindToSCMProxy` · `DelegateActivationToSCM`.
  - combase refs 16 (`_refs/`: StubDesc `0x274270` · `gGotSecurityData` `0x338290` · `_dwFlags` `0x33814c` · `_ph` `0x338110` · `_pdsaLocalResolver` `0x338c78` · 그 밖 전역 11), 지도 `_map/CoInitializeEx_*` (깊이 10) · `_map/CoCreateInstance_*` (깊이 12). PE 데이터 (`pefile`): 인터페이스 구조체 넷 · 바인딩 글 · StubDesc · 프로시저 형식 · 형식 문자열.
  - rpcss (새 analyze → export depth 0): `_Connect` · `_Disconnect` · `SetAppID` · `IObjectExporterSecCallback` · `StartObjectExporter` · `UseProtseqIfNecessary` · `StartListeningIfNecessary` · `RegisterAuthInfoIfNecessary` · `ServiceMain` · `InitializeSCMBeforeListen` · `InitializeSCMAfterListen` · `StartServerAndWait` · `ComputeSecurity` · `GetLegacySecurity` · `GetClientServerSvcs` · `CheckLocalCall` · `AllocateReservedIdsInternal` · `CopyMyOrBindings`, refs 11. 서버 인터페이스 구조체 · 디스패치 표 · `gaProtseqInfo` · `.data` 초기값은 `pefile`.
  - kernelbase: `^IsDeveloperModeEnabled$` depth 1.
  - rpcrt4 · RpcEpMap: 있던 `BaseBind.asm` · `PickAssociation.asm` · `Bind__4e330.asm` · `rpcepmap/InitializeService.asm` 읽기만.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - combase 람다 `operator____18b9c0.asm` `+0x14c · +0x15d · +0x18f · +0x1a0 · +0x1d2 · +0x1e3 · +0x211 · +0x222 · +0x2ad ~ +0x2fe · +0x33c · +0x341 · +0x34a · +0x3ca · +0x3e9 · +0x431 · +0x468 · +0x499 · +0x53e · +0x548 · +0x55d ~ +0x576 · +0x5f5 · +0x616 · +0x62b · +0x647 · +0x6b4 ~ +0x8c1 (Connect 앞뒤 전부) · +0x8c6 ~ +0x9c8 (검사 전부) · +0x9c8 ~ +0xb32 (전역 넣기 전부) · +0xb37 ~ +0xb9f · +0xba9 ~ +0xcdf · +0xdb1`.
  - `Connect__12f318.asm +0x2e · +0x39 · +0x44 · +0xac · +0xb7 · +0xd1 · +0xdf · +0x114 · +0x11b · +0x135 · +0x141 · +0x16a · +0x174 · +0x17b`; `RetryRPC.asm` 전부 (떨어진 범위 `+0x21fa62 ~ +0x21faa4` 포함); `CheckStatus.asm` 전부; `CreateFromBindingString.asm +0x2f · +0x63 · +0x79 · +0x8f · +0xa5 · +0xab · +0xb2 · +0xbd · +0xc1 · +0xc8 · +0xe5 · +0xee · +0x167 ~ +0x174 · +0x1b6`; `CreateFromBindingHandle.asm +0x23 ~ +0x6a · +0x14b731 · +0x14b734`; `BindWithTimeout.asm +0x0 ~ +0x22`; `GetConnection.asm +0x10 · +0x24 · +0x34 · +0x56 · +0xbe ~ +0xd2`; `CoInitializeSecurity.asm +0xa6 · +0xac · +0x33b · +0x345 · +0x3f6 · +0x420 · +0x43c · +0x47d ~ +0x494 · +0x5f9 · +0x64a · +0x662 · +0x78a · +0x89c · +0x8a9`; `InitializeSecurity.asm +0x1c ~ +0x9a`; `InitRemoting.asm +0xa9 · +0xb6 · +0x29d`; `CoGetDefaultContext.asm +0x3f`; `InitChannelIfNecessary.asm +0x119`; `CallTheResolver.asm +0x78 · +0x106 · +0x117 · +0x122 · +0x1b7`; `DelegateActivationToSCM.asm +0x156 · +0x1ac`; `BindToSCMProxy.asm +0x17 · +0x49 · +0x81`; `SetAppID.asm +0x25 · +0x2c`; `InitializeSystemSecurityDescriptors.asm +0x1e · +0x4f · +0x8c · +0xc2 · +0xf4 · +0x12b`; `IsDeveloperLicenseInstalled.asm +0x10`; `_map/CoCreateInstance_edges.tsv` 의 사슬 다섯 줄; `_refs/0xa14d0.tsv` (call 46 + 데이터 2) · `_refs/0x18b9c0.tsv` (call 1 + 데이터 1) · `_refs/0x338110.tsv` (65줄).
  - rpcss `_Connect.asm +0x161 ~ +0x1ae · +0x264 · +0x27d · +0x2bf · +0x2ca · +0x2d5 · +0x31d · +0x37a · +0x38d · +0x3bd · +0x3dd · +0x46c · +0x4aa · +0x527 · +0x562 · +0x573 · +0x583 · +0x58d · +0x59b · +0x733 · +0x73a · +0x73f`; `StartObjectExporter.asm +0xb43 · +0xb65`; `UseProtseqIfNecessary.asm +0x23e`; rpcepmap `InitializeService.asm +0x23`.
  - rpcrt4 `BaseBind.asm +0x91 · +0x95 · +0x99 · +0x3a4`; `PickAssociation.asm +0x95 ~ +0xc4`.
  - `pdb_aliases.py --lookup combase.dll 0x37958 · 0x3790c · 0x11ab98 · 0x11add8 · 0xa14d0 · 0x18b9c0 · 0x12f318 · 0x7deec · 0x37608 · 0x5e9f0`, `--lookup rpcss.dll 0x76d80 · 0x38090` — `0x5e9f0` 만 별칭 무리 (아래 "flow-writer 대조" 4).
  - `../PE_emulator/ext/syscall/syscall_layer_cpp/src/rpc/lrpc.cpp` (359줄) · `epm.cpp` (47줄) 를 **줄 번호만 훑음** (읽기만, 고치지 않음). 원 요청 `367_internals-com-rpcss.md` (79줄) 는 숫자 · 대표 줄만 읽음.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **RPCSS 는 "건물 관리 사무소" 다.** COM 을 쓰는 프로그램은 처음 무언가를 하려 할 때 이 사무소에 전화를 건다. 전화번호 (`epmapper`) 는 수첩에 이미 적혀 있어서, 안내 데스크 (EPM `ept_map`) 에 물어보지 않고 곧장 건다.
- 전화가 이어지면 **창구 넷** (해결사 · 활성화 제어 · 실행 중 목록 · 클래스 관리) 에 차례로 "말 걸어도 되나요?" (bind) 를 묻는다. 한 창구라도 "그런 창구 없어요" (`0x6b5`) 라고 하면 그 자리에서 전화를 끊는다.
- 넷 다 받아 주면 첫 창구에 **"저 왔어요" (`Connect`)** 를 보낸다 — 내 방 주소 (창 스테이션 글) · 내 이름 (exe 경로) 을 주고, 사무소에서 **출입증 (컨텍스트 핸들) · 사무소 직원 번호 (rpcss PID) · 내 서명 · 사무소 연락처 (로컬 해결사 주소)** 를 받는다. 이 넷 가운데 하나라도 빈칸이면 "이상한데?" 하고 포기한다 (`0x8000ffff`).
- "바빠요" (`0x6bb`) 라는 대답만은 **0.1초 쉬고 끝없이 다시 건다.** 그 밖의 "못 해요" 는 한 번에 받아들이고 끝낸다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{combase,rpcss,rpcepmap,rpcrt4}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `combase!CoInitializeSecurity` (`0x7e880`) | **`+0x47d`** | **`+0x482`** | **`CRpcResolver::GetConnection`** (`0xa14d0`) — `+0xa6 CMP [gGotSecurityData],EDI` · `+0xac JZ` (0 일 때만) | [정적] |
| 〃 | `+0x420` | `+0x427` | `I_RpcSetDCOMAppId` (rpcrt4, 프로세스 안) — `+0x3f6` `g_AppId` ≠ `GUID_NULL` 일 때 | [정적] |
| 〃 | `+0x43c` | `+0x441` | **`SetAppID`** (`0x7f818`) = ILocalObjectExporter 프로시저 1 | [정적] |
| `combase!GetConnection` | **`+0xbe`** | **`+0xc3`** | **람다 `operator()`** (`0x18b9c0`) — `+0x34` / `+0x56 TEST [_dwFlags],1` 꺼져 있을 때. 이 람다를 부르는 곳은 여기 하나 (`_refs/0x18b9c0.tsv`) | [정적] |
| 람다 `operator()` | `+0x14c` · `+0x18f` · `+0x1d2` · `+0x211` | `+0x153` · `+0x196` · `+0x1d9` · `+0x218` | `HeapAlloc(0x18)` ×4 — `CFastBH` 넷, 인터페이스 구조체는 뒤 `+0x15d` · `+0x1a0` · `+0x1e3` · `+0x222 LEA RCX` | [정적] |
| 〃 | `+0x2b0` | `+0x2b7` | `CreateWellKnownSid(24 = NetworkService)` (`+0x2ad LEA ECX,[RDI-0x2c]`) | [정적] |
| 〃 | **`+0x33c`** | **`+0x341`** | **`CFastBH::CreateFromBindingString`** (`0x11ab98`) — 1 번 bind (`ILocalObjectExporter`). 실패면 `+0x34a JMP +0xcb8` (줄 `0x300`) | [정적] |
| 〃 | `+0x3ca` · `+0x3e9` · `+0x468` | `+0x3cf` · `+0x3ee` · `+0x46d` | `CFastBH::CreateFromBindingHandle` (`0x11add8`) — 2 · 3 · 4 번 bind | [정적] |
| 〃 | `+0x431` | `+0x436` | `IsDeveloperLicenseInstalled` — 음수면 `Connect` 전에 끝 (줄 `0x314`) | [정적] |
| 〃 | `+0x499` | `+0x4a0` | `HeapAlloc(0x60)` — IPID 표 정보 (개발자 라이선스 있을 때만) | [정적] |
| 〃 | `+0x548` | `+0x54d` | `RegisterWithPsmIfNecessary` (음수면 줄 `0x331`) | [정적] |
| 〃 | `+0x5f5` · `+0x616` | `+0x5fa` · `+0x61b` | `AppModelPolicy_GetPolicy` ×2 → 깃발 `\|= 2` (`+0x603`) · `\|= 4` (`+0x624`) | [정적] |
| 〃 | `+0x62b` | `+0x630` | `SuspendImpersonate` (음수면 줄 `0x359`, `+0x647`) | [정적] |
| 〃 | **`+0x82e`** | **`+0x833`** | **`Connect`** (`0x12f318`) — RCX `[R14]` (`+0x78c`) · RDX `_pwszWinstaDesktop` (`+0x742`) · R8 exe 경로 (`+0x751`) · R9 IPID 표 (`+0x77e`) · `[RSP+0x20]` 깃발 (`+0x82a`) · `[RSP+0x28]` `0x8664` (`+0x823`) | [정적] |
| 〃 | **`+0x884`** | **`+0x889`** | **`CRpcResolver::RetryRPC`** (`0x3790c`) — 1 이면 `+0x897 TEST` · `+0x899 JNZ +0x6b4` (다시) | [정적] |
| 〃 | `+0x8a3` | `+0x8a8` | `ResumeImpersonate` | [정적] |
| 〃 | **`+0x8aa`** | **`+0x8af`** | **`CRpcResolver::CheckStatus`** (`0x37958`) — 음수면 `+0x8b3 JNS` 안 탐 → `+0x8bc` 줄 `0x384` | [정적] |
| 〃 | `+0x8cf` · `+0x8f1` · `+0x909` · `+0x989` · `+0xb37` | `+0x8d4` · `+0x8f6` · `+0x90e` · `+0x98e` · `+0xb3c` | `MicrosoftTelemetryAssertTriggeredNoArgs` — 응답 검사 실패 다섯 갈래 (뒤 `0x8000ffff`) | [정적] |
| 〃 | `+0x925` · `+0x954` | `+0x92c` · `+0x959` | `HeapAlloc(0x10)` · `Attach` → `_pdsaLocalResolver` (실패면 `+0x962` 줄 `0x38e` → `+0x562` `0x8007000e`) | [정적] |
| 〃 | `+0x9a3` | `+0x9a8` | unique_ptr 를 새 값으로 바꾸는 공용 도우미 (`+0x998 LEA RCX,[gLegacySecurity]`) — 같은 주소 이름 11개, 후보: `unique_ptr<tagSECURITYBINDING>::reset` · `unique_ptr<tagDUALSTRINGARRAY>::reset` … | [정적] |
| 〃 | `+0x9b3` | `+0x9b8` | `InitializeSystemSecurityDescriptors` (`0x127200`) — 음수면 `+0x9be` 줄 `0x39c` | [정적] |
| 〃 | `+0xa5b` · `+0xa7e` | `+0xa60` · `+0xa83` | `memset(_OidsReserved, 0, 0x50)` · `memcpy(_OidsReserved, 배열, 개수×8)` | [정적] |
| `combase!CFastBH::CreateFromBindingString` | `+0x79` | `+0x80` | `RpcStringBindingParseW(글 0x2cc510, …, &끝점, …)` | [정적] |
| 〃 | `+0xc8` | `+0xcf` | `RpcBindingCreateW(틀, 보안, 옵션, &핸들)` — `+0xb2` 프로토콜 3 · `+0xbd` 끝점 | [정적] |
| 〃 | `+0xe5` | `+0xea` | `CFastBH::BindWithTimeout` (`0x11af5c`) | [정적] |
| `combase!CFastBH::CreateFromBindingHandle` | `+0x23` · `+0x3a` · `+0x54` | `+0x2a` · `+0x41` · `+0x59` | `RpcBindingCopy` · `RpcBindingUnbind` · `BindWithTimeout` → `+0x6a CMPXCHG [CFastBH+0x10]` | [정적] |
| `combase!CFastBH::BindWithTimeout` | `+0xc` | `+0x13` | `RpcBindingBind(NULL, 핸들, 인터페이스)` — `+0xa XOR ECX,ECX` (비동기 상태 NULL) | [정적] |
| `combase!Connect` (`0x12f318`) | **`+0x17b`** | **`+0x182`** | **`NdrClientCall2(ILocalObjectExporter_StubDesc 0x274270, lclor ProcFormatString+0x2)`** — 프로시저 0 | [정적] |
| `combase!CRpcResolver::RetryRPC` | `+0x21fa93` | `+0x21fa9a` | `Sleep(100)` (`+0x21fa8e MOV ECX,0x64`) — 떨어진 범위 | [정적] |
| `combase!InitializeSecurity` (`0x7deec`) | `+0x7c` | `+0x81` | `CoInitializeSecurity` (암묵 호출, `gpsaSecurity` NULL 일 때만 `+0x21`) | [정적] |
| 〃 | `+0x95` | `+0x9a` | `UninitializeSecurity` — 음수 반환일 때 (`0x80070057` 은 `+0x90` 에서 `0x80040153` 로 바꾼 뒤) | [정적] |
| `combase!CoGetDefaultContext` | `+0x3f` | `+0x44` | `InitChannelIfNecessary` | [정적] |
| `combase!InitChannelIfNecessary` | `+0x119` | `+0x11e` | `CComApartment::InitRemoting` (`0x36ad4`) | [정적] |
| `combase!CComApartment::InitRemoting` | `+0xb6` | `+0xbb` | `InitializeSecurity` — 앞 `+0xa9` CFG 간접 호출 결과에 달림 | [정적] |
| 〃 | `+0x29d` | `+0x2a2` | `CComApartment::CallTheResolver` (`0x37608`) | [정적] |
| `combase!CComApartment::CallTheResolver` | `+0x78` · `+0x106` · `+0x117` · `+0x122` · `+0x1b7` | `+0x7d` · `+0x10d` · `+0x11c` · `+0x127` · `+0x1bc` | `EnsureLinkToPrimaryOxid` · **`NdrClientCall2` (프로시저 7)** · `RetryRPC` · `CheckStatus` · `OXIDEntry::RegisterOXIDAndOIDs` (→ `ServerRegisterOXID` 프로시저 6) | [정적] |
| `combase!DelegateActivationToSCM` | `+0x156` | `+0x15b` | `CRpcResolver::BindToSCMProxy` | [정적] |
| `combase!CRpcResolver::BindToSCMProxy` | `+0x17` · `+0x49` · `+0x81` | `+0x1c` · `+0x4e` · `+0x86` | `GetConnection` · `MakeSCMProxy` · `MakeCloakedSCMProxy` | [정적] |
| `rpcss!_Connect` (`0x76d80`) | `+0x161` · `+0x177` | `+0x166` · `+0x17c` | `CheckLocalCall` · `RetrievePIDForLocalClient` — 실패면 `+0x168` / `+0x180 JZ → +0x73a MOV EAX,5` | [정적] |
| 〃 | `+0x27d` | `+0x282` | `IsGetDesktopAppXComRootHandlePresent` (깃발 4 일 때) | [정적] |
| 〃 | `+0x37a` · `+0x38d` · `+0x3bd` | `+0x37f` · `+0x392` · `+0x3c2` | `GetClientServerSvcs` · `UpdateChannelHooks` · `CopyMyOrBindings` (DSA) | [정적] |
| 〃 | `+0x527` · `+0x583` · `+0x59b` | `+0x52c` · `+0x588` · `+0x5a2` | `CProcess` 생성자 (컨텍스트 핸들) · `AllocateReservedIdsInternal(10, …)` · `GetCurrentProcessId` | [정적] |
| `rpcss!StartObjectExporter` | `+0xb65` | `+0xb6c` | `RpcServerRegisterIf3` — `+0xb43 LEA RCX,[0x180129500]` (ILocalObjectExporter 서버 구조체) | [정적] |
| `rpcss!UseProtseqIfNecessary` | `+0x23e` | `+0x245` | `RpcServerUseProtseqEpExW(protseq, 0x32, 끝점, …)` | [정적] |
| `rpcepmap!InitializeService` | `+0x23` | `+0x28` | `UseProtseqIfNecessary(16)` (RpcEpMap 안의 같은 이름 함수) | [정적] |

### 1. bind — 무엇을 내미나 (물음 1) [정적]

**1-1. 연결 · 바인딩 핸들 만들기** (람다 `+0x2ad ~ +0x33c`, `CreateFromBindingString`)

| 칸 | 값 | 줄 |
|---|---|---|
| 바인딩 글 | `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` (combase `.rdata` rva `0x2cc510`, UTF-16 — 결과서 `pefile`) | `CreateFromBindingString +0x2f LEA R12,[0x1802cc510]` → `+0x63 MOV RCX,R12` → `+0x79 RpcStringBindingParseW` |
| 프로토콜 · 끝점 | LRPC (`3`), 끝점 `epmapper`, 네트워크 주소 NULL | `+0xb2 MOV [RBP-0x19],3` · `+0xbd MOV [RBP-0x9],RAX` (끝점) · `+0xc8 RpcBindingCreateW` |
| 바인딩 옵션 | Version 1 · Flags 1 · ComTimeout 5 · CallTimeout `0xffffffff` (구조체 이름 `[이름 추측]`) | `+0xa5` · `+0xab` · `+0xc1 MOV [RBP+0x1f],5` · `+0x8f OR [RBP+0x23],-1` |
| **`ept_map` 을 묻나** | **안 묻는다.** 끝점이 틀에 박혀 있고, combase 가 가져오는 RPC 함수에 끝점 풀이가 없다 (결과서 import 목록: `RpcStringBindingParseW` · `RpcBindingCreateW` · `RpcBindingCopy` · `RpcBindingUnbind` · `RpcBindingBind` · `NdrClientCall2` · `NdrClientCall3` 뿐) | 위 줄 + `BindWithTimeout +0xc` |
| 동기 · 비동기 | **동기** — `RpcBindingBind(NULL, 핸들, 인터페이스)`. `BindWithTimeout` 의 셋째 인자 (시간제한) 는 안 쓰인다 | `BindWithTimeout +0x4 MOV R8,RDX` · `+0x7 MOV RDX,RCX` · `+0xa XOR ECX,ECX` · `+0xc` |
| 요구 서버 SID | **NetworkService `S-1-5-20`** → QOS `Sid` 칸 | 람다 `+0x2ad LEA ECX,[RDI-0x2c]` (`0x44 − 0x2c = 0x18`) · `+0x2b0 CreateWellKnownSid` · `+0x2d1 MOV [RBP+0x80],RAX` |
| 가장 · 추적 · 능력 | QOS Version 4 · Capabilities 1 · IdentityTracking 1 (동적) · ImpersonationType 3 · 유효 전용 0 | `+0x2c3 [RBP+0x60]=4` · `+0x2e0 [RBP+0x64]` · `+0x2e3 [RBP+0x68]` · `+0x2e6 [RBP+0x6c]=3` · `+0x2ed [RBP+0x88]=0` |
| 인증 | AuthnLevel 6 · AuthnSvc 10 · 보안 구조체 버전 1 | `+0x2fe [RBP+0x48]=6` · `+0x2f7 [RBP+0x4c]=0xa` · `+0x2f4 [RBP+0x38]` |
| 연결 · bind 로 나가는 쪽 | 연결 `NtAlpcConnectPort` 의 `RequiredServerSid` (= 위 SID) · `SecurityQos`. **bind 메시지 `0x70` B 에는 보안 칸이 없다** (run133 1-2) | QOS 가 연결 객체로 옮겨 가는 줄은 rpcrt4 `RpcBindingCreateW` 안 — 안 읽음 `[추측]` |

**1-2. bind 넷** (인터페이스 구조체 — combase PE, 결과서 `pefile`. 모두 `RPC_CLIENT_INTERFACE` 길이 `0x60`, `DispatchTable` · 끝점 · `InterpreterInfo` · `Flags` 0, `TransferSyntax` = NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0)

| 차례 | PDB 이름 | rva | UUID | 버전 | bind 호출 | 실패 줄 |
|---|---|---|---|---|---|---|
| 1 | `ILocalObjectExporter___RpcClientInterface` | `0x2fcd00` (`+0x15d LEA`) | `e60c73e6-88f9-11cf-9af1-0020af6e72f4` | **2.0** | `+0x33c CreateFromBindingString` | `0x300` |
| 2 | `IMachineActivatorControl___RpcClientInterface` | `0x2fcdf0` (`+0x1a0`) | `c6f3ee72-ce7e-11d1-b71e-00c04fc3111a` | 1.0 | `+0x3ca CreateFromBindingHandle` | `0x306` |
| 3 | `IROT___RpcClientInterface` | `0x2fcf30` (`+0x1e3`) | `b9e79e60-3d52-11ce-aaa1-00006901293f` | 0.2 | `+0x3e9` | `0x30a` |
| 4 | `ISCM___RpcClientInterface` | `0x2fd220` (`+0x222`) | `412f241e-c12a-11ce-abff-0020af6e7a17` | 0.2 | `+0x468` | `0x30e` |

- 원 요청의 `[추측 — 공개 이름]` "`e60c73e6… = ILocalObjectExporter`" 은 **PDB 이름으로 닫혔다** `[정적]` (`.asm` `+0x15d` 주석 `ILocalObjectExporter___RpcClientInterface`).
- 2 ~ 4 번: 1 번 핸들을 `RpcBindingCopy` (`+0x23`) → `RpcBindingUnbind` (`+0x3a`) → 다른 인터페이스로 `BindWithTimeout` (`+0x54`) → `+0x6a CMPXCHG` 로 `CFastBH+0x10` 에. 끝점 이름이 같아 rpcrt4 `PickAssociation` 이 이미 있는 연결을 다시 쓸 것으로 보인다 — `+0xa1 _wcsicmp` (끝점) · `+0xb4 / +0xb9` 상태 ≠ 2 · ≠ 5 · `+0xbe CMP [RDI+0x48],R13D` 만 봤다 `[추측: 보안 칸 비교 줄은 이 함수에서 못 봄]` → **새 `NtAlpcConnectPort` 없이 같은 포트에 bind (종류 1) 가 세 번 더** `[추측]` (동적 Q283 D2).
- `CFastBH` 넷은 람다를 부를 때마다 새로 만든다 (`HeapAlloc(0x18)` ×4) → 실패를 기억하는 칸이 없다. `GetConnection +0x34 / +0x56 TEST [_dwFlags],1` 이 서 있어야만 (성공 때 람다 `+0xae4 OR [_dwFlags],R15D`) 람다를 건너뛴다. 그래서 COM 을 부를 때마다 처음부터 다시 연결 · bind 한다 `[정적]` (요청 쪽 로그의 "여섯 샘플이 6,837 ~ 6,995 번씩" 과 같은 모양 — 에뮬레이터 로그라 `[관찰]` 아님).

**1-3. 내미는 전송 문법** — 넷 다 `Flags` 비트 25 (`0x2000000`) 꺼짐 · `InterpreterInfo` 0. rpcrt4 `BaseBind +0x91 MOV EAX,[R8+0x58]` · `+0x95 BT EAX,0x19` · `+0x99 JNC → +0x3a4 MOV RDX,[R8+0x18]` — 이 비트가 꺼지면 문법 표 대신 **구조체 자신의 `TransferSyntax` 하나** 를 쓴다 `[정적]`. 그래서 bind 문법 마스크는 **`1` (NDR 2.0)** `[추측: 짐작 — 마스크를 만드는 `Bind__4e330 +0x4e0` 까지 이 갈래로 이어 보지는 않음]`. 스텁도 `NdrClientCall2` 라 어차피 NDR 2.0 전용. **서버 흉내는 `+0x48 = 1` 로 답** (NDR64 `2` 로 답하면 이 클라이언트는 못 푼다).

**1-4. 진짜에서 받는 모듈**

- `rpcss.dll`: `StartObjectExporter +0xb43 LEA RCX,[0x180129500]` → `+0xb65 RpcServerRegisterIf3(구조체, NULL, NULL, Flags 0x20, MaxCalls 0x4d2, 0, 콜백 LocalInterfaceOnlySecCallback (rva 0x1fbf0), SD)` `[정적]`. `Flags 0x20` = 로컬 전용 `[이름 추측]`. 프로시저 12개 모두 `NdrServerCall2` (동기) — 결과서 `pefile` 디스패치 표.
- 나머지 셋도 rpcss 안 (서버 구조체 rva — IMachineActivatorControl `0x129060` (7) · IROT `0x129130` (11) · ISCM `0x128f00` (27)). 공개 DCOM `IObjectExporter` (`99fcfec4…`, `0x1292d0`) 도 같은 프로세스에 있지만 이번 길엔 안 나온다.
- 끝점: `rpcss!UseProtseqIfNecessary +0x23e RpcServerUseProtseqEpExW` · 전역 표 `gaProtseqInfo` (rva `0x164030`, 칸 24 B) **칸 16 = {1, `ncalrpc`, `epmapper`}** (결과서 `pefile`). `RpcEpMap!InitializeService +0x23 UseProtseqIfNecessary(16)` — **칸 번호가 같다** → 같은 (`ncalrpc`, `epmapper`) 를 두 모듈이 연다 = **포트 하나를 서비스 둘이 나눠 쓴다** `[정적]`. 둘이 같은 `svchost -k rpcss` 안이라는 것은 원 요청의 `[수집] reg-users` 서비스 설정이 근거 (코드만으로는 못 정함). run145 의 EPM (`e1af8308…`) · RpcEpMap 로컬 (`0b0a6584…`) 과 **한 포트의 다른 인터페이스**.

### 2. 첫 프로시저 `Connect` (물음 2) [정적]

**2-0. 자리**: combase `Connect` rva `0x12f318` (399 B) — `+0x16a LEA RDX,[lclor__MIDL_ProcFormatString+0x2]` (rva `0x2c26e2`) · `+0x174 LEA RCX,[ILocalObjectExporter_StubDesc]` (rva `0x274270`) · `+0x17b NdrClientCall2`. **프로시저 0**, 스택 `0x108` (핸들 + 32 칸), `Oi_flags 0x68` · `Oi2 0x47` (결과서 `pefile` 풀이). 서버 `rpcss!_Connect` rva `0x76d80` (1887 B) — 서버 프로시저 문자열 0 번 (rva `0x14fb82`) 이 클라이언트 것과 바이트까지 같다 (결과서). 반환 `error_status_t` (0 = 성공).

**2-1. 요청 칸** (입력)

| 차례 (스택) | 칸 | 타입 | 이 호출의 값 | 줄 |
|---|---|---|---|---|
| — (RCX) | 바인딩 핸들 | 명시 핸들 — 선 위에 안 실림 | 1 번 bind 핸들 | 람다 `+0x78c MOV RCX,[R14]` |
| 0 (RDX) | 창 스테이션 · 데스크톱 글 | `[in, unique, string] wchar_t*` | `_pwszWinstaDesktop` (예 `WinSta0\Default`), 핸들 단계 0 이면 **NULL** (run136) | `+0x742` |
| 1 (R8) | exe 경로 | 같은 꼴 | `GetModuleFileNameW(NULL, buf, 0x207)` 결과 | `+0x751 LEA R8,[RBP+0x150]` |
| 2 (R9) | IPID 표 정보 | `[in, unique]` 96 B 구조체 | **보통 NULL** — 개발자 라이선스가 있을 때만 채움 | `+0x77e MOV R9,RBX` · 채움 `+0x499 ~ +0x53e` |
| 3 (`0x20`) | 깃발 | `[in] ulong` | 0 ~ 7 (`g_GLBOPT_RoFlags` 비트 3 · `AppModelPolicy` 둘 → `\|= 2` · `\|= 4`), 보통 0 | `+0x82a MOV [RSP+0x20],EDI` |
| 4 (`0x28`) | 머신 | `[in] ushort` | **`0x8664`** 고정 | `+0x823` |
| 9 (`0x50`) | 예약 OID 요청 수 | `[in, range(0, 0x20000)] long` | **10** 고정 | `Connect +0x11b MOV [RSP+0x60],0xa` |

- 요청 스텁 고정 부분 22 B (유니크 포인터 id 셋 + 깃발 · 머신 · 요청 수 + 정렬), 뒤에 비-NULL 포인터 본문 `[규칙]` (결과서). **서버 흉내는 요청 본문을 안 읽어도 된다** — 요청 머리 `+0x3c` (프로시저 번호) · `+0x38` (슬롯) 만 보면 됨 (run133 2-1).

**2-2. 응답 칸** (출력 — 선 위 차례는 파라미터 번호 순 `[규칙]`. "검사" 열은 람다 줄, 전역 rva 는 combase. 가름: (가) 상수 · 규칙 (나) 프로세스 · 연결마다 (다) 난수 · 식별자 (라) 0 / NULL / 빈 것이어도 지나감)

| 차례 | 칸 → 받는 곳 | 타입 | 가름 | combase 검사 · 쓰는 줄 | 0 · NULL | 최소 대답 (꼴만) |
|---|---|---|---|---|---|---|
| 5 | **`_ph`** (`0x338110`) | 컨텍스트 핸들 20 B (속성 4 + GUID 16) | (다)+(나) rpcss `CProcess` (`_Connect +0x527`) | `+0x8c6 CMP [_ph],R13` · `+0x8cd JNZ` — **NULL 이면 `+0x8d4` 줄 `0x387` → `0x8000ffff`**. 뒤에서 65곳이 읽음 (`Disconnect` · `SetAppID +0x25` · `CallTheResolver` · `IrotGetObject` …) | **불가** (20 B 가 다 0 이면 NULL 핸들) | 속성 0 + 0 아닌 GUID (연결마다 하나, 뒤 프로시저가 `[in]` 으로 되돌려 줌) |
| 6 | 핑 주기 `giPingPeriod` (`0x336288`) | `ulong` | (가) rpcss `g_usBaseTimeoutInterval` PE 초기값 `0x168` = 360 (`_Connect +0x562`) | `+0xad6 IMUL …,0x3e8` · `+0xaeb` 저장 (ms) | 검사 없음, 0 은 부적절 `[추측]` | 360 |
| 7 | **로컬 해결사 바인딩** → `_pdsaLocalResolver` (`0x338c78`) | `[out] DUALSTRINGARRAY**` | (나)/(다) rpcss 듣는 주소 (`CopyMyOrBindings`, `_Connect +0x3bd`) | `+0x8de CMP [RBP-0x48],R13` · `+0x8e2 JZ +0xb37` → **`+0xb3c` 줄 `0x388` → `0x8000ffff`**; 아니면 `+0x925 HeapAlloc(0x10)` · `+0x954 Attach`, `+0x959` 다시 NULL 이면 `+0x962` 줄 `0x38e` → `0x8007000e` | **포인터 NULL 불가.** `wNumEntries 0` 은 이 함수 안에선 지나감, 뒤 쓰임 `[공백]` | 비-NULL 구조체, 내용 `TODO(VM)` |
| 8 | 로컬 MID `gLocalMid` (`0x336290`) | `ULONG64` | (다) (`_Connect +0x573`) | 검사 없음, 15곳이 읽음 | 가능, 0 은 피함 `[추측]` | 0 아닌 상수 |
| 10 | 예약 OID 배열 → `_OidsReserved` (`0x3362a0`, 0x50 B) | `ULONG64[]` 조건 길이 = 입력 10 — 선 위에 열 칸 다 | (다) (`_Connect +0x583 AllocateReservedIdsInternal`) | `+0xa5b memset(…,0,0x50)` · `+0xa7e memcpy(…, 개수×8)` | 값 0 가능 | 열 칸 0 |
| 11 | 예약 OID 개수 `_cReservedOidsAvail` (`0x33813c`) | `ulong` | (다)/(나) | `+0xa60` · `+0xa6d` 저장 · `+0xa73 SHL R8,3` — **상한 검사 없음** | **0 가능 (라). 10 초과 금지** (0x50 B 를 넘쳐 씀) | **0** |
| 12 | 설정 비트 (→ 전역 아홉) | `ulong` | (가) rpcss 설정 전역 (`_Connect +0x2bf ~ +0x31d`) | `+0x9c8 ~ +0xa48` (`g_GLBOPT_EHValue` · `gAccessRestrictionsViaPolicy` · `gfBreakOnSilencedExceptions` · `gEnableNetworkDCOM` · `gEnableContainerDCOM` · `gEnableOldModalLoop` · `gfLogCallFailure` · `gfLogInvalidSecurityDescriptor` · `gCapabilities`), 뒤 `+0xa8a ~ +0xacf` 가 `gEnableNetworkDCOM` 다시 정함 | 가능 | 0 (`TODO(VM)`) |
| 13 / 14 | 컨테이너 인증 서비스 들어옴 / 나감 (`0x33827c` / `0x33828c`) | `ulong` ×2 | (가) 기본 0 · 0 | 스텁이 전역에 곧장 (`Connect +0xdf / +0xd1`) | 가능 | 0 · 0 |
| 15 | 레거시 보안 글 → `gLegacySecurity` (`0x338d18`) | `[out] wchar_t**` | (가) 기본 NULL | `+0x970 TEST RDX,RDX` · `+0x973 JZ` (NULL 이면 건너뜀); 있으면 `+0x975 ~ +0x983` 글자 수 **2 미만 → `+0x98e` 줄 `0x398` → `0x8000ffff`**; 아니면 `+0x998 ~ +0x9a3` 넣음 | **NULL 가능 (라)** | NULL |
| 16 / 17 | 인증 수준 / 가장 수준 → `gAuthnLevel` (`0x334e5c`) / `gImpLevel` (`0x334e60`) | `ulong` ×2 | (가) 기본 2 · 2 (rpcss `ComputeSecurity`, 레지스트리가 바꿀 수 있음) | 스텁이 곧장 (`Connect +0xb7 / +0xac`). `CoInitializeSecurity +0x33b / +0x345` 가 나중에 덮어씀 | 가능, 0 은 뒤 쓰임 `[공백]` | 2 · 2 |
| 18 / 19 | 서버 인증 서비스 개수 / 목록 → `gServerSvcList` (`0x338d08`) | `ulong` / `[out] USHORT**` (널 허용) | (가) | `+0x84b MOV [RCX+8],RDX` (개수) | NULL + 0 가능 (라) `[추측: 읽는 쪽이 템플릿 안이라 refs 로 안 닫힘]` | 0 / NULL |
| 20 / 21 | 클라이언트 인증 서비스 개수 / 목록 → `gClientSvcList` (`0x338cf8`) | `ulong` / `[out] struct**` (원소 16 B) | (가) | `+0x85c MOV [RAX+8],RCX` | 가능 (라) | 0 / NULL |
| 22 / 23 | 채널 훅 개수 / GUID 목록 → `gaChannelHook` (`0x338d60`) | `ulong` / `[out] GUID**` | (가) (`_Connect +0x38d`) | `+0x86d MOV [RAX+8],RCX` | 가능 (라) | 0 / NULL |
| 24 | 스레드 번호 → OLE TLS `+0x10` (`GetConnection +0x10 ~ +0x24` · 람다 `+0x6c4` · `+0x6c8 ADD RSI,0x10`) | `ulong` | (나) rpcss `gNextThreadID++` (PE 초기 1, `_Connect +0x58d XADD`) | 읽는 줄 없음 | 가능 | 1, 2, 3 … |
| 25 | **`gdwScmProcessID`** (`0x33800c`) | `ulong` | (나) rpcss PID (`_Connect +0x59b GetCurrentProcessId`) | `+0x8e8 CMP` · `+0x8ef JNZ` — **0 이면 `+0x8f6` 줄 `0x389` → `0x8000ffff`** | **불가** | 0 아닌 값 — bind 답 `+0x08` (서버 PID) 와 같게 `[추측]` |
| 26 | **`_ProcessSignature`** (`0x338120`) | `ULONG64` | (다)+(나) (`CProcess+0x248`) | `+0x900 CMP` · `+0x907 JNZ` — **0 이면 `+0x90e` 줄 `0x38a` → `0x8000ffff`**. 읽는 쪽 `DelegateActivationToSCM +0x1ac` | **불가** | 0 아닌 값 |
| 27 | `_GuidRPCSSProcessIdentifier` (`0x336b68`) | GUID 16 B | (나)(다) (`CProcess+0x68 ~ +0x78`) | 검사 없음 | 가능, 0 아닌 값 권장 | 임의 GUID |
| 28 | SD 다섯 조각 크기 | `ulong[5]` | (가) | `+0x9ac LEA RDX,[RBP+0x90]` → `+0x9b3 InitializeSystemSecurityDescriptors` (크기마다 `LocalAlloc` + `memcpy` — `.asm +0x4f · +0x8c · +0xc2 · +0xf4 · +0x12b`, 검사는 `LocalAlloc` NULL 뿐) | 가능 (라) | 0 ×5 |
| 29 / 30 | SD 덩어리 총 크기 / 덩어리 | `ulong` / `[out] uchar**` (널 허용) | (가) | 칸 28 합 ≤ 총 크기여야 함 — **안 막음 (넘쳐 읽기)** | NULL 은 크기 합 0 일 때만 | 0 / NULL |
| 31 | 반환 상태 | `error_status_t` | 서버가 정함 | `+0x884 RetryRPC` · `+0x8aa CheckStatus` (3절) | 0 = 성공 | **0** |

- **"성공" 을 알리는 줄** `[정적]`: 위 검사를 다 지나고 `InitializeSystemSecurityDescriptors` 도 성공한 **다음** `+0xa83 MOV [gGotSecurityData],R15D` (= 1, `+0x768 MOV R15D,1`) → `+0xae4 OR [_dwFlags],R15D` → `+0xb30 XOR EAX,EAX` 0 반환. `gGotSecurityData` 를 뒤에서 읽는 쪽 (이름만, `_refs/0x338290.tsv` — 결과서): `CoInitializeSecurity +0xa6 / +0x48e` · `SetExplicitWinstaDesktopString +0xa` (켜져 있으면 "너무 늦음") · 전역 옵션 `Set` / `Query` `[이름 추측]` · `CoAllowUnmarshalerCLSID` · `CoEnableAutomaticClientDependencyCreation` · `IsMethodInvocationAllowed` · `IsObjectCreationAllowed`. 지우는 곳 `CRpcResolver::Cleanup +0x2c`.
- 응답 선 위 진짜 바이트 (조건 길이 · 정렬 · 포인터 id) 는 안 풀었다 — 차례는 `[규칙]` (동적 Q283 D7).

**2-3. 서버 쪽 검사 (`rpcss!_Connect`)** — 요청 · 응답 꼴을 정하는 것만

- 호출자: `+0x161 CheckLocalCall` (ncalrpc 아니면) · `+0x177 RetrievePIDForLocalClient` 실패 → **상태 5** (`+0x168` / `+0x180 JZ → +0x73a MOV EAX,5`).
- 입력: **winsta 글 포인터 NULL** (`+0x186 CMP [RBP-0x38],RDX` · `+0x18a JZ → +0x733`) · 출력 포인터 NULL (`+0x190 ~ +0x1a8`) · 깃발 `& 0xfffffff8` · 깃발 4 인데 `+0x27d IsGetDesktopAppXComRootHandlePresent` 거짓 → **상태 `0x57`** (`+0x733 MOV EAX,0x57`). exe 경로는 NULL 검사 없음 (결과서).
- → **진짜 rpcss 는 winsta 글이 NULL 이면 `Connect` 를 거절한다** → combase `CheckStatus(0x57)` = `0x80070057`. run136 의 "핸들 단계 0 → 글 NULL → `Connect(NULL)`" 길의 진짜 끝이다 (아래 "앞 run 고쳐 읽기").

### 3. 실패 처리 (물음 2 실패 쪽) [정적]

`RetryRPC` (rva `0x3790c`) · `CheckStatus` (rva `0x37958`) 는 combase 의 프로시저 스텁이 같이 쓰는 도우미다 (`CallTheResolver +0x117 / +0x122` 도 같은 둘).

| 받은 것 | `RetryRPC` (`+0x884`) | `CheckStatus` (`+0x8aa`) | 다시 거나 | 결과 HRESULT | 줄 |
|---|---|---|---|---|---|
| 상태 0 | 0 | 0 | 아니오 | 검사 (2-2) 를 지나면 0 | `RetryRPC +0x4 ~ +0xd` · `CheckStatus +0x8 · +0xa · +0xc` |
| **상태 `0x6bb`** (이름 `[이름 추측: SERVER_TOO_BUSY]`) | **1** — 앞서 `Sleep(100)` | (고리 안에선 안 불림) | **예 — 횟수 한도 없음.** 람다 `+0x899 JNZ → +0x6b4` (목록 셋 · 레거시 글을 비우고 `Connect` 다시) | 없음 (끝없이) | `RetryRPC +0x4 MOV EDX,0x6bb` · `+0x9 CMP` · `+0xb JZ +0x21` · `+0x21fa8e ~ +0x21fa9f MOV EAX,1` |
| 그 밖 양수 N (`0x6b5` · `0x6ba` · `0x6be` · `0x6bf` · `0x57` · `0xe` …) | 0 | `0x80070000 \| (N & 0xffff)` | 아니오 | `0x8007NNNN` → `+0x8bc` 줄 `0x384` → 람다 반환 → `GetConnection` → `CoInitializeSecurity +0x486 JS +0x89c` → `+0x8a9` 같은 값 | `CheckStatus +0x27 TEST EBX,EBX` · `+0x29 JLE` · `+0x2b MOVZX` · `+0x2e OR EBX,0x80070000` |
| 상태 5 | 0 | `0x80070005` | 아니오 | 위와 같되 `GetConnection +0xc5 CMP EAX,0x80070005` · `+0xca JZ` → 떨어진 갈래 (`+0x1bc84c`, 안 읽음 — **Q257b**) | `GetConnection +0xc5 / +0xca` |
| 음수 | 0 | 그대로 | 아니오 | 그 값 | `CheckStatus +0x29 JLE` |
| **bind 단계 fault** (넷 중 어느 것이든, 상태 N) | **안 부름** | **안 부름** — `CreateFromBindingString +0x16b MOVZX EBX,DI` · `+0x16e OR EBX,0x80070000` (Handle 쪽은 `+0x14b731 / +0x14b734`) 이 같은 규칙 | 아니오 | `0x8007NNNN` → 람다 `Return_HrMsg` (줄 `0x300` / `0x306` / `0x30a` / `0x30e`). **N = `0x6b5` 면 `0x800706b5`** (= 요청 쪽 로그의 `CoInitializeSecurity` 값) | 람다 `+0x341 ~ +0x34a` → `+0xcb8` |
| 상태 0 인데 `_ph` / DSA / `gdwScmProcessID` / `_ProcessSignature` 가 0 | | | 아니오 | **`0x8000ffff`** + 텔레메트리 호출, 줄 `0x387` / `0x388` / `0x389` / `0x38a` | `+0x8c6 ~ +0x913` · `+0xb37 ~ +0xb4f` |
| 레거시 글 < 2 글자 · DSA 객체 실패 · SD 만들기 실패 | | | 아니오 | `0x8000ffff` (`0x398`) · `0x8007000e` (`0x38e`) · 그 값 (`0x39c`) | `+0x983 ~ +0x993` · `+0x959 ~ +0x967` · `+0x9b8 ~ +0x9c3` |
| 암묵 길 (`InitRemoting` → `InitializeSecurity`) 에서 `0x80070057` | | | 아니오 | **`0x80040153`** (`[이름 추측]`) 로 바뀜. 다른 음수는 그대로 + `UninitializeSecurity` | `InitializeSecurity +0x83 CMP · +0x88 JZ · +0x90 MOV EBX,0x80040153 · +0x95` |

- **정직한 실패로 쓸 수 있는 값**: `0x6bb` 를 뺀 아무 양수. 가장 자연스러운 것은 **`0x6ba`** (rpcss 가 안 떠 있을 때와 같은 HRESULT `0x800706ba`) — 결과서 권고. **피할 것**: `0x6bb` (끝없음) · `5` (따로 갈래) · "0 인데 빈 응답" (`0x8000ffff` 로 끝나긴 하나 텔레메트리 호출).
- fault 응답으로 이 상태를 주면: 클라이언트 `SanitizeErrorCodeFromServer` 는 0 · 치명 14종만 `0x6c0` 으로 바꾸고 그 밖은 그대로 넘긴다 (run133 1-3) → **`0x6bb` 도 그대로 닿는다** (금지 꼴이 실제로 걸림). 전송 쪽 오류 (ALPC → RPC, run133 2-5 표) 에는 `0x6bb` 로 가는 줄이 없다 — 서버가 직접 줄 때뿐.

### 4. 그다음 지도 (물음 3) [정적]

- **4-a. `CoInitializeSecurity`** — `+0xa6 / +0xac` (`gGotSecurityData == 0`) → `+0x47d GetConnection` → `+0x486 JS +0x89c` (실패면 `+0x8a9` 같은 값) / 성공 `+0x48c ~ +0x494 JNZ +0xb2` 로 본 줄기 합류. 이 포트로 더 가는 것은 **`+0x3f6 ~ +0x43c` 한 곳**: `g_AppId` (`0x336fa8`) ≠ `GUID_NULL` 이면 `+0x420 I_RpcSetDCOMAppId` → **`+0x43c SetAppID`** = 프로시저 1 (`[in] _ph`, `[in] GUID*` — `SetAppID +0x25 MOV R9,[_ph]` · `+0x2c LEA RDX,[…ProcFormatString+0xe0]`). `g_AppId` 를 정하는 쪽은 `+0x64a utGetAppIdForModule` · `+0x662 wCLSIDFromString` (안 읽음). 보통 exe 는 AppID 가 없어 `GUID_NULL` 일 것 `[추측]`.
- **4-b. `CoInitializeEx`** — 지도 `_map/CoInitializeEx_*` (깊이 10, 함수 514) 에 `GetConnection` · `Connect` · `SetAppID` 가 없다. 이 포트로 닿는 것은 되돌리기 길의 `Disconnect` (10) · `ServerFreeOXIDAndOIDs` (8) 뿐 (`… → CRpcResolver::Cleanup → Disconnect` — 이름 `Cleanup` 이 겹쳐 중간 고리는 `[추측]`). → **`CoInitializeEx` 만으로는 `Connect` 가 안 나간다** (간접 호출 33곳은 못 봄 `[공백]`).
- **4-c. 첫 COM 사용의 첫 부름**:
  - (a) **in-proc `CoCreateInstance`** — `CoCreateInstance +0xd1 → CoCreateInstanceExCopy +0x162 → DoCreateInstance +0x1d7 → ICoCreateInstanceExWorker +0xb52 → GetActivationPropertiesIn +0x1fa → CoGetDefaultContext +0x3f → InitChannelIfNecessary +0x119 → InitRemoting` (앞 다섯 고리는 `_map/CoCreateInstance_edges.tsv` 줄, 뒤는 `.asm`). `InitRemoting +0xb6 InitializeSecurity` → `+0x7c CoInitializeSecurity` **암묵 호출** (인자 `ECX 0 · EDX −1 · R8 0 · R9 0 · [RSP+0x20] 1 · [+0x28] 2 · [+0x30] 0 · [+0x38] 8 · [+0x40] 0`) → bind ×4 + `Connect`, 이어 `+0x29d CallTheResolver` → **프로시저 7** (`+0x106 NdrClientCall2`, 서버 `_ServerAllocateOIDs`) → `+0x1b7 RegisterOXIDAndOIDs` → **프로시저 6** (`ServerRegisterOXID`). 그래서 `CoInitializeSecurity` 를 직접 안 부른 프로세스도 `CoCreateInstance` 한 번에 같은 `0x800706b5` 를 만난다.
  - (b) **`CoGetObject`** — ole32 쪽이라 안 열었다 `[공백]`. 어느 길이든 처음엔 같은 `GetConnection` 에서 막히는 모양. ROT 후보 첫 부름 `IrotGetObject` (IROT 3, `GetConnection +0x23`) · `IMgotGetObject` (IROT 9).
  - (c) **out-of-proc · 서비스** — `DelegateActivationToSCM +0x156 BindToSCMProxy` → `+0x17 GetConnection` · `+0x49 MakeSCMProxy` · `+0x81 MakeCloakedSCMProxy` = **ILocalObjectExporter 가 아니라 ORPC 프록시** 로 rpcss 활성화 쪽. `DelegateActivationToSCM +0x1ac` 가 `_ProcessSignature` 를 읽는다. "클래스 · 서비스 없음" HRESULT 길은 안 읽음 `[공백]`.
- **4-d. `GetConnection` 을 부르는 곳 — 46곳** (`_refs/0xa14d0.tsv`, call 46 + 데이터 2): `CoInitializeSecurity +0x47d` · 해결사 쪽 8 (`GetUpdatedResolverBindings` · `GetDefaultSecurityPermissions` · `ClientRegisterOIDWithPingServer` · `ClientDeRegisterOIDFromPingServer` · `GetLocalMIDEntry` · `FindOrCreateMIDEntry` · `GetSystemSecurityDescriptors` · `EnsureWorkerThread`) · `BindToSCMProxy +0x17` · IROT 11 (`Irot*` · `IMgot*`) · ISCM · IMachineActivatorControl 25 (`NotifyStarted` · `GetThreadID` · `IsObjectCreationAllowed` · `RetireServer` …). 목록 전부는 결과서 4-d.
- **4-e. ILocalObjectExporter 프로시저 표** (결과서 — 클라이언트 · 서버 프로시저 문자열을 차례로 풀어 매김, 두 쪽 파라미터 수 · 속성 · 스택 칸 열두 개 모두 같음)

| # | combase 스텁 (rva) | rpcss 서버 (rva) |
|---|---|---|
| 0 | `Connect` (`0x12f318`) | `_Connect` (`0x76d80`) |
| 1 | `SetAppID` (`0x7f818`) | `_SetAppID` (`0x6d340`) |
| 2 | `GetDefaultSecurityPermissions` (`0x18d514`) | `_GetDefaultSecurityPermissions` (`0xad320`) |
| 3 | `ServerGetReservedID` (`0x9478c`) | `_AllocateReservedIds` (`0x6d220`) |
| 4 | `ClientBulkUpdateOIDWithPingServer` (`0x40110`) | `_BulkUpdateOIDs` (`0x21e70`) |
| 5 | `ClientResolveOXID` (`0x1355e0`) · `ClientResolveMachineLocalOXID` (`0x99c64`) | `_ClientResolveOXID` (`0x67880`) |
| 6 | `ServerRegisterOXID` (`0xa1e68`) | `_ServerAllocateOXIDAndOIDs` (`0x13990`) |
| 7 | `CComApartment::CallTheResolver` (`0x37608`) | `_ServerAllocateOIDs` (`0x213e0`) |
| 8 | `ServerFreeOXIDAndOIDs` (`0x123504`) | `_ServerFreeOXIDAndOIDs` (`0x15bc0`) |
| 9 | `SetServerOIDFlags` (`0x12b010`) | `_SetServerOIDFlags` (`0x12120`) |
| 10 | `Disconnect` (`0x3e884`) | `_Disconnect` (`0x6ad70`) |
| 11 | `GetUpdatedResolverBindings` (`0x18dca4`) | `_GetUpdatedResolverBindings` (`0xafd40`) |

### 호출 관계

```
combase!CoInitializeSecurity (0x7e880)                                                       [정적]
 ├─ (1) +0x482  CRpcResolver::GetConnection (0xa14d0)      gGotSecurityData == 0 · _dwFlags 비트 0 꺼짐
 │              └─ +0xc3  operator() (0x18b9c0)
 │                        ├─ +0x2b7  CreateWellKnownSid(24)                 요구 서버 SID = NetworkService
 │                        ├─ +0x341  CFastBH::CreateFromBindingString       ILocalObjectExporter v2.0
 │                        │          ├─ +0x80  RpcStringBindingParseW("ncalrpc:[epmapper,…]")
 │                        │          ├─ +0xcf  RpcBindingCreateW            정적 끝점 · LRPC 3
 │                        │          └─ +0xea  BindWithTimeout +0x13 RpcBindingBind(NULL, …)  ── ALPC ──▶ (svchost rpcss) bind
 │                        ├─ +0x3cf · +0x3ee · +0x46d  CFastBH::CreateFromBindingHandle   IMachineActivatorControl · IROT · ISCM
 │                        │          └─ +0x2a RpcBindingCopy · +0x41 RpcBindingUnbind · +0x59 BindWithTimeout
 │                        ├─ +0x436  IsDeveloperLicenseInstalled   · +0x54d RegisterWithPsmIfNecessary · +0x630 SuspendImpersonate
 │                        ├─ +0x833  Connect (0x12f318) +0x182 NdrClientCall2 (프로시저 0)  ── ALPC ──▶ rpcss!_Connect
 │                        ├─ +0x889  RetryRPC           0x6bb → Sleep(100) → +0x6b4 (다시, 한도 없음)
 │                        ├─ +0x8af  CheckStatus        N → 0x8007NNNN
 │                        ├─ (검사)  _ph · DSA · gdwScmProcessID · _ProcessSignature  0 이면 0x8000ffff
 │                        ├─ +0x9b8  InitializeSystemSecurityDescriptors
 │                        └─ +0xa83  gGotSecurityData = 1 · +0xae4 _dwFlags |= 1
 └─ (2) +0x441  SetAppID (프로시저 1)                       g_AppId ≠ GUID_NULL 일 때만

(첫 COM 사용) … CoGetDefaultContext +0x44 InitChannelIfNecessary +0x11e CComApartment::InitRemoting      [정적]
 ├─ +0xbb   InitializeSecurity +0x81 CoInitializeSecurity   (암묵 — 위 길 전부)
 └─ +0x2a2  CallTheResolver
            ├─ +0x10d  NdrClientCall2 (프로시저 7)  · +0x11c RetryRPC · +0x127 CheckStatus
            └─ +0x1bc  OXIDEntry::RegisterOXIDAndOIDs → ServerRegisterOXID (프로시저 6)

rpcss!_Connect (0x76d80)                                                                      [정적]
 ├─ +0x166  CheckLocalCall · +0x17c RetrievePIDForLocalClient      실패 → 5
 ├─ (+0x186 winsta 글 NULL · 출력 NULL · 깃발)                        → 0x57
 ├─ +0x37f GetClientServerSvcs · +0x392 UpdateChannelHooks · +0x3c2 CopyMyOrBindings
 └─ +0x52c CProcess · +0x588 AllocateReservedIdsInternal · +0x5a2 GetCurrentProcessId
rpcss!UseProtseqIfNecessary +0x245 RpcServerUseProtseqEpExW   gaProtseqInfo 칸 16 = ncalrpc · epmapper
RpcEpMap!InitializeService +0x28 UseProtseqIfNecessary(16)    같은 칸 → 한 포트를 나눠 씀
```

- (나무 오프셋은 ret 자리. `── ALPC ──▶` 는 다른 프로세스로 가는 RPC — 연결 · 메시지 길은 rpcrt4 안이라 이번에 안 읽음. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 우리 (PE_emulator) 맞나 · 틀리나")

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/rpc/lrpc.cpp` (359줄) · `epm.cpp` (47줄) 를 줄 번호만 훑었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| `lrpc.cpp:74 ~ :77` 창구가 아는 인터페이스 (`window.definition->interfaces`) 와 UUID · 버전이 안 맞으면 fault `rpc_s_unknown_if` = `0x6b5` (`:21`). 창구 정의 `epm.cpp:33 ~ :44` 는 EPM `e1af8308…` v3.0 하나 (`:38 ~ :39`) | 진짜 combase 는 같은 포트에 **EPM 이 아닌 네 인터페이스** 를 bind (1 번 `e60c73e6…` v2.0). 하나라도 모르면 그 자리에서 `0x800706b5` | **맞음 — 지금 대답은 안전한 정직한 실패.** bind 단계 fault 는 `RetryRPC` 에 안 닿고 (3절 마지막에서 둘째 줄) 재시도 없이 끝난다. 요청 쪽 로그 "한 API 안 거절 1" 과 같은 모양 |
| `lrpc.cpp:79 ~ :80` `syntax = (mask & 2) ? 2 : (mask & 1)` + 창구 마스크 1 (`epm.cpp:42`) | 이 클라이언트는 NDR 2.0 전용 (`NdrClientCall2`), 제안 마스크 `1` 짐작 (1-3) | **맞음.** 새 인터페이스를 올릴 창구도 마스크 `1` |
| `lrpc.cpp:102 ~ :103` 요청 → `bindings[슬롯]` 로 인터페이스를 찾은 뒤 `procedures` 를 **`opnum` 만** 비교 | 같은 포트에 인터페이스 다섯 (EPM + 넷) 이면 번호가 **겹친다**: EPM `8` = `ept_map_auth_async` vs ILocalObjectExporter `8` = `ServerFreeOXIDAndOIDs` · IROT 0 ~ 10 · ISCM 0 ~ 26 … | **틀려질 예정.** 프로시저 표를 **(인터페이스 UUID, 번호)** 로 찾아야 한다 (`:100` 에서 이미 `uuid` 를 꺼내 둠 — 비교에 안 씀) |
| `lrpc.cpp:83 ~ :84` `connection.bindings[슬롯]` | 한 연결에 bind 넷, 슬롯 서로 다름 `[추측: 같은 연결 재사용 — Q283 D2]` | 맞음 (새 연결이어도 연결마다 같은 코드) |
| `lrpc.cpp:186` 동기 연결 (`flags & synchronous`) 은 창구가 있으면 받음 | 이 길은 동기 bind (`RpcBindingBind(NULL, …)`) · 동기 스텁 (`NdrClientCall2`). 요구 서버 SID 가 와도 받음 | 맞음 |
| `epm.cpp:41` 프로시저 8 하나 | 이 포트의 다른 인터페이스 몸은 아직 없음 | 짓는 중 |

**성공 쪽을 만들 때 stub 이 돌려줄 최소 대답** (결과서 그대로, 칸 근거는 2-2 표):

1. **연결**: 동기 연결을 받음 (`RequiredServerSid` = NetworkService SID 가 와도 — 이미 됨).
2. **bind 넷** (1-2 표의 UUID · 버전): 각각 bind 답 = 종류 1 · `+0x48 = 1` (NDR 2.0) · `+0x08` = 서버 프로세스 ID (0 아닌 값, 아래 `gdwScmProcessID` 와 같게 `[추측]`) · 슬롯 = 제안 그대로. **넷 중 하나라도 거절하면 `0x800706b5` 로 끝** — 넷을 한꺼번에 올려야 한다.
3. **`Connect` (ILocalObjectExporter 슬롯, 번호 0) 응답 최소 칸**: 컨텍스트 핸들 (속성 0 + 0 아닌 GUID) · 핑 360 · DSA (비-NULL 구조체) · MID (0 아닌 값) · OID 배열 열 칸 0 + 개수 **0** · 설정 비트 0 · 컨테이너 0 / 0 · 레거시 글 NULL · 인증 2 · 가장 2 · 서비스 목록 셋 (개수 0 / NULL) · 스레드 번호 (1 부터) · **PID (0 아님)** · **서명 (0 아닌 u64)** · 프로세스 GUID · SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL · **상태 0**. `TODO(VM)` 로 둘 것: DSA 내용 · 설정 비트 · 서비스 목록 · SD 조각 (진짜 값은 Q283 D5).
4. 값을 "지어내는" 칸은 서버 **자기 정체** (핸들 · PID · 서명 · GUID · MID · 스레드 번호) 뿐. 시스템 설정 사실을 지어내는 칸은 없다 (2 / 2 · 360 · 0 은 rpcss 코드의 기본값 `[정적]`).
5. 그 뒤 프로시저 (7 · 6 · 10 · 1 …) 는 몸을 모르면 fault — 상태는 `0x6ba` 를 권한다.

**금지 꼴** (결과서 1 ~ 8):

1. **fault 상태 `0x6bb`** (또는 서버 반환 `0x6bb`) — `RetryRPC` 가 100 ms 마다 **끝없이** 다시 건다 (3절). `SanitizeErrorCodeFromServer` 가 안 막는다.
2. **응답 없음** — 동기 호출이 영영 안 돌아온다 (호출 시간제한 `0xffffffff` 를 무한으로 짐작 `[추측]`).
3. **상태 0 인데 `_ph` · DSA · `gdwScmProcessID` · `_ProcessSignature` 가운데 하나가 0 / NULL** — `0x8000ffff` + 텔레메트리. 컨텍스트 핸들 20 B 를 다 0 으로 주면 NULL 핸들.
4. **예약 OID 개수 > 10** — `_OidsReserved` (0x50 B) 넘쳐 쓰기.
5. **SD 크기 합 > 총 크기, 또는 크기가 있는데 덩어리 NULL** — 넘쳐 읽기.
6. **bind 답 문법 `2` (NDR64)** — 이 클라이언트는 NDR 2.0 전용.
7. **상태 `5`** — `GetConnection +0xc5` 의 따로 갈래 (Q257b, 안 읽음).
8. **bind 답 마스크 `3`** — "비트 하나" 검사에 걸려 `0x6c0` (run133 1-3).

**권하는 실패 값**: `Connect` 이후 프로시저 몸이 없을 때는 **`0x6ba`** (rpcss 가 없을 때와 같은 `0x800706ba`, 한 번에 끝남). 지금의 bind 단계 `0x6b5` 도 깨끗이 끝나므로 그대로 둬도 안전하다.

**"그다음 벽" 한 줄**: `Connect` 가 성공하면 **첫 COM 사용 (`CoCreateInstance`, in-proc 이어도) 에서 `InitRemoting → CallTheResolver` 가 같은 연결로 프로시저 7 → 6 (OID 할당 · OXID 등록) 을 바로 보낸다.** 그 몸을 짓기 전에는 7 번에서 `0x6ba` 류로 끝내는 것이 안전하다 — 그러면 `CoInitializeSecurity` 는 성공하고 첫 COM 사용이 `0x800706ba` 류로 실패한다 (지금의 `CoInitializeSecurity = 0x800706b5` 와 증상이 달라져 로그 비교가 달라 보일 수 있다). 그다음은 활성화 (`DelegateActivationToSCM` → ORPC) · ROT (IROT 3 · 9).

**정적으로 못 닫은 것** (결과서 그대로):

- `[공백]` bind 2 ~ 4 번이 같은 연결을 쓰는지 (`PickAssociation` 비교 줄 일부만 봄).
- `[공백]` DUALSTRINGARRAY 진짜 내용과 뒤 쓰임 (`MakeSCMProxy` 가 이 글로 활성화 연결을 만드는 것으로 보임 `[추측]`).
- `[공백]` 설정 비트 · 서비스 목록 · 채널 훅 · SD 조각의 VM 진짜 값.
- `[공백]` `LocalInterfaceOnlySecCallback` (rva `0x1fbf0`, 2721 B) 이 로컬 ncalrpc 클라이언트를 거절하는 조건 — 거절하면 bind 가 5 로 끝나 Q257b 갈래.
- `[공백]` `IsDeveloperModeEnabled` (kernelbase) 가 읽는 레지스트리 (`KIsUnlockSettingEnabled` 안 읽음).
- `[공백]` `CoGetObject` (ole32) 첫 부름 · 활성화 "클래스 없음" HRESULT 길.
- `[공백]` 응답 선 위 진짜 바이트.

## 확인된 것

- `[정적]` combase 의 RPCSS 연결은 바인딩 글 `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` 에 끝점을 박아 **`ept_map` 을 안 묻는다**. bind 는 `RpcBindingBind(NULL, …)` 로 **동기**.
- `[정적]` 한 번의 `GetConnection` 에서 bind 넷 — `ILocalObjectExporter` (`e60c73e6-88f9-11cf-9af1-0020af6e72f4` v2.0, **PDB 이름으로 확정**) · `IMachineActivatorControl` v1.0 · `IROT` v0.2 · `ISCM` v0.2, 넷 다 NDR 2.0 하나. 하나라도 실패면 `0x8007NNNN` 으로 끝, 실패를 기억하는 칸 없음.
- `[정적]` 요구 서버 SID = NetworkService, 가장 3 · 인증 6 · 서비스 10 — 연결 쪽에만 실리고 bind 메시지엔 없음.
- `[정적]` 진짜에서 받는 모듈 = `rpcss.dll` (`StartObjectExporter +0xb65`, 로컬 전용 · 동기 서버 스텁). `epmapper` 끝점은 rpcss.dll · RpcEpMap.dll 이 같은 표 칸 16 으로 함께 연다.
- `[정적]` 첫 프로시저 = ILocalObjectExporter **0 번 `Connect`** (입력 6 · 출력 25 · 상태). combase 가 **반드시 0 아니어야 하는 칸 넷**: `_ph` · DSA 포인터 · `gdwScmProcessID` · `_ProcessSignature`. 예약 OID 개수 · SD 크기 합은 안 막음. 성공 표시 `gGotSecurityData = 1` 은 모든 검사 뒤.
- `[정적]` `RetryRPC` 는 `0x6bb` 일 때만 `Sleep(100)` 뒤 다시 (한도 없음), `CheckStatus` 는 양수 N → `0x8007NNNN`. bind 단계 fault 는 재시도 없이 같은 규칙으로 끝.
- `[정적]` 진짜 rpcss `_Connect` 는 winsta 글 NULL · 출력 NULL · 깃발 이상이면 `0x57`, 로컬 호출 아님 · PID 못 얻음이면 `5`.
- `[정적]` `CoInitializeSecurity` 는 이 포트로 `Connect` 뒤 `SetAppID` (1) 만 더, `CoInitializeEx` 는 초기화 길에서 이 포트로 안 감, 첫 `CoCreateInstance` 는 암묵 `CoInitializeSecurity` 뒤 프로시저 7 → 6.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 combase · rpcrt4 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`) — 이번 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다. **어긋난 곳 넷** (이 문서는 고친 값을 씀) + 보탬:

1. **인터페이스 구조체를 다는 줄** — 결과서 1-2 "람다 `+0x14c …` 가 `0x1802fcd00` 을 `CFastBH` 첫 칸에" → `+0x14c` 는 `CFastBH` 0x18 B 의 `HeapAlloc` call (ret `+0x153`). 구조체를 잡는 줄은 **`+0x15d LEA RCX,[0x1802fcd00]`** (나머지 셋 `+0x1a0` · `+0x1e3` · `+0x222`).
2. **`RpcBindingUnbind`** — 결과서 1-2 "`+0x35 RpcBindingUnbind` (ret `+0x46`)" → `+0x35` 는 인자 `MOV RCX,[RSP+0x30]`, **call 은 `+0x3a` (ret `+0x41`)**, `+0x46` 은 `MOV EDI,EAX`.
3. **`_ProcessSignature == 0` 줄** — 결과서 2-2 "`+0x913 MOV EDX,0x38a`" → **`+0x90e MOV EDX,0x38a`** (`+0x909` 텔레메트리 call, `+0x913` 은 `JMP +0xb41`).
4. **레거시 글을 넣는 줄** — 결과서 2-2 "`+0x99f ~ +0x9a3` `gLegacySecurity` 에 넣음" → 전역 주소는 **`+0x998 LEA RCX,[gLegacySecurity]`**, `+0x99f` 는 지역 칸 비우기, `+0x9a3` 은 unique_ptr 바꾸기 공용 도우미 call (ret `+0x9a8`). 이 도우미 (rva `0x5e9f0`) 는 별칭 무리 — `pdb_aliases.py --lookup combase.dll 0x5e9f0` 은 **11개**, Ghidra `.asm` 주석은 `ALIASES(14)` 라 수가 다르다 (Ghidra 가 템플릿 이름을 더 셈 `[추측]`). 하는 일로 적었다.
5. (보탬) **첫 bind 의 call** — `CreateFromBindingString +0xe5 BindWithTimeout` (ret `+0xea`) 은 결과서 표에 없던 줄.
6. (보탬) **`rpcss!_Connect +0x3dd ~ +0x4ae`** (결과서 2-2 SD 조각 범위) — `+0x3dd` 는 `LookupOrCreateTokenForRPCClient` call, `+0x4ae` 는 명령 경계가 아니다 (`+0x4aa HeapAlloc` call, ret `+0x4b1`). SD 다섯 조각 줄은 결과서의 디컴파일 근거 그대로 두고 범위만 `+0x3dd ~ +0x4aa` 로 읽는다.
7. (보탬) **`CoCreateInstance` 사슬 앞 다섯 고리** (`CoCreateInstance +0xd1` · `CoCreateInstanceExCopy +0x162` · `DoCreateInstance +0x1d7` · `ICoCreateInstanceExWorker +0xb52` · `GetActivationPropertiesIn +0x1fa`) 는 `.asm` 이 없어 **`_map/CoCreateInstance_edges.tsv` 줄** 로만 맞췄다 (지도는 이름 기준 — 결과서 스크립트 문제 2. 다섯 이름은 지도에서 "alias 1개 더" = 장식 이름 표시). `ICoCreateInstanceExWorker` 는 `+0xc97` 에서도 `GetActivationPropertiesIn` 을 부른다.
8. (보탬) **`InitializeSecurity`** — `0x80070057` 를 `+0x90` 에서 `0x80040153` 로 바꾼 뒤에도 `+0x95 UninitializeSecurity` 를 부른다 (다른 음수와 같은 줄로 감). 0 이상이면 `+0x8c JNS → +0x16` (뮤텍스 풀고 반환).
9. (보탬) **`GetConnection` 부르는 곳 46** — `_refs/0xa14d0.tsv` 48줄 = call 46 + 데이터 2 (`0x340840` · 되감기 정보). 람다 `0x18b9c0` 은 call 1 (`GetConnection +0xbe`) + 데이터 1.
- 그 밖 표의 줄 (람다 `+0x2ad ~ +0x34a` · `+0x823 ~ +0x8c6` · `+0x8de ~ +0x8f6` · `+0x959 ~ +0x98e` · `+0x9b3 ~ +0x9c3` · `+0xa5b ~ +0xaeb` · `+0xb37 ~ +0xb9f` · `+0xcb8`, `Connect` 스텁 14줄, `RetryRPC` · `CheckStatus` 전부, `CreateFromBindingString` 13줄, `BindWithTimeout`, `GetConnection` · `CoInitializeSecurity` · `InitializeSecurity` · `InitRemoting` · `CallTheResolver` · `BindToSCMProxy` · `DelegateActivationToSCM` · `SetAppID` · `InitializeSystemSecurityDescriptors`, rpcss `_Connect` 21줄 · `StartObjectExporter` · `UseProtseqIfNecessary`, RpcEpMap `InitializeService`, rpcrt4 `BaseBind` · `PickAssociation`) 은 **일치**. 점프 목적지 (`+0x34a → +0xcb8` · `+0x899 → +0x6b4` · `+0x8e2 → +0xb37` · `+0x967 → +0x562` · `GetConnection +0xca → +0x1bc84c` · `CoInitializeSecurity +0xac → +0x47d` · `+0x486 → +0x89c` · `+0x494 → +0xb2` · `_Connect +0x18a → +0x733` · `+0x168 / +0x180 → +0x73a`) 도 주소 빼기로 맞음.
- PE_emulator 줄 번호: 결과서 표 "`lrpc.cpp:99 ~ :103`" 는 `:99` 가 슬롯 못 찾음 fault, **opnum 비교는 `:102 ~ :103`** (오케스트레이터가 준 번호와 같음). `epm.cpp:42` 마스크 1 · `:41` 프로시저 8 · `lrpc.cpp:21` `0x6b5` · `:186` 동기 연결 — 줄 번호 맞음.

## 앞 run 고쳐 읽기

틀린 것은 없고 `[추측]` · `[공백]` 이 정적으로 닫히거나 끝이 이어진 것이라 "정정" 이 아니라 "고쳐 읽기" 로 각 문서 맨 위에 한 줄씩 넣었다.

- **run136**: (a) "`Connect` 안 (RPCSS 쪽 RPC · epmapper 길 — run129 · run133 의 rpcrt4 몫과 이어질 것 `[추측]`)" → **`[정적]`**: 바인딩 글 `ncalrpc:[epmapper,…]` 로 `ept_map` 없이 곧장, bind 넷 (`e60c73e6…` = `ILocalObjectExporter` v2.0 은 PDB 이름) 뒤 프로시저 0 (1 · 2 · 4절). (b) "핸들 단계 0 → 0 + 글 NULL → `Connect(NULL)`" 길의 **진짜 끝**: rpcss `_Connect +0x186` 이 winsta 글 NULL 이면 상태 `0x57` → `CoInitializeSecurity` = `0x80070057` (2-3). combase 쪽 줄은 그대로 맞다.
- **run137**: "같은 비동기 틀을 RPCSS (`CoInitializeSecurity`) 도 쓴다 (원 요청 말) `[추측: RPCSS 쪽 연결 객체가 같은 썽크를 쓰는지는 안 읽음]`" → combase 의 RPCSS 길 (bind 넷 + `Connect`) 은 **동기** 다 `[정적]`: `BindWithTimeout +0xc RpcBindingBind(NULL, …)` (비동기 상태 NULL), 스텁 `NdrClientCall2`. 비동기 완료 포트 틀은 이 길에서 안 쓴다. 연결 깃발 자체는 rpcrt4 안이라 안 읽었고, 요청 쪽 로그의 `Flags 0x20000` (동기) 과 맞는 모양 (에뮬레이터 로그라 `[관찰]` 아님).

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (combase · rpcrt4 안 올림) 이고, RPCSS 는 다른 프로세스 (svchost) 다.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" D1 ~ D11 — 대기열 **Q283**. **D2 · D3 · D5 · D7 필수**, D4 · D6 값 읽기, **D8 ~ D11 선택**. 멈춤점은 test.exe 가 아니라 COM 을 쓰는 프로세스 (`schtasks.exe` · `taskkill.exe` 같은 자식이나 COM 보안을 여는 GUI 프로세스) 조건, 서버 쪽 (D8 · D9) 은 svchost (rpcss) 조건. 고르기는 사용자 결정 — Q256 · Q271 · Q280 의 RPC 묶음과 같은 세션에 얹을 꼴):

1. **D1** `combase!CRpcResolver::GetConnection+0xbe` — 처음 연결 길인지 (`_dwFlags & 1 == 0`).
2. **D2 (필수)** `rpcrt4!LRPC_CASSOCIATION::Bind+0x626` (call) / `+0x62d` (ret) 와 `NtAlpcConnectPort` 입구 — 한 `GetConnection` 안에서 연결 몇 번 · bind 몇 번 (UUID 앞 4 B · `+0x48` 마스크 · `+0x4c` 슬롯). 1-2 의 "같은 연결, bind ×4" `[추측]` 을 닫음.
3. **D3 (필수)** `rpcrt4!CompleteBind` 입구 `db @rdx L0x70` — 진짜 rpcss 가 고른 `+0x48` (1 인가) · `+0x08` PID.
4. **D4** `combase+0x18c1ee` (람다 `+0x82e`) — `rcx` · `du @rdx` · `du @r8` · `r9` (NULL 인가) · `[rsp+0x20]` 깃발 · `[rsp+0x28]` `0x8664`.
5. **D5 (필수)** `combase+0x18c1f3` (`+0x833`) — `eax` 상태, 이어 `_ph` · `gdwScmProcessID` · `_ProcessSignature` · `giPingPeriod` · `gLocalMid` · `gAuthnLevel L2` · `_cReservedOidsAvail` · `_OidsReserved L10` · `_pdsaLocalResolver` — 2-2 표의 진짜 값.
6. **D6** `combase+0x18c26f` (`+0x8af`, `CheckStatus` 반환) — `eax`.
7. **D7 (필수)** 같은 실행의 응답 스텁 바이트 — `rpcrt4!DoSendReceive+0x154` 반환 뒤 `UnpackResponse` 입구 (run133 2-3), 응답 `+0x40` 부터 (조건 길이 · 정렬 · 포인터 id).
8. **D8 (선택, 서버)** `rpcss!_Connect+0x73f` (공통 반환 `eax`) · `+0x59b` · `+0x562` · `+0x573` — 서버가 주는 값 · winsta NULL 이 `0x57` 인지.
9. **D9 (선택, 서버)** `rpcss!LocalInterfaceOnlySecCallback` 반환 — 0 / 5 (Q257b 와 묶음).
10. **D10 (선택)** `combase!IsDeveloperLicenseInstalled+0x10` `eax` · `kernelbase!IsDeveloperModeEnabled` 가 읽는 키.
11. **D11 (선택)** `combase!CComApartment::CallTheResolver+0x10d` · `ServerRegisterOXID` 의 `NdrClientCall2` ret — 첫 COM 사용의 둘째 · 셋째 부름 상태.

정적 (결과서 "다음 요청 후보" 1 ~ 6 — 대기열 **Q284**. Q257b (창 스테이션 커널 본문 · `E_ACCESSDENIED`) 에는 보태지 않음):

1. **rpcss 프로시저 7 · 6 · 10 · 1 몸** (`_ServerAllocateOIDs` · `_ServerAllocateOXIDAndOIDs` · `_Disconnect` · `_SetAppID`) 의 입력 · 출력 꼴과 combase 쪽 검사 (`CallTheResolver +0x117 ~ +0x1b7` · `RegisterOXIDAndOIDs`) — 첫 COM 사용의 벽.
2. **`LocalInterfaceOnlySecCallback`** (rva `0x1fbf0`, 2721 B) — 로컬 클라이언트 거절 조건.
3. **`MakeSCMProxy` · `DelegateActivationToSCM`** — `_pdsaLocalResolver` 가 어떤 문자열 바인딩을 기대하나, 활성화 첫 ORPC 프로시저 · "클래스 / 서비스 없음" HRESULT 길.
4. **ole32 `CoGetObject`** (모니커) 의 첫 RPC · ROT `IrotGetObject` (IROT 3) 요청 · 응답 꼴.
5. **rpcrt4 `PickAssociation`** 비교 줄 전부 · `RpcBindingCopy` / `RpcBindingUnbind` — bind 넷이 연결을 나눠 쓰는지 (D2 와 겹침).
6. `KIsUnlockSettingEnabled` — `Connect` 앞 개발자 모드 검사가 읽는 레지스트리.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 (bind · `Connect` · 응답 검사 · 실패 처리) 에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`. 추적 켜짐 (`gfEnableTracing` · TraceLogging 공급자) 은 `RetryRPC` · `CheckStatus` 에서 기록만 더하고 반환은 안 바꾼다 (`CheckStatus +0x15 ~ +0x71` → `+0x27` 로 합류).
- **스크립트 문제 둘** (결과서 — 하네스 제안 거리, flow-writer 는 `.claude/` 안 고침):
  1. `ExportFunctions.java` 는 이름 정규식으로만 고른다 — combase `operator()` 70개 · `Connect` 5개. 결과서가 스크래치 복사본에 `rva:` 선택자를 넣어 뽑음. 제안: `rva:` 접두사 · 이름에 `(` `<` 가 있으면 파일 이름에 늘 `__<rva>`.
  2. `CallGraphMap.java` 의 `_edges.tsv` 가 이름 기준이라 겹치는 이름 (`operator()` · `Release` · `Cleanup` · `InitRemoting`) 을 지나는 사슬을 믿을 수 없다 (예: 지도의 `ICoCreateInstanceExWorker +0x12f1 → operator() → Connect` 는 가짜). 제안: `caller_rva` · `callee_rva` 열.
- **읽을 때 조심**: `Connect.asm` 은 우리 것이 아니다 (rva `0xaef40`) — `Connect__12f318.asm`. `InitRemoting.asm` 이 rva `0x36ad4` (`InitRemoting__11e0fc.asm` 은 다른 것). 람다 `.asm` 의 `_calls.tsv` 줄은 caller 이름 `operator()` 로 다른 람다와 섞인다 — 람다 호출 지점은 `.asm` 의 `; CALL ret=` 주석으로.
- **별칭 수 차이**: rva `0x5e9f0` 을 Ghidra `.asm` 은 `ALIASES(14)`, `pdb_aliases.py` 는 11개로 센다 (flow-writer 대조 4).
- 같은 때 explainer 가 run148 해설서를 쓰는 중 — 이 문서는 그 파일을 안 건드렸다.
