# 133 lsa-sspi — 정적 분석 결과

- 요청서: `_workspace/sast/133_lsa-sspi.md` (Q249, PE_emulator `309_internals-lsa-sspi.md` — 원문 읽음). 정적만.
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`.
- 날짜: 2026-11-23. 새로 analyze 한 모듈 **다섯 개** (한 번에 하나씩, 직전마다 `free -g` = 전체 46 / available 27~28, 도는 Ghidra java 는 다른 decompiler 몫 0~2개 — ntoskrnl·ntdll·combase·win32u 프로젝트는 건드리지 않음):
  advapi32 (29초) · sspicli (16초) · wininet (54초) · sspisrv (10초) · lsasrv (2분 41초). `secur32` 는 analyze 안 함 (PE 내보내기 표만 봄, 아래 1-1).
- rpcrt4 는 **다시 analyze 하지 않고** `^이름$` export 만 했다 (`I_RpcMapWin32Status` · `NdrClientCall3` · `NdrpClientCall3`, depth 0, 두 번, 이미 있던 `AlpcConnect` · `PerformRpcInitialization` · `InitializeDefaultSecurityDescriptor` · `RpcpNormalizeSecurityDescriptor` 의 `.asm` 은 sast/127·130 에서 뽑은 것을 다시 읽음). 첫 export (`I_RpcMapWin32Status`) 때는 다른 decompiler 의 java 가 2개 돌고 있었으나 (`rpcrt4.lock` 없음, 잠금 충돌 없이 끝남 — sast/132 는 ntdll 쪽을 쓰는 중이었다), 둘째 (`NdrpClientCall3`) 때는 0개였다.
- 근거 표기: `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` (부르기는 call 자리, 괄호가 돌아올 자리 `ret`). 같은 이름 함수가 여럿인 곳은 없었다.
- PE 데이터 (UUID · 문자열 · NDR 형식 문자열) 는 `.rdata` 를 `pefile` 로 직접 읽은 것 → `[정적: PE]`.

## 스크립트 문제

없음. 참고 (고칠 것은 아님):
- 서로 다른 decompiler 가 같은 스크래치 폴더 (`…/scratchpad/`) 의 같은 이름 임시 스크립트 (`rd.py`) 를 덮어쓴 적이 있다 (하네스 문제 아님 — 이번엔 `scratchpad/s133/` 로 옮겨 씀).
- 간접 호출은 `.asm` 에서 `48 ff 15 …` 로 시작한다 (REX 접두사 `48` 포함). `+0x` 가 call 명령 첫 바이트다.

## PDB GUID 확인 (새로 analyze 한 모듈)

| 모듈 | 바이너리 | PDB guid+age (심볼 서버 키) | `llvm-pdbutil dump --summary` GUID | 함수 수 (`_index.tsv`) | 로그 |
|---|---|---|---|---|---|
| advapi32 | 778,344 B | `43EC8DCD3F9CB692FE88343843C328201` | `{43EC8DCD-3F9C-B692-FE88-343843C32820}` | 1,905 | `ms/ghidra/advapi32.analyze.log` — `SetPdb:` 줄 있음, `GetUserNameW` 등 PDB 이름으로 잡힘 |
| sspicli | 307,328 B | `3EDD48FFBFD3AE1FDDE522AB7C3F659D1` | `{3EDD48FF-BFD3-AE1F-DDE5-22AB7C3F659D}` | 561 | 〃 `SetPdb:` 있음 (`IsOkayToExec`, `InitState` 등) |
| wininet | 2,716,272 B | `409B26F3321EC0CD492ADBAE0EDF24F31` | `{409B26F3-321E-C0CD-492A-DBAE0EDF24F3}` | 6,546 (`FUN_` 0개) | 〃 `SetPdb:` 있음 |
| sspisrv | 61,440 B | `AD966CDE72001D529BEE7D19DAEEE3B21` | `{AD966CDE-7200-1D52-9BEE-7D19DAEEE3B2}` | 76 | 〃 `SetPdb:` 있음 |
| lsasrv | 1,880,064 B | `5F41AB2E82AE7286452DFA4ACC4CD6201` | `{5F41AB2E-82AE-7286-452D-FA4ACC4CD620}` | 4,408 | 〃 `SetPdb:` 있음 |

- 위 guid 앞 32 글자가 summary GUID 와 같다 (맨 끝 숫자 = 심볼 서버 키의 Age. PDB 안 Age 는 2~3, 바이너리 Age 1 — sast/126·127·130 과 같은 모양).
- 커널 GUID (`C29EBFB0…`) 는 ntoskrnl 만 해당이라 이번엔 해당 없음. 공개 PDB 라 구조체 타입 이름이 없다 (이름은 모두 코드 줄에서 읽은 것).
- 참고: `secur32.dll` PDB (`4086D48D454CB5A55D7006FDFF298D3C1`) 는 받아 놓았고 analyze 는 안 했다.

## 버전·스크립트 대조

"알려진 사실" 표에 이 모듈들 줄이 없다. 대신 `.asm` 의 `+0x` 를 **원본 바이트** (`pefile` 로 읽은 파일 바이트) 와 맞췄다 — 36곳 전부 일치:
- `advapi32 GetUserNameW +0xf` · `GetUserNameA +0xf` = `48 ff 15` (`CALL [IAT]`).
- `sspicli GetUserNameExW +0x52` = `e8` · `+0xac` = `0f ba e5 10` (`BT EBP,0x10`); `SspipGetUserName +0x57` = `e8` · `+0x1a7` = `48 ff 15` · `+0x1c4` = `8b c8 48 ff 15` (예외 처리기: `MOV ECX,EAX; CALL …`); `IsOkayToExec +0x4b/+0x54` = `e8`; `IsSPMgrReady +0x6d/+0x95` = `48 ff 15`; `InitState +0x65` = `e8`, `+0xf6` = `41 8d 50 10` (`LEA EDX,[R8+0x10]`); `CreateRpcConnection +0x29` = `e8`, `+0x6d` = `48 ff 15`, `+0x87` = `8b c8 48 ff 15`; `SecpGetRpcBinding +0x3f/+0x5b` = `48 ff 15`.
- `rpcrt4 PerformRpcInitialization +0x2b3` · `InitializeDefaultSecurityDescriptor +0x13c` = `e8`; `RpcpNormalizeSecurityDescriptor +0x12a/+0x158/+0x19c` = `48 ff 15`.
- `wininet InternalInternetOpen +0x395` = `e8`, `+0x39e` = `0f 85`; `GlobalDataInitialize +0x1076` = `e8`; `InitializeGlobalUserName +0x25`, `_InitOnceGlobalUserName +0x58` = `48 ff 15`; `InternetOpenA +0x4f` = `e8`.
- `sspisrv SspiSrvInitialize +0x85/+0x115` = `48 ff 15`; `SspirGetUserName +0x62` = `e8`.
- `lsasrv SspiExGetUserName +0x174` = `41 0f ba e7 10` (`BT R15D,0x10`), `+0x310` = `48 ff 15`; `LsapGetNameForLogonSessionHelper +0x33` = `41 0f b7 f0` (`MOVZX ESI,R8W`).
- **판정: 일치.** 스크립트 문제 없음.

---

## 먼저 읽는 한 줄 답

**가장 큰 한 줄: 두 길 (`GetUserName*` · `InternetOpen*`) 은 같은 창구다 — lsass 의 SSPI 전용 RPC (`sspirpc`), lsarpc (MS-LSAD·MS-LSAT) 가 아니다.** 포트 `\RPC Control\lsasspirpc` (ncalrpc), 인터페이스 **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0**, 동기 호출 (`NdrClientCall3`). 프로세스당 처음 한 번 **프로시저 0 (연결)**, 그다음 **프로시저 15 (사용자 이름 묻기)**. `InternetOpenW` 가 LSA 를 부르는 건 보안 꾸러미 묻기가 아니라 **wininet 이 "현재 사용자 이름" 을 한 번 캐시하려고 `GetUserNameExA(NameSamCompatible)` 을 부르기 때문**이다 (그래서 `EnumerateSecurityPackages` 류 `[추측]` 은 틀림).

**질문 1 (`GetUserNameW`)**
1. 사슬: `advapi32!GetUserNameW` (35바이트, 감싸개) → [delay import] `sspicli!GetUserNameExW(0x10002, 버퍼, &크기)` → `SspipGetUserName` → `IsOkayToExec` (→ `IsSPMgrReady` · `InitState` → `CreateRpcConnection` → `SecpGetRpcBinding`) → `NdrClientCall3(sspirpc_ProxyInfo, 프로시저 15)`. RPC 는 **sspicli** 가 부른다 (advapi32·sechost 는 안 낀다. `advapi32!LsaGetUserName` 은 lsarpc 로 가는 별개 함수라 이 길이 아님).
2. 바인딩: `RpcStringBindingComposeW(NULL, "ncalrpc", NULL, "lsasspirpc", "" , &문자열)` → `RpcBindingFromStringBindingW` 뿐 (인증·QOS 설정 호출 없음). 포트 이름 = rpcrt4 규칙으로 **`\RPC Control\lsasspirpc`**. 인터페이스 **`4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0**, 전송 문법 NDR 2.0 (+ NDR64 도 제안하는 표), 기본 끝점 0개, 동기.
3. 연결 인자 6 (서버 보안 요구 SD) 은 **sspicli 가 주는 게 아니다**. rpcrt4 가 프로세스에서 처음 RPC 를 쓸 때 한 번 만드는 전역 `gDefaultSecurityDescriptor` 다 — `\RPC Control` 열기 (`NtOpenDirectoryObject`) · SD 묻기 (`NtQuerySecurityObject 0x17` ×2) 가 바로 그 줄 (`rpcrt4 RpcpNormalizeSecurityDescriptor +0x12a / +0x158 / +0x19c`, ← `InitializeDefaultSecurityDescriptor +0x13c` ← `PerformRpcInitialization +0x2b3`). 이게 실패하면 rpcrt4 초기화가 오류 14 로 실패하니 (`+0x2ed MOV EBX,0xe`) **지금 에뮬레이터가 성공시키는 게 맞다**.
4. 실패: 연결 `0xc0000034` → rpcrt4 `0x6ba` (1722) ×4 (첫 시도 + 다시 3, sast/130) → `NdrClientCall3` 이 **예외**로 던짐 → sspicli 의 `__except` 처리기가 `I_RpcMapWin32Status(1722)` = **`0xc0020017`** 로 바꿈 → `GetUserNameExW` 가 `RtlNtStatusToDosError(0xc0020017)` = **1722** 를 `SetLastError` 하고 **FALSE** → `GetUserNameW` 0. **다른 길로 물러서는 코드 없음** (토큰에서 이름 찾기 없음).

