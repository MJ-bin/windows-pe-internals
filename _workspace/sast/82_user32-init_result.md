# 82 user32-init — 정적 분석 결과 (Q150a, "최소 길 지도")

- 요청서: `_workspace/sast/82_user32-init.md`
- 모듈 / 바이너리: user32 (`ms/bin/.../System32/user32.dll`, PDB guid+age = `CDB947D1B96B87977915DE04CF54279C1`), win32u (`.../win32u.dll`, PDB guid+age = `6295787D0B7E537E98F77F31F4426D1C1`). ntdll 은 읽기만 (이미 있는 `.asm` + 몇 개 depth 0 추가, `-readOnly`). kernelbase·ucrtbase·condrv·sxs·sxssrv·basesrv 프로젝트는 열지 않음 (kernelbase 는 `.dll` 파일을 `objdump` 로 8줄 읽은 것뿐).
- 새로 analyze: user32 (`ms/ghidra/user32.gpr`, `_index.tsv` 3248줄), win32u (`ms/ghidra/win32u.gpr`, `_index.tsv` 1517줄). 둘 다 `SetPdb:` 줄 있음, SCRIPT ERROR 0개. 몇 분 안에 끝남.
- 뽑은 함수 (user32, depth 0): `UserClientDllInitialize`·`_DllMainCRTStartupForGS2`·`_UserClientDllInitialize`·`InitializeUMHandleList`·`InitializeNtdllUserPfn`·`InitializeCsrUserPfn`·`ClientThreadSetup`·`__ClientThreadSetup`·`InitProcessGdiScaling`·`InitProcessSystemDpiBehavior`·`InitProcessDpiAwareness`·`InitInstrument`·`GetDpiForCurrentProcess`·`Parse*`·`IsDisplaySettingsGroupPolicyFlagSet`·`SetProcessDpiAwarenessContext`·`SetProcessDpiAwarenessInternal`·`InitClientDrawing` 등 → `ms/decomp/user32/`, `ms/disassemble/user32/`. 지도: `ms/disassemble/user32/_map/UserClientDllInitialize_{edges,nodes}.tsv` (136개, 줄기 2단계까지만 읽음). win32u 스텁 12개 → `ms/disassemble/win32u/`. ntdll 추가 depth 0: `RtlInitializeNtUserPfn`·`RtlRetrieveNtUserPfn`·`RtlResetNtUserPfn`·`CsrAllocateCaptureBuffer`·`CsrAllocateMessagePointer`·`RtlGetCurrentServiceSessionId`.
- 쓴 `refs`: `ms/disassemble/user32/_refs/` 의 `0xda590`(gpsi), `0xda350`(gSharedInfo, 0x240B), `0xb63f8`(NtUserProcessConnect IAT 칸), `0xb63f0`(NtUserInitializeClientPfnArrays), `0xb6408`, `0xb64d0`, `__ClientThreadSetup`.
- **win32u 전체 syscall 번호표 (질문 4 의 "싸게 뽑히면 전체")**: `ms/disassemble/win32u/_syscalls.tsv` (1499개, 열: 번호 16진·10진·이름·rva·크기·같은 주소 export 별칭). 스텁 바이트(`4C 8B D1 B8 imm32`)를 직접 읽어 만든 것이고, Ghidra `.asm` 12개와 번호가 12/12 같음. 번호 중복 없음. sast/83 이 그대로 쓸 수 있음.
- 버전 확인:
  - 이번 모듈 대조: PE 헤더 `AddressOfEntryPoint` = `0x51120` = Ghidra 함수 `UserClientDllInitialize` 시작 (일치). `.asm` 의 `CALL qword ptr [0x1800b5b38]` 칸 = PE 가져오기 표의 `ntdll!CsrClientConnectToServer` 칸 `0xb5b38` (일치).
  - 알려진 사실 표 (ntdll): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` 일치 (`ms/disassemble/ntdll/_calls.tsv`).
  - 커널 PDB GUID 는 이번에 안 건드림.
- 스크립트 문제: 없음.
- 한계: **user32·win32u PDB 는 공개 심볼만이라 구조체 타입이 0개** (`llvm-pdbutil dump -types` → 0 records; `pdb_struct.py --list` 도 빈 결과). `SHAREDINFO`·`SERVERINFO`·`CLIENTINFO` 칸 이름은 PDB 로 못 붙임 → 아래 칸 이름은 "코드가 읽는 오프셋" 이 `[정적]`, 이름은 `[추측]` 로 표시. (`_PEB`/`_TEB`/`_SILO_USER_SHARED_DATA` 이름은 ntoskrnl PDB 에서 옴.)

## 먼저 읽을 것 (핵심 다섯 줄)

1. user32 의 진입점(`DllMain`) = `UserClientDllInitialize` (rva `0x51120`, 49B) → 진짜 몸은 `_UserClientDllInitialize` (rva `0x6ba84`, 2745B) 로 꼬리 점프. PROCESS_ATTACH(1) 일 때 안에서 syscall 은 **무조건 부르는 win32k syscall 이 0개**. `NtUserProcessConnect`·`NtUserInitializeClientPfnArrays` 는 `DllMain` 에 없다 — `ClientThreadSetup` 안에만 있고, 그것도 `gfServerProcess != 0` (csrss 같은 native 서브시스템 이미지) 일 때만.
2. 보통 프로세스가 `SharedSection`·`gSharedInfo` 를 받는 길은 **ntdll `CsrClientConnectToServer` (서버 번호 3)** 하나다: 연결이 이미 있으면 (`CsrPortHandle != 0`) `CsrpClientConnectToServer` → `CsrClientCallServer` → **`NtAlpcSendWaitReceivePort` (0x8e)** 한 번이고, 그 답 안(0x248B)의 `+8 ~ +0x247` (0x240B) 이 그대로 `gSharedInfo` 가 된다.
3. **`NtOpenSection(\Sessions\N\Windows\SharedSection)` 은 user32 안에 없다.** ntdll `CsrpConnectToServer +0x1b6/+0x1bb` 에만 있고, `CsrPortHandle == 0` (= 이 프로세스에서 처음 하는 CSR 연결) 일 때만 탄다. user32 `DllMain` 시점엔 kernelbase 가 이미 BASESRV 로 연결했으니 (run68) 정상 길에서는 **user32 가 `NtOpenSection` 을 안 부른다**. PE_emulator 가 user32 `DllMain` 에서 `NtOpenSection(SharedSection)` 을 봤다면 그 시점 `CsrPortHandle` 이 0 이었다는 뜻 (아래 질문 1(b), "지금 우리").
4. `DllMain` 이 읽는 psi(`gpsi`) 칸은 딱 셋: `psi+0x0` (dword 비트 `0x4`), `psi+0x8bc` (byte 비트 `0x20`), 조건부로 `psi+0x1b66` (word). `psi` 는 읽기 가능한 메모리여야 하고 (널이면 `+0x38e` 에서 바로 터짐), 0 으로 채운 `0x1b68` 바이트면 길이 끝까지 간다 (아래 질문 2, 읽는 쪽 추정).
5. TEB `Win32ThreadInfo` (`+0x78`) 는 **user32 코드가 안 쓴다** (`_UserClientDllInitialize`·`ClientThreadSetup` 에 `+0x78` 쓰기 줄 없음). 스레드가 처음 win32k syscall 을 부를 때 커널이 GUI 스레드로 바꾸고, 그 안에서 user32 `__ClientThreadSetup` 이 거꾸로 불린다. user32 `DllMain` 이 그 첫 win32k syscall 을 부르는지는 **gdi32 `GdiDllInitialize` (+0x996) 에 달렸다 → Q150b**.

## 질문별 답 [정적]

### 질문 1. user32 `DllMain` (PROCESS_ATTACH) 의 줄기

**진입 줄기** (`ms/disassemble/user32/UserClientDllInitialize.asm`, `_DllMainCRTStartupForGS2.asm`):

- `UserClientDllInitialize` (`0x51120`): `+0x14 CMP reason,1`, 1 이면 `+0x18/+0x1d` `_DllMainCRTStartupForGS2` (`__security_init_cookie` 만 하고 `1` 반환). 그다음 `+0x2c` `JMP` 꼬리 호출로 `_UserClientDllInitialize` (`0x6ba84`). 반환값은 그쪽 `EAX` 가 그대로 `DllMain` 반환값.
- `_UserClientDllInitialize`: `reason==1` 이면 아래 표, `reason==0` 이면 정리 (`FreeLibrary`·`DeleteCriticalSection`·…), 2·3 이면 `+0x776` 에서 바로 `GdiDllInitialize(hmod, reason)` 로. `+0xb5/+0xbc` `DisableThreadLibraryCalls(hmod)` 를 부르므로 스레드 attach/detach 는 안 옴.
- `_UserClientDllInitialize +0x2aa` 의 `CsrClientConnectToServer` 인자는 `.asm` 로 읽음 (Ghidra 가 import 서명을 4개로 잘못 붙임): `RCX` = 객체 디렉터리 문자열, `EDX` = `3`, `R8` = 0 으로 채운 0x248B 버퍼 주소 (`RSP+0x60`), `R9D` = `0x248` (값, 포인터 아님), `[RSP+0x20]` = `&gfServerProcess` (user32 rva `0xda9e8`).

**syscall·바깥 호출 차례 표 (reason = 1, 보통 프로세스: `gfServerProcess == 0`, PEB 비트필드 `+3` 가 0)**

`call/ret` 는 `_UserClientDllInitialize` 안 오프셋 (`.asm` 의 `CALL … ret=` 줄). syscall 번호는 `[ntdll]` / `[win32u]` 로 구분.

| 차례 | 자리 call/ret | 부르는 것 | syscall (번호) | 인자 · 읽는 것 | 다음으로 가려면 | 실패면 |
|---|---|---|---|---|---|---|
| 0 | `+0xae` | (코드) PEB `+0x7d0` (`Win32ClientProcInfo`) ← 0, PEB `+0x2c0` (`SessionId`) 를 `ESI` 에 | — | | | |
| 1 | `+0xc8 ~ +0x185` | `RtlInitializeCriticalSection` ×10 (`gcsClipboard`·`gcsLookaside`·`gcsHdc`·`gcsAccelCache`·`gcsDDEML`·`gcsUserApiHook`·`gcsDManipHook`·`gcsMPH`·`gcsClassRegister`·`gcsAvrt`) | 없음 (ntdll 코드) | | 반환 NTSTATUS 열 개를 OR 해서 음수 아님 | `+0x193 JL` → **FALSE** |
| 2 | `+0x1a0/+0x1a5`, `+0x1b4/+0x1b9` | `InitializeUMHandleList` ×2 (rva `0x53c9c`; 안에서 `RtlInitializeCriticalSection` 한 번) | 없음 | `gPackedProcessDpiInfo+0x60`/`+0x10` 자리의 두 목록 | 음수 아님 | `+0x1a7`/`+0x1bb JS` → **FALSE** |
| 3 | `+0x1d1/+0x1d8` | `NtQuerySystemInformation` | `[ntdll] 0x36` | 클래스 `0` (SystemBasicInformation), 버퍼 `RBP+0x1c0`, 길이 `0x40`, 반환길이 NULL | **성공** + 버퍼 `+0x28` (`MaximumUserModeAddress`) 이 `gHighestUserAddress` (rva `0xda9f8`) 로 감 (`+0x1e5 ~ +0x1ec`) | `+0x1df JS` → **FALSE** |
| 4 | `+0x1f3/+0x1f8` | `InitializeNtdllUserPfn` (rva `0x6b924`) → ntdll `RtlInitializeNtUserPfn` (`0x11f7c0`, call `+0x3f/+0x46`) → `RtlRetrieveNtUserPfn` (`0x11fc30`, `+0x62/+0x69`) | syscall 은 직접 없음. `RtlInitializeNtUserPfn` 안 `LdrProtectMrdata(0/1)` (`+0x85/+0x8a`, `+0xd0/+0xd5`) 가 `.mrdata` 쓰기를 열고 닫음 (`NtProtectVirtualMemory 0x50`, sast/55) | 크기 `0xc0`·`0xc0`·`0x90` 세 표를 ntdll 로 복사. **프로세스당 한 번만** (`DAT_1801e7340` 플래그, 두 번째는 `0xc000000d`) | 반환 `1` | `InitializeNtdllUserPfn` 이 `0` → `+0x1fa JZ` → **FALSE** |
| 5 | `+0x248/+0x24f` | `WTSGetServiceSessionId` (kernelbase 가 ntdll `RtlGetCurrentServiceSessionId` (rva `0x38fc0`) 로 `JMP`) | 없음 | 값 = `*(DWORD*)PEB.SharedData` (PEB `+0x90`, 널이면 0). `ESI`(`PEB.SessionId`)와 비교 | | |
| 6 | `+0x282/+0x287` | `StringCchPrintfW(buf, 0x100, L"%ws\%ld%ws", L"\Sessions", SessionId, L"\Windows")` — **세션이 서비스 세션과 다를 때만** (`+0x25b CMP ESI,EAX` → `+0x25d JZ` 로 건너뜀). 같으면 객체 디렉터리 = `+0x254` 에서 미리 담은 `\Windows` | 없음 | | | |
| 7 | `+0x2aa/+0x2b1` | **`CsrClientConnectToServer`** (ntdll `0xdb7a0`) 아래 (a) | 아래 (a) | `\Sessions\1\Windows` (또는 `\Windows`), 서버 `3`, 0x248B 버퍼, `0x248`, `&gfServerProcess` | 반환 음수 아님, 그리고 응답 안 `+8` 부터 0x240B | `+0x2b8 JS` → **FALSE** |
| 8 | `+0x30b ~ +0x37c`, `+0x380 ~ +0x387` | (코드) 응답 `+8` 부터 0x240B 를 `gSharedInfo` (rva `0xda350`) 로 복사; 첫 8B 를 `gpsi` (rva `0xda590`) 로 | 없음 | 아래 질문 2 | | (보호 프로세스 갈래만 `+0x2fe JNZ` → **FALSE**) |
| 9 | `+0x38e ~ +0x3d6` | `psi` 첫 dword 비트 `0x4` 가 켜졌을 때만 `_InitializeImmEntryTable` (`0x26fb0`)·`GetImmFileName`·`GetModuleHandleW`. 비트와 상관없이 **항상** `+0x3d1/+0x3d6` CFG 간접 호출 `gImmApiEntries+0xa0` (rva `0xd9630`) | 없음 | 파일 안 정적 값이 `0x5a1a0` (3B, `XOR EAX,EAX; RET`) → IME 안 켜졌으면 아무 일도 안 함 | | |
| 10 | `+0x3d8/+0x3dd` | `InitializeCsrUserPfn` (rva `0x666dc`) — **`gfServerProcess != 0` 일 때만** | | | | `+0x3df JZ` → FALSE (서버 길만) |
| 11 | `+0x3fc`, `+0x409`, `+0x410/+0x417` | (코드) PEB `+0x58` ← `apfnDispatch` (rva `0xb4720`, 커널 콜백 표), PEB `+0x230` (`PostProcessInitRoutine`) ← 0, `RegisterWaitForInputIdle(WaitForInputIdle)` | 없음 | | | |
| 12 | `+0x41c ~ +0x437`, `+0x445/+0x44c`, `+0x463/+0x46a` | `hmodUser`·`pUserHeap` (PEB `+0x30` ProcessHeap) 저장. `g_systemCallFilterId == 7` (파일 안 처음 값) 일 때 `GetProcessMitigationPolicy(self, 0xb, &v, 4)` | 안에서 부르는 syscall 은 kernelbase 쪽이라 이번에 안 풂 `[추측: NtQueryInformationProcess]` | 결과 `v & 0xf` | 실패(`0`)여도 계속 | |
| 13 | `+0x48e ~ +0x568` | `g_systemCallFilterId == 5` 일 때만 `apfnDispatch` 0x498B 를 `RtlAllocateHeap(pUserHeap,0,0x498)` 에 복사해 PEB `+0x58` 교체 + `InitWinpropManager` ×2 | 없음 | | 할당 성공 | `+0x4b5 JZ` → **FALSE** |
| 14 | `+0x576/+0x57b` | `InitInstrument` (rva `0x502c4`; `gfServerProcess == 0` 일 때) | **`[ntdll] NtOpenKey 0x12`** (`InitInstrument +0x98/+0x9f`, `\Registry\Machine\System\CurrentControlSet\Control\Error Message Instrument\`, 접근 `0x20019`), `NtQueryValueKey 0x17` (`+0xe4/+0xeb` `EnableLogging`, `+0x132/+0x139` `LogSeverity`, 조각 `EnableDefaultReply`), `NtClose 0xf` (`+0x14f/+0x156`) | | 키가 없어도 됨 (열기 실패 → 0 반환, `gfEMIEnable` ← 0) | FALSE 아님 |
| 15 | `+0x590`, `+0x5b9`, `+0x5d2/+0x5d9` | `RtlQueryElevationFlags`, (승격 아님이면) `RtlQueryInformationActiveActivationContext(5,…,0xc)`, 매니페스트 `uiAccess` 이면 **`NtUserSetProcessUIAccessZorder`** | **`[win32u] 0x157b`** (조건부) | 인자 없음 | | FALSE 아님 |
| 16 | `+0x602 ~ +0x63c` | TraceLogging 등록 ×3 (`+0x5e5`·`+0x5f1`·`+0x5fd`), `WppInitUm`, WIL 함수 포인터 대입, `WilInitialize_Result(0)` (`+0x710`), UserModeMenus 기능 켜짐이면 `RtlAllocateHeap(pUserHeap,8,8)` → PEB `+0x7d0` (`+0x73b ~ +0x760`) | 없음 (ETW 등록은 ntdll 코드) | | 할당 성공 | `+0x74a JZ` → **FALSE** |
| 17 | `+0x767/+0x76c`, `+0x76c/+0x771` | `InitProcessGdiScaling` (`0x4c68c`), `InitProcessSystemDpiBehavior` (`0x4c58c`) | `LdrQueryImageFileExecutionOptions` (IFEO 값 `GdiScaling`·`PerProcessSystemDpi`, 안에서 `NtOpenKey` 류), 키 없으면 `IsDisplaySettingsGroupPolicyFlagSet` (`NtOpenKey`·`NtQueryValueKey`×2·`NtClose`, 키 `\Registry\Machine\Software\Policies\Microsoft\Windows\Display`), `ParseGdiScalingElement` (`QueryActCtxSettingsW`) | 읽고 쓰는 칸: PEB `+0x20`(ProcessParameters)→`+0x60`, PEB `+0x2d0` (`AppCompatFlagsUser`) 비트 | 실패해도 계속 | FALSE 아님 |
| 18 | `+0x996/+0x99d` | **`GdiDllInitialize(hmodUser32, 1)`** (gdi32 rva `0xa6e0`) | **Q150b** (gdi32 쪽이 delay import 로 gdi32full 에 넘김) | | 반환이 **음수가 아님** (`0`=FALSE 도 통과, `+0xa8c NOT EBX; SHR 0x1f`) | `+0x9af JNS` 안 타면 `+0x9b1` → **FALSE** |
| 19 | `+0x9b8/+0x9bd` | `InitProcessDpiAwareness` (`0x4cf68`) | IFEO `DpiAwareness`, `ParseDpiAwarenessElement`/`ParseDpiAwareElement` (`QueryActCtxSettingsW`). **`[win32u] NtUserSetProcessDpiAwarenessContext 0x1576`** 은 `SetProcessDpiAwarenessContext +0x3a/+0x41` — 조건 아래 (c) | 읽는 칸: `gpsi+0x8bc`, TEB `+0x800`·`+0x8e0` | | FALSE 아님 |
| 20 | `+0x9e9`, `+0xa17/+0xa1e`, `+0xa4f`, `+0xa80/+0xa87` | 매니페스트 `disableWindowFiltering`=true 이면 `NtUserDisableProcessWindowFiltering`, `forceFocusBasedMouseWheel`=true 이면 `NtUserSetProcessMousewheelRoutingMode(1)` | **`[win32u] 0x13ec`**, **`[win32u] 0x1579`** (둘 다 조건부) | | | FALSE 아님 |
| 21 | `+0xa8c ~ +0xa93` | 반환 `EAX = (~GdiDllInitialize 결과) >> 31` = 결과가 음수 아니면 `1` | | | | |

#### (a) `CsrClientConnectToServer` 인자와 run68 의 BASESRV 연결과 다른 점

- 인자 (위 표 7번): 객체 디렉터리 `\Sessions\N\Windows` (N = `PEB.SessionId`, 서비스 세션 `PEB.SharedData` 첫 dword 와 같으면 그냥 `\Windows`; 문자열은 user32 rva `0xbc418` `\Windows`, `0xba170` `\Sessions`, 서식 `0xba1a0` `%ws\%ld%ws`), 서버 번호 **`3`** (USERSRV), 연결 정보 버퍼 **0x248B** (전부 0 으로 보냄: `+0x7b ~ +0x86` memset), 길이 `0x248`, 다섯째 인자 `&gfServerProcess`.
- 0x248B 의 뜻 `[정적 + 추측]`: 앞 8B 는 `DllMain` 이 안 읽음 (`ClientThreadSetup` 서버 갈래에서 `dwDispatchCount` 를 넣어 보냄), 뒤 0x240B = `SHAREDINFO` 모양 (`ClientThreadSetup` 서버 갈래의 `NtUserProcessConnect(-1, 0x240, buf)` 크기와 같음, `gSharedInfo` 도 정확히 0x240B: rva `0xda350 ~ 0xda590` 바로 뒤가 `gpsi`).
- run68 (kernelbase → BASESRV, 서버 번호 1, 연결 정보 8B) 과 **다른 점 둘**: ① 연결이 이미 있다. ntdll `CsrClientConnectToServer +0xa4 CMP CsrPortHandle,0` 에서 0 이 아니면 `+0xab JZ` 를 안 타고 `+0xad ~ +0xb5` 로 **`CsrpClientConnectToServer`** (rva `0xdb990`, ret `+0xba`) 를 부른다. 이미 연결됐으면 **`CsrpConnectToServer` (`NtOpenSection`·`NtCreateSection`·`NtConnectPort`·`NtMapViewOfSection`) 를 다시 안 탄다.** ② 연결 정보가 8B → 0x248B 로 커서 세 칸이 더해진다.
- `CsrpClientConnectToServer` (`.asm` `ms/disassemble/ntdll/CsrpClientConnectToServer.asm`): `+0x40/+0x45 CsrAllocateCaptureBuffer(1, 0x248)` (힙 = `CsrPortHeap`, 곧 csrss 와 공유하는 포트 뷰 안), `+0x57/+0x5c CsrAllocateMessagePointer` → 메시지 `+0x48` 에 포인터, `+0x69/+0x6e` 복사 (호출자 버퍼 → 캡처 버퍼 0x248B), 메시지 `+0x40` = 서버 번호 `3`, `+0x50` = 길이, **`+0x7f/+0x84 CsrClientCallServer(msg, capture, ApiNumber 0, DataLength 0x18)`**, 음수 아니면 `+0x95/+0x9a` 다시 복사 (캡처 버퍼 → 호출자 버퍼), `+0x9d/+0xa2 CsrFreeCaptureBuffer`. 할당 실패면 `0xc0000017`.
- `CsrClientCallServer` (rva `0xdbdd0`): 메시지 `+0x30` ← ApiNumber (`0` = CSRSRV 서버 번호 0 의 첫 API), 길이 필드, `+0xed/+0xf2` **`NtAlpcSendWaitReceivePort` (`[ntdll] 0x8e`)**: 포트 = `CsrPortHandle`, 플래그 `0x20000`, 보내는 메시지 = 받는 메시지 = 같은 버퍼, 받는 길이 `0x3b8`. 반환: syscall 이 음수면 `+0x131` 로 메시지 `+0x34` (Status) 에 적고, 아니면 메시지 `+0x34` 를 그대로 돌려줌. **즉 "답" = ① syscall 성공 ② 메시지 `+0x34` = 0 ③ 캡처 버퍼 안 0x248B 의 `+8` 부터 `SHAREDINFO`.** `LdrpIsSecureProcess` 가 0 이 아니면 `+0x1e JNZ` 로 따로 빠지고, 보호 프로세스(`PEB+3` 비트 `0x2`, `+0x65`) 는 ApiNumber 의 서버 번호가 2 일 때만 `0xc0000022` 로 거절 (`+0x149 ~ +0x15c`) — 서버 번호 0 인 이 연결은 통과.
- 서버 갈래: `CsrServerApiRoutine != 0` (csrss 안) 이면 포트 대신 그 함수 포인터를 부름 (`+0x17b ~ +0x182`). 보통 프로세스는 0.
- `gfServerProcess`: ntdll 이 PE 헤더 서브시스템(`NT헤더+0x5c`) 이 1 (NATIVE) 이면 `*flag = 1`, 아니면 `*flag = 0` (`CsrClientConnectToServer +0x8f ~ +0xc7`, `+0x1cb`). test.exe 같은 콘솔/GUI 이미지는 0.

#### (b) `NtOpenSection("\Sessions\N\Windows\SharedSection")` 은 어디에

- user32 `.asm` 어디에도 없다 (`_UserClientDllInitialize` 의 바깥 호출 목록에 `NtOpenSection` 없음, 지도 `_map` 에도 없음).
- 있는 곳: ntdll `CsrpConnectToServer` (rva `0xdbfe4`) `+0x1b6/+0x1bb` (`NtOpenSection`, 접근 4). 같은 함수의 뒤 `+0x338/+0x33d` 가 `NtMapViewOfSection` (`PAGE_READONLY`, ViewUnmap, `MEM_TOP_DOWN|SEC_NO_CHANGE`, run68/sast63 표). **이 길은 `CsrPortHandle == 0` 일 때만** (`CsrClientConnectToServer +0xab`). 
- 그러니 user32 `DllMain` 에서 `NtOpenSection(SharedSection)` 이 `0xc000003a` 로 실패해 `0xc0000142` 가 났다면, 그 시점에 ntdll 의 `CsrPortHandle` (rva `0x1d4e20`) 이 0 이었다는 뜻이다 (user32 의 서버 `3` 연결이 "첫 연결" 로 취급됨).
- `NtMapViewOfSection` 인자는 run68 표 그대로 (이번엔 새로 안 봄): 섹션 핸들, 읽기 전용(`PAGE_READONLY 2`), `0x500000`.

#### (c) win32u 쪽 syscall

- **`DllMain` 본체(`_UserClientDllInitialize`)가 부르는 win32k syscall 은 전부 조건부** (표 15·19·20):
  | win32u 이름 | 번호 | 부르는 자리 | 조건 | 인자 | 실패면 |
  |---|---|---|---|---|---|
  | `NtUserSetProcessUIAccessZorder` | `0x157b` | `_UserClientDllInitialize +0x5d2/+0x5d9` | 승격 아님 + 액티베이션 컨텍스트의 `uiAccess` 켜짐 | 없음 | 반환 안 봄 |
  | `NtUserSetProcessDpiAwarenessContext` | `0x1576` | `SetProcessDpiAwarenessContext +0x3a/+0x41` ← `InitProcessDpiAwareness +0xd5/+0xda`·`+0x187/+0x18c`·(`SetProcessDpiAwarenessInternal +0x25` 꼬리) | IFEO `DpiAwareness` 가 있거나, 매니페스트 dpi 요소가 있거나, **`psi+0x8bc` 비트 `0x20` 이 켜져 있고 시스템 DPI < 121**, 또는 TEB `+0x8e0` 비트 `0x80` | 컨텍스트 값 (`-1`·`-2`·`-3`·`-4`·`-6`) | `DllMain` 은 안 봄 |
  | `NtUserGetDpiForCurrentProcess` | `0x1440` | `GetDpiForCurrentProcess +0x28/+0x2f` (캐시 `gPackedProcessDpiInfo` rva `0xdac30` 가 비었을 때) ← `InitProcessDpiAwareness +0x16f/+0x174` | `psi+0x8bc` 비트 `0x20` 켜짐 | 없음 | — |
  | `NtUserDisableProcessWindowFiltering` | `0x13ec` | `+0xa17/+0xa1e` | 매니페스트 `disableWindowFiltering` = `true` | 없음 | — |
  | `NtUserSetProcessMousewheelRoutingMode` | `0x1579` | `+0xa80/+0xa87` | 매니페스트 `forceFocusBasedMouseWheel` = `true` | `1` | — |
  `psi` 를 0 으로 채우고 매니페스트·IFEO 가 없고 TEB `Win32ClientInfo` 가 0 이면 `+0x8bc` 비트가 0 → `InitProcessDpiAwareness` 는 `uVar5 = 0` 이고 TEB `+0x800` 비트 29·`+0x8e0` 비트 `0x80` 도 0 이라 `SetProcessDpiAwarenessContext` 를 안 부르고 `return` → **위 다섯 모두 안 부른다.** 이 판단은 디컴파일 읽기 `[정적]`이고 실제 갈래는 동적. (TEB `+0x800` 비트 29 가 켜져 있으면 `-3` 로 `SetProcessDpiAwarenessContext` 를 부르는 쪽으로 떨어진다.)
- 사용자가 예상한 **`NtUserProcessConnect` (`0x10e2`)·`NtUserInitializeClientPfnArrays` (`0x14aa`)** 는 `DllMain` 에 없다. 이 둘을 가리키는 IAT 칸(`0xb63f8`, `0xb63f0`) 을 읽는 코드는 user32 전체에서 **`ClientThreadSetup` 한 곳** (`+0x120/+0x127` `NtUserProcessConnect(-1, 0x240, buf)`, `+0x1fc` `RtlRetrieveNtUserPfn`, `+0x228/+0x22f` `NtUserInitializeClientPfnArrays(pfn 표 3개, hmodUser)`) 뿐이고 (`_refs/0xb63f8.tsv`·`0xb63f0.tsv`), 그 블록은 `gfServerProcess != 0` (`+0xdd`, `+0x1b9`) 이고 `gfFirstThread != 0` 일 때만. 실패면 `RtlSetLastWin32Error(0xe)` 후 FALSE 반환 (`+0x23d`, `+0x256`). 보통 프로세스는 이 블록을 안 탄다 → **보통 프로세스의 `gSharedInfo` 는 csrss 답(위 (a))에서만 온다.**

#### (d) `DllMain` 이 FALSE 를 돌려주는 자리 (전부 `_UserClientDllInitialize`, `.asm` 에서 목적지 `0x18006c435` = `+0x9b1` `XOR EAX,EAX` 확인)

| # | 점프 자리 | 조건 |
|---|---|---|
| 1 | `+0x193 JL` | 임계구역 10개 초기화 상태를 OR 한 값이 음수 |
| 2 | `+0x1a7 JS` | `InitializeUMHandleList` #1 이 음수 |
| 3 | `+0x1bb JS` | `InitializeUMHandleList` #2 가 음수 |
| 4 | `+0x1df JS` | `NtQuerySystemInformation(0)` 이 음수 |
| 5 | `+0x1fa JZ` | `InitializeNtdllUserPfn` 이 0 (ntdll `RtlInitializeNtUserPfn`/`RtlRetrieveNtUserPfn` 실패, 이미 한 번 불렸어도 실패) |
| 6 | `+0x2b8 JS` | `CsrClientConnectToServer` 가 음수 |
| 7 | `+0x2fe JNZ` | 보호 프로세스(PEB `+3` 비트 `0x2` 켜짐, `0x40` 꺼짐) 인데 PEB `+0x58` 이 이미 0 이 아님 |
| 8 | `+0x3df JZ` | `InitializeCsrUserPfn` 이 0 (`gfServerProcess != 0` 일 때만) |
| 9 | `+0x4b5 JZ` | `g_systemCallFilterId == 5` 갈래에서 `RtlAllocateHeap(0x498)` 이 NULL |
| 10 | `+0x74a JZ` | UserModeMenus 기능 켜짐 갈래에서 `RtlAllocateHeap(8)` 이 NULL |
| 11 | `+0x9af JNS` 안 탄 쪽 → `+0x9b1` | `GdiDllInitialize` 가 음수 |

- 위에서 `psi` 가 널이어도 FALSE 가 아니라 **`+0x38e` 에서 접근 위반** 이다 (검사 없음). `CsrClientConnectToServer` 성공 + 널 `psi` 는 `DllMain` 이 FALSE 를 못 돌려주고 터진다.
- 반환값이 `GdiDllInitialize` 가 음수가 아닌지만 보므로, gdi32 쪽이 `FALSE`(0) 를 줘도 user32 `DllMain` 은 `TRUE` 다.

**지금 우리 (PE_emulator) 맞나·틀리나**
- 틀림(바로잡을 점) 하나: "user32 `DllMain` → `NtOpenSection(SharedSection)`" 은 정상 길이 아니다. 정상 길은 user32 가 `CsrClientConnectToServer(…, 3, …)` → `CsrpClientConnectToServer` → `NtAlpcSendWaitReceivePort` 한 번이다. `NtOpenSection` 이 보였다면 `CsrPortHandle == 0` (kernelbase 의 BASESRV 연결이 안 되었거나 우리가 그 호출을 건너뜀) 쪽을 먼저 의심.
- 맞음: 실패 이름 `0xc0000142` (`STATUS_DLL_INIT_FAILED`) 는 `DllMain` 이 FALSE 를 돌려줄 때 로더가 쓰는 값이라 위 표 11군데 중 하나와 이어진다 (`NtOpenSection` 갈래면 표 6번).

### 질문 2. 공유 데이터 최소 칸

- 어디서 받나: **csrss 답 안 `+8` 부터 0x240B → `gSharedInfo`** (user32 가 내보내는 이름 `gSharedInfo`, rva `0xda350`, `ClientThreadSetup` 서버 갈래도 같은 모양). `gpsi` (rva `0xda590`) ← `gSharedInfo` 첫 8B (`+0x380 ~ +0x387`). 공유 섹션 `SharedSection` 뷰의 오프셋으로 받는 게 **아니다** (`NtMapViewOfSection` 은 ntdll 첫 연결 길에만; 답 안의 `psi` 포인터가 가리키는 곳이 그 데스크톱 힙 뷰일 가능성이 높지만 정적으로는 못 정함 `[추측]`).
- `gHandleTable`·`gpsiDisplay` 는 user32 PDB 공개 심볼에 없음 (`gpsi`·`gSharedInfo`·`gfServerProcess`·`hmodUser`·`gHighestUserAddress`·`pUserHeap`·`?gfFirstThread@@3HA`·`?pdiLocal@@3PEAUtagDESKTOPINFO@@EA`·`?gpClientProcInfo@@3PEAUtagCLIENTPROCINFO@@EA` 만 확인). 핸들 표는 `gSharedInfo` 안의 칸 (`aheList` 류 `[추측]`, `HMValidate*` 가 읽음; 창 없는 프로세스의 `DllMain` 은 안 읽음).

**데이터 칸 표 (`DllMain` 이 읽고 쓰는 것만)**

| 어디 | 칸 | 크기 | 쓰는 줄 / 읽는 줄 | 뜻 | 없으면 |
|---|---|---|---|---|---|
| csrss 답 버퍼 (호출자 스택 `RSP+0x60`) | `+0x0 ~ +0x7` | 8 | `DllMain` 안 읽음 | `[추측]` 개수/버전 | — |
| 같음 | `+0x8 ~ +0x247` | 0x240 | 읽기 `+0x30b ~ +0x37c` (복사) | `SHAREDINFO` 모양 | `gSharedInfo` 가 0 → `psi` 널 → `+0x38e` 접근 위반 |
| `gSharedInfo` (user32 rva `0xda350`) | `+0x0` | 8 | 쓰기 `+0x31f`, 읽기 `+0x380` | `psi` (`SERVERINFO*`, 읽기 가능한 메모리) `[이름 추측]` | 위와 같음 |
| 같음 | `+0x8 ~ +0x23f` | 0x238 | 쓰기만 (복사), `DllMain` 은 안 읽음 | `aheList`·`HeEntrySize`·`ulSharedDelta`·창 메시지 표 … `[이름 추측]` | `DllMain` 은 통과. 창 API 쓸 때 필요 → 다음 바퀴 |
| `gpsi` (rva `0xda590`) | 8 | | 쓰기 `+0x387`, 읽기 `InitProcessDpiAwareness +0x9f`·`GetDpiForCurrentProcess +0x6` | `psi` 사본 | |
| `psi` | `+0x0` dword 비트 `0x4` | 4 | `+0x38e TEST byte ptr [RAX],4` | 켜지면 IME 표 로드 (`_InitializeImmEntryTable`·`GetImmFileName`·`GetModuleHandleW`) `[의미 추측]` | 0 이면 IME 건너뜀 (최소) |
| `psi` | `+0x8bc` byte 비트 `0x20` | 1 | `InitProcessDpiAwareness +0xa8` | 켜지면 `GetDpiForCurrentProcess` 를 불러 DPI 판정 → win32k syscall 가능성 | 0 이면 DPI 쪽 win32k syscall 안 감 |
| `psi` | `+0x1b66` word | 2 | `GetDpiForCurrentProcess +0x13` (위 비트가 켜졌을 때만) | 시스템 DPI 캐시 `[이름 추측]` | |
| `gHighestUserAddress` (rva `0xda9f8`) | | 8 | 쓰기 `+0x1ec` ← `NtQuerySystemInformation` 버퍼 `+0x28` | `MaximumUserModeAddress` | |
| `gfServerProcess` (rva `0xda9e8`) | | 4 (ntdll 은 1B 만 씀) | 쓰기: ntdll `CsrClientConnectToServer +0xc7`/`+0x1cb`; 읽기 `+0x2be`·`+0x568`·`InitProcessDpiAwareness` 등 | 0 = 보통 프로세스 | 1 이면 서버 갈래 전부 |
| PEB `+0x58` (`KernelCallbackTable`/`UserSharedInfoPtr`) | | 8 | 쓰기 `+0x3fc` ← `apfnDispatch` (`0xb4720`, 0x498B 표; `__ClientThreadSetup` 은 `+0x2a0` = 84번째 칸, rva `0xb49c0`) | 커널→유저 콜백 표 | 커널이 `__ClientThreadSetup` 을 못 부름 |
| PEB `+0x230` (`PostProcessInitRoutine`) | | 8 | 쓰기 `+0x409` ← 0 | | |
| PEB `+0x7d0` (`Win32ClientProcInfo`) | | 8 | 쓰기 `+0xae` ← 0; UserModeMenus 켜짐이면 `+0x760` ← `gpClientProcInfo` | | |
| PEB `+0x2c0` (`SessionId`) | | 4 | 읽기 `+0x9f` | 세션 번호 → 객체 디렉터리 이름 | |
| PEB `+0x90` (`SharedData`) | | 8 | 읽기 ntdll `RtlGetCurrentServiceSessionId +0x0 ~ +0x17` | 첫 dword = `ServiceSessionId` (`_SILO_USER_SHARED_DATA +0x0`) | 널이면 서비스 세션 = 0 |
| PEB `+0x30` (`ProcessHeap`) | | 8 | 읽기 `+0x423` → `pUserHeap` (rva `0xdaa10`) | | |
| PEB `+0x2d0` (`AppCompatFlagsUser`) | | 8 | 읽고 쓰기 `InitProcessGdiScaling`·`InitProcessSystemDpiBehavior`·`InitProcessDpiAwareness` | DPI/GDI 스케일 비트 | |
| PEB `+0x3` 비트 `0x2`·`0x40` | | 1 | 읽기 `+0x210`·`+0x21f`·`+0x2d9`·`+0x2eb` | 보호 프로세스(PPL 아님) 여부 | 0 이면 보통 갈래 |
| TEB `+0x800` (`Win32ClientInfo`) 비트 29, `+0x8e0` 비트 `0x10`·`0x80` | | | 읽기 `InitProcessDpiAwareness +0xc1` 근처 | GUI 변환 전엔 0 | |
| `gfFirstThread` (rva `0xd996c`) | | 4 | 처음 값 `1` (파일 안), `ClientThreadSetup +0x82/+0x8c` 가 읽고 0 으로 | 첫 GUI 스레드 표시 | |

**지금 우리 맞나·틀리나**
- 우리가 `SharedSection` 만 채우는 쪽이라면 **그것만으론 모자라 보인다**: user32 `DllMain` 이 필요로 하는 건 "`NtAlpcSendWaitReceivePort` 답 안의 `psi` 포인터 + 그 포인터가 가리키는 `0x1b68`B 이상 읽기 가능한 메모리" 다 (`SharedSection` 뷰와 `psi` 가 같은 메모리인지는 정적으로 못 정함). `NtOpenSection` 에서 멈춘 건 그보다 앞 문제다 (질문 1(b)).

### 질문 3. GUI 스레드로 바뀌는 때

- `DllMain` 코드 안에 TEB `Win32ThreadInfo` (`+0x78`) 를 쓰는 줄은 **없다** (`_UserClientDllInitialize.asm`, `ClientThreadSetup.asm` 에서 `+0x78` 쓰기 검색 결과 0). `Win32ThreadInfo` 를 채우는 건 user32 가 아니라 win32k 쪽 `[추측: win32kbase.sys 의 스레드 콜아웃; sast/48 도 같은 쪽으로 짐작]`.
- 바뀌는 트리거 = **그 스레드의 첫 win32k 시스템 호출 (번호 `0x1000` 이상)**. 커널 쪽 이름만: `KiConvertToGuiThread +0x4` 가 `PsConvertToGuiThread` 를 부름 (`ms/disassemble/ntoskrnl/KiConvertToGuiThread.asm`; sast/45 가 `PsConvertToGuiThread +0x1e0 / +0x238` 에서 win32k 콜아웃을 부른다고 읽음 `[정적, 커널]`).
- 변환 뒤 user32 가 불리는 방식: **`__ClientThreadSetup` (rva `0x4f7d0`) 은 끝에서 `NtCallbackReturn` (`[ntdll] 5`) 을 부른다** → 커널이 `KeUserModeCallback` 으로 부른 콜백이다. 이 함수의 주소가 `apfnDispatch` 의 `+0x2a0` 칸(rva `0xb49c0`, 84번째) 에 들어 있다 `[정적: refs]`. 그 안 `ClientThreadSetup` (`0x4f810`) 이 하는 일 (보통 프로세스, `gfServerProcess == 0`, 이 프로세스에서 첫 GUI 스레드; 오프셋은 `ClientThreadSetup.asm`): `+0x93/+0x9a` `GdiProcessSetup` (gdi32full), `+0x272/+0x279` `NtUserRemoteConnectState` (`[win32u] 0x151f`), `+0x285/+0x28a` `InitClientDrawing`, `+0x29c/+0x2a3` `NtUserGetThreadDesktop` (`0x107b`), `+0x2c0/+0x2c7` `NtUserRegisterLPK` (`0x150a`, `InitializeLpkHooks` 가 정한 값이 있을 때), `+0x2f8/+0x2ff` `RtlAllocateHeap(pUserHeap,8,0x48)` → `pdiLocal`, `+0x356/+0x35d` `NtUserGetProcessUIContextInformation` (`0x1475`), `+0x374/+0x37b` `LoadAppInitDlls` (`gfServerProcess == 0 && gfLogonProcess == 0`), UserModeMenus 기능이면 `win32u!NtUserInitializeUserModeMenus` (`0x14ae`) 를 `GetProcAddress` 로 찾아 CFG 호출 (`+0x3c2`, `+0x3e8`). TEB 쪽은 `Win32ClientInfo` (`+0x800`) 비트 `8` 켜기 (`+0xca` 에서 이미 켜져 있으면 FALSE(0) 반환 = 같은 스레드에서 두 번째 콜백), `+0x820` ← `pdiLocal`, `+0x8d0`·`+0x910`·`+0x918` 쓰기 (칸 이름은 CLIENTINFO 타입이 없어 `[추측]`).
- `user32 DllMain` 이 `DllMain` 안에서 win32k syscall 을 부르면 그때 바뀌나: **바뀐다** (그 스레드의 첫 win32k syscall 이므로). 위 질문 1(c) 의 다섯 syscall 은 조건부라 `psi` 를 0 으로 채운 보통 프로세스에선 **user32 코드가 직접 부르지 않는다.** 하지만 표 18번 `GdiDllInitialize(+0x996)` 는 무조건 부른다 — gdi32 쪽이 `NtGdi*` (예: `NtGdiInit` `[win32u] 0x12ed`) 를 부르는지는 **이번엔 안 봄 → Q150b**. gdi32!`GdiDllInitialize` (rva `0xa6e0`) 는 `ext-ms-win-gdi-internal-uap-init-l1-1-0` 지연 가져오기 칸 (`0x1800280e0`) 으로 `JMP` 하는 껍데기이다 (gdi32.dll 를 `objdump` 로 6바이트 읽음). 그 API 세트가 gdi32full 로 풀려 같은 이름 (gdi32full 내보내기: `GdiDllInitialize` rva `0x82070`, `GdiDllInitializeWrapper` `0x71030`) 으로 이어진다고 읽히지만 `[추측]`, API 세트 해석은 이번에 안 봄.

**지금 우리 맞나·틀리나**: 정보 없음 — PE_emulator 쪽에 `Win32ThreadInfo`/GUI 스레드 흉내가 있는지는 모름. 알아둘 점: **win32k syscall 이 하나라도 부르면 그 스레드에서 `__ClientThreadSetup` 콜백이 user→kernel→user 로 돌아와야** 하므로, 엔진이 "win32k 첫 호출 → 콜백 (`KernelCallbackTable[84]`) 부르기"를 못 하면 `ClientThreadSetup` 이 돌지 않아 `pdiLocal`·TEB `+0x820` 등이 안 채워진다 `[추측]` (우리 쪽에 그 흉내가 있는지는 모름).

### 질문 4. win32u syscall 번호표 (이번 길)

전체 1499개는 `ms/disassemble/win32u/_syscalls.tsv`. 이번 길에 나온 것만 (스텁 모양은 전부 `MOV R10,RCX; MOV EAX,imm; TEST byte ptr [0x7ffe0308],1; JNZ→INT 0x2e; SYSCALL; RET`, 24B):

| 번호 | 이름 | win32u rva | 이번 길에서 |
|---|---|---|---|
| `0x1000` | `NtUserGetThreadState` | `0x12b0` | (참고) 가장 낮은 번호 = 흔한 첫 win32k 호출 후보 |
| `0x107b` | `NtUserGetThreadDesktop` | `0x2210` | `ClientThreadSetup +0x29c` |
| `0x10e2` | `NtUserProcessConnect` | `0x2ef0` | `ClientThreadSetup +0x120`, **서버 프로세스 갈래만** |
| `0x12ed` | `NtGdiInit` | `0x7050` | gdi32 쪽 후보 (Q150b) |
| `0x12ee` | `NtGdiInit2` | `0x7070` | 같음 |
| `0x13ec` | `NtUserDisableProcessWindowFiltering` | `0x9030` | `DllMain +0xa17` (조건부) |
| `0x1440` | `NtUserGetDpiForCurrentProcess` | `0x9ab0` | `GetDpiForCurrentProcess +0x28` (조건부) |
| `0x1475` | `NtUserGetProcessUIContextInformation` | `0xa150` | `ClientThreadSetup +0x356` |
| `0x14a6` | `NtUserInitAnsiOem` | `0xa770` | `InitOemXlateTables +0x12f` (서버 갈래만) |
| `0x14aa` | `NtUserInitializeClientPfnArrays` | `0xa7f0` | `ClientThreadSetup +0x228`, 서버 갈래만 |
| `0x14ae` | `NtUserInitializeUserModeMenus` | `0xa870` | `ClientThreadSetup` 이 `GetProcAddress` 로 찾아 부름 (기능 켜짐일 때) |
| `0x150a` | `NtUserRegisterLPK` | `0xb3f0` | `ClientThreadSetup +0x2c0` (조건부) |
| `0x151f` | `NtUserRemoteConnectState` | `0xb690` | `ClientThreadSetup +0x272` |
| `0x1576` | `NtUserSetProcessDpiAwarenessContext` | `0xc170` | `SetProcessDpiAwarenessContext +0x3a` (조건부) |
| `0x1579` | `NtUserSetProcessMousewheelRoutingMode` | `0xc1d0` | `DllMain +0xa80` (조건부) |
| `0x157b` | `NtUserSetProcessUIAccessZorder` | `0xc210` | `DllMain +0x5d2` (조건부) |

- ntdll 쪽 번호 (같은 길): `NtQuerySystemInformation 0x36`, `NtOpenKey 0x12`, `NtQueryValueKey 0x17`, `NtClose 0xf`, `NtProtectVirtualMemory 0x50`, `NtCallbackReturn 0x5`, **`NtAlpcSendWaitReceivePort 0x8e`** (ntdll rva `0x161f60`). 첫 연결 길(질문 1(b)): `NtOpenSection 0x37`, `NtCreateSection 0x4a`, `NtMapViewOfSection 0x28`, `NtConnectPort 0xa4`.
- 이름표 팁: 번호는 스텁에 박힌 `MOV EAX,imm32` 값 그대로다. 스텁은 모두 `4C 8B D1 B8 <imm32>` 로 시작하는 24B 라서, ntdll `Nt*` 이름표 만들 때와 같은 방법으로 읽으면 된다 (`_syscalls.tsv` 가 그 결과, 번호순).

**지금 우리 맞나·틀리나**: 맞음 — 우리 `[추측]` 대로 win32u 번호는 `0x1000` 대(`0x1000 ~ 0x15da`)이고, 이름은 위 `_syscalls.tsv` 로 붙일 수 있다.

## 동적분석으로 확인해야 할 것 (Q104 에 붙임)

- user32 `DllMain` 시점의 ntdll `CsrPortHandle` (rva `0x1d4e20`) — 0 이 아닌지 (kernelbase 가 BASESRV 연결을 마쳤나). 0 이면 user32 가 `CsrpConnectToServer` 첫 연결 길로 들어가 `NtOpenSection` 이 나온다. 멈춤점 후보: `user32!_UserClientDllInitialize +0x2aa` (call) 에서 `dq ntdll+0x1d4e20 L1`.
- `NtAlpcSendWaitReceivePort` (ntdll `CsrClientCallServer +0xed/+0xf2`) 가 받은 실제 답: 메시지 `+0x34` (Status), 캡처 버퍼 안 0x248B 의 `psi` 포인터 값과 그 가리키는 곳의 `+0x0`·`+0x8bc`·`+0x1b66`. 멈춤점 후보: `ntdll!CsrpClientConnectToServer +0x9a` (복사 직후).
- `gfServerProcess` 가 실제로 0 인지 (`user32!gfServerProcess`), `gSharedInfo` 에 받은 0x240B 덤프.
- `psi+0x8bc` 비트 `0x20` 이 정말 켜져 있나 → `InitProcessDpiAwareness` 가 `NtUserSetProcessDpiAwarenessContext`/`NtUserGetDpiForCurrentProcess` 를 부르는지 (user32 `DllMain` 안 첫 win32k syscall 후보).
- 이 프로세스의 첫 win32k syscall 이 `DllMain` 안인지 (아마 gdi32 `GdiDllInitialize` 또는 DPI 쪽), 그때 `__ClientThreadSetup` 이 `KernelCallbackTable[84]` 로 불리는지, TEB `+0x78` 이 언제 채워지는지. 멈춤점 후보: `user32!ClientThreadSetup` 입구, `nt!PsConvertToGuiThread`, 조건 `thread == test.exe`.
- `gdi32full` 이 `DllMain` 시점에 이미 올라와 있나 (gdi32 → gdi32full 지연 가져오기라 `GdiDllInitialize +0x996` 에서 처음 로드될 수 있음).
- `g_systemCallFilterId` 가 5 로 바뀌는지 (`+0x46a` 직후 `v & 0xf`) — 5 면 `apfnDispatch` 복사 갈래.
- UserModeMenus WIL 기능이 켜졌는지 (`__private_IsEnabled` 결과) — 켜지면 PEB `+0x7d0` 이 채워지고 `NtUserInitializeUserModeMenus` 가 불림.

## Q150b 로 넘길 것 (gdi32 쪽)

- 표 18번 **`GdiDllInitialize(hmodUser32, 1)` (user32 `+0x996/+0x99d`)** — gdi32 (`0xa6e0`) → gdi32full (`0x82070`). 알고 싶은 것: ① 안에서 부르는 `NtGdi*` syscall(`NtGdiInit 0x12ed` 가 보임) 이 있나, 있으면 **그게 이 스레드의 첫 win32k syscall 이라 `__ClientThreadSetup` 콜백이 돌아야 한다**, ② 공유 GDI 핸들 표(PEB `GdiSharedHandleTable`) 를 어디서 받나, ③ 음수를 돌려주는 자리 (user32 는 음수만 FALSE 로 봄; 0 도 통과), ④ 같은 함수가 gdi32 `DllMain` 과 user32 `DllMain` 에서 두 번(다른 `hmod`) 불리므로 두 번째 호출이 무엇을 건너뛰나.
- `ClientThreadSetup +0x93/+0x9a` **`GdiProcessSetup`** (user32 가 gdi32 에서 가져오는 이름, user32 IAT `0xb4fd8`; gdi32 쪽은 `ext-ms-win-gdi-internal-desktop-l1-1-0` 지연 가져오기 칸 `0x27b80` 으로 넘김 — gdi32 가져오기 표에서 확인).
- 최소 조건 표 재료: 이번 결과서 질문 1 표(차례·syscall·답), 질문 2 표(데이터 칸), 질문 1(d) FALSE 목록. 요청서 질문 5 의 "창 안 만드는 프로세스" 는 `psi` 가 읽기 가능 0 채움 + csrss 답 + 아래 후보를 합쳐서.

## 다음 요청 후보 (더 깊이 볼 것)

1. `SHAREDINFO`/`SERVERINFO` 칸 이름 — PDB 에 타입이 없어서 `win32kbase.sys`/`win32kfull.sys` 쪽 `NtUserProcessConnect` 구현(`xxxUserProcessConnect` 류)을 보면 `psi` 를 어떻게 채우는지, `+0x8bc`·`+0x1b66`·`+0x0` 이 뭔지 알 수 있다 (사용자 지정 "win32k 안쪽 깊이 안 넣음" 에서 빠졌으므로 별도 결정).
2. csrss(`winsrv`/`csrsrv`) 가 USERSRV 연결 요청 (`CsrSrvClientConnect`, ApiNumber 0, 서버 번호 3) 에서 답을 만드는 쪽 — 사용자가 이미 `csrsrv` 는 봤으므로 (run71) `CsrSrvClientConnect` 와 USERSRV 연결 함수만.
3. 창 하나 안 만드는 프로세스가 흔히 부르는 user32 API (`GetSystemMetrics`·`MessageBox`·`GetDpiForSystem`·`GetKeyboardLayout` 등) 가 읽는 `psi` 칸 (`RealGetSystemMetrics` 가 `gpsi` 읽는 곳 6곳 등, `ms/disassemble/user32/_refs/0xda590.tsv` 368줄).
4. `ClientThreadSetup` 의 `InitClientDrawing`·`LoadAppInitDlls`(kernelbase `api-ms-win-core-appinit`)·`NtUserRegisterLPK` 깊이.
5. `InitializeLpkHooks` (`0x501e0`) 가 `DAT_1800dac34` (LPK 사용 표시) 를 언제 정하나 — 이 값이 `ClientThreadSetup` 의 `NtUserRegisterLPK` 호출 조건.

## 판정

**분석성공 (최소 길 지도 수준)**. 요청서 질문 1~4 에 모두 `.asm` 줄 근거와 함께 답했다. 단 ① 질문 2·3 의 칸 이름과 "커널이 누가 `Win32ThreadInfo` 를 쓰나" 는 PDB 타입이 없어 `[추측]`, ② 질문 3 의 "user32 `DllMain` 안에서 실제로 첫 win32k syscall 이 어디서 나는가" 는 gdi32 `GdiDllInitialize` 에 달려 있어 Q150b 와 동적 확인이 필요하다.
