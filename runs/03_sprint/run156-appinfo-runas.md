# 2026-11-27 (작성일, 정적 분석 2026-11-27) run156 — `runas` 승격 부탁 길 (Q290 + Q276a, 정적만): `ShellExecuteExW` (verb `runas`) 의 승격 부탁은 **windows.storage 안에서 끝난다** — shell32 `ShellExecuteExW` 는 지연 로드 썽크 한 줄이고, windows.storage `ShellExecuteExW` → … → `CInvokeCreateProcessVerb::Execute` → `Launch` → `_TryCreateProcess` → `_PrepareAndCallCreateProcess` → `CallCreateProcess +0xc04` → **`AicLaunchAdminProcess +0x636 Ndr64AsyncClientCall`** (비동기, 프로시저 **0** = appinfo `RAiLaunchAdminProcess`). 인터페이스는 **`201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0**, `ncalrpc`, **끝점 없음** → rpcrt4 가 `ept_map` 을 묻는다. 원문 "막힌 자리" 의 `0xe1af8308…` 는 **EPM 자신** (run145) 이고, 그 요청 안 SID 는 클라이언트가 QOS V3 에 넣은 **LocalSystem `S-1-5-18`** ("서버는 SYSTEM 이어야 한다") 이다. 승격 쪽으로 가는 조건은 둘 — verb 가 "runas" (`QueryExists` 글자 비교) 이고, **`SHTestTokenMembership(NULL, 0x220)` (켜진 Administrators 그룹) 이 거짓**이어야 한다 (참이면 RPC 없이 그냥 `CreateProcessW`). 서버는 consent 승인 뒤 받은 승격 토큰으로 **호출자를 부모로 (`0x20000` 속성)**, `CREATE_SUSPENDED` 가 켜진 플래그 `0x04080414 | 0x80004` 로 `CreateProcessAsUserW` 하고, 두 핸들을 **호출자 핸들 표로 복제**해 돌려준다. **깨우는 것은 클라이언트** (`CallCreateProcess +0xdf7 ResumeThread`). 오류는 `ShellExecuteExW` 가 FALSE 로 돌려주되, `SEE_MASK_FLAG_NO_UI` 가 꺼져 있으면 **`GetLastError` 는 원래 값이 아니라 `0x4c7`** 이 되고, `hInstApp` 은 `0x4c7` · `0x6f7` · `0x5` 모두 **5**. 앞의 RPCSS 연결 셋은 이 길에 **꼭 필요하지 않다** (`CoCreateInstance(ImmersiveShell)` 실패를 무시하고 감)

