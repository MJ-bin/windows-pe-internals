# 2026-11-23 (작성일, 정적 분석 2026-11-23) run135 — LSA 이름 묻기 RPC (Q249, 정적만): `GetUserName*` 과 `InternetOpen*` 은 **같은 창구** 로 간다 — lsass 의 SSPI 전용 RPC **`\RPC Control\lsasspirpc`** (ncalrpc, 인터페이스 **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0**, sspicli 의 `NdrClientCall3` = 동기). **lsarpc (MS-LSAD · MS-LSAT) 가 아니다.** 프로세스마다 처음 한 번 **프로시저 0 (연결)**, 그다음부터 **프로시저 15 (사용자 이름 묻기)**. wininet 은 보안 꾸러미를 묻는 게 아니라 **`GetUserNameExA(2)` 로 현재 사용자 이름을 한 번 캐시** 하려고 LSA 에 간다 — 요청서의 `EnumerateSecurityPackages` 류 `[추측]` 은 틀렸다. 연결이 실패하면 둘 다 LastError **1722** 로 끝나고, 다른 길로 물러서는 코드는 없다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("`GetUserName*` · `InternetOpen*` 이 LSA 쪽에 거는 RPC", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/309_internals-lsa-sspi.md`, 튜닝 `309_tune12.md` V4, 병목 49 · `GetUserName*` 0 반환 198 · `InternetOpen*` NULL 60) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 advapi32 · sspicli · wininet · rpcrt4 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 과 같은 줄), `sample/*.c` · `log.h` 에 `GetUserName` · `InternetOpen` 같은 글자가 없다 `[소스]`. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (새 PDB 다섯 줄 · rpcrt4 줄 · 근거 줄) · 3장 run133 절 한 줄 · 3장 끝 새 절 · 4장 (Q249 닫힘 줄 · run135 줄 셋) · 5장 (run135 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q249** (사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 셋째). 결과서 판정 **분석성공** — 질문 1 · 2 는 코드 줄로 닫고, 질문 3 은 클라이언트 형식 문자열 + 서버 (`sspisrv` · `lsasrv`) 코드까지 읽어 닫음. NDR 바이트 꼴은 규칙으로 끌어낸 것이라 동적 확인 몫. `[공백]` 넷 (연결 인자 6 SD 의 최종 내용 · `GlobalDataInitialize +0x1076` 앞 건너뛰기 갈래 · 형식 3 · 8 · 12 의 서버 결과 · bind 마스크 1 / 2 중 진짜 서버의 선택).
> - 이어 쓰는 앞 run: **run129** (rpcrt4 `AlpcConnect` 의 Ex 길과 상태 표 — `0xc0000034` → `0x6ba` 1722), **run133** (bind · 요청 · 응답 머리, 동기 길 `Flags 0x20000`, 재시도 "첫 시도 + 다시 3"), **run99** (rpcrt4 첫 초기화가 `\RPC Control` SD 를 빌려 기본 SD 를 만드는 줄).
> - 남은 동적 → **Q254 (동적: `GetUserNameW` · `InternetOpenW` 한 번씩 — 포트 · bind 마스크 · 프로시저 0 · 15 바이트)**, 남은 정적 → **Q255 (정적: 결과서 다음 요청 후보 1 ~ 4 — `LsapGetNameForLogonSessionHelper` 형식별 · 프로시저 3 꾸러미 묻기 · `LookupAccount*` · `SHGetKnownFolderPath` / `gethostbyname` RPC)**.

## 요약