**질문 2 (`InternetOpenW`)**
1. 사슬: `InternetOpenW +0x17d` → `InternalInternetOpen +0x395` (**`GlobalDataInitialized == 0` 일 때만**) → `GlobalDataInitialize +0x1076` → `InitializeGlobalUserName +0x25` (`InitOnceExecuteOnce`) → `_InitOnceGlobalUserName +0x58` → [delay import] `SspiCli!GetUserNameExA(2, 256바이트 버퍼, &256)` → `GetUserNameExW(2)` → 위 1 과 같은 길 (`IsSPMgrReady` 의 `NtOpenEvent \SECURITY\LSA_AUTHENTICATION_INITIALIZED` → `NtQueryEvent` → 연결 → 프로시저 0 → 프로시저 15). 같은 인터페이스, 다른 점은 **형식 칸 = 2** (advapi32 는 `0x10002`).
2. 실패하면: `GetUserNameExA` FALSE → 콜백이 `SetLastError` 후 FALSE → `InitOnceExecuteOnce` 실패 → `InitializeGlobalUserName` 이 그 오류값을 돌려줌 → `GlobalDataInitialize` 가 끝으로 가서 그 값을 반환 (`GlobalDataInitialized = 1` 은 맨 끝 성공 자리 `+0x3103` 에서만 씀) → **`InternalInternetOpen +0x39e JNZ`** → `SetLastError(그 값)`, 반환 R13 = **0** → `InternetOpenW` NULL, LastError **1722**. 실패하면 플래그가 안 서서 **다음 `InternetOpen*` 때마다 다시 시도한다** (로그의 "되풀이" 와 맞음).

**질문 3 (서버 최소 답)** — 연결용 프로시저가 따로 있다 (**프로시저 0**), 이름 묻기는 **프로시저 15**.
1. bind: 인터페이스 UUID·버전 그대로 (sast/130 1-2), 수락 문법 비트 하나 (**1 = NDR 2.0 을 권함** — 아래 표는 NDR 2.0 기준).
2. 프로시저 0 요청 스텁 8바이트 (`char*` 유일 포인터 참조 ID 0 + 연결 종류 `long` = **2**), 응답 스텁 **32바이트**: `[0] 깃발 u32` · `[4] 꾸러미 수 u32` · `[8] 컨텍스트 핸들 20바이트` · `[0x1c] 상태 u32 = 0`.
3. 프로시저 15 요청 스텁 **0x2c 바이트** (컨텍스트 핸들 20 + 4 맞춤 + `CLIENT_ID` 16 + 형식 `long`), 응답 스텁 **28 + 이름 바이트 수 (+ 4 맞춤)**: `RPC_UNICODE_STRING` (Length · MaximumLength = Length+2 · 참조 ID · MaxCount · Offset 0 · ActualCount · 글자) + `long` 오류값 + `long` 상태. 이름 값은 서버 (lsasrv `SspiExGetUserName`) 가 정한다: 형식 `0x10002` 면 **도메인 없이 `user` 만**, 형식 2 면 `DOMAIN\user`.
4. **클라이언트 코드 + sspisrv + lsasrv 로 닫혔다** — 서버 쪽 프로시저는 `sspisrv.dll` (lsass 가 올림) 에 있고 실제 일은 `lsasrv!SspiExConnectRpc` / `SspiExGetUserName` 이 한다. 서버 바이트 꼴 중 NDR 틀은 형식 문자열 + NDR 2.0 규칙으로 도출한 것이라 `[정적 + 규칙]` (동적 3 에서 바이트로 확인).

**지금 우리 (PE_emulator)**: 아래 "지금 우리 맞나·틀리나" 6줄. 한 줄 요약: **지금 `lsarpc` 창구는 이 두 길과 무관하고, `NtAlpcConnectPortEx` 줄 (`:305~:310`) 이 창구 검사를 안 거치고 `0xc0000034` 로 가는 것이 막힘의 직접 원인**이다. 그 앞 syscall (`NtOpenEvent`·`NtQueryEvent`·`\RPC Control` 열기·SD 묻기) 은 맞게 가고 있다.

---

## 1. `advapi32!GetUserNameW` 길 (질문 1)

### 1-1. 사슬 표

DLL 경계: advapi32 → **sspicli** (RPC) → rpcrt4 (이름만) → lsass (`sspisrv` → `lsasrv`). `advapi32` 의 `GetUserNameExW`·`GetUserNameExA` 는 **`SspiCli.dll` delay import** (IAT `0x1800b8188` · `0x1800b8180`, `pefile` 로 확인). `secur32.dll` 의 `GetUserNameExW` · `GetUserNameExA` · `EnumerateSecurityPackages*` · `QuerySecurityPackageInfo*` · `AcquireCredentialsHandle*` 는 모두 **`SSPICLI.` 로 가는 포워더**라 사슬에 안 낀다 (secur32 코드 안 열었음).

