# 144 com-rpcss — 정적 분석 결과

- 요청서: `_workspace/sast/144_com-rpcss.md` (Q257a). PE_emulator 원문 `../PE_emulator/_workspace/05_tuning_planner/367_internals-com-rpcss.md` 를 끝까지 읽고 시작했다.
- 빌드: Windows 11 24H2 26100.9457 x64. 날짜 2026-11-26.
- 표기: `[정적]` = 코드에 그렇게 쓰여 있다 (실행해서 본 것이 아님). `[규칙]` = 공개 NDR 규칙으로 정한 것 (서버 코드에 쓰는 줄 없음). `[추측]` · `[공백]` · `[이름 추측]` 은 따로 표시. 줄 근거 `+0x…` 는 함수 시작 기준, 부르기는 call 자리 (괄호 안이 돌아올 자리 `ret`), 점프 목적지는 "목적지 − 함수 시작". **핵심 호출 · 상수 줄의 `+0x…` (람다 · `RetryRPC` · `CheckStatus` · `CreateFromBindingString` · `rpcss!_Connect` 등 60여 줄) 는 원본 바이트 (`pefile`) 와 한 번 더 맞췄다** (아래 "버전·스크립트 대조"). 나머지는 `.asm` 줄 그대로.
- 폴더 이름: `ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/` 는 모듈 이름을 소문자로 쓴다 (`combase`, `rpcss`, `kernelbase`).
- 도는 작업: 없음 (내 Ghidra 는 전부 끝남). sast/142 · 143 · 145 의 프로젝트 (user32 · win32kfull · win32kbase · RpcEpMap · rpcrt4 · gdi32 · gdi32full · gdiplus) 는 열지 않았다. rpcrt4 · RpcEpMap 은 **있는 asm 읽기만**.
- **판정: 분석성공.** 요청 1 · 2 · 3 의 작은 물음마다 줄 근거와 함께 답했다. 정적으로 못 닫은 것 (진짜 응답 값 · 4 번 bind 가 연결을 나눠 쓰는지 · 보안 콜백 몸 등) 은 "동적으로 확인할 것" 에 모았다.

## 스크립트 문제

고치지는 않았다. 둘 다 "이름" 으로만 고르고 이어 붙이는 데서 나온 불편이다.

1. **`ExportFunctions.java` 는 이름 정규식으로만 고른다.** combase 의 람다는 이름이 `operator()` 인 것이 **70개**, `Connect` 가 5개 (rpcrt4 가 아니라 combase 안) 라서:
   - `operator()` 하나만 뽑을 수 없다 (정규식은 70개를 다 뽑는다). 요청서가 말한 대로 rva 로 뽑았다: 스크래치패드에 `ExportFunctions.java` 복사본 `…/scratchpad/s144/ExportFunctionsRva.java` 를 만들어 **세 군데만** 바꿨다 (정규식 자리에 `rva:0x18b9c0,0x…` 를 주면 그 rva 의 함수만 고름 · 이름에 `(` 나 `<` 가 있으면 파일 이름에 항상 `__<rva>` 를 붙임). 실행은 `…/scratchpad/s144/export_rva.sh combase.dll 'rva:0x18b9c0' 0`. 결과 파일: `ms/disassemble/combase/operator____18b9c0.asm` · `ms/decomp/combase/operator____18b9c0.c` (함수 시작 `0x18018b9c0` 크기 3558 B).
     - 바뀐 줄 (제안): `Pattern pat = Pattern.compile(args[2]);` 앞뒤에 `rva:` 접두사 해석 (`Long.decode` 로 쉼표 목록), 고르는 줄 `if (pat.matcher(f.getName()).find())` 를 `byRva ? rvaSet.contains(f.getEntryPoint().getOffset() - base) : pat.matcher(…)` 로, 파일 이름 `if (k > 1)` 를 `if (k > 1 || name.indexOf('(') >= 0 || name.indexOf('<') >= 0)` 로.
   - `Connect` 정규식 `^Connect$` 는 5개를 다 뽑는다: **`Connect.asm` 은 rva `0xaef40` (우리 것이 아님)**, 우리 RPC 스텁은 **`Connect__12f318.asm`**. 나머지 셋 (`__1421c0` · `__1907a0` · `__26baf0`) 은 16 B 짜리 COM 프록시 껍데기.
   - `_calls.tsv` 에는 람다 줄이 caller 이름 `operator()` 로 들어간다 (다른 람다 줄과 섞임). 람다의 호출 지점은 `.asm` 의 `; CALL ret=` 주석이 정확하다.
2. **`CallGraphMap.java` 지도는 이름 기준** (`_edges.tsv` 에 rva 가 없다). `operator()` · `Release` · `Cleanup` · `InitRemoting` (2개) 같은 겹치는 이름에서 서로 다른 함수가 한 칸으로 합쳐져, **그 이름을 지나는 사슬은 믿을 수 없다** (예: `CoCreateInstance` 지도의 `ICoCreateInstanceExWorker +0x12f1 → operator() → Connect` 는 가짜 — 람다 `0x18b9c0` 의 부르는 곳은 `GetConnection +0xbe` 하나뿐이다, `_refs/0x18b9c0.tsv`). 이 문서는 이름이 하나뿐인 함수로만 이어진 사슬만 썼고, 쓴 사슬은 `.asm` 의 call 줄로 다시 확인했다. 제안: `_edges.tsv` 에 `caller_rva` · `callee_rva` 열 추가.

## PDB GUID 확인 · 버전·스크립트 대조

| 모듈 | 바이너리 | PDB guid+age | 확인 |
|---|---|---|---|
| combase | `combase.dll` (이미 analyze, 다시 안 함) | `DA1DC0C4F35CEE58F672CBB20460D11D1` | `ms/sym/combase.dll.pdb.info.txt` = 요청서가 말한 기존 값과 **같음**. 바이너리 디버그 디렉터리 앞 16자 `DA1DC0C4F35CEE58` 도 같음 |
| **rpcss (새로)** | `rpcss.dll` | **`316298244AA2A8A6A520F13E7355EC211`** | `fetch_pdb.py` 출력 · `llvm-pdbutil-14 dump --summary` 의 GUID `{31629824-4AA2-A8A6-A520-F13E7355EC21}` · 바이너리 디버그 디렉터리 앞 16자 `316298244AA2A8A6` 모두 같음 (PDB 안 Age 3, 바이너리 Age 1 — sast/143 RpcEpMap 과 같은 모양). **분석 증거 셋**: `ms/ghidra/rpcss.gpr` · `ms/decomp/rpcss/_index.tsv` (4,784줄, `FUN_` 1개) · `ms/ghidra/rpcss.analyze.log` 의 `SetPdb: rpcss.dll <- …/rpcss.dll.pdb` 줄. analyze 는 약 1분 (`MAXMEM=8G`; 시작 전 `free -g` total 46 / available 27, 도는 java 없음) |
| kernelbase | `kernelbase.dll` (이미 analyze) | (기존) | `export` 만 (`IsDeveloperModeEnabled`) |

- "알려진 사실" 표에 combase · rpcss 줄은 없다. 대신 둘을 맞췄다 (원본 바이트 대조는 `pefile` 로 `rva + 오프셋` 의 첫 바이트가 `e8` / `48 ff 15` 인지 · 상수가 같은지 본 것):
  - sast/134 의 오프셋이 새 `.asm` 에서 그대로 나온다: `GetConnection +0xbe` (ret `+0xc3`) 가 람다를 부르고, 람다 `+0x82e` (ret `+0x833`) 가 `Connect` 를 부르고, `CoInitializeSecurity +0x47d` (ret `+0x482`) 가 `GetConnection` 을 부른다.
  - **인용한 핵심 `+0x…` 를 원본 바이트로 확인** — 보인 줄은 전부 일치. 호출은 `e8` 또는 `48 ff 15` (REX 붙은 `call [rip+…]`): 람다 `+0x33c` · `+0x3ca` · `+0x3e9` · `+0x468` · `+0x82e` · `+0x884` · `+0x8a3` · `+0x8aa` (`e8`), `+0x2b0` (`48 ff 15`), `+0x899` (`0f 85 15 fe ff ff` → 목적지 `+0x89f − 0x1eb = +0x6b4`), `+0x823` (`66 c7 44 24 28 64 86` = `[RSP+0x28]=0x8664`), `+0xb4f` (`41 b9 ff ff 00 80` = `R9D=0x8000ffff`), `+0xa83` (`44 89 3d …` = `gGotSecurityData` 쓰기); `RetryRPC +0x4` (`ba bb 06 00 00`), `CheckStatus +0x2e` (`81 cb 00 00 07 80`); `CreateFromBindingString +0x79 · +0xc8` (`48 ff 15`), `+0xb2` (`c7 45 e7 03 00 00 00`); `rpcss!_Connect +0x733` (`b8 57 00 00 00`), `+0x73a` (`b8 05 00 00 00`), `+0x186` (`48 39 55 c8`) 등.

---

## 먼저 읽는 한 줄 답