- **창구 하나** `[정적]`: `GetUserName*` 도 `InternetOpen*` 도 결국 **`sspicli!GetUserNameExW` → `SspipGetUserName`** 을 지난다. RPC 를 부르는 건 **sspicli** 다 (advapi32 · sechost 는 안 낀다).
  - 바인딩 = `RpcStringBindingComposeW(NULL, "ncalrpc", NULL, "lsasspirpc", "")` → `RpcBindingFromStringBindingW` 뿐 (`SecpGetRpcBinding +0x3f` · `+0x5b`). 인증 · QOS 설정 호출이 없다.
  - rpcrt4 가 끝점 앞에 `\RPC Control\` 를 붙이므로 ALPC 포트 이름은 **`\RPC Control\lsasspirpc`** `[정적 + 규칙]` (최종 문자열은 안 봄 → Q254).
  - 끝점이 문자열에 들어 있어서 **epmapper 를 안 탄다** (run129 의 winnsi 와 다른 점) `[추측: 끝점 있는 바인딩에서 BindToEpMapper 를 안 탄다는 줄은 안 읽음]`.
- **요청서 · 원문 짐작 바로잡기** `[정적]`:
  - "lsarpc 인가 SSPI 전용 창구인가?" → **SSPI 전용 (`sspirpc`, 끝점 `lsasspirpc`)**. `advapi32!LsaGetUserName` (lsarpc 쪽) 은 별개 함수라 이 길에 없다.
  - "`InternetOpenW` 가 보안 꾸러미를 묻는다 (`EnumerateSecurityPackages` 류) `[추측]`" → **틀림.** `GlobalDataInitialize +0x1076 InitializeGlobalUserName` → `_InitOnceGlobalUserName +0x58 GetUserNameExA(2, …)` 다. 꾸러미 함수들 (`AcquireCredentialsHandleA` 등) 은 TLS 손잡기 쪽 (`SecurityPkgInitialize +0x23e`) 에서만 부른다.
- **프로시저 둘** `[정적]`: 프로세스에서 처음이면 `IsOkayToExec` → `IsSPMgrReady` (이벤트 `\SECURITY\LSA_AUTHENTICATION_INITIALIZED` 열고 묻기) → `InitState` → `CreateRpcConnection` → **프로시저 0** (연결, 컨텍스트 핸들 20 B 받음). 그 뒤로는 `IsOkayToExec +0xf` (`DllState & 0x40000000`) 에서 끝나고 **프로시저 15 만** 간다.
- **연결 인자 6 (서버 보안 요구)** `[정적]`: sspicli 가 주는 게 아니라 rpcrt4 전역 **`gDefaultSecurityDescriptor`** (`AlpcConnect +0x3f5`). 에뮬레이터 로그의 `NtOpenDirectoryObject \RPC Control` · `NtQuerySecurityObject 0x17` ×2 는 run99 의 그 초기화 (`RpcpNormalizeSecurityDescriptor +0x12a` · `+0x158` · `+0x19c`) 다 — 이 길의 sspicli 줄이 아니다.
- **실패 값** `[정적]`: 연결 `0xc0000034` ×4 → rpcrt4 `0x6ba` (1722) → `NdrClientCall3` 이 예외 → sspicli 처리기 `I_RpcMapWin32Status` = `0xc0020017` → `GetUserNameExW` 가 `RtlNtStatusToDosError` = **1722** 를 `SetLastError` 하고 FALSE → `GetUserNameW` 0. `InternetOpenW` 는 `GlobalDataInitialize` 가 그 값을 돌려 **NULL + 1722**, 그리고 성공 표시 (`GlobalDataInitialized = 1`) 를 못 세워 **부를 때마다 다시 시도** 한다.
- **서버 최소 답** `[정적 + 규칙]`: bind (UUID · 버전, 마스크 하나) → 프로시저 0 응답 32 B (깃발 · 꾸러미 수 · 컨텍스트 핸들 20 B · 상태 0) → 프로시저 15 응답 `28 + Length` B (`RPC_UNICODE_STRING` + 오류칸 + 상태). 이름 모양은 **형식에 `0x10000` 비트가 있으면 `user`, 없으면 `DOMAIN\user`** (lsasrv `SspiExGetUserName +0x174` · `+0x17f`).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔셋째 바퀴. plan **Q249**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 다섯** (한 번에 하나, 직전마다 `free -g` available 27 ~ 28 G): advapi32 (29초) · sspicli (16초) · wininet (54초) · sspisrv (10초) · lsasrv (2분 41초). secur32 는 analyze 안 함 (PE 내보내기 표만 — 아래 1-1). rpcrt4 는 다시 analyze 안 하고 `^이름$` export 만 2번 (`I_RpcMapWin32Status` · `NdrClientCall3` · `NdrpClientCall3`). 같은 시간 다른 decompiler 는 sast/132 (ntdll 쪽) — 잠금 충돌 없음 (결과서).

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| advapi32 | `advapi32.dll` 778,344 B | **`43EC8DCD3F9CB692FE88343843C328201`** | 1,905 |
| sspicli | `sspicli.dll` 307,328 B | **`3EDD48FFBFD3AE1FDDE522AB7C3F659D1`** | 561 |
| wininet | `wininet.dll` 2,716,272 B | **`409B26F3321EC0CD492ADBAE0EDF24F31`** | 6,546 (`FUN_` 0) |
| sspisrv | `sspisrv.dll` 61,440 B | **`AD966CDE72001D529BEE7D19DAEEE3B21`** | 76 |
| lsasrv | `lsasrv.dll` 1,880,064 B | **`5F41AB2E82AE7286452DFA4ACC4CD6201`** | 4,408 |
| rpcrt4 | `rpcrt4.dll` | `0204A8A137C42B7DF1F0811A3A8277B21` (기준 줄과 같음) | — |

- 다섯 모두 `llvm-pdbutil dump --summary` GUID 가 위 앞 32 글자와 같다. 맨 끝 숫자는 심볼 서버 키의 Age (바이너리 1), PDB 안 Age 는 2 ~ 3 (run128 · run129 · run133 과 같은 모양). `ms/ghidra/<모듈>.analyze.log` 다섯 장에 `SetPdb:` 줄 있음 — 함수 이름이 PDB 이름 (`GetUserNameW` · `IsOkayToExec` · `InitState` …) 으로 잡힘.
- secur32 PDB (`4086D48D454CB5A55D7006FDFF298D3C1`) 는 받아만 두고 analyze 안 함.
- 공개 PDB 라 구조체 타입 이름이 없다. 칸 이름은 코드 줄에서 읽은 것.
- 버전 대조 (결과서): "알려진 사실" 표에 이 모듈들 줄이 없어 `.asm` `+0x` 를 원본 바이트 (`pefile`) 와 36곳 맞춤 — **일치**.
- 별칭: `pdb_aliases.py --lookup` 로 sspicli `0xaa24` (`InitState`) · `0x98b0` (`SspipGetUserName`), lsasrv `0x2a2e0` (`SspiExGetUserName`) · `0x2b070` (`LsapGetNameForLogonSession`), sspisrv `0x12c0` (`SspirGetUserName`) · `0x1b50` (`SspirConnectRpc`) — **모두 별칭 무리 아님**. `_calls.tsv` 의 `alias:X+1` (예: `IsOkayToExec +0x54 → alias:InitState+1`) 은 같은 함수의 C++ 장식 이름 하나 (`.asm` `ALIASES(1)`) 라 PDB 이름을 그대로 썼다 (run129 · run133 과 같은 모양).

## 목적

- PE_emulator 에서 `GetUserName*` 이 0, `InternetOpen*` 이 NULL 로 끝나는 샘플이 많다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님). 두 길 다 `NtAlpcConnectPortEx` 를 넷씩 받고 (에뮬레이터 대답 `0xc0000034`) 실패한다. **포트 이름은 그쪽 로그에 안 찍혀** 어느 창구인지부터 몰랐다.
- 요청서 `_workspace/sast/133_lsa-sspi.md` 의 물음:
  1. `advapi32!GetUserNameW` 길 — 사슬 (DLL 경계 포함), 바인딩 (포트 이름 · UUID · 버전 · 프로시저 · 동기 / 비동기), 연결 인자 6 의 출처, `\RPC Control` SD 묻기가 어느 줄인지, 실패가 어떤 값을 거쳐 0 이 되나 · 물러서는 길.
  2. `wininet!InternetOpenW` → LSA — LSA 준비 이벤트 · 연결 사슬 (꾸러미 묻기인가), 실패하면 왜 NULL 인가, 1 과 같은 인터페이스인가.
  3. 한 번 성공시키는 서버 최소 답 — bind 뒤 프로시저 차례, 클라이언트가 응답에서 읽는 칸 (NDR 꼴).

## 방법

- 정적 sast/133:
  - 먼저 import · export 로 길을 가림: advapi32 의 `GetUserNameExW` · `GetUserNameExA` 는 **`SspiCli.dll` 지연 가져오기** (IAT `0x1800b8188` · `0x1800b8180`). wininet 의 `GetUserNameExA` 도 `SspiCli.dll` 지연 가져오기 (IAT rva `0x28f3a8`). secur32 의 `GetUserNameExW/A` · `EnumerateSecurityPackages*` · `QuerySecurityPackageInfo*` · `AcquireCredentialsHandle*` 는 모두 **`SSPICLI.` 로 가는 포워더** — secur32 는 사슬에 안 낀다 (`pefile`, `[정적: PE]`).
  - advapi32 depth 0: `GetUserNameW` · `GetUserNameA` · `LsaGetUserName` · `LsarGetUserName` · `LsapCreateBindingHandleForLocal` (마지막 셋은 "이 길이 아님" 확인용).
  - sspicli: `GetUserNameExW` · `GetUserNameExA` · `SspipGetUserName` (+ 예외 필터 `SspipGetUserName$filt$0`) · `IsOkayToExec` · `IsSPMgrReady` · `InitState` · `CreateRpcConnection` · `SecpGetRpcBinding` · `DisconnectRpcConnection` · `SecpSetSession` · `LsaConnectUntrusted` · 꾸러미 쪽 (`EnumerateSecurityPackagesW` · `QuerySecurityPackageInfoW` · `SecLocatePackageExW` · `SecpLoadLsaPackages` · `CallSPM` · `CallRpcSPM` 등 — 다음 요청 후보 2 용).
  - wininet: `InternetOpenW` · `InternetOpenA` · `InternalInternetOpen` · `GlobalDataInitialize` · `InitializeGlobalUserName` · `_InitOnceGlobalUserName` · `SecurityPkgInitialize` · `WxGetLastError` 등 + 지도 `_map/InternetOpenW_*` (기준 20, 59 함수).
  - sspisrv (서버 껍데기, lsass 가 올림): `SspiSrvInitialize` · `SspirConnectRpc` · `SspirGetUserName` · `SspirCallRpc` · `LsapMakeLowBoxRpcSD` 등.
  - lsasrv (서버 몸통, 질문 3 만): `SspiExConnectRpc` · `SspiExGetUserName` · `LsapGetNameForLogonSession` · `LsapGetNameForLogonSessionHelper` · `LsapDuplicateString` · `CheckAppcontainerUserNameAccess` 등 + refs 2장 (`_refs/SspiExConnectRpc.tsv` · `SspiExGetUserName.tsv`).
  - rpcrt4: export 만 (`I_RpcMapWin32Status` · `NdrClientCall3` · `NdrpClientCall3`). 있던 `AlpcConnect` · `PerformRpcInitialization` · `InitializeDefaultSecurityDescriptor` · `RpcpNormalizeSecurityDescriptor` 는 읽기만.
  - 일회용 읽기 (`pefile`, 파일로 안 남김): sspicli 끝점 문자열 (rva `0x32928` `ncalrpc` · `0x32938` `lsasspirpc` · `0x32950` `""`), 클라이언트 인터페이스 (`0x26bb0`, 길이 `0x60`), 프록시 정보 `sspirpc_ProxyInfo` (`0x25000`), 문법 표 (`0x25590`, 2줄), NDR 2.0 프로시저 형식 (`0x32ae2`, 오프셋 표 `0x34d40`), 타입 형식 (`0x34d72`); sspisrv 인터페이스 (`0x75a0`) · 디스패치 표 (`0x6c20`, 18개) · 루틴 표 (`0x6010`) · 비동기 짝 인터페이스 (`0x7f70`); ntdll 상태 → 오류 표 (`RtlpRunTable 0x195b50` · `RtlpStatusTable 0x193a10`), rpcrt4 `StatusMap` (`0xec000`, 108칸).
- 결과서: `_workspace/sast/133_lsa-sspi_result.md` (**분석성공**, 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` call · ret 짝 — advapi32 `GetUserNameW` · `GetUserNameA` · `LsaGetUserName` · `LsapCreateBindingHandleForLocal`; sspicli `GetUserNameExA` · `GetUserNameExW` · `SspipGetUserName` · `IsOkayToExec` · `IsSPMgrReady` · `InitState` · `CreateRpcConnection` · `SecpGetRpcBinding` · `CallRpcSPM`; wininet `InternetOpenW` · `InternetOpenA` · `InternalInternetOpen` · `GlobalDataInitialize` · `InitializeGlobalUserName` · `_InitOnceGlobalUserName` · `SecurityPkgInitialize`; sspisrv `SspiSrvInitialize` · `SspirConnectRpc` · `SspirGetUserName`; lsasrv `SspiExGetUserName`; rpcrt4 `PerformRpcInitialization` · `InitializeDefaultSecurityDescriptor` · `RpcpNormalizeSecurityDescriptor` · `AlpcConnect` · `NdrClientCall3` · `NdrpClientCall3`.
  - `.asm` 줄: 결과서 1-1 · 1-3 · 1-4 · 1-5 · 2-1 · 3-1 · 3-2 · 3-3 의 오프셋 거의 전부 (아래 "flow-writer 대조" 에 어긋난 곳만), `AlpcConnect +0x222 ~ +0x29a` · `+0x3a9 ~ +0x401`, `SspiExGetUserName +0x174 ~ +0x4d5` 전부, 지도 `wininet/_map/InternetOpenW_{edges,nodes,summary}`.

## 결과

비유 (run129 · run133 의 전화 이야기 이어서):