| 차례 | 함수 (모듈) | 하는 일 | 실패 때 | 줄 |
|---|---|---|---|---|
| 1 | `GetUserNameW` (advapi32, rva 0x30360) | 인자 (버퍼·크기 포인터) 를 한 칸씩 밀고 **형식 `0x10002`** 를 넣어 `GetUserNameExW` 부름, 결과 바이트를 BOOL 로 | 반환 0 (LastError 는 안 건드림 — 안쪽이 세팅) | `GetUserNameW.asm` `+0xa MOV ECX,0x10002`, `+0xf` CALL [IAT] (ret `+0x16`), `+0x1b MOVZX EAX,AL` |
| 1' | `GetUserNameA` (advapi32) | 같음, 부르는 쪽만 `sspicli!GetUserNameExA` | 같음 | `GetUserNameA.asm` `+0xa`, `+0xf` (ret `+0x16`) |
| 2 | `GetUserNameExA` (sspicli, rva 0x12fd0) | `cch*2` 바이트 와이드 버퍼를 `RtlAllocateHeap`, `GetUserNameExW` 부름, 와이드→ANSI | 할당 실패 `SetLastError(8)`; 와이드 실패면 안쪽 LastError 그대로 (`*크기` 는 2배) | `GetUserNameExA.asm` `+0x44` (ret `+0x4b`), `+0x5b SetLastError`, `+0x89` (ret `+0x8e`), `+0xd9 RtlUnicodeStringToAnsiString` |
| 3 | `GetUserNameExW` (sspicli, rva 0x9770) | 사용자 버퍼를 `UNICODE_STRING {Length 0, MaximumLength = 2×cch (최대 0xfffe), Buffer}` 로 감싸 `SspipGetUserName` 부름. 성공이면 `*크기` 에 글자 수 (형식에 `0x10000` 비트가 있으면 **+1**, 널 포함) 를 쓰고 널 붙임 | **상태 → LastError** (아래 1-4 표) 후 FALSE. 버퍼 모자람: 비트 있으면 `0xc0000023` (→ 122), 없으면 `0x80000005` (→ 234) | `+0x2a CMP EAX,0x7fff / JA +0xec`, `+0x52` (ret `+0x57`), `+0x5b JNS +0xac`, `+0x5d CMP EAX,0x80000005`, `+0x68 CMP EAX,0xc0000001`, `+0x75` (ret `+0x7c`), `+0x83 SetLastError` (ret `+0x8a`), `+0x94 SHR EBX,0x1f / XOR BL,1` (반환 = 상태 ≥ 0), `+0xac BT EBP,0x10`, `+0xe5 MOV EBX,0xc0000023` |
| 4 | `SspipGetUserName` (sspicli, rva 0x98b0) | ① `IsOkayToExec` ② `NtQueryInformationThread(-2, 0, 0x30바이트)` 로 현재 스레드의 `CLIENT_ID` (PID·TID) 얻기 ③ lsass 안 호출이면 (`SecpLsaInprocDispatch` 가 있으면) 직접 부름, 아니면 **`NdrClientCall3(sspirpc_ProxyInfo, 15, …)`** ④ 받은 이름을 사용자 `UNICODE_STRING` 에 `RtlCopyUnicodeString` ⑤ 받은 글자 버퍼 `LocalFree` | RPC 예외 → 처리기가 `I_RpcMapWin32Status(예외코드)` 를 상태로. 사용자 버퍼 `MaximumLength` < 받은 `Length` 면 `0x80000005` | `+0x57` (ret `+0x5c`), `+0xa7` (ret `+0xae`), `+0xd6 MOV RAX,[SecpLsaInprocDispatch]`, `+0x1a7` (ret `+0x1ae`), `+0x1e7 CMP [R14+2],AX`, `+0x1f6` (ret `+0x1fd`), `+0x162 LocalFree`, 처리기 `+0x1c4 MOV ECX,EAX / +0x1c6 CALL I_RpcMapWin32Status` |
| 5 | `IsOkayToExec` (sspicli, rva 0x9b10) | **이미 연결됐으면** (`DllState & 0x40000000`) 전역 `SecDllClient` 만 넘기고 끝. 아니면 `IsSPMgrReady` → `InitState`. 앞 단계가 `0xc00000a5`/`0xc0000022` (가장 수준·접근 거부) 면 **스레드의 가장 토큰을 잠깐 내렸다 (`NtSetInformationThread(-2, 5, 0)`) 다시 시도하고 복구** | 상태를 그대로 위로 (`0xc0000142` = `SecDllClient` 없음) | `+0xf TEST [DllState],0x40000000`, `+0x4b` (ret `+0x50`), `+0x54` (ret `+0x59`), `+0x80 CMP EAX,0xc00000a5`, `+0x87 CMP EAX,0xc0000022`, `+0xa2 NtOpenThreadToken`, `+0xc8`/`+0x116 NtSetInformationThread`, `+0xf6 MOV EAX,0xc0000142` |
| 6 | `IsSPMgrReady` (sspicli, rva 0xa90c) | 이벤트 **`\SECURITY\LSA_AUTHENTICATION_INITIALIZED`** (`.data` 의 `szLsaEvent`, rva 0x416a0) 를 `NtOpenEvent(접근 1, 속성 `OBJ_CASE_INSENSITIVE` 0x40)` → `NtQueryEvent(클래스 0, 8바이트)` → `NtClose`. **`EventState` 가 0 이 아니면 성공 (0)**, 0 이면 `0xc0000001` | 열기 실패면 그 상태 (예: `0xc0000034` → LastError 2) | `+0x31 RtlInitUnicodeString` (ret `+0x38`), `+0x58 MOV EDX,1`, `+0x5d MOV [RBP-0x18],0x40`, `+0x6d NtOpenEvent` (ret `+0x74`), `+0x95 NtQueryEvent` (ret `+0x9c`), `+0xa7 NtClose`, `+0xb7 ~ +0xc5` (`EventState` → 0 / `0xc0000001`) |
| 7 | `InitState` (sspicli, rva 0xaa24) | `csSecurity` 락 → `CreateRpcConnection(NULL, 2, &컨텍스트, &SecLsaPackageCount, &깃발)` → 성공이면 `LocalAlloc(0x40, 0x30)` 로 `SecDllClient` 만들고 `[+0x18] = 컨텍스트 핸들` → (깃발 `0x10000` 일 때만 `SecpSetSession` + **프로시저 16**, 보통 안 탐) → `SecLsaMode = 깃발` → `LoadLibraryExW("sspicli.dll", NULL, 0x800)` (자기 고정) → `DllState \|= 0x40000000` | 실패하면 `SecDllClient` 를 풀고 상태를 위로 | `+0x65` (ret `+0x6a`), `+0x9e LocalAlloc`, `+0xc8 MOV [RCX+0x18],RAX`, `+0xcc TEST [RSP+0x50],0x10000`, `+0xf6 LEA EDX,[R8+0x10]` / `+0x101 NdrClientCall3` (프로시저 16), `+0x13b MOV [SecLsaMode],EAX`, `+0x1cb LoadLibraryExW`, `+0x1d7 BTS [DllState],0x1e`, `+0x218 LocalFree` |
| 8 | `CreateRpcConnection` (sspicli, rva 0xb7fc) | `SecpGetRpcBinding` → **`NdrClientCall3(sspirpc_ProxyInfo, 프로시저 0, …)`** → `RpcBindingUnbind` · `RpcBindingFree` (이 바인딩은 연결 때만 쓰고 버림 — 뒤 호출은 컨텍스트 핸들이 바인딩을 들고 있음) | **RPC 예외 → 처리기 → `I_RpcMapWin32Status`** → 그 상태를 반환 | `+0x29` (ret `+0x2e`), `+0x6d` (ret `+0x74`), `+0xa0` (ret `+0xa7`), `+0xb1` (ret `+0xb8`), 처리기 `+0x87 MOV ECX,EAX / +0x89 CALL I_RpcMapWin32Status` |
| 9 | `SecpGetRpcBinding` (sspicli, rva 0xb8dc) | `RpcStringBindingComposeW` + `RpcBindingFromStringBindingW` + `RpcStringFreeW` | 오류를 `I_RpcMapWin32Status` | `+0x3f` (ret `+0x46`), `+0x5b` (ret `+0x62`), `+0x6e` (ret `+0x75`), `+0x96` (ret `+0x9d`) |
| 10 | rpcrt4 (이름만, sast/127·130) | 첫 RPC 호출 초기화 (`PerformRpcInitialization`) · bind · `AlpcConnect` (`NtAlpcConnectPortEx`) | 아래 1-4 | `NdrpClientCall3 +0x81e I_RpcNegotiateTransferSyntax` (첫 호출이 문법 협상 = bind 를 일으킴) |

- 같은 DLL 의 `SspiCli` 안 **`DllState` · `SecDllClient`** 는 프로세스 전역이다: **연결 (이벤트 열기 + 프로시저 0) 은 프로세스당 한 번**, 두 번째 `GetUserName*` 부터는 `IsOkayToExec +0xf` 에서 끝나 **프로시저 15 만** 간다.

### 1-2. 바인딩 표

| 항목 | 값 | 근거 |
|---|---|---|
| 포트 (끝점) 문자열 | 프로토콜 `ncalrpc`, 끝점 **`lsasspirpc`**, 옵션 `""`(빈 문자열), 객체 UUID·네트워크 주소 NULL | `SecpGetRpcBinding.asm` `+0x1a LEA R9,[0x180032938]`, `+0x25 LEA RDX,[0x180032928]`, `+0x2c LEA RAX,[0x180032950]`, `+0x3f`; 문자열은 `[정적: PE]` sspicli rva 0x32928 `ncalrpc` · 0x32938 `lsasspirpc` · 0x32950 `""` |
| 실제 ALPC 포트 이름 | **`\RPC Control\lsasspirpc`** (rpcrt4 가 끝점 앞에 `\RPC Control\` 를 붙임) | sast/127 2-1 (`AlpcConnect` 규칙) + 위 문자열. `[정적 + 규칙]` — 최종 `UNICODE_STRING` 은 안 봄 (동적 1) |
| 끝점 풀이 (epmapper) | **안 탐** — 끝점이 문자열에 들어 있고, 인터페이스에 기본 끝점도 없음 (`RpcProtseqEndpointCount` 0). 로그에 `\RPC Control\epmapper` 연결이 없는 것과 맞음 | sast/127 2-1 (끝점이 있으면 풀이 안 한다는 규칙) `[추측: BindToEpMapper 안 탐은 끝점 있는 바인딩에서 안 읽음]` |
| 인터페이스 UUID | **`4f32adc8-6052-4a04-8701-293ccf2096f0`** | `[정적: PE]` sspicli rva 0x26bb0 (`RPC_CLIENT_INTERFACE`, 길이 0x60, `InterfaceId`); 서버 `sspisrv` rva 0x75a0 에도 같은 UUID |
| 버전 | **1.0** | 〃 |
| 전송 문법 | 클라이언트 인터페이스 표에 NDR 2.0 (`8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0) 과 NDR64 (`71710533-beba-4937-8319-b5dbef9ccc36` v1.0) 두 줄 (`nCount 2`). bind 마스크는 **3** 일 것 (`[추측]` — sast/130 의 "표의 문법마다 OR" 규칙) | `[정적: PE]` `sspirpc_ProxyInfo` (rva 0x25000) `+0x20 nCount = 2`, `+0x28 pSyntaxInfo` (rva 0x25590, 항목 0x50 바이트) |
| 깃발 | 인터페이스 `Flags = 0x6000000` (비동기 비트 `0x2000000` 도 켜져 있으나, **프로시저 형식에 비동기 핸들이 없어** 동기 호출) | `[정적: PE]` rva 0x26bb0 `+0x58`, 프로시저 형식 (아래 3 절) |
| 프로시저 | **0 = 연결**, **15 = 사용자 이름 묻기** (이 길에서). 서버 표 순서는 1-5 | 1-1 표 7·8·4, 서버 표 1-5 |
| 동기 / 비동기 | **동기** (`NdrClientCall3`, 비동기 짝인 `NdrAsyncClientCall` 이 아님; 프로시저에 비동기 핸들·`HasAsyncHandle` 없음 — 옵션 깃발 `0x46` / `0x45` 에 해당 비트 없음). 그래서 `NtAlpcConnectPortEx` Flags = `0x20000` (sast/127 2-1), bind 도 동기 (같은 버퍼로 답, sast/130 1-1) | `[정적]` `SspipGetUserName +0x1a7`, `CreateRpcConnection +0x6d`; rpcrt4 내부 bind 동기 여부는 sast/130 1-1 의 `[추측]` 과 같은 등급 |
| 서버 보안 요구 (연결 인자 6) | **`gDefaultSecurityDescriptor`** (rpcrt4 전역, rva 0x103e60). 서버 SID (`[this+0xc0]`) 는 없음 — sspicli 가 `RpcBindingSetAuthInfo*` · `RpcBindingSetOption` 을 **안 부름** (`_calls.tsv` 에 없음) | `AlpcConnect.asm +0x3f2 TEST RDX,RDX`, `+0x3f5 CMOVZ RDX,[gDefaultSecurityDescriptor]`, `+0x3fd MOV [RBP-0x80],RDX`, `+0x27a TEST RDX,RDX / JNZ +0x3a9`, `+0x3b9 NtAlpcConnectPortEx` (ret `+0x3c0`) |
| 컨텍스트 핸들 | 프로시저 0 이 **돌려주는 20바이트** 가 이후 호출의 **첫 인자 + 바인딩 정보** (`FC_BIND_CONTEXT`). 연결 한 번에 ALPC 연결도 한 번 | 3 절 형식 문자열 |
| 인증 | 서버 쪽 `RpcServerRegisterIf3` 의 인터페이스 보안 콜백 **NULL** — 클라이언트에게 인증 수준을 요구하지 않음 (nsisvc 와 다름) | `sspisrv SspiSrvInitialize +0x115` 직전 `+0xf3 AND [RSP+0x30],0` |