(글의 오프셋은 **함수 시작 기준** `+0x…` — call 자리, ret 는 아래 "정적 — 호출 지점" 표. `.rdata` · 구조체 자리는 각 모듈 rva. 결과서 값과 call 줄이 다르게 읽히는 곳은 아래 "flow-writer 대조". 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원문 `../PE_emulator/_workspace/05_tuning_planner/374_internals-appinfo-runas.md`, 읽기만 — 튜닝 `374_tune19.md` UAC1 · 탓 35 `runas_fail`) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 `ShellExecute*` 를 안 부른다 — `sample/` 에 `ShellExecute` · `runas` · `shell32` 글자가 없다 `[소스]`. 초기화 끝 (P2) 모듈은 test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2 여섯 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run143 · run155 와 같은 줄) — shell32 · windows.storage 가 없다. appinfo 는 **다른 프로세스** (svchost 안 서비스). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (shell32 / windows.storage 줄 · 새 appinfo 줄 · kernelbase 줄 한 마디 · 근거 줄) · 3장 run143 절 한 줄 + 새 절 "`runas` 승격 부탁" · 4장 (run143 줄 둘에 표시 · run156 줄 셋) · 5장 (run156 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q290** (380 차례 ⑤, 사용자 결정 2026-11-27 PE_emulator D179) + **Q276a** (Q276 ① 을 앞으로 — windows.storage · shell32 analyze · 지도, 뿌리 `ShellExecuteExW`). 결과서 판정 **분석성공** — 원문 물음 셋 모두 줄 근거로 답함. 못 닫은 것: `hProcess` 칸 (`CShellExecute+0xa8`) 을 채우는 곳, `this+0x1f8` 비트의 출처, consent.exe 쪽 토큰 원천, rpcrt4 SID 재시도 이음.
> - 이어 쓰는 앞 run: **run143** (windows.storage `CInvokeCreateProcessVerb::Execute` rva `0xe1de0` → `Launch` → `_TryCreateProcess` → `_PrepareAndCallCreateProcess +0xdf _FixupWorkingDirectory` — 이번 사슬이 같은 함수를 지나간다, "`ShellExecute*` → `Execute` 는 가상 함수라 `[공백]`" 이 이번에 닫힘), **run145** (`ept_map_auth_async` = EPM 프로시저 8 · 고르기 규칙 · SID 로 SD 짓기 5절), **run149 · run151** (COM → RPCSS bind 넷 · `Connect` — 원문 "RPCSS 연결 셋" 과 견줌), **run34** (`CreateProcessInternalW` — 이번엔 `+0x47ba BaseCheckElevation` 한 줄만).
> - 앞 run 고쳐 읽기 하나: run143 문서 맨 위 한 줄 (원문은 그대로).
> - 남은 것 → 동적은 Q280 · Q283 RPC 묶음에 얹을 꼴 (아래 "아직 확인 필요" D1 ~ D8), 디버거 없이 떠 올 것 (`whoami /all` 두 번 · UAC 레지스트리 · verb 키 · 서비스 설정), 정적은 결과서 "다음 요청 후보" 1 ~ 6 — 대기열 자리는 오케스트레이터가 정함.

## 요약

- **부탁 사슬** `[정적]`: shell32 `ShellExecuteExW` (`0x2013f0`) = `JMP [IAT]` 한 줄 → 지연 로드 → **windows.storage `ShellExecuteExW` (`0x38680`) `+0xbc`** `ShellExecuteNormal` `+0x55` `CShellExecute::ExecuteNormal` `+0x265` `_DoExecute` `+0xca` `_ExecuteAssoc` `+0x175` ⇢ (`[RAX+0x48]`) `CBindAndInvokeStaticVerb::Execute` `+0x18f` `TryCreateProcessDdeHandler` `+0xc0` `InitAndCallExecute` `+0x214` ⇢ (`[RAX+0x48]`) **`CInvokeCreateProcessVerb::Execute`** `+0x58` `Launch` `+0x10d` `_TryCreateProcess` `+0x3c` `_PrepareAndCallCreateProcess` `+0x14d` `CallCreateProcess` **`+0xc04` `AicLaunchAdminProcess`** **`+0x636` `Ndr64AsyncClientCall`**.
  - 가상 호출 두 칸의 대상은 PE 가상표로 확인했다 (flow-writer, `pefile`): `CBindAndInvokeStaticVerb` 의 `IExecuteCommand` 가상표 (rva `0x6a1fd0`) `+0x48` = `0x3212d0`, `CInvokeCreateProcessVerb` 의 같은 가상표 (rva `0x6a2068`) `+0x48` = `0xe1de0`. 그 자리에 **어느 객체가 오는지** 는 만드는 줄 (`CExecuteAssociation_CreateInstance +0x66` · `TryCreateProcessDdeHandler +0x3b`) 로 이은 것이다.
- **승격으로 갈리는 두 문** `[정적]`:
  1. verb — `CInvokeCreateProcessVerb::Execute +0x4a SetElevationExecMethodIfNeeded` → `+0x2a` ⇢ `QueryExists(0x2020011)` (`CAssocShellVerbElement` 가상표 `+0x30` = `0x210150`, flow-writer 확인). 레지스트리 값 `Elevate` 가 없으면 `+0x49 StrCmpICW(verb, L"runas")` → 참이면 `+0x7e [this+0x1a8] = 2` (방법 2 = 승격).
  2. 토큰 — `CallCreateProcess +0x3a CMP [this+0x1a8],2` → `+0x4a SHTestTokenMembership(NULL, 0x220)` (`S-1-5-32-544` 을 `CheckTokenMembership(NULL, …)` 로) 가 **참이면 방법을 0 으로** (`+0x53 XOR R14D,R14D`) → RPC 없이 `+0x1f0 CreateProcessW`. 거짓이어야 `+0x13f JNZ +0x210` 으로 두 `CreateProcess*` 를 건너뛰고 승격 쪽으로 간다.
- **RPC** `[정적]` + `[정적: PE]`: `201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0 (클라이언트 구조체 rva `0x702b90`, 서버 `appinfo` rva `0x50190`), 프로시저 0 (`+0x62d XOR EDX,EDX`), 비동기 · 명시 바인딩. 바인딩 글 = 객체 UUID `201ef99a-…` (같은 글) · `ncalrpc` · 주소 · 끝점 NULL (`AicpCreateBindingHandleEx +0x5c`). 인증 6 · 10 · QOS V3 `Sid` = `CreateWellKnownSid(22)` (`+0xc7 LEA ECX,[RDX+0x16]` · `+0xca`).
  - 요청 11칸 (+ 바인딩): 실행 파일 · 명령줄 · `StartFlags` · **`CreationFlags 0x04080414`** · 현재 폴더 · `WinSta0\Default` 꼴 · 시작 정보 56 B · `HWND` · **시간 제한 `0xffffffff`** · out `PROCESS_INFORMATION` 24 B · out `ELEVATION_REASON` · 반환.
- **응답 뒤 (클라이언트)** `[정적]`: 핸들 검사 없음. 오류 0 이면 `+0xc93 PcaMonitorProcess2` (반환 안 씀) → `+0xdda SetProcessLaunchForegroundPolicy` (있을 때) → **`+0xde6` 호출자 플래그에 `CREATE_SUSPENDED` 가 없으면 `+0xdf7 ResumeThread(hThread)`** → `+0xe40 SetLastError`.
- **오류 → 반환** `[정적]`: `ShellExecuteExW` 는 오류 ≠ 0 · ≠ `0x8000000a` 면 FALSE (`+0x48` · `+0xee`). `_TryErrorMsgBox` 가 상자를 띄울 조건 (NO_UI 꺼짐 · 오류가 0 / `0x4c7` / `0x8000000a` 아님) 이면 `Finalize +0x65 / +0x6f` 가 **반환 오류를 `0x4c7` 로 바꾼다**. `hInstApp` 은 표에 없는 값이면 5 (`_FinalMapError` 표 rva `0x768560`). RPC 재시도 집합은 `{0x6ac, 0x6b5, 0x6ba, 0x6d9}` (두 번까지, 사이에 `AicpStartAIS` = SCM 으로 `AppInfo` 시작) — **`0x6f7` 은 안 든다.**
- **RPCSS 연결 셋** `[정적]` + `[추측: 짝]`: 이 길의 COM 호출은 `CallCreateProcess +0x930 CoCreateInstance(CLSID_ImmersiveShell, CLSCTX_LOCAL_SERVER)` (5초 제한 `+0x8f6`) 하나이고, 실패하면 `+0x93e JS` 로 결과를 안 쓰고 `+0xc04` 로 간다. 원문 로그의 bind 거절 셋이 이 호출에서 났다는 짝은 `[추측]` (순서만 맞음).
- **appinfo (들어주는 쪽)** `[정적]`: `RAiLaunchAdminProcess` (`0x43070`) → `+0x407 CheckElevationEnabled` → `+0x437 AiGetClientInformation` (호출자 PID → `NtOpenProcess(0x1004c0)` · 토큰 사본 · 세션) → … → `+0xe33 AiCheckLUA` (→ consent.exe, 토큰 없음 + `0x10` 꺼짐 = **`0x4c7`**, 승격 아님 · 관리자 아님 = **`0x2e4`**, 세션 id 를 호출자 것으로) → **`+0x1096 AiLaunchProcess`** (`0x30370`) → `+0x9c7` 속성 `0x20000` (부모 = 호출자) · `+0x9fb` `0x60001` → `+0x10a2` (또는 `+0x12a7` · `+0x138e`) **`CreateProcessAsUserW(토큰, …, FALSE, 플래그 | 0x80004, …)`** → `+0x1499` · `+0x14ef` **`NtDuplicateObject(-1, h, 호출자, …, 옵션 2)`** → `+0x131f RpcAsyncCompleteCall`. 서버는 `CREATE_SUSPENDED` 가 켜져 있으면 안 깨운다 (`+0x1567` 앞 검사).
  - 토큰 칸 (승격 종류 · 무결성 · 그룹 · 특권) 은 **appinfo 안에 짓는 줄이 없다** — consent 가 돌려준 토큰을 "승격(`0x14`) 이고 관리자" 로 검사만 한다. 원천은 `LUAGetElevatedToken` 의 `TokenLinkedToken` 일 것 `[추측]`.
- **Q276a 지도** `[정적]`: windows.storage `_map/ShellExecuteExW_*` 함수 424 (깊이 10, 기준 20) — 직접 호출만이라 `_ExecuteAssoc` (깊이 4) 까지 닿고 가상 호출에서 끊긴다, `AicLaunchAdminProcess` 는 지도에 없음 (사슬은 `_refs/` 로 거꾸로 이음). shell32 `_map` 은 노드 1 · 간선 0 (썽크).
- **PE_emulator 쪽 한 줄씩** → 끝 "PE_emulator 쪽" 절.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열넷째 바퀴. plan **Q290 + Q276a**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 셋** — shell32 (함수 34,079 · `FUN_` 5) · windows.storage (43,911 · 13) · appinfo (1,387 · 0), 하나씩 (`MAXMEM` 12G · 12G · 8G). kernelbase 는 있던 프로젝트에서 `SHCoCreateInstance` 하나만 뽑음. rpcrt4 · combase · rpcss · kernel32 · ole32 는 **Ghidra 로 안 열었다** (있던 `.asm` 글 읽기 · `pefile` · `objdump` 만).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| shell32.dll | `011DE059CF3D0B8174FA9B5324EA76541` | 같음 (run143 — 그땐 PDB 만, 이번에 처음 analyze). PDB 안 Age 2 |
| windows.storage.dll (`Windows.Storage.pdb`) | `EF42E52BBA15CEFDA22CB070B2FB69881` | 같음 (run143 — 같은 사정). PDB 안 Age 2 |
| appinfo.dll | `9A9DD55CF57F3F72CCF874887B92209C1` | **새 줄**. PDB 안 Age 3 |
| KernelBase.dll | `72CCE506944DE0267C172AD64287F9BD1` | 같음 (run149 등) |

- 바이너리 쪽 Age 1 · PDB 쪽 Age 2 ~ 3 은 rpcss · RpcEpMap 때와 같은 모양 (결과서). analyze 로그에 `SetPdb:` 줄 셋 다 있음.
- 버전 대조 (결과서 "버전 확인" — 일치): sast/140 의 `CInvokeCreateProcessVerb::Execute` rva `0xe1de0` = 새 `Execute__e1de0.asm` 첫 줄 `rva=0xe1de0`, 사슬 직접 호출 24줄을 원본 바이트로 다시 계산 (`E8` 목적지 · `48 FF 15` 7바이트), kernelbase `CreateProcessInternalW +0x47ba` 는 있던 `_calls.tsv` 와 새 `.asm` 이 같음. flow-writer 도 아래 "방법" 의 줄을 `.asm` 으로 다시 봤다.

## 목적

- PE_emulator 쪽 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `r19 00858b3f74b5 seq 1561484 (ShellExecuteExW lpVerb "runas")` → `1567119 · 1567154 · 1569187` (RPCSS `0xe60c73e6` bind 거절 `0x6b5` ×3) → `1569230` (epmapper bind) → `1569243` (`ept_map` — iface `0xe1af8308` proc 8 · reply 2 · rpc_status `0x6f7`) → `1570099` (rax 0). `ShellExecuteW` 꼴 `b3a62167e174 seq 10159 → 10440`. 원문 물음 셋:
  1. **부탁하는 쪽** — `ShellExecuteEx*` (`runas`) → RPC 를 거는 함수 사슬, UUID · 버전 · 프로시저 · protseq · 끝점 (왜 SID 를 넣나), 요청 · 응답 꼴.
  2. **응답 뒤** — 클라이언트 검사 · 깨우기 · `hProcess` 조건, 오류 값마다 반환 · `GetLastError` (`0x4c7` · `0x6f7` · `0x5`), RPCSS 연결 셋이 꼭 필요한가.
  3. **들어주는 쪽 (appinfo)** — 승인 뒤 자식을 만들 때 토큰 · 부모 · 플래그 (멈춘 채 만들고 누가 깨우나).
- Q276a: windows.storage · shell32 analyze + 지도 (뿌리 `ShellExecuteExW`) — run143 "다음 요청 후보 1".

## 방법

- 정적 sast/152 (요청서 `_workspace/sast/152_appinfo-runas.md`, 결과서 `_workspace/sast/152_appinfo-runas_result.md`, 판정 위, 스크립트 문제 없음).
  - windows.storage `.asm`/`.c` (사슬 + 깊이 1 callee 약 130 함수, appinfo 포함): `ShellExecuteExW` · `ShellExecuteNormal` · `ExecuteNormal` · `_DoExecute` · `_ExecuteAssoc` · `_GetExecute` · `_SHCoCreateInstance` · `SHCoCreateInstanceWorker` · `Execute__3212d0` · `TryCreateProcessDdeHandler` · `InitAndCallExecute` · `Execute__e1de0` · `SetElevationExecMethodIfNeeded` · `QueryExists__210150` · `Launch__e102c` · `_TryCreateProcess` · `_PrepareAndCallCreateProcess` · `CallCreateProcess` · `SHTestTokenMembership` · `AicLaunchAdminProcess` · `AicpCreateBindingHandleEx` · `AicpStartAIS` · `AicpAsyncInitializeHandle` · `AicpAsyncFinishCall` · `Finalize__2fae0c` · `_FinalMapError` · `_TryErrorMsgBox` · `OnCreated__17cd70` 등, `_map/` · `_refs/`.
  - appinfo: `RAiLaunchAdminProcess` · `AiLaunchProcess` · `AiCheckLUA` · `AiLaunchConsentUI` · `AiGetClientInformation` · `LUAGetElevatedToken` · `AiEnableDesktopRpcInterface` · `ServiceMain` · `RAiForceElevationPromptForCOM` · `RAiGetElevatedToken` 등.
  - shell32: `ShellExecuteExW.asm` · `DelayLoad_ShellExecuteExW.asm` · `_map/`. kernelbase: `SHCoCreateInstance.asm` (새) · `CreateProcessInternalW.asm` (있던 것).
  - RPC 구조체 · NDR64 형식 · 서버 표 · 오류 표 · CLSID 는 결과서가 `pefile` 로 읽음.
- flow-writer 가 다시 본 것:
  - `.asm` 줄: 위 windows.storage 함수의 결과서 인용 줄 전부 (`CallCreateProcess` 30줄 · `AicLaunchAdminProcess` 24줄 · `AicpCreateBindingHandleEx` 9줄 · 사슬 함수 각 2 ~ 8줄), appinfo `RAiLaunchAdminProcess` 22줄 · `AiCheckLUA` 9줄 · `AiGetClientInformation` 8줄 · `AiLaunchProcess` 20줄 (+ `+0x145b ~ +0x1499` 옵션 계산) · `AiLaunchConsentUI` 3줄 · `ServiceMain` 12줄 · `AiEnableDesktopRpcInterface +0x1f ~ +0x139`, kernelbase `SHCoCreateInstance +0x3f` · `CreateProcessInternalW +0x47ba`.
  - `pefile` (스크래치 `s156/`, 레포 밖 — `-I` 로는 `pefile` 를 못 찾아 빼고 돌림, 스크립트 폴더엔 내 파일만, MS 바이너리만 읽음): windows.storage 클라이언트 구조체 `0x702b90` (길이 `0x60` · `201ef99a…` v1.0 · NDR 2.0 문법) · 글 `0x779b80` (`201ef99a-…`) · `0x73b130` (`ncalrpc`) · `0x734ae0` (`runas`); appinfo 서버 구조체 `0x50190` (같은 UUID · v1.0) · 주석 글 `0x55280` (`AppInfo`); **가상표 셋** — PDB 공개 이름 (`llvm-pdbutil-14 dump -publics`) 으로 자리를 찾고 칸을 읽음: `??_7CBindAndInvokeStaticVerb@@6BIExecuteCommand@@@` (rva `0x6a1fd0`) `+0x48` = `0x3212d0`, `??_7CInvokeCreateProcessVerb@@6BIExecuteCommand@@@` (rva `0x6a2068`) `+0x48` = `0xe1de0`, `??_7CAssocShellVerbElement@@6BIAssociationElement@@@` (rva `0x6976c8`) `+0x30` = `0x210150` (`QueryExists`) · `+0x40` = `0x295fa0` (`QueryObject`).
  - 지도: `windows.storage/_map/ShellExecuteExW_summary.txt` · `_nodes.tsv` 깊이 칸 세기, `shell32/_map/ShellExecuteExW_summary.txt`.
  - 별칭: `pdb_aliases.py appinfo.dll` (표 새로 지음 — 무리 50곳) → `--lookup appinfo.dll 0x1bf0c`. 이 문서의 windows.storage 이름은 `_calls.tsv` 가 `ALIASES(1)` (PDB 이름 하나) 로 적은 것.
  - `../PE_emulator` (읽기만): `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` (286줄) · `include/syscall_layer/tokens.hpp` (50줄), 원문 374.
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- 관리자 일 (승격 실행) 은 **직접 못 하고 관리실 (appinfo) 에 신청서를 넣는** 일이다. 신청서엔 "무엇을 · 어디서 · 어떤 창에서" 를 적고, 관리실은 "정말요?" 창 (consent) 을 띄운 뒤 **신청한 사람 이름으로 (부모 = 호출자)** 프로그램을 **멈춘 채** 띄워 그 열쇠 (핸들) 를 신청자 주머니에 넣어 준다. 깨우는 건 신청자 몫이다 `[추측: 비유]`.
- 관리실 전화번호는 수첩에 없어서 **안내 데스크 (EPM) 에 먼저 묻는다** — 원문의 `0xe1af8308` 은 그 안내 데스크 번호다. "관리실은 SYSTEM 이 받아야 해요" 라고 같이 말하는 게 SID `S-1-5-18` 이다 `[추측: 비유]`.
- 이미 관리자 열쇠를 **꺼내 든** 사람 (켜진 Administrators 그룹) 이면 신청서 없이 그냥 문을 연다 (`CreateProcessW`) `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{shell32,windows.storage,appinfo,kernelbase}/`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `shell32!ShellExecuteExW` (`0x2013f0`, 6 B) | `+0x0` | — | `JMP [0x18077a3e0]` = 지연 로드 슬롯 → `DelayLoad_ShellExecuteExW +0x7` `__tailMerge_windows_storage_dll` → `Windows.Storage.dll!ShellExecuteExW` | [정적] |
| `windows.storage!ShellExecuteExW` (`0x38680`) | `+0xbc` | `+0xc1` | `ShellExecuteNormal` (`0x353d08`) — 앞 `+0x2e CMP [RCX],0x70` (cbSize, 틀리면 `hInstApp=5` · `+0x3c SetLastError(5)`) | [정적] |
| 〃 | `+0x3c` | `+0x43` | `SetLastError(EDI)` — 끝 `+0xe0 JMP +0x3a` 로 성공 · 실패 모두 여기 | [정적] |
| `ShellExecuteNormal` | `+0x55` | `+0x5a` | `CShellExecute::ExecuteNormal` (`0x17b518`) — 앞 `+0x2a operator new` · `+0x42` 생성자 | [정적] |
| 〃 | `+0x60` | `+0x65` | `CShellExecute::Finalize` (`0x2fae0c`) → `+0x68 MOV EBX,EAX` (= `dwErrCode`) | [정적] |
| `CShellExecute::ExecuteNormal` | `+0x140` / `+0x252` | `+0x145` / `+0x257` | `_Init` (verb → `this+0x60`) / `SHCoInitialize` | [정적] |
| 〃 | **`+0x265`** | **`+0x26a`** | **`CShellExecute::_DoExecute`** (`0x5bf5ac`) | [정적] |
| `_DoExecute` | **`+0xca`** | **`+0xcf`** | **`_ExecuteAssoc`** (`0x241298`) — 앞 `+0x105 _CreateAssocFromItem` / `+0xb8 _CreateAssocForClassOrKey` | [정적] |
| 〃 | `+0x134` · `+0x152` · `+0x15c` | `+0x139` · `+0x157` · `+0x161` | `_ReportHresult` · `_FinalMapError` · `_TryErrorMsgBox` | [정적] |
| `_ExecuteAssoc` | `+0xa7` | `+0xac` | ⇢ `IAssociationArray::QueryObject` (`+0x91` IID `7f9185b0-…` · `+0x98` verb · `+0x9c EDX=0x2200010`) | [정적] |
| 〃 | **`+0x175`** | **`+0x17a`** | ⇢ **`[RAX+0x48]` `IExecuteCommand::Execute`** — `CBindAndInvokeStaticVerb` 가상표 `+0x48` = `Execute__3212d0` | [정적] (가상표 `pefile`) |
| `CAssocShellVerbElement::_GetExecute` (`0x1ef524`) | `+0x4a` | `+0x51` | kernelbase `SHCoCreateInstance` (`CLSID_ExecuteAssociation`) `+0x3f` → `SHCoCreateInstanceWorker` (windows.storage) → `_SHCoCreateInstance +0xae _CreateFromDllGetClassObject` | [정적] · api 집합 → 모듈 [추측] |
| `CBindAndInvokeStaticVerb::Execute` (`0x3212d0`) | `+0x16f` · `+0x17c` | `+0x174` · `+0x181` | `TryExecuteCommandHandler` (DelegateExecute) · `TryDropTargetHandler` — "내 일 아님" 이어야 다음 줄 | [정적] |
| 〃 | **`+0x18f`** (· `+0x1cb`) | **`+0x194`** (· `+0x1d0`) | **`TryCreateProcessDdeHandler`** (`0x321750`) | [정적] |
| `TryCreateProcessDdeHandler` | `+0x3b` / **`+0xc0`** | `+0x40` / **`+0xc5`** | `CreateInvokeCreateProcessVerb` (`new` 0x348 B) / **`InitAndCallExecute`** (`0x3214ec`) | [정적] |
| `InitAndCallExecute` | **`+0x214`** | **`+0x219`** | ⇢ **`[RAX+0x48]`** — `CInvokeCreateProcessVerb` 가상표 `+0x48` = **`Execute__e1de0`** | [정적] (가상표 `pefile`) |
| `CInvokeCreateProcessVerb::Execute` (`0xe1de0`) | `+0x35` | `+0x3a` | `IUnknown_GetParentWindow` (→ `this+0xc0` 창) | [정적] |
| 〃 | **`+0x4a`** | **`+0x4f`** | **`SetElevationExecMethodIfNeeded`** (`0xe2128`) — `+0x19 EDX=0x2020011` · `+0x2a` ⇢ `[RAX+0x30]` `QueryExists` · **`+0x7e [RBX+0x1a8]=2`** | [정적] |
| 〃 | **`+0x58`** | **`+0x5d`** | **`Launch`** (`0xe102c`) | [정적] |
| `CAssocShellVerbElement::QueryExists` (`0x210150`) | `+0x49` | `+0x50` | `StrCmpICW(…, L"runas")` (`+0x42 LEA RDX,[0x180734ae0]`) | [정적] (글 `pefile`) |
| `Launch` | **`+0x10d`** | **`+0x112`** | **`_TryCreateProcess`** (`0x17d118`) — 앞 `+0x21 SetCommandTemplate` · `+0x34 ProcessCommandTemplate` | [정적] |
| `_TryCreateProcess` | **`+0x3c`** | **`+0x41`** | **`_PrepareAndCallCreateProcess`** (`0x17d38c`) — 앞 `+0x2e _PolicyAllowsCreateProcess`, 뒤 `+0x79 NotifyCreatedProcess` · `+0x85 _FixActivationStealingApps` | [정적] |
| `_PrepareAndCallCreateProcess` | (줄) `+0x2b` | — | `MOV [RDI+0xe4],0x4000410` (만들기 플래그 기본값) | [정적] |
| 〃 | `+0xdf` | `+0xe4` | `_FixupWorkingDirectory` (run143 의 볼륨 길 시작) | [정적] |
| 〃 | **`+0x14d`** | **`+0x152`** | **`CallCreateProcess`** (`0x17eb78`) — 실패 때 `+0x45c InvokeAppActivationFailedHandler` | [정적] |
| `CallCreateProcess` | **`+0x4a`** | **`+0x4f`** | **`SHTestTokenMembership(NULL, 0x220)`** (`+0x3a CMP [RCX+0x1a8],2` · `+0x43 EDX=0x220` · `+0x48 XOR ECX,ECX`) → 참이면 `+0x53 XOR R14D,R14D` (방법 0) | [정적] |
| 〃 | `+0x19b` / `+0x1f0` | `+0x1a2` / `+0x1f7` | `CreateProcessAsUserW` (방법 1) / `CreateProcessW` (방법 0) — 방법 2 면 `+0x13f JNZ +0x210` 로 둘 다 건너뜀 | [정적] |
| 〃 | `+0x8f6` / **`+0x930`** | `+0x8fb` / **`+0x937`** | `CRPCTimeout(5000)` / **`CoCreateInstance(CLSID_ImmersiveShell, CLSCTX_LOCAL_SERVER)`** → `+0x93e JS +0x997` (실패 무시) | [정적] |
| 〃 | **`+0xc04`** | **`+0xc09`** | **`AicLaunchAdminProcess`** (`0x331204`) → `+0xc0c TEST EAX,EAX` · `+0xc15 OR ESI,0x80070000` | [정적] |
| 〃 | `+0xc93` / `+0xdda` | `+0xc9a` / `+0xde1` | `PcaMonitorProcess2` (반환 안 씀) / `SetProcessLaunchForegroundPolicy` | [정적] |
| 〃 | **`+0xdf7`** | **`+0xdfe`** | **`ResumeThread`** (`+0xde6 TEST byte [R15+0xe4],4`) | [정적] |
| 〃 | `+0xe40` | `+0xe47` | `SetLastError` | [정적] |
| `AicLaunchAdminProcess` (`0x331204`) | `+0x1c2` | `+0x1c7` | `AicpCreateBindingHandleEx` (`0x628484`) — 캐시 `+0x1a3 CMP [0x180831100]` · `+0x1e4 CMPXCHG` | [정적] |
| 〃 | `+0x57b` | `+0x580` | `AicpAsyncInitializeHandle` | [정적] |
| 〃 | **`+0x636`** | **`+0x63d`** | **`Ndr64AsyncClientCall`** (`+0x62f LEA RCX,[0x1806c4920]` 프록시 정보 · `+0x62d XOR EDX,EDX` 프로시저 0 · `+0x5bb OR [RSP+0x68],-1` 시간 제한) | [정적] |
| 〃 | `+0x4af` | `+0x4b4` | `AicpAsyncFinishCall` (메시지 펌프하며 기다림 → `RpcAsyncCompleteCall`) | [정적] |
| 〃 | `+0x564` | `+0x569` | `AicpStartAIS` (재시도 — `+0x528 LEA EAX,[RBX-0x6ac]` 비트표) · 셋째 실패 `+0x6be MOV EBX,0x425` | [정적] |
| 〃 | `+0x3c1` · `+0x460` · `+0x6d0` | `+0x3c8` · `+0x467` · `+0x6d7` | `GetAncestor` · `EnableWindow` (창 있을 때) | [정적] |
| 〃 | `+0x4f8` · `+0x518` | `+0x4fd` · `+0x51d` | 오류 때 두 핸들 `CloseHandle` | [정적] |
| 〃 | `+0x741` | `+0x748` | `RpcBindingFree` → `+0x74d MOV EAX,EBX` (반환) | [정적] |
| `AicpCreateBindingHandleEx` | `+0x5c` | `+0x63` | `RpcStringBindingComposeW` (`+0x4e XOR R9D,R9D` 끝점 NULL) | [정적] |
| 〃 | `+0x7a` | `+0x81` | `RpcBindingFromStringBindingW` | [정적] |
| 〃 | `+0xca` | `+0xd1` | `CreateWellKnownSid(22)` (`+0xc7 LEA ECX,[RDX+0x16]`) | [정적] |
| 〃 | `+0x126` | `+0x12d` | `RpcBindingSetAuthInfoExW` (QOS V3 `+0xe5 ~ +0x10a`) | [정적] |
| `appinfo!RAiLaunchAdminProcess` (`0x43070`) | `+0x407` | `+0x40e` | `CheckElevationEnabled` (kernel32 — `RtlQueryElevationFlags & 1`, `objdump`) | [정적] |
| 〃 | `+0x437` | `+0x43c` | `AiGetClientInformation` (`0xac00`) | [정적] |
| 〃 | `+0x44f` · `+0x47d` · `+0x4a6` · `+0x4e4` | `+0x454` · `+0x482` · `+0x4ab` · `+0x4e9` | `LUAGetUACPromptPolicy` · `AiCheckForElevatedUser` · `AiCheckForAdminUser` · `AiConvertWow64Paths` | [정적] |
| 〃 | `+0x580` · `+0x5c4` · `+0xb92` | `+0x587` · `+0x5cb` · `+0xb99` | `RpcImpersonateClient` · `CreateFileW` · `RpcRevertToSelf` | [정적] |
| 〃 | `+0xabc` · `+0xb15` · `+0xb2a` · `+0xb8d` | `+0xac3` · `+0xb1a` · `+0xb2f` · `+0xb92` | `CheckElevation` · `AiCheckSecureApplicationDirectoryEx` · `AiCheckSecureApplicationDirectory` · `AiIsEXESafeToAutoApprove` | [정적] |
| 〃 | `+0xbf0` · `+0xefa` | `+0xbf5` · `+0xf01` | `AiIsElevationAllowedForSession` (→ `0x32`) · `RegisterAppXPackageIfNecessary2` (→ `0x3d35`) | [정적] |
| 〃 | **`+0xe33`** | **`+0xe38`** | **`AiCheckLUA`** (`0x16214`) | [정적] |
| 〃 | **`+0x1096`** | **`+0x109b`** | **`AiLaunchProcess`** (`0x30370`) | [정적] |
| 〃 | `+0x131f` | `+0x1326` | `RpcAsyncCompleteCall` | [정적] |
| `AiGetClientInformation` | `+0x5e` · `+0xa7` | `+0x65` · `+0xae` | `I_RpcBindingInqLocalClientPID` · `NtOpenProcess(0x1004c0)` | [정적] |
| 〃 | `+0xce` · `+0xf5` · `+0x141` · `+0x150` | `+0xd5` · `+0xfc` · `+0x148` · `+0x157` | `RpcImpersonateClient` · `NtOpenThreadToken` · `NtDuplicateToken` · `RpcRevertToSelf` | [정적] |
| 〃 | `+0x229` | `+0x230` | `NtQueryInformationToken` (세션 id) | [정적] |
| `AiCheckLUA` | `+0x3e` · `+0x100` | `+0x43` · `+0x105` | `AipRequireElevationPrompt` · `AiLaunchConsentUI` | [정적] |
| 〃 | (줄) `+0x118` · `+0x121` | — | `TEST byte [RDI],0x10` · **`MOV EBX,0x4c7`** | [정적] |
| 〃 | `+0x16e` · `+0x1af` | `+0x173` · `+0x1b4` | `AiCheckForElevatedUser` · `AiCheckForAdminUser` (실패 `+0x18f MOV EBX,0x2e4`) | [정적] |
| 〃 | `+0x1cf` | `+0x1d6` | `NtSetInformationToken(토큰, 0xc, 호출자 세션)` | [정적] |
| `AiLaunchConsentUI` | `+0x296` · `+0x2aa` · `+0x2d4` | `+0x29b` · `+0x2af` · `+0x2db` | `AiLaunchProcess` (consent.exe) · `AipVerifyConsent` · `WaitForSingleObject(시간 제한)` | [정적] |
| `AiLaunchProcess` (`0x30370`) | `+0x25a` · `+0x5c4` | `+0x261` · `+0x5cb` | `NtDuplicateToken` · `CreateEnvironmentBlock` | [정적] |
| 〃 | `+0x9c7` | `+0x9ce` | `UpdateProcThreadAttribute` (`+0x9ba R8D=0x20000`), `+0x9fb R8D=0x60001` | [정적] |
| 〃 | `+0xeb9` | `+0xec0` | `ImpersonateLoggedOnUser` | [정적] |
| 〃 | **`+0x10a2`** · `+0x12a7` · `+0x138e` | `+0x10a9` · `+0x12ae` · `+0x1395` | **`CreateProcessAsUserW`** (각 앞 `+0x106a` · `+0x126b` · `+0x1356 OR EAX,0x80004`) | [정적] |
| 〃 | `+0x1334` · `+0x141a` | `+0x1339` · `+0x141f` | `AipValidateLaunchedProcess` | [정적] |
| 〃 | **`+0x1499`** · **`+0x14ef`** | `+0x14a0` · `+0x14f6` | **`NtDuplicateObject`** (`+0x1462 AND ESI,0x401` → `+0x1482 AND ECX,2` 옵션) | [정적] |
| 〃 | `+0x1567` | `+0x156e` | `ResumeThread` (`CreationFlags & 4` 가 0 일 때만) | [정적] |
| `appinfo!ServiceMain` | `+0x9e4` | `+0x9eb` | `RpcServerUseProtseqW("ncalrpc", 10, NULL)` | [정적] |
| 〃 | `+0xa33` · `+0xa5b` / `+0xb03` · `+0xb2b` | `+0xa3a` · `+0xa62` / `+0xb0a` · `+0xb32` | `RpcServerRegisterIfEx` · `RpcEpRegisterW` — **다른** 인터페이스 둘 (`0497b57d-…` · `0f738e20-…`) | [정적] |
| 〃 | `+0xaab` | `+0xab0` | 지연 로드 함수가 있는지 묻는 함수 (같은 주소 이름 2개, 후보: `IsAiExtEnableDesktopRpcInterfacePresent` · `IsAiExtDisableDesktopRpcInterfacePresent`) | [정적] |
| 〃 | **`+0xab4`** | **`+0xabb`** | **`AiExtEnableDesktopRpcInterface`** (지연 로드 → `appinfoext.dll` 내보내기 `0x1f50` = `JMP [IAT]` → `appinfo!AiEnableDesktopRpcInterface`, `objdump + pefile`) | [정적] |
| 〃 | `+0xd18` · `+0xd62` | `+0xd1f` · `+0xd69` | `RpcServerUseProtseqEpW` (`AppInfoSandboxLaunch`) · `RpcServerRegisterIf3` | [정적] |
| `appinfo!AiEnableDesktopRpcInterface` (`0x12920`) | `+0x1f` | `+0x26` | `RpcServerInqBindings` | [정적] |
| 〃 | **`+0x56`** | `+0x5d` | **`RpcServerRegisterIfEx(&0x50190, NULL, NULL, 0x29, 0x4d2, NULL)`** (`+0x3b LEA R9D,[RAX+0x29]` · `+0x3f R14D=0x4d2` · `+0x45 LEA RCX`) | [정적] |
| 〃 | **`+0x139`** | `+0x140` | **`RpcEpRegisterW(&0x50190, 바인딩, NULL, L"AppInfo")`** (`+0x12c LEA RCX` · `+0x133 XOR R8D,R8D` · `+0x122 LEA R14,[0x180055280]`) | [정적] (글 `pefile`) |
| `kernelbase!CreateProcessInternalW` | `+0x47ba` | `+0x47c1` | `BaseCheckElevation` (암묵 승격 `0xc000042c` → `0x2e4` 갈래 — runas 길 아님) | [정적] |

- 별칭: 위 windows.storage · appinfo 이름은 `_calls.tsv` 가 `alias:…+1 · ALIASES(1)` 로 적은 것 (PDB 이름 하나 — 무리 아님). 무리는 `ServiceMain +0xaab` 하나 (위 표에 하는 일로 적음, `pdb_aliases.py --lookup appinfo.dll 0x1bf0c` → 2개).

### 1. 부탁 사슬 (원문 1) [정적]

#### 1-1. 썽크에서 windows.storage 로

- shell32 `ShellExecuteExW` 는 6바이트 `JMP [IAT]` 하나다. 지연 로드 슬롯이 처음엔 `DelayLoad_ShellExecuteExW` 를 가리키고, 그것이 `__tailMerge_windows_storage_dll` 로 꼬리 점프해 **`Windows.Storage.dll` 의 `ShellExecuteExW`** 를 채운다 (PE 지연 가져오기 표에 `ShellExecuteExW` · `ShellExecuteW` · `ShellExecuteExA` … 가 `Windows.Storage.dll` 아래, 결과서 `pefile`).
- 그래서 shell32 쪽 일은 **여기서 끝** — 지도도 노드 1 · 간선 0.

#### 1-2. windows.storage 안 — 직접 · 가상 호출 사슬

| # | 함수 (rva) | 넘어가는 줄 | 한 일 |
|---|---|---|---|
| 1 | `ShellExecuteExW` (`0x38680`) | `+0xbc` → 2 | `cbSize ≠ 0x70` 이면 `hInstApp=5` · `SetLastError(5)` · FALSE. `+0xa8 TEST EBP,0x100100` 가 꺼져 있으면 `+0xb0 InRunDllProcess` |
| 2 | `ShellExecuteNormal` (`0x353d08`) | `+0x55` → 3 | `new CShellExecute (0x250)`, 끝 `Finalize` 반환 = `dwErrCode` |
| 3 | `CShellExecute::ExecuteNormal` (`0x17b518`) | `+0x265` → 4 | `+0x140 _Init` (verb → `this+0x60`), `+0x252 SHCoInitialize` (= `CoInitializeEx` — run149: RPCSS 안 감) |
| 4 | `CShellExecute::_DoExecute` (`0x5bf5ac`) | `+0xca` → 5 | 연관 배열 만들기 (`_CreateAssocFromItem` / `_CreateAssocForClassOrKey`), 끝에 오류 보고 · 상자 · `SetEvent` |
| 5 | `CShellExecute::_ExecuteAssoc` (`0x241298`) | `+0xa7` ⇢ `QueryObject(0x2200010, verb, IID_IExecuteCommand)`, `+0x148 _InitCommandExec`, **`+0x175` ⇢ `[+0x48]` → 7** | verb 요소가 `CAssocShellVerbElement::QueryObject` (`0x295fa0` — 그 클래스 가상표 `+0x40`) → `_GetExecute` → `SHCoCreateInstance(CLSID_ExecuteAssociation {1649d1cf-…})` (6) |
| 6 | `_GetExecute` (`0x1ef524`) → kernelbase `SHCoCreateInstance` (`0xe0a20`) | `+0x4a` · `+0x3f` | 지연 로드 `SHCoCreateInstanceWorker` → windows.storage `_SHCoCreateInstance +0xae _CreateFromDllGetClassObject` (자기 `DllGetClassObject`) — **COM 을 안 탈 것** `[추측: 클래스 표 안 읽음]`. 만들어지는 객체: `CExecuteAssociation_CreateInstance +0x66` 의 `CBindAndInvokeStaticVerb` 생성자 (결과서) |
| 7 | `CBindAndInvokeStaticVerb::Execute` (`0x3212d0`) | `+0x18f` → 8 | `CheckFileAccess` · `TryShellExecuteRedirection` · `TryExecuteCommandHandler` (DelegateExecute) · `TryDropTargetHandler` 가 지나가야 함 `[공백: VM 레지스트리]` |
| 8 | `TryCreateProcessDdeHandler` (`0x321750`) → `InitAndCallExecute` (`0x3214ec`) | `+0x3b` 객체 만듦 · `+0xc0` → `+0x214` ⇢ `[+0x48]` → 9 | `new CInvokeCreateProcessVerb (0x348 B)` |
| 9 | **`CInvokeCreateProcessVerb::Execute`** (`0xe1de0`) | `+0x4a` 방법 정하기 · `+0x58` → 10 | 창 → `this+0xc0`, verb 가 "runas" 면 `this+0x1a8 = 2` |
| 10 | `Launch` (`0xe102c`) | `+0x10d` → 11 | 명령 템플릿 (`HKCR\exefile\shell\runas\command` 류 `[추측: 키 경로]`) |
| 11 | `_TryCreateProcess` (`0x17d118`) | `+0x3c` → 12 | `SHRestricted` · UNC 정책, 성공 뒤 `NotifyCreatedProcess` |
| 12 | `_PrepareAndCallCreateProcess` (`0x17d38c`) | `+0x14d` → 13 | 플래그 기본값 `0x4000410`, `+0xdf _FixupWorkingDirectory` (run143 볼륨 길 — `+0x14d` 보다 앞, 사이 갈래 `+0x81` 은 안 따짐) |
| 13 | `CallCreateProcess` (`0x17eb78`) | `+0xc04` → 14 | 아래 1-3 갈림길 |
| 14 | **`AicLaunchAdminProcess`** (`0x331204`) | **`+0x636` `Ndr64AsyncClientCall`** | 아래 2절 |

- **5 → 7 · 8 → 9 의 가상 호출**: 칸 번호 `[+0x48]` 와 가상표 값이 맞는다 (flow-writer `pefile`, "방법"). 그 자리에 오는 객체는 만드는 줄로 이었다 (8 의 `+0x3b`, 6 의 `CExecuteAssociation_CreateInstance +0x66`). `IAssociationArray::QueryObject` 가 `CBindAndInvokeStaticVerb` 를 돌려주는 줄 자체는 `[공백]` (결과서).

#### 1-3. `CallCreateProcess` 갈림길 — 방법 `this+0x1a8` (0 `CreateProcessW` · 1 `CreateProcessAsUserW` · 2 승격)

- `+0x3a` 방법이 2 면 `+0x4a SHTestTokenMembership(NULL, 0x220)` — `SHTestTokenMembership +0x6a AllocateAndInitializeSid` (`S-1-5-32-544`) · `+0xbc CheckTokenMembership(NULL, …)`. **참이면 방법 0** → `+0x1f0 CreateProcessW` (RPC 없음). 그룹이 꺼져 있거나 "거부 전용 (deny-only)" 이라 거짓이어야 승격 쪽으로 간다 — `CheckTokenMembership` 이 켜진 그룹만 센다는 것은 `[추측: SDK 설명, 안 읽음]`.
- 방법 2 면 `+0x136 TEST R14D,R14D` (0 이면 `+0x139 JZ` → `CreateProcessW`) · `+0x13b CMP R14D,1` · **`+0x13f JNZ +0x210`** — 두 호출을 다 건너뛰고 `+0x213 DelegateSwitchToDesktopIfNeeded` 뒤 승격 쪽. 패키지 신분 실행이면 `+0x130 JNZ +0x210` 도 같은 자리.
- 방법 0 이 `0x800702e4` (`ERROR_ELEVATION_REQUIRED` — kernelbase `CreateProcessInternalW +0x47ba BaseCheckElevation` 이 `0xc000042c`) 로 실패해도 같은 승격 쪽으로 온다 — kernelbase 가 끼는 곳은 **이 암묵 승격 갈래** 이고 runas 는 아니다.
- 승격 쪽 첫 문: `this+0x1f8` 값 `0x2` (`+0x835` · `+0x844 TEST`) 가 켜져 있으면 `+0x86c AicCheckAdminApp` · `LUAShouldElevate` 로 "정말 관리자 앱인가" 를 보고 아니면 `0x800702e4` 로 RPC 없이 끝. 이 비트를 누가 세우나는 `[공백]`.
- 그다음 `+0x8f6 CRPCTimeout(5000)` → `+0x930 CoCreateInstance(ImmersiveShell)` (조건 `(this+0x1f8 & 0x10) == 0`) → `+0x9af Disarm` → **`+0xc04 AicLaunchAdminProcess`**.
- RPC 로 보내는 값: `StartFlags = (this+0x1f8 & 0x10) | (이전 오류 ≠ 0x2e4 ? 1 : 0)` 에 `0x40` (ImmersiveSystemMode — COM 이 성공했을 때) · `0x80` · `0x100` · `0x800` 이 조건부, `CreationFlags = [this+0xe4] | 0x80004` (`+0xc4 / +0xcb`).

### 2. RPC (원문 1) [정적] + [정적: PE]

| 칸 | 값 | 근거 |
|---|---|---|
| 인터페이스 | **`201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0**, 문법 NDR 2.0 `8a885d04-…` v2.0 · NDR64 `71710533-…` v1.0 | 클라이언트 rva `0x702b90` (길이 `0x60`, flow-writer `pefile` 같은 값) · 서버 appinfo rva `0x50190` (같은 UUID · 버전) |
| 프로시저 | **0 = `RAiLaunchAdminProcess`**. 서버 표 8칸: 0 `RAiLaunchAdminProcess` · 1 `RAiProcessRunOnce` · 2 `RAiLogonWithSmartCardCreds` · 3 `RAiOverrideDesktopPromptPolicy` · 4 `RAiDisableElevationForSession` · 5 `RAiEnableElevationForSession` · 6 `RAiForceElevationPromptForCOM` · 7 `RAiGetElevatedToken` | `AicLaunchAdminProcess +0x62d XOR EDX,EDX` · 서버 `MIDL_SERVER_INFO` `0x51ae0` → 함수 표 `0x50cd0` · `RPC_DISPATCH_TABLE` `0x52830` (결과서 `pefile`) |
| 호출 꼴 | **비동기** (`Ndr64AsyncClientCall`, 이벤트 알림 `RpcAsyncInitializeHandle(…, 0x70)` + `CreateEventW`), 명시 바인딩, 기다림 = 메시지 펌프 (`AicpMsgWaitForCompletion`) 뒤 `RpcAsyncCompleteCall` | `+0x57b` · `+0x636` · `+0x4af` |
| protseq · 끝점 | `ncalrpc` · **끝점 없음** — `RpcStringBindingComposeW(ObjUuid = L"201ef99a-…", L"ncalrpc", NULL, NULL, NULL)` → `RpcBindingFromStringBindingW` → `RpcBindingSetAuthInfoExW` | `AicpCreateBindingHandleEx +0x5c · +0x7a · +0x126` (글 `0x779b80` · `0x73b130`, flow-writer `pefile`) |
| 인증 | 수준 6 (패킷 프라이버시) · 서비스 10 (WINNT) · 주체 이름 NULL · `RPC_SECURITY_QOS_V3` { 3, MUTUAL_AUTH 1, IdentityTracking 0 (정적), Impersonation 3, **`Sid` = `CreateWellKnownSid(22)` = `S-1-5-18`** } | `+0xc7 ~ +0x12d` |
| 바인딩 캐시 | 전역 `[0x180831100]` 에 한 번 만든 핸들을 `CMPXCHG` 로 두고 다시 씀 (`StartFlags & 0x400` 꺼짐일 때) | `+0x1a3` · `+0x1e4` |
| **`ept_map` 을 묻는 까닭** | 끝점 없는 바인딩 → rpcrt4 가 `\RPC Control\epmapper` 에 **`ept_map_auth_async` (EPM `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0, 프로시저 8 — run145)** 를 보냄. 질문 탑 1층 = `201ef99a` v1.0, **객체 UUID = `201ef99a-…`** (nil 아님), **SID = `S-1-5-18`** (QOS V3 `Sid` 가 `NormalizeServerSid` 를 거쳐 감 `[정적: 줄 둘, 이음은 rpcrt4 몫]`) | rpcrt4 `ResolveEndpoint +0x20c NormalizeServerSid` · `+0x246 GetNextNormalizedSid` (`0x6d9` 면 다음 SID 로 한 번 더) · `LookupNextChunk +0x11d` — 있던 `.asm` (결과서) |
| 서버 등록 (장부 값) | `ServiceMain +0x9e4 RpcServerUseProtseqW("ncalrpc", 10, NULL)` (동적 끝점) → `+0xab4` ⇒ `AiEnableDesktopRpcInterface +0x56 RpcServerRegisterIfEx(201ef99a, NULL, NULL, 0x29, 0x4d2, NULL)` → `+0x139 RpcEpRegisterW(201ef99a, 모든 바인딩, UUID 벡터 NULL → 객체 nil, L"AppInfo")`. 플래그 `0x29` = AUTOLISTEN · ALLOW_SECURE_ONLY · ALLOW_LOCAL_ONLY `[추측: SDK 값 이름]`, 최대 호출 1234, **보안 콜백 없음** | 위 호출 지점 표 (flow-writer 가 `+0x1f ~ +0x139` · 주석 글 `0x55280` 을 다시 봄) |

요청 · 응답 칸 (NDR64 프로시저 형식 rva `0x703700` — 스택 `0x70` · 칸 12 · 상수 버퍼 40 / 104, 결과서 `pefile`. 클라이언트 값은 `AicLaunchAdminProcess +0x58a ~ +0x62f`):

| 칸 | 스택 | 형식 | 클라이언트가 넣는 값 |
|---|---|---|---|
| 바인딩 | `0x08` | explicit handle | 새로 / 전역 캐시 |
| p0 실행 파일 | `0x10` | `[in, unique, string]` | `lpApplicationName` (`.cmd`/`.bat` 면 NULL), `AicpExpandMappedDrive` 로 매핑 드라이브를 풀어 줌 |
| p1 명령줄 | `0x18` | `[in, unique, string]` | `this+0x200` |
| p2 `StartFlags` | `0x20` | int32 | 1-3 끝 + `& ~0x400`, WOW64 리다이렉션이면 `\|8`, 호스트가 포그라운드 권한이면 `\|0x10` (`+0x37f ~ +0x39c`) |
| p3 `CreationFlags` | `0x28` | int32 | **`0x04080414`** = `CREATE_SUSPENDED 0x4` · `CREATE_NEW_CONSOLE 0x10` · `CREATE_UNICODE_ENVIRONMENT 0x400` · `EXTENDED_STARTUPINFO_PRESENT 0x80000` · `CREATE_DEFAULT_ERROR_MODE 0x04000000` (`_PrepareAndCallCreateProcess +0x2b` `0x4000410` + `CallCreateProcess +0xcb` `0x80004`), 잡 이탈 류면 `+0x368 BTS [RDI+0xe4],0x18` |
| p4 현재 폴더 | `0x30` | `[in, string]` | `this+0x50`, 비면 `GetCurrentDirectoryW` |
| p5 창 스테이션\바탕화면 | `0x38` | `[in, string]` | `lpDesktop` 또는 `GetProcessWindowStation` + `\` + `GetThreadDesktop` 이름 (보통 `WinSta0\Default`) |
| p6 시작 정보 | `0x40` | `[in]` 56 B | `STARTUPINFOW` 의 `lpTitle` · 위치 · 크기 · 글자 수 · 색 · `dwFlags` · `wShowWindow` (앞 `0x30` B) + `dwFlags & 0x400` 이면 모니터 정보 8 B |
| p7 창 | `0x48` | int64 `HWND` | `this+0xc0` |
| p8 시간 제한 | `0x50` | int32 | **`0xffffffff`** (`+0x5bb`) — 서버가 consent 기다림에 씀 (`AiLaunchConsentUI +0x2d4`) |
| p9 `PROCESS_INFORMATION` | `0x58` | `[out]` 24 B | `hProcess` 8 · `hThread` 8 · PID 4 · TID 4 (`this+0x180`) |
| p10 `ELEVATION_REASON` | `0x60` | `[out]` int32 | 클라이언트는 안 씀. 서버가 runas (`StartFlags & 1`) 때 **6** 을 넣음 `[정적: 값만, 이름 추측]` |
| 반환 | `0x68` | int32 Win32 오류 | `RpcAsyncCompleteCall` 이 돌려줌 (RPC 오류가 우선) |

- 응답의 두 핸들은 **호출자 프로세스 안 값** (서버가 복제 — 4절).

### 3. 응답 뒤 · 오류 (원문 2) [정적]

#### 3-1. 클라이언트가 하는 일

- **핸들 검사 없음.** 오류 0 일 때만: `+0xc93 PcaMonitorProcess2(hProcess, 1, 실행 파일, 명령줄, 폴더, 2)` (조건 `(this+0x1f8 & 1) || 승격 성공`, 반환 안 씀) → (패키지 앱이면 `RunWithApplicationInstanceManager`) → `+0xdda SetProcessLaunchForegroundPolicy(pid, 4)` (함수가 있고 `this+0x1f8 & 0x20`) → **`+0xde6 TEST byte [R15+0xe4],4` → `+0xdf7 ResumeThread([R15+0x188])`** (실패하면 패키지 앱일 때만 `TerminateProcess`) → `+0xe40 SetLastError`.
- 그 뒤 `_TryCreateProcess +0x79 NotifyCreatedProcess` (DDE 면 `WaitForInputIdle(…, 60000)`) · `+0x85 _FixActivationStealingApps`.
- **`hProcess` 를 돌려주는 조건**: `Finalize +0x30 CMP [RBX+0xa0],0` (오류 0) · `+0x39 TEST byte [RDI+4],0x40` (`SEE_MASK_NOCLOSEPROCESS`) 이면 `+0x3f ~ +0x46` `pExecInfo->hProcess (+0x68) = [this+0xa8]`. **`this+0xa8` 을 누가 채우나는 `[공백]`** (`OnCreated +0x38` ⇢ `[+0x18]` 로 `this+0x98` 을 받고 `+0x55 WaitForInputIdle` 까지만).

#### 3-2. 오류 흐름

- `AicLaunchAdminProcess +0x74d` 반환 E → `CallCreateProcess +0xc0c` (양수면 `+0xc15 | 0x80070000`) → … → `_DoExecute +0x134 _ReportHresult` (Win32 시설이면 아래 16비트를 `this+0xa0`) → `+0x152 _FinalMapError` → `+0x15c _TryErrorMsgBox` → `Finalize +0x59 _FinalMapError(…, &hInstApp)` · **`+0x5e CMP [RBX+0xcd],0` · `+0x65 MOV ECX,0x4c7` · `+0x6f CMOVNZ EAX,ECX`** → `ShellExecuteExW +0x3c SetLastError` · `+0x48` / `+0xee CMP EDI,0x8000000a`.
- **BOOL**: 0 또는 `0x8000000a` 면 TRUE, 그 밖 FALSE.
- **`hInstApp`**: 오류 0 → `this+0x98` (비면 42). 2 · 3 · 5 · 8 그대로. 표 (rva `0x768560`, `_FinalMapError +0xde LEA R9`) 의 `0x20→26` `0x0e→8` `0xa1→3` `0x35→3` `0x94→3` `0x4b3→3` `0x47e→10` `0x47f→12` `0x481→15` `0x480→16` `0x482→20` `0x483→31` `0x484→29` `0x485→32`, **그 밖 5**.
- **`0x4c7` 덮어쓰기**: `_TryErrorMsgBox` 가 오류 ∉ {0, `0x4c7`, `0x8000000a`} · `fMask & 0x400` 꺼짐 · 창이 NULL 이거나 살아 있음이면 (상자 함수가 있으면 띄우고) `this[0xcd] = 1` → `Finalize` 가 반환 오류를 `0x4c7` 로.

| E (서버 / RPC) | 어디서 | NO_UI 켬 → `GetLastError` / BOOL | NO_UI 끔 | `hInstApp` |
|---|---|---|---|---|
| 0 | 성공 | 0 / TRUE | 0 / TRUE | 42 또는 `this+0x98` |
| **`0x4c7`** | consent 가 토큰을 안 줌 · `*param & 0x10` 꺼짐 (`AiCheckLUA +0x118 · +0x121`) | `0x4c7` / FALSE | `0x4c7` / FALSE (상자 없음) | 5 |
| `0x2e4` | 받은 토큰이 승격 아님 (`+0x16e` → `+0x18f`) · 관리자 아님 (`+0x1af`) | `0x2e4` / FALSE | **`0x4c7`** / FALSE | 5 |
| **`0x5`** | 호출자 권한으로 실행 파일 열기 실패 (`RAiLaunchAdminProcess +0x5c4 CreateFileW`) · `AiLaunchProcess` 검사 · `AipValidateLaunchedProcess` | 5 / FALSE | **`0x4c7`** / FALSE | 5 |
| **`0x6f7`** (원문 막힌 자리) | RPC 런타임 오류, 재시도 집합 밖 → 그대로 | `0x6f7` / FALSE | **`0x4c7`** / FALSE | 5 |
| `0x6ac` · `0x6b5` · `0x6ba` · `0x6d9` | `+0x528 ~ +0x56d` 비트표 `0x200000004201` → `AicpStartAIS` (`OpenSCManagerW` → `OpenServiceW(L"AppInfo", 0x14)` → `StartServiceW` (`0x420` 무시) → `QueryServiceStatus` 500 ms × 최대 11) 뒤 **두 번까지** 다시, 셋째 실패 `0x425` (`+0x6be`) | 그 값 / FALSE | `0x4c7` / FALSE | 5 |
| `0x57` · `0x32` · `0x3d35` | 경로 길이 · 세션 승격 금지 (`+0xbf0`) · 패키지 등록 (`+0xefa`) | 그 값 / FALSE | `0x4c7` / FALSE | 5 |

- 원문 로그의 `rax 0` (FALSE) 는 `0x6f7` 줄과 맞는다. `GetLastError` 가 `0x6f7` / `0x4c7` 중 무엇인지는 그 샘플의 `fMask & 0x400` 에 달림 `[공백: 샘플 소스 안 봄]`.
- 서버는 `0x4c7` 을 로그용 HRESULT 로는 0 으로 적지만 RPC 반환값은 `0x4c7` 그대로 (`RAiLaunchAdminProcess` 끝, 결과서 `.c`).

#### 3-3. RPCSS 연결 셋 — 꼭 필요한가: **아니오** `[정적]` (짝은 `[추측]`)

- 이 길의 COM 후보 셋: ① `ExecuteNormal +0x252 SHCoInitialize` = `CoInitializeEx` (run149 — RPCSS 안 감) ② `_GetExecute +0x4a SHCoCreateInstance(CLSID_ExecuteAssociation)` — 자기 `DllGetClassObject` 먼저 (`_SHCoCreateInstance +0xae`), 성공하면 COM 안 탐 `[추측: 클래스 표 안 읽음]` ③ **`CallCreateProcess +0x930 CoCreateInstance(CLSID_ImmersiveShell {c2f03a33-…}, NULL, CLSCTX_LOCAL_SERVER, IID {6d5140c1-…})`** (조건 `(this+0x1f8 & 0x10) == 0`).
- ③ 이 실패해도 `+0x93c TEST EAX,EAX` · `+0x93e JS +0x997` → 결과를 안 쓰고 `+0x9af Disarm` → `+0xc04` 로 간다. 성공했을 때 달라지는 것은 `StartFlags |= 0x40` 하나.
- 원문 로그 순서 (RPCSS bind 거절 ×3 → epmapper bind → `ept_map`) 는 ③ → `+0xc04` 차례와 맞지만, 그 bind 셋이 ③ 에서 났다는 짝은 로그에 호출자가 없어서 `[추측]` (run149 의 "첫 COM 사용 → RPCSS bind 넷" 과 같은 손).

### 4. appinfo 가 자식을 만드는 법 (원문 3) [정적]

#### 4-1. `RAiLaunchAdminProcess` (`0x43070`) 차례

| 단계 | 줄 | 한 일 |
|---|---|---|
| UAC 켜짐 | `+0x407` `CheckElevationEnabled` | 0 이면 승인 없이 **호출자 토큰 사본**으로 곧장 `AiLaunchProcess` (승격 없음) |
| 호출자 | `+0x437` `AiGetClientInformation` | 호출자 PID → **프로세스 핸들 (`0x1004c0` = SYNCHRONIZE · QUERY_INFORMATION · CREATE_PROCESS · DUP_HANDLE)** · 가장 토큰 사본 · 세션 id |
| 정책 | `+0x44f` · `+0x47d` · `+0x4a6` · (`StartFlags & 8`) `+0x4e4` | `LUAGetUACPromptPolicy` 가 읽는 값: `EnableLUA` · `ConsentPromptBehaviorUser` · `ConsentPromptBehaviorAdmin` · `ConsentPromptBehaviorEnhancedAdmin` · `EnableAutomaticAdmin` (`HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System`) |
| 실행 파일 | `+0x580 RpcImpersonateClient` → `+0x5c4 CreateFileW(GENERIC_READ\|EXECUTE, 공유 5, OPEN_EXISTING)` → `+0xabc CheckElevation` · `+0xb15/+0xb2a` 보안 폴더 · `+0xb8d` 자동 승인 → `+0xb92 RpcRevertToSelf` | 호출자 권한으로 열려야 함 (아니면 앱 실행 별칭 갈래 또는 `0x5`) |
| runas 표시 | `StartFlags & 1` → 요구 수준 2 · 플래그 `0x200` · `ELEVATION_REASON = 6` | 비트 `0x10` → `0x800`, `0x80` → `0x100000` |
| 승인 | **`+0xe33 AiCheckLUA`** | `AipRequireElevationPrompt` 가 "필요 없음" 이면 0 · 토큰 없음, 아니면 `AiLaunchConsentUI` (consent.exe `%u %u %p` 를 `AiLaunchProcess` 로 → `AipVerifyConsent` → `ResumeThread` · `WaitForSingleObject(시간 제한)` → 종료 코드 0 이면 consent 가 넘긴 토큰) → 검사 (`0x4c7` · `0x2e4`) → `+0x1cf` 세션 id 를 호출자 것으로 |
| 만들기 | **`+0x1096 AiLaunchProcess`** | 4-2 |
| 끝 | `+0x131f RpcAsyncCompleteCall` | 반환 = `local_678` |

#### 4-2. `AiLaunchProcess` (`0x30370`)

| 항목 | 값 · 방식 | 줄 |
|---|---|---|
| **토큰** | 승인 뒤 받은 **승격 토큰** (승인이 필요 없었으면 호출자 토큰 사본). 플래그 `0x80` 이 꺼져 있으면 `NtDuplicateToken` 으로 한 번 더 복사. 조건: `TokenElevation ≠ 0` · 관리자 · 세션 = 호출자 세션. **토큰 칸 (종류 · 무결성 · 그룹 · 특권) 을 짓는 줄은 appinfo 에 없다** | `+0x25a`, `AiCheckLUA +0x16e · +0x1af · +0x1cf` |
| 토큰 원천 `[추측]` | `LUAGetElevatedToken` (`0x4d9b4`): `TokenElevationType` 이 Limited (3) 면 `TokenLinkedToken` 을 돌려주고, Full (2) 이거나 Default (1) 인데 승격이면 자기, Default 인데 승격 아니면 없음. `RAiGetElevatedToken` (프로시저 7) · `AiGetElevationToken` 이 부름. consent 가 이걸로 토큰을 받는지는 consent.exe 를 안 읽어서 모름 | 결과서 `.c` |
| **부모** | **호출자 프로세스** — `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS (0x20000)` = `AiGetClientInformation` 이 연 핸들 | `+0x9ba` · `+0x9c7` |
| 속성 목록 | 늘: `0x20000` + **`0x60001`** (4 B, 승격 때 `2` 가 켜질 수 있음 `[추측: 비트 뜻]`). 조건부: `0x20008` 패키지 · `0x20012` · `0x20011` `[추측]` · `0x20007` 완화 정책 · `0x20009`/`0x20021` 보안 능력 · `0x2000a` 잡 목록 | `+0x9fb ~ +0xd47` |
| **만드는 호출** | `CreateProcessAsUserW(토큰, exe, 명령줄, NULL, NULL, **FALSE**, **호출자 플래그 \| 0x80004**, 환경, 폴더, `STARTUPINFOEXW` (cb `0x70`), &pi)` — 세 갈래가 같은 인자. 환경은 `CreateEnvironmentBlock(…, 토큰, FALSE)` → `AipHardenEnvironment` → `AipExpandEnvironment`, 앞뒤로 `ImpersonateLoggedOnUser` · `RevertToSelf` | `+0x10a2` · `+0x12a7` · `+0x138e`, `+0x5c4`, `+0xeb9` |
| 만든 뒤 검사 | `AipValidateLaunchedProcess` — `NtQueryInformationProcess(h, 0x2c, …)` 로 연 파일과 맞나, 이미지 파일 옵션 `Debugger` · `AppExecutionAliasRedirect` 가름, 실패 `0x5` | `+0x1334` · `+0x141a` |
| **플래그** | ShellExecute 가 보낸 `0x04080414` — `\| 0x80004` 해도 같다 | |
| **누가 깨우나** | 서버는 `CreationFlags & 4` 가 0 일 때만 `+0x1567 ResumeThread` → ShellExecute 길에선 **안 깨움**. **클라이언트가** `CallCreateProcess +0xdf7` 에서 (호출자의 원래 플래그에 `CREATE_SUSPENDED` 가 없을 때) | `+0x1567`, `CallCreateProcess +0xde6 ~ +0xdfe` |
| **핸들 돌려주기** | `NtDuplicateObject(-1, hProcess, 호출자 프로세스, &out, …)` 뒤 `hThread` 도, 이어 PID · TID. 서버 원본은 `CloseHandle`. 옵션 · 접근은 플래그 `& 0x401` 로 갈림: 0 이면 **옵션 2 (`DUPLICATE_SAME_ACCESS`) · 접근 0**, 아니면 옵션 0 · 접근 `0x20000100` (flow-writer 가 `+0x1462 ~ +0x1495` 로 봄, 비트 뜻 `[추측]`) | `+0x1499` · `+0x14ef` |

### 5. Q276a — windows.storage · shell32 지도 상태 [정적]

- **windows.storage**: `ms/disassemble/windows.storage/_map/ShellExecuteExW_{edges,nodes}.tsv` + `_summary.txt`. 뿌리 `ShellExecuteExW` (`0x38680`), 깊이 10, 공용 도구 기준 20 → **함수 424** (펼침 308 · 공용 104 · 깊이 끝 12), 끊긴 끝 192 (외부 168 · 간접 24). `nodes.tsv` 행 616 (= 함수 424 + 끊긴 끝 192), 간선 1,439.
  - 깊이별 행 수 (함수 + 끊긴 끝) `[1, 8, 25, 41, 101, 91, 104, 96, 90, 41, 18]` — 결과서 값. 함수만 (기준 20) 은 `[1, 7, 20, 34, 69, 54, 80, 66, 56, 25, 12]` (`_summary.txt`).
  - **한계**: 직접 `call` 만 따라서 `_ExecuteAssoc` (깊이 4) 까지 닿고 가상 호출 (`+0x175` · `InitAndCallExecute +0x214`) 에서 끊긴다. **`AicLaunchAdminProcess` · `CInvokeCreateProcessVerb::Execute` 는 지도에 없다** — 1-2 사슬은 `_refs/` (`AicLaunchAdminProcess` → `CallCreateProcess` → … → `0xe102c`) 로 거꾸로 잇고 가상표로 맞췄다.
- **shell32**: `ms/disassemble/shell32/_map/ShellExecuteExW_*` — 노드 1 · 간선 0 (썽크). 여기서 끝.
- **run143 "다음 요청 후보 1" (= Q276 ①)** 은 위 1-2 로 닫혔다.

### 호출 관계

```
shell32!ShellExecuteExW  JMP [IAT] ⇒ (지연 로드) windows.storage                      [정적]
windows.storage!ShellExecuteExW
 └─ +0xc1  ShellExecuteNormal
            ├─ +0x5a  CShellExecute::ExecuteNormal
            │          └─ +0x26a  _DoExecute
            │                      └─ +0xcf  _ExecuteAssoc
            │                                 └─ +0x17a ⇢ [+0x48] CBindAndInvokeStaticVerb::Execute
            │                                             └─ +0x194  TryCreateProcessDdeHandler
            │                                                         └─ +0xc5  InitAndCallExecute
            │                                                                    └─ +0x219 ⇢ [+0x48] CInvokeCreateProcessVerb::Execute
            │      ┌──────────────────────────────────────────────────────────────────────┘
            │      ├─ +0x4f  SetElevationExecMethodIfNeeded   (verb "runas" → [this+0x1a8] = 2)
            │      └─ +0x5d  Launch +0x112 _TryCreateProcess +0x41 _PrepareAndCallCreateProcess
            │                 └─ +0x152  CallCreateProcess
            │                             ├─ +0x4f   SHTestTokenMembership(NULL, 0x220)   참 → 방법 0 → +0x1f7 CreateProcessW (RPC 없음)
            │                             ├─ +0x937  CoCreateInstance(ImmersiveShell, LOCAL_SERVER)   실패 무시
            │                             ├─ +0xc09  AicLaunchAdminProcess
            │                             │           ├─ +0x1c7  AicpCreateBindingHandleEx   ncalrpc · 끝점 없음 · QOS V3 Sid S-1-5-18
            │                             │           ├─ +0x63d  Ndr64AsyncClientCall   201ef99a v1.0 프로시저 0 (비동기)
            │                             │           │           ⇒ (rpcrt4) ept_map_auth_async (EPM e1af8308 프로시저 8)   [run145]
            │                             │           │           ⇒ (svchost) appinfo!RAiLaunchAdminProcess
            │                             │           │                ├─ +0x43c  AiGetClientInformation   호출자 PID · 프로세스 핸들 · 토큰 사본
            │                             │           │                ├─ +0xe38  AiCheckLUA → AiLaunchConsentUI (consent.exe)   0x4c7 · 0x2e4
            │                             │           │                ├─ +0x109b AiLaunchProcess
            │                             │           │                │           ├─ +0x9ce   UpdateProcThreadAttribute(0x20000 = 호출자)
            │                             │           │                │           ├─ +0x10a9  CreateProcessAsUserW(승격 토큰, 0x04080414 | 0x80004)
            │                             │           │                │           └─ +0x14a0 · +0x14f6  NtDuplicateObject → 호출자 핸들 표
            │                             │           │                └─ +0x1326 RpcAsyncCompleteCall
            │                             │           └─ +0x4b4  AicpAsyncFinishCall   (오류 0x6ac·0x6b5·0x6ba·0x6d9 → +0x569 AicpStartAIS, 두 번까지)
            │                             ├─ +0xc9a  PcaMonitorProcess2   반환 안 씀
            │                             └─ +0xdfe  ResumeThread(hThread)   호출자가 CREATE_SUSPENDED 를 안 달라 했을 때
            └─ +0x65  Finalize   (상자 띄웠으면 오류 → 0x4c7) → ShellExecuteExW +0x43 SetLastError
```

- (나무 오프셋은 ret 자리, 괄호는 조건 · 값. `⇢` 는 가상 호출 — 칸 `[+0x48]` 의 대상은 PE 가상표로 확인. `⇒` 는 다른 모듈 (지연 로드) 이나 ALPC 로 다른 프로세스. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (원문 "답이 오면 쓰는 곳" + 결과서 "지금 맞나 · 틀리나")

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` (286줄) · `include/syscall_layer/tokens.hpp` (50줄) 를 읽었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

**원문 "이미 아는 것" 과 지금 파일이 다른 곳 두 군데** (결과서는 원문 인용만 보고 씀):

- 원문 "SID 가 들어 있으면 답을 안 만들어 fault `0x6f7` (`epm.cpp:18`)" → **지금 파일은 다르다.** `:231 ~ :240` `ept_map_auth` 가 (BG04 표시) SID 를 `is_valid_sid` 로 **검사만** 하고 SD 없이 장부 검색으로 넘긴다 (`0x6f7` 은 SID 가 틀릴 때만, `:236`). `:18` 은 지금 `EpmEntry` 칸 줄. 그래서 지금 판에서 이 요청이 막히는 곳은 **장부** — `ifobj_list()` (`:42 ~ :47`) 에 nsi 한 항목뿐이라 `201ef99a` 는 `:116` `ept_s_not_registered` (`0x16c9a0d6`) · 탑 0 으로 간다 `[추측: 코드 읽기, 로그 없음]`. 그러면 rpcrt4 는 다음 정규화 SID 로 한 번 더 묻고 (`ResolveEndpoint +0x246`, 결과서) 끝내 `0x6d9` 면 `AicLaunchAdminProcess` 가 **`AicpStartAIS` (SCM `OpenServiceW("AppInfo")`) 를 두 번까지** 부른다 (`+0x528 ~ +0x56d`) — r19 의 `0x6f7` 꼴과는 다른 막힘이 나올 것 `[추측]`.
- 원문 "에뮬레이터 사용자: **관리자 그룹** · 승격 안 됨 · Medium (`tokens.hpp:35 ~ :41`)" → 지금 `:21 ~ :27` 그룹 목록은 Everyone · Users (`S-1-5-32-545`) · INTERACTIVE · Authenticated Users · 로그온 SID 다섯뿐이고 **Administrators (`S-1-5-32-544`) 가 없다**. `:35 ~ :41` 은 무결성 Medium · 종류 · 세션 1 · `elevation_type = 1` (Default) · `elevated = false`. 그래서 지금은 `SHTestTokenMembership(NULL, 0x220)` 가 **거짓** (그룹이 아예 없음) → RPC 길로 간다 — r19 로그가 `ept_map` 까지 간 것과 맞는다 `[추측: 우리 CheckTokenMembership 길은 안 봄]`.

**원문 "답이 오면 쓰는 곳" 마다 한 줄**:

| 자리 | 쓸 꼴 |
|---|---|
| `epm.cpp` 장부에 그 인터페이스 | 항목 `{ 객체 nil, {201ef99a-7fa0-444c-9399-19ba84f12a1a, 1u << 16} (주 << 16 \| 부 — `:45` 꼴), 주석 "AppInfo", 탑 [{ "ncalrpc", 우리 appinfo 창구 포트 (`LRPC-`+18자) — 2층 NDR 2.0 v2.0 }] }`. 진짜는 서비스가 **시작 때 등록** (`AiEnableDesktopRpcInterface +0x56 · +0x139`) `[정적]` |
| SID 든 요청 받기 | 이 요청의 SID = **`S-1-5-18`**, 객체 UUID = **`201ef99a-…` (nil 아님)**. 장부 객체가 nil 이라 `wild_card_match` (`:91 ~ :95`) 를 그대로 통과 (run145 고르기 규칙 3). 진짜 `AccessCheck` 를 넣을 때는 **등록자 토큰 = 우리 appinfo 창구 = SYSTEM** 이어야 통과 (run145 5절) — 지금 `:105` 는 공사 중이라 영향 없음 `[정적]` + `[추측: 우리 코드]` |
| 새 창구 (승격 요청 → 승인) | `\RPC Control\LRPC-…` 에서 `201ef99a` v1.0 bind (NDR 2.0 v2.0 / NDR64 v1.0) → **프로시저 0, 비동기**. 요청 11칸 (2절 표) 을 읽고 응답 = `PROCESS_INFORMATION` 24 B · `ELEVATION_REASON` (runas 면 6) · 반환 0. 인터페이스가 LOCAL_ONLY · SECURE_ONLY 라 호출자 신분 (인증 6 · 10) 이 있다 `[정적]` · 비트 이름 `[추측]` |
| 엔진 입구로 승격 자식 | `NtCreateUserProcess` 입구로 **부모 = 호출 PID (`0x20000` 속성)**, **토큰 = 승격 토큰**, 플래그는 들어온 `0x04080414` 그대로 (**멈춘 채**), `bInheritHandles = FALSE`. 만든 두 핸들을 **호출자 핸들 표에 `DUPLICATE_SAME_ACCESS` 로 복제**해 응답에. **깨우지 않는다** — 클라이언트 `CallCreateProcess +0xdf7` 가 깨움 `[정적]` |
| 팀원 토큰에 승격 꼴 | (a) **호출자 토큰**: `SHTestTokenMembership(NULL, 0x220)` 가 **거짓**이어야 RPC 길 — 진짜 분리 토큰 꼴이면 Administrators 를 **거부 전용 (deny-only)** 으로 넣고 `elevation_type = 3` (Limited) + **`TokenLinkedToken` = 승격 토큰** 짝 (`LUAGetElevatedToken` 이 Limited → Linked 를 읽음) `[정적: appinfo 줄]` · `[추측: deny-only 속성 값 · 진짜 그룹 꼴 — VM whoami]`. 지금처럼 그룹이 없어도 RPC 길은 탄다. 반대로 그룹을 **켜면** ShellExecute 는 RPC 없이 `CreateProcessW` 로 가 버린다 (`+0x4a` → `+0x53`). (b) **승격 토큰**: `TokenElevation 1` · `elevation_type 2` (Full) · Administrators 켜짐 · 무결성 High · 세션 = 호출자 세션 — appinfo 가 보는 것은 승격 · 관리자 · 세션 셋뿐 (`AiCheckLUA +0x16e · +0x1af · +0x1cf`), 나머지 칸 값은 VM `whoami /all` |
| 원문 iface `0xe1af8308` | **EPM 자신** (`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0, 프로시저 8 = `ept_map_auth_async`, run145). 승격 인터페이스는 그 요청 탑 안의 `201ef99a` |
| RPCSS 연결 셋 | **승격에 꼭 필요하지 않다** — `CoCreateInstance(ImmersiveShell)` 실패를 무시하고 `+0xc04` 로 감 `[정적]`. 그 셋이 이 호출에서 났다는 짝은 `[추측]` → 지금의 거절 그대로 둬도 됨 |
| cmstp COM 승격 (딸림) | **같은 서버 인터페이스 `201ef99a` · 같은 승인 엔진 (`AiCheckLUA` → consent), 다른 프로시저** — 6 `RAiForceElevationPromptForCOM` (`AiBuildCOMParams` → `AipGetTokenForService` → `AiCheckLUA`) · 7 `RAiGetElevatedToken` 은 **프로세스가 아니라 토큰을 돌려준다**. 클라이언트는 windows.storage `AicForceElevationPromptForCOM` (`0x628130`, 파일 작업 쪽) 말고 **rpcss 가 `ole32!CoAicGetTokenForCOM` (지연 로드) 을 부르는 길** 이 따로 있다 (안 읽음). 즉 창구 · 장부는 같이 쓰고 **프로시저 몸 · 응답 꼴은 따로** `[정적]` + `[공백: ole32 쪽]` |

**stub 이 채울 최소 목록** (정적에서 닫힌 만큼, 위에서 아래 순서):

1. EPM 장부에 `201ef99a` v1.0 항목 (객체 nil · `AppInfo`). SID `S-1-5-18` · 객체 `201ef99a-…` 인 `ept_map_auth_async` 에 그 탑을 돌려줌. 없으면 상태 `0x16c9a0d6` · 탑 0 → rpcrt4 다음 SID 재시도 → `AicpStartAIS` (SCM `AppInfo`).
2. appinfo 창구: bind + 프로시저 0 (비동기) 요청 풀기.
3. 응답: 반환 0 · `PROCESS_INFORMATION` (호출자 표 핸들 둘 · PID · TID) · `ELEVATION_REASON` 6.
4. 엔진: 승격 토큰으로 호출자의 자식을 **멈춘 채** 만들기 (깨우기는 클라이언트).
5. 클라이언트 부수 호출이 안 막히게: `PcaMonitorProcess2` (지연 로드 `ext-ms-win-appcompat-pcacli-l1-1-0`, 반환 무시 — **모듈이 없을 때 지연 로드 실패가 예외가 되는지는 안 봄** `[공백]`), `CoCreateInstance(ImmersiveShell)` 실패 허용 (지금처럼), `SetProcessLaunchForegroundPolicy` (있을 때만).
6. 앞 조건: 호출자 토큰 그룹 상태 (Administrators 가 켜져 있지 않을 것), verb `runas` 를 풀 레지스트리 (`HKCR\exefile\shell\runas\command` 류, `DelegateExecute` · `DropTarget` 없음) — r19 가 `ept_map` 까지 갔으니 지금도 통과하는 것으로 보임 `[추측]`.

## 확인된 것

- `[정적]` shell32 `ShellExecuteExW` 는 지연 로드 썽크 한 줄이고, 승격 부탁은 windows.storage `AicLaunchAdminProcess +0x636 Ndr64AsyncClientCall` (프로시저 0) 에서 나간다. 사슬 14단 (1-2 표), 가상 호출 두 칸 (`_ExecuteAssoc +0x175` · `InitAndCallExecute +0x214`, `[+0x48]`) 의 대상은 PE 가상표 값 (flow-writer).
- `[정적]` 승격으로 가려면 verb "runas" (`QueryExists` `StrCmpICW`, 레지스트리 `Elevate` 가 없을 때) 이고 `SHTestTokenMembership(NULL, 0x220)` 가 거짓.
- `[정적: PE]` 인터페이스 `201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0 (클라이언트 `0x702b90` · 서버 `0x50190`), 바인딩 글 객체 UUID = 같은 글 · `ncalrpc` · 끝점 없음, QOS V3 `Sid` = `S-1-5-18`.
- `[정적]` 서버는 `AiEnableDesktopRpcInterface +0x56 · +0x139` 에서 동적 끝점 · 객체 nil · 주석 `AppInfo` 로 EPM 에 등록한다 (`ServiceMain +0xab4` 지연 로드 → `appinfoext` 썽크 → 이 함수).
- `[정적]` 요청 11칸 · 응답 3칸 (2절), `CreationFlags 0x04080414` (`CREATE_SUSPENDED` 포함), 시간 제한 `0xffffffff`.
- `[정적]` 클라이언트는 핸들을 검사하지 않고, 호출자 플래그에 `CREATE_SUSPENDED` 가 없으면 직접 `ResumeThread`. 서버는 이 길에선 안 깨운다.
- `[정적]` `ShellExecuteExW` 는 오류 ≠ 0 · ≠ `0x8000000a` 면 FALSE, 상자 조건이면 `GetLastError` = `0x4c7`, `hInstApp` 은 표 밖 값이면 5. RPC 재시도 집합에 `0x6f7` 은 없다.
- `[정적]` appinfo 는 consent 뒤 토큰을 "승격 · 관리자 · 세션" 으로 검사만 하고, 호출자를 부모 (`0x20000`) 로 `CreateProcessAsUserW(…, FALSE, 플래그 | 0x80004, …)` 한 뒤 핸들 둘을 호출자 핸들 표로 복제한다.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 shell32 · windows.storage 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`) — 이 길은 test.exe 에선 안 생긴다.

## flow-writer 대조 (결과서와 다르게 적은 곳 — 결론 바꾸는 어긋남 없음)

결과서 오프셋을 `.asm` · `pefile` 로 맞춰 봤다 (약 160줄). **결론을 바꾸는 어긋남은 없다.** 범위 시작 하나 · 즉시값 줄 하나 · 지도 숫자 하나를 바꿔 적었고, 보탬 넷:

1. **요청 인자 놓는 범위** — 결과서 (2) 표 "`AicLaunchAdminProcess.asm +0x5a5 ~ +0x62f` 인자 놓기" 의 `+0x5a5` 는 명령 경계가 아니다 (`+0x5a1 LEA RAX,[RSP+0xa8]` · `+0x5a9 MOV [RSP+0x78],RAX`). 이 문서는 바인딩 핸들 고르기 (`+0x58a ~ +0x599`) 부터 쳐서 **`+0x58a ~ +0x62f`** 로 적었다.
2. **`Finalize` 의 `0x4c7`** — 결과서 "`+0x6f CMOVNZ EAX,0x4c7`" → `.asm` 은 **`+0x65 MOV ECX,0x4c7` · `+0x6f CMOVNZ EAX,ECX`** (뜻은 같다).
3. **지도 깊이별 숫자** — 결과서 "깊이별 새 함수 `[1, 8, 25, …, 18]`" 은 `nodes.tsv` 의 **행 수 (함수 + 끊긴 끝, 616)** 를 깊이로 센 값이다 (flow-writer 가 다시 셈). 함수만 (기준 20, 424) 은 `_summary.txt` 의 `[1, 7, 20, 34, 69, 54, 80, 66, 56, 25, 12]`. 이 문서는 둘을 나눠 적었다. 간선은 `edges.tsv` 1,440줄 − 머리 1 = 1,439.
- 보탬 1: **가상 호출 대상 셋을 가상표로 확인** — 5절 "방법" 의 `pefile` 값. 결과서 사슬 5 → 7 · 8 → 9 와 `SetElevationExecMethodIfNeeded +0x2a` ⇢ `QueryExists` 와 맞는다.
- 보탬 2: **서버 등록 줄** — 결과서 (2) 표 "서버 쪽 등록" 은 `AiEnableDesktopRpcInterface` 안 오프셋을 안 적었다. `.asm` 으로 `+0x1f RpcServerInqBindings` · **`+0x56 RpcServerRegisterIfEx`** (`+0x45 LEA RCX,[0x180050190]` — Ghidra 주석은 남의 가상표 이름 `+0xe0` 으로 붙지만 rva `0x50190` 은 `pefile` 로 `201ef99a` v1.0 구조체) · **`+0x139 RpcEpRegisterW`** (`+0x133 XOR R8D,R8D` UUID 벡터 NULL · `+0x122` 주석 글 rva `0x55280` = `AppInfo`). 같은 함수가 다른 인터페이스 셋도 `+0x94` · `+0xce` · `+0x108` / `+0x15f` · `+0x185` · `+0x1ab` 로 등록한다.
- 보탬 3: **`NtDuplicateObject` 옵션 · 접근** — 결과서 "옵션 `ECX = (flags & 0x401) ? 0 : 2`" 는 맞고, 같은 줄 (`+0x1462 AND ESI,0x401` · `+0x1471 SBB EDI,EDI` · `+0x1473 AND EDI,0x20000100` · `+0x1495 MOV [RSP+0x20],EDI`) 로 보면 **`& 0x401` 이 켜진 갈래는 접근 `0x20000100`** 이다 (꺼진 갈래 = 접근 0 · 옵션 2 = 결과서 값). ShellExecute 길에서 이 비트가 켜지는지는 안 봄.
- 보탬 4: **별칭 무리 하나** — `ServiceMain +0xaab` 의 Ghidra 이름 `IsAiExtDisableDesktopRpcInterfacePresent` 는 같은 주소 이름 2개 무리 (`pdb_aliases.py --lookup appinfo.dll 0x1bf0c` → `IsAiExtEnableDesktopRpcInterfacePresent` · `IsAiExtDisableDesktopRpcInterfacePresent`). 바로 뒤 `+0xab4` 가 `AiExtEnableDesktopRpcInterface` 라 "Enable 쪽이 있나" 를 묻는 자리로 읽힌다 `[추측]`.
- 그 밖 결과서 표의 줄 (windows.storage 사슬 · `CallCreateProcess` · `AicLaunchAdminProcess` · `AicpCreateBindingHandleEx` · `ShellExecuteExW` · `Finalize` · `_FinalMapError` · `OnCreated` · kernelbase 둘 · appinfo `RAiLaunchAdminProcess` · `AiCheckLUA` · `AiGetClientInformation` · `AiLaunchProcess` · `AiLaunchConsentUI` · `ServiceMain`) 과 `.rdata` 값 (UUID · 글 넷) 은 모두 결과서와 같았다. PDB guid 넷도 `ms/sym/*.info.txt` 와 같다.

## 앞 run 고쳐 읽기

원문은 그대로 두고 문서 맨 위에 한 줄 넣었다.

- **run143** (`runs/03_sprint/run143-mountdev-name.md`): "`ShellExecute*` → `Execute` 는 가상 함수라 직접 호출로 안 이어진다 `[공백]`" (`:23` · `:208` · 나무 `:274` · `:328`, 맨 위 한 줄을 넣어 지금은 각 +2) → **정적으로 닫힘** — `ShellExecuteExW` → … → `_ExecuteAssoc +0x175` ⇢ `CBindAndInvokeStaticVerb::Execute +0x18f` → `TryCreateProcessDdeHandler +0xc0` → `InitAndCallExecute +0x214` ⇢ `CInvokeCreateProcessVerb::Execute` (가상 칸 `[+0x48]` 의 대상은 가상표 값). 그 뒤는 run143 사슬 그대로 (`Launch +0x10d` → `_TryCreateProcess +0x3c` → `_PrepareAndCallCreateProcess +0xdf _FixupWorkingDirectory`). "실제로 불리나" (Q275 5) 는 그대로 동적.
- 틀린 줄은 없다 — run143 이 `[공백]` · `[추측]` 으로 둔 줄이 채워졌을 뿐이다. run143 해설서는 손대지 않았다 (해설서는 근거 문서가 아님).

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** test.exe 초기화 밖 (shell32 · windows.storage 안 올림, appinfo 는 다른 프로세스) 이다.

## 아직 확인 필요

동적 (결과서 "VM 으로 확인할 것 — 디버거 필요" — Q280 · Q283 RPC 묶음에 얹을 꼴, 멈춤점은 test.exe 가 아니라 `ShellExecuteExW` 를 부르는 프로세스 / svchost (appinfo) 조건):

1. **D1 요청 스텁** — `windows_storage!AicLaunchAdminProcess+0x636` (call 직전) `[rsp+0x20 ~ +0x78]` 12칸 · `rcx` `0x1806c4920` · `edx` 0. `StartFlags` · `0x04080414` · `-1`.
2. **D2 `ept_map_auth_async` 요청** — SID `01 01 00 00 00 00 00 05 12 00 00 00` 인가 · 객체 UUID · 탑. `rpcepmap!ept_map_auth` 입구 (run145) 또는 rpcrt4 `LookupNextChunk+0x11d` 직전.
3. **D3 응답 스텁** — `appinfo!RAiLaunchAdminProcess+0x131f` (call 직전) · `windows_storage!AicLaunchAdminProcess+0x4b4` `eax`.
4. **D4 방법 값** — `CInvokeCreateProcessVerb::Execute+0x4f` 뒤 `dd @rbx+0x1a8 L1` (2 인가), `CallCreateProcess+0x4f` `eax` (0 이어야 RPC 길).
5. **D5 COM** — `CallCreateProcess+0x937` `eax` (원문의 RPCSS bind 셋과 같은 호출인가 — 3-3 `[추측]` 짝).
6. **D6 진짜 승격 토큰** — `appinfo!AiLaunchProcess+0x10a2` `rcx` → `!token` (종류 · 연결 · 무결성 · 그룹 · 특권), 대조로 `AiGetClientInformation+0x148` 뒤 호출자 토큰 사본.
7. **D7 돌려받은 핸들** — `CallCreateProcess+0xdf7` `rcx` (hThread) 가 호출자 표 안 값인지.
8. **D8 창 갈래** — `AicLaunchAdminProcess+0x390` (`HostProcessHasForegroundRight`) · `+0x3c1` (hwnd ≠ NULL 일 때).

디버거 없이 (VM 에서 떠 올 것):

- 승격된 창 · 승격 전 창 `whoami /all` 한 번씩 (Administrators 가 "Group used for deny only" 인가 — D4 와 같은 뜻, 승격 쪽 High · 특권 목록).
- UAC 레지스트리 `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` 의 `EnableLUA` · `ConsentPromptBehaviorAdmin` · `ConsentPromptBehaviorUser` · `ConsentPromptBehaviorEnhancedAdmin` · `EnableAutomaticAdmin` (+ `PromptOnSecureDesktop` `[추측]`).
- verb 키 `reg query HKCR\exefile\shell\runas /s` · `HKCR\exefile\shell\open /s` (`DelegateExecute` · `DropTarget` 없음 확인).
- 서비스 `sc qc appinfo` · `sc query appinfo` · `HKLM\SYSTEM\CurrentControlSet\Services\Appinfo` (`ObjectName` · `ServiceDll`).
- 원문 샘플이 `fMask` 에 `SEE_MASK_FLAG_NO_UI (0x400)` 를 쓰는지 (3-2 의 `GetLastError` 갈림) — PE_emulator 쪽 표본에서.

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. windows.storage — `CShellExecute+0xa8` · `+0x98` 를 쓰는 곳 (`hProcess` · `hInstApp` 값, 3-1 `[공백]`).
2. windows.storage — `DllGetClassObject` 클래스 표에 `CLSID_ExecuteAssociation` 이 있나 + `_CreateAssocFromItem` 이 `HKCR\exefile\shell\runas` 를 읽는 줄 ("COM 없이 verb 가 풀리나").
3. kernel32 — `BaseCheckElevation` · `CheckElevation` · `CheckElevationEnabled` (암묵 승격 갈래).
4. consent.exe 새 analyze — 토큰이 만들어지는 곳 (D6 의 정적 쪽, 4-2 `[추측]` 원천).
5. rpcrt4 — `ResolveEndpoint` SID 재시도 횟수 · 끝 오류, QOS `Sid` → `ept_map_auth_async` SID 인자 이음 (sast/154 뒤).
6. appinfo — `AiCheckForAdminUser` · `AipRequireElevationPrompt` 플래그 표 (`0x10` · `0x20` · `0x40` · `0x200` …) 한 장.
- 그 밖 `[공백]`: `this+0x1f8` 비트 (`0x1` · `0x2` · `0x10` · `0x20`) 의 출처, NDR 문법 중 무엇으로 협상되나, `PcaMonitorProcess2` 안 (또 RPC 를 거나), `ole32!CoAicGetTokenForCOM` 가 부르는 프로시저 (cmstp 쪽), api 집합 `ext-ms-win-shell32-shellcom-l1-1-0` · `ext-ms-win-appcompat-pcacli-l1-1-0` 의 실제 DLL.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: appinfo `AipValidateLaunchedProcess` 가 이미지 파일 옵션 `Debugger` 값으로 갈래를 가른다 `[정적]` — VM 에서 IFEO `Debugger` 로 붙이는 방법을 쓰면 이 검사 길이 달라진다. 커널 디버거 (KDNET) 로 보는 건 이 값과 상관없다 `[추측]`. 또 consent 창을 기다리는 시간 제한이 `0xffffffff` 라 승인 창 앞에서 멈춤점으로 오래 붙잡아도 시간 초과는 없다 `[정적: 값]`.
- **읽을 때 조심** (하네스 제안 거리 아님, 기록만): Ghidra 가 `.rdata` 구조체 자리 (`0x180050190`) 에 남의 가상표 이름 `+0xe0` 주석을 붙인다 — 구조체 정체는 `pefile` 로 본다. 지도 `_summary.txt` 의 "깊이별 새 함수" 와 `nodes.tsv` 행 수는 세는 대상이 다르다 (끊긴 끝 포함 여부).
- **도구**: flow-writer 의 `pefile` 스크래치는 레포 밖, `python3 -I` 로는 사용자 패키지의 `pefile` 를 못 찾아 빼고 돌렸다 (run150 · run155 와 같은 까닭, 스크립트 폴더는 내 파일만). `pdb_aliases.py appinfo.dll` 로 `ms/sym/appinfo.dll.aliases.tsv` 를 처음 지었다 (커밋 안 되는 폴더). 스크립트 문제 없음 (결과서).