| # | 물음 | 한 줄 답 |
|---|---|---|
| 1-a | 바인딩 글 | **`ncalrpc:[epmapper,Security=Impersonation Dynamic False]`** (combase `.rdata` rva `0x2cc510`, UTF-16). `CreateFromBindingString +0x79` 가 `RpcStringBindingParseW` 로 끝점 `epmapper` 를 뽑고, `+0xc8 RpcBindingCreateW` 의 틀에 **정적 끝점** (프로토콜 번호 `3` = LRPC, `+0xb2`) 으로 넣는다. |
| 1-b | `ept_map` 안 묻고 곧장 가나 | **예, 코드가 그렇다.** 끝점이 글에 박혀 있고, combase 가 가져오는 RPC 함수 (`llvm-readobj --coff-imports`) 에 **`RpcEpResolveBinding` · `RpcMgmtEpEltInq*` 가 없다** (`RpcStringBindingParseW` · `RpcBindingCreateW` · `RpcBindingCopy` · `RpcBindingUnbind` · `RpcBindingBind` · `NdrClientCall2` · `NdrClientCall3` 만). 바인딩은 `BindWithTimeout +0xc` 의 `RpcBindingBind(NULL, 핸들, 인터페이스)` — 비동기 상태가 NULL 이라 **동기** (로그의 `Flags 0x20000` 과 맞음). |
| 1-c | 인터페이스 · 문법 | **한 번의 `GetConnection` 안에서 bind 가 네 번** 나간다: ILocalObjectExporter `e60c73e6-88f9-11cf-9af1-0020af6e72f4` **v2.0** (`+0x33c`, 우리가 본 것) → IMachineActivatorControl `c6f3ee72-ce7e-11d1-b71e-00c04fc3111a` v1.0 (`+0x3ca`) → IROT `b9e79e60-3d52-11ce-aaa1-00006901293f` v0.2 (`+0x3e9`) → ISCM `412f241e-c12a-11ce-abff-0020af6e7a17` v0.2 (`+0x468`). 하나라도 fault 면 거기서 끝 (`0x800706b5`). 넷 다 전송 문법 칸은 **NDR 2.0 v2.0 하나** → bind 마스크 **1** `[추측: 짐작]`. |
| 1-d | 보안 (연결 · bind 꼴을 바꾸는 것) | **요구 서버 SID = NetworkService (`S-1-5-20`)** (`CreateWellKnownSid(24)` → QOS `Sid`), 가장 수준 3 (`IMPERSONATE`), 추적 `DYNAMIC`, 상호 인증 능력 1, 인증 수준 6 · 서비스 10. bind 메시지 `0x70` B 에는 이 값들이 칸으로 안 들어간다. 연결 (`NtAlpcConnectPort`) 쪽 `RequiredServerSid` 와 `SecurityQos` 로만 나간다. |
| 1-e | 이 인터페이스를 받는 모듈 | **`rpcss.dll`** (`StartObjectExporter +0xb65 RpcServerRegisterIf3`). `epmapper` 끝점은 `rpcss.dll` 도 `RpcEpMap.dll` 도 같은 (`ncalrpc`, `epmapper`) 쌍으로 연다 → **한 서비스 묶음 (`svchost -k rpcss`) 의 한 포트를 나눠 쓴다** (sast/143 결과서 "동적으로 확인할 것" 맨 앞). |
| 2-a | 첫 프로시저 | **프로시저 0 = `Connect`** (combase 스텁 rva `0x12f318`, rpcss `_Connect` rva `0x76d80`). 스텁 `NdrClientCall2` (**NDR 2.0 전용**), 서버 `NdrServerCall2` (동기). 입력 6칸 (winsta 글 · exe 경로 · IPID 표 구조체 포인터 · 깃발 · 머신 `0x8664` · 예약 OID 요청 수 `10`) · 출력 25칸 · 반환 상태. 칸 표는 아래 표 (2). |
| 2-b | combase 가 응답에서 거는 검사 | `Connect` 가 0 이어도 **`_ph` (컨텍스트 핸들) · 로컬 해결사 DUALSTRINGARRAY 포인터 · `gdwScmProcessID` · `_ProcessSignature` 중 하나라도 0 / NULL 이면 `0x8000ffff`**. 레거시 보안 글이 있으면 2 글자 이상. 예약 OID 개수는 **10 이하여야 한다 (combase 가 안 막는다 → 어기면 `_OidsReserved` 0x50 B 를 넘쳐 씀)**, SD 다섯 조각 크기 합은 덩어리 크기 이하여야 한다 (안 막는다 → 어기면 over-read). 그 밖 칸은 받아서 전역에 넣을 뿐. |
| 2-c | out 값 가름 | (가) 상수 · 규칙: 핑 주기 (PE 초기 360) · 인증 수준 2 · 가장 수준 2 · 컨테이너 인증 서비스 0/0 · 설정 비트. (나) 프로세스마다: rpcss PID · 스레드 번호 · 컨텍스트 핸들. (다) 난수 · 식별자: 머신 MID · 프로세스 서명 · 프로세스 GUID · OID. (라) 비어도 지나감: 예약 OID 개수 0 · 레거시 보안 글 NULL · 서버/클라이언트 인증 서비스 목록 · 채널 훅 · SD 다섯 조각 (크기 0). |
| 2-d | 실패 쪽 | `RetryRPC` 는 **상태 `0x6bb` (1723) 일 때만 1** (`Sleep(100)` 뒤, **횟수 제한 없음**) — 그 밖은 0. `CheckStatus` 는 0 → 0, 양수 N → `0x80070000 \| (N & 0xffff)`, 음수 → 그대로. **깨끗이 끝나는 상태 = `0x6bb` 를 뺀 아무 양수** (`0x6b5` · `0x6ba` · `0x6bf` · `0x57` …), 단 `5` 는 `GetConnection +0xc5` 의 별도 갈래 (피함, Q257b). **금지 = fault 상태 `0x6bb`** (끝없이 다시 건다). bind 단계 fault 는 `RetryRPC` 에 닿기 전에 끝난다. |
| 3-a | `CoInitializeSecurity` · `CoInitializeEx` | `CoInitializeSecurity` 는 `+0x47d GetConnection` (위 bind ×4 + `Connect`) 뒤 **`+0x43c SetAppID` (프로시저 1) 만** 더 부를 수 있다 — `g_AppId != GUID_NULL` 일 때만. `CoInitializeEx` 는 **초기화 길에서 이 포트로 안 부른다** (지도 깊이 10 안에 `GetConnection` · `Connect` 없음); 끝낼 때만 `Disconnect` (10) · `ServerFreeOXIDAndOIDs` (8). |
| 3-b | 첫 COM 사용의 첫 부름 | (a) `CoCreateInstance` (in-proc 이어도): `…→ CoGetDefaultContext +0x3f → InitChannelIfNecessary +0x119 → CComApartment::InitRemoting (rva 0x36ad4)` 가 **`+0xb6 InitializeSecurity → CoInitializeSecurity` (암묵 호출)** 로 위 연결을 먼저 만들고, 이어 **`+0x29d CallTheResolver` (프로시저 7) → `RegisterOXIDAndOIDs` → `ServerRegisterOXID` (프로시저 6)**. (b) `CoGetObject` 는 ole32 쪽이라 이번에 안 열었다 `[공백]` — ROT 쪽 후보 `IrotGetObject` (IROT 3) · `IMgotGetObject` (IROT 9). (c) out-of-proc: `DelegateActivationToSCM +0x156 BindToSCMProxy → GetConnection`, 뒤는 ORPC 프록시 (`MakeSCMProxy`) — 다음 요청. |
| 3-c | `GetConnection` 부르는 곳 | **46곳** (표 4-d). 대부분 combase 의 프로시저 스텁 한 개당 하나. |
| 우리 | PE_emulator | 지금 대답 (bind 에 `0x6b5`) 은 **깨끗한 정직한 실패라 안전** (재시도 없음, `CoInitializeSecurity` = `0x800706b5` 그대로). 성공시키려면 **bind 넷 + `Connect` 몸** 이 한꺼번에 필요하고, 프로시저 찾기가 인터페이스를 안 가려서 (`lrpc.cpp:102~103`) 같은 포트에 인터페이스를 늘리면 번호가 겹친다. 아래 "PE_emulator 쪽". |

---

## 표 (1) bind 표 — `칸 · 값 · 출처 줄`

### 1-1. 연결 · 바인딩 핸들 만들기 (람다 `+0x2b0` ~ `+0x33c`, `CreateFromBindingString`)

| 칸 | 값 | 출처 줄 |
|---|---|---|
| 바인딩 글 | `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` | `CreateFromBindingString.asm +0x2f LEA R12,[0x1802cc510]` (= `RpcStringBindingParseW` 첫 인자 `+0x63 MOV RCX,R12`); 글 자체는 combase PE `.rdata` rva `0x2cc510` (`pefile` UTF-16) |
| 프로토콜 · 끝점 | LRPC (`ProtocolSequence = 3`), 끝점 `epmapper` (파서가 뽑음), 네트워크 주소 NULL | `+0x79` (ret `+0x80`) `RpcStringBindingParseW(글, NULL, NULL, NULL, &끝점, NULL)`; `+0xb2 MOV [RBP-0x19],3`; `+0xc8` (ret `+0xcf`) `RpcBindingCreateW(틀, 보안, 옵션, &핸들)`; 틀의 `StringEndpoint` = `+0xbd MOV [RBP-0x9],RAX` |
| 바인딩 옵션 | `Version 1 · Flags 1 · ComTimeout 5 · CallTimeout 0xffffffff` (이름은 `RPC_BINDING_HANDLE_OPTIONS_V1` 짐작 `[이름 추측]`) | `+0xa5 / +0xab / +0xc1 MOV dword [RBP+0x17/+0x1b/+0x1f]`, `+0x8f OR dword [RBP+0x23],-1` |
| **`ept_map` 을 안 묻나** | **안 묻는다.** 끝점이 틀에 있고, combase 안에 끝점 풀이 호출이 없다. 로그와 같음 (556/566 에서 연결 → bind 사이 EPM 없음) | 위 두 줄 + `BindWithTimeout.asm +0xc RpcBindingBind` 뿐 |
| 동기 · 비동기 | **동기**: `RpcBindingBind(NULL, 핸들, 인터페이스)` | `BindWithTimeout +0x0 ~ +0xc`: `MOV R8,RDX · MOV RDX,RCX · XOR ECX,ECX` (비동기 상태 = NULL) → ret `+0x13`. (`BindWithTimeout` 의 셋째 인자 `K` (시간제한) 는 **쓰이지 않는다**) |
| 요구 서버 SID | **NetworkService `S-1-5-20`** (바이트 `01 01 00 00 00 00 00 05 14 00 00 00`) — `CreateWellKnownSid(24 = WinNetworkServiceSid)` 로 만들어 QOS 의 `Sid` 칸에 | 람다 `+0x2ad LEA ECX,[RDI-0x2c]` (RDI = `0x44` → `0x18`) · `+0x2b0` (ret `+0x2b7`) `CreateWellKnownSid`; `+0x2d1 MOV [RBP+0x80],RAX` (RAX = SID 버퍼 `RBP+0x100`) |
| 가장 · 추적 · 능력 | QOS V4: `Version 4 · Capabilities 1 · IdentityTracking 1 (동적) · ImpersonationType 3 (IMPERSONATE)`, 유효 전용 0 | 람다 `+0x2c3 [RBP+0x60]=4 · +0x2e0 [RBP+0x64]=1 · +0x2e3 [RBP+0x68]=1 · +0x2e6 [RBP+0x6c]=3 · +0x2ed [RBP+0x88]=0` |
| 인증 | `AuthnLevel 6` (패킷 프라이버시) · `AuthnSvc 10` (WINNT) · 서버 이름 NULL · 신원 NULL | 람다 `+0x2fe [RBP+0x48]=6 · +0x2f7 [RBP+0x4c]=0xa · +0x2f4 [RBP+0x38]=1` (보안 구조체 버전) |
| 연결 · bind 에 나가는 쪽 | 연결: `RequiredServerSid` = 위 SID, `SecurityQos` = (가장 3 − 1, 추적 동적) — sast/127 2-1 표의 `[this+0xc0]` · `[this+0xd8]-1` 칸. **bind 메시지 `0x70` B 에는 보안 칸이 없다** (sast/130 1-2 표) | 이 칸들이 QOS 에서 연결 객체로 옮겨 가는 줄은 rpcrt4 `RpcBindingCreateW` 안이라 안 읽음 `[추측]` |

### 1-2. bind 넷 — 인터페이스 구조체 (combase PE 에서 읽음)

모두 `RPC_CLIENT_INTERFACE` `Length 0x60`, **`DispatchTable` · 끝점 · `InterpreterInfo` · `Flags` 가 전부 0**, `TransferSyntax` = NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0. 구조체 rva · PDB 이름:

| 순서 | 인터페이스 (PDB 이름) | rva | UUID | 버전 | bind 호출 | 실패 때 (줄 번호 · 글) |
|---|---|---|---|---|---|---|
| 1 | `ILocalObjectExporter___RpcClientInterface` | `0x2fcd00` | `e60c73e6-88f9-11cf-9af1-0020af6e72f4` | **2.0** | `+0x33c CreateFromBindingString` (ret `+0x341`) | 람다 `+0x34a JMP → +0xcb8` (= `0x18018c678`): 줄 `0x300` "Failed to create binding handle to rpcss (ILocalObjectExporter)" |
| 2 | `IMachineActivatorControl___RpcClientInterface` | `0x2fcdf0` | `c6f3ee72-ce7e-11d1-b71e-00c04fc3111a` | 1.0 | `+0x3ca CreateFromBindingHandle` (ret `+0x3cf`) | 줄 `0x306` (IMachineActivatorControl) |
| 3 | `IROT___RpcClientInterface` | `0x2fcf30` | `b9e79e60-3d52-11ce-aaa1-00006901293f` | 0.2 | `+0x3e9` (ret `+0x3ee`) | 줄 `0x30a` (IROT) |
| 4 | `ISCM___RpcClientInterface` | `0x2fd220` | `412f241e-c12a-11ce-abff-0020af6e7a17` | 0.2 | `+0x468` (ret `+0x46d`) | 줄 `0x30e` (ISCM) |

- 요청서의 `[추측 — 공개 이름]` (`e60c73e6… = ILocalObjectExporter`) 은 **PDB 이름으로 닫혔다** `[정적]`: 람다 `+0x14c …` 가 `0x1802fcd00` 을 `CFastBH` 첫 칸에 넣고, 그 심볼이 `ILocalObjectExporter___RpcClientInterface`.
- **2 ~ 4 번 bind** (`CreateFromBindingHandle.asm`): `+0x23 RpcBindingCopy` (ret `+0x2a`) → `+0x35 RpcBindingUnbind` (ret `+0x46`) → `+0x54 BindWithTimeout` (ret `+0x59`) → `+0x6a CMPXCHG [CFastBH+0x10]`. 즉 1 번 핸들을 복사해 풀고 다른 인터페이스로 **다시 bind**. 같은 끝점 이름이라 rpcrt4 `PickAssociation` (`.asm +0x95 ~ +0xc4`: 전역 `LrpcAssociations` 를 돌며 `_wcsicmp(끝점)` 같음 · 상태 ≠ 2 · ≠ 5 · `[+0x48] == 0`) 이 **이미 있는 연결을 다시 쓴다** `[추측: 보안 칸 비교는 이 함수에서 못 봤다]` → 새 `NtAlpcConnectPort` 없이 **같은 포트에 bind 메시지 (종류 1) 가 세 번 더** 온다 (인터페이스마다 슬롯 번호가 다름 — sast/130 1-2 `+0x4c`). 동적으로 닫을 것: D2.
- 4 개 `CFastBH` 는 `GetConnection` 이 람다를 부를 때마다 **새로 만든다** (`+0x14c · +0x18f · +0x1d2 · +0x211 HeapAlloc(0x18)`, `m_hRpc` = 0 으로 시작) → 한 번 실패하면 **음수 캐시가 없다.** `GetConnection +0x34 / +0x56 TEST [_dwFlags],1` 가 서 있어야만 (성공 때 람다 `+0xae4 OR [_dwFlags],1`) 건너뛴다. 로그의 "API 한 번 안의 거절 1" 이 이 모양과 맞고, 여섯 샘플이 7천 번 거절당한 것도 이 때문 (각 COM 부름이 처음부터 다시).