### 1-3. `\RPC Control` SD 묻기 (`NtQuerySecurityObject 0x17`) 가 이 길의 어느 줄인가 (한 줄)

**sspicli 줄이 아니다 — 프로세스에서 첫 RPC 호출 (`SecpGetRpcBinding +0x3f RpcStringBindingComposeW`) 때 rpcrt4 가 한 번 하는 초기화:** `PerformRpcInitialization +0x2b3` (call, ret `+0x2b8`) → `InitializeDefaultSecurityDescriptor +0x13c` (ret `+0x141`) → `RpcpNormalizeSecurityDescriptor`:
- `+0x12a NtOpenDirectoryObject(\RPC Control, 접근 0x20001)` (ret `+0x131`) — 이름은 `\RPC Control\` 에서 끝 `\` 를 뺀 것 (`+0x105 MOV EAX,0xfffe / +0x111 ADD [..],AX`); 앱컨테이너면 앞에 앱컨테이너 이름 경로.
- `+0x158 NtQuerySecurityObject(핸들, 0x17, NULL, 0, &크기)` (ret `+0x15f`) → **`0xc0000023` 이어야** (`+0x164 CMP EAX,0xc0000023 / JNZ 실패`) → `AllocWrapper(크기)` → `+0x19c` 같은 호출을 다시 (ret `+0x1a3`).
- 그다음 `OpenThreadToken(현재 스레드, 8, TRUE)` (없으면 `ERROR_NO_TOKEN 0x3f0` → `OpenProcessToken(8)`) → `RtlNewSecurityObjectEx(\RPC Control 의 SD, 우리 SD, …)` → `RpcpCopySecurityDescriptor` → 전역 `gDefaultSecurityDescriptor` (`+0x1e0 ~ +0x224`).
- 우리 쪽 SD (`InitializeDefaultSecurityDescriptor.c`): DACL 하나, 허용 ACE (접근 `0x80000000` = GENERIC_READ 로 읽음) 한 줄, SID = `S-1-5-7` (ANONYMOUS LOGON, 하위 권한 1개 값 7). → 이 SD 가 **`\RPC Control` 의 부모 SD 와 합쳐져** 연결 인자 6 이 된다. 에뮬레이터는 **NULL 이 아닌 포인터**만 받으면 된다 (내용을 서버가 보는지는 커널 몫 `[공백]`).
- **이 초기화는 프로세스당 한 번**이다 (`PerformRpcInitialization +0` 맨 앞 `RpcHasBeenInitialized` 검사). 그 뒤 첫 RPC 뿐 아니라 **다른 모든 RPC 의 연결도 이 SD 를 인자 6 으로 쓴다** — 그래서 에뮬레이터 로그의 Ex 연결이 "늘 넷씩" 오는 것.

### 1-4. 실패가 `GetUserNameW` 0 이 되는 값 — 갈래 표

`GetUserNameExW +0x5b JNS` 뒤 갈래: 상태 < 0 이면 (`+0x5d` 0x80000005 → 크기 알려 주고, `+0x68` 0xc0000001 + 서버 오류칸 ≠ 0 → 그 값을 LastError, 그 밖 → `RtlNtStatusToDosError(상태)`) `SetLastError` 하고 반환 `(상태 ≥ 0)` = **0**. 아래 값은 `RtlNtStatusToDosError` 를 ntdll 표 (`RtlpRunTable` 0x195b50, `RtlpStatusTable` 0x193a10) 로 직접 되풀이해 계산한 것 `[정적: PE]` (`I_RpcMapWin32Status` 의 `StatusMap` = rpcrt4 rva 0xec000 108칸).

| 어디서 났나 | 상태 (sspicli 가 보는 값) | LastError | 근거 |
|---|---|---|---|
| ALPC 연결 `0xc0000034` ×4 → rpcrt4 `0x6ba` (1722) | RPC 예외 1722 → `I_RpcMapWin32Status` = `0xc0020017` | **1722** | sast/127 2-1 표 + `StatusMap[1722]`; `CreateRpcConnection +0x87 ~ +0x97` (처리기) → `InitState` → `IsOkayToExec` → `SspipGetUserName +0x60 JS +0x110` → `GetUserNameExW +0x75` |
| ALPC 연결 `0xc0000041` (거부) | 예외 14 → `0xc0000017` | 8 | sast/127 표 `0xc0000041 → 0xe` + `StatusMap[14]` |
| ALPC 연결 `0xc0000022` / `0xc00000a5` | 예외 5 → `0xc0000022` | 5 | 〃 |
| bind 가 `0x6c2`/`0x6c0` 등 (문법 못 고름·프로토콜 오류) | `0xc002001f` / `0xc002001d` | `0x6c2` / `0x6c0` (1730 / 1728) | sast/130 1-3 표 + `StatusMap` |
| `0xc0000008` (핸들 무효) 보내기 | `0x6bf` → `0xc002001c` | 1727 | sast/130 2-5 + `StatusMap[0x6bf]` |
| `NtOpenEvent` 못 염 (`0xc0000034`) | 그대로 | 2 | `IsSPMgrReady +0x7d JS +0xd4` → 상태 그대로 → `IsOkayToExec +0x52 JS +0x80`, `+0x8c JNZ +0x3f` (그 상태를 반환) |
| 이벤트 열었으나 `EventState == 0` | `0xc0000001` | 31 (`+0x68` 갈래, 서버 오류칸이 아직 0 이라 `RtlNtStatusToDosError(0xc0000001)`) | `IsSPMgrReady +0xb7 ~ +0xc5` + `GetUserNameExW +0x68 CMP EAX,0xc0000001 / +0x6d JZ +0x118` (오류칸 `[RSP+0x58]` 이 0 이면 `+0x124 JMP +0x73` → `RtlNtStatusToDosError`) |
| 서버가 `0xc0000022` + 오류칸 5 (앱컨테이너 거절) | 그대로 | 5 | `SspiExGetUserName +0x2f1 / +0x2f9` |
| 서버가 `0xc000005f` (`STATUS_NO_SUCH_LOGON_SESSION`) | 그대로 | 1312 | `SspiExGetUserName +0x4af` |
| 버퍼 모자람 | `0x80000005` (형식 비트 없음) / `0xc0000023` (비트 있음) | 234 / **122** | `GetUserNameExW +0xe5`, `+0x129 ~ +0x131` |

- **물러서는 길 없음**: `GetUserNameW` (35바이트) · `GetUserNameExW` · `SspipGetUserName` 어디에도 토큰·레지스트리·환경 변수에서 이름을 찾는 호출이 없다 (`_calls.tsv` 호출 표와 위 `.asm` 전체). 유일한 "다시 시도" 는 `IsOkayToExec +0x80` 의 가장 수준을 내리고 재시도 (가장 토큰이 이벤트를 못 열 때) 와 rpcrt4 의 연결 재시도 (첫 시도 + 다시 3).
- 반환값: sspicli `GetUserNameExW` 의 `BYTE` → advapi32 가 `MOVZX EAX,AL` 로 BOOL. 성공이면 1.

### 1-5. 서버 쪽에서 본 같은 길 (lsass) — `sspisrv.dll` → `lsasrv.dll`

| 항목 | 값 | 근거 |
|---|---|---|
| 포트를 여는 곳 | `sspisrv!SspiSrvInitialize`: `RpcServerUseProtseqEpW("ncalrpc", 10, "lsasspirpc", SD)` → `RpcServerRegisterIf3(인터페이스, NULL, NULL, 깃발 0x21, 최대 호출 0x4d2, 최대 크기 0x100000, 콜백 NULL, SD)` | `SspiSrvInitialize.asm +0x85` (ret `+0x8c`), `+0x115` (ret `+0x11c`); 문자열 `[정적: PE]` sspisrv rva 0x8850 `lsasspirpc` · 0x8868 `ncalrpc`; SD 는 `LsapMakeLowBoxRpcSD` (+0x3b, ret `+0x40`) |
| 인터페이스 | `4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0 (rva 0x75a0), 서버 루틴 **18개**. 기능 플래그 (`Feature_Schannel_KsecddAsyncRpc`) 가 켜지면 비동기 짝 인터페이스 `8923ae44-b00f-4d17-aa8f-57a3a46efb0f` v1.0 (4개) 도 올림 — 이 두 길과는 무관 | `SspiSrvInitialize +0x169 ~ +0x1da`; `[정적: PE]` rva 0x7f70 |
| 서버 프로시저 표 (번호 = 서버 루틴 순서, 주소로 이름을 맞춤) | 0 `SspirConnectRpc` · 1·2 `SspirDisconnectRpcAsync` · 3 `SspirCallRpc` · 4 `SspirAcquireCredentialsHandle` · 5 `SspirFreeCredentialsHandle` · 6 `SspirProcessSecurityContext` · 7 `SspirDeleteSecurityContext` · 8 `SspirSslQueryCredentialsAttributes` · 9 `SspirNegQueryContextAttributes` · 10 `SspirSslSetCredentialsAttributes` · 11 `SspirApplyControlToken` · 12 `SspirLogonUser` · 13 `SspirLogonSystemManagedAdmin` · 14 `SspirLookupAccountSid` · **15 `SspirGetUserName`** · 16 `SspirGetInprocDispatchTable` · 17 `SspirSslSetCredentialsAttributesEx` | `[정적: PE]` `RPC_DISPATCH_TABLE` (sspisrv rva 0x6c20, 18개) + `MIDL_SERVER_INFO` 의 루틴 표 rva 0x6010 `[0x1b50, 0x1bb0, 0x1bb0, 0x1340, 0x1550, 0x1ac0, 0x1010, 0x1480, 0x30d0, 0x2ee0, 0x3200, 0x1c10, 0x16f0, 0x2e20, 0x1cb0, 0x12c0, 0x2d00, 0x32a0]` ↔ `_index.tsv` 주소 |
| 프로시저 0 | `sspisrv!SspirConnectRpc` → `gLsapSspiExtension[+0x18]` (= `lsasrv!SspiExConnectRpc`, `refs`: `LsapSspiExtensionFunctions+0x18` 가 그 함수 주소) | `SspirConnectRpc.asm +0x1b MOV RAX,[RAX+0x18]`, `+0x48` CALL (ret `+0x4d`); `lsasrv/_refs/SspiExConnectRpc.tsv` |
| 프로시저 15 | `sspisrv!SspirGetUserName` → `gLsapSspiExtension[+0x78]` (= `lsasrv!SspiExGetUserName`, `refs`: `LsapSspiExtensionFunctions+0x78`). 인자 포인터 셋 (`CLIENT_ID*`, 출력 문자열, 오류칸) 중 하나라도 NULL 이면 `0xc000000d`, 표에 함수가 없으면 `0xc00000bb` | `SspirGetUserName.asm +0x13 CMP [RAX+0x78],0`, `+0x1a MOV EAX,0xc00000bb`, `+0x6d MOV EAX,0xc000000d`, `+0x62` CALL (ret `+0x67`); `lsasrv/_refs/SspiExGetUserName.tsv` |

