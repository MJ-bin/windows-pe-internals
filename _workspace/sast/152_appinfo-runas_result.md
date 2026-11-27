# 152 appinfo-runas — 정적 분석 결과

- 요청서: `_workspace/sast/152_appinfo-runas.md` (PE_emulator 원문 `374_internals-appinfo-runas.md` 읽기만. 대기열 Q290 + Q276a)
- 날짜: 2026-11-27 · 빌드 26100.9457 x64 `System32/`
- **열어 본 프로젝트**: `windows.storage` · `shell32` · `appinfo` (셋 다 이번에 처음 analyze) · `kernelbase` (있던 것, `SHCoCreateInstance` 한 함수만 뽑음). 다른 프로젝트 (rpcrt4 · combase · rpcss · kernel32 · ole32) 는 **Ghidra 로 열지 않았다**. 있던 `.asm`/`_index.tsv` 를 글로 읽거나 `pefile` · `objdump` (Ghidra 아님) 로만 봤다.
- 스크립트 문제: 없음.

## PDB GUID 확인 (셋 + kernelbase)

| 모듈 | `ms/sym/<이름>.pdb.info.txt` guid+age | PDB 안 (`llvm-pdbutil-14 dump --summary`) | 판정 |
|---|---|---|---|
| shell32.dll | `011DE059CF3D0B8174FA9B5324EA76541` | `{011DE059-CF3D-0B81-74FA-9B5324EA7654}` Age 2 | 같음 (새 줄) |
| windows.storage.dll (`Windows.Storage.pdb`) | `EF42E52BBA15CEFDA22CB070B2FB69881` | `{EF42E52B-BA15-CEFD-A22C-B070B2FB6988}` Age 2 | 같음 (새 줄) |
| appinfo.dll | `9A9DD55CF57F3F72CCF874887B92209C1` | `{9A9DD55C-F57F-3F72-CCF8-74887B92209C}` Age 3 | 같음 (새 줄) |
| KernelBase.dll | `72CCE506944DE0267C172AD64287F9BD1` | — | run149 와 같음 |

- 바이너리 쪽 Age 는 1, PDB 쪽 Age 는 2~3 (rpcss · RpcEpMap 때와 같은 모양). 셋 다 `SetPdb:` 줄이 analyze 로그에 있고 `FUN_` 이름은 shell32 5개 · windows.storage 13개 · appinfo 0개 (함수 수 34,079 · 43,911 · 1,387).
- analyze 는 하나씩 돌림 (`MAXMEM` shell32 12G → windows.storage 12G → appinfo 8G, 시작 때 `free` available 29G / 26G). 걸린 시간: shell32 약 2분 30초 · windows.storage 약 3분 · appinfo 1분 안쪽. 메모리 모자란 일 없음.

## 버전 확인 (알려진 사실 표에 이 모듈 줄은 없음 → 다른 근거 셋)

1. sast/140 이 적은 `CInvokeCreateProcessVerb::Execute` windows.storage rva `0xe1de0` — 새 `Execute__e1de0.asm` 첫 줄 `rva=0xe1de0` · PDB 이름 `CInvokeCreateProcessVerb::Execute` 로 **일치**.
2. 이 결과서의 사슬 직접 호출 24줄을 **원본 바이트** (`pefile`, 함수 시작 rva + call 오프셋) 로 다시 계산: `E8` 의 목적지가 전부 기대한 함수 시작과 맞음 (예: `CallCreateProcess +0xc04 → 0x331204`, `ShellExecuteExW +0xbc → 0x353d08`). `call [IAT]` 는 `48 FF 15` (REX 포함 7바이트, ret = call + 7) — **일치**.
3. KernelBase 줄 (`CreateProcessInternalW +0x47ba` → `BaseCheckElevation`) 은 있던 `_calls.tsv` 와 새 `.asm` 이 같음.

## 먼저 읽는 한 줄 답