### 1-3. 내미는 전송 문법 (run133 `+0x48` 마스크 칸)

- combase 의 네 구조체는 **`Flags` 비트 25 (`0x2000000`) 가 꺼져 있고 `InterpreterInfo` 가 0** (위 hexdump 와 같은 값: `+0x50 ~ +0x5f` 전부 0). rpcrt4 `BaseBind +0x91 MOV EAX,[R8+0x58]` · `+0x95 BT EAX,0x19` · `+0x99 JNC +0x3a4`: 이 비트가 꺼지면 문법 표 (EPM 의 두 줄 NDR 2.0 + NDR64 같은 것) 대신 **`+0x3a4 MOV RDX,[R8+0x18]`** — 구조체 자신의 `TransferSyntax` (+0x18) 하나만 쓴다. → bind 문법 마스크는 **`1` (NDR 2.0) 하나** `[추측: 짐작 — 마스크를 만드는 줄 (Bind__4e330 +0x4e0) 까지 이 갈래로 이어 따라가지는 않음]`.
- 스텁이 `NdrClientCall2` 라서 어차피 **NDR 2.0 전용**이다. **서버 흉내는 `+0x48 = 1` 로 답할 것** (제안이 1 이든 3 이든 "제안의 부분집합 + 비트 하나" 를 만족, sast/130 1-3). NDR64 (`2`) 로 답하면 이 클라이언트는 풀 수 없다.

### 1-4. 진짜에서 받는 모듈

- `rpcss.dll`: `StartObjectExporter.asm +0xb43 LEA RCX,[0x180129500]` (ILocalObjectExporter 서버 구조체 rva `0x129500`) → `+0xb65` (ret `+0xb6c`) `RpcServerRegisterIf3(구조체, NULL, NULL, Flags 0x20, MaxCalls 0x4d2, MaxRpcSize 0, 콜백 = LocalInterfaceOnlySecCallback (rva 0x1fbf0, 2721 B), SD = CreateSd)`. **`Flags 0x20` = 로컬 전용 (`RPC_IF_ALLOW_LOCAL_ONLY` 짐작)** `[이름 추측]`. 프로시저 12개 모두 `NdrServerCall2` (동기).
- 나머지 셋: 서버 구조체 rva — IMachineActivatorControl `0x129060` (프로시저 7) · IROT `0x129130` (11) · ISCM `0x128f00` (27, 등록 `InitializeSCM +0x10e`). 공개 DCOM 인터페이스 `IObjectExporter` `99fcfec4-5260-101b-bbcb-00aa0021347a` v0.0 (`0x1292d0`, 프로시저 6, 콜백 `IObjectExporterSecCallback`) 도 같은 프로세스에 있지만 combase 의 이번 길에는 안 나온다.
- 끝점: `rpcss!UseProtseqIfNecessary +0x23e` (ret `+0x245`) 가 `RpcServerUseProtseqEpExW(protseq, 0x32, 끝점, …)` 를 부르고, 인자는 전역 표 `gaProtseqInfo` (rva `0x164030`, 칸 24 B) 의 **칸 16 = {1, `ncalrpc`, `epmapper`}** (`pefile` 로 읽음). `RpcEpMap!InitializeService +0x23` (ret `+0x28`) `UseProtseqIfNecessary(16)` (읽은 `.asm`: `ms/disassemble/rpcepmap/InitializeService.asm`) — **칸 번호 16 이 같다**. 같은 (`ncalrpc`, `epmapper`) 를 두 모듈이 연다 → 포트 하나를 서비스 둘이 나눠 쓴다 (둘이 같은 `svchost -k rpcss` 안인 것은 `[수집] reg-users` 서비스 설정이 근거; 코드만으로는 못 정함). 이 인터페이스가 EPM (`e1af8308…`) · RpcEpMap 의 로컬 인터페이스 (`0b0a6584…`) 와 **한 포트의 다른 인터페이스**라는 점은 sast/143 결과서와 같은 그림.

---

## 표 (2) 첫 프로시저 `Connect`

### 2-0. 이름 · 자리

- combase: `Connect` rva `0x12f318` (399 B, `Connect__12f318.asm`). `+0x16a LEA RDX,[lclor__MIDL_ProcFormatString+0x2]` (rva `0x2c26e2`) · `+0x174 LEA RCX,[ILocalObjectExporter_StubDesc]` (rva `0x274270`) · `+0x17b` (ret `+0x182`) `NdrClientCall2`. **프로시저 번호 0**, 스택 `0x108` (=33 칸: 핸들 + 파라미터 32), 파라미터 32개 (반환 포함), `Oi_flags 0x68` (rpc 플래그 · 새 초기화 · 통신/오류 처리), `Oi2 0x47`.
- 스텁이 **상수를 직접 채우는 칸**: 출력 중 열 칸은 호출자가 안 넘기고 스텁 안에서 전역 주소를 박아 넣는다 (`+0x2e _GuidRPCSSProcessIdentifier` · `+0x39 _ProcessSignature` · `+0x44 gdwScmProcessID` · `+0xac gImpLevel` · `+0xb7 gAuthnLevel` · `+0xd1 gOutgoingContainerAuthnSvc` · `+0xdf gIncomingContainerAuthnSvc` · `+0x114 gLocalMid` · `+0x135 giPingPeriod` · `+0x141 _ph`) 와 입력 상수 `+0x11b MOV [RSP+0x60],0xa` (= 예약 OID 요청 수 10).
- rpcss 서버: `_Connect` rva `0x76d80` (1887 B, `_Connect.asm`). 서버 프로시저 문자열의 0 번 항목 (rva `0x14fb82`) 이 클라이언트 것 (combase `0x2c26e2`) 과 **바이트까지 같다** (`pefile` 비교 True). 형식 (타입) 문자열은 combase rva `0x2fc862` (`StubDesc +0x40 pFormatTypes`).
- 반환 `error_status_t` (형식 `0x10`, 반환 칸) — 서버가 돌려주는 RPC/Win32 오류 번호 (0 = 성공).

### 2-1. 요청 칸 (입력) — 차례 · 타입 · 이 호출의 값

| 차례 | 스택 | 칸 | 타입 (형식 문자열 오프셋) | 이 호출에서 값 | 줄 |
|---|---|---|---|---|---|
| — | 0 (RCX) | `hRpc` | 명시적 바인딩 핸들 (`FC_BIND_PRIMITIVE`) — **선 위에 안 실림** | 1 번 bind 로 만든 핸들 | 람다 `+0x78c MOV RCX,[R14]` |
| 0 | `0x8` (RDX) | winsta · 데스크톱 글 | `[in, unique, string] wchar_t*` (`FC_UP` 단순 → `FC_C_WSTRING`, 형식 `0x2`) | `_pwszWinstaDesktop` (예 `WinSta0\Default`) — 핸들 0 이면 **NULL** (sast/134) | 람다 `+0x742 MOV RDX,[0x180338118]` |
| 1 | `0x10` (R8) | exe 경로 | 같은 꼴 (`0x2`) | `GetModuleFileNameW(NULL, buf, 0x207)` 결과, 실패하면 빈 글 | 람다 `+0x751 LEA R8,[RBP+0x150]` · `+0x123` (ret `+0x12a`) |
| 2 | `0x18` (R9) | IPID 표 정보 | `[in, unique]` 96 B 구조체 (`FC_UP` → `FC_FORCED_BOGUS_STRUCT 0x60`, 형식 `0x6` → `0x3a`) | **보통 NULL.** 개발자 라이선스가 설치된 컴퓨터에서만 0x60 B 를 채워 보냄 | 람다 `+0x431` (ret `+0x436`) `IsDeveloperLicenseInstalled` → `+0x77e MOV R9,RBX`; 채우는 줄 `+0x499` (`HeapAlloc(0x60)`, ret `+0x4a0`) ~ `+0x53e` |
| 3 | `0x20` | 깃발 | `[in] ulong` (`FC_LONG`) | 0 ~ 7: `g_GLBOPT_RoFlags` 비트 3, `AppModelPolicy` 둘 (`+0x5f5` · `+0x616` `AppModelPolicy_GetPolicy`) 이 `\|= 2 · 4`. 보통 0 | 람다 `+0x82a MOV [RSP+0x20],EDI` |
| 4 | `0x28` | 머신 | `[in] ushort` (`FC_SHORT`) | **`0x8664`** 고정 | 람다 `+0x823` |
| 9 | `0x50` | 예약 OID 요청 수 | `[in, range(0, 0x20000)] long` (형식 `0x9c` = `FC_RANGE`) | **10** 고정 | `Connect +0x11b` |

요청 스텁의 고정 부분은 **22 바이트** (프로시저 형식의 `constant_client_buffer_size`): 유니크 포인터 id 셋 (4 B ×3) + 깃발 4 + 머신 2 + 요청 수 4 (+ 정렬). 뒤에 비-NULL 포인터의 글 · 구조체 본문이 붙는다 `[규칙]`. **서버 흉내는 요청 본문을 읽을 필요가 없다** — 요청 머리 `+0x3c` (프로시저 번호) 와 `+0x38` (슬롯 = 1 번 bind 의 것) 만 보면 된다. 진짜 rpcss 가 읽는 것은 아래 "서버 쪽 검사".

### 2-2. 응답 칸 표 (출력) — `칸 · 타입 · 가름 · combase 가 읽는 줄 · 0 가능 여부 · 최소 대답`

NDR 선 위 차례는 아래 표의 차례 (파라미터 번호 순서) `[규칙]`. 형식 문자열 바이트는 `[정적]`, 코드 이름 (`FC_*`, 컨텍스트 핸들 깃발 · 파라미터 속성 비트) 은 공개 NDR 코드 표로 풀었다 `[규칙]`. 출력 칸마다 "combase 가 읽는 줄" 은 람다 `operator()` (`+0x…`), 전역 이름 옆 rva 는 combase. "rpcss" 는 서버가 값을 정하는 줄 (`_Connect +0x…`, 디컴파일로 확인).