---

## 2. `wininet!InternetOpenW` → LSA (질문 2)

### 2-1. 사슬 표

| 차례 | 함수 (모듈) | 하는 일 | 실패 때 | 줄 |
|---|---|---|---|---|
| 1 | `InternetOpenW` (wininet, rva 0xcaa40) | 문자열을 ANSI 로 바꾼 뒤 `InternalInternetOpen` 부르고 **그 결과를 그대로** 반환 | 반환 = 안쪽 반환 (NULL) | `InternetOpenW.asm +0x17d` (ret `+0x182`), `+0x1ce MOV RAX,RBX` |
| 1' | `InternetOpenA` (rva 0xca760) | 같은 함수를 부름 | 같음 | `InternetOpenA.asm +0x4f` (ret `+0x54`) |
| 2 | `InternalInternetOpen` (rva 0xcb39c) | 전역 `GlobalDataInitialized` 가 0 이면 `GlobalDataInitialize` 를 먼저 부름 (모든 wininet 진입점이 그렇다 — `GlobalDataInitialize` 의 caller 46곳). 성공해야 핸들 객체를 만든다 | **`+0x39e JNZ +0x435`** → 추적 한 줄 → `+0x34e SetLastError(ESI)` → 반환 `R13` (처음에 `XOR R13D,R13D` = 0) = **NULL** | `+0x8c CMP [GlobalDataInitialized],0`, `+0x93 JZ +0x395`, `+0x395` (ret `+0x39a`), `+0x39c TEST EAX,EAX`, `+0x435`, `+0x34e` (ret `+0x355`), `+0x35a MOV RAX,R13` |
| 3 | `GlobalDataInitialize` (rva 0x16be0, 12,561바이트) | 전역 설정 읽기·레지스트리·`WSAStartup`·ETW 등록 …의 **한가운데에서 `InitializeGlobalUserName` 을 부름**. 결과를 `[0x18027baa0]` 에 저장, **0 이 아니면 곧바로 끝 (`+0x20bd`) 으로 가서 그 값을 반환** | 반환 = 그 오류값 (`+0x212e MOV EBX,[0x18027baa0]` … `+0x21b5 MOV EAX,EBX`). **`GlobalDataInitialized = 1` 은 맨 끝 성공 자리 `+0x3103` 에서만 쓴다** → 실패하면 다음 호출 때 또 시도 | `+0x1076` (ret `+0x107b`), `+0x107b MOV [0x18027baa0],EAX`, `+0x1089 JNZ +0x20bd`, `+0x3103 MOV [GlobalDataInitialized],1` |
| 4 | `InitializeGlobalUserName` (rva 0x1200cc; `CacheClientGlobalInitialize` 도 부름) | `InitOnceExecuteOnce(&once, _InitOnceGlobalUserName)` — **한 번 성공하면 다시 안 부름** | 콜백이 FALSE 면 `WxGetLastError` 값을 반환 | `+0x25` (ret `+0x2c`), `+0x33 JZ +0x5f`, `+0x5f` (ret `+0x64`) |
| 5 | `_InitOnceGlobalUserName` (rva 0x111520) | **`GetUserNameExA(2, buf, &0x100)`** (형식 2 = NameSamCompatible) → 문자열에 `\` (0x5c) 가 있으면 그 뒤만 남김 (`StrChrA` + `memmove`) → 소문자 사본까지 만들어 전역 `g_pszCurrentUser` · `g_pszCurrentUserLowerCase` · `g_cchCurrentUserLowerCase` 에 저장 | `GetUserNameExA` FALSE → `WxGetLastError` 를 HRESULT 로 (`(ushort)오류 \| 0x80070000`) 만들어 `SetLastError(Win32)` 하고 **FALSE** (같은 오류값이 위로 감) | `+0x58` (ret `+0x5f`), `+0x66 JZ +0x1de`, `+0x76 StrChrA`, `+0x136/+0x13c/+0x143` (전역 저장), `+0x1de WxGetLastError`, `+0x160 SetLastError` |
| 6 | `SspiCli!GetUserNameExA` (wininet 의 **delay import**, IAT rva 0x28f3a8) | 위 1-1 의 2 번 → 3 번 → 이후 1-1 과 **완전히 같음** (형식 칸만 2) | 1-4 표와 같음 → 1722 | `pefile` delay import 표 `SspiCli.dll GetUserNameExA 0x28f3a8` |

### 2-2. 질문에 대한 답

- **보안 꾸러미 묻기인가?** 아니다. `InternetOpenW` 사슬 (지도 `ms/disassemble/wininet/_map/InternetOpenW_edges.tsv`, 깊이 12) 에는 `EnumerateSecurityPackages*` · `QuerySecurityPackageInfo*` · `AcquireCredentialsHandle*` · `InitSecurityInterface*` 호출이 **하나도 없다**. wininet 이 이것들을 delay import 하지만 (`EnumerateSecurityPackagesA`, `AcquireCredentialsHandleA/W`, `GetUserNameExA`) 쓰는 곳은 TLS 핸드셰이크 (`SecurityPkgInitialize` ← `SecureHandshake_Fsm`·`SecureNegotiate_Fsm`) 다.
- **1 의 창구와 같은 인터페이스인가?** 같다 — 같은 `sspicli!GetUserNameExW` → `SspipGetUserName` → `IsOkayToExec` → 프로시저 0 → 프로시저 15. 차이는 ① 호출 형식 칸 = 2 (advapi32 는 `0x10002`), ② 이 이름은 wininet 이 `\` 뒤만 잘라 쓴다는 것 (1 은 서버가 자름).
- **왜 `NULL` 인가 (치명으로 보는 줄)**: `GlobalDataInitialize +0x1089 JNZ` (오류값이 0 이 아니면 끝으로) → `InternalInternetOpen +0x39e JNZ +0x435` → `+0x34e SetLastError` → NULL. 즉 **"현재 사용자 이름을 못 얻으면 wininet 전역 초기화가 실패"** 로 본다 (이름이 길든 도메인이 어떻든 상관없이 `GetUserNameExA` 가 FALSE 면).
- **진짜 Windows 에서 `InternetOpenW` 가 돌려줘야 하는 것**: 연결이 성공하고 `GetUserNameExA(2)` 가 성공하면 핸들 (NULL 아님). 이름 값은 `g_pszCurrentUser` 에만 쓰이고 `InternetOpenW` 결과엔 안 실린다. 성공시키려면 에뮬레이터 서버가 1 의 프로시저 0·15 에 **형식 2 용 이름 (`...\user` 꼴, 비어 있지 않게)** 을 답하면 된다.
- 건너뛰는 갈래: `GlobalDataInitialize` 안에서 `+0x1076` 까지 오는 길에 이 호출을 건너뛰는 갈래가 있는지는 따져 보지 않았다 `[공백]` — 로그 (`r12 … seq 20561 → 20569 NtOpenEvent`) 에서 `InternetOpenW` 직후에 이벤트 열기가 보이는 것과 맞다.

---

## 3. 요청·응답 NDR 꼴 (질문 3)

### 3-0. 근거와 한계

- 형식은 `sspirpc_ProxyInfo` (sspicli rva 0x25000) 에서 읽음 `[정적: PE]`: `pStubDesc` = rva 0x25180, `RpcInterfaceInformation` = rva 0x26bb0, **NDR 2.0 용** 프로시저 형식 문자열 = rva 0x32ae2 (프로시저 N 의 위치 = `FormatStringOffset` 표 rva 0x34d40 의 N번째 u16: 프로시저 0 → 0, 15 → 1168), 형식 (타입) 문자열 = rva 0x34d72. NDR64 용 형식은 `pSyntaxInfo[1]` (rva 0x25590 + 0x50) 쪽에 따로 있다 (안 풂).
- 아래 오프셋은 **스텁 데이터 시작 = LRPC 메시지 `+0x68` 기준** (요청, sast/130 2-1) / **응답은 메시지 `+0x40`** (sast/130 2-3). 요청 `DataLength = 스텁 길이 + 0x40`, `TotalLength = DataLength + 0x28`. 응답 `DataLength = 스텁 길이 + 0x18`, `TotalLength = 스텁 길이 + 0x40`.
- 바이트 꼴은 **형식 문자열 + NDR 2.0 규칙** (정렬·유일 포인터 참조 ID·적합 가변 배열 머리) 으로 도출한 것이다 `[정적 + 규칙]` — rpcrt4 의 마샬러 코드는 읽지 않았다. 동적 3 으로 진짜 바이트를 확인할 것.
- 서버가 문법 마스크로 **NDR64 (2)** 를 고르면 꼴이 달라진다 — 이 표는 마스크 **1 (NDR 2.0)** 을 가정. bind 답은 sast/130 1-3 대로 "제안한 마스크 중 하나" 면 되므로 에뮬레이터는 **1 을 고르면 된다** `[추측: 클라이언트가 표의 NDR 2.0 줄을 쓰는 것은 NdrpClientCall3 안 I_RpcNegotiateTransferSyntax 결과 — 안 읽음]`.

### 3-1. 프로시저 0 — 연결 (프로세스당 한 번)

프로시저 형식 (rva 0x32ae2): 핸들 형식 = **명시적 프리미티브 핸들 (바인딩 핸들, 스택 +0)**, 스택 크기 0x38, 옵션 깃발 `0x46` (클라이언트 크기 필요 · 반환값 있음 · 확장), 인자 6개: `[in, unique, string] char*` · `[in] long` · `[out] long*` · `[out] long*` · `[out] 컨텍스트 핸들 (포인터 경유)` · 반환 `long`. 호출 쪽: `CreateRpcConnection +0x6d` 직전 인자 — `R14 = RCX (NULL)`, `ESI = 2`, 출력 포인터 셋 (`InitState +0x48 ~ +0x5d`: `&[RSP+0x50]` = 깃발, `&SecLsaPackageCount`, `&[RSP+0x58]` = 컨텍스트).

| 방향 | 스텁 오프셋 | 크기 | 뜻 | 값 / 클라이언트가 보는 것 | 근거 |
|---|---|---|---|---|---|
| 요청 | `+0x00` | u32 | 이름 (`char*`) 의 **유일 포인터 참조 ID** | **0** (NULL 이름; 문자열이 있으면 0 이 아닌 ID + 뒤에 적합 문자열) — 이 길에선 항상 NULL (`InitState +0x63 XOR ECX,ECX`) | `InitState.asm +0x63 XOR ECX,ECX`, 형식 `12 08 22 5c` (유일 포인터 → C 문자열) |
| 요청 | `+0x04` | u32 | 연결 종류 | **2** (SSPI 클라이언트 `[이름 추측]`; `LsaConnectUntrusted` 는 0) | `InitState +0x5e MOV EDX,2`; `LsaConnectUntrusted +0xe5 XOR EDX,EDX` |
| (요청 총 8바이트) | | | 프로시저 형식의 "클라이언트 버퍼 크기" 칸도 8 | | 프로시저 형식 `+0x12 = 08 00` |
| 응답 | `+0x00` | u32 | **깃발** (`SecLsaMode` 에 저장). 클라이언트는 **`0x10000` 비트만** 본다 (켜지면 `SecpSetSession` + 프로시저 16 으로 감) | **`0x10000` 을 피할 것**. 서버 (`SspiExConnectRpc`) 는 클라이언트가 lsass 자신이면 `0x10000`, 아니고 보호 프로세스가 아니면 `0x20000`, 보호 프로세스면 0 을 줌 → 일반 클라이언트에겐 `0x20000` 이 자연스러움 (0 도 클라이언트가 안 따짐) | `InitState +0xcc TEST [RSP+0x50],0x10000`, `+0x13b MOV [SecLsaMode],EAX`; `lsasrv SspiExConnectRpc.c` (`*param_3 = 0 / 0x10000 / \|= 0x20000`) |
| 응답 | `+0x04` | u32 | **꾸러미 수** (`SecLsaPackageCount` 에 저장; 이 길에서는 더 안 씀) | 임의의 작은 수 (서버는 `SpmpCurrentPackageCount()`) — **값은 정하지 않음** | `InitState +0x52 LEA R9,[SecLsaPackageCount]`; `SspiExConnectRpc` `*param_4 = SpmpCurrentPackageCount()` |
| 응답 | `+0x08` | 20 | **컨텍스트 핸들** (`u32 속성` + `GUID 16`, 4 정렬) — 클라이언트가 `[SecDllClient+0x18]` 에 보관해 **프로시저 15 의 첫 인자**로 되돌려 줌 | 속성 0 + **0 이 아닌 UUID** (속성 0 · UUID 0 은 "NULL 핸들" 이라 다음 호출이 `CANNOT_BE_NULL` 로 막힘 `[추측: MS-RPCE 규칙, rpcrt4 안 읽음]`). 에뮬레이터는 UUID 로 연결을 구분하면 됨 (지금 `lsar_find_context` 가 `stub+4` 부터 16바이트를 UUID 로 보는 것과 같은 꼴) | `InitState +0xc8 MOV [RCX+0x18],RAX`, 형식 `30 a0 00 00` (컨텍스트 핸들, 포인터 경유 출력) |
| 응답 | `+0x1c` | u32 | **상태 (NTSTATUS)** | **0**. 음수면 `InitState` 가 실패로 (`+0x6e JNS`) | `InitState +0x6a MOV EBX,EAX / +0x6c TEST / +0x6e JNS +0xaa` |
| (응답 총 32바이트) | | | | | |

### 3-2. 프로시저 15 — 사용자 이름 묻기 (`GetUserName*` · `InternetOpen*` 마다)

프로시저 형식 (rva 0x32ae2 + 1168 = 0x32f72): 핸들 형식 = **컨텍스트 핸들 (스택 +0, 바인딩을 컨텍스트에서 얻음)**, 스택 크기 0x30, 옵션 깃발 `0x45`, 인자 6개 (p0 컨텍스트 핸들 `in`, 널 불가 / p1 `[in] CLIENT_ID*` / p2 `[in] long` / p3 `[out] RPC_UNICODE_STRING*` / p4 `[out] long*` / 반환 `long`).

| 방향 | 스텁 오프셋 | 크기 | 뜻 | 값 / 클라이언트가 보는 것 | 근거 |
|---|---|---|---|---|---|
| 요청 | `+0x00` | 20 | **컨텍스트 핸들** (프로시저 0 이 돌려준 20바이트 그대로) | 같은 값 | `SspipGetUserName +0x6b MOV RDI,[RDI+0x18]`, `+0x196 MOV R9,RDI` |
| 요청 | `+0x14` | 4 | 맞춤 (hyper 8 정렬) | 0 | NDR 규칙 `[정적 + 규칙]` |
| 요청 | `+0x18` | 8 | `CLIENT_ID.UniqueProcess` (64비트) | 호출 스레드의 PID (`NtQueryInformationThread(-2, 0)` 의 `ThreadBasicInformation +0x10`) | `SspipGetUserName +0xa7`, `+0xb9 MOV RAX,[RSP+0x78]`, 형식 `15 07 10 00 0b 0b 5c 5b` |
| 요청 | `+0x20` | 8 | `CLIENT_ID.UniqueThread` | 호출 스레드의 TID (`+0x18`) | `+0xc6 MOV RAX,[RSP+0x80]` |
| 요청 | `+0x28` | u32 | **형식 (`EXTENDED_NAME_FORMAT` + 깃발)** | **`0x10002`** (advapi32 `GetUserNameW/A`: 하위 16비트 2 = `NameSamCompatible` + 비트 16) 또는 **2** (wininet, 공개 `GetUserNameExW(NameSamCompatible)`) | `GetUserNameW +0xa`, `_InitOnceGlobalUserName +0x51 MOV ECX,2`, `SspipGetUserName +0x184 MOV [RSP+0x28],R15D` |
| (요청 총 **0x2c** 바이트) | | | | | |
| 응답 | `+0x00` | u16 | `Length` (바이트, **널 제외**) | 이름 글자 수 × 2 | 형식 `1a 03 10 00 …` (`RPC_UNICODE_STRING`), `lsasrv LsapDuplicateString` (`*param_1 = *param_2`) |
| 응답 | `+0x02` | u16 | `MaximumLength` | **`Length + 2`** (서버가 널 한 글자를 더해 복사) | `LsapDuplicateString.c` (`param_1[1] = uVar1 = *param_2 + 2`) |
| 응답 | `+0x04` | u32 | `Buffer` 의 **유일 포인터 참조 ID** | 0 이 아닌 값 (이름이 있으면) | 형식 `12 00 ca ff` (유일 포인터 → 적합 가변 배열) |
| 응답 | `+0x08` | u32 | 적합 가변 배열 `MaxCount` | **`MaximumLength / 2`** (`17 55 02 00` = 구조체 `+2` 의 `u16` 을 2로 나눔) | 형식 문자열 rva 0x34d72 + 0xb8 |
| 응답 | `+0x0c` | u32 | `Offset` | 0 | NDR 규칙 |
| 응답 | `+0x10` | u32 | `ActualCount` | **`Length / 2`** (`17 55 00 00` = 구조체 `+0` 의 `u16` ÷ 2). 옵션 확장 깃발 `0x43` 의 "클라이언트 상관 검사" 비트 `[추측: 이름의 뜻]` 때문에 `MaxCount`·`ActualCount` 가 위 두 칸과 어긋나면 RPC 예외일 것 | 〃 + 프로시저 형식 확장 `0a 43 …` |
| 응답 | `+0x14` | `Length` | 글자 (UTF-16LE, **널 없음**) | 이름 (정책: 사용자 결정) | NDR 규칙 |
| 응답 | `+0x14 + Length` (4 정렬) | u32 | **`[out] long*` 오류칸** — 클라이언트 `SspipGetUserName` 의 세 번째 인자 | **0** (상태가 `0xc0000001` 일 때만 클라이언트가 읽어 `SetLastError`) | `SspipGetUserName +0x4a MOV [R8],R12D` (미리 0), `GetUserNameExW +0x68 ~ +0x7c` |
| 응답 | 그 뒤 | u32 | **상태 (NTSTATUS)** | **0** = 성공. 음수면 1-4 표 | `SspipGetUserName +0x1b3 MOV RBX,RAX`, `+0x108 TEST EBX,EBX / +0x10a JNS +0x1e2` |
| (응답 총 = **28 + Length** (+ 4 정렬 맞춤 최대 2)) | | | | | |

- 클라이언트가 응답에서 하는 일 (`SspipGetUserName +0x1e2 ~ +0x202`): 받은 `Length` > 사용자 버퍼 `MaximumLength` 면 `0x80000005` (사용자 `Length` 에 받은 `Length` 를 써 줌); 아니면 `RtlCopyUnicodeString` 으로 사용자 버퍼에 복사. 글자 버퍼 (`Buffer`) 는 `LocalFree` 로 풂 (스텁이 `LocalAlloc` 계열로 만든다). **`MaximumLength` 는 클라이언트가 안 읽는다** (복사는 `Length` 만).
- **형식 `0x10002` 의 이름 모양**은 서버가 정한다 → 3-3.
- 이 호출의 컨텍스트 핸들 (첫 인자) 이 `[in]` 이므로 연결 뒤 같은 ALPC 포트 연결을 계속 쓴다 (`NtAlpcConnectPortEx` 는 프로세스당 연결 한 번만).

### 3-3. 서버 (lsasrv `SspiExGetUserName`) 가 하는 일 — 값을 만드는 규칙

`lsasrv!SspiExGetUserName(컨텍스트, CLIENT_ID*, 형식, 출력 문자열, 오류칸)` (rva 0x2a2e0, 1,241바이트):

| 단계 | 하는 일 | 실패 때 | 줄 |
|---|---|---|---|
| 1 | 출력 문자열을 비우고 `*오류칸 = 0` | | `RtlInitUnicodeString(출력, 0)` (`+0x82`), `*param_5 = 0` |
| 2 | `SspipBuildCallInfo` → `LsapGetClientInfoEx` (호출한 클라이언트의 로그온 세션 정보) → `CheckAppcontainerUserNameAccess(형식, …)` | 앱컨테이너가 이름을 읽을 권한이 없으면 **`*오류칸 = 5`, 상태 `0xc0000022`** | `+0xbb`, `+0xd2`, `+0xec`; `+0x2f1 MOV [R12],5`, `+0x2f9 MOV EDI,0xc0000022` |
| 3 | `LsapLocateLogonSession(클라이언트의 LUID)` | 세션 없음 → **`0xc000005f`** | `+0x122`; `+0x4af MOV EDI,0xc000005f` |
| 4 | `LsapGetNameForLogonSession(세션, 형식, &이름 지도, 0)` — 이름 지도 (`_LSAP_DS_NAME_MAP`) 의 `+0x18` 이 `UNICODE_STRING`. 도우미 `LsapGetNameForLogonSessionHelper` 는 **하위 16비트만 형식으로, 17번째 비트 (`0x20000`) 를 따로 본다** | 이름이 비면 `0xc0000073`; `0xc0000001` 이면 `GetLastError()` 를 `*오류칸` 에 | `+0x14f` (ret `+0x154`); 도우미 `+0x33 MOVZX ESI,R8W`, `+0x5d BT R8D,0x11`; `+0x460 CMP EDI,0xc0000001` |
| 5a | **형식에 비트 16 (`0x10000`) 이 켜져 있으면** (advapi32 `GetUserNameW/A` 의 `0x10002`): 이름에서 `\` 를 찾아 **그 뒤만** (`\` 없으면 통째) 돌려줌 → `LsapDuplicateString` 으로 복사 | | `+0x174 BT R15D,0x10 / +0x179 JC +0x26f`, `+0x28c wcschr` (ret `+0x293`), `+0x2a5 RtlInitUnicodeString` (ret `+0x2ac`), `+0x19f LsapDuplicateString` (ret `+0x1a4`, 두 갈래 공통) |
| 5b | **비트 16 이 꺼져 있고 하위 16비트가 `0xc` (12) 가 아니면** (`2` = wininet / 공개 `GetUserNameExW(NameSamCompatible)`): 이름 지도의 이름 **그대로** (`DOMAIN\user`) | | `+0x17f CMP R15W,0xc / +0x184 JZ +0x303` (아니면 그대로 `+0x19f`) |
| 5c | 하위 16비트가 `0xc` 면 두 번째 이름 (형식 2, `+0x24f`) 과 합쳐 `도메인\…` 조립 (안 따짐) | 메모리 부족 `0xc0000017` | `+0x310 wcschr` (ret `+0x317`), `+0x371 VerifyStackAvailable`, `+0x426 RtlInitUnicodeString`, `+0x434 MOV EDI,0xc0000017` |
| (특례) | 로그온 세션이 시스템 세션 (`LsapSystemLogonId`) 이고 비트 16 이 켜져 있으면 `LocalSystemNameMap` 을 씀 | | `+0x203 ~ +0x228` |

- **그러므로 에뮬레이터 서버의 답 규칙**: `형식 & 0x10000` 이면 **도메인 없는 이름 (`user`)**, 아니면 **`DOMAIN\user` 꼴 (컴퓨터 이름 또는 도메인 + `\` + 이름)**. **이름 값 자체는 정하지 않음 (사용자 결정, 수집 묶음 `reg-users`)**. 비어 있지 않아야 한다 (비면 실제 서버는 `0xc0000073`).
- 다른 형식 값 (`NameDisplay` 3 · `NameUserPrincipal` 8 · `NameDnsDomain` 12 등) 은 같은 프로시저 15 에 형식 칸만 바뀐다. 형식별 결과 (도메인에 안 붙은 PC 에서 몇 개가 실패하는지) 는 `LsapGetNameForLogonSessionHelper` (3,638바이트) 안이라 이번에 안 풂 `[공백]` — `GetUserNameExW` 30 샘플 중 29 가 0 인 이유가 여기일 수 있다 (다음 요청 후보).

### 3-4. bind 답 (sast/130 와 이어서)

- 요청서 순서의 `연결 → bind → 프로시저 0 → 프로시저 15` 에서 bind 메시지의 `+0x34` UUID = `4f32adc8-6052-4a04-8701-293ccf2096f0`, `+0x44` 버전 = `1.0` (sast/130 1-2 의 칸 그대로). 에뮬레이터 창구 정의에는 **UUID 16바이트 + 버전 (주 1, 부 0)** 을 넣으면 된다 (`GUID` 메모리 모양: `c8 ad 32 4f 52 60 04 4a 87 01 29 3c cf 20 96 f0`).
- 답 `+0x48` (수락 마스크) 은 **1** 권장, 답 `+0x08` 은 0 이 아닌 서버 PID (sast/130 1-3).
- 서버가 모르는 프로시저를 받으면 (`0`·`15` 외) fault — 값은 sast/130 에 없음 (Q246).

---

## 지금 우리 (PE_emulator) 맞나·틀리나

(읽은 파일: `ext/syscall/syscall_layer_cpp/src/alpc/nt_alpc_create_port.cpp` 의 `:180~:195` · `:296~:312`, `rpc/lrpc.cpp` `:26`, `rpc/lsarpc.cpp` `:120~:135`, `sync.cpp:8~:20` — 2026-11-23 시점 줄 번호)

1. **`rpc/lsarpc.cpp` 의 창구 (`\RPC Control\LSARPC_ENDPOINT`, UUID `12345778-1234-abcd-ef00-0123456789ab`) 는 이 두 길의 창구가 아니다 — 틀림.** `GetUserName*`·`InternetOpen*` 은 lsarpc 가 아니라 **sspirpc** (`\RPC Control\lsasspirpc`, `4f32adc8-6052-4a04-8701-293ccf2096f0` v1.0) 로 간다. `lrpc.cpp:26` 의 `window_definitions` 배열 (지금 창구 1개) 에 **새 창구 1개** (포트 이름 · UUID/버전 · 프로시저 `0`·`15`) 가 필요하다. lsarpc 창구는 그대로 두면 된다 (`LsaOpenPolicy` 류 몫).
2. **`nt_alpc_create_port.cpp` 의 `NtAlpcConnectPortEx` 줄 (`:305 ~ :310`) 이 막힘의 직접 원인 — 틀림.** ① 이 줄은 `service_window_connect` 를 **안 부른다** (plain 연결 `:189` 만 부름) ② `live_server_required = a[5]\|\|a[6]\|\|a[7]\|\|a[8]\|\|a[9]` (`:307`) 가 rpcrt4 의 빈 8바이트 속성 버퍼 (`a[9]`, sast/127 `AlpcConnect +0x25e`) 때문에 **늘 참**이라 창구 이름이 맞아도 `0xc0000034` ③ `if (a[5]) return stopped(...)` (`:310`) 가 서버 SD 가 있으면 멈춤. **rpcrt4 는 `gDefaultSecurityDescriptor` 가 비-NULL 이면 (= 모든 정상 프로세스에서) 늘 Ex 길로 오므로 (1-3), 창구 연결을 Ex 에도 붙여야 한다.** 필요한 조건: 이름이 창구와 같으면 `a[5]` (SD) · `a[9]` (빈 속성) 를 값 안 보고 받아 핸들을 `*PortHandle` 에 쓰고 성공 (sast/127 2-3 의 ① 과 같은 응답).
3. **`NtOpenEvent` / `NtQueryEvent` (`sync.cpp:8 ~ :20`) — 맞음.** `\SECURITY\LSA_AUTHENTICATION_INITIALIZED` 는 이미 있고 (`OBJ_CASE_INSENSITIVE` 0x40 이라 대소문자 접힘 OK). **`NtQueryEvent(클래스 0)` 의 `EventState` 가 0 이 아니어야** 한다 (`IsSPMgrReady +0xb7 ~ +0xc5`; 0 이면 `0xc0000001` → LastError 31 로 `GetUserNameW` 0, 연결 시도조차 안 함). 로그에서 연결 시도가 이어지는 걸 보면 지금은 맞게 나가고 있다.
4. **`NtOpenDirectoryObject \RPC Control` · `NtQuerySecurityObject 0x17` ×2 (rpcrt4 초기화) — 맞음, 실패시키면 안 됨.** 이 초기화가 실패하면 오류 14 로 rpcrt4 가 아예 안 된다 (`RpcpNormalizeSecurityDescriptor +0x2ed`, `PerformRpcInitialization +0x2b8 TEST/JNZ`). **첫 `NtQuerySecurityObject` 는 `0xc0000023` + 필요한 크기** 를 돌려줘야 하고 (`+0x164`), 두 번째에 SD 를 채워야 한다 (`+0x1aa JS` 실패). 로그의 "3563 · 3564 두 번" 과 맞음.
5. **"넷씩 묶여 오는 Ex 연결" — 코드와 맞음.** 연결이 `0xc0000034` 면 rpcrt4 가 첫 시도 + 다시 3 (sast/130 2-6) 해서 넷, 그 뒤 `GetUserNameW` **0 + LastError 1722**, `InternetOpenW` **NULL + LastError 1722**. 지금 로그의 결과 (0 / NULL) 는 코드가 그렇게 짜여 있어서다. `InternetOpen*` 은 **실패하면 플래그가 안 서서 호출 때마다 다시 시도**한다 (로그가 한도까지 되풀이되는 이유).
6. **창구를 열어 성공시키는 최소 순서 (제안)**: 연결 (위 2) → bind (UUID·버전, 마스크 1, sast/130) → **프로시저 0** 응답 32바이트 (깃발 `0x20000` 권장 / 꾸러미 수 임의 / 컨텍스트 핸들 20바이트 / 상태 0) → **프로시저 15** 응답 (3-2 표; `형식 & 0x10000` 이면 `user`, 아니면 `DOMAIN\user`). 서버 상태는 **연결별 컨텍스트 UUID 하나** 뿐이면 충분 (`lsarpc.cpp` 의 `connection.contexts` 와 같은 모양). 두 번째 `GetUserName*` 부터는 프로시저 0 없이 **프로시저 15 만** 온다 (`IsOkayToExec +0xf`).

---

## 동적으로 확인할 것 (VM — `GetUserNameW` 한 번 + `InternetOpenW` 한 번)

1. **포트 이름 · Ex 인자**: `rpcrt4!LRPC_CASSOCIATION::AlpcConnect +0x3b9` (call 직전: `rdx` = `ConnectionPortObjectAttributes` 안 이름 = **`\RPC Control\lsasspirpc` 인지**, `[rsp+0x28]` = SD 포인터 → `!sd` 로 내용, `[rsp+0x20]` Flags `0x20000` 인지) 와 `+0x3c0` (ret, `eax`). 같은 호출이 프로세스당 **한 번 (연결 성공 시)** 인지, 두 번째 `GetUserNameW` 에선 안 나오는지.
2. **연결 성공 시의 호출 사이 순서**: `sspicli!SspipGetUserName +0x5c` (`IsOkayToExec` ret, `eax`), `+0x1ae` (`NdrClientCall3` ret — 프로시저 15 직후, `eax` = 상태), `sspicli!CreateRpcConnection +0x74` (프로시저 0 ret), `sspicli!InitState +0x6a`, `sspicli!IsSPMgrReady +0x9c` (`NtQueryEvent` ret; `[rbp+0x1c]` = `EventState`). 콜스택에 `GetUserNameW+0x16` → `GetUserNameExW+0x57` → `SspipGetUserName+0x1ae` 가 보이는지 (정적 `ret_off` 와 맞춤).
3. **메시지 바이트**: bind (`Bind__4e330 +0x62d` ret 직후 답 버퍼 `+0x48` = 진짜 서버가 고른 문법 마스크 — **1 인지 2 인지**), **프로시저 0 요청 (`+0x68` 부터 8바이트)·응답 (`+0x40` 부터 32바이트: 깃발 값, 꾸러미 수, 핸들 속성 값)**, **프로시저 15 요청 (`+0x68` 부터 0x2c바이트: 형식 `0x10002` / 2)·응답 (28 + Length 바이트, `MaximumLength = Length + 2` 인지)**. 위 NDR 도출 (3-0) 확인용.
4. **형식 `2` 이름의 진짜 모양**: `wininet!_InitOnceGlobalUserName +0x5f` (ret, `rax` = BOOL, `rsp+0x40` 의 ANSI 문자열) — `DOMAIN\user` 의 `DOMAIN` 이 컴퓨터 이름인지. (값은 정하지 않지만 꼴 확인.) 같은 호출이 프로세스당 한 번인지 (`InitOnceExecuteOnce` 성공 뒤 안 부름).
5. **`GetUserNameW` 가 돌려주는 크기·널**: `sspicli!GetUserNameExW +0xdc` 근처 `[rdi]` (= `*pcbBuffer`) — 글자 수 + 1 (널 포함) 인지.
6. **연결 실패 때의 재시도 횟수 (VM 에서 서버를 못 닿게 만들 수 있으면)**: `AlpcConnect +0x3b9` 호출 수 = 4 인지, 최종 `GetLastError` 가 1722 인지.
7. **`sspicli!IsOkayToExec +0x80` 갈래**: 가장 수준이 낮은 스레드 (이름 묻기 전에 `ImpersonateSelf(Anonymous)` 등) 에서 이 갈래가 도는지 — 정상 샘플에서는 안 돌 것으로 보임 `[추측]`.

## 다음 요청 후보

1. **`GetUserNameExW` 형식별 서버 결과** — `lsasrv!LsapGetNameForLogonSessionHelper` (3.6KB) 를 형식 3·8·12 별로: 도메인에 안 붙은 PC 에서 어느 형식이 `0xc0000073` 등으로 실패하나 (`GetUserNameExW` 30 샘플 중 29 가 0 인 이유 후보, 요청서의 `[수집]` 이름 정책 근거).
2. **`QuerySecurityPackageInfoW` · `EnumerateSecurityPackagesW` 길 (샘플 18 + 에뮬레이터 로그의 `QuerySecurityPackageInfoW`)** — `SecLocatePackageExW` → `SecpLoadLsaPackages` → `SecpGetBinding`/`SecpEnumeratePackages` → `CallSPM` → `CallRpcSPM +0xda` (프로시저 3 = `SspirCallRpc` 로 보임 `[추측: LEA EDX,[RSI+3] 의 RSI]`) — **같은 창구 `lsasspirpc`**, 프로시저 3 의 형식·응답 (패키지 표). 이미 `sspicli` 가 분석돼 있어 가볍다.
3. **`LookupAccountSidW` · `LookupAccountNameW` (35 샘플)** — `advapi32!LsaICLookupSids`·`LsaICLookupNames` → lsarpc (`LsapCreateBindingHandleForLocal`, `RpcBindingCreateW`) 인지, 서버 프로시저 14 (`SspirLookupAccountSid`) 인지.
4. **`SHGetKnownFolderPath` (43) · `gethostbyname` (20)** 의 RPC 쪽 (요청서 로그의 "Ex 연결을 받은 샘플" 나머지) — 같은 창구인지 확인.

## 판정

**분석성공.** 질문 1·2 는 코드 줄 근거로 닫았다 (사슬 · 포트 이름 문자열 · 인터페이스 UUID/버전/프로시저 번호 · 동기 · 연결 SD 의 출처 · 실패 값 흐름 · 물러서기 없음). 질문 3 은 클라이언트 형식 문자열 + 서버 (`sspisrv`·`lsasrv`) 코드까지 읽어 닫았고, **NDR 바이트 꼴은 규칙 도출이라 동적 3 으로 확인**하라고 남겼다. `[공백]`: ① 연결 인자 6 SD 의 최종 내용과 커널이 그걸 어떻게 쓰는지 ② `GlobalDataInitialize` 안 `+0x1076` 앞의 건너뛰기 갈래 ③ 형식 3·8·12 등의 서버 결과 ④ bind 마스크 1/2 중 실제 서버의 선택.