1. **부탁하는 쪽**: 사슬은 **windows.storage 안에서 끝난다** (shell32 의 `ShellExecuteExW` 는 `JMP [IAT]` 한 줄짜리 지연 로드 썽크). `windows.storage!ShellExecuteExW` → `ShellExecuteNormal` → `CShellExecute::ExecuteNormal` → `_DoExecute` → `_ExecuteAssoc` → (가상 호출) `CBindAndInvokeStaticVerb::Execute` → `TryCreateProcessDdeHandler` → (가상 호출) `CInvokeCreateProcessVerb::Execute` → `Launch` → `_TryCreateProcess` → `_PrepareAndCallCreateProcess` → `CallCreateProcess` → **`AicLaunchAdminProcess` (rva `0x331204`) `+0x636` `Ndr64AsyncClientCall`** (프로시저 **0**, 비동기). verb `runas` 는 `CAssocShellVerbElement::QueryExists(0x2020011)` 가 "runas" 글자 비교로 참을 줘서 `CreateProcessMethod = 2` (승격) 가 되고, 그러면 **첫 `CreateProcessW` 를 건너뛰고** 곧장 RPC 로 간다.
   - 인터페이스 UUID `201ef99a-7fa0-444c-9399-19ba84f12a1a` **v1.0**, 프로시저 0 = `RAiLaunchAdminProcess` (서버 표 `appinfo` 로 확인), protseq `ncalrpc`, **끝점 = 동적** (바인딩 글에 끝점 없음 → rpcrt4 가 `ept_map` 을 물음), 바인딩 글에 객체 UUID 가 **인터페이스 UUID 와 같은 글**.
   - **원문 "막힌 자리" 의 `0xe1af8308…` 는 이 인터페이스가 아니다.** `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 은 **EPM 자신** (run145: 프로시저 8 = `ept_map_auth_async`). 그 요청 안의 탑이 `201ef99a` 를 묻는다.
   - **SID 를 넣는 까닭**: `AicpCreateBindingHandleEx` 가 `RpcBindingSetAuthInfoExW` 에 `RPC_SECURITY_QOS_V3` 를 주는데 `Sid` 칸 = `CreateWellKnownSid(22)` = **LocalSystem `S-1-5-18`** (서버가 SYSTEM 이어야 한다는 요구). 인증 수준 6 (패킷 프라이버시) · 서비스 10 (WINNT) · 가장 3.
   - 요청 12칸 · 응답 3칸은 아래 (2) 표.
2. **응답 뒤**: 클라이언트는 핸들 검사를 **안 한다** — 반환 오류가 0 이면 곧바로 `PcaMonitorProcess2` (결과 안 씀) → `ResumeThread(hThread)` (호출자가 `CREATE_SUSPENDED` 를 안 달라 했을 때) → `SetLastError`. 오류 값은 `HRESULT_FROM_WIN32` 로 바꿔 `CShellExecute+0xa0` 에 담고 `ShellExecuteExW` 가 **오류 ≠ 0 이고 ≠ `0x8000000a` 이면 FALSE**. 메시지 상자 조건이 맞으면 (`fMask & SEE_MASK_FLAG_NO_UI(0x400)` 가 꺼져 있고 오류가 `0x4c7` 아님) **`GetLastError` 는 원래 오류가 아니라 `0x4c7` 로 바뀐다**. `hInstApp` 은 `0x4c7` · `0x6f7` · `0x5` 모두 **5**. 표는 (3). **RPCSS 연결 셋 (`0xe60c73e6` bind 거절) 은 승격 길에 꼭 필요하지 않다** [정적]: 이 길의 진짜 COM 호출은 `CallCreateProcess +0x930` 의 `CoCreateInstance(CLSID_ImmersiveShell, 로컬 서버)` 뿐이고 실패하면 결과를 안 쓰고 계속한다. `CLSID_ExecuteAssociation` 은 windows.storage 안 `DllGetClassObject` 로 먼저 만들어서 COM 을 안 탄다 `[추측: 클래스 표 안 읽음]`.
3. **들어주는 쪽 (appinfo `RAiLaunchAdminProcess`)**: 토큰 = 승인 (`AiCheckLUA` → `AiLaunchConsentUI` → consent.exe) 뒤 **돌아온 승격 토큰** (없으면 호출자 토큰 사본), 조건: `TokenElevation ≠ 0` 이고 관리자여야 하며 아니면 **`0x2e4`**, 세션 id 를 호출자 것으로 고침. 부모 = **호출자 프로세스** (`I_RpcBindingInqLocalClientPID` → `NtOpenProcess(0x1004c0)` → 속성 `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS 0x20000`). 만들기 = `CreateProcessAsUserW(token, exe, cmd, NULL, NULL, FALSE, 호출자 플래그 | 0x80004, 환경, 폴더, STARTUPINFOEX, &pi)`. ShellExecute 가 보내는 플래그는 `0x04080414` (**`CREATE_SUSPENDED` 포함**) → 서버는 깨우지 않고 **클라이언트가 `ResumeThread`**. 서버는 `NtDuplicateObject(…, 호출자 프로세스, DUPLICATE_SAME_ACCESS)` 로 `hProcess`·`hThread` 를 호출자 핸들 표에 복제해 돌려준다.

## (1) 부탁 사슬 표 — `ShellExecuteExW` (verb `runas`) → RPC 부르는 줄

`call/ret` 는 `.asm` 의 `CALL … ret=` 줄. 근거 파일은 모두 `ms/disassemble/<모듈>/<함수>.asm` (windows.storage 쪽 같은 이름 `.c` 는 `ms/decomp/windows.storage/`). 이름이 겹치는 것은 `__<rva>` 꼴 (`Execute__e1de0`, `Execute__3212d0`, `Launch__e102c`, `_InitCommandExec__24145c`).

| # | 모듈 | 함수 (rva) | call / ret | 부르는 것 · 한 일 | 근거 |
|---|---|---|---|---|---|
| 0 | shell32 | `ShellExecuteExW` (`0x2013f0`, 6바이트 썽크) | `+0x0 JMP [0x18077a3e0]` | 지연 로드 슬롯 `__imp_ShellExecuteExW` → `DelayLoad_ShellExecuteExW` `+0x7 TAILJMP __tailMerge_windows_storage_dll` → **`Windows.Storage.dll` 의 `ShellExecuteExW`** (PE 지연 가져오기 표에 `ShellExecuteExW` · `ShellExecuteW` · `ShellExecuteExA` … 가 `Windows.Storage.dll` 아래) | `shell32/ShellExecuteExW.asm`, `DelayLoad_ShellExecuteExW.asm` `[정적]` |
| 1 | windows.storage | `ShellExecuteExW` (`0x38680`) | `+0xbc / +0xc1` | `ShellExecuteNormal`. 앞뒤: `+0x2e CMP [RCX],0x70` (cbSize 틀리면 `hInstApp=5`, `SetLastError(5)`, FALSE), `+0x9f StartActivity`, `+0xb0 InRunDllProcess`, 끝 `+0x3c SetLastError(EDI)` | `windows.storage/ShellExecuteExW.asm` |
| 2 | " | `ShellExecuteNormal` (`0x353d08`) | `+0x2a/+0x2f` `operator new(0x250)`, `+0x42/+0x47` `CShellExecute` 생성자, **`+0x55 / +0x5a` `CShellExecute::ExecuteNormal`**, `+0x60 / +0x65` `Finalize` | 반환 `EAX = Finalize` 의 값 (`+0x68 MOV EBX,EAX`) = `ShellExecuteExW` 의 `dwErrCode` | `ShellExecuteNormal.asm` |
| 3 | " | `CShellExecute::ExecuteNormal` (`0x17b518`) | `+0x140/+0x145` `_Init` (`lpVerb` 를 `this+0x60` 에 복사 — `_Init__17b990.c`), `+0x11a` `TryShellExecuteHooks`, `+0x199` `SetFile`, `+0x252/+0x257` `SHCoInitialize`, **`+0x265 / +0x26a` `_DoExecute`** | `SHCoInitialize` = `CoInitializeEx` 만 (run149: 이것은 RPCSS 안 감) | `ExecuteNormal.asm` |
| 4 | " | `CShellExecute::_DoExecute` (`0x5bf5ac`) | `+0xca/+0xcf` **`_ExecuteAssoc`** (앞에 `+0x105/+0x10a` `_CreateAssocFromItem` 또는 `+0xb8/+0xbd` `_CreateAssocForClassOrKey`), 끝 `+0x134 _ReportHresult`, `+0x152 _FinalMapError`, `+0x15c _TryErrorMsgBox`, `+0x16d SetEvent` | | `_DoExecute.asm`, `.c` |
| 5 | " | `CShellExecute::_ExecuteAssoc` (`0x241298`) | `+0x91/+0x98/+0x9c` 인자 (IID `7f9185b0-…` = `IExecuteCommand`, 글자 = `this+0x60` = verb, 질문 `0x2200010`) → **`+0xa7/+0xac` `IAssociationArray::QueryObject` (가상)**, `+0x148/+0x14d` `_InitCommandExec`, **`+0x175/+0x17a` `IExecuteCommand::Execute` (가상, 슬롯 `[+0x48]`)** | `QueryObject` 가 verb 요소 `CAssocShellVerbElement::QueryObject(0x2200010)` 로 가서 `_GetExecute` → `SHCoCreateInstance(CLSID_ExecuteAssociation {1649d1cf-deaf-4a68-abe8-5c9f68572fd1})` (아래 6) | `_ExecuteAssoc.asm`, `QueryObject__295fa0.c`, `_GetExecute.c` |
| 6 | windows.storage (+ kernelbase) | `CAssocShellVerbElement::_GetExecute` (`0x1ef524`) | `+0x4a/+0x51` `SHCoCreateInstance` | **kernelbase `SHCoCreateInstance` (`0xe0a20`)** `+0x3f/+0x46` → 지연 로드 `SHCoCreateInstanceWorker` (api `ext-ms-win-shell32-shellcom-l1-1-0` → shell32 의 내보내기 `0x71ce3a` 가 포워드 → windows.storage `SHCoCreateInstanceWorker` `0x114b40`) → `_SHCoCreateInstance` `+0xae` **`_CreateFromDllGetClassObject`** (windows.storage 자기 `DllGetClassObject`). 성공하면 COM 을 안 탄다 | `kernelbase/SHCoCreateInstance.asm`, `SHCoCreateInstanceWorker.c`, `_SHCoCreateInstance.c` `[정적]` · api 집합 → 모듈 `[추측]` |
| 7 | " | `CBindAndInvokeStaticVerb::Execute` (`0x3212d0`, `Execute__3212d0`) | `+0xa6 CheckFileAccess`, `+0xbb TryShellExecuteRedirection`, `+0x16f TryExecuteCommandHandler` (DelegateExecute), `+0x17c TryDropTargetHandler`, **`+0x18f/+0x194` (그리고 `+0x1cb/+0x1d0`) `TryCreateProcessDdeHandler`** | 앞의 처리기들이 "내 일 아님" 으로 지나가야 이 줄에 닿는다 (레지스트리에 `DelegateExecute` · `DropTarget` 이 없어야) `[공백: VM 레지스트리]` | `Execute__3212d0.asm` |
| 8 | " | `TryCreateProcessDdeHandler` (`0x321750`) | `+0x3b/+0x40` `CreateInvokeCreateProcessVerb` (`new CInvokeCreateProcessVerb`, 0x348 바이트), **`+0xc0/+0xc5` `InitAndCallExecute`** | `InitAndCallExecute` (`0x3214ec`) `+0x214/+0x219` **`[RAX+0x48]` = `IExecuteCommand::Execute` (가상)** → 9 | `TryCreateProcessDdeHandler.asm`, `InitAndCallExecute.asm` |
| 9 | " | `CInvokeCreateProcessVerb::Execute` (`0xe1de0`) | `+0x35` `IUnknown_GetParentWindow` (→ `this+0xc0` = 창), **`+0x4a/+0x4f` `SetElevationExecMethodIfNeeded`**, **`+0x58/+0x5d` `Launch`** | `SetElevationExecMethodIfNeeded` (`0xe2128`): `+0x19 MOV EDX,0x2020011`, `+0x2a` 가상 `QueryExists`, 성공이면 **`+0x7e MOV [RBX+0x1a8],2`** (= `CreateProcessMethod` 승격). `CAssocShellVerbElement::QueryExists` (`0x210150`) `+0x42 LEA RDX,L"runas"` · `+0x49/+0x50 StrCmpICW` → **verb 가 "runas" 면 참** (먼저 레지스트리 값 `Elevate` 를 묻고 없을 때만 이 글자 비교) | `Execute__e1de0.asm`, `SetElevationExecMethodIfNeeded.asm`, `QueryExists__210150.asm`, 값 이름표 `s_rgqkvVerb` (`0x2020011 → "Elevate"`) |
| 10 | " | `CInvokeCreateProcessVerb::Launch` (`0xe102c`) | `+0x21 SetCommandTemplate`, `+0x34 ProcessCommandTemplate`, **`+0x10d/+0x112` `_TryCreateProcess`** | | `Launch__e102c.asm` |
| 11 | " | `_TryCreateProcess` (`0x17d118`) | `+0x2e _PolicyAllowsCreateProcess`, **`+0x3c/+0x41` `_PrepareAndCallCreateProcess`**, 성공 뒤 `+0x79 NotifyCreatedProcess`, `+0x85 _FixActivationStealingApps` | `_PolicyAllowsCreateProcess`: `SHRestricted(RESTRICTRUN/DISALLOWRUN)` · UNC 검사 | `_TryCreateProcess.asm` |
| 12 | " | `_PrepareAndCallCreateProcess` (`0x17d38c`) | **`+0x2b MOV [RDI+0xe4],0x4000410`** (만들기 플래그 기본값), `+0x13c NotifyBeforeCreateProcess`, **`+0x14d/+0x152` `CallCreateProcess`**, 실패 때 `+0x45c InvokeAppActivationFailedHandler` | | `_PrepareAndCallCreateProcess.asm` |
| 13 | " | `CallCreateProcess` (`0x17eb78`) | **`+0x4a/+0x4f` `SHTestTokenMembership(NULL, 0x220)`**, `+0xc4/+0xcb` 플래그 `= [this+0xe4] \| 0x80004`, (방법 0/1 만) `+0x19b CreateProcessAsUserW` · `+0x1f0 CreateProcessW`, `+0x86c AicCheckAdminApp`, `+0x8f6 CRPCTimeout(5000)`, `+0x930/+0x937` `CoCreateInstance(ImmersiveShell)`, **`+0xc04/+0xc09` `AicLaunchAdminProcess`**, `+0xc93 PcaMonitorProcess2`, `+0xdf7/+0xdfe ResumeThread`, `+0xe40 SetLastError` | 아래 "CallCreateProcess 갈림길" | `CallCreateProcess.asm` |
| 14 | " | `AicLaunchAdminProcess` (`0x331204`) | `+0x1c2/+0x1c7` `AicpCreateBindingHandleEx`, `+0x57b AicpAsyncInitializeHandle`, **`+0x636/+0x63d` `Ndr64AsyncClientCall`**, `+0x4af AicpAsyncFinishCall`, `+0x564 AicpStartAIS`, `+0x741 RpcBindingFree` | 아래 (2) | `AicLaunchAdminProcess.asm` |

**CallCreateProcess 갈림길** (`this+0x1a8` = 방법 0 = `CreateProcessW` / 1 = `CreateProcessAsUserW` / 2 = 승격) `[정적]`:
- `+0x4a SHTestTokenMembership(NULL, 0x220)` — `S-1-5-32-544` (BUILTIN\Administrators) 를 `CheckTokenMembership(NULL, …)` 로 묻는다 (`SHTestTokenMembership.asm +0x6a AllocateAndInitializeSid`, `+0xbc CheckTokenMembership`). **방법 2 이고 참이면 방법을 0 으로 낮춘다 → 승격 RPC 없이 그냥 `CreateProcessW`.** 그룹이 "사용 안 함 (deny-only)" 이어야 길이 이어진다.
- 방법 2 면 첫 `CreateProcess*` 를 건너뛰고 (`uVar11 = 0`) 승격 쪽으로: `+0x136 TEST R14D,R14D` (방법) 가 0 이면 `CreateProcessW` (`+0x1f0`), `+0x13b CMP R14D,1 / +0x13f JNZ → +0x210` 로 1 이 아니면 (= 2) 두 호출을 다 건너뛰고 `DelegateSwitchToDesktopIfNeeded` (`+0x213`) 로. 패키지 신분 실행이면 (`+0x12e TEST AL,AL / +0x130 JNZ → +0x210`) 이것도 건너뜀. 방법 0 이 `0x800702e4` (= `ERROR_ELEVATION_REQUIRED`, kernelbase `CreateProcessInternalW +0x47ba/+0x47c1` `BaseCheckElevation` → `0xc000042c`) 로 실패해도 같은 승격 쪽으로 온다 — **이쪽이 KernelBase 가 사슬에 끼는 곳** (runas 는 아님).
- 승격 쪽 첫 문: `this+0x1f8` 의 비트 2 가 꺼져 있으면 곧장 RPC, 켜져 있으면 `AicCheckAdminApp` · `LUAShouldElevate` 로 "정말 관리자 앱인가" 를 먼저 보고 아니면 `0x800702e4` 로 RPC 없이 끝 (`+0x86c`, `+0x899`, `+0x8ab`) `[공백: 이 비트를 누가 세우나]`.
- RPC 로 보내는 값: `StartFlags = (this+0x1f8 & 0x10) | (이전 오류가 0x2e4 아님 ? 1 : 0)` 에 `0x40` (ImmersiveSystemMode) · `0x80` · `0x100` · `0x800` 가 조건부로 얹힘, `CreationFlags = [this+0xe4] \| 0x80004`.

## (2) RPC 표

| 칸 | 값 | 근거 |
|---|---|---|
| 인터페이스 UUID · 버전 | `201ef99a-7fa0-444c-9399-19ba84f12a1a` v**1.0**. 전송 문법 두 개: NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0 · NDR64 `71710533-beba-4937-8319-b5dbef9ccc36` v1.0 | windows.storage 클라이언트 구조체 rva `0x702b90` (길이 `0x60`), 증명서 `MIDL_SYNTAX_INFO` rva `0x702c90`; appinfo 서버 구조체 rva `0x50190` (같은 UUID · 버전 · 문법). 이 UUID 를 가진 파일: windows.storage · appinfo 외에 pcaui · msra · winlogon · lsm · explorer(SysWOW64) 등 (바이트 검색) |
| 프로시저 | **0 = `RAiLaunchAdminProcess`** (서버 표: 0 `RAiLaunchAdminProcess`, 1 `RAiProcessRunOnce`, 2 `RAiLogonWithSmartCardCreds`, 3 `RAiOverrideDesktopPromptPolicy`, 4 `RAiDisableElevationForSession`, 5 `RAiEnableElevationForSession`, 6 `RAiForceElevationPromptForCOM`, 7 `RAiGetElevatedToken`). 클라이언트 쪽 `AicLaunchAdminProcess +0x62f LEA RCX,[0x1806c4920]` `+0x62d XOR EDX,EDX` (프로시저 번호 0) | appinfo 서버 구조체 rva `0x50190` → `MIDL_SERVER_INFO` rva `0x51ae0` → 서버 함수 표 (`DispatchTable`) rva `0x50cd0` 8칸 (+ 썽크 `Ndr64AsyncServerCallAll`), `RPC_DISPATCH_TABLE` (개수 8) rva `0x52830` · `pefile` |
| 호출 꼴 | **비동기** (`Ndr64AsyncClientCall`, `RPC_ASYNC_STATE` 알림 = 이벤트 `AicpAsyncInitializeHandle` `RpcAsyncInitializeHandle(…,0x70)` + `CreateEventW`), **명시적 바인딩 핸들**, 기다림 `AicpAsyncFinishCall` (`AicpMsgWaitForCompletion` 으로 메시지 펌프 하며 대기, 끝나면 `RpcAsyncCompleteCall`) | `AicLaunchAdminProcess.asm +0x57b · +0x636 · +0x4af`, `AicpAsyncInitializeHandle.c`, `AicpAsyncFinishCall.c` |
| protseq · 끝점 | `ncalrpc` · **끝점 없음 (동적)**. `RpcStringBindingComposeW(ObjUuid=L"201ef99a-7fa0-444c-9399-19ba84f12a1a", ProtSeq=L"ncalrpc", Addr=NULL, Endpoint=NULL, Options=NULL)` → `RpcBindingFromStringBindingW` → `RpcBindingSetAuthInfoExW` | `AicpCreateBindingHandleEx.asm +0x5c · +0x7a · +0x126` (글자 rva `0x779b80`, `0x73b130`) |
| 인증 | 수준 **6** (`PKT_PRIVACY`) · 서비스 **10** (`WINNT`) · 서버 주체 이름 NULL · `RPC_SECURITY_QOS_V3` { Version 3, Capabilities 1 (MUTUAL_AUTH), IdentityTracking = 호출 인자 3 (`AicLaunchAdminProcess` 가 **0**=정적), Impersonation 3, **`Sid` = `CreateWellKnownSid(22)` = LocalSystem `S-1-5-18`** } | `AicpCreateBindingHandleEx.asm +0xc7 LEA ECX,[RDX+0x16]`, `+0xca CreateWellKnownSid`, `+0xe5 ~ +0x10a` QOS 칸, `+0x126/+0x12d RpcBindingSetAuthInfoExW` |
| 바인딩 핸들 캐시 | 전역 `[0x180831100]` 에 한 번 만든 핸들을 `CMPXCHG` 로 저장해 다음 부름에 재사용 (`StartFlags` 비트 `0x400` 이 꺼져 있을 때) | `AicLaunchAdminProcess.asm +0x1a3 · +0x1e4` |
| `ept_map` 을 묻는 까닭 | 끝점 없는 바인딩 → rpcrt4 가 `\RPC Control\epmapper` 에 `ept_map_auth_async` (프로시저 8, run145) 를 보냄. 요청 탑: 1층 `201ef99a` v1.0 · 2층 NDR 2.0 (또는 NDR64) · 3층 ncalrpc, **객체 UUID = `201ef99a-…` (바인딩 글의 그 UUID)**, **SID = `S-1-5-18`** (QOS V3 `Sid` 가 `NormalizeServerSid` 를 거쳐 감) | `rpcrt4/ResolveEndpoint.asm +0x20c NormalizeServerSid`, `+0x246 GetNextNormalizedSid` (있던 `.asm`, `0x6d9` 로 거절되면 **다음 정규화 SID 로 한 번 더**) · `LookupNextChunk +0x11d Ndr64AsyncClientCall` — 이 두 줄의 이음은 rpcrt4 몫 |
| 서버 쪽 등록 (장부에 들어갈 값) | `RpcServerUseProtseqW(L"ncalrpc", 10, NULL)` (동적 끝점) → `RpcServerRegisterIfEx(&iface, NULL, NULL, **0x29**, **0x4d2**, NULL)` → `RpcEpRegisterW(&iface, 모든 바인딩, **UUID 벡터 NULL → 객체 nil**, L"AppInfo")`. 플래그 `0x29` = AUTOLISTEN `0x1` \| ALLOW_SECURE_ONLY `0x8` \| ALLOW_LOCAL_ONLY `0x20` `[비트 이름은 SDK 값 기준 추측]`, 최대 호출 1234, **보안 콜백 없음** | `appinfo!AiEnableDesktopRpcInterface` (내보내기 서수 2, rva `0x12920`): `RpcServerRegisterIfEx(&DAT_180050190 …)` (rva `0x50190` = 201ef99a 구조체) → 다른 세 인터페이스도 같이 → `RpcEpRegisterW(&DAT_180050190 …)`. 이 함수를 부르는 길: `ServiceMain +0xaab/+0xab0` `IsAiExtDisableDesktopRpcInterfacePresent` → **`+0xab4/+0xabb` `AiExtEnableDesktopRpcInterface`** (지연 로드, api `ext-ms-win-security-appinfoext-l1-1-0` → `appinfoext.dll` 의 내보내기 `0x1f50`, **몸이 `JMP [IAT]` 한 줄 → `appinfo.dll!AiEnableDesktopRpcInterface`** `[objdump + pefile]`). `ServiceMain` 자신이 직접 등록하는 것은 **다른** 인터페이스 둘 (`0497b57d-2e66-424f-a0c6-157cd5d41700` v1.0 `+0xa33`/`+0xa5b`, `0f738e20-73c0-4ca8-aa6a-8dfef545fea8` v1.0 `+0xb03`/`+0xb2b`, 같은 플래그 `0x29`) 와 `RpcServerUseProtseqW("ncalrpc", 10, NULL)` (`+0x9e4/+0x9eb`) · 샌드박스 실행용 정적 끝점 `AppInfoSandboxLaunch` (`UseProtseqEpW +0xd18`, `RegisterIf3 +0xd62`) |
| 요청 12칸 (NDR64 프로시저 형식, 스택 오프셋은 `[rsp+0x20]` 부터 8바이트씩) | 아래 표 | `AicLaunchAdminProcess.asm +0x5a5 ~ +0x62f` 인자 놓기 · NDR64 프로시저 형식 rva `0x703700` (스택 크기 `0x70`, 상수 클라이언트 버퍼 40, 상수 서버 버퍼 104, 칸 12, 플래그 `0x010c0040`) |

요청 · 응답 칸 (스택 오프셋은 NDR64 형식의 값, 둘째 열은 클라이언트가 넣는 것):

| 칸 | 스택 | 형식 (NDR64) | 클라이언트가 넣는 값 |
|---|---|---|---|
| (바인딩 핸들) | `0x08` | explicit handle | `AicpCreateBindingHandleEx` 가 만든 것 / 전역 캐시 |
| p0 실행 파일 | `0x10` | `[in, unique, string]` wchar | `CallCreateProcess` 의 `lpApplicationName` (`.cmd`/`.bat` 면 NULL), `AicpExpandMappedDrive` 로 매핑 드라이브 풀어 줌 (`WNetGetConnectionW`) |
| p1 명령줄 | `0x18` | `[in, unique, string]` | `this+0x200` |
| p2 `StartFlags` | `0x20` | int32 | 위 (1) 끝 문단 + `AicLaunchAdminProcess` 가 `& ~0x400` 하고 WOW64 리다이렉션이면 `\|8`, 호스트 프로세스가 포그라운드 권한이 있으면 `\|0x10` (`+0x37f ~ +0x39c`) |
| p3 `CreationFlags` | `0x28` | int32 | **`0x04080414`** = `NEW_CONSOLE 0x10` \| `UNICODE_ENVIRONMENT 0x400` \| `DEFAULT_ERROR_MODE 0x04000000` \| `CREATE_SUSPENDED 0x4` \| `EXTENDED_STARTUPINFO_PRESENT 0x80000` (`_PrepareAndCallCreateProcess +0x2b`, `CallCreateProcess +0xcb`). 호출자가 `CREATE_BREAKAWAY_FROM_JOB` 류를 주면 `+0x368 BTS [RDI+0xe4],0x18` 로 `0x01000000` 이 더해짐 |
| p4 현재 폴더 | `0x30` | `[in, string]` (널 아님) | `this+0x50`, 비어 있으면 `GetCurrentDirectoryW(0x104)` |
| p5 창 스테이션\바탕화면 | `0x38` | `[in, string]` (널 아님) | `STARTUPINFO.lpDesktop` 가 비지 않으면 그것, 아니면 `GetProcessWindowStation` 이름 + `\` + `GetThreadDesktop` 이름 (`AicpGetCurrentDesktop`, 보통 `WinSta0\Default`) |
| p6 시작 정보 | `0x40` | `[in]` 구조체 56바이트 (`APP_STARTUP_INFO`) | `STARTUPINFOW` 의 `lpTitle`(포인터) · `dwX/Y/XSize/YSize/XCountChars/YCountChars/FillAttribute/dwFlags/wShowWindow` 를 앞 `0x30` 바이트에 베끼고, `dwFlags & 0x400` 이면 `hStdOutput` (= HMONITOR) 를 `GetMonitorInfoW` 로 풀어 뒤 8바이트 (`+0x282 ~ +0x2c9`) |
| p7 창 | `0x48` | int64 (`HWND`) | `this+0xc0` (부모 창, `IUnknown_GetParentWindow`). NULL 이 아니면 호출 전 `GetAncestor(…,2)` 루트 창을 `EnableWindow(FALSE)`, 끝나면 `TRUE` (`+0x3c1`, `+0x460`, `+0x6d0`) |
| p8 시간 제한 | `0x50` | int32 | **`0xffffffff`** (`+0x5bb OR [RSP+0x68],-1`) — 서버는 이 값을 consent.exe 기다리는 시간으로 씀 (`AiLaunchConsentUI +0x2d4 WaitForSingleObject(…, 시간)`) |
| p9 `ProcessInformation` | `0x58` | `[out]` 구조체 **24 B** = `PROCESS_INFORMATION` ( `hProcess` 8 · `hThread` 8 · `dwProcessId` 4 · `dwThreadId` 4 ) | 호출자 칸 `this+0x180` (아래 응답). 오류 때는 클라이언트가 두 핸들을 `CloseHandle` (`+0x4f8`, `+0x518`) |
| p10 `ELEVATION_REASON` | `0x60` | `[out]` int32 | `&local_7c0` (클라이언트는 이 값을 뒤에서 안 씀) |
| 반환 | `0x68` | int32 (Win32 오류) | `RpcAsyncCompleteCall` 이 돌려줌 (RPC 오류가 있으면 그것이 우선) |

응답 값: 핸들 두 개는 **호출자 프로세스 안에서 유효한 값** (서버가 `NtDuplicateObject(자기 핸들, 호출자 프로세스, DUPLICATE_SAME_ACCESS)` 로 복제 — `AiLaunchProcess +0x1499 / +0x14ef`, 옵션 `[rsp+0x30] = 2`), PID · TID 는 `AiLaunchProcess` 끝 `p_Var27->dwProcessId/dwThreadId`. `ELEVATION_REASON` 은 서버가 `*local_508 = local_644` (`RAiLaunchAdminProcess` 끝, `RpcAsyncCompleteCall` 바로 앞) 로 채우고, 처음 값과 runas (`StartFlags & 1`) 때 값이 **6** `[정적: 값만, 이름은 추측]`.

## (3) 오류 값 → `ShellExecuteEx*` 반환 · `GetLastError` 표

흐름 `[정적]`: `AicLaunchAdminProcess` 가 Win32 오류 E 를 돌려줌 (`+0x74d MOV EAX,EBX`) → `CallCreateProcess +0xc0c TEST EAX,EAX / JLE`, 양수면 `+0xc12 MOVZX ESI,AX / +0xc15 OR ESI,0x80070000` (= `HRESULT_FROM_WIN32`), 음수면 실패 갈래 → `_PrepareAndCallCreateProcess` (`+0x45c` 실패 처리기들) → `_TryCreateProcess` 가 `this+0xb8` 에 담고 0 반환 → `_ExecuteAssoc` 의 반환 HRESULT → `_DoExecute +0x134 _ReportHresult`: 실패 HRESULT 가 Win32 시설 (`0x7`) 이면 아래 16비트, 아니면 그대로를 `this+0xa0` 에 → `+0x152 _FinalMapError` → `+0x15c _TryErrorMsgBox` → `ShellExecuteNormal` 의 `Finalize` (`+0x59 _FinalMapError(…, &hInstApp)` · `+0x5e CMP [RBX+0xcd],0 / +0x6f CMOVNZ EAX,0x4c7`) → `ShellExecuteExW +0x3c SetLastError` · `+0xee CMP EDI,0x8000000a`.

- **반환 BOOL**: `dwErrCode == 0` 또는 `== 0x8000000a` 이면 TRUE, 그 밖은 FALSE (`ShellExecuteExW.asm +0x48 ~ +0x55 · +0xee ~ +0xfa`).
- **`hInstApp`** (`_FinalMapError`): 오류 0 → `this+0x98` (비어 있으면 `0x2a`=42). 오류 2·3·5·8 → 그대로. 아래 표에 있는 값은 매핑, **그 밖은 5** (`SE_ERR_ACCESSDENIED`). 표 (rva `0x768560`, 16쌍): `0x20→26` `0x0e→8` `0xa1→3` `0x35→3` `0x94→3` `0x4b3→3` `0x47e→10` `0x47f→12` `0x481→15` `0x480→16` `0x482→20` `0x483→31` `0x484→29` `0x485→32`. 대응 읽기: `pefile` (`_FinalMapError.asm +0xde LEA R9,[0x180768560]`).
- **메시지 상자와 `0x4c7` 덮어쓰기** (`_TryErrorMsgBox`): 오류가 0 · `0x4c7` · `0x8000000a` 가 아니고 `fMask & 0x400` (`SEE_MASK_FLAG_NO_UI`) 가 꺼져 있고 창이 NULL 이거나 살아 있으면 → (`SHExecuteErrorMessageBox` 가 있으면 **띄움**) 그리고 `this[0xcd] = 1`. 그러면 `Finalize` 가 **반환 오류를 `0x4c7` 로 바꾼다**. `hInstApp` 은 원래 오류로 이미 계산됨.

| 서버/RPC 가 돌려주는 E | 어디서 나오나 | `fMask` NO_UI 켬 → `GetLastError` / BOOL | NO_UI 끔 → `GetLastError` / BOOL | `hInstApp` |
|---|---|---|---|---|
| **0** | 성공 | 0 / TRUE | 0 / TRUE | `0x2a` 또는 `this+0x98` |
| **`0x4c7`** `ERROR_CANCELLED` | consent 가 토큰을 안 줬고 `*param_2 & 0x10` 이 꺼져 있음 — `AiCheckLUA +0x118 TEST … 0x10`, `+0x121 MOV EBX,0x4c7` | 0x4c7 / FALSE | 0x4c7 / FALSE (상자 안 띄움) | **5** |
| **`0x2e4`** `ERROR_ELEVATION_REQUIRED` | 승인 뒤 받은 토큰이 승격 아님 (`+0x16e AiCheckForElevatedUser`, `+0x18f MOV EBX,0x2e4`) 이거나 관리자 아님 (`+0x1af AiCheckForAdminUser`) | 0x2e4 / FALSE | **0x4c7** / FALSE (상자 띄움 → 덮어씀) | 5 |
| **`0x5`** `ERROR_ACCESS_DENIED` | 서버: 호출자 권한으로 실행 파일 열기 실패 (`RAiLaunchAdminProcess +0x5c4 CreateFileW(0xa0000000, 공유 5, OPEN_EXISTING)` → `GetLastError`), `AiLaunchProcess` 검사 실패 `uExitCode=5`, `AipValidateLaunchedProcess` 실패 | 5 / FALSE | **0x4c7** / FALSE | 5 |
| **`0x6f7`** `RPC_X_BAD_STUB_DATA` (원문 막힌 자리의 우리 `ept_map` fault) | RPC 런타임 오류. 재시도 집합 `{0x6ac, 0x6b5, 0x6ba, 0x6d9}` 에 **안 든다** → 그대로 반환 | 0x6f7 / FALSE | **0x4c7** / FALSE | 5 |
| **`0x6ac` · `0x6b5` · `0x6ba` · `0x6d9`** (`NO_ENDPOINT_FOUND` · `UNKNOWN_IF` · `SERVER_UNAVAILABLE` · `EPT_S_NOT_REGISTERED`) | `AicLaunchAdminProcess +0x528 ~ +0x56d`: `EBX-0x6ac` 가 비트표 `0x200000004201` 에 들면 `AicpStartAIS` (`OpenSCManagerW` → `OpenServiceW(L"AppInfo", 0x14)` → `StartServiceW` (0x420 은 이미 실행 중이라 무시) → `QueryServiceStatus` 를 500 ms 씩 최대 11번 보다 RUNNING(4) 이면 0, STOPPED(1) 이면 종료 코드, 못 기다리면 `0x5b4`) 하고 **두 번까지** 다시 부름. 세 번째 실패는 `0x425` (`+0x6be MOV EBX,0x425`). `AicpStartAIS` 가 0 이 아니면 그 값이 곧 오류 | 그 값 / FALSE | `0x4c7` / FALSE | 5 |
| `0x57` · `0x32` · `0x3d35` 등 서버가 만든 값 | 경로 길이 `≥ 0x8000` 또는 매핑 뒤 (`RAiLaunchAdminProcess.c` 의 `local_678 = 0x57`), 세션이 승격 금지 (`+0xbf0 AiIsElevationAllowedForSession` → `0x32`), 패키지 등록 실패 (`+0xefa RegisterAppXPackageIfNecessary2` → `0x3d35`) | 그 값 / FALSE | `0x4c7` / FALSE | 5 |

- 원문 로그의 `rax 0` (FALSE) 는 위 `0x6f7` 줄과 맞는다. `GetLastError` 가 `0x6f7` 인지 `0x4c7` 인지는 샘플의 `fMask & 0x400` 에 달렸다 `[공백: 샘플 소스를 안 봄]`.
- **`GetLastError` 와 별개로 `hInstApp` 과 `SetLastError` 시점**: `SetLastError(E)` 는 `ShellExecuteExW +0x3c` 한 곳 — 성공 때도 `SetLastError(0)` 을 부른다.
- 서버가 줄 때 서버 쪽에서 `0x4c7` 은 로그용 HRESULT 로는 0 으로 기록 (`RAiLaunchAdminProcess` 끝 `if (local_678 == 0x4c7) local_648 = 0`) — RPC 반환값 자체는 `0x4c7` 그대로.

## (4) appinfo 자식 만들기 표 — `RAiLaunchAdminProcess` (rva `0x43070`) → `AiLaunchProcess` (rva `0x30370`)

앞 단계 (기록 순서, 모두 `ms/disassemble/appinfo/RAiLaunchAdminProcess.asm`):

| 단계 | 줄 (call / ret) | 한 일 |
|---|---|---|
| UAC 켜짐 확인 | `+0x407/+0x40e` `CheckElevationEnabled` | kernel32 의 내보내기 (rva `0x42b80`) — **`objdump`**: `RtlQueryElevationFlags(&f)` 뒤 `*out = f & 1`. 0 이면 승인 없이 곧장 `AiLaunchProcess` (아래 "승인 건너뜀"). Ghidra 로 안 열음 `[objdump]` |
| 호출자 알아보기 | `+0x437/+0x43c` `AiGetClientInformation` (`+0x5e I_RpcBindingInqLocalClientPID` → `+0xa7 NtOpenProcess(0x1004c0)` → `+0xce RpcImpersonateClient` · `+0xf5 NtOpenThreadToken(0xf01ff)` · `+0x141 NtDuplicateToken` · `+0x150 RpcRevertToSelf` → `+0x229 NtQueryInformationToken(0xc)` 세션 id) | 호출자 **프로세스 핸들** · **토큰 사본** · 세션 id · PID |
| 정책 | `+0x44f LUAGetUACPromptPolicy`, `+0x47d AiCheckForElevatedUser`, `+0x4a6 AiCheckForAdminUser`, (`StartFlags & 8` 이면) `+0x4e4 AiConvertWow64Paths` | `LUAGetUACPromptPolicy` 가 읽는 값 이름: `EnableLUA`, `ConsentPromptBehaviorUser`, `ConsentPromptBehaviorAdmin`, `ConsentPromptBehaviorEnhancedAdmin` (그림자 관리자), 키 `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System` (`AipGetElevationPromptType`, `AipCheckAutomaticAdminPolicy` 의 `EnableAutomaticAdmin`) |
| 실행 파일 열기 | `+0x580 RpcImpersonateClient` · `+0x5c4 CreateFileW(GENERIC_READ\|EXECUTE, 공유 5, OPEN_EXISTING)` · `+0xabc CheckElevation` (kernel32, 매니페스트 요구 단계) · `+0xb15/+0xb2a AiCheckSecureApplicationDirectory(Ex)` · `+0xb8d AiIsEXESafeToAutoApprove` · `+0xb92 RpcRevertToSelf` | 호출자 권한으로 파일이 열려야 함 (안 열리면 앱 실행 별칭 `GetAppExecutionAliasPath…` 갈래, 아니면 `0x5`) |
| **runas 표시** | `if (StartFlags & 1) { if (요구수준 < 2) flags \|= 0x200; 요구수준 = 2; ELEVATION_REASON = 6 }` | 요청자가 "runas" 임을 못박음. 이 비트는 `AicLaunchAdminProcess` 가 위에서 `(이전 오류 ≠ 0x2e4)` 로 세움. 비트 `0x10` → `flags \|= 0x800`, 비트 `0x80` → `\|= 0x100000` |
| 승인 | `+0xe33/+0xe38` `AiCheckLUA` (`+0x3e AipRequireElevationPrompt` → `(flags & 0x18)==0x10` 이면 **0 반환·토큰 없음 = 승인 불필요** → `+0x100/+0x105 AiLaunchConsentUI` → 토큰 없고 `0x10` 꺼짐이면 `+0x121` **`0x4c7`** → `+0x16e AiCheckForElevatedUser` / `+0x1af AiCheckForAdminUser` 실패 `+0x18f` **`0x2e4`** → `+0x1cf NtSetInformationToken(token, 0xc, 호출자 세션 id)` → 토큰을 돌려줌) | consent 창 본문은 범위 밖. 부르는 쪽: `AiLaunchConsentUI` 는 `consent.exe %u %u %p` 를 `AiLaunchProcess` 로 띄우고 (`+0x296`), `AipVerifyConsent` (`+0x2aa`) 후 `ResumeThread` · `WaitForSingleObject(시간 제한)` · 종료 코드 0 이면 consent 가 넘긴 토큰 `local_120` 을 돌려줌 |
| 만들기 | `+0x1096/+0x109b` `AiLaunchProcess` | 아래 표 |
| 끝 | `+0x131f RpcAsyncCompleteCall` (바로 앞 `*local_508 = local_644` = ELEVATION_REASON) | 반환 오류 = `local_678` |

`AiLaunchProcess` (rva `0x30370`):

| 항목 | 값 · 방식 | 근거 (줄) |
|---|---|---|
| **토큰** | `RAiLaunchAdminProcess` 가 넘긴 토큰: **`AiCheckLUA` 가 consent 뒤 돌려준 승격 토큰** (`local_610`), 승인이 필요 없었으면 호출자 토큰 사본 (`pvVar23`). `AiLaunchProcess` 는 플래그 비트 7 (`0x80`) 이 꺼져 있으면 `NtDuplicateToken` (`+0x25a`) 으로 한 번 더 복사해서 쓴다. 승격 토큰의 **조건**: `TokenElevation(0x14) ≠ 0` · 관리자 · 세션 id = 호출자 세션. **정적으로 보이는 토큰 칸 (종류 · 무결성 · 그룹 · 특권) 은 appinfo 안에 없다** — 토큰 자체는 consent.exe 쪽이 만들어 돌려주고 appinfo 는 위 세 가지만 검사 | `RAiLaunchAdminProcess.c` (`pvVar18 = pvVar23; if (local_610 != 0) pvVar18 = local_610`), `AiLaunchProcess.asm +0x25a`, `AiCheckLUA.asm +0x16e · +0x1af · +0x1cf` |
| 승격 토큰의 원천 `[추측]` | appinfo 에 **`LUAGetElevatedToken`** (rva `0x4d9b4`) 이 있다: `NtQueryInformationToken(0x12 TokenElevationType)` 이 **Limited(3)** 이면 `0x13 TokenLinkedToken` 을 돌려주고, Full(2) 이거나 (Default(1) 이면서 `TokenElevation(0x14) ≠ 0`) 이면 자기 복사, Default 이면서 승격 아니면 **토큰 없음**. `RAiGetElevatedToken` (프로시저 7) 와 `AiGetElevationToken` 이 이걸 부른다. consent.exe 가 승인 뒤 이 프로시저로 토큰을 받는지는 **consent.exe 를 안 읽어서** 모름 | `LUAGetElevatedToken.c`, `AiGetElevationToken.c`, `RAiGetElevatedToken` 호출표 |
| **부모 지정** | **호출자 프로세스** — `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS (0x20000)` 에 `&local_9b8` (= 인자 1 = `AiGetClientInformation` 이 연 호출자 프로세스 핸들, 접근 `0x1004c0` = `SYNCHRONIZE\|QUERY_INFORMATION\|CREATE_PROCESS\|DUP_HANDLE`), 인자 17 이 있으면 그것 (이번엔 NULL) | `AiLaunchProcess.asm +0x9ba MOV R8D,0x20000 / +0x9c7 UpdateProcThreadAttribute`; `AiGetClientInformation.asm +0xa7` |
| **속성 목록** | 항상: `0x20000` (부모) + **`0x60001`** (`ProcThreadAttributeExtendedFlags`, 값 = 4바이트 `local_9d0`, 승격 때 `2` 가 켜질 수 있음 `[추측: 비트 뜻]`) (`+0x9fb`). 조건부: `0x20008` 패키지 이름 · `0x20012` · `0x20011` (SAFEOPEN 류 520바이트 `[추측]`) · `0x20007` 완화 정책 · `0x20009`/`0x20021` 보안 능력 · `0x2000a` 작업(Job) 목록 | `AiLaunchProcess.asm +0x9ba ~ +0xd47` |
| **만드는 호출** | `CreateProcessAsUserW(token, exe, 명령줄, NULL, NULL, **bInheritHandles = FALSE**, **호출자 플래그 \| 0x80004**, 환경 블록, 현재 폴더, `STARTUPINFOEXW` (cb `0x70`, `lpAttributeList`), &pi)`. 세 갈래 (`+0x10a2`, `+0x12a7`, `+0x138e`) 가 같은 인자. 환경은 `CreateEnvironmentBlock(&env, token, FALSE)` (`+0x5c4`) → `AipHardenEnvironment` → `AipExpandEnvironment`. 앞뒤로 `ImpersonateLoggedOnUser` (`+0xeb9`) · `RevertToSelf` | `AiLaunchProcess.asm +0x106a/+0x126b/+0x1356 OR EAX,0x80004`, `+0x10a2/+0x12a7/+0x138e` |
| **만든 뒤 검사** | `AipValidateLaunchedProcess(경로, hProcess, 파일 핸들)` (`+0x1334`, `+0x141a`): `NtQueryInformationProcess(hProc, 0x2c, 열어 둔 파일 핸들)` 이 `0xc0000001` 이면 `AiOpenWOWStubs` 를 돌고, 이미지 파일 옵션 `Debugger` / `AppExecutionAliasRedirect` 로 가름, 실패는 `0x5` | `AipValidateLaunchedProcess.c` |
| **만드는 플래그 (ShellExecute 가 보내는 것)** | `0x04080414` (위 (2) p3) — 서버가 한 번 더 `\| 0x80004` 해도 같다 | |
| **멈춘 채 만들고 누가 깨우나** | 서버는 `if ((CreationFlags & 4) == 0) ResumeThread` (`+0x1567`, 실행 앞 `AiLaunchProcess.c:813`) — ShellExecute 는 `0x4` 를 **이미 켜서** 보내니 서버는 깨우지 않는다. **클라이언트가 깨운다**: `CallCreateProcess +0xde6 TEST byte [R15+0xe4],4` (호출자의 원래 플래그에 `CREATE_SUSPENDED` 가 없었으면) `+0xdf7/+0xdfe ResumeThread([R15+0x188]=hThread)`. 실패 (-1) 하면 패키지 앱일 때만 `TerminateProcess(0x80004005)` | `AiLaunchProcess.asm +0x1567`, `CallCreateProcess.asm +0xde6 ~ +0xe21` |
| **핸들 돌려주기** | `NtDuplicateObject(-1, hProcess, 호출자 프로세스, &out, 접근 0, 속성 0, 옵션 2)` 뒤 `hThread` 도 (`+0x1499`, `+0x14ef`); 이어 `dwProcessId` · `dwThreadId` 채움. 서버 자신의 원본 핸들은 `CloseHandle` | `AiLaunchProcess.asm +0x1485 ~ +0x1495` (옵션 `ECX = (flags & 0x401) ? 0 : 2`) |
| **승인 건너뜀** (`CheckElevationEnabled` = 0) | `AiLaunchProcess` 를 **호출자 토큰 사본**으로 곧장 부름 — 승격 없음 | `RAiLaunchAdminProcess.c` `if (local_5b0[0] == 0) goto LAB_180043fda` |

## Q276a — windows.storage · shell32 지도 상태

- **windows.storage**: `ms/disassemble/windows.storage/_map/ShellExecuteExW_{edges,nodes}.tsv` + `_summary.txt`. 뿌리 `ShellExecuteExW` (rva `0x38680`) 깊이 0 → 최대 10. 함수 424개 (펼침 308 · 공용 도구 104 · 깊이 끝 12), 끊긴 끝 192 (외부 168 · 간접 24). `nodes.tsv` 617줄, `edges.tsv` 1,440줄. 깊이별 새 함수 `[1, 8, 25, 41, 101, 91, 104, 96, 90, 41, 18]`. **한계**: 지도는 직접 `call` 만 따라서 `ShellExecuteExW` (0) → `ShellExecuteNormal` (1) → `ExecuteNormal` (2) → `_DoExecute` (3) → `_ExecuteAssoc` (4) 까지만 닿고, 그 뒤 **가상 호출 (`_ExecuteAssoc +0x175`, `InitAndCallExecute +0x214`) 에서 끊긴다.** `AicLaunchAdminProcess` 는 **지도에 없다** — 위 (1) 사슬은 `refs` (`_refs/AicLaunchAdminProcess.tsv` → `CallCreateProcess` → `_PrepareAndCallCreateProcess` → `_TryCreateProcess` → `Launch` → `Execute`) 로 거꾸로 이었다. sast/140 "다음 요청 후보 1" (`ShellExecute*` → `CInvokeCreateProcessVerb::Execute` 가 이어지지 않음) 은 이 결과서 (1) 의 5 ~ 9 줄로 닫힘.
- **shell32**: `ms/disassemble/shell32/_map/ShellExecuteExW_{edges,nodes}.tsv` — **뿌리 1개 · 깊이 0 · 노드 1 · 간선 0** (썽크 `JMP [IAT]` 한 줄이라 호출 없음). shell32 쪽 일은 여기까지.
- 이번에 새로 만든 파일: `ms/decomp|disassemble/{windows.storage,appinfo}/` 약 130 함수 (사슬 + 깊이 1 callee), `ms/disassemble/{windows.storage,shell32}/_refs/*.tsv`, `ms/disassemble/kernelbase/SHCoCreateInstance.asm`.

## 질문별 답 [정적] (자세히)

### 질문 1 — 부탁하는 쪽

- **함수 사슬 · 줄**: (1) 표. 승격을 부탁하는 함수는 **windows.storage `AicLaunchAdminProcess`** (이름은 PDB 에 있음, `[정적] PDB`). `AicpCreateBindingHandleEx` (`+0x1c2/+0x1c7`) 에서 바인딩 핸들, `+0x636/+0x63d` 에서 `Ndr64AsyncClientCall`.
- **UUID · 버전 · 프로시저 · protseq · 끝점**: (2) 표.
- **끝점이 동적이라 `ept_map` 을 묻는가**: 예 — 바인딩 글에 끝점 칸이 없다 (`RpcStringBindingComposeW` 의 Endpoint 인자 NULL, `AicpCreateBindingHandleEx +0x4e XOR R9D,R9D`). 서버 쪽은 `RpcServerUseProtseqW("ncalrpc", 10, NULL)` 로 끝점을 안 정함. 따라서 클라이언트 rpcrt4 가 `ept_map_auth_async` 를 보낸다 (run145). **원문의 `0xe1af8308…` 는 EPM 인터페이스** (run145 의 `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0) 이고 승격 인터페이스는 아니다.
- **요청 인자 꼴**: (2) 표 p0 ~ p10. 실행 파일 · 명령줄 · `StartFlags` · `CreationFlags 0x04080414` · 현재 폴더 · 창 스테이션\바탕화면 · 시작 정보 56바이트 · `HWND` · 시간 제한 `0xffffffff`.
- **응답 꼴**: `PROCESS_INFORMATION` 24바이트 (핸들은 호출자 쪽 값) · `ELEVATION_REASON` 4바이트 · 반환 오류 4바이트.

### 질문 2 — 응답 뒤

- **클라이언트 검사**: 핸들 유효성 검사 **없음**. 오류가 0 일 때만 이어서: `PcaMonitorProcess2(hProcess, 1, 실행파일, 명령줄, 폴더, 2)` (`CallCreateProcess +0xc93/+0xc9a`, 조건 `(this+0x1f8 & 1) \|\| 승격 성공`, **반환값을 안 씀** — 다음 줄이 `LEA`) → (패키지 앱이면 `RunWithApplicationInstanceManager`) → `+0xdda SetProcessLaunchForegroundPolicy(pid, 4)` (함수가 있고 `this+0x1f8 & 0x20` 일 때) → `+0xdf7 ResumeThread` → `+0xe40 SetLastError`. 이어 `_PrepareAndCallCreateProcess` 가 `DelegateSwitchToDesktopIfNeeded`, `_TryCreateProcess +0x79 NotifyCreatedProcess` (`ICreatedProcess` 쪽 콜백 + DDE 일 때 `WaitForInputIdle(hProcess, 60000)`), `+0x85 _FixActivationStealingApps`.
- **스레드 깨우기**: 예, 클라이언트가 (3 의 "멈춘 채 만들고" 줄).
- **`hProcess` 를 돌려주는 조건**: `Finalize +0x30 CMP [RBX+0xa0],0` (오류 0) 이고 `+0x39 TEST byte [RDI+4],0x40` (`SEE_MASK_NOCLOSEPROCESS`) 이면 `+0x3f ~ +0x46` `pExecInfo->hProcess (+0x68) = [this+0xa8]`. **`[this+0xa8]` 를 누가 채우는지는 `[공백]`** — `CShellExecute::OnCreated` (rva `0x17cd70`, `+0x38` 에서 가상 호출로 `this+0x98` 에 값을 받고 `+0x55 WaitForInputIdle(this+0x98, 60000)`) 까지만 이었다. `_FinalMapError` 는 `hInstApp = this+0x98` 이라 두 칸이 같은 핸들일 가능성이 높다 `[추측]`. 다음 요청 후보 1.
- **오류 값마다 반환**: (3) 표.
- **RPCSS 연결 셋이 꼭 필요한가 — 아니오 (정적)**:
  - 이 길의 COM 호출 후보 셋을 줄로 가름. ① `ExecuteNormal +0x252` `SHCoInitialize` = `CoInitializeEx` (run149: RPCSS 안 감). ② `_GetExecute +0x4a SHCoCreateInstance(CLSID_ExecuteAssociation)` → 위 (1) 6 번 — windows.storage 자기 `DllGetClassObject` 로 먼저 만들어 보고 (`_SHCoCreateInstance +0xae`), 성공이면 COM 으로 안 감 `[추측: 클래스 표에 이 CLSID 가 있다고 봄 — 표 안 읽음]`. ③ **`CallCreateProcess +0x930/+0x937` `CoCreateInstance(CLSID_ImmersiveShell {c2f03a33-21f5-47fa-b4bb-156362a2f239}, NULL, CLSCTX_LOCAL_SERVER (4), IID {6d5140c1-7436-11ce-8034-00aa006009fa})`** — 조건 `(this+0x1f8 & 0x10) == 0`. 이 호출이 run149 의 "첫 COM 사용 → RPCSS 연결 (bind 넷)" 을 일으킬 가장 유력한 줄이다 (원문 로그 순서: RPCSS bind 거절 ×3 → epmapper bind → `ept_map` 과 맞음) `[추측: 로그와 이 줄의 짝은 안 확인]`.
  - 실패해도 **이어 간다**: `+0x93c TEST EAX,EAX / +0x93e JS +0x997` → 결과를 안 쓰고 `+0x9af CBaseRPCTimeout::Disarm` 로 가서 다음 줄 (`AicLaunchAdminProcess +0xc04`) 로. 성공하면 `IImmersiveSystemMode` 로 `StartFlags \|= 0x40` 만 달라질 수 있음. `+0x8f6 CRPCTimeout(5000)` 은 이 호출에 5초 제한을 거는 것.

### 질문 3 — 들어주는 쪽 (appinfo)

- 토큰 · 부모 · 플래그 · 깨우기 · 핸들: (4) 표.
- 정적으로 안 보이는 것: **토큰의 구체 칸** (승격 종류 `TokenElevationType` = Full(2) 일 것, 무결성 High, 관리자 그룹이 켜짐, 특권 전부) 은 consent.exe 가 돌려주는 값이라 appinfo 코드에는 "승격(`0x14`) 이고 관리자" 두 가지 검사만 있다. 승격 쪽 원천은 `LUAGetElevatedToken` 의 **연결 토큰 (TokenLinkedToken)** 일 것 `[추측]`.
- 부모는 **호출자** 프로세스 (explorer 가 아니라 `ShellExecute` 를 부른 쪽, 원문 가정 "부모 지정 방식 (속성 목록 등)" 의 답 = `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS`).

## PE_emulator 쪽 — 지금 맞나·틀리나

(`src/rpc/epm.cpp` · `tokens.hpp` 는 **원문과 run145 의 인용만** 보고 씀. 고칠지는 사용자 · PE_emulator 결정.)

| 원문 "답이 오면 쓰는 곳" | 쓸 꼴 (한 줄) |
|---|---|
| `ept_map` 장부에 그 인터페이스 항목 | 항목 `{ 객체 nil · 인터페이스 `201ef99a-7fa0-444c-9399-19ba84f12a1a` 주 1 부 0 · 주석 `"AppInfo"` (≤ 63자) · 탑 `[{ ncalrpc, 우리 appinfo 창구 포트 이름 `LRPC-`+18자 }]` }` — 서비스가 **시작 때 등록**하는 꼴 (`AiEnableDesktopRpcInterface`). 탑 2층은 NDR 2.0 `v2.0` (run145 표) |
| SID 든 요청 받기 | 이 요청의 SID 는 **`S-1-5-18` (LocalSystem)**, 객체 UUID 는 **`201ef99a-…`** (nil 아님). run145 대로 SID → SD (`AddAccessAllowedAce`) → 등록자 토큰과 `AccessCheck`: **등록자 = 우리 appinfo 창구의 토큰 = SYSTEM** 이어야 통과. 장부 객체가 nil 이라 질문 객체가 달라도 맞음 (run145 고르기 규칙 3) |
| 새 창구 (승격 요청 → 승인) | `\RPC Control\LRPC-…` 포트에서 `201ef99a` v1.0 bind 받기 (NDR 2.0 `v2.0` 과/또는 NDR64 `v1.0`), **프로시저 0 비동기**. 요청 12칸을 읽고 응답 3칸 (`PROCESS_INFORMATION 24B` · `ELEVATION_REASON` · 반환 오류) 을 `RpcAsyncCompleteCall` 꼴로 보냄. 인터페이스 플래그 `LOCAL_ONLY\|SECURE_ONLY` 라 호출자 신분이 있어야 함 |
| 엔진 입구로 승격 자식 짓기 | `NtCreateUserProcess` 입구로 **부모 = 호출 PID (속성 `0x20000`)**, **토큰 = 승격 토큰 사본**, 플래그 `CreateProcessAsUserW` 와 같이 `CREATE_SUSPENDED` 켠 채 (`0x04080414` 들어옴), 만든 `hProcess`/`hThread` 를 **호출자 핸들 표에 `DUPLICATE_SAME_ACCESS` 로 복제**해 응답에. 깨우기는 클라이언트 (`CallCreateProcess +0xdf7`) 몫이라 서버는 안 깨움 |
| 팀원 토큰에 승격 꼴 | (a) **호출자 토큰은 `SHTestTokenMembership(NULL, 0x220)` 가 거짓**이어야 RPC 길로 온다 (관리자 그룹이 deny-only/꺼짐) — 팀원 `tokens.hpp:35~41` "관리자 그룹 · 승격 안 됨" 이 **켜진 그룹**이면 ShellExecute 는 **RPC 없이 `CreateProcessW`** 로 가 버린다 `[정적 CallCreateProcess +0x4a, 우리 토큰 값은 안 봄]`. (b) 승격 토큰 = `TokenElevation 1` · `TokenElevationType 2 (Full)` · 관리자 그룹 켜짐 · 무결성 High · 세션 id = 호출자 세션 (appinfo 가 `AiCheckForElevatedUser` · `AiCheckForAdminUser` · `NtSetInformationToken(0xc)` 로 확인/설정). (c) 호출자 토큰은 `TokenElevationType 3 (Limited)` + `TokenLinkedToken` 이 승격 토큰 (`LUAGetElevatedToken`) |
| cmstp COM 승격이 같은 길인가 (딸림) | **같은 서버 인터페이스 `201ef99a` · 같은 승인 엔진, 다른 프로시저.** 프로시저 6 `RAiForceElevationPromptForCOM` (`AiBuildCOMParams` → `AipGetTokenForService` → `AiCheckLUA` → consent) 과 7 `RAiGetElevatedToken` 이 있고 **프로세스가 아니라 토큰을 돌려준다**. 클라이언트는 windows.storage 의 `AicForceElevationPromptForCOM` (rva `0x628130`, `PromptDangerousOperation +0x326` 가 부름 — 파일 작업 쪽) 말고도 **rpcss 가 `ole32!CoAicGetTokenForCOM` (지연 로드) 을 부르는 길**이 따로 있다 (이쪽 안은 안 읽음 — 아래 "다른 프로젝트 몫"). 즉 ShellExecute 길과 **서버 몸 일부 (승인) 만 같고 클라이언트·응답 꼴은 다르다** |

**stub 이 채울 최소 목록** (정적에서 닫힌 만큼, 위에서 아래 순서):
1. EPM 장부에 `201ef99a` v1.0 항목 (nil 객체, `AppInfo`) + `ept_map_auth_async` 가 SID `S-1-5-18` · 객체 UUID `201ef99a-…` 인 요청에 이 항목의 탑을 돌려줌 (없으면 `0x16c9a0d6` 상태 · 탑 0 → 클라이언트가 **다음 정규화 SID 로 재시도**, 끝내 안 되면 `AicpStartAIS` → SCM `OpenService("AppInfo")`).
2. appinfo 창구: bind 받기 + 프로시저 0 (비동기) 요청 해석.
3. 응답: 반환 0, `PROCESS_INFORMATION` (호출자 표 핸들 2개 · PID · TID), `ELEVATION_REASON` 6.
4. 엔진: 승격 토큰으로 호출자의 자식 (일시 정지) 만들기.
5. 클라이언트 쪽 부수 호출이 막히지 않게: `PcaMonitorProcess2` (지연 로드 `ext-ms-win-appcompat-pcacli-l1-1-0`, 반환 무시 — **모듈이 없으면 지연 로드 실패가 예외가 되는지 안 봄** `[공백]`), `CoCreateInstance(ImmersiveShell)` 실패 허용 (이미 로그에서 거절을 받고 지나감), `SetProcessLaunchForegroundPolicy` (있을 때만).
6. 위 앞 조건: 호출자 토큰 그룹 상태 (deny-only), verb `runas` 의 레지스트리 (`HKCR\exefile\shell\runas\command`, 아래 VM 목록).

## 정적으로 못 닫은 줄 (`[공백]` · `[추측]` 모음)

- `[공백]` `CShellExecute+0xa8` (`hProcess` 로 나가는 칸) 을 쓰는 곳 · `OnCreated` 가 부르는 가상 메서드 이름.
- `[공백]` `this+0x1f8` 의 비트 (`0x1` PCA 감시 · `0x2` 관리자 앱 확인 갈래 · `0x10` COM 호출 건너뜀 · `0x20` 포그라운드 정책) 를 누가 어떻게 세우는지.
- `[공백]` `CLSID_ExecuteAssociation` 이 windows.storage 안 클래스 표에 있어 COM 을 안 타는지.
- `[공백]` `IAssociationArray::QueryObject` 가 `CBindAndInvokeStaticVerb` 를 돌려주는 줄 (5 → 7 의 이음은 `_GetExecute` → `SHCoCreateInstance` → `CExecuteAssociation_CreateInstance` 까지는 줄로 이었고, 생성자 `CBindAndInvokeStaticVerb` 호출은 `CExecuteAssociation_CreateInstance +0x66` 한 곳).
- `[추측]` consent.exe 가 `RAiGetElevatedToken` 으로 토큰을 받는다 (consent.exe 를 안 읽음).
- `[추측]` 플래그 · 속성 비트 이름 (`0x29`, `0x60001` 값, `0x20011`).
- `[공백]` `AiCheckForAdminUser` 가 보는 것 (그룹 · 토큰 종류) 상세.
- `[공백]` NDR 문법 두 개 중 어느 쪽으로 협상되는지 (로컬 호출이라 rpcrt4 몫).

## VM 으로 확인할 것 — 디버거 필요 (대기열 Q280 · Q283 RPC 묶음에 얹을 꼴)

멈춤점은 `모듈!함수+0x…` (WinDbg `bp` 는 test.exe 프로세스 조건 필요, CLAUDE.md).

| # | 알고 싶은 것 | 멈춤점 · 볼 것 |
|---|---|---|
| D1 | **요청 스텁 바이트**: p0 ~ p10 이 위 표와 맞는가, `StartFlags` · `CreationFlags 0x04080414` · `Timeout -1` | `windows_storage!AicLaunchAdminProcess+0x636` (call 직전, `[rsp+0x20..0x78]` 12칸 + RCX `0x1806c4920`·EDX 0). 그리고 rpcrt4 `Ndr64AsyncClientCall` 입구 |
| D2 | **`ept_map_auth_async` 요청 바이트** (SID 칸, 객체 UUID, 탑) · 응답 | `rpcepmap!ept_map_auth` (run145 의 입구) 또는 rpcrt4 `LookupNextChunk+0x11d` 직전. SID 가 `S-1-5-18` (`01 01 00 00 00 00 00 05 12 00 00 00`) 인가 |
| D3 | **응답 스텁 바이트**: PI 24B · ElevationReason · 반환 | `appinfo!RAiLaunchAdminProcess+0x131f` (call `RpcAsyncCompleteCall` 직전: `local_678`, `*local_588`), 클라이언트 `windows_storage!AicLaunchAdminProcess+0x4af` (`AicpAsyncFinishCall` 반환 `EAX`) |
| D4 | **방법 값**: `this+0x1a8 == 2` 인가, `SHTestTokenMembership` 반환 | `windows_storage!CInvokeCreateProcessVerb::Execute+0x4f` 뒤 `[rbx+0x1a8]`, `windows_storage!CallCreateProcess+0x4f` 직후 `EAX` (0 이어야 RPC 길) |
| D5 | `CoCreateInstance(ImmersiveShell)` 이 불리는가 · HRESULT | `windows_storage!CallCreateProcess+0x937` 직후 `EAX` (run149 의 RPCSS bind 거절 셋과 같은 호출인지) |
| D6 | **진짜 승격 토큰 칸** (종류 · 무결성 · 그룹 · 특권) | `appinfo!AiLaunchProcess+0x10a2` (`CreateProcessAsUserW` 첫 인자 `rcx` = 토큰) → `!token` / `NtQueryInformationToken` 으로 elevation type · linked · IL · groups · privileges. 대조용으로 호출자 쪽 `AiGetClientInformation+0x148` 뒤 토큰 사본 |
| D7 | 클라이언트가 돌려받은 핸들 · `ResumeThread` | `windows_storage!CallCreateProcess+0xdf7` 의 `rcx` (hThread) 가 호출자 핸들 표 안 값인지 |
| D8 | `HostProcessHasForegroundRight` · `GetWindowBand` · `EnableWindow` 가 실제로 불리는지 (hwnd 가 NULL 이 아닐 때) | `windows_storage!AicLaunchAdminProcess+0x390`, `+0x3c1` |

## VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것

- **승격된 창에서 `whoami /all`** (원문 요청): 그룹 (BUILTIN\Administrators `Enabled`, `Mandatory Label\High Mandatory Level`) · 특권 목록 · SID. 대조로 **승격 전 창** `whoami /all` 도 한 번 (Administrators 가 "Group used for deny only" 인지 — D4 와 같은 뜻).
- **UAC 레지스트리** `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` 에서 코드가 읽는 값만: `EnableLUA`, `ConsentPromptBehaviorAdmin`, `ConsentPromptBehaviorUser`, `ConsentPromptBehaviorEnhancedAdmin`, `EnableAutomaticAdmin` (+ 보안 데스크톱 값은 `AipCheckLegacySecureDesktopPolicy` 안을 안 읽어서 `PromptOnSecureDesktop` 짐작 `[추측]`).
- **verb 키**: `reg query HKCR\exefile\shell\runas /s` (`command` 기본값, `HasLUAShield` 가 있는지, **`Elevate` 값은 없어도 됨** — "runas" 글자 비교가 대신함), `reg query HKCR\exefile\shell\open /s` 에 `DelegateExecute` · `DropTarget` 이 **없는지** (있으면 7 번 줄에서 길이 갈림).
- **서비스**: `sc qc appinfo` (시작 종류, 보통 수동/트리거), `sc query appinfo`, `reg query HKLM\SYSTEM\CurrentControlSet\Services\Appinfo` (`ImagePath` svchost 그룹, `ObjectName` = LocalSystem, `Parameters\ServiceDll` = appinfo.dll). `AicpStartAIS` 가 `OpenServiceW("AppInfo", 0x14)` 로 시작을 시도하니 서비스 이름 `AppInfo`.
- **테스트 프로그램 쪽**: 샘플이 `SHELLEXECUTEINFO.fMask` 에 `SEE_MASK_FLAG_NO_UI (0x400)` 를 쓰는지 (소스) — (3) 표의 `GetLastError` 갈림.

## 다른 프로젝트 몫 (이번에 못 연 것 — 이름 · 물음)

1. **rpcrt4**: ① `LRPC_BASE_BINDING_HANDLE::ResolveEndpoint +0x1e8 ~ +0x24b` — 상태 `0x6d9` 면 `NormalizeServerSid` · `GetNextNormalizedSid` 로 SID 를 바꿔 **다시 `ept_map`** (있던 `.asm` 를 훑기만 함): 재시도 횟수 · 끝나는 오류 값. ② QOS V3 `Sid` 가 `ept_map_auth_async` 의 SID 인자로 가는 줄 (`LookupNextChunk +0x11d Ndr64AsyncClientCall` 앞). ③ `Ndr64AsyncClientCall` 의 NDR 문법 고르기 · 비동기 완료 규칙. (sast/149 → 154 가 rpcrt4 를 쓰는 중이라 안 엶)
2. **combase** (run149 와 같은 길): `CoCreateInstance(ImmersiveShell, CLSCTX_LOCAL_SERVER)` 가 RPCSS bind 거절을 받으면 어떤 HRESULT 로 얼마 만에 끝나는가 — 새 일은 없을 듯, run149 로 닫을 수 있는지만.
3. **kernel32** (프로젝트 `kernel32` 있음, 이 요청 범위 밖): `BaseCheckElevation` (rva `0x1e970`, 527 B) · `BaseElevationPostProcessing` · `CheckElevation` (`0x37470`) · `CheckElevationEnabled` (`0x42b80`) — 암묵 승격 (`CreateProcessW` → `0x2e4`) 길과 appinfo 의 "매니페스트 요구 수준".
4. **ole32 / combase / rpcss**: rpcss 가 지연 로드하는 `ole32!CoAicGetTokenForCOM` (ole32 내보내기 rva `0x4b160`, 몸은 `0x180038c6c` 로 인자를 그대로 넘김, `objdump`) 가 어느 서버 호출 (프로시저 6 / 7 / 다른 인터페이스) 로 가는가 — cmstp COM 승격의 클라이언트 쪽.
5. **pcacli**: `PcaMonitorProcess2` (api 집합 `ext-ms-win-appcompat-pcacli-l1-1-0`) 의 안: 또 다른 RPC/ALPC 를 거는가 (반환은 무시되지만 호출은 일어남).
6. **consent.exe**: 승인 뒤 토큰을 appinfo 에 어떻게 돌려주는가 (이름만).
7. **api 집합 스키마**: `ext-ms-win-shell32-shellcom-l1-1-0` · `ext-ms-win-appcompat-pcacli-l1-1-0` 가 어느 DLL 로 가는지 (에뮬레이터의 api 집합 표에 필요).

## 다음 요청 후보

1. windows.storage: `CShellExecute+0xa8` · `+0x98` 를 쓰는 곳 (`ICreateProcessOutputs` 몸, `CInvokeCreateProcessVerb` 의 `+0x88` 하위 객체) — `hProcess` · `hInstApp` 의 값.
2. windows.storage: `DllGetClassObject` 의 클래스 표에 `CLSID_ExecuteAssociation` 이 있는지 + `_CreateAssocFromItem` 이 `HKCR\exefile\shell\runas` 를 읽는 줄 — "COM 없이 verb 가 풀리나" 를 닫음.
3. kernel32 `BaseCheckElevation` · `CheckElevation` (위 3).
4. consent.exe 새 analyze — 토큰이 만들어지는 곳 (D6 의 정적 쪽).
5. rpcrt4 `ResolveEndpoint` 재시도 + SID (위 1) — sast/154 가 끝난 뒤.
6. appinfo: `AiCheckForAdminUser` · `AipRequireElevationPrompt` 의 플래그 표 (`0x10` · `0x20` · `0x40` · `0x200` …) 한 장 — 승격 종류 (consent / 자격 증명 / 자동 승인) 가름.

## 판정

**분석성공** — 질문 1 · 2 · 3 모두 줄 근거로 답함. 닫지 못한 것: ① `hProcess` 칸을 채우는 곳, ② `this+0x1f8` 비트의 출처, ③ consent.exe 쪽 토큰 원천 (정적으로는 appinfo 의 두 가지 검사까지), ④ rpcrt4 의 SID 재시도 이음 (다른 프로젝트 몫). 스크립트 문제 없음.

## 부록 — 근거 파일 (모두 `ms/` 아래, 레포 미커밋) · 재현

- windows.storage `.asm`/`.c` (`ms/disassemble|decomp/windows.storage/`): `ShellExecuteExW`, `ShellExecuteNormal`, `ExecuteNormal`, `_Init__17b990`, `_DoExecute`, `_ExecuteAssoc`, `_InitCommandExec__24145c`, `QueryObject__295fa0`, `_GetExecute`, `SHCoCreateInstanceWorker`, `_SHCoCreateInstance`, `Execute__3212d0`, `TryCreateProcessDdeHandler`, `CreateInvokeCreateProcessVerb`, `InitAndCallExecute`, `Execute__e1de0`, `SetElevationExecMethodIfNeeded`, `QueryExists__210150`, `Launch__e102c`, `_TryCreateProcess`, `_PrepareAndCallCreateProcess`, `CallCreateProcess`, `SHTestTokenMembership`, `AicLaunchAdminProcess`, `AicpCreateBindingHandleEx`, `AicpStartAIS`, `AicpAsyncInitializeHandle`, `AicpAsyncFinishCall`, `AicpGetCurrentDesktop`, `AicpExpandMappedDrive`, `Finalize__2fae0c`, `_FinalMapError`, `_ReportHresult`, `_ReportHinst`, `_TryErrorMsgBox`, `NotifyCreatedProcess`, `_PolicyAllowsCreateProcess`, `OnCreated__17cd70`, `AicForceElevationPromptForCOM`, `_calls.tsv`, `_map/`, `_refs/` (`0x702b90`, `AicLaunchAdminProcess`, `CallCreateProcess`, `_PrepareAndCallCreateProcess`, `_TryCreateProcess`, `0xe102c`, `0x320e20`, `TryCreateProcessDdeHandler`, `0x734ae0` · `0x734bb0` · `0x738f10`, `0x268f14`, `AicForceElevationPromptForCOM`).
- appinfo: `RAiLaunchAdminProcess`, `AiLaunchProcess`, `AiCheckLUA`, `AiLaunchConsentUI`, `AiGetClientInformation`, `AiGetElevationToken`, `LUAGetElevatedToken`, `LUAGetUACPromptPolicy`, `AipRequireElevationPrompt`, `AiCheckForElevatedUser`, `AipValidateLaunchedProcess`, `AiEnableDesktopRpcInterface`, `ServiceMain`, `RAiForceElevationPromptForCOM`, `RAiGetElevatedToken`, `AipGetElevationPromptType`, `AipCheckAutomaticAdminPolicy`, `_calls.tsv`.
- shell32: `ShellExecuteExW.asm`, `DelayLoad_ShellExecuteExW.asm`, `_map/`. kernelbase: `SHCoCreateInstance.asm`, `CreateProcessInternalW.asm` (있던 것).
- 재현 (Ghidra 아님): RPC 인터페이스 · NDR64 프로시저 형식 · 서버 표 · 오류 표 · CLSID 값은 `pefile` 로 구조체를 읽음 (windows.storage 클라이언트 `0x702b90` / `MIDL_STUBLESS_PROXY_INFO` 로 `0x6c4920` → `MIDL_SYNTAX_INFO` `0x702c90` → NDR64 proc 포인터표 `0x7034b0` → proc 0 `0x703700`; appinfo `0x50190` → `MIDL_SERVER_INFO` `0x51ae0` → 서버 함수 표 `0x50cd0`, `RPC_DISPATCH_TABLE` `0x52830`). `appinfoext.dll` 의 내보내기는 `pefile` + `objdump`. 이름은 `ms/sym/*.pdb` + `_index.tsv`. kernel32 `CheckElevationEnabled` · ole32 `CoAicGetTokenForCOM` 은 `objdump -d -M intel --no-show-raw-insn --start-address=…`. UUID 바이트 검색은 `System32` · `SysWOW64` 6,858 파일.