| 차례 (스택) | 칸 → 받는 곳 | 타입 | 가름 | combase 가 읽는 줄 | 0 · NULL 가능 | 최소 대답 (꼴만) |
|---|---|---|---|---|---|---|
| 5 (`0x30`) | **`_ph`** (`CRpcResolver::_ph`, rva `0x338110`) | 컨텍스트 핸들 20 B (속성 u32 + GUID 16, `FC_BIND_CONTEXT` 깃발 `0xa4` = 포인터 거쳐 · 출력 · 직렬화 없음; 형식 `0x68`) | **(다)+(나)** 서버가 연결마다 만드는 불투명 핸들 (rpcss: `CProcess` 객체, `_Connect +0x527`) | 람다 `+0x8c6 CMP [_ph],0` · `+0x8cd JNZ`; **NULL 이면 `+0x8d4 MOV EDX,0x387` → `+0xb4f` `Return_Hr(0x8000ffff)`**. 뒤에서 읽는 쪽 **65곳** (`Disconnect +0x6` · `SetAppID +0x25` · `CallTheResolver +0xba` · `ClientResolveOXID +0x4e` · `IrotGetObject +0x71` · … `_refs/0x338110.tsv`) | **불가** (속성 4 B 와 GUID 16 B 가 전부 0 이면 NULL 핸들) | 속성 0 + **0 이 아닌 GUID** (우리 쪽이 연결마다 하나 만들어 기억 — 뒤 프로시저 (SetAppID 1 · Disconnect 10 …) 가 `[in]` 으로 되돌려 준다) |
| 6 (`0x38`) | **핑 주기** `giPingPeriod` (rva `0x336288`) | `ulong` | **(가)** rpcss `g_usBaseTimeoutInterval` — PE 초기값 `0x168` = **360** (`.data` rva `0x1648f8` 의 파일 바이트 `68 01`), 쓰는 곳은 `InitPingInterval +0x35` 와 `Ordinal_3 +0x26` 뿐; `_Connect +0x562` | 람다 `+0xad6 IMUL EAX,[giPingPeriod],0x3e8` · `+0xaeb` 저장 (×1000 = ms). 읽는 쪽 `GetBulkUpdateRate +0xf` · `ClientBulkUpdateOIDWithPingServer +0x2ca` · `CanRunDown +0x179` | 가능 (검사 없음), 단 핑 간격으로 곱해 쓰니 **0 은 부적절** `[추측]` | 360 |
| 7 (`0x40`) | **로컬 해결사 바인딩** → `_pdsaLocalResolver` (rva `0x338c78`) | `[out] DUALSTRINGARRAY**` (`FC_RP`·deref → `FC_UP` → `FC_CSTRUCT {u16 wNumEntries, u16 wSecurityOffset, u16 aStringArray[wNumEntries]}`; 형식 `0x70 → 0x74 → 0x8e`; 조건 길이 = `wNumEntries`) | **(나)/(다)** rpcss 가 듣는 주소들 (`CopyMyOrBindings`, `_Connect +0x3bd`) | 람다 `+0x8de CMP [RBP-0x48],0` · `+0x8e2 JZ +0xb37` → `+0xb3c MOV EDX,0x388` → E_UNEXPECTED; 아니면 `+0x925` (ret `+0x92c`) `HeapAlloc(0x10)` 로 `CDualStringArray {ptr, 참조 1}` 를 만들고 `+0x954` (ret `+0x959`) `Attach`, `+0x959 CMP [_pdsaLocalResolver],0` · `+0x962 MOV EDX,0x38e` → `0x8007000e`. 읽는 쪽 `GetLocalResolverBindings +0x2b` · `SetLocalResolverBindings +0x47` · `GetCurrentMachineNames +0x2f` · `Cleanup +0x62` | **포인터 NULL 불가.** 안 빈 배열 (`wNumEntries 0`) 은 **이 함수 안에서는 통과** `[정적]`, 뒤 쓰임은 `[공백]` (활성화 때 `MakeSCMProxy` 가 이 글로 연결 — 다음 요청) | 비-NULL 포인터 + 구조체. 내용은 `TODO(VM)` (진짜 문자열 바인딩) |
| 8 (`0x48`) | **로컬 MID** `gLocalMid` (rva `0x336290`) | `ULONG64` (`FC_HYPER`) | **(다)** 머신 식별자 (rpcss `gLocalMid`, 채우는 곳 `Initialize` rva `0x939f0` `+0x24` `[이름 추측]`; `_Connect +0x573`) | 검사 없음. 읽는 쪽 `GetLocalMIDEntry +0x75` · `FindOrCreateMIDEntry +0xa2` · `RegisterOXIDAndOIDs +0xa0` · `ClientResolveMachineLocalOXID +0x4f` · `CompleteObjRef +0x68` … (15곳) | 가능 (이 함수), MID 로 쓰이니 0 은 피함 `[추측]` | 0 이 아닌 상수 하나 |
| (9, 입력 `0x50`) | 위 표 (2-1) | | | | | |
| 10 (`0x58`) | **예약 OID 배열** → `_OidsReserved` (rva `0x3362a0`, 0x50 B = 8 B × 10) | `ULONG64[]` (`FC_CARRAY` 원소 8, 조건 길이 = 입력 `0x50` = 10; 형식 `0xa6`) — **선 위에 열 칸이 다 실림** | **(다)** rpcss `AllocateReservedIdsInternal(10, 배열, &개수)` (`_Connect +0x583`) | 람다 `+0xa5b memset(_OidsReserved,0,0x50)` · `+0xa7e` (ret `+0xa83`) `memcpy(_OidsReserved, 배열, 개수×8)`. 읽는 쪽 **`ServerGetReservedID` 한 곳** (`+0x57 · +0x66 · +0x6d · +0x14d · +0x165`, `_refs/0x3362a0.tsv`) | 배열 값은 **0 도 가능** (개수가 0 이면 안 쓰임) | 열 칸 모두 0 (개수 0) |
| 11 (`0x60`) | **예약 OID 개수** `_cReservedOidsAvail` (rva `0x33813c`) | `ulong` | **(다/나)** | 람다 `+0xa60 MOV EAX,[RBP-0x38]` · `+0xa6d MOV [_cReservedOidsAvail],EAX` · `+0xa73 SHL R8,3` · `+0xa7e memcpy`. **개수 상한 검사 없음**. 읽는 쪽 `ServerGetReservedID +0x51 · +0x13d` (줄이는 곳 `+0x72`) — `_refs/0x33813c.tsv` 는 4 B 로 다시 뽑음 | **0 가능 (라). 10 초과 금지** (`_OidsReserved` 넘쳐 씀) | **0** |
| 12 (`0x68`) | **설정 비트** (람다 지역 → 아홉 전역) | `ulong` | **(가)** rpcss 설정 전역 (`_Connect +0x2bf ~ +0x31d` (`+0x2bf OR [RBX],1` = bit0, `+0x2ca OR [RBX],2` = bit1, `+0x2d5 BTS [RBX],8` = `0x100` …, 끝 `+0x31d` 가 `gdwInvalidSecurityDescriptorLoggingLevel` 비교)): bit0 `NetworkDCOM` · bit1 `ContainerDCOM` · bit2 `MutualAuth` · bit3 `SecureRefs` · `0x10` 서버 예외 잡기 · `0x20` 조용한 예외 멈춤 · `0x40` 호출 실패 기록 (수준 2) · `0x80` 나쁜 SD 기록 (수준 2) · `0x100` 옛 모달 루프 · `0x200` 접근 제한을 정책으로 | 람다 `+0x9c8 ~ +0xa48`: `g_GLBOPT_EHValue` (`0x10` 이 꺼져 있고 값이 1 이하면 1) · `gAccessRestrictionsViaPolicy` · `gfBreakOnSilencedExceptions` · `gEnableNetworkDCOM` · `gEnableContainerDCOM` · `gEnableOldModalLoop` · `gfLogCallFailure` · `gfLogInvalidSecurityDescriptor` · `gCapabilities = (bit2) \| (bit3 ? 2 : 0)`; 끝에 `+0xa8a ~ +0xacf` 가 수명 관리자에 따라 `gEnableNetworkDCOM` 을 다시 정함 | 가능 (모든 값 받음) | 0 (`TODO(VM)`: 진짜 VM 설정 값) |
| 13 / 14 (`0x70` / `0x78`) | **컨테이너 인증 서비스** 들어오는 / 나가는 → `gIncomingContainerAuthnSvc` (rva `0x33827c`) / `gOutgoingContainerAuthnSvc` (rva `0x33828c`) | `ulong` ×2 | **(가)** 기본 **0 · 0** (`ComputeSecurity +0x784 / +0x78f` 가 0 으로 시작, 레지스트리가 바꿀 수 있음) | 스텁이 전역 주소에 곧바로 받음 (`Connect +0xd1 / +0xdf`); 람다에는 검사 줄 없음 | 가능 | 0 · 0 |
| 15 (`0x80`) | **레거시 보안 글** → `gLegacySecurity` (rva `0x338d18`) | `[out] wchar_t**` (`FC_RP`·deref → 형식 `0x2` 유니크 문자열; 형식 `0xbc`) | **(가)** 기본 **NULL** (`GetLegacySecurity +0x29 MOV EAX,[s_dwLegacySecurityLen]` · `+0x31 JNZ`: 길이 0 이면 NULL 반환) | 람다 `+0x970 TEST RDX,RDX` · `+0x973 JZ` (NULL 이면 건너뜀); 있으면 `+0x975 ~ +0x987` 글자 수 **2 미만이면 `+0x98e MOV EDX,0x398` → E_UNEXPECTED**; 아니면 `+0x99f ~ +0x9a3` `gLegacySecurity` 에 넣음. 읽는 쪽 `CoInitializeSecurity +0x5f9` · `UninitializeSecurity +0x9d` | **NULL 가능 (라)** | NULL (유니크 포인터 id 0) |
| 16 / 17 (`0x88` / `0x90`) | **인증 수준 · 가장 수준** → `gAuthnLevel` (rva `0x334e5c`) / `gImpLevel` (rva `0x334e60`) | `ulong` ×2 | **(가)** 기본 **2 · 2** (`ComputeSecurity +0x76c MOV EBX,2`, `+0x7b8 / +0x7be` 가 `s_lAuthnLevel` · `s_lImpLevel` 에 저장; 레지스트리 `HKLM\Software\Microsoft\Ole` 의 `LegacyAuthenticationLevel` · `LegacyImpersonationLevel` 가 있으면 그 값 `+0x1103` / `+0x1190`) | 스텁이 곧바로 받음 (`Connect +0xb7 / +0xac`). 읽는 쪽 `DefaultBlanket +0x186` · `CreateDefaultHandle +0x5f` · `CheckAccess +0x9b` · `GetRegistryAuthnLevel`; `CoInitializeSecurity +0x33b / +0x345` 가 **나중에 덮어씀** | 가능 (검사 없음), 단 0 은 "기본" 이라 뒤 쓰임 `[공백]` | 2 · 2 |
| 18 (`0x98`) | **서버 인증 서비스 개수** → `gServerSvcList.m_size` | `ulong` | **(가)** rpcss 에 등록된 서비스 수 (`GetClientServerSvcs`, `_Connect +0x37a`) | 람다 `+0x84b MOV [RCX+8],RDX` (개수를 배열 객체 크기 칸에) | 가능 (라) | 0 |
| 19 (`0xa0`) | **서버 인증 서비스 목록** → `gServerSvcList.m_ptr` (rva `0x338d08`) | `[out] USHORT**` (`FC_UP` · 널 허용 + 조건 길이 = 칸 18; 형식 `0xc0`) | **(가)** | 〃 (위 줄) | **NULL + 개수 0 가능 (라)** `[추측: 목록을 읽는 쪽이 `unique_any_array_ptr` 안이라 refs 로 안 닫힘]` | NULL |
| 20 (`0xa8`) / 21 (`0xb0`) | **클라이언트 인증 서비스** 개수 / 목록 → `gClientSvcList` (rva `0x338cf8`) | `ulong` / `[out] struct{wchar_t* 주체 이름; USHORT 서비스}**` (원소 16 B, 형식 `0xde → 0xe6`) | **(가)** | 람다 `+0x85c MOV [RAX+8],RCX` (개수); `CoInitializeSecurity +0x78a` 가 이 전역에 **쓴다** | 가능 (라) | 0 / NULL |
| 22 (`0xb8`) / 23 (`0xc0`) | **채널 훅** 개수 / GUID 목록 → `gaChannelHook` (rva `0x338d60`) | `ulong` / `[out] GUID**` (원소 16 B, 형식 `0x120`) | **(가)** (`UpdateChannelHooks`, `_Connect +0x38d`) | 람다 `+0x86d MOV [RAX+8],RCX`; 읽는 쪽 `CleanupChannelHooks +0x71` | 가능 (라) | 0 / NULL |
| 24 (`0xc8`) | **스레드 번호** → **OLE TLS 데이터 `+0x10`** (`TEB+0x1758` `ReservedForOle` 가 가리키는 칸; `GetConnection +0x10 ~ +0x24`, 람다 `+0x6c4 MOV RSI,[R12]` · `+0x6c8 ADD RSI,0x10`; 칸 이름 `[이름 추측: dwApartmentID 칸]`) | `ulong` | **(나)** 서버 전역 카운터 `gNextThreadID++` (PE 초기 1; `_Connect +0x58d XADD`) | 읽는 줄 없음 (그대로 저장) | 가능 | 1, 2, 3 … (연결마다 1 씩) |
| 25 (`0xd0`) | **`gdwScmProcessID`** (rva `0x33800c`) | `ulong` | **(나)** rpcss 의 PID (`GetCurrentProcessId`, `_Connect +0x59b`) | 람다 `+0x8e8 CMP [gdwScmProcessID],0` · `+0x8ef JNZ`; **0 이면 `+0x8f6 MOV EDX,0x389` → E_UNEXPECTED**. 읽는 쪽 `MakeProxyHelper +0x34` · `IsClientRpcss +0x4b` · `RundownOid +0x54` · `RPCSStoOLE32SecurityCallback +0x7e` · `ORPCInterfaceSecCallback +0x462` · 람다 `+0x8e8` ("이 호출자가 rpcss 인가" 비교) | **0 불가** | 0 이 아닌 값. bind 답 `+0x08` (서버 프로세스 ID) 과 **같은 값**으로 (`[추측]`) |
| 26 (`0xd8`) | **`_ProcessSignature`** (rva `0x338120`) | `ULONG64` | **(다)+(나)** 서버가 프로세스마다 만든 값 (`CProcess+0x248`, `_Connect` 가 `*param_28` 에) | 람다 `+0x900 CMP [_ProcessSignature],0` · `+0x907 JNZ`; **0 이면 `+0x913 MOV EDX,0x38a` → E_UNEXPECTED**. 읽는 쪽 `DelegateActivationToSCM +0x1ac` (활성화 요청에 실어 보냄 `[추측]`) | **0 불가** | 0 이 아닌 값 (연결마다 달라도 됨) |
| 27 (`0xe0`) | **`_GuidRPCSSProcessIdentifier`** (rva `0x336b68`) | GUID 16 B (`FC_RP` → 형식 `0x2e`) | **(나)(다)** 클라이언트 프로세스마다 서버가 정함 (`CProcess+0x68 ~ +0x78`) | 검사 없음. 읽는 쪽 `CoGetProcessIdentifier +0x5` · `MakeServerEntry +0x57` | 가능, 단 0 이 아닌 값 권장 | 연결마다 임의 GUID |
| 28 (`0xe8`) | **SD 다섯 조각 크기** (람다 지역 `ulong[5]`) | `ulong[5]` (20 B, `FC_SMFARRAY`; 형식 `0x15e`) | **(가)** 순서: 등록 없는 실행 권한 · 접근 권한 · 실행 제한 · 접근 제한 · (있으면) 기본 접근 제한 (`_Connect +0x3dd ~ +0x4ae`) | 람다 `+0x9ac LEA RDX,[RBP+0x90]` → `+0x9b3` `InitializeSystemSecurityDescriptors` (`.asm +0x1e ~ +0x137`: 크기마다 `LocalAlloc(0x40, 크기)` + `memcpy`; 검사는 `LocalAlloc` NULL 뿐) | 가능 (라) — 크기 0 이면 빈 SD 다섯 | 0 ×5 |
| 29 (`0xf0`) | **SD 덩어리 총 크기** | `ulong` (`0x152`) | **(가)** | 〃 (칸 30 의 길이) | **칸 28 합 ≤ 이 값** (안 막음 → over-read) | 칸 28 합 (= 0) |
| 30 (`0xf8`) | **SD 덩어리** (`R9` 로 `InitializeSystemSecurityDescriptors` 에) | `[out] uchar**` (`FC_UP` · 널 허용 + 조건 길이 = 칸 29; 형식 `0x164`) | **(가)** | `.asm +0x4f / +0x8c / +0xc2 / +0xf4 / +0x12b` `memcpy(조각, 덩어리 + 누적, 크기)` | NULL 은 **크기 합이 0 일 때만** | NULL |
| 31 (`0x100`) | **반환 상태** | `error_status_t` | 서버가 정함 | 람다 `+0x884 RetryRPC` · `+0x8aa CheckStatus` (표 3) | 0 = 성공 | **0** |