- `GetUserName` 은 "나 누구예요?" 를 lsass 에 전화로 묻는 일이다. 전화번호 (`lsasspirpc`) 는 수첩에 이미 적혀 있어서 114 안내 (epmapper) 를 안 거친다 `[추측: 비유]`.
- 처음 한 번은 "저 이 동네 새로 왔어요" 하고 회원 카드 (컨텍스트 핸들) 를 받는다 (프로시저 0). 그다음부턴 카드만 내밀고 "제 이름이요" 만 묻는다 (프로시저 15) `[추측: 비유]`.
- wininet 은 인터넷을 처음 열 때 "지금 쓰는 사람 이름" 을 수첩에 한 번 적어 두려고 똑같은 전화를 건다. 그 전화가 안 되면 인터넷 열기 자체를 포기한다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{advapi32,sspicli,wininet,sspisrv,lsasrv,rpcrt4}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `advapi32!GetUserNameW` (`0x30360`, 35 B) | `+0xf` | `+0x16` | [IAT `0x1800b8188`] `sspicli!GetUserNameExW` — 형식 `0x10002` (`+0xa`) | [정적] |
| `advapi32!GetUserNameA` | `+0xf` | `+0x16` | [IAT `0x1800b8180`] `sspicli!GetUserNameExA` — 형식 `0x10002` (`+0xa`) | [정적] |
| `sspicli!GetUserNameExA` (`0x12fd0`) | `+0x44` · `+0x89` · `+0xd9` | `+0x4b` · `+0x8e` · `+0xe0` | `RtlAllocateHeap` · **`GetUserNameExW`** · `RtlUnicodeStringToAnsiString` | [정적] |
| `sspicli!GetUserNameExW` (`0x9770`) | **`+0x52`** | **`+0x57`** | **`SspipGetUserName`** | [정적] |
| 〃 | `+0x75` · `+0x83` | `+0x7c` · `+0x8a` | `RtlNtStatusToDosError` · `SetLastError` (실패 갈래) | [정적] |
| `sspicli!SspipGetUserName` (`0x98b0`) | `+0x57` | `+0x5c` | `IsOkayToExec` | [정적] |
| 〃 | `+0xa7` | `+0xae` | `NtQueryInformationThread(-2, 0, 0x30 B)` — 현재 스레드 `CLIENT_ID` | [정적] |
| 〃 | **`+0x1a7`** | **`+0x1ae`** | **`NdrClientCall3(sspirpc_ProxyInfo, 15, …)`** — 이름 묻기 | [정적] |
| 〃 | `+0x1f6` · `+0x162` | `+0x1fd` · `+0x169` | `RtlCopyUnicodeString` · `LocalFree` (받은 글자 버퍼) | [정적] |
| `sspicli!SspipGetUserName$filt$0` (`0x23180`, 예외 필터) | `+0xe` | `+0x15` | `I_RpcExceptionFilter` | [정적] |
| `sspicli!IsOkayToExec` (`0x9b10`) | `+0x4b` · `+0x54` | `+0x50` · `+0x59` | `IsSPMgrReady` · `InitState` | [정적] |
| 〃 | `+0xa2` · `+0xc8` · `+0x116` | `+0xa9` · `+0xcf` · `+0x11d` | `NtOpenThreadToken` · `NtSetInformationThread` ×2 (가장 토큰 잠깐 내렸다 복구) | [정적] |
| `sspicli!IsSPMgrReady` (`0xa90c`) | `+0x31` | `+0x38` | `RtlInitUnicodeString(\SECURITY\LSA_AUTHENTICATION_INITIALIZED)` | [정적] |
| 〃 | **`+0x6d`** · **`+0x95`** · `+0xa7` | **`+0x74`** · **`+0x9c`** · `+0xae` | **`NtOpenEvent`** (접근 1, 속성 0x40) · **`NtQueryEvent`** (클래스 0, 8 B) · `NtClose` | [정적] |
| `sspicli!InitState` (`0xaa24`) | **`+0x65`** | **`+0x6a`** | **`CreateRpcConnection(NULL, 2, …)`** | [정적] |
| 〃 | `+0x9e` | `+0xa5` | `LocalAlloc(0x40, 0x30)` — `SecDllClient` | [정적] |
| 〃 | `+0xd6` · `+0x101` | `+0xdb` · `+0x108` | `SecpSetSession` · `NdrClientCall3` 프로시저 16 (깃발 `0x10000` 일 때만) | [정적] |
| 〃 | `+0x1cb` | `+0x1d2` | `LoadLibraryExW("sspicli.dll", NULL, 0x800)` — 자기 고정 | [정적] |
| 〃 | `+0x207` · `+0x218` | `+0x20c` · `+0x21f` | `DisconnectRpcConnection` · `LocalFree` (실패 정리) | [정적] |
| `sspicli!CreateRpcConnection` (`0xb7fc`) | `+0x29` | `+0x2e` | `SecpGetRpcBinding` | [정적] |
| 〃 | **`+0x6d`** | **`+0x74`** | **`NdrClientCall3(sspirpc_ProxyInfo, 0, …)`** — 연결 | [정적] |
| 〃 | `+0xa0` · `+0xb1` | `+0xa7` · `+0xb8` | `RpcBindingUnbind` · `RpcBindingFree` (연결 뒤 바인딩 버림) | [정적] |
| `sspicli!SecpGetRpcBinding` (`0xb8dc`) | **`+0x3f`** · `+0x5b` · `+0x6e` | **`+0x46`** · `+0x62` · `+0x75` | **`RpcStringBindingComposeW("ncalrpc", "lsasspirpc")`** · `RpcBindingFromStringBindingW` · `RpcStringFreeW` | [정적] |
| 〃 | `+0x96` | `+0x9d` | `I_RpcMapWin32Status` (오류) | [정적] |
| `sspicli!CallRpcSPM` (`0x9db0`, 이번 길 아님) | `+0xda` | `+0xe1` | `NdrClientCall3(sspirpc_ProxyInfo, RSI+3, …)` — 꾸러미 쪽 (Q255 ②) | [정적] |
| `advapi32!LsaGetUserName` (이번 길 아님) | `+0x5a` | `+0x61` | `NdrClientCall3` (lsarpc 쪽 — `LsapCreateBindingHandleForLocal +0x64 RpcBindingCreateW`) | [정적] |
| `wininet!InternetOpenW` (`0xcaa40`) | `+0x17d` | `+0x182` | `InternalInternetOpen` | [정적] |
| `wininet!InternetOpenA` (`0xca760`) | `+0x4f` | `+0x54` | `InternalInternetOpen` | [정적] |
| `wininet!InternalInternetOpen` (`0xcb39c`) | **`+0x395`** | **`+0x39a`** | **`GlobalDataInitialize`** (`GlobalDataInitialized == 0` 일 때만, `+0x8c` · `+0x93`) | [정적] |
| 〃 | `+0x34e` | `+0x355` | `SetLastError` (실패 갈래) | [정적] |
| `wininet!GlobalDataInitialize` (`0x16be0`, 12,561 B, caller 46) | **`+0x1076`** | **`+0x107b`** | **`InitializeGlobalUserName`** | [정적] |
| `wininet!InitializeGlobalUserName` (`0x1200cc`) | `+0x25` · `+0x5f` | `+0x2c` · `+0x64` | `InitOnceExecuteOnce(_InitOnceGlobalUserName)` · `WxGetLastError` | [정적] |
| `wininet!_InitOnceGlobalUserName` (`0x111520`) | **`+0x58`** | **`+0x5f`** | **[IAT `0x18028f3a8`] `SspiCli!GetUserNameExA(2, 256 B, &0x100)`** | [정적] |
| 〃 | `+0x76` · `+0x160` · `+0x1de` | `+0x7d` · `+0x167` · `+0x1e3` | `StrChrA` (`\` 찾기) · `SetLastError` · `WxGetLastError` | [정적] |
| `wininet!SecurityPkgInitialize` (이번 길 아님) | `+0x23e` | `+0x245` | `AcquireCredentialsHandleA` (TLS 손잡기 쪽) | [정적] |
| `rpcrt4!PerformRpcInitialization` | `+0x2b3` | `+0x2b8` | `InitializeDefaultSecurityDescriptor` (run99) | [정적] |
| `rpcrt4!InitializeDefaultSecurityDescriptor` | `+0x13c` | `+0x141` | `RpcpNormalizeSecurityDescriptor` (run99) | [정적] |
| `rpcrt4!RpcpNormalizeSecurityDescriptor` | `+0x12a` · `+0x158` · `+0x19c` | `+0x131` · `+0x15f` · `+0x1a3` | `NtOpenDirectoryObject(\RPC Control)` · `NtQuerySecurityObject(0x17)` ×2 (run99) | [정적] |
| `rpcrt4!LRPC_CASSOCIATION::AlpcConnect` | **`+0x3b9`** | **`+0x3c0`** | **`NtAlpcConnectPortEx`** — 인자 6 = `gDefaultSecurityDescriptor` (run129) | [정적] |
| `rpcrt4!NdrClientCall3` | `+0xe8` | `+0xed` | `NdrpClientCall3` | [정적] |
| `rpcrt4!NdrpClientCall3` | `+0x81e` | `+0x823` | `I_RpcNegotiateTransferSyntax` (첫 호출이 bind 를 일으킴) | [정적] |
| `sspisrv!SspiSrvInitialize` (서버) | `+0x3b` | `+0x40` | `LsapMakeLowBoxRpcSD` | [정적] |
| 〃 | **`+0x85`** | **`+0x8c`** | **`RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)`** | [정적] |
| 〃 | `+0x115` · `+0x1d3` | `+0x11c` · `+0x1da` | `RpcServerRegisterIf3` (본 인터페이스 · 비동기 짝 — 기능 플래그 `+0x169`) | [정적] |
| `sspisrv!SspirConnectRpc` (프로시저 0) | `+0x48` | `+0x4d` | 간접 `[gLsapSspiExtension+0x18]` → `lsasrv!SspiExConnectRpc` `[추측: 아래 대조]` | [정적] |
| `sspisrv!SspirGetUserName` (프로시저 15) | `+0x62` | `+0x67` | 간접 `[gLsapSspiExtension+0x78]` → `lsasrv!SspiExGetUserName` `[추측: 아래 대조]` | [정적] |
| `lsasrv!SspiExGetUserName` (`0x2a2e0`, 1,241 B) | `+0xbb` · `+0xd2` · `+0xec` | `+0xc0` · `+0xd7` · `+0xf1` | `SspipBuildCallInfo` · `LsapGetClientInfoEx` · `CheckAppcontainerUserNameAccess` | [정적] |
| 〃 | `+0x122` · **`+0x14f`** | `+0x127` · **`+0x154`** | `LsapLocateLogonSession` · **`LsapGetNameForLogonSession`** | [정적] |
| 〃 | `+0x28c` · `+0x2a5` · **`+0x19f`** | `+0x293` · `+0x2ac` · **`+0x1a4`** | `wcschr('\')` · `RtlInitUnicodeString` · **`LsapDuplicateString`** (출력 이름) | [정적] |

### 1. `GetUserNameW` 길 (질문 1) [정적]

#### 1-1. 사슬

DLL 경계: **advapi32 → sspicli (RPC) → rpcrt4 (이름만) → lsass (`sspisrv` → `lsasrv`)**.

1. `advapi32!GetUserNameW` — 35 B 감싸개. 인자를 한 칸씩 밀고 **형식 `0x10002`** (하위 16비트 2 = `NameSamCompatible`, 비트 16 은 "도메인 없이" 표시 — 3-3) 를 넣어 `GetUserNameExW` 를 부르고, 결과 바이트를 `MOVZX` 로 BOOL 로 (`+0xa` · `+0xf` · `+0x1b`). LastError 는 안 건드린다.
   - `GetUserNameA` 는 같은 꼴로 `GetUserNameExA` 를 부른다. `GetUserNameExA` 는 `cch × 2` 바이트 와이드 버퍼를 힙에 잡고 (`+0x44`, 실패면 `SetLastError(8)` `+0x5b`) `GetUserNameExW` → 와이드를 ANSI 로 (`+0xd9`).
2. `sspicli!GetUserNameExW` — 사용자 버퍼를 `UNICODE_STRING {0, 2×cch (최대 0xfffe), Buffer}` 로 감싸 `SspipGetUserName` (`+0x52`). 성공이면 `*크기` 에 글자 수 (형식에 `0x10000` 비트가 있으면 **+1, 널 포함** — `+0xac BT EBP,0x10`) 를 쓰고 널을 붙인다.
3. `SspipGetUserName` — ① `IsOkayToExec` (`+0x57`) ② `NtQueryInformationThread(-2, 0)` 로 PID · TID (`+0xa7`) ③ lsass 안이면 (`SecpLsaInprocDispatch` 있으면, `+0xd6`) 직접, 아니면 **`NdrClientCall3(…, 15, …)`** (`+0x1a7`) ④ 받은 이름을 사용자 버퍼로 복사 (`+0x1f6`, 모자라면 `0x80000005`) ⑤ 받은 글자 버퍼 `LocalFree` (`+0x162`).
4. `IsOkayToExec` — **이미 연결됐으면** (`DllState & 0x40000000`, `+0xf`) 전역 `SecDllClient` 만 넘기고 끝. 아니면 `IsSPMgrReady` (`+0x4b`) → `InitState` (`+0x54`). 앞 단계가 `0xc00000a5` / `0xc0000022` 면 스레드의 가장 토큰을 잠깐 내리고 (`NtSetInformationThread(-2, 5, 0)`) 다시 해 본 뒤 복구 (`+0x80` · `+0x87` · `+0xa2` · `+0xc8` · `+0x116`).
5. `IsSPMgrReady` — `\SECURITY\LSA_AUTHENTICATION_INITIALIZED` 를 `NtOpenEvent(접근 1, OBJ_CASE_INSENSITIVE)` (`+0x58` · `+0x5d` · `+0x6d`) → `NtQueryEvent(클래스 0)` (`+0x95`) → `NtClose`. **`EventState ≠ 0` 이면 0, 아니면 `0xc0000001`** (`+0xb7 ~ +0xc5`). 열기가 실패하면 그 상태 그대로 (`+0x7d JS`).
6. `InitState` — `CreateRpcConnection(NULL, 2, &컨텍스트, &SecLsaPackageCount, &깃발)` (`+0x63` · `+0x5e` · `+0x65`) → 성공이면 `SecDllClient` 를 만들어 `[+0x18] = 컨텍스트` (`+0x9e` · `+0xc8`) → 깃발에 `0x10000` 이 있을 때만 `SecpSetSession` + 프로시저 16 (`+0xcc` · `+0xf6` · `+0x101`) → `SecLsaMode = 깃발` (`+0x13b`) → 자기 고정 `LoadLibraryExW` (`+0x1cb`) → `DllState |= 0x40000000` (`+0x1d7`).
7. `CreateRpcConnection` — `SecpGetRpcBinding` (`+0x29`) → **`NdrClientCall3(…, 0, …)`** (`+0x6d`) → `RpcBindingUnbind` · `RpcBindingFree` (`+0xa0` · `+0xb1`). 이 바인딩은 연결 때만 쓰고 버린다 — 뒤 호출은 컨텍스트 핸들이 바인딩을 들고 간다 (형식 `FC_BIND_CONTEXT`, 3-2).
8. `SecpGetRpcBinding` — `RpcStringBindingComposeW` (`+0x3f`, 인자 `+0x1a` · `+0x25` · `+0x2c`) + `RpcBindingFromStringBindingW` (`+0x5b`) + `RpcStringFreeW` (`+0x6e`).
9. rpcrt4 (이름만): 첫 RPC 면 `PerformRpcInitialization` (run99), 첫 호출의 문법 협상 = bind (`NdrpClientCall3 +0x81e I_RpcNegotiateTransferSyntax`), 연결 = `AlpcConnect +0x3b9 NtAlpcConnectPortEx` (run129).

- `DllState` · `SecDllClient` 는 프로세스 전역이다 → **연결 (이벤트 열기 + 프로시저 0) 은 프로세스당 한 번**, 그 뒤 `GetUserName*` 은 프로시저 15 만.
- **이 길이 아닌 것**: `advapi32!LsaGetUserName` 은 `LsapCreateBindingHandleForLocal` (`RpcBindingCreateW` · `RpcBindingBind`) 로 가는 별개 함수다 (lsarpc 쪽, 결과서). secur32 는 포워더뿐 (`[정적: PE]`).

#### 1-2. 바인딩

| 항목 | 값 | 근거 |
|---|---|---|
| 끝점 문자열 | 프로토콜 `ncalrpc`, 끝점 **`lsasspirpc`**, 옵션 `""`, 객체 UUID · 주소 NULL | `SecpGetRpcBinding +0x1a` · `+0x25` · `+0x2c` · `+0x3f`; 문자열 `[정적: PE]` sspicli rva `0x32928` · `0x32938` · `0x32950` |
| ALPC 포트 이름 | **`\RPC Control\lsasspirpc`** | run129 (rpcrt4 가 끝점 앞에 `\RPC Control\` 를 붙임) + 위 문자열. `[정적 + 규칙]` — 최종 `UNICODE_STRING` 은 안 봄 (Q254 ①) |
| epmapper 풀이 | **안 탐** — 끝점이 문자열에 있고, 인터페이스 기본 끝점 0개 (`RpcProtseqEndpointCount 0`) | `[추측: 끝점 있는 바인딩에서 BindToEpMapper 를 안 탄다는 줄은 안 읽음]`. 요청 쪽 로그에 `\RPC Control\epmapper` 연결이 없는 것과 맞음 (에뮬레이터 로그) |
| 인터페이스 | **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0** | `[정적: PE]` sspicli rva `0x26bb0` (`RPC_CLIENT_INTERFACE`, 길이 `0x60`); 서버 sspisrv rva `0x75a0` 에도 같은 UUID |
| 전송 문법 | 표에 NDR 2.0 (`8a885d04-…` v2.0) · NDR64 (`71710533-…` v1.0) 두 줄 → bind 제안 마스크는 **3** 일 것 `[추측: run133 "표의 문법마다 OR" 규칙]` | `[정적: PE]` `sspirpc_ProxyInfo +0x20 nCount = 2`, `pSyntaxInfo` rva `0x25590` |
| 인터페이스 깃발 | `0x6000000` — 비동기 비트 `0x2000000` 도 켜져 있지만 프로시저 형식에 비동기 핸들이 없어 **동기 호출** | `[정적: PE]` rva `0x26bb0 +0x58`, 옵션 깃발 `0x46` · `0x45` |
| 동기 / 비동기 | **동기** — `NdrClientCall3` (비동기 짝 `NdrAsyncClientCall` 아님) | `SspipGetUserName +0x1a7`, `CreateRpcConnection +0x6d`. rpcrt4 안에서 bind 가 동기 갈래 (`Flags 0x20000` · 같은 버퍼) 로 가는 줄은 run133 1-1 과 같은 등급 `[추측]` |
| 프로시저 | **0 = 연결**, **15 = 이름 묻기** (이 길), 16 = 세션 (깃발 `0x10000` 때만) | 위 호출 지점 표 |
| 연결 인자 6 (서버 보안 요구) | **`gDefaultSecurityDescriptor`** (rpcrt4 전역 rva `0x103e60`). 서버 SID (`[this+0xc0]`) 는 없음 — sspicli 는 `RpcBindingSetAuthInfo*` · `RpcBindingSetOption` 을 안 부른다 (`_calls.tsv` 에 없음) | `AlpcConnect +0x24c` · `+0x24f` (SID 없으면) → `+0x3f2` · `+0x3f5 CMOVZ` · `+0x3fd` → `+0x27a TEST RDX` · `+0x27d JNZ` → `+0x3b9 NtAlpcConnectPortEx` |
| 서버 인증 요구 | 없음 — 서버 `RpcServerRegisterIf3` 의 보안 콜백 **NULL** (nsisvc 의 "수준 ≥ 6" 과 다름, run133) | `SspiSrvInitialize +0xf3 AND [RSP+0x30],0` · `+0x115` |

#### 1-3. `\RPC Control` SD 묻기 (`NtQuerySecurityObject 0x17`) 는 어느 줄인가

- **sspicli 줄이 아니다.** 프로세스에서 첫 RPC 호출 (`SecpGetRpcBinding +0x3f RpcStringBindingComposeW`) 때 rpcrt4 가 한 번 하는 초기화다 — run99 그대로: `PerformRpcInitialization +0x2b3` → `InitializeDefaultSecurityDescriptor +0x13c` → `RpcpNormalizeSecurityDescriptor`:
  - `+0x12a NtOpenDirectoryObject(\RPC Control, 0x20001)` — 이름은 `\RPC Control\` 에서 끝 `\` 를 뺀 것 (`+0x105` · `+0x111`).
  - `+0x158 NtQuerySecurityObject(…, 0x17, NULL, 0, &크기)` → **`0xc0000023` 이어야** (`+0x164`) → 버퍼 잡고 `+0x19c` 다시 (`+0x1aa JS` 실패).
  - 그 뒤 토큰 · `RtlNewSecurityObjectEx` · `RpcpCopySecurityDescriptor` (`+0x224`) → 전역 `gDefaultSecurityDescriptor`.
  - 실패하면 오류 14 (`+0x2ed MOV EBX,0xe`) → `PerformRpcInitialization +0x2b8` 에서 RPC 초기화 자체가 실패.
- 이 초기화는 프로세스당 한 번 (`RpcHasBeenInitialized`, run99). 그 뒤 **모든 RPC 연결이 이 SD 를 인자 6 으로** 쓴다 (서버 SID 를 안 준 바인딩이면).

#### 1-4. 실패가 0 이 되는 값

`GetUserNameExW +0x5b JNS` 뒤: 상태 < 0 이면 `+0x5d` (`0x80000005` → 크기만 알려 줌) · `+0x68` (`0xc0000001` + 서버 오류칸 ≠ 0 → 그 값) · 그 밖 `RtlNtStatusToDosError` (`+0x75`) → `SetLastError` (`+0x83`) → 반환 = 상태 ≥ 0 (`+0x94`) = **0**. LastError 숫자는 결과서가 ntdll 표 · rpcrt4 `StatusMap` 을 `pefile` 로 직접 계산한 것 `[정적: PE]`.

| 어디서 | sspicli 가 보는 상태 | LastError | 근거 |
|---|---|---|---|
| ALPC 연결 `0xc0000034` ×4 → rpcrt4 `0x6ba` | RPC 예외 1722 → `I_RpcMapWin32Status` = **`0xc0020017`** | **1722** | run129 표 + `StatusMap[1722]`; `CreateRpcConnection` 처리기 → `InitState` → `IsOkayToExec` → `SspipGetUserName +0x60 JS` → `GetUserNameExW +0x75` |
| ALPC 연결 `0xc0000041` | 예외 14 → `0xc0000017` | 8 | run129 표 (`0xc0000041 → 0xe`) |
| ALPC 연결 `0xc0000022` / `0xc00000a5` | 예외 5 → `0xc0000022` | 5 | 〃 |
| bind `0x6c2` / `0x6c0` | `0xc002001f` / `0xc002001d` | 1730 / 1728 | run133 1-3 |
| 보내기 `0xc0000008` | `0x6bf` → `0xc002001c` | 1727 | run133 2-5 |
| 이벤트 못 엶 (`0xc0000034`) | 그대로 | 2 | `IsSPMgrReady +0x7d JS` → `IsOkayToExec +0x52 JS` · `+0x8c JNZ` |
| 이벤트 열었지만 `EventState == 0` | `0xc0000001` | 31 | `IsSPMgrReady +0xb7 ~ +0xc5`, `GetUserNameExW +0x68` · `+0x6d` → `+0x118` (오류칸 0) → `+0x124 JMP +0x73` |
| 서버 `0xc0000022` + 오류칸 5 (앱컨테이너 거절) | 그대로 | 5 | `SspiExGetUserName +0x2f1` · `+0x2f9` |
| 서버 `0xc000005f` (로그온 세션 없음) | 그대로 | 1312 | `SspiExGetUserName +0x4af` |
| 버퍼 모자람 | `0x80000005` (비트 없음) / `0xc0000023` (비트 있음) | 234 / **122** | `GetUserNameExW +0xe5` · `+0x129 ~ +0x131` |

- **물러서는 길 없음**: `GetUserNameW` · `GetUserNameExW` · `SspipGetUserName` 어디에도 토큰 · 레지스트리 · 환경 변수에서 이름을 찾는 호출이 없다 (`_calls.tsv` + `.asm`). "다시 해 보기" 는 둘뿐 — `IsOkayToExec +0x80` 의 가장 수준 내리기, rpcrt4 연결 재시도 (첫 시도 + 다시 3, run133).

#### 1-5. 서버 쪽 (lsass) — `sspisrv.dll` → `lsasrv.dll`

- 포트를 여는 곳: `sspisrv!SspiSrvInitialize +0x85 RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)` → `+0x115 RpcServerRegisterIf3(인터페이스, …, 깃발 0x21, 최대 호출 0x4d2, 최대 크기 0x100000, 콜백 NULL, SD)`. SD 는 `LsapMakeLowBoxRpcSD` (`+0x3b`). 문자열 `[정적: PE]` sspisrv rva `0x8850` · `0x8868`.
  - 기능 플래그 `Feature_Schannel_KsecddAsyncRpc` (`+0x169`) 가 켜지면 비동기 짝 인터페이스 `8923ae44-b00f-4d17-aa8f-57a3a46efb0f` v1.0 (4개) 도 올린다 (`+0x1d3`) — 이 두 길과 무관.
- 서버 프로시저 표 (`[정적: PE]` 디스패치 표 rva `0x6c20` 18개 + 루틴 표 `0x6010` 주소 ↔ `_index.tsv`):
  - 0 `SspirConnectRpc` · 1 · 2 `SspirDisconnectRpcAsync` · 3 `SspirCallRpc` · 4 `SspirAcquireCredentialsHandle` · 5 `SspirFreeCredentialsHandle` · 6 `SspirProcessSecurityContext` · 7 `SspirDeleteSecurityContext` · 8 `SspirSslQueryCredentialsAttributes` · 9 `SspirNegQueryContextAttributes` · 10 `SspirSslSetCredentialsAttributes` · 11 `SspirApplyControlToken` · 12 `SspirLogonUser` · 13 `SspirLogonSystemManagedAdmin` · 14 `SspirLookupAccountSid` · **15 `SspirGetUserName`** · 16 `SspirGetInprocDispatchTable` · 17 `SspirSslSetCredentialsAttributesEx`.
- 프로시저 0: `SspirConnectRpc +0x1b MOV RAX,[RAX+0x18]` → `+0x48` 간접 → `lsasrv!SspiExConnectRpc` (`LsapSspiExtensionFunctions+0x18` 에 그 함수 주소, `lsasrv/_refs/SspiExConnectRpc.tsv`).
- 프로시저 15: `SspirGetUserName +0x13 CMP [RAX+0x78],0` (없으면 `+0x1a` `0xc00000bb`) → `+0x62` 간접 → `lsasrv!SspiExGetUserName` (`LsapSspiExtensionFunctions+0x78`). 인자 포인터 셋 중 하나라도 NULL 이면 `0xc000000d` (`+0x6d`).

### 2. `InternetOpenW` → LSA (질문 2) [정적]

1. `InternetOpenW` — 문자열을 ANSI 로 바꾼 뒤 `InternalInternetOpen` (`+0x17d`), 그 결과를 그대로 반환 (`+0x1ce`). `InternetOpenA` 도 같은 함수 (`+0x4f`).
2. `InternalInternetOpen` — 처음에 `R13 = 0` (`+0x50`). 전역 `GlobalDataInitialized` 가 0 이면 (`+0x8c` · `+0x93`) **`GlobalDataInitialize`** (`+0x395`). 결과가 0 이 아니면 **`+0x39e JNZ +0x435`** → 추적 한 줄 → `+0x34e SetLastError(ESI)` → `+0x35a MOV RAX,R13` = **NULL**.
3. `GlobalDataInitialize` (12,561 B — 전역 설정 · 레지스트리 · `WSAStartup` · ETW …, caller 46곳) 한가운데에서 **`InitializeGlobalUserName`** (`+0x1076`). 결과를 `[0x18027baa0]` 에 두고 (`+0x107b`) 0 이 아니면 **곧바로 끝 (`+0x1089 JNZ +0x20bd`)** 으로 가서 그 값을 반환 (`+0x212e` · `+0x21b5`). **`GlobalDataInitialized = 1` 은 맨 끝 성공 자리 `+0x3103` 에서만** 쓴다.
4. `InitializeGlobalUserName` — `InitOnceExecuteOnce(&once, _InitOnceGlobalUserName)` (`+0x25`) — 한 번 성공하면 다시 안 부른다. 콜백이 FALSE 면 `WxGetLastError` 값을 반환 (`+0x33` · `+0x5f`).
5. `_InitOnceGlobalUserName` — **`GetUserNameExA(2, 버퍼 256 B, &0x100)`** (`+0x51` · `+0x58`, 형식 2 = `NameSamCompatible`) → `\` 가 있으면 그 뒤만 남김 (`+0x76 StrChrA`) → 소문자 사본까지 전역 `g_pszCurrentUser` · `g_pszCurrentUserLowerCase` · `g_cchCurrentUserLowerCase` 에 (`+0x136` · `+0x13c` · `+0x143`). 실패면 `WxGetLastError` (`+0x1de`) → `SetLastError` (`+0x160`) → FALSE.
6. `SspiCli!GetUserNameExA` → `GetUserNameExW(2)` → 1-1 의 3 번부터 **완전히 같음** (형식 칸만 2).

- **꾸러미 묻기인가?** → **아니다.** `_calls.tsv` 에서 wininet 이 꾸러미 함수를 부르는 곳은 `SecurityPkgInitialize +0x23e AcquireCredentialsHandleA` 하나 (TLS 손잡기 쪽, 결과서는 `SecureHandshake_Fsm` · `SecureNegotiate_Fsm` 에서 온다고 적음). `InternetOpenW` 에서 LSA 이벤트 · 연결이 나는 까닭은 `GetUserNameExA` 다. **요청서 · 원문의 `EnumerateSecurityPackages` / `QuerySecurityPackageInfo` / `AcquireCredentialsHandle` 류 `[추측]` 은 틀렸다.**
- **1 과 같은 창구인가?** → 같다 (`lsasspirpc`, 같은 인터페이스, 프로시저 0 → 15). 다른 점: ① 형식 2 (advapi32 는 `0x10002`) ② 이름을 서버가 아니라 wininet 이 `\` 뒤만 자른다.
- **왜 NULL 인가**: `GlobalDataInitialize +0x1089 JNZ` → `InternalInternetOpen +0x39e JNZ +0x435` → `+0x34e SetLastError` → NULL. wininet 은 "현재 사용자 이름을 못 얻으면 전역 초기화 실패" 로 본다. LastError 는 1-4 표 그대로 (연결 실패면 **1722**).
- **실패하면 다음에 또 시도** — 성공 표시가 안 서서 `InternetOpen*` 마다 다시 `GlobalDataInitialize` 로 간다. 단 `InitOnceExecuteOnce` 가 실패를 기억하는지는 안 따졌다 — 결과서는 "다시 시도" 로 적었고 요청 쪽 로그의 "되풀이" 와 맞다 `[추측: InitOnce 실패 뒤 재실행 규칙은 kernelbase 몫, 안 읽음]`.
- **진짜 Windows 에서 돌려줘야 하는 것**: 연결 · `GetUserNameExA(2)` 가 성공하면 핸들 (NULL 아님). 이름 값은 `g_pszCurrentUser` 에만 쓰이고 `InternetOpenW` 결과엔 안 실린다.

### 3. 서버 최소 답 (질문 3) [정적 + 규칙]

- 형식은 `sspirpc_ProxyInfo` (sspicli rva `0x25000`) 의 **NDR 2.0** 프로시저 형식 (rva `0x32ae2`, 프로시저 0 → 오프셋 0, 15 → 1168) · 타입 형식 (rva `0x34d72`) 에서 읽음 `[정적: PE]`. 바이트 꼴은 **형식 문자열 + NDR 2.0 규칙** (정렬 · 유일 포인터 참조 ID · 적합 가변 배열 머리) 으로 끌어낸 것 — rpcrt4 마샬러는 안 읽었다. 동적으로 바이트 확인 (Q254 ③).
- 오프셋은 **스텁 시작 기준** — 요청은 LRPC 메시지 `+0x68`, 응답은 `+0x40` (run133 2-1 · 2-3).
- **bind 마스크 1 (NDR 2.0) 가정**. 진짜 서버가 NDR64 (2) 를 고르면 꼴이 다르다. 에뮬레이터는 1 을 고르면 된다 `[추측: 클라이언트가 그 뒤 NDR 2.0 형식을 쓰는 줄 (I_RpcNegotiateTransferSyntax 결과) 은 안 읽음]`.

#### 3-1. 프로시저 0 — 연결 (프로세스당 한 번)

형식: 명시 바인딩 핸들, 스택 `0x38`, 옵션 `0x46`, 인자 `[in, unique, string] char*` · `[in] long` · `[out] long*` ×2 · `[out]` 컨텍스트 핸들 · 반환 `long`.

| 방향 | 스텁 오프셋 | 크기 | 뜻 | 값 | 근거 |
|---|---|---|---|---|---|
| 요청 | `+0x00` | u32 | 이름 (`char*`) 유일 포인터 참조 ID | **0** (늘 NULL) | `InitState +0x63 XOR ECX,ECX`, 형식 `12 08 22 5c` |
| 요청 | `+0x04` | u32 | 연결 종류 | **2** (`LsaConnectUntrusted` 는 0) | `InitState +0x5e MOV EDX,2`; `LsaConnectUntrusted +0xe5 XOR EDX,EDX` |
| (요청 8 B) | | | 형식의 클라이언트 버퍼 크기도 8 | | 형식 `+0x12 = 08 00` |
| 응답 | `+0x00` | u32 | 깃발 → `SecLsaMode` | **`0x10000` 은 피할 것** (켜지면 `SecpSetSession` + 프로시저 16). 진짜 서버는 일반 클라이언트에 `0x20000` | `InitState +0xcc` · `+0x13b`; `lsasrv SspiExConnectRpc` (`0` / `0x10000` / `\|= 0x20000`) |
| 응답 | `+0x04` | u32 | 꾸러미 수 → `SecLsaPackageCount` | 아무 작은 수 (이 길에선 안 씀) | `InitState +0x52`; `SspiExConnectRpc` (`SpmpCurrentPackageCount()`) |
| 응답 | `+0x08` | 20 | 컨텍스트 핸들 (속성 u32 + GUID 16) → `[SecDllClient+0x18]` | 속성 0 + **0 이 아닌 UUID** `[추측: 0 · 0 은 NULL 핸들로 막힐 것, MS-RPCE 규칙, rpcrt4 안 읽음]` | `InitState +0xc8`, 형식 `30 a0 00 00` |
| 응답 | `+0x1c` | u32 | 상태 | **0** (음수면 `InitState +0x6e JNS` 로 실패) | `InitState +0x6a ~ +0x6e` |
| (응답 32 B) | | | | | |

#### 3-2. 프로시저 15 — 이름 묻기 (`GetUserName*` · `InternetOpen*` 마다)

형식 (rva `0x32f72`): 컨텍스트 핸들 바인딩 (널 불가), 스택 `0x30`, 옵션 `0x45`, 인자 컨텍스트 핸들 · `[in] CLIENT_ID*` · `[in] long` 형식 · `[out] RPC_UNICODE_STRING*` · `[out] long*` 오류칸 · 반환 `long`.

| 방향 | 스텁 오프셋 | 크기 | 뜻 | 값 | 근거 |
|---|---|---|---|---|---|
| 요청 | `+0x00` | 20 | 컨텍스트 핸들 | 프로시저 0 이 준 그대로 | `SspipGetUserName +0x6b` · `+0x196` |
| 요청 | `+0x14` | 4 | 맞춤 (hyper 8 정렬) | 0 | NDR 규칙 |
| 요청 | `+0x18` · `+0x20` | 8 · 8 | `CLIENT_ID` PID · TID | 부른 스레드 것 | `SspipGetUserName +0xa7` · `+0xb9` · `+0xc6`, 형식 `15 07 10 00 0b 0b 5c 5b` |
| 요청 | `+0x28` | u32 | **형식** | **`0x10002`** (advapi32) / **2** (wininet · 공개 `GetUserNameExW(NameSamCompatible)`) | `GetUserNameW +0xa`, `_InitOnceGlobalUserName +0x51`, `SspipGetUserName +0x184` |
| (요청 `0x2c` B) | | | | | |
| 응답 | `+0x00` · `+0x02` | u16 · u16 | `Length` (널 빼고) · `MaximumLength` | 글자 × 2 · **`Length + 2`** (클라이언트는 `MaximumLength` 를 안 읽음) | 형식 `1a 03 10 00 …`; `lsasrv LsapDuplicateString` |
| 응답 | `+0x04` | u32 | `Buffer` 유일 포인터 참조 ID | 0 아님 | 형식 `12 00 ca ff` |
| 응답 | `+0x08` · `+0x0c` · `+0x10` | u32 ×3 | 적합 가변 배열 `MaxCount` · `Offset` · `ActualCount` | `MaximumLength / 2` · 0 · `Length / 2` (어긋나면 RPC 예외일 것 `[추측]`) | 타입 형식 `+0xb8` (`17 55 02 00` · `17 55 00 00`), 확장 `0a 43` |
| 응답 | `+0x14` | `Length` | 글자 (UTF-16LE, 널 없음) | 이름 (값은 사용자 결정) | NDR 규칙 |
| 응답 | 그 뒤 (4 정렬) | u32 | 오류칸 | **0** (상태 `0xc0000001` 일 때만 클라이언트가 읽음) | `SspipGetUserName +0x4a`, `GetUserNameExW +0x68 ~ +0x7c` |
| 응답 | 그 뒤 | u32 | 상태 | **0** | `SspipGetUserName +0x1b3` · `+0x108` · `+0x10a` |
| (응답 `28 + Length` B, + 맞춤 최대 2) | | | | | |

- 클라이언트가 응답에서 하는 일: 받은 `Length` > 사용자 버퍼면 `0x80000005` (`+0x1e7`), 아니면 `RtlCopyUnicodeString` (`+0x1f6`), 받은 글자 버퍼는 `LocalFree`.
- 컨텍스트 핸들이 바인딩을 들고 가므로 연결 뒤 **같은 ALPC 연결을 계속** 쓴다 — `NtAlpcConnectPortEx` 는 프로세스당 한 번.

#### 3-3. 서버가 이름을 고르는 규칙 — `lsasrv!SspiExGetUserName`

1. 출력을 비우고 `*오류칸 = 0` (`+0x82`).
2. `SspipBuildCallInfo` · `LsapGetClientInfoEx` · `CheckAppcontainerUserNameAccess` (`+0xbb` · `+0xd2` · `+0xec`) — 앱컨테이너가 이름을 못 읽으면 `*오류칸 = 5`, `0xc0000022` (`+0x2f1` · `+0x2f9`).
3. `LsapLocateLogonSession` (`+0x122`) — 없으면 `0xc000005f` (`+0x4af`).
4. `LsapGetNameForLogonSession(세션, 형식, …)` (`+0x14f`) — 도우미 `LsapGetNameForLogonSessionHelper` 는 하위 16비트만 형식으로 (`+0x33 MOVZX ESI,R8W`), `0x20000` 비트를 따로 본다 (`+0x5d BT R8D,0x11`). `0xc0000001` 이면 `GetLastError` 를 오류칸에 (`+0x460`).
5. 갈래:
   - **비트 16 (`0x10000`) 켜짐** (advapi32 `0x10002`): `\` 뒤만 (`\` 없으면 통째) → `LsapDuplicateString` (`+0x174` · `+0x179 JC +0x26f` → `+0x28c wcschr` · `+0x2a5` → `+0x19f`).
   - **비트 꺼짐, 하위 16비트 ≠ `0xc`** (형식 2): 이름 지도의 이름 **그대로 `DOMAIN\user`** (`+0x17f` · `+0x184` → `+0x19f`).
   - 하위 16비트 = `0xc`: 형식 2 이름을 한 번 더 받아 (`+0x24f`) `도메인\…` 조립 (`+0x310` · `+0x371` · `+0x426`, 메모리 부족 `0xc0000017` `+0x434`) — 안 따짐.
   - 특례: 시스템 로그온 세션 + 비트 16 → `LocalSystemNameMap` (`+0x203 ~ +0x228`).
- **에뮬레이터 답 규칙**: `형식 & 0x10000` → `user`, 아니면 `DOMAIN\user`. 값 자체는 정하지 않음 (사용자 결정, 수집 묶음 `reg-users`). 비어 있지 않게.
- 다른 형식 (3 · 8 · 12 …) 은 같은 프로시저 15 에 형식 칸만 바뀐다. 형식별로 실패하는지는 도우미 (3,638 B) 안이라 `[공백]` → Q255 ①.

#### 3-4. bind

- bind 메시지 `+0x34` = `4f32adc8-6052-4a04-8701-293ccf2096f0` (메모리 모양 `c8 ad 32 4f 52 60 04 4a 87 01 29 3c cf 20 96 f0`), `+0x44` = 1.0 (run133 1-2 칸 그대로). 답 `+0x48` 수락 마스크 **1** 권장, `+0x08` 0 아닌 서버 PID (run133 1-3).
- 모르는 프로시저 (`0` · `15` 밖) 는 fault — 값은 run133 에 없음 (Q246).

### 호출 관계

```
(PE_emulator 요청 — test.exe 초기화 밖. 위 +0x.. 는 ret 자리)

advapi32!GetUserNameW / GetUserNameA                          형식 0x10002
 └─ +0x16  [지연] sspicli!GetUserNameExW  (A: GetUserNameExA +0x8e → GetUserNameExW)
wininet!InternetOpenW +0x182 / InternetOpenA +0x54  InternalInternetOpen
 └─ +0x39a  GlobalDataInitialize        (GlobalDataInitialized == 0 일 때만; 실패면 +0x39e → NULL)
      └─ +0x107b  InitializeGlobalUserName
                  └─ +0x2c  InitOnceExecuteOnce ⇢ _InitOnceGlobalUserName
                              └─ +0x5f  [지연] sspicli!GetUserNameExA(2) → GetUserNameExW  형식 2

sspicli!GetUserNameExW
 └─ +0x57  SspipGetUserName
            ├─ +0x5c  IsOkayToExec                     (DllState & 0x40000000 이면 바로 끝)
            │          ├─ +0x50  IsSPMgrReady
            │          │          ├─ +0x74  NtOpenEvent(\SECURITY\LSA_AUTHENTICATION_INITIALIZED)
            │          │          ├─ +0x9c  NtQueryEvent   EventState 0 → 0xc0000001
            │          │          └─ +0xae  NtClose
            │          └─ +0x59  InitState
            │                     ├─ +0x6a  CreateRpcConnection
            │                     │          ├─ +0x2e  SecpGetRpcBinding
            │                     │          │          ├─ +0x46  RpcStringBindingComposeW("ncalrpc", "lsasspirpc")
            │                     │          │          │          ⇒ (첫 RPC) PerformRpcInitialization +0x2b8 → … +0x131 NtOpenDirectoryObject · +0x15f / +0x1a3 NtQuerySecurityObject   (run99)
            │                     │          │          └─ +0x62  RpcBindingFromStringBindingW
            │                     │          ├─ +0x74  NdrClientCall3  프로시저 0   ⇒ NdrpClientCall3 +0x823 (bind) · AlpcConnect +0x3c0 NtAlpcConnectPortEx (run129 · run133)
            │                     │          └─ +0xa7 / +0xb8  RpcBindingUnbind · RpcBindingFree
            │                     ├─ +0xa5  LocalAlloc → SecDllClient[+0x18] = 컨텍스트
            │                     ├─ (깃발 0x10000) +0xdb SecpSetSession · +0x108 NdrClientCall3 프로시저 16
            │                     └─ +0x1d2 LoadLibraryExW("sspicli.dll")
            ├─ +0xae  NtQueryInformationThread   CLIENT_ID
            ├─ +0x1ae NdrClientCall3  프로시저 15   (예외 → 필터 +0x15 I_RpcExceptionFilter · 처리기 I_RpcMapWin32Status)
            └─ +0x1fd RtlCopyUnicodeString

[lsass 쪽]
sspisrv!SspiSrvInitialize ├─ +0x8c  RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)
                          └─ +0x11c RpcServerRegisterIf3(4f32adc8-… v1.0, 콜백 NULL)