- **전역에 들어가고 "성공" 을 알리는 줄**: `gGotSecurityData` (rva `0x338290`) = **람다 `+0xa83 MOV [0x180338290],R15D` (=1)** — 위 검사를 다 지나고 `InitializeSystemSecurityDescriptors` 도 성공한 **다음**. 이어 `+0xae4 OR [_dwFlags],1` (`GetConnection +0x34 / +0x56` 가 보는 "이미 연결됨" 깃발) · 마지막 `+0xb30 XOR EAX,EAX` 로 0 반환. 지우는 곳 `Cleanup +0x2c`. **읽는 쪽 (이름만, `_refs/0x338290.tsv`)**: `CoInitializeSecurity +0xa6 / +0x48e` · `SetExplicitWinstaDesktopString +0xa` (켜져 있으면 `RpcETooLate`) · `Set +0xeb / +0x1b9 / +0x2b2 / +0x2ea` · `Query +0x79` (둘 다 이름이 겹침 — 아마 전역 옵션 `Set`/`Query` `[이름 추측]`) · `CoAllowUnmarshalerCLSID +0x22` · `CoEnableAutomaticClientDependencyCreation +0x4` · `IsMethodInvocationAllowed +0x6b` · `IsObjectCreationAllowed +0x6d`.
- 응답 선 위 순서는 `[규칙]`: 컨텍스트 핸들 20 → 핑 u32 → DSA 유니크 id (+ 본문) → MID u64 (8 정렬) → OID 배열 (조건 길이 u32 = 10, 8 정렬, 80 B) → 개수 → 설정 비트 → 컨테이너 둘 → 레거시 글 id → 인증 · 가장 → 서버 개수 · id → 클라이언트 개수 · id → 채널 훅 개수 · id → 스레드 번호 · PID → 서명 u64 → GUID 16 → SD 크기 20 → 총 크기 → 덩어리 id → 상태. NULL 인 유니크 포인터는 id 0 만 실린다. **진짜 바이트는 안 풀었다** (VM 묶음).

### 2-3. 서버 쪽 검사 (rpcss `_Connect`) — 요청 · 응답 꼴을 정하는 것

- 호출자 확인 (`+0x161` ret `+0x166` `CheckLocalCall`): `I_RpcBindingInqTransportType == 4` (ncalrpc) 아니면 **상태 5**. `+0x177` (ret `+0x17c`) `RetrievePIDForLocalClient` (ALPC 메시지의 호출자 PID) 실패도 **5** (`+0x168 / +0x180 JZ → +0x73a MOV EAX,5`).
- 입력 검사 (`+0x186 ~ +0x264`): **winsta 글 포인터 NULL** (`+0x186 CMP [RBP-0x38],RDX`) · 출력 포인터 NULL · 깃발 `& 0xfffffff8` ≠ 0 · 깃발 4 인데 `IsGetDesktopAppXComRootHandlePresent` (`+0x27d`) 가 거짓 → **상태 `0x57`** (`+0x733 MOV EAX,0x57`). 즉 **진짜 rpcss 는 winsta 글이 NULL 이면 `Connect` 를 거절한다** → combase `CheckStatus(0x57)` = `0x80070057` (sast/134 의 "핸들 0 → NULL 을 그대로 `Connect` 에" 길의 진짜 끝). exe 경로 (R8) 는 NULL 검사 없음.
- 메모리 부족 `0xe`. 그 밖 성공 0.

---

## 표 (3) 실패 처리 표

`RetryRPC` (rva `0x3790c`, 137 B) · `CheckStatus` (rva `0x37958`, 114 B) 는 combase 의 **모든** 프로시저 스텁 (`CallTheResolver +0x117 / +0x122` 처럼) 이 같이 쓰는 도우미다.

| 받은 것 | `RetryRPC` (`+0x884`) 가 하는 일 | `CheckStatus` (`+0x8aa`) 가 하는 일 | 다시 거나 | 돌려주는 HRESULT | 줄 |
|---|---|---|---|---|---|
| 상태 **0** | 0 반환 (`+0xd XOR EAX,EAX`) | 0 반환 (`+0xc`) | 아니오 | (뒤 검사들을 지나면 람다 0) | `RetryRPC +0x4 ~ +0xd` · `CheckStatus +0x8 ~ +0xc` |
| 상태 **`0x6bb`** (1723, `RPC_S_SERVER_TOO_BUSY` 짐작 `[이름 추측]`) | **1 반환** — 앞서 `Sleep(100)` (`+0x21fa8e MOV ECX,0x64` · `+0x21fa93` ret `+0x21fa9a`) | (루프 안에서는 안 불림) | **예 — 횟수 제한 없음.** 람다 `+0x899 JNZ → +0x6b4` 로 `Connect` 직전부터 되돌아가 (서버 · 클라이언트 목록 · 채널 훅 · 레거시 글을 비우고) 같은 요청을 다시 보냄 | 없음 (끝없이 반복) | `RetryRPC +0x4 MOV EDX,0x6bb` · `+0x9 CMP ECX,EDX` · `+0xb JZ +0x21` · far `+0x21fa9f MOV EAX,1`; 람다 `+0x884` (ret `+0x889`) · `+0x897 TEST EAX,EAX` · `+0x899 JNZ +0x6b4` |
| 그 밖 양수 N (`0x6b5` · `0x6ba` · `0x6be` · `0x6bf` · `0x57` · `0xe` …) | 0 | **`0x80070000 \| (N & 0xffff)`** | **아니오** | `0x8007NNNN` → 람다 `+0x8b3 JNS` 안 지남 → `+0x8bc MOV EDX,0x384` (글 "Failure in Connect call to rpcss") → `Return_HrMsg` → 람다 EDI 반환 → `GetConnection +0xd2 JNS` 안 지남 → `CoInitializeSecurity +0x486 JS +0x89c` → 같은 값 | `CheckStatus +0x27 TEST EBX,EBX` · `+0x29 JLE` (양수만 변환) · `+0x2b MOVZX EBX,BX` · `+0x2e OR EBX,0x80070000` |
| 상태 **5** | 0 | `0x80070005` | 아니오 | 같은 길 + **`GetConnection +0xc5 CMP EAX,0x80070005` → `JZ +0x1bc84c`** (`GetProcessToken` · `LogAccessFailed` 갈래 — **안 읽음**, Q257b) | `GetConnection +0xc5` / `+0xca` |
| 음수 (`0xc…` NTSTATUS 꼴) | 0 | **그대로** (`JLE` 로 변환 건너뜀) | 아니오 | 그 값 | `CheckStatus +0x29 JLE +0xc` |
| **bind 단계 fault** (네 번 중 어느 것이든, 상태 N) | **안 부름** (`Connect` 앞) | **안 부름** — `CreateFromBindingString +0x16b MOVZX EBX,DI` · `+0x16e OR EBX,0x80070000` 이 같은 규칙으로 직접 변환 (`CreateFromBindingHandle` 도 같은 꼴) | 아니오 | `0x8007NNNN` → 람다 `Return_HrMsg` (`+0x300` / `+0x306` / `+0x30a` / `+0x30e`) → 반환. **N = `0x6b5` 면 `0x800706b5`** = 로그의 `CoInitializeSecurity = 0x800706b5` | `CreateFromBindingString +0xee JNZ +0x1b6` (추적 글) → `+0x167` → `+0x16b MOVZX` · `+0x16e OR EBX,0x80070000` → `+0x174 JMP +0x10a`; `CreateFromBindingHandle` 은 먼 범위 `+0x14b731 MOVZX EBX,DI` · `+0x14b734 OR EBX,0x80070000`; 람다 `+0x341 ~ +0x34a` |
| `Connect` 0 인데 `_ph == NULL` | | | 아니오 | **`0x8000ffff`** (E_UNEXPECTED), 줄 `0x387` | 람다 `+0x8c6 ~ +0x8d9` → `+0xb41 ~ +0xb9f MOV EDI,0x8000ffff` |
| `Connect` 0 인데 DSA 포인터 NULL / `gdwScmProcessID == 0` / `_ProcessSignature == 0` | | | 아니오 | **`0x8000ffff`**, 줄 `0x388` / `0x389` / `0x38a` | `+0x8de` · `+0x8e8` · `+0x900` |
| DSA 객체 만들기 실패 | | | 아니오 | `0x8007000e` (줄 `0x38e`) | `+0x959 ~ +0x967` |
| 레거시 보안 글 길이 < 2 | | | 아니오 | `0x8000ffff` (줄 `0x398`) | `+0x983 ~ +0x993` |
| `InitializeSystemSecurityDescriptors` 실패 (`LocalAlloc` NULL) | | | 아니오 | 그 값 (줄 `0x39c`) | `+0x9b8 ~ +0x9c3` |
| `IsDeveloperLicenseInstalled` 음수 (Connect 앞) | | | 아니오 | 그 값 (줄 `0x314`) | `+0x431` (ret `+0x436`) · `+0x43b ~ +0x452` |
| `RegisterWithPsmIfNecessary` 음수 (Connect 앞) | | | 아니오 | 그 값 (줄 `0x331`). 보통 패키지 앱이 아니면 호출 안에서 0 (`RegisterWithPsmIfNecessary +0x14 ~ +0x1f`) | 람다 `+0x548` (ret `+0x54d`) |
| `SuspendImpersonate` 음수 | | | 아니오 | 그 값 (줄 `0x359`) | 람다 `+0x62b` · `+0x647` |
| **암묵 길 (`InitRemoting`)** 에서 `CoInitializeSecurity` 가 `0x80070057` | | | 아니오 | **`0x80040153`** (`REGDB_E_INVALIDVALUE` 짐작) 로 바뀜. 다른 음수는 그대로 + `UninitializeSecurity` | `InitializeSecurity +0x83 CMP EAX,0x80070057` · `+0x88 JZ +0x90` · `+0x90 MOV EBX,0x80040153` · `+0x95` |

- **"서버가 그 일을 못 함" 으로 쓸 수 있는 정직한 값**: `0x6bb` 를 뺀 아무 양수 N. **가장 자연스러운 것은 `0x6ba` (1722, 서버 없음)** — rpcss 가 안 떠 있을 때와 같은 HRESULT `0x800706ba`. `0x6b5` (bind 단계, 지금) · `0x6bf` · `0x6be` 도 깨끗이 끝난다. **피할 것: `0x6bb`, `5` (별도 갈래), `0` 인데 값이 빈 응답 (E_UNEXPECTED 로 끝나지만 `MicrosoftTelemetryAssertTriggeredNoArgs` 호출이 남음)**.
- fault 응답으로 이 상태를 주는 길: 서버 쪽 fault (`+0x28 == 2`, `+0x30` 상태) 는 클라이언트의 `SanitizeErrorCodeFromServer` 가 **0 · 치명 14종만 `0x6c0` 으로 바꾸고** (sast/130 1-3), 그 밖 값은 그대로 전달하므로 `0x6bb` 도 그대로 통과한다 (금지 꼴이 실제로 닿는다). 전송 쪽 오류 (ALPC 상태 → RPC 상태, sast/130 2-5 표) 에는 `0x6bb` 로 가는 줄이 **없다** — `0x6bb` 는 서버가 직접 주는 경우뿐.

---

## 표 (4) 그다음 지도 — 이름 · 번호 · 줄만 (본문은 다음 요청)

### 4-a. `CoInitializeSecurity` (명시 호출) 나머지

- `+0xa6 CMP [gGotSecurityData],EDI` · `+0xac JZ +0x47d` → **`+0x47d GetConnection` (ret `+0x482`)** → `+0x486 JS +0x89c` (실패면 `+0x8a9 MOV EAX,EDI` 로 같은 HRESULT 반환) / 성공이면 `+0x48c XOR EDI,EDI` · `+0x48e CMP [gGotSecurityData]` · `+0x494 JNZ +0xb2` 로 **본 줄기에 합류** (전역에 넣는 일은 이 함수 몫, 이 포트로 더 안 간다).
- 이 포트로 가는 줄은 **`+0x3f6 ~ +0x43c`** 한 곳: `g_AppId` (rva `0x336fa8`) 가 `GUID_NULL` 이 아니면 `+0x420 I_RpcSetDCOMAppId` (rpcrt4, 프로세스 안) → **`+0x43c SetAppID` (ret `+0x441`)** = **ILocalObjectExporter 프로시저 1** (`[in] _ph`, `[in] GUID*`; combase 스텁 rva `0x7f818`, 서버 `_SetAppID` rva `0x6d340`). `g_AppId` 를 정하는 쪽은 `+0x64a utGetAppIdForModule` · `+0x662 wCLSIDFromString` (실행 파일 이름으로 레지스트리 AppID 찾기 — 안 읽음 `[공백]`). 일반 exe 는 AppID 가 없어 `GUID_NULL` 일 것 `[추측]`.
- 같은 함수의 `+0xdd RegisterAuthnServices` (조각 `FUN_18025a534 +0xdd`) 는 프로세스 안 (RPC 런타임 등록).

### 4-b. `CoInitializeEx`

- `CoInitializeEx.asm +0x44 _CoInitializeEx` → `_CoInitializeEx +0x34f ThreadFirstInitialize` …. 지도 `ms/disassemble/combase/_map/CoInitializeEx_{edges,nodes}.tsv` (뿌리 `CoInitializeEx`, 깊이 10, 공용 도구 기준 1000 = 거의 안 거름, 함수 514개): **`GetConnection` · `Connect` · `SetAppID` 가 없다.** 이 포트로 닿는 것은 두 스텁뿐: `Disconnect` (프로시저 10, 깊이 7 · 8) 와 `ServerFreeOXIDAndOIDs` (프로시저 8, 깊이 8) — `ThreadFirstInitialize → IncrementProcessInitializeCount → ProcessUninitialize → …Cleanup… → CRpcResolver::Cleanup (rva 0x3e5ec — `gGotSecurityData` · `_dwFlags` 를 **지우는** 곳, `_refs` 의 `Cleanup +0x2c / +0x23`) → Disconnect` 로 이어지는 **되돌리기 길** (이름 `Cleanup` 이 겹쳐 중간 고리는 `[추측]`).
- → **`CoInitializeEx` 만으로는 `Connect` 가 안 나간다** (끊긴 끝 `indirect-cfg` 33곳의 간접 호출은 못 봄 `[공백]`). 처음 나가는 것은 첫 COM 사용 때 (4-c).

### 4-c. 첫 COM 사용 때 이 포트로 가는 첫 부름

`CoCreateInstance` 지도 (`_map/CoCreateInstance_*`, 뿌리 `CoCreateInstance`, 깊이 12, 함수 1,555개) 에서 이름이 하나뿐인 함수로만 이어진 사슬, 각 고리를 `.asm` call 줄로 확인 (괄호는 앞 함수의 call 자리):

```
CoCreateInstance +0xd1 → CoCreateInstanceExCopy +0x162 → DoCreateInstance +0x1d7 → ICoCreateInstanceExWorker
  +0xb52 → GetActivationPropertiesIn +0x1fa → CoGetDefaultContext (+0x3f 확인) → InitChannelIfNecessary (+0x119 확인)
  → CComApartment::InitRemoting (rva 0x36ad4)
        +0xb6 (확인, 조건: 호출 앞 +0xa9 간접 호출이 참) → InitializeSecurity (rva 0x7deec) +0x7c (확인) → CoInitializeSecurity   ← 암묵 호출
        +0x29d (확인) → CComApartment::CallTheResolver (rva 0x37608) → EnsureLinkToPrimaryOxid(+0x78) · NdrClientCall2(+0x106, 프로시저 7) · RetryRPC(+0x117) · CheckStatus(+0x122) · RegisterOXIDAndOIDs(+0x1b7) → ServerRegisterOXID (프로시저 6)
```

- **(a) in-proc `CoCreateInstance`**: 클래스가 in-proc 이어도 위 길이 먼저 탄다 → ① `InitializeSecurity` 가 `CoInitializeSecurity` 를 **암묵 호출** (`InitializeSecurity.asm +0x1c ~ +0x7c`: 뮤텍스 · `gpsaSecurity` (`0x338270`) 가 NULL 일 때만 · 인자 `ECX=0 · EDX=-1 · R8=0 · R9=0 · [RSP+0x20]=1 · [RSP+0x28]=2 · [RSP+0x30]=0 · [RSP+0x38]=8 · [RSP+0x40]=0`) → `GetConnection` → **bind ×4 + `Connect`**, ② 이어 **프로시저 7 (OID 할당 `[이름 추측: ServerAllocateOIDs]`) → 프로시저 6 (OXID 등록)**. 그래서 **`CoInitializeSecurity` 를 직접 안 부른 프로세스 (r18 432 샘플)** 도 `CoCreateInstance` 한 번에 같은 `0x800706b5` 를 만난다 (로그의 `CoCreateInstance` 1 샘플과 맞음; 시스템 DLL 안에서 삼켜지는 건 부른 쪽이 반환값을 안 보는 것 `[추측]`).
- **(b) `CoGetObject`** — `CoGetObject` 는 combase 가 아니라 **ole32** (`MkParseDisplayName` · 모니커) 라 이번에 안 열었다 `[공백]`. 어느 길이든 처음엔 같은 `GetConnection` → bind ×4 → `Connect` (그 앞이 `GetConnection` 호출처 46곳, 4-d) 에서 막히므로 `0x800706b5` 로 끝나는 모양은 로그 (`CoGetObject` 46 샘플) 와 같다. 후보 첫 부름: ROT `IrotGetObject` (IROT 프로시저 3, `GetConnection +0x23`) · `IMgotGetObject` (IROT 9, `+0x29`).
- **(c) out-of-proc · 서비스**: `DelegateActivationToSCM +0x156` (ret `+0x15b`) `BindToSCMProxy` → `BindToSCMProxy +0x17 GetConnection` · `+0x49 MakeSCMProxy` · `+0x81 MakeCloakedSCMProxy` — **ILocalObjectExporter 가 아니라 ORPC (DCOM) 프록시**로 rpcss 활성화 인터페이스에 걸린다. 활성화 요청에 위 `_ProcessSignature` (`DelegateActivationToSCM +0x1ac` 가 읽음) 와 `_pdsaLocalResolver` (로컬 해결사 문자열 바인딩), `gdwScmProcessID` (rpcss 인지 가리는 비교) 가 쓰일 것으로 보인다 `[추측]`. "그 클래스 · 서비스 없음" 일 때의 HRESULT 길은 **이번에 안 읽었다** `[공백]` (이름 · 상수는 다음 요청). 지금 확인된 상수는 4-a · 표 3 의 `0x80070057 → 0x80040153` 뿐.

### 4-d. `GetConnection` (rva `0xa14d0`) 을 부르는 곳 — 46곳 (`ms/disassemble/combase/_refs/0xa14d0.tsv`)

- **명시 · 암묵 보안**: `CoInitializeSecurity +0x47d`.
- **해결사 쪽 도우미 (ILocalObjectExporter 를 쓰는 것으로 보임 `[추측]`; 프로시저 번호는 표 4-e)**: `GetUpdatedResolverBindings +0x17` (11) · `GetDefaultSecurityPermissions +0x22` (2) · `ClientRegisterOIDWithPingServer +0x2e9` · `ClientDeRegisterOIDFromPingServer +0x108` · `GetLocalMIDEntry +0x3e` · `FindOrCreateMIDEntry +0x93` · `GetSystemSecurityDescriptors +0xf` · `EnsureWorkerThread +0x16`.
- **ORPC SCM 프록시 만들기**: `BindToSCMProxy +0x17` (`DelegateActivationToSCM +0x156` 이 부름).
- **IROT** (`IrotRegister` 0 · `IrotRevoke` 1 · `IrotIsRunning` 2 · `IrotGetObject` 3 · `IrotNoteChangeTime` 4 · `IrotGetTimeOfLastChange` 5 · `IrotEnumRunning` 6 · `IMgotRegister` 7 · `IMgotRevoke` 8 · `IMgotGetObject` 9 · `IMgotEnumRunning` 10 — rpcss 서버 표 순서): `IrotGetObject +0x23` · `IrotRevoke +0x25` · `IrotNoteChangeTime +0x1e` · `IrotRegister +0x2a` · `IrotIsRunning +0x1e` · `IrotEnumRunning +0x1e` · `IrotGetTimeOfLastChange +0x20` · `IMgotGetObject +0x29` · `IMgotRevoke +0x25` · `IMgotEnumRunning +0x26` · `IMgotRegister +0x29`.
- **ISCM · IMachineActivatorControl** (서버 이름이 같음 `[이름 추측: 클라이언트 스텁 이름 = 서버 프로시저 이름]`): `NotifyStarted +0x22` · `NotifySurrogateStarted +0x19` · `NotifyActivatableClassStarted +0x1e` · `RegisterWindowPropInterface +0x24` · `GetWindowPropInterface +0x3c` · `QueryServerProcessHandleHeld +0x1e` · `NotifyComClassChangesFromDeployment +0xf` · `NotifyDDStartOrStop +0x16` · `GetThreadID +0x15` · `ControlTracingForProcess +0x1d` · `DecodeProxy +0x3d` · `EnableDisableDynamicIPTracking +0x16` · `FlushSCMBindings +0x17` · `GetCurrentAddrExclusionList +0x1e` · `IsObjectCreationAllowed +0x2a` · `NotifyWinRTActivationStoreChanged +0xf` · `QueryPIDForActivation +0x20` · `RegisterConsoleHandles +0x25` · `RegisterRacActivationToken +0x1e` · `RetireServer +0x17` · `RevokeConsoleHandles +0x17` · `RevokeRacActivationToken +0x17` · `SetAddrExclusionList +0x1d` · `UpdateActivationSettings +0x47` · `LogMachineClassesRootPermissions +0xf`.

### 4-e. ILocalObjectExporter 프로시저 표 (번호 · combase 스텁 · rpcss 서버)

combase 쪽 번호는 각 스텁의 `LEA RDX,[…ProcFormatString+…]` 가 가리키는 프로시저 문자열 위치 (프로시저 12개를 차례로 풀어 매김), 서버 이름은 rpcss 디스패치 표 (`DispatchTable` rva `0x128760`, 칸 번호 = 프로시저 번호).