SspirConnectRpc  (0)  +0x4d ⇢ lsasrv!SspiExConnectRpc
SspirGetUserName (15) +0x67 ⇢ lsasrv!SspiExGetUserName
                                ├─ +0x127 LsapLocateLogonSession
                                ├─ +0x154 LsapGetNameForLogonSession
                                └─ +0x1a4 LsapDuplicateString   (0x10000 → \ 뒤만 · 아니면 DOMAIN\user)
```

- `⇢` 는 간접 · 함수 포인터 호출, `⇒` 는 rpcrt4 안을 건너뜀.

## 확인된 것

- `[정적]` `GetUserNameW` · `GetUserNameA` = 형식 `0x10002` 감싸개 → sspicli 지연 가져오기 (`+0xa` · `+0xf`).
- `[정적]` RPC 를 부르는 건 sspicli — `SspipGetUserName +0x1a7` (프로시저 15) · `CreateRpcConnection +0x6d` (프로시저 0) · `InitState +0x101` (프로시저 16), 셋 다 `NdrClientCall3` (동기 스텁).
- `[정적]` 바인딩 = `ncalrpc` + `lsasspirpc` (`SecpGetRpcBinding +0x1a ~ +0x3f`, 문자열 `[정적: PE]`). 인증 · 옵션 설정 호출 없음.
- `[정적: PE]` 인터페이스 `4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0 — 클라이언트 (sspicli `0x26bb0`) · 서버 (sspisrv `0x75a0`) 같음. 문법 표 2줄.
- `[정적]` 이벤트 `\SECURITY\LSA_AUTHENTICATION_INITIALIZED` 열기 · 묻기 (`IsSPMgrReady +0x6d` · `+0x95`), `EventState` 0 이면 연결 시도도 안 함.
- `[정적]` 연결은 프로세스당 한 번 (`IsOkayToExec +0xf` · `InitState +0x1d7`).
- `[정적]` 연결 인자 6 = rpcrt4 `gDefaultSecurityDescriptor` (`AlpcConnect +0x3f5`, 서버 SID 없을 때). `\RPC Control` SD 묻기는 run99 초기화 줄.
- `[정적]` 실패 → `I_RpcMapWin32Status` → `RtlNtStatusToDosError` → `SetLastError` → FALSE, 물러서는 호출 없음. 숫자 (1722 등) 는 `[정적: PE]` 표 계산.
- `[정적]` wininet: `InternalInternetOpen +0x395` → `GlobalDataInitialize +0x1076` → `InitializeGlobalUserName +0x25` → `_InitOnceGlobalUserName +0x58 GetUserNameExA(2)`. 실패면 `+0x1089` → `+0x39e` → NULL, 성공 표시는 `+0x3103` 에서만.
- `[정적]` 서버: `sspisrv!SspiSrvInitialize +0x85` 가 `lsasspirpc` 를 열고, 프로시저 0 · 15 는 `SspirConnectRpc` · `SspirGetUserName` → lsasrv 표 칸 `+0x18` · `+0x78`.
- `[정적]` 이름 규칙: 형식 비트 16 → `\` 뒤만, 아니면 그대로 (`SspiExGetUserName +0x174 ~ +0x19f`).
- `[정적 + 규칙]` 프로시저 0 요청 8 B · 응답 32 B, 프로시저 15 요청 `0x2c` B · 응답 `28 + Length` B.
- `[정적]` PDB 다섯 (advapi32 `43EC8DCD…201` · sspicli `3EDD48FF…9D1` · wininet `409B26F3…F31` · sspisrv `AD966CDE…B21` · lsasrv `5F41AB2E…201`).

### flow-writer 대조 (결과서와 다른 곳 · 보탬)

- **예외 처리기 줄은 `.asm` 에 없다** — 결과서의 `SspipGetUserName +0x1c4 MOV ECX,EAX / +0x1c6 CALL I_RpcMapWin32Status` 와 `CreateRpcConnection +0x87 · +0x89 · +0x97` 은 `.asm` · `_calls.tsv` 에 안 나온다 (`SspipGetUserName.asm` 은 `+0x1bf` 다음이 `+0x1e2`). Ghidra 가 `__except` 착지점을 코드로 안 따라가서다 (기준 "예외 착지점" 줄과 같은 사정). 결과서는 이 줄들을 **원본 바이트** (`8b c8 48 ff 15`) 로 확인했다고 적었다 — 뜻은 그대로 둔다.
  - (보탬) 필터 쪽은 `.asm` 이 있다: `SspipGetUserName$filt$0` (rva `0x23180`) `+0xe I_RpcExceptionFilter`. 1722 가 이 필터에 걸려 처리기로 가는 건 run129 의 "치명 14종만 바깥으로" 와 맞아 보인다 `[추측: 1722 는 그 14종이 아닐 것, 표 안 다시 봄]`. `CreateRpcConnection` 의 필터 파일은 뽑히지 않았다.
- **wininet 지도 범위** — 결과서는 "`InternetOpenW` 사슬 (지도, 깊이 12) 에 꾸러미 함수가 하나도 없다" 고 적었다. 그런데 남긴 지도 파일 (`_map/InternetOpenW_edges.tsv`, 기준 20 · 59 함수) 은 **`GlobalDataInitialize` (caller 46) 에서 멈춰** 그 아래 (`InitializeGlobalUserName` 포함) 를 담지 않는다 (`_nodes.tsv` `helper`). `GlobalDataInitialize` 의 **직접** 호출 (`_calls.tsv`) 에도 꾸러미 함수는 없다. 그보다 깊은 곳까지 없다는 건 남은 파일로는 못 본다 `[공백: 작음]`. 결론 (LSA 연결은 `GetUserNameExA` 에서) 은 `_InitOnceGlobalUserName +0x58` 로 따로 서 있어 그대로.
- **`SspirConnectRpc` · `SspirGetUserName` 의 간접 대상** — `_calls.tsv` 는 `indirect:cfg:?` · `indirect:cfg:[RAX+0x78]` 이다. 결과서는 "`gLsapSspiExtension[+0x18]` = `lsasrv!SspiExConnectRpc`" 로 이었는데, 근거는 lsasrv refs (`LsapSspiExtensionFunctions+0x18` · `+0x78` 에 그 함수 주소) 뿐이다. **sspisrv 의 `gLsapSspiExtension` 이 lsasrv 의 `LsapSspiExtensionFunctions` 를 가리키게 되는 줄은 안 봤다** — 그래서 그 이음은 `[추측: 칸 오프셋 · 이름이 맞아서]`.
- **(보탬) `SspiExGetUserName` 의 기능 플래그** — `+0x1c3` · `+0x26f` 에 `Feature_LsaGetUserNameExEmpty` 검사가 있다. 켜져 있으면 **이름이 빈 채 성공일 때 원격 측정 assert 만** 부르고 (`+0x2dc ~ +0x2e7` · `+0x44b ~ +0x456 MicrosoftTelemetryAssertTriggeredNoArgs`) 길은 같다. 결과서의 "이름이 비면 `0xc0000073`" 은 `SspiExGetUserName` 안이 아니라 `LsapGetNameForLogonSession +0xbd` · 도우미 (`+0x13d` · `+0x1f5` · `+0x53b` 등) 쪽 줄이다. "빈 이름 성공" 이 어느 갈래에서 나는지는 `[공백]` (Q255 ①). 에뮬레이터 권고 "비어 있지 않게" 는 그대로.
- **(보탬) `InitState` 의 다른 호출** — 결과서가 안 다룬 `+0x15e IsHvsiContainer` · `+0x1ae RtlInitializeGenericTableAvl` · `+0x207 DisconnectRpcConnection` 도 있다 (`_calls.tsv`). 연결 성공 뒤 · 실패 정리 쪽으로 보이나 갈래는 안 읽음.
- **(보탬) `AlpcConnect` 의 Ex 고르기 순서** — 결과서 1-2 의 `+0x3f2 ~ +0x3fd` 는 `+0x24c TEST R14,R14` (서버 SID `[RSI+0xc0]`) · `+0x24f JZ +0x3f2` 에서 들어가 `+0x401 JMP +0x255` 로 돌아온 뒤 `+0x27a` 에서 고른다. 즉 **서버 SID 가 없는 바인딩은 (SD 인자 → 없으면 `gDefaultSecurityDescriptor`) 가 NULL 이 아닌 한 늘 Ex**. 결과서와 같은 뜻.
- 3-1 의 "`InitState +0x48 ~ +0x5d`" 는 범위 끝이 명령 시작이 아니다 (`+0x5e MOV EDX,2` 앞까지라는 뜻으로 읽음).
- 그 밖 결과서 오프셋 (호출 지점 표 · 1-1 ~ 3-3 줄) 은 `.asm` 과 **모두 맞았다**.

## 정정

- **이전 run 문서에 틀린 곳은 없다** — 맨 위 정정 줄 없음.
- **요청서 · 원문 짐작 바로잡기** (run 문서가 아니라 `_workspace/` · PE_emulator 쪽이라 고치지 않고 여기 적음):
  - `_workspace/sast/133_lsa-sspi.md` 질문 2 · 원문 `309_internals-lsa-sspi.md` 의 "`InternetOpenW` → 보안 꾸러미 묻기 (`EnumerateSecurityPackages` / `QuerySecurityPackageInfo` / `AcquireCredentialsHandle` 류) `[추측]`" → **틀림.** 실제는 `GetUserNameExA(2)` (2절).
  - "MS-LSAD · MS-LSAT 의 `lsarpc` 인가?" → **아니다. `lsasspirpc` (SSPI 전용, 인터페이스 `4f32adc8-…`)** (1-2).
  - 원문 "`GetUserNameW` → sspicli `GetUserNameExW` 로 보임 `[추측]`" → **맞음** `[정적]` (`GetUserNameW +0xf`).
- **run133 의 `[추측]` 하나가 반쯤 올라감**: run133 요약 "동기 길 (`Flags 0x20000`, 같은 버퍼) 은 … LSA 같은 다른 클라이언트가 쓴다 `[추측]`" — LSA (sspicli) 쪽이 **동기 스텁 `NdrClientCall3`** 을 쓰는 건 이제 `[정적]` 이다. 다만 rpcrt4 안에서 그게 bind · 요청의 동기 갈래 (`0x20000` · 같은 버퍼) 로 이어지는 줄은 run133 1-1 과 같은 등급으로 `[추측]` 그대로 (Q254 ② 로 확인). run133 문서는 안 고치고 flow.md run133 절에 run135 표시만 붙였다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나·틀리나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

결과서가 읽은 파일 · 줄 (2026-11-23 시점): `ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp` `:180 ~ :195` · `:296 ~ :312`, `rpc/lrpc.cpp:26`, `rpc/lsarpc.cpp:120 ~ :135`, `sync.cpp:8 ~ :20`.

| 자리 | 판정 | 까닭 |
|---|---|---|
| `rpc/lsarpc.cpp` 창구 (`\RPC Control\LSARPC_ENDPOINT`, `12345778-1234-abcd-ef00-0123456789ab`) | **이 두 길의 창구가 아니다 — 틀림** | 두 길은 `\RPC Control\lsasspirpc` · `4f32adc8-…` v1.0. `lrpc.cpp:26` `window_definitions` 에 **새 창구 하나** (포트 이름 · UUID · 버전 · 프로시저 0 · 15) 가 필요. lsarpc 창구는 그대로 (`LsaOpenPolicy` 류 몫) |
| `nt_alpc_create_port.cpp:305 ~ :310` (`NtAlpcConnectPortEx`) | **틀림 — 막힘의 직접 원인** | ① `service_window_connect` 를 안 부름 (plain `:189` 만) ② `live_server_required = a[5]…a[9]` 가 rpcrt4 의 빈 8 B `a[9]` 때문에 늘 참 ③ `if (a[5]) return stopped(...)` (`:310`). rpcrt4 는 `gDefaultSecurityDescriptor` 가 있으면 늘 Ex 로 오므로 창구 연결을 Ex 에도 붙여야: 이름이 창구와 같으면 `a[5]` (SD) · `a[9]` (빈 속성) 는 값 안 보고 받아 핸들 쓰고 성공 |
| `sync.cpp:8 ~ :20` (`NtOpenEvent` · `NtQueryEvent`) | **맞음** | 이벤트 이름은 이미 있음. `NtQueryEvent(0)` 의 `EventState ≠ 0` 이어야 (0 이면 LastError 31, 연결 시도 안 함) — 요청 쪽 로그에서 연결이 이어지니 지금 맞게 나감 |
| `\RPC Control` 열기 · `NtQuerySecurityObject 0x17` ×2 | **맞음 · 실패시키면 안 됨** | 실패하면 rpcrt4 초기화가 오류 14. 첫 번째는 `0xc0000023` + 크기, 두 번째에 SD |
| "Ex 연결 넷씩" | **코드와 맞음** | `0xc0000034` → 첫 시도 + 다시 3 → `GetUserNameW` 0 / `InternetOpenW` NULL, LastError 1722. `InternetOpen*` 은 실패하면 부를 때마다 다시 |
| 최소 순서 (제안) | — | 연결 (위) → bind (UUID · 버전, 마스크 1) → 프로시저 0 응답 32 B (깃발 `0x20000` 권장 · 꾸러미 수 아무거나 · 컨텍스트 20 B · 0) → 프로시저 15 응답 (3-2, `형식 & 0x10000` 이면 `user` 아니면 `DOMAIN\user`). 서버 상태는 연결별 컨텍스트 UUID 하나면 충분. 두 번째부터는 프로시저 15 만 |

- 공통 몫 (run129 · run133) 은 그대로 쓴다: 연결 인자 · 상태 표, bind · 요청 · 응답 머리, 속성 규칙, 재시도. LSA 길의 다른 점은 **끝점이 고정 (`ept_map` 없음)** · **동기 스텁** 둘.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 대기열 **Q254 (동적: `GetUserNameW` · `InternetOpenW` 한 번씩 — 포트 · bind 마스크 · 프로시저 0 · 15 바이트)**. test.exe 가 아니라 `GetUserNameW` · `InternetOpenW` 를 한 번씩 부르는 작은 프로그램 — 고르기는 사용자 결정. 유저 멈춤점, 그 프로세스 조건, 모두 ret 자리):
1. **포트 이름 · Ex 인자** — `rpcrt4!LRPC_CASSOCIATION::AlpcConnect+0x3b9` (call 직전) `rdx` 의 이름 = `\RPC Control\lsasspirpc` 인지, `[rsp+0x28]` SD (`!sd`), Flags `0x20000`, `+0x3c0` `eax`. 프로세스당 한 번인지 (두 번째 `GetUserNameW` 엔 안 나오는지).
2. **차례** — `sspicli!SspipGetUserName+0x5c` · `+0x1ae`, `CreateRpcConnection+0x74`, `InitState+0x6a`, `IsSPMgrReady+0x9c` (`[rbp+0x1c]` = `EventState`). 콜스택 `GetUserNameW+0x16` → `GetUserNameExW+0x57` → `SspipGetUserName+0x1ae`.
3. **메시지 바이트** — bind 답 `+0x48` (진짜 서버가 1 을 고르나 2 를 고르나, run133 `Bind__4e330 +0x62d`), 프로시저 0 요청 `+0x68` 8 B · 응답 `+0x40` 32 B, 프로시저 15 요청 `0x2c` B · 응답 `28 + Length` B (`MaximumLength = Length + 2` 인지). 3 절 NDR 도출 확인.
4. **형식 2 이름의 진짜 모양** — `wininet!_InitOnceGlobalUserName+0x5f` (`rax`, `rsp+0x40` ANSI) — `DOMAIN` 이 컴퓨터 이름인지, 프로세스당 한 번인지.
5. **`*크기`** — `sspicli!GetUserNameExW+0xdc` 근처 `[rdi]` = 글자 수 + 1 인지.
6. **연결 실패 때 재시도** — `AlpcConnect+0x3b9` 4번 · 최종 1722 (서버를 못 닿게 할 수 있으면).
7. **`IsOkayToExec+0x80` 갈래** — 가장 수준이 낮은 스레드에서만 도는지 `[추측: 정상 샘플에선 안 돎]`.
- (보탬) 같은 에뮬레이터 로그에서 winnsi 의 epmapper 연결은 **plain** (run129), 이번 LSA 연결은 **Ex** 로 찍혔다 (요청 쪽 로그 — `[관찰]` 아님). 둘 다 서버 SID 없는 바인딩이라 코드로는 둘 다 Ex 여야 해서, run129 의 "plain 인 까닭 `[공백]`" 은 그대로다 — 위 1 과 같은 자리 (`AlpcConnect+0x27a` `rdx`, Q231 ④) 로 함께 볼 수 있다.

정적 (결과서 "다음 요청 후보" 1 ~ 4 — 대기열 **Q255 (정적: 결과서 다음 요청 후보 1 ~ 4 — `LsapGetNameForLogonSessionHelper` 형식별 · 프로시저 3 꾸러미 묻기 · `LookupAccount*` · `SHGetKnownFolderPath` / `gethostbyname` RPC)**):
1. `lsasrv!LsapGetNameForLogonSessionHelper` (3.6 KB) — 형식 3 · 8 · 12 별로 도메인에 안 붙은 PC 에서 무엇이 실패하나 (`GetUserNameExW` 30 샘플 중 29 가 0 인 이유 후보). 위 "빈 이름 성공" 갈래도.
2. `QuerySecurityPackageInfoW` · `EnumerateSecurityPackagesW` 길 — `SecLocatePackageExW` → `SecpLoadLsaPackages` → … → `CallSPM` → `CallRpcSPM +0xda` (프로시저 `RSI+3` — 3 = `SspirCallRpc` 로 보임 `[추측]`). 같은 창구 `lsasspirpc`, 프로시저 3 의 형식 · 응답 (패키지 표).
3. `LookupAccountSidW` · `LookupAccountNameW` — `advapi32!LsaICLookupSids` · `LsaICLookupNames` 가 lsarpc (`LsapCreateBindingHandleForLocal` · `RpcBindingCreateW`) 인지, 서버 프로시저 14 (`SspirLookupAccountSid`) 인지.
4. `SHGetKnownFolderPath` · `gethostbyname` 의 RPC 쪽 — 요청 쪽 로그의 "Ex 연결을 받은 샘플" 나머지가 같은 창구인지.

- 그 밖 `[공백]`: 연결 인자 6 SD 의 최종 내용과 커널이 그걸 어떻게 쓰는지, `GlobalDataInitialize +0x1076` 앞 건너뛰기 갈래, `GlobalDataInitialize` 깊은 곳의 꾸러미 호출 유무 (위 대조), `InitOnceExecuteOnce` 가 실패 뒤 콜백을 다시 부르는 규칙, `gLsapSspiExtension` ↔ `LsapSspiExtensionFunctions` 이음.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: 다른 decompiler 와 같은 스크래치 폴더의 같은 이름 임시 스크립트 (`rd.py`) 가 덮어쓰인 일이 있어 `scratchpad/s133/` 로 옮겨 썼다 — 기록만 (하네스 제안 아님).
- 지도 파일 (`_map/*_edges.tsv`) 은 기준 (caller ≥ 20) 에서 공용 도구로 멈춘 함수의 아래를 안 담는다. "지도에 없다" 를 근거로 쓸 땐 멈춘 함수 (`_nodes.tsv` `helper`) 를 같이 볼 것 — 기록만 (요약 `summary.txt` 에 이미 적혀 있어 하네스 제안 아님).