| # | combase 스텁 (rva) | rpcss 서버 (rva) | 한 줄 |
|---|---|---|---|
| 0 | `Connect` (`0x12f318`) | `_Connect` (`0x76d80`) | 이 문서의 표 (2) |
| 1 | `SetAppID` (`0x7f818`, `+0x2c LEA RDX,[…+0xe0]`) | `_SetAppID` (`0x6d340`) | `[in] 컨텍스트 핸들, [in] GUID*` |
| 2 | `GetDefaultSecurityPermissions` (`0x18d514`) | `_GetDefaultSecurityPermissions` (`0xad320`) | 입력 없음, 출력만 (`[out]` 셋 + 상태) — 뜻은 안 풂 `[공백]` |
| 3 | `ServerGetReservedID` (`0x9478c`) | `_AllocateReservedIds` (`0x6d220`) | 예약 OID 더 받기 |
| 4 | `ClientBulkUpdateOIDWithPingServer` (`0x40110`) | `_BulkUpdateOIDs` (`0x21e70`) | OID 핑 묶음 갱신 |
| 5 | `ClientResolveOXID` (`0x1355e0`) · `ClientResolveMachineLocalOXID` (`0x99c64`) | `_ClientResolveOXID` (`0x67880`) | OXID → 바인딩 풀이 |
| 6 | `ServerRegisterOXID` (`0xa1e68`) | `_ServerAllocateOXIDAndOIDs` (`0x13990`) | OXID 등록 |
| 7 | `CallTheResolver` (`0x37608`, `CComApartment::` 의 것) | `_ServerAllocateOIDs` (`0x213e0`) | OID 할당 |
| 8 | `ServerFreeOXIDAndOIDs` (`0x123504`) | `_ServerFreeOXIDAndOIDs` (`0x15bc0`) | 해제 |
| 9 | `SetServerOIDFlags` (`0x12b010`) | `_SetServerOIDFlags` (`0x12120`) | |
| 10 | `Disconnect` (`0x3e884`) | `_Disconnect` (`0x6ad70`) | 연결 끊기 (`[in,out] 컨텍스트 핸들`) |
| 11 | `GetUpdatedResolverBindings` (`0x18dca4`) | `_GetUpdatedResolverBindings` (`0xafd40`) | |

(**열두 프로시저 모두** 클라이언트 · 서버 프로시저 문자열의 파라미터 수 · 속성 · 스택 칸이 같다 — 클라이언트 문자열 오프셋 = 서버 오프셋 + 2 (`pefile` 로 둘을 차례로 풀어 비교). 그래서 번호 짝은 확실하고, 이름은 각 쪽 PDB 이름 그대로다.) 다른 세 인터페이스의 서버 이름 표는 `rpcss.dll` 디스패치 표 (위 1-4 의 rva) 에서: IMachineActivatorControl `ProcessActivatorStarted · …Initializing · …Ready · …Stopped · …Paused · …Resumed · …UserInitializing`, ISCM `ServerRegisterClsid · ServerRevokeClsid · ServerRegisterActivatableClasses · ServerRevokeActivatableClasses · GetThreadID · UpdateActivationSettings · RegisterWindowPropInterface · GetWindowPropInterface · EnableDisableDynamicIPTracking · GetCurrentAddrExclusionList · SetAddrExclusionList · FlushSCMBindings · RetireServer · NotifyDDStartOrStop · IsObjectCreationAllowed · ControlTracingForProcess · QueryPIDForActivation · NotifyWinRTActivationStoreChanged · DecodeProxy · NotifyPsmResume · QueryServerProcessHandleHeld · RegisterRacActivationToken · RevokeRacActivationToken · RegisterConsoleHandles · RevokeConsoleHandles · NotifyComClassChangesFromDeployment · LogMachineClassesRootPermissions`.

---

## "지금 우리 (PE_emulator) 맞나 · 틀리나"

읽은 파일: `ext/syscall/syscall_layer_cpp/src/rpc/lrpc.cpp` · `epm.cpp` (2026-11-26 읽은 시점, 읽기만).

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| `lrpc.cpp:74 ~ :77` 창구가 아는 인터페이스는 `window.definition->interfaces` (EPM `e1af8308…` v3.0 하나, `epm.cpp:33 ~ :44`), 그 밖 bind 는 fault `0x6b5` | 진짜 combase 는 같은 연결로 EPM 이 아닌 **네 인터페이스**를 bind (1 번 `e60c73e6…` v2.0). 하나라도 모르면 거기서 `0x800706b5` 로 끝 | **지금 대답은 안전한 정직한 실패 — 맞음.** 재시도 없이 깨끗이 끝남 (표 3). 로그 "한 API 안 거절 1" 과 일치 |
| `lrpc.cpp:80 syntax = (mask & 2) ? 2 : (mask & 1)` + 창구 마스크 1 (`epm.cpp:42`) | 이 클라이언트는 NDR 2.0 전용 (`NdrClientCall2`). 제안 마스크는 `1` 로 짐작 (1-3) | **맞음.** 창구 마스크가 1 이라 `1` 로 답한다. 이 인터페이스들을 올릴 창구도 마스크 `1` 로 |
| `lrpc.cpp:99 ~ :103` 요청 → `bindings[슬롯]` 로 인터페이스를 찾은 뒤 `procedures` 를 **`opnum` 만** 비교 | 같은 포트에 인터페이스 5개 (EPM + 4) 가 오면 번호가 **겹친다**: EPM `8` = `ept_map_auth_async` vs ILocalObjectExporter `8` = `ServerFreeOXIDAndOIDs` · IROT 0 ~ 10 · ISCM 0 ~ 26 … | **틀려질 예정.** 프로시저 표를 (인터페이스 UUID, 번호) 로 찾아야 한다 `[소스: 읽은 시점 코드]` |
| `lrpc.cpp:186` 동기 연결 (`Flags & synchronous`) 은 창구가 있으면 받음 | 이 호출은 동기 연결 + 동기 bind (`RpcBindingBind(NULL…)`) | 맞음 |
| 슬롯 (`lrpc.cpp:83 ~ :84` `connection.bindings[슬롯]`) | 한 연결에 bind 가 넷 (슬롯 서로 다름) `[추측: 같은 연결 재사용]` — 슬롯 표는 이미 여러 개 받을 수 있는 꼴 | 맞음 (4 번 bind 가 새 연결이어도 연결마다 같은 코드가 돈다) |
| `epm.cpp` EPM 프로시저 8 이 하나뿐 | 이번 포트의 다른 인터페이스 몸은 아직 없음 | 짓는 중 |

### stub 이 돌려줄 최소 대답 (성공 쪽을 만들 때)

1. **연결**: 동기 연결을 받음 (`RequiredServerSid` = NetworkService SID 가 와도 받음 — 이미 됨).
2. **bind 넷** (인터페이스 UUID · 버전 표 1-2): 각각 bind 답 = 종류 1 · `+0x48 = 1` (NDR 2.0) · `+0x08` = 서버 프로세스 ID (0 이 아닌 값, 아래 `gdwScmProcessID` 와 같게) · 슬롯 = 제안한 값 그대로. **넷 중 하나라도 거절하면 `0x800706b5` 로 끝난다.**
3. **`Connect` (슬롯 → ILocalObjectExporter, 번호 0)**: 응답 스텁 = 표 (2-2) "최소 대답" 열. 요약: 컨텍스트 핸들 (0 이 아닌 GUID) · 핑 360 · DSA (비-NULL 구조체) · MID (0 이 아닌 값) · OID 배열 열 칸 0 + 개수 0 · 설정 비트 0 · 컨테이너 0/0 · 레거시 NULL · 인증 2 · 가장 2 · 서비스 목록 셋 개수 0 / NULL · 스레드 번호 (1 부터) · PID (0 이 아닌 값) · 서명 (0 이 아닌 u64) · 프로세스 GUID · SD 크기 0 ×5 · 총 크기 0 · 덩어리 NULL · **상태 0**. 값 중 **`TODO(VM)`** 로 둘 것: DSA 내용 · 설정 비트 · 서비스 목록들 · SD 조각 (진짜 값은 VM 묶음).
4. 값을 "지어낸" 칸은 서버 **자기 정체** (핸들 · PID · 서명 · GUID · MID · 스레드 번호) 뿐 — 시스템 설정 사실을 지어내는 칸은 없다 (기본값 2/2 · 360 · 0 은 rpcss 코드의 기본값 `[정적]`).
5. 그 뒤 프로시저 (7 · 6 · 10 · 1 · …) 는 **몸을 모르면 fault** (아래 "그다음 벽"). fault 상태는 `0x6ba` 를 권한다.

### 금지 꼴

1. **fault 상태 `0x6bb`** (또는 서버 반환 `0x6bb`): `RetryRPC` 가 100 ms 마다 영원히 다시 건다 (표 3).
2. **응답 없음**: 동기 호출 (`NtAlpcSendWaitReceivePort` `Flags 0x20000`) 이 영영 안 돌아온다 — 호출 시간제한은 `CallTimeout 0xffffffff` (무한) `[추측]`.
3. **상태 0 인데 `_ph` · DSA · `gdwScmProcessID` · `_ProcessSignature` 중 하나가 0 / NULL**: 끝나기는 하지만 `0x8000ffff` + 텔레메트리 호출. 컨텍스트 핸들의 속성과 GUID 를 둘 다 0 으로 주면 NULL 핸들.
4. **예약 OID 개수 > 10**: `_OidsReserved` (0x50 B) 를 넘쳐 쓴다 (메모리 오염).
5. **SD 크기 합 > 덩어리 크기, 또는 크기가 있는데 덩어리 NULL**: `InitializeSystemSecurityDescriptors` 가 범위 밖을 읽는다.
6. **문법 마스크 `2` (NDR64) 로 bind 답**: 이 클라이언트 스텁은 NDR 2.0 전용.
7. **상태 `5`**: `GetConnection +0xc5` 의 별도 갈래 (안 읽음) — 피하기.
8. **bind 마스크 `3` (둘 다) 로 답**: "비트 하나" 검사에 걸려 `0x6c0` (sast/130 1-3).

### "그다음 벽" 한 줄

`Connect` 가 성공하면 같은 연결로 **첫 COM 사용에서 `CComApartment::InitRemoting → CallTheResolver` 가 프로시저 7 → 6 (OID 할당 · OXID 등록)** 을 바로 보내고 (`CoCreateInstance` 는 in-proc 이어도), 그다음은 **활성화 (`DelegateActivationToSCM` → ORPC)** 와 ROT (IROT 3 · 9) 라 — 그 몸을 짓기 전에는 7 번에서 `0x6ba` 류 정직한 실패로 끝내는 것이 안전하다 (그러면 `CoInitializeSecurity` 는 성공하고 첫 COM 사용 (`CoCreateInstance` 등) 이 `0x800706ba` 류로 실패한다 — 지금의 `CoInitializeSecurity = 0x800706b5` 와 증상이 달라져 로그 비교가 달라 보일 수 있다).

---

## 정적으로 못 닫은 것

- `[공백]` **bind 2 ~ 4 번이 같은 연결 (같은 포트 · 같은 `NtAlpcConnectPort`) 을 쓰는지**: `PickAssociation` 이 끝점 이름 · 상태 · `[+0x48]` 만 비교하는 줄까지만 봤다. 보안 값 (SID · 가장 수준) 이 다르면 새 연결이 될 수 있다.
- `[공백]` DUALSTRINGARRAY 의 **진짜 내용**과 뒤 쓰임 (`MakeSCMProxy` 가 이 글로 활성화 연결을 만드는 것으로 보임 `[추측]`).
- `[공백]` 설정 비트 · 서버/클라이언트 인증 서비스 목록 · 채널 훅 · SD 다섯 조각의 **VM 쪽 진짜 값** (rpcss 전역은 레지스트리에서 채워짐: 인증/가장 수준은 `ComputeSecurity +0x1103 / +0x1190` 의 `RegQueryValueExW`).
- `[공백]` `LocalInterfaceOnlySecCallback` (rva `0x1fbf0`, 2721 B) 가 **로컬 ncalrpc 클라이언트 (test.exe 같은) 를 거절하는 조건** — 안 읽음. 거절하면 bind 가 `5` 로 끝나 `GetConnection +0xc5 E_ACCESSDENIED` 갈래 (Q257b) 로 들어간다.
- `[공백]` `IsDeveloperModeEnabled` (KERNELBASE) 가 **어떤 레지스트리 · 값을 읽는지** (`KIsUnlockSettingEnabled` 안 읽음): `Connect` 앞에서 도는 클라이언트 쪽 검사, 음수면 `Connect` 에 못 간다 (줄 `0x314`).
- `[공백]` `CoGetObject` (ole32), 모니커별 첫 부름. 활성화 "클래스 없음" HRESULT 길.
- `[추측]` bind 마스크 `1` (짐작) · OLE TLS `+0x10` 의 이름 · `RetryRPC` 의 `0x6bb` 이름 (`SERVER_TOO_BUSY`) · `0x80040153` 이름.
- `[공백]` 응답 선 위 **진짜 바이트** (조건 길이 · 정렬 · 포인터 id 값) — 이 문서의 선 위 순서는 NDR 규칙 `[규칙]`.

## 동적으로 확인할 것 (VM 묶음 — Q256 · Q271 · Q280 의 RPC 묶음에 얹기)

멈춤점은 **test.exe (또는 `schtasks.exe` · `taskkill.exe` 같은 COM 쓰는 자식) 프로세스일 때만** 조건을 붙인다. 진짜 rpcss 쪽 (`svchost -k rpcss` 의 rpcss) 은 따로 그 프로세스 조건.

| # | 멈춤점 | 볼 것 | 닫는 질문 |
|---|---|---|---|
| D1 | `combase!CRpcResolver::GetConnection+0xbe` (람다 호출) 한 번 | 이 호출이 처음인지 (`_dwFlags & 1 == 0`) | 처음 연결 길인지 |
| D2 | `rpcrt4!LRPC_CASSOCIATION::Bind+0x626` (call) / `+0x62d` (ret) (bind 메시지 보내기, sast/130) 와 `NtAlpcConnectPort` 입구 | 한 `GetConnection` 안에서 **연결이 몇 번**, bind 가 몇 번 (UUID 앞 4 B · `+0x48` 마스크 · `+0x4c` 슬롯) 나가는가 | 1-2 의 "같은 연결, bind ×4" |
| D3 | `rpcrt4!CompleteBind` 입구 (sast/130) · 진짜 서버가 보낸 bind 답 `db @rdx L0x70` | 진짜 rpcss 가 고르는 `+0x48` 마스크 (1 인가) · `+0x08` PID | 마스크 1 · PID 값 |
| D4 | **`combase+0x18c1ee`** (람다 `+0x82e`, `Connect` 호출) 입구 | `rcx` (바인딩 핸들), `rdx` / `r8` (`du` 로 winsta 글 · exe 경로), `r9` (NULL 인가), 스택 `[rsp+0x20]` 깃발 · `[rsp+0x28]` `0x8664` | 요청 값 (표 2-1) |
| D5 | **`combase+0x18c1f3`** (`+0x833`, `Connect` 반환) | `eax` = 상태. 이어 `dq combase!CRpcResolver::_ph L1` · `dd combase!gdwScmProcessID L1` · `dq combase!CRpcResolver::_ProcessSignature L1` · `dd combase!giPingPeriod L1` · `dq combase!gLocalMid L1` · `dd combase!gAuthnLevel L2` · `dd combase!CRpcResolver::_cReservedOidsAvail L1` · `dq combase!CRpcResolver::_OidsReserved L10` · `dq combase!CRpcResolver::_pdsaLocalResolver L1` | 표 (2-2) 의 진짜 값 (가) ~ (다) |
| D6 | `combase+0x18c26f` (`+0x8af`, `CheckStatus` 반환) | `eax` = HRESULT | 표 (3) |
| D7 | 같은 실행의 응답 스텁 바이트: `rpcrt4!DoSendReceive+0x154` 반환 후 `UnpackResponse` 입구 (sast/130 2-3) | 응답 `+0x40` 부터 스텁 (조건 길이 · 정렬 · 포인터 id) | 응답 바이트 꼴 (`[규칙]` 확인) |
| D8 | **서버**: `rpcss!_Connect+0x73f` (공통 반환, 상태 `eax`) · `rpcss!_Connect+0x59b` (PID) · `+0x562` (`g_usBaseTimeoutInterval`) · `+0x573` (`gLocalMid`) | 서버가 주는 값과 상태 (5 · `0x57` 갈래 포함) | 진짜 값 · winsta 글 NULL 이 `0x57` 인지 |
| D9 | **서버**: `rpcss!LocalInterfaceOnlySecCallback` 반환 (`ret` 직전 `eax`) | 0 인가 5 인가 | 로컬 클라이언트 통과 조건 · Q257b |
| D10 | `combase!IsDeveloperLicenseInstalled+0x10` 반환 (`eax`) · `kernelbase!IsDeveloperModeEnabled` | HRESULT · 읽는 레지스트리 키 | `Connect` 앞 검사 |
| D11 | `combase!CComApartment::CallTheResolver+0x10d` (`NdrClientCall2` ret) · `combase!ServerRegisterOXID` 의 `NdrClientCall2` ret | 첫 COM 사용의 둘째 · 셋째 부름 상태 | 4-c 사슬 |

- **dast 요청 필요**: D2 · D3 · D5 · D7 (필수 — 응답 값 · 마스크 · 연결 수), D4 · D6 · D8 (값 읽기), D9 · D10 · D11 (선택).
- 요청서의 동적 멈춤점 `combase+0x18c1ee` 는 `operator() +0x82e` 가 **맞다** (`0x18018b9c0 + 0x82e = 0x18018c1ee`).

## 다음 요청 후보

1. **rpcss 서버 프로시저 몸** (정직한 최소 응답이 어떤 값이어야 combase 가 지나가나): 프로시저 7 `_ServerAllocateOIDs` · 6 `_ServerAllocateOXIDAndOIDs` · 10 `_Disconnect` · 1 `_SetAppID` 의 입력 · 출력 꼴과 combase 쪽 검사 (`CallTheResolver +0x117 ~ +0x1b7`, `RegisterOXIDAndOIDs`) — 첫 COM 사용의 벽.
2. **`LocalInterfaceOnlySecCallback`** (2721 B) — 로컬 클라이언트 거절 조건 (Q257b 와 묶음).
3. **`MakeSCMProxy` · `DelegateActivationToSCM`** — `_pdsaLocalResolver` 가 어떤 문자열 바인딩을 기대하나, 활성화 첫 ORPC 프로시저 · "클래스 / 서비스 없음" HRESULT 길 (표 4-c (c)).
4. **ole32 `CoGetObject`** (모니커) 의 첫 RPC · ROT `IrotGetObject` 프로시저 3 의 요청 · 응답 꼴.
5. rpcrt4 `PickAssociation` 비교 줄 전부 · `RpcBindingCopy` / `RpcBindingUnbind` — bind ×4 가 연결을 나눠 쓰는지 (D2 와 중복 확인).
6. `KIsUnlockSettingEnabled` — `Connect` 앞의 개발자 모드 검사가 읽는 레지스트리.
7. (하네스) `ExportFunctions` 에 `rva:` 선택 · `CallGraphMap` 에 rva 열 — "스크립트 문제" 참고.

## 뽑은 것

- **combase** (`ms/disassemble/combase/`, `ms/decomp/combase/`; 다시 analyze 안 함):
  - rva 로 (스크래치 복사본): `operator____18b9c0.asm` · `.c` (람다 `operator()` 0x18b9c0, 3558 B).
  - 정규식 `^(Connect|RetryRPC|CheckStatus|GetConnection|CreateFromBindingString|CreateFromBindingHandle|RegisterWithPsmIfNecessary|SuspendImpersonate|ResumeImpersonate|InitializeSystemSecurityDescriptors|CoInitializeSecurity|Return_Hr)$` depth 0 (16개; **`Connect__12f318.asm` 이 우리 것**, `Connect.asm` 은 rva 0xaef40 다른 함수).
  - `^(SetServerOIDFlags|ServerFreeOXIDAndOIDs|ClientResolveMachineLocalOXID|ServerRegisterOXID|ClientBulkUpdateOIDWithPingServer|SetAppID|CallTheResolver|GetUpdatedResolverBindings|GetDefaultSecurityPermissions|ClientResolveOXID|ServerGetReservedID|Disconnect|BindWithTimeout)$`, `^(InitChannelIfNecessary|InitRemoting|InitializeSecurity|CoGetDefaultContext)$` (`InitRemoting.asm` = rva 0x36ad4, `InitRemoting__11e0fc.asm` 은 다른 것), `^(IsDeveloperLicenseInstalled|SuspendImpersonate|ResumeImpersonate)$`, `^(BindToSCMProxy|DelegateActivationToSCM)$`.
  - `refs` (`_refs/`): `0x274270` (StubDesc) · `0x338290` (`gGotSecurityData`) · `0x33814c` (`_dwFlags`) · `0x338110` (`_ph`) · `0x338c78` (`_pdsaLocalResolver`) · `0x33800c` · `0x338120` · `0x336288` · `0x336290` · `0x336b68` · `0x334e5c` · `0x334e60` · `0x338d08` · `0x338cf8` · `0x338d60` · `0x338d18`. (`0xa14d0` · `0x12f318` · `0x18b9c0` 은 sast/134 것.)
  - 지도 (`_map/`): `CoInitializeEx_*` (깊이 10, 기준 1000) · `CoCreateInstance_*` (깊이 12, 기준 1000). `CoGetObject` 는 combase 에 없어 지도가 안 만들어짐.
- **rpcss** (새 analyze, `ms/disassemble/rpcss/`, `ms/decomp/rpcss/`): `^(_Connect|_Disconnect|SetAppID|IObjectExporterSecCallback|StartObjectExporter)$` · `^(UseProtseqIfNecessary|StartListeningIfNecessary|RegisterAuthInfoIfNecessary|ServiceMain|InitializeSCMBeforeListen|InitializeSCMAfterListen|StartServerAndWait)$` · `^(ComputeSecurity|GetLegacySecurity|GetClientServerSvcs|CheckLocalCall|AllocateReservedIdsInternal|CopyMyOrBindings)$` (모두 depth 0). `refs` (`_refs/`): `0x134944` · `0x134958` · `0x12d160` · `0x1641b8` · `0x129500` · `0x128f00` · `0x1292d0` · `0x1648f8` · `0x166068` · `0x165568` · `0x165564`.
- **kernelbase**: `^IsDeveloperModeEnabled$` depth 1 (`IsDeveloperModeEnabled` · `IsLockdownDeveloperModeEnabled` · `KIsDeveloperModeEnabled`).
- **일회용 읽기 (스크래치패드 `…/scratchpad/s144/`, 커밋 안 함)**: `pe.py` (pefile 로 rva 읽기) · `procfmt.py` / `allprocs.py` (MIDL 프로시저 형식 문자열 풀이 — `Oi_flags` · 파라미터 속성 · 스택 오프셋) · `srvif.py` (rpcss 서버 인터페이스 구조체 · 디스패치 표) · `stubdesc.py` · `ExportFunctionsRva.java` · `export_rva.sh`. combase/rpcss PE 데이터 읽은 곳: 인터페이스 구조체 4 (`0x2fcd00` · `0x2fcdf0` · `0x2fcf30` · `0x2fd220`) · 바인딩 글 (`0x2cc510`) · `StubDesc` (`0x274270`) · 프로시저 형식 (`0x2c26e2` ~) · 형식 문자열 (`0x2fc862` ~ `+0x1c0`) · rpcss 서버 구조체 5 · `gaProtseqInfo` (`0x164030`) · `.data` 초기값 (`g_usBaseTimeoutInterval` `0x1648f8` = `0x168`, `gNextThreadID` `0x164910` = 1) · 레지스트리 값 이름 두 개 (`0x1306b8` `LegacyAuthenticationLevel`, `0x130600` `LegacyImpersonationLevel`).
- 읽은 기존 결과서: sast/130 (bind 메시지 · 응답 검사 · ALPC→RPC 상태 표) · sast/132 (MIDL 형식 문자열 읽는 법) · sast/134 (람다 → `Connect`) · sast/143 (같은 포트의 EPM · `epmapper` 한 서비스 묶음). 기존 asm 읽기: `ms/disassemble/rpcrt4/BaseBind.asm` · `PickAssociation.asm` · `Bind__4e330.asm` · `ms/disassemble/rpcepmap/InitializeService.asm` · `StartEndpointMapper` 의 `_calls.tsv` 줄.
- 로그: `ms/ghidra/rpcss.analyze.{log,stdout}` · `combase.export.sast144{a..f}.stdout` · `combase.refs.sast144_*.stdout` · `combase.map.sast144{a,b}.stdout` · `rpcss.export.sast144{a,b,c}.stdout` · `rpcss.refs.sast144_*.stdout` · `kernelbase.export.sast144a.stdout`. **도는 작업 없음.**
